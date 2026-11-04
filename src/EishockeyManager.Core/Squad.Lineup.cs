using EishockeyManager.Formats;

namespace EishockeyManager.Core;

/// <summary>Platz in der Aufstellung: Block 0–3 mit drei Stürmern und zwei Verteidigern, dazu der Torhüter.</summary>
public readonly record struct LineupSlot(int Block, PlayerRole Role, int Index)
{
    public static LineupSlot Goalkeeper => new(0, PlayerRole.Goalkeeper, 0);

    public static LineupSlot Forward(int block, int index) => new(block, PlayerRole.Forward, index);

    public static LineupSlot Defender(int block, int index) => new(block, PlayerRole.Defender, index);

    /// <summary>Alle 21 Plätze: je Block erst die Stürmer, dann die Verteidiger, zuletzt der Torhüter.</summary>
    public static IEnumerable<LineupSlot> All
    {
        get
        {
            for (var b = 0; b < Lineup.Blocks; b++)
            {
                for (var i = 0; i < Lineup.ForwardsPerBlock; i++)
                    yield return Forward(b, i);
                for (var i = 0; i < Lineup.DefendersPerBlock; i++)
                    yield return Defender(b, i);
            }
            yield return Goalkeeper;
        }
    }
}

public sealed partial class GameState
{
    /// <summary>
    /// Bereinigung gehört zur Simulation; Ansichten dürfen weder Kader noch Aufstellung verändern. Managervereine stellt
    /// mit der Einstellung „automatisch aufstellen“ der Tageslauf neu auf (<see cref="ApplyAutoLineups"/>, Teil 8).
    /// </summary>
    private void CleanLineups()
    {
        foreach (var club in Clubs)
        {
            club.Lineup.RemoveUnavailable(club.Players);
            if (!Managers.Any(m => m.ClubIndex == club.Index))
                club.Lineup.FillGaps(club.Players);
        }
    }
}

/// <summary>
/// Aufstellung wie im Original: vier Blöcke (3 Stürmer, 2 Verteidiger) und ein Torhüter (TW).
/// Ein Spieler steht höchstens auf einem Platz; Plätze nehmen nur Spieler der passenden Art an.
/// </summary>
public sealed class Lineup
{
    public const int Blocks = 4;
    public const int ForwardsPerBlock = 3;
    public const int DefendersPerBlock = 2;

    /// <summary>
    /// Blöcke, die die Auto-Aufstellung füllt. Im Video bleibt Block 4 bei 8 Verteidigern und
    /// 10 Stürmern leer (zwei Verteidiger und ein Stürmer auf RESERVE).
    /// </summary>
    public const int AutoBlocks = 3;

    private readonly Player?[,] _forwards = new Player?[Blocks, ForwardsPerBlock];
    private readonly Player?[,] _defenders = new Player?[Blocks, DefendersPerBlock];
    private Player? _goalkeeper;

    public Player? this[LineupSlot slot]
    {
        get => slot.Role switch
        {
            PlayerRole.Goalkeeper => _goalkeeper,
            PlayerRole.Defender => _defenders[slot.Block, slot.Index],
            _ => _forwards[slot.Block, slot.Index],
        };
        private set
        {
            switch (slot.Role)
            {
                case PlayerRole.Goalkeeper: _goalkeeper = value; break;
                case PlayerRole.Defender: _defenders[slot.Block, slot.Index] = value; break;
                default: _forwards[slot.Block, slot.Index] = value; break;
            }
        }
    }

    public Player? Goalkeeper => _goalkeeper;

    /// <summary>Alle aufgestellten Spieler.</summary>
    public IEnumerable<Player> Players => LineupSlot.All.Select(s => this[s]).OfType<Player>();

    /// <summary>Anzahl der aufgestellten Spieler.</summary>
    public int Count => Players.Count();

    public bool Contains(Player player) => FindSlot(player) is not null;

