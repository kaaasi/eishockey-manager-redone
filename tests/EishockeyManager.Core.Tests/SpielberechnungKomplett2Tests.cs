using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur spielberechnung-komplett, Teil 2: Strafen in der Managerpaarung (R8W3I-03 bis -06) und die Testlücke
/// <c>Forfeit_LeagueMatch_UpdatesDayForm</c> (Teil von R8W3I-10). Fundstellen: <c>docs/spielberechnung.md</c> §13, §16.
/// </summary>
public class SpielberechnungKomplett2Tests
{
    /// <summary>Zählt die Zufallszüge (alle Züge der Spielberechnung laufen über <c>Next(min, max)</c>).</summary>
    private sealed class CountingRandom(int seed) : Random(seed)
    {
        public int Calls { get; private set; }

        public override int Next(int minValue, int maxValue)
        {
            Calls++;
            return base.Next(minValue, maxValue);
        }
    }

    private static Player P(PlayerRole role, int number) =>
        new() { Name = $"P{number}", Role = role, Number = number, Condition = 60, Technique = 50, Form = 50, Energy = 100 };

    /// <summary>Sieben Kaderplätze, Reihe 0 aufgestellt; <paramref name="dressed"/> = eingesetzt.</summary>
    private static LiveSquad Squad(bool dressed)
    {
        var s = new LiveSquad();
        int[] positions = [1, 1, 1, 0x81, 0x81, 0x81, 0x82];
        PlayerRole[] roles = [PlayerRole.Goalkeeper, PlayerRole.Defender, PlayerRole.Defender, PlayerRole.Forward,
            PlayerRole.Forward, PlayerRole.Forward, PlayerRole.Forward];
        for (var i = 0; i < positions.Length; i++)
            s.Players.Add(new LivePlayer { Player = P(roles[i], i + 1), Position = positions[i], Dressed = dressed, Energy = 90 });
        return s;
    }

    /// <summary>
    /// R8W3I-03: [ORIGINAL] 0x378C5 prüft den Zähler vor dem Zug (0x378ED → 0x37A2A, `cmp al,0x96; jnc` an 0x37A30) und
    /// zieht höchstens 150-mal. Vorher: 151 Züge, wenn kein Spieler passt.
    /// </summary>
    [Fact]
    public void PenalizedPlayer_AtMost150Draws()
    {
        var team = new LiveTeam { Squad = Squad(dressed: false), Managed = true };
        var random = new CountingRandom(1);
        Assert.Null(OriginalMinutes.PenalizedPlayer(team, random));
        Assert.Equal(150, random.Calls);
        Assert.Equal(150, OriginalMinutes.MaxPenaltyDraws);
    }

    /// <summary>
    /// R8W3I-04: [ORIGINAL] Ohne passenden Spieler (Rückgabe 100) springt 0x35B33 nach 0x3583B: keine Anzeige, keine
    /// Kettenstrafe. Strafbank und Strafminuten des Vereins sind schon gesetzt. Vorher: ein Strafereignis ohne Spieler.
    /// </summary>
    [Fact]
    public void Penalty_WithoutPlayer_NoEvent_ButBoxAndMinutes()
    {
        var found = false;
        for (var seed = 0; seed < 400 && !found; seed++)
        {
            var club = new LiveTeam { Squad = Squad(dressed: false), Managed = true, Effort = 100 };
            var opponent = new LiveTeam { Effort = 100 };
            var events = new List<MatchEvent>();
            OriginalMinutes.Penalty(club, opponent, 0, 10, chained: false, events, new Random(seed));
            if (club.PenaltyMinutes == 0)
                continue;
            found = true;
            Assert.Empty(events);
            Assert.Equal(0, opponent.PenaltyMinutes); // keine Kettenstrafe
            Assert.True(club.Box.Skaters < 5);
        }
        Assert.True(found);
    }

