using System.Globalization;
using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.UI.Screens.Team;

/// <summary>
/// Verträge (Hauptmenü → Mannschaft → Verträge) mit Vertragsverhandlung. Vermessen aus dem UI-Video
/// (ca. 10:20–10:55): Fenster (1,14,264,209), Spalten ART NAME SP ST TO STATUS E V.DAUER WERT,
/// Klick auf einen Spieler öffnet die Verhandlung (grauer Rahmen 166 × 67, unter bzw. über der Zeile).
/// [ORIGINAL] Bildschirm em.exe 0x3FFE0 (Ov 0x108), Verhandlung 0x4A1DB (Aufruf 0x40208, Lage nach Zeile &lt; 12): Regeln in
/// <see cref="Contracts.Negotiate"/>; Prüfung beim Anklicken von Laufzeit und Gehalt (<see cref="Contracts.Availability"/>);
/// Eingabe der Laufzeit füllt das Gehalt mit der Forderung (<see cref="Contracts.DemandAfterYears"/>).
/// </summary>
public sealed class ContractsScreen : Screen
{
    public static readonly Rect Window = new(1, 14, 264, 209);
    public const int HeaderY = 20;
    public const int FirstRowY = 29;
    public const int RowPitch = 6;
    public const int StatusY = 213;

    /// <summary>Lage der Verhandlung: Zeilen im oberen Teil → darunter (y = 109), sonst darüber (y = 29).</summary>
    public static Rect DialogRect(int row) => new(1, FirstRowY + row * RowPitch < 100 ? 109 : 29, 166, 67);

    public static Rect YearsField(Rect dialog) => new(76, dialog.Y + 26, 80, 7);
    public static Rect SalaryField(Rect dialog) => new(76, dialog.Y + 34, 80, 7);
    public static Rect InsuranceField(Rect dialog) => new(76, dialog.Y + 42, 80, 7);
    public static Rect AcceptButton(Rect dialog) => Widgets.ButtonRect(11, dialog.Y + 51);
    public static Rect CancelButton(Rect dialog) => Widgets.ButtonRect(90, dialog.Y + 51);

    private enum Field { None, Years, Salary }

    private readonly Screen _back;
    private readonly MessageBox _message = new();
    private Player? _player;
    private int _row;
    private int _years, _salary; // eingegebene Werte, 0 = keine Eingabe (wie im Original)
    private bool _insured, _demandFilled;
    private Field _edit;
    private string _editText = "";
    private int _mouseX = -1, _mouseY = -1;
    private string? _status;

    public ContractsScreen(ScreenHost host, Screen back) : base(host) => _back = back;

    private Club Club => TeamUi.OwnClub(Host);

    /// <summary>Offene Meldung (z. B. „… ist im Moment nicht verhandlungsbereit.“).</summary>
    public MessageBox Message => _message;

    /// <summary>Text der Statuszeile (Hinweiszeile 0x41683) nach dem letzten Bild.</summary>
    public string? Status => _status;

    /// <summary>Spieler, mit dem gerade verhandelt wird (oder null).</summary>
    public Player? Negotiating => _player;

    public static int RowAt(int y) => y < FirstRowY - 1 ? -1 : (y - FirstRowY + 1) / RowPitch;

