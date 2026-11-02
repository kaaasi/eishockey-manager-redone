using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens.Team;

/// <summary>
/// Dialoge des Transfer-Bildschirms im Stil der Vertragsverhandlung (<see cref="TeamUi.DialogFrame"/>).
/// [APPROX] Lage und Aufbau sind nur für die Talentsuche im Video belegt („LetsShow“, ca. 11:15);
/// Kauf, Leihe, Verkauf und Gebote nutzen die Texte aus em.exe.
/// </summary>
public sealed class TransferDialog
{
    public enum DialogKind { Buy, Loan, Sale, Bid, Talent, Offer, Subsidy }

    public enum Outcome { None, Confirm, Cancel }

    public static readonly Rect Box = new(76, 80, 168, 72);

    /// <summary>Fragefenster (Video „LetsShow“, ca. 11:15): oranger Rahmen, zwei „?“-Felder, Trennlinie über den Knöpfen.</summary>
    public static readonly Rect QuestionBox = new(75, 107, 168, 76);

    public static Rect AmountField => new(Box.X + 84, Box.Y + 36, 76, 7);

    /// <summary>Fragefenster (Talentsuche, Sponsorzuschuss) statt Verhandlungsdialog?</summary>
    public bool IsQuestion => Kind is DialogKind.Talent or DialogKind.Subsidy;

    /// <summary>Angebotsfenster 0x491F9 (unaufgefordertes Angebot, Argument 1, bzw. Angebot für einen Spieler auf dem Markt, Argument 0)?</summary>
    public bool IsOfferWindow => Kind is DialogKind.Offer or DialogKind.Bid;

    public Rect ConfirmButton => IsOfferWindow ? Widgets.ButtonRect(90, OfferBox.Y + 54)
        : IsQuestion ? Widgets.ButtonRect(95, 167) : Widgets.ButtonRect(Box.X + 12, Box.Y + 55);
    public Rect CancelButton => IsOfferWindow ? Widgets.ButtonRect(11, OfferBox.Y + 54)
        : IsQuestion ? Widgets.ButtonRect(165, 167) : Widgets.ButtonRect(Box.X + 99, Box.Y + 55);

    /// <summary>
    /// Angebotsfenster 0x491F9 (unaufgefordertes Angebot). [ORIGINAL] Rahmen über 0x490DE mit y = 0 und Höhe 0x47: x 2 … 0xA8,
    /// y 0x1C … 0x63 (wie die Vertragsverhandlung beim Kauf, <see cref="ContractDialog.Top"/>, nur 4 Pixel höher).
    /// </summary>
    public static readonly Rect OfferBox = new(1, 29, 166, 71);

    private string _edit = "";
    private bool _editing;

    private TransferDialog(DialogKind kind) => Kind = kind;

    public DialogKind Kind { get; }
    public TransferListing? Listing { get; private init; }
    public TransferBid? Bid { get; private init; }
    public Player? Player { get; private init; }

    /// <summary>Gebotener Betrag (Kauf, Leihe).</summary>
    public long Amount { get; private set; }

    /// <summary>Ablöse bzw. Preis zur Anzeige.</summary>
    public long Price { get; private init; }

    public bool Editing => _editing;

    public static TransferDialog ForBuy(TransferListing listing, bool loan) => new(loan ? DialogKind.Loan : DialogKind.Buy)
    {
        Listing = listing,
        Player = listing.Player,
        Price = loan ? listing.LoanFee : listing.Price,
        Amount = loan ? listing.LoanFee : listing.Price,
    };

    public static TransferDialog ForSale(Player player, TransferListing? listing, long price) => new(DialogKind.Sale)
    {
        Player = player,
        Listing = listing,
        Price = listing?.Price ?? price,
    };

    public static TransferDialog ForBid(TransferBid bid) => new(DialogKind.Bid)
    {
        Bid = bid,
        Listing = bid.Listing,
        Player = bid.Listing.Player,
        Price = bid.Amount,
    };

