using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur zufallsstrom, Rest von Teil 3 (N3 = B, A558): Der Spielstart zieht in der Reihenfolge des Originals aus dem gemeinsamen
/// Strom <see cref="GameState.Random"/> – Poolaufbau 0x5AF50 mit den Kaderplätzen der Manager (Datei 0x5B0BB–0x5B2BE),
/// Managerschleife der Spielvorbereitung (Platzwahl Datei 0x29C91, Startwerte Datei 0x29DA4–0x29EF7), Nationalspieler (Datei
/// 0x2A0B5), Markt nach der Wanderung (Datei 0x43A83, 0x43A9F). Jede Probe rechnet die Würfe ab der Grenze eines Schritts mit einer
/// Kopie des Stroms nach. Vor dem Umbau: Platzwahl vor dem Pool, Belastung, Alter, Startwerte und Markt aus einer eigenen Quelle
/// (Saat · 7919 + 17), Tendenz und +0x19 erst nach allen Kadern, drei Mischungen ohne Gegenstück vor dem Spielplan.
/// </summary>
public class ZufallsstromSpielstartTests
{
    private sealed record Steps(Dictionary<NewGame.StartStep, RngState> Stream,
        Dictionary<NewGame.StartStep, List<(int Strain, int Trend, int Control, int Age, int Over, int Under, int Condition, int Technique, int Form)>> Squads);

    private static (GameState Game, Steps Steps) Create(int seed, GameDuration duration, params int[] clubs) =>
        Create(TestGames.CreateDatabase(), seed, duration, clubs);

    private static (GameState Game, Steps Steps) Create(ClubDatabase database, int seed, GameDuration duration, params int[] clubs)
    {
        var setup = new GameSetup { Duration = duration };
        foreach (var (club, i) in clubs.Select((c, i) => (c, i)))
            setup.Managers.Add(new ManagerSetup { Name = $"Trainer {i}", Head = i, ClubIndex = club });
        var steps = new Steps([], []);
        var game = NewGame.Create(database, setup, seed, (step, g) =>
        {
            steps.Stream[step] = g.Random.Capture();
            steps.Squads[step] = g.Managers.SelectMany(m => g.Clubs[m.ClubIndex].Players)
                .Select(p => (p.Strain, p.TrendScore, p.TrendControl, p.Age, p.HiddenSkill2, p.HiddenSkill1, p.Condition, p.Technique, p.Form))
                .ToList();
        });
        return (game, steps);
    }

    private static int R(Random random, int a, int b) => TransferRules.Random16(random, a, b);

