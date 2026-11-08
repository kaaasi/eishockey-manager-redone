using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using Xunit.Abstractions;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur hok-import, Teil 2: Import eines Original-Spielstands in den Spielkern (<see cref="GameState.ImportOriginal"/>) nach der
/// Feldkarte in <c>docs/formate/spielstand-original.md</c>. Abnahme laut Arbeitspaket: FILE05, FILE11, FILE16A, FILE29 laden,
/// Rundreise Speichern/Laden, danach <see cref="GameState.Continue"/> über einen Spieltag und eine Mehrsaisonprobe.
/// </summary>
public sealed class OriginalImportTests(ITestOutputHelper output)
{
    private const int Seed = 4711;

    public static TheoryData<string> Saves => new() { "FILE05.HOK", "FILE11.HOK", "FILE16A.HOK", "FILE29.HOK" };

    /// <summary>Datum, Saison, Spieltag, Manager und Verein folgen dem HOK (Regeln 1, 5).</summary>
    [GameDataTheory]
    [MemberData(nameof(Saves))]
    public void Import_SetsDateManagersAndClubs(string file)
    {
        var (save, database) = Load(file);
        var game = GameState.ImportOriginal(save, database, Seed);
        var clubOf = OriginalImport.ClubIndices(save, database);

        Assert.Equal(OriginalImport.Day(save), game.CurrentDay);
        Assert.Equal(OriginalImport.Season(save), game.Season);
        Assert.Equal(OriginalImport.Mode(save), game.Mode);
        Assert.Equal(save.HeaderManagerCount, game.Managers.Count);
        var records = save.Block("965B");
        for (var m = 0; m < game.Managers.Count; m++)
        {
            var manager = game.Managers[m];
            Assert.Equal(save.Managers[m].Name, manager.Name);
            Assert.Equal(clubOf[records[m * OriginalSaveGame.ManagerRecordLength + 0x14]], manager.ClubIndex);
            Assert.Equal(BitConverter.ToInt32(records, m * OriginalSaveGame.ManagerRecordLength + 0x18F) / 3, manager.Money);
            Assert.Equal(records[m * OriginalSaveGame.ManagerRecordLength + 0xE4], game.Finance(manager).Stadium.TicketPrice);
            var youth = save.Block("A075").AsSpan(0x15 * m);
            Assert.Equal([youth[0], youth[1], youth[2]], game.Finance(manager).Youth.Select(t => (byte)t.Strength));
            Assert.Equal([youth[3] + 1, youth[4] + 1, youth[5] + 1], game.Finance(manager).Youth.Select(t => t.Rank));
            Assert.Equal(BitConverter.ToInt32(save.Block("020D"), 4 * m) / 3, game.Finance(manager).TalentPrice);
        }
        if (game.Mode != GameMode.WorldOnly)
        {
            Assert.Equal(OriginalImport.NextMatchDay(save, League.Bundesliga) - 1, game.PlayedMatchDays);
            foreach (var league in Enum.GetValues<League>())
                Assert.Equal(Enumerable.Range((int)league * 12, 12).Select(s => clubOf[s]), game.LeagueSlots(league));
        }
        Assert.Empty(game.ValidatePlayerIdentity());
        output.WriteLine($"{file}: {game.DateText}, Spieltage {game.PlayedMatchDays}, Phase {game.Phase}, Konto {game.Managers[0].Money}");
    }

