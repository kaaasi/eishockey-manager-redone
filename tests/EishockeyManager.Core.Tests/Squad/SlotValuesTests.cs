using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur kaderplatz-werte (Runde 22), Teil 1: Werte am Kaderplatz statt am Spieler (A545, A674, A690, A691, A714).
/// Fundstellen: <c>Squad.SlotValues.cs</c> (0x42CB1, Kauf 0x477E7–0x47907), <c>Transfer.PoolDistribution.cs</c> (Drift 0x054D9).
/// </summary>
public class SlotValuesTests
{
    private static GameRandom Copy(GameRandom random) => GameRandom.FromState(random.Capture());

    private static (GameState Game, Manager Manager, Club Club, TransferMarket Market, TransferListing Listing) MarketGame()
    {
        var game = TestGames.Create(4);
        var manager = game.ActiveManager;
        manager.Money = 100_000_000;
        var club = game.Clubs[manager.ClubIndex];
        var listing = game.TransferMarket.Listings.First(l => l.Seller is null && l.From != club && !l.IsFreeAgent
            && l.Player.Role != PlayerRole.Goalkeeper);
        return (game, manager, club, game.TransferMarket, listing);
    }

    /// <summary>
    /// A674/A545: Der Kauf vom Computermarkt legt einen neuen Kaderplatz an (Datei 0x477E7 → 0x42CB1): +0x19 = 0 (memset 0x42E3A),
    /// Belastung random(80, 120) und Tendenz 0x4907A neu – gezogen beim Annehmen vor der Verhandlung –, Strafen, Pokal- und
    /// Europapokalzähler 0, Liga-Tore/-Spiele aus dem Pool (+0x1B/+0x1C). Beleg FILE05 → FILE07 (DAFFNER: Markt +0x19 = 0xCA →
    /// 0). Vorher behielt der Spieler alle Werte des Markt-Platzes (rot: Über-/Unterzahl ≠ 0, Strafen und Pokaltore blieben).
    /// </summary>
    [Fact]
    public void Buy_FromComputerMarket_CreatesFreshSlot()
    {
        var (game, manager, club, market, listing) = MarketGame();
        var p = listing.Player;
        (p.HiddenSkill1, p.HiddenSkill2) = (9, 11);
        p.Penalties2 = 3;
        p.Penalties5 = 1;
        p.Games = 7;
        p.CupGames = 2;
        p.Goals = 5;
        p.CupGoals = 2;
        var marketTrend = (p.TrendScore, p.TrendControl);

        var twin = Copy(game.Random);
        Assert.True(TransferRules.Accepts(TransferRules.Thirds(listing.Price), TransferRules.Thirds(listing.Price * 2), false, twin));
        var expected = SquadSlotRules.Roll(twin); // nach der Annahme 0x4991D, vor der Verhandlung
        Assert.Equal(TransferResult.Accepted, market.Bid(game, manager, listing, listing.Price * 2, loan: false, out var purchase));
        Assert.Equal(expected, purchase!.NewSlot);
        Assert.Equal(NegotiationResult.Accepted, market.Sign(game, purchase, 0, 0, insured: false, demandFilled: false));

        Assert.Contains(p, club.Players);
        Assert.Equal((0, 0), (p.HiddenSkill1, p.HiddenSkill2));
        Assert.Equal(expected.Strain, p.Strain);
        Assert.Equal((expected.TrendScore, expected.TrendControl), (p.TrendScore, p.TrendControl));
        Assert.NotEqual(marketTrend, (p.TrendScore, p.TrendControl));
        Assert.Equal((0, 0), (p.Penalties2, p.Penalties5));
        Assert.Equal((5, 0, 3, 0), (p.Games, p.CupGames, p.Goals, p.CupGoals));
        Assert.Equal(0, p.GamesForClub);
    }

