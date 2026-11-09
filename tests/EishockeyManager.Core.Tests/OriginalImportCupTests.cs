using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur hok-import, Teil 4c (E14-4 = A): DEB-Pokal, Europapokal und WM kommen aus dem HOK statt aus dem Nachspielen. Zuordnung
/// aus em.exe (0x341AB, 0x3489D, 0x34B76, 0x30427): Feldkarte <c>docs/formate/spielstand-original.md</c>, „Pokale und WM (Teil 4c)“.
/// </summary>
public sealed class OriginalImportCupTests
{
    private const int Seed = 4711;

    /// <summary>
    /// FILE38: Viertelfinale (Runde 3) vor dem ersten Spiel – Paarungen aus 0x5CFB, Plätze 2k mit Heimrecht. FILE39: nach dem
    /// ersten Spiel – Plätze getauscht, Tore aus 0xD014, Siege wie 0xD075.
    /// </summary>
    [GameDataFact]
    public void Import_DebCupQuarterFinal()
    {
        var (save38, game38, clubOf) = Import("FILE38.HOK");
        var cup = game38.DebCup!;
        Assert.Equal(2, cup.CurrentRound);
        var slots = OriginalImport.CupSlots(save38, 0).ToArray();
        Assert.Equal(Enumerable.Range(0, 4).Select(k => (clubOf[slots[2 * k]], clubOf[slots[2 * k + 1]])),
            cup.Rounds[2].Select(t => (t.First, t.Second)));
        Assert.All(cup.Rounds[2], t => Assert.Empty(t.Games));

        var (save39, game39, _) = Import("FILE39.HOK");
        var ties = game39.DebCup!.Rounds[2];
        Assert.Equal(cup.Rounds[2].Select(t => (t.First, t.Second)), ties.Select(t => (t.First, t.Second)));
        var now = OriginalImport.CupSlots(save39, 0).ToArray();
        var goals = OriginalImport.DebCupGoals(save39, 0).ToArray();
        var wins = save39.Block("D075");
        for (var k = 0; k < 4; k++)
        {
            var game = Assert.Single(ties[k].Games);
            Assert.Equal(DebCup.DayOf(2, 0), game.Day);
            Assert.Equal(ties[k].First, game.Home);
            Assert.Equal((clubOf[now[2 * k + 1]], clubOf[now[2 * k]]), (game.Home, game.Away)); // nach dem Spiel getauscht
            Assert.Equal((goals[2 * k + 1], goals[2 * k]), ((byte)game.HomeGoals, (byte)game.AwayGoals));
            Assert.Equal((wins[2 * k], wins[2 * k + 1]), ((byte)ties[k].Wins(clubOf[now[2 * k]]), (byte)ties[k].Wins(clubOf[now[2 * k + 1]])));
        }
        Assert.Equal((1, 8), (ties[0].Games[0].AwayGoals, ties[0].Games[0].HomeGoals)); // 2 gegen 9: 1:8 aus Sicht von Platz 0
    }

    /// <summary>FILE44: Endspiel nach drei Spielen, Marken + 30 (64 = 4 Tore, 2 Marken), Sieger = DGROUP 0x387.</summary>
    [GameDataFact]
    public void Import_DebCupFinalWithMarks()
    {
        var (save, game, clubOf) = Import("FILE44.HOK");
        var cup = game.DebCup!;
        Assert.Equal(4, cup.CurrentRound);
        var tie = Assert.Single(cup.Rounds[4]);
        Assert.Equal(3, tie.Games.Count);
        Assert.Equal(clubOf[save.Block("0387")[0]], cup.Winner);
        Assert.Equal(cup.Winner, tie.Winner);
        Assert.Equal((4, 4, 2), (tie.Games[0].HomeGoals, tie.Games[0].AwayGoals, tie.MarksOf(0)));
        Assert.Equal(tie.First, tie.Games[0].Winner); // Gleichstand mit Marken: Heimverein gewinnt (0x34C76 „jnc“)
        Assert.Equal((3, 3, 2), (tie.Games[1].HomeGoals, tie.Games[1].AwayGoals, tie.MarksOf(1)));
        Assert.Equal((1, 4, 0), (tie.Games[2].HomeGoals, tie.Games[2].AwayGoals, tie.MarksOf(2)));
    }

    /// <summary>Alle Stände: laufende Runde = 0x5CF3 − 1, Teilnehmer = Plätze, Siege = 0xD075 (in der Runde), Sieger = 0x387 nach dem Endspiel.</summary>
    [GameDataFact]
    public void Import_DebCupAllStands()
    {
        var database = ClubDatabase.Load(TestGameData.Load());
        var folder = Path.GetDirectoryName(OriginalImportTests.OriginalSave("FILE01.HOK"))!;
        var checkedStands = 0;
        foreach (var path in Directory.GetFiles(folder, "FILE*.HOK").Order(StringComparer.Ordinal))
        {
            var save = OriginalSaveGame.Read(File.ReadAllBytes(path));
            var game = GameState.ImportOriginal(save, database, Seed);
            if (game.DebCup is not { } cup || save.Block("5CF3")[0] == 0)
                continue;
            var clubOf = OriginalImport.ClubIndices(save, database);
            int round = save.Block("5CF3")[0];
            var name = Path.GetFileName(path);
            Assert.True(round - 1 == cup.CurrentRound, $"{name}: Runde {cup.CurrentRound + 1}, HOK {round}");
            var slots = OriginalImport.CupSlots(save, 0).ToArray();
            var teams = DebCup.Teams >> (round - 1);
            Assert.Equal(slots[..teams].Select(s => clubOf[s]).Order(), cup.Rounds[^1].SelectMany(t => new[] { t.First, t.Second }).Order());
            var wins = save.Block("D075");
            if (wins.Any(w => w != 0))
                for (var i = 0; i < teams; i++)
                    Assert.Equal(wins[i], (byte)cup.Rounds[^1].Single(t => t.Involves(clubOf[slots[i]])).Wins(clubOf[slots[i]]));
            Assert.Equal(cup.Winner is null ? null : clubOf[save.Block("0387")[0]], cup.Winner);
            checkedStands++;
        }
        Assert.True(checkedStands >= 30, $"nur {checkedStands} Stände mit DEB-Pokal");
    }

