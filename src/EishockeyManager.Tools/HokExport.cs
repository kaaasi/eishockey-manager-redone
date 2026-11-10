using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Tools;

/// <summary>
/// <c>emtool hok export &lt;spielstand&gt; &lt;ziel.hok&gt; [--gamedata ordner] [--seed n] [--force]</c>: Remake-Spielstand (<c>*.ems</c>)
/// als Original-Spielstand <c>*.HOK</c> schreiben (Spur hok-export, Teil 3, R15-D5 = D: nur über die Kommandozeile, keine Option im
/// Speichern-Dialog). Export nach <see cref="OriginalExport.Export"/>; das Ergebnis wird mit <see cref="OriginalSaveGame.Read"/>
/// gegengelesen (Länge, Kennungen, Prüfsummen) und lässt sich mit <c>emtool hok dump</c> ansehen.
/// <para>
/// Prüfhilfe für die DOSBox-Probe (R15-D5 = D): <c>--neu &lt;name&gt; &lt;vereinsindex&gt;</c> statt des Spielstands legt ein neues
/// Remake-Spiel an (Level 3, Dauer ∞, Mittelfeld; ein Manager), spielt mit <c>--spieltage n</c> n Ligaspieltage über
/// <see cref="GameState.Continue"/> und exportiert dann; <c>--ems datei</c> schreibt den Remake-Stand daneben.
/// </para>
/// [APPROX] Spielstand-ID, Füllbyte und Startschlüssel aus der Saat (Vorgabe 1993), damit das Werkzeug wiederholbar ist.
/// </summary>
internal static class HokExport
{
    public const int DefaultSeed = 1993;

    private const string Usage =
        "Aufruf: emtool hok export <spielstand> <ziel.hok> [--gamedata ordner] [--seed n] [--force]\n" +
        "        emtool hok export --neu <name> <vereinsindex> <ziel.hok> [--spieltage n] [--ems datei] [--gamedata ordner] [--seed n] [--force]";

    public static int Run(IReadOnlyList<string> args)
    {
        try
        {
            string? gameData = null, ems = null, newName = null;
            int? newClub = null;
            var seed = DefaultSeed;
            var matchDays = 0;
            var force = false;
            var positional = new List<string>();
            for (var i = 0; i < args.Count; i++)
            {
                switch (args[i])
                {
                    case "--gamedata": gameData = Next(args, ref i); break;
                    case "--seed": seed = int.Parse(Next(args, ref i)); break;
                    case "--force": force = true; break;
                    case "--spieltage": matchDays = int.Parse(Next(args, ref i)); break;
                    case "--ems": ems = Next(args, ref i); break;
                    case "--neu":
                        newName = Next(args, ref i);
                        newClub = int.Parse(Next(args, ref i));
                        break;
                    default:
                        if (args[i].StartsWith("--", StringComparison.Ordinal))
                            throw new ArgumentException($"Unbekannte Option „{args[i]}“.");
                        positional.Add(args[i]);
                        break;
                }
            }
            if (positional.Count != (newName is null ? 2 : 1))
            {
                Console.Error.WriteLine(Usage);
                return 1;
            }
            var target = positional[^1];
            if (Path.GetExtension(target).Length == 0)
                target += ".HOK";
            foreach (var file in new[] { target, ems }.OfType<string>())
                if (File.Exists(file) && !force)
                    throw new ArgumentException($"{file} gibt es schon (--force zum Überschreiben).");

            var data = GameData.Locate(gameData);
            var rawDatabase = File.ReadAllBytes(data.GraphicsPath(23));
            GameState game;
            if (newName is not null)
            {
                var setup = new GameSetup();
                setup.AddManager(new ManagerSetup { Name = newName, ClubIndex = newClub });
                game = NewGame.Create(ClubDatabase.Parse(rawDatabase), setup, seed);
                for (var d = 0; d < matchDays && !game.RegularSeasonOver; d++)
                {
                    var played = game.PlayedMatchDays;
                    while (game.PlayedMatchDays == played && game.Phase == SeasonPhase.Running && !game.RegularSeasonOver)
                        game.Continue();
                }
                if (ems is not null)
                    File.WriteAllBytes(ems, SaveGame.Save(game));
            }
            else
            {
                var loaded = SaveGame.Load(File.ReadAllBytes(positional[0]));
                game = loaded.Game ?? throw new ArgumentException($"{positional[0]}: {loaded.Error}");
            }

            var bytes = OriginalExport.Export(game, rawDatabase, seed);
            var check = OriginalSaveGame.Read(bytes);
            if (!check.ChecksumsValid)
                throw new InvalidOperationException("Prüfsumme des Exports falsch (Fehler im Schreiber).");
            File.WriteAllBytes(target, bytes);
            var manager = game.Managers[0];
            Console.WriteLine($"Exportiert: → {target} ({bytes.Length} Byte, Prüfsummen gültig)");
            Console.WriteLine($"  {game.DateText}, Saison {game.Season}, Spieltag {game.PlayedMatchDays + 1}, Manager {game.Managers.Count}, " +
                              $"„{manager.Name}“ ({game.Clubs[manager.ClubIndex].Name}), Konto {manager.Money} DM, Saat {seed}");
            if (ems is not null)
                Console.WriteLine($"  Remake-Stand: {ems}");
            return 0;
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or OverflowException or InvalidOperationException
                                       or InvalidDataException or IOException)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static string Next(IReadOnlyList<string> args, ref int i) =>
        ++i < args.Count ? args[i] : throw new ArgumentException($"{args[i - 1]} braucht einen Wert.");
}
