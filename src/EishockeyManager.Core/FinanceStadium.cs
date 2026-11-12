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
/// Laufende oder eben fertige Baustelle (Ansicht, nicht gespeichert). <see cref="Target"/> = Zielstufe des Bereichs bzw.
/// Platzzahl nach dem Bau.
/// </summary>
public sealed record StadiumConstruction(StadiumProject Project, StadiumFeature Feature, int Target, int WeeksLeft)
{
    /// <summary>Bezeichnung wie im Bildschirm („Kino“, „Sitzplätze“).</summary>
    public string Label => Project switch
    {
        StadiumProject.Sitzplaetze => "Sitzplätze",
        StadiumProject.Stehplaetze => "Stehplätze",
        _ => Stadium.Features[(int)Feature].Label,
    };
}

/// <summary>
/// Angebot der Baufirma nach Klick auf „ausbauen“ (Frage „Möchten Sie Ihr Stadion ausbauen ?“). <see cref="Target"/> =
/// Zielstufe bzw. Zahl der 500er-Schritte, <see cref="Cost"/> vor Rabatt, <see cref="Weeks"/> = angezeigte Bauzeit („ca.“).
/// </summary>
public sealed record StadiumQuote(StadiumProject Project, StadiumFeature Feature, int Target, long Cost, int Weeks, int Discount)
{
    /// <summary>[ORIGINAL] Datei 0x3E389 ff.: Kosten −= Kosten · r / 100 (ganzzahlig).</summary>
    public long DiscountedCost => Cost - Cost * Discount / 100;
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

    /// <summary>Projektnummer der Sitz- und Stehplätze (nach den 9 Bereichen), wie im Original 0–10.</summary>
    public const int SeatsProject = 9;
    public const int StandingProject = 10;
    public const int ProjectCount = 11;

    /// <summary>[ORIGINAL] Datei 0x3DEB4–0x3DF1E: Plätze in Schritten zu 500, Sitz- und Stehplätze zusammen höchstens 25.000.</summary>
    public const int PlaceStep = 500;
    public const int MaxPlaces = 25_000;
    /// <summary>[ORIGINAL] em.exe 0x3DDB8–0x3DDC5: neuer Preis nur bei 9 &lt; Preis &lt; 31 (unabhängige Nachprüfung, Runde 5).</summary>
    public const int MinTicketPrice = 10;
    public const int MaxTicketPrice = 30;

    private readonly int[] _levels = new int[FeatureCount];

    /// <summary>Zielstufe je Bereich im Bau, 0 = kein Bau. [ORIGINAL] Stadion-Datensatz +2·i+1 (re-geld.md, „Datensatz“).</summary>
    private readonly int[] _targets = new int[FeatureCount];

    /// <summary>Restwochen je Projekt 0–10, 0 = keine Baustelle. [ORIGINAL] Stadion-Datensatz +0x12 + p.</summary>
    private readonly int[] _weeksLeft = new int[ProjectCount];

    /// <summary>
    /// „Die Baufirma ist noch beleidigt...“ je Projekt. [ORIGINAL] DGROUP 0xCC09 + 11 · Manager + p: gesetzt beim Angebot
    /// (Datei 0x3E1FE ff.), gelöscht bei „Ja“ oder im Tagesabschluss (Datei 0x3ED19–0x3ED48).
    /// </summary>
    private readonly bool[] _offended = new bool[ProjectCount];

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

    /// <summary>Sitz- bzw. Stehplätze nach dem Bau, 0 = kein Bau. [ORIGINAL] Stadion-Datensatz +0x1F + 4·j.</summary>
    public int SeatsTarget { get; set; }
    public int StandingTarget { get; set; }

    public int Level(StadiumFeature feature) => _levels[(int)feature];

    public void SetLevel(StadiumFeature feature, int level) =>
        _levels[(int)feature] = Math.Clamp(level, 0, Features[(int)feature].Levels.Length - 1);

    public string LevelText(StadiumFeature feature) => Features[(int)feature].Levels[Level(feature)];

    public bool IsMaxed(StadiumFeature feature) => Level(feature) >= Features[(int)feature].Levels.Length - 1;

