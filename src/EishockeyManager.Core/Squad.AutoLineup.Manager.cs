using System.Runtime.CompilerServices;

namespace EishockeyManager.Core;

public sealed partial class Manager
{
    /// <summary>
    /// Einstellung „automatisch aufstellen“ des Managers (DGROUP 0x375 + 2 · Manager). [ORIGINAL] A936: Die Neuaufstellung 0x426BC liest
    /// das Byte des Managers (Datei 0x426C4–0x426E4, Manager 1 → 0x377; Beleg <c>k07_automatik.py</c>), das Abschalten in der Aufstellung
    /// schreibt es (Datei 0x40ADE, 0x40DE4), der Taktikbildschirm schaltet es um (Datei 0x47CA0). Startwert 1 (statisch, Datei 0x16DD0 +
    /// 0x375). Die Einstellung bleibt beim Manager, nicht beim Verein; der Verein des Managers liest und schreibt sie über
    /// <see cref="Club.AutoLineupEnabled"/>.
    /// </summary>
    public bool AutoLineupEnabled { get; set; } = true;
}

public sealed partial class Club
{
    /// <summary>
    /// Manager eines Vereins für „automatisch aufstellen“ (A936). Kein Spielstandfeld: die Zuordnung folgt aus
    /// <see cref="Manager.ClubIndex"/> und wird bei Spielstart, Hinzufügen und Laden gesetzt (<see cref="GameState.BindLineupOwners"/>).
    /// </summary>
    private static readonly ConditionalWeakTable<Club, Manager> s_lineupOwners = new();

    /// <summary>Manager dieses Vereins, solange er ihn noch führt (<see cref="Manager.ClubIndex"/> = <see cref="Index"/>), sonst null.</summary>
    internal Manager? LineupOwner => s_lineupOwners.TryGetValue(this, out var manager) && manager.ClubIndex == Index ? manager : null;

    internal void BindLineupOwner(Manager? manager)
    {
        if (manager is null)
            s_lineupOwners.Remove(this);
        else
            s_lineupOwners.AddOrUpdate(this, manager);
    }

    /// <summary>
    /// Einstellung „automatisch aufstellen“, die für diesen Verein gilt: die des Managers (A936), ohne Manager die des Vereins. Schreiben
    /// setzt beide, damit das Vereinsfeld den Wert des Managers spiegelt.
    /// </summary>
    private bool AutoSetting
    {
        get => LineupOwner?.AutoLineupEnabled ?? _autoLineup;
        set
        {
            _autoLineup = value;
            if (LineupOwner is { } manager)
                manager.AutoLineupEnabled = value;
        }
    }
}

public sealed partial class GameState
{
    /// <summary>
    /// Ordnet jedem Verein eines Managers diesen Manager für „automatisch aufstellen“ zu (A936, <see cref="Club.LineupOwner"/>); das
    /// Vereinsfeld übernimmt den Wert des Managers. Wechselt ein Manager den Verein, nimmt er die Einstellung mit.
    /// </summary>
    public void BindLineupOwners()
    {
        foreach (var club in Clubs)
        {
            var manager = Managers.FirstOrDefault(m => m.ClubIndex == club.Index);
            club.BindLineupOwner(manager);
            if (manager is not null)
                club.AutoLineupEnabled = manager.AutoLineupEnabled;
        }
    }
}
