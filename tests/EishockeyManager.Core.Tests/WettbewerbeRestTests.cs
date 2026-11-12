using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur wettbewerbe-rest (Runde 17). Teil 1: Nationalteam/Länder (A008/A110, A084, A024).
/// </summary>
public class WettbewerbeRestTests
{
    private static GameState Create(int seed = 3, int club = 2)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Trainer", Head = 0, ClubIndex = club, StartLeague = NewGame.DatabaseLeague(club) ?? League.Oberliga });
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    /// <summary>Länderplatz ohne Kader wie <c>GameState.RatedNation</c>.</summary>
    private static Club Nation(GameState game, int index) =>
        new() { Index = index, Name = game.Clubs[index].Name, Strength = game.Clubs[index].Strength };

    private static GameState WithCoach()
    {
        var game = Create();
        game.AppointNationalCoach(game.Managers[0]);
        game.AutoNominate();
        game.NationalAccount = 10_000_000;
        return game;
    }

    /// <summary>
    /// A008: [ORIGINAL] Das Freundschaftsspiel (Datei 0x30365, Code 0xD) setzt den WM-Merker DGROUP 0x12B nicht – nur die
    /// WM-Gruppe 0x30F4E und die Endrunde 0x31012 tun das. Vorher rechnete das Remake es mit den WM-Regeln.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Friendly_UsesRulesWithoutWorldCupFlag(bool home)
    {
        var game = WithCoach();
        MatchEngine.LastRules = null;
        Assert.Equal(FriendlyResult.Played, game.PlayFriendly(54, home));
        var rules = Assert.IsType<MatchRules>(MatchEngine.LastRules);
        Assert.False(rules.WorldCup);
        Assert.False(rules.Playoff);
        Assert.False(rules.KnockOut);
    }

    /// <summary>
    /// A008: [ORIGINAL] Mit dem Bundestrainer setzt 0x544C0 dessen Satz aktiv (DGROUP 0xA729 + 5, Datei 0x545A6), der Merker
    /// 0x36FEA führt in die Minutenschleife 0x37088 – kein schneller Weg (Code 0xD hätte keinen Schalter, 0x3707B).
    /// </summary>
    [Fact]
    public void Friendly_WithCoach_RunsMinuteLoop()
    {
        var game = WithCoach();
        OriginalMinutes.FastCupCalls = 0;
        OriginalMinutes.MinuteLoopCalls = 0;
        Assert.Equal(FriendlyResult.Played, game.PlayFriendly(55, true));
        Assert.Equal(0, OriginalMinutes.FastCupCalls);
        Assert.Equal(1, OriginalMinutes.MinuteLoopCalls);
        Assert.True(MatchEngine.LastRules!.Value.ManagerMatch);
    }

    /// <summary>
    /// A008: Länderpaarung ohne Bundestrainer mit Code 0xD nimmt den schnellen Weg ohne Anzeigeschalter (Datei 0x3707B →
    /// 0x3731F) und ohne WM-Glättung (0x374EF, DGROUP 0x12B = 0) – auch bei eingeschaltetem WM-Schalter.
    /// </summary>
    [Fact]
    public void FriendlyWithoutCoach_TakesFastWay_EvenWithWorldCupSwitch()
    {
        var game = Create();
        game.Options.ShowWorldCup = true;
        OriginalMinutes.FastCupCalls = 0;
        OriginalMinutes.MinuteLoopCalls = 0;
        MatchEngine.PlayWithEvents(game, Nation(game, 54), Nation(game, 55), MatchCompetition.Friendly, knockOut: false,
            new Random(5));
        Assert.Equal(1, OriginalMinutes.FastCupCalls);
        Assert.Equal(0, OriginalMinutes.MinuteLoopCalls);
        Assert.False(MatchEngine.LastRules!.Value.WorldCup);
    }

    /// <summary>
    /// A084: [ORIGINAL] Die Strafen des Bundestrainer-Teams lesen DGROUP 0x974B + 6 · 0x1AF = 0xA165 (Datei 0x35890, Feld
    /// 0x16 = 6) – im Ergebnisblock 0xA0CB die Heimtore der sechsten Paarung des 13. Bundesliga-Spieltags. Vorher 20.
    /// </summary>
    [Fact]
    public void NationalPenaltyManagerValue_IsHomeGoalsOfBundesligaDay13Pairing6()
    {
        var game = Create();
        game.AppointNationalCoach(game.Managers[0]);
        game.AutoNominate();
        var nation = game.NationalCoach!.Nation;
        var club = new Club { Index = nation, Name = game.Clubs[nation].Name, Strength = game.Clubs[nation].Strength };
        Assert.Equal(0, MatchEngine.NationalPenaltyManagerValue(game)); // Spieltag 13 nicht gespielt: Byte 0
        Assert.Equal(0, MatchEngine.LiveTeamOf(game, club, Nation(game, 54), managed: true, managerMatch: true).PenaltyManagerValue);

        while (game.PlayedMatchDays < 13)
            TestSeason.PlayMatchDay(game);
        var day = game.Schedules[League.Bundesliga][12];
        var (home, away) = day.Matches[5];
        var expected = game.Results.Single(r => r.League == League.Bundesliga && r.MatchDay == day.Number
            && r.Home == home && r.Away == away).HomeGoals;
        Assert.Equal(expected, MatchEngine.NationalPenaltyManagerValue(game));
        Assert.Equal(expected,
            MatchEngine.LiveTeamOf(game, club, Nation(game, 54), managed: true, managerMatch: true).PenaltyManagerValue);
    }

