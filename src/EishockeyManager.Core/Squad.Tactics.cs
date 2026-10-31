namespace EishockeyManager.Core;

/// <summary>Torwartwechsel (Taktik oben links): „AUTOM.“ oder „MANUELL“.</summary>
public enum GoalieMode
{
    Auto,
    Manual,
}

/// <summary>Linienwechsel-Modi in der Reihenfolge des Originals (Texte aus em.exe).</summary>
public enum LineChangeMode
{
    /// <summary>„Automatisch“</summary>
    Automatic,

    /// <summary>„Spiel nur mit 2 Reihen“</summary>
    TwoLinesOnly,

    /// <summary>„Energiestärkster Block“</summary>
    StrongestBlock,

    /// <summary>„Nach Gegnerreihe“</summary>
    ByOpponentLine,

    /// <summary>„Minuten-Vorgabe (s.u.)“ – Minuten je Reihe in den Feldern darunter.</summary>
    MinutesPreset,

    /// <summary>„Voll manuell“</summary>
    FullyManual,
}

/// <summary>Einstellung von Einsatz und Angriff: „AUTOM.“, „SEMI“, „MANUELL“.</summary>
public enum TacticMode
{
    Auto,
    Semi,
    Manual,
}

/// <summary>Spielstände, für die Einsatz und Angriff eingestellt werden (Zeilen der Taktik).</summary>
public enum ScoreSituation
{
    /// <summary>„Grundwert“</summary>
    Base,

    /// <summary>„hohe Führung“</summary>
    HighLead,

    /// <summary>„knappe Führung“</summary>
    NarrowLead,

    /// <summary>„Unentschieden“</summary>
    Tied,

    /// <summary>„knapper Rückstand“</summary>
    NarrowDeficit,

    /// <summary>„hoher Rückstand“</summary>
    HighDeficit,
}

/// <summary>
/// Taktik eines Vereins (Hauptmenü → Mannschaft → Taktik): Torwartwechsel, Linienwechsel mit Minuten je Reihe
/// und für jeden der vier Blöcke Einsatz und Angriff je Spielstand.
/// </summary>
/// <remarks>
/// Belege aus em.exe (Taktikbildschirm Datei 0x47F2B, Spielvorbereitung Datei 0x8365, Spielstand Datei 0x939A):
/// <list type="bullet">
/// <item>[ORIGINAL] Je Block und Spielstand ein Byte; Zeile „Grundwert“ ist der Grundwert, die übrigen fünf Zeilen
///   sind Zuschläge darauf. Einsatz ist ein Prozentwert (Obergrenze 100), Angriff eine Stufe (Obergrenze 7);
///   Grundwert + Zuschlag darf die Obergrenze nicht überschreiten (Datei 0x488E9–0x48925, 0x48971–0x489A8).
///   Je Manager 4 Blöcke × 6 Werte: Einsatz DGROUP 0xCAE9 + 0x30 · Manager, Angriff 0xCB01 + 0x30 · Manager.</item>
/// <item>[ORIGINAL] Modus 0 „AUTOM.“, 1 „SEMI“, 2 „MANUELL“ (Texte DGROUP 0x5A4; Einsatz DGROUP 0xA6FB, Angriff 0xA707
///   je Manager). Nur bei „SEMI“ sind die Werte hell und mit den Pfeilen änderbar (Datei 0x47D9B, 0x488F6), Schritt 1.</item>
/// <item>[ORIGINAL] Vor jedem Spiel setzt die Automatik die Werte des Vereins, wenn der Modus „AUTOM.“ ist oder der
///   Verein keinen Manager hat (Datei 0x8365, Aufruf je Paarung Datei 0x36D12): Sind die doppelten Stärken beider
///   Teams weniger als 25 auseinander, Einsatz 60 mit Zuschlägen 0/25/20/30/0, sonst 40 mit 20/20/15/20/0
///   (DGROUP 0x4912); Angriff 0 mit 5/6/4/4/0, wenn das eigene Team schwächer ist, sonst 4/5/6/7/4 (DGROUP 0x491C).</item>
/// <item>[ORIGINAL] Im Spiel gilt Grundwert + Zuschlag des Spielstands; „hoch“ ab 3 Toren Differenz. Einsatz wird auf
///   0…100 begrenzt, Angriff in drei Bit gespeichert (Datei 0x939A–0x9506). Bei „MANUELL“ überschreibt das Spiel
///   die Werte nicht (sie werden im Live-Spiel gesetzt, Datei 0x94E2, 0x9516).</item>
/// <item>[APPROX] Vereine: Bis zum ersten Spiel, und solange das Remake keine Spielvorbereitung je Paarung hat, zeigt
///   „AUTOM.“ die Zeile für ein ausgeglichenes Spiel gegen einen gleich starken Gegner (Video `bundestrainer_00058`:
///   60/0/25/20/30/0 und 0/4/5/6/7/4). Eigene Werte eines neuen Vereins starten mit derselben Zeile.</item>
/// <item>[ORIGINAL] Nationalteam (#192, <see cref="National"/>): kein Startwert. Die Automatik 0x8365 schreibt
///   Einsatz (Felder 0x26–0x2B) und Angriff (0x35–0x3A) in den Vereinssatz nur vor einem Spiel, und nur wenn der Verein
///   keinen Manager hat oder dessen Modus „AUTOM.“ ist (Datei 0x8421–0x847D Einsatz, 0x8481–0x84BD Angriff); Aufrufe
///   nur je Paarung des Spieltagsablaufs (Datei 0x36D12/0x36D49, 0x36F27/0x36F5A). Es gibt keinen Start- oder
///   Ernennungswert und keine Unterscheidung nach Modus: bis zum ersten Spiel steht „AUTOM.“ auf 0 (DOSBox
///   `nur-weltkugel/26`), danach auf den Werten des letzten Spiels (<see cref="RecordAutoLine"/>). Die eigenen Werte
///   des Bundestrainers (DGROUP 0xCAE9/0xCB01 + 0x30 · 5) schreibt nur der Taktikbildschirm (Datei 0x48607 ff.) und
///   das Laden (Datei 0x5C38C, 0x5CD7D) – sie beginnen ebenfalls bei 0.</item>
/// <item>[APPROX] „MANUELL“ verwendet bis zum Live-Spiel die eigenen Werte wie „SEMI“. Der Energieabzug im Spiel
///   (Datei 0x9463–0x94A7, Einsatz − (50 − Energie) bei Energie unter 50) ist nicht übernommen.</item>
/// </list>
/// </remarks>
public sealed class Tactics
{
    public const int Blocks = Lineup.Blocks;
    public const int Situations = 6;

