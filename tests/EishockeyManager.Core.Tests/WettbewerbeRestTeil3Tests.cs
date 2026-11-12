using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur wettbewerbe-rest (Runde 17), Teil 3: Spielplan und Start (A113, A142).
/// </summary>
public class WettbewerbeRestTeil3Tests
{
    private static readonly string[] OriginalTable =
    [
        "ab cd ef gh ij kl", "da fc he jg li bk", "ah gb cj id ke fl", "ea bi lc df kg hj", "al jb ce gd fh ik", "ac lb dj ei fg kh",
        "ja be kc gl hd if", "ak fb cg ld ej hi", "fa bc de lh gi jk", "ag hb kd ic jf el", "ia bd ch fk eg lj",
    ];

    /// <summary>Paarungen eines Spieltags als Buchstaben der Satzfolge (a = Platz 0).</summary>
    private static string Letters(MatchDay day, IReadOnlyList<int> slots) =>
        string.Join(" ", day.Matches.Select(m => $"{(char)('a' + slots.ToList().IndexOf(m.Home))}{(char)('a' + slots.ToList().IndexOf(m.Away))}"));

    private static string Swapped(string row) => string.Join(" ", row.Split(' ').Select(p => $"{p[1]}{p[0]}"));

    /// <summary>Plan einer Liga gegen die feste Tabelle DGROUP 0x2DC über die aktuelle Satzfolge.</summary>
    private static void AssertPlanFollowsSlots(GameState game, League league)
    {
        var slots = game.LeagueSlots(league);
        var plan = game.Schedules[league];
        Assert.Equal(12, slots.Count);
        Assert.Equal(44, plan.Count);
        for (var d = 0; d < 44; d++)
        {
            var row = OriginalTable[d % 11];
            var swap = d / 11 % 2 == 1;
            Assert.Equal(swap ? Swapped(row) : row, Letters(plan[d], slots));
        }
    }

    /// <summary>
    /// A113: [ORIGINAL] Datei 0x4E7C7 schreibt Satzplätze (Buchstabe − 'a' + Basis). Beim Spielstart ist die Satzfolge die
    /// Startmischung (Datei 0x43A17) – der Plan des Remakes richtete sich vorher nach dem Rang des Vereinsindex.
    /// </summary>
    [Theory]
    [InlineData(3)]
    [InlineData(17)]
    public void NewGame_LeaguePlan_FollowsSlotOrder(int seed)
    {
        var game = TestGames.Create(seed);
        foreach (var league in Enum.GetValues<League>())
        {
            Assert.False(game.LeagueSlots(league).SequenceEqual(game.LeagueSlots(league).Order()), "Satzfolge ungemischt");
            AssertPlanFollowsSlots(game, league);
        }
    }

    /// <summary>
    /// A113, Mehrsaisonprobe (4 Saisonwechsel, AGENTS.md §5): Nach jeder Mischung im Saisonwechsel (Datei 0x5644E) folgt der
    /// Plan der neuen Satzfolge; jede Paarung genau einmal je Runde. Passiver und aktiver Manager.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultiSeason_FourChanges_PlanFollowsSlotOrder(bool active)
    {
        var game = TestGames.Create(11);
        var manager = game.ActiveManager;
        for (var change = 0; change < 4; change++)
        {
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 1000, "Saisonende nicht erreicht");
                foreach (var m in game.Managers)
                    m.Money = Math.Max(m.Money, 5_000_000);
                if (active)
                    TestSeason.KeepSquad(game, manager, extendExpired: true);
                game.Continue();
            }
            Assert.Equal(SeasonPhase.SeasonOver, game.Phase);
            var season = game.Season;
            game.StartNextSeason();
            Assert.Equal(season + 1, game.Season);
            foreach (var league in Enum.GetValues<League>())
            {
                AssertPlanFollowsSlots(game, league);
                var first = game.Schedules[league].Take(11).SelectMany(d => d.Matches)
                    .Select(m => (Math.Min(m.Home, m.Away), Math.Max(m.Home, m.Away))).ToList();
                Assert.Equal(66, first.Distinct().Count());
            }
        }
    }

    /// <summary>
    /// A113/A142: [ORIGINAL] Aufstiegsrunde nach der Vierergruppen-Tabelle DGROUP 0x324 über Datei 0x4E7C7 (Aufruf Datei
    /// 0x3068B), Buchstabe = Platz der Teilnehmerliste (11., 12. oben, 1., 2. unten). Video <c>folge07_03307</c>: Berliner SC
    /// (a) – EA Kempten (b), ECD Sauerland (c) – SC Riessersee (d); dann d–a, b–c; dann a–c, b–d. Vorher Kreisverfahren nach
    /// Zufallsmischung.
    /// </summary>
    [Fact]
    public void RelegationGroup_UsesFourTeamTableInParticipantOrder()
    {
        int[] t = [10, 11, 12, 13];
        var group = new RelegationGroup(League.Bundesliga, t, new Random(7));
        string Day(int r) => string.Join(" ", group.Rounds[r].Matches.Select(m => $"{(char)('a' + Array.IndexOf(t, m.Home))}{(char)('a' + Array.IndexOf(t, m.Away))}"));

        Assert.Equal(6, group.Rounds.Count);
        Assert.Equal(["ab cd", "da bc", "ac bd", "ba dc", "ad cb", "ca db"], Enumerable.Range(0, 6).Select(Day));
    }

    /// <summary>
    /// A142: [ORIGINAL] Aufstiegsrunde an den Play-Off-Spielen 0 und 2 jeder Runde (DGROUP 0xCCA3 ∈ {0, 2}, Datei 0x31A2C), also an
    /// den Play-Off-Terminen 0, 2, 5, 7, 10, 12 = 6.2., 10.2., 20.2., 25.2., 22.3., 29.3. (Video <c>folge07_03307</c>).
    /// </summary>
    [Fact]
    public void RelegationDates_AsOriginal()
    {
        var days = SeasonCalendar.DaysOf(DayEvents.Playoffs);
        var dates = RelegationGroup.DateIndices.Select(i => SeasonCalendar.LayoutDate(days[i])).Select(d => $"{d.Day}.{d.Month}.");
        Assert.Equal(["6.2.", "10.2.", "20.2.", "25.2.", "22.3.", "29.3."], dates);
    }
}