    /// <summary>Unaufgefordertes Angebot (Bit 0x40) für einen Spieler des eigenen Kaders.</summary>
    public static TransferDialog ForUnsolicited(UnsolicitedOffer offer) => new(DialogKind.Offer)
    {
        Offer = offer,
        Player = offer.Player,
        Price = offer.Amount,
    };

    /// <summary>Unaufgefordertes Angebot im Fenster (nur <see cref="DialogKind.Offer"/>).</summary>
    public UnsolicitedOffer? Offer { get; private init; }

    /// <summary>
    /// Frage nach dem Sponsorzuschuss nach einem Kauf vom Computermarkt. [ORIGINAL] Datei 0x063BE: Frage 0x4C246 mit den Zeilen
    /// „Nehmen Sie einen Sponsor-“ (DGROUP 0x12DD), „zuschuß von“ (0x12E1) und Betrag + „ DM an ?“ (0x14D9), Knöpfe
    /// „Aber immer!“ (0x1311) und „Lieber nicht.“ (0x1315). [APPROX] Fenster wie die Talentsuche, Titel = Name des Managers
    /// (0x4C246 erhält den Manager [0xA70E]); kein Videobeleg.
    /// </summary>
    public static TransferDialog ForSubsidy(SponsorSubsidyOffer offer) => new(DialogKind.Subsidy)
    {
        Subsidy = offer,
        Price = offer.Amount,
    };

    /// <summary>Angebotener Sponsorzuschuss (nur <see cref="DialogKind.Subsidy"/>).</summary>
    public SponsorSubsidyOffer? Subsidy { get; private init; }

    /// <summary>Rückfrage zur Talentsuche mit dem Preis nach Rabatt (<see cref="GameState.TalentSearchPrice"/>).</summary>
    public static TransferDialog ForTalent(long price) => new(DialogKind.Talent) { Price = price };

    private string ConfirmText => Kind switch
    {
        DialogKind.Sale or DialogKind.Offer or DialogKind.Bid => "Verkaufen",
        DialogKind.Talent or DialogKind.Subsidy => "Aber immer!",
        _ => "Mach' ich!",
    };

    private string CancelText => Kind switch
    {
        DialogKind.Sale when Listing is not null => "Behalten",
        DialogKind.Offer or DialogKind.Bid => "Behalten",
        DialogKind.Talent or DialogKind.Subsidy => "Lieber nicht.",
        _ => "Abbruch",
    };

    /// <summary>Knopf unter der gedrückten linken Maustaste (Angebotsfenster), sonst null.</summary>
    public Rect? PressedButton { get; private set; }

    public Outcome Update(InputState input, Assets assets)
    {
        PressedButton = IsOfferWindow ? ContractDialog.PressedAt(input, CancelButton, ConfirmButton) : null;
        if (_editing)
        {
            foreach (var ch in input.Text)
                if (char.IsAsciiDigit(ch) && _edit.Length < 8)
                    _edit += ch;
            if (input.Pressed(Key.Backspace) && _edit.Length > 0)
                _edit = _edit[..^1];
            if (input.Pressed(Key.Escape))
            {
                _editing = false;
                return Outcome.None;
            }
            if (!input.Pressed(Key.Enter) && !input.LeftClick)
                return Outcome.None;
            if (long.TryParse(_edit, out var value))
                Amount = value;
            _editing = false;
            if (!input.LeftClick)
                return Outcome.None;
        }

        if (input.Pressed(Key.Escape) || input.RightClick || input.Clicked(CancelButton))
            return Outcome.Cancel;
        if (input.Clicked(ConfirmButton) || input.Pressed(Key.Enter))
            return Outcome.Confirm;
        if ((Kind is DialogKind.Buy or DialogKind.Loan) && input.Clicked(AmountField))
        {
            _editing = true;
            _edit = "";
        }
        return Outcome.None;
    }

