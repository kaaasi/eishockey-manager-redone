namespace EishockeyManager.Core;

/// <summary>Ausgang der Verhandlung am Saisonende (<see cref="GameState.NegotiateRenewal"/>, <see cref="GameState.CancelRenewal"/>).</summary>
/// <param name="Stays">„… bleibt Ihnen auch die nächste Saison erhalten“.</param>
/// <param name="Compensation">Ablösesumme in DM, wenn der Spieler geht („Sie erhalten eine Ablösesumme von … DM“).</param>
public sealed record ContractRenewal(Player Player, bool Stays, long Compensation)
{
    /// <summary>Meldungen des Originals nach der Verhandlung (DGROUP 0x138D–0x13A1, Datei 0x27DDB, 0x27E8B, 0x27EDF).</summary>
    public IReadOnlyList<string[]> Messages => Stays
        ? [[Player.Name, "bleibt Ihnen auch", "die nächste Saison erhalten"]]
        :
        [
            [Player.Name, "kehrt Ihrem Verein", "den Rücken..."],
            ["Sie erhalten eine", "Ablösesumme von", $"{Compensation:N0} DM".Replace(',', '.')],
        ];
}

/// <summary>
/// Verhandlung ausgelaufener Verträge am Saisonende (Spur <c>transfer-vertraege-komplett</c>, Teil 1).
/// </summary>
/// <remarks>
/// [ORIGINAL] em.exe Saisonende 0x26F20 (Ov 0xD0), Datei 0x27D05–0x27EE4, je Manager und Kaderplatz mit Laufzeit +0x0B = 0
/// (nach dem Herunterzählen 0x27500): +0x18 = 101 (Angebot „1 Jahr“), +0x0B = 1, Gehalt +0x28 = Wert(Gehalt) · (147 − Alter) / 100
/// (Alter Spielerpool +0x14, schon erhöht 0x274A7), dann Vertragsverhandlung 0x118:0x75 (0x4A1DB). Steht der Vertrag danach
/// mit Laufzeit ≠ 0: „… bleibt Ihnen auch die nächste Saison erhalten“; sonst (auch „Abbruch“): „… kehrt Ihrem Verein den
/// Rücken...“, der Manager erhält Wert(Preis) / 2 („Ablösesumme“, 0x27DF3–0x27E1C) und der Kaderplatz wird frei (0x108:0x48).
/// <para>Abbildung im Remake: Der Kaderwechsel (<see cref="GameState.StartNextSeason"/>) lässt den Spieler mit Laufzeit 0 und dem
/// Angebot „von 0 auf 1 Jahr“ (<see cref="ContractNotice.Offered"/>, <see cref="Player.NoticeYears"/> = 1) im Kader – genau der
/// Zustand +0x0B = 0, +0x18 = 101 des Originals, ohne neues Spielstandsfeld (<see cref="StartNextSeasonWithRenewals"/>). Der
/// Bildschirm nach „Erhaltene Prämien“ führt die Verhandlungen; was bis zum ersten Tag der neuen Saison offen ist, endet wie
/// „Abbruch“ (<see cref="ResolveRenewals"/>). Ohne Oberfläche (<see cref="StartNextSeason"/>) enden sie sofort so.</para>
/// <para>[ORIGINAL] Reihenfolge in 0x26F20 (Spur kader-transfer, Teil 5, A226): erst für alle Manager Prämien, Altern und
/// Herunterzählen (Schleife Datei 0x26FB0–0x279FA), dann für alle Manager die Saisonzähler des Kaderplatzes auf 0 (Datei
/// 0x27A0C–0x27AA8: Strafen +0x00/+0x01, Tore +0x03–+0x05, Saisonspiele +0x06–+0x08; 29 Plätze), erst dann die Verhandlungen
/// (Schleife ab Datei 0x27B64). Die Spiele beim jetzigen Verein (+0x1C/+0x1E/+0x20) bleiben stehen und gehen in Gehalt und
/// Ablöse ein; das Remake überträgt sie vor dem Zurücksetzen (<see cref="CarryGamesForClub"/>, <see cref="Player.GamesForClub"/>).
/// Der Auf- und Abstieg liegt im Original schon davor (Datei 0x56137–0x563AD vor dem Aufruf 0x5645D), wie im Remake.
/// [APPROX] Die Ablöse bucht das Remake als Transfereinnahme (<see cref="GameState.Book"/>); das Original schreibt sie ohne den
/// Aufruf 0x18:0x223e des Kaufs direkt auf das Konto.</para>
/// </remarks>
public sealed partial class GameState
{
    /// <summary>Wartet der Spieler auf die Verhandlung am Saisonende (Laufzeit 0 mit Angebot „1 Jahr“)?</summary>
    public static bool IsRenewalPending(Player p) =>
        p.ContractYears == 0 && !p.OnLoan && p.Notice == ContractNotice.Offered && p.NoticeYears == 1;

