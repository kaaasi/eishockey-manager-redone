namespace EishockeyManager.Core;

/// <summary>
/// Angenommener Kauf vor der Vertragsverhandlung (Datei 0x4781B–0x4786A). Der neue Kaderplatz trägt schon das Gebot als Preis
/// (+0x2C); Laufzeit und Gehalt, mit denen der Dialog beginnt, stehen in <see cref="Years"/> und <see cref="Salary"/>.
/// </summary>
/// <remarks>
/// [ORIGINAL] Laufzeit und Gehalt des neuen Kaderplatzes aus 0x42CB1 (Aufruf Datei 0x477E7): <see cref="Contracts.PurchaseYears"/>
/// und Wert(Gehalt) im Verein des Käufers (<see cref="Contracts.ValueSalary"/>, 0x42F11). [APPROX] Der Zustand lebt nur im
/// Dialog (kein Spielstandsfeld); <see cref="TransferMarket.Sign"/> prüft vor dem Abschluss erneut.
/// </remarks>
public sealed record TransferPurchase(TransferListing Listing, Manager Buyer, long Offer, int Years, int Salary)
{
    public Player Player => Listing.Player;
}

public sealed partial class TransferMarket
{
    /// <summary>
    /// „Mach' ich!“ im Verhandlungsfenster nach einem angenommenen Kauf. Steht der Vertrag, zahlt der Käufer, der Spieler
    /// wechselt mit dem verhandelten Vertrag und das Ergebnis ist <see cref="NegotiationResult.Accepted"/>; sonst findet der
    /// Transfer nicht statt (Meldung des Dialogs, dann „Der Transfer findet nicht statt.“). [ORIGINAL] Datei 0x4786A ruft den
    /// Dialog 0x4A1DB mit dem neuen Kaderplatz; die Sperre nach einer gescheiterten Verhandlung zieht das Original
    /// (random(10, 18), 0x4AD3E), sie verfällt mit dem freigegebenen Platz (0x47A51) – das Remake zieht sie ebenso und
    /// verwirft sie. Die Verfügbarkeit (Leihe, Ruhestand, Sperre) prüft der Dialog am neuen Kaderplatz nicht: [APPROX] der
    /// Platz ist frisch angelegt (0x108:0x5C).
    /// </summary>
    public NegotiationResult Sign(GameState game, TransferPurchase purchase, int years, int salary, bool insured,
        bool demandFilled) => Sign(game, purchase, years, salary, insured, demandFilled, out _);

    /// <summary>
    /// Wie <see cref="Sign(GameState, TransferPurchase, int, int, bool, bool)"/>; nach einem Kauf vom Computermarkt liefert
    /// <paramref name="subsidy"/> den gewürfelten Sponsorzuschuss (Datei 0x47A34, nach dem Abzug des Preises).
    /// </summary>
    public NegotiationResult Sign(GameState game, TransferPurchase purchase, int years, int salary, bool insured,
        bool demandFilled, out SponsorSubsidyOffer? subsidy)
    {
        subsidy = null;
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(purchase);
        var manager = purchase.Buyer;
        var listing = purchase.Listing;
        if (!game.Managers.Contains(manager) || !_listings.Contains(listing) || !IsCurrentListing(game, listing)
            || purchase.Offer > manager.Money || game.Clubs[manager.ClubIndex].Players.Count >= MaxSquad)
            return NegotiationResult.Unfair;
        var (result, newYears, newSalary) = Contracts.NegotiateTerms(game, manager, listing.Player, purchase.Years,
            purchase.Salary, years, salary, demandFilled);
        if (result != NegotiationResult.Accepted)
        {
            _ = ContractRules.LockDays(game.ContractRandom);
            return result;
        }
        subsidy = Complete(game, listing, manager, purchase.Offer, loan: false);
        var p = listing.Player;
        p.ContractYears = newYears;
        Contracts.ApplyInsurance(game, manager, p, insured, accepted: true, lockOnFailure: false);
        p.MonthlySalary = newSalary;
        return result;
    }

    /// <summary>„Abbruch“ im Verhandlungsfenster nach einem Kauf: nichts ändert sich (Ergebnis 0, ohne Sperre, Datei 0x47874).</summary>
    public static string[] NotTakingPlace => Message(TransferResult.Impossible);
}
