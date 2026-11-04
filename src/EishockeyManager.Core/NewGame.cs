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

        var swaps = new List<(int Club, int Partner)>();
        var managers = AssignClubs(clubs, setup, random, swaps);
        foreach (var (club, partner) in swaps)
            (clubs[club], clubs[partner]) = (WithStrength(clubs[club], clubs[partner].Strength),
                WithStrength(clubs[partner], clubs[club].Strength));

        var schedules = new Dictionary<League, IReadOnlyList<MatchDay>>();
        foreach (var league in Enum.GetValues<League>())
        {
            var members = clubs.Where(c => c.League == league).Select(c => c.Index).ToList();
            // [APPROX] Die Reihenfolge der Vereine spielt für den festen Spielplan keine Rolle (Schedule.Rounds sortiert nach
            // Vereinsindex); die Mischung bleibt, damit der Zufallsverbrauch des Spielstarts unverändert bleibt.
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
        game.InitializeLeagueSlots(); // Spur saisonwechsel Teil 3 (S-K6): Ligamischung 0x35EDD, verbraucht keinen Zufall
        game.BeginFirstTurn(); // ui-rest Teil 4 (R12U-04): Faktor des ersten Managerzugs (0x44E3F), eigene Zufallsquelle
        return game;
    }

    /// <summary>
    /// Vereinsvergabe beim Spielstart (Befund R2-07, U15-a): Erst werden die festen Wahlen aller Manager geprüft und der
    /// Reihe nach in die Start-Liga gesetzt (<see cref="PlaceInStartLeague"/>), zuletzt folgen die Zufallswahlen unter
    /// Ausschluss aller bereits vergebenen Vereine. Die Reihenfolge der Manager bleibt erhalten.
    /// </summary>
    /// <param name="swaps">Erhält je Platztausch das Paar (Managerverein, Tauschpartner); die Teamwerte bleiben am Platz
    /// (<see cref="Create"/> tauscht dafür die Stärke).</param>
    /// <exception cref="ArgumentException">Fester Verein außerhalb 0–47 oder von zwei Managern gewählt.</exception>
    public static List<Manager> AssignClubs(IReadOnlyList<Club> clubs, GameSetup setup, Random random,
        List<(int Club, int Partner)>? swaps = null)
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

        var league = setup.StartLeague;
        var earlier = new HashSet<int>();
        foreach (var chosen in setup.Managers.Select(m => m.ClubIndex).OfType<int>())
        {
            if (PlaceInStartLeague(clubs, chosen, league, earlier, random) is { } partner && partner != chosen)
                swaps?.Add((chosen, partner));
            earlier.Add(chosen);
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
            // [APPROX] Zufallswahl nach Start-Stärke gibt es im Original nicht (jeder Manager wählt einen Verein, Datei
            // 0x2964C); das Remake zieht aus dem Drittel der Start-Liga ohne Europapokal-Teilnehmer (Zeile 1, Datei 0x45CD0).
            // Zu Spielbeginn ist nur der Deutsche Meister (Verein 2) als Teilnehmer belegt. Kein Platztausch.
            var excluded = new HashSet<int>(reserved);
            if (league == League.Bundesliga && clubs[EuropeanCup.InitialChampion].League == League.Bundesliga)
                excluded.Add(EuropeanCup.InitialChampion);
            assigned[i] = PickRandomClub(clubs, league, m.StartStrength, excluded, random);
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
    /// Setzt den fest gewählten Verein eines Managers in die Start-Liga (U15-a, Entscheidung Nutzer D7 = D).
    /// [ORIGINAL] em.exe Datei 0x29C3B–0x29CF2: Behalten nur, wenn Start-Liga ≠ Bundesliga, der Verein schon in ihr spielt
    /// und sein Platz ≤ DGROUP 0x7CB (35) ist. Sonst Zufallsplatz r der Start-Liga (Datei 0x29C78
    /// <c>random([0x7C0+L], [0x7C0+L]+[0x7B1+L]−1)</c>), neu gezogen, solange r der Verein eines früheren Managers ist
    /// (Datei 0x29C9B–0x29CC3) oder in Zeile 1 des Europapokals steht (Datei 0x29CCC, 0x45CD0(r, 1)); dann Tausch
    /// Datei 0x29CF2 <c>0x36169(v, r, 0x80)</c>: Name, Logo, Manager und Spieler wandern, die Teamwerte bleiben am Platz.
    /// Der Verdrängte übernimmt Platz und Liga des Managervereins (auch „ohne Liga“). Bei Start-Liga Bundesliga wird
    /// immer gezogen, r = v ist möglich. Belege FILE27/FILE28 (Köln fest, Start-Liga 2. Liga: Weißwasser bzw. Hannover in
    /// der Bundesliga mit Kölns Teamwerten), FILE15 (Bayreuth bleibt in der 2. Liga); <c>docs/analyse/re-runde-13.md</c>, U15-a.
    /// [APPROX] Zufallsquelle: <see cref="GameState.Random"/> statt des gemeinsamen Zufalls; gezogen wird ein Verein der
    /// Start-Liga in Indexfolge (die Satzfolge entsteht im Remake erst nach der Vergabe, R-K5). Europapokal-Zeile 1 zu
    /// Spielbeginn: nur der Deutsche Meister (Verein 2).
    /// </summary>
    /// <returns>Tauschpartner (auch der Verein selbst bei r = v) oder null, wenn der Verein bleibt.</returns>
    internal static int? PlaceInStartLeague(IReadOnlyList<Club> clubs, int club, League league,
        IReadOnlySet<int> earlierManagers, Random random)
    {
        var own = clubs[club];
        if (league != League.Bundesliga && own.League == league && club <= LastLeagueSlot)
            return null;
        var members = clubs.Where(c => c.League == league).Select(c => c.Index).Order().ToList();
        var candidates = members.Where(r => !earlierManagers.Contains(r)
            && !(league == League.Bundesliga && r == EuropeanCup.InitialChampion)).ToList();
        if (candidates.Count == 0)
            return null;
        int partner;
        do
            partner = members[random.Next(members.Count)];
        while (!candidates.Contains(partner));
        if (partner == club)
            return partner;
        var other = clubs[partner];
        (own.League, other.League) = (other.League, own.League);
        return partner;
    }

    /// <summary>Letzter Ligaplatz. [ORIGINAL] DGROUP 0x7CB = 35 (Plätze 0–35 Bundesliga bis Oberliga).</summary>
    private const int LastLeagueSlot = 35;

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

    /// <summary>Kopie eines Vereins mit anderer Stärke (Teamwerte bleiben beim Platztausch am Platz, U15-a).</summary>
    private static Club WithStrength(Club club, int strength)
    {
        var copy = new Club
        {
            Index = club.Index, Name = club.Name, Strength = strength, LogoGraphic = club.LogoGraphic, League = club.League,
        };
        copy.Players.AddRange(club.Players);
        return copy;
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
    /// <summary>
    /// [ORIGINAL] Liga-Spielplan em.exe Datei 0x4E7C7 (A113, A282): Zeile d = 1…11 der Tabelle aus DGROUP 0x2DC,
    /// Buchstabe 'a'…'l' = Platz des Vereins innerhalb der Liga (Vereinsindex aufsteigend, Basis 0/12/24). Ab Spieltag 12
    /// wiederholt sich die Tabelle mit getauschtem Heimrecht (Runde 2 und 4), Runde 3 wie Runde 1.
    /// </summary>
    private static readonly string[] OriginalTable =
    [
        "abcdefghijkl",
        "dafchejglibk",
        "ahgbcjidkefl",
        "eabilcdfkghj",
        "aljbcegdfhik",
        "aclbdjeifgkh",
        "jabekcglhdif",
        "akfbcgldejhi",
        "fabcdelhgijk",
        "aghbkdicjfel",
        "iabdchfkeglj",
    ];

    /// <summary>
    /// Mehrere Runden: Hin- und Rückrunde werden wiederholt (4 Runden = 44 Spieltage bei 12 Vereinen).
    /// Bei genau 12 Vereinen gilt die feste Tabelle des Originals ([ORIGINAL]); die Reihenfolge von <paramref name="teams"/>
    /// spielt dann keine Rolle (Platz = Rang des Vereinsindex).
    /// </summary>
    public static IReadOnlyList<MatchDay> Rounds(IReadOnlyList<int> teams, int rounds)
    {
        if (teams.Count == 12 && rounds % 2 == 0)
            return OriginalRounds(teams.Order().ToList(), rounds);
        var two = DoubleRoundRobin(teams);
        var all = new List<MatchDay>();
        for (var r = 0; r < rounds / 2; r++)
            all.AddRange(two.Select(d => new MatchDay(all.Count + 1, d.Matches)));
        if (rounds % 2 == 1)
            all.AddRange(two.Take(two.Count / 2).Select(d => new MatchDay(all.Count + 1, d.Matches)));
        return all;
    }

    private static IReadOnlyList<MatchDay> OriginalRounds(IReadOnlyList<int> sorted, int rounds)
    {
        var days = new List<MatchDay>();
        for (var day = 1; day <= rounds * 11; day++)
        {
            var d = day;
            var swap = false;
            while (d >= 12)
            {
                d -= 11;
                swap = !swap;
            }

            // Wiederholte Runden teilen die Paarungsliste (wie vor Runde 12; Spielstand speichert sie als Verweis).
            if (day > 22)
            {
                days.Add(new MatchDay(day, days[day - 23].Matches));
                continue;
            }

            var row = OriginalTable[d - 1];
            var matches = new List<(int, int)>();
            for (var k = 0; k < 6; k++)
            {
                var home = sorted[row[2 * k] - 'a'];
                var away = sorted[row[2 * k + 1] - 'a'];
                matches.Add(swap ? (away, home) : (home, away));
            }
            days.Add(new MatchDay(day, matches));
        }
        return days;
    }

    /// <summary>
    /// Hin- und Rückrunde nach dem Kreisverfahren (Rückfall für Ligen mit ungleich 12 Vereinen und die Play-off-Runden).
    /// [APPROX] Nur der Liga-Spielplan mit 12 Vereinen ist aus dem Original belegt (<see cref="Rounds"/>).
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
