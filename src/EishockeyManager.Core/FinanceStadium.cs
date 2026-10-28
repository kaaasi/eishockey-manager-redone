namespace EishockeyManager.Core;

/// <summary>Ausbaubereiche des Stadions in der Reihenfolge des Bildschirms Stadion (Texte aus em.exe).</summary>
public enum StadiumFeature
{
    Ueberdachung,
    Anzeigetafel,
    Kuehlanlage,
    Sitzkomfort,
    Zustand,
    Verkehrsanbindung,
    Gastronomie,
    Kino,
    Bowlingbahn,
}

/// <summary>Was gerade gebaut wird: ein Ausbaubereich oder Plätze.</summary>
public enum StadiumProject
{
    Feature,
    Sitzplaetze,
    Stehplaetze,
}

/// <summary>
/// Laufende Baustelle. <see cref="Target"/> = Zielstufe des Bereichs bzw. Zahl der Ausbauschritte bei Plätzen.
/// </summary>
public sealed record StadiumConstruction(StadiumProject Project, StadiumFeature Feature, int Target, int WeeksLeft, long Cost)
{
    public int WeeksLeft { get; set; } = WeeksLeft;

    /// <summary>Bezeichnung wie im Bildschirm („Kino“, „Sitzplätze“).</summary>
    public string Label => Project switch
    {
        StadiumProject.Sitzplaetze => "Sitzplätze",
        StadiumProject.Stehplaetze => "Stehplätze",
        _ => Stadium.Features[(int)Feature].Label,
    };
}

/// <summary>Stadion eines Managervereins (Bildschirm Stadion, kb\14).</summary>
public sealed class Stadium
{
    public static readonly int FeatureCount = Enum.GetValues<StadiumFeature>().Length;

    /// <summary>Bezeichnungen und Stufen wie in em.exe.</summary>
    public static readonly (string Label, string[] Levels)[] Features =
    [
        ("Überdachung", ["OHNE DACH", "HALBOFFEN", "HALLE"]),
        ("Anzeigetafel", ["MANUELL", "DIGITAL", "HIGH-TECH"]),
        ("Kühlanlage", ["'KÜHLI'", "'FROSTA'", "'MEGAFROST'"]),
        ("Sitzkomfort", ["PLASTIKSCHALEN", "SITZE MIT KISSEN", "FAST SOFAS"]),
        ("Zustand", ["GANZ ÜBEL", "SCHLECHT", "MITTEL", "GANZ GUT", "SEHR GUT"]),
        ("Verkehrsanb.", ["FUßWEG", "STRAßE", "AUTOBAHN"]),
        ("Gastronomie", ["OHNE", "IMBIßBUDE", "KNEIPEN", "RESTAURANT"]),
        ("Kino", ["OHNE", "KLEIN", "MITTEL", "GROß"]),
        ("Bowlingbahn", ["OHNE", "VORHANDEN"]),
    ];

    public const int SeatStep = 500;
    public const int StandingStep = 1_000;
    public const int MaxSeats = 15_000;
    public const int MaxStanding = 15_000;
    public const int MinTicketPrice = 5;
    public const int MaxTicketPrice = 50;

    private readonly int[] _levels = new int[FeatureCount];

    public Stadium()
    {
        _levels[(int)StadiumFeature.Zustand] = 2; // „MITTEL“ (Video, Spielbeginn)
    }

    public int Seats { get; set; } = 2_000;
    public int Standing { get; set; } = 2_500;
    public int Capacity => Seats + Standing;

    /// <summary>
    /// Eintritt in DM, mit ▲▼ in 1-DM-Schritten. [ORIGINAL] Spielstart em.exe 0x29F2E: (40 − 4 × Start-Liga) / 3,
    /// also 13 / 12 / 10 DM für Bundesliga / 2. Liga / Oberliga (Video: 10 DM in der Oberliga).
    /// </summary>
    public int TicketPrice { get; set; } = 10;

    public StadiumConstruction? Construction { get; set; }

    public int Level(StadiumFeature feature) => _levels[(int)feature];

    public void SetLevel(StadiumFeature feature, int level) =>
        _levels[(int)feature] = Math.Clamp(level, 0, Features[(int)feature].Levels.Length - 1);

    public string LevelText(StadiumFeature feature) => Features[(int)feature].Levels[Level(feature)];

    public bool IsMaxed(StadiumFeature feature) => Level(feature) >= Features[(int)feature].Levels.Length - 1;

