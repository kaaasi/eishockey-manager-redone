using System.Runtime.CompilerServices;
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
    /// Kadergröße der Manager-Vereine je Start-Liga (Torhüter, Verteidiger, Stürmer).
    /// [ORIGINAL] em.exe Datei 0x29A3F: Tabelle DGROUP 0x169 (Datei 0x16F39: 2/10/13/0, 2/9/11/0, 2/8/10/0) wird mit
    /// <c>[0x11A] · 4 + Gruppe</c> gelesen, 0x11A = Start-Liga; der Spiel-Level wirkt nicht (Inventur A170).
    /// Beleg: FILE01/FILE09/FILE10 (Bundesliga) 25 Plätze, FILE12 und folge01 (Oberliga) 20.
    /// </summary>
    public static (int Goalkeepers, int Defenders, int Forwards) ManagedSquadSize(League startLeague) => startLeague switch
    {
        League.Bundesliga => (2, 10, 13),
        League.ZweiteLiga => (2, 9, 11),
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

    /// <summary>
    /// Kader aller Vereine für den Import eines Originalspielstands (<paramref name="forImport"/>; der Import ersetzt Pool und
    /// Managerkader danach aus dem Spielstand) und für Aufbauten ohne <see cref="NewGame.Create"/>. [APPROX] Werte und Markt
    /// kommen hier aus einer eigenen Quelle aus der Saat; der Spielstart selbst zieht in der Reihenfolge des Originals aus dem
    /// gemeinsamen Strom (<see cref="BuildStartPool"/>, <see cref="FinishStartSquads"/>; Spur zufallsstrom, Teil 3).
    /// Ergänzte Plätze ziehen beim Import keine Spieler aus Computervereinen (Platzhalter, wie vor #736).
    /// </summary>
    public static void Initialize(GameState game, int seed, bool forImport = false)
    {
        var random = ComputerSquadSource(seed);
        // Spur transfer-vertraege-komplett, Teil 5: Spielerpool vor den Kadern; seine Datenbankspieler fehlen dann den
        // Computervereinen (jeder Name nur einmal, Entscheidung Nutzer 01.11.2026). Spur zufallsstrom (A212): gemeinsamer Strom.
        game.TransferMarket.ReservePool(game, managerSlots: !forImport); // Spur pool (#743): Import ohne Managerplätze
        var startLeague = StartLeagues(game);
        var startBase = startLeague.ToDictionary(e => e.Key, e => StartBase(e.Value.League, e.Value.Strength));
        var names = new NameDraw(game);
        foreach (var club in game.Clubs.Where(c => startLeague.ContainsKey(c.Index)))
            BuildManagedSquad(club, ManagedSquadSize(startLeague[club.Index].League), names.Placeholder, null);

        foreach (var club in game.Clubs)
        {
            if (club.Players.Count == 0 && club.League is null && !startLeague.ContainsKey(club.Index))
                continue; // Europapokalgegner und leere Einträge spielen ohne Kader

            if (!startLeague.ContainsKey(club.Index))
                BuildComputerSquad(club, names);

            club.RenumberPlayers(); // A091: Nr. = Kaderplatz + 1
            RollValues(club, startBase.TryGetValue(club.Index, out var b) ? b : null, random);
            club.Lineup.AutoFill(club.Players);
            club.ReferenceRating = TeamStrength.Rating(club.Lineup);
        }

        game.TransferMarket.Initialize(game, random);
    }

    /// <summary>
    /// [APPROX] Eigene Zufallsquelle der Computerkader (aus der Saat abgeleitet): Im Original hat ein Computerverein keine
    /// Spieler, ihre Werte haben kein Gegenstück im gemeinsamen Strom und würden dessen Folge gegenüber dem Original verschieben.
    /// </summary>
    private static Random ComputerSquadSource(int seed) => new(unchecked(seed * 7919 + 17));

    /// <summary>
    /// Spielstart, Schritt „Pool“ (Spur zufallsstrom, Teil 3): Poolaufbau mit den Kaderplätzen der Manager aus dem gemeinsamen
    /// Strom (<see cref="TransferMarket.ReserveStartPool"/>; [ORIGINAL] Datei 0x29896 → 0x5AF50, Kaderplätze Datei 0x5B0BB–0x5B2BE).
    /// </summary>
    /// <returns>Namen, die der Poolaufbau außerhalb des Pools vergeben hat (für <see cref="TransferMarket.RenameStartNationals"/>).</returns>
    internal static HashSet<string> BuildStartPool(GameState game) => game.TransferMarket.ReserveStartPool(game);

    /// <summary>
    /// Spielstart nach der Spielvorbereitung: Kader der Computervereine, Rückennummern, Aufstellung und Bezugsstärke aller
    /// Vereine, Startkader in ihre Poolplätze. Verbraucht den gemeinsamen Strom nicht. [APPROX] Die Werte der Computerkader
    /// kommen aus <see cref="ComputerSquadSource"/> (ohne Gegenstück im Original); Stärke und Liga gelten nach dem Platztausch.
    /// </summary>
    internal static void FinishStartSquads(GameState game, int seed)
    {
        var random = ComputerSquadSource(seed);
        var managed = StartLeagues(game);
        var names = new NameDraw(game);
        foreach (var club in game.Clubs)
        {
            var own = managed.TryGetValue(club.Index, out var start);
            if (club.Players.Count == 0 && club.League is null && !own)
                continue; // Europapokalgegner und leere Einträge spielen ohne Kader

            if (!own)
                BuildComputerSquad(club, names);
            else if (game.WorldOnly) // „nur Weltkugel“: kein Poolplatz der Manager (BuildPool), Kader wie bisher aus der eigenen Quelle
                BuildManagedSquad(club, ManagedSquadSize(start.League), names.Placeholder, null);
            club.RenumberPlayers(); // A091: Nr. = Kaderplatz + 1
            if (!own)
                RollValues(club, null, random);
            else if (game.WorldOnly)
                RollValues(club, StartBase(start.League, start.Strength), random);
            club.Lineup.AutoFill(club.Players);
            club.ReferenceRating = TeamStrength.Rating(club.Lineup);
        }
        game.TransferMarket.SeatManagerSquads(game);
    }

    /// <summary>
    /// Kader eines Manager-Vereins. [ORIGINAL] Datei 0x5B0CC–0x5B2BE: je Art (Torhüter, Verteidiger, Stürmer) so viele Plätze wie
    /// <see cref="ManagedSquadSize"/>, Name vom Datenbankplatz des Vereins (0x5AA34), der Datenbankplatz läuft über alle Arten weiter
    /// (bei 2/8/10 werden die Datenbank-Verteidiger auf Platz 11 und 12, im Video LANGHOFF und RIEMER, zu Stürmern); ein leerer
    /// Datenbankplatz bekommt einen Namen der Art aus einem anderen Verein (0x5AAA0, <see cref="NameDraw"/>; FILE01 KÖLNER EC:
    /// Platz 12 UVIRA, Platz 25 LALA). A394 (Spur kader-regeln): [ORIGINAL] ein leerer Datenbankplatz bleibt an seiner Stelle
    /// (kb\23.vga: EC HARZ BRAUNLAGE Platz 8, EHC STRAUBING Platz 10, 1.EV WEIDEN Platz 8 leer, dahinter Verteidiger), der Platz
    /// kommt aus <see cref="FromDatabase"/>. [APPROX] Spieler ohne gemerkten Platz (Aufbauten ohne <see cref="NewGame.Create"/>,
    /// Import) zählen wie bisher ab Anfang der Art.
    /// </summary>
    /// <param name="name">Name für einen leeren Datenbankplatz der Art.</param>
    /// <param name="seated">Je angelegtem Kaderplatz in Platzfolge (Würfe des Kaderplatzes beim Spielstart).</param>
    internal static void BuildManagedSquad(Club club, (int G, int D, int F) size, Func<PlayerRole, Player> name, Action<Player>? seated)
    {
        var database = new Dictionary<int, Player>();
        foreach (var role in Enum.GetValues<PlayerRole>())
        {
            var (first, end) = DatabaseSlots(role);
            var players = club.Players.Where(p => p.Role == role).ToList();
            for (var j = 0; j < players.Count; j++)
            {
                var at = DatabaseSlotOf(players[j]) ?? first + j;
                if (at >= first && at < end)
                    database.TryAdd(at, players[j]);
            }
        }
        club.Players.Clear();

        var slot = 0;
        Add(PlayerRole.Goalkeeper, size.G);
        Add(PlayerRole.Defender, size.D);
        Add(PlayerRole.Forward, size.F);

        void Add(PlayerRole role, int count)
        {
            for (var i = 0; i < count; i++, slot++)
            {
                var player = database.TryGetValue(slot, out var p)
                    ? new Player { Name = p.Name, Role = role, IsForeigner = p.IsForeigner }
                    : name(role);
                club.Players.Add(player);
                seated?.Invoke(player);
            }
        }
    }

    /// <summary>
    /// Kaderspieler aus der Datenbank; merkt den Datenbankplatz für <see cref="BuildManagedSquad"/> (A394). Der Platz ist kein
    /// Spielzustand: Er gilt nur für die Spieler dieses Aufbaus und wird nicht gespeichert.
    /// </summary>
    internal static Player FromDatabase(DatabasePlayer source)
    {
        var p = new Player { Name = source.Name, Role = source.Role, IsForeigner = source.IsForeigner };
        if (source.Slot >= 0)
            DatabaseSlotTable.AddOrUpdate(p, new StrongBox<int>(source.Slot));
        return p;
    }

    private static readonly ConditionalWeakTable<Player, StrongBox<int>> DatabaseSlotTable = new();

    /// <summary>Datenbankplatz eines Spielers aus <see cref="FromDatabase"/>, sonst null.</summary>
    internal static int? DatabaseSlotOf(Player p) => DatabaseSlotTable.TryGetValue(p, out var slot) ? slot.Value : null;

    /// <summary>Kaderplätze der Datenbank je Art: Torhüter 0–1, Verteidiger 2–11, Stürmer 12–24. [ORIGINAL] DGROUP 0x164.</summary>
    private static (int First, int End) DatabaseSlots(PlayerRole role) => role switch
    {
        PlayerRole.Goalkeeper => (0, 2),
        PlayerRole.Defender => (2, 12),
        _ => (12, ClubDatabase.SquadSize),
    };

    /// <summary>
    /// Computerverein: alle Datenbankspieler; fehlende Torhüter, Verteidiger, Stürmer werden ergänzt. [APPROX] Im Original hat ein
    /// Computerverein keine Spieler; der Pool nimmt den Vereinen 0–11 die meisten Datenbankspieler (jeder Name nur einmal), daher
    /// heißen die Ergänzungen „SPIELER n“ (<see cref="NameDraw.Placeholder"/>).
    /// </summary>
    private static void BuildComputerSquad(Club club, NameDraw names)
    {
        var (g, d, f) = ManagedSquadSize(League.Bundesliga);
        Fill(PlayerRole.Goalkeeper, g);
        Fill(PlayerRole.Defender, Math.Min(d, 8));
        Fill(PlayerRole.Forward, Math.Min(f, 10));

        void Fill(PlayerRole role, int minimum)
        {
            while (club.Players.Count(p => p.Role == role) < minimum)
                club.Players.Add(names.Placeholder(role));
        }

        var sorted = club.Players.OrderBy(p => p.Role).ToList();
        club.Players.Clear();
        club.Players.AddRange(sorted);
    }

    /// <summary>
    /// Platzhalter „SPIELER n“ für ergänzte Kaderplätze. [APPROX] Im Original haben Computervereine keine Spieler; die Namen
    /// leerer Datenbankplätze der Managerkader zieht der Poolaufbau (<see cref="TransferMarket.ReserveStartPool"/>, Datei 0x5AAA0).
    /// </summary>
    internal sealed class NameDraw(GameState game)
    {
        private int _placeholders;

        /// <summary>Platzhalter „SPIELER n“ mit einer im Spiel noch freien Nummer.</summary>
        public Player Placeholder(PlayerRole role)
        {
            var used = game.Clubs.SelectMany(c => c.Players).Select(p => p.Name).ToHashSet();
            string name;
            do
                name = $"SPIELER {++_placeholders}";
            while (used.Contains(name));
            return new Player { Name = name, Role = role };
        }
    }

    /// <summary>
    /// Start-Liga und Start-Stärke je Manager-Verein für Kadergröße (<see cref="ManagedSquadSize"/>) und Startwerte
    /// (<see cref="StartBase"/>). [APPROX] Das Original kennt nur eine Start-Liga und eine Start-Stärke für alle
    /// Manager und setzt den Verein in die Start-Liga; das Remake führt beides je Manager und lässt einen fest
    /// gewählten Verein in seiner Liga, daher zählt die Liga des Vereins (ligalose Vereine sind dann schon Oberliga).
    /// </summary>
    private static Dictionary<int, (League League, StartStrength Strength)> StartLeagues(GameState game)
    {
        var result = new Dictionary<int, (League, StartStrength)>();
        for (var i = 0; i < game.Managers.Count; i++)
        {
            var club = game.Clubs[game.Managers[i].ClubIndex];
            var setup = i < game.Setup.Managers.Count ? game.Setup.Managers[i] : new ManagerSetup();
            result.TryAdd(club.Index, (club.League ?? setup.StartLeague, setup.StartStrength));
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
                PlayerFactory.RollSquadValues(p, start, start, random, managed: true);
            else
                PlayerFactory.RollSquadValues(p, condition[p.Role], technique[p.Role], random);
        }
    }

    /// <summary>random(a, b) wie im Original: beide Grenzen inklusive. [ORIGINAL] 1058:0647</summary>
    public static int Between(Random random, int min, int max) => random.Next(min, max + 1);
}
