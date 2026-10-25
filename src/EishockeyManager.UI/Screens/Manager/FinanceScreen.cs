using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens.ManagerArea;

/// <summary>
/// Bildschirm Finanzen – Bank und Börse (Hintergrund kb\152, ganzer Bildschirm). Vermessen aus dem UI-Video (5:40–6:20).
/// <list type="bullet">
/// <item>Oben die Bank und drei Kästen für die Mitmanager (Kopf), darunter Saldo und Zinsen pro Monat.</item>
/// <item>Mitte: Knöpfe [+][−] links und [−][+] rechts, Pfeile ◄ ►; darüber erscheint der Hinweis zur gewählten
///   Bank, zum Manager oder zur Aktie („Geld von der Bank abheben/leihen (4%).“, „Aktien von … kaufen.“).
///   Linke Hälfte = holen/leihen/kaufen, rechte Hälfte = einzahlen/verleihen/verkaufen.</item>
/// <item>Unten die vier Aktien (Kurs, Zahl, Kauf, Wert), der Kursverlauf und rechts die Übersicht.</item>
/// </list>
/// Klick auf das Bankbild oder den Kontostand öffnet das Finanzbuch.
/// </summary>
/// <remarks>
/// [APPROX] Im Video wird nur mit der Maus über die Felder gefahren, aber nichts gebucht. Bedienung daher angenommen:
/// Überfahren wählt Bank/Manager/Aktie, [+]/[−] stellen den Betrag der jeweiligen Seite ein (Rechtsklick = zehnfach),
/// ◄ führt die linke, ► die rechte Buchung aus; der Betrag steht im schwarzen Balken.
/// </remarks>
public sealed class FinanceScreen : Screen
{
    public const int Background = 152;

    // ---- Vermessung (kb\152 und Video) -------------------------------------------------------

    /// <summary>Kasten 0 = Bank, 1–3 = Mitmanager.</summary>
    public static Rect PartnerBox(int i) => i == 0 ? new Rect(3, 2, 101, 53) : new Rect(118 + (i - 1) * 71, 2, 57, 53);

    /// <summary>Bankbild (Klick = Finanzbuch).</summary>
    public static readonly Rect BankPicture = new(21, 4, 66, 49);

    /// <summary>Schwarze Fläche für den Managerkopf (37 × 38).</summary>
    public static Rect HeadArea(int i) => new(128 + (i - 1) * 71, 10, 37, 38);

    private static int CaptionCenter(int i) => 75 + i * 71;
    private const int CaptionY1 = 57;
    private const int CaptionY2 = 65;

    public static readonly Rect LeftPlus = new(4, 76, 16, 9);
    public static readonly Rect LeftMinus = new(23, 76, 16, 9);
    public static readonly Rect RightMinus = new(280, 76, 16, 9);
    public static readonly Rect RightPlus = new(298, 76, 16, 9);
    public static readonly Rect LeftArrow = new(150, 76, 9, 10);
    public static readonly Rect RightArrow = new(160, 76, 9, 10);

    /// <summary>Bereich mit Hinweistext und schwarzem Balken (x 4–315, y 89–96).</summary>
    public static readonly Rect ActionBar = new(0, 73, 320, 25);
    private static readonly Rect AmountBar = new(4, 89, 312, 8);
    private const int LeftCenter = 93;
    private const int RightCenter = 227;
    private const int HintY1 = 75;
    private const int HintY2 = 81;
    private const int AmountY = 90;

    /// <summary>Zeile einer Aktie: Logo (35 × 25) und Infofeld (83 × 30).</summary>
    public static Rect StockRow(int i) => new(4, 103 + i * 69 / 2, 125, 30);
    private static int StockTextY(int i) => 105 + i * 35;
    private const int StockTextX = 47;
    private const int StockLinePitch = 7;

    /// <summary>Schwarzes Feld des Kursverlaufs (110 × 133), eine Spalte je Tag.</summary>
    public static readonly Rect Chart = new(132, 104, Stock.HistoryLength, 133);

