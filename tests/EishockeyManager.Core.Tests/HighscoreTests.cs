using EishockeyManager.Core;

namespace EishockeyManager.Tests;

/// <summary>
/// Spur tabellen-highscore (Teil 2): Datei, Einfügen und Schlüssel der Bestenliste – [ORIGINAL] em.exe Datei 0x5D617 / 0x5D73D / 0x28A2C.
/// </summary>
public class HighscoreTests
{
    private static HighscoreEntry E(string name, string club, int score, uint stamp = 0) =>
        new(name, club, 0, 0, 0, (short)score, stamp);

    [Fact]
    public void File_HasOriginalLayout()
    {
        var table = new HighscoreTable();
        table.Insert(new HighscoreEntry("Anna", "ECHT", 1, 2, 3, 0x1234, 0xA1B2C3D4));
        var data = table.ToBytes();

        Assert.Equal(464, data.Length);
        Assert.Equal((byte)'A', data[0]);
        Assert.Equal(0, data[4]); // Name mit 0 abgeschlossen, 26 Byte
        Assert.Equal((byte)'E', data[26]); // Verein ab 26
        Assert.Equal([1, 2, 3], data[49..52]);
        Assert.Equal([0x34, 0x12], data[52..54]);
        Assert.Equal([0xD4, 0xC3, 0xB2, 0xA1], data[54..58]);

        var back = HighscoreTable.Parse(data);
        Assert.Equal(table.Entries[0], back.Entries[0]);
    }

    [Fact]
    public void Parse_WrongLength_GivesEmptyList() =>
        Assert.All(HighscoreTable.Parse(new byte[10]).Entries, e => Assert.True(e.IsEmpty));

    [Fact]
    public void Insert_SortsDescending_AndKeepsEight()
    {
        var table = new HighscoreTable();
        foreach (var score in new[] { 5, 90, 40, 7, 300, 12, 60, 25, 1, 500 })
            table.Insert(E($"M{score}", "V", score));
        Assert.Equal([500, 300, 90, 60, 40, 25, 12, 7], table.Entries.Select(e => (int)e.Score));
    }

    [Fact]
    public void Insert_EqualScore_GoesBehindExisting()
    {
        var table = new HighscoreTable();
        table.Insert(E("Erster", "V", 50));
        table.Insert(E("Zweiter", "V", 50));
        Assert.Equal(["Erster", "Zweiter"], table.Entries.Take(2).Select(e => e.Name));
    }

    /// <summary>Gleicher Name und Verein ersetzt den alten Eintrag (kein Doppel).</summary>
    [Fact]
    public void Insert_SameNameAndClub_ReplacesInPlace()
    {
        var table = new HighscoreTable();
        table.Insert(E("Anna", "V", 100));
        table.Insert(E("Bert", "V", 80));
        table.Insert(E("Anna", "V", 120));
        Assert.Equal([("Anna", 120), ("Bert", 80)], table.Entries.Take(2).Select(e => (e.Name, (int)e.Score)));
        Assert.Equal(2, table.Entries.Count(e => !e.IsEmpty));
    }

    /// <summary>
    /// [ORIGINAL] Eigenheit von 0x5D7BB: Gibt es einen Platz mit weniger Punkten, ersetzt der neue Eintrag den gleichnamigen
    /// auch dann, wenn er schlechter ist (hier ist ein leerer Platz „weniger Punkte“).
    /// </summary>
    [Fact]
    public void Insert_SameNameWithFewerPoints_OverwritesBetterEntry()
    {
        var table = new HighscoreTable();
        table.Insert(E("Anna", "V", 100));
        table.Insert(E("Anna", "V", 60));
        Assert.Equal(60, table.Entries[0].Score);
        Assert.Equal(1, table.Entries.Count(e => !e.IsEmpty));
    }

