using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Runde 11, Spur wettbewerbe-spiel Teil 5 (Inventur P2 Nr. 1, G6): Minuten-Vorgabe nach Datei 0x48E62/0x4889F,
/// Einsatz bei „MANUELL“ nach Datei 0x939A, kein Einsatz-Zuschlag auf die Spielstärke, „eingesetzt“ im Spiel
/// (A168, A176–A178, A182, A090).
/// </summary>
public class TaktikTeil5Tests
{
    /// <summary>[ORIGINAL] Datei 0x4889F–0x488CE: je Teil zusammen höchstens 20 Minuten (0x14).</summary>
    [Fact]
    public void Minutes_AtMostTwentyPerPart()
    {
        var t = new Tactics();
        t.SetMinutes(0, false, 15);
        t.SetMinutes(1, false, 10);
        Assert.Equal(5, t.Minutes(1, false));
        t.SetMinutes(2, false, 9);
        Assert.Equal(0, t.Minutes(2, false));
        t.SetMinutes(0, true, 25);
        Assert.Equal(Tactics.MaxMinutesPerPart, t.Minutes(0, true)); // der andere Teil zählt nicht mit
        Assert.Equal(15, t.Minutes(0, false));
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x48EEA–0x4901D: Werte ⌊Min/2⌋ | (Min &amp; 1) (Reihe 4: ⌊Min/2⌋), dann von Reihe 4 rückwärts auf
    /// Summe 8 (−1 nur bei Werten &gt; 1; +1 bei Werten ≤ 1 nicht für Reihe 4). Beispiele aus re-geld.md (G6).
    /// </summary>
    [Theory]
    [InlineData(7, 7, 6, 0, 3, 3, 2, 0)]
    [InlineData(5, 5, 5, 5, 3, 2, 2, 1)]
    [InlineData(0, 0, 0, 0, 2, 3, 3, 0)]
    [InlineData(20, 0, 0, 0, 8, 0, 0, 0)]
    [InlineData(2, 0, 1, 0, 3, 2, 3, 0)]
    public void MinuteQuota_SumsToEight(int m0, int m1, int m2, int m3, int q0, int q1, int q2, int q3)
    {
        var t = new Tactics();
        int[] minutes = [m0, m1, m2, m3];
        for (var line = 0; line < 4; line++)
            t.SetMinutes(line, forwards: true, minutes[line]);
        Assert.Equal([q0, q1, q2, q3], Enumerable.Range(0, 4).Select(l => t.MinuteQuota(l, forwards: true)));
        Assert.Equal(8, Enumerable.Range(0, 4).Sum(l => t.MinuteQuota(l, forwards: false)));
    }

    /// <summary>[ORIGINAL] Datei 0x9140: Modus 4 zählt gegen die umgerechneten Werte (Summe 8), nicht gegen die Minuten.</summary>
    [Fact]
    public void ChangeLines_MinutesPreset_UsesQuota()
    {
        var tactics = new Tactics { LineChange = LineChangeMode.MinutesPreset };
        tactics.SetMinutes(0, false, 7);
        tactics.SetMinutes(1, false, 7);
        tactics.SetMinutes(2, false, 6); // 3/3/2/0
        var team = new LiveTeam
        {
            Energy = 90, Squad = new LiveSquad { Tactics = tactics }, Managed = true, LineChange = LineChangeMode.MinutesPreset,
        };
        var opponent = new LiveTeam { Energy = 90 };
        var lines = new List<int>();
        for (var minute = 0; minute < 10; minute++)
        {
            OriginalMinutes.ChangeLines(team, opponent);
            lines.Add(team.DefenseLine);
        }
        Assert.Equal([0, 0, 0, 1, 1, 1, 1, 2, 2, 2], lines);
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x9516–0x9523: bei „MANUELL“ (DGROUP 0xA6FA + Manager = 2) schreibt 0x939A den Einsatz nicht –
    /// er bleibt beim Wert vor dem Spiel (Grundwert + „Unentschieden“, Datei 0x48E62 Schritt 4), auch ohne Energieabzug;
    /// ebenso der Angriff (Datei 0x94E2, DGROUP 0xA706 + Manager ≥ 2).
    /// </summary>
    [Fact]
    public void Manual_KeepsEffortAndAttack_DuringMatch()
    {
        var tactics = new Tactics { EffortMode = TacticMode.Manual, AttackMode = TacticMode.Manual };
        var team = new LiveTeam { Energy = 30, Tactics = tactics, Effort = 77, AttackLevel = 5 };
        OriginalMinutes.FollowTactics(team, goalDifference: -4);
        Assert.Equal(77, team.Effort);
        Assert.Equal(5, team.AttackLevel);

        var semi = Tactics.LegacyStart(); // Startwerte vor A180 (Runde 14)
        semi.EffortMode = TacticMode.Semi;
        semi.AttackMode = TacticMode.Semi;
        var other = new LiveTeam { Energy = 30, Tactics = semi, Effort = 77, AttackLevel = 5 };
        OriginalMinutes.FollowTactics(other, goalDifference: 0);
        // Grundwert 60 + Unentschieden 20 − (50 − 30) = 60 (Energieabzug Datei 0x9490–0x94A7).
        Assert.Equal(60, other.Effort);
        Assert.Equal((0 + 6) & Tactics.MaxAttack, other.AttackLevel);
    }

    /// <summary>
    /// A178/A182: Der Einsatz wirkt im Original nur über Vereinsfeld 0x26 in 0x368B1/0x977F (Spielberechnung), nicht als
    /// Zuschlag auf eine Spielstärke; die Stärke aus der Aufstellung bleibt bei jedem Einsatz gleich.
    /// </summary>
    [Fact]
    public void Effort_DoesNotShiftMatchStrength()
    {
        var game = TestGames.Create();
        var club = game.Clubs[2];
        var before = club.MatchStrength;
        club.Tactics.EffortMode = TacticMode.Semi;
        for (var b = 0; b < Tactics.Blocks; b++)
            club.Tactics.SetEffort(b, ScoreSituation.Base, 0);
        Assert.Equal(before, club.MatchStrength);
    }

    /// <summary>
    /// A168: Im Kader der Managerpaarung ist nur eingesetzt, wer im Verein eingesetzt ist (Kaderplatz +0x0A, Datei 0x98F8,
    /// 0x87F7, 0x388F6) – nicht jeder spielfähige Spieler.
    /// </summary>
    [Fact]
    public void LiveSquad_DressedFollowsClub()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Trainer", Head = 0, ClubIndex = 2 });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 3);
        var club = game.Clubs[2];
        var reserve = club.Dressed.First(p => !club.Lineup.Contains(p));
        Assert.Equal(LineupResult.Undressed, club.Undress(reserve));
        Assert.True(reserve.Available);

        var squad = LiveSquad.Create(club, 2, club.Tactics);
        Assert.False(squad.Players.Single(p => p.Player == reserve).Dressed);
        Assert.All(squad.Players.Where(p => club.Lineup.Contains(p.Player)), p => Assert.True(p.Dressed));
    }
}
