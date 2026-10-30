namespace EishockeyManager.Core;

/// <summary>
/// Werbeflächen im Bildschirm Werbung (kb\151). [ORIGINAL] Das Original führt elf Flächen je Manager (Verträge DGROUP
/// 0xC15B + 0x6E · Manager, 10 Byte je Fläche): 0–6 Banden, 7 Trikot, 8 Fernsehen, 9 und 10 die beiden Tafeln auf
/// dem Eis; Angebote je Art k (0xC313 + 0x1F4 · Manager + 0x64 · k): 0 Bande, 1 Trikot, 2 Fernsehen, 3 und 4 Eis.
/// </summary>
public enum AdSlotKind
{
    /// <summary>Sieben Bandenfelder oben.</summary>
    Bande,

    /// <summary>Erste Tafel auf dem Eis (Fläche 9): nur mit Überdachung HALLE (0x2B56A).</summary>
    Eisflaeche,

    /// <summary>Trikotwerbung (Spieler links).</summary>
    Trikot,

    /// <summary>Fernsehvertrag (Fernseher links unten), fünf Sender.</summary>
    Fernsehen,

    /// <summary>Zweite Tafel auf dem Eis (Fläche 10): nur mit Kühlanlage 'MEGAFROST' (0x2B579).</summary>
    Eisflaeche2,
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
/// (0 = keine): nach Ablauf bittet der Partner an den Verhandlungstisch. <paramref name="Rating"/> = Bewertung des
/// Vereins beim Angebot ([ORIGINAL] Feld +8 = W / 10, 0x3FDA4); sie entscheidet später über die Verlängerung.
/// </summary>
public sealed record SponsorOffer(AdSlotKind Kind, int Sponsor, int Months, int Boards, long MonthlyAmount, int OptionMonths = 0, int Rating = 0);

/// <summary>Laufender Werbevertrag.</summary>
public sealed class SponsorContract
{
    public required AdSlotKind Kind { get; init; }
    public required int Sponsor { get; init; }

    /// <summary>Belegte Bandenfelder (nur bei <see cref="AdSlotKind.Bande"/>).</summary>
    public required int[] Boards { get; init; }

    public required long MonthlyAmount { get; init; }
    public int MonthsLeft { get; set; }

    /// <summary>Verlängerungsoption in Monaten (0 = keine). [ORIGINAL] Vertragsfeld +6 (0x3FD52); nach der Verhandlung 0.</summary>
    public int OptionMonths { get; set; }

    /// <summary>Bewertung des Vereins beim Angebot ([ORIGINAL] Vertragsfeld +8), Maßstab der Verlängerung (0x2B773).</summary>
    public int SignedRating { get; init; }

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
    /// <summary>
    /// Faktor je Art (DGROUP 0x4F05 = 8, 20, 32, 15, 15, /10, #133): Bande 8, Trikot 20, Fernsehen 32, beide Eistafeln 15.
    /// </summary>
    public static int KindFactor(AdSlotKind kind) => kind switch
    {
        AdSlotKind.Bande => 8,
        AdSlotKind.Trikot => 20,
        AdSlotKind.Fernsehen => 32,
        _ => 15,
    };

    /// <summary>Partnerversatz im Betrag: Art 2 (Fernsehen) zählt den Partner um 3 höher (0x3FC81).</summary>
    public static int PartnerOffset(AdSlotKind kind) => kind == AdSlotKind.Fernsehen ? 3 : 0;

    /// <summary>Konjunktur-Faktor (DGROUP 0x2C0 = 110): Beträge × (110 − 20) / 100.</summary>
    public const int Economy = 110;

