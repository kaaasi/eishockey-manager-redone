using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur livespiel-kern, Teil A3 (L6): Eingriff in die laufende Managerpaarung als Befehle (<c>docs/analyse/re-livespiel.md</c>
/// §10). Vor dem Teil rot: es gab keine Befehle (<c>MatchIntervention</c> fehlte, Build-Fehler).
/// </summary>
public sealed class LivespielKernInterventionTests
{
    private static (GameState Game, LiveTeam Managed, LiveTeam Computer) Teams(int seed = 3)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "M", Head = 0, ClubIndex = 2 });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, seed);
        var home = game.Clubs[2];
        var away = game.Clubs.First(c => c.Index != 2 && c.League == home.League);
        return (game, MatchEngine.LiveTeamOf(game, home, away, managed: true, managerMatch: true),
            MatchEngine.LiveTeamOf(game, away, home, managed: false, managerMatch: true));
    }

    /// <summary>Abnahme A3: Rechnung der Minute mit geändertem Einsatz – die Stärken der Minute (0x368B1) folgen dem neuen Wert.</summary>
    [Fact]
    public void Effort_ChangesMinuteStrengths()
    {
        var (_, team, opponent) = Teams();
        Assert.True(MatchIntervention.SetEffort(team, 0));
        var low = OriginalMinutes.Strengths(team, opponent, 10);
        Assert.True(MatchIntervention.SetEffort(team, 100));
        var high = OriginalMinutes.Strengths(team, opponent, 10);
        Assert.Equal(100, team.Effort);
        Assert.True(high.Normal > low.Normal, $"{high} vs {low}");
    }

    /// <summary>Abnahme A3: Rechnung mit geänderter Angriffsstufe – die Chancen je Minute (0x9F5C) hängen von Bits 4–6 ab.</summary>
    [Fact]
    public void Attack_ChangesMinuteChances()
    {
        var (_, team, opponent) = Teams();
        int Chances(int attack)
        {
            Assert.True(MatchIntervention.SetAttack(team, attack));
            var n = 0;
            for (var seed = 0; seed < 400; seed++)
            {
                var flags = OriginalMatch.MinuteChances(
                    new MatchSide(60, team.Box.Skaters, team.AttackLevel), new MatchSide(60, opponent.Box.Skaters, opponent.AttackLevel),
                    10, 20, false, false, new Random(seed));
                n += flags & 1;
            }
            return n;
        }
        Assert.NotEqual(Chances(0), Chances(7));
        Assert.Equal(7, team.AttackLevel);
    }

    /// <summary>Abnahme A3: Rechnung mit anderer Reihe auf dem Eis (Vereinsfeld 0x2E/0x30) – Reihenwerte aus 0x98F8.</summary>
    [Fact]
    public void Line_ChangesMinuteStrengths()
    {
        var (_, team, opponent) = Teams();
        var before = OriginalMinutes.Strengths(team, opponent, 10);
        var changed = false;
        for (var line = 1; line < 3 && !changed; line++)
        {
            Assert.True(MatchIntervention.SetLine(team, 1, line));
            Assert.Equal(line, team.AttackLine);
            changed = OriginalMinutes.Strengths(team, opponent, 10) != before;
        }
        Assert.True(changed);
        Assert.True(MatchIntervention.SetLine(team, 0, 2));
        Assert.Equal(2, team.DefenseLine);
    }

    /// <summary>Abnahme A3: vierte Reihe ohne eingesetzten Spieler → keine Änderung ([ORIGINAL] 0x38F80–0x38F99 über 0x08E5F).</summary>
    [Fact]
    public void FourthLine_WithoutPlayer_NoChange()
    {
        var (_, team, _) = Teams();
        for (var part = 0; part < 2; part++)
        {
            var fourth = team.Squad!.HasFourthLine(part);
            var before = part == 0 ? team.DefenseLine : team.AttackLine;
            Assert.Equal(fourth, MatchIntervention.SetLine(team, part, 3));
            Assert.Equal(fourth ? 3 : before, part == 0 ? team.DefenseLine : team.AttackLine);
        }
        foreach (var p in team.Squad!.Players.Where(p => (p.Position & 0x7F) == 4))
            p.Dressed = false;
        team.AttackLine = 1;
        Assert.False(MatchIntervention.SetLine(team, 1, 3));
        Assert.Equal(1, team.AttackLine);
    }

    /// <summary>[ORIGINAL] Nur der Verein mit Manager (Datei 0x38E5F/0x38E7B); ungültige Werte ändern nichts.</summary>
    [Fact]
    public void ComputerTeamAndInvalidValues_NoChange()
    {
        var (_, team, computer) = Teams();
        var effort = computer.Effort;
        Assert.False(MatchIntervention.SetEffort(computer, 40));
        Assert.False(MatchIntervention.SetAttack(computer, 3));
        Assert.False(MatchIntervention.SetLine(computer, 0, 1));
        Assert.Equal(effort, computer.Effort);
        Assert.False(MatchIntervention.SetEffort(team, 101));
        Assert.False(MatchIntervention.SetAttack(team, 8));
        Assert.False(MatchIntervention.SetLine(team, 2, 0));
        Assert.False(MatchIntervention.SetLine(team, 0, 4));
    }

    /// <summary>
    /// Abnahme A3: Das Nachführen 0x939A überschreibt den Eingriff in gerader Minute – außer bei „MANUELL“ (A176, Datei 0x9516,
    /// 0x94E2).
    /// </summary>
    [Fact]
    public void FollowTactics_OverwritesUnlessManual()
    {
        var (game, team, _) = Teams();
        team.Tactics!.EffortMode = TacticMode.Auto;
        team.Tactics.AttackMode = TacticMode.Auto;
        MatchIntervention.SetEffort(team, 0);
        MatchIntervention.SetAttack(team, 7);
        OriginalMinutes.FollowTactics(team, 0);
        Assert.NotEqual(0, team.Effort);
        Assert.NotEqual(7, team.AttackLevel);

        team.Tactics.EffortMode = TacticMode.Manual;
        team.Tactics.AttackMode = TacticMode.Manual;
        MatchIntervention.SetEffort(team, 8);
        MatchIntervention.SetAttack(team, 7);
        OriginalMinutes.FollowTactics(team, 0);
        Assert.Equal((8, 7), (team.Effort, team.AttackLevel));
        Assert.NotNull(game);
    }

    /// <summary>[ORIGINAL] 0x5434C: nach Aufstellung/Taktik im Spiel Feldspieler neu zählen (0x38A36); Taste → Modus.</summary>
    [Fact]
    public void AfterLineup_RecountsSkaters()
    {
        var (_, team, _) = Teams();
        MatchEngine.RecountSkaters(team);
        var skaters = team.Box.Skaters;
        team.Box.Skaters = 1;
        MatchIntervention.AfterLineup(team);
        Assert.Equal(skaters, team.Box.Skaters);
        Assert.Equal(InterventionMode.Lineup, MatchIntervention.ModeFor(rightButton: false));
        Assert.Equal(InterventionMode.Tactics, MatchIntervention.ModeFor(rightButton: true));
    }
}
