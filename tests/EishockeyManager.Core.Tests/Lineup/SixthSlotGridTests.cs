using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur <c>aufstellung</c>, Teil 2 K07 (4), A971/A972: Kennzeichen „sechster Platz offen“ als Bildschirmzustand
/// (<see cref="LineupGrid"/>). [ORIGINAL] Beleg <c>tools/reverse-engineering/abweichungen/k07_sechster.py</c>, Fälle a–d; Aufbau Datei
/// 0x403DB–0x404FD, Leeren Datei 0x40B69–0x40C98, Herausnehmen aus der Liste Datei 0x41032–0x41049, Abrechnung Datei 0x54918.
/// Synthetisch, ohne Originaldaten.
/// </summary>
public class SixthSlotGridTests
{
    private static int s_nextId = 97_100;

    private static Player NewPlayer(PlayerRole role, string name)
    {
        var p = new Player { Name = name, Role = role, Condition = 70, Technique = 70, Form = 50, Age = 25, ContractYears = 2 };
        typeof(Player).GetProperty(nameof(Player.Id))!.SetValue(p, Interlocked.Increment(ref s_nextId));
        return p;
    }

    /// <summary>Drei Blöcke, kein Torhüter, sechster Feldspieler über einen Klick mit Gitter geöffnet (Fall a/b/c/d: Aufbau).</summary>
    private static (Club Club, LineupGrid Grid, Player Sixth) OpenSixth()
    {
        var club = new Club { Index = 3, Name = "Test", Strength = 70 };
        for (var i = 0; i < 2; i++) club.Players.Add(NewPlayer(PlayerRole.Goalkeeper, $"T{i}"));
        for (var i = 0; i < 8; i++) club.Players.Add(NewPlayer(PlayerRole.Defender, $"V{i}"));
        for (var i = 0; i < 12; i++) club.Players.Add(NewPlayer(PlayerRole.Forward, $"S{i}"));
        club.RenumberPlayers();
        club.AutoLineup();
        club.AutoLineupEnabled = false;
        club.ClearLineupSlot(LineupSlot.Goalkeeper);
        var grid = LineupGrid.Build(club.Lineup);
        Assert.False(grid.SixthOpen);
        int? line = 0;
        var sixth = club.Players.First(p => p.Role == PlayerRole.Defender && !club.Lineup.Contains(p));
        Assert.Equal(LineupResult.Assigned, club.ClickPlayer(sixth, ref line, right: false, grid));
        Assert.True(grid.SixthOpen); // Datei 0x414B4
        Assert.Same(sixth, club.Lineup.SixthSkater);
        return (club, grid, sixth);
    }

    private static Player Goalie(Club club) => club.Players.First(p => p.Role == PlayerRole.Goalkeeper && !club.Lineup.Contains(p));

    /// <summary>
    /// Fall a (A971/A972): Sturmzelle 0 der Reihe 1 leeren → Kennzeichen 0, das Gitter zeigt den sechsten in der Sturmzelle, sein
    /// Kaderplatz bleibt Abwehr Reihe 1 (+0x15 = 1). Vorher: Platz blieb offen, Sturmzelle leer.
    /// </summary>
    [Fact]
    public void CaseA_ClearForwardCell_ClosesFlagAndShowsSixthInForwardCell()
    {
        var (club, grid, sixth) = OpenSixth();
        var cell = LineupSlot.Forward(0, 0);
        var removed = club.Lineup[cell]!;

        club.ClearLineupSlot(cell, grid);

        Assert.False(grid.SixthOpen);
        Assert.Equal(cell, grid.SixthShownAt);
        Assert.Same(sixth, grid.PlayerAt(club.Lineup, cell));
        Assert.Same(sixth, club.Lineup.SixthSkater);
        Assert.Equal(1, club.Lineup.PositionOf(sixth));
        Assert.True(club.IsDressed(sixth));
        Assert.False(club.Lineup.Contains(removed));
        Assert.False(club.IsDressed(removed));
    }

    /// <summary>Fall b: Abwehrzelle leeren → der sechste rückt in die Abwehrzelle, Kennzeichen 0 (wie bisher, <c>MoveSixthSkaterInto</c>).</summary>
    [Fact]
    public void CaseB_ClearDefenceCell_SixthMovesIn()
    {
        var (club, grid, sixth) = OpenSixth();
        var cell = LineupSlot.Defender(0, 0);

        club.ClearLineupSlot(cell, grid);

        Assert.False(grid.SixthOpen);
        Assert.Null(grid.SixthShownAt);
        Assert.Same(sixth, club.Lineup[cell]);
        Assert.Null(club.Lineup.SixthSkater);
    }

    /// <summary>
    /// Fall c (A971): nach a ein Torhüter in die Torhüterreihe → eingesetzt, keine Meldung „Schon 6 Feldspieler“; Reihe 1 hat
    /// danach sechs Feldspieler und Torhüter. Vorher: <see cref="LineupResult.SixSkaters"/>.
    /// </summary>
    [Fact]
    public void CaseC_AfterForwardCleared_GoalkeeperAccepted()
    {
        var (club, grid, sixth) = OpenSixth();
        club.ClearLineupSlot(LineupSlot.Forward(0, 0), grid);
        int? line = Lineup.GoalkeeperLine;
        var goalie = Goalie(club);

        Assert.Equal(LineupResult.Assigned, club.ClickPlayer(goalie, ref line, right: false, grid));

        Assert.Same(goalie, club.Lineup.Goalkeeper);
        Assert.Same(sixth, club.Lineup.SixthSkater);
        Assert.Equal(1, club.Lineup.PositionOf(sixth));
    }

