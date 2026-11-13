using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens.ManagerArea;

/// <summary>
/// Bildschirm Finanzen – Bank und Börse (Hintergrund kb\152, ganzer Bildschirm). Vermessen aus dem UI-Video (5:40–6:20).
/// <list type="bullet">
/// <item>Oben die Bank und drei Kästen für die Mitmanager (Kopf), darunter Saldo und Zinsen pro Monat.</item>
/// <item>Mitte: Knöpfe [+][−] links und [−][+] rechts, Pfeile ◄ ►, darunter der Betragsbalken (em.exe Datei 0x58409).
///   Ein Klick in die linke bzw. rechte Hälfte wählt die Seite und zeigt ihren Hinweis („Geld von der Bank
///   abheben/leihen (4%).“, „Aktien von … kaufen.“). Linke Hälfte = holen/leihen/kaufen, rechte = einzahlen/verleihen/verkaufen.</item>
/// <item>Unten die vier Aktien (Kurs, Zahl, Kauf, Wert), der Kursverlauf und rechts die Übersicht.</item>
/// </list>
/// Das Finanzbuch (Remake-Erweiterung A311) öffnet der Knopf <see cref="BookButton"/> (#840); Bankbild und Kontostand wählen
/// wie im Original nur die Bank bzw. tun nichts.
/// </summary>
/// <remarks>
/// [ORIGINAL] Bank, Mitmanager und Aktie werden erst per Klick gewählt (em.exe 0x59B2D, Befund NB-02 #206; Klickfelder
/// <see cref="BankSelectArea"/>, <see cref="PartnerSelectArea"/>, <see cref="StockSelectArea"/>), Überfahren ändert nichts.
/// [ORIGINAL] Bedienung nach em.exe Datei 0x58A57 (Spur geld Teil 5, G4; docs/finanzen-rekonstruktion.md, „Bedienung“):
/// Klick in den Streifen y 73–100 wählt die Seite (Vorgabe je Auswahl im Balken), [+]/[−] ändern den Betrag um 1.000 DM
/// bzw. 1 Stück (gehalten wiederholt), ein Klick auf den Balken setzt Pixel · q, ein Klick in die Zeile y 73–88 bucht.
/// Leihen beim Mitmanager geht sofort (Entscheidung E2).
/// </remarks>
public sealed class FinanceScreen : Screen
{
    public const int Background = 152;

    // ---- Vermessung (kb\152 und Video) -------------------------------------------------------

    /// <summary>Kasten 0 = Bank, 1–3 = Mitmanager.</summary>
    public static Rect PartnerBox(int i) => i == 0 ? new Rect(3, 2, 101, 53) : new Rect(118 + (i - 1) * 71, 2, 57, 53);

    /// <summary>Bankbild; ein Klick wählt nur die Bank (<see cref="BankSelectArea"/>, #840).</summary>
    public static readonly Rect BankPicture = new(21, 4, 66, 49);

    /// <summary>
    /// Knopf „Buch“ zum Finanzbuch (#840, Nutzerwunsch 13.11.2026). [APPROX] Remake-Erweiterung ohne Vorbild: freie Fläche links
    /// unter dem Bankkasten, außerhalb von Bank-Klickfeld (y &lt; 54) und Streifen (y 73–100), Baustein <see cref="SmallButton"/>.
    /// </summary>
    public static readonly Rect BookButton = new(4, 58, 30, Widgets.ButtonHeight);

    /// <summary>Schwarze Fläche für den Managerkopf (37 × 38).</summary>
    public static Rect HeadArea(int i) => new(128 + (i - 1) * 71, 10, 37, 38);

    private static int CaptionCenter(int i) => 75 + i * 71;
    private const int CaptionY1 = 57;
    private const int CaptionY2 = 65;

    /// <summary>
    /// [ORIGINAL] Klickfelder der Knöpfe (em.exe Datei 0x59050–0x590A1: Mauszeile 74–87, Spalte 4–34 links bzw. 284–314
    /// rechts; Datei 0x58F80–0x58FA1: „+“ links bei x &lt; 19, rechts bei x &gt; 299, sonst „−“). Sie wirken nur auf der
    /// gewählten Seite.
    /// </summary>
    public static readonly Rect LeftPlus = new(4, 74, 15, 14);
    /// <inheritdoc cref="LeftPlus"/>
    public static readonly Rect LeftMinus = new(19, 74, 16, 14);
    /// <inheritdoc cref="LeftPlus"/>
    public static readonly Rect RightMinus = new(284, 74, 16, 14);
    /// <inheritdoc cref="LeftPlus"/>
    public static readonly Rect RightPlus = new(300, 74, 15, 14);

