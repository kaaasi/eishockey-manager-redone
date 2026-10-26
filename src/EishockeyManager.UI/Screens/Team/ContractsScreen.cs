using System.Globalization;
using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.UI.Screens.Team;

/// <summary>
/// Verträge (Hauptmenü → Mannschaft → Verträge) mit Vertragsverhandlung. Vermessen aus dem UI-Video
/// (ca. 10:20–10:55): Fenster (1,14,264,209), Spalten ART NAME SP ST TO STATUS E V.DAUER WERT,
/// Klick auf einen Spieler öffnet die Verhandlung (grauer Rahmen 166 × 67, unter bzw. über der Zeile).
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
    private int _years, _salary;
    private bool _insured;
    private Field _edit;
    private string _editText = "";
    private int _mouseX = -1, _mouseY = -1;
    private string? _status;

    public ContractsScreen(ScreenHost host, Screen back) : base(host) => _back = back;

    private Club Club => TeamUi.OwnClub(Host);

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
        var p = rows[row];
        if (p.OnLoan)
        {
            _message.Show("Dieser Spieler ist", "nur ausgeliehen.");
            return;
        }
        _player = p;
        _row = row;
        _years = Math.Max(p.ContractYears, Contracts.MinYears);
        _salary = p.MonthlySalary;
        _insured = p.Insured;
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
        if (input.Clicked(YearsField(dialog)))
            StartEdit(Field.Years);
        else if (input.Clicked(SalaryField(dialog)))
            StartEdit(Field.Salary);
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

    private void CommitEdit()
    {
        if (_editText.Length > 0 && int.TryParse(_editText, out var value))
        {
            if (_edit == Field.Years)
                _years = Math.Clamp(value, Contracts.MinYears, Contracts.MaxYears);
            else
                _salary = Math.Max(value, Contracts.MinSalary); // Video: 0 DM wird zu 1 DM
        }
        _edit = Field.None;
    }

    private void Offer()
    {
        var game = Host.Game!;
        var result = Contracts.Offer(_player!, _years, _salary, _insured, game.PlayedMatchDays, game.Random);
        _message.Show(Contracts.Message(result));
        if (result == NegotiationResult.Accepted)
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

        DrawValue(s, Field.Years, d.Y + 27, Player.YearsText(_years));
        DrawValue(s, Field.Salary, d.Y + 35, FormatMoney(_salary));
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
        var row = RowAt(_mouseY);
        if (row < 0 || row >= rows.Count)
            return null;
        var p = rows[row];
        return _mouseX switch
        {
            < 24 => "Art des Spielers: " + p.RoleName,
            < 80 => $"Name: {p.Name} ({p.Age} Jahre)",
            < 95 => "Spiele gesamt (Liga und Pokal)",
            < 108 => "Durchschnittsstärke",
            < 121 => "Tore gesamt (Liga und Pokal)",
            < 158 => "Status: " + TeamUi.StatusText(Club.StatusOf(p)),
            < 172 => "Tendenz des Spielers: " + TeamUi.TrendText(p.Trend),
            < 210 => "Dauer des Vertrages mit dem Spieler (in Saisons)",
            _ => "Monatliches Gehalt des Spielers",
        };
    }
}

/// <summary>Anmeldung im Hauptmenü.</summary>
internal static class ContractsMenuEntries
{
    [MenuEntry("Verträge")]
    private static Screen Open(ScreenHost host, Screen back) => new ContractsScreen(host, back);
}
