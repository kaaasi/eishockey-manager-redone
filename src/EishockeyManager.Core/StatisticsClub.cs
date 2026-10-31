namespace EishockeyManager.Core;

public sealed partial class Club
{
    /// <summary>
    /// Strafminuten des Vereins in den Ligaspielen dieser Saison (Fair-Play-Tabelle). [ORIGINAL] DGROUP 0x82A1 + 0x50 ·
    /// Verein (Wort): Datei 0x35967–0x35986 addiert bei jeder Strafe (Code &amp; 7 = 2 bzw. 5 Minuten) nur am
    /// Ligaspieltag (Wettbewerbscode DGROUP 0xD0C2 = 7); Datei 0x5647A–0x56497 setzt die Werte der Vereine 0–63 nach den
    /// Prämien des Saisonendes auf 0.
    /// </summary>
    public int LeaguePenaltyMinutes { get; set; }
}

/// <summary>Zeile der Fair-Play-Tabelle: Platz, Verein, Strafminuten und Ligaspiele.</summary>
public sealed record FairPlayRow(int Rank, Club Club, int PenaltyMinutes, int Games)
{
    /// <summary>
    /// [ORIGINAL] „PRO SPIEL“ in Hundertsteln (Datei 0x2A4B5–0x2A4FA): Strafminuten · 100 / Spiele, ohne Spiele durch 1.
    /// </summary>
    public int PerGameHundredths => PenaltyMinutes * 100 / Math.Max(1, Games);
}

public sealed partial class GameState
{
    /// <summary>
    /// Strafminuten eines Ligaspiels gutschreiben (Aufruf aus <see cref="PlayNextMatchDay"/>). [ORIGINAL] Datei 0x35967:
    /// nur am Ligaspieltag; Pokale, Play-Offs, Relegation und WM zählen nicht.
    /// </summary>
    public static void CountLeaguePenalties(Club home, Club away, MatchReport report)
    {
        home.LeaguePenaltyMinutes += report.HomePenaltyMinutes;
        away.LeaguePenaltyMinutes += report.AwayPenaltyMinutes;
    }

    /// <summary>[ORIGINAL] Neue Saison: Strafminuten aller Vereine auf 0 (Datei 0x5647A, nach den Prämien 0x5645D).</summary>
    internal void ResetLeaguePenalties()
    {
        foreach (var club in Clubs)
            club.LeaguePenaltyMinutes = 0;
    }

    /// <summary>
    /// Ligaspiele eines Vereins in dieser Saison. [ORIGINAL] Datei 0x2A4C1–0x2A4DD: Vereinsstatistik +0 (Heimspiele) +
    /// +1 (Auswärtsspiele), DGROUP 0x8278 + 0x50 · Verein; im Remake die gespielten Ligaspiele in <see cref="Results"/>.
    /// </summary>
    public int LeagueGames(Club club) => Results.Count(r => r.Involves(club.Index));

    /// <summary>
    /// Fair-Play-Tabelle einer Liga. [ORIGINAL] em.exe Ov 0xE0, Datei 0x2A1E0 (<c>0xE0:0x20</c>): die zwölf Vereine ab
    /// DGROUP 0x7C0 + Liga in Indexfolge, Austauschsortierung aufsteigend nach Strafminuten (Datei 0x2A21D–0x2A292:
    /// Tausch nur bei echt mehr Minuten). Bei Gleichstand um Platz 1 steht der Verein mit dem kleineren Index vorn
    /// (das entscheidet die Fair-Play-Prämie); dahinter ist die Sortierung nicht stabil und wird genau so nachgebildet.
    /// [APPROX] Reihenfolge der Ligaplätze: Vereinsindex des Remakes (das Original tauscht die Vereinssätze beim Auf- und
    /// Abstieg, die Indexfolge innerhalb einer Liga kann daher abweichen).
    /// </summary>
    public IReadOnlyList<FairPlayRow> FairPlayTable(League league)
    {
        var order = LeagueClubs(league).OrderBy(c => c.Index).ToList();
        for (var i = 0; i < order.Count; i++)
            for (var j = i + 1; j < order.Count; j++)
                if (order[i].LeaguePenaltyMinutes > order[j].LeaguePenaltyMinutes)
                    (order[i], order[j]) = (order[j], order[i]);
        return order.Select((c, i) => new FairPlayRow(i + 1, c, c.LeaguePenaltyMinutes, LeagueGames(c))).ToList();
    }

