namespace EishockeyManager.Core;

/// <summary>
/// Hot-Seat (Befund R2-06): Bereitschaftsphase vor jeder Tagesverarbeitung. Jeder aktive (nicht entlassene)
/// Manager ist je Tag genau einmal am Zug und bestätigt mit „und tschüß…“ (<see cref="Continue"/>).
/// Erst die Bestätigung des letzten noch offenen Managers spielt den Tag – genau einmal.
/// Infoleiste: Lampe rot = noch nicht bereit (am Zug bzw. noch dran), grün = bereit.
/// </summary>
/// <remarks>
/// [APPROX] Reihenfolge im Original nicht belegt: Ein neuer Tag beginnt beim ersten aktiven Manager,
/// danach in Setup-Reihenfolge.
/// </remarks>
public sealed partial class GameState
{
    private readonly HashSet<Manager> _readyManagers = [];

    /// <summary>Hat der Manager für den aktuellen Tag schon „und tschüß…“ bestätigt?</summary>
    public bool IsReady(Manager manager) => _readyManagers.Contains(manager);

    /// <summary>Spielt das nächste <see cref="Continue"/> den Tag (alle anderen aktiven Manager sind bereit)?</summary>
    public bool IsLastTurnOfDay =>
        ActiveManagers.All(m => m == ActiveManager || _readyManagers.Contains(m));

    /// <summary>Neue Bereitschaftsphase: niemand bereit, am Zug ist der erste aktive Manager.</summary>
    public void StartTurns()
    {
        _readyManagers.Clear();
        for (var i = 0; i < Managers.Count; i++)
        {
            if (Managers[i].Dismissed)
                continue;
            CurrentManager = i;
            return;
        }
    }

    /// <summary>
    /// Bestätigung des Managers am Zug. Liefert true, wenn der Zug nur weitergegeben wurde (Tag noch nicht spielen),
    /// false, wenn alle aktiven Manager bereit sind und der Tag gespielt werden soll.
    /// </summary>
    private bool PassTurn()
    {
        if (IsLastTurnOfDay)
            return false;
        _readyManagers.Add(ActiveManager);
        for (var step = 1; step <= Managers.Count; step++)
        {
            var next = (CurrentManager + step) % Managers.Count;
            if (!Managers[next].Dismissed && !_readyManagers.Contains(Managers[next]))
            {
                CurrentManager = next;
                return true;
            }
        }
        return false; // nicht erreichbar: IsLastTurnOfDay war false, also gibt es einen offenen Manager
    }
}
