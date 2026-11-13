namespace EishockeyManager.Core;

/// <summary>Womit der Eingriff über Kopf/Logo beginnt ([ORIGINAL] Datei 0x5434C, Wort-Argument Modus).</summary>
public enum InterventionMode
{
    /// <summary>Linke Taste (und Torszene nach Verletzung): Aufstellung zuerst (Modus 1).</summary>
    Lineup = 1,

    /// <summary>Rechte Taste: Taktik zuerst (Modus 2).</summary>
    Tactics = 2,
}

/// <summary>
/// Eingriff in eine laufende Managerpaarung als Befehle des Spielkerns (Spur livespiel-kern, Teil A3, L6;
/// <c>docs/analyse/re-livespiel.md</c> §10). Der Live-Bildschirm (Spur livespiel-ui-2, B3) setzt Klicks in diese Befehle um;
/// sie wirken ab der nächsten Rechnung auf die <see cref="LiveTeam"/> der Paarung.
/// <list type="bullet">
/// <item>[ORIGINAL] Nur für den Verein der angeklickten Seite mit Manager (Vereinsfeld 0x16 ≠ 0, Datei 0x38E5F/0x38E7B):
///   sonst ändert kein Befehl etwas (<see cref="LiveTeam.Managed"/>).</item>
/// <item>[ORIGINAL] Einsatz: Vereinsfeld 0x26 = Treffer − 1 (Datei 0x38ECD–0x38EFE), über 26 Pixelspalten 0, 4 … 100.</item>
/// <item>[ORIGINAL] Angriff: Vereinsfeld 0x2C Bits 4–6 = Treffer − 1 (Datei 0x38F02–0x38F40), 0 … 7; Modus und Torhüterbit bleiben.</item>
/// <item>[ORIGINAL] Reihe auf dem Eis je Teil: Vereinsfeld 0x2E + 2t = j (Datei 0x38F44–0x38FBE); die vierte Reihe (j = 3) nur,
///   wenn 0x08E5F (<see cref="LiveSquad.HasFourthLine"/>) einen eingesetzten Spieler meldet.</item>
/// <item>[ORIGINAL] Nach Schieber und Lampen keine Neuzählung und kein Neuzeichnen; erst die Anzeigetafel am Minutenende zeigt
///   den Stand. Das Nachführen 0x939A überschreibt Einsatz und Angriff in der nächsten geraden Minute, außer bei „MANUELL“
///   (<see cref="OriginalMinutes.FollowTactics"/>, A176); die Reihe gilt bis zum nächsten Reihenwechsel 0x8EF2.</item>
/// <item>[ORIGINAL] Eingriff über Kopf/Logo (Datei 0x38FC0–0x38FED → 0x5434C): Aufstellung bzw. Taktik mit Kennzeichen „im
///   Spiel“ (DGROUP 0x2D6 + Manager, von der Spielabrechnung Datei 0x54821 auf 5 gesetzt; [APPROX] Rücksetzen nicht gefunden, es
///   gilt „≠ 0 = im Spiel“), danach Feldspieler neu zählen 0x38A36 (Datei 0x5438D–0x54419, <see cref="MatchEngine.RecountSkaters"/>).</item>
/// </list>
/// </summary>
public static class MatchIntervention
{
    /// <summary>[ORIGINAL] Größter Einsatz (Schieber 0x534BC mit n = 100).</summary>
    public const int MaxEffort = 100;

    /// <summary>[ORIGINAL] Größte Angriffsstufe (Schieber mit n = 7, Bits 4–6).</summary>
    public const int MaxAttack = 7;

    /// <summary>[ORIGINAL] Vier Reihen je Teil (Lampen k = 0 … 3, Datei 0x38FB1).</summary>
    public const int LineCount = 4;

    /// <summary>[ORIGINAL] Wert von DGROUP 0x2D6 + Manager während des Spiels (Datei 0x54821).</summary>
    public const int InMatchFlag = 5;

    /// <summary>Einsatz setzen (0 … 100). Liefert false ohne Manager oder bei einem Wert außerhalb.</summary>
    public static bool SetEffort(LiveTeam team, int effort)
    {
        if (!team.Managed || effort is < 0 or > MaxEffort)
            return false;
        team.Effort = effort;
        return true;
    }

    /// <summary>Angriffsstufe setzen (0 … 7). Liefert false ohne Manager oder bei einem Wert außerhalb.</summary>
    public static bool SetAttack(LiveTeam team, int attack)
    {
        if (!team.Managed || attack is < 0 or > MaxAttack)
            return false;
        team.AttackLevel = attack;
        return true;
    }

    /// <summary>
    /// Reihe <paramref name="line"/> (0 … 3) des Teils <paramref name="part"/> (0 Abwehr, 1 Sturm) aufs Eis. Liefert false
    /// ohne Manager, bei ungültigen Werten und für die vierte Reihe ohne eingesetzten Spieler (Datei 0x38F80–0x38F99).
    /// </summary>
    public static bool SetLine(LiveTeam team, int part, int line)
    {
        if (!team.Managed || part is < 0 or > 1 || line is < 0 or >= LineCount)
            return false;
        if (line == LineCount - 1 && !(team.Squad?.HasFourthLine(part) ?? false))
            return false;
        if (part == 0)
            team.DefenseLine = line;
        else
            team.AttackLine = line;
        return true;
    }

    /// <summary>[ORIGINAL] Datei 0x38FCF–0x38FED: linke Taste (Maustaste 1) Aufstellung, rechte (3) Taktik.</summary>
    public static InterventionMode ModeFor(bool rightButton) => rightButton ? InterventionMode.Tactics : InterventionMode.Lineup;

    /// <summary>
    /// Nach Aufstellung/Taktik im Spiel (Datei 0x5438D–0x54419): Feldspieler des Managervereins neu zählen (0x38A36). Die
    /// geänderte Aufstellung selbst übernimmt der Aufrufer in <see cref="LiveTeam.Squad"/>.
    /// </summary>
    public static void AfterLineup(LiveTeam team)
    {
        if (team.Managed)
            MatchEngine.RecountSkaters(team);
    }
}