    public LineupSlot? FindSlot(Player player)
    {
        foreach (var slot in LineupSlot.All)
            if (ReferenceEquals(this[slot], player))
                return slot;
        return null;
    }

    /// <summary>Spieler eines Blocks (Stürmer und Verteidiger).</summary>
    public IEnumerable<Player> Block(int block)
    {
        for (var i = 0; i < ForwardsPerBlock; i++)
            if (_forwards[block, i] is { } f)
                yield return f;
        for (var i = 0; i < DefendersPerBlock; i++)
            if (_defenders[block, i] is { } d)
                yield return d;
    }

    /// <summary>Darf der Spieler auf diesen Platz? Nur passende Art und nur einsatzfähige Spieler.</summary>
    public static bool Fits(LineupSlot slot, Player player) => player.Role == slot.Role && player.Available;

    /// <summary>
    /// Setzt einen Spieler auf einen Platz. Steht er schon woanders, tauscht er mit dem bisherigen
    /// Inhaber des Zielplatzes (sofern dieser dort hinpasst), sonst wird sein alter Platz frei.
    /// </summary>
    public bool Assign(LineupSlot slot, Player player)
    {
        if (!Fits(slot, player))
            return false;
        var previous = FindSlot(player);
        var occupant = this[slot];
        if (previous is { } p)
            this[p] = occupant is not null && Fits(p, occupant) ? occupant : null;
        this[slot] = player;
        return true;
    }

    public void Clear(LineupSlot slot) => this[slot] = null;

    /// <summary>
    /// Setzt einen Spieler ohne Prüfung der Art auf einen Platz (automatische Aufstellung, Ausweichrollen Datei 0x42838:
    /// im Original bestimmt +0x15 den Platz, nicht die Rolle).
    /// </summary>
    internal void Place(LineupSlot slot, Player player) => this[slot] = player;

    public void Remove(Player player)
    {
        if (FindSlot(player) is { } slot)
            this[slot] = null;
    }

    public void ClearAll()
    {
        foreach (var slot in LineupSlot.All)
            this[slot] = null;
    }

    /// <summary>
    /// Auto-Aufstellung („A“): bester Torhüter, dann je Block die stärksten freien Verteidiger und Stürmer.
    /// [APPROX] Reihenfolge nach Spielwert (Stärke und Energie); die Auswahlregel des Originals ist nicht rekonstruiert.
    /// </summary>
    public void AutoFill(IEnumerable<Player> squad, int blocks = AutoBlocks)
    {
        ClearAll();
        FillGaps(squad, blocks);
        SortBlocksByNumber();
    }

    /// <summary>Innerhalb eines Blocks stehen die Rückennummern aufsteigend (Video: „15 18 20 / 5 8“).</summary>
    public void SortBlocksByNumber()
    {
        for (var b = 0; b < Blocks; b++)
        {
            Sort(_forwards, b, ForwardsPerBlock);
            Sort(_defenders, b, DefendersPerBlock);
        }

        static void Sort(Player?[,] grid, int block, int count)
        {
            var sorted = Enumerable.Range(0, count).Select(i => grid[block, i])
                .OrderBy(p => p is null).ThenBy(p => p?.Number ?? 0).ToList();
            for (var i = 0; i < count; i++)
                grid[block, i] = sorted[i];
        }
    }

    /// <summary>Füllt leere Plätze der ersten <paramref name="blocks"/> Blöcke mit den besten freien Spielern.</summary>
    public void FillGaps(IEnumerable<Player> squad, int blocks = AutoBlocks)
    {
        var free = squad.Where(p => p.Available && !Contains(p))
            .OrderByDescending(TeamStrength.PlayerValue)
            .ThenBy(p => p.Number)
            .ToList();

        Player? Take(PlayerRole role)
        {
            var p = free.FirstOrDefault(x => x.Role == role);
            if (p is not null)
                free.Remove(p);
            return p;
        }

        if (_goalkeeper is null)
            _goalkeeper = Take(PlayerRole.Goalkeeper);
        for (var b = 0; b < blocks; b++)
        {
            for (var i = 0; i < ForwardsPerBlock; i++)
                _forwards[b, i] ??= Take(PlayerRole.Forward);
            for (var i = 0; i < DefendersPerBlock; i++)
                _defenders[b, i] ??= Take(PlayerRole.Defender);
        }
    }

