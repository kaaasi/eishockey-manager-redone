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
/// Regeln des Spielerpools und des Listenwechsels nach em.exe (Spur transfer-vertraege-komplett, Teil 5, Entscheidung Nutzer
/// 01.11.2026). Fundstellen in <c>docs/transfer.md</c>, Abschnitt „Teil 5“.
/// </summary>
/// <remarks>
/// Das Original verwaltet nur diese 200 Spieler im Detail (Managerkader und Transfermarkt); Computervereine haben keine
/// Spieler. Der Transfermarkt zieht deshalb aus dem Pool, nicht aus Vereinskadern. [APPROX] Das Remake behält seine Kader der
/// Computervereine (Statistik, Kader-KI) – ein Poolspieler trägt den Namen eines Datenbankspielers seines Vereins, der im
/// Remake zugleich im Kader dieses Vereins steht (zwei getrennte Spieler gleichen Namens).
/// </remarks>
public static class TransferPoolRules
{
    /// <summary>Größe des Pools (Index 0 … 199; Index 0 bleibt leer). [ORIGINAL] 200 × 33 Byte, far:8253.</summary>
    public const int Size = 200;

    /// <summary>Kaderplätze des Markt-Satzes, die der Listenwechsel bedient. [ORIGINAL] Datei 0x49516 (Argument 10).</summary>
    public const int MarketSlots = 10;

    /// <summary>Versuche beim Ziehen eines freien Poolspielers. [ORIGINAL] Datei 0x49624 (0x5DC), danach Abbruch.</summary>
    public const int MaxDrawAttempts = 1500;

    /// <summary>
    /// Art eines Poolplatzes nach dem Index. [ORIGINAL] Datei 0x5B391–0x5B4A9 (Rollenwert +0x18): 1–20 Torhüter, 21–96
    /// Verteidiger, 97–179 Stürmer, 180–183 Torhüter, 184–190 Verteidiger, 191–199 Stürmer (Nationalspieler).
    /// </summary>
    public static PlayerRole RoleOf(int index) => index switch
    {
        <= 20 => PlayerRole.Goalkeeper,
        <= 96 => PlayerRole.Defender,
        <= 179 => PlayerRole.Forward,
        <= 183 => PlayerRole.Goalkeeper,
        <= 190 => PlayerRole.Defender,
        _ => PlayerRole.Forward,
    };

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
    public static void RollRoleValue(int index, Random random)
    {
        if (RoleOf(index) != PlayerRole.Goalkeeper)
            _ = TransferRules.Random16(random, 0, 16);
    }

    /// <summary>
    /// Grundstärken beim Listenwechsel aus den Teamwerten des Vereins. [ORIGINAL] Datei 0x4981E–0x4991C (Ov 0x118):
    /// t = 1 bei einem Nationalteam (0x128:0x20 = 0x4D2F0, Index 52 … 63), sonst 0; Kondition +0x15 = Vereinsfeld 0x19 +
    /// random(10 · t, 14) − 10, begrenzt auf 10 … 99 (0x120:0x52); Technik +0x16 = Vereinsfeld 0x1B + random(10 · t, 14) − 10,
    /// mit random(0, 4) = 0 zusätzlich + random(12, 20), begrenzt auf 10 … 99. random(0, 14) bzw. random(10, 14).
    /// [APPROX] Vereinsfelder aus Datenbankstärke und Abweichung des Laders (<see cref="MatchEngine.TeamValuesOf"/>); für
    /// Nationalteams die Datenbankstärke statt der Ländertabelle DGROUP 0x4C4E.
    /// </summary>
    public static void RollMarketStrengths(Player p, Club club, Random random)
    {
        var t = club.Index is >= NationalTeams.First and <= NationalTeams.Last ? 1 : 0;
        var values = MatchEngine.TeamValuesOf(club);
        p.Condition = Math.Clamp(club.Strength + values.AttackCondition + TransferRules.Random16(random, 10 * t, 14) - 10, 10, 99);
        var technique = club.Strength + values.AttackTechnique + TransferRules.Random16(random, 10 * t, 14) - 10;
        if (TransferRules.Random16(random, 0, 4) == 0)
            technique += TransferRules.Random16(random, 12, 20);
        p.Technique = Math.Clamp(technique, 10, 99);
    }

    /// <summary>
    /// Kaderplatz-Felder des neuen Markt-Platzes, soweit das Remake sie führt. [ORIGINAL] 0x42E4C: Energie +0x13 =
    /// random(80, 120); Datei 0x497C7–0x49803: +0x19 = random(3, 12) · 16 + random(3, 12) (Bedeutung offen, nur gezogen).
    /// [APPROX] Energie auf 99 begrenzt (Remake-Skala); die Nachbearbeitung 0x118:0x4D (0x4907A) ist nicht abgebildet.
    /// </summary>
    public static void RollSlotFields(Player p, Random random)
    {
        p.Energy = Player.Clamp(TransferRules.Random16(random, 80, 120));
        _ = TransferRules.Random16(random, 3, 12);
        _ = TransferRules.Random16(random, 3, 12);
    }

