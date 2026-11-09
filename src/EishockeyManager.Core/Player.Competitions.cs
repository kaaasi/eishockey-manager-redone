namespace EishockeyManager.Core;

/// <summary>
/// Wettbewerb der Spielerzähler (A135, Runde 14, <c>docs/analyse/re-runde-14.md</c>). [ORIGINAL] Index DGROUP 0xD0BF: 0 setzen
/// die Liga-Spieltage (Datei 0x35EBA), die Play-offs und Aufstiegsrunden (Datei 0x30EA6, 0x31A1E), 1 der DEB-Pokal (Datei
/// 0x35E72), 2 der Europapokal (Datei 0x313C5). Die Spielabrechnung 0x544C0 wählt das Feld nach ihrem Argument (1 = Pokal,
/// 8 = Europapokal, sonst Liga; Datei 0x5494C–0x5499E).
/// </summary>
public enum CompetitionSlot
{
    League = 0,
    Cup = 1,
    Europe = 2,
}

/// <summary>Zuordnung der Wettbewerbe der Spielberechnung zu den Zählern.</summary>
public static class CompetitionSlots
{
    /// <summary>Anzahl der Zähler je Spieler (Liga, DEB-Pokal, Europapokal).</summary>
    public const int Count = 3;

    /// <summary>
    /// Zähler eines Wettbewerbs (siehe <see cref="CompetitionSlot"/>). [APPROX] Die WM des Bundestrainers zählt wie bisher
    /// in der Liga; welches Feld das Original bei Länderspielen bucht, ist nicht gelesen.
    /// </summary>
    public static CompetitionSlot Of(MatchCompetition competition) => competition switch
    {
        MatchCompetition.DebCup => CompetitionSlot.Cup,
        MatchCompetition.EuropeanCup => CompetitionSlot.Europe,
        _ => CompetitionSlot.League,
    };
}

/// <summary>
/// Tore und Spiele je Wettbewerb (A135). [ORIGINAL] Kaderplatz Saisontore +0x03/+0x04/+0x05 (Byte), Saisonspiele
/// +0x06/+0x07/+0x08 (Byte), beim jetzigen Verein Spiele +0x1C/+0x1E/+0x20 und Tore +0x22/+0x24/+0x26 (Wörter); Beleg FILE38 →
/// FILE39 (Pokalspiel: +0x07/+0x1E + 1, +0x06 unverändert; Tor in +0x04/+0x24) und FILE39 → FILE40 (Liga: +0x06/+0x1C, Tore
/// +0x03/+0x22). Die Spalten SP und TO der Kaderliste zeigen die Summe (<c>file39_3</c>: SP 32 = 26 Liga + 6 Pokal).
/// Das Remake führt <see cref="Games"/> und <see cref="Goals"/> weiter als Summe; Liga = Summe − Pokal − Europapokal.
/// </summary>
public sealed partial class Player
{
    /// <summary>Saisonspiele im DEB-Pokal (Kaderplatz +0x07), Teil von <see cref="Games"/>.</summary>
    public int CupGames { get; set; }

    /// <summary>Saisonspiele im Europapokal (Kaderplatz +0x08), Teil von <see cref="Games"/>.</summary>
    public int EuropeGames { get; set; }

    /// <summary>Saisontore im DEB-Pokal (Kaderplatz +0x04), Teil von <see cref="Goals"/>.</summary>
    public int CupGoals { get; set; }

    /// <summary>Saisontore im Europapokal (Kaderplatz +0x05), Teil von <see cref="Goals"/>.</summary>
    public int EuropeGoals { get; set; }

    /// <summary>Übertrag der Pokalspiele beim jetzigen Verein (+0x1E) wie <see cref="ClubGamesCarry"/> für die Liga.</summary>
    public int ClubCupGamesCarry { get; set; }

    /// <summary>Übertrag der Europapokalspiele beim jetzigen Verein (+0x20) wie <see cref="ClubGamesCarry"/> für die Liga.</summary>
    public int ClubEuropeGamesCarry { get; set; }

    /// <summary>Übertrag der Ligatore beim jetzigen Verein (+0x22) aus abgelaufenen Saisons, abzüglich der Tore vor dem Eintritt.</summary>
    public int ClubGoalsCarry { get; set; }

    /// <summary>Übertrag der Pokaltore beim jetzigen Verein (+0x24).</summary>
    public int ClubCupGoalsCarry { get; set; }

