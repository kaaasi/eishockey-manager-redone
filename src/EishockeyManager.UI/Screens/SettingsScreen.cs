using EishockeyManager.Core;
using EishockeyManager.UI.Screens.Team;

namespace EishockeyManager.UI.Screens;

/// <summary>
/// Geräteeinstellungen des Remakes: Lautstärke Effekte, Vollbild, automatisch speichern. Jede Änderung gilt sofort und wird
/// gespeichert. Geöffnet über den Zusatzknopf „Gerät“ im Optionsfenster (<see cref="GameOptionsScreen.DeviceButton"/>, #836).
/// </summary>
/// <remarks>
/// Der Einstellungsbildschirm des Originals (em.exe 0x2CD1C, DOSBox-Bilder 48–51) ist <see cref="GameOptionsScreen"/>; ihn
/// öffnet die Kachel „Einstellungen“ (#836). Die Torszenen (Index 9) schaltet nur dieser Bildschirm. [APPROX] Die Geräteeinstellungen (Lautstärke, Vollbild,
/// automatisch speichern) gibt es im Original nicht; ihr Layout ist neu, aber aus den Bausteinen des Originals gebaut:
/// Fenster wie die Hinweise (Rahmen kb\2 orange auf Hintergrund kb\22), Pfeile ▲▼ aus kb\4 wie bei der Taktik,
/// Lampen aus kb\3 (grün = an, rot = aus), Knopf aus kb\2.
/// </remarks>
public sealed class SettingsScreen : Screen
{
    public static readonly Rect Window = new(24, 53, 272, 138);

    private const int LabelX = 44;
    private const int FirstRowY = 90;
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
        Fullscreen,
        AutoSave,
    }

    private static readonly string[] Labels =
        ["Lautstärke Effekte", "Vollbild", "Automatisch speichern"];

    /// <summary>Zeile i (ganze Breite, zum Umschalten der Lampen).</summary>
    public static Rect RowRect(Row row) => new(LabelX - 4, FirstRowY + RowPitch * (int)row - 5, 236, 15);

    /// <summary>Pfeile ▲▼ einer Lautstärkezeile; obere Hälfte lauter, untere leiser.</summary>
    public static Rect ArrowRect(Row row) =>
        new(ArrowsX, FirstRowY + RowPitch * (int)row - 6, TeamUi.ArrowWidth, TeamUi.ArrowHeight);

    /// <summary>Zelle 1–10 des Lautstärkebalkens (Klick setzt die Lautstärke direkt).</summary>
    public static Rect CellRect(Row row, int level) =>
        new(BarX + (level - 1) * CellPitch, FirstRowY + RowPitch * (int)row - 1, CellWidth, CellHeight);

    public static readonly Rect BackButton = Widgets.ButtonRect(160 - Widgets.ButtonWidth / 2, 171);

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
        var arrows = ArrowRect(Row.EffectsVolume);
        if (input.Clicked(arrows))
        {
            var up = input.MouseY < arrows.Y + arrows.H / 2;
            Settings.EffectsVolume += up ? 1 : -1;
            changed = true;
        }
        for (var level = 1; level <= GameSettings.MaxVolume; level++)
        {
            if (!input.Clicked(CellRect(Row.EffectsVolume, level)))
                continue;
            // Klick auf die einzige volle Zelle schaltet ganz aus
            Settings.EffectsVolume = Settings.EffectsVolume == level && level == 1 ? 0 : level;
            changed = true;
        }

        foreach (var row in new[] { Row.Fullscreen, Row.AutoSave })
        {
            if (!input.Clicked(RowRect(row)))
                continue;
            switch (row)
            {
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

    private static bool Toggle(Row row) => row switch
    {
        Row.AutoSave => Settings.AutoSave,
        _ => Settings.Fullscreen,
    };

    public override void Draw(Surface s)
    {
        s.Palette = Assets.Palette;
        s.Blit(Assets.Image(MainMenuScreen.Background), 0, 0);
        Widgets.Frame(s, Assets, Window, FrameStyle.Orange, Colors.PanelFill);
        Widgets.ShadowText(s, Assets.Bold, 160, Window.Y + 10, "Einstellungen", Colors.White, Colors.Black);
        DrawDeviceSettings(s);
        Widgets.Button(s, Assets, BackButton.X, BackButton.Y, "Zurück");
    }

    private void DrawDeviceSettings(Surface s)
    {
        foreach (var row in Enum.GetValues<Row>())
        {
            var y = FirstRowY + RowPitch * (int)row;
            s.Text(Assets.Mini, LabelX, y, Labels[(int)row], Colors.White);

            if (row == Row.EffectsVolume)
            {
                var volume = Settings.EffectsVolume;
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

        s.TextCentered(Assets.Mini, 160, 160, "F8 = Ton aus/an    F11 = Vollbild", Colors.White);
    }
}
