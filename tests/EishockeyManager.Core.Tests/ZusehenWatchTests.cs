using System.Text;
using System.Text.Json.Nodes;
using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur zusehen, Teil 1 (F018, A445): Markierung zum Zusehen, Schalter em.exe Datei 0x0AE2B (<c>GameState.Watch.cs</c>).
/// Vor dem Teil rot: <see cref="Club.WatchMode"/>, <see cref="GameState.SetWatchMode"/> und <see cref="GameState.ToggleWatch"/>
/// fehlten (Build-Fehler); die Live-Anzeige nahm fest die Managervereine (<c>MatchEngine.IsManaged</c>).
/// </summary>
public sealed class ZusehenWatchTests
{
    private static GameState Game(int seed, params int[] managerClubs)
    {
        var setup = new GameSetup();
        foreach (var club in managerClubs)
            setup.Managers.Add(new ManagerSetup { Name = $"M{club}", Head = 0, ClubIndex = club });
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    /// <summary>[ORIGINAL] Spielstart Datei 0x298EA/0x29C1C: je Manager 0x0AE2B(Verein, 1, 2); alle anderen Vereine Modus 0 (Lader 0x2EFA8).</summary>
    [Fact]
    public void NewGame_MarksManagerClubsLive()
    {
        var game = Game(1, 2, 5);
        Assert.Equal([2, 5], game.Clubs.Where(c => c.WatchMode != 0).Select(c => c.Index).Order());
        Assert.All(game.Managers, m => Assert.Equal(GameState.WatchLive, game.Clubs[m.ClubIndex].WatchMode));
    }

    /// <summary>[ORIGINAL] Dritter Knopf Datei 0x4E440–0x4E46A: 0 → 1 → 2 → 0.</summary>
    [Fact]
    public void Toggle_CyclesThroughModes()
    {
        var game = Game(1, 2);
        var club = game.Clubs[7];
        Assert.Equal(1, game.ToggleWatch(club));
        Assert.False(club.WatchedLive);
        Assert.Equal(2, game.ToggleWatch(club));
        Assert.True(club.WatchedLive);
        Assert.Equal(0, game.ToggleWatch(club));
        Assert.Equal(0, club.WatchMode);
    }

    /// <summary>
    /// [ORIGINAL] Datei 0xAE99/0xAF3D: Erreicht der Zähler der Vereine im Modus 2 genau vier, lehnt 0x0AE2B ohne Verdrängen ab
    /// (Modus 0) – das Vereinsfenster verdrängt nur bei Vereinen mit Manager (Vereinsfeld 0x16).
    /// </summary>
    [Fact]
    public void FifthLiveClub_WithoutManager_IsRejected()
    {
        var game = Game(1, 2);
        foreach (var c in new[] { 7, 9, 11 })
            Assert.Equal(2, game.SetWatchMode(game.Clubs[c], evict: false, GameState.WatchLive));
        Assert.Equal(4, game.Clubs.Count(c => c.Index < NationalTeams.First && c.WatchedLive));

        Assert.Equal(1, game.ToggleWatch(game.Clubs[13]));
        Assert.Equal(0, game.ToggleWatch(game.Clubs[13])); // 1 → 2 abgelehnt → 0
        Assert.Equal(0, game.Clubs[13].WatchMode);
        Assert.Equal(4, game.Clubs.Count(c => c.Index < NationalTeams.First && c.WatchedLive));
    }

    /// <summary>
    /// [ORIGINAL] Datei 0xAEB4–0xAF2E: mit Verdrängen geht der erste Verein davor im Modus 2 ohne Manager auf Modus 1; ein schon
    /// markierter Verein zählt mit. Länder bilden eine eigene Gruppe (Datei 0xAE41–0xAE53).
    /// </summary>
    [Fact]
    public void Evict_DemotesFirstUnmanagedLiveClubBefore()
    {
        var game = Game(1, 20);
        foreach (var c in new[] { 3, 4, 6 })
            game.SetWatchMode(game.Clubs[c], evict: false, GameState.WatchLive);
        // Verein 20 (Manager) ist der vierte im Modus 2: setzt man ihn neu, verdrängt er Verein 3.
        Assert.Equal(2, game.SetWatchMode(game.Clubs[20], evict: true, GameState.WatchLive));
        Assert.Equal(1, game.Clubs[3].WatchMode);
        Assert.Equal([4, 6, 20], game.Clubs.Where(c => c.Index < NationalTeams.First && c.WatchedLive).Select(c => c.Index));
        // Länder zählen getrennt: vier Vereine im Modus 2 hindern kein Land.
        game.SetWatchMode(game.Clubs[3], evict: false, GameState.WatchLive);
        Assert.Equal(2, game.SetWatchMode(game.Clubs[NationalTeams.First + 1], evict: false, GameState.WatchLive));
    }

    /// <summary>Ohne Verein zum Verdrängen (alle mit Manager) bleibt der Modus 2 stehen ([ORIGINAL] Datei 0xAF30–0xAF3B).</summary>
    [Fact]
    public void Evict_WithoutCandidate_KeepsLive()
    {
        var game = Game(1, 2, 3, 4, 5);
        Assert.Equal(2, game.SetWatchMode(game.Clubs[9], evict: true, GameState.WatchLive));
        Assert.Equal(5, game.Clubs.Count(c => c.Index < NationalTeams.First && c.WatchedLive));
    }

    /// <summary>[ORIGINAL] Ernennung zum Bundestrainer Datei 0x55457: 0x0AE2B(Deutschland, 1, 2).</summary>
    [Fact]
    public void NationalCoach_MarksGermany()
    {
        var game = Game(1, 2);
        Assert.Equal(0, game.Clubs[NationalTeams.Germany].WatchMode);
        game.AppointNationalCoach(game.Managers[0]);
        Assert.True(game.Clubs[NationalTeams.Germany].WatchedLive);
    }

    /// <summary>
    /// Wirkung auf die Live-Anzeige ([ORIGINAL] Spielabrechnung Datei 0x54BF7–0x54C23, Vereinsfeld 0x34 Bit 7): Eine Paarung
    /// zweier Computervereine bekommt eine Reihe, wenn einer markiert ist; der abgeschaltete Managerverein bekommt keine.
    /// </summary>
    [Fact]
    public void Scoreboard_FollowsWatchMarks()
    {
        var game = Game(3, 2);
        var managed = game.Clubs[2];
        var opponents = game.Clubs.Where(c => c.Index != 2 && c.League == managed.League).ToList();
        game.ToggleWatch(opponents[0]);
        game.ToggleWatch(opponents[0]);
        MatchEngine.PlayWithEvents(game, opponents[0], opponents[1], MatchCompetition.League, knockOut: false, new Random(1));
        var row = Assert.Single(game.LiveScoreboard!.Rows);
        Assert.Equal((opponents[0].Index, opponents[1].Index), (row.HomeClub, row.AwayClub));

        Assert.Equal(0, game.ToggleWatch(managed)); // 2 → 0
        MatchEngine.PlayWithEvents(game, managed, opponents[2], MatchCompetition.League, knockOut: false, new Random(2));
        Assert.Single(game.LiveScoreboard!.Rows);
        Assert.Null(game.LiveScoreboard.RowOf(managed.Index));
    }

    /// <summary>
    /// Migration 51 → 52 (<c>SaveGame.Migration.Zusehen.cs</c>): Ein Stand ohne <c>WatchMode</c> lädt mit Modus 2 genau für die
    /// Vereine nicht entlassener Manager und das Land des Bundestrainers (die feste Regel vorher). Rot ohne den Eintrag
    /// <c>[51] = MigrateZusehen</c>; der strenge Leser lehnt das aktuelle Schema ohne das Feld ab.
    /// </summary>
    [Fact]
    public void Schema51_WithoutWatchMode_MarksManagedClubs()
    {
        var game = Game(1, 2, 5);
        game.AppointNationalCoach(game.Managers[1]);
        game.ToggleWatch(game.Clubs[9]);
        game.ToggleWatch(game.Clubs[9]);
        var saved = SaveGame.Load(SaveGame.Save(game));
        Assert.True(saved.Success, saved.Error);
        Assert.Equal(game.Clubs.Select(c => c.WatchMode), saved.Game!.Clubs.Select(c => c.WatchMode));

        var root = JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        foreach (var club in root["vereine"]!.AsArray().OfType<JsonObject>())
            Assert.True(club.Remove("WatchMode"));
        Assert.False(SaveGame.Load(Encoding.UTF8.GetBytes(root.ToJsonString())).Success);

        root["schema"] = 51;
        var result = SaveGame.Load(Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.True(result.Success, result.Error);
        Assert.Equal([2, 5, NationalTeams.Germany],
            result.Game!.Clubs.Where(c => c.WatchMode == GameState.WatchLive).Select(c => c.Index).Order());
        Assert.All(result.Game.Clubs.Where(c => c.Index is not (2 or 5 or NationalTeams.Germany)), c => Assert.Equal(0, c.WatchMode));
    }

    /// <summary>
    /// Mehrsaisonprobe (AGENTS.md §5; die Markierung hängt am Verein und wandert bei Platztausch und Ligawechsel mit): zwei
    /// Manager, Manager 0 passiv, Manager 1 aktiv (verlängert), vier Saisonwechsel. Die Managervereine bleiben im Modus 2 und
    /// bekommen in jeder Saison Reihen der Live-Anzeige; Speichern/Laden ist am Ende bytegleich.
    /// </summary>
    [Fact]
    public void MultiSeason_FourSeasonChanges_ManagerClubsStayWatched()
    {
        var game = Game(7, 2, 5);
        var active = game.Managers[1];
        for (var change = 0; change < 4; change++)
        {
            var rows = 0;
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 5000, "Saisonende nicht erreicht");
                foreach (var manager in game.Managers)
                    manager.Money = 10_000_000; // keine Entlassung
                TestSeason.KeepSquad(game, active, extendExpired: true);
                game.Continue();
                if (game.LiveScoreboard is { } board)
                    rows += board.Rows.Count(r => game.Managers.Any(m => r.HomeClub == m.ClubIndex || r.AwayClub == m.ClubIndex));
            }
            Assert.True(rows > 0);
            Assert.All(game.Managers, m => Assert.True(game.Clubs[m.ClubIndex].WatchedLive));
            game.StartNextSeason();
        }
        Assert.Equal(GameState.FirstSeason + 4, game.Season);
        Assert.Equal(2, game.Clubs.Count(c => c.Index < NationalTeams.First && c.WatchedLive));
        var loaded = SaveGame.Load(SaveGame.Save(game));
        Assert.True(loaded.Success, loaded.Error);
        Assert.Equal(SaveGame.Save(game), SaveGame.Save(loaded.Game!));
    }
}
