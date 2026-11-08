using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur <c>transfer-vertraege-komplett</c>, Teil 4: Testlücken aus den Reviews R8W3I-10 (Ergebnis „unfair“ der
/// Vertragsverhandlung), R8W3I-16 (Umkehrung des Gegenbeispiels nach dem Saisonende) und SW-01 (Zugfolge der Werbebewertung
/// bei einem Spieler mit Angebot).
/// </summary>
public class TransferVertraegeKomplettTeil4Tests
{
    /// <summary>Zeichnet jeden Zufallszug als (min, max) auf und reicht ihn an einen festen Strom weiter.</summary>
    private sealed class Recorder(int seed) : Random
    {
        private readonly Random _inner = new(seed);
        public List<(int Min, int Max)> Calls { get; } = [];

        public override int Next(int minValue, int maxValue)
        {
            Calls.Add((minValue, maxValue));
            return _inner.Next(minValue, maxValue);
        }

        public override int Next(int maxValue) => Next(0, maxValue);

        public override int Next()
        {
            Calls.Add((0, int.MaxValue));
            return _inner.Next();
        }

        public override double NextDouble()
        {
            Calls.Add((-1, -1));
            return _inner.NextDouble();
        }
    }

    /// <summary>
    /// R8W3I-10: Negative Eingaben ergeben „Dieser Betrag ist nicht erlaubt (unfair) !“. Der Spieler bleibt unverändert, es
    /// gibt keine Sperre und keinen Zufallszug (die Prüfung steht vor Verfügbarkeit und Entscheidung).
    /// </summary>
    [Theory]
    [InlineData(2, -1)]
    [InlineData(-1, 2_000)]
    [InlineData(-1, -1)]
    public void Negotiate_NegativeInput_IsUnfairAndChangesNothing(int years, int salary)
    {
        var game = TestGames.Create();
        var manager = game.Managers[0];
        var p = game.Clubs[manager.ClubIndex].Players.First(x => Contracts.Availability(game, x) is null);
        (p.ContractYears, p.MonthlySalary, p.NegotiationLock) = (2, 4_000, 0);
        var state = game.ContractRandom.Capture();
        var money = manager.Money;

        Assert.Equal(NegotiationResult.Unfair, Contracts.Negotiate(game, manager, p, years, salary, p.Insured, demandFilled: false));

        Assert.Equal((2, 4_000, 0), (p.ContractYears, p.MonthlySalary, p.NegotiationLock));
        Assert.Equal(money, manager.Money);
        Assert.Equal(GameRandom.FromState(state).Next(), game.ContractRandom.Next()); // kein Zug verbraucht
        Assert.Equal(["Dieser Betrag ist", "nicht erlaubt (unfair) !"], Contracts.Message(NegotiationResult.Unfair, p));
    }

    /// <summary>
    /// R8W3I-16 (zweite Hälfte des Gegenbeispiels aus R8W3I-12): [ORIGINAL] 0x4A81B–0x4A9A1 schreibt nach dem Saisonende auf
    /// jedem Level Laufzeit und Forderung in den Kaderplatz. Gleiche Laufzeit zum alten Gehalt (3 Jahre, 2.300 DM) ist damit
    /// auch auf Level 5 nicht „gleich oder besser“ und wird nicht ohne Weiteres angenommen; der Vertrag bleibt.
    /// </summary>
    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    public void Negotiate_AfterSeason_SameYearsOldSalary_IsNotAccepted(int level)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Test", Head = 0, ClubIndex = 2, Level = level });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 3);
        var manager = game.Managers[0];
        var club = game.Clubs[manager.ClubIndex];
        LifecycleRegressionTests.FinishSeason(game);
        Assert.True(game.AfterSeasonEnd);
        var p = club.Players.First(x => x.Role == PlayerRole.Forward && Contracts.Availability(game, x) is null);
        p.ContractYears = 3;
        p.MonthlySalary = 2_300;
        if (level < 5) // Vorbedingung (auf Level 5 ist das Feld leer, die Forderung wird trotzdem geschrieben)
            Assert.True(Contracts.DemandAfterYears(game, manager, p, 3) > 2_300);

        Assert.NotEqual(NegotiationResult.Accepted, Contracts.Negotiate(game, manager, p, 3, 2_300, false, demandFilled: false));
        Assert.Equal((3, 2_300), (p.ContractYears, p.MonthlySalary));
    }

    /// <summary>
    /// SW-01: [ORIGINAL] 0x3F9B8–0x3FAA9 – erste Schleife bestimmt den Wert aller Spieler, die zweite bestimmt ihn je Spieler
    /// neu (0x3FA56) <b>vor</b> dessen Zug random(35, 55) (0x3FA76), danach random(20, 50). Ein Spieler mit Angebot zieht im
    /// Wert random(95, 100); ohne den Neuaufruf in der zweiten Schleife fehlte sein zweiter Zug und die Folge verschöbe sich.
    /// </summary>
    [Fact]
    public void SponsorRating_PlayerWithBid_DrawsValueAgainBeforeStarCheck()
    {
        var game = TestGames.Create();
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        var bidPlayer = club.Players.First(p => p.Role == PlayerRole.Forward);
        game.TransferMarket.Offer(game, manager, bidPlayer);
        for (var day = 0; day < 40 && game.TransferMarket.Bids.Count == 0; day++)
            game.TransferMarket.AdvanceDay(game, game.Random);
        Assert.Single(game.TransferMarket.Bids);
        // A380: Der Spieler auf der Liste verlässt den Kader; im Original trägt nur der Markt-Platz Bit 0x80. Der Fall
        // „Kaderplatz mit Gebot“ bleibt für Altstände (vor A380 noch im Kader) erreichbar und wird so nachgestellt.
        Assert.DoesNotContain(bidPlayer, club.Players);
        club.Players.Add(bidPlayer);
        var players = club.Players.ToList();

        // Züge des Spielerwerts je Spieler (nur der Spieler mit Angebot zieht).
        var perPlayer = players.Select(pl =>
        {
            var r = new Recorder(0);
            game.PlayerValueOf(pl, club, PlayerValueKind.Price, r, manager.Level);
            return r.Calls;
        }).ToList();
        Assert.Equal([(95, 101)], perPlayer[players.IndexOf(bidPlayer)]);
        Assert.Equal(1, perPlayer.Count(c => c.Count > 0));

        var expected = perPlayer.SelectMany(c => c).ToList();
        foreach (var calls in perPlayer)
        {
            expected.AddRange(calls);
            expected.Add((35, 56));
        }
        expected.Add((20, 51));

        var recorder = new Recorder(7);
        var rating = SponsorRating.Compute(game, manager, recorder);
        Assert.InRange(rating, 0, 100);
        Assert.Equal(expected, recorder.Calls);
    }
}
