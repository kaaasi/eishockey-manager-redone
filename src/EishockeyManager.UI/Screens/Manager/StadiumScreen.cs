using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens.ManagerArea;

/// <summary>
/// Bildschirm Stadion: oben das Stadion (kb\14, 320 × 168, eigene Palette) mit den gebauten Teilen aus kb\8,
/// unten die Ausbaustufen, Plätze, Eintritt und Konto. Vermessen am UI-Video (8:00–8:30):
/// Überfahren eines Bereichs (im Bild oder in der Liste) zeigt „… ausbauen“, Klick öffnet die Stufenwahl mit ▲▼
/// und den Kosten; Klick auf die Stufe fragt „Möchten Sie Ihr Stadion ausbauen ?“.
/// </summary>
public sealed class StadiumScreen : Screen
{
    public const int Background = 14;
    public const int Parts = 8;
    public const int ImageHeight = 168;

    // ---- Vermessung ------------------------------------------------------------------------

    /// <summary>Zeile i der Liste links (Überdachung … Bowlingbahn), Abstand 7 Pixel.</summary>
    public static Rect FeatureRow(int i) => new(0, 172 + 7 * i, 190, 7);
    private const int ListColonX = 57;
    private const int ListValueX = 59;

    /// <summary>[ORIGINAL] Maus-y, oberhalb derer die rechte Taste nicht umschaltet (Datei 0x3DB62: <c>cmp si, 0xA7</c>, „größer“).</summary>
    public const int BuildSwitchY = 167;

    /// <summary>
    /// Hinweiszeile „… ausbauen“, Oberkante. [ORIGINAL] gemessen am DOSBox-Bild <c>file01_3_stadion</c> (Gastronomie, Referenz-Repo
    /// <c>original/spielstaende/bilder</c>): x = 213, y = 201 (vorher 200).
    /// </summary>
    public const int HintY = 201;

    /// <summary>Anzeige „Bau“ statt „Ist“ (nur lesen).</summary>
    public bool ShowsBuildTable => _showBuild;

    /// <summary>
    /// Wert einer Zeile der Teile-Tabelle ohne „: “. [ORIGINAL] Datei 0x3D4FE–0x3D5E9: Modus 1 ohne Zielstufe „(keine Bautätigkeiten)“
    /// (DGROUP 0xDD0), sonst Name der Zielstufe + „ (Noch “ + Restwochen + „ Wochen“ (bei genau 1 ohne das letzte Zeichen) + „)“.
    /// </summary>
    public static string FeatureValue(Stadium stadium, StadiumFeature feature, bool buildMode)
    {
        if (!buildMode)
            return stadium.LevelText(feature);
        var target = stadium.TargetLevel(feature);
        if (target == 0)
            return "(keine Bautätigkeiten)";
        var weeks = stadium.WeeksLeft(StadiumProject.Feature, feature);
        return Stadium.Features[(int)feature].Levels[target] + " (Noch " + weeks + (weeks == 1 ? " Woche" : " Wochen") + ")";
    }

    /// <summary>
    /// Wert einer Platzzeile. [ORIGINAL] Datei 0x3D63D–0x3D76A: Modus 0 die Plätze, Modus 1 ohne Bau „(kein Bau...)“ (DGROUP 0xDD4),
    /// sonst Plätze nach dem Bau + „ (“ + Restwochen + „)“.
    /// </summary>
    public static string PlacesValue(Stadium stadium, StadiumProject project, bool buildMode)
    {
        var seats = project == StadiumProject.Sitzplaetze;
        if (!buildMode)
            return Number(seats ? stadium.Seats : stadium.Standing);
        var target = seats ? stadium.SeatsTarget : stadium.StandingTarget;
        return target == 0 ? "(kein Bau...)" : Number(target) + " (" + stadium.WeeksLeft(project, StadiumFeature.Ueberdachung) + ")";
    }

    public static readonly Rect SeatsRow = new(212, 172, 104, 7);
    public static readonly Rect StandingRow = new(212, 179, 104, 7);

