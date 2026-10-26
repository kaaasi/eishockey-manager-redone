using System.Globalization;
using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.UI.Screens.Team;

/// <summary>
/// Transfer (Hauptmenü → Mannschaft → Transfer). Vermessen aus dem UI-Video (ca. 10:57–11:20):
/// links der eigene Kader (ART NAME SP STÄRKEN TO E), rechts die Transferliste mit Kaufpreis (K) und Leihgebühr (L),
/// unten Kontostand, Schulden, Anlagen, Saldo und der Knopf „Talent“.
/// </summary>
/// <remarks>
/// Bedienung: Name in der Transferliste = Spielerinfo; Preis (K) = kaufen, (L) = leihen; eigener Spieler = verkaufen
/// bzw. vom Markt nehmen. [APPROX] Kauf-, Verkaufs- und Gebotsdialoge sind im Video nicht zu sehen; sie folgen
/// der Vertragsverhandlung und nutzen die Texte aus em.exe.
/// </remarks>
public sealed class TransferScreen : Screen
{
    public static readonly Rect SquadWindow = new(1, 14, 168, 209);
    public static readonly Rect ListWindow = new(175, 14, 144, 209);
    public const int HeaderY = 20;
    public const int FirstRowY = 29;
    public const int RowPitch = 6;
    public const int StatusY = 213;

    public const int ListHeaderY = 30;
    public const int FirstEntryY = 40;
    public const int EntryPitch = 16;
    public const int ListLeft = 181;
    public const int ListRight = 313;

    public static readonly Rect TalentButton = new(261, 226, Widgets.ButtonWidth, Widgets.ButtonHeight);

    /// <summary>Zeile „Name“ eines Eintrags der Transferliste.</summary>
    public static Rect EntryNameRow(int i) => new(ListLeft - 2, FirstEntryY - 1 + EntryPitch * i, ListRight - ListLeft + 4, RowPitch);

    /// <summary>Kaufpreis (K) eines Eintrags.</summary>
    public static Rect EntryBuy(int i) => new(ListLeft - 2, FirstEntryY + 5 + EntryPitch * i, 70, RowPitch + 1);

    /// <summary>Leihgebühr (L) eines Eintrags.</summary>
    public static Rect EntryLoan(int i) => new(ListLeft + 68, FirstEntryY + 5 + EntryPitch * i, ListRight - ListLeft - 66, RowPitch + 1);

    public static int SquadRowAt(int y) => y < FirstRowY - 1 ? -1 : (y - FirstRowY + 1) / RowPitch;

    private readonly Screen _back;
    private readonly MessageBox _message = new();
    private readonly PlayerInfoPopup _info = new();
    private TransferDialog? _dialog;
    private int _mouseX = -1, _mouseY = -1;

    public TransferScreen(ScreenHost host, Screen back) : base(host) => _back = back;

    private GameState Game => Host.Game!;
    private Manager Manager => Game.ActiveManager;
    private Club Club => TeamUi.OwnClub(Host);
    private TransferMarket Market => Game.TransferMarket;

    /// <summary>Offener Dialog (Kauf, Leihe, Verkauf, Gebot, Talentsuche) oder null.</summary>
    public TransferDialog? Dialog => _dialog;

    public PlayerInfoPopup Info => _info;

    public override void Enter() => ShowNextBid();

    private void ShowNextBid()
    {
        if (Market.Bids.FirstOrDefault(b => b.Listing.Seller == Manager) is { } bid)
            _dialog = TransferDialog.ForBid(bid);
    }

    public override void Update(InputState input)
    {
        _mouseX = input.MouseX;
        _mouseY = input.MouseY;
        if (_message.Update(input))
            return;
        if (_info.Update(input))
            return;
        if (_dialog is not null)
        {
            switch (_dialog.Update(input, Assets))
            {
                case TransferDialog.Outcome.Confirm:
                    Confirm(_dialog);
                    break;
                case TransferDialog.Outcome.Cancel:
                    Cancel(_dialog);
                    break;
            }
            return;
        }

        if (input.Pressed(Key.Escape) || input.RightClick)
        {
            Host.Show(_back);
            return;
        }
        if (!input.LeftClick)
            return;

        var x = input.MouseX;
        var y = input.MouseY;
        if (TalentButton.Contains(x, y))
        {
            if (Game.Finance(Manager).TalentSearch is not null)
                _message.Show(TalentScout.Message(TalentSearchStart.AlreadyRunning)); // eine Suche für beide Einstiege
            else
                _dialog = TransferDialog.ForTalent(Game.TalentSearchPrice(Manager));
            return;
        }

        var listings = Market.Listings;
        for (var i = 0; i < listings.Count; i++)
        {
            var l = listings[i];
            if (EntryNameRow(i).Contains(x, y))
            {
                _info.Show(l.Player, l.Price);
                return;
            }
            if (l.Seller == Manager)
                continue;
            if (EntryBuy(i).Contains(x, y))
            {
                _dialog = TransferDialog.ForBuy(l, loan: false);
                return;
            }
            if (EntryLoan(i).Contains(x, y))
            {
                _dialog = TransferDialog.ForBuy(l, loan: true);
                return;
            }
        }

        if (SquadWindow.Contains(x, y))
        {
            var rows = TeamUi.Rows(Club);
            var row = SquadRowAt(y);
            if (row >= 0 && row < rows.Count)
            {
                var p = rows[row];
                if (p.OnLoan)
                    _message.Show("Dieser Spieler ist", "nur ausgeliehen.");
                else
                    _dialog = TransferDialog.ForSale(p, Market.ListingOf(p), TransferMarket.Price(p));
            }
            return;
        }
        if (!ListWindow.Contains(x, y))
            Host.Show(_back);
    }

