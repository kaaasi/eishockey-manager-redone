using EishockeyManager.Core;
using EishockeyManager.UI.Screens.Team;

namespace EishockeyManager.UI.Screens.Competitions;

/// <summary>
/// DEB-Pokal-Auslosung im Spielablauf: wann sie erscheint und was danach kommt.
/// </summary>
/// <remarks>
/// Belege aus em.exe (Overlay „DEB-Pokal-Auslosung“, Dateioffsets):
/// <list type="bullet">
/// <item>[ORIGINAL] Die Anzeige 0x33B83 läuft innerhalb der Auslosung: erste Runde 0x341AB beim Spielstart (Aufruf 0x43A8C,
///   Video „Die Oberfläche von EM“ 3:40 direkt nach „Los jetzt!“) und beim Saisonwechsel (0x56609, zwischen
///   WM-Gruppen 0x5644E und Bestenliste 0x56647); Runden 2–5 in 0x3489D, aufgerufen nach dem dritten Termin der
///   vorigen Runde (0x34F64).</item>
/// <item>[ORIGINAL] Keine Anzeige ohne DEB-Pokal: Spielmodus „nur Weltkugel“ (DGROUP 0xD0B9 = 1) oder DEB-Pokal aus
///   (DGROUP 0x227 = 0), Datei 0x33BAB.</item>
/// <item>[ORIGINAL] Weniger als vier Plätze (Endspiel): kein Fenster und keine Frage, die Auslosung gilt als „live“
///   (0x33BC3 → 0x33E54); am Ende folgt die Übersicht „DEB-Pokalspiele“ (0x340C7 → 0x32E23).</item>
/// <item>[ORIGINAL] Im Durchlauf ohne Halt (DGROUP 0x1DB ≠ 0, 0x33D60) entfällt die Frage – das Remake hat diesen
///   Durchlauf nicht.</item>
/// </list>
/// </remarks>
public static class DebCupDrawFlow
{
    /// <summary>[ORIGINAL] Mindestzahl der Plätze für Fenster und Frage (Datei 0x33BC3: <c>cmp n, 4; jnc</c>).</summary>
    public const int MinSlotsForWindow = 4;

    /// <summary>
    /// Bildschirm nach dem Ziehen der ersten Runde (Spielstart, Saisonwechsel) oder <paramref name="next"/>, wenn es keinen
    /// DEB-Pokal gibt. Ändert nichts am Spielstand.
    /// </summary>
    public static Screen AtSeasonStart(ScreenHost host, Screen next) =>
        host.Game?.DebCup is { CurrentRound: 0 } cup ? Create(host, 0, cup.Draw.ToList(), next) : next;

    /// <summary>Bildschirm für eine ausgeloste Runde (<see cref="CupDrawStep"/>); danach geht es zu <paramref name="next"/>.</summary>
    public static Screen Create(ScreenHost host, int round, IReadOnlyList<int> slots, Screen next)
    {
        if (host.Game?.DebCup is null || slots.Count == 0)
            return next;
        if (slots.Count < MinSlotsForWindow)
            return new DebCupGamesScreen(host, next);
        return new DebCupDrawLiveScreen(host, round, slots, next);
    }
}

