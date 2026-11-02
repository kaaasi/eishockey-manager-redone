using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur wettbewerbe-spiel, Teil 7: Kaderplatz +0x19 (A185, A186), docs/analyse/re-nachtrag.md Abschnitt 6.
/// Rot vor Teil 7: alle Spieler hatten 7/7 (Vorgabe in <c>Squad.Training.cs</c>).
/// </summary>
public class HiddenSkillsTests
{
    private static IEnumerable<Player> ManagedPlayers(GameState game) =>
        game.Managers.Select(m => m.ClubIndex).Distinct().SelectMany(i => game.Clubs[i].Players);

    /// <summary>[ORIGINAL] Datei 0x299F4: Spielstart, beide Halbbytes random(3, 12); FILE01/09/10/12 alle in 3 … 12.</summary>
    [Fact]
    public void NewGame_ManagedSquads_StartValuesThreeToTwelve()
    {
        var game = GameModeTests.Create(3, true, true, 2, 30);
        var players = ManagedPlayers(game).ToList();
        Assert.True(players.Count > 30);
        Assert.All(players, p =>
        {
            Assert.InRange(p.HiddenSkill1, HiddenSkills.StartMin, HiddenSkills.StartMax);
            Assert.InRange(p.HiddenSkill2, HiddenSkills.StartMin, HiddenSkills.StartMax);
        });
        // Verteilung: zehn Werte je Halbbyte; bei über 60 Würfen erscheinen Rand und Mitte.
        var values = players.SelectMany(p => new[] { p.HiddenSkill1, p.HiddenSkill2 }).ToHashSet();
        Assert.True(values.Count >= 8, string.Join(",", values));
        Assert.Contains(players, p => p.HiddenSkill1 != p.HiddenSkill2);
    }

    /// <summary>Startwürfe hängen am Startwert und sind wiederholbar; Computervereine behalten die Vorgabe.</summary>
    [Fact]
    public void NewGame_StartValues_DeterministicPerSeed_ComputerClubsUnchanged()
    {
        static string Print(GameState g) => string.Join(",", ManagedPlayers(g).Select(p => p.HiddenSkill2 << 4 | p.HiddenSkill1));
        Assert.Equal(Print(TestGames.Create(3)), Print(TestGames.Create(3)));
        Assert.NotEqual(Print(TestGames.Create(3)), Print(TestGames.Create(4)));
        var game = TestGames.Create(3);
        var managed = game.Managers.Select(m => m.ClubIndex).ToHashSet();
        Assert.All(game.Clubs.Where(c => !managed.Contains(c.Index)).SelectMany(c => c.Players),
            p => Assert.Equal((7, 7), (p.HiddenSkill2, p.HiddenSkill1)));
    }

    /// <summary>
    /// [APPROX] Eigene Zufallsquelle: Der Startwurf verbraucht <see cref="GameState.Random"/> nicht.
    /// </summary>
    [Fact]
    public void NewGame_StartValues_DoNotConsumeGameRandom()
    {
        var game = TestGames.Create(3);
        var before = game.Random.Capture();
        var a = HiddenSkills.SourceFor(game, 0x19).Next();
        Assert.Equal(before, game.Random.Capture());
        Assert.Equal(a, HiddenSkills.SourceFor(game, 0x19).Next());
    }

