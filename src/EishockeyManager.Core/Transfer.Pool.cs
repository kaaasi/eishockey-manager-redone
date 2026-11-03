using EishockeyManager.Formats;

namespace EishockeyManager.Core;

/// <summary>Besitzer eines Platzes im Spielerpool (Pool +0x1A).</summary>
public enum PoolOwner
{
    /// <summary>Frei (Besitzer 6): kann vom Listenwechsel gezogen werden.</summary>
    Free,

    /// <summary>Auf dem Transfermarkt (Besitzer 4, Kaderplatz-Satz 4).</summary>
    Market,

    /// <summary>
    /// Vergeben: gekauft oder geliehen. [APPROX] Das Original führt hier die Nummer des Managers (0–3) und gibt den Platz bei
    /// Vertragsende oder Verkauf wieder frei (Besitzer 6); das Remake behält den Spieler in seinem Kader bzw. bei den
    /// Vertragslosen und erneuert den Platz beim Saisonwechsel (<see cref="TransferMarket.StartPoolSeason"/>).
    /// </summary>
    Taken,
}

/// <summary>Ein Platz im Spielerpool far:8253 (200 × 33 Byte, Index 1–199; Platz 0 ist leer).</summary>
public sealed class PoolSlot
{
    /// <summary>Index im Pool (1–199).</summary>
    public required int Index { get; init; }

    /// <summary>Spieler des Platzes (Name +0x00, Talent +0x12, Alter +0x14, Grundstärken +0x15–0x17, Rollenwert +0x18).</summary>
    public required Player Player { get; set; }

    /// <summary>Verein bzw. Nationalteam (+0x1D), Index in <see cref="GameState.Clubs"/>.</summary>
    public required int Club { get; set; }

    /// <summary>Besitzer (+0x1A).</summary>
    public PoolOwner Owner { get; set; }
}

/// <summary>
/// Aufteilung des Spielerpools nach Index: Ligaspieler (Torhüter, Verteidiger, Stürmer) und Nationalspieler (ebenso).
/// [ORIGINAL] <see cref="Original"/> = 200 Plätze (Index 0 leer): 1–20 Torhüter, 21–96 Verteidiger, 97–179 Stürmer,
/// 180–183 Torhüter, 184–190 Verteidiger, 191–199 Stürmer (Rollenwerte Datei 0x5B391–0x5B4A9, Ziehung 0x495D8, Namen
/// 0x5ACA0 mit Satz DGROUP 0x55F4). Andere Größen (<see cref="For"/>) teilen im selben Verhältnis auf – Vorbereitung für
/// größere Pools (Wunsch Nutzer 01.11.2026, z. B. 400), nicht Teil des Originals.
/// </summary>
public sealed record TransferPoolLayout(
    int Size, int LeagueGoalkeepers, int LeagueDefenders, int LeagueForwards,
    int NationalGoalkeepers, int NationalDefenders, int NationalForwards)
{
    /// <summary>Pool des Originals: 200 × 33 Byte (far:8253).</summary>
    public static readonly TransferPoolLayout Original = new(200, 20, 76, 83, 4, 7, 9);

    /// <summary>Aufteilung für <paramref name="size"/> Plätze (Index 0 leer): 200 wie im Original, sonst im Verhältnis des Originals.</summary>
    public static TransferPoolLayout For(int size)
    {
        if (size == Original.Size)
            return Original;
        if (size < 8)
            throw new ArgumentOutOfRangeException(nameof(size), size, "Der Pool braucht mindestens je einen Platz jeder Art.");
        var factor = (size - 1) / (double)(Original.Size - 1);
        int Part(int n) => Math.Max(1, (int)Math.Round(n * factor));
        var (lg, ld, ng, nd, nf) = (Part(20), Part(76), Part(4), Part(7), Part(9));
        var lf = size - 1 - lg - ld - ng - nd - nf;
        return new TransferPoolLayout(size, lg, ld, Math.Max(1, lf), ng, nd, nf);
    }

    /// <summary>Letzter Index der Ligaspieler (Original 179).</summary>
    public int LeagueLast => LeagueGoalkeepers + LeagueDefenders + LeagueForwards;

    /// <summary>Erster Index der Nationalspieler (Original 180).</summary>
    public int FirstNational => LeagueLast + 1;

    /// <summary>Letzter Index (Original 199).</summary>
    public int Last => Size - 1;

    /// <summary>Art nach Index.</summary>
    public PlayerRole RoleOf(int index)
    {
        if (index <= LeagueGoalkeepers)
            return PlayerRole.Goalkeeper;
        if (index <= LeagueGoalkeepers + LeagueDefenders)
            return PlayerRole.Defender;
        if (index <= LeagueLast)
            return PlayerRole.Forward;
        if (index <= LeagueLast + NationalGoalkeepers)
            return PlayerRole.Goalkeeper;
        if (index <= LeagueLast + NationalGoalkeepers + NationalDefenders)
            return PlayerRole.Defender;
        return PlayerRole.Forward;
    }

    /// <summary>Wechsel der Kaderart beim Benennen der Ligaspieler (Original 21, 97; DGROUP 0x382).</summary>
    public (int Defenders, int Forwards) LeagueRoleStarts => (LeagueGoalkeepers + 1, LeagueGoalkeepers + LeagueDefenders + 1);

    /// <summary>
    /// Nationalspieler beim Benennen: ab dem letzten Ligaindex (Original 179, Satz DGROUP 0x55F4 – der Platz 179 bekommt so
    /// einen Nationalspielernamen), Wechsel der Kaderart bei 184 und 191.
    /// </summary>
    public (int Start, int Defenders, int Forwards) NationalNaming =>
        (LeagueLast, FirstNational + NationalGoalkeepers, FirstNational + NationalGoalkeepers + NationalDefenders);
}

