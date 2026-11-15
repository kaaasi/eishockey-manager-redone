using EishockeyManager.Formats;
using EishockeyManager.UI.Screens.Competitions;

namespace EishockeyManager.UI.Screens.Live;

/// <summary>Kopfzeile der Torszene (Argumente von 0x56EDD).</summary>
/// <param name="SpectatorText">Zuschauerzeile, wie sie die Torszene 0x53C57 bildet (Zahl + „ ZUSCHAUER“, „AUSWÄRTS“ oder leer).</param>
public sealed record GoalSceneHeader(string HomeName, string AwayName, int HomeGoals, int AwayGoals, int Minute,
    string SpectatorText);

/// <summary>
/// Ablauf einer Torszene als Modell (Spur torszenen, Teil C2; <c>docs/analyse/re-livespiel.md</c> §16): Kamera, Puckschatten
/// und zwölf Objekte je Bild. Kein Zufall, kein Spielzustand – reine Anzeige (R18-D3 = A); die Wanduhr macht der Bildschirm.
/// <list type="bullet">
/// <item>[ORIGINAL] Start <c>48h:5250h</c> (Datei 0x12C40–0x12CF7): Kamera Kopf − (16 | 32), je ≥ 0; Objekt 0 = Kopf +0x08/+0x0A
///   mit Figur 1 (Puckschatten, Farbe 0xFB), Objekte 1 … 12 aus dem Kopf.</item>
/// <item>[ORIGINAL] Bild <c>48h:5316h</c> (Datei 0x12D06–0x12D69): Kamera um Byte 0 (begrenzt, <see cref="GoalSceneRink.MoveCamera"/>),
///   Objekt 0 um Byte 1, je Objekt Figur immer übernehmen und Bewegung addieren, wenn Figur oder Bewegung ≠ 0.</item>
/// </list>
/// </summary>
public sealed class GoalSceneAnimation
{
    private readonly (int Figure, int X, int Y)[] _objects = new (int, int, int)[GoalScene.ObjectCount];

    public GoalSceneAnimation(GoalScene scene)
    {
        Scene = scene;
        Camera = GoalSceneRink.StartCamera(scene);
        Shadow = (scene.PuckX, scene.PuckY);
        for (var k = 0; k < _objects.Length; k++)
            _objects[k] = (scene.Objects[k].Figure, scene.Objects[k].X, scene.Objects[k].Y);
    }

    public GoalScene Scene { get; }

    /// <summary>Linke obere Ecke des Sichtfensters auf der Eisfläche.</summary>
    public (int X, int Y) Camera { get; private set; }

    /// <summary>Objekt 0 (Kopf +0x08/+0x0A, Figur 1). [ORIGINAL] Im Video <c>eisbaeren_f01_01150</c> ein Pixel unter dem Puck.</summary>
    public (int X, int Y) Shadow { get; private set; }

    /// <summary>Objekte 1 … 12: Figur und Position.</summary>
    public IReadOnlyList<(int Figure, int X, int Y)> Objects => _objects;

    /// <summary>Zahl der schon angewandten Bilder.</summary>
    public int FramesApplied { get; private set; }

    public bool Finished => FramesApplied >= Scene.Frames.Count;

    /// <summary>Wendet das nächste Bild an; liefert dessen Toneffekt (Byte 3, 0 = keiner) oder null am Ende.</summary>
    public int? Step()
    {
        if (Finished)
            return null;
        var frame = Scene.Frames[FramesApplied++];
        Camera = GoalSceneRink.MoveCamera(Camera, frame.Camera);
        var (px, py) = GoalScene.DecodeMove(frame.Puck);
        Shadow = (Shadow.X + px, Shadow.Y + py);
        for (var k = 0; k < _objects.Length; k++)
        {
            var (figure, move) = frame.Objects[k];
            var (x, y) = (_objects[k].X, _objects[k].Y);
            if ((figure | move) != 0)
            {
                var (dx, dy) = GoalScene.DecodeMove(move);
                (x, y) = (x + dx, y + dy);
            }
            _objects[k] = (figure, x, y);
        }
        return frame.Sound;
    }
}