    /// <summary>
    /// Einzelprämien eines Managers am Saisonende in der Reihenfolge des Fensters „Saisonende“ (Datei 0x27184–0x2725D):
    /// bester Torhüter (Symbol 12), Topscorer (Symbol 3), Fair-Play-Erster (Symbol 13). Je Ereignis
    /// <see cref="SinglePremium"/> über <see cref="SeasonPremium"/>.
    /// <list type="bullet">
    /// <item>[ORIGINAL] Torhüter und Topscorer: Kennungen aus <c>0xC8:0x20(1)</c> je Manager (Datei 0x5576E), beide
    ///   aus der Liga des Managers (<see cref="PlayerStatistics"/>).</item>
    /// <item>[ORIGINAL] Fair-Play: <c>0xE0:0x20(0)</c> (Datei 0x27213) liefert 1, wenn der Erste der Fair-Play-Tabelle der
    ///   eigenen Liga der Verein des Managers ist (Vereinsfeld 0x16 = Manager + 1, Datei 0x2A29D–0x2A2CC).</item>
    /// </list>
    /// </summary>
    public IReadOnlyList<SeasonEvent> SinglePremiumEvents(Manager manager, League league)
    {
        var events = new List<SeasonEvent>();
        if (PlayerStatistics.BestGoalie(this, league) is { } goalie && goalie.Club.Index == manager.ClubIndex)
            events.Add(SeasonEvent.BestGoalie);
        if (PlayerStatistics.TopScorer(this, league) is { } scorer && scorer.Club.Index == manager.ClubIndex)
            events.Add(SeasonEvent.TopScorer);
        if (FairPlayTable(league) is [var first, ..] && first.Club.Index == manager.ClubIndex)
            events.Add(SeasonEvent.FairPlay);
        return events;
    }
}

/// <summary>Zeilen der Serien im Bildschirm „Verein“ (Texte DGROUP 0xADC, Einträge 1–7).</summary>
public enum SeriesKind
{
    Won = 0,
    Lost = 1,
    Drawn = 2,
    NotWon = 3,
    NotLost = 4,
    CleanSheet = 5,
    NoGoal = 6,
}

/// <summary>Spalten der Serien und der Torquoten: G(esamt), H(eim), A(uswärts).</summary>
public enum SeriesColumn
{
    Total = 0,
    Home = 1,
    Away = 2,
}

/// <summary>
/// Serien eines Vereins und Rekordserien eines Managers (Spur statistiken-verein). [ORIGINAL] je 21 Byte, Zeile
/// <see cref="SeriesKind"/> · 3 + Spalte <see cref="SeriesColumn"/>: laufende Serien des Vereins DGROUP 0xB57B + 0x15 ·
/// Verein, Rekorde des Managers DGROUP 0xBABB + 0x15 · Manager (Anzeige Datei 0x2D8A3 bzw. 0x2D95A mit Basis − 3).
/// </summary>
public static class Series
{
    /// <summary>Werte je Satz (7 Zeilen × 3 Spalten).</summary>
    public const int Count = 21;

    public static int Index(SeriesKind kind, SeriesColumn column) => 3 * (int)kind + (int)column;

    /// <summary>
    /// Ein Ligaergebnis in die laufenden Serien eines Vereins eintragen. [ORIGINAL] Datei 0x50443–0x50AB4 (Teil der
    /// Tabelleneintragung 0x50283, nur bei Wettbewerbscode DGROUP 0xD0C2 = 7 bzw. 8): je Verein Spalte Gesamt und die
    /// eigene Spalte (Heim bzw. Auswärts); Sieg → GEWONNEN und NICHT VERLOREN + 1, VERLOREN, UNENTSCHIEDEN und NICHT
    /// GEWONNEN 0; Niederlage → VERLOREN und NICHT GEWONNEN + 1, die übrigen 0; Unentschieden → UNENTSCHIEDEN, NICHT
    /// GEWONNEN und NICHT VERLOREN + 1, GEWONNEN und VERLOREN 0; kein Gegentor → OHNE GEGENTOR + 1 sonst 0; kein eigenes
    /// Tor → OHNE TORERFOLG + 1 sonst 0. Bytes: + 1 läuft wie im Original bei 255 auf 0.
    /// </summary>
    public static void Enter(int[] series, int goalsFor, int goalsAgainst, bool home)
    {
        var side = home ? SeriesColumn.Home : SeriesColumn.Away;
        foreach (var column in new[] { SeriesColumn.Total, side })
        {
            Set(series, SeriesKind.Won, column, goalsFor > goalsAgainst);
            Set(series, SeriesKind.Lost, column, goalsFor < goalsAgainst);
            Set(series, SeriesKind.Drawn, column, goalsFor == goalsAgainst);
            Set(series, SeriesKind.NotWon, column, goalsFor <= goalsAgainst);
            Set(series, SeriesKind.NotLost, column, goalsFor >= goalsAgainst);
            Set(series, SeriesKind.CleanSheet, column, goalsAgainst == 0);
            Set(series, SeriesKind.NoGoal, column, goalsFor == 0);
        }
    }

