namespace EishockeyManager.Core;

/// <summary>Werte der Kontoleiste im Transfermarkt (Kontostand, Schulden, Anlagen, Saldo) in DM.</summary>
public readonly record struct TransferAccountBar(long Money, long Debts, long Assets)
{
    /// <summary>Saldo = Kontostand + Anlagen − Schulden (Feld k = 3).</summary>
    public long Balance => Money + Assets - Debts;
}

public sealed partial class GameState
{
    /// <summary>
    /// Kontoleiste der Transferliste. [ORIGINAL] Datei 0x463CC (Aufrufer nur 0x465F1, <c>docs/analyse/re-geld.md</c>):
    /// Kontostand; Schulden = 0x57768 (Modus 0) Σ der eigenen negativen Darlehen bei Managern und Bank
    /// (<see cref="TotalDebts"/>); Anlagen = Σ Stückzahl · Kurs (<see cref="DepotValue"/>) + Σ der positiven Salden gegenüber
    /// jedem anderen Manager und der Bank (Forderungen samt Sparbuch); Saldo = Konto + Anlagen − Schulden.
    /// [APPROX] Saldo je Partner wie bei <see cref="TotalDebts"/>: Kredite zwischen zwei Managern verrechnet, bei der Bank
    /// Sparbuch minus Kredit.
    /// </summary>
    public TransferAccountBar TransferAccount(Manager manager)
    {
        var index = IndexOf(manager);
        long claims = 0;
        for (var partner = 0; partner < Managers.Count; partner++)
            if (partner != index)
                claims += Math.Max(0, LoanAmount(index, partner) - LoanAmount(partner, index));
        claims += Math.Max(0, Finance(manager).Bank.Balance);
        return new TransferAccountBar(manager.Money, TotalDebts(manager), DepotValue(manager) + claims);
    }
}
