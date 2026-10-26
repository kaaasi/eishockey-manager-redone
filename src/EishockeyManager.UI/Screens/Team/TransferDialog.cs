using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens.Team;

/// <summary>
/// Dialoge des Transfer-Bildschirms im Stil der Vertragsverhandlung (<see cref="TeamUi.DialogFrame"/>).
/// [APPROX] Lage und Aufbau sind nur für die Talentsuche im Video belegt („LetsShow“, ca. 11:15);
/// Kauf, Leihe, Verkauf und Gebote nutzen die Texte aus em.exe.
/// </summary>
public sealed class TransferDialog
{
    public enum DialogKind { Buy, Loan, Sale, Bid, Talent }

    public enum Outcome { None, Confirm, Cancel }

    public static readonly Rect Box = new(76, 80, 168, 72);

    /// <summary>Fragefenster (Video „LetsShow“, ca. 11:15): oranger Rahmen, zwei „?“-Felder, Trennlinie über den Knöpfen.</summary>
    public static readonly Rect QuestionBox = new(75, 107, 168, 76);

    public static Rect AmountField => new(Box.X + 84, Box.Y + 36, 76, 7);

    /// <summary>Fragefenster (Talentsuche, Gebote) statt Verhandlungsdialog?</summary>
    public bool IsQuestion => Kind is DialogKind.Talent or DialogKind.Bid;

    public Rect ConfirmButton => IsQuestion ? Widgets.ButtonRect(95, 167) : Widgets.ButtonRect(Box.X + 12, Box.Y + 55);
    public Rect CancelButton => IsQuestion ? Widgets.ButtonRect(165, 167) : Widgets.ButtonRect(Box.X + 99, Box.Y + 55);

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

    /// <summary>Rückfrage zur Talentsuche mit dem Preis nach Rabatt (<see cref="GameState.TalentSearchPrice"/>).</summary>
    public static TransferDialog ForTalent(long price) => new(DialogKind.Talent) { Price = price };

    private string ConfirmText => Kind switch
    {
        DialogKind.Sale => "Verkaufen",
        DialogKind.Talent => "Aber immer!",
        _ => "Mach' ich!",
    };

    private string CancelText => Kind switch
    {
        DialogKind.Sale when Listing is not null => "Behalten",
        DialogKind.Talent => "Lieber nicht.",
        _ => "Abbruch",
    };

    public Outcome Update(InputState input, Assets assets)
    {
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

    /// <summary>Fragefenster wie „LetsShow“ im Video. [APPROX] Die „?“-Felder sind gezeichnet (keine Grafik gefunden).</summary>
    private void DrawQuestion(Surface s, Assets a)
    {
        var b = QuestionBox;
        var cx = b.X + b.W / 2 + 1;
        Widgets.Frame(s, a, b, FrameStyle.Orange);
        QuestionMark(s, a, b.X + 7, b.Y + 4);
        QuestionMark(s, a, b.Right - 29, b.Y + 4);
        string[] lines = Kind == DialogKind.Talent
            ? ["Möchten Sie, daß Ihr 'Spion'", "für Sie auf Talentsuche geht?", "Preis: " + TransferScreen.Money(Price)]
            : ["Nehmen Sie das Angebot", $"({TransferScreen.Money(Price)}) für", Player!.Name + " an ?"];
        var title = Kind == DialogKind.Talent ? "LetsShow" : "ANGEBOT von " + Bid!.Buyer.Name;
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
