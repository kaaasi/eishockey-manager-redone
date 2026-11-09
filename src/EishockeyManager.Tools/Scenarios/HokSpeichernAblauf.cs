using EishockeyManager.UI;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.SaveLoad;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>
/// Spur hok-import, Teil 5 (A338/A339): Speichern-Dialog mit 53 Einträgen wie in der DOSBox-Aufnahme – Vergleich mit den Bildern
/// des Referenz-Repos (<c>original/spielstaende/bilder/a338_1…3</c>). Die Namen entsprechen der Aufnahme (Verzeichnisse HIGH, KB,
/// TORE; AUTOSAVE, FILE…, KOPIE01–KOPIE30 als Spielstände), der Inhalt von KOPIE30 ist FILE46 (Kopf für das Infofeld).
/// Ausführen: <c>dotnet run --project src/EishockeyManager.Tools -- screens gamedata &lt;ziel&gt; speichern-original</c>.
/// </summary>
internal static class HokSpeichernAblauf
{
    [Scenario("speichern-original")]
    private static void Speichern(ScreenDriver d)
    {
        var source = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(
            Directory.ResolveLinkTarget(d.Host.Assets.Game.RootDirectory.TrimEnd('/', '\\'), true)?.FullName
            ?? d.Host.Assets.Game.RootDirectory.TrimEnd('/', '\\')).TrimEnd('/', '\\'))!, "spielstaende");
        if (Environment.GetEnvironmentVariable("EM_SAVEGAMES") is { Length: > 0 } folder)
            source = folder;
        var directory = Path.Combine(Path.GetTempPath(), "em-speichern-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            foreach (var dir in new[] { "AA", "BB", "HIGH", "KB", "TORE" }) // ".", ".." der Aufnahme nur als Platzhalter
                Directory.CreateDirectory(Path.Combine(directory, dir));
            var names = new List<string> { "AUTOSAVE", "FILE01", "FILE02", "FILE03", "FILE11", "FILE12", "FILE13", "FILE14", "FILE18", "FILE43A",
                "FILE44", "FILE46", "FILE47", "FILE48", "FILE49", "FILE49A" };
            names.AddRange(Enumerable.Range(1, 30).Select(i => $"KOPIE{i:D2}"));
            foreach (var name in names)
                File.Copy(Path.Combine(source, "FILE46.HOK"), Path.Combine(directory, name + ".HOK"));
            File.WriteAllBytes(Path.Combine(directory, "EM.EXE"), []);
            File.WriteAllBytes(Path.Combine(directory, "INFOFILE"), []);

            var context = SaveContext.For(d.Host);
            context.Directory = directory;
            context.AutoSaveOverride = false;
            var title = new TitleScreen(d.Host);
            d.Host.Show(title);
            d.Host.Show(new SaveLoadScreen(d.Host, title, SaveLoadMode.Save));
            d.Move(310, 166); d.Save("01_seite1");
            d.Click(177, 172); d.Frame(i => { i.MouseX = 177; i.MouseY = 172; });
            d.Move(310, 166); d.Save("02_seite2");
            // KOPIE30 = Zeile 4 der ersten Spalte auf Seite 2; Info bei gehaltener rechter Taste
            d.Frame(i => { i.MouseX = 100; i.MouseY = 55 + 7 * 4 + 3; i.RightClick = true; i.RightDown = true; });
            d.Save("03_infofeld");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
