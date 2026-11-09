using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using Xunit.Abstractions;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur spielberechnung-komplett, Teil 1 (#204): Wanderung der Teamwerte em.exe 0x9D1B. Im Original bleiben die Teamwerte
/// der Computervereine nicht fest: Spielstart und Saisonwechsel ziehen sie mit großem Schritt in die Grenzen ihrer Liga
/// (DGROUP 0x125: 69 … 94, 39 … 73, 15 … 43), der Tageslauf bewegt sie zweimal je Woche mit kleinem Schritt (1 … 99).
/// Belegt an den Videos der Reihe `folgen` (Stärken und Paarungen über 13 Saisons, docs/spielberechnung.md §15).
/// </summary>
public sealed class TeamValueDriftTests(ITestOutputHelper output)
{
    private static GameState Game(int seed = 3)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Test", Head = 0, ClubIndex = 2 });
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    private static (int Min, int Max) Bounds(League? league) => MatchEngine.DriftBounds(league);

    private static IEnumerable<int> ConditionAndTechnique(Club club)
    {
        var v = club.TeamValues!.Value;
        return [club.Strength + v.DefenseCondition, club.Strength + v.AttackCondition,
            club.Strength + v.DefenseTechnique, club.Strength + v.AttackTechnique];
    }

    /// <summary>
    /// Regressionstest #204 (vor der Spur rot: die Teamwerte blieben nach dem Lader fest, Datenbank ± 10 %).
    /// [ORIGINAL] 0x43A73 → 0x9D1B(10): nach dem Spielstart liegen Kondition und Technik jedes Vereins in den Grenzen seiner
    /// Liga, die Form in 45 … 55.
    /// </summary>
    [Fact]
    public void GameStart_DriftsIntoLeagueBounds()
    {
        var game = Game();
        // #688 (R688-04): Nach dem ersten Spieltag hat der Tageswechsel des Halttags schon die kleine Wanderung gerechnet (vor
        // dem Menü wie im Original); geprüft wird deshalb direkt die Wanderung des Spielstarts.
        MatchEngine.EnsureTeamValues(game);
        var outside = 0;
        foreach (var club in game.Clubs.Where(c => c.Index < NationalTeams.First && c.Players.Count > 0))
        {
            var (min, max) = Bounds(club.League);
            outside += ConditionAndTechnique(club).Count(x => x < min || x > max);
            Assert.InRange(club.TeamValues!.Value.DefenseForm, 45, 55);
            Assert.InRange(club.TeamValues!.Value.AttackForm, 45, 55);
        }
        // Direkt nach der Wanderung des Spielstarts alles in den Liga-Grenzen.
        Assert.Equal(0, outside);
    }

    /// <summary>[ORIGINAL] 0x56448 → 0x9D1B(10) nach Auf- und Abstieg: wieder in den Grenzen der (neuen) Liga.</summary>
    [Fact]
    public void SeasonChange_DriftsIntoBoundsOfNewLeague()
    {
        var game = Game();
        TestSeason.PlayMatchDay(game);
        var before = game.Clubs.ToDictionary(c => c.Index, c => c.TeamValues!.Value);
        RngTwinProbeTests.Run(game, active: false, changes: 1);
        foreach (var club in game.Clubs.Where(c => c.Index < NationalTeams.First && c.Players.Count > 0))
        {
            var (min, max) = Bounds(club.League);
            Assert.All(ConditionAndTechnique(club), x => Assert.InRange(x, min, max));
        }
        Assert.Contains(game.Clubs, c => c.Index < NationalTeams.First && c.TeamValues!.Value with { DayForm = 0 }
            != before[c.Index] with { DayForm = 0 });
    }

    /// <summary>[ORIGINAL] 0x5525A–0x55292: Tageszähler ≡ 0 oder 4 (mod 7), also zweimal je Woche.</summary>
    [Fact]
    public void WeeklyDrift_TwicePerWeek()
    {
        Assert.Equal([0, 4, 7, 11, 14, 18], Enumerable.Range(0, 20).Where(MatchEngine.IsDriftDay));
    }

    /// <summary>
    /// [ORIGINAL] Modus 1: Schritt s = lrandom(0, 2r + 2) − 2 mit r = lrandom(0, 2), je Wert + lrandom(s − 1, s + 1)
    /// (Technik nur mit 1/3), begrenzt auf 1 … 99. Nationalteams und Europapokal-Gegner behalten Kondition und Technik.
    /// </summary>
    [Fact]
    public void WeeklyDrift_SmallSteps_WithinOneToNinetyNine()
    {
        var game = Game();
        MatchEngine.EnsureTeamValues(game);
        var before = game.Clubs.ToDictionary(c => c.Index, c => c.TeamValues!.Value);
        MatchEngine.DriftTeamValues(game, TeamValueDrift.Weekly);
        foreach (var club in game.Clubs)
        {
            var b = before[club.Index];
            var a = club.TeamValues!.Value;
            if (club.Index >= NationalTeams.First)
            {
                Assert.Equal((b.DefenseCondition, b.AttackCondition, b.DefenseTechnique, b.AttackTechnique),
                    (a.DefenseCondition, a.AttackCondition, a.DefenseTechnique, a.AttackTechnique));
                if (club.Index > NationalTeams.Last)
                    Assert.Equal(b, a);
                continue;
            }
            Assert.All(ConditionAndTechnique(club), x => Assert.InRange(x, 1, 99));
            Assert.InRange(a.DefenseCondition - b.DefenseCondition, -3, 5);
            Assert.InRange(a.AttackTechnique - b.AttackTechnique, -3, 5);
        }
    }

    /// <summary>
    /// Saisonverteilung gegen das Video (#204, Abnahme 2), Originaldaten, Managerverein 42 aktiv, 12 Seeds × 4 Saisons.
    /// Video (Reihe `folgen`, 18 Saisons in zwei Spielen): acht Meister, Düsseldorf und Köln zusammen 5 von 18, Meister mit
    /// Datenbankstärke ≤ 72 (Freiburg, Schwenningen, Nürnberg) 8 von 18. Vor der Spur: Düsseldorf und Köln 33 von 48, nie ein
    /// Meister unter 82 (docs/spielberechnung.md §13).
    /// </summary>
    [GameDataFact]
    public void ChampionSpread_LikeVideo()
    {
        var database = ClubDatabase.Load(TestGameData.Load());
        var champions = new List<int>();
        for (var seed = 1; seed <= 12; seed++)
        {
            var setup = new GameSetup();
            setup.Managers.Add(new ManagerSetup { Name = "Probe", ClubIndex = 42 });
            var game = NewGame.Create(database, setup, seed);
            RngTwinProbeTests.Run(game, active: true);
            champions.AddRange(game.History.Select(r => r.Titles.TryGetValue(Title.GermanChampion, out var c) ? c : -1));
        }
        Assert.Equal(48, champions.Count(c => c >= 0));
        var strength = database.Clubs.ToDictionary(c => c.Index, c => c.Strength);
        var topTwo = champions.Count(c => c is 2 or 5);
        var weak = champions.Count(c => strength[c] < 80);
        output.WriteLine($"Meister: {champions.Distinct().Count()} Vereine, Düsseldorf+Köln {topTwo}/48, unter 80: {weak}/48");
        Assert.InRange(topTwo, 0, 23);
        Assert.InRange(weak, 4, 48);
        Assert.True(champions.Distinct().Count() >= 6);
    }

    /// <summary>
    /// [ORIGINAL] Original-Spielstände nach der Wanderung des Spielstarts (FILE01: Kölner EC, Mittelfeld; FILE09: zwei
    /// Manager, Abstiegskampf; beide vor dem ersten Spieltag gesichert): Kondition und Technik jedes Computervereins liegen in
    /// den Grenzen seiner Liga (Vereinsplatz 0–11 Bundesliga, 12–23 Zweite Liga, sonst Oberliga; [0x7C1] = 12, [0x7C2] = 24),
    /// mehrere genau auf einer Grenze. Ohne Wanderung lägen sie bei Datenbank ± 10 % (Düsseldorf bis 101, Rosenheim ab 63).
    /// </summary>
    [GameDataFact]
    public void OriginalSaves_TeamValuesWithinLeagueBounds()
    {
        var onBound = 0;
        foreach (var name in new[] { "FILE01.HOK", "FILE09.HOK" })
        {
            var clubs = OriginalSaveGame.Read(File.ReadAllBytes(OriginalSave(name))).Block("far:9657");
            for (var slot = 0; slot < 48; slot++)
            {
                var record = clubs.AsSpan(slot * 0x3D, 0x3D);
                if (record[0x16] != 0)
                    continue; // Managerverein: Werte aus den Spielern (0x952C)
                var (min, max) = Bounds(slot < 12 ? League.Bundesliga : slot < 24 ? League.ZweiteLiga : League.Oberliga);
                for (var field = 0x18; field <= 0x1B; field++)
                {
                    Assert.InRange(record[field], min, max);
                    if (record[field] == min || record[field] == max)
                        onBound++;
                }
            }
        }
        output.WriteLine($"Werte auf einer Grenze: {onBound}");
        Assert.True(onBound >= 8);
    }

    /// <summary>Original-Spielstand: <c>EM_SAVEGAMES</c> oder <c>spielstaende</c> neben dem aufgelösten <c>gamedata</c>.</summary>
    private static string OriginalSave(string name)
    {
        var folder = Environment.GetEnvironmentVariable("EM_SAVEGAMES");
        if (string.IsNullOrEmpty(folder))
        {
            var root = TestGameData.Load().RootDirectory;
            var real = Directory.ResolveLinkTarget(root.TrimEnd('/', '\\'), true)?.FullName ?? root;
            folder = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(real).TrimEnd('/', '\\'))!, "spielstaende");
        }
        var path = Path.Combine(folder, name);
        return File.Exists(path) ? path
            : throw new FileNotFoundException($"Original-Spielstand {name} fehlt in {folder} (Ordner per EM_SAVEGAMES angeben).");
    }
}
