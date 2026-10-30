using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens;

/// <summary>
/// Die sechs Lampen der Infoleiste im Hauptmenü (Spur pixel-verein, Befund PV-03). Sie sind Warnlampen des Managers
/// am Zug, keine Anzeige, welcher Manager schon bestätigt hat.
/// </summary>
/// <remarks>
/// [ORIGINAL] em.exe Datei 0x42996–0x42CA6 (Manager am Zug DGROUP 0xA70E, Managerstruktur 0x1AF Byte): alle sechs
/// Lampen beginnen grün und werden rot, wenn
/// <list type="number">
/// <item>Lampe 0: kein Torhüter aufgestellt ist oder eine der Reihen 1–3 nicht genau fünf eingesetzte Spieler hat
///   (Kaderplätze 0x5DC3 + 0x618 · Manager + 0x34 · Platz, Felder 0x0F, 0x0A, 0x15; Rollenwert 0x18 = 0 → Torhüter);</item>
/// <item>Lampe 1: ein eigener Spieler – im Kader oder auf dem Transfermarkt – Feld 0x1E ≠ 0 hat;</item>
/// <item>Lampe 2: der Kontostand (0x97EA) negativ ist und noch Geld auf dem Sparbuch (Bank-Saldo 0x9806 &gt; 0) oder
///   Aktien im Depot liegen;</item>
/// <item>Lampe 3: Trainingskosten je Woche (0x97EE) anfallen;</item>
/// <item>Lampe 4: eine der elf Werbeflächen (DGROUP 0xC15B + 0x6E · Manager + 10 · Fläche) frei ist – die beiden
///   Eisflächen (9, 10) zählen nur mit Überdachung „HALLE“ (0x9771 = 2) bzw. Kühlanlage „MEGAFROST“ (0x9775 = 2);</item>
/// <item>Lampe 5: das Depot weniger wert ist, als es gekostet hat (Σ Stück · Kurs &lt; Σ Kaufpreis).</item>
/// </list>
/// Beim Bundestrainer (Manager 5) sind die Lampen 2–5 immer grün (Datei 0x42C4B). Zeichnen: Quelle kb\3 (196 | 219 + 6 ·
/// grün) 6 × 6, Ziel (226 | 33 + 7 · Lampe), Datei 0x42C62–0x42CA6. Belege im Video: Spielbeginn nur Lampe 4 rot
/// (folge01_00600, duell_s2a_00135), nach Vergabe aller Werbeflächen alle grün (duell_s2a_01203, folge03_00018),
/// Eisbären am 2. Spieltag Lampen 3 und 4 rot (eisbaeren_f01_01430).
/// </remarks>
public static class MenuLamps
{
    public const int Count = 6;
    public const int X = 226;

    /// <summary>Obere Kante der Lampe. [ORIGINAL] Datei 0x42C6C: 0x21 + 7 · Lampe.</summary>
    public static int Y(int lamp) => 33 + 7 * lamp;

    /// <summary>Ist die Lampe rot?</summary>
    public static bool[] Red(GameState game)
    {
        var red = new bool[Count];
        if (game.NationalCoachMenu)
        {
            // [APPROX] Vor der Ernennung gibt es keinen WM-Kader: Lampe 0 rot wie ein leerer Kader im Original.
            red[0] = game.NationalCoach is not { } coach || !Complete(coach.Formation);
            return red; // Lampe 1 siehe unten; 2–5 beim Bundestrainer immer grün
        }

        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        var finance = manager.FinanceData;
        red[0] = !Complete(club.Lineup);
        // [APPROX] Lampe 1: Bedeutung von Spielerfeld 0x1E nicht geklärt (nicht Verletzung oder Sperre, die stehen im
        // Kaderplatz) – bleibt grün.
        red[2] = manager.Money < 0 && finance is { } f2 && (f2.Bank.Balance > 0 || f2.Holdings.Any(h => h.Count != 0));
        red[3] = manager.Training.RawCostPerTrainingDay != 0;
        red[4] = finance is { } f4 && FreeAdSpace(f4);
        red[5] = finance is { } f5 && DepotValue(game, f5) < f5.Holdings.Sum(h => h.PurchaseCost);
        return red;
    }

    /// <summary>Torhüter aufgestellt und die Reihen 1–3 mit je fünf Spielern besetzt (Reihe 4 zählt nicht).</summary>
    public static bool Complete(Lineup lineup)
    {
        if (lineup.Goalkeeper is null)
            return false;
        for (var block = 0; block < Lineup.AutoBlocks; block++)
            if (lineup.Block(block).Count() != Lineup.ForwardsPerBlock + Lineup.DefendersPerBlock)
                return false;
        return true;
    }

    /// <summary>
    /// Freie Werbefläche: sieben Banden, Trikot, Fernsehen und jede freigegebene Eistafel.
    /// [ORIGINAL] em.exe Datei 0x42C09–0x42C1E (Fläche 9 nur mit Überdachung HALLE) und 0x42C20–0x42C35 (Fläche 10 nur
    /// mit Kühlanlage MEGAFROST) – jede Tafel mit eigener Freigabe und eigenem Vertrag (Review-Befund R6-R01).
    /// </summary>
    private static bool FreeAdSpace(ManagerFinance finance)
    {
        var ads = finance.Sponsoring;
        if (ads.FreeBoards().Any())
            return true;
        if (ads.ContractAt(new AdSlot(AdSlotKind.Trikot)) is null || ads.ContractAt(new AdSlot(AdSlotKind.Fernsehen)) is null)
            return true;
        var stadium = finance.Stadium;
        if (stadium.Level(StadiumFeature.Ueberdachung) == 2 && ads.ContractAt(new AdSlot(AdSlotKind.Eisflaeche)) is null)
            return true;
        return stadium.Level(StadiumFeature.Kuehlanlage) == 2 && ads.ContractAt(new AdSlot(AdSlotKind.Eisflaeche2)) is null;
    }

    /// <summary>Depotwert zum Kurs; ohne Aktien 0, ohne die Börse anzulegen (Bildschirme ändern keinen Zustand).</summary>
    private static long DepotValue(GameState game, ManagerFinance finance)
    {
        long value = 0;
        if (finance.Holdings.All(h => h.Count == 0))
            return 0;
        for (var i = 0; i < finance.Holdings.Length; i++)
            value += (long)finance.Holdings[i].Count * game.Stocks.Stocks[i].Price;
        return value;
    }
}
