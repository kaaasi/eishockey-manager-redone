using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur zufallsstrom, Teil 1 (N3 = B): Transfer und Pool ziehen aus dem gemeinsamen Strom <see cref="GameState.Random"/>, in der
/// Reihenfolge des Originals. Jede Probe rechnet die Folge der Würfe mit einer Kopie des Stroms nach (Fundstellen in em.exe) und
/// vergleicht den Zustand danach; die früheren eigenen Quellen bleiben unberührt. Vor der Spur rot: Der gemeinsame Strom stand,
/// gezogen wurde aus <c>ContractRandom</c>, <c>TrendRandom</c> bzw. Quellen aus Saat, Saison und Tag.
/// </summary>
public class ZufallsstromTeil1Tests
{
    private static GameState Create(int seed = 3, params int[] clubs)
    {
        var setup = new GameSetup();
        foreach (var (club, i) in (clubs.Length == 0 ? [2] : clubs).Select((c, i) => (c, i)))
            setup.Managers.Add(new ManagerSetup { Name = $"Trainer {i}", Head = i, ClubIndex = club });
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    private static GameRandom Copy(GameRandom random) => GameRandom.FromState(random.Capture());

    private static int R(Random random, int a, int b) => TransferRules.Random16(random, a, b);

    /// <summary>
    /// A720, A248, A726: 0x07169 je Manager – erste Schleife über die Kaderplätze (Datei 0x07402–0x0795D): Ruhestand random(350, 450)
    /// (0x075CA), Verfall des unaufgeforderten Angebots random(0, 3) für jeden Platz (0x0762D), Verfall des Verlängerungsangebots
    /// random(0, 5) nur mit offenem Angebot (0x07692), Verlängerungsangebot random(0, Grenze) und nur bei 0 random(0, 3) (0x0781E,
    /// 0x07833), Jahre random(0, 100) (0x07876); zweite Schleife (Datei 0x07996–0x07AEF): random(0, 200) &lt; w, dann random(0, 450)
    /// (0x07A1E, 0x07A34). Zwei Manager: der zweite zieht hinter dem ersten weiter (keine je Manager abgeleitete Quelle).
    /// </summary>
    [Fact]
    public void DailyRun_ContractsAndOffers_DrawFromSharedStreamInOriginalOrder()
    {
        var game = Create(3, 2, 14);
        foreach (var manager in game.Managers)
            foreach (var p in game.Clubs[manager.ClubIndex].Players)
            {
                p.Age = 24; // kein Ruhestand: v = 24 · c / 10 ≤ 240 < 350
                p.ContractYears = 2; // kein Verlängerungsangebot, dritter Wurf entfällt
                p.Notice = ContractNotice.None;
                p.UnsolicitedOffer = null;
            }
        var contract = game.ContractRandom.Capture();
        var sim = Copy(game.Random);
        var rolled = 0;
        foreach (var manager in game.Managers)
        {
            var club = game.Clubs[manager.ClubIndex];
            foreach (var p in club.Players)
            {
                R(sim, 350, 450);
                R(sim, 0, 3);
                if (R(sim, 0, SquadRules.OfferLimit(p)) == 0)
                    R(sim, 0, 3);
            }
            foreach (var p in club.Players)
            {
                var w = game.PlayerValueOf(p, club, PlayerValueKind.Price, sim, manager.Level) / 10000;
                if (R(sim, 0, 200) < w && R(sim, 0, 450) == 0)
                    rolled++;
            }
            Assert.Equal(0, rolled); // Saat 3: kein Angebot, die Folge endet je Platz nach den Würfen oben

            game.AnnounceContractEnds(game.CurrentDay, manager);
            Assert.Equal(sim.Capture(), game.Random.Capture());
        }
        Assert.Equal(contract, game.ContractRandom.Capture());
    }

    /// <summary>
    /// A248: Der Verfallswurf random(0, 3) fällt in der ersten Schleife (Datei 0x0762D) zwischen Ruhestand und Verlängerungsangebot;
    /// bei 0 und offenem Angebot verfällt es. Die zweite Schleife (<c>RollUnsolicitedOffers</c>) lässt ein offenes Angebot stehen.
    /// </summary>
    [Fact]
    public void DailyRun_OpenUnsolicitedOffer_ExpiresInFirstLoopOnZero()
    {
        var game = Create(3);
        var manager = game.Managers[0];
        var club = game.Clubs[manager.ClubIndex];
        foreach (var p in club.Players)
        {
            p.Age = 24;
            p.ContractYears = 2;
        }
        var player = club.Players[0];
        var buyer = game.Clubs[20];
        var expired = false;
        for (var run = 0; run < 200 && !expired; run++)
        {
            player.UnsolicitedOffer = buyer;
            game.RollUnsolicitedOffers(manager);
            Assert.Same(buyer, player.UnsolicitedOffer);

            var sim = Copy(game.Random);
            R(sim, 350, 450);
            var expected = R(sim, 0, 3) == 0;
            game.AnnounceContractEnds(game.CurrentDay, manager);
            Assert.Equal(expected, player.UnsolicitedOffer is null);
            expired |= expected;
        }
        Assert.True(expired, "kein Verfall in 200 Läufen");
    }

    /// <summary>
    /// A708, A725: Der Listenwechsel 0x494EF legt den Kaderplatz des Markts über 0x42CB1 an – Energie random(80, 120) (Datei
    /// 0x42E4C), dann 0x4907A (0x42E6B) – alles aus einer Quelle. Probe an <c>RollSlotFields</c> und am Listenwechsel im Spiel.
    /// </summary>
    [Fact]
    public void ListChange_SlotFields_TrendFromSharedStream()
    {
        var p = new Player { Name = "A", Role = PlayerRole.Forward };
        var twin = new Player { Name = "B", Role = PlayerRole.Forward };
        var (random, sim) = (new GameRandom(5), new GameRandom(5));
        TransferPoolRules.RollSlotFields(p, random);
        var strain = R(sim, PlayerFactory.StartStrainMin, PlayerFactory.StartStrainMax);
        twin.RollTrend(sim);
        Assert.Equal((strain, twin.TrendScore, twin.TrendControl), (p.Strain, p.TrendScore, p.TrendControl));

        var game = Create(3);
        var before = game.TransferMarket.Listings.Select(l => l.Player).ToHashSet();
        for (var i = 0; i < 20 && game.TransferMarket.Listings.All(l => before.Contains(l.Player)); i++)
            game.TransferMarket.ChangeList(game, game.Random);
        Assert.Contains(game.TransferMarket.Listings, l => !before.Contains(l.Player) && l.Player.TrendScore != 0);
    }

    /// <summary>
    /// A722: Tendenz der Kaderplätze (0x42CB1 → 0x4907A, Datei 0x42E6B) je Manager und Kaderplatz in Platzfolge aus dem
    /// gemeinsamen Strom: random(35, 65), random(1, 13), random(1, 3), random(0, 1). A676 (Teil 2): direkt danach je Platz +0x19 =
    /// random(3, 12) · 16 + random(3, 12), oberes Halbbyte zuerst, aus demselben Strom. Rest von Teil 3: Der Spielstart würfelt
    /// beides schon im Poolaufbau (Datei 0x5B1CD–0x5B245, <c>ZufallsstromSpielstartTests</c>); <c>ApplyStartSalaries</c> zieht nur
    /// noch für Plätze ohne Steuerbyte nach (Import, „nur Weltkugel“) und sonst nichts.
    /// </summary>
    [Fact]
    public void GameStart_SquadTrend_FromSharedStreamInSlotOrder()
    {
        var game = Create(3, 2, 14);
        var untouched = game.Random.Capture();
        game.ApplyStartSalaries();
        Assert.Equal(untouched, game.Random.Capture());
        foreach (var p in game.Managers.SelectMany(m => game.Clubs[m.ClubIndex].Players))
            p.TrendControl = 0;
        var sim = Copy(game.Random);
        var expected = new List<(int, int)>();
        var hidden = new List<(int, int)>();
        foreach (var manager in game.Managers)
            foreach (var _ in game.Clubs[manager.ClubIndex].Players)
            {
                var score = R(sim, 35, 65);
                var control = R(sim, 1, 13);
                control |= R(sim, 1, 3) << 4;
                control |= R(sim, 0, 1) << 7;
                expected.Add((score, control));
                var over = R(sim, 3, 12);
                hidden.Add((over, R(sim, 3, 12)));
            }

        game.ApplyStartSalaries();

        Assert.Equal(expected,
            game.Managers.SelectMany(m => game.Clubs[m.ClubIndex].Players).Select(p => (p.TrendScore, p.TrendControl)).ToList());
        Assert.Equal(hidden,
            game.Managers.SelectMany(m => game.Clubs[m.ClubIndex].Players).Select(p => (p.HiddenSkill2, p.HiddenSkill1)).ToList());
        Assert.Equal(sim.Capture(), game.Random.Capture());
    }

    /// <summary>
    /// A212: Der Pool des Spielstarts (0x5AF50, Aufruf Datei 0x29896) zieht aus dem gemeinsamen Strom: derselbe Spielstand mit
    /// einem Wurf mehr davor baut einen anderen Pool, und der Strom ist danach weitergezählt. Vorher: eigene Quelle aus der Saat,
    /// beide Pools gleich, Strom unberührt.
    /// </summary>
    [Fact]
    public void GameStart_Pool_DrawsFromSharedStream()
    {
        static List<(string, int, int, int)> Pool(GameState game) =>
            game.TransferMarket.Pool.Select(s => (s.Player.Name, s.Player.Talent, s.Player.Condition, s.Player.Technique)).ToList();

        var (a, b) = (Create(3), Create(3));
        Assert.Equal(Pool(a), Pool(b));
        var before = a.Random.Capture();
        var (marketA, marketB) = (new TransferMarket(), new TransferMarket());
        marketA.ReservePool(a);
        Assert.NotEqual(before, a.Random.Capture());
        _ = b.Random.Next();
        marketB.ReservePool(b);
        Assert.NotEqual(
            marketA.Pool.Select(s => (s.Player.Talent, s.Player.Condition, s.Player.Technique)).ToList(),
            marketB.Pool.Select(s => (s.Player.Talent, s.Player.Condition, s.Player.Technique)).ToList());
    }

    /// <summary>
    /// A721: Laufbahnende eines freien Poolplatzes beim Saisonwechsel (Datei 0x2787A–0x279DA) aus dem gemeinsamen Strom: Wurf
    /// random(37, 42) (0x27897), Alter random(18, 25) (0x2794F), s = random(40, 92) (0x27972), Grundstärken random(s − 5, s + 5)
    /// (0x27985, 0x279BF). [APPROX] Davor der Namenswurf des Remakes, danach Form und Talent.
    /// </summary>
    [Fact]
    public void PoolSeason_CareerEndOfFreeSlot_DrawsFromSharedStream()
    {
        var game = Create(3);
        var slot = game.TransferMarket.Pool.First(s => s.Owner == PoolOwner.Free
            && !game.Clubs.Any(c => c.Players.Contains(s.Player)) && !game.TransferMarket.FreeAgents.Contains(s.Player));
        var old = slot.Player;
        old.Age = 50; // über random(37, 42): hört immer auf
        var contract = game.ContractRandom.Capture();
        var sim = Copy(game.Random);
        R(sim, 37, 42);
        var names = game.Clubs.SelectMany(c => c.Players).Concat(game.TransferMarket.FreeAgents).ToList();
        var name = PlayerFactory.TalentName(names, sim);
        var age = R(sim, 18, 25);
        var s = R(sim, 40, 92);
        var condition = Player.Clamp(R(sim, s - 5, s + 5));
        var technique = Player.Clamp(R(sim, s - 5, s + 5));
        var form = R(sim, 45, 60);
        var talent = R(sim, 0, 9);

        game.RenewAgedPoolPlayer(slot);

        var p = slot.Player;
        Assert.NotSame(old, p);
        Assert.Equal((name, age, condition, technique, form, talent), (p.Name, p.Age, p.Condition, p.Technique, p.Form, p.Talent));
        Assert.Equal(sim.Capture(), game.Random.Capture());
        Assert.Equal(contract, game.ContractRandom.Capture());
    }
}
