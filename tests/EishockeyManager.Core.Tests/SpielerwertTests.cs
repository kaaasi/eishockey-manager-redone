using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur spielerwert: Wert und Gehalt eines Spielers nach em.exe 0x49D36 (0x118:0x70) und ihr Einsatz in Highscore
/// (0x5D04F) und Werbebewertung (0x3F833).
/// </summary>
public class SpielerwertTests
{
    /// <summary>
    /// Sollwerte aus der Originalfunktion: eigener 8086-Emulator lief 0x49D36 samt LXMUL/LDIV/LXLSH (Datei 0x21CA, 0x227C,
    /// 0x232A), clamp 0x4BD35 und Spielklasse 0x34FD5 aus em.exe; random(95, 100) (0xC037) lieferte den Wert der Spalte
    /// „Zufall“. Spalten: 5-Min, 2-Min, Tore +3/+4/+5, Tendenz, KO, TE, Spiele +0x1C/+0x1E/+0x20, Angebot (Verein),
    /// Alter, Torhüter, Spielklasse, [0xED8], Argument, Zufall, Ergebnis (Drittel-DM), Zahl der Zufallszüge. Die Fälle sind aus
    /// 2.000 Zufallsfällen (alle gleich) nach Zweigen ausgewählt; die letzten vier sind von Hand (u. a. LAPORTE 76/79, 19 J.).
    /// </summary>
    public static IEnumerable<object?[]> OriginalCases => new List<object?[]>
    {
        new object?[] { 0, 1, 178, 36, 0, 31, 107, 15, 125, 49615, 0, null, 20, false, 2, 3, 0, 99, 933000, 0 },
        new object?[] { 1, 0, 0, 0, 0, 35, 62, 101, 27, 0, 0, null, 60, false, 1, 2, 0, 99, 633000, 0 },
        new object?[] { 1, 0, 0, 67, 66, 42, 52, 76, 351, 4338, 0, null, 46, false, 1, 5, 0, 98, 633000, 0 },
        new object?[] { 99, 3, 40, 0, 157, 58, 33, 111, 48, 14136, 44, null, 47, false, 1, 2, 0, 95, 750000, 0 },
        new object?[] { 143, 12, 160, 0, 191, 49, 97, 48, 59, 0, 592, null, 44, true, 4, 3, 0, 96, 1200000, 0 },
        new object?[] { 1, 3, 242, 174, 139, 62, 38, 31, 60, 552, 0, null, 46, true, 4, 2, 0, 98, 504000, 0 },
        new object?[] { 2, 3, 21, 1, 0, 40, 5, 70, 57, 104, 52, 0, 16, true, 1, 1, 3, 95, 18900, 0 },
        new object?[] { 2, 216, 0, 0, 0, 52, 74, 70, 209, 12, 516, null, 59, false, 1, 1, 3, 97, 55200, 0 },
        new object?[] { 52, 0, 239, 41, 5, 60, 119, 19, 39422, 560, 119, null, 45, false, 1, 5, 3, 96, 48300, 0 },
        new object?[] { 2, 1, 0, 159, 0, 55, 80, 3, 50, 9982, 22, null, 45, true, 2, 3, 3, 95, 22200, 0 },
        new object?[] { 197, 0, 0, 0, 21, 49, 89, 58, 0, 0, 32096, 20, 33, true, 2, 3, 3, 98, 82200, 0 },
        new object?[] { 2, 3, 129, 51, 0, 67, 42, 8, 280, 567, 29, null, 29, true, 4, 3, 1, 99, 17700, 0 },
        new object?[] { 159, 8, 39, 44, 0, 64, 56, 66, 24814, 0, 21886, 63, 35, true, 4, 2, 3, 96, 79800, 0 },
        new object?[] { 2, 60, 24, 89, 0, 50, 114, 107, 70, 12, 42, null, 50, true, 4, 3, 3, 95, 251100, 0 },
        new object?[] { 0, 0, 0, 0, 5, 35, 118, 98, 49, 53, 1, null, 40, false, 4, 5, 1, 96, 66900, 0 },
        new object?[] { 1, 0, 51, 0, 93, 47, 28, 108, 12794, 22, 0, null, 27, false, 1, 4, 2, 96, 309000, 0 },
        new object?[] { 1, 3, 0, 0, 64, 31, 29, 54, 47751, 5, 0, null, 18, true, 2, 2, 2, 97, 426000, 0 },
        new object?[] { 1, 199, 23, 29, 24, 53, 45, 102, 28064, 50695, 0, 64, 42, true, 1, 1, 2, 97, 681000, 0 },
        new object?[] { 1, 1, 186, 0, 0, 64, 10, 62, 334, 55, 0, 64, 41, false, 1, 5, 2, 95, 213000, 0 },
        new object?[] { 150, 156, 7, 0, 224, 54, 61, 24, 188, 162, 61331, 20, 47, false, 4, 0, 2, 96, 144000, 1 },
        new object?[] { 2, 0, 0, 0, 0, 66, 34, 37, 157, 36, 45301, 0, 44, true, 1, 2, 0, 99, 501000, 1 },
        new object?[] { 0, 0, 0, 4, 0, 34, 3, 22, 30582, 20489, 528, 63, 47, true, 4, 5, 2, 100, 96000, 1 },
        new object?[] { 0, 57, 218, 21, 54, 67, 71, 20, 36, 40287, 45, null, 35, true, 4, 1, 1, 100, 53100, 0 },
        new object?[] { 1, 3, 36, 22, 173, 34, 101, 42, 0, 26020, 54, null, 43, false, 1, 5, 1, 97, 39300, 0 },
        new object?[] { 0, 1, 132, 0, 13, 54, 20, 108, 55624, 464, 0, 100, 48, true, 1, 1, 3, 95, 66300, 0 },
        new object?[] { 2, 140, 9, 246, 31, 64, 26, 117, 336, 0, 21, null, 48, false, 1, 2, 3, 99, 66000, 0 },
        new object?[] { 0, 0, 0, 0, 0, 49, 76, 79, 0, 0, 0, null, 19, false, 1, 2, 0, 95, 999000, 0 },
        new object?[] { 0, 0, 0, 0, 0, 49, 76, 79, 0, 0, 0, null, 19, false, 1, 2, 1, 95, 29700, 0 },
        new object?[] { 0, 0, 0, 0, 0, 30, 1, 1, 0, 0, 0, null, 60, false, 4, 5, 1, 95, 1500, 0 },
        new object?[] { 0, 0, 0, 0, 0, 30, 1, 1, 0, 0, 0, null, 60, false, 4, 5, 0, 95, 15000, 0 },
    };

