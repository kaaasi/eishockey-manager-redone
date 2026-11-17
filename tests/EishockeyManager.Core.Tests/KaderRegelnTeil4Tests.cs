using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur <c>kader-regeln</c>, Runde 22, Teil 4: Kaderplatz aus Talentsuche und Jugend (A827), leere Datenbankplätze im Kaderaufbau
/// (A394), Nachfolger eines Ruheständlers im Pool (A631).
/// </summary>
public class KaderRegelnTeil4Tests
{
    private static GameRandom Copy(GameRandom random) => GameRandom.FromState(random.Capture());

    private static int R(Random random, int a, int b) => TransferRules.Random16(random, a, b);

    /// <summary>
    /// A827: [ORIGINAL] 0x06D7F schreibt nach dem Gehalt +0x2C = Wert(Preis) (Datei 0x06F5E–0x06F74, 0x14C7:0x70 Art 0) – im
    /// Remake <see cref="Player.PurchasePrice"/>, der Listenpreis beim Verkauf. Vorher blieb er 0.
    /// </summary>
    [Theory]
    [InlineData(3, 60)]
    [InlineData(11, 45)]
    public void ScoutedTalent_PriceIsValuePrice(int seed, int s)
    {
        var game = TestGames.Create(seed);
        var manager = game.ActiveManager;

        var p = game.JoinScoutedTalent(manager, s);

        Assert.NotNull(p);
        Assert.True(p.PurchasePrice > 0);
        Assert.Equal(Contracts.ValuePrice(game, manager, p), p.PurchasePrice);
    }

    /// <summary>
    /// A827: [ORIGINAL] Die Platzsuche random(1, 179) (Datei 0x06DAE) wiederholt ohne Grenze, bis ein freier Platz fällt. Der
    /// einzige freie Platz fällt hier erst nach mehr als 1.500 Würfen; das Talent ist trotzdem sein Spieler (vorher ein neuer
    /// Spieler mit gezogenem Namen) und der Strom steht danach um genau diese Würfe weiter.
    /// </summary>
    [Fact]
    public void ScoutedTalent_SearchesWithoutLimit()
    {
        var game = TestGames.Create(3);
        var manager = game.ActiveManager;
        var last = game.TransferMarket.PoolLayout.LeagueLast;
        // Stromstand suchen, an dem ein Platz erst nach mehr als 1.500 Würfen das erste Mal fällt (Sammelbilderproblem).
        var (target, hit) = (-1, 0);
        for (var advance = 0; target < 0; advance++)
        {
            Assert.True(advance < 2_000, "kein Stromstand gefunden");
            var sim = Copy(game.Random);
            var seen = new HashSet<int>();
            var n = 0;
            int index;
            do
            {
                index = R(sim, 1, last);
                n++;
            } while (!seen.Add(index) || seen.Count < last);
            if (n > TalentAttemptsBefore)
                (target, hit) = (index, n);
            else
                _ = game.Random.Next();
        }
        foreach (var slot in game.TransferMarket.Pool.Where(s => s.Index >= 1 && s.Index <= last))
            slot.Owner = slot.Index == target ? PoolOwner.Free : PoolOwner.Taken;
        var expected = game.TransferMarket.Pool.First(s => s.Index == target).Player;
        var after = Copy(game.Random);
        for (var n = 0; n < hit; n++)
            _ = R(after, 1, last);
        var age = R(after, PlayerFactory.ScoutedMinAge, PlayerFactory.ScoutedMaxAge); // erster Wurf nach der Suche (Datei 0x06DE1)

        var p = game.JoinScoutedTalent(manager, 60);

        Assert.Same(expected, p);
        Assert.Equal(age, p!.Age);
    }

    /// <summary>
    /// A631: [ORIGINAL] Neuanlage im selben Pooleintrag (Datei 0x27947–0x279D6) schreibt nur Alter, Grundstärken und +0x19 – Name,
    /// Talent und Form (+0x17) bleiben, gezogen werden nur random(37, 42), Alter, s und die beiden Grundstärken. Vorher: neuer
    /// Name aus <see cref="PlayerFactory.TalentName"/> (zwei bis drei Würfe im gemeinsamen Strom), Form und Talent neu gewürfelt.
    /// </summary>
    [Theory]
    [InlineData(3)]
    [InlineData(17)]
    public void PoolRenewal_KeepsNameTalentForm(int seed)
    {
        var game = TestGames.Create(seed);
        var slot = game.TransferMarket.Pool.First(s => s.Owner == PoolOwner.Free
            && !game.Clubs.Any(c => c.Players.Contains(s.Player)) && !game.TransferMarket.FreeAgents.Contains(s.Player));
        var old = slot.Player;
        old.Age = 50;
        old.Talent = 7;
        old.Form = 58;
        old.PoolForm = 47; // Pool +0x17 weicht von der Kaderform ab
        var sim = Copy(game.Random);
        _ = R(sim, 37, 42);
        var age = R(sim, 18, 25);
        var s = R(sim, 40, 92);
        var condition = Player.Clamp(R(sim, s - 5, s + 5));
        var technique = Player.Clamp(R(sim, s - 5, s + 5));

        game.RenewAgedPoolPlayer(slot);

        var p = slot.Player;
        Assert.NotSame(old, p);
        Assert.Contains(old, game.RetiredPlayers);
        Assert.Equal((old.Name, old.Role, 7, 47), (p.Name, p.Role, p.Talent, p.Form));
        Assert.Equal((age, condition, technique), (p.Age, p.Condition, p.Technique));
        Assert.Equal(sim.Capture(), game.Random.Capture());
    }