    /// <summary>
    /// Index eines Poolspielers für den Listenwechsel. [ORIGINAL] Datei 0x495D8–0x9628: random(1, 179), mit random(0, 5) = 0
    /// stattdessen random(180, 199) (Nationalspieler).
    /// </summary>
    public static int DrawIndex(Random random)
    {
        var index = TransferRules.Random16(random, 1, 179);
        if (TransferRules.Random16(random, 0, 5) == 0)
            index = TransferRules.Random16(random, 180, 199);
        return index;
    }

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

    /// <summary>Spielerpool (Index 1–199) in Indexreihenfolge; leer in Spielständen vor Teil 5, bis zum ersten Listenwechsel.</summary>
    public IReadOnlyList<PoolSlot> Pool => _pool;

    /// <summary>Poolspieler, die nicht in einem Kader, bei den Vertragslosen oder im Ruhestand stehen (frei oder auf dem Markt).</summary>
    public IEnumerable<Player> PoolPlayers => _pool.Where(s => s.Owner != PoolOwner.Taken).Select(s => s.Player);

    private PoolSlot? SlotOf(Player player) => _pool.FirstOrDefault(s => ReferenceEquals(s.Player, player));

    /// <summary>
    /// Pool zu Spielbeginn. [ORIGINAL] Spielvorbereitung 0x5AF50 (66C3:0730): Startwerte je Index 1–199
    /// (<see cref="TransferPoolRules.RollStartValues"/>), dann je Index Talent +0x12 = random(0, 9) und für noch namenlose Plätze
    /// ein Datenbankname (0x5AAA0, <see cref="NameFromLeague"/>), dann die Rollenwerte (<see cref="TransferPoolRules.RollRoleValue"/>),
    /// zuletzt die Nationalspieler 179–199 (0x2A0B5 → 0x5ACA0 mit Art 0, <see cref="RenameNationals"/>).
    /// [APPROX] Das Original zieht danach die Managerkader aus diesem Pool (DGROUP 0x381); das Remake baut sie wie bisher aus der
    /// Datenbank, alle 199 Plätze bleiben frei.
    /// </summary>
    internal void BuildPool(GameState game, Random random)
    {
        _pool.Clear();
        for (var i = 1; i < TransferPoolRules.Size; i++)
        {
            var p = new Player { Name = "", Role = TransferPoolRules.RoleOf(i), ContractYears = 0 };
            TransferPoolRules.RollStartValues(p, random);
            _pool.Add(new PoolSlot { Index = i, Player = p, Club = 0, Owner = PoolOwner.Free });
        }
        var role = 0;
        foreach (var slot in _pool)
        {
            if (slot.Index == 21 || slot.Index == 97) // [ORIGINAL] Grenzen DGROUP 0x382: 21, 97, 200
                role++;
            slot.Player.Talent = TransferRules.Random16(random, 0, 9);
            if (slot.Player.Name.Length == 0)
                NameFromLeague(game, slot, role, random);
        }
        foreach (var slot in _pool)
            TransferPoolRules.RollRoleValue(slot.Index, random);
        RenameNationals(game, random);
        foreach (var slot in _pool.Where(s => s.Player.Id == 0 && game.NextPlayerId > 1))
            game.AssignPlayerId(slot.Player);
    }

