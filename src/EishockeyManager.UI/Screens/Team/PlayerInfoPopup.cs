using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens.Team;

/// <summary>
/// Spielerinfo (Klick auf einen Namen in der Transferliste). Vermessen aus dem UI-Video (ca. 11:10, „HANFT“):
/// oranger Kopf mit Name, Länderspielen, Alter, Stärke und Gesicht (kb\10), darunter Tore/Spiele je Wettbewerb,
/// Daten (Kaufpreis, Marktwert, Versicherung, Sperren, Gehalt, Vertrag) und vier Balken
/// (Unterzahl, Überzahl, Tendenz, Erschöpfung); Knopf „Aha !“ schließt.
/// </summary>
/// <remarks>
/// [APPROX] Unterzahl/Überzahl gibt es in den Spielerdaten des Remakes nicht; sie werden aus Technik bzw.
/// Kondition und Technik abgeleitet. Tore/Spiele nur Liga (Pokale folgen mit dem Paket „wettbewerbe“).
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

    /// <summary>Balken 0–3 (Unterzahl, Überzahl, Tendenz, Erschöpfung).</summary>
    public static Rect Bar(int index) => new(X(7 + 103 * (index % 2)), Y(120 + 19 * (index / 2)), BarWidth, BarHeight);

    /// <summary>Farbstufen der Balken von schlecht (dunkelrot) nach gut (dunkelgrün), je 12 px.</summary>
    public static readonly byte[] BarColors = [16, 17, 18, 19, 15, 14, 13, 12];

    private static readonly string[] BarLabels = ["Unterzahl", "Überzahl", "Tendenz", "Erschöpfung"];

    private Player? _player;
    private long _price;

    public bool Visible => _player is not null;
    public Player? Player => _player;

    public void Show(Player player, long price)
    {
        _player = player;
        _price = price;
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

    /// <summary>Werte der vier Balken (0–99).</summary>
    public static int[] BarValues(Player p) =>
    [
        p.Technique,
        (p.Condition + p.Technique) / 2,
        p.Trend switch { PlayerTrend.Rising => 80, PlayerTrend.Falling => 20, _ => 50 },
        100 - Math.Clamp(p.Energy, 0, 100),
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
        s.Blit(a.Image(10), Face(p), face.X + 3, face.Y + 3);

        // Tore / Spiele
        const byte head = Colors.White, headShadow = 5, label = TeamUi.DimColor, value = TeamUi.DimColor;
        string[] columns = ["Liga", "DEB-Pokal", "Europacup"];
        int[] centers = [X(58), X(117), X(177)];
        for (var i = 0; i < 3; i++)
            TeamUi.LabelCentered(s, a, centers[i], Y(52), columns[i], head, headShadow);
        s.Text(mini, X(7), Y(60), "Tore:", label);
        s.Text(mini, X(7), Y(67), "Spiele:", label);
        for (var i = 0; i < 3; i++)
        {
            var league = i == 0 && p.Games > 0;
            s.TextRight(mini, centers[i] + 24, Y(60), league ? $"{p.Goals}( -)" : "-( -)", value);
            s.TextRight(mini, centers[i] + 24, Y(67), league ? $"{p.Games}( -)" : "-( -)", value);
        }

        // Daten
        s.Text(mini, X(7), Y(79), "Daten:", label);
        string[] dataHeads = ["Kaufpreis", "Marktwert", "Versicherung", "Sperren in Min.", "Gehalt/M.", "Vertrag"];
        string[] dataValues =
        [
            _price > 0 ? TransferScreen.Money(_price) : "-",
            "-",
            p.Insured ? "ja" : "keine",
            p.SuspendedGames > 0 ? p.SuspendedGames.ToString() : "-",
            p.MonthlySalary > 0 ? TransferScreen.Money(p.MonthlySalary) : "-",
            p.ContractYears > 0 ? Player.YearsText(p.ContractYears) : "-",
        ];
        for (var i = 0; i < 6; i++)
        {
            var y = i < 3 ? Y(79) : Y(95);
            TeamUi.LabelCentered(s, a, centers[i % 3], y, dataHeads[i], head, headShadow);
            s.TextCentered(mini, centers[i % 3], y + 7, dataValues[i], value);
        }

        // Balken
        var values = BarValues(p);
        for (var i = 0; i < 4; i++)
        {
            var r = Bar(i);
            s.TextCentered(mini, r.X + r.W / 2, r.Y - 6, BarLabels[i], 35);
            DrawBar(s, r, values[i]);
        }

        TeamUi.DialogButton(s, a, AhaButton.X, AhaButton.Y, "Aha !");
    }

    /// <summary>
    /// Balken wie im Video: heller Außenrand (2/3), schwarzer Innenrand, acht Farbstufen à 12 px über 6 Zeilen,
    /// Markierung beim Wert (0–99). [APPROX] Farbe der Markierung.
    /// </summary>
    public static void DrawBar(Surface s, Rect r, int value)
    {
        s.Fill(r, Colors.Black);
        s.HLine(r.X, r.Y, r.W, 2);
        s.HLine(r.X, r.Y + r.H - 1, r.W, 2);
        s.HLine(r.X, r.Y + r.H - 2, r.W, 3);
        s.VLine(r.X, r.Y, r.H, 3);
        s.VLine(r.X + 1, r.Y + 1, r.H - 3, 2);
        s.VLine(r.X + r.W - 1, r.Y + 1, r.H - 2, 197);
        const int step = 12;
        var x0 = r.X + 2;
        for (var i = 0; i < BarColors.Length; i++)
            s.Fill(new Rect(x0 + step * i, r.Y + 3, step, 6), BarColors[i]);
        var x = x0 + Math.Clamp(value, 0, 99) * (step * BarColors.Length - 1) / 99;
        s.VLine(x, r.Y + 3, 6, 1);
    }

    /// <summary>Gesicht aus kb\10 (4 × 10 Köpfe à 17 × 21). [APPROX] Zuordnung über den Namen.</summary>
    public static Rect Face(Player p)
    {
        var hash = 0;
        foreach (var ch in p.Name)
            hash = unchecked(hash * 31 + ch);
        var i = (int)((uint)hash % 40);
        return new Rect(i % 4 * 17, i / 4 * 21, 17, 21);
    }
}
