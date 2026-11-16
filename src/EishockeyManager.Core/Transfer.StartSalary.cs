namespace EishockeyManager.Core;

public sealed partial class GameState
{
    /// <summary>
    /// Startwerte, Preis und Gehalt der Kaderplätze eines Managers (Spielstart und neuer Manager) aus dem gemeinsamen Strom
    /// <see cref="Random"/> (Spur zufallsstrom, Teil 3; N3 = B). [ORIGINAL] Managerschleife der Spielvorbereitung nach dem
    /// Platztausch, Datei 0x29DA4–0x29EF7 mit Basis DGROUP 0x4B42 [Start-Liga · 3 + Start-Stärke]
    /// (<see cref="SquadSetup.StartBase"/>): je Kaderplatz drei Würfe random(Basis − 2, Basis + 2) (Datei 0x29E00) – der erste ist
    /// die Kondition (Kaderplatz +0x10, Pool +0x15), der zweite die Technik (+0x11, Pool +0x16), der dritte wird verworfen –, dann
    /// Form random(40, 60) (Datei 0x29E18; +0x12, Pool +0x17), +0x2C = Wert(Preis) (Datei 0x29EB4) und +0x28 = Wert(Gehalt)
    /// (Datei 0x29EE4), die keinen Zufall ziehen (Art 1 bzw. Art 0 ohne Angebot, 0x4A0F9). Computervereine haben im Original kein
    /// Gehaltsfeld; ihr Gehalt bleibt <see cref="Contracts.BaseSalary"/>.
    /// </summary>
    internal void RollStartValues(Manager manager, int startBase) => RollStartValues(manager, startBase, Random);

    internal void RollStartValues(Manager manager, int startBase, Random random)
    {
        foreach (var p in Clubs[manager.ClubIndex].Players)
        {
            p.Condition = Player.Clamp(TransferRules.Random16(random, startBase - 2, startBase + 2));
            p.Technique = Player.Clamp(TransferRules.Random16(random, startBase - 2, startBase + 2));
            _ = TransferRules.Random16(random, startBase - 2, startBase + 2);
            p.Form = TransferRules.Random16(random, 40, 60);
            // [ORIGINAL] 0x29EB4 vor 0x29EE4: erst +0x2C = Wert(Preis), dann +0x28 = Wert(Gehalt).
            p.PurchasePrice = Contracts.ValuePrice(this, manager, p);
            p.MonthlySalary = Contracts.ValueSalary(this, manager, p);
        }
    }

    /// <summary>
    /// Kaderplätze der Manager ohne Steuerbyte nachziehen (Import eines Originalspielstands, Modus „nur Weltkugel“, Aufbauten ohne
    /// <see cref="NewGame.Create"/>): Tendenz und Steuerbyte (A134, <see cref="Player.RollTrend"/>), +0x19
    /// (<see cref="HiddenSkills.Roll"/>) aus dem gemeinsamen Strom, je Manager und Kaderplatz in Platzfolge; Preis und Gehalt für
    /// alle Plätze (ohne Zufall). Der Spielstart würfelt beides schon an der Stelle des Originals (Poolaufbau Datei 0x5B1CD–0x5B245,
    /// <see cref="TransferMarket.ReserveStartPool"/>; Startwerte <see cref="RollStartValues(Manager, int)"/>) – dort ist das
    /// Steuerbyte gesetzt (Zähler 1–13, nie 0), und hier fällt kein Wurf.
    /// </summary>
    internal void ApplyStartSalaries()
    {
        foreach (var manager in Managers)
        {
            foreach (var p in Clubs[manager.ClubIndex].Players)
            {
                if (p.TrendControl == 0)
                {
                    // A134: [ORIGINAL] 0x42CB1 ruft nach der Energie 0x4907A (Datei 0x42E6B): Tendenz +0x0E = random(35, 65),
                    // Steuerbyte +0x14; A676: danach +0x19 = random(3, 12) · 16 + random(3, 12).
                    p.RollTrend(Random);
                    HiddenSkills.Roll(p, Random);
                }
                // [ORIGINAL] 0x29EB4 vor 0x29EE4: erst +0x2C = Wert(Preis), dann +0x28 = Wert(Gehalt).
                p.PurchasePrice = Contracts.ValuePrice(this, manager, p);
                p.MonthlySalary = Contracts.ValueSalary(this, manager, p);
            }
        }
    }
}
