using EishockeyManager.Formats;

namespace EishockeyManager.Core;

/// <summary>Erzeugt einen neuen Spielstand aus Datenbank und Spielvorbereitung.</summary>
public static class NewGame
{
    /// <summary>Vereine 0–47 können in der Spielvorbereitung gewählt werden (4 Seiten à 12).</summary>
    public const int SelectableClubs = 48;

    public static GameState Create(ClubDatabase database, GameSetup setup, int seed)
    {
        var random = new GameRandom(seed);

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

        var managers = AssignClubs(clubs, setup, random);

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
        game.AssignStartPlayerIds(); // Spur „speichern“: dauerhafte Spieler-IDs, verbraucht keinen Zufall
        game.InitializeManagerSystems();
        return game;
    }

    /// <summary>
    /// Exklusive Vereinsvergabe (Befund R2-07): Erst werden die festen Wahlen aller Manager reserviert und geprüft,
    /// dann rücken ligalose Wahlen in die Oberliga (Verdrängung nur nicht reservierter Vereine), zuletzt folgen die
    /// Zufallswahlen unter Ausschluss aller bereits vergebenen Vereine. Die Reihenfolge der Manager bleibt erhalten.
    /// </summary>
    /// <exception cref="ArgumentException">Fester Verein außerhalb 0–47 oder von zwei Managern gewählt.</exception>
    public static List<Manager> AssignClubs(IReadOnlyList<Club> clubs, GameSetup setup, Random random)
    {
        var reserved = new HashSet<int>();
        foreach (var m in setup.Managers)
        {
            if (m.ClubIndex is not { } chosen)
                continue;
            if (chosen < 0 || chosen >= SelectableClubs)
                throw new ArgumentException($"Verein {chosen} ist nicht wählbar (0–{SelectableClubs - 1}).", nameof(setup));
            if (!reserved.Add(chosen))
                throw new ArgumentException($"Verein {chosen} wurde von mehreren Managern gewählt.", nameof(setup));
        }

        // [APPROX] Ein Verein ohne Liga (Index 36–47) rückt in die Oberliga und verdrängt dort einen zufälligen
        // Verein, den kein Manager fest gewählt hat.
        foreach (var chosen in setup.Managers.Select(m => m.ClubIndex).OfType<int>())
        {
            var club = clubs[chosen];
            if (club.League is not null)
                continue;
            var candidates = clubs.Where(c => c.League == League.Oberliga && !reserved.Contains(c.Index)).ToList();
            var replaced = candidates[random.Next(candidates.Count)];
            replaced.League = null;
            club.League = League.Oberliga;
        }

        var assigned = new int[setup.Managers.Count];
        for (var i = 0; i < setup.Managers.Count; i++)
        {
            var m = setup.Managers[i];
            if (m.ClubIndex is { } chosen)
            {
                assigned[i] = chosen;
                continue;
            }
            assigned[i] = PickRandomClub(clubs, m.StartLeague, m.StartStrength, reserved, random);
            reserved.Add(assigned[i]);
        }

        return setup.Managers
            .Select((m, i) => new Manager
            {
                Name = m.Name, Head = m.Head, ClubIndex = assigned[i], Level = m.Level,
                Money = FinanceRules.StartMoney(m.Level), // [ORIGINAL] 0x29BD5 (Spur finanzen-2, #78)
            })
            .ToList();
    }

    /// <summary>
    /// Zufälliger Verein der Start-Liga aus dem gewählten Drittel (nach Stärke sortiert), ohne die vergebenen Vereine.
    /// Ist das Drittel voll vergeben, wird aus der ganzen Liga gewählt.
    /// </summary>
    public static int PickRandomClub(IReadOnlyList<Club> clubs, League league, StartStrength strength,
        IReadOnlySet<int> taken, Random random)
    {
        var sorted = clubs.Where(c => c.League == league).OrderByDescending(c => c.Strength).ToList();
        var third = sorted.Count / 3;
        var band = sorted.Skip((int)strength * third).Take(third).Where(c => !taken.Contains(c.Index)).ToList();
        if (band.Count == 0)
            band = sorted.Where(c => !taken.Contains(c.Index)).ToList();
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
