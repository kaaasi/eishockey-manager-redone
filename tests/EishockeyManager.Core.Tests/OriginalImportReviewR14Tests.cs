using System.Buffers.Binary;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur hok-import, Befunde des Reviews zu PR #670 (<c>docs/reviews/2026-11-09-integration-hok-import-r14.md</c>): R14HI-02
/// (laufende Stadionausbauten), R14HI-03 (Werbevertrag mit Betrag, der nicht durch 3 teilbar ist), R14HI-04 (Zeilen der Feldkarte
/// mit Status B/A, die der Import nicht übernahm).
/// </summary>
public sealed class OriginalImportReviewR14Tests
{
    private const int Seed = 4711;

    /// <summary>R14HI-02: FILE43 – Sitzkomfort Stufe 0 mit Ziel 1 und 8 Restwochen (Manager-Satz +0x117 + 2 · 3, +0x128 + 3); die 370.000 DM sind schon abgebucht.</summary>
    [GameDataFact]
    public void Import_StadiumConstruction_File43()
    {
        var (_, game, _) = Import("FILE43.HOK");
        var stadium = game.Finance(game.Managers[0]).Stadium;
        var construction = Assert.Single(stadium.Constructions);
        Assert.Equal((StadiumProject.Feature, StadiumFeature.Sitzkomfort, 1, 8), (construction.Project, construction.Feature, construction.Target, construction.WeeksLeft));
        Assert.Equal(0, stadium.Level(StadiumFeature.Sitzkomfort));
        Assert.True(stadium.IsUnderConstruction(StadiumProject.Feature, StadiumFeature.Sitzkomfort));
        Assert.Equal(668_860, game.Managers[0].Money);
    }

    /// <summary>R14HI-02: FILE44A – Gastronomie Stufe 2 mit Ziel 3 (Restaurant), noch 1 Woche; das Kino ist fertig (Stufe 2, kein Ziel).</summary>
    [GameDataFact]
    public void Import_StadiumConstruction_File44A()
    {
        var (_, game, _) = Import("FILE44A.HOK");
        var stadium = game.Finance(game.Managers[0]).Stadium;
        var construction = Assert.Single(stadium.Constructions);
        Assert.Equal((StadiumProject.Feature, StadiumFeature.Gastronomie, 3, 1), (construction.Project, construction.Feature, construction.Target, construction.WeeksLeft));
        Assert.Equal(2, stadium.Level(StadiumFeature.Gastronomie));
        Assert.Equal(2, stadium.Level(StadiumFeature.Kino));
        Assert.False(stadium.IsUnderConstruction(StadiumProject.Feature, StadiumFeature.Kino));
    }

    /// <summary>Ohne Baustelle (FILE41, FILE44) bleibt die Liste leer – auch nach dem Nachspielen.</summary>
    [GameDataTheory]
    [InlineData("FILE41.HOK")]
    [InlineData("FILE44.HOK")]
    public void Import_NoConstruction_WhenOriginalHasNone(string file)
    {
        var (_, game, _) = Import(file);
        Assert.Empty(game.Finance(game.Managers[0]).Stadium.Constructions);
    }

    /// <summary>
    /// R14HI-03: Werbevertrag mit 1.000.001 Drittel-DM (nicht durch 3 teilbar), gültige Prüfsummen (im Test über
    /// <see cref="OriginalSavePatcher.Encode"/> erzeugt). Vorher warf <c>ApplySponsoring</c> eine <c>KeyNotFoundException</c>, die
    /// <c>TryImport</c> nicht fing; jetzt lädt der Stand mit dem Vertrag (Betrag / 3 abgerundet).
    /// </summary>
    [GameDataFact]
    public void TryImport_SponsorContractAmountNotDivisibleByThree_Loads()
    {
        var original = File.ReadAllBytes(OriginalImportTests.OriginalSave("FILE05.HOK"));
        var save = OriginalSaveGame.Read(original);
        var contracts = save.Block("C15B");
        BinaryPrimitives.WriteInt32LittleEndian(contracts.AsSpan(0), 1_000_001); // Fläche 0 (Bande), Manager 0
        contracts[4] = 0; // Partner
        contracts[5] = 6; // Restmonate
        var patched = OriginalSavePatcher.Encode(original.AsSpan(0, OriginalSaveGame.PlainLength), save);
        var database = ClubDatabase.Load(TestGameData.Load());
        Assert.True(OriginalSaveGame.Read(patched).ChecksumsValid);

        var result = OriginalImport.TryImport(patched, database, Seed);
        Assert.True(result.Success, result.Error);
        var contract = Assert.Single(game(result).Finance(game(result).Managers[0]).Sponsoring.Contracts);
        Assert.Equal((AdSlotKind.Bande, 0, 333_333, 6), (contract.Kind, contract.Sponsor, contract.MonthlyAmount, contract.MonthsLeft));
        Assert.Equal(new[] { 0 }, contract.Boards);
        static GameState game(OriginalImportResult r) => r.Game!;
    }

