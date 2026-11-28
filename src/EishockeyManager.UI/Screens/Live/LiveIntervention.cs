using EishockeyManager.Core;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Screens.Team;

namespace EishockeyManager.UI.Screens.Live;

/// <summary>
/// Eingriff im Spiel über Kopf/Logo (Spur livespiel-ui-2, B3; <c>docs/archiv/analyse/re-livespiel.md</c> §4, §10, §15): Datei 0x5434C
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

    /// <summary>
    /// Wie <see cref="Open"/> für das Nationalteam des Bundestrainers (A455, Spur national Teil 4). [ORIGINAL] Vereinsfeld 0x16 = 6
    /// → 0x5434C mit Manager 5 (Datei 0x38FC0–0x38FED), 0x48E28 ohne Ausnahme: Aufstellung 0x40364 auf den Kaderplätzen des
    /// Satzes 5 mit Kennwert DGROUP 0x2D6 + 5 = 5 bzw. Taktik 0x47F2B mit 1 (<see cref="NationalCoach.ClickPlayerInMatch"/>).
    /// </summary>
    public static Screen OpenNational(ScreenHost host, Screen back, InterventionMode mode) =>
        mode == InterventionMode.Lineup ? NationalLineup(host, back) : NationalTactics(host, back);

    private static Screen NationalLineup(ScreenHost host, Screen back) =>
        new NationalLineupScreen(host, back, () => NationalTactics(host, back)) { InMatch = true };

    private static Screen NationalTactics(ScreenHost host, Screen back) =>
        new TacticsScreen(host, back, () => host.Game!.NationalCoach!.Tactics, () => NationalLineup(host, back)) { InMatch = true };

    private static Screen Lineup(ScreenHost host, Screen back) =>
        new LineupScreen(host, back, () => Tactics(host, back)) { InMatch = true };

    private static Screen Tactics(ScreenHost host, Screen back) =>
        new TacticsScreen(host, back, () => TeamUi.OwnClub(host).Tactics, () => Lineup(host, back)) { InMatch = true };

    /// <summary>
    /// [ORIGINAL] Kopfzeile Datei 0x40290 (nur mit Kennwert ≠ 0, Aufrufe Aufstellung Datei 0x40530 und Taktik Datei 0x480D2):
    /// Schrift 2, Vereinsname links ab x = 1, Managername rechtsbündig bis x = 317, Grundlinie 10, Farbe 30, Schatten 0.
    /// </summary>
    public static void DrawHeader(Surface s, Assets a, GameState game) => DrawHeader(s, a, game, national: false);

    /// <summary>
    /// Kopfzeile für den Taktikbildschirm: das Nationalteam, wenn <paramref name="tactics"/> die Taktik des Bundestrainers ist
    /// (siehe <see cref="DrawHeader(Surface, Assets, GameState, bool)"/>).
    /// </summary>
    public static void DrawHeader(Surface s, Assets a, GameState game, Tactics tactics) =>
        DrawHeader(s, a, game, national: game.NationalCoach is { } coach && ReferenceEquals(coach.Tactics, tactics));

    /// <summary>
    /// Kopfzeile wie oben. [ORIGINAL] Datei 0x40290 liest Verein DGROUP 0x966F + 0x1AF · m und Namen DGROUP 0x965B + 0x1AF · m
    /// des Managers m = DGROUP 0xA70E. Für Manager 5 (Nationalteam, A455) also das Land (0x9EDA := 0x6AC, Datei 0x55482–0x55485) und
    /// den Namen des Bundestrainers (beim Amtsantritt nach 0x9EC6 kopiert, Datei 0x55464–0x5547A).
    /// </summary>
    public static void DrawHeader(Surface s, Assets a, GameState game, bool national)
    {
        var manager = game.ActiveManager;
        var club = national && game.NationalCoach is { } coach ? game.Clubs[coach.Nation] : game.Clubs[manager.ClubIndex];
        if (national && game.NationalCoach is { } c && c.Manager < game.Managers.Count)
            manager = game.Managers[c.Manager];
        CompetitionWidgets.Centered(s, a.Flow, HeaderLeft, 0, HeaderBaseline, club.Name, HeaderColor, 0, left: true);
        var x = HeaderRight - CompetitionWidgets.Width(a.Flow, manager.Name);
        CompetitionWidgets.Centered(s, a.Flow, x, 0, HeaderBaseline, manager.Name, HeaderColor, 0, left: true);
    }
}
