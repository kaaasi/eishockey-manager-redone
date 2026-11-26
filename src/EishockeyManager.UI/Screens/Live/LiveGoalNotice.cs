using EishockeyManager.Core;
using EishockeyManager.UI.Screens.Competitions;

namespace EishockeyManager.UI.Screens.Live;

/// <summary>
/// Tormeldung einer Paarung ohne Anzeigereihe im Live-Bildschirm (Spur zusehen-rest, A832; Kern
/// <see cref="LiveScoreboard.Notices"/>). Reine Anzeige: kein Zug, kein Spielwert (<c>AGENTS.md</c> §4).
/// <list type="bullet">
/// <item>[ORIGINAL] Abschluss 0xA484, Datei 0xAC12–0xAD6E: Schrift 3 (0x0BECD), Heimverein zentriert bei x 245 und „-“
///   (DGROUP 0x495C) zentriert bei x 295, beide Grundlinie 192; Gastverein zentriert bei x 269, Grundlinie 199; je Farbe 1,
///   Schatten 0 (0x0B6A5). Doppelpunkt aus zwei Punkten (i = 0, 3): Rechteck (257 | 207 + i)–(258 | 208 + i) Farbe 0, dann
///   (256 | 206 + i)–(257 | 207 + i) Farbe 4 (0x0B620, Ecken eingeschlossen).</item>
/// <item>[ORIGINAL] Datei 0xAD1D–0xAD4B: Pause 50, Spielstand 0x52190(0, Heim, Gast, 239, 204) – Zehner und Einer bei x, x + 9,
///   x + 21, x + 30 wie die Anzeigetafel –, Pause 70.</item>
/// <item>[ORIGINAL] Datei 0xAD52–0xAD6E: Rechteck (196 | 184) 118 × 32 aus der Hintergrundkopie (VGA-Seite 1, 0x4B00)
///   zurückholen – die Meldung verschwindet samt einem Reportertext darunter.</item>
/// <item>[ORIGINAL] Das Original meldet das Tor während der Minutenrechnung aller Paarungen an der Stelle der Paarung; so zeigt
///   der Live-Bildschirm am Ligaspieltag die Meldungen in Paarungsreihenfolge zwischen den Meldungen der Reihen (S01,
///   <see cref="LiveMatchScreen"/>), an Pokalterminen für die Paarungen mit Minutenweg ebenso (<c>MatchEngine.PlayDate</c>).
///   [APPROX] Paarungen des schnellen Pokalwegs (<c>MatchEngine.FastCupPath</c>, Remake je Paarung statt je Tag) rechnet das
///   Remake nach der Minutenschleife; ihre Tore meldet der Live-Bildschirm nicht. Enter überspringt die Meldungen wie den Rest
///   der Anzeige. Die Hintergrundkopie ist hier das Leeren des Reporterkastens (<see cref="LiveReporter.ClearLines"/>).</item>
/// </list>
/// </summary>
public sealed class LiveGoalNotice
{
    /// <summary>[ORIGINAL] Pause nach den Namen (Datei 0xAD1F: <c>50h:19Eh(50)</c>) und nach dem Spielstand (0xAD46: 70).</summary>
    public const int PauseNames = 50, PauseScore = 70;

    /// <summary>[ORIGINAL] DGROUP 0x495C.</summary>
    public const string Separator = "-";

    /// <summary>[ORIGINAL] Farbe 1 und Schatten 0 der Namen (Datei 0xAC25–0xAC2F).</summary>
    public const byte TextColor = 1, ShadowColor = 0;

    /// <summary>[ORIGINAL] Zurückgeholtes Rechteck (Datei 0xAD52–0xAD5F).</summary>
    public static readonly Rect Area = new(196, 184, 118, 32);

    /// <summary>[ORIGINAL] Spielstand bei (239 | 204) (Datei 0xAD2B–0xAD2E).</summary>
    public const int ScoreX = 239, ScoreY = 204;

    /// <summary>Angezeigte Meldung oder null.</summary>
    public GoalNotice? Current { get; private set; }

    /// <summary>Steht der Spielstand schon (nach der Pause 50)?</summary>
    public bool ScoreShown { get; private set; }

    /// <summary>Ablauf einer Meldung als Bilder; am Ende ist nichts mehr angezeigt.</summary>
    public IEnumerable<bool> Show(GoalNotice notice)
    {
        Current = notice;
        ScoreShown = false;
        yield return true;
        for (var i = 0; i < LiveReporter.PauseFrames(PauseNames); i++)
            yield return true;
        ScoreShown = true;
        yield return true;
        for (var i = 0; i < LiveReporter.PauseFrames(PauseScore); i++)
            yield return true;
        Current = null;
        ScoreShown = false;
    }

    /// <summary>Zeichnet die laufende Meldung (über den Reporterkasten).</summary>
    public void Draw(Surface s, Assets a, GameState game)
    {
        if (Current is { } notice)
            Draw(s, a, game, notice, ScoreShown);
    }

    /// <summary>Zeichnet eine Meldung: Namen, „-“ und Doppelpunkt, mit <paramref name="score"/> auch den Spielstand.</summary>
    public static void Draw(Surface s, Assets a, GameState game, GoalNotice notice, bool score)
    {
        CompetitionWidgets.Centered(s, a.Mini, 245, 245, 192, game.Clubs[notice.HomeClub].Name, TextColor, ShadowColor);
        CompetitionWidgets.Centered(s, a.Mini, 295, 295, 192, Separator, TextColor, ShadowColor);
        CompetitionWidgets.Centered(s, a.Mini, 269, 269, 199, game.Clubs[notice.AwayClub].Name, TextColor, ShadowColor);
        for (var i = 0; i < 4; i += 3)
        {
            s.Fill(new Rect(257, 207 + i, 2, 2), 0);
            s.Fill(new Rect(256, 206 + i, 2, 2), 4);
        }
        if (!score)
            return;
        LiveWidgets.Digit(s, a, 0, notice.HomeGoals / 10 % 10, ScoreX, ScoreY);
        LiveWidgets.Digit(s, a, 0, notice.HomeGoals % 10, ScoreX + 9, ScoreY);
        LiveWidgets.Digit(s, a, 0, notice.AwayGoals / 10 % 10, ScoreX + 21, ScoreY);
        LiveWidgets.Digit(s, a, 0, notice.AwayGoals % 10, ScoreX + 30, ScoreY);
    }
}
