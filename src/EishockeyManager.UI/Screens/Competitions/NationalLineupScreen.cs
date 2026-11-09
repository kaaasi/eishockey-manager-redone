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
/// Bedienung wie im Original (A010, Spur nationalteam Runde 13; Regeln <see cref="NationalCoach.ClickPlayer"/>): Klick auf
/// einen Block bzw. den Torhüterkasten wählt die Reihe (Nummer in Farbe 0x10), Klick auf einen Spieler der Liste setzt ihn
/// auf den ersten freien Platz seiner Art in dieser Reihe oder nimmt einen Aufgestellten heraus; Linksklick auf einen belegten
/// Platzknopf leert ihn (der Spieler ist danach nicht mehr eingesetzt), Rechtsklick außerhalb der Listenzeilen – auch auf
/// einen Platz – verlässt den Bildschirm (Spur nationalteam Runde 14 Teil 4, Hinweis aus #667). Tasten A und C wie beim
/// Verein. [APPROX] Die Spielerinfo auf Rechtsklick (Datei 0x40872) ist nicht übernommen; Escape führt wie der Rechtsklick
/// außerhalb der Liste zurück.
/// </para>
/// </remarks>
public sealed class NationalLineupScreen : Screen
{
    /// <summary>Farbe der Zeile „Eingesetzt: n“. [ORIGINAL] Datei 0x41D84 (Farbe 0x10).</summary>
    public const byte DressedColor = 16;

    /// <summary>Spalte E bei Belastung über 130. [ORIGINAL] Datei 0x4233F (<c>mov byte [bp-0x6], 0x10</c>).</summary>
    public const byte ExhaustionColor = 0x10;

    private readonly Screen _back;
    private readonly MessageBox _message;
    /// <summary>[ORIGINAL] Farbe der Nummer der gewählten Reihe (Datei 0x4090A, sonst 2 Datei 0x408D3).</summary>
    public const byte SelectedLineColor = 0x10;

    private int? _selectedLine;
    private bool _changed;
    private int _mouseX = -1, _mouseY = -1;

    public NationalLineupScreen(ScreenHost host, Screen back) : base(host)
    {
        _back = back;
        _message = new(() => host.Game?.ActiveManager.Name ?? "");
    }

    private NationalCoach? Coach => Host.Game?.NationalCoach;

    /// <summary>Gewählte Reihe 0–3 bzw. <see cref="NationalCoach.GoalkeeperLine"/>, null = keine (DGROUP [bp−9] = 0xFF).</summary>
    public int? SelectedLine => _selectedLine;

    /// <summary>Meldungsfenster bzw. Kurzmeldung des Bildschirms.</summary>
    public MessageBox Message => _message;

    public override void Update(InputState input)
    {
        _mouseX = input.MouseX;
        _mouseY = input.MouseY;
        if (_message.Update(input))
            return;
        if (Coach is not { } coach || input.Pressed(Key.Escape))
        {
            Leave(_back);
            return;
        }

        var rows = coach.Rows;
        switch (input.Text.ToUpperInvariant())
        {
            case "A":
                coach.AutoFormation();
                _changed = true;
                return;
            case "C":
                coach.ClearFormation();
                _changed = true;
                return;
        }

        if (input.LeftClick && TeamUi.SwitchButton.Contains(input.MouseX, input.MouseY))
        {
            Leave(Tactics(Host, _back));
            return;
        }

        var inList = LineupScreen.Window.Contains(input.MouseX, input.MouseY);
        var row = inList ? LineupScreen.RowAt(input.MouseY) : -1;
        var player = row >= 0 && row < rows.Count ? rows[row] : null;
        if (!input.LeftClick && !input.RightClick)
            return;
        if (player is null)
        {
            // [ORIGINAL] Datei 0x40886–0x4088D: nur die linke Taste (DGROUP 0x5A16 = 1) erreicht Blöcke und Plätze; die rechte
            // (3) außerhalb der Listenzeilen – auch auf einem Platz – verlässt den Bildschirm (Datei 0x40D6B–0x40D78, Zeile 0x7F
            // aus 0x41683).
            if (input.RightClick)
                Leave(_back);
            else
                ClickLines(coach, input.MouseX, input.MouseY);
            return;
        }
        var selected = _selectedLine;
        var result = coach.ClickPlayer(player, ref selected, right: input.RightClick);
        _selectedLine = selected;
        if (result is LineupResult.Assigned or LineupResult.TakenOut or LineupResult.Dressed or LineupResult.Undressed)
            _changed = true;
        Report(result);
    }

    /// <summary>
    /// Linksklick neben der Liste. [ORIGINAL] Datei 0x40890–0x40943: ein Block bzw. der Torhüterkasten wählt die Reihe. Datei
    /// 0x40AA3–0x40D0D: Klick auf einen belegten Platz schaltet „automatisch aufstellen“ ab und meldet das (Datei 0x40ACF–0x40AF3,
    /// „Auto-Aufstellung / ist deaktiviert !“, DGROUP 0x1249/0x124D); ein Verletzter oder Gesperrter mit Restdauer (+0x0D ≠ 0,
    /// +0x09 &amp; 3 ≠ 0) bleibt mit „Spieler ist“ / „noch gesperrt !“ stehen (Datei 0x40B37–0x40B5D), sonst wird der Platz leer
    /// und der Spieler ist nicht mehr eingesetzt (Datei 0x40CF8–0x40D0D). Ein leerer Platz ändert nichts (Datei 0x40AC4).
    /// </summary>
    private void ClickLines(NationalCoach coach, int x, int y)
    {
        if (LineAt(x, y) is { } line)
            _selectedLine = line;
        if (!OnSlot(x, y, out var slot) || coach.Formation[slot] is not { } placed)
            return;
        var wasAuto = coach.AutoLineupEnabled;
        if (placed.InjuredGames > 0 || placed.SuspendedGames > 0)
        {
            coach.SwitchOffAutoLineup();
            _message.ShowShort("Spieler ist", "noch gesperrt !"); // ersetzt die Kurzmeldung davor
            return;
        }
        coach.ClearSlot(slot);
        _changed = true;
        if (wasAuto)
            _message.ShowShort("Auto-Aufstellung", "ist deaktiviert !");
    }

