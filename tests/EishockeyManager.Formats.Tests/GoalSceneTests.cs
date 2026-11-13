using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Formats.Tests;

/// <summary>
/// Spur livespiel-kern, Teil A4: Leser der Torszenen <c>tore\</c> (<c>docs/analyse/re-livespiel.md</c> §11). Vor dem Teil rot:
/// es gab keinen Leser (<c>GoalScene</c> fehlte, Build-Fehler).
/// </summary>
public sealed class GoalSceneTests
{
    /// <summary>
    /// Abnahme A4: alle 48 Dateien lesbar, Bildzahl = (Länge − 86) / 28, Byte 2 = 0, Effektzählung 149/53/25 (Probe der Spur re-livespiel-2):
    /// Ton 3 (Puck) 149, Ton 4 (Check) 53, Ton 1 (Jubel) 25, Kennung „TOR!“.
    /// </summary>
    [GameDataFact]
    public void AllScenes_Readable()
    {
        var dir = TestGameData.Load().GoalScenesDirectory;
        var files = Directory.GetFiles(dir).Where(f => Path.GetFileName(f) != "anzahl").ToList();
        Assert.Equal(48, files.Count);
        var sounds = new Dictionary<int, int>();
        var frames = 0;
        foreach (var file in files)
        {
            var length = new FileInfo(file).Length;
            var scene = GoalScene.Load(file);
            Assert.Equal("TOR!", scene.Id);
            Assert.Equal((length - 86) / 28, scene.Frames.Count);
            Assert.Equal(0, (length - 86) % 28);
            Assert.All(scene.Frames, f => Assert.Equal(0, f.Unused));
            Assert.All(scene.Objects, o => Assert.Equal(0, o.Unused));
            foreach (var f in scene.Frames.Where(f => f.Sound != 0))
                sounds[f.Sound] = sounds.GetValueOrDefault(f.Sound) + 1;
            frames += scene.Frames.Count;
        }
        Assert.Equal(6600, frames);
        Assert.Equal((149, 53, 25), (sounds.GetValueOrDefault(3), sounds.GetValueOrDefault(4), sounds.GetValueOrDefault(1)));
        Assert.Equal([1, 3, 4], sounds.Keys.Order());
    }

    /// <summary>[ORIGINAL] <c>tore\anzahl</c> = <c>MAXSZENE:11|2|2</c>: normale 11, Penalty 2, sechs Feldspieler 2 (0x57480).</summary>
    [GameDataFact]
    public void Counts_FromOriginalFile()
    {
        var counts = GoalSceneCounts.Load(Path.Combine(TestGameData.Load().GoalScenesDirectory, "anzahl"));
        Assert.Equal(new GoalSceneCounts(11, 2, 2), counts);
    }

    [Theory]
    [InlineData("MAXSZENE:11|2|2", 11, 2, 2)]
    [InlineData("MAXSZENE:11|2|2\r\n", 11, 2, 2)]
    [InlineData("x 123,45,6", 123, 45, 6)]
    public void Counts_ReadBackwards(string text, int normal, int penalty, int six) =>
        Assert.Equal(new GoalSceneCounts(normal, penalty, six), GoalSceneCounts.Parse(System.Text.Encoding.ASCII.GetBytes(text)));

    [Fact]
    public void Counts_BelowOne_Fail() =>
        Assert.Throws<InvalidDataException>(() => GoalSceneCounts.Parse("MAXSZENE:0|2|2"u8));

    /// <summary>[ORIGINAL] Bewegungsbyte 48h:51ADh: Bit 7 Vorzeichen y, Bit 6 Vorzeichen x, Bits 5–3 |dx|, Bits 2–0 |dy|.</summary>
    [Theory]
    [InlineData(0x00, 0, 0)]
    [InlineData(0x3F, 7, 7)]
    [InlineData(0xFF, -7, -7)]
    [InlineData(0x4A, -1, 2)]
    [InlineData(0x8A, 1, -2)]
    public void DecodeMove(int move, int dx, int dy) => Assert.Equal((dx, dy), GoalScene.DecodeMove((byte)move));

    [Fact]
    public void Parse_Synthetic()
    {
        var data = new byte[GoalScene.HeaderSize + 2 * GoalScene.FrameSize + 2];
        "TOR!"u8.CopyTo(data);
        data[4] = 0x01; data[5] = 0x02; // Kamera x = 0x0102 (Big-Endian)
        data[0x0C + 1] = 7; data[0x0C + 3] = 9; // Objekt 0: Figur 7, x = 9
        data[GoalScene.HeaderSize + 3] = 4;
        data[GoalScene.HeaderSize + GoalScene.FrameSize + 4] = 12;
        data[^2] = 0xAA; data[^1] = 0xAA;
        var scene = GoalScene.Parse(data);
        Assert.Equal(0x0102, scene.CameraX);
        Assert.Equal(new GoalSceneObject(0, 7, 9, 0), scene.Objects[0]);
        Assert.Equal(2, scene.Frames.Count);
        Assert.Equal(4, scene.Frames[0].Sound);
        Assert.Equal(12, scene.Frames[1].Objects[0].Figure);
        Assert.Throws<InvalidDataException>(() => GoalScene.Parse(data.AsSpan(0, data.Length - 2)));
    }
}
