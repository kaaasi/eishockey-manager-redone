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
///   laufenden Wettbewerb (+0x06 + k, +0x1C + 2k nach DGROUP 0xD0C2, Datei 0x415B7–0x41622). Das Remake zählt die Spiele
///   eines Managervereins am Spielende aus der Aufstellung (<c>MatchEngine.CreditLineup</c>, Datei 0x5494C; ein im Spiel neu Aufgestellter zählt mit dem Wettbewerbsfeld, <c>PlayedFromBench</c>): Wer während des
///   Spiels herausgenommen wird, zählt nicht, wer hineinkommt, zählt erst am Ende (Zählzeitpunkt, A898).</item>
/// <item>A808: [ORIGINAL] Datei 0x40DAF–0x40E2A: ein Spieler auf der Strafbank (Status +0x09 &amp; 3 = 1, Datei 0x35C02) ergibt für
///   beide Tasten nur die Meldung „Spieler ist“ / „Gesperrt.“ (<see cref="LineupResult.Suspended"/>, Abfrage
///   <see cref="MatchEngine.PenaltyBoxMinutes"/>); er bleibt draußen, bis die Strafbank ihn freigibt.</item>
/// <item>[ORIGINAL] #939 (A937): Der sechste Feldspielerplatz gilt im Spiel wie außerhalb. Die Abweisung „Schon 6 Feldspieler / auf
///   dem Eis !“ (Datei 0x40EC4–0x40EFF: Torhüter, Torhüterreihe, Platz offen, linke Taste) prüft den Kennwert nicht, und das Öffnen
///   (Datei 0x413C9–0x414B4) reicht ihn nur an das Aufstellen 0x41533 weiter (Datei 0x413E8). Danach zählt die Neuzählung 0x38A36
///   den sechsten Feldspieler über +0x15 = 1 als dritten Verteidiger der Reihe 1 (<see cref="MatchEngine.RecountSkaters"/>).</item>
/// <item>[APPROX] Bei Rechtsklick ohne gewählte Reihe meldet das Original nichts (Meldung „Keine Reihe“ nur für die linke Taste,
///   Datei 0x40E63); Art-Prüfung und Plätze wie beim Linksklick (<see cref="ClickPlayer"/>).</item>
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
        // A808: [ORIGINAL] Status 1 (Strafbank, +0x09 Bit 0) meldet nur „Gesperrt.“ und bricht ab (Datei 0x40DAF–0x40E2A).
        if (MatchEngine.PenaltyBoxMinutes(this, player) > 0)
            return LineupResult.Suspended;
        // [ORIGINAL] Datei 0x40EC4–0x40EFF, ohne Blick auf den Kennwert (siehe Klasse).
        if (!right && player.Role == PlayerRole.Goalkeeper && line == Lineup.GoalkeeperLine && Lineup.SixthSkaterOpen)
            return LineupResult.SixSkaters;
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
        foreach (var slot in Lineup.SearchSlots(l, player.Role))
        {
            if (Lineup[slot] is not null)
                continue;
            // [ORIGINAL] Datei 0x41533: Platz +0x15 und eingesetzt +0x0A = 1, ohne Grenze 22 (Datei 0x40E2A); andere Hälfte der
            // Reihe wie beim Linksklick (Datei 0x414D3, A683).
            TakeOverDressed();
            if (!(slot.Role == player.Role ? Lineup.Assign(slot, player) : Lineup.PlaceSkater(slot, player)))
                return LineupResult.WrongRole;
            _dressed.Add(player.Id);
            return LineupResult.Assigned;
        }
        // [ORIGINAL] A937: sechster Feldspielerplatz (Datei 0x413C9–0x414B4), Kennwert an 0x41533 (Datei 0x413E8): eingesetzt.
        if (Lineup.TryOpenSixthSkater(l, player))
        {
            TakeOverDressed();
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
