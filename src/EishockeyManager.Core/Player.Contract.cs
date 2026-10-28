namespace EishockeyManager.Core;

/// <summary>Eigentümer und unveränderter Stammvertrag während einer Leihe bis zum Saisonende.</summary>
public sealed record PlayerLoan(Club Origin, int ContractYears, int MonthlySalary, bool Insured);

/// <summary>
/// Ankündigung eines Spielers im letzten Vertragsjahr (Posteingang, Texte aus em.exe). Gilt bis zum Saisonwechsel.
/// </summary>
public enum ContractNotice
{
    /// <summary>Noch nichts angekündigt.</summary>
    None,

    /// <summary>„… bietet Ihnen an, von 1 auf n Jahre zu verlängern.“</summary>
    Offered,

    /// <summary>„… kündigt an, daß er seinen Vertrag nicht mehr verlängern wird.“ – nicht verhandlungsbereit.</summary>
    Refused,

    /// <summary>Wie <see cref="Refused"/>, setzt sich aber mit Ablauf des Vertrages zur Ruhe.</summary>
    Retiring,
}

/// <summary>Vertragsdaten eines Spielers (Bildschirm „Verträge“: V.DAUER und Gehalt).</summary>
public sealed partial class Player
{
    /// <summary>Restlaufzeit des Vertrags in Saisons („2 Jahre“). 0 = Vertrag läuft zum Saisonende aus.</summary>
    public int ContractYears { get; set; }

    /// <summary>Monatliches Gehalt in DM (Spalte WERT, „Monatliches Gehalt des Spielers“).</summary>
    public int MonthlySalary { get; set; }

    /// <summary>Versicherung gegen Verletzungen (Anzeige „(v)“ hinter der Vertragsdauer).</summary>
    public bool Insured { get; set; }

    /// <summary>Ausgeliehener Spieler („ (ausgeliehen)“) – kehrt zum Saisonende zum Eigentümer zurück.</summary>
    public bool OnLoan { get; set; }

    public PlayerLoan? Loan { get; set; }

    /// <summary>
    /// Spieltag, an dem der Spieler zuletzt ein Vertragsangebot abgelehnt hat („Angebot wurde bereits abgelehnt.“),
    /// oder null.
    /// </summary>
    public int? RejectedOfferDay { get; set; }

    /// <summary>Ankündigung im letzten Vertragsjahr (nur Spieler der Manager-Vereine), bis zum Saisonwechsel.</summary>
    public ContractNotice Notice { get; set; }

    /// <summary>Bei <see cref="ContractNotice.Offered"/>: angebotene Gesamtlaufzeit („von 1 auf n Jahre“).</summary>
    public int NoticeYears { get; set; }

    /// <summary>„2 Jahre“, „1 Jahr“.</summary>
    public static string YearsText(int years) => years == 1 ? "1 Jahr" : $"{years} Jahre";

    /// <summary>Spalte V.DAUER: „2 Jahre“, mit Versicherung „4 Jahre(v)“.</summary>
    public string ContractText => YearsText(ContractYears) + (Insured ? "(v)" : "");
}

public sealed partial class GameState
{
    // Vor jeder Mutation des Saisonwechsels prüfen: Ein boolesches Leihflag allein reicht nicht zur Rückgabe.
    private void ValidateSeasonSquads()
    {
        var players = Clubs.SelectMany(c => c.Players).ToArray();
        if (players.Distinct().Count() != players.Length)
            throw new InvalidOperationException("Ein Spieler steht in mehreren Kadern.");
        foreach (var club in Clubs)
            foreach (var player in club.Players)
                if (player.OnLoan != (player.Loan is not null)
                    || player.Loan is { } loan && (!Clubs.Contains(loan.Origin) || loan.Origin == club))
                    throw new InvalidOperationException($"Ungültiger Leihursprung für {player.Name}.");
        // Kapazität wird nicht vorab geprüft, sondern beim Rückführen aufgelöst (siehe StartSquadSeason): So können
        // Vorprüfung und Ausführung nicht auseinanderlaufen (Review PR35-01).
    }

