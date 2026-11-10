namespace EishockeyManager.Formats.Tests;

/// <summary>
/// Spur hok-export, Teil 1: <see cref="OriginalSaveWriter"/> schreibt einen Original-Spielstand von Grund auf (synthetisch, ohne
/// Originaldaten); <see cref="OriginalSaveGame.Read"/> liest Kopf, Blöcke und beide Prüfsummen zurück.
/// </summary>
public sealed class OriginalSaveWriterTests
{
    [Fact]
    public void Write_ThenRead_GivesSameHeaderBlocksAndValidChecksums()
    {
        var writer = new OriginalSaveWriter
        {
            HeaderYear = 1994, HeaderMatchDay = 7, SaveId = -20251, Filler = 0x5A, Key = 0xE5,
        };
        writer.Managers.Add(new OriginalSaveManagerHeader(1, false, "Joerg", "KÖLNER EC"));
        writer.Managers.Add(new OriginalSaveManagerHeader(2, false, "Anna", "EV LANDSHUT"));
        var random = new Random(7);
        foreach (var block in OriginalSaveGame.Layout.Where(b => !b.IsChecksum))
            random.NextBytes(writer.Block(block.Name));

        var bytes = writer.ToArray();
        var save = OriginalSaveGame.Read(bytes);

        Assert.Equal(OriginalSaveGame.FileLength, bytes.Length);
        Assert.True(save.ChecksumsValid);
        Assert.Equal(2, save.Checksums.Count);
        Assert.Equal(1994, save.HeaderYear);
        Assert.Equal(7, save.HeaderByte7A9);
        Assert.Equal(-20251, save.SaveId);
        Assert.Equal(0xE5, save.Key);
        Assert.Equal(0x5A, save.Filler);
        Assert.Equal(new[] { "Joerg", "Anna" }, save.Managers.Select(m => m.Name));
        Assert.Equal("KÖLNER EC", save.Managers[0].Club);
        Assert.Equal(2, save.Managers[1].StartLeague);
        foreach (var (block, _, data) in save.Blocks)
            Assert.Equal(writer.Block(block.Name), data);
        Assert.Equal(2, OriginalSaveHeader.TryRead(bytes)!.Managers.Count);
    }

    [Fact]
    public void Name_IsTruncatedAndNullTerminated()
    {
        var field = OriginalSaveWriter.Name("ABCDEFGHIJKLMNOPQRSTUVWXYZ");
        Assert.Equal(OriginalSaveWriter.NameLength, field.Length);
        Assert.Equal(0, field[^1]);
        Assert.Equal((byte)'A', field[0]);
        Assert.Equal(new byte[] { (byte)'[', 0 }, OriginalSaveWriter.Name("Ä", 2));
    }

    [Fact]
    public void Randomize_KeepsIdInOriginalRange()
    {
        var writer = new OriginalSaveWriter();
        writer.Randomize(new Random(1993));
        Assert.InRange(writer.SaveId & 0xFFFF, OriginalSavePatcher.MinSaveId16, 0xFFFF);
        Assert.True(writer.SaveId < 0);
    }

    [Fact]
    public void Block_UnknownName_Throws() =>
        Assert.Throws<KeyNotFoundException>(() => new OriginalSaveWriter().Block("FFFF"));

    [Fact]
    public void TooManyManagers_Throws()
    {
        var writer = new OriginalSaveWriter();
        for (var i = 0; i < 5; i++)
            writer.Managers.Add(new OriginalSaveManagerHeader(0, false, $"M{i}", "X"));
        Assert.Throws<InvalidOperationException>(() => writer.ToArray());
    }
}