    /// <summary>
    /// Startstadion je Liga. [ORIGINAL] Spielstart em.exe 0x29F77 ff.: 3.000 − 500 × Liga Sitzplätze, 4.500 − 1.000 × Liga
    /// Stehplätze (Liga 0 = Bundesliga); Überdachung 2 − Liga, Anzeigetafel/Kühlanlage/Verkehrsanbindung nur in der
    /// Bundesliga, Gastronomie (Imbißbude) in Bundesliga und 2. Liga, Zustand MITTEL. Video (Oberliga): 2.000/2.500.
    /// </summary>
    public static Stadium ForLeague(League? league)
    {
        var tier = league is { } l ? (int)l : 2;
        var stadium = new Stadium { Seats = 3_000 - 500 * tier, Standing = 4_500 - 1_000 * tier, TicketPrice = (40 - 4 * tier) / 3 };
        stadium.SetLevel(StadiumFeature.Ueberdachung, 2 - tier);
        if (tier == 0)
        {
            stadium.SetLevel(StadiumFeature.Anzeigetafel, 1);
            stadium.SetLevel(StadiumFeature.Kuehlanlage, 1);
            stadium.SetLevel(StadiumFeature.Verkehrsanbindung, 1);
        }
        if (tier < 2)
            stadium.SetLevel(StadiumFeature.Gastronomie, 1);
        return stadium;
    }

    /// <summary>
    /// Kosten des ersten Ausbauschritts. Aus dem UI-Video (Oberliga, Spielbeginn): Kühlanlage 'FROSTA' 640.000 DM,
    /// Straße 1.200.000 DM, Imbißbude 580.000 DM, Kino KLEIN 520.000 DM, Bowlingbahn 450.000 DM.
    /// [APPROX] Überdachung, Anzeigetafel, Sitzkomfort und Zustand geschätzt; jede weitere Stufe kostet ein Vielfaches.
    /// </summary>
    public long FirstStepCost(StadiumFeature feature) => feature switch
    {
        StadiumFeature.Ueberdachung => 1_500_000,
        StadiumFeature.Anzeigetafel => 250_000,
        StadiumFeature.Kuehlanlage => 640_000,
        StadiumFeature.Sitzkomfort => 60L * Seats,
        StadiumFeature.Zustand => 100_000 + 20L * Capacity,
        StadiumFeature.Verkehrsanbindung => 1_200_000,
        StadiumFeature.Gastronomie => 580_000,
        StadiumFeature.Kino => 520_000,
        _ => 450_000,
    };

    /// <summary>[APPROX] Bauzeit in Wochen je Stufe.</summary>
    public static int WeeksPerStep(StadiumFeature feature) => feature switch
    {
        StadiumFeature.Ueberdachung => 8,
        StadiumFeature.Verkehrsanbindung => 6,
        StadiumFeature.Kino or StadiumFeature.Bowlingbahn => 5,
        StadiumFeature.Gastronomie or StadiumFeature.Kuehlanlage => 3,
        _ => 2,
    };

    /// <summary>
    /// Kosten und Bauzeit für den Ausbau eines Bereichs auf <paramref name="target"/> (Stufe n → n+1 kostet
    /// n+1 × ersten Schritt); null, wenn die Stufe nicht über der aktuellen liegt oder es sie nicht gibt.
    /// </summary>
    public (long Cost, int Weeks)? Offer(StadiumFeature feature, int target)
    {
        var current = Level(feature);
        if (target <= current || target >= Features[(int)feature].Levels.Length)
            return null;
        long cost = 0;
        for (var step = current + 1; step <= target; step++)
            cost += step * FirstStepCost(feature);
        return (cost, (target - current) * WeeksPerStep(feature));
    }

    /// <summary>[APPROX] Plätze bauen: 500 Sitzplätze für 110.000 DM (3 Wochen), 1.000 Stehplätze für 60.000 DM (2 Wochen).</summary>
    public (long Cost, int Weeks)? PlacesOffer(StadiumProject project, int steps = 1)
    {
        if (steps <= 0)
            return null;
        return project switch
        {
            StadiumProject.Sitzplaetze when Seats + steps * SeatStep <= MaxSeats => (steps * SeatStep * 220L, 2 + steps),
            StadiumProject.Stehplaetze when Standing + steps * StandingStep <= MaxStanding => (steps * StandingStep * 60L, 1 + steps),
            _ => null,
        };
    }

    /// <summary>Schließt eine Baustelle ab.</summary>
    public void Complete(StadiumConstruction construction)
    {
        switch (construction.Project)
        {
            case StadiumProject.Sitzplaetze:
                Seats = Math.Min(MaxSeats, Seats + construction.Target * SeatStep);
                break;
            case StadiumProject.Stehplaetze:
                Standing = Math.Min(MaxStanding, Standing + construction.Target * StandingStep);
                break;
            default:
                SetLevel(construction.Feature, construction.Target);
                break;
        }
    }

    /// <summary>[APPROX] Attraktivität des Stadions als Faktor auf die Zuschauernachfrage (≈ 0,9 … 1,6).</summary>
    public double Attractiveness =>
        0.9
        + 0.08 * Level(StadiumFeature.Ueberdachung)
        + 0.03 * Level(StadiumFeature.Anzeigetafel)
        + 0.03 * Level(StadiumFeature.Kuehlanlage)
        + 0.03 * Level(StadiumFeature.Sitzkomfort)
        + 0.04 * (Level(StadiumFeature.Zustand) - 2)
        + 0.06 * Level(StadiumFeature.Verkehrsanbindung)
        + 0.03 * Level(StadiumFeature.Gastronomie)
        + 0.02 * Level(StadiumFeature.Kino)
        + 0.04 * Level(StadiumFeature.Bowlingbahn);

