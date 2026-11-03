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

    private readonly Screen? _back;
    private readonly MessageBox _message;
    private readonly Queue<string[]> _messages = new();
    private ContractDialog? _dialog;
    private int _row;
    private int _mouseX = -1, _mouseY = -1;
    private string? _status;

    // Saisonende (0x26F20): offene Verhandlungen je Manager, danach der nächste Bildschirm.
    private readonly Queue<(int Manager, Player Player)>? _renewals;
    private readonly Func<Screen>? _next;
    private (int Manager, Player Player)? _renewal;

    public ContractsScreen(ScreenHost host, Screen back) : base(host)
    {
        _back = back;
        _message = new(() => host.Game?.ActiveManager.Name ?? "");
    }

    private ContractsScreen(ScreenHost host, Queue<(int, Player)> renewals, Func<Screen> next) : base(host)
    {
        _renewals = renewals;
        _next = next;
        _message = new(() => host.Game?.ActiveManager.Name ?? "");
    }

    /// <summary>
    /// Verhandlungen am Saisonende (em.exe 0x26F20, Datei 0x27CE5–0x27F49): je Manager und Spieler mit ausgelaufenem Vertrag
    /// das Verhandlungsfenster (Lage nach der Zeile wie im Bildschirm „Verträge“, 0x27D74), danach „… bleibt Ihnen auch die
    /// nächste Saison erhalten“ bzw. „… kehrt Ihrem Verein den Rücken...“ mit der Ablöse. [APPROX] Die Liste dahinter ist die
    /// des Bildschirms „Verträge“ (Original: Kaderliste 0x108:0x43, nicht vermessen). Ohne offene Verhandlung: <paramref name="next"/>.
    /// </summary>
    public static Screen ForSeasonEnd(ScreenHost host, Func<Screen> next)
    {
        var game = host.Game!;
        var queue = new Queue<(int, Player)>();
        for (var i = 0; i < game.Managers.Count; i++)
            if (!game.Managers[i].Dismissed)
                foreach (var p in game.PendingRenewals(game.Managers[i]))
                    queue.Enqueue((i, p));
        return queue.Count == 0 ? next() : new ContractsScreen(host, queue, next);
    }

    /// <summary>Am Saisonende: Verhandlung mit diesem Spieler läuft bzw. steht als Nächstes an.</summary>
    public bool SeasonEnd => _renewals is not null;

    private Club Club => TeamUi.OwnClub(Host);

    /// <summary>Offene Meldung (z. B. „… ist im Moment nicht verhandlungsbereit.“).</summary>
    public MessageBox Message => _message;

    /// <summary>Text der Statuszeile (Hinweiszeile 0x41683) nach dem letzten Bild.</summary>
    public string? Status => _status;

    /// <summary>Spieler, mit dem gerade verhandelt wird (oder null).</summary>
    public Player? Negotiating => _dialog?.Player;

    /// <summary>Offenes Verhandlungsfenster (oder null).</summary>
    public ContractDialog? Dialog => _dialog;

    public static int RowAt(int y) => y < FirstRowY - 1 ? -1 : (y - FirstRowY + 1) / RowPitch;

    public override void Enter()
    {
        if (_renewals is not null)
            NextRenewal();
    }

    public override void Update(InputState input)
    {
        _mouseX = input.MouseX;
        _mouseY = input.MouseY;
        // Im Video bleibt der letzte Text stehen, solange die Maus im Dialog nichts Erklärbares trifft
        // („Status: RESERVE“ über „keine“).
        var status = HoverText(TeamUi.Rows(Club));
        if (status is not null || _dialog is null)
            _status = status;
        if (_message.Update(input))
        {
            if (!_message.Visible && _messages.TryDequeue(out var lines))
                _message.Show(lines);
            if (!_message.Visible && _renewals is not null)
                NextRenewal();
            return;
        }
        if (_dialog is not null)
        {
            UpdateDialog(input);
            return;
        }
        if (_renewals is not null)
        {
            NextRenewal();
            return;
        }

        if (input.Pressed(Key.Escape) || input.RightClick)
        {
            Host.Show(_back!);
            return;
        }
        if (!input.LeftClick)
            return;
        if (!Window.Contains(input.MouseX, input.MouseY))
        {
            Host.Show(_back!);
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
        Open(p, row);
    }

    private void Open(Player p, int row)
    {
        var game = Host.Game!;
        _row = row;
        _dialog = new ContractDialog(p, DialogRect(row), Contracts.CurrentYears(p), p.MonthlySalary, p.InsurancePremium,
            () => Contracts.InsuranceOffer(game, game.ActiveManager, p), years => Contracts.DemandAfterYears(game, game.ActiveManager, p, years),
            () => Contracts.Availability(game, p));
    }

    /// <summary>Nächste Verhandlung am Saisonende öffnen, sonst zum nächsten Bildschirm.</summary>
    private void NextRenewal()
    {
        var game = Host.Game!;
        while (_dialog is null && !_message.Visible && _renewals!.TryDequeue(out var next))
        {
            if (!GameState.IsRenewalPending(next.Player))
                continue;
            game.CurrentManager = next.Manager;
            var row = TeamUi.Rows(Club).ToList().IndexOf(next.Player);
            if (row < 0)
                continue;
            _renewal = next;
            Open(next.Player, row);
        }
        if (_dialog is null && !_message.Visible && _renewals!.Count == 0)
            Host.Show(_next!());
    }

    private void UpdateDialog(InputState input)
    {
        var d = _dialog!;
        switch (d.Update(input))
        {
            case ContractDialog.Outcome.Offer:
                Offer(d);
                break;
            case ContractDialog.Outcome.Cancel:
                _dialog = null;
                if (_renewal is { } r)
                    Show(Host.Game!.CancelRenewal(Host.Game!.Managers[r.Manager], r.Player)?.Messages ?? []);
                _renewal = null;
                break;
            default:
                if (d.Refusal is { } refusal)
                    _message.Show(Contracts.Message(refusal, d.Player));
                break;
        }
    }

    /// <summary>„Mach' ich!“: Verhandlung nach 0x4AB66; der Dialog schließt in jedem Fall, Absagen mit Meldung.</summary>
    private void Offer(ContractDialog d)
    {
        var game = Host.Game!;
        var p = d.Player;
        _dialog = null;
        if (_renewal is { } r)
        {
            _renewal = null;
            if (game.NegotiateRenewal(game.Managers[r.Manager], p, d.Years, d.Salary, d.Insured, d.DemandFilled) is not
                var (result, outcome))
                return;
            var lines = new List<string[]>();
            if (result != NegotiationResult.Accepted)
                lines.Add(Contracts.Message(result, p));
            lines.AddRange(outcome.Messages);
            Show(lines);
            return;
        }
        var answer = Contracts.Negotiate(game, game.ActiveManager, p, d.Years, d.Salary, d.Insured, d.DemandFilled);
        if (answer != NegotiationResult.Accepted)
            _message.Show(Contracts.Message(answer, p));
    }

    private void Show(IReadOnlyList<string[]> messages)
    {
        foreach (var lines in messages)
            _messages.Enqueue(lines);
        if (!_message.Visible && _messages.TryDequeue(out var first))
            _message.Show(first);
        else if (_renewals is not null)
            NextRenewal();
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
        var hoverRow = _dialog is null && Window.Contains(_mouseX, _mouseY) ? RowAt(_mouseY) : -1;
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
            // [ORIGINAL] Spalte E = Belastung · 10 / 15, Farbe 0x10 über 130 wie in der Nationalaufstellung (Datei 0x42335–0x4236D;
            // A184, Integration Runde 12, R12I-01).
            s.TextRight(mini, 169, y, TrainingRules.ExhaustionDisplay(p).ToString(),
                TrainingRules.ExhaustionWarning(p) ? Competitions.NationalLineupScreen.ExhaustionColor : c);
            s.Text(mini, 177, y, p.ContractText, c);
            s.TextRight(mini, 259, y, $"{p.MonthlySalary} DM", c);
        }

        _dialog?.Draw(s, Assets);
        TeamUi.StatusLine(s, Assets, 131, StatusY, _status);
        _message.Draw(s, Assets);
    }

    /// <summary>„1.500 DM“ (im Dialog mit Tausenderpunkt, in der Liste ohne).</summary>
    public static string FormatMoney(int amount) =>
        amount.ToString("#,0", CultureInfo.GetCultureInfo("de-DE")) + " DM";

    private string? HoverText(IReadOnlyList<Player> rows)
    {
        if (_dialog is not null)
        {
            var d = _dialog.Rect;
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