    [Fact]
    public void Insert_BelowFullTable_IsDropped()
    {
        var table = new HighscoreTable();
        for (var i = 1; i <= 8; i++)
            table.Insert(E($"M{i}", "V", 100 + i));
        table.Insert(E("Zu schwach", "V", 50));
        Assert.DoesNotContain(table.Entries, e => e.Name == "Zu schwach");
    }

    [Fact]
    public void Key_WorldOnly_Is30000()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", Level = 0 });
        setup.ToggleClubCompetitions(); // Fahne aus → nur Weltkugel
        Assert.Equal(2, setup.ModeBits);
        Assert.Equal(30000, HighscoreRules.Key(setup));
    }

    /// <summary>[ORIGINAL] Datei 0x28A42: Level + 4 · Dauer + 32 · Startliga + 128 · Startstärke − 2; nur bei Level &gt; 2 und …</summary>
    [Theory]
    [InlineData(3, GameDuration.Unbegrenzt, League.Bundesliga, StartStrength.Mittelfeld, 3 + 12 + 0 + 128 - 2)]
    [InlineData(5, GameDuration.EineSaison, League.Bundesliga, StartStrength.OberesDrittel, 5 + 0 + 0 + 0 - 2)] // Dauer 0, Liga 0
    [InlineData(4, GameDuration.EineSaison, League.ZweiteLiga, StartStrength.Mittelfeld, 4 + 0 + 32 + 128 - 2)]
    [InlineData(3, GameDuration.DreiSaisons, League.Bundesliga, StartStrength.OberesDrittel, null)]
    [InlineData(2, GameDuration.Unbegrenzt, League.Bundesliga, StartStrength.Mittelfeld, null)] // Level zu klein
    [InlineData(0, GameDuration.EineSaison, League.Bundesliga, StartStrength.OberesDrittel, null)]
    // Bedingung „Dauer = eine Saison UND Startliga = Bundesliga“ (0x289A3–0x289C5), jede Hälfte einzeln (Review H2-02):
    [InlineData(4, GameDuration.EineSaison, League.ZweiteLiga, StartStrength.OberesDrittel, null)] // nur Dauer passt
    [InlineData(4, GameDuration.EineSaison, League.Oberliga, StartStrength.Abstiegskampf, null)] // nur Dauer passt
    [InlineData(4, GameDuration.DreiSaisons, League.Bundesliga, StartStrength.Abstiegskampf, null)] // nur Liga passt
    [InlineData(5, GameDuration.EineSaison, League.Bundesliga, StartStrength.Abstiegskampf, 5 + 0 + 0 + 256 - 2)] // beides passt
    public void Key_FromSetup(int level, GameDuration duration, League league, StartStrength strength, int? key)
    {
        var setup = new GameSetup { Duration = duration };
        setup.Managers.Add(new ManagerSetup { Name = "A", Level = level, StartLeague = league, StartStrength = strength });
        Assert.Equal(key, HighscoreRules.Key(setup));
    }

    [Fact]
    public void FileName_IsHDotKey() => Assert.Equal("h.131", HighscoreRules.FileName(131));

    // ---- Punkteformel (Datei 0x5D04F) --------------------------------------------------------------------------------------

    /// <summary>
    /// Goldwerte: Die Funktion 0x5D04F wurde in einem eigenen 8086-Emulator (nicht im Repo) mit zufälligen Rohwerten ausgeführt;
    /// die Aufrufe der Nachbarfunktionen (Bewertung, Schulden, Spielerwerte, Laufzeit-Multiplikation und -Division) sind dort durch
    /// ihre Ergebnisse ersetzt. Das Modell dieser Datei stimmte in 500 Zufallsfällen mit dem Original überein; hier stehen 14 davon.
    /// </summary>
    public static IEnumerable<object[]> EmulatorCases => Cases.Select(c => new object[] { c.Input, c.Expected });

    private sealed record Case(HighscoreInputs Input, int Expected)
    {
    }

    private static Case[] Cases => [
        new(
            new HighscoreInputs
            {
                Rating = 65, StadiumLevels = [2, 1, 3, 1, 1, 2, 2, 0, 1], Seats = 9000, Standing = 12000,
                Wealth = 52690359, Rank0 = 4, LeagueOffset = 0, Cups = [6, 5, 30, 3],
                History = [new(0x09, 0x06, 0x8D), new(0x0A, 0x02, 0x8D), new(0x34, 0x04, 0x09), new(0x0B, 0x86, 0x00), new(0x0B, 0x05, 0x0A)],
            }, 3234),
        new(
            new HighscoreInputs
            {
                Rating = 74, StadiumLevels = [3, 1, 3, 2, 2, 2, 1, 2, 3], Seats = 7702, Standing = 17260,
                Wealth = 87990184, Rank0 = 6, LeagueOffset = 12, Cups = [6, 3, 1, 0],
                History = [new(0xB5, 0x1E, 0x00), new(0x0A, 0x04, 0x09), new(0x02, 0x04, 0x8D), new(0x02, 0x03, 0x00), new(0x34, 0x03, 0x09), new(0x34, 0x86, 0x00)],
            }, 3997),
        new(
            new HighscoreInputs
            {
                Rating = 78, StadiumLevels = [1, 2, 3, 0, 1, 1, 2, 2, 1], Seats = 13578, Standing = 20638,
                Wealth = 36773237, Rank0 = 10, LeagueOffset = 24, Cups = [30, 0, 5, 6],
                History = [new(0x06, 0x1E, 0x8D), new(0x01, 0x03, 0x8D), new(0x03, 0x04, 0x00)],
            }, 3003),
        new(
            new HighscoreInputs
            {
                Rating = 98, StadiumLevels = [2, 2, 0, 1, 3, 1, 3, 0, 3], Seats = 17035, Standing = 14627,
                Wealth = 59224453, Rank0 = 5, LeagueOffset = 12, Cups = [5, 1, 30, 30],
                History = [],
            }, 3276),
        new(
            new HighscoreInputs
            {
                Rating = 35, StadiumLevels = [3, 1, 2, 1, 1, 1, 3, 1, 0], Seats = 18137, Standing = 29175,
                Wealth = 103875978, Rank0 = 7, LeagueOffset = 0, Cups = [2, 2, 6, 4],
                History = [new(0x07, 0x01, 0x00), new(0x09, 0x81, 0x00), new(0x03, 0x02, 0x8D), new(0x06, 0x01, 0x09), new(0x0C, 0x81, 0x0A)],
            }, 3235),
        new(
            new HighscoreInputs
            {
                Rating = 68, StadiumLevels = [0, 3, 1, 3, 2, 2, 1, 3, 0], Seats = 1968, Standing = 1382,
                Wealth = 61511469, Rank0 = 3, LeagueOffset = 24, Cups = [3, 30, 5, 3],
                History = [],
            }, 3038),
        new(
            new HighscoreInputs
            {
                Rating = 5, StadiumLevels = [2, 2, 2, 3, 3, 0, 0, 3, 3], Seats = 12934, Standing = 19130,
                Wealth = 56154543, Rank0 = 9, LeagueOffset = 24, Cups = [5, 30, 4, 30],
                History = [new(0xFF, 0x86, 0x8D), new(0xFF, 0x86, 0x0A), new(0x02, 0x06, 0x0A), new(0x08, 0x05, 0x8D), new(0x05, 0x06, 0x0A), new(0x01, 0x06, 0x0A), new(0xFF, 0x04, 0x0A)],
            }, 3099),
        new(
            new HighscoreInputs
            {
                Rating = 55, StadiumLevels = [3, 3, 3, 3, 2, 1, 2, 3, 2], Seats = 18680, Standing = 25491,
                Wealth = 57906781, Rank0 = 9, LeagueOffset = 12, Cups = [30, 2, 30, 30],
                History = [new(0x0C, 0x02, 0x00), new(0x02, 0x81, 0x8D), new(0xB5, 0x81, 0x0A), new(0x35, 0x02, 0x09), new(0x0C, 0x03, 0x00), new(0xFF, 0x03, 0x09)],
            }, 3752),
        new(
            new HighscoreInputs
            {
                Rating = 33, StadiumLevels = [2, 1, 1, 1, 0, 1, 3, 3, 3], Seats = 8836, Standing = 21890,
                Wealth = 78559035, Rank0 = 2, LeagueOffset = 0, Cups = [1, 2, 1, 6],
                History = [new(0x01, 0x05, 0x09), new(0x34, 0x04, 0x0A), new(0xFF, 0x02, 0x8D), new(0x33, 0x86, 0x8D), new(0xFF, 0x06, 0x8D), new(0x0A, 0x06, 0x8D), new(0x0A, 0x86, 0x09)],
            }, 3046),
        new(
            new HighscoreInputs
            {
                Rating = 66, StadiumLevels = [2, 3, 1, 1, 3, 3, 2, 3, 2], Seats = 13818, Standing = 10380,
                Wealth = 102710912, Rank0 = 11, LeagueOffset = 0, Cups = [30, 6, 2, 4],
                History = [new(0x08, 0x1E, 0x09), new(0x03, 0x03, 0x09), new(0x34, 0x02, 0x8D)],
            }, 4064),
        new(
            new HighscoreInputs
            {
                Rating = 83, StadiumLevels = [1, 2, 1, 0, 0, 3, 1, 0, 0], Seats = 4882, Standing = 21084,
                Wealth = 19767897, Rank0 = 11, LeagueOffset = 24, Cups = [3, 6, 6, 1],
                History = [new(0x0A, 0x86, 0x8D), new(0x01, 0x1E, 0x0A), new(0x04, 0x03, 0x0A)],
            }, 2468),
        new(
            new HighscoreInputs
            {
                Rating = 59, StadiumLevels = [2, 0, 3, 0, 2, 0, 0, 1, 2], Seats = 13526, Standing = 18049,
                Wealth = 94438506, Rank0 = 8, LeagueOffset = 0, Cups = [4, 4, 3, 2],
                History = [new(0x09, 0x1E, 0x8D)],
            }, 2943),
        new(
            new HighscoreInputs
            {
                Rating = 82, StadiumLevels = [1, 2, 2, 3, 3, 2, 0, 3, 2], Seats = 5762, Standing = 7791,
                Wealth = 49074154, Rank0 = 11, LeagueOffset = 12, Cups = [30, 30, 6, 6],
                History = [new(0x0C, 0x01, 0x09), new(0x35, 0x06, 0x09), new(0x85, 0x06, 0x00), new(0x34, 0x04, 0x0A)],
            }, 3630),
        new(
            new HighscoreInputs
            {
                Rating = 100, StadiumLevels = [0, 3, 0, 2, 1, 1, 1, 2, 3], Seats = 17819, Standing = 1246,
                Wealth = 49610954, Rank0 = 1, LeagueOffset = 0, Cups = [4, 4, 5, 6],
                History = [new(0xFF, 0x02, 0x0A)],
            }, 3006)
    ];

    [Theory]
    [MemberData(nameof(EmulatorCases))]
    public void Points_MatchOriginalFunction(HighscoreInputs input, int expected) => Assert.Equal(expected, HighscoreRules.Points(input));

    /// <summary>Eigenheit des Originals: Der Meister (Kennzahl 0x80 | 0x35) bekommt einen negativen Ligaterm, (36 − 181) · 7.</summary>
    [Fact]
    public void Points_ChampionSeason_HasNegativeLeagueTerm()
    {
        var champion = new HighscoreInputs { Rank0 = 35, History = [new SeasonCodes(0xB5, 0x1E, 0)] };
        var runnerUp = new HighscoreInputs { Rank0 = 35, History = [new SeasonCodes(0x35, 0x1E, 0)] };
        Assert.True(HighscoreRules.Points(champion) < HighscoreRules.Points(runnerUp));
    }

    /// <summary>Eigenheit: „nicht im Wettbewerb“ (0x1E) zählt beim DEB-Pokal 10 · (0x1E &amp; 7) = 60 Punkte (Summe, vor Teilung).</summary>
    [Fact]
    public void Points_DebNotParticipating_Counts60()
    {
        var input = new HighscoreInputs { Rank0 = 35, History = [new SeasonCodes(SeasonCodes.Empty, SeasonCodes.DebNotParticipating, 0)] };
        // 60 / (1 + 1) = 30; 30 / 20 = 1
        Assert.Equal(1, HighscoreRules.Points(input));
        var winner = new HighscoreInputs { Rank0 = 35, History = [new SeasonCodes(SeasonCodes.Empty, 0x86, SeasonCodes.EuropeWinner)] };
        // 80 + 130 = 210 / 2 = 105; 105 / 20 = 5
        Assert.Equal(5, HighscoreRules.Points(winner));
    }

    /// <summary>Eigenheit: Der Europapokal zählt nur, wenn die DEB-Kennzahl vor der Maskierung ≥ 8 war (Sieger oder 0x1E).</summary>
    [Fact]
    public void Points_EuropeIgnored_ForEliminatedDebParticipant()
    {
        var eliminated = new HighscoreInputs { Rank0 = 35, History = [new SeasonCodes(SeasonCodes.Empty, 0x03, SeasonCodes.EuropeWinner)] };
        var plain = new HighscoreInputs { Rank0 = 35, History = [new SeasonCodes(SeasonCodes.Empty, 0x03, 0)] };
        Assert.Equal(HighscoreRules.Points(plain), HighscoreRules.Points(eliminated));
    }

    /// <summary>Stadion: Sitz- und Stehplätze rechnen als 32-Bit-Produkt ohne Vorzeichen (Überlauf wie im Original).</summary>
    [Fact]
    public void Points_SeatsProduct_Overflows32Bit()
    {
        var big = HighscoreRules.Points(new HighscoreInputs { Rank0 = 35, Seats = 8000 });
        var small = HighscoreRules.Points(new HighscoreInputs { Rank0 = 35, Seats = 7000 });
        Assert.True(big < small, "8000 Plätze × 540000 überlaufen 2³²");
    }

    [Fact]
    public void Points_WealthCapped_At50Million()
    {
        var capped = HighscoreRules.Points(new HighscoreInputs { Rank0 = 35, Wealth = 50_000_000 });
        Assert.Equal(capped, HighscoreRules.Points(new HighscoreInputs { Rank0 = 35, Wealth = 900_000_000 }));
        Assert.Equal(1250, capped); // 50 Mio. Drittel / 2000 = 25.000, geteilt durch 20
    }

    [Fact]
    public void SeasonCodes_FromHistory()
    {
        var h = new ManagerSeasonHistory(1994, 3, League.Bundesliga, 4, LeagueOutcome.Rank, CupOutcome.Eliminated, 2, CupOutcome.NotParticipating, false);
        Assert.Equal(new SeasonCodes(4, 3, 0), SeasonCodes.Of(h));
        var cupWinner = h with { Deb = CupOutcome.Winner, DebRound = 5, Europe = CupOutcome.Winner };
        Assert.Equal(new SeasonCodes(4, 0x86, 0x8D), SeasonCodes.Of(cupWinner));
        Assert.Equal(0x33, SeasonCodes.Of(h with { LeagueOutcome = LeagueOutcome.PlayoffQuarterFinal }).League);
        Assert.Equal(SeasonCodes.DebNotParticipating, SeasonCodes.Of(h with { Deb = CupOutcome.NotParticipating }).Deb);
    }
}
