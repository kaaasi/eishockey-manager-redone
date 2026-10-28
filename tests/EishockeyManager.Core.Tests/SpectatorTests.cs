using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur finanzen-2, Teil „Zuschauer“: Formel aus em.exe 0x3F185 (Belege: docs/finanzen-rekonstruktion.md).
/// Gegen den Stand 5f8e02a rot (dort Nachfrage × Faktoren, Gastronomie-Einnahme).
/// </summary>
public class SpectatorTests
{
    /// <summary>random(a, b) liefert immer die untere Grenze – macht die Formel von Hand nachrechenbar.</summary>
    private sealed class MinRandom : Random
    {
        public override int Next(int minValue, int maxValue) => minValue;
        public override double NextDouble() => 0;
    }

    /// <summary>random(a, b) liefert immer die obere Grenze.</summary>
    private sealed class MaxRandom : Random
    {
        public override int Next(int minValue, int maxValue) => maxValue - 1;
        public override double NextDouble() => 0.999;
    }

    [Fact]
    public void Oberliga_StartStadium_ElevenMarks_LowerAndUpperBound()
    {
        // Hand nachgerechnet (Oberliga, Stärken 30/30, Plätze 5/5, Level 3, 11 DM, Startstadion 2.000/2.500, Zustand MITTEL):
        // f = ((58 + 58) / 2 + 136) / 2 = 97; z = 70·97 + 1200 = 7990; Zustand 2 × random(50, 133); + 500·2; + random(1400, 1800)
        // (Stehplätze < Sitzplätze + 2000); H = 62; K = 100 − 5·16 = 20 − random(4, 10); × (100 + K·45/50) / 100; − 9000;
        // × 99/100 (Zustand) × 95/100 (Sitzkomfort). Untere Grenze 2.781, obere 2.869 – Video Saison 1: Schnitt 3.049.
        var stadium = Stadium.ForLeague(League.Oberliga);
        stadium.TicketPrice = 11;
        Assert.Equal(2781, SpectatorRules.Compute(stadium, 30, 30, 5, 5, 2, 2, 0, 2, new MinRandom()));
        Assert.Equal(2869, SpectatorRules.Compute(stadium, 30, 30, 5, 5, 2, 2, 0, 2, new MaxRandom()));
    }

    [Fact]
    public void Bundesliga_StartStadium_SellsOutForStrongTeams()
    {
        // Bundesliga-Startstadion (3.000/4.500, HALLE, Anzeigetafel, Kühlanlage, Verkehr, Imbißbude), 13 DM, Stärken 70/70:
        // z = 70·122 + 4800 + 1748 (Ausbauten, untere Grenzen) + 1000 + 1400 = 17.488; K = 100 − 5·19 − 1 = 4; H = 90;
        // × 101/100 − 9000 = 8.662; × 99/100 × 95/100 = 8.146 > 7.500 Plätze → ausverkauft.
        var stadium = Stadium.ForLeague(League.Bundesliga);
        Assert.Equal(7500, SpectatorRules.Compute(stadium, 70, 70, 5, 5, 0, 0, 0, 2, new MinRandom()));
    }

    [Fact]
    public void HigherPrice_LowersAttendance_AndCupBonusRaisesIt()
    {
        var stadium = Stadium.ForLeague(League.Oberliga);
        stadium.Seats = 15_000;
        stadium.Standing = 15_000;
        int At(int price, int importance)
        {
            stadium.TicketPrice = price;
            return SpectatorRules.Compute(stadium, 30, 30, 5, 5, 2, 2, importance, 2, new MinRandom());
        }
        Assert.True(At(10, 0) > At(14, 0));
        Assert.True(At(14, 0) > At(20, 0));
        Assert.True(At(10, 2) > At(10, 1));
        Assert.True(At(10, 1) > At(10, 0));
    }

    [Fact]
    public void CrossLeagueGuests_LeaderWithBonusFromAbove_LastWithMalusFromBelow()
    {
        // [ORIGINAL] 0x3F1AF/0x3F32C: Gast aus einer höheren Liga zählt als Tabellenführer und bringt 3.500 je Ligastufe;
        // Gast aus einer niedrigeren Liga zählt als Letzter und kostet 800 je Ligastufe.
        var stadium = Stadium.ForLeague(League.Oberliga);
        stadium.Seats = 15_000;
        stadium.Standing = 15_000;
        var leader = SpectatorRules.Compute(stadium, 30, 30, 5, 0, 2, 2, 0, 2, new MinRandom());
        var fromBundesliga = SpectatorRules.Compute(stadium, 30, 30, 5, 5, 2, 0, 0, 2, new MinRandom());
        Assert.True(fromBundesliga > leader + 7000 * 99 * 95 / 10000 - 100, $"leader={leader} fromBundesliga={fromBundesliga}");

        var hall = Stadium.ForLeague(League.Bundesliga);
        hall.Seats = 15_000;
        hall.Standing = 15_000;
        var againstLast = SpectatorRules.Compute(hall, 60, 60, 5, 11, 0, 0, 0, 2, new MinRandom());
        var againstSecondLeague = SpectatorRules.Compute(hall, 60, 60, 5, 5, 0, 1, 0, 2, new MinRandom());
        Assert.True(againstSecondLeague < againstLast, $"againstLast={againstLast} againstSecondLeague={againstSecondLeague}");
        Assert.True(againstSecondLeague > againstLast - 1000, $"againstLast={againstLast} againstSecondLeague={againstSecondLeague}");
    }

    [Fact]
    public void HomeMatch_BooksTicketsOnly_NoGastronomy()
    {
        var game = TestGames.Create();
        var me = game.ActiveManager;
        var stadium = game.Finance(me).Stadium;
        stadium.Seats = 100;
        stadium.Standing = 0;
        stadium.TicketPrice = 9;
        stadium.SetLevel(StadiumFeature.Gastronomie, 3);
        var book = game.Finance(me).Book;
        var before = book.CurrentTotal;

        game.SettleHomeMatch(me.ClubIndex, (me.ClubIndex + 1) % 12, 1);

        Assert.Equal(900, book.Current(FinanceCategory.Eintritt));
        Assert.Equal(900, book.CurrentTotal - before);
        Assert.Equal((100, 900L), game.Finance(me).LastHomeGame);
    }

    [Fact]
    public void Attendance_StaysBetweenFiftyAndCapacity()
    {
        var random = new Random(7);
        var stadium = Stadium.ForLeague(League.ZweiteLiga);
        for (var i = 0; i < 2000; i++)
        {
            stadium.TicketPrice = Stadium.MinTicketPrice + random.Next(Stadium.MaxTicketPrice - Stadium.MinTicketPrice + 1);
            var z = SpectatorRules.Compute(stadium, random.Next(20, 90), random.Next(20, 90), random.Next(12), random.Next(12),
                1, random.Next(3), random.Next(3), random.Next(6), random);
            Assert.InRange(z, 50, stadium.Capacity);
        }
    }
}
