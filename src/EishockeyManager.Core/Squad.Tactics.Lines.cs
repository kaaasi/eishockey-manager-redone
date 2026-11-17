namespace EishockeyManager.Core;

/// <summary>
/// Reihe auf dem Eis je Teil nach dem letzten Spiel eines Vereins (#872, Runde 20, Spur pruefung-karte Teil 2).
/// </summary>
/// <remarks>
/// [ORIGINAL] Vereinsfelder 0x2E (Abwehr) und 0x30 (Sturm) bleiben im Vereinssatz zwischen den Spielen stehen: Die
/// Spielvorbereitung 0x84D7 (Datei 0x84EB–0x857D, Argument 0) setzt je Verein nur die Minuten auf dem Eis 0x2F/0x31 auf 0
/// (Datei 0x853F) und bei Computervereinen den Wechselmodus 0x2C Bits 0–2 (Datei 0x8521), die Reihen nicht. Geschrieben werden
/// sie nur im Reihenwechsel 0x8EF2 (Datei 0x91CF/0x91E3, Managerpaarungen), im Eingriffsmenü 0x38D96 (Datei 0x38FAA,
/// <c>mov [es:bx+0x2e+2·Teil],al</c>; Remake <c>MatchEngine.Intervention.cs</c>, landet über <c>KeepMatchFields</c> im Feld), in
/// der Neuzählung 0x388F6 (Datei 0x38A2A) und bei gesetztem 0x2C Bit 3 in 0x84D7 selbst (Datei 0x857D; für jeden Verein, auch
/// Managervereine, und nicht an 0x16 gebunden). Beleg: DOSBox `runde-19`, KÖLNER EC in FILE50 mit 0/2 beginnt Spieltag
/// 2 mit Abwehr 0 und Sturm 2 bis Minute 3 (<c>docs/analyse/abgleich-dosbox.md</c> §4.1). Beim Spielstart stehen beide Felder
/// aller Vereine auf 0 (FILE02).
/// </remarks>
public sealed partial class Tactics
{
    /// <summary>Vereinsfeld 0x2E nach dem letzten Spiel (0 … 3).</summary>
    private int _matchDefenseLine;

    /// <summary>Vereinsfeld 0x30 nach dem letzten Spiel (0 … 3).</summary>
    private int _matchAttackLine;

    /// <summary>Abwehrreihe auf dem Eis am Ende des letzten Spiels (Vereinsfeld 0x2E).</summary>
    public int MatchDefenseLine => _matchDefenseLine;

    /// <summary>Sturmreihe auf dem Eis am Ende des letzten Spiels (Vereinsfeld 0x30).</summary>
    public int MatchAttackLine => _matchAttackLine;

    /// <summary>Hält die Reihen auf dem Eis am Ende eines Spiels fest (Vereinsfelder 0x2E/0x30, siehe Klasse).</summary>
    public void RecordMatchLines(int defenseLine, int attackLine)
    {
        _matchDefenseLine = Math.Clamp(defenseLine, 0, MaxLine);
        _matchAttackLine = Math.Clamp(attackLine, 0, MaxLine);
    }

    /// <summary>Höchste Reihe (vierte Reihe = 3, <c>LiveSquad.HasFourthLine</c>).</summary>
    private const int MaxLine = 3;
}
