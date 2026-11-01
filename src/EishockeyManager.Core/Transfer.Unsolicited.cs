namespace EishockeyManager.Core;

/// <summary>
/// Unaufgeforderte Angebote für Spieler im Kader eines Managers (Kaderplatz +0x09 Bit 0x40, Verein in +0x16) und das
/// Angebotsfenster 0x491F9 (auch für Angebote zu Spielern auf dem Markt, Bit 0x80). Spur transfer-vertraege-komplett, Teil 3.
/// Beträge in Drittel-DM wie <see cref="TransferRules"/>; Fundstellen in <c>docs/transfer.md</c>.
/// </summary>
public static class UnsolicitedOfferRules
{
    /// <summary>Erster Verein der Europapokal-Gegner (Index &gt; 0x3F): höheres Angebot, Spieler verlässt die Liga.</summary>
    public const int FirstForeignClub = 0x40;

    /// <summary>
    /// Tageswurf je Kaderplatz. [ORIGINAL] Datei 0x079DE–0x07A54 (Schalter DGROUP 0x4860 = 0): w = Wert(Preis) / 10000;
    /// random(0, 200) &lt; w, dann random(0, 450) = 0. Der zweite Wurf fällt nur nach dem ersten.
    /// </summary>
    public static bool Roll(int valueThirds, Random random) =>
        TransferRules.Random16(random, 0, 200) < valueThirds / 10000 && TransferRules.Random16(random, 0, 450) == 0;

    /// <summary>
    /// Bietet ein Europapokal-Gegner? [ORIGINAL] Datei 0x07A61–0x07A7E: w &gt; 90 und random(0, 10) = 0 (der Wurf nur bei w &gt; 90).
    /// </summary>
    public static bool Foreign(int valueThirds, Random random) =>
        valueThirds / 10000 > 90 && TransferRules.Random16(random, 0, 10) == 0;

    /// <summary>
    /// Höhe des Angebots, gewürfelt beim Anklicken. [ORIGINAL] Datei 0x46E0E–0x46E84: V = Wert(Preis);
    /// V · random(87, 125) bzw. bei einem Verein &gt; 0x3F V · random(112, 155), / 30000 · 300 (also volle 100 DM).
    /// </summary>
    public static int AmountThirds(int valueThirds, bool foreignBuyer, Random random)
    {
        var factor = foreignBuyer ? TransferRules.Random16(random, 112, 155) : TransferRules.Random16(random, 87, 125);
        return unchecked(valueThirds * factor) / 30000 * 300;
    }

    /// <summary>
    /// Ablösesumme im Angebotsfenster bei einem Angebot für einen Spieler auf dem Markt. [ORIGINAL] Datei 0x49332–0x49371
    /// (Argument 4 = 0, Aufruf 0x473FF): Angebot − Angebot / 7 · Restlaufzeit (+0x0B); der Rest wird gutgeschrieben. Beim
    /// unaufgeforderten Angebot (Argument 1, Aufruf 0x46EA5) bleibt der Betrag, die Zeile zeigt „keine“.
    /// </summary>
    public static int MarketTransferFeeThirds(int offerThirds, int contractYears) =>
        offerThirds - offerThirds / 7 * contractYears;

