namespace EishockeyManager.Core;

/// <summary>
/// Kaderplatz +0x19: unteres Halbbyte = Unterzahl (<see cref="Player.HiddenSkill1"/>), oberes = Überzahl
/// (<see cref="Player.HiddenSkill2"/>), je 0 … 15. Analyse: docs/archiv/analyse/re-nachtrag.md, Abschnitt 6 (A185, A186).
/// </summary>
/// <remarks>
/// [ORIGINAL] Schreiber: Spielstart Managerkader Datei 0x299F4 und Managerkader-Neuanlage in Datei 0x5AF50 (Datei 0x5B239,
/// 0x5B262), Talent Datei 0x06F23, Listenwechsel auf den Transfermarkt Datei 0x49803 – je (random(3, 12) &lt;&lt; 4) |
/// random(3, 12), das obere Halbbyte zuerst gezogen; Kader des Nationalteams Datei 0x3CB2C = 0x77; Training Datei 0x06B01
/// (0 … 15, <see cref="TrainingRules.Apply"/>). Beleg Spielstände: FILE01/09/10/12 alle Plätze 3 … 12.
/// [ORIGINAL] Spur kaderplatz-werte (A674): Der Wert gehört zum Kaderplatz. Jeder neue Platz beginnt mit 0 (memset 0x42E3A in
/// 0x42CB1, <see cref="Player.ApplyNewSlot"/>); der Kauf vom Markt legt so einen Platz an und kopiert den Markt-Platz nur bei einer
/// Leihe oder beim Zurückziehen des eigenen Angebots darüber (Datei 0x478CF–0x47907) – beim Kauf ist der Wert danach 0 (FILE05 →
/// FILE07: DAFFNER Markt 0xCA → 0). Länderspiele rechnen mit 7/7 (<see cref="ForMatch"/>), ohne den Vereinswert zu ändern.
/// Spur zufallsstrom (A675, A676; N3 = B): Jeder Startwurf zieht an der Stelle des Originals aus dem gemeinsamen Strom
/// <see cref="GameState.Random"/> – Spielstart je Kaderplatz nach 0x42CB1 (Datei 0x299BF, 0x299D3;
/// <see cref="GameState.ApplyStartSalaries"/>), Talent Datei 0x06F02/0x06F16 (<see cref="GameState.JoinScoutedTalent(Manager, int, int)"/>),
/// Markt Datei 0x497C7 (<see cref="TransferPoolRules.RollSlotFields"/>). Die frühere eigene Quelle (aus dem Zustand des Stroms
/// abgeleitet) entfällt; neue Spieler ohne Kaderplatz eines Managers (Nachwuchs der Computervereine, Neuanlage im Pool) würfeln
/// nicht mehr und behalten den Vorgabewert, bis sie einen Kaderplatz bekommen.
/// </remarks>
public static class HiddenSkills
{
    /// <summary>Grenzen des Startwurfs je Halbbyte. [ORIGINAL] random(3, 12), siehe oben.</summary>
    public const int StartMin = 3, StartMax = 12;

    /// <summary>Wert je Halbbyte im Kader des Nationalteams. [ORIGINAL] Datei 0x3CB2C: 0x77.</summary>
    public const int NationalTeam = 7;

    /// <summary>Größter Wert je Halbbyte (Training). [ORIGINAL] Datei 0x4BD35: 0 … 15.</summary>
    public const int Max = 15;

    /// <summary>
    /// Startwurf eines Kaderplatzes: erst Überzahl (oberes Halbbyte), dann Unterzahl. [ORIGINAL] Datei 0x299B7–0x299F4.
    /// </summary>
    public static void Roll(Player p, Random random)
    {
        ArgumentNullException.ThrowIfNull(p);
        p.HiddenSkill2 = TransferRules.Random16(random, StartMin, StartMax);
        p.HiddenSkill1 = TransferRules.Random16(random, StartMin, StartMax);
    }

    /// <summary>
    /// Werte im Spiel: (Überzahl, Unterzahl). Im Länderspiel des Bundestrainers 7/7 (Satz 5, Datei 0x3CB2C), sonst die des
    /// Spielers.
    /// </summary>
    public static (int Overplay, int Underplay) ForMatch(Player p, bool nationalTeam) =>
        nationalTeam ? (NationalTeam, NationalTeam) : (p.HiddenSkill2, p.HiddenSkill1);
}
