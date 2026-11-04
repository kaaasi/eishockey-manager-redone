using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur saison, Teil 2: Manager am Zug während des Saisonendes wie em.exe (DGROUP 0xA70E/0xA70D). Das Fenster „Saisonende“
/// (Datei 0x26F20) setzt den Zeiger je Manager vor dessen Seite (Datei 0x26FB0), die Verhandlungen je Manager (Datei 0x27B73),
/// danach beginnt die Zugfolge wieder bei Manager 0 (Datei 0x564E1). Fundstellen: GameState.SeasonEndTurn.cs.
/// </summary>
public class SaisonendeManagerZugTests
{
    private static GameState Create(int seed, params int[] clubs)
    {
        var setup = new GameSetup { Duration = GameDuration.Unbegrenzt };
        for (var i = 0; i < clubs.Length; i++)
            setup.Managers.Add(new ManagerSetup { Name = $"M{i}", Head = i, ClubIndex = clubs[i] });
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    private static void PlayToSeasonEnd(GameState game)
    {
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 5000, "Saisonende nicht erreicht");
            foreach (var m in game.Managers)
            {
                m.Money = 10_000_000;
                TestSeason.KeepSquad(game, m);
            }
            game.Continue();
        }
        Assert.Equal(SeasonPhase.SeasonOver, game.Phase);
    }

    [Fact]
    public void SeasonEndPages_EachManagerOnTurnForOwnPage_ThenTurnOrderStartsAtFirst()
    {
        var game = Create(seed: 4, 5, 30, 20);
        PlayToSeasonEnd(game);
        var record = game.History[^1];
        Assert.Equal([0, 1, 2], record.Managers.Select(m => m.Manager));

        // 0x26FB0: [0xA70E] = [0xA70D] = Manager der Seite, vor dem Fenster.
        for (var page = 0; page < record.Managers.Count; page++)
        {
            game.ShowSeasonEndManager(page);
            Assert.Equal(record.Managers[page].Manager, game.CurrentManager);
        }
        // 0x27B73: Verhandlungen je Manager.
        game.SwitchToRenewalManager(1);
        Assert.Equal(1, game.CurrentManager);
        // 0x564E1: danach wieder Manager 0.
        game.ReturnToFirstActiveManager();
        Assert.Equal(0, game.CurrentManager);
    }

    [Fact]
    public void SeasonEndPages_DismissedManagerHasNoPage_FirstActiveManagerOnTurnAfterwards()
    {
        // [APPROX] Entlassung (SeasonDismissal.cs): Ein entlassener Manager hat keine Seite und ist danach nicht am Zug.
        var game = Create(seed: 4, 5, 30);
        game.Managers[0].Dismissed = true;
        PlayToSeasonEnd(game);
        var record = game.History[^1];
        Assert.Equal([1], record.Managers.Select(m => m.Manager));

        game.ShowSeasonEndManager(0);
        Assert.Equal(1, game.CurrentManager);
        game.ShowSeasonEndManager(3); // begrenzt auf die letzte Seite
        Assert.Equal(1, game.CurrentManager);
        game.ReturnToFirstActiveManager();
        Assert.Equal(1, game.CurrentManager);
    }
}