    /// <summary>
    /// Bietender Verein. [ORIGINAL] Datei 0x059B8 (0x238:0x1838): Bereich 0 … 42, bei einem Europapokal-Gegner 64 … 149;
    /// Spielerstärke s = (Grundstärken +0x15 + +0x16 + +0x17) / 3; Schleife: Verein c = random(Bereich); Managerverein
    /// (+0x16 ≠ 0) → neuer Versuch ohne zweiten Wurf; Vereinsstärke = (Kondition + Technik von Abwehr und Sturm + 100) / 6
    /// (Felder 0x18–0x1B); nach mehr als 300 Versuchen angenommen, sonst wenn |Vereinsstärke − s| &lt; random(2, 10 · t) mit
    /// t = 1, ab Versuch 100 t = 2, ab 200 t = 3.
    /// [APPROX] Das Original übergibt die Nummer des Kaderplatzes statt der Poolnummer (0x07A81), misst also die Grundstärken
    /// eines fremden Pooleintrags; das Remake nimmt die Stärke des Spielers selbst (<see cref="Player.Strength"/>).
    /// Ohne passenden Verein im Bereich (synthetische Spielstände) liefert das Remake nach 10.000 Versuchen −1.
    /// </summary>
    public static int Bidder(int playerStrength, bool foreign, Func<int, int?> clubStrength, Random random)
    {
        var (lo, hi) = foreign ? (FirstForeignClub, 149) : (0, 42);
        var tolerance = 1;
        for (var attempt = 0; attempt < 10_000; )
        {
            var club = TransferRules.Random16(random, lo, hi);
            if (clubStrength(club) is { } strength
                && (attempt > 300 || Math.Abs(strength - playerStrength) < TransferRules.Random16(random, 2, tolerance * 10)))
                return club;
            attempt++;
            if (attempt == 100)
                tolerance = 2;
            if (attempt == 200)
                tolerance = 3;
        }
        return -1;
    }

    /// <summary>Vereinsstärke im Sinne von 0x059B8: (Kondition und Technik von Abwehr und Sturm + 100) / 6.</summary>
    public static int ClubStrength(Club club)
    {
        var v = club.Strength;
        var t = club.TeamValues;
        var sum = 4 * v + (t?.DefenseCondition ?? 0) + (t?.AttackCondition ?? 0) + (t?.DefenseTechnique ?? 0)
            + (t?.AttackTechnique ?? 0);
        return (sum + 100) / 6;
    }
}

/// <summary>Offenes Angebotsfenster eines unaufgeforderten Angebots (Bit 0x40 ist beim Öffnen schon gelöscht).</summary>
public sealed record UnsolicitedOffer(Manager Manager, Player Player, Club Buyer, long Amount);

public sealed partial class GameState
{
    /// <summary>
    /// Täglich im Zug jedes Managers (Tagesablauf 0x07169, Datei 0x07972–0x07AEF): bis zum Saisonende (Tageszähler &lt; 233)
    /// je Kaderplatz ohne Leihe (+0x0C) und ohne Rücktritt (+0x18 Bit 0x80) der Wurf <see cref="UnsolicitedOfferRules.Roll"/>;
    /// ohne offene Meldung des Spielers (Pool +0x1E) → +0x09 |= 0x40, Verein nach <see cref="UnsolicitedOfferRules.Bidder"/>
    /// und die Meldung „… ist an … interessiert.“ (0x14CF:0x39, Vorlage DGROUP 0x29B7).
    /// [APPROX] Zufall: eigene, je Saison und Tag abgeleitete Quelle wie <see cref="ContractTurnFactor"/>, damit Spielergebnisse
    /// und Markt ihre Folgen behalten; „offene Meldung“ = noch nicht angeklicktes Angebot; Rücktritt = Ankündigung
    /// <see cref="ContractNotice.Retiring"/>.
    /// </summary>
    internal void RollUnsolicitedOffers()
    {
        if (AfterSeasonEnd)
            return;
        var random = new GameRandom(unchecked(Season * 1_000_033 + CurrentDay * 6_151
            + Managers.Sum(m => m.ClubIndex * 43 + m.Head) + 29));
        foreach (var manager in ActiveManagers)
        {
            var club = Clubs[manager.ClubIndex];
            foreach (var p in club.Players.ToArray())
            {
                if (p.OnLoan || p.Notice == ContractNotice.Retiring)
                    continue;
                var value = PlayerValueOf(p, club, PlayerValueKind.Price, random, manager.Level);
                if (!UnsolicitedOfferRules.Roll(value, random) || p.UnsolicitedOffer is not null)
                    continue;
                var foreign = UnsolicitedOfferRules.Foreign(value, random);
                var buyer = UnsolicitedOfferRules.Bidder(p.Strength, foreign, BidderStrength, random);
                if (buyer < 0)
                    continue;
                p.UnsolicitedOffer = Clubs.First(c => c.Index == buyer);
                foreach (var m in Managers.Where(m => m.ClubIndex == club.Index && !m.Dismissed))
                    Notify(m, $"{p.UnsolicitedOffer.Name} ist an", $"{p.Name} interessiert.");
            }
        }
    }

