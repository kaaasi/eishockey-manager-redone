using EishockeyManager.Core;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur national, Teil 1 (Runde 22): Satz des Bundestrainers und Managerwert (A424, A097, A551), Tagesform nach WM-Gruppenspielen
/// (A109) und Einsatz des Vorspiels je Land in der Spielvorbereitung 0x8365 (A494). Fundstellen in den Klassenkommentaren
/// (<c>MatchEngine.National.cs</c>, <c>CupsNational.ManagerRecord.cs</c>, <c>CupsWorld.cs</c>).
/// </summary>
public class NationalTeil1Tests
{
    private static GameState CreateWorldOnly(int seed)
    {
        var game = GameModeTests.Create(seed, flag: false, globe: true, 5);
        Assert.Equal(GameMode.WorldOnly, game.Mode);
        return game;
    }

    /// <summary>Sprung zur WM und Ernennung, Kader „auto“.</summary>
    private static NationalCoach Appoint(GameState game)
    {
        game.Continue();
        game.AutoNominate();
        Assert.NotNull(game.NationalCoach);
        Assert.NotEmpty(game.NationalCoach!.Squad);
        return game.NationalCoach;
    }

    private static int GroupResults(GameState game) => game.WorldCup?.Groups.Sum(g => g.Results.Count) ?? 0;

    /// <summary>Bis nach dem nächsten WM-Termin mit Gruppenspielen.</summary>
    private static void PlayNextGroupDate(GameState game)
    {
        var before = GroupResults(game);
        for (var guard = 0; GroupResults(game) == before; guard++)
        {
            Assert.True(guard < 60, "kein WM-Gruppentermin erreicht");
            game.Continue();
        }
    }

    private static IEnumerable<int> Nations => Enumerable.Range(NationalTeams.First, NationalTeams.Last - NationalTeams.First + 1);

    /// <summary>
    /// A109: [ORIGINAL] Datei 0x30F7E → 0x30852 → 0x50283 (Code 8) → 0x4F527: nach den Gruppenspielen eines Termins wandert die
    /// Tagesform beider Länder um random(0, 2 · |Tordifferenz|) zum Sieger hin, begrenzt auf 47 … 53. Rot vor Teil 1: die
    /// Tagesform der Länder blieb über die ganze WM stehen.
    /// </summary>
    [Fact]
    public void WorldCupGroupDate_UpdatesDayFormOfBothNations_TowardsWinner()
    {
        var game = CreateWorldOnly(seed: 11);
        Appoint(game);
        MatchEngine.EnsureTeamValues(game);
        var changed = 0;
        for (var date = 0; date < WorldCup.GroupDates; date++)
        {
            foreach (var n in Nations)
                game.Clubs[n].TeamValues = MatchEngine.TeamValuesOf(game.Clubs[n]) with { DayForm = 50 };
            var known = GroupResults(game);
            PlayNextGroupDate(game);
            foreach (var m in game.WorldCup!.Groups.SelectMany(g => g.Results.Skip(g.Results.Count - WorldCup.GroupSize / 2)))
            {
                var home = game.Clubs[m.Home].TeamValues!.Value.DayForm;
                var away = game.Clubs[m.Away].TeamValues!.Value.DayForm;
                Assert.InRange(home, MatchEngine.DayFormMin, MatchEngine.DayFormMax);
                Assert.InRange(away, MatchEngine.DayFormMin, MatchEngine.DayFormMax);
                if (m.HomeGoals == m.AwayGoals)
                    Assert.True(home == 50 && away == 50, $"Unentschieden {home}/{away}"); // random(0, 0) = 0
                else if (m.HomeGoals > m.AwayGoals)
                    Assert.True(home >= 50 && away <= 50, $"{m.Home}-{m.Away} {m.HomeGoals}:{m.AwayGoals} → {home}/{away}");
                else
                    Assert.True(home <= 50 && away >= 50, $"{m.Home}-{m.Away} {m.HomeGoals}:{m.AwayGoals} → {home}/{away}");
                if (home != 50 || away != 50)
                    changed++;
            }
            Assert.Equal(known + WorldCup.GroupSize, GroupResults(game));
        }
        Assert.True(changed > 0, "Tagesform der Länder nach den WM-Gruppenspielen nicht nachgeführt");
    }