/// <summary>Spieler auf einem Kaderplatz der Datenbank (kb\23), vor dem Aufbau der Kader festgehalten.</summary>
public sealed class PoolDatabaseName
{
    /// <summary>Verein (Index in <see cref="GameState.Clubs"/>).</summary>
    public required int Club { get; init; }

    /// <summary>Kaderplatz 0–24 (0–1 Torhüter, 2–11 Verteidiger, 12–24 Stürmer).</summary>
    public required int Slot { get; init; }

    public required string Name { get; init; }
}

/// <summary>
/// Regeln des Spielerpools und des Listenwechsels nach em.exe (Spur transfer-vertraege-komplett, Teil 5, Entscheidung Nutzer
/// 01.11.2026). Fundstellen in <c>docs/transfer.md</c>, Abschnitt „Teil 5“.
/// </summary>
/// <remarks>
/// Das Original verwaltet nur diese 200 Spieler im Detail (Managerkader und Transfermarkt); Computervereine haben keine
/// Spieler. Der Transfermarkt zieht deshalb aus dem Pool, nicht aus Vereinskadern. Das Remake behält seine Kader der
/// Computervereine (Statistik, Kader-KI), nimmt die Datenbankspieler des Pools aber vor dem Aufbau heraus
/// (<see cref="TransferMarket.ReservePool"/>, Entscheidung Nutzer 01.11.2026): jeder Name steht nur einmal im Spiel.
/// Unterschieden werden Spieler ohnehin über ihre ID (<see cref="Player.Id"/>).
/// </remarks>
public static class TransferPoolRules
{
    /// <summary>Größe des Pools im Original (Index 0 … 199; Index 0 bleibt leer). [ORIGINAL] 200 × 33 Byte, far:8253.</summary>
    public const int Size = 200;

    /// <summary>Kaderplätze des Markt-Satzes, die der Listenwechsel bedient. [ORIGINAL] Datei 0x49516 (Argument 10).</summary>
    public const int MarketSlots = 10;

    /// <summary>Versuche beim Ziehen eines freien Poolspielers. [ORIGINAL] Datei 0x49624 (0x5DC), danach Abbruch.</summary>
    public const int MaxDrawAttempts = 1500;

    /// <summary>Art eines Platzes im Pool des Originals (<see cref="TransferPoolLayout.Original"/>).</summary>
    public static PlayerRole RoleOf(int index) => TransferPoolLayout.Original.RoleOf(index);

    /// <summary>
    /// Startwerte eines Poolplatzes. [ORIGINAL] Datei 0x5AFB2–0x5B0B1 (je Index 1–199 in dieser Reihenfolge): Alter +0x14 =
    /// random(18, 33); b = random(30, 89), unter 45 mit random(0, 1) ≠ 0 neu random(40, 89); +0x16 = b + 10 − random(0, 20);
    /// Form +0x17 = random(45, 55); +0x15 = b + 10 − random(0, 20); +0x19 = 0. Kondition = +0x15, Technik = +0x16
    /// (Kaderplatz +0x10/+0x11, kopiert in 0x42EC9–0x42EEB).
    /// </summary>
    public static void RollStartValues(Player p, Random random)
    {
        p.Age = TransferRules.Random16(random, 18, 33);
        var b = TransferRules.Random16(random, 30, 89);
        if (b < 45 && TransferRules.Random16(random, 0, 1) != 0)
            b = TransferRules.Random16(random, 40, 89);
        p.Technique = (byte)(b + 10 - TransferRules.Random16(random, 0, 20));
        p.Form = TransferRules.Random16(random, 45, 55);
        p.Condition = (byte)(b + 10 - TransferRules.Random16(random, 0, 20));
    }

    /// <summary>
    /// Rollenwert +0x18 (verbraucht nur den Zufall). [ORIGINAL] Datei 0x5B3BB–0x5B49D: Torhüter 0, Verteidiger 25 + random(0, 16),
    /// Stürmer 75 + random(0, 16). Das Remake führt nur die Art (<see cref="RoleOf"/>).
    /// </summary>
    public static void RollRoleValue(TransferPoolLayout layout, int index, Random random)
    {
        if (layout.RoleOf(index) != PlayerRole.Goalkeeper)
            _ = TransferRules.Random16(random, 0, 16);
    }