    private static void Set(int[] series, SeriesKind kind, SeriesColumn column, bool continues)
    {
        var i = Index(kind, column);
        series[i] = continues ? (series[i] + 1) & 0xFF : 0;
    }

    /// <summary>
    /// Rekorde fortschreiben: je Wert das Maximum aus Rekord und laufender Serie. [ORIGINAL] Datei 0x50CDB–0x50D65
    /// (Ende von 0x50B45, nach den Paarungen eines Ligaspieltags): für alle Manager 0 … [0x386] − 1 mit dem Verein aus
    /// Managerfeld 0x966F; übernommen wird nur ein echt größerer Wert.
    /// </summary>
    public static void UpdateRecords(int[] records, int[] current)
    {
        for (var i = 0; i < Count; i++)
            if (records[i] < current[i])
                records[i] = current[i];
    }
}

public sealed partial class Club
{
    /// <summary>
    /// Laufende Serien des Vereins (<see cref="Series"/>). [ORIGINAL] Keine Schreibstelle außer 0x50283 (Ligaergebnis) und
    /// dem Tausch der Vereinssätze 0x36554–0x36593: die Serien laufen über das Saisonende hinweg weiter.
    /// </summary>
    public int[] SeriesCurrent { get; init; } = new int[Series.Count];
}

public sealed partial class Manager
{
    /// <summary>
    /// Rekordserien des Managers (in Klammern hinter der laufenden Serie). [ORIGINAL] DGROUP 0xBABB + 0x15 · Manager;
    /// außer 0x50D4B keine Schreibstelle – die Rekorde gelten für das ganze Spiel, auch über einen Vereinswechsel.
    /// </summary>
    public int[] SeriesRecords { get; init; } = new int[Series.Count];

    /// <summary>
    /// Über-/Unterzahl der Saison (<see cref="PowerPlay"/>, Managerfelder DGROUP 0x97D2 + 2 · k, k = 0 … 5).
    /// </summary>
    public int[] PowerPlayCounts { get; init; } = new int[PowerPlay.Count];
}

/// <summary>Zähler der Über-/Unterzahl je Manager (Reihenfolge der Managerfelder 0x97D2 ff.).</summary>
public enum PowerPlayCounter
{
    /// <summary>TORE IN ÜBERZAHL (0x97D2).</summary>
    GoalsInPowerPlay = 0,

    /// <summary>GEGENTORE IN ÜBERZAHL (0x97D4).</summary>
    GoalsAgainstInPowerPlay = 1,

    /// <summary>TORE IN UNTERZAHL (0x97D6).</summary>
    GoalsShortHanded = 2,

    /// <summary>GEGENTORE IN UNTERZAHL (0x97D8).</summary>
    GoalsAgainstShortHanded = 3,

    /// <summary>Zahl der Überzahlspiele (0x97DA), Teiler von „ÜBERZAHL ERFOLG“.</summary>
    PowerPlays = 4,

    /// <summary>Zahl der Unterzahlspiele (0x97DC), Teiler von „UNTERZAHL ERFOLG“.</summary>
    ShortHanded = 5,
}

