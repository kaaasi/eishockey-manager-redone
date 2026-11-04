using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Paket „wettbewerbe“, PR 6: Bundestrainer – WM-Kader und „WM Vorbereitung“ (Freundschaftsspiele) nach em.exe,
/// dazu zwei Korrekturen an der WM (Werte Deutschlands, Mischen der Länder).
/// Befund vorher (Stand PR 5): Es gab weder Kader noch Freundschaftsspiele; Deutschland spielte jede WM mit den
/// Startwerten aus DGROUP 0x4C4E, und die Gruppen wurden jede Saison unabhängig neu gemischt.
/// </summary>
public class NationalCoachTests
{
    private static GameState Create(int seed = 3, int club = 2)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Trainer", Head = 0, ClubIndex = club, StartLeague = NewGame.DatabaseLeague(club) ?? League.Oberliga });
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    /// <summary>Zufallsquelle mit fester Zahlenfolge (Werte innerhalb der verlangten Grenzen).</summary>
    private sealed class Sequence(params int[] values) : Random
    {
        private int _next;

        public override int Next(int minValue, int maxValue)
        {
            var v = values[_next++ % values.Length];
            Assert.InRange(v, minValue, maxValue - 1);
            return v;
        }
    }

    private static void PlayToSeasonEnd(GameState game, Action<GameState>? beforeWorldCup = null)
    {
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 2000, "Saisonende nicht erreicht");
            // Wie in WorldCupTests: Der Verein des Managers soll über mehrere Saisons nicht in die Entlassung laufen.
            foreach (var manager in game.Managers)
                manager.Money = Math.Max(manager.Money, 10_000_000);
            if (beforeWorldCup is not null && game.CurrentDay == SeasonCalendar.DaysOf(DayEvents.WorldCup)[0] && game.NationalCoach is null)
                beforeWorldCup(game);
            game.Continue();
        }
    }

    // ---- Kosten, Etat, Bonus ----------------------------------------------------------------------------------

    [Theory]
    [InlineData(52, false, 100_000)]
    [InlineData(52, true, 200_000)]
    [InlineData(62, false, 15_000)]
    [InlineData(63, true, 130_000)]
    public void FriendlyCost_FromTable0x26C_HomeDouble(int nation, bool home, long expected) =>
        Assert.Equal(expected, NationalCoach.FriendlyCost(nation, home));

    [Theory]
    [InlineData(1, 1_000_000)]
    [InlineData(4, 700_000)]
    [InlineData(8, 300_000)]
    [InlineData(9, 100_000)]
    [InlineData(12, 100_000)]
    public void Budget_FromWorldCupPlace(int place, long expected) => Assert.Equal(expected, NationalCoach.BudgetFor(place));

    [Fact]
    public void Budget_FirstAppointment_Is500000() => Assert.Equal(500_000, Create().NationalBudget);

    [Theory]
    // Heimspiel gegen Russland (200.000 DM = 600.000 Drittel → 300): x = (300 + 50) / 50 = 7 → höchstens 2 + 1
    [InlineData(200_000, 50, true, 5, 0, 7, 3)]
    // Heimspiel 100.000 DM, x = (150 + 0) / 50 = 3: 2:1 → 1 − 1 + 3 = 3 → begrenzt auf 2
    [InlineData(100_000, 0, true, 2, 1, 3, 2)]
    // auswärts 15.000 DM, x = (22 + 50) / 50 = 1: 0:3 → −3 + random(1, 1) = −2 → mindestens 0
    [InlineData(15_000, 50, false, 0, 3, 1, 0)]
    // auswärts 15.000 DM, x = 22 / 50 = 0: 4:0 → 4 − 1 − (2 − 0 − 1) = 2 → höchstens 1
    [InlineData(15_000, 0, false, 4, 0, 0, 1)]
    // Heimspiel 50.000 DM, x = (75 + 25) / 50 = 2: 0:0 → −1 → 0
    [InlineData(50_000, 25, true, 0, 0, 2, 0)]
    public void FriendlyBonus_AsInOriginal(long cost, int roll, bool home, int own, int against, int x, int bonus)
    {
        var result = NationalCoach.FriendlyBonus(cost, roll, home, own, against, new Sequence(1));
        Assert.Equal((x, bonus), result);
    }

    [Fact]
    public void ApplyBonus_OneValueGetsOneLess_CappedAt99()
    {
        var p = new Player { Name = "A", Role = PlayerRole.Forward, Condition = 50, Technique = 98, Form = 60 };
        NationalCoach.ApplyBonus(p, x: 3, bonus: 2); // x > 2: erster Wert eins weniger
        Assert.Equal((51, 99, 62), (p.Condition, p.Technique, p.Form));
        var q = new Player { Name = "B", Role = PlayerRole.Defender, Condition = 40, Technique = 40, Form = 40 };
        NationalCoach.ApplyBonus(q, x: 1, bonus: 2); // x < 3: zweiter Wert eins weniger
        Assert.Equal((42, 41, 42), (q.Condition, q.Technique, q.Form));
        NationalCoach.ApplyBonus(q, x: 7, bonus: 0);
        Assert.Equal((42, 41, 42), (q.Condition, q.Technique, q.Form));
    }

    // ---- WM-Kader ---------------------------------------------------------------------------------------------

    [Fact]
    public void AutoNominate_Picks3_10_16_BestUninjuredGermans()
    {
        var game = Create();
        game.AppointNationalCoach(game.Managers[0]);
        var candidates = game.NationalCandidates();
        Assert.All(candidates, c => Assert.False(c.Player.IsForeigner));
        var injured = candidates.First(c => c.Player.Role == PlayerRole.Forward).Player;
        injured.InjuredGames = 3;

        game.AutoNominate();
        var squad = game.NationalCoach!.Squad;
        Assert.Equal(NationalCoach.MaxSquad, squad.Count);
        Assert.Equal([3, 10, 16], new[] { PlayerRole.Goalkeeper, PlayerRole.Defender, PlayerRole.Forward }
            .Select(r => squad.Count(m => m.Player.Role == r)));
        Assert.DoesNotContain(squad, m => m.Player == injured);
        foreach (var role in Enum.GetValues<PlayerRole>())
        {
            var weakestIn = squad.Where(m => m.Player.Role == role).Min(m => Sum(m.Player));
            var bestOut = candidates.Where(c => c.Player.Role == role && !game.NationalCoach.Contains(c.Player) && c.Player != injured)
                .Select(c => Sum(c.Player)).DefaultIfEmpty(0).Max();
            Assert.True(weakestIn >= bestOut, $"{role}: {weakestIn} < {bestOut}");
        }
        Assert.All(squad, m => Assert.Contains(game.Clubs[m.Club].Players, p => p == m.Player));
        static int Sum(Player p) => p.Condition + p.Technique + p.Form;
    }

    [Fact]
    public void ToggleNomination_AddRemove_FullAndInjured()
    {
        var game = Create();
        game.AppointNationalCoach(game.Managers[0]);
        var candidates = game.NationalCandidates();
        var first = candidates[0].Player;
        Assert.Equal(NominationResult.Added, game.ToggleNomination(first));
        Assert.Equal(NominationResult.Removed, game.ToggleNomination(first));
        Assert.Empty(game.NationalCoach!.Squad);

        candidates[1].Player.InjuredGames = 2;
        Assert.Equal(NominationResult.Unavailable, game.ToggleNomination(candidates[1].Player));

        game.AutoNominate();
        var outside = candidates.First(c => !game.NationalCoach.Contains(c.Player) && c.Player.InjuredGames == 0).Player;
        Assert.Equal(NominationResult.SquadFull, game.ToggleNomination(outside));
        Assert.Equal(29, game.NationalCoach.Squad.Count);
    }

    // ---- WM Vorbereitung --------------------------------------------------------------------------------------

    [Fact]
    public void PlayFriendly_BooksCost_PlaysAtOnce_AppliesBonus_LimitsTo8()
    {
        var game = Create();
        var manager = game.Managers[0];
        var before = manager.Money;
        game.AppointNationalCoach(manager);
        Assert.Equal(before, manager.Money); // Etat aufs Bundestrainer-Konto, nicht aufs Vereinskonto (Spur finanzen-2)
        Assert.Equal(500_000, game.NationalAccount);
        game.AutoNominate();
        var coach = game.NationalCoach!;
        var values = coach.Squad.Select(m => m.Player.Condition + m.Player.Technique + m.Player.Form).Sum();

        game.NationalAccount = 10_000_000;
        var book = game.Finance(manager).Book;
        Assert.Equal(FriendlyResult.OwnNation, game.PlayFriendly(NationalTeams.Germany, true));
        for (var i = 0; i < NationalCoach.MaxFriendlies; i++)
            Assert.Equal(FriendlyResult.Played, game.PlayFriendly(52 + i % 4, i % 2 == 0));
        Assert.Equal(FriendlyResult.TooMany, game.PlayFriendly(60, false));

        long cost = Enumerable.Range(0, 8).Sum(i => NationalCoach.FriendlyCost(52 + i % 4, i % 2 == 0));
        Assert.Equal(10_000_000 - cost, game.NationalAccount);
        Assert.Equal(before, manager.Money);
        Assert.Equal(0, book.Current(FinanceCategory.Freundschaftsspiele));
        Assert.Equal(8, coach.Friendlies.Count);
        Assert.All(coach.Friendlies, f => Assert.Contains(NationalTeams.Germany, new[] { f.Home, f.Away }));
        Assert.True(coach.Squad.Select(m => m.Player.Condition + m.Player.Technique + m.Player.Form).Sum() >= values);
        Assert.Contains(coach.Squad, m => m.Player.Caps == 8);
    }

    [Fact]
    public void PlayFriendly_WithoutMoney_ChangesNothing()
    {
        var game = Create();
        var manager = game.Managers[0];
        game.AppointNationalCoach(manager);
        game.NationalAccount = 199_999;
        Assert.Equal(FriendlyResult.NotEnoughMoney, game.PlayFriendly(52, true));
        Assert.Equal(199_999, game.NationalAccount);
        Assert.Empty(game.NationalCoach!.Friendlies);
        Assert.Equal(0, game.Finance(manager).Book.Current(FinanceCategory.Freundschaftsspiele));
    }

    [Fact]
    public void WorldCup_UsesCoachLineup_ForOwnNation()
    {
        var game = Create();
        game.AppointNationalCoach(game.Managers[0]);
        Assert.Equal(0, game.NationalMatchStrength(NationalTeams.Germany)); // leerer Kader
        game.AutoNominate();
        var lineup = game.NationalCoach!.Lineup();
        Assert.NotNull(lineup.Goalkeeper);
        Assert.Equal(6, lineup.Defenders.Count);
        Assert.Equal(9, lineup.Forwards.Count);
        // Spur wettbewerbe-spiel, Teil 3: Stärke 0x368B1 aus dem Kader (0x98F8 mit Manager 5) statt Mittel der 16 Plätze;
        // die Formel prüft WettbewerbeSpiel3Tests.
        Assert.True(game.NationalMatchStrength(NationalTeams.Germany) > 0);
    }

    // ---- Korrekturen an der WM ---------------------------------------------------------------------------------

    /// <summary>
    /// Regressionstest (vor dem Fix rot): Ohne Bundestrainer würfelt das Original die Werte Deutschlands am 13. April
    /// neu aus (Kondition 84–94, Technik 79–89). Vorher blieb die Technik der Abwehr bei 91 aus DGROUP 0x4C4E.
    /// </summary>
    [Fact]
    public void Germany_WithoutCoach_IsRerolledBeforeWorldCup()
    {
        for (var seed = 1; seed <= 6; seed++)
        {
            var game = Create(seed, 30);
            var germany = game.Clubs[NationalTeams.Germany];
            while (game.CurrentDay <= SeasonCalendar.ClubSeasonEnd && game.Phase == SeasonPhase.Running)
                game.Continue();
            var defense = StrengthTable.Compute(game, [germany], StrengthView.Abwehr)[0];
            var attack = StrengthTable.Compute(game, [germany], StrengthView.Sturm)[0];
            Assert.InRange(defense.Condition, 84, 94);
            Assert.InRange(attack.Condition, 84, 94);
            Assert.InRange(defense.Technique, 79, 89);
            Assert.InRange(attack.Technique, 79, 89);
        }
    }

    [Fact]
    public void Germany_RatingsBeforeFirstReroll_AreTableValues_ThenRerolled()
    {
        var game = Create(2, 30);
        Assert.Equal(NationalTeamRatings.Of(NationalTeams.Germany), game.NationalRatings(NationalTeams.Germany));
        while (game.CurrentDay <= SeasonCalendar.ClubSeasonEnd && game.Phase == SeasonPhase.Running)
            game.Continue();
        var (cd, td, ca, ta) = game.NationalRatings(NationalTeams.Germany);
        // Spur wettbewerbe-spiel, Teil 3: Die ausgewürfelten Werte sind die Vereinsfelder 0x18–0x1B des Landes (vorher:
        // Mittel der vier Werte als Stärke).
        var (defense, attack, _) = game.NationalTeamValues(NationalTeams.Germany);
        Assert.Equal((cd, td, ca, ta), (defense.Condition, defense.Technique, attack.Condition, attack.Technique));
    }

    /// <summary>[ORIGINAL] Datei 0x35EDD: 20 Tausche zweier Plätze random(52, 63), auch derselbe Platz.</summary>
    [Fact]
    public void Mix_Swaps20PairsInOrder()
    {
        var order = Enumerable.Range(52, 12).ToList();
        // Tausch (0, 11), dann 19-mal (3, 3) – ändert nichts
        WorldCup.Mix(order, new Sequence([0, 11, .. Enumerable.Repeat(3, 38)]));
        Assert.Equal([63, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 52], order);
    }

    /// <summary>Regressionstest (vor dem Fix rot): Die Tausche wirken über die Saisons fort, Gruppen = Plätze 1–6 / 7–12.</summary>
    [Fact]
    public void WorldCupGroups_FollowCumulativeOrder()
    {
        var game = Create(4, 30);
        var cup = game.WorldCup!;
        var order1 = cup.Order.ToList();
        Assert.Equal(order1.Take(6), cup.Groups[0].Teams);
        Assert.Equal(order1.Skip(6), cup.Groups[1].Teams);
        PlayToSeasonEnd(game);
        game.StartNextSeason();
        var order2 = cup.Order.ToList();
        Assert.Equal(order2.Take(6), game.WorldCup!.Groups[0].Teams);
        Assert.NotEqual(order1, order2);
    }

    // ---- Mehrsaisonprobe ---------------------------------------------------------------------------------------

    /// <summary>
    /// Vier Saisonwechsel, je ein Spiel mit aktivem (Kader, 8 Freundschaftsspiele) und passivem Bundestrainer
    /// (ernannt, sonst nichts): Etat nach WM-Platz, Kosten im Finanzbuch „Freundschaftsspiele“, Amt und Kader enden
    /// mit der Saison.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FourSeasons_ActiveAndPassiveCoach(bool active)
    {
        var game = Create(7, 30);
        var manager = game.Managers[0];
        long expectedBudget = NationalCoach.FirstBudget;
        for (var season = 0; season < 5; season++)
        {
            var budget = 0L;
            var friendlies = 0L;
            PlayToSeasonEnd(game, g =>
            {
                Assert.Equal(expectedBudget, g.NationalBudget);
                var money = g.NationalAccount;
                g.AppointNationalCoach(manager);
                budget = g.NationalAccount - money;
                if (!active)
                    return;
                g.AutoNominate();
                var before = g.NationalAccount;
                // So viele Auswärtsspiele, wie der Etat hergibt (Spur finanzen-2: Kosten vom Bundestrainer-Konto).
                for (var i = 0; i < NationalCoach.MaxFriendlies; i++)
                {
                    var result = g.PlayFriendly(52 + (i + season) % 4, false);
                    if (result == FriendlyResult.NotEnoughMoney)
                        break;
                    Assert.Equal(FriendlyResult.Played, result);
                }
                friendlies = before - g.NationalAccount;
            });
            Assert.Equal(expectedBudget, budget);
            var book = game.Finance(manager).Book;
            Assert.Equal(0, book.Current(FinanceCategory.Freundschaftsspiele));
            Assert.True(friendlies >= 0 && game.NationalAccount >= 0);
            Assert.True(!active || game.NationalCoach!.Friendlies.Count >= 1);
            Assert.Equal(active, game.NationalCoach!.Friendlies.Count > 0);

            var place = game.WorldCup!.Ranking.ToList().IndexOf(NationalTeams.Germany) + 1;
            expectedBudget = NationalCoach.BudgetFor(place);
            Assert.Equal(expectedBudget, game.NationalBudget);
            if (season == 4)
                break;
            game.StartNextSeason();
            Assert.Null(game.NationalCoach);
            Assert.Equal(0, game.Finance(manager).Book.Current(FinanceCategory.Freundschaftsspiele));
        }
    }
}