    /// <summary>R14HI-03: Ein Importfehler mit gültigen Prüfsummen ist ein Ladefehler ohne Prüfsummen-Kennzeichen (Zahl der Manager 0, DGROUP 0x386).</summary>
    [GameDataFact]
    public void TryImport_ImportError_IsReportedWithoutChecksumFlag()
    {
        var original = File.ReadAllBytes(OriginalImportTests.OriginalSave("FILE05.HOK"));
        var save = OriginalSaveGame.Read(original);
        save.Block("0386")[0] = 0;
        var result = OriginalImport.TryImport(OriginalSavePatcher.Encode(original.AsSpan(0, OriginalSaveGame.PlainLength), save),
            ClubDatabase.Load(TestGameData.Load()), Seed);
        Assert.False(result.Success);
        Assert.False(result.ChecksumError);
        Assert.Contains("0x386", result.Error);
    }

    // ---- R14HI-04: Zeilen der Feldkarte mit Status B/A ------------------------------------------------------------

    /// <summary>
    /// Freundschaftsspiele des Bundestrainers (R14HI-04): Zahl DGROUP 0xD0C1, je Spiel Heim 0xD0DB, Gast 0xD0D3, Heimtore 0xD0CB,
    /// Gasttore 0xD0C3 ([ORIGINAL] Schreiber 0x3C248–0x3C288, Ergebnis von 0x30365 = Heimtore &lt;&lt; 8 | Gasttore). FILE19/FILE20:
    /// Deutschland – Schweden 2:1 (Heimspiel, Kosten 110.000 DM); FILE18 noch keins.
    /// </summary>
    [GameDataTheory]
    [InlineData("FILE18.HOK", 0)]
    [InlineData("FILE19.HOK", 1)]
    [InlineData("FILE20.HOK", 1)]
    public void Import_FriendliesFromOriginal(string file, int count)
    {
        var (save, game, clubOf) = Import(file);
        var coach = game.NationalCoach!;
        Assert.Equal(count, coach.Friendlies.Count);
        Assert.Equal(count, save.Block("D0C1")[0]);
        for (var i = 0; i < count; i++)
        {
            var f = coach.Friendlies[i];
            Assert.Equal((clubOf[save.Block("D0DB")[i]], clubOf[save.Block("D0D3")[i]], (int)save.Block("D0CB")[i], (int)save.Block("D0C3")[i]),
                (f.Home, f.Away, f.HomeGoals, f.AwayGoals));
            Assert.Equal(coach.Nation, f.Home);
            Assert.Equal("SCHWEDEN", game.Clubs[f.Away].Name);
            Assert.Equal((2, 1), (f.HomeGoals, f.AwayGoals));
        }
    }

    /// <summary>
    /// Zuschauerstatistik (R14HI-04): Heimspiele +0xED, Kurve +0xFE (Zuschauer / 1000, 22 Plätze), Summe +0x183, Rekord +0x187 mit
    /// Gegner +0x114, Minuskulisse +0x18B mit Gegner +0x115 (Satzplätze, Regel 1). FILE44: 22 Heimspiele, 145.109 Zuschauer; FILE46
    /// (neue Saison): Zähler 0, Gegner bleiben stehen.
    /// </summary>
    [GameDataTheory]
    [InlineData("FILE05.HOK")]
    [InlineData("FILE44.HOK")]
    [InlineData("FILE46.HOK")]
    public void Import_StadiumStatisticsFromOriginal(string file)
    {
        var (save, game, clubOf) = Import(file);
        var record = save.Block("965B").AsSpan(0, OriginalSaveGame.ManagerRecordLength).ToArray();
        var stats = game.Finance(game.Managers[0]).HomeStatistics;
        int games = record[0xED];
        Assert.Equal(games, stats.Games);
        Assert.Equal(Enumerable.Range(0, games).Select(i => (int)record[0xFE + i]), stats.Curve);
        Assert.Equal(BinaryPrimitives.ReadInt32LittleEndian(record.AsSpan(0x183)), stats.Total);
        Assert.Equal(BinaryPrimitives.ReadInt32LittleEndian(record.AsSpan(0x187)), stats.Record);
        Assert.Equal(BinaryPrimitives.ReadInt32LittleEndian(record.AsSpan(0x18B)), stats.Minimum);
        Assert.Equal((clubOf[record[0x114]], clubOf[record[0x115]]), (stats.RecordOpponent, stats.MinimumOpponent));
        if (file == "FILE44.HOK")
            Assert.Equal((22, 145_109L, 7500, 5249, "EC HEDOS MÜNCHEN", "EHC FREIBURG"),
                (stats.Games, stats.Total, stats.Record, stats.Minimum, game.Clubs[stats.RecordOpponent].Name, game.Clubs[stats.MinimumOpponent].Name));
        if (file == "FILE46.HOK")
            Assert.Equal((0, StadiumStatistics.InitialMinimum), (stats.Games, stats.Minimum));
    }

