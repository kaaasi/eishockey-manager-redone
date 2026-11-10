using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using EishockeyManager.Tools;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur abgleich-original, Teil 1: Werkzeug <c>emtool abgleich</c> (<see cref="HokCompare"/>), Abdruck und Feldklassen
/// (<see cref="HokCompareClasses"/>, R15-D2 = A).
/// </summary>
public class AbgleichOriginalWerkzeugTests
{
    /// <summary>Jedes Schemafeld des Spielstands ist genau einer Klasse zugeordnet, und die Tabelle nennt kein Feld, das es nicht gibt.</summary>
    [Fact]
    public void Classes_CoverEverySchemaField()
    {
        var schema = SaveGame.SchemaDescription()
            .Where(l => !l.Contains(" = ", StringComparison.Ordinal) && !l.Contains(" : ", StringComparison.Ordinal))
            .Select(l => l[..l.IndexOf(':', StringComparison.Ordinal)])
            .Where(f => !f.StartsWith("ValueTuple<", StringComparison.Ordinal))
            .ToHashSet(StringComparer.Ordinal);
        var classified = HokCompareClasses.All.Keys.ToHashSet(StringComparer.Ordinal);
        Assert.Empty(schema.Except(classified).Order());
        Assert.Empty(classified.Except(schema).Order());
        Assert.DoesNotContain(HokCompareClasses.All.Values, r => r.Class == HokCompareClass.Unclassified);
    }

    [Fact]
    public void Rows_SameGame_NoDeviation()
    {
        var game = TestGames.Create();
        var rows = HokCompare.Rows(game, game, crossesSeason: false, []);
        Assert.True(rows.Count > 1000, $"nur {rows.Count} Zeilen");
        Assert.DoesNotContain(rows, r => r.Deviation);
        Assert.DoesNotContain(rows, r => r.Class == HokCompareClass.Unclassified);
        // Kader der Computervereine erzeugt der Import (Feldkarte Regel 3): nicht im Abdruck.
        var computer = game.Clubs.First(c => game.Managers.All(m => m.ClubIndex != c.Index));
        Assert.DoesNotContain(rows, r => r.Path.StartsWith($"Clubs[{computer.Index} {computer.Name}].Players", StringComparison.Ordinal));
        var mine = game.Clubs[game.ActiveManager.ClubIndex];
        Assert.Contains(rows, r => r.Path == $"Clubs[{mine.Index} {mine.Name}].Players[{mine.Players[0].Name}].Age");
    }

    /// <summary>Feste Felder exakt, Geld im Bereich (ohne Stand a: ±50.000 DM), Eingriffe ausgenommen.</summary>
    [Fact]
    public void Rows_JudgeByClass()
    {
        var original = TestGames.Create(seed: 7);
        var remake = TestGames.Create(seed: 7);
        var me = remake.ActiveManager;
        var stadium = remake.Finance(me).Stadium;
        stadium.TicketPrice += 1;
        me.Money = original.ActiveManager.Money * 11 / 10;

        var rows = HokCompare.Rows(original, remake, crossesSeason: false, []);
        var deviation = Assert.Single(rows, r => r.Deviation);
        Assert.Equal("Managers[0].FinanceData.Stadium.TicketPrice", deviation.Path);
        Assert.Equal(HokCompareClass.Exact, deviation.Class);

        me.Money = original.ActiveManager.Money * 2;
        rows = HokCompare.Rows(original, remake, crossesSeason: false, ["Managers[0].FinanceData.Stadium.*"]);
        deviation = Assert.Single(rows, r => r.Deviation);
        Assert.Equal("Managers[0].Money", deviation.Path);
        Assert.Equal(HokCompareClass.Random, deviation.Class);
        Assert.Equal(HokCompareClass.Intervention, rows.Single(r => r.Path == "Managers[0].FinanceData.Stadium.TicketPrice").Class);
    }

    /// <summary>
    /// Geld wird an der Veränderung seit Stand a gemessen (Review P2-1): ±10 % der Veränderung des Originals, mindestens 50.000 DM –
    /// nicht ±25 % des Kontostands.
    /// </summary>
    [Fact]
    public void Rows_MoneyJudgedByChangeSinceStart()
    {
        var original = TestGames.Create(seed: 13);
        var remake = TestGames.Create(seed: 13);
        original.ActiveManager.Money = 10_000_000;
        var start = new Dictionary<string, string?> { ["Managers[0].Money"] = "12000000" };

        HokCompareRow Money(long remakeMoney)
        {
            remake.ActiveManager.Money = remakeMoney;
            return HokCompare.Rows(original, remake, crossesSeason: false, [], start).Single(r => r.Path == "Managers[0].Money");
        }

        Assert.False(Money(10_200_000).Deviation); // Original −2 Mio., Remake −1,8 Mio.: 10 % der Veränderung
        var row = Money(10_300_000);              // ±25 % des Kontostands hätten 2,5 Mio. erlaubt
        Assert.True(row.Deviation);
        Assert.Contains("seit a: Original −2000000, Remake −1700000", row.Note);
        start["Managers[0].Money"] = "10000000";  // keine Veränderung: fester Deckel 50.000 DM
        Assert.False(Money(10_050_000).Deviation);
        Assert.True(Money(10_050_001).Deviation);
    }

