using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens.ManagerArea;

/// <summary>
/// Bildschirm „Jugendteams“ (Manager → Jugend), vermessen am UI-Video (8:33–8:45): oben der Tabellenstand der
/// drei Jugendmannschaften als Fahnen in drei Ligen, unten die Förderung je Mannschaft als Balken.
/// Klick in einen Balken setzt die Förderung, links/rechts daneben ändert sie um einen Schritt.
/// </summary>
/// <remarks>
/// [ORIGINAL] em.exe Datei 0x3A24B (Bildschirm) und 0x39BBD (Förderbalken); Zuordnung in <c>docs/finanzen-rekonstruktion.md</c>.
/// </remarks>
/// <remarks>
/// [ORIGINAL] Keine Talentsuche: Die Texte des „Spions“ nutzt nur die Transferliste (Datei 0x465F1, DGROUP 0x1339; A328,
/// Spur geld Teil 6). [APPROX] Ein Klick außerhalb der Förderzeilen kehrt zurück (Abbruchbedingung von 0x3A24B nicht gelesen).
/// </remarks>
public sealed class YouthScreen : Screen
{
    public static readonly Rect ChartWindow = new(31, 21, 240, 115);
    /// <summary>Förderfenster. [ORIGINAL] Rahmen bei (104|156), Datei 0x3A293 (UI-Video 8:37: Oberkante 156).</summary>
    public static readonly Rect FundingWindow = new(104, 156, 96, 76);

    /// <summary>Förderbalken der Mannschaft i (70 Pixel = Deckel der Mannschaft).</summary>
    /// <remarks>[ORIGINAL] Umriss (0x78|0xA7 + 24 · Team)–(0xBE|+11) in Farbe 0x1D, innen Farbe 5 (Datei 0x39C51, 0x39C73).</remarks>
    public static Rect Bar(int i) => new(120, 167 + 24 * i, 71, 12);

    private const int ChartLeft = 57;
    private const int ChartRight = 243;
    private const int FirstLineY = 53;
    private const int LeaguePitch = 24;
    private static int FlagX(int i) => 60 + 69 * i;

    /// <summary>Farben rot, grün, blau für Fahnenstange, Beschriftung und Balken. [ORIGINAL] DGROUP 0x4ED5.</summary>
    public static readonly byte[] TeamColors = [Colors.Red, 15, 23];

    /// <summary>
    /// Fahnen (16 × 10) in kb\33 ab x = 30 + 16 · Team. [ORIGINAL] Quelle und Lage Datei 0x3A3F6 (kb\33 = Grafik 0x21,
    /// Datei 0x3A25C). [APPROX] Durchsichtig ist die Hintergrundfarbe 4 der Grafik (im Video durchsichtig; den Blit-Parameter
    /// 0xC0 nicht entschlüsselt).
    /// </summary>
    private const int FlagImage = 33;
    private const byte FlagTransparent = 4;

    /// <summary>Unterkante der Fahnenstange (Datei 0x3A3D9, y = 0x7D).</summary>
    private const int PoleBottom = 125;
    private const byte LineColor = Colors.Red;
    private const byte AxisColor = 50;
    private const byte LabelColor = 212;
    private const byte BarColor = 5;
    private const byte BarOutline = 0x1D;

    /// <summary>Innenfläche beider Fenster. [ORIGINAL] UI-Video 8:37 (81, 113, 130) = Farbe 4, nicht <see cref="Colors.PanelFill"/>.</summary>
    private const byte WindowFill = 4;

    /// <summary>Kästchenfarben je Team: hell (oben/links), Mitte, dunkel (unten/rechts). [ORIGINAL] DGROUP 0x4EAE, Funktion Datei 0x39AD6.</summary>
    private static readonly byte[,] BevelColors = { { 18, 17, 16 }, { 15, 13, 12 }, { 23, 22, 8 } };
    private const byte LineHighlight = 59;

    /// <summary>Zeilen der Mini-Schrift über der Grundlinie (Ziffern und Großbuchstaben, gemessen UI-Video 8:35: y 103–107).</summary>
    private const int LabelHeight = 5;

    private readonly Screen _back;

    public YouthScreen(ScreenHost host, Screen back) : base(host) => _back = back;

    private GameState Game => Host.Game!;

    public override void Update(InputState input)
    {
        if (input.Pressed(Key.Escape) || input.RightClick)
        {
            Host.Show(_back);
            return;
        }
        if (!input.LeftClick)
            return;
        for (var i = 0; i < 3; i++)
        {
            // [ORIGINAL] Datei 0x3A67F: Zeile y 0xA8–0xB1 (168–177) + 24 · Team über die ganze Breite; x < 121 und x > 190 sind
            // die Pfeile (± Schritt), dazwischen setzt der Klick die Förderung.
            var bar = Bar(i);
            if (input.MouseY <= bar.Y || input.MouseY >= bar.Y + 11)
                continue;
            var kind = (YouthTeamKind)i;
            var me = Game.ActiveManager;
            if (input.MouseX > bar.X + YouthTeam.BarPixels)
                Game.StepYouthFunding(me, kind, up: true);
            else if (input.MouseX < bar.X + 1)
                Game.StepYouthFunding(me, kind, up: false);
            else
                Game.SetYouthFunding(me, kind, YouthTeam.BarFunding(kind, input.MouseX - bar.X));
            return;
        }
        if (!FundingWindow.Contains(input.MouseX, input.MouseY))
            Host.Show(_back);
    }

