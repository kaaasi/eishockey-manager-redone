using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens;

/// <summary>
/// Fenster „Saisonende“ je Manager: Hauptmenü mit Infoleiste (29. April), statt der Titelleiste „Saisonende“
/// in großer Schrift, darunter das Fenster „Ereignisse“ (Symbole aus kb\13 oder „Keine besonderen Ereignisse...“),
/// Platzierung und Prämie (Video 11:53 und 18:38). Danach beginnt die neue Saison – oder das Spiel endet.
/// </summary>
public sealed class SeasonEndSummaryScreen : Screen
{
    /// <summary>
    /// [ORIGINAL] Fenster 0x0B155 bei (4 | 80) mit 13 × 6 Teilen (Datei 0x26DE6–0x26E04 in 0x26D9A); vorher (3 | 80), 314 × 113.
    /// </summary>
    public static readonly Rect Window = new(4, 80, 13 * 24, 6 * 19);
    public const int IconWidth = 24;
    public const int IconHeight = 21;

    private readonly MainMenuScreen _menu;
    private int _index;

    public SeasonEndSummaryScreen(ScreenHost host, MainMenuScreen menu) : base(host) => _menu = menu;

    /// <summary>
    /// Öffnet das Fenster für den ersten Manager. Der Aufruf gehört in die Eingabe des Bildschirms davor (R12U-02):
    /// Der Managerwechsel steht im Spielkern, <see cref="Enter"/> und <see cref="Draw"/> ändern nichts.
    /// </summary>
    public static SeasonEndSummaryScreen Open(ScreenHost host, MainMenuScreen menu)
    {
        host.Game!.ShowSeasonEndManager(0);
        return new SeasonEndSummaryScreen(host, menu);
    }

    private GameState Game => Host.Game!;
    private SeasonRecord Record => Game.History[^1];

    /// <summary>
    /// Ereignissymbol in kb\13 (Zellen 24 × 21 im Raster 24 × 21).
    /// [ORIGINAL] Symbolnummer → Zelle (Datei 0x6CA6 + 0x39: <c>while (n &gt; 5) { n -= 6; y += 21; }</c>, x = n · 24): sechs Zellen je
    /// Zeile, <see cref="Symbol"/> = Spalte <c>n % 6</c>, Zeile <c>n / 6</c>. Statusbits 0–6 zeigen die Symbole 2, 1, 0, 5, 4, 6, 8
    /// (Tabelle DGROUP 0x4B2E, Datei 0x2714F): Bit 0 Aufstieg = 2 (Pfeil hoch), Bit 1 Abstieg = 1 (Pfeil runter), Bit 2 Zwangsabstieg = 0,
    /// Bit 4 (DEB-Pokal) = 4 (Fahne), Bit 5 (Europapokal) = 6 (Landkarte mit Pokal), Bit 6 (unter den ersten fünf der 1. Liga) = 8 (Weltkugel).
    /// [APPROX] Meister: Das Original zeigt zu Bit 3 das Symbol 5 (DGROUP 0x4B2E); das Remake nimmt weiter den Puck (Zelle 3/0), bis ein Bild den
    /// Meistertitel belegt.
    /// [ORIGINAL] Einzelprämien nach den Statusbits (Datei 0x2718A, 0x271D0, 0x2721D, Spur statistiken #135): bester Torhüter
    /// Symbol 12, Topscorer Symbol 3, Fair-Play Symbol 13.
    /// </summary>
    public static Rect EventIcon(SeasonEvent e) => e switch
    {
        SeasonEvent.Promotion => Symbol(2),
        SeasonEvent.Relegation => Symbol(1),
        // [ORIGINAL] Statusbit 0x04 (Zwangsabstieg) → Symbol 0, Tabelle DGROUP 0x4B2E (Datei 0x2714F); DOSBox-Beleg
        // `file16_9_ereignisse_29april` (Spur saisonwechsel Teil 5, D3). Vorher fiel es auf Symbol 6.
        SeasonEvent.ForcedRelegation => Symbol(0),
        SeasonEvent.GermanChampion => Cell(3, 0),
        SeasonEvent.CupWinner => Symbol(4),
        SeasonEvent.BestGoalie => Symbol(12),
        SeasonEvent.TopScorer => Symbol(3),
        SeasonEvent.FairPlay => Symbol(13),
        _ => Symbol(6),
    };

    /// <summary>Einzelprämie (Symbol nach den Statusbits, also nach <see cref="TopFiveIcon"/>)?</summary>
    public static bool IsSinglePremium(SeasonEvent e) => e is SeasonEvent.BestGoalie or SeasonEvent.TopScorer or SeasonEvent.FairPlay;