    /// <summary>
    /// [ORIGINAL] Pfeilpaare über <see cref="UI.ArrowPair"/> (0x120:0x3E, Halten 0): Eintritt Datei 0x3DD89 (192 | 189, 202 | 189),
    /// Stufenwahl 0x3E0BC (192 | 210, 202 | 210), Flächen 8 × 9. Linker Pfeil liefert −1: Eintritt = Eintritt − (−1) (0x3DDB3),
    /// Ziel = Ziel − (−1) (0x3E0D7), also links „mehr“. Geändert wird nur im Frame des Klicks (Ereignis 1, 0x3DD93/0x3E0CD):
    /// gehaltene Taste wiederholt nichts.
    /// </summary>
    public static readonly Rect TicketUp = new(192, 189, ArrowPair.HitWidth, ArrowPair.HitHeight);
    public static readonly Rect TicketDown = new(202, 189, ArrowPair.HitWidth, ArrowPair.HitHeight);
    public static readonly Rect LevelUp = new(192, 210, ArrowPair.HitWidth, ArrowPair.HitHeight);
    public static readonly Rect LevelDown = new(202, 210, ArrowPair.HitWidth, ArrowPair.HitHeight);
    private readonly ArrowPair _ticketArrows = new(TicketUp.X, TicketUp.Y, TicketDown.X, TicketDown.Y);
    private readonly ArrowPair _levelArrows = new(LevelUp.X, LevelUp.Y, LevelDown.X, LevelDown.Y);
    public static readonly Rect LevelBox = new(212, 208, 103, 11);
    // [ORIGINAL] gemessen am DOSBox-Bild file15_2_stadion (Referenz-Repo, FILE15.HOK; Spur geld Teil 3, Pixelprobe G7):
    // Beschriftung ab x = 213, Doppelpunkt x = 267, Werte ab x = 271, Konto-Betrag rechtsbündig bei 312, Eintritt-Zeile y = 190.
    private const int RightLabelX = 213;
    private const int RightColonX = 267;
    private const int RightValueX = 271;
    private const int AccountRight = 312;
    private const int TicketRowY = 190;

    /// <summary>
    /// Bereiche im Stadionbild. Gastronomie, Kino, Bowlingbahn, Überdachung und Verkehrsanbindung aus den
    /// Mauspositionen im Video; [APPROX] Anzeigetafel und Sitzkomfort an den Tribünen angenommen.
    /// [APPROX] Das Kino liegt seit der Tabelle (A323) bis y 39 hinauf; die Fläche blieb unverändert, weil der Treffer im Original
    /// nicht belegt ist.
    /// </summary>
    public static readonly (StadiumFeature Feature, Rect Area)[] Hotspots =
    [
        (StadiumFeature.Anzeigetafel, new Rect(112, 56, 82, 7)),
        (StadiumFeature.Sitzkomfort, new Rect(100, 102, 106, 12)),
        (StadiumFeature.Ueberdachung, new Rect(90, 63, 126, 39)),
        (StadiumFeature.Kino, new Rect(18, 53, 72, 30)),
        (StadiumFeature.Gastronomie, new Rect(4, 110, 90, 44)),
        (StadiumFeature.Bowlingbahn, new Rect(194, 110, 80, 44)),
        (StadiumFeature.Verkehrsanbindung, new Rect(250, 50, 70, 118)),
    ];

    /// <summary>
    /// [ORIGINAL] Teile des Stadionbilds aus kb\8: Das Original zeichnet sie aus einer Tabelle (em.exe Datei 0x3D904–0x3D9EB, DGROUP:
    /// Ziel 0x4F3D, Größe 0x4F5B, Quelle 0x4F79, je Eintrag 2 Byte, Index 6 · Bereich + 2 · (Stufe − 1)). Stufe 0 zeichnet nichts;
    /// Pixelprobe der Bilder file43_2, file43_7, file44_7, file44a_10, file44b_6 (0 abweichende Pixel im Bildbereich, Sitzung 3/7).
    /// Verkehrsanbindung hat nur zwei Stufen (Straße, Autobahn).
    /// </summary>
    public readonly record struct PartEntry(Rect Source, int DestX, int DestY);

    private static PartEntry E(int sx, int sy, int w, int h, int dx, int dy) => new(new Rect(sx, sy, w, h), dx, dy);

