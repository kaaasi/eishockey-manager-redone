using EishockeyManager.Tests.Support;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.ManagerArea;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur menue-befunde (Runde 7): Nachrichtenleiste nur mit Nachricht (#170) und Handbuchfrage des Originals (#185,
/// bewusst nicht übernommen, <c>docs/portierung.md</c>, <c>docs/reverse-engineering.md</c> „Kopierschutz“).
/// </summary>
public class MenueBefundeTests
{
    private static bool BarAreaEqualsBackground(ScreenDriver d)
    {
        var background = d.Host.Assets.Image(MainMenuScreen.Background);
        for (var y = NewsBar.Area.Y; y < NewsBar.Area.Bottom; y++)
        for (var x = NewsBar.Area.X; x < NewsBar.Area.Right; x++)
            if (d.Surface.Pixels[y * Surface.Width + x] != background.Pixels[y * Surface.Width + x])
                return false;
        return true;
    }

    /// <summary>
    /// PV-08 (#170): Hauptmenü ohne Nachricht und ohne große Kachel zeigt den Hintergrund, keine Leiste
    /// (Vorlagen eisbaeren_f01_00348, speedrun_t1_00121, eisbaeren_f16_00106, eisbaeren_f24_00040, _00424).
    /// [ORIGINAL] em.exe Datei 0x4527C–0x45291.
    /// </summary>
    [GameDataFact]
    public void MainMenu_WithoutMessage_ShowsNoNewsBar()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        Assert.False(d.Host.Game!.ActiveManager.HasNewMessage);
        d.Move(1, 1);
        Assert.True(BarAreaEqualsBackground(d), "ohne Nachricht bleibt der Hintergrund stehen");
    }

    /// <summary>PV-08: Mit neuer Nachricht erscheint die Leiste (folge01_00708: Verletzung außerhalb des Spiels).</summary>
    [GameDataFact]
    public void MainMenu_WithNewMessage_ShowsNewsBar_UntilMenuIsRebuilt()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        game.Notify(game.ActiveManager, "Verletzung außerhalb des Spiels:", "A.BRESAGK (Oberschenkelzerrung)");
        d.Host.Current!.Enter(); // Menü neu aufbauen (im Spiel kommt die Nachricht mit dem Tagesablauf; ui-meldungen Teil 4, 0x4527C)
        d.Move(1, 1);
        Assert.False(BarAreaEqualsBackground(d), "neue Nachricht zeigt die Leiste");

        // Klick auf die Leiste öffnet den Posteingang, Rückkehr baut das Menü ohne Leiste neu auf.
        d.Click(100, 210);
        Assert.IsType<InboxScreen>(d.Host.Current);
        d.Press(Key.Escape);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        d.Move(1, 1);
        Assert.True(BarAreaEqualsBackground(d), "gelesene Nachricht: Menü ohne Leiste");
    }

    /// <summary>Ohne sichtbare Leiste öffnet ein Klick an ihrer Stelle den Posteingang nicht.</summary>
    [GameDataFact]
    public void MainMenu_WithoutBar_ClickAtBarPositionDoesNothing()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.Click(100, 210);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
    }

    /// <summary>
    /// Handbuchfrage („PASSWORTABFRAGE“, em.exe Datei 0x4CD46; Aufrufe 0x34DB7, 0x4387B, 0x566AC): Das Remake übernimmt
    /// sie nicht (Entscheidung Nutzer, 30.10.2026). Das Original fragt beim Spielstart mit Wahrscheinlichkeit 1/3 und
    /// bleibt nach zwei falschen Antworten hängen; hier laufen Spielstart und die ersten Spieltage ohne Abfrage durch.
    /// </summary>
    [GameDataFact]
    public void NewGame_AndFirstMatchDays_RunWithoutManualQuestion()
    {
        for (var seed = 1; seed <= 12; seed++)
        {
            var d = FlowTests.CreateDriver(seed);
            d.StartNewGame();
            Assert.IsType<MainMenuScreen>(d.Host.Current);
            for (var day = 0; day < 3; day++)
            {
                d.PlayMatchDay();
                Assert.IsType<MainMenuScreen>(d.Host.Current);
            }
        }
    }

    /// <summary>Kein Bildschirm des Remakes ist eine Handbuch- oder Passwortabfrage (keine Spur soll sie mitportieren).</summary>
    [Fact]
    public void NoScreenImplementsTheManualQuestion()
    {
        var forbidden = new[] { "Passwort", "Handbuch", "Kopierschutz", "Copyprotection", "ManualQuestion" };
        var screens = typeof(Screen).Assembly.GetTypes()
            .Where(t => typeof(Screen).IsAssignableFrom(t) && forbidden.Any(f => t.Name.Contains(f, StringComparison.OrdinalIgnoreCase)))
            .Select(t => t.FullName)
            .ToList();
        Assert.Empty(screens);
    }
}
