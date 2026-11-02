using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur spielstaerke-manager-2: Managerteil der Spielberechnung (0x98F8, 0x87F7, 0x8EF2, 0x80CB, 0x939A, 0x378C5, 0x952C,
/// 0x84D7) und R8-A03. Fundstellen: <c>docs/spielberechnung.md</c> §13.
/// </summary>
public class SpielstaerkeManager2Tests
{
    private static Player P(PlayerRole role, int c, int t, int f, int number = 1, int energy = 100) =>
        new() { Name = $"P{number}", Role = role, Number = number, Condition = c, Technique = t, Form = f, Energy = energy };

    private static LivePlayer L(Player p, int position, int energy = 90) =>
        new() { Player = p, Position = position, Dressed = true, Energy = energy };

    /// <summary>Abwehr der Reihe 0: Torhüter (Position 1) und zwei Verteidiger; Sturm: drei Stürmer.</summary>
    private static LiveSquad Squad(int energy = 90)
    {
        var s = new LiveSquad { Difficulty = 2 };
        s.Players.Add(L(P(PlayerRole.Goalkeeper, 60, 70, 50, 1), 1, energy));
        s.Players.Add(L(P(PlayerRole.Defender, 80, 90, 40, 2), 1, energy));
        s.Players.Add(L(P(PlayerRole.Defender, 70, 60, 60, 3), 1, energy));
        s.Players.Add(L(P(PlayerRole.Forward, 50, 80, 50, 4), 0x81, energy));
        s.Players.Add(L(P(PlayerRole.Forward, 60, 70, 50, 5), 0x81, energy));
        s.Players.Add(L(P(PlayerRole.Forward, 70, 60, 50, 6), 0x81, energy));
        s.Players.Add(L(P(PlayerRole.Forward, 99, 99, 99, 7), 0x82, energy));
        return s;
    }

    // ---- R8-A03 ------------------------------------------------------------------------------------------------

    /// <summary>
    /// R8-A03: Zugfolge des Laders 0x2EF1B genau nachgerechnet (ein Zusatzzug zwischen Kondition und Technik fiel bisher nur
    /// in Abdrucktests auf). Je Verein: Tagesform, dann je Teil Kondition (2 Züge), Technik (2 Züge), Form.
    /// </summary>
    [Fact]
    public void Loader_DrawSequence_Exact()
    {
        var game = TestGames.Create(3);
        var state = game.Random;
        var check = GameRandom.FromState(state.Capture());
        MatchEngine.LoadTeamValues(game);
        int R(int a, int b) => check.Next(a, b + 1);
        foreach (var club in game.Clubs.OrderBy(c => c.Index))
        {
            var day = R(47, 53);
            var t = club.Strength / 10;
            var dc = -R(0, t) + R(0, t);
            var dt = -R(0, t) + R(0, t);
            var df = R(45, 55) - (club.Index > 64 ? 5 : 0);
            var ac = -R(0, t) + R(0, t);
            var at = -R(0, t) + R(0, t);
            var af = R(45, 55) - (club.Index > 64 ? 5 : 0);
            if (club.Index is >= NationalTeams.First and <= NationalTeams.Last)
                dc = dt = ac = at = 0;
            Assert.Equal(new ClubTeamValues(dc, ac, dt, at, df, af, day), club.TeamValues);
        }
        Assert.Equal(check.Next(), state.Next());
    }

    /// <summary>
    /// R8-A03: Auch eine Spielwertung 0:2 läuft durch die Tabellenrechnung 0x50283 und damit durch 0x4F527 (das gewertete
    /// Ergebnis steht mit Marke 0x5A in der Ergebnisliste und wird beim Lesen aufgelöst, Datei 0x36AAA/0x520A6).
    /// </summary>
    [Fact]
    public void Forfeit_LeagueMatch_UpdatesDayForm()
    {
        var winners = new HashSet<int>();
        for (var seed = 1; seed <= 8; seed++)
        {
            var game = TestGames.Create(seed);
            MatchEngine.EnsureTeamValues(game);
            var club = game.Clubs[game.Managers[0].ClubIndex];
            // Teil 8 (A068): weniger Aufgestellte gibt es nur nach einem Eingriff von Hand; der schaltet die automatische Aufstellung ab (Datei 0x40ADE), sonst stellt der Tageslauf wieder voll auf (0x07C07).
            club.AutoLineupEnabled = false;
            foreach (var p in club.Lineup.Players.Skip(8).ToList())
                club.Lineup.Remove(p);
            foreach (var c in game.Clubs)
                c.TeamValues = c.TeamValues!.Value with { DayForm = 50 };
            TestSeason.PlayMatchDay(game);
            var result = game.Results.Single(r => r.Involves(club.Index));
            Assert.Equal(2, result.Home == club.Index ? result.AwayGoals : result.HomeGoals);
            var opponent = game.Clubs[result.Home == club.Index ? result.Away : result.Home];
            Assert.InRange(club.TeamValues!.Value.DayForm, 47, 50);
            winners.Add(opponent.TeamValues!.Value.DayForm);
        }
        Assert.Contains(winners, f => f > 50);
    }

    // ---- 0x98F8 Reihenwerte aus den Spielern -------------------------------------------------------------------

    /// <summary>[ORIGINAL] 0x98F8: Abwehr = (2 Verteidiger · 80 % + Torhüter · 140 %) / 3, Energiefaktor e/2 + 50.</summary>
    [Fact]
    public void ManagedLineValues_DefenseWithGoalie()
    {
        var squad = Squad(energy: 90);
        var v = MatchEngine.ManagedLineValues(squad, 0, 0, 0, 100, 1, inMatch: true);
        // Technik: Torhüter 70 · 140 % = 98; Verteidiger 90 · 95 % = 85 → · 80 % = 68; 60 · 95 % = 57 → 45.
        Assert.Equal((98 + 68 + 45) / 3, v.Technique);
        // Kondition: (60 + 80 + 70) / 3 = 70; Einsatz 100 → 70 − (100 − 70) / 4 = 63.
        Assert.Equal(63, v.Condition);
        Assert.Equal(50, v.Form);
        Assert.True(squad.HasGoalie);
    }

    [Fact]
    public void ManagedLineValues_AttackOnlyOwnLine_AndEffort()
    {
        var squad = Squad(energy: 60);
        var v = MatchEngine.ManagedLineValues(squad, 0, 1, 0, 60, 1, inMatch: true);
        // Faktor 60/2 + 50 = 80: 64 + 56 + 48 = 168 / 3 = 56; Kondition 60 − (100 − 36) / 4 = 44.
        Assert.Equal(new PartValues(44, 56, 50), v);
        var line1 = MatchEngine.ManagedLineValues(squad, 1, 1, 0, 60, 1, inMatch: true);
        Assert.Equal(99 * 80 / 100 / 3, line1.Technique);
    }

    /// <summary>[ORIGINAL] 0x9BAC/0x9C01: Unterzahl kostet Technik; ohne Torhüter bricht die Abwehr zusammen.</summary>
    [Fact]
    public void ManagedLineValues_UnderplayAndNoGoalie()
    {
        var squad = Squad();
        var even = MatchEngine.ManagedLineValues(squad, 0, 0, 0, 60, 1, inMatch: true);
        var shortHanded = MatchEngine.ManagedLineValues(squad, 0, 0, -1, 60, 1, inMatch: true);
        Assert.Equal(Math.Max(0, even.Technique - (45 - 2 * 7 - 7)), shortHanded.Technique);
        squad.Players[0].Dressed = false;
        var open = MatchEngine.ManagedLineValues(squad, 0, 0, 0, 60, 10, inMatch: true);
        Assert.False(squad.HasGoalie);
        Assert.Equal(new PartValues(0, 0, 0), open);
    }

    /// <summary>[ORIGINAL] 0x952C: Teamwerte = Mittel der Reihen 0–2 ohne Spiel; Managerwert = d/5 + 20 (0 … 40).</summary>
    [Fact]
    public void ManagedTeamValues_ManagerValue()
    {
        var squad = new LiveSquad();
        squad.Players.Add(L(P(PlayerRole.Forward, 90, 30, 50), 0x81));
        squad.Players.Add(L(P(PlayerRole.Forward, 90, 30, 50, 2), 0x81));
        squad.Players.Add(L(P(PlayerRole.Forward, 90, 30, 50, 3), 0x81));
        var (defense, attack, managerValue) = MatchEngine.ManagedTeamValues(squad);
        // Sturm Reihe 0: (90, 30, 50); laufende Summen 60, 60, 60 → d = 180; Abwehr leer → 0.
        Assert.Equal(new PartValues(30, 10, 16), attack);
        Assert.Equal(new PartValues(0, 0, 0), defense);
        Assert.Equal(Math.Clamp(180 / 5 + 20, 0, 40), managerValue);
        Assert.Equal(100, MatchEngine.ManagedTeamValues(squad, nineOrMore: false).ManagerValue);
    }

    // ---- 0x84D7 / 0x87F7 Energie -------------------------------------------------------------------------------

    [Theory]
    [InlineData(100, 90)]   // Belastung 50 → 101 − 6 = 95 → 90
    [InlineData(0, 82)]     // Belastung 150 → 101 − 18 = 83 → 82
    public void PreparedPlayerEnergy_FromFatigue(int energy, int expected) =>
        Assert.Equal(expected, MatchEngine.PreparedPlayerEnergy(P(PlayerRole.Forward, 50, 50, 50, energy: energy)));

    [Theory]
    [InlineData(85, 90)]
    [InlineData(50, 69)]
    public void RecoveredEnergy_PlusTwenty(int energy, int expected) => Assert.Equal(expected, MatchEngine.RecoveredEnergy(energy));

    /// <summary>[ORIGINAL] 0x87F7 Computerverein: Reihe auf dem Eis verliert, die anderen erholen sich (unter 90).</summary>
    [Fact]
    public void DrainEnergy_ComputerLines()
    {
        var team = new LiveTeam { Defense = new(70, 70, 50), Attack = new(70, 70, 50), Energy = 80, Effort = 60 };
        var random = new Random(5);
        OriginalMinutes.DrainEnergy(team, random);
        var check = new Random(5);
        int R(int a, int b) => check.Next(a, b + 1);
        for (var part = 0; part < 2; part++)
        {
            for (var line = 0; line < 4; line++)
            {
                if (line == 0)
                {
                    var d = (R(0, 60) + 75) / 40 + 1;
                    d += 1 >= R(0, 5) ? 4 : 0;
                    d += 1 > R(5, 15) ? 8 : 0;
                    Assert.Equal(80 - d, team.LineEnergy(part, 0));
                }
                else
                {
                    var bonus = R(0, 4) == 0 && 70 > R(0, 100) ? 1 : 0;
                    Assert.Equal(81 + bonus, team.LineEnergy(part, line));
                }
            }
        }
        Assert.Equal([1, 1], team.OnIceMinutes);
    }

    /// <summary>[ORIGINAL] 0x87F7 Managerverein: Energie der Reihen = Summe der Feldspieler (Abwehr + 90) / 3.</summary>
    [Fact]
    public void DrainEnergy_ManagedLineAverages()
    {
        var squad = Squad(energy: 95);
        var team = new LiveTeam { Squad = squad, Managed = true, Effort = 60, Energy = 90 };
        OriginalMinutes.DrainEnergy(team, new Random(1));
        var d0 = squad.Players[1].Energy + squad.Players[2].Energy;
        Assert.Equal((90 + d0) / 3, team.LineEnergy(0, 0));
        Assert.Equal(squad.Players.Skip(3).Take(3).Sum(p => p.Energy) / 3, team.LineEnergy(1, 0));
        Assert.True(squad.Players[3].Energy < 95);
        Assert.Equal(95, squad.Players[6].Energy); // Reihe 1 nicht auf dem Eis, ≥ 90: keine Erholung
        Assert.Equal(90 / 3, team.LineEnergy(0, 1)); // nur der Platzhalter 90
    }

    // ---- 0x8EF2 / 0x91FF / 0x80CB Reihenwechsel ----------------------------------------------------------------

    private static LiveTeam Lines(int mode, params int[] energies)
    {
        var team = new LiveTeam { Energy = 90, LineMode = mode };
        for (var line = 0; line < 4; line++)
        {
            team.SetLineEnergy(0, line, energies[line]);
            team.SetLineEnergy(1, line, energies[line]);
        }
        return team;
    }

    [Fact]
    public void LinesByEnergy_ThreeOrTwoLines()
    {
        var team = Lines(2, 50, 80, 70, 99);
        Assert.Equal((1, 2), OriginalMinutes.LinesByEnergy(team, 0, fourthLine: false, mode: 2));
        Assert.Equal((3, 1), OriginalMinutes.LinesByEnergy(team, 0, fourthLine: true, mode: 2));
        Assert.Equal((1, 0), OriginalMinutes.LinesByEnergy(team, 0, fourthLine: false, mode: 1));
    }

    /// <summary>[ORIGINAL] Modus 2: Wechsel erst nach mehr als 3 Minuten auf dem Eis, dann zur energiestärksten Reihe.</summary>
    [Fact]
    public void ChangeLines_StrongestAfterThreeMinutes()
    {
        var team = Lines(2, 50, 80, 70, 99);
        var opponent = Lines(2, 90, 90, 90, 90);
        team.OnIceMinutes[0] = team.OnIceMinutes[1] = 3;
        OriginalMinutes.ChangeLines(team, opponent);
        Assert.Equal(0, team.DefenseLine);
        team.OnIceMinutes[0] = team.OnIceMinutes[1] = 4;
        OriginalMinutes.ChangeLines(team, opponent);
        Assert.Equal(1, team.DefenseLine);
        Assert.Equal(1, team.AttackLine);
        Assert.Equal([0, 0], team.OnIceMinutes);
    }

    /// <summary>[ORIGINAL] Modus 3: Reihe nach der gegnerischen Reihe des anderen Teils.</summary>
    [Fact]
    public void ChangeLines_ByOpponentLine()
    {
        var team = Lines(3, 50, 80, 70, 99);
        var opponent = Lines(2, 90, 90, 90, 90);
        opponent.AttackLine = 2;
        OriginalMinutes.ChangeLines(team, opponent);
        Assert.Equal(2, team.DefenseLine);
        Assert.Equal(0, team.AttackLine);
    }

    /// <summary>
    /// [ORIGINAL] Modus 4: Minuten-Vorgabe, Reihen mit Vorgabe 0 werden übersprungen. Runde 11 (G6): gezählt wird gegen
    /// die umgerechneten Werte (2/0/1/0 → 3/2/3/0, Datei 0x48E62), vorher gegen die Minuten.
    /// </summary>
    [Fact]
    public void ChangeLines_MinutesPreset()
    {
        var tactics = new Tactics { LineChange = LineChangeMode.MinutesPreset };
        tactics.SetMinutes(0, false, 2);
        tactics.SetMinutes(1, false, 0);
        tactics.SetMinutes(2, false, 1);
        var squad = new LiveSquad { Tactics = tactics };
        var team = new LiveTeam { Energy = 90, Squad = squad, Managed = true, LineChange = LineChangeMode.MinutesPreset };
        var opponent = Lines(2, 90, 90, 90, 90);
        var lines = new List<int>();
        for (var minute = 0; minute < 7; minute++)
        {
            OriginalMinutes.ChangeLines(team, opponent);
            lines.Add(team.DefenseLine);
        }
        Assert.Equal([0, 0, 0, 1, 1, 1, 2], lines);
        Assert.Equal(4, team.LineMode);
    }

    [Fact]
    public void ChangeLines_FullyManual_NoChange()
    {
        var team = new LiveTeam { Energy = 90, Managed = true, LineChange = LineChangeMode.FullyManual, LineMode = 2 };
        team.OnIceMinutes[0] = 10;
        OriginalMinutes.ChangeLines(team, Lines(2, 90, 90, 90, 90));
        Assert.Equal(0, team.DefenseLine);
    }

    /// <summary>[ORIGINAL] 0x80CB: Modi nach Spielstand und Stärke, Torhüter raus in Minute 59 bei einem Tor Rückstand.</summary>
    [Fact]
    public void ComputerLineModes_ScoreStrengthAndGoalie()
    {
        var a = new LiveTeam();
        var b = new LiveTeam();
        OriginalMinutes.ComputerLineModes(a, b, 3, 1, 41, 100, 100);
        Assert.Equal((2, 1), (a.LineMode, b.LineMode));
        OriginalMinutes.ComputerLineModes(a, b, 1, 1, 21, 100, 120);
        Assert.Equal((3, 2), (a.LineMode, b.LineMode));
        OriginalMinutes.ComputerLineModes(a, b, 1, 1, 21, 120, 100);
        Assert.Equal((2, 3), (a.LineMode, b.LineMode));
        OriginalMinutes.ComputerLineModes(a, b, 1, 1, 21, 100, 105);
        Assert.Equal((2, 2), (a.LineMode, b.LineMode));
        OriginalMinutes.ComputerLineModes(a, b, 1, 2, 59, 100, 100);
        Assert.True(a.GoaliePulled);
        Assert.Equal(6, a.Box.Skaters);
        OriginalMinutes.ComputerLineModes(a, b, 2, 2, 61, 100, 100);
        Assert.False(a.GoaliePulled);
        Assert.Equal(5, a.Box.Skaters);
        var managed = new LiveTeam { Managed = true, LineChange = LineChangeMode.TwoLinesOnly, LineMode = 1 };
        OriginalMinutes.ComputerLineModes(managed, b, 1, 2, 59, 100, 100);
        Assert.Equal(1, managed.LineMode);
        Assert.False(managed.GoaliePulled);
    }

    // ---- 0x378C5 Strafspieler -----------------------------------------------------------------------------------

    [Fact]
    public void PenalizedPlayer_OnlyOnIce_AndNoneForSmallSquads()
    {
        var squad = Squad();
        var team = new LiveTeam { Squad = squad, Managed = true };
        for (var seed = 0; seed < 200; seed++)
        {
            var p = OriginalMinutes.PenalizedPlayer(team, new Random(seed));
            if (p is not null)
                Assert.Equal(0, p.Line);
        }
        var small = new LiveSquad();
        small.Players.AddRange(squad.Players.Take(5));
        Assert.Null(OriginalMinutes.PenalizedPlayer(new LiveTeam { Squad = small }, new Random(1)));
    }

    /// <summary>[ORIGINAL] 0x35BFD/0x35691: Der bestrafte Feldspieler geht vom Eis und kommt beim Freiwerden zurück.</summary>
    [Fact]
    public void Penalty_TakesPlayerOffIce_UntilReleased()
    {
        for (var seed = 0; seed < 500; seed++)
        {
            var squad = Squad();
            var team = new LiveTeam { Squad = squad, Managed = true, Effort = 100, PenaltyManagerValue = 40 };
            var opponent = new LiveTeam { Effort = 0 };
            var events = new List<MatchEvent>();
            if (OriginalMinutes.Penalty(team, opponent, 0, 1, chained: true, events, new Random(seed)) == 0)
                continue;
            var e = Assert.Single(events);
            if (e.Player is null)
                continue;
            var off = squad.Players.Single(p => p.Player == e.Player);
            Assert.False(off.Dressed);
            for (var minute = 0; minute < e.PenaltyMinutes; minute++)
                team.Box.Tick();
            Assert.True(off.Dressed);
            return;
        }
        Assert.Fail("keine Strafe mit Spieler gezogen");
    }

    // ---- 0x939A Taktik mit Energie, Anschluss -------------------------------------------------------------------

    /// <summary>
    /// Regressionstest: Managervereine spielen in der Managerpaarung mit den Werten ihrer Spieler (vorher Teamwert aus der
    /// Datenbankstärke) – bessere Technik der ersten Reihe erhöht die Stärke, die zweite Reihe wirkt beim Anpfiff nicht.
    /// </summary>
    [Fact]
    public void ManagedClub_StrengthFromPlayersOnIce()
    {
        var game = TestGames.Create(3);
        var club = game.Clubs[game.Managers[0].ClubIndex];
        var opponent = game.Clubs.First(c => c != club && c.Players.Count > 0);
        int Normal()
        {
            var team = MatchEngine.LiveTeamOf(game, club, opponent, managed: true, managerMatch: true);
            var opp = MatchEngine.LiveTeamOf(game, opponent, club, managed: false, managerMatch: true);
            return OriginalMinutes.Strengths(team, opp, 1).Normal;
        }
        var before = Normal();
        foreach (var p in club.Lineup.Block(1))
            p.Technique = Player.MaxValue;
        Assert.Equal(before, Normal());
        foreach (var p in club.Lineup.Block(0))
            p.Technique = Player.MaxValue;
        Assert.True(Normal() > before);
    }

    /// <summary>
    /// Mehrsaisonprobe (synthetisch, Modus 4, passiv und aktiv): Managerverein spielt über vier Saisonwechsel mit seinen
    /// Spielern; Ergebnisse plausibel (Tore je Spiel, keine Kantersiege durch fehlenden Torhüter), Strafen nur für Spieler
    /// der Aufstellung.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FourSeasonChanges_ManagedMatchesPlausible(bool active)
    {
        var game = GameModeTests.Create(3, true, true, 2, 30);
        long goals = 0, games = 0, maxGoals = 0;
        RngTwinProbeTests.Run(game, active, beforeChange: g =>
        {
            foreach (var r in g.Results.Where(r => g.Managers.Any(m => r.Involves(m.ClubIndex))))
            {
                goals += r.HomeGoals + r.AwayGoals;
                games++;
                maxGoals = Math.Max(maxGoals, Math.Max(r.HomeGoals, r.AwayGoals));
            }
            foreach (var m in g.Managers)
                Assert.All(g.Clubs[m.ClubIndex].Players, p => Assert.True(p.Penalties2 + p.Penalties5 <= p.Games * 4));
        });
        Assert.True(games > 100);
        Assert.InRange(goals / (double)games, 4.0, 11.0);
        Assert.True(maxGoals < 20);
    }
}

/// <summary>
/// Testaufbau: Managervereine spielen seit 0x98F8 mit den Werten ihrer Spieler. Tests, die einen Managerverein mit der
/// Spielstärke seiner Datenbank voraussetzen (Tabellenplatz als Vorbedingung), setzen die Spielerwerte darauf.
/// Nur als Vorbedingung erlaubt, nie als Aussage über die Spielstärke: Im Original hängen die Spielerwerte nur von
/// Start-Liga und Start-Stärke ab (DGROUP 0x4B42), nicht von der Datenbank (R8W3I-15, docs/spielberechnung.md §14).
/// </summary>
internal static class ManagedSquads
{
    public static void PlayAtDatabaseStrength(GameState game)
    {
        foreach (var m in game.Managers)
        {
            var club = game.Clubs[m.ClubIndex];
            var value = Math.Min(Player.MaxValue, club.Strength);
            foreach (var p in club.Players)
            {
                p.Condition = p.Technique = value;
                p.Form = 50;
            }
        }
    }

    /// <summary>
    /// Vorbedingung „Managerverein wird Bundestrainer“ (unter den ersten fünf der Bundesliga): Spielerwerte 99. Seit der
    /// Wanderung der Teamwerte (0x9D1B, spielberechnung-komplett, #204) liegen die Computervereine der Bundesliga zwischen
    /// 69 und 94 (in der Saison bis 99); mit Datenbankstärke erreichte Verein 2 die ersten fünf in 10 von 12 Seeds, mit
    /// 99 in 12 von 12. Nur als Vorbedingung erlaubt.
    /// </summary>
    public static void PlayAtTopStrength(GameState game) => PlayAtTopStrength(game, [.. Enumerable.Range(0, game.Managers.Count)]);

    /// <summary>
    /// Nur die Vereine der genannten Manager (Spielberechnung Teil 5: mit dem Penaltyschießen 0x3901B ändert sich der
    /// Zufallsverbrauch, zwei gleich starke Managervereine tauschen dann die Tabellenplätze).
    /// </summary>
    public static void PlayAtTopStrength(GameState game, params int[] managers)
    {
        foreach (var m in managers.Select(i => game.Managers[i]))
            foreach (var p in game.Clubs[m.ClubIndex].Players)
            {
                p.Condition = p.Technique = Player.MaxValue;
                p.Form = 50;
            }
    }
}
