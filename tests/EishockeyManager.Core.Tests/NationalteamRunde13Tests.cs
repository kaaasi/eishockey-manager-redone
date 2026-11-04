using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur nationalteam, Runde 13, Teil 1: Kader und Werte des Bundestrainers nach der DOSBox-Aufnahme `runde-12`
/// (<c>docs/analyse/re-runde-13.md</c>, Sitzung 1: A009, A010, A023, A085).
/// </summary>
public class NationalteamRunde13Tests
{
    /// <summary>Modus „nur Weltkugel“: Sprung zur WM, Manager 0 ist Bundestrainer, Kader „auto“.</summary>
    private static (GameState Game, NationalCoach Coach) Coach()
    {
        var game = GameModeTests.Create(7, false, true, 2);
        game.Continue();
        game.AutoNominate();
        return (game, game.NationalCoach!);
    }

    private static Player First(NationalCoach coach, PlayerRole role, int skip = 0) =>
        coach.Rows.Where(p => p.Role == role && p.Available).Skip(skip).First();

    // ---- A023 ----------------------------------------------------------------------------------------------------

    /// <summary>
    /// A023: [ORIGINAL] Felder 0x10–0x12 = Kondition, Technik, Form. FILE18 → FILE19 (x &gt; 2, Bonus 2): KÜNAST 79/81/53 →
    /// 80/83/55; ein Wert 99 (ZERWESZ, SCHINKO bei +0x10) bleibt 99, die übrigen +2.
    /// </summary>
    [Fact]
    public void A023_FriendlyBonus_AsFile18ToFile19()
    {
        var kuenast = new Player { Name = "KÜNAST", Role = PlayerRole.Goalkeeper, Condition = 79, Technique = 81, Form = 53 };
        NationalCoach.ApplyBonus(kuenast, x: 3, bonus: 2);
        Assert.Equal((80, 83, 55), (kuenast.Condition, kuenast.Technique, kuenast.Form));
        var zerwesz = new Player { Name = "ZERWESZ", Role = PlayerRole.Forward, Condition = 99, Technique = 90, Form = 54 };
        NationalCoach.ApplyBonus(zerwesz, x: 3, bonus: 2);
        Assert.Equal((99, 92, 56), (zerwesz.Condition, zerwesz.Technique, zerwesz.Form));
    }

    // ---- A085 ----------------------------------------------------------------------------------------------------

    /// <summary>
    /// A085: Regressionstest (vorher rot: Gleichstand nach dem Namen). [ORIGINAL] Bei gleicher Summe steht der zuerst
    /// durchsuchte Spieler vorn (Datei 0x3CC01, FILE18: LEE vor KUHNKE); [APPROX] im Remake der mit der kleineren Spieler-Id.
    /// </summary>
    [Fact]
    public void A085_AutoNominate_TieKeepsCandidateOrder_NotName()
    {
        var game = GameModeTests.Create(7, false, true, 2);
        game.Continue();
        var forwards = game.NationalCandidates().Select(c => c.Player).Where(p => p.Role == PlayerRole.Forward).ToList();
        var (early, late) = forwards.SelectMany(a => forwards.Select(b => (a, b)))
            .First(t => t.a.Id < t.b.Id && string.CompareOrdinal(t.a.Name, t.b.Name) > 0);
        foreach (var p in new[] { early, late })
            p.Condition = p.Technique = p.Form = Player.MaxValue;

        game.AutoNominate();
        var rows = game.NationalCoach!.Rows.Where(p => p.Role == PlayerRole.Forward).ToList();
        Assert.Equal([early, late], rows.Take(2));
        // [ORIGINAL] Plätze 0–2 Torhüter, 3–12 Verteidiger, 13–28 Stürmer (FILE18, Satz 5).
        Assert.Equal(Enumerable.Repeat(PlayerRole.Goalkeeper, 3).Concat(Enumerable.Repeat(PlayerRole.Defender, 10))
            .Concat(Enumerable.Repeat(PlayerRole.Forward, 16)), game.NationalCoach.Squad.Select(m => m.Player.Role));
    }

    // ---- A009 ----------------------------------------------------------------------------------------------------

