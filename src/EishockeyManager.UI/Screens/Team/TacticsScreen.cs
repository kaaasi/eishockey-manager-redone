using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens.Team;

/// <summary>
/// Taktik (Hauptmenü → Mannschaft → Taktik oder aus der Aufstellung unten rechts). Vermessen aus dem UI-Video
/// (ca. 9:20–10:10) per Template-Matching der Grafiken aus kb\2, kb\4 und kb\9:
/// oben links Torwartwechsel, links Linienwechsel mit Minuten je Reihe, rechts Einsatz (Gesicht) und Angriff
/// (Trompete) je Block und Spielstand.
/// </summary>
/// <remarks>
/// Bedienung: Pfeil oben = nächster Wert bzw. Modus, unten = voriger; Reiter 1–4 wählen den Block.
/// Felder sind nur aktiv, wenn der zugehörige Modus es erlaubt (Minuten nur bei „Minuten-Vorgabe“,
/// Einsatz/Angriff nicht bei „AUTOM.“). [APPROX] Richtung der Pfeile bei den Modi.
/// </remarks>
public sealed class TacticsScreen : Screen
{
    public static readonly Rect GoaliePanel = new(9, 33, 120, 38);
    public static readonly Rect LinePanel = new(9, 84, 120, 133);
    public static readonly Rect BlockPanel = new(145, 21, 168, 190);

    public static readonly Rect GoalieButton = new(71, 43, 40, 16);
    public static readonly Rect LineChangeButton = new(13, 117, 97, 16);
    public static readonly Rect EffortButton = new(152, 81, 44, 16);
    public static readonly Rect AttackButton = new(248, 81, 44, 16);

    /// <summary>Pfeilpaare (17 hoch) neben den Modus-Knöpfen.</summary>
    public static readonly Rect GoalieArrows = new(115, 43, TeamUi.ArrowWidth, TeamUi.ArrowHeight);
    public static readonly Rect LineChangeArrows = new(114, 117, TeamUi.ArrowWidth, TeamUi.ArrowHeight);
    public static readonly Rect EffortArrows = new(200, 81, TeamUi.ArrowWidth, TeamUi.ArrowHeight);
    public static readonly Rect AttackArrows = new(296, 81, TeamUi.ArrowWidth, TeamUi.ArrowHeight);

    public const int FieldWidth = 23;
    public const int FieldHeight = 12;
    public const int RowPitch = 16;

    /// <summary>Reiter der Blöcke 1–4.</summary>
    public static Rect BlockTab(int block) => new(182 + 25 * block, 36, 21, 12);

    /// <summary>Minutenfeld einer Reihe (0–3), Abwehr links, Sturm rechts.</summary>
    public static Rect MinutesField(int line, bool forwards) => new(forwards ? 88 : 12, 146 + RowPitch * line, FieldWidth, FieldHeight);

    public static Rect MinutesArrows(int line, bool forwards) =>
        new(forwards ? 114 : 38, 145 + RowPitch * line, TeamUi.ArrowWidth, TeamUi.CompactArrowHeight);

    /// <summary>Feld eines Spielstands, Einsatz links, Angriff rechts.</summary>
    public static Rect SituationField(ScoreSituation situation, bool attack) =>
        new(attack ? 271 : 151, 103 + RowPitch * (int)situation, FieldWidth, FieldHeight);

    public static Rect SituationArrows(ScoreSituation situation, bool attack) =>
        new(attack ? 296 : 177, 102 + RowPitch * (int)situation, TeamUi.ArrowWidth, TeamUi.CompactArrowHeight);

    private readonly Screen _back;
    private int _block;

    public TacticsScreen(ScreenHost host, Screen back) : base(host) => _back = back;

    private Tactics Tactics => TeamUi.OwnClub(Host).Tactics;

    /// <summary>Gewählter Block (0–3).</summary>
    public int Block => _block;

    public override void Update(InputState input)
    {
        if (input.Pressed(Key.Escape) || input.RightClick)
        {
            Host.Show(_back);
            return;
        }
        if (!input.LeftClick)
            return;

        var x = input.MouseX;
        var y = input.MouseY;
        var t = Tactics;

        if (TeamUi.SwitchButton.Contains(x, y))
        {
            if (MenuEntries.TryCreate("Aufstellung", Host, _back, out var lineup))
                Host.Show(lineup!);
            return;
        }

        for (var b = 0; b < Tactics.Blocks; b++)
            if (BlockTab(b).Contains(x, y))
                _block = b;

        if (Step(GoalieArrows, x, y) is { } g)
            t.Goalie = Cycle(t.Goalie, g);
        if (Step(LineChangeArrows, x, y) is { } l)
            t.LineChange = Cycle(t.LineChange, l);
        if (Step(EffortArrows, x, y) is { } e)
            t.EffortMode = Cycle(t.EffortMode, e);
        if (Step(AttackArrows, x, y) is { } a)
            t.AttackMode = Cycle(t.AttackMode, a);

        if (t.MinutesActive)
        {
            for (var line = 0; line < Lineup.Blocks; line++)
            {
                foreach (var forwards in new[] { false, true })
                    if (Step(MinutesArrows(line, forwards), x, y) is { } d)
                        t.SetMinutes(line, forwards, t.Minutes(line, forwards) + d);
            }
        }

        foreach (var situation in Enum.GetValues<ScoreSituation>())
        {
            if (t.EffortMode != TacticMode.Auto && Step(SituationArrows(situation, false), x, y) is { } de)
                t.SetEffort(_block, situation, t.Effort(_block, situation) + de);
            if (t.AttackMode != TacticMode.Auto && Step(SituationArrows(situation, true), x, y) is { } da)
                t.SetAttack(_block, situation, t.Attack(_block, situation) + da);
        }
    }

