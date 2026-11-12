namespace EishockeyManager.Tools;

/// <summary>Klasse eines Feldes im Abgleich (R15-D2 = A, Feldklassen).</summary>
public enum HokCompareClass
{
    /// <summary>Deterministisch: muss nach dem Nachspielen exakt gleich sein.</summary>
    Exact,

    /// <summary>Zufallsabhängig: höchstens ein Bereich (<see cref="HokCompareRule.Tolerance"/>) oder nur über Invarianten geprüft.</summary>
    Random,

    /// <summary>Im Paar durch einen bekannten Nutzereingriff geändert (README der Spielstände): nicht verglichen.</summary>
    Intervention,

    /// <summary>Nicht im HOK (Feldkarte N/U) oder Zustand des Remakes: weder abgedruckt noch verglichen.</summary>
    NotInHok,

    /// <summary>Kein Eintrag in <see cref="HokCompareClasses"/>: wird als Abweichung gemeldet, bis es eingeordnet ist.</summary>
    Unclassified,
}

/// <summary>
/// Regel für ein Schemafeld. <paramref name="Tolerance"/>/<paramref name="Relative"/>: zulässiger Abstand zum Original
/// (der größere zählt); ohne beide prüft <see cref="HokCompareClass.Random"/> nichts außer dem Vorhandensein.
/// <paramref name="SeasonBound"/>: deterministisch innerhalb einer Saison, nach einem Saisonwechsel zufallsabhängig (Ligazusammensetzung
/// folgt aus den Ergebnissen). <paramref name="FromStart"/>: <paramref name="Relative"/> bezieht sich auf die Veränderung des Originals
/// seit Stand a (|b − a|), nicht auf den Stand b; ohne Wert von a gilt nur <paramref name="Tolerance"/>.
/// <paramref name="SeasonTolerance"/>: <paramref name="Tolerance"/> in Paaren über einen Saisonwechsel (größere Zufallsschritte).
/// </summary>
public sealed record HokCompareRule(HokCompareClass Class, string Note = "", double? Tolerance = null, double? Relative = null,
    bool SeasonBound = false, bool FromStart = false, double? SeasonTolerance = null)
{
    /// <summary>Klasse im Paar: <see cref="SeasonBound"/> wird nach einem Saisonwechsel zufallsabhängig.</summary>
    public HokCompareClass ClassFor(bool crossesSeason) =>
        SeasonBound && crossesSeason && Class == HokCompareClass.Exact ? HokCompareClass.Random : Class;

    /// <summary>Regel im Paar: über einen Saisonwechsel gilt <see cref="SeasonTolerance"/> statt <see cref="Tolerance"/>.</summary>
    public HokCompareRule ForPair(bool crossesSeason) =>
        crossesSeason && SeasonTolerance is { } season ? this with { Tolerance = season } : this;
}

/// <summary>
/// Feldklassen des Abgleichs in einer Datei (Spur abgleich-original, Teil 1; R15-D2 = A). Schlüssel ist das Schemafeld
/// <c>Typ.Feld</c> aus <c>SaveGame.SchemaDescription()</c> bzw. der Feldkarte in <c>docs/formate/spielstand-original.md</c>.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><b>NotInHok</b>: alle Felder mit Status N oder U der Feldkarte (Stand Schema 47) – der Import setzt dort Vorgaben, ein
///   Vergleich sagt nichts. Ausnahme Spieler-ID: Status N, die IDs vergibt der Import neu.</item>
/// <item><b>Exact</b>: Status B/A und ohne Zufallszug zwischen zwei Halten ohne Eingriff (Datum, Zähler, Spielplan, Verträge,
///   Alter, Bau, Einstellungen, Kredit, Sponsorverträge). <c>SeasonBound</c>, wo die Ligazusammensetzung eingeht.</item>
/// <item><b>Random</b>: Ergebnisse und alles, was aus ihnen folgt (Tabelle, Statistik, Zuschauer, Geld), Spielerwerte mit
///   Zufallsanteil (Form, Kondition, Verletzung), Markt, Pool, Aktienkurse, Nachrichten, Angebote. Bereiche sind erste
///   Schätzungen `[APPROX]` und werden an den Paaren (Teil 2) nachgezogen.</item>
/// </list>
/// Nutzereingriffe je Paar stehen in <see cref="Interventions"/> (Pfadmuster mit <c>*</c>).
/// </remarks>
public static class HokCompareClasses
{
    private static readonly HokCompareRule Exact = new(HokCompareClass.Exact);
    private static readonly HokCompareRule InSeason = new(HokCompareClass.Exact, "Ligazusammensetzung", SeasonBound: true);
    private static readonly HokCompareRule Random = new(HokCompareClass.Random);
    private static readonly HokCompareRule NotInHok = new(HokCompareClass.NotInHok, "Feldkarte N/U");
    private static readonly HokCompareRule Unclassified = new(HokCompareClass.Unclassified, "nicht eingeordnet");

