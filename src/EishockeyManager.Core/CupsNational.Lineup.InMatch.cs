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
///   0x4115F/0x40CFD herausgenommen bleibt eingesetzt, 0x40F35/0x41287 Rechtsklick auf einen Aufgestellten ändert nichts).</item>
/// <item>[APPROX] Wie dort zählt das Remake die Länderspiele am Spielende aus der Aufstellung (<c>MatchEngine.CreditWorldCupSquad</c>),
///   nicht beim Aufstellen im Spiel (Datei 0x415B7–0x41622).</item>
/// </list>
/// </summary>
public sealed partial class NationalCoach
{
    /// <summary>Klick auf einen Spieler der Kaderliste im Spiel (siehe Klasse; ohne Spiel <see cref="ClickPlayer"/>).</summary>
    public LineupResult ClickPlayerInMatch(Player player, ref int? line, bool right)
    {
        player = SlotOf(player);
        if (!Contains(player))
            return LineupResult.NotInSquad;
        DisableAutoLineup();
        if (player.InjuredGames > 0)
            return LineupResult.Injured;
        if (player.SuspendedGames > 0)
            return LineupResult.Suspended;
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
        foreach (var slot in SlotsOf(l, player.Role))
        {
            if (Formation[slot] is not null)
                continue;
            // [ORIGINAL] Datei 0x41533: Platz +0x15 und eingesetzt +0x0A = 1, ohne Grenze 22 (Datei 0x40E2A).
            if (!Formation.Assign(slot, player))
                return LineupResult.WrongRole;
            _dressed.Add(player);
            return LineupResult.Assigned;
        }
        return LineupResult.NoRoom;
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
