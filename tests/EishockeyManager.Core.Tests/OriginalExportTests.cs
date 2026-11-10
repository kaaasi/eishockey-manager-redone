using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using Xunit.Abstractions;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur hok-export, Teil 1: Export eines Remake-Spielstands als Original-Spielstand <c>*.HOK</c> (<see cref="OriginalExport"/>).
/// Prüft Aufbau (Länge, Kennungen, Prüfsummen), die Lesbarkeit mit <see cref="OriginalSaveGame.Read"/> und den Import des
/// Ergebnisses; die Rundreise gegen die Stände des Originals folgt in Teil 2.
/// </summary>
public sealed class OriginalExportTests(ITestOutputHelper output)
{
    private const int Seed = 4711;

    /// <summary>Ein neues Spiel lässt sich exportieren; das Ergebnis hat Länge, Kennungen und gültige Prüfsummen des Originals.</summary>
    [GameDataFact]
    public void NewGame_ExportsReadableHok()
    {
        var database = ClubDatabase.Load(TestGameData.Load());
        var game = NewGameForExport(database);

        var bytes = OriginalExport.Export(game, RawDatabase(), Seed);
        var save = OriginalSaveGame.Read(bytes);

        Assert.Equal(OriginalSaveGame.FileLength, bytes.Length);
        Assert.True(save.ChecksumsValid);
        Assert.Equal(1, save.HeaderManagerCount);
        Assert.Equal("Joerg", save.Managers[0].Name);
        Assert.Equal(game.Clubs[game.Managers[0].ClubIndex].Name, save.Managers[0].Club);
        Assert.Equal(1, BitConverter.ToInt32(save.Block("03B7")));
        Assert.Equal(1993, save.HeaderYear);
    }

    /// <summary>Der Export verändert den Spielstand nicht und ist bei gleicher Saat gleich (keine Zufallszüge aus dem Spiel).</summary>
    [GameDataFact]
    public void Export_IsPure()
    {
        var database = ClubDatabase.Load(TestGameData.Load());
        var game = NewGameForExport(database);
        TestSeason.PlayMatchDay(game);
        var before = SaveGame.Save(game);

        var a = OriginalExport.Export(game, RawDatabase(), Seed);
        var b = OriginalExport.Export(game, RawDatabase(), Seed);

        Assert.Equal(a, b);
        Assert.Equal(before, SaveGame.Save(game));
    }

    /// <summary>Diagnose: Original-Stand → Import → Export, Abweichungen je Block (für Teil 2).</summary>
    [GameDataFact]
    public void Diagnose_RoundTrip()
    {
        foreach (var path in Directory.GetFiles(Path.GetDirectoryName(OriginalImportTests.OriginalSave("FILE01.HOK"))!, "FILE*.HOK").Order())
            Diagnose(Path.GetFileName(path));
    }

    private void Diagnose(string file)
    {
        var database = ClubDatabase.Load(TestGameData.Load());
        var original = OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave(file)));
        GameState game;
        try { game = GameState.ImportOriginal(original, database, Seed); }
        catch (InvalidDataException e) { output.WriteLine($"{file} Import: {e.Message}"); return; }
        var exported = OriginalSaveGame.Read(OriginalExport.Export(game, RawDatabase(), Seed));
        foreach (var (block, _, data) in original.Blocks)
        {
            var other = exported.Block(block.Name);
            var diff = Enumerable.Range(0, data.Length).Where(i => data[i] != other[i]).ToList();
            if (diff.Count > 0)
                output.WriteLine($"{file} {block.Name,-9} {diff.Count,5}/{data.Length,-5} " +
                                 string.Join(" ", diff.Take(12).Select(i => $"{i:X}:{data[i]:X2}/{other[i]:X2}")));
        }
    }

    internal static byte[] RawDatabase() => File.ReadAllBytes(TestGameData.Load().GraphicsPath(23));

    internal static GameState NewGameForExport(ClubDatabase database)
    {
        var setup = new GameSetup();
        setup.AddManager(new ManagerSetup { Name = "Joerg", Head = 6, ClubIndex = 2 });
        return NewGame.Create(database, setup, Seed);
    }
}
