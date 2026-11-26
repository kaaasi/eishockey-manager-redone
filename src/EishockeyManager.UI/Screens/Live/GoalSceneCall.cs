using System.Runtime.CompilerServices;
using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.UI.Screens.Competitions;

namespace EishockeyManager.UI.Screens.Live;

/// <summary>
/// Anzeigeteil der Torszene 0x53C57 (Spur torszenen, Teil C3; Datei 0x53DCA–0x5406A, <c>docs/analyse/re-livespiel.md</c> §16):
/// ruft für eine Chance (Art 0/1) den Abspieler <see cref="GoalSceneScreen"/> mit der Szene, die der Spielkern gewählt hat
/// (<see cref="LiveScoreboardRow.Scenes"/>, R18-D3 = A). Reine Anzeige: kein Zug, kein Spielwert (<c>AGENTS.md</c> §4).
/// <list type="bullet">
/// <item>[ORIGINAL] Datei 0x53DD2–0x53DE3: nur mit Darstellung <c>[0x226]</c> ≠ 0 (die Lampe „Schnelldurchlauf“ setzt sie
///   auf 0, <see cref="LiveClock.ToggleLamp"/>); sonst weder Pausen noch Szene.</item>
/// <item>[ORIGINAL] Datei 0x53DE6–0x53DFB: ohne Durchlauf (<c>DGROUP 0x1DB</c> = 0, im Remake immer) Pause 30 =
///   10 Takte; dann <c>DGROUP 0xCC35</c> = 0.</item>
/// <item>[ORIGINAL] Datei 0x53E0C–0x53EE3: Spielstand aus dem Ergebnisbyte der Paarung, bei einem Tor (Art 1) die Seite der
///   Chance um 1 vermindert – das Byte zählt das Tor schon, der Abspieler erhöht am Ende. Das Remake rechnet die Minute vor der
///   Anzeige; der Stand vor der Chance sind die Tore der früheren Minuten und der früheren Chancen dieser Minute
///   (<see cref="LiveMatchScreen"/>), das ist dasselbe.</item>
/// <item>[ORIGINAL] Datei 0x53EE6–0x53F72: Zuschauerzeile und Hallenstufe (<see cref="Spectators"/>).</item>
/// <item>[ORIGINAL] Datei 0x53F86–0x53FC2: Trikotfarben Vereinsfeld 0x3B beider Vereine, entzerrt 0x53BF9
///   (<see cref="GoalSceneGraphics.SeparateColors"/>); Aufruf 0x567C8 mit Modus <c>2 · [0x5A55]</c> (Datei 0x53FCE):
///   Darstellung 2 = Dauer-Eisfeld = Modus 2.</item>
/// <item>[ORIGINAL] Datei 0x54033–0x5406A: <c>DGROUP 0xCC35</c> + 1, Pause 20 = 6 Takte, eine Taste lesen (<c>48h:2E1Eh</c>);
///   bei „r“ oder „w“ (0x72/0x77, nur klein) und weniger als drei Szenen zurück nach Datei 0x53E02 – ohne Pause 30, mit
///   neuem Spielstand und neuer Zuschauerzeile, also höchstens drei Abläufe je Chance.</item>
/// <item>[APPROX] Die Wiederholung zeigt dieselbe Szene noch einmal ohne neuen Zug (R19-E1 = A, Nutzer 14.11.2026). Das
///   Original zieht im Abspieler neu (0x575C3, Datei 0x56BBC); originalgetreu erst mit dem bitgenauen Zufallsmodus.</item>
/// <item>[APPROX] Taste: <c>48h:2E1Eh</c> liest ein Zeichen aus dem Tastaturpuffer; das Remake nimmt das erste Zeichen, das
///   während der Szene oder der Pause 20 getippt wurde.</item>
/// <item>[ORIGINAL] Dauer-Eisfeld (A472, Spur torszene-dauer Teil 2): Bei Darstellung 2 zeigt der Spieltag durchgehend das Eis
///   (Modus 1/3 aus dem Spieltagsablauf 0x36B5D, <see cref="GoalSceneRinkView"/>); die Szene läuft darauf in Modus 2 mit der
///   Halle des Aufbaus (Hallenstufe 2 bei mehreren Managern, Datei 0x568A9–0x568B6) – die Stufe aus <see cref="Spectators"/>
///   gilt dort nur für den Aufbau (<see cref="LiveMatchScreen"/> ersetzt sie im Auftrag).</item>
/// </list>
/// </summary>
public static class GoalSceneCall
{
    /// <summary>[ORIGINAL] Pause vor der Szene (Datei 0x53DF1: <c>50h:19Eh(30)</c>).</summary>
    public const int PauseBefore = 30;

    /// <summary>[ORIGINAL] Pause nach jeder Szene (Datei 0x54042: <c>50h:19Eh(20)</c>).</summary>
    public const int PauseAfter = 20;

    /// <summary>[ORIGINAL] Höchstzahl der Abläufe je Chance (Datei 0x54055: <c>cmp [0xCC35], 3</c>).</summary>
    public const int MaxPlays = 3;