    /// <summary>
    /// Name eines namenlosen Platzes. [ORIGINAL] Datei 0x5AAA0 (66C3:0280): Kaderplatz s = random(DGROUP 0x164[k],
    /// 0x165[k] − 1) mit k = Art (0, 2, 12, 25); Verein c = random(0, 11), nach einem doppelten Namen einmal random(0, 35)
    /// (Merker DGROUP 0x55EE); Managerverein verworfen; leerer Name → neuer Versuch; Name schon im Pool (1–199) → Merker,
    /// neuer Versuch; danach +0x1D = c. [APPROX] Kaderplatz s = s-ter Spieler der Art im Kader des Remakes
    /// (<see cref="SquadName"/>); nach 10.000 Versuchen „SPIELER n“.
    /// </summary>
    private void NameFromLeague(GameState game, PoolSlot slot, int role, Random random)
    {
        var wide = false;
        for (var attempt = 0; attempt < 10_000; attempt++)
        {
            var s = TransferRules.Random16(random, SlotBounds[role], SlotBounds[role + 1] - 1);
            int c;
            do
                c = TransferRules.Random16(random, 0, wide ? 35 : 11);
            while (IsManaged(game, game.Clubs[c]));
            var name = SquadName(game.Clubs[c], s);
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
        Rename(game, slot, $"SPIELER {slot.Index}");
    }

    /// <summary>Grenzen der Kaderplätze je Art in der Datenbank. [ORIGINAL] DGROUP 0x164 bzw. 0x55F0: 0, 2, 12, 25.</summary>
    private static readonly int[] SlotBounds = [0, 2, 12, 25];

    /// <summary>
    /// Name auf Datenbank-Kaderplatz <paramref name="slot"/> des Vereins oder null (leerer Platz). [APPROX] Das Remake kennt die
    /// Platznummer nicht mehr: Platz s = (s − Anfang der Art)-ter Spieler dieser Art im Kader, fehlt er, ist der Platz leer.
    /// Vom Remake ergänzte Plätze der Computerkader („SPIELER n“, <see cref="SquadSetup"/>) zählen mit.
    /// </summary>
    private static string? SquadName(Club club, int slot)
    {
        var role = ClubDatabase.RoleForSlot(slot);
        var start = role switch { PlayerRole.Goalkeeper => 0, PlayerRole.Defender => 2, _ => 12 };
        var players = club.Players.Where(p => p.Role == role).ToList();
        var i = slot - start;
        return i < players.Count && players[i].Name.Length > 0 ? players[i].Name : null;
    }

    /// <summary>
    /// Neue Namen und Vereine der freien Plätze einer Art. [ORIGINAL] Datei 0x5ACA0 (0x140:0x48) mit Satz DGROUP 0x55F4 + 6 · Art:
    /// Art 0 Indizes 179 … 199, Vereine 52 … 63 (DGROUP 0x6AD … + 11), Wechsel der Kaderart bei 184 und 191; Art 1 Indizes
    /// 1 … 179, Vereine 0 … 23, Wechsel bei 21 und 97. Nur Plätze mit Besitzer 6, deren Verein (+0x1D) kein Managerverein ist.
    /// Je Platz: Verein c = random(lo, hi), Kaderplatz s = random(Anfang, Ende − 1) der Art, mit random(0, 15) = 0 stattdessen
    /// random(0, 1) (ein Torhütername); leerer Name → neuer Versuch; Name schon an einem anderen Platz der Art (vergeben oder mit
    /// kleinerem Index) → neuer Versuch; dann Name und +0x1D = c. Gerufen in der Spielvorbereitung (nur Art 0, 0x2A0B5) und beim
    /// Saisonwechsel (Art 0, dann Art 1, 0x56467).
    /// [APPROX] Die Sonderprüfung gegen DGROUP 0x6AC (Startwert 56 = Deutschland, nur ohne Manager verworfen) ist nicht
    /// abgebildet – FILE05 zeigt einen Poolspieler Deutschlands; nach 10.000 Versuchen bleibt der Platz unverändert.
    /// </summary>
    private void Rename(GameState game, bool nationals, Random random)
    {
        var (start, end, first, second, lo, hi) = nationals
            ? (179, 200, 184, 191, NationalTeams.First, NationalTeams.Last)
            : (1, 180, 21, 97, 0, 23);
        var role = 0;
        for (var i = start; i < end; i++)
        {
            if (i == first || i == second)
                role++;
            var slot = _pool[i - 1];
            if (slot.Owner != PoolOwner.Free || IsManaged(game, game.Clubs[slot.Club]))
                continue;
            // [APPROX] Ohne Spieler im Vereinsbereich (synthetische Datenbank ohne Nationalkader) bleibt der Platz.
            if (!game.Clubs.Skip(lo).Take(hi - lo + 1).Any(c => c.Players.Count > 0))
                continue;
            for (var attempt = 0; attempt < 10_000; attempt++)
            {
                var c = TransferRules.Random16(random, lo, hi);
                var s = TransferRules.Random16(random, SlotBounds[role], SlotBounds[role + 1] - 1);
                if (TransferRules.Random16(random, 0, 15) == 0)
                    s = TransferRules.Random16(random, 0, 1);
                var name = SquadName(game.Clubs[c], s);
                if (name is null)
                    continue;
                if (_pool.Skip(start - 1).Take(end - start)
                    .Any(o => o != slot && o.Player.Name == name && (o.Owner != PoolOwner.Free || o.Index < i)))
                    continue;
                Rename(game, slot, name);
                slot.Club = c;
                break;
            }
        }
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
            Technique = old.Technique, Form = old.Form, Energy = old.Energy,
        };
        if (old.Id != 0)
            game.AssignPlayerId(fresh);
        slot.Player = fresh;
    }

    private void RenameNationals(GameState game, Random random) => Rename(game, nationals: true, random);

    /// <summary>
    /// Saisonwechsel des Pools. [ORIGINAL] Saisonende 0x274A7: Alter +0x14 aller Poolplätze + 1; Datei 0x56467/0x56470: neue
    /// Namen der freien Plätze (<see cref="Rename"/>, Art 0, dann Art 1). [APPROX] Vergebene Plätze, deren Spieler nicht mehr in
    /// einem Managerkader steht, bekommen einen neuen Spieler mit den Werten des alten (im Original wird der Platz bei Verkauf
    /// oder Vertragsende wieder frei); der alte bleibt, wo er ist. Die Spieler im Managerkader altern dort.
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
                Rename(game, slot, slot.Player.Name);
                slot.Owner = PoolOwner.Free;
            }
            slot.Player.Age++;
        }
        Rename(game, nationals: true, random);
        Rename(game, nationals: false, random);
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
            BuildPool(game, random);

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
                var candidate = _pool[TransferPoolRules.DrawIndex(random) - 1];
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
            TransferPoolRules.RollMarketStrengths(p, game.Clubs[stand], random);
            TransferPoolRules.RollSlotFields(p, random);
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
