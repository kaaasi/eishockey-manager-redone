using System.Runtime.CompilerServices;

namespace EishockeyManager.Core;

/// <summary>
/// A071: „Manager am Zug“ DGROUP 0xA70E während der Spiele eines Spieltags (Verletzungsprüfung 0x38B8F).
/// </summary>
/// <remarks>
/// [ORIGINAL] 0x38B8F bricht ab, wenn der Managerwert des Managers [0xA70E] (DGROUP 0x974B + 0x1AF · [0xA70E], Datei
/// 0x38C1C–0x38C2E) 100 ist – nicht der des geprüften Managers. [0xA70E] ist während der Spiele ein globaler Merker:
/// <list type="bullet">
/// <item>0 zu Beginn jedes Spieltagsablaufs 0x36B5D (Datei 0x36B7A; je Wettbewerb und Tag ein Aufruf);</item>
/// <item>je Minute der Managerpaarung nach den Abschlüssen 0x87F7 für Heim, dann Gast: Vereinsfeld 0x16 (Manager + 1), bei
///   einem Manager − 1 (Datei 0x08834, 0x08A3E) – nach der Minute also der Manager des Gastes bzw. 0;</item>
/// <item>jede Strafe 0x35818 (auch in Paarungen ohne Manager): Vereinsfeld 0x16 des bestraften Vereins, bei einem Manager − 1
///   (Datei 0x35ABC–0x35AC6);</item>
/// <item>die Verletzung ohne Live-Anzeige: der verletzte Manager (Datei 0x5419A; mit Anzeige sichert 0x5434C den Wert, Datei
///   0x54352/0x54419); die Zahlung 0x07089 sichert ihn ebenso (Datei 0x38D36–0x38D75), die Spielwertung 0x54420 auch.</item>
/// </list>
/// Managerwert 100 heißt „weniger als neun Spieler“ (0x952C, im Tageslauf vor den Spielen für jeden Manager). Wirksam nur mit
/// mehreren Managern: steht [0xA70E] auf einem Manager, der heute nicht spielfähig ist (z. B. Manager 0 nach einem Gast ohne
/// Manager), gibt es in dieser Minute keine Verletzung.
/// [APPROX] Das Remake hält den Merker je Spielstand, Tag und Wettbewerb (ein Spieltagsablauf); der Bundestrainer (Satz 5) und
/// DGROUP 0x1DB (zweite Bedingung der Live-Anzeige) fehlen.
/// </remarks>
public sealed class TurnManagerRegister
{
    private readonly Func<int, int> _managerValue;
    private readonly Dictionary<int, int> _values = [];

    internal TurnManagerRegister(Func<int, int> managerValue, bool liveDisplay)
    {
        _managerValue = managerValue;
        LiveDisplay = liveDisplay;
    }

    /// <summary>DGROUP 0xA70E.</summary>
    public int Manager { get; internal set; }

    /// <summary>
    /// Live-Anzeige am Spieltag (DGROUP 0x5A56): die Verletzung setzt den Merker dann nicht. Im Minutenschritt setzt
    /// <see cref="LiveMatchDay.Begin"/> nach der Vorbereitung aller Paarungen den Wert des ganzen Ablaufs (R26-G1).
    /// </summary>
    public bool LiveDisplay { get; internal set; }

    /// <summary>Managerwert DGROUP 0x974B + 0x1AF · Manager (vor den Spielen des Tages gerechnet, im Spiel unverändert).</summary>
    public int ManagerValue(int manager)
    {
        if (!_values.TryGetValue(manager, out var value))
            _values[manager] = value = _managerValue(manager);
        return value;
    }
}

public static partial class MatchEngine
{
    private sealed class TurnManagerDay
    {
        public int Day;
        public MatchCompetition Competition;
        public TurnManagerRegister Register = null!;
    }

    private static readonly ConditionalWeakTable<GameState, TurnManagerDay> TurnManagerDays = new();

    /// <summary>Merker [0xA70E] des laufenden Spieltagsablaufs (neu mit 0 an einem anderen Tag oder in einem anderen Wettbewerb).</summary>
    internal static TurnManagerRegister TurnManagerOf(GameState game, MatchCompetition competition, bool liveDisplay)
    {
        var day = TurnManagerDays.GetValue(game, _ => new TurnManagerDay { Day = -1 });
        if (day.Day != game.CurrentDay || day.Competition != competition)
        {
            day.Day = game.CurrentDay;
            day.Competition = competition;
            day.Register = new TurnManagerRegister(m => StoredManagerValue(game, m), liveDisplay);
        }
        return day.Register;
    }

    /// <summary>
    /// [ORIGINAL] DGROUP 0x974B + 0x1AF · m aus 0x952C (wie der Wert der Strafen, <see cref="LiveTeamOf"/>); ohne Satz 0 (A082).
    /// </summary>
    private static int StoredManagerValue(GameState game, int manager)
    {
        if (manager < 0 || manager >= game.Managers.Count)
            return 0;
        var club = game.Clubs.FirstOrDefault(c => c.Index == game.Managers[manager].ClubIndex);
        return club is { Players.Count: > 0 }
            ? ManagedTeamValues(LiveSquad.Create(club, 0, null), nineOrMore: !game.IsShortHanded(club)).ManagerValue
            : 0;
    }
}