    /// <summary>+1 für die obere, −1 für die untere Hälfte eines Pfeilpaars, sonst null.</summary>
    private static int? Step(Rect arrows, int x, int y) =>
        arrows.Contains(x, y) ? (y < arrows.Y + arrows.H / 2 ? 1 : -1) : null;

    private static T Cycle<T>(T value, int delta) where T : struct, Enum
    {
        var values = Enum.GetValues<T>();
        var i = Array.IndexOf(values, value);
        return values[(i + delta + values.Length) % values.Length];
    }

    public override void Draw(Surface s)
    {
        var t = Tactics;
        s.Palette = Assets.Palette;
        s.Blit(Assets.Image(MainMenuScreen.Background), 0, 0);

        // Torwartwechsel
        Widgets.Frame(s, Assets, GoaliePanel, FrameStyle.Orange);
        s.Blit(Assets.Image(9), new Rect(0, 0, 57, 24), 13, 39);
        ModeButton(s, GoalieButton, Tactics.ModeText(t.Goalie));
        TeamUi.Arrows(s, Assets, GoalieArrows.X, GoalieArrows.Y);

        // Linienwechsel
        Widgets.Frame(s, Assets, LinePanel, FrameStyle.Orange);
        s.Blit(Assets.Image(9), new Rect(57, 0, 57, 24), 41, 90);
        ModeButton(s, LineChangeButton, Tactics.LineChangeText(t.LineChange));
        TeamUi.Arrows(s, Assets, LineChangeArrows.X, LineChangeArrows.Y);
        s.Text(Assets.Mini, 17, 138, "Abwehr", Colors.White); // Video: ohne Schatten
        s.Text(Assets.Mini, 95, 138, "Sturm", Colors.White);
        for (var line = 0; line < Lineup.Blocks; line++)
        {
            TeamUi.LabelCentered(s, Assets, 67, 150 + RowPitch * line, $"REIHE {line + 1}");
            foreach (var forwards in new[] { false, true })
            {
                ValueField(s, MinutesField(line, forwards), t.Minutes(line, forwards), t.MinutesActive);
                var a = MinutesArrows(line, forwards);
                TeamUi.CompactArrows(s, Assets, a.X, a.Y);
            }
        }

        // Einsatz und Angriff je Block
        Widgets.Frame(s, Assets, BlockPanel, FrameStyle.Orange);
        for (var b = 0; b < Tactics.Blocks; b++)
        {
            var r = BlockTab(b);
            TeamUi.StretchedButton(s, Assets, r);
            var label = (b + 1).ToString();
            if (b == _block)
                s.TextCentered(Assets.Mini, r.X + r.W / 2 + 2, r.Y + 3, label, Colors.White);
            else
                TeamUi.LabelCentered(s, Assets, r.X + r.W / 2 + 2, r.Y + 3, label, TeamUi.TabTextColor, TeamUi.TabTextShadow);
        }
        s.Blit(Assets.Image(9), new Rect(114, 0, 57, 24), 152, 54);
        s.Blit(Assets.Image(9), new Rect(171, 0, 57, 24), 248, 54);
        ModeButton(s, EffortButton, Tactics.ModeText(t.EffortMode));
        ModeButton(s, AttackButton, Tactics.ModeText(t.AttackMode));
        TeamUi.Arrows(s, Assets, EffortArrows.X, EffortArrows.Y);
        TeamUi.Arrows(s, Assets, AttackArrows.X, AttackArrows.Y);
        foreach (var situation in Enum.GetValues<ScoreSituation>())
        {
            var row = (int)situation;
            TeamUi.LabelCentered(s, Assets, 228, 107 + RowPitch * row, Tactics.SituationText(situation));
            ValueField(s, SituationField(situation, false), t.Effort(_block, situation), t.EffortMode != TacticMode.Auto);
            ValueField(s, SituationField(situation, true), t.Attack(_block, situation), t.AttackMode != TacticMode.Auto);
            var e = SituationArrows(situation, false);
            var a = SituationArrows(situation, true);
            TeamUi.CompactArrows(s, Assets, e.X, e.Y);
            TeamUi.CompactArrows(s, Assets, a.X, a.Y);
        }

        TeamUi.DrawSwitch(s, Assets, toTactics: false);
    }

    private void ModeButton(Surface s, Rect r, string text)
    {
        TeamUi.StretchedButton(s, Assets, r);
        TeamUi.LabelCentered(s, Assets, r.X + r.W / 2 + 1, r.Y + 6, text, TeamUi.ButtonTextColor, TeamUi.ButtonTextShadow);
    }

    private void ValueField(Surface s, Rect r, int value, bool active)
    {
        TeamUi.StretchedButton(s, Assets, r, pressed: true);
        // Video: „0“ steht 3 px rechts der Mitte → rechtsbündig bei Right − 6
        s.TextRight(Assets.Mini, r.Right - 6, r.Y + 4, value.ToString(), active ? TeamUi.ActiveValueColor : TeamUi.InactiveValueColor);
    }
}

/// <summary>Anmeldung im Hauptmenü.</summary>
internal static class TacticsMenuEntries
{
    [MenuEntry("Taktik")]
    private static Screen Open(ScreenHost host, Screen back) => new TacticsScreen(host, back);
}
