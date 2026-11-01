using EishockeyManager.Core;
using Xunit;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur wettbewerbe-komplett Teil 1: Startordnung der Ligatabelle ab der zweiten Saison (em.exe behält die Platzliste
/// DGROUP 0x5CCB, Auf-/Abstieg tauscht Vereinsindizes über 0x36169) und Tore modulo 30 in den Gruppentabellen (0x30906).
/// </summary>
public class WettbewerbeStartordnungTests
{
    private static SeasonRecord Record()
    {
        // Abschlusstabellen (Platz 1 zuerst) einer gedachten Vorsaison mit vertauschten Vereinsindizes.
        var record = new SeasonRecord
        {
            Season = GameState.FirstSeason,
            FinalTables = new Dictionary<League, IReadOnlyList<int>>
            {
                [League.Bundesliga] = [5, 0, 1, 2, 3, 4, 6, 7, 8, 9, 11, 10],
                [League.ZweiteLiga] = [13, 12, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23],
                [League.Oberliga] = [24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 35, 34],
            },
        };
        // Aufstiegsrunde 1./2. Liga in Tabellenreihenfolge: 12, 13 oben; 11, 10 unten.
        record.PromotedInto[League.Bundesliga].AddRange([12, 13]);
        record.RelegatedInto[League.ZweiteLiga].AddRange([11, 10]);
        // Aufstiegsrunde 2. Liga/Oberliga: nur ein Wechsel (23 bleibt oben).
        record.PromotedInto[League.ZweiteLiga].Add(24);
        record.RelegatedInto[League.Oberliga].Add(22);
        // Oberliga: Letzter (34), dann Vorletzter (35) gehen, 40 und 37 kommen.
        record.LeftOberliga.AddRange([34, 35]);
        record.PromotedInto[League.Oberliga].AddRange([40, 37]);
        return record;
    }

    [Fact]
    public void SuccessorOrder_PartnerTakesTheListPlace()
    {
        // [ORIGINAL] 0x5CCB bleibt stehen; 0x36169 tauscht Absteiger und Aufsteiger paarweise (Datei 0x56137–0x5636D).
        var record = Record();
        Assert.Equal([5, 0, 1, 2, 3, 4, 6, 7, 8, 9, 12, 13],
            LeagueTable.SuccessorOrder(record, League.Bundesliga, [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 12, 13]));
        Assert.Equal([10, 11, 14, 15, 16, 17, 18, 19, 20, 21, 24, 23],
            LeagueTable.SuccessorOrder(record, League.ZweiteLiga, [10, 11, 14, 15, 16, 17, 18, 19, 20, 21, 23, 24]));
        Assert.Equal([22, 25, 26, 27, 28, 29, 30, 31, 32, 33, 37, 40],
            LeagueTable.SuccessorOrder(record, League.Oberliga, [22, 25, 26, 27, 28, 29, 30, 31, 32, 33, 37, 40]));
    }

    [Fact]
    public void SuccessorOrder_UnknownMembersFollowInClubOrder()
    {
        // [APPROX] Vereine ohne Platz in der Vorsaison (z. B. nach einem Moduswechsel) folgen in Vereinsreihenfolge.
        var record = Record();
        Assert.Equal([5, 0, 1, 44, 46],
            LeagueTable.SuccessorOrder(record, League.Bundesliga, [0, 1, 5, 44, 46]));
    }

    [Fact]
    public void SecondSeason_StartsWithTheFinalOrderOfTheFirst()
    {
        var game = TestGames.Create();
        SeasonRecord? record = null;
        RngTwinProbeTests.Run(game, active: false, changes: 1, beforeChange: g => record = g.History[^1]);
        Assert.NotNull(record);
        Assert.Equal(GameState.FirstSeason + 1, game.Season);

        foreach (var league in Enum.GetValues<League>())
        {
            var start = LeagueTable.Compute(game, league, TableView.Gesamt, 0).Select(r => r.Club.Index).ToList();
            Assert.Equal(Expected(record!, league), start);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultiSeason_FourChanges_StartOrderAndGroupGoals(bool active)
    {
        // Mehrsaisonprobe: vor jedem Saisonwechsel Paare in Tabellenreihenfolge und Gruppentore unter 30 (0x30906/0x30916
        // ziehen 30 ab, die Marke kommt im Remake nicht vor); nach jedem Wechsel Startordnung = Platzliste der Vorsaison.
        var game = TestGames.Create(3);
        var checkedSeasons = 0;
        SeasonRecord? last = null;
        RngTwinProbeTests.Run(game, active, beforeChange: g =>
        {
            var record = g.History[^1];
            foreach (var group in g.RelegationGroups)
            {
                var order = group.Table(g.Clubs).Select(r => r.Club.Index).ToList();
                var promoted = record.PromotedInto[group.Upper];
                var relegated = record.RelegatedInto[group.Lower];
                Assert.Equal(promoted.OrderBy(order.IndexOf), promoted);
                Assert.Equal(relegated.OrderBy(order.IndexOf), relegated);
                Assert.All(group.Results, r => Assert.True(r.HomeGoals < 30 && r.AwayGoals < 30));
            }
            var groups = (g.EuropeanCup?.Groups ?? []).Concat(g.EuropeanCup?.FinalRound is { } f ? [f] : [])
                .Concat(g.WorldCup?.Groups ?? []);
            foreach (var group in groups)
                Assert.All(group.Results, r => Assert.True(r.HomeGoals < 30 && r.AwayGoals < 30));
            last = record;
        }, afterStep: g =>
        {
            if (last is null || g.Season != last.Season + 1 || g.PlayedMatchDays != 0 || g.Phase != SeasonPhase.Running)
                return;
            foreach (var league in Enum.GetValues<League>())
                Assert.Equal(Expected(last, league),
                    LeagueTable.Compute(g, league, TableView.Gesamt, 0).Select(r => r.Club.Index).ToList());
            checkedSeasons++;
            last = null;
        });
        Assert.Equal(4, checkedSeasons);
    }

    /// <summary>Erwartung unabhängig von <see cref="LeagueTable.SuccessorOrder"/>: Abschlusstabelle, Partner einsetzen.</summary>
    private static List<int> Expected(SeasonRecord record, League league)
    {
        var swap = new Dictionary<int, int>();
        void Add(List<int> a, List<int> b)
        {
            Assert.Equal(a.Count, b.Count);
            for (var i = 0; i < a.Count; i++)
                swap[a[i]] = b[i];
        }
        switch (league)
        {
            case League.Bundesliga:
                Add(record.RelegatedInto[League.ZweiteLiga], record.PromotedInto[League.Bundesliga]);
                break;
            case League.ZweiteLiga:
                Add(record.PromotedInto[League.Bundesliga], record.RelegatedInto[League.ZweiteLiga]);
                Add(record.RelegatedInto[League.Oberliga], record.PromotedInto[League.ZweiteLiga]);
                break;
            default:
                Add(record.PromotedInto[League.ZweiteLiga], record.RelegatedInto[League.Oberliga]);
                Add(record.LeftOberliga, record.PromotedInto[League.Oberliga]);
                break;
        }
        return record.FinalTables[league].Select(c => swap.GetValueOrDefault(c, c)).ToList();
    }
}
