using System.Text;
using System.Text.Json.Nodes;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur hok-play-off-marken (Runde 22, A612, #896): Marken (+30) der Play-off-Spiele in DGROUP 0xCCB2 + 14 · Spiel + Platz.
/// Vorher zog der Import sie ab (<c>OriginalImport.Playoffs.cs</c> [APPROX]) und der Export schrieb sie nicht. Bedeutung
/// (em.exe 0x32AA3, 0x376F0–0x377A3, 0x315E9): <see cref="PlayoffSeries.Marks"/>.
/// </summary>
public sealed class HokPlayOffMarkenTests
{
    private const int Seed = 4711;

    /// <summary>
    /// Import FILE22/FILE44: Marken je Spiel = Torbyte div 30 beider Vereine. Fundstellen: an jedem Spiel der ersten offenen Paarung
    /// einer Runde eine Marke (Zusatzwurf des schnellen Wegs, 0x376E1 → 0x32AA3 sieht den Gleichstand 0:0 der noch nicht gerechneten
    /// Paarungen, 0x317B2 leert sie nach jedem Spiel), zwei bei Gleichstand nach 60 Minuten (0x32AA3 + 0x37761; FILE44 Viertelfinale
    /// 2–7 Spiel 3 „2:4“ = 0x40, Halbfinale 2 Spiel 3 „5:3“ = 0x3F). Das Endspiel (eine Paarung) hat keine. Vorher: alle 0 → rot.
    /// </summary>
    [GameDataTheory]
    [InlineData("FILE22.HOK")]
    [InlineData("FILE44.HOK")]
    public void Import_PlayoffMarks_FromHok(string file)
    {
        var (save, game) = Import(file);
        var playoffs = game.Playoffs!;
        var results = save.Block("CCB2");
        int[][] pairSlots = [[0, 2, 4, 6], [8, 10], [12]];
        for (var r = 0; r < 3; r++)
            for (var k = 0; k < playoffs.Rounds[r].Count; k++)
            {
                var s = playoffs.Rounds[r][k];
                var p = pairSlots[r][k];
                Assert.Equal(s.Games.Count, s.Marks.Count);
                for (var g = 0; g < s.Games.Count; g++)
                    Assert.Equal(results[14 * g + p] / 30 + results[14 * g + p + 1] / 30, s.MarksOf(g));
            }

        if (file == "FILE22.HOK")
        {
            Assert.Equal([1, 1, 1], playoffs.Rounds[0][0].Marks);
            Assert.Equal([1, 1, 1, 0], playoffs.Rounds[1][0].Marks);
            Assert.All(playoffs.Rounds[2][0].Marks, m => Assert.Equal(0, m));
        }
        else
        {
            Assert.Equal([1, 1, 1, 1, 1], playoffs.Rounds[0][0].Marks);
            Assert.Equal([0, 0, 2, 0, 0], playoffs.Rounds[0][1].Marks);
            Assert.Equal([1, 1, 1], playoffs.Rounds[1][0].Marks);
            Assert.Equal([0, 0, 2, 0], playoffs.Rounds[1][1].Marks);
            Assert.All(playoffs.Rounds[2][0].Marks, m => Assert.Equal(0, m));
        }
    }

    /// <summary>
    /// Lage der Marken am HOK: [ORIGINAL] 0x31701–0x31734 schiebt sie nach dem Tausch 0x31505 auf das Byte des Gastes; nur das
    /// fünfte Spiel ohne Seriensieg des Gastes tauscht nicht (0x3168B → 0x3173A), dann bleiben sie beim Heimverein. Grundlage der
    /// Exportregel in <c>OriginalExport.Cups.cs</c>.
    /// </summary>
    [GameDataTheory]
    [InlineData("FILE22.HOK")]
    [InlineData("FILE44.HOK")]
    public void Hok_PlayoffMarks_SitOnGuestByte(string file)
    {
        var (save, game) = Import(file);
        var results = save.Block("CCB2");
        var clubOf = OriginalImport.ClubIndices(save, ClubDatabase.Load(TestGameData.Load()));
        var slots = OriginalImport.PlayoffSlots(save).ToArray();
        int[][] pairSlots = [[0, 2, 4, 6], [8, 10], [12]];
        var checkedGames = 0;
        for (var r = 0; r < 3; r++)
            for (var k = 0; k < game.Playoffs!.Rounds[r].Count; k++)
            {
                var s = game.Playoffs.Rounds[r][k];
                var p = pairSlots[r][k];
                for (var g = 0; g < s.Games.Count; g++)
                {
                    if (s.MarksOf(g) == 0)
                        continue;
                    var m = s.Games[g];
                    var carrier = results[14 * g + p] >= 30 ? clubOf[slots[p]] : clubOf[slots[p + 1]];
                    var expected = g < Playoffs.DatesPerRound - 1 || s.Wins(m.Away) >= PlayoffSeries.WinsNeeded ? m.Away : m.Home;
                    Assert.Equal(expected, carrier);
                    checkedGames++;
                }
            }
        Assert.True(checkedGames >= 6, $"{file}: nur {checkedGames} Spiele mit Marken.");
    }

    /// <summary>
    /// Export → Import (eigener Spielstand in der laufenden Viertelfinalrunde): die Marken jedes Spiels kommen zurück, im Torbyte des
    /// Gastes. Vorher schrieb der Export keine (Import 0 → rot).
    /// </summary>
    [GameDataFact]
    public void Export_PlayoffMarks_RoundTrip()
    {
        var raw = OriginalExportTests.RawDatabase();
        var database = ClubDatabase.Parse(raw);
        var game = OpenQuarterFinal(database);
        var round = game.Playoffs!.Rounds[0];
        var n = 0;
        foreach (var s in round)
        {
            s.Marks.Clear();
            for (var g = 0; g < s.Games.Count; g++)
                s.Marks.Add(n++ % 3);
        }

        var save = OriginalSaveGame.Read(OriginalExport.Export(game, raw));
        var results = save.Block("CCB2");
        var again = GameState.ImportOriginal(save, database, Seed);
        for (var k = 0; k < round.Count; k++)
        {
            var s = round[k];
            var t = again.Playoffs!.Rounds[0].Single(x => x.HomeClub == s.HomeClub);
            Assert.Equal(s.Marks, t.Marks);
            for (var g = 0; g < s.Games.Count; g++)
            {
                // Laufende Runde nach ungerader Spielzahl getauscht: Gast des Spiels g = Verein auf dem markierten Platz.
                var p = 2 * k;
                var swapped = OriginalImport.PlayoffGame(save) % 2 == 1;
                var guestOnFirst = (s.Games[g].Away == s.HomeClub) != swapped;
                Assert.Equal(30 * s.Marks[g], (results[14 * g + (guestOnFirst ? p : p + 1)] / 30) * 30);
                Assert.Equal(0, results[14 * g + (guestOnFirst ? p + 1 : p)] / 30);
            }
        }
    }

    /// <summary>
    /// R924-02 (#928): <c>PlayoffSeries.MarksFromReport</c> – zwei Marken bei Verlängerung, Penaltyschießen (Sieger bzw. Zusatztore),
    /// sonst 0, auch bei Spielwertung. Mutation <c>false &amp;&amp; (…)</c> in <c>Playoffs.cs</c> muss rot werden.
    /// </summary>
    [Fact]
    public void MarksFromReport_TieAfterSixtyMinutes_TwoMarks()
    {
        Assert.Equal(0, PlayoffSeries.MarksFromReport(new MatchReport { HomeGoals = 3, AwayGoals = 1 }));
        Assert.Equal(PlayoffSeries.TieMarks, PlayoffSeries.MarksFromReport(new MatchReport { HomeGoals = 2, AwayGoals = 2, Overtime = true }));
        Assert.Equal(PlayoffSeries.TieMarks, PlayoffSeries.MarksFromReport(new MatchReport { HomeGoals = 2, AwayGoals = 2, ShootoutHomeWins = true }));
        Assert.Equal(PlayoffSeries.TieMarks, PlayoffSeries.MarksFromReport(new MatchReport { HomeGoals = 2, AwayGoals = 2, ShootoutGoals = (1, 0) }));
        Assert.Equal(0, PlayoffSeries.MarksFromReport(new MatchReport { HomeGoals = 2, AwayGoals = 0, Forfeit = true, Overtime = true }));
    }

    /// <summary>
    /// R924-01 (#928): Fünftes Spiel mit 2:2 vor dem Spiel und Heimsieg: Der Tausch 0x316C9 entfällt (Siegbyte 0x14 nur bei Seriensieg
    /// des Gastes, Datei 0x3168B → 0x3173A), die Marken bleiben beim Heimverein (0x31701–0x31734). Mutation
    /// <c>var onAway = true;</c> in <c>OriginalExport.Cups.cs</c> muss rot werden.
    /// </summary>
    [GameDataFact]
    public void Export_PlayoffMarks_FifthGameHomeWin_StayOnHomeByte()
    {
        var raw = OriginalExportTests.RawDatabase();
        var database = ClubDatabase.Parse(raw);
        var game = OpenQuarterFinal(database);
        var series = game.Playoffs!.Rounds[0][0];
        var (h, a) = (series.HomeClub, series.AwayClub);
        series.Games.Clear();
        series.Marks.Clear();
        // Spiel 1 H, 2 A, 3 A, 4 H gewonnen (2:2), Spiel 5 (Heimrecht H) gewinnt H.
        int[] winners = [h, a, a, h, h];
        for (var g = 0; g < winners.Length; g++)
        {
            var (home, away) = g % 2 == 0 ? (h, a) : (a, h);
            var homeWins = winners[g] == home;
            series.Games.Add(new KnockoutMatch(g, home, away, homeWins ? 2 : 1, homeWins ? 1 : 2));
            series.Marks.Add(g == 4 ? PlayoffSeries.TieMarks : 0);
        }
        Assert.Equal(3, series.Wins(h));
        Assert.Equal(2, series.Wins(a));

        var save = OriginalSaveGame.Read(OriginalExport.Export(game, raw));
        var results = save.Block("CCB2");

        // Länge der laufenden Runde = 5 Spiele (ungerade): Tausch, der Heimverein der Paarung steht auf Platz p + 1.
        Assert.Equal(PlayoffSeries.TieMarks, results[14 * 4 + 1] / 30);
        Assert.Equal(0, results[14 * 4 + 0] / 30);
    }

    /// <summary>Spielstand mit mindestens einer offenen Viertelfinalrunde (Termine 1–2 gespielt).</summary>
    private static GameState OpenQuarterFinal(ClubDatabase database)
    {
        // Continue hält nach jedem Spiel des Managervereins: ein Bundesligist, der die Play-offs erreicht, liefert eine offene Runde.
        for (var club = 0; club < 12; club++)
        {
            var setup = new GameSetup();
            setup.AddManager(new ManagerSetup { Name = "Joerg", Head = 6, ClubIndex = club });
            var candidate = NewGame.Create(database, setup, Seed);
            TestSeason.PlayRegularSeason(candidate);
            PlayPlayoffGames(candidate, 2 * 4);
            if (candidate.Playoffs!.Rounds.Count == 1 && candidate.Playoffs.Rounds[0].Any(x => !x.IsDecided))
                return candidate;
        }
        throw new InvalidOperationException("Keine offene Viertelfinalrunde gefunden.");
    }

    /// <summary>
    /// Migration 55 → 56 (<c>SaveGame.Migration.HokPlayOffMarken.cs</c>): ein Stand ohne <c>Marks</c> bekommt je Spiel zwei Marken bei
    /// Penaltysieger, sonst 0, und lädt. Synthetisch (ohne Originaldaten): Hauptrunde und drei Play-off-Termine.
    /// </summary>
    [Fact]
    public void SaveGame_Schema55_GetsPlayoffMarks()
    {
        var game = NewGame.Create(TestGames.CreateDatabase(), Setup(), 7);
        TestSeason.PlayRegularSeason(game);
        PlayPlayoffGames(game, 2 * 4);
        Assert.NotEmpty(game.PlayoffResults);
        // R924-03 (#928): at least one game with a penalty winner, so the branch "penalty winner → 2 marks" runs.
        var first = game.Playoffs!.Rounds[0][0];
        first.Games[0] = first.Games[0] with { HomeGoals = 2, AwayGoals = 2, PenaltyWinner = first.Games[0].Home };
        var root = JsonNode.Parse(Encoding.UTF8.GetString(SaveGame.Save(game)))!.AsObject();
        root["schema"] = 55;
        var removed = 0;
        foreach (var series in Objects(root).Where(o => o.ContainsKey("HomeClub") && o.ContainsKey("Games")).ToList())
            removed += series.Remove("Marks") ? 1 : 0;
        Assert.True(removed >= 4);

        var loaded = SaveGame.Load(Encoding.UTF8.GetBytes(root.ToJsonString()));

        Assert.True(loaded.Success, loaded.Error);
        foreach (var s in loaded.Game!.Playoffs!.Rounds[0])
        {
            Assert.Equal(s.Games.Count, s.Marks.Count);
            for (var g = 0; g < s.Games.Count; g++)
                Assert.Equal(s.Games[g].PenaltyWinner is null ? 0 : PlayoffSeries.TieMarks, s.Marks[g]);
        }
        Assert.Equal(PlayoffSeries.TieMarks, loaded.Game.Playoffs.Rounds[0][0].Marks[0]);
    }

    /// <summary>
    /// Eigene Spiele: Marken aus dem Spielbericht – zwei, wenn das Spiel nach 60 Minuten unentschieden stand (Penaltysieger bzw.
    /// Verlängerung), sonst 0; je Spiel genau ein Eintrag.
    /// </summary>
    [Fact]
    public void PlayDate_RecordsMarksPerGame()
    {
        // Spur live-verschraenkt Teil 2 (Runde 26, S01, R21-D2 = A, R15-E2 = C): Saat 8 statt 7 – der Ligaspieltag rechnet je
        // Minute alle Paarungen aus dem gemeinsamen Zufall (0x37A62, docs/analyse/live-spieltag.md), anderer Zufallsverlauf ab dem
        // ersten Ligaspieltag; mit 7 ist die Vorbedingung verfehlt. Rückschaltprobe siehe PR #1032. Die Probe hängt an der Saat.
        var game = NewGame.Create(TestGames.CreateDatabase(), Setup(), 8);
        TestSeason.PlayRegularSeason(game);
        for (var guard = 0; game.Playoffs is null || game.Playoffs.Rounds.Count < 2; guard++)
        {
            Assert.True(guard < 400, "Play-offs erreichen das Halbfinale nicht.");
            game.Continue();
        }
        foreach (var s in game.Playoffs.Rounds[0])
        {
            Assert.Equal(s.Games.Count, s.Marks.Count);
            for (var g = 0; g < s.Games.Count; g++)
            {
                Assert.Contains(s.Marks[g], new[] { 0, PlayoffSeries.TieMarks });
                if (s.Games[g].HomeGoals == s.Games[g].AwayGoals)
                    Assert.Equal(PlayoffSeries.TieMarks, s.Marks[g]);
            }
        }
    }

    /// <summary>Spielt, bis mindestens <paramref name="games"/> Play-off-Spiele gespielt sind (zwei Termine des Viertelfinales).</summary>
    private static void PlayPlayoffGames(GameState game, int games)
    {
        for (var guard = 0; game.PlayoffResults.Count < games; guard++)
        {
            Assert.True(guard < 400, "Play-offs erreichen den Termin nicht.");
            game.Continue();
        }
    }

    private static GameSetup Setup()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "PASSIV", Head = 0, ClubIndex = 30 });
        return setup;
    }

    private static IEnumerable<JsonObject> Objects(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            yield return obj;
            foreach (var (_, child) in obj)
                foreach (var o in Objects(child))
                    yield return o;
        }
        else if (node is JsonArray array)
            foreach (var child in array)
                foreach (var o in Objects(child))
                    yield return o;
    }

    private static (OriginalSaveGame Save, GameState Game) Import(string file)
    {
        var save = OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave(file)));
        return (save, GameState.ImportOriginal(save, ClubDatabase.Load(TestGameData.Load()), Seed));
    }
}
