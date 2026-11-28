using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur <c>feldspieler-sechs</c>, Runde 23, Teil 2 (#939, A937): sechs Feldspieler ohne Torhüter in der Spielrechnung und im Eingriff
/// während des Live-Spiels. [ORIGINAL] em.exe Datei 0x40EC4–0x40EFF und 0x413C9–0x414B4 (Aufstellung, ohne Blick auf den Kennwert
/// „im Spiel“), Neuzählung 0x38A36 mit 0x388F6, Reihenwerte 0x98F8–0x9C63 (Torhüter nur über Gruppe 0x04180 = 0 und +0x15 = 1,
/// Datei 0x9966–0x999A). Synthetisch, ohne Originaldaten.
/// </summary>
public class SixthSkaterInMatchTests
{
    private static int s_nextId = 93_960;

    private static Player NewPlayer(PlayerRole role, string name)
    {
        var p = new Player { Name = name, Role = role, Condition = 70, Technique = 70, Form = 50, Age = 25, ContractYears = 2 };
        typeof(Player).GetProperty(nameof(Player.Id))!.SetValue(p, Interlocked.Increment(ref s_nextId));
        return p;
    }

    /// <summary>2 Torhüter, 8 Verteidiger, 12 Stürmer; „A“ stellt drei Blöcke auf, Einstellung aus.</summary>
    private static Club NewClub(bool withGoalie)
    {
        var club = new Club { Index = 3, Name = "Test", Strength = 70 };
        for (var i = 0; i < 2; i++) club.Players.Add(NewPlayer(PlayerRole.Goalkeeper, $"T{i}"));
        for (var i = 0; i < 8; i++) club.Players.Add(NewPlayer(PlayerRole.Defender, $"V{i}"));
        for (var i = 0; i < 12; i++) club.Players.Add(NewPlayer(PlayerRole.Forward, $"S{i}"));
        club.RenumberPlayers();
        club.AutoLineup();
        club.AutoLineupEnabled = false;
        if (!withGoalie)
            club.ClearLineupSlot(LineupSlot.Goalkeeper);
        return club;
    }

    private static Player Free(Club club, PlayerRole role) =>
        club.Players.Where(p => p.Role == role && !club.Lineup.Contains(p)).OrderBy(p => !club.IsDressed(p)).First();

    /// <summary>Spielkader wie in der Managerpaarung (MatchEngine.Live: Freiwerden 0x35691 zählt neu).</summary>
    private static LiveTeam NewTeam(Club club)
    {
        var team = new LiveTeam
        {
            Managed = true,
            Squad = LiveSquad.Create(club, 2, club.Tactics),
            InjuryManager = 0,
            Effort = 60,
            Energy = 90,
        };
        team.Box.Released = () => MatchEngine.RecountSkaters(team);
        return team;
    }

    /// <summary>Verein ohne Torhüter mit sechstem Feldspieler, im Spiel aufgestellt und übernommen.</summary>
    private static (Club Club, LiveTeam Team, Player Sixth) SixSkatersInMatch()
    {
        var club = NewClub(withGoalie: false);
        var team = NewTeam(club);
        int? line = 0;
        var sixth = Free(club, PlayerRole.Defender);
        Assert.Equal(LineupResult.Assigned, club.ClickPlayerInMatch(sixth, ref line, right: false));
        LiveLineup.Apply(team, club);
        return (club, team, sixth);
    }

    /// <summary>
    /// A937: Im Spiel öffnet ein Klick in die volle Reihe 1 ohne Torhüter den sechsten Feldspielerplatz wie außerhalb (Datei 0x413C9–0x414B4,
    /// Kennwert nur an 0x41533 weitergereicht, Datei 0x413E8); der Spieler ist eingesetzt (+0x0A = 1). Vorher: „Kein / Platz mehr !“.
    /// </summary>
    [Fact]
    public void InMatchClick_Line1FullWithoutGoalie_OpensSixthSkaterSlot()
    {
        var club = NewClub(withGoalie: false);
        int? line = 0;
        var sixth = Free(club, PlayerRole.Defender);

        Assert.Equal(LineupResult.Assigned, club.ClickPlayerInMatch(sixth, ref line, right: false));

        Assert.Same(sixth, club.Lineup.SixthSkater);
        Assert.True(club.Lineup.SixthSkaterOpen);
        Assert.True(club.IsDressed(sixth));
        Assert.Equal(LineupResult.NoRoom, club.ClickPlayerInMatch(Free(club, PlayerRole.Forward), ref line, right: false));
    }

    /// <summary>
    /// A937: Rechtsklick auf einen Nichtaufgestellten geht im Spiel in den Aufstellweg (Datei 0x40F35–0x40F42 → 0x41273) und öffnet den
    /// Platz ebenso, auch für einen Stürmer (+0x15 = 1).
    /// </summary>
    [Fact]
    public void InMatchRightClick_ForwardOpensSixthSkaterSlot()
    {
        var club = NewClub(withGoalie: false);
        int? line = 0;
        var forward = Free(club, PlayerRole.Forward);

        Assert.Equal(LineupResult.Assigned, club.ClickPlayerInMatch(forward, ref line, right: true));

        Assert.Same(forward, club.Lineup.SixthSkater);
        Assert.Equal(1, club.Lineup.PositionOf(forward));
    }

    /// <summary>
    /// A937: „Schon 6 Feldspieler / auf dem Eis !“ auch im Spiel (Datei 0x40EC4–0x40EFF prüft den Kennwert nicht): Linksklick auf einen
    /// Torhüter bei gewählter Torhüterreihe und offenem Platz stellt ihn nicht auf. Vorher: der Torhüter kam ins Tor.
    /// </summary>
    [Fact]
    public void InMatchClick_GoalieWhileSixthOpen_SixSkatersMessage()
    {
        var club = NewClub(withGoalie: false);
        int? line = 0;
        var sixth = Free(club, PlayerRole.Defender);
        Assert.Equal(LineupResult.Assigned, club.ClickPlayer(sixth, ref line, right: false)); // vor dem Spiel geöffnet
        line = Lineup.GoalkeeperLine;
        var goalie = Free(club, PlayerRole.Goalkeeper);

        Assert.Equal(LineupResult.SixSkaters, club.ClickPlayerInMatch(goalie, ref line, right: false));

        Assert.Null(club.Lineup[LineupSlot.Goalkeeper]);
        Assert.Same(sixth, club.Lineup.SixthSkater);
    }

    /// <summary>
    /// [ORIGINAL] Neuzählung 0x38A36 nach dem Eingriff (Datei 0x5438D–0x54419): kein Torhüter (0x388F6 Modus 0), Abwehrreihe 0, drei
    /// Verteidiger (+0x15 = 1) und drei Stürmer; Grenze 6 (d + f = 6), also 0xCC37 = 3/3, Vereinsfeld 0x32 = 6, 0x2C Bit 3 gesetzt.
    /// </summary>
    [Fact]
    public void AfterIntervention_SixSkatersCounted_GoaliePulled()
    {
        var (_, team, sixth) = SixSkatersInMatch();
        var squad = team.Squad!;

        Assert.Equal(1, squad.Players.Single(p => ReferenceEquals(p.Player, sixth)).Position);
        Assert.Equal([3, 3], squad.SkatersPerPart);
        Assert.Equal(0, team.DefenseLine);
        Assert.Equal(6, team.Box.Skaters);
        Assert.True(team.GoaliePulled);
    }

    /// <summary>
    /// [ORIGINAL] 0x38A36 mit einem Spieler auf der Strafbank: ohne Torhüter und d + f = 5 &lt; 6 gilt die Grenze 5, Überschuss 1 →
    /// f = 3 − 1 = 2; d + f = 5, Bit 3 aus.
    /// </summary>
    [Fact]
    public void SixSkaters_OnePenalized_FiveOnIce_GoalieNotPulled()
    {
        var (_, team, _) = SixSkatersInMatch();
        var boxed = team.Squad!.Players.First(p => p.Position == 0x81 && p.Dressed);
        boxed.Dressed = false;
        team.Box.Occupants[0] = boxed;

        MatchEngine.RecountSkaters(team);

        Assert.Equal([3, 2], team.Squad.SkatersPerPart);
        Assert.Equal(5, team.Box.Skaters);
        Assert.False(team.GoaliePulled);
    }

    /// <summary>
    /// [ORIGINAL] Reihenwerte 0x98F8–0x9C63 im Spiel: der sechste Feldspieler zählt als dritter Feldspieler der Abwehr (Grenze 0xCC37 = 3),
    /// DGROUP 0xDE16 bleibt 0 (kein Torhüter, Datei 0x9920/0x999A) und das Tor ist offen (0xA83E, <see cref="LiveTeam.OpenNet"/>).
    /// Ohne ihn zählen nur zwei: Kondition kleiner.
    /// </summary>
    [Fact]
    public void LineValues_SixthSkaterCountsInDefence_OpenNet()
    {
        var (club, team, sixth) = SixSkatersInMatch();
        var opponent = new LiveTeam { Defense = new(50, 50, 50), Attack = new(50, 50, 50), Energy = 90, Effort = 60 };
        var squad = team.Squad!;

        var withSixth = MatchEngine.ManagedLineValues(squad, 0, 0, 1, 60, 10, inMatch: true);
        Assert.False(squad.HasGoalie);
        OriginalMinutes.Strengths(team, opponent, 10);
        Assert.True(team.OpenNet);

        club.ClearLineupSlotInMatch(LineupSlot.SixthSkater);
        LiveLineup.Apply(team, club);
        Assert.Equal(2, squad.SkatersPerPart[0]);
        var without = MatchEngine.ManagedLineValues(squad, 0, 0, 0, 60, 10, inMatch: true);
        Assert.True(withSixth.Condition > without.Condition, $"{withSixth.Condition} ≤ {without.Condition}");
        Assert.True(club.IsDressed(sixth));
    }

    /// <summary>
    /// Ganzes Managerspiel (0x37DCF–0x380A4) mit sechs Feldspielern ohne Torhüter: je Minute zählt 0x38B8F neu (6 auf dem Eis, Bit 3),
    /// jeder gegnerische Abschluss ist ein Tor (0xA83E). Über dieselben Saaten fallen gegen sechs Feldspieler mehr Gegentore als gegen
    /// fünf mit Torhüter.
    /// </summary>
    [Fact]
    public void ManagerMatch_SixSkaters_RunsAndConcedesMore()
    {
        int conceded6 = 0, concededGoalie = 0;
        for (var seed = 0; seed < 40; seed++)
        {
            var (_, team, _) = SixSkatersInMatch();
            var report = PlayAgainst(team, seed);
            Assert.Equal(team.Box.Skaters == 6, team.GoaliePulled);
            Assert.True(team.OpenNet);
            conceded6 += report.AwayGoals;

            var withGoalie = NewTeam(NewClub(withGoalie: true));
            concededGoalie += PlayAgainst(withGoalie, seed).AwayGoals;
            Assert.False(withGoalie.OpenNet);
        }
        Assert.True(conceded6 > concededGoalie, $"{conceded6} ≤ {concededGoalie}");
    }

    private static MatchReport PlayAgainst(LiveTeam home, int seed)
    {
        var away = new LiveTeam { Defense = new(60, 60, 60), Attack = new(60, 60, 60), Energy = 90, Effort = 60 };
        return OriginalMinutes.Play(home, away, new MatchRules(ManagerMatch: true), new Random(seed));
    }
}
