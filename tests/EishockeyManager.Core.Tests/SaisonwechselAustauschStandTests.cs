using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Integrationsfehler Runde 12 (PR fix/r12-doppelwechsel): Zweitligist mit Zwangsabstieg steigt über die Aufstiegsrunde auf,
/// die Gruppentabelle Bundesliga/2. Liga am 29. April weicht aber von der am 13. April ab. Vorher nahm der Zwangsabstieg die
/// Listen der Gruppe Bundesliga/2. Liga vom 13. April, den Doppelwechsel (<see cref="GameState.ResolveDoubleMoves"/>) und die
/// Gruppe 2. Liga/Oberliga aber vom 29. April: Ein Bundesligist ging in die Oberliga, ohne als Absteiger der 2. Liga zu
/// zählen, und die Ligen hatten 11/13/12 Vereine (Mehrsaisonprobe, 5. Saisonwechsel nach kader-transfer).
/// </summary>
/// <remarks>
/// [ORIGINAL] em.exe leert am 29. April alle Auf-/Absteigerlisten (Datei 0x560EB–0x5612E) und füllt sie im Austausch aller
/// Gruppen aus den Gruppentabellen dieses Tages (Datei 0x56137–0x563A4, Tausch Datei 0x562F2/0x5636D); re-saisonwechsel.md §1
/// Schritte 5–7. Synthetisch: Die Ergebnisse der Aufstiegsrunde werden nach dem 13. April umgeschrieben – nur Testaufbau, um
/// zwei verschiedene Tabellenstände zu erzeugen.
/// </remarks>
public class SaisonwechselAustauschStandTests
{
    private const int Club = 12; // synthetische Datenbank: Vereine 12–23 in der 2. Liga

    [Fact]
    public void ForcedPromotedClub_TableChangedAfterClubSeason_ListsFollowTableOf29April()
    {
        var game = SaisonwechselZwangsabstiegTests.Create(seed: 1, clubs: Club);
        bool IsBundesliga(int c) => game.Clubs[c].League == League.Bundesliga;
        // Stand 13. April: [12, A, Y, B] – Verein 12 steigt auf, Paar 12 ↔ B (A Bundesliga, Y 2. Liga). Die Spiele bis zum
        // vorletzten Spieltag der Aufstiegsrunde werden so festgelegt, der letzte bleibt Zufall (Vorbedingung unten).
        int[] Order(RelegationGroup g)
        {
            var bundesliga = g.Clubs.Where(IsBundesliga).Order().ToList();
            return [Club, bundesliga[0], g.Clubs.Single(c => !IsBundesliga(c) && c != Club), bundesliga[1]];
        }
        PlayToClubSeasonOver(game, Order);

        var group = game.RelegationGroups.Single(g => g.Upper == League.Bundesliga);
        int[] before = Order(group);
        var (a, y, b) = (before[1], before[2], before[3]);
        var record13 = game.CurrentRecord!;
        Assert.Equal([Club], record13.PromotedInto[League.Bundesliga]); // Vorbedingung Testaufbau
        Assert.Equal([b], record13.RelegatedInto[League.ZweiteLiga]);

        // Stand 29. April: [12, Y, A, B] – zwei Aufsteiger, Paar 12 ↔ A. Vorher ging A (Platz von 12) in die Oberliga, B blieb
        // Absteiger in die 2. Liga, nur 12 stieg auf: Bundesliga 11, 2. Liga 13.
        int[] table = [Club, y, a, b];
        Arrange(group, table);
        Assert.Equal(table, group.Table(game.Clubs).Select(r => r.Club.Index));

        for (var guard = 0; game.Phase == SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 100, "29. April nicht erreicht");
            game.Managers[0].Money = -700_000;
            game.Continue();
        }
        var record = game.History[^1];
        Assert.Contains(SeasonEvent.ForcedRelegation, record.Managers[0].Events);

        var (promoted, relegated) = GameState.RelegationExchange(table, IsBundesliga);
        var partner = GameState.RelegationPlaces(table, IsBundesliga)[Club];
        Assert.Equal(promoted, record.PromotedInto[League.Bundesliga]);
        Assert.Equal(relegated.Where(c => c != partner), record.RelegatedInto[League.ZweiteLiga]);
        Assert.Contains(partner, record.RelegatedInto[League.Oberliga]);
        Assert.DoesNotContain(Club, record.RelegatedInto[League.Oberliga]);

        game.StartNextSeason();
        Assert.Equal(League.Bundesliga, game.Clubs[Club].League);
        Assert.Equal(League.Oberliga, game.Clubs[partner].League);
        Assert.All(Enum.GetValues<League>(), l => Assert.Equal(GameState.ClubsPerLeague, game.LeagueClubs(l).Count()));
        SaveGameTests.AssertRoundTrip(game);
    }

    /// <summary>Hauptrunde mit Siegen des Vereins 12, Aufstiegsrunde Bundesliga/2. Liga nach <paramref name="order"/>, bis „Saisonende“ (13. April). Nur Testaufbau.</summary>
    private static void PlayToClubSeasonOver(GameState game, Func<RelegationGroup, int[]> order)
    {
        for (var guard = 0; game.Phase != SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 2000, "Saisonende nicht erreicht");
            game.Managers[0].Money = 10_000_000;
            TestSeason.KeepSquad(game, game.Managers[0]);
            if (game.RegularSeasonOver)
                game.Continue();
            else
                TestSeason.PlayMatchDay(game);
            WinAll(game.Results);
            foreach (var group in game.RelegationGroups.Where(g => g.Upper == League.Bundesliga))
                Arrange(group, order(group));
        }
    }

    private static void WinAll(List<MatchResult> results)
    {
        for (var i = 0; i < results.Count; i++)
            if (results[i] is var r && r.Involves(Club))
                results[i] = r.Home == Club ? r with { HomeGoals = 9, AwayGoals = 0 } : r with { HomeGoals = 0, AwayGoals = 9 };
    }

    /// <summary>Ergebnisse der Gruppe so umschreiben, dass die Tabelle <paramref name="order"/> lautet (Besserer gewinnt 9:0).</summary>
    private static void Arrange(RelegationGroup group, int[] order)
    {
        for (var i = 0; i < group.Results.Count; i++)
        {
            var r = group.Results[i];
            var homeWins = Array.IndexOf(order, r.Home) < Array.IndexOf(order, r.Away);
            group.Results[i] = r with { HomeGoals = homeWins ? 9 : 0, AwayGoals = homeWins ? 0 : 9 };
        }
    }
}
