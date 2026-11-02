using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.UI.Screens.Team;

namespace EishockeyManager.UI.Screens.Competitions;

/// <summary>
/// Aufstellung des Nationalteams (Menü des Bundestrainers → Mannschaft → Aufstellung). Spur nationalteam-2.
/// </summary>
/// <remarks>
/// [ORIGINAL] Derselbe Bildschirm wie beim Verein (em.exe 0x108:0x2a, Datei 0x40364, Aufruf aus dem Menü mit
/// Kennwert 0), nur mit dem WM-Kader des „Managers 5“: Maße und Bausteine daher aus <see cref="LineupScreen"/>.
/// Unterschiede, die das Video `bundestrainer_00056` zeigt: NR ist die Zeile im Kader (1–29) und steht nur bei
/// eingesetzten Spielern, sonst bleiben NR und STATUS leer; „Eingesetzt“ zählt die Eingesetzten (höchstens 22).
/// Regeln: <see cref="NationalCoach"/> (Datei <c>CupsNational.Lineup.cs</c>).
/// <para>
/// Bedienung: Platz anklicken, dann Spieler (oder umgekehrt); Rechtsklick auf einen Platz leert ihn (der Spieler ist
/// danach nicht mehr eingesetzt); Linksklick auf einen Reservisten nimmt ihn heraus, Rechtsklick auf einen nicht
/// Eingesetzten setzt ihn ein. [APPROX] Ein Linksklick auf einen nicht eingesetzten oder aufgestellten Spieler wählt
/// ihn wie beim Verein aus; die Spielerinfo auf Rechtsklick (Datei 0x40872) ist nicht übernommen. Tasten A und C wie
/// beim Verein.
/// </para>
/// </remarks>
public sealed class NationalLineupScreen : Screen
{
    /// <summary>Farbe der Zeile „Eingesetzt: n“. [ORIGINAL] Datei 0x41D84 (Farbe 0x10).</summary>
    public const byte DressedColor = 16;

    private readonly Screen _back;
    private readonly MessageBox _message;
    private Player? _selectedPlayer;
    private LineupSlot? _selectedSlot;
    private int _mouseX = -1, _mouseY = -1;

    public NationalLineupScreen(ScreenHost host, Screen back) : base(host)
    {
        _back = back;
        _message = new(() => host.Game?.ActiveManager.Name ?? "");
    }

    private NationalCoach? Coach => Host.Game?.NationalCoach;

    public Player? SelectedPlayer => _selectedPlayer;

    public LineupSlot? SelectedSlot => _selectedSlot;

    public override void Update(InputState input)
    {
        _mouseX = input.MouseX;
        _mouseY = input.MouseY;
        if (_message.Update(input))
            return;
        if (Coach is not { } coach || input.Pressed(Key.Escape))
        {
            Host.Show(_back);
            return;
        }

        var rows = coach.Rows;
        switch (input.Text.ToUpperInvariant())
        {
            case "A":
                coach.AutoFormation();
                ClearSelection();
                return;
            case "C":
                coach.ClearFormation();
                ClearSelection();
                return;
        }

        if (input.LeftClick && TeamUi.SwitchButton.Contains(input.MouseX, input.MouseY))
        {
            Host.Show(Tactics(Host, _back));
            return;
        }

        if (OnSlot(input.MouseX, input.MouseY, out var slot))
        {
            if (input.RightClick)
            {
                coach.ClearSlot(slot);
                ClearSelection();
            }
            else if (input.LeftClick)
            {
                if (_selectedPlayer is { } p)
                    TryAssign(coach, slot, p);
                else
                    _selectedSlot = _selectedSlot == slot ? null : slot;
            }
            return;
        }

        var inList = LineupScreen.Window.Contains(input.MouseX, input.MouseY);
        var row = inList ? LineupScreen.RowAt(input.MouseY) : -1;
        var player = row >= 0 && row < rows.Count ? rows[row] : null;
        if (input.RightClick)
        {
            if (player is null)
                Host.Show(_back);
            else if (!coach.IsDressed(player))
                Report(coach.Dress(player), player);
            return;
        }
        if (!input.LeftClick || player is null)
            return;
        if (_selectedSlot is { } target)
            TryAssign(coach, target, player);
        else if (coach.IsDressed(player) && !coach.Formation.Contains(player))
        {
            coach.Undress(player);
            ClearSelection();
        }
        else
            _selectedPlayer = _selectedPlayer == player ? null : player;
    }

