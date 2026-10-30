using EishockeyManager.Formats;

namespace EishockeyManager.Core;

/// <summary>
/// Erzeugt zu Spielbeginn die Kader aller Vereine mit Spielerwerten, Verträgen und Auto-Aufstellung.
/// </summary>
/// <remarks>
/// Das Original verwaltet nur die Kader der menschlichen Manager und den Transfermarkt im Detail;
/// Computervereine spielen dort über Teamwerte. Das Remake erzeugt für alle Vereine vollständige Kader
/// (Namen aus der Datenbank) und leitet die Spielerwerte aus der Vereinsstärke ab.
/// </remarks>
public static class SquadSetup
{
    /// <summary>
    /// Kadergröße der Manager-Vereine je Spiel-Level (Torhüter, Verteidiger, Stürmer).
    /// [ORIGINAL] Tabelle DGROUP 0x169: 2/10/13, 2/9/11, 2/8/10. Level 3 = 2/8/10 ist im Video belegt.
    /// [APPROX] Level 0 nutzt wie Level 1 den größten Kader.
    /// </summary>
    public static (int Goalkeepers, int Defenders, int Forwards) ManagedSquadSize(int level) => level switch
    {
        <= 1 => (2, 10, 13),
        2 => (2, 9, 11),
        _ => (2, 8, 10),
    };

    /// <summary>Kaderspieler sind 19–31 Jahre alt. [ORIGINAL] 66C3:0730</summary>
    public const int MinAge = 19, MaxAge = 31;

    /// <summary>Vertragslaufzeit zu Spielbeginn: im Video bei allen Spielern „2 Jahre“.</summary>
    public const int StartContractYears = 2;

    /// <summary>
    /// Basiswert für Kondition und Technik der Computervereine (und Talente) aus der Vereinsstärke.
    /// [APPROX] Das Original führt Computervereine nur über Teamwerte = Datenbankwert (3810:4D3B); das Remake
    /// staucht die Skala, weil die Spielstärke ohnehin aus der Datenbank kommt (<see cref="TeamStrength"/>).
    /// Manager-Vereine nutzen stattdessen <see cref="StartBase"/>.
    /// </summary>
    public static int BaseValue(int clubStrength) => 20 + clubStrength / 2;

    /// <summary>
    /// Basis für Kondition und Technik der Kaderspieler eines Manager-Vereins zu Spielbeginn.
    /// [ORIGINAL] Tabelle DGROUP 0x4B42 (gelesen in em.exe Datei 0x29DA4), Index Start-Liga · 3 + Start-Stärke
    /// (Setup-Werte DGROUP 0x11A/0x11B). Der Spiel-Level und die Datenbankstärke des Vereins wirken nicht.
    /// </summary>
    private static readonly int[,] StartBaseTable =
    {
        { 87, 81, 70 }, // Bundesliga: Oberes Drittel, Mittelfeld, Abstiegskampf
        { 68, 55, 42 }, // 2. Liga
        { 39, 31, 21 }, // Oberliga
    };

    /// <summary>Basis der Startwerte nach Liga und Start-Stärke, siehe <see cref="StartBaseTable"/>.</summary>
    public static int StartBase(League league, StartStrength strength) => StartBaseTable[(int)league, (int)strength];

    /// <summary>Erzeugt die Kader aller Vereine. Nutzt einen eigenen Zufallsgenerator, damit der Spielablauf unverändert bleibt.</summary>
    public static void Initialize(GameState game, int seed)
    {
        var random = new Random(unchecked(seed * 7919 + 17));
        var usedNames = new HashSet<string>(game.Clubs.SelectMany(c => c.Players).Select(p => p.Name));
        var namePool = usedNames.ToList();
        var managed = game.Managers.GroupBy(m => m.ClubIndex).ToDictionary(g => g.Key, g => g.First().Level);
        var startBase = StartBases(game);

        foreach (var club in game.Clubs)
        {
            if (club.Players.Count == 0 && club.League is null && !managed.ContainsKey(club.Index))
                continue; // Europapokalgegner und leere Einträge spielen ohne Kader

            if (managed.TryGetValue(club.Index, out var level))
                BuildManagedSquad(club, ManagedSquadSize(level), namePool, usedNames, random);
            else
                BuildComputerSquad(club, namePool, usedNames, random);

            foreach (var p in club.Players)
                p.Number = 0;
            foreach (var p in club.Players)
                p.Number = PlayerFactory.FreeNumber(club, p); // 1, 2, 3 … in Kaderreihenfolge
            RollValues(club, startBase.TryGetValue(club.Index, out var b) ? b : null, random);
            club.Lineup.AutoFill(club.Players);
            club.ReferenceRating = TeamStrength.Rating(club.Lineup);
        }

        game.TransferMarket.Initialize(game, random);
    }

