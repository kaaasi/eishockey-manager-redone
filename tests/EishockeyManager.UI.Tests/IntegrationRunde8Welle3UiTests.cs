using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;

namespace EishockeyManager.UI.Tests;

/// <summary>Integration Runde 8, Welle 3 (Leitstelle): Anschlüsse aus den Spur-PRs in fremden Bildschirmen.</summary>
public class IntegrationRunde8Welle3UiTests
{
    /// <summary>
    /// #195 nach dem Saisonwechsel (Vorschlag aus PR #265): [ORIGINAL] Auch in der zweiten Saison läuft im Modus
    /// „nur Weltkugel“ zuerst die Tagesfunktion Datei 0x54C9F (Sprung zum WM-Termin); das erste Hauptmenü der neuen
    /// Saison steht am ersten WM-Tag, nicht am Tag 0.
    /// </summary>
    [GameDataFact]
    public void Mode1_AfterSeasonChange_FirstHaltIsFirstWorldCupDay()
    {
        var d = Nationalteam2Tests.StartWorldOnlyThroughSetup();
        var game = d.Host.Game!;
        var season = game.Season;
        var guard = 0;
        while (!(game.Season > season && d.Host.Current is MainMenuScreen) && guard++ < 20000)
            d.Advance();
        Assert.True(game.Season > season, "kein Saisonwechsel erreicht");
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        Assert.Equal(GameState.FirstWorldCupDay, game.CurrentDay);
    }
}