    /// <summary>
    /// Grundstärken beim Listenwechsel aus den Teamwerten des Vereins. [ORIGINAL] Datei 0x4981E–0x4991C (Ov 0x118):
    /// t = 1 bei einem Nationalteam (0x128:0x20 = 0x4D2F0, Index 52 … 63), sonst 0; Kondition +0x15 = Vereinsfeld 0x19 +
    /// random(10 · t, 14) − 10, begrenzt auf 10 … 99 (0x120:0x52); Technik +0x16 = Vereinsfeld 0x1B + random(10 · t, 14) − 10,
    /// mit random(0, 4) = 0 zusätzlich + random(12, 20), begrenzt auf 10 … 99. random(0, 14) bzw. random(10, 14).
    /// Vereinsfelder eines Nationalteams: [ORIGINAL] Lader 0x2EF1B setzt Kondition/Technik aus der Ländertabelle DGROUP 0x4C4E
    /// (ohne Streuung), Deutschland ohne Bundestrainer neu gewürfelt (0x55506) – <see cref="GameState.NationalRatings"/> (A209).
    /// [APPROX] Vereinsfelder der Vereine aus Datenbankstärke und Abweichung des Laders (<see cref="MatchEngine.TeamValuesOf"/>);
    /// Länderwerte aus der Aufstellung eines Bundestrainers (0x952C) gehen nicht ein; ohne <paramref name="game"/> die Tabelle.
    /// </summary>
    public static void RollMarketStrengths(Player p, Club club, Random random, GameState? game = null)
    {
        var national = club.Index is >= NationalTeams.First and <= NationalTeams.Last;
        var t = national ? 1 : 0;
        int condition, techniqueBase;
        if (national)
        {
            var (_, _, ca, ta) = game?.NationalRatings(club.Index) ?? NationalTeamRatings.Of(club.Index);
            (condition, techniqueBase) = (ca, ta);
        }
        else
        {
            var values = MatchEngine.TeamValuesOf(club);
            (condition, techniqueBase) = (club.Strength + values.AttackCondition, club.Strength + values.AttackTechnique);
        }
        p.Condition = Math.Clamp(condition + TransferRules.Random16(random, 10 * t, 14) - 10, 10, 99);
        var technique = techniqueBase + TransferRules.Random16(random, 10 * t, 14) - 10;
        if (TransferRules.Random16(random, 0, 4) == 0)
            technique += TransferRules.Random16(random, 12, 20);
        p.Technique = Math.Clamp(technique, 10, 99);
    }

    /// <summary>
    /// Kaderplatz-Felder des neuen Markt-Platzes, soweit das Remake sie führt. [ORIGINAL] 0x42E4C: Energie +0x13 =
    /// random(80, 120); 0x42E6B: Nachbearbeitung 0x118:0x4D = Datei 0x4907A (Tendenz und Steuerbyte, <see cref="Player.RollTrend"/>);
    /// Datei 0x497C7–0x49803: +0x19 = random(3, 12) · 16 + random(3, 12) (Über-/Unterzahl, <see cref="HiddenSkills"/>).
    /// [APPROX] Energie auf 99 begrenzt (Remake-Skala). Die Tendenz zieht das Remake aus <paramref name="trendRandom"/>, sonst aus
    /// <paramref name="random"/> (siehe <see cref="GameState.TrendRandom"/>). Integration Runde 11: Tendenz (kader-transfer Teil 4)
    /// und +0x19 (wettbewerbe-spiel Teil 7) zusammen, in der Reihenfolge des Originals.
    /// </summary>
    public static void RollSlotFields(Player p, Random random, Random? trendRandom = null)
    {
        // [ORIGINAL] Belastung des neuen Kaderplatzes random(80, 120) (Datei 0x42E4C–0x42E5E; A184, Integration Runde 12, R12I-01).
        p.Strain = TransferRules.Random16(random, 80, 120);
        p.RollTrend(trendRandom ?? random);
        HiddenSkills.Roll(p, random); // Spur wettbewerbe-spiel, Teil 7: +0x19 wie gezogen (A185, A186)
    }

    /// <summary>
    /// Index eines Poolspielers für den Listenwechsel. [ORIGINAL] Datei 0x495D8–0x9628: random(1, 179), mit random(0, 5) = 0
    /// stattdessen random(180, 199) (Nationalspieler); bei anderer Poolgröße die Bereiche von <paramref name="layout"/>.
    /// </summary>
    public static int DrawIndex(TransferPoolLayout layout, Random random)
    {
        var index = TransferRules.Random16(random, 1, layout.LeagueLast);
        if (TransferRules.Random16(random, 0, 5) == 0)
            index = TransferRules.Random16(random, layout.FirstNational, layout.Last);
        return index;
    }

    /// <summary>Index im Pool des Originals (<see cref="TransferPoolLayout.Original"/>).</summary>
    public static int DrawIndex(Random random) => DrawIndex(TransferPoolLayout.Original, random);

    /// <summary>
    /// Ersatzverein für die Grundstärken, wenn der Poolverein ein Managerverein ist. [ORIGINAL] Datei 0x96683–0x96EB: Liga l
    /// = erste Liga mit Start DGROUP 0x7C0[l] ≤ c &lt; Start + 12 (0x7B1), sonst 2; dann random(0x7C6[2l], 0x7C7[2l])
    /// (0 … 11, 12 … 23, 24 … 35), bis ein Verein ohne Manager getroffen ist. Der Poolverein (+0x1D) bleibt.
    /// </summary>
    public static int StandIn(int club, Func<int, bool> managed, Random random)
    {
        var league = club is >= 0 and < 36 ? club / 12 : 2;
        for (var attempt = 0; attempt < 10_000; attempt++)
        {
            var c = TransferRules.Random16(random, 12 * league, 12 * league + 11);
            if (!managed(c))
                return c;
        }
        return club; // [APPROX] alle Vereine der Liga mit Manager (nur synthetisch denkbar)
    }
}

