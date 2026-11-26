using EishockeyManager.Core;
using EishockeyManager.UI.Screens.Competitions;

namespace EishockeyManager.UI.Screens.Live;

/// <summary>
/// Penalty-Bildschirm des Penaltyschießens (F076, Spur live-verschraenkt Teil 3b; <c>docs/analyse/live-spieltag.md</c> §10):
/// Ankündigung vor den Schüssen und Endergebnis danach, je auf dem Hintergrund mit drei Farbstreifen. Nur im Anzeigeweg von
/// 0x3901B, also wenn ein Verein der Paarung live zugesehen wird (Datei 0x390B8–0x390D6, <see cref="Club.WatchedLive"/>).
/// <list type="bullet">
/// <item>[ORIGINAL] Hintergrund 0x35CBE: ganzer Bildschirm Farbe 11 · DGROUP 0x6AE (0x134C2; 0x6AE ist immer 1, ohne Schreiber),
///   dann (0x0B620) x 1 … 318, y 1 … 79 Farbe 0, y 80 … 159 Farbe 0x12, y 160 … 238 Farbe 0x18; Schrift 1.</item>
/// <item>[ORIGINAL] Ankündigung (Datei 0x3917F–0x39373): „!    Penaltyschiessen    !“ (DGROUP 0xCF8) auf Grundlinie 32 Farbe 0x1E,
///   Heimverein 99 Farbe 0x18, „gegen“ (DGROUP 0xCFC) 123 Farbe 0x1E, Gast 152 Farbe 0x18; je zentriert x 0 … 320, Schatten 0
///   (0x0B6A5). Einblenden 0x0C5CE, Klick 38h:118h (0x0B908, linke oder rechte Taste).</item>
/// <item>[ORIGINAL] Endergebnis (Datei 0x395B8–0x39769): „Endergebnis:“ (DGROUP 0xD00) statt der Überschrift, Vereine und „gegen“
///   wie oben, dazu Penaltytore Heim + „ : “ (DGROUP 0x4EA9) + Gast + „   n.P.“ (DGROUP 0xD04) auf Grundlinie 212 Farbe 0x10;
///   davor Pause 50 (Datei 0x3958D–0x395A2), danach Klick. Die Zahlen sind die Treffer des Penaltyschießens (Datei 0x39284 setzt
///   DGROUP 0x5C4B/0x5C4C vor den Schüssen auf 0). Mindestbreite DGROUP 0x374 = 1: mit Darstellung 0 baut Datei 0x39421 den
///   Live-Bildschirm 0x5353C auf, dessen Grafiklader 0xC17D 0x374 = 1 setzt (Datei 0x0C1A1); [APPROX] mit Darstellung 1/2
///   (eigenes Eis, T7) nicht verfolgt.</item>
/// <item>[APPROX] Die Schüsse selbst (Datei 0x3941A–0x39588: Live-Bildschirm bzw. eigenes Eis, Torszene je Schuss, Spielstand
///   0x51FEA, Pause 80) zeigt das Remake noch nicht – Teil 2 der Spur <c>torszene-dauer</c> (T7, Entscheidung Leitstelle
///   26.11.2026); zwischen Ankündigung und Endergebnis nur die Pause 50.</item>
/// </list>
/// </summary>
public sealed class LivePenaltyScreen : Screen
{
    public const string Title = "!    Penaltyschiessen    !";
    public const string ResultTitle = "Endergebnis:";
    public const string Versus = "gegen";
    public const string AfterPenalties = "   n.P.";

    /// <summary>[ORIGINAL] Farbe des Grunds und Farben der drei Streifen (0x35CBE).</summary>
    public const byte BorderColor = 11;

    public static readonly (Rect Area, byte Color)[] Stripes =
    [
        (new Rect(1, 1, 318, 79), 0),
        (new Rect(1, 80, 318, 80), 0x12),
        (new Rect(1, 160, 318, 79), 0x18),
    ];

    public const int TitleBaseline = 32;
    public const int HomeBaseline = 99;
    public const int VersusBaseline = 123;
    public const int AwayBaseline = 152;
    public const int ResultBaseline = 212;

    /// <summary>[ORIGINAL] Pause vor dem Endergebnis (Datei 0x3959D: 0x130EE(50)).</summary>
    public const int PauseBeforeResult = 50;

    private readonly Screen _back;
    private readonly Club _home;
    private readonly Club _away;

    /// <param name="result">Treffer des Penaltyschießens (Heim, Gast) für das Endergebnis; null für die Ankündigung.</param>
    public LivePenaltyScreen(ScreenHost host, Screen back, Club home, Club away, (int Home, int Away)? result) : base(host)
    {
        _back = back;
        _home = home;
        _away = away;
        Result = result;
    }

    /// <summary>Treffer des Endergebnisses oder null (Ankündigung).</summary>
    public (int Home, int Away)? Result { get; }

    /// <summary>[ORIGINAL] Zeile des Endergebnisses (Datei 0x3969F–0x39725).</summary>
    public static string ResultLine((int Home, int Away) goals) => $"{goals.Home} : {goals.Away}{AfterPenalties}";

    public override void Update(InputState input)
    {
        // [ORIGINAL] 38h:118h wartet auf einen Mausklick (0x0B908); [APPROX] Escape als Komfort des Remakes (wie B11 = A,
        // Nutzer 24.11.2026).
        if (input.LeftClick || input.RightClick || input.Pressed(Key.Escape))
            Host.Show(_back);
    }

    public override void Draw(Surface s)
    {
        s.Palette = Assets.Palette;
        s.Clear(BorderColor);
        foreach (var (area, color) in Stripes)
            s.Fill(area, color);
        var font = Assets.Bold;
        CompetitionWidgets.Centered(s, font, 0, 320, TitleBaseline, Result is null ? Title : ResultTitle, 0x1E, 0);
        CompetitionWidgets.Centered(s, font, 0, 320, HomeBaseline, _home.Name, 0x18, 0);
        CompetitionWidgets.Centered(s, font, 0, 320, VersusBaseline, Versus, 0x1E, 0);
        CompetitionWidgets.Centered(s, font, 0, 320, AwayBaseline, _away.Name, 0x18, 0);
        if (Result is { } goals)
            CompetitionWidgets.Centered(s, font, 0, 320, ResultBaseline, ResultLine(goals), 0x10, 0);
    }
}
