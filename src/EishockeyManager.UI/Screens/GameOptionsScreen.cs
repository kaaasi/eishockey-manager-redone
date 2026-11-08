using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Screens.Team;

namespace EishockeyManager.UI.Screens;

/// <summary>
/// Optionsbildschirm des Originals (em.exe Datei 0x2CD1C; DOSBox-Bilder 48–51, <c>docs/dosbox-befunde.md</c>): schaltet die
/// Spielstand-Optionen <see cref="GameOptions"/>. Geöffnet über den Knopf „Spielstand“ der Einstellungen.
/// </summary>
/// <remarks>
/// [ORIGINAL] Aufbau (Datei 0x2CD1C–0x2D292): Hintergrund kb\22, drei Fenster 0x30:0x335 aus Rahmenteilen 24 × 19
/// (<see cref="TopWindow"/>, <see cref="ReportWindow"/>, <see cref="CupWindow"/>); Schalter aus kb\33 über 0x2CBC5
/// (Pinguin = an, Loch = aus, groß 19 × 23 bzw. klein 11 × 13, Farbe 0xC0 durchsichtig) an den Stellen der Tabellen
/// DGROUP 0x4C06 (x), 0x4BE8 (y), 0x4BCA (klein); Treffer = Rechteck des Schalters (0x2D2A6–0x2D31A). Spielgeschwindigkeit
/// 0x2CC26: acht kleine Schalter im Abstand 12 ab (190 | 84), Anzahl Pinguine = Wert / 9 + 1; Klick x 191–285, y 84–96
/// setzt <c>(x − 190) / 12 · 10</c> (0x2D51B). Torszenen (Index 9) = 2 zeichnet einen roten Punkt auf den Pinguin
/// (0x2D048) und zeigt beim ersten Mal die Meldung „Neuer Spielmodus:“ (0x2D4EE, Merker DGROUP 0xE044).
/// Kopfzeile Schrift Mini Farbe 1: „V1.00 - Ende: “ + Endjahr bzw. „ Nie.“ (DGROUP 0x5A14 ≥ 2000) + „ (Level n)“ mit
/// n = 6 − DGROUP 0xED8 = Level + 1. Rechtsklick (Mausereignis 3) verlässt den Bildschirm.
/// <para>
/// [APPROX] Nicht nachgebaut: die zweite Kopfzeile „RAM: … Bytes frei.“ (DOS-Speicher, im Remake ohne Bedeutung); die
/// Speicherprüfung vor Torszenen-Stufe 2 (0x2D362: unter 200.000 Bytes frei → Stufe 0); die Folge des WM-Schalters über
/// das Manager-Statusbyte (0x2D3D5, 0x30:0xB). Hintergrundgrafik (Index 13, DGROUP 0x22A) und Scrolling (Index 14, je
/// Manager) sind im Remake ohne Bedeutung und werden fest als „an“ gezeigt (wie Bild 48), Klicks darauf wirken nicht.
/// Die Meldung „Neuer Spielmodus:“ erscheint einmal je geöffnetem Bildschirm statt einmal je Programmlauf. Level der
/// Kopfzeile: aktiver Manager (DGROUP 0xED8 ist ein einzelner Wert).
/// </para>
/// </remarks>
public sealed class GameOptionsScreen : Screen
{
    /// <summary>[ORIGINAL] Fenster oben (0x38 | 1), 10 × 6 Rahmenteile.</summary>
    public static readonly Rect TopWindow = new(56, 1, 240, 114);

    /// <summary>[ORIGINAL] Fenster der Meldungsmatrix (0x5C | 0x77), 9 × 3 Rahmenteile.</summary>
    public static readonly Rect ReportWindow = new(92, 119, 216, 57);

    /// <summary>[ORIGINAL] Fenster der Pokale (0x2E | 0xB3), 10 × 3 Rahmenteile.</summary>
    public static readonly Rect CupWindow = new(46, 179, 240, 57);

    /// <summary>Anzahl der Schalter (Index 0–14 wie im Original, 15 = Zusatzschalter „Ganzer Tag“).</summary>
    public const int SwitchCount = 16;

    /// <summary>
    /// Index 15: Zusatzschalter „Ganzer Tag“ (<see cref="GameOptions.ManagerDayMinuteLoop"/>, #349, R14-D4 = A). Kein Schalter des
    /// Originals; [APPROX] Platz rechts neben „WM-Spiele“ im Pokalfenster, Schalter klein wie Index 10–12.
    /// </summary>
    public const int ManagerDaySwitch = 15;

    /// <summary>Index des Torszenen-Schalters (DGROUP 0x226, dreistufig).</summary>
    public const int GoalScenesSwitch = 9;

    /// <summary>Index 10–12: DEB-Pokal, Europapokal, WM-Spiele.</summary>
    public const int DebCupSwitch = 10;

    /// <summary>Index 13: Hintergrundgrafik (fest an).</summary>
    public const int BackgroundSwitch = 13;

    /// <summary>Index 14: Scrolling (fest an).</summary>
    public const int ScrollingSwitch = 14;

