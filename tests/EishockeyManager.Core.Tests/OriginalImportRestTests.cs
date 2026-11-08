using System.Buffers.Binary;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur hok-import, Teil 4a (E14-4 = A): Bereiche, die der Import bisher aus dem Nachspielen nahm, kommen aus dem HOK – Börse,
/// Werbung, Training, Taktik, Serien und Rekorde, Strafminuten und Platzfelder, Über-/Unterzahl, Darlehen, Nationalkonto.
/// Werte je Feld: Feldkarte in <c>docs/formate/spielstand-original.md</c>.
/// </summary>
public sealed class OriginalImportRestTests
{
    private const int Seed = 4711;

    /// <summary>Börse FILE01: Kurse 84 / 119 / 135 / 147 DM (Bild des Nutzers), Basiswert, Tendenz und Verlauf aus D0F4/01FD/A891/A895.</summary>
    [GameDataFact]
    public void Import_StocksFromOriginal()
    {
        var (save, game) = Import("FILE01.HOK");
        Assert.Equal([84, 119, 135, 147], game.Stocks.Stocks.Select(s => s.Price));
        Assert.Equal([10273L, 11283, 13864, 17443], game.Stocks.Stocks.Select(s => s.Base));
        Assert.Equal([1, 7, 2, 2], game.Stocks.Stocks.Select(s => s.Countdown));
        Assert.Equal([0, 2, 0, 0], game.Stocks.Stocks.Select(s => s.Direction));
        Assert.All(game.Stocks.Stocks, s => Assert.Equal(Stock.HistoryLength, s.History.Count));
        Assert.Equal(219 / 3, game.Stocks.Stocks[0].History[0]);
        Assert.Equal(StockMarket.Defaults.Select(d => d.Name), game.Stocks.Stocks.Select(s => s.Name));
        _ = save;
    }

    /// <summary>Aktienbesitz D214 (an keinem Stand belegt): Kaufpreis und Stückzahl je Manager und Aktie.</summary>
    [GameDataFact]
    public void Import_StockHoldings()
    {
        var (save, database) = Load("FILE05.HOK");
        var block = save.Block("D214");
        BinaryPrimitives.WriteInt32LittleEndian(block.AsSpan(8 * 2), 30_000); // Manager 0, Aktie 2: 10.000 DM
        BinaryPrimitives.WriteInt32LittleEndian(block.AsSpan(8 * 2 + 4), 75);
        var game = GameState.ImportOriginal(save, database, Seed);
        var holding = game.Finance(game.Managers[0]).Holdings[2];
        Assert.Equal((75, 10_000L), (holding.Count, holding.PurchaseCost));
        Assert.Equal(0, game.Finance(game.Managers[0]).Holdings[0].Count);
    }

    /// <summary>Trainingsplan A8A1 (Tag · 4 + Einheit): FILE08 mit geändertem Plan, FILE09 zwei Manager alles frei.</summary>
    [GameDataTheory]
    [InlineData("FILE08.HOK")]
    [InlineData("FILE09.HOK")]
    public void Import_TrainingPlan(string file)
    {
        var (save, game) = Import(file);
        for (var m = 0; m < game.Managers.Count; m++)
        {
            var plan = game.Managers[m].Training;
            var units = save.Block("A8A1").AsSpan(28 * m, 28).ToArray();
            for (var day = 0; day < TrainingPlan.Days; day++)
                for (var unit = 0; unit < TrainingPlan.UnitsPerDay; unit++)
                    Assert.Equal((TrainingKind)units[4 * day + unit], plan[day, unit]);
            Assert.Equal(100, plan.PriceIndex);
        }
        if (file == "FILE08.HOK")
            Assert.Equal(TrainingKind.Free, game.Managers[0].Training[2, 0]); // FILE08: Einheit 8 von 5 auf 1 geändert
    }

