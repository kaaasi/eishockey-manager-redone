using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens.ManagerArea;

/// <summary>
/// Bildschirm Werbung (kb\151 mit eigener Palette, Logos aus kb\11). Vermessen am UI-Video (6:20–7:40):
/// <list type="bullet">
/// <item>Oben sieben Bandenfelder, links der Spieler (Trikot), Mitte die Zuschauer mit zwei Tafeln (Eisfläche, je
///   eine eigene Fläche: oben nur mit Überdachung HALLE, unten nur mit Kühlanlage 'MEGAFROST', em.exe 0x2B56A),
///   links unten der Fernseher. Klick wählt die Fläche (roter Rahmen).</item>
/// <item>Klick auf einen abgelaufenen Vertrag mit Option startet die Verhandlung (0x2B75F): Frage
///   „Möchten Sie den Vertrag mit … verlängern ?“ oder Absage „Die Firma … wird den Vertrag nicht verlängern.“</item>
/// <item>Unten: Logo mit ▲▼ zum Blättern durch die Firmen bzw. Sender, Angebot (Dauer, Banden, Betrag pro Monat),
///   Knöpfe „Info“ (Firmen-Info) und „Annehmen“, darunter Einnahmen, Ausgaben und Saldo pro Monat.</item>
/// </list>
/// </summary>
public sealed class AdvertisingScreen : Screen
{
    public const int Background = 151;
    public const int Logos = 11;

    // ---- Vermessung ------------------------------------------------------------------------

    /// <summary>Bandenfeld 0–6 (roter Rahmen), Logo bei (+1, +1).</summary>
    public static Rect BoardRect(int i) => new(39 + 35 * i, 34, 35, 25);

    public static readonly Rect JerseyPanel = new(2, 85, 79, 74);
    public static readonly Rect IcePanel = new(86, 85, 230, 74);
    public static readonly Rect TvPanel = new(2, 163, 79, 75);

    private static readonly (int X, int Y)[] IceBoards = [(268, 92), (268, 128)];

    /// <summary>Eistafel 0/1 als Fläche 9/10. [ORIGINAL] 0x2B5DB: Rahmen bei x = 0x10B, y = 0x5B + 0x24 · i, 35 × 25.</summary>
    public static Rect IceBoardRect(int i) => new(267, 91 + 36 * i, 35, 25);

    /// <summary>Ja/Nein der Verlängerungsfrage. [APPROX] Lage nicht aus dem Video (Frage 0x120:0x7A).</summary>
    public static readonly Rect YesButton = new(110, 136, Widgets.ButtonWidth, Widgets.ButtonHeight);
    public static readonly Rect NoButton = new(170, 136, Widgets.ButtonWidth, Widgets.ButtonHeight);
    private static readonly (int X, int Y) JerseyLogo = (31, 116);
    private static readonly (int X, int Y) TvLogo = (11, 173);

    /// <summary>[ORIGINAL] 0x2B268: Umrandung (99|178)–(135|204), Farbe 24 bzw. 7 beim Blättern.</summary>
    public static readonly Rect LogoBox = new(99, 178, 37, 27);

    /// <summary>[ORIGINAL] 0x2A9AD: Logo bzw. schwarze leere Fläche (0x30:0x800) innerhalb der Umrandung, 35 × 25.</summary>
    public static readonly Rect LogoArea = new(100, 179, 35, 25);

    /// <summary>[APPROX] Trefferflächen der Pfeile; das Original prüft sie in 0x120:0x3E (138|183) bzw. (138|192).</summary>
    public static readonly Rect ArrowUp = new(137, 181, 13, 11);
    public static readonly Rect ArrowDown = new(137, 192, 13, 12);

    /// <summary>[ORIGINAL] 0x2B2CE: Pfeilpaar kb\4 (20|0) 10 × 17 bei (138|183), Rahmen (137|182)–(148|200) Farbe 24 (0x2B300).</summary>
    public static readonly Rect ArrowFrame = new(137, 182, 12, 19);
    public static readonly Rect InfoButton = new(257, 170, Widgets.ButtonWidth, Widgets.ButtonHeight);
    public static readonly Rect AcceptButton = new(257, 186, Widgets.ButtonWidth, Widgets.ButtonHeight);