    /// <summary>
    /// Höhe der Fahne: oberes Ende der Fahnenstange. [ORIGINAL] Datei 0x3A3B2: <c>y = 0x36 + 24 · Liga + 2 · Platz</c>
    /// (0-basiert); 12. Platz 3. Liga = 124 (Folge 4 0:28:25), 6. Platz 3. Liga = 112 (UI-Video 8:35).
    /// </summary>
    public static int FlagY(YouthTeam team) => FirstLineY + 1 + LeaguePitch * (team.League - 1) + 2 * (team.Rank - 1);

    /// <summary>
    /// Grundlinie der Beschriftung „n.Platz m.Liga“. [ORIGINAL] Datei 0x3A435: y − 5, bei y &lt; 0x70 außerhalb der Bereiche
    /// 0x43–0x47 und 0x5B–0x5F 16 Pixel tiefer (unter der Fahne).
    /// </summary>
    public static int LabelBaseline(YouthTeam team)
    {
        var y = FlagY(team);
        if (y < 0x70 && y is not (>= 0x43 and <= 0x47) && y is not (>= 0x5B and <= 0x5F))
            y += 16;
        return y - 5;
    }

    public override void Draw(Surface s)
    {
        var mini = Assets.Mini;
        var teams = Game.Finance(Game.ActiveManager).Youth;
        s.Palette = Assets.Palette;
        s.Blit(Assets.Image(MainMenuScreen.Background), 0, 0);

        Widgets.Frame(s, Assets, ChartWindow, FrameStyle.Orange, WindowFill);
        s.TextCentered(Assets.Bold, 151, 26, "Jugendteams", Colors.White);
        s.Fill(new Rect(ChartLeft - 3, FirstLineY, 3, LeaguePitch * 3 + 3), AxisColor);
        for (var l = 0; l <= 3; l++)
        {
            s.HLine(ChartLeft, FirstLineY + LeaguePitch * l - 1, ChartRight - ChartLeft, LineHighlight);
            s.HLine(ChartLeft, FirstLineY + LeaguePitch * l, ChartRight - ChartLeft, LineColor);
        }
        s.HLine(ChartLeft - 3, FirstLineY + LeaguePitch * 3 + 1, ChartRight - ChartLeft + 3, AxisColor);

        for (var i = 0; i < teams.Length; i++)
        {
            var team = teams[i];
            var color = TeamColors[i];
            var x = FlagX(i);
            var y = FlagY(team);
            s.VLine(x, y, PoleBottom - y + 1, color);
            s.Blit(Assets.Image(FlagImage), new Rect(30 + 16 * i, 0, 16, 10), x, y - 3, FlagTransparent);
            var top = LabelBaseline(team) - LabelHeight + 1;
            // [APPROX] Im Original eine Zeichenkette „n.Platz“ + Liga mit Feldbreite 2 (DGROUP 0x374) + „.Liga“ (Datei 0x3A488);
            // hier rückt die Liga um die Breite der Platzzahl nach, Abstand am UI-Video 8:35 gemessen.
            var place = $"{team.Rank}.Platz";
            s.Text(mini, x + 2, top, place, color);
            s.Text(mini, x + 37 + mini.Measure(place) - mini.Measure("6.Platz"), top, $"{team.League}.Liga", color);
        }

        Widgets.Frame(s, Assets, FundingWindow, FrameStyle.Orange, WindowFill);
        for (var i = 0; i < teams.Length; i++)
        {
            var bar = Bar(i);
            s.Text(mini, bar.X, bar.Y - 6, teams[i].Label, LabelColor);
            s.Text(mini, bar.X + 38, bar.Y - 6, ":", LabelColor);
            s.TextRight(mini, bar.Right, bar.Y - 6, teams[i].Funding.ToString(), LabelColor);
            s.Fill(bar, BarColor);
            s.Outline(bar, BarOutline);
            if (teams[i].BarFill > 0) // [ORIGINAL] Datei 0x39DAA: Kästchen (121|y+1)–(119+w|y+10)
                Bevel(s, bar.X + 1, bar.Y + 1, bar.X + teams[i].BarFill, bar.Y + 10, i);
            Bevel(s, 110, bar.Y + 2, 117, bar.Y + 7, i); // [ORIGINAL] Datei 0x39D75
        }
    }

    /// <summary>Kästchen mit hellem Rand oben/links und dunklem unten/rechts (Eckpunkte einschließlich). [ORIGINAL] Datei 0x39AD6.</summary>
    private static void Bevel(Surface s, int x1, int y1, int x2, int y2, int team)
    {
        s.HLine(x1, y1, x2 - x1, BevelColors[team, 0]);
        s.VLine(x1, y1, y2 - y1 + 1, BevelColors[team, 0]);
        s.VLine(x2, y1, y2 - y1 + 1, BevelColors[team, 2]);
        s.HLine(x1 + 1, y2, x2 - x1, BevelColors[team, 2]);
        if (x2 - x1 > 1 && y2 - y1 > 1)
            s.Fill(new Rect(x1 + 1, y1 + 1, x2 - x1 - 1, y2 - y1 - 1), BevelColors[team, 1]);
    }
}

/// <summary>Anmeldung im Hauptmenü (Manager → Jugend).</summary>
internal static class YouthMenuEntries
{
    [MenuEntry("Jugend")]
    private static Screen Open(ScreenHost host, Screen back) => new YouthScreen(host, back);
}