    [Theory]
    [MemberData(nameof(OriginalCases))]
    public void Compute_MatchesOriginalFunction(int pen5, int pen2, int g0, int g1, int g2, int trend, int condition, int technique,
        int games0, int games1, int games2, int? offer, int age, bool goalkeeper, int clubClass, int levelValue, int mode, int draw,
        int expected, int draws)
    {
        var inputs = new PlayerValueInputs
        {
            Penalties5 = pen5, Penalties2 = pen2, Goals0 = g0, Goals1 = g1, Goals2 = g2, Trend = trend,
            Condition = condition, Technique = technique, Games0 = games0, Games1 = games1, Games2 = games2,
            OfferClub = offer, Age = age, Goalkeeper = goalkeeper, ClubClass = clubClass, LevelValue = levelValue,
        };
        var calls = new List<(int, int)>();
        var value = PlayerValue.Compute(inputs, (PlayerValueKind)(mode & 1), (a, b) => { calls.Add((a, b)); return draw; }, (mode & 2) != 0);
        Assert.Equal(expected, value);
        Assert.Equal(draws, calls.Count);
        Assert.All(calls, c => Assert.Equal((95, 100), c));
    }

    private static PlayerValueInputs Forward => new() { Trend = 49, Condition = 60, Technique = 60, Age = 24, ClubClass = 1, LevelValue = 2 };

