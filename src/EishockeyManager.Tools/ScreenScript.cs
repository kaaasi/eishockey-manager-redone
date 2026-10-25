using EishockeyManager.Formats;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens;

/// <summary>
/// Spielt einen festen Ablauf durch (Titel → Spielvorbereitung → Hauptmenü) und speichert jeden
/// Zwischenstand als PNG. Dient dem Pixelvergleich mit Aufnahmen des Originals.
/// </summary>
public static class ScreenScript
{
    public static void Run(GameData game, string target)
    {
        Directory.CreateDirectory(target);
        var host = new ScreenHost(new Assets(game)) { Seed = 1993 };
        var surface = new Surface();
        var input = new InputState();

        void Frame(Action<InputState>? setup = null)
        {
            input.Reset();
            setup?.Invoke(input);
            host.Update(input);
            host.Draw(surface);
        }
        void Click(int x, int y) => Frame(i => { i.MouseX = x; i.MouseY = y; i.LeftClick = true; });
        void Move(int x, int y) => Frame(i => { i.MouseX = x; i.MouseY = y; });
        void Type(string text) => Frame(i => i.Text = text);
        void Press(Key key) => Frame(i => i.Keys.Add(key));
        void Save(string name)
        {
            IndexedPngWriter.Save(Path.Combine(target, name + ".png"), surface.ToImage(), surface.Palette);
            Console.WriteLine($"  {name}.png");
        }

        host.Show(new TitleScreen(host));
        Frame(); Save("01_titel");
        Click(160, 120); Move(150, 20); Save("02_setup_leer");
        Click(20, 60); Move(200, 140); Save("03_setup_kopfwahl");
        Click(200, 100); Type("LetsShow"); Move(100, 70); Save("04_setup_name");
        Press(Key.Enter); Click(150, 60);
        for (var p = 0; p < 3; p++) Frame(i => i.Wheel = -1);
        Move(209, 140); Save("05_setup_vereinswahl");
        Click(209, 140); Move(100, 150); Save("06_setup_fertig");
        Click(286, 212); Move(10, 230); Save("07_hauptmenue");
        Click(190, 110); Move(10, 230); Save("08_hauptmenue_statistiken");
        Click(45, 110); Move(10, 230); Save("09_hauptmenue_manager");
        Click(27, 220); Save("10_platzhalter");
    }
}
