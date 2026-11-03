using EishockeyManager.Core;
using EishockeyManager.UI.Screens.Competitions;

namespace EishockeyManager.UI.Screens;

/// <summary>
/// Info-Fenster aus der Infoleiste des Hauptmenüs (Spur ui-meldungen, Teil 2, UM-2): Klick auf den Gegnernamen oder auf den
/// eigenen Vereinsnamen öffnet das Gegner-/Vereinsfenster 0x128:0x2A (Datei 0x4D385) mit Seite 2.
/// </summary>
/// <remarks>
/// [ORIGINAL] Hauptmenüschleife, vor dem Infofile (Datei 0x453E5), nur linke Maustaste (DGROUP 0x5A16 = 1):
/// <list type="bullet">
/// <item>Datei 0x45375–0x453AE: 34 &lt; y &lt; 44, 137 &lt; x &lt; 231 und Gegner-Zwischenspeicher DGROUP 0xE1A6 ≠ 0xFF → Fenster
///   für den Gegner (Name in Zeile 1 der Infoleiste).</item>
/// <item>Datei 0x453B4–0x453DF: 21 &lt; y &lt; 31, x &gt; 229 → Fenster für den eigenen Verein ([bp−9]; Bundestrainer: das
///   Nationalteam) – das ist der Vereinsname oben rechts, nicht das Logo („Tag weiter“, <see cref="MainMenuScreen.ContinueLogo"/>).</item>
/// </list>
/// Länder zeigt <see cref="TeamInfoWindow"/>, Vereine und Europapokal-Gegner <see cref="ClubInfoWindow"/>. Öffnen und Schließen
/// ändern keinen Spielzustand.
/// </remarks>
public sealed class InfoBarWindows
{
    public static readonly Rect OpponentName = new(138, 35, 93, 9);
    public static readonly Rect OwnClubName = new(230, 22, 90, 9);

    public TeamInfoWindow Nation { get; } = new();
    public ClubInfoWindow Club { get; } = new();

    public bool Visible => Nation.Visible || Club.Visible;

    public void Show(int team)
    {
        if (team is >= NationalTeams.First and <= NationalTeams.Last)
            Nation.Show(team);
        else
            Club.Show(team);
    }

    /// <summary>Öffnet oder bedient ein Fenster; true, wenn die Eingabe verbraucht ist.</summary>
    public bool Update(GameState game, InputState input)
    {
        if (Nation.Update(input) | Club.Update(input))
            return true;
        if (input.Clicked(OpponentName) && game.UpcomingMatch(NationalMenu.Team(game)) is { } next)
        {
            Show(next.Opponent.Index);
            return true;
        }
        if (input.Clicked(OwnClubName))
        {
            Show(NationalMenu.Team(game));
            return true;
        }
        return false;
    }

    public void Draw(Surface s, Assets a, GameState game)
    {
        Nation.Draw(s, a, game);
        Club.Draw(s, a, game);
    }
}
