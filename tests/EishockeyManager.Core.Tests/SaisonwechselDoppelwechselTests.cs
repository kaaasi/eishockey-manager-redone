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
        // Review #458, SW-05: Partner ist der Bundesligist, der mit Verein 12 tauscht (gleicher Paarindex in der
        // Gruppentabelle Bundesliga/2. Liga), nicht irgendein Absteiger. Seit der Integration Runde 11 ist Verein 12 hier das
        // erste Paar; den Paarindex prüft DoubleMove_PartnerHasSamePairIndex (zweites Paar).
        var places = game.RelegationGroups.Single(g => g.Upper == League.Bundesliga).Table(game.Clubs).Select(r => r.Club.Index).ToArray();
        var pair = places.Take(2).Where(c => game.Clubs[c].League != League.Bundesliga).ToList().IndexOf(Club);
        Assert.True(pair >= 0, "Testaufbau: Verein 12 nicht unter den ersten beiden der Aufstiegsrunde");
        Assert.Equal(places.Skip(2).Take(2).Where(c => game.Clubs[c].League == League.Bundesliga).ElementAt(pair), partner);
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

    // ---- Review #458, SW-06: Partner über die Platzabbildung des ersten Tauschs ----------------------------------------------
    // Synthetisch: Vereine 0–11 Bundesliga, 12–23 2. Liga. Listen wie nach dem Zwangsabstieg (RecordRelegationGroup).

    private static bool IsBundesliga(int club) => club < 12;

    private static SeasonRecord Record(IReadOnlyList<int> upperPlaces, params int[] intoOberliga)
    {
        var record = new SeasonRecord { Season = 0, FinalTables = new Dictionary<League, IReadOnlyList<int>>() };
        var (promoted, relegated) = GameState.RelegationExchange(upperPlaces, IsBundesliga);
        record.PromotedInto[League.Bundesliga].AddRange(promoted);
        record.RelegatedInto[League.ZweiteLiga].AddRange(relegated);
        record.RelegatedInto[League.Oberliga].AddRange(intoOberliga);
        return record;
    }

    [Fact]
    public void DoubleMove_PartnerHasSamePairIndex()
    {
        // Gruppentabelle [12, 13, 10, 5]: Paare 12↔10 und 13↔5. Zweitligist 13 (zwangsabgestiegen, Platz in der Gruppe
        // 2. Liga/Oberliga) steigt auf; in die Oberliga geht sein Partner 5, nicht der erste Absteiger 10.
        int[] places = [12, 13, 10, 5];
        var record = Record(places, 20, 13);
        GameState.ResolveDoubleMoves(record, places, IsBundesliga);
        Assert.Equal([12, 13], record.PromotedInto[League.Bundesliga]);
        Assert.Equal([10], record.RelegatedInto[League.ZweiteLiga]);
        Assert.Equal([20, 5], record.RelegatedInto[League.Oberliga]);
        AssertEachClubMovesOnce(record);
    }

    [Fact]
    public void DoubleMove_DoublePlace_SecondLeagueClubOnForcedPlaceDropsInstead()
    {
        // [A=12, B=13, X1=1, X1=1]: Bundesliga-Zwangsabsteiger 1 hat den zweiten Bundesligisten überschrieben. Platzlogik:
        // 1→Platz A, 12→Platz B, 13→Platz 1; also Promoted [13], Relegated [1]. Ist B (13) zugleich Zwangsabsteiger der 2. Liga,
        // geht sein Platz – mit A darauf – in die Oberliga; 1 geht nur in die 2. Liga (vorher: 1 in die Oberliga, A blieb).
        int[] places = [12, 13, 1, 1];
        var record = Record(places, 20, 13);
        Assert.Equal([13], record.PromotedInto[League.Bundesliga]);
        Assert.Equal([1], record.RelegatedInto[League.ZweiteLiga]);
        GameState.ResolveDoubleMoves(record, places, IsBundesliga);
        Assert.Equal([13], record.PromotedInto[League.Bundesliga]);
        Assert.Equal([1], record.RelegatedInto[League.ZweiteLiga]);
        Assert.Equal([20, 12], record.RelegatedInto[League.Oberliga]);
        AssertEachClubMovesOnce(record);
    }

    [Fact]
    public void DoubleMove_DoublePlace_ForcedClubNotPromoted_StaysInSecondLeague()
    {
        // Wie oben, aber A (12) ist der Zwangsabsteiger der 2. Liga: Er steigt nicht auf, sein Platz trägt nach dem ersten
        // Tausch den Bundesligisten 1, der damit in die Oberliga geht; A bleibt (auf Platz B) in der 2. Liga
        // (vorher: A in die Oberliga, 1 in die 2. Liga).
        int[] places = [12, 13, 1, 1];
        var record = Record(places, 20, 12);
        GameState.ResolveDoubleMoves(record, places, IsBundesliga);
        Assert.Equal([13], record.PromotedInto[League.Bundesliga]);
        Assert.Empty(record.RelegatedInto[League.ZweiteLiga]);
        Assert.Equal([20, 1], record.RelegatedInto[League.Oberliga]);
        AssertEachClubMovesOnce(record);
    }

    [Fact]
    public void NoForcedClubInUpperGroup_ListsUnchanged()
    {
        int[] places = [12, 13, 10, 5];
        var record = Record(places, 20, 21);
        GameState.ResolveDoubleMoves(record, places, IsBundesliga);
        Assert.Equal([12, 13], record.PromotedInto[League.Bundesliga]);
        Assert.Equal([10, 5], record.RelegatedInto[League.ZweiteLiga]);
        Assert.Equal([20, 21], record.RelegatedInto[League.Oberliga]);
    }
}