    /// <summary>Stärke eines Vereins für <see cref="UnsolicitedOfferRules.Bidder"/>; null = Managerverein bzw. unbekannt.</summary>
    private int? BidderStrength(int index) =>
        Managers.Any(m => m.ClubIndex == index) || Clubs.FirstOrDefault(c => c.Index == index) is not { } club
            ? null
            : UnsolicitedOfferRules.ClubStrength(club);

    /// <summary>
    /// Anklicken eines Spielers mit Angebot im eigenen Kader der Transferliste. [ORIGINAL] Datei 0x46E04–0x46ED2: Höhe nach
    /// <see cref="UnsolicitedOfferRules.AmountThirds"/>, Bit 0x40 und die Meldung werden vor der Antwort gelöscht – das Angebot
    /// verfällt also auch bei „Behalten“. Liefert null ohne Angebot. Zufall der Verträge (<see cref="ContractRandom"/>).
    /// </summary>
    public UnsolicitedOffer? OpenUnsolicitedOffer(Manager manager, Player player)
    {
        var club = Clubs[manager.ClubIndex];
        if (player.UnsolicitedOffer is not { } buyer || !club.Players.Contains(player))
            return null;
        player.UnsolicitedOffer = null;
        var value = PlayerValueOf(player, club, PlayerValueKind.Price, ContractRandom, manager.Level);
        var amount = UnsolicitedOfferRules.AmountThirds(value, buyer.Index >= UnsolicitedOfferRules.FirstForeignClub,
            ContractRandom) / 3;
        return new UnsolicitedOffer(manager, player, buyer, amount);
    }

    /// <summary>
    /// „Verkaufen“ im Angebotsfenster. [ORIGINAL] Datei 0x46F3C–0x46FCF: Konto + Angebot (ohne Abzug), Kaderplatz entfernt
    /// (0x108:0x48), Pool +0x1D = Verein; Verein &lt; 0x40 nimmt den Spieler auf (0x18:0x175C), sonst Besitzer 6 (Pool).
    /// [APPROX] Ein voller Computerkader bzw. ein Europapokal-Gegner gibt den Spieler in den Pool der Vertragslosen.
    /// </summary>
    public TransferResult AcceptUnsolicitedOffer(UnsolicitedOffer offer)
    {
        var club = Clubs[offer.Manager.ClubIndex];
        var p = offer.Player;
        if (!Managers.Contains(offer.Manager) || !club.Players.Contains(p) || p.OnLoan)
            return TransferResult.Impossible;
        if (TransferMarket.ListingOf(p) is { } listing)
            TransferMarket.Withdraw(listing);
        Book(offer.Manager, FinanceCategory.Transfers, offer.Amount);
        club.Players.Remove(p);
        club.Lineup.Remove(p);
        p.UnsolicitedOffer = null;
        var buyer = offer.Buyer;
        if (buyer.Index < UnsolicitedOfferRules.FirstForeignClub && !TransferMarket.IsNationalTeam(buyer)
            && buyer.Players.Count < PlayerFactory.MaxNumber && Managers.All(m => m.ClubIndex != buyer.Index))
            PlayerFactory.Join(buyer, p, SquadSetup.StartContractYears, loan: false);
        else
            TransferMarket.Release(club, p);
        return TransferResult.Accepted;
    }
}
