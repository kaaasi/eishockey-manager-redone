using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Integration Runde 9 (Leitstelle): Anschlüsse zwischen den Spuren.
/// </summary>
public class IntegrationRunde9Tests
{
    /// <summary>
    /// Anschluss optionen-wirkung → spielberechnung-komplett (#292): Ein Pokalspiel ohne Managerverein läuft bei
    /// eingeschaltetem Anzeigeschalter durch die Minutenschleife (em.exe Datei 0x3702E–0x37085 → 0x37088), sonst über den
    /// schnellen Weg 0x3731F. Beide Wege verbrauchen den Zufall verschieden; vorher nahm <c>PlayWithEvents</c> den Schalter
    /// immer als aus.
    /// </summary>
    [Theory]
    [InlineData(MatchCompetition.DebCup)]
    [InlineData(MatchCompetition.EuropeanCup)]
    [InlineData(MatchCompetition.WorldCup)]
    public void CupMatchWithoutManager_FollowsDisplaySwitch(MatchCompetition competition)
    {
        var game = TestGames.Create(3);
        MatchEngine.EnsureTeamValues(game);
        var managed = game.Managers.Select(m => m.ClubIndex).ToHashSet();
        var clubs = game.Clubs.Where(c => c.Index < 12 && !managed.Contains(c.Index)).Take(2).ToList();

        int NextAfterMatch(bool display)
        {
            switch (competition)
            {
                case MatchCompetition.DebCup: game.Options.ShowDebCup = display; break;
                case MatchCompetition.EuropeanCup: game.Options.ShowEuropeanCup = display; break;
                default: game.Options.ShowWorldCup = display; break;
            }
            var random = new Random(7);
            MatchEngine.PlayWithEvents(game, clubs[0], clubs[1], competition, knockOut: false, random);
            return random.Next();
        }

        var fast = NextAfterMatch(display: false);
        Assert.Equal(fast, NextAfterMatch(display: false));
        Assert.NotEqual(fast, NextAfterMatch(display: true));
    }
}
