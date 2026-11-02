namespace EishockeyManager.Core;

/// <summary>
/// Transferfolgen beim Computerverein (Spur <c>kader-transfer</c>, Teil 3; Inventur F005, F006). Beträge wie
/// <see cref="TransferRules"/> in Drittel-DM gerechnet; Fundstellen in <c>docs/transfer.md</c>.
/// </summary>
public static class ComputerClubTransferRules
{
    /// <summary>
    /// Teamwerte des Käufers nach einem Verkauf. [ORIGINAL] em.exe Datei 0x058DC (Basis 0x238:0x175C), Argumente Verein und
    /// Spielerstärke s = (Kaderplatz +0x10 + +0x11 + +0x12) / 3 (Datei 0x46F8F–0x46FAE bzw. 0x4745F–0x4747E):
    /// <list type="number">
    /// <item>Mittel a = (Kondition + Technik eines Teils) / 2 (Schleife zweimal über dieselben Felder, Summe / 4).</item>
    /// <item>d = (s − a + 10) / 3 (ganzzahlig, gegen null gerundet).</item>
    /// <item>Teil k = random(0, 1) (0 = Abwehr, 1 = Sturm), dann d mit 0x120:0x52 auf 1 … 10 begrenzt.</item>
    /// <item>Kondition 0x18 + k += random(d − 1, d + 1), dann Technik 0x1A + k += random(d − 1, d + 1); ohne Grenze
    ///   (Byte-Addition).</item>
    /// </list>
    /// [APPROX] Den Teil für das Mittel a liest das Original über das Register <c>si</c>, das die Funktion vorher nicht
    /// setzt: es ist die Mauszeile des Transferbildschirms (Datei 0x46AA1) und zeigt damit auf beliebige Bytes der
    /// Vereinstabelle. Das Remake nimmt das Mittel beider Teile ((Kondition + Technik von Abwehr und Sturm) / 4); es zieht
    /// dafür keinen Zufall, die Reihenfolge der Züge bleibt die des Originals.
    /// </summary>
    /// <param name="values">Teamwerte des Käufers (Abweichungen vom Datenbankwert).</param>
    /// <param name="clubStrength">Datenbankwert des Käufers (<see cref="Club.Strength"/>).</param>
    /// <param name="playerStrength">Stärke des Spielers (<see cref="Player.Strength"/>).</param>
    public static ClubTeamValues StrengthenBuyer(ClubTeamValues values, int clubStrength, int playerStrength, Random random)
    {
        ArgumentNullException.ThrowIfNull(random);
        var sum = 4 * clubStrength + values.DefenseCondition + values.AttackCondition
            + values.DefenseTechnique + values.AttackTechnique;
        var mean = sum / 4;
        var d = (playerStrength - mean + 10) / 3;
        var part = TransferRules.Random16(random, 0, 1);
        d = Math.Clamp(d, 1, 10);
        var condition = TransferRules.Random16(random, d - 1, d + 1);
        var technique = TransferRules.Random16(random, d - 1, d + 1);
        return part == 0
            ? values with
            {
                DefenseCondition = values.DefenseCondition + condition,
                DefenseTechnique = values.DefenseTechnique + technique,
            }
            : values with
            {
                AttackCondition = values.AttackCondition + condition,
                AttackTechnique = values.AttackTechnique + technique,
            };
    }

    /// <summary>
    /// Sponsorzuschuss nach einem Kauf vom Computermarkt. [ORIGINAL] em.exe Datei 0x063BE (Basis 0x18:0x223E), gerufen
    /// Datei 0x47A34 mit dem gezahlten Betrag P (Gebot, nach dem Abzug vom Konto): nur bei random(0, 6) = 0, dann
    /// Z = P · random(25, 65) / 30000 · 300 (volle 100 DM); Frage „Nehmen Sie einen Sponsor- / zuschuß von / Z DM an ?“
    /// mit „Aber immer!“ / „Lieber nicht.“ (0x4C246), bei Ja Konto + Z. Liefert den Zuschuss in DM oder null.
    /// </summary>
    /// <param name="paid">Gezahlter Betrag in DM.</param>
    public static long? SponsorSubsidy(long paid, Random random)
    {
        ArgumentNullException.ThrowIfNull(random);
        if (TransferRules.Random16(random, 0, 6) != 0)
            return null;
        var factor = TransferRules.Random16(random, 25, 65);
        return unchecked(TransferRules.Thirds(paid) * factor) / 30000 * 300 / 3;
    }
}

/// <summary>Angebot eines Sponsorzuschusses nach einem Kauf vom Computermarkt (<see cref="ComputerClubTransferRules.SponsorSubsidy"/>).</summary>
public sealed record SponsorSubsidyOffer(Manager Manager, long Amount);

public sealed partial class TransferMarket
{
    /// <summary>
    /// Nach dem Abzug des Kaufpreises: Zuschuss würfeln, wenn der Spieler vom Computermarkt kam. [ORIGINAL] Datei
    /// 0x47A00–0x47A34: nicht bei eigenem Spieler und nicht beim Kauf von einem anderen Manager (Rückfrage 0x4774E), auch
    /// bei einer Leihe. [APPROX] Vertragslose wie Marktspieler (im Original stehen sie in derselben Liste). Das Angebot ist
    /// kein Spielstandsfeld: es geht an den Aufrufer, die Oberfläche fragt sofort (<see cref="GameState.AcceptSponsorSubsidy"/>);
    /// Aufrufer ohne Frage verwerfen es wie „Lieber nicht.“.
    /// </summary>
    private static SponsorSubsidyOffer? RollSponsorSubsidy(GameState game, TransferListing listing, Manager manager, long paid)
    {
        if (listing.Seller is not null)
            return null;
        return ComputerClubTransferRules.SponsorSubsidy(paid, game.Random) is { } amount
            ? new SponsorSubsidyOffer(manager, amount)
            : null;
    }
}

public sealed partial class GameState
{
    /// <summary>
    /// „Aber immer!“ auf die Frage nach dem Sponsorzuschuss. [ORIGINAL] Datei 0x064AD: Konto + Z.
    /// [APPROX] Gebucht als Transfereinnahme (das Original schreibt den Betrag ohne Buchungsaufruf aufs Konto; wie die
    /// Ablöse am Saisonende, <c>docs/transfer.md</c>).
    /// </summary>
    public void AcceptSponsorSubsidy(SponsorSubsidyOffer offer)
    {
        ArgumentNullException.ThrowIfNull(offer);
        if (Managers.Contains(offer.Manager) && offer.Amount > 0)
            Book(offer.Manager, FinanceCategory.Transfers, offer.Amount);
    }

    /// <summary>
    /// Verkauf eines Managerspielers an einen Verein: Teamwerte des Käufers steigen
    /// (<see cref="ComputerClubTransferRules.StrengthenBuyer"/>). [ORIGINAL] Aufrufe Datei 0x46FB3 (unaufgefordertes Angebot,
    /// nur Verein &lt; 0x40) und 0x47483 (Gebot auf einen eigenen Spieler am Markt, ohne Prüfung).
    /// [APPROX] Ohne gezogene Teamwerte (vor dem ersten Spiel, <see cref="MatchEngine.LoadTeamValues"/>) entfällt die Wirkung.
    /// </summary>
    internal void StrengthenBuyer(Club buyer, Player player)
    {
        if (buyer.TeamValues is not { } values)
            return;
        buyer.TeamValues = ComputerClubTransferRules.StrengthenBuyer(values, buyer.Strength, player.Strength, Random);
    }
}