    /// <summary>[ORIGINAL] Obergrenze des Einsatzes (Prozent; Datei 0x488E9: 0x64).</summary>
    public const int MaxEffort = 100;

    /// <summary>[ORIGINAL] Obergrenze des Angriffs (Datei 0x48923: 7).</summary>
    public const int MaxAttack = 7;

    /// <summary>Höchstwert eines Minutenfeldes. [APPROX] Ein Spiel dauert 60 Minuten.</summary>
    public const int MaxMinutes = 60;

    /// <summary>[ORIGINAL] Tordifferenz ab der eine Führung bzw. ein Rückstand „hoch“ ist (Datei 0x93E7: Differenz &gt; 2).</summary>
    public const int HighMargin = 3;

    /// <summary>[ORIGINAL] Grenze der doppelten Stärkedifferenz für ein ausgeglichenes Spiel (Datei 0x840E: 0x19).</summary>
    public const int EvenMatchLimit = 25;

    /// <summary>[ORIGINAL] Einsatz der Automatik: Grundwert und Zuschläge (DGROUP 0x4912 ff., Datei 0x8413/0x841C).</summary>
    private static readonly int[] EvenEffort = [60, 0, 25, 20, 30, 0];
    private static readonly int[] UnevenEffort = [40, 20, 20, 15, 20, 0];

    /// <summary>[ORIGINAL] Angriff der Automatik (DGROUP 0x491C ff.; Grundwert 0, Datei 0x8499).</summary>
    private static readonly int[] UnderdogAttack = [0, 5, 6, 4, 4, 0];
    private static readonly int[] FavouriteAttack = [0, 4, 5, 6, 7, 4];

    /// <summary>[APPROX] Bezugswert des Einsatzes für den Stärkezuschlag: Automatik bei Gleichstand im ausgeglichenen Spiel.</summary>
    public const int ReferenceEffort = 80;

    private readonly int[,] _effort = new int[Blocks, Situations];
    private readonly int[,] _attack = new int[Blocks, Situations];
    private readonly int[,] _minutes = new int[Blocks, 2];

