using System.Text.Json.Nodes;
using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur wettbewerbe-komplett Teil 2: Daten des Gegner-Fensters 0x128:0x2A (Rekorde 0x51133, Kopf-an-Kopf-Liste 0x4F5A8,
/// Bilanz, Serien) für Länder. Vorher führte das Remake keine dieser Daten; das Fenster fehlte ganz.
/// </summary>
public class GegnerFensterTests
{
    private const int A = 52, B = 53;

    private static (int[] Words, int[] Opponents) Records() =>
        (new int[TeamRecords.Teams * TeamRecords.Kinds], new int[TeamRecords.Teams * TeamRecords.Kinds]);

    private static int Word(int[] words, int club, TeamRecordKind kind) => words[TeamRecords.Index(club, kind)];

    /// <summary>
    /// [ORIGINAL] 0x51133 mit den Argumenten aus 0x50283: Heimsieg 5:2 → H-Sieg des Heimteams 0x0502 und A-Niederlage
    /// des Gastes 0x0502 (je mit Gegner); Tore H 0x0500, Gegentore H 2, Tore A 2, Gegentore A 0x0500.
    /// </summary>
    [Fact]
    public void HomeWin_FillsWinLossAndGoalRecords()
    {
        var (w, o) = Records();
        TeamRecords.Enter(w, o, A, B, 5, 2);
        Assert.Equal(0x0502, Word(w, A, TeamRecordKind.HomeWin));
        Assert.Equal(B, o[TeamRecords.Index(A, TeamRecordKind.HomeWin)]);
        Assert.Equal(0x0502, Word(w, B, TeamRecordKind.AwayLoss));
        Assert.Equal(A, o[TeamRecords.Index(B, TeamRecordKind.AwayLoss)]);
        Assert.Equal(0x0500, Word(w, A, TeamRecordKind.HomeGoalsFor));
        Assert.Equal(2, Word(w, A, TeamRecordKind.HomeGoalsAgainst));
        Assert.Equal(2, Word(w, B, TeamRecordKind.AwayGoalsFor));
        Assert.Equal(0x0500, Word(w, B, TeamRecordKind.AwayGoalsAgainst));
    }

    /// <summary>
    /// [ORIGINAL] Modus ≠ 0 vergleicht vorzeichenlos (<c>jnc</c>): Die Differenz einer Auswärtsniederlage ist als 16-Bit-Zahl
    /// größer als der leere Eintrag 0 – H-Niederlage und A-Sieg bleiben leer. Ein kleinerer Heimsieg verdrängt den größeren
    /// nicht, ein größerer schon.
    /// </summary>
    [Fact]
    public void AwayWin_IsNeverRecorded_AndOnlyLargerWinsReplace()
    {
        var (w, o) = Records();
        TeamRecords.Enter(w, o, A, B, 1, 7);
        Assert.Equal(0, Word(w, A, TeamRecordKind.HomeLoss));
        Assert.Equal(0, Word(w, B, TeamRecordKind.AwayWin));
        Assert.Equal(7, Word(w, B, TeamRecordKind.AwayGoalsFor));
        Assert.Equal(0x0100, Word(w, B, TeamRecordKind.AwayGoalsAgainst));
        TeamRecords.Enter(w, o, A, B, 4, 1);
        TeamRecords.Enter(w, o, A, B, 3, 1);
        Assert.Equal(0x0401, Word(w, A, TeamRecordKind.HomeWin));
        TeamRecords.Enter(w, o, A, B, 9, 2);
        Assert.Equal(0x0902, Word(w, A, TeamRecordKind.HomeWin));
    }

    /// <summary>
    /// [ORIGINAL] 0x4F5A8 für Länder: erst die geraden Plätze, dann die ungeraden; voll → die Folge 0, 2, 4, 6, 8, 1, 3, 5,
    /// 7, 9 rückt nach vorn, der neue Wert steht auf Platz 9. Byte = hi · 16 + lo.
    /// </summary>
    [Fact]
    public void HeadToHead_NationListIsAFifoOfTen()
    {
        var list = Enumerable.Repeat(GameState.HeadToHeadEmpty, 10).ToArray();
        for (var n = 1; n <= 12; n++)
            TeamRecords.EnterHeadToHead(list, 0, nation: true, side: 1, lo: n % 16, hi: 0);
        // Spiele 3 … 12 in der Reihenfolge 0, 2, 4, 6, 8, 1, 3, 5, 7, 9
        Assert.Equal([3, 8, 4, 9, 5, 10, 6, 11, 7, 12], list);
        var fresh = Enumerable.Repeat(GameState.HeadToHeadEmpty, 10).ToArray();
        TeamRecords.EnterHeadToHead(fresh, 0, nation: true, side: 1, lo: 3, hi: 5);
        Assert.Equal(0x53, fresh[0]);
    }

    /// <summary>
    /// Ein WM-Gruppenspiel Deutschlands trägt Kopf-an-Kopf (Gegnertore zuerst), Rekorde, Serien und Bilanz ein; ein
    /// Endrundenspiel nur Kopf-an-Kopf.
    /// </summary>
    [Fact]
    public void GermanyMatches_FillHeadToHead_GroupAlsoRecordsAndSeries()
    {
        var game = GameModeTests.Create(7, false, true, 2);
        const int other = NationalTeams.Germany + 1;
        game.EnterNationalResult(NationalTeams.Germany, other, 4, 2, NationalMatchKind.WorldCupGroup);
        game.EnterNationalResult(other, NationalTeams.Germany, 3, 3, NationalMatchKind.WorldCupKnockout);
        var list = game.HeadToHead(0, other);
        Assert.Equal(0x24, list[0]); // Heimspiel Deutschlands: lo = 4 (Deutschland), hi = 2 (Gegner) → „2:4“
        Assert.Equal(0x33, list[2]);
        Assert.Equal(0x0402, game.TeamRecord(NationalTeams.Germany, TeamRecordKind.HomeWin).Word);
        Assert.Equal(1, game.Clubs[other].SeriesCurrent[Series.Index(SeriesKind.Lost, SeriesColumn.Total)]);
        Assert.Equal([0, 0, 0, 0, 0, 0, 0, 1], game.NationalForm(other));
        Assert.Equal([0, 0, 0, 0, 0, 0, 0, 3], game.NationalForm(NationalTeams.Germany));
        Assert.Equal(0, game.TeamRecord(other, TeamRecordKind.HomeGoalsFor).Word); // Endrunde: keine Rekorde
    }

    /// <summary>
    /// Mehrsaisonprobe Modus 1, 2 und 4 (passiv und aktiv), vier Saisonwechsel: Jede gespielte WM trägt ihre Gruppenspiele
    /// als Bilanz der Länder ein (je fünf, höchstens acht Zeichen), die Kopf-an-Kopf-Liste bleibt bei zehn Einträgen, und
    /// Sichern/Laden behält alles. Modus 2 hat keine WM.
    /// </summary>
    [Theory]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    public void MultiSeason_NationalData_GrowsPerWorldCup(bool flag, bool globe, bool active)
    {
        var game = flag ? GameModeTests.Create(3, flag, globe, 2, 30) : GameModeTests.Create(7, flag, globe, 2);
        for (var change = 0; change <= 4; change++)
        {
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 3000, "Saisonende nicht erreicht");
                foreach (var m in game.Managers)
                    m.Money = Math.Max(m.Money, 10_000_000);
                if (active && game.NationalCoach is { Squad.Count: 0 })
                    game.AutoNominate();
                if (active && flag)
                    GameModeTests.ActiveSecondManager(game);
                game.Continue();
            }
            if (game.Phase == SeasonPhase.GameOver)
                break;
            var worldCups = globe ? change + 1 : 0;
            for (var n = NationalTeams.First; n <= NationalTeams.Last; n++)
            {
                Assert.Equal(Math.Min(GameState.NationalFormLength, 5 * worldCups), game.NationalForm(n).Count(f => f != 0));
                Assert.Equal(globe ? 5 : 0, game.NationalGroupRecord(n).Games);
                Assert.True(game.HeadToHead(0, n).Count(v => v != GameState.HeadToHeadEmpty) <= GameState.HeadToHeadLength);
            }
            if (globe)
                Assert.Contains(Enumerable.Range(NationalTeams.First, 12), n => game.HeadToHead(0, n)[0] != GameState.HeadToHeadEmpty);
            var loaded = SaveGame.Load(SaveGame.Save(game));
            Assert.True(loaded.Success, loaded.Error);
            Assert.Equal(SaveGame.Save(game), SaveGame.Save(loaded.Game!));
            if (change == 4)
                break;
            game.StartNextSeason();
        }
    }

    /// <summary>Schema 17 (vor dieser Spur): Die Felder werden leer angelegt (Kopf-an-Kopf 255).</summary>
    [Fact]
    public void Schema17_IsMigrated_WithEmptyTeamData()
    {
        var game = GameModeTests.Create(7, false, true, 2);
        var root = JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        root["schema"] = 17;
        var spiel = root["spiel"]!.AsObject();
        foreach (var f in new[] { "_teamRecords", "_teamRecordOpponents", "_headToHead", "_nationalForm" })
            Assert.True(spiel.Remove(f), f);
        var result = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.True(result.Success, result.Error);
        Assert.All(Enumerable.Range(0, TeamRecords.Teams), c => Assert.All(result.Game!.HeadToHead(5, c), v => Assert.Equal(GameState.HeadToHeadEmpty, v)));
        Assert.Equal(SaveGame.Save(game), SaveGame.Save(result.Game!));
    }
}
