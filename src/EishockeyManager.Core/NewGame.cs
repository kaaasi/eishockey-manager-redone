using EishockeyManager.Formats;

namespace EishockeyManager.Core;

/// <summary>Erzeugt einen neuen Spielstand aus Datenbank und Spielvorbereitung.</summary>
public static class NewGame
{
    /// <summary>Vereine 0–47 können in der Spielvorbereitung gewählt werden (4 Seiten à 12).</summary>
    public const int SelectableClubs = 48;

    public static GameState Create(ClubDatabase database, GameSetup setup, int seed) => Create(database, setup, seed, null);

    /// <summary>
    /// Spielstart in der Reihenfolge des Originals (Spur zufallsstrom, Teil 3; N3 = B) – alle Würfe mit Gegenstück im Original aus
    /// dem gemeinsamen Strom <see cref="GameState.Random"/>:
    /// <list type="number">
    /// <item>[ORIGINAL] Lader 0x2EF1B (Datei 0x439BB), Ligamischung (Datei 0x43A17, <see cref="StartOrder"/>).</item>
    /// <item>[ORIGINAL] Spielvorbereitung 0x28165, neues Spiel: Poolaufbau 0x5AF50 (Aufruf Datei 0x29896) samt den Kaderplätzen der
    ///   Manager (Datei 0x5B0BB–0x5B2BE; <see cref="TransferMarket.ReserveStartPool"/>).</item>
    /// <item>[ORIGINAL] Managerschleife Datei 0x29B57–0x2A0A3, je Manager: Platz in der Start-Liga (Wurf Datei 0x29C91, Tausch
    ///   0x29CF2; A558), Sonderfall „eine Saison, ein Manager“ (Datei 0x29CFA–0x29D9C), Startwerte, Preis und Gehalt je Kaderplatz
    ///   (Datei 0x29DA4–0x29EF7; <see cref="GameState.RollStartValues"/>).</item>
    /// <item>[ORIGINAL] Nationalspieler des Pools benennen (Datei 0x2A0B5 → 0x5ACA0).</item>
    /// <item>[ORIGINAL] Nach der Spielvorbereitung: Wanderung (Datei 0x43A73), Pool auf die Ligen verteilen (Datei 0x43A83 →
    ///   0x057E3), erster Listenwechsel (Datei 0x43A9F → 0x494EF; <see cref="TransferMarket.OpenStartMarket"/>).</item>
    /// </list>
    /// [APPROX] Ohne Gegenstück im Original und deshalb außerhalb des Stroms: die Kader der Computervereine (eigene Quelle aus der
    /// Saat, <see cref="SquadSetup.FinishStartSquads"/>) und die Ligamischung vor der Spielvorbereitung (aus der Saat abgeleitet, weil
    /// die Vereinsauswahl sie vor dem Spielstart zeigt, <see cref="StartOrder"/>). Die Zufallswahl eines Vereins (ohne Gegenstück)
    /// zieht vor dem Pool aus dem Strom (<see cref="AssignClubs"/>). Teil 4: Europapokal (Datei 0x298B1, nach dem Pool) und
    /// DEB-Pokal (Datei 0x43A8C, zwischen 0x057E3 und 0x494EF) losen an den Stellen des Originals aus dem Strom; die Länderreihenfolge
    /// der WM gehört zur Mischung vor der Spielvorbereitung (<see cref="GameState.InitializeCompetitions"/>). Werbeangebote je
    /// Manager in der Managerschleife (Datei 0x2A03B–0x2A061), 90 Börsentage nach dem ersten Listenwechsel (Datei 0x43AD5).
    /// </summary>
    /// <param name="observer">Nur Tests: wird an den Grenzen der Schritte gerufen (Strom und Zustand dort).</param>
    internal static GameState Create(ClubDatabase database, GameSetup setup, int seed, Action<StartStep, GameState>? observer)
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
        // Spur start-lader (A106): [ORIGINAL] Lader 0x2EF1B beim Programmstart vor der Spielvorbereitung (Datei 0x439BB).
        MatchEngine.LoadTeamValues(clubs, random, database.Clubs
            .Where(c => c.Strengths.Defense == c.Strength && c.Strengths.Attack != 0).ToDictionary(c => c.Index, c => (int)c.Strengths.Attack)); // A105/A836

