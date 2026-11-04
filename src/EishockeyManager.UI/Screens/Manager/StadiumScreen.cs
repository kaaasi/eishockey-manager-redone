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

    /// <summary>Teile aus kb\8 (Quelle, Ziel). Lage per Pixelvergleich mit kb\14 bestimmt bzw. [APPROX] auf den freien Flächen.</summary>
    private static readonly Rect Strasse = new(0, 0, 68, 118);
    private static readonly Rect Autobahn = new(68, 0, 75, 118);
    private static readonly Rect Halle = new(0, 117, 143, 58);
    private static readonly Rect Halboffen = new(143, 0, 143, 59);
    private static readonly Rect Bowling = new(143, 59, 64, 57);
    private static readonly Rect Restaurant = new(143, 115, 67, 56);
    private static readonly Rect KinoKlein = new(143, 171, 55, 23);
    private static readonly Rect KinoMittel = new(209, 87, 54, 23);
    private static readonly Rect KinoGross = new(209, 59, 61, 28);
    private static readonly Rect Imbiss = new(211, 110, 50, 28);
    private static readonly Rect Kneipe = new(211, 138, 56, 40);

    // ---- Farben (Palette von kb\14) --------------------------------------------------------

    private const byte PanelColor = 4;
    private const byte PanelLine = 57;
    private const byte LabelColor = 252;
    private const byte ValueColor = 31;
    private const byte LevelColor = 24;
    /// <summary>[ORIGINAL] Kostenzeile Datei 0x3E59A–0x3E5C7: Farbe 8, wenn das Konto reicht, sonst 17.</summary>
    private const byte CostColor = 17;
    private const byte AffordableColor = 8;
    private const byte BoxColor = 240;
    private const byte DialogText = 252;

    // ---- Zustand ---------------------------------------------------------------------------

    /// <summary>Gewählter oder überfahrener Ausbau: ein Bereich oder Plätze.</summary>
    public readonly record struct Choice(StadiumProject Project, StadiumFeature Feature);

    private readonly Screen _back;
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

    public static readonly Rect YesButton = new(70, 128, 80, 9);
    public static readonly Rect NoButton = new(170, 128, 80, 9);
    private static readonly Rect Dialog = new(50, 84, 220, 60);

    public override void Update(InputState input)
    {
        if (_message.Update(input))
            return; // [ORIGINAL] 0x0C0B8: ohne Knopf und ohne Eingabe, schließt nach 75 Takten von selbst
        if (_quote is { } quote)
        {
            // [ORIGINAL] Datei 0x3E389 (Ja) / 0x3E482 (Nein): Nein lässt die Baufirma beleidigt.
            // [APPROX] Jeder andere Klick oder jede Taste gilt als „Nein“ (Dialog des Remakes).
            var yes = input.Clicked(YesButton) || input.Pressed(Key.Enter);
            if (yes || input.LeftClick || input.RightClick || input.Keys.Count > 0)
            {
                Game.AnswerConstruction(Me, quote, yes);
                _quote = null;
                _selected = null;
            }
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
            _quote = Game.QuoteConstruction(Me, sel.Project, sel.Feature, _target);
        else if (_hover is { } hover)
            Select(hover);
    }

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
        if (_quote is not null)
            DrawConfirm(s);
        _message.Draw(s, Assets);
    }

    private void Part(Surface s, Rect source, int x, int y) => s.Blit(Assets.Image(Parts), source, x, y, transparent: 0);

    private void DrawBuildings(Surface s)
    {
        var st = Stadium;
        switch (st.Level(StadiumFeature.Verkehrsanbindung))
        {
            case 1: Part(s, Strasse, 244, 50); break;
            case 2: Part(s, Autobahn, 246, 50); break;
        }
        switch (st.Level(StadiumFeature.Ueberdachung))
        {
            case 1: Part(s, Halboffen, 81, 54); break;
            case 2: Part(s, Halle, 80, 56); break;
        }
        // [APPROX] Imbiß und Kneipe auf der Fläche des Restaurants, Kinos auf der Fläche oben links.
        switch (st.Level(StadiumFeature.Gastronomie))
        {
            case 1: Part(s, Imbiss, 23, 118); break;
            case 2: Part(s, Kneipe, 20, 108); break;
            case 3: Part(s, Restaurant, 14, 92); break;
        }
        switch (st.Level(StadiumFeature.Kino))
        {
            case 1: Part(s, KinoKlein, 27, 57); break;
            case 2: Part(s, KinoMittel, 27, 57); break;
            case 3: Part(s, KinoGross, 24, 54); break;
        }
        if (st.Level(StadiumFeature.Bowlingbahn) > 0)
            Part(s, Bowling, 202, 93);
    }

    private void DrawList(Surface s)
    {
        var mini = Assets.Mini;
        for (var i = 0; i < Stadium.FeatureCount; i++)
        {
            var y = FeatureRow(i).Y + 1;
            s.Text(mini, 0, y, Stadium.Features[i].Label, LabelColor);
            s.Text(mini, ListColonX, y, ":", LabelColor);
            s.Text(mini, ListValueX, y, Stadium.LevelText((StadiumFeature)i), ValueColor);
        }
    }

    private void DrawRight(Surface s)
    {
        var mini = Assets.Mini;
        void Row(int y, string label, string value)
        {
            s.Text(mini, RightLabelX, y, label, LabelColor);
            s.Text(mini, RightColonX, y, ":", LabelColor);
            s.Text(mini, RightValueX, y, value, ValueColor);
        }
        Row(SeatsRow.Y + 1, "Sitzplätze", Number(Stadium.Seats));
        Row(StandingRow.Y + 1, "Stehplätze", Number(Stadium.Standing));
        Arrows(s, _ticketArrows);
        Row(TicketRowY, "Eintritt", $"{Stadium.TicketPrice} DM");

        var shown = _hover ?? _selected;
        if (shown is { } choice)
        {
            var weeks = Stadium.WeeksLeft(choice.Project, choice.Feature);
            if (Stadium.IsUnderConstruction(choice.Project, choice.Feature) && weeks > 0)
            {
                // [APPROX] Laufende Baustelle statt der Stufenwahl; Text wie Tabelle „Bau“ Datei 0x3D5CA–0x3D5E5.
                s.Text(mini, RightLabelX, 200, Label(choice), ValueColor);
                s.Text(mini, RightLabelX + 2, 221, weeks == 1 ? "(Noch 1 Woche)" : $"(Noch {weeks} Wochen)", CostColor);
            }
            else
            {
                s.Text(mini, RightLabelX, 200, Label(choice) + " ausbauen", ValueColor);
                if (_selected == choice && CurrentCost() is { } cost)
                {
                    Arrows(s, _levelArrows);
                    s.Outline(LevelBox, BoxColor);
                    s.TextCentered(mini, LevelBox.X + LevelBox.W / 2, LevelBox.Y + 3, TargetText(choice), LevelColor);
                    s.Text(mini, RightLabelX + 2, 221, "Kosten: " + GameState.FormatMoney(cost),
                        Me.Money >= cost ? AffordableColor : CostColor);
                }
            }
        }

        s.Text(mini, RightLabelX, 232, "Konto:", LabelColor);
        s.TextRight(mini, AccountRight, 232, GameState.FormatMoney(Me.Money), LabelColor);
    }

    /// <summary>
    /// ▲▼ aus kb\4 über <see cref="UI.ArrowPair"/>: [ORIGINAL] losgelassen Quelle (22 | 1 + 9 · j) 8 × 7 nach (x + 2 | y + 1),
    /// gedrückt (22 | 19 + 9 · j) (Datei 0x4BAD3–0x4BBFE); im Video orange mit dunkler Kante.
    /// </summary>
    private void Arrows(Surface s, ArrowPair pair)
    {
        var image = Assets.Image(ArrowPair.Image);
        pair.DrawReleased(s, image, -1);
        pair.DrawReleased(s, image, +1);
        pair.Draw(s, image);
    }

    /// <summary>Stufenwahl sichtbar (Pfeile 0x3E0BC werden nur dann abgefragt).</summary>
    private bool LevelArrowsShown => _selected is { } c && !(Stadium.IsUnderConstruction(c.Project, c.Feature) && Stadium.WeeksLeft(c.Project, c.Feature) > 0)
        && CurrentCost() is not null;

    private void DrawConfirm(Surface s)
    {
        var mini = Assets.Mini;
        var choice = _selected!.Value;
        var quote = _quote!;
        s.Fill(Dialog, Colors.Black);
        s.Outline(Dialog, BoxColor);
        s.TextCentered(mini, 160, Dialog.Y + 6, "Möchten Sie Ihr Stadion ausbauen ?", DialogText);
        s.TextCentered(mini, 160, Dialog.Y + 16, $"{Label(choice)}: {TargetText(choice)}", LevelColor);
        // [ORIGINAL] Texte „Bauzeit: ca. w Wochen“ (DGROUP 0xDE0) und „Rabatt: r%“ (Datei 0x3E1FE ff.).
        s.TextCentered(mini, 160, Dialog.Y + 26, $"Bauzeit: ca. {quote.Weeks} Wochen", DialogText);
        s.TextCentered(mini, 160, Dialog.Y + 34, $"Kosten: {GameState.FormatMoney(quote.Cost)}  Rabatt: {quote.Discount}%", CostColor);
        s.TextCentered(mini, YesButton.X + YesButton.W / 2, YesButton.Y + 2, "Aber klar", ValueColor);
        s.TextCentered(mini, NoButton.X + NoButton.W / 2, NoButton.Y + 2, "Kein Gedanke", ValueColor);
    }
}

/// <summary>Anmeldung im Hauptmenü (Manager → Stadion).</summary>
internal static class StadiumMenuEntries
{
    [MenuEntry("Stadion")]
    private static Screen Open(ScreenHost host, Screen back) => new StadiumScreen(host, back);
}