    /// <summary>Taktik FILE40: Werte des Blocks 0 aus CAE9, Minuten CC43, „automatisch aufstellen“ 0x375 = 0 (manuell).</summary>
    [GameDataFact]
    public void Import_TacticsAndAutoLineup()
    {
        var (_, game) = Import("FILE40.HOK");
        var tactics = game.Clubs[game.Managers[0].ClubIndex].Tactics;
        Assert.Equal([36, 21, 22, 18, 23, 4], Enumerable.Range(0, 6).Select(i => tactics.Effort(0, (ScoreSituation)i)));
        Assert.Equal([1, 4, 6, 6, 2, 1], Enumerable.Range(0, 6).Select(i => tactics.Attack(0, (ScoreSituation)i)));
        Assert.All(Enumerable.Range(0, 6), i => Assert.Equal(0, tactics.Effort(3, (ScoreSituation)i)));
        Assert.Equal([2, 1, 3, 1], Enumerable.Range(0, 4).Select(l => tactics.Minutes(l, false)));
        Assert.Equal([1, 4, 2, 2], Enumerable.Range(0, 4).Select(l => tactics.Minutes(l, true)));
        Assert.Equal((TacticMode.Auto, TacticMode.Auto, LineChangeMode.Automatic), (tactics.EffortMode, tactics.AttackMode, tactics.LineChange));
        Assert.False(game.Clubs[game.Managers[0].ClubIndex].AutoLineupEnabled);

        var (_, file05) = Import("FILE05.HOK");
        Assert.True(file05.Clubs[file05.Managers[0].ClubIndex].AutoLineupEnabled);
    }

    /// <summary>Taktikmodi SEMI/MANUELL und Reihenwechsel „Minuten-Vorgabe“ aus A6FB/A707/A701 (an keinem Stand ≠ 0).</summary>
    [GameDataFact]
    public void Import_TacticModes()
    {
        var (save, database) = Load("FILE40.HOK");
        save.Block("A6FB")[0] = 1;
        save.Block("A707")[0] = 2;
        save.Block("A701")[0] = 4;
        var game = GameState.ImportOriginal(save, database, Seed);
        var tactics = game.Clubs[game.Managers[0].ClubIndex].Tactics;
        Assert.Equal((TacticMode.Semi, TacticMode.Manual, LineChangeMode.MinutesPreset), (tactics.EffortMode, tactics.AttackMode, tactics.LineChange));
    }

    /// <summary>
    /// Serien (AB7B+0xA00), Rekordserien des Managers (+0xF40), Rekorde (+0x1194/+0xF94), Kopf-an-Kopf (+0), Strafminuten 0x82A1 und
    /// Platzfelder 0x828B je Satzplatz → Verein nach Regel 1 (FILE36, 19 Spieltage).
    /// </summary>
    [GameDataFact]
    public void Import_SeriesRecordsAndTableFields()
    {
        var (save, game) = Import("FILE36.HOK");
        var database = ClubDatabase.Load(TestGameData.Load());
        var clubOf = OriginalImport.ClubIndices(save, database);
        var block = save.Block("AB7B");
        var table = save.Block("8257");
        for (var slot = 0; slot < 36; slot++)
        {
            var club = game.Clubs[clubOf[slot]];
            Assert.Equal(block.AsSpan(0xA00 + 21 * slot, 21).ToArray().Select(b => (int)b), club.SeriesCurrent);
            Assert.Equal(BinaryPrimitives.ReadInt16LittleEndian(table.AsSpan(OriginalImport.TableField(0x82A1, slot))), club.LeaguePenaltyMinutes);
            for (var kind = 0; kind < TeamRecords.Kinds; kind++)
            {
                var word = BinaryPrimitives.ReadUInt16LittleEndian(block.AsSpan(0x1194 + 0x10 * slot + 2 * kind));
                var (w, opponent) = game.TeamRecord(clubOf[slot], (TeamRecordKind)kind);
                Assert.Equal(word, w);
                if (word != 0)
                    Assert.Equal(clubOf[block[0xF94 + 8 * slot + kind]], opponent);
            }
            Assert.Equal(block.AsSpan(10 * slot, 10).ToArray().Select(b => (int)b), game.HeadToHead(0, clubOf[slot]));
        }
        Assert.Equal(block.AsSpan(0xF40, 21).ToArray().Select(b => (int)b), game.Managers[0].SeriesRecords);
        Assert.Contains(game.Managers[0].SeriesRecords, v => v > 0);
        Assert.Equal([12, 1, 1, 24, 71, 81], game.Managers[0].PowerPlayCounts);

        // Platzfelder: Halbbyte i = Platz nach Spieltag i + 1 (0 = Platz 1), gerades i im unteren Halbbyte.
        var first = game.Clubs[clubOf[0]];
        Assert.Equal([7, 8, 9, 10, 9, 10], Enumerable.Range(0, 6).Select(first.StoredRankSlot));
    }

