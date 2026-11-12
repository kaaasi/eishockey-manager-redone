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

        var places = StartOrder(seed); // R-K5: Ligamischung vor der Spielvorbereitung (Datei 0x43A17)
        var swaps = new List<(int Club, int Partner)>();
        var managers = AssignClubs(clubs, setup, random, swaps, places);
        foreach (var (club, partner) in swaps)
            (clubs[club], clubs[partner]) = (WithStrength(clubs[club], clubs[partner].Strength),
                WithStrength(clubs[partner], clubs[club].Strength));

        var schedules = new Dictionary<League, IReadOnlyList<MatchDay>>();
        foreach (var league in Enum.GetValues<League>())
        {
            var members = clubs.Where(c => c.League == league).Select(c => c.Index).ToList();
            // [APPROX] Das Original mischt hier nicht; die Mischung bleibt nur, damit der Zufallsverbrauch des Spielstarts
            // unverändert bleibt. Der Plan richtet sich nach der Satzfolge (A113, Schedule.ForSlots).
            Shuffle(members, random);
            schedules[league] = Schedule.ForSlots(StartSlots(clubs, places, league), rounds: 4);
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
        game.InitializeLeagueSlots(places); // S-K6/R-K5: Satzfolge = Startmischung mit den Plätzen nach dem Managertausch
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
    /// <param name="places">Satzfolge Platz → Verein (48 Einträge, <see cref="StartOrder"/>); der Zufallsplatz wird darin
    /// gezogen, der Tausch tauscht die Plätze. Ohne Angabe gilt die Indexfolge.</param>
    /// <exception cref="ArgumentException">Fester Verein außerhalb 0–47 oder von zwei Managern gewählt.</exception>
    public static List<Manager> AssignClubs(IReadOnlyList<Club> clubs, GameSetup setup, Random random,
        List<(int Club, int Partner)>? swaps = null, int[]? places = null)
    {
        places ??= Enumerable.Range(0, SelectableClubs).ToArray();
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

        // [ORIGINAL] Die Start-Liga ist ein Wert für alle Manager (DGROUP 0x11A, L1); die Spielvorbereitung setzt sie über
        // GameSetup.StartLeague für alle gleich. Gelesen wird sie je Manager, damit Aufbauten im Code (Tests, ältere
        // Spielstände) Manager verschiedener Ligen anlegen können; bei gleicher Liga ist das Ergebnis dasselbe.
        var earlier = new HashSet<int>();
        foreach (var m in setup.Managers)
        {
            if (m.ClubIndex is not { } chosen)
                continue;
            if (PlaceInStartLeague(clubs, chosen, m.StartLeague, earlier, random, places) is { } partner && partner != chosen)
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
            // 0x2964C); das Remake zieht aus dem Drittel der Start-Liga ohne den Verein auf Platz 0 – wie der Filter
            // 0x45CD0(r, 1) beim Spielstart (leere Tabelle DGROUP 0x5CFB, U15-c in re-runde-14.md). Kein Platztausch.
            var excluded = new HashSet<int>(reserved);
            if (m.StartLeague == League.Bundesliga)
                excluded.Add(places[0]);
            assigned[i] = PickRandomClub(clubs, m.StartLeague, m.StartStrength, excluded, random);
            reserved.Add(assigned[i]);
        }

        if (OneSeasonSingleManager(setup) && clubs[assigned[0]].League == League.Bundesliga)
        {
            // U15-c: ein Manager, eine Saison → Managerverein auf Platz 0 (Teamwerte bleiben am Platz).
            var own = Array.IndexOf(places, assigned[0]);
            var partner = places[0];
            if (own > 0)
            {
                (places[own], places[0]) = (partner, assigned[0]);
                swaps?.Add((assigned[0], partner));
            }
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
    /// Der Zufallsplatz r ist ein Satzplatz der Startmischung (<paramref name="places"/>, R-K5): gezogen wird der Platz, nicht der
    /// Vereinsindex; nach dem Tausch stehen v und r auf den Plätzen des anderen.
    /// [ORIGINAL] Europapokal-Zeile 1 beim Spielstart: Die Tabelle DGROUP 0x5CFB ist noch leer (Startwert 0, DGROUP 0x5CF4 = 0
    /// → 64 Einträge 0), gefüllt wird sie erst nach der Spielvorbereitung (0x341AB über Datei 0x43A8C). Der Filter trifft
    /// also genau **Platz 0**, nicht den Deutschen Meister (U15-c, <c>docs/analyse/re-runde-14.md</c>; FILE01/09/10: kein
    /// Managerverein auf Platz 0). [APPROX] Zufallsquelle: <see cref="GameState.Random"/> statt des gemeinsamen Zufalls.
    /// </summary>
    /// <returns>Tauschpartner (auch der Verein selbst bei r = v) oder null, wenn der Verein bleibt.</returns>
    internal static int? PlaceInStartLeague(IReadOnlyList<Club> clubs, int club, League league,
        IReadOnlySet<int> earlierManagers, Random random, int[] places)
    {
        var own = clubs[club];
        var ownPlace = Array.IndexOf(places, club);
        if (league != League.Bundesliga && own.League == league && ownPlace <= LastLeagueSlot)
            return null;
        var first = (int)league * LeagueSize;
        bool Excluded(int slot) => earlierManagers.Contains(places[slot]) || slot == 0;
        if (Enumerable.Range(first, LeagueSize).All(Excluded))
            return null;
        int partnerPlace;
        do
            partnerPlace = first + random.Next(LeagueSize);
        while (Excluded(partnerPlace));
        var partner = places[partnerPlace];
        if (partner == club)
            return partner;
        var other = clubs[partner];
        (own.League, other.League) = (other.League, own.League);
        (places[ownPlace], places[partnerPlace]) = (partner, club);
        return partner;
    }

    /// <summary>
    /// Sonderfall „eine Saison, ein Manager“ (U15-c). [ORIGINAL] em.exe Datei 0x29CFA–0x29D9C: neues Spiel (<c>[bp−0x64]</c> = 0),
    /// Spielende DGROUP 0x5A14 = 1994 und DGROUP 0x386 = 1 Manager → Managerfeld DGROUP 0x9744 + 0x1AF · m = 1, dann Zufallsplatz r
    /// der Bundesliga, bis 0x45CD0(r, 1) ≠ 0, und Tausch 0x36169(v, r, 0x80). Beim Spielstart ist die Europapokal-Tabelle leer,
    /// der Filter trifft nur Platz 0: Der Managerverein landet immer auf Platz 0 (FILE29: ESV KAUFBEUREN auf Platz 0,
    /// HOK 0x00F70 = 1; <c>docs/analyse/re-runde-14.md</c>, U15-c). [APPROX] Das Remake tauscht direkt, ohne die Zufallszüge der
    /// Schleife; das Feld 0x9744 bildet es nicht ab (Wirkung außer im Highscore nicht gelesen, <see cref="HighscoreRules"/>).
    /// </summary>
    internal static bool OneSeasonSingleManager(GameSetup setup) =>
        setup.Managers.Count == 1 && setup.Duration == GameDuration.EineSaison;

    /// <summary>
    /// Satzfolge einer Liga beim Spielstart: Startmischung mit den Plätzen nach dem Managertausch (wie
    /// <see cref="GameState.InitializeLeagueSlots"/>). Passt die Folge nicht zu den Mitgliedern (synthetische Aufbauten), gilt
    /// die Indexfolge.
    /// </summary>
    private static IReadOnlyList<int> StartSlots(IReadOnlyList<Club> clubs, int[] places, League league)
    {
        var members = clubs.Where(c => c.League == league).Select(c => c.Index).Order().ToList();
        var segment = places.Skip((int)league * LeagueSize).Take(LeagueSize).ToList();
        return segment.Order().SequenceEqual(members) ? segment : members;
    }

    /// <summary>Plätze je Liga. [ORIGINAL] DGROUP 0x7B1 + Liga = 12.</summary>
    private const int LeagueSize = 12;

    /// <summary>
    /// Satzfolge Platz → Verein beim Programmstart (R-K5, #549): Ligamischung 0x35EDD vor der Spielvorbereitung, je Liga 0–2
    /// 55 Tausche zweier Plätze random(b, b + 11) (auch derselbe Platz); Plätze 36–47 (ohne Liga) bleiben in Datenbankfolge.
    /// Die Vereinsauswahl zeigt auf Seite p die Plätze 12 · p … 12 · p + 11 dieser Folge (Datei 0x28030), der Spielstart
    /// übernimmt sie als Satzfolge der Ligen (<see cref="GameState.LeagueSlots"/>).
    /// [ORIGINAL] em.exe Datei 0x43A17 (Aufruf 0x35EDD) vor Datei 0x43A61 (Spielvorbereitung 0x28165), danach keine weitere
    /// Mischung bis zum Hauptmenü (Datei 0x43A66–0x43B40); Belege <c>oberflaeche_0128/_0133/_0136</c>, FILE01/10/27/28
    /// (<c>docs/analyse/re-runde-13.md</c>, R-K5); Tauschschleife Datei 0x35FD2–0x3613F.
    /// [APPROX] Zufallsquelle: Das Original zieht nach srand(time) (Datei 0x439F5) aus dem gemeinsamen Zufall; das Remake
    /// leitet die Mischung aus dem Spielstart-Seed ab, ohne <see cref="GameState.Random"/> weiterzuschalten. Dieselbe Saat
    /// gibt in der Spielvorbereitung und beim Spielstart dieselbe Folge (der Bildschirm würfelt nichts).
    /// </summary>
    public static int[] StartOrder(int seed)
    {
        var places = Enumerable.Range(0, SelectableClubs).ToArray();
        var random = new GameRandom(unchecked(seed * 7919 + 0x35EDD) & int.MaxValue);
        for (var league = 0; league < 3; league++)
        {
            var b = league * LeagueSize;
            for (var n = 0; n < GameState.LeagueSlotSwaps; n++)
            {
                var x = b + random.Next(LeagueSize);
                var y = b + random.Next(LeagueSize);
                (places[x], places[y]) = (places[y], places[x]);
            }
        }
        return places;
    }

    /// <summary>
    /// Liga eines Vereins laut Datenbank (Index 0–11 Bundesliga, 12–23 2. Liga, 24–35 Oberliga, sonst ohne Liga) – die
    /// Start-Liga, in der ein fest gewählter Verein der 2. Liga oder Oberliga bleibt (<see cref="PlaceInStartLeague"/>).
    /// [ORIGINAL] DGROUP 0x7C0 (erster Platz je Liga 0/12/24) und 0x7B1 (12 Plätze je Liga), Datei 0x29C78.
    /// </summary>
    public static League? DatabaseLeague(int clubIndex) => clubIndex switch
    {
        < 0 => null,
        < 12 => League.Bundesliga,
        < 24 => League.ZweiteLiga,
        <= LastLeagueSlot => League.Oberliga,
        _ => null,
    };

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

    /// <summary>
    /// Kopie eines Vereins mit anderer Stärke (Teamwerte bleiben beim Platztausch am Platz, U15-a). Zu diesem Zeitpunkt trägt
    /// der Verein nur die Felder aus <see cref="Create"/> (Index, Name, Stärke, Logo, Liga, Datenbankspieler).
    /// </summary>
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

    /// <summary>
    /// Liga-Spielplan über die Satzfolge (A113). [ORIGINAL] em.exe Datei 0x4E7C7 schreibt die Paarungen als Satzplatz
    /// <c>Buchstabe − 'a' + Basis</c> (DGROUP 0x5D7B/0x5D7C); Buchstabe a … l ist also der Platz des Vereins in der Satzfolge
    /// der Liga (<see cref="GameState.LeagueSlots"/>: Startmischung Datei 0x43A17, Mischung im Saisonwechsel Datei 0x5644E,
    /// Tausch 0x36169), nicht der Rang seines Vereinsindex. Aufrufer mit der Liga-Tabelle DGROUP 0x2DC: Datei 0x05CA0,
    /// 0x36B5D, 0x44E22, 0x4E8A3, 0x50B45 (je Spieltag neu berechnet; die Satzfolge ändert sich nur beim Spielstart und im
    /// Saisonwechsel, das Remake legt den Plan deshalb dann an). Andere Ligagrößen gibt es im Original nicht (DGROUP 0x7B1
    /// + Liga = 12); für sie gilt wie bisher <see cref="Rounds"/>.
    /// </summary>
    /// <param name="slots">Vereine der Liga in Satzfolge (Platz 0 = Buchstabe a).</param>
    public static IReadOnlyList<MatchDay> ForSlots(IReadOnlyList<int> slots, int rounds)
    {
        if (slots.Count != 12 || rounds % 2 != 0)
            return Rounds(slots, rounds);
        return OriginalRounds(slots, rounds);
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
