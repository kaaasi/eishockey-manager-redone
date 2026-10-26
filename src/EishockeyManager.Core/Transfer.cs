using EishockeyManager.Formats;

namespace EishockeyManager.Core;

/// <summary>Ein Spieler auf der Transferliste.</summary>
public sealed class TransferListing
{
    public required Player Player { get; init; }

    /// <summary>Bisheriger Verein bzw. Nationalteam („vom TSV ERDING“, „aus FRANKREICH“).</summary>
    public required Club From { get; init; }

    /// <summary>Kaufpreis (Ablöse), Anzeige „231.000 DM (K)“.</summary>
    public required long Price { get; set; }

    /// <summary>Leihgebühr für eine Saison, Anzeige „(L)“. Im Video immer ein Drittel des Kaufpreises (abgerundet).</summary>
    public long LoanFee => Price / 3;

    /// <summary>Manager, der den Spieler auf den Markt gesetzt hat, oder null.</summary>
    public Manager? Seller { get; init; }

    /// <summary>Spieltag, an dem der Spieler auf den Markt kam.</summary>
    public int ListedDay { get; init; }

    /// <summary>Statuszeile: „vom TSV ERDING“ bzw. bei Nationalteams „aus FRANKREICH“.</summary>
    public string OriginText => (TransferMarket.IsNationalTeam(From) ? "aus " : "vom ") + From.Name;
}

/// <summary>Angebot eines Computervereins für einen Spieler, den ein Manager auf den Markt gesetzt hat.</summary>
public sealed record TransferBid(TransferListing Listing, Club Buyer, long Amount);

/// <summary>Ergebnis von Kauf, Leihe oder Verkauf (Meldungstexte aus em.exe).</summary>
public enum TransferResult
{
    /// <summary>„Ihr Angebot wurde angenommen !“</summary>
    Accepted,

    /// <summary>„Ihr Angebot wurde abgelehnt.“</summary>
    Rejected,

    /// <summary>„Sie haben nicht genug Geld !“</summary>
    NotEnoughMoney,

    /// <summary>„Schon 22 Mann im Team !“</summary>
    SquadFull,

    /// <summary>„Schon 3 Spieler auf dem Transfermarkt.“</summary>
    TooManyOwnListings,

    /// <summary>„Schon 11 Mann auf dem Transfermarkt.“</summary>
    MarketFull,

    /// <summary>„Dieser Betrag ist nicht erlaubt (unfair) !“</summary>
    Unfair,

    /// <summary>„Der Transfer findet nicht statt.“ – z. B. zu wenige Spieler einer Art im Kader.</summary>
    Impossible,
}

/// <summary>
/// Transfermarkt: Kauf (K), Leihe (L) und Verkauf eigener Spieler.
/// </summary>
/// <remarks>
/// [ORIGINAL] Grenzen aus den Meldungen in em.exe: 22 Mann im Team, 3 eigene und 11 Spieler insgesamt auf dem Markt,
/// Leihgebühr = Kaufpreis / 3 (Video). [APPROX] Preisformel (an den sechs Preisen im UI-Video kalibriert, ±8.000 DM),
/// Annahme von Angeboten, Wechsel der Liste je Spieltag und Gebote für eigene Spieler.
/// Der Markt aktualisiert sich selbst (<see cref="CatchUp"/>), bis das Paket „saison“ ihn je Spieltag aufruft.
/// </remarks>
public sealed class TransferMarket
{
    public const int MaxSquad = 22;
    public const int MaxOwnListings = 3;
    public const int MaxListings = 11;

    /// <summary>Anzahl Spieler auf dem Markt zu Spielbeginn (Video: 6).</summary>
    public const int StartListings = 6;

    /// <summary>Mindestbestand je Art im Kader, damit ein Verkauf möglich ist. [APPROX]</summary>
    public const int MinGoalkeepers = 2, MinDefenders = 4, MinForwards = 6;

    private readonly List<TransferListing> _listings = [];
    private readonly List<TransferBid> _bids = [];

    public IReadOnlyList<TransferListing> Listings => _listings;

    /// <summary>Offene Gebote für Spieler der Manager („ANGEBOT von …“).</summary>
    public IReadOnlyList<TransferBid> Bids => _bids;

    /// <summary>Letzter Spieltag, bis zu dem der Markt fortgeschrieben ist.</summary>
    public int UpdatedDay { get; private set; }

    public static bool IsNationalTeam(Club club) => club.Index is >= 52 and < 64; // ClubCategory.NationalTeam

