namespace EishockeyManager.Core;

/// <summary>
/// Eigene Spieler auf der Transferliste außerhalb des Kaders (A380, Spur kader-start Teil 2c): Gehalt und Rückkehr am Saisonende.
/// </summary>
public sealed partial class TransferMarket
{
    /// <summary>
    /// Eigene Markt-Plätze des Vereins, deren Spieler nicht (mehr) im Kader steht (<see cref="Offer"/>); Altstände vor A380 mit
    /// dem Spieler noch im Kader zählen nicht doppelt.
    /// </summary>
    public IEnumerable<TransferListing> OwnListingsOutsideSquad(Club club) =>
        _listings.Where(l => l.Seller is not null && l.From == club && !l.IsFreeAgent && !l.FromPool
                             && !club.Players.Contains(l.Player));

    /// <summary>
    /// Spieler aller eigenen Markt-Plätze außerhalb des Kaders (A380). Sie stehen in keinem Kader und gehören trotzdem zum
    /// Spielstand (<see cref="GameState.AllPlayers"/>, Review R14KS-01).
    /// </summary>
    internal IEnumerable<Player> ListedOutsideSquads(GameState game) =>
        game.Clubs.SelectMany(OwnListingsOutsideSquad).Select(l => l.Player);

    /// <summary>
    /// Markt-Teil des Tageslaufs 0x07169 für einen Manager, ohne Listenwechsel (Review R14NT4-05). Aufrufer seit #678: der Hauptweg
    /// am Takttag je Manager und Schritt (c) der WM-Schleife für Manager 0 (<c>GameState.DailyRun.cs</c>). [ORIGINAL] Datei 0x0718F–0x07197: Die Schleife über alle Markt-Plätze (Satz 4,
    /// DGROUP 0x7623) mit dem Verfall der Gebote (Datei 0x07215–0x0726A) läuft nur für Manager 0 (DGROUP 0xA70E = 0); die zweite
    /// Schleife (ab Datei 0x0727C) würfelt Angebote nur für die Markt-Plätze mit Pool +0x1A = Manager (Datei 0x072A0–0x072DD).
    /// Den Listenwechsel 0x494EF rufen nur Datei 0x437D5 und 0x54C9F (Hauptweg 0x54E7C), nicht 0x07169; im Original bleibt die
    /// Liste in der WM gleich (FILE19 → FILE20).
    /// </summary>
    public void CatchUpManagerDay(GameState game, Manager manager)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(manager);
        UpdatedDay = game.PlayedMatchDays;
        AdvanceBids(game, game.Random, expire: game.Managers.Count > 0 && game.Managers[0] == manager, manager);
    }

    /// <summary>
    /// Saisonwechsel: Nicht verkaufte eigene Spieler kehren in den Kader zurück, nach Spielerart einsortiert, nicht eingesetzt
    /// (A091/A380, re-runde-14). [ORIGINAL] Saisonschleife 0x26F20 über alle Sätze (auch Satz 4 = Markt): Pool +0x1A (Besitzer)
    /// &lt; 4 und ≠ Satz → Kaderplatz anlegen (0x108:0x5C, Datei 0x42CB1), Platz kopieren (Datei 0x275D2), +0x0C = 0 und
    /// +0x18 &amp;= 0x7F (Datei 0x2764B, 0x27665), Markt-Platz frei (0x108:0x48, Datei 0x27628); Beleg FILE44 → FILE46
    /// (HOHENADL, Platz 11 vor dem ersten Stürmer). Das Remake gibt die Spieler im Saisonhaken der Manager vor dem Übertrag der
    /// Spiele und dem Löschen der Saisonzähler zurück (wie das Original: Löschen erst ab Datei 0x27A0C, Review R14KS-02); sie
    /// laufen dann wie alle Kaderspieler durch den Kaderwechsel (Altern, Laufzeit − 1, Verhandlung bei Laufzeit 0).
    /// [APPROX] Volle Kader (29) behalten den Spieler nicht: er wird vertragslos wie bei der Leihrückkehr ohne freie Nummer.
    /// </summary>
    internal void ReturnListedPlayers(GameState game)
    {
        foreach (var club in game.Clubs)
            foreach (var listing in OwnListingsOutsideSquad(club).ToList())
            {
                Unlist(listing);
                var p = listing.Player;
                if (club.Players.Count >= MaxSquad)
                {
                    p.ContractYears = 0;
                    p.MonthlySalary = 0;
                    p.Insured = false;
                    p.InsuranceLocked = false;
                    Release(club, p);
                    continue;
                }
                p.UnsolicitedOffer = null; // +0x09 &= 0x3F (Datei 0x274EB)
                PlayerFactory.InsertSlot(club, p, managerSquad: true);
                club.RenumberPlayers();
            }
    }
}

public static partial class FinanceRules
{
    /// <summary>
    /// Monatliche Gehälter eines Managervereins samt eigener Spieler auf der Transferliste. [ORIGINAL] em.exe 0x3E8DC–0x3E99F:
    /// Summe +0x28 (Gehalt) und +0x30 über die Kaderplätze des Managers und über die Markt-Plätze (Satz 4) mit Pool +0x1A =
    /// Manager – der Spieler auf der Liste kostet weiter sein Gehalt (A380).
    /// </summary>
    public static long MonthlyWages(GameState game, Club club) =>
        MonthlyWages(club) + game.TransferMarket.OwnListingsOutsideSquad(club).Sum(l => (long)l.Player.MonthlySalary);

    /// <summary>Monatliche Versicherungsprämien wie <see cref="MonthlyWages(GameState, Club)"/> (Kaderplätze und eigene Markt-Plätze).</summary>
    public static long MonthlyInsurance(GameState game, Club club) =>
        MonthlyInsurance(club) + game.TransferMarket.OwnListingsOutsideSquad(club).Sum(l => (long)Contracts.InsurancePremium(l.Player));
}
