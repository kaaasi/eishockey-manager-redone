using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens.Team;

/// <summary>
/// Spielerinfo (Klick auf einen Namen in der Transferliste). Vermessen aus dem UI-Video (ca. 11:10, „HANFT“):
/// oranger Kopf mit Name, Länderspielen, Alter, Stärke und Gesicht (kb\10), darunter Tore/Spiele je Wettbewerb,
/// Daten (Kaufpreis, Marktwert, Versicherung, Sperren, Gehalt, Vertrag) und vier Balken
/// (Unterzahl, Überzahl, Tendenz, Erschöpfung); Knopf „Aha !“ schließt.
/// </summary>
/// <remarks>
/// Das Fenster ist die Funktion 0x041AC („Spielerdaten“, Spur spielerinfo); Felder und Regeln in
/// <see cref="PlayerDataWindow"/>. Unterzahl und Überzahl sind die verdeckten Fähigkeiten des Kaderplatzes
/// (<see cref="Player.HiddenSkill1"/>/<see cref="Player.HiddenSkill2"/>, A371; [ORIGINAL] Datei 0x4B09–0x4B52).
/// </remarks>
public sealed class PlayerInfoPopup
{
    /// <summary>Lage im Video (Template-Fit); alle Teile sind relativ dazu angegeben.</summary>
    public static readonly Rect Box = new(89, 55, 216, 171);

    private static int X(int dx) => Box.X + dx;
    private static int Y(int dy) => Box.Y + dy;

    public static readonly Rect Header = new(X(10), Y(6), 195, 40);
    public static readonly Rect AhaButton = new(X(79), Y(153), Widgets.ButtonWidth, Widgets.ButtonHeight);

    public const int BarWidth = 100;
    public const int BarHeight = 12;

    /// <summary>
    /// Balken 0–3 (Unterzahl, Überzahl, Tendenz, Erschöpfung). [ORIGINAL] em.exe 0x041AC, Aufrufe 0x985/0x9CA/0xA0D/0xA5B: links
    /// Fensterursprung + 7, rechts + 0x6C (= 108), Zeilenabstand 0x13. Bisher rechts + 110.
    /// </summary>
    public static Rect Bar(int index) => new(X(7 + 101 * (index % 2)), Y(120 + 19 * (index / 2)), BarWidth, BarHeight);

    /// <summary>Farbstufen der Balken von schlecht (dunkelrot) nach gut (dunkelgrün), je 12 px.</summary>
    public static readonly byte[] BarColors = [16, 17, 18, 19, 15, 14, 13, 12];

    private static readonly string[] BarLabels = ["Unterzahl", "Überzahl", "Tendenz", "Erschöpfung"];

    private Player? _player;
    private long _price;
    private PlayerDataContext _context = PlayerDataContext.Unknown;

    public bool Visible => _player is not null;
    public Player? Player => _player;

    /// <summary>Betrag im Feld „Kaufpreis“ des offenen Fensters.</summary>
    public long Price => _price;

    /// <summary>
    /// Öffnet das Fenster. Aufruf stabil (<c>TransferScreen</c>); ohne Zusammenhang bleibt der Marktwert „-“.
    /// </summary>
    public void Show(Player player, long price) => Show(player, price, PlayerDataContext.Unknown);

    /// <summary>Öffnet das Fenster mit Marktwert und Sichtbarkeitsregeln (<see cref="PlayerDataContext.Create"/>).</summary>
    public void Show(Player player, long price, PlayerDataContext context)
    {
        _player = player;
        _price = price;
        _context = context;
    }

    public void Close() => _player = null;

    /// <summary>Liefert true, solange das Fenster offen ist (Eingaben verbraucht).</summary>
    public bool Update(InputState input)
    {
        if (_player is null)
            return false;
        if (input.Clicked(AhaButton) || input.RightClick || input.Pressed(Key.Escape) || input.Pressed(Key.Enter))
            Close();
        return true;
    }

