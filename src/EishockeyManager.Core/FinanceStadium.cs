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

/// <summary>Zuschauerformel des Originals (Spur finanzen-2). [ORIGINAL] em.exe Datei 0x3F185 ff. (Einsprung 0x100:0x52).</summary>
public static class SpectatorRules
{
    /// <summary>Grundzuschauer je Liga (DGROUP 0x4FD0): Bundesliga, 2. Liga, Oberliga.</summary>
    private static readonly int[] LeagueBase = [4800, 3250, 1200];

    /// <summary>Spanne je Ausbaustufe (DGROUP 0x4FE0 / 0x4FF2), skaliert mit (110 − 2 × Tabellenplatz) / 60.</summary>
    private static readonly int[] FeatureLow = [350, 100, 60, 30, 30, 80, 50, 50, 50];
    private static readonly int[] FeatureHigh = [450, 150, 100, 55, 80, 190, 80, 90, 90];

    /// <summary>
    /// Zuschauer eines Heimspiels. Stärken = Mittel der sechs Teamwerte (0x128:0x25), Plätze 0–11 (Gast aus höherer
    /// Liga 0, aus niedrigerer 11), Ligen 0 = Bundesliga, <paramref name="importance"/> 0 Liga/Freundschaft, 1 DEB-Pokal
    /// und Play-Off/Aufstieg, 2 Europapokal (Wettbewerbscode 0xD0C2), <paramref name="levelSteps"/> = 5 − Level.
    /// </summary>
    public static int Compute(Stadium stadium, int homeStrength, int awayStrength, int homeRank, int awayRank,
        int homeLeague, int awayLeague, int importance, int levelSteps, Random random)
    {
        // [APPROX] random(a, b) mit a > b (negativer Preisfaktor K) ist im Original nicht belegt; hier werden die Grenzen getauscht.
        int Rnd(int a, int b) => a <= b ? SquadSetup.Between(random, a, b) : SquadSetup.Between(random, b, a);
        var hs = homeStrength + 17;
        var @as = awayStrength + 17;
        if (homeLeague < awayLeague) awayRank = 11;
        else if (homeLeague > awayLeague) awayRank = 0;
        if (homeRank is < 0 or > 11) homeRank = 5;
        if (awayRank is < 0 or > 11) awayRank = 5;

        var price = stadium.TicketPrice;
        var halfPrice3 = price * 3 / 2;
        var f = ((5 * hs / 4 + 5 * @as / 4) / 2 + 200 * (22 - homeRank - awayRank / 2) / 22) / 2;
        long z = 70L * f + LeagueBase[homeLeague];
        if (homeLeague > awayLeague) z += 3500 * (homeLeague - awayLeague);
        else if (homeLeague < awayLeague) z -= 800 * (awayLeague - homeLeague);
        var g = 110 - 2 * homeRank;
        for (var i = 0; i < Stadium.FeatureCount; i++)
        {
            var level = stadium.Level((StadiumFeature)i);
            if (level > 0)
                z += level * Rnd(g * FeatureLow[i] / 60, g * FeatureHigh[i] / 60);
        }
        z += 500 * levelSteps;
        if (stadium.Standing < stadium.Seats + 2000)
            z += Rnd(1400, 1800);
        var capacity = stadium.Capacity;
        importance = Math.Min(importance, 3);
        z = z * (100 + 20 * importance) / 100;

        var h = Math.Min(90, (hs + @as + 30) / 2);
        long k = 100 - 5 * halfPrice3;
        if (k < -30) k += k / 2;
        else if (k < 80) k -= Rnd((int)(k / 5), (int)(k / 2));
        z = z * (100 + k * (100 - 9 * h / 10) / 50) / 100;
        z -= 9000;

        var tiny = false;
        if (z < Rnd(150, 200))
        {
            if (16 + 3 * (2 - homeLeague) > halfPrice3)
            {
                z /= Rnd(2, 5);
                tiny = true;
            }
        }
        if (!tiny)
            while (z < Rnd(200, 300))
                z += 50;

        if (homeLeague == 0 && stadium.Level(StadiumFeature.Ueberdachung) < 2)
            z = z * (50 + 25 * stadium.Level(StadiumFeature.Ueberdachung)) / 100;
        if (homeLeague == 0 && stadium.Level(StadiumFeature.Kuehlanlage) == 0)
            z = z * 2 / 3;
        z = z * (93 + 3 * stadium.Level(StadiumFeature.Zustand)) / 100;
        z = z * (95 + 5 * stadium.Level(StadiumFeature.Sitzkomfort)) / 100;
        if (z < 50) z = Rnd(50, 100);
        return (int)Math.Min(z, capacity);
    }

    /// <summary>Stärke eines Vereins für die Zuschauerformel: Mittel der sechs Teamwerte. [ORIGINAL] 0x4D316.</summary>
    public static int Strength(GameState game, Club club)
    {
        var (d, a) = StrengthTable.TeamValues(game, club);
        return (int)((long)d.C + (long)d.T + (long)d.F + (long)a.C + (long)a.T + (long)a.F) / 6;
    }
}

public sealed partial class GameState
{
    /// <summary>Liga-Code des Originals (0 = Bundesliga). [APPROX] Vereine ohne Liga (Europapokal) wie Bundesliga.</summary>
    private static int LeagueCode(Club club) => club.League is { } l ? (int)l : 0;

    /// <summary>
    /// Tabellenplatz 0–11 vor dem Spieltag (Vortabelle wie bisher). [APPROX] Vor dem ersten Spieltag die Reihenfolge
    /// der Liga, ohne Liga 0; welchen Platz das Original dann einsetzt, ist nicht belegt.
    /// </summary>
    private int TableRank(Club club, int matchDay)
    {
        if (club.League is not { } league)
            return 0;
        if (matchDay <= 1)
            return Math.Max(0, LeagueClubs(league).ToList().IndexOf(club));
        var row = LeagueTable.Compute(this, league, TableView.Gesamt, matchDay - 1).FirstOrDefault(r => r.Club.Index == club.Index);
        return row is null ? 5 : row.Rank - 1;
    }

    /// <summary>
    /// Zuschauer eines Heimspiels nach der Formel des Originals (<see cref="SpectatorRules.Compute"/>).
    /// </summary>
    public int Spectators(Club home, Club away, Stadium stadium, int matchDay, Random random, int importance = 0, int level = 3) =>
        SpectatorRules.Compute(stadium, SpectatorRules.Strength(this, home), SpectatorRules.Strength(this, away),
            TableRank(home, matchDay), TableRank(away, matchDay), LeagueCode(home), LeagueCode(away), importance,
            ManagerSetup.LevelCount - 1 - level, random);
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