    /// <summary>
    /// Kaufpreis. [APPROX] 5.200 DM je Stärkepunkt, 6.000 DM je Jahr unter 31, Torhüter +93.000 DM,
    /// auf 1.000 DM gerundet, mindestens 20.000 DM. Video: LAPORTE 76/79/47, 19 J. → 338.000 DM (Formel: 339.000).
    /// </summary>
    public static long Price(Player p)
    {
        var value = 5200.0 * (p.Condition + p.Technique + p.Form) / 3 + 6000.0 * (31 - p.Age) - 83000;
        if (p.Role == PlayerRole.Goalkeeper)
            value += 93000;
        return Math.Max(20_000, (long)Math.Round(value / 1000) * 1000);
    }

    /// <summary>Neue Saison: Der Spieltagszähler beginnt wieder bei 0.</summary>
    public void StartSeason() => UpdatedDay = 0;

    /// <summary>Füllt den Markt zu Spielbeginn.</summary>
    public void Initialize(GameState game, Random random)
    {
        _listings.Clear();
        _bids.Clear();
        UpdatedDay = game.PlayedMatchDays;
        for (var i = 0; i < StartListings; i++)
            AddRandomListing(game, random);
    }

    /// <summary>Schreibt den Markt bis zum aktuellen Spieltag fort (je Spieltag <see cref="AdvanceDay"/>).</summary>
    public void CatchUp(GameState game)
    {
        while (UpdatedDay < game.PlayedMatchDays)
        {
            UpdatedDay++;
            AdvanceDay(game, game.Random);
        }
    }

    /// <summary>
    /// Ein Spieltag: [APPROX] mit 30 % verschwindet ein fremder Spieler (anderswo verpflichtet), mit 50 % kommt einer
    /// dazu; für eigene Spieler bieten Computervereine mit 35 % je Spieltag 80–110 % des Preises.
    /// </summary>
    public void AdvanceDay(GameState game, Random random)
    {
        var foreign = _listings.Where(l => l.Seller is null).ToList();
        if (foreign.Count > 2 && random.NextDouble() < 0.3)
            _listings.Remove(foreign[random.Next(foreign.Count)]);
        if (_listings.Count < MaxListings && random.NextDouble() < 0.5)
            AddRandomListing(game, random);

        foreach (var listing in _listings.Where(l => l.Seller is not null).ToList())
        {
            if (_bids.Any(b => b.Listing == listing) || random.NextDouble() >= 0.35)
                continue;
            var buyers = game.Clubs.Where(c => c.League is not null && c != listing.From && !IsManaged(game, c)).ToList();
            var buyer = buyers[random.Next(buyers.Count)];
            var amount = (long)Math.Round(listing.Price * (0.8 + 0.3 * random.NextDouble()) / 1000) * 1000;
            _bids.Add(new TransferBid(listing, buyer, amount));
        }
    }

    private static bool IsManaged(GameState game, Club club) => game.Managers.Any(m => m.ClubIndex == club.Index);

