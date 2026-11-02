using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur geld, Teil 5 (G4; A034, A312): Höchstwerte, Vorgaben und Buchung der Finanzbedienung nach em.exe
/// (docs/analyse/re-geld.md, „Datei 0x58409 – Betragsbalken“; Höchstwerte Datei 0x596C2–0x59A12, Buchung Datei
/// 0x5915F–0x5952D). Leihen beim Mitmanager im lokalen Spiel sofort ohne Anfrage (Entscheidung Nutzer E2).
/// </summary>
public class FinanceBedienungTests
{
    private static GameState TwoManagers()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Anna", Head = 0, ClubIndex = 2 });
        setup.Managers.Add(new ManagerSetup { Name = "Bernd", Head = 1, ClubIndex = 5 });
        return NewGame.Create(TestGames.CreateDatabase(), setup, 3);
    }

    /// <summary>E2 / Datei 0x59269–0x5932B: Geld fließt sofort, der Mitmanager bekommt keine Anfrage (A034).</summary>
    [Fact]
    public void BorrowFromManager_MovesMoneyAtOnce_WithoutRequest()
    {
        var game = TwoManagers();
        var (a, b) = (game.Managers[0], game.Managers[1]);
        a.Money = 100_000;
        b.Money = 500_000;

        Assert.Equal(300_000, game.BorrowFromManager(0, 1, 300_000));
        Assert.Equal(400_000, a.Money);
        Assert.Equal(200_000, b.Money);
        Assert.Equal(300_000, game.LoanAmount(lender: 1, borrower: 0));
        Assert.Empty(game.Inbox(b));

        // Grenze: sein Konto (200.000) bzw. 1 Mio. minus eigene Schuld (700.000)
        Assert.Equal(200_000, game.MaxBorrowFromManager(0, 1));
        Assert.Equal(200_000, game.BorrowFromManager(0, 1, 900_000));
        Assert.Equal(0, b.Money);
        Assert.Equal(0, game.BorrowFromManager(0, 1, 1_000));
    }

    /// <summary>
    /// Datei 0x59872–0x598DC: Links ist der Höchstwert mindestens die eigene Forderung – sie wird auch eingezogen, wenn das
    /// Konto des anderen leer ist; sein Konto darf dabei negativ werden.
    /// </summary>
    [Fact]
    public void BorrowFromManager_CollectsClaim_EvenFromEmptyAccount()
    {
        var game = TwoManagers();
        var (a, b) = (game.Managers[0], game.Managers[1]);
        a.Money = 1_000_000;
        Assert.Equal(400_000, game.PayToManager(from: 0, to: 1, 400_000));
        b.Money = -50_000;

        Assert.Equal(400_000, game.BorrowDefault(0, 1));
        Assert.Equal(400_000, game.MaxBorrowFromManager(0, 1));
        Assert.Equal(400_000, game.BorrowFromManager(0, 1, 400_000));
        Assert.Equal(-450_000, b.Money);
        Assert.Equal(0, game.ManagerLoanNet(0, 1));
        Assert.Empty(game.ManagerLoans);
    }

    /// <summary>Datei 0x5992B: Eine eigene Schuld beim anderen erhöht die Grenze beim Zahlen nicht (nur Abzug bei Feld &gt; 0).</summary>
    [Fact]
    public void PayToManager_OwnDebtDoesNotRaiseLimit()
    {
        var game = TwoManagers();
        var (a, b) = (game.Managers[0], game.Managers[1]);
        a.Money = 5_000_000;
        b.Money = 5_000_000;
        game.BorrowFromManager(0, 1, 800_000); // Anna schuldet Bernd 800.000

        Assert.Equal(1_000_000, game.MaxLendToManager(0, 1));
        Assert.Equal(800_000, game.LendDefault(0, 1));
        Assert.Equal(1_000_000, game.PayToManager(from: 0, to: 1, 3_000_000));
        Assert.Equal(200_000, game.ManagerLoanNet(0, 1));
        Assert.Equal(800_000, game.MaxLendToManager(0, 1));
        Assert.Null(game.LendDefault(0, 1));

        a.Money = 0;
        Assert.Equal(0, game.MaxLendToManager(0, 1));
    }

    /// <summary>Datei 0x5977C–0x598DC: je Buchung max(2 Mio. − Kredit, Sparbuch); Vorgabe Sparbuch bzw. Kredit (höchstens Konto).</summary>
    [Fact]
    public void Bank_LimitsAndDefaults()
    {
        var game = TestGames.Create();
        var m = game.ActiveManager;
        var bank = game.Finance(m).Bank;
        m.Money = 3_000_000;

        Assert.Null(game.WithdrawalDefault(m));
        Assert.Null(game.DepositDefault(m));
        Assert.Equal(3_000_000, GameState.MaxDeposit(m));
        game.DepositToBank(m, 2_500_000);
        Assert.Equal(2_500_000, game.WithdrawalDefault(m));
        Assert.Equal(2_500_000, game.MaxWithdrawal(m));
        game.DepositToBank(m, 500_000);
        Assert.Equal(0, GameState.MaxDeposit(m));

        bank.Savings = 400_000;
        Assert.Equal(FinanceRules.CreditLimit, game.MaxWithdrawal(m));
        Assert.Equal(FinanceRules.CreditLimit, game.WithdrawFromBank(m, 5_000_000));
        Assert.Equal(0, bank.Savings);
        Assert.Equal(1_600_000, bank.Credit);
        Assert.Equal(400_000, game.MaxWithdrawal(m));

        m.Money = 300_000;
        Assert.Equal(300_000, game.DepositDefault(m));
        m.Money = -1;
        Assert.Null(game.DepositDefault(m));
    }

    /// <summary>Mehrsaisonprobe (Geld): 4 Saisonwechsel, aktiver Manager leiht sofort beim passiven und zahlt zurück.</summary>
    [Fact]
    public void MultiSeason_FourChanges_BorrowAndRepayAtOnce()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Passiv", Head = 0, ClubIndex = 2, StartLeague = League.Bundesliga });
        setup.Managers.Add(new ManagerSetup { Name = "Aktiv", Head = 1, ClubIndex = 30, StartLeague = League.Oberliga });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 17);
        var passive = game.Managers[0];
        var active = game.Managers[1];
        var firstSeason = game.Season;

        for (var change = 0; change < 4; change++)
        {
            TestSeason.KeepSquad(game, passive);
            TestSeason.KeepSquad(game, active);
            passive.Money = Math.Max(passive.Money, 2_000_000);
            var before = (Active: active.Money, Passive: passive.Money);
            Assert.Equal(FinanceRules.ManagerLoanLimit, game.BorrowFromManager(1, 0, 5_000_000));
            Assert.Equal(before.Active + FinanceRules.ManagerLoanLimit, active.Money);
            Assert.Equal(before.Passive - FinanceRules.ManagerLoanLimit, passive.Money);
            Assert.All(game.Managers, m => Assert.DoesNotContain(game.Inbox(m), x => x.Kind == MessageKind.LoanRequest));

            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 2000, "Saisonende nicht erreicht");
                foreach (var manager in game.Managers)
                    manager.Money = Math.Max(manager.Money, 5_000_000); // keine Entlassung
                game.Continue();
                Assert.InRange(game.ManagerLoanNet(0, 1), 0, FinanceRules.ManagerLoanLimit);
            }

            // Integration Runde 11 (geld + saisonwechsel): Der aktive Manager (Verein 30, Oberliga) endet unter den letzten
            // zwei und bekommt am 29. April 400.000 DM gesetzt (S-K1, Datei 0x562D8). Die Probe prüft die Rückzahlung des
            // vollen Kredits, deshalb erst danach wieder genug Geld geben.
            active.Money = Math.Max(active.Money, 5_000_000);
            Assert.Equal(FinanceRules.ManagerLoanLimit, game.LendDefault(1, 0));
            Assert.Equal(FinanceRules.ManagerLoanLimit, game.PayToManager(from: 1, to: 0, game.LendDefault(1, 0)!.Value));
            Assert.Equal(0, game.ManagerLoanNet(1, 0));
            Assert.Empty(game.ManagerLoans);
            game.StartNextSeason();
        }
        Assert.Equal(firstSeason + 4, game.Season);
    }
}
