namespace EishockeyManager.Core;

/// <summary>
/// Transferregeln nach em.exe Overlay 0x118 (Spur <c>transfer-original</c>). Beträge in <b>Drittel-DM</b> wie im Original
/// (Kaderplatz +0x2C, Eingabe · 3 über DGROUP 0x79D/0x7A1, Datei 0x4727B); 32-Bit-Arithmetik mit Überlauf wie LXMUL/LDIV.
/// Beschreibung und Fundstellen: <c>docs/transfer.md</c>.
/// </summary>
public static class TransferRules
{
    /// <summary>
    /// random(a, b) des Originals mit 16-Bit-Argumenten. [ORIGINAL] Datei 0xC037 (0x99F:0x847): Grenzen als Wörter, bei b &lt; a
    /// getauscht, Ergebnis rand() mod (hi + 1 − lo) + lo (16 Bit). Aufrufer übergeben teils abgeschnittene 32-Bit-Werte
    /// (z. B. Wert / 10 bei 0x47379). Für gewöhnliche Grenzen wie <see cref="SquadSetup.Between"/>.
    /// </summary>
    public static int Random16(Random random, int a, int b)
    {
        short lo = unchecked((short)a), hi = unchecked((short)b);
        if (hi < lo)
            (lo, hi) = (hi, lo);
        var span = unchecked((short)(hi + 1 - lo));
        if (span > 0)
            return SquadSetup.Between(random, lo, lo + span - 1);
        // Spanne über 32767: idiv mit negativem Teiler, Rest hat das Vorzeichen des (positiven) Dividenden.
        return unchecked((short)(random.Next(32768) % span + lo));
    }

    /// <summary>
    /// Annahme eines Gebots für einen Spieler vom Transfermarkt. [ORIGINAL] em.exe Datei 0x4991D–0x49A3A (Ov 0x118 + 0x391D),
    /// gerufen beim Kauf bzw. bei der Leihe von 0x472C8 mit dem Listenpreis +0x2C (ohne Bit 31): bei Leihe Preis / 3;
    /// unter Preis · random(75, 85) / 100 abgelehnt; über Preis · random(110, 130) / 100 angenommen; dazwischen angenommen,
    /// wenn random(70, 120) &lt; Gebot · 100 / Preis. Die Züge fallen in dieser Reihenfolge und nur so weit wie nötig.
    /// </summary>
    /// <param name="priceThirds">Listenpreis in Drittel-DM (+0x2C).</param>
    /// <param name="offerThirds">Gebot in Drittel-DM.</param>
    public static bool Accepts(int priceThirds, int offerThirds, bool loan, Random random)
    {
        var price = priceThirds & 0x7FFF_FFFF;
        if (loan)
            price /= 3;
        if (unchecked(price * Random16(random, 75, 85)) / 100 > offerThirds)
            return false;
        if (unchecked(price * Random16(random, 110, 130)) / 100 < offerThirds)
            return true;
        var percent = unchecked(offerThirds * 100) / price;
        return Random16(random, 70, 120) < percent;
    }

    /// <summary>
    /// Gebot eines Managers für einen Spieler, den ein anderer Manager auf den Markt gesetzt hat. [ORIGINAL] em.exe Datei
    /// 0x47546–0x475DB: Wert 0x118:0x70 frisch berechnet, bei Leihe / 3; „Dieser Betrag ist nicht erlaubt (unfair) !“
    /// (DGROUP 0xD90/0xD94), wenn das Gebot unter 60 % oder über 140 % liegt (Grenzen selbst erlaubt).
    /// </summary>
    public static bool IsFair(int valueThirds, int offerThirds, bool loan)
    {
        if (loan)
            valueThirds /= 3;
        return unchecked(valueThirds * 60) / 100 <= offerThirds && unchecked(valueThirds * 140) / 100 >= offerThirds;
    }

    /// <summary>
    /// Tageswurf für ein Angebot auf einen eigenen Spieler auf dem Markt. [ORIGINAL] em.exe Datei 0x072DD–0x07355
    /// (Tagesablauf 0x07169, je Markt-Platz mit Besitzer = Manager): w = Wert / 10000 (16 Bit); Angebot, wenn
    /// random(0, 180) &lt; w und random(0, 2) ≠ 0 und für den Spieler keine Meldung offen ist (Pool +0x1E). Der Zweig über
    /// DGROUP 0x4860 (random(0, 3) = 0 → sofort) entfällt: 0x4860 wird von keinem Code beschrieben und ist in em.exe 0.
    /// </summary>
    /// <returns>true, wenn die beiden Würfe ein Angebot erlauben (die offene Meldung prüft der Aufrufer danach).</returns>
    public static bool BidRoll(int valueThirds, Random random)
    {
        var w = unchecked((short)(valueThirds / 10_000));
        if (Random16(random, 0, 180) >= w)
            return false;
        return Random16(random, 0, 2) != 0;
    }

    /// <summary>
    /// Ablöse, die ein Computerverein für einen eigenen Spieler auf dem Markt bietet. [ORIGINAL] em.exe Datei 0x47347–0x473DF
    /// (Anklicken eines Spielers mit Angebot, Kaderplatz +0x09 Bit 0x80): V = Wert 0x118:0x70 (mit Angebot, also
    /// · random(95, 100) / 100 bzw. · 1,6), dann V + random(V / 20, V / 10) − V / 15 (Grenzen als 16-Bit-Wörter), dann
    /// · random(85, 115) / 30000 · 300. Ergebnis in Drittel-DM, Vielfaches von 300 (100 DM).
    /// </summary>
    public static int BidAmountThirds(int valueThirds, Random random)
    {
        var v = valueThirds;
        var r = Random16(random, v / 20, v / 10);
        var amount = unchecked(v + (r - v / 15));
        amount = unchecked(amount * Random16(random, 85, 115)) / 30_000;
        return unchecked(amount * 300);
    }

    /// <summary>Listenpreis eines Spielers in Drittel-DM: Wert 0x118:0x70 ohne Angebot (Art 0). [ORIGINAL] Datei 0x29EB4.</summary>
    public static int PriceThirds(Player player) =>
        PlayerValue.Compute(PlayerValue.Inputs(player, null), PlayerValueKind.Price, NoRandom);

    private static int NoRandom(int a, int b) =>
        throw new InvalidOperationException("Der Preis ohne Angebot zieht keinen Zufall.");

    /// <summary>Drittel-DM eines DM-Betrags als 32-Bit-Wert des Originals.</summary>
    public static int Thirds(long dm) => unchecked((int)(dm * 3));
}