    /// <summary>Pfeile ◄ ► im Hintergrund. Gebucht wird mit einem Klick irgendwo in <see cref="BookingRow"/>.</summary>
    public static readonly Rect LeftArrow = new(150, 76, 9, 10);
    /// <inheritdoc cref="LeftArrow"/>
    public static readonly Rect RightArrow = new(160, 76, 9, 10);

    /// <summary>[ORIGINAL] Streifen, in dem ein Klick die Seite wählt (Datei 0x59531–0x59556: 72 &lt; y &lt; 101).</summary>
    public static readonly Rect ActionBar = new(0, 73, 320, 28);

    /// <summary>[ORIGINAL] Zeile, in der ein Klick bucht (Datei 0x5911D–0x5915C: 72 &lt; y &lt; 89, Betrag ≠ 0).</summary>
    public static readonly Rect BookingRow = new(0, 73, 320, 16);

    /// <summary>[ORIGINAL] Klickfeld des Balkens (Datei 0x590AD–0x590DE: 3 &lt; x &lt; 315, x ≠ 159, 89 &lt; y &lt; 99).</summary>
    public static readonly Rect AmountBarArea = new(4, 90, 311, 9);

    /// <summary>[ORIGINAL] Fläche des Balkens (Datei 0x5851F–0x5853A: x 4–313, y 89–96, Farbe 0).</summary>
    public static readonly Rect AmountBar = new(4, 89, 310, 8);

    /// <summary>[ORIGINAL] Linke Kante der Beschriftung: 36 links, 167 rechts (Datei 0x596A9–0x596B5: Seite · 131 + 36).</summary>
    public static int SideX(bool right) => right ? 167 : 36;

    /// <summary>[ORIGINAL] Texte zentriert zwischen x und x + 115 (Datei 0x0B6A5: x + 57 − Breite / 2).</summary>
    private static int SideCenter(bool right) => SideX(right) + 115 / 2;

    private const int HintY1 = 75;  // [ORIGINAL] Grundlinie 80 (Datei 0x59A94), Oberkante 75
    private const int HintY2 = 81;  // [ORIGINAL] Grundlinie 86 (Datei 0x59ABA)
    private const int AmountY = 90; // [ORIGINAL] Grundlinie 95 (Datei 0x58589)

    /// <summary>
    /// [ORIGINAL] Klickfeld der Bank (em.exe 0x59BB8–0x59BCB: x &lt; 106, y &lt; 54); sonst ohne Wirkung (#840).
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

    private const int LabelX = 249;
    private const int ValueRight = 304;

    // ---- Farben (Standardpalette) ----------------------------------------------------------

    /// <summary>
    /// [ORIGINAL] Schriftfarben der Aktien im Infofeld (em.exe 0x58304: DGROUP 0x552A = 14, 22, 17, 9; grün, blau, rot,
    /// orange), dieselben wie die Oberkante der Linien im Kursverlauf. Vorher aus dem Video geschätzt (15, 151, 105, 59).
    /// </summary>
    public static readonly byte[] StockColors = [14, 22, 17, 9];
    private const byte TextColor = 212;

    /// <summary>[ORIGINAL] Farbe von Hinweis und Betrag (Datei 0x58586, 0x59A91: 0x1E).</summary>
    public const byte BarTextColor = 30;

    /// <summary>[ORIGINAL] Farbe des Balkens (Datei 0x58548: 9).</summary>
    public const byte BarColor = 9;

    /// <summary>[ORIGINAL] Rahmen der Auswahl (Datei 0x59B7C, 0x59BCF: 7 für Mitmanager und Bank; 0x59C38: 0x18 für Aktien).</summary>
    public const byte SelectionColor = 7;
    /// <inheritdoc cref="SelectionColor"/>
    public const byte StockSelectionColor = 0x18;

    // ---- Zustand ---------------------------------------------------------------------------

