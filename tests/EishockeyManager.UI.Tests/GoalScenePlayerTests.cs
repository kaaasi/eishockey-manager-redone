using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens.Live;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur torszenen, Teil C2: Abspieler 0x567C8 (<c>docs/analyse/re-livespiel.md</c> §16). Vor dem Teil rot: es gab keinen
/// Abspieler (<c>GoalSceneAnimation</c>, <c>GoalSceneGraphics</c>, <c>GoalSceneScreen</c> fehlten, Build-Fehler).
/// Vorlage: Video <c>eisbaeren_f01_01150</c> = Szene <c>7.at</c>/<c>7.av</c> nach 33 Bildern (Kamera (112 | 528)).
/// </summary>
public sealed class GoalScenePlayerTests
{
    /// <summary>[ORIGINAL] 0x53BF9: Summe der Halbbyte-Abstände &lt; 6 → Gastfarbe = Heimfarbe XOR 0xFFF.</summary>
    [Theory]
    [InlineData(0xF00, 0xF01, 0x0FF)] // Abstand 1
    [InlineData(0x123, 0x234, 0xEDC)] // 1 + 1 + 1 = 3
    [InlineData(0x123, 0x128, 0xEDC)] // 5
    [InlineData(0x123, 0x129, 0x129)] // 6 → bleibt
    [InlineData(0xF00, 0xFFF0, 0xFFF0)] // Eisbären Berlin – EC Ratingen
    [InlineData(0xF00F, 0x000F, 0xFFF0)] // Bits 12–15 zählen nicht, XOR nur über 0xFFF
    public void SeparateColors(int home, int away, int expectedAway) =>
        Assert.Equal((home, expectedAway), GoalSceneGraphics.SeparateColors(home, away));

    /// <summary>[ORIGINAL] Datei 0x56C29–0x56C44: (c &gt;&gt; 6) &amp; 63, (c &gt;&gt; 2) &amp; 63, (c &lt;&lt; 2) &amp; 63.</summary>
    [Fact]
    public void ColorDac()
    {
        Assert.Equal(((byte)60, (byte)0, (byte)0), GoalSceneGraphics.ColorDac(0xF00));
        Assert.Equal(((byte)63, (byte)60, (byte)0), GoalSceneGraphics.ColorDac(0xFFF0));
        Assert.Equal(((byte)0, (byte)3, (byte)60), GoalSceneGraphics.ColorDac(0x00F));
    }

    /// <summary>Synthetische Szene: Startkamera, Bewegungen, Figurwechsel, Ton je Bild, Ende.</summary>
    [Fact]
    public void Animation_AppliesFrames()
    {
        var data = new byte[GoalScene.HeaderSize + 2 * GoalScene.FrameSize + 2];
        "TOR!"u8.CopyTo(data);
        void Word(int o, int v) { data[o] = (byte)(v >> 8); data[o + 1] = (byte)v; }
        Word(4, 20); Word(6, 10); Word(8, 300); Word(10, 400); // Kamera (20 | 10) → Start (4 | 0)
        data[0x0D] = 5; Word(0x0E, 100); Word(0x10, 200); // Objekt 1: Figur 5 bei (100 | 200)
        var f0 = GoalScene.HeaderSize;
        data[f0] = 0b1000_1001; data[f0 + 1] = 0b0100_1000; data[f0 + 3] = 3; data[f0 + 4] = 7; data[f0 + 5] = 0b0001_0010;
        var f1 = f0 + GoalScene.FrameSize;
        data[f1] = 0b1000_0111; // Kamera y − 7 → begrenzt auf 0
        Word(f1 + GoalScene.FrameSize, GoalScene.EndMarker);
        var a = new GoalSceneAnimation(GoalScene.Parse(data));
        Assert.Equal((4, 0), a.Camera);
        Assert.Equal(3, a.Step());
        Assert.Equal((5, 0), a.Camera); // dx +1, dy −1 → 0
        Assert.Equal((299, 400), a.Shadow);
        Assert.Equal((7, 102, 202), a.Objects[0]);
        Assert.Equal(0, a.Step());
        Assert.Equal((0, 102, 202), a.Objects[0]); // Figur übernommen, ohne Bewegung keine Verschiebung
        Assert.Equal((5, 0), a.Camera);
        Assert.True(a.Finished);
        Assert.Null(a.Step());
    }

    private static (ScreenHost Host, GoalSceneGraphics Graphics, GameData Data) Load()
    {
        var data = TestGameData.Load();
        return (new ScreenHost(new Assets(data)), new GoalSceneGraphics(data), data);
    }

    private static readonly GoalSceneHeader BerlinRatingen = new("EISBÄREN BERLIN", "EC RATINGEN", 0, 0, 6, " ZUSCHAUER");

