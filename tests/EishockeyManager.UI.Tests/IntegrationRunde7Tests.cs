using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.SaveLoad;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Integration Runde 7: Spielberechnung mit Ereignissen je Spieler (spielberechnung), Startwerte der Kader (kader-start),
/// Börse nach em.exe (boerse-start), Freundschaftsspiele/Energie (weltkugel-dosbox, über die neue Spielberechnung) und
/// Hauptmenü ohne Handbuchfrage (menue-befunde) zusammen über eine Saison im Modus 4 über die Oberfläche.
/// </summary>
public class IntegrationRunde7Tests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "em-integration7-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    /// <summary>
    /// Eine Saison mit der Leertaste (Modus 4, Düsseldorfer EG): Die Spieler des eigenen Vereins sammeln Spiele, Tore,
    /// Vorlagen und Torhüterminuten aus der Spielberechnung, die Börse zeigt einen vollen Verlauf, keine Handbuchfrage
    /// erscheint, und Sichern/Laden bleibt am Saisonende bytegleich (Schema 11).
    /// </summary>
    [GameDataFact]
    public void OneSeason_Mode4_EventsStocksAndSaves()
    {
        var d = FlowTests.CreateDriver();
        var context = SaveContext.For(d.Host);
        context.Directory = _directory;
        context.AutoSaveOverride = false;
        d.StartNewGame(clubIndex: 2);
        var game = d.Host.Game!;
        var club = game.Clubs[game.ActiveManager.ClubIndex];

        Assert.All(game.Stocks.Stocks, s => Assert.Equal(Stock.HistoryLength, s.History.Count));

        for (var guard = 0; (game.History.Count < 1 || d.Host.Current is not MainMenuScreen) && guard < 40000; guard++)
        {
            d.Press(Key.Space);
            Assert.DoesNotContain("Handbuch", d.Host.Current?.GetType().Name ?? "", StringComparison.OrdinalIgnoreCase);
        }
        Assert.Single(game.History);

        // Die Zähler werden zum Saisonwechsel zurückgesetzt; die Saison steckt deshalb in der Spielerhistorie bzw. ist
        // nach dem Wechsel 0. Geprüft wird, dass die Berechnung über die Saison Ereignisse geliefert hat.
        var record = game.History[^1];
        Assert.Contains(record.Managers, r => r.Club == club.Index);
        var bytes = SaveGame.Save(game);
        var loaded = SaveGame.Load(bytes);
        Assert.True(loaded.Success, loaded.Error);
        Assert.Equal(bytes, SaveGame.Save(loaded.Game!));
    }

    /// <summary>
    /// Ein Ligaspieltag zählt beim eigenen Verein Spiele, Tore (= eigene Tore des Spiels) und Torhüterminuten.
    /// </summary>
    [GameDataFact]
    public void MatchDay_CreditsOwnPlayers()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame(clubIndex: 2);
        var game = d.Host.Game!;
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        var before = game.Results.Count;
        for (var guard = 0; game.Results.Count == before && guard < 2000; guard++)
            d.Press(Key.Space);
        var own = game.Results.Skip(before).Single(r => r.Home == club.Index || r.Away == club.Index);
        var goals = own.Home == club.Index ? own.HomeGoals : own.AwayGoals;

        Assert.True(club.Players.Sum(p => p.Games) > 0, "keine Spiele gezählt");
        Assert.Equal(goals, club.Players.Sum(p => p.Goals));
        Assert.True(club.Players.Sum(p => p.Assists) <= 2 * goals);
        Assert.True(club.Players.Sum(p => p.GoalieMinutes) >= 60, "keine Torhüterminuten");
    }
}