    public static readonly Dictionary<StadiumFeature, PartEntry[]> PartTable = new()
    {
        [StadiumFeature.Verkehrsanbindung] = [E(0, 0, 66, 118, 244, 50), E(66, 0, 76, 118, 244, 50)],
        [StadiumFeature.Gastronomie] = [E(211, 109, 50, 30, 25, 112), E(211, 139, 56, 39, 21, 105), E(143, 115, 68, 58, 13, 91)],
        [StadiumFeature.Bowlingbahn] = [E(142, 60, 68, 55, 201, 94)],
        [StadiumFeature.Ueberdachung] = [E(142, 0, 144, 60, 79, 57), E(0, 118, 143, 57, 80, 57)],
        [StadiumFeature.Kino] = [E(144, 173, 52, 21, 29, 44), E(210, 86, 53, 23, 27, 42), E(210, 60, 58, 26, 24, 39)],
    };

    /// <summary>[ORIGINAL] Zeichenfolge der Bereiche (DGROUP 0x4F97 = 5, 6, 8, 0, 7): Kino zuletzt, Restaurant liegt vor der Halle.</summary>
    public static readonly StadiumFeature[] PartOrder =
        [StadiumFeature.Verkehrsanbindung, StadiumFeature.Gastronomie, StadiumFeature.Bowlingbahn, StadiumFeature.Ueberdachung, StadiumFeature.Kino];

    /// <summary>[ORIGINAL] Maskenindex der Teile (Datei 0x3D92D: <c>mov al, 0xC0</c>, letztes Argument der Kopie 0x50:0xA79).</summary>
    public const int PartMask = 0xC0;

    // ---- Farben (Palette von kb\14) --------------------------------------------------------

    private const byte PanelColor = 4;
    private const byte PanelLine = 57;
    private const byte LabelColor = 252;
    private const byte ValueColor = 31;

    /// <summary>[ORIGINAL] Wertfarbe der Tabelle 0x3D45F: 31 − 30 · Modus (Datei 0x3D5FB–0x3D607, 0x3D737–0x3D743), im Bau-Modus 1.</summary>
    private const byte BuildValueColor = 1;
    private const byte LevelColor = 24;
    /// <summary>[ORIGINAL] Kostenzeile Datei 0x3E59A–0x3E5C7: Farbe 8, wenn das Konto reicht, sonst 17.</summary>
    private const byte CostColor = 17;
    private const byte AffordableColor = 8;
    private const byte BoxColor = 240;

    // ---- Zustand ---------------------------------------------------------------------------

    /// <summary>Gewählter oder überfahrener Ausbau: ein Bereich oder Plätze.</summary>
    public readonly record struct Choice(StadiumProject Project, StadiumFeature Feature);

    private readonly Screen _back;
    /// <summary>
    /// Tabelle im Modus 1 („Bau“, Datei 0x3D45F, Argument [bp+0xA]). [ORIGINAL] Umschalten mit der rechten Maustaste bei Maus-y &gt;
    /// 167 (Datei 0x3DB5B–0x3DB7D: <c>cmp [0x5A16], 3 / cmp si, 0xA7</c>, si = Maus-y); die rechte Taste verlässt den Bildschirm sonst
    /// (Datei 0x3E4D4). Aufbau des Bildschirms: Modus 0.
    /// </summary>
    private bool _showBuild;
    private Choice? _hover;
    private Choice? _selected;
    private int _target;
    private StadiumQuote? _quote;
    private readonly Team.MessageBox _message;

    public StadiumScreen(ScreenHost host, Screen back) : base(host)
    {
        _back = back;
        _message = new Team.MessageBox(() => Host.Game?.ActiveManager.Name ?? "");
    }

    public Choice? Selected => _selected;

    /// <summary>Zielstufe (Bereich) bzw. Zahl der Ausbauschritte (Plätze).</summary>
    public int Target => _target;

    public bool AsksConfirmation => _quote is not null;

    /// <summary>Angebot der Baufirma, solange die Frage offen ist.</summary>
    public StadiumQuote? Quote => _quote;

    /// <summary>
    /// Kurzmeldung statt Auswahl („Die Baufirma“ / „ist noch beleidigt...“, <see cref="ShortMessageTexts.BuilderOffended"/>).
    /// [ORIGINAL] Datei 0x3DE9F über 0x0C0B8 (Integration Runde 12, Anschluss ui-meldungen Teil 3, UM-4).
    /// </summary>
    public Team.MessageBox Message => _message;

    private GameState Game => Host.Game!;
    private Core.Manager Me => Game.ActiveManager;
    private Stadium Stadium => Game.Finance(Me).Stadium;