    /// <summary>
    /// Tendenz (R14HI-04): Wert des Kaderplatzes +0x0E (30 … 68, <see cref="Player.TrendScore"/>, [ORIGINAL] Datei 0x4907A/0x41AE0)
    /// und Stufe daraus; Steuerbyte +0x14. FILE05: HEISS 35 (fallend), CISSEWSKI 60 (steigend), KÜHN 50 (konstant).
    /// </summary>
    [GameDataFact]
    public void Import_PlayerTrendFromSlot()
    {
        var (save, game, _) = Import("FILE05.HOK");
        var club = game.Clubs[game.Managers[0].ClubIndex];
        for (var i = 0; i < club.Players.Count; i++)
        {
            var slot = OriginalImport.SquadSlot(save, 0, i);
            Assert.Equal(slot[0x0E], club.Players[i].TrendScore);
            Assert.Equal(Player.TrendOf(slot[0x0E]), club.Players[i].Trend);
            Assert.Equal(slot[0x14], club.Players[i].TrendControl);
        }
        Assert.Equal((35, PlayerTrend.Falling), (club.Players[0].TrendScore, club.Players[0].Trend));
        Assert.Equal((60, PlayerTrend.Rising), (club.Players[1].TrendScore, club.Players[1].Trend));
        Assert.Equal((50, PlayerTrend.Constant), (club.Players[5].TrendScore, club.Players[5].Trend));
    }

    /// <summary>
    /// Ankündigung (R14HI-04): Kaderplatz +0x18 &gt; 99 = Angebot des Spielers („von 1 auf n Jahre“, n = +0x18 − 100; docs/transfer.md
    /// 0x4A3AF, Saisonende 0x27D05: 101), Bit 0x80 = hört auf (0x4A692), 1 … 99 = Verhandlungssperre. An keinem Stand belegt → im Test
    /// über <see cref="OriginalSavePatcher.Encode"/> gesetzt (FILE05, Plätze 0–2).
    /// </summary>
    [GameDataFact]
    public void Import_ContractNoticeFromSlot()
    {
        var original = File.ReadAllBytes(OriginalImportTests.OriginalSave("FILE05.HOK"));
        var save = OriginalSaveGame.Read(original);
        var squad = save.Block("5DC3");
        squad[0 * 0x34 + 0x18] = 102; // Angebot: von 1 auf 2 Jahre
        squad[1 * 0x34 + 0x18] = 0x80; // hört am Saisonende auf
        squad[2 * 0x34 + 0x18] = 12; // Sperre 12 Tage
        var patched = OriginalSavePatcher.Encode(original.AsSpan(0, OriginalSaveGame.PlainLength), save);
        var database = ClubDatabase.Load(TestGameData.Load());
        var game = GameState.ImportOriginal(OriginalSaveGame.Read(patched), database, Seed);
        var players = game.Clubs[game.Managers[0].ClubIndex].Players;
        Assert.Equal((ContractNotice.Offered, 2, 0), (players[0].Notice, players[0].NoticeYears, players[0].NegotiationLock));
        Assert.Equal((ContractNotice.Retiring, 0), (players[1].Notice, players[1].NegotiationLock));
        Assert.Equal((ContractNotice.None, 12), (players[2].Notice, players[2].NegotiationLock));
        Assert.All(players.Skip(3), p => Assert.Equal((ContractNotice.None, 0), (p.Notice, p.NegotiationLock)));
    }