public sealed partial class TransferMarket
{
    private readonly List<PoolSlot> _pool = [];
    private readonly List<PoolDatabaseName> _databaseNames = [];

    /// <summary>Spielerpool (Index 1 … Größe − 1) in Indexreihenfolge; leer in Spielständen vor Teil 5, bis zum ersten Listenwechsel.</summary>
    public IReadOnlyList<PoolSlot> Pool => _pool;

    /// <summary>Aufteilung des Pools nach seiner Größe (ohne Pool die des Originals).</summary>
    public TransferPoolLayout PoolLayout => _pool.Count == 0 ? TransferPoolLayout.Original : TransferPoolLayout.For(_pool.Count + 1);

    /// <summary>Spieler der Datenbank je Verein und Kaderplatz, festgehalten vor dem Aufbau der Kader (Quelle der Poolnamen).</summary>
    public IReadOnlyList<PoolDatabaseName> DatabaseNames => _databaseNames;

    /// <summary>Poolspieler, die nicht in einem Kader, bei den Vertragslosen oder im Ruhestand stehen (frei oder auf dem Markt).</summary>
    public IEnumerable<Player> PoolPlayers => _pool.Where(s => s.Owner != PoolOwner.Taken).Select(s => s.Player);

    private PoolSlot? SlotOf(Player player) => _pool.FirstOrDefault(s => ReferenceEquals(s.Player, player));

    /// <summary>
    /// Spielstart, vor dem Aufbau der Kader (<see cref="SquadSetup.Initialize"/>): Datenbankspieler festhalten, den Pool
    /// bauen (<see cref="BuildPool"/>) und die so vergebenen Datenbankspieler aus den Kadern der Computervereine nehmen.
    /// Entscheidung Nutzer 01.11.2026 (Vorschlag 1): jeder Name steht nur einmal im Spiel, wie im Original, wo Computervereine
    /// keine Spieler haben. Fehlende Plätze ergänzt der Kaderaufbau wie bisher. [APPROX] Eigene Zufallsquelle aus dem Startwert,
    /// damit der Kaderaufbau seine Zufallsfolge behält. Spieler werden über ihre ID unterschieden; gleiche Namen bleiben
    /// möglich, wenn eine geänderte Datenbank sie enthält.
    /// </summary>
    public void ReservePool(GameState game, int seed, TransferPoolLayout? layout = null)
    {
        CaptureDatabaseNames(game);
        BuildPool(game, new GameRandom(unchecked(seed * 7919 + 29)), layout ?? TransferPoolLayout.Original, new HashSet<string>());
        foreach (var slot in _pool)
        {
            var club = game.Clubs[slot.Club];
            if (IsManaged(game, club))
                continue;
            if (club.Players.FirstOrDefault(p => p.Name == slot.Player.Name) is { } taken)
            {
                club.Players.Remove(taken);
                club.Lineup.Remove(taken);
                club.Lineup.FillGaps(club.Players);
            }
        }
    }

    /// <summary>
    /// Kaderplätze der Datenbank aus den Kadern beim Spielstart (<see cref="NewGame"/> übernimmt die Datenbankspieler je Art in
    /// Platzreihenfolge). [APPROX] Leere Plätze sind nicht mehr bekannt: Platz = Anfang der Art + laufende Nummer.
    /// </summary>
    private void CaptureDatabaseNames(GameState game)
    {
        _databaseNames.Clear();
        foreach (var club in game.Clubs)
            foreach (var role in Enum.GetValues<PlayerRole>())
            {
                var start = SlotStart(role);
                var players = club.Players.Where(p => p.Role == role && p.Name.Length > 0).ToList();
                for (var j = 0; j < players.Count && start + j < SlotBounds[(int)RoleIndex(role) + 1]; j++)
                    _databaseNames.Add(new PoolDatabaseName { Club = club.Index, Slot = start + j, Name = players[j].Name });
            }
    }

    private static int RoleIndex(PlayerRole role) => role switch
    {
        PlayerRole.Goalkeeper => 0,
        PlayerRole.Defender => 1,
        _ => 2,
    };

    private static int SlotStart(PlayerRole role) => SlotBounds[RoleIndex(role)];

    /// <summary>Namen, die außerhalb des Pools vergeben sind: Kader, Vertragslose, Laufbahnende.</summary>
    private HashSet<string> HeldNames(GameState game) =>
        game.Clubs.SelectMany(c => c.Players).Concat(FreeAgents).Concat(game.RetiredPlayers)
            .Where(p => SlotOf(p) is null).Select(p => p.Name).ToHashSet(StringComparer.Ordinal);

    /// <summary>Datenbankname auf (Verein, Kaderplatz), der noch nicht außerhalb des Pools vergeben ist; sonst null (leerer Platz).</summary>
    private static string? NameAt(Dictionary<(int Club, int Slot), string> names, HashSet<string> held, int club, int slot) =>
        names.TryGetValue((club, slot), out var name) && !held.Contains(name) ? name : null;