    public override void Update(InputState input)
    {
        _mouseX = input.MouseX;
        _mouseY = input.MouseY;
        // Im Video bleibt der letzte Text stehen, solange die Maus im Dialog nichts Erklärbares trifft
        // („Status: RESERVE“ über „keine“).
        var status = HoverText(TeamUi.Rows(Club));
        if (status is not null || _player is null)
            _status = status;
        if (_message.Update(input))
            return;
        if (_player is not null)
        {
            UpdateDialog(input);
            return;
        }

        if (input.Pressed(Key.Escape) || input.RightClick)
        {
            Host.Show(_back);
            return;
        }
        if (!input.LeftClick)
            return;
        if (!Window.Contains(input.MouseX, input.MouseY))
        {
            Host.Show(_back);
            return;
        }

        var rows = TeamUi.Rows(Club);
        var row = RowAt(input.MouseY);
        if (row < 0 || row >= rows.Count)
            return;
        // [ORIGINAL] Bildschirm 0x3FFE0 (Datei 0x4015D–0x401D3): ausgeliehen bzw. Ruhestand angekündigt → Meldung statt
        // Verhandlung. [APPROX] Die Absage der Spur „kader“ (R2-11) ebenso. Die Sperre meldet erst der Dialog (0x4A704).
        var p = rows[row];
        if (p.OnLoan || p.Notice is ContractNotice.Refused or ContractNotice.Retiring)
        {
            _message.Show(Contracts.Message(Contracts.Availability(Host.Game!, p)!.Value, p));
            return;
        }
        _player = p;
        _row = row;
        _years = 0;
        _salary = 0;
        _insured = _player.Insured;
        _demandFilled = false;
        _edit = Field.None;
    }

    private void UpdateDialog(InputState input)
    {
        var dialog = DialogRect(_row);
        if (_edit != Field.None)
        {
            foreach (var ch in input.Text)
                if (char.IsAsciiDigit(ch) && _editText.Length < (_edit == Field.Years ? 1 : 7))
                    _editText += ch;
            if (input.Pressed(Key.Backspace) && _editText.Length > 0)
                _editText = _editText[..^1];
            if (input.Pressed(Key.Enter))
            {
                CommitEdit();
                return;
            }
            if (input.Pressed(Key.Escape))
            {
                _edit = Field.None;
                return;
            }
            if (input.LeftClick)
                CommitEdit(); // Klick daneben übernimmt die Eingabe und wirkt weiter
            else
                return;
        }

        if (input.Pressed(Key.Escape) || input.RightClick || input.Clicked(CancelButton(dialog)))
        {
            _player = null;
            return;
        }
        if (input.Clicked(YearsField(dialog)) || input.Clicked(SalaryField(dialog)))
        {
            if (Contracts.Availability(Host.Game!, _player!) is { } refusal)
            {
                _message.Show(Contracts.Message(refusal, _player!));
                return;
            }
            StartEdit(input.Clicked(YearsField(dialog)) ? Field.Years : Field.Salary);
        }
        else if (input.Clicked(InsuranceField(dialog)))
            _insured = !_insured;
        else if (input.Clicked(AcceptButton(dialog)) || input.Pressed(Key.Enter))
            Offer();
    }

    private void StartEdit(Field field)
    {
        _edit = field;
        _editText = "";
    }

    /// <summary>
    /// Eingabe übernehmen. [ORIGINAL] Laufzeit (0x4A7C5): leere Eingabe = bisherige Laufzeit ohne Forderung, sonst Forderung ins Gehaltsfeld
    /// (nicht im Level 5) und Merker „Forderung steht im Feld“; Gehalt (0x4A9F5): leere Eingabe = bisheriges Gehalt, Merker
    /// gelöscht. Werte über 4 Jahre bleiben stehen und scheitern erst bei „Mach' ich!“.
    /// </summary>
    private void CommitEdit()
    {
        var p = _player!;
        var value = 0;
        var empty = _editText.Length == 0 || !int.TryParse(_editText, out value);
        if (_edit == Field.Years && empty)
        {
            _years = Contracts.CurrentYears(p); // 0x4A808: leere Eingabe, keine Forderung, Merker gelöscht
            _demandFilled = false;
        }
        else if (_edit == Field.Years)
        {
            _years = Math.Max(value, Contracts.MinYears);
            var game = Host.Game!;
            if (Contracts.DemandAfterYears(game, game.ActiveManager, p, _years) is { } demand)
                _salary = demand;
            _demandFilled = true;
        }
        else if (_edit == Field.Salary)
        {
            _salary = empty ? p.MonthlySalary : Math.Max(value, Contracts.MinSalary); // Video: 0 DM wird zu 1 DM
            _demandFilled = false;
        }
        _edit = Field.None;
    }