    /// <summary>[ORIGINAL] Zuschauertext ohne Manager (DGROUP 0x54C9) bzw. für Länderteams (DGROUP 0x54D9, 15 Leerzeichen).</summary>
    public const string AwayText = "AUSWÄRTS       ", NationalText = "               ";

    /// <summary>[ORIGINAL] Anhang an die Zuschauerzahl (DGROUP 0x54BE).</summary>
    public const string SpectatorSuffix = " ZUSCHAUER";

    /// <summary>[ORIGINAL] Hallenstufe ohne Manager, für Länderteams und ohne Plätze (Datei 0x53F55, 0x53F5F, 0x54B24).</summary>
    public const int DefaultHallLevel = 2;

    /// <summary>Wiederholt die Taste die Szene? ([ORIGINAL] Datei 0x5405C–0x54067: 0x72 „r“, 0x77 „w“).</summary>
    public static bool IsReplayKey(char? key) => key is 'r' or 'w';

    /// <summary>
    /// [ORIGINAL] Zuschauerzeile und Hallenstufe (Datei 0x53EE6–0x53F72): Heimverein ≥ <c>DGROUP 0x6AD</c> (Länderteam) →
    /// 15 Leerzeichen, Stufe 2; Heimverein mit Manager (Vereinsfeld 0x16) → Zuschauer des Satzes <c>DGROUP 0x5D99 + 6 · m</c>
    /// (+2, long, Umwandlung 0x0BAB2) + „ ZUSCHAUER“ und Kennbyte +0 = ⌊4 · Zuschauer / Plätze⌋ (Spielabrechnung Datei
    /// 0x54AE8–0x54B12, ohne Plätze 2); sonst „AUSWÄRTS“, Stufe 2. Maßgeblich ist immer der Heimverein, auch wenn nur der
    /// Gast einen Manager hat. [ORIGINAL] Zahl **ohne** Tausenderpunkt: 0x0BAB2 → 0x0B954 liest <c>DGROUP 0x38D</c> (Datei
    /// 0x0B98D); dessen Grundzustand ist 1, jeder Schreiber setzt nach Gebrauch 1 zurück (z. B. Datei 0x53ABD, 0x575D3), nur
    /// die Zuschauerzeile des Live-Bildschirms setzt vorher 0 (Datei 0x53A9C). Video <c>eisbaeren_f01_01150</c>: die Zahl liegt
    /// unter der Einblendung des Aufnehmenden (x &lt; 40), „ZUSCHAUER“ beginnt bei x 44 = 8 + Breite „dddd “ (36; mit Punkt
    /// „d.ddd “ wären es 39).
    /// </summary>
    public static (string Text, int HallLevel) Spectators(GameState game, Club home, Club away)
    {
        if (home.Index >= NationalTeams.First)
            return (NationalText, DefaultHallLevel);
        if (game.ActiveManagers.FirstOrDefault(m => m.ClubIndex == home.Index) is not { } manager)
            return (AwayText, DefaultHallLevel);
        var spectators = game.HomeGate(home, away)?.Spectators ?? 0;
        var places = game.Finance(manager).Stadium.Capacity;
        var level = places == 0 ? DefaultHallLevel : Math.Clamp(4 * spectators / places, 0, 4);
        return (Statistics.ClubStatisticsScreen.Number(spectators, 1, false) + SpectatorSuffix, level);
    }

    private static readonly ConditionalWeakTable<Assets, GoalSceneGraphics> Graphics = new();

    /// <summary>Grafik der Torszene je Datensatz (einmal geladen; kein Spielzustand).</summary>
    public static GoalSceneGraphics GraphicsFor(Assets assets) => Graphics.GetValue(assets, a => new GoalSceneGraphics(a.Game));

    /// <summary>Gibt es die Szenen-Dateien (<c>tore\</c>)?</summary>
    public static bool Available(Assets assets) => Directory.Exists(assets.Game.GoalScenesDirectory);

    /// <summary>Auftrag an den Abspieler für eine Chance ([ORIGINAL] Argumente Datei 0x53FC5–0x5402A).</summary>
    public static GoalSceneRequest Request(Assets assets, GameState game, LiveMatch match, ScoreboardScene scene,
        (int Home, int Away) goals, int minute)
    {
        var (text, level) = Spectators(game, match.Home, match.Away);
        var file = GoalScene.Load(Path.Combine(assets.Game.GoalScenesDirectory, scene.Choice.FileName));
        var header = new GoalSceneHeader(match.Home.Name, match.Away.Name, goals.Home, goals.Away, minute, text);
        return new GoalSceneRequest(file, scene.Goal, scene.Side, level, ColorCode(assets, match.Home),
            ColorCode(assets, match.Away), header, game.Options.LiveMode == GameOptions.PermanentRinkLiveMode);
    }

    /// <summary>
    /// [ORIGINAL] Vereinsfeld 0x3B (Farbcode der Datenbank, <c>OriginalExport.League.cs</c>); nach einem Platztausch der Farbcode
    /// des eingetauschten Vereins (0x36169 tauscht +0x3B mit, <see cref="ClubIdentity"/>, #857).
    /// </summary>
    public static int ColorCode(Assets assets, Club club) =>
        ClubIdentity.Of(assets.Database, club) is var i && i < assets.Database.Clubs.Count ? assets.Database[i].ColorCode : 0;
}