    /// <summary>A084: Länder ohne Bundestrainer (Vereinsfeld 0x16 = 0) lesen keinen Managerwert (Datei 0x3588C).</summary>
    [Fact]
    public void NationWithoutCoach_KeepsNeutralPenaltyValue()
    {
        var game = Create();
        var team = MatchEngine.LiveTeamOf(game, Nation(game, 54), Nation(game, 55), managed: false, managerMatch: false);
        Assert.False(team.Managed);
        Assert.Equal(20, team.PenaltyManagerValue);
    }

    /// <summary>
    /// A024: [ORIGINAL] Leere Formation mit „automatisch aufstellen“ spielt mit der Aufstellung, die die Neuaufstellung 0x426BC
    /// für Satz 5 beim nächsten Menüaufbau setzt (Datei 0x450C6 → 0x426BC, Regel 0x42838) – gleiche Plätze, 22 eingesetzt.
    /// Vorher eine eigene Automatik nach Kondition + Technik + Form, alle Verfügbaren eingesetzt.
    /// </summary>
    [Fact]
    public void EmptyFormationWithAutoLineup_PlaysLineupOfMenuRebuild()
    {
        var game = WithCoach();
        var coach = game.NationalCoach!;
        Assert.True(coach.AutoLineupEnabled);
        Assert.Equal(0, coach.Formation.Count);
        var standIn = coach.Lineup();
        var standInSquad = MatchEngine.NationalSquad(game, coach);
        var standInValues = game.NationalPanelLineValue(coach.Nation, 0, attack: true);

        coach.AutoFormation(); // was das Original beim Menüaufbau macht
        var rebuilt = coach.Lineup();
        Assert.Equal(rebuilt.Goalkeeper, standIn.Goalkeeper);
        Assert.Equal(rebuilt.Defenders, standIn.Defenders);
        Assert.Equal(rebuilt.Forwards, standIn.Forwards);
        var squad = MatchEngine.NationalSquad(game, coach);
        Assert.Equal(squad.Players.Select(p => (p.Player, p.Position, p.Dressed)),
            standInSquad.Players.Select(p => (p.Player, p.Position, p.Dressed)));
        Assert.Equal(22, standInSquad.Players.Count(p => p.Dressed)); // [ORIGINAL] Ersatz reihum bis 22 (Datei 0x427C0)
        Assert.Equal(game.NationalPanelLineValue(coach.Nation, 0, attack: true), standInValues);
    }

    /// <summary>
    /// A024: [ORIGINAL] Reihenwerte der Mannschaftsfelder (Datei 0x3B65C): (Kondition + Technik) / 2 aus 0x977F ohne Spiel –
    /// für Länder ohne Bundestrainer Teamwerte + 4 − 4 · Reihe, begrenzt auf 0 … 100.
    /// </summary>
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, true)]
    public void PanelLineValue_OtherNation_IsTeamValuePlusLineShift(int line, bool attack)
    {
        var game = Create();
        var (defense, attackValues, _) = game.NationalTeamValues(55);
        var part = attack ? attackValues : defense;
        var shift = 4 - 4 * line;
        var expected = (Math.Clamp(part.Condition + shift, 0, 100) + Math.Clamp(part.Technique + shift, 0, 100)) / 2;
        Assert.Equal(expected, game.NationalPanelLineValue(55, line, attack));
    }

    /// <summary>
    /// A024: [ORIGINAL] Land des Bundestrainers: Spielerweg 0x98F8 (Vereinsfeld 0x16 = 6, Datei 0x97DC–0x97E2) aus den
    /// eingesetzten Spielern der Reihe.
    /// </summary>
    [Fact]
    public void PanelLineValue_CoachNation_FromSquad()
    {
        var game = WithCoach();
        var coach = game.NationalCoach!;
        coach.AutoFormation();
        var squad = MatchEngine.NationalSquad(game, coach);
        var v = MatchEngine.ManagedLineValues(squad, 1, 0, 0, 0, 0, inMatch: false);
        Assert.Equal((v.Condition + v.Technique) / 2, game.NationalPanelLineValue(coach.Nation, 1, attack: false));
        Assert.True(game.NationalPanelLineValue(coach.Nation, 1, attack: false) > 0);
    }
}
