using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur kader-start (#168): Kondition und Technik der Kaderspieler zu Spielbeginn.
/// [ORIGINAL] em.exe Datei 0x29DA4 ff.: Basis = Tabelle DGROUP 0x4B42 [Start-Liga · 3 + Start-Stärke],
/// je Spieler random(Basis − 2, Basis + 2), Form random(40, 60).
/// </summary>
public class KaderStartTests
{
    /// <summary>
    /// folge01 (Berliner SC, Oberliga, Abstiegskampf, Level 5; Aufstellung folge01_00528/_00550):
    /// alle 20 Spieler mit Kondition und Technik 19–23.
    /// </summary>
    [GameDataFact]
    public void Folge01_BerlinerSc_Abstiegskampf_Values19To23()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup
        {
            Name = "Kingsday", ClubIndex = 42, Level = 5,
            StartLeague = League.Oberliga, StartStrength = StartStrength.Abstiegskampf,
        });
        var game = NewGame.Create(ClubDatabase.Load(TestGameData.Load()), setup, seed: 1);
        var club = game.Clubs[42];

        Assert.Equal(League.Oberliga, club.League);
        Assert.Equal(20, club.Players.Count);
        Assert.All(club.Players, p =>
        {
            Assert.InRange(p.Condition, 19, 23);
            Assert.InRange(p.Technique, 19, 23);
            Assert.InRange(p.Form, 40, 60);
        });
    }

    /// <summary>
    /// Gegenprobe UI-Video (oberflaeche_0900/_0910): Berliner SC spielt Oberliga (DEB-Pokal oberflaeche_0400),
    /// Start-Stärke Mittelfeld → Werte 29–33.
    /// </summary>
    [GameDataFact]
    public void UiVideo_BerlinerSc_Mittelfeld_Values29To33()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "LetsShow", ClubIndex = 42, StartStrength = StartStrength.Mittelfeld });
        var game = NewGame.Create(ClubDatabase.Load(TestGameData.Load()), setup, seed: 2);

        Assert.All(game.Clubs[42].Players, p =>
        {
            Assert.InRange(p.Condition, 29, 33);
            Assert.InRange(p.Technique, 29, 33);
        });
    }

    /// <summary>Tabelle DGROUP 0x4B42 (Datei 0x29DB6): Zeile = Liga, Spalte = Start-Stärke.</summary>
    [Theory]
    [InlineData(League.Bundesliga, StartStrength.OberesDrittel, 87)]
    [InlineData(League.Bundesliga, StartStrength.Mittelfeld, 81)]
    [InlineData(League.Bundesliga, StartStrength.Abstiegskampf, 70)]
    [InlineData(League.ZweiteLiga, StartStrength.OberesDrittel, 68)]
    [InlineData(League.ZweiteLiga, StartStrength.Mittelfeld, 55)]
    [InlineData(League.ZweiteLiga, StartStrength.Abstiegskampf, 42)]
    [InlineData(League.Oberliga, StartStrength.OberesDrittel, 39)]
    [InlineData(League.Oberliga, StartStrength.Mittelfeld, 31)]
    [InlineData(League.Oberliga, StartStrength.Abstiegskampf, 21)]
    public void StartBase_Table(League league, StartStrength strength, int expected) =>
        Assert.Equal(expected, SquadSetup.StartBase(league, strength));

    /// <summary>Der Spiel-Level wirkt nicht auf die Startwerte (im Original kein Lesezugriff in diesem Abschnitt).</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(5)]
    public void StartValues_IndependentOfLevel(int level)
    {
        var game = SyntheticGame(level, League.ZweiteLiga, StartStrength.OberesDrittel);
        var club = game.Clubs[game.Managers[0].ClubIndex];
        Assert.All(club.Players, p =>
        {
            Assert.InRange(p.Condition, 66, 70);
            Assert.InRange(p.Technique, 66, 70);
        });
    }

    /// <summary>
    /// Mehrere Manager: jeder bekommt die Basis aus der Liga seines Vereins und seiner eigenen Start-Stärke.
    /// Computervereine behalten die Herleitung aus der Datenbankstärke.
    /// </summary>
    [Fact]
    public void TwoManagers_EachOwnBase_ComputerClubsUnchanged()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", StartLeague = League.Bundesliga, StartStrength = StartStrength.OberesDrittel });
        setup.Managers.Add(new ManagerSetup { Name = "B", StartLeague = League.Oberliga, StartStrength = StartStrength.Abstiegskampf });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, seed: 5);

        AssertRange(game.Clubs[game.Managers[0].ClubIndex], 85, 89);
        AssertRange(game.Clubs[game.Managers[1].ClubIndex], 19, 23);

        var managed = game.Managers.Select(m => m.ClubIndex).ToHashSet();
        foreach (var club in game.Clubs.Where(c => c.Players.Count > 0 && !managed.Contains(c.Index)))
        {
            var b = SquadSetup.BaseValue(club.Strength);
            var spread = b / 10 + 2;
            AssertRange(club, b - spread, b + spread);
        }

        static void AssertRange(Club club, int min, int max) => Assert.All(club.Players, p =>
        {
            Assert.InRange(p.Condition, min, max);
            Assert.InRange(p.Technique, min, max);
        });
    }

    /// <summary>
    /// Mehrsaisonprobe (Abnahme 3): stärkste Startbasis (Bundesliga, Oberes Drittel = 87) und schwächste (Oberliga,
    /// Abstiegskampf = 21), je passiv und aktiv über vier Saisonwechsel mit den Prüfungen der Spur kader
    /// (kein Spieler verloren oder doppelt, Verträge angekündigt). Die geänderten Abdrücke stehen in
    /// <see cref="RngTwinProbeTests"/>.
    /// </summary>
    [Theory]
    [InlineData(League.Bundesliga, StartStrength.OberesDrittel, false)]
    [InlineData(League.Bundesliga, StartStrength.OberesDrittel, true)]
    [InlineData(League.Oberliga, StartStrength.Abstiegskampf, false)]
    [InlineData(League.Oberliga, StartStrength.Abstiegskampf, true)]
    public void MultiSeason_FourChanges_ExtremeStartBases(League league, StartStrength strength, bool active)
    {
        var game = SyntheticGame(3, league, strength);
        var club = game.Clubs[game.Managers[0].ClubIndex];
        var start = SquadSetup.StartBase(league, strength);
        Assert.All(club.Players, p => Assert.InRange(p.Condition, start - 2, start + 2));

        SquadLifecycleTests.RunProbe(game, active, changes: 4);

        Assert.Equal(1997, game.Season);
        Assert.All(game.Clubs.SelectMany(c => c.Players), p =>
        {
            Assert.InRange(p.Condition, 1, Player.MaxValue);
            Assert.InRange(p.Technique, 1, Player.MaxValue);
        });
    }

    private static GameState SyntheticGame(int level, League league, StartStrength strength)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "T", Level = level, StartLeague = league, StartStrength = strength });
        // Runde 12, Teil 2: Seed 9 → 12 (anderer Zufallsverlauf ohne Verein 2 als Zufallswahl; bei Seed 9/10 bleibt der aktive Manager unter 18 Spielern).
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed: 12);
    }
}
