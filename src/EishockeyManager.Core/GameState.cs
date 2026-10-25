using System.Globalization;
using EishockeyManager.Formats;

namespace EishockeyManager.Core;

public sealed class Player
{
    public required string Name { get; init; }
    public required PlayerRole Role { get; init; }
    public bool IsForeigner { get; init; }
}

public sealed class Club
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

public sealed class Manager
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
public sealed class GameState
{
    public const long StartMoney = 400_000;
    public const int FirstSeason = 1993;
    public const int ClubsPerLeague = 12;

    public required GameSetup Setup { get; init; }
    public required IReadOnlyList<Club> Clubs { get; init; }
    public required IReadOnlyList<Manager> Managers { get; init; }
    public required Dictionary<League, IReadOnlyList<MatchDay>> Schedules { get; init; }

    public int Season { get; set; } = FirstSeason;

    /// <summary>Nächster Ligaspieltag (0 = vor dem ersten Spieltag).</summary>
    public int PlayedMatchDays { get; set; }

    /// <summary>Welcher Manager gerade am Zug ist.</summary>
    public int CurrentManager { get; set; }

    public Manager ActiveManager => Managers[CurrentManager];

    public IEnumerable<Club> LeagueClubs(League league) => Clubs.Where(c => c.League == league);

    /// <summary>Datumszeile der Infoleiste, z. B. „Sonntag, 10. Sept.  1993“.</summary>
    /// <remarks>[APPROX] Das Original zählt die Wochen vom 10. September an; der genaue Kalender ist noch nicht rekonstruiert.</remarks>
    public string DateText => $"Sonntag, 10. Sept.  {Season}";

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
