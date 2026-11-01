using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur spielberechnung-komplett, Teil 4: Über-/Unterzahl-Zähler der Manager in der Minutenrechnung (0xA484, 0x35818) und
/// Länderwerte aus der Aufstellung des Bundestrainers im Stärkevergleich (#193, 0x952C). Fundstellen:
/// <c>docs/spielberechnung.md</c> §18.
/// </summary>
public class SpielberechnungKomplett4Tests
{
    /// <summary>Liefert vorgegebene Werte (geprüft gegen die Grenzen), danach immer das Minimum.</summary>
    private sealed class ScriptedRandom(params int[] values) : Random(0)
    {
        private readonly Queue<int> _values = new(values);

        public override int Next(int minValue, int maxValue)
        {
            var v = _values.Count > 0 ? _values.Dequeue() : minValue;
            Assert.InRange(v, minValue, maxValue - 1);
            return v;
        }
    }

    private static LiveTeam Managed(int[] counts) => new() { Managed = true, Effort = 60, PowerPlayCounts = counts };

    private static int[] Counts() => new int[PowerPlay.Count];

    private static void Occupy(LiveTeam team, int penalties)
    {
        for (var i = 0; i < penalties; i++)
            team.Box.Add(team.Box.Count, 2);
    }

    /// <summary>
    /// [ORIGINAL] 0x35A26–0x35B22: Nach der Strafe |eigene − gegnerische Feldspieler| = 1 → Gegnermanager +0x177 + 8
    /// (Überzahlspiele), eigener Manager 0x97DC (Unterzahlspiele). Züge: Strafe (lrandom(0, 12) = 0), zwei Minuten
    /// (lrandom(0, 15) = 1), keine Kette (lrandom(0, 3) = 1). Vorher: kein Zähler.
    /// </summary>
    [Fact]
    public void Penalty_StartingPowerPlay_CountsBothManagers()
    {
        var (own, other) = (Counts(), Counts());
        var club = Managed(own);
        var opponent = Managed(other);
        Assert.Equal(2, OriginalMinutes.Penalty(club, opponent, 0, 10, chained: false, [], new ScriptedRandom(0, 1, 1)));
        Assert.Equal(1, own[(int)PowerPlayCounter.ShortHanded]);
        Assert.Equal(0, own[(int)PowerPlayCounter.PowerPlays]);
        Assert.Equal(1, other[(int)PowerPlayCounter.PowerPlays]);
        Assert.Equal(0, other[(int)PowerPlayCounter.ShortHanded]);
    }

    /// <summary>
    /// [ORIGINAL] 0x120:0x61 ist |a − b|: Steht die bestrafte Mannschaft danach 4 gegen 3 noch in Überzahl, zählen trotzdem
    /// Überzahl beim Gegner und Unterzahl bei ihr (übernommen); bei 4 gegen 4 nichts.
    /// </summary>
    [Theory]
    [InlineData(2, 1)] // 5 gegen 3 → 4 gegen 3
    [InlineData(1, 0)] // 5 gegen 4 → 4 gegen 4
    public void Penalty_CountsOnAbsoluteDifferenceOne(int opponentPenalties, int expected)
    {
        var (own, other) = (Counts(), Counts());
        var club = Managed(own);
        var opponent = Managed(other);
        Occupy(opponent, opponentPenalties);
        // Strafe, zwei Minuten, keine Kette.
        OriginalMinutes.Penalty(club, opponent, 0, 10, chained: false, [], new ScriptedRandom(0, 1, 1));
        Assert.Equal(expected, own[(int)PowerPlayCounter.ShortHanded]);
        Assert.Equal(expected, other[(int)PowerPlayCounter.PowerPlays]);
    }

    /// <summary>
    /// [ORIGINAL] 0x35C7B–0x35CB2: Bleibt die Kettenstrafe gegen den Gegner aus (Rückgabe 0), wird nur der zuletzt gezählte
    /// Wert zurückgenommen – bei zwei Managervereinen die Unterzahl des bestraften, die Überzahl des Gegners bleibt.
    /// </summary>
    [Fact]
    public void Penalty_ChainWithoutPenalty_RollsBackLastCounter()
    {
        var (own, other) = (Counts(), Counts());
        // Strafe, zwei Minuten, Kette (lrandom(0, 3) = 0); Kette: lrandom(0, 12) = 1, lrandom(0, 110 − E) = 1 → keine Strafe.
        OriginalMinutes.Penalty(Managed(own), Managed(other), 0, 10, chained: false, [], new ScriptedRandom(0, 1, 0, 1, 1));
        Assert.Equal(0, own[(int)PowerPlayCounter.ShortHanded]);
        Assert.Equal(1, other[(int)PowerPlayCounter.PowerPlays]);

        // Gegner ohne Manager: zuletzt gezählt ist die eigene Unterzahl, sie wird zurückgenommen.
        var alone = Counts();
        OriginalMinutes.Penalty(Managed(alone), new LiveTeam(), 0, 10, chained: false, [], new ScriptedRandom(0, 1, 0, 1, 1));
        Assert.Equal(0, alone[(int)PowerPlayCounter.ShortHanded]);
    }