    /// <summary>Spieler im Kader des Managers, deren Vertrag am Saisonende verhandelt wird, in Kaderreihenfolge.</summary>
    public IReadOnlyList<Player> PendingRenewals(Manager manager) =>
        Clubs[manager.ClubIndex].Players.Where(IsRenewalPending).ToList();

    /// <summary>Gibt es noch offene Verhandlungen am Saisonende (bei irgendeinem aktiven Manager)?</summary>
    public bool HasPendingRenewals => ActiveManagers.Any(m => Clubs[m.ClubIndex].Players.Any(IsRenewalPending));

    /// <summary>
    /// Gehalt, mit dem die Verhandlung am Saisonende beginnt, in Drittel-DM. [ORIGINAL] Datei 0x27D1F–0x27D70:
    /// Wert(Gehalt) (0x118:0x70 Art 1) · (147 − Alter) / 100.
    /// </summary>
    public int RenewalSalaryThirds(Manager manager, Player p)
    {
        var value = PlayerValueOf(p, Clubs[manager.ClubIndex], PlayerValueKind.Salary, NoValueRandom, manager.Level);
        return unchecked(value * (147 - p.Age)) / 100;
    }

    /// <summary>Kaderwechsel: Vertrag ist ausgelaufen, der Spieler wartet auf die Verhandlung (0x27D15–0x27D70).</summary>
    private void BeginRenewal(Manager manager, Player p)
    {
        p.Notice = ContractNotice.Offered;
        p.NoticeYears = 1;
        p.ContractYears = 0;
        // [ORIGINAL] +0x28 in Drittel-DM; [APPROX] das Remake führt Gehälter in DM (ganze DM: Teil 2, R8W3I-08).
        p.MonthlySalary = Math.Max(Contracts.MinSalary, RenewalSalaryThirds(manager, p) / 3);
    }

    /// <summary>
    /// „Mach' ich!“ in der Verhandlung am Saisonende: Verhandlung wie im Bildschirm „Verträge“ (<see cref="Contracts.Negotiate"/>,
    /// nach dem Saisonende), danach bleibt der Spieler oder geht gegen Ablöse. Ohne offene Verhandlung: null.
    /// </summary>
    public (NegotiationResult Result, ContractRenewal Outcome)? NegotiateRenewal(Manager manager, Player p, int years, int salary,
        bool insured, bool demandFilled)
    {
        if (!IsPending(manager, p))
            return null;
        var result = Contracts.Negotiate(this, manager, p, years, salary, insured, demandFilled);
        // [ORIGINAL] 0x27D99–0x27DAA: Ergebnis 1 und Laufzeit +0x0B ≠ 0 → bleibt.
        return (result, result == NegotiationResult.Accepted && p.ContractYears > 0 ? Stay(p) : Leave(manager, p));
    }

    /// <summary>„Abbruch“ in der Verhandlung am Saisonende: der Spieler geht gegen Ablöse (Ergebnis 0, ohne Sperre). Ohne offene
    /// Verhandlung: null.</summary>
    public ContractRenewal? CancelRenewal(Manager manager, Player p) => IsPending(manager, p) ? Leave(manager, p) : null;

