using EishockeyManager.UI.Screens.Team;

namespace EishockeyManager.UI.Screens;

/// <summary>
/// Bildschirm „Einstellungen“ (Kachel unter „Gemischtes“): Lautstärke Effekte und Musik, Musik auch im Spiel,
/// Torszenen, Vollbild, automatisch speichern. Jede Änderung gilt sofort und wird gespeichert.
/// </summary>
/// <remarks>
/// [APPROX] Das Original hat keinen Einstellungen-Bildschirm (im Video der Oberfläche nicht vorhanden, der Ton
/// wird dort beim Start eingerichtet). Das Layout ist neu, aber aus den Bausteinen des Originals gebaut:
/// Fenster wie die Hinweise (Rahmen kb\2 orange auf Hintergrund kb\22), Pfeile ▲▼ aus kb\4 wie bei der Taktik,
/// Lampen aus kb\3 (grün = an, rot = aus), Knopf aus kb\2.
/// </remarks>
public sealed class SettingsScreen : Screen
{
    public static readonly Rect Window = new(24, 36, 272, 172);

    private const int LabelX = 44;
    private const int FirstRowY = 73;
    private const int RowPitch = 17;
    private const int BarX = 168;
    private const int CellWidth = 7;
    private const int CellPitch = 8;
    private const int CellHeight = 7;
    private const int ArrowsX = 252;
    private const int ValueX = 272;
    private const int LampX = 252;

    public enum Row
    {
        EffectsVolume,
        MusicVolume,
        MusicInMatch,
        GoalScenes,
        Fullscreen,
        AutoSave,
    }

    private static readonly string[] Labels =
        ["Lautstärke Effekte", "Lautstärke Musik", "Musik auch im Spiel", "Torszenen zeigen", "Vollbild", "Automatisch speichern"];

    /// <summary>Zeile i (ganze Breite, zum Umschalten der Lampen).</summary>
    public static Rect RowRect(Row row) => new(LabelX - 4, FirstRowY + RowPitch * (int)row - 5, 236, 15);

    /// <summary>Pfeile ▲▼ einer Lautstärkezeile; obere Hälfte lauter, untere leiser.</summary>
    public static Rect ArrowRect(Row row) =>
        new(ArrowsX, FirstRowY + RowPitch * (int)row - 6, TeamUi.ArrowWidth, TeamUi.ArrowHeight);

    /// <summary>Zelle 1–10 des Lautstärkebalkens (Klick setzt die Lautstärke direkt).</summary>
    public static Rect CellRect(Row row, int level) =>
        new(BarX + (level - 1) * CellPitch, FirstRowY + RowPitch * (int)row - 1, CellWidth, CellHeight);

    public static readonly Rect BackButton = Widgets.ButtonRect(160 - Widgets.ButtonWidth / 2, 188);

    private readonly Screen _back;

    public SettingsScreen(ScreenHost host, Screen back) : base(host) => _back = back;

    private static GameSettings Settings => GameSettings.Current;

    public override void Update(InputState input)
    {
        if (input.Pressed(Key.Escape) || input.Pressed(Key.Enter) || input.RightClick || input.Clicked(BackButton))
        {
            Host.Show(_back);
            return;
        }
        if (!input.LeftClick)
            return;

        var changed = false;
        foreach (var row in new[] { Row.EffectsVolume, Row.MusicVolume })
        {
            var arrows = ArrowRect(row);
            if (input.Clicked(arrows))
            {
                var up = input.MouseY < arrows.Y + arrows.H / 2;
                SetVolume(row, Volume(row) + (up ? 1 : -1));
                changed = true;
            }
            for (var level = 1; level <= GameSettings.MaxVolume; level++)
            {
                if (!input.Clicked(CellRect(row, level)))
                    continue;
                // Klick auf die einzige volle Zelle schaltet ganz aus
                SetVolume(row, Volume(row) == level && level == 1 ? 0 : level);
                changed = true;
            }
        }

        foreach (var row in new[] { Row.MusicInMatch, Row.GoalScenes, Row.Fullscreen, Row.AutoSave })
        {
            if (!input.Clicked(RowRect(row)))
                continue;
            switch (row)
            {
                case Row.MusicInMatch:
                    Settings.MusicInMatch = !Settings.MusicInMatch;
                    break;
                case Row.GoalScenes:
                    Settings.GoalScenes = !Settings.GoalScenes;
                    break;
                case Row.AutoSave:
                    Settings.AutoSave = !Settings.AutoSave;
                    break;
                default:
                    Settings.Fullscreen = !Settings.Fullscreen;
                    break;
            }
            changed = true;
        }

        if (changed)
            GameSettings.SaveCurrent();
    }

    private static int Volume(Row row) => row == Row.EffectsVolume ? Settings.EffectsVolume : Settings.MusicVolume;

    private static void SetVolume(Row row, int value)
    {
        if (row == Row.EffectsVolume)
            Settings.EffectsVolume = value;
        else
            Settings.MusicVolume = value;
    }

    private static bool Toggle(Row row) => row switch
    {
        Row.MusicInMatch => Settings.MusicInMatch,
        Row.GoalScenes => Settings.GoalScenes,
        Row.AutoSave => Settings.AutoSave,
        _ => Settings.Fullscreen,
    };

    public override void Draw(Surface s)
    {
        s.Palette = Assets.Palette;
        s.Blit(Assets.Image(MainMenuScreen.Background), 0, 0);
        Widgets.Frame(s, Assets, Window, FrameStyle.Orange, Colors.PanelFill);
        Widgets.ShadowText(s, Assets.Bold, 160, Window.Y + 10, "Einstellungen", Colors.White, Colors.Black);

        foreach (var row in Enum.GetValues<Row>())
        {
            var y = FirstRowY + RowPitch * (int)row;
            s.Text(Assets.Mini, LabelX, y, Labels[(int)row], Colors.White);

            if (row is Row.EffectsVolume or Row.MusicVolume)
            {
                var volume = Volume(row);
                for (var level = 1; level <= GameSettings.MaxVolume; level++)
                {
                    var cell = CellRect(row, level);
                    if (level <= volume)
                        s.Fill(cell, Colors.Yellow);
                    else
                        s.Outline(cell, Colors.Black);
                }
                TeamUi.Arrows(s, Assets, ArrowsX, ArrowRect(row).Y);
                s.TextRight(Assets.Mini, ValueX + 12, y, volume.ToString(), Colors.Yellow);
            }
            else
            {
                var on = Toggle(row);
                Widgets.Lamp(s, Assets, LampX, y, red: !on);
                s.Text(Assets.Mini, LampX + 10, y, on ? "an" : "aus", on ? Colors.Yellow : Colors.White);
            }
        }

        s.TextCentered(Assets.Mini, 160, 177, "F8 = Ton aus/an    F11 = Vollbild", Colors.White);
        Widgets.Button(s, Assets, BackButton.X, BackButton.Y, "Zurück");
    }
}

/// <summary>Meldet den Bildschirm unter „Gemischtes“ an.</summary>
internal static class SettingsMenuEntries
{
    [MenuEntry("Einstellungen")]
    private static Screen Open(ScreenHost host, Screen back) => new SettingsScreen(host, back);
}