/// <summary>
/// „DEB-Pokal Auslosung“ mit der Frage an alle Manager, ob sie die Auslosung live verfolgen wollen.
/// „Aber klar“: Plätze werden einzeln aufgedeckt, danach „TASTE DRÜCKEN“ und die Übersicht „DEB-Pokalspiele“.
/// „Kein Gedanke“: zurück, ohne Anzeige und ohne Übersicht. Die Paarungen stehen vorher fest
/// (<see cref="CupDrawStep.Slots"/> bzw. <see cref="DebCup.Draw"/>); der Bildschirm ändert nichts am Spielstand.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>[ORIGINAL] Ablauf 0x33B83: Fenster mit leeren Streifen (kb\25) für alle Plätze, dann Frage 0x120:0x7A mit den
///   Texten DGROUP 0x12F1 „Sind Ihre Nerven stark genug,“, 0x12F5 „um die DEB-Pokal-Auslosung“, 0x1329 „live zu verfolgen
///   ?“ und den Knöpfen 0x132D „Aber klar“ / 0x1331 „Kein Gedanke“; Video 3:40–3:46 mit Überschrift „An Alle:“.</item>
/// <item>[APPROX] Lage vermessen am Video 3:46 (<c>oberflaeche_0346</c>): Rahmen wie das Fragefenster der Talentsuche
///   (x 77–240, y 109–181 orange), Texte um x = 157 zentriert, „?“-Felder bei x + 9 / rechts − 30, Knöpfe 1 px höher als
///   dort. Grafik der „?“-Felder nicht gefunden, gezeichnet.</item>
/// <item>[ORIGINAL] Antwort ≠ 0 → live (DGROUP 0xE068 = 1), sonst Fenster schließen ohne Übersicht (0x33DB1 → 0x340C7).</item>
/// <item>[ORIGINAL] Live: Plätze in zufälliger Reihenfolge einzeln aufdecken (0x34521 ff., 0x349E3 ff.), danach „TASTE
///   DRÜCKEN“, Tastendruck, Übersicht „DEB-Pokalspiele“ (0x340C7: 0x34141 ff., 0x34188).</item>
/// <item>[APPROX] Das Original zieht die Aufdeckreihenfolge mit random(0, n − 1) aus dem gemeinsamen Zufallsgenerator; mit
///   und ohne Anzeige laufen die Spiele danach also verschieden. Hier kommt die Reihenfolge aus einer eigenen, aus der Ziehung
///   abgeleiteten Quelle: Paarungen und alle späteren Ergebnisse sind mit und ohne Anzeige gleich.</item>
/// <item>[APPROX] Tempo ein Platz je <see cref="RevealFrames"/> Bilder (Video 3:50–3:53 etwa ein Platz je Sekunde); Klick oder
///   Taste deckt alle übrigen auf (Original: DGROUP 0x5A16, 0x34A88). Die rollende Kugel des Originals fehlt.</item>
/// <item>[APPROX] Tasten im Fragefenster: Enter wie „Aber klar“ (keine Leertaste, Entscheidung Nutzer 31.10.2026, #205), Escape und rechte Maustaste wie „Kein
///   Gedanke“ (Tastenbelegung von 0x120:0x7A nicht geprüft).</item>
/// </list>
/// </remarks>
public sealed class DebCupDrawLiveScreen : Screen
{
    public enum Phase { Ask, Reveal, Done }

    public const string DialogTitle = "An Alle:";
    public static readonly string[] DialogLines = ["Sind Ihre Nerven stark genug,", "um die DEB-Pokal-Auslosung", "live zu verfolgen ?"];
    public const string YesText = "Aber klar";
    public const string NoText = "Kein Gedanke";

    public static Rect DialogBox => TransferDialog.QuestionBox;
    public static Rect YesButton => Widgets.ButtonRect(94, 166);
    public static Rect NoButton => Widgets.ButtonRect(164, 166);

    /// <summary>Bilder je aufgedecktem Platz.</summary>
    public const int RevealFrames = 60;

    private readonly IReadOnlyList<int> _slots;
    private readonly Screen _next;
    private readonly int[] _order;
    private int _frame;

    public DebCupDrawLiveScreen(ScreenHost host, int round, IReadOnlyList<int> slots, Screen next) : base(host)
    {
        Round = round;
        _slots = slots.ToList();
        _next = next;
        _order = RevealOrder(round, _slots);
    }

    /// <summary>Runde der Auslosung (0 = Erste Runde).</summary>
    public int Round { get; }

    public Phase Current { get; private set; } = Phase.Ask;

    /// <summary>Anzahl der schon aufgedeckten Plätze.</summary>
    public int Revealed { get; private set; }

