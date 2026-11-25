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
    /// Regressionstest (vorher rot): Nach dem Penaltyschießen steht der Endstand (Gleichstand + Tore). [ORIGINAL] Minutenweg
    /// 0x39789/0x397B3: Gleichstand + Penaltytore 2 … 5, ungleich, eine Marke. Schneller Weg 0x37761: Heim = h + random(0, 2) + 30,
    /// Gast = g + random(0, 2) – seit Spur re-reste Teil 2 (#925 (b), <c>docs/analyse/re-reste-23.md</c> §1) genau ein Wurf mit zwei
    /// Marken (0x32B9D, 0x3775D); ein stehender Gleichstand zählt für den Heimverein (0x34C76). Vorher verlangte der Test auch dort
    /// „bis ungleich“ und eine Marke (Rückschaltprobe: alter Code → rot, Marke 1 statt 2). Fixlauf RR-01 (Review #965, R15-E2 = C):
    /// eine ungleiche Paarung mit offener Paarung dahinter bekommt den Zusatzwurf mit einer Marke (0x32B23/0x32B3F → 0x376F0,
    /// 0x3775D; <c>MatchEngine.DebCupExtraRoll</c>) – im schnellen Weg sind deshalb Marken 0, 1 und 2 möglich; Marke 1 nur ungleich
    /// mit Sieger = Penaltysieger (Rückschaltprobe: alter Code + dieser Test → grün, da nur gelockert; RR-01 prüft <c>ReResteTests</c>).
    /// </summary>
    [Theory]
    // Fixlauf wettbewerbe-rest nach Rebase auf main 3cd9169 (R15-E2 = C): Minutenweg mit Saat 4 statt 5 – mit #784 und dem Spielplan
    // über die Satzfolge (A113, Datei 0x4E7C7) gibt es bei 5 kein Penaltyschießen (Vorbedingung); 1, 4, 7–10, 12 grün.
    // Rückschaltprobe: alter Code mit Saat 4 ebenfalls grün – die Probe hängt an der Saat.
    // Spur tagesabschluss Teil 2 (Runde 26, R26-E1 = B, R21-D2 = A, R15-E2 = C): Minutenweg mit Saat 7 statt 4 – die wöchentlichen Stadionschäden 0x3CFA0 (Datei 0x3D330) würfeln je Manager und Bautag mindestens random(0, 2·d + 8) (Datei 0x3CFC5), anderer Zufallsverlauf ab dem ersten Bautag; bei 4 gibt es kein Penaltyschießen (Vorbedingung), unter 1–3, 5–10, 12 erfüllen 1–3, 5, 7–9 sie. Die Probe hängt an der Saat.
    [InlineData(7, true)]
    [InlineData(11, true)]
    [InlineData(5, false)] // #621 (R13GN-04): Saat wieder 5 – die Änderung 5 → 4 (L13) war nicht nötig
    public void DebCup_NoMatchEndsLevel_ShootoutInFinalScore(int seed, bool minuteLoop)
    {
        var game = SeasonWithCups(seed);
        game.Options.ShowDebCup = minuteLoop; // Anzeigeschalter DGROUP 0x227: Minutenweg bzw. schneller Weg (0x3702E)
        PlayToSeasonEnd(game);
        var cup = Assert.IsType<DebCup>(game.DebCup);
        var games = cup.Rounds.SelectMany(r => r).SelectMany(t => t.Games).ToList();
        Assert.NotEmpty(games);
        var withMarks = cup.Rounds.SelectMany(r => r).SelectMany(t => t.Games.Select((m, i) => (Match: m, Marks: t.MarksOf(i)))).ToList();
        // Spiele des Managervereins laufen auch ohne Anzeigeschalter durch die Minutenschleife (Vereinsfeld 0x16, MatchEngine.Live);
        // die Prüfungen des schnellen Wegs gelten nur für die übrigen (Fixlauf RR-01: der geänderte Zufallsstrom brachte bei Saat 5
        // ein Verlängerungstor des Managervereins, Marke 2 ohne Penaltysieger).
        var managerClubs = game.ActiveManagers.Select(m => m.ClubIndex).ToHashSet();
        var fast = withMarks.Where(x => minuteLoop || !(managerClubs.Contains(x.Match.Home) || managerClubs.Contains(x.Match.Away))).ToList();
        if (minuteLoop)
            Assert.All(games, m => Assert.NotEqual(m.HomeGoals, m.AwayGoals));
        else
            // #925 (b): stehender Gleichstand nur mit zwei Marken, Sieger der Heimverein (0x34C76 jnc).
            Assert.All(withMarks.Where(x => x.Match.HomeGoals == x.Match.AwayGoals),
                x => Assert.Equal((2, x.Match.Home), (x.Marks, x.Match.Winner)));
        // Minutenweg: Marke 2 Penaltyschießen, 1 Tor in der Verlängerung (C07); schneller Weg: 2 (Wurf bei Gleichstand) oder 1 (Wurf einer
        // ungleichen Paarung mit offener Paarung dahinter, RR-01 – Endstand ungleich).
        // C07 (Runde 25, wettbewerbe-anzeige, R15-E2 = C): [ORIGINAL] Verlängerungstor eine Marke, Penaltyschießen und Gleichstand im schnellen Weg zwei (Beleg c07_marken.py, re-abweichungen C07); vorher umgekehrt.
        var expectedMarks = 2;
        if (!minuteLoop)
            Assert.All(fast.Where(x => x.Marks == 1),
                x => Assert.NotEqual(x.Match.HomeGoals, x.Match.AwayGoals));
        var marked = fast.Where(x => x.Marks == expectedMarks).ToList();
        Assert.True(marked.Count > 0, "Kein Penaltyschießen in der Saison – Test prüft nichts");
        Assert.All(fast.Where(x => x.Marks == expectedMarks || (!minuteLoop && x.Marks == 1)),
            x => Assert.Equal(x.Match.Winner, x.Match.PenaltyWinner));
        // Schneller Weg: ein Wurf random(0, 2) je Seite → Abstand 0 … 2; Minutenweg: Penaltytore 2 … 5 → Abstand 1 … 3.
        Assert.All(marked, x => Assert.InRange(Math.Abs(x.Match.HomeGoals - x.Match.AwayGoals), minuteLoop ? 1 : 0, minuteLoop ? 3 : 2));
    }

    /// <summary>
    /// Regressionstest (vorher rot): WM-Endrunde ebenso – Endstand ungleich, Marke 2 nach dem schnellen Weg (C07, vorher 1) (Code 10 nimmt
    /// immer 0x3731F, R9S-02), Abstand höchstens 2 (random(0, 2) je Seite, 0x376F0).
    /// </summary>
    [Fact]
    public void WorldCupKnockout_ShootoutInFinalScore_TwoMarks()
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
                    // C07 (Runde 25, wettbewerbe-anzeige, R15-E2 = C): [ORIGINAL] Verlängerungstor eine Marke, Penaltyschießen und Gleichstand im schnellen Weg zwei (Beleg c07_marken.py, re-abweichungen C07); vorher umgekehrt.
                    Assert.Equal(m.PenaltyWinner is null ? 0 : 2, marks);
                    if (marks == 2)
                    {
                        marked++;
                        Assert.InRange(Math.Abs(m.HomeGoals - m.AwayGoals), 1, 2);
                    }
                }
        }
        Assert.True(marked > 0, "Kein WM-Penaltyschießen – Test prüft nichts");
    }

    /// <summary>
    /// Endstand und Marken aus dem Spielbericht. [ORIGINAL] C07 (Runde 25, Beleg c07_marken.py): Penaltyschießen zwei Marken
    /// (0x32AA3 + 0x3901B, Datei 0x39770–0x397B3); ein Tor in der Verlängerung schreibt den Stand ohne Marken + Tor mit Heim + 30
    /// (Datei 0x381FF–0x3824B, 0x38402) – eine Marke. Vorher umgekehrt nach eisbaeren_f44_01330 „4: 3..“.
    /// </summary>
    [Fact]
    public void FromReport_EndScoreAndMarks()
    {
        var game = TestGames.Create(3);
        var (home, away) = (game.Clubs[4], game.Clubs[5]);

        var regular = CupMatch.FromReport(1, home, away, new MatchReport { HomeGoals = 3, AwayGoals = 1 });
        Assert.Equal((3, 1, 0, (int?)null), (regular.Match.HomeGoals, regular.Match.AwayGoals, regular.Marks, regular.Match.PenaltyWinner));

        // C07 (Runde 25, wettbewerbe-anzeige, R15-E2 = C): [ORIGINAL] Verlängerungstor eine Marke, Penaltyschießen und Gleichstand im schnellen Weg zwei (Beleg c07_marken.py, re-abweichungen C07); vorher umgekehrt.
        var overtime = CupMatch.FromReport(1, home, away, new MatchReport { HomeGoals = 3, AwayGoals = 4, Overtime = true });
        Assert.Equal((3, 4, 1, (int?)null), (overtime.Match.HomeGoals, overtime.Match.AwayGoals, overtime.Marks, overtime.Match.PenaltyWinner));

        var shootout = CupMatch.FromReport(1, home, away, new MatchReport
        {
            HomeGoals = 2, AwayGoals = 2, Overtime = true, ShootoutGoals = (3, 5), ShootoutHomeWins = false,
        });
        Assert.Equal((5, 7, 2, (int?)away.Index), (shootout.Match.HomeGoals, shootout.Match.AwayGoals, shootout.Marks, shootout.Match.PenaltyWinner));

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
        root["schema"] = 30; // vor Teil 1 (Integration Runde 11: 30 → 31)
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
    /// des Berichts bleibt der Gleichstand (Spielerzähler nur für echte Tore). Seit Spur re-reste Teil 2 (#925 (b)) darf der Wurf im
    /// DEB-Pokal gleich bleiben (x = y, <c>ShootoutHomeWins</c> = false, Wertung für den Heimverein in <see cref="CupMatch.FromReport"/>);
    /// vorher verlangte der Test x ≠ y (Rückschaltprobe: alter Code → grün, da bis ungleich; die Umsetzung prüft <c>ReResteTests</c>).
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
            Assert.True(report.FastCupPath);
            Assert.Equal(x > y, report.ShootoutHomeWins);
        }
        Assert.True(ties > 0);
    }
}
