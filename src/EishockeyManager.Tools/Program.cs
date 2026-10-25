using EishockeyManager.Formats;

// emtool – Hilfswerkzeug zum Untersuchen und Exportieren der Originaldateien.
//
//   emtool info   [spielordner]              Übersicht über alle Grafiken
//   emtool export [spielordner] [zielordner] Alle dekodierbaren Grafiken als PNG

var command = args.Length > 0 ? args[0].ToLowerInvariant() : "help";

try
{
    switch (command)
    {
        case "info":
            Info(GameData.Locate(args.ElementAtOrDefault(1)));
            return 0;
        case "export":
            Export(GameData.Locate(args.ElementAtOrDefault(1)), args.ElementAtOrDefault(2) ?? "export");
            return 0;
        default:
            Console.WriteLine("""
                emtool – Werkzeug für die Originaldateien des Eishockey Managers

                  emtool info   [spielordner]
                  emtool export [spielordner] [zielordner]

                Ohne Spielordner wird nach einem Ordner "gamedata" gesucht.
                """);
            return command == "help" ? 0 : 1;
    }
}
catch (Exception ex) when (ex is DirectoryNotFoundException or IOException)
{
    Console.Error.WriteLine(ex.Message);
    return 2;
}

static void Info(GameData game)
{
    Console.WriteLine($"Spielordner: {game.RootDirectory}");
    Console.WriteLine($"{"Datei",-10} {"Größe",8}  {"Breite",6} {"Höhe",5}  Palette");
    foreach (var number in game.GraphicsNumbers())
    {
        var path = game.GraphicsPath(number);
        var bytes = File.ReadAllBytes(path);
        if (VgaImage.TryParse(bytes, out var image))
            Console.WriteLine($"{number + ".vga",-10} {bytes.Length,8}  {image!.Width,6} {image.Height,5}  {(image.Palette is null ? "" : "eigene")}");
        else
            Console.WriteLine($"{number + ".vga",-10} {bytes.Length,8}  (kein Bild / unbekanntes Format)");
    }
}

static void Export(GameData game, string targetDirectory)
{
    Directory.CreateDirectory(targetDirectory);
    var palette = game.LoadDefaultPalette();
    int ok = 0, skipped = 0;
    foreach (var number in game.GraphicsNumbers())
    {
        var bytes = File.ReadAllBytes(game.GraphicsPath(number));
        if (!VgaImage.TryParse(bytes, out var image))
        {
            skipped++;
            continue;
        }
        IndexedPngWriter.Save(Path.Combine(targetDirectory, $"{number}.png"), image!, palette);
        ok++;
    }
    Console.WriteLine($"{ok} Bilder nach '{Path.GetFullPath(targetDirectory)}' exportiert, {skipped} Dateien übersprungen (kein Bildformat).");
}
