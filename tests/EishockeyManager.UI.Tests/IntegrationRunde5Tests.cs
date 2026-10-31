using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Screens.SaveLoad;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Integration Runde 5: Live-Auslosung des DEB-Pokals (Spur debpokal-auslosung), Zug des Bundestrainers im Modus 4
/// (Spur bundestrainer-modus4) und Taktik nach em.exe (Spur taktik-verein) in einer Saison über die Oberfläche, dazu
/// die Migrationskette 4 → 5 → 6 (beide Spuren hatten 4 → 5 eingereicht).
/// </summary>
public class IntegrationRunde5Tests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "em-integration5-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    private ScreenDriver Start()
    {
        var d = FlowTests.CreateDriver();
        var context = SaveContext.For(d.Host);
        context.Directory = _directory;
        context.AutoSaveOverride = false;
        d.StartNewGame(clubIndex: 2); // Modus 4, Düsseldorfer EG (wie Ablauf „bundestrainer-modus4“)
        return d;
    }

    /// <summary>
    /// Eine Saison im Modus 4 mit „weiter“ (Hauptmenü: Vereinslogo, sonst Leertaste): Die Live-Auslosung („Aber klar“) erscheint, der Zug des
    /// Bundestrainers kommt, der Saisonwechsel läuft durch. Im Zug sind Vereins- und Nationaltaktik mit den Startwerten
    /// nach em.exe belegt, das Öffnen der Taktik ist zustandsrein und Sichern/Laden im Zug bytegleich.
    /// </summary>
    [GameDataFact]
    public void Mode4Season_WithLiveDraw_CoachTurn_AndTactics()
    {
        var d = Start();
        var game = d.Host.Game!;
        var draws = 0;
        var checkedTurn = false;
        for (var guard = 0; game.History.Count < 1 && guard < 30000; guard++)
        {
            d.Advance();
            if (d.Host.Current is DebCupDrawLiveScreen)
                draws++;
            if (game.NationalTurn && d.Host.Current is MainMenuScreen)
            {
                if (!checkedTurn)
                {
                    CheckTurn(d);
                    checkedTurn = true;
                }
            }
        }
        for (var guard = 0; d.Host.Current is not MainMenuScreen && guard < 30000; guard++)
        {
            d.Advance();
            if (d.Host.Current is DebCupDrawLiveScreen)
                draws++;
        }

        Assert.Single(game.History);
        Assert.Equal(GameState.FirstSeason + 1, game.Season);
        Assert.True(draws > 0, "Live-Auslosung nie gezeigt.");
        Assert.True(checkedTurn, "Zug des Bundestrainers im Modus 4 nie erreicht.");
        Assert.False(game.NationalTurn);
        var bytes = SaveGame.Save(game);
        Assert.Equal(bytes, SaveGame.Save(SaveGame.Load(bytes).Game!));
    }

    private static void CheckTurn(ScreenDriver d)
    {
        var game = d.Host.Game!;
        var coach = game.NationalCoach!;
        // #192 (Spur nationalteam): Die Taktik des Nationalteams hat keinen Startwert – eigene Werte 0 (DOSBox nur-weltkugel/26).
        foreach (var s in Enum.GetValues<ScoreSituation>())
            Assert.Equal(0, coach.Tactics.Effort(0, s));

        var before = StateFingerprint.Of(game);
        d.OpenTile("Taktik");
        Assert.IsNotType<MainMenuScreen>(d.Host.Current);
        d.Press(Key.Escape);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        Assert.Equal(before, StateFingerprint.Of(game));

        var bytes = SaveGame.Save(game);
        var loaded = SaveGame.Load(bytes);
        Assert.True(loaded.Success, loaded.Error);
        Assert.True(loaded.Game!.NationalTurn);
        Assert.Equal(bytes, SaveGame.Save(loaded.Game));
    }
}
