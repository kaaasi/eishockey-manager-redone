using EishockeyManager.Formats;

namespace EishockeyManager.Core;

/// <summary>
/// Bildschirmzustand des Aufstellungsgitters (K07, A971/A972). [ORIGINAL] Das Original führt das Kennzeichen „sechster Platz offen“
/// (<c>[bp−0x0E]</c>) und das Gitter (<c>[bp−0x46]</c>, 5 Blöcke × 6 Zellen) als Variablen der Aufstellung 0x40364, nicht im
/// Spielstand (Beleg <c>tools/reverse-engineering/abweichungen/k07_sechster.py</c>):
/// <list type="bullet">
/// <item>Aufbau (Datei 0x403DB–0x404FD): gesetzt genau dann, wenn Reihe 1 sechs Plätze mit +0x15 zählt (<c>[bp−0x0F]</c>) und die
///   Torhüterzelle (<c>[bp−0x2A]</c>) leer ist (<see cref="Build"/>).</item>
/// <item>Öffnen durch einen Klick in die Kaderliste (Datei 0x413C9–0x414B4, <see cref="Lineup.TryOpenSixthSkater"/>): gesetzt.</item>
/// <item>Leeren einer Zelle der Reihe 1 bei gesetztem Kennzeichen (Datei 0x40B69–0x40C98): <b>jede</b> Zelle – auch eine
///   Sturmzelle – bekommt Zelle 5, Zelle 5 = 0, Kennzeichen 0; der sechste Feldspieler behält +0x15 = 1 (Abwehr). Bei einer
///   Sturmzelle zeigt das Gitter ihn dort (<see cref="SixthShownAt"/>).</item>
/// <item>Herausnehmen aus der Kaderliste (Datei 0x41032–0x41049): nur bei einer Abwehrzelle der Reihe 1 (si &gt; 2) Zelle 5 dorthin
///   und Kennzeichen 0, ohne Blick auf das Kennzeichen.</item>
/// </list>
/// Das Remake hält diesen Zustand im Bildschirm (AGENTS.md §4: Öffnen ändert nichts) und reicht ihn den Regeln von
/// <see cref="Club.ClickPlayer"/> und <see cref="Club.ClearLineupSlot"/> mit.
/// </summary>
public sealed class LineupGrid
{
    /// <summary>Kennzeichen „sechster Platz offen“ (<c>[bp−0x0E]</c>).</summary>
    public bool SixthOpen { get; internal set; }

    private LineupSlot? _shownAt;

    /// <summary>
    /// Sturmzelle der Reihe 1, in der das Gitter den sechsten Feldspieler zeigt (A972: nach dem Leeren dieser Zelle bei offenem
    /// Platz). Der Spieler bleibt auf dem sechsten Platz der Aufstellung (+0x15 = 1).
    /// </summary>
    public LineupSlot? SixthShownAt => _shownAt;

    /// <summary>
    /// Aufbau des Bildschirms. [ORIGINAL] Datei 0x403DB–0x404FD: Reihe 1 hat sechs Plätze mit +0x15 ≠ 0 und kein Torhüter steht.
    /// Die Sturmzelle von <see cref="SixthShownAt"/> gibt es danach nicht mehr: der neue Aufbau ordnet nach +0x15 (Abwehr), der
    /// sechste steht dann in Zelle 5 und ist bei geschlossenem Platz unsichtbar (Fall d).
    /// </summary>
    public static LineupGrid Build(Lineup lineup)
    {
        ArgumentNullException.ThrowIfNull(lineup);
        return new LineupGrid
        {
            SixthOpen = lineup.SixthSkater is not null && lineup.Goalkeeper is null && lineup.Block(0).Count() == 5,
        };
    }

    /// <summary>Gilt <see cref="SixthShownAt"/> noch (der sechste steht weiter auf seinem Platz, die Sturmzelle ist leer)?</summary>
    internal bool ShowsSixthAt(Lineup lineup, LineupSlot slot)
    {
        if (_shownAt is not { } at || at != slot)
            return false;
        if (lineup.SixthSkater is null || lineup[at] is not null)
        {
            _shownAt = null;
            return false;
        }
        return true;
    }

    /// <summary>Platz der Aufstellung hinter einer Zelle des Gitters (die Sturmzelle von <see cref="SixthShownAt"/> zeigt den sechsten).</summary>
    public LineupSlot SlotBehind(Lineup lineup, LineupSlot cell) => ShowsSixthAt(lineup, cell) ? LineupSlot.SixthSkater : cell;