    /// <summary>Setzt einen Spieler eines Computervereins oder Nationalteams auf den Markt.</summary>
    private void AddRandomListing(GameState game, Random random)
    {
        var sources = game.Clubs.Where(c => c.Players.Count > 18 && !IsManaged(game, c)).ToList();
        if (sources.Count == 0)
            return;
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var club = sources[random.Next(sources.Count)];
            var player = club.Players[random.Next(club.Players.Count)];
            if (_listings.Any(l => l.Player == player) || !CanLeave(club, player))
                continue;
            _listings.Add(new TransferListing
            {
                Player = player,
                From = club,
                Price = Price(player),
                ListedDay = game.PlayedMatchDays,
            });
            return;
        }
    }

    /// <summary>Bleiben nach dem Abgang genug Spieler der Art?</summary>
    public static bool CanLeave(Club club, Player player)
    {
        var minimum = player.Role switch
        {
            PlayerRole.Goalkeeper => MinGoalkeepers,
            PlayerRole.Defender => MinDefenders,
            _ => MinForwards,
        };
        return club.Players.Count(p => p.Role == player.Role) > minimum;
    }

    /// <summary>
    /// Kauf (<paramref name="loan"/> = false) bzw. Leihe eines Spielers zum gebotenen Betrag.
    /// [APPROX] Ab dem geforderten Betrag sicher, darunter bis 85 % mit sinkender Wahrscheinlichkeit.
    /// </summary>
    public TransferResult Buy(GameState game, Manager manager, TransferListing listing, long offer, bool loan)
    {
        var club = game.Clubs[manager.ClubIndex];
        if (offer < 1)
            return TransferResult.Unfair;
        if (club.Players.Count >= MaxSquad)
            return TransferResult.SquadFull;
        if (offer > manager.Money)
            return TransferResult.NotEnoughMoney;
        if (!_listings.Contains(listing) || listing.Seller == manager)
            return TransferResult.Impossible;

        var asked = loan ? listing.LoanFee : listing.Price;
        var ratio = offer / (double)asked;
        if (ratio < 1 && (ratio <= 0.85 || game.Random.NextDouble() >= (ratio - 0.85) / 0.15))
            return TransferResult.Rejected;

        game.Book(manager, FinanceCategory.Transfers, -offer);
        if (listing.Seller is { } seller)
            game.Book(seller, FinanceCategory.Transfers, offer);
        Move(listing, club, loan);
        return TransferResult.Accepted;
    }

    /// <summary>Spieler wechselt vom bisherigen Verein in den Kader des Käufers.</summary>
    private void Move(TransferListing listing, Club to, bool loan)
    {
        var p = listing.Player;
        var from = listing.From;
        _listings.Remove(listing);
        _bids.RemoveAll(b => b.Listing == listing);
        from.Players.Remove(p);
        from.Lineup.Remove(p);
        if (listing.Seller is null)
            from.Lineup.FillGaps(from.Players); // Computerverein stellt selbst um, Manager nicht

        p.Number = Enumerable.Range(1, 99).First(n => to.Players.All(q => q.Number != n));
        p.OnLoan = loan;
        p.Insured = false;
        p.RejectedOfferDay = null;
        p.ContractYears = loan ? 1 : SquadSetup.StartContractYears;
        p.MonthlySalary = Contracts.Demand(p);
        to.Players.Add(p);
    }

    /// <summary>Setzt einen eigenen Spieler auf den Markt („Verkaufen“).</summary>
    public TransferResult Offer(GameState game, Manager manager, Player player)
    {
        var club = game.Clubs[manager.ClubIndex];
        if (!club.Players.Contains(player) || player.OnLoan || !CanLeave(club, player))
            return TransferResult.Impossible;
        if (_listings.Any(l => l.Player == player))
            return TransferResult.Impossible;
        if (_listings.Count(l => l.Seller == manager) >= MaxOwnListings)
            return TransferResult.TooManyOwnListings;
        if (_listings.Count >= MaxListings)
            return TransferResult.MarketFull;
        _listings.Add(new TransferListing
        {
            Player = player,
            From = club,
            Price = Price(player),
            Seller = manager,
            ListedDay = game.PlayedMatchDays,
        });
        return TransferResult.Accepted;
    }

    /// <summary>Nimmt einen eigenen Spieler vom Markt („Behalten“).</summary>
    public void Withdraw(TransferListing listing)
    {
        _listings.Remove(listing);
        _bids.RemoveAll(b => b.Listing == listing);
    }

    /// <summary>Eigene Spieler auf dem Markt.</summary>
    public TransferListing? ListingOf(Player player) => _listings.FirstOrDefault(l => l.Player == player);

    /// <summary>Gebot annehmen: Spieler wechselt, der Manager erhält die Ablöse.</summary>
    public void Accept(GameState game, TransferBid bid)
    {
        if (bid.Listing.Seller is { } seller)
            game.Book(seller, FinanceCategory.Transfers, bid.Amount);
        Move(bid.Listing, bid.Buyer, loan: false);
    }

    /// <summary>Gebot ablehnen.</summary>
    public void Decline(TransferBid bid) => _bids.Remove(bid);

    /// <summary>Text der Meldung zum Ergebnis.</summary>
    public static string[] Message(TransferResult result) => result switch
    {
        TransferResult.Accepted => ["Ihr Angebot wurde", "angenommen !"],
        TransferResult.Rejected => ["Ihr Angebot", "wurde abgelehnt."],
        TransferResult.NotEnoughMoney => ["Sie haben nicht", "genug Geld !"],
        TransferResult.SquadFull => ["Schon 22 Mann", "im Team !"],
        TransferResult.TooManyOwnListings => ["Schon 3 Spieler auf", "dem Transfermarkt."],
        TransferResult.MarketFull => ["Schon 11 Mann auf", "dem Transfermarkt."],
        TransferResult.Unfair => ["Dieser Betrag ist", "nicht erlaubt (unfair) !"],
        _ => ["Der Transfer", "findet nicht statt."],
    };
}

public sealed partial class GameState
{
    public TransferMarket TransferMarket { get; } = new();
}
