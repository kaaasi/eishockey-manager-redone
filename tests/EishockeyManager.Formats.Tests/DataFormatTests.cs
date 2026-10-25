using System.Buffers.Binary;
using System.Text;
using EishockeyManager.Formats;

namespace EishockeyManager.Formats.Tests;

public class PaletteTests
{
    [Theory]
    [InlineData(0x00, 0)]
    [InlineData(0x3F, 255)]
    [InlineData(0xE4, 0x24 << 2 | 0x24 >> 4)] // 8-Bit-Wert aus kb\21: nur die unteren 6 Bit zählen
    [InlineData(0x40, 0)]
    public void DacToRgb_MasksToSixBits(byte stored, int expected) =>
        Assert.Equal(expected, Palette.DacToRgb(stored));

    [Fact]
    public void StoredBytes_AreKeptForRoundTrip()
    {
        var raw = Enumerable.Range(0, Palette.StoredLength).Select(i => (byte)i).ToArray();
        var palette = Palette.FromBytes(raw);
        Assert.Equal(raw, palette.StoredBytes.ToArray());
    }
}

public class GameTextTests
{
    [Fact]
    public void Decode_MapsGameUmlautsAndStopsAtNull()
    {
        var bytes = Encoding.ASCII.GetBytes("EISB[REN K\\LN ]BER {|}~\0REST");
        Assert.Equal("EISBÄREN KÖLN ÜBER äöüß", GameText.Decode(bytes));
    }

    [Fact]
    public void Decode_Cp437Umlaut() =>
        Assert.Equal("ÖSTERREICH", GameText.Decode([0x99, .. "STERREICH"u8]));

    [Fact]
    public void Encode_ReplacesUnsupportedCharacters()
    {
        Assert.Equal("Cafe"u8.ToArray(), GameText.Encode("Café"));
        Assert.Equal("G]NTER"u8.ToArray(), GameText.Encode("GÜNTER"));
        Assert.Equal("a?b"u8.ToArray(), GameText.Encode("a[b")); // '[' ist im Spiel das Ä und wird ersetzt
    }

    [Fact]
    public void Encode_Decode_RoundTrip() =>
        Assert.Equal("Düsseldorfer EG", GameText.Decode(GameText.Encode("Düsseldorfer EG")));
}

public class ClubDatabaseTests
{
    private static byte[] CreateDatabase()
    {
        var data = new byte[ClubDatabase.FileLength];

        // Verein 0 mit zwei Spielern (Torhüter, Stürmer mit Ausländerflag)
        var club = ClubDatabase.SquadOffset;
        Encoding.ASCII.GetBytes("D]SSELDORFER EG").CopyTo(data, club);
        var slot0 = club + ClubDatabase.NameLength;
        Encoding.ASCII.GetBytes("HEISS").CopyTo(data, slot0);
        var slot12 = club + ClubDatabase.NameLength + 12 * ClubDatabase.PlayerRecordLength;
        Encoding.ASCII.GetBytes("TRUNTSCHKA").CopyTo(data, slot12);
        data[slot12 + ClubDatabase.PlayerNameLength] = 1;

        data[ClubDatabase.StrengthOffset] = 92;
        data[ClubDatabase.LogoOffset] = 0x50 + 5; // (0x55 + 0xB0) & 0x3F = 5
        data[ClubDatabase.ColorOffset] = 0xFF;      // hi = -1
        data[ClubDatabase.ColorOffset + 1] = 0x10;  // lo = 16

        // Europapokal-Gegner 64
        Encoding.ASCII.GetBytes("KLAGENFURTER AC").CopyTo(data, ClubDatabase.EuropeanNamesOffset);
        return data;
    }

    [Fact]
    public void Parse_ReadsClubWithSquad()
    {
        var db = ClubDatabase.Parse(CreateDatabase());
        var club = db[0];

        Assert.Equal(150, db.Clubs.Count);
        Assert.Equal("DÜSSELDORFER EG", club.Name);
        Assert.Equal(92, club.Strength);
        Assert.Equal(85, club.LogoGraphic);
        Assert.Equal(-256 + 16, club.ColorCode);
        Assert.Equal(ClubCategory.FirstLeague, club.Category);
        Assert.Equal(2, club.Players.Count);
        Assert.Equal(new DatabasePlayer("HEISS", false, PlayerRole.Goalkeeper), club.Players[0]);
        Assert.Equal(new DatabasePlayer("TRUNTSCHKA", true, PlayerRole.Forward), club.Players[1]);
    }

