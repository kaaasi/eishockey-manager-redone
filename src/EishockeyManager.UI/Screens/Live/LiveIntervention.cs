using EishockeyManager.Core;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Screens.Team;

namespace EishockeyManager.UI.Screens.Live;

/// <summary>
/// Eingriff im Spiel über Kopf/Logo (Spur livespiel-ui-2, B3; <c>docs/analyse/re-livespiel.md</c> §4, §10, §15): Datei 0x5434C
/// mit der Schleife 0x48E28 – Aufstellung bzw. Taktik mit Kennwert „im Spiel“, Wechsel über den Knopf unten rechts, bis einer
/// der beiden Bildschirme verlassen wird; danach zurück in den Live-Bildschirm.
/// </summary>
public static class LiveIntervention
{
    /// <summary>[ORIGINAL] Kopfzeile 0x40290: Grundlinie 10, Farbe 30, Schatten 0 (Datei 0x40290, <c>re-rest.md</c>).</summary>
    public const int HeaderBaseline = 10;

    public const byte HeaderColor = 30;

    /// <summary>[ORIGINAL] Vereinsname ab x = 1, Managername rechtsbündig bis x = 317.</summary>
    public const int HeaderLeft = 1, HeaderRight = 317;

    /// <summary>
    /// Ersten Bildschirm des Eingriffs bauen: <see cref="InterventionMode.Lineup"/> (linke Taste) die Aufstellung,
    /// <see cref="InterventionMode.Tactics"/> (rechte Taste) die Taktik, jeweils mit Kennwert „im Spiel“ und Rückweg
    /// <paramref name="back"/>. [ORIGINAL] Datei 0x48E28: Rückgabe 1 → Aufstellung, sonst Taktik, 0 → Ende; im Remake der
    /// Wechselknopf unten rechts bzw. Escape/Rechtsklick außerhalb.
    /// </summary>
    public static Screen Open(ScreenHost host, Screen back, InterventionMode mode) =>
        mode == InterventionMode.Lineup ? Lineup(host, back) : Tactics(host, back);

    private static Screen Lineup(ScreenHost host, Screen back) =>
        new LineupScreen(host, back, () => Tactics(host, back)) { InMatch = true };

    private static Screen Tactics(ScreenHost host, Screen back) =>
        new TacticsScreen(host, back, () => TeamUi.OwnClub(host).Tactics, () => Lineup(host, back)) { InMatch = true };

    /// <summary>
    /// [ORIGINAL] Kopfzeile Datei 0x40290 (nur mit Kennwert ≠ 0, Aufrufe Aufstellung Datei 0x40530 und Taktik Datei 0x480D2):
    /// Schrift 2, Vereinsname links ab x = 1, Managername rechtsbündig bis x = 317, Grundlinie 10, Farbe 30, Schatten 0.
    /// </summary>
    public static void DrawHeader(Surface s, Assets a, GameState game)
    {
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        CompetitionWidgets.Centered(s, a.Flow, HeaderLeft, 0, HeaderBaseline, club.Name, HeaderColor, 0, left: true);
        var x = HeaderRight - CompetitionWidgets.Width(a.Flow, manager.Name);
        CompetitionWidgets.Centered(s, a.Flow, x, 0, HeaderBaseline, manager.Name, HeaderColor, 0, left: true);
    }
}