    /// <summary>
    /// Zeigerlänge der Erschöpfung in Pixeln. [ORIGINAL] em.exe Datei 0x041AC, Versätze 0xA1C–0xA60 (Aufruf der Balkenroutine 0xAC9 bei 0xA63):
    /// (100 − Belastung +0x13) · 94 / 100, im Balken auf 1 … 94 begrenzt (0xBBA–0xBC7); frisch (50) → 47, ab 99 → 1 (#805).
    /// </summary>
    public static int ExhaustionBarPixels(Player p) => PlayerDataWindow.BarPixels((100 - p.Strain) * 94 / 100);

    /// <summary>
    /// Zeigerlänge von Unterzahl und Überzahl in Pixeln. [ORIGINAL] A371, em.exe Datei 0x4B09–0x4B23 und 0x4B4A–0x4B52: Halbbyte
    /// (0–15) von Kaderplatz +0x19 · 94 / 15 (<see cref="Player.HiddenSkill1"/> unten, <see cref="Player.HiddenSkill2"/> oben), im
    /// Balken auf 1 … 94 begrenzt. Spieler ohne Kaderplatz zeigen den Platzhalter 7 (Vorgabe 7, kein Original-Fall).
    /// </summary>
    public static int HiddenSkillBarPixels(int skill) => PlayerDataWindow.BarPixels(skill * 94 / 15);

    /// <summary>Zeigerlängen der vier Balken in Pixeln (1–94).</summary>
    public static int[] BarPixelLengths(Player p)
    {
        var values = BarValues(p);
        return
        [
            HiddenSkillBarPixels(p.HiddenSkill1),
            HiddenSkillBarPixels(p.HiddenSkill2),
            PlayerDataWindow.BarPixelsFromPercent(values[2]),
            ExhaustionBarPixels(p),
        ];
    }

    /// <summary>Werte der Balken Unterzahl, Überzahl und Tendenz (0–99) auf der Skala des Remakes.</summary>
    public static int[] BarValues(Player p) =>
    [
        p.HiddenSkill1 * 99 / 15, // [ORIGINAL] A371: Halbbyte +0x19 (siehe HiddenSkillBarPixels)
        p.HiddenSkill2 * 99 / 15,
        // [ORIGINAL] Balken (+0x0E − 30) · 100 / 38 (0x041AC); [APPROX] ohne Wert die Stufe
        p.TrendScore != 0
            ? Math.Clamp((p.TrendScore - 30) * 100 / 38, 0, 99)
            : p.Trend switch { PlayerTrend.Rising => 84, PlayerTrend.Falling => 15, _ => 50 },
    ];

    public void Draw(Surface s, Assets a)
    {
        if (_player is not { } p)
            return;
        var mini = a.Mini;
        Widgets.Frame(s, a, Box, FrameStyle.Orange);

        // Kopf: Name in der Titelschrift, dunkelrot mit orangem Schatten (Video)
        const byte red = 16, redShadow = 9;
        s.Fill(Header, 19);
        s.Outline(Header, 27);
        s.Outline(Header.Inflate(-1), 21);
        Widgets.ShadowText(s, a.Flow, X(92), Y(13), p.Name, red, redShadow);
        TeamUi.LabelCentered(s, a, X(92), Y(27), $"Länderspiele:{p.Caps}", red, redShadow);
        TeamUi.Label(s, a, X(24), Y(35), $"Alter:{p.Age}", red, redShadow);
        var strength = $"Stärke:{p.Strength}";
        TeamUi.Label(s, a, X(158) - mini.Measure(strength), Y(35), strength, red, redShadow);
        var face = new Rect(X(164), Y(11), 24, 28);
        s.Fill(face, 27);
        s.Outline(face, 10);
        s.Blit(a.Image(10), Face(p), face.X + 3, face.Y + 4); // [ORIGINAL] 0x041AC 0x368–0x3C3: Ziel (Ursprung + 0xA7 | + 0x0F)

        // Tore / Spiele: Torhüter nur Liga, Beschriftung „GTore:“ (0x041AC 0x45D–0x570)
        const byte head = Colors.White, headShadow = 5, label = TeamUi.DimColor, value = TeamUi.DimColor;
        var columnCount = PlayerDataWindow.CompetitionColumns(p);
        int[] centers = [X(58), X(117), X(177)];
        for (var i = 0; i < 3; i++)
            TeamUi.LabelCentered(s, a, centers[i], Y(52), PlayerDataWindow.CompetitionHeads[i], head, headShadow);
        s.Text(mini, X(7), Y(60), PlayerDataWindow.GoalsLabel(p), label);
        s.Text(mini, X(7), Y(67), "Spiele:", label);
        for (var i = 0; i < columnCount; i++)
        {
            s.TextRight(mini, centers[i] + 24, Y(60), PlayerDataWindow.CompetitionCell(p, i, goals: true), value);
            s.TextRight(mini, centers[i] + 24, Y(67), PlayerDataWindow.CompetitionCell(p, i, goals: false), value);
        }

        // Daten
        s.Text(mini, X(7), Y(79), "Daten:", label);
        var dataValues = PlayerDataWindow.DataValues(p, _price, _context);
        for (var i = 0; i < 6; i++)
        {
            var y = i < 3 ? Y(79) : Y(95);
            TeamUi.LabelCentered(s, a, centers[i % 3], y, PlayerDataWindow.DataHeads[i], head, headShadow);
            s.TextCentered(mini, centers[i % 3], y + 7, dataValues[i], value);
        }

        // Balken
        var lengths = BarPixelLengths(p);
        for (var i = 0; i < 4; i++)
        {
            var r = Bar(i);
            // [ORIGINAL] Beschriftung zentriert um x + 0x2F, Grundlinie y + 1 = Balken − 2, Farbe 1, ohne Schatten (Datei 0x04C83–0x04CA8)
            Competitions.CompetitionWidgets.Centered(s, mini, r.X + 0x2F, r.X + 0x2F, r.Y - 2, BarLabels[i], BarFrameColor, null);
            DrawBarPixels(s, r, lengths[i]);
        }

        TeamUi.DialogButton(s, a, AhaButton.X, AhaButton.Y, "Aha !");
    }

