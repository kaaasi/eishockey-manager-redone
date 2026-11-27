namespace EishockeyManager.Core;

/// <summary>
/// Spielberechnung: <see cref="PlayWithEvents"/> – Berechnung des Originals Minute für Minute mit Ereignissen und Spielerzählern
/// (Spur spielberechnung, <c>MatchEngine.Live.cs</c>); Liga, Play-offs, Aufstiegsrunde, DEB-Pokal, Europapokal, WM und die
/// Länder-Freundschaftsspiele (seit Runde 17 mit Wettbewerbscode 0xD, <see cref="MatchCompetition.Friendly"/>, A008/A110).
/// Der frühere Poisson-Ergebnisweg ohne Aufrufer im Spielablauf ist entfallen (Spur aufraeumen-tot, Konzept K3).
/// </summary>
public static partial class MatchEngine
{
}
