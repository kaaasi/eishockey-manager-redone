using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur <c>kader-transfer</c>, Runde 12, Teil 1 (U4, U5 aus <c>docs/analyse/re-nachtrag.md</c>, Abschnitt 1): Verfall offener
/// Angebote (Tagesablauf 0x07169, Datei 0x07626 und 0x0767A) und Laufbahnende der Computerspieler im Saisonende (0x2787A).
/// Vorher rot: Angebote verfielen nie (Verlängerungsangebot, Angebot eines Vereins, Gebot am Markt); Computerspieler hörten
/// nach der eigenen Kader-KI-Tabelle (33–37) auf und ersetzt wurden sie nur durch Vertragslose oder 17–19-jährige Talente.
/// </summary>
public class KaderTransferNachtraegeTests
{
    private const int MaxDays = 120;

    /// <summary>random(a, b) = a.</summary>
    private sealed class Lowest : Random
    {
        public override int Next(int minValue, int maxValue) => minValue;
        public override int Next(int maxValue) => 0;
        public override int Next() => 0;
        public override double NextDouble() => 0;
    }

    /// <summary>random(a, b) = b (obere Grenze eingeschlossen, wie bei <see cref="TransferRules.Random16"/>).</summary>
    private sealed class Highest : Random
    {
        public override int Next(int minValue, int maxValue) => maxValue - 1;
        public override int Next(int maxValue) => maxValue - 1;
        public override int Next() => int.MaxValue - 1;
        public override double NextDouble() => 0.999;
    }

    private static Club OwnClub(GameState game) => game.Clubs[game.ActiveManager.ClubIndex];