    /// <summary>Nach dem Import geht der Pokal nach Remake-Regeln weiter: FILE39 bis nach dem dritten Termin, nächste Runde aus den Siegern.</summary>
    [GameDataFact]
    public void Import_DebCupContinues()
    {
        var (_, game, _) = Import("FILE39.HOK");
        var cup = game.DebCup!;
        var last = DebCup.DayOf(2, 2);
        while (game.CurrentDay <= last && game.Phase == SeasonPhase.Running)
            game.Continue();
        Assert.Equal(3, cup.CurrentRound);
        Assert.Equal(cup.Rounds[2].Select(t => t.Winner!.Value).Order(), cup.Rounds[3].SelectMany(t => new[] { t.First, t.Second }).Order());
        Assert.All(cup.Rounds[2], t => Assert.Equal(1, t.Games.Count(g => g.Day == DebCup.DayOf(2, 0))));
    }

    /// <summary>
    /// FILE36: Europapokal nach sieben Terminen – Gruppen 0xD004, Ergebnisse 0xCDB8 + 0x90, Endrunde 0xD00C mit dem ersten Termin.
    /// FILE38: nach zwölf Terminen; Sieger = Erster der Endrunde = DGROUP 0x388 des Folgestands FILE43.
    /// </summary>
    [GameDataFact]
    public void Import_EuropeanCupFromOriginal()
    {
        var (save, game, clubOf) = Import("FILE36.HOK");
        var cup = game.EuropeanCup!;
        var teams = OriginalImport.GroupTeams(save, 2).ToArray();
        Assert.Equal(teams.Select(t => clubOf[t]), cup.Draw);
        Assert.Equal(teams[..4].Select(t => clubOf[t]), cup.Groups[0].Teams);
        Assert.All(cup.Groups, g => Assert.Equal(12, g.Results.Count));
        var first = OriginalImport.GroupDate(save, 2, 0, 0, 2)[0];
        Assert.Equal((clubOf[first.Home], clubOf[first.Away], first.HomeGoals, first.AwayGoals),
            (cup.Groups[0].Results[0].Home, cup.Groups[0].Results[0].Away, cup.Groups[0].Results[0].HomeGoals, cup.Groups[0].Results[0].AwayGoals));
        var final = cup.FinalRound!;
        Assert.Equal(OriginalImport.GroupTeams(save, 3)[..4].ToArray().Select(t => clubOf[t]), final.Teams);
        Assert.Equal(2, final.Results.Count);
        Assert.Equal((6, 7), (final.Results[0].HomeGoals, final.Results[0].AwayGoals));
        Assert.Null(cup.Winner);
        Assert.Contains(clubOf[save.Block("A776")[0]], cup.GermanEntrants);

        var (_, game38, clubOf38) = Import("FILE38.HOK");
        var (save43, _, _) = Import("FILE43.HOK");
        Assert.Equal(12, game38.EuropeanCup!.FinalRound!.Results.Count);
        Assert.Equal(clubOf38[save43.Block("0388")[0]], game38.EuropeanCup.Winner);
    }

    /// <summary>
    /// WM im Modus „nur Weltkugel“: FILE48 nach dem ersten Termin (Gruppen 52–57/58–63, Ergebnisse 0xCDB8), FILE20 nach dem
    /// Halbfinale (K.-o.-Plätze 0x5D5B wie im Remake).
    /// </summary>
    [GameDataFact]
    public void Import_WorldCupFromOriginal()
    {
        var (save48, game48, clubOf) = Import("FILE48.HOK");
        var wm = game48.WorldCup!;
        Assert.Equal(Enumerable.Range(52, 12).Select(s => clubOf[s]), wm.Order);
        Assert.All(wm.Groups, g => Assert.Equal(3, g.Results.Count));
        var r = OriginalImport.GroupDate(save48, 0, 0, 0, 3)[1];
        Assert.Equal((r.HomeGoals, r.AwayGoals), (wm.Groups[0].Results[1].HomeGoals, wm.Groups[0].Results[1].AwayGoals));

        var (save20, game20, clubOf20) = Import("FILE20.HOK");
        var wm20 = game20.WorldCup!;
        Assert.All(wm20.Groups, g => Assert.Equal(15, g.Results.Count));
        Assert.Equal(2, wm20.Knockout.Count);
        var saved = OriginalImport.CupSlots(save20, 3).ToArray();
        for (var stage = 0; stage < 2; stage++)
            for (var k = 0; k < 4; k++)
                Assert.Equal(clubOf20[saved[8 * stage + 2 * k]], wm20.Knockout[stage][k].Winner);
        Assert.Equal(Enumerable.Range(0, 4).Select(k => (clubOf20[saved[16 + 2 * k]], clubOf20[saved[17 + 2 * k]])), wm20.KnockoutPairings(2));
    }

    private static (OriginalSaveGame Save, GameState Game, int[] ClubOf) Import(string file)
    {
        var save = OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave(file)));
        var database = ClubDatabase.Load(TestGameData.Load());
        return (save, GameState.ImportOriginal(save, database, Seed), OriginalImport.ClubIndices(save, database));
    }
}