    /// <summary>
    /// A494: [ORIGINAL] 0x368B1 rechnet die Stärke vor dem Spiel mit dem stehenden Einsatz 0x26 (Datei 0x9C66). Rot vor Teil 1:
    /// <see cref="GameState.NationalMatchStrength(int)"/> nahm immer den Einsatz der Automatik beim Stand 0:0.
    /// </summary>
    [Fact]
    public void NationalMatchStrength_ReadsStandingEffortOfLastMatch()
    {
        var game = CreateWorldOnly(seed: 3);
        const int nation = NationalTeams.First; // ohne Bundestrainer
        game.NationFieldTactics(nation).RecordMatchFields(0, 0);
        var low = game.NationalMatchStrength(nation);
        game.NationFieldTactics(nation).RecordMatchFields(100, 0);
        var high = game.NationalMatchStrength(nation);
        Assert.True(high > low, $"Stärke mit Einsatz 100 ({high}) nicht über der mit Einsatz 0 ({low})");
        // Neues Spiel: 0x26 = 0 (FILE02).
        Assert.All(Nations, n => Assert.Equal(0, CreateWorldOnly(seed: 3).NationFieldTactics(n).MatchEffort));
    }

    /// <summary>
    /// A494: [ORIGINAL] 0x8365 schreibt für Länder ohne Bundestrainer den Grundwert (60 bzw. 40) nach 0x26, der Angriff 0x2C bleibt
    /// aus dem letzten Spiel (Datei 0x8443–0x84BD); die Werte stehen bis zum nächsten Spiel. Länder, die nicht gegen das Land des
    /// Bundestrainers spielen, ändern sie im Spiel nicht (kein 0x939A ohne Managerpaarung). Rot vor Teil 1: 0x26 blieb 0.
    /// </summary>
    [Fact]
    public void WorldCupMatch_WritesBaseEffortForUnmanagedNations()
    {
        var game = CreateWorldOnly(seed: 5);
        var coach = Appoint(game);
        PlayNextGroupDate(game);
        var results = game.WorldCup!.Groups.SelectMany(g => g.Results).ToList();
        var checkedNations = 0;
        foreach (var m in results.Where(r => r.Home != coach.Nation && r.Away != coach.Nation))
            foreach (var n in new[] { m.Home, m.Away })
            {
                var t = game.NationFieldTactics(n);
                Assert.Contains(t.MatchEffort, new[] { 40, 60 });
                Assert.Equal(0, t.MatchAttack);
                checkedNations++;
            }
        Assert.Equal(10, checkedNations); // fünf der sechs Paarungen ohne Deutschland
    }

    private static int[] ShownEffortLine(Tactics t) =>
        [.. Enumerable.Range(0, Tactics.Situations).Select(i => t.ShownEffort(0, (ScoreSituation)i))];

    private static int[] ShownAttackLine(Tactics t) =>
        [.. Enumerable.Range(0, Tactics.Situations).Select(i => t.ShownAttack(0, (ScoreSituation)i))];

