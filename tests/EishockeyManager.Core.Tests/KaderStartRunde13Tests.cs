namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur <c>kader-start</c>, Runde 13, Teil 1: Nachträge aus Review #529 (R12K-01, #530).
/// </summary>
public class KaderStartRunde13Tests
{
    /// <summary>
    /// R12K-01 (#530), U5: Ein freier Poolplatz außerhalb jedes Kaders mit Alter 60 bekommt beim Saisonwechsel einen neuen
    /// Spieler derselben Art, 18–25 Jahre alt, im selben Platz; der alte beendet die Laufbahn. [ORIGINAL] Datei 0x2787A–0x279DA
    /// (<c>RenewAgedPoolPlayer</c>, Aufruf aus <c>StartPoolSeason</c>). Vorher ohne gezielten Test: Wurde der Aufruf entfernt,
    /// blieben alle Tests der Spur grün.
    /// </summary>
    [Theory]
    [InlineData(3)]
    [InlineData(11)]
    public void FreePoolSlotAtAge60_RetiresAndGetsYoungSuccessorInSameSlot(int seed)
    {
        var game = TestGames.Create(seed);
        bool InAnySquad(Player p) => game.Clubs.Any(c => c.Players.Contains(p)) || game.TransferMarket.FreeAgents.Contains(p);
        var slots = game.TransferMarket.Pool
            .Where(s => s.Owner == PoolOwner.Free && !InAnySquad(s.Player))
            .Take(10).ToList();
        Assert.Equal(10, slots.Count);
        var before = slots.ToDictionary(s => s, s => (Old: s.Player, s.Player.Role));
        foreach (var slot in slots)
            slot.Player.Age = 60;

        SquadLifecycleTests.PlaySeason(game);
        game.StartNextSeason();

        foreach (var slot in slots)
        {
            var (old, role) = before[slot];
            Assert.Contains(old, game.RetiredPlayers);
            Assert.NotSame(old, slot.Player);
            Assert.Equal(role, slot.Player.Role);
            Assert.InRange(slot.Player.Age, 18, 25);
            Assert.False(InAnySquad(slot.Player));
            Assert.Contains(slot, game.TransferMarket.Pool);
        }
    }

    /// <summary>
    /// R12K-03 (#532): <see cref="TestSeason.KeepSquad"/> verlängert standardmäßig nur angebotene Verträge; ein Spieler im
    /// letzten Vertragsjahr ohne Angebot bleibt bei einem Jahr (Kaderschwund passiver Proben sichtbar). Mit
    /// <c>extendExpired: true</c> bekommt er wie bisher einen Vertrag über zwei Jahre. Vorher rot: Standard war <c>true</c>.
    /// </summary>
    [Fact]
    public void KeepSquad_DefaultExtendsOnlyOfferedContracts()
    {
        var game = TestGames.Create();
        var manager = game.ActiveManager;
        var player = game.Clubs[manager.ClubIndex].Players.First(p => !p.OnLoan);
        player.ContractYears = 1;
        player.Notice = ContractNotice.None;
        player.NegotiationLock = 0;

        TestSeason.KeepSquad(game, manager);
        Assert.Equal(1, player.ContractYears);

        TestSeason.KeepSquad(game, manager, extendExpired: true);
        Assert.Equal(2, player.ContractYears); // Vertrag über zwei Jahre (Forderung steht im Feld)
    }
}
