using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur national, Teil 4 (Runde 22, A455): Eingriff beim Nationalteam im Live-Spiel – Regeln „im Spiel“ der Aufstellung des
/// Satzes 5 (<c>CupsNational.Lineup.InMatch.cs</c>), Übernahme in den Spielkader (<see cref="MatchEngine.ApplyNationalLineup"/>)
/// und die Markierung zum Zusehen am Länderspiel (<c>CupsNational.GameState.cs</c>, <c>RatedNation</c>). Den Klick selbst prüft
/// <c>EishockeyManager.UI.Tests.NationalInMatchLineupTests</c>.
/// </summary>
public class NationalInMatchLineupTests
{
    private static (GameState Game, NationalCoach Coach) Appointed(int seed = 5)
    {
        var game = GameModeTests.Create(seed, flag: false, globe: true, 5);
        Assert.Equal(GameMode.WorldOnly, game.Mode);
        game.Continue();
        game.AutoNominate();
        var coach = Assert.IsType<NationalCoach>(game.NationalCoach);
        coach.AutoFormation();
        return (game, coach);
    }

    private static Player Placed(NationalCoach coach, PlayerRole role, out LineupSlot slot)
    {
        var s = LineupSlot.All.First(x => x.Role == role && coach.Formation[x] is not null);
        slot = s;
        return coach.Formation[s]!;
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x4115F/0x40CFD: ein im Spiel herausgenommener bzw. vom Platz genommener Spieler bleibt eingesetzt; außerhalb
    /// des Spiels nicht (Datei 0x4115F mit Kennwert 0). Vor dem Teil rot: es gab nur <see cref="NationalCoach.ClickPlayer"/>.
    /// </summary>
    [Fact]
    public void InMatch_TakenOutPlayer_StaysDressed()
    {
        var (_, coach) = Appointed();
        var player = Placed(coach, PlayerRole.Forward, out var slot);
        int? line = null;
        Assert.Equal(LineupResult.TakenOut, coach.ClickPlayerInMatch(player, ref line, right: false));
        Assert.Equal(slot.Block, line);
        Assert.Null(coach.Formation[slot]);
        Assert.True(coach.IsDressed(player));
        Assert.False(coach.AutoLineupEnabled);

        var other = Placed(coach, PlayerRole.Defender, out var slot2);
        coach.ClearSlotInMatch(slot2);
        Assert.Null(coach.Formation[slot2]);
        Assert.True(coach.IsDressed(other));

        // Gegenprobe ohne Spiel: herausgenommen = nicht mehr eingesetzt.
        var third = Placed(coach, PlayerRole.Forward, out _);
        line = null;
        Assert.Equal(LineupResult.TakenOut, coach.ClickPlayer(third, ref line, right: false));
        Assert.False(coach.IsDressed(third));
    }

    /// <summary>[ORIGINAL] Datei 0x41287–0x4129E: Rechtsklick auf einen Aufgestellten ändert im Spiel nichts.</summary>
    [Fact]
    public void InMatch_RightClickOnPlaced_Unchanged()
    {
        var (_, coach) = Appointed();
        var player = Placed(coach, PlayerRole.Forward, out var slot);
        int? line = null;
        Assert.Equal(LineupResult.Unchanged, coach.ClickPlayerInMatch(player, ref line, right: true));
        Assert.Same(player, coach.Formation[slot]);
        Assert.True(coach.IsDressed(player));
    }

    /// <summary>[ORIGINAL] Datei 0x40E2A: die Grenze „22 eingesetzte Spieler“ gilt im Spiel nicht.</summary>
    [Fact]
    public void InMatch_No22Limit()
    {
        var (_, coach) = Appointed();
        Assert.Equal(NationalCoach.MaxDressed, coach.DressedCount);
        var spare = coach.Rows.First(p => p.Role == PlayerRole.Forward && !coach.IsDressed(p) && p.Available);
        var slot = LineupSlot.All.First(s => s.Role == PlayerRole.Forward && coach.Formation[s] is not null);
        coach.ClearSlotInMatch(slot); // Platz frei, Spieler bleibt eingesetzt: weiter 22
        Assert.Equal(NationalCoach.MaxDressed, coach.DressedCount);
        int? line = slot.Block;
        Assert.Equal(LineupResult.RosterFull, coach.ClickPlayer(spare, ref line, right: false));
        line = slot.Block;
        Assert.Equal(LineupResult.Assigned, coach.ClickPlayerInMatch(spare, ref line, right: false));
        Assert.Same(spare, coach.Formation[slot]);
        Assert.Equal(NationalCoach.MaxDressed + 1, coach.DressedCount);
    }

    /// <summary>
    /// Übernahme nach dem Eingriff: Platz +0x15 aus der Aufstellung, eingesetzt +0x0A, Feldspieler neu gezählt (0x38A36, Datei
    /// 0x5438D–0x54419); ein Kader eines Vereins (ohne Satz 5) bleibt unberührt.
    /// </summary>
    [Fact]
    public void ApplyNationalLineup_TakesPositionsAndRecounts()
    {
        var (game, coach) = Appointed();
        var squad = MatchEngine.NationalSquad(game, coach);
        var team = new LiveTeam { Managed = true, Squad = squad, InjuryManager = NationalCoach.ManagerRecord };
        MatchEngine.RecountSkaters(team);
        var player = Placed(coach, PlayerRole.Forward, out var slot);
        var live = squad.Players.Single(p => p.Player == player);
        Assert.Equal(LiveSquad.PositionOf(slot), live.Position);
        var skaters = squad.SkatersPerPart.ToArray();

        coach.ClearSlotInMatch(slot);
        Assert.Equal(LiveSquad.PositionOf(slot), live.Position); // erst mit der Übernahme
        MatchEngine.ApplyNationalLineup(team, coach);
        Assert.Equal(0, live.Position);
        Assert.True(live.Dressed);
        Assert.Equal([skaters[0], skaters[1] - 1], squad.SkatersPerPart);

        var club = new LiveTeam { Managed = true, Squad = new LiveSquad() };
        MatchEngine.ApplyNationalLineup(club, coach); // kein Kader des Satzes 5
        Assert.Empty(club.Squad.Players);
    }

    /// <summary>
    /// [ORIGINAL] Die Ernennung markiert Deutschland zum Zusehen (0x0AE2B, Datei 0x5545F); das Länderspiel trägt die Markierung des
    /// Vereinsplatzes (Vereinsfeld 0x34 Bit 7), also Anzeigereihe und Live-Bildschirm. Vor dem Teil rot: der Spielpartner
    /// <c>RatedNation</c> hatte keine Markierung, WM-Spiele liefen nie über den Live-Bildschirm.
    /// </summary>
    [Fact]
    public void RatedNation_CarriesWatchMark()
    {
        var (game, coach) = Appointed();
        Assert.True(game.Clubs[coach.Nation].WatchedLive);
        Assert.True(game.RatedNation(coach.Nation).WatchedLive);
        var other = Enumerable.Range(NationalTeams.First, NationalTeams.Last - NationalTeams.First + 1)
            .First(n => n != coach.Nation && !game.Clubs[n].WatchedLive);
        Assert.False(game.RatedNation(other).WatchedLive);
    }
}