    public void Draw(Surface s, Assets a)
    {
        if (IsQuestion)
        {
            DrawQuestion(s, a);
            return;
        }
        if (IsOfferWindow)
        {
            DrawOffer(s, a);
            return;
        }
        var mini = a.Mini;
        var b = Box;
        var cx = b.X + b.W / 2;
        TeamUi.DialogFrame(s, b);
        TeamUi.DialogTitle(s, a, cx + 1, b.Y + 5, Kind switch
        {
            DialogKind.Buy => Player!.Name + " kaufen",
            DialogKind.Loan => Player!.Name + " leihen",
            DialogKind.Bid => "ANGEBOT von " + Bid!.Buyer.Name,
            DialogKind.Talent => "LetsShow",
            _ => "Transfermarkt",
        });
        s.HLine(b.X + 4, b.Y + 14, b.W - 8, Colors.White);

        switch (Kind)
        {
            case DialogKind.Buy or DialogKind.Loan:
                s.TextCentered(mini, cx, b.Y + 18, $"{Player!.Name}, {Player.Age} Jahre alt", Colors.White);
                s.Text(mini, b.X + 6, b.Y + 27, Kind == DialogKind.Buy ? "ABLÖSESUMME :" : "LEIHGEBÜHR :", Colors.White);
                s.Text(mini, b.X + 84, b.Y + 27, TransferScreen.Money(Price), Colors.White);
                s.Text(mini, b.X + 6, b.Y + 37, "Ihr Angebot:", Colors.White);
                if (_editing)
                {
                    var x = s.Text(mini, b.X + 84, b.Y + 37, _edit, Colors.White);
                    s.Fill(new Rect(x + 1, b.Y + 37, 3, 5), 1);
                }
                else
                    s.Text(mini, b.X + 84, b.Y + 37, TransferScreen.Money(Amount), Colors.White);
                break;
            case DialogKind.Sale:
                s.TextCentered(mini, cx, b.Y + 18, $"{Player!.Name}, {Player.Age} Jahre alt", Colors.White);
                s.Text(mini, b.X + 6, b.Y + 27, "Ablöse:", Colors.White);
                s.Text(mini, b.X + 84, b.Y + 27, TransferScreen.Money(Price), Colors.White);
                s.TextCentered(mini, cx, b.Y + 39, Listing is null ? "Auf den Transfermarkt ?" : "Steht auf dem Transfermarkt.", Colors.White);
                break;
        }

        TeamUi.DialogButton(s, a, ConfirmButton.X, ConfirmButton.Y, ConfirmText);
        TeamUi.DialogButton(s, a, CancelButton.X, CancelButton.Y, CancelText);
    }

    /// <summary>
    /// Angebotsfenster nach em.exe 0x491F9 (Argument 4 = 1, Aufruf 0x46EA5). [ORIGINAL] Titel 0x490DE: „Manager“ (DGROUP 0xC74)
    /// + Name, zentriert 6 … 0xA3, Grundlinie y 0x28, Farbe 0x0A mit Schatten 0x1C, Linie y 0x2B; „ANGEBOT von &lt;Verein&gt; :“
    /// bei (7 | 0x34), Betrag mit „ DM“ zentriert 4 … 0xA6 bei y 0x3C; „ABLÖSESUMME :“ bei (7 | 0x44), darunter „keine“ (y 0x4C);
    /// Knöpfe „Behalten“ (0x0B | 0x53) und „Verkaufen“ (0x5A | 0x53), Textfarbe 7. Bei einem Angebot für einen Spieler auf dem
    /// Markt (Argument 0, Aufruf 0x473FF) steht unter „ABLÖSESUMME :“ statt „keine“ der Rest Angebot − Angebot / 7 · Restlaufzeit
    /// (0x49332–0x49371, <see cref="TransferMarket.TransferFee"/>), der beim Verkauf gutgeschrieben wird. Grundlinien wie beim Verhandlungsfenster
    /// auf die Oberkante der Schrift umgerechnet (Mini 4, Titel 6 Pixel darüber). [APPROX] Kein Videobeleg; Lage nur aus dem Code.
    /// </summary>
    private void DrawOffer(Surface s, Assets a)
    {
        var mini = a.Mini;
        var b = OfferBox;
        TeamUi.DialogFrame(s, b);
        TeamUi.DialogTitle(s, a, 84, 0x28 - 6, "Manager " + Player!.Name);
        s.HLine(5, 0x2B, 159, Colors.White);
        var buyer = Kind == DialogKind.Bid ? Bid!.Buyer : Offer!.Buyer;
        s.Text(mini, 7, 0x34 - 4, "ANGEBOT von " + buyer.Name + " :", Colors.White);
        s.TextCentered(mini, (4 + 0xA6) / 2, 0x3C - 4, TransferScreen.Money(Price), Colors.White);
        s.Text(mini, 7, 0x44 - 4, "ABLÖSESUMME :", Colors.White);
        var fee = Kind == DialogKind.Bid ? TransferScreen.Money(TransferMarket.TransferFee(Bid!)) : "keine";
        s.TextCentered(mini, (4 + 0xA6) / 2, 0x4C - 4, fee, Colors.White);
        ContractDialog.Button(s, a, CancelButton, CancelText, PressedButton == CancelButton);
        ContractDialog.Button(s, a, ConfirmButton, ConfirmText, PressedButton == ConfirmButton);
    }