    /// <summary>
    /// Bundestrainer (R14HI-04): im Amt, wenn der Vereinssatz des Landes (DGROUP 0x6AC) +0x16 = 6 ist (Ernennung 0x55494, Amtsende
    /// 0x55752); Manager aus Satz 5 +0xEB (0x554B1), Land aus Satz 5 +0x14 (0x55485). DGROUP 0xD0BA ist nur das Kennzeichen „Ernennung
    /// steht an“ (0x54D19 → 1, 0x554BA → 0) und an allen Ständen 0. FILE11/FILE18: Manager 0 im Amt; FILE05: niemand.
    /// </summary>
    [GameDataTheory]
    [InlineData("FILE05.HOK", false)]
    [InlineData("FILE11.HOK", true)]
    [InlineData("FILE18.HOK", true)]
    [InlineData("FILE47.HOK", true)]
    public void Import_NationalCoachFromClubRecord(string file, bool inOffice)
    {
        var (save, game, clubOf) = Import(file);
        var nation = save.Block("06AC")[0];
        var record = save.Block("far:9657").AsSpan(nation * OriginalSaveGame.ClubRecordLength, OriginalSaveGame.ClubRecordLength);
        Assert.Equal(inOffice, record[0x16] == 6);
        if (!inOffice)
        {
            Assert.Null(game.NationalCoach);
            return;
        }
        var coach = game.NationalCoach!;
        var coachRecord = save.Block("965B").AsSpan(5 * OriginalSaveGame.ManagerRecordLength, OriginalSaveGame.ManagerRecordLength);
        Assert.Equal((coachRecord[0xEB], clubOf[coachRecord[0x14]]), (coach.Manager, coach.Nation));
        Assert.Equal(clubOf[nation], coach.Nation);
    }

    /// <summary>Bundestrainer aus dem HOK statt aus dem Nachspielen: FILE05 (Modus 4, kein Amt) mit gesetztem Vereinsfeld +0x16 = 6 und Satz 5 +0xEB = 0 → Manager 0 im Amt.</summary>
    [GameDataFact]
    public void Import_NationalCoach_WhenOnlyTheOriginalHasOne()
    {
        var original = File.ReadAllBytes(OriginalImportTests.OriginalSave("FILE05.HOK"));
        var save = OriginalSaveGame.Read(original);
        var nation = save.Block("06AC")[0];
        save.Block("far:9657")[nation * OriginalSaveGame.ClubRecordLength + 0x16] = 6;
        var coachRecord = save.Block("965B").AsSpan(5 * OriginalSaveGame.ManagerRecordLength, OriginalSaveGame.ManagerRecordLength);
        coachRecord[0xEB] = 0;
        coachRecord[0x14] = nation;
        var database = ClubDatabase.Load(TestGameData.Load());
        var game = GameState.ImportOriginal(OriginalSaveGame.Read(OriginalSavePatcher.Encode(original.AsSpan(0, OriginalSaveGame.PlainLength), save)), database, Seed);
        Assert.NotNull(game.NationalCoach);
        Assert.Equal(0, game.NationalCoach!.Manager);
        Assert.Equal(NationalTeams.Germany, game.NationalCoach.Nation);
    }

    /// <summary>
    /// Taktik des Nationalteams (R14HI-04): Anzeige unter „AUTOM.“ aus dem Vereinssatz des Landes (+0x26–0x2B Einsatz, +0x35–0x3A
    /// Angriff; FILE19 nach dem Freundschaftsspiel 60 20 20 15 20 0 / 0 5 6 4 4 0), eigene Werte aus Satz 5 – mit Bundestrainer in
    /// dessen Taktik, ohne in der Taktik des Landes (<c>_vacantNationalTactics</c>, FILE05: alles 0).
    /// </summary>
    [GameDataTheory]
    [InlineData("FILE19.HOK")]
    [InlineData("FILE20.HOK")]
    [InlineData("FILE05.HOK")]
    public void Import_NationalTacticsShownAutoLine(string file)
    {
        var (save, game, _) = Import(file);
        var nation = save.Block("06AC")[0];
        var record = save.Block("far:9657").AsSpan(nation * OriginalSaveGame.ClubRecordLength, OriginalSaveGame.ClubRecordLength);
        var tactics = game.NationalTactics;
        Assert.True(tactics.KeepsLastAutoLine);
        Assert.Equal((TacticMode)save.Block("A6FB")[5], tactics.EffortMode);
        for (var i = 0; i < Tactics.Situations; i++)
        {
            Assert.Equal(record[0x26 + i], tactics.ShownEffort(0, (ScoreSituation)i));
            Assert.Equal(record[0x35 + i], tactics.ShownAttack(0, (ScoreSituation)i));
        }
        if (file == "FILE19.HOK")
            Assert.Equal(new[] { 60, 20, 20, 15, 20, 0 }, Enumerable.Range(0, 6).Select(i => tactics.ShownEffort(0, (ScoreSituation)i)));
    }

