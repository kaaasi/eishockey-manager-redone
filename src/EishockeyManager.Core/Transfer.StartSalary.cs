namespace EishockeyManager.Core;

public sealed partial class GameState
{
    /// <summary>
    /// Spielstart: Jeder Kaderplatz eines Managers bekommt als Gehalt den Spielerwert Art 1 (<see cref="Contracts.ValueSalary"/>).
    /// [ORIGINAL] Datei 0x29EB4–0x29EF3 (0x28165, Schleife über die Kaderplätze des Managers [0xA70E]): +0x2C = Wert(Preis),
    /// +0x28 = Wert(Gehalt). Computervereine haben im Original kein Gehaltsfeld (Datensatz 0x21 Byte); das Remake lässt ihr
    /// Gehalt aus <see cref="Contracts.BaseSalary"/>. Verbraucht keinen Zufall (Art 1 zieht nicht).
    /// [APPROX] Den Preis (+0x2C) führt das Remake nicht als Feld; er wird bei Bedarf neu berechnet.
    /// </summary>
    internal void ApplyStartSalaries()
    {
        foreach (var manager in Managers)
        {
            foreach (var p in Clubs[manager.ClubIndex].Players)
                p.MonthlySalary = Contracts.ValueSalary(this, manager, p);
        }
    }
}