    private Dictionary<(int Club, int Slot), string> NameMap() =>
        _databaseNames.GroupBy(n => (n.Club, n.Slot)).ToDictionary(g => g.Key, g => g.First().Name);

    /// <summary>
    /// Pool bauen. [ORIGINAL] Spielvorbereitung 0x5AF50 (66C3:0730): Startwerte je Index (<see cref="TransferPoolRules.RollStartValues"/>),
    /// dann je Index Talent +0x12 = random(0, 9) und für noch namenlose Plätze ein Datenbankname (0x5AAA0,
    /// <see cref="NameFromLeague"/>), dann die Rollenwerte (<see cref="TransferPoolRules.RollRoleValue"/>), zuletzt die
    /// Nationalspieler (0x2A0B5 → 0x5ACA0 mit Art 0, <see cref="Rename(GameState, bool, Random, HashSet{string}, bool)"/>).
    /// [APPROX] Das Original zieht danach die Managerkader aus diesem Pool (DGROUP 0x381); das Remake baut sie wie bisher aus der
    /// Datenbank, alle Plätze bleiben frei. Größe nach <paramref name="layout"/>.
    /// </summary>
    internal void BuildPool(GameState game, Random random, TransferPoolLayout layout, HashSet<string> held)
    {
        _pool.Clear();
        for (var i = 1; i < layout.Size; i++)
        {
            var p = new Player { Name = "", Role = layout.RoleOf(i), ContractYears = 0 };
            TransferPoolRules.RollStartValues(p, random);
            _pool.Add(new PoolSlot { Index = i, Player = p, Club = 0, Owner = PoolOwner.Free });
        }
        var names = NameMap();
        var (defenders, forwards) = layout.LeagueRoleStarts;
        var role = 0;
        foreach (var slot in _pool)
        {
            if (slot.Index == defenders || slot.Index == forwards) // [ORIGINAL] Grenzen DGROUP 0x382: 21, 97, 200
                role++;
            slot.Player.Talent = TransferRules.Random16(random, 0, 9);
            if (slot.Player.Name.Length == 0)
                NameFromLeague(game, slot, role, random, names, held);
        }
        foreach (var slot in _pool)
            TransferPoolRules.RollRoleValue(layout, slot.Index, random);
        Rename(game, nationals: true, random, held, exchange: false);
        foreach (var slot in _pool.Where(s => s.Player.Id == 0 && game.NextPlayerId > 1))
            game.AssignPlayerId(slot.Player);
    }

    /// <summary>
    /// Name eines namenlosen Platzes. [ORIGINAL] Datei 0x5AAA0 (66C3:0280): Kaderplatz s = random(DGROUP 0x164[k],
    /// 0x165[k] − 1) mit k = Art (0, 2, 12, 25); Verein c = random(0, 11), nach einem doppelten Namen einmal random(0, 35)
    /// (Merker DGROUP 0x55EE); Managerverein verworfen; leerer Name → neuer Versuch; Name schon im Pool → Merker, neuer
    /// Versuch; danach +0x1D = c. [APPROX] Gibt es keinen freien Datenbanknamen dieser Art mehr (synthetische Datenbank,
    /// Spielstand vor Teil 5), heißt der Platz „POOLSPIELER n“ und gehört dem n-ten Verein der Bundesliga ohne Manager.
    /// </summary>
    private void NameFromLeague(GameState game, PoolSlot slot, int role, Random random,
        Dictionary<(int Club, int Slot), string> names, HashSet<string> held)
    {
        bool Available(int c, int s) => NameAt(names, held, c, s) is { } n && _pool.All(o => o.Player.Name != n);
        var possible = Enumerable.Range(0, 36).Where(c => !IsManaged(game, game.Clubs[c]))
            .Any(c => Enumerable.Range(SlotBounds[role], SlotBounds[role + 1] - SlotBounds[role]).Any(s => Available(c, s)));
        if (possible)
        {
            var wide = false;
            for (var attempt = 0; attempt < 100_000; attempt++)
            {
                var s = TransferRules.Random16(random, SlotBounds[role], SlotBounds[role + 1] - 1);
                int c;
                do
                    c = TransferRules.Random16(random, 0, wide ? 35 : 11);
                while (IsManaged(game, game.Clubs[c]));
                var name = NameAt(names, held, c, s);
                if (name is null)
                    continue;
                if (_pool.Any(o => o.Player.Name == name))
                {
                    wide = true;
                    continue;
                }
                Rename(game, slot, name);
                slot.Club = c;
                return;
            }
        }
        Rename(game, slot, $"POOLSPIELER {slot.Index}");
        var club = (slot.Index - 1) % 12;
        for (var k = 0; k < 36 && IsManaged(game, game.Clubs[club]); k++)
            club = (club + 1) % 36;
        slot.Club = club;
    }

    /// <summary>Grenzen der Kaderplätze je Art in der Datenbank. [ORIGINAL] DGROUP 0x164 bzw. 0x55F0: 0, 2, 12, 25.</summary>
    private static readonly int[] SlotBounds = [0, 2, 12, 25];