    /// <summary>„Mach' ich!“: Verhandlung nach 0x4AB66; der Dialog schließt in jedem Fall, Absagen mit Meldung.</summary>
    private void Offer()
    {
        var game = Host.Game!;
        var p = _player!;
        var result = Contracts.Negotiate(game, game.ActiveManager, p, _years, _salary, _insured, _demandFilled);
        if (result != NegotiationResult.Accepted)
            _message.Show(Contracts.Message(result, p));
        _player = null;
    }

    public override void Draw(Surface s)
    {
        var mini = Assets.Mini;
        var club = Club;
        s.Palette = Assets.Palette;
        s.Blit(Assets.Image(MainMenuScreen.Background), 0, 0);
        Widgets.Frame(s, Assets, Window, FrameStyle.Orange);

        const byte h = TeamUi.HeaderColor;
        s.Text(mini, 6, HeaderY, "ART", h);
        s.Text(mini, 26, HeaderY, "NAME", h);
        s.Text(mini, 82, HeaderY, "SP", h);
        s.Text(mini, 97, HeaderY, "ST", h);
        s.Text(mini, 109, HeaderY, "TO", h);
        s.Text(mini, 123, HeaderY, "STATUS", h);
        s.Text(mini, 162, HeaderY, "E", h);
        s.Text(mini, 171, HeaderY, "V.DAUER", h);
        s.Text(mini, 217, HeaderY, "WERT", h);
        s.HLine(6, 25, 246, TeamUi.LineColor);
        s.HLine(6, 210, 246, TeamUi.LineColor);

        var rows = TeamUi.Rows(club);
        var hoverRow = _player is null && Window.Contains(_mouseX, _mouseY) ? RowAt(_mouseY) : -1;
        for (var i = 0; i < rows.Count; i++)
        {
            var p = rows[i];
            var y = FirstRowY + i * RowPitch;
            var c = TeamUi.RoleColor(p.Role);
            if (i == hoverRow)
                TeamUi.HoverRow(s, 6, y, 254, RowPitch);
            s.Text(mini, 7, y, p.RoleShort, c);
            s.Text(mini, 25, y, p.Name, c);
            s.TextRight(mini, 91, y, p.Games.ToString(), c);
            s.TextRight(mini, 106, y, p.Strength.ToString(), c);
            s.TextRight(mini, 117, y, p.Role == PlayerRole.Goalkeeper ? "-" : p.Goals.ToString(), c);
            s.Text(mini, 123, y, TeamUi.StatusText(club.StatusOf(p)), c);
            s.TextRight(mini, 169, y, p.Energy.ToString(), c);
            s.Text(mini, 177, y, p.ContractText, c);
            s.TextRight(mini, 259, y, $"{p.MonthlySalary} DM", c);
        }

        if (_player is not null)
            DrawDialog(s);
        TeamUi.StatusLine(s, Assets, 131, StatusY, _status);
        _message.Draw(s, Assets, 133, 118);
    }

    private void DrawDialog(Surface s)
    {
        var mini = Assets.Mini;
        var p = _player!;
        var d = DialogRect(_row);
        TeamUi.DialogFrame(s, d);
        TeamUi.DialogTitle(s, Assets, 84, d.Y + 5, "Vertragsverhandlung");
        s.HLine(5, d.Y + 14, 159, Colors.White);
        s.TextCentered(mini, 80, d.Y + 18, $"{p.Name}, {p.Age} Jahre alt", Colors.White); // Video: Mitte bei x = 79–80

        const byte label = Colors.White;
        s.Text(mini, 7, d.Y + 27, "Vertragsdauer", label);
        s.Text(mini, 7, d.Y + 35, "Gehalt / Monat", label);
        s.Text(mini, 7, d.Y + 43, "Versicherung", label);
        for (var i = 0; i < 3; i++)
            s.Text(mini, 77, d.Y + 27 + 8 * i, ":", Colors.White);

        DrawValue(s, Field.Years, d.Y + 27, Player.YearsText(_years != 0 ? _years : DisplayedYears(p)));
        DrawValue(s, Field.Salary, d.Y + 35, FormatMoney(_salary != 0 ? _salary : p.MonthlySalary));
        s.Text(mini, 81, d.Y + 43, _insured ? "abgeschlossen" : "keine", Colors.White); // [APPROX] Text für „mit Versicherung“

        TeamUi.DialogButton(s, Assets, AcceptButton(d).X, AcceptButton(d).Y, "Mach' ich!");
        TeamUi.DialogButton(s, Assets, CancelButton(d).X, CancelButton(d).Y, "Abbruch");
    }