    /// <summary>
    /// Poolaufbau 0x5AF50 (Aufruf Datei 0x29896): Startwerte je Index (Datei 0x5AFB2–0x5B0B6), dann je Manager und Kaderplatz
    /// Belastung random(80, 120) (0x42E4C in 0x42CB1, Aufruf Datei 0x5B1CD), Tendenz random(35, 65) und Steuerbyte random(1, 13),
    /// random(1, 3), random(0, 1) (0x4907A, Datei 0x42E6B), Alter random(19, 31) (Datei 0x5B1F5), +0x19 = random(3, 12) · 16 +
    /// random(3, 12) (Datei 0x5B218, 0x5B245); danach je Index Talent random(0, 9) (Datei 0x5B2E8) und der Rollenwert (Datei
    /// 0x5B3BB–0x5B49D). Die synthetische Datenbank hat keine Namen: 0x5AAA0 zieht nichts. Die Nationalspieler benennt erst Datei
    /// 0x2A0B5 – der Strom steht nach dem Rollenwert an der Grenze zur Managerschleife. Das Talent des Kaderplatzes ist das seines
    /// Poolplatzes.
    /// </summary>
    [Fact]
    public void PoolBuild_ManagerSquadSlots_DrawnBetweenStartValuesAndTalents()
    {
        var (game, steps) = Create(3, GameDuration.Unbegrenzt, 2, 14);
        var layout = TransferPoolLayout.Original;
        var sim = GameRandom.FromState(steps.Stream[NewGame.StartStep.Pool]);
        for (var i = 1; i < layout.Size; i++)
            TransferPoolRules.RollStartValues(new Player { Name = "", Role = layout.RoleOf(i) }, sim);
        var expected = new List<(int, int, int, int, int, int)>();
        for (var k = 0; k < 2 * 25; k++) // Start-Liga Bundesliga: 2 + 10 + 13 Plätze je Manager
        {
            var strain = R(sim, 80, 120);
            var score = R(sim, 35, 65);
            var control = R(sim, 1, 13);
            control |= R(sim, 1, 3) << 4;
            control |= R(sim, 0, 1) << 7;
            var age = R(sim, 19, 31);
            var over = R(sim, 3, 12);
            expected.Add((strain, score, control, age, over, R(sim, 3, 12)));
        }
        var talents = new int[layout.Size];
        for (var i = 1; i < layout.Size; i++)
            talents[i] = R(sim, 0, 9);
        for (var i = 1; i < layout.Size; i++)
            TransferPoolRules.RollRoleValue(layout, i, sim);

        Assert.Equal(expected, steps.Squads[NewGame.StartStep.Managers].Select(p => (p.Strain, p.Trend, p.Control, p.Age, p.Over, p.Under)).ToList());
        Assert.Equal(sim.Capture(), steps.Stream[NewGame.StartStep.EuropeanCup]); // danach lost der Europapokal (Datei 0x298B1, Teil 4)
        // bis zum Ende des Spielstarts unverändert (kein zweiter Wurf, keine eigene Quelle)
        Assert.Equal(expected, steps.Squads[NewGame.StartStep.Done].Select(p => (p.Strain, p.Trend, p.Control, p.Age, p.Over, p.Under)).ToList());
        var seats = TransferMarket.ManagerSquadIndices(game, layout).ToList();
        Assert.Equal(50, seats.Count);
        foreach (var (_, _, index) in seats)
        {
            var slot = game.TransferMarket.Pool[index - 1];
            Assert.Contains(slot.Player, game.Clubs[slot.Club].Players);
            Assert.Equal(talents[index], slot.Player.Talent);
        }
    }

    /// <summary>
    /// Originaldatenbank, KÖLNER EC (zwei leere Datenbankplätze, FILE01: Platz 12 und 25 mit Namen anderer Vereine): Ein leerer
    /// Platz zieht seinen Namen im Poolaufbau aus dem Strom (0x5AAA0, Aufruf Datei 0x5B190) – Kaderplatz random(Anfang, Ende − 1)
    /// der Art, Verein random(0, 11), Managerverein neu gezogen, leerer Name → neuer Versuch – direkt vor den Würfen des Platzes.
    /// Der gezogene Spieler steht danach nur im Managerkader; die Nationalspieler (Datei 0x2A0B5) ziehen nach der Managerschleife.
    /// </summary>
    [GameDataFact]
    public void PoolBuild_EmptyDatabaseSlot_DrawsNameFromSharedStream()
    {
        var database = ClubDatabase.Load(TestGameData.Load());
        var cologne = database.Clubs.Single(c => c.Name.Contains("LNER EC", StringComparison.Ordinal)).Index;
        var (game, steps) = Create(database, 3, GameDuration.Unbegrenzt, cologne);
        var names = game.TransferMarket.DatabaseNames.ToDictionary(n => (n.Club, n.Slot), n => n.Name);
        var layout = TransferPoolLayout.Original;
        var sim = GameRandom.FromState(steps.Stream[NewGame.StartStep.Pool]);
        for (var i = 1; i < layout.Size; i++)
            TransferPoolRules.RollStartValues(new Player { Name = "", Role = layout.RoleOf(i) }, sim);
        int[] bounds = [0, 2, 12, 25];
        var expected = new List<(string, int, int)>();
        var drawn = new List<string>();
        for (var slot = 0; slot < 25; slot++)
        {
            var role = slot < 2 ? 0 : slot < 12 ? 1 : 2;
            if (!names.TryGetValue((cologne, slot), out var name))
            {
                while (true)
                {
                    var s = R(sim, bounds[role], bounds[role + 1] - 1);
                    int c;
                    do
                        c = R(sim, 0, 11);
                    while (c == cologne);
                    if (names.TryGetValue((c, s), out name) && !drawn.Contains(name))
                        break;
                }
                drawn.Add(name);
            }
            var strain = R(sim, 80, 120);
            for (var k = 0; k < 4; k++)
                sim.Next(); // Tendenz und Steuerbyte (0x4907A), in der synthetischen Probe nachgerechnet
            expected.Add((name, strain, R(sim, 19, 31)));
            R(sim, 3, 12);
            R(sim, 3, 12);
        }

        Assert.Equal(2, drawn.Count);
        var squad = game.Clubs[cologne].Players;
        Assert.Equal(expected, squad.Select(p => (p.Name, p.Strain, p.Age)).ToList());
        foreach (var name in drawn)
            Assert.Equal(1, game.Clubs.SelectMany(c => c.Players).Concat(game.TransferMarket.Pool.Select(o => o.Player)).Distinct()
                .Count(p => p.Name == name));
        Assert.NotEqual(steps.Stream[NewGame.StartStep.Nationals], steps.Stream[NewGame.StartStep.Rest]); // Datei 0x2A0B5 → 0x5ACA0
        Assert.Equal(steps.Stream[NewGame.StartStep.Rest], steps.Stream[NewGame.StartStep.Drift]);
    }