    /// <summary>[ORIGINAL] Knöpfe „Ja.“ / „Nein.“ des Fragefensters Datei 0x4C246 (<see cref="OriginalDialog"/>).</summary>
    public static readonly Rect YesButton = OriginalDialog.LeftButton;
    public static readonly Rect NoButton = OriginalDialog.RightButton;

    /// <summary>
    /// [ORIGINAL] Ausbau-Frage: Fragefenster Datei 0x4C246 (Aufruf Datei 0x3E37A), Titel = Managername, Zeilen „Möchten Sie Ihr Stadion
    /// ausbauen ?“, „Rabatt: r%“, „Bauzeit: ca. w Wochen“, Knöpfe „Ja.“/„Nein.“; Datei 0x3E2EF–0x3E37A; Bilder file43_5, file44_3, file44_5.
    /// Nur die linke Maustaste zählt (Baustein), kein Klick daneben.
    /// </summary>
    private OriginalDialog? _dialog;
    public OriginalDialog? Dialog => _dialog;

    public override void Update(InputState input)
    {
        if (_message.Update(input))
            return; // [ORIGINAL] 0x0C0B8: ohne Knopf und ohne Eingabe, schließt nach 75 Takten von selbst
        if (_quote is { } quote && _dialog is { } dialog)
        {
            // [ORIGINAL] Datei 0x3E389 (Ja) / 0x3E482 (Nein): Nein lässt die Baufirma beleidigt; nur Klicks auf die Knöpfe antworten.
            dialog.Update(input);
            if (dialog.Result is { } answer)
            {
                Game.AnswerConstruction(Me, quote, answer == 1);
                _quote = null;
                _dialog = null;
                _selected = null;
            }
            return;
        }
        if (input.RightClick && input.MouseY > BuildSwitchY)
        {
            _showBuild = !_showBuild;
            return;
        }
        if (input.Pressed(Key.Escape) || input.RightClick)
        {
            Host.Show(_back);
            return;
        }

        _hover = HitTest(input.MouseX, input.MouseY);
        var ticket = _ticketArrows.Update(input);
        var level = LevelArrowsShown ? _levelArrows.Update(input) : 0;
        if (!input.LeftClick)
            return;

        if (ticket != 0) Stadium.TicketPrice = Math.Clamp(Stadium.TicketPrice - ticket, Stadium.MinTicketPrice, Stadium.MaxTicketPrice);
        else if (level != 0) Step(-level);
        else if (_selected is { } sel && input.Clicked(LevelBox))
        {
            _quote = Game.QuoteConstruction(Me, sel.Project, sel.Feature, _target);
            if (_quote is { } q)
                _dialog = new OriginalDialog(OriginalDialogKind.Question, Me.Name, "Möchten Sie Ihr Stadion ausbauen ?",
                    ConstructionDiscountLine(q.Discount), ConstructionWeeksLine(q.Weeks), "Ja.", "Nein.");
        }
        else if (_hover is { } hover)
            Select(hover);
    }

    /// <summary>
    /// Zeile 2 der Ausbau-Frage. [ORIGINAL] Datei 0x3E238–0x3E37A: DGROUP 0x43B7 „Rabatt:“ (ohne Leerzeichen), die Zahl mit
    /// Mindestbreite 2 (Zahlenausgabe 0x0B954, <see cref="EishockeyManager.UI.Screens.Statistics.ClubStatisticsScreen.Number"/>) und DGROUP 0x502F „#“,
    /// in der Schrift das Prozentzeichen (R14UR-03).
    /// </summary>
    public static string ConstructionDiscountLine(int discount) => "Rabatt:" + EishockeyManager.UI.Screens.Statistics.ClubStatisticsScreen.Number(discount, 2, false) + "#";

    /// <summary>Zeile 3 der Ausbau-Frage. [ORIGINAL] Datei 0x3E238–0x3E37A: DGROUP 0x2F31 „Bauzeit: ca. “, Zahl Breite 2, DGROUP 0x2F1F „ Wochen“.</summary>
    public static string ConstructionWeeksLine(int weeks) => "Bauzeit: ca. " + EishockeyManager.UI.Screens.Statistics.ClubStatisticsScreen.Number(weeks, 2, false) + " Wochen";

