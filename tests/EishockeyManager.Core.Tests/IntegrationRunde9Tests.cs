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

    /// <summary>
    /// R9S-02 (#350): Die WM-Endrunde (Wettbewerbscode 10, Datei 0x31017) prüft den WM-Schalter nicht – 0x3702E vergleicht
    /// 0x229 nur bei Code 8 (WM-Gruppe); Code 10 fällt bis 0x37074 durch und springt zum schnellen Weg 0x3731F.
    /// </summary>
    [Fact]
    public void WorldCupKnockoutWithoutManager_AlwaysFastPath()
    {
        var game = TestGames.Create(3);
        MatchEngine.EnsureTeamValues(game);
        var managed = game.Managers.Select(m => m.ClubIndex).ToHashSet();
        var clubs = game.Clubs.Where(c => c.Index < 12 && !managed.Contains(c.Index)).Take(2).ToList();

        int NextAfterMatch(bool display)
        {
            game.Options.ShowWorldCup = display;
            var random = new Random(7);
            MatchEngine.PlayWithEvents(game, clubs[0], clubs[1], MatchCompetition.WorldCup, knockOut: true, random);
            return random.Next();
        }

        Assert.Equal(NextAfterMatch(display: false), NextAfterMatch(display: true));
    }
}