    /// <summary>
    /// [ORIGINAL] Datei 0x0767A–0x076E3: Ein offenes Verlängerungsangebot (+0x18 &gt; 99) verfällt mit random(0, 5) = 0 (1/6 je Tag);
    /// +0x18 = random(9, 17) (Verhandlungssperre in Tagen), die Meldung ist gelöscht.
    /// </summary>
    [Fact]
    public void OfferedExtension_Expires_WithLockOfNineToSeventeenDays()
    {
        var game = TestGames.Create();
        var p = OwnClub(game).Players.First(q => q.Role == PlayerRole.Forward);
        p.ContractYears = 1;
        p.Notice = ContractNotice.Offered;
        p.NoticeYears = 3;
        p.NegotiationLock = 0;

        var days = 0;
        while (p.Notice == ContractNotice.Offered && days++ < MaxDays)
            game.Continue();

        Assert.NotEqual(ContractNotice.Offered, p.Notice);
        Assert.Equal(0, p.NoticeYears);
        Assert.InRange(p.NegotiationLock, 9, 17);
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x07626–0x07673: Ein offenes Angebot eines Vereins (Kaderplatz +0x09 &gt; 0x1F, Bit 0x40) verfällt mit
    /// random(0, 3) = 0 (1/4 je Tag) samt Meldung.
    /// </summary>
    [Fact]
    public void UnsolicitedOffer_Expires()
    {
        var game = TestGames.Create();
        var p = OwnClub(game).Players.First(q => q.Role == PlayerRole.Forward);
        p.UnsolicitedOffer = game.Clubs.First(c => c.Index != game.ActiveManager.ClubIndex);

        var days = 0;
        while (p.UnsolicitedOffer is not null && days++ < MaxDays)
            game.Continue();

        Assert.Null(p.UnsolicitedOffer);
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x07215–0x0726A: dasselbe für die Marktplätze (Bit 0x80, Gebot eines Computervereins): random(0, 3) = 0.
    /// </summary>
    [Fact]
    public void MarketBid_Expires_WhileListingStays()
    {
        var game = TestGames.Create();
        var manager = game.ActiveManager;
        var player = OwnClub(game).Players.First(q => q.Role == PlayerRole.Forward);
        Assert.Equal(TransferResult.Accepted, game.TransferMarket.Offer(game, manager, player));
        var listing = game.TransferMarket.ListingOf(player)!;
        var random = new GameRandom(5);

        TransferBid? first = null;
        for (var day = 0; day < 4000 && (first is null || game.TransferMarket.Bids.Contains(first)); day++)
        {
            game.TransferMarket.AdvanceDay(game, random);
            first ??= game.TransferMarket.Bids.FirstOrDefault(b => b.Listing == listing);
        }

        Assert.NotNull(first);
        Assert.Contains(listing, game.TransferMarket.Listings);
        Assert.DoesNotContain(first, game.TransferMarket.Bids);
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x2787A: Laufbahnende bei Alter &gt; random(37, 42). Kleinster Wurf 37: ab 38; größter Wurf 42: ab 43.
    /// </summary>
    [Fact]
    public void CareerEnds_AfterRandomAgeBetween37And42()
    {
        Assert.False(SquadRules.CareerEnds(37, new Lowest()));
        Assert.True(SquadRules.CareerEnds(38, new Lowest()));
        Assert.False(SquadRules.CareerEnds(42, new Highest()));
        Assert.True(SquadRules.CareerEnds(43, new Highest()));
        Assert.False(SquadRules.CareerEnds(30, new Highest()));
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x2787A–0x279DA, Neuanlage im selben Pooleintrag: Alter random(18, 25), s = random(40, 92), Grundstärken
    /// je random(s − 5, s + 5). Mit dem kleinsten Wurf: 18 Jahre, s = 40, Kondition = Technik = 35.
    /// </summary>
    [Fact]
    public void CreateRenewal_FollowsOriginalRanges()
    {
        var game = TestGames.Create();
        var low = PlayerFactory.CreateRenewal(game, "NEU", PlayerRole.Defender, new Lowest());
        Assert.Equal(PlayerRole.Defender, low.Role);
        Assert.Equal(18, low.Age);
        Assert.Equal(35, low.Condition);
        Assert.Equal(35, low.Technique);
        Assert.Equal(0, low.Caps);
        var high = PlayerFactory.CreateRenewal(game, "NEU", PlayerRole.Forward, new Highest());
        Assert.Equal(25, high.Age);
        Assert.Equal(97, high.Condition); // s = 92, random(87, 97)
        Assert.Equal(97, high.Technique);
    }

    /// <summary>
    /// U5: Ein Computerspieler weit über der Grenze hört beim Saisonwechsel auf; ein Spieler derselben Art, 18–25 Jahre alt,
    /// übernimmt seinen Platz im selben Verein ([ORIGINAL] Neuanlage im selben Pooleintrag, Datei 0x279DA).
    /// </summary>
    [Fact]
    public void ComputerPlayerAtAge60_Retires_AndYoungSuccessorTakesThePlace()
    {
        var game = TestGames.Create();
        var club = game.Clubs.First(c => c.Index != game.ActiveManager.ClubIndex && c.Players.Count > 0);
        var old = club.Players.First(p => p.Role == PlayerRole.Defender);
        old.Age = 60;
        var defenders = club.Players.Count(p => p.Role == PlayerRole.Defender);
        var others = club.Players.Where(p => p != old).ToHashSet();

        SquadLifecycleTests.PlaySeason(game);
        game.StartNextSeason();

        Assert.Contains(old, game.RetiredPlayers);
        Assert.DoesNotContain(old, club.Players);
        Assert.Equal(defenders, club.Players.Count(p => p.Role == PlayerRole.Defender));
        Assert.Contains(club.Players, p => !others.Contains(p) && p.Role == PlayerRole.Defender && p.Age is >= 18 and <= 25);
    }

    /// <summary>
    /// U5 (Mehrsaisonprobe, vier Saisonwechsel, passiver und aktiver Manager): Computerspieler älter als 36 kommen vor (Grenze
    /// random(37, 42)), keiner ist älter als 42, ihr Ersatz im selben Verein ist 18–25 Jahre alt und die Kader bleiben besetzt.
    /// Vorher rot: Altersgrenze 33–37, nie ein Computerspieler über 36.
    /// </summary>
    [Theory]
    [InlineData(false, 3)]
    [InlineData(true, 3)]
    [InlineData(false, 11)]
    [InlineData(true, 11)]
    public void MultiSeason_FourChanges_ComputerPlayersRetireAfterAge37To42(bool active, int seed)
    {
        var game = TestGames.Create(seed);
        var managed = game.Managers.Select(m => m.ClubIndex).ToHashSet();
        var computer = game.Clubs.Where(c => c.Players.Count > 0 && !managed.Contains(c.Index)).ToArray();
        var olderThan36 = 0;
        // Startalter 19–31: In vier Wechseln erreicht keiner die Grenze. Sechs Jahre älter machen, damit sie greift.
        foreach (var p in computer.SelectMany(c => c.Players))
            p.Age += 6;

        for (var change = 1; change <= 4; change++)
        {
            SquadLifecycleTests.PlaySeason(game, active ? () => SquadLifecycleTests.ExtendOffers(game) : null);
            game.StartNextSeason();
            if (active)
                SquadLifecycleTests.SignFreeAgents(game);

            foreach (var club in computer)
            {
                olderThan36 += club.Players.Count(p => p.Age > 36);
                Assert.All(club.Players, p => Assert.InRange(p.Age, PlayerFactory.TalentMinAge, 42));
                foreach (var role in Enum.GetValues<PlayerRole>())
                    Assert.True(club.Players.Count(p => p.Role == role) >= SquadRules.MinimumOf(role));
            }
            Assert.All(game.TransferMarket.FreeAgents, p => Assert.InRange(p.Age, PlayerFactory.TalentMinAge, 42));
        }
        Assert.True(olderThan36 > 0, "Kein Computerspieler über 36 gesehen: Grenze weiter 33–37?");
    }
}