    /// <summary>[ORIGINAL] 0x4A0C5: Angebot eines Vereins ab Index 64 → · 160 / 100 ohne Zufall, sonst · random(95, 100) / 100.</summary>
    [Fact]
    public void Offer_ForeignClubRaisesPrice_OtherClubsDrawRandom()
    {
        var plain = PlayerValue.Compute(Forward, PlayerValueKind.Price, (_, _) => throw new InvalidOperationException());
        var foreign = PlayerValue.Compute(Forward with { OfferClub = 64 }, PlayerValueKind.Price, (_, _) => throw new InvalidOperationException());
        var draws = 0;
        var german = PlayerValue.Compute(Forward with { OfferClub = 63 }, PlayerValueKind.Price, (_, b) => { draws++; return b; });
        Assert.True(foreign > plain);
        Assert.Equal(plain, german); // random = 100 → unverändert
        Assert.Equal(1, draws);
        // Gehalt: das Angebot spielt keine Rolle und zieht nichts.
        Assert.Equal(PlayerValue.Compute(Forward, PlayerValueKind.Salary, (_, _) => 0),
            PlayerValue.Compute(Forward with { OfferClub = 63 }, PlayerValueKind.Salary, (_, _) => throw new InvalidOperationException()));
    }

    /// <summary>[ORIGINAL] 0x49F96: Gehalt mit dem Level – je schwerer (kleineres 5 − Level), desto teurer.</summary>
    [Fact]
    public void Salary_RisesWithLevel_AndRoundsTo300()
    {
        var easy = PlayerValue.Compute(Forward with { LevelValue = 5 }, PlayerValueKind.Salary, (_, _) => 0);
        var hard = PlayerValue.Compute(Forward with { LevelValue = 0 }, PlayerValueKind.Salary, (_, _) => 0);
        Assert.True(hard > easy);
        Assert.Equal(0, easy % 300);
        Assert.Equal(0, PlayerValue.Compute(Forward, PlayerValueKind.Price, (_, _) => 0) % 3000);
    }

    /// <summary>Remake → Eingänge: Torhüter, Spielklasse nach der aktuellen Liga, vertragslos = Klasse 1, Tendenz-Mitten.</summary>
    [Fact]
    public void Inputs_FromRemakePlayer()
    {
        var game = TestGames.Create();
        var club = game.Clubs.First(c => c.League == League.ZweiteLiga && c.Players.Count > 0);
        var goalie = club.Players.First(p => p.Role == PlayerRole.Goalkeeper);
        goalie.Trend = PlayerTrend.Rising;
        var x = PlayerValue.Inputs(goalie, club, level: 4);
        Assert.True(x.Goalkeeper);
        Assert.Equal(2, x.ClubClass);
        Assert.Equal(62, x.Trend);
        Assert.Equal(1, x.LevelValue);
        Assert.Equal(goalie.Condition, x.Condition);
        Assert.Equal(goalie.Technique, x.Technique);
        Assert.Equal(1, PlayerValue.Inputs(goalie, null).ClubClass);
        Assert.Null(x.OfferClub);
    }

    /// <summary>
    /// Regression (vorher rot: „Kaufpreis · 3 / 2“): Das Vermögen der Highscore zählt je Kaderspieler den halben Wert nach
    /// 0x118:0x70 (Datei 0x5D242–0x5D28A).
    /// </summary>
    [Fact]
    public void Highscore_Wealth_CountsHalfPlayerValue()
    {
        var game = TestGames.Create();
        var m = game.Managers[0];
        var club = game.Clubs[m.ClubIndex];
        var marks = m.Money + game.DepotValue(m) + game.ManagerReceivables(m) - game.ManagerDebts(m) + game.Finance(m).Bank.Balance;
        var players = club.Players.Sum(p => (long)(game.PlayerValueOf(p, club, PlayerValueKind.Price, new Random(0), m.Level) / 2));
        Assert.Equal(marks * FinanceRules.ThirdsPerMark + players, HighscoreRules.Inputs(game, 0).Wealth);
    }