    /// <summary>Taktik des Nationalteams (gleicher Bildschirm wie beim Verein), Umschalter zurück zur Aufstellung.</summary>
    public static Screen Tactics(ScreenHost host, Screen back) =>
        new TacticsScreen(host, back, () => host.Game!.NationalCoach!.Tactics, () => new NationalLineupScreen(host, back));

    private void ClearSelection()
    {
        _selectedPlayer = null;
        _selectedSlot = null;
    }

    private void TryAssign(NationalCoach coach, LineupSlot slot, Player player)
    {
        ClearSelection();
        Report(coach.Assign(slot, player), player);
    }

    /// <summary>Meldungen wie beim Verein; „Schon 22 Mann / im Team !“ aus DGROUP 0x11D5.</summary>
    private void Report(LineupResult result, Player player)
    {
        switch (result)
        {
            case LineupResult.Injured:
                _message.Show(player.Name, "ist verletzt.");
                break;
            case LineupResult.Suspended:
                _message.Show(player.Name, "noch gesperrt !");
                break;
            case LineupResult.WrongRole:
                _message.Show("Der darf da", "nicht rein !");
                break;
            case LineupResult.RosterFull:
                _message.Show("Schon 22 Mann", "im Team !");
                break;
        }
    }

    private static bool OnSlot(int x, int y, out LineupSlot slot)
    {
        foreach (var s in LineupSlot.All)
        {
            if (LineupScreen.SlotButton(s).Contains(x, y))
            {
                slot = s;
                return true;
            }
        }
        slot = default;
        return false;
    }