    /// <summary>
    /// Neue Namen und Vereine der freien Plätze einer Art. [ORIGINAL] Datei 0x5ACA0 (0x140:0x48) mit Satz DGROUP 0x55F4 + 6 · Art:
    /// Art 0 Indizes 179 … 199, Vereine 52 … 63 (DGROUP 0x6AD … + 11), Wechsel der Kaderart bei 184 und 191; Art 1 Indizes
    /// 1 … 179, Vereine 0 … 23, Wechsel bei 21 und 97 (bei anderer Poolgröße nach <see cref="TransferPoolLayout"/>). Nur Plätze
    /// mit Besitzer 6, deren Verein (+0x1D) kein Managerverein ist. Je Platz: Verein c = random(lo, hi), Kaderplatz s =
    /// random(Anfang, Ende − 1) der Art, mit random(0, 15) = 0 stattdessen random(0, 1) (ein Torhütername); leerer Name → neuer
    /// Versuch; Name schon an einem anderen Platz der Art → neuer Versuch; dann Name und +0x1D = c. Gerufen in der
    /// Spielvorbereitung (nur Art 0, 0x2A0B5) und beim Saisonwechsel (Art 0, dann Art 1, 0x56467).
    /// [APPROX] Jeder Name nur einmal im Spiel (Entscheidung Nutzer 01.11.2026):
    /// <list type="bullet">
    /// <item>Steht der Datenbankspieler im Kader eines Computervereins und hat er die Art des Platzes, tauschen Pool und Kader:
    ///   Er wird Poolspieler (mit seinen Werten statt denen des Platzes), der bisherige Poolspieler übernimmt seinen Kaderplatz;
    ///   einer anderen Art (Torhütername mit random(0, 15) = 0) oder ein Poolspieler ab 37 Jahren (Laufbahnende der Kader-KI,
    ///   <see cref="SquadRules.RetireMaxAge"/>) verwirft den Versuch. Im Original hat der
    ///   Computerverein keine Spieler; so bleiben Kadergröße, Namen und IDs eindeutig.</item>
    /// <item>Steht er in einem Managerkader, bei den Vertragslosen, im Ruhestand, ist er geliehen oder nominiert, gilt der Platz
    ///   als leer.</item>
    /// <item>Ein Name an einem anderen Poolplatz verwirft den Versuch immer (das Original lässt freie Plätze mit größerem Index
    ///   zu, die danach selbst umbenannt werden); gibt es im Bereich keinen freien Namen, bleibt der Platz.</item>
    /// </list>
    /// Die Sonderprüfung gegen DGROUP 0x6AC (Startwert 56 = Deutschland, nur ohne Manager verworfen) ist nicht abgebildet –
    /// FILE05 zeigt einen Poolspieler Deutschlands.
    /// </summary>
    private void Rename(GameState game, bool nationals, Random random, HashSet<string> held, bool exchange)
    {
        var layout = PoolLayout;
        var naming = layout.NationalNaming;
        var leagueStarts = layout.LeagueRoleStarts;
        var (start, end, first, second, lo, hi) = nationals
            ? (naming.Start, layout.Size, naming.Defenders, naming.Forwards, NationalTeams.First, NationalTeams.Last)
            : (1, layout.LeagueLast + 1, leagueStarts.Defenders, leagueStarts.Forwards, 0, 23);
        var names = NameMap();
        // Spieler der Computerkader im Bereich, die getauscht werden dürfen.
        var squads = new Dictionary<string, (Club Club, Player Player)>(StringComparer.Ordinal);
        foreach (var club in game.Clubs.Where(c => exchange && c.Index >= lo && c.Index <= hi && !IsManaged(game, c)))
            foreach (var p in club.Players.Where(p => !p.OnLoan && p.Loan is null && game.NationalCoach?.Contains(p) != true))
                squads.TryAdd(p.Name, (club, p));
        var blocked = new HashSet<string>(held, StringComparer.Ordinal);
        blocked.ExceptWith(squads.Keys);
        var poolNames = _pool.Select(o => o.Player.Name).ToHashSet(StringComparer.Ordinal);
        var candidates = names.Where(kv => kv.Key.Club >= lo && kv.Key.Club <= hi && !blocked.Contains(kv.Value))
            .Select(kv => kv.Value).ToHashSet(StringComparer.Ordinal);
        var role = 0;
        for (var i = start; i < end; i++)
        {
            if (i == first || i == second)
                role++;
            var slot = _pool[i - 1];
            if (slot.Owner != PoolOwner.Free || IsManaged(game, game.Clubs[slot.Club]))
                continue;
            if (!candidates.Any(n => !poolNames.Contains(n)))
                continue;
            for (var attempt = 0; attempt < 100_000; attempt++)
            {
                var c = TransferRules.Random16(random, lo, hi);
                var s = TransferRules.Random16(random, SlotBounds[role], SlotBounds[role + 1] - 1);
                if (TransferRules.Random16(random, 0, 15) == 0)
                    s = TransferRules.Random16(random, 0, 1);
                var name = NameAt(names, blocked, c, s);
                if (name is null || poolNames.Contains(name))
                    continue;
                var old = slot.Player;
                // Der Kader behält seine Besetzung je Art: getauscht wird nur innerhalb der Art.
                // Ein Poolspieler im Alter des sicheren Laufbahnendes (SquadRules.RetireMaxAge) kommt in keinen Kader.
                if (squads.TryGetValue(name, out var same)
                    && (same.Player.Role != old.Role || old.Age >= SquadRules.RetireMaxAge))
                    continue;
                if (squads.Remove(name, out var holder))
                {
                    Exchange(slot, holder.Club, holder.Player);
                    squads[old.Name] = (holder.Club, old); // steht jetzt im Kader, kann wieder getauscht werden
                }
                else if (old.Games == 0 && game.NationalCoach?.Contains(old) != true)
                    Rename(game, slot, name);
                else
                    break; // [APPROX] einen Spieler mit Laufbahn nicht verwerfen
                poolNames.Remove(old.Name);
                poolNames.Add(name);
                slot.Club = c;
                break;
            }
        }
    }

