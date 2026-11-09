using EishockeyManager.Formats;

namespace EishockeyManager.Core;

/// <summary>
/// Trainingsarten in der Reihenfolge der Leiste unten in kb\5 (Bildchen 29 × 20 bei x = 26 + 30·Art, y = 218).
/// Das Original zeigt nur Bildchen; die Namen hier sind nach den Bildchen gewählt. Wirkungen: siehe
/// <see cref="TrainingRules.Load"/>.
/// </summary>
public enum TrainingKind : byte
{
    /// <summary>Pinguin vor jubelnder Kulisse – das einzige Training, das Geld kostet; steigert die Wirkung der Woche.</summary>
    Special = 0,

    /// <summary>Strand – frei.</summary>
    Free = 1,

    /// <summary>Tor mit Puck und kleinem Schläger.</summary>
    GoalDrill = 2,

    /// <summary>Hantel – Kraft (Kondition).</summary>
    Weights = 3,

    /// <summary>Kreisende Pfeile vor dem Tor – Spielzüge (Technik).</summary>
    Combinations = 4,

    /// <summary>Spielfeld mit Mittelkreis.</summary>
    Scrimmage = 5,

    /// <summary>Tor mit großem Schläger – Torschuss.</summary>
    Shooting = 6,

    /// <summary>Schlittschuh – Eislaufen (Kondition).</summary>
    Skating = 7,

    /// <summary>Bande mit Bögen.</summary>
    Boards = 8,
}

/// <summary>
/// Trainingsplan eines Managers: 7 Tage (Mo–So) × 4 Einheiten. Auf dem Bildschirm (kb\5) stehen die Einheiten
/// 0/1 im oberen Kasten eines Tages, 2/3 im unteren. [ORIGINAL] Manager-Feld DGROUP 0xA8A1 (28 Byte je Manager),
/// Aufbau, Kosten und Voreinstellung aus em.exe (siehe docs/reverse-engineering.md, Abschnitt Training).
/// </summary>
public sealed class TrainingPlan
{
    public const int Days = 7;
    public const int UnitsPerDay = 4;
    public const int Kinds = 9;

    /// <summary>Spieltage des Originals (rot in kb\5): Mittwoch und Sonntag.</summary>
    public static readonly int[] MatchDays = [2, 6];

    private readonly TrainingKind[] _units = new TrainingKind[Days * UnitsPerDay];

    public TrainingKind this[int day, int unit]
    {
        get => _units[day * UnitsPerDay + unit];
        set => _units[day * UnitsPerDay + unit] = value;
    }

    /// <summary>
    /// Preisstufe 0–2: Liga des Vereins bei Spielbeginn (1. Liga 0, 2. Liga 1, Oberliga 2). [ORIGINAL] Im Original
    /// beim Spielstart aus dem Vereinsindex gesetzt (≥ 12 und ≥ 24) und danach nicht mehr verändert.
    /// </summary>
    public int PriceTier
    {
        get => _priceTier;
        set
        {
            _priceTier = value;
            PriceTierSet = true;
        }
    }

    private int _priceTier;

    /// <summary>Wurde die Preisstufe schon festgelegt (beim ersten Vorbereiten)?</summary>
    public bool PriceTierSet { get; private set; }

    /// <summary>
    /// Preisindex in Prozent. [ORIGINAL] DGROUP 0x2B8, Startwert 100; außer Laden/Speichern schreibt ihn nichts, er bleibt 100
    /// (Leser Datei 0x3E811, Datei 0x59DBF; docs/analyse/re-nachtrag.md Abschnitt 5, A183).
    /// </summary>
    public int PriceIndex { get; set; } = 100;

    /// <summary>
    /// Alle Einheiten 0 (leerer Speicher im Original)? Dann füllt <see cref="Prepare"/> den Standardplan – wie im
    /// Original auch, wenn jemand alle 28 Einheiten mit Art 0 belegt.
    /// </summary>
    public bool IsEmpty => _units.All(u => u == 0);