    /// <summary>[ORIGINAL] Datei 0x06F23: Talent bekommt den Startwurf.</summary>
    [Fact]
    public void Talent_GetsStartValues()
    {
        var game = TestGames.Create(3);
        var random = new Random(5);
        var talents = Enumerable.Range(0, 40)
            .Select(i => PlayerFactory.CreateTalent(game, $"T{i}", PlayerRole.Forward, 50, random)).ToList();
        Assert.All(talents, p =>
        {
            Assert.InRange(p.HiddenSkill1, HiddenSkills.StartMin, HiddenSkills.StartMax);
            Assert.InRange(p.HiddenSkill2, HiddenSkills.StartMin, HiddenSkills.StartMax);
        });
        Assert.True(talents.Select(p => p.HiddenSkill2 << 4 | p.HiddenSkill1).Distinct().Count() > 10);
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x497C7–0x49803: Listenwechsel zieht +0x19 für den neuen Marktplatz (bisher gezogen und verworfen);
    /// die Zufallsfolge bleibt gleich, weil die Züge schon vorhanden waren.
    /// </summary>
    [Fact]
    public void MarketListing_UsesDrawnValues()
    {
        var game = TestGames.Create(3);
        game.TransferMarket.ChangeList(game, new GameRandom(11));
        var market = game.TransferMarket.Pool.Where(s => s.Owner == PoolOwner.Market).Select(s => s.Player).ToList();
        Assert.NotEmpty(market);
        Assert.All(market, p =>
        {
            Assert.InRange(p.HiddenSkill1, HiddenSkills.StartMin, HiddenSkills.StartMax);
            Assert.InRange(p.HiddenSkill2, HiddenSkills.StartMin, HiddenSkills.StartMax);
        });
        Assert.Contains(market, p => (p.HiddenSkill2, p.HiddenSkill1) != (7, 7));
    }

    /// <summary>Reihenfolge der Züge: oberes Halbbyte (Überzahl) zuerst. [ORIGINAL] Datei 0x299B7–0x299F4.</summary>
    [Fact]
    public void Roll_UpperNibbleFirst()
    {
        var p = new Player { Name = "X", Role = PlayerRole.Forward };
        var check = new GameRandom(9);
        HiddenSkills.Roll(p, new GameRandom(9));
        Assert.Equal(TransferRules.Random16(check, 3, 12), p.HiddenSkill2);
        Assert.Equal(TransferRules.Random16(check, 3, 12), p.HiddenSkill1);
    }

    /// <summary>
    /// RN-1: [ORIGINAL] Datei 0x3CB2C – der Kader des Nationalteams hat 0x77. Länderspiele rechnen mit 7/7, nicht mit den
    /// Vereinswerten der Spieler; der Vereinswert bleibt unverändert.
    /// </summary>
    [Theory]
    [InlineData(false, 15)]
    [InlineData(true, 7)]
    public void NationalMatch_UsesSevenSeven(bool national, int expectedUnderplay)
    {
        var squad = new LiveSquad { Difficulty = 2, NationalCoach = national };
        foreach (var (role, pos) in new[] { (PlayerRole.Goalkeeper, 1), (PlayerRole.Defender, 1), (PlayerRole.Defender, 1) })
            squad.Players.Add(new LivePlayer
            {
                Player = new Player { Name = "X", Role = role, Condition = 60, Technique = 70, Form = 50, HiddenSkill1 = 15, HiddenSkill2 = 15 },
                Position = pos,
                Dressed = true,
                Energy = 100,
            });
        var even = MatchEngine.ManagedLineValues(squad, 0, 0, 0, 60, 1, inMatch: true, national);
        var shortHanded = MatchEngine.ManagedLineValues(squad, 0, 0, -1, 60, 1, inMatch: true, national);
        Assert.Equal(Math.Max(0, even.Technique - (45 - 3 * expectedUnderplay)), shortHanded.Technique);
        Assert.All(squad.Players, p => Assert.Equal(15, p.Player.HiddenSkill1));
    }

    /// <summary>
    /// Mehrsaisonprobe (Modus 4, passiv und aktiv, vier Saisonwechsel): Werte der Managerkader bleiben in 0 … 15, neue Spieler
    /// im Managerkader haben gewürfelte Werte.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FourSeasonChanges_ValuesStayInRange(bool active)
    {
        var game = GameModeTests.Create(3, true, true, 2, 30);
        var changes = 0;
        var rolled = 0;
        RngTwinProbeTests.Run(game, active, beforeChange: g =>
        {
            changes++;
            var players = ManagedPlayers(g).ToList();
            Assert.All(players, p =>
            {
                Assert.InRange(p.HiddenSkill1, 0, HiddenSkills.Max);
                Assert.InRange(p.HiddenSkill2, 0, HiddenSkills.Max);
            });
            rolled += players.Count(p => (p.HiddenSkill2, p.HiddenSkill1) != (7, 7));
        });
        Assert.Equal(4, changes);
        // Passiv laufen die Verträge aus (Kader am Ende leer, wie in RngTwinProbeTests); in der ersten Saison zählen die
        // Startwürfe, aktiv auch die verlängerten und neu verpflichteten Spieler.
        Assert.True(rolled > 20, $"rolled={rolled}");
    }
}