    private void DrawValue(Surface s, Field field, int y, string text)
    {
        if (_edit != field)
        {
            s.Text(Assets.Mini, 81, y, text, Colors.White);
            return;
        }
        var x = s.Text(Assets.Mini, 81, y, _editText, Colors.White);
        s.Fill(new Rect(x + 1, y, 3, 5), 1); // Eingabemarke wie im Video
    }

    /// <summary>
    /// Laufzeit im Dialog ohne Eingabe: die bisherige, bei einem Verlängerungsangebot des Spielers die angebotene
    /// (Original: Kaderplatz +0x18 &gt; 99 → +0x0B = +0x18 − 100, Datei 0x4A3AF).
    /// </summary>
    private static int DisplayedYears(Player p) => Contracts.CurrentYears(p);

    /// <summary>„1.500 DM“ (im Dialog mit Tausenderpunkt, in der Liste ohne).</summary>
    public static string FormatMoney(int amount) =>
        amount.ToString("#,0", CultureInfo.GetCultureInfo("de-DE")) + " DM";

    private string? HoverText(IReadOnlyList<Player> rows)
    {
        if (_player is not null)
        {
            var d = DialogRect(_row);
            if (YearsField(d).Inflate(0).Contains(_mouseX, _mouseY) || new Rect(6, d.Y + 26, 70, 7).Contains(_mouseX, _mouseY))
                return "Dauer des Vertrages mit dem Spieler (in Saisons)";
            if (SalaryField(d).Contains(_mouseX, _mouseY) || new Rect(6, d.Y + 34, 70, 7).Contains(_mouseX, _mouseY))
                return "Monatliches Gehalt des Spielers";
            return null;
        }
        if (!Window.Contains(_mouseX, _mouseY))
            return null;
        var layout = Layout(rows.Count);
        var row = PlayerHintLine.Row(layout, _mouseY);
        var column = PlayerHintLine.Column(layout, _mouseX);
        if (row < 0 || row >= rows.Count || column < 0)
            return null;
        var p = rows[row];
        return PlayerHintLine.Text(p, column, new PlayerHintInfo(Club.IsDressed(p), LineOf(p)), showExhaustion: false);
    }

    /// <summary>
    /// Maße der Liste für die Hinweiszeile. [ORIGINAL] em.exe Datei 0x400B4–0x400E0 (in 0x3FFE0): Ursprung 7 | 29, Rand 0xFF,
    /// Spaltenmaske 0x3D146 (Art, Name, Spiele, Durchschnittsstärke, Tore, Status, Tendenz, Vertragsdauer, Gehalt).
    /// </summary>
    public static PlayerListLayout Layout(int rows) => new(7, 29, rows - 1, 0xFF, 0x3D146);

    /// <summary>Reihe (1–4), in der der Spieler aufgestellt ist, sonst 0 (Kaderplatz +0x15 &amp; 0x7F).</summary>
    private int LineOf(Player p)
    {
        foreach (var slot in LineupSlot.All)
            if (Club.Lineup[slot] == p)
                return slot.Block + 1;
        return 0;
    }
}

/// <summary>Anmeldung im Hauptmenü.</summary>
internal static class ContractsMenuEntries
{
    [MenuEntry("Verträge")]
    private static Screen Open(ScreenHost host, Screen back) => new ContractsScreen(host, back);
}