    public void Fill(TrainingKind kind) => Array.Fill(_units, kind);

    /// <summary>Wie oft eine Art im Plan steht.</summary>
    public int Count(TrainingKind kind) => _units.Count(u => u == kind);

    /// <summary>
    /// Vorbereitung beim Zug eines Managers. [ORIGINAL] Funktion 46E:0653: bei Level 5 (Wert 5 − Level = 0) ist
    /// immer alles frei; sonst wird ein leerer Plan mit dem Standardplan gefüllt.
    /// </summary>
    public void Prepare(int level)
    {
        if (TrainingRules.LevelValue(level) == 0)
        {
            Fill(TrainingKind.Free);
            return;
        }
        if (!IsEmpty)
            return;

        // Arten 2–8 reihum, die Hantel (3) die ersten beiden Male übersprungen; danach Erholung an den Spieltagen
        // (Mi, So je ab der zweiten Einheit) und am Samstag nachmittags.
        var kind = 2;
        var skip = 2;
        for (var day = 0; day < Days; day++)
        {
            for (var unit = 0; unit < UnitsPerDay; unit++)
            {
                this[day, unit] = (TrainingKind)kind;
                kind++;
                if (kind == 3 && skip > 0)
                {
                    skip--;
                    kind++;
                }
                if (kind == 9)
                    kind = 2;
                foreach (var (d, u) in DefaultRest)
                    if (d == day && u == unit)
                        this[day, unit] = TrainingKind.Free;
            }
        }
    }

    /// <summary>Freie Einheiten des Standardplans (Tag, Einheit). [ORIGINAL] DGROUP 0x48EB.</summary>
    private static readonly (int Day, int Unit)[] DefaultRest = [(2, 1), (2, 2), (2, 3), (6, 1), (6, 2), (6, 3), (5, 2), (5, 3)];

    /// <summary>Kosten je Einheit „Special“ in internen Einheiten (1/3 DM). [ORIGINAL] 12.000 − 1.200 × Preisstufe.</summary>
    public long SpecialUnitCost => (12000L - 1200L * PriceTier) * PriceIndex / 100;

    /// <summary>Grundgebühr, sobald „Special“ im Plan steht (1/3 DM). [ORIGINAL] 21.000 − 6.000 × Preisstufe.</summary>
    public long SpecialBaseCost => 21000L - 6000L * PriceTier;

    /// <summary>
    /// Kosten je Trainingstag (zweimal pro Woche) in internen Einheiten des Originals. [ORIGINAL] Manager-Feld 0x97EE,
    /// beim Verlassen des Bildschirms gespeichert und an jedem Trainingstag vom Konto abgezogen.
    /// </summary>
    public long RawCostPerTrainingDay
    {
        get
        {
            var special = Count(TrainingKind.Special) * SpecialUnitCost;
            return special == 0 ? 0 : special + SpecialBaseCost;
        }
    }

    /// <summary>
    /// Geldbeträge rechnet das Original intern in Drittel-DM: die Ausgabefunktion (99F:02D8) teilt durch 3
    /// (DGROUP 0x79D = 3, 0x7A1 = 1). Belegt im UI-Video (Oberliga): eine Einheit „Special“ → 12.400 DM,
    /// zwei → 18.800 DM pro Woche.
    /// </summary>
    public const int InternalUnitsPerMark = 3;

    /// <summary>Anzeige „Kosten pro Woche:“ in DM (zwei Trainingstage).</summary>
    public long WeeklyCost => 2 * RawCostPerTrainingDay / InternalUnitsPerMark;

    /// <summary>Abzug vom Konto je Trainingstag in DM.</summary>
    public long CostPerTrainingDay => RawCostPerTrainingDay / InternalUnitsPerMark;

    /// <summary>Preisstufe aus der Liga des Vereins (null = ohne Liga → wie Oberliga).</summary>
    public static int TierOf(League? league) => league is { } l ? (int)l : 2;
}

