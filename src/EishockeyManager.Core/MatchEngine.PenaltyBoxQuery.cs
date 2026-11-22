using System.Runtime.CompilerServices;

namespace EishockeyManager.Core;

/// <summary>
/// A808 (Spur wettbewerbe-regeln, Teil-V; Entscheidung Leitstelle 22.11.2026): Strafbank-Stand eines Spielers in der laufenden
/// Paarung als Abfrage für die Aufstellung im Spiel.
/// <list type="bullet">
/// <item>[ORIGINAL] Jede Strafe eines Feldspielers setzt Kaderplatz +0x0A = 0, Status +0x09 Bit 0 und +0x0D = Strafcode &amp; 7,
///   also 2 bzw. 5 Minuten (Datei 0x35BFD–0x35C0C). Das Freiwerden 0x35691 setzt +0x0A zurück, löscht Bit 0 und +0x0D (Datei
///   0x35791–0x3579A). Status 1 gilt also genau, solange der Spieler auf einem Platz der Strafbank sitzt
///   (<see cref="PenaltyBox.Occupants"/>).</item>
/// <item>[ORIGINAL] Im Spiel zählt +0x0D nicht herunter: die beiden Abzüge stehen in der Spielabrechnung nach dem Spiel (Datei
///   0x5487C–0x5488C und 0x548EB–0x5490C, nur Liga). Die Hinweiszeile zeigt also die volle Strafe (2 bzw. 5), nicht die
///   Restzeit der Strafbank.</item>
/// <item>[APPROX] Nur Vereine; die Nationalaufstellung des Bundestrainers (<c>NationalLineupScreen</c>) fragt nicht ab. Was nach
///   dem Spiel mit +0x0D/Bit 0 eines bei Spielende noch Bestraften geschieht (Abzug 0x5490C, Löschen 0x551D4–0x551E0), bildet
///   das Remake nicht ab: nach dem Spiel ist niemand mehr auf der Strafbank.</item>
/// </list>
/// Darstellungszustand der laufenden Paarung, nicht im Spielstand (wie <see cref="LiveTeam"/>).
/// </summary>
public static partial class MatchEngine
{
    private static readonly ConditionalWeakTable<Club, LiveTeam> PenaltyBoxTeams = new();

    /// <summary>Anschluss in <see cref="BeginMatch"/>: Mannschaft der neuen Paarung des Vereins merken bzw. vergessen.</summary>
    private static void TrackPenaltyBox(Club club, LiveTeam? team)
    {
        if (team is null)
            PenaltyBoxTeams.Remove(club);
        else
            PenaltyBoxTeams.AddOrUpdate(club, team);
    }

    /// <summary>
    /// Strafminuten (+0x0D: 2 oder 5) eines Spielers des Vereins, der in der laufenden bzw. letzten Paarung des Vereins auf der
    /// Strafbank sitzt (Status +0x09 Bit 0), sonst 0. Nur im Spiel sinnvoll (Aufstellung mit Kennwert „im Spiel“).
    /// </summary>
    public static int PenaltyBoxMinutes(Club club, Player player)
    {
        ArgumentNullException.ThrowIfNull(club);
        if (!PenaltyBoxTeams.TryGetValue(club, out var team))
            return 0;
        foreach (var occupant in team.Box.Occupants)
            if (occupant is not null && ReferenceEquals(occupant.Player, player))
                return occupant.PenaltyMinutes;
        return 0;
    }
}
