using System.Buffers.Binary;
using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur kader-rest (Runde 17), Teil 3 – A196: „Schulden gesamt“ (0x57768) und die Kontoleiste der Transferliste (0x463CC) lesen je
/// Partner ein gespiegeltes Darlehensfeld (Managersatz +0x19B + 4 · i). Probe: Export in das Originalformat, dort nach dem Code
/// des Originals gerechnet, gleich den Werten des Remakes.
/// </summary>
public class KaderRestGeldTests
{
    private static GameState TwoManagers()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", Head = 0, ClubIndex = 5, StartLeague = League.Bundesliga });
        setup.Managers.Add(new ManagerSetup { Name = "B", Head = 1, ClubIndex = 6, StartLeague = League.Bundesliga });
        return NewGame.Create(TestGames.CreateDatabase(), setup, 3);
    }

    private static int Field(byte[] records, int record, int partner) =>
        BinaryPrimitives.ReadInt32LittleEndian(records.AsSpan(record * OriginalSaveGame.ManagerRecordLength + 0x19B + 4 * partner));

    /// <summary>[ORIGINAL] Datei 0x5777F–0x57834 (Modus 0, Partner 0x63): Σ −Feld(ich, i) &lt; 0, i ≠ ich, i &lt; Managerzahl oder i = 4.</summary>
    private static long OriginalDebtsThirds(byte[] records, int me, int managers)
    {
        long sum = 0;
        for (var i = 0; i < 5; i++)
            if (i != me && (i < managers || i == 4) && Field(records, me, i) < 0)
                sum -= Field(records, me, i);
        return sum;
    }

    /// <summary>[ORIGINAL] Datei 0x464AA–0x46500: Σ −Feld(i, ich) &lt; 0, i = 0–4 ohne ich.</summary>
    private static long OriginalClaimsThirds(byte[] records, int me)
    {
        long sum = 0;
        for (var i = 0; i < 5; i++)
            if (i != me && Field(records, i, me) < 0)
                sum -= Field(records, i, me);
        return sum;
    }

    /// <summary>
    /// Kredite in beide Richtungen (Altstand oder Werkzeug) und Bankkredit: Das exportierte Feld ist der Saldo je Partner, und
    /// 0x57768 bzw. 0x463CC darauf ergeben <see cref="GameState.TotalDebts"/> und die Anlagen der Kontoleiste. Vorher schrieb der
    /// Export je Kredit; der zweite überschrieb den ersten (Feld A→B = +20.000 statt −30.000 DM).
    /// </summary>
    [Fact]
    public void Export_NetFieldPerPartner_OriginalSumsMatchRemake()
    {
        var game = TwoManagers();
        var (a, b) = (game.Managers[0], game.Managers[1]);
        game.Finance(a).Bank.Credit = 100_000;
        game.Finance(b).Bank.Savings = 40_000;
        game.ManagerLoans.Add(new ManagerLoan { Lender = 1, Borrower = 0, Amount = 50_000 });
        game.ManagerLoans.Add(new ManagerLoan { Lender = 0, Borrower = 1, Amount = 20_000 });

        var records = OriginalSaveGame.Read(OriginalExport.Export(game, null)).Block("965B");
        Assert.Equal(-90_000, Field(records, 0, 1)); // A schuldet B netto 30.000 DM
        Assert.Equal(90_000, Field(records, 1, 0));
        Assert.Equal(-300_000, Field(records, 0, 4));
        Assert.Equal(300_000, Field(records, 4, 0));
        Assert.Equal(-120_000, Field(records, 4, 1));

        for (var m = 0; m < 2; m++)
        {
            var manager = game.Managers[m];
            Assert.Equal(game.TotalDebts(manager), OriginalDebtsThirds(records, m, 2) / 3);
            Assert.Equal(game.TransferAccount(manager).Assets - game.DepotValue(manager), OriginalClaimsThirds(records, m) / 3);
        }
        Assert.Equal(130_000, game.TotalDebts(a));
        Assert.Equal(0, game.TotalDebts(b));
    }

    /// <summary>
    /// [ORIGINAL] Buchung Datei 0x591E2–0x5932B: eigenes Feld ± v, das des anderen ∓ v – je Paar nur ein Saldo. Die Buchungen
    /// des Remakes (<see cref="GameState.PayToManager"/>, <see cref="GameState.BorrowFromManager"/>) halten je Paar höchstens
    /// eine Richtung; Schulden und Anlagen folgen dem Saldo.
    /// </summary>
    [Fact]
    public void Bookings_KeepOneDirectionPerPair()
    {
        var game = TwoManagers();
        var (a, b) = (game.Managers[0], game.Managers[1]);
        a.Money = b.Money = 500_000;
        Assert.Equal(80_000, game.BorrowFromManager(0, 1, 80_000));
        Assert.Equal(30_000, game.PayToManager(0, 1, 30_000));
        Assert.Equal(100_000, game.PayToManager(0, 1, 100_000));
        var pair = Assert.Single(game.ManagerLoans);
        Assert.Equal((0, 1, 50_000L), (pair.Lender, pair.Borrower, pair.Amount));
        Assert.Equal(0, game.TotalDebts(a));
        Assert.Equal(50_000, game.TotalDebts(b));
        Assert.Equal(50_000, game.TransferAccount(a).Assets - game.DepotValue(a));
    }
}