    /// <summary>Höhe der Zeile, auf der der Kurs der gewählten Aktie liegt (Video: 172/173).</summary>
    private const double ChartCenterY = 172.5;

    /// <summary>Maßstab des Kursverlaufs: 1,5 Pixel je DM (Video, Abstand der Kurven).</summary>
    public const double PixelsPerDm = 1.5;

    /// <summary>[APPROX] Bildlauf des Kursverlaufs in DM je Frame.</summary>
    private const double ScrollSpeed = 1.0;

    public static readonly Rect AccountField = new(246, 103, 70, 17);
    private const int LabelX = 249;
    private const int ValueRight = 304;

    // ---- Farben (Standardpalette) ----------------------------------------------------------

    /// <summary>Farben der Aktien in Infofeld und Kursverlauf: grün, blau, rot, orange.</summary>
    public static readonly byte[] StockColors = [15, 151, 105, 59];
    private const byte TextColor = 212;
    private const byte LabelTop = 212;
    private const byte LabelBottom = 50;
    private const byte AccountColor = 12;

    // ---- Zustand ---------------------------------------------------------------------------

    private readonly Screen _back;
    private Selection _selection = new(SelectionKind.Bank, 0);
    private long _leftAmount;
    private long _rightAmount;
    private Rect? _pressed;
    private int _mouseX = -1;
    private int _mouseY = -1;
    private double _viewPrice = double.NaN;

    public FinanceScreen(ScreenHost host, Screen back) : base(host) => _back = back;

    public enum SelectionKind { Bank, Manager, Stock }

    /// <summary>Gewähltes Gegenüber: Bank, Mitmanager (Index im Spiel) oder Aktie (0–3).</summary>
    public readonly record struct Selection(SelectionKind Kind, int Index);

    public Selection Selected => _selection;
    public long LeftAmount => _leftAmount;
    public long RightAmount => _rightAmount;

    /// <summary>Mittlerer Kurs des sichtbaren Ausschnitts.</summary>
    public double ViewPrice => _viewPrice;

    private GameState Game => Host.Game!;
    private Core.Manager Me => Game.ActiveManager;

    /// <summary>Mitmanager in der Reihenfolge der Kästen 1–3.</summary>
    private List<int> Partners => Enumerable.Range(0, Game.Managers.Count).Where(i => i != Game.CurrentManager).Take(3).ToList();

    public override void Enter()
    {
        var prices = Game.Stocks.Stocks.Select(s => s.Price).ToList();
        // Video 5:45: zu Beginn liegt die Mitte zwischen höchstem und niedrigstem Kurs.
        _viewPrice = (prices.Max() + prices.Min()) / 2.0;
    }

    public override void Update(InputState input)
    {
        _pressed = null;
        if (input.MouseX >= 0)
        {
            _mouseX = input.MouseX;
            _mouseY = input.MouseY;
        }
        if (input.Pressed(Key.Escape) || input.RightClicked(new Rect(0, 0, 320, 70)))
        {
            Host.Show(_back);
            return;
        }

        UpdateHover();
        ScrollChart();

        var factor = input.RightClick ? 10 : 1;
        bool Hit(Rect r) => (input.LeftClick || input.RightClick) && r.Contains(input.MouseX, input.MouseY);

        if (Hit(LeftPlus)) Change(left: true, +factor, LeftPlus);
        else if (Hit(LeftMinus)) Change(left: true, -factor, LeftMinus);
        else if (Hit(RightPlus)) Change(left: false, +factor, RightPlus);
        else if (Hit(RightMinus)) Change(left: false, -factor, RightMinus);
        else if (input.Clicked(LeftArrow)) { _pressed = LeftArrow; ExecuteLeft(); }
        else if (input.Clicked(RightArrow)) { _pressed = RightArrow; ExecuteRight(); }
        else if (input.Clicked(BankPicture) || input.Clicked(AccountField))
            Host.Show(new FinanceBookScreen(Host, this));
        else if (input.RightClick && !ActionBar.Contains(input.MouseX, input.MouseY))
            Host.Show(_back);
    }