    /// <summary>Fragefenster wie „LetsShow“ im Video. [APPROX] Die „?“-Felder sind gezeichnet (keine Grafik gefunden).</summary>
    private void DrawQuestion(Surface s, Assets a)
    {
        var b = QuestionBox;
        var cx = b.X + b.W / 2 + 1;
        Widgets.Frame(s, a, b, FrameStyle.Orange);
        QuestionMark(s, a, b.X + 7, b.Y + 4);
        QuestionMark(s, a, b.Right - 29, b.Y + 4);
        string[] lines = Kind switch
        {
            DialogKind.Talent => ["Möchten Sie, daß Ihr 'Spion'", "für Sie auf Talentsuche geht?", "Preis: " + TransferScreen.Money(Price)],
            DialogKind.Subsidy => ["Nehmen Sie einen Sponsor-", "zuschuß von", TransferScreen.Money(Price) + " an ?"],
            _ => ["Nehmen Sie das Angebot", $"({TransferScreen.Money(Price)}) für", Player!.Name + " an ?"],
        };
        var title = Kind switch
        {
            DialogKind.Talent => "LetsShow",
            DialogKind.Subsidy => Subsidy!.Manager.Name,
            _ => "ANGEBOT von " + Bid!.Buyer.Name,
        };
        Widgets.ShadowText(s, a.Flow, cx, b.Y + 12, title, Colors.White, Colors.Black);
        s.HLine(b.X + 34, b.Y + 24, b.W - 68, Colors.White);
        for (var i = 0; i < lines.Length; i++)
            TeamUi.LabelCentered(s, a, cx, b.Y + 30 + 8 * i, lines[i], Colors.White, Colors.Black);
        s.HLine(b.X + 5, b.Y + 56, b.W - 10, TeamUi.DialogDark);
        s.HLine(b.X + 5, b.Y + 57, b.W - 10, 37);
        TeamUi.DialogButton(s, a, ConfirmButton.X, ConfirmButton.Y, ConfirmText);
        TeamUi.DialogButton(s, a, CancelButton.X, CancelButton.Y, CancelText);
    }

    /// <summary>Grünes Feld (22 × 22) mit schwarzem „?“.</summary>
    private static void QuestionMark(Surface s, Assets a, int x, int y)
    {
        var r = new Rect(x, y, 22, 22);
        s.Fill(r, 14);
        s.Outline(r, 12);
        s.HLine(x + 1, y + 1, 20, 15);
        s.VLine(x + 1, y + 1, 20, 15);
        s.TextCentered(a.Bold, x + 11, y + 4, "?", Colors.Black);
    }
}
