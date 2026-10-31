using EishockeyManager.Tests.Support;
using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

public class SeasonCalendarTests
{
    [Fact]
    public void FirstDay_IsSunday10Sept() =>
        Assert.Equal("Sonntag, 10. Sept.  1993", SeasonCalendar.Format(0, 1993));

    [Fact]
    public void MatchDays_MatchObservationsFromOriginal()
    {
        var days = SeasonCalendar.LeagueMatchDays(44);
        // Belegt in Aufnahmen des Originals (Infoleiste des Hauptmenüs)
        Assert.Equal(0, days[0]);
        Assert.Equal("Mittwoch, 13. Sept.  1993", SeasonCalendar.Format(days[1], 1993));
        Assert.Equal("Sonntag, 5. Nov.  1995", SeasonCalendar.Format(days[16], 1995));
        Assert.Equal("Freitag, 8. Dez.  1993", SeasonCalendar.Format(days[23], 1993));
        Assert.Equal("Mittwoch, 17. Januar  1996", SeasonCalendar.Format(days[36], 1995));
        Assert.Equal("Freitag, 2. Februar  1994", SeasonCalendar.Format(days[42], 1993));
        Assert.Equal("Sonntag, 4. Februar  1994", SeasonCalendar.Format(days[43], 1993));
    }

    [Fact]
    public void Calendar_HasNoLeapDay()
    {
        // „Donnerstag, 22. März 1995“ und „Samstag, 14. April 1995“ (Video) – auch in Schaltjahren
        Assert.Equal("Donnerstag, 22. März  1996", SeasonCalendar.Format(193, 1995));
        Assert.Equal("Samstag, 14. April  1996", SeasonCalendar.Format(SeasonCalendar.ClubSeasonEnd + 1, 1995));
        Assert.Equal("Montag, 29. April  1994", SeasonCalendar.Format(SeasonCalendar.SeasonEnd, 1993));
    }

    [Fact]
    public void Calendar_CountsPerCompetition()
    {
        Assert.Equal(44, SeasonCalendar.DaysOf(DayEvents.League).Count);
        Assert.Equal(15, SeasonCalendar.DaysOf(DayEvents.Playoffs).Count);
        Assert.All(SeasonCalendar.DaysOf(DayEvents.Playoffs), d => Assert.True(d > SeasonCalendar.LeagueMatchDays(44)[^1]));
        Assert.Equal(DayEvents.SeasonEnd, SeasonCalendar.Events(SeasonCalendar.SeasonEnd));
    }

    /// <summary>Vergleich mit der Kalendertabelle in em.exe (übersprungen ohne Originaldateien).</summary>
    [GameDataFact]
    public void Calendar_MatchesTableInEmExe()
    {
        var data = TestGameData.Load();
        var exe = File.ReadAllBytes(Path.Combine(data.RootDirectory, "em.exe"));
        const int offset = 0x175F9;
        for (var day = 0; day < SeasonCalendar.Length; day++)
            Assert.True((byte)SeasonCalendar.Days[day] == exe[offset + day], $"Tag {day}");
    }
}

public class MatchEngineTests
{
    [Fact]
    public void AverageGoals_AroundSixAndAHalf()
    {
        var random = new Random(42);
        var a = new Club { Index = 0, Name = "A", Strength = 60 };
        var b = new Club { Index = 1, Name = "B", Strength = 60 };
        var total = 0;
        var draws = 0;
        const int n = 20000;
        for (var i = 0; i < n; i++)
        {
            var (h, g) = MatchEngine.Play(a, b, random);
            total += h + g;
            if (h == g) draws++;
        }
        Assert.InRange(total / (double)n, 6.0, 7.0);
        Assert.InRange(draws / (double)n, 0.05, 0.2);
    }

    [Fact]
    public void StrongerTeam_WinsMoreOften()
    {
        var random = new Random(1);
        var strong = new Club { Index = 0, Name = "S", Strength = 90 };
        var weak = new Club { Index = 1, Name = "W", Strength = 40 };
        var wins = 0;
        for (var i = 0; i < 2000; i++)
        {
            var (h, g) = MatchEngine.Play(strong, weak, random);
            if (h > g) wins++;
        }
        Assert.True(wins > 1400);
    }
}

public class LeagueTableTests
{
    [Fact]
    public void FullSeason_TableIsConsistent()
    {
        var game = TestGames.Create();
        while (game.PlayNextMatchDay()) { }

        Assert.Equal(44, game.PlayedMatchDays);
        var table = LeagueTable.Compute(game, League.Bundesliga, TableView.Gesamt, 44);
        Assert.Equal(12, table.Count);
        Assert.All(table, r => Assert.Equal(44, r.Games));
        Assert.Equal(table.Sum(r => r.GoalsFor), table.Sum(r => r.GoalsAgainst));
        Assert.Equal(table.Sum(r => r.PointsPlus), table.Sum(r => r.PointsMinus));
        Assert.Equal(Enumerable.Range(1, 12), table.Select(r => r.Rank));
        Assert.All(table, r => Assert.Equal(8, r.Balance.Length));

        var home = LeagueTable.Compute(game, League.Bundesliga, TableView.Heim, 44);
        Assert.All(home, r => Assert.Equal(22, r.Games));
        // Spur tabellen-original: Die Bilanz gilt für alle Ansichten (em.exe 0x4F78D), früher nur Heimspiele in „Heim“.
        Assert.All(home, r => Assert.Equal(table.Single(t => t.Club.Index == r.Club.Index).Balance, r.Balance));
    }

    [Fact]
    public void Meetings_AndRankHistory()
    {
        var game = TestGames.Create();
        for (var i = 0; i < 12; i++) game.PlayNextMatchDay();
        var club = game.ActiveManager.ClubIndex;
        Assert.Equal(12, game.RankHistory(club).Count);
        var opponent = game.Results.First(r => r.Involves(club));
        var other = opponent.Home == club ? opponent.Away : opponent.Home;
        Assert.Equal(2, game.Meetings(club, other).Count()); // nach 12 Spieltagen: Hin- und Rückspiel
    }
}