    /// <summary>[ORIGINAL] 0x2A9AD: Bezeichnung bei x = 0x9C, Wert („: …“) bei 0xBF, Hinweis bei 0x113.</summary>
    public const int LabelX = 156;
    public const int ValueX = 191;
    public const int NoteX = 275;

    /// <summary>[ORIGINAL] 0x2A9AD: Grundlinien Name 0xB3, Dauer 0xBD, Banden 0xC5, Angebot/Prämie 0xCD.</summary>
    public const int NameLine = 179, DurationLine = 189, BoardsLine = 197, AmountLine = 205;

    /// <summary>Firmen-Info: schwarzes Fenster über der Mitte.</summary>
    public static readonly Rect InfoWindow = new(30, 91, 260, 58);

    // ---- Farben (Palette von kb\151) -------------------------------------------------------

    private const byte TextColor = 144;

    /// <summary>[ORIGINAL] 0x30:0x885 in 0x2A9AD mit Farbe 0, ohne Schatten (0xFF).</summary>
    private const byte PanelTextColor = 0;

    /// <summary>[ORIGINAL] Auswahlrahmen 0x2B63F (Farbe 0x12).</summary>
    private const byte FrameRed = 18;

    /// <summary>[ORIGINAL] Umrandungen von Logo- und Pfeilkasten 0x2B268/0x2B300 (Farbe 0x18).</summary>
    private const byte BoxBorder = 24;

    /// <summary>[ORIGINAL] Umrandung des Logokastens beim Blättern durch die Angebote (0x2B719, Farbe 7).</summary>
    private const byte BrowseBorder = 7;

    /// <summary>[ORIGINAL] Trennlinie 0x2B28B: 0x30:0x995 (90|213)–(310|213), Farbe 0x1C.</summary>
    private const byte LineColor = 28;
    private const byte InfoTitle = 254;
    private const byte InfoText = 208;
    private const byte ButtonText = 32;

    // ---- Zustand ---------------------------------------------------------------------------

    private readonly Screen _back;
    private AdSlot? _slot;
    private int _companyCursor = -1;
    private int _stationCursor = -1;
    private int? _infoFor;
    private Rect? _pressed;
    private SponsorContract? _renewal;
    private readonly EishockeyManager.UI.Screens.Team.MessageBox _message = new();

    public AdvertisingScreen(ScreenHost host, Screen back) : base(host) => _back = back;

    public AdSlot? SelectedSlot => _slot;

    /// <summary>Gezeigter Partner (−1 = keiner).</summary>
    public int Cursor => _slot?.Kind == AdSlotKind.Fernsehen ? _stationCursor : _companyCursor;

    public bool ShowsInfo => _infoFor is not null;

    /// <summary>Vertrag, zu dem die Verlängerungsfrage offen ist.</summary>
    public SponsorContract? RenewalQuestion => _renewal;

    /// <summary>Offene Meldung (Absage), sonst leer.</summary>
    public IReadOnlyList<string> MessageLines => _message.Lines;

    private GameState Game => Host.Game!;
    private Core.Manager Me => Game.ActiveManager;
    private Sponsoring Ads => Game.Ads(Me);