    /// <summary>
    /// A674: Die Leihe kopiert den Markt-Platz über den neuen (Datei 0x47907): Über-/Unterzahl und Belastung bleiben, verbraucht wird
    /// der Zufall von 0x42CB1 (Belastung, Tendenz, Leihgehalt random(90, 115)).
    /// </summary>
    [Fact]
    public void Loan_FromComputerMarket_KeepsMarketSlot_ConsumesSlotRandom()
    {
        var (game, manager, club, market, listing) = MarketGame();
        var p = listing.Player;
        (p.HiddenSkill1, p.HiddenSkill2) = (9, 11);
        var strain = p.Strain;
        var check = Copy(game.Random);

        Assert.Equal(TransferResult.Accepted, market.Bid(game, manager, listing, listing.LoanFee * 3, loan: true, out var purchase));
        Assert.Null(purchase);
        Assert.Contains(p, club.Players);
        Assert.Equal((9, 11, strain), (p.HiddenSkill1, p.HiddenSkill2, p.Strain));

        // Annahme der Leihe (TransferRules.Accepts) zieht vorher; danach 0x42CB1: Belastung, drei Tendenzwürfe, Leihgehalt.
        _ = TransferRules.Accepts(TransferRules.Thirds(listing.Price), TransferRules.Thirds(listing.LoanFee * 3), true, check);
        _ = SquadSlotRules.Roll(check);
        SquadSlotRules.ConsumeLoanSalary(check);
        _ = ComputerClubTransferRules.SponsorSubsidy(listing.LoanFee * 3, check); // Zuschuss 0x47A34 nach dem Abzug
        Assert.Equal(check.Next(), Copy(game.Random).Next());
    }

    /// <summary>
    /// A714: Die Drift 0x054D9 ändert den Poolsatz auch von Spielern mit Kaderplatz; der Kaderplatz behält seine Werte, der Export
    /// schreibt Pool +0x15/+0x16 aus den Poolwerten. Vorher blieben Kader- und Marktspieler ohne Drift (rot: PoolCondition null).
    /// </summary>
    [Fact]
    public void Drift_ChangesPoolValuesOfSquadPlayers_KeepsSlotValues()
    {
        var game = TestGames.Create(4);
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        var squad = club.Players.Select(p => (p, p.Condition, p.Technique, Pool: p.PoolStrengths)).ToList();
        var market = game.TransferMarket;

        _ = market.PoolQuotas(game, game.Random);

        Assert.All(squad, s => Assert.Equal((s.Condition, s.Technique), (s.p.Condition, s.p.Technique)));
        Assert.Contains(squad, s => s.p.PoolCondition is not null || s.p.PoolTechnique is not null);
        Assert.All(squad.Where(s => market.Pool.Any(slot => ReferenceEquals(slot.Player, s.p))), s =>
        {
            var (c, t) = s.p.PoolStrengths;
            Assert.InRange(c - s.Pool.Condition, -3, 3);
            Assert.InRange(t - s.Pool.Technique, -3, 3);
        });
    }

    /// <summary>
    /// A714/A674: Verlässt ein Spieler den Managerkader an einen Computerverein, entfällt der Kaderplatz (Datei 0x46F3C–0x46FCF):
    /// es gelten die Poolwerte. Vorher behielt er die Werte des Kaderplatzes.
    /// </summary>
    [Fact]
    public void Join_ComputerSquad_AdoptsPoolValues()
    {
        var game = TestGames.Create(4);
        var p = game.Clubs[game.ActiveManager.ClubIndex].Players.First(q => q.Role != PlayerRole.Goalkeeper);
        p.SetPoolStrengths(p.Condition - 4, p.Technique + 2, hasSlot: true);
        var (c, t) = p.PoolStrengths;
        var computer = game.Clubs.First(k => k.League == League.Bundesliga && !game.Managers.Any(m => m.ClubIndex == k.Index));
        game.Clubs[game.ActiveManager.ClubIndex].RemovePlayer(p);

        PlayerFactory.Join(computer, p, 2, loan: false, managerSquad: false);

        Assert.Equal((c, t), (p.Condition, p.Technique));
        Assert.Null(p.PoolCondition);
        Assert.Null(p.PoolTechnique);
    }

    /// <summary>A545: Liga-Tore und -Spiele eines neuen Kaderplatzes höchstens 255 (Pool +0x1B/+0x1C sind Bytes, Datei 0x542D5).</summary>
    [Fact]
    public void ApplyNewSlot_CapsLeagueCounters_GoalieWithoutGoalsAgainst()
    {
        var p = new Player { Name = "X", Role = PlayerRole.Forward, Games = 300, Goals = 280, CupGoals = 10 };
        p.ApplyNewSlot(new SlotStart(90, 50, 0x25), fromManager: false);
        Assert.Equal((255, 255, 0), (p.Games, p.Goals, p.CupGoals));
        Assert.Equal(90, p.Strain);

        var g = new Player { Name = "G", Role = PlayerRole.Goalkeeper, GoalsAgainst = 12, InjuredGames = 3, Penalties2 = 1 };
        g.ApplyNewSlot(new SlotStart(100, 40, 0x11), fromManager: true);
        Assert.Equal((0, 0), (g.GoalsAgainst, g.Goals));
        Assert.Equal((3, 1), (g.InjuredGames, g.Penalties2)); // vom Manager: Verletzung und Strafen kopiert (0x4787D–0x478CB)
    }
}