    private Choice? HitTest(int x, int y)
    {
        if (x < 0)
            return null;
        if (y < ImageHeight)
        {
            foreach (var (feature, area) in Hotspots)
                if (area.Contains(x, y))
                    return new Choice(StadiumProject.Feature, feature);
            return null;
        }
        for (var i = 0; i < Stadium.FeatureCount; i++)
            if (FeatureRow(i).Contains(x, y))
                return new Choice(StadiumProject.Feature, (StadiumFeature)i);
        if (SeatsRow.Contains(x, y))
            return new Choice(StadiumProject.Sitzplaetze, default);
        if (StandingRow.Contains(x, y))
            return new Choice(StadiumProject.Stehplaetze, default);
        return null;
    }

    /// <summary>
    /// Wählt einen Ausbau; Zielstufe = nächste Stufe bzw. ein Ausbauschritt. Beleidigte Baufirma: Kurzmeldung, keine Auswahl;
    /// Projekt im Bau oder am Ende: keine Auswahl. [ORIGINAL] Datei 0x3DE71–0x3DF81.
    /// </summary>
    public void Select(Choice choice)
    {
        _selected = null;
        if (ShortMessageTexts.IsBuilderOffended(Stadium, choice.Project, choice.Feature))
        {
            var text = ShortMessageTexts.BuilderOffended; // [ORIGINAL] Datei 0x3DE9F, danach keine Auswahl
            _message.ShowShort(text.Line1, text.Line2);
            return;
        }
        if (!Stadium.CanSelect(choice.Project, choice.Feature))
            return;
        _selected = choice;
        _target = Stadium.FirstTarget(choice.Project, choice.Feature);
    }

    /// <summary>
    /// ▲▼: höchstens bis zum Höchstziel; ein höheres Ziel nur, wenn das Konto die Kosten deckt.
    /// [ORIGINAL] Datei 0x3E107–0x3E137.
    /// </summary>
    private void Step(int delta)
    {
        var c = _selected!.Value;
        var next = _target + delta;
        if (Stadium.Cost(c.Project, c.Feature, next) is not { } cost)
            return;
        if (delta > 0 && Me.Money < cost)
            return;
        _target = next;
    }

    /// <summary>Kosten der gewählten Stufe vor Rabatt.</summary>
    public long? CurrentCost() => _selected is { } c ? Stadium.Cost(c.Project, c.Feature, _target) : null;

    private static string Label(Choice c) => c.Project switch
    {
        StadiumProject.Sitzplaetze => "Sitzplätze",
        StadiumProject.Stehplaetze => "Stehplätze",
        _ => Stadium.Features[(int)c.Feature].Label,
    };

    private string TargetText(Choice c)
    {
        if (c.Project == StadiumProject.Feature)
            return Stadium.Features[(int)c.Feature].Levels[_target];
        // [ORIGINAL] Kostenzeile Datei 0x3E52B: Platzzahl nach dem Bau.
        return Number(Stadium.PlacesAfter(c.Project, _target));
    }

    private static string Number(long n) => n.ToString("#,0", System.Globalization.CultureInfo.GetCultureInfo("de-DE"));

    // ---- Zeichnen --------------------------------------------------------------------------

    public override void Draw(Surface s)
    {
        var background = Assets.Image(Background);
        s.Palette = background.Palette ?? Assets.Palette;
        s.Blit(background, 0, 0);
        DrawBuildings(s);
        s.Fill(new Rect(0, ImageHeight, Surface.Width, Surface.Height - ImageHeight), PanelColor);
        s.HLine(0, ImageHeight, Surface.Width, PanelLine);
        DrawList(s);
        DrawRight(s);
        if (_dialog is not null)
            DrawConfirm(s);
        _message.Draw(s, Assets);
    }

    private void DrawBuildings(Surface s)
    {
        var st = Stadium;
        foreach (var feature in PartOrder)
        {
            var level = st.Level(feature);
            if (level <= 0)
                continue;
            var e = PartTable[feature][level - 1];
            s.Blit(Assets.Image(Parts), e.Source, e.DestX, e.DestY, transparent: PartMask);
        }
    }

