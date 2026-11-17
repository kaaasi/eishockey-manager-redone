namespace EishockeyManager.UI.Screens.Team;

/// <summary>
/// Weg zur Einstellung „automatisch aufstellen“ eines anderen Teams (Bundestrainer) im Taktikbildschirm
/// (<see cref="TacticsScreen"/>; Spur einzelpunkte, A492/A673/A681). <c>Close</c> bekommt den Stand beim Öffnen.
/// </summary>
public sealed record AutoLineupSwitch(Func<bool> Get, Action<bool> Set, Action<bool> Close);