    private readonly Screen _back;
    private Selection _selection = new(SelectionKind.None, 0);
    /// <summary>Gewählte Seite: null = keine, false = links, true = rechts (em.exe <c>[bp-0x1c]</c> 0/36/167).</summary>
    private bool? _side;
    /// <summary>Betrag der Seite; null = noch keiner (em.exe −1, Datei 0x596B8).</summary>
    private long? _amount;
    private long _max;
    /// <summary>Zuletzt gezeichneter Balken (das Original zeichnet bei Betrag 0 nichts neu, Datei 0x584B0).</summary>
    private (long Amount, int Pixels)? _bar;
    /// <summary>Rahmen früherer Auswahlen, im Original mit Farbe 0 übermalt (Datei 0x59CA3 ff.).</summary>
    private readonly List<Rect> _erased = [];
    private int _holdDirection;
    private int _holdWait;
    private int _holdSteps;
    /// <summary>Unterkante des Ausschnitts in Drittel-DM (em.exe: Versatz des Kursverlaufs, Zeile 130 der Zeichenfläche).</summary>
    private int _offset = int.MinValue;
    /// <summary>Maßstab: Drittel-DM je Pixel (em.exe <c>[bp-0x1e]</c>), anfangs <see cref="ChartScale"/>.</summary>
    private int _scale = ChartScale;

    public FinanceScreen(ScreenHost host, Screen back) : base(host) => _back = back;

    /// <summary>[ORIGINAL] Anfangs ist nichts gewählt (em.exe Datei 0x58A68: <c>[bp-0x16]</c> = 0xFF).</summary>
    public enum SelectionKind { None, Bank, Manager, Stock }

    /// <summary>Gewähltes Gegenüber: Bank, Mitmanager (Index im Spiel) oder Aktie (0–3).</summary>
    public readonly record struct Selection(SelectionKind Kind, int Index);

    public Selection Selected => _selection;

    /// <summary>Gewählte Seite: null = keine, false = links (holen/leihen/kaufen), true = rechts.</summary>
    public bool? ActiveSide => _side;

    /// <summary>Betrag im Balken in DM bzw. Stück; null = keiner.</summary>
    public long? Amount => _amount;

    /// <summary>Höchstwert der gewählten Seite (beim Wählen der Seite bestimmt).</summary>
    public long MaxAmount => _max;

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
        _erased.Clear();
        _holdDirection = 0;
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
        if (input.Pressed(Key.Escape) || input.RightClick)
        {
            Host.Show(_back);
            return;
        }

        if (_holdDirection != 0 && Hold(input))
            return;

        var regulator = input.LeftClick && RegulatorArea.Contains(input.MouseX, input.MouseY);
        UpdateRegulator(input);
        // [ORIGINAL] Verlässt die Maus die Zeilen des Streifens samt Beschriftung (y 73–100), verschwinden Seite und Balken sofort,
        // ohne Klick (Datei 0x59531–0x5955F → 0x59ADD; DOSBox-Sitzung 2, `file41_2…8`; A313).
        if (_side is not null && !ActionBar.Contains(input.MouseX, input.MouseY))
            Deactivate();
        if (!input.LeftClick)
            return;
        var (x, y) = (input.MouseX, input.MouseY);
        if (regulator)
        {
            // [ORIGINAL] Nach dem Regler (Datei 0x58F46 ff.) gilt die Taste als losgelassen: Seite aus, Auswahl bleibt.
            Deactivate();
            return;
        }

        if (_side is { } right && _amount is not null && ButtonArea(right).Contains(x, y))
        {
            _holdDirection = right ? (x > 299 ? 1 : -1) : (x < 19 ? 1 : -1);
            _holdSteps = 0;
            Step();
            return;
        }
        if (_side is { } side && AmountBarArea.Contains(x, y) && x != 159)
        {
            SetPixels(side ? x - 160 : 158 - x);
            return;
        }
        if (BookingRow.Contains(x, y) && _side is not null && _amount is > 0 && _selection.Kind != SelectionKind.None)
        {
            Execute();
            Deactivate();
            Reselect(SelectionKind.None, 0);
            return;
        }
        if (ActionBar.Contains(x, y))
        {
            if (_selection.Kind != SelectionKind.None && _side != x > 159)
                Activate(x > 159);
            return;
        }
        if (BookButton.Contains(x, y))
        {
            Host.Show(new FinanceBookScreen(Host, this));
            return;
        }

