using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens.Team;

namespace EishockeyManager.UI.Tests;

/// <summary>Spur wettbewerbe-regeln (Runde 22): Anschluss der Kernereignisse an die Bildschirme.</summary>
public class WettbewerbeRegelnUiTests
{
    /// <summary>
    /// A553: [ORIGINAL] Verlassen des Taktikbildschirms ruft 0x48E62 (Aufruf Datei 0x48E19): Vereinsfeld 0x26 = Grundwert +
    /// „Unentschieden“ (Datei 0x4902B–0x4903E), Angriff bleibt. Bloßes Öffnen ändert nichts (AGENTS.md §4).
    /// </summary>
    [GameDataFact]
    public void TacticsScreen_Leave_WritesMatchEffort()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var t = game.Clubs[game.ActiveManager.ClubIndex].Tactics;
        t.EffortMode = TacticMode.Semi;
        t.SetEffort(0, ScoreSituation.Base, 50);
        t.SetEffort(0, ScoreSituation.Tied, 10);
        t.RecordMatchFields(33, 2);

        d.OpenTile("Taktik");
        Assert.IsType<TacticsScreen>(d.Host.Current);
        Assert.Equal((33, 2), (t.MatchEffort, t.MatchAttack));
        d.Press(Key.Escape);

        Assert.IsNotType<TacticsScreen>(d.Host.Current);
        Assert.Equal((60, 2), (t.MatchEffort, t.MatchAttack));
    }
}
