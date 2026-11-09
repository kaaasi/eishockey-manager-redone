namespace EishockeyManager.Core;

/// <summary>
/// Vertragsverhandlung nach em.exe Overlay 0x118 (Spur <c>transfer-original-vertrag</c>): Dialog 0x4A1DB (Stubeintrag
/// 0x118:0x75), Annahme 0x49A3B, Sperre Kaderplatz +0x18. Beträge in <b>Drittel-DM</b> wie im Original (Kaderplatz +0x28);
/// 32-Bit-Arithmetik wie LXMUL/LDIV (Werte bleiben hier weit unter dem Überlauf). Fundstellen: <c>docs/transfer.md</c>.
/// </summary>
public static class ContractRules
{
    /// <summary>
    /// Längste Laufzeit. [ORIGINAL] Datei 0x4AB93: mehr als 4 Jahre → „SOOOO lange / verpflichtet / sich kein /
    /// Eishockey-Spieler !“ (DGROUP 0x1471–0x147F), die Verhandlung scheitert.
    /// </summary>
    public const int MaxYears = 4;

    /// <summary>Spieltage einer Liga (DGROUP 0x7B6–0x7B8 = 44, 44, 44).</summary>
    public const int SeasonMatchDays = 44;

    /// <summary>
    /// Tageszähler DGROUP 0x3B7 nach dem Saisonende: 0x26F20 setzt ihn auf 233 (Datei 0x26F55). Alle Vergleiche der
    /// Verhandlung lauten „Zähler &gt; 232“ (0x4A963, 0x49CB3) bzw. „Zähler &lt; 233“ (0x4A70A).
    /// </summary>
    public const int AfterSeasonDay = 233;

    /// <summary>
    /// Restanteil der Saison in Prozent: 100 − 100 · nächster Spieltag / 44. [ORIGINAL] Datei 0x4A851–0x4A8A6 und
    /// 0x49B97–0x49BD8: nächster Spieltag DGROUP 0x7A9 + Liga des Managers (Manager +0xD7), Spieltage 0x7B6 + Liga.
    /// </summary>
    public static int SeasonRemaining(int nextMatchDay, int seasonMatchDays = SeasonMatchDays) =>
        100 - 100 * nextMatchDay / seasonMatchDays;

    /// <summary>
    /// Gehaltsforderung, die der Dialog nach der Eingabe der Laufzeit ins Feld „Gehalt / Monat“ schreibt.
    /// [ORIGINAL] Datei 0x4A81B–0x4A9F2: S = Wert(Gehalt) · F / 100 mit F = DGROUP 0xE1A4 (random(85, 115) zu Beginn jedes
    /// Managerzugs, Datei 0x44E3F); U = (Jahre − 1) · 100 + Rest; W = (U / 6 + 122) · 8 / 10 (LXLSH 3, Datei 0x4A8F8);
    /// S = S · (W + 4) / 100; nach dem Saisonende zusätzlich S = S · (152 − Alter) / 100 (Datei 0x4A974). Vor dem Saisonende
    /// ist die Forderung nie kleiner als das bisherige Gehalt (0x4A9B3); nach dem Saisonende steht in +0x28 an dieser Stelle
    /// schon die Forderung selbst (0x4A9A1), die Untergrenze wirkt nicht (R8W3I-02, #270).
    /// <para>Das Alter liest das Original aus Kaderplatz 3 des Managers statt aus dem verhandelten Spieler (Register DI
    /// steht nach der Zeichenschleife 0x4A3D3–0x4A407 auf 3, Datei 0x4A93F); der Aufrufer übergibt dieses Alter.</para>
    /// <para>Nach dem Saisonende schreibt das Original die Forderung und die Laufzeit sofort in den Kaderplatz (0x4A9A1);
    /// <see cref="Contracts.Negotiate"/> übernimmt sie mit „Mach' ich!“ (Ablehnung und Abbruch stellen beides zurück, 0x4AD3E→0x4AD54).</para>
    /// </summary>
    /// <param name="salaryValueThirds">Spielerwert 0x118:0x70 Art 1 (Gehalt) in Drittel-DM.</param>
    /// <param name="turnFactor">DGROUP 0xE1A4 (85–115).</param>
    /// <param name="remaining"><see cref="SeasonRemaining"/>.</param>
    /// <param name="years">eingegebene Laufzeit.</param>
    /// <param name="afterSeason">Tageszähler &gt; 232.</param>
    /// <param name="ageForFactor">Alter für den Faktor nach dem Saisonende (Kaderplatz 3, siehe oben).</param>
    /// <param name="currentSalaryThirds">bisheriges Gehalt (Kaderplatz +0x28).</param>
    public static int DemandThirds(int salaryValueThirds, int turnFactor, int remaining, int years, bool afterSeason,
        int ageForFactor, int currentSalaryThirds)
    {
        var s = unchecked(salaryValueThirds * turnFactor) / 100;
        var u = (years - 1) * 100 + remaining;
        var w = (u / 6 + 122) * 8 / 10;
        s = unchecked(s * (w + 4)) / 100;
        if (afterSeason)
            s = unchecked(s * (27 - ageForFactor + 125)) / 100;
        return afterSeason ? s : Math.Max(s, currentSalaryThirds);
    }