    // [ORIGINAL] DGROUP 0x4C06 (x), 0x4BE8 (y), 0x4BCA (1 = kleiner Schalter)
    private static readonly int[] SwitchX = [222, 222, 222, 252, 252, 252, 282, 282, 282, 200, 184, 214, 200, 244, 220, 268];
    private static readonly int[] SwitchY = [130, 144, 158, 130, 144, 158, 130, 144, 158, 24, 188, 204, 218, 39, 54, 218];
    private static readonly bool[] SwitchSmall = [true, true, true, true, true, true, true, true, true, false, true, true, true, false, false, true];

    private const byte Transparent = 0xC0;
    private const int SpeedX = 190;
    private const int SpeedY = 84;
    private const int SpeedPitch = 12;
    private const int SpeedSlots = 8;

    /// <summary>[ORIGINAL] Klickfläche des Reglers „Spielgeschwindigkeit“: x 191–285, y 84–96 (0x2D51B–0x2D52F).</summary>
    public static readonly Rect SpeedArea = new(191, 84, 95, 13);

    private readonly Screen _back;
    private readonly MessageBox _message;
    private bool _newModeShown;

    public GameOptionsScreen(ScreenHost host, Screen back) : base(host)
    {
        _back = back;
        _message = new(() => host.Game?.ActiveManager.Name ?? "");
    }

    private GameOptions Options => Host.Game!.Options;

    /// <summary>Angezeigte Meldung (leer, wenn keine offen ist).</summary>
    public IReadOnlyList<string> MessageLines => _message.Lines;

    /// <summary>Trefferrechteck des Schalters <paramref name="index"/> (0–14).</summary>
    public static Rect SwitchRect(int index) =>
        SwitchSmall[index] ? new(SwitchX[index], SwitchY[index], 11, 13) : new(SwitchX[index], SwitchY[index], 19, 23);

    /// <summary>Schalter des Matrixfelds (Liga · 3 + Zeile).</summary>
    public static int ReportSwitch(ReportKind kind, League league) => GameOptions.ReportIndex(kind, league);

    /// <summary>Wert des Schalters <paramref name="index"/> (0 = Loch, sonst Pinguin; Torszenen 0–2).</summary>
    public int SwitchValue(int index) => index switch
    {
        < 9 => Options.Reports[index] ? 1 : 0,
        GoalScenesSwitch => Options.LiveMode,
        10 => Options.ShowDebCup ? 1 : 0,
        11 => Options.ShowEuropeanCup ? 1 : 0,
        12 => Options.ShowWorldCup ? 1 : 0,
        ManagerDaySwitch => Options.ManagerDayMinuteLoop ? 1 : 0,
        _ => 1,
    };

    /// <summary>Anzahl der Pinguine im Regler „Spielgeschwindigkeit“ ([ORIGINAL] Wert / 9 + 1, höchstens 8).</summary>
    public static int SpeedPenguins(int speed) => Math.Min(SpeedSlots, speed / 9 + 1);

    public override void Update(InputState input)
    {
        if (_message.Update(input))
            return;
        if (input.Pressed(Key.Escape) || input.Pressed(Key.Enter) || input.RightClick)
        {
            Host.Show(_back);
            return;
        }
        if (!input.LeftClick)
            return;

        if (input.Clicked(SpeedArea))
        {
            Options.SetMatchSpeed((input.MouseX - SpeedX) / SpeedPitch * 10);
            return;
        }
        for (var i = 0; i < SwitchCount; i++)
        {
            if (!input.Clicked(SwitchRect(i)))
                continue;
            Toggle(i);
            return;
        }
    }

    private void Toggle(int index)
    {
        var options = Options;
        switch (index)
        {
            case < 9:
                options.Reports[index] ^= true;
                break;
            case GoalScenesSwitch:
                options.NextLiveMode();
                if (options.LiveMode == GameOptions.PermanentRinkLiveMode && !_newModeShown)
                {
                    _newModeShown = true;
                    // [ORIGINAL] Texte DGROUP 0x4D38/0x4D4A/0x4D69/0x4D84
                    _message.Show("Neuer Spielmodus:", "Das Eisfeld bleibt während des", "gesamten Spieles an. Keine",
                        "Teamveränderungen möglich.");
                }
                break;
            case 10:
                options.ShowDebCup = !options.ShowDebCup;
                break;
            case 11:
                options.ShowEuropeanCup = !options.ShowEuropeanCup;
                break;
            case 12:
                options.ShowWorldCup = !options.ShowWorldCup;
                break;
            case ManagerDaySwitch:
                options.ManagerDayMinuteLoop = !options.ManagerDayMinuteLoop;
                break;
        }
    }