    public override void Update(InputState input)
    {
        _pressed = null;
        if (_message.Update(input))
            return;
        if (_renewal is { } pending)
        {
            if (!input.LeftClick)
                return;
            if (YesButton.Contains(input.MouseX, input.MouseY) || NoButton.Contains(input.MouseX, input.MouseY))
            {
                Game.AnswerRenewal(Me, pending, yes: YesButton.Contains(input.MouseX, input.MouseY));
                _renewal = null;
            }
            return;
        }
        if (_infoFor is not null)
        {
            if (input.LeftClick || input.RightClick || input.Keys.Count > 0)
                _infoFor = null;
            return;
        }
        if (input.Pressed(Key.Escape) || input.RightClick)
        {
            Host.Show(_back);
            return;
        }
        if (!input.LeftClick)
            return;

        var (x, y) = (input.MouseX, input.MouseY);
        AdSlot? clicked = null;
        for (var i = 0; i < Sponsoring.BoardCount; i++)
            if (BoardRect(i).Contains(x, y))
                clicked = new AdSlot(AdSlotKind.Bande, i);
        if (JerseyPanel.Contains(x, y))
            clicked = new AdSlot(AdSlotKind.Trikot);
        else if (IcePanel.Contains(x, y))
            // [APPROX] Trefferfläche: obere Hälfte der Mitte = Tafel 1, untere = Tafel 2.
            clicked = new AdSlot(y < IcePanel.Y + IcePanel.H / 2 ? AdSlotKind.Eisflaeche : AdSlotKind.Eisflaeche2);
        else if (TvPanel.Contains(x, y))
            clicked = new AdSlot(AdSlotKind.Fernsehen);
        if (clicked is { } chosen)
        {
            // [ORIGINAL] 0x2B633: Klick auf eine Fläche zeigt deren Vertrag (Modus 0), nicht das zuletzt geblätterte Angebot.
            _slot = chosen;
            _companyCursor = _stationCursor = -1;
            if (Ads.ContractAt(chosen) is { } contract && Game.AwaitsRenewal(contract))
            {
                Negotiate(contract);
                return;
            }
        }

        if (ArrowUp.Contains(x, y))
            Browse(-1, ArrowUp);
        else if (ArrowDown.Contains(x, y))
            Browse(+1, ArrowDown);
        else if (InfoButton.Contains(x, y) && CanShowInfo())
        {
            _pressed = InfoButton;
            _infoFor = Cursor;
        }
        else if (AcceptButton.Contains(x, y) && CurrentOffer() is not null)
        {
            _pressed = AcceptButton;
            Game.AcceptOffer(Me, _slot!.Value, Cursor);
        }
    }

    /// <summary>Verhandlung über einen abgelaufenen Vertrag mit Option (Texte em.exe DGROUP 0x3D3D–0x3D81).</summary>
    private void Negotiate(SponsorContract contract)
    {
        var name = contract.Partner.Name;
        switch (Game.OpenRenewal(Me, contract))
        {
            case RenewalOutcome.Question:
                _renewal = contract;
                break;
            case RenewalOutcome.Declined:
                // [APPROX] Zeilenaufteilung: die vier Texte der Meldung 0x120:0x4D je eine Zeile.
                _message.Show("Die Firma", name, "wird den Vertrag", "nicht verlängern.");
                break;
        }
    }

    private void Browse(int delta, Rect button)
    {
        if (_slot is not { } slot)
            return;
        _pressed = button;
        var count = Sponsor.For(slot.Kind).Length;
        var next = Cursor < 0 ? (delta > 0 ? 0 : count - 1) : (Cursor + delta + count) % count;
        if (slot.Kind == AdSlotKind.Fernsehen)
            _stationCursor = next;
        else
            _companyCursor = next;
    }

    private bool CanShowInfo() => _slot is { } slot && slot.Kind != AdSlotKind.Fernsehen && Cursor >= 0;

    /// <summary>Angebot des gezeigten Partners für die gewählte (freie) Fläche.</summary>
    public SponsorOffer? CurrentOffer()
    {
        if (_slot is not { } slot || Cursor < 0 || Ads.ContractAt(slot) is not null || !Game.IsAdSlotAvailable(Me, slot))
            return null;
        var offer = Ads.OfferFrom(slot.Kind, Cursor);
        if (offer is null || (slot.Kind == AdSlotKind.Bande && Ads.FreeBoards().Count() < offer.Boards))
            return null;
        return offer;
    }

    // ---- Zeichnen --------------------------------------------------------------------------

