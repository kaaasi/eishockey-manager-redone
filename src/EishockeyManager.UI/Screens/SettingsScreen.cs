using EishockeyManager.Core;
using EishockeyManager.UI.Screens.Team;

namespace EishockeyManager.UI.Screens;

/// <summary>
/// Bildschirm „Einstellungen“ (Kachel unter „Gemischtes“): Lautstärke Effekte,
/// Torszenen, Vollbild, automatisch speichern. Jede Änderung gilt sofort und wird gespeichert.
/// </summary>
/// <remarks>
/// Das Original hat einen Einstellungsbildschirm (em.exe 0x2CD1C, DOSBox-Bilder 48–51 in
/// <c>docs/dosbox-befunde.md</c>), der Spielstand-Optionen (Drittelpause, Ergebnisse, Tabelle je Liga, Pokale, Torszenen
/// …) bearbeitet; sein Layout ist hier nicht nachgebaut. [APPROX] Die Geräteeinstellungen (Lautstärke, Vollbild,
/// automatisch speichern) gibt es im Original nicht; ihr Layout ist neu, aber aus den Bausteinen des Originals gebaut:
/// Fenster wie die Hinweise (Rahmen kb\2 orange auf Hintergrund kb\22), Pfeile ▲▼ aus kb\4 wie bei der Taktik,
/// Lampen aus kb\3 (grün = an, rot = aus), Knopf aus kb\2. Die Seite „Spielstand-Optionen“ (Knopf „Spielstand“) zeigt die
/// Optionen des Originals nur an (<see cref="GameOptionsView"/>); ihre Umsetzung ist Paket <c>optionen-wirkung</c>.
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
        GoalScenes,
        Fullscreen,
        AutoSave,
    }

    private static readonly string[] Labels =
        ["Lautstärke Effekte", "Torszenen zeigen", "Vollbild", "Automatisch speichern"];

    /// <summary>Zeile i (ganze Breite, zum Umschalten der Lampen).</summary>
    public static Rect RowRect(Row row) => new(LabelX - 4, FirstRowY + RowPitch * (int)row - 5, 236, 15);

    /// <summary>Pfeile ▲▼ einer Lautstärkezeile; obere Hälfte lauter, untere leiser.</summary>
    public static Rect ArrowRect(Row row) =>
        new(ArrowsX, FirstRowY + RowPitch * (int)row - 6, TeamUi.ArrowWidth, TeamUi.ArrowHeight);

    /// <summary>Zelle 1–10 des Lautstärkebalkens (Klick setzt die Lautstärke direkt).</summary>
    public static Rect CellRect(Row row, int level) =>
        new(BarX + (level - 1) * CellPitch, FirstRowY + RowPitch * (int)row - 1, CellWidth, CellHeight);

    public static readonly Rect BackButton = Widgets.ButtonRect(160 - Widgets.ButtonWidth / 2 + 36, 171);

    /// <summary>Wechselt zwischen Geräteeinstellungen und der lesenden Anzeige der Spielstand-Optionen.</summary>
    public static readonly Rect OptionsButton = Widgets.ButtonRect(160 - Widgets.ButtonWidth / 2 - 36, 171);

    private const int OptionsLabelX = 44;
    private const int MatrixX = 176;
    private const int MatrixPitch = 28;

    private readonly Screen _back;

    /// <summary>Zeigt die Seite „Spielstand-Optionen“ (nur Anzeige) statt der Geräteeinstellungen.</summary>
    public bool ShowsGameOptions { get; private set; }

    /// <summary>Anzeigewerte der Spielstand-Optionen (Modus des laufenden Spiels, sonst Vorgabe).</summary>
    public GameOptionsView GameOptions => GameOptionsView.For(Host.Game?.Mode);

    public SettingsScreen(ScreenHost host, Screen back) : base(host) => _back = back;

    private static GameSettings Settings => GameSettings.Current;

    public override void Update(InputState input)
    {
        if (input.Pressed(Key.Escape) || input.Pressed(Key.Enter) || input.RightClick || input.Clicked(BackButton))
        {
            Host.Show(_back);
            return;
        }
        if (input.Clicked(OptionsButton))
        {
            ShowsGameOptions = !ShowsGameOptions;
            return;
        }
        if (!input.LeftClick || ShowsGameOptions)
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

        foreach (var row in new[] { Row.GoalScenes, Row.Fullscreen, Row.AutoSave })
        {
            if (!input.Clicked(RowRect(row)))
                continue;
            switch (row)
            {
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

    private static bool Toggle(Row row) => row switch
    {
        Row.GoalScenes => Settings.GoalScenes,
        Row.AutoSave => Settings.AutoSave,
        _ => Settings.Fullscreen,
    };

    public override void Draw(Surface s)
    {
        s.Palette = Assets.Palette;
        s.Blit(Assets.Image(MainMenuScreen.Background), 0, 0);
        Widgets.Frame(s, Assets, Window, FrameStyle.Orange, Colors.PanelFill);
        Widgets.ShadowText(s, Assets.Bold, 160, Window.Y + 10,
            ShowsGameOptions ? "Spielstand-Optionen" : "Einstellungen", Colors.White, Colors.Black);
        if (ShowsGameOptions)
            DrawGameOptions(s);
        else
            DrawDeviceSettings(s);
        Widgets.Button(s, Assets, OptionsButton.X, OptionsButton.Y, ShowsGameOptions ? "Gerät" : "Spielstand");
        Widgets.Button(s, Assets, BackButton.X, BackButton.Y, "Zurück");
    }

    private void DrawGameOptions(Surface s)
    {
        var view = GameOptions;
        s.Text(Assets.Mini, OptionsLabelX, 86, "Spielmodus", Colors.White);
        s.Text(Assets.Mini, MatrixX, 86, GameOptionsView.ModeName(view.Mode), Colors.Yellow);
        for (var c = 0; c < 3; c++)
            s.Text(Assets.Mini, MatrixX + c * MatrixPitch, 98, GameOptionsView.MatrixColumns[c], Colors.White);
        for (var r = 0; r < 3; r++)
        {
            var y = 109 + r * 10;
            s.Text(Assets.Mini, OptionsLabelX, y, GameOptionsView.MatrixRows[r], Colors.White);
            for (var c = 0; c < 3; c++)
                Widgets.Lamp(s, Assets, MatrixX + c * MatrixPitch, y, red: !view.Matrix[r * 3 + c]);
        }
        (string Name, bool On)[] cups = [("DEB-Pokal", view.GermanCup), ("Europapokal", view.EuropeanCup), ("WM-Spiele", view.WorldCup)];
        for (var i = 0; i < cups.Length; i++)
        {
            var y = 142 + i * 10;
            s.Text(Assets.Mini, OptionsLabelX, y, cups[i].Name, Colors.White);
            Widgets.Lamp(s, Assets, MatrixX, y, red: !cups[i].On);
        }
        s.Text(Assets.Mini, MatrixX + 28, 152, "Nur Anzeige", Colors.White);
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

/// <summary>Meldet den Bildschirm unter „Gemischtes“ an.</summary>
internal static class SettingsMenuEntries
{
    [MenuEntry("Einstellungen")]
    private static Screen Open(ScreenHost host, Screen back) => new SettingsScreen(host, back);
}