    /// <summary>Entfernt Spieler, die nicht (mehr) im Kader oder nicht einsatzfähig sind.</summary>
    public void RemoveUnavailable(IReadOnlyCollection<Player> squad)
    {
        foreach (var slot in LineupSlot.All)
            if (this[slot] is { } p && (!p.Available || !squad.Contains(p)))
                this[slot] = null;
    }
}

/// <summary>
/// „Eingesetzt“ im Vereinskader (Aufstellung unten rechts „Eingesetzt: n“).
/// </summary>
/// <remarks>
/// Das Original benutzt für Verein und Nationalteam denselben Aufstellungsbildschirm mit Kennwert 0 (Datei 0x40364,
/// Belege bei <see cref="NationalCoach"/>):
/// <list type="bullet">
/// <item>[ORIGINAL] „Eingesetzt: n“ zählt die Kaderplätze mit Feld 0x0A ≠ 0 des aktiven Managers (0x120:0x66 Modus 3,
///   Aufruf Datei 0x41D3F), Farbe 16 (Datei 0x41D84).</item>
/// <item>[ORIGINAL] Höchstens 22 eingesetzt; ein weiterer wird beim Aufstellen oder Einsetzen abgewiesen
///   („Schon 22 Mann / im Team !“, Datei 0x40E2A–0x40E61). Aufstellen setzt ein (Datei 0x415B2), Platz leeren nimmt
///   heraus (Datei 0x40CF8–0x40D0D); Linksklick nimmt einen eingesetzten, nicht aufgestellten Spieler heraus,
///   Rechtsklick setzt einen nicht eingesetzten ein (Datei 0x40F45–0x40F59, 0x411FE).</item>
/// <item>[ORIGINAL] A166: Beim Spielstart (Datei 0x28165), am Ende der Transferliste (Datei 0x47BB5), im Tageslauf (Datei
///   0x7C07) und am Spieltag (Datei 0x54D9E/0x5530B) setzt Datei 0x426BC die Eingesetzten (<see cref="AutoLineupRule"/>;
///   nachgerechnet gegen FILE01, FILE05, FILE07, FILE16A, FILE22–FILE28, Regressionstest mit dem Kader aus FILE05/FILE07/FILE26).
///   Bis zum ersten solchen Lauf (oder ohne Einstellung) gelten die Aufgestellten und die Ersatzrunde von 0x426BC
///   (Datei 0x427C0–0x4282C, <see cref="AutoDressed"/>). Computervereine haben im Original keine Kaderplätze und damit kein
///   +0x0A; für sie ist „eingesetzt“ nur ein Remake-Wert.</item>
/// <item>[ORIGINAL] A167: Ein neuer Kaderplatz entsteht leer (Datei 0x42E3A löscht 0x34 Byte, also +0x0A = 0 und +0x15 = 0);
///   eingesetzt wird der neue Spieler nur durch die Auto-Aufstellung beim Verlassen der Transferliste (Datei 0x47BB5, FILE07:
///   DAFFNER) – nach der ersten Änderung von Hand bleibt er nicht eingesetzt. Aufgestellte zählen immer als eingesetzt
///   (Aufstellen setzt ein, Datei 0x415B2; Platz leeren nimmt heraus, FILE06). [APPROX] Kauf bei ausgeschalteter Einstellung
///   zeigt kein Stand. Bewusste Modellabweichung: Ein Verletzter behält im Original seine Reihe +0x15 und verliert nur +0x0A
///   (Datei 0x5C50, FILE24 STEFAN); das Remake nimmt ihn aus der Aufstellung (<see cref="RefillLineup"/>). Gleichwertig,
///   solange kein Leser +0x15 ohne +0x0A auswertet; betrifft auch Export und Import von Spielständen.</item>
/// <item>[APPROX] Eingesetzt wirkt bisher nur auf die Anzeige; die Spielstärke kommt weiter aus der Aufstellung.</item>
/// </list>
/// </remarks>
public sealed partial class Club
{
    /// <summary>[ORIGINAL] Höchstens 22 eingesetzte Spieler (Datei 0x40E4D: Vergleich mit 0x16).</summary>
    public const int MaxDressed = 22;

