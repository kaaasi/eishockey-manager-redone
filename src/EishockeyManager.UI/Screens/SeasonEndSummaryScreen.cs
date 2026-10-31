using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens;

/// <summary>
/// Fenster „Saisonende“ je Manager: Hauptmenü mit Infoleiste (29. April), statt der Titelleiste „Saisonende“
/// in großer Schrift, darunter das Fenster „Ereignisse“ (Symbole aus kb\13 oder „Keine besonderen Ereignisse...“),
/// Platzierung und Prämie (Video 11:53 und 18:38). Danach beginnt die neue Saison – oder das Spiel endet.
/// </summary>
public sealed class SeasonEndSummaryScreen : Screen
{
    public static readonly Rect Window = new(3, 80, 314, 113);
    public const int IconWidth = 24;
    public const int IconHeight = 21;
    public const int IconPitch = 35;

    private readonly MainMenuScreen _menu;
    private int _index;

    public SeasonEndSummaryScreen(ScreenHost host, MainMenuScreen menu) : base(host) => _menu = menu;

    private GameState Game => Host.Game!;
    private SeasonRecord Record => Game.History[^1];

    /// <summary>
    /// Ereignissymbol in kb\13 (Zellen 24 × 21 im Raster 24 × 21).
    /// [ORIGINAL] Symbolnummer → Zelle (Datei 0x6CA6 + 0x39: <c>while (n &gt; 5) { n -= 6; y += 21; }</c>, x = n · 24): sechs Zellen je
    /// Zeile, <see cref="Symbol"/> = Spalte <c>n % 6</c>, Zeile <c>n / 6</c>. Statusbits 0–6 zeigen die Symbole 2, 1, 0, 5, 4, 6, 8
    /// (Tabelle DGROUP 0x4B2E, Datei 0x2714F): Bit 0 Aufstieg = 2 (Pfeil hoch), Bit 1 Abstieg = 1 (Pfeil runter),
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

    private static Rect Cell(int column, int row) => new(column * IconWidth, row * IconHeight, IconWidth, IconHeight);

    public override void Update(InputState input)
    {
        if (!(input.LeftClick || input.RightClick || input.Keys.Count > 0))
            return;
        _index++;
        if (_index < Record.Managers.Count)
        {
            Game.CurrentManager = Record.Managers[_index].Manager;
            return;
        }
        if (Game.IsGameOver)
        {
            Host.Show(new SeasonEndGameOverScreen(Host, null, null));
            return;
        }
        Statistics.HighscoreStore.For(Host).Record(); // [ORIGINAL] Saisonwechsel: Manager in die Bestenliste eintragen (Datei 0x56647)
        Game.StartNextSeason();
        Game.CurrentManager = Game.ActiveManagers.Select(m => Game.Managers.ToList().IndexOf(m)).First();
        // [ORIGINAL] Datei 0x56609: Auslosung der ersten DEB-Pokal-Runde beim Saisonwechsel (Spur debpokal-auslosung)
        Host.Show(Competitions.DebCupDrawFlow.AtSeasonStart(Host, _menu));
    }

    public override void Enter()
    {
        if (Record.Managers.Count > 0)
            Game.CurrentManager = Record.Managers[Math.Min(_index, Record.Managers.Count - 1)].Manager;
    }

    public override void Draw(Surface s)
    {
        _menu.SelectedBigTile = -1;
        _menu.Draw(s);

        // Titelleiste durch „Saisonende“ ersetzen, Kacheln ausblenden (nur Sternenhintergrund)
        var background = Assets.Image(MainMenuScreen.Background);
        s.Blit(background, new Rect(0, 0, Surface.Width, 21), 0, 0);
        s.Blit(background, new Rect(0, 77, Surface.Width, Surface.Height - 77), 0, 77);
        Widgets.ShadowText(s, Assets.Bold, 160, 4, "Saisonende", Colors.White, 0);

        Widgets.Frame(s, Assets, Window, FrameStyle.Orange, SeasonEndScreen.WindowFill);
        var flow = Assets.Flow;
        Widgets.ShadowText(s, flow, 160, 84, "Ereignisse", 11, 198);
        s.HLine(10, 91, 300, 1);

        if (Record.Managers.Count == 0)
            return;
        var result = Record.Managers[Math.Min(_index, Record.Managers.Count - 1)];
        var icons = result.Events.Where(e => !IsSinglePremium(e)).Select(EventIcon).ToList();
        if (Game.HasTopFiveStatus(Record, result))
            icons.Add(TopFiveIcon);
        // [ORIGINAL] Datei 0x2717F–0x2725D: nach den Statusbits 0–6 die Einzelprämien in der Folge Torhüter, Topscorer, Fair-Play.
        icons.AddRange(result.Events.Where(IsSinglePremium).Select(EventIcon));
        if (icons.Count == 0)
            Widgets.ShadowText(s, flow, 160, 97, "Keine besonderen Ereignisse...", Colors.White, 0);
        else
        {
            var sheet = Assets.Image(13);
            var x = 160 - (icons.Count * IconPitch - (IconPitch - IconWidth)) / 2;
            foreach (var icon in icons)
            {
                s.Outline(new Rect(x - 1, 102, IconWidth + 2, IconHeight + 2), 0);
                s.Blit(sheet, icon, x, 103);
                x += IconPitch;
            }
        }

        Widgets.ShadowText(s, flow, 160, 164, $"{result.Rank}. Platz der {SeasonInfo.LeagueGenitive(result.League)}", Colors.White, 0);
        var premium = result.Premium > 0 ? GameState.FormatMoney(result.Premium) : "keine";
        Widgets.ShadowText(s, flow, 160, 175, $"Erhaltene Prämien: {premium}", Colors.White, 0);
    }
}