    private bool IsPending(Manager manager, Player p)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(p);
        return Managers.Contains(manager) && Clubs[manager.ClubIndex].Players.Contains(p) && IsRenewalPending(p);
    }

    private static ContractRenewal Stay(Player p)
    {
        // Das Angebot ist eingelöst; im neuen letzten Vertragsjahr darf der Spieler wieder ankündigen (Spur „kader“).
        p.Notice = ContractNotice.None;
        p.NoticeYears = 0;
        return new ContractRenewal(p, true, 0);
    }

    private ContractRenewal Leave(Manager manager, Player p)
    {
        var club = Clubs[manager.ClubIndex];
        // [ORIGINAL] 0x27DE3–0x27E1C: Wert(Preis) / 2 in Drittel-DM aufs Konto (Anzeige / 3).
        var value = PlayerValueOf(p, club, PlayerValueKind.Price, ContractRandom, manager.Level);
        var compensation = (long)(value / 2) / 3;
        if (compensation > 0)
            Book(manager, FinanceCategory.Transfers, compensation);
        club.RemovePlayer(p);
        if (TransferMarket.ListingOf(p) is { } listing)
            TransferMarket.Withdraw(listing);
        p.ContractYears = 0;
        p.MonthlySalary = 0;
        p.Insured = false;
        p.InsuranceLocked = false;
        p.Notice = ContractNotice.None;
        p.NoticeYears = 0;
        p.NegotiationLock = 0;
        TransferMarket.Release(club, p);
        return new ContractRenewal(p, false, compensation);
    }

    /// <summary>
    /// Erster Tag der neuen Saison: noch offene Verhandlungen enden wie „Abbruch“ (der Spieler geht gegen Ablöse). Ohne
    /// Bildschirm (Tests, Hot-Seat ohne Oberfläche) ist das der Ausgang ohne Eingabe; im Spiel führt der Bildschirm nach den
    /// Prämien alle Verhandlungen vorher.
    /// </summary>
    internal void ResolveRenewals()
    {
        foreach (var manager in ActiveManagers.ToList())
            foreach (var p in PendingRenewals(manager))
                CancelRenewal(manager, p);
    }

    /// <summary>
    /// Saisonwechsel aus der Oberfläche: wie <see cref="StartNextSeason"/>, aber ausgelaufene Verträge der Manager bleiben zur
    /// Verhandlung im Kader (<see cref="PendingRenewals"/>), bis der Bildschirm sie führt. Ohne diesen Aufruf (Tests, Abläufe
    /// ohne Oberfläche) enden sie sofort wie „Abbruch“.
    /// </summary>
    public void StartNextSeasonWithRenewals()
    {
        s_deferRenewals = true;
        try
        {
            StartNextSeason();
        }
        finally
        {
            s_deferRenewals = false;
        }
    }

    // Nur für die Dauer von StartNextSeasonWithRenewals gesetzt; statisch, damit es kein Spielstandsfeld ist.
    [ThreadStatic] private static bool s_deferRenewals;

    private static readonly Random NoValueRandom = new GameRandom(0);

    /// <summary>
    /// Saisonwechsel, vor dem Zurücksetzen der Saisonzähler: Die Spiele der abgelaufenen Saison gehen in die Spiele beim jetzigen
    /// Verein über (<see cref="Player.ClubGamesCarry"/>). [ORIGINAL] Datei 0x27A0C–0x27AA8 lässt +0x1C stehen; nur Datei 0x41622
    /// und 0x5499E schreiben das Wort (erhöhen). [APPROX] Auch für Spieler der Computervereine (im Original ohne Kaderplatz) und
    /// geliehene Spieler (zählen nach der Rückkehr beim Stammverein weiter).
    /// </summary>
    internal void CarryGamesForClub()
    {
        foreach (var p in Clubs.SelectMany(c => c.Players))
            p.ClubGamesCarry = p.GamesForClub;
    }
}
