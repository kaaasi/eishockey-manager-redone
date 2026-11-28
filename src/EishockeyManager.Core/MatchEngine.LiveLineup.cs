namespace EishockeyManager.Core;

/// <summary>
/// Übernahme einer im Spiel geänderten Aufstellung in die Minutenrechnung (Eingriff über Kopf/Logo, Spur livespiel-ui-2 B3;
/// <c>docs/archiv/analyse/re-livespiel.md</c> §15). Im Original schreiben Aufstellung und Minutenrechnung dieselben Kaderplätze
/// (DGROUP 0x5DC3 + 0x618 · Manager + 0x34 · Platz); das Remake führt sie getrennt (<see cref="Club.Lineup"/> und
/// <see cref="LiveSquad"/>) und gleicht nach dem Eingriff ab.
/// <list type="bullet">
/// <item>[ORIGINAL] Platz +0x15 aus der Aufstellung (<see cref="LiveSquad.PositionOf"/>); Energie (+0x1B) und Tore des Spiels
///   bleiben.</item>
/// <item>[ORIGINAL] Eingesetzt +0x0A wie in der Aufstellung, außer für Spieler auf der Strafbank (dort +0x0A = 0 bis zum
///   Freiwerden 0x35691) und im Spiel Verletzte (Datei 0x5C50 → +0x0A = 0).</item>
/// <item>A443: [ORIGINAL] Das Aufstellen 0x41533 schreibt +0x0A = 1 ohne Blick auf die Strafbank (Datei 0x415AF–0x415B2), aber
///   der Klick der Aufstellung erreicht es für einen Bestraften nicht: Jede Strafe setzt Status +0x09 Bit 0 und +0x0D = Minuten
///   (Datei 0x35BFD–0x35C0C), und der Klick auf einen Spieler mit Status 1 oder 2 meldet nur und bricht ab (Datei
///   0x40DAF–0x40E2A). Ein Bestrafter bleibt also bis zum Freiwerden draußen – wie hier. A808: Die Aufstellung im Spiel sperrt
///   den Klick ebenso (<see cref="Club.ClickPlayerInMatch"/> über <see cref="MatchEngine.PenaltyBoxMinutes"/>); der Abgleich
///   hält ihn zusätzlich draußen, falls er vor der Strafe aufgestellt war und seinen Platz behält.</item>
/// <item>A677: [ORIGINAL] Ein im Spiel neu Aufgestellter ohne Kennzeichen „hat gespielt“ (+0x1A Bit 7) bekommt es und ein Spiel
///   (Datei 0x415B7–0x41622, <see cref="LivePlayer.Played"/>); wer herausgenommen wird, behält sein Spiel. [ORIGINAL] S07 (A898):
///   gezählt wird sofort beim Aufstellen (<see cref="Club.ClickPlayerInMatch"/>, <see cref="MatchEngine.CountPlacedInMatch"/>), auch
///   wenn der Spieler im selben Eingriff wieder herauskommt; die Aufgestellten beim Spielbeginn zählen vor den Minuten
///   (<c>MatchEngine.LiveCredit.cs</c>).</item>
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
            var position = club.Lineup.PositionOf(p.Player);
            var touched = position != p.Position;
            p.Position = position;
            if (!boxed.Contains(p))
                p.Dressed = p.Player.Available && club.IsDressed(p.Player);
            // A677; S07: mit Zählung beim Spielbeginn sofort gezählt (MatchEngine.LiveCredit.cs), sonst am Spielende.
            if (touched && position != 0 && p.Dressed && !p.Played && !MatchEngine.CountPlacedInMatch(club, p.Player))
                p.Played = p.PlayedFromBench = true;
        }
        MatchIntervention.AfterLineup(team);
    }
}
