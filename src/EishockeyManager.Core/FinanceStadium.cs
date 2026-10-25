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

/// <summary>Laufende Baustelle.</summary>
public sealed record StadiumConstruction(StadiumProject Project, StadiumFeature Feature, int WeeksLeft, long Cost)
{
    public int WeeksLeft { get; set; } = WeeksLeft;
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
        ("Kino", ["OHNE", "KLEIN", "MITTEL"]),
        // [APPROX] Stufen der Bowlingbahn: in em.exe folgen nach Kino keine eigenen Texte → gleiche Liste angenommen.
        ("Bowlingbahn", ["OHNE", "KLEIN", "MITTEL"]),
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

    /// <summary>Eintritt in DM (Video: 10 DM zu Spielbeginn), mit ▲▼ in 1-DM-Schritten.</summary>
    public int TicketPrice { get; set; } = 10;

    public StadiumConstruction? Construction { get; set; }

    public int Level(StadiumFeature feature) => _levels[(int)feature];

    public void SetLevel(StadiumFeature feature, int level) =>
        _levels[(int)feature] = Math.Clamp(level, 0, Features[(int)feature].Levels.Length - 1);

    public string LevelText(StadiumFeature feature) => Features[(int)feature].Levels[Level(feature)];

    public bool IsMaxed(StadiumFeature feature) => Level(feature) >= Features[(int)feature].Levels.Length - 1;

    /// <summary>[APPROX] Startstadion je Liga. Im Video (Oberliga): 2.000 Sitz- und 2.500 Stehplätze.</summary>
    public static Stadium ForLeague(League? league) => league switch
    {
        League.Bundesliga => new Stadium { Seats = 3_500, Standing = 5_000 },
        League.ZweiteLiga => new Stadium { Seats = 2_500, Standing = 3_500 },
        _ => new Stadium(),
    };

    /// <summary>[APPROX] Kosten und Bauzeit (Wochen) des nächsten Ausbauschritts; null, wenn nichts mehr geht.</summary>
    public (long Cost, int Weeks)? Offer(StadiumProject project, StadiumFeature feature = default)
    {
        switch (project)
        {
            case StadiumProject.Sitzplaetze:
                return Seats >= MaxSeats ? null : (SeatStep * 220L, 3);
            case StadiumProject.Stehplaetze:
                return Standing >= MaxStanding ? null : (StandingStep * 60L, 2);
        }
        if (IsMaxed(feature))
            return null;
        var next = Level(feature) + 1;
        return feature switch
        {
            StadiumFeature.Ueberdachung => (next * 900_000L, next * 8),
            StadiumFeature.Anzeigetafel => (next * 150_000L, next * 2),
            StadiumFeature.Kuehlanlage => (next * 250_000L, next * 3),
            StadiumFeature.Sitzkomfort => (next * 30L * Seats, 2),
            StadiumFeature.Zustand => (next * 40_000L + Capacity * 10L, 2),
            StadiumFeature.Verkehrsanbindung => (next * 400_000L, next * 6),
            StadiumFeature.Gastronomie => (next * 120_000L, next * 3),
            StadiumFeature.Kino => (next * 350_000L, next * 5),
            StadiumFeature.Bowlingbahn => (next * 250_000L, next * 4),
            _ => null,
        };
    }

    /// <summary>Schließt eine Baustelle ab.</summary>
    public void Complete(StadiumConstruction construction)
    {
        switch (construction.Project)
        {
            case StadiumProject.Sitzplaetze:
                Seats = Math.Min(MaxSeats, Seats + SeatStep);
                break;
            case StadiumProject.Stehplaetze:
                Standing = Math.Min(MaxStanding, Standing + StandingStep);
                break;
            default:
                SetLevel(construction.Feature, Level(construction.Feature) + 1);
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
        + 0.02 * Level(StadiumFeature.Bowlingbahn);

    /// <summary>[APPROX] Gastronomie-Umsatz je Zuschauer in DM.</summary>
    public int GastronomyPerSpectator => Level(StadiumFeature.Gastronomie) switch
    {
        1 => 2,
        2 => 4,
        3 => 6,
        _ => 0,
    };

    /// <summary>[APPROX] Unterhalt pro Monat: Sockel, Plätze, Ausbauten; Kino und Bowling bringen etwas ein.</summary>
    public long MonthlyUpkeep =>
        3_000 + Capacity * 6L / 10
        + 2_000L * (Level(StadiumFeature.Ueberdachung) + Level(StadiumFeature.Anzeigetafel) + Level(StadiumFeature.Kuehlanlage))
        - 6_000L * (Level(StadiumFeature.Kino) + Level(StadiumFeature.Bowlingbahn));
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