    /// <summary>
    /// R8W3I-05: [ORIGINAL] Strafen lesen den gespeicherten Managerwert 0x974B (Vereinsfeld 0x16 = Manager + 1, also des
    /// nächsten Managers); 0x952C setzt ihn bei weniger als neun aufgestellten und eingesetzten Spielern auf 100
    /// (0x9698–0x96A8). Vorher: der Zweig war toter Code, der Wert blieb 20 … 40.
    /// </summary>
    [Fact]
    public void PenaltyManagerValue_NextManagerShortHanded_Is100()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Erster", Head = 0, ClubIndex = 2 });
        setup.Managers.Add(new ManagerSetup { Name = "Zweiter", Head = 1, ClubIndex = 5 });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 3);
        MatchEngine.EnsureTeamValues(game);
        var club = game.Clubs[game.Managers[0].ClubIndex];
        var next = game.Clubs[game.Managers[1].ClubIndex];
        var opponent = game.Clubs[7];

        var normal = MatchEngine.LiveTeamOf(game, club, opponent, managed: true, managerMatch: true).PenaltyManagerValue;
        Assert.InRange(normal, 0, 40);

        foreach (var p in next.Lineup.Players.Skip(8).ToList())
            next.Lineup.Remove(p);
        Assert.True(game.IsShortHanded(next));
        Assert.Equal(100, MatchEngine.LiveTeamOf(game, club, opponent, managed: true, managerMatch: true).PenaltyManagerValue);
    }

    /// <summary>
    /// R8W3I-06: [ORIGINAL] 0x35AB8–0x35B2C ruft 0x378C5 für jeden Verein mit Vereinsfeld 0x16 ≠ 0, auch für das Nationalteam
    /// des Bundestrainers (Manager 5). Vorher: ohne Kader keine Spielerwahl, die Gutschrift zog einen Zufallsspieler.
    /// </summary>
    [Fact]
    public void NationalTeam_PenaltiesPickPlayerFromCoachSquad()
    {
        var game = TestGames.Create(3);
        game.AppointNationalCoach(game.Managers[0]);
        game.AutoNominate();
        var coach = game.NationalCoach!;
        Assert.True(coach.Squad.Count >= 8);
        var nation = new Club { Index = coach.Nation, Name = "Land", Strength = 60 };
        var other = new Club { Index = coach.Nation == NationalTeams.First ? NationalTeams.First + 1 : NationalTeams.First, Name = "Gegner", Strength = 60 };

        var team = MatchEngine.LiveTeamOf(game, nation, other, managed: true, managerMatch: true);
        // Spur wettbewerbe-spiel, Teil 3 (A079): [ORIGINAL] das Land spielt wie ein Managerverein mit dem Kader (0x98F8 mit
        // Manager 5); der Strafspieler kommt aus demselben Kader (PenaltyPlayers = Squad).
        var squad = Assert.IsType<LiveSquad>(team.Squad);
        Assert.Same(squad, team.PenaltyPlayers);
        Assert.Equal(coach.Squad.Count, squad.Players.Count);
        var lineup = coach.Lineup().Players.ToHashSet();
        Assert.All(squad.Players.Where(p => p.Position != 0), p => Assert.Contains(p.Player, lineup));

        var picked = 0;
        for (var seed = 0; seed < 300; seed++)
        {
            var t = MatchEngine.LiveTeamOf(game, nation, other, managed: true, managerMatch: true);
            var events = new List<MatchEvent>();
            OriginalMinutes.Penalty(t, new LiveTeam(), 0, 10, chained: true, events, new Random(seed));
            foreach (var e in events.Where(e => e.Player is not null))
            {
                picked++;
                Assert.Contains(e.Player!, lineup);
                Assert.NotEqual(PlayerRole.Goalkeeper, e.Player!.Role);
            }
        }
        Assert.True(picked > 0);
    }

    /// <summary>
    /// Testlücke aus R8W3I-10: [ORIGINAL] 0x4F527 führt nach dem Ligaspiel auch die Tagesform des Verlierers nach (47 … 53).
    /// <c>Forfeit_LeagueMatch_UpdatesDayForm</c> prüfte nur den Sieger; ein unveränderter Wert 50 beim Verlierer blieb grün.
    /// Gegenprobe: ohne Nachführung des Verlierers (UpdateDayForm nur für den Sieger) rot.
    /// </summary>
    [Fact]
    public void Forfeit_LeagueMatch_LoserDayFormDrops()
    {
        var losers = new List<int>();
        for (var seed = 1; seed <= 8; seed++)
        {
            var game = TestGames.Create(seed);
            MatchEngine.EnsureTeamValues(game);
            var club = game.Clubs[game.Managers[0].ClubIndex];
            foreach (var p in club.Lineup.Players.Skip(8).ToList())
                club.Lineup.Remove(p);
            foreach (var c in game.Clubs)
                c.TeamValues = c.TeamValues!.Value with { DayForm = 50 };
            TestSeason.PlayMatchDay(game);
            var result = game.Results.Single(r => r.Involves(club.Index));
            Assert.True(result.Home == club.Index ? result.HomeGoals < result.AwayGoals : result.AwayGoals < result.HomeGoals);
            losers.Add(club.TeamValues!.Value.DayForm);
        }
        Assert.All(losers, f => Assert.InRange(f, 47, 50));
        Assert.Contains(losers, f => f < 50);
    }
}
