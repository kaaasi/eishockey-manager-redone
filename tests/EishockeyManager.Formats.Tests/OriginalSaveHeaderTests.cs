using System.Text;
using EishockeyManager.Formats;

namespace EishockeyManager.Formats.Tests;

/// <summary>
/// Kopf-Leser der Spielstand-Info (Spur hok-import Teil 5, A339; em.exe Datei 0x5B661–0x5B736). Synthetische Köpfe,
/// keine Originaldaten nötig; der Abgleich mit FILE46 steht in <c>SaveLoadDialogOriginalTests</c>.
/// </summary>
public class OriginalSaveHeaderTests
{
    private static byte[] Header(int year, byte count, byte matchDay, params (byte League, byte World, string Name, string Club)[] managers)
    {
        var file = new byte[OriginalSaveHeader.Length];
        Encoding.ASCII.GetBytes(OriginalSaveGame.Magic).CopyTo(file, 0);
        BitConverter.GetBytes(year).CopyTo(file, 0x0A);
        file[0x0E] = count;
        file[0x0F] = matchDay;
        for (var i = 0; i < managers.Length; i++)
        {
            var e = 0x10 + 0x28 * i;
            file[e] = managers[i].League;
            file[e + 1] = managers[i].World;
            Encoding.ASCII.GetBytes(managers[i].Name).CopyTo(file, e + 2);
            Encoding.ASCII.GetBytes(managers[i].Club).CopyTo(file, e + 0x15);
        }
        return file;
    }

    [Fact]
    public void TryRead_ReturnsYearMatchDayAndManagers()
    {
        var info = OriginalSaveHeader.TryRead(Header(1994, 2, 7, (0, 0, "Anna", "KOELNER EC"), (2, 1, "Bernd", "EV LANDSHUT")));
        Assert.NotNull(info);
        Assert.Equal(1994, info.Year);
        Assert.Equal(7, info.MatchDay);
        Assert.Equal(
            [new OriginalSaveManagerHeader(0, false, "Anna", "KOELNER EC"), new OriginalSaveManagerHeader(2, true, "Bernd", "EV LANDSHUT")],
            info.Managers);
    }

    [Fact]
    public void TryRead_ReadsOnlyTheHeader_NoLengthOrChecksumCheck()
    {
        // [ORIGINAL] 0x5B661 liest nur 0xB0 Byte; Datenteil und Prüfsummen bleiben ungeprüft
        Assert.NotNull(OriginalSaveHeader.TryRead(Header(1993, 1, 1, (0, 0, "X", "Y"))));
    }

    [Fact]
    public void TryRead_LimitsManagersToFour()
    {
        Assert.Empty(OriginalSaveHeader.TryRead(Header(1993, 0, 1))!.Managers);
        Assert.Equal(4, OriginalSaveHeader.TryRead(Header(1993, 9, 1))!.Managers.Count);
    }

    [Fact]
    public void TryRead_WrongMagicOrTooShort_ReturnsNull()
    {
        var wrong = Header(1993, 1, 1);
        wrong[0] = (byte)'X';
        Assert.Null(OriginalSaveHeader.TryRead(wrong));
        Assert.Null(OriginalSaveHeader.TryRead(new byte[OriginalSaveHeader.Length - 1]));
        // #716 R14G3-03: gültig beginnender Kopf, ein Byte zu kurz; Gegenprobe Mindestlänge Length → Length - 1 → rot
        Assert.Null(OriginalSaveHeader.TryRead(Header(1993, 0, 1).AsSpan(0, OriginalSaveHeader.Length - 1)));
        var noTerminator = Header(1993, 1, 1);
        noTerminator[OriginalSaveGame.Magic.Length] = (byte)'1';
        Assert.Null(OriginalSaveHeader.TryRead(noTerminator));
    }
}
