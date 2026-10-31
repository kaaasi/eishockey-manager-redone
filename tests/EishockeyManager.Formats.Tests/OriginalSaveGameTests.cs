using EishockeyManager.Formats;

namespace EishockeyManager.Formats.Tests;

/// <summary>
/// Original-Spielstände <c>*.HOK</c>, nur mit synthetischen Bytes (keine Originaldateien im Repo).
/// Die Verschlüsselung wird hier unabhängig vom Leser nach em.exe 0x5A894 nachgebaut.
/// </summary>
public class OriginalSaveGameTests
{
    /// <summary>[ORIGINAL] Länge der vom Nutzer gelieferten Spielstände FILE01/FILE02 (47.949 Byte).</summary>
    [Fact]
    public void Layout_HasOriginalFileLengthAndChecksumPositions()
    {
        Assert.Equal(47_949, OriginalSaveGame.FileLength);
        Assert.Equal(94, OriginalSaveGame.Layout.Count(b => !b.IsChecksum));
        Assert.Equal(2, OriginalSaveGame.Layout.Count(b => b.IsChecksum));
        Assert.True(OriginalSaveGame.Layout[^1].IsChecksum);

        var save = OriginalSaveGame.Read(Build(_ => { }, key: 0x6E));
        Assert.Equal([0x8F09, 0xBB4C], save.Checksums.Select(c => c.FileOffset));
    }

    [Fact]
    public void DecryptByte_InvertsEncryptionForAllValues()
    {
        for (var key = 0; key < 256; key++)
        for (var plain = 0; plain < 256; plain++)
            Assert.Equal((byte)plain, OriginalSaveGame.DecryptByte(Encrypt((byte)plain, (byte)key), (byte)key));
    }

    [Fact]
    public void Read_DecodesKnownFieldsAndValidChecksums()
    {
        var file = Build(blocks =>
        {
            var manager = blocks["965B"];
            "Joerg"u8.CopyTo(manager);
            manager[0x14] = 2;
            manager[0x973F - 0x965B] = 13;
            BitConverter.GetBytes(1_500_000).CopyTo(manager, 0x97EA - 0x965B);
            BitConverter.GetBytes(1993).CopyTo(blocks["5A18"], 0);
            BitConverter.GetBytes(8).CopyTo(blocks["5A1C"], 0);
            BitConverter.GetBytes(10).CopyTo(blocks["5A20"], 0);
            blocks["0386"][0] = 1;
        }, key: 0xE5);

        var save = OriginalSaveGame.Read(file);

        Assert.True(save.ChecksumsValid);
        Assert.Equal(0xE5, save.Key);
        var manager = save.Block("965B");
        Assert.Equal(13, manager[0x973F - 0x965B]);
        Assert.Equal(1_500_000, BitConverter.ToInt32(manager, 0x97EA - 0x965B));
        Assert.Equal(1993, BitConverter.ToInt32(save.Block("5A18")));
        Assert.Single(save.Managers);
        Assert.Equal("Joerg", save.Managers[0].Name);
        Assert.Equal("KÖLNER EC", save.Managers[0].Club);
    }

    /// <summary>[ORIGINAL] Am Ende von FILE01 stehen Nullbytes; verschlüsselt ergibt das 2·k mit k −= 0x29 je Byte.</summary>
    [Fact]
    public void ZeroBytes_EncryptToDoubleKeyStepping()
    {
        var file = Build(_ => { }, key: 0x77);
        var tail = file.AsSpan(file.Length - 5, 4).ToArray();
        for (var i = 1; i < tail.Length; i++)
            Assert.Equal((byte)(tail[i - 1] - 0x52), tail[i]);
    }

    /// <summary>Kaderplatz und Pool-Spieler wie an FILE05 belegt (Werte von „MENDE“ bzw. „KÜNAST“ nachgebildet).</summary>
    [Fact]
    public void Squad_ReadsSlotAndPoolFields()
    {
        var file = Build(blocks =>
        {
            var pool = blocks["far:8253"];
            "MENDE"u8.CopyTo(pool.AsSpan(27 * 33));
            pool[27 * 33 + 0x12] = 1;
            pool[27 * 33 + 0x14] = 27;
            pool[27 * 33 + 0x18] = 40;
            "K]NAST"u8.CopyTo(pool.AsSpan(1 * 33));

            var squad = blocks["5DC3"];
            var slot = squad.AsSpan(7 * 0x34, 0x34);
            slot[0x0A] = 1;
            slot[0x0B] = 2;
            slot[0x0F] = 27;
            slot[0x10] = 80; slot[0x11] = 82; slot[0x12] = 60;
            slot[0x15] = 0x01;
            slot[0x06] = 3;
            slot[0x1C] = 3;
            slot[0x03] = 1;
            slot[0x00] = 1;
            slot[0x01] = 2;
            slot[0x13] = 50;
            BitConverter.GetBytes(12_100 * 3).CopyTo(slot[0x28..]);
            BitConverter.GetBytes(305_000 * 3).CopyTo(slot[0x2C..]);

            var market = squad.AsSpan(4 * 0x618, 0x34);
            market[0x0F] = 1;
            market[0x15] = 0x83;
            BitConverter.GetBytes(435_000 * 3).CopyTo(market[0x2C..]);
        }, key: 0x42);

        var save = OriginalSaveGame.Read(file);

        var player = Assert.Single(save.Squad(0));
        Assert.Equal((7, 27, "MENDE"), (player.Slot, player.PoolIndex, player.Name));
        Assert.Equal((80, 82, 60), (player.Strength1, player.Strength2, player.Strength3));
        Assert.Equal((27, 1, 40), (player.Age, player.Talent, player.RoleValue));
        Assert.True(player.Dressed);
        Assert.Equal(2, player.ContractYears);
        Assert.Equal((1, false), (player.Line, player.Forward));
        Assert.Equal((3, 3, 1), (player.Games, player.ClubGames, player.Goals));
        Assert.Equal((2, 1, 33), (player.TwoMinutePenalties, player.FiveMinutePenalties, player.E));
        Assert.False(player.Insured);
        Assert.Equal((12_100 * 3, 305_000 * 3), (player.SalaryThirds, player.PriceThirds));

        var offer = Assert.Single(save.Squad(4));
        Assert.Equal("KÜNAST", offer.Name);
        Assert.Equal((3, true, false), (offer.Line, offer.Forward, offer.Dressed));
        Assert.Equal(435_000 * 3, offer.PriceThirds);
    }

