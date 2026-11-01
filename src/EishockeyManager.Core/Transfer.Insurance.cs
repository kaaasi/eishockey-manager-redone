namespace EishockeyManager.Core;

/// <summary>
/// Spur transfer-vertraege-komplett, Teil 2: Leistung der Spielerversicherung (em.exe 0x07089 und Saisonende 0x2772B).
/// Prämie und Abschluss: <see cref="Player.InsurancePremium"/>, <see cref="Contracts.InsuranceOffer"/>,
/// <see cref="Contracts.ApplyInsurance"/>.
/// </summary>
public sealed partial class GameState
{
    /// <summary>
    /// Zahlung der Versicherung für einen verletzten Spieler eines Managers und Meldung. [ORIGINAL] Datei 0x7089–0x7168:
    /// Betrag = +0x30 · 4 (0x709B, Verschiebung um 2), Meldung „NAME ist verletzt.“ / „Ein Fall für die Versicherung: Sie zahlt
    /// Ihnen … DM“ (DGROUP 0x134D–0x1359); bei Laufbahnende (+0x0D = 0xFF, Saisonende 0x2772B–0x277DE) · 60 und „NAME ist
    /// invalide.“ (0x135D). Ohne Versicherung (+0x30 = 0) keine Zahlung (0x752D, 0x38D46). [APPROX] Das Original schreibt den
    /// Betrag ohne Buchungsaufruf aufs Konto (0x7147); hier als Einnahme der Kategorie Versicherung gebucht.
    /// Anschluss: Verletzungen gibt es im Remake noch nicht (spielberechnung-komplett, Teil 3: Spiel 0x38D6A, Tagesablauf 0x7542).
    /// </summary>
    /// <returns>gezahlter Betrag in DM, 0 ohne Versicherung.</returns>
    public long PayInsurance(Manager manager, Player p, bool careerEnd)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(p);
        if (p.InsurancePremium <= 0)
            return 0;
        var amount = (long)p.InsurancePremium * (careerEnd ? Contracts.CareerEndPayoutFactor : Contracts.InjuryPayoutFactor);
        Book(manager, FinanceCategory.Versicherung, amount);
        Notify(manager, InsuranceMessage(p, amount, careerEnd));
        return amount;
    }

    /// <summary>Meldung der Versicherungsleistung (DGROUP 0x134D–0x135D).</summary>
    public static string[] InsuranceMessage(Player p, long amount, bool careerEnd)
    {
        ArgumentNullException.ThrowIfNull(p);
        return [p.Name + (careerEnd ? " ist invalide." : " ist verletzt."),
            "Ein Fall für die Versicherung: Sie zahlt Ihnen " + FormatMoney(amount)];
    }
}