    /// <summary>
    /// Symbol 8 (Weltkugel) für Statusbit 0x40 „unter den ersten fünf der 1. Liga“ ([ORIGINAL] Datei 0x2714F, Tabelle DGROUP 0x4B2E; Befund #125).
    /// Das Bit ist abgeleitet, nicht gespeichert (<see cref="GameState.HasTopFiveStatus"/>) und kommt als letztes Symbol (Bit 6).
    /// </summary>
    public static Rect TopFiveIcon => Symbol(8);

    /// <summary>Zelle der Symbolnummer <paramref name="n"/> im Raster mit sechs Zellen je Zeile.</summary>
    public static Rect Symbol(int n) => Cell(n % 6, n / 6);

    /// <summary>
    /// Symbole des Fensters in der Folge des Originals: Statusbits 0–6 (Datei 0x2714F–0x2717D), Einzelprämien Torhüter, Topscorer,
    /// Fair-Play (Datei 0x2717F–0x2725D), dann je Jugendspieler, der in den Kader rückt, Symbol 9 (Datei 0x272D8–0x27320).
    /// </summary>
    public static List<Rect> Icons(GameState game, SeasonRecord record, ManagerSeasonResult result)
    {
        // Symbol 9 kommt nicht aus den Statusbits, sondern zählt je Neuzugang am Ende (sonst fiele es auf Symbol 6).
        var icons = result.Events.Where(e => !IsSinglePremium(e) && e != SeasonEvent.YouthJoined).Select(EventIcon).ToList();
        if (game.HasTopFiveStatus(record, result))
            icons.Add(TopFiveIcon);
        // [ORIGINAL] Datei 0x2717F–0x2725D: nach den Statusbits 0–6 die Einzelprämien in der Folge Torhüter, Topscorer, Fair-Play.
        icons.AddRange(result.Events.Where(IsSinglePremium).Select(EventIcon));
        // [ORIGINAL] Datei 0x27302–0x2730F: je erfolgreichem Aufruf 0x20:0x89F (Jugend in den Kader) Symbol 9 an der nächsten Stelle.
        for (var i = 0; i < YouthJoined(result); i++)
            icons.Add(Symbol(EventSymbols.Youth));
        return icons;
    }

    /// <summary>
    /// Zahl der Jugendspieler, die im Fenster dieses Managers in den Kader rücken (Symbol 9): je Neuzugang ein
    /// <see cref="SeasonEvent.YouthJoined"/>, das der Spielkern vor dem Fenster vermerkt ([ORIGINAL] Jugend in der Managerschleife
    /// von 0x26F20, Datei 0x2728C–0x27320; <c>GameState.SeasonEndYouthEvents</c>, Spur <c>kader-rest</c> #791).
    /// </summary>
    public static int YouthJoined(ManagerSeasonResult result) => result.Events.Count(e => e == SeasonEvent.YouthJoined);

    /// <summary>Mindestbreite der Platzzahl (DGROUP 0x374) in der Zeile „n. Platz der …“.</summary>
    public const int PlaceWidth = 5;

    /// <summary>
    /// Zeile „n. Platz der …“: Die Platzzahl kommt aus der Zahlenausgabe 0x38:0x2C2 (Datei 0x2C10B in 0x2C099) mit der Mindestbreite
    /// DGROUP 0x374; fehlende Stellen zählen beim Zentrieren mit der Breite der Ziffer 0 (<see cref="Competitions.CompetitionWidgets.Number"/>).
    /// [APPROX] 0x26F20 setzt 0x374 nicht selbst; die Breite 5 ist der Wert, mit dem alle vier DOSBox-Bilder übereinstimmen
    /// (<c>file46_2</c> „10.“ pixelgleich, <c>file16_9</c>, <c>file13_15</c>, <c>file23_1</c>: Mitte der Zeile 11,5–15,5 Pixel rechts von 160).
    /// </summary>
    public static string PlaceText(ManagerSeasonResult result) =>
        $"{Competitions.CompetitionWidgets.Number(result.Rank, PlaceWidth)}. Platz der {SeasonInfo.LeagueGenitive(result.League)}";

    private static Rect Cell(int column, int row) => new(column * IconWidth, row * IconHeight, IconWidth, IconHeight);

