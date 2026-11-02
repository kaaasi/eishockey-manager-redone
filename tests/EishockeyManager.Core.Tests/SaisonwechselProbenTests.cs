using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur saisonwechsel, Teil 4 (Proben und Altlasten): Startordnung geladener Spielstände ab Saison 2 (#329, R9W-D02) und
/// Play-offs ohne Managerverein mit jedem Feld „Ergebnisse“ der Meldungsmatrix (#362, PSW-01).
/// </summary>
public class SaisonwechselProbenTests
{
    private static readonly TableView[] Views = [TableView.Gesamt, TableView.Heim, TableView.Auswaerts];

    /// <summary>
    /// #329: Ab Saison 2 beginnt die Platzliste mit der Ordnung der Vorsaison nach Auf- und Abstieg
    /// (<see cref="LeagueTable.SuccessorOrder"/>, em.exe Datei 0x55E39–0x55EF8, 0x36169). Ein gesicherter und wieder geladener
    /// Spielstand muss jede Tabelle der laufenden Saison – auch die Reihenfolge bei vollem Gleichstand und die Tendenz – genau so
    /// zeigen wie vor dem Sichern. Grundlage ist der gespeicherte Saisonrekord (Abschlusstabellen, Auf- und Absteiger in
    /// Tabellenreihenfolge der Aufstiegsrunde).
    /// </summary>
    [Fact]
    public void LoadedGameInSecondSeason_KeepsStartOrderAndTables()
    {
        var game = SaisonwechselZwangsabstiegTests.Create(seed: 71, clubs: 5);
        var record = SaisonwechselZwangsabstiegTests.PlaySeason(game, _ => 1_000_000);
        game.StartNextSeason();
        Assert.Equal(GameState.FirstSeason + 1, game.Season);
        Assert.Equal(0, game.PlayedMatchDays);

        // Vor dem ersten Spieltag stehen alle gleich: Die Tabelle zeigt die Startordnung, und die folgt der Vorsaison.
        var differsFromIndexOrder = false;
        foreach (var league in Enum.GetValues<League>())
        {
            var members = game.LeagueClubs(league).Select(c => c.Index).ToList();
            var start = LeagueTable.Compute(game, league, TableView.Gesamt, 0).Select(r => r.Club.Index).ToList();
            Assert.Equal(LeagueTable.SuccessorOrder(record, league, members), start);
            differsFromIndexOrder |= !start.SequenceEqual(members.Order());
        }
        Assert.True(differsFromIndexOrder, "Testaufbau: Startordnung gleich der Vereinsreihenfolge");

        for (var i = 0; i < 3; i++)
            TestSeason.PlayMatchDay(game);
        var loaded = SaveGame.Load(SaveGame.Save(game)).Game!;

        Assert.Equal(game.Season, loaded.Season);
        Assert.Equal(game.PlayedMatchDays, loaded.PlayedMatchDays);
        foreach (var league in Enum.GetValues<League>())
            foreach (var view in Views)
                for (var day = 0; day <= game.PlayedMatchDays; day++)
                    Assert.Equal(Snapshot(game, league, view, day), Snapshot(loaded, league, view, day));
    }

    private static List<(int Club, int Rank, int Previous, int Points, string Balance)> Snapshot(GameState game, League league, TableView view, int day) =>
        LeagueTable.Compute(game, league, view, day).Select(r => (r.Club.Index, r.Rank, r.PreviousRank, r.PointsPlus, r.Balance)).ToList();

    /// <summary>
    /// #362 (PSW-01): Jedes der drei Felder „Ergebnisse“ (DGROUP 0x21E/0x221/0x224, em.exe Datei 0x3705F–0x3706D, ODER) schaltet
    /// Play-offs und Aufstiegsrunde ohne Managerverein allein auf die Minutenschleife; die Zeilen „Drittelpause“ und „Tabelle“
    /// derselben Liga nicht. Vergleich über den Zufallsverbrauch: aus = schneller Weg 0x3731F (wie ein Pokalspiel mit
    /// ausgeschaltetem Schalter).
    /// </summary>
    [Theory]
    [InlineData(MatchCompetition.Playoff, true, League.Bundesliga)]
    [InlineData(MatchCompetition.Playoff, true, League.ZweiteLiga)]
    [InlineData(MatchCompetition.Playoff, true, League.Oberliga)]
    [InlineData(MatchCompetition.Relegation, false, League.Bundesliga)]
    [InlineData(MatchCompetition.Relegation, false, League.ZweiteLiga)]
    [InlineData(MatchCompetition.Relegation, false, League.Oberliga)]
    public void PlayoffWithoutManager_EachResultsFieldAloneSwitchesToMinuteLoop(MatchCompetition competition, bool knockOut,
        League league)
    {
        var game = TestGames.Create(3);
        MatchEngine.EnsureTeamValues(game);
        var managed = game.Managers.Select(m => m.ClubIndex).ToHashSet();
        var clubs = game.Clubs.Where(c => c.Index < 12 && !managed.Contains(c.Index)).Take(2).ToList();
        Assert.All(Enum.GetValues<League>(), l => Assert.False(game.Options.ShowsReport(ReportKind.Results, l)));

        int NextAfterMatch(MatchCompetition played)
        {
            var random = new Random(7);
            MatchEngine.PlayWithEvents(game, clubs[0], clubs[1], played, knockOut, random);
            return random.Next();
        }
        int WithField(ReportKind kind)
        {
            game.Options.ToggleReport(kind, league);
            try
            {
                return NextAfterMatch(competition);
            }
            finally
            {
                game.Options.ToggleReport(kind, league);
            }
        }

        game.Options.ShowDebCup = false;
        var fast = NextAfterMatch(MatchCompetition.DebCup);
        Assert.Equal(fast, NextAfterMatch(competition));
        Assert.NotEqual(fast, WithField(ReportKind.Results));
        Assert.Equal(fast, WithField(ReportKind.Intermission));
        Assert.Equal(fast, WithField(ReportKind.Table));
    }

    /// <summary>
    /// Abnahme der Spur, Mehrsaisonprobe Modus 1 („nur Weltkugel“, 20 Saisonwechsel, passiver und aktiver Bundestrainer):
    /// Ohne Vereinssaison steigt niemand auf oder ab, die Oberliga tauscht nicht, kein Zwangsabstieg, kein Saisonende-Ereignis,
    /// das Vereinskonto bleibt unverändert. Modus 2 und 4: <see cref="SaisonwechselOberligaTests"/>,
    /// <see cref="SaisonwechselZwangsabstiegTests"/>.
    /// [APPROX] Ob em.exe im Modus 1 den Saisonwechsel 0x563AF überhaupt durchläuft, ist nicht gelesen (Spur wettbewerbe-weltkugel).
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WorldOnly_TwentySeasonChanges_NoExchangeNoForcedRelegation(bool active)
    {
        var game = GameModeTests.Create(7, false, true, 5);
        Assert.Equal(GameMode.WorldOnly, game.Mode);
        var manager = game.Managers[0];
        var money = manager.Money;
        var leagues = game.Clubs.Select(c => c.League).ToList();
        var outside = game.OutsidePlaces().ToList();
        for (var change = 0; change < 20; change++)
        {
            if (active)
            {
                game.Continue();
                game.AutoNominate();
                for (var opponent = NationalTeams.First; opponent <= NationalTeams.Last; opponent++)
                    if (opponent != NationalTeams.Germany)
                        game.PlayFriendly(opponent, home: opponent % 2 == 0);
            }
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 1000, "Saisonende nicht erreicht");
                game.Continue();
            }
            var record = game.History[^1];
            Assert.Empty(record.Managers);
            Assert.Empty(record.LeftOberliga);
            Assert.All(Enum.GetValues<League>(), l => Assert.Empty(record.PromotedInto[l]));
            Assert.All(Enum.GetValues<League>(), l => Assert.Empty(record.RelegatedInto[l]));
            game.StartNextSeason();
            Assert.Equal(leagues, game.Clubs.Select(c => c.League).ToList());
            Assert.Equal(outside, game.OutsidePlaces().ToList());
            Assert.Equal(money, manager.Money);
            Assert.False(manager.Dismissed);
        }
        Assert.Equal(20, game.History.Count);
    }
}