    /// <summary>
    /// [APPROX] Mittelwerte des Pools (200 Spieler): ±8 Punkte. Innerhalb einer Saison über die Saaten 1993, 4711, 1, 2, 3, 77 höchstens
    /// 5,7 Punkte Abstand (FILE02→03 Saat 3, Belastung; Review P3-2); die Abweichungen nach dem Saisonwechsel (#731) liegen weit darüber.
    /// </summary>
    private static readonly HokCompareRule PoolMean = new(HokCompareClass.Random, "Bereich", 8);

    /// <summary>
    /// [APPROX] Kontostand: Abstand zum Original höchstens 50.000 DM oder 10 % der Veränderung des Originals seit Stand a (der größere
    /// Wert). Gemessen über elf Paare und sechs Saaten: höchstens 33.000 DM bzw. 9 % (ohne die Befunde #732, #734, #737). Die Gegenprobe
    /// „Gehälter fehlen“ (FILE22→23, rund 790.000 DM) ist rot (Review P2-1); der frühere Bereich ±25 % des Kontostands b war es nicht.
    /// </summary>
    private static readonly HokCompareRule Money = new(HokCompareClass.Random, "Bereich seit a", 50_000, 0.10, FromStart: true);

    /// <summary>
    /// [APPROX] Kondition/Technik der Teamwerte (Abweichung vom Datenbankwert): ±15 innerhalb einer Saison, ±35 über einen
    /// Saisonwechsel. Die große Wanderung (<c>MatchEngine.DriftTeamValues</c>, Modus 10) zieht je Verein einen eigenen Schritt
    /// (−6 … +12, je Wert ±1) und begrenzt auf die Liga, dazu die Wanderungen der Endrunde; zwei unabhängige Züge liegen bis 31
    /// auseinander (FILE13→14, sechs Saaten, nach #729 sichtbar). Eine Verwechslung absolut/Abweichung (#729) liegt bei 40–100.
    /// </summary>
    private static readonly HokCompareRule TeamStrength = new(HokCompareClass.Random, "Bereich", 15, SeasonTolerance: 35);

    private static HokCompareRule Range(double tolerance, double? relative = null) => new(HokCompareClass.Random, "Bereich", tolerance, relative);

    /// <summary>Regel des Schemafelds; Hilfsfelder ohne Eintrag erben nichts und gelten als nicht eingeordnet.</summary>
    public static HokCompareRule Of(string field)
    {
        if (Rules.TryGetValue(field, out var rule))
            return rule;
        if (field.StartsWith("Invariante.PoolMittel", StringComparison.Ordinal))
            return PoolMean;
        if (field.StartsWith("Invariante.", StringComparison.Ordinal))
            return Exact;
        return Unclassified;
    }

    /// <summary>Alle eingeordneten Schemafelder (für den Vollständigkeitstest).</summary>
    public static IReadOnlyDictionary<string, HokCompareRule> All => Rules;

    private static readonly Dictionary<string, HokCompareRule> Rules = Build();

