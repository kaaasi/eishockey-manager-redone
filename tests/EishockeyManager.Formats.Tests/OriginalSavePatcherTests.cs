using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Formats.Tests;

/// <summary>
/// <c>emtool hok patch</c>: Spielstände fürs Original erzeugen. Synthetische Tests bauen den Stand unabhängig vom
/// Patcher nach em.exe 0x5A894 (Verschlüsselung wie in <see cref="OriginalSaveGameTests"/>).
/// </summary>
public class OriginalSavePatcherTests
{
    private static KeyValuePair<string, string> C(string name, string value) => new(name, value);

    [Fact]
    public void Patch_ChangesOnlyNamedFields_AndKeepsHeaderFillerAndKey()
    {
        var template = Template();
        var before = OriginalSaveGame.Read(template);

        var patched = OriginalSavePatcher.Patch(template, [C("konto", "750000"), C("eintritt", "21")], newId: 1234);
        var after = OriginalSaveGame.Read(patched);

        Assert.True(after.ChecksumsValid);
        Assert.Equal(1234, after.SaveId);
        Assert.Equal(before.Filler, after.Filler);
        Assert.Equal(before.Key, after.Key);
        Assert.Equal(before.Managers, after.Managers);
        // Klartextkopf bis auf die ID byteweise gleich
        Assert.Equal(template[..0xB4], patched[..0xB4]);
        Assert.Equal(template[0xB8..0xBA], patched[0xB8..0xBA]);

        foreach (var (block, _, data) in before.Blocks)
        {
            var other = after.Block(block.Name);
            if (block.Name != "965B")
                Assert.Equal(data, other);
            else
            {
                var diff = Enumerable.Range(0, data.Length).Where(i => data[i] != other[i]).ToList();
                Assert.All(diff, i => Assert.True(i is >= 0x97EA - 0x965B and < 0x97EA - 0x965B + 4 || i == 0x973F - 0x965B));
                Assert.Equal(2_250_000, BitConverter.ToInt32(other, 0x97EA - 0x965B));
                Assert.Equal(21, other[0x973F - 0x965B]);
            }
        }
    }

    [Fact]
    public void Patch_WithoutChanges_OnlyChangesIdAndStaysDecodable()
    {
        var template = Template();
        var patched = OriginalSavePatcher.Patch(template, [], random: new Random(7));
        var a = OriginalSaveGame.Read(template);
        var b = OriginalSaveGame.Read(patched);

        Assert.True(b.ChecksumsValid);
        Assert.NotEqual(a.SaveId, b.SaveId);
        Assert.InRange(b.SaveId, short.MinValue, -1);
        Assert.Equal(template.Length, patched.Length);
        Assert.All(a.Blocks, x => Assert.Equal(x.Data, b.Block(x.Block.Name)));
    }

    /// <summary>Lesen und wieder Schreiben ergibt dieselben Bytes (Gegenprobe der Verschlüsselung und der Prüfsummen).</summary>
    [Fact]
    public void Encode_RoundTripsReadResult_ForRandomContent()
    {
        var rnd = new Random(42);
        var template = Template(fill: blocks =>
        {
            foreach (var b in blocks.Values.Where(v => v.Length > 1)) rnd.NextBytes(b);
            blocks["0386"][0] = 1;
        }, key: 0x9C);
        var save = OriginalSaveGame.Read(template);

        Assert.Equal(template, OriginalSavePatcher.Encode(template.AsSpan(0, OriginalSaveGame.PlainLength), save));
    }

    [Fact]
    public void Patch_DatumZaehlerSpieltagLevel_SetAllCoupledBytes()
    {
        var patched = OriginalSavePatcher.Patch(Template(),
            [C("datum", "20.09.1993"), C("zaehler", "11"), C("spieltag", "4"), C("level", "5")], newId: 5);
        var s = OriginalSaveGame.Read(patched);

        Assert.True(s.ChecksumsValid);
        Assert.Equal(20, BitConverter.ToInt32(s.Block("5A20")));
        Assert.Equal(8, BitConverter.ToInt32(s.Block("5A1C"))); // Monat 0-basiert
        Assert.Equal(1993, BitConverter.ToInt32(s.Block("5A18")));
        Assert.Equal(1993, s.HeaderYear);
        Assert.Equal(11, BitConverter.ToInt32(s.Block("03B7")));
        Assert.Equal(11, BitConverter.ToInt16(s.Block("A969")));
        Assert.Equal([4, 4, 4], s.Block("07A9").Take(3));
        Assert.Equal(4, s.HeaderByte7A9);
        Assert.Equal(5, s.Block("0118")[0]);
        Assert.Equal(0, s.Block("0ED8")[0]);
    }