    /// <summary>
    /// Angezeigte Werte unter „AUTOM.“ (Grundwert und fünf Zuschläge) = Werte, die die Automatik vor dem letzten Spiel
    /// geschrieben hat (Vereinssatz 0x26–0x2B bzw. 0x35–0x3A). Nur beim Nationalteam geführt; null = Anzeige wie bisher
    /// (Vereine, [APPROX] siehe Klasse).
    /// </summary>
    private readonly int[]? _shownAutoEffort;
    private readonly int[]? _shownAutoAttack;

    /// <summary>Taktik eines Vereins; eigene Werte starten mit der Zeile für ein ausgeglichenes Spiel ([APPROX], siehe Klasse).</summary>
    public Tactics()
        : this(EvenEffort, FavouriteAttack)
    {
    }

    private Tactics(int[] effort, int[] attack)
    {
        for (var b = 0; b < Blocks; b++)
        {
            for (var i = 0; i < Situations; i++)
            {
                _effort[b, i] = effort[i];
                _attack[b, i] = attack[i];
            }
        }
    }

    /// <summary>
    /// [ORIGINAL] Taktik des Nationalteams ohne Startwert (#192, siehe Klasse): eigene Werte und Anzeige unter „AUTOM.“
    /// stehen auf 0, bis die Automatik vor dem ersten Spiel schreibt.
    /// </summary>
    public static Tactics National()
    {
        var zero = new int[Situations];
        return new Tactics(zero, zero, new int[Situations], new int[Situations]);
    }

    private Tactics(int[] effort, int[] attack, int[] shownAutoEffort, int[] shownAutoAttack)
        : this(effort, attack)
    {
        _shownAutoEffort = shownAutoEffort;
        _shownAutoAttack = shownAutoAttack;
    }

    /// <summary>Führt diese Taktik die Werte des letzten Spiels für die Anzeige unter „AUTOM.“ (Nationalteam)?</summary>
    public bool KeepsLastAutoLine => _shownAutoEffort is not null;

    /// <summary>
    /// [ORIGINAL] Spielvorbereitung 0x8365 für die Anzeige: schreibt die Zeile der Automatik gegen einen Gegner, wenn
    /// der jeweilige Modus „AUTOM.“ ist (Einsatz und Angriff getrennt, Datei 0x8421 bzw. 0x8481). Nur bei Taktiken
    /// mit <see cref="KeepsLastAutoLine"/>; die Werte bleiben bis zum nächsten Spiel stehen, auch über den
    /// Saisonwechsel. Die Werte im Spiel selbst bestimmt weiter <see cref="EffectiveEffort"/>/<see cref="EffectiveAttack"/>.
    /// </summary>
    public void RecordAutoLine(int ownStrength, int opponentStrength)
    {
        if (_shownAutoEffort is null || _shownAutoAttack is null)
            return;
        for (var i = 0; i < Situations; i++)
        {
            if (EffortMode == TacticMode.Auto)
                _shownAutoEffort[i] = AutoEffort(ownStrength, opponentStrength, (ScoreSituation)i);
            if (AttackMode == TacticMode.Auto)
                _shownAutoAttack[i] = AutoAttack(ownStrength, opponentStrength, (ScoreSituation)i);
        }
    }

    public GoalieMode Goalie { get; set; }
    public LineChangeMode LineChange { get; set; }
    public TacticMode EffortMode { get; set; }
    public TacticMode AttackMode { get; set; }

    /// <summary>[ORIGINAL] Eigene Werte sind nur bei „SEMI“ änderbar (Datei 0x488F6/0x48930).</summary>
    public static bool Editable(TacticMode mode) => mode == TacticMode.Semi;

    /// <summary>Eigener Einsatz eines Blocks (0–3) für einen Spielstand: Grundwert bzw. Zuschlag.</summary>
    public int Effort(int block, ScoreSituation situation) => _effort[block, (int)situation];

    /// <summary>Eigenen Einsatz setzen; begrenzt auf 0 … <see cref="EffortLimit"/>.</summary>
    public void SetEffort(int block, ScoreSituation situation, int value) =>
        _effort[block, (int)situation] = Math.Clamp(value, 0, EffortLimit(block, situation));

    /// <summary>Eigener Angriff eines Blocks (0–3) für einen Spielstand: Grundwert bzw. Zuschlag.</summary>
    public int Attack(int block, ScoreSituation situation) => _attack[block, (int)situation];

