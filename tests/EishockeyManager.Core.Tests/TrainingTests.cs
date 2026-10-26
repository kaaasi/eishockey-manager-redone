using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

public class TrainingTests
{
    private static Player NewPlayer(PlayerRole role = PlayerRole.Forward, int age = 30, int energy = 80) =>
        new() { Name = "TEST", Role = role, Condition = 50, Technique = 50, Form = 50, Age = age, Energy = energy };

    private static TrainingPlan FreePlan()
    {
        var plan = new TrainingPlan();
        plan.Fill(TrainingKind.Free);
        return plan;
    }

    [Fact]
    public void WeeklyCost_MatchesVideo()
    {
        // UI-Video 11:30 (BERLINER SC, Oberliga): eine Einheit „Special“ → 12.400 DM, zwei → 18.800 DM
        var plan = FreePlan();
        plan.PriceTier = TrainingPlan.TierOf(League.Oberliga);
        Assert.Equal(0, plan.WeeklyCost);
        plan[2, 2] = TrainingKind.Special;
        Assert.Equal(12_400, plan.WeeklyCost);
        plan[6, 2] = TrainingKind.Special;
        Assert.Equal(18_800, plan.WeeklyCost);
        Assert.Equal(9_400, plan.CostPerTrainingDay);
    }

    [Fact]
    public void WeeklyCost_IsHigherInTheFirstLeague()
    {
        var plan = FreePlan();
        plan[0, 0] = TrainingKind.Special;
        plan.PriceTier = 0;
        Assert.Equal(2 * (12_000 + 21_000) / 3, plan.WeeklyCost);
    }

    [Fact]
    public void Prepare_FillsTheDefaultPlanOnce()
    {
        var plan = new TrainingPlan();
        plan.Prepare(level: 3);
        Assert.Equal(TrainingKind.GoalDrill, plan[0, 0]);
        Assert.Equal(TrainingKind.Combinations, plan[0, 1]); // Hantel die ersten beiden Male übersprungen
        Assert.Equal(TrainingKind.Weights, plan[3, 1]);
        foreach (var (day, unit) in new[] { (2, 1), (2, 2), (2, 3), (6, 1), (6, 2), (6, 3), (5, 2), (5, 3) })
            Assert.Equal(TrainingKind.Free, plan[day, unit]);
        Assert.Equal(0, plan.Count(TrainingKind.Special));

        plan[0, 0] = TrainingKind.Skating;
        plan.Prepare(level: 3);
        Assert.Equal(TrainingKind.Skating, plan[0, 0]);
    }

    [Fact]
    public void Prepare_AtLevel5_FreesEverything()
    {
        var plan = new TrainingPlan();
        plan.Prepare(level: 3);
        plan.Prepare(level: 5);
        Assert.Equal(TrainingPlan.Days * TrainingPlan.UnitsPerDay, plan.Count(TrainingKind.Free));
    }

    [Theory]
    [InlineData(300, 20)]
    [InlineData(250, 10)]
    [InlineData(202, 0)]
    [InlineData(100, -20)]
    [InlineData(20, -20)]
    public void Round_Energy(int value, int expected) =>
        Assert.Equal(expected, TrainingRules.Round(value, energy: true, new Random(1)));

    [Fact]
    public void Round_Values_IsPlusMinusOneWithProbability()
    {
        var random = new Random(5);
        Assert.All(Enumerable.Range(0, 200), _ => Assert.Equal(0, TrainingRules.Round(101, false, random)));
        var ups = Enumerable.Range(0, 10_000).Count(_ => TrainingRules.Round(200, false, random) == 1);
        Assert.InRange(ups, 1_800, 2_350); // 21/101 ≈ 20,8 %
        Assert.All(Enumerable.Range(0, 200), _ => Assert.True(TrainingRules.Round(0, false, random) <= 0));
    }

    [Fact]
    public void FreeWeek_RecoversEnergyAndLosesSkills()
    {
        var plan = FreePlan();
        var random = new Random(7);
        var players = Enumerable.Range(0, 400).Select(_ => NewPlayer(energy: 40)).ToList();
        var outcome = TrainingRules.Apply(players[0], plan, level: 3, random);
        Assert.Equal(20, outcome.Recovery);
        Assert.Equal(60, players[0].Energy);
        foreach (var p in players.Skip(1))
            TrainingRules.Apply(p, plan, 3, random);
        Assert.All(players, p => Assert.InRange(p.Condition, 49, 50));
        Assert.InRange(players.Count(p => p.Condition == 49), 40, 130); // ≈ 21 %
    }