    /// <summary>Taktik des Nationalteams (gleicher Bildschirm wie beim Verein), Umschalter zurück zur Aufstellung.</summary>
    public static Screen Tactics(ScreenHost host, Screen back) =>
        new TacticsScreen(host, back, () => host.Game!.NationalCoach!.Tactics, () => new NationalLineupScreen(host, back));

    /// <summary>
    /// Verlassen (Datei 0x41506–0x4151A): 0x952C schreibt die Länderwerte aus der Aufstellung
    /// (<see cref="GameState.OnNationalLineupLeft"/>), hier nur nach einer Änderung (siehe dort, [APPROX]).
    /// </summary>
    private void Leave(Screen next)
    {
        if (_changed)
            Host.Game?.OnNationalLineupLeft();
        _changed = false;
        Host.Show(next);
    }

    /// <summary>
    /// Meldungen wie beim Verein (gleicher Bildschirm 0x40364, Kurzmeldung 0x0C0B8; <see cref="LineupScreen"/>): „Spieler ist“ /
    /// „Verletzt.“ bzw. „Gesperrt.“ (Datei 0x40E01–0x40E27), „Schon 22 Mann / im Team !“ (DGROUP 0x11D5), „Keine Reihe /
    /// ausgewählt !“ (DGROUP 0x1221), „Der darf da / nicht rein !“ (DGROUP 0x1229), „Kein / Platz mehr !“ (Datei 0x414C6).
    /// </summary>
    private void Report(LineupResult result)
    {
        switch (result)
        {
            case LineupResult.Injured:
                _message.ShowShort("Spieler ist", "Verletzt.");
                break;
            case LineupResult.Suspended:
                _message.ShowShort("Spieler ist", "Gesperrt.");
                break;
            case LineupResult.WrongRole:
                _message.ShowShort("Der darf da", "nicht rein !");
                break;
            case LineupResult.RosterFull:
                _message.ShowShort("Schon 22 Mann", "im Team !");
                break;
            case LineupResult.NoLineSelected:
                _message.ShowShort("Keine Reihe", "ausgewählt !");
                break;
            case LineupResult.NoRoom:
                _message.ShowShort(ShortMessageTexts.NoRoom.Line1, ShortMessageTexts.NoRoom.Line2);
                break;
        }
    }

    /// <summary>Reihe unter der Maus: Block 0–3 oder Torhüterkasten (<see cref="NationalCoach.GoalkeeperLine"/>).</summary>
    public static int? LineAt(int x, int y)
    {
        for (var b = 0; b < Lineup.Blocks; b++)
            if (LineupScreen.BlockBox(b).Contains(x, y))
                return b;
        return LineupScreen.GoalieBox.Contains(x, y) ? NationalCoach.GoalkeeperLine : null;
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
            if (i == hoverRow)
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
            // [ORIGINAL] Spalte E = Belastung · 10 / 15, Farbe 0x10 über 130 (Datei 0x42335–0x4236D; A184, Runde 12).
            s.TextRight(mini, 231, y, TrainingRules.ExhaustionDisplay(p).ToString(), TrainingRules.ExhaustionWarning(p) ? ExhaustionColor : c);
        }
        TeamUi.StatusLine(s, Assets, 131, LineupScreen.StatusY, HoverText(coach, rows));

        for (var b = 0; b < Lineup.Blocks; b++)
        {
            Widgets.Frame(s, Assets, LineupScreen.BlockBox(b), FrameStyle.Orange);
            s.Text(mini, 251, 33 + 42 * b, $"{b + 1}.", b == _selectedLine ? SelectedLineColor : TeamUi.DimColor);
        }
        Widgets.Frame(s, Assets, LineupScreen.GoalieBox, FrameStyle.Orange);
        // [ORIGINAL] Zahl der eingesetzten Spieler (0x120:0x66 Modus 3, Datei 0x41D20) in Farbe 16 (Datei 0x41D84,
        // Video: dunkles Weinrot); die Vereinsaufstellung nimmt 17.
        s.TextCentered(mini, 282, 189, $"Eingesetzt: {coach.DressedCount}", DressedColor);
        s.Text(mini, 250, 202, "TW", _selectedLine == NationalCoach.GoalkeeperLine ? SelectedLineColor : TeamUi.DimColor);

        foreach (var slot in LineupSlot.All)
        {
            var r = LineupScreen.SlotButton(slot);
            var number = coach.Formation[slot] is { } player ? coach.NumberOf(player)?.ToString() ?? "" : "";
            TeamUi.SmallButton(s, Assets, r.X, r.Y, number, pressed: false);
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