        var places = StartOrder(seed); // R-K5: Ligamischung vor der Spielvorbereitung (Datei 0x43A17)
        var managers = AssignClubs(clubs, setup, random, places);
        var schedules = new Dictionary<League, IReadOnlyList<MatchDay>>();
        var game = new GameState
        {
            Setup = setup,
            Clubs = clubs,
            Managers = managers,
            Schedules = schedules,
            Random = random,
        };

        observer?.Invoke(StartStep.Pool, game);
        var held = SquadSetup.BuildStartPool(game); // Datei 0x29896: Pool mit den Kaderplätzen der Manager

        observer?.Invoke(StartStep.EuropeanCup, game);
        game.DrawStartEuropeanCup(); // Datei 0x298B1 → 0x3139F(0) → 0x341AB(1, 0): nach dem Pool, vor der Managerschleife

        observer?.Invoke(StartStep.Managers, game);
        // Zufällig zugeteilte Vereine (ohne Gegenstück im Original) verdrängt kein Platztausch.
        var earlier = managers.Where((_, i) => setup.Managers[i].ClubIndex is null).Select(m => m.ClubIndex).ToHashSet();
        for (var i = 0; i < managers.Count; i++)
        {
            var m = setup.Managers[i];
            if (m.ClubIndex is { } chosen)
            {
                // A558 (N3 = B): [ORIGINAL] Platzwahl in der Managerschleife nach dem Pool (Datei 0x29C3B–0x29CF7).
                if (PlaceInStartLeague(clubs, chosen, m.StartLeague, earlier, random, places) is { } partner && partner != chosen)
                    Club.SwapStartSlot(clubs[chosen], clubs[partner]);
                earlier.Add(chosen);
            }
            if (OneSeasonSingleManager(setup) && clubs[managers[i].ClubIndex].League == League.Bundesliga)
            {
                // U15-c: ein Manager, eine Saison → Managerverein auf Platz 0 (Teamwerte bleiben am Platz). [ORIGINAL] Datei
                // 0x29D28–0x29D7F: Zufallsplatz der Bundesliga, neu gezogen, bis 0x45CD0(r, 1) ≠ 0 – also bis Platz 0 fällt.
                while (random.Next(LeagueSize) != 0)
                {
                }
                var own = Array.IndexOf(places, managers[i].ClubIndex);
                var partner = places[0];
                if (own > 0)
                {
                    (places[own], places[0]) = (partner, managers[i].ClubIndex);
                    Club.SwapStartSlot(clubs[managers[i].ClubIndex], clubs[partner]);
                }
            }
            if (!game.WorldOnly)
                game.RollStartValues(managers[i], SquadSetup.StartBase(clubs[managers[i].ClubIndex].League ?? m.StartLeague, m.StartStrength));
            // Teil 4 (A464 Rest): [ORIGINAL] Werbebewertung 0x3F833 (Datei 0x2A03B) und Werbeangebote 0x3FB47 (Datei 0x2A061) je
            // Manager nach Preis und Gehalt, vor den Teamwerten (Datei 0x2A073).
            observer?.Invoke(StartStep.Offers, game);
            game.RenewOffers(managers[i]);
            observer?.Invoke(StartStep.ManagerDone, game);
        }

        observer?.Invoke(StartStep.Nationals, game);
        game.TransferMarket.RenameStartNationals(game, held); // Datei 0x2A0B5 → 0x5ACA0, nach der Managerschleife

