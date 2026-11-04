using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur saisonwechsel, Teil 1 (S-K1, S-K2, A162, A197, #166): Oberliga-Austausch wie em.exe Datei 0x56238–0x5636D,
/// belegt durch den DOSBox-Lauf FILE12–FILE14 (docs/analyse/re-saisonwechsel.md, „Auswertung DOSBox-Beleg“).
/// </summary>
public class SaisonwechselOberligaTests
{
    private static GameState Create(int seed, GameDuration duration = GameDuration.Unbegrenzt, bool europe = true, params int[] clubs)
    {
        var setup = new GameSetup { Duration = duration, EuropeAndWorldCup = europe };
        for (var i = 0; i < clubs.Length; i++)
            setup.Managers.Add(new ManagerSetup { Name = $"M{i}", Head = i, ClubIndex = clubs[i], StartLeague = NewGame.DatabaseLeague(clubs[i]) ?? League.Oberliga });
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    /// <summary>
    /// Schreibt die Oberliga-Ergebnisse so um, dass <paramref name="last"/> Letzter und <paramref name="secondLast"/> Vorletzter
    /// wird (Ergebnisse der übrigen Spiele bleiben). Nur Testaufbau: der Ausgang der Hauptrunde ist nicht Gegenstand.
    /// </summary>
    private static void ForceBottom(GameState game, int last, int secondLast)
    {
        int Goals(int club, int other, int original) =>
            club == last ? 0 : club == secondLast ? (other == last ? 1 : 0) : other == last || other == secondLast ? 10 : original;
        for (var i = 0; i < game.Results.Count; i++)
            if (game.Results[i] is { League: League.Oberliga } r && (r.Involves(last) || r.Involves(secondLast)))
                game.Results[i] = r with { HomeGoals = Goals(r.Home, r.Away, r.HomeGoals), AwayGoals = Goals(r.Away, r.Home, r.AwayGoals) };
    }

    /// <summary>Hauptrunde spielen, Tabellenende setzen, bis „Saisonende“ (29. April). Konto vorher auf <paramref name="money"/>.</summary>
    private static SeasonRecord PlaySeason(GameState game, Func<GameState, (int Last, int SecondLast)?> bottom, long money = 10_000_000)
    {
        for (var guard = 0; !game.RegularSeasonOver; guard++)
        {
            Assert.True(guard < 2000, "Hauptrunde nicht beendet");
            foreach (var m in game.Managers)
            {
                m.Money = 10_000_000; // keine Entlassung, kein Kaderverlust durch Geld (Test prüft den Austausch)
                TestSeason.KeepSquad(game, m);
            }
            TestSeason.PlayMatchDay(game);
            if (bottom(game) is { } b) // nach jedem Spieltag, damit auch die Aufstiegsrunden die gesetzte Tabelle sehen
                ForceBottom(game, b.Last, b.SecondLast);
        }
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 1000, "Saisonende nicht erreicht");
            foreach (var m in game.Managers)
                m.Money = money;
            game.Continue();
        }
        return game.History[^1];
    }

    private static int[] Oberliga(GameState game) => game.LeagueClubs(League.Oberliga).Select(c => c.Index).Order().ToArray();

    private static int OtherOberligist(GameState game, params int[] except) =>
        game.LeagueClubs(League.Oberliga).Select(c => c.Index).First(c => !except.Contains(c));

    [Fact]
    public void ManagerClubLast_StaysInOberliga_OnlyOneNewcomer_Money400k_BothArrows()
    {
        // [ORIGINAL] DOSBox-Beleg #389: Krefelder EV (Letzter, −491.221 DM) bleibt, nur TEV Miesbach rückt nach, ESC Wedemark
        // (11.) steigt ab, Konto 400.000 DM, Pfeil hoch und Pfeil runter, keine Aufstiegsprämie.
        var game = Create(seed: 5, clubs: 30);
        var second = OtherOberligist(game, 30);
        var before = Oberliga(game);

        var record = PlaySeason(game, _ => (30, second), money: -491_221);

        Assert.Equal([30, second], record.LeftOberliga);
        Assert.Equal(30, record.PromotedInto[League.Oberliga][0]);
        var newcomer = record.PromotedInto[League.Oberliga][1];
        Assert.NotEqual(30, newcomer);
        Assert.Null(game.Clubs[newcomer].League);

        var result = Assert.Single(record.Managers);
        Assert.Equal(12, result.Rank);
        Assert.Contains(SeasonEvent.Promotion, result.Events);
        Assert.Contains(SeasonEvent.Relegation, result.Events);
        // Nur Einzelprämien, keine 300.000 DM für den Aufstieg (Review #441, SW-03: genau ein Vielfaches je Einzelprämie –
        // die Aufstiegsprämie ist selbst ein Vielfaches von 100.000 DM).
        var singles = result.Events.Count(e => e is SeasonEvent.BestGoalie or SeasonEvent.TopScorer or SeasonEvent.FairPlay);
        Assert.Equal(GameState.SinglePremium * singles, result.Premium);
        Assert.Equal(GameState.OberligaKeptMoney + result.Premium, game.Managers[0].Money);

        game.StartNextSeason();
        Assert.Equal(League.Oberliga, game.Clubs[30].League);
        Assert.Null(game.Clubs[second].League);
        var after = Oberliga(game);
        var fromOutside = after.Except(before).Where(c => !record.RelegatedInto[League.Oberliga].Contains(c));
        Assert.Equal([newcomer], fromOutside);
        Assert.DoesNotContain(second, after);
    }

    [Fact]
    public void ManagerClubSecondLast_Stays_LastLeaves()
    {
        var game = Create(seed: 7, clubs: 30);
        var last = OtherOberligist(game, 30);

        var record = PlaySeason(game, _ => (last, 30), money: 50_000);

        Assert.Equal([last, 30], record.LeftOberliga);
        Assert.Equal(30, record.PromotedInto[League.Oberliga][1]);
        Assert.True(GameState.KeptInOberliga(record, 30));
        Assert.False(GameState.KeptInOberliga(record, last));
        Assert.Equal(GameState.OberligaKeptMoney + record.Managers[0].Premium, game.Managers[0].Money);
        game.StartNextSeason();
        Assert.Equal(League.Oberliga, game.Clubs[30].League);
        Assert.Null(game.Clubs[last].League);
    }

    [Fact]
    public void TwoManagerClubsLastAndSecondLast_NobodyChanges()
    {
        var game = Create(seed: 9, clubs: [30, 31]);
        var before = Oberliga(game);
        var outsideStart = game.OutsidePlaces().Order().ToArray();

        var record = PlaySeason(game, _ => (31, 30), money: 1_000_000);

        Assert.Equal([31, 30], record.LeftOberliga);
        Assert.Equal([31, 30], record.PromotedInto[League.Oberliga]);
        Assert.All(record.Managers, r => Assert.Contains(SeasonEvent.Relegation, r.Events));
        Assert.All(game.Managers, (m, i) => Assert.Equal(GameState.OberligaKeptMoney + record.Managers[i].Premium, m.Money));
        game.StartNextSeason();
        var outsideBefore = outsideStart;
        Assert.Contains(30, Oberliga(game));
        Assert.Contains(31, Oberliga(game));
        Assert.Equal(outsideBefore, game.Clubs.Where(c => c.Index < NewGame.SelectableClubs && c.League is null).Select(c => c.Index));
        Assert.DoesNotContain(before, c => game.Clubs[c].League is null);
    }

    [Fact]
    public void ManagerClubTenth_NoException_TwoLeave_MoneyUnchanged()
    {
        // Gegenprobe: Außerhalb der letzten zwei wirkt nichts davon; vorher stieg der nächstschlechtere Verein ab.
        var game = Create(seed: 11, clubs: 30);
        var last = OtherOberligist(game, 30);
        var second = OtherOberligist(game, 30, last);

        var record = PlaySeason(game, _ => (last, second), money: 123_456);

        Assert.Equal([last, second], record.LeftOberliga);
        Assert.DoesNotContain(30, record.PromotedInto[League.Oberliga]);
        Assert.Equal(2, record.PromotedInto[League.Oberliga].Distinct().Count());
        Assert.DoesNotContain(SeasonEvent.Relegation, record.Managers[0].Events);
        Assert.Equal(123_456 + record.Managers[0].Premium, game.Managers[0].Money);
    }

    [Fact]
    public void ManagerClubLast_FormerlyNextWorseNonManagerWasRelegated_NowOnlyTheOtherLeaves()
    {
        // Roter Test für den alten Stand (A162): Where(…).Take(2) ließ den Drittletzten absteigen.
        var game = Create(seed: 13, clubs: 30);
        var second = OtherOberligist(game, 30);

        var record = PlaySeason(game, _ => (30, second));

        Assert.Equal(2, record.LeftOberliga.Count);
        Assert.Equal(record.FinalTables[League.Oberliga][^1], record.LeftOberliga[0]);
        Assert.Equal(record.FinalTables[League.Oberliga][^2], record.LeftOberliga[1]);
    }

    [Fact]
    public void Newcomers_OnlyFromPlaces36To41_AbsteigerTakesNewcomersPlace_Over20Seasons()
    {
        // [ORIGINAL] random(36, 41), Folgezug ≠ Vorzug; Absteiger übernimmt den Platz des Nachrückers (Wedemark 35 → 40).
        var game = Create(seed: 21, clubs: 2);
        var neverPromoted = Enumerable.Range(42, 6).ToArray();
        Assert.Equal(Enumerable.Range(36, 12), game.OutsidePlaces());

        for (var season = 0; season < 20; season++)
        {
            var placesBefore = game.OutsidePlaces().ToArray();
            var record = PlaySeason(game, _ => null);
            var arriving = record.PromotedInto[League.Oberliga];
            Assert.Equal(2, arriving.Distinct().Count());
            Assert.All(arriving.Where(c => !GameState.KeptInOberliga(record, c)), c => Assert.Contains(c, placesBefore.Take(GameState.OutsideDrawPlaces)));

            var placesAfter = game.OutsidePlaces();
            for (var k = 0; k < 2; k++)
                if (!GameState.KeptInOberliga(record, arriving[k])) // ein Managerverein (passiv abgestiegen) kann als Letzter bleiben
                    Assert.Equal(record.LeftOberliga[k], placesAfter[Array.IndexOf(placesBefore, arriving[k])]);
            Assert.Equal(neverPromoted, placesAfter.Skip(GameState.OutsideDrawPlaces));
            Assert.Equal(42, game.EternalTable.Count);
            Assert.DoesNotContain(game.EternalTable, e => neverPromoted.Contains(e.Club));

            game.StartNextSeason();
            Assert.All(neverPromoted, c => Assert.Null(game.Clubs[c].League));
            Assert.Equal(GameState.ClubsPerLeague, game.LeagueClubs(League.Oberliga).Count());
        }
        // Absteiger werden wieder ziehbar: Über 20 Saisons rückt mindestens ein früherer Oberliga-Absteiger wieder nach.
        var left = game.History.SelectMany(r => r.LeftOberliga).ToHashSet();
        Assert.Contains(game.History.Skip(1).SelectMany(r => r.PromotedInto[League.Oberliga]), left.Contains);
    }

    [Fact]
    public void StartSwap_DisplacedClubTakesChosenPlace_NeverPromoted()
    {
        // Manager wählt Verein 44 (ohne Liga): der verdrängte Oberligist liegt danach auf Platz 44 und rückt nie nach.
        var game = Create(seed: 3, clubs: 44);
        var displaced = Enumerable.Range(24, 12).Single(c => game.Clubs[c].League is null);

        var places = game.OutsidePlaces();
        Assert.Equal(displaced, places[44 - GameState.FirstOutsidePlace]);
        Assert.DoesNotContain(displaced, places.Take(GameState.OutsideDrawPlaces));
        Assert.DoesNotContain(game.EternalTable, e => e.Club == displaced);
        Assert.Contains(game.EternalTable, e => e.Club == 44);
    }

    [Theory]
    [InlineData(true)]  // Modus 4 (Fahne + Weltkugel)
    [InlineData(false)] // Modus 2 (nur Fahne)
    public void MultiSeason_PassiveAndActive_ManagerKeptEachTime(bool europe)
    {
        // Mehrsaisonprobe (AGENTS.md §5): aktiver Manager in der Oberliga, jede Saison Letzter → bleibt; passiver Manager
        // in der Bundesliga daneben unberührt. 20 Saisonwechsel.
        var game = Create(seed: 31, europe: europe, clubs: [30, 2]);
        for (var season = 0; season < 20; season++)
        {
            var record = PlaySeason(game, g => (30, OtherOberligist(g, 30)), money: -100_000);
            Assert.True(GameState.KeptInOberliga(record, 30));
            Assert.Equal(GameState.OberligaKeptMoney + record.Managers[0].Premium, game.Managers[0].Money);
            game.StartNextSeason();
            Assert.Equal(League.Oberliga, game.Clubs[30].League);
            Assert.Equal(GameState.ClubsPerLeague, game.LeagueClubs(League.Oberliga).Count());
            Assert.Equal(game.Clubs.Count(c => c.League is null && c.Index < NewGame.SelectableClubs), game.OutsidePlaces().Count);
            Assert.All(game.OutsidePlaces(), c => Assert.Null(game.Clubs[c].League));
        }
    }
}