    /// <summary>
    /// Monatsbetrag eines Angebots in Drittel-DM (0x3FC1A ff.): random(W·m, 13·(W·m/10)) × Faktor/10
    /// × (65 + 8·(p + 3·[Fernsehen])) × 6 / 100 × (110 − 20) / 100 mit W = 10 × Bewertung und m = random(20, 22).
    /// </summary>
    public static long OfferAmountThirds(int rating, AdSlotKind kind, int partner, int m, Random random)
    {
        var wm = 10L * rating * m;
        long amount = SquadSetup.Between(random, (int)wm, (int)(13 * (wm / 10)));
        amount = amount * KindFactor(kind) / 10;
        var p = partner + PartnerOffset(kind);
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

    /// <summary>
    /// Tage der Angebotserneuerung (#133). [ORIGINAL] 0x3EE6B–0x3EEA2: Tag = Monatslänge (DGROUP 0x393) oder
    /// Tag = ⌊Monatslänge / 2⌋, also Monatsletzter und Monatsmitte.
    /// </summary>
    public static bool IsOfferDay(DateOnly date)
    {
        var days = DateTime.DaysInMonth(date.Year, date.Month);
        return date.Day == days || date.Day == days / 2;
    }

    /// <summary>
    /// Verlängerung eines Vertrags mit Option (#133). [ORIGINAL] 0x2B773–0x2B7C3, beim Klick im Werbebildschirm:
    /// Frage nur bei random(90, 120) · Bewertung_alt / 100 &lt; Bewertung_neu, sonst Absage.
    /// </summary>
    public static bool RenewalOffered(int signedRating, int currentRating, Random random) =>
        (long)SquadSetup.Between(random, 90, 120) * signedRating / 100 < currentRating;
}

/// <summary>Ergebnis eines Klicks auf einen abgelaufenen Vertrag mit Option.</summary>
public enum RenewalOutcome
{
    /// <summary>Kein Vertrag, der auf Verhandlung wartet.</summary>
    None,

    /// <summary>„Möchten Sie den Vertrag mit … verlängern ?“ – Antwort mit <see cref="GameState.AnswerRenewal"/>.</summary>
    Question,

    /// <summary>„Die Firma … wird den Vertrag nicht verlängern.“ – Vertrag gelöscht.</summary>
    Declined,
}

public sealed partial class ManagerFinance
{
    public Sponsoring Sponsoring { get; } = new();

    /// <summary>
    /// Gespeicherte Werbebewertung ([ORIGINAL] Manager-Feld 0x97CE): beim Spielstart (0x2A03B) und nach jedem
    /// Tagesabschluss (0x5569F) neu berechnet; die Angebote rechnen mit diesem Wert (0x3FB5A). −1 = noch nicht berechnet
    /// (Spielstände vor Schema 7).
    /// </summary>
    public int SponsorRating { get; set; } = -1;
}

public sealed partial class GameState
{
    /// <summary>Werbung eines Managers; Angebote entstehen beim Spielstart und Monatswechsel.</summary>
    public Sponsoring Ads(Manager manager) => Finance(manager).Sponsoring;

    /// <summary>
    /// Angebote beim Spielstart ([ORIGINAL] 0x2A03B–0x2A061): Bewertung berechnen und speichern, dann Angebote; ohne
    /// Würfeln nur bei einem Spiel über eine Saison (Spielende 1994, 0x2A051), sonst wie eine Erneuerung (#133).
    /// </summary>
    public void RenewOffers(Manager manager)
    {
        UpdateSponsorRating(manager);
        RefreshSponsorOffers(manager, noDice: Setup.Duration == GameDuration.EineSaison);
    }

    /// <summary>Werbebewertung neu berechnen und speichern ([ORIGINAL] 0x100:0x57 schreibt 0x97CE).</summary>
    public int UpdateSponsorRating(Manager manager) =>
        Finance(manager).SponsorRating = SponsorRating.Compute(this, manager);

