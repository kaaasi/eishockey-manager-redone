namespace EishockeyManager.Core;

/// <summary>
/// Kategorien des Finanzbuchs (Klick auf Bank/Konto im Bildschirm Finanzen).
/// Reihenfolge wie in <c>docs/anforderungen/bildschirme.md</c>.
/// </summary>
public enum FinanceCategory
{
    Eintritt,
    Gastronomie,
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
        _ => category.ToString(),
    };
}

/// <summary>
/// Größenordnungen der Finanzen. [APPROX] Die Formeln des Originals (Overlay 502F „Einnahmen/Ausgaben“)
/// sind noch nicht rekonstruiert. Kalibriert auf das UI-Video: Berliner SC (Oberliga) hat zu Spielbeginn
/// 400.000 DM, monatliche Ausgaben 83.233 DM und Einnahmen 3.966 DM (Bildschirm Werbung, 6:20).
/// </summary>
public static class FinanceRules
{
    /// <summary>Zinssatz Bankkredit pro Monat (Text in em.exe: „Geld von der Bank abheben/leihen (4%)“).</summary>
    public const double CreditRate = 0.04;

    /// <summary>Zinssatz Sparbuch pro Monat („Geld aufs Sparbuch einzahlen (1%)“).</summary>
    public const double SavingsRate = 0.01;

    /// <summary>Zinssatz zwischen Managern pro Monat („Geld von … leihen (2.5%)“).</summary>
    public const double ManagerLoanRate = 0.025;

    /// <summary>[APPROX] Höchstbetrag des Bankkredits.</summary>
    public const long CreditLimit = 1_000_000;

    /// <summary>
    /// Monatliche Gehälter eines Vereins: Summe der Vertragsgehälter (<see cref="Player.MonthlySalary"/>, Bildschirm
    /// „Verträge“). Nur ein Verein ganz ohne Kader (z. B. ein von Hand angelegter Spielstand ohne
    /// <see cref="SquadSetup"/>) wird über die <see cref="FallbackWages"/> abgerechnet.
    /// </summary>
    public static long MonthlyWages(Club club) =>
        club.Players.Count == 0 ? FallbackWages(club) : club.Players.Sum(p => (long)p.MonthlySalary);

    /// <summary>Monatliche Versicherungsprämien der Verträge (Finanzbuch „Versicherung“, getrennt von den Gehältern).</summary>
    public static long MonthlyInsurance(Club club) => club.Players.Sum(p => (long)Contracts.InsurancePremium(p));

    /// <summary>
    /// [APPROX] Ersatzformel aus Runde 1, ausdrücklich begrenzt auf Vereine ohne Kader (siehe <see cref="MonthlyWages"/>):
    /// <see cref="MinimumSquad"/> × <see cref="MonthlyWagePerPlayer"/>. Nicht kalibriert, keine Spielregel.
    /// </summary>
    public static long FallbackWages(Club club) => MinimumSquad * MonthlyWagePerPlayer(club.Strength);

    /// <summary>[APPROX] Monatsgehalt je Spieler der Ersatzformel: Sockel + Faktor × Vereinsstärke.</summary>
    public static long MonthlyWagePerPlayer(int clubStrength) => 1_400 + 80L * clubStrength;

    /// <summary>[APPROX] Kadergröße der Ersatzformel.</summary>
    public const int MinimumSquad = 20;

    /// <summary>[APPROX] Grundeinnahmen pro Monat (Verband, Fernsehen) je Liga.</summary>
    public static long BaseIncome(League? league) => league switch
    {
        League.Bundesliga => 40_000,
        League.ZweiteLiga => 15_000,
        League.Oberliga => 4_000,
        _ => 2_000,
    };
}
