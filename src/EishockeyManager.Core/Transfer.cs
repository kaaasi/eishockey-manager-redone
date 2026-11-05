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

    /// <summary>
    /// Spieler aus dem Spielerpool (<see cref="TransferMarket.Pool"/>), nicht aus einem Kader; <see cref="From"/> ist der
    /// Poolverein (+0x1D). [ORIGINAL] Listenwechsel 0x494EF (Spur transfer-vertraege-komplett, Teil 5).
    /// </summary>
    public bool FromPool { get; init; }

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

    /// <summary>„Schon 29 Mann / im Team.“ (DGROUP 0x127D)</summary>
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

    /// <summary>
    /// Der verkaufende Manager wird gefragt („Nehmen Sie das Angebot (…) für … an ?“, Datei 0x47655–0x47743); Antwort mit
    /// <see cref="TransferMarket.AnswerSeller"/>. Spur kader-transfer, Teil 4 (A258).
    /// </summary>
    SellerAsked,
}

/// <summary>
/// Transfermarkt: Kauf (K), Leihe (L) und Verkauf eigener Spieler.
/// </summary>
/// <remarks>
/// [ORIGINAL] Grenzen aus den Meldungen in em.exe: 29 Mann im Team (<see cref="MaxSquad"/>), 3 eigene und 11 Spieler insgesamt auf dem Markt,
/// Leihgebühr = Kaufpreis / 3 (Datei 0x462EB). Preis, Annahme eines Gebots, Unfair-Grenze, Tageswurf und Höhe der Angebote
/// für eigene Spieler nach Ov 0x118 (<see cref="TransferRules"/>, <c>docs/transfer.md</c>); Wechsel der Liste aus dem
/// Spielerpool nach 0x494EF (<see cref="ChangeList"/>), täglich (<see cref="CatchUp"/>, 0x54E71) mit bietendem Verein nach 0x059B8.
/// Ein verkaufender Manager wird gefragt (<see cref="AnswerSeller"/>, 0x47655–0x47743); [APPROX] ohne Oberfläche
/// (<see cref="Buy"/>) eine simulierte Antwort.
/// </remarks>
public sealed partial class TransferMarket
{
    /// <summary>
    /// Höchstzahl Spieler eines Managers für Kauf und Leihe. [ORIGINAL] em.exe Datei 0x477CD: der Kauf ruft „Kaderplatz
    /// anlegen“ (0x108:0x5C = Datei 0x42CB1) mit Grenze 0x1D; dort wird bei mehr als 28 Spielern des Managers (Zählung
    /// 0xF8:0x75) mit „Schon 29 Mann / im Team.“ (DGROUP 0x127D) abgebrochen. Beleg FILE05 → FILE07: Kölner EC kauft als
    /// 26. Spieler. Die frühere Grenze 22 stammte aus „Schon 22 Mann / im Team !“, der Meldung der Aufstellung
    /// (<see cref="Club.MaxDressed"/>). [APPROX] Das Remake zählt nur den eigenen Kader; ob 0xF8:0x75 verliehene Spieler
    /// mitzählt (Inventur F089), ist offen.
    /// </summary>
    public const int MaxSquad = 29;
    public const int MaxOwnListings = 3;
    public const int MaxListings = 11;

    /// <summary>
    /// Anzahl Spieler auf dem Markt zu Spielbeginn im Video (6). Seit Teil 5 ergibt sie sich aus dem Listenwechsel
    /// (<see cref="ChangeList"/>, im Mittel 6,5); die Konstante bleibt als Bezugswert.
    /// </summary>
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

    /// <summary>Gespielte Ligaspieltage beim letzten Tagesablauf des Markts (<see cref="CatchUp"/>).</summary>
    public int UpdatedDay { get; private set; }

    public static bool IsNationalTeam(Club club) => club.Index is >= 52 and < 64; // ClubCategory.NationalTeam