    /// <summary>
    /// A558: Managerschleife Datei 0x29B57–0x2A0A3 nach dem Pool, je Manager: Zufallsplatz der Start-Liga random(erster, erster + 11)
    /// (Datei 0x29C91), neu gezogen bei dem Verein eines früheren Managers (Datei 0x29C9B–0x29CC3) oder Platz 0 (Datei 0x29CCC),
    /// Tausch (Datei 0x29CF2); dann je Kaderplatz drei Würfe random(Basis − 2, Basis + 2) – Kondition, Technik, verworfen (Datei
    /// 0x29E00) – und Form random(40, 60) (Datei 0x29E18). Erst danach der nächste Manager. Verein 14 (2. Liga) kommt durch den
    /// Tausch in die Bundesliga.
    /// </summary>
    [Fact]
    public void ManagerLoop_PlaceThenStartValues_PerManagerAfterPool()
    {
        const int seed = 3;
        var (game, steps) = Create(seed, GameDuration.Unbegrenzt, 2, 14);
        var sim = GameRandom.FromState(steps.Stream[NewGame.StartStep.Managers]);
        var places = NewGame.StartOrder(seed);
        var start = SquadSetup.StartBase(League.Bundesliga, StartStrength.Mittelfeld);
        var expected = new List<(int, int, int)>();
        var earlier = new List<int>();
        foreach (var club in new[] { 2, 14 })
        {
            int r;
            do
                r = sim.Next(12);
            while (r == 0 || earlier.Contains(places[r]));
            var own = Array.IndexOf(places, club);
            (places[own], places[r]) = (places[r], club);
            earlier.Add(club);
            for (var k = 0; k < 25; k++)
            {
                var condition = R(sim, start - 2, start + 2);
                var technique = R(sim, start - 2, start + 2);
                R(sim, start - 2, start + 2);
                expected.Add((condition, technique, R(sim, 40, 60)));
            }
        }

        Assert.Equal(expected, steps.Squads[NewGame.StartStep.Nationals].Select(p => (p.Condition, p.Technique, p.Form)).ToList());
        Assert.Equal(sim.Capture(), steps.Stream[NewGame.StartStep.Nationals]);
        Assert.Equal(places.Take(12), game.LeagueSlots(League.Bundesliga));
        Assert.Equal(places.Skip(12).Take(12), game.LeagueSlots(League.ZweiteLiga));
        Assert.Equal(League.Bundesliga, game.Clubs[14].League);
        Assert.Equal(12, game.LeagueClubs(League.Bundesliga).Count());
        Assert.All(game.Managers.SelectMany(m => game.Clubs[m.ClubIndex].Players), p => Assert.True(p.MonthlySalary > 0));
    }

