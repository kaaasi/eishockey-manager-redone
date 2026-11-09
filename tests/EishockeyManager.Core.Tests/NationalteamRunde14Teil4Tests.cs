using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur nationalteam, Runde 14, Teil 4 (#664, R14NT-08): WM-Schleife des Originals (Datei 0x55570–0x55740) für alle Spielarten
/// mit Vereinssaison – auch ohne Bundestrainer (Modus 2, Modus 4 ohne Manager unter den ersten fünf).
/// </summary>
/// <remarks>
/// [ORIGINAL] Ein Tag der Schleife (selbst nachgelesen mit <c>tools/reverse-engineering/disasm.py</c>):
/// <list type="number">
/// <item>Datei 0x55587–0x555D6: nur mit Bundestrainer und Tagesbyte ≠ 0 sein Menü und 0x952C(5, 0).</item>
/// <item>Datei 0x555DD–0x555F3: Modus ≠ 2 und Tagesbyte ≠ 0 → WM-Spiele 0x30E7B(1). 0x36B5D setzt dabei DGROUP 0xA70E := 0
///   (Datei 0x36B7A).</item>
/// <item>Datei 0x5561A–0x5565C: Tageszähler ≡ 0 oder 4 (mod 7) → Tagesablauf 0x07169 für Manager DGROUP 0xA70E – an einem
///   WM-Spieltag also Manager 0, sonst 5 (Datei 0x55581/0x555BC), und 0x07169 endet bei 0xA70E ≥ Zahl der Manager sofort
///   (Datei 0x07172–0x0717B → 0x07C0C). Im WM-Zeitraum (Tage 216–230) trifft das nur die Halbfinal- bzw. 5. Gruppentage 224
///   und 228.</item>
/// <item>Datei 0x55670–0x55721: nächster Tag, je Manager 0x3ED01 (Tagesabschluss), 0x3F833 (Werbebewertung), am Tag ≡ 0
///   (mod 7) 0x3D316 (Stadionbau), je Kaderplatz 0x07C12 (Heilung). Keine Wanderung 0x9D1B, keine Talentsuche 0x06FA5,
///   kein Listenwechsel 0x494EF (Datei 0x54E71 nur im Hauptweg).</item>
/// </list>
/// </remarks>
public class NationalteamRunde14Teil4Tests
{
    /// <summary>Zustand am ersten Tag nach der Vereinssaison (14. April), verglichen mit dem Saisonende.</summary>
    private sealed record Window(long[] Training, long[] TrainingCost, Player[] Listings, string TeamValues)
    {
        /// <summary>Stand nehmen; vorher eine Einheit „Special“ für alle Manager, damit jeder Trainingstag Geld kostet (Zähler).</summary>
        public static Window Take(GameState game)
        {
            foreach (var m in game.Managers)
                PaidTraining(m);
            return Snapshot(game);
        }

        private static Window Snapshot(GameState game) => new(
            [.. game.Managers.Select(m => game.Finance(m).Book.Current(FinanceCategory.Training))],
            [.. game.Managers.Select(m => game.PrepareTraining(m).CostPerTrainingDay)],
            [.. game.TransferMarket.Listings.Select(l => l.Player)],
            LeagueTeamValues(game));

        public long[] TrainingDelta(GameState game) =>
            [.. game.Managers.Select((m, i) => game.Finance(m).Book.Current(FinanceCategory.Training) - Training[i])];
    }

    /// <summary>Plan mit einer Einheit „Special“ (Art 0 kostet, Datei 46E:0C89); sonst nur freie Arten.</summary>
    private static void PaidTraining(Manager manager)
    {
        manager.Training.Fill(TrainingKind.GoalDrill);
        manager.Training[0, 0] = TrainingKind.Special;
    }

    /// <summary>Teamwerte aller Ligavereine (die Wanderung 0x9D1B würfelt deren Form 0x1C/0x1D neu).</summary>
    private static string LeagueTeamValues(GameState game) =>
        string.Join(";", game.Clubs.Where(c => c.League is not null).Select(c => c.TeamValues?.ToString() ?? "-"));

    /// <summary>Erster Verein der 2. Liga (Manager dort kommt nie unter die ersten fünf der 1. Liga).</summary>
    private static int SecondLeagueClub(int skip = 0) =>
        Enumerable.Range(0, 200).Where(i => NewGame.DatabaseLeague(i) == League.ZweiteLiga).Skip(skip).First();

    private static void TopUp(GameState game)
    {
        foreach (var m in game.Managers)
            m.Money = Math.Max(m.Money, 10_000_000);
    }

    private static void PlayToSeasonOver(GameState game, Action<GameState>? eachTurn = null)
    {
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 3000, "Saisonende nicht erreicht");
            TopUp(game);
            eachTurn?.Invoke(game);
            game.Continue();
        }
        Assert.Equal(SeasonPhase.SeasonOver, game.Phase);
    }

    /// <summary>
    /// #664 (1), Modus 2 („nur Fahne“): Regressionstest (vorher rot: Training an den Trainingstagen, Listenwechsel und
    /// Wanderung bis zum 29. April). [ORIGINAL] Datei 0x553FF → 0x554CF: Ohne Ernennung (Modus 2, Datei 0x55436) würfelt das
    /// Original die Werte Deutschlands und läuft dann ebenfalls in die Schleife 0x55570. Ohne WM-Spiele bleibt DGROUP 0xA70E = 5,
    /// 0x07169 tut also nichts: kein Training, kein Markt, keine Aufstellungsautomatik; keine Wanderung.
    /// </summary>
    [Theory]
    [InlineData(3)]
    [InlineData(11)]
    public void ClubOnly_WorldCupPeriod_NoTrainingNoMarketNoDrift(int seed)
    {
        var game = GameModeTests.Create(seed, true, false, 2);
        Assert.Equal(GameMode.ClubOnly, game.Mode);
        NationalCoachMode4Tests.PlayToClubSeasonEnd(game);
        Assert.Null(game.NationalCoach);
        var injured = game.Clubs[2].Players[0];
        injured.InjuredGames = 3; // Heilung 0x07C12 läuft weiter: Tage 217, 224, 231 (≡ 0 mod 7)
        var before = Window.Take(game);

        PlayToSeasonOver(game);

        Assert.All(before.TrainingDelta(game), d => Assert.Equal(0, d));
        Assert.Equal(before.Listings, game.TransferMarket.Listings.Select(l => l.Player));
        Assert.Empty(game.TransferMarket.Bids);
        Assert.Equal(before.TeamValues, LeagueTeamValues(game));
        Assert.Equal(0, injured.InjuredGames);
    }

    /// <summary>
    /// #664 (1), Modus 4 ohne Bundestrainer (zwei Manager in der 2. Liga): Regressionstest (vorher rot: beide trainierten an den
    /// Trainingstagen, die Wanderung lief). [ORIGINAL] Ohne Manager unter den ersten fünf (Datei 0x55440–0x554BF) keine Ernennung,
    /// dieselbe Schleife; an den WM-Spieltagen 224 und 228 (≡ 0/4 mod 7) läuft nach den Spielen 0x07169 für Manager 0
    /// (DGROUP 0xA70E = 0 aus 0x36B5D), also zweimal sein Training und keins für Manager 1.
    /// </summary>
    [Theory]
    [InlineData(3)]
    [InlineData(11)]
    public void Mode4_NoCoach_TrainingOnlyManagerZeroAfterWorldCupDriftDays(int seed)
    {
        var game = GameModeTests.Create(seed, true, true, SecondLeagueClub(), SecondLeagueClub(1));
        Assert.Equal(GameMode.Full, game.Mode);
        TopUp(game);
        NationalCoachMode4Tests.PlayToClubSeasonEnd(game);
        Assert.Null(game.NationalCoach);
        var before = Window.Take(game);
        Assert.True(before.TrainingCost[0] > 0);

        PlayToSeasonOver(game);

        Assert.Equal([-2 * before.TrainingCost[0], 0], before.TrainingDelta(game));
        Assert.Equal(before.TeamValues, LeagueTeamValues(game));
        Assert.Equal(GameState.FinalWorldCupStage, game.WorldCupStage);
        Assert.NotNull(game.WorldCup!.Winner);
    }

    /// <summary>
    /// #664 (2), Modus 4 mit Bundestrainer (Manager 0 ernannt): Regressionstest (vorher rot: Training an den Trainingstagen des
    /// Remakes). [ORIGINAL] Wie ohne Bundestrainer: 0x07169 nur nach den WM-Spielen der Tage 224 und 228 (Datei 0x5565C).
    /// [APPROX] Manager 0 auch dann, wenn die letzte Strafe des Spieltags Deutschland traf (0x35818 schreibt dann 0xA70E :=
    /// Feld +0x16 − 1 = 5, Datei 0x35AB8–0x35AC6; 0x07169 täte nichts).
    /// </summary>
    [Theory]
    [InlineData(3)]
    [InlineData(11)]
    public void Mode4_Coach_TrainingOnlyAfterWorldCupDriftDays(int seed)
    {
        var game = GameModeTests.Create(seed, true, true, 2);
        ManagedSquads.PlayAtTopStrength(game, 0);
        NationalCoachMode4Tests.PlayToClubSeasonEnd(game);
        Assert.Equal(0, game.NationalCoach?.Manager);
        var before = Window.Take(game);

        PlayToSeasonOver(game, g =>
        {
            if (g.NationalTurn && g.NationalCoach!.Squad.Count == 0)
                g.AutoNominate();
        });

        Assert.Equal([-2 * before.TrainingCost[0]], before.TrainingDelta(game));
        Assert.Equal(before.TeamValues, LeagueTeamValues(game));
        Assert.Equal(GameState.FinalWorldCupStage, game.WorldCupStage);
    }

    /// <summary>
    /// Mehrsaisonprobe zu #664 (AGENTS.md §5: 4 Saisonwechsel, passiver und aktiver Manager), Modus 4 mit zwei Managern: Manager
    /// 0 bei Verein 2 (aktiv mit Höchstwerten, dann meist Bundestrainer), Manager 1 in der 2. Liga (nie Bundestrainer). In jeder
    /// Saison: WM vollständig, im WM-Zeitraum genau zwei Trainings von Manager 0 und keins von Manager 1, keine Wanderung der
    /// Ligavereine; Speichern/Laden stabil.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Mode4_FourSeasonChanges_WorldCupLoopWithAndWithoutCoach(bool active)
    {
        var game = GameModeTests.Create(5, true, true, 2, SecondLeagueClub());
        var coachSeasons = 0;
        for (var season = 0; season < 5; season++)
        {
            for (var guard = 0; game.CurrentDay <= SeasonCalendar.ClubSeasonEnd; guard++)
            {
                Assert.True(guard < 3000, "13. April nicht erreicht");
                TopUp(game);
                if (active)
                {
                    TestSeason.KeepSquad(game, game.Managers[0], extendExpired: true);
                    ManagedSquads.PlayAtTopStrength(game, 0);
                }
                game.Continue();
            }
            Assert.NotEqual(1, game.NationalCoach?.Manager);
            coachSeasons += game.NationalCoach is null ? 0 : 1;
            var before = Window.Take(game);
            PlayToSeasonOver(game, g =>
            {
                if (active && g.NationalTurn && g.NationalCoach!.Squad.Count == 0)
                    g.AutoNominate();
            });

            Assert.Equal([-2 * before.TrainingCost[0], 0], before.TrainingDelta(game));
            Assert.Equal(before.TeamValues, LeagueTeamValues(game));
            Assert.Equal(GameState.FinalWorldCupStage, game.WorldCupStage);
            var saved = SaveGame.Save(game);
            Assert.Equal(saved, SaveGame.Save(SaveGame.Load(saved).Game!));
            if (season < 4)
                game.StartNextSeason();
        }
        if (active)
            Assert.True(coachSeasons > 0, "aktiv: nie Bundestrainer");
    }

    /// <summary>
    /// „Nur Weltkugel“ über eine WM, Automatik an und aus (Regressionsschutz: der Modus hat keine Vereinssaison und bleibt
    /// außerhalb der Schleife des Remakes für die Vereinssaison). Mit Automatik stellt jedes Menü des Bundestrainers neu auf,
    /// ohne (Aufstellung im ersten Zug geleert) bleibt sie über alle Termine leer; die WM endet mit Stufe 7, kein Training.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WorldOnly_OneWorldCup_AutoOnOff(bool auto)
    {
        var game = GameModeTests.Create(7, false, true, 2);
        Assert.Equal(GameMode.WorldOnly, game.Mode);
        PaidTraining(game.Managers[0]);
        var training = game.Finance(game.Managers[0]).Book.Current(FinanceCategory.Training);
        var turns = 0;
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 100, "Saisonende nicht erreicht");
            if (game.NationalCoachMenu && game.NationalCoach is { } coach)
            {
                game.OnMainMenu(game.Managers[0], isReturn: false);
                if (turns++ == 0)
                {
                    game.AutoNominate();
                    game.OnMainMenu(game.Managers[0], isReturn: true);
                    if (!auto)
                        coach.ClearFormation();
                }
                Assert.Equal(auto, coach.AutoLineupEnabled);
                Assert.Equal(auto, coach.Formation.Goalkeeper is not null);
            }
            game.Continue();
        }
        Assert.Equal(SeasonPhase.SeasonOver, game.Phase);
        Assert.True(turns >= SeasonCalendar.DaysOf(DayEvents.WorldCup).Count());
        Assert.Equal(GameState.FinalWorldCupStage, game.WorldCupStage);
        Assert.Equal(training, game.Finance(game.Managers[0]).Book.Current(FinanceCategory.Training));
    }
}
