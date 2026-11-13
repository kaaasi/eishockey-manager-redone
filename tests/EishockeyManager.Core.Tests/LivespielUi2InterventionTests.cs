using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur livespiel-ui-2, B3 (Fortsetzung 1): Aufstellung im Spiel (Kennwert DGROUP 0x2D6 + Manager, <c>docs/analyse/re-livespiel.md</c>
/// §15) und Übernahme in den Spielkader. Vor dem Teil rot: <see cref="Club.ClickPlayerInMatch"/>,
/// <see cref="Club.ClearLineupSlotInMatch"/> und <see cref="LiveLineup"/> fehlten (Build-Fehler); mit den Regeln ohne Spiel
/// (<see cref="Club.ClickPlayer"/>) verliert ein herausgenommener Spieler „eingesetzt“ und der 23. wird abgewiesen.
/// Synthetisch, ohne Originaldaten.
/// </summary>
public sealed class LivespielUi2InterventionTests
{
    private static int s_nextId = 60_000;

    private static Player NewPlayer(PlayerRole role, string name)
    {
        var p = new Player { Name = name, Role = role, Condition = 70, Technique = 70, Form = 50, Age = 25, ContractYears = 2 };
        typeof(Player).GetProperty(nameof(Player.Id))!.SetValue(p, Interlocked.Increment(ref s_nextId));
        return p;
    }

    /// <summary>2 Torhüter, 9 Verteidiger, 16 Stürmer: „A“ stellt auf und setzt 22 ein, fünf bleiben draußen.</summary>
    private static Club NewClub()
    {
        var club = new Club { Index = 3, Name = "Test", Strength = 70 };
        for (var i = 0; i < 2; i++) club.Players.Add(NewPlayer(PlayerRole.Goalkeeper, $"T{i}"));
        for (var i = 0; i < 9; i++) club.Players.Add(NewPlayer(PlayerRole.Defender, $"V{i}"));
        for (var i = 0; i < 16; i++) club.Players.Add(NewPlayer(PlayerRole.Forward, $"S{i}"));
        club.RenumberPlayers();
        club.AutoLineup();
        club.AutoLineupEnabled = false;
        return club;
    }

    private static Player Undressed(Club club, PlayerRole role) =>
        club.Players.First(p => p.Role == role && !club.IsDressed(p));

    /// <summary>[ORIGINAL] Datei 0x4115F: Linksklick auf einen Aufgestellten nimmt ihn heraus, im Spiel bleibt er eingesetzt.</summary>
    [Fact]
    public void LeftClickOnPlaced_InMatch_TakesOut_StaysDressed()
    {
        var club = NewClub();
        var a = club.Lineup[LineupSlot.Forward(1, 2)]!;
        int? line = 0;
        Assert.Equal(LineupResult.TakenOut, club.ClickPlayerInMatch(a, ref line, right: false));
        Assert.Equal(1, line);
        Assert.Null(club.Lineup[LineupSlot.Forward(1, 2)]);
        Assert.True(club.IsDressed(a));
    }

    /// <summary>[ORIGINAL] Datei 0x40F35 → 0x41287: Rechtsklick auf einen Aufgestellten ändert im Spiel nichts.</summary>
    [Fact]
    public void RightClickOnPlaced_InMatch_Unchanged()
    {
        var club = NewClub();
        var a = club.Lineup[LineupSlot.Defender(0, 0)]!;
        int? line = 2;
        Assert.Equal(LineupResult.Unchanged, club.ClickPlayerInMatch(a, ref line, right: true));
        Assert.Same(a, club.Lineup[LineupSlot.Defender(0, 0)]);
        Assert.Equal(2, line);
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x40E2A (Grenze 22 nur ohne Spiel) und 0x40F35 (rechte Taste im Spiel = Aufstellweg): Ein nicht
    /// Eingesetzter kommt als 23. in die gewählte Reihe – mit beiden Tasten.
    /// </summary>
    [Fact]
    public void Placing_InMatch_NoLimit22_RightClickPlacesLikeLeft()
    {
        var club = NewClub();
        Assert.Equal(Club.MaxDressed, club.DressedCount);
        var d = Undressed(club, PlayerRole.Forward);
        int? line = 3;
        var outside = line;
        Assert.Equal(LineupResult.RosterFull, club.ClickPlayer(d, ref outside, right: false)); // ohne Spiel abgewiesen
        Assert.Equal(LineupResult.Assigned, club.ClickPlayerInMatch(d, ref line, right: true));
        Assert.Equal(3, club.Lineup.FindSlot(d)!.Value.Block);
        var f = Undressed(club, PlayerRole.Forward);
        Assert.NotSame(d, f);
        Assert.Equal(LineupResult.Assigned, club.ClickPlayerInMatch(f, ref line, right: false));
        Assert.Equal(Club.MaxDressed + 2, club.DressedCount);
        int? none = null;
        var g = club.Players.First(p => p.Role == PlayerRole.Forward && club.IsDressed(p) && !club.Lineup.Contains(p));
        Assert.Equal(LineupResult.Unchanged, club.ClickPlayerInMatch(g, ref none, right: true));
        Assert.Equal(LineupResult.NoLineSelected, club.ClickPlayerInMatch(g, ref none, right: false));
    }

    /// <summary>[ORIGINAL] Datei 0x40CFD: Platz leeren im Spiel – der Spieler bleibt eingesetzt (ohne Spiel heraus).</summary>
    [Fact]
    public void ClearSlot_InMatch_StaysDressed()
    {
        var club = NewClub();
        var a = club.Lineup[LineupSlot.Forward(2, 0)]!;
        club.ClearLineupSlotInMatch(LineupSlot.Forward(2, 0));
        Assert.Null(club.Lineup[LineupSlot.Forward(2, 0)]);
        Assert.True(club.IsDressed(a));
        var b = club.Lineup[LineupSlot.Forward(2, 1)]!;
        club.ClearLineupSlot(LineupSlot.Forward(2, 1));
        Assert.False(club.IsDressed(b));
    }

    private static LiveTeam NewTeam(Club club) => new()
    {
        Managed = true,
        Squad = LiveSquad.Create(club, 2, club.Tactics),
        InjuryManager = 0,
    };

    /// <summary>
    /// [ORIGINAL] Datei 0x41533 (Platz +0x15) und 0x5438D–0x54419 (Neuzählung 0x38A36): Nach dem Eingriff steht der neue Platz im
    /// Spielkader, Energie und Tore des Spiels bleiben, die Feldspieler der Reihe auf dem Eis sind neu gezählt; die vierte Reihe
    /// gilt erst danach als besetzt (0x08E5F).
    /// </summary>
    [Fact]
    public void Apply_TakesPositions_KeepsEnergy_Recounts()
    {
        var club = NewClub();
        var team = NewTeam(club);
        var squad = team.Squad!;
        Assert.False(squad.HasFourthLine(1));
        var moved = club.Lineup[LineupSlot.Forward(0, 1)]!;
        var live = squad.Players.Single(p => p.Player == moved);
        live.Energy = 40;
        live.MatchGoals = 1;
        int? line = 0;
        club.ClickPlayerInMatch(moved, ref line, right: false);
        line = 3;
        Assert.Equal(LineupResult.Assigned, club.ClickPlayerInMatch(moved, ref line, right: false));
        Assert.Equal(0x81, live.Position); // vor dem Abgleich alter Platz

        LiveLineup.Apply(team, club);
        Assert.Equal(0x84, live.Position);
        Assert.True(live.Dressed);
        Assert.Equal(40, live.Energy);
        Assert.Equal(1, live.MatchGoals);
        Assert.True(squad.HasFourthLine(1));
        Assert.Equal(2, squad.SkatersPerPart[1]); // Reihe 1 auf dem Eis hat nur noch zwei Stürmer
        Assert.Equal(4, team.Box.Skaters);
    }

    /// <summary>Spieler auf der Strafbank bleiben bis zum Freiwerden draußen (+0x0A = 0, 0x35691), auch wenn er aufgestellt bleibt.</summary>
    [Fact]
    public void Apply_KeepsBoxedPlayerOff()
    {
        var club = NewClub();
        var team = NewTeam(club);
        var boxed = team.Squad!.Players.First(p => p.Position == 0x81);
        boxed.Dressed = false;
        team.Box.Occupants[0] = boxed;
        LiveLineup.Apply(team, club);
        Assert.False(boxed.Dressed);
        Assert.Equal(0x81, boxed.Position);
    }

    /// <summary>Computervereine (ohne Spielkader) bleiben unberührt.</summary>
    [Fact]
    public void Apply_WithoutSquad_Nothing()
    {
        var club = NewClub();
        var team = new LiveTeam { Managed = false };
        LiveLineup.Apply(team, club);
        Assert.Equal(OriginalMatch.FullStrength, team.Box.Skaters);
    }
}
