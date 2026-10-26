namespace EishockeyManager.Core;

/// <summary>Werbeflächen im Bildschirm Werbung (kb\151).</summary>
public enum AdSlotKind
{
    /// <summary>Sieben Bandenfelder oben.</summary>
    Bande,

    /// <summary>Werbung auf dem Eis (mittleres Feld, zwei Tafeln).</summary>
    Eisflaeche,

    /// <summary>Trikotwerbung (Spieler links).</summary>
    Trikot,

    /// <summary>Fernsehvertrag (Fernseher links unten).</summary>
    Fernsehen,
}

/// <summary>Werbefläche: Art und – bei Banden – Feld 0–6.</summary>
public readonly record struct AdSlot(AdSlotKind Kind, int Index = 0)
{
    public override string ToString() => Kind == AdSlotKind.Bande ? $"Bande {Index + 1}" : Kind.ToString();
}

/// <summary>Werbepartner: zehn Firmen (Bande, Eis, Trikot) und fünf Fernsehsender. Texte aus em.exe.</summary>
public sealed record Sponsor(string Name, bool IsTelevision, string InfoTitle, string[] Info)
{
    public static readonly Sponsor[] Companies =
    [
        new("JWE Service GmbH", false, "J.W.E. Service GmbH",
            ["Telekommunikation vom Fachbetrieb.", "Telefone, Anrufbeantworter, Telefax", "Beratung - Verkauf - Installation", "Telefon/Fax: 0511/602094"]),
        // [APPROX] Aufteilung Titel/Zeilen bei CCH und KRON nicht im Video zu sehen; nach dem Muster von JWE und Herrenhäuser.
        new("CCH", false, "Computer Center Holstein",
            ["Ihr Lieferant fuer PCs und PC Zubehoer", "Max-Planck-Str. 9", "2420 Eutin", "Tel.: 04521-74300"]),
        new("Korona Soft", false, "", ["Der Computerspiele Blitzversand", "Tel.: 05241-1828"]),
        new("PC Games", false, "",
            ["Jeden Monat neu! Spieletests, Insiderberichte und alles", "rund um MS-DOS kompatible Rachnersysteme!", "Jeden Monat neu mit randvoller Coverdisk an Ihrem Kiosk!"]),
        new("Amiga Games", false, "",
            ["Jeden Monat neu! Spieletests, Insiderberichte und alles", "rund um den Amiga! Jeden Monat neu mit randvoller", "Coverdisk an Ihrem Kiosk!"]),
        new("Rushware", false, "", ["Deutschlands Top Computer- und Videospiel Vertrieb", "Rushware - Online with the trend"]),
        new("Herrenhäuser", false, "Herrenhäuser Brauerei Hannover",
            ["Gegr. 1868. Bedeutendste Privatbrauerei Norddeutschlands.", "Seit vielen Generationen in Privatbesitz.", "Produkte: Pils und Diplomat.", "Das Pils, das noch 90 Tage reift."]),
        new("Play Time", false, "", ["Monatlich erscheinende Fachzeitschrift für alle", "Computer- und Videospielsysteme"]),
        new("Software 2000", false, "", ["Publisher für anspruchsvolle Software", "Hotline : 04521-800444", "Mailbox : 04521-800447"]),
        new("KRON Soft", false, "KrOn Software",
            ["Gegr. 1988. Bescheidendstes Programmierteam Deutschlands.", "Besteht aus: Werner Krahe und Jens Onnen.", "Würden uns freuen, wenn Sie dieses", "Spielchen ORIGINAL (legal) erworben haben..."]),
    ];

    public static readonly Sponsor[] Stations =
    [
        new("ARB", true, "", []),
        new("4 SAT", true, "", []),
        new("SET 1", true, "", []),
        new("LTR minus", true, "", []),
        new("premium", true, "", []),
    ];

    /// <summary>Mögliche Partner einer Werbefläche (Fernsehen: Sender, sonst Firmen).</summary>
    public static Sponsor[] For(AdSlotKind kind) => kind == AdSlotKind.Fernsehen ? Stations : Companies;
}

/// <summary>Angebot eines Partners für eine Art von Werbefläche.</summary>
public sealed record SponsorOffer(AdSlotKind Kind, int Sponsor, int Months, int Boards, long MonthlyAmount);

/// <summary>Laufender Werbevertrag.</summary>
public sealed class SponsorContract
{
    public required AdSlotKind Kind { get; init; }
    public required int Sponsor { get; init; }

    /// <summary>Belegte Bandenfelder (nur bei <see cref="AdSlotKind.Bande"/>).</summary>
    public required int[] Boards { get; init; }

    public required long MonthlyAmount { get; init; }
    public int MonthsLeft { get; set; }

    public Sponsor Partner => Core.Sponsor.For(Kind)[Sponsor];

    public bool Covers(AdSlot slot) => slot.Kind == Kind && (Kind != AdSlotKind.Bande || Boards.Contains(slot.Index));
}

/// <summary>Werbung eines Managers: Verträge und aktuelle Angebote.</summary>
public sealed class Sponsoring
{
    public const int BoardCount = 7;

    public List<SponsorContract> Contracts { get; } = [];

    /// <summary>Angebote je Art der Werbefläche, Schlüssel = Partnerindex.</summary>
    public Dictionary<AdSlotKind, Dictionary<int, SponsorOffer>> Offers { get; } = [];

    public long MonthlyIncome => Contracts.Sum(c => c.MonthlyAmount);