        Deactivate();
        if (SelectAt(x, y) && _selection.Kind == SelectionKind.Stock)
            _offset = StockOffset(_selection.Index);
    }

    /// <summary>[ORIGINAL] Klickfeld der Knöpfe einer Seite (Datei 0x59050–0x590A1).</summary>
    public static Rect ButtonArea(bool right) => new(right ? 284 : 4, 74, 31, 14);

    /// <summary>
    /// [ORIGINAL] Gehaltener Knopf (Datei 0x58F80–0x590A1): weiter, solange die Taste gedrückt ist und die Maus im Klickfeld
    /// der Seite liegt; nach dem k-ten Schritt wartet das Original (8 − k) / 3 BIOS-Takte (Datei 0x5901A–0x5902B, Pause
    /// 0x130EE), ab dem achten Schritt nicht mehr. [APPROX] Takte in Bilder bei 60 Hz umgerechnet (2 Takte = 7, 1 Takt = 3,
    /// sonst 1 Bild je Schritt); die Schleife des Originals läuft ohne Pause so schnell wie das Zeichnen.
    /// </summary>
    private bool Hold(InputState input)
    {
        if (input.LeftClick || !input.LeftDown || _side is not { } right || !ButtonArea(right).Contains(input.MouseX, input.MouseY))
        {
            _holdDirection = 0;
            return false;
        }
        if (--_holdWait <= 0)
            Step();
        return true;
    }

    private static readonly int[] HoldFrames = [7, 7, 3, 3, 3, 1, 1];

    private void Step()
    {
        _holdWait = _holdSteps < HoldFrames.Length ? HoldFrames[_holdSteps] : 1;
        _holdSteps++;
        if (_amount is not { } amount || FinanceAmountBar.Press(amount, _holdDirection, _max, IsStock) is not { } next)
            return;
        _amount = next;
        DrawnBar(FinanceAmountBar.Pixels(amount + _holdDirection * FinanceAmountBar.Step(IsStock), _max, IsStock));
    }

    /// <summary>[ORIGINAL] Klick auf den Balken (Datei 0x590E0–0x5911A): s = x − 160 rechts bzw. 158 − x links.</summary>
    private void SetPixels(int pixels)
    {
        _amount = FinanceAmountBar.FromPixels(pixels, _max, IsStock);
        DrawnBar(Math.Clamp(pixels, 0, FinanceAmountBar.Divisor)); // [APPROX] negatives s (andere Hälfte) ohne Balken
    }

    /// <summary>[ORIGINAL] Datei 0x584AD: bei Betrag 0 zeichnet das Original nichts (der alte Balken bleibt stehen).</summary>
    private void DrawnBar(int pixels)
    {
        if (_amount is > 0 and var amount)
            _bar = (amount, pixels);
    }

    private bool IsStock => _selection.Kind == SelectionKind.Stock;

    /// <summary>
    /// [ORIGINAL] Seite wählen (Datei 0x59562–0x59A60): Hinweis zeigen, Höchstwert und Vorgabe bestimmen, Balken mit der
    /// Vorgabe zeichnen. Ohne Vorgabe bleibt der Betrag leer, die Knöpfe wirken dann erst nach einem Klick auf den Balken.
    /// </summary>
    private void Activate(bool right)
    {
        _side = right;
        _bar = null;
        _max = right ? MaxRight() : MaxLeft();
        var preset = right ? RightDefault() : LeftDefault();
        _amount = preset is { } v ? Math.Min(v, _max) : null;
        if (preset is { } p)
            DrawnBar(FinanceAmountBar.Pixels(p, _max, IsStock));
    }

    /// <summary>
    /// [ORIGINAL] Maus außerhalb des Streifens y 73–100 (auch ohne Klick, Datei 0x59531–0x5955F) oder Klick außerhalb
    /// (Datei 0x59ADD–0x59B28): Hinweis und Balken löschen, keine Seite.
    /// [APPROX] Der Betrag wird mit gelöscht; im Original bleibt er stehen, und ein späterer Klick in die Buchungszeile
    /// bucht ihn erneut mit der dann gültigen (auch leeren) Auswahl (Datei 0x5911D prüft weder Seite noch Auswahl).
    /// </summary>
    private void Deactivate()
    {
        _side = null;
        _amount = null;
        _bar = null;
        _holdDirection = 0;
    }

    /// <summary>
    /// [ORIGINAL] Auswahl nur bei linker Maustaste (em.exe 0x59B2D: <c>DGROUP 0x5A16 == 1</c>, sonst weiter bei 0x59D42).
    /// Jeder Linksklick außerhalb des Streifens hebt die bisherige Auswahl auf (Datei 0x59B37) und wählt neu, was getroffen
    /// ist. Gibt zurück, ob ein Klickfeld getroffen wurde.
    /// </summary>
    private bool SelectAt(int x, int y)
    {
        if (BankSelectArea.Contains(x, y))
            return Reselect(SelectionKind.Bank, 0);
        var partners = Partners;
        for (var i = 0; i < partners.Count; i++)
            if (PartnerSelectArea(i + 1).Contains(x, y))
                return Reselect(SelectionKind.Manager, partners[i]);
        for (var i = 0; i < 4; i++)
            if (StockSelectArea(i).Contains(x, y))
                return Reselect(SelectionKind.Stock, i);
        Reselect(SelectionKind.None, 0);
        return false;
    }

    /// <summary>
    /// [ORIGINAL] Neue Auswahl (Datei 0x59CA3 ff.): hat sie sich geändert, übermalt das Original den alten Rahmen mit Farbe 0.
    /// </summary>
    private bool Reselect(SelectionKind kind, int index)
    {
        var selection = new Selection(kind, index);
        if (selection != _selection && SelectionFrame(_selection) is { } old)
            _erased.Add(old);
        _selection = selection;
        return kind != SelectionKind.None;
    }

    /// <summary>
    /// [ORIGINAL] Rahmen der Auswahl (Umriss 0x0B5A7, Ecken eingeschlossen): Bank (23 | 6)–(83 | 50) (Datei 0x59BCF),
    /// Mitmanager in Kasten k = 1–3 (56 + 71·k | 9)–(95 + 71·k | 48) (Datei 0x59B7C), Aktie i (4 | 106 + 34·i)–(38 | 130 + 34·i)
    /// (Datei 0x59C38).
    /// </summary>
    public Rect? SelectionFrame(Selection selection) => selection.Kind switch
    {
        SelectionKind.Bank => new Rect(23, 6, 61, 45),
        SelectionKind.Manager when Partners.IndexOf(selection.Index) is >= 0 and var k => new Rect(127 + k * 71, 9, 40, 40),
        SelectionKind.Stock => new Rect(4, 106 + selection.Index * 34, 35, 25),
        _ => null,
    };

    /// <summary>
    /// [ORIGINAL] Klick auf eine Aktie setzt den Ausschnitt sofort (em.exe 0x59C6B–0x59C8F, innerhalb der Klickauswahl ab
    /// 0x59B2D): Versatz = Kurs in Drittel-DM − 130, die Aktie liegt dann auf der mittleren Zeile. Entscheidung Nutzer
    /// (30.10.2026, #188): sofort wie im Original, nicht langsam nach dem Video; Befund NB-02 (#206): erst beim Klick,
    /// nicht schon beim Überfahren (Deutung aus #188 berichtigt).
    /// </summary>
    private int StockOffset(int stock) => Game.Stocks.Stocks[stock].Price * 3 - ChartCanvas.H;

    /// <summary>Höchstbetrag der linken Buchung (holen, leihen, kaufen; em.exe Datei 0x596C2–0x598DC).</summary>
    public long MaxLeft() => _selection.Kind switch
    {
        SelectionKind.Bank => Game.MaxWithdrawal(Me),
        SelectionKind.Manager => Game.MaxBorrowFromManager(Game.CurrentManager, _selection.Index),
        // [ORIGINAL] Datei 0x596E7–0x59731: Konto / Kurs, 0 bei Konto ≤ 0; Kurs 0 [APPROX] 0 statt Absturz (#757).
        SelectionKind.Stock => Game.MaxStockPurchase(Me, _selection.Index),
        _ => 0,
    };

    /// <summary>Höchstbetrag der rechten Buchung (einzahlen, verleihen, verkaufen; em.exe Datei 0x5974E, 0x598DF–0x5997B).</summary>
    public long MaxRight() => _selection.Kind switch
    {
        SelectionKind.Bank => GameState.MaxDeposit(Me),
        SelectionKind.Manager => Game.MaxLendToManager(Game.CurrentManager, _selection.Index),
        SelectionKind.Stock => Game.Finance(Me).Holdings[_selection.Index].Count,
        _ => 0,
    };

    /// <summary>[ORIGINAL] Vorgabe links: Sparbuch bzw. Forderung (Datei 0x59872–0x598DC); beim Kauf keine.</summary>
    public long? LeftDefault() => _selection.Kind switch
    {
        SelectionKind.Bank => Game.WithdrawalDefault(Me),
        SelectionKind.Manager => Game.BorrowDefault(Game.CurrentManager, _selection.Index),
        // [ORIGINAL] Datei 0x59737: bei Konto ≤ 0 Betrag 0 statt keiner.
        SelectionKind.Stock when Me.Money <= 0 => 0,
        _ => null,
    };

    /// <summary>[ORIGINAL] Vorgabe rechts: Schuld höchstens Konto (Datei 0x59994–0x59A12) bzw. alle Aktien (0x5974E–0x59779).</summary>
    public long? RightDefault() => _selection.Kind switch
    {
        SelectionKind.Bank => Game.DepositDefault(Me),
        SelectionKind.Manager => Game.LendDefault(Game.CurrentManager, _selection.Index),
        SelectionKind.Stock => Game.Finance(Me).Holdings[_selection.Index].Count is > 0 and var count ? count : null,
        _ => null,
    };

    /// <summary>
    /// [ORIGINAL] Buchung (Datei 0x5915F–0x5952D): links holen/leihen/kaufen, rechts einzahlen/verleihen/verkaufen; danach
    /// ist nichts mehr gewählt. Ein Mitmanager wird nicht gefragt (Entscheidung E2).
    /// </summary>
    private void Execute()
    {
        var amount = _amount ?? 0;
        var right = _side == true;
        switch (_selection.Kind)
        {
            case SelectionKind.Bank when right:
                Game.DepositToBank(Me, amount);
                break;
            case SelectionKind.Bank:
                Game.WithdrawFromBank(Me, amount);
                break;
            case SelectionKind.Manager when right:
                Game.PayToManager(Game.CurrentManager, _selection.Index, amount);
                break;
            case SelectionKind.Manager:
                Game.BorrowFromManager(Game.CurrentManager, _selection.Index, amount);
                break;
            case SelectionKind.Stock when right:
                Game.SellStock(Me, _selection.Index, (int)amount);
                break;
            case SelectionKind.Stock:
                Game.BuyStock(Me, _selection.Index, (int)amount);
                break;
        }
    }

    /// <summary>
    /// [ORIGINAL] Hinweistexte (zweizeilig) aus DGROUP 0xE3C ff. (Datei 0x595CC–0x596A6); „#“ ist im Spielzeichensatz das
    /// Prozentzeichen. Bei Mitmanager und Aktie beginnt die zweite Zeile mit einem Leerzeichen (DGROUP 0xE54 ff., 0xE60 ff.).
    /// </summary>
    public (string[] Left, string[] Right) Hints() => _selection.Kind switch
    {
        SelectionKind.Bank => (["Geld von der Bank", "abheben/leihen (4#)."], ["Geld aufs Sparbuch", "einzahlen (1#)"]),
        SelectionKind.Manager => (
            [$"Geld von {Game.Managers[_selection.Index].Name}", " leihen (2.5#)."],
            [$"Geld an {Game.Managers[_selection.Index].Name}", " verleihen (2.5#)."]),
        SelectionKind.Stock => (
            [$"Aktien von {Game.Stocks.Stocks[_selection.Index].Name}", " kaufen."],
            [$"Aktien von {Game.Stocks.Stocks[_selection.Index].Name}", " verkaufen."]),
        _ => ([], []),
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
        SmallButton.Draw(s, Assets, BookButton, "Buch");
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
        if (_side is { } right)
        {
            var (left, rightHint) = Hints();
            var lines = right ? rightHint : left;
            s.TextCentered(mini, SideCenter(right), HintY1, lines[0], BarTextColor);
            s.TextCentered(mini, SideCenter(right), HintY2, lines[1], BarTextColor);

            // [ORIGINAL] Balken (Datei 0x58515–0x585A6): Fläche x 4–313 Farbe 0, dann rechts 160 … 159 + s bzw. links
            // 158 − s … 158 in Farbe 9, darüber der Betrag „v DM“ bzw. „v St.“.
            if (_bar is { } bar)
            {
                s.Fill(AmountBar, 0);
                // Links ist die Fläche einschließlich x = 158 ein Pixel breiter als rechts (Ecken eingeschlossen).
                s.Fill(right ? new Rect(160, 89, bar.Pixels, 8) : new Rect(158 - bar.Pixels, 89, bar.Pixels + 1, 8), BarColor);
                var text = IsStock ? ManagerText.Pieces(bar.Amount) : ManagerText.Money(bar.Amount);
                s.TextCentered(mini, SideCenter(right), AmountY, text, BarTextColor);
            }
        }

        // [ORIGINAL] Gedrückte Knöpfe zeichnet das Original nicht (Datei 0x58F80–0x5904F zeichnet nur den Balken, A316).
        foreach (var frame in _erased)
            s.Outline(frame, 0);
        if (SelectionFrame(_selection) is { } current)
            s.Outline(current, _selection.Kind == SelectionKind.Stock ? StockSelectionColor : SelectionColor);
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

    /// <summary>[ORIGINAL] Beschriftung der rechten Spalte (Datei 0x580AE–0x580DE): Farbe 0x1E, Schatten 6, einfarbig (#839).</summary>
    public const byte SummaryLabelColor = 0x1E;
    /// <inheritdoc cref="SummaryLabelColor"/>
    public const byte SummaryLabelShadow = 6;

    /// <summary>
    /// [ORIGINAL] Farbe eines Betrags der rechten Spalte nach Vorzeichen (Datei 0x5811F–0x5815B): 0x1E − 0x12 = 12 bei
    /// positivem, 0x1E − 0x0E = 16 bei negativem Betrag, sonst 0x1E; ohne Schatten (0xFF).
    /// </summary>
    public static byte SummaryValueColor(long amount) => amount > 0 ? (byte)12 : amount < 0 ? (byte)16 : SummaryLabelColor;

    private void DrawSummary(Surface s)
    {
        var mini = Assets.Mini;
        // [ORIGINAL] Datei 0x57ED6–0x5818D: 15 Zeilen aus der Tabelle DGROUP 0xDF0, Beschriftung auf Grundlinie y + 2 (danach
        // y + 10), Betrag auf Grundlinie y (danach y + 8), Start y = 0x6B. Grundlinien damit 109, 117, 127, … 233.
        void Label(int baseline, string text) => Competitions.CompetitionWidgets.Centered(s, mini, LabelX, LabelX, baseline, text,
            SummaryLabelColor, SummaryLabelShadow, left: true);
        void Value(int baseline, long amount) =>
            s.TextRight(mini, ValueRight, Competitions.CompetitionWidgets.Top(mini, baseline), ManagerText.Money(amount), SummaryValueColor(amount));

        var finance = Game.Finance(Me);
        Label(109, "Kontostand:");
        // [APPROX] Lage des Kontostands zentriert wie im Video (5:40–6:20); das Original füllt die Zahl auf (DGROUP 0x374 = 9).
        s.TextCentered(mini, 278, Competitions.CompetitionWidgets.Top(mini, 117), ManagerText.Money(Me.Money), SummaryValueColor(Me.Money));
        Label(127, "Untereinander:");
        Value(135, Game.ManagerReceivables(Me));
        Value(143, -Game.ManagerDebts(Me));
        Label(153, "Bank:");
        Value(161, finance.Bank.Savings);
        Value(169, -finance.Bank.Credit);
        Label(179, "Aktien:");
        Label(189, "Kaufpreis:");
        var cost = Game.DepotPurchaseCost(Me);
        var value = Game.DepotValue(Me);
        Value(197, cost);
        Label(207, "Depot-Wert:");
        Value(215, value);
        Label(225, "Depot-Saldo:");
        Value(233, value - cost);
    }
}

/// <summary>Anmeldung im Hauptmenü (Manager → Finanzen).</summary>
internal static class FinanceMenuEntries
{
    [MenuEntry("Finanzen")]
    private static Screen Open(ScreenHost host, Screen back) => new FinanceScreen(host, back);
}