    /// <summary>
    /// Nimmt der Spieler das Angebot an? [ORIGINAL] em.exe Datei 0x49A3B–0x49D35 (gerufen von 0x4ACDA mit Laufzeit und Gehalt
    /// in Drittel-DM; 0 = unverändert):
    /// <list type="number">
    /// <item>Laufzeit gleich: mehr Gehalt → ja, weniger → nein, gleich → weiter.</item>
    /// <item>Laufzeit kürzer: über 120 % des bisherigen Gehalts → ja; bis 105 % → nein (der Dialog lässt kürzere Laufzeiten
    /// nicht zu, 0x4AC75).</item>
    /// <item>D = Wert(Gehalt) · random(85, 115) / 100; U = (Jahre − 1) · 100 + Rest; W = (U / 6 + 110) · 8 / 10;
    /// D = D · random(W − 6, W + 4) / 100; nach dem Saisonende D = D · random(132 − Alter, 152 − Alter) / 100.</item>
    /// <item>D ≥ 100.000 Drittel-DM → nein (0x49D09); sonst ja, wenn das Gehalt über D liegt.</item>
    /// </list>
    /// Den Wert ruft das Original vor dem ersten Zug (0x49C0F); er zieht hier keinen Zufall (Art 1).
    /// </summary>
    public static bool Accepts(int currentYears, int currentSalaryThirds, int years, int salaryThirds, int salaryValueThirds,
        int remaining, bool afterSeason, int age, Random random)
    {
        if (years == 0)
            years = currentYears;
        if (salaryThirds == 0)
            salaryThirds = currentSalaryThirds;
        if (years == currentYears)
        {
            if (currentSalaryThirds < salaryThirds)
                return true;
            if (currentSalaryThirds > salaryThirds)
                return false;
        }
        if (currentYears > years)
        {
            if ((long)currentSalaryThirds * 120 / 100 < salaryThirds)
                return true;
            if ((long)currentSalaryThirds * 105 / 100 >= salaryThirds)
                return false;
        }

        var u = (years - 1) * 100 + remaining;
        var d = unchecked(salaryValueThirds * TransferRules.Random16(random, 85, 115)) / 100;
        var w = (u / 6 + 110) * 8 / 10;
        d = unchecked(d * TransferRules.Random16(random, w - 6, w + 4)) / 100;
        if (afterSeason)
            d = unchecked(d * TransferRules.Random16(random, 27 - age + 105, 27 - age + 125)) / 100;
        if (d >= 100_000)
            return false;
        return salaryThirds > d;
    }

    /// <summary>
    /// Sperre nach einer gescheiterten Verhandlung in Tagen. [ORIGINAL] Datei 0x4AD3E: Kaderplatz +0x18 = random(10, 18);
    /// der Tagesablauf zählt sie herunter (0x76F6), solange sie gilt und das Saisonende nicht erreicht ist, antwortet der
    /// Spieler „ist im Moment nicht verhandlungsbereit.“ (0x4A704).
    /// </summary>
    public static int LockDays(Random random) => TransferRules.Random16(random, 10, 18);

