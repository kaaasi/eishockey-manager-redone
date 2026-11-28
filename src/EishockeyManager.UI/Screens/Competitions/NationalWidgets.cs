using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens.Competitions;

/// <summary>Bausteine der Bundestrainer-Bildschirme „Spieler in WM-Kader“ und „WM Vorbereitung“ (Overlay 0xF8).</summary>
public static class NationalWidgets
{
    /// <summary>[ORIGINAL] Rahmenfarben der Kästen (DGROUP 0x4EDB), Innenfläche Farbe 1.</summary>
    private static readonly byte[] BoxColors = [7, 4, 29];

    /// <summary>
    /// [ORIGINAL] Kasten (Datei 0x3B700): drei Rahmenlinien in den Farben 7, 4, 29, jeweils ein Pixel weiter innen,
    /// danach die Innenfläche (x1 + 3 | y1 + 3) … (x2 − 3 | y2 − 3) in Farbe 1. Koordinaten einschließlich.
    /// </summary>
    public static void Box(Surface s, int x1, int y1, int x2, int y2)
    {
        for (var i = 0; i < BoxColors.Length; i++)
            Outline(s, x1 + i, y1 + i, x2 - i, y2 - i, BoxColors[i]);
        s.Fill(new Rect(x1 + 3, y1 + 3, x2 - x1 - 5, y2 - y1 - 5), 1);
    }

    /// <summary>Rechteck-Umriss (em.exe 0x902:0x787), Koordinaten einschließlich.</summary>
    public static void Outline(Surface s, int x1, int y1, int x2, int y2, byte color)
    {
        s.HLine(x1, y1, x2 - x1 + 1, color);
        s.HLine(x1, y2, x2 - x1 + 1, color);
        s.VLine(x1, y1, y2 - y1 + 1, color);
        s.VLine(x2, y1, y2 - y1 + 1, color);
    }

    /// <summary>
    /// Fläche (em.exe 0x902:0x800), Koordinaten einschließlich; vertauschte Ecken werden geordnet (Balken der Live-Anzeige,
    /// <see cref="LiveWidgets"/>).
    /// </summary>
    public static void FillInclusive(Surface s, int x1, int y1, int x2, int y2, byte color) =>
        s.Fill(new Rect(Math.Min(x1, x2), Math.Min(y1, y2), Math.Abs(x2 - x1) + 1, Math.Abs(y2 - y1) + 1), color);

    /// <summary>Linker Rand des Mannschaftsfelds für Seite 0 (Heim) bzw. 1 (Gast).</summary>
    public static int PanelX(int side) => 31 + 134 * side;

    public const int PanelY = 194;

    /// <summary>
    /// [ORIGINAL] Mannschaftsfeld (Datei 0x3B4C6): Linie (x … x + 122 | 210) Farbe 0, Namensfeld (x | 193) … (x + 122 | 200)
    /// Farbe 4 mit dem Namen (Flow, zentriert, Grundlinie 200, Farbe 0), Kopf „ART REIHE: 1. 2. 3.“ (Mini, x + 10,
    /// Grundlinie 208), Wertefeld (x + 64 | 212) … (x + 122 | 223) Farbe 4, Zeilen „Abwehr“ und „Sturm“ (x + 4,
    /// Grundlinie 216 / 223) mit den Werten der Reihen 1–3 bei x + 70 + 20 · Reihe (Farbe 1, zweistellig).
    /// [ORIGINAL] Reihenwerte (B17, A285, A419): 0x28:0x16BF = 0x977F(Verein, Reihe, Teil, 0, 0) (Datei 0x3B65C), angezeigt
    /// (Byte 2 + Byte 1) / 2 (Datei 0x3B664–0x3B691) über die Zahlenausgabe 0x38:0x2C2 (Datei 0x3B6C4) – siehe
    /// <see cref="LineValue"/>.
    /// </summary>
    public static void TeamPanel(Surface s, Assets a, GameState game, int club, int side)
    {
        var x = PanelX(side);
        const int y = PanelY;
        s.HLine(x, y + 16, 123, Colors.Black);
        FillInclusive(s, x, y - 1, x + 122, y + 6, 4);
        CompetitionWidgets.Centered(s, a.Flow, x, x + 122, y + 6, game.Clubs[club].Name, Colors.Black, null);
        CompetitionWidgets.Left(s, a.Mini, x + 10, y + 14, "ART        REIHE:       1.      2.      3.", Colors.Black);
        FillInclusive(s, x + 64, y + 18, x + 122, y + 29, 4);
        string[] labels = ["Abwehr", "Sturm"];
        for (var row = 0; row < 2; row++)
        {
            var baseline = y + 22 + 7 * row;
            CompetitionWidgets.Left(s, a.Mini, x + 4, baseline, labels[row], 1);
            for (var line = 0; line < NationalCoach.Lines; line++)
                CompetitionWidgets.Left(s, a.Mini, x + 70 + 20 * line, baseline,
                    CompetitionWidgets.Number(LineValue(game, club, line, row == 1), 2), 1);
        }
    }

    /// <summary>
    /// Wert einer Reihe (siehe <see cref="TeamPanel"/>). [ORIGINAL] B17 (A285, A419, A961; Aufräumkonzept K9): 0x977F wie in
    /// Datei 0x3B65C, (Kondition + Technik) / 2 der Rückgabe (Datei 0x3B664–0x3B691) – <see cref="GameState.NationalPanelLineValue"/>,
    /// Beleg `b17_reihenwerte_national.py` (300 von 300 gleich, docs/archiv/analyse/re-abweichungen.md B17). Länder ohne
    /// Bundestrainer: Teamwerte + 4 − 4 · Reihe, je auf 0 … 100 begrenzt; Land des Bundestrainers: Spielerweg 0x98F8 über
    /// <see cref="MatchEngine.ManagedLineValues"/> (A494). Keine eigene Grenze bei 99: 0x977F liefert höchstens 100, die
    /// Zahlenausgabe zeigt dann drei Stellen. Vorher (K + T) / 2 der Länderwerte bzw. der Aufstellung, auf 0–99 begrenzt.
    /// </summary>
    public static int LineValue(GameState game, int club, int line, bool attack) =>
        game.NationalPanelLineValue(club, line, attack);
}
