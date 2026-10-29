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
    public void Key_FromSetup(int level, GameDuration duration, League league, StartStrength strength, int? key)
    {
        var setup = new GameSetup { Duration = duration };
        setup.Managers.Add(new ManagerSetup { Name = "A", Level = level, StartLeague = league, StartStrength = strength });
        Assert.Equal(key, HighscoreRules.Key(setup));
    }

    [Fact]
    public void FileName_IsHDotKey() => Assert.Equal("h.131", HighscoreRules.FileName(131));
}
