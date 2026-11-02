using System.Text.Json.Nodes;
using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur wettbewerbe-spiel, Teil 8: automatische Aufstellung 0x426BC/0x42838 (A068, A165, A169; re-nachtrag §3, Review #446)
/// und Neuzählung der Feldspieler 0x38A36 (A089, A070; <c>docs/spielberechnung.md</c> §22).
/// </summary>
public class AutoLineupTeil8Tests
{
    private static Player P(PlayerRole role, int c, int t, int f, int number = 1) =>
        new() { Name = $"{role}{number}", Role = role, Condition = c, Technique = t, Form = f, Number = number };

    private static GameState Create(int seed = 3)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Trainer", Head = 0, ClubIndex = 2 });
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    // ---- 0x42838 ------------------------------------------------------------------------------------------------------

    [Fact]
    public void FindBest_Tie_LaterSlotWins()
    {
        // [ORIGINAL] Datei 0x4292F: übersprungen nur bei größerem Bestwert → bei Gleichstand der spätere Platz.
        var slots = new List<Player> { P(PlayerRole.Forward, 60, 60, 60, 1), P(PlayerRole.Forward, 61, 59, 60, 2) };
        Assert.Equal(1, AutoLineupRule.FindBest(slots, [false, false], 2));
        // Wert ist (K + T + F) / 3 ganzzahlig: 181/3 = 60 = 180/3.
        slots.Add(P(PlayerRole.Forward, 59, 60, 60, 3));
        Assert.Equal(1, AutoLineupRule.FindBest(slots, [false, false, false], 2));
    }

    [Fact]
    public void FindBest_FallbackRoles_FirstFreeGoalieIsKeptBack()
    {
        var g1 = P(PlayerRole.Goalkeeper, 90, 90, 90, 1);
        var g2 = P(PlayerRole.Goalkeeper, 10, 10, 10, 2);
        var d = P(PlayerRole.Defender, 50, 50, 50, 3);
        // Sturm → Abwehr: der Verteidiger.
        Assert.Equal(2, AutoLineupRule.FindBest([g1, g2, d], [false, false, false], 2));
        // Nur Torhüter frei: der erste zählt −1 (nie gewählt), der zweite 1 – trotz schwächerer Werte.
        Assert.Equal(1, AutoLineupRule.FindBest([g1, g2, d], [false, false, true], 2));
        // Ein einziger freier Torhüter bleibt zurück.
        Assert.Equal(-1, AutoLineupRule.FindBest([g1, g2, d], [false, true, true], 1));
        // Torhütersuche: Ausweichrolle Abwehr.
        Assert.Equal(2, AutoLineupRule.FindBest([g1, g2, d], [true, true, false], 0));
        // Verletzte und Gesperrte zählen nicht (+0x09 & 3).
        d.InjuredGames = 2;
        Assert.Equal(-1, AutoLineupRule.FindBest([g1, g2, d], [true, true, false], 0));
    }

    // ---- 0x426BC ------------------------------------------------------------------------------------------------------

    [Fact]
    public void Apply_FillsThreeBlocks_ByValue_AndDressesAtMostTwentyTwo()
    {
        var squad = new List<Player>();
        var n = 1;
        for (var i = 0; i < 3; i++)
            squad.Add(P(PlayerRole.Goalkeeper, 50 + i, 50, 50, n++));
        for (var i = 0; i < 9; i++)
            squad.Add(P(PlayerRole.Defender, 40 + 3 * i, 50, 50, n++));
        for (var i = 0; i < 14; i++)
            squad.Add(P(PlayerRole.Forward, 30 + 3 * i, 50, 50, n++));
        var lineup = new Lineup();

        var dressed = AutoLineupRule.Apply(lineup, squad);

        Assert.Equal(22, dressed.Count);
        Assert.Same(squad[2], lineup.Goalkeeper); // bester Torhüter
        // Abwehr: Blöcke der Reihe nach mit den jeweils besten freien (je Block 2).
        var defenders = squad.Where(p => p.Role == PlayerRole.Defender).OrderByDescending(AutoLineupRule.Value).ToList();
        Assert.Equal(defenders.Take(2).ToHashSet(), lineup.Block(0).Where(p => p.Role == PlayerRole.Defender).ToHashSet());
        Assert.Equal(defenders.Skip(4).Take(2).ToHashSet(), lineup.Block(2).Where(p => p.Role == PlayerRole.Defender).ToHashSet());
        var forwards = squad.Where(p => p.Role == PlayerRole.Forward).OrderByDescending(AutoLineupRule.Value).ToList();
        Assert.Equal(forwards.Take(3).ToHashSet(), lineup.Block(0).Where(p => p.Role == PlayerRole.Forward).ToHashSet());
        Assert.Empty(lineup.Block(3));
        Assert.Equal(16, lineup.Count);
        // Ersatz reihum Sturm, Abwehr, Torhüter: 6 weitere (Zähler ab 1, Grenze 23) → 2 Stürmer, 2 Verteidiger, 2 Torhüter.
        var reserves = dressed.Where(p => !lineup.Contains(p)).ToList();
        Assert.Equal(2, reserves.Count(p => p.Role == PlayerRole.Forward));
        Assert.Equal(2, reserves.Count(p => p.Role == PlayerRole.Defender));
        Assert.Equal(2, reserves.Count(p => p.Role == PlayerRole.Goalkeeper));
    }

    [Fact]
    public void Apply_FewDefenders_ForwardFillsDefenderSlot()
    {
        // Ausweichrolle Abwehr → Sturm: im Original steht der Stürmer dann auf dem Abwehrplatz (+0x15 ohne Bit 7).
        var squad = new List<Player> { P(PlayerRole.Goalkeeper, 50, 50, 50, 1) };
        for (var i = 0; i < 5; i++)
            squad.Add(P(PlayerRole.Defender, 50, 50, 50, 2 + i));
        for (var i = 0; i < 12; i++)
            squad.Add(P(PlayerRole.Forward, 30 + i, 50, 50, 10 + i));
        var lineup = new Lineup();
        AutoLineupRule.Apply(lineup, squad);

        var slot = lineup[LineupSlot.Defender(2, 1)];
        Assert.NotNull(slot);
        Assert.Equal(PlayerRole.Forward, slot!.Role);
        Assert.Same(squad[^1], slot); // der beste Stürmer geht zuerst in die Abwehr
    }

    // ---- Einstellung „automatisch aufstellen“ -------------------------------------------------------------------------

    [Fact]
    public void Setting_StartsOn_ManualChangeSwitchesItOff()
    {
        var game = Create();
        var club = game.Clubs[2];
        Assert.True(club.AutoLineupEnabled);
        Assert.Equal(16, club.Lineup.Count);

        var slot = LineupSlot.Forward(0, 0);
        club.ClearLineupSlot(slot); // Datei 0x40ADE: Einstellung 0
        Assert.False(club.AutoLineupEnabled);
        game.Continue();
        Assert.Null(club.Lineup[slot]);

        club.AutoLineup(); // „A“: ein und neu aufstellen
        Assert.True(club.AutoLineupEnabled);
        Assert.NotNull(club.Lineup[slot]);
    }

    [Fact]
    public void Daily_InjuredPlayer_IsReplacedByRule()
    {
        // Regressionstest A068: vorher füllte der Tageslauf beim Managerverein nichts (CleanLineups nur ohne Manager); ein
        // Gesperrter blieb als Lücke. Mit der Einstellung stellt 0x426BC am Ende des Tageslaufs (0x07C07) neu auf.
        var game = Create();
        var club = game.Clubs[2];
        var victim = club.Lineup[LineupSlot.Forward(0, 0)]!;
        victim.SuspendedGames = 3;
        game.Continue();
        Assert.False(club.Lineup.Contains(victim));
        Assert.False(club.IsDressed(victim));
        Assert.Equal(16, club.Lineup.Count);
        Assert.True(club.DressedCount <= Club.MaxDressed);
    }

    [Fact]
    public void Migration_31To32_SetsSettingOn()
    {
        var game = Create();
        game.Clubs[2].AutoLineupEnabled = true;
        var root = JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        root["schema"] = 31; // vor Teil 8 (Integration Runde 11: 31 → 32)
        foreach (var club in root["vereine"]!.AsArray().Select(n => n!.AsObject()))
        {
            Assert.True(club.Remove("_autoLineup"));
            club["_dressedByHand"] = false; // Stand vor Teil 8 ohne Eingriff von Hand (der Start setzt seit Teil 8 von Hand ein)
        }
        var result = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.True(result.Success, result.Error);
        Assert.All(result.Game!.Clubs, c => Assert.True(c.AutoLineupEnabled));
        var again = SaveGame.Save(result.Game);
        Assert.Equal(again, SaveGame.Save(SaveGame.Load(again).Game!));
    }

    /// <summary>
    /// WS-01 (Review #468): Ein Stand vor Teil 8, in dem von Hand aufgestellt wurde, behält seine Aufstellung – die
    /// Migration setzt die Einstellung dann aus (im Original schaltet jeder Eingriff sie ab, Datei 0x40ADE/0x40DE4).
    /// </summary>
    [Fact]
    public void Migration_31To32_KeepsHandMadeLineup()
    {
        var game = Create();
        var club = game.Clubs[2];
        var slot = LineupSlot.Forward(0, 0);
        var choice = club.Players.First(p => p.Role == PlayerRole.Forward && p.Available && club.IsDressed(p) && !club.Lineup.Contains(p));
        Assert.Equal(LineupResult.Assigned, club.AssignToLineup(slot, choice));
        Assert.False(club.AutoLineupEnabled);
        var root = JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        root["schema"] = 31;
        foreach (var c in root["vereine"]!.AsArray().Select(n => n!.AsObject()))
            Assert.True(c.Remove("_autoLineup"));
        var result = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.True(result.Success, result.Error);
        var loaded = result.Game!;
        Assert.False(loaded.Clubs[2].AutoLineupEnabled);
        loaded.Continue();
        Assert.Equal(choice.Id, loaded.Clubs[2].Lineup[slot]!.Id);
    }

    // ---- Mehrsaisonprobe -----------------------------------------------------------------------------------------------

    /// <summary>
    /// Mehrsaisonprobe (4 Saisonwechsel, Modus nach Fahne/Weltkugel; passiv und aktiv): Mit der Einstellung stehen im
    /// Managerverein nach jedem Tag nur spielfähige Kaderspieler, höchstens 22 eingesetzt, Torhüter aufgestellt, solange
    /// einer spielfähig ist. Der aktive Manager greift je Saison von Hand ein (Einstellung aus) und schaltet sie zur
    /// Saisonmitte mit „A“ wieder ein.
    /// </summary>
    [Theory]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public void MultiSeason_AutoLineup_StaysConsistent(bool flag, bool globe, bool active)
    {
        var game = GameModeTests.Create(5, flag, globe, 2, 30);
        var manager = game.Managers[0];
        for (var change = 0; change <= 4; change++)
        {
            var club = game.Clubs[manager.ClubIndex];
            TestSquadKeeper.KeepPlayable(game, manager);
            if (active)
            {
                club.AutoLineup();
                club.ClearLineupSlot(LineupSlot.All.First(s => club.Lineup[s] is not null));
                Assert.False(club.AutoLineupEnabled);
            }
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 1000, "Saisonende nicht erreicht");
                foreach (var m in game.Managers)
                    m.Money = 10_000_000;
                TestSquadKeeper.KeepPlayable(game, manager); // Vorbedingung: Kader bleibt (synthetische Datenbank)
                game.Continue();
                club = game.Clubs[manager.ClubIndex];
                if (active && guard == 60)
                    club.AutoLineup();
                if (club.AutoLineupEnabled && flag)
                    Check(club);
            }
            var loaded = SaveGame.Load(SaveGame.Save(game));
            Assert.True(loaded.Success, loaded.Error);
            Assert.Equal(club.AutoLineupEnabled, loaded.Game!.Clubs[club.Index].AutoLineupEnabled);
            if (change < 4)
                game.StartNextSeason();
        }
        Assert.Equal(5, game.History.Count);

        static void Check(Club club)
        {
            Assert.All(club.Lineup.Players, p => Assert.True(p.Available && club.Players.Contains(p)));
            Assert.True(club.DressedCount <= Club.MaxDressed, $"{club.DressedCount} Eingesetzte");
            if (club.Players.Count(p => p.Available && p.Role == PlayerRole.Goalkeeper) > 1)
                Assert.NotNull(club.Lineup.Goalkeeper);
        }
    }

    // ---- 0x38A36 ------------------------------------------------------------------------------------------------------

    private static (LiveTeam Team, LiveSquad Squad) Team(int forwardsInLine0, bool goalie = true)
    {
        var squad = new LiveSquad();
        if (goalie)
            squad.Players.Add(new LivePlayer { Player = P(PlayerRole.Goalkeeper, 50, 50, 50), Position = 1, Dressed = true });
        for (var i = 0; i < 2; i++)
            squad.Players.Add(new LivePlayer { Player = P(PlayerRole.Defender, 50, 50, 50), Position = 1, Dressed = true });
        for (var i = 0; i < forwardsInLine0; i++)
            squad.Players.Add(new LivePlayer { Player = P(PlayerRole.Forward, 50, 50, 50), Position = 0x81, Dressed = true });
        var team = new LiveTeam { Squad = squad, Managed = true, InjuryManager = 0 };
        return (team, squad);
    }

    [Fact]
    public void Recount_FullLine_FiveSkaters()
    {
        var (team, squad) = Team(3);
        MatchEngine.RecountSkaters(team);
        Assert.Equal([2, 3], squad.SkatersPerPart);
        Assert.Equal(5, team.Box.Skaters);
        Assert.False(team.GoaliePulled);
    }

    [Fact]
    public void Recount_GapInLine_FourSkaters()
    {
        // Regressionstest A089: vorher fest 2/3 und 5 Feldspieler, auch wenn die Reihe nur zwei Stürmer hat.
        var (team, squad) = Team(2);
        MatchEngine.RecountSkaters(team);
        Assert.Equal([2, 2], squad.SkatersPerPart);
        Assert.Equal(4, team.Box.Skaters);
    }

    [Fact]
    public void Recount_PenaltyBox_ReducesForwards()
    {
        // Torhüter auf der Strafbank bleibt auf dem Eis: 1 + 2 + 3 + 1 = 7 > 6 → Sturm 3 − 1 = 2, Feldspieler 4.
        var (team, squad) = Team(3);
        team.Box.Add(0, 2);
        MatchEngine.RecountSkaters(team);
        Assert.Equal([2, 2], squad.SkatersPerPart);
        Assert.Equal(4, team.Box.Skaters);
        // Freiwerden 0x35691 zählt neu (Datei 0x35810).
        team.Box.Released = () => MatchEngine.RecountSkaters(team);
        team.Box.Release(0, force: true);
        Assert.Equal([2, 3], squad.SkatersPerPart);
        Assert.Equal(5, team.Box.Skaters);
    }

    [Fact]
    public void Recount_NoGoalie_DefenseLineReset_LimitFive()
    {
        var (team, squad) = Team(3, goalie: false);
        team.DefenseLine = 2;
        MatchEngine.RecountSkaters(team);
        Assert.Equal(0, team.DefenseLine); // Datei 0x38A2A
        Assert.Equal([2, 3], squad.SkatersPerPart);
        Assert.Equal(5, team.Box.Skaters);
    }

    [Fact]
    public void Recount_ComputerClub_Unchanged()
    {
        var (team, squad) = Team(2);
        var computer = new LiveTeam { Squad = squad, Managed = false };
        MatchEngine.RecountSkaters(computer);
        Assert.Equal([2, 3], squad.SkatersPerPart);
        Assert.Equal(5, computer.Box.Skaters);
    }
}
