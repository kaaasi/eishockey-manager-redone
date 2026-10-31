using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.SaveLoad;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Integration Runde 6: Werbebewertung und Monatsletzter (Spur finanzen-werbung), Zuschauerstatistik (Spur
/// heimspiel-statistik), Statusbit 0x40 (Spur saisonende-status) und die vermessenen Bildschirme (Spuren pixel-verein,
/// pixel-wettbewerbe) zusammen über zwei Saisons im Modus 4 über die Oberfläche.
/// </summary>
public class IntegrationRunde6Tests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "em-integration6-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    /// <summary>
    /// Zwei Saisons mit „weiter“ (Vereinslogo bzw. Leertaste; Modus 4, Düsseldorfer EG, Bundesliga): Die Zuschauerstatistik füllt sich mit
    /// Ligaheimspielen und beginnt nach dem Saisonwechsel neu, die Werbebewertung bleibt in 0–100, das Statusbit 0x40
    /// folgt der Bundesliga-Abschlusstabelle, das Öffnen von Werbung und Finanzen ist zustandsrein, und Sichern/Laden
    /// bleibt an jedem Saisonende bytegleich (Schema 8).
    /// </summary>
    [GameDataFact]
    public void TwoSeasons_Mode4_StatisticsRatingStatusAndSaves()
    {
        var d = FlowTests.CreateDriver();
        var context = SaveContext.For(d.Host);
        context.Directory = _directory;
        context.AutoSaveOverride = false;
        d.StartNewGame(clubIndex: 2);
        var game = d.Host.Game!;
        var me = game.ActiveManager;
        var meIndex = game.CurrentManager;

        var maxGames = 0;
        var screensChecked = false;
        var seasonsChecked = 0;
        for (var guard = 0; seasonsChecked < 2 && guard < 60000; guard++)
        {
            d.Advance();
            if (d.Host.Current is not MainMenuScreen || game.NationalTurn)
                continue;

            var stats = game.Finance(me).HomeStatistics;
            maxGames = Math.Max(maxGames, stats.Games);
            Assert.InRange(game.Finance(me).SponsorRating, -1, 100);

            if (!screensChecked && stats.Games >= 3)
            {
                foreach (var tile in new[] { "Werbung", "Finanzen" })
                {
                    var before = StateFingerprint.Of(game);
                    d.OpenTile(tile);
                    Assert.IsNotType<MainMenuScreen>(d.Host.Current);
                    for (var esc = 0; d.Host.Current is not MainMenuScreen && esc < 5; esc++)
                        d.Press(Key.Escape);
                    Assert.IsType<MainMenuScreen>(d.Host.Current);
                    Assert.Equal(before, StateFingerprint.Of(game));
                }
                screensChecked = true;
            }

            if (game.History.Count > seasonsChecked)
            {
                seasonsChecked = game.History.Count;
                var record = game.History[^1];
                var result = Assert.Single(record.Managers, r => r.Manager == meIndex);
                Assert.Equal(record.IsTopFive(result.Club), game.HasTopFiveStatus(record, result));
                Assert.True(maxGames > 0, "Keine Ligaheimspiele in der Statistik.");
                Assert.True(game.Finance(me).HomeStatistics.Games < maxGames, "Statistik nach dem Saisonwechsel nicht neu begonnen.");
                maxGames = game.Finance(me).HomeStatistics.Games;

                var bytes = SaveGame.Save(game);
                var loaded = SaveGame.Load(bytes);
                Assert.True(loaded.Success, loaded.Error);
                Assert.Equal(bytes, SaveGame.Save(loaded.Game!));
            }
        }

        Assert.Equal(2, game.History.Count);
        Assert.Equal(2, seasonsChecked);
        Assert.True(screensChecked, "Werbung/Finanzen nie geprüft.");
    }
}
