using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

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
        Assert.Equal(2, club.Players.Count(p => p.Role == PlayerRole.Goalkeeper));
        Assert.Equal(10, club.Players.Count(p => p.Role == PlayerRole.Defender));
        Assert.Equal(13, club.Players.Count(p => p.Role == PlayerRole.Forward));
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

    private static GameState Reload(GameState game)
    {
        var loaded = SaveGame.Load(SaveGame.Save(game));
        Assert.True(loaded.Success, loaded.Error);
        return loaded.Game!;
    }

    /// <summary>
    /// Review P1-1 (2026-11-14): neues Spiel → sichern → laden → Manager hinzufügen → Spieltage → sichern/laden bytegleich.
    /// Der Spielstandleser legte die Managerliste als Array an; <see cref="GameState.AddManager"/> warf danach und hatte
    /// <c>Setup.Managers</c> schon verlängert.
    /// </summary>
    [Fact]
    public void AddManager_AfterLoad_ThenMatchDays_SaveLoadIsByteIdentical()
    {
        var game = Reload(MidSeason());

        var added = game.AddManager(Newcomer());
        for (var i = 0; i < 6; i++)
            game.Continue();

        Assert.Equal(2, game.Managers.Count);
        Assert.Equal(game.Managers.Count, game.Setup.Managers.Count);
        Assert.Same(added, game.Managers[1]);
        var saved = SaveGame.Save(game);
        var loaded = Reload(game);
        Assert.Equal(saved, SaveGame.Save(loaded));
        Assert.Equal("Neu", loaded.Managers[1].Name);
        // Auch das geladene Spiel nimmt weitere Manager an (bis vier).
        loaded.AddManager(new ManagerSetup { Name = "Drei", Head = 5, ClubIndex = 8 });
        Assert.Equal(3, loaded.Managers.Count);
        Assert.Equal(3, loaded.Setup.Managers.Count);
    }

    /// <summary>Wie oben mit Originaldaten (Probe des Reviews: 1 Manager, Verein 2, Seed 5; Neuer Verein 7).</summary>
    [GameDataFact]
    public void AddManager_AfterLoad_OriginalData_SaveLoadIsByteIdentical()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", Head = 0, ClubIndex = 2 });
        var game = Reload(NewGame.Create(ClubDatabase.Load(TestGameData.Load()), setup, 5));

        game.AddManager(new ManagerSetup { Name = "N", Head = 3, ClubIndex = 7 });
        for (var i = 0; i < 3; i++)
            TestSeason.PlayMatchDay(game);

        Assert.Equal(2, game.Managers.Count);
        Assert.Equal(2, game.Setup.Managers.Count);
        Assert.NotEmpty(game.Clubs[7].Players);
        var saved = SaveGame.Save(game);
        Assert.Equal(saved, SaveGame.Save(Reload(game)));
    }

    /// <summary>
    /// Review P1-1: Abgelehntes Hinzufügen ändert nichts (alle Prüfungen vor der ersten Änderung); <c>Setup.Managers</c> und
    /// <c>Managers</c> bleiben gleich lang, auch im geladenen Spiel.
    /// </summary>
    [Fact]
    public void AddManager_Rejected_LeavesGameUnchanged()
    {
        var game = Reload(MidSeason());
        var before = SaveGame.Save(game);

        Assert.Throws<ArgumentException>(() => game.AddManager(Newcomer(game.Managers[0].ClubIndex)));
        Assert.Throws<ArgumentException>(() => game.AddManager(new ManagerSetup { Name = "", ClubIndex = 8 }));
        Assert.Throws<ArgumentException>(() => game.AddManager(Newcomer(NewGame.SelectableClubs)));

        Assert.Single(game.Managers);
        Assert.Single(game.Setup.Managers);
        Assert.Equal(before, SaveGame.Save(game));
    }

    /// <summary>
    /// Review P1-1: Mehrere Zeilen der Spielvorbereitung („Los jetzt!“) werden erst alle geprüft, dann alle angelegt;
    /// scheitert eine, bleibt das Spiel unverändert.
    /// </summary>
    [Fact]
    public void AddManagers_ChecksAllRowsBeforeAddingAny()
    {
        var game = Reload(MidSeason());
        var before = SaveGame.Save(game);

        Assert.Throws<ArgumentException>(() => game.AddManagers([Newcomer(7), new ManagerSetup { Name = "B", ClubIndex = 7 }]));
        Assert.Throws<InvalidOperationException>(() => game.AddManagers(
        [
            Newcomer(7), new ManagerSetup { Name = "B", ClubIndex = 8 }, new ManagerSetup { Name = "C", ClubIndex = 9 },
            new ManagerSetup { Name = "D", ClubIndex = 10 },
        ]));
        Assert.Equal(before, SaveGame.Save(game));

        game.AddManagers([Newcomer(7), new ManagerSetup { Name = "B", ClubIndex = 8 }]);
        Assert.Equal(3, game.Managers.Count);
        Assert.Equal(3, game.Setup.Managers.Count);
    }

    /// <summary>
    /// Review P3-1: Werbeangebote und Tendenz des Neuen ziehen aus <see cref="GameState.FinanceRandom"/> und
    /// <see cref="GameState.TrendRandom"/> ([APPROX] in <see cref="GameState.AddManager"/>); gleiche Spiele bleiben nach dem
    /// Hinzufügen gleich, auch über weitere Tage.
    /// </summary>
    [Fact]
    public void AddManager_IsDeterministic_ForEqualGames()
    {
        var a = MidSeason();
        var b = Reload(MidSeason());

        a.AddManager(Newcomer());
        b.AddManager(Newcomer());
        for (var i = 0; i < 4; i++)
        {
            a.Continue();
            b.Continue();
        }

        Assert.Equal(SaveGame.Save(a), SaveGame.Save(b));
        Assert.NotEmpty(a.Finance(a.Managers[1]).Sponsoring.Offers);
    }

    /// <summary>
    /// Mehrsaisonprobe mit Laden (Review P3-2), Originaldaten, ohne Geldfixierung: Manager 0 passiv, der neue kommt nach dem
    /// Laden an Spieltag 3 hinzu und hält seinen Kader. Nach jedem der vier Saisonwechsel sichern und mit dem geladenen Spiel
    /// weiter; Sichern/Laden bleibt bytegleich, keiner wird entlassen, das Geld bleibt ganzzahlig im Buch erklärt.
    /// </summary>
    [GameDataFact]
    public void AddManager_AfterLoad_MultiSeason_OriginalData_WithoutMoneyFixing()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", Head = 0, ClubIndex = 2 });
        var game = NewGame.Create(ClubDatabase.Load(TestGameData.Load()), setup, 5);
        for (var i = 0; i < 3; i++)
            TestSeason.PlayMatchDay(game);
        game = Reload(game);
        game.AddManager(new ManagerSetup { Name = "N", Head = 3, ClubIndex = 7 });
        var firstSeason = game.Season;

        for (var change = 0; change < 4; change++)
        {
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 5000, "Saisonende nicht erreicht");
                TestSeason.KeepSquad(game, game.Managers[1], extendExpired: true);
                game.Continue();
            }
            TestSeason.KeepSquad(game, game.Managers[0]);
            TestSeason.KeepSquad(game, game.Managers[1], extendExpired: true);
            game.StartNextSeason();
            var saved = SaveGame.Save(game);
            game = Reload(game);
            Assert.Equal(saved, SaveGame.Save(game));
        }

        Assert.Equal(firstSeason + 4, game.Season);
        Assert.Equal(2, game.Managers.Count);
        Assert.Equal(2, game.Setup.Managers.Count);
        Assert.All(game.Managers, m => Assert.False(m.Dismissed));
        var club = game.Clubs[game.Managers[1].ClubIndex];
        Assert.InRange(club.Players.Count, 18, 29);
        Assert.All(club.Players, p => Assert.Empty(PlayerFactory.Validate(club, p)));
        Assert.Equal(2, game.History[^1].Managers.Count);
    }
}
