using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Integration Runde 8, Welle 3 (Leitstelle): Anschlüsse, die laut Plan die Leitstelle setzt.
/// </summary>
public class IntegrationRunde8Welle3Tests
{
    /// <summary>
    /// #193: Nach einem Freundschaftsspiel schreibt das Original die Teamwerte des Nationalteams aus der Aufstellung des
    /// Bundestrainers (0x952C mit Manager 5, Aufruf 0x555D6). Die Länderwerte Deutschlands folgen danach der Aufstellung.
    /// </summary>
    [Fact]
    public void Friendly_SetsGermanyRatingsFromCoachLineup()
    {
        var game = GameModeTests.Create(7, false, true, 2);
        game.Continue();
        game.AutoNominate();
        var coach = game.NationalCoach!;
        Assert.Equal(NationalTeams.Germany, coach.Nation);
        FillFormation(coach);
        var before = game.NationalRatings(NationalTeams.Germany);
        Assert.Equal(FriendlyResult.Played, game.PlayFriendly(52, home: true));

        var (defense, attack, _) = MatchEngine.LineupTeamValues(coach.Formation);
        Assert.NotEqual(before, game.NationalRatings(NationalTeams.Germany));
        Assert.Equal((defense.Condition, defense.Technique, attack.Condition, attack.Technique),
            game.NationalRatings(NationalTeams.Germany));
    }

    /// <summary>[APPROX] Ohne eigene Aufstellung (Automatik) bleiben die Länderwerte stehen.</summary>
    [Fact]
    public void Friendly_WithoutFormation_KeepsGermanyRatings()
    {
        var game = GameModeTests.Create(7, false, true, 2);
        game.Continue();
        game.AutoNominate();
        Assert.Equal(0, game.NationalCoach!.Formation.Count);
        var before = game.NationalRatings(NationalTeams.Germany);
        Assert.Equal(FriendlyResult.Played, game.PlayFriendly(52, home: true));
        Assert.Equal(before, game.NationalRatings(NationalTeams.Germany));
    }

    private static void FillFormation(NationalCoach coach)
    {
        var keepers = coach.Rows.Where(p => p.Role == PlayerRole.Goalkeeper && p.Available).ToList();
        var defenders = coach.Rows.Where(p => p.Role == PlayerRole.Defender && p.Available).ToList();
        var forwards = coach.Rows.Where(p => p.Role == PlayerRole.Forward && p.Available).ToList();
        Assert.Equal(LineupResult.Assigned, coach.Assign(LineupSlot.Goalkeeper, keepers[0]));
        for (var b = 0; b < 3; b++)
        {
            for (var i = 0; i < 2 && b * 2 + i < defenders.Count; i++)
                Assert.Equal(LineupResult.Assigned, coach.Assign(LineupSlot.Defender(b, i), defenders[b * 2 + i]));
            for (var i = 0; i < 3 && b * 3 + i < forwards.Count; i++)
                Assert.Equal(LineupResult.Assigned, coach.Assign(LineupSlot.Forward(b, i), forwards[b * 3 + i]));
        }
    }

    /// <summary>
    /// R8W3I-01 (#269): [ORIGINAL] 0x37E2D/0x37E62 lädt den Spielstand einmal je Minute in lokale Variablen; 0x80CB
    /// (0x38011) und 0x939A (0x3805A/0x3806E) bekommen den Stand vor den Toren dieser Minute. Gegenbeispiel: 1:2 vor
    /// Minute 59, der Heimverein gleicht in der Minute aus – das Original nimmt den Torhüter trotzdem heraus
    /// (own + 1 == other mit dem alten Stand). Gesucht wird ein Seed mit Ausgleich in der Minute; bei ihm muss der
    /// Heimverein, wenn er die Seite x ist, den Torhüter herausnehmen.
    /// </summary>
    [Fact]
    public void ManagerMatch_ComputerModes_UseScoreBeforeMinute()
    {
        var pulled = 0;
        var equalized = 0;
        for (var seed = 0; seed < 4000 && pulled == 0; seed++)
        {
            var home = new LiveTeam { Defense = new(99, 99, 99), Attack = new(99, 99, 99), Energy = 90, Effort = 60 };
            var away = new LiveTeam { Defense = new(1, 1, 1), Attack = new(1, 1, 1), Energy = 90, Effort = 60 };
            var report = new MatchReport { HomeGoals = 1, AwayGoals = 2 };
            OriginalMinutes.PlayMinute(home, away, new MatchRules(ManagerMatch: true), 59, 60, report, new Random(seed));
            if (report.HomeGoals != 2 || report.AwayGoals != 2)
                continue;
            equalized++;
            if (home.GoaliePulled)
                pulled++;
        }
        Assert.True(equalized > 0, "kein Ausgleich in Minute 59 gefunden");
        Assert.True(pulled > 0, $"bei {equalized} Ausgleichen nie den Torhüter herausgenommen (Stand nach den Toren gelesen)");
    }

    /// <summary>
    /// R8W3I-02 (#270): [ORIGINAL] Nach dem Saisonende schreibt 0x4A9A1 Forderung und Laufzeit in den Kaderplatz, bevor
    /// 0x4A9B3 vergleicht – die Untergrenze „bisheriges Gehalt“ wirkt dann nicht (Vergleich mit sich selbst).
    /// </summary>
    [Fact]
    public void Demand_AfterSeason_HasNoFloor()
    {
        Assert.Equal(14_400, ContractRules.DemandThirds(9_000, 100, 100, 2, true, 27, 20_000));
        Assert.Equal(20_000, ContractRules.DemandThirds(9_000, 100, 100, 2, false, 27, 20_000));
    }

    /// <summary>
    /// R8W3I-02: Nach dem Saisonende gilt die eingegebene Laufzeit als bisherige (0x4A9A1 schreibt +0x0B), eine kürzere
    /// Laufzeit ist also nicht „kürzer“ (0x4AC75); ein Gehalt unter der Forderung lehnt 0x49A3B ohne Zufallszug ab
    /// (gleiche Laufzeit, weniger Gehalt). Vorher: <see cref="NegotiationResult.TooShort"/> bzw. Wurf über Accepts.
    /// </summary>
    [Fact]
    public void Negotiate_AfterSeason_UsesWrittenDemandAndYears()
    {
        var game = TestGames.Create(3);
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        LifecycleRegressionTests.FinishSeason(game);
        Assert.True(game.AfterSeasonEnd);
        var players = club.Players.Where(p => p.Role == PlayerRole.Forward && Contracts.Availability(game, p) is null).Take(2).ToList();
        Assert.Equal(2, players.Count);

        var p = players[0];
        p.ContractYears = 3;
        var demand = Contracts.DemandAfterYears(game, manager, p, 2);
        Assert.NotNull(demand);
        Assert.Equal(NegotiationResult.Accepted, Contracts.Negotiate(game, manager, p, 2, demand!.Value, false, demandFilled: false));
        Assert.Equal(2, p.ContractYears);
        Assert.Equal(demand.Value, p.MonthlySalary);

        var q = players[1];
        q.ContractYears = 3;
        var demandQ = Contracts.DemandAfterYears(game, manager, q, 2)!.Value;
        var twin = GameRandom.FromState(game.ContractRandom.Capture());
        Assert.Equal(NegotiationResult.NotInterested, Contracts.Negotiate(game, manager, q, 2, demandQ - 1, false, demandFilled: false));
        Assert.Equal(SquadSetup.Between(twin, 10, 18), q.NegotiationLock); // nur der Zug der Sperre (0x4AD3E)
    }
}