    public override void Draw(Surface s)
    {
        s.Palette = Assets.Palette;
        s.Blit(Assets.Image(MainMenuScreen.Background), 0, 0);
        Widgets.Frame(s, Assets, TopWindow, FrameStyle.Orange);
        Widgets.Frame(s, Assets, ReportWindow, FrameStyle.Orange);
        Widgets.Frame(s, Assets, CupWindow, FrameStyle.Orange);

        // [ORIGINAL] Schrift 3 (Mini), Farbe 7, Schatten 0; Spaltenköpfe 0x30:0x831 Farbe 30
        var small = Assets.Mini;
        string[] rows = ["Drittelpause", "Ergebnisse", "Tabelle"];
        for (var r = 0; r < rows.Length; r++)
            Centered(s, small, 150, 140 + 12 * r, rows[r]);
        string[] leagues = ["1.Liga", "2.Liga", "3.Liga"];
        for (var c = 0; c < leagues.Length; c++)
            CompetitionWidgets.Left(s, small, 215 + 30 * c, 128, leagues[c], LeagueHeadColor);
        string[] cups = ["DEB-Pokal", "Europapokal", "WM-Spiele"];
        for (var c = 0; c < cups.Length; c++)
            Centered(s, small, 100, 193 + 16 * c, cups[c]);
        Centered(s, small, 243, 225, "Ganzer Tag"); // Zusatzschalter (kein Original)

        // [ORIGINAL] Schrift 2 (Flow)
        string[] top = ["Torszenen", "Hintergrundgrafik", "Scrolling", "Spielgeschwindigkeit"];
        for (var t = 0; t < top.Length; t++)
            Centered(s, Assets.Flow, 120, 35 + 20 * t, top[t]);

        for (var i = 0; i < SwitchCount; i++)
            DrawSwitch(s, i, SwitchValue(i));
        if (Options.LiveMode == GameOptions.PermanentRinkLiveMode)
            DrawRedDot(s);
        DrawSpeed(s, Options.MatchSpeed);

        CompetitionWidgets.Left(s, small, 62, 10, HeaderText(), HeaderColor);
        _message.Draw(s, Assets);
    }

    /// <summary>
    /// Weiß mit schwarzem Schatten, zentriert um (x1 + x2) / 2 (Aufruf 0x30:0x885 mit x1 = 100, x2 = 200 bzw. x1 = x2).
    /// [ORIGINAL] vermessen an Bild 48: Linke Kante = Mitte − Breite / 2 („Ergebnisse“ mit ungerader Breite liegt einen
    /// Pixel weiter rechts als bei <see cref="CompetitionWidgets.Centered"/>).
    /// </summary>
    private static void Centered(Surface s, BitmapFont font, int center, int baseline, string text)
    {
        var x = center - CompetitionWidgets.Width(font, text) / 2;
        var top = CompetitionWidgets.Top(font, baseline);
        CompetitionWidgets.Draw(s, font, x + 1, top + 1, text, Colors.Black);
        CompetitionWidgets.Draw(s, font, x, top, text, Colors.White);
    }

    /// <summary>[ORIGINAL] Farbe der Spaltenköpfe „1.Liga“ … (0x30:0x831, Farbe 30).</summary>
    private const byte LeagueHeadColor = 30;

    /// <summary>[ORIGINAL] Farbe der Kopfzeile (Farbe 1).</summary>
    private const byte HeaderColor = 1;

    /// <summary>Kopfzeile „V1.00 - Ende: Nie. (Level 1)“.</summary>
    public string HeaderText()
    {
        var game = Host.Game!;
        // [ORIGINAL] Spielende DGROUP 0x5A14 = 1994 + 2 · Dauer, bei „unbegrenzt“ 22222 (Datei 0x2987C) → „Nie.“
        var end = game.SeasonLimit is null ? " Nie." : (1994 + 2 * (int)game.Setup.Duration).ToString(System.Globalization.CultureInfo.InvariantCulture);
        var level = game.ActiveManager.Level + 1;
        return $"V1.00 - Ende: {end} (Level {level})"; // [ORIGINAL] DGROUP 0x3173, 0x30DB („ Nie.“), 0x3182
    }

    private void DrawSwitch(Surface s, int index, int value)
    {
        var small = SwitchSmall[index] ? 1 : 0;
        var source = new Rect(small * 19, value == 0 ? 23 : 0, 19 - 8 * small, 23 - 10 * small);
        s.Blit(Assets.Image(33), source, SwitchX[index], SwitchY[index], Transparent);
    }

    /// <summary>[ORIGINAL] Roter Punkt 0x2D048: Rechteck (207 | 36)–(210 | 38) Farbe 18, Ecken Farbe 9, (208 | 37) Farbe 24.</summary>
    private static void DrawRedDot(Surface s)
    {
        s.Fill(new Rect(207, 36, 4, 3), 18);
        s.Set(207, 36, 9);
        s.Set(207, 38, 9);
        s.Set(210, 36, 9);
        s.Set(210, 38, 9);
        s.Set(208, 37, 24);
    }

    private void DrawSpeed(Surface s, int speed)
    {
        var penguins = SpeedPenguins(speed);
        // [ORIGINAL] Puffer 100 × 13 mit Farbe 4 gefüllt (0x2CC6A), dann auf (190 | 84) kopiert
        s.Fill(new Rect(SpeedX, SpeedY, 100, 13), 4);
        for (var i = 0; i < SpeedSlots; i++)
        {
            var source = new Rect(19, i < penguins ? 0 : 23, 11, 13);
            s.Blit(Assets.Image(33), source, SpeedX + SpeedPitch * i, SpeedY, Transparent);
        }
    }
}