    public override void Draw(Surface s)
    {
        var background = Assets.Image(Background);
        s.Palette = background.Palette ?? Assets.Palette;
        s.Blit(background, 0, 0);
        DrawContracts(s);
        DrawSelection(s);
        DrawOfferPanel(s);
        DrawTotals(s);
        if (_infoFor is { } company)
            DrawInfo(s, Sponsor.Companies[company]);
        if (_renewal is { } pending)
            DrawRenewalQuestion(s, pending);
        _message.Draw(s, Assets);
    }

    /// <summary>[APPROX] Frage im Rahmen der Meldungen (0x120:0x7A), Lage und Knöpfe nicht aus dem Video.</summary>
    private void DrawRenewalQuestion(Surface s, SponsorContract contract)
    {
        var mini = Assets.Mini;
        var frame = new Rect(70, 92, 180, 66);
        Widgets.Frame(s, Assets, frame, FrameStyle.Orange);
        string[] lines = ["Möchten Sie den Vertrag mit", contract.Partner.Name, "verlängern ?"];
        for (var i = 0; i < lines.Length; i++)
            s.TextCentered(mini, 160, frame.Y + 10 + 8 * i, lines[i], Colors.White);
        Button(s, YesButton, "Ja");
        Button(s, NoButton, "Nein");
    }

    /// <summary>Kachel in kb\11: Spalte 0 Firmen, Spalte 1 Sender (oben) und Trikots 8–9, Spalte 2 Trikots 0–7.</summary>
    public static Rect LogoTile(AdSlotKind kind, int sponsor) => kind switch
    {
        AdSlotKind.Fernsehen => new Rect(36, 1 + 25 * sponsor, 33, 23),
        AdSlotKind.Trikot when sponsor >= 8 => new Rect(36, 1 + 25 * (sponsor - 3), 30, 23),
        AdSlotKind.Trikot => new Rect(71, 1 + 25 * sponsor, 30, 23),
        _ => new Rect(1, 1 + 25 * sponsor, 33, 23),
    };

    private void DrawLogo(Surface s, AdSlotKind kind, int sponsor, int x, int y) =>
        s.Blit(Assets.Image(Logos), LogoTile(kind, sponsor), x, y);

    private void DrawContracts(Surface s)
    {
        foreach (var contract in Ads.Contracts)
        {
            switch (contract.Kind)
            {
                case AdSlotKind.Bande:
                    foreach (var b in contract.Boards)
                        DrawLogo(s, AdSlotKind.Bande, contract.Sponsor, BoardRect(b).X + 1, BoardRect(b).Y + 1);
                    break;
                case AdSlotKind.Eisflaeche or AdSlotKind.Eisflaeche2:
                    // [APPROX] Im Video ist die Eisfläche nicht belegt; Logo der Firma auf der eigenen Tafel angenommen.
                    var (x, y) = IceBoards[contract.Kind == AdSlotKind.Eisflaeche ? 0 : 1];
                    DrawLogo(s, AdSlotKind.Bande, contract.Sponsor, x, y);
                    break;
                case AdSlotKind.Trikot:
                    DrawLogo(s, AdSlotKind.Trikot, contract.Sponsor, JerseyLogo.X, JerseyLogo.Y);
                    break;
                case AdSlotKind.Fernsehen:
                    DrawLogo(s, AdSlotKind.Fernsehen, contract.Sponsor, TvLogo.X, TvLogo.Y);
                    break;
            }
        }
    }

    private void DrawSelection(Surface s)
    {
        if (_slot is not { } slot)
            return;
        var frame = slot.Kind switch
        {
            AdSlotKind.Bande => BoardRect(slot.Index),
            AdSlotKind.Trikot => JerseyPanel,
            AdSlotKind.Eisflaeche => IceBoardRect(0),
            AdSlotKind.Eisflaeche2 => IceBoardRect(1),
            _ => TvPanel,
        };
        s.Outline(frame, FrameRed);
    }

    /// <summary>Text im Angebots-/Vertragsfeld: x links, Grundlinie wie im Aufruf 0x30:0x885.</summary>
    public readonly record struct PanelText(int X, int Baseline, string Text);

