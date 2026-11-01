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

    /// <summary>[APPROX] Vertragsloser Spieler; From ist nur noch der letzte Verein, die Ablöse ist 0.</summary>
    public bool IsFreeAgent { get; init; }

    /// <summary>Spieltag, an dem der Spieler auf den Markt kam.</summary>
    public int ListedDay { get; init; }

    /// <summary>
    /// Sperre nach einem abgelehnten Gebot (Kaderplatz +0x1A des Markt-Satzes): Bit 0x80 und Bit 1 &lt;&lt; Manager.
    /// [ORIGINAL] gesetzt Datei 0x472F4, geprüft beim Anklicken 0x47140 („Angebot wurde bereits abgelehnt.“), gelöst im
    /// Tagesablauf 0x071CC (<see cref="TransferMarket.ReleaseBidLocks"/>).
    /// </summary>
    public int RejectedBy { get; set; }

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

    /// <summary>„Angebot wurde bereits abgelehnt.“ – Sperre nach abgelehntem Gebot (DGROUP 0x1219/0x121D, Datei 0x47B01).</summary>
    AlreadyRejected,
}

/// <summary>
/// Transfermarkt: Kauf (K), Leihe (L) und Verkauf eigener Spieler.
/// </summary>
/// <remarks>
/// [ORIGINAL] Grenzen aus den Meldungen in em.exe: 22 Mann im Team, 3 eigene und 11 Spieler insgesamt auf dem Markt,
/// Leihgebühr = Kaufpreis / 3 (Datei 0x462EB). Preis, Annahme eines Gebots, Unfair-Grenze, Tageswurf und Höhe der Angebote
/// für eigene Spieler nach Ov 0x118 (<see cref="TransferRules"/>, <c>docs/transfer.md</c>). [APPROX] Wechsel der Liste je
/// Spieltag, Wahl des bietenden Vereins und die Antwort eines menschlichen Verkäufers.
/// Der Markt aktualisiert sich selbst (<see cref="CatchUp"/>), bis das Paket „saison“ ihn je Spieltag aufruft.
/// </remarks>
public sealed partial class TransferMarket
{
    public const int MaxSquad = 22;
    public const int MaxOwnListings = 3;
    public const int MaxListings = 11;

    /// <summary>Anzahl Spieler auf dem Markt zu Spielbeginn (Video: 6).</summary>
    public const int StartListings = 6;

    /// <summary>
    /// Größe des Pools vertragsloser Spieler nach dem Saisonwechsel. [APPROX] Das Original führt keinen solchen
    /// Pool sichtbar; die Grenze hält ihn klein, Überzählige (die ältesten) beenden ihre Laufbahn.
    /// </summary>
    public const int MaxFreeAgents = 60;

    /// <summary>Mindestbestand je Art im Kader, damit ein Verkauf möglich ist. [APPROX]</summary>
    public const int MinGoalkeepers = 2, MinDefenders = 4, MinForwards = 6;

    private readonly List<TransferListing> _listings = [];
    private readonly List<TransferBid> _bids = [];
    private readonly List<TransferListing> _freeAgents = [];

    /// <summary>Vertragslose Spieler, auch wenn die sichtbare Transferliste voll ist.</summary>
    public IEnumerable<Player> FreeAgents => _freeAgents.Select(l => l.Player);

    public IReadOnlyList<TransferListing> Listings => _listings;

    /// <summary>Offene Gebote für Spieler der Manager („ANGEBOT von …“).</summary>
    public IReadOnlyList<TransferBid> Bids => _bids;

    /// <summary>Letzter Spieltag, bis zu dem der Markt fortgeschrieben ist.</summary>
    public int UpdatedDay { get; private set; }

    public static bool IsNationalTeam(Club club) => club.Index is >= 52 and < 64; // ClubCategory.NationalTeam

    /// <summary>
    /// Kaufpreis in DM: Spielerwert 0x118:0x70 (Art 0, ohne Angebot) in Drittel-DM / 3, also ein Vielfaches von 1.000 DM.
    /// [ORIGINAL] Listenpreis = Kaderplatz +0x2C des Markt-Satzes 4, gesetzt mit dem Wert (Spielstart Datei 0x29EB4), angezeigt
    /// als „… DM (K)“ über 0x99F:0x2D8 (Betrag / 3, Datei 0x4628B); Gegenprobe an den Original-Spielständen und den sechs
    /// Videopreisen in <c>TransferOriginalTests</c>.
    /// </summary>
    public static long Price(Player p) => TransferRules.PriceThirds(p) / 3;