    /// <summary>
    /// Sonderfall „eine Saison, ein Manager“ (Datei 0x29CFA–0x29D9C): nach der Platzwahl Zufallsplätze der Bundesliga, bis Platz 0
    /// fällt (Datei 0x29D3B, 0x29D76) – die Würfe kommen aus dem Strom, vor den Startwerten.
    /// </summary>
    [Fact]
    public void ManagerLoop_OneSeasonSingleManager_DrawsUntilPlaceZero()
    {
        const int seed = 5;
        var (game, steps) = Create(seed, GameDuration.EineSaison, 2);
        var sim = GameRandom.FromState(steps.Stream[NewGame.StartStep.Managers]);
        int r;
        do
            r = sim.Next(12);
        while (r == 0);
        while (sim.Next(12) != 0)
        {
        }
        var start = SquadSetup.StartBase(League.Bundesliga, StartStrength.Mittelfeld);
        for (var k = 0; k < 25; k++)
        {
            R(sim, start - 2, start + 2);
            R(sim, start - 2, start + 2);
            R(sim, start - 2, start + 2);
            R(sim, 40, 60);
        }

        Assert.Equal(sim.Capture(), steps.Stream[NewGame.StartStep.Nationals]);
        Assert.Equal(2, game.LeagueSlots(League.Bundesliga)[0]);
    }

    /// <summary>
    /// Nach der Spielvorbereitung zieht bis zur Wanderung (Datei 0x43A73) nichts aus dem Strom (Teil 4: auch keine Saat einer Pokalquelle mehr): keine Mischung vor dem Spielplan
    /// (das Original mischt dort nicht), Computerkader aus der eigenen Quelle (ohne Gegenstück), Tendenz und +0x19 nicht ein
    /// zweites Mal. Der Markt (Datei 0x43A83 → 0x057E3, Datei 0x43A9F → 0x494EF) zieht nach der Wanderung aus dem Strom.
    /// </summary>
    [Fact]
    public void AfterSetup_NothingDrawsBeforeDrift_MarketDrawsAfterIt()
    {
        var (game, steps) = Create(3, GameDuration.Unbegrenzt, 2, 14);

        Assert.Equal(steps.Stream[NewGame.StartStep.Rest], steps.Stream[NewGame.StartStep.Drift]);
        Assert.NotEqual(steps.Stream[NewGame.StartStep.Drift], steps.Stream[NewGame.StartStep.Market]);
        Assert.NotEqual(steps.Stream[NewGame.StartStep.Market], steps.Stream[NewGame.StartStep.Turn]);
        Assert.NotEmpty(game.TransferMarket.Listings);
    }

    /// <summary>
    /// Der Markt des Spielstarts hängt am Strom: ein Wurf mehr vor dem Verteilen (Datei 0x43A83) gibt eine andere Liste. Vorher
    /// (eigene Quelle aus der Saat) blieb sie gleich.
    /// </summary>
    [Fact]
    public void StartMarket_FollowsSharedStream()
    {
        static List<string> Listed(GameState g) => g.TransferMarket.Listings.Select(l => l.Player.Name).ToList();

        var (a, _) = Create(3, GameDuration.Unbegrenzt, 2);
        var (b, _) = Create(3, GameDuration.Unbegrenzt, 2);
        Assert.Equal(Listed(a), Listed(b));
        _ = b.Random.Next();
        a.TransferMarket.OpenStartMarket(a);
        b.TransferMarket.OpenStartMarket(b);
        Assert.NotEqual(Listed(a), Listed(b));
    }
}
