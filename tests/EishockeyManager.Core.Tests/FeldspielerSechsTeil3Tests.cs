using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur <c>feldspieler-sechs</c>, Runde 23, Teil 3 (#939, A875, Rest A490): sechster Feldspielerplatz in der Nationalaufstellung
/// (Satz 5, gleicher Bildschirm 0x40364 wie beim Verein: Datei 0x40EC4–0x40EFF, 0x413C9–0x414B4) und Mehrsaisonprobe des Vereins.
/// Synthetisch, ohne Originaldaten.
/// </summary>
public class FeldspielerSechsTeil3Tests
{
    /// <summary>Bundestrainer ernannt, Kader nominiert, „A“ aufgestellt, dann den Torhüter heraus.</summary>
    private static (GameState Game, NationalCoach Coach) CoachWithoutGoalie(int seed = 5)
    {
        var game = GameModeTests.Create(seed, flag: false, globe: true, 5);
        game.Continue();
        game.AutoNominate();
        var coach = Assert.IsType<NationalCoach>(game.NationalCoach);
        coach.AutoFormation();
        coach.ClearSlot(LineupSlot.Goalkeeper);
        return (game, coach);
    }

    private static Player Free(NationalCoach coach, PlayerRole role) =>
        coach.Squad.Select(m => m.Player).First(p => p.Role == role && p.Available && !coach.Formation.Contains(p));

    /// <summary>
    /// A875: Reihe 1 voll (3 + 2), kein Torhüter: ein weiterer Verteidiger kommt auf den sechsten Feldspielerplatz, eingesetzt; er zählt
    /// in der Aufstellung des Spiels (Länderspiele), nicht in den Reihenwerten der Anzeige. Vorher: „Kein / Platz mehr !“.
    /// </summary>
    [Fact]
    public void NationalClick_Line1FullWithoutGoalie_OpensSixthSkaterSlot()
    {
        var (_, coach) = CoachWithoutGoalie();
        var values = Enumerable.Range(0, NationalCoach.Lines).Select(l => coach.Lineup().LineValue(l, attack: false)).ToList();
        int? line = 0;
        var sixth = Free(coach, PlayerRole.Defender);

        Assert.Equal(LineupResult.Assigned, coach.ClickPlayer(sixth, ref line, right: false));

        Assert.Same(sixth, coach.Formation.SixthSkater);
        Assert.True(coach.IsDressed(sixth));
        Assert.Contains(sixth, coach.Lineup().Players);
        Assert.Equal(values, Enumerable.Range(0, NationalCoach.Lines).Select(l => coach.Lineup().LineValue(l, attack: false)));
        Assert.Equal(LineupResult.NoRoom, coach.ClickPlayer(Free(coach, PlayerRole.Forward), ref line, right: false));
    }

    /// <summary>A875: „Schon 6 Feldspieler / auf dem Eis !“ für einen Torhüter in der Torhüterreihe, außerhalb und im Spiel.</summary>
    [Fact]
    public void NationalClick_GoalieWhileSixthOpen_SixSkatersMessage()
    {
        var (_, coach) = CoachWithoutGoalie();
        int? line = 0;
        Assert.Equal(LineupResult.Assigned, coach.ClickPlayer(Free(coach, PlayerRole.Defender), ref line, right: false));
        var goalie = Free(coach, PlayerRole.Goalkeeper);

        line = NationalCoach.GoalkeeperLine;
        Assert.Equal(LineupResult.SixSkaters, coach.ClickPlayer(goalie, ref line, right: false));
        Assert.Equal(LineupResult.SixSkaters, coach.ClickPlayerInMatch(goalie, ref line, right: false));
        Assert.Null(coach.Formation[LineupSlot.Goalkeeper]);
    }

    /// <summary>
    /// A875: Im Länderspiel öffnet der Eingriff den Platz ebenso (Kennwert nur an 0x41533, Datei 0x413E8); nach der Übernahme zählt 0x38A36
    /// sechs Feldspieler ohne Torhüter (0xCC37 = 3/3, Vereinsfeld 0x32 = 6, 0x2C Bit 3), der Spieler steht auf +0x15 = 1.
    /// </summary>
    [Fact]
    public void NationalInMatch_OpensSixthSkater_RecountsSix()
    {
        var (game, coach) = CoachWithoutGoalie();
        var squad = MatchEngine.NationalSquad(game, coach);
        var team = new LiveTeam { Managed = true, Squad = squad, InjuryManager = NationalCoach.ManagerRecord };
        int? line = 0;
        var forward = Free(coach, PlayerRole.Forward);

        Assert.Equal(LineupResult.Assigned, coach.ClickPlayerInMatch(forward, ref line, right: false));
        MatchEngine.ApplyNationalLineup(team, coach);

        Assert.Same(forward, coach.Formation.SixthSkater);
        Assert.Equal(1, squad.Players.Single(p => ReferenceEquals(p.Player, forward)).Position);
        Assert.Equal([3, 3], squad.SkatersPerPart);
        Assert.Equal(6, team.Box.Skaters);
        Assert.True(team.GoaliePulled);
        Assert.Equal(1, MatchEngine.NationalSquad(game, coach).Players.Single(p => ReferenceEquals(p.Player, forward)).Position);
    }

    /// <summary>
    /// Mehrsaisonprobe (AGENTS.md §5): vier Saisonwechsel mit einem Managerverein ohne Torhüter und sechstem Feldspieler, passiv (einmal
    /// vor Saison 1 aufgestellt) und aktiv (vor jedem Schritt wieder geöffnet, soweit möglich); Kader voll gehalten (<c>KeepSquad</c> mit
    /// Verlängerung). Nach jedem Schritt: Der sechste Platz ist nur ohne Torhüter belegt, steht auf +0x15 = 1, sein Spieler auf keinem zweiten Platz; Sichern und Laden halten ihn. Aktiv spielt der
    /// Verein in jeder Saison mit herausgenommenem Torhüter (Vereinsfeld 0x2C Bit 3 am Spielende, A838).
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultiSeason_SixthSkaterLineupStaysConsistent(bool active)
    {
        var game = TestGames.Create(23);
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        Assert.True(Open(club), "sechster Platz vor Saison 1 nicht zu öffnen");
        for (var change = 0; change < 4; change++)
        {
            var pulled = false;
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 1000, "Saisonende nicht erreicht");
                TestSeason.KeepSquad(game, manager, extendExpired: true);
                if (manager.Money < 2_000_000)
                    manager.Money = 2_000_000; // keine Entlassung im Testlauf
                if (active)
                    Open(club);
                game.Continue();
                pulled |= club.Tactics.GoaliePulledAtEnd;
                AssertConsistent(club);
            }
            if (active)
                Assert.True(pulled, $"Saison {change + 1}: nie ohne Torhüter gespielt");
            var loaded = SaveGame.Load(SaveGame.Save(game)).Game!.Clubs[club.Index];
            Assert.Equal(club.Lineup.SixthSkater?.Id, loaded.Lineup.SixthSkater?.Id);
            game.StartNextSeason();
        }
    }

    /// <summary>Torhüter heraus, Automatik aus, Reihe 1 auffüllen und den sechsten Platz öffnen (Club.ClickPlayer).</summary>
    private static bool Open(Club club)
    {
        club.AutoLineupEnabled = false;
        if (club.Lineup[LineupSlot.Goalkeeper] is not null)
            club.ClearLineupSlot(LineupSlot.Goalkeeper);
        if (club.Lineup.SixthSkaterOpen)
            return true;
        foreach (var p in club.Players.Where(p => p.Role != PlayerRole.Goalkeeper && p.Available && !club.Lineup.Contains(p)).ToList())
        {
            int? line = 0;
            club.ClickPlayer(p, ref line, right: false);
            if (club.Lineup.SixthSkaterOpen)
                return true;
        }
        return false;
    }

    private static void AssertConsistent(Club club)
    {
        if (club.Lineup.SixthSkater is not { } sixth)
            return;
        Assert.Null(club.Lineup[LineupSlot.Goalkeeper]);
        Assert.Equal(LineupSlot.SixthSkater, club.Lineup.FindSlot(sixth));
        Assert.Equal(1, club.Lineup.PositionOf(sixth));
        Assert.Single(LineupSlot.All, s => ReferenceEquals(club.Lineup[s], sixth));
        Assert.Contains(sixth, club.Players);
    }
}
