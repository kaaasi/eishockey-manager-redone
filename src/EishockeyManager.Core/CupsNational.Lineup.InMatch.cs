using EishockeyManager.Formats;

namespace EishockeyManager.Core;

/// <summary>
/// Aufstellung des Nationalteams während eines Live-Spiels (Eingriff über Kopf/Logo, A455, Spur national Teil 4).
/// <list type="bullet">
/// <item>[ORIGINAL] Der Klick auf Kopf/Logo des Landes des Bundestrainers (Vereinsfeld 0x16 = 6, Datei 0x38FC0–0x38FED) ruft
///   0x5434C mit Manager 5; DGROUP 0xA70E = 5, dann 0x48E28 (Datei 0x48E2B–0x48E5E) ohne Ausnahme für Manager 5: Aufstellung
///   0x40364 mit Kennwert DGROUP 0x2D6 + 5 bzw. Taktik 0x47F2B mit 1. Der Kennwert ist nach dem Laden 5 (DGROUP-Startwerte
///   0x2D6–0x2DB = 05 05 05 05 05 05, Datei 0x170A6; Schreiber nur Datei 0x54821, ebenfalls 5), also „im Spiel“.</item>
/// <item>[ORIGINAL] Der Aufstellungsbildschirm ist derselbe wie beim Verein (Datei 0x40364), nur auf den Kaderplätzen des
///   Satzes 5: es gelten die Regeln „im Spiel“ von <see cref="Club.ClickPlayerInMatch"/> (Datei 0x40E2A keine Grenze 22,
///   0x4115F/0x40CFD herausgenommen bleibt eingesetzt, 0x40F35/0x41287 Rechtsklick auf einen Aufgestellten ändert nichts), auch der
///   sechste Feldspielerplatz (#939, A875: Datei 0x40EC4–0x40EFF und 0x413C9–0x414B4 ohne Blick auf den Kennwert).</item>
/// <item>[ORIGINAL] S07 (A874): Wie beim Verein zählen die Länderspiele beim Spielbeginn (Abrechnung 0x544C0 vor den Minuten, Datei
///   0x54910–0x5499E) und ein im Spiel neu Aufgestellter sofort (Datei 0x415B7–0x41622, <c>MatchEngine.CountPlacedInMatch</c> über
///   den Vereinsplatz des Landes der laufenden Paarung); die Abrechnung am Spielende (<c>MatchEngine.CreditWorldCupSquad</c>) zählt
///   sie nicht erneut. Ohne Vereinsplatz (Aufruf außerhalb des Live-Bildschirms) zählt wie bisher das Spielende.</item>
/// </list>
/// </summary>
public sealed partial class NationalCoach
{
    /// <summary>
    /// Klick auf einen Spieler der Kaderliste im Spiel (siehe Klasse; ohne Spiel <see cref="ClickPlayer"/>).
    /// <paramref name="nationClub"/>: Vereinsplatz des Landes (laufende Paarung) für die Strafbank-Abfrage.
    /// </summary>
    public LineupResult ClickPlayerInMatch(Player player, ref int? line, bool right, Club? nationClub = null)
    {
        player = SlotOf(player);
        if (!Contains(player))
            return LineupResult.NotInSquad;
        DisableAutoLineup();
        if (player.InjuredGames > 0)
            return LineupResult.Injured;
        if (player.SuspendedGames > 0)
            return LineupResult.Suspended;
        // Aufräumkonzept K2: [ORIGINAL] Die Abfrage Datei 0x40DAF–0x40E27 liest den Status +0x09 &amp; 3 des Kaderplatzes des
        // Managers am Zug (DGROUP 0x5DC3 + 0x618 · DGROUP 0xA70E) – für Manager 5 wie für einen Verein; Status 1 (Strafbank) setzt
        // 0x35818 im Satz 5 (Teamfeld +0x16 = 6, Datei 0x35AB8–0x35AC6, `or [es:bx+0x09],1` Datei 0x35C02). Beide Tasten: nur
        // „Spieler ist“ / „Gesperrt.“ (wie <see cref="Club.ClickPlayerInMatch"/>, A808).
        if (nationClub is not null && MatchEngine.PenaltyBoxMinutes(nationClub, player) > 0)
            return LineupResult.Suspended;
        // [ORIGINAL] Datei 0x40EC4–0x40EFF (A875).
        if (!right && player.Role == PlayerRole.Goalkeeper && line == GoalkeeperLine && Formation.SixthSkaterOpen)
            return LineupResult.SixSkaters;
        if (Formation.FindSlot(player) is not null)
        {
            if (right)
                return LineupResult.Unchanged; // [ORIGINAL] Datei 0x41287–0x4129E
            line = TakeOut(player, keepDressed: true); // [ORIGINAL] Datei 0x4115F: +0x0A = Kennwert ≠ 0
            return LineupResult.TakenOut;
        }
        if (line is not { } l)
            return right ? LineupResult.Unchanged : LineupResult.NoLineSelected;
        if ((player.Role == PlayerRole.Goalkeeper) != (l == GoalkeeperLine))
            return LineupResult.WrongRole;
        foreach (var slot in Core.Lineup.SearchSlots(l, player.Role))
        {
            if (Formation[slot] is not null)
                continue;
            // [ORIGINAL] Datei 0x41533: Platz +0x15 und eingesetzt +0x0A = 1, ohne Grenze 22 (Datei 0x40E2A); andere Hälfte der
            // Reihe wie beim Linksklick (Datei 0x414D3, A683/A490).
            if (!(slot.Role == player.Role ? Formation.Assign(slot, player) : Formation.PlaceSkater(slot, player)))
                return LineupResult.WrongRole;
            _dressed.Add(player);
            CountPlaced(nationClub, player); // S07 A874: [ORIGINAL] Datei 0x415B7–0x41622, sofort
            return LineupResult.Assigned;
        }
        // [ORIGINAL] A875: sechster Feldspielerplatz (Datei 0x413C9–0x414B4), Kennwert an 0x41533 (Datei 0x413E8): eingesetzt.
        if (Formation.TryOpenSixthSkater(l, player))
        {
            _dressed.Add(player);
            CountPlaced(nationClub, player); // S07 A874
            return LineupResult.Assigned;
        }
        return LineupResult.NoRoom;
    }

    private static void CountPlaced(Club? nationClub, Player player)
    {
        if (nationClub is not null)
            MatchEngine.CountPlacedInMatch(nationClub, player);
    }

    /// <summary>Platz leeren im Spiel: der Spieler bleibt eingesetzt ([ORIGINAL] Datei 0x40CFD).</summary>
    public void ClearSlotInMatch(LineupSlot slot)
    {
        if (Formation[slot] is not { } player)
            return;
        DisableAutoLineup();
        Formation.Clear(slot);
        _dressed.Add(player);
    }
}
