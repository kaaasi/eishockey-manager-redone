using System.Buffers.Binary;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Integration Runde 14, kader-start nach dem Merge von #670 (hok-import): Der Import der Original-Stände füllt die Felder der
/// Schemas 42/43 (Start-Liga ohne Manager, Tabellenfeld A046, Zähler je Wettbewerb A135) aus dem HOK.
/// </summary>
public class KaderStartRunde14ImportTests
{
    private static (OriginalSaveGame Save, GameState Game, int[] ClubOf) Import(string file)
    {
        var save = OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave(file)));
        var database = ClubDatabase.Load(TestGameData.Load());
        return (save, GameState.ImportOriginal(save, database, 4711), OriginalImport.ClubIndices(save, database));
    }

    /// <summary>FILE39 (nach einem Pokalspiel): Spiele und Tore je Wettbewerb aus Kaderplatz +0x03–0x08 und +0x1C–0x26.</summary>
    [GameDataTheory]
    [InlineData("FILE39.HOK")]
    [InlineData("FILE40.HOK")]
    public void Import_CountersPerCompetition_FromSquadSlots(string file)
    {
        var (save, game, _) = Import(file);
        var club = game.Clubs[game.Managers[0].ClubIndex];
        var checkedCup = 0;
        for (var i = 0; i < club.Players.Count; i++)
        {
            var slot = OriginalImport.SquadSlot(save, 0, i).ToArray();
            var p = club.Players[i];
            int Word(int at) => BinaryPrimitives.ReadInt16LittleEndian(slot.AsSpan(at));
            Assert.Equal((slot[0x07], slot[0x08]), ((byte)p.CupGames, (byte)p.EuropeGames));
            Assert.Equal(slot[0x06], p.SeasonGames(CompetitionSlot.League));
            Assert.Equal(Word(0x1C), p.ClubGames(CompetitionSlot.League));
            Assert.Equal(Word(0x1E), p.ClubGames(CompetitionSlot.Cup));
            Assert.Equal(Word(0x20), p.ClubGames(CompetitionSlot.Europe));
            if (p.Role != PlayerRole.Goalkeeper)
            {
                Assert.Equal((slot[0x04], slot[0x05]), ((byte)p.CupGoals, (byte)p.EuropeGoals));
                Assert.Equal(Word(0x22), p.ClubGoals(CompetitionSlot.League));
                Assert.Equal(Word(0x24), p.ClubGoals(CompetitionSlot.Cup));
            }
            checkedCup += slot[0x07];
        }
        Assert.True(checkedCup > 0, "Stand ohne Pokalspiele");
    }

    /// <summary>
    /// FILE14 (vor dem 1. Spieltag der zweiten Saison): Tabellenfeld DGROUP 0x8288 = Platz der Vorsaison; Start-Liga der
    /// Spielvorbereitung (DGROUP 0x11A) auch im eigenen Feld der Spielvorbereitung (Schema 42).
    /// </summary>
    [GameDataFact]
    public void Import_TableRankAndStartLeague()
    {
        var (save, game, clubOf) = Import("FILE14.HOK");
        var table = save.Block("8257");
        var nonZero = 0;
        for (var slot = 0; slot < OriginalImport.TeamSlots; slot++)
        {
            if (clubOf[slot] >= OriginalImport.TeamSlots)
                continue;
            var value = table[OriginalImport.TableField(0x8288, slot)];
            Assert.Equal(value, game.Clubs[clubOf[slot]].StoredTableRank);
            nonZero += value > 0 ? 1 : 0;
        }
        Assert.True(nonZero > 0);
        var league = (League)save.Block("0118")[2];
        Assert.Equal(league, game.Setup.StartLeague);
        game.Setup.Managers.Clear();
        Assert.Equal(league, game.Setup.StartLeague); // eigenes Feld, nicht nur Manager 0
    }
}
