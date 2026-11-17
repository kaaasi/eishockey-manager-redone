using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.UI.Screens.Team;

namespace EishockeyManager.UI.Tests;

/// <summary>Spur einzelpunkte, Teil 1 (Runde 22): Bedienung und Anzeige der Spielerdaten.</summary>
public class EinzelpunkteBedienungTests
{
    private static Player Skater(int skill1, int skill2) => new()
    {
        Name = "TEST", Role = PlayerRole.Forward, Condition = 3, Technique = 9, HiddenSkill1 = skill1, HiddenSkill2 = skill2,
    };

    /// <summary>
    /// A371, em.exe Datei 0x4B09–0x4B23 (Unterzahl: (+0x19 &amp; 0xF) · 94 / 15) und 0x4B4A–0x4B52 (Überzahl: (+0x19 &gt;&gt; 4) · 94 / 15),
    /// auf 1 … 94 begrenzt. Vorher: Technik bzw. (Kondition + Technik) / 2. Erwartete Werte von Hand gerechnet.
    /// </summary>
    [Theory]
    [InlineData(0, 15, 1, 94)]
    [InlineData(7, 7, 43, 43)]
    [InlineData(15, 1, 94, 6)]
    [InlineData(3, 12, 18, 75)]
    public void InfoPopup_UnderOverBars_UseHiddenSkills(int skill1, int skill2, int under, int over)
    {
        var lengths = PlayerInfoPopup.BarPixelLengths(Skater(skill1, skill2));
        Assert.Equal(under, lengths[0]);
        Assert.Equal(over, lengths[1]);
    }
}
