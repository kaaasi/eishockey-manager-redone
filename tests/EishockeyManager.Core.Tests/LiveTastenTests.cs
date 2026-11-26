using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur live-verschraenkt, Teil 4 (S05, Leertaste als Teil von A449; <c>docs/analyse/live-spieltag.md</c> §2 Fall 2, §4 E6):
/// [ORIGINAL] Datei 0x37D0B–0x37D89 – nach der Leertaste springt die Minutenschleife 0x37A62 an das Minutenende
/// (<c>jmp 0x38551</c>): in dieser Minute rechnet keine Paarung, die Schleife zählt weiter, das Ende der Schleife (0x32AA3,
/// Verlängerung bei Gleichstand nach 60) bleibt. Vor dem Teil rot: <see cref="LiveMatchDay.Skip"/> fehlte (Build-Fehler).
/// </summary>
public sealed class LiveTastenTests
{
    private static GameState NewGame(int seed)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", Head = 0, ClubIndex = 2 });
        return Core.NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    private static LiveMatchDay Begin(GameState game, Random random, bool knockOut) =>
        LiveMatchDay.Begin(game, Enumerable.Range(0, 6)
            .Select(i => new LiveFixture(game.Clubs[2 * i], game.Clubs[2 * i + 1], MatchCompetition.League, knockOut, random)));

    /// <summary>Übersprungene Minute: keine Ereignisse, kein Zufallszug, Minute und letzte Minute aller Paarungen weiter.</summary>
    [Fact]
    public void Skip_NoPairingComputed_NoRandomDrawn()
    {
        var randomA = new Random(7);
        var randomB = new Random(7);
        var a = Begin(NewGame(3), randomA, knockOut: false);
        Begin(NewGame(3), randomB, knockOut: false);
        for (var m = 1; m <= 4; m++)
            Assert.True(a.Skip());
        Assert.Equal(4, a.Minute);
        Assert.All(a.Matches.Where(m => m.Stepped), m => Assert.Equal(4, m.LastMinute));
        Assert.All(a.Matches, m => Assert.Empty(m.Report.Events));
        Assert.Equal(randomB.Next(), randomA.Next());
        Assert.True(a.Step());
        Assert.Equal(5, a.Minute);
        a.Finish();
    }

    /// <summary>
    /// Leertaste in Minute 60 eines K.-o.-Tags: das Ende der Schleife bleibt – entschiedene Paarungen enden, Paarungen mit
    /// Gleichstand gehen in die Verlängerung ([ORIGINAL] 0x32AA3 Schalter 0 nach dem Drittel, Datei 0x37211).
    /// </summary>
    [Fact]
    public void SkipMinute60_KnockOut_TiedPairingsPlayOvertime()
    {
        var found = false;
        for (var seed = 0; seed < 40 && !found; seed++)
        {
            var day = Begin(NewGame(seed), new Random(seed), knockOut: true);
            while (day.Minute < 59)
                day.Step();
            if (day.Matches.All(m => m.Report.HomeGoals != m.Report.AwayGoals))
            {
                day.Finish();
                continue;
            }
            found = true;
            Assert.True(day.Skip());
            Assert.All(day.Matches.Where(m => m.Stepped),
                m => Assert.Equal(m.Report.HomeGoals == m.Report.AwayGoals, m.Running));
            Assert.All(day.Matches, m => Assert.DoesNotContain(m.Report.Events, e => e.Minute == 60));
            day.Finish();
            Assert.All(day.Matches.Where(m => m.Report.Overtime), m => Assert.Equal(70, m.LastMinute));
        }
        Assert.True(found, "kein K.-o.-Tag mit Gleichstand nach 59 Minuten gefunden");
    }
}