    private void DrawList(Surface s)
    {
        var mini = Assets.Mini;
        for (var i = 0; i < Stadium.FeatureCount; i++)
        {
            var y = FeatureRow(i).Y + 1;
            s.Text(mini, 0, y, Stadium.Features[i].Label, LabelColor);
            s.Text(mini, ListColonX, y, ":", LabelColor);
            s.Text(mini, ListValueX, y, FeatureValue(Stadium, (StadiumFeature)i, _showBuild), _showBuild ? BuildValueColor : ValueColor);
        }
    }

    private void DrawRight(Surface s)
    {
        var mini = Assets.Mini;
        void Row(int y, string label, string value, byte color = ValueColor)
        {
            s.Text(mini, RightLabelX, y, label, LabelColor);
            s.Text(mini, RightColonX, y, ":", LabelColor);
            s.Text(mini, RightValueX, y, value, color);
        }
        var placesColor = _showBuild ? BuildValueColor : ValueColor;
        Row(SeatsRow.Y + 1, "Sitzplätze", PlacesValue(Stadium, StadiumProject.Sitzplaetze, _showBuild), placesColor);
        Row(StandingRow.Y + 1, "Stehplätze", PlacesValue(Stadium, StadiumProject.Stehplaetze, _showBuild), placesColor);
        Arrows(s, _ticketArrows);
        Row(TicketRowY, "Eintritt", $"{Stadium.TicketPrice} DM");

        var shown = _hover ?? _selected;
        if (shown is { } choice)
        {
            // [ORIGINAL] Bereich im Bau: wie sonst „<Bereich> ausbauen“, kein „(Noch n Wochen)“, keine Stufenwahl, keine Kostenzeile
            // (Maus über Gastronomie bei KNEIPEN im Bau, file44_6; Sitzung 3).
            s.Text(mini, RightLabelX, HintY, Label(choice) + " ausbauen", ValueColor);
            if (_selected == choice && CurrentCost() is { } cost)
            {
                Arrows(s, _levelArrows);
                s.Outline(LevelBox, BoxColor);
                s.TextCentered(mini, LevelBox.X + LevelBox.W / 2, LevelBox.Y + 3, TargetText(choice), LevelColor);
                s.Text(mini, RightLabelX + 2, 221, "Kosten: " + GameState.FormatMoney(cost),
                    Me.Money >= cost ? AffordableColor : CostColor);
            }
        }

        s.Text(mini, RightLabelX, 232, "Konto:", LabelColor);
        s.TextRight(mini, AccountRight, 232, GameState.FormatMoney(Me.Money), LabelColor);
    }

    /// <summary>
    /// ▲▼ aus kb\4 über <see cref="UI.ArrowPair"/>: [ORIGINAL] losgelassen Quelle (22 | 1 + 9 · j) 8 × 7 nach (x + 2 | y + 1),
    /// gedrückt (22 | 19 + 9 · j) (Datei 0x4BAD3–0x4BBFE); im Video orange mit dunkler Kante.
    /// [ORIGINAL] Davor je Pfeil die Grundgrafik kb\4 (20 | 9 · j) 10 × 8 nach (x + 10 · j | y) (Datei 0x3DAE0–0x3DB29 Eintritt,
    /// 0x3DFAB–0x3DFF4 Stufe); jeder Pfeil ist damit 10 Pixel breit (#597).
    /// </summary>
    private void Arrows(Surface s, ArrowPair pair)
    {
        var image = Assets.Image(ArrowPair.Image);
        for (var j = 0; j < 2; j++)
            s.Blit(image, new Rect(20, 9 * j, 10, 8), pair.First.X + 10 * j, pair.First.Y);
        pair.DrawReleased(s, image, -1);
        pair.DrawReleased(s, image, +1);
        pair.Draw(s, image);
    }

    /// <summary>Stufenwahl sichtbar (Pfeile 0x3E0BC werden nur dann abgefragt).</summary>
    private bool LevelArrowsShown => _selected is { } c && !(Stadium.IsUnderConstruction(c.Project, c.Feature) && Stadium.WeeksLeft(c.Project, c.Feature) > 0)
        && CurrentCost() is not null;

    private void DrawConfirm(Surface s) => _dialog!.Draw(s, Assets);
}

/// <summary>Anmeldung im Hauptmenü (Manager → Stadion).</summary>
internal static class StadiumMenuEntries
{
    [MenuEntry("Stadion")]
    private static Screen Open(ScreenHost host, Screen back) => new StadiumScreen(host, back);
}
