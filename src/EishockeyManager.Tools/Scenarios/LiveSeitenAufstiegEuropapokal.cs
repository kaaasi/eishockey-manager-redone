using EishockeyManager.Core;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Screens.Live;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>
/// Spur live-seiten-rest (Rest S04): Aufstiegsrunde und Europapokal als Seiten im Spielablauf (0x31CD5 mit Merker „im Spiel“).
/// Europapokal mit den Ständen der Videobilder (Seiten im Spielablauf, <c>docs/analyse/live-spieltag.md</c> §11):
/// <c>eisbaeren_f51_03346</c> (Gruppe 2 nach Minute 60 des dritten Termins), <c>eisbaeren_f58_01323</c> (Endrunde nach Minute 60
/// des vierten Endrundentermins); Aufstiegsrunde aus einem gerechneten Spielstand am ersten Gruppenspieltag nach Minute 20.
/// </summary>
internal static class LiveSeitenAufstiegEuropapokal
{
    /// <summary>Gruppe a–d mit Ergebnissen je Runde „a-b|c-d“ als „l:r/l:r“ (wie <c>WettbewerbeVideo</c>).</summary>
    private static CupGroup Group(GameState game, int number, int firstDate, string[] teams, params string[] results)
    {
        var group = new CupGroup(number, teams.Select(t => WettbewerbeVideo.Club(game, t)).ToList());
        var dates = SeasonCalendar.DaysOf(DayEvents.EuropeanCup);
        for (var round = 0; round < 3; round++)
        {
            var pairs = group.Pairings(round);
            var legs = results[round].Split('|');
            for (var p = 0; p < 2; p++)
            {
                var (home, away) = pairs[p];
                var scores = legs[p].Split('/');
                for (var leg = 0; leg < scores.Length; leg++)
                {
                    var g = scores[leg].Split(':').Select(int.Parse).ToArray();
                    group.Results.Add(leg == 0
                        ? new KnockoutMatch(dates[firstDate + round], home, away, g[0], g[1])
                        : new KnockoutMatch(dates[firstDate + round + 3], away, home, g[1], g[0]));
                }
            }
        }
        return group;
    }

    [Scenario("live-aufstieg-europapokal")]
    private static void Run(ScreenDriver d)
    {
        d.StartNewGame();
        var game = d.Host.Game!;
        var back = new MainMenuScreen(d.Host);
        var dates = SeasonCalendar.DaysOf(DayEvents.EuropeanCup);
        var own = game.Managers[0].ClubIndex;
        game.Managers[0].ClubIndex = WettbewerbeVideo.Club(game, "EISB"); // nur für die Farbe der Europapokal-Bilder
        void Show(string name, UI.Screen screen)
        {
            d.Host.Show(screen);
            d.Move(0, 239);
            d.Save(name);
        }

        // Laufende Stände stehen hier schon als Ergebnis in der Gruppe (Rückfall von GroupLive.Score).
        Show("eisbaeren_f51_03346", new EuropeanCupScreen(d.Host, back, 1,
            Group(game, 2, 0, ["JOKERIT HELSINKI", "AMBRI-PIOTTA", "EISB", "IFK HELSINKI"], "4:3|6:2", "1:1|1:3", "3:3|2:2"),
            new GroupLive(60, dates[2], (_, _) => null)));
        Show("eisbaeren_f58_01323", new EuropeanCupScreen(d.Host, back, 2,
            Group(game, 3, EuropeanCup.GroupDates, ["EISB", "VITKOVICE", "KOSICE", "DUKLA TRENCIN"],
                "3:2/2:0|3:4/0:5", "3:2|4:3", "3:0|6:3"),
            new GroupLive(60, dates[EuropeanCup.GroupDates + 3], (_, _) => null)));

        game.Managers[0].ClubIndex = own;

        // Aufstiegsrunde: erster Gruppenspieltag nach Minute 20 (Gruppe 0 und 1). Ohne Managerverein in den Play-offs springt die
        // Zeitleiste bis zum Saisonende; daher ein zweites Spiel mit einem Verein der Gruppe 0 aus dem ersten Durchlauf.
        var first = PromotionRoundScreen.DayOf(0);
        while (game.Phase == SeasonPhase.Running)
            game.Continue();
        var candidates = game.RelegationGroups.Single(g => g.Upper == League.Bundesliga).Clubs;
        RelegationGroup? group = null;
        foreach (var club in candidates)
        {
            d.StartNewGame(clubIndex: club);
            game = d.Host.Game!;
            while (game.CurrentDay < first && game.Phase == SeasonPhase.Running)
                game.Continue();
            group = game.RelegationGroups.SingleOrDefault(g => g.Upper == League.Bundesliga);
            if (game.CurrentDay == first && group is { PlayedRounds: 0 })
                break;
            group = null;
        }
        if (group is null)
            throw new InvalidOperationException("kein Spielstand am ersten Gruppenspieltag");
        back = new MainMenuScreen(d.Host);
        var random = new Random(1);
        var day = LiveMatchDay.Begin(game, group.Rounds[0].Matches.Select(m =>
            new LiveFixture(game.Clubs[m.Home], game.Clubs[m.Away], MatchCompetition.Relegation, false, random)).ToList());
        while (day.Minute < 20 && day.Step())
        {
        }
        var live = new GroupLive(20, first, (l, r) => LivePageScreens.Score(day, l, r, 20, false));
        Show("aufstieg_live_1", new PromotionRoundScreen(d.Host, back, League.ZweiteLiga, live));
        Show("aufstieg_live_2", new PromotionRoundScreen(d.Host, back, League.Oberliga, live));
    }
}
