using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur saisonwechsel, Teil 6 (D5 = A, Entscheidung Nutzer 03.11.2026): Saisonende-Fenster und Startordnung beim Doppelwechsel wie
/// im Original. Aufbau wie der DOSBox-Lauf FILE16 → FILE17 (docs/analyse/re-saisonwechsel.md, „Auswertung D3“): SV Bayreuth
/// (Managerverein, 2. Liga, Zwangsabstieg) wird Zweiter der Aufstiegsrunde Bundesliga/2. Liga und steigt auf, EV Landshut
/// (Bundesliga, Platz 3) geht in die Oberliga; Gruppe 2. Liga/Oberliga: SC Riessersee, ESC Wolfsburg, EV Landsberg, SC Memmingen.
/// Beleg `file16_8_saisonende`: Spalte „Zweite Liga“ rot SC Riessersee; `file17_3`: Riessersee auf Platz 1 (Platz von Bayreuth),
/// Wolfsburg auf Platz 11 (Platz von Memmingen), übrige Plätze wie vorher.
/// </summary>
public class SaisonwechselDoppelwechselAnzeigeTests
{
    // Synthetische Indizes: 0–11 Bundesliga, 12–23 2. Liga, 24–35 Oberliga.
    private const int Deg = 0, Landshut = 3, Bayreuth = 12, Sauerland = 13, Memmingen = 22, Landsberg = 23, Wolfsburg = 24, Riessersee = 25;

    private static League Level(int c) => (League)(c / GameState.ClubsPerLeague);

    private static SeasonRecord D3Record(bool withManagerEvent = true)
    {
        int[] Table(int league, params int[] order) =>
            [.. order, .. Enumerable.Range(league * 12, 12).Where(c => !order.Contains(c))];
        var record = new SeasonRecord
        {
            Season = 0,
            FinalTables = new Dictionary<League, IReadOnlyList<int>>
            {
                [League.Bundesliga] = Table(0, Deg, 1, Landshut),
                // 2. Liga: Bayreuth Erster, Memmingen 11., Landsberg 12. (file16_4)
                [League.ZweiteLiga] = [Bayreuth, .. Enumerable.Range(13, 9), Memmingen, Landsberg],
                [League.Oberliga] = Table(2, Riessersee, Wolfsburg),
            },
        };
        int[] upper = [Deg, Bayreuth, Landshut, Sauerland];   // file16_6
        int[] lower = [Riessersee, Wolfsburg, Landsberg, Memmingen];
        GameState.InsertForcedRelegation(lower, 0, Bayreuth, c => Level(c) == League.ZweiteLiga);
        var (up1, down1) = GameState.RelegationExchange(upper, c => Level(c) == League.Bundesliga);
        var (up2, down2) = GameState.RelegationExchange(lower, c => Level(c) == League.ZweiteLiga);
        record.PromotedInto[League.Bundesliga].AddRange(up1);
        record.RelegatedInto[League.ZweiteLiga].AddRange(down1);
        record.PromotedInto[League.ZweiteLiga].AddRange(up2);
        record.RelegatedInto[League.Oberliga].AddRange(down2);
        GameState.ResolveDoubleMoves(record, upper, c => Level(c) == League.Bundesliga);
        if (withManagerEvent)
            record.Managers.Add(new ManagerSeasonResult(0, Bayreuth, League.ZweiteLiga, 1, 200_000, [SeasonEvent.ForcedRelegation, SeasonEvent.TopScorer]));
        return record;
    }

    private static List<int> Members(SeasonRecord record, League league) =>
        Enumerable.Range(0, 36).Where(c => record.NextLeague(c, Level(c)) == league).ToList();

    [Fact]
    public void Setup_MatchesFile17()
    {
        var record = D3Record();
        Assert.Equal([Bayreuth], record.PromotedInto[League.Bundesliga]);
        Assert.Empty(record.RelegatedInto[League.ZweiteLiga]);
        Assert.Equal([Riessersee, Wolfsburg], record.PromotedInto[League.ZweiteLiga]);
        Assert.Equal([Landshut, Memmingen], record.RelegatedInto[League.Oberliga]);
        Assert.All(Enum.GetValues<League>(), l => Assert.Equal(12, Members(record, l).Count));
    }

