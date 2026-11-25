namespace EishockeyManager.Core;

/// <summary>
/// Wer eine Paarung mit Anzeigereihe Minute für Minute treibt (Spur livespiel-ui-2, B2; Live-Bildschirm). <see cref="Run"/>
/// bekommt den vorbereiteten Minutenschritt (<see cref="LiveMatchDay"/> mit genau einer Paarung) und darf nur
/// <see cref="LiveMatchDay.Step"/> und die Befehle aus <see cref="MatchIntervention"/> aufrufen; den Abschluss
/// (<see cref="LiveMatchDay.Finish"/>, restliche Minuten) macht der Spielkern nach der Rückkehr.
/// </summary>
public interface ILiveMatchDriver
{
    void Run(LiveMatchDay day);
}

/// <summary>
/// Anschluss des Minutenschritts an den Spieltagsablauf (Spur livespiel-ui-2, B2; <c>docs/analyse/re-livespiel.md</c> §15).
/// <list type="bullet">
/// <item>Ist ein <see cref="ILiveMatchDriver"/> eingesetzt (<see cref="UseLiveDriver"/>, je Thread), läuft jede Paarung mit
///   Anzeigereihe (Managerpaarung, <see cref="LiveScoreboard"/>) in <see cref="PlayWithEvents"/> über
///   <see cref="LiveMatchDay"/> mit derselben Vorgabe und demselben Zufall; der Treiber rechnet die Minuten auf Befehl.
///   Paarungen ohne Reihe, Spielwertungen und der schnelle Weg laufen wie bisher.</item>
/// <item>[ORIGINAL] Ligaspieltag (S01, Spur live-verschraenkt): <see cref="PlayMatchDay"/> rechnet je Minute alle Paarungen des
///   Spieltags (0x37A62); der Treiber bekommt den ganzen Spieltag, alle Managerpaarungen laufen gleichzeitig. Ohne Eingriff ist
///   das Ergebnis über den Live-Bildschirm dasselbe wie ohne Anzeige.</item>
/// <item>[APPROX] Pokal-, Play-off-, Europapokal- und WM-Termine rufen weiter <see cref="PlayWithEvents"/> je Paarung
///   (Ablauf in <c>Playoffs.cs</c>, <c>CupsDeb.cs</c>, <c>CupsGroup.cs</c>, <c>CupsWorld.cs</c> außerhalb der Spur): dort läuft die
///   Paarung mit Reihe für sich durch alle Minuten, mehrere Managerpaarungen eines Termins nacheinander; das Original rechnet
///   auch dort je Minute alle Paarungen der Pokalzeilen (Datei 0x380D0–0x3853E).</item>
/// <item>Zeit vergeht weiter nur über <see cref="GameState.Continue"/>: Der Treiber wird innerhalb dieses Aufrufs gerufen.</item>
/// </list>
/// </summary>
public static partial class MatchEngine
{
    [ThreadStatic]
    private static ILiveMatchDriver? liveDriver;

    /// <summary>Eingesetzter Treiber des laufenden Threads oder null.</summary>
    public static ILiveMatchDriver? LiveDriver => liveDriver;

    /// <summary>Setzt den Treiber für den laufenden Thread, bis das Ergebnis freigegeben wird (der vorige gilt wieder).</summary>
    public static IDisposable UseLiveDriver(ILiveMatchDriver driver)
    {
        ArgumentNullException.ThrowIfNull(driver);
        var previous = liveDriver;
        liveDriver = driver;
        return new Restore(() => liveDriver = previous);
    }

    /// <summary>
    /// Anschluss am Anfang von <see cref="PlayWithEvents"/>: mit Treiber die Paarung über den Minutenschritt spielen, wenn sie
    /// eine Anzeigereihe bekommt; sonst null (dann rechnet <see cref="PlayWithEvents"/> wie bisher).
    /// </summary>
    private static MatchReport? PlayWithDriver(GameState game, Club home, Club away, MatchCompetition competition, bool knockOut,
        Random random)
    {
        if (liveDriver is not { } driver || (!home.WatchedLive && !away.WatchedLive))
            return null;
        var day = LiveMatchDay.Begin(game, [new LiveFixture(home, away, competition, knockOut, random)]);
        var match = day.Matches[0];
        if (match.Stepped && match.Row is not null)
        {
            // Der Treiber darf den Spieltag nicht abschließen; Abschluss hier wie PlayWithEvents (EndMatch).
            var previous = liveDriver;
            liveDriver = null; // keine verschachtelten Treiberaufrufe (Aufstellung im Spiel rechnet keine Spiele)
            try
            {
                driver.Run(day);
            }
            finally
            {
                liveDriver = previous;
            }
        }
        return day.Finish()[0];
    }

    /// <summary>
    /// Ein Spieltag mit Minutenweg über alle Paarungen eines Termins (S01, Spur live-verschraenkt Teil 2;
    /// <c>docs/analyse/live-spieltag.md</c>): [ORIGINAL] 0x36B5D bereitet alle Paarungen vor und ruft die Minutenschleife 0x37A62
    /// je Drittel für alle Paarungen zugleich – Minute für Minute jede Paarung in der Reihenfolge der Vorgabe, alle aus dem
    /// gemeinsamen Zufall (Liga Datei 0x37DB7–0x380CD: Ligen 0 → 2, je Liga die Paarungen des Spieltags). Mit eingesetztem Treiber
    /// und mindestens einer Paarung mit Reihe bekommt der Treiber den ganzen Termin (<see cref="ILiveMatchDriver.Run"/> einmal);
    /// ohne Eingriff ist das Ergebnis dasselbe wie ohne Treiber. Liefert die Berichte in der Reihenfolge der Vorgabe.
    /// </summary>
    public static IReadOnlyList<MatchReport> PlayMatchDay(GameState game, IReadOnlyList<LiveFixture> fixtures)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(fixtures);
        var day = LiveMatchDay.Begin(game, fixtures);
        if (liveDriver is { } driver && day.Matches.Any(m => m.Stepped && m.Row is not null))
        {
            liveDriver = null; // keine verschachtelten Treiberaufrufe
            try
            {
                driver.Run(day);
            }
            finally
            {
                liveDriver = driver;
            }
        }
        return day.Finish();
    }

    private sealed class Restore(Action action) : IDisposable
    {
        private Action? action = action;

        public void Dispose()
        {
            action?.Invoke();
            action = null;
        }
    }
}

public sealed partial class GameState
{
    // Statisch je Thread, kein Instanzfeld: Instanzfelder gehören zum Spielstand (SaveGame.Fields, Schema-Abdruck).
    [ThreadStatic]
    private static List<SeasonStep>? runningSteps;

    /// <summary>
    /// Schritte, die der auf diesem Thread laufende <see cref="Continue"/>-Aufruf bisher gesammelt hat (Spur livespiel-ui-2,
    /// R830-01); außerhalb des Aufrufs leer. Hält der Spielkern an einer Managerpaarung (<see cref="ILiveMatchDriver.Run"/>, auf
    /// demselben Thread), zeigt die Oberfläche diese Schritte vor dem Live-Bildschirm – in derselben Reihenfolge wie ohne
    /// Live-Spiel (z. B. das Banner „Play-Off / Aufstiegsrunden“ vor dem ersten Play-off-Spiel). Kein Spielzustand.
    /// </summary>
    public static IReadOnlyList<SeasonStep> StepsSoFar => runningSteps ?? [];
}
