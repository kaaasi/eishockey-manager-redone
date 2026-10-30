using EishockeyManager.Formats;

namespace EishockeyManager.Tools;

/// <summary>
/// <c>emtool hok patch &lt;vorlage&gt; &lt;ziel&gt; [--manager n] [--id n] [--force] feld=wert …</c>: aus einem
/// Original-Spielstand einen veränderten erzeugen, den das DOS-Original laden kann. Nur belegte Felder
/// (<c>emtool hok patch</c> ohne Argumente listet sie); Prüfsummen neu, neue Spielstand-ID.
/// Aufbau: <c>docs/formate/spielstand-original.md</c>.
/// </summary>
internal static class HokPatch
{
    public static int Run(IReadOnlyList<string> args)
    {
        if (args.Count < 2)
        {
            Console.Error.WriteLine("Aufruf: emtool hok patch <vorlage> <ziel> [--manager n] [--id n] [--force] feld=wert …");
            Console.Error.WriteLine("Felder:");
            foreach (var f in OriginalSavePatcher.Fields)
                Console.Error.WriteLine($"  {f.Syntax,-28} {f.Description}");
            return 1;
        }

        string source = args[0], target = args[1];
        var manager = 0;
        int? id = null;
        var force = false;
        var changes = new List<KeyValuePair<string, string>>();
        try
        {
            for (var i = 2; i < args.Count; i++)
            {
                switch (args[i])
                {
                    case "--manager": manager = int.Parse(Next(args, ref i)); break;
                    case "--id": id = int.Parse(Next(args, ref i)); break;
                    case "--force": force = true; break;
                    default:
                        var eq = args[i].IndexOf('=');
                        if (eq <= 0)
                            throw new ArgumentException($"„{args[i]}“ ist weder Option noch feld=wert.");
                        changes.Add(new(args[i][..eq], args[i][(eq + 1)..]));
                        break;
                }
            }
            if (Path.GetFullPath(source) == Path.GetFullPath(target))
                throw new ArgumentException("Ziel darf nicht die Vorlage sein.");
            if (File.Exists(target) && !force)
                throw new ArgumentException($"{target} gibt es schon (--force zum Überschreiben).");

            var template = File.ReadAllBytes(source);
            var result = OriginalSavePatcher.Patch(template, changes, manager, id);
            File.WriteAllBytes(target, result);

            var saved = OriginalSaveGame.Read(result);
            Console.Write(HokDump.Describe(saved));
            Console.WriteLine();
            Console.WriteLine($"Geschrieben: {target} ({result.Length} Byte), neue Spielstand-ID {saved.SaveId}, Prüfsummen {(saved.ChecksumsValid ? "gültig" : "FALSCH")}.");
            return saved.ChecksumsValid ? 0 : 3;
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or OverflowException or InvalidDataException)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static string Next(IReadOnlyList<string> args, ref int i) =>
        ++i < args.Count ? args[i] : throw new ArgumentException($"{args[i - 1]} braucht einen Wert.");
}
