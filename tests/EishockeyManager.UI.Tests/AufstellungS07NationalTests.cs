using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Screens.Live;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur <c>aufstellung</c>, Teil 3 S07 (A874): Länderspiele des Bundestrainers zählen wie beim Verein beim Spielbeginn (Abrechnung
/// 0x544C0 vor den Minuten am Satz 5, Datei 0x54910–0x5499E) und beim Aufstellen im Spiel sofort (Datei 0x415B7–0x41622), am
/// Spielende nicht erneut. WM: Feld Europapokal (+0x08/+0x20, Wettbewerb 8).
/// </summary>
public sealed class AufstellungS07NationalTests
{
    /// <summary>
    /// In Minute 5 haben die Aufgestellten ihr Länderspiel schon; ein im Spiel neu Aufgestellter bekommt es sofort; nach dem Spielende
    /// haben alle genau dieses eine. Vorher (rot): gezählt erst am Spielende aus der Aufstellung.
    /// </summary>
    [GameDataFact]
    public void WorldCupMatch_GamesCountAtKickOffAndPlacement_NotAgainAtEnd()
    {
        var (d, _, coach) = NationalCoachLiveInterventionScreenTests.StartWorldCup();
        var (_, match, side) = NationalCoachLiveInterventionScreenTests.RunToCoachMatch(d, coach);
        var team = side == 0 ? match.HomeTeam! : match.AwayTeam!;
        var nation = side == 0 ? match.Home : match.Away;
        var squad = team.Squad!;
        var starters = squad.Players.Where(p => p.Position != 0 && p.Dressed).Select(p => p.Player).ToList();
        Assert.NotEmpty(starters);
        Assert.All(squad.Players.Where(p => p.Position != 0 && p.Dressed), p => Assert.True(p.Played));
        var atMinute5 = squad.Players.ToDictionary(p => p.Player, p => (p.Player.Games, p.Player.EuropeGames));

        coach.SwitchOffAutoLineup();
        var bench = squad.Players.First(p => p.Position == 0 && p.Player.Available && p.Player.Role == PlayerRole.Forward
            && !coach.Formation.Contains(p.Player)).Player;
        var slot = LineupSlot.All.First(s => s.Role == PlayerRole.Forward && coach.Formation[s] is not null);
        coach.ClearSlotInMatch(slot);
        int? line = slot.Block;
        Assert.Equal(LineupResult.Assigned, coach.ClickPlayerInMatch(bench, ref line, right: false, nation));
        Assert.Equal(atMinute5[bench].Games + 1, bench.Games);
        Assert.Equal(atMinute5[bench].EuropeGames + 1, bench.EuropeGames);
        MatchEngine.ApplyNationalLineup(team, coach);
        var keeper = coach.SourceOf(Assert.IsType<Player>(coach.Lineup().Goalkeeper));
        var minutes = keeper.GoalieMinutes;

        // Spur live-verschraenkt Teil 3 (S04, R15-E2 = C): [ORIGINAL] nach dem Drittel zeigt der Seitenwähler 0x3A88A die
        // WM-Seiten mit Minute (Schalter WM-Spiele, beim Bundestrainer an; Datei 0x3858C–0x386CD, drittelseiten.py); Rechtsklick
        // führt zurück ins Spiel.
        for (var frames = 0; d.Host.Current is LiveMatchScreen or CupPairingsScreen or WorldCupKnockoutScreen; frames++)
        {
            Assert.True(frames < 200_000, "Spiel endet nicht");
            if (d.Host.Current is LiveMatchScreen { WaitingForClick: true })
                d.Click(160, 120);
            else if (d.Host.Current is CupPairingsScreen or WorldCupKnockoutScreen)
                d.RightClick(5, 5);
            else
                d.Frame();
        }

        Assert.True(keeper.GoalieMinutes > minutes); // die Abrechnung am Spielende ist gelaufen (CreditWorldCupSquad)
        foreach (var p in starters)
            Assert.Equal(atMinute5[p], (p.Games, p.EuropeGames));
        Assert.Equal(atMinute5[bench].Games + 1, bench.Games);
        Assert.Equal(atMinute5[bench].EuropeGames + 1, bench.EuropeGames);
    }
}
