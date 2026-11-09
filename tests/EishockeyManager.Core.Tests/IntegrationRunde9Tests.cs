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

    /// <summary>
    /// R9S-03 (#351, Entscheidung Nutzer 01.11.2026): Play-offs und Aufstiegsrunde (Wettbewerbscode 1) ohne Managerverein
    /// nehmen den schnellen Weg 0x3731F, außer ein Feld „Ergebnisse“ der Meldungsmatrix (DGROUP 0x21E/0x221/0x224) ist an –
    /// dann die Minutenschleife (em.exe Datei 0x37058–0x37085). Voreingestellt ist die Matrix aus.
    /// </summary>
    [Theory]
    [InlineData(MatchCompetition.Playoff, true)]
    [InlineData(MatchCompetition.Relegation, false)]
    public void PlayoffWithoutManager_FastUnlessResultsReported(MatchCompetition competition, bool knockOut)
    {
        // #603 (R13KI-01): Saat wieder 3. Der Weg wird über die Testhaken der beiden Wege bestimmt, nicht mehr über den gleichen
        // Zufallsverbrauch wie ein DEB-Pokalspiel – der schnelle Weg mit Play-off-Kennung (FastCupResultWithTie mit rules.Playoff)
        // verbraucht je nach Teamwerten anders als der DEB-Pokal (Saat 3: 3:2 gegen 5:2).
        var game = TestGames.Create(3);
        MatchEngine.EnsureTeamValues(game);
        var managed = game.Managers.Select(m => m.ClubIndex).ToHashSet();
        var clubs = game.Clubs.Where(c => c.Index < 12 && !managed.Contains(c.Index)).Take(2).ToList();
        Assert.All(Enum.GetValues<League>(), l => Assert.False(game.Options.ShowsReport(ReportKind.Results, l)));

        (int Fast, int Minutes) Paths()
        {
            OriginalMinutes.FastCupCalls = 0;
            OriginalMinutes.MinuteLoopCalls = 0;
            MatchEngine.PlayWithEvents(game, clubs[0], clubs[1], competition, knockOut, new Random(7));
            return (OriginalMinutes.FastCupCalls, OriginalMinutes.MinuteLoopCalls);
        }

        var off = Paths();
        game.Options.ToggleReport(ReportKind.Results, League.Oberliga);
        var on = Paths();
        game.Options.ToggleReport(ReportKind.Results, League.Oberliga);

        Assert.Equal((1, 0), off); // schneller Weg 0x3731F
        Assert.Equal(0, on.Fast);
        Assert.True(on.Minutes > 0); // Minutenschleife
    }
}
