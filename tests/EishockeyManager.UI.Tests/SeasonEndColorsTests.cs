using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;

namespace EishockeyManager.UI.Tests;

/// <summary>ui-rest Teil 3 (R-K2): Farben 18/12/22 der Saisonende-Zeilen gegen die DOSBox-Bilder des Originals.</summary>
public class SeasonEndColorsTests
{
    /// <summary>
    /// [ORIGINAL] Datei 0x4CCB4: Zeichen in Palettenfarbe 18 (Absteiger), 12 (Aufsteiger), 22 (Meister). In den DOSBox-Bildern
    /// <c>file13_14_saisonende_uebersicht</c> und <c>file16_8_saisonende</c> (Referenz-Repo, <c>original/spielstaende/bilder</c>) kommen
    /// genau diese drei Farben außerhalb von Fenster und Text vor: (243 | 0 | 0), (81 | 97 | 16), (48 | 65 | 195).
    /// </summary>
    [GameDataFact]
    public void Palette18_12_22_MatchDosBoxPictures()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var guard = 0;
        while (d.Host.Current is not SeasonEndScreen && guard++ < 20000)
        {
            KeepSquadHelper.KeepSquads(d.Host.Game!);
            d.Advance();
        }
        Assert.IsType<SeasonEndScreen>(d.Host.Current);
        d.Frame();

        var rgb = d.Surface.Palette.Rgb;
        Assert.Equal(new byte[] { 243, 0, 0 }, rgb.Slice(18 * 3, 3).ToArray());
        Assert.Equal(new byte[] { 81, 97, 16 }, rgb.Slice(12 * 3, 3).ToArray());
        Assert.Equal(new byte[] { 48, 65, 195 }, rgb.Slice(22 * 3, 3).ToArray());
        // #620 (R13GN-03): auch die Indizes selbst müssen im Bild stehen (mit den alten 238/13/151 blieb der Test grün).
        // Nur die Spalten der Zeichen (vor den Vereinsnamen) der drei Ligen, beide Zeilenblöcke.
        var used = SeasonEndScreen.LeagueColumns
            .SelectMany(x => Enumerable.Range(x, SeasonEndScreen.NameOffset))
            .SelectMany(x => Enumerable.Range(SeasonEndScreen.TopRowY - 6, SeasonEndScreen.BottomRowY + 2 * SeasonEndScreen.RowPitch
                - SeasonEndScreen.TopRowY + 6).Select(y => d.Surface.Pixels[y * Surface.Width + x]))
            .ToHashSet();
        Assert.Contains((byte)18, used);
        Assert.Contains((byte)12, used);
        Assert.Contains((byte)22, used);
    }
}