    /// <summary>
    /// Kader eines Manager-Vereins: die Datenbankspieler der Reihe nach, die ersten als Torhüter,
    /// dann Verteidiger, dann Stürmer. [ORIGINAL] Im Video werden bei 2/8/10 die Datenbank-Verteidiger
    /// auf Platz 11 und 12 (LANGHOFF, RIEMER) zu Stürmern.
    /// </summary>
    private static void BuildManagedSquad(Club club, (int G, int D, int F) size, List<string> pool,
        HashSet<string> used, Random random)
    {
        var goalies = club.Players.Where(p => p.Role == PlayerRole.Goalkeeper).Select(p => p.Name).ToList();
        var skaters = club.Players.Where(p => p.Role != PlayerRole.Goalkeeper).Select(p => p.Name).ToList();
        var foreigners = club.Players.Where(p => p.IsForeigner).Select(p => p.Name).ToHashSet();
        club.Players.Clear();

        for (var i = 0; i < size.G; i++)
            club.Players.Add(NewPlayer(Take(goalies, i), PlayerRole.Goalkeeper));
        for (var i = 0; i < size.D; i++)
            club.Players.Add(NewPlayer(Take(skaters, i), PlayerRole.Defender));
        for (var i = 0; i < size.F; i++)
            club.Players.Add(NewPlayer(Take(skaters, size.D + i), PlayerRole.Forward));

        string Take(List<string> names, int index) => index < names.Count ? names[index] : RandomName(pool, used, random);
        Player NewPlayer(string name, PlayerRole role) => new() { Name = name, Role = role, IsForeigner = foreigners.Contains(name) };
    }

    /// <summary>Computerverein: alle Datenbankspieler; fehlende Torhüter, Verteidiger, Stürmer werden ergänzt.</summary>
    private static void BuildComputerSquad(Club club, List<string> pool, HashSet<string> used, Random random)
    {
        var (g, d, f) = ManagedSquadSize(0);
        Fill(PlayerRole.Goalkeeper, g);
        Fill(PlayerRole.Defender, Math.Min(d, 8));
        Fill(PlayerRole.Forward, Math.Min(f, 10));

        void Fill(PlayerRole role, int minimum)
        {
            while (club.Players.Count(p => p.Role == role) < minimum)
                club.Players.Add(new Player { Name = RandomName(pool, used, random), Role = role });
        }

        var sorted = club.Players.OrderBy(p => p.Role).ToList();
        club.Players.Clear();
        club.Players.AddRange(sorted);
    }

    /// <summary>
    /// Freier Platz bekommt einen zufälligen, noch nicht vergebenen Datenbanknamen. [ORIGINAL] 66C3:0280.
    /// Sind alle vergeben (z. B. Testdatenbank ohne Spieler), entsteht „SPIELER n“.
    /// </summary>
    private static string RandomName(List<string> pool, HashSet<string> used, Random random)
    {
        var free = pool.Where(n => !used.Contains(n)).ToList();
        var name = free.Count > 0 ? free[random.Next(free.Count)] : $"SPIELER {used.Count + 1}";
        used.Add(name);
        return name;
    }

    /// <summary>
    /// Basis der Startwerte je Manager-Verein: Liga des Vereins bei Spielbeginn und Start-Stärke des Managers
    /// (<see cref="StartBase"/>). [APPROX] Das Original kennt nur eine Start-Liga und eine Start-Stärke für alle
    /// Manager und setzt den Verein in die Start-Liga; das Remake führt beides je Manager und lässt einen fest
    /// gewählten Verein in seiner Liga, daher zählt die Liga des Vereins (ligalose Vereine sind dann schon Oberliga).
    /// </summary>
    private static Dictionary<int, int> StartBases(GameState game)
    {
        var result = new Dictionary<int, int>();
        for (var i = 0; i < game.Managers.Count; i++)
        {
            var club = game.Clubs[game.Managers[i].ClubIndex];
            var setup = i < game.Setup.Managers.Count ? game.Setup.Managers[i] : new ManagerSetup();
            result.TryAdd(club.Index, StartBase(club.League ?? setup.StartLeague, setup.StartStrength));
        }
        return result;
    }

    /// <summary>
    /// Spielerwerte. Computervereine: [ORIGINAL] Teamwerte = Basis ± random(0, Basis/10) je Abwehr/Sturm
    /// (3810:4D3B), Basis [APPROX] aus <see cref="BaseValue"/>. Manager-Vereine: [ORIGINAL] em.exe Datei 0x29DA4 ff.,
    /// jeder Kaderspieler random(Basis − 2, Basis + 2) für Technik und Kondition (Torhüter eingeschlossen),
    /// Form random(40, 60), Basis aus <see cref="StartBase"/>. Die Werte je Spieler legt
    /// <see cref="PlayerFactory.RollSquadValues"/> fest (±2 um den übergebenen Teamwert, Form 40–60).
    /// </summary>
    private static void RollValues(Club club, int? managedBase, Random random)
    {
        var b = BaseValue(club.Strength);
        int Team() => b + Between(random, -b / 10, b / 10);
        var condition = new Dictionary<PlayerRole, int>();
        var technique = new Dictionary<PlayerRole, int>();
        // Die Teamwerte werden auch für Manager-Vereine gezogen (und dort verworfen), damit die Zufallsfolge für
        // alle übrigen Vereine und den Transfermarkt unverändert bleibt.
        foreach (var role in Enum.GetValues<PlayerRole>())
        {
            condition[role] = Team();
            technique[role] = Team();
        }

        foreach (var p in club.Players)
        {
            if (managedBase is { } start)
                PlayerFactory.RollSquadValues(p, start, start, random);
            else
                PlayerFactory.RollSquadValues(p, condition[p.Role], technique[p.Role], random);
        }
    }

    /// <summary>random(a, b) wie im Original: beide Grenzen inklusive. [ORIGINAL] 1058:0647</summary>
    public static int Between(Random random, int min, int max) => random.Next(min, max + 1);
}
