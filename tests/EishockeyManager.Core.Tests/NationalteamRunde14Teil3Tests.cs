using System.Text.Json.Nodes;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur nationalteam, Runde 14, Teil 3: A009 – Einstellung „automatisch aufstellen“ des Bundestrainers (DGROUP 0x37F aktuell,
/// 0x380 gesichert; <c>docs/analyse/re-runde-14.md</c> Sitzung 5, L8). Vor dem Fix rot: Das Remake kannte die Einstellung nicht,
/// setzte bei leerer Formation seine eigene Automatik ein und schrieb dann keine Länderwerte.
/// </summary>
public class NationalteamRunde14Teil3Tests
{
    /// <summary>„nur Weltkugel“, erster WM-Tag: Ernennung, Kader „auto“, Rückkehr ins Menü (Datei 0x450C6).</summary>
    private static (GameState Game, NationalCoach Coach) CoachWithSquad(int seed = 7)
    {
        var game = GameModeTests.Create(seed, false, true, 2);
        game.Continue();
        game.AutoNominate();
        game.OnMainMenu(game.ActiveManager, isReturn: true);
        return (game, game.NationalCoach!);
    }

    /// <summary>
    /// [ORIGINAL] Vorgabe an (FILE11/18/49: 0x37F = 0x380 = 1); der Aufbau des Menüs stellt neu auf (Datei 0x450C6 → 0x426BC):
    /// 22 eingesetzt wie nach „auto“ und Rückkehr (FILE49A: der neue ENGLBRECHT ist eingesetzt).
    /// </summary>
    [Fact]
    public void MenuBuild_WithDefaultSetting_LinesUpTheSquad()
    {
        var (_, coach) = CoachWithSquad();
        Assert.True(coach.AutoLineupEnabled);
        Assert.Equal(NationalCoach.MaxDressed, coach.DressedCount);
        Assert.True(coach.Formation.Count > 0);
    }

    /// <summary>
    /// [ORIGINAL] A009, FILE47/48: Leeren schaltet die Einstellung ab (0x37F 1 → 0, Datei 0x40ADE), die Reserve bleibt eingesetzt,
    /// das Verlassen der Aufstellung schreibt 0 in die Länderwerte (0x18–0x1D, FILE47). Das Weiterschalten sichert 0 (FILE48
    /// `00 00`); die leere Aufstellung spielt (kein Spieler bekommt ein Spiel) und bleibt im nächsten Zug leer.
    /// </summary>
    [Fact]
    public void ClearedLineup_SwitchesOff_ValuesZero_PlaysEmpty()
    {
        var (game, coach) = CoachWithSquad();
        var reserve = coach.Squad.Select(m => m.Player).Count(p => coach.IsDressed(p) && !coach.Formation.Contains(p));
        coach.ClearFormation();
        Assert.False(coach.AutoLineupEnabled);
        Assert.Equal(0, coach.Formation.Count);
        Assert.Equal(reserve, coach.DressedCount);

        game.OnNationalLineupLeft();
        Assert.Equal((0, 0, 0, 0), game.NationalRatings(NationalTeams.Germany));
        var values = game.Clubs[NationalTeams.Germany].TeamValues!.Value;
        Assert.Equal((0, 0), (values.DefenseForm, values.AttackForm));

        var games = coach.Squad.Sum(m => m.Player.Games);
        game.OnMainMenu(game.ActiveManager, isReturn: true); // Rückkehr: Einstellung aus, keine Neuaufstellung
        Assert.Equal(0, coach.Formation.Count);
        game.Continue(); // erster WM-Termin
        Assert.Equal(1, game.WorldCupStage);
        Assert.Equal(games, coach.Squad.Sum(m => m.Player.Games));
        Assert.Empty(coach.Lineup().Players);

        game.OnMainMenu(game.ActiveManager, isReturn: false); // nächster Zug: 0x37F := 0x380 = 0
        Assert.False(coach.AutoLineupEnabled);
        Assert.Equal(0, coach.Formation.Count);
    }

    /// <summary>
    /// [ORIGINAL] A009, FILE47: Leeren, Sichern im selben Zug (0x37F = 0, 0x380 = 1), Laden → der Zug beginnt neu (0x37F := 0x380)
    /// und das Menü stellt wieder auf (22 eingesetzt).
    /// </summary>
    [Fact]
    public void ClearedLineup_SavedInTurn_LoadLinesUpAgain()
    {
        var (game, coach) = CoachWithSquad();
        coach.ClearFormation();
        var loaded = SaveGame.Load(SaveGame.Save(game)).Game!;
        Assert.Equal(0, loaded.NationalCoach!.Formation.Count);
        loaded.OnMainMenu(loaded.ActiveManager, isReturn: false);
        Assert.True(loaded.NationalCoach.AutoLineupEnabled);
        Assert.Equal(NationalCoach.MaxDressed, loaded.NationalCoach.DressedCount);
    }

    /// <summary>
    /// [ORIGINAL] Ein Klick in der Kaderliste schaltet ab, auch wenn er nichts ändert (Datei 0x40DCD–0x40DE4 vor den Prüfungen);
    /// ein Klick auf einen leeren Platz nicht (Datei 0x40AC4).
    /// </summary>
    [Fact]
    public void Click_SwitchesOff_EmptySlotDoesNot()
    {
        var (_, coach) = CoachWithSquad();
        var empty = LineupSlot.All.Where(s => coach.Formation[s] is null).Cast<LineupSlot?>().FirstOrDefault();
        if (empty is { } slot)
        {
            coach.ClearSlot(slot);
            Assert.True(coach.AutoLineupEnabled);
        }
        int? line = null;
        var player = coach.Squad[0].Player;
        coach.ClickPlayer(player, ref line, right: false);
        Assert.False(coach.AutoLineupEnabled);
        coach.AutoFormation(); // „A“ schaltet wieder ein ([APPROX] wie beim Verein)
        Assert.True(coach.AutoLineupEnabled);
    }

    /// <summary>
    /// Mehrsaisonprobe (4 Saisonwechsel, „nur Weltkugel“, passiv und aktiv): [ORIGINAL] Die Bytes 0x37F/0x380 sind global und
    /// überdauern das Amt (kein Schreiber im Saisonwechsel). Der passive Bundestrainer spielt jede WM mit voller Aufstellung; der
    /// aktive leert sie im ersten Zug der ersten Saison – danach bleibt die Einstellung über alle Saisons aus.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FourSeasons_SettingCarriesOver(bool active)
    {
        var game = GameModeTests.Create(5, false, true, 2);
        for (var season = 0; season < 5; season++)
        {
            game.Continue();
            var coach = game.NationalCoach!;
            game.AutoNominate();
            game.OnMainMenu(game.ActiveManager, isReturn: true);
            Assert.Equal(!active || season == 0, coach.AutoLineupEnabled);
            if (active && season == 0)
                coach.ClearFormation();
            Assert.Equal(!active, coach.Formation.Count > 0);
            for (var guard = 0; game.Phase == SeasonPhase.Running; guard++)
            {
                Assert.True(guard < 50);
                game.Continue();
                if (game.Phase == SeasonPhase.Running)
                {
                    game.OnMainMenu(game.ActiveManager, isReturn: false);
                    Assert.Equal(!active, coach.Formation.Count > 0);
                    Assert.True(coach.DressedCount <= NationalCoach.MaxDressed);
                }
            }
            if (season < 4)
                game.StartNextSeason();
        }
    }

    /// <summary>
    /// Migration 40 → 41: Ein Bundestrainer mit eigener Aufstellung behält sie (Einstellung aus), einer ohne bekommt den Startwert 1.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Migration_SetsSettingFromFormation(bool handMade)
    {
        var (game, coach) = CoachWithSquad();
        coach.ClearFormation();
        if (handMade)
        {
            var keeper = coach.Squad.Select(m => m.Player).First(p => p.Role == Formats.PlayerRole.Goalkeeper && p.Available);
            Assert.Equal(LineupResult.Assigned, coach.Assign(LineupSlot.Goalkeeper, keeper));
        }
        var root = JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        root["schema"] = 40;
        Assert.True(root["spiel"]!.AsObject().Remove("_nationalAutoLineupSaved"));
        var json = root["spiel"]!["NationalCoach"]!.AsObject();
        foreach (var key in new[] { "_autoLineup", "_autoLineupSaved", "_turnOpen" })
            Assert.True(json.Remove(key), key);
        var result = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.True(result.Success, result.Error);
        var loaded = result.Game!;
        Assert.Equal(!handMade, loaded.NationalCoach!.AutoLineupEnabled);
        loaded.OnMainMenu(loaded.ActiveManager, isReturn: false);
        if (handMade)
            Assert.Equal(1, loaded.NationalCoach.Formation.Count);
        else
            Assert.Equal(NationalCoach.MaxDressed, loaded.NationalCoach.DressedCount);
    }

    /// <summary>
    /// Kette 39 → 40 → 41 (Integration Runde 14): Ein echter Schema-39-Stand kennt weder <c>ManagerDayMinuteLoop</c> (saison, 40)
    /// noch die Felder der Bundestrainer-Einstellung (nationalteam, 41); beide Migrationen laufen nacheinander und ändern sich nicht.
    /// </summary>
    [Fact]
    public void Migration_Chain39To41_AddsBothSettings()
    {
        var (game, coach) = CoachWithSquad();
        coach.ClearFormation();
        var root = JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        root["schema"] = 39;
        Assert.True(root["spiel"]!["Setup"]!["Options"]!.AsObject().Remove("ManagerDayMinuteLoop"));
        Assert.True(root["spiel"]!.AsObject().Remove("_nationalAutoLineupSaved"));
        var json = root["spiel"]!["NationalCoach"]!.AsObject();
        foreach (var key in new[] { "_autoLineup", "_autoLineupSaved", "_turnOpen" })
            Assert.True(json.Remove(key), key);
        var result = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.True(result.Success, result.Error);
        Assert.False(result.Game!.Options.ManagerDayMinuteLoop);
        Assert.True(result.Game.NationalCoach!.AutoLineupEnabled);
        Assert.Equal(SaveGame.Save(result.Game), SaveGame.Save(SaveGame.Load(SaveGame.Save(result.Game)).Game!));
    }

    /// <summary>
    /// [ORIGINAL] A085 (FILE49 → FILE49A): Bei vollem Kader bewirkt ein Klick auf einen weiteren Spieler nichts; Herausnehmen eines
    /// Torhüters und Aufnehmen eines anderen setzt den Neuen auf den freien Torhüterplatz (Zeile 3), eingesetzt erst durch den
    /// nächsten Menüaufbau mit „automatisch aufstellen“.
    /// </summary>
    [Fact]
    public void FullSquad_ClickDoesNothing_KeeperSwapTakesRowThree()
    {
        var (game, coach) = CoachWithSquad();
        Assert.Equal(NationalCoach.MaxSquad, coach.Squad.Count);
        var squad = coach.Squad.Select(m => m.Player).ToList();
        var outsider = game.NationalCandidates().Select(c => c.Player)
            .First(p => !coach.Contains(p) && p.InjuredGames == 0 && p.Role == Formats.PlayerRole.Goalkeeper);
        Assert.Equal(NominationResult.SquadFull, game.ToggleNomination(outsider));
        Assert.Equal(squad, coach.Squad.Select(m => m.Player));

        var third = coach.Rows[2];
        Assert.Equal(Formats.PlayerRole.Goalkeeper, third.Role);
        Assert.Equal(NominationResult.Removed, game.ToggleNomination(third));
        Assert.Equal(NominationResult.Added, game.ToggleNomination(outsider));
        Assert.Same(outsider, coach.Rows[2]);
        Assert.False(coach.IsDressed(outsider));
        game.OnMainMenu(game.ActiveManager, isReturn: true);
        Assert.Equal(NationalCoach.MaxDressed, coach.DressedCount);
    }
}