    [Fact]
    public void HardTraining_CostsEnergyAndBuildsCondition()
    {
        var plan = new TrainingPlan();
        plan.Prepare(level: 3); // Standardplan
        var random = new Random(11);
        var players = Enumerable.Range(0, 400).Select(_ => NewPlayer(PlayerRole.Defender, energy: 40)).ToList();
        foreach (var p in players)
            Assert.InRange(TrainingRules.Apply(p, plan, 3, random).Recovery, -20, 5); // frei: +20
        // Der Standardplan hält die Werte (Normtabelle 0x48CD ≈ Belastung des Standardplans).
        Assert.True(players.Count(p => p.Condition == 50 && p.Technique == 50) > 300);

        // Kraftbetont, aber abwechslungsreich: Kondition steigt, Technik verfällt.
        int[][] focus = [[3, 7, 3, 2], [3, 7, 3, 4], [1, 1, 1, 1], [3, 7, 3, 5], [3, 7, 3, 6], [3, 8, 1, 1], [1, 1, 1, 1]];
        for (var day = 0; day < TrainingPlan.Days; day++)
            for (var unit = 0; unit < TrainingPlan.UnitsPerDay; unit++)
                plan[day, unit] = (TrainingKind)focus[day][unit];
        players = Enumerable.Range(0, 400).Select(_ => NewPlayer(PlayerRole.Defender)).ToList();
        foreach (var p in players)
            TrainingRules.Apply(p, plan, 3, random);
        Assert.InRange(players.Count(p => p.Condition == 51), 50, 120);
        Assert.Equal(0, players.Count(p => p.Condition < 50));
        Assert.InRange(players.Count(p => p.Technique == 49), 50, 120);

        plan.Fill(TrainingKind.Weights);
        Assert.Equal(-20, TrainingRules.Apply(NewPlayer(PlayerRole.Defender), plan, 3, random).Recovery);
    }

    [Fact]
    public void Special_RaisesTheBonus()
    {
        var plan = new TrainingPlan();
        plan.Prepare(level: 3);
        var without = TrainingRules.Apply(NewPlayer(), plan, 3, new Random(1)).Bonus;
        plan[0, 3] = TrainingKind.Special;
        var with = TrainingRules.Apply(NewPlayer(), plan, 3, new Random(1)).Bonus;
        Assert.True(with > without);
    }

    [Fact]
    public void TrainingOnMatchDayAfternoon_IsPunished()
    {
        var plan = new TrainingPlan();
        plan.Prepare(level: 3);
        var before = TrainingRules.Apply(NewPlayer(), plan, 3, new Random(1)).Bonus;
        plan[2, 3] = TrainingKind.Skating;
        var after = TrainingRules.Apply(NewPlayer(), plan, 3, new Random(1)).Bonus;
        Assert.Equal(-20, after - before);
    }

    [Fact]
    public void LowLevels_IgnoreThePlanning()
    {
        var plan = FreePlan();
        Assert.Equal(0, TrainingRules.Apply(NewPlayer(), plan, level: 1, new Random(1)).Bonus);
        Assert.NotEqual(0, TrainingRules.Apply(NewPlayer(), plan, level: 3, new Random(1)).Bonus);
    }

    [Fact]
    public void Bars_UseGroupAverages()
    {
        var players = new[] { NewPlayer(PlayerRole.Goalkeeper, energy: 100), NewPlayer(PlayerRole.Goalkeeper, energy: 0) };
        var (e, k, t) = TrainingRules.Bars(players, 0);
        Assert.Equal((150 - 50) * 36 / 150, e);
        Assert.Equal(50 * 36 / 100, k);
        Assert.Equal(50 * 36 / 100, t);
        Assert.Equal((0, 0, 0), TrainingRules.Bars(players, 2));
    }

    [Fact]
    public void CatchUp_TrainsTwiceAWeekAndPaysForSpecial()
    {
        var game = TestGames.Create();
        var manager = game.ActiveManager;
        var plan = game.PrepareTraining(manager);
        Assert.Equal(0, plan.PriceTier); // Verein 2 spielt in der 1. Liga
        plan[1, 0] = TrainingKind.Special;
        // Trainingskosten stehen im Finanzbuch (Kontostand ändert sich auch durch andere Buchungen)
        long Training() => game.Finance(manager).Book.Current(FinanceCategory.Training);

        game.CatchUpTraining();
        Assert.Equal(0, Training()); // Spielbeginn: noch kein Trainingstag

        TestSeason.PlayMatchDay(game);
        TestSeason.PlayMatchDay(game);
        game.CatchUpTraining();
        var days = Enumerable.Range(1, game.Day).Count(GameState.IsTrainingDay);
        Assert.True(days > 0);
        Assert.Equal(-days * plan.CostPerTrainingDay, Training());

        game.CatchUpTraining();
        Assert.Equal(-days * plan.CostPerTrainingDay, Training());
    }
}