/// <summary>
/// Wirkung des Trainings auf einen Spieler. [ORIGINAL] Nachgebaut aus em.exe, Funktion 46E:00BC (Datei 0x659C),
/// Tabellen DGROUP 0x4861/0x48CD/0x48E5 und Rundung 46E:000A. Läuft an jedem Trainingstag (zweimal pro Woche)
/// für alle Spieler der menschlichen Manager, immer mit dem ganzen Wochenplan.
/// </summary>
public static class TrainingRules
{
    /// <summary>Werte 0–3: Kondition, Technik und zwei versteckte Fähigkeiten (Spielerfeld 0x19, je ein Halbbyte).</summary>
    public const int Effects = 4;

    /// <summary>
    /// Belastung je Art, Wirkung und Gruppe (Torhüter, Abwehr, Sturm). [ORIGINAL] DGROUP 0x4861 (9 × 4 × 3 Byte).
    /// </summary>
    private static readonly byte[,,] Table =
    {
        { { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 } },
        { { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 } },
        { { 5, 2, 2 }, { 5, 5, 5 }, { 0, 1, 1 }, { 0, 1, 1 } },
        { { 8, 8, 6 }, { 0, 0, 0 }, { 0, 2, 2 }, { 0, 0, 0 } },
        { { 0, 0, 0 }, { 7, 7, 5 }, { 0, 1, 1 }, { 0, 1, 1 } },
        { { 4, 4, 4 }, { 6, 6, 6 }, { 0, 0, 0 }, { 0, 0, 0 } },
        { { 4, 2, 2 }, { 6, 4, 6 }, { 0, 1, 1 }, { 0, 1, 1 } },
        { { 7, 3, 5 }, { 1, 1, 1 }, { 0, 2, 2 }, { 0, 2, 2 } },
        { { 2, 1, 1 }, { 6, 5, 5 }, { 0, 1, 1 }, { 0, 3, 3 } },
    };

    /// <summary>Wochenbelastung, die einen Wert gerade hält (= 100 %). [ORIGINAL] DGROUP 0x48CD.</summary>
    private static readonly int[,] Norm = { { 83, 53, 55 }, { 94, 85, 85 }, { 101, 23, 23 }, { 101, 25, 25 } };

    /// <summary>Erholung, die die Energie gerade hält. [ORIGINAL] DGROUP 0x48E5.</summary>
    private static readonly int[] EnergyNorm = [111, 104, 102];

    public static int Load(TrainingKind kind, int effect, int group) => Table[(int)kind, effect, group];

    /// <summary>Gruppe des Spielers: 0 Torhüter, 1 Abwehr, 2 Sturm. [ORIGINAL] 238:0000 (Rollenwert / 25, höchstens 2).</summary>
    public static int Group(PlayerRole role) => role switch
    {
        PlayerRole.Goalkeeper => 0,
        PlayerRole.Defender => 1,
        _ => 2,
    };

    /// <summary>Wert „5 − Level“ des Originals (DGROUP 0xED8). Ab 4 (Level 0/1) wirkt die Planung nicht.</summary>
    public static int LevelValue(int level) => 5 - level;

    /// <summary>Untere und obere Grenze der Belastung nach Training und Spiel. [ORIGINAL] Datei 0x6B13–0x6B1A, 0x549A5–0x549BB.</summary>
    public const int MinStrain = 50, MaxStrain = 150;

    /// <summary>
    /// Belastung eines Spielers ohne Kaderplatz-Startwert. [APPROX] Entspricht der früheren Energie 0 des Remakes (Belastung
    /// 150), damit Spieler ohne gesetzten Wert sich wie bisher verhalten (z. B. Poolspieler vor dem Tausch, Transfer.Pool).
    /// </summary>
    public const int DefaultStrain = 150;

    /// <summary>
    /// Belastung (Kaderplatz +0x13) wie das Training sie liest: das Byte ohne Begrenzung. [ORIGINAL] Datei 0x6623.
    /// Bis Runde 11 war das 150 − Energie (A184).
    /// </summary>
    public static int Fatigue(Player p) => p.Strain;

