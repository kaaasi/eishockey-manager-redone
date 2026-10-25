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
