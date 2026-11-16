using System.Buffers.Binary;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur hok-import-werte (Runde 21, N4 = C): Werte, die das HOK führt und der Import bisher verwarf. A607 Gehalt der Marktplätze
/// (Kaderplatz Satz 4, +0x28), A624 Gegentore des Torhüters (+0x03; Datei 0xA3F7).
/// </summary>
public sealed class HokImportWerteTests
{
    private const int Seed = 4711;
    private const int SquadRecord = 0x618;

    public static TheoryData<string> Saves() => OriginalExportRoundTripTests.Saves();

    /// <summary>
    /// A607: Jedes Poolangebot trägt das Gehalt +0x28 / 3 seines Marktplatzes. [ORIGINAL] Datei 0x497A1 (Listenwechsel 0x494EF)
    /// schreibt +0x28 = Wert(Gehalt). Vor dem Fix setzte der Import es auf 0 (rot an jedem Stand mit Markt). Der Vertrag bleibt 0:
    /// +0x0B ist 0 oder nach dem Herunterzählen am Saisonende 0xFF (Datei 0x27500).
    /// </summary>
    [GameDataTheory]
    [MemberData(nameof(Saves))]
    public void Import_MarketListing_KeepsSalaryOfSlot(string file)
    {
        var (save, game) = Import(file);
        var squad = save.Block("5DC3");
        var byPlayer = game.TransferMarket.Listings.Where(l => l.FromPool).ToDictionary(l => l.Player);
        var checkedSlots = 0;
        for (var k = 0; k < OriginalSaveGame.SquadSlotsPerManager; k++)
        {
            var s = squad.AsSpan(OriginalImport.MarketRecord * SquadRecord + k * OriginalSaveGame.SquadSlotLength,
                OriginalSaveGame.SquadSlotLength);
            int index = s[0x0F];
            if (index == 0 || OriginalImport.PoolEntry(save, index)[0x1A] != OriginalImport.PoolOwnerMarket)
                continue;
            var player = game.TransferMarket.Pool[index - 1].Player;
            Assert.True(byPlayer.ContainsKey(player), $"{file} Marktplatz {k}: kein Angebot");
            Assert.True(s[0x0B] is 0 or 0xFF, $"{file} Marktplatz {k}: +0x0B = {s[0x0B]}");
            Assert.Equal(0, player.ContractYears);
            Assert.Equal(BinaryPrimitives.ReadInt32LittleEndian(s[0x28..]) / 3, player.MonthlySalary);
            Assert.NotEqual(0, player.MonthlySalary); // an allen Ständen belegt
            checkedSlots++;
        }
        Assert.True(checkedSlots > 0, $"{file}: kein Marktplatz geprüft");
    }

    /// <summary>
    /// A607, Gegenprobe mit Nutzerangabe: FILE05 DAFFNER auf dem Markt mit 9.900 DM Gehalt; nach dem Kauf (FILE07, Kaderplatz 25)
    /// steht dasselbe Gehalt im Kader (Wert(Gehalt) beim Kauf, Datei 0x42F11–0x42F65).
    /// </summary>
    [GameDataFact]
    public void Import_File05_Daffner_SalaryAsOnMarket()
    {
        var (_, game) = Import("FILE05.HOK");
        var daffner = game.TransferMarket.Listings.Single(l => l.Player.Name == "DAFFNER").Player;
        Assert.Equal(9_900, daffner.MonthlySalary);
        Assert.Equal(0, daffner.ContractYears);
        var (_, bought) = Import("FILE07.HOK");
        Assert.Equal(9_900, bought.Clubs[bought.Managers[0].ClubIndex].Players.Single(p => p.Name == "DAFFNER").MonthlySalary);
    }

    /// <summary>
    /// A624: An allen Ständen sind +0x04/+0x05 und +0x22–0x27 jedes Torhüters in den Managerkadern und auf dem Markt 0 (Datei 0xA3F7
    /// zählt nur +0x03, nur in der Liga); die Gegentore des Imports sind +0x03.
    /// </summary>
    [GameDataTheory]
    [MemberData(nameof(Saves))]
    public void Import_Goalkeeper_GoalsAgainstOnlyInLeagueField(string file)
    {
        var (save, game) = Import(file);
        var squad = save.Block("5DC3");
        for (var record = 0; record <= OriginalImport.MarketRecord; record++)
        {
            for (var k = 0; k < OriginalSaveGame.SquadSlotsPerManager; k++)
            {
                var s = squad.AsSpan(record * SquadRecord + k * OriginalSaveGame.SquadSlotLength, OriginalSaveGame.SquadSlotLength);
                int index = s[0x0F];
                if (index == 0 || OriginalImport.PoolEntry(save, index)[0x18] != 0)
                    continue;
                Assert.True(s[0x04] == 0 && s[0x05] == 0 && s[0x22..0x28].IndexOfAnyExcept((byte)0) < 0,
                    $"{file} Satz {record} Platz {k}: Torhüter mit Werten außerhalb +0x03");
            }
        }
        if (OriginalImport.Mode(save) == GameMode.WorldOnly)
            return; // Kader des Managervereins übernimmt der Import nicht (Regel 1)
        for (var m = 0; m < game.Managers.Count; m++)
        {
            var club = game.Clubs[game.Managers[m].ClubIndex];
            for (var k = 0; k < OriginalSaveGame.SquadSlotsPerManager; k++)
            {
                var s = squad.AsSpan(m * SquadRecord + k * OriginalSaveGame.SquadSlotLength, OriginalSaveGame.SquadSlotLength);
                int index = s[0x0F];
                if (index == 0 || OriginalImport.PoolEntry(save, index)[0x18] != 0)
                    continue;
                var keeper = game.TransferMarket.Pool[index - 1].Player;
                if (!club.Players.Contains(keeper))
                    continue; // nur auf dem Markt-Platz (A380)
                Assert.Equal(s[0x03], keeper.GoalsAgainst);
            }
        }
    }

    /// <summary>
    /// A624, synthetisch (Originalstände haben keinen Fall): Ein Wert in +0x04 eines Torhüters zählt nicht als Gegentor. Vor dem Fix
    /// summierte der Import +0x03–0x05 (rot: 13 + 5 statt 13).
    /// </summary>
    [GameDataFact]
    public void Import_Goalkeeper_IgnoresCupFieldAsGoalsAgainst()
    {
        var save = OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave("FILE05.HOK")));
        var squad = save.Block("5DC3");
        var k = Enumerable.Range(0, OriginalSaveGame.SquadSlotsPerManager)
            .First(i => squad[i * OriginalSaveGame.SquadSlotLength + 0x0F] is var index and > 0
                        && OriginalImport.PoolEntry(save, index)[0x18] == 0 && squad[i * OriginalSaveGame.SquadSlotLength + 0x03] > 0);
        var at = k * OriginalSaveGame.SquadSlotLength;
        int expected = squad[at + 0x03];
        squad[at + 0x04] = 5;
        var game = GameState.ImportOriginal(save, ClubDatabase.Load(TestGameData.Load()), Seed);
        var keeper = game.TransferMarket.Pool[squad[at + 0x0F] - 1].Player;
        Assert.Equal(expected, keeper.GoalsAgainst);
        Assert.Equal(0, keeper.CupGoals);
    }

    private static (OriginalSaveGame Save, GameState Game) Import(string file)
    {
        var save = OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave(file)));
        return (save, GameState.ImportOriginal(save, ClubDatabase.Load(TestGameData.Load()), Seed));
    }
}
