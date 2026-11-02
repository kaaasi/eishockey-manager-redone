using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur wettbewerbe-spiel, Teil 1: Pokal- und WM-Gleichstand (Inventur P2 Nr. 3; A003, A032, A275, A277). Fundstellen:
/// <c>docs/spielberechnung.md</c> §3 (schneller Weg 0x376F0–0x377A3) und §19 (Penaltyschießen 0x3901B, Endstand
/// 0x39770–0x397B3), Marken der Verlängerung 0x38402.
/// Befund vorher (main 7332a0f): Ein Pokal- oder WM-K.-o.-Spiel mit Penaltyschießen stand als Gleichstand mit getrenntem
/// Sieger im Spiel; das Original schreibt den Endstand (Gleichstand + Penaltytore) mit einer Marke + 30. Ein in der
/// Verlängerung entschiedenes Spiel bekam keine Marke (Original: zwei Punkte, Video eisbaeren_f44_01330 „4: 3..“).
/// </summary>
public class WettbewerbeSpiel1Tests
{
    private static GameState SeasonWithCups(int seed)
    {
        var game = TestGames.Create(seed);
        Assert.True(game.Setup.DebCup);
        return game;
    }

    private static void PlayToSeasonEnd(GameState game)
    {
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 2000, "Saisonende nicht erreicht");
            foreach (var manager in game.Managers)
                manager.Money = Math.Max(manager.Money, 10_000_000);
            game.Continue();
        }
    }

    /// <summary>
    /// Regressionstest (vorher rot): Kein Spiel des DEB-Pokals endet im Spielstand unentschieden – nach dem Penaltyschießen
    /// steht der Endstand (Gleichstand + Tore). [ORIGINAL] Schneller Weg 0x37761: Heim = h + random(0, 2) + 30, Gast =
    /// g + random(0, 2), bis ungleich; Minutenweg 0x39789/0x397B3: Gleichstand + Penaltytore 2 … 5.
    /// </summary>
    [Theory]
    [InlineData(5, true)]
    [InlineData(11, true)]
    [InlineData(5, false)]
    public void DebCup_NoMatchEndsLevel_ShootoutInFinalScore(int seed, bool minuteLoop)
    {
        var game = SeasonWithCups(seed);
        game.Options.ShowDebCup = minuteLoop; // Anzeigeschalter DGROUP 0x227: Minutenweg bzw. schneller Weg (0x3702E)
        PlayToSeasonEnd(game);
        var cup = Assert.IsType<DebCup>(game.DebCup);
        var games = cup.Rounds.SelectMany(r => r).SelectMany(t => t.Games).ToList();
        Assert.NotEmpty(games);
        Assert.All(games, m => Assert.NotEqual(m.HomeGoals, m.AwayGoals));
        var marked = cup.Rounds.SelectMany(r => r).SelectMany(t => t.Games.Select((m, i) => (Match: m, Marks: t.MarksOf(i))))
            .Where(x => x.Marks == 1).ToList();
        Assert.True(marked.Count > 0, "Kein Penaltyschießen in der Saison – Test prüft nichts");
        Assert.All(marked, x => Assert.Equal(x.Match.Winner, x.Match.PenaltyWinner));
        // Schneller Weg: random(0, 2) je Seite → Abstand 1 oder 2; Minutenweg: Penaltytore 2 … 5 → Abstand 1 … 3.
        Assert.All(marked, x => Assert.InRange(Math.Abs(x.Match.HomeGoals - x.Match.AwayGoals), 1, minuteLoop ? 3 : 2));
    }

    /// <summary>
    /// Regressionstest (vorher rot): WM-Endrunde ebenso – Endstand ungleich, Marke 1 nach dem schnellen Weg (Code 10 nimmt
    /// immer 0x3731F, R9S-02), Abstand höchstens 2 (random(0, 2) je Seite, 0x376F0).
    /// </summary>
    [Fact]
    public void WorldCupKnockout_ShootoutInFinalScore_OneMark()
    {
        var marked = 0;
        for (var seed = 1; seed <= 6; seed++)
        {
            var game = SeasonWithCups(seed);
            PlayToSeasonEnd(game);
            if (game.WorldCup is not { } wm)
                continue;
            for (var stage = 0; stage < wm.Knockout.Count; stage++)
                for (var i = 0; i < wm.Knockout[stage].Count; i++)
                {
                    var m = wm.Knockout[stage][i];
                    Assert.NotEqual(m.HomeGoals, m.AwayGoals);
                    var marks = wm.MarksOf(stage, i);
                    Assert.Equal(m.PenaltyWinner is null ? 0 : 1, marks);
                    if (marks == 1)
                    {
                        marked++;
                        Assert.InRange(Math.Abs(m.HomeGoals - m.AwayGoals), 1, 2);
                    }
                }
        }
        Assert.True(marked > 0, "Kein WM-Penaltyschießen – Test prüft nichts");
    }

    /// <summary>
    /// Endstand und Marken aus dem Spielbericht. [ORIGINAL] 0x37761 (schneller Weg) und 0x39789 (Minutenweg): eine Marke
    /// + 30 auf dem Endstand nach dem Penaltyschießen; 0x38402: ein Tor in der Verlängerung schreibt Heim + 30 auf den
    /// schon markierten Stand (zwei Punkte, eisbaeren_f44_01330 „4: 3..“).
    /// </summary>
    [Fact]
    public void FromReport_EndScoreAndMarks()
    {
        var game = TestGames.Create(3);
        var (home, away) = (game.Clubs[4], game.Clubs[5]);

        var regular = CupMatch.FromReport(1, home, away, new MatchReport { HomeGoals = 3, AwayGoals = 1 });
        Assert.Equal((3, 1, 0, (int?)null), (regular.Match.HomeGoals, regular.Match.AwayGoals, regular.Marks, regular.Match.PenaltyWinner));

        var overtime = CupMatch.FromReport(1, home, away, new MatchReport { HomeGoals = 3, AwayGoals = 4, Overtime = true });
        Assert.Equal((3, 4, 2, (int?)null), (overtime.Match.HomeGoals, overtime.Match.AwayGoals, overtime.Marks, overtime.Match.PenaltyWinner));

        var shootout = CupMatch.FromReport(1, home, away, new MatchReport
        {
            HomeGoals = 2, AwayGoals = 2, Overtime = true, ShootoutGoals = (3, 5), ShootoutHomeWins = false,
        });
        Assert.Equal((5, 7, 1, (int?)away.Index), (shootout.Match.HomeGoals, shootout.Match.AwayGoals, shootout.Marks, shootout.Match.PenaltyWinner));

        var forfeit = CupMatch.FromReport(1, home, away, new MatchReport { HomeGoals = 0, AwayGoals = 2, Forfeit = true });
        Assert.Equal(0, forfeit.Marks);
    }

    /// <summary>
    /// Migration 22 → 23: Ein älterer Stand ohne Marken bekommt je Spiel 1 bei Penaltysieger, sonst 0; nach dem Laden sichert
    /// der Stand wieder byteidentisch, weitere Pokalspiele hängen ihre Marken richtig an.
    /// </summary>
    [Fact]
    public void Migration_OldSaveWithoutMarks_DerivesMarksFromPenaltyWinner()
    {
        var game = SeasonWithCups(5);
        PlayToSeasonEnd(game);
        var root = System.Text.Json.Nodes.JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        root["schema"] = 22;
        var stripped = 0;
        Strip(root);
        Assert.True(stripped >= 2, "Keine Marken im Stand gefunden");

        var result = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.True(result.Success, result.Error);
        var loaded = result.Game!;
        var cup = Assert.IsType<DebCup>(loaded.DebCup);
        foreach (var tie in cup.Rounds.SelectMany(r => r))
        {
            Assert.Equal(tie.Games.Count, tie.Marks.Count);
            for (var i = 0; i < tie.Games.Count; i++)
                Assert.Equal(tie.Games[i].PenaltyWinner is null ? 0 : 1, tie.Marks[i]);
        }
        var bytes = SaveGame.Save(loaded);
        Assert.Equal(bytes, SaveGame.Save(SaveGame.Load(bytes).Game!));

        void Strip(System.Text.Json.Nodes.JsonNode? node)
        {
            if (node is System.Text.Json.Nodes.JsonObject obj)
            {
                if (obj.Remove("Marks") | obj.Remove("_knockoutMarks"))
                    stripped++;
                foreach (var (_, child) in obj.ToArray())
                    Strip(child);
            }
            else if (node is System.Text.Json.Nodes.JsonArray array)
                foreach (var child in array)
                    Strip(child);
        }
    }

    /// <summary>
    /// Schneller Weg: Der Bericht trägt die Zusatztore random(0, 2) als <see cref="MatchReport.ShootoutGoals"/>; der Spielstand
    /// des Berichts bleibt der Gleichstand (Spielerzähler nur für echte Tore).
    /// </summary>
    [Fact]
    public void FastPath_ReportCarriesExtraGoals()
    {
        var game = TestGames.Create(3);
        game.Options.ShowDebCup = false; // schneller Weg 0x3731F
        MatchEngine.EnsureTeamValues(game);
        var (home, away) = (game.Clubs[4], game.Clubs[5]);
        var ties = 0;
        for (var seed = 0; seed < 400; seed++)
        {
            var report = MatchEngine.PlayWithEvents(game, home, away, MatchCompetition.DebCup, knockOut: true, new Random(seed));
            if (report.ShootoutHomeWins is null)
                continue;
            ties++;
            Assert.Equal(report.HomeGoals, report.AwayGoals);
            var (x, y) = Assert.IsType<(int, int)>(report.ShootoutGoals);
            Assert.InRange(x, 0, 2);
            Assert.InRange(y, 0, 2);
            Assert.NotEqual(x, y);
            Assert.Equal(x > y, report.ShootoutHomeWins);
        }
        Assert.True(ties > 0);
    }
}
