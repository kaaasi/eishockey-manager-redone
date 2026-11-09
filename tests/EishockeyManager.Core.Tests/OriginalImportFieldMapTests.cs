using System.Text.RegularExpressions;
using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur hok-import, Teil 1: Die Feldkarte in <c>docs/formate/spielstand-original.md</c> (Abschnitt „Feldkarte
/// Remake-Spielstand ↔ HOK“) ordnet jedes Feld des Remake-Spielstands genau einmal einem HOK-Feld zu. Ändert sich das
/// Schema, wird dieser Test rot, bis die Karte nachgezogen ist – der Import (Teil 2) richtet sich nach ihr.
/// </summary>
public partial class OriginalImportFieldMapTests
{
    private const string Heading = "## Feldkarte Remake-Spielstand (Schema ";

    private static readonly string[] Statuses = ["B", "A", "N", "U"];

    /// <summary>Zeile der Karte: Feld, Status, HOK-Quelle, Import.</summary>
    private sealed record Row(string Field, string Status, string Source, string Import, int Line);

    [Fact]
    public void FieldMap_NamesSchemaVersion()
    {
        var text = Document();
        var start = text.IndexOf(Heading, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Abschnitt „{Heading}…“ fehlt in spielstand-original.md.");
        var version = text[(start + Heading.Length)..].Split(')')[0];
        Assert.Equal(SaveGame.SchemaVersion.ToString(System.Globalization.CultureInfo.InvariantCulture), version);
    }

    [Fact]
    public void FieldMap_CoversEverySchemaFieldExactlyOnce()
    {
        var rows = Rows();
        var schema = SchemaFields();
        var duplicates = rows.GroupBy(r => r.Field).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        Assert.True(duplicates.Count == 0, "Doppelt in der Feldkarte: " + string.Join(", ", duplicates));
        var mapped = rows.Select(r => r.Field).ToHashSet(StringComparer.Ordinal);
        var missing = schema.Where(f => !mapped.Contains(f)).ToList();
        Assert.True(missing.Count == 0, "Fehlt in der Feldkarte: " + string.Join(", ", missing));
        var unknown = mapped.Where(f => !schema.Contains(f)).Order(StringComparer.Ordinal).ToList();
        Assert.True(unknown.Count == 0, "Nicht im Schema: " + string.Join(", ", unknown));
    }

    [Fact]
    public void FieldMap_RowsHaveValidStatusAndSource()
    {
        var blocks = OriginalSaveGame.Layout.Where(b => !b.IsChecksum).Select(b => b.Name).ToHashSet(StringComparer.Ordinal);
        foreach (var row in Rows())
        {
            Assert.True(Statuses.Contains(row.Status), $"Zeile {row.Line} ({row.Field}): Status „{row.Status}“.");
            Assert.False(string.IsNullOrWhiteSpace(row.Import), $"Zeile {row.Line} ({row.Field}): Importregel fehlt.");
            if (row.Status is "B" or "A")
            {
                // Belegte und abgeleitete Felder nennen ihre Quelle: einen Block des Stroms oder den Klartextkopf.
                var match = BlockReference().Match(row.Source);
                Assert.True(match.Success, $"Zeile {row.Line} ({row.Field}): Quelle ohne Block („{row.Source}“).");
                var name = match.Groups[1].Value;
                Assert.True(name == "Kopf" || blocks.Contains(name),
                    $"Zeile {row.Line} ({row.Field}): Block „{name}“ gibt es im Spielstand nicht.");
            }
        }
    }

    /// <summary>
    /// Review R14HI-04: Der Status B/A muss zum Code passen. Jede Zeile mit Status B/A nennt Blöcke (und Versätze) als Quelle; jeder
    /// genannte Block muss im Importcode (<c>Core/OriginalImport*.cs</c>, Kopf: <c>Formats/OriginalSaveHeader.cs</c>) gelesen werden –
    /// als <c>Block("…")</c> bzw. als Zeichenkette – und jeder Versatz <c>+0x…</c> der Quelle als Hexzahl im Importcode vorkommen.
    /// Vorher führte die Karte z. B. `D0C1`, `CD58`, `0387` und `965B+0x117` als belegt, ohne dass ein Import sie las.
    /// </summary>
    [Fact]
    public void FieldMap_ImportedRows_HaveTheirBlocksAndOffsetsInTheImportCode()
    {
        var code = ImportCode();
        var blocksInCode = CodeBlockLiteral().Matches(code).Select(m => m.Groups[1].Value).ToHashSet(StringComparer.Ordinal);
        var hexInCode = HexLiteral().Matches(code).Select(m => Normalize(m.Value)).ToHashSet(StringComparer.Ordinal);
        var problems = new List<string>();
        foreach (var row in Rows().Where(r => r.Status is "B" or "A"))
        {
            foreach (Match reference in SourceReference().Matches(row.Source))
            {
                var block = reference.Groups[1].Value;
                if (block != "Kopf" && !blocksInCode.Contains(block))
                    problems.Add($"Zeile {row.Line} ({row.Field}): Block `{block}` liest kein Import.");
                foreach (Capture offset in reference.Groups[2].Captures)
                    if (!hexInCode.Contains(Normalize(offset.Value)))
                        problems.Add($"Zeile {row.Line} ({row.Field}): Versatz {offset.Value} von `{block}` steht in keinem Import.");
            }
        }
        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    private static string ImportCode()
    {
        var root = RepositoryRoot();
        var files = Directory.GetFiles(Path.Combine(root, "src", "EishockeyManager.Core"), "OriginalImport*.cs")
            .Append(Path.Combine(root, "src", "EishockeyManager.Formats", "OriginalSaveHeader.cs"));
        return string.Join("\n", files.Select(File.ReadAllText));
    }

    private static string Normalize(string hex) =>
        "0x" + Convert.ToInt32(hex.TrimStart('+')[2..], 16).ToString("X", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Alle Felder des Schemas („Typ.Feld“), ohne Aufzählungen, Implementierungszeilen und Tupelfelder.</summary>
    private static HashSet<string> SchemaFields() =>
        SaveGame.SchemaDescription()
            .Where(l => !l.Contains(" : ", StringComparison.Ordinal) && !l.Contains(" = ", StringComparison.Ordinal)
                && l.Contains(": ", StringComparison.Ordinal) && !l.StartsWith("ValueTuple<", StringComparison.Ordinal))
            .Select(l => l[..l.IndexOf(": ", StringComparison.Ordinal)])
            .ToHashSet(StringComparer.Ordinal);

    private static List<Row> Rows()
    {
        var lines = Document().Split('\n');
        var start = Array.FindIndex(lines, l => l.StartsWith(Heading, StringComparison.Ordinal));
        Assert.True(start >= 0, "Abschnitt Feldkarte fehlt.");
        var rows = new List<Row>();
        for (var i = start + 1; i < lines.Length && !lines[i].StartsWith("## ", StringComparison.Ordinal); i++)
        {
            var match = MapRow().Match(lines[i].TrimEnd('\r'));
            if (match.Success)
                rows.Add(new Row(match.Groups[1].Value, match.Groups[2].Value.Trim(), match.Groups[3].Value.Trim(),
                    match.Groups[4].Value.Trim(), i + 1));
        }
        Assert.NotEmpty(rows);
        return rows;
    }

    private static string Document() => File.ReadAllText(Path.Combine(RepositoryRoot(), "docs", "formate", "spielstand-original.md"));

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "EishockeyManager.slnx")))
            directory = directory.Parent;
        Assert.NotNull(directory);
        return directory.FullName;
    }

    /// <summary>Tabellenzeile <c>| `Typ.Feld` | S | Quelle | Import |</c>.</summary>
    [GeneratedRegex(@"^\| `([A-Za-z_][\w<>,]*\.[A-Za-z_]\w*)` \|([^|]*)\|([^|]*)\|(.*)\|$")]
    private static partial Regex MapRow();

    /// <summary>Erster Blockverweis der Quelle: <c>`965B`</c>, <c>`far:8253`</c> oder <c>`Kopf`</c>.</summary>
    [GeneratedRegex(@"`(Kopf|far:[0-9A-F]{4}|[0-9A-F]{4})[`+]")]
    private static partial Regex BlockReference();

    /// <summary>Alle Blockverweise der Quelle mit ihren Versätzen: <c>`965B+0x116+2i`</c> → Block `965B`, Versätze 0x116.</summary>
    [GeneratedRegex(@"`(Kopf|far:[0-9A-F]{4}|[0-9A-F]{4})(\+0x[0-9A-Fa-f]+)*[^`]*`")]
    private static partial Regex SourceReference();

    /// <summary>Blockname als Zeichenkette im Code: <c>Block("965B")</c>, <c>"far:9657"</c>.</summary>
    [GeneratedRegex(@"""(far:[0-9A-F]{4}|[0-9A-F]{4})""")]
    private static partial Regex CodeBlockLiteral();

    /// <summary>Hexzahl im Code.</summary>
    [GeneratedRegex(@"0x[0-9A-Fa-f]+")]
    private static partial Regex HexLiteral();
}
