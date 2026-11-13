using EishockeyManager.Formats;

namespace EishockeyManager.Core;

/// <summary>
/// Aufstellung während eines Live-Spiels (Eingriff über Kopf/Logo, Spur livespiel-ui-2 B3; <c>docs/analyse/re-livespiel.md</c>
/// §10 und §15). Die Aufstellung 0x40364 bekommt im Spiel als Kennwert DGROUP 0x2D6 + Manager (≠ 0) und prüft ihn an acht
/// Stellen; ohne Kennwert gelten die Regeln von <see cref="ClickPlayer"/>.
/// <list type="bullet">
/// <item>[ORIGINAL] Datei 0x40E2A: die Grenze „22 eingesetzte Spieler“ gilt nur außerhalb des Spiels.</item>
/// <item>[ORIGINAL] Datei 0x4115F–0x4116F (Kaderliste) und 0x40CFD (Platz leeren): Ein aus seinem Platz genommener Spieler bleibt
///   im Spiel eingesetzt (+0x0A = Kennwert ≠ 0 → 1).</item>
/// <item>[ORIGINAL] Datei 0x40F35–0x40F42: im Spiel geht die rechte Taste nicht in den Ein-/Aussetz-Zweig 0x40F45, sondern in den
///   Aufstellweg 0x41273; dort Datei 0x41287–0x4129E: ein eingesetzter Aufgestellter (+0x0A ≠ 0, +0x15 ≠ 0) bleibt im Spiel
///   unverändert. Rechtsklick auf einen Aufgestellten ändert also nichts, Rechtsklick auf einen anderen Spieler stellt ihn wie
///   der Linksklick in die gewählte Reihe.</item>
/// <item>[ORIGINAL] Datei 0x4135E und 0x413E8 reichen den Kennwert an das Aufstellen 0x41533 weiter: im Spiel bekommt ein neu
///   Aufgestellter ohne Kennzeichen „hat gespielt“ (Spielerfeld +0x1A Bit 7) das Kennzeichen und sofort ein Spiel im
///   laufenden Wettbewerb (+0x06 + k, +0x1C + 2k nach DGROUP 0xD0C2, Datei 0x415B7–0x41622). [APPROX] Das Remake zählt die Spiele
///   eines Managervereins am Spielende aus der Aufstellung (<c>MatchEngine.CreditLineup</c>, Datei 0x5494C): Wer während des
///   Spiels herausgenommen wird, zählt nicht, wer hineinkommt, zählt erst am Ende.</item>
/// <item>[APPROX] Bei Rechtsklick ohne gewählte Reihe meldet das Original nichts (Meldung „Keine Reihe“ nur für die linke Taste,
///   Datei 0x40E63); Art-Prüfung und Plätze wie beim Linksklick (<see cref="ClickPlayer"/>, auch dessen [APPROX] zum sechsten
///   Feldspielerplatz 0x413E8–0x414B4).</item>
/// </list>
/// </summary>
public sealed partial class Club
{
    /// <summary>Klick auf einen Spieler der Kaderliste im Spiel (siehe Klasse; ohne Spiel <see cref="ClickPlayer"/>).</summary>
    public LineupResult ClickPlayerInMatch(Player player, ref int? line, bool right)
    {
        if (!Players.Contains(player))
            return LineupResult.NotInSquad;
        DisableAutoLineup();
        if (player.InjuredGames > 0)
            return LineupResult.Injured;
        if (player.SuspendedGames > 0)
            return LineupResult.Suspended;
        if (Lineup.FindSlot(player) is { } from)
        {
            if (right)
                return LineupResult.Unchanged; // [ORIGINAL] Datei 0x41287–0x4129E
            TakeOverDressed();
            Lineup.Clear(from);
            _dressed.Add(player.Id); // [ORIGINAL] Datei 0x4115F: +0x0A = Kennwert ≠ 0
            line = Lineup.LineOf(from);
            return LineupResult.TakenOut;
        }
        if (line is not { } l)
            return right ? LineupResult.Unchanged : LineupResult.NoLineSelected;
        if ((player.Role == PlayerRole.Goalkeeper) != (l == Lineup.GoalkeeperLine))
            return LineupResult.WrongRole;
        foreach (var slot in Lineup.SlotsOfLine(l, player.Role))
        {
            if (Lineup[slot] is not null)
                continue;
            // [ORIGINAL] Datei 0x41533: Platz +0x15 und eingesetzt +0x0A = 1, ohne Grenze 22 (Datei 0x40E2A).
            TakeOverDressed();
            if (!Lineup.Assign(slot, player))
                return LineupResult.WrongRole;
            _dressed.Add(player.Id);
            return LineupResult.Assigned;
        }
        return LineupResult.NoRoom;
    }

    /// <summary>Platz leeren im Spiel: der Spieler bleibt eingesetzt ([ORIGINAL] Datei 0x40CFD).</summary>
    public void ClearLineupSlotInMatch(LineupSlot slot)
    {
        if (Lineup[slot] is not { } player)
            return;
        TakeOverDressed();
        Lineup.Clear(slot);
        _dressed.Add(player.Id);
        DisableAutoLineup();
    }
}
