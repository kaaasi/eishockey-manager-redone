using EishockeyManager.Formats;

namespace EishockeyManager.Core;

/// <summary>Gewählte Torszene: Nummer n, Penalty- bzw. Sechser-Szene und Dateiname unter <c>tore\</c>.</summary>
public readonly record struct GoalSceneChoice(int Number, bool Penalty, bool SixSkaters, string FileName);

/// <summary>Eine gewählte Szene einer Anzeigereihe (Minute, Seite der Chance, Tor oder vergeben).</summary>
public readonly record struct ScoreboardScene(int Minute, int Side, bool Goal, GoalSceneChoice Choice);

/// <summary>Ein Strafgrund einer Anzeigereihe: Index in die Liste DGROUP 0x5CC (0 … 12).</summary>
public readonly record struct ScoreboardPenaltyReason(int Minute, int Side, int Reason);

/// <summary>
/// Zufall der Live-Anzeige im Spielkern (Spur livespiel-kern, Teil A4, L4/L7-Kern; Entscheidung Nutzer R18-D3 = A):
/// Züge, die das Original nur mit Anzeige macht, zieht der Kern unter der Bedingung des Originals aus dem Spielzufall – auch
/// ohne offenen Bildschirm, damit die Zufallsfolge wie im Original verschoben wird.
/// <list type="bullet">
/// <item>[ORIGINAL] Szenenwahl 0x575C3 (aus dem Abspieler 0x567C8, Datei 0x56BBC): n = random(2, Normal); dann
///   random(0, Tor ? 210 : 255) = 0 → Penalty; Penalty → n = random(2, Penalty), sonst bei sechs Feldspielern
///   n = random(2, Sechs) – auch bei gleichen Grenzen ein Zug (Datei 0xC037). Dateiname „n“ + „.h“/„.a“ + „v“/„t“ + „e“
///   (Penalty) + „j“ (sechs) (Datei 0x57645–0x576E9). Der Abspieler läuft aus der Torszene 0x53C57 nur für Art 0/1 und
///   mit Darstellung DGROUP 0x226 ≠ 0 (Datei 0x53DB1–0x53DE3, 0x53E02) bei Live-Anzeige am Spieltag (Datei 0x53F75).</item>
/// <item>[ORIGINAL] Strafgrund: Die Strafe 0x35818 ruft den Reporter, wenn einer der beiden Vereine einen Manager hat (Datei
///   0x35C1C–0x35C73); der Reporter 0x52520 arbeitet nur mit Live-Anzeige am Spieltag (DGROUP 0x5A56) und Geschwindigkeit
///   unter 70 (Datei 0x5252A–0x5253B) und zieht bei der 2-Minuten-Strafe random(0, 12) (Datei 0x52A9A–0x52AA8); die
///   5-Minuten-Strafe nimmt Index 5 ohne Zug (Datei 0x52A94).</item>
/// <item>[APPROX] Die Wiederholung einer Szene mit Taste „r“/„w“ (bis dreimal, Datei 0x54033–0x54069) zieht im Original neu;
///   sie ist Sache des Bildschirms (Spur C). Die Rückkehr der Torszene bei einem Managerverein mit zu wenigen fitten Spielern
///   (<c>120h:66h</c>, Datei 0x53D73–0x53D89) ist nicht nachgebaut.</item>
/// </list>
/// </summary>
public static class GoalSceneSelection
{
    /// <summary>[ORIGINAL] Datei 0x575F2/0x575F7: Penalty-Szene mit 1/256 (vergeben) bzw. 1/211 (Tor).</summary>
    public const int MissPenaltyRange = 0xFF;

    public const int GoalPenaltyRange = 0xD2;

    /// <summary>[ORIGINAL] Zahl der Strafgründe (DGROUP 0x5CC, 13 Fernzeiger) und Index der 5-Minuten-Strafe.</summary>
    public const int ReasonCount = 13;

    public const int MajorPenaltyReason = 5;

    /// <summary>[ORIGINAL] Strafgründe (DGROUP 0x5CC; Umlaute nach DIN 66003 dort, hier als Text).</summary>
    public static readonly string[] Reasons =
    [
        "Stockschlags.", "Ellbogenchecks.", "hohem Stocks.", "Haltens.", "Hakens.", "Übertr. Härte.", "Cross-Checks.",
        "Faustschlags.", "Bandenchecks.", "Reklamierens.", "Beinstellen.", "Torverschiebens.", "Stockstichs.",
    ];

    /// <summary>[ORIGINAL] Inhalt von <c>tore\anzahl</c> der DOS-Version: <c>MAXSZENE:11|2|2</c> (Lader 0x57480 beim Programmstart).</summary>
    public static readonly GoalSceneCounts OriginalCounts = new(11, 2, 2);

