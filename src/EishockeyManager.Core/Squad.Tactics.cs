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
/// [APPROX] Wertebereich, Bedeutung von „SEMI“ und die Auswertung im Spiel sind nicht aus dem Original belegt
/// (im Video stehen alle Werte auf 0). Annahmen: Werte −3…+3, der Wert eines Spielstands wird zum Grundwert addiert;
/// bei „AUTOM.“ wählt das Spiel selbst (<see cref="AutoValue"/>), bei „SEMI“ gelten die eigenen Werte, die
/// Automatik passt sie aber um höchstens 1 an, bei „MANUELL“ gelten sie genau.
/// </remarks>
public sealed class Tactics
{
    public const int Blocks = Lineup.Blocks;
    public const int Situations = 6;
    public const int MinValue = -3;
    public const int MaxValue = 3;

    /// <summary>Höchstwert eines Minutenfeldes. [APPROX] Ein Spiel dauert 60 Minuten.</summary>
    public const int MaxMinutes = 60;

    /// <summary>Tordifferenz ab der eine Führung bzw. ein Rückstand „hoch“ ist. [APPROX]</summary>
    public const int HighMargin = 3;

    private readonly int[,] _effort = new int[Blocks, Situations];
    private readonly int[,] _attack = new int[Blocks, Situations];
    private readonly int[,] _minutes = new int[Blocks, 2];

    public GoalieMode Goalie { get; set; }
    public LineChangeMode LineChange { get; set; }
    public TacticMode EffortMode { get; set; }
    public TacticMode AttackMode { get; set; }

    /// <summary>Eingestellter Einsatz eines Blocks (0–3) für einen Spielstand.</summary>
    public int Effort(int block, ScoreSituation situation) => _effort[block, (int)situation];

    public void SetEffort(int block, ScoreSituation situation, int value) =>
        _effort[block, (int)situation] = Math.Clamp(value, MinValue, MaxValue);

    /// <summary>Eingestellter Angriff eines Blocks (0–3) für einen Spielstand.</summary>
    public int Attack(int block, ScoreSituation situation) => _attack[block, (int)situation];

    public void SetAttack(int block, ScoreSituation situation, int value) =>
        _attack[block, (int)situation] = Math.Clamp(value, MinValue, MaxValue);

    /// <summary>Minuten-Vorgabe einer Reihe (0–3); <paramref name="forwards"/> = Sturm, sonst Abwehr.</summary>
    public int Minutes(int line, bool forwards) => _minutes[line, forwards ? 1 : 0];

    public void SetMinutes(int line, bool forwards, int value) =>
        _minutes[line, forwards ? 1 : 0] = Math.Clamp(value, 0, MaxMinutes);

    /// <summary>Sind die Minutenfelder in Gebrauch? (Nur bei „Minuten-Vorgabe (s.u.)“.)</summary>
    public bool MinutesActive => LineChange == LineChangeMode.MinutesPreset;

    /// <summary>Spielstand aus Sicht des eigenen Teams.</summary>
    public static ScoreSituation Situation(int goalDifference) => goalDifference switch
    {
        >= HighMargin => ScoreSituation.HighLead,
        > 0 => ScoreSituation.NarrowLead,
        0 => ScoreSituation.Tied,
        > -HighMargin => ScoreSituation.NarrowDeficit,
        _ => ScoreSituation.HighDeficit,
    };

    /// <summary>
    /// Wahl der Automatik. [APPROX] Bei Rückstand mehr Angriff und Einsatz, bei hoher Führung weniger.
    /// </summary>
    public static int AutoValue(ScoreSituation situation) => situation switch
    {
        ScoreSituation.HighLead => -1,
        ScoreSituation.NarrowDeficit => 1,
        ScoreSituation.HighDeficit => 2,
        _ => 0,
    };

    /// <summary>Wirksamer Einsatz eines Blocks beim gegebenen Spielstand.</summary>
    public int EffectiveEffort(int block, int goalDifference) => Effective(EffortMode, _effort, block, goalDifference);

    /// <summary>Wirksamer Angriff eines Blocks beim gegebenen Spielstand (für die Spielsimulation).</summary>
    public int EffectiveAttack(int block, int goalDifference) => Effective(AttackMode, _attack, block, goalDifference);

    private static int Effective(TacticMode mode, int[,] values, int block, int goalDifference)
    {
        var situation = Situation(goalDifference);
        var auto = AutoValue(situation);
        if (mode == TacticMode.Auto)
            return auto;
        var own = Math.Clamp(values[block, (int)ScoreSituation.Base] + values[block, (int)situation], MinValue, MaxValue);
        if (mode == TacticMode.Manual)
            return own;
        return Math.Clamp(own + Math.Sign(auto - own), MinValue, MaxValue);
    }

    /// <summary>„AUTOM.“, „SEMI“, „MANUELL“.</summary>
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
    /// Zuschlag auf die Spielstärke durch den Einsatz bei ausgeglichenem Spielstand, gewichtet wie die Blöcke.
    /// [APPROX] Ein Punkt Einsatz im ganzen Team = 1 Stärkepunkt. Kostet (später) Energie.
    /// </summary>
    public double EffortStrengthBonus()
    {
        double sum = 0;
        for (var b = 0; b < Blocks; b++)
            sum += TeamStrength.BlockWeights[b] * EffectiveEffort(b, 0);
        return sum;
    }
}

public sealed partial class Club
{
    public Tactics Tactics { get; } = new();
}