    private void Confirm(TransferDialog d)
    {
        _dialog = null;
        switch (d.Kind)
        {
            case TransferDialog.DialogKind.Buy or TransferDialog.DialogKind.Loan:
                _message.Show(TransferMarket.Message(Market.Buy(Game, Manager, d.Listing!, d.Amount, d.Kind == TransferDialog.DialogKind.Loan)));
                break;
            case TransferDialog.DialogKind.Sale when d.Listing is null:
                var result = Market.Offer(Game, Manager, d.Player!);
                if (result != TransferResult.Accepted)
                    _message.Show(TransferMarket.Message(result));
                break;
            case TransferDialog.DialogKind.Sale:
                break; // „Verkaufen“ bei einem Spieler auf dem Markt: bleibt auf dem Markt
            case TransferDialog.DialogKind.Bid:
                Market.Accept(Game, d.Bid!);
                ShowNextBid();
                break;
            case TransferDialog.DialogKind.Talent:
                _message.Show(TalentScout.Message(TalentScout.Search(Game, Manager)));
                break;
        }
    }

    private void Cancel(TransferDialog d)
    {
        _dialog = null;
        if (d.Kind == TransferDialog.DialogKind.Sale && d.Listing is not null)
            Market.Withdraw(d.Listing); // „Behalten“
        else if (d.Kind == TransferDialog.DialogKind.Bid)
        {
            Market.Decline(d.Bid!);
            ShowNextBid();
        }
    }

    public override void Draw(Surface s)
    {
        var mini = Assets.Mini;
        var club = Club;
        s.Palette = Assets.Palette;
        s.Blit(Assets.Image(MainMenuScreen.Background), 0, 0);

        // Eigener Kader
        Widgets.Frame(s, Assets, SquadWindow, FrameStyle.Orange);
        const byte h = TeamUi.HeaderColor;
        s.Text(mini, 7, HeaderY, "ART", h);
        s.Text(mini, 27, HeaderY, "NAME", h);
        s.Text(mini, 83, HeaderY, "SP", h);
        s.Text(mini, 97, HeaderY, "STÄRKEN", h);
        s.Text(mini, 134, HeaderY, "TO", h);
        s.Text(mini, 147, HeaderY, "E", h);
        s.HLine(6, 25, 147, TeamUi.LineColor);
        s.HLine(6, 209, 147, TeamUi.LineColor);

        var rows = TeamUi.Rows(club);
        var hover = _dialog is null && !_info.Visible && SquadWindow.Contains(_mouseX, _mouseY) ? SquadRowAt(_mouseY) : -1;
        for (var i = 0; i < rows.Count; i++)
        {
            var p = rows[i];
            var y = FirstRowY + i * RowPitch;
            var c = TeamUi.RoleColor(p.Role);
            if (i == hover || (_dialog?.Player == p))
                TeamUi.HoverRow(s, 6, y, 158, RowPitch);
            s.Text(mini, 7, y, p.RoleShort, c);
            s.Text(mini, 25, y, Market.ListingOf(p) is null ? p.Name : p.Name + " *", c); // [APPROX] * = auf dem Markt
            s.TextRight(mini, 90, y, p.Games.ToString(), c);
            s.TextRight(mini, 104, y, p.Condition.ToString(), c);
            s.TextRight(mini, 116, y, p.Technique.ToString(), c);
            s.TextRight(mini, 128, y, p.Form.ToString(), c);
            s.TextRight(mini, 139, y, p.Role == PlayerRole.Goalkeeper ? "-" : p.Goals.ToString(), c);
            s.TextRight(mini, 152, y, p.Energy.ToString(), c);
        }

        // Transferliste
        Widgets.Frame(s, Assets, ListWindow, FrameStyle.Orange);
        TeamUi.DialogTitle(s, Assets, 247, 17, "Transferliste");
        const byte lh = TeamUi.DimColor; // Video: Kopfzeile der Transferliste gedämpft
        s.Text(mini, ListLeft, ListHeaderY, "ART", lh);
        s.Text(mini, 201, ListHeaderY, "NAME", lh);
        s.Text(mini, 251, ListHeaderY, "STÄRKEN", lh);
        s.Text(mini, 292, ListHeaderY, "ALTER", lh);
        EntryLine(s, 36);

        var listings = Market.Listings;
        var hoverEntry = -1;
        for (var i = 0; i < listings.Count; i++)
            if (_dialog is null && !_info.Visible && new Rect(ListLeft - 2, FirstEntryY - 1 + EntryPitch * i, ListRight - ListLeft + 4, 13).Contains(_mouseX, _mouseY))
                hoverEntry = i;
        for (var i = 0; i < listings.Count; i++)
        {
            var l = listings[i];
            var p = l.Player;
            var y = FirstEntryY + EntryPitch * i;
            var c = TeamUi.RoleColor(p.Role);
            if (i == hoverEntry || _dialog?.Listing == l)
                TeamUi.HoverRow(s, ListLeft - 1, y, ListRight - ListLeft + 3, 13);
            s.Text(mini, ListLeft, y, p.RoleLetter.ToString(), c);
            s.Text(mini, 189, y, p.Name, c);
            s.TextRight(mini, 259, y, p.Condition.ToString(), c);
            s.TextRight(mini, 271, y, p.Technique.ToString(), c);
            s.TextRight(mini, 283, y, p.Form.ToString(), c);
            s.TextRight(mini, 310, y, p.Age + " J.", c);
            s.Text(mini, ListLeft, y + 6, Money(l.Price) + " (K)", TeamUi.DimColor);
            s.TextRight(mini, 312, y + 6, Money(l.LoanFee) + " (L)", TeamUi.DimColor);
            EntryLine(s, y + 12);
        }

        if (StatusText(rows, listings) is { } status)
            TeamUi.LabelCentered(s, Assets, 86, StatusY, status, 196, 5); // Video: hell mit Schatten
        DrawAccountBar(s);

        _dialog?.Draw(s, Assets);
        _info.Draw(s, Assets);
        _message.Draw(s, Assets, 160, 118);
    }