    private void UpdateHover()
    {
        if (_mouseX < 0)
            return;
        if (PartnerBox(0).Contains(_mouseX, _mouseY))
            Select(new Selection(SelectionKind.Bank, 0));
        var partners = Partners;
        for (var i = 0; i < partners.Count; i++)
            if (PartnerBox(i + 1).Contains(_mouseX, _mouseY))
                Select(new Selection(SelectionKind.Manager, partners[i]));
        for (var i = 0; i < 4; i++)
            if (StockRow(i).Contains(_mouseX, _mouseY))
                Select(new Selection(SelectionKind.Stock, i));
    }

    private void Select(Selection selection)
    {
        if (selection == _selection)
            return;
        if ((selection.Kind == SelectionKind.Stock) != (_selection.Kind == SelectionKind.Stock))
            _leftAmount = _rightAmount = 0;
        _selection = selection;
    }

    /// <summary>Der Kursverlauf läuft langsam zur gewählten Aktie (Video 5:58 und 6:15).</summary>
    private void ScrollChart()
    {
        if (_selection.Kind != SelectionKind.Stock || double.IsNaN(_viewPrice))
            return;
        var target = Game.Stocks.Stocks[_selection.Index].Price;
        var delta = target - _viewPrice;
        _viewPrice = Math.Abs(delta) <= ScrollSpeed ? target : _viewPrice + Math.Sign(delta) * ScrollSpeed;
    }

    /// <summary>[APPROX] Schrittweite: 10.000 DM bzw. 10 Aktien je Klick.</summary>
    private long Step => _selection.Kind == SelectionKind.Stock ? 10 : 10_000;

    private void Change(bool left, int steps, Rect button)
    {
        _pressed = button;
        if (left)
            _leftAmount = Math.Clamp(_leftAmount + steps * Step, 0, MaxLeft());
        else
            _rightAmount = Math.Clamp(_rightAmount + steps * Step, 0, MaxRight());
    }

    /// <summary>Höchstbetrag der linken Buchung (holen, leihen, kaufen).</summary>
    public long MaxLeft() => _selection.Kind switch
    {
        SelectionKind.Bank => Game.MaxWithdrawal(Me),
        SelectionKind.Manager => Math.Max(0, Game.Managers[_selection.Index].Money),
        _ => Math.Max(0, Me.Money) / Game.Stocks.Stocks[_selection.Index].Price,
    };

    /// <summary>Höchstbetrag der rechten Buchung (einzahlen, verleihen, verkaufen).</summary>
    public long MaxRight() => _selection.Kind switch
    {
        SelectionKind.Stock => Game.Finance(Me).Holdings[_selection.Index].Count,
        _ => Math.Max(0, Me.Money),
    };

    private void ExecuteLeft()
    {
        var amount = Math.Min(_leftAmount, MaxLeft());
        switch (_selection.Kind)
        {
            case SelectionKind.Bank:
                Game.WithdrawFromBank(Me, amount);
                break;
            case SelectionKind.Manager:
                // [APPROX] Laut Anleitung muss der andere Manager zustimmen (Angebot im Posteingang) – folgt mit „Neuer“.
                Game.LendBetweenManagers(_selection.Index, Game.CurrentManager, amount);
                break;
            default:
                Game.BuyStock(Me, _selection.Index, (int)amount);
                break;
        }
        _leftAmount = 0;
    }

    private void ExecuteRight()
    {
        var amount = Math.Min(_rightAmount, MaxRight());
        switch (_selection.Kind)
        {
            case SelectionKind.Bank:
                Game.DepositToBank(Me, amount);
                break;
            case SelectionKind.Manager:
                Game.PayToManager(Game.CurrentManager, _selection.Index, amount);
                break;
            default:
                Game.SellStock(Me, _selection.Index, (int)amount);
                break;
        }
        _rightAmount = 0;
    }

