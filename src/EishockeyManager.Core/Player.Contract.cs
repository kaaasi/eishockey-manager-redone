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

    /// <summary>
    /// Versicherungsprämie in DM je Monat (Kaderplatz +0x30, im Original Drittel-DM); 0 = nicht versichert. [ORIGINAL] Im
    /// Verhandlungsfenster Wert(Gehalt) / 2 (Datei 0x4AA9C, <see cref="Contracts.InsuranceOffer"/>); monatlich mit den Gehältern
    /// abgerechnet (0x3E8DC); Leistung bei Verletzungen <see cref="GameState.PayInsurance"/>.
    /// </summary>
    public int InsurancePremium { get; set; }

    /// <summary>
    /// Kaderplatz +0x2C Bit 31: Versicherung im Verhandlungsfenster abgeschlossen. [ORIGINAL] Datei 0x4AB73–0x4AB8D setzt das Bit
    /// bei „Mach' ich!“, wenn das Fenster die Versicherung neu eingeschaltet hat – vor der Prüfung des Ergebnisses, also auch
    /// bei einer gescheiterten Verhandlung (die Prämie wird dann zurückgesetzt, 0x4AD54, das Bit nicht). Danach lässt sich die
    /// Versicherung nicht mehr umschalten (0x4AA74).
    /// </summary>
    public bool InsuranceLocked { get; set; }

    /// <summary>
    /// Versichert (Anzeige „(v)“ hinter der Vertragsdauer, em.exe 0x42410: +0x30 ≠ 0). Setzen ohne bekannte Prämie:
    /// [APPROX] <see cref="Contracts.LegacyInsurancePercent"/> % des Gehalts (bisheriges Modell; Aufrufer ohne Spielstand).
    /// </summary>
    public bool Insured
    {
        get => InsurancePremium > 0;
        set => InsurancePremium = !value ? 0
            : InsurancePremium > 0 ? InsurancePremium
            : Math.Max(1, MonthlySalary * Contracts.LegacyInsurancePercent / 100);
    }

    /// <summary>Ausgeliehener Spieler („ (ausgeliehen)“) – kehrt zum Saisonende zum Eigentümer zurück.</summary>
    public bool OnLoan { get; set; }

    public PlayerLoan? Loan { get; set; }

    /// <summary>
    /// Verhandlungssperre in Tagen (Kaderplatz +0x18, Werte 1–99): nach einer gescheiterten Verhandlung random(10, 18)
    /// (<see cref="ContractRules.LockDays"/>), täglich um 1 herunter. 0 = keine Sperre.
    /// </summary>
    public int NegotiationLock { get; set; }

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
        // Spur transfer-vertraege-komplett: ausgelaufene Verträge im Kader eines Managers werden am Saisonende verhandelt
        // (0x27D05). Im Modus „nur Weltkugel“ gibt es keine Vereinssaison und damit keine Verhandlung.
        var renewers = WorldOnly ? new Dictionary<int, Manager>() : ActiveManagers.ToDictionary(m => m.ClubIndex);
        var returns = new List<(Club Owner, Player Player)>();
        foreach (var (club, player) in players)
        {
            player.Age++;
            // [ORIGINAL] R8W3I-09: Das Saisonende 0x26F20 lässt die Sperre +0x18 stehen (0x274E8–0x27539); nur der Durchlauf ohne
            // Halt (DGROUP 0x1DB, gibt es im Remake nicht) setzt sie bei 0x27534 auf 0. Sie zählt im Tagesablauf weiter herunter.
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
                player.Insured = loan.Insured; // [APPROX] Prämie der Rückkehr: bisherige, sonst LegacyInsurancePercent
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
            if (player.ContractYears == 0 && owner == club && renewers.TryGetValue(owner.Index, out var renewer))
            {
                BeginRenewal(renewer, player);
                if (!s_deferRenewals)
                    CancelRenewal(renewer, player); // ohne Oberfläche: wie „Abbruch“ (Spieler geht gegen Ablöse)
                continue;
            }
            if (player.ContractYears == 0)
            {
                owner.Players.Remove(player);
                owner.Lineup.Remove(player);
                player.MonthlySalary = 0;
                player.Insured = false;
                player.InsuranceLocked = false;
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
                player.InsuranceLocked = false;
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

/// <summary>Ergebnis einer Vertragsverhandlung (Meldungen aus em.exe, Dialog 0x4A1DB).</summary>
public enum NegotiationResult
{
    /// <summary>Vertrag geändert; der Dialog schließt ohne Meldung.</summary>
    Accepted,

    /// <summary>„… ist nicht an Ihrem Angebot interessiert.“ (DGROUP 0x148D–0x1497, Datei 0x4ACFE).</summary>
    NotInterested,

    /// <summary>„… ist im Moment nicht verhandlungsbereit.“ – Sperre nach einer gescheiterten Verhandlung (0x4A704).</summary>
    NotWilling,

    /// <summary>„… wird sich mit Ablauf des Vertrages zur Ruhe setzen.“ (Kaderplatz +0x18 Bit 0x80, 0x4A6C7).</summary>
    Retiring,

    /// <summary>„… ist leider von … nur ausgeliehen.“ (Kaderplatz +0x0C, 0x4A692).</summary>
    Loaned,

    /// <summary>„SOOOO lange verpflichtet sich kein Eishockey-Spieler !“ – mehr als 4 Jahre (0x4AB93).</summary>
    TooLong,

    /// <summary>„So dumm ist … leider nicht...“ – kürzer als der laufende Vertrag (0x4AC75).</summary>
    TooShort,

    /// <summary>„Dieser Betrag ist nicht erlaubt (unfair) !“ – [APPROX] Gehalt unter 1 DM (Eingabe im Video).</summary>
    Unfair,
}

/// <summary>
/// Gehälter und Vertragsverhandlung. Verhandlung nach em.exe 0x4A1DB/0x49A3B (<see cref="ContractRules"/>,
/// <see cref="Negotiate"/>). [APPROX] Startgehalt und Gehalt nach einem Wechsel (<see cref="Demand"/>) sind noch nicht auf
/// den Spielerwert 0x118:0x70 Art 1 umgestellt (Spielstart 0x29EE4, Kauf 0x4A1DB aus 0x4786A). Belegt im Video:
/// Startgehälter der Feldspieler 900–1.400 DM, Torhüter 1.500 DM.
/// </summary>
public static class Contracts
{
    public const int MinYears = 1;

    /// <summary>Längste Laufzeit (<see cref="ContractRules.MaxYears"/>).</summary>
    public const int MaxYears = ContractRules.MaxYears;

    /// <summary>Kleinster erlaubter Betrag. Im Video springt eine Eingabe von 0 DM auf 1 DM.</summary>
    public const int MinSalary = 1;

    /// <summary>[APPROX] Prämie in Prozent des Gehalts, wenn eine Versicherung ohne bekannten Betrag gesetzt wird
    /// (<see cref="Player.Insured"/>, Migration älterer Spielstände). Das Original kennt nur den Betrag +0x30.</summary>
    public const int LegacyInsurancePercent = 10;

    /// <summary>Vielfaches der Prämie bei einer Verletzung (Datei 0x7089: +0x30 · 4, Aufrufe 0x7542 Tagesablauf, 0x38D6A Spiel).</summary>
    public const int InjuryPayoutFactor = 4;

    /// <summary>Vielfaches der Prämie, wenn die Verletzung die Laufbahn beendet (+0x0D = 0xFF, Saisonende 0x2772B–0x277DE: · 60).</summary>
    public const int CareerEndPayoutFactor = 60;

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

    /// <summary>Monatliche Versicherungsprämie eines Spielers (Kaderplatz +0x30, <see cref="Player.InsurancePremium"/>).</summary>
    public static int InsurancePremium(Player p) => p.InsurancePremium;

    /// <summary>
    /// Prämie, die das Verhandlungsfenster beim Einschalten einträgt. [ORIGINAL] Datei 0x4AA9C–0x4AABF: Wert(Gehalt) (0x9D36 Art 1)
    /// / 2, in Drittel-DM. [APPROX] Ganze DM (Datenmodell des Remakes, abgerundet; mindestens 1 DM, damit „versichert“ erhalten
    /// bleibt). Rein, ohne Zufall.
    /// </summary>
    public static int InsuranceOffer(GameState game, Manager manager, Player p)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(manager);
        var club = game.Clubs[manager.ClubIndex];
        var value = game.PlayerValueOf(p, club, PlayerValueKind.Salary, NoRandom, manager.Level);
        return (int)Math.Max(1, value / 2 / 3);
    }

    /// <summary>
    /// Lässt sich die Versicherung im Verhandlungsfenster umschalten? [ORIGINAL] Datei 0x4AA74–0x4AA90: nicht nach einem Abschluss
    /// im Fenster (+0x2C Bit 31, <see cref="Player.InsuranceLocked"/>) und nicht bei einem verletzten Spieler (+0x09 &amp; 3 = 2).
    /// </summary>
    public static bool CanChangeInsurance(Player p) => !p.InsuranceLocked && p.InjuredGames == 0;

    /// <summary>
    /// Versicherung nach „Mach' ich!“ (nicht nach „Abbruch“: 0x4AB6E springt vorbei). <paramref name="insured"/> ist der Stand
    /// im Fenster; ohne <see cref="CanChangeInsurance"/> bleibt er wie beim Öffnen. [ORIGINAL] 0x4AB73–0x4AB8D: neu eingeschaltet →
    /// Bit 31 (<see cref="Player.InsuranceLocked"/>), auch wenn die Verhandlung scheitert; gescheitert → Prämie wie beim Öffnen
    /// (0x4AD54); angenommen → Prämie aus dem Fenster (eingeschaltet: <see cref="InsuranceOffer"/>, ausgeschaltet: 0).
    /// </summary>
    internal static void ApplyInsurance(GameState game, Manager manager, Player p, bool insured, bool accepted, bool lockOnFailure)
    {
        if (!CanChangeInsurance(p))
            insured = p.Insured;
        var added = insured && !p.Insured;
        if (added && (accepted || lockOnFailure))
            p.InsuranceLocked = true;
        if (!accepted)
            return;
        if (added)
            p.InsurancePremium = InsuranceOffer(game, manager, p);
        else if (!insured)
            p.InsurancePremium = 0;
    }

    /// <summary>Monatliche Gehalts- und Versicherungskosten eines Kaders (für das Paket „manager“).</summary>
    public static long MonthlyCosts(Club club) => club.Players.Sum(p => (long)p.MonthlySalary + InsurancePremium(p));

    /// <summary>
    /// Darf über Laufzeit oder Gehalt verhandelt werden? [ORIGINAL] Datei 0x4A692–0x4A770 (beim Anklicken der Felder
    /// „Vertragsdauer“ und „Gehalt / Monat“): ausgeliehen → <see cref="NegotiationResult.Loaned"/>; Ruhestand angekündigt →
    /// <see cref="NegotiationResult.Retiring"/>; Sperre +0x18 und Saisonende nicht erreicht →
    /// <see cref="NegotiationResult.NotWilling"/>. [APPROX] Die Absage <see cref="ContractNotice.Refused"/> des Remakes
    /// (Spur „kader“, R2-11) gilt wie eine Sperre bis zum Saisonwechsel. null = Verhandlung möglich.
    /// </summary>
    public static NegotiationResult? Availability(GameState game, Player p)
    {
        if (p.OnLoan)
            return NegotiationResult.Loaned;
        if (p.Notice == ContractNotice.Retiring)
            return NegotiationResult.Retiring;
        if (p.Notice == ContractNotice.Refused || p.NegotiationLock > 0 && !game.AfterSeasonEnd)
            return NegotiationResult.NotWilling;
        return null;
    }

    /// <summary>
    /// Gehaltsforderung in DM, die nach Eingabe der Laufzeit im Feld „Gehalt / Monat“ steht (<see cref="ContractRules.DemandThirds"/>),
    /// oder null, wenn das Original das Feld nicht füllt (Level 5: DGROUP 0xED8 = 5 − Level = 0, Datei 0x4A9DB). Rein.
    /// </summary>
    public static int? DemandAfterYears(GameState game, Manager manager, Player p, int years) =>
        DemandAfterYears(game, manager, p, years, p.MonthlySalary);

    /// <summary>Wie oben mit ausdrücklichem bisherigem Gehalt (Kauf: Vertrag des neuen Kaderplatzes, <see cref="TransferPurchase"/>).</summary>
    public static int? DemandAfterYears(GameState game, Manager manager, Player p, int years, int currentSalary) =>
        manager.Level >= 5 ? null : ComputedDemand(game, manager, p, years, currentSalary);

    /// <summary>
    /// Forderung in DM unabhängig vom Level. [ORIGINAL] 0x4A81B–0x4A9A1 rechnet ohne Levelabfrage (einzige Abfrage von
    /// DGROUP 0xED8 in 0x4A9DB, nur für das Füllen des Feldes; R8W3I-12, #272). [APPROX] R8W3I-08: Das Original rechnet und
    /// speichert Drittel-DM; das Remake speichert ganze DM und rundet die Forderung (und die Vergleichsforderung aus dem
    /// bisherigen Gehalt) ab – bis 2/3 DM je Monat.
    /// </summary>
    private static int ComputedDemand(GameState game, Manager manager, Player p, int years, int currentSalary)
    {
        var club = game.Clubs[manager.ClubIndex];
        var value = game.PlayerValueOf(p, club, PlayerValueKind.Salary, NoRandom, manager.Level);
        // [ORIGINAL] Alter aus Kaderplatz 3 des Managers (0x4A93F); [APPROX] Reihenfolge des Kaders wie im Remake.
        var age = club.Players.Count > 3 ? club.Players[3].Age : p.Age;
        return ContractRules.DemandThirds(value, game.ContractTurnFactor, game.ContractSeasonRemaining, years,
            game.AfterSeasonEnd, age, currentSalary * 3) / 3;
    }

    private static readonly Random NoRandom = new NoDrawRandom();

    /// <summary>
    /// „Mach' ich!“ im Verhandlungsdialog. [ORIGINAL] Datei 0x4AB66–0x4AD7A: mehr als 4 Jahre → <see cref="NegotiationResult.TooLong"/>;
    /// steht die vom Dialog gefüllte Forderung im Gehaltsfeld (<paramref name="demandFilled"/>, nur nach Eingabe der Laufzeit
    /// und ohne spätere Gehaltseingabe) → angenommen; ebenso ohne Eingaben bzw. bei gleicher Laufzeit und mindestens gleichem
    /// Gehalt; kürzere Laufzeit → <see cref="NegotiationResult.TooShort"/>; sonst entscheidet 0x49A3B
    /// (<see cref="ContractRules.Accepts"/>), abgelehnt → <see cref="NegotiationResult.NotInterested"/>. Scheitert die
    /// Verhandlung, ist der Spieler random(10, 18) Tage gesperrt (0x4AD3E). Zufall aus <see cref="GameState.ContractRandom"/>.
    /// </summary>
    /// <param name="years">eingegebene Laufzeit, 0 = nicht eingegeben.</param>
    /// <param name="salary">Gehalt im Feld in DM, 0 = nicht eingegeben.</param>
    public static NegotiationResult Negotiate(GameState game, Manager manager, Player p, int years, int salary, bool insured,
        bool demandFilled)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(p);
        if (salary < 0 || years < 0)
            return NegotiationResult.Unfair;
        // Eingaben setzen im Original das Anklicken der Felder voraus, das Leihe, Ruhestand und Sperre schon abweist.
        if ((years != 0 || salary != 0) && Availability(game, p) is { } refusal)
            return refusal;
        var (result, newYears, newSalary) = NegotiateTerms(game, manager, p, CurrentYears(p), p.MonthlySalary, years, salary,
            demandFilled);
        // Prämie vor dem neuen Gehalt: das Original rechnet sie beim Umschalten im Fenster (0x4AA9C), vor „Mach' ich!“.
        ApplyInsurance(game, manager, p, insured, result == NegotiationResult.Accepted, lockOnFailure: true);
        if (result == NegotiationResult.Accepted)
        {
            p.ContractYears = newYears;
            p.MonthlySalary = newSalary;
        }
        else
            p.NegotiationLock = ContractRules.LockDays(game.ContractRandom);
        return result;
    }

    /// <summary>
    /// Verhandlung ohne Prüfung der Verfügbarkeit gegen einen gegebenen Vertrag (<paramref name="current"/> Jahre,
    /// <paramref name="currentSalary"/> DM); ändert den Spieler nicht. Liefert den neuen Vertrag, wenn er steht. Gemeinsamer Kern
    /// von <see cref="Negotiate"/> und dem Kauf (<see cref="TransferMarket.Sign"/>, Datei 0x4786A ruft denselben Dialog).
    /// </summary>
    internal static (NegotiationResult Result, int Years, int Salary) NegotiateTerms(GameState game, Manager manager, Player p,
        int current, int currentSalary, int years, int salary, bool demandFilled)
    {
        if (salary < 0 || years < 0)
            return (NegotiationResult.Unfair, 0, 0);
        // [ORIGINAL] 0x4A9A1 (R8W3I-02 #270, R8W3I-12 #272): Nach dem Saisonende schreibt der Dialog Forderung und Laufzeit
        // auf jedem Level sofort in den Kaderplatz; „Mach' ich!“ vergleicht danach mit diesen Werten (0x4ABD1–0x4AC75).
        // [ORIGINAL] 0x4AD3E→0x4AD54: Bei Ablehnung und Abbruch stellt das Original die beim Öffnen gesicherten Werte zurück
        // (0x4A239–0x4A25F) – das Remake ändert den Spieler dann gar nicht.
        if (game.AfterSeasonEnd && years != 0)
            (current, currentSalary) = (years, ComputedDemand(game, manager, p, years, currentSalary));
        var result = Decide(game, manager, p, current, currentSalary, years, salary, demandFilled);
        return (result, years == 0 ? current : years, salary == 0 ? currentSalary : salary);
    }

    /// <summary>
    /// Laufzeit, mit der der Dialog rechnet: die bisherige, bei einem Verlängerungsangebot des Spielers die angebotene.
    /// [ORIGINAL] Kaderplatz +0x18 &gt; 99 (Angebot, Tagesablauf) → beim Öffnen +0x0B = +0x18 − 100 (Datei 0x4A3AF); „Mach'
    /// ich!“ ohne Eingaben nimmt das Angebot damit zum bisherigen Gehalt an.
    /// </summary>
    /// <remarks>[APPROX] Das Remake behält die Ankündigung bis zum Saisonwechsel (Spur „kader“); das Angebot gilt nur, solange
    /// der Spieler im letzten Vertragsjahr ist. Am Saisonende (Laufzeit 0, Angebot 1 Jahr) ist es das Angebot +0x18 = 101 des
    /// Originals (Datei 0x27D15, <see cref="GameState.IsRenewalPending"/>).</remarks>
    public static int CurrentYears(Player p) =>
        p.Notice == ContractNotice.Offered && p.ContractYears <= 1 ? p.NoticeYears : p.ContractYears;

    private static NegotiationResult Decide(GameState game, Manager manager, Player p, int current, int currentSalary, int years,
        int salary, bool demandFilled)
    {
        if (years > ContractRules.MaxYears)
            return NegotiationResult.TooLong;
        var accept = demandFilled
                     || years == 0 && (salary == 0 || current == 0)
                     || years == current && salary >= currentSalary;
        var y = years == 0 ? current : years;
        var s = salary == 0 ? currentSalary : salary;
        if (y < current)
            return NegotiationResult.TooShort;
        if (accept)
            return NegotiationResult.Accepted;
        var club = game.Clubs[manager.ClubIndex];
        var value = game.PlayerValueOf(p, club, PlayerValueKind.Salary, NoRandom, manager.Level);
        return ContractRules.Accepts(current, currentSalary * 3, y, s * 3, value, game.ContractSeasonRemaining,
            game.AfterSeasonEnd, p.Age, game.ContractRandom)
            ? NegotiationResult.Accepted
            : NegotiationResult.NotInterested;
    }

    /// <summary>Meldung zum Ergebnis einer Verhandlung mit diesem Spieler (Zeilen wie im Original, 0x120:0x4D).</summary>
    public static string[] Message(NegotiationResult result, Player p) => result switch
    {
        NegotiationResult.NotWilling => [p.Name, "ist im Moment", "nicht verhandlungs-", "bereit."],
        NegotiationResult.Retiring => [p.Name, "wird sich mit", "Ablauf des Vertrages", "zur Ruhe setzen."],
        NegotiationResult.Loaned => [p.Name, "ist leider von", p.Loan?.Origin.Name ?? "", "nur ausgeliehen."],
        NegotiationResult.NotInterested => [p.Name, "ist nicht an", "Ihrem Angebot", "interessiert."],
        NegotiationResult.TooShort => ["So dumm", "ist", p.Name, "leider nicht..."],
        NegotiationResult.TooLong => ["SOOOO lange", "verpflichtet", "sich kein", "Eishockey-Spieler !"],
        NegotiationResult.Accepted => [],
        _ => ["Dieser Betrag ist", "nicht erlaubt (unfair) !"],
    };

    /// <summary>Spielerwert Art 1 zieht keinen Zufall; jeder Zug wäre ein Fehler.</summary>
    private sealed class NoDrawRandom : Random
    {
        public override int Next(int minValue, int maxValue) =>
            throw new InvalidOperationException("Das Gehalt nach 0x118:0x70 zieht keinen Zufall.");

        public override int Next() => throw new InvalidOperationException("Das Gehalt nach 0x118:0x70 zieht keinen Zufall.");

        public override int Next(int maxValue) =>
            throw new InvalidOperationException("Das Gehalt nach 0x118:0x70 zieht keinen Zufall.");

        public override double NextDouble() =>
            throw new InvalidOperationException("Das Gehalt nach 0x118:0x70 zieht keinen Zufall.");
    }
}