    /// <summary>Neue Saison: alte Listings und Gebote verwerfen, Markt aus den aktuellen Eigentümern neu bilden.</summary>
    public void StartSeason(GameState game)
    {
        // Neue Listing-Identitäten: Auch eine noch gehaltene Referenz aus der Vorsaison ist ungültig.
        var released = _freeAgents.ToArray();
        _freeAgents.Clear();
        foreach (var old in released)
            Release(old.From, old.Player);
        Initialize(game, game.Random);
    }

    internal void Release(Club from, Player player)
    {
        _freeAgents.Add(new TransferListing { Player = player, From = from, Price = 0, IsFreeAgent = true });
    }

    /// <summary>Vertragsloser Spieler verlässt den Pool (Verpflichtung durch einen Computerverein oder Laufbahnende).</summary>
    internal void Sign(Player player)
    {
        foreach (var listing in _freeAgents.Where(l => l.Player == player).ToList())
        {
            _freeAgents.Remove(listing);
            _listings.Remove(listing);
            _bids.RemoveAll(b => b.Listing == listing);
        }
    }

    /// <summary>Füllt den Markt zu Spielbeginn.</summary>
    public void Initialize(GameState game, Random random)
    {
        _listings.Clear();
        _bids.Clear();
        UpdatedDay = game.PlayedMatchDays;
        foreach (var listing in _freeAgents.Take(StartListings))
            _listings.Add(listing);
        for (var i = _listings.Count; i < StartListings; i++)
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
    /// dazu. Angebote für eigene Spieler auf dem Markt nach dem Original (<see cref="TransferRules.BidRoll"/>, Höhe
    /// <see cref="TransferRules.BidAmountThirds"/>); [APPROX] bietender Verein zufällig unter den Computervereinen der Ligen
    /// (Original 0x238:0x1838 nicht belegt), die Höhe wird schon beim Angebot statt beim Anklicken gewürfelt.
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
            var value = game.PlayerValueOf(listing.Player, listing.From, PlayerValueKind.Price, random);
            if (!TransferRules.BidRoll(value, random) || _bids.Any(b => b.Listing == listing))
                continue;
            var buyers = game.Clubs.Where(c => c.League is not null && c != listing.From && !IsManaged(game, c)).ToList();
            var buyer = buyers[random.Next(buyers.Count)];
            var withOffer = PlayerValue.Compute(PlayerValue.Inputs(listing.Player, listing.From, buyer), PlayerValueKind.Price,
                (a, b) => TransferRules.Random16(random, a, b));
            _bids.Add(new TransferBid(listing, buyer, TransferRules.BidAmountThirds(withOffer, random) / 3));
        }
    }

    private static bool IsManaged(GameState game, Club club) => game.Managers.Any(m => m.ClubIndex == club.Index);

    /// <summary>Setzt einen Spieler eines Computervereins oder Nationalteams auf den Markt.</summary>
    private void AddRandomListing(GameState game, Random random)
    {
        var free = _freeAgents.FirstOrDefault(l => !_listings.Contains(l));
        if (free is not null)
        {
            _listings.Add(free);
            return;
        }
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
    /// Ist der Spieler für diesen Manager nach einem abgelehnten Gebot gesperrt? [ORIGINAL] Datei 0x47140–0x4715E: Bit 0x80
    /// und das Bit des Managers in Kaderplatz +0x1A → „Angebot wurde bereits abgelehnt.“ (geprüft beim Anklicken).
    /// </summary>
    public static bool IsLocked(GameState game, Manager manager, TransferListing listing)
    {
        var bit = ManagerBit(game, manager);
        return (listing.RejectedBy & 0x80) != 0 && (listing.RejectedBy & bit) != 0;
    }

    private static int ManagerBit(GameState game, Manager manager)
    {
        var index = game.Managers.ToList().IndexOf(manager);
        return index is >= 0 and < 7 ? 1 << index : 0;
    }

    /// <summary>
    /// Tagesablauf (nur im Zug von Manager 0, Datei 0x0718F): Ist auf einem Markt-Platz allein Manager 0 gesperrt
    /// (+0x1A &amp; 0x7F = 1), hebt random(0, 2) = 0 seine Sperre auf; Bit 0x80 bleibt (0x071CC–0x07202). Sperren anderer
    /// Manager löst das Original nie (der Vergleich nutzt die Managernummer 0 des Zugs). Der Test 0x07209 (<c>test +0x1A, 0</c>)
    /// ist wirkungslos.
    /// </summary>
    public void ReleaseBidLocks(Random random)
    {
        foreach (var listing in _listings)
            if ((listing.RejectedBy & 0x7F) == 1 && TransferRules.Random16(random, 0, 2) == 0)
                listing.RejectedBy ^= 1;
    }

    /// <summary>
    /// Kauf (<paramref name="loan"/> = false) bzw. Leihe eines Spielers zum gebotenen Betrag. Vom Computermarkt nach
    /// <see cref="TransferRules.Accepts"/> (Listenpreis, bei Leihe / 3), abgelehnt → Sperre <see cref="TransferListing.RejectedBy"/>
    /// (0x472F4); wie im Original fällt die Annahme vor der Geldprüfung (0x472C8 vor 0x474F0). Von einem anderen Manager
    /// erst die Geldprüfung, dann die Unfair-Grenze <see cref="TransferRules.IsFair"/> auf dem frischen Wert (0x474F0 vor
    /// 0x47546, R8-C02). [APPROX] Antwort des verkaufenden Managers (Original fragt ihn, DGROUP 0xD98): ab dem
    /// geforderten Betrag sicher, darunter bis 85 % mit sinkender Wahrscheinlichkeit. Vertragslose Spieler (Ablöse 0)
    /// kommen immer.
    /// </summary>
    public TransferResult Buy(GameState game, Manager manager, TransferListing listing, long offer, bool loan)
    {
        var result = Bid(game, manager, listing, offer, loan, out var purchase);
        // Ohne Oberfläche: „Mach' ich!“ ohne Eingaben – der Vertrag des neuen Kaderplatzes steht (Datei 0x4AB66).
        if (purchase is not null && Sign(game, purchase, 0, 0, insured: false, demandFilled: false) != NegotiationResult.Accepted)
            return TransferResult.Impossible;
        return result;
    }

    /// <summary>
    /// Gebot auf einen Eintrag: alle Prüfungen und die Annahme wie <see cref="Buy"/>. Eine Leihe ist damit abgeschlossen; beim
    /// Kauf liefert die Annahme den noch offenen Vertrag (<paramref name="purchase"/>), der mit <see cref="Sign"/> verhandelt
    /// wird. [ORIGINAL] Datei 0x4781B–0x47874: erst nach der Verhandlung 0x4A1DB fließt Geld und wechselt der Spieler; scheitert
    /// sie (auch „Abbruch“), „Der Transfer findet nicht statt.“ (0x47A3E, DGROUP 0x1201). Die Leihe ruft keine Verhandlung
    /// (Bedingung +0x11 = 0 und Leihe = 0, Datei 0x47851–0x4785B).
    /// </summary>
    public TransferResult Bid(GameState game, Manager manager, TransferListing listing, long offer, bool loan,
        out TransferPurchase? purchase)
    {
        purchase = null;
        if (!game.Managers.Contains(manager) || !IsCurrentListing(game, listing)
            || listing.From.Index == manager.ClubIndex && !listing.IsFreeAgent
            || loan && listing.IsFreeAgent)
            return TransferResult.Impossible;
        var club = game.Clubs[manager.ClubIndex];
        if (offer < 0 || offer == 0 && !listing.IsFreeAgent)
            return TransferResult.Unfair;
        if (club.Players.Count >= MaxSquad)
            return TransferResult.SquadFull;
        if (!_listings.Contains(listing) || listing.Seller == manager)
            return TransferResult.Impossible;
        if (IsLocked(game, manager, listing))
            return TransferResult.AlreadyRejected;

        var asked = loan ? listing.LoanFee : listing.Price;
        if (asked < 0)
            return TransferResult.Impossible;
        if (listing.Seller is not null)
        {
            if (offer > manager.Money)
                return TransferResult.NotEnoughMoney;
            var value = game.PlayerValueOf(listing.Player, listing.From, PlayerValueKind.Price, game.Random);
            if (!TransferRules.IsFair(value, TransferRules.Thirds(offer), loan))
                return TransferResult.Unfair;
            var ratio = asked == 0 ? 1 : offer / (double)asked;
            if (ratio < 1 && (ratio <= 0.85 || game.Random.NextDouble() >= (ratio - 0.85) / 0.15))
                return TransferResult.Rejected;
        }
        else if (!listing.IsFreeAgent
                 && !TransferRules.Accepts(TransferRules.Thirds(listing.Price), TransferRules.Thirds(offer), loan, game.Random))
        {
            listing.RejectedBy |= 0x80 | ManagerBit(game, manager);
            return TransferResult.Rejected;
        }
        if (offer > manager.Money)
            return TransferResult.NotEnoughMoney;

        if (!loan)
        {
            purchase = new TransferPurchase(listing, manager, offer, Contracts.PurchaseYears,
                Contracts.ValueSalary(game, manager, listing.Player));
            return TransferResult.Accepted;
        }
        Complete(game, listing, manager, offer, loan: true);
        return TransferResult.Accepted;
    }

    private void Complete(GameState game, TransferListing listing, Manager manager, long offer, bool loan)
    {
        game.Book(manager, FinanceCategory.Transfers, -offer);
        if (listing.Seller is { } seller)
            game.Book(seller, FinanceCategory.Transfers, offer);
        Move(listing, game.Clubs[manager.ClubIndex], loan);
    }

    /// <summary>Spieler wechselt vom bisherigen Verein in den Kader des Käufers.</summary>
    private void Move(TransferListing listing, Club to, bool loan)
    {
        var p = listing.Player;
        var from = listing.From;
        _listings.Remove(listing);
        _bids.RemoveAll(b => b.Listing == listing);
        if (!listing.IsFreeAgent)
        {
            from.Players.Remove(p);
            from.Lineup.Remove(p);
            if (listing.Seller is null)
                from.Lineup.FillGaps(from.Players); // Computerverein stellt selbst um, Manager nicht
        }

        var agreement = loan ? new PlayerLoan(from, p.ContractYears, p.MonthlySalary, p.Insured) : null;
        _freeAgents.Remove(listing);
        PlayerFactory.Join(to, p, loan ? 1 : SquadSetup.StartContractYears, loan, agreement);
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

    /// <summary>
    /// Gebot annehmen: Spieler wechselt, der Manager erhält die Ablöse. Liefert <see cref="TransferResult.Impossible"/>,
    /// wenn das Gebot nicht (mehr) offen oder nicht ausführbar ist; dann ändert sich weder Geld noch Kader.
    /// </summary>
    public TransferResult Accept(GameState game, TransferBid bid)
    {
        // TransferBid ist ein record: Wertgleichheit reicht nicht als Nachweis eines offenen Gebots.
        if (!_bids.Any(open => ReferenceEquals(open, bid)))
            return TransferResult.Impossible;
        if (bid.Amount <= 0
            || bid.Listing.Seller is not { } seller || !IsCurrentListing(game, bid.Listing)
            || !game.Clubs.Contains(bid.Buyer) || bid.Buyer == bid.Listing.From
            || IsManaged(game, bid.Buyer) || bid.Buyer.Players.Count >= PlayerFactory.MaxNumber)
        {
            // Offenes, aber nicht mehr ausführbares Gebot (z. B. Mindestbesetzung nach einem anderen Verkauf):
            // verwerfen, damit die Ansicht es nicht kommentarlos erneut vorlegt. [APPROX] Verhalten des Originals offen.
            _bids.RemoveAll(open => ReferenceEquals(open, bid));
            return TransferResult.Impossible;
        }
        game.Book(seller, FinanceCategory.Transfers, bid.Amount);
        Move(bid.Listing, bid.Buyer, loan: false);
        return TransferResult.Accepted;
    }

    private bool IsCurrentListing(GameState game, TransferListing listing)
    {
        if (!_listings.Contains(listing) || !game.Clubs.Contains(listing.From)
            || listing.Player.OnLoan || listing.Player.Loan is not null)
            return false;
        if (listing.IsFreeAgent)
            return _freeAgents.Contains(listing) && !game.Clubs.Any(c => c.Players.Contains(listing.Player));
        return listing.From.Players.Contains(listing.Player) && CanLeave(listing.From, listing.Player)
            && game.Clubs.Where(c => c != listing.From).All(c => !c.Players.Contains(listing.Player))
            && (listing.Seller is null
                ? !IsManaged(game, listing.From)
                : game.Managers.Contains(listing.Seller) && listing.Seller.ClubIndex == listing.From.Index);
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
        TransferResult.AlreadyRejected => ["Angebot wurde", "bereits abgelehnt."],
        _ => ["Der Transfer", "findet nicht statt."],
    };
}

public sealed partial class GameState
{
    public TransferMarket TransferMarket { get; } = new();
}
