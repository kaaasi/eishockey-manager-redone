using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur saison, Teil 4 (A058, A163; docs/analyse/re-runde-13.md, DOSBox-Spielstände FILE20 → FILE21): Saisonwechsel im Modus
/// „nur Weltkugel“. Die große Wanderung der Teamwerte (Datei 0x56448 → 0x9D1B Modus 10) und der Oberliga-Austausch
/// (Datei 0x56238–0x56284) laufen wie in den anderen Modi; Auf- und Abstieg zwischen den Ligen gibt es ohne Spiele nicht.
/// </summary>
public class SaisonWeltkugelTests
{
    private static GameState CreateWorldOnly(int seed, int club = 5)
    {
        var game = GameModeTests.Create(seed, flag: false, globe: true, club);
        Assert.Equal(GameMode.WorldOnly, game.Mode);
        return game;
    }

    /// <summary>Eine Saison bis zum Bildschirm „Saisonende“ (passiv: Kader automatisch; aktiv: dazu Freundschaftsspiele).</summary>
    private static void PlaySeason(GameState game, bool active)
    {
        game.Continue(); // Sprung zur WM, Ernennung
        game.AutoNominate();
        if (active)
            for (var opponent = NationalTeams.First; opponent <= NationalTeams.Last; opponent++)
                if (opponent != NationalTeams.Germany)
                    game.PlayFriendly(opponent, home: opponent % 2 == 0);
        for (var guard = 0; game.Phase == SeasonPhase.Running; guard++)
        {
            Assert.True(guard < 100, "Saisonende nicht erreicht");
            game.Continue();
        }
        Assert.Equal(SeasonPhase.SeasonOver, game.Phase);
    }

    private static int[] Members(GameState game, League league) =>
        game.LeagueClubs(league).Select(c => c.Index).Order().ToArray();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WorldOnly_MultiSeason_OberligaExchangesLastTwoOfStartOrder_LeaguesOneTwoUnchanged(bool active)
    {
        // Roter Test für den alten Stand: Im Modus „nur Weltkugel“ verließ niemand die Oberliga. Mehrsaisonprobe: 4 Wechsel.
        var game = CreateWorldOnly(seed: 7);
        var bundesliga = Members(game, League.Bundesliga);
        var zweite = Members(game, League.ZweiteLiga);
        for (var change = 0; change < 4; change++)
        {
            PlaySeason(game, active);
            var record = game.History[^1];
            // Ohne Ligaspiele ist die Abschlusstabelle die Startordnung (Platzliste 0x5CCB): Letzter, dann Vorletzter.
            var start = record.FinalTables[League.Oberliga];
            Assert.Equal([start[^1], start[^2]], record.LeftOberliga);
            if (change == 0)
                Assert.Equal([35, 34], record.LeftOberliga); // FILE20 → FILE21: HEILBRONN (34) und BAD TÖLZ (35) gehen
            var arriving = record.PromotedInto[League.Oberliga];
            Assert.Equal(2, arriving.Count);
            Assert.NotEqual(arriving[0], arriving[1]);
            Assert.All(arriving, c => Assert.Null(game.Clubs[c].League));
            Assert.Empty(record.PromotedInto[League.Bundesliga]);
            Assert.Empty(record.PromotedInto[League.ZweiteLiga]);
            Assert.All(record.RelegatedInto.Values, Assert.Empty);
            Assert.Empty(record.Managers); // keine Prämien, kein Platz (Modus 1)

            var oberliga = Members(game, League.Oberliga).Except(record.LeftOberliga).Concat(arriving).Order().ToArray();
            game.StartNextSeason();
            Assert.Equal(bundesliga, Members(game, League.Bundesliga));
            Assert.Equal(zweite, Members(game, League.ZweiteLiga));
            Assert.Equal(oberliga, Members(game, League.Oberliga));
            Assert.All(record.LeftOberliga, c => Assert.Null(game.Clubs[c].League));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WorldOnly_MultiSeason_SeasonDriftMovesLeagueClubsWithinBounds_NationalTeamsKeepConditionAndTechnique(bool active)
    {
        // Roter Test für den alten Stand: Im Modus „nur Weltkugel“ wanderten die Teamwerte beim Saisonwechsel nicht.
        var game = CreateWorldOnly(seed: 19);
        for (var change = 0; change < 4; change++)
        {
            PlaySeason(game, active);
            var before = game.Clubs.Where(c => c.Index <= NationalTeams.Last)
                .ToDictionary(c => c.Index, c => MatchEngine.TeamValuesOf(c));
            game.StartNextSeason();
            var moved = 0;
            foreach (var club in game.Clubs.Where(c => c.Index <= NationalTeams.Last))
            {
                var old = before[club.Index];
                var now = MatchEngine.TeamValuesOf(club);
                Assert.InRange(now.DefenseForm, 45, 55);
                Assert.InRange(now.AttackForm, 45, 55);
                if (club.Index >= NationalTeams.First)
                {
                    Assert.Equal((old.DefenseCondition, old.AttackCondition, old.DefenseTechnique, old.AttackTechnique),
                        (now.DefenseCondition, now.AttackCondition, now.DefenseTechnique, now.AttackTechnique));
                    continue;
                }
                var (min, max) = MatchEngine.DriftBounds(club.League);
                foreach (var deviation in new[] { now.DefenseCondition, now.AttackCondition, now.DefenseTechnique, now.AttackTechnique })
                    Assert.InRange(club.Strength + deviation, min, max);
                if ((old.DefenseCondition, old.AttackCondition, old.DefenseTechnique, old.AttackTechnique)
                    != (now.DefenseCondition, now.AttackCondition, now.DefenseTechnique, now.AttackTechnique))
                    moved++;
            }
            Assert.True(moved >= 10, $"Saisonwechsel {change + 1}: nur {moved} Vereine gewandert");
        }
    }
}
