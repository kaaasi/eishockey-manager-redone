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
    private readonly MessageBox _message;
    private readonly PlayerInfoPopup _info = new();
    private TransferDialog? _dialog;
    private readonly Queue<string[]> _messages = new();
    // Kauf nach der Annahme: Vertragsverhandlung 0x4A1DB (Aufruf 0x4786A), dann Abschluss oder „findet nicht statt“.
    private TransferPurchase? _purchase;
    private ContractDialog? _contract;
    private int _mouseX = -1, _mouseY = -1;

    public TransferScreen(ScreenHost host, Screen back) : base(host)
    {
        _back = back;
        _message = new(() => host.Game?.ActiveManager.Name ?? "");
    }

    private GameState Game => Host.Game!;
    private Manager Manager => Game.ActiveManager;
    private Club Club => TeamUi.OwnClub(Host);
    private TransferMarket Market => Game.TransferMarket;

    /// <summary>Offener Dialog (Kauf, Leihe, Verkauf, Gebot, Talentsuche) oder null.</summary>
    public TransferDialog? Dialog => _dialog;

    public PlayerInfoPopup Info => _info;

    /// <summary>Verhandlungsfenster nach einem angenommenen Kauf (oder null).</summary>
    public ContractDialog? Contract => _contract;

    /// <summary>Text der Statuszeile unter der Maus (Herkunft eines Markt-Spielers bzw. Hinweiszeile 0x41683 des Kaders).</summary>
    public string? Status => StatusText(TeamUi.Rows(Club), Market.Listings);

    /// <summary>Offene Meldung (z. B. „Der Transfer findet nicht statt.“).</summary>
    public MessageBox Message => _message;

    public override void Enter() => ShowNextBid();

    private void ShowNextBid()
    {
        if (Market.Bids.FirstOrDefault(b => b.Listing.Seller == Manager) is { } bid)
            _dialog = TransferDialog.ForBid(bid);
    }

    /// <summary>
    /// Texte der Transferliste, die das Original über die Kurzmeldung 0x0C0B8 zeigt (ohne Knopf, schließt von selbst):
    /// [ORIGINAL] Datei 0x46A4D „genug Geld“, 0x47106 „Schon 3 Spieler auf“, 0x472EC und 0x47AAE „wurde abgelehnt“, 0x47A80 „findet nicht
    /// statt“, 0x47B11 „bereits abgelehnt“ (die sieben Aufrufe im Bereich 0x465F1) und 0x42CE3 „Schon 11 Mann auf“ (Integration
    /// Runde 11, ui-meldungen, K2). [APPROX] Alle übrigen Meldungen (angenommen, Kader voll, unfair, ausgeliehen, Vertrag) gehen im Original
    /// nicht über 0x0C0B8; sie bleiben Meldungsfenster mit Klick. Der Aufruf 0x46DEF („Dieser Spieler hört am Saisonende auf.“) hat im Remake
    /// keine Entsprechung.
    /// </summary>
    private static readonly TransferResult[] ShortResults =
    [
        TransferResult.NotEnoughMoney, TransferResult.TooManyOwnListings, TransferResult.MarketFull, TransferResult.Rejected,
        TransferResult.AlreadyRejected, TransferResult.Impossible,
    ];

    private static readonly string[][] ShortTexts = ShortResults.Select(TransferMarket.Message).ToArray();

    /// <summary>Meldet die Transferliste dieses Ergebnis als Kurzmeldung (0x0C0B8)?</summary>
    public static bool IsShortMessage(TransferResult result) =>
        ShortTexts.Any(t => t.AsSpan().SequenceEqual(TransferMarket.Message(result)));

    private void Say(params string[] lines)
    {
        if (ShortTexts.Any(t => t.AsSpan().SequenceEqual(lines)))
            _message.ShowShort(lines[0], lines[1]);
        else
            _message.Show(lines);
    }

    public override void Update(InputState input)
    {
        _mouseX = input.MouseX;
        _mouseY = input.MouseY;
        if (_message.Update(input))
        {
            if (!_message.Visible && _messages.TryDequeue(out var lines))
                Say(lines);
            return;
        }
        if (_info.Update(input))
            return;
        if (_contract is not null)
        {
            UpdateContract(input);
            return;
        }
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
            Leave();
            return;
        }
        if (!input.LeftClick)
            return;

        var x = input.MouseX;
        var y = input.MouseY;
        // [ORIGINAL] em.exe Datei 0x468C1: Der Knopf „Talent“ wertet nur aus, solange keine Suche läuft (Zähler 0);
        // sonst fällt der Klick durch (Integration Runde 11, Anschluss aus geld Teil 6, #448).
        if (TalentButton.Contains(x, y) && Game.Finance(Manager).TalentSearch is null)
        {
            _dialog = TransferDialog.ForTalent(Game.TalentSearchPrice(Manager));
            return;
        }

        var listings = Market.Listings;
        for (var i = 0; i < listings.Count; i++)
        {
            var l = listings[i];
            if (EntryNameRow(i).Contains(x, y))
            {
                // Spielerdaten-Fenster 0x041AC mit Sichtbarkeit nach Verein (#253): Marktspieler gehören nicht zum eigenen
                // Verein, vertragslose zu keinem.
                _info.Show(l.Player, l.Price,
                    PlayerDataContext.Create(Game, l.Player, l.IsFreeAgent ? null : l.From, Club, Manager.Level));
                return;
            }
            if (l.Seller == Manager)
                continue;
            if (EntryBuy(i).Contains(x, y) || EntryLoan(i).Contains(x, y))
            {
                // [ORIGINAL] Sperre nach abgelehntem Gebot wird beim Anklicken geprüft (Datei 0x47140, Meldung 0x47B01).
                if (TransferMarket.IsLocked(Game, Manager, l))
                    Say(TransferMarket.Message(TransferResult.AlreadyRejected));
                else
                    _dialog = TransferDialog.ForBuy(l, loan: EntryLoan(i).Contains(x, y));
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
                // [ORIGINAL] Datei 0x46E04: Spieler mit unaufgefordertem Angebot (+0x09 Bit 0x40) öffnen das Angebotsfenster.
                if (Game.OpenUnsolicitedOffer(Manager, p) is { } offer)
                    _dialog = TransferDialog.ForUnsolicited(offer);
                else if (p.OnLoan)
                    Say("Dieser Spieler ist", "nur ausgeliehen.");
                else
                    _dialog = TransferDialog.ForSale(p, Market.ListingOf(p), TransferMarket.Price(p));
            }
            return;
        }
        if (!ListWindow.Contains(x, y))
            Leave();
    }

    /// <summary>
    /// [ORIGINAL] Ende der Transferliste Datei 0x465F1 (Datei 0x47BB5): vor der Rückkehr Neuaufstellung 0x426BC (Stub
    /// 0x108:0x4D) mit der Einstellung „automatisch aufstellen“ (<see cref="Club.ApplyAutoLineup"/>; Integration Runde 11,
    /// Anschluss aus wettbewerbe-spiel Teil 8).
    /// </summary>
    private void Leave()
    {
        Game.Clubs[Manager.ClubIndex].ApplyAutoLineup();
        Host.Show(_back);
    }

    private void Confirm(TransferDialog d)
    {
        _dialog = null;
        switch (d.Kind)
        {
            case TransferDialog.DialogKind.Buy or TransferDialog.DialogKind.Loan:
                var bid = Market.Bid(Game, Manager, d.Listing!, d.Amount, d.Kind == TransferDialog.DialogKind.Loan, out var purchase,
                    out var subsidy, out var question);
                if (question is not null)
                    _dialog = TransferDialog.ForSeller(question); // [ORIGINAL] 0x47655: Frage an den verkaufenden Manager
                else
                    ShowBidResult(bid, purchase, subsidy);
                break;
            case TransferDialog.DialogKind.Seller:
                ShowBidResult(Market.AnswerSeller(Game, d.Question!, accept: true, out var sold, out var soldSubsidy), sold, soldSubsidy);
                break;
            case TransferDialog.DialogKind.Sale when d.Listing is null:
                var result = Market.Offer(Game, Manager, d.Player!);
                if (result != TransferResult.Accepted)
                    Say(TransferMarket.Message(result));
                break;
            case TransferDialog.DialogKind.Sale:
                break; // „Verkaufen“ bei einem Spieler auf dem Markt: bleibt auf dem Markt
            case TransferDialog.DialogKind.Bid:
                var accepted = Market.Accept(Game, d.Bid!);
                if (accepted != TransferResult.Accepted)
                    Say(TransferMarket.Message(accepted)); // „Der Transfer findet nicht statt.“
                ShowNextBid();
                break;
            case TransferDialog.DialogKind.Talent:
                Say(TalentScout.Message(TalentScout.Search(Game, Manager)));
                break;
            case TransferDialog.DialogKind.Offer:
                if (Game.AcceptUnsolicitedOffer(d.Offer!) != TransferResult.Accepted)
                    Say(TransferMarket.Message(TransferResult.Impossible));
                break;
            case TransferDialog.DialogKind.Subsidy:
                AnswerSubsidy(d.Subsidy!, accept: true);
                break;
        }
    }

    /// <summary>Ergebnis eines Gebots: Verhandlung beim Kauf, Abschluss der Leihe oder Meldung.</summary>
    private void ShowBidResult(TransferResult bid, TransferPurchase? purchase, SponsorSubsidyOffer? subsidy)
    {
        if (purchase is not null)
            OpenContract(purchase);
        else if (bid == TransferResult.Accepted)
            ShowAccepted(subsidy); // Leihe
        else
            Say(TransferMarket.Message(bid));
    }

    /// <summary>
    /// Abschluss eines Kaufs bzw. einer Leihe: zuerst die Frage nach dem Sponsorzuschuss, falls gewürfelt (Datei 0x47A34),
    /// dann die Meldung. [APPROX] Die Meldung „Ihr Angebot wurde angenommen !“ zeigt das Remake zusätzlich (siehe
    /// <see cref="TransferMarket.Message"/>). Der Zuschuss lebt nur im Fenster (kein Spielstandsfeld).
    /// </summary>
    private void ShowAccepted(SponsorSubsidyOffer? subsidy)
    {
        if (subsidy is not null)
            _dialog = TransferDialog.ForSubsidy(subsidy);
        else
            Say(TransferMarket.Message(TransferResult.Accepted));
    }

    private void AnswerSubsidy(SponsorSubsidyOffer subsidy, bool accept)
    {
        if (accept)
            Game.AcceptSponsorSubsidy(subsidy);
        Say(TransferMarket.Message(TransferResult.Accepted));
    }

    /// <summary>
    /// Verhandlungsfenster nach der Annahme. [ORIGINAL] Datei 0x4785D: Aufruf mit y = 0 (oben); der neue Kaderplatz ist frisch,
    /// Laufzeit und Gehalt sind die des Wechsels (<see cref="TransferPurchase"/>).
    /// </summary>
    private void OpenContract(TransferPurchase purchase)
    {
        var game = Game;
        _purchase = purchase;
        _contract = new ContractDialog(purchase.Player, ContractDialog.Top, purchase.Years, purchase.Salary, insurance: 0,
            () => Contracts.InsuranceOffer(game, purchase.Buyer, purchase.Player), years => Contracts.DemandAfterYears(game, purchase.Buyer, purchase.Player, years, purchase.Salary), () => null);
    }

    private void UpdateContract(InputState input)
    {
        var d = _contract!;
        var outcome = d.Update(input);
        if (outcome == ContractDialog.Outcome.None)
            return;
        var purchase = _purchase!;
        _contract = null;
        _purchase = null;
        if (outcome == ContractDialog.Outcome.Cancel)
        {
            Say(TransferMarket.NotTakingPlace); // 0x47874 → 0x47A3E
            return;
        }
        var result = Market.Sign(Game, purchase, d.Years, d.Salary, d.Insured, d.DemandFilled, out var subsidy);
        if (result == NegotiationResult.Accepted)
        {
            ShowAccepted(subsidy);
            return;
        }
        Say(Contracts.Message(result, purchase.Player));
        _messages.Enqueue(TransferMarket.NotTakingPlace);
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
        else if (d.Kind == TransferDialog.DialogKind.Subsidy)
            AnswerSubsidy(d.Subsidy!, accept: false);
        else if (d.Kind == TransferDialog.DialogKind.Seller) // „Nein !“: abgelehnt und gesperrt (0x47AC8)
            Say(TransferMarket.Message(Market.AnswerSeller(Game, d.Question!, accept: false, out _, out _)));
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
            // [ORIGINAL] Spalte E = Belastung · 10 / 15, Farbe 0x10 über 130 wie in der Nationalaufstellung (Datei 0x42335–0x4236D;
            // A184, Integration Runde 12, R12I-01).
            s.TextRight(mini, 152, y, TrainingRules.ExhaustionDisplay(p).ToString(),
                TrainingRules.ExhaustionWarning(p) ? Competitions.NationalLineupScreen.ExhaustionColor : c);
        }

        // Transferliste
        Widgets.Frame(s, Assets, ListWindow, FrameStyle.Orange);
        TeamUi.DialogTitle(s, Assets, 246, 19, "Transferliste"); // Pixelvergleich oberflaeche_1102: (−1 | +2)
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
            s.Text(mini, ListLeft, y, p.RoleLetter.ToString(), c);
            s.Text(mini, 189, y, p.Name, c);
            // Pixelvergleich oberflaeche_1102 (Spur transfer-original-vertrag): rechtsbündige Spalten 3 Pixel weiter rechts
            s.TextRight(mini, 262, y, p.Condition.ToString(), c);
            s.TextRight(mini, 274, y, p.Technique.ToString(), c);
            s.TextRight(mini, 286, y, p.Form.ToString(), c);
            s.TextRight(mini, 313, y, p.Age + " J.", c);
            s.Text(mini, ListLeft, y + 6, Money(l.Price) + " (K)", TeamUi.DimColor);
            s.TextRight(mini, 315, y + 6, Money(l.LoanFee) + " (L)", TeamUi.DimColor);
            EntryLine(s, y + 12);
        }
        for (var i = 0; i < listings.Count; i++)
            if (i == hoverEntry || _dialog?.Listing == listings[i])
                HoverEntry(s, FirstEntryY + EntryPitch * i);

        if (StatusText(rows, listings) is { } status)
            TeamUi.LabelCentered(s, Assets, 86, StatusY, status, 196, 5); // Video: hell mit Schatten
        DrawAccountBar(s);

        _dialog?.Draw(s, Assets);
        _contract?.Draw(s, Assets);
        _info.Draw(s, Assets);
        _message.Draw(s, Assets);
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
            // [ORIGINAL] Hinweiszeile 0x41683, Aufruf 0x46A83: Ursprung 7 | 29, Maske 0x90C6, Rand 155.
            var layout = PlayerListLayout.TransferList(rows.Count);
            var row = PlayerHintLine.Row(layout, _mouseY);
            var column = PlayerHintLine.Column(layout, _mouseX);
            if (row >= 0 && row < rows.Count && column >= 0)
                return PlayerHintLine.Text(rows[row], column, new PlayerHintInfo(Club.IsDressed(rows[row]), LineOf(rows[row])),
                    showExhaustion: false);
        }
        return null;
    }

    /// <summary>Reihe (1–4), in der der Spieler aufgestellt ist, sonst 0 (Kaderplatz +0x15 &amp; 0x7F).</summary>
    private int LineOf(Player p)
    {
        foreach (var slot in LineupSlot.All)
            if (Club.Lineup[slot] == p)
                return slot.Block + 1;
        return 0;
    }

    /// <summary>
    /// Leiste unten: Kontostand, Schulden, Anlagen, Saldo. [ORIGINAL] Werte nach Datei 0x463CC
    /// (<see cref="GameState.TransferAccount"/>; Spur <c>kader-transfer</c>, Teil 4, A381). [APPROX] Lage aus dem Video,
    /// x der Anlagen 132 statt 133 (3 + 65 · k) und Zeilen 226/232 statt der Grundlinien 229/236 nicht nachgemessen.
    /// </summary>
    private void DrawAccountBar(Surface s)
    {
        var account = Game.TransferAccount(Manager);
        // Streifen über die ganze Breite (Video: Oberkante 3, Füllung 4, unten 6/200/6)
        s.Fill(new Rect(0, 224, Surface.Width, 16), TeamUi.DialogFill);
        s.HLine(0, 224, Surface.Width, 3);
        s.HLine(0, 237, Surface.Width, TeamUi.DialogShadow);
        s.HLine(0, 238, Surface.Width, TeamUi.DialogDark);
        s.HLine(0, 239, Surface.Width, TeamUi.DialogShadow);
        (string Label, long Value, int X)[] fields =
        [
            ("Kontostand:", account.Money, 3),
            ("Schulden:", account.Debts, 68),
            ("Anlagen:", account.Assets, 132),
            ("Saldo:", account.Balance, 198),
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
        // [ORIGINAL] em.exe Datei 0x46704: Knopf „Talent“ nur bei Zähler 0 (keine laufende Suche).
        if (Game.Finance(Manager).TalentSearch is null)
            TeamUi.DialogButton(s, Assets, TalentButton.X, TalentButton.Y, "Talent");
    }

    /// <summary>Beige Trennlinie mit dunkler Linie darunter (Video: Index 93 / 198).</summary>
    /// <summary>
    /// Markierung des Eintrags unter der Maus: Die beiden Trennlinien über und unter dem Eintrag (obere Zeile der Linie) werden
    /// dunkelrot, die untere Zeile (198) bleibt. [ORIGINAL] Pixelvergleich `oberflaeche_1100` (LANGE) und `oberflaeche_1102`
    /// (LAPORTE): Zeilen y − 4 und y + 12 gemessen (68–75 | 38 | 40–47), Index 201 (65 | 0 | 0) mit Unschärfe; die Zeile
    /// y − 1 bleibt Hintergrund. Vorher zeichnete das Remake hellrot (17) in Zeile y − 1, die untere Linie überschrieb
    /// <see cref="EntryLine"/>. Index 201 statt 16/253 aus der Rotkomponente geschätzt: [APPROX] (Video unscharf).
    /// </summary>
    private static void HoverEntry(Surface s, int y)
    {
        s.HLine(ListLeft - 1, y - 4, ListRight - ListLeft + 3, HoverEntryColor);
        s.HLine(ListLeft - 1, y + 12, ListRight - ListLeft + 3, HoverEntryColor);
    }

    /// <summary>Farbe der Trennlinien um den markierten Eintrag der Transferliste (siehe <see cref="HoverEntry"/>).</summary>
    public const byte HoverEntryColor = 201;

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
