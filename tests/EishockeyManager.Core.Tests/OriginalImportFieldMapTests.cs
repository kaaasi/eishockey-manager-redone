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

    private static string Document()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "EishockeyManager.slnx")))
            directory = directory.Parent;
        Assert.NotNull(directory);
        return File.ReadAllText(Path.Combine(directory.FullName, "docs", "formate", "spielstand-original.md"));
    }

    /// <summary>Tabellenzeile <c>| `Typ.Feld` | S | Quelle | Import |</c>.</summary>
    [GeneratedRegex(@"^\| `([A-Za-z_][\w<>,]*\.[A-Za-z_]\w*)` \|([^|]*)\|([^|]*)\|(.*)\|$")]
    private static partial Regex MapRow();

    /// <summary>Erster Blockverweis der Quelle: <c>`965B`</c>, <c>`far:8253`</c> oder <c>`Kopf`</c>.</summary>
    [GeneratedRegex(@"`(Kopf|far:[0-9A-F]{4}|[0-9A-F]{4})[`+]")]
    private static partial Regex BlockReference();
}