/// <summary>
/// Über- und Unterzahl der Managervereine (Spur statistiken-verein). Die Zählung gehört zur Minutenrechnung
/// (<c>MatchEngine*.cs</c>, Spur <c>spielstaerke-manager</c>); hier stehen die Regeln, der Zustand, der Saisonwechsel und
/// die Anzeige.
/// <list type="bullet">
/// <item>[ORIGINAL] Tore (Datei 0xAA2C–0xAAEF in der Abschlussfunktion 0xA484, nach einem Treffer): Feldspieler Heim
///   [bp−0x11] gegen Gast [bp−0x12]; für den Heimmanager bei Heim &gt; Gast Zähler k = Seite des Schützen (0 Heim,
///   1 Gast), bei Heim &lt; Gast k = 2 + Seite; für den Gastmanager bei Heim &lt; Gast k = 1 − Seite, bei Heim &gt; Gast
///   k = 3 − Seite (<see cref="Goal"/>).</item>
/// <item>[ORIGINAL] Beginn einer Überzahl (Strafwurf 0x35818, Datei 0x35A84–0x35B18): Steht es nach der Strafe
///   gegnerische minus eigene Feldspieler = 1, zählt der Gegnermanager k = 4 und der eigene Manager k = 5; fällt die
///   Strafe im 1/4-Folgewurf für den Gegner nicht an, wird der zuletzt gezählte Wert zurückgenommen (0x35C7B–0x35CB2).
///   Nach einem Tor in Überzahl (0xA939–0xA9A2) zählt eine danach noch bestehende Überzahl erneut (k = 5 bzw. 4).</item>
/// <item>[ORIGINAL] Saisonwechsel 0x5588E–0x558B2: alle sechs Zähler je Manager auf 0.</item>
/// <item>[ORIGINAL] Anzeige 0x2D9D9–0x2DB22: Werte k = 0 … 3; „ÜBERZAHL ERFOLG“ = k0 · 100 / k4 (ohne Überzahl 0 %),
///   „UNTERZAHL ERFOLG“ = 100 − k3 · 100 / k5 (ohne Unterzahl 100 %), beide auf 0 … 100 begrenzt.</item>
/// </list>
/// </summary>
public static class PowerPlay
{
    /// <summary>Zähler je Manager.</summary>
    public const int Count = 6;

    /// <summary>
    /// Zähler zu einem Tor aus Sicht eines Managers. <paramref name="managerHome"/>: der Manager spielt Heim;
    /// <paramref name="homeSkaters"/>/<paramref name="awaySkaters"/> Feldspieler beim Tor; <paramref name="homeScored"/>:
    /// das Tor fiel für die Heimmannschaft. Null bei gleicher Zahl Feldspieler. [ORIGINAL] Datei 0xAA2C–0xAAEF.
    /// </summary>
    public static PowerPlayCounter? Goal(bool managerHome, int homeSkaters, int awaySkaters, bool homeScored)
    {
        if (homeSkaters == awaySkaters)
            return null;
        var side = homeScored ? 0 : 1;
        var managerAhead = managerHome ? homeSkaters > awaySkaters : awaySkaters > homeSkaters;
        var own = managerHome ? side : 1 - side; // 0: eigenes Tor, 1: Gegentor
        return (PowerPlayCounter)(managerAhead ? own : 2 + own);
    }

    /// <summary>Wert in Prozent der Anzeige „ÜBERZAHL ERFOLG“. [ORIGINAL] Datei 0x2DA06–0x2DAD2.</summary>
    public static int PowerPlaySuccess(IReadOnlyList<int> counts)
    {
        var plays = counts[(int)PowerPlayCounter.PowerPlays];
        var value = plays == 0 ? 0 : counts[(int)PowerPlayCounter.GoalsInPowerPlay] * 100 / plays;
        return Math.Clamp(value, 0, 100);
    }

    /// <summary>Wert in Prozent der Anzeige „UNTERZAHL ERFOLG“. [ORIGINAL] Datei 0x2DA4A–0x2DAD2.</summary>
    public static int ShortHandedSuccess(IReadOnlyList<int> counts)
    {
        var plays = counts[(int)PowerPlayCounter.ShortHanded];
        var value = plays == 0 ? 100 : 100 - counts[(int)PowerPlayCounter.GoalsAgainstShortHanded] * 100 / plays;
        return Math.Clamp(value, 0, 100);
    }
}

/// <summary>Werte des Bildschirms „Verein“ für einen Manager (alles nur gelesen).</summary>
public sealed partial class GameState
{
    /// <summary>
    /// Ligaergebnis in die Serien beider Vereine eintragen (Aufruf aus <see cref="PlayNextMatchDay"/>). [ORIGINAL]
    /// 0x50283 (Datei 0x50443–0x50AB4).
    /// </summary>
    internal static void CountSeries(Club home, Club away, int homeGoals, int awayGoals)
    {
        Series.Enter(home.SeriesCurrent, homeGoals, awayGoals, home: true);
        Series.Enter(away.SeriesCurrent, awayGoals, homeGoals, home: false);
    }

    /// <summary>Rekordserien aller Manager nach einem Ligaspieltag fortschreiben. [ORIGINAL] Datei 0x50CDB–0x50D65.</summary>
    internal void UpdateSeriesRecords()
    {
        foreach (var manager in Managers)
            Series.UpdateRecords(manager.SeriesRecords, Clubs[manager.ClubIndex].SeriesCurrent);
    }

