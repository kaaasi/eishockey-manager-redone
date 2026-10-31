using System.Text;
using EishockeyManager.Formats;

namespace EishockeyManager.Tools;

/// <summary>
/// <c>emtool hok dump &lt;datei&gt; [block …]</c>: Original-Spielstand entschlüsseln, Prüfsummen prüfen, bekannte
/// Felder benennen. Mit Blocknamen (z. B. <c>965B</c>, <c>far:9657</c>) zusätzlich den Klartext dieser Blöcke als
/// Hexdump. Nur Lesen; Aufbau in <c>docs/formate/spielstand-original.md</c>.
/// </summary>
internal static class HokDump
{
    public static int Run(IReadOnlyList<string> args)
    {
        if (args.Count >= 1 && args[0].Equals("patch", StringComparison.OrdinalIgnoreCase))
            return HokPatch.Run(args.Skip(1).ToList());
        if (args.Count < 2 || !args[0].Equals("dump", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("Aufruf: emtool hok dump <datei> [block …] | emtool hok patch <vorlage> <ziel> feld=wert …");
            return 1;
        }

        var save = OriginalSaveGame.Read(File.ReadAllBytes(args[1]));
        Console.Write(Describe(save));
        foreach (var name in args.Skip(2))
        {
            var entry = save.Blocks.FirstOrDefault(b => b.Block.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (entry.Data is null)
            {
                Console.Error.WriteLine($"Block {name} unbekannt.");
                return 1;
            }
            Console.WriteLine();
            Console.WriteLine($"Block {entry.Block.Name} (Datei 0x{entry.FileOffset:X5}, {entry.Data.Length} Byte):");
            Console.Write(HexDump(entry.Data));
        }
        return save.ChecksumsValid ? 0 : 3;
    }

    /// <summary>Text der Übersicht (ohne Hexdumps).</summary>
    public static string Describe(OriginalSaveGame save)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Kennung {OriginalSaveGame.Magic}, Kopf: Jahr {save.HeaderYear}, Manager {save.HeaderManagerCount}, Byte 0x7A9 = {save.HeaderByte7A9}");
        foreach (var m in save.Managers)
            sb.AppendLine($"  Kopf-Manager „{m.Name}“, Verein „{m.Club}“, Start-Liga {m.StartLeague}{(m.OnlyWorldMode ? ", nur Weltkugel" : "")}");
        sb.AppendLine($"Spielstand-ID {save.SaveId}, Füllbyte 0x{save.Filler:X2}, Startschlüssel 0x{save.Key:X2}");
        foreach (var c in save.Checksums)
            sb.AppendLine($"Prüfsumme bei 0x{c.FileOffset:X5}: gelesen 0x{c.Stored:X2}, berechnet 0x{c.Computed:X2} → {(c.IsValid ? "gültig" : "FALSCH")}");

        var day = I32(save.Block("5A20"), 0);
        var month = I32(save.Block("5A1C"), 0);
        var year = I32(save.Block("5A18"), 0);
        sb.AppendLine($"Datum {day:00}.{month + 1:00}.{year}, Spielende {I16(save.Block("5A14"), 0)}, Saisonzähler {I16(save.Block("03BD"), 0)}");
        var setup = save.Block("0118");
        sb.AppendLine($"Regler Level {setup[0]}, Dauer {setup[1]}, Start-Liga {setup[2]}, Start-Stärke {setup[3]}; 5 − Level = {save.Block("0ED8")[0]}");
        sb.AppendLine($"Spielmodus 0xD0B9 = {save.Block("D0B9")[0]}, Manager am Zug {save.Block("A70E")[0]}, Preisindex {I32(save.Block("02BC"), 0)}, Konjunktur {I32(save.Block("02C0"), 0)}");

        var count = save.Block("0386")[0];
        var managers = save.Block("965B");
        var clubs = save.Block("far:9657");
        for (var i = 0; i < count && i < 4; i++)
        {
            var r = managers.AsSpan(i * OriginalSaveGame.ManagerRecordLength, OriginalSaveGame.ManagerRecordLength).ToArray();
            var club = r[0x14];
            var clubName = club < 150 ? GameText.Decode(clubs.AsSpan(club * OriginalSaveGame.ClubRecordLength, 0x16)) : "–";
            var konto = I32(r, 0x97EA - 0x965B);
            sb.AppendLine($"Manager {i}: „{GameText.Decode(r.AsSpan(0, 0x13))}“, Verein {club} {clubName}, Liga-Stufe {r[0x9746 - 0x965B]}");
            sb.AppendLine($"  Konto {konto} Drittel = {konto / 3} DM, Eintritt {r[0x973F - 0x965B]} DM");
            var stadium = 0x9771 - 0x965B;
            sb.AppendLine($"  Stadion-Stufen {string.Join(" ", Enumerable.Range(0, 9).Select(n => I16(r, stadium + 2 * n)))}, " +
                          $"Sitzplätze {I16(r, stadium + 0x1D)}, Stehplätze {I16(r, stadium + 0x21)}");
        }

        for (var record = 0; record <= 4; record++)
        {
            if (record < 4 && record >= count)
                continue;
            var squad = save.Squad(record);
            sb.AppendLine(record == 4 ? $"Transfermarkt ({squad.Count}):" : $"Kader Manager {record} ({squad.Count}):");
            foreach (var p in squad)
            {
                var line = p.Line == 0 ? "–" : $"{p.Line}{(p.Forward ? "S" : "A")}";
                sb.AppendLine($"  {p.Slot + 1,2} {p.Name,-17} {p.Strength1,2} {p.Strength2,2} {p.Strength3,2}  Alter {p.Age,2} Talent {p.Talent} " +
                              $"Rolle {p.RoleValue,2}  SP {p.Games} TO {(p.IsGoalkeeper ? "-" : p.Goals.ToString())} 2m {p.TwoMinutePenalties} 5m {p.FiveMinutePenalties} E {p.E,2}  {(p.Dressed ? "eingesetzt" : "          ")} Reihe {line,-2} " +
                              $"Vertrag {p.ContractYears} J.{(p.Insured ? " (v)" : "")}, Gehalt {p.SalaryThirds / 3} DM, Preis {p.PriceThirds / 3} DM");
            }
        }

        sb.AppendLine();
        sb.AppendLine("Blöcke (Datei-Offset, DGROUP, Länge, Deutung):");
        foreach (var (block, offset, data) in save.Blocks)
            sb.AppendLine($"  0x{offset:X5}  {block.Name,-9} {data.Length,5}  {block.Meaning}");
        return sb.ToString();
    }

    private static string HexDump(byte[] data)
    {
        var sb = new StringBuilder();
        for (var o = 0; o < data.Length; o += 16)
        {
            var line = data.AsSpan(o, Math.Min(16, data.Length - o));
            sb.Append($"{o:X4}  ");
            foreach (var b in line)
                sb.Append($"{b:X2} ");
            sb.Append(' ', 3 * (16 - line.Length) + 1);
            foreach (var b in line)
                sb.Append(b is >= 0x20 and < 0x7F ? (char)b : '.');
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private static int I32(ReadOnlySpan<byte> b, int o) => BitConverter.ToInt32(b.Slice(o, 4));
    private static short I16(ReadOnlySpan<byte> b, int o) => BitConverter.ToInt16(b.Slice(o, 2));
}
