using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Formats.Tests;

/// <summary>
/// Spur torszenen, Teil C1: Figuren und Eisfläche der Torszene (<c>docs/analyse/re-livespiel.md</c> §16). Vor dem Teil rot:
/// es gab weder Figurzuordnung noch Eisfläche (<c>GoalSceneFigures</c>, <c>GoalSceneRink</c> fehlten, Build-Fehler).
/// </summary>
public sealed class GoalSceneRinkTests
{
    /// <summary>[ORIGINAL] Datei 0x129E7–0x12A50: Grenzen der drei Figurtafeln und der eingebauten Figuren.</summary>
    [Theory]
    [InlineData(0, 0, 0, 5)]
    [InlineData(1, 0, 1, 5)]
    [InlineData(2, 233, 0, 32)]
    [InlineData(41, 233, 39, 32)]
    [InlineData(42, 234, 0, 32)]
    [InlineData(81, 234, 39, 32)]
    [InlineData(82, 235, 0, 32)]
    [InlineData(93, 235, 11, 32)]
    public void Figure_Resolves(int figure, int graphic, int index, int size) =>
        Assert.Equal(new GoalSceneFigureSource(graphic, index, size), GoalSceneFigures.Resolve(figure));

    [Theory]
    [InlineData(94)]
    [InlineData(255)]
    public void Figure_Above93_NotDrawn(int figure) => Assert.Null(GoalSceneFigures.Resolve(figure));

    /// <summary>[ORIGINAL] Kamera begrenzt auf 0 … 0x160 bzw. 0 … 0x3B8 (Datei 0x127B7–0x12869); Start Kopf − (16 | 32), ≥ 0.</summary>
    [Fact]
    public void Camera_ClampsAndMoves()
    {
        Assert.Equal((3, 0), GoalSceneRink.MoveCamera((0, 2), 0b1001_1010)); // dx = +3, dy = −2
        Assert.Equal((0, 952), GoalSceneRink.MoveCamera((2, 950), 0b0111_1111)); // dx = −7, dy = +7
        Assert.Equal((352, 0), GoalSceneRink.MoveCamera((350, 0), 0b0011_1000));
        Assert.Equal((GoalSceneRink.Width, GoalSceneRink.Height), (672, 1152));
    }

    [Fact]
    public void MapGraphic_ByHallLevel()
    {
        Assert.Equal([236, 242, 243, 244, 245], Enumerable.Range(0, 5).Select(GoalSceneRink.MapGraphic));
        Assert.Equal(236, GoalSceneRink.MapGraphic(5));
    }

    /// <summary>
    /// Abnahme C1: jede in den 48 Szenen vorkommende Figurnummer (Kopf und Bilder) hat ein Bild – Tafeln <c>kb\233 … 235</c>
    /// mit 40/40/12 Bildern (Größentabelle DGROUP 0x5404 … 0x5409), eingebaute Figuren aus em.exe.
    /// </summary>
    [GameDataFact]
    public void AllSceneFigures_HaveImage()
    {
        var data = TestGameData.Load();
        var sheets = new Dictionary<int, SpriteSheet>
        {
            [233] = SpriteSheet.Load(data.GraphicsPath(233)),
            [234] = SpriteSheet.Load(data.GraphicsPath(234)),
            [235] = SpriteSheet.Load(data.GraphicsPath(235)),
        };
        Assert.Equal((40, 40, 12), (sheets[233].Count, sheets[234].Count, sheets[235].Count));
        var builtIn = GoalSceneFigures.LoadBuiltIn(Path.Combine(data.RootDirectory, "em.exe"));
        Assert.All(builtIn, b => Assert.Equal(25, b.Length));
        Assert.Equal([0x00, 0xC6, 0xC7], builtIn[0].Distinct().Order().Select(b => (int)b)); // grauer Puck (Video eisbaeren_f01_01150)
        Assert.Equal([0x00, 0xFB], builtIn[1].Distinct().Order().Select(b => (int)b));

        var figures = new SortedSet<int>();
        foreach (var file in Directory.GetFiles(data.GoalScenesDirectory).Where(f => Path.GetFileName(f) != "anzahl"))
        {
            var scene = GoalScene.Load(file);
            figures.UnionWith(scene.Objects.Select(o => (int)o.Figure));
            figures.UnionWith(scene.Frames.SelectMany(f => f.Objects).Select(o => (int)o.Figure));
        }
        Assert.Equal(86, figures.Count);
        Assert.Equal((0, 93), (figures.Min, figures.Max));
        foreach (var figure in figures)
        {
            var source = GoalSceneFigures.Resolve(figure);
            Assert.NotNull(source);
            var s = source.Value;
            if (s.Graphic == 0)
                Assert.InRange(s.Index, 0, 1);
            else
                Assert.InRange(s.Index, 0, sheets[s.Graphic].Count - 1);
        }
    }

    /// <summary>
    /// Abnahme C1: Eisfläche als Bild (672 × 1 152) für alle fünf Hallenstufen; Ausschnitt vermessen gegen
    /// <c>eisbaeren_f01_01150</c>: Kamera (112 | 528) – blauer Anstoßpunkt im Video bei x 219 … 228, y 41 … 54, rote Mittellinie
    /// in den Zeilen 46/47 (Template-Matching über alle Kamerapositionen, §16).
    /// </summary>
    [GameDataFact]
    public void Rink_Renders_AndMatchesVideoMeasurement()
    {
        var data = TestGameData.Load();
        for (var level = 0; level <= 4; level++)
        {
            Assert.Equal(GoalSceneRink.Columns * GoalSceneRink.Rows * 2,
                new FileInfo(data.GraphicsPath(GoalSceneRink.MapGraphic(level))).Length);
            var rink = GoalSceneRink.Load(data, level);
            Assert.Equal((672, 1152), (rink.Image.Width, rink.Image.Height));
            Assert.NotNull(rink.Image.Palette);

            var view = rink.View((112, 528));
            Assert.Equal((320, 200), (view.Width, view.Height));
            const byte dotBlue = 236;
            var dot = Enumerable.Range(30, 35).SelectMany(y => Enumerable.Range(200, 50).Select(x => (x, y)))
                .Where(p => view.Pixels[p.y * 320 + p.x] == dotBlue).ToList();
            Assert.Equal((219, 41), (dot.Min(p => p.x), dot.Min(p => p.y)));
            Assert.InRange(dot.Max(p => p.x), 228, 229);
            Assert.Equal(54, dot.Max(p => p.y));
            var (r, _, b) = rink.Image.Palette![dotBlue];
            Assert.True(r == 0 && b > 200, "Anstoßpunkt blau wie im Video");
            foreach (var y in new[] { 46, 47 })
            {
                var line = Enumerable.Range(0, 320).Count(x => rink.Image.Palette[view.Pixels[y * 320 + x]] is var c && c.B < 160);
                Assert.True(line > 280, $"Mittellinie in Zeile {y}: {line} Pixel");
            }
            Assert.True(Enumerable.Range(0, 320).Count(x => rink.Image.Palette[view.Pixels[44 * 320 + x]] is var c && c.B < 160) < 20);
        }
    }
}