    /// <summary>
    /// Ergebnisblock 0xA0CB mit dem Spielplan 0x4E7C7 über die Satzfolge: die daraus berechnete Tabelle gleicht den
    /// Tabellensätzen des Originals (DGROUP 0x825A + 0x50 · Satz: Punkte +0/+1, Tore +0x16/+0x17, Gegentore +0x1A/+0x1B, Spiele +0x1E/+0x1F,
    /// Siege +0x26/+0x27, Niederlagen +0x2A/+0x2B; reverse-engineering.md „Ligatabelle“).
    /// </summary>
    [GameDataTheory]
    [InlineData("FILE05.HOK")]
    [InlineData("FILE16A.HOK")]
    [InlineData("FILE24.HOK")]
    [InlineData("FILE31.HOK")]
    [InlineData("FILE36.HOK")]
    [InlineData("FILE37A.HOK")]
    [InlineData("FILE40.HOK")]
    public void Import_LeagueTableMatchesOriginalTableRecords(string file)
    {
        var (save, database) = Load(file);
        var game = GameState.ImportOriginal(save, database, Seed);
        var clubOf = OriginalImport.ClubIndices(save, database);
        var table = save.Block("8257");
        Assert.True(game.PlayedMatchDays > 0);
        foreach (var league in Enum.GetValues<League>())
        {
            var rows = LeagueTable.Compute(game, league, TableView.Gesamt, game.PlayedMatchDays).ToDictionary(r => r.Club.Index);
            for (var slot = 12 * (int)league; slot < 12 * ((int)league + 1); slot++)
            {
                var o = 3 + 0x50 * slot;
                var row = rows[clubOf[slot]];
                Assert.Equal(table[o] + table[o + 1], row.PointsPlus);
                Assert.Equal(table[o + 0x16] + table[o + 0x17], row.GoalsFor);
                Assert.Equal(table[o + 0x1A] + table[o + 0x1B], row.GoalsAgainst);
                Assert.Equal(table[o + 0x26] + table[o + 0x27], row.Wins);
                Assert.Equal(table[o + 0x2A] + table[o + 0x2B], row.Losses);
                Assert.Equal(table[o + 0x1E] + table[o + 0x1F], row.Games);
            }
        }
    }

    /// <summary>Alle Stände FILE01–FILE41 (auch Folgesaison, „nur Weltkugel“, zwei Manager) ergeben einen gültigen Spielstand.</summary>
    [GameDataFact]
    public void Import_AllOriginalStands_AreValid()
    {
        var database = ClubDatabase.Load(TestGameData.Load());
        var folder = Path.GetDirectoryName(OriginalSave("FILE01.HOK"))!;
        var files = Directory.GetFiles(folder, "FILE*.HOK").Order(StringComparer.Ordinal).ToList();
        Assert.True(files.Count >= 40, $"nur {files.Count} Stände in {folder}");
        foreach (var path in files)
        {
            var save = OriginalSaveGame.Read(File.ReadAllBytes(path));
            var game = GameState.ImportOriginal(save, database, Seed);
            Assert.Equal(OriginalImport.Day(save), game.CurrentDay);
            Assert.Empty(game.ValidatePlayerIdentity());
            output.WriteLine($"{Path.GetFileName(path)}: {game.DateText}, {game.Mode}, Spieltage {game.PlayedMatchDays}, Phase {game.Phase}");
        }
    }

    /// <summary>Kader des Managervereins: Namen, Werte, Vertrag und Aufstellung aus den Kaderplätzen 5DC3 (FILE05: Köln).</summary>
    [GameDataFact]
    public void Import_ManagerSquadFromSquadSlots()
    {
        var (save, database) = Load("FILE05.HOK");
        var game = GameState.ImportOriginal(save, database, Seed);
        var club = game.Clubs[game.Managers[0].ClubIndex];
        var slots = save.Squad(0);
        Assert.Equal(slots.Select(s => s.Name), club.Players.Select(p => p.Name));
        for (var i = 0; i < slots.Count; i++)
        {
            var (s, p) = (slots[i], club.Players[i]);
            Assert.Equal((s.Strength1, s.Strength2, s.Strength3), (p.Condition, p.Technique, p.Form));
            Assert.Equal((s.Age, s.Talent, s.ContractYears, s.SalaryThirds / 3), (p.Age, p.Talent, p.ContractYears, p.MonthlySalary));
            Assert.Equal(s.IsGoalkeeper, p.Role == PlayerRole.Goalkeeper);
            Assert.Equal(i + 1, p.Number);
            if (s.Line != 0)
                Assert.True(club.Lineup.Contains(p), $"{p.Name} (Reihe {s.Line}) fehlt in der Aufstellung");
        }
        Assert.Equal("CISSEWSKI", club.Lineup.Goalkeeper?.Name);
        Assert.Equal(slots.Count(s => s.Line != 0), club.Lineup.Count);
    }

