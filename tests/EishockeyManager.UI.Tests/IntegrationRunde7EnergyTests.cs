using EishockeyManager.Core;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// R7-04 (#202): Der Energiewert der Freundschaftsspiele ist unabhängig von der Konstante des Produktcodes belegt.
/// [ORIGINAL] em.exe Datei 0x3BB51: <c>mov byte [bx+0x5DD6], 0x46</c> (Kaderfeld 0x13 des Nationalkaders = 70).
/// </summary>
public class IntegrationRunde7EnergyTests
{
    [GameDataFact]
    public void FriendlyEnergy_IsTheByteWrittenByEmExe()
    {
        var exe = File.ReadAllBytes(Path.Combine(TestGameData.Load().RootDirectory, "em.exe"));
        Assert.Equal(new byte[] { 0xC6, 0x87, 0xD6, 0x5D, 0x46 }, exe[0x3BB51..0x3BB56]);
        Assert.Equal(70, exe[0x3BB55]);
        Assert.Equal(NationalCoach.FriendlyEnergy, exe[0x3BB55]);
    }

    [Fact]
    public void FriendlyEnergy_IsSeventy() => Assert.Equal(70, NationalCoach.FriendlyEnergy);
}