    private static Dictionary<string, HokCompareRule> Build()
    {
        var rules = new Dictionary<string, HokCompareRule>(StringComparer.Ordinal);
        void Set(HokCompareRule rule, params string[] fields)
        {
            foreach (var f in fields)
                rules.Add(f, rule);
        }

        // ---- Feldkarte N/U (Stand Schema 47) und Remake-eigener Zustand ----------------------------------------------
        Set(NotInHok,
            "GameState.CurrentRecord", "GameState.History", "GameState.NationalBookings", "GameState.NationalBudget",
            "GameState.Random", "GameState.RetiredPlayers", "GameState._contractRandom", "GameState._contractTurnFactor",
            "GameState._cupRandom", "GameState._eternal", "GameState._financeRandom", "GameState._germanyRatings",
            "GameState._managerBalance", "GameState._managerHistory", "GameState._nationalForm", "GameState._nextLeagueSlots",
            "GameState._readyManagers", "GameState._seasonEndYouthPending", "GameState._trendRandom", "GameState._turnRandom",
            "GameState._worldCupRankings",
            "GameState.NextPlayerId",
            // Merker des Remakes „Saisonstatistik schon gebucht“: das HOK führt keine Saisonhistorie (History N), der Import setzt −1.
            "GameState.StatisticsRecordedSeason",
            "GameSetup.DebCup", "GameOptions.ManagerDayMinuteLoop", "ManagerSetup.Head",
            "Manager.Dismissed", "Manager.Head",
            "ManagerFinance.Book", "ManagerFinance.LastHomeGame", "ManagerFinance.ManagerLoanAccrualThirds",
            "FinanceBook._current", "FinanceBook._previous", "Stadium._offended",
            "TrainingPlan.PriceTierSet", "TrainingPlan._priceTier",
            "Message.Amount", "Message.Borrower", "Message.Contract", "Message.Done",
            "Club.ReferenceRating", "Tactics.Goalie",
            "Player.Caps", "Player.Id", "Player.SuspendedGames",
            "PlayerLoan.ContractYears", "PlayerLoan.Insured", "PlayerLoan.MonthlySalary", "PlayerLoan.Origin",
            "PoolDatabaseName.Club", "PoolDatabaseName.Name", "PoolDatabaseName.Slot",
            "TransferBid.Amount", "TransferBid.Buyer", "TransferBid.Listing",
            "TransferListing.ListedDay", "TransferListing.Seller",
            "TransferMarket._bids", "TransferMarket._databaseNames", "TransferMarket._freeAgents",
            "TransferMarket._poolTurnBundesliga", "TransferMarket._poolTurnOberliga", "TransferMarket._poolTurnZweiteLiga",
            "WorldCup.Winner", "WorldCup._knockoutMarks", "WorldCup._order", "WorldCup._slots",
            "NationalCoach._turnOpen", "FriendlyMatch.Bonus",
            "NationalBooking.Amount", "NationalBooking.Kind", "NationalBooking.Season",
            "SeasonRecord.FinalTables", "SeasonRecord.LeftOberliga", "SeasonRecord.Managers", "SeasonRecord.PromotedInto",
            "SeasonRecord.RelegatedInto", "SeasonRecord.Season", "SeasonRecord.Titles",
            "ManagerSeasonResult.Club", "ManagerSeasonResult.Events", "ManagerSeasonResult.League", "ManagerSeasonResult.Manager",
            "ManagerSeasonResult.Premium", "ManagerSeasonResult.Rank",
            "ManagerSeasonHistory.Club", "ManagerSeasonHistory.Deb", "ManagerSeasonHistory.DebRound", "ManagerSeasonHistory.Europe",
            "ManagerSeasonHistory.EuropeFinalRound", "ManagerSeasonHistory.League", "ManagerSeasonHistory.LeagueOutcome",
            "ManagerSeasonHistory.Rank", "ManagerSeasonHistory.Season",
            "EternalEntry.Championships", "EternalEntry.Club", "EternalEntry.CupWins", "EternalEntry.EuropeanCupWins",
            "EternalEntry.Points");

        // ---- GameState --------------------------------------------------------------------------------------------
        Set(Exact,
            "GameState.Clubs", "GameState.Competitions", "GameState.Managers", "GameState.NationalCoach", "GameState.Setup",
            "GameState.TransferMarket", "GameState.CurrentDay", "GameState.CurrentManager", "GameState.DayOpened",
            "GameState.FinanceDay", "GameState.LoadCounterOffset", "GameState.ManagerLoans", "GameState.Phase",
            "GameState.PlayedMatchDays", "GameState.RankSlotsRecorded", "GameState.Season",
            "GameState.TrainedUntil", "GameState._nationalAutoLineupSaved", "GameState._nationalTurn",
            "GameState._vacantNationalTactics", "GameState._stocks");
        Set(InSeason, "GameState.Schedules", "GameState._leagueSlots", "GameState.Results");
        Set(Random, "GameState.PlayoffResults", "GameState.Playoffs", "GameState.RelegationGroups", "GameState._headToHead",
            "GameState._teamRecordOpponents", "GameState._teamRecords");
        Set(Range(50_000, 0.5), "GameState.NationalAccount");

        // ---- GameSetup, GameOptions, ManagerSetup ------------------------------------------------------------------
        Set(Exact, "GameSetup.ClubCompetitions", "GameSetup.Duration", "GameSetup._startLeague", "GameSetup.EuropeAndWorldCup",
            "GameSetup.Managers", "GameSetup.Options", "GameOptions.LiveMode", "GameOptions.MatchSpeed", "GameOptions.Reports",
            "GameOptions.ShowDebCup", "GameOptions.ShowEuropeanCup", "GameOptions.ShowWorldCup", "ManagerSetup.ClubIndex",
            "ManagerSetup.Level", "ManagerSetup.Name", "ManagerSetup.StartLeague", "ManagerSetup.StartStrength");

        // ---- Manager, ManagerFinance -------------------------------------------------------------------------------
        Set(Exact, "Manager.ClubIndex", "Manager.FinanceData", "Manager.Level", "Manager.Name", "Manager.Training",
            "ManagerFinance.Bank", "ManagerFinance.Holdings", "ManagerFinance.Sponsoring", "ManagerFinance.Stadium",
            "ManagerFinance.TalentSearch", "ManagerFinance.Youth", "BankAccount.Credit", "BankAccount.Savings",
            "ManagerLoan.Amount", "ManagerLoan.Borrower", "ManagerLoan.Lender",
            "Stadium.Seats", "Stadium.SeatsTarget", "Stadium.Standing", "Stadium.StandingTarget", "Stadium.TicketPrice",
            "Stadium._levels", "Stadium._targets", "Stadium._weeksLeft",
            "YouthTeam.Funding", "YouthTeam.Kind", "TrainingPlan.PriceIndex", "TrainingPlan._units", "TalentSearch.TicksLeft",
            "SponsorContract.Boards", "SponsorContract.Kind", "SponsorContract.MonthlyAmount", "SponsorContract.MonthsLeft",
            "SponsorContract.OptionMonths", "SponsorContract.SignedRating", "SponsorContract.Sponsor",
            "StockHolding.Count", "StockHolding.PurchaseCost", "Stock.Name");
        Set(InSeason, "ManagerFinance.CostLeague", "YouthTeam.League");
        Set(Money, "Manager.Money");
        Set(Random, "Manager.PowerPlayCounts", "Manager.SeriesRecords", "Manager._newMessage",
            "ManagerFinance.Inbox", "ManagerFinance.InterestAccrualThirds", "ManagerFinance.SponsorRating",
            "ManagerFinance.TalentPrice", "ManagerFinance._homeStatistics",
            "StadiumStatistics.Games", "StadiumStatistics.Minimum", "StadiumStatistics.MinimumOpponent", "StadiumStatistics.Record",
            "StadiumStatistics.RecordOpponent", "StadiumStatistics.Total", "StadiumStatistics._curve",
            "YouthTeam.Rank", "YouthTeam.Strength", "Sponsoring.Contracts", "Sponsoring.Offers",
            "SponsorOffer.Boards", "SponsorOffer.Kind", "SponsorOffer.MonthlyAmount", "SponsorOffer.Months",
            "SponsorOffer.OptionMonths", "SponsorOffer.Rating", "SponsorOffer.Sponsor",
            "StockMarket.Stocks", "Stock.Base", "Stock.Countdown", "Stock.Direction", "Stock.History");
        Set(Random, "Stock.Price");

        // ---- Nachrichten -------------------------------------------------------------------------------------------
        Set(Random, "Message.Day", "Message.DaysLeft", "Message.Kind", "Message.Lines", "Message.Player", "Message.Season");

        // ---- Vereine, Teamwerte, Taktik, Aufstellung ---------------------------------------------------------------
        Set(Exact, "Club.Index", "Club.LogoGraphic", "Club.Name", "Club.Strength", "Club.Tactics", "Club._autoLineup",
            "Club._dressedByHand", "Club.Players", "Club.TeamValues",
            "Tactics.AttackMode", "Tactics.EffortMode", "Tactics.LineChange", "Tactics._attack", "Tactics._effort",
            "Tactics._minutes");
        // Anzeige „AUTOM.“: Zeile der Automatik gegen den letzten Gegner (Stärken aus den zufallsabhängigen Teamwerten).
        Set(Random, "Tactics._shownAutoAttack", "Tactics._shownAutoEffort");
        // #772 (Spur abgleich-rest): Vereinsfelder 0x26 und 0x2C Bits 4–6 nach dem letzten Spiel; der Import liest sie nicht.
        Set(Random, "Tactics._matchAttack", "Tactics._matchEffort");
        Set(InSeason, "Club.League");
        Set(Random, "Club.ForfeitsMatch", "Club.LeaguePenaltyMinutes", "Club.Lineup", "Club.SeriesCurrent", "Club._dressed",
            "Club._rankSlots", "Club._tableRank", "Lineup._defenders", "Lineup._forwards", "Lineup._goalkeeper");
        Set(Range(15), "ClubTeamValues.AttackForm", "ClubTeamValues.DayForm", "ClubTeamValues.DefenseForm");
        Set(TeamStrength, "ClubTeamValues.AttackCondition", "ClubTeamValues.AttackTechnique", "ClubTeamValues.DefenseCondition",
            "ClubTeamValues.DefenseTechnique");

        // ---- Spieler (nur Managerkader, Markt, Pool, Nationalkader) -------------------------------------------------
        Set(Exact, "Player.Age", "Player.ContractYears", "Player.InsuranceLocked",
            "Player.InsurancePremium", "Player.IsForeigner", "Player.Loan", "Player.MonthlySalary", "Player.Name",
            "Player.NegotiationLock", "Player.Notice", "Player.NoticeYears", "Player.Number", "Player.OnLoan",
            "Player.PurchasePrice", "Player.Role");
        Set(Random, "Player.Assists", "Player.ClubGamesCarry", "Player.ClubCupGamesCarry", "Player.ClubEuropeGamesCarry",
            "Player.ClubGoalsCarry", "Player.ClubCupGoalsCarry", "Player.ClubEuropeGoalsCarry", "Player.CupGames",
            "Player.EuropeGames", "Player.CupGoals", "Player.EuropeGoals", "Player.Games", "Player.GoalieMinutes", "Player.Goals",
            "Player.GoalsAgainst", "Player.InjuredGames", "Player.InjuryKind", "Player.Penalties2", "Player.Penalties5", "Player.PoolForm",
            "Player.Trend", "Player.TrendControl", "Player.TrendScore", "Player.UnsolicitedOffer");
        Set(Range(15), "Player.Condition", "Player.Form");
        Set(Range(10), "Player.Technique");
        Set(Range(2), "Player.Talent");
        Set(Range(5), "Player.HiddenSkill1", "Player.HiddenSkill2");
        Set(Range(20), "Player.Strain");

        // ---- Pool, Transfermarkt -----------------------------------------------------------------------------------
        Set(Exact, "TransferMarket.UpdatedDay");
        Set(Random, "TransferMarket._listings", "TransferMarket._pool", "PoolSlot.Club", "PoolSlot.Index", "PoolSlot.Owner",
            "PoolSlot.Manager", "PoolSlot.Player", "TransferListing.From", "TransferListing.FromPool", "TransferListing.IsFreeAgent",
            "TransferListing.Player", "TransferListing.Price", "TransferListing.RejectedBy");

        // ---- Liga, Pokale, Nationalteam ----------------------------------------------------------------------------
        Set(InSeason, "MatchDay.Matches", "MatchDay.Number", "MatchResult.Away", "MatchResult.Home", "MatchResult.League",
            "MatchResult.MatchDay");
        Set(Random with { Note = "Ergebnis" }, "MatchResult.AwayGoals", "MatchResult.HomeGoals");
        Set(Random, "MatchBalance.Draws", "MatchBalance.GoalsAgainst", "MatchBalance.GoalsFor", "MatchBalance.Losses",
            "MatchBalance.PointsMinus", "MatchBalance.PointsPlus", "MatchBalance.Wins",
            "KnockoutMatch.Away", "KnockoutMatch.AwayGoals", "KnockoutMatch.Day", "KnockoutMatch.Home", "KnockoutMatch.HomeGoals",
            "KnockoutMatch.PenaltyWinner", "CupTie.First", "CupTie.Games", "CupTie.Marks", "CupTie.Second",
            "CupGroup.Number", "CupGroup.Results", "CupGroup.Teams", "DebCup.Winner", "DebCup._draw", "DebCup._rounds",
            "EuropeanCup.FinalRound", "EuropeanCup.Winner", "EuropeanCup._draw", "EuropeanCup._groups",
            "WorldCup.Ranking", "WorldCup._groups", "WorldCup._knockout",
            "Playoffs.Seeds", "Playoffs._rounds", "PlayoffSeries.Games", "PlayoffSeries.HomeClub", "PlayoffSeries.AwayClub",
            "RelegationGroup.Clubs", "RelegationGroup.PlayedRounds", "RelegationGroup.Results", "RelegationGroup.Rounds",
            "RelegationGroup.Upper", "NationalCoach._dressed", "NationalCoach._friendlies",
            "FriendlyMatch.Away", "FriendlyMatch.AwayGoals", "FriendlyMatch.Home", "FriendlyMatch.HomeGoals");
        Set(InSeason, "EuropeanCup.GermanEntrants");
        Set(Exact, "NationalCoach.Formation", "NationalCoach._autoLineup", "NationalCoach._autoLineupSaved", "NationalCoach.Manager",
            "NationalCoach.Nation", "NationalCoach.Tactics", "NationalCoach._squad", "NationalSquadMember.Club",
            "NationalSquadMember.Player");
        return rules;
    }

