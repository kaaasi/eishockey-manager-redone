using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur nationalteam-2: Aufstellung und Taktik des Nationalteams.
/// Befund vorher: Das Nationalteam hatte weder Aufstellung noch Taktik; es spielte immer mit der Automatik aus
/// <see cref="NationalCoach.Lineup"/>, die Kacheln zeigten den Platzhalter.
/// </summary>
public class NationalLineupTests
{
    private static GameState Create(int seed = 3)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Trainer", Head = 0, ClubIndex = 2 });
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    private static NationalCoach Nominated(GameState game)
    {
        game.AppointNationalCoach(game.Managers[0]);
        game.AutoNominate();
        return game.NationalCoach!;
    }

    private static Player Weakest(NationalCoach coach, PlayerRole role) =>
        coach.Rows.Where(p => p.Role == role).OrderBy(NationalCoach.Value).First();

    /// <summary>Platz für einen Nicht-Eingesetzten schaffen: einen Reservisten herausnehmen (Linksklick).</summary>
    private static void MakeRoom(NationalCoach coach)
    {
        if (coach.DressedCount < NationalCoach.MaxDressed)
            return;
        var reserve = coach.Rows.First(p => coach.StatusOf(p) == PlayerStatus.Reserve);
        Assert.Equal(LineupResult.Undressed, coach.Undress(reserve));
    }

    /// <summary>Abnahme (1): andere Aufstellung → andere Stärke im nächsten WM-Spiel (WM-Partner aus der Stärke).</summary>
    [Fact]
    public void OtherFormation_GivesOtherWorldCupStrength()
    {
        var game = Create();
        var coach = Nominated(game);

        coach.AutoFormation();
        var auto = game.NationalMatchStrength(NationalTeams.Germany);

        // Den schwächsten Torhüter ins Tor, den schwächsten Stürmer in Reihe 1.
        var keeper = Weakest(coach, PlayerRole.Goalkeeper);
        MakeRoom(coach);
        Assert.Equal(LineupResult.Assigned, coach.Assign(LineupSlot.Goalkeeper, keeper));
        var forward = Weakest(coach, PlayerRole.Forward);
        MakeRoom(coach);
        Assert.Equal(LineupResult.Assigned, coach.Assign(LineupSlot.Forward(0, 0), forward));
        Assert.Same(keeper, coach.Lineup().Goalkeeper);
        Assert.Same(forward, coach.Lineup().Forwards[0]);
        var swapped = coach.Lineup().Players.Sum(NationalCoach.Value);

        // Sturm der Reihe 2 leeren: drei Lücken zählen 0 – die WM-Stärke sinkt sicher.
        for (var i = 0; i < Lineup.ForwardsPerBlock; i++)
            coach.ClearSlot(LineupSlot.Forward(1, i));
        var changed = game.NationalMatchStrength(NationalTeams.Germany);
        Assert.True(changed < auto, $"{changed} < {auto}");
        Assert.True(coach.Lineup().Players.Sum(NationalCoach.Value) < swapped);
        Assert.Equal(13, coach.Lineup().Players.Count());

        // Nur die Aufgestellten spielen: Reihe 4 und Reserve zählen nicht.
        var outside = coach.Rows.Where(p => !coach.Formation.Contains(p)).ToList();
        Assert.All(outside, p => Assert.DoesNotContain(p, coach.Lineup().Players));
    }

    /// <summary>Taktik wirkt wie beim Managerverein: Einsatz „SEMI“ 100 % in allen Blöcken erhöht die Stärke (Bezug 80 %).</summary>
    [Fact]
    public void Tactics_ChangeStrength()
    {
        var game = Create();
        var coach = Nominated(game);
        coach.AutoFormation();
        var before = game.NationalMatchStrength(NationalTeams.Germany);
        coach.Tactics.EffortMode = TacticMode.Semi;
        for (var b = 0; b < Tactics.Blocks; b++)
        {
            foreach (var r in Enum.GetValues<ScoreSituation>().Skip(1))
                coach.Tactics.SetEffort(b, r, 0);
            coach.Tactics.SetEffort(b, ScoreSituation.Base, Tactics.MaxEffort);
        }
        // Spur wettbewerbe-spiel, Teil 3 (A014): [ORIGINAL] kein Zuschlag mehr (vorher + 2), der Einsatz 100 statt 80 der
        // Automatik wirkt über die Kondition (0x98F8: Kondition − (100 − Einsatz · Kondition / 100) / 4).
        Assert.True(game.NationalMatchStrength(NationalTeams.Germany) > before);
    }

