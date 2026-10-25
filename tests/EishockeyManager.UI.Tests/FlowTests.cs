using EishockeyManager.Formats;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>Prüft die Anmeldung der Bildschirme (läuft ohne Originaldateien).</summary>
public class MenuEntryTests
{
    [Fact]
    public void Registry_IsValid() => Assert.NotEmpty(MenuEntries.All); // wirft bei unbekannten oder doppelten Kacheln

    [Fact]
    public void TileNames_AreUnique()
    {
        var names = MenuTiles.Small.SelectMany(t => t).Where(t => !t.IsEmpty).Select(t => t.Name).ToList();
        Assert.Equal(names.Count, names.Distinct().Count());
    }
}

/// <summary>
/// Spielt Abläufe mit den echten Originaldateien durch. Ohne Ordner "gamedata" (z. B. auf GitHub)
/// werden die Tests übersprungen – die Originaldateien liegen bewusst nicht im Repository.
/// </summary>
public class FlowTests
{
    internal static ScreenDriver? CreateDriver()
    {
        try
        {
            return ScreenDriver.Create(GameData.Locate());
        }
        catch (DirectoryNotFoundException)
        {
            return null;
        }
    }

    [Fact]
    public void FullSeason_FromTitleToLastMatchDay()
    {
        var d = CreateDriver();
        if (d is null)
            return; // keine Originaldateien vorhanden

        d.StartNewGame();
        Assert.NotNull(d.Host.Game);
        while (!d.Host.Game!.RegularSeasonOver)
            d.PlayMatchDay();
        Assert.Equal(44, d.Host.Game.PlayedMatchDays);
    }

    [Fact]
    public void EveryMenuTile_OpensAndDraws()
    {
        var d = CreateDriver();
        if (d is null)
            return;

        d.StartNewGame();
        d.PlayMatchDay();
        foreach (var name in MenuTiles.AllNames)
        {
            d.OpenTile(name);
            Assert.IsNotType<MainMenuScreen>(d.Host.Current);
            d.Frame();
            d.Press(Key.Escape);
            if (d.Host.Current is not MainMenuScreen)
                d.RightClick(1, 1);
            Assert.IsType<MainMenuScreen>(d.Host.Current);
        }
    }
}
