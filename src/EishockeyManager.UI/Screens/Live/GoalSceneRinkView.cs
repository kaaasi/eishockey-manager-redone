using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.UI.Screens.Live;

/// <summary>
/// Dauer-Eisfeld (A472, Spur torszene-dauer Teil 2; <c>docs/analyse/torszene-dauer.md</c> §2–§6): das Eisfeld, das der
/// Abspieler 0x567C8 in Modus 1 aufbaut und das bis zum Abbau (Modus 3) stehen bleibt. Reine Anzeige, kein Spielzustand.
/// <list type="bullet">
/// <item>[ORIGINAL] Aufbau Modus 1 (Datei 0x56ABC–0x56B4A): Bilder laden (Hallenstufe aus dem Aufruf, mit mehr als einem
///   Manager <c>[0x386]</c> &gt; 1 immer 2, Datei 0x568A9–0x568B6), Palette aus, Bildschirm teilen und Objekte leeren
///   <c>48h:538Ch</c> (Datei 0x12DC5–0x12DEE: Sichtfenster <c>cs:0x10</c>/<c>cs:0x12</c> = 0, alle Figuren 0), Eis zeichnen
///   <c>48h:5438h</c>, Kopfzeile <c>0x56EDD(0, …)</c> mit Kopfbild, Palette <c>kb\247</c> (<c>48h:47F0h</c>), einblenden.</item>
/// <item>[ORIGINAL] Szene Modus 2 (<see cref="GoalSceneScreen"/> mit <see cref="GoalSceneRequest.Continuous"/>): nichts laden,
///   also die Halle des Aufbaus; Trikotfarben und Kopfzeile der Szene (Datei 0x56C29–0x56CC1); danach bleibt das letzte Bild
///   mit der Kopfzeile der Szene stehen (Datei 0x56E37–0x56E3F gibt nur den Szenenpuffer frei, <see cref="Keep"/>).</item>
/// <item>[ORIGINAL] Je Minute nur die Minutenzeile 0x570E8 (Datei 0x37CD5–0x37CEA, <see cref="Minute"/>).</item>
/// <item>[ORIGINAL] Abbau Modus 3 (Datei 0x56E42–0x56ECF): Bilder frei, Palette aus, Hintergrund <c>110h:7Ah</c>, Palette an –
///   im Remake das Ende dieses Objekts.</item>
/// </list>
/// </summary>
public sealed class GoalSceneRinkView
{
    private readonly GoalSceneGraphics _graphics;
    private GoalSceneAnimation? _last;

    private GoalSceneRinkView(GoalSceneGraphics graphics, int hallLevel, GoalSceneHeader header)
    {
        _graphics = graphics;
        HallLevel = hallLevel;
        Header = header;
        Palette = graphics.BasePalette;
        Minute = header.Minute;
    }

    /// <summary>Halle des Aufbaus (gilt für alle Szenen bis zum Abbau).</summary>
    public int HallLevel { get; }

    /// <summary>Kopfzeile: die des Aufbaus, nach einer Szene die der Szene (mit den Toren nach dem Treffer).</summary>
    public GoalSceneHeader Header { get; private set; }

    /// <summary>Minute der Minutenzeile (DGROUP 0xE19E).</summary>
    public int Minute { get; set; }

    public Palette Palette { get; private set; }

    /// <summary>Wie viele Szenen auf diesem Eis liefen.</summary>
    public int Scenes { get; private set; }

    /// <summary>
    /// [ORIGINAL] Hallenstufe des Aufbaus (Datei 0x568A9–0x568B6): mit mehr als einem Manager (DGROUP 0x386) 2, sonst das
    /// Argument.
    /// </summary>
    public static int BuildHallLevel(GameState game, int level) => game.Managers.Count > 1 ? GoalSceneCall.DefaultHallLevel : level;

    /// <summary>
    /// Aufbau für den Spieltag (T1, [ORIGINAL] Datei 0x3710B–0x37171): Kopfzeile Heim <c>DGROUP 0xE2AA</c>, Gast <c>0xE2A9</c>,
    /// 0 : 0, Minute 0, Zuschauertext leer (DGROUP 0x4EA2), Hallenstufe <c>0xE2A8</c>. Die Abrechnung 0x544C0 schreibt die
    /// drei Werte für jede Paarung mit zugesehenem Verein (Datei 0x54C07–0x54C5E): es gilt die letzte solche Paarung in der
    /// Reihenfolge des Ablaufs, die Stufe ist das Kennbyte des Managers des Heimvereins, ohne Manager 2
    /// (<see cref="GoalSceneCall.Spectators"/>). [APPROX] Das Remake nimmt die letzte Paarung mit Anzeigereihe (höchstens vier,
    /// <see cref="LiveScoreboard.MaxRows"/>); mehr zugesehene Paarungen verhindert im Original die Grenze in 0xAE2B.
    /// </summary>
    public static GoalSceneRinkView? ForMatchDay(Assets assets, GameState game, LiveMatchDay day)
    {
        if (day.Matches.LastOrDefault(m => m.Row is not null) is not { } match)
            return null;
        var level = GoalSceneCall.Spectators(game, match.Home, match.Away).HallLevel;
        return Build(assets, game, match.Home, match.Away, 0, level);
    }

    /// <summary>
    /// Aufbau des eigenen Eises des Penaltyschießens (T7, [ORIGINAL] Datei 0x3937F–0x393D8): Vereine der Paarung, 0 : 0,
    /// Minute 70, Hallenstufe 2, Zuschauertext leer.
    /// </summary>
    public static GoalSceneRinkView ForShootout(Assets assets, GameState game, Club home, Club away) =>
        Build(assets, game, home, away, LiveMatchDay.LastMinute, GoalSceneCall.DefaultHallLevel);

    private static GoalSceneRinkView Build(Assets assets, GameState game, Club home, Club away, int minute, int level) =>
        new(GoalSceneCall.GraphicsFor(assets), BuildHallLevel(game, level),
            new GoalSceneHeader(home.Name, away.Name, 0, 0, minute, ""));

    /// <summary>Szene Modus 2 abgeschlossen: ihr letztes Bild, ihre Kopfzeile und Farben bleiben stehen.</summary>
    public void Keep(GoalSceneScreen scene)
    {
        _last = scene.Animation;
        Header = scene.Header;
        Palette = scene.Palette;
        Minute = scene.Header.Minute;
        Scenes++;
    }

    /// <summary>Eis mit dem letzten Szenenbild (nach dem Aufbau leer bei Sichtfenster (0 | 0)), Kopfzeile, Minutenzeile.</summary>
    public void Draw(Surface s, Assets assets)
    {
        s.Palette = Palette;
        if (_last is { } last)
            _graphics.DrawField(s, last, HallLevel);
        else
        {
            var rink = _graphics.Rink(HallLevel).Image;
            s.Blit(rink.Pixels, rink.Width, new Rect(0, 0, GoalSceneRink.ViewWidth, GoalSceneRink.ViewHeight), 0, 0);
        }
        _graphics.DrawHeader(s, assets, Header, 0);
        GoalSceneGraphics.DrawMinute(s, assets, Minute);
    }
}