    /// <summary>
    /// Tausch beim Umbenennen: Der Kaderspieler <paramref name="player"/> wird Poolspieler des Platzes, der bisherige Poolspieler
    /// übernimmt seinen Platz im Kader (Rückennummer und Vertrag neu wie bei einem Wechsel, Aufstellung wie nach einem Verkauf
    /// eines Computervereins, <see cref="TransferMarket.Move"/>).
    /// </summary>
    private static void Exchange(PoolSlot slot, Club club, Player player)
    {
        var old = slot.Player;
        var years = Math.Max(1, player.ContractYears);
        club.Players.Remove(player);
        club.Lineup.Remove(player);
        player.UnsolicitedOffer = null;
        player.ContractYears = 0;
        player.MonthlySalary = 0;
        player.Insured = false;
        player.Notice = ContractNotice.None;
        if (old.Strain >= TrainingRules.DefaultStrain)
            old.Strain = TrainingRules.DefaultStrain - PlayerFactory.TalentEnergy; // [APPROX] Energie gibt es nur am Kaderplatz (+0x13); wie ein Talent
        PlayerFactory.Join(club, old, years, loan: false);
        club.Lineup.FillGaps(club.Players);
        slot.Player = player;
    }

    /// <summary>
    /// Neuer Name eines Platzes. [APPROX] Das Original überschreibt den Namen am Platz; im Remake ist der Name fest, der Platz
    /// bekommt einen neuen Spieler mit den Werten des alten (und eine neue ID, wenn der alte schon eine hatte). Freie
    /// Poolspieler stehen nirgends sonst, der alte Spieler entfällt.
    /// </summary>
    private static void Rename(GameState game, PoolSlot slot, string name)
    {
        var old = slot.Player;
        var fresh = new Player
        {
            Name = name, Role = old.Role, Age = old.Age, Talent = old.Talent, Condition = old.Condition,
            Technique = old.Technique, Form = old.Form, Strain = old.Strain,
        };
        if (old.Id != 0)
            game.AssignPlayerId(fresh);
        slot.Player = fresh;
    }

    /// <summary>
    /// Saisonwechsel des Pools. [ORIGINAL] Saisonende 0x274A7: Alter +0x14 aller Poolplätze + 1 (ohne Laufbahnende –
    /// Entscheidung Nutzer 01.11.2026: Originaltreue; Verjüngung beim Umbenennen ist als Erweiterung nach v1.0 vorgemerkt,
    /// <c>docs/anforderungen/zusatzfunktionen.md</c>); Datei 0x56467/0x56470: neue Namen der freien Plätze (Art 0, dann Art 1).
    /// [APPROX] Vergebene Plätze, deren Spieler nicht mehr in einem Managerkader steht, bekommen einen neuen Spieler mit den
    /// Werten des alten (im Original wird der Platz bei Verkauf oder Vertragsende wieder frei); der alte bleibt, wo er ist, und
    /// behält seinen Namen – der neue Platz wird beim Umbenennen neu benannt oder heißt „POOLSPIELER n“.
    /// </summary>
    internal void StartPoolSeason(GameState game, Random random)
    {
        if (_pool.Count == 0)
            return;
        var managed = game.Managers.Where(m => !m.Dismissed).Select(m => game.Clubs[m.ClubIndex]).ToList();
        foreach (var slot in _pool)
        {
            if (slot.Owner == PoolOwner.Taken)
            {
                if (managed.Any(c => c.Players.Contains(slot.Player)))
                    continue;
                Rename(game, slot, $"POOLSPIELER {slot.Index}");
                slot.Owner = PoolOwner.Free;
            }
            slot.Player.Age++;
        }
        Rename(game, nationals: true, random, HeldNames(game), exchange: true);
        Rename(game, nationals: false, random, HeldNames(game), exchange: true); // nach dem Tausch neu bestimmt
    }