    /// <summary>
    /// Kaufpreis in DM: Spielerwert 0x118:0x70 (Art 0, ohne Angebot) in Drittel-DM / 3, also ein Vielfaches von 1.000 DM.
    /// [ORIGINAL] Listenpreis = Kaderplatz +0x2C des Markt-Satzes 4, gesetzt mit dem Wert (Spielstart Datei 0x29EB4), angezeigt
    /// als „… DM (K)“ über 0x99F:0x2D8 (Betrag / 3, Datei 0x4628B); Gegenprobe an den Original-Spielständen und den sechs
    /// Videopreisen in <c>TransferOriginalTests</c>.
    /// </summary>
    public static long Price(Player p) => TransferRules.PriceThirds(p) / 3;

    /// <summary>
    /// Neue Saison: alte Listings und Gebote verwerfen, Pool fortschreiben (<see cref="StartPoolSeason"/>), auf die Ligen umverteilen
    /// (<see cref="DistributePoolAtSeasonChange"/>) und die Liste
    /// wechseln (<see cref="ChangeList"/>, [ORIGINAL] Saisonstart 0x43A9F). [APPROX] Das Original behält die Markt-Plätze bis
    /// zum Wechsel; das Remake gibt sie vorher frei, weil Listing-Identitäten nicht in die neue Saison reichen.
    /// </summary>
    public void StartSeason(GameState game)
    {
        // Neue Listing-Identitäten: Auch eine noch gehaltene Referenz aus der Vorsaison ist ungültig.
        var released = _freeAgents.ToArray();
        _freeAgents.Clear();
        foreach (var old in released)
            Release(old.From, old.Player);
        _listings.Clear();
        _bids.Clear();
        foreach (var slot in _pool.Where(s => s.Owner == PoolOwner.Market))
            slot.Owner = PoolOwner.Free;
        UpdatedDay = game.PlayedMatchDays;
        // [APPROX] R204: Spieler, deren Vertrag in einem Managerverein endete, bleiben die Saison über verpflichtbar (höchstens
        // StartListings). Im Original werden sie freie Poolspieler (Besitzer 6) und kommen nur über den Listenwechsel zurück.
        foreach (var free in _freeAgents.Where(l => IsManaged(game, l.From)).Take(StartListings))
            _listings.Add(free);
        StartPoolSeason(game, game.Random);
        // [ORIGINAL] 0x07C7E nach den neuen Namen (Spur kader-transfer, Teil 5); Teil des Tageslaufs 0x54C9F, den es im Modus
        // „nur Weltkugel“ nicht gibt (0x54CEE).
        if (!game.WorldOnly)
            DistributePoolAtSeasonChange(game, game.Random);
        ChangeList(game, game.Random);
    }

