using System.Runtime.CompilerServices;
using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens.Competitions;

/// <summary>
/// Gemerkte Seite der Kachel „Paarungen → WM“ (A293, R14NT-05, Entscheidung Leitstelle L11 vom 08.11.2026: wie im Original).
/// <list type="bullet">
/// <item>[ORIGINAL] Die Kachel übergibt dem Seitenwähler 0x3A88A die Seite aus DGROUP 0x5122 + 5 = 0x5127 und merkt dessen
///   Rückgabe dort (Datei 0x458A2–0x458BB): 8/9 WM-Paarungen Gruppe 1/2, 10 WM-Endrunde (<c>docs/reverse-engineering.md</c>,
///   Seitenwähler). Startseite 8.</item>
/// <item>[ORIGINAL] Nur beim ersten Aufbau eines Zugs des Bundestrainers setzt die Gegnersuche des Hauptmenüs (Datei
///   0x44F9E/0x44FB7 → 0x44A48) die Seite bei WM-Stufe DGROUP 0x7AE = 5 auf 10 (Datei 0x44B15–0x44B1C); die Rückkehr ins Menü
///   springt nach 0x44FBD (Datei 0x45961) und umgeht 0x44A48. Danach gilt die zuletzt gezeigte Seite.</item>
/// <item>[ORIGINAL] Der Saisonwechsel setzt die Seiten auf die Startwerte zurück (Datei 0x5649D–0x564BA: 0x5123 … 0x5127 :=
///   0x5128 … 0x512C).</item>
/// <item>Zustand der Oberfläche, kein Spielzustand: nicht im Spielstand (L11). Gemerkt je <see cref="GameState"/>-Objekt.
///   [APPROX] Nach dem Laden beginnt die Seite mit dem Startwert; das Original behält das globale Byte des laufenden Programms.
///   [APPROX] „Erster Aufbau eines Zugs“ wird beim Öffnen der Kachel erkannt (erster Aufruf an diesem Tag im Zug des
///   Bundestrainers); da sich die WM-Stufe innerhalb eines Zugs nicht ändert, gleicht das dem Setzen beim Aufbau.</item>
/// </list>
/// </summary>
internal static class WorldCupPairingsPages
{
    public const int Group1 = 8;
    public const int Group2 = 9;
    public const int Knockout = 10;

    private sealed class Memory
    {
        public int Season = -1;
        public int Page = Group1;
        public (int Season, int Day)? CoachTurn;
    }

    private static readonly ConditionalWeakTable<GameState, Memory> Pages = new();

    /// <summary>Gemerkte Seite (8, 9 oder 10) ohne die Regeln des Zuganfangs; ohne Spielstand die Startseite.</summary>
    public static int Current(GameState? game) => game is null ? Group1 : Of(game).Page;

    /// <summary>Kachel „Paarungen → WM“: gemerkte Seite, beim ersten Aufbau eines Zugs des Bundestrainers mit Stufe 5 die Endrunde.</summary>
    public static Screen FromTile(ScreenHost host, Screen back)
    {
        if (host.Game is { } game)
        {
            var m = Of(game);
            if (game.NationalCoachMenu && m.CoachTurn != (game.Season, game.CurrentDay))
            {
                m.CoachTurn = (game.Season, game.CurrentDay);
                if (game.WorldCupStage == WorldCupKnockoutScreen.CoachStartStage)
                    m.Page = Knockout;
            }
        }
        return Open(host, back, Current(host.Game));
    }

    /// <summary>
    /// Zeigt eine Seite und merkt sie (Rückgabe des Seitenwählers, Datei 0x458BB). „Gruppe“ der Endrunde führt zur Gruppe
    /// <paramref name="previousGroup"/> zurück (Datei 0x3AC75–0x3AC81, vorige Seite; aus der Kachel Gruppe 1, DOSBox `file49_8`).
    /// </summary>
    public static Screen Open(ScreenHost host, Screen back, int page, int previousGroup = 0)
    {
        if (host.Game is { } game)
            Of(game).Page = page;
        return page switch
        {
            Knockout => new WorldCupKnockoutScreen(host, back, () => Open(host, back, GroupPage(previousGroup))),
            Group2 => new CupPairingsScreen(host, back, 1),
            _ => new CupPairingsScreen(host, back, 0),
        };
    }

    /// <summary>Seite einer Gruppe (0 oder 1).</summary>
    public static int GroupPage(int group) => group == 0 ? Group1 : Group2;

    private static Memory Of(GameState game)
    {
        var m = Pages.GetOrCreateValue(game);
        if (m.Season != game.Season)
        {
            m.Season = game.Season;
            m.Page = Group1; // Saisonwechsel: Startwert (Datei 0x564AF)
            m.CoachTurn = null;
        }
        return m;
    }
}