    /// <summary>
    /// Gemeinsamer Kaderübergang: alle Spieler einmal altern, Leihen zurückführen, Verträge beenden, Laufbahnenden,
    /// Kader-KI der Computervereine (Verlängern, Neuzugänge aus Pool und Talenten), Pool begrenzen.
    /// </summary>
    private void StartSquadSeason()
    {
        var managed = Managers.Select(m => m.ClubIndex).ToHashSet();
        // Kadergröße je Position vor dem Wechsel: Computervereine füllen höchstens bis dahin wieder auf.
        var before = Clubs.Where(c => c.Players.Count > 0 && !managed.Contains(c.Index)).ToDictionary(c => c,
            c => Enum.GetValues<Formats.PlayerRole>().ToDictionary(r => r, r => c.Players.Count(p => p.Role == r)));

        AgeFreeAgents();
        // Snapshot vor Rückgaben: Vereinsreihenfolge darf keinen Spieler zweimal altern lassen.
        var players = Clubs.SelectMany(c => c.Players.Select(p => (Club: c, Player: p))).ToArray();
        var returns = new List<(Club Owner, Player Player)>();
        foreach (var (club, player) in players)
        {
            player.Age++;
            player.RejectedOfferDay = null;
            var notice = player.Notice;
            player.Notice = ContractNotice.None;
            player.NoticeYears = 0;
            var owner = club;
            if (player.Loan is { } loan)
            {
                club.Players.Remove(player);
                club.Lineup.Remove(player);
                owner = loan.Origin;
                player.ContractYears = loan.ContractYears;
                player.MonthlySalary = loan.MonthlySalary;
                player.Insured = loan.Insured;
                player.OnLoan = false;
                player.Loan = null;
            }

            player.ContractYears = Math.Max(0, player.ContractYears - 1);
            if (!managed.Contains(owner.Index))
            {
                // Kader-KI (R2-12) statt der Überbrückung „Computervereine verlängern immer“ aus Welle B.
                ComputerContractDecision(owner, club, player);
                if (RetiredPlayers.Contains(player))
                    continue;
            }
            else if (player.ContractYears == 0 && notice == ContractNotice.Retiring)
            {
                // „… wird sich mit Ablauf des Vertrages zur Ruhe setzen.“ – angekündigt im letzten Vertragsjahr.
                club.Players.Remove(player);
                club.Lineup.Remove(player);
                Retire(owner, player);
                continue;
            }
            if (player.ContractYears == 0)
            {
                owner.Players.Remove(player);
                owner.Lineup.Remove(player);
                player.MonthlySalary = 0;
                player.Insured = false;
                TransferMarket.Release(owner, player);
            }
            else if (owner != club)
                returns.Add((owner, player));
        }
        // Erst nach allen Abgängen Nummern vergeben, unabhängig von der Vereinsreihenfolge.
        foreach (var (owner, player) in returns)
        {
            if (owner.Players.Count >= PlayerFactory.MaxNumber)
            {
                // [APPROX] Stammverein hat keine freie Rückennummer mehr: Spieler wird vertragslos statt verloren.
                player.ContractYears = 0;
                player.MonthlySalary = 0;
                player.Insured = false;
                TransferMarket.Release(owner, player);
                continue;
            }
            player.Number = PlayerFactory.FreeNumber(owner, player);
            owner.Players.Add(player); // Stammvertrag erhalten; Join würde ihn überschreiben.
        }
        RefillComputerSquads(before);
        LimitFreeAgents();
        CleanLineups();
        TransferMarket.StartSeason(this);
    }
}

/// <summary>Ergebnis einer Vertragsverhandlung.</summary>
public enum NegotiationResult
{
    /// <summary>„Ihr Angebot wurde angenommen !“</summary>
    Accepted,

    /// <summary>„Ihr Angebot wurde abgelehnt.“</summary>
    Rejected,

    /// <summary>„Angebot wurde bereits abgelehnt.“ – an diesem Spieltag keine neue Verhandlung.</summary>
    AlreadyRejected,

    /// <summary>„Dieser Betrag ist nicht erlaubt (unfair) !“</summary>
    Unfair,

    /// <summary>„… ist im Moment nicht verhandlungsbereit.“ – Spieler hat angekündigt, nicht zu verlängern.</summary>
    NotWilling,

    /// <summary>„… wird sich mit Ablauf des Vertrages zur Ruhe setzen.“</summary>
    Retiring,
}

/// <summary>
/// Gehälter und Vertragsverhandlung.
/// [APPROX] Die Regeln des Originals (Overlay 4D30) sind nicht rekonstruiert. Belegt im Video: Startgehälter
/// der Feldspieler 900–1.400 DM ohne erkennbaren Zusammenhang mit der Stärke, Torhüter 1.500 DM; eine
/// Verlängerung von 2 auf 4 Jahre zum bisherigen Gehalt wird angenommen.
/// </summary>
public static class Contracts
{
    public const int MinYears = 1;
    public const int MaxYears = 5;

