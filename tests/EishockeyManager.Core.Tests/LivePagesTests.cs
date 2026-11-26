using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur live-verschraenkt, Teil 3 (S04: A292, A436, F068; <c>docs/analyse/live-spieltag.md</c> §8): Seiten nach dem Drittel.
/// Erwartete Werte aus der Nachbildung <c>tools/reverse-engineering/live/drittelseiten.py</c> (0x37A62 Datei 0x3858C–0x386CD und
/// Seitenwähler 0x3A88A Datei 0x3A8A3–0x3AA01 aus den Bytes von em.exe).
/// </summary>
public sealed class LivePagesTests
{
    private static GameOptions With(params int[] on)
    {
        var o = new GameOptions { ShowDebCup = false };
        foreach (var i in on)
        {
            switch (i)
            {
                case < 9: o.Reports[i] = true; break;
                case 10: o.ShowDebCup = true; break;
                case 11: o.ShowEuropeanCup = true; break;
                case 12: o.ShowWorldCup = true; break;
            }
        }
        return o;
    }

    private static string Pages(GameOptions o, MatchCompetition c, bool knockOut = false, int game = 1, bool afterShootout = false) =>
        string.Join(" | ", new[] { 20, 40, 60 }.Select(m => m + ":" + string.Join(",",
            LivePages.After(o, m, c, [League.Bundesliga, League.ZweiteLiga, League.Oberliga], knockOut, game,
                afterShootout: afterShootout).Select(p => p.Kind == LivePageKind.LeaguePairings ? "L" + (int)p.League : p.Kind.ToString()))));

    [Fact]
    public void LeagueDay_IntermissionAndResultsPerLeague()
    {
        // drittelseiten.py Fall 1
        Assert.Equal("20: | 40: | 60:", Pages(With(), MatchCompetition.League));
        Assert.Equal("20:L0 | 40:L0 | 60:", Pages(With(0), MatchCompetition.League));
        Assert.Equal("20: | 40: | 60:L1", Pages(With(4), MatchCompetition.League));
        Assert.Equal("20: | 40: | 60:", Pages(With(2), MatchCompetition.League)); // „Tabelle“ zeigt keine Seite im Spiel
        Assert.Equal("20:L0,L1,L2 | 40:L0,L1,L2 | 60:L0,L1,L2", Pages(With(0, 1, 3, 4, 6, 7), MatchCompetition.League));
    }

    [Fact]
    public void DebCup_DefaultOptions_OnlyMinute60()
    {
        // drittelseiten.py Fall 2, DEB-Pokal (Code 6)
        Assert.Equal("20: | 40: | 60:DebCupGames", Pages(With(10), MatchCompetition.DebCup));
        Assert.Equal("20: | 40: | 60:", Pages(With(), MatchCompetition.DebCup));
        Assert.Equal("20:DebCupGames | 40:DebCupGames | 60:", Pages(With(0), MatchCompetition.DebCup));
        Assert.Equal("20:DebCupGames | 40:DebCupGames | 60:DebCupGames", Pages(With(0, 10), MatchCompetition.DebCup));
        Assert.Equal("20: | 40: | 60:", Pages(With(1, 3, 4), MatchCompetition.DebCup));
        Assert.True(new GameOptions().ShowDebCup); // [ORIGINAL] Anfangswert DGROUP 0x227 = 1
    }

    [Fact]
    public void Playoffs_PageOneAndPromotionRound()
    {
        // drittelseiten.py Fall 2, Code 1: Spiel 1 ohne Aufstiegsrunde, Spiel 0 mit
        Assert.Equal("20:PlayoffGames | 40:PlayoffGames | 60:", Pages(With(0), MatchCompetition.Playoff));
        Assert.Equal("20: | 40: | 60:PlayoffGames", Pages(With(1), MatchCompetition.Playoff));
        Assert.Equal("20: | 40: | 60:", Pages(With(3, 4), MatchCompetition.Playoff, game: 1));
        Assert.Equal("20:PromotionRound | 40:PromotionRound | 60:PromotionRound", Pages(With(3, 4), MatchCompetition.Playoff, game: 0));
        Assert.Equal("20: | 40: | 60:", Pages(With(10, 11, 12), MatchCompetition.Playoff, game: 0));
    }

    [Fact]
    public void EuropeanAndWorldCup_SwitchPlusIntermission()
    {
        Assert.Equal("20: | 40: | 60:EuropeanCupTable", Pages(With(11), MatchCompetition.EuropeanCup));
        Assert.Equal("20:EuropeanCupTable | 40:EuropeanCupTable | 60:EuropeanCupTable", Pages(With(0, 11), MatchCompetition.EuropeanCup));
        Assert.Equal("20: | 40: | 60:", Pages(With(0), MatchCompetition.EuropeanCup));
        Assert.Equal("20: | 40: | 60:WorldCupGroups", Pages(With(12), MatchCompetition.WorldCup));
        Assert.Equal("20:WorldCupGroups | 40:WorldCupGroups | 60:WorldCupGroups", Pages(With(0, 12), MatchCompetition.WorldCup));
        Assert.Equal("20: | 40: | 60:WorldCupKnockout", Pages(With(12), MatchCompetition.WorldCup, knockOut: true));
        Assert.Equal("20: | 40: | 60:", Pages(With(0, 10, 11), MatchCompetition.WorldCup, knockOut: true));
        Assert.Equal("20: | 40: | 60:", Pages(With(0, 1, 10, 11, 12), MatchCompetition.Friendly));
    }

    [Fact]
    public void AfterShootoutAndOvertime_Minute70()
    {
        // drittelseiten.py Fall 3: nach dem Penaltyschießen 0x3A88A(Code, 0, 70)
        Assert.Single(LivePages.After(With(10), 70, MatchCompetition.DebCup, afterShootout: true));
        Assert.Empty(LivePages.After(With(), 70, MatchCompetition.DebCup, afterShootout: true));
        Assert.Single(LivePages.After(With(1), 70, MatchCompetition.Playoff, afterShootout: true));
        Assert.Empty(LivePages.After(With(0), 70, MatchCompetition.Playoff, afterShootout: true));
        Assert.Single(LivePages.After(With(12), 70, MatchCompetition.WorldCup, worldCupKnockout: true, afterShootout: true));
        // Seite 2 nur am Drittelende, nicht nach dem Penaltyschießen (Datei 0x372A7 ruft nur die Seite des Codes)
        Assert.Equal([LivePageKind.PlayoffGames], LivePages.After(With(1, 3), 70, MatchCompetition.Playoff, playoffGame: 0,
            afterShootout: true).Select(p => p.Kind));
        Assert.Equal([LivePageKind.PlayoffGames, LivePageKind.PromotionRound],
            LivePages.After(With(1, 3), 65, MatchCompetition.Playoff, playoffGame: 0).Select(p => p.Kind));
        Assert.Empty(LivePages.After(With(0, 1, 10), 30, MatchCompetition.DebCup)); // kein Drittelende
    }

    [Fact]
    public void PermanentRink_NoPages()
    {
        // drittelseiten.py Fall 1 und 4: DGROUP 0x5A55 = 1
        foreach (var c in Enum.GetValues<MatchCompetition>())
            foreach (var m in new[] { 20, 40, 60, 70 })
                Assert.Empty(LivePages.After(With(0, 1, 3, 4, 6, 7, 10, 11, 12), m, c, [League.Bundesliga], permanentRink: true));
    }
}