    /// <summary>Gegenprobe zu c: bei offenem Platz weist der Torhüterklick weiter ab (Datei 0x40EC4–0x40EFF).</summary>
    [Fact]
    public void Open_GoalkeeperRejected()
    {
        var (club, grid, _) = OpenSixth();
        int? line = Lineup.GoalkeeperLine;

        Assert.Equal(LineupResult.SixSkaters, club.ClickPlayer(Goalie(club), ref line, right: false, grid));
        Assert.Null(club.Lineup.Goalkeeper);
    }

    /// <summary>
    /// Fall d (A972): nach a ein neuer Aufbau → Kennzeichen 0, der sechste wird nicht gezeigt (kein Knopf, Sturmzelle leer), spielt
    /// aber weiter in Reihe 1 und bleibt eingesetzt (+0x0A ≠ 0 und +0x15 ≠ 0, Abrechnung Datei 0x54918).
    /// </summary>
    [Fact]
    public void CaseD_Rebuild_FlagOffSixthHiddenButPlays()
    {
        var (club, grid, sixth) = OpenSixth();
        var cell = LineupSlot.Forward(0, 0);
        club.ClearLineupSlot(cell, grid);

        var rebuilt = LineupGrid.Build(club.Lineup);

        Assert.False(rebuilt.SixthOpen);
        Assert.Null(rebuilt.SixthShownAt);
        Assert.Null(rebuilt.PlayerAt(club.Lineup, cell));
        Assert.Same(sixth, club.Lineup.SixthSkater);
        Assert.Equal(1, club.Lineup.PositionOf(sixth));
        Assert.True(club.IsDressed(sixth));
        Assert.Contains(sixth, club.Lineup.Players);
    }

    /// <summary>Aufbau mit voller Reihe 1 und sechstem ohne Torhüter: Kennzeichen 1 (Datei 0x404E8–0x404FD).</summary>
    [Fact]
    public void Build_SixInLineOneWithoutGoalie_Open()
    {
        var (club, _, _) = OpenSixth();
        Assert.True(LineupGrid.Build(club.Lineup).SixthOpen);
    }

    /// <summary>
    /// Die Sturmzelle mit dem sechsten ist belegt: ein Klick auf sie nimmt ihn heraus; ein neuer Stürmer kommt nicht hinein
    /// (Suche Datei 0x412A1–0x41381) und der Platz öffnet nicht erneut (<c>[APPROX]</c>, siehe <c>Lineup.TryOpenSixthSkater</c>).
    /// </summary>
    [Fact]
    public void ShownCell_IsOccupied_ClickRemovesSixth()
    {
        var (club, grid, sixth) = OpenSixth();
        var cell = LineupSlot.Forward(0, 0);
        club.ClearLineupSlot(cell, grid);
        int? line = 0;
        var forward = club.Players.First(p => p.Role == PlayerRole.Forward && !club.Lineup.Contains(p));

        Assert.Equal(LineupResult.NoRoom, club.ClickPlayer(forward, ref line, right: false, grid));
        Assert.Null(club.Lineup[cell]);

        club.ClearLineupSlot(cell, grid);
        Assert.False(club.Lineup.Contains(sixth));
        Assert.False(club.IsDressed(sixth));
        Assert.Null(grid.SixthShownAt);
    }

    /// <summary>
    /// Herausnehmen aus der Kaderliste (Datei 0x41032–0x41049): ein Stürmer der Reihe 1 lässt das Kennzeichen stehen, ein Verteidiger
    /// schließt es (der sechste rückt nach).
    /// </summary>
    [Fact]
    public void ListTakeOut_ForwardKeepsFlag_DefenderCloses()
    {
        var (club, grid, sixth) = OpenSixth();
        int? line = null;
        var forward = club.Lineup[LineupSlot.Forward(0, 1)]!;
        Assert.Equal(LineupResult.TakenOut, club.ClickPlayer(forward, ref line, right: false, grid));
        Assert.True(grid.SixthOpen);
        Assert.Null(grid.SixthShownAt);

        var defender = club.Lineup[LineupSlot.Defender(0, 1)]!;
        Assert.Equal(LineupResult.TakenOut, club.ClickPlayer(defender, ref line, right: false, grid));
        Assert.False(grid.SixthOpen);
        Assert.Same(sixth, club.Lineup[LineupSlot.Defender(0, 1)]);
    }

    /// <summary>Im Spiel gelten dieselben Gitterregeln (Datei 0x40B69 prüft den Kennwert nicht).</summary>
    [Fact]
    public void InMatch_ClearForwardCell_SameAsOutside()
    {
        var (club, grid, sixth) = OpenSixth();
        var cell = LineupSlot.Forward(0, 2);

        club.ClearLineupSlotInMatch(cell, grid);

        Assert.False(grid.SixthOpen);
        Assert.Same(sixth, grid.PlayerAt(club.Lineup, cell));
        int? line = Lineup.GoalkeeperLine;
        Assert.Equal(LineupResult.Assigned, club.ClickPlayerInMatch(Goalie(club), ref line, right: false, grid));
    }
}