    /// <summary>Faktor DGROUP 0xE1A4: random(85, 115), gezogen zu Beginn jedes Managerzugs (Datei 0x44E3F).</summary>
    public static int TurnFactor(Random random) => TransferRules.Random16(random, 85, 115);
}

public sealed partial class GameState
{
    /// <summary>
    /// Faktor der Gehaltsforderung (DGROUP 0xE1A4) des laufenden Managerzugs. [ORIGINAL] Gezogen zu Beginn jedes Managerzugs
    /// (Datei 0x44E3F, <see cref="BeginManagerTurn"/>; Spur ui-meldungen Teil 4, E4 = B). Der erste Tag eines neuen Spiels zieht
    /// ihn in <see cref="NewGame.Create"/> (R12U-04). [APPROX] Solange in einem älteren
    /// Spielstand noch nicht gezogen wurde, gilt der frühere, je Saison und Tag abgeleitete Wert. Rein (zieht nichts).
    /// </summary>
    public int ContractTurnFactor => _contractTurnFactor != 0 ? _contractTurnFactor
        : ContractRules.TurnFactor(new GameRandom(unchecked(Season * 1_000_003 + CurrentDay * 7_919
            + Managers.Sum(m => m.ClubIndex * 41 + m.Head) + 17)));

    /// <summary>
    /// Nach dem Saisonende (Tageszähler 0x3B7 &gt; 232)? [APPROX] Das Remake hat zwischen dem Bildschirm „Saisonende“ und
    /// der neuen Saison keine Spieltage; als „nach dem Saisonende“ gilt die Phase <see cref="SeasonPhase.SeasonOver"/> und die Zeit
    /// der Verhandlungen am Saisonende (<see cref="HasPendingRenewals"/>: Original 0x26F20 mit 0x3B7 = 233, Datei 0x26F55).
    /// </summary>
    public bool AfterSeasonEnd => Phase == SeasonPhase.SeasonOver || HasPendingRenewals;

    /// <summary>
    /// Restanteil der Saison für die Gehaltsrechnung (nächster Spieltag = gespielte + 1). [APPROX] Während der Verhandlungen am
    /// Saisonende (Spieltage schon zurückgesetzt) wie in der Phase <see cref="SeasonPhase.SeasonOver"/>: nächster Spieltag 45.
    /// </summary>
    public int ContractSeasonRemaining => ContractRules.SeasonRemaining(
        (Phase != SeasonPhase.SeasonOver && HasPendingRenewals ? ContractRules.SeasonMatchDays : PlayedMatchDays) + 1);

    /// <summary>
    /// Täglich (Tagesablauf 0x07169, Datei 0x76E5–0x76F6): Verhandlungssperren der Spieler aller Manager zählen um einen Tag
    /// herunter; Sperren nach abgelehnten Geboten lösen. Gerufen aus <see cref="AnnounceContractEnds"/>.
    /// </summary>
    private void AdvanceNegotiationLocks(Manager? only = null)
    {
        foreach (var club in ActiveManagers.Where(m => only is null || m == only).Select(m => Clubs[m.ClubIndex]).Distinct())
            foreach (var p in club.Players)
                if (p.NegotiationLock > 0)
                    p.NegotiationLock--;
        // Sperren nach abgelehntem Gebot (Tagesablauf 0x071CC); Zufall der Verträge, damit Spielergebnisse unberührt bleiben.
        // Die Quelle erst anfassen, wenn eine Sperre besteht: Ihr Startwert hängt vom Zeitpunkt der ersten Nutzung ab.
        // #678: [ORIGINAL] nur im Lauf von Manager 0 (Datei 0x0718F–0x07197).
        if ((only is null || (Managers.Count > 0 && Managers[0] == only))
            && TransferMarket.Listings.Any(l => (l.RejectedBy & 0x7F) == 1))
            TransferMarket.ReleaseBidLocks(ContractRandom);
    }
}
