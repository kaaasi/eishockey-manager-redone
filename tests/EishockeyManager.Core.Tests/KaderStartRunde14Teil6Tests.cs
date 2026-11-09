using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur kader-start Runde 14 Teil 6 (E14-7): Reihenlogik der Vereinsaufstellung wie A010 (<see cref="Club.ClickPlayer"/>,
/// em.exe Datei 0x40D8D–0x414C6, derselbe Code wie <see cref="NationalCoach.ClickPlayer"/>). Synthetisch, ohne Originaldaten.
/// </summary>
public class KaderStartRunde14Teil6Tests
{
    private static int s_nextId = 50_000;

    private static Player NewPlayer(PlayerRole role, string name)
    {
        var p = new Player { Name = name, Role = role, Condition = 70, Technique = 70, Form = 50, Age = 25, ContractYears = 2 };
        typeof(Player).GetProperty(nameof(Player.Id))!.SetValue(p, Interlocked.Increment(ref s_nextId));
        return p;
    }

    /// <summary>2 Torhüter, 8 Verteidiger, 12 Stürmer; „A“ stellt drei Blöcke auf, die Übrigen sind eingesetzte Reserve (22).</summary>
    private static Club NewClub()
    {
        var club = new Club { Index = 3, Name = "Test", Strength = 70 };
        for (var i = 0; i < 2; i++) club.Players.Add(NewPlayer(PlayerRole.Goalkeeper, $"T{i}"));
        for (var i = 0; i < 8; i++) club.Players.Add(NewPlayer(PlayerRole.Defender, $"V{i}"));
        for (var i = 0; i < 12; i++) club.Players.Add(NewPlayer(PlayerRole.Forward, $"S{i}"));
        club.RenumberPlayers();
        club.AutoLineup();
        club.AutoLineupEnabled = false;
        return club;
    }

    private static Player Reserve(Club club, PlayerRole role) =>
        club.Players.First(p => p.Role == role && club.IsDressed(p) && !club.Lineup.Contains(p));

    [Fact]
    public void LeftClick_WithoutLine_NoLineSelected_NothingChanges()
    {
        var club = NewClub();
        var p = Reserve(club, PlayerRole.Forward);
        int? line = null;
        Assert.Equal(LineupResult.NoLineSelected, club.ClickPlayer(p, ref line, right: false));
        Assert.True(club.IsDressed(p));
        Assert.Null(line);
    }

    [Fact]
    public void LeftClick_FirstFreeSlotOfLine_FullLineNoRoom_GoalkeeperWrongLine()
    {
        var club = NewClub();
        var fwd = Reserve(club, PlayerRole.Forward);
        int? line = 0;
        Assert.Equal(LineupResult.NoRoom, club.ClickPlayer(fwd, ref line, right: false));
        line = 3;
        Assert.Equal(LineupResult.Assigned, club.ClickPlayer(fwd, ref line, right: false));
        Assert.Same(fwd, club.Lineup[LineupSlot.Forward(3, 0)]);
        var gk = Reserve(club, PlayerRole.Goalkeeper);
        Assert.Equal(LineupResult.WrongRole, club.ClickPlayer(gk, ref line, right: false));
        line = Lineup.GoalkeeperLine;
        Assert.Equal(LineupResult.WrongRole, club.ClickPlayer(Reserve(club, PlayerRole.Defender), ref line, right: false));
        Assert.Equal(LineupResult.NoRoom, club.ClickPlayer(gk, ref line, right: false));
    }

    [Fact]
    public void ClickOnPlaced_TakesOut_SelectsHisLine_LeftUndresses_RightKeeps()
    {
        var club = NewClub();
        var a = club.Lineup[LineupSlot.Defender(2, 1)]!;
        int? line = 0;
        Assert.Equal(LineupResult.TakenOut, club.ClickPlayer(a, ref line, right: false));
        Assert.Equal(2, line);
        Assert.False(club.IsDressed(a));
        Assert.Null(club.Lineup[LineupSlot.Defender(2, 1)]);

        var gk = club.Lineup.Goalkeeper!;
        Assert.Equal(LineupResult.TakenOut, club.ClickPlayer(gk, ref line, right: true));
        Assert.Equal(Lineup.GoalkeeperLine, line);
        Assert.True(club.IsDressed(gk));
        Assert.Null(club.Lineup.Goalkeeper);
    }

    [Fact]
    public void RightClick_TogglesDressed_TwentyThirdRefused_AutoLineupOff()
    {
        var club = NewClub();
        club.Players.Add(NewPlayer(PlayerRole.Forward, "S12"));
        club.AutoLineupEnabled = true;
        var extra = club.Players[^1];
        int? line = null;
        Assert.Equal(22, club.DressedCount);
        Assert.Equal(LineupResult.RosterFull, club.ClickPlayer(extra, ref line, right: true));
        Assert.False(club.AutoLineupEnabled); // Datei 0x40DE4: vor allen Prüfungen
        var reserve = Reserve(club, PlayerRole.Forward);
        Assert.Equal(LineupResult.Undressed, club.ClickPlayer(reserve, ref line, right: true));
        Assert.Equal(LineupResult.Dressed, club.ClickPlayer(extra, ref line, right: true));
        Assert.Equal(22, club.DressedCount);
    }
}