    /// <summary>
    /// Wert der Spalte E (Aufstellung, Verträge, Transfer) und der Hinweiszeile „Erschöpfung“: Belastung · 10 / 15.
    /// [ORIGINAL] Datei 0x4235E–0x4236D (Listen) und 0x41B29–0x41B38 (Hinweiszeile). Beleg: nach „WM Vorbereitung“
    /// (Belastung 70) zeigt das Original 46 (docs/dosbox-befunde.md).
    /// </summary>
    public static int ExhaustionDisplay(Player p) => p.Strain * 10 / 15;

    /// <summary>Spalte E in Hervorhebungsfarbe 0x10 bei Belastung über 130. [ORIGINAL] Datei 0x42338–0x4233F.</summary>
    public static bool ExhaustionWarning(Player p) => p.Strain > 130;

    /// <summary>
    /// Nach dem Spiel eines Managervereins: Belastung aller Kaderplätze auf 50 … 150. [ORIGINAL] Spielabrechnung 0x544C0,
    /// Schleife über die Kaderplätze ab Datei 0x548A8, Grenzen Datei 0x549A2–0x549BB (auch für nicht Eingesetzte).
    /// </summary>
    public static void ClampStrain(IEnumerable<Player> players)
    {
        foreach (var p in players)
            p.Strain = Math.Clamp(p.Strain, MinStrain, MaxStrain);
    }

    /// <summary>
    /// Ein Trainingstag für einen Spieler. Liefert die Änderungen (für Tests und Anzeige).
    /// </summary>
    public static TrainingOutcome Apply(Player p, TrainingPlan plan, int level, Random random)
    {
        var group = Group(p.Role);
        var fatigue = Fatigue(p);
        var acc = new long[Effects];
        var count = new int[TrainingPlan.Kinds];
        long energy = 0;
        var bonus = -3 + (p.Age - 30) / 3;

        for (var day = 0; day < TrainingPlan.Days; day++)
        {
            for (var effect = 0; effect < Effects; effect++)
            {
                energy += 10;
                for (var unit = 0; unit < TrainingPlan.UnitsPerDay; unit++)
                {
                    var kind = plan[day, unit];
                    count[(int)kind]++; // im Original je Wirkung gezählt, also viermal je Einheit
                    int v = Table[(int)kind, effect, group];
                    energy -= v;
                    acc[effect] += v;
                }
            }

            // Abends frei (oder „Special“) erholt.
            var evening = (int)plan[day, 3];
            if (evening != 0)
                evening += (int)plan[day, 2];
            if (evening < 3)
                energy += 2;

            var drills = 0;
            for (var unit = 0; unit < TrainingPlan.UnitsPerDay; unit++)
            {
                if (unit < 3 && (int)plan[day, unit] > 1 && plan[day, unit] == plan[day, unit + 1])
                    bonus -= 3; // dieselbe Art zweimal hintereinander
                if (plan[day, unit] == TrainingKind.GoalDrill)
                    drills++;
            }
            if (drills > 1)
                energy -= 10;
        }

        if (count[0] > 3)
            energy += count[0];
        foreach (var day in TrainingPlan.MatchDays)
        {
            if ((int)plan[day, 0] > 1 && (int)plan[day, 1] > 1)
                bonus -= 6; // am Spieltag den ganzen Vormittag trainiert
        }
        foreach (var day in TrainingPlan.MatchDays)
        {
            if ((int)plan[day, 2] > 1 || (int)plan[day, 3] > 1)
                bonus -= 20; // am Spieltag nachmittags trainiert
        }
        bonus -= 5;
        for (var kind = 1; kind < TrainingPlan.Kinds; kind++)
        {
            if (count[kind] == 0)
                bonus -= 5; // Abwechslung: jede fehlende Art (auch „frei“) kostet
        }
        if (LevelValue(level) > 3)
            bonus = 0;
        if (count[0] > 4)
            count[0] = 4;
        if (count[0] != 0)
            bonus += count[0] + (count[0] - 1) / 2 + 2;

        var changes = new int[Effects];
        for (var effect = 0; effect < Effects; effect++)
        {
            acc[effect] = acc[effect] * (bonus + 100) / 100;
            if (fatigue > 125)
                acc[effect] -= acc[effect] * ((fatigue - 125) * 2) / 100;
            changes[effect] = Round((int)(acc[effect] * 100 / Norm[effect, group]), false, random);
        }

        p.Condition = Player.Clamp(p.Condition + changes[0]);
        p.Technique = Player.Clamp(p.Technique + changes[1]);
        if (group != 0)
        {
            p.HiddenSkill1 = Math.Clamp(p.HiddenSkill1 + changes[2], 0, 15);
            p.HiddenSkill2 = Math.Clamp(p.HiddenSkill2 + changes[3], 0, 15);
        }

        var recovery = Round((int)(energy * 100 / EnergyNorm[group]) * 2, true, random);
        // [ORIGINAL] Datei 0x6B08–0x6B1A: Belastung − Erholung (Byte), dann 50 … 150.
        p.Strain = Math.Clamp((fatigue - recovery) & 0xFF, MinStrain, MaxStrain);

        return new TrainingOutcome(changes[0], changes[1], changes[2], changes[3], recovery, bonus);
    }

