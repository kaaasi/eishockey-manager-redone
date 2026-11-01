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
/// [ORIGINAL] Bank, Mitmanager und Aktie werden erst per Klick gewählt (em.exe 0x59B2D, Befund NB-02 #206; Klickfelder
/// <see cref="BankSelectArea"/>, <see cref="PartnerSelectArea"/>, <see cref="StockSelectArea"/>), Überfahren ändert nichts.
/// [APPROX] Im Video wird nichts gebucht. Bedienung daher angenommen: [+]/[−] stellen den Betrag der jeweiligen Seite ein (Rechtsklick = zehnfach),
/// ◄ führt die linke, ► die rechte Buchung aus; der Betrag steht im schwarzen Balken. Leihen bei einem Mitmanager
/// schickt ihm eine Anfrage in den Posteingang.
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

    /// <summary>
    /// [ORIGINAL] Klickfeld der Bank (em.exe 0x59BB8–0x59BCB: x &lt; 106, y &lt; 54). Das Bankbild darin öffnet zusätzlich
    /// das Finanzbuch (<see cref="BankPicture"/>).
    /// </summary>
    public static readonly Rect BankSelectArea = new(0, 0, 106, 54);

    /// <summary>
    /// [ORIGINAL] Klickfeld des Mitmanagers in Kasten 1–3 (em.exe 0x59B40–0x59B79: 117 + 71·k &lt; x &lt; 174 + 71·k,
    /// y &lt; 54, nur wenn der Kasten besetzt ist).
    /// </summary>
    public static Rect PartnerSelectArea(int box) => new(118 + (box - 1) * 71, 0, 56, 54);

    /// <summary>
    /// [ORIGINAL] Klickfeld der Aktie i (em.exe 0x59BF2–0x59C35: x &lt; 47, y &gt; 101, i = (y − 102) / 34) – nur die
    /// Logospalte, nicht das Infofeld.
    /// </summary>
    public static Rect StockSelectArea(int i) => new(0, 102 + i * 34, 47, 34);

    /// <summary>Zeile einer Aktie: Logo (35 × 25) und Infofeld (83 × 30).</summary>
    public static Rect StockRow(int i) => new(4, 103 + i * 69 / 2, 125, 30);
    private static int StockTextY(int i) => 105 + i * 35;
    private const int StockTextX = 47;
    private const int StockLinePitch = 7;

    /// <summary>Schwarzes Feld des Kursverlaufs im Hintergrund kb\152 (110 × 133).</summary>
    public static readonly Rect Chart = new(132, 104, 110, 133);

    /// <summary>
    /// [ORIGINAL] Zeichenfläche des Kursverlaufs (em.exe 0x588CA): Puffer 106 × 130, kopiert nach (134 | 107).
    /// Tag k des Verlaufs liegt bei x = 3·k, also 36 Tage über die ganze Breite.
    /// </summary>
    public static readonly Rect ChartCanvas = new(134, 107, 106, 130);

    /// <summary>[ORIGINAL] Waagrechter Abstand zweier Tage im Kursverlauf (0x5898E: Tag · 3).</summary>
    public const int ChartDayWidth = 3;

    /// <summary>
    /// [ORIGINAL] Standardmaßstab: Drittel-DM je Pixel (0x58CAF: 2, also 1,5 Pixel je DM). Der Regler (0x58E38 ff.)
    /// ändert ihn auf 2–64 (<see cref="Scale"/>).
    /// </summary>
    public const int ChartScale = 2;

    public static readonly Rect AccountField = new(246, 103, 70, 17);
    private const int LabelX = 249;
    private const int ValueRight = 304;

    // ---- Farben (Standardpalette) ----------------------------------------------------------

    /// <summary>
    /// [ORIGINAL] Schriftfarben der Aktien im Infofeld (em.exe 0x58304: DGROUP 0x552A = 14, 22, 17, 9; grün, blau, rot,
    /// orange), dieselben wie die Oberkante der Linien im Kursverlauf. Vorher aus dem Video geschätzt (15, 151, 105, 59).
    /// </summary>
    public static readonly byte[] StockColors = [14, 22, 17, 9];
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
    /// <summary>Unterkante des Ausschnitts in Drittel-DM (em.exe: Versatz des Kursverlaufs, Zeile 130 der Zeichenfläche).</summary>
    private int _offset = int.MinValue;
    /// <summary>Maßstab: Drittel-DM je Pixel (em.exe <c>[bp-0x1e]</c>), anfangs <see cref="ChartScale"/>.</summary>
    private int _scale = ChartScale;

    public FinanceScreen(ScreenHost host, Screen back) : base(host) => _back = back;

    public enum SelectionKind { Bank, Manager, Stock }

    /// <summary>Gewähltes Gegenüber: Bank, Mitmanager (Index im Spiel) oder Aktie (0–3).</summary>
    public readonly record struct Selection(SelectionKind Kind, int Index);

    public Selection Selected => _selection;
    public long LeftAmount => _leftAmount;
    public long RightAmount => _rightAmount;

    /// <summary>Kurs in DM auf der mittleren Zeile der Zeichenfläche (Zeile 65, Bildschirmzeile 172).</summary>
    public double ViewPrice => (_offset + ChartCanvas.H) / 3.0;

    /// <summary>Unterkante des sichtbaren Ausschnitts in Drittel-DM (<see cref="InitialChartOffset"/>).</summary>
    public int ChartOffset => _offset;

    /// <summary>Maßstab des Kursverlaufs in Drittel-DM je Pixel (2 = Standard).</summary>
    public int Scale => _scale;

    private GameState Game => Host.Game!;
    private Core.Manager Me => Game.ActiveManager;

    /// <summary>Mitmanager in der Reihenfolge der Kästen 1–3.</summary>
    private List<int> Partners => Enumerable.Range(0, Game.Managers.Count).Where(i => i != Game.CurrentManager).Take(3).ToList();

    public override void Enter()
    {
        // Rückkehr aus dem Finanzbuch mit gewählter Aktie: Ausschnitt der Aktie wie vorher (bis #206 setzte ihn das
        // Überfahren in jedem Bild neu).
        _offset = _selection.Kind == SelectionKind.Stock ? StockOffset(_selection.Index) : InitialChartOffset(Game.Stocks);
        _scale = ChartScale;
        _dragging = false;
        _markerX = -1;
    }

    // ---- Regler für Ausschnitt und Maßstab (#196) -------------------------------------------

    /// <summary>
    /// [ORIGINAL] Klickfeld des Reglers (em.exe 0x58D3A–0x58D72): x 131–242, Mauszeile 107–235, nur linke Taste.
    /// x &lt; 180 (0x58DA7) wählt den Ausschnitt (Streifen bei x 132), sonst den Maßstab (Streifen bei x 240).
    /// </summary>
    public static readonly Rect RegulatorArea = new(131, 107, 112, 129);

    private const int RegulatorSplitX = 180;
    private const int RegulatorMinY = 0x68;
    private const int RegulatorMaxY = 0xEA;
    private const int RegulatorStripTop = 0x68;
    private const int RegulatorStripHeight = 0xEC - 0x68 + 1;
    private const byte RegulatorMarkerColor = 0x18;
    private const byte RegulatorStripColor = 0;

    private bool _dragging;
    private bool _dragOffset;
    private bool _dragFirst;
    private int _markerX = -1;
    private int _markerY;

    /// <summary>Höchster Wert aller Verläufe in Drittel-DM (em.exe 0x58BED–0x58C1A, <c>[bp-0x20]</c>).</summary>
    private int HighestHistory() => Game.Stocks.Stocks.Max(st => st.History.Count == 0 ? 0 : st.History.Max()) * 3;

    /// <summary>
    /// [ORIGINAL] Ziehen mit der linken Taste (0x58D1E–0x58F4D): Beim Drücken im Feld wird der Modus aus x festgelegt;
    /// solange die Taste gehalten wird, gilt die Mauszeile (ab der zweiten Runde auf 0x68–0xEA begrenzt).
    /// Ausschnitt: Versatz = Maximum − (y − 106) · Maximum / 131 (0x58DF6–0x58E35);
    /// Maßstab: (32 − (y − 106) · 32 / 131) · 2, mindestens 2 (0x58E7E–0x58EC5). Beide Rechnungen mit abgeschnittener
    /// Ganzzahldivision. [APPROX] Streifen und Marke bleiben bis zum Verlassen des Bildschirms stehen (das Original
    /// löscht sie nach dem Loslassen nicht; ein späteres Neuzeichnen des Bildschirms ist nicht nachgeprüft).
    /// </summary>
    private void UpdateRegulator(InputState input)
    {
        if (input.LeftClick && RegulatorArea.Contains(input.MouseX, input.MouseY))
        {
            _dragging = true;
            _dragFirst = true;
            _dragOffset = input.MouseX < RegulatorSplitX;
        }
        if (!_dragging)
            return;
        if (!input.LeftDown && !input.LeftClick)
        {
            _dragging = false;
            return;
        }

        var y = _dragFirst ? input.MouseY : Math.Clamp(input.MouseY, RegulatorMinY, RegulatorMaxY);
        _dragFirst = false;
        _markerX = _dragOffset ? 0x84 : 0xF0;
        _markerY = y;
        if (_dragOffset)
        {
            var highest = HighestHistory();
            _offset = highest - (int)((long)(y - 106) * highest / 131);
        }
        else
        {
            _scale = Math.Max(2, (32 - (y - 106) * 32 / 131) * 2);
        }
    }

    private void DrawRegulator(Surface s)
    {
        if (_markerX < 0)
            return;
        s.Fill(new Rect(_markerX, RegulatorStripTop, 2, RegulatorStripHeight), RegulatorStripColor);
        s.Fill(new Rect(_markerX, _markerY, 2, 2), RegulatorMarkerColor);
    }

    /// <summary>
    /// [ORIGINAL] Anfänglicher Ausschnitt (em.exe 0x58BD4–0x58CAC): Ausgehend vom mittleren Kurs (Summe der vier Kurse in
    /// Drittel-DM / 4) werden Versätze von Mittel − 200 (mindestens 0) bis unter Mittel + 200 in Schritten von 10 geprüft;
    /// je Versatz zählt, wie viele der 4 × 36 Verlaufspunkte echt innerhalb der Fläche liegen (0 &lt; Kurs/2 − Versatz/2
    /// &lt; 130). Der erste Versatz mit den meisten Punkten gewinnt (ab mehr als 140 Punkten bricht die Suche ab).
    /// Das Original merkt sich dabei den schon um 10 erhöhten Versatz – der Ausschnitt liegt also 10 Drittel (5 Pixel)
    /// höher als der gezählte; das ist hier übernommen.
    /// </summary>
    public static int InitialChartOffset(StockMarket market)
    {
        var average = market.Stocks.Sum(s => s.Price * 3) / 4;
        var offset = Math.Max(0, average - 200);
        var best = 0;
        var bestOffset = 0; // [APPROX] im Original nicht vorbelegt (Stapelwert), wenn kein Punkt sichtbar ist
        while (average + 200 > offset)
        {
            var count = 0;
            foreach (var stock in market.Stocks)
                foreach (var price in stock.History)
                {
                    var dy = price * 3 / 2 - offset / 2;
                    if (dy > 0 && dy < ChartCanvas.H)
                        count++;
                }
            offset += 10;
            if (count > best)
            {
                best = count;
                bestOffset = offset;
                if (count > 140)
                    break;
            }
        }
        return bestOffset;
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

        UpdateRegulator(input);

        if (input.LeftClick && SelectAt(input.MouseX, input.MouseY) && _selection.Kind == SelectionKind.Stock)
            _offset = StockOffset(_selection.Index);

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

    /// <summary>
    /// [ORIGINAL] Auswahl nur bei linker Maustaste (em.exe 0x59B2D: <c>DGROUP 0x5A16 == 1</c>, sonst weiter bei 0x59D42).
    /// Gibt zurück, ob ein Klickfeld getroffen wurde.
    /// </summary>
    private bool SelectAt(int x, int y)
    {
        if (BankSelectArea.Contains(x, y))
        {
            Select(new Selection(SelectionKind.Bank, 0));
            return true;
        }
        var partners = Partners;
        for (var i = 0; i < partners.Count; i++)
            if (PartnerSelectArea(i + 1).Contains(x, y))
            {
                Select(new Selection(SelectionKind.Manager, partners[i]));
                return true;
            }
        for (var i = 0; i < 4; i++)
            if (StockSelectArea(i).Contains(x, y))
            {
                Select(new Selection(SelectionKind.Stock, i));
                return true;
            }
        return false;
    }

    private void Select(Selection selection)
    {
        if (selection == _selection)
            return;
        if ((selection.Kind == SelectionKind.Stock) != (_selection.Kind == SelectionKind.Stock))
            _leftAmount = _rightAmount = 0;
        _selection = selection;
    }

    /// <summary>
    /// [ORIGINAL] Klick auf eine Aktie setzt den Ausschnitt sofort (em.exe 0x59C6B–0x59C8F, innerhalb der Klickauswahl ab
    /// 0x59B2D): Versatz = Kurs in Drittel-DM − 130, die Aktie liegt dann auf der mittleren Zeile. Entscheidung Nutzer
    /// (30.10.2026, #188): sofort wie im Original, nicht langsam nach dem Video; Befund NB-02 (#206): erst beim Klick,
    /// nicht schon beim Überfahren (Deutung aus #188 berichtigt).
    /// </summary>
    private int StockOffset(int stock) => Game.Stocks.Stocks[stock].Price * 3 - ChartCanvas.H;

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
                // Der andere Manager bekommt eine Anfrage in den Posteingang und muss zustimmen.
                Game.RequestLoan(Game.CurrentManager, _selection.Index, amount);
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
        DrawRegulator(s);
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

    /// <summary>
    /// [ORIGINAL] Zeile eines Kurses in der Zeichenfläche (0x58922): 130 − Kurs/Maßstab + Versatz/Maßstab, Kurs in
    /// Drittel-DM, beide Divisionen ganzzahlig. Zeile 0 = Bildschirmzeile 107.
    /// </summary>
    public static int CanvasY(int price, int offset, int scale = ChartScale) => ChartCanvas.H - price * 3 / scale + offset / scale;

    /// <summary>Bildschirmzeile eines Kurses beim aktuellen Bildausschnitt.</summary>
    public int ChartY(int price) => ChartCanvas.Y + CanvasY(price, _offset, _scale);

    /// <summary>
    /// [ORIGINAL] Kursverlauf (0x588CA): je Aktie (0–3, spätere übermalen frühere) eine Linie von Tag zu Tag, drei Pixel
    /// dick – zwei Zeilen in der Farbe DGROUP 0x5533 + Aktie, die dritte in 0x5537 + Aktie (Segment 0x585C2).
    /// </summary>
    private void DrawChart(Surface s)
    {
        for (var i = 0; i < 4; i++)
        {
            var history = Game.Stocks.Stocks[i].History;
            for (var k = 1; k < history.Count; k++)
                ChartSegment(s, k * ChartDayWidth, CanvasY(history[k - 1], _offset, _scale), CanvasY(history[k], _offset, _scale), i);
        }
    }

    /// <summary>[ORIGINAL] Linienfarben je Aktie: oben DGROUP 0x5533 (14, 22, 17, 9), unten 0x5537 (12, 8, 16, 27).</summary>
    public static readonly byte[] ChartTopColors = [14, 22, 17, 9];

    /// <inheritdoc cref="ChartTopColors"/>
    public static readonly byte[] ChartBottomColors = [12, 8, 16, 27];

    /// <summary>
    /// [ORIGINAL] Ein Abschnitt des Verlaufs (0x585C2): von (x1 − 3 | y0) nach (x1 | y1). Liegen beide Enden über bzw.
    /// unter der Fläche, entfällt er; sonst werden die Enden auf die Zeilen 0 und 130 gekürzt (lineare Teilung,
    /// ganzzahlig). Danach zwei Linien in der oberen Farbe (y, y + 1) und eine in der unteren (y + 2).
    /// [APPROX] Pixelschritte der Linie nach Bresenham; die Linienroutine des Originals ist nicht nachgeprüft.
    /// </summary>
    private static void ChartSegment(Surface s, long x, long yPrevious, long yCurrent, int stock)
    {
        // a = Vortag, b = Tag; im Original (x0 | y0) und (x1 | y1), gekürzt wird zuerst b.
        var xa = x - ChartDayWidth;
        var ya = yPrevious;
        var xb = x;
        var yb = yCurrent;
        const long bottom = 130;
        if ((ya > bottom && yb > bottom) || (ya < 0 && yb < 0))
            return;
        if (yb > bottom)
        {
            xb += (xa - xb) * (bottom - yb) / (ya - yb);
            yb = bottom;
        }
        if (yb < 0)
        {
            xb += (xa - xb) * (0 - yb) / (ya - yb);
            yb = 0;
        }
        if (ya > bottom)
        {
            xa += (xb - xa) * (bottom - ya) / (yb - ya);
            ya = bottom;
        }
        if (ya < 0)
        {
            xa += (xb - xa) * (0 - ya) / (yb - ya);
            ya = 0;
        }
        if (xa < 0 || xa > 107 || ya < 0 || ya > bottom || xb < 0 || xb > 107 || yb < 0 || yb > bottom)
            return;
        Line(s, (int)xa, (int)ya, (int)xb, (int)yb, ChartTopColors[stock]);
        Line(s, (int)xa, (int)ya + 1, (int)xb, (int)yb + 1, ChartTopColors[stock]);
        Line(s, (int)xa, (int)ya + 2, (int)xb, (int)yb + 2, ChartBottomColors[stock]);
    }

    /// <summary>Linie in der Zeichenfläche, beschnitten auf 106 × 130 (das Original kopiert nur diesen Teil).</summary>
    private static void Line(Surface s, int x0, int y0, int x1, int y1, byte color)
    {
        int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        var err = dx + dy;
        while (true)
        {
            if (x0 >= 0 && x0 < ChartCanvas.W && y0 >= 0 && y0 < ChartCanvas.H)
                s.Set(ChartCanvas.X + x0, ChartCanvas.Y + y0, color);
            if (x0 == x1 && y0 == y1)
                break;
            var e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
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