    /// <summary>Übertrag der Europapokaltore beim jetzigen Verein (+0x26).</summary>
    public int ClubEuropeGoalsCarry { get; set; }

    /// <summary>Saisonspiele eines Wettbewerbs (+0x06 + Wettbewerb).</summary>
    public int SeasonGames(CompetitionSlot slot) => slot switch
    {
        CompetitionSlot.Cup => CupGames,
        CompetitionSlot.Europe => EuropeGames,
        _ => Math.Max(0, Games - CupGames - EuropeGames),
    };

    /// <summary>Saisontore eines Wettbewerbs (+0x03 + Wettbewerb).</summary>
    public int SeasonGoals(CompetitionSlot slot) => slot switch
    {
        CompetitionSlot.Cup => CupGoals,
        CompetitionSlot.Europe => EuropeGoals,
        _ => Math.Max(0, Goals - CupGoals - EuropeGoals),
    };

    /// <summary>Spiele beim jetzigen Verein in einem Wettbewerb (+0x1C + 2 · Wettbewerb).</summary>
    public int ClubGames(CompetitionSlot slot) => Math.Max(0, SeasonGames(slot) + slot switch
    {
        CompetitionSlot.Cup => ClubCupGamesCarry,
        CompetitionSlot.Europe => ClubEuropeGamesCarry,
        _ => ClubGamesCarry,
    });

    /// <summary>Tore beim jetzigen Verein in einem Wettbewerb (+0x22 + 2 · Wettbewerb).</summary>
    public int ClubGoals(CompetitionSlot slot) => Math.Max(0, SeasonGoals(slot) + slot switch
    {
        CompetitionSlot.Cup => ClubCupGoalsCarry,
        CompetitionSlot.Europe => ClubEuropeGoalsCarry,
        _ => ClubGoalsCarry,
    });

    /// <summary>Ein Spiel in einem Wettbewerb zählen (Summe und Wettbewerbsfeld).</summary>
    public void CountGame(CompetitionSlot slot)
    {
        Games++;
        if (slot == CompetitionSlot.Cup)
            CupGames++;
        else if (slot == CompetitionSlot.Europe)
            EuropeGames++;
    }

    /// <summary>Ein Tor in einem Wettbewerb zählen (Summe und Wettbewerbsfeld).</summary>
    public void CountGoal(CompetitionSlot slot)
    {
        Goals++;
        if (slot == CompetitionSlot.Cup)
            CupGoals++;
        else if (slot == CompetitionSlot.Europe)
            EuropeGoals++;
    }

    /// <summary>
    /// Neuer Kaderplatz: Zähler beim Verein auf 0 ([ORIGINAL] Datei 0x42CB1, FILE07), die Saisonzähler bleiben.
    /// </summary>
    internal void StartClubCounters()
    {
        ClubGamesCarry = -SeasonGames(CompetitionSlot.League);
        ClubCupGamesCarry = -CupGames;
        ClubEuropeGamesCarry = -EuropeGames;
        ClubGoalsCarry = -SeasonGoals(CompetitionSlot.League);
        ClubCupGoalsCarry = -CupGoals;
        ClubEuropeGoalsCarry = -EuropeGoals;
    }

    /// <summary>
    /// Saisonwechsel: Zähler beim Verein vor dem Zurücksetzen der Saisonzähler übertragen ([ORIGINAL] Datei 0x27A0C–0x27AA8
    /// löscht +0x03–+0x08, nicht +0x1C–+0x26; A088).
    /// </summary>
    internal void CarryClubCounters()
    {
        var games = new int[CompetitionSlots.Count];
        var goals = new int[CompetitionSlots.Count];
        for (var i = 0; i < CompetitionSlots.Count; i++)
        {
            games[i] = ClubGames((CompetitionSlot)i);
            goals[i] = ClubGoals((CompetitionSlot)i);
        }
        (ClubGamesCarry, ClubCupGamesCarry, ClubEuropeGamesCarry) = (games[0], games[1], games[2]);
        (ClubGoalsCarry, ClubCupGoalsCarry, ClubEuropeGoalsCarry) = (goals[0], goals[1], goals[2]);
    }

    /// <summary>Saisonzähler je Wettbewerb auf 0 (mit <see cref="Games"/>/<see cref="Goals"/>).</summary>
    internal void ResetCompetitionCounters() => CupGames = EuropeGames = CupGoals = EuropeGoals = 0;
}