    /// <summary>[APPROX] Gastronomie-Umsatz je Zuschauer in DM.</summary>
    public int GastronomyPerSpectator => Level(StadiumFeature.Gastronomie) switch
    {
        1 => 2,
        2 => 4,
        3 => 6,
        _ => 0,
    };

    /// <summary>
    /// Unterhalt pro Monat in Drittel-DM je Ausbaustufe. [ORIGINAL] em.exe 0x3EA0B ff. mit Tabelle DGROUP 0x94:
    /// Überdachung 24.000, Anzeigetafel 7.500, Kühlanlage 33.000, Sitzkomfort 3.000, Zustand 4.800, Verkehrsanbindung
    /// 9.000, Gastronomie 15.000, Kino 15.000, Bowlingbahn 7.500 (× Stufe); dazu 15 je Sitz- und 12 je Stehplatz (DGROUP 0xB8).
    /// </summary>
    private static readonly long[] UpkeepPerLevelThirds = [24_000, 7_500, 33_000, 3_000, 4_800, 9_000, 15_000, 15_000, 7_500];

    /// <summary>Unterhalt pro Monat in Drittel-DM. [ORIGINAL] siehe <see cref="UpkeepPerLevelThirds"/>.</summary>
    public long MonthlyUpkeepThirds =>
        Enumerable.Range(0, FeatureCount).Sum(i => _levels[i] * UpkeepPerLevelThirds[i]) + 15L * Seats + 12L * Standing;

    /// <summary>Unterhalt pro Monat in DM (Finanzbuch „Unterhalt“).</summary>
    public long MonthlyUpkeep => FinanceRules.ToMarks(MonthlyUpkeepThirds);
}

public sealed partial class GameState
{
    /// <summary>
    /// Zuschauer eines Heimspiels. [APPROX] Nachfrage je Liga × Tabellenplatz × Gegner × Stadion × Preis, gedeckelt
    /// durch die Plätze. Preisempfindlichkeit so gewählt, dass sich ein volles Stadion lohnt.
    /// </summary>
    public int Spectators(Club home, Club away, Stadium stadium, int matchDay, Random random)
    {
        var demand = home.League switch
        {
            League.Bundesliga => 6_500.0,
            League.ZweiteLiga => 3_200.0,
            League.Oberliga => 1_600.0,
            _ => 1_000.0,
        };

        if (home.League is { } league && matchDay > 1)
        {
            var table = LeagueTable.Compute(this, league, TableView.Gesamt, matchDay - 1);
            var row = table.FirstOrDefault(r => r.Club.Index == home.Index);
            if (row is not null && table.Count > 1)
                demand *= 1.3 - 0.6 * (row.Rank - 1) / (table.Count - 1);
        }

        var average = LeagueClubs(home.League ?? League.Oberliga).Select(c => (double)c.Strength).DefaultIfEmpty(50).Average();
        demand *= Math.Clamp(0.8 + 0.2 * away.Strength / Math.Max(1, average), 0.8, 1.3);
        demand *= stadium.Attractiveness;
        demand *= Math.Pow(10.0 / stadium.TicketPrice, 1.3);
        demand *= 0.9 + 0.2 * random.NextDouble();
        return (int)Math.Clamp(demand, 0, stadium.Capacity);
    }
}

public sealed partial class GameState
{
    /// <summary>
    /// Beginnt einen Stadionausbau („Möchten Sie Ihr Stadion ausbauen ?“): Kosten sofort (Finanzbuch „Bau“),
    /// fertig nach der Bauzeit. Es gibt höchstens eine Baustelle. Liefert false, wenn das nicht geht.
    /// [APPROX] Ob das Original mehrere Baustellen oder Ratenzahlung kennt, ist offen.
    /// </summary>
    public bool StartConstruction(Manager manager, StadiumProject project, StadiumFeature feature, int target)
    {
        var stadium = Finance(manager).Stadium;
        if (stadium.Construction is not null)
            return false;
        var offer = project == StadiumProject.Feature ? stadium.Offer(feature, target) : stadium.PlacesOffer(project, target);
        if (offer is not { } o)
            return false;
        Book(manager, FinanceCategory.Bau, -o.Cost);
        stadium.Construction = new StadiumConstruction(project, feature, target, o.Weeks, o.Cost);
        return true;
    }

    /// <summary>Wöchentlich: Baufortschritt. Liefert den abgeschlossenen Bau (für die Meldung im Posteingang).</summary>
    private StadiumConstruction? AdvanceConstruction(Manager manager)
    {
        var stadium = Finance(manager).Stadium;
        if (stadium.Construction is not { } construction)
            return null;
        construction.WeeksLeft--;
        if (construction.WeeksLeft > 0)
            return null;
        stadium.Complete(construction);
        stadium.Construction = null;
        return construction;
    }
}
