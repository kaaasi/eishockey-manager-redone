using EishockeyManager.Formats;

// emtool – Hilfswerkzeug zum Untersuchen und Exportieren der Originaldateien.
//
//   emtool info    [spielordner]              Übersicht über alle Grafiken
//   emtool export  [spielordner] [zielordner] Grafiken, Halle, Figuren und Fonts als PNG
//   emtool export-sound [spielordner] [zielordner] Soundeffekte und Musik als WAV
//   emtool vereine [spielordner] [nummer]     Vereine aus der Datenbank (mit Nummer: Kader)
//   emtool screens [spielordner] [zielordner] [ablauf …]  Bildschirme ohne Fenster als PNG
//   emtool hok dump <datei> [block …]           Original-Spielstand *.HOK entschlüsseln und prüfen
//   emtool hok patch <vorlage> <ziel> feld=wert …   veränderten Spielstand fürs Original erzeugen
//   emtool hok import <hok> <ziel>              Original-Spielstand als Remake-Spielstand (*.ems) importieren
//   emtool hok export <spielstand> <ziel.hok>   Remake-Spielstand als Original-Spielstand *.HOK exportieren

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
        case "export-sound":
            EishockeyManager.Tools.SoundExport.Run(GameData.Locate(args.ElementAtOrDefault(1)), args.ElementAtOrDefault(2) ?? "sound");
            return 0;
        case "vereine":
            Clubs(GameData.Locate(args.ElementAtOrDefault(1)), args.ElementAtOrDefault(2));
            return 0;
        case "screens":
            EishockeyManager.Tools.Scenarios.ScenarioRunner.Run(
                GameData.Locate(args.ElementAtOrDefault(1)), args.ElementAtOrDefault(2) ?? "screens", args.Skip(3).ToList());
            return 0;
        case "hok" when args.ElementAtOrDefault(1) == "export": // Spur hok-export (Runde 15)
            return EishockeyManager.Tools.HokExport.Run(args.Skip(2).ToList());
        case "hok":
            return EishockeyManager.Tools.HokDump.Run(args.Skip(1).ToList());
        default:
            Console.WriteLine("""
                emtool – Werkzeug für die Originaldateien des Eishockey Managers

                  emtool info    [spielordner]
                  emtool export  [spielordner] [zielordner]
                  emtool export-sound [spielordner] [zielordner]
                  emtool vereine [spielordner] [vereinsnummer]
                  emtool screens [spielordner] [zielordner] [ablauf …]
                  emtool hok dump <datei> [block …]
                  emtool hok patch <vorlage> <ziel> feld=wert …
                  emtool hok import <hok> <ziel> [--gamedata ordner] [--seed n] [--force]
                  emtool hok export <spielstand> <ziel.hok> [--gamedata ordner] [--seed n] [--force]

                Ohne Spielordner wird nach einem Ordner "gamedata" gesucht.
                """);
            Console.WriteLine("Abläufe für screens: " + string.Join(", ", EishockeyManager.Tools.Scenarios.ScenarioRunner.All.Keys));
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
            Console.WriteLine($"{number + ".vga",-10} {bytes.Length,8}  (kein Bild – siehe docs/formate)");
    }
}

static void Export(GameData game, string targetDirectory)
{
    var palette = game.LoadDefaultPalette();

    var imageDirectory = Path.Combine(targetDirectory, "bilder");
    Directory.CreateDirectory(imageDirectory);
    int ok = 0, skipped = 0;
    foreach (var number in game.GraphicsNumbers())
    {
        var bytes = File.ReadAllBytes(game.GraphicsPath(number));
        if (!VgaImage.TryParse(bytes, out var image))
        {
            skipped++;
            continue;
        }
        IndexedPngWriter.Save(Path.Combine(imageDirectory, $"{number}.png"), image!, palette);
        ok++;
    }
    Console.WriteLine($"{ok} Bilder exportiert, {skipped} Dateien sind keine Bilder.");

    var sceneDirectory = Path.Combine(targetDirectory, "torszenen");
    Directory.CreateDirectory(sceneDirectory);
    var tiles = RinkTileSet.Load(game);
    foreach (var mapNumber in new[] { 236, 242, 243, 244, 245 })
    {
        var map = RinkMap.Load(game.GraphicsPath(mapNumber));
        IndexedPngWriter.Save(Path.Combine(sceneDirectory, $"halle_{mapNumber}.png"), map.Render(tiles), palette);
    }
    foreach (var (number, name) in new[] { (233, "heim"), (234, "gast"), (235, "torhueter") })
    {
        var sheet = SpriteSheet.Load(game.GraphicsPath(number));
        IndexedPngWriter.Save(Path.Combine(sceneDirectory, $"figuren_{name}.png"), sheet.ToContactSheet(), palette);
    }
    Console.WriteLine($"Hallen und Figuren der Torszenen exportiert ({tiles.Count} Kacheln).");

    var exe = Path.Combine(game.RootDirectory, "em.exe");
    if (File.Exists(exe))
    {
        const string sample = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZÄÖÜäöüß abcdefghijklmnopqrstuvwxyz";
        foreach (var kind in Enum.GetValues<FontKind>())
        {
            var font = BitmapFont.LoadFromExe(exe, kind);
            var width = font.Measure(sample) + 4;
            var pixels = new byte[width * (font.Height + 4)];
            font.Draw(pixels, width, 2, 2, sample, 255);
            IndexedPngWriter.Save(Path.Combine(targetDirectory, $"font_{kind.ToString().ToLowerInvariant()}.png"),
                new VgaImage(width, font.Height + 4, pixels), Palette.Grayscale());
        }
        Console.WriteLine("Fonts exportiert.");
    }

    Console.WriteLine($"Ziel: {Path.GetFullPath(targetDirectory)}");
}

static void Clubs(GameData game, string? selection)
{
    var db = ClubDatabase.Load(game);
    if (int.TryParse(selection, out var index) && index >= 0 && index < db.Clubs.Count)
    {
        var club = db[index];
        Console.WriteLine($"{club.Index}: {club.Name} – Stärke {club.Strength}, Logo {club.LogoGraphic?.ToString() ?? "–"}, {club.Category}");
        foreach (var player in club.Players)
            Console.WriteLine($"  {player.Role,-11} {player.Name}{(player.IsForeigner ? " (Ausländer)" : "")}");
        return;
    }

    foreach (var club in db.Clubs)
        Console.WriteLine($"{club.Index,3}  {club.Name,-23} {club.Strength,3}  {club.Category,-17} {club.Players.Count,2} Spieler");
}
