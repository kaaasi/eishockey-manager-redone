using EishockeyManager.Formats;

namespace EishockeyManager.Core;

/// <summary>Erzeugt einen neuen Spielstand aus Datenbank und Spielvorbereitung.</summary>
public static class NewGame
{
    /// <summary>Vereine 0–47 können in der Spielvorbereitung gewählt werden (4 Seiten à 12).</summary>
    public const int SelectableClubs = 48;

    public static GameState Create(ClubDatabase database, GameSetup setup, int seed)
    {
        var random = new Random(seed);

        var clubs = database.Clubs.Select(c =>
        {
            var club = new Club
            {
                Index = c.Index,
                Name = c.Name,
                Strength = c.Strength,
                LogoGraphic = c.LogoGraphic,
                League = c.Category switch
                {
                    ClubCategory.FirstLeague => League.Bundesliga,
                    ClubCategory.SecondLeague => League.ZweiteLiga,
                    ClubCategory.Oberliga => League.Oberliga,
                    _ => null,
                },
            };
            club.Players.AddRange(c.Players.Select(p => new Player { Name = p.Name, Role = p.Role, IsForeigner = p.IsForeigner }));
            return club;
        }).ToList();

        var managers = new List<Manager>();
        foreach (var m in setup.Managers)
        {
            var clubIndex = m.ClubIndex ?? PickRandomClub(clubs, m.StartLeague, m.StartStrength, managers, random);
            var club = clubs[clubIndex];

            // [APPROX] Ein Verein ohne Liga (Index 36–47) rückt in die Oberliga und verdrängt dort
            // einen zufälligen Verein, der von keinem Manager geführt wird.
            if (club.League is null)
            {
                var candidates = clubs
                    .Where(c => c.League == League.Oberliga && managers.All(x => x.ClubIndex != c.Index))
                    .ToList();
                var replaced = candidates[random.Next(candidates.Count)];
                replaced.League = null;
                club.League = League.Oberliga;
            }

            managers.Add(new Manager { Name = m.Name, Head = m.Head, ClubIndex = clubIndex, Level = m.Level });
        }

        var schedules = new Dictionary<League, IReadOnlyList<MatchDay>>();
        foreach (var league in Enum.GetValues<League>())
        {
            var members = clubs.Where(c => c.League == league).Select(c => c.Index).ToList();
            Shuffle(members, random);
            schedules[league] = Schedule.Rounds(members, rounds: 4);
        }

        var game = new GameState
        {
            Setup = setup,
            Clubs = clubs,
            Managers = managers,
            Schedules = schedules,
            Random = random,
        };
        SquadSetup.Initialize(game, seed); // Paket „mannschaft“: Kader, Spielerwerte, Aufstellung
        return game;
    }

    /// <summary>
    /// Zufälliger Verein der Start-Liga aus dem gewählten Drittel (nach Stärke sortiert).
    /// </summary>
    public static int PickRandomClub(IReadOnlyList<Club> clubs, League league, StartStrength strength,
        IEnumerable<Manager> taken, Random random)
    {
        var used = taken.Select(m => m.ClubIndex).ToHashSet();
        var sorted = clubs.Where(c => c.League == league).OrderByDescending(c => c.Strength).ToList();
        var third = sorted.Count / 3;
        var band = sorted.Skip((int)strength * third).Take(third).Where(c => !used.Contains(c.Index)).ToList();
        if (band.Count == 0)
            band = sorted.Where(c => !used.Contains(c.Index)).ToList();
        return band[random.Next(band.Count)].Index;
    }

    private static void Shuffle<T>(IList<T> list, Random random)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}

public static class Schedule
{
    /// <summary>Mehrere Runden: Hin- und Rückrunde werden wiederholt (4 Runden = 44 Spieltage bei 12 Vereinen).</summary>
    public static IReadOnlyList<MatchDay> Rounds(IReadOnlyList<int> teams, int rounds)
    {
        var two = DoubleRoundRobin(teams);
        var all = new List<MatchDay>();
        for (var r = 0; r < rounds / 2; r++)
            all.AddRange(two.Select(d => new MatchDay(all.Count + 1, d.Matches)));
        if (rounds % 2 == 1)
            all.AddRange(two.Take(two.Count / 2).Select(d => new MatchDay(all.Count + 1, d.Matches)));
        return all;
    }

    /// <summary>
    /// Hin- und Rückrunde nach dem Kreisverfahren.
    /// [APPROX] Der Spielplan des Originals ist noch nicht rekonstruiert.
    /// </summary>
    public static IReadOnlyList<MatchDay> DoubleRoundRobin(IReadOnlyList<int> teams)
    {
        var list = teams.ToList();
        if (list.Count % 2 == 1)
            list.Add(-1);
        var n = list.Count;
        var rounds = new List<MatchDay>();

        for (var round = 0; round < n - 1; round++)
        {
            var matches = new List<(int, int)>();
            for (var i = 0; i < n / 2; i++)
            {
                var a = list[i];
                var b = list[n - 1 - i];
                if (a < 0 || b < 0)
                    continue;
                matches.Add(round % 2 == 0 ? (a, b) : (b, a));
            }
            rounds.Add(new MatchDay(round + 1, matches));

            // Kreisverfahren: erstes Element fest, Rest rotieren
            var last = list[n - 1];
            list.RemoveAt(n - 1);
            list.Insert(1, last);
        }

        var second = rounds.Select((d, i) => new MatchDay(rounds.Count + i + 1,
            d.Matches.Select(m => (m.Item2, m.Item1)).ToList())).ToList();
        return [.. rounds, .. second];
    }
}
