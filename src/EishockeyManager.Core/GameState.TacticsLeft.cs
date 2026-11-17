namespace EishockeyManager.Core;

/// <summary>Ereignis „Taktikbildschirm verlassen“ (A553, Spur wettbewerbe-regeln).</summary>
public sealed partial class GameState
{
    /// <summary>
    /// [ORIGINAL] Der Taktikbildschirm des Vereins setzt beim Aufbau Vereinsfeld 0x26 = 0x2D (Grundwert, Datei 0x48003–0x48007),
    /// bearbeitet 0x26 als Grundwert und ruft beim Verlassen ohne Bedingung 0x48E62 (Aufruf Datei 0x48E19): Zuschläge 0x27–0x2B
    /// höchstens 100 − Grundwert, dann 0x2D = 0x26 und 0x26 = 0x2D + 0x29 (Datei 0x4902B–0x4903E; 0x29 = Zuschlag
    /// „Unentschieden“). Die Angriffsbits 0x2C schreibt er nicht. Danach beginnt ein Managerverein mit „SEMI“/„MANUELL“ das nächste
    /// Spiel mit diesem Einsatz (<see cref="MatchEngine.PrepareMatch"/> schreibt 0x26 nur ohne Manager bzw. bei „AUTOM.“), die
    /// Stärke der Vorbereitung 0x368B1 liest ihn in jedem Modus. Bei „AUTOM.“ stehen in den Feldern die Zeilen der Automatik
    /// (<see cref="Tactics.ShownEffort"/>). Verlassen ist eine Bedienaktion, kein bloßes Öffnen (AGENTS.md §4).
    /// [APPROX] Nur der Vereinsbildschirm außerhalb des Spiels; der Bundestrainer hält seine eigenen Werte getrennt
    /// (DGROUP 0xCAE9, Datei 0x48607), der Eingriff im Live-Spiel führt die Werte in <see cref="LiveTeam"/>.
    /// </summary>
    public void OnTacticsLeft(Club club)
    {
        ArgumentNullException.ThrowIfNull(club);
        if (club.Index >= NationalTeams.First || !ActiveManagers.Any(m => m.ClubIndex == club.Index))
            return;
        var tactics = club.Tactics;
        tactics.RecordMatchFields(
            tactics.ShownEffort(0, ScoreSituation.Base) + tactics.ShownEffort(0, ScoreSituation.Tied), tactics.MatchAttack);
    }
}
