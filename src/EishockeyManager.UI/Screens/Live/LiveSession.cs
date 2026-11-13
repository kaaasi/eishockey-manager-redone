using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens.Live;

/// <summary>
/// „Tag weiter“ mit Live-Spiel (Spur livespiel-ui-2, B2): <see cref="GameState.Continue"/> läuft auf einem eigenen Thread mit
/// dem Treiber <see cref="ILiveMatchDriver"/> (<c>MatchEngine.LiveDriver.cs</c>). Kommt eine Managerpaarung mit Anzeigereihe,
/// hält der Spielkern an und übergibt den Minutenschritt (<see cref="Day"/>); der Live-Bildschirm rechnet jede Minute mit
/// <see cref="Step"/> und gibt mit <see cref="Release"/> zurück, dann läuft der Tag weiter bis zur nächsten Managerpaarung
/// oder bis <see cref="GameState.Continue"/> fertig ist (<see cref="Steps"/>).
/// <para>
/// Es rechnet immer nur ein Thread: Die Oberfläche wartet, während der Spielkern rechnet, und der Spielkern wartet, während
/// die Oberfläche zeichnet oder Befehle (<see cref="MatchIntervention"/>) gibt. Spielzustand ändert sich also nur in
/// <see cref="GameState.Continue"/> (AGENTS.md §4) – in den Minuten, die der Bildschirm anstößt.
/// </para>
/// </summary>
public sealed class LiveSession : ILiveMatchDriver
{
    private enum Command { None, Step, Release }

    private readonly SemaphoreSlim _toWorker = new(0, 1);
    private readonly SemaphoreSlim _toUi = new(0, 1);
    private Command _command;
    private bool _stepResult;
    private Exception? _error;

    private readonly GameState _game;

    private LiveSession(GameState game) => _game = game;

    /// <summary>Wartender Minutenschritt (eine Managerpaarung) oder null.</summary>
    public LiveMatchDay? Day { get; private set; }

    /// <summary>Ergebnis von <see cref="GameState.Continue"/>, sobald der Aufruf fertig ist; sonst null.</summary>
    public IReadOnlyList<SeasonStep>? Steps { get; private set; }

    /// <summary>
    /// Paarungen mit Anzeigereihe des laufenden Spieltags in ihrer Reihenfolge (Reihen p = 0 … 3 des Live-Bildschirms), auch die
    /// schon fertigen; beginnt mit jedem neuen Tag oder Wettbewerb neu (wie <see cref="LiveScoreboard"/>).
    /// </summary>
    public IReadOnlyList<LiveMatch> Shown => _shown;

    private readonly List<LiveMatch> _shown = [];
    private (int Day, MatchCompetition Competition)? _shownKey;

    /// <summary>Spieltage (<see cref="GameState.Day"/>) und Wettbewerbe, in denen eine Paarung live lief.</summary>
    public HashSet<(int Day, MatchCompetition Competition)> LiveDays { get; } = [];

    /// <summary>Ist <see cref="GameState.Continue"/> fertig?</summary>
    public bool Completed => Steps is not null;

    /// <summary>Startet „Tag weiter“ und wartet, bis der Spielkern zum ersten Mal anhält oder fertig ist.</summary>
    public static LiveSession Start(GameState game)
    {
        ArgumentNullException.ThrowIfNull(game);
        var session = new LiveSession(game);
        var thread = new Thread(() => session.Work(game)) { IsBackground = true, Name = "Tag weiter (Live)" };
        thread.Start();
        session.WaitForWorker();
        return session;
    }

    /// <summary>Rechnet die nächste Minute (<see cref="LiveMatchDay.Step"/>) im Spielkern; false, wenn das Spiel aus ist.</summary>
    public bool Step()
    {
        if (Day is null)
            return false;
        _command = Command.Step;
        _toWorker.Release();
        WaitForWorker();
        return _stepResult;
    }

    /// <summary>Gibt die Paarung an den Spielkern zurück (Abschluss, restliche Minuten) und wartet auf den nächsten Halt.</summary>
    public void Release()
    {
        if (Day is null)
            return;
        _command = Command.Release;
        _toWorker.Release();
        WaitForWorker();
    }

    void ILiveMatchDriver.Run(LiveMatchDay day)
    {
        Day = day;
        var key = (_game.Day, day.Matches[0].Competition);
        LiveDays.Add(key);
        if (_shownKey != key)
            _shown.Clear();
        _shownKey = key;
        _shown.Add(day.Matches[0]);
        _toUi.Release();
        while (true)
        {
            _toWorker.Wait();
            if (_command == Command.Release)
            {
                Day = null;
                return; // der Spielkern rechnet weiter; der nächste Halt oder das Ende gibt _toUi frei
            }
            _stepResult = day.Step();
            _toUi.Release();
        }
    }

    private void Work(GameState game)
    {
        try
        {
            using (MatchEngine.UseLiveDriver(this))
                Steps = game.Continue();
        }
        catch (Exception e)
        {
            _error = e;
            Steps = [];
        }
        finally
        {
            Day = null;
            _toUi.Release();
        }
    }

    private void WaitForWorker()
    {
        _toUi.Wait();
        if (_error is { } e)
        {
            _error = null;
            throw new InvalidOperationException("Fehler im Spieltag (Live).", e);
        }
    }
}