/// <summary>
/// Grafik der Torszene (Spur torszenen, Teil C2): Eisflächen je Hallenstufe, Figurtafeln, eingebaute Figuren, Kopfbild und
/// Palette; zeichnet Bild und Kopfzeile in eine <see cref="Surface"/>.
/// <list type="bullet">
/// <item>[ORIGINAL] Bildschirm: Eis im Sichtfenster 320 × 200 oben, Kopfbild <c>kb\246</c> darunter (Zeilen 200 … 239; Video
///   <c>eisbaeren_f01_01150</c>, im Original über den Zeilenvergleich des CRTC, Datei 0x12D7C ff.).</item>
/// <item>[ORIGINAL] Zeichenfolge je Bild (Datei 0x12BDB–0x12C3F): Objekt 0, dann 1 … 12; linke obere Ecke (x − 80 | y − 96),
///   Farbe 0 durchsichtig, abgeschnitten am Sichtfenster.</item>
/// <item>[ORIGINAL] Trikotfarben (Datei 0x56C29–0x56C86): Palettenindex 0xFE = Heimfarbe, 0xFD = Gastfarbe, Komponenten des
///   16-Bit-Farbcodes (c &gt;&gt; 6) &amp; 63, (c &gt;&gt; 2) &amp; 63, (c &lt;&lt; 2) &amp; 63 (<c>50h:63Fh</c>); vorher
///   <see cref="SeparateColors"/>.</item>
/// </list>
/// </summary>
public sealed class GoalSceneGraphics
{
    /// <summary>[ORIGINAL] Palettenindizes der Trikotfarben (Datei 0x56C45, 0x56C75).</summary>
    public const byte HomeColorIndex = 0xFE, AwayColorIndex = 0xFD;

    /// <summary>[ORIGINAL] Kopfbild unter dem Eis.</summary>
    public const int HeaderTop = GoalSceneRink.ViewHeight;

    private readonly GameData _game;
    private readonly GoalSceneRink?[] _rinks = new GoalSceneRink?[5];
    private readonly Dictionary<int, SpriteSheet> _sheets;
    private readonly byte[][] _builtIn;
    private readonly Palette _palette;

    public GoalSceneGraphics(GameData game)
    {
        _game = game;
        _sheets = new() { [233] = SpriteSheet.Load(game.GraphicsPath(233)), [234] = SpriteSheet.Load(game.GraphicsPath(234)),
            [235] = SpriteSheet.Load(game.GraphicsPath(235)) };
        _builtIn = GoalSceneFigures.LoadBuiltIn(Path.Combine(game.RootDirectory, "em.exe"));
        _palette = Palette.Load(game.GraphicsPath(GoalSceneRink.PaletteGraphic));
        Header = GoalSceneRink.DecodeHeader(File.ReadAllBytes(game.GraphicsPath(GoalSceneRink.HeaderGraphic)));
    }

    /// <summary>Kopfbild <c>kb\246</c> (320 × 40).</summary>
    public VgaImage Header { get; }

    public GoalSceneRink Rink(int hallLevel)
    {
        var level = hallLevel is >= 0 and <= 4 ? hallLevel : 0;
        return _rinks[level] ??= GoalSceneRink.Load(_game, level);
    }

    /// <summary>
    /// [ORIGINAL] Trikotfarben entzerren 0x53BF9 (Datei 0x53BF9–0x53C56): Summe der Beträge der drei Halbbyte-Differenzen
    /// (Bits 0–3, 4–7, 8–11); unter 6 wird die Gastfarbe Heimfarbe XOR 0xFFF.
    /// </summary>
    public static (int Home, int Away) SeparateColors(int home, int away)
    {
        var sum = 0;
        for (var shift = 0; shift <= 8; shift += 4)
            sum += Math.Abs(((home >> shift) & 0xF) - ((away >> shift) & 0xF));
        return sum < 6 ? (home, home ^ 0xFFF) : (home, away);
    }

    /// <summary>[ORIGINAL] DAC-Werte (6 Bit) eines Farbcodes wie in Datei 0x56C29–0x56C44.</summary>
    public static (byte R, byte G, byte B) ColorDac(int code) =>
        ((byte)((code >> 6) & 63), (byte)((code >> 2) & 63), (byte)((code << 2) & 63));

    /// <summary>Palette <c>kb\247</c> mit den Trikotfarben (Farbcodes schon entzerrt).</summary>
    public Palette PaletteFor(int homeColor, int awayColor)
    {
        var palette = _palette;
        foreach (var (index, code) in new[] { (HomeColorIndex, homeColor), (AwayColorIndex, awayColor) })
        {
            var (r, g, b) = ColorDac(code);
            palette = palette.WithColor(index, Palette.DacToRgb(r), Palette.DacToRgb(g), Palette.DacToRgb(b));
        }
        return palette;
    }

    /// <summary>Zeichnet Eis, Puckschatten und Objekte des aktuellen Standes ins Sichtfenster (Zeilen 0 … 199).</summary>
    public void DrawField(Surface s, GoalSceneAnimation animation, int hallLevel)
    {
        var rink = Rink(hallLevel).Image;
        var (cx, cy) = animation.Camera;
        s.Blit(rink.Pixels, rink.Width, new Rect(cx, cy, GoalSceneRink.ViewWidth, GoalSceneRink.ViewHeight), 0, 0);
        DrawFigure(s, 1, animation.Shadow.X, animation.Shadow.Y, cx, cy);
        foreach (var (figure, x, y) in animation.Objects)
            DrawFigure(s, figure, x, y, cx, cy);
    }

