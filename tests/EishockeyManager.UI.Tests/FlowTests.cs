using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spielt Abläufe mit den echten Originaldateien durch. Ohne Ordner "gamedata" (z. B. auf GitHub)
/// werden die Tests übersprungen – die Originaldateien liegen bewusst nicht im Repository.
/// </summary>
public class FlowTests
{
    private static ScreenHost? CreateHost()
    {
        try
        {
            return new ScreenHost(new Assets(GameData.Locate())) { Seed = 1993 };
        }
        catch (DirectoryNotFoundException)
        {
            return null;
        }
    }

    private sealed class Driver(ScreenHost host)
    {
        private readonly Surface _surface = new();
        private readonly InputState _input = new();

        public void Frame(Action<InputState>? setup = null)
        {
            _input.Reset();
            setup?.Invoke(_input);
            host.Update(_input);
            host.Draw(_surface);
        }

        public void Click(int x, int y) => Frame(i => { i.MouseX = x; i.MouseY = y; i.LeftClick = true; });
        public void Type(string text) => Frame(i => i.Text = text);
        public void Press(Key key) => Frame(i => i.Keys.Add(key));
    }

    [Fact]
    public void FullSeason_FromTitleToLastMatchDay()
    {
        var host = CreateHost();
        if (host is null)
            return; // keine Originaldateien vorhanden

        var d = new Driver(host);
        host.Show(new TitleScreen(host));
        d.Frame();
        d.Click(160, 120);
        Assert.IsType<SetupScreen>(host.Current);

        d.Click(20, 60);       // neuer Manager → Kopfauswahl
        d.Click(200, 100);     // Kopf wählen → Namenseingabe
        d.Type("Test");
        d.Press(Key.Enter);
        d.Click(286, 212);     // Los jetzt!
        Assert.IsType<MainMenuScreen>(host.Current);
        Assert.NotNull(host.Game);

        while (!host.Game!.RegularSeasonOver)
        {
            d.Press(Key.Space);
            while (host.Current is not MainMenuScreen)
                d.Press(Key.Space);
        }
        Assert.Equal(44, host.Game.PlayedMatchDays);

        // Alle drei Tabellen öffnen und zeichnen
        foreach (var (x, _) in new[] { (27, 0), (80, 1), (133, 2) })
        {
            d.Click(270, 110);
            d.Click(x, 220);
            Assert.IsType<TableScreen>(host.Current);
            d.Click(160, 170);
            d.Press(Key.Escape);
        }
    }
}