    /// <summary>
    /// Regression (vorher rot: „Kaufpreis · 3“): Die Werbebewertung nimmt Ø(Wert / 5500) und die Stars (Wert / 7000) aus dem
    /// Spielerwert nach 0x118:0x70 (0x3F9E7, 0x3FA56).
    /// </summary>
    [Fact]
    public void SponsorRating_UsesPlayerValue()
    {
        var game = TestGames.Create();
        var m = game.Managers[0];
        var club = game.Clubs[m.ClubIndex];
        var finance = game.Finance(m);
        var tier = (int)finance.CostLeague;
        var p = game.CurrentParticipation(club.Index);
        foreach (var pl in club.Players) // Tore, Spiele und Strafen heben den Wert; der Kaufpreis des Remakes kennt sie nicht
            (pl.Goals, pl.Games, pl.Penalties5) = (50, 1000, 5);
        var values = club.Players.Select(pl => (long)game.PlayerValueOf(pl, club, PlayerValueKind.Price, new Random(0), m.Level)).ToList();

        var random = new Random(7);
        var s = SponsorRating.CompetitionScore(tier, p.Championship, p.Cup, p.CupRound, p.Europe, p.EuropeRound) + 12L * (11 - 6);
        s += SponsorRating.AverageValueScore(values);
        var stars = SponsorRating.StarCount(values, random);
        s += (long)SquadSetup.Between(random, 20, 50) * stars + 3L * SponsorRating.StadiumWorth(finance.Stadium);

        Assert.Equal(SponsorRating.Finish(s, tier), SponsorRating.Compute(game, m, new Random(7)));
    }

    /// <summary>
    /// [ORIGINAL] Ein Spieler mit Angebot zieht in der Werbebewertung in beiden Schleifen random(95, 100) – vor dem Zug
    /// random(35, 55) seiner Starprüfung. Das Gebot eines Computervereins im Remake (Transferliste) gilt als Angebot.
    /// </summary>
    [Fact]
    public void PlayerWithBid_DrawsRandomInValue()
    {
        var game = TestGames.Create();
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        var player = club.Players.First(p => p.Role == PlayerRole.Forward);
        game.TransferMarket.Offer(game, manager, player);
        for (var day = 0; day < 40 && game.TransferMarket.Bids.Count == 0; day++)
            game.TransferMarket.AdvanceDay(game, game.Random);
        var bid = Assert.Single(game.TransferMarket.Bids);
        Assert.True(bid.Buyer.Index <= 63); // Ligaverein → Zufall

        var a = new Random(9);
        var b = new Random(9);
        game.PlayerValueOf(player, club, PlayerValueKind.Price, a);
        Assert.NotEqual(b.Next(), a.Next()); // ein Zug verbraucht
        var other = club.Players.First(p => p != player);
        var c = new Random(9);
        game.PlayerValueOf(other, club, PlayerValueKind.Price, c);
        Assert.Equal(new Random(9).Next(), c.Next()); // ohne Angebot kein Zug
    }

    /// <summary>
    /// Mehrsaisonprobe (4 Saisonwechsel, passiver und aktiver Manager – aktiv: verlängert angebotene Verträge): Werte bleiben ganze 1.000 DM (3.000 Drittel), Gehälter
    /// ganze 100 DM, die Werbebewertung 0–100, und zwei gleiche Läufe ergeben dieselben Werte und Kontostände.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FourSeasonChanges_ValuesStayConsistent(bool active)
    {
        string Run()
        {
            var game = TestGames.Create(5);
            var log = new List<string>();
            for (var change = 0; change < 4; change++)
            {
                for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
                {
                    Assert.True(guard < 1000, "Saisonende nicht erreicht");
                    var m = game.Managers[0];
                    m.Money = Math.Max(m.Money, 1_000_000); // keine Entlassung
                    if (active)
                        TestSeason.KeepSquad(game, m);
                    game.Continue();
                }
                Assert.Equal(SeasonPhase.SeasonOver, game.Phase);
                game.StartNextSeason();
                foreach (var m in game.Managers)
                {
                    var club = game.Clubs[m.ClubIndex];
                    foreach (var p in club.Players)
                    {
                        var price = game.PlayerValueOf(p, club, PlayerValueKind.Price, new Random(1), m.Level);
                        var salary = game.PlayerValueOf(p, club, PlayerValueKind.Salary, new Random(1), m.Level);
                        Assert.True(price > 0 && price % 3000 == 0, $"{p.Name}: {price}");
                        Assert.True(salary > 0 && salary % 300 == 0, $"{p.Name}: {salary}");
                        log.Add($"{p.Id}:{price}:{salary}");
                    }
                    Assert.InRange(SponsorRating.Compute(game, m, new Random(1)), 0, 100);
                    Assert.True(HighscoreRules.Inputs(game, game.IndexOf(m)).Wealth != 0);
                    log.Add($"M{m.Money}");
                }
            }
            return string.Join(",", log);
        }

        Assert.Equal(Run(), Run());
    }
}