    public override void Update(InputState input)
    {
        if (!(input.LeftClick || input.RightClick || input.Keys.Count > 0))
            return;
        _index++;
        if (_index < Record.Managers.Count)
        {
            Game.ShowSeasonEndManager(_index); // Eingabe löst den Managerwechsel aus (R12U-02), nicht Enter/Draw
            return;
        }
        if (Game.IsGameOver)
        {
            Host.Show(new SeasonEndGameOverScreen(Host, null, null));
            return;
        }
        Statistics.HighscoreStore.For(Host).Record(); // [ORIGINAL] Saisonwechsel: Manager in die Bestenliste eintragen (Datei 0x56647)
        // Spur transfer-vertraege-komplett: ausgelaufene Verträge bleiben zur Verhandlung im Kader (0x27D05) und werden vor
        // dem ersten Tag der neuen Saison verhandelt.
        Game.StartNextSeasonWithRenewals();
        Host.Show(Team.ContractsScreen.ForSeasonEnd(Host, AfterRenewals));
    }

    private Screen AfterRenewals()
    {
        Game.ReturnToFirstActiveManager();
        // [ORIGINAL] #195 (Integration Runde 8, Welle 3, Vorschlag aus PR #265): Im Modus „nur Weltkugel“ läuft auch nach dem
        // Saisonwechsel zuerst die Tagesfunktion Datei 0x54C9F (Sprung zum WM-Termin); das Menü kommt erst am Tagesende.
        if (Game.WorldOnly)
            Game.Continue();
        // [ORIGINAL] Datei 0x56609: Auslosung der ersten DEB-Pokal-Runde beim Saisonwechsel (Spur debpokal-auslosung)
        return Competitions.DebCupDrawFlow.AtSeasonStart(Host, _menu);
    }

    public override void Draw(Surface s)
    {
        var result = Record.Managers.Count == 0 ? null : Record.Managers[Math.Min(_index, Record.Managers.Count - 1)];
        _menu.SelectedBigTile = -1;
        // #768: [ORIGINAL] 0x26F20 zeigt je Manager zuerst das Fenster 0x26D9A (Aufruf Datei 0x2700A) und bucht danach die Prämien
        // (Datei 0x270DB–0x27258); die Infoleiste zeigt also das Konto vor der Prämie (`file46_2`: FILE44 − April-Abschluss
        // 503.307 DM = 17.523.876 DM bei 100.000 DM Prämie). Das Remake bucht im Spielkern vorher und zieht sie hier für die
        // Anzeige ab.
        _menu.InfoMoney = result is not null && !Game.NationalCoachMenu && Game.CurrentManager == result.Manager
            ? Game.ActiveManager.Money - result.Premium
            : null;
        try
        {
            _menu.Draw(s);
        }
        finally
        {
            _menu.InfoMoney = null;
        }

        // Titelleiste durch „Saisonende“ ersetzen, Kacheln ausblenden (nur Sternenhintergrund)
        var background = Assets.Image(MainMenuScreen.Background);
        s.Blit(background, new Rect(0, 0, Surface.Width, 21), 0, 0);
        s.Blit(background, new Rect(0, 77, Surface.Width, Surface.Height - 77), 0, 77);
        Widgets.ShadowText(s, Assets.Bold, 160, 4, "Saisonende", Colors.White, 0);

        // [ORIGINAL] 0x26D9A (Datei 0x26DE6–0x26E66): Fenster, Schrift 2, „Ereignisse“ zentriert x 0–321 auf der Grundlinie 89,
        // Farbe 0x0A, Schatten 0x1C; Linie (13 | 91)–(306 | 91) Farbe 7 (0x0B7B5).
        Widgets.Frame(s, Assets, Window, FrameStyle.Orange, SeasonEndScreen.WindowFill);
        var flow = Assets.Flow;
        Competitions.CompetitionWidgets.Centered(s, flow, 0, 321, 89, "Ereignisse", 0x0A, 0x1C);
        s.HLine(13, 91, 294, Colors.White);

        if (result is null)
            return;
        var icons = Icons(Game, Record, result);
        if (icons.Count == 0)
            // [ORIGINAL] Datei 0x27326–0x27348: zentriert x 10–305, Grundlinie 100, Farbe 7, Schatten 0.
            Competitions.CompetitionWidgets.Centered(s, flow, 10, 305, 100, "Keine besonderen Ereignisse...", Colors.White, 0);
        for (var i = 0; i < icons.Count; i++)
        {
            var (x, y) = EventSymbols.Position(i);
            EventSymbols.Draw(s, Assets, icons[i], x, y);
        }

        // [ORIGINAL] Datei 0x2705F–0x2707D bzw. 0x273D1–0x273EF: zentriert x 0–320, Grundlinie 170 bzw. 180, Farbe 7, Schatten 0.
        Competitions.CompetitionWidgets.Centered(s, flow, 0, 320, 170, PlaceText(result), Colors.White, 0);
        var premium = result.Premium > 0 ? GameState.FormatMoney(result.Premium) : "keine";
        Competitions.CompetitionWidgets.Centered(s, flow, 0, 320, 180, $"Erhaltene Prämien: {premium}", Colors.White, 0);
    }
}