    /// <summary>Ein Spieler, den es nur auf einer Seite gibt: im Managerkader eine Abweichung, auf dem Markt (zufallsabhängig) nicht.</summary>
    [Fact]
    public void Rows_MissingPlayer_DeviationOnlyInManagerSquad()
    {
        var original = TestGames.Create(seed: 9);
        var remake = TestGames.Create(seed: 9);
        var squad = remake.Clubs[remake.ActiveManager.ClubIndex].Players;
        var gone = squad[^1];
        squad.RemoveAt(squad.Count - 1);

        var rows = HokCompare.Rows(original, remake, crossesSeason: false, []);
        Assert.Contains(rows, r => r.Deviation && r.Path.EndsWith($".Players[{gone.Name}].Age", StringComparison.Ordinal) && r.Remake is null);
        Assert.Contains(rows, r => r.Deviation && r.Path.EndsWith(".Players.Count", StringComparison.Ordinal));
        Assert.DoesNotContain(rows, r => r.Deviation && r.Path.StartsWith("TransferMarket", StringComparison.Ordinal));
    }

    /// <summary>Saisongebundene Felder (Liga) sind nach einem Saisonwechsel zufallsabhängig.</summary>
    [Fact]
    public void Rows_SeasonBoundFieldsRandomAcrossSeasons()
    {
        var original = TestGames.Create(seed: 11);
        var remake = TestGames.Create(seed: 11);
        var club = remake.Clubs.First(c => c.League == League.Bundesliga);
        club.League = League.ZweiteLiga;
        var path = $"Clubs[{club.Index} {club.Name}].League";

        Assert.Contains(HokCompare.Rows(original, remake, crossesSeason: false, []), r => r.Deviation && r.Path == path);
        var across = HokCompare.Rows(original, remake, crossesSeason: true, []).Single(r => r.Path == path);
        Assert.False(across.Deviation);
        Assert.Equal(HokCompareClass.Random, across.Class);
    }

    [Fact]
    public void PlayTo_ReachesLaterHalt_AndReportsSkippedHalt()
    {
        var target = TestGames.Create(seed: 5);
        TestSeason.PlayMatchDay(target);
        TestSeason.PlayMatchDay(target);
        target.OnMainMenu(target.ActiveManager, isReturn: false);

        var game = TestGames.Create(seed: 5);
        Assert.Null(HokCompare.PlayTo(game, target));
        Assert.Equal(target.CurrentDay, game.CurrentDay);
        Assert.Equal(target.PlayedMatchDays, game.PlayedMatchDays);

        var early = TestGames.Create(seed: 5);
        Assert.Contains("übersprungen", HokCompare.PlayTo(target, early));
    }

    [Theory]
    [InlineData("FILE02.HOK", "FILE03.HOK", "FILE02→FILE03")]
    [InlineData("a/file44.hok", "b/FILE46.HOK", "FILE44→FILE46")]
    public void PairName_FromFileNames(string a, string b, string expected) => Assert.Equal(expected, HokCompare.PairName(a, b));

    /// <summary>Mit Originaldaten: FILE02 → FILE03 (drei Spieltage ohne Eingriff) erreicht den Halt, alle Felder eingeordnet.</summary>
    [GameDataFact]
    public void Compare_File02To03_ReachesHalt()
    {
        var report = HokCompare.Compare(OriginalImportTests.OriginalSave("FILE02.HOK"), OriginalImportTests.OriginalSave("FILE03.HOK"),
            ClubDatabase.Load(TestGameData.Load()));
        Assert.Null(report.PlayError);
        Assert.Equal("FILE02→FILE03", report.Pair);
        Assert.Equal(0, report.Count(HokCompareClass.Unclassified));
        foreach (var path in new[] { "CurrentDay", "PlayedMatchDays", "Season", "Phase", "Invariante.SpieleJeVerein[Bundesliga]" })
        {
            var row = report.Rows.Single(r => r.Path == path);
            Assert.Equal(HokCompareClass.Exact, row.Class);
            Assert.False(row.Deviation, row.ToString());
        }
        var output = new StringWriter();
        HokCompare.Print(report, all: false, output);
        Assert.Contains("Abgleich FILE02→FILE03", output.ToString());
    }
}