    /// <summary>Hinweistexte (zweizeilig) wie in em.exe; „#“ ist im Spielzeichensatz das Prozentzeichen.</summary>
    public (string[] Left, string[] Right) Hints() => _selection.Kind switch
    {
        SelectionKind.Bank => (["Geld von der Bank", "abheben/leihen (4#)."], ["Geld aufs Sparbuch", "einzahlen (1#)"]),
        SelectionKind.Manager => (
            [$"Geld von {Game.Managers[_selection.Index].Name}", "leihen (2.5#)."],
            [$"Geld an {Game.Managers[_selection.Index].Name}", "verleihen (2.5#)."]),
        _ => (
            [$"Aktien von {Game.Stocks.Stocks[_selection.Index].Name}", "kaufen."],
            [$"Aktien von {Game.Stocks.Stocks[_selection.Index].Name}", "verkaufen."]),
    };

    // ---- Zeichnen --------------------------------------------------------------------------

    public override void Draw(Surface s)
    {
        s.Palette = Assets.Palette;
        s.Blit(Assets.Image(Background), 0, 0);
        DrawPartners(s);
        DrawActionBar(s);
        for (var i = 0; i < 4; i++)
            DrawStock(s, i);
        DrawChart(s);
        DrawSummary(s);
    }

    private void DrawPartners(Surface s)
    {
        var mini = Assets.Mini;
        var bank = Game.Finance(Me).Bank;
        s.TextCentered(mini, CaptionCenter(0), CaptionY1, ManagerText.Money(bank.Balance), TextColor);
        s.TextCentered(mini, CaptionCenter(0), CaptionY2, ManagerText.Money(bank.MonthlyInterest) + "/M.", TextColor);

        var partners = Partners;
        for (var i = 0; i < partners.Count; i++)
        {
            var other = Game.Managers[partners[i]];
            var head = HeadArea(i + 1);
            var src = Widgets.HeadSource(other.Head);
            s.Blit(Assets.Image(0), new Rect(src.X + 1, src.Y + 1, head.W, head.H), head.X, head.Y);
            var self = Game.CurrentManager;
            var balance = Game.LoanAmount(self, partners[i]) - Game.LoanAmount(partners[i], self);
            s.TextCentered(mini, CaptionCenter(i + 1), CaptionY1, ManagerText.Money(balance), TextColor);
            s.TextCentered(mini, CaptionCenter(i + 1), CaptionY2,
                ManagerText.Money(Game.ManagerLoanInterest(self, partners[i])) + "/M.", TextColor);
        }
    }

    private void DrawActionBar(Surface s)
    {
        var mini = Assets.Mini;
        if (ActionBar.Contains(_mouseX, _mouseY))
        {
            var (left, right) = Hints();
            var lines = _mouseX < 160 ? left : right;
            var center = _mouseX < 160 ? LeftCenter : RightCenter;
            s.TextCentered(mini, center, HintY1, lines[0], TextColor);
            s.TextCentered(mini, center, HintY2, lines[1], TextColor);
        }

        string Format(long amount) => _selection.Kind == SelectionKind.Stock ? ManagerText.Pieces(amount) : ManagerText.Money(amount);
        if (_leftAmount > 0)
            s.TextCentered(mini, LeftCenter, AmountY, Format(_leftAmount), TextColor);
        if (_rightAmount > 0)
            s.TextCentered(mini, RightCenter, AmountY, Format(_rightAmount), TextColor);

        // [APPROX] Gedrückter Knopf: Rahmen hervorheben (Original nicht im Video zu sehen).
        if (_pressed is { } p)
            s.Outline(p, Colors.White);
        if (_selection.Kind == SelectionKind.Stock)
            s.Outline(StockRow(_selection.Index) with { W = 35 + 4, X = 2, Y = StockRow(_selection.Index).Y + 1, H = 29 }, Colors.Yellow);
        else if (_selection.Kind == SelectionKind.Manager)
        {
            var box = Partners.IndexOf(_selection.Index) + 1;
            if (box > 0)
                s.Outline(HeadArea(box).Inflate(1), Colors.Yellow);
        }
    }

