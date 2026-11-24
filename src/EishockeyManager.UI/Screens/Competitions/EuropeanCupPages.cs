using System.Runtime.CompilerServices;
using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens.Competitions;

/// <summary>
/// Gemerkte Seite der Kacheln „Tabelle Europapokal“ (Tabellen, Platz 4) und „Europapokal“ (Paarungen, Platz 4) – B10 (A268,
/// A269), Spur wettbewerbe-anzeige Teil 1.
/// <list type="bullet">
/// <item>[ORIGINAL] „Tabelle Europapokal“ übergibt dem Seitenwähler 0x3A88A (Stub 0xF8:0x3E) die Seite aus DGROUP 0x511F + 4 =
///   0x5123 und merkt dessen Rückgabe dort (Datei 0x45852–0x45865); „Europapokal“ der Paarungen ebenso mit DGROUP 0x5122 + 4 =
///   0x5126 (Datei 0x458A8–0x458BB). Zwei getrennte Bytes, Startwert je 3 (DGROUP 0x5123 = 0x5126 = 3 im Abbild).</item>
/// <item>[ORIGINAL] Seiten 3 und 4 sind die Gruppen 1 und 2 (Sprungtabelle cs:0x12FB, Datei 0x3ACCB: Seite 3/4 → Datei 0x3AB43,
///   Aufruf 0xE8:0x6B mit Wettbewerb 2 und Gruppe = Seite − 3), Seite 5 die Endrunde (Datei 0x3AB8A, Wettbewerb 3). Die Knöpfe
///   des Gruppenbildschirms 0x31CD5 geben die neue Seite zurück (Worttabelle DGROUP 0x4DF0 + 4 · Wettbewerb + 2 · Knopf, Datei
///   0x32A85–0x32A99; Gruppen 4 − Gruppe bzw. 5, Datei 0x31F91–0x31F9A; Endrunde 4 bzw. 3) – also
///   <see cref="EuropeanCupScreen.View"/> + 3.</item>
/// <item>[ORIGINAL] Rechtsklick und Taste beenden mit 0 (Rückgabe 0 bei Knopf −, Datei 0x32A78–0x32A81); der Seitenwähler gibt
///   dann die zuletzt gezeigte Seite ([bp−1], Datei 0x3AA01–0x3AA0A, 0x3ACC4) zurück – beim nächsten Öffnen erscheint die
///   zuletzt gezeigte Ansicht.</item>
/// <item>[ORIGINAL] Der Saisonwechsel setzt 0x5123 … 0x5127 auf 0x5128 … 0x512C zurück (Datei 0x5649D–0x564BA; 0x5128 = 0x512B
///   = 3).</item>
/// <item>Zustand der Oberfläche, kein Spielzustand (wie <see cref="WorldCupPairingsPages"/>, L11): nicht im Spielstand, gemerkt
///   je <see cref="GameState"/>-Objekt. [APPROX] Nach dem Laden beginnt die Seite mit dem Startwert; das Original behält das
///   globale Byte des laufenden Programms.</item>
/// </list>
/// </summary>
public static class EuropeanCupPages
{
    /// <summary>Kachel des Bildschirms: Tabellen (DGROUP 0x5123) oder Paarungen (DGROUP 0x5126).</summary>
    public enum Tile
    {
        Table,
        Pairings,
    }

    /// <summary>Seite 3 des Seitenwählers: Gruppe 1 (Startwert).</summary>
    public const int Group1 = 3;

    private sealed class Memory
    {
        public int Season = -1;
        public int Page = Group1;
        public EuropeanCupScreen? Shown;
    }

    private static readonly ConditionalWeakTable<GameState, Memory[]> Pages = new();

    /// <summary>Gemerkte Seite (3, 4 oder 5) der Kachel; ohne Spielstand die Startseite.</summary>
    public static int Current(GameState? game, Tile tile)
    {
        if (game is null)
            return Group1;
        var m = Of(game, tile);
        return m.Shown is { } shown ? Group1 + shown.View : m.Page;
    }

    /// <summary>Öffnet die Kachel mit der zuletzt gezeigten Seite (Datei 0x45862 bzw. 0x458BB).</summary>
    public static Screen FromTile(ScreenHost host, Screen back, Tile tile)
    {
        var page = Current(host.Game, tile);
        var screen = new EuropeanCupScreen(host, back, page - Group1);
        if (host.Game is { } game)
        {
            var m = Of(game, tile);
            m.Page = page;
            m.Shown = screen; // die Ansicht beim Verlassen ist die Rückgabe des Seitenwählers
        }
        return screen;
    }

    private static Memory Of(GameState game, Tile tile)
    {
        var all = Pages.GetValue(game, _ => [new Memory(), new Memory()]);
        var m = all[(int)tile];
        if (m.Season != game.Season)
        {
            m.Season = game.Season;
            m.Page = Group1; // Saisonwechsel: Startwert (Datei 0x564AF ff.)
            m.Shown = null;
        }
        return m;
    }
}
