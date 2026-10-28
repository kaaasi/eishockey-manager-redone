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

/// <summary>
/// Angebot eines Partners für eine Art von Werbefläche. <paramref name="OptionMonths"/> = Verlängerungsoption
/// (0 = keine): nach Ablauf fragt der Partner, ob um diese Monate verlängert wird.
/// </summary>
public sealed record SponsorOffer(AdSlotKind Kind, int Sponsor, int Months, int Boards, long MonthlyAmount, int OptionMonths = 0);

/// <summary>Laufender Werbevertrag.</summary>
public sealed class SponsorContract
{
    public required AdSlotKind Kind { get; init; }
    public required int Sponsor { get; init; }

    /// <summary>Belegte Bandenfelder (nur bei <see cref="AdSlotKind.Bande"/>).</summary>
    public required int[] Boards { get; init; }

    public required long MonthlyAmount { get; init; }
    public int MonthsLeft { get; set; }

    /// <summary>Verlängerungsoption in Monaten (0 = keine). [ORIGINAL] Vertragsfeld +6 (0x3FD52).</summary>
    public int OptionMonths { get; init; }

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
/// Werbeangebote nach dem Original (Spur finanzen-2). [ORIGINAL] em.exe 0x3FB47 (Einsprung 0x100:0x5C): je Art und
/// Partner ein Angebot aus der Bewertung des Vereins (<see cref="SponsorRating"/>), am Spielstart, am Monatsersten
/// und zur Monatsmitte. Belege: docs/finanzen-rekonstruktion.md.
/// </summary>
public static class SponsorRules
{
    /// <summary>Faktor je Art (DGROUP 0x4F05, /10): Bande 8, Eis 20, Trikot 32, Fernsehen 15.</summary>
    public static int KindFactor(AdSlotKind kind) => kind switch
    {
        AdSlotKind.Bande => 8,
        AdSlotKind.Eisflaeche => 20,
        AdSlotKind.Trikot => 32,
        _ => 15,
    };

    /// <summary>Konjunktur-Faktor (DGROUP 0x2C0 = 110): Beträge × (110 − 20) / 100.</summary>
    public const int Economy = 110;

    /// <summary>Trikotwerbung nur von den Partnern 0–4 (0x3FBDA).</summary>
    public const int ShirtPartners = 5;

    /// <summary>
    /// Monatsbetrag eines Angebots in Drittel-DM (0x3FC1A ff.): random(W·m, 13·(W·m/10)) × Faktor/10
    /// × (65 + 8·(p + 3·[Trikot])) × 6 / 100 × (110 − 20) / 100 mit W = 10 × Bewertung und m = random(20, 22).
    /// </summary>
    public static long OfferAmountThirds(int rating, AdSlotKind kind, int partner, int m, Random random)
    {
        var wm = 10L * rating * m;
        long amount = SquadSetup.Between(random, (int)wm, (int)(13 * (wm / 10)));
        amount = amount * KindFactor(kind) / 10;
        var p = partner + (kind == AdSlotKind.Trikot ? 3 : 0);
        amount = amount * (65 + 8 * p) * 6 / 100;
        return amount * (Economy - 20) / 100;
    }

    /// <summary>Laufzeit 12 oder 24 Monate (0x3FD3C), Option mit 1/8: 12 × random(1, 3 − Laufzeit/12) Monate (0x3FD52).</summary>
    public static (int Months, int OptionMonths) Duration(Random random)
    {
        var years = SquadSetup.Between(random, 1, 2);
        var option = SquadSetup.Between(random, 0, 7) == 0 ? 12 * SquadSetup.Between(random, 1, 3 - years) : 0;
        return (12 * years, option);
    }

    /// <summary>Bandenfelder: random(2, 3) mit Wahrscheinlichkeit 1/(m − 14), sonst 1 (0x3FD07).</summary>
    public static int Boards(int m, Random random) =>
        SquadSetup.Between(random, 0, m - 15) == 0 ? SquadSetup.Between(random, 2, 3) : 1;
}

public sealed partial class ManagerFinance
{
    public Sponsoring Sponsoring { get; } = new();
}

public sealed partial class GameState
{
    /// <summary>Werbung eines Managers; Angebote entstehen beim Spielstart und Monatswechsel.</summary>
    public Sponsoring Ads(Manager manager) => Finance(manager).Sponsoring;