    internal void Release(Club from, Player player)
    {
        player.UnsolicitedOffer = null; // Kaderplatz entfällt samt +0x09 Bit 0x40
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

    /// <summary>
    /// Füllt den Markt zu Spielbeginn: Pool (schon vor den Kadern gebaut, <see cref="ReservePool"/>), Umverteilung auf die Ligen
    /// (<see cref="DistributePoolAtStart"/>), dann der Listenwechsel des ersten Saisonstarts
    /// (<see cref="ChangeList"/>; im Mittel 6–7 Spieler, Video 6, FILE05 7).
    /// </summary>
    public void Initialize(GameState game, Random random)
    {
        _listings.Clear();
        _bids.Clear();
        UpdatedDay = game.PlayedMatchDays;
        if (_pool.Count == 0)
            BuildPool(game, random, TransferPoolLayout.Original, new HashSet<string>());
        DistributePoolAtStart(game, random); // [ORIGINAL] 0x057E3 vor dem ersten Listenwechsel (Spur kader-transfer, Teil 5)
        ChangeList(game, random);
    }

    /// <summary>
    /// Tagesablauf des Markts, gerufen einmal je Kalendertag aus dem Tagesablauf der Zeitleiste (<see cref="AdvanceDay"/>).
    /// [ORIGINAL] Der Listenwechsel (Datei 0x54E65–0x54E7C) und der Angebotswurf für eigene Spieler auf dem Markt (Tagesablauf
    /// 0x07169, Datei 0x072DD) fallen täglich, nicht je Ligaspieltag (Spur <c>kader-transfer</c>, Teil 4, A256).
    /// <see cref="UpdatedDay"/> hält wie bisher die gespielten Ligaspieltage beim letzten Tagesablauf.
    /// </summary>
    public void CatchUp(GameState game)
    {
        UpdatedDay = game.PlayedMatchDays;
        AdvanceDay(game, game.Random);
    }

    /// <summary>
    /// Ein Tag: Listenwechsel mit <see cref="ListChangeRoll"/> ([ORIGINAL] 0x54E71). Angebote für eigene Spieler auf dem Markt
    /// nach dem Original (<see cref="TransferRules.BidRoll"/>, Datei 0x072DD); bietender Verein nach 0x059B8
    /// (<see cref="UnsolicitedOfferRules.Bidder"/> mit Europapokal-Schalter 0, Aufruf 0x07364; A257). [APPROX] Die Höhe
    /// (<see cref="TransferRules.BidAmountThirds"/>) wird schon beim Angebot statt beim Anklicken (0x47347) gewürfelt; Bieter
    /// mit der Stärke des Spielers wie bei <see cref="UnsolicitedOfferRules.Bidder"/>.
    /// </summary>
    public void AdvanceDay(GameState game, Random random)
    {
        if (ListChangeRoll(game.Managers.Count, random))
            ChangeList(game, random);

        // [ORIGINAL] Verfall offener Gebote an den Marktplätzen (Datei 0x07215–0x0726A): random(0, 3) = 0 löscht Bit 0x80 samt
        // Meldung (Spur kader-transfer, Runde 12, U4); der Wurf fällt nur bei einem offenen Gebot.
        foreach (var bid in _bids.ToList())
            if (TransferRules.Random16(random, 0, 3) == 0)
                _bids.Remove(bid);

        foreach (var listing in _listings.Where(l => l.Seller is not null).ToList())
        {
            var value = game.PlayerValueOf(listing.Player, listing.From, PlayerValueKind.Price, random);
            if (!TransferRules.BidRoll(value, random) || _bids.Any(b => b.Listing == listing))
                continue;
            var index = UnsolicitedOfferRules.Bidder(listing.Player.Strength, foreign: false, game.BidderStrength,
                random);
            if (index < 0)
                continue;
            var buyer = game.Clubs.First(c => c.Index == index);
            var withOffer = PlayerValue.Compute(PlayerValue.Inputs(listing.Player, listing.From, buyer), PlayerValueKind.Price,
                (a, b) => TransferRules.Random16(random, a, b));
            _bids.Add(new TransferBid(listing, buyer, TransferRules.BidAmountThirds(withOffer, random) / 3));
        }
    }

    private static bool IsManaged(GameState game, Club club) => game.Managers.Any(m => m.ClubIndex == club.Index);

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
    /// 0x47546, R8-C02). [APPROX] Ohne Oberfläche ersetzt eine simulierte Antwort die Frage an den verkaufenden Manager
    /// (Original 0x47655–0x47743, im Remake <see cref="AnswerSeller"/>): ab dem geforderten Betrag sicher, darunter bis 85 %
    /// mit sinkender Wahrscheinlichkeit; abgelehnt sperrt wie „Nein !“. Vertragslose Spieler (Ablöse 0) kommen immer.
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
        out TransferPurchase? purchase) => Bid(game, manager, listing, offer, loan, out purchase, out _);

    /// <summary>
    /// Wie <see cref="Bid(GameState, Manager, TransferListing, long, bool, out TransferPurchase?)"/>; nach einer Leihe vom
    /// Computermarkt liefert <paramref name="subsidy"/> den gewürfelten Sponsorzuschuss (Datei 0x47A34), den die Oberfläche
    /// erfragt (<see cref="GameState.AcceptSponsorSubsidy"/>).
    /// </summary>
    public TransferResult Bid(GameState game, Manager manager, TransferListing listing, long offer, bool loan,
        out TransferPurchase? purchase, out SponsorSubsidyOffer? subsidy) =>
        Bid(game, manager, listing, offer, loan, askSeller: false, out purchase, out subsidy, out _);

