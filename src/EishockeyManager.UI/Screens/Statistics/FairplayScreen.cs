using EishockeyManager.Core;
using EishockeyManager.UI.Screens.Competitions;

namespace EishockeyManager.UI.Screens.Statistics;

/// <summary>
/// Statistiken → Fairplay: „Die Fair-Play-Tabelle der Liga“ mit Strafminuten gesamt und je Spiel
/// (<see cref="GameState.FairPlayTable"/>). Zeigt nur vorhandenen Zustand; jeder Klick oder jede Taste schließt.
/// </summary>
/// <remarks>
/// Aufbau aus em.exe Ov 0xE0, Funktion Datei 0x2A1E0 mit Argument 1 (Menüsprung Datei 0x45816); Vermessung nur aus dem
/// Code – die Referenzbilder waren für diese Sitzung nicht erreichbar, ein Pixelvergleich am Video steht aus
/// (Fundstellen F8 0:21:38 u. a. in <c>docs/referenz-videos.md</c>).
/// <list type="bullet">
/// <item>[ORIGINAL] Fenster 11 × 7 Teile ab (30 | 24) (0x30:0x335 mit 0, 2; Datei 0x2A2CF); Linie (36 … 284 | 46) Farbe 1.</item>
/// <item>[ORIGINAL] Titel Flow „Die Fair-Play-Tabelle der Liga“ (DGROUP 0x4C7E) zentriert 35 … 285, Grundlinie 35, Farbe 10,
///   Schatten 28; Kopf (Mini, Farbe 1) DGROUP 0x4C9D bei (38 | 44).</item>
/// <item>[ORIGINAL] Zwölf Zeilen, Grundlinie 55 + 8 n (Datei 0x2A39B–0x2A5BD); Farbe 24 für Managervereine (Vereinsfeld
///   0x16 ≠ 0), sonst 30: „Platz. Verein“ (Mindestbreite 2) bei x = 38, „Minuten min.“ (Mindestbreite 3) bei x = 150,
///   je Spiel „M.HH min.“ (Mindestbreite 2, Hundertstel zweistellig) bei x = 205.</item>
/// <item>[ORIGINAL] Danach Warten auf eine Maustaste (0x38:0x118, Datei 0x2A5DD). [APPROX] Tasten schließen ebenfalls.</item>
/// </list>
/// </remarks>
public sealed class FairplayScreen : Screen
{
    public static readonly Rect Window = new(30, 24, 11 * 24, 7 * 19);
    public const string Title = "Die Fair-Play-Tabelle der Liga";
    public const string Header = "PL.    VEREIN                           GES.STRAFZEIT       PRO SPIEL";
    public const int FirstRow = 55;
    public const int RowPitch = 8;

    private readonly Screen _back;

    public FairplayScreen(ScreenHost host, Screen back) : base(host) => _back = back;

    /// <summary>Tabelle der Liga des Managers am Zug (leer ohne Liga).</summary>
    public static IReadOnlyList<FairPlayRow> Table(GameState game) =>
        game.Clubs[game.ActiveManager.ClubIndex].League is { } league ? game.FairPlayTable(league) : [];

    /// <summary>[ORIGINAL] „PRO SPIEL“ (Datei 0x2A501–0x2A589).</summary>
    public static string PerGame(FairPlayRow row)
    {
        var v = row.PerGameHundredths;
        return CompetitionWidgets.Number(v / 100, 2) + "." + (v % 100 < 10 ? "0" : "") + (v % 100) + " min.";
    }

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
        s.HLine(36, 46, 284 - 36 + 1, 1);
        CompetitionWidgets.Centered(s, Assets.Flow, 35, 285, 35, Title, 10, 28);
        var mini = Assets.Mini;
        CompetitionWidgets.Left(s, mini, 38, 44, Header, 1);
        if (Host.Game is not { } game)
            return;
        var managerClubs = game.ActiveManagers.Select(m => m.ClubIndex).ToHashSet();
        foreach (var row in Table(game))
        {
            var y = FirstRow + RowPitch * (row.Rank - 1);
            var color = managerClubs.Contains(row.Club.Index) ? Colors.Yellow : Colors.TileBackground;
            CompetitionWidgets.Left(s, mini, 38, y, CompetitionWidgets.Number(row.Rank, 2) + ". " + row.Club.Name, color);
            CompetitionWidgets.Left(s, mini, 150, y, CompetitionWidgets.Number(row.PenaltyMinutes, 3) + " min.", color);
            CompetitionWidgets.Left(s, mini, 205, y, PerGame(row), color);
        }
    }
}