    /// <summary>
    /// Angebote erneuern ([ORIGINAL] 0x3FB47) mit der gespeicherten Bewertung (W = 10 · 0x97CE). Ohne
    /// <paramref name="noDice"/> wird Partner p nur mit Wahrscheinlichkeit 1/(⌊2p/3⌋ + 1) neu bedient (sonst bleibt sein
    /// Angebot); danach fällt das Angebot aus, wenn random(0, 5 − W/200) ≠ 0 und random(0, 4) ≤ p. Fernsehen hat nur
    /// fünf Sender (Partner ≥ 5 gelöscht, 0x3FBDA). Partner mit laufendem Vertrag dieser Art bieten nichts an
    /// ([APPROX] wie bisher; das Original würfelt auch für sie).
    /// </summary>
    public void RefreshSponsorOffers(Manager manager, bool noDice)
    {
        var finance = Finance(manager);
        var sponsoring = finance.Sponsoring;
        if (finance.SponsorRating < 0)
            UpdateSponsorRating(manager);
        var rating = finance.SponsorRating;
        var w = 10 * rating;
        foreach (var kind in Enum.GetValues<AdSlotKind>())
        {
            var offers = sponsoring.Offers.TryGetValue(kind, out var existing) ? existing : sponsoring.Offers[kind] = [];
            var partners = Sponsor.For(kind);
            for (var p = 0; p < partners.Length; p++)
            {
                if (!noDice)
                {
                    if (SquadSetup.Between(FinanceRandom, 0, 2 * p / 3) != 0)
                        continue;
                    if (SquadSetup.Between(FinanceRandom, 0, 5 - w / 200) != 0 && SquadSetup.Between(FinanceRandom, 0, 4) <= p)
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
                offers[p] = new SponsorOffer(kind, p, months, boards, FinanceRules.ToMarks(amount), option, rating);
            }
            // Nach Partner geordnet neu aufbauen: Die Reihenfolge eines Dictionary hängt sonst von früheren
            // Entnahmen ab (wiederverwendete Plätze) und wäre nach Sichern und Laden eine andere.
            sponsoring.Offers[kind] = offers.OrderBy(o => o.Key).ToDictionary();
        }
    }

    /// <summary>
    /// Tagesabschluss der Werbung ([ORIGINAL] 0x3EE6B): am Monatsletzten und zur Monatsmitte neue Angebote (#133).
    /// </summary>
    private void DailySponsorOffers(DateOnly date)
    {
        if (!SponsorRules.IsOfferDay(date))
            return;
        foreach (var manager in Managers)
            RefreshSponsorOffers(manager, noDice: false);
    }

    /// <summary>Nach jedem Tagesabschluss: Bewertung aller Manager neu speichern ([ORIGINAL] 0x5569F nach 0x100:0x4D).</summary>
    private void DailySponsorRatings()
    {
        foreach (var manager in Managers)
            UpdateSponsorRating(manager);
    }

    /// <summary>
    /// Werbefläche nutzbar? [ORIGINAL] 0x2B56A–0x2B587: Eistafel 1 nur mit Überdachung HALLE (Stufe 2), Eistafel 2 nur
    /// mit Kühlanlage 'MEGAFROST' (Stufe 2); sonst bleibt die Fläche im Werbebildschirm gesperrt.
    /// </summary>
    public bool IsAdSlotAvailable(Manager manager, AdSlot slot)
    {
        var stadium = Finance(manager).Stadium;
        return slot.Kind switch
        {
            AdSlotKind.Eisflaeche => stadium.Level(StadiumFeature.Ueberdachung) == 2,
            AdSlotKind.Eisflaeche2 => stadium.Level(StadiumFeature.Kuehlanlage) == 2,
            _ => true,
        };
    }

    /// <summary>
    /// Nimmt ein Angebot für eine Werbefläche an. Bei Banden mit mehreren Feldern werden das gewählte und die nächsten
    /// freien Felder belegt. Liefert den Vertrag oder null, wenn die Fläche belegt, gesperrt oder nicht genug Felder frei sind.
    /// </summary>
    public SponsorContract? AcceptOffer(Manager manager, AdSlot slot, int sponsor)
    {
        var sponsoring = Ads(manager);
        var offer = sponsoring.OfferFrom(slot.Kind, sponsor);
        if (offer is null || sponsoring.ContractAt(slot) is not null || !IsAdSlotAvailable(manager, slot))
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
            SignedRating = offer.Rating,
        };
        sponsoring.Contracts.Add(contract);
        sponsoring.Offers[slot.Kind].Remove(sponsor);
        return contract;
    }