    /// <summary>
    /// Gebot aus der Oberfläche: wie <see cref="Bid(GameState, Manager, TransferListing, long, bool, out TransferPurchase?, out SponsorSubsidyOffer?)"/>,
    /// aber bei einem verkaufenden Manager <see cref="TransferResult.SellerAsked"/> mit der Frage <paramref name="question"/>
    /// statt der simulierten Antwort (Datei 0x47655–0x47743, A258).
    /// </summary>
    public TransferResult Bid(GameState game, Manager manager, TransferListing listing, long offer, bool loan,
        out TransferPurchase? purchase, out SponsorSubsidyOffer? subsidy, out SellerQuestion? question) =>
        Bid(game, manager, listing, offer, loan, askSeller: true, out purchase, out subsidy, out question);

    private TransferResult Bid(GameState game, Manager manager, TransferListing listing, long offer, bool loan, bool askSeller,
        out TransferPurchase? purchase, out SponsorSubsidyOffer? subsidy, out SellerQuestion? question)
    {
        purchase = null;
        subsidy = null;
        question = null;
        if (!game.Managers.Contains(manager) || !IsCurrentListing(game, listing)
            || listing.From.Index == manager.ClubIndex && !listing.IsFreeAgent && !listing.FromPool
            || loan && listing.IsFreeAgent
            || loan && listing.FromPool && listing.From.Index == manager.ClubIndex)
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
            if (askSeller)
            {
                question = new SellerQuestion(listing, manager, listing.Seller, offer, loan);
                return TransferResult.SellerAsked;
            }
            var ratio = asked == 0 ? 1 : offer / (double)asked;
            if (ratio < 1 && (ratio <= 0.85 || game.Random.NextDouble() >= (ratio - 0.85) / 0.15))
            {
                Lock(game, listing, manager);
                return TransferResult.Rejected;
            }
        }
        else if (!listing.IsFreeAgent
                 && !TransferRules.Accepts(TransferRules.Thirds(listing.Price), TransferRules.Thirds(offer), loan, game.Random))
        {
            Lock(game, listing, manager);
            return TransferResult.Rejected;
        }
        if (offer > manager.Money)
            return TransferResult.NotEnoughMoney;
        return Close(game, manager, listing, offer, loan, out purchase, out subsidy);
    }

    /// <summary>[ORIGINAL] Datei 0x47AC8–0x47ADD: abgelehntes Gebot sperrt den Markt-Platz für den Bieter (+0x1A |= 0x80 | Bit).</summary>
    private static void Lock(GameState game, TransferListing listing, Manager manager) =>
        listing.RejectedBy |= 0x80 | ManagerBit(game, manager);

    /// <summary>Angenommenes Gebot: Kauf mit offenem Vertrag (<paramref name="purchase"/>) bzw. Leihe abgeschlossen.</summary>
    private TransferResult Close(GameState game, Manager manager, TransferListing listing, long offer, bool loan,
        out TransferPurchase? purchase, out SponsorSubsidyOffer? subsidy)
    {
        purchase = null;
        subsidy = null;
        if (!loan)
        {
            purchase = new TransferPurchase(listing, manager, offer, Contracts.PurchaseYears,
                Contracts.ValueSalary(game, manager, listing.Player));
            return TransferResult.Accepted;
        }
        subsidy = Complete(game, listing, manager, offer, loan: true);
        return TransferResult.Accepted;
    }

    private SponsorSubsidyOffer? Complete(GameState game, TransferListing listing, Manager manager, long offer, bool loan)
    {
        game.Book(manager, FinanceCategory.Transfers, -offer);
        if (listing.Seller is { } seller)
            game.Book(seller, FinanceCategory.Transfers, offer);
        Move(listing, game.Clubs[manager.ClubIndex], loan, managerSquad: true);
        // [ORIGINAL] Datei 0x4781B–0x47833: +0x2C des neuen Kaderplatzes = Gebot (Kauf und Leihe), Bit 31 bleibt.
        listing.Player.PurchasePrice = offer;
        // [ORIGINAL] Datei 0x47A18–0x47A34: nach dem Abzug Frage nach dem Sponsorzuschuss (nur Computermarkt).
        return RollSponsorSubsidy(game, listing, manager, offer);
    }

    /// <summary>Spieler wechselt vom bisherigen Verein in den Kader des Käufers.</summary>
    private void Move(TransferListing listing, Club to, bool loan, bool managerSquad)
    {
        var p = listing.Player;
        var from = listing.From;
        _listings.Remove(listing);
        _bids.RemoveAll(b => b.Listing == listing);
        if (listing.FromPool)
        {
            if (SlotOf(p) is { } slot)
                slot.Owner = PoolOwner.Taken;
        }
        else if (!listing.IsFreeAgent)
        {
            from.RemovePlayer(p);
            if (listing.Seller is null)
                from.Lineup.FillGaps(from.Players); // Computerverein stellt selbst um, Manager nicht
        }

        p.UnsolicitedOffer = null; // Kaderplatz entfällt samt +0x09 Bit 0x40
        var agreement = loan ? new PlayerLoan(from, p.ContractYears, p.MonthlySalary, p.Insured) : null;
        _freeAgents.Remove(listing);
        PlayerFactory.Join(to, p, loan ? 1 : SquadSetup.StartContractYears, loan, agreement, managerSquad);
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
    /// Gutschrift beim Annehmen eines Gebots für einen eigenen Spieler auf dem Markt (Kaderplatz +0x09 Bit 0x80).
    /// [ORIGINAL] Datei 0x49332–0x49371 (Angebotsfenster 0x491F9 mit Argument 0, Aufruf 0x473FF), danach 0x4748A: Angebot −
    /// Angebot / 7 · Restlaufzeit (+0x0B) in Drittel-DM (<see cref="UnsolicitedOfferRules.MarketTransferFeeThirds"/>); das
    /// Fenster zeigt diesen Rest als „ABLÖSESUMME“. Entscheidung Nutzer 01.11.2026.
    /// </summary>
    public static long TransferFee(TransferBid bid) =>
        UnsolicitedOfferRules.MarketTransferFeeThirds(TransferRules.Thirds(bid.Amount), bid.Listing.Player.ContractYears) / 3;

    /// <summary>
    /// Gebot annehmen: Spieler wechselt, der Manager erhält die Ablöse (<see cref="TransferFee"/>). Liefert <see cref="TransferResult.Impossible"/>,
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
        // [ORIGINAL] Datei 0x47483: Teamwerte des bietenden Vereins steigen (0x58DC), vor der Gutschrift 0x4748A.
        game.StrengthenBuyer(bid.Buyer, bid.Listing.Player);
        game.Book(seller, FinanceCategory.Transfers, TransferFee(bid));
        Move(bid.Listing, bid.Buyer, loan: false, managerSquad: false); // Bieter ist nie ein Managerverein (oben)
        return TransferResult.Accepted;
    }

    private bool IsCurrentListing(GameState game, TransferListing listing)
    {
        if (!_listings.Contains(listing) || !game.Clubs.Contains(listing.From)
            || listing.Player.OnLoan || listing.Player.Loan is not null)
            return false;
        if (listing.IsFreeAgent)
            return _freeAgents.Contains(listing) && !game.Clubs.Any(c => c.Players.Contains(listing.Player));
        if (listing.FromPool)
            return SlotOf(listing.Player) is { Owner: PoolOwner.Market } && !game.Clubs.Any(c => c.Players.Contains(listing.Player));
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
        TransferResult.SquadFull => ["Schon 29 Mann", "im Team."],
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
