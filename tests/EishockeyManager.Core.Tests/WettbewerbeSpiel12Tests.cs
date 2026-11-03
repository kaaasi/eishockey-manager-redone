using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur wettbewerbe-spiel, Runde 12, Teil 1: Serien der Pokal- und Play-off-Spiele (R11A-01) und Neuzählung der
/// Feldspieler 0x38A36 (WS-04, Review #468).
/// </summary>
public class WettbewerbeSpiel12Tests
{
    // ---- R11A-01: Serien nur aus Liga (Code 7) und WM-Gruppe (Code 8) ------------------------------------------------

    /// <summary>
    /// R11A-01 (Review #478): Die Lücke „Serien der Pokal-/Play-off-Spiele“ besteht im Original nicht. [ORIGINAL] Die
    /// Ergebniseintragung 0x50283 schreibt nach der Begegnungstabelle (Datei 0x502CC–0x5030B) alles Weitere – Tabellenfelder,
    /// Serien Datei 0x50443–0x50AB4, Tagesform – nur bei Wettbewerbscode DGROUP 0xD0C2 = 7 oder 8; sonst Sprung ans
    /// Funktionsende (Datei 0x5030D–0x5031B → 0x50B41). Die Codes setzen: Liga 7 (Datei 0x35EC4), WM-Gruppe 8 (0x30F53),
    /// DEB-Pokal 6 (0x35E7C), Play-off- und Aufstiegsrundentag 1 (0x31AB7, beide über 0x36B5D), Europapokal 3 (0x31490),
    /// WM-Endrunde 10 (0x31017), Freundschaftsspiel 0xD (0x30390). Andere Schreiber der Serien DGROUP 0xB57B gibt es nur im
    /// Rekordabgleich 0x50D4B und beim Tausch der Vereinssätze 0x36554.
    /// <para>Probe über eine ganze Saison: Die laufenden Serien jedes Vereins sind genau die Fortschreibung seiner
    /// Ligaergebnisse; DEB-Pokal, Play-offs, Aufstiegsrunden und Europapokal ändern nichts. Gegenprobe: ein Aufruf von
    /// <c>CountSeries</c> in <see cref="GameState.RecordMeeting"/> (also für Pokal und Play-offs) macht den Test rot.</para>
    /// </summary>
    [Theory]
    [InlineData(3)]
    [InlineData(7)]
    public void Series_OnlyLeagueResults_NotCupsOrPostseason(int seed)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Oben", Head = 0, ClubIndex = 0 });
        setup.Managers.Add(new ManagerSetup { Name = "Mitte", Head = 1, ClubIndex = 12 });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, seed);
        var clubs = Enumerable.Range(0, MeetingHistory.FirstNation).Where(i => i < game.Clubs.Count).ToList();
        var expected = clubs.ToDictionary(i => i, i => game.Clubs[i].SeriesCurrent.ToArray());

        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 2000, "Saisonende nicht erreicht");
            foreach (var m in game.Managers)
                m.Money = Math.Max(m.Money, 10_000_000);
            TestSquadKeeper.KeepPlayable(game, game.Managers[0]);
            TestSquadKeeper.KeepPlayable(game, game.Managers[1]);
            game.Continue();
        }

        foreach (var r in game.Results)
        {
            Series.Enter(expected[r.Home], r.HomeGoals, r.AwayGoals, home: true);
            Series.Enter(expected[r.Away], r.AwayGoals, r.HomeGoals, home: false);
        }
        foreach (var i in clubs)
            Assert.True(expected[i].SequenceEqual(game.Clubs[i].SeriesCurrent), $"Serien Verein {i} weichen ab");

        // Nicht leer: Es gab Pokal- und Nachsaisonspiele deutscher Vereine, deren Ausgang die Serien geändert hätte.
        Assert.Contains(game.DebCup!.Matches, m => m.Home < MeetingHistory.FirstNation && m.Away < MeetingHistory.FirstNation);
        Assert.NotEmpty(game.PlayoffResults);
        Assert.NotEmpty(game.RelegationGroups.SelectMany(g => g.Results));
    }

    // ---- WS-04: Neuzählung 0x38A36, Grenze 5 und Abzug vom Sturm ------------------------------------------------------

    private static Player P(PlayerRole role, int number) =>
        new() { Name = $"{role}{number}", Role = role, Condition = 50, Technique = 50, Form = 50, Number = number };

    private static (LiveTeam Team, LiveSquad Squad) Team(int defenders, int forwards, bool goalie)
    {
        var squad = new LiveSquad();
        if (goalie)
            squad.Players.Add(new LivePlayer { Player = P(PlayerRole.Goalkeeper, 1), Position = 1, Dressed = true });
        for (var i = 0; i < defenders; i++)
            squad.Players.Add(new LivePlayer { Player = P(PlayerRole.Defender, 2 + i), Position = 1, Dressed = true });
        for (var i = 0; i < forwards; i++)
            squad.Players.Add(new LivePlayer { Player = P(PlayerRole.Forward, 10 + i), Position = 0x81, Dressed = true });
        return (new LiveTeam { Squad = squad, Managed = true, InjuryManager = 0 }, squad);
    }

    /// <summary>
    /// WS-04, Grenze 5. [ORIGINAL] Datei 0x38ABE–0x38ADA: Grenze 6, ohne Torhüter (0x388F6 Modus 0 = 0) und mit
    /// d + f &lt; 6 nur 5; Datei 0x38AFA–0x38B0F: g + d + f + b über der Grenze → f = 3 − Überschuss.
    /// Ohne Torhüter, 2 + 3 auf dem Eis und ein Spieler auf der Strafbank: 0 + 2 + 3 + 1 = 6 &gt; 5 → Sturm 2.
    /// Gegenprobe: Grenze fest 6 lässt den Sturm bei 3 (rot).
    /// </summary>
    [Fact]
    public void Recount_NoGoalie_PenaltyBox_LimitFiveCutsForward()
    {
        var (team, squad) = Team(2, 3, goalie: false);
        team.Box.Add(0, 2);
        MatchEngine.RecountSkaters(team);
        Assert.Equal([2, 2], squad.SkatersPerPart);
        Assert.Equal(4, team.Box.Skaters);
        Assert.False(team.GoaliePulled);
    }

    /// <summary>
    /// WS-04, Grenze 6 ohne Torhüter. [ORIGINAL] Datei 0x38AC9–0x38ADA: ohne Torhüter, aber d + f ≥ 6 bleibt die Grenze 6;
    /// 3 + 3 = 6 ist nicht darüber, Sturm 3, und d + f = 6 setzt Vereinsfeld 0x2C Bit 3 (Datei 0x38B5A–0x38B6F).
    /// Gegenprobe: Grenze ohne Torhüter immer 5 → Sturm 2 (rot).
    /// </summary>
    [Fact]
    public void Recount_NoGoalie_SixSkaters_KeepsLimitSix()
    {
        var (team, squad) = Team(3, 3, goalie: false);
        MatchEngine.RecountSkaters(team);
        Assert.Equal([3, 3], squad.SkatersPerPart);
        Assert.Equal(6, team.Box.Skaters);
        Assert.True(team.GoaliePulled);
    }

    /// <summary>
    /// WS-04, Abzug vom Sturm. [ORIGINAL] Datei 0x38B0B–0x38B0F: <c>mov dl, 3; sub dl, al</c> – der Sturm wird auf
    /// 3 − Überschuss gesetzt, nicht die gezählten Stürmer um den Überschuss vermindert. Reihe mit zwei Stürmern und zwei
    /// Strafbankplätzen: 1 + 2 + 2 + 2 = 7 &gt; 6 → Sturm 3 − 1 = 2 (gezählt − Überschuss wäre 1).
    /// </summary>
    [Fact]
    public void Recount_TwoForwards_TwoInBox_ForwardsThreeMinusExcess()
    {
        var (team, squad) = Team(2, 2, goalie: true);
        team.Box.Add(0, 2);
        team.Box.Add(1, 2);
        MatchEngine.RecountSkaters(team);
        Assert.Equal([2, 2], squad.SkatersPerPart);
        Assert.Equal(4, team.Box.Skaters);
    }
}
