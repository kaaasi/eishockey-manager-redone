using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens.Team;

/// <summary>
/// Training (Hauptmenü → Mannschaft → Training), Hintergrund kb\5. Aufbau und Bedienung nach em.exe
/// (Overlay-Funktion ab Datei 0x59DBF) und dem UI-Video (ca. 11:10–12:40):
/// oben „Kosten pro Woche:“, darunter der Kasten E K T mit den Balken je Gruppe (Torhüter, Abwehr, Sturm),
/// je Wochentag zwei Kästen mit je zwei Einheiten, unten die Leiste der neun Trainingsarten.
/// </summary>
/// <remarks>
/// Bedienung wie im Original: Art in der Leiste anklicken (gelb unterstrichen), dann Einheiten anklicken oder mit
/// gedrückter Taste überstreichen. Rechtsklick oder Esc = zurück. Die Balken werden wie im Original nur beim
/// Öffnen berechnet.
/// </remarks>
public sealed class TrainingScreen : Screen
{
    public const int Background = 5;

    /// <summary>Bildchen einer Art in der Leiste (zugleich Quelle für die Einheiten). [ORIGINAL]</summary>
    public static Rect KindIcon(TrainingKind kind) => new(26 + 30 * (int)kind, 218, 29, 20);

    /// <summary>Klickfläche der Leiste: x 26–293, y 215–237. [ORIGINAL]</summary>
    public static readonly Rect KindBar = new(26, 215, 268, 23);

    /// <summary>Unterstreichung der gewählten Art. [ORIGINAL] y = 238, Farbe 24.</summary>
    public const int UnderlineY = 238;

    public const byte UnderlineColor = 24;

    private static readonly int[] UnitY = [103, 125, 168, 190];

    /// <summary>Lage einer Einheit (Bildchen 29 × 20). [ORIGINAL] x = 14 + 44·Tag.</summary>
    public static Rect Unit(int day, int unit) => new(14 + 44 * day, UnitY[unit], 29, 20);

    /// <summary>Klickfläche einer Einheit (24 × 19). [ORIGINAL]</summary>
    public static Rect UnitHit(int day, int unit) => new(14 + 44 * day, UnitY[unit], 24, 19);

    /// <summary>Balken: linke Spalte je Wert und Gruppe, Unterkante y = 93, höchstens 36 px. [ORIGINAL]</summary>
    public static int BarX(int value, int group) => 148 + 9 * value + 2 * group;

    public const int BarBottom = 93;

    /// <summary>Farben der Balken je Wert (zwei Spalten). [ORIGINAL] DGROUP 0x5544: E rot, K blau, T grün.</summary>
    public static readonly byte[][] BarColors = [[17, 18], [22, 23], [12, 15]];

    /// <summary>Zeile „Kosten pro Woche:“ – mittig, weiß mit dunklem Schatten. [ORIGINAL] Text und Farben 7/6.</summary>
    public const int CostY = 37;

    /// <summary>Mitte der Kostenzeile (Original: zentriert zwischen x = 0 und 319; Video: x = 159).</summary>
    public const int CostCenterX = 159;

    private readonly Screen _back;
    private TrainingPlan _plan = new();
    private bool _pressed;

    public TrainingScreen(ScreenHost host, Screen back) : base(host) => _back = back;

    /// <summary>Gewählte Art (zu Beginn wie im Original die erste).</summary>
    public TrainingKind Selected { get; private set; } = TrainingKind.Special;

    public TrainingPlan Plan => _plan;

    public override void Enter()
    {
        var game = Host.Game!;
        _plan = game.PrepareTraining(game.ActiveManager);
        _pressed = false;
    }

    public override void Update(InputState input)
    {
        if (input.Pressed(Key.Escape) || input.RightClick)
        {
            Host.Show(_back);
            return;
        }
        // Nur Drücken auf diesem Bildschirm zählt (nicht die noch gehaltene Taste vom Menü).
        if (input.LeftClick)
            _pressed = true;
        else if (!input.LeftDown)
            _pressed = false;
        if (!_pressed)
            return;

        var x = input.MouseX;
        var y = input.MouseY;
        if (KindBar.Contains(x, y))
        {
            Selected = (TrainingKind)Math.Min((x - 26) / 30, TrainingPlan.Kinds - 1);
            return;
        }
        for (var day = 0; day < TrainingPlan.Days; day++)
            for (var unit = 0; unit < TrainingPlan.UnitsPerDay; unit++)
                if (UnitHit(day, unit).Contains(x, y))
                    _plan[day, unit] = Selected;
    }

    public override void Draw(Surface s)
    {
        s.Palette = Assets.Palette;
        var image = Assets.Image(Background);
        s.Blit(image, 0, 0);

        Widgets.ShadowText(s, Assets.Mini, CostCenterX, CostY, "Kosten pro Woche:" + _plan.WeeklyCost + " DM", Colors.White, 6);

        var players = TeamUi.OwnClub(Host).Players;
        for (var group = 0; group < 3; group++)
        {
            var (e, k, t) = TrainingRules.Bars(players, group);
            int[] heights = [e, k, t];
            for (var value = 0; value < 3; value++)
            {
                var h = heights[value];
                if (h <= 0)
                    continue;
                for (var column = 0; column < 2; column++)
                    s.VLine(BarX(value, group) + column, BarBottom + 1 - h, h, BarColors[value][column]);
            }
        }

        for (var day = 0; day < TrainingPlan.Days; day++)
        {
            for (var unit = 0; unit < TrainingPlan.UnitsPerDay; unit++)
            {
                var r = Unit(day, unit);
                s.Blit(image, KindIcon(_plan[day, unit]), r.X, r.Y);
            }
        }

        var selected = KindIcon(Selected);
        s.HLine(selected.X, UnderlineY, selected.W, UnderlineColor);
    }
}

internal static class TrainingMenuEntries
{
    [MenuEntry("Training")]
    private static Screen Open(ScreenHost host, Screen back) => new TrainingScreen(host, back);
}