    /// <summary>
    /// Ein Tor eines Ligaspiels, Pokal- oder Play-off-Spiels in den Über-/Unterzahl-Zählern der beteiligten Manager
    /// eintragen (<see cref="PowerPlay.Goal"/>). Für die Minutenrechnung (Spur <c>spielstaerke-manager</c>); das Original
    /// zählt in jedem Spiel mit Managerbeteiligung (0xA484 prüft keinen Wettbewerb).
    /// </summary>
    public void CountPowerPlayGoal(Club home, Club away, int homeSkaters, int awaySkaters, bool homeScored)
    {
        foreach (var (club, isHome) in new[] { (home, true), (away, false) })
            if (ManagerOf(club) is { } manager && PowerPlay.Goal(isHome, homeSkaters, awaySkaters, homeScored) is { } k)
                manager.PowerPlayCounts[(int)k]++;
    }

    /// <summary>[ORIGINAL] Saisonwechsel Datei 0x5588E–0x558B2: Über-/Unterzahl-Zähler aller Manager auf 0.</summary>
    internal void ResetPowerPlayStatistics()
    {
        foreach (var manager in Managers)
            Array.Clear(manager.PowerPlayCounts);
    }

    private Manager? ManagerOf(Club club) => Managers.FirstOrDefault(m => m.ClubIndex == club.Index);

    /// <summary>
    /// „TORE/SPIEL“ bzw. „GEGENTORE/SPIEL“ in Zehnteln. [ORIGINAL] 0x2ECA0: Ligatabelle des Vereins (DGROUP 0x8270 + 0x50 ·
    /// Verein: Tore +0/+1, Gegentore +4/+5, Spiele +8/+9 für Heim/Auswärts), Gesamt = Summe beider; Wert = Tore · 10 /
    /// Spiele (LDIV), ohne Spiel 0. Im Remake die Ligaergebnisse der Saison (<see cref="Results"/>).
    /// </summary>
    public int GoalsPerGameTenths(Club club, SeriesColumn column, bool against)
    {
        var games = 0;
        var goals = 0;
        foreach (var result in Results.Where(r => r.Involves(club.Index)))
        {
            var (goalsFor, goalsAgainst, home) = result.For(club.Index);
            if (column == SeriesColumn.Home && !home || column == SeriesColumn.Away && home)
                continue;
            games++;
            goals += against ? goalsAgainst : goalsFor;
        }
        return games == 0 ? 0 : goals * 10 / games;
    }

    /// <summary>
    /// „BENÖTIGTER SCHNITT“: Zuschauer je Heimspiel, die den Monatsfehlbetrag decken. [ORIGINAL] 0x3EB30:
    /// (Ausgaben 0x3E811(1, 1) − Einnahmen 0x3E657(1)) in Drittel-DM · 12 / 23 · [0x7A1] / [0x79D] (= 1 / 3) / Eintritt
    /// (Managerfeld 0x973F), negativ → 0. [APPROX] Ausgaben und Einnahmen liefert das Remake in DM
    /// (<see cref="MonthlyExpenses"/>, <see cref="MonthlyIncome"/>); sie werden mal 3 als Drittel genommen, die Rundung
    /// der Einzelposten kann den Wert um wenige Zuschauer verschieben.
    /// </summary>
    public long RequiredAverage(Manager manager)
    {
        var thirds = (MonthlyExpenses(manager) - MonthlyIncome(manager)) * 3;
        var price = Math.Max(1, Finance(manager).Stadium.TicketPrice);
        var value = thirds * 12 / 23 * 1 / 3 / price;
        return Math.Max(0, value);
    }

    /// <summary>
    /// „SCHULDEN GESAMT“ in DM. [ORIGINAL] 0x57768 mit (0, 0x63): Summe der negativen Salden gegenüber allen anderen
    /// Managern und der Bank (Partner 4). [APPROX] Saldo je Partner im Remake: Kredite zwischen zwei Managern verrechnet,
    /// bei der Bank Kredit minus Sparbuch.
    /// </summary>
    public long TotalDebts(Manager manager)
    {
        var index = IndexOf(manager);
        long debts = 0;
        for (var partner = 0; partner < Managers.Count; partner++)
            if (partner != index)
                debts += Math.Max(0, LoanAmount(partner, index) - LoanAmount(index, partner));
        debts += Math.Max(0, -Finance(manager).Bank.Balance);
        return debts;
    }
}
