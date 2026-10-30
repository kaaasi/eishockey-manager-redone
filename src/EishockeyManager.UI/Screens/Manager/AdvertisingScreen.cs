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

    public static readonly Rect LogoBox = new(99, 178, 37, 27);
    public static readonly Rect ArrowUp = new(137, 181, 13, 11);
    public static readonly Rect ArrowDown = new(137, 192, 13, 12);
    public static readonly Rect InfoButton = new(257, 170, Widgets.ButtonWidth, Widgets.ButtonHeight);
    public static readonly Rect AcceptButton = new(257, 186, Widgets.ButtonWidth, Widgets.ButtonHeight);

    private const int TextX = 155;
    private const int ColonX = 190;
    private const int ValueX = 194;
    private static readonly int[] LineY = [175, 185, 193, 201];

    /// <summary>Firmen-Info: schwarzes Fenster über der Mitte.</summary>
    public static readonly Rect InfoWindow = new(30, 91, 260, 58);

    // ---- Farben (Palette von kb\151) -------------------------------------------------------

    private const byte TextColor = 144;
    private const byte FrameRed = 110;
    private const byte EmptyBoxBorder = 213;
    private const byte LogoBoxBorder = 7;
    private const byte InfoTitle = 254;
    private const byte InfoText = 208;
    private const byte ButtonText = 32;
    private const byte ArrowFill = 200;
    private const byte ArrowColor = 206;

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
            _slot = chosen;
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

    private void DrawOfferPanel(Surface s)
    {
        var flow = Assets.Mini;
        var cursor = Cursor;
        var kind = _slot?.Kind ?? AdSlotKind.Bande;

        if (_slot is not null && cursor >= 0)
        {
            s.Fill(LogoBox, LogoBoxBorder);
            DrawLogo(s, kind == AdSlotKind.Fernsehen ? AdSlotKind.Fernsehen : AdSlotKind.Bande, cursor, LogoBox.X + 2, LogoBox.Y + 2);
        }
        else
        {
            s.Fill(LogoBox, Colors.Black);
            s.Outline(LogoBox, EmptyBoxBorder);
        }
        DrawArrows(s);

        if (_slot is not { } slot)
            return;

        var contract = Ads.ContractAt(slot);
        if (cursor < 0)
        {
            if (contract is null)
                s.Text(flow, TextX, LineY[1], "Diese Werbefläche ist noch unbefleckt.", TextColor);
            else
                DrawTerms(s, contract.Partner.Name, contract.MonthsLeft, contract.Kind == AdSlotKind.Bande ? contract.Boards.Length : 0,
                    contract.MonthlyAmount);
            return;
        }

        var partner = Sponsor.For(slot.Kind)[cursor];
        var offer = CurrentOffer();
        if (offer is null)
        {
            s.Text(flow, TextX, LineY[0], partner.Name, TextColor);
            s.Text(flow, TextX, LineY[1], "Es liegt momentan kein Angebot vor...", TextColor);
        }
        else
        {
            DrawTerms(s, partner.Name, offer.Months, slot.Kind == AdSlotKind.Bande ? offer.Boards : 0, offer.MonthlyAmount);
            Button(s, AcceptButton, "Annehmen");
        }
        if (CanShowInfo())
            Button(s, InfoButton, "Info");
    }

    private void DrawTerms(Surface s, string name, int months, int boards, long amount)
    {
        var flow = Assets.Mini;
        s.Text(flow, TextX, LineY[0], name, TextColor);
        Term(s, LineY[1], "Dauer", $"{months} Monat{(months == 1 ? "" : "e")}");
        if (boards > 0)
            Term(s, LineY[2], "Banden", boards.ToString());
        Term(s, LineY[3], "Angebot", GameState.FormatMoney(amount).Replace(" DM", " DM  pro Monat"));
    }

    private void Term(Surface s, int y, string label, string value)
    {
        s.Text(Assets.Mini, TextX, y, label, TextColor);
        s.Text(Assets.Mini, ColonX, y, ":", TextColor);
        s.Text(Assets.Mini, ValueX, y, value, TextColor);
    }

    /// <summary>Knopf aus kb\2 mit heller Schrift wie im Video.</summary>
    private void Button(Surface s, Rect r, string text)
    {
        var pressed = _pressed == r;
        s.Blit(Assets.Image(2), new Rect(17, pressed ? 191 : 179, Widgets.ButtonWidth, Widgets.ButtonHeight), r.X, r.Y);
        s.TextCentered(Assets.Mini, r.X + r.W / 2 + (pressed ? 1 : 0), r.Y + 4 + (pressed ? 1 : 0), text, ButtonText);
    }

    /// <summary>[APPROX] ▲▼-Kasten: im Original eine Grafik, die in keiner kb-Datei gefunden wurde – hier gezeichnet.</summary>
    private void DrawArrows(Surface s)
    {
        var box = new Rect(ArrowUp.X, ArrowUp.Y, ArrowUp.W, ArrowDown.Bottom - ArrowUp.Y);
        s.Fill(box, ArrowFill);
        s.Outline(box, EmptyBoxBorder);
        s.HLine(box.X + 1, ArrowDown.Y, box.W - 2, EmptyBoxBorder);
        for (var row = 0; row < 4; row++)
        {
            var up = _pressed == ArrowUp ? 1 : 0;
            var down = _pressed == ArrowDown ? 1 : 0;
            s.HLine(box.X + 6 - row, ArrowUp.Y + 3 + row + up, 1 + 2 * row, ArrowColor);
            s.HLine(box.X + 6 - row, ArrowDown.Y + 7 - row + down, 1 + 2 * row, ArrowColor);
        }
    }

    private void DrawTotals(Surface s)
    {
        var mini = Assets.Mini;
        s.HLine(89, 212, 223, TextColor);
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