    /// <summary>Plätze der Anzeige: aufgedeckte mit Verein, übrige null (grüner Streifen).</summary>
    public IReadOnlyList<int?> Slots
    {
        get
        {
            var shown = new int?[_slots.Count];
            for (var i = 0; i < Revealed; i++)
                shown[_order[i]] = _slots[_order[i]];
            return shown;
        }
    }

    /// <summary>
    /// [APPROX] Reihenfolge des Aufdeckens: Mischung aus einer eigenen Quelle, die nur von Runde und Ziehung abhängt
    /// (gleiche Bildschirmfotos bei gleicher Ziehung, kein Eingriff in <see cref="GameState.CupRandom"/>).
    /// </summary>
    public static int[] RevealOrder(int round, IReadOnlyList<int> slots)
    {
        var seed = round;
        foreach (var club in slots)
            seed = unchecked(seed * 31 + club + 1);
        var random = new System.Random(seed);
        var order = Enumerable.Range(0, slots.Count).ToArray();
        random.Shuffle(order);
        return order;
    }

    public override void Update(InputState input)
    {
        switch (Current)
        {
            case Phase.Ask:
                if (input.Clicked(NoButton) || input.RightClick || input.Pressed(Key.Escape))
                    Host.Show(_next);
                else if (input.Clicked(YesButton) || input.Pressed(Key.Enter))
                    Current = Phase.Reveal;
                return;

            case Phase.Reveal:
                if (input.LeftClick || input.RightClick || input.Keys.Count > 0)
                    Revealed = _slots.Count;
                else if (++_frame >= RevealFrames)
                {
                    _frame = 0;
                    Revealed++;
                }
                if (Revealed >= _slots.Count)
                    Current = Phase.Done;
                return;

            case Phase.Done:
                if (input.LeftClick || input.RightClick || input.Keys.Count > 0)
                    Host.Show(new DebCupGamesScreen(Host, _next));
                return;
        }
    }

    public override void Draw(Surface s)
    {
        DebCupDrawScreen.DrawBoard(s, Assets, Host.Game, Slots);
        if (Current == Phase.Ask)
            DrawQuestion(s, Assets);
    }

    /// <summary>Fragefenster wie das der Talentsuche: oranger Rahmen, zwei „?“-Felder, Überschrift, drei Zeilen, zwei Knöpfe.</summary>
    private static void DrawQuestion(Surface s, Assets a)
    {
        var b = DialogBox;
        var cx = b.X + b.W / 2 - 2;
        Widgets.Frame(s, a, b, FrameStyle.Orange);
        QuestionMark(s, a, b.X + 9, b.Y + 3);
        QuestionMark(s, a, b.Right - 30, b.Y + 3);
        Widgets.ShadowText(s, a.Flow, cx, b.Y + 12, DialogTitle, Colors.White, Colors.Black);
        s.HLine(b.X + 34, b.Y + 24, b.W - 68, Colors.White);
        for (var i = 0; i < DialogLines.Length; i++)
            TeamUi.LabelCentered(s, a, cx, b.Y + 30 + 8 * i, DialogLines[i], Colors.White, Colors.Black);
        s.HLine(b.X + 5, b.Y + 56, b.W - 10, TeamUi.DialogDark);
        s.HLine(b.X + 5, b.Y + 57, b.W - 10, 37);
        TeamUi.DialogButton(s, a, YesButton.X, YesButton.Y, YesText);
        TeamUi.DialogButton(s, a, NoButton.X, NoButton.Y, NoText);
    }

    /// <summary>
    /// Grünes Feld (22 × 22) mit schwarzem „?“ – wie <c>TransferDialog.QuestionMark</c> (dort privat; Dateigrenze der Spur,
    /// daher hier wiederholt).
    /// </summary>
    private static void QuestionMark(Surface s, Assets a, int x, int y)
    {
        var r = new Rect(x, y, 22, 22);
        s.Fill(r, 14);
        s.Outline(r, 12);
        s.HLine(x + 1, y + 1, 20, 15);
        s.VLine(x + 1, y + 1, 20, 15);
        s.TextCentered(a.Bold, x + 11, y + 4, "?", Colors.Black);
    }
}