    /// <summary>
    /// Angebote beim Spielstart: jede Art und jeder Partner bekommt eines ([ORIGINAL] 0x3FBA0, Flag „Spielstart“).
    /// </summary>
    public void RenewOffers(Manager manager) => RefreshSponsorOffers(manager, atStart: true);

    /// <summary>
    /// Angebote erneuern ([ORIGINAL] 0x3FB47): Partner p wird nur mit Wahrscheinlichkeit 1/(⌊2p/3⌋ + 1) neu bedient
    /// (sonst bleibt sein Angebot); Trikot nur Partner 0–4; danach fällt das Angebot aus, wenn random(0, 5 − W/200) ≠ 0
    /// und random(0, 4) ≤ p. Partner mit laufendem Vertrag dieser Art bieten nichts an ([APPROX] wie bisher).
    /// </summary>
    public void RefreshSponsorOffers(Manager manager, bool atStart)
    {
        var sponsoring = Finance(manager).Sponsoring;
        var rating = SponsorRating.Compute(this, manager);
        var w = 10 * rating;
        foreach (var kind in Enum.GetValues<AdSlotKind>())
        {
            var offers = sponsoring.Offers.TryGetValue(kind, out var existing) ? existing : sponsoring.Offers[kind] = [];
            var partners = Sponsor.For(kind);
            for (var p = 0; p < partners.Length; p++)
            {
                if (!atStart)
                {
                    if (SquadSetup.Between(FinanceRandom, 0, 2 * p / 3) != 0)
                        continue;
                    if (kind == AdSlotKind.Trikot && p >= SponsorRules.ShirtPartners
                        || SquadSetup.Between(FinanceRandom, 0, 5 - w / 200) != 0 && SquadSetup.Between(FinanceRandom, 0, 4) <= p)
                    {
                        offers.Remove(p);
                        continue;
                    }
                }
                if (sponsoring.Contracts.Any(c => c.Kind == kind && c.Sponsor == p))
                {
                    offers.Remove(p);
                    continue;
                }
                var m = SquadSetup.Between(FinanceRandom, 20, 22);
                var amount = SponsorRules.OfferAmountThirds(rating, kind, p, m, FinanceRandom);
                var boards = kind == AdSlotKind.Bande ? SponsorRules.Boards(m, FinanceRandom) : 1;
                var (months, option) = SponsorRules.Duration(FinanceRandom);
                offers[p] = new SponsorOffer(kind, p, months, boards, FinanceRules.ToMarks(amount), option);
            }
            // Nach Partner geordnet neu aufbauen: Die Reihenfolge eines Dictionary hängt sonst von früheren
            // Entnahmen ab (wiederverwendete Plätze) und wäre nach Sichern und Laden eine andere.
            sponsoring.Offers[kind] = offers.OrderBy(o => o.Key).ToDictionary();
        }
    }

    /// <summary>
    /// Tagesabschluss der Werbung ([ORIGINAL] 0x3EE6B): am Monatsersten und zur Monatsmitte neue Angebote.
    /// </summary>
    private void DailySponsorOffers(DateOnly date)
    {
        var days = DateTime.DaysInMonth(date.Year, date.Month);
        if (date.Day != 1 && date.Day != days / 2)
            return;
        foreach (var manager in Managers)
            RefreshSponsorOffers(manager, atStart: false);
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
            OptionMonths = offer.OptionMonths,
        };
        sponsoring.Contracts.Add(contract);
        sponsoring.Offers[slot.Kind].Remove(sponsor);
        return contract;
    }

    /// <summary>
    /// Monatlich: Werbeeinnahmen buchen, Laufzeiten herunterzählen ([ORIGINAL] 0x3EF73). Neue Angebote kommen
    /// über den Tagesabschluss (<see cref="DailySponsorOffers"/>).
    /// </summary>
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
    }
}
