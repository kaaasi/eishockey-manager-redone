using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using Xunit.Abstractions;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur spielstaerke-manager (#204, #179): Teamwerte des Laders 0x2EF1B, Tagesform 0x4F527 und Saisonverteilung.
/// Fundstellen: <c>docs/spielberechnung.md</c> §12.
/// </summary>
public class SpielstaerkeManagerTests(ITestOutputHelper output)
{
    private static GameState Game(int managerClub = 2, int seed = 3)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Test", Head = 0, ClubIndex = managerClub });
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    /// <summary>
    /// Regressionstest #204 (vor der Spur rot: der Lader wurde nicht nachgebildet, alle Vereine spielten mit Teamwert ohne
    /// Streuung, Form 50 und Tagesform 50). [ORIGINAL] 0x2EF1B: Kondition/Technik v ± v/10, Form 45 … 55, Tagesform 47 … 53.
    /// </summary>
    [Fact]
    public void Loader_SpreadsTeamValuesWithinOriginalBounds()
    {
        var game = Game();
        MatchEngine.LoadTeamValues(game);
        foreach (var club in game.Clubs)
        {
            var v = Assert.IsType<ClubTeamValues>(club.TeamValues);
            var t = club.Strength / 10;
            foreach (var d in new[] { v.DefenseCondition, v.AttackCondition, v.DefenseTechnique, v.AttackTechnique })
                Assert.InRange(d, -t, t);
            var low = club.Index > 64 ? 40 : 45;
            Assert.InRange(v.DefenseForm, low, low + 10);
            Assert.InRange(v.AttackForm, low, low + 10);
            Assert.InRange(v.DayForm, 47, 53);
        }
        var spreads = game.Clubs.Where(c => c.Index is < NationalTeams.First or > NationalTeams.Last)
            .SelectMany(c => new[] { c.TeamValues!.Value.DefenseTechnique, c.TeamValues!.Value.AttackCondition }).ToList();
        Assert.Contains(spreads, d => d != 0);
        Assert.True(game.Clubs.Select(c => c.TeamValues!.Value.DayForm).Distinct().Count() > 1);
    }

    /// <summary>[ORIGINAL] Nationalteams (52 … 63) bekommen Kondition und Technik aus DGROUP 0x4C4E, ohne Streuung.</summary>
    [Fact]
    public void Loader_NationalTeamsWithoutSpread()
    {
        var game = Game();
        MatchEngine.LoadTeamValues(game);
        foreach (var club in game.Clubs.Where(c => c.Index is >= NationalTeams.First and <= NationalTeams.Last))
        {
            var v = club.TeamValues!.Value;
            Assert.Equal(0, v.DefenseCondition | v.AttackCondition | v.DefenseTechnique | v.AttackTechnique);
        }
    }

    /// <summary>
    /// Das Original lädt einmal (Lader 0x2EF1B); danach wandern Kondition, Technik und Form (0x9D1B, Spur
    /// spielberechnung-komplett, #204 – vorher hieß der Test „…KeptAcrossSeasons“ und prüfte feste Werte). Die Werte werden
    /// beim ersten Spiel gezogen, nicht neu geladen: Der Lader läuft nur einmal.
    /// </summary>
    [Fact]
    public void TeamValues_DrawnOnceThenDrift()
    {
        var game = Game();
        Assert.All(game.Clubs, c => Assert.Null(c.TeamValues));
        TestSeason.PlayMatchDay(game);
        Assert.All(game.Clubs, c => Assert.NotNull(c.TeamValues));
        var first = game.Clubs.ToDictionary(c => c.Index, c => c.TeamValues!.Value with { DayForm = 0 });
        RngTwinProbeTests.Run(game, active: false, changes: 1);
        Assert.Contains(game.Clubs, c => c.Index < NationalTeams.First && first[c.Index] != (c.TeamValues!.Value with { DayForm = 0 }));
        // Europapokal-Gegner (Index > 64) wandern nicht
        Assert.All(game.Clubs.Where(c => c.Index > NationalTeams.Last),
            c => Assert.Equal(first[c.Index], c.TeamValues!.Value with { DayForm = 0 }));
    }

    /// <summary>Regressionstest: LiveTeam übernimmt Streuung, Form und Tagesform des Laders (vorher 50/50 fest).</summary>
    [Fact]
    public void LiveTeam_UsesLoaderValues_ForComputerClubs()
    {
        var game = Game();
        var club = game.Clubs.First(c => c.Index != game.Managers[0].ClubIndex && c.Players.Count > 0 && MatchEngine.TeamValue(game, c) is > 10 and < 90);
        var opponent = game.Clubs.First(c => c != club && c.Players.Count > 0);
        club.TeamValues = new ClubTeamValues(-3, 2, 1, -4, 46, 54, 52);
        var value = MatchEngine.TeamValue(game, club);
        var team = MatchEngine.LiveTeamOf(game, club, opponent, managed: false, managerMatch: false);
        Assert.Equal(new PartValues(value - 3, value + 1, 46), team.Defense);
        Assert.Equal(new PartValues(value + 2, value - 4, 54), team.Attack);
        Assert.Equal(52, team.ClubForm);
    }

    /// <summary>Managervereine: Reihenwerte im Original aus den Spielern (0x98F8) – keine Streuung, aber Tagesform.</summary>
    [Fact]
    public void LiveTeam_ManagedClub_OnlyDayForm()
    {
        var game = Game();
        var club = game.Clubs[game.Managers[0].ClubIndex];
        var opponent = game.Clubs.First(c => c != club && c.Players.Count > 0);
        club.TeamValues = new ClubTeamValues(-3, 2, 1, -4, 46, 54, 52);
        var value = MatchEngine.TeamValue(game, club);
        var team = MatchEngine.LiveTeamOf(game, club, opponent, managed: true, managerMatch: true);
        Assert.Equal(new PartValues(value, value, 50), team.Defense);
        Assert.Equal(new PartValues(value, value, 50), team.Attack);
        Assert.Equal(52, team.ClubForm);
    }

    /// <summary>[ORIGINAL] 0x4F527: Tagesform ± random(0, 2·|Tordifferenz|), begrenzt auf 47 … 53; Unentschieden ändert nichts.</summary>
    [Theory]
    [InlineData(50, 3, 50, 53)]
    [InlineData(50, -2, 47, 50)]
    [InlineData(53, 5, 53, 53)]
    [InlineData(47, -4, 47, 47)]
    [InlineData(49, 0, 49, 49)]
    public void DayForm_FollowsGoalDifference(int start, int difference, int min, int max)
    {
        var club = new Club { Index = 3, Name = "X", Strength = 60, TeamValues = new ClubTeamValues(0, 0, 0, 0, 50, 50, start) };
        var seen = new HashSet<int>();
        var random = new Random(1);
        for (var i = 0; i < 200; i++)
        {
            club.TeamValues = club.TeamValues!.Value with { DayForm = start };
            MatchEngine.UpdateDayForm(club, difference, random);
            seen.Add(club.TeamValues!.Value.DayForm);
        }
        Assert.All(seen, f => Assert.InRange(f, min, max));
        Assert.Contains(min, seen);
        Assert.Contains(max, seen);
    }

    /// <summary>Tagesform wird nur nach Ligaspielen nachgeführt (Wettbewerbscode 7), nicht im Pokal.</summary>
    [Fact]
    public void DayForm_OnlyAfterLeagueMatches()
    {
        var game = Game();
        MatchEngine.LoadTeamValues(game);
        var (home, away) = (game.Clubs[10], game.Clubs[11]);
        var before = (home.TeamValues, away.TeamValues);
        var changed = false;
        for (var i = 0; i < 30 && !changed; i++)
        {
            MatchEngine.PlayWithEvents(game, home, away, MatchCompetition.DebCup, knockOut: true, new Random(i));
            Assert.Equal(before, (home.TeamValues, away.TeamValues));
            MatchEngine.PlayWithEvents(game, home, away, MatchCompetition.League, knockOut: false, new Random(i));
            changed = before != (home.TeamValues, away.TeamValues);
        }
        Assert.True(changed);
    }

    /// <summary>Spielstand vor Schema 12: TeamValues fehlen → null, beim nächsten Spiel gezogen; Sichern und Laden erhält sie.</summary>
    [Fact]
    public void SaveGame_RoundTripAndMigration()
    {
        var game = Game();
        TestSeason.PlayMatchDay(game);
        var data = SaveGame.Save(game);
        var loaded = Load(data);
        Assert.Equal(game.Clubs.Select(c => c.TeamValues), loaded.Clubs.Select(c => c.TeamValues));

        var root = System.Text.Json.Nodes.JsonNode.Parse(data)!.AsObject();
        root["schema"] = 11;
        foreach (var club in root["vereine"]!.AsArray().OfType<System.Text.Json.Nodes.JsonObject>())
            Assert.True(club.Remove("TeamValues"));
        var old = Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.All(old.Clubs, c => Assert.Null(c.TeamValues));
        TestSeason.PlayMatchDay(old);
        Assert.All(old.Clubs, c => Assert.NotNull(c.TeamValues));
    }

    /// <summary>
    /// Mehrsaisonprobe Modus 1, 2 und 4 (Abnahme 3), passiv und aktiv: vier Saisonwechsel. Seit der Wanderung 0x9D1B
    /// (#204) bleiben die Teamwerte nicht gleich: Kondition und Technik liegen stets in 1 … 99 (Lader bis 101 bis zur ersten
    /// Wanderung), direkt nach jedem Saisonwechsel in den Grenzen der Liga, die Form in 45 … 55, die Tagesform in 47 … 53;
    /// Europapokal-Gegner bleiben gleich. Die Abdrücke stehen in
    /// <see cref="RngTwinProbeTests"/>, <see cref="GameModeTests.Mode4_ReferenceValues_Unchanged"/> und
    /// <see cref="SpielberechnungAnschlussTests.FourSeasonChanges_WorldOnly"/>.
    /// </summary>
    [Theory]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public void FourSeasonChanges_TeamValuesWithinBounds(bool flag, bool globe, bool active)
    {
        var game = GameModeTests.Create(7, flag, globe, 2);
        Dictionary<int, ClubTeamValues>? first = null;
        var changes = 0;
        var seasonStart = false;
        void Check(GameState g)
        {
            if (first is null && g.Clubs.All(c => c.TeamValues is not null))
                first = g.Clubs.ToDictionary(c => c.Index, c => c.TeamValues!.Value with { DayForm = 0 });
            if (first is null)
                return;
            foreach (var c in g.Clubs)
            {
                var v = c.TeamValues!.Value;
                Assert.InRange(v.DayForm, MatchEngine.DayFormMin, MatchEngine.DayFormMax);
                if (c.Index > NationalTeams.Last)
                {
                    Assert.Equal(first[c.Index], v with { DayForm = 0 });
                    continue;
                }
                Assert.InRange(v.DefenseForm, 45, 55);
                Assert.InRange(v.AttackForm, 45, 55);
                if (c.Index >= NationalTeams.First || c.Players.Count == 0)
                    continue;
                var (min, max) = seasonStart ? c.League switch
                {
                    League.Bundesliga => (69, 94),
                    League.ZweiteLiga => (39, 73),
                    _ => (15, 43),
                } : (1, 99);
                foreach (var x in new[] { v.DefenseCondition, v.AttackCondition, v.DefenseTechnique, v.AttackTechnique })
                    Assert.InRange(c.Strength + x, min, max);
            }
        }
        for (; changes < 4 && game.Phase != SeasonPhase.GameOver; changes++)
        {
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 3000, "Saisonende nicht erreicht");
                foreach (var m in game.Managers)
                    m.Money = Math.Max(m.Money, 10_000_000);
                if (active)
                    TestSeason.KeepSquad(game, game.Managers[0]);
                game.Continue();
                Check(game);
            }
            game.StartNextSeason();
            seasonStart = true;
            Check(game);
            seasonStart = false;
        }
        Assert.Equal(4, changes);
        Assert.NotNull(first);
    }

    private static GameState Load(byte[] data)
    {
        var result = SaveGame.Load(data);
        Assert.True(result.Success, result.Error);
        return result.Game!;
    }

    /// <summary>
    /// Saisonverteilung (#204, Abnahme 2): Meister über 12 Seeds × 4 Saisons mit Originaldaten, Managerverein 42 (Oberliga)
    /// aktiv, wie die Nachprüfung Runde 7 (vorher Düsseldorf 21 von 48). Ausgabe für den PR; geprüft wird nur, dass die
    /// Titel streuen. Ergebnis und Einordnung: <c>docs/spielberechnung.md</c> §12.
    /// </summary>
    [GameDataFact]
    public void SeasonDistribution_OriginalDatabase()
    {
        var champions = new Dictionary<int, int>();
        var perSeed = new List<string>();
        long goals = 0, games = 0, draws = 0;
        for (var seed = 1; seed <= 12; seed++)
        {
            var setup = new GameSetup();
            setup.Managers.Add(new ManagerSetup { Name = "Probe", ClubIndex = 42 });
            var game = NewGame.Create(ClubDatabase.Load(TestGameData.Load()), setup, seed);
            var seq = new List<int>();
            RngTwinProbeTests.Run(game, active: true, beforeChange: g =>
            {
                foreach (var r in g.Results.Where(r => r.League == League.Bundesliga))
                {
                    goals += r.HomeGoals + r.AwayGoals;
                    games++;
                    if (r.HomeGoals == r.AwayGoals)
                        draws++;
                }
            });
            foreach (var record in game.History)
                if (record.Titles.TryGetValue(Title.GermanChampion, out var c))
                {
                    seq.Add(c);
                    champions[c] = champions.GetValueOrDefault(c) + 1;
                }
            perSeed.Add($"Seed {seed}: {string.Join(",", seq)}");
        }
        foreach (var line in perSeed)
            output.WriteLine(line);
        foreach (var (club, n) in champions.OrderByDescending(kv => kv.Value))
            output.WriteLine($"Verein {club}: {n}");
        output.WriteLine($"Bundesliga: {goals / (double)games:F2} Tore je Spiel, {100.0 * draws / games:F1} % Unentschieden");
        Assert.Equal(48, champions.Values.Sum());
        Assert.True(champions.Count >= 3);
    }
}
