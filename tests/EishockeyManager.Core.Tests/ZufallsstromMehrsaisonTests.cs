using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur zufallsstrom, Mehrsaisonprobe (AGENTS.md §5; die Spur berührt Kader, Geld und Saisonwechsel): vier Saisonwechsel mit
/// zwei Managern, Manager 0 passiv, Manager 1 je nach Lauf passiv oder aktiv (verlängert Verträge). Ein Zwilling aus derselben
/// Saat wird alle 40 Schritte gespeichert und neu geladen. Weil Tendenz, Faktor je Zug, Verträge, Angebote, Pool, Talentsuche,
/// Stadionangebot und der Spielstart nur noch aus dem gemeinsamen Strom <see cref="GameState.Random"/> ziehen, der im Spielstand
/// steht, bleiben beide Läufe an jedem Saisonwechsel bytegleich – eine Quelle außerhalb des Spielstands oder ein Wurf beim Laden
/// würde sie trennen.
/// </summary>
public class ZufallsstromMehrsaisonTests
{
    private static GameState Create(int seed)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Passiv", Head = 0, ClubIndex = 2 });
        setup.Managers.Add(new ManagerSetup { Name = "Zweiter", Head = 1, ClubIndex = 14 });
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    private static void Step(GameState game, bool active)
    {
        foreach (var manager in game.Managers)
            manager.Money = 10_000_000; // keine Entlassung
        if (active)
            TestSeason.KeepSquad(game, game.Managers[1], extendExpired: true);
        game.Continue();
    }

    [Theory]
    [InlineData(false, 4)]
    [InlineData(true, 9)]
    public void FourSeasonChanges_TwinWithSaveAndLoad_StaysOnTheSameStream(bool active, int seed)
    {
        var game = Create(seed);
        var twin = Create(seed);
        Assert.Equal(SaveGame.Save(game), SaveGame.Save(twin));
        var steps = 0;
        for (var change = 0; change < 4; change++)
        {
            var before = game.Random.Capture();
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 5000, "Saisonende nicht erreicht");
                Step(game, active);
                Step(twin, active);
                if (++steps % 40 == 0)
                {
                    var loaded = SaveGame.Load(SaveGame.Save(twin));
                    Assert.True(loaded.Success, loaded.Error);
                    twin = loaded.Game!;
                }
            }
            Assert.NotEqual(before, game.Random.Capture());
            Assert.Equal(game.Random.Capture(), twin.Random.Capture());
            Assert.Equal(SaveGame.Save(game), SaveGame.Save(twin));
            foreach (var manager in game.Managers)
            {
                var squad = game.Clubs[manager.ClubIndex].Players;
                if (active && manager == game.Managers[1])
                    Assert.NotEmpty(squad); // der passive Kader schwindet mit den auslaufenden Startverträgen (R12K-03)
                // jeder Kaderplatz trägt Tendenz und Steuerbyte aus dem Strom (Zähler 1–13) und ein Gehalt
                Assert.All(squad, p => Assert.InRange(p.TrendControl & 0x0F, 1, 13));
                Assert.All(squad, p => Assert.True(p.MonthlySalary > 0, p.Name));
            }
            game.StartNextSeason();
            twin.StartNextSeason();
        }
        Assert.Equal(GameState.FirstSeason + 4, game.Season);
        Assert.Equal(SaveGame.Save(game), SaveGame.Save(twin));
    }
}
