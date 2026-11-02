namespace EishockeyManager.Core;

public sealed partial class GameState
{
    /// <summary>
    /// Spielstart: Jeder Kaderplatz eines Managers bekommt als Gehalt den Spielerwert Art 1 (<see cref="Contracts.ValueSalary"/>).
    /// [ORIGINAL] Datei 0x29EB4–0x29EF3 (0x28165, Schleife über die Kaderplätze des Managers [0xA70E]): +0x2C = Wert(Preis),
    /// +0x28 = Wert(Gehalt). Computervereine haben im Original kein Gehaltsfeld (Datensatz 0x21 Byte); das Remake lässt ihr
    /// Gehalt aus <see cref="Contracts.BaseSalary"/>. Verbraucht keinen Zufall (Art 1 zieht nicht).
    /// Der Preis steht seit Spur kader-transfer (Teil 2) in <see cref="Player.PurchasePrice"/>: Wert(Preis) / 3 DM; ohne Angebot
    /// eines Vereins (Spielbeginn) zieht auch Art 0 keinen Zufall (0x4A0F9).
    /// </summary>
    internal void ApplyStartSalaries()
    {
        foreach (var manager in Managers)
        {
            foreach (var p in Clubs[manager.ClubIndex].Players)
            {
                // [ORIGINAL] 0x29EB4 vor 0x29EE4: erst +0x2C = Wert(Preis), dann +0x28 = Wert(Gehalt).
                p.PurchasePrice = Contracts.ValuePrice(this, manager, p);
                p.MonthlySalary = Contracts.ValueSalary(this, manager, p);
            }
        }
    }
}
