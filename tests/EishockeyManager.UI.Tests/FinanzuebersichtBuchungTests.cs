using EishockeyManager.Tests.Support;
using EishockeyManager.Core;
using EishockeyManager.UI.Screens.ManagerArea;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur finanzuebersicht (Runde 21, N5 = C): Sechs Posten schreibt das Original ohne Buchungsaufruf aufs Konto, das Remake
/// bucht sie über <see cref="GameState.Book"/> (A192, A207, A511, A513, A703, A716). Die Original-Finanzübersicht (rechte Spalte,
/// em.exe Datei 0x57ECF, Sprungtabelle 0x581A6) zeigt nur Konto (DGROUP 0x97EA), Darlehen (0x97F6 + 4 · i), Monatszinsen
/// (0x57C64) und Depot (0xD214), keine Kategorie – der Posten erscheint dort also nur über den Kontostand. Geprüft wird je
/// Posten: Nach der Buchung ändert sich im Bildschirm Finanzen nur die Zahl unter „Kontostand:“, im Finanzbuch steht der
/// Betrag in der Kategorie des Remakes.
/// </summary>
public class FinanzuebersichtBuchungTests
{
    /// <summary>Zeilen der Kontostand-Zahl: Grundlinie 117 (Datei 0x57ED6: Start 0x6B, Wertzeile + 10), Mini-Schrift.</summary>
    private const int BalanceTop = 110;
    private const int BalanceBottom = 119;

    /// <summary>Rechte Spalte (Text bei x = 249, Werte rechtsbündig bei x = 246; Hintergrund ab x = 246, Datei 0x57EE9).</summary>
    private const int SummaryLeft = 240;

    public static TheoryData<string> Postings => new() { "A192", "A207", "A511", "A513", "A703", "A716" };

    [GameDataTheory]
    [MemberData(nameof(Postings))]
    public void Posting_ChangesOnlyBalanceInSummary_AndCategoryInBook(string id)
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var me = game.ActiveManager;
        me.Money = 1_000_000; // genug für Bau und Talentsuche, Betrag bleibt positiv (Farbe 12 vor und nach der Buchung)
        d.OpenTile("Finanzen");
        Assert.IsType<FinanceScreen>(d.Host.Current);
        d.Frame();
        var before = (byte[])d.Surface.Pixels.Clone();
        var book = game.Finance(me).Book;
        var money = me.Money;

        var (category, amount) = Post(game, me, id);

        Assert.NotEqual(0, amount);
        Assert.Equal(money + amount, me.Money);
        Assert.Equal(amount, book.Current(category));

        d.Frame();
        Assert.IsType<FinanceScreen>(d.Host.Current);
        var after = d.Surface.Pixels;
        var changed = 0;
        for (var i = 0; i < after.Length; i++)
        {
            if (after[i] == before[i])
                continue;
            int x = i % Surface.Width, y = i / Surface.Width;
            Assert.True(x >= SummaryLeft && y >= BalanceTop && y <= BalanceBottom,
                $"{id}: Pixel ({x} | {y}) außerhalb der Kontostand-Zahl geändert");
            changed++;
        }
        Assert.True(changed > 0, $"{id}: Kontostand-Zahl unverändert");
    }

    /// <summary>Löst den Posten über den Aufruf des Remakes aus; gibt Kategorie und gebuchten Betrag zurück.</summary>
    private static (FinanceCategory Category, long Amount) Post(GameState game, Manager me, string id)
    {
        var club = game.Clubs[me.ClubIndex];
        switch (id)
        {
            case "A192":
                // Core/StadiumStatistics.cs (SettleMatchGate, intern): Book(Strafen, −ShortHandedPenalty). Hier nur über Book (Kategorie
                // und Betrag stehen im Test, Review #901 P3-1, #902); den echten Pfad und die Kategorie prüft
                // HeimspielStatistikTests.ShortHanded_AtHome_PaysPenalty_… (Book.Current(Strafen)) in Core.Tests.
                game.Book(me, FinanceCategory.Strafen, -GameState.ShortHandedPenalty);
                return (FinanceCategory.Strafen, -GameState.ShortHandedPenalty);
            case "A207":
            {
                var p = club.Players[0];
                p.InsurancePremium = Math.Max(p.InsurancePremium, 1_000);
                return (FinanceCategory.Versicherung, game.PayInsurance(me, p, careerEnd: false));
            }
            case "A511":
            {
                var level = game.Finance(me).Stadium.Level(StadiumFeature.Gastronomie);
                var quote = game.QuoteConstruction(me, StadiumProject.Feature, StadiumFeature.Gastronomie, level + 1);
                Assert.NotNull(quote);
                Assert.True(game.AnswerConstruction(me, quote, accept: true));
                return (FinanceCategory.Bau, -quote.DiscountedCost);
            }
            case "A513":
            {
                var price = game.TalentSearchPrice(me);
                Assert.Equal(TalentSearchStart.Started, game.StartTalentSearch(me));
                return (FinanceCategory.Talentsuche, -price);
            }
            case "A703":
                game.AcceptSponsorSubsidy(new SponsorSubsidyOffer(me, 40_000));
                return (FinanceCategory.Transfers, 40_000);
            case "A716":
                // Core/Transfer.SeasonEnd.cs (Leave, privat): Book(Transfers, Ablöse). Hier nur über Book (Review #901 P3-1, #902); den
                // echten Pfad und die Kategorie prüfen TransferVertraegeKomplettTests.SeasonEnd_PlayerLeaves_WithCompensation,
                // TagestaktSommerpauseReviewTests.File44_… und KaderStartTeil4Tests.ManagerLoan_ExpiringLoanSlot_… (Mutation
                // Transfers → Unterhalt: 6 Core-Tests rot).
                game.Book(me, FinanceCategory.Transfers, 75_000);
                return (FinanceCategory.Transfers, 75_000);
            default:
                throw new ArgumentOutOfRangeException(nameof(id), id, null);
        }
    }
}