    /// <summary>
    /// Inhalt des Feldes unten nach em.exe 0x2A9AD. Modus 0 (Klick auf eine Fläche, 0x2B633): Vertrag der Fläche, sonst
    /// „Diese Werbefläche ist noch unbefleckt.“ Modus 1 (Blättern, 0x2BE95): Angebotssatz des Partners – auch wenn die
    /// Fläche belegt ist oder Banden fehlen (dann nur ohne „Annehmen“, 0x2BF86) –, sonst „Es liegt momentan kein
    /// Angebot vor...“. Gesperrte Eistafeln (0x2B589) bleiben leer.
    /// </summary>
    public IEnumerable<PanelText> PanelTexts()
    {
        if (_slot is not { } slot || !Game.IsAdSlotAvailable(Me, slot))
            yield break;
        var offerMode = Cursor >= 0;
        string name;
        int months, option, boards;
        long amount;
        if (offerMode)
        {
            name = Sponsor.For(slot.Kind)[Cursor].Name;
            if (Ads.OfferFrom(slot.Kind, Cursor) is not { } offer)
            {
                yield return new(LabelX, NameLine, name);
                yield return new(LabelX, DurationLine, "Es liegt momentan kein Angebot vor...");
                yield break;
            }
            (months, option, boards, amount) = (offer.Months, offer.OptionMonths, offer.Boards, offer.MonthlyAmount);
        }
        else
        {
            if (Ads.ContractAt(slot) is not { } contract)
            {
                yield return new(LabelX, DurationLine, "Diese Werbefläche ist noch unbefleckt.");
                yield break;
            }
            name = contract.Partner.Name;
            // [ORIGINAL] 0x2AD15: im Vertrag die Zahl der Banden desselben Partners.
            (months, option, boards, amount) = (contract.MonthsLeft, contract.OptionMonths, contract.Boards.Length, contract.MonthlyAmount);
        }

        yield return new(LabelX, NameLine, name);
        yield return new(LabelX, DurationLine, "Dauer");
        // [ORIGINAL] 0x2AB6F–0x2AC27: „: n“, mit Option „ - n+Option“, „ Monat“ und „e“ bei n > 1 oder Option.
        var duration = $": {months}" + (option > 0 ? $" - {months + option}" : "") + " Monat" + (months > 1 || option > 0 ? "e" : "");
        yield return new(ValueX, DurationLine, duration);
        // [ORIGINAL] 0x2AC51: im Vertrag „! Option !“, wenn die Laufzeit abgelaufen ist und eine Option besteht.
        // „Kündbar.“ (Bit 0x80 der Option, gesetzt beim Aufstieg 0x2713F) kennt das Remake noch nicht (Restlücke).
        if (!offerMode && option > 0 && months == 0)
            yield return new(NoteX, DurationLine, "! Option !");
        if (slot.Kind == AdSlotKind.Bande)
        {
            yield return new(LabelX, BoardsLine, "Banden");
            yield return new(ValueX, BoardsLine, $": {boards}");
        }
        yield return new(LabelX, AmountLine, offerMode ? "Angebot" : "Prämie");
        var money = GameState.FormatMoney(amount);
        if (money.EndsWith(" DM", StringComparison.Ordinal))
            money = money[..^3];
        yield return new(ValueX, AmountLine, $": {money} DM  pro Monat");
    }

    /// <summary>Logo im Feld: Modus 1 immer der geblätterte Partner, Modus 0 der Vertragspartner (0x2AA03).</summary>
    private (AdSlotKind Kind, int Sponsor)? PanelLogo()
    {
        if (_slot is not { } slot || !Game.IsAdSlotAvailable(Me, slot))
            return null;
        if (Cursor >= 0)
            return (slot.Kind, Cursor);
        return Ads.ContractAt(slot) is { } contract ? (contract.Kind, contract.Sponsor) : null;
    }

