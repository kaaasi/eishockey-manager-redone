namespace EishockeyManager.Core;

/// <summary>
/// Übernahme einer im Spiel geänderten Aufstellung in die Minutenrechnung (Eingriff über Kopf/Logo, Spur livespiel-ui-2 B3;
/// <c>docs/analyse/re-livespiel.md</c> §15). Im Original schreiben Aufstellung und Minutenrechnung dieselben Kaderplätze
/// (DGROUP 0x5DC3 + 0x618 · Manager + 0x34 · Platz); das Remake führt sie getrennt (<see cref="Club.Lineup"/> und
/// <see cref="LiveSquad"/>) und gleicht nach dem Eingriff ab.
/// <list type="bullet">
/// <item>[ORIGINAL] Platz +0x15 aus der Aufstellung (<see cref="LiveSquad.PositionOf"/>); Energie (+0x1B) und Tore des Spiels
///   bleiben.</item>
/// <item>[ORIGINAL] Eingesetzt +0x0A wie in der Aufstellung, außer für Spieler auf der Strafbank (dort +0x0A = 0 bis zum
///   Freiwerden 0x35691) und im Spiel Verletzte (Datei 0x5C50 → +0x0A = 0). [APPROX] Das Original lässt die Aufstellung auch
///   einen Bestraften wieder einsetzen (0x41533 schreibt +0x0A = 1 ohne Blick auf die Strafbank); das Remake hält ihn draußen,
///   bis er frei wird.</item>
/// <item>[ORIGINAL] Danach Feldspieler neu zählen 0x38A36 (Datei 0x5438D–0x54419, <see cref="MatchIntervention.AfterLineup"/>).</item>
/// </list>
/// </summary>
public static class LiveLineup
{
    /// <summary>Aufstellung des Vereins in den Spielkader <paramref name="team"/> übernehmen und neu zählen.</summary>
    public static void Apply(LiveTeam team, Club club)
    {
        if (!team.Managed || team.Squad is not { } squad)
            return;
        var boxed = team.Box.Occupants.Where(o => o is not null).ToHashSet();
        foreach (var p in squad.Players)
        {
            p.Position = club.Lineup.FindSlot(p.Player) is { } slot ? LiveSquad.PositionOf(slot) : 0;
            if (!boxed.Contains(p))
                p.Dressed = p.Player.Available && club.IsDressed(p.Player);
        }
        MatchIntervention.AfterLineup(team);
    }
}
