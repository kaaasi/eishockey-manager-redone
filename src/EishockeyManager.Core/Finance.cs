namespace EishockeyManager.Core;

/// <summary>
/// Kategorien des Finanzbuchs (Klick auf Bank/Konto im Bildschirm Finanzen).
/// Reihenfolge wie in <c>docs/anforderungen/bildschirme.md</c>.
/// </summary>
public enum FinanceCategory
{
    Eintritt,
    // „Gastronomie“ entfällt (Spur finanzen-2): Das Original kennt keine Gastronomie-Einnahme, nur die Ausbaustufe.
    Sponsoren,
    Transfers,
    Gehaelter,
    Unterhalt,
    Zinsen,
    Training,
    Bau,
    Talentsuche,
    Strafen,
    Freundschaftsspiele,
    Versicherung,
    ManagerKredite,
    Jugend,
    Aktien,

    /// <summary>Saisonprämien für Platzierung und Titel (Bildschirm „Saisonende“). Neu in Runde 2.</summary>
    Praemien,
}

/// <summary>Einnahmen und Ausgaben je Kategorie für die laufende und die vorige Saison.</summary>
public sealed class FinanceBook
{
    public static readonly int CategoryCount = Enum.GetValues<FinanceCategory>().Length;

    private long[] _current = new long[CategoryCount];
    private long[] _previous = new long[CategoryCount];

    /// <summary>Saldo einer Kategorie (positiv = Einnahme) in der laufenden Saison.</summary>
    public long Current(FinanceCategory category) => _current[(int)category];

    /// <summary>Saldo einer Kategorie in der vorigen Saison.</summary>
    public long Previous(FinanceCategory category) => _previous[(int)category];

    public long CurrentTotal => _current.Sum();
    public long PreviousTotal => _previous.Sum();

    public void Add(FinanceCategory category, long amount) => _current[(int)category] += amount;

    /// <summary>Saisonwechsel: laufende Saison wird zur vorigen.</summary>
    public void NewSeason()
    {
        _previous = _current;
        _current = new long[CategoryCount];
    }

    /// <summary>Anzeigename im Finanzbuch.</summary>
    public static string Label(FinanceCategory category) => category switch
    {
        FinanceCategory.Gehaelter => "Gehälter",
        FinanceCategory.ManagerKredite => "Manager-Kredite",
        FinanceCategory.Praemien => "Prämien",
        _ => category.ToString(),
    };
}

/// <summary>
/// Regeln der Finanzen. Zinssätze aus den Texten in em.exe; Monatsformeln in <c>FinanceMonthly.cs</c>
/// (Belege: <c>docs/finanzen-rekonstruktion.md</c>).
/// </summary>
public static partial class FinanceRules
{
    /// <summary>Zinssatz Bankkredit pro Monat (Text in em.exe: „Geld von der Bank abheben/leihen (4%)“).</summary>
    public const double CreditRate = 0.04;

    /// <summary>Zinssatz Sparbuch pro Monat („Geld aufs Sparbuch einzahlen (1%)“).</summary>
    public const double SavingsRate = 0.01;

    /// <summary>Zinssatz zwischen Managern pro Monat („Geld von … leihen (2.5%)“).</summary>
    public const double ManagerLoanRate = 0.025;

    /// <summary>
    /// [ORIGINAL] Höchstbetrag des Bankkredits: em.exe Datei 0x596C2–0x59A12 (Höchstwert links „holen/leihen“ =
    /// 6.000.000 Drittel-DM minus Schuld bei der Bank), also 2 Mio. DM (docs/analyse/re-geld.md, „Betragsbalken“).
    /// </summary>
    public const long CreditLimit = 2_000_000;

    /// <summary>
    /// [ORIGINAL] Grenze zwischen zwei Managern je Richtung: em.exe Datei 0x596C2–0x59A12 (3.000.000 Drittel-DM minus
    /// bestehende Forderung bzw. Schuld), also 1 Mio. DM.
    /// </summary>
    public const long ManagerLoanLimit = 1_000_000;

    /// <summary>[ORIGINAL] Börsengebühr in Promille beim Kauf und Verkauf: em.exe Datei 0x5915F–0x5952D (w · 15 / 1000).</summary>
    public const long StockFeePerMille = 15;

    /// <summary>
    /// Börsengebühr für einen Umsatz <paramref name="value"/> in DM. [APPROX] Das Original rechnet in Drittel-DM
    /// (⌊3w · 15 / 1000⌋); das Remake führt ganze DM und verwirft den Drittel-Rest (⌊⌊3w · 15 / 1000⌋ / 3⌋ = ⌊w · 15 / 1000⌋).
    /// </summary>
    public static long StockFee(long value) => value * StockFeePerMille / 1000;

    /// <summary>
    /// Monatliche Gehälter eines Vereins: Summe der Vertragsgehälter (<see cref="Player.MonthlySalary"/>, Bildschirm
    /// „Verträge“). [ORIGINAL] em.exe 0x3E8DC: Summe über die Spieler des Managers, ohne Kader also 0 – die
    /// frühere Ersatzpauschale für leere Kader gibt es im Original nicht.
    /// </summary>
    public static long MonthlyWages(Club club) => club.Players.Sum(p => (long)p.MonthlySalary);

    /// <summary>Monatliche Versicherungsprämien der Verträge (Finanzbuch „Versicherung“, getrennt von den Gehältern).</summary>
    public static long MonthlyInsurance(Club club) => club.Players.Sum(p => (long)Contracts.InsurancePremium(p));
}
