using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur nationalteam, Runde 13, Teil 3: WM-Tabelle nach der DOSBox-Aufnahme `runde-12` (<c>docs/analyse/re-runde-13.md</c>,
/// A293, A294).
/// </summary>
public class NationalteamRunde13UiTests
{
    /// <summary>
    /// A294: Regressionstest (vorher rot: Linksklick außerhalb kehrte zurück). [ORIGINAL] Ein Linksklick außerhalb der Zeilen
    /// und des Knopfs bewirkt nichts (Datei 0x2FA2D–0x2FADC); zurück nur mit Rechtsklick.
    /// A293: [ORIGINAL] Gruppenknopf genau bei x 237–293, y 211–222 (Datei 0x2FA4A): (240 | 215) wechselt, (236 | 215) nicht.
    /// </summary>
    [GameDataFact]
    public void A293_A294_LeftClickOutsideStays_GroupButtonExact_RightClickReturns()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        d.OpenTile("Tabelle WM");
        d.Frame();
        var screen = Assert.IsType<WorldCupScreen>(d.Host.Current);

        d.Click(100, 30);
        Assert.Same(screen, d.Host.Current);
        d.Click(300, 30);
        Assert.Same(screen, d.Host.Current);

        d.Click(236, 215);
        Assert.Same(screen, d.Host.Current);
        Assert.Same(game.WorldCup!.Groups[0], screen.Group);
        d.Click(240, 215);
        Assert.Same(game.WorldCup.Groups[1], screen.Group);
        d.Click(293, 222);
        Assert.Same(game.WorldCup.Groups[0], screen.Group);

        d.RightClick(100, 30);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
    }

    /// <summary>
    /// A293: [ORIGINAL] Die Gruppentabelle hat keinen Knopf „Endrunde“ (0x2F5B6): Auch nach der Gruppenphase wirkt ein Klick
    /// links neben dem Gruppenknopf (vorher „Endrunde“ bei x = 173) nicht; die Endrundenspiele kommen aus „Paarungen → WM“.
    /// </summary>
    [GameDataFact]
    public void A293_NoKnockoutButton_KnockoutViaPairings()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        for (var guard = 0; game.WorldCup!.KnockoutPairings(0).Count == 0; guard++)
        {
            Assert.True(guard < 20000, "WM-Endrunde nicht erreicht");
            d.Advance();
        }
        d.Host.Show(new MainMenuScreen(d.Host));
        d.OpenTile("Tabelle WM");
        d.Frame();
        var screen = Assert.IsType<WorldCupScreen>(d.Host.Current);
        d.Click(173 + 5, WorldCupScreen.ButtonY + 5);
        Assert.Same(screen, d.Host.Current);
        d.RightClick(100, 30);

        d.OpenTile("WM");
        Assert.IsType<CupPairingsScreen>(d.Host.Current);
        d.Click(CupPairingsScreen.Buttons[1].X + 5, CupPairingsScreen.Buttons[1].Y + 5);
        Assert.IsType<WorldCupKnockoutScreen>(d.Host.Current);
    }
}