    /// <summary>Kaderfeld 0x0A „eingesetzt“: neue Kaderspieler nicht eingesetzt; Platz setzen/leeren; Grenze 22.</summary>
    [Fact]
    public void Dressing_FollowsOriginalRules()
    {
        var game = Create();
        var coach = Nominated(game);
        Assert.Equal(NationalCoach.MaxSquad, coach.Rows.Count);
        Assert.Equal(0, coach.DressedCount);
        Assert.All(coach.Rows, p => Assert.Null(coach.NumberOf(p)));
        Assert.All(coach.Rows, p => Assert.Null(coach.StatusOf(p)));

        var keeper = coach.Rows[0];
        Assert.Equal(PlayerRole.Goalkeeper, keeper.Role);
        Assert.Equal(LineupResult.WrongRole, coach.Assign(LineupSlot.Forward(0, 0), keeper));
        Assert.Equal(LineupResult.Assigned, coach.Assign(LineupSlot.Goalkeeper, keeper));
        Assert.True(coach.IsDressed(keeper));
        Assert.Equal(1, coach.NumberOf(keeper));
        Assert.Equal(PlayerStatus.InTeam, coach.StatusOf(keeper));

        // Platz leeren: nicht mehr eingesetzt (Datei 0x40D0D).
        coach.ClearSlot(LineupSlot.Goalkeeper);
        Assert.False(coach.IsDressed(keeper));
        Assert.Equal(0, coach.Formation.Count);

        // Rechtsklick setzt ein (RESERVE), Linksklick nimmt heraus; Aufgestellte bleiben unberührt.
        Assert.Equal(LineupResult.Dressed, coach.Dress(keeper));
        Assert.Equal(PlayerStatus.Reserve, coach.StatusOf(keeper));
        Assert.Equal(LineupResult.Undressed, coach.Undress(keeper));
        Assert.Null(coach.StatusOf(keeper));

        foreach (var p in coach.Rows.Take(NationalCoach.MaxDressed))
            Assert.Equal(LineupResult.Dressed, coach.Dress(p));
        Assert.Equal(NationalCoach.MaxDressed, coach.DressedCount);
        var extra = coach.Rows[NationalCoach.MaxDressed];
        Assert.Equal(LineupResult.RosterFull, coach.Dress(extra));
        var slot = extra.Role == PlayerRole.Forward ? LineupSlot.Forward(0, 0) : LineupSlot.Defender(0, 0);
        Assert.Equal(LineupResult.RosterFull, coach.Assign(slot, extra));
        Assert.Null(coach.NumberOf(extra));

        // Nummer = Zeile (nach Art sortiert), wie im Video (1–11, 14–22, 25, 26).
        Assert.Equal(Enumerable.Range(1, NationalCoach.MaxDressed),
            coach.Rows.Select(coach.NumberOf).OfType<int>());
        Assert.Equal(coach.Rows.OrderBy(p => p.Role).ToList(), coach.Rows);
    }

    [Fact]
    public void InjuredPlayer_IsRejected_AndDropsOutOfMatchLineup()
    {
        var game = Create();
        var coach = Nominated(game);
        coach.AutoFormation();
        var forward = coach.Formation[LineupSlot.Forward(0, 0)]!;
        Assert.Contains(forward, coach.Lineup().Players);
        forward.InjuredGames = 2;
        Assert.DoesNotContain(forward, coach.Lineup().Players);
        coach.ClearSlot(LineupSlot.Forward(0, 0));
        Assert.Equal(LineupResult.Injured, coach.Assign(LineupSlot.Forward(0, 0), forward));
    }

    /// <summary>Entlassen aus dem WM-Kader und „auto“ räumen die Aufstellung.</summary>
    [Fact]
    public void SquadChanges_ClearFormation()
    {
        var game = Create();
        var coach = Nominated(game);
        coach.AutoFormation();
        var forward = coach.Formation[LineupSlot.Forward(0, 0)]!;
        Assert.Equal(NominationResult.Removed, game.ToggleNomination(forward));
        Assert.False(coach.Formation.Contains(forward));
        Assert.False(coach.IsDressed(forward));
        game.AutoNominate();
        Assert.Equal(0, coach.Formation.Count);
        Assert.Equal(0, coach.DressedCount);
    }

    /// <summary>Abnahme (2): Spielstand mit Aufstellung, Taktik und Eingesetzten – Roundtrip byte-gleich.</summary>
    [Fact]
    public void RoundTrip_KeepsFormationTacticsAndDressing()
    {
        var game = Create();
        var coach = Nominated(game);
        coach.AutoFormation();
        MakeRoom(coach);
        Assert.Equal(LineupResult.Assigned, coach.Assign(LineupSlot.Goalkeeper, Weakest(coach, PlayerRole.Goalkeeper)));
        coach.Tactics.AttackMode = TacticMode.Semi;
        coach.Tactics.SetAttack(2, ScoreSituation.HighDeficit, 2);
        var bytes = SaveGame.Save(game);
        var loaded = SaveGame.Load(bytes).Game!;
        var copy = loaded.NationalCoach!;
        foreach (var slot in LineupSlot.All)
            Assert.Equal(coach.Formation[slot]?.Id, copy.Formation[slot]?.Id);
        Assert.Equal(coach.Rows.Select(coach.NumberOf), copy.Rows.Select(copy.NumberOf));
        Assert.Equal(TacticMode.Semi, copy.Tactics.AttackMode);
        Assert.Equal(2, copy.Tactics.Attack(2, ScoreSituation.HighDeficit));
        Assert.Equal(game.NationalMatchStrength(NationalTeams.Germany), loaded.NationalMatchStrength(NationalTeams.Germany));
        Assert.Equal(bytes, SaveGame.Save(loaded));
    }