    /// <summary>
    /// A009: Regressionstest (vorher rot: das Freundschaftsspiel schrieb die Länderwerte, #193). [ORIGINAL] FILE18 → FILE19:
    /// Werte von Verein 54 unverändert trotz Bonus und neuer Aufstellung; erst das Verlassen der Aufstellung rechnet.
    /// </summary>
    [Fact]
    public void A009_Friendly_DoesNotRecomputeNationalValues()
    {
        var (game, coach) = Coach();
        coach.AutoFormation();
        var before = game.NationalRatings(NationalTeams.Germany);
        Assert.Equal(FriendlyResult.Played, game.PlayFriendly(52, home: true));
        Assert.Equal(before, game.NationalRatings(NationalTeams.Germany));

        game.OnNationalLineupLeft();
        var (d, a, _) = MatchEngine.LineupTeamValues(coach.Formation);
        var (defense, attack, _) = game.NationalTeamValues(NationalTeams.Germany);
        Assert.Equal((d, a), (defense, attack));
        Assert.NotEqual(before, game.NationalRatings(NationalTeams.Germany));
    }

    /// <summary>
    /// A009: [ORIGINAL] Leere Plätze zählen 0 (Datei 0x09718–0x09771 ohne Sonderzweig). FILE20: nur JOORIS (88/96/57) in
    /// Reihe 1 Sturm → Felder 0x18–0x1D = 00 09 00 0A 00 06; die Form steht im Vereinsplatz (0x1C/0x1D), auch in der
    /// Stärketabelle.
    /// </summary>
    [Fact]
    public void A009_SingleForward_GivesFile20Values()
    {
        var (game, coach) = Coach();
        coach.ClearFormation();
        var jooris = First(coach, PlayerRole.Forward);
        jooris.Condition = 88;
        jooris.Technique = 96;
        jooris.Form = 57;
        Assert.Equal(LineupResult.Assigned, coach.Assign(LineupSlot.Forward(0, 0), jooris));

        game.OnNationalLineupLeft();
        var (defense, attack, _) = game.NationalTeamValues(NationalTeams.Germany);
        Assert.Equal((0, 0, 0, 9, 10, 6),
            (defense.Condition, defense.Technique, defense.Form, attack.Condition, attack.Technique, attack.Form));
        Assert.Equal((0, 0, 9, 10), game.NationalRatings(NationalTeams.Germany)); // (KA, TA, KS, TS)
        var club = game.Clubs[NationalTeams.Germany];
        Assert.Equal((0, 6), (club.TeamValues!.Value.DefenseForm, club.TeamValues.Value.AttackForm));
        var row = StrengthTable.Compute(game, [club], StrengthView.Sturm).Single();
        Assert.Equal((9, 10, 6), (row.Condition, row.Technique, row.Form));
    }

    /// <summary>
    /// A009: [ORIGINAL] Gelesen wird der Stand der letzten Rechnung, nicht die aktuelle Aufstellung; der Tageslauf vor den
    /// Spielen des Tages (nach dem Halt des Bundestrainers, Datei 0x555D6) rechnet neu.
    /// </summary>
    [Fact]
    public void A009_ValuesFollowLineupOnlyAtDayOrLeaving()
    {
        var (game, coach) = Coach();
        coach.AutoFormation();
        game.OnNationalLineupLeft();
        var computed = game.NationalTeamValues(NationalTeams.Germany);

        // Aufstellung ändern, ohne den Bildschirm zu verlassen: die Werte bleiben.
        coach.ClearSlot(LineupSlot.Forward(0, 0));
        coach.ClearSlot(LineupSlot.Forward(0, 1));
        Assert.Equal(computed, game.NationalTeamValues(NationalTeams.Germany));
        var expected = MatchEngine.LineupTeamValues(coach.Formation);
        Assert.NotEqual(computed.Attack, expected.Attack);

        // Der Tag (Weiterschalten): 0x952C vor den Spielen.
        var day = game.CurrentDay;
        game.Continue();
        Assert.True(game.CurrentDay > day);
        // Zwei Stürmer weniger in Reihe 1: Kondition des Sturms sinkt (genau: siehe A009_SingleForward_GivesFile20Values).
        Assert.True(game.NationalRatings(NationalTeams.Germany).ConditionAttack < computed.Attack.Condition);
    }

    // ---- A010 ----------------------------------------------------------------------------------------------------