    /// <summary>
    /// Rundung des Originals (46E:000A). Werte: 100 = neutral. Ohne <paramref name="energy"/>: Abweichung auf ±20
    /// begrenzt, Ergebnis ±1 mit Wahrscheinlichkeit Abweichung/101 (unter 3 nie). Mit: Abweichung auf ±100 begrenzt,
    /// Ergebnis Abweichung/5 (unter 3 → 0).
    /// </summary>
    public static int Round(int value, bool energy, Random random)
    {
        var sign = 1;
        if (!energy)
            value = Math.Clamp(value, 80, 120) - 100;
        else
            value = Math.Clamp(value, 100, 300) - 200;
        if (value < 0)
            sign = -1;
        value = Math.Abs(value);
        if (value < 3)
            sign = 0;
        if (!energy)
        {
            if (SquadSetup.Between(random, 0, 100) > value)
                sign = 0;
        }
        else
            sign *= value / 5;
        return sign;
    }

    /// <summary>
    /// Höhe der Balken E, K, T im Bildschirm (0–36 px) für eine Gruppe. [ORIGINAL] Mittel der Gruppe:
    /// E = (150 − Belastung über 50) · 36 / 150, K und T = Wert · 36 / 100. Null, wenn die Gruppe leer ist.
    /// </summary>
    public static (int Energy, int Condition, int Technique) Bars(IEnumerable<Player> players, int group)
    {
        var members = players.Where(p => Group(p.Role) == group).ToList();
        if (members.Count == 0)
            return (0, 0, 0);
        var strain = members.Sum(p => Math.Max(Fatigue(p), 50) - 50) / members.Count;
        var condition = members.Sum(p => p.Condition) / members.Count;
        var technique = members.Sum(p => p.Technique) / members.Count;
        return ((150 - strain) * 36 / 150, condition * 36 / 100, technique * 36 / 100);
    }
}

/// <summary>Änderungen eines Trainingstags: Kondition, Technik, versteckte Fähigkeiten, Erholung, Wochenbonus in %.</summary>
public readonly record struct TrainingOutcome(int Condition, int Technique, int Skill1, int Skill2, int Recovery, int Bonus);

public sealed partial class Player
{
    /// <summary>
    /// Unterzahl: unteres Halbbyte von Kaderplatz +0x19 (0–15), vom Training verändert, wirkt in der Abwehr in Unterzahl.
    /// [ORIGINAL] Startwerte und Schreiber siehe <see cref="HiddenSkills"/> (docs/analyse/re-nachtrag.md Abschnitt 6).
    /// [APPROX] Vorgabe 7 für Spieler, die nie in einen Managerkader oder den Markt kamen (Computervereine; im Original ohne
    /// Kaderplatz) und für Spielstände vor Teil 7.
    /// </summary>
    public int HiddenSkill1 { get; set; } = 7;

