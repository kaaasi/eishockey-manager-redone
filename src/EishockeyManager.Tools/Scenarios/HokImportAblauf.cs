using EishockeyManager.Formats;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.SaveLoad;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>
/// Spur hok-import, Teil 3: Original-Spielstände im Laden-Dialog importieren, Hauptmenü danach – Vergleich mit den
/// DOSBox-Bildern des Referenz-Repos (<c>original/spielstaende/bilder/file05_1_hauptmenue.png</c>,
/// <c>file29_4_hauptmenue.png</c>, <c>kaputt_mogelguru.png</c>). Spielstände aus <c>EM_SAVEGAMES</c> oder
/// <c>spielstaende</c> neben dem aufgelösten Ordner <c>gamedata</c>; sie werden in einen Zwischenordner kopiert.
/// </summary>
internal static class HokImportAblauf
{
    [Scenario("hok-import")]
    private static void Import(ScreenDriver d)
    {
        var source = SaveGamesFolder(d.Host.Assets.Game);
        var directory = Path.Combine(Path.GetTempPath(), "em-hok-import-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            foreach (var name in new[] { "FILE05.HOK", "FILE29.HOK", "KAPUTT.HOK" })
                File.Copy(Path.Combine(source, name), Path.Combine(directory, name));
            var context = SaveContext.For(d.Host);
            context.Directory = directory;
            context.AutoSaveOverride = false;

            var title = new TitleScreen(d.Host);
            d.Host.Show(title);
            d.Host.Show(new SaveLoadScreen(d.Host, title, SaveLoadMode.Load));
            d.Move(10, 230); d.Save("01_laden_liste");

            d.Type("kaputt"); d.Press(Key.Enter); d.Move(160, 75); d.Save("02_mogelguru");
            d.Click(160, 120);
            for (var i = 0; i < "KAPUTT".Length; i++)
                d.Press(Key.Backspace);

            d.Type("file05"); d.Press(Key.Enter); d.Move(10, 230); d.Save("03_hauptmenue_file05");

            d.Host.Show(new SaveLoadScreen(d.Host, d.Host.Current!, SaveLoadMode.Load));
            d.Type("file29"); d.Press(Key.Enter); d.Move(10, 230); d.Save("04_hauptmenue_file29");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string SaveGamesFolder(GameData game)
    {
        var folder = Environment.GetEnvironmentVariable("EM_SAVEGAMES");
        if (!string.IsNullOrEmpty(folder))
            return folder;
        var root = game.RootDirectory.TrimEnd('/', '\\');
        var real = Directory.ResolveLinkTarget(root, true)?.FullName ?? root;
        return Path.Combine(Path.GetDirectoryName(Path.GetFullPath(real).TrimEnd('/', '\\'))!, "spielstaende");
    }
}
