namespace EishockeyManager.Core;

/// <summary>Spielvorbereitung 0x8365: Einsatz und Angriff vor dem Spiel (#772, Spur abgleich-rest).</summary>
public static partial class MatchEngine
{
    /// <summary>
    /// [ORIGINAL] Spielvorbereitung 0x8365 je Paarung (Aufrufe Datei 0x36D12/0x36D49, vor der Minutenschleife):
    /// <list type="number">
    /// <item>Stärke 0x368B1(Verein, Gewichtung 1, Minute 0, Differenz 0, neu) beider Vereine, doppelt verglichen (Datei
    ///   0x839D–0x83D6, <c>shl ax,1</c>); 0x368B1 ruft 0x977F mit „im Spiel“ = 1, also mit Energie der Reihe und mit dem
    ///   Einsatz 0x26 des letzten Spiels (<see cref="Tactics.MatchEffort"/>, Datei 0x9C66). Eigene Stärke ≥ gegnerische →
    ///   Favorit (0x2C Bit 7, Zeile DGROUP 0x491C + 5).</item>
    /// <item>|Differenz| &lt; 25 → Grundwert 60, sonst 40 (Datei 0x8400–0x841C). Ohne Manager (Vereinsfeld 0x16 = 0) oder
    ///   bei Einsatz „AUTOM.“ (DGROUP 0xA6FA + Manager = 0): Zuschläge 0x27–0x2B und 0x26 = 0x2D = Grundwert, ohne Zuschlag
    ///   „Unentschieden“ (Datei 0x8443–0x847D).</item>
    /// <item>Ohne Manager oder bei Angriff „AUTOM.“: nur die Zeile 0x35–0x3A; die Angriffsbits 0x2C (4–6) bleiben aus dem
    ///   letzten Spiel stehen (Datei 0x8481–0x84BD, <see cref="Tactics.MatchAttack"/>).</item>
    /// </list>
    /// Die Zeilen der Automatik nimmt 0x939A in den geraden Minuten der Managerpaarung (<see cref="OriginalMinutes.FollowTactics"/>
    /// mit <see cref="LiveTeam.TacticsStrength"/>); die Anzeige „AUTOM.“ hält <see cref="Tactics.RecordAutoLine"/> fest (A180).
    /// [APPROX] Das Original ruft 0x368B1 mit den Reihen 0x2E/0x30 aus dem Vorspiel; das Remake mit Reihe 0 (Computervereine
    /// wechseln Reihen nur in Managerpaarungen). Ob 0x977F den Managerverein dabei aus den Spielern bildet, hängt an der Liste
    /// DGROUP 0x233 (0x52B91), die erst die Live-Anzeige 0x5353C füllt; das Remake nimmt in der Managerpaarung die Spieler.
    /// [APPROX] Managervereine mit „SEMI“/„MANUELL“ behalten Einsatz und Angriff der eigenen Werte beim Stand 0:0
    /// (Taktikbildschirm Datei 0x48E62 für 0x26; ob er die Angriffsbits schreibt, ist nicht gelesen). Nationalteams wie
    /// bisher (<see cref="NationalLiveTeam"/>).
    /// </summary>
    public static void PrepareMatch(GameState game, Club home, Club away, LiveTeam homeTeam, LiveTeam awayTeam)
    {
        if (IsNation(home) || IsNation(away))
            return;
        var homeStrength = PreparedStrength(home, homeTeam, awayTeam);
        var awayStrength = PreparedStrength(away, awayTeam, homeTeam);
        Prepare(game, home, homeTeam, homeStrength, awayStrength);
        Prepare(game, away, awayTeam, awayStrength, homeStrength);
    }

    /// <summary>Stärke 0x368B1 eines Vereins vor dem Spiel (Gewichtung 1, Minute 0) mit dem Einsatz des letzten Spiels.</summary>
    internal static int PreparedStrength(Club club, LiveTeam team, LiveTeam opponent)
    {
        var effort = team.Effort;
        team.Effort = club.Tactics.MatchEffort;
        var strength = OriginalMinutes.Strengths(team, opponent, 0).Normal;
        team.Effort = effort;
        return strength;
    }

    private static void Prepare(GameState game, Club club, LiveTeam team, int own, int opponent)
    {
        var tactics = club.Tactics;
        var unmanaged = !game.ActiveManagers.Any(m => m.ClubIndex == club.Index);
        // A180: [ORIGINAL] Zeile der Automatik im Vereinssatz – für die Anzeige „AUTOM.“ festgehalten; ohne Manager ohne
        // Blick auf die Modi (Datei 0x842B).
        tactics.RecordAutoLine(own, opponent, unmanaged);
        team.TacticsStrength = own;
        team.TacticsOpponentStrength = opponent;
        if (unmanaged || tactics.EffortMode == TacticMode.Auto)
            team.Effort = Tactics.PreparedEffort(own, opponent);
        if (unmanaged || tactics.AttackMode == TacticMode.Auto)
            team.AttackLevel = tactics.MatchAttack;
    }

    /// <summary>[ORIGINAL] Vereinsfelder 0x26 und 0x2C (Bits 4–6) bleiben nach dem Spiel stehen (<see cref="Tactics.MatchEffort"/>).</summary>
    private static void KeepMatchFields(Club club, LiveTeam team)
    {
        if (!IsNation(club))
            club.Tactics.RecordMatchFields(team.Effort, team.AttackLevel);
    }
}
