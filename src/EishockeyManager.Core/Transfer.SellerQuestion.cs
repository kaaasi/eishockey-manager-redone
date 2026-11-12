namespace EishockeyManager.Core;

/// <summary>
/// Offene Frage an einen verkaufenden Manager (Spur kader-transfer, Teil 4, A258). [ORIGINAL] Datei 0x47655–0x47743: Text
/// DGROUP 0xD98 „Nehmen Sie das Angebot (“ + Betrag + 0x14D5 „ DM)“, Zeile 2 0xD9C „für “ + Name + 0xDA0 „ an ?“, Knöpfe
/// 0x12D5 „Na gut.“ / 0x12D9 „Nein !“, Meldung/Frage 0x4C246 (Stub 0x120:0x7A) an den Besitzer des Spielers (Pool +0x1A).
/// </summary>
/// <param name="Listing">Markt-Eintrag des Verkäufers.</param>
/// <param name="Buyer">Bietender Manager.</param>
/// <param name="Seller">Gefragter Manager.</param>
/// <param name="Offer">Gebot in DM (Ablöse bzw. Leihgebühr).</param>
/// <param name="Loan">Leihe statt Kauf.</param>
public sealed record SellerQuestion(TransferListing Listing, Manager Buyer, Manager Seller, long Offer, bool Loan);

public sealed partial class TransferMarket
{
    /// <summary>
    /// Antwort des verkaufenden Managers auf <paramref name="question"/>. [ORIGINAL] „Na gut.“ (Datei 0x4774A): angenommen, weiter
    /// wie beim Computermarkt (Kaderplatz 0x477E7, Verhandlung, Geld). „Nein !“: Meldung DGROUP 0x1209 und Sperre des Markt-Platzes
    /// für den Bieter (0x47AC8–0x47ADD). [APPROX] Zwischen Frage und Antwort prüft das Remake Eintrag, Kader und Geld erneut (im
    /// Original ändert sich dazwischen nichts).
    /// </summary>
    public TransferResult AnswerSeller(GameState game, SellerQuestion question, bool accept, out TransferPurchase? purchase,
        out SponsorSubsidyOffer? subsidy)
    {
        purchase = null;
        subsidy = null;
        var (listing, buyer) = (question.Listing, question.Buyer);
        if (!game.Managers.Contains(buyer) || !_listings.Contains(listing) || listing.Seller != question.Seller
            || !IsCurrentListing(game, listing))
            return TransferResult.Impossible;
        if (!accept)
        {
            Lock(game, listing, buyer);
            return TransferResult.Rejected;
        }
        if (IsSquadFull(game, buyer, listing.From))
            return TransferResult.SquadFull;
        if (question.Offer > buyer.Money)
            return TransferResult.NotEnoughMoney;
        return Close(game, buyer, listing, question.Offer, question.Loan, out purchase, out subsidy);
    }
}
