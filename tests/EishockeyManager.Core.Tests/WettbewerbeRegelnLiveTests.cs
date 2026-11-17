using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur wettbewerbe-regeln (Runde 22), Teil 3 und A553: Taktikschreiber und Live-Einzelregeln. Je Befund ein Test mit Fundstelle
/// in em.exe.
/// </summary>
public class WettbewerbeRegelnLiveTests
{
    private static (GameState Game, Club Own, Club Opponent) ManagedPair()
    {
        var game = TestGames.Create(3);
        MatchEngine.EnsureTeamValues(game);
        var own = game.Clubs[game.Managers[0].ClubIndex];
        var opponent = game.Clubs[own.Index == 4 ? 5 : 4];
        return (game, own, opponent);
    }

    /// <summary>
    /// A553: [ORIGINAL] 0x8365 schreibt Vereinsfeld 0x26 nur ohne Manager bzw. bei „AUTOM.“ (Datei 0x8443–0x847D), die
    /// Angriffsbits 0x2C nie (Datei 0x8481–0x84BD). Ein Managerverein mit „SEMI“/„MANUELL“ beginnt also mit den Feldern vom Ende
    /// des Vorspiels bzw. vom letzten Verlassen des Taktikbildschirms. Vorher (rot): eigene Werte beim Stand 0:0 (Grundwert +
    /// „Unentschieden“, eigener Angriff).
    /// </summary>
    [Theory]
    [InlineData(TacticMode.Semi)]
    [InlineData(TacticMode.Manual)]
    public void ManagedSemiOrManual_StartsWithMatchFields(TacticMode mode)
    {
        var (game, own, opponent) = ManagedPair();
        var t = own.Tactics;
        t.EffortMode = mode;
        t.AttackMode = mode;
        t.SetEffort(0, ScoreSituation.Base, 50);
        t.SetEffort(0, ScoreSituation.Tied, 10);
        t.SetAttack(0, ScoreSituation.Base, 5);
        t.SetAttack(0, ScoreSituation.Tied, 1);
        t.RecordMatchFields(33, 2);

        var team = MatchEngine.LiveTeamOf(game, own, opponent, managed: true, managerMatch: true);
        var other = MatchEngine.LiveTeamOf(game, opponent, own, managed: false, managerMatch: true);
        MatchEngine.PrepareMatch(game, own, opponent, team, other);

        Assert.Equal((33, 2), (team.Effort, team.AttackLevel));
    }

    /// <summary>
    /// A553: [ORIGINAL] Der Taktikbildschirm setzt beim Aufbau 0x26 = 0x2D (Datei 0x48003–0x48007) und beim Verlassen in
    /// 0x48E62 (Aufruf Datei 0x48E19) 0x2D = 0x26, 0x26 = 0x2D + 0x29 (Datei 0x4902B–0x4903E): Vereinsfeld 0x26 = Grundwert +
    /// Zuschlag „Unentschieden“. Die Angriffsbits schreibt er nicht.
    /// </summary>
    [Theory]
    [InlineData(TacticMode.Semi)]
    [InlineData(TacticMode.Manual)]
    public void TacticsLeft_WritesBasePlusTiedEffort_KeepsAttack(TacticMode mode)
    {
        var (game, own, opponent) = ManagedPair();
        var t = own.Tactics;
        t.EffortMode = mode;
        t.AttackMode = mode;
        t.SetEffort(0, ScoreSituation.Base, 50);
        t.SetEffort(0, ScoreSituation.Tied, 10);
        t.SetAttack(0, ScoreSituation.Base, 5);
        t.RecordMatchFields(33, 2);

        game.OnTacticsLeft(own);

        Assert.Equal((60, 2), (t.MatchEffort, t.MatchAttack));
        var team = MatchEngine.LiveTeamOf(game, own, opponent, managed: true, managerMatch: true);
        var other = MatchEngine.LiveTeamOf(game, opponent, own, managed: false, managerMatch: true);
        MatchEngine.PrepareMatch(game, own, opponent, team, other);
        Assert.Equal((60, 2), (team.Effort, team.AttackLevel));
    }

    /// <summary>
    /// A553: [ORIGINAL] 0x48E62 schreibt ohne Blick auf den Modus; bei „AUTOM.“ stehen in 0x27–0x2B/0x2D die Zeilen der
    /// Automatik (<see cref="Tactics.ShownEffort"/>). Das Feld liest danach die Stärke 0x368B1 der Vorbereitung.
    /// </summary>
    [Fact]
    public void TacticsLeft_Auto_WritesShownAutoLine()
    {
        var (game, own, _) = ManagedPair();
        var t = own.Tactics;
        t.EffortMode = TacticMode.Auto;
        t.RecordMatchFields(33, 4);

        game.OnTacticsLeft(own);

        var expected = t.ShownEffort(0, ScoreSituation.Base) + t.ShownEffort(0, ScoreSituation.Tied);
        Assert.Equal((expected, 4), (t.MatchEffort, t.MatchAttack));
    }

    /// <summary>Nur Managervereine (der Bildschirm zeigt den Verein des Managers am Zug); Computervereine bleiben unverändert.</summary>
    [Fact]
    public void TacticsLeft_ComputerClub_Unchanged()
    {
        var (game, _, opponent) = ManagedPair();
        opponent.Tactics.RecordMatchFields(33, 4);

        game.OnTacticsLeft(opponent);

        Assert.Equal((33, 4), (opponent.Tactics.MatchEffort, opponent.Tactics.MatchAttack));
    }
}