    /// <summary>
    /// Monatsletzter: Werbeeinnahmen aller Verträge buchen ([ORIGINAL] 0x3E70C in den Einnahmen des Monatsabschlusses
    /// 0x3F02C), auch abgelaufener Verträge mit Option, solange nicht verhandelt wurde (#133).
    /// [APPROX] Das Original summiert in Drittel-DM; das Remake führt die Beträge in DM (Rundung ≤ 1 DM je Vertrag).
    /// </summary>
    private void BookSponsoring(Manager manager)
    {
        var income = Finance(manager).Sponsoring.MonthlyIncome;
        if (income != 0)
            Book(manager, FinanceCategory.Sponsoren, income);
    }

    /// <summary>
    /// Monatserster: Restlaufzeiten herunterzählen ([ORIGINAL] 0x3EF52–0x3F010). Bei 0 endet ein Vertrag ohne Option
    /// still; mit Option bleibt er (und zahlt), und der Manager bekommt einmal die Meldung „Ein Werbepartner mit
    /// Verlängerungsoption / bittet Sie an den Verhandlungstisch.“ – die Frage kommt erst im Werbebildschirm.
    /// </summary>
    private void CountDownSponsorContracts(Manager manager)
    {
        var sponsoring = Finance(manager).Sponsoring;
        var notified = false;
        foreach (var contract in sponsoring.Contracts.ToList())
        {
            if (contract.MonthsLeft <= 0)
                continue;
            if (--contract.MonthsLeft > 0)
                continue;
            if (contract.OptionMonths <= 0)
                sponsoring.Contracts.Remove(contract);
            else if (!notified)
            {
                notified = true;
                Notify(manager, "Ein Werbepartner mit Verlängerungsoption", "bittet Sie an den Verhandlungstisch.");
            }
        }
    }

    /// <summary>Abgelaufener Vertrag mit Option, über den noch nicht verhandelt wurde.</summary>
    public bool AwaitsRenewal(SponsorContract contract) => contract.MonthsLeft <= 0 && contract.OptionMonths > 0;

    /// <summary>
    /// Klick auf einen abgelaufenen Vertrag mit Option im Werbebildschirm ([ORIGINAL] 0x2B75F–0x2B889): zuerst
    /// random(90, 120), dann die Bewertung neu berechnen und speichern (0x2B7A6). Frage nur, wenn
    /// <see cref="SponsorRules.RenewalOffered"/>; sonst Absage, Vertrag gelöscht.
    /// </summary>
    public RenewalOutcome OpenRenewal(Manager manager, SponsorContract contract)
    {
        var sponsoring = Finance(manager).Sponsoring;
        if (!sponsoring.Contracts.Contains(contract) || !AwaitsRenewal(contract))
            return RenewalOutcome.None;
        var factor = SquadSetup.Between(FinanceRandom, 90, 120);
        var current = UpdateSponsorRating(manager);
        if ((long)factor * contract.SignedRating / 100 < current)
            return RenewalOutcome.Question;
        contract.OptionMonths = 0;
        sponsoring.Contracts.Remove(contract);
        return RenewalOutcome.Declined;
    }

    /// <summary>
    /// Antwort auf „Möchten Sie den Vertrag mit … verlängern ?“ ([ORIGINAL] 0x2B81E–0x2B88C): Ja verlängert um die
    /// Option (Restlaufzeit = Option, Option danach 0), Nein löscht den Vertrag.
    /// </summary>
    public bool AnswerRenewal(Manager manager, SponsorContract contract, bool yes)
    {
        var sponsoring = Finance(manager).Sponsoring;
        if (!sponsoring.Contracts.Contains(contract) || !AwaitsRenewal(contract))
            return false;
        if (yes)
            contract.MonthsLeft = contract.OptionMonths;
        else
            sponsoring.Contracts.Remove(contract);
        contract.OptionMonths = 0;
        return true;
    }
}
