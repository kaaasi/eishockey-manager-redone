using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur „speichern“, Teil „ids“, Abnahme 2: Jeder Spieler hat eine dauerhafte, eindeutige ID; nach 4 Saisonwechseln
/// (passiv und aktiv) sind alle IDs über Kader, Pool, Leihen und Laufbahnende eindeutig, kein Verweis zeigt ins
/// Leere und keine ID wird neu vergeben.
/// </summary>
public class PlayerIdentityTests
{
    [Fact]
    public void NewGame_EveryPlayerHasUniqueId_InClubOrder()
    {
        var game = TestGames.Create();
        var players = game.Clubs.SelectMany(c => c.Players).ToArray();
        Assert.NotEmpty(players);
        Assert.Equal(Enumerable.Range(1, players.Length), players.Select(p => p.Id));
        Assert.Equal(players.Length + 1, game.NextPlayerId);
        Assert.Empty(game.ValidatePlayerIdentity());
    }

    [Fact]
    public void FindPlayer_ReturnsThePlayerOrNull()
    {
        var game = TestGames.Create();
        var player = game.Clubs.SelectMany(c => c.Players).Last();
        Assert.Same(player, game.FindPlayer(player.Id));
        Assert.Null(game.FindPlayer(0));
        Assert.Null(game.FindPlayer(game.NextPlayerId));
    }

    /// <summary>Gegenprobe: Die Prüfung findet doppelte und fehlende IDs sowie Verweise ins Leere.</summary>
    [Fact]
    public void Validate_ReportsBrokenIdentity()
    {
        var game = TestGames.Create();
        var own = game.Clubs[game.Managers[0].ClubIndex];
        var other = game.Clubs.First(c => c != own && c.Players.Count > 0);

        var twice = other.Players[0];
        own.Players.Add(twice);
        Assert.Contains(game.ValidatePlayerIdentity(), m => m.Contains("mehrfach"));
        own.Players.Remove(twice);

        var withoutId = PlayerFactory.CreateTalent("OHNE ID", PlayerRole.Forward, 50, new Random(1));
        own.Players.Add(withoutId);
        Assert.Contains(game.ValidatePlayerIdentity(), m => m.Contains("ungültige ID 0"));
        own.Players.Remove(withoutId);
        Assert.Empty(game.ValidatePlayerIdentity());

        var lineupPlayer = own.Lineup.Players.First();
        own.Players.Remove(lineupPlayer);
        Assert.Contains(game.ValidatePlayerIdentity(), m => m.Contains("gehört nicht zum Kader"));
    }

    /// <summary>Talente (Kader-KI der Computervereine) bekommen neue IDs oberhalb aller bisherigen.</summary>
    [Fact]
    public void Talent_GetsFreshId()
    {
        var game = TestGames.Create();
        var before = game.NextPlayerId;
        var talent = PlayerFactory.CreateTalent(game, "TALENT", PlayerRole.Forward, 50, new Random(1));
        Assert.Equal(before, talent.Id);
        Assert.Equal(before + 1, game.NextPlayerId);
    }

    public static TheoryData<bool, int> Probes => new() { { false, 3 }, { true, 3 }, { false, 11 }, { true, 11 } };

    [Theory]
    [MemberData(nameof(Probes))]
    public void FourSeasonChanges_IdsStayUniqueAndAreNeverReused(bool active, int seed) =>
        RunProbe(TestGames.Create(seed), active);

    [GameDataTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void FourSeasonChanges_OriginalDatabase(bool active)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Probe", ClubIndex = 42 });
        RunProbe(NewGame.Create(ClubDatabase.Load(TestGameData.Load()), setup, seed: 3), active);
    }

    private static void RunProbe(GameState game, bool active)
    {
        var seen = new Dictionary<int, Player>();
        var maximum = 0;
        var checks = 0;
        var talents = 0;
        RngTwinProbeTests.Run(game, active, Check);
        Assert.True(checks > 100, "zu wenige Prüfungen");
        Assert.True(talents > 0, "keine neuen Spieler in vier Saisons");
        Assert.NotEmpty(game.RetiredPlayers);

        void Check(GameState g)
        {
            checks++;
            Assert.Empty(g.ValidatePlayerIdentity());
            var fresh = new List<Player>();
            foreach (var p in g.AllPlayers)
            {
                if (seen.TryGetValue(p.Id, out var known))
                    Assert.Same(known, p); // eine ID gehört immer demselben Spieler
                else
                    fresh.Add(p);
            }
            // Neue Spieler bekommen nur IDs oberhalb aller bisher gesehenen: keine ID wird neu vergeben.
            Assert.All(fresh, p => Assert.True(p.Id > maximum, $"{p.Name}: ID {p.Id} ≤ {maximum}"));
            if (maximum > 0)
                talents += fresh.Count;
            foreach (var p in fresh)
                seen[p.Id] = p;
            if (fresh.Count > 0)
                maximum = Math.Max(maximum, fresh.Max(p => p.Id));
        }
    }
}
