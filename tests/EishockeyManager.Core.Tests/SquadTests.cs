using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

public class SquadTests
{
    [Fact]
    public void ManagedClub_GetsSquadOfLevel3_WithAutoLineup()
    {
        var game = TestGames.Create();
        var club = game.Clubs[2];
        Assert.Equal(2, club.Players.Count(p => p.Role == PlayerRole.Goalkeeper));
        Assert.Equal(8, club.Players.Count(p => p.Role == PlayerRole.Defender));
        Assert.Equal(10, club.Players.Count(p => p.Role == PlayerRole.Forward));
        Assert.Equal(Enumerable.Range(1, 20), club.Players.Select(p => p.Number));

        // Auto-Aufstellung: Torhüter + drei Blöcke = 16 Spieler, Block 4 bleibt leer (Video: „Eingesetzt: 20“
        // zählt den Kader, 4 Spieler stehen auf RESERVE).
        Assert.Equal(16, club.Lineup.Count);
        Assert.Empty(club.Lineup.Block(3));
        Assert.Equal(4, club.Players.Count(p => club.StatusOf(p) == PlayerStatus.Reserve));
        Assert.Equal(club.Players.Where(p => p.Role == PlayerRole.Goalkeeper).MaxBy(TeamStrength.PlayerValue), club.Lineup.Goalkeeper);
    }

    [Fact]
    public void AllLeagueClubs_HaveSquadsAndStartAtDatabaseStrength()
    {
        var game = TestGames.Create();
        foreach (var club in game.Clubs.Where(c => c.League is not null))
        {
            Assert.True(club.Players.Count >= 20, club.Name);
            Assert.Equal(club.Strength, club.MatchStrength);
            Assert.All(club.Players, p =>
            {
                Assert.InRange(p.Age, SquadSetup.MinAge, SquadSetup.MaxAge);
                Assert.InRange(p.Talent, 0, 9);
                Assert.Equal(2, p.ContractYears);
                Assert.True(p.MonthlySalary >= 100);
            });
        }
        Assert.Equal(game.Clubs.SelectMany(c => c.Players).Count(), game.Clubs.SelectMany(c => c.Players).Select(p => p.Name).Distinct().Count());
    }

    [Fact]
    public void Squads_AreReproducible()
    {
        var a = TestGames.Create(seed: 5).Clubs[2].Players.Select(p => (p.Name, p.Condition, p.Technique, p.Form)).ToList();
        var b = TestGames.Create(seed: 5).Clubs[2].Players.Select(p => (p.Name, p.Condition, p.Technique, p.Form)).ToList();
        Assert.Equal(a, b);
    }

    [Fact]
    public void WeakerLineup_LowersMatchStrength()
    {
        var club = TestGames.Create().Clubs[2];
        var before = club.MatchStrength;
        club.Lineup.Clear(LineupSlot.Goalkeeper);
        Assert.True(club.MatchStrength < before);
        club.Lineup.FillGaps(club.Players);
        Assert.Equal(before, club.MatchStrength);

        // Reservespieler in Block 4 entlasten: mindestens gleich stark
        club.Lineup.FillGaps(club.Players, blocks: Lineup.Blocks);
        Assert.True(club.MatchStrength >= before - 1);
    }

    [Fact]
    public void Assign_OnlyMatchingRole_AndSwaps()
    {
        var club = TestGames.Create().Clubs[2];
        var lineup = club.Lineup;
        var forward = club.Players.First(p => p.Role == PlayerRole.Forward && lineup.Contains(p));
        Assert.False(lineup.Assign(LineupSlot.Defender(0, 0), forward)); // „Der darf da nicht rein !“

        var a = lineup[LineupSlot.Forward(0, 0)]!;
        var b = lineup[LineupSlot.Forward(1, 0)]!;
        Assert.True(lineup.Assign(LineupSlot.Forward(1, 0), a));
        Assert.Same(a, lineup[LineupSlot.Forward(1, 0)]);
        Assert.Same(b, lineup[LineupSlot.Forward(0, 0)]);

        var injured = lineup[LineupSlot.Defender(0, 0)]!;
        injured.InjuredGames = 3;
        Assert.Equal(PlayerStatus.Injured, club.StatusOf(injured));
        lineup.RemoveUnavailable(club.Players);
        Assert.False(lineup.Contains(injured));
        Assert.False(lineup.Assign(LineupSlot.Defender(0, 0), injured));
    }

    [Fact]
    public void Strength_IsAverageOfConditionTechniqueForm()
    {
        // Belegt im Video: 33/30/41 → 34, 16/20/53 → 29
        Assert.Equal(34, new Player { Name = "A", Role = PlayerRole.Goalkeeper, Condition = 33, Technique = 30, Form = 41 }.Strength);
        Assert.Equal(29, new Player { Name = "B", Role = PlayerRole.Defender, Condition = 16, Technique = 20, Form = 53 }.Strength);
    }
}

public class ContractTests
{
    private static Player NewPlayer() => new()
    {
        Name = "TEST", Role = PlayerRole.Defender, Condition = 35, Technique = 35, Form = 35, Talent = 4,
    };

    [Fact]
    public void Extension_AtCurrentSalary_IsAccepted()
    {
        var p = NewPlayer();
        p.MonthlySalary = Contracts.BaseSalary(p, new Random(1));
        p.ContractYears = 2;
        Assert.Equal(NegotiationResult.Accepted, Contracts.Offer(p, 4, p.MonthlySalary, insured: true, matchDay: 1, new Random(1)));
        Assert.Equal(4, p.ContractYears);
        Assert.Equal("4 Jahre(v)", p.ContractText);
    }

    [Fact]
    public void LowOffer_IsRejected_AndBlockedForTheMatchDay()
    {
        var p = NewPlayer();
        p.MonthlySalary = Contracts.Demand(p);
        Assert.Equal(NegotiationResult.Rejected, Contracts.Offer(p, 2, p.MonthlySalary / 2, false, 3, new Random(1)));
        Assert.Equal(NegotiationResult.AlreadyRejected, Contracts.Offer(p, 2, p.MonthlySalary * 2, false, 3, new Random(1)));
        Assert.Equal(NegotiationResult.Accepted, Contracts.Offer(p, 2, p.MonthlySalary, false, 4, new Random(1)));
    }

    [Fact]
    public void ZeroSalary_IsUnfair() =>
        Assert.Equal(NegotiationResult.Unfair, Contracts.Offer(NewPlayer(), 2, 0, false, 1, new Random(1)));
}