        observer?.Invoke(StartStep.Rest, game);
        // Der Plan richtet sich nach der Satzfolge (A113, Schedule.ForSlots); das Original mischt hier nicht.
        foreach (var league in Enum.GetValues<League>())
            schedules[league] = Schedule.ForSlots(StartSlots(clubs, places, league), rounds: 4);

        SquadSetup.FinishStartSquads(game, seed); // Paket „mannschaft“: Computerkader, Rückennummern, Aufstellung
        game.AssignStartPlayerIds(); // Spur „speichern“: dauerhafte Spieler-IDs, verbraucht keinen Zufall
        GameState.s_startDrawsAtOriginalPlace = true;
        try
        {
            game.InitializeManagerSystems();
        }
        finally
        {
            GameState.s_startDrawsAtOriginalPlace = false;
        }
        game.InitializeLeagueSlots(places); // S-K6/R-K5: Satzfolge = Startmischung mit den Plätzen nach dem Managertausch
        observer?.Invoke(StartStep.Drift, game);
        MatchEngine.StartGame(game); // Spur start-lader: Teamwerte der Managervereine (0x2A073), Wanderung des Spielstarts (0x43A73)
        observer?.Invoke(StartStep.Market, game);
        // Datei 0x43A83 (0x057E3), 0x43A8C (0x341AB(0, 0): DEB-Pokal), 0x43A9F (0x494EF)
        game.TransferMarket.OpenStartMarket(game, () =>
        {
            observer?.Invoke(StartStep.DebCup, game);
            game.DrawStartDebCup();
            observer?.Invoke(StartStep.Listing, game);
        });
        observer?.Invoke(StartStep.Stocks, game);
        game.CreateStartStocks(); // Datei 0x43AA4–0x43AE0: Startkurse, 90 Börsentage 0x5784A
        observer?.Invoke(StartStep.Turn, game);
        game.BeginFirstTurn(); // ui-rest Teil 4 (R12U-04): Faktor des ersten Managerzugs (0x44E3F)
        observer?.Invoke(StartStep.Done, game);
        return game;
    }

    /// <summary>Schritte des Spielstarts für den Beobachter von <see cref="Create(ClubDatabase, GameSetup, int, Action{StartStep, GameState}?)"/>.</summary>
    internal enum StartStep
    {
        /// <summary>Vor dem Poolaufbau (Datei 0x29896).</summary>
        Pool,
        /// <summary>Nach dem Poolaufbau, vor der Auslosung des Europapokals (Datei 0x298B1).</summary>
        EuropeanCup,
        /// <summary>Vor der Managerschleife (Datei 0x29B57).</summary>
        Managers,
        /// <summary>Je Manager vor Werbebewertung und Werbeangeboten (Datei 0x2A03B).</summary>
        Offers,
        /// <summary>Je Manager nach den Werbeangeboten (Ende des Schleifendurchlaufs, Datei 0x2A0A3).</summary>
        ManagerDone,
        /// <summary>Vor dem Benennen der Nationalspieler (Datei 0x2A0B5).</summary>
        Nationals,
        /// <summary>Nach der Spielvorbereitung, vor Computerkadern und Wanderung.</summary>
        Rest,
        /// <summary>Vor Teamwerten der Managervereine und Wanderung (Datei 0x43A73).</summary>
        Drift,
        /// <summary>Vor dem Verteilen des Pools (Datei 0x43A83).</summary>
        Market,
        /// <summary>Nach dem Verteilen des Pools, vor der Auslosung des DEB-Pokals (Datei 0x43A8C).</summary>
        DebCup,
        /// <summary>Vor dem ersten Listenwechsel (Datei 0x43A9F).</summary>
        Listing,
        /// <summary>Vor den 90 Börsentagen des Spielstarts (Datei 0x43AD5).</summary>
        Stocks,
        /// <summary>Vor dem Faktor des ersten Managerzugs (Datei 0x44E3F).</summary>
        Turn,
        /// <summary>Spielstart abgeschlossen.</summary>
        Done,
    }

    /// <summary>
    /// Vereinsvergabe beim Spielstart (Befund R2-07, U15-a): prüft die festen Wahlen aller Manager und teilt Managern ohne Wahl
    /// einen Verein zu (unter Ausschluss aller gewählten Vereine). Die Reihenfolge der Manager bleibt erhalten. Den Platz in der
    /// Start-Liga zieht erst die Managerschleife nach dem Pool (<see cref="PlaceInStartLeague"/>, A558).
    /// </summary>
    /// <param name="places">Satzfolge Platz → Verein (48 Einträge, <see cref="StartOrder"/>). Ohne Angabe gilt die Indexfolge.</param>
    /// <exception cref="ArgumentException">Fester Verein außerhalb 0–47 oder von zwei Managern gewählt.</exception>
    public static List<Manager> AssignClubs(IReadOnlyList<Club> clubs, GameSetup setup, Random random, int[]? places = null)
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
            // 0x45CD0(r, 1) beim Spielstart (leere Tabelle DGROUP 0x5CFB, U15-c in re-runde-14.md). Kein Platztausch. Der Wurf
            // kommt vor dem Pool aus dem gemeinsamen Strom (der Pool braucht die Vereine der Manager).
            var excluded = new HashSet<int>(reserved);
            if (m.StartLeague == League.Bundesliga)
                excluded.Add(places[0]);
            assigned[i] = PickRandomClub(clubs, m.StartLeague, m.StartStrength, excluded, random);
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
    /// Der Zufallsplatz r ist ein Satzplatz der Startmischung (<paramref name="places"/>, R-K5): gezogen wird der Platz, nicht der
    /// Vereinsindex; nach dem Tausch stehen v und r auf den Plätzen des anderen.
    /// [ORIGINAL] Europapokal-Zeile 1 beim Spielstart: Die Tabelle DGROUP 0x5CFB ist noch leer (Startwert 0, DGROUP 0x5CF4 = 0
    /// → 64 Einträge 0), gefüllt wird sie erst nach der Spielvorbereitung (0x341AB über Datei 0x43A8C). Der Filter trifft
    /// also genau **Platz 0**, nicht den Deutschen Meister (U15-c, <c>docs/analyse/re-runde-14.md</c>; FILE01/09/10: kein
    /// Managerverein auf Platz 0). Spur zufallsstrom (A558, N3 = B): Der Wurf kommt aus dem gemeinsamen Strom
    /// <see cref="GameState.Random"/> an der Stelle des Originals – in der Managerschleife nach dem Pool (Datei 0x29C91).
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
    /// HOK 0x00F70 = 1; <c>docs/analyse/re-runde-14.md</c>, U15-c). Spur zufallsstrom (N3 = B): Die Würfe der Schleife kommen aus dem
    /// gemeinsamen Strom, bis Platz 0 fällt. [APPROX] Das Feld 0x9744 bildet das Remake nicht ab (Wirkung außer im Highscore nicht
    /// gelesen, <see cref="HighscoreRules"/>); der Sonderfall greift im Remake nur, wenn der Verein in der Bundesliga steht.
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
}

public sealed partial class Club
{
    /// <summary>
    /// Platztausch beim Spielstart (U15-a): Stärke und Teamwerte bleiben am Platz, also tauschen die beiden Vereine sie (Spur
    /// start-lader: auch die Werte des Laders, die beim Tausch 0x36169 schon im Vereinssatz stehen). Name, Logo und Spieler
    /// bleiben beim Verein; die Liga tauscht <see cref="NewGame.PlaceInStartLeague"/>.
    /// </summary>
    internal static void SwapStartSlot(Club a, Club b)
    {
        (StrengthField(a), StrengthField(b)) = (b.Strength, a.Strength);
        (a.TeamValues, b.TeamValues) = (b.TeamValues, a.TeamValues);
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
