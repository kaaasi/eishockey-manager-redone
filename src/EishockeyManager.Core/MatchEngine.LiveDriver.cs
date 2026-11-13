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
/// <item>[APPROX] Zufall: Das Original rechnet je Minute alle Paarungen des Spieltags (0x37A62), das Remake Paarung für
///   Paarung aus dem gemeinsamen Spielzufall (<see cref="GameState.PlayNextMatchDay"/>, Wettbewerbe). Der Anschluss behält
///   diese Reihenfolge: Die Paarung mit Reihe läuft für sich durch alle Minuten, bevor die nächste Paarung beginnt. Ohne
///   Eingriff ist das Ergebnis eines Spieltags über den Live-Bildschirm damit genau das ohne Anzeige (Minutenschritt einer
///   Paarung = <see cref="PlayWithEvents"/>, Spur livespiel-kern A5); mehrere Managerpaarungen eines Spieltags laufen
///   nacheinander statt gleichzeitig.</item>
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
        if (liveDriver is not { } driver || (!IsManaged(game, home) && !IsManaged(game, away)))
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
