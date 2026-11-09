using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using Xunit.Abstractions;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Integration Runde 14 (<c>hok-import</c> auf <c>main</c> mit Schema 41): Der Import deckt die Felder ab, die <c>nationalteam</c>
/// (Schema 41) nach der Feldkarte ergänzt hat, und alle Original-Stände überstehen die Rundreise Remake-Speichern/Laden.
/// </summary>
public sealed class OriginalImportRunde14Tests(ITestOutputHelper output)
{
    private const int Seed = 4711;

    /// <summary>
    /// A009: „automatisch aufstellen“ des Bundestrainers liegt wie das der Manager im Block 0375 (DGROUP 0x37F aktuell, 0x380
    /// gesichert; <c>0x375 + 2 · 5</c>). Der Import übernimmt den gesicherten Wert (Zug nicht offen, wie nach der Migration).
    /// </summary>
    [GameDataFact]
    public void Import_TakesNationalCoachAutoLineupFromBlock0375()
    {
        var database = ClubDatabase.Load(TestGameData.Load());
        var files = AllStands();
        var withCoach = 0;
        var off = 0;
        foreach (var path in files)
        {
            var save = OriginalSaveGame.Read(File.ReadAllBytes(path));
            var saved = save.Block("0375")[0x0B] != 0;
            var game = GameState.ImportOriginal(save, database, Seed);
            var name = Path.GetFileName(path);
            if (!saved)
                off++;
            if (game.NationalCoach is not { } coach)
                continue;
            withCoach++;
            Assert.True(saved == coach.AutoLineupEnabled, $"{name}: Bundestrainer „automatisch aufstellen“ {coach.AutoLineupEnabled}, HOK 0x380 = {saved}");
        }
        output.WriteLine($"{files.Count} Stände, davon {withCoach} mit Bundestrainer, {off} mit 0x380 = 0");
        Assert.True(withCoach > 0);
    }

    /// <summary>Jeder Original-Stand: laden, speichern, laden, speichern – gleiche Bytes; danach ein Spieltag ohne Ausnahme.</summary>
    [GameDataFact]
    public void Import_AllStands_RoundTripIsStable()
    {
        var database = ClubDatabase.Load(TestGameData.Load());
        var rows = new List<string> { "| Stand | Datum | Modus | Manager | Rundreise | Spieltag |", "|---|---|---|---:|---|---|" };
        foreach (var path in AllStands())
        {
            var save = OriginalSaveGame.Read(File.ReadAllBytes(path));
            var game = GameState.ImportOriginal(save, database, Seed);
            var first = SaveGame.Save(game);
            var loaded = SaveGame.Load(first);
            Assert.True(loaded.Game is not null, $"{Path.GetFileName(path)}: Laden scheitert: {loaded.Error}");
            var second = SaveGame.Save(loaded.Game!);
            Assert.True(first.AsSpan().SequenceEqual(second), $"{Path.GetFileName(path)}: Rundreise verändert den Spielstand");
            var day = loaded.Game!.CurrentDay;
            for (var guard = 0; loaded.Game.CurrentDay == day && guard < 100; guard++)
                loaded.Game.Continue();
            Assert.Empty(loaded.Game.ValidatePlayerIdentity());
            rows.Add($"| {Path.GetFileName(path)} | {game.DateText} | {game.Mode} | {game.Managers.Count} | stabil | ok |");
        }
        foreach (var row in rows)
            output.WriteLine(row);
    }

    private static List<string> AllStands()
    {
        var folder = Path.GetDirectoryName(OriginalImportTests.OriginalSave("FILE01.HOK"))!;
        var files = Directory.GetFiles(folder, "FILE*.HOK").Order(StringComparer.Ordinal).ToList();
        Assert.True(files.Count >= 40, $"nur {files.Count} Stände in {folder}");
        return files;
    }
}
