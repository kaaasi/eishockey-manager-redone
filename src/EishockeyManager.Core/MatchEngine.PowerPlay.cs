namespace EishockeyManager.Core;

/// <summary>
/// Über-/Unterzahl-Zähler der Manager in der Minutenrechnung (Spur spielberechnung-komplett, Teil 4; Regeln, Anzeige und
/// Saisonwechsel: <see cref="PowerPlay"/>, Spur statistiken-verein). Fundstellen: <c>docs/spielberechnung.md</c> §18.
/// Kein Zufallszug: die Zähler ändern weder Ergebnisse noch den Zufallsverbrauch.
/// </summary>
public static partial class OriginalMinutes
{
    /// <summary>
    /// Nach einem Tor: Strafe des Gegners beenden und die Zähler der Manager fortschreiben. [ORIGINAL] Abschluss 0xA484:
    /// <list type="number">
    /// <item>Datei 0xA8AC–0xA933: Hat die verteidigende Mannschaft Spieler auf der Strafbank (0x35540(…, 0)), endet eine
    ///   Strafe (0x35691(…, 0, 0)). Standen vorher gleich viele Feldspieler auf dem Eis und hat einer der Vereine einen
    ///   Manager, zählt danach eine Überzahl der verteidigenden Mannschaft (0xA939–0xA9A2): Manager des Schützen
    ///   „Unterzahlspiele“ (+0x177 + 0xA), Manager der verteidigenden Mannschaft „Überzahlspiele“ (+0x177 + 8).</item>
    /// <item>Datei 0xAA09–0xAAEF: Tore nach der Feldspielerlage zu Beginn des Abschlusses (<paramref name="homeSkaters"/>,
    ///   <paramref name="awaySkaters"/>, [bp−0x11]/[bp−0x12]) für den Heim- und den Gastmanager
    ///   (<see cref="PowerPlay.Goal"/>); bei zusammen 11 Feldspielern (5 gegen 6, Torhüter draußen) nichts (0xAA18–0xAA29).</item>
    /// </list>
    /// </summary>
    /// <param name="side">Seite des Schützen (0 Heim, 1 Gast).</param>
    public static void GoalScored(LiveTeam home, LiveTeam away, int side, int homeSkaters, int awaySkaters)
    {
        var defender = side == 0 ? away : home;
        var shooter = side == 0 ? home : away;
        if (defender.Box.Count > 0)
        {
            var even = (defender.Managed || shooter.Managed) && shooter.Box.Skaters == defender.Box.Skaters;
            defender.Box.Release(0, force: false);
            if (even && defender.Box.Skaters > shooter.Box.Skaters)
            {
                if (shooter.Managed)
                    CountPowerPlay(shooter, PowerPlayCounter.ShortHanded, 1);
                if (defender.Managed)
                    CountPowerPlay(defender, PowerPlayCounter.PowerPlays, 1);
            }
        }
        if (homeSkaters + awaySkaters == 11)
            return;
        var homeScored = side == 0;
        if (home.Managed && PowerPlay.Goal(true, homeSkaters, awaySkaters, homeScored) is { } h)
            CountPowerPlay(home, h, 1);
        if (away.Managed && PowerPlay.Goal(false, homeSkaters, awaySkaters, homeScored) is { } a)
            CountPowerPlay(away, a, 1);
    }

    /// <summary>
    /// Beginn einer Über-/Unterzahl nach einer Strafe gegen <paramref name="club"/>. [ORIGINAL] Datei 0x35A26–0x35B22
    /// (nach dem Eintrag auf der Strafbank, vor der Spielerwahl 0x378C5): Ist der Abstand der Feldspieler danach genau 1
    /// (<c>0x120:0x61</c> = |a − b| – auch wenn die bestrafte Mannschaft weiter in Überzahl ist, übernommen), zählt der
    /// Manager des Gegners „Überzahlspiele“ (+0x177 + 8) und dann der eigene „Unterzahlspiele“ (0x97DC).
    /// </summary>
    /// <returns>Der zuletzt gezählte Wert ([bp−0xa]/[bp−0xb]) für die Rücknahme in <see cref="Penalty"/>, sonst null.</returns>
    private static (LiveTeam Team, PowerPlayCounter Counter)? CountPowerPlayStart(LiveTeam club, LiveTeam opponent)
    {
        if (Math.Abs(club.Box.Skaters - opponent.Box.Skaters) != 1)
            return null;
        (LiveTeam, PowerPlayCounter)? last = null;
        if (opponent.Managed)
        {
            CountPowerPlay(opponent, PowerPlayCounter.PowerPlays, 1);
            last = (opponent, PowerPlayCounter.PowerPlays);
        }
        if (club.Managed)
        {
            CountPowerPlay(club, PowerPlayCounter.ShortHanded, 1);
            last = (club, PowerPlayCounter.ShortHanded);
        }
        return last;
    }

    private static void CountPowerPlay(LiveTeam team, PowerPlayCounter counter, int delta)
    {
        if (team.PowerPlayCounts is { } counts)
            counts[(int)counter] += delta;
    }
}
