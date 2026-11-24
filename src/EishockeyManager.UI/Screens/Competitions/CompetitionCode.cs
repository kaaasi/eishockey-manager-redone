using System.Runtime.CompilerServices;
using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens.Competitions;

/// <summary>
/// Der Teil des Wettbewerbscodes DGROUP 0xD0C2, den die Gegner- und Vereinsfenster lesen (B15/B16, A286; Spur
/// wettbewerbe-anzeige Teil 2, Beleg docs/analyse/re-abweichungen.md B15/B16 Punkt 4).
/// <list type="bullet">
/// <item>[ORIGINAL] Das Fenster 0x4D385 zeichnet den dritten Knopf (Datei 0x4D7AA) und die Zeile „Historische Ergebnisse gegen …“
///   mit der Liste (Datei 0x4E07A) nur bei Code 0; bei 7, 8 und 13 fehlen beide.</item>
/// <item>[ORIGINAL] Code 0 setzt der Aufbau des Hauptmenüs (Datei 0x44E32) – nicht die Rückkehr aus einer Kachel (Datei 0x45961 →
///   0x44FBD); das Freundschaftsspiel setzt 0xD (Datei 0x30390). Nach einem Freundschaftsspiel bleibt der Code darum bis zum
///   nächsten Aufbau des Hauptmenüs stehen, also bis zum nächsten Zug (nächster Tag oder nächster Manager; DOSBox 44,
///   <c>bundestrainer_00417</c>).</item>
/// <item>Die Spieltagsabläufe (Code 1–8) erreichen das Fenster im Remake nicht: die Live-Anzeige der Paarungen hat kein Fenster
///   (<see cref="CupPairingsScreen"/>), Kacheln gibt es erst nach dem nächsten Aufbau des Hauptmenüs.</item>
/// <item>Zustand der Oberfläche, kein Spielzustand (wie <see cref="WorldCupPairingsPages"/>, L11): nicht im Spielstand. Gemerkt
///   je <see cref="GameState"/>-Objekt als Zug (Saison, Tag, Manager am Zug, Bundestrainer am Zug), in dem zuletzt ein
///   Freundschaftsspiel stattfand. [APPROX] Nach dem Laden gilt Code 0; das Original behält das globale Byte.</item>
/// </list>
/// </summary>
public static class CompetitionCode
{
    private sealed class Memory
    {
        public (int Season, int Day, int Manager, bool Coach)? FriendlyTurn;
    }

    private static readonly ConditionalWeakTable<GameState, Memory> Codes = new();

    private static (int, int, int, bool) Turn(GameState game) =>
        (game.Season, game.CurrentDay, game.CurrentManager, game.NationalCoachMenu);

    /// <summary>[ORIGINAL] Freundschaftsspiel gespielt: Code 0xD (Datei 0x30390) bis zum nächsten Aufbau des Hauptmenüs.</summary>
    public static void FriendlyPlayed(GameState game) => Codes.GetOrCreateValue(game).FriendlyTurn = Turn(game);

    /// <summary>
    /// [ORIGINAL] Code 0 (Hauptmenü): Zeile „Historische Ergebnisse“ und dritter Knopf werden gezeichnet (Datei 0x4D7AA,
    /// 0x4E07A). Ohne Spielstand Code 0.
    /// </summary>
    public static bool IsMainMenu(GameState? game) =>
        game is null || !Codes.TryGetValue(game, out var m) || m.FriendlyTurn != Turn(game);
}