    private void DrawFigure(Surface s, int figure, int x, int y, int cameraX, int cameraY)
    {
        if (GoalSceneFigures.Resolve(figure) is not { } source)
            return;
        var pixels = source.Graphic == 0 ? _builtIn[source.Index] : _sheets[source.Graphic][source.Index];
        var left = x - GoalSceneRink.FigureOffsetX - cameraX;
        var top = y - GoalSceneRink.FigureOffsetY - cameraY;
        for (var row = 0; row < source.Size; row++)
        {
            var sy = top + row;
            if (sy is < 0 or >= GoalSceneRink.ViewHeight)
                continue;
            for (var col = 0; col < source.Size; col++)
            {
                var sx = left + col;
                var value = pixels[row * source.Size + col];
                if (value != 0 && sx is >= 0 and < GoalSceneRink.ViewWidth)
                    s.Pixels[sy * Surface.Width + sx] = value;
            }
        }
    }

    /// <summary>
    /// [ORIGINAL] Kopfzeile 0x56EDD (Datei 0x56EDD–0x570E7; Koordinaten im Kopfbild, hier + 200): Modus 0 = Kopfbild neu,
    /// Modus ≠ 2 = Schrift 2, Kästen Farbe 4, Text Farbe 7 mit Schatten 0: Heimname (9 | 6)–(152 | 14) zentriert 9 … 151 auf
    /// Grundlinie 13, Gastname (169 | 6)–(312 | 14) zentriert 169 … 311, Zuschauertext (8 | 26)–(188 | 34) linksbündig ab 8 auf
    /// Grundlinie 33, Minutenzeile 0x570E8. Immer: Mindestbreite 2, w = Breite „00“, Torkästen (80 − w/2 | 16)–(80 − w/2 + w | 24)
    /// und (240 − w/2 | 16)–…, Tore zentriert 9 … 151 bzw. 169 … 311 auf Grundlinie 23.
    /// </summary>
    public void DrawHeader(Surface s, Assets assets, GoalSceneHeader header, int mode)
    {
        const int top = HeaderTop;
        var font = assets.Flow;
        if (mode == 0)
            s.Blit(Header, 0, top);
        if (mode != 2)
        {
            Box(s, 9, 6, 152, 14);
            CompetitionWidgets.Centered(s, font, 9, 151, top + 13, header.HomeName, Colors.White, Colors.Black);
            Box(s, 169, 6, 312, 14);
            CompetitionWidgets.Centered(s, font, 169, 311, top + 13, header.AwayName, Colors.White, Colors.Black);
            Box(s, 8, 26, 188, 34);
            CompetitionWidgets.Centered(s, font, 8, 0, top + 33, header.SpectatorText, Colors.White, Colors.Black, left: true);
            DrawMinute(s, assets, header.Minute);
        }
        var w = CompetitionWidgets.Width(font, "00");
        foreach (var (center, x1, x2, goals) in new[] { (80, 9, 151, header.HomeGoals), (240, 169, 311, header.AwayGoals) })
        {
            var left = center - w / 2; // [ORIGINAL] 0x47 − w/2 + 9 bzw. + 0xA9 (Datei 0x5701E–0x5702A, 0x57081–0x5708D)
            Box(s, left, 16, left + w, 24);
            CompetitionWidgets.Centered(s, font, x1, x2, top + 23, CompetitionWidgets.Number(goals, 2), Colors.White, Colors.Black);
        }
    }

    /// <summary>[ORIGINAL] Minutenzeile 0x570E8: Kasten (232 | 26)–(301 | 34) Farbe 4, Minute (Mindestbreite 2) + „.MINUTE“ ab 232.</summary>
    public static void DrawMinute(Surface s, Assets assets, int minute)
    {
        Box(s, 232, 26, 301, 34);
        CompetitionWidgets.Centered(s, assets.Flow, 232, 0, HeaderTop + 33, CompetitionWidgets.Number(minute, 2) + ".MINUTE",
            Colors.White, Colors.Black, left: true);
    }

    /// <summary>[ORIGINAL] Rechteck 0x0B620 mit eingeschlossenen Ecken, Farbe 4, im Kopfbild.</summary>
    private static void Box(Surface s, int x1, int y1, int x2, int y2) =>
        s.Fill(new Rect(x1, HeaderTop + y1, x2 - x1 + 1, y2 - y1 + 1), HeaderBoxColor);

    /// <summary>[ORIGINAL] Farbe der Kästen (Datei 0x56F15).</summary>
    public const byte HeaderBoxColor = 4;
}
