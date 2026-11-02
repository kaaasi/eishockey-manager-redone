using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur saisonwechsel, Teil 5 (Review #441, SW-01): Ein Zweitligist mit Zwangsabstieg, der zugleich über die Aufstiegsrunde
/// in die Bundesliga aufsteigt. Vorher stand er in <see cref="SeasonRecord.PromotedInto"/> der Bundesliga und in
/// <see cref="SeasonRecord.RelegatedInto"/> der Oberliga; die Ligen hatten 12/13/11 Vereine, die Folgesaison brach ab.
/// </summary>
public class SaisonwechselDoppelwechselTests
{
    private const int Club = 12; // synthetische Datenbank: Vereine 12–23 in der 2. Liga

    /// <summary>
    /// Eine Saison bis „Saisonende“: Konto bis zum Ende der Vereinssaison 10 Mio. DM, danach je Manager
    /// <paramref name="money"/>. Ligaspiele des Vereins <paramref name="winner"/> (falls gesetzt) werden nach jedem Spieltag
    /// als 9:0-Siege umgeschrieben (mit <paramref name="winRound"/> auch die Spiele der Aufstiegsrunde), die von
    /// <paramref name="loser"/> als 0:9-Niederlagen. Nur Testaufbau.
    /// </summary>
    private static SeasonRecord PlaySeason(GameState game, Func<int, long> money, int? winner, int? loser = null,
        bool winRound = false)
    {
        for (var guard = 0; !game.RegularSeasonOver; guard++)
        {
            Assert.True(guard < 2000, "Hauptrunde nicht beendet");
            foreach (var m in game.Managers)
            {
                m.Money = 10_000_000;
                TestSeason.KeepSquad(game, m);
            }
            TestSeason.PlayMatchDay(game);
            Rewrite(game.Results, winner, loser);
        }
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 1000, "Saisonende nicht erreicht");
            for (var i = 0; i < game.Managers.Count; i++)
                game.Managers[i].Money = game.Phase == SeasonPhase.ClubSeasonOver ? money(i) : 10_000_000;
            game.Continue();
            if (winRound && winner is { } w)
                foreach (var group in game.RelegationGroups)
                    Rewrite(group.Results, w, null);
        }
        return game.History[^1];
    }

    private static void Rewrite(List<MatchResult> results, int? winner, int? loser)
    {
        for (var i = 0; i < results.Count; i++)
        {
            var r = results[i];
            if (winner is { } w && r.Involves(w))
                results[i] = r.Home == w ? r with { HomeGoals = 9, AwayGoals = 0 } : r with { HomeGoals = 0, AwayGoals = 9 };
            else if (loser is { } l && r.Involves(l))
                results[i] = r.Home == l ? r with { HomeGoals = 0, AwayGoals = 9 } : r with { HomeGoals = 9, AwayGoals = 0 };
        }
    }

    private static void AssertLeagueSizes(GameState game) =>
        Assert.All(Enum.GetValues<League>(), l => Assert.Equal(GameState.ClubsPerLeague, game.LeagueClubs(l).Count()));

    /// <summary>Jeder Verein höchstens in einer Liste des Auf- und Abstiegs.</summary>
    private static void AssertEachClubMovesOnce(SeasonRecord record)
    {
        var moves = Enum.GetValues<League>().SelectMany(l => record.PromotedInto[l].Concat(record.RelegatedInto[l])).ToList();
        Assert.Equal(moves.Count, moves.Distinct().Count());
    }

    [Fact]
    public void ForcedFromSecondLeague_PromotedInRelegationRound_StaysUp_PartnerDropsTwoLeagues()
    {
        // Gegenbeispiel aus dem Review (SW-01): seed 1, Manager auf Verein 12, alle 2.-Liga-Spiele gewonnen, Konto ab
        // 13. April −700.000 DM. Verein 12 gewinnt die Aufstiegsrunde und wird zugleich zwangsweise abgestiegen.
        var game = SaisonwechselZwangsabstiegTests.Create(seed: 1, clubs: Club);
        // Integration Runde 11 (geld + saisonwechsel + kader-transfer): Mit dem integrierten Zufallsverlauf gewinnt Verein 12 die
        // Aufstiegsrunde nicht mehr von selbst (Verein 14); die Rundenspiele werden deshalb wie in der Mehrsaisonprobe festgelegt.
        var record = PlaySeason(game, _ => -700_000, winner: Club, winRound: true);

        Assert.Equal(League.ZweiteLiga, game.Clubs[Club].League);
        Assert.Contains(SeasonEvent.ForcedRelegation, record.Managers[0].Events);
        Assert.Contains(Club, record.PromotedInto[League.Bundesliga]); // Testaufbau: Aufstieg über die Aufstiegsrunde

        // [APPROX] Regel (Review SW-01, Datei 0x55F93 ff.): Der Zwangsabsteiger bleibt oben; der Bundesligist, der durch
        // den ersten Tausch auf seinen Platz kam, wird mit der Gruppe 2. Liga/Oberliga in die Oberliga getauscht.
        Assert.DoesNotContain(Club, record.RelegatedInto[League.Oberliga]);
        AssertEachClubMovesOnce(record);
        var partner = Assert.Single(record.RelegatedInto[League.Oberliga], c => game.Clubs[c].League == League.Bundesliga);
        Assert.DoesNotContain(partner, record.RelegatedInto[League.ZweiteLiga]);
        Assert.Equal(record.PromotedInto[League.Bundesliga].Count - 1, record.RelegatedInto[League.ZweiteLiga].Count);
        Assert.Equal(record.PromotedInto[League.ZweiteLiga].Count, record.RelegatedInto[League.Oberliga].Count);

        game.StartNextSeason();
        Assert.Equal(League.Bundesliga, game.Clubs[Club].League);
        Assert.Equal(League.Oberliga, game.Clubs[partner].League);
        AssertLeagueSizes(game);

        // Folgesaison läuft durch (vorher ArgumentOutOfRangeException) und der Spielstand übersteht den Rundlauf.
        SaveGameTests.AssertRoundTrip(game);
        PlaySeason(game, _ => 1_000_000, winner: null);
        game.StartNextSeason();
        AssertLeagueSizes(game);
    }

    [Theory]
    [InlineData(true)]  // Modus 4
    [InlineData(false)] // Modus 2
    public void MultiSeason_DoubleMoveRecurs_LeaguesStay12(bool europe)
    {
        // Mehrsaisonprobe (AGENTS.md §5), 8 Saisonwechsel: Aktiver Manager (Verein 12) mit −700.000 DM gewinnt in der 2. Liga
        // und in der Oberliga alle Liga- und Aufstiegsrundenspiele (Doppelfall in jeder Zweitligasaison) und verliert in der Bundesliga alle (Zwangsabstieg);
        // passiver Manager (Verein 20) mit Guthaben. Je Wechsel 12/12/12, jeder Verein höchstens einmal bewegt, Rundlauf.
        var game = SaisonwechselZwangsabstiegTests.Create(seed: 1, europe: europe, clubs: [Club, 20]);
        var doubleMoves = 0;
        for (var season = 0; season < 8; season++)
        {
            var league = game.Clubs[Club].League!.Value;
            var record = PlaySeason(game, i => i == 0 ? -700_000 : 2_000_000,
                winner: league == League.Bundesliga ? null : Club, loser: league == League.Bundesliga ? Club : null, winRound: true);
            AssertEachClubMovesOnce(record);
            var forced = record.Managers[0].Events.Contains(SeasonEvent.ForcedRelegation);
            var promoted = record.PromotedInto[League.Bundesliga].Contains(Club);
            if (forced && promoted)
                doubleMoves++;
            Assert.All(record.Managers.Skip(1), r => Assert.DoesNotContain(SeasonEvent.ForcedRelegation, r.Events));
            game.StartNextSeason();
            AssertLeagueSizes(game);
            if (league == League.ZweiteLiga && forced)
                Assert.Equal(promoted ? League.Bundesliga : League.Oberliga, game.Clubs[Club].League);
            SaveGameTests.AssertRoundTrip(game);
        }
        Assert.True(doubleMoves >= 3, $"Doppelfall nur {doubleMoves}-mal");
    }
}