    /// <summary>Rundreise: importierter Stand → Remake-Spielstand → Laden, danach ein Spieltag über die Zeitleiste.</summary>
    [GameDataTheory]
    [MemberData(nameof(Saves))]
    public void Import_RoundTripAndContinue(string file)
    {
        var (save, database) = Load(file);
        var game = GameState.ImportOriginal(save, database, Seed);
        using var stream = new MemoryStream();
        SaveGame.Save(game, stream);
        var loaded = SaveGame.Load(stream.ToArray());
        Assert.True(loaded.Success, loaded.Error);
        var copy = loaded.Game!;
        Assert.Equal(game.CurrentDay, copy.CurrentDay);
        Assert.Equal(game.Season, copy.Season);
        Assert.Equal(game.PlayedMatchDays, copy.PlayedMatchDays);
        Assert.Equal(game.Managers.Select(m => m.Money), copy.Managers.Select(m => m.Money));
        Assert.Equal(game.Results, copy.Results);
        Assert.Equal(game.Clubs[game.Managers[0].ClubIndex].Players.Select(p => (p.Id, p.Name)),
            copy.Clubs[copy.Managers[0].ClubIndex].Players.Select(p => (p.Id, p.Name)));

        var day = copy.CurrentDay;
        var played = copy.PlayedMatchDays;
        if (copy.Mode == GameMode.WorldOnly || copy.RegularSeasonOver)
            copy.Continue();
        else
            TestSeason.PlayMatchDay(copy);
        Assert.True(copy.CurrentDay > day || copy.PlayedMatchDays > played || copy.Phase != SeasonPhase.Running);
        Assert.Empty(copy.ValidatePlayerIdentity());
    }

    /// <summary>
    /// Mehrsaisonprobe (AGENTS.md §5): vier Saisonwechsel nach dem Import, passiver Manager (greift nicht ein) und aktiver
    /// Manager (hält den Kader, genug Geld). Der Spielstand bleibt gültig und läuft ohne Ausnahme weiter.
    /// </summary>
    [GameDataTheory]
    [InlineData("FILE05.HOK", false)]
    [InlineData("FILE05.HOK", true)]
    [InlineData("FILE16A.HOK", false)]
    [InlineData("FILE11.HOK", true)]
    public void Import_FourSeasonChanges(string file, bool active)
    {
        var (save, database) = Load(file);
        var game = GameState.ImportOriginal(save, database, Seed);
        var start = game.Season;
        for (var change = 0; change < 4 && game.Phase != SeasonPhase.GameOver; change++)
        {
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 3000, "Saisonende nicht erreicht");
                if (active)
                    foreach (var m in game.Managers)
                    {
                        m.Money = Math.Max(m.Money, 2_000_000);
                        TestSeason.KeepSquad(game, m, extendExpired: true);
                    }
                game.Continue();
            }
            if (game.Phase == SeasonPhase.GameOver)
                break;
            game.StartNextSeason();
            Assert.Empty(game.ValidatePlayerIdentity());
        }
        output.WriteLine($"{file} aktiv={active}: Saison {start} → {game.Season}, Phase {game.Phase}");
        if (active)
            Assert.Equal(start + 4, game.Season);
        Assert.True(game.Season > start || game.Phase == SeasonPhase.GameOver);
    }

    private static (OriginalSaveGame Save, ClubDatabase Database) Load(string file) =>
        (OriginalSaveGame.Read(File.ReadAllBytes(OriginalSave(file))), ClubDatabase.Load(TestGameData.Load()));

    private static string OriginalSave(string name)
    {
        var folder = Environment.GetEnvironmentVariable("EM_SAVEGAMES");
        if (string.IsNullOrEmpty(folder))
        {
            var root = TestGameData.Load().RootDirectory;
            var real = Directory.ResolveLinkTarget(root.TrimEnd('/', '\\'), true)?.FullName ?? root;
            folder = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(real).TrimEnd('/', '\\'))!, "spielstaende");
        }
        return Path.Combine(folder, name);
    }
}