    /// <summary>Kleinster erlaubter Betrag. Im Video springt eine Eingabe von 0 DM auf 1 DM.</summary>
    public const int MinSalary = 1;

    /// <summary>Monatliche Versicherungsprämie in Prozent des Gehalts. [APPROX]</summary>
    public const int InsurancePercent = 10;

    /// <summary>
    /// Gehaltsforderung aus Stärke und Talent, auf 100 DM gerundet. Torhüter verdienen ein Viertel mehr.
    /// [APPROX] So liegen Feldspieler der Stärke 35 bei 900–1.300 DM, Torhüter bei 1.100–1.600 DM.
    /// </summary>
    public static int Demand(Player p)
    {
        double value = 25 * p.Strength + 50 * p.Talent;
        if (p.Role == Formats.PlayerRole.Goalkeeper)
            value *= 1.25;
        return Math.Max(100, (int)Math.Round(value / 100) * 100);
    }

    /// <summary>Gehalt bei Spielbeginn: die Forderung des Spielers.</summary>
    public static int BaseSalary(Player p, Random random) => Demand(p);

    /// <summary>Monatliche Versicherungsprämie eines Spielers.</summary>
    public static int InsurancePremium(Player p) => p.Insured ? p.MonthlySalary * InsurancePercent / 100 : 0;

    /// <summary>Monatliche Gehalts- und Versicherungskosten eines Kaders (für das Paket „manager“).</summary>
    public static long MonthlyCosts(Club club) => club.Players.Sum(p => (long)p.MonthlySalary + InsurancePremium(p));

    /// <summary>
    /// Vertragsangebot. Ab der Forderung nimmt der Spieler sicher an, darunter mit sinkender Wahrscheinlichkeit
    /// (bei 80 % der Forderung nie). Nach einer Ablehnung verhandelt er erst am nächsten Spieltag wieder.
    /// </summary>
    public static NegotiationResult Offer(Player p, int years, int salary, bool insured, int matchDay, Random random)
    {
        // Angekündigte Absage bzw. Ruhestand gilt bis zum Saisonwechsel (R2-11).
        if (p.Notice == ContractNotice.Refused)
            return NegotiationResult.NotWilling;
        if (p.Notice == ContractNotice.Retiring)
            return NegotiationResult.Retiring;
        if (salary < MinSalary || years < MinYears || years > MaxYears)
            return NegotiationResult.Unfair;
        if (p.RejectedOfferDay == matchDay)
            return NegotiationResult.AlreadyRejected;

        // Startgehalt = Forderung, daher wird eine Verlängerung zum bisherigen Gehalt angenommen
        // (Video: 2 → 4 Jahre), solange der Spieler nicht stärker geworden ist.
        var demand = Demand(p);
        var ratio = salary / (double)demand;
        var accept = ratio >= 1 || (ratio > 0.8 && random.NextDouble() < (ratio - 0.8) / 0.2);
        if (!accept)
        {
            p.RejectedOfferDay = matchDay;
            return NegotiationResult.Rejected;
        }

        p.ContractYears = years;
        p.MonthlySalary = salary;
        p.Insured = insured;
        return NegotiationResult.Accepted;
    }

    /// <summary>
    /// Meldung zum Ergebnis einer Verhandlung mit diesem Spieler. Die beiden Absagen nennen den Namen
    /// (Originaltexte aus em.exe: „ist im Moment / nicht verhandlungs- / bereit.“, „wird sich mit /
    /// Ablauf des Vertrages / zur Ruhe setzen.“; Name als eigene Zeile [APPROX]).
    /// </summary>
    public static string[] Message(NegotiationResult result, Player p) => result switch
    {
        NegotiationResult.NotWilling => [p.Name, "ist im Moment", "nicht verhandlungs-", "bereit."],
        NegotiationResult.Retiring => [p.Name, "wird sich mit", "Ablauf des Vertrages", "zur Ruhe setzen."],
        _ => Message(result),
    };

    /// <summary>Text der Meldung zum Ergebnis (Originaltexte aus em.exe).</summary>
    public static string[] Message(NegotiationResult result) => result switch
    {
        NegotiationResult.Accepted => ["Ihr Angebot wurde", "angenommen !"],
        NegotiationResult.Rejected => ["Ihr Angebot", "wurde abgelehnt."],
        NegotiationResult.AlreadyRejected => ["Angebot wurde", "bereits abgelehnt."],
        _ => ["Dieser Betrag ist", "nicht erlaubt (unfair) !"],
    };
}