    /// <summary>Szene wählen ([ORIGINAL] 0x575C3, Züge in genau dieser Reihenfolge).</summary>
    /// <param name="goal">Art 1 (Tor) bzw. 0 (vergeben).</param>
    /// <param name="penalty">Kennzeichen der Torszene (aus dem Abschluss 0xAB69 immer 0, nur das Penaltyschießen setzt es).</param>
    /// <param name="side">Seite der Chance (0 Heim, 1 Gast).</param>
    /// <param name="sixSkaters">Verein dieser Seite mit Feldspielern + belegten Strafbankplätzen = 6 (Datei 0x56B4D–0x56BA8).</param>
    public static GoalSceneChoice Choose(bool goal, bool penalty, int side, bool sixSkaters, GoalSceneCounts counts, Random random)
    {
        var n = OriginalMatch.Rnd(random, 2, counts.Normal);
        if (OriginalMatch.Rnd(random, 0, goal ? GoalPenaltyRange : MissPenaltyRange) == 0)
            penalty = true;
        var six = false;
        if (penalty)
        {
            n = OriginalMatch.Rnd(random, 2, counts.Penalty);
        }
        else if (sixSkaters)
        {
            six = true;
            n = OriginalMatch.Rnd(random, 2, counts.SixSkaters);
        }
        var name = $"{n}.{(side == 0 ? 'h' : 'a')}{(goal ? 't' : 'v')}{(penalty && !six ? "e" : "")}{(six ? "j" : "")}";
        return new GoalSceneChoice(n, penalty, six, name);
    }

    /// <summary>Strafgrund ([ORIGINAL] Datei 0x52A8E–0x52AA8): 5 Minuten → Index 5 ohne Zug, sonst random(0, 12).</summary>
    public static int PenaltyReason(int minutes, Random random) =>
        minutes == 5 ? MajorPenaltyReason : OriginalMatch.Rnd(random, 0, ReasonCount - 1);
}

public sealed partial class LiveScoreboardRow
{
    private readonly List<ScoreboardScene> scenes = [];
    private readonly List<ScoreboardPenaltyReason> reasons = [];

    /// <summary>Gewählte Torszenen (Teil A4).</summary>
    public IReadOnlyList<ScoreboardScene> Scenes => scenes;

    /// <summary>Gezogene Strafgründe (Teil A4).</summary>
    public IReadOnlyList<ScoreboardPenaltyReason> PenaltyReasons => reasons;

    internal void AddScene(ScoreboardScene scene) => scenes.Add(scene);

    internal void AddPenaltyReason(ScoreboardPenaltyReason reason) => reasons.Add(reason);
}

public static partial class OriginalMinutes
{
    /// <summary>
    /// [ORIGINAL] Aufruf der Torszene 0x53C57 aus dem Abschluss 0xA484 (Datei 0xAB90) für eine Paarung mit Anzeigereihe, nach
    /// jedem Abschluss mit Art = Tor (1) bzw. vergeben (0), vor dem Torschützen 0x541D9; nicht nach dem Abbruch in der
    /// Verlängerung (0xA642–0xA650). A1: Drittelzähler (<see cref="LiveScoreboardRow.Chances"/>); A4: Szenenwahl bei
    /// Darstellung ≠ 0 (<see cref="GoalSceneSelection.Choose"/>).
    /// </summary>
    internal static void GoalScene(int minute, int side, bool goal, LiveTeam shooter, Random random)
    {
        if (Display is not { Row: { } row } display)
            return;
        row.AddChance(minute, side, goal);
        if (display.Options is not { LiveMode: not 0 })
            return;
        var six = shooter.Box.Skaters + shooter.Box.Count == 6; // [ORIGINAL] Datei 0x56B4D–0x56BA8: ohne Torhüter
        var choice = GoalSceneSelection.Choose(goal, penalty: false, side, six, GoalSceneSelection.OriginalCounts, random);
        row.AddScene(new ScoreboardScene(minute, side, goal, choice));
    }

    /// <summary>
    /// Strafgrund des Reporters nach einer Strafe mit Spieler (Datei 0x35C1C–0x35C73 → 0x52520): Managerpaarung, Live-Anzeige
    /// am Spieltag, Geschwindigkeit unter 70; 2 Minuten ziehen random(0, 12).
    /// </summary>
    internal static void PenaltyReason(LiveTeam club, LiveTeam opponent, int side, int minute, int minutes, Random random)
    {
        if (Display is not { DayActive: true, Options: { } options } display || (!club.Managed && !opponent.Managed)
            || options.MatchSpeed >= LiveClock.FastSpeed)
            return;
        var reason = GoalSceneSelection.PenaltyReason(minutes, random);
        display.Row?.AddPenaltyReason(new ScoreboardPenaltyReason(minute, side, reason));
    }
}
