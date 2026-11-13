using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur livespiel-kern, Teil A5 (R18-E1 = A): Minutenschritt im Kern (<c>MatchEngine.LiveStep.cs</c>,
/// <c>docs/analyse/re-livespiel.md</c> §13 A5). Vor dem Teil rot: es gab keinen Minutenschritt (<c>LiveMatchDay</c> fehlte,
/// Build-Fehler).
/// </summary>
public sealed class LivespielKernLiveStepTests
{
    private const int ManagedClub = 2;

    private static GameState NewGame(int seed = 5)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "M", Head = 0, ClubIndex = ManagedClub });
        return Core.NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    /// <summary>Paarungen des ersten Ligaspieltags der Liga des Managervereins (mit der Managerpaarung).</summary>
    private static List<(int Home, int Away)> FirstDay(GameState game)
    {
        var league = game.Clubs[ManagedClub].League!.Value;
        return [.. game.Schedules[league][0].Matches];
    }

    private static string Fingerprint(MatchReport r) =>
        $"{r.HomeGoals}:{r.AwayGoals} ot={r.Overtime} pm={r.HomePenaltyMinutes}/{r.AwayPenaltyMinutes} " +
        $"so={r.ShootoutHomeWins}/{r.ShootoutGoals} f={r.Forfeit} " +
        string.Join(",", r.Events.Select(e => $"{e.Minute}/{e.Side}/{e.Kind}/{e.PenaltyMinutes}/{e.Player?.Name}"));

    /// <summary>
    /// Abnahme A5: Minutenschritt ohne Eingriff = <see cref="MatchEngine.PlayWithEvents"/> – je Paarung derselbe Bericht und
    /// danach derselbe Spielstand (Spielerzähler, Vereinsfelder, Tagesform; ganzer Spielstand verglichen), Liga und K.-o.
    /// (Verlängerung, Penaltyschießen).
    /// </summary>
    [Theory]
    [InlineData(MatchCompetition.League, false)]
    [InlineData(MatchCompetition.DebCup, true)]
    public void SinglePairing_WithoutIntervention_EqualsPlayWithEvents(MatchCompetition competition, bool knockOut)
    {
        var overtime = 0;
        for (var seed = 0; seed < 40; seed++)
        {
            var a = NewGame();
            var b = NewGame();
            var (home, away) = FirstDay(a).First(m => m.Home == ManagedClub || m.Away == ManagedClub);
            var ra = new GameRandom(seed);
            var rb = new GameRandom(seed);
            var expected = MatchEngine.PlayWithEvents(a, a.Clubs[home], a.Clubs[away], competition, knockOut, ra);
            var day = LiveMatchDay.Begin(b, [new LiveFixture(b.Clubs[home], b.Clubs[away], competition, knockOut, rb)]);
            var steps = 0;
            while (day.Step())
                steps++;
            var actual = day.Finish().Single();
            Assert.Equal(Fingerprint(expected), Fingerprint(actual));
            Assert.Equal(expected.Overtime ? 70 : 60, steps);
            Assert.Equal(ra.Capture(), rb.Capture());
            Assert.Equal(SaveGame.Save(a), SaveGame.Save(b));
            overtime += expected.Overtime ? 1 : 0;
        }
        if (knockOut)
            Assert.True(overtime > 0, "keine Verlängerung in der Probe");
    }

    /// <summary>
    /// Abnahme A5: ganzer Spieltag, je Minute alle Paarungen verschränkt (0x37A62), mit eigenem Zufall je Paarung = Paarung
    /// für Paarung über <see cref="MatchEngine.PlayWithEvents"/>; Anzeigereihe der Managerpaarung gleich.
    /// </summary>
    [Fact]
    public void WholeDay_Interleaved_EqualsSequentialPlayWithEvents()
    {
        for (var seed = 0; seed < 5; seed++)
        {
            var a = NewGame(seed);
            var b = NewGame(seed);
            var pairs = FirstDay(a);
            Assert.True(pairs.Count > 1);
            var expected = pairs.Select((p, i) => MatchEngine.PlayWithEvents(a, a.Clubs[p.Home], a.Clubs[p.Away],
                MatchCompetition.League, knockOut: false, new GameRandom(100 * seed + i))).ToList();
            var day = LiveMatchDay.Begin(b, pairs.Select((p, i) => new LiveFixture(b.Clubs[p.Home], b.Clubs[p.Away],
                MatchCompetition.League, KnockOut: false, new GameRandom(100 * seed + i))));
            var actual = day.Finish();
            Assert.Equal(expected.Select(Fingerprint), actual.Select(Fingerprint));
            Assert.Equal(SaveGame.Save(a), SaveGame.Save(b));
            var rowA = a.LiveScoreboard!.RowOf(ManagedClub)!;
            var rowB = b.LiveScoreboard!.RowOf(ManagedClub)!;
            Assert.Same(rowB, day.MatchOf(ManagedClub)!.Row);
            Assert.Equal(rowA.ChanceList, rowB.ChanceList);
            Assert.Equal(rowA.PenaltyList, rowB.PenaltyList);
            Assert.True(rowB.Finished);
        }
    }

    private static GameState NewGame(int seed, int[] managedClubs, int liveMode, int speed)
    {
        var setup = new GameSetup();
        foreach (var club in managedClubs)
            setup.Managers.Add(new ManagerSetup { Name = $"M{club}", Head = 0, ClubIndex = club });
        var game = Core.NewGame.Create(TestGames.CreateDatabase(), setup, seed);
        game.Options.LiveMode = liveMode;
        game.Options.MatchSpeed = speed;
        return game;
    }

    private static string RowFingerprint(LiveScoreboardRow r) =>
        $"{r.HomeClub}-{r.AwayClub} c=[{string.Join(",", r.ChanceList)}] p=[{string.Join(",", r.PenaltyList)}] " +
        $"s=[{string.Join(",", r.Scenes)}] g=[{string.Join(",", r.PenaltyReasons)}] f={r.Finished}";

    /// <summary>
    /// #825 (R814 P3-3, Probe P-1 des Reviews #822): ganzer Spieltag verschränkt = Paarung für Paarung über
    /// <see cref="MatchEngine.PlayWithEvents"/> auch mit mehreren Managern, im K.-o. (Play-off, DEB-Pokal: Verlängerung,
    /// Penaltyschießen) und mit Anzeigezügen (Darstellung 1, Geschwindigkeit 40: Szenenwahl und Strafgrund im Kern, R18-D3).
    /// Verglichen: Berichte, ganzer Spielstand, alle Anzeigereihen mit Szenen und Strafgründen.
    /// </summary>
    [Theory]
    [InlineData(new[] { 2 }, MatchCompetition.Playoff, true, 1, 40)]
    [InlineData(new[] { 2, 3 }, MatchCompetition.League, false, 0, 70)]
    [InlineData(new[] { 2, 3 }, MatchCompetition.DebCup, true, 1, 40)]
    [InlineData(new[] { 2, 5, 7 }, MatchCompetition.League, false, 1, 40)]
    [InlineData(new[] { 2, 5, 7 }, MatchCompetition.Playoff, true, 0, 70)]
    [InlineData(new[] { 2, 5, 7 }, MatchCompetition.DebCup, true, 1, 40)]
    public void WholeDay_SeveralManagers_KnockOutAndDisplay_EqualsSequential(int[] managedClubs,
        MatchCompetition competition, bool knockOut, int liveMode, int speed)
    {
        var scenes = 0;
        var reasons = 0;
        var overtime = 0;
        for (var seed = 0; seed < 12; seed++)
        {
            var a = NewGame(seed, managedClubs, liveMode, speed);
            var b = NewGame(seed, managedClubs, liveMode, speed);
            var pairs = FirstDay(a);
            Assert.Contains(pairs, p => managedClubs.Contains(p.Home) || managedClubs.Contains(p.Away));
            var expected = pairs.Select((p, i) => MatchEngine.PlayWithEvents(a, a.Clubs[p.Home], a.Clubs[p.Away],
                competition, knockOut, new GameRandom(100 * seed + i))).ToList();
            var day = LiveMatchDay.Begin(b, pairs.Select((p, i) => new LiveFixture(b.Clubs[p.Home], b.Clubs[p.Away],
                competition, knockOut, new GameRandom(100 * seed + i))));
            var actual = day.Finish();
            Assert.Equal(expected.Select(Fingerprint), actual.Select(Fingerprint));
            Assert.Equal(SaveGame.Save(a), SaveGame.Save(b));
            Assert.Equal(a.LiveScoreboard!.Rows.Select(RowFingerprint), b.LiveScoreboard!.Rows.Select(RowFingerprint));
            foreach (var row in b.LiveScoreboard!.Rows)
            {
                scenes += row.Scenes.Count;
                reasons += row.PenaltyReasons.Count;
            }
            overtime += expected.Count(r => r.Overtime);
        }
        if (liveMode != 0)
        {
            Assert.True(scenes > 0, "keine Szenen in der Probe");
            Assert.True(reasons > 0, "keine Strafgründe in der Probe");
        }
        if (knockOut)
            Assert.True(overtime > 0, "keine Verlängerung in der Probe");
    }

    /// <summary>
    /// [ORIGINAL] 0x37A62: Minute für Minute alle Paarungen – nach Schritt m hat jede laufende Paarung genau Minute m gerechnet,
    /// ihr Bericht enthält keine späteren Ereignisse; Spielwertung und schneller Weg laufen nicht durch die Schleife.
    /// </summary>
    [Fact]
    public void Step_AdvancesAllPairingsOneMinute()
    {
        var game = NewGame();
        var pairs = FirstDay(game);
        var shared = new GameRandom(9);
        var day = LiveMatchDay.Begin(game, pairs.Select(p => new LiveFixture(game.Clubs[p.Home], game.Clubs[p.Away],
            MatchCompetition.League, KnockOut: false, shared)));
        Assert.Equal(0, day.Minute);
        for (var m = 1; m <= 60; m++)
        {
            Assert.True(day.Step());
            Assert.Equal(m, day.Minute);
            foreach (var match in day.Matches)
            {
                Assert.Equal(m, match.LastMinute);
                Assert.All(match.Report.Events, e => Assert.True(e.Minute <= m));
                Assert.Equal(m < 60, match.Running);
            }
        }
        Assert.False(day.Step());
        Assert.Equal(pairs.Count, day.Finish().Count);
        Assert.Throws<InvalidOperationException>(() => day.Finish());
    }

    /// <summary>
    /// Abnahme A5: Ein Eingriff nach Minute m wirkt erst ab m + 1 – Minuten 1 … m unverändert, die Rechnung der Minute m + 1
    /// liest den neuen Einsatz (Stärken 0x368B1), und das Spiel verläuft danach anders.
    /// </summary>
    [Fact]
    public void Intervention_AfterMinuteM_TakesEffectFromNextMinute()
    {
        const int m = 30;
        var differed = 0;
        for (var seed = 0; seed < 20; seed++)
        {
            var (plain, plainMatch) = Started(seed);
            var (changed, changedMatch) = Started(seed);
            for (var i = 0; i < m; i++)
            {
                plain.Step();
                changed.Step();
            }
            var team = changedMatch.TeamOf(ManagedClub)!;
            var opponent = changedMatch.HomeTeam == team ? changedMatch.AwayTeam! : changedMatch.HomeTeam!;
            team.Tactics!.EffortMode = TacticMode.Manual; // sonst überschreibt 0x939A den Einsatz in gerader Minute
            var before = OriginalMinutes.Strengths(team, opponent, m + 1);
            var effort = team.Effort == 0 ? MatchIntervention.MaxEffort : 0;
            Assert.True(MatchIntervention.SetEffort(team, effort));
            Assert.NotEqual(before, OriginalMinutes.Strengths(team, opponent, m + 1));
            Assert.Equal(Fingerprint(plainMatch.Report), Fingerprint(changedMatch.Report)); // Minuten 1 … m unverändert
            changed.Step();
            Assert.Equal(m + 1, changedMatch.LastMinute);
            Assert.Equal(effort, team.Effort); // MANUELL: Minute m + 1 hat mit dem neuen Einsatz gerechnet
            plain.Step();
            var early = Fingerprint(plainMatch.Report) != Fingerprint(changedMatch.Report);
            var a = Fingerprint(plain.Finish()[0]);
            var b = Fingerprint(changed.Finish()[0]);
            if (a != b || early)
                differed++;
        }
        Assert.True(differed > 0, "der Eingriff hat in keiner Probe gewirkt");

        static (LiveMatchDay Day, LiveMatch Match) Started(int seed)
        {
            var game = NewGame();
            var (home, away) = FirstDay(game).First(p => p.Home == ManagedClub || p.Away == ManagedClub);
            var day = LiveMatchDay.Begin(game, [new LiveFixture(game.Clubs[home], game.Clubs[away], MatchCompetition.League,
                KnockOut: false, new GameRandom(seed))]);
            return (day, day.Matches[0]);
        }
    }

    /// <summary>Eingriffe wirken nur auf den Managerverein (A3); der Computerverein der Paarung bleibt unverändert.</summary>
    [Fact]
    public void Intervention_ComputerSide_Refused()
    {
        var game = NewGame();
        var (home, away) = FirstDay(game).First(p => p.Home == ManagedClub || p.Away == ManagedClub);
        var day = LiveMatchDay.Begin(game, [new LiveFixture(game.Clubs[home], game.Clubs[away], MatchCompetition.League,
            KnockOut: false, new GameRandom(1))]);
        day.Step();
        var computer = day.Matches[0].TeamOf(home == ManagedClub ? away : home)!;
        Assert.False(MatchIntervention.SetEffort(computer, 0));
        Assert.True(day.Matches[0].TeamOf(ManagedClub)!.Managed);
        Assert.Null(day.Matches[0].TeamOf(999));
    }
}