    /// <summary>
    /// A394: [ORIGINAL] Kaderplätze des Managers (Datei 0x5B0CC–0x5B2BE) lesen den Datenbankplatz gleicher Nummer; ein leerer
    /// Platz bleibt an seiner Stelle und bekommt einen gezogenen Namen. kb\23.vga: EC HARZ BRAUNLAGE Platz 8 leer (Platz 9 belegt),
    /// EHC STRAUBING Platz 10 leer (11 belegt), 1.EV WEIDEN Platz 8 leer (9 belegt) – vorher rückte der Verteidiger dahinter auf
    /// den leeren Platz vor, und der gezogene Name stand am Ende.
    /// </summary>
    [GameDataTheory]
    [InlineData("EC HARZ BRAUNLAGE")]
    [InlineData("EHC STRAUBING")]
    [InlineData("1.EV WEIDEN")]
    public void ManagerSquad_EmptyDatabaseSlotStaysInPlace(string name)
    {
        var database = ClubDatabase.Load(TestGameData.Load());
        var source = database.Clubs.Single(c => c.Name == name);
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", ClubIndex = source.Index, StartLeague = League.Oberliga });
        setup.StartLeague = League.Oberliga;

        var game = NewGame.Create(database, setup, 5);

        var club = game.Clubs.Single(c => c.Name == name);
        var (g, d, f) = SquadSetup.ManagedSquadSize(League.Oberliga);
        Assert.Equal(g + d + f, club.Players.Count);
        var bySlot = source.Players.ToDictionary(p => p.Slot, p => p.Name);
        for (var i = 0; i < club.Players.Count; i++)
        {
            if (bySlot.TryGetValue(i, out var expected) && expected != club.Players[i].Name)
                Assert.Fail($"Platz {i + 1}: {club.Players[i].Name} statt {expected}");
            if (!bySlot.ContainsKey(i))
                Assert.DoesNotContain(club.Players[i].Name, source.Players.Select(p => p.Name));
        }
    }

    /// <summary>
    /// Mehrsaisonprobe (4 Saisonwechsel, passiv und aktiv): Jeder Nachfolger eines Ruheständlers im Pool trägt Name und Rolle des
    /// Platzes (A631); aktiv holt der Manager je Saison ein Talent, dessen Preis Wert(Preis) ist (A827). Rundlauf des Spielstands.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultiSeason_FourChanges_RenewalsAndTalents(bool active)
    {
        var game = TestGames.Create(21);
        var manager = game.ActiveManager;
        var (renewals, talents) = (0, 0);
        for (var change = 0; change < 4; change++)
        {
            var scouted = false;
            for (var day = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; day++)
            {
                Assert.True(day < 1000, "Saisonende nicht erreicht");
                TestSeason.KeepSquad(game, manager);
                manager.Money = Math.Max(manager.Money, 5_000_000);
                if (active && !scouted && game.Phase == SeasonPhase.Running && game.OwnedPlayers(manager) < TalentScout.MaxOwned - 1)
                {
                    var p = game.JoinScoutedTalent(manager, 55);
                    Assert.NotNull(p);
                    Assert.Equal(Contracts.ValuePrice(game, manager, p), p.PurchasePrice);
                    (scouted, talents) = (true, talents + 1);
                }
                game.Continue();
            }
            // Je Wechsel zwei freie Poolplätze außerhalb der Kader über die Altersgrenze random(37, 42) heben.
            foreach (var aged in game.TransferMarket.Pool.Where(s => s.Owner == PoolOwner.Free
                         && !game.Clubs.Any(c => c.Players.Contains(s.Player)) && !game.TransferMarket.FreeAgents.Contains(s.Player)).Take(2))
                aged.Player.Age = 45;
            var before = game.TransferMarket.Pool.ToDictionary(s => s.Index, s => s.Player);
            game.StartNextSeason();
            foreach (var slot in game.TransferMarket.Pool)
            {
                if (!before.TryGetValue(slot.Index, out var old) || ReferenceEquals(old, slot.Player) || !game.RetiredPlayers.Contains(old)
                    || game.Clubs.Any(c => c.Players.Contains(slot.Player)))
                    continue;
                renewals++;
                Assert.Equal((old.Name, old.Role), (slot.Player.Name, slot.Player.Role));
            }
            var saved = SaveGame.Save(game);
            var loaded = SaveGame.Load(saved);
            Assert.True(loaded.Success, loaded.Error);
            Assert.Equal(saved, SaveGame.Save(loaded.Game!));
        }
        Assert.True(renewals > 0, "kein Nachfolger im Pool");
        Assert.Equal(active ? 4 : 0, talents);
    }

    /// <summary>Grenze des alten Codes (TalentPoolAttempts).</summary>
    private const int TalentAttemptsBefore = 1_500;
}
