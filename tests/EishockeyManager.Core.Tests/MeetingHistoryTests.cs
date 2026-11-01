using System.Text.Json.Nodes;
using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur optionen-wirkung, Teil 3: bisherige Begegnungen der Infoleiste nach em.exe 0x4F5A8 (Eintrag) und 0x43E21 (Anzeige).
/// Vorher las das Remake nur die Ligaergebnisse der laufenden Saison, chronologisch, und mischte Heim- und Auswärtsspiele.
/// </summary>
public class MeetingHistoryTests
{
    private static int[] Table() => MeetingHistory.NewTable();

    [Fact]
    public void Entries_ShowAwayGamesFirst_NewestFirst_TwoPerSide()
    {
        // Reihenfolge der Spiele: H 4:0, A 5:0, H 8:4, A 2:1, A 5:0 → Bild folge06_00459 zeigt „A 5:0,A 5:0,H 4:0,H 8:4“
        // genau in dieser Form (Auswärts zuerst, je Seite zwei).
        var t = Table();
        MeetingHistory.Record(t, 7, home: true, 8, 4);
        MeetingHistory.Record(t, 7, home: false, 5, 0);
        MeetingHistory.Record(t, 7, home: true, 4, 0);
        MeetingHistory.Record(t, 7, home: false, 2, 1);
        MeetingHistory.Record(t, 7, home: false, 5, 0);

        Assert.Equal(["A 5:0", "A 2:1", "H 4:0", "H 8:4"], MeetingHistory.Entries(t, 7));
        Assert.Empty(MeetingHistory.Entries(t, 8));
    }

    [Fact]
    public void Record_FullSide_ShiftsAndKeepsNewestFive()
    {
        var t = Table();
        for (var g = 1; g <= 7; g++)
            MeetingHistory.Record(t, 3, home: true, g, 0);

        // Plätze 0–4 = Spiele 3–7; angezeigt die beiden neuesten.
        Assert.Equal(["H 7:0", "H 6:0"], MeetingHistory.Entries(t, 3));
        Assert.Equal(3, t[3 * 10 + 0 * 2 + 0] & 0xF);
        Assert.All(Enumerable.Range(0, 5), slot => Assert.Equal(MeetingHistory.Empty, t[3 * 10 + slot * 2 + 1]));
    }

    [Fact]
    public void Record_NationalTeams_UseOneListOfTen_WithoutHomeAwayPrefix()
    {
        var t = Table();
        var nation = MeetingHistory.FirstNation + 2;
        for (var g = 1; g <= 11; g++)
            MeetingHistory.Record(t, nation, home: g % 2 == 0, g % 16, 1);

        // Zehn Plätze am Stück (Seite 0, dann Seite 1); das elfte Spiel schiebt das erste hinaus.
        Assert.Equal(2, t[nation * 10] & 0xF);
        Assert.Equal(11, t[nation * 10 + 4 * 2 + 1] & 0xF);
        Assert.Equal(["11:1", "10:1", "6:1", "5:1"], MeetingHistory.Entries(t, nation));
    }

    [Fact]
    public void Record_StoresFourBitsPerNumber_LikeTheOriginal()
    {
        var t = Table();
        MeetingHistory.Record(t, 1, home: true, 16, 2); // 16 + 2 · 16 = 0x30 → „0:3“
        Assert.Equal(["H 0:3"], MeetingHistory.Entries(t, 1));
    }

    [Fact]
    public void RecordMeeting_WritesBothManagedClubs_FromTheirOwnView()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Eins", Head = 0, ClubIndex = 2 });
        setup.Managers.Add(new ManagerSetup { Name = "Zwei", Head = 1, ClubIndex = 5 });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 3);

        game.RecordMeeting(2, 5, 3, 1);
        game.RecordMeeting(9, 10, 3, 1); // ohne Manager: nichts

        Assert.Equal(["H 3:1"], MeetingHistory.Entries(game.Managers[0].MeetingTable, 5));
        Assert.Equal(["A 1:3"], MeetingHistory.Entries(game.Managers[1].MeetingTable, 2));
        Assert.All(game.Managers, m => Assert.Empty(MeetingHistory.Entries(m.MeetingTable, 9)));
        Assert.All(game.Managers, m => Assert.Empty(MeetingHistory.Entries(m.MeetingTable, 10)));
    }

    /// <summary>
    /// Regressionstest: Die Begegnungen gelten über alle Saisons (Original-Tabelle wird nie geleert; folge06_00459 zeigt am
    /// 41. Spieltag vier Begegnungen). Vorher: <c>GameState.Meetings</c> las nur <c>Results</c>, das zum Saisonstart geleert
    /// wird. Mehrsaisonprobe über vier Saisonwechsel; Speichern/Laden trägt die Tabelle weiter.
    /// </summary>
    [Fact]
    public void Meetings_SurviveFourSeasonChanges_AndSaveGame()
    {
        var game = TestGames.Create();
        var manager = game.ActiveManager;
        var counts = new List<int>();
        for (var change = 0; change < 4; change++)
        {
            PlayToSeasonEnd(game);
            counts.Add(manager.MeetingTable.Count(v => v != MeetingHistory.Empty));
            game.StartNextSeason();
            Assert.Equal(counts[^1], manager.MeetingTable.Count(v => v != MeetingHistory.Empty));
        }

        Assert.True(counts[0] > 0);
        Assert.True(counts[1] > counts[0], string.Join(",", counts)); // 2. Saison kommt dazu, nichts wird gelöscht
        Assert.All(Enumerable.Range(0, MeetingHistory.Opponents), o => Assert.InRange(MeetingHistory.Entries(manager.MeetingTable, o).Count, 0, 4));
        Assert.Empty(MeetingHistory.Entries(manager.MeetingTable, manager.ClubIndex));

        var loaded = SaveGame.Load(SaveGame.Save(game));
        Assert.True(loaded.Success, loaded.Error);
        Assert.Equal(manager.MeetingTable, loaded.Game!.ActiveManager.MeetingTable);
    }

    [Fact]
    public void Migration_FromSchema18_StartsWithEmptyTables()
    {
        var game = TestGames.Create();
        var json = JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        json["schema"] = 18;
        foreach (var m in json["manager"]!.AsArray())
            Assert.True(m!.AsObject().Remove("MeetingTable"));

        var result = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(json.ToJsonString()));
        Assert.True(result.Success, result.Error);
        Assert.Equal(SaveGame.Save(game), SaveGame.Save(result.Game!));
    }

    private static void PlayToSeasonEnd(GameState game)
    {
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 1000, "Saisonende nicht erreicht");
            foreach (var m in game.Managers)
                m.Money = 10_000_000; // keine Entlassung im passiven Testlauf
            game.Continue();
        }
    }
}
