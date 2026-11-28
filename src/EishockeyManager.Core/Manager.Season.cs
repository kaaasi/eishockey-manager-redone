namespace EishockeyManager.Core;

public sealed partial class Manager
{
    /// <summary>
    /// Managerbyte DGROUP 0x9744 + 0x1AF · m („eine Saison, ein Manager“). [ORIGINAL] A847: Datei 0x29D23 setzt es beim Spielstart
    /// auf 1, wenn das Spielende 1994 und genau ein Manager ist (einziger Schreiber); die Bestenliste liest es als viertes Pokalbyte
    /// (Datei 0x5D351, 4 Bytes ab 0x9741) und zählt 150 Summenpunkte. [APPROX] Ältere Spielstände kennen es nicht: false.
    /// </summary>
    public bool OneSeasonFlag { get; set; }
}