    /// <summary>
    /// Abnahme C2: Szene <c>7.at</c> nach 33 Bildern gegen <c>eisbaeren_f01_01150</c> (Vergleich im Scratch: Eis und Figuren
    /// mittlere Abweichung 15,0 je Pixel, Kopfzeile ohne Verschiebung am besten). Hier die vermessenen Stellen: Puck grau in
    /// (84 … 88 | 87 … 91), Puckschatten ein Pixel tiefer, Trikot Gast gelb, Textkästen der Kopfzeile pixelgleich mit dem Video
    /// (helle Pixel: Heimname x 35 … 124, Gastname 206 … 273, y 207 … 213; Tore x 80 … 85 bzw. 240 … 245, y 217 … 223;
    /// „6.MINUTE“ x 240 … 287, y 227 … 233).
    /// </summary>
    [GameDataFact]
    public void Scene7at_Frame33_MatchesVideo()
    {
        var (host, graphics, data) = Load();
        var scene = GoalScene.Load(Path.Combine(data.GoalScenesDirectory, "7.at"));
        var db = host.Assets.Database.Clubs;
        int Color(string name) => db.First(c => c.Name == name).ColorCode;
        var request = new GoalSceneRequest(scene, Goal: true, Side: 1, HallLevel: 4, Color("EISBÄREN BERLIN"), Color("EC RATINGEN"),
            BerlinRatingen);
        var screen = new GoalSceneScreen(host, request, graphics, () => { });
        for (var i = 0; i < 33; i++)
            screen.Tick();
        Assert.Equal((112, 528), screen.Animation.Camera);
        Assert.Equal((276, 712), screen.Animation.Shadow);
        Assert.Equal((0, 276, 711), screen.Animation.Objects[10]);

        var s = new Surface();
        screen.Draw(s);
        byte At(int x, int y) => s.Pixels[y * Surface.Width + x];
        Assert.Equal(0xC6, At(86, 89));
        Assert.Equal(0xC7, At(84, 88));
        Assert.Equal(0xFB, At(86, 92));
        var (r, g, b) = s.Palette[GoalSceneGraphics.AwayColorIndex];
        Assert.True(r > 240 && g > 230 && b < 10, "Gast gelb");
        Assert.Equal((Palette.DacToRgb(60), (byte)0, (byte)0), s.Palette[GoalSceneGraphics.HomeColorIndex]);

        (int, int, int, int) Box(int x0, int x1, int y0, int y1)
        {
            var p = Enumerable.Range(y0, y1 - y0).SelectMany(y => Enumerable.Range(x0, x1 - x0).Select(x => (x, y)))
                .Where(q => At(q.x, q.y) == Colors.White).ToList();
            return (p.Min(q => q.x), p.Min(q => q.y), p.Max(q => q.x), p.Max(q => q.y));
        }
        Assert.Equal((35, 207, 124, 213), Box(9, 153, 204, 216));
        Assert.Equal((206, 207, 273, 213), Box(169, 313, 204, 216));
        Assert.Equal((80, 217, 85, 223), Box(60, 100, 215, 226));
        Assert.Equal((240, 217, 245, 223), Box(220, 260, 215, 226));
        Assert.Equal((240, 227, 287, 233), Box(232, 310, 225, 236));
    }

    /// <summary>
    /// Töne je Bild wie Byte 3 (Ton s → <c>kb\(237 + s)</c>), Ende: Tor der Gastseite → Kopfzeile 0 : 1, dann Pause 6 = 2 Takte.
    /// Abspielen mit 60 Bildern je Sekunde: ein Szenenbild je 18,2-Hz-Takt.
    /// </summary>
    [GameDataFact]
    public void Scene7at_SoundsAndEnd()
    {
        var (host, graphics, data) = Load();
        var scene = GoalScene.Load(Path.Combine(data.GoalScenesDirectory, "7.at"));
        var finished = 0;
        var screen = new GoalSceneScreen(host, new GoalSceneRequest(scene, true, 1, 2, 0xF00, 0xFFF0, BerlinRatingen),
            graphics, () => finished++);
        var updates = 0;
        while (!screen.Done && updates < 10_000)
        {
            screen.Update(new InputState());
            updates++;
        }
        Assert.Equal(1, finished);
        Assert.Equal(scene.Frames.Where(f => f.Sound != 0).Select(f => (int)f.Sound), screen.Sounds);
        Assert.Equal((0, 1), (screen.Header.HomeGoals, screen.Header.AwayGoals));
        var ticks = scene.Frames.Count + 1 + GoalSceneScreen.EndPauseTicks;
        Assert.InRange(updates, (int)(ticks * GoalSceneScreen.FramesPerTick) - 1, (int)(ticks * GoalSceneScreen.FramesPerTick) + 2);

        var continuous = new GoalSceneScreen(host, new GoalSceneRequest(scene, false, 0, 2, 0xF00, 0xFFF0, BerlinRatingen, true),
            graphics, () => { });
        for (var i = 0; i <= scene.Frames.Count; i++)
            continuous.Tick();
        Assert.True(continuous.Done); // Modus 2: keine Pause
        Assert.Equal((0, 0), (continuous.Header.HomeGoals, continuous.Header.AwayGoals));
    }
}