    private void DrawOfferPanel(Surface s)
    {
        // [ORIGINAL] 0x2B6AD setzt die Umrandung nach jedem Klick auf Farbe 0x18 zurück, 0x2B719 färbt sie beim Blättern
        // durch die Angebote (Zustand [0x5A16] = 1) weiß (7). Video 6:40/6:44 hell, 6:20/6:24 gelb.
        s.Outline(LogoBox, Cursor >= 0 && _slot is { } shown && Game.IsAdSlotAvailable(Me, shown) ? BrowseBorder : BoxBorder);
        if (PanelLogo() is { } logo)
            // [ORIGINAL] 0x2AA6B: 35 × 25 aus kb\11, Spalte 0 (Firmen) bzw. 35 (Sender), Zeile 25 · Logo, nach (100|179).
            s.Blit(Assets.Image(Logos), new Rect(logo.Kind == AdSlotKind.Fernsehen ? 35 : 0, 25 * logo.Sponsor, 35, 25), LogoArea.X, LogoArea.Y);
        else if (_slot is { } slot && Game.IsAdSlotAvailable(Me, slot) && Cursor < 0)
            s.Fill(LogoArea, Colors.Black); // [ORIGINAL] 0x2AB1F: leere Fläche
        DrawArrows(s);

        foreach (var text in PanelTexts())
            Competitions.CompetitionWidgets.Centered(s, Assets.Mini, text.X, text.X, text.Baseline, text.Text, PanelTextColor, null, left: true);

        if (CurrentOffer() is not null)
            Button(s, AcceptButton, "Annehmen");
        if (CanShowInfo())
            Button(s, InfoButton, "Info");
    }

    /// <summary>Knopf aus kb\2 mit heller Schrift wie im Video.</summary>
    private void Button(Surface s, Rect r, string text)
    {
        var pressed = _pressed == r;
        s.Blit(Assets.Image(2), new Rect(17, pressed ? 191 : 179, Widgets.ButtonWidth, Widgets.ButtonHeight), r.X, r.Y);
        s.TextCentered(Assets.Mini, r.X + r.W / 2 + (pressed ? 1 : 0), r.Y + 4 + (pressed ? 1 : 0), text, ButtonText);
    }

    /// <summary>
    /// Pfeilpaar aus kb\4 mit Rahmen (0x2B2CE/0x2B300). [APPROX] Eine gedrückte Darstellung (Rückmeldung in 0x120:0x3E)
    /// ist nicht nachgebaut.
    /// </summary>
    private void DrawArrows(Surface s)
    {
        s.Blit(Assets.Image(4), new Rect(20, 0, 10, 17), ArrowFrame.X + 1, ArrowFrame.Y + 1);
        s.Outline(ArrowFrame, BoxBorder);
    }

    private void DrawTotals(Surface s)
    {
        var mini = Assets.Mini;
        s.HLine(90, 213, 221, LineColor);
        var income = Game.MonthlyIncome(Me);
        var expenses = Game.MonthlyExpenses(Me);
        s.Text(mini, 95, 219, "Einnahmen:", TextColor);
        s.TextRight(mini, 197, 219, GameState.FormatMoney(income), TextColor);
        s.Text(mini, 95, 227, "Ausgaben :", TextColor);
        s.TextRight(mini, 197, 227, GameState.FormatMoney(expenses), TextColor);
        s.Text(mini, 219, 223, "Saldo  :", TextColor);
        s.TextRight(mini, 297, 223, GameState.FormatMoney(income - expenses), TextColor);
    }

    private void DrawInfo(Surface s, Sponsor company)
    {
        var mini = Assets.Mini;
        s.Fill(InfoWindow, Colors.Black);
        s.TextCentered(mini, 160, InfoWindow.Y + 5, "Firmen-Info: " + company.InfoTitle, InfoTitle);
        for (var i = 0; i < company.Info.Length; i++)
            s.TextCentered(mini, 160, InfoWindow.Y + 18 + 8 * i, company.Info[i], InfoText);
    }
}

/// <summary>Anmeldung im Hauptmenü (Manager → Werbung).</summary>
internal static class AdvertisingMenuEntries
{
    [MenuEntry("Werbung")]
    private static Screen Open(ScreenHost host, Screen back) => new AdvertisingScreen(host, back);
}
