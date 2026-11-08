using System.Buffers.Binary;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using Xunit.Abstractions;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur hok-import, Teil 1: Stichproben der Feldkarte (<c>docs/formate/spielstand-original.md</c>) an den Original-Ständen.
/// Geprüft werden die Grundregeln, auf denen der Import aufbaut: Vereinszuordnung über den Namen (Regel 1), Satzplätze je
/// Liga (Regel 2) und die Versätze im Managersatz, die die Karte neu ausrechnet (Konto, Bank, Stadion).
/// </summary>
public sealed class OriginalImportFieldMapDataTests(ITestOutputHelper output)
{
    private const int ClubRecord = OriginalSaveGame.ClubRecordLength;

    /// <summary>
    /// Regel 1: Jeder der 150 Vereinssätze trägt den Namen genau eines Datenbankvereins. Einzige Ausnahme ist „nur Weltkugel“:
    /// der Satz des Managervereins heißt dann wie das Land (FILE11, Kopfroutine 0x5B556), sein Datenbankverein ist der
    /// eine Name, der im Satzbestand fehlt.
    /// </summary>
    [GameDataTheory]
    [InlineData("FILE05.HOK", false)]
    [InlineData("FILE11.HOK", true)]
    [InlineData("FILE16A.HOK", false)]
    [InlineData("FILE29.HOK", false)]
    public void ClubRecords_MapToDatabaseByName(string file, bool onlyWorld)
    {
        var database = ClubDatabase.Load(TestGameData.Load());
        var save = OriginalSaveGame.Read(File.ReadAllBytes(OriginalSave(file)));
        var clubs = save.Block("far:9657");
        var names = Enumerable.Range(0, ClubDatabase.TotalClubs).Select(slot => Name(clubs.AsSpan(slot * ClubRecord, 0x16))).ToList();
        var known = database.Clubs.Select(c => c.Name).ToList();
        Assert.Equal(known.Count, known.Distinct().Count());

        var missing = known.Except(names).ToList();
        var duplicates = names.GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        output.WriteLine($"{file}: fehlend {string.Join(", ", missing)}; doppelt {string.Join(", ", duplicates)}");
        Assert.All(names, n => Assert.Contains(n, known));
        if (!onlyWorld)
        {
            Assert.Empty(missing);
            Assert.Empty(duplicates);
            return;
        }
        // Managerverein (Satz aus 965B+0x14) heißt wie das Land 06AC; genau ein Datenbankname fehlt.
        var managerSlot = save.Block("965B")[0x14];
        var nation = save.Block("06AC")[0];
        Assert.Equal(names[nation], names[managerSlot]);
        Assert.Single(missing);
        Assert.Equal([names[nation]], duplicates);
    }

    /// <summary>Regel 2: Satzplätze 0–11, 12–23, 24–35 sind die drei Ligen; die Tabelle 5CCB nennt nur Plätze ihrer Liga.</summary>
    [GameDataTheory]
    [InlineData("FILE05.HOK")]
    [InlineData("FILE16A.HOK")]
    [InlineData("FILE29.HOK")]
    public void LeagueTable_UsesSlotsOfItsLeague(string file)
    {
        var table = OriginalSaveGame.Read(File.ReadAllBytes(OriginalSave(file))).Block("5CCB");
        for (var league = 0; league < 3; league++)
            Assert.Equal(Enumerable.Range(12 * league, 12), table.AsSpan(12 * league, 12).ToArray().Select(b => (int)b).Order());
    }

    /// <summary>
    /// Versätze im Managersatz (Karte, Spalte HOK-Quelle): Konto +0x18F, Darlehen Bank +0x1AB (= 0x97F6 + 4 · 4),
    /// Bank-Satz 4 Konto, Stadion +0x116 mit Sitz +0x133 und Steh +0x137, Eintritt +0xE4. Werte aus spielstand-original.md.
    /// </summary>
    [GameDataTheory]
    [InlineData("FILE01.HOK", 1_500_000, 0, 13, 3_000, 4_500)]
    [InlineData("FILE08.HOK", 2_212_062 + 2_103_000, -2_103_000, 17, 3_000, 4_500)]
    [InlineData("FILE09.HOK", 1_200_000, 0, 13, 4_200, 7_000)]
    public void ManagerRecordOffsets_MatchDocumentedValues(string file, int account, int bankLoan, int ticket, int seats, int standing)
    {
        var block = OriginalSaveGame.Read(File.ReadAllBytes(OriginalSave(file))).Block("965B");
        var managers = block.AsSpan();
        Assert.Equal(account, BinaryPrimitives.ReadInt32LittleEndian(managers[0x18F..]));
        Assert.Equal(bankLoan, BinaryPrimitives.ReadInt32LittleEndian(managers[0x1AB..]));
        Assert.Equal(-bankLoan, BinaryPrimitives.ReadInt32LittleEndian(managers[(4 * OriginalSaveGame.ManagerRecordLength + 0x19B)..]));
        Assert.Equal(ticket, managers[0xE4]);
        Assert.Equal(seats, BinaryPrimitives.ReadInt16LittleEndian(managers[0x133..]));
        Assert.Equal(standing, BinaryPrimitives.ReadInt16LittleEndian(managers[0x137..]));
        Assert.Equal([2, 1, 1, 0, 2, 1, 1, 0, 0], Enumerable.Range(0, 9).Select(i => (int)block[0x116 + 2 * i]));
    }

    private static string Name(ReadOnlySpan<byte> bytes)
    {
        var end = bytes.IndexOf((byte)0);
        return GameText.Decode(end < 0 ? bytes : bytes[..end]);
    }

    /// <summary>Original-Spielstand: <c>EM_SAVEGAMES</c> oder <c>spielstaende</c> neben dem aufgelösten <c>gamedata</c>.</summary>
    private static string OriginalSave(string name)
    {
        var folder = Environment.GetEnvironmentVariable("EM_SAVEGAMES");
        if (string.IsNullOrEmpty(folder))
        {
            var root = TestGameData.Load().RootDirectory;
            var real = Directory.ResolveLinkTarget(root.TrimEnd('/', '\\'), true)?.FullName ?? root;
            folder = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(real).TrimEnd('/', '\\'))!, "spielstaende");
        }
        var path = Path.Combine(folder, name);
        return File.Exists(path) ? path
            : throw new FileNotFoundException($"Original-Spielstand {name} fehlt in {folder} (Ordner per EM_SAVEGAMES angeben).");
    }
}