    /// <summary>Spieler-IDs der von Hand Eingesetzten (IDs statt Verweise: Abgänge brauchen keine Bereinigung).</summary>
    private readonly HashSet<int> _dressed = [];
    private bool _dressedByHand;

    /// <summary>Eingesetzte Spieler des Kaders (siehe Klassenbemerkung).</summary>
    public IReadOnlyList<Player> Dressed
    {
        get
        {
            var squad = Players.ToList();
            if (!_dressedByHand)
                return AutoDressed(squad);
            return squad.Where(p => Lineup.Contains(p) || _dressed.Contains(p.Id)).ToList();
        }
    }

    /// <summary>Anzahl der Eingesetzten („Eingesetzt: n“).</summary>
    public int DressedCount => Dressed.Count;

    public bool IsDressed(Player player) => Dressed.Contains(player);

    /// <summary>
    /// A166: Eingesetzte, solange niemand von Hand oder über <see cref="ApplyAutoLineup"/> eingesetzt hat – die Aufgestellten,
    /// dann die Ersatzrunde von Datei 0x426BC (<see cref="AutoLineupRule.FillReserves"/>) in Kaderplatzfolge.
    /// </summary>
    private List<Player> AutoDressed(List<Player> squad)
    {
        var dressed = squad.Select(Lineup.Contains).ToArray();
        AutoLineupRule.FillReserves(squad, dressed, 1 + dressed.Count(d => d));
        return squad.Where((_, i) => dressed[i]).ToList();
    }

    /// <summary>Erste Änderung von Hand: die bisher automatisch Eingesetzten festhalten.</summary>
    private void TakeOverDressed()
    {
        if (_dressedByHand)
            return;
        var current = Dressed;
        _dressed.Clear();
        _dressed.UnionWith(current.Select(p => p.Id));
        _dressedByHand = true;
    }

    /// <summary>Spieler auf einen Platz setzen; er wird dabei eingesetzt (Datei 0x415B2).</summary>
    public LineupResult AssignToLineup(LineupSlot slot, Player player)
    {
        if (!Players.Contains(player))
            return LineupResult.NotInSquad;
        if (player.InjuredGames > 0)
            return LineupResult.Injured;
        if (player.SuspendedGames > 0)
            return LineupResult.Suspended;
        if (player.Role != slot.Role)
            return LineupResult.WrongRole;
        if (!IsDressed(player) && DressedCount >= MaxDressed)
            return LineupResult.RosterFull;
        TakeOverDressed();
        if (!Lineup.Assign(slot, player))
            return LineupResult.WrongRole;
        _dressed.Add(player.Id);
        DisableAutoLineup();
        return LineupResult.Assigned;
    }

    /// <summary>Platz leeren; der Spieler ist danach nicht mehr eingesetzt (Datei 0x40CF8–0x40D0D).</summary>
    public void ClearLineupSlot(LineupSlot slot)
    {
        if (Lineup[slot] is not { } player)
            return;
        TakeOverDressed();
        Lineup.Clear(slot);
        _dressed.Remove(player.Id);
        DisableAutoLineup();
    }

    /// <summary>Alle Plätze leeren („C“); die Aufgestellten sind danach nicht eingesetzt.</summary>
    public void ClearLineup()
    {
        foreach (var slot in LineupSlot.All)
            ClearLineupSlot(slot);
    }