    /// <summary>
    /// A494 am DOSBox-Paar FILE18 → FILE19 (Freundschaftsspiel DEUTSCHLAND – SCHWEDEN, Heimspiel): [ORIGINAL] 0x8365 läuft auch im
    /// Freundschaftsspiel (Datei 0x30365 → 0x36B5D → 0x36F27/0x36F5A) und rechnet mit dem stehenden Einsatz 0x26 = 0 beider Länder.
    /// Deutschland zuerst: ungleich (Satz 54: 0x2D = 40, 0x27–0x2A = 20/20/15/20, Angriff 0x36–0x39 = 5/6/4/4 des Schwächeren);
    /// Schweden danach gegen den neuen Einsatz 40 Deutschlands: ausgeglichen (Satz 59: 0x2D = 60, 0x28–0x2A = 25/20/30, Angriff
    /// 0x36–0x3A = 4/5/6/7/4 des Favoriten). Rot vor Teil 1: das Freundschaftsspiel bereitete nicht vor (Anzeige blieb 0), und die
    /// Stärke rechnete mit dem Einsatz beim Stand 0:0.
    /// </summary>
    [GameDataFact]
    public void File18_FriendlyAgainstSweden_PreparesBothNationsLikeFile19()
    {
        var game = GameState.ImportOriginal(
            Formats.OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave("FILE18.HOK"))),
            Formats.ClubDatabase.Load(TestGameData.Load()), 1993);
        var sweden = game.Clubs.Single(c => c.Name == "SCHWEDEN").Index; // Satz 59 im Spielstand
        Assert.Equal(0, game.NationFieldTactics(NationalTeams.Germany).MatchEffort);
        Assert.Equal(0, game.NationFieldTactics(sweden).MatchEffort);
        // Stärken vor dem Spiel: Deutschland 210 gegen Schweden 225 (|2 · 15| ≥ 25), danach Schweden 225 gegen 219 (Einsatz 40).
        Assert.Equal((210, 225, 219), (game.NationalMatchStrength(NationalTeams.Germany), game.NationalMatchStrength(sweden),
            game.NationalMatchStrength(NationalTeams.Germany, 40)));
        Assert.Equal(FriendlyResult.Played, game.PlayFriendly(sweden, home: true));

        var germany = game.NationFieldTactics(NationalTeams.Germany);
        Assert.Equal([40, 20, 20, 15, 20, 0], ShownEffortLine(germany));
        Assert.Equal([0, 5, 6, 4, 4, 0], ShownAttackLine(germany));
        var swe = game.NationFieldTactics(sweden);
        Assert.Equal([60, 0, 25, 20, 30, 0], ShownEffortLine(swe));
        Assert.Equal([0, 4, 5, 6, 7, 4], ShownAttackLine(swe));
    }

    /// <summary>
    /// A424/A097: [ORIGINAL] Das Land des Bundestrainers spielt in der Managerpaarung mit Satz 5: Verletzungen (Satz 5), eigener
    /// Managerwert aus 0x952C und Zähler der Über-/Unterzahl im Satz des Bundestrainers. Rot vor Teil 1: Managerwert 20, kein
    /// Satz für die Verletzungen, keine Zähler.
    /// </summary>
    [Fact]
    public void NationalLiveTeam_OfCoach_UsesManagerRecordFive()
    {
        var game = CreateWorldOnly(seed: 7);
        var coach = Appoint(game);
        var team = MatchEngine.NationalLiveTeam(game, coach.Nation, 50, 50, managerMatch: true);
        Assert.NotNull(team.Squad);
        Assert.Equal(NationalCoach.ManagerRecord, team.InjuryManager);
        Assert.Same(coach.PowerPlayCounts, team.PowerPlayCounts);
        Assert.Equal(MatchEngine.ManagedTeamValues(team.Squad!).ManagerValue, team.ManagerValue);
        Assert.NotEqual(100, team.ManagerValue); // „auto“ stellt mehr als neun auf

        // Andere Länder und das Land ohne Managerpaarung: ohne Satz.
        var other = MatchEngine.NationalLiveTeam(game, NationalTeams.First, 50, 50, managerMatch: true);
        Assert.Null(other.InjuryManager);
        Assert.Null(other.PowerPlayCounts);
        Assert.Null(MatchEngine.NationalLiveTeam(game, coach.Nation, 50, 50, managerMatch: false).InjuryManager);
    }

    /// <summary>
    /// A424: [ORIGINAL] 0x38B8F verletzt auch Spieler des Bundestrainers (Satz 5 mit Verein DGROUP 0x9EDA ≤ 0x3F, Datei
    /// 0x38BE3–0x38BF0). Rot vor Teil 1: das Nationalteam hatte keinen Satz, es gab nie eine Verletzung.
    /// </summary>
    [Fact]
    public void Injuries_HitNationalSquadOfCoach()
    {
        var game = CreateWorldOnly(seed: 9);
        var coach = Appoint(game);
        var random = new Random(1);
        var injured = 0;
        for (var tries = 0; tries < 20000 && injured == 0; tries++)
        {
            var team = MatchEngine.NationalLiveTeam(game, coach.Nation, 50, 50, managerMatch: true);
            var opponent = MatchEngine.NationalLiveTeam(game, NationalTeams.First, 50, 50, managerMatch: true);
            var report = new MatchReport();
            OriginalMinutes.Injuries(team, opponent, 10, report, random);
            injured += report.Events.Count(e => e.Kind == MatchEventKind.Injury && e.Side == 0);
            Assert.DoesNotContain(report.Events, e => e.Kind == MatchEventKind.Injury && e.Side == 1);
        }
        Assert.Equal(1, injured);
        Assert.Contains(coach.Squad, m => m.Player.InjuredGames > 0);
    }

    /// <summary>
    /// A551 und A097 im Ablauf: [ORIGINAL] Belastungszug 0x8C53 auch im Länderspiel (Kaderplatz +0x13 des Satzes 5) und Zähler
    /// der Über-/Unterzahl im Satz des Bundestrainers. Rot vor Teil 1: Belastung der Nationalspieler unverändert, Zähler 0.
    /// </summary>
    [Fact]
    public void WorldCup_OfCoach_RaisesStrainAndCountsPowerPlay()
    {
        var game = CreateWorldOnly(seed: 13);
        var coach = Appoint(game);
        foreach (var m in coach.Squad)
            m.Player.Strain = 0;
        for (var guard = 0; game.WorldCup?.Winner is null; guard++)
        {
            Assert.True(guard < 100, "WM nicht beendet");
            game.Continue();
        }
        Assert.Contains(coach.Squad, m => m.Player.Strain > 0);
        Assert.True(coach.PowerPlayCounts.Sum() > 0, "keine Über-/Unterzahl im Satz des Bundestrainers gezählt");
    }

    /// <summary>
    /// Mehrsaisonprobe (AGENTS.md §5): vier Saisonwechsel im Modus „nur Weltkugel“, passiv (nur „auto“) und aktiv (dazu
    /// Freundschaftsspiele). Je Saison ein neuer Satz 5 mit eigenen Zählern; Tagesform der Länder in 47 … 53, Einsatz 0x26 der
    /// Länder in 0 … 100; Speichern und Laden behält Zähler und Einsatz.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WorldOnly_MultiSeason_ManagerRecordAndNationFields(bool active)
    {
        var game = CreateWorldOnly(seed: 17);
        var counted = 0;
        for (var change = 0; change < 4; change++)
        {
            var coach = Appoint(game);
            Assert.All(coach.PowerPlayCounts, c => Assert.Equal(0, c));
            if (active)
                for (var opponent = NationalTeams.First; opponent <= NationalTeams.Last; opponent++)
                    if (opponent != NationalTeams.Germany)
                        game.PlayFriendly(opponent, home: opponent % 2 == 0);
            for (var guard = 0; game.Phase == SeasonPhase.Running; guard++)
            {
                Assert.True(guard < 100, "Saisonende nicht erreicht");
                game.Continue();
            }
            counted += coach.PowerPlayCounts.Sum();
            foreach (var n in Nations)
            {
                Assert.InRange(game.Clubs[n].TeamValues!.Value.DayForm, MatchEngine.DayFormMin, MatchEngine.DayFormMax);
                Assert.InRange(game.NationFieldTactics(n).MatchEffort, 0, 100);
            }

            var loaded = SaveGame.Load(SaveGame.Save(game));
            Assert.True(loaded.Success, loaded.Error);
            Assert.Equal(coach.PowerPlayCounts, loaded.Game!.NationalCoach!.PowerPlayCounts);
            Assert.Equal(Nations.Select(n => game.NationFieldTactics(n).MatchEffort),
                Nations.Select(n => loaded.Game.NationFieldTactics(n).MatchEffort));
            game.StartNextSeason();
        }
        Assert.True(counted > 0);
    }
}
