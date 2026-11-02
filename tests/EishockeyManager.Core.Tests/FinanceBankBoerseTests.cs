using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur geld, Teil 4 (G3, A033): Kreditrahmen der Bank, Grenze zwischen Managern und Börsengebühr nach em.exe
/// (docs/analyse/re-geld.md, „Datei 0x58409 – Betragsbalken“, Höchstwerte Datei 0x596C2–0x59A12, Buchung Datei
/// 0x5915F–0x5952D).
/// </summary>
public class FinanceBankBoerseTests
{
    private static GameState TwoManagers()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Anna", Head = 0, ClubIndex = 2 });
        setup.Managers.Add(new ManagerSetup { Name = "Bernd", Head = 1, ClubIndex = 5 });
        return NewGame.Create(TestGames.CreateDatabase(), setup, 3);
    }

    /// <summary>Datei 0x596C2 ff.: links höchstens 6.000.000 Drittel-DM minus Schuld = 2 Mio. DM Kredit.</summary>
    [Fact]
    public void Bank_CreditUpToTwoMillion()
    {
        var game = TestGames.Create();
        var m = game.ActiveManager;
        var bank = game.Finance(m).Bank;
        var money = m.Money;

        Assert.Equal(2_000_000, game.MaxWithdrawal(m));
        Assert.Equal(2_000_000, game.WithdrawFromBank(m, 5_000_000));
        Assert.Equal(2_000_000, bank.Credit);
        Assert.Equal(money + 2_000_000, m.Money);
        Assert.Equal(0, game.WithdrawFromBank(m, 1_000));
        Assert.Equal(0, game.MaxWithdrawal(m));
    }

    /// <summary>
    /// Rechts (verleihen) höchstens 3.000.000 Drittel-DM minus was der andere mir schuldet, und nicht mehr als das
    /// eigene Konto: 1 Mio. DM Forderung je Richtung; eigene Schulden werden zuerst getilgt.
    /// </summary>
    [Fact]
    public void Managers_LendAtMostOneMillionNet()
    {
        var game = TwoManagers();
        var (a, b) = (game.Managers[0], game.Managers[1]);
        a.Money = 5_000_000;
        b.Money = 5_000_000;

        Assert.Equal(1_000_000, game.MaxLendToManager(0, 1));
        Assert.Equal(1_000_000, game.PayToManager(from: 0, to: 1, 2_000_000));
        Assert.Equal(1_000_000, game.LoanAmount(lender: 0, borrower: 1));
        Assert.Equal(0, game.PayToManager(from: 0, to: 1, 10_000));

        // Bernd schuldet Anna 1 Mio.: seine Schuld erhöht die Grenze nicht (Abzug nur bei Darlehensfeld > 0, Datei 0x59924;
        // Teil 5 berichtigt den Sollwert 2 Mio. aus Teil 4) – er zahlt höchstens 1 Mio., das tilgt die Schuld.
        Assert.Equal(1_000_000, game.MaxLendToManager(1, 0));
        Assert.Equal(1_000_000, game.PayToManager(from: 1, to: 0, 3_000_000));
        Assert.Equal(0, game.LoanAmount(lender: 0, borrower: 1));
        Assert.Equal(0, game.LoanAmount(lender: 1, borrower: 0));
    }

    /// <summary>Links (leihen) höchstens 1 Mio. DM minus eigene Schuld beim anderen, und nur bis zu seinem Konto.</summary>
    [Fact]
    public void Managers_BorrowAtMostOneMillionAndLendersAccount()
    {
        var game = TwoManagers();
        var (a, b) = (game.Managers[0], game.Managers[1]);
        b.Money = 600_000;
        Assert.Equal(600_000, game.MaxBorrowFromManager(0, 1));
        b.Money = 5_000_000;
        Assert.Equal(1_000_000, game.MaxBorrowFromManager(0, 1));

        Assert.Equal(1_000_000, game.LendBetweenManagers(lender: 1, borrower: 0, 1_500_000));
        Assert.Equal(0, game.MaxBorrowFromManager(0, 1));
        Assert.Equal(0, game.LendBetweenManagers(lender: 1, borrower: 0, 10_000));

        // Teil 5 (Datei 0x59872–0x598DC): leeres Konto des anderen sperrt nur neues Leihen; die eigene Forderung (Bernd an
        // Anna 1 Mio.) bleibt einziehbar – Sollwert 0 aus Teil 4 deshalb auf 1 Mio. geändert.
        b.Money = 0;
        a.Money = 0;
        Assert.Equal(1_000_000, game.MaxBorrowFromManager(1, 0));
        Assert.Equal(0, game.MaxBorrowFromManager(0, 1));
    }

    /// <summary>Datei 0x5915F ff.: Kosten w + w · 15 / 1000, der Kaufpreis enthält die Gebühr (10 Stück zu 100 DM = 1.015 DM).</summary>
    [Fact]
    public void Stocks_BuyCostsOneAndAHalfPercentFee()
    {
        var game = TestGames.Create();
        var m = game.ActiveManager;
        game.Stocks.Stocks[0].Price = 100;
        m.Money = 100_000;

        Assert.Equal(10, game.BuyStock(m, 0, 10));
        Assert.Equal(100_000 - 1_015, m.Money);
        Assert.Equal(1_015, game.Finance(m).Holdings[0].PurchaseCost);
    }

    /// <summary>Höchstwert links = Konto / Kurs ohne Gebühr: wer genau 1.000 DM hat, kauft 10 Stück und steht bei −15 DM.</summary>
    [Fact]
    public void Stocks_MaxCountIgnoresFee()
    {
        var game = TestGames.Create();
        var m = game.ActiveManager;
        game.Stocks.Stocks[0].Price = 100;
        m.Money = 1_000;

        Assert.Equal(10, game.BuyStock(m, 0, 50));
        Assert.Equal(-15, m.Money);
    }

    /// <summary>
    /// Verkauf: Konto += w − w · 15 / 1000; Kaufpreis = Kaufpreis · ⌊(Zahl − v) · 100 / Zahl⌋ / 100. Gewinn/Verlust
    /// (Erlös minus abgegangener Kaufpreis) ins Finanzbuch „Aktien“.
    /// </summary>
    [Fact]
    public void Stocks_SellWithFeeAndWholePercentRemainder()
    {
        var game = TestGames.Create();
        var m = game.ActiveManager;
        var holding = game.Finance(m).Holdings[0];
        game.Stocks.Stocks[0].Price = 300;
        holding.Count = 3;
        holding.PurchaseCost = 1_000;
        m.Money = 0;

        Assert.Equal(1, game.SellStock(m, 0, 1));
        Assert.Equal(300 - 4, m.Money);                 // 300 · 15 / 1000 = 4 (ganzzahlig)
        Assert.Equal(2, holding.Count);
        Assert.Equal(660, holding.PurchaseCost);        // ⌊2 · 100 / 3⌋ = 66 %
        Assert.Equal(296 - 340, game.Finance(m).Book.Current(FinanceCategory.Aktien));

        Assert.Equal(2, game.SellStock(m, 0, 2));
        Assert.Equal(0, holding.Count);
        Assert.Equal(0, holding.PurchaseCost);
    }

    /// <summary>
    /// Mehrsaisonprobe (Geld) über 4 Saisonwechsel, passiver und aktiver Manager: der aktive holt jede Saison den
    /// vollen Bankkredit, verleiht über die Grenze hinaus an den passiven und handelt Aktien mit Gebühr. Kredit und
    /// Forderung bleiben in ihren Grenzen, das Finanzbuch „Aktien“ folgt der Verkaufsregel.
    /// </summary>
    [Fact]
    public void MultiSeason_FourChanges_LimitsAndFeeHold()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Passiv", Head = 0, ClubIndex = 2, StartLeague = League.Bundesliga });
        setup.Managers.Add(new ManagerSetup { Name = "Aktiv", Head = 1, ClubIndex = 30, StartLeague = League.Oberliga });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 11);
        var passive = game.Managers[0];
        var active = game.Managers[1];
        var bank = game.Finance(active).Bank;
        var firstSeason = game.Season;

        for (var change = 0; change < 4; change++)
        {
            TestSeason.KeepSquad(game, passive);
            TestSeason.KeepSquad(game, active);
            game.WithdrawFromBank(active, 10_000_000);
            Assert.Equal(FinanceRules.CreditLimit, bank.Credit);
            Assert.Equal(0, game.MaxWithdrawal(active));
            active.Money = Math.Max(active.Money, 2_000_000);
            game.PayToManager(from: 1, to: 0, 5_000_000);
            Assert.Equal(FinanceRules.ManagerLoanLimit, game.ManagerLoanNet(1, 0));

            var before = active.Money;
            long paid = 0;
            for (var i = 0; i < 4; i++)
            {
                var value = 50L * game.Stocks.Stocks[i].Price;
                Assert.Equal(50, game.BuyStock(active, i, 50));
                paid += value + value * 15 / 1000;
            }
            Assert.Equal(before - paid, active.Money);
            var trading = game.Finance(active).Book.Current(FinanceCategory.Aktien);

            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 2000, "Saisonende nicht erreicht");
                foreach (var manager in game.Managers)
                    manager.Money = Math.Max(manager.Money, 5_000_000); // keine Entlassung
                game.Continue();
                Assert.InRange(bank.Credit, 0, FinanceRules.CreditLimit);
                Assert.InRange(game.ManagerLoanNet(1, 0), -FinanceRules.ManagerLoanLimit, FinanceRules.ManagerLoanLimit);
            }

            long expected = 0;
            var holdings = game.Finance(active).Holdings;
            for (var i = 0; i < 4; i++)
            {
                var h = holdings[i];
                var count = h.Count / 2;
                var value = (long)count * game.Stocks.Stocks[i].Price;
                var rest = h.PurchaseCost * ((h.Count - count) * 100L / h.Count) / 100;
                expected += value - value * 15 / 1000 - (h.PurchaseCost - rest);
                Assert.Equal(count, game.SellStock(active, i, count));
                Assert.Equal(rest, h.PurchaseCost);
            }
            Assert.Equal(trading + expected, game.Finance(active).Book.Current(FinanceCategory.Aktien));
            Assert.All(game.Finance(passive).Holdings, h => Assert.Equal(0, h.Count));

            // Kredit zurück, damit die nächste Saison wieder den vollen Rahmen prüft; Forderung tilgt der passive.
            active.Money = Math.Max(active.Money, 5_000_000);
            game.DepositToBank(active, bank.Credit);
            passive.Money = Math.Max(passive.Money, 2_000_000);
            game.PayToManager(from: 0, to: 1, game.LoanAmount(lender: 1, borrower: 0));
            Assert.Equal(0, game.ManagerLoanNet(1, 0));
            game.StartNextSeason();
        }
        Assert.Equal(firstSeason + 4, game.Season);
    }
}