    /// <summary>
    /// Wechsel der Transferliste. [ORIGINAL] em.exe Datei 0x494EF (Stub 0x118:0x5C), gerufen beim Saisonstart (0x43A9F) und im
    /// Tageslauf mit random(0, Managerzahl + 2) = 0 (0x54E71), Markt-Satz 4 mit 10 Plätzen:
    /// <list type="number">
    /// <item>Je Platz k = 0 … 9 (0x494FE–0x49549): Platz frei (0x108:0x48 = 0x425B6) – ein Platz eines Managers bleibt und
    ///   zählt, ein Markt-Platz wird entfernt (die folgenden rücken auf, k zählt trotzdem weiter) und der Poolspieler wird
    ///   wieder frei (Besitzer 6).</item>
    /// <item>n = 10 − Managerplätze; n-mal (0x49566–0x4980F): Platz cx = random(0, 9). Gehört er einem Manager, nichts; ein
    ///   Markt-Platz wird entfernt (Besitzer 6). Dann ein freier Poolspieler (<see cref="TransferPoolRules.DrawIndex"/>, bis zu
    ///   1500 Versuche, sonst Abbruch), Form +0x17 = random(45, 55), Grundstärken aus dem Poolverein bzw. bei einem Managerverein
    ///   aus einem Ersatzverein (<see cref="TransferPoolRules.StandIn"/>, <see cref="TransferPoolRules.RollMarketStrengths"/>),
    ///   neuer Kaderplatz am Ende (0x108:0x5C), Besitzer 4, +0x1D = Poolverein, Preis +0x2C = Wert(Preis), Gehalt +0x28 =
    ///   Wert(Gehalt), +0x19 (<see cref="TransferPoolRules.RollSlotFields"/>).</item>
    /// </list>
    /// [APPROX] Ohne Pool (Spielstände vor Teil 5) wird er zuerst gebaut. Markt-Einträge vertragsloser Spieler alter Spielstände
    /// zählen als Markt-Plätze.
    /// </summary>
    public void ChangeList(GameState game, Random random)
    {
        ArgumentNullException.ThrowIfNull(game);
        if (_pool.Count == 0)
        {
            // [APPROX] Spielstand vor Teil 5: Datenbank nicht mehr bekannt; Namen der Kader gelten als vergeben.
            if (_databaseNames.Count == 0)
                CaptureDatabaseNames(game);
            BuildPool(game, random, TransferPoolLayout.Original, HeldNames(game));
        }
        var layout = PoolLayout;

        var held = 0;
        for (var k = 0; k < TransferPoolRules.MarketSlots; k++)
        {
            if (k >= _listings.Count)
                continue;
            if (IsHeld(_listings[k]))
                held++;
            else
                RemoveMarketSlot(k);
        }

        var n = TransferPoolRules.MarketSlots - held;
        for (var j = 0; j < n; j++)
        {
            var cx = TransferRules.Random16(random, 0, TransferPoolRules.MarketSlots - 1);
            if (cx < _listings.Count)
            {
                if (IsHeld(_listings[cx]))
                    continue;
                RemoveMarketSlot(cx);
            }

            PoolSlot? slot = null;
            for (var attempt = 0; attempt < TransferPoolRules.MaxDrawAttempts && slot is null; attempt++)
            {
                var candidate = _pool[TransferPoolRules.DrawIndex(layout, random) - 1];
                if (candidate.Owner == PoolOwner.Free)
                    slot = candidate;
            }
            if (slot is null)
                return; // [ORIGINAL] 0x4962A: nach 1500 Versuchen endet der Wechsel

            var p = slot.Player;
            p.Form = TransferRules.Random16(random, 45, 55);
            var stand = IsManaged(game, game.Clubs[slot.Club])
                ? TransferPoolRules.StandIn(slot.Club, c => IsManaged(game, game.Clubs[c]), random)
                : slot.Club;
            TransferPoolRules.RollMarketStrengths(p, game.Clubs[stand], random, game);
            TransferPoolRules.RollSlotFields(p, random, game.TrendRandom);
            p.ContractYears = 0;
            p.MonthlySalary = 0;
            slot.Owner = PoolOwner.Market;
            _listings.Add(new TransferListing
            {
                Player = p,
                From = game.Clubs[slot.Club],
                Price = Price(p),
                ListedDay = game.PlayedMatchDays,
                FromPool = true,
            });
        }
    }

    /// <summary>
    /// Platz, den der Listenwechsel nicht räumt: eigener Spieler eines Managers ([ORIGINAL] Besitzer ≠ 4, 0x425F0) oder
    /// [APPROX] ein Vertragsloser aus einem Managerverein (Remake-Ergänzung R204, <see cref="TransferMarket.StartSeason"/>).
    /// </summary>
    private static bool IsHeld(TransferListing listing) => listing.Seller is not null || listing.IsFreeAgent;

    /// <summary>Entfernt den Markt-Platz k (die folgenden rücken auf) und gibt einen Poolspieler frei.</summary>
    private void RemoveMarketSlot(int k)
    {
        var listing = _listings[k];
        _listings.RemoveAt(k);
        _bids.RemoveAll(b => b.Listing == listing);
        if (listing.FromPool && SlotOf(listing.Player) is { Owner: PoolOwner.Market } slot)
            slot.Owner = PoolOwner.Free;
    }

    /// <summary>Täglicher Wurf des Listenwechsels. [ORIGINAL] Datei 0x54E65–0x54E7C: random(0, Managerzahl + 2) = 0.</summary>
    public static bool ListChangeRoll(int managers, Random random) => TransferRules.Random16(random, 0, managers + 2) == 0;
}