    public override void Draw(Surface s)
    {
        var mini = Assets.Mini;
        s.Palette = Assets.Palette;
        s.Blit(Assets.Image(MainMenuScreen.Background), 0, 0);
        Widgets.Frame(s, Assets, LineupScreen.Window, FrameStyle.Orange);
        if (Coach is not { } coach)
            return;

        const byte h = TeamUi.HeaderColor;
        const int y0 = LineupScreen.HeaderY;
        s.Text(mini, 6, y0, "NR", h);
        s.Text(mini, 18, y0, "ART", h);
        s.Text(mini, 38, y0, "NAME", h);
        s.Text(mini, 95, y0, "SP", h);
        s.Text(mini, 109, y0, "STÄRKEN", h);
        s.Text(mini, 146, y0, "TO", h);
        s.Text(mini, 159, y0, "2m", h);
        s.Text(mini, 172, y0, "5m", h);
        s.Text(mini, 186, y0, "STATUS", h);
        s.Text(mini, 227, y0, "E", h);
        s.HLine(6, 25, 227, TeamUi.LineColor);
        s.HLine(6, 210, 227, TeamUi.LineColor);

        var rows = coach.Rows;
        var hoverRow = LineupScreen.Window.Contains(_mouseX, _mouseY) ? LineupScreen.RowAt(_mouseY) : -1;
        for (var i = 0; i < rows.Count; i++)
        {
            var p = rows[i];
            var y = LineupScreen.FirstRowY + i * LineupScreen.RowPitch;
            var c = TeamUi.RoleColor(p.Role);
            if (i == hoverRow || p == _selectedPlayer)
                TeamUi.HoverRow(s, LineupScreen.ListLeft + 3, y, LineupScreen.ListWidth - 8, LineupScreen.RowPitch);
            if (coach.NumberOf(p) is { } number)
                s.TextRight(mini, 17, y, number.ToString(), c);
            s.Text(mini, 18, y, p.RoleShort, c);
            s.Text(mini, 36, y, p.Name, c);
            s.TextRight(mini, 102, y, p.Games.ToString(), c);
            s.TextRight(mini, 116, y, p.Condition.ToString(), c);
            s.TextRight(mini, 128, y, p.Technique.ToString(), c);
            s.TextRight(mini, 140, y, p.Form.ToString(), c);
            s.TextRight(mini, 152, y, p.Role == PlayerRole.Goalkeeper ? "-" : p.Goals.ToString(), c);
            s.TextRight(mini, 165, y, p.Penalties2.ToString(), c);
            s.TextRight(mini, 178, y, p.Penalties5.ToString(), c);
            if (coach.StatusOf(p) is { } status)
                s.Text(mini, 185, y, TeamUi.StatusText(status), c);
            s.TextRight(mini, 231, y, p.Energy.ToString(), c);
        }
        TeamUi.StatusLine(s, Assets, 131, LineupScreen.StatusY, HoverText(coach, rows));

        for (var b = 0; b < Lineup.Blocks; b++)
        {
            Widgets.Frame(s, Assets, LineupScreen.BlockBox(b), FrameStyle.Orange);
            s.Text(mini, 251, 33 + 42 * b, $"{b + 1}.", TeamUi.DimColor);
        }
        Widgets.Frame(s, Assets, LineupScreen.GoalieBox, FrameStyle.Orange);
        // [ORIGINAL] Zahl der eingesetzten Spieler (0x120:0x66 Modus 3, Datei 0x41D20) in Farbe 16 (Datei 0x41D84,
        // Video: dunkles Weinrot); die Vereinsaufstellung nimmt 17.
        s.TextCentered(mini, 282, 189, $"Eingesetzt: {coach.DressedCount}", DressedColor);
        s.Text(mini, 250, 202, "TW", TeamUi.DimColor);

        foreach (var slot in LineupSlot.All)
        {
            var r = LineupScreen.SlotButton(slot);
            var number = coach.Formation[slot] is { } player ? coach.NumberOf(player)?.ToString() ?? "" : "";
            TeamUi.SmallButton(s, Assets, r.X, r.Y, number, pressed: slot == _selectedSlot);
        }

        TeamUi.DrawSwitch(s, Assets, toTactics: true);
        _message.Draw(s, Assets);
    }

    /// <summary>
    /// Text der Statuszeile wie beim Verein; NR ist hier die Platznummer im WM-Kader. [ORIGINAL] Listenzeilen über die
    /// Hinweiszeile em.exe 0x41683 (<see cref="PlayerHintLine"/>): Der Bildschirm ist derselbe wie beim Verein (Datei
    /// 0x40364, siehe Klasse), also auch der Aufruf 0x40845 mit Ursprung 7 | 29, Maske 0xF0C7, Rand 0xE9 (R8W3-06). Ein
    /// Spielerdaten-Fenster (0x041AC) ruft die Aufstellung auch beim Verein am Head nicht; hier daher ebenfalls nicht.
    /// </summary>
    private string? HoverText(NationalCoach coach, IReadOnlyList<Player> rows)
    {
        if (OnSlot(_mouseX, _mouseY, out var slot))
            return coach.Formation[slot] is { } sp ? $"Name: {sp.Name} ({sp.Age} Jahre)" : null;
        var layout = PlayerListLayout.Lineup(rows.Count);
        var row = PlayerHintLine.Row(layout, _mouseY);
        var column = PlayerHintLine.Column(layout, _mouseX);
        if (row < 0 || column < 0)
            return null;
        var p = rows[row];
        return PlayerHintLine.Text(p, column, new PlayerHintInfo(coach.IsDressed(p), LineOf(coach, p)), showExhaustion: true);
    }

    /// <summary>Reihe (1–4), in der der Spieler im Nationalteam aufgestellt ist, sonst 0 (Kaderplatz +0x15 &amp; 0x7F).</summary>
    private static int LineOf(NationalCoach coach, Player p)
    {
        foreach (var slot in LineupSlot.All)
            if (coach.Formation[slot] == p)
                return slot.Block + 1;
        return 0;
    }
}
