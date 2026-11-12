using System.Runtime.CompilerServices;
using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens.Competitions;

/// <summary>
/// Seiten der Kacheln „Paarungen 1. Liga / 2. Liga / Oberliga“ und ihre gemerkte Seite (A280, Spur ui-rest Teil 1).
/// <list type="bullet">
/// <item>[ORIGINAL] Die drei Kacheln übergeben dem Seitenwähler 0x3A88A die Seite aus DGROUP 0x5125 (eine Stelle für alle drei
///   Ligen) mit der Liga als Wettbewerb und merken dessen Rückgabe dort (Datei 0x4587E–0x45891; Sprungtabelle cs:0x290F,
///   Datei 0x45A5F: Plätze 0–2 → 0x4587E). Einziger Leser und Schreiber von 0x5125 (Querverweis 0x5125).</item>
/// <item>[ORIGINAL] Seite 7 = Ligapaarungen 0x128:0x39 (<see cref="LeaguePairingsScreen"/>); ihr mittlerer Knopf gibt 1 zurück
///   (Datei 0x4F059–0x4F06A: Knopf 3, Rückgabe [bp−0x11] = 1). Seite 1 (Datei 0x3AA21): Wettbewerb 0 (Bundesliga) →
///   „Play-Off Spiele“ 0x32E23 mit Wettbewerb 2 (<see cref="PlayoffGamesScreen"/>), sonst „Aufstieg“ 0x31CD5 mit Wettbewerb 1
///   und Gruppe Liga − 1 (Datei 0x3AAA4–0x3AB40, <see cref="PromotionRoundScreen"/>). Deren Knopf „Liga“ gibt 7 zurück
///   (0x32E23: (1 &lt;&lt; 2) + 3, Datei 0x33A99–0x33AAC; 0x31CD5: Worttabelle DGROUP 0x4DF0 + 4 · 1 = 7, 7, Datei 0x32A85).</item>
/// <item>[ORIGINAL] Rechtsklick (und Taste) beendet die Seite mit 0; der Seitenwähler gibt dann die zuletzt gezeigte Seite
///   ([bp−1], Datei 0x3AA01–0x3AA0A, 0x3ACC4) zurück, die Kachel merkt sie. Wer die Play-Off-Seite mit Rechtsklick verlässt,
///   landet beim nächsten Öffnen jeder der drei Kacheln auf Seite 1 dieser Liga (2. Liga: „Aufstieg  1.Liga“).</item>
/// <item>[ORIGINAL] Startwert 7 (DGROUP 0x5125 im Abbild); der Saisonwechsel setzt 0x5123 … 0x5127 auf 0x5128 … 0x512C zurück
///   (Datei 0x5649D–0x564BA, 0x512A = 7).</item>
/// <item>Zustand der Oberfläche, kein Spielzustand (wie <see cref="WorldCupPairingsPages"/>, L11): nicht im Spielstand, gemerkt
///   je <see cref="GameState"/>-Objekt. [APPROX] Nach dem Laden beginnt die Seite mit dem Startwert; das Original behält das
///   globale Byte des laufenden Programms.</item>
/// </list>
/// </summary>
public static class LeaguePairingsPages
{
    /// <summary>Seite 7 des Seitenwählers: Ligapaarungen.</summary>
    public const int Pairings = 7;

    /// <summary>Seite 1 des Seitenwählers: „Play-Off Spiele“ (Bundesliga) bzw. „Aufstieg“ (übrige Ligen).</summary>
    public const int Final = 1;

    private sealed class Memory
    {
        public int Season = -1;
        public int Page = Pairings;
    }

    private static readonly ConditionalWeakTable<GameState, Memory> Pages = new();

    /// <summary>Gemerkte Seite (7 oder 1); ohne Spielstand die Startseite.</summary>
    public static int Current(GameState? game) => game is null ? Pairings : Of(game).Page;

    /// <summary>Kachel „Paarungen &lt;Liga&gt;“: die gemerkte Seite für diese Liga.</summary>
    public static Screen FromTile(ScreenHost host, Screen back, League league) => Open(host, back, league, Current(host.Game));

    /// <summary>Zeigt Seite <paramref name="page"/> der Liga und merkt sie (Datei 0x45891).</summary>
    public static Screen Open(ScreenHost host, Screen back, League league, int page)
    {
        if (host.Game is { } game)
            Of(game).Page = page;
        if (page != Final)
            return new LeaguePairingsScreen(host, back, league);
        return league == League.Bundesliga
            ? new PlayoffGamesScreen(host, back)
            : new PromotionRoundScreen(host, back, league);
    }

    private static Memory Of(GameState game)
    {
        var m = Pages.GetOrCreateValue(game);
        if (m.Season != game.Season)
        {
            m.Season = game.Season;
            m.Page = Pairings; // Saisonwechsel: Startwert (Datei 0x564AF, 0x512A = 7)
        }
        return m;
    }
}
