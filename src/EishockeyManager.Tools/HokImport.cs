using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Tools;

/// <summary>
/// <c>emtool hok import &lt;hok&gt; &lt;ziel&gt; [--gamedata ordner] [--seed n] [--force]</c>: Original-Spielstand in einen
/// Remake-Spielstand (<c>*.ems</c>) umwandeln (Spur hok-import, Teil 3). Import nach <see cref="OriginalImport.TryImport"/>,
/// Schreiben mit <see cref="SaveGame.Save(GameState)"/>; ohne Endung bekommt das Ziel <c>.ems</c>.
/// Falsche Prüfsumme: Abbruch mit Rückgabe 3 wie „Der kleine Mogelguru“ im Original.
/// [APPROX] Zufall: feste Saat 1993, solange <c>--seed</c> fehlt (Feldkarte Regel 6: neue Saat; fest, damit das Werkzeug
/// wiederholbar ist).
/// </summary>
internal static class HokImport
{
    public const int DefaultSeed = 1993;

    public static int Run(IReadOnlyList<string> args)
    {
        if (args.Count < 2)
        {
            Console.Error.WriteLine("Aufruf: emtool hok import <hok> <ziel> [--gamedata ordner] [--seed n] [--force]");
            return 1;
        }

        string source = args[0], target = args[1];
        if (Path.GetExtension(target).Length == 0)
            target += SaveGame.FileExtension;
        string? gameData = null;
        var seed = DefaultSeed;
        var force = false;
        try
        {
            for (var i = 2; i < args.Count; i++)
            {
                switch (args[i])
                {
                    case "--gamedata": gameData = Next(args, ref i); break;
                    case "--seed": seed = int.Parse(Next(args, ref i)); break;
                    case "--force": force = true; break;
                    default: throw new ArgumentException($"Unbekannte Option „{args[i]}“.");
                }
            }
            if (File.Exists(target) && !force)
                throw new ArgumentException($"{target} gibt es schon (--force zum Überschreiben).");

            var database = ClubDatabase.Load(GameData.Locate(gameData));
            var result = OriginalImport.TryImport(File.ReadAllBytes(source), database, seed);
            if (result.ChecksumError)
            {
                Console.Error.WriteLine("Der kleine Mogelguru verabscheut Ihre Taten ! (Prüfsumme falsch, nichts geschrieben)");
                return 3;
            }
            if (result.Game is not { } game)
            {
                Console.Error.WriteLine($"{source} kann nicht importiert werden: {result.Error}");
                return 1;
            }

            File.WriteAllBytes(target, SaveGame.Save(game));
            var manager = game.ActiveManager;
            Console.WriteLine($"Importiert: {source} → {target}");
            Console.WriteLine($"  {game.DateText}, Saison {game.Season}, Manager {game.Managers.Count}, am Zug „{manager.Name}“, " +
                              $"Konto {manager.Money} DM, Saat {seed}");
            return 0;
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or OverflowException)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static string Next(IReadOnlyList<string> args, ref int i) =>
        ++i < args.Count ? args[i] : throw new ArgumentException($"{args[i - 1]} braucht einen Wert.");
}