    [Fact]
    public void SeasonEndWindow_SecondLeagueColumn_ShowsClubOnSwappedPlace()
    {
        // Vorher: Spalte „Zweite Liga“ ohne roten Eintrag (Landshut nur in der Oberliga-Liste).
        var record = D3Record();
        Assert.Equal([Riessersee], GameState.ShownRelegatedInto(record, League.ZweiteLiga));
        Assert.Equal([Landshut, Memmingen], GameState.ShownRelegatedInto(record, League.Oberliga));
        Assert.Empty(GameState.ShownRelegatedInto(record, League.Bundesliga));
        // Daten bleiben: Riessersee steigt auf, Landshut geht in die Oberliga.
        Assert.Equal(League.ZweiteLiga, record.NextLeague(Riessersee, League.Oberliga));
        Assert.Equal(League.Oberliga, record.NextLeague(Landshut, League.Bundesliga));
    }

    [Fact]
    public void StartOrder_SecondLeague_RiesserseeOnBayreuthsPlace_WolfsburgOnMemmingensPlace()
    {
        // Roter Test für den alten Stand: Riessersee fand keinen Partner und stand hinten.
        var record = D3Record();
        var order = LeagueTable.SuccessorOrder(record, League.ZweiteLiga, Members(record, League.ZweiteLiga));
        Assert.Equal(12, order.Count);
        Assert.Equal(Riessersee, order[0]);
        Assert.Equal(Wolfsburg, order[10]);
        Assert.Equal(Landsberg, order[11]);
        Assert.Equal(Enumerable.Range(13, 9), order.Skip(1).Take(9));
    }

    [Fact]
    public void StartOrder_Bundesliga_BayreuthOnLandshutsPlace()
    {
        var record = D3Record();
        var order = LeagueTable.SuccessorOrder(record, League.Bundesliga, Members(record, League.Bundesliga));
        Assert.Equal(Bayreuth, order[2]);
        Assert.Equal([Deg, 1], order.Take(2));
    }

    [Fact]
    public void StartOrder_Oberliga_LandshutOnRiesserseesPlace_MemmingenOnWolfsburgsPlace()
    {
        var record = D3Record();
        var order = LeagueTable.SuccessorOrder(record, League.Oberliga, Members(record, League.Oberliga));
        Assert.Equal([Landshut, Memmingen], order.Take(2));
    }

    [Fact]
    public void WithoutManagerEvent_SinglePair_SameResult()
    {
        // [APPROX] Paarindex ohne Ereignis unbekannt: angehängt – bei einem Paar je Gruppe dasselbe Ergebnis.
        var record = D3Record(withManagerEvent: false);
        Assert.Equal([Riessersee], GameState.ShownRelegatedInto(record, League.ZweiteLiga));
        Assert.Equal(Riessersee, LeagueTable.SuccessorOrder(record, League.ZweiteLiga, Members(record, League.ZweiteLiga))[0]);
    }

    [Fact]
    public void NoDoubleMove_ListsUnchanged()
    {
        var record = new SeasonRecord
        {
            Season = 0,
            FinalTables = new Dictionary<League, IReadOnlyList<int>>
            {
                [League.Bundesliga] = Enumerable.Range(0, 12).ToArray(),
                [League.ZweiteLiga] = Enumerable.Range(12, 12).ToArray(),
                [League.Oberliga] = Enumerable.Range(24, 12).ToArray(),
            },
        };
        record.PromotedInto[League.Bundesliga].Add(12);
        record.RelegatedInto[League.ZweiteLiga].Add(11);
        record.PromotedInto[League.ZweiteLiga].Add(24);
        record.RelegatedInto[League.Oberliga].Add(23);
        foreach (var league in Enum.GetValues<League>())
        {
            Assert.Equal(record.RelegatedInto[league], GameState.ShownRelegatedInto(record, league));
            Assert.Equal(record.RelegatedInto[league], GameState.FirstExchangeRelegatedInto(record, league));
        }
    }
}