    /// <summary>Abnahme (2): Schema 3 → 4 – ein Bundestrainer ohne Aufstellung bekommt eine leere Formation.</summary>
    [Fact]
    public void SchemaThreeSave_IsMigrated()
    {
        var game = Create();
        var coach = Nominated(game);
        var strength = game.NationalMatchStrength(NationalTeams.Germany);
        var root = System.Text.Json.Nodes.JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        root["schema"] = 3;
        var national = root["spiel"]!["NationalCoach"]!.AsObject();
        Assert.True(national.Remove("Formation"));
        Assert.True(national.Remove("Tactics"));
        Assert.True(national.Remove("_dressed"));

        var result = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.True(result.Success, result.Error);
        var loaded = result.Game!.NationalCoach!;
        Assert.Equal(0, loaded.Formation.Count);
        Assert.Equal(0, loaded.DressedCount);
        Assert.Equal(TacticMode.Auto, loaded.Tactics.EffortMode);
        Assert.Equal(coach.Squad.Count, loaded.Squad.Count);
        Assert.Equal(strength, result.Game.NationalMatchStrength(NationalTeams.Germany));
        var again = SaveGame.Save(result.Game);
        Assert.Equal(again, SaveGame.Save(SaveGame.Load(again).Game!));
    }

    /// <summary>Schema 3 ohne Bundestrainer (null) bleibt ladbar.</summary>
    [Fact]
    public void SchemaThreeSave_WithoutCoach_IsMigrated()
    {
        var game = Create();
        var root = System.Text.Json.Nodes.JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        root["schema"] = 3;
        var result = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.True(result.Success, result.Error);
        Assert.Null(result.Game!.NationalCoach);
    }

    /// <summary>
    /// Abnahme (4): Mehrsaisonprobe Modus 1 („nur Weltkugel“), 4 Saisonwechsel, passiver und aktiver Bundestrainer.
    /// Der aktive stellt jede Saison selbst auf (schwächster Torhüter, Reihe 4 besetzt, Taktik geändert): Länderspiele
    /// zählen nur für seine Reihen 1–3 und den Torhüter; zum Saisonwechsel endet das Amt samt Aufstellung.
    /// </summary>
    [Theory]
    [InlineData(false, 7)]
    [InlineData(true, 7)]
    [InlineData(true, 19)]
    public void WorldOnly_MultiSeason_WithChangedFormation(bool active, int seed)
    {
        var game = GameModeTests.Create(seed, false, true, 5);
        var account = new ClubAccountProbe(game, game.Managers[0]); // #732: Vereinskonto nur über das Finanzbuch
        for (var change = 0; change <= 4; change++)
        {
            Assert.Null(game.NationalCoach);
            game.Continue();
            var coach = game.NationalCoach!;
            Assert.Equal(0, coach.Formation.Count);
            Assert.Equal(0, coach.DressedCount);
            Dictionary<Player, int>? caps = null;
            if (active)
            {
                game.AutoNominate();
                coach.AutoFormation();
                // #192: Die Taktik gehört zum Nationalteam und bleibt über den Saisonwechsel stehen (Spur nationalteam).
                Assert.Equal(change == 0 ? TacticMode.Auto : TacticMode.Manual, coach.Tactics.EffortMode);
                coach.Tactics.EffortMode = TacticMode.Auto;
                var auto = game.NationalMatchStrength(NationalTeams.Germany);
                MakeRoom(coach);
                Assert.Equal(LineupResult.Assigned,
                    coach.Assign(LineupSlot.Goalkeeper, Weakest(coach, PlayerRole.Goalkeeper)));
                foreach (var p in coach.Rows.Where(p => p.Role == PlayerRole.Forward && !coach.Formation.Contains(p)).Take(1))
                {
                    if (!coach.IsDressed(p))
                        MakeRoom(coach);
                    Assert.Equal(LineupResult.Assigned, coach.Assign(LineupSlot.Forward(3, 0), p));
                }
                coach.Tactics.EffortMode = TacticMode.Manual;
                coach.Tactics.SetEffort(0, ScoreSituation.Base, 0);
                Assert.NotEqual(auto, game.NationalMatchStrength(NationalTeams.Germany));
                caps = coach.Rows.ToDictionary(p => p, p => p.Caps);
            }
            while (game.Phase == SeasonPhase.Running)
                game.Continue();

            Assert.Same(coach, game.NationalCoach);
            if (caps is not null)
            {
                var playing = coach.Lineup().Players.ToHashSet();
                Assert.Equal(16, playing.Count);
                foreach (var (p, before) in caps)
                    if (playing.Contains(p))
                        Assert.True(p.Caps > before, $"{p.Name}: keine Länderspiele gezählt");
                    else
                        Assert.Equal(before, p.Caps);
            }
            Assert.Equal(game.NationalAccount, game.NationalBookings.Sum(b => b.Amount));
            account.SeasonEnd();
            if (change < 4)
            {
                game.StartNextSeason();
                account.SeasonStart();
            }
        }
        Assert.Equal(5, game.History.Count);
    }
}
