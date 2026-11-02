using System.Text.Json.Nodes;
using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur statistiken-verein (Runde 8, Welle 3): Werte des Bildschirms „Verein“ (em.exe 0x2D5AE) – Serien (0x50283,
/// 0x50CDB), Über-/Unterzahl (0xAA2C, 0x2D9D9, 0x5588E), Torquoten (0x2ECA0), Benötigter Schnitt (0x3EB30) und Schulden
/// (0x57768). Gegen Base 99fa995 rot (Serien, Rekorde, Zähler und Werte fehlten). Dazu R8-B02 (Strafminuten im Ligaspiel).
/// </summary>
public class StatistikenVereinTests
{
    private static GameState NewGame1(int seed = 3)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Test", Head = 0, ClubIndex = 5, StartLeague = League.Bundesliga });
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    private static int Value(int[] series, SeriesKind kind, SeriesColumn column) => series[Series.Index(kind, column)];

    [Fact]
    public void Series_Win_Draw_Loss_FollowTheOriginalRules()
    {
        var s = new int[Series.Count];
        Series.Enter(s, 3, 0, home: true);  // Heimsieg zu null
        Series.Enter(s, 2, 1, home: false); // Auswärtssieg
        Assert.Equal(2, Value(s, SeriesKind.Won, SeriesColumn.Total));
        Assert.Equal(1, Value(s, SeriesKind.Won, SeriesColumn.Home));
        Assert.Equal(1, Value(s, SeriesKind.Won, SeriesColumn.Away));
        Assert.Equal(2, Value(s, SeriesKind.NotLost, SeriesColumn.Total));
        Assert.Equal(0, Value(s, SeriesKind.NotWon, SeriesColumn.Total));
        Assert.Equal(0, Value(s, SeriesKind.CleanSheet, SeriesColumn.Total)); // Gegentor im zweiten Spiel
        Assert.Equal(1, Value(s, SeriesKind.CleanSheet, SeriesColumn.Home));  // Heimspalte bleibt stehen

        Series.Enter(s, 1, 1, home: true); // Unentschieden
        Assert.Equal(0, Value(s, SeriesKind.Won, SeriesColumn.Total));
        Assert.Equal(0, Value(s, SeriesKind.Won, SeriesColumn.Home));
        Assert.Equal(1, Value(s, SeriesKind.Won, SeriesColumn.Away)); // Auswärtsspalte unberührt
        Assert.Equal(1, Value(s, SeriesKind.Drawn, SeriesColumn.Total));
        Assert.Equal(1, Value(s, SeriesKind.NotWon, SeriesColumn.Total));
        Assert.Equal(3, Value(s, SeriesKind.NotLost, SeriesColumn.Total));
        Assert.Equal(0, Value(s, SeriesKind.Lost, SeriesColumn.Total));

        Series.Enter(s, 0, 4, home: false); // Niederlage ohne Tor
        Assert.Equal(1, Value(s, SeriesKind.Lost, SeriesColumn.Total));
        Assert.Equal(1, Value(s, SeriesKind.Lost, SeriesColumn.Away));
        Assert.Equal(0, Value(s, SeriesKind.Drawn, SeriesColumn.Total));
        Assert.Equal(2, Value(s, SeriesKind.NotWon, SeriesColumn.Total));
        Assert.Equal(0, Value(s, SeriesKind.NotLost, SeriesColumn.Total));
        Assert.Equal(1, Value(s, SeriesKind.NoGoal, SeriesColumn.Total));
        Assert.Equal(1, Value(s, SeriesKind.NoGoal, SeriesColumn.Away));
        Assert.Equal(0, Value(s, SeriesKind.NoGoal, SeriesColumn.Home));
    }

    [Fact]
    public void Series_IsAByte_Wraps()
    {
        var s = new int[Series.Count];
        s[Series.Index(SeriesKind.NotLost, SeriesColumn.Total)] = 255;
        Series.Enter(s, 1, 0, home: true);
        Assert.Equal(0, Value(s, SeriesKind.NotLost, SeriesColumn.Total));
    }

    [Fact]
    public void Records_TakeOnlyGreaterValues()
    {
        var records = new int[Series.Count];
        records[0] = 5;
        var current = new int[Series.Count];
        current[0] = 3;
        current[1] = 4;
        Series.UpdateRecords(records, current);
        Assert.Equal(5, records[0]);
        Assert.Equal(4, records[1]);
    }

    /// <summary>Ligaspieltag: Serien aller Vereine aus den Ergebnissen, Rekorde der Manager danach.</summary>
    [Fact]
    public void MatchDays_CountSeriesForAllClubs_AndRecordsForManagers()
    {
        var game = NewGame1();
        var expected = new Dictionary<int, int[]>();
        foreach (var club in game.Clubs)
            expected[club.Index] = new int[Series.Count];
        var record = new int[Series.Count];
        for (var day = 0; day < 6; day++)
        {
            var before = game.Results.Count;
            TestSeason.PlayMatchDay(game);
            foreach (var r in game.Results.Skip(before))
            {
                Series.Enter(expected[r.Home], r.HomeGoals, r.AwayGoals, home: true);
                Series.Enter(expected[r.Away], r.AwayGoals, r.HomeGoals, home: false);
            }
            Series.UpdateRecords(record, expected[game.ActiveManager.ClubIndex]);
        }
        foreach (var club in game.Clubs)
            Assert.Equal(expected[club.Index], club.SeriesCurrent);
        Assert.Equal(record, game.ActiveManager.SeriesRecords);
        Assert.Contains(game.ActiveManager.SeriesRecords, v => v > 0);
    }

    /// <summary>[ORIGINAL] 0xAA2C–0xAAEF: Zähler je Lage, Seite des Managers und Schütze.</summary>
    [Theory]
    [InlineData(true, 5, 4, true, PowerPlayCounter.GoalsInPowerPlay)]
    [InlineData(true, 5, 4, false, PowerPlayCounter.GoalsAgainstInPowerPlay)]
    [InlineData(true, 4, 5, true, PowerPlayCounter.GoalsShortHanded)]
    [InlineData(true, 4, 5, false, PowerPlayCounter.GoalsAgainstShortHanded)]
    [InlineData(false, 4, 5, false, PowerPlayCounter.GoalsInPowerPlay)]
    [InlineData(false, 4, 5, true, PowerPlayCounter.GoalsAgainstInPowerPlay)]
    [InlineData(false, 5, 3, false, PowerPlayCounter.GoalsShortHanded)]
    [InlineData(false, 5, 3, true, PowerPlayCounter.GoalsAgainstShortHanded)]
    public void PowerPlayGoal_MapsToTheOriginalCounter(bool managerHome, int home, int away, bool homeScored, PowerPlayCounter expected) =>
        Assert.Equal(expected, PowerPlay.Goal(managerHome, home, away, homeScored));

    [Fact]
    public void PowerPlayGoal_EvenStrength_CountsNothing() => Assert.Null(PowerPlay.Goal(true, 4, 4, true));

    [Fact]
    public void PowerPlaySuccess_Percentages_WithEmptyDivisorsAndClamp()
    {
        Assert.Equal(0, PowerPlay.PowerPlaySuccess([0, 0, 0, 0, 0, 0]));
        Assert.Equal(100, PowerPlay.ShortHandedSuccess([0, 0, 0, 0, 0, 0]));
        Assert.Equal(35, PowerPlay.PowerPlaySuccess([5, 0, 0, 2, 14, 6]));   // Video folge01_01022: 35 %
        Assert.Equal(67, PowerPlay.ShortHandedSuccess([5, 0, 0, 2, 14, 6])); // 100 − 200 / 6 = 67 %
        Assert.Equal(100, PowerPlay.PowerPlaySuccess([9, 0, 0, 0, 3, 0]));
        Assert.Equal(0, PowerPlay.ShortHandedSuccess([0, 0, 0, 9, 0, 3]));
    }

    [Fact]
    public void CountPowerPlayGoal_OnlyManagerClubs()
    {
        var game = NewGame1();
        var manager = game.ActiveManager;
        var own = game.Clubs[manager.ClubIndex];
        var other = game.Clubs.First(c => c.Index != own.Index && c.League == own.League);
        game.CountPowerPlayGoal(own, other, 5, 4, homeScored: true);
        game.CountPowerPlayGoal(other, own, 5, 4, homeScored: true);
        Assert.Equal([1, 0, 0, 1, 0, 0], manager.PowerPlayCounts);
    }

    [Fact]
    public void GoalsPerGame_TotalHomeAway_InTenths()
    {
        var game = NewGame1();
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        Assert.Equal(0, game.GoalsPerGameTenths(club, SeriesColumn.Total, against: false));
        game.Results.Add(new MatchResult(League.Bundesliga, 1, club.Index, 1, 4, 1));
        game.Results.Add(new MatchResult(League.Bundesliga, 2, 2, club.Index, 2, 1));
        game.Results.Add(new MatchResult(League.Bundesliga, 3, club.Index, 3, 3, 3));
        Assert.Equal(26, game.GoalsPerGameTenths(club, SeriesColumn.Total, against: false)); // 8 / 3
        Assert.Equal(35, game.GoalsPerGameTenths(club, SeriesColumn.Home, against: false));  // 7 / 2
        Assert.Equal(10, game.GoalsPerGameTenths(club, SeriesColumn.Away, against: false));
        Assert.Equal(20, game.GoalsPerGameTenths(club, SeriesColumn.Total, against: true));  // 6 / 3
        Assert.Equal(20, game.GoalsPerGameTenths(club, SeriesColumn.Away, against: true));
    }

    [Fact]
    public void RequiredAverage_FollowsFormula_AndIsNeverNegative()
    {
        var game = NewGame1();
        var manager = game.ActiveManager;
        var thirds = (game.MonthlyExpenses(manager) - game.MonthlyIncome(manager)) * 3;
        var price = game.Finance(manager).Stadium.TicketPrice;
        Assert.Equal(Math.Max(0, thirds * 12 / 23 / 3 / price), game.RequiredAverage(manager));
        game.Finance(manager).Stadium.TicketPrice = 2 * price;
        Assert.Equal(Math.Max(0, thirds * 12 / 23 / 3 / (2 * price)), game.RequiredAverage(manager));
        Assert.True(game.RequiredAverage(manager) >= 0);
    }

    [Fact]
    public void TotalDebts_BankCreditAndNetManagerLoans()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", Head = 0, ClubIndex = 5, StartLeague = League.Bundesliga });
        setup.Managers.Add(new ManagerSetup { Name = "B", Head = 1, ClubIndex = 6, StartLeague = League.Bundesliga });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 3);
        var a = game.Managers[0];
        Assert.Equal(0, game.TotalDebts(a));
        game.Finance(a).Bank.Credit = 100_000;
        game.ManagerLoans.Add(new ManagerLoan { Lender = 1, Borrower = 0, Amount = 50_000 });
        game.ManagerLoans.Add(new ManagerLoan { Lender = 0, Borrower = 1, Amount = 20_000 });
        Assert.Equal(130_000, game.TotalDebts(a));
        Assert.Equal(0, game.TotalDebts(game.Managers[1])); // B schuldet netto nichts
    }

    /// <summary>R8-B02: Heim- und Gaststrafminuten jedes Ligaspiels landen beim jeweiligen Verein; Pokal zählt nicht.</summary>
    [Fact]
    public void LeaguePenalties_SumOfReports_HomeAndAway()
    {
        var home = new Club { Index = 0, Name = "H", Strength = 50 };
        var away = new Club { Index = 1, Name = "A", Strength = 50 };
        var report = new MatchReport { HomePenaltyMinutes = 12, AwayPenaltyMinutes = 7 };
        GameState.CountLeaguePenalties(home, away, report);
        GameState.CountLeaguePenalties(away, home, new MatchReport { HomePenaltyMinutes = 2, AwayPenaltyMinutes = 5 });
        Assert.Equal(17, home.LeaguePenaltyMinutes);
        Assert.Equal(9, away.LeaguePenaltyMinutes);
    }

    /// <summary>R8-B02: Nur Ligaspieltage zählen – über die Zeitleiste bis nach dem ersten DEB-Pokal-Termin.</summary>
    [Fact]
    public void LeaguePenalties_OnlyLeagueMatchDaysCount()
    {
        var game = NewGame1();
        var sums = new List<int>();
        for (var day = 0; day < 8; day++)
        {
            var before = game.Clubs.Sum(c => c.LeaguePenaltyMinutes);
            var played = game.PlayedMatchDays;
            game.Continue();
            var after = game.Clubs.Sum(c => c.LeaguePenaltyMinutes);
            if (game.PlayedMatchDays == played)
                Assert.Equal(before, after); // Tage ohne Ligaspiel (auch Pokaltermine) ändern nichts
            else
                sums.Add(after - before);
        }
        Assert.NotEmpty(sums);
    }

    [Fact]
    public void ScreenValues_AreReadOnly()
    {
        var game = NewGame1();
        TestSeason.PlayMatchDay(game);
        var manager = game.ActiveManager;
        var before = SaveGame.Save(game);
        _ = game.RequiredAverage(manager);
        _ = game.TotalDebts(manager);
        _ = game.GoalsPerGameTenths(game.Clubs[manager.ClubIndex], SeriesColumn.Home, against: true);
        Assert.Equal(before, SaveGame.Save(game));
    }

    /// <summary>Migration der Spur (vorläufig 14 → 15): ältere Spielstände laden mit leeren Serien und Zählern.</summary>
    [Fact]
    public void OldSave_WithoutSeries_LoadsWithZeros()
    {
        var game = NewGame1();
        TestSeason.PlayMatchDay(game);
        var root = JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        root["schema"] = 14;
        foreach (var club in root["vereine"]!.AsArray())
            Assert.True(club!.AsObject().Remove("SeriesCurrent"));
        foreach (var m in root["manager"]!.AsArray())
        {
            Assert.True(m!.AsObject().Remove("SeriesRecords"));
            Assert.True(m.AsObject().Remove("PowerPlayCounts"));
        }
        var loaded = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.True(loaded.Success, loaded.Error);
        Assert.All(loaded.Game!.Clubs, c => Assert.All(c.SeriesCurrent, v => Assert.Equal(0, v)));
        Assert.All(loaded.Game.Managers, m => Assert.Equal(new int[PowerPlay.Count], m.PowerPlayCounts));
        Assert.All(loaded.Game.Managers, m => Assert.Equal(Series.Count, m.SeriesRecords.Length));
    }

    /// <summary>
    /// Mehrsaisonprobe (4 Saisonwechsel, passiver und aktiver Manager): Serien laufen über das Saisonende weiter und
    /// stimmen mit allen Ligaergebnissen seit Spielbeginn überein; Rekorde fallen nie; Über-/Unterzahl beginnt jede
    /// Saison bei 0; Sichern und Laden ändern nichts.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultiSeason_SeriesContinue_RecordsNeverFall_PowerPlayResets(bool active)
    {
        var game = TestGames.Create(21);
        var manager = game.ActiveManager;
        var expected = game.Clubs.ToDictionary(c => c.Index, _ => new int[Series.Count]);
        var lastRecords = new int[Series.Count];
        for (var change = 0; change < 4; change++)
        {
            var counted = 0;
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 1000, "Saisonende nicht erreicht");
                // Integration Runde 11 (+ wettbewerbe-spiel): Kader spielfähig halten (TestSquadKeeper) – mit den Regeln aus
                // kader-transfer schrumpfte der passive Kader in Saison 4 auf 9 (8 aufgestellt), alle Spiele 0:2 ohne Minutenrechnung.
                TestSquadKeeper.KeepPlayable(game, manager);
                if (manager.Money < 2_000_000)
                    manager.Money = 2_000_000;
                if (active && game.Phase == SeasonPhase.Running)
                    game.CountPowerPlayGoal(game.Clubs[manager.ClubIndex], game.Clubs[0], 5, 4, homeScored: true);
                game.Continue();
                foreach (var r in game.Results.Skip(counted))
                {
                    Series.Enter(expected[r.Home], r.HomeGoals, r.AwayGoals, home: true);
                    Series.Enter(expected[r.Away], r.AwayGoals, r.HomeGoals, home: false);
                }
                counted = game.Results.Count;
                for (var i = 0; i < Series.Count; i++)
                    Assert.True(manager.SeriesRecords[i] >= lastRecords[i]);
                manager.SeriesRecords.CopyTo(lastRecords, 0);
            }
            // [ORIGINAL] 0x50283 trägt auch WM-Gruppenspiele (Code 8) in die Serien ein – die Länder (Spur wettbewerbe-komplett, Teil 2).
            foreach (var r in game.WorldCup?.Groups.SelectMany(g => g.Results) ?? [])
            {
                Series.Enter(expected[r.Home], r.HomeGoals, r.AwayGoals, home: true);
                Series.Enter(expected[r.Away], r.AwayGoals, r.HomeGoals, home: false);
            }
            // Spielberechnung Teil 4: die Minutenrechnung zählt jetzt selbst (vorher blieb der passive Manager bei 0).
            Assert.True(manager.PowerPlayCounts.Sum() > 0);
            if (active)
                Assert.True(manager.PowerPlayCounts[0] > 0);
            game.StartNextSeason();
            Assert.All(manager.PowerPlayCounts, v => Assert.Equal(0, v));
            foreach (var club in game.Clubs)
                Assert.Equal(expected[club.Index], club.SeriesCurrent);
            var bytes = SaveGame.Save(game);
            var loaded = SaveGame.Load(bytes);
            Assert.True(loaded.Success, loaded.Error);
            Assert.Equal(bytes, SaveGame.Save(loaded.Game!));
        }
    }
}