    /// <summary>Platzfelder nach dem 3. Spieltag (FILE03) gegen die Tabelle 0x5CCB: Feld 2 = Platz des Satzes in der Tabelle.</summary>
    [GameDataFact]
    public void Import_RankSlotsMatchTable()
    {
        var (save, game) = Import("FILE03.HOK");
        var database = ClubDatabase.Load(TestGameData.Load());
        var clubOf = OriginalImport.ClubIndices(save, database);
        var order = save.Block("5CCB").AsSpan(0, 12).ToArray();
        for (var place = 0; place < 12; place++)
            Assert.Equal(place, game.Clubs[clubOf[order[place]]].StoredRankSlot(2));
    }

    /// <summary>Werbeangebote C313 an FILE05; ein Vertrag C15B über zwei Banden und einer fürs Trikot (gepatcht, an keinem Stand).</summary>
    [GameDataFact]
    public void Import_Sponsoring()
    {
        var (save, database) = Load("FILE05.HOK");
        var contracts = save.Block("C15B");
        foreach (var area in new[] { 2, 3 })
            WriteContract(contracts.AsSpan(10 * area), 60_000, partner: 4, months: 11, option: 0x80 | 12, rating: 70);
        WriteContract(contracts.AsSpan(10 * 7), 150_000, partner: 1, months: 5, option: 0, rating: 66);
        var game = GameState.ImportOriginal(save, database, Seed);
        var sponsoring = game.Finance(game.Managers[0]).Sponsoring;

        var bande = sponsoring.OfferFrom(AdSlotKind.Bande, 0);
        Assert.Equal(new SponsorOffer(AdSlotKind.Bande, 0, 12, 1, 51652 / 3, 0, 68), bande);
        Assert.Equal([0, 1, 2], sponsoring.Offers[AdSlotKind.Bande].Keys.Order());
        Assert.Equal([0, 4, 6], sponsoring.Offers[AdSlotKind.Trikot].Keys.Order());
        Assert.Equal([0, 1], sponsoring.Offers[AdSlotKind.Fernsehen].Keys.Order());
        Assert.Equal([0, 2], sponsoring.Offers[AdSlotKind.Eisflaeche].Keys.Order());
        Assert.Equal([1, 2, 6, 8], sponsoring.Offers[AdSlotKind.Eisflaeche2].Keys.Order());
        Assert.Equal(3, sponsoring.OfferFrom(AdSlotKind.Eisflaeche, 2)!.Boards);

        Assert.Equal(2, sponsoring.Contracts.Count);
        var board = sponsoring.ContractAt(new AdSlot(AdSlotKind.Bande, 3))!;
        Assert.Equal((4, 20_000L, 11, 12, 70), (board.Sponsor, board.MonthlyAmount, board.MonthsLeft, board.OptionMonths, board.SignedRating));
        Assert.Equal([2, 3], board.Boards);
        var shirt = sponsoring.ContractAt(new AdSlot(AdSlotKind.Trikot))!;
        Assert.Equal((1, 50_000L, 5, 0), (shirt.Sponsor, shirt.MonthlyAmount, shirt.MonthsLeft, shirt.OptionMonths));
        Assert.Equal(70_000L, sponsoring.MonthlyIncome);
    }