    /// <summary>Eigenen Angriff setzen; begrenzt auf 0 … <see cref="AttackLimit"/>.</summary>
    public void SetAttack(int block, ScoreSituation situation, int value) =>
        _attack[block, (int)situation] = Math.Clamp(value, 0, AttackLimit(block, situation));

    /// <summary>Höchster zulässiger Einsatz eines Feldes (siehe <see cref="Limit"/>).</summary>
    public int EffortLimit(int block, ScoreSituation situation) => Limit(_effort, MaxEffort, block, situation);

    /// <summary>Höchster zulässiger Angriff eines Feldes (siehe <see cref="Limit"/>).</summary>
    public int AttackLimit(int block, ScoreSituation situation) => Limit(_attack, MaxAttack, block, situation);

    /// <summary>
    /// [ORIGINAL] Zuschlag ≤ Obergrenze − Grundwert; Grundwert ≤ Obergrenze − größter Zuschlag der Zeilen
    /// „hohe Führung“ bis „knapper Rückstand“ (Datei 0x4896C: Schleife 1…4 – „hoher Rückstand“ wird dabei nicht
    /// geprüft). Ein Wert über der Grenze (z. B. nach Senken der Grenze) bleibt stehen, steigt aber nicht weiter.
    /// </summary>
    private static int Limit(int[,] values, int max, int block, ScoreSituation situation)
    {
        int limit;
        if (situation == ScoreSituation.Base)
        {
            limit = max;
            for (var i = 1; i < Situations - 1; i++)
                limit = Math.Min(limit, max - values[block, i]);
        }
        else
            limit = max - values[block, (int)ScoreSituation.Base];
        return Math.Max(limit, Math.Min(values[block, (int)situation], max));
    }

    /// <summary>Minuten-Vorgabe einer Reihe (0–3); <paramref name="forwards"/> = Sturm, sonst Abwehr.</summary>
    public int Minutes(int line, bool forwards) => _minutes[line, forwards ? 1 : 0];

    public void SetMinutes(int line, bool forwards, int value) =>
        _minutes[line, forwards ? 1 : 0] = Math.Clamp(value, 0, MaxMinutes);

    /// <summary>Sind die Minutenfelder in Gebrauch? (Nur bei „Minuten-Vorgabe (s.u.)“.)</summary>
    public bool MinutesActive => LineChange == LineChangeMode.MinutesPreset;

    /// <summary>[ORIGINAL] Spielstand aus Sicht des eigenen Teams (Datei 0x93E2–0x9460).</summary>
    public static ScoreSituation Situation(int goalDifference) => goalDifference switch
    {
        >= HighMargin => ScoreSituation.HighLead,
        > 0 => ScoreSituation.NarrowLead,
        0 => ScoreSituation.Tied,
        > -HighMargin => ScoreSituation.NarrowDeficit,
        _ => ScoreSituation.HighDeficit,
    };

    /// <summary>
    /// [ORIGINAL] Einsatz der Automatik für ein Spiel (Datei 0x8365 ff.): ausgeglichen, wenn die doppelten Stärken
    /// weniger als <see cref="EvenMatchLimit"/> auseinanderliegen.
    /// </summary>
    public static int AutoEffort(int ownStrength, int opponentStrength, ScoreSituation situation) =>
        (Math.Abs(2 * ownStrength - 2 * opponentStrength) < EvenMatchLimit ? EvenEffort : UnevenEffort)[(int)situation];

    /// <summary>[ORIGINAL] Angriff der Automatik: Zeile für den Schwächeren, wenn die eigene Stärke kleiner ist.</summary>
    public static int AutoAttack(int ownStrength, int opponentStrength, ScoreSituation situation) =>
        (ownStrength < opponentStrength ? UnderdogAttack : FavouriteAttack)[(int)situation];

    /// <summary>
    /// Angezeigter Einsatz: bei „AUTOM.“ beim Nationalteam die Werte des letzten Spiels (vorher 0, [ORIGINAL]), beim
    /// Verein die Automatik für ein ausgeglichenes Spiel ([APPROX], siehe Klasse); sonst der eigene Wert.
    /// </summary>
    public int ShownEffort(int block, ScoreSituation situation) =>
        EffortMode != TacticMode.Auto ? Effort(block, situation)
        : _shownAutoEffort is { } shown ? shown[(int)situation]
        : AutoEffort(0, 0, situation);

