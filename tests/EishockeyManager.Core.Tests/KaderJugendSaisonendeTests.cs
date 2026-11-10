using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur kader, Teil 1 (#698): Kader des passiven Managers nach dem FILE44-Import. [ORIGINAL] Das Original ergänzt den Kader eines
/// Managers am Saisonende nur über die Jugend (Fenster „Saisonende“ 0x26F20, Datei 0x2728C–0x27320 →
/// <see cref="GameState.JoinSeasonEndYouth"/>); eine Zwangsergänzung gibt es nicht – mit weniger als neun Spielern werden die Spiele
/// gewertet („Weniger als neun Spieler!“, FILE13: Krefelder EV im DEB-Pokal). Wer am Saisonende jede Verhandlung abbricht, verliert
/// wie im Original alle Spieler mit ausgelaufenem Vertrag (0x27D05).
/// </summary>
public class KaderJugendSaisonendeTests
{
    private static GameState ImportFile44(int seed) => GameState.ImportOriginal(
        OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave("FILE44.HOK"))),
        ClubDatabase.Load(TestGameData.Load()), seed);

    /// <summary>[ORIGINAL] Datei 0x272B4–0x272C3: Bis zum Jahr 1994 (erster Saisonwechsel des Spiels) kommt niemand.</summary>
    [Fact]
    public void UpTo1994_NoYouth()
    {
        var game = TestGames.Create();
        var club = game.Clubs[game.Managers[0].ClubIndex];
        var count = club.Players.Count;
        for (var i = 0; i < 200; i++)
            Assert.Empty(game.JoinSeasonEndYouth(1994));
        Assert.Equal(count, club.Players.Count);
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x2728C–0x272F6: n = random(0, 2), mit random(0, 1) ≠ 0 n = 0 (also P(n = 0) = 2/3, Mittel 0,5); Talent nach
    /// 0x06D7F mit s = Stärke der Junioren und 60 % von Wert(Gehalt) (Datei 0x272D8–0x272DE, 0x06F27–0x06F5A).
    /// </summary>
    [Fact]
    public void From1995_ZeroToTwo_JuniorStrength_SixtyPercentSalary()
    {
        var game = TestGames.Create();
        var manager = game.Managers[0];
        var club = game.Clubs[manager.ClubIndex];
        var s = game.Finance(manager).Youth[(int)YouthTeamKind.Junioren].Strength;
        var counts = new int[SeasonEndYouth.MaxCount + 1];
        const int trials = 600;
        for (var i = 0; i < trials; i++)
        {
            var joined = game.JoinSeasonEndYouth(1995);
            counts[joined.Count]++;
            foreach (var p in joined)
            {
                Assert.Contains(p, club.Players);
                Assert.InRange(p.Age, PlayerFactory.ScoutedMinAge, PlayerFactory.ScoutedMaxAge);
                Assert.InRange(p.Technique, s - 5, s + 1);
                Assert.InRange(p.Condition, s - 5, s + 1);
                Assert.InRange(p.ContractYears, 2, 3);
                Assert.Equal(Math.Max(Contracts.MinSalary, Contracts.ValueSalary(game, manager, p) * 60 / 100), p.MonthlySalary);
                club.RemovePlayer(p);
            }
        }
        Assert.InRange(counts[0], trials * 55 / 100, trials * 78 / 100);
        Assert.True(counts[1] > 0 && counts[2] > 0);
    }

    /// <summary>[ORIGINAL] Grenze 28 eigene Spieler wie bei der Talentsuche (Datei 0x06D8D–0x06D97).</summary>
    [Fact]
    public void From1995_FullSquad_NoYouth()
    {
        var game = TestGames.Create();
        var manager = game.Managers[0];
        while (game.OwnedPlayers(manager) < TalentScout.MaxOwned)
            Assert.NotNull(game.JoinScoutedTalent(manager, 50));
        for (var i = 0; i < 100; i++)
            Assert.Empty(game.JoinSeasonEndYouth(1995));
    }

    /// <summary>
    /// Regressionstest #698 und Mehrsaisonprobe (vier Saisonwechsel ab FILE44, passiv und aktiv). Vorher rot: Der passive Kader war
    /// nach dem zweiten Wechsel leer und blieb es (0 Spieler 1995–1997), weil die Jugend am Saisonende fehlte. Passiv („Abbruch“ in
    /// jeder Verhandlung) gehen alle Spieler aus FILE44 mit Vertragsende, wie im Original; danach stehen nur Junioren im Kader.
    /// Aktiv werden alle Verträge zur Forderung verlängert; die Jugend kommt dazu. Beim ersten Wechsel (29.04.1994) kommt
    /// niemand (FILE44 → FILE46 und FILE13 → FILE14: Kader ohne Neuzugang).
    /// </summary>
    [GameDataTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void File44_FourSeasonChanges_YouthJoinsFrom1995(bool active)
    {
        // Je Lauf kommt mit Wahrscheinlichkeit (2/3)³ ≈ 0,3 niemand (drei Wechsel ab 1995); vier Saaten zusammen.
        var youth = new[] { 4711, 1, 2, 3 }.Sum(seed => FourSeasonChanges(seed, active));
        Assert.True(youth > 0, "Keine Jugend am Saisonende 1995–1997");
    }

    private static int FourSeasonChanges(int seed, bool active)
    {
        var game = ImportFile44(seed);
        var manager = game.Managers[0];
        var club = game.Clubs[manager.ClubIndex];
        var youth = new HashSet<Player>();
        for (var season = 0; season < 4; season++)
        {
            // Bekannt ist jeder Spieler außerhalb des freien Pools (auch der verliehene HOHENADL, der beim ersten Wechsel zurückkommt,
            // A115); Junioren kommen aus dem freien Pool (0x06D7F).
            var known = game.AllPlayers.Except(game.TransferMarket.PoolPlayers).ToHashSet();
            TestSeason.PlayRegularSeason(game);
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 5000, "Saisonende nicht erreicht");
                if (game.NationalTurn && game.NationalCoach!.Squad.Count == 0)
                    game.AutoNominate();
                game.Continue();
            }
            game.StartNextSeasonWithRenewals();
            foreach (var p in game.PendingRenewals(manager))
            {
                if (active)
                {
                    manager.Money = Math.Max(manager.Money, 100_000_000);
                    game.NegotiateRenewal(manager, p, 2, Contracts.DemandAfterYears(game, manager, p, 2) ?? 2 * p.MonthlySalary, false,
                        demandFilled: true);
                }
                else
                    game.CancelRenewal(manager, p);
            }
            Assert.Empty(game.PendingRenewals(manager));
            var newcomers = club.Players.Where(p => !known.Contains(p)).ToList();
            if (game.Season <= SeasonEndYouth.LastYearWithout)
                Assert.Empty(newcomers);
            foreach (var p in newcomers)
            {
                // Schon gealtert und heruntergezählt (Datei 0x274A7, 0x27500): 19–22 Jahre, Laufzeit 1–2.
                Assert.InRange(p.Age, PlayerFactory.ScoutedMinAge + 1, PlayerFactory.ScoutedMaxAge + 1);
                Assert.InRange(p.ContractYears, 1, 2);
                youth.Add(p);
            }
            Assert.All(club.Players, p => Assert.True(p.ContractYears > 0, p.Name));
        }
        Assert.Equal(1997, game.Season);
        if (!active)
            Assert.All(club.Players, p => Assert.Contains(p, youth));
        else
            Assert.Contains(club.Players, p => !youth.Contains(p));
        return youth.Count;
    }
}