    [Fact]
    public void Patch_StadiumAndSecondManager()
    {
        var template = Template(fill: b => b["0386"][0] = 2);
        var patched = OriginalSavePatcher.Patch(template,
            [C("stadion", "1,2,3,4,5,6,7,8,9"), C("sitz", "5000"), C("steh", "250")], manager: 1, newId: 9);
        var m = OriginalSaveGame.Read(patched).Block("965B");
        var o = OriginalSaveGame.ManagerRecordLength;

        Assert.Equal(Enumerable.Range(1, 9), Enumerable.Range(0, 9).Select(n => (int)BitConverter.ToInt16(m, o + 0x9771 - 0x965B + 2 * n)));
        Assert.Equal(5000, BitConverter.ToInt16(m, o + 0x9771 - 0x965B + 0x1D));
        Assert.Equal(250, BitConverter.ToInt16(m, o + 0x9771 - 0x965B + 0x21));
        Assert.All(m.AsSpan(0, o).ToArray(), x => Assert.Equal(0, x)); // Manager 0 unberührt
    }

    [Theory]
    [InlineData("gibtsnicht", "1")]   // unbekannt
    [InlineData("dauer", "2")]        // abgewiesen, nicht belegt
    [InlineData("liga", "1")]
    [InlineData("staerke", "2")]
    [InlineData("eintritt", "0")]     // Bereich
    [InlineData("eintritt", "100")]
    [InlineData("eintritt", "x")]
    [InlineData("level", "6")]
    [InlineData("datum", "31.02.1993")]
    [InlineData("datum", "1993")]
    [InlineData("stadion", "1,2,3")]
    [InlineData("stadion", "1,2,3,4,5,6,7,8,10")]
    [InlineData("spieltag", "0")]
    [InlineData("konto", "999999999999")]
    public void Patch_RejectsUnknownRejectedAndInvalidFields(string field, string value)
    {
        Assert.Throws<ArgumentException>(() => OriginalSavePatcher.Patch(Template(), [C(field, value)]));
    }

    [Fact]
    public void Patch_RejectsDuplicateFieldBadManagerAndBadTemplate()
    {
        var template = Template();
        Assert.Throws<ArgumentException>(() => OriginalSavePatcher.Patch(template, [C("konto", "1"), C("KONTO", "2")]));
        Assert.Throws<ArgumentException>(() => OriginalSavePatcher.Patch(template, [C("konto", "1")], manager: 1));
        template[^1] ^= 0xFF;
        Assert.Throws<InvalidDataException>(() => OriginalSavePatcher.Patch(template, [C("konto", "1")]));
    }

    [Fact]
    public void Patch_DoesNotModifyTemplateBytes()
    {
        var template = Template();
        var copy = (byte[])template.Clone();
        OriginalSavePatcher.Patch(template, [C("konto", "5")]);
        Assert.Equal(copy, template);
    }

    [Fact]
    public void NewId_StaysInObservedRangeAndDiffersFromOld()
    {
        var rnd = new Random(1);
        for (var i = 0; i < 20_000; i++)
        {
            var id = OriginalSavePatcher.NewId(5, rnd);
            Assert.NotEqual(5, id);
            Assert.InRange(id, short.MinValue, -1); // wie FILE01–03: 16 Bit 0x8000–0xFFFF, vorzeichenerweitert
        }
    }

    // ---- Originaldaten: Spielstände des Nutzers (Referenz-Repo original/spielstaende, nie im Code-Repo) ----

    [GameDataFact]
    public void Original_RoundTripIsByteIdentical_ForAllSavedGames()
    {
        foreach (var file in OriginalSaves())
        {
            var bytes = File.ReadAllBytes(file);
            var save = OriginalSaveGame.Read(bytes);
            Assert.Equal(bytes, OriginalSavePatcher.Encode(bytes.AsSpan(0, OriginalSaveGame.PlainLength), save));
        }
    }