    private string? StatusText(IReadOnlyList<Player> rows, IReadOnlyList<TransferListing> listings)
    {
        if (_dialog is not null || _info.Visible)
            return null;
        for (var i = 0; i < listings.Count; i++)
            if (new Rect(ListLeft - 2, FirstEntryY - 1 + EntryPitch * i, ListRight - ListLeft + 4, 13).Contains(_mouseX, _mouseY))
                return listings[i].OriginText;
        if (SquadWindow.Contains(_mouseX, _mouseY))
        {
            var row = SquadRowAt(_mouseY);
            if (row >= 0 && row < rows.Count)
                return $"Name: {rows[row].Name} ({rows[row].Age} Jahre)";
        }
        return null;
    }

    /// <summary>
    /// Leiste unten: Kontostand, Schulden, Anlagen, Saldo. [APPROX] Schulden und Anlagen liefert das Paket „manager“;
    /// bis dahin 0 DM.
    /// </summary>
    private void DrawAccountBar(Surface s)
    {
        var money = Manager.Money;
        // Streifen über die ganze Breite (Video: Oberkante 3, Füllung 4, unten 6/200/6)
        s.Fill(new Rect(0, 224, Surface.Width, 16), TeamUi.DialogFill);
        s.HLine(0, 224, Surface.Width, 3);
        s.HLine(0, 237, Surface.Width, TeamUi.DialogShadow);
        s.HLine(0, 238, Surface.Width, TeamUi.DialogDark);
        s.HLine(0, 239, Surface.Width, TeamUi.DialogShadow);
        (string Label, long Value, int X)[] fields =
        [
            ("Kontostand:", money, 3),
            ("Schulden:", 0, 68),
            ("Anlagen:", 0, 132),
            ("Saldo:", money, 198),
        ];
        foreach (var (label, value, x) in fields)
        {
            var saldo = x == 198; // Video: Saldo dunkel und ohne Einrückung
            if (saldo)
            {
                s.Text(Assets.Mini, x, 226, label, TeamUi.DialogDark);
                s.Text(Assets.Mini, x, 232, Money(value), TeamUi.DialogDark);
            }
            else
            {
                TeamUi.Label(s, Assets, x, 226, label, Colors.White, TeamUi.DialogShadow);
                s.Text(Assets.Mini, x + 5, 232, Money(value), Colors.White);
            }
        }
        TeamUi.DialogButton(s, Assets, TalentButton.X, TalentButton.Y, "Talent");
    }

    /// <summary>Beige Trennlinie mit dunkler Linie darunter (Video: Index 93 / 198).</summary>
    private static void EntryLine(Surface s, int y)
    {
        s.HLine(ListLeft - 1, y, ListRight - ListLeft + 3, TeamUi.EntryLineColor);
        s.HLine(ListLeft - 1, y + 1, ListRight - ListLeft + 3, 198);
    }

    /// <summary>„231.000 DM“.</summary>
    public static string Money(long amount) => amount.ToString("#,0", CultureInfo.GetCultureInfo("de-DE")) + " DM";
}

/// <summary>Anmeldung im Hauptmenü.</summary>
internal static class TransferMenuEntries
{
    [MenuEntry("Transfer")]
    private static Screen Open(ScreenHost host, Screen back) => new TransferScreen(host, back);
}
