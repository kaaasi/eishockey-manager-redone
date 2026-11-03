using EishockeyManager.Tests.Support;
using EishockeyManager.Formats;
using Xunit.Abstractions;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur <c>kader-transfer</c>, Teil 3 (WS-05, Review Runde 11): Der Rückfallweg von <see cref="TestSquadKeeper"/> (Spieler eines
/// Computervereins ohne Kauf umsetzen) verdeckte, ob zu Saisonbeginn Vertragslose auf der Transferliste stehen. Jetzt meldet
/// <see cref="TestSquadKeeper.KeepReport"/>, was passiert ist; die Proben unten machen es sichtbar und sichern ab, dass der
/// Rückfallweg nur bei leerem Markt greift.
/// </summary>
public class KaderTransferWs05Tests(ITestOutputHelper output)
{
    /// <summary>Leert die Transferliste von Vertragslosen (Testeingriff über das private Feld; die Liste ist nach außen nur lesbar).</summary>
    private static void RemoveFreeAgents(GameState game)
    {
        var field = typeof(TransferMarket).GetField("_listings", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        ((List<TransferListing>)field.GetValue(game.TransferMarket)!).RemoveAll(l => l.IsFreeAgent);
    }

    /// <summary>Ohne Vertragslose auf der Liste und mit kleinem Kader: Meldung „Markt leer“; ohne Rückfallweg bleibt der Kader klein.</summary>
    [Fact]
    public void KeepPlayable_EmptyMarket_ReportsMarketEmpty_AndNoFallbackMovesNobody()
    {
        var game = TestGames.Create();
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        RemoveFreeAgents(game);
        foreach (var p in club.Players.Skip(10).ToArray())
        {
            club.Lineup.Remove(p);
            club.Players.Remove(p);
        }
        var before = club.Players.Count;
        var report = TestSquadKeeper.KeepPlayable(game, manager, allowFallback: false);
        Assert.True(report.MarketEmpty);
        Assert.Equal(0, report.Moved);
        Assert.Equal(before, club.Players.Count);
    }

    /// <summary>Mit Rückfallweg wird der Kader aufgefüllt, aber als „umgesetzt“ gemeldet – nie als Kauf.</summary>
    [Fact]
    public void KeepPlayable_EmptyMarket_FallbackIsReportedAsMoved()
    {
        var game = TestGames.Create();
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        RemoveFreeAgents(game);
        foreach (var p in club.Players.Skip(10).ToArray())
        {
            club.Lineup.Remove(p);
            club.Players.Remove(p);
        }
        // Spender mit mehr als MinSquad + 2 Spielern sicherstellen (die synthetischen Kader haben genau 20).
        var others = game.Clubs.Where(c => c != club && c.League is not null && game.Managers.All(m => m.ClubIndex != c.Index)).ToList();
        foreach (var from in others.Skip(1).Take(2))
        foreach (var p in from.Players.Take(8).ToArray())
        {
            from.Lineup.Remove(p);
            from.Players.Remove(p);
            p.Number = PlayerFactory.FreeNumber(others[0], p);
            others[0].Players.Add(p);
        }
        var report = TestSquadKeeper.KeepPlayable(game, manager);
        Assert.True(report.MarketEmpty);
        Assert.Equal(0, report.Bought);
        Assert.True(report.Moved > 0);
        Assert.True(club.Players.Count >= TestSquadKeeper.MinSquad);
    }

    /// <summary>
    /// Mehrsaisonprobe (4 Saisonwechsel, passiv und aktiv): Der Rückfallweg greift nur, wenn der Markt wirklich leer war.
    /// Die Zahlen je Saison stehen in der Testausgabe (Befund im PR).
    /// </summary>
    [Theory]
    [InlineData(3, false)]
    [InlineData(3, true)]
    [InlineData(21, false)]
    [InlineData(21, true)]
    public void MultiSeason_FallbackOnlyWhenMarketEmpty(int seed, bool active) => RunSeasons(TestGames.Create(seed), seed, active, "synthetisch");

    /// <summary>Dieselbe Probe mit den Originaldaten – dort zeigt sich, ob zu Saisonbeginn Vertragslose verpflichtbar sind (Befund WS-05).</summary>
    [GameDataTheory]
    [InlineData(3, false)]
    [InlineData(3, true)]
    [InlineData(5, false)]
    [InlineData(5, true)]
    public void MultiSeason_OriginalData_FallbackOnlyWhenMarketEmpty(int seed, bool active)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Test", Head = 0, ClubIndex = 2 });
        RunSeasons(NewGame.Create(ClubDatabase.Load(TestGameData.Load()), setup, seed), seed, active, "Originaldaten");
    }

    private void RunSeasons(GameState game, int seed, bool active, string source)
    {
        var manager = game.ActiveManager;
        for (var change = 0; change < 4; change++)
        {
            int bought = 0, moved = 0, empty = 0;
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 1000, "Saisonende nicht erreicht");
                var report = TestSquadKeeper.KeepPlayable(game, manager);
                bought += report.Bought;
                moved += report.Moved;
                if (report.MarketEmpty)
                    empty++;
                if (report.Moved > 0)
                    Assert.True(report.MarketEmpty, "Rückfallweg genutzt, obwohl Vertragslose auf der Liste standen");
                if (manager.Money < 2_000_000)
                    manager.Money = 2_000_000;
                game.Continue();
            }
            output.WriteLine($"{source} seed={seed} active={active} Saison {change + 1}: gekauft={bought} umgesetzt={moved} Markt-leer-Tage={empty}");
            game.StartNextSeason();
        }
    }
}