    [GameDataFact]
    public void Original_PatchedFile01_HasValidChecksumsAndOnlyTheChangedFields()
    {
        var bytes = File.ReadAllBytes(OriginalSave("FILE01.HOK"));
        var before = OriginalSaveGame.Read(bytes);

        var patched = OriginalSavePatcher.Patch(bytes,
            [C("konto", "1000000"), C("eintritt", "25"), C("datum", "20.09.1993"), C("zaehler", "11"), C("spieltag", "4")],
            random: new Random(3));
        var after = OriginalSaveGame.Read(patched);

        Assert.True(after.ChecksumsValid);
        Assert.Equal(bytes.Length, patched.Length);
        Assert.NotEqual(before.SaveId, after.SaveId);
        Assert.Equal(before.Key, after.Key);
        Assert.Equal(before.Filler, after.Filler);
        Assert.Equal(3_000_000, BitConverter.ToInt32(after.Block("965B"), 0x97EA - 0x965B));
        Assert.Equal(25, after.Block("965B")[0x973F - 0x965B]);
        var changed = before.Blocks.Where(b => !b.Data.AsSpan().SequenceEqual(after.Block(b.Block.Name))).Select(b => b.Block.Name).Order();
        // FILE01 steht schon im 09.1993: vom Datum ändert sich nur der Tag.
        Assert.Equal(new[] { "03B7", "07A9", "5A20", "965B", "A969" }.Order(), changed);
    }

    /// <summary>FILE02 ist FILE01 mit Eintritt 17: gleicher Patch auf FILE01 ergibt denselben Klartext wie FILE02.</summary>
    [GameDataFact]
    public void Original_PatchingFile01WithEintritt17_EqualsPlaintextOfFile02()
    {
        var f1 = File.ReadAllBytes(OriginalSave("FILE01.HOK"));
        var f2 = OriginalSaveGame.Read(File.ReadAllBytes(OriginalSave("FILE02.HOK")));
        var p = OriginalSaveGame.Read(OriginalSavePatcher.Patch(f1, [C("eintritt", "17")]));

        Assert.All(f2.Blocks, b => Assert.Equal(b.Data, p.Block(b.Block.Name)));
    }

    private static IEnumerable<string> OriginalSaves() =>
        new[] { "FILE01.HOK", "FILE02.HOK", "FILE03.HOK" }.Select(OriginalSave);

    /// <summary>
    /// Ordner mit den Original-Spielständen: <c>EM_SAVEGAMES</c> oder <c>spielstaende</c> neben dem (aufgelösten)
    /// Ordner <c>gamedata</c> (Referenz-Repo: <c>original/gamedata</c>, <c>original/spielstaende</c>).
    /// </summary>
    private static string OriginalSave(string name)
    {
        var folder = Environment.GetEnvironmentVariable("EM_SAVEGAMES");
        if (string.IsNullOrEmpty(folder))
        {
            var root = TestGameData.Load().RootDirectory;
            var real = Directory.ResolveLinkTarget(root.TrimEnd('/', '\\'), true)?.FullName ?? root;
            folder = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(real).TrimEnd('/', '\\'))!, "spielstaende");
        }
        var path = Path.Combine(folder, name);
        return File.Exists(path) ? path
            : throw new FileNotFoundException($"Original-Spielstand {name} fehlt in {folder} (Ordner per EM_SAVEGAMES angeben).");
    }

    private static byte[] Template(Action<Dictionary<string, byte[]>>? fill = null, byte key = 0xE5)
    {
        var blocks = OriginalSaveGame.Layout.Where(b => !b.IsChecksum).ToDictionary(b => b.Name, b => new byte[b.Length]);
        blocks["0386"][0] = 1;
        fill?.Invoke(blocks);

        var file = new List<byte>();
        file.AddRange("KRON_0.99\0"u8.ToArray());
        file.AddRange(BitConverter.GetBytes(1993));
        file.Add(blocks["0386"][0]);
        file.Add(1);
        var header = new byte[0xA0];
        for (var m = 0; m < blocks["0386"][0]; m++)
        {
            "Joerg"u8.CopyTo(header.AsSpan(m * 0x28 + 2));
            "K\\LNER EC"u8.CopyTo(header.AsSpan(m * 0x28 + 0x15));
        }
        file.AddRange(header);
        file.AddRange("EH0\0"u8.ToArray());
        file.AddRange(BitConverter.GetBytes(-26229));
        file.Add(0x35);
        file.Add(key);

        byte k = key, sum = key;
        foreach (var block in OriginalSaveGame.Layout)
        {
            if (block.IsChecksum)
            {
                file.Add(sum);
                continue;
            }
            foreach (var plain in blocks[block.Name])
            {
                file.Add((byte)((plain ^ k) + k));
                sum += plain;
                k -= 0x29;
            }
        }
        return [.. file];
    }
}