    private void DrawStock(Surface s, int i)
    {
        var mini = Assets.Mini;
        var stock = Game.Stocks.Stocks[i];
        var holding = Game.Finance(Me).Holdings[i];
        var color = StockColors[i];
        var y = StockTextY(i);
        s.Text(mini, StockTextX, y, $"Kurs: {ManagerText.Money(stock.Price)}", color);
        s.Text(mini, StockTextX, y + StockLinePitch, $"Zahl: {ManagerText.Pieces(holding.Count)}", color);
        s.Text(mini, StockTextX, y + 2 * StockLinePitch, $"Kauf:{ManagerText.Money(holding.PurchaseCost)}", color);
        s.Text(mini, StockTextX, y + 3 * StockLinePitch, $"Wert:{ManagerText.Money((long)holding.Count * stock.Price)}", color);
    }

    /// <summary>Bildschirmzeile eines Kurses beim aktuellen Bildausschnitt.</summary>
    public int ChartY(double price) => (int)Math.Floor(ChartCenterY - (price - _viewPrice) * PixelsPerDm);

    private void DrawChart(Surface s)
    {
        for (var i = 0; i < 4; i++)
        {
            var history = Game.Stocks.Stocks[i].History;
            var x0 = Chart.Right - history.Count;
            for (var k = 0; k < history.Count; k++)
            {
                var y = ChartY(history[k]);
                var yPrev = k == 0 ? y : ChartY(history[k - 1]);
                ChartColumn(s, x0 + k, Math.Min(y, yPrev), Math.Max(y, yPrev) + 1, StockColors[i]);
            }
        }
    }

    /// <summary>Senkrechtes Stück (2 Pixel dick wie im Video), auf das schwarze Feld beschnitten.</summary>
    private static void ChartColumn(Surface s, int x, int y0, int y1, byte color)
    {
        if (x < Chart.X || x >= Chart.Right)
            return;
        for (var y = Math.Max(y0, Chart.Y); y <= Math.Min(y1, Chart.Bottom - 1); y++)
            s.Set(x, y, color);
    }

    private void DrawSummary(Surface s)
    {
        var mini = Assets.Mini;
        void Label(int y, string text) => ManagerText.TwoTone(s, mini, LabelX, y, text, LabelTop, LabelBottom);
        void Value(int y, long amount) => s.TextRight(mini, ValueRight, y, ManagerText.Money(amount), TextColor);

        var finance = Game.Finance(Me);
        Label(105, "Kontostand:");
        s.TextCentered(mini, 278, 113, ManagerText.Money(Me.Money), Me.Money < 0 ? Colors.Red : AccountColor);
        Label(123, "Untereinander:");
        Value(131, Game.ManagerReceivables(Me));
        Value(139, -Game.ManagerDebts(Me));
        Label(149, "Bank:");
        Value(157, finance.Bank.Savings);
        Value(165, -finance.Bank.Credit);
        Label(175, "Aktien:");
        Label(183, "Kaufpreis:");
        var cost = Game.DepotPurchaseCost(Me);
        var value = Game.DepotValue(Me);
        Value(192, cost);
        Label(202, "Depot-Wert:");
        Value(211, value);
        Label(221, "Depot-Saldo:");
        Value(230, value - cost);
    }
}

/// <summary>Anmeldung im Hauptmenü (Manager → Finanzen).</summary>
internal static class FinanceMenuEntries
{
    [MenuEntry("Finanzen")]
    private static Screen Open(ScreenHost host, Screen back) => new FinanceScreen(host, back);
}