    /// <summary>
    /// Balken 0x04C75 (Aufrufe aus 0x041AC mit x = Fenster + 7 bzw. + 0x6C und y = <see cref="Bar"/>.Y − 3). [ORIGINAL] Datei
    /// 0x04CB0–0x04DAD (Abweichung B33/A372): Fläche (x + 1 | y + 3)–(x + 99 | y + 13) Farbe 1, darin (x + 2 | y + 4)–(x + 98 | y + 12)
    /// Farbe 0, acht Farbstufen i (x + 2 + 12 i | y + 6)–(x + 14 + 12 i | y + 10) in Farbe DGROUP 0x482A[7 − i]
    /// (<see cref="BarColors"/>), Zeiger als Linie (x + 1 + Länge | y + 4)–(x + 1 + Länge | y + 12) in der Farbe seines Abschnitts
    /// (Länge 1–94, Datei 0x04D66–0x04DA8). Vorher am Video geschätzt: Rand 2/3/197, Farbband 6 Zeilen.
    /// </summary>
    public static void DrawBar(Surface s, Rect r, int value) => DrawBarPixels(s, r, PlayerDataWindow.BarPixelsFromPercent(value));

    /// <summary>Balken wie <see cref="DrawBar"/> mit der Zeigerlänge in Pixeln (auf 1–94 begrenzt, 0xBBA–0xBC7).</summary>
    public static void DrawBarPixels(Surface s, Rect r, int pixels)
    {
        s.Fill(new Rect(r.X + 1, r.Y, 99, 11), BarFrameColor);
        s.Fill(new Rect(r.X + 2, r.Y + 1, 97, 9), Colors.Black);
        const int step = 12;
        for (var i = 0; i < BarColors.Length; i++)
            s.Fill(new Rect(r.X + 2 + step * i, r.Y + 3, step + 1, 5), BarColors[i]);
        var px = PlayerDataWindow.BarPixels(pixels);
        s.VLine(r.X + 1 + px, r.Y + 1, 9, BarColors[px / step]);
    }

    /// <summary>[ORIGINAL] Farbe der äußeren Fläche und der Beschriftung (Datei 0x04C86, 0x04CB0).</summary>
    public const byte BarFrameColor = 1;

    /// <summary>Gesicht aus kb\10 (4 × 10 Köpfe à 17 × 21): Spalte nach Alter, Zeile nach Talent, siehe <see cref="PlayerDataWindow.Face"/>.</summary>
    public static Rect Face(Player p) => PlayerDataWindow.Face(p);
}
