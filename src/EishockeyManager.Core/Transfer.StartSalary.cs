namespace EishockeyManager.Core;

public sealed partial class GameState
{
    /// <summary>
    /// Spielstart: Jeder Kaderplatz eines Managers bekommt als Gehalt den Spielerwert Art 1 (<see cref="Contracts.ValueSalary"/>).
    /// [ORIGINAL] Datei 0x29EB4–0x29EF3 (0x28165, Schleife über die Kaderplätze des Managers [0xA70E]): +0x2C = Wert(Preis),
    /// +0x28 = Wert(Gehalt). Computervereine haben im Original kein Gehaltsfeld (Datensatz 0x21 Byte); das Remake lässt ihr
    /// Gehalt aus <see cref="Contracts.BaseSalary"/>. Verbraucht keinen Zufall (Art 1 zieht nicht).
    /// Der Preis steht seit Spur kader-transfer (Teil 2) in <see cref="Player.PurchasePrice"/>: Wert(Preis) / 3 DM; ohne Angebot
    /// eines Vereins (Spielbeginn) zieht auch Art 0 keinen Zufall (0x4A0F9). Davor Tendenz und Steuerbyte je Platz (A134,
    /// <see cref="Player.RollTrend"/> aus <see cref="TrendRandom"/>).
    /// </summary>
    internal void ApplyStartSalaries()
    {
        foreach (var manager in Managers)
        {
            foreach (var p in Clubs[manager.ClubIndex].Players)
            {
                // A134 (Spur kader, Teil 2): [ORIGINAL] Der Spielstart legt jeden Platz über 0x42CB1 an (Datei 0x29996), das nach der
                // Energie 0x4907A ruft (Datei 0x42E6B): Tendenz +0x0E = random(35, 65), Steuerbyte +0x14 – vor Wert(Preis) und
                // Wert(Gehalt) bei Datei 0x29EB4, die die Tendenz lesen (0x4A0A7). Beleg FILE01/FILE27/FILE28/FILE29 (sofort
                // gesichert): alle Plätze mit Wert 35–65, Zähler 1–13, Schritt 1–3. [APPROX] Eigene Quelle TrendRandom wie beim Markt.
                p.RollTrend(TrendRandom);
                // [ORIGINAL] 0x29EB4 vor 0x29EE4: erst +0x2C = Wert(Preis), dann +0x28 = Wert(Gehalt).
                p.PurchasePrice = Contracts.ValuePrice(this, manager, p);
                p.MonthlySalary = Contracts.ValueSalary(this, manager, p);
            }
        }
    }
}