    /// <summary>
    /// Bekannte Nutzereingriffe je Paar (README der Spielstände im Referenz-Repo, <c>original/spielstaende/README.md</c>):
    /// Pfadmuster des Abdrucks, <c>*</c> = beliebige Zeichen. Schlüssel „a→b“ mit Dateinamen ohne Endung.
    /// </summary>
    public static IReadOnlyDictionary<string, string[]> Interventions { get; } = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        // FILE13→14 (#734, R15-E6 = A): Sitzungszustand. Nach dem Laden zeigte die Aufstellung der damaligen DOSBox-Sitzung
        // (Runde 10) nur 5 Eingesetzte (`file13_2`), im DEB-Pokal standen weniger als 9 Spieler (README); frisch geladen stellt
        // das Original voll auf (Probe Nutzer 11.11.2026, `file13_r15_1…3`, `file12_r15_1…3`: „Eingesetzt: 20“, AUTOM.), wie
        // 0x426BC mit den Daten von FILE13. [ORIGINAL] Ohne Nutzerhandlung schreibt em.exe in DGROUP 0x375/0x376 keine 0
        // (Aufstellung von Hand Datei 0x40ADE/0x40DE4, Taktikknopf 0x47F4E/0x49058; Laden/Sichern kopieren den Block
        // 0x5C3E3/0x5B9DD; #733). Folgen der Handaufstellung: Konto (Strafen 0:2 und 200.000 DM je Spiel mit weniger als neun,
        // Platz 12; mit dieser Aufstellung im Bereich, AbgleichFixMoneyTests), Automatik, Teamwerte und verdeckte Werte des
        // Managervereins. Die Belastung der Spieler bleibt verglichen (#730, Spur abgleich-rest).
        ["FILE13→FILE14"] = ["Managers[0].Money", "Clubs[6 KREFELDER EV]._autoLineup", "Clubs[6 KREFELDER EV].TeamValues.*",
            "Clubs[6 KREFELDER EV].Players[*].HiddenSkill*"],
        // FILE46: „Saisonende und Vertragsverhandlung am 30.04.1994 mit Nichtverlängerung von KJELLBERG“.
        ["FILE44→FILE46"] = ["Clubs[*].Players[KJELLBERG]*", "Clubs[*].Players.Count", "Clubs[*].Lineup*"],
        // FILE48: „FILE18 geladen (nicht FILE47), Kader geleert, einen Tag weitergespielt“ (Block 0375 Byte 10/11 = 0).
        ["FILE18→FILE48"] = ["NationalCoach._squad*", "NationalCoach._autoLineup*", "NationalCoach._dressed*", "_nationalAutoLineupSaved",
            "Clubs[56 DEUTSCHLAND].TeamValues.*"], // leerer Kader: Länderwerte der Reihen 0 (0x952C mit Manager 5), alle sechs Werte
    };

    /// <summary>Trifft der Pfad eines der Muster (<c>*</c> = beliebig viele Zeichen)?</summary>
    public static bool Matches(string path, IEnumerable<string> patterns) =>
        patterns.Any(p => System.Text.RegularExpressions.Regex.IsMatch(path,
            "^" + System.Text.RegularExpressions.Regex.Escape(p).Replace(@"\*", ".*", StringComparison.Ordinal) + "$"));
}