    /// <summary>
    /// Startstadion je Liga. [ORIGINAL] Spielstart em.exe 0x29F77 ff.: 3.000 − 500 × Liga Sitzplätze, 4.500 − 1.000 × Liga
    /// Stehplätze (Liga 0 = Bundesliga); Überdachung 2 − Liga, Anzeigetafel/Kühlanlage/Verkehrsanbindung nur in der
    /// Bundesliga, Gastronomie (Imbißbude) in Bundesliga und 2. Liga, Zustand MITTEL. Video (Oberliga): 2.000/2.500.
    /// [ORIGINAL] 0x29FB3–0x29FCD: Bei einem Spiel über eine Saison (Spielende 1994) +1.200 Sitz- und +2.500 Stehplätze
    /// (unabhängige Nachprüfung, Runde 5).
    /// </summary>
    public static Stadium ForLeague(League? league, bool oneSeasonGame = false)
    {
        var tier = league is { } l ? (int)l : 2;
        var stadium = new Stadium { Seats = 3_000 - 500 * tier, Standing = 4_500 - 1_000 * tier, TicketPrice = (40 - 4 * tier) / 3 };
        if (oneSeasonGame)
        {
            stadium.Seats += 1_200;
            stadium.Standing += 2_500;
        }
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
    /// Kosten je Stufe bzw. je 500 Plätze in DM. [ORIGINAL] DGROUP 0xC0 (11 Langwörter in Drittel-DM: 3.600.000,
    /// 1.350.000, 1.920.000, 1.110.000, 990.000, 3.900.000, 1.740.000, 1.560.000, 1.350.000, 540.000, 405.000), hier durch 3.
    /// Alle Werte sind Vielfache von 10.000 DM, Rabatt und Drittelung bleiben daher ganzzahlig wie im Original.
    /// Video (Oberliga, Spielbeginn): Kühlanlage 640.000, Imbißbude 580.000, Kino 520.000, Bowlingbahn 450.000 DM bestätigt;
    /// „Straße 1.200.000 DM“ weicht von 1.300.000 ab (Videostelle offen, re-geld.md „Abgleich mit dem Remake“).
    /// </summary>
    private static readonly long[] StepCosts =
        [1_200_000, 450_000, 640_000, 370_000, 330_000, 1_300_000, 580_000, 520_000, 450_000, 180_000, 135_000];

    /// <summary>
    /// Bauzeit `random(lo, hi)` in Wochen je Projekt. [ORIGINAL] Bytes DGROUP 0xEC + 2·p (lo) und 0xED + 2·p (hi),
    /// gelesen in Datei 0x3E252–0x3E270.
    /// </summary>
    private static readonly (int Lo, int Hi)[] BuildWeeks =
        [(20, 30), (12, 17), (16, 25), (6, 9), (5, 8), (40, 60), (17, 24), (14, 18), (10, 15), (12, 17), (10, 14)];

    /// <summary>
    /// Ziehungen des Angebots in der Reihenfolge des Originals: Bauzeit w = random(lo, hi), Rabatt r = random(0, 10), mit
    /// random(0, 2) = 0 dagegen 0 (Rabatt 0 in 1/3 + 2/3 · 1/11 der Fälle). [ORIGINAL] Datei 0x3E252–0x3E30B.
    /// </summary>
    public static (int Weeks, int Discount) DrawOffer(Random random, StadiumProject project, StadiumFeature feature)
    {
        var (lo, hi) = BuildTime(project, feature);
        var weeks = SquadSetup.Between(random, lo, hi);
        var discount = SquadSetup.Between(random, 0, 10);
        if (SquadSetup.Between(random, 0, 2) == 0)
            discount = 0;
        return (weeks, discount);
    }

    /// <summary>Restwochen nach „Ja“: w + random(0, 4) − 2 (die angezeigte Zeit ist „ca.“). [ORIGINAL] Datei 0x3E3F4–0x3E3FB.</summary>
    public static int DrawWeeksLeft(Random random, int weeks) => weeks + SquadSetup.Between(random, 0, 4) - 2;

    /// <summary>Projektnummer 0–10 wie im Original.</summary>
    public static int ProjectIndex(StadiumProject project, StadiumFeature feature) => project switch
    {
        StadiumProject.Sitzplaetze => SeatsProject,
        StadiumProject.Stehplaetze => StandingProject,
        _ => (int)feature,
    };

    /// <summary>Kosten je Stufe bzw. je 500 Plätze (DM).</summary>
    public static long StepCost(StadiumProject project, StadiumFeature feature) => StepCosts[ProjectIndex(project, feature)];

    /// <summary>Spanne der Bauzeit in Wochen.</summary>
    public static (int Lo, int Hi) BuildTime(StadiumProject project, StadiumFeature feature) =>
        BuildWeeks[ProjectIndex(project, feature)];

    /// <summary>Restwochen eines Projekts (0 = keine Baustelle).</summary>
    public int WeeksLeft(StadiumProject project, StadiumFeature feature) => _weeksLeft[ProjectIndex(project, feature)];

    /// <summary>Zielstufe eines Bereichs im Bau (0 = kein Bau).</summary>
    public int TargetLevel(StadiumFeature feature) => _targets[(int)feature];

    /// <summary>Baufirma nach „Nein“ beleidigt (Projekt gesperrt, bis der Tagesabschluss es freigibt).</summary>
    public bool IsOffended(StadiumProject project, StadiumFeature feature) => _offended[ProjectIndex(project, feature)];

    internal void SetOffended(int project, bool value) => _offended[project] = value;

    /// <summary>[ORIGINAL] Datei 0x3DF1F–0x3DF81: Projekt im Bau, wenn Zielstufe bzw. Zielplätze ≠ 0.</summary>
    public bool IsUnderConstruction(StadiumProject project, StadiumFeature feature) => project switch
    {
        StadiumProject.Sitzplaetze => SeatsTarget != 0,
        StadiumProject.Stehplaetze => StandingTarget != 0,
        _ => _targets[(int)feature] != 0,
    };

    /// <summary>Alle laufenden Baustellen in Projektreihenfolge (mehrere zugleich, je Projekt höchstens eine).</summary>
    public IEnumerable<StadiumConstruction> Constructions
    {
        get
        {
            for (var i = 0; i < FeatureCount; i++)
                if (_targets[i] != 0)
                    yield return new StadiumConstruction(StadiumProject.Feature, (StadiumFeature)i, _targets[i], _weeksLeft[i]);
            if (SeatsTarget != 0)
                yield return new StadiumConstruction(StadiumProject.Sitzplaetze, default, SeatsTarget, _weeksLeft[SeatsProject]);
            if (StandingTarget != 0)
                yield return new StadiumConstruction(StadiumProject.Stehplaetze, default, StandingTarget, _weeksLeft[StandingProject]);
        }
    }

    /// <summary>
    /// Höchstes Ziel: Bereich = höchste Stufe; Plätze = Zahl der 500er-Schritte bis 25.000 zusammen, wobei eine Platzart im
    /// Bau mit ihrer Zielzahl zählt. [ORIGINAL] Datei 0x3DEB4–0x3DF1E.
    /// </summary>
    public int MaxTarget(StadiumProject project, StadiumFeature feature)
    {
        if (project == StadiumProject.Feature)
            return Features[(int)feature].Levels.Length - 1;
        var seats = SeatsTarget != 0 ? SeatsTarget : Seats;
        var standing = StandingTarget != 0 ? StandingTarget : Standing;
        return Math.Max(0, (MaxPlaces - seats - standing) / PlaceStep);
    }

    /// <summary>
    /// Projekt wählbar: nicht im Bau, noch Luft nach oben und Baufirma nicht beleidigt. [ORIGINAL] Datei 0x3DE71–0x3DF81.
    /// </summary>
    public bool CanSelect(StadiumProject project, StadiumFeature feature) =>
        !IsOffended(project, feature) && !IsUnderConstruction(project, feature) && FirstTarget(project, feature) <= MaxTarget(project, feature);

    /// <summary>Erstes Ziel der Stufenwahl: jetzige Stufe + 1 bzw. ein Schritt (+500 Plätze). [ORIGINAL] Datei 0x3D7F8, re-geld.md „Ausbau“ Schritt 4.</summary>
    public int FirstTarget(StadiumProject project, StadiumFeature feature) =>
        project == StadiumProject.Feature ? Level(feature) + 1 : 1;

    /// <summary>
    /// Kosten bis zum Ziel: (Ziel − jetzt) · K. Bei Plätzen ist <paramref name="target"/> die Zahl der 500er-Schritte.
    /// Null, wenn das Ziel außerhalb liegt. [ORIGINAL] Datei 0x3D7F8, re-geld.md „Ausbau“ Schritt 4
    /// (linear, keine Steigerung je Stufe; Konto-Prüfung Datei 0x3E107–0x3E137).
    /// </summary>
    public long? Cost(StadiumProject project, StadiumFeature feature, int target)
    {
        if (target < FirstTarget(project, feature) || target > MaxTarget(project, feature))
            return null;
        var steps = project == StadiumProject.Feature ? target - Level(feature) : target;
        return steps * StepCost(project, feature);
    }

    /// <summary>Platzzahl nach <paramref name="steps"/> Schritten.</summary>
    public int PlacesAfter(StadiumProject project, int steps) =>
        (project == StadiumProject.Sitzplaetze ? Seats : Standing) + steps * PlaceStep;

    /// <summary>Trägt eine Baustelle ein. [ORIGINAL] Datei 0x3E389–0x3E47F.</summary>
    internal void Begin(StadiumProject project, StadiumFeature feature, int target, int weeks)
    {
        switch (project)
        {
            case StadiumProject.Sitzplaetze: SeatsTarget = PlacesAfter(project, target); break;
            case StadiumProject.Stehplaetze: StandingTarget = PlacesAfter(project, target); break;
            default: _targets[(int)feature] = target; break;
        }
        _weeksLeft[ProjectIndex(project, feature)] = weeks;
    }

    /// <summary>
    /// Baustelle aus einem Original-Spielstand (Spur hok-import, R14HI-02): Zielstufe (Manager-Satz +0x117 + 2 · Bereich) und
    /// Restwochen (+0x128 + Projekt) unverändert übernehmen; Plätze im Bau stehen schon in <see cref="SeatsTarget"/>/<see cref="StandingTarget"/>.
    /// </summary>
    internal void ImportConstruction(int project, int target, int weeks)
    {
        if (project < FeatureCount)
            _targets[project] = target;
        _weeksLeft[project] = weeks;
    }

    /// <summary>
    /// Eine Bauwoche: Restwochen − 1 je Baustelle; bei 0 Stufe = Zielstufe bzw. Plätze = Zielplätze. Liefert die fertigen
    /// Projekte in Projektreihenfolge. [ORIGINAL] Datei 0x3D316.
    /// </summary>
    public IReadOnlyList<StadiumConstruction> AdvanceWeek()
    {
        var done = new List<StadiumConstruction>();
        for (var p = 0; p < ProjectCount; p++)
        {
            if (_weeksLeft[p] <= 0 || --_weeksLeft[p] > 0)
                continue;
            if (p < FeatureCount)
            {
                // [APPROX] Ohne Ziel (Restwochen ohne Baustelle, im Original nicht erreichbar) bleibt die Stufe.
                if (_targets[p] == 0)
                    continue;
                _levels[p] = _targets[p];
                done.Add(new StadiumConstruction(StadiumProject.Feature, (StadiumFeature)p, _targets[p], 0));
                _targets[p] = 0;
            }
            else if (p == SeatsProject && SeatsTarget != 0)
            {
                Seats = SeatsTarget;
                done.Add(new StadiumConstruction(StadiumProject.Sitzplaetze, default, SeatsTarget, 0));
                SeatsTarget = 0;
            }
            else if (p == StandingProject && StandingTarget != 0)
            {
                Standing = StandingTarget;
                done.Add(new StadiumConstruction(StadiumProject.Stehplaetze, default, StandingTarget, 0));
                StandingTarget = 0;
            }
        }
        return done;
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
    /// Liga 0, aus niedrigerer 11), Ligen 0 = Bundesliga, <paramref name="levelSteps"/> = 5 − Level.
    /// <paramref name="importance"/> = Spielbedeutung 0–2. [ORIGINAL] 0x545CD–0x54601: [Code 6] + 2·[Code 3] + [Code 1]
    /// mit dem Wettbewerbscode DGROUP 0xD0C2 (Spur heimspiel-statistik): Code 6 DEB-Pokal (0x35E7C, Schalter DEB-Pokal
    /// 0x227) → 1; Code 1 Play-Off-/Aufstiegsrunden (0x31AB7, Gruppen aus DGROUP 0xCF20) → 1; Code 3 Europapokal-Gruppen
    /// → 2, Code 5 Europapokal-Endrunde → 0 (<see cref="EuropeanCupImportance"/>); Code 7 Liga (0x35EC4), 8/10 WM
    /// (0x30F53/0x31017) → 0.
    /// </summary>
    public static int Compute(Stadium stadium, int homeStrength, int awayStrength, int homeRank, int awayRank,
        int homeLeague, int awayLeague, int importance, int levelSteps, Random random)
    {
        // [ORIGINAL] Datei 0xC037 (0x38:0x847 in finanzen-rekonstruktion.md): random(a, b) mit b < a tauscht die Grenzen (finanzen-rekonstruktion.md „Zufall“; Fall negativer Preisfaktor K).
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
            // [ORIGINAL] 0x3F63C–0x3F65B: geteilt wird nur bei teuren Karten (unabhängige Nachprüfung, Runde 5).
            if (16 + 3 * (2 - homeLeague) < halfPrice3)
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

    /// <summary>
    /// Spielbedeutung eines Europapokaltermins (0–11) für <see cref="Compute"/>. [ORIGINAL] Europapokal 0x31490–0x314A1:
    /// Wettbewerbscode 3 für die Gruppentermine 0–5, Code 5 für die Endrunde ([0x7AF] &gt; 5); Bonus nur für Code 3
    /// (0x545DC). Vorher bekam auch die Endrunde den Bonus 2.
    /// </summary>
    public static int EuropeanCupImportance(int date) => date < EuropeanCup.GroupDates ? 2 : 0;

    /// <summary>Stärke eines Vereins für die Zuschauerformel: Mittel der sechs Vereinsfelder 0x18–0x1D (Kondition, Technik, Form von Abwehr und Sturm), nicht der Spielerwerte. [ORIGINAL] 0x4D316; Computervereine Lader/Wanderung, Manager Aufstellung (0x952C), siehe <see cref="StrengthTable"/>.</summary>
    public static int Strength(GameState game, Club club)
    {
        var (d, a) = StrengthTable.TeamValues(game, club);
        return (int)((long)d.C + (long)d.T + (long)d.F + (long)a.C + (long)a.T + (long)a.F) / 6;
    }
}

public sealed partial class GameState
{
    /// <summary>
    /// Liga-Code des Originals (0 = Bundesliga, 1 = 2. Liga, 2 = Oberliga). A045 (Spur wettbewerbe-rest, Teil 2): [ORIGINAL] Die
    /// Spielabrechnung übergibt 0x34FD5(Verein) / 2 (Datei 0x54515–0x54534 → 0x3F185, Argumente 2/3, Vergleich Datei 0x3F1AF):
    /// Index &lt; 12 → 1, &lt; 24 → 2, bis DGROUP 0x6AD = 52 → 4, darüber 1 (<see cref="CupClass.Of"/>). Europapokal-Gegner (64–149)
    /// und Nationalteams ab 53 zählen also wie die Bundesliga, Vereine ohne Liga bis 52 wie die Oberliga (bis Runde 16 alle
    /// ligalosen wie die Bundesliga).
    /// </summary>
    private static int LeagueCode(Club club) => CupClass.Of(club) >> 1;

    /// <summary>
    /// Tabellenplatz 0–11 vor dem Spieltag. [ORIGINAL] A046 (Runde 14): Die Zuschauerformel liest das Tabellenfeld DGROUP
    /// 0x8288 + 0x50 · Verein (Datei 0x54608–0x5464A, <see cref="Club.StoredTableRank"/>). Ab dem 2. Spieltag steht dort der
    /// Platz nach dem letzten Ligaspieltag – das Remake rechnet ihn aus der Tabelle; vor dem 1. Spieltag 0 im ersten Spieljahr
    /// bzw. der Endstand der Vorsaison. Ohne Liga 0.
    /// </summary>
    private int TableRank(Club club, int matchDay)
    {
        if (club.League is not { } league)
            return 0;
        if (matchDay <= 1)
            return club.StoredTableRank;
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
    /// Klick auf „ausbauen“: Angebot der Baufirma mit Bauzeit und Rabatt. Nur bei wählbarem Projekt, Ziel über dem
    /// jetzigen Stand und Konto ≥ Kosten; sonst null ohne Zufall. Setzt „beleidigt“, bis die Antwort „Ja“ lautet.
    /// [ORIGINAL] Datei 0x3E1FE ff.: w = random(lo, hi) einmal (nicht je Stufe), Merker DGROUP 0xCC09 = 1, Rabatt
    /// r = random(0, 10), danach mit random(0, 2) = 0 dagegen 0 – Reihenfolge der Ziehungen so.
    /// [APPROX] Zufallsquelle ist <see cref="FinanceRandom"/>; das Original hat einen gemeinsamen Generator.
    /// </summary>
    public StadiumQuote? QuoteConstruction(Manager manager, StadiumProject project, StadiumFeature feature, int target)
    {
        var stadium = Finance(manager).Stadium;
        if (!stadium.CanSelect(project, feature) || stadium.Cost(project, feature, target) is not { } cost || manager.Money < cost)
            return null;
        var (weeks, discount) = Stadium.DrawOffer(FinanceRandom, project, feature);
        stadium.SetOffended(Stadium.ProjectIndex(project, feature), true);
        return new StadiumQuote(project, feature, target, cost, weeks, discount);
    }

    /// <summary>
    /// Antwort auf „Möchten Sie Ihr Stadion ausbauen ?“. Ja: Merker aus, Konto −= Kosten nach Rabatt, Restwochen =
    /// w + random(0, 4) − 2, Ziel eintragen. Nein: keine Baustelle, die Baufirma bleibt beleidigt.
    /// [ORIGINAL] Datei 0x3E389–0x3E47F (Ja) und 0x3E482 (Nein). Buchung im Finanzbuch „Bau“ nach Entscheidung E1
    /// (re-geld.md); [APPROX] das Original zieht den Betrag ohne Kategorie direkt vom Konto ab – Kontostand und Saldo gleich.
    /// Liefert false, wenn sich der Stand seit dem Angebot geändert hat (Projekt nicht mehr frei oder Kosten anders);
    /// das Konto wird hier nicht mehr geprüft. [ORIGINAL] Datei 0x3E389 ff. zieht bei „Ja“ ohne erneute Kontoprüfung ab (die
    /// Prüfung steht nur am Angebot, 0x3E107); im Bildschirm ist die Rückfrage modal, das Konto ändert sich dazwischen nicht.
    /// </summary>
    public bool AnswerConstruction(Manager manager, StadiumQuote quote, bool accept)
    {
        if (!accept)
            return false;
        var stadium = Finance(manager).Stadium;
        var project = Stadium.ProjectIndex(quote.Project, quote.Feature);
        if (stadium.IsUnderConstruction(quote.Project, quote.Feature) || stadium.Cost(quote.Project, quote.Feature, quote.Target) != quote.Cost)
            return false;
        stadium.SetOffended(project, false);
        Book(manager, FinanceCategory.Bau, -quote.DiscountedCost);
        stadium.Begin(quote.Project, quote.Feature, quote.Target, Stadium.DrawWeeksLeft(FinanceRandom, quote.Weeks));
        return true;
    }

    /// <summary>Angebot einholen und sofort annehmen (Abläufe, Tests). Liefert false, wenn kein Angebot zustande kommt.</summary>
    public bool StartConstruction(Manager manager, StadiumProject project, StadiumFeature feature, int target) =>
        QuoteConstruction(manager, project, feature, target) is { } quote && AnswerConstruction(manager, quote, accept: true);

    /// <summary>
    /// Tagesabschluss: „beleidigte“ Baufirmen je Projekt mit random(0, 10) = 0 wieder frei (im Mittel nach 11 Tagen).
    /// [ORIGINAL] Datei 0x3ED19–0x3ED48. [APPROX] Das Original zieht für alle 11 Projekte jeden Tag; hier nur für
    /// gesetzte Merker, damit der gemeinsame Finanzzufall sonst unverändert bleibt (Verteilung gleich).
    /// </summary>
    private void ForgiveBuilders(Manager manager)
    {
        var stadium = Finance(manager).Stadium;
        for (var p = 0; p < Stadium.ProjectCount; p++)
        {
            var (project, feature) = p switch
            {
                Stadium.SeatsProject => (StadiumProject.Sitzplaetze, default(StadiumFeature)),
                Stadium.StandingProject => (StadiumProject.Stehplaetze, default),
                _ => (StadiumProject.Feature, (StadiumFeature)p),
            };
            if (stadium.IsOffended(project, feature) && SquadSetup.Between(FinanceRandom, 0, 10) == 0)
                stadium.SetOffended(p, false);
        }
    }

    /// <summary>Tagesabschluss aller Manager für das Stadion (<see cref="ForgiveBuilders"/>).</summary>
    private void DailyStadium()
    {
        foreach (var manager in Managers)
            ForgiveBuilders(manager);
    }

    /// <summary>Wöchentlich: Baufortschritt. Liefert die abgeschlossenen Bauten (je eine Meldung im Posteingang).</summary>
    private IReadOnlyList<StadiumConstruction> AdvanceConstruction(Manager manager) => Finance(manager).Stadium.AdvanceWeek();
}