    /// <summary>
    /// Regressionstest #230: Tore = Summe aus +0x03, +0x04 und +0x05 (em.exe 0x421F4–0x22A), Versicherung auch dann, wenn nur
    /// das obere Wort +0x32 gesetzt ist (0x42413–0x4241B: <c>or</c> beider Wörter), Torhüter ohne Torsumme.
    /// </summary>
    [Fact]
    public void Squad_GoalsSumAllThreeCompetitions_InsuredUsesBothWords_GoalkeeperRole()
    {
        var file = Build(blocks =>
        {
            var pool = blocks["far:8253"];
            "TORWART"u8.CopyTo(pool.AsSpan(5 * 33));
            pool[5 * 33 + 0x18] = 0;
            "STUERMER"u8.CopyTo(pool.AsSpan(6 * 33));
            pool[6 * 33 + 0x18] = 80;

            var squad = blocks["5DC3"];
            var a = squad.AsSpan(0, 0x34);
            a[0x0F] = 6; a[0x03] = 1; a[0x04] = 2; a[0x05] = 4; a[0x32] = 1; // nur das obere Wort
            var b = squad.AsSpan(0x34, 0x34);
            b[0x0F] = 5; b[0x30] = 1; // nur das untere Wort
            var c = squad.AsSpan(2 * 0x34, 0x34);
            c[0x0F] = 6; c[0x04] = 3;
        }, key: 0x42);

        var list = OriginalSaveGame.Read(file).Squad(0);

        Assert.Equal(3, list.Count);
        Assert.Equal((7, true), (list[0].Goals, list[0].Insured));
        Assert.True(list[1].IsGoalkeeper);
        Assert.True(list[1].Insured);
        Assert.Equal((3, false), (list[2].Goals, list[2].Insured));
        Assert.False(list[2].IsGoalkeeper);
    }

    [Fact]
    public void Read_ReportsWrongChecksum()
    {
        var file = Build(_ => { }, key: 0x80);
        file[^1] ^= 0xFF;

        var save = OriginalSaveGame.Read(file);

        Assert.True(save.Checksums[0].IsValid);
        Assert.False(save.Checksums[1].IsValid);
        Assert.False(save.ChecksumsValid);
    }

    [Fact]
    public void Read_RejectsForeignFiles()
    {
        var file = Build(_ => { }, key: 1);
        Assert.Throws<InvalidDataException>(() => OriginalSaveGame.Read(file.AsSpan(0, 100)));
        Assert.Throws<InvalidDataException>(() => OriginalSaveGame.Read(file.AsSpan(0, file.Length - 1)));
        file[0] = (byte)'X';
        Assert.Throws<InvalidDataException>(() => OriginalSaveGame.Read(file));
    }

    private static byte Encrypt(byte plain, byte key) => (byte)((plain ^ key) + key);

    /// <summary>Baut einen Spielstand wie die Sicherungsroutine: Klartextkopf, Schlüssel, Blöcke, Prüfsummen.</summary>
    private static byte[] Build(Action<Dictionary<string, byte[]>> fill, byte key)
    {
        var blocks = OriginalSaveGame.Layout.Where(b => !b.IsChecksum).ToDictionary(b => b.Name, b => new byte[b.Length]);
        fill(blocks);

        var file = new List<byte>();
        file.AddRange("KRON_0.99\0"u8.ToArray());
        file.AddRange(BitConverter.GetBytes(1993));
        file.Add(blocks["0386"][0]);
        file.Add(1);
        var header = new byte[0xA0];
        for (var m = 0; m < blocks["0386"][0]; m++)
        {
            blocks["965B"].AsSpan(m * 0x1AF, 0x13).CopyTo(header.AsSpan(m * 0x28 + 2));
            "K\\LNER EC"u8.CopyTo(header.AsSpan(m * 0x28 + 0x15));
        }
        file.AddRange(header);
        file.AddRange("EH0\0"u8.ToArray());
        file.AddRange(BitConverter.GetBytes(4711));
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
                file.Add(Encrypt(plain, k));
                sum += plain;
                k -= 0x29;
            }
        }
        return [.. file];
    }
}