    [Fact]
    public void Parse_ReadsEuropeanOpponentWithoutSquad()
    {
        var club = ClubDatabase.Parse(CreateDatabase())[64];
        Assert.Equal("KLAGENFURTER AC", club.Name);
        Assert.Equal(ClubCategory.EuropeanOpponent, club.Category);
        Assert.Empty(club.Players);
        Assert.Null(club.LogoGraphic);
    }
}

public class RinkAndSpriteTests
{
    [Fact]
    public void TileSet_ConvertsColumnMajorToRowMajor()
    {
        var raw = new byte[RinkTileSet.BytesPerTile];
        raw[3 * 16 + 1] = 7; // Spalte x = 3, Zeile y = 1
        var tiles = new RinkTileSet(raw);
        Assert.Equal(7, tiles[0][1 * 16 + 3]);
    }

    [Fact]
    public void RinkMap_ReadsBigEndianAndMirrorFlag()
    {
        var data = new byte[RinkMap.ByteLength];
        BinaryPrimitives.WriteUInt16BigEndian(data, 0x8000 | 0x01F3);
        var map = RinkMap.Parse(data);
        Assert.Equal((0x1F3, true), map[0, 0]);
        Assert.Equal((0, false), map[1, 0]);
    }

    [Fact]
    public void RinkMap_RenderMirrorsTile()
    {
        var raw = new byte[RinkTileSet.BytesPerTile];
        raw[0] = 9; // Pixel (0, 0)
        var tiles = new RinkTileSet(raw);
        var data = new byte[RinkMap.ByteLength];
        BinaryPrimitives.WriteUInt16BigEndian(data, 0x8000);
        var image = RinkMap.Parse(data).Render(tiles);
        Assert.Equal(1152, image.Width);
        Assert.Equal(672, image.Height);
        Assert.Equal(9, image[15, 0]);
        Assert.Equal(0, image[0, 0]);
    }

    [Fact]
    public void PackedData_DecodesMultipleStreams()
    {
        var first = PackBits.Encode(Enumerable.Repeat((byte)1, 300).ToArray());
        var second = PackBits.Encode([2, 3, 4]);
        var body = first.Concat(second).ToArray();
        var file = new byte[2 + body.Length];
        BinaryPrimitives.WriteUInt16LittleEndian(file, (ushort)body.Length);
        body.CopyTo(file, 2);

        var output = PackedData.Decode(file);
        Assert.Equal(303, output.Length);
        Assert.Equal(4, output[302]);
    }

    [Fact]
    public void SpriteSheet_ContactSheetLayout()
    {
        var data = new byte[SpriteSheet.BytesPerFrame * 3];
        data[2 * SpriteSheet.BytesPerFrame] = 5; // Bild 2, Pixel (0, 0)
        var sheet = new SpriteSheet(data).ToContactSheet(perRow: 2);
        Assert.Equal(64, sheet.Width);
        Assert.Equal(64, sheet.Height);
        Assert.Equal(5, sheet[0, 32]);
    }
}

public class BitmapFontTests
{
    [Fact]
    public void Parse_ReadsHeaderAndGlyphs()
    {
        // Segment: Name bei 0x10, Kopf bei 0x20, Glyphen ab 0x100
        var segment = new byte[0x400];
        Encoding.ASCII.GetBytes("test.font\0").CopyTo(segment, 0x10);
        BinaryPrimitives.WriteUInt16LittleEndian(segment.AsSpan(0x20), 0x10); // Name
        BinaryPrimitives.WriteUInt16LittleEndian(segment.AsSpan(0x22), 2);    // Höhe
        BinaryPrimitives.WriteUInt16LittleEndian(segment.AsSpan(0x24), 1);    // Grundlinie

        // Alle Zeichen zeigen auf ein leeres Glyph der Breite 3 …
        segment[0x100] = 3;
        for (var c = 0; c < BitmapFont.CharCount; c++)
            BinaryPrimitives.WriteUInt16LittleEndian(segment.AsSpan(0x26 + c * 2), 0x100);

        // … außer 'A': Breite 2, Zeile 0 = 10, Zeile 1 = 01
        BinaryPrimitives.WriteUInt16LittleEndian(segment.AsSpan(0x26 + ('A' - 0x20) * 2), 0x110);
        segment[0x110] = 2;
        segment[0x111] = 0b1000_0000;
        segment[0x112] = 0b0100_0000;

        var font = BitmapFont.Parse(segment, 0x20);

        Assert.Equal("test.font", font.Name);
        Assert.Equal(2, font.Height);
        Assert.True(font[(byte)'A'][0, 0]);
        Assert.False(font[(byte)'A'][1, 0]);
        Assert.True(font[(byte)'A'][1, 1]);
        Assert.Equal(2 + 3, font.Measure("A "));
    }
}
