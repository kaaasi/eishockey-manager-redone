namespace EishockeyManager.Core;

/// <summary>
/// Einsatz und Angriff eines Vereins nach seinem letzten Spiel (#772, Spur abgleich-rest).
/// </summary>
/// <remarks>
/// [ORIGINAL] Vereinsfeld 0x26 (Einsatz) und Vereinsfeld 0x2C Bits 4–6 (Angriff) bleiben im Vereinssatz zwischen den Spielen
/// stehen. Die Spielvorbereitung 0x8365 liest 0x26 über die Stärke 0x368B1 (Datei 0x9C66: Kondition − (100 − Einsatz ·
/// Kondition / 100) / 4) und schreibt danach 0x26 neu; die Angriffsbits schreibt sie nicht (Datei 0x8481–0x84BD) – die
/// schreiben nur 0x939A (gerade Minuten der Managerpaarung, Datei 0x9506) und das Live-Spiel 0x38D96. Ein Computerverein
/// spielt deshalb ohne Manager mit dem Angriff aus seinem letzten Managerspiel (FILE03: die drei Kölner Gegner 5/4/6, alle
/// anderen 0; <c>docs/analyse/belastung-staerke.md</c> §5). Beim Spielstart stehen beide Felder auf 0 (FILE02).
/// </remarks>
public sealed partial class Tactics
{
    /// <summary>Vereinsfeld 0x26 nach dem letzten Spiel (0 … 100).</summary>
    private int _matchEffort;

    /// <summary>Vereinsfeld 0x2C Bits 4–6 nach dem letzten Spiel (0 … 7).</summary>
    private int _matchAttack;

    /// <summary>Einsatz am Ende des letzten Spiels (Vereinsfeld 0x26).</summary>
    public int MatchEffort => _matchEffort;

    /// <summary>Angriff am Ende des letzten Spiels (Vereinsfeld 0x2C, Bits 4–6).</summary>
    public int MatchAttack => _matchAttack;

    /// <summary>Hält Einsatz und Angriff am Ende eines Spiels fest (Vereinsfelder 0x26 und 0x2C, siehe Klasse).</summary>
    public void RecordMatchFields(int effort, int attack)
    {
        _matchEffort = Math.Clamp(effort, 0, MaxEffort);
        _matchAttack = Math.Clamp(attack, 0, MaxAttack);
    }

    /// <summary>
    /// [ORIGINAL] Grundwert des Einsatzes der Automatik (Datei 0x8400–0x841C): 60, wenn die doppelten Stärken weniger als
    /// <see cref="EvenMatchLimit"/> auseinanderliegen, sonst 40. 0x8365 setzt Vereinsfeld 0x26 = 0x2D = Grundwert, ohne den
    /// Zuschlag „Unentschieden“ (Datei 0x8473–0x847D).
    /// </summary>
    public static int PreparedEffort(int ownStrength, int opponentStrength) =>
        AutoEffort(ownStrength, opponentStrength, ScoreSituation.Base);
}