    /// <summary>Angezeigter Angriff: wie <see cref="ShownEffort"/>.</summary>
    public int ShownAttack(int block, ScoreSituation situation) =>
        AttackMode != TacticMode.Auto ? Attack(block, situation)
        : _shownAutoAttack is { } shown ? shown[(int)situation]
        : AutoAttack(0, 0, situation);

    /// <summary>
    /// [ORIGINAL] Wirksamer Einsatz eines Blocks beim gegebenen Spielstand: Grundwert + Zuschlag, 0…100.
    /// Bei „AUTOM.“ die Automatik gegen einen Gegner der Stärke <paramref name="opponentStrength"/>.
    /// </summary>
    public int EffectiveEffort(int block, int goalDifference, int ownStrength = 0, int opponentStrength = 0)
    {
        var situation = Situation(goalDifference);
        var value = EffortMode == TacticMode.Auto
            ? AutoEffort(ownStrength, opponentStrength, ScoreSituation.Base) + AutoEffort(ownStrength, opponentStrength, situation)
            : _effort[block, 0] + _effort[block, (int)situation];
        return Math.Clamp(value, 0, MaxEffort);
    }

    /// <summary>
    /// [ORIGINAL] Wirksamer Angriff eines Blocks beim gegebenen Spielstand: (Grundwert + Zuschlag) in drei Bit
    /// (Datei 0x94F9–0x9506: <c>shl al,4</c>, Maske 0x70).
    /// </summary>
    public int EffectiveAttack(int block, int goalDifference, int ownStrength = 0, int opponentStrength = 0)
    {
        var situation = Situation(goalDifference);
        var value = AttackMode == TacticMode.Auto
            ? AutoAttack(ownStrength, opponentStrength, ScoreSituation.Base) + AutoAttack(ownStrength, opponentStrength, situation)
            : _attack[block, 0] + _attack[block, (int)situation];
        return value & MaxAttack;
    }

    /// <summary>„AUTOM.“, „SEMI“, „MANUELL“ (DGROUP 0x5A4).</summary>
    public static string ModeText(TacticMode mode) => mode switch
    {
        TacticMode.Semi => "SEMI",
        TacticMode.Manual => "MANUELL",
        _ => "AUTOM.",
    };

    public static string ModeText(GoalieMode mode) => mode == GoalieMode.Manual ? "MANUELL" : "AUTOM.";

    public static string LineChangeText(LineChangeMode mode) => mode switch
    {
        LineChangeMode.TwoLinesOnly => "Spiel nur mit 2 Reihen",
        LineChangeMode.StrongestBlock => "Energiestärkster Block",
        LineChangeMode.ByOpponentLine => "Nach Gegnerreihe",
        LineChangeMode.MinutesPreset => "Minuten-Vorgabe (s.u.)",
        LineChangeMode.FullyManual => "Voll manuell",
        _ => "Automatisch",
    };

    public static string SituationText(ScoreSituation situation) => situation switch
    {
        ScoreSituation.HighLead => "hohe Führung",
        ScoreSituation.NarrowLead => "knappe Führung",
        ScoreSituation.Tied => "Unentschieden",
        ScoreSituation.NarrowDeficit => "knapper Rückstand",
        ScoreSituation.HighDeficit => "hoher Rückstand",
        _ => "Grundwert",
    };

    /// <summary>
    /// Zuschlag auf die Spielstärke durch den Einsatz bei Gleichstand, gewichtet wie die Blöcke.
    /// [APPROX] Wie der Einsatz im Original auf das Ergebnis wirkt, ist nicht rekonstruiert (Spielsimulation, Overlay
    /// 5E73). „AUTOM.“ gibt keinen Zuschlag; sonst je Block (Einsatz − <see cref="ReferenceEffort"/>) / 10,
    /// begrenzt auf −3 … +3 (Wertebereich des bisherigen Zuschlags). Kostet (später) Energie.
    /// </summary>
    public double EffortStrengthBonus()
    {
        if (EffortMode == TacticMode.Auto)
            return 0;
        double sum = 0;
        for (var b = 0; b < Blocks; b++)
            sum += TeamStrength.BlockWeights[b] * Math.Clamp((EffectiveEffort(b, 0) - ReferenceEffort) / 10.0, -3, 3);
        return sum;
    }
}

public sealed partial class Club
{
    public Tactics Tactics { get; } = new();
}
