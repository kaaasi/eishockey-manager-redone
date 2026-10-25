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
        Click(160, 120); Press(Key.Escape);

        // Spieltag: Banner, „Spiele laufen“, zurück ins Menü
        Click(274, 170); Frame(); Save("11_spieltag_banner");
        for (var i = 0; i < MatchDayScreen.BannerFrames + 40 * MatchDayScreen.FramesPerMinute; i++) Frame();
        Save("12_spiele_laufen");
        Press(Key.Space); Move(10, 230); Save("13_nach_spieltag");

        // 16 weitere Spieltage im Schnelldurchlauf (Menü → Spieltag, Banner überspringen, zurück)
        for (var md = 0; md < 16; md++)
        {
            Press(Key.Space);
            while (host.Current is not MainMenuScreen) Press(Key.Space);
        }
        Move(10, 230); Save("14_nach_17_spieltagen");

        // Tabelle Oberliga (Berliner SC rückt in die Oberliga)
        Click(270, 110); Click(127, 220); Save("15_tabelle_oberliga");
        Click(160, 170); Save("16_tabelle_oberliga_heim");
        Press(Key.Escape); Click(270, 110); Click(27, 220); Save("17_tabelle_bundesliga");
    }
}