    /// <summary>Kettenstrafe verhängt (4 gegen 4): Zähler der ersten Strafe bleiben, die zweite zählt nicht.</summary>
    [Fact]
    public void Penalty_ChainWithPenalty_KeepsCounters()
    {
        var (own, other) = (Counts(), Counts());
        var club = Managed(own);
        var opponent = Managed(other);
        // Strafe, zwei Minuten, Kette; Kette: Strafe, zwei Minuten (Platz 0 frei, lrandom(0, 15) = 1).
        OriginalMinutes.Penalty(club, opponent, 0, 10, chained: false, [], new ScriptedRandom(0, 1, 0, 0, 1));
        Assert.Equal((4, 4), (club.Box.Skaters, opponent.Box.Skaters));
        Assert.Equal([0, 0, 0, 0, 0, 1], own);
        Assert.Equal([0, 0, 0, 0, 1, 0], other);
    }

    /// <summary>
    /// [ORIGINAL] 0xAA09–0xAAEF: Tor nach der Feldspielerlage zu Beginn des Abschlusses für Heim- und Gastmanager; das
    /// Gegentor beendet die Strafe (0xA8AC). Vorher: <see cref="GameState.CountPowerPlayGoal"/> war nicht angeschlossen.
    /// </summary>
    [Fact]
    public void GoalScored_PowerPlayGoal_CountsForBothManagers()
    {
        var (h, a) = (Counts(), Counts());
        var home = Managed(h);
        var away = Managed(a);
        Occupy(away, 1);
        OriginalMinutes.GoalScored(home, away, 0, home.Box.Skaters, away.Box.Skaters);
        Assert.Equal(5, away.Box.Skaters); // Strafe beendet
        Assert.Equal([1, 0, 0, 0, 0, 0], h); // Tor in Überzahl
        Assert.Equal([0, 0, 0, 1, 0, 0], a); // Gegentor in Unterzahl
    }

    /// <summary>[ORIGINAL] 0xAA18–0xAA29: zusammen 11 Feldspieler (5 gegen 6, Torhüter draußen) → kein Zähler.</summary>
    [Fact]
    public void GoalScored_FiveAgainstSix_CountsNothing()
    {
        var (h, a) = (Counts(), Counts());
        OriginalMinutes.GoalScored(Managed(h), Managed(a), 1, 5, 6);
        Assert.All(h, v => Assert.Equal(0, v));
        Assert.All(a, v => Assert.Equal(0, v));
        // 4 gegen 6 (zusammen 10) zählt: Gast trifft in Überzahl.
        OriginalMinutes.GoalScored(Managed(h), Managed(a), 1, 4, 6);
        Assert.Equal([0, 0, 0, 1, 0, 0], h);
        Assert.Equal([1, 0, 0, 0, 0, 0], a);
    }

    /// <summary>
    /// [ORIGINAL] 0xA8F9–0xA9A2: Gleich viele Feldspieler (4 gegen 4) und das Gegentor beendet die Strafe der verteidigenden
    /// Mannschaft → danach 5 gegen 4: Manager des Schützen Unterzahlspiele, Manager der verteidigenden Mannschaft
    /// Überzahlspiele. Das Tor selbst fiel bei gleicher Zahl (kein Torzähler).
    /// </summary>
    [Fact]
    public void GoalScored_EvenStrengthGoalEndingPenalty_CountsNewPowerPlay()
    {
        var (h, a) = (Counts(), Counts());
        var home = Managed(h);
        var away = Managed(a);
        Occupy(home, 1);
        Occupy(away, 1);
        OriginalMinutes.GoalScored(home, away, 1, 4, 4);
        Assert.Equal((5, 4), (home.Box.Skaters, away.Box.Skaters));
        Assert.Equal([0, 0, 0, 0, 1, 0], h);
        Assert.Equal([0, 0, 0, 0, 0, 1], a);
    }

