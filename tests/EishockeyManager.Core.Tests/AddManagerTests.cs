using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur nutzer-befunde, Teil 3 (#841): weitere Manager im laufenden Spiel (Kachel „Neuer“ → Spielvorbereitung im
/// Hinzufügen-Modus, em.exe 0x28165 mit DGROUP 0x386 &gt; 0).
/// </summary>
public class AddManagerTests
{
    private static GameState MidSeason(int matchDays = 3)
    {
        var game = TestGames.Create();
        for (var i = 0; i < matchDays; i++)
            TestSeason.PlayMatchDay(game);
        return game;
    }

    private static ManagerSetup Newcomer(int? club = 7) => new() { Name = "Neu", Head = 4, ClubIndex = club };

    [Fact]
    public void AddManager_MidSeason_SeatsPoolSquadWithStartValues()
    {
        var game = MidSeason();
        var level = game.Setup.Managers[0].Level;

        var manager = game.AddManager(Newcomer());

        Assert.Equal(2, game.Managers.Count);
        Assert.Same(manager, game.Managers[1]);
        Assert.Equal(7, manager.ClubIndex);
        Assert.Equal(level, manager.Level);
        Assert.Equal(FinanceRules.StartMoney(level), manager.Money); // [ORIGINAL] Datei 0x29BD5
        var club = game.Clubs[7];
        // [ORIGINAL] Kadertabelle DGROUP 0x169 nach Start-Liga Bundesliga: 2/10/13
        Assert.Equal(2, club.Players.Count(p => p.Role == Formats.PlayerRole.Goalkeeper));
        Assert.Equal(10, club.Players.Count(p => p.Role == Formats.PlayerRole.Defender));
        Assert.Equal(13, club.Players.Count(p => p.Role == Formats.PlayerRole.Forward));
        var start = SquadSetup.StartBase(League.Bundesliga, StartStrength.Mittelfeld);
        var pool = game.TransferMarket.Pool;
        foreach (var p in club.Players)
        {
            var slot = Assert.Single(pool, s => ReferenceEquals(s.Player, p));
            Assert.Equal(PoolOwner.Taken, slot.Owner);
            Assert.Equal(7, slot.Club);
            Assert.InRange(slot.Index, 1, 179); // keine Nationalspieler (Datei 0x29962)
            Assert.InRange(p.Age, 19, 31);
            Assert.InRange(p.Technique, start - 2, start + 2);
            Assert.InRange(p.Condition, start - 2, start + 2);
            Assert.InRange(p.Form, 40, 60);
            Assert.InRange(p.Strain, 80, 120);
            Assert.Equal(1, p.ContractYears); // [ORIGINAL] 0x42CB1(i, 1, …) → +0x0B = 1
            Assert.True(p.MonthlySalary > 0);
            Assert.Empty(PlayerFactory.Validate(club, p));
        }
        Assert.Equal(Enumerable.Range(1, club.Players.Count), club.Players.Select(p => p.Number));
        Assert.Equal(2, game.Setup.Managers.Count);
        Assert.Equal(League.Bundesliga, game.Setup.Managers[1].StartLeague);
    }

    [Fact]
    public void AddManager_KeepsTurnAndGivesNewcomerTheSameDay()
    {
        var game = MidSeason();
        var day = game.Day;
        var played = game.PlayedMatchDays;

        game.AddManager(Newcomer());

        Assert.Equal(0, game.CurrentManager); // [ORIGINAL] Datei 0x4575B–0x45761: Manager am Zug bleibt
        Assert.False(game.IsLastTurnOfDay);
        game.Continue();
        Assert.Equal(1, game.CurrentManager);
        Assert.Equal(day, game.Day);
        Assert.Equal(played, game.PlayedMatchDays);
    }

    [Fact]
    public void AddManager_DoesNotConsumeMainRandom()
    {
        var game = MidSeason();
        var before = game.Random.Capture();

        game.AddManager(Newcomer());

        var after = game.Random.Capture();
        Assert.Equal(before.Next, after.Next);
        Assert.Equal(before.NextP, after.NextP);
        Assert.Equal(before.SeedArray, after.SeedArray);
    }

    [Fact]
    public void AddManager_WithoutClub_PicksFreeClubOfStartLeague()
    {
        var game = MidSeason();

        var manager = game.AddManager(Newcomer(club: null));

        Assert.NotEqual(game.Managers[0].ClubIndex, manager.ClubIndex);
        Assert.Equal(League.Bundesliga, game.Clubs[manager.ClubIndex].League);
    }

    [Fact]
    public void AddManager_Rejects_TakenClub_EmptyName_AndFifthManager()
    {
        var game = MidSeason(0);
        Assert.Throws<ArgumentException>(() => game.AddManager(Newcomer(game.Managers[0].ClubIndex)));
        Assert.Throws<ArgumentException>(() => game.AddManager(new ManagerSetup { Name = "", ClubIndex = 8 }));
        Assert.Single(game.Managers);

        game.AddManager(Newcomer(7));
        game.AddManager(new ManagerSetup { Name = "Drei", ClubIndex = 8 });
        game.AddManager(new ManagerSetup { Name = "Vier", ClubIndex = 9 });

        Assert.False(game.CanAddManager); // [ORIGINAL] Datei 0x45057: Sperre bei vier Managern
        Assert.Throws<InvalidOperationException>(() => game.AddManager(new ManagerSetup { Name = "Fünf", ClubIndex = 10 }));
        Assert.Equal(4, game.Managers.Count);
    }

    [Fact]
    public void AddManager_RemovesMarketSlotsOfTheClub()
    {
        var game = MidSeason();
        var club = game.TransferMarket.Listings.FirstOrDefault(l => l.Seller is null && !game.IsClubManaged(l.From.Index)
            && l.From.Index < NewGame.SelectableClubs)?.From;
        Assert.NotNull(club);

        game.AddManager(Newcomer(club.Index));

        Assert.DoesNotContain(game.TransferMarket.Listings, l => l.From == club);
    }

    [Fact]
    public void AddManager_SurvivesSaveAndLoad()
    {
        var game = MidSeason();
        game.AddManager(Newcomer());

        var loaded = SaveGame.Load(SaveGame.Save(game)).Game!;

        Assert.Equal(2, loaded.Managers.Count);
        Assert.Equal("Neu", loaded.Managers[1].Name);
        Assert.Equal(game.Clubs[7].Players.Select(p => p.Name), loaded.Clubs[7].Players.Select(p => p.Name));
        Assert.Equal(SaveGame.Save(game), SaveGame.Save(loaded));
    }

    /// <summary>
    /// Mehrsaisonprobe (AGENTS.md §5): Manager kommt in Saison 1 hinzu, dann vier Saisonwechsel; Manager 0 passiv,
    /// der neue aktiv (verlängert seine Verträge). Beide bleiben im Spiel, Kader und Pool bleiben stimmig.
    /// </summary>
    [Fact]
    public void AddManager_MultiSeason_FourSeasonChanges_PassiveAndActive()
    {
        var game = MidSeason(5);
        var added = game.AddManager(Newcomer());

        for (var change = 0; change < 4; change++)
        {
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 5000, "Saisonende nicht erreicht");
                foreach (var manager in game.Managers)
                    manager.Money = 10_000_000; // keine Entlassung
                TestSeason.KeepSquad(game, added, extendExpired: true);
                game.Continue();
            }
            Assert.Equal(SeasonPhase.SeasonOver, game.Phase);
            game.StartNextSeason();
        }

        Assert.Equal(GameState.FirstSeason + 4, game.Season);
        Assert.Equal(2, game.Managers.Count);
        Assert.All(game.Managers, m => Assert.False(m.Dismissed));
        var squad = game.Clubs[added.ClubIndex].Players;
        Assert.InRange(squad.Count, 18, 29);
        Assert.All(squad, p => Assert.Empty(PlayerFactory.Validate(game.Clubs[added.ClubIndex], p)));
        var pooled = game.TransferMarket.Pool.Select(s => s.Player).ToHashSet(ReferenceEqualityComparer.Instance);
        Assert.All(squad.Where(p => !p.OnLoan), p => Assert.Contains(p, pooled));
        Assert.Equal(2, game.History[^1].Managers.Count);
    }
}