    /// <summary>
    /// Auto-Aufstellung („A“): Einstellung „automatisch aufstellen“ ein und sofort neu aufstellen (Datei 0x426BC,
    /// <see cref="AutoLineupRule"/>). [APPROX] Im Original schaltet der Taktikbildschirm die Einstellung ein, gefolgt von
    /// derselben Neuaufstellung (Datei 0x48E62, <c>docs/analyse/re-geld.md</c> Taktik Schritt 5).
    /// </summary>
    public void AutoLineup()
    {
        _autoLineup = true;
        ApplyAutoLineup();
    }

    /// <summary>Linksklick in der Kaderliste: eingesetzten, nicht aufgestellten Spieler herausnehmen (Datei 0x40F59).</summary>
    public LineupResult Undress(Player player)
    {
        if (!Players.Contains(player) || Lineup.Contains(player) || !IsDressed(player))
            return LineupResult.Unchanged;
        TakeOverDressed();
        _dressed.Remove(player.Id);
        DisableAutoLineup();
        return LineupResult.Undressed;
    }

    /// <summary>
    /// Neuaufstellung nach Verletzung oder Heilung (<see cref="PlayerInjuries.RefillLineup"/>). [ORIGINAL] Datei 0x5C50
    /// (Verletzung 0x05B1A): der Verletzte verliert +0x0A (nicht mehr eingesetzt). Datei 0x426BC setzt höchstens 22 ein
    /// (Zähler [bp-9] ab 1, Vergleich mit 0x17 bei 0x427C0/0x4282C). [ORIGINAL] A189: Mit der Einstellung „automatisch
    /// aufstellen“ stellt 0x426BC nach dem Spieltag ganz neu auf (Datei 0x54D8C–0x54D9E, 0x552F9–0x5530B; FILE24
    /// „Eingesetzt: 22“, <see cref="ApplyAutoLineup"/>). [APPROX] Ohne die Einstellung schreibt im Original im Verletzungsweg
    /// niemand +0x15 (Schreiber nur 0x426BC, Oberfläche, Transfer, Saisonwechsel) – keine Neuaufstellung, die Lücke bliebe
    /// vermutlich; kein Stand im Handmodus belegt das. Das Remake füllt dann zuerst mit eingesetzten Reservisten, andere nur,
    /// solange weniger als 22 eingesetzt sind.
    /// </summary>
    public void RefillLineup()
    {
        var removed = Lineup.Players.Where(p => !p.Available || !Players.Contains(p)).ToList();
        Lineup.RemoveUnavailable(Players);
        if (ApplyAutoLineup())
            return;
        if (!_dressedByHand)
        {
            Lineup.FillGaps(Players);
            return;
        }
        foreach (var p in removed)
            _dressed.Remove(p.Id);
        var before = Dressed.ToHashSet();
        Lineup.FillGaps(Players.Where(before.Contains));
        var room = MaxDressed - before.Count;
        if (room <= 0)
            return;
        Lineup.FillGaps(Players);
        var added = Lineup.Players.Where(p => !before.Contains(p))
            .OrderByDescending(TeamStrength.PlayerValue).ThenBy(p => p.Number).ToList();
        foreach (var p in added.Skip(room))
            Lineup.Clear(Lineup.FindSlot(p)!.Value);
        foreach (var p in added.Take(room))
            _dressed.Add(p.Id);
    }

    /// <summary>Rechtsklick in der Kaderliste: nicht eingesetzten Spieler einsetzen (Datei 0x40E2A, 0x41208).</summary>
    public LineupResult Dress(Player player)
    {
        if (!Players.Contains(player) || IsDressed(player))
            return LineupResult.Unchanged;
        if (DressedCount >= MaxDressed)
            return LineupResult.RosterFull;
        TakeOverDressed();
        _dressed.Add(player.Id);
        DisableAutoLineup();
        return LineupResult.Dressed;
    }
}
