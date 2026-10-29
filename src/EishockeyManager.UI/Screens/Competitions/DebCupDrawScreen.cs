using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens.Competitions;

/// <summary>
/// „DEB-Pokal Auslosung“: die Paarungen der laufenden Pokalrunde in zwei Spalten (links gegen rechts, Zeile für Zeile).
/// Zeigt nur vorhandenen Zustand (<see cref="DebCup.Draw"/>), verändert nichts.
/// </summary>
/// <remarks>
/// Aufbau aus em.exe (Overlay „DEB-Pokal-Auslosung“, Datei 0x33B83 und 0x33E5E), vermessen am Video
/// „Die Oberfläche von EM“ (3:55):
/// <list type="bullet">
/// <item>[ORIGINAL] Fenster 13 × 12 Rahmenteile ab (3 | 3), Titel zentriert zwischen x = 0 und 321.</item>
/// <item>[ORIGINAL] Plätze: Spalte links x = 37, rechts x = 164 (DGROUP 0x4DD0), Streifen 120 × 10 (kb\25),
///   Zeile y = 128 − (n/2 · 11)/2 + 11 · Zeile; Name zentriert im Streifen (x 2 … 119), Grundlinie +7, schwarzer Schatten.</item>
/// <item>[ORIGINAL] Farbe nach Spielklasse (DGROUP 0x4E17: 9, 10, 11), Vereine der Manager 24 (gelb), Kennziffer „(n)“.</item>
/// <item>[ORIGINAL] „TASTE DRÜCKEN“ zentriert zwischen x = 90 und 237, Grundlinie 221.</item>
/// </list>
/// [APPROX] Das Original zeigt die Auslosung nach dem dritten Termin einer Runde (auf Wunsch „live“, Platz für Platz);
/// hier ist sie über Paarungen → DEB-Pokal jederzeit fertig sichtbar. Noch nicht gezogene Plätze (vor der ersten
/// Runde nie der Fall) erscheinen als grüne Streifen.
/// </remarks>
public sealed class DebCupDrawScreen : Screen
{
    public static readonly Rect Window = new(3, 3, 312, 228);
    public const int TitleBaseline = 26;
    public static readonly int[] ColumnX = [37, 164];
    public const int StripWidth = 120;
    public const int StripHeight = 10;
    public const int RowPitch = 11;
    public const int StripGraphic = 25;
    public const string Title = "DEB-Pokal Auslosung";
    public const string PressKey = "TASTE DRÜCKEN";

    /// <summary>[ORIGINAL] Schattenfarbe des Titels (letztes Argument des Textaufrufs 1, Namen und Hinweis 0).</summary>
    public const byte TitleShadow = 1;

    /// <summary>Farben der Spielklassen 1–3 (DGROUP 0x4E17).</summary>
    public static readonly byte[] ClassColors = [9, 10, 11];

    private readonly Screen _back;
    private readonly IReadOnlyList<int?>? _slots;

    /// <param name="slots">Nur für Bildschirmfotos: feste Belegung statt der laufenden Auslosung (null = Streifen).</param>
    public DebCupDrawScreen(ScreenHost host, Screen back, IReadOnlyList<int?>? slots = null) : base(host)
    {
        _back = back;
        _slots = slots;
    }

    /// <summary>Plätze der Anzeige: feste Belegung oder die Ziehung der laufenden Runde.</summary>
    public IReadOnlyList<int?> Slots =>
        _slots ?? Host.Game?.DebCup?.Draw.Select(c => (int?)c).ToList() ?? [];

    /// <summary>Oberkante des Streifens von Platz <paramref name="slot"/> bei <paramref name="count"/> Plätzen.</summary>
    public static (int X, int Y) SlotPosition(int slot, int count) =>
        (ColumnX[slot % 2], 0x80 - count / 2 * RowPitch / 2 + slot / 2 * RowPitch);

    public override void Update(InputState input)
    {
        if (input.LeftClick || input.RightClick || input.Keys.Count > 0)
            Host.Show(_back);
    }

    public override void Draw(Surface s)
    {
        s.Palette = Assets.Palette;
        s.Blit(Assets.Image(MainMenuScreen.Background), 0, 0);
        Widgets.Frame(s, Assets, Window, FrameStyle.Orange);

        CompetitionWidgets.Centered(s, Assets.Bold, 0, 321, TitleBaseline, Title, Colors.White, TitleShadow);

        var slots = Slots;
        var managers = Host.Game?.Managers.Select(m => m.ClubIndex).ToHashSet() ?? [];
        var mini = Assets.Mini;
        for (var i = 0; i < slots.Count; i++)
        {
            var (x, y) = SlotPosition(i, slots.Count);
            if (slots[i] is not { } index || Host.Game is not { } game)
            {
                s.Blit(Assets.Image(StripGraphic), x, y);
                continue;
            }
            var club = game.Clubs[index];
            var text = $"{club.Name}({CupClass.Digit(club)})";
            var color = managers.Contains(index) ? Colors.Yellow : ClassColors[CupClass.Digit(club) - 1];
            CompetitionWidgets.Centered(s, mini, x + 2, x + StripWidth - 1, y + 7, text, color, Colors.Black);
        }

        if (slots.All(c => c is not null)) // erst nach der letzten Kugel (Video 3:45 ohne, 3:55 mit Hinweis)
            CompetitionWidgets.Centered(s, mini, 90, 237, 221, PressKey, Colors.White, Colors.Black);
    }
}

/// <summary>
/// Anmeldung im Hauptmenü: Paarungen → DEB-Pokal.
/// [ORIGINAL] Die Kachel ruft denselben Stub wie „Tabelle DEB-Pokal“ (Datei 0x4584A/0x45897, Stub 0xE8:0x75 → 0x32E23): die
/// Übersicht „DEB-Pokalspiele“ (<see cref="DebCupGamesScreen"/>), nicht die Auslosung (Review R4-01; Entscheidung des Nutzers
/// 29.10.2026). Minimale Änderung an der Spur „wettbewerbe“. <see cref="DebCupDrawScreen"/> bleibt für die Live-Auslosung.
/// </summary>
internal static class CompetitionMenuEntries
{
    [MenuEntry("DEB-Pokal")]
    private static Screen DebCup(ScreenHost host, Screen back) => new DebCupGamesScreen(host, back);
}