    /// <summary>Bankkredit FILE08 (−2.103.000 Drittel-DM), Sparbuch und Darlehen zwischen Managern (FILE09, zwei Manager, gepatcht).</summary>
    [GameDataFact]
    public void Import_BankAndManagerLoans()
    {
        var (_, file08) = Import("FILE08.HOK");
        Assert.Equal((701_000L, 0L), (file08.Finance(file08.Managers[0]).Bank.Credit, file08.Finance(file08.Managers[0]).Bank.Savings));
        Assert.Empty(file08.ManagerLoans);

        var (save, database) = Load("FILE09.HOK");
        var records = save.Block("965B");
        var second = OriginalSaveGame.ManagerRecordLength;
        BinaryPrimitives.WriteInt32LittleEndian(records.AsSpan(second + 0x19B), -300_000); // Manager 1 schuldet Manager 0
        BinaryPrimitives.WriteInt32LittleEndian(records.AsSpan(0x19B + 4), 300_000);
        BinaryPrimitives.WriteInt32LittleEndian(records.AsSpan(0x19B + 16), 90_000); // Manager 0: Sparbuch
        var game = GameState.ImportOriginal(save, database, Seed);
        var loan = Assert.Single(game.ManagerLoans);
        Assert.Equal((0, 1, 100_000L), (loan.Lender, loan.Borrower, loan.Amount));
        Assert.Equal((0L, 30_000L), (game.Finance(game.Managers[0]).Bank.Credit, game.Finance(game.Managers[0]).Bank.Savings));
    }

    /// <summary>Nationalkonto Satz 5 +0x18F (FILE18: 3.000.000 Drittel-DM) mit einer Übertragsbuchung; Talentsuche 965B+0xEC.</summary>
    [GameDataFact]
    public void Import_NationalAccountAndTalentSearch()
    {
        var (_, file18) = Import("FILE18.HOK");
        Assert.Equal(1_000_000L, file18.NationalAccount);
        var booking = Assert.Single(file18.NationalBookings);
        Assert.Equal(NationalBookingKind.Carryover, booking.Kind);

        var (save, database) = Load("FILE05.HOK");
        save.Block("965B")[0xEC] = 9;
        var game = GameState.ImportOriginal(save, database, Seed);
        Assert.Equal(9, game.Finance(game.Managers[0]).TalentSearch?.TicksLeft);
        Assert.Equal(0L, game.NationalAccount);
    }

    /// <summary>Kennzeichen „neue Nachricht“ DGROUP 0x5A50 (FILE20: Manager 0 = 1, FILE05: 0).</summary>
    [GameDataTheory]
    [InlineData("FILE20.HOK", true)]
    [InlineData("FILE05.HOK", false)]
    public void Import_NewMessageFlag(string file, bool expected)
    {
        var (_, game) = Import(file);
        Assert.Equal(expected, game.Managers[0].HasNewMessage);
    }

    private static void WriteContract(Span<byte> c, int amountThirds, int partner, int months, int option, int rating)
    {
        BinaryPrimitives.WriteInt32LittleEndian(c, amountThirds);
        c[4] = (byte)partner;
        c[5] = (byte)months;
        c[6] = (byte)option;
        c[8] = (byte)rating;
    }

    private static (OriginalSaveGame Save, GameState Game) Import(string file)
    {
        var (save, database) = Load(file);
        return (save, GameState.ImportOriginal(save, database, Seed));
    }

    private static (OriginalSaveGame Save, ClubDatabase Database) Load(string file) =>
        (OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave(file))), ClubDatabase.Load(TestGameData.Load()));
}