    /// <summary>
    /// A010: Regressionstest. [ORIGINAL] Volle Reihe → „Kein / Platz mehr !“, keine Verdrängung (DOSBox `file19_08`: RESERVE-
    /// Verteidiger RYAN bei voller Reihe 1). Formation und Zahl der Eingesetzten bleiben, der Reservist bleibt Reservist.
    /// </summary>
    [Fact]
    public void A010_FullLine_NoRoom_NothingChanges()
    {
        var (_, coach) = Coach();
        coach.ClearFormation();
        var d0 = First(coach, PlayerRole.Defender);
        var d1 = First(coach, PlayerRole.Defender, 1);
        var reserve = First(coach, PlayerRole.Defender, 2);
        int? line = 0;
        Assert.Equal(LineupResult.Assigned, coach.ClickPlayer(d0, ref line, right: false));
        Assert.Equal(LineupResult.Assigned, coach.ClickPlayer(d1, ref line, right: false));
        Assert.Equal(LineupResult.Dressed, coach.ClickPlayer(reserve, ref line, right: true));
        Assert.Equal((d0, d1), (coach.Formation[LineupSlot.Defender(0, 0)], coach.Formation[LineupSlot.Defender(0, 1)]));
        var dressed = coach.DressedCount;

        Assert.Equal(LineupResult.NoRoom, coach.ClickPlayer(reserve, ref line, right: false));
        Assert.Equal((d0, d1), (coach.Formation[LineupSlot.Defender(0, 0)], coach.Formation[LineupSlot.Defender(0, 1)]));
        Assert.Equal(dressed, coach.DressedCount);
        Assert.Equal(PlayerStatus.Reserve, coach.StatusOf(reserve));
        Assert.Equal(0, line);

        // In Reihe 2 ist Platz: derselbe Klick setzt ihn auf den ersten freien Abwehrplatz.
        line = 1;
        Assert.Equal(LineupResult.Assigned, coach.ClickPlayer(reserve, ref line, right: false));
        Assert.Same(reserve, coach.Formation[LineupSlot.Defender(1, 0)]);
    }

    /// <summary>
    /// A010: [ORIGINAL] Linksklick auf einen Aufgestellten nimmt ihn heraus, er ist danach nicht eingesetzt, und seine Reihe
    /// wird die gewählte (`file19_06` → `file19_07`: Reihe 3 gewählt, ZERWESZ aus Reihe 1 → „Eingesetzt“ 22 → 21, Reihe 1
    /// gewählt). Rechtsklick nimmt ihn heraus und lässt ihn eingesetzt (Datei 0x411FE).
    /// </summary>
    [Fact]
    public void A010_ClickOnPlacedPlayer_TakesOutAndSelectsHisLine()
    {
        var (_, coach) = Coach();
        coach.AutoFormation();
        var zerwesz = coach.Formation[LineupSlot.Forward(0, 0)]!;
        var dressed = coach.DressedCount;
        int? line = 2;
        Assert.Equal(LineupResult.TakenOut, coach.ClickPlayer(zerwesz, ref line, right: false));
        Assert.Equal(0, line);
        Assert.Null(coach.Formation[LineupSlot.Forward(0, 0)]);
        Assert.False(coach.IsDressed(zerwesz));
        Assert.Equal(dressed - 1, coach.DressedCount);
        Assert.Null(coach.NumberOf(zerwesz));

        var other = coach.Formation[LineupSlot.Forward(1, 2)]!;
        Assert.Equal(LineupResult.TakenOut, coach.ClickPlayer(other, ref line, right: true));
        Assert.Equal(1, line);
        Assert.True(coach.IsDressed(other));
        Assert.Equal(PlayerStatus.Reserve, coach.StatusOf(other));
    }

    /// <summary>
    /// A010: [ORIGINAL] Ohne gewählte Reihe „Keine Reihe / ausgewählt !“; Torhüter nur in den Torhüterkasten, Feldspieler nicht
    /// dorthin (Datei 0x40E77–0x40EB9); Rechtsklick auf einen Reservisten nimmt ihn heraus (Datei 0x40F59).
    /// </summary>
    [Fact]
    public void A010_LineRulesAndRightClick()
    {
        var (_, coach) = Coach();
        coach.ClearFormation();
        var keeper = First(coach, PlayerRole.Goalkeeper);
        var forward = First(coach, PlayerRole.Forward);
        int? line = null;
        Assert.Equal(LineupResult.NoLineSelected, coach.ClickPlayer(forward, ref line, right: false));
        line = 0;
        Assert.Equal(LineupResult.WrongRole, coach.ClickPlayer(keeper, ref line, right: false));
        line = NationalCoach.GoalkeeperLine;
        Assert.Equal(LineupResult.WrongRole, coach.ClickPlayer(forward, ref line, right: false));
        Assert.Equal(LineupResult.Assigned, coach.ClickPlayer(keeper, ref line, right: false));
        Assert.Same(keeper, coach.Formation.Goalkeeper);
        Assert.Equal(0, coach.Formation.Count - 1);

        Assert.Equal(LineupResult.Dressed, coach.ClickPlayer(forward, ref line, right: true));
        Assert.Equal(LineupResult.Undressed, coach.ClickPlayer(forward, ref line, right: true));
        Assert.False(coach.IsDressed(forward));

        forward.InjuredGames = 2;
        Assert.Equal(LineupResult.Injured, coach.ClickPlayer(forward, ref line, right: true));
        Assert.False(coach.IsDressed(forward));
    }
}