    /// <summary>Überzahl: oberes Halbbyte von Kaderplatz +0x19 (0–15), wirkt im Sturm in Überzahl; sonst wie <see cref="HiddenSkill1"/>.</summary>
    public int HiddenSkill2 { get; set; } = 7;
}

public sealed partial class Manager
{
    /// <summary>Trainingsplan (Hauptmenü → Mannschaft → Training).</summary>
    public TrainingPlan Training { get; } = new();
}

public sealed partial class GameState
{
    /// <summary>Saison und Tag, bis zu denen das Training fortgeschrieben ist.</summary>
    public (int Season, int Day) TrainedUntil { get; private set; } = (FirstSeason, 0);

    /// <summary>
    /// Trainingstage des Hauptwegs: die Takttage des Tageszählers (<see cref="IsDailyRunDay"/>, #678), im Tageswechsel
    /// <c>OnDayOpen(day)</c> vor dem Menü (#688) Mittwoch und Samstag. [ORIGINAL] Training 0x0659C nur in 0x07169 (Datei 0x07B5E), das der Hauptweg
    /// bei Zähler 0x3B7 ≡ 0/4 (mod 7) ruft (Datei 0x55253–0x552A6); vorher [APPROX] Mittwoch und Sonntag.
    /// </summary>
    public static bool IsTrainingDay(int day) => IsDailyRunDay(day);

    /// <summary>Plan eines Managers vorbereiten (Preisstufe bei Spielbeginn, Standardplan bzw. Level 5).</summary>
    public TrainingPlan PrepareTraining(Manager manager)
    {
        var plan = manager.Training;
        if (!plan.PriceTierSet)
            plan.PriceTier = TrainingPlan.TierOf(Clubs[manager.ClubIndex].League);
        plan.Prepare(manager.Level);
        return plan;
    }

    /// <summary>
    /// Ein Trainingstag: Kosten vom Konto, Wirkung auf alle Spieler der Manager. [ORIGINAL] 46E:0C89.
    /// </summary>
    public void TrainOneDay()
    {
        foreach (var manager in Managers)
            TrainManager(manager);
    }

    /// <summary>
    /// Trainingstag eines Managers: Kosten vom Konto, Wirkung auf seine Kaderplätze. [ORIGINAL] 46E:0C89, je Kaderplatz 0x0659C im
    /// Tagesablauf 0x07169 (Datei 0x07B5E).
    /// </summary>
    internal void TrainManager(Manager manager)
    {
        var plan = PrepareTraining(manager);
        Book(manager, FinanceCategory.Training, -plan.CostPerTrainingDay);
        foreach (var player in Clubs[manager.ClubIndex].Players)
            TrainingRules.Apply(player, plan, manager.Level, Random);
    }

    /// <summary>
    /// Schreibt das Training bis zum aktuellen Tag fort (jeder Trainingstag einmal, alle Manager). Nur Tests und Werkzeuge
    /// (R688-09): Die Zeitleiste trainiert seit #678 im Tagesablauf 0x07169 je Manager (<c>GameState.DailyRun.cs</c>) und setzt
    /// <see cref="TrainedUntil"/> selbst. Der Tag des Spielbeginns zählt nicht. [APPROX] Beim Saisonwechsel werden nicht nachgeholte Tage der alten
    /// Saison übersprungen.
    /// </summary>
    public void CatchUpTraining()
    {
        var (season, day) = TrainedUntil;
        if (season != Season)
            day = 0;
        while (day < Day)
        {
            day++;
            if (IsTrainingDay(day))
                TrainOneDay();
        }
        TrainedUntil = (Season, day);
    }
}
