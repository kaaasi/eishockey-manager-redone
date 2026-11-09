using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using Xunit.Abstractions;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur hok-import, Review R14HI-01 (PR #670, <c>docs/reviews/2026-11-09-integration-hok-import-r14.md</c>): Play-offs und
/// Aufstiegsrunden kommen aus dem HOK (DGROUP 0x5D3B, 0xCCA3, 0xCCA4, 0xCCB2 bzw. 0xCFF4, 0xCED8, 0xCDB8, 0xCD88, 0xCD58) statt aus
/// dem Nachspielen. Gegenbeispiele des Reviews: FILE44 (Platzliste 3 2 0 1), FILE44 → Saisonwechsel = Ligen wie FILE46,
/// FILE22 → Saisonwechsel = FILE23.
/// </summary>
public sealed class OriginalImportPostseasonTests(ITestOutputHelper output)
{
    private const int Seed = 4711;

    /// <summary>Aufstiegsrunden FILE44 und FILE22: Teilnehmer, Paarungen, Tore, Punkte und Platzliste wie im HOK.</summary>
    [GameDataTheory]
    [InlineData("FILE44.HOK")]
    [InlineData("FILE22.HOK")]
    public void Import_RelegationGroups_FromOriginal(string file)
    {
        var (save, game, clubOf) = Import(file);
        Assert.Equal(2, game.RelegationGroups.Count);
        var teams = OriginalImport.GroupTeams(save, 1).ToArray();
        var points = save.Block("CD58");
        var ranking = save.Block("CD88");
        for (var g = 0; g < 2; g++)
        {
            var group = game.RelegationGroups[g];
            Assert.Equal(g == 0 ? League.Bundesliga : League.ZweiteLiga, group.Upper);
            var expectedClubs = teams.Skip(4 * g).Take(4).Select(t => clubOf[t]).ToList();
            Assert.Equal(expectedClubs, group.Clubs);
            Assert.Equal(6, group.PlayedRounds);
            Assert.Equal(12, group.Results.Count);
            for (var d = 0; d < 6; d++)
            {
                var original = OriginalImport.GroupDate(save, 1, g, d, 2);
                var played = group.Results.Where(r => r.MatchDay == d + 1).ToList();
                Assert.Equal(original.Select(p => (clubOf[p.Home], clubOf[p.Away], p.HomeGoals, p.AwayGoals)),
                    played.Select(r => (r.Home, r.Away, r.HomeGoals, r.AwayGoals)));
            }
            var table = group.Table(game.Clubs);
            var expectedOrder = Enumerable.Range(0, 4).Select(i => expectedClubs[ranking[12 + 6 * g + i]]).ToList();
            Assert.Equal(expectedOrder, table.Select(r => r.Club.Index));
            for (var i = 0; i < 4; i++)
                Assert.Equal(points[12 + 6 * g + i], table.First(r => r.Club.Index == expectedClubs[i]).PointsPlus);
        }
        // Gegenbeispiel des Reviews: FILE44 Gruppe Bundesliga, Platzliste 3 2 0 1 → die beiden Zweitligisten steigen auf.
        if (file == "FILE44.HOK")
            Assert.Equal(new[] { clubOf[teams[3]], clubOf[teams[2]] }, game.RelegationGroups[0].Qualified(game.Clubs));
    }

    /// <summary>
    /// Play-offs FILE44 und FILE22 (Endspiel gespielt): drei Runden aus DGROUP 0x5D3B (Plätze 0–7, 8–11, 12–13), Spiele aus
    /// 0xCCB2 (Tore modulo 30), Siege wie 0xCCA4 (0x14 = drei Siege des Gastes im fünften Spiel), Meister = DGROUP 0xD0E6.
    /// </summary>
    [GameDataTheory]
    [InlineData("FILE44.HOK")]
    [InlineData("FILE22.HOK")]
    public void Import_Playoffs_FromOriginal(string file)
    {
        var (save, game, clubOf) = Import(file);
        var playoffs = game.Playoffs!;
        var slots = OriginalImport.PlayoffSlots(save).ToArray();
        var wins = save.Block("CCA4");
        var results = save.Block("CCB2");
        Assert.Equal(3, playoffs.Rounds.Count);
        Assert.Equal(clubOf[save.Block("D0E6")[0]], playoffs.Champion);
        // Plätze der Paarungen je Runde in Platzfolge (seit #676 wie Playoffs.Rounds; vorher [0, 4, 2, 6] für die alte Paarung).
        int[][] pairSlots = [[0, 2, 4, 6], [8, 10], [12]];
        var all = new List<KnockoutMatch>();
        for (var round = 0; round < 3; round++)
        {
            var series = playoffs.Rounds[round];
            Assert.Equal(4 >> round, series.Count);
            for (var k = 0; k < series.Count; k++)
            {
                var p = pairSlots[round][k];
                var s = series[k];
                // Nach dem Rundenwechsel stehen die Paare in Setzreihenfolge (0x31C3A); im fertigen Endspiel bleibt der Tausch des
                // fünften Spiels stehen, wenn der Gast die Serie gewann (Siegbyte 0x14 auf Platz 2k): dann ist Platz 2k der Gast von Spiel 1.
                var swapped = round == 2 && wins[p] == 0x14;
                var (slotFirst, slotSecond) = swapped ? (p + 1, p) : (p, p + 1);
                Assert.Equal((clubOf[slots[slotFirst]], clubOf[slots[slotSecond]]), (s.HigherSeed, s.LowerSeed));
                var expectedGames = Enumerable.Range(0, 5).Where(g => results[14 * g + p] != OriginalImport.CupNotPlayed).ToList();
                Assert.Equal(expectedGames.Count, s.Games.Count);
                for (var g = 0; g < expectedGames.Count; g++)
                {
                    var (a, b) = (results[14 * g + slotFirst] % 30, results[14 * g + slotSecond] % 30);
                    var (home, away) = g % 2 == 0 ? (s.HigherSeed, s.LowerSeed) : (s.LowerSeed, s.HigherSeed);
                    var (hg, ag) = g % 2 == 0 ? (a, b) : (b, a);
                    Assert.Equal((home, away, hg, ag), (s.Games[g].Home, s.Games[g].Away, s.Games[g].HomeGoals, s.Games[g].AwayGoals));
                }
                Assert.Equal(wins[slotFirst] == 0x14 ? 3 : wins[slotFirst], s.Wins(s.HigherSeed));
                Assert.Equal(wins[slotSecond] == 0x14 ? 3 : wins[slotSecond], s.Wins(s.LowerSeed));
                Assert.True(s.IsDecided);
                all.AddRange(s.Games);
            }
            // Sieger rücken wie in Tabelle 0x144 auf (Platz 0 → 8, 4 → 9, 2 → 10, 6 → 11; 8 → 12, 10 → 13): Serie k der nächsten Runde
            // = Sieger der Serie k gegen Sieger der Serie k + Serienzahl / 2.
            if (round > 0)
                for (var k = 0; k < series.Count; k++)
                    Assert.Equal((playoffs.Rounds[round - 1][k].Winner, playoffs.Rounds[round - 1][k + series.Count].Winner), (series[k].HigherSeed, series[k].LowerSeed));
        }
        Assert.Equal(all.OrderBy(g => g.Day).ThenBy(g => g.Home), game.PlayoffResults.OrderBy(g => g.Day).ThenBy(g => g.Home));
        Assert.Equal(playoffs.Champion, game.CurrentRecord?.Titles[Title.GermanChampion]);
    }

    /// <summary>
    /// FILE44 (14.04.1994, Aufstiegsrunden fertig) → Saisonwechsel: Bundesliga und 2. Liga wie FILE46 (EC Kassel und ES Weißwasser
    /// steigen auf, nicht Freiburg und Preussen). Oberliga bis auf die zwei Nachrücker aus der Regionalliga (Zufall) gleich.
    /// </summary>
    [GameDataTheory]
    [InlineData("FILE44.HOK", "FILE46.HOK")]
    [InlineData("FILE22.HOK", "FILE23.HOK")]
    public void Import_SeasonChange_LeaguesLikeNextOriginalStand(string file, string next)
    {
        var (save, game, _) = Import(file);
        var nextSave = OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave(next)));
        var database = ClubDatabase.Load(TestGameData.Load());
        var nextClubOf = OriginalImport.ClubIndices(nextSave, database);
        var before = Enum.GetValues<League>().ToDictionary(l => l, l => game.Clubs.Where(c => c.League == l).Select(c => c.Index).ToHashSet());

        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 100, "Saisonende nicht erreicht");
            game.Continue();
        }
        Assert.Equal(SeasonPhase.SeasonOver, game.Phase);
        game.StartNextSeason();

        foreach (var league in Enum.GetValues<League>())
        {
            var slots = Enumerable.Range(12 * (int)league, 12).Select(s => nextClubOf[s]).ToHashSet();
            var remake = game.Clubs.Where(c => c.League == league).Select(c => c.Index).ToHashSet();
            if (league == League.Oberliga)
            {
                // Nachrücker aus der Regionalliga zieht das Remake zufällig (SeasonFlow.Oberliga.cs); nur die Vereine vergleichen, die
                // vorher in einer Liga standen.
                var known = before.Values.SelectMany(s => s).ToHashSet();
                slots.IntersectWith(known);
                remake.IntersectWith(known);
            }
            output.WriteLine($"{league}: Original {Names(game, slots)}; Remake {Names(game, remake)}");
            Assert.Equal(slots.Order(), remake.Order());
        }
    }

    /// <summary>
    /// Alle Stände ab dem Ende der Hauptrunde: mit Vereinssaison (FILE22, FILE44) kommen Play-offs und beide Aufstiegsrunden aus dem
    /// HOK; „nur Weltkugel“ (FILE11, FILE18–21, FILE47–49A) hat keine (DGROUP 0xCFFC leer, 0xCCA3 = 0). Vor dem Ende der Hauptrunde
    /// gibt es nichts zu übernehmen.
    /// </summary>
    [GameDataFact]
    public void Import_AllStands_PostseasonMatchesOriginal()
    {
        var folder = Path.GetDirectoryName(OriginalImportTests.OriginalSave("FILE01.HOK"))!;
        var database = ClubDatabase.Load(TestGameData.Load());
        var checkedStands = 0;
        foreach (var path in Directory.GetFiles(folder, "FILE*.HOK").Order(StringComparer.Ordinal))
        {
            var save = OriginalSaveGame.Read(File.ReadAllBytes(path));
            var game = GameState.ImportOriginal(save, database, Seed);
            if (OriginalImport.Mode(save) == GameMode.WorldOnly)
            {
                Assert.Null(game.Playoffs);
                Assert.Empty(game.RelegationGroups);
                Assert.Equal(0, OriginalImport.PlayoffGame(save));
                continue;
            }
            if (!game.RegularSeasonOver)
            {
                Assert.Null(game.Playoffs);
                continue;
            }
            checkedStands++;
            Assert.Equal(OriginalImport.PlayoffRound(save) + 1, game.Playoffs!.Rounds.Count);
            Assert.Equal(2, game.RelegationGroups.Count);
            var points = save.Block("CD58");
            for (var g = 0; g < 2; g++)
                Assert.Equal(Enumerable.Range(0, 4).Sum(i => points[12 + 6 * g + i]) / 4, game.RelegationGroups[g].PlayedRounds);
            output.WriteLine($"{Path.GetFileName(path)}: Runde {OriginalImport.PlayoffRound(save)}, Spiel {OriginalImport.PlayoffGame(save)}, Termine {game.RelegationGroups[0].PlayedRounds}/{game.RelegationGroups[1].PlayedRounds}");
        }
        Assert.True(checkedStands >= 2, $"nur {checkedStands} Stände nach der Hauptrunde");
    }

    /// <summary>
    /// Mehrsaisonprobe nach dem Import aller Original-Stände (Auftrag Leitstelle Runde 14, Schritt 9): vier Saisonwechsel mit
    /// aktiven Managern (Konto mindestens 2 Mio. DM, Kader gehalten) ohne Ausnahme, Spielerkennungen gültig, Ligen zu je zwölf
    /// Vereinen. Stände mit Spieldauer „eine Saison“ enden vorher (GameOver).
    /// </summary>
    [GameDataFact]
    public void Import_AllStands_FourSeasonChanges()
    {
        var folder = Path.GetDirectoryName(OriginalImportTests.OriginalSave("FILE01.HOK"))!;
        var database = ClubDatabase.Load(TestGameData.Load());
        foreach (var path in Directory.GetFiles(folder, "FILE*.HOK").Order(StringComparer.Ordinal))
        {
            var save = OriginalSaveGame.Read(File.ReadAllBytes(path));
            var game = GameState.ImportOriginal(save, database, Seed);
            var start = game.Season;
            var changes = 0;
            for (; changes < 4 && game.Phase != SeasonPhase.GameOver; changes++)
            {
                for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
                {
                    Assert.True(guard < 3000, $"{Path.GetFileName(path)}: Saisonende nicht erreicht");
                    foreach (var m in game.Managers)
                    {
                        m.Money = Math.Max(m.Money, 2_000_000);
                        TestSeason.KeepSquad(game, m, extendExpired: true);
                    }
                    game.Continue();
                }
                if (game.Phase == SeasonPhase.GameOver)
                    break;
                game.StartNextSeason();
                Assert.Empty(game.ValidatePlayerIdentity());
                if (!game.WorldOnly)
                    foreach (var league in Enum.GetValues<League>())
                        Assert.Equal(GameState.ClubsPerLeague, game.Clubs.Count(c => c.League == league));
            }
            output.WriteLine($"{Path.GetFileName(path)}: {start} → {game.Season}, {changes} Wechsel, {game.Phase}");
            Assert.True(game.Season == start + 4 || game.Phase == SeasonPhase.GameOver, $"{Path.GetFileName(path)}: nur bis {game.Season}");
        }
    }

    private static string Names(GameState game, IEnumerable<int> clubs) => string.Join(", ", clubs.Order().Select(c => game.Clubs[c].Name));

    private static (OriginalSaveGame Save, GameState Game, int[] ClubOf) Import(string file)
    {
        var save = OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave(file)));
        var database = ClubDatabase.Load(TestGameData.Load());
        return (save, GameState.ImportOriginal(save, database, Seed), OriginalImport.ClubIndices(save, database));
    }
}