    public SponsorContract? ContractAt(AdSlot slot) => Contracts.FirstOrDefault(c => c.Covers(slot));

    public SponsorOffer? OfferFrom(AdSlotKind kind, int sponsor) =>
        Offers.TryGetValue(kind, out var byKind) && byKind.TryGetValue(sponsor, out var offer) ? offer : null;

    public IEnumerable<int> FreeBoards() =>
        Enumerable.Range(0, BoardCount).Where(b => ContractAt(new AdSlot(AdSlotKind.Bande, b)) is null);
}

/// <summary>
/// Größenordnungen der Werbung. [APPROX] Formeln des Originals unbekannt; kalibriert auf das UI-Video (Oberliga,
/// Spielbeginn): Bande 3.454 und 5.464 DM für 12 Monate, Fernsehen 16.503 DM für 24 Monate, drei Verträge
/// (zwei Banden, Trikot) zusammen 18.098 DM, Fernsehvertrag ARB 19.147 DM pro Monat.
/// </summary>
public static class SponsorRules
{
    public static double LeagueFactor(League? league) => league switch
    {
        League.Bundesliga => 4.0,
        League.ZweiteLiga => 2.0,
        _ => 1.0,
    };

    /// <summary>Spanne des Monatsbetrags je Fläche in der Oberliga.</summary>
    public static (int Min, int Max) Range(AdSlotKind kind) => kind switch
    {
        AdSlotKind.Bande => (3_000, 6_000),
        AdSlotKind.Eisflaeche => (6_000, 10_000),
        AdSlotKind.Trikot => (7_000, 11_000),
        _ => (14_000, 20_000),
    };

    /// <summary>Wahrscheinlichkeit, dass ein freier Partner in einem Monat ein Angebot macht.</summary>
    public const double OfferChance = 0.4;

    public static readonly int[] Durations = [6, 12, 12, 24];
}

public sealed partial class ManagerFinance
{
    public Sponsoring Sponsoring { get; } = new();
}

public sealed partial class GameState
{
    /// <summary>Werbung eines Managers; Angebote entstehen beim Spielstart und Monatswechsel.</summary>
    public Sponsoring Ads(Manager manager) => Finance(manager).Sponsoring;

    /// <summary>Würfelt die Angebote neu aus (monatlich). Partner mit laufendem Vertrag dieser Art bieten nichts an.</summary>
    public void RenewOffers(Manager manager)
    {
        var sponsoring = Finance(manager).Sponsoring;
        var factor = SponsorRules.LeagueFactor(Clubs[manager.ClubIndex].League);
        foreach (var kind in Enum.GetValues<AdSlotKind>())
        {
            var offers = new Dictionary<int, SponsorOffer>();
            var partners = Sponsor.For(kind);
            for (var p = 0; p < partners.Length; p++)
            {
                if (FinanceRandom.NextDouble() >= SponsorRules.OfferChance)
                    continue;
                if (sponsoring.Contracts.Any(c => c.Kind == kind && c.Sponsor == p))
                    continue;
                var (min, max) = SponsorRules.Range(kind);
                var boards = kind == AdSlotKind.Bande && FinanceRandom.NextDouble() < 0.25 ? 2 : 1;
                var months = SponsorRules.Durations[FinanceRandom.Next(SponsorRules.Durations.Length)];
                var perBoard = min + FinanceRandom.NextDouble() * (max - min);
                offers[p] = new SponsorOffer(kind, p, months, boards, (long)Math.Round(perBoard * boards * factor));
            }
            sponsoring.Offers[kind] = offers;
        }
    }

    /// <summary>
    /// Nimmt ein Angebot für eine Werbefläche an. Bei Banden mit mehreren Feldern werden das gewählte und die nächsten
    /// freien Felder belegt. Liefert den Vertrag oder null, wenn die Fläche belegt ist oder nicht genug Felder frei sind.
    /// </summary>
    public SponsorContract? AcceptOffer(Manager manager, AdSlot slot, int sponsor)
    {
        var sponsoring = Ads(manager);
        var offer = sponsoring.OfferFrom(slot.Kind, sponsor);
        if (offer is null || sponsoring.ContractAt(slot) is not null)
            return null;

        int[] boards = [];
        if (slot.Kind == AdSlotKind.Bande)
        {
            var free = sponsoring.FreeBoards().ToList();
            var ordered = free.Where(b => b >= slot.Index).Concat(free.Where(b => b < slot.Index)).Take(offer.Boards).ToArray();
            if (ordered.Length < offer.Boards)
                return null;
            boards = ordered;
        }

        var contract = new SponsorContract
        {
            Kind = slot.Kind,
            Sponsor = sponsor,
            Boards = boards,
            MonthlyAmount = offer.MonthlyAmount,
            MonthsLeft = offer.Months,
        };
        sponsoring.Contracts.Add(contract);
        sponsoring.Offers[slot.Kind].Remove(sponsor);
        return contract;
    }

    /// <summary>Monatlich: Werbeeinnahmen buchen, Laufzeiten herunterzählen, neue Angebote.</summary>
    private void SettleSponsoring(Manager manager)
    {
        var sponsoring = Finance(manager).Sponsoring;
        foreach (var contract in sponsoring.Contracts.ToList())
        {
            Book(manager, FinanceCategory.Sponsoren, contract.MonthlyAmount);
            contract.MonthsLeft--;
            if (contract.MonthsLeft <= 0)
            {
                sponsoring.Contracts.Remove(contract);
                ContractExpired(manager, contract);
            }
        }
        RenewOffers(manager);
    }
}