    /// <summary>Spieler, den das Gitter in der Zelle zeigt.</summary>
    public Player? PlayerAt(Lineup lineup, LineupSlot cell)
    {
        ArgumentNullException.ThrowIfNull(lineup);
        return lineup[SlotBehind(lineup, cell)];
    }

    internal void ForgetShown() => _shownAt = null;

    internal void ShowSixthAt(LineupSlot slot) => _shownAt = slot;
}

public sealed partial class Lineup
{
    /// <summary>
    /// Zelle des Gitters leeren (Klick auf einen Platz, Datei 0x40B69–0x40C98; siehe <see cref="LineupGrid"/>). Ohne Gitter wie
    /// <see cref="Clear(LineupSlot)"/>.
    /// </summary>
    public void Clear(LineupSlot cell, LineupGrid? grid)
    {
        if (grid is null)
        {
            Clear(cell);
            return;
        }
        var slot = grid.SlotBehind(this, cell);
        if (slot != cell)
        {
            // Die Sturmzelle zeigt den sechsten (A972): sie leeren heißt ihn herausnehmen.
            grid.ForgetShown();
            Clear(slot);
            return;
        }
        var lineOne = slot.Role != PlayerRole.Goalkeeper && slot.Block == 0;
        if (grid.SixthOpen && lineOne)
        {
            // [ORIGINAL] Datei 0x40B95: jede Zelle der Reihe 1 bekommt Zelle 5, Kennzeichen 0 (A971).
            grid.SixthOpen = false;
            if (slot.Role == PlayerRole.Forward)
            {
                this[slot] = null;
                if (_sixthSkater is not null)
                    grid.ShowSixthAt(slot);
                return;
            }
        }
        Clear(slot);
        if (_sixthSkater is null)
            grid.ForgetShown();
    }

    /// <summary>
    /// Spieler aus der Kaderliste herausnehmen (Datei 0x41032–0x41049; siehe <see cref="LineupGrid"/>): nur eine Abwehrzelle der
    /// Reihe 1 schließt den Platz.
    /// </summary>
    internal void Remove(LineupSlot from, LineupGrid? grid)
    {
        Clear(from);
        if (grid is null)
            return;
        if (from.Role == PlayerRole.Defender && from.Block == 0)
            grid.SixthOpen = false;
        if (_sixthSkater is null)
            grid.ForgetShown();
    }

    /// <summary>
    /// Ist der Platz für die Suche frei? Die Sturmzelle von <see cref="LineupGrid.SixthShownAt"/> ist im Original belegt (Zelle 5
    /// steht dort, Datei 0x412A1–0x41381).
    /// </summary>
    internal bool IsFreeCell(LineupSlot slot, LineupGrid? grid) => this[slot] is null && grid?.ShowsSixthAt(this, slot) != true;

    /// <summary>
    /// Kennzeichen für die Abweisung „Schon 6 Feldspieler / auf dem Eis !“ (Datei 0x40EC4–0x40EFF): das des Gitters, ohne Gitter
    /// <see cref="SixthSkaterOpen"/>.
    /// </summary>
    internal bool SixthOpen(LineupGrid? grid) => grid?.SixthOpen ?? SixthSkaterOpen;

    /// <summary>
    /// Sechsten Platz öffnen (<see cref="TryOpenSixthSkater(int, Player)"/>) und das Kennzeichen setzen (Datei 0x414B4). Die
    /// Sturmzelle von <see cref="LineupGrid.SixthShownAt"/> zählt als belegt.
    /// [APPROX] Zeigt das Gitter den sechsten in einer Sturmzelle (A972), öffnet das Original den Platz erneut (Kennzeichen 0,
    /// Torhüterzelle leer) und stellt einen siebten Feldspieler mit +0x15 = 1 in Zelle 5; das Remake hat dafür keinen Platz
    /// („Kein / Platz mehr !“).
    /// </summary>
    internal bool TryOpenSixthSkater(int line, Player player, LineupGrid? grid)
    {
        if (grid?.SixthShownAt is not null && grid.ShowsSixthAt(this, grid.SixthShownAt.Value))
            return false;
        if (!TryOpenSixthSkater(line, player))
            return false;
        if (grid is not null)
            grid.SixthOpen = true;
        return true;
    }
}