    /// <summary>Computervereine haben keine Zähler; ein Tor ohne Manager ändert nichts und wirft nicht.</summary>
    [Fact]
    public void GoalScored_ComputerClubs_NoCounters()
    {
        var home = new LiveTeam();
        var away = new LiveTeam();
        Occupy(away, 1);
        OriginalMinutes.GoalScored(home, away, 0, 5, 4);
        Assert.Equal(5, away.Box.Skaters);
    }

    /// <summary>
    /// Anschluss: Eine Saison mit Manager füllt seine Zähler (Bildschirm „Verein“, Spur statistiken-verein). Vorher blieben
    /// alle sechs Werte 0. Die Zähler ziehen keinen Zufall, Ergebnisse und Abdrücke bleiben gleich.
    /// </summary>
    [Fact]
    public void Season_FillsManagerPowerPlayCounters()
    {
        var game = TestGames.Create(5);
        var manager = game.Managers[0];
        Assert.Same(manager.PowerPlayCounts, MatchEngine.LiveTeamOf(game, game.Clubs[manager.ClubIndex],
            game.Clubs.First(c => c.Index != manager.ClubIndex), managed: true, managerMatch: true).PowerPlayCounts);
        TestSeason.PlayRegularSeason(game);
        var counts = manager.PowerPlayCounts;
        Assert.True(counts[(int)PowerPlayCounter.PowerPlays] > 0);
        Assert.True(counts[(int)PowerPlayCounter.ShortHanded] > 0);
        Assert.True(counts[0] + counts[1] + counts[2] + counts[3] > 0);
        Assert.All(counts, v => Assert.True(v >= 0));
    }

    /// <summary>
    /// #193: [ORIGINAL] Der Stärkevergleich zeigt für das Land des Bundestrainers die Teamwerte aus seiner Aufstellung
    /// (0x952C mit Manager 5: Kondition, Technik und Form; Tageslauf 0x54F9C, nach Freundschaftsspielen 0x555D6). Vorher:
    /// Würfelwerte und das Mittel der Spielerform.
    /// </summary>
    [Fact]
    public void StrengthTable_CoachNation_ShowsLineupValues()
    {
        var game = GameModeTests.Create(7, false, true, 2);
        game.Continue();
        game.AutoNominate();
        var coach = game.NationalCoach!;
        Assert.Equal(NationalTeams.Germany, coach.Nation);
        FillFormation(coach);
        var germany = new Club { Index = NationalTeams.Germany, Name = "Deutschland", Strength = 60 };

        var (d, a, _) = MatchEngine.LineupTeamValues(coach.Formation);
        var row = StrengthTable.Compute(game, [germany], StrengthView.Abwehr).Single();
        Assert.Equal((d.Condition, d.Technique, d.Form), (row.Condition, row.Technique, row.Form));
        row = StrengthTable.Compute(game, [germany], StrengthView.Sturm).Single();
        Assert.Equal((a.Condition, a.Technique, a.Form), (row.Condition, row.Technique, row.Form));

        // DOSBox 27 → 41: der Bonus der Freundschaftsspiele wirkt über die Aufstellung auf die Form im Stärkevergleich.
        var before = StrengthTable.Compute(game, [germany], StrengthView.Gesamt).Single();
        foreach (var p in coach.Formation.Players)
            p.Form += 5;
        var after = StrengthTable.Compute(game, [germany], StrengthView.Gesamt).Single();
        Assert.True(after.Form > before.Form);
    }

    /// <summary>[APPROX] Ohne eigene Aufstellung bleiben die Länderwerte (wie nach dem Freundschaftsspiel in PlayFriendly).</summary>
    [Fact]
    public void StrengthTable_CoachWithoutFormation_KeepsRatings()
    {
        var game = GameModeTests.Create(7, false, true, 2);
        game.Continue();
        game.AutoNominate();
        Assert.Equal(0, game.NationalCoach!.Formation.Count);
        var germany = new Club { Index = NationalTeams.Germany, Name = "Deutschland", Strength = 60 };
        var (cd, td, _, _) = game.NationalRatings(NationalTeams.Germany);
        var row = StrengthTable.Compute(game, [germany], StrengthView.Abwehr).Single();
        Assert.Equal((cd, td), (row.Condition, row.Technique));
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
}
