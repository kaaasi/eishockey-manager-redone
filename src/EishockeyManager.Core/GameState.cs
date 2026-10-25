using System.Globalization;
using EishockeyManager.Formats;

namespace EishockeyManager.Core;

public sealed partial class Player
{
    public required string Name { get; init; }
    public required PlayerRole Role { get; init; }
    public bool IsForeigner { get; init; }
}

public sealed partial class Club
{
    public required int Index { get; init; }
    public required string Name { get; init; }
    public required int Strength { get; init; }

    /// <summary>Nummer der Logografik (kb\80–143) oder null.</summary>
    public int? LogoGraphic { get; init; }

    /// <summary>Liga, in der der Verein spielt, oder null (Nationalteam, Europapokal, ohne Liga).</summary>
    public League? League { get; set; }

    public List<Player> Players { get; } = [];
}

public sealed partial class Manager
{
    public required string Name { get; init; }
    public required int Head { get; init; }
    public required int ClubIndex { get; set; }
    public required int Level { get; init; }

    /// <summary>Kontostand in DM. Startwert im Original: 400.000 DM.</summary>
    public long Money { get; set; } = GameState.StartMoney;
}

/// <summary>Ein Spieltag: Paarungen (Heim, Gast) als Vereinsindizes.</summary>
public sealed record MatchDay(int Number, IReadOnlyList<(int Home, int Away)> Matches);

/// <summary>Gesamter Spielstand.</summary>
public sealed partial class GameState
{
    public const long StartMoney = 400_000;
    public const int FirstSeason = 1993;
    public const int ClubsPerLeague = 12;

    public required GameSetup Setup { get; init; }
    public required IReadOnlyList<Club> Clubs { get; init; }
    public required IReadOnlyList<Manager> Managers { get; init; }
    public required Dictionary<League, IReadOnlyList<MatchDay>> Schedules { get; init; }

    public int Season { get; set; } = FirstSeason;

    /// <summary>Gespielte Ligaspieltage (0 = vor dem ersten Spieltag).</summary>
    public int PlayedMatchDays { get; set; }

    /// <summary>Alle Ergebnisse der laufenden Saison.</summary>
    public List<MatchResult> Results { get; } = [];

    /// <summary>Tage (seit Saisonbeginn) der Ligaspieltage.</summary>
    public IReadOnlyList<int> MatchDayDates => SeasonCalendar.LeagueMatchDays(LeagueMatchDays);

    /// <summary>Anzahl der Ligaspieltage: vier Runden à 11 Spieltage (im Original belegt: 37. Spieltag im Januar).</summary>
    public int LeagueMatchDays => Schedules.Values.Max(s => s.Count);

    /// <summary>Aktueller Tag seit Saisonbeginn: Tag des zuletzt gespielten Spieltags.</summary>
    public int Day => PlayedMatchDays == 0 ? 0 : MatchDayDates[PlayedMatchDays - 1];

    public bool RegularSeasonOver => PlayedMatchDays >= LeagueMatchDays;

    /// <summary>Zufallsquelle des Spielstands.</summary>
    public Random Random { get; init; } = new();

    /// <summary>Welcher Manager gerade am Zug ist.</summary>
    public int CurrentManager { get; set; }

    public Manager ActiveManager => Managers[CurrentManager];

    public IEnumerable<Club> LeagueClubs(League league) => Clubs.Where(c => c.League == league);

    /// <summary>Datumszeile der Infoleiste, z. B. „Sonntag, 10. Sept.  1993“.</summary>
    public string DateText => SeasonCalendar.Format(Day, Season);

    /// <summary>Spielt den nächsten Ligaspieltag in allen Ligen. Liefert false, wenn die Hauptrunde vorbei ist.</summary>
    public bool PlayNextMatchDay()
    {
        if (RegularSeasonOver)
            return false;
        foreach (var (league, days) in Schedules)
        {
            if (PlayedMatchDays >= days.Count)
                continue;
            var day = days[PlayedMatchDays];
            foreach (var (home, away) in day.Matches)
            {
                var (hg, ag) = MatchEngine.Play(Clubs[home], Clubs[away], Random);
                Results.Add(new MatchResult(league, day.Number, home, away, hg, ag));
            }
        }
        PlayedMatchDays++;
        SettleFinances(Results.Where(r => r.MatchDay == PlayedMatchDays)); // Paket „manager“ (GameState.Manager.cs)
        return true;
    }

    /// <summary>Bisherige Begegnungen zweier Vereine in dieser Saison, älteste zuerst.</summary>
    public IEnumerable<MatchResult> Meetings(int club, int opponent) =>
        Results.Where(r => r.Involves(club) && r.Involves(opponent));

    /// <summary>Platz eines Vereins nach jedem gespielten Spieltag (für die Tabellenkurve).</summary>
    public IReadOnlyList<int> RankHistory(int club)
    {
        var history = new List<int>();
        if (Clubs[club].League is not { } league)
            return history;
        for (var md = 1; md <= PlayedMatchDays; md++)
        {
            var table = LeagueTable.Compute(this, league, TableView.Gesamt, md);
            history.Add(table.First(r => r.Club.Index == club).Rank);
        }
        return history;
    }

    /// <summary>Nächster Gegner des Vereins in der Liga oder null.</summary>
    public (Club Opponent, bool Home)? NextLeagueMatch(int clubIndex)
    {
        var club = Clubs[clubIndex];
        if (club.League is not { } league || !Schedules.TryGetValue(league, out var days))
            return null;
        if (PlayedMatchDays >= days.Count)
            return null;
        foreach (var (home, away) in days[PlayedMatchDays].Matches)
        {
            if (home == clubIndex)
                return (Clubs[away], true);
            if (away == clubIndex)
                return (Clubs[home], false);
        }
        return null;
    }

    /// <summary>Geldbetrag im Stil des Originals: „400.000 DM“, „-239.321 DM“.</summary>
    public static string FormatMoney(long amount) =>
        amount.ToString("#,0", CultureInfo.GetCultureInfo("de-DE")) + " DM";
}