    /// <summary>
    /// Titelträger (R14HI-04): DEB-Pokalsieger DGROUP 0x387 und Europacupsieger 0x388 nach dem Endspiel bzw. dem zwölften Termin aus dem
    /// HOK (FILE44: ESV Kaufbeuren und Malmö FF, FILE38: Malmö FF). Vor dem Endspiel halten beide den Vorjahressieger (FILE05: EV Landshut), den das
    /// Remake nur in <c>History</c> führt (leer, Feldkarte N) – dann bleibt <c>Winner</c> null.
    /// </summary>
    [GameDataTheory]
    [InlineData("FILE44.HOK", true, true)]
    [InlineData("FILE38.HOK", false, true)]
    [InlineData("FILE05.HOK", false, false)]
    public void Import_CupWinnersFromOriginal(string file, bool debFinished, bool europeFinished)
    {
        var (save, game, clubOf) = Import(file);
        Assert.Equal(debFinished ? clubOf[save.Block("0387")[0]] : null, game.DebCup!.Winner);
        Assert.Equal(europeFinished ? clubOf[save.Block("0388")[0]] : null, game.EuropeanCup!.Winner);
        if (debFinished)
            Assert.Equal("ESV KAUFBEUREN", game.Clubs[game.DebCup.Winner!.Value].Name);
        if (europeFinished)
            Assert.Equal("MALMÖ FF", game.Clubs[game.EuropeanCup.Winner!.Value].Name);
    }

    // ---- R14HI-05: Testlücken M03, M09 ----------------------------------------------------------------------------

    /// <summary>
    /// Regel 1 im Modus „nur Weltkugel“ (M03): Der Satz des Managervereins trägt den Namen des Landes (FILE11: zwei Sätze
    /// „DEUTSCHLAND“). Der Satz des Landes (`06AC`) bekommt den Datenbankindex Deutschlands, der Satz des Managervereins
    /// (`965B+0x14`) den einzigen Datenbanknamen, der im Satzbestand fehlt – nicht umgekehrt.
    /// </summary>
    [GameDataTheory]
    [InlineData("FILE11.HOK")]
    [InlineData("FILE18.HOK")]
    public void ClubIndices_WorldOnly_NationKeepsItsIndex_ManagerSlotGetsTheMissingClub(string file)
    {
        var save = OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave(file)));
        var database = ClubDatabase.Load(TestGameData.Load());
        var clubOf = OriginalImport.ClubIndices(save, database);
        int nationSlot = save.Block("06AC")[0], managerSlot = save.Block("965B")[0x14];
        var records = save.Block("far:9657");
        Assert.Equal(OriginalImport.ClubName(records, nationSlot), OriginalImport.ClubName(records, managerSlot)); // beide „DEUTSCHLAND“
        Assert.Equal(database.Clubs.Single(c => c.Name == "DEUTSCHLAND").Index, clubOf[nationSlot]);
        Assert.Equal(NationalTeams.Germany, clubOf[nationSlot]);
        var names = Enumerable.Range(0, ClubDatabase.TotalClubs).Select(slot => OriginalImport.ClubName(records, slot)).ToHashSet();
        var missing = database.Clubs.Single(c => !names.Contains(c.Name));
        Assert.Equal(missing.Index, clubOf[managerSlot]);
        Assert.NotEqual(clubOf[nationSlot], clubOf[managerSlot]);
    }

    /// <summary>
    /// „automatisch aufstellen“ (M09): aktueller Wert DGROUP 0x375 + 2 · m, nicht der gesicherte 0x376. FILE32/FILE33 haben 0/1
    /// (Handbetrieb, Sicherung noch 1), FILE05 1/1.
    /// </summary>
    [GameDataTheory]
    [InlineData("FILE32.HOK", false)]
    [InlineData("FILE33.HOK", false)]
    [InlineData("FILE05.HOK", true)]
    public void Import_AutoLineup_ReadsCurrentByteNotTheSavedOne(string file, bool enabled)
    {
        var (save, game, _) = Import(file);
        Assert.Equal(enabled ? 1 : 0, save.Block("0375")[0]);
        Assert.Equal(1, save.Block("0375")[1]);
        Assert.Equal(enabled, game.Clubs[game.Managers[0].ClubIndex].AutoLineupEnabled);
    }

    internal static (OriginalSaveGame Save, GameState Game, int[] ClubOf) Import(string file)
    {
        var save = OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave(file)));
        var database = ClubDatabase.Load(TestGameData.Load());
        return (save, GameState.ImportOriginal(save, database, Seed), OriginalImport.ClubIndices(save, database));
    }
}
