using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur neuer-manager (#847, R18-E5 = A): Ein über „Neuer“ hinzukommender Manager tauscht seinen gewählten Verein wie im
/// Original in die Start-Liga des Spiels (em.exe Datei 0x29C3B–0x29CF7, Tausch 0x36169 mit Schalter 0x80). Belege:
/// <c>docs/reverse-engineering.md</c>, Abschnitt „Neuer Manager mitten in der Saison“.
/// </summary>
public class NeuerManagerTests
{
    private const int Chosen = 7;

    private static GameState MidSeason(int seed, int matchDays, League startLeague = League.Bundesliga, int ownClub = 2)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Test", Head = 0, ClubIndex = ownClub });
        setup.StartLeague = startLeague; // ein Wert für alle (DGROUP 0x11A), gelesen über Manager 0
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, seed);
        for (var i = 0; i < matchDays; i++)
            TestSeason.PlayMatchDay(game);
        return game;
    }

    private static ManagerSetup Newcomer(int club = Chosen) => new() { Name = "Neu", Head = 4, ClubIndex = club };

    /// <summary>Was im Original am Vereinsplatz bleibt (Vereinssatz außer Kennung, Tabellensatz, Serien), je Index.</summary>
    private static object PlaceState(GameState game, int index)
    {
        var club = game.Clubs[index];
        var row = club.League is { } league
            ? LeagueTable.Compute(game, league, TableView.Gesamt, game.PlayedMatchDays).Single(r => r.Club == club)
            : null;
        return string.Join("|",
            club.Index, club.League, club.Strength, club.TeamValues, club.StoredTableRank, club.LeaguePenaltyMinutes,
            string.Join(",", Enumerable.Range(0, GameState.RankSlotCount).Select(club.StoredRankSlot)),
            string.Join(",", club.SeriesCurrent),
            row is null ? "-" : $"{row.Games}:{row.PointsPlus}:{row.GoalsFor}:{row.GoalsAgainst}",
            string.Join(",", game.LeagueSlots(League.Bundesliga)), string.Join(",", game.LeagueSlots(League.ZweiteLiga)),
            game.Results.Count(r => r.Home == index || r.Away == index));
    }

    /// <summary>Erster Spielstand (Saat 1…, nach 3 Spieltagen), in dem der Zufallsplatz nicht der eigene ist.</summary>
    private static (GameState Game, int Seed) FirstSwappingGame(League startLeague = League.Bundesliga)
    {
        for (var seed = 1; seed < 40; seed++)
        {
            var probe = MidSeason(seed, 3, startLeague);
            if (probe.AddManager(Newcomer()).ClubIndex != Chosen)
                return (MidSeason(seed, 3, startLeague), seed);
        }
        throw new Xunit.Sdk.XunitException("Kein Platztausch in 40 Spielständen (Verein bleibt immer an seinem Platz).");
    }

    /// <summary>
    /// Regressionstest #847: Bisher blieb der gewählte Verein mitten in der Saison an seinem Platz (<c>[APPROX]</c>). Jetzt
    /// übernimmt er den gezogenen Platz der Start-Liga: Name, Logo, Spieler und Manager wandern, Liga, Datenbankstärke,
    /// Teamwerte, Tabellenstand, Platzverlauf, Serien, Ergebnisse und Satzfolge bleiben am Platz ([ORIGINAL] 0x36169, Schalter
    /// 0x80, Datei 0x3628F–0x364B4).
    /// </summary>
    [Fact]
    public void AddManager_BundesligaStart_SwapsIdentityIntoDrawnPlace_RecordStaysAtPlace()
    {
        var (game, _) = FirstSwappingGame();
        var names = game.Clubs.Select(c => c.Name).ToList();
        var objects = game.Clubs.ToList();
        var places = Enumerable.Range(0, game.Clubs.Count).Select(i => PlaceState(game, i)).ToList();
        var results = game.Results.ToList();
        var oldPlayers = game.Clubs.Select(c => c.Players.ToList()).ToList();

        var manager = game.AddManager(Newcomer());

        var r = manager.ClubIndex;
        Assert.NotEqual(Chosen, r);
        Assert.Equal(League.Bundesliga, game.Clubs[r].League);
        Assert.Equal(names[Chosen], game.Clubs[r].Name);
        Assert.Equal(names[r], game.Clubs[Chosen].Name);
        Assert.Same(objects[Chosen], game.Clubs[r]);
        Assert.Same(objects[r], game.Clubs[Chosen]);
        Assert.Equal(r, game.Clubs[r].Index);
        Assert.Equal(Chosen, game.Clubs[Chosen].Index);
        Assert.Equal(r, game.Setup.Managers[1].ClubIndex);
        for (var i = 0; i < game.Clubs.Count; i++)
            Assert.Equal(places[i], PlaceState(game, i));
        Assert.Equal(results, game.Results);
        // Der Verdrängte behält seine Spieler, der Manager hat den Startkader aus dem Pool.
        Assert.Equal(oldPlayers[r], game.Clubs[Chosen].Players);
        Assert.Empty(game.Clubs[r].Players.Intersect(oldPlayers[Chosen]));
        var pool = game.TransferMarket.Pool;
        Assert.All(game.Clubs[r].Players, p => Assert.Equal(r, Assert.Single(pool, s => ReferenceEquals(s.Player, p)).Club));
        Assert.DoesNotContain(pool, s => s.Club == Chosen && game.Clubs[r].Players.Contains(s.Player));
        Assert.True(game.IsClubManaged(r));
        Assert.False(game.IsClubManaged(Chosen));
    }

    /// <summary>
    /// Zufallsplatz: nie der Verein eines vorhandenen Managers, nie Platz 0 (erster Bundesliga-Satz) und nie ein deutscher
    /// Europapokal-Teilnehmer ([ORIGINAL] Datei 0x29C9B–0x29CD5, 0x45CD0(r, 1) mitten in der Saison).
    /// </summary>
    [Fact]
    public void AddManager_DrawnPlace_ExcludesManagersPlaceZeroAndEuropeanCup()
    {
        var hits = new HashSet<int>();
        for (var seed = 1; seed <= 30; seed++)
        {
            var game = MidSeason(seed, 2);
            var excluded = new HashSet<int> { game.Managers[0].ClubIndex, game.LeagueSlots(League.Bundesliga)[0] };
            excluded.UnionWith(game.EuropeanCup!.Draw);
            var r = game.AddManager(Newcomer()).ClubIndex;
            Assert.DoesNotContain(r, excluded.Where(c => c != Chosen));
            Assert.Equal(League.Bundesliga, game.Clubs[r].League);
            hits.Add(r);
        }
        Assert.True(hits.Count >= 4, $"Zu wenige verschiedene Plätze: {string.Join(",", hits)}");
    }

    /// <summary>Start-Liga 2. Liga / Oberliga, Verein schon in ihr: kein Tausch ([ORIGINAL] Datei 0x29C3B–0x29C75).</summary>
    [Theory]
    [InlineData(League.ZweiteLiga, 15)]
    [InlineData(League.Oberliga, 30)]
    public void AddManager_ClubAlreadyInStartLeague_StaysAtItsPlace(League startLeague, int club)
    {
        var game = MidSeason(5, 3, startLeague, ownClub: club + 1);
        var objects = game.Clubs.ToList();

        var manager = game.AddManager(Newcomer(club));

        Assert.Equal(club, manager.ClubIndex);
        Assert.Equal(objects, game.Clubs);
        Assert.Equal(startLeague, game.Clubs[club].League);
    }

    /// <summary>Verein außerhalb der Start-Liga (Bundesliga, 2. Liga, ohne Liga) wandert in jede Start-Liga.</summary>
    [Theory]
    [InlineData(League.Bundesliga, 20)]
    [InlineData(League.Bundesliga, 40)]
    [InlineData(League.ZweiteLiga, 3)]
    [InlineData(League.ZweiteLiga, 30)]
    [InlineData(League.Oberliga, 5)]
    [InlineData(League.Oberliga, 44)]
    public void AddManager_ClubOutsideStartLeague_MovesIntoStartLeague(League startLeague, int club)
    {
        var game = MidSeason(5, 3, startLeague, ownClub: startLeague == League.Bundesliga ? 2 : 12 * (int)startLeague + 1);
        var name = game.Clubs[club].Name;
        var leagueBefore = game.Clubs[club].League;

        var manager = game.AddManager(Newcomer(club));

        Assert.NotEqual(club, manager.ClubIndex);
        Assert.Equal(startLeague, game.Clubs[manager.ClubIndex].League);
        Assert.Equal(name, game.Clubs[manager.ClubIndex].Name);
        Assert.Equal(leagueBefore, game.Clubs[club].League); // der Verdrängte übernimmt den Platz (auch „ohne Liga“)
        Assert.NotEqual(name, game.Clubs[club].Name);
    }

    /// <summary>
    /// Mehrere neue Zeilen: jeder Manager behält seinen gewählten Verein (Kennung), alle stehen in der Start-Liga, keiner auf dem
    /// Platz eines anderen; die Kennung eines getroffenen, noch nicht angelegten Vereins wandert mit (Datei 0x361E4).
    /// </summary>
    [Fact]
    public void AddManagers_SeveralRows_EachKeepsChosenIdentityInStartLeague()
    {
        for (var seed = 1; seed <= 12; seed++)
        {
            var game = MidSeason(seed, 2);
            var names = game.Clubs.Select(c => c.Name).ToList();
            int[] chosen = [Chosen, 20, 4];

            var added = game.AddManagers(chosen.Select(c => Newcomer(c)).ToList());

            for (var i = 0; i < chosen.Length; i++)
            {
                Assert.Equal(names[chosen[i]], game.Clubs[added[i].ClubIndex].Name);
                Assert.Equal(League.Bundesliga, game.Clubs[added[i].ClubIndex].League);
                Assert.Equal(added[i].ClubIndex, game.Setup.Managers[i + 1].ClubIndex);
                Assert.Equal(25, game.Clubs[added[i].ClubIndex].Players.Count);
            }
            Assert.Equal(4, game.Managers.Select(m => m.ClubIndex).Distinct().Count());
            Assert.Equal(names[2], game.Clubs[game.Managers[0].ClubIndex].Name);
        }
    }

    /// <summary>Nach dem Laden ([#842 P1-1] atomar): Tausch, Spieltage, Sichern/Laden bytegleich, Kennung bleibt.</summary>
    [Fact]
    public void AddManager_AfterLoad_SwapThenMatchDays_SaveLoadIsByteIdentical()
    {
        var (game, _) = FirstSwappingGame();
        var loaded = Reload(game);
        var name = loaded.Clubs[Chosen].Name;

        var manager = loaded.AddManager(Newcomer());
        for (var i = 0; i < 4; i++)
            TestSeason.PlayMatchDay(loaded);

        Assert.NotEqual(Chosen, manager.ClubIndex);
        var again = Reload(loaded);
        Assert.Equal(SaveGame.Save(loaded), SaveGame.Save(again));
        Assert.Equal(name, again.Clubs[again.Managers[1].ClubIndex].Name);
        Assert.Equal(League.Bundesliga, again.Clubs[again.Managers[1].ClubIndex].League);
        Assert.Equal(again.Managers[1].ClubIndex, again.Clubs[again.Managers[1].ClubIndex].Index);
    }

    /// <summary>Gleicher Spielstand, gleicher Platz (eigene Zufallsquelle, die Hauptquelle bleibt unberührt).</summary>
    [Fact]
    public void AddManager_Swap_IsDeterministic_AndKeepsMainRandom()
    {
        var (a, seed) = FirstSwappingGame();
        var b = MidSeason(seed, 3);
        var c = MidSeason(seed, 3);

        Assert.Equal(a.AddManager(Newcomer()).ClubIndex, b.AddManager(Newcomer()).ClubIndex);
        Assert.Equal(SaveGame.Save(a), SaveGame.Save(b));
        Assert.Equal(c.Random.Next(), a.Random.Next());
    }

    /// <summary>Originaldaten: Verein der 2. Liga (SV BAYREUTH, 23) kommt in Saison 1 dazu und spielt in der Bundesliga.</summary>
    [GameDataFact]
    public void AddManager_OriginalData_SecondDivisionClubMovesIntoBundesliga()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", Head = 0, ClubIndex = 2 });
        var game = Reload(NewGame.Create(ClubDatabase.Load(TestGameData.Load()), setup, 5));
        for (var i = 0; i < 3; i++)
            TestSeason.PlayMatchDay(game);
        var name = game.Clubs[23].Name;
        Assert.Equal(League.ZweiteLiga, game.Clubs[23].League);

        var manager = game.AddManager(new ManagerSetup { Name = "N", Head = 3, ClubIndex = 23 });
        for (var i = 0; i < 3; i++)
            TestSeason.PlayMatchDay(game);

        Assert.Equal(League.Bundesliga, game.Clubs[manager.ClubIndex].League);
        Assert.Equal(name, game.Clubs[manager.ClubIndex].Name);
        Assert.Equal(League.ZweiteLiga, game.Clubs[23].League);
        var table = LeagueTable.Compute(game, League.Bundesliga, TableView.Gesamt, game.PlayedMatchDays);
        Assert.Contains(table, r => r.Club.Index == manager.ClubIndex);
        Assert.Single(table.Select(r => r.Games).Distinct()); // der Platz bringt seine Spiele mit
        Assert.Equal(SquadSetup.ManagedSquadSize(League.Bundesliga) is var (g, d, f) ? g + d + f : 0,
            game.Clubs[manager.ClubIndex].Players.Count);
        Assert.Equal(SaveGame.Save(game), SaveGame.Save(Reload(game)));
    }

    private static GameState Reload(GameState game)
    {
        var loaded = SaveGame.Load(SaveGame.Save(game));
        Assert.True(loaded.Success, loaded.Error);
        return loaded.Game!;
    }
}
