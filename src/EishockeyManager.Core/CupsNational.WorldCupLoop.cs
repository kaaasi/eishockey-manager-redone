namespace EishockeyManager.Core;

/// <summary>
/// WM-Schleife des Originals (Spur nationalteam Runde 14 Teil 4, #664/R14NT-08; Review #662 R14NT-01/02): die Tage nach dem
/// 13. April in den Spielarten mit Vereinssaison, mit und ohne Bundestrainer.
/// </summary>
/// <remarks>
/// [ORIGINAL] Tagesfunktion 0x54C9F, selbst nachgelesen (<c>tools/reverse-engineering/disasm.py gamedata/em.exe 0x55374 0x55760</c>):
/// <list type="bullet">
/// <item>Am Tag mit Markierung 255 geht es nach den Managermenüs über 0x553A2 nach 0x553FF: Ernennung nur bei Modus ≠ 2 und einem
///   Manager unter den ersten fünf (Datei 0x55436–0x554BF, DGROUP 0x9EDA := 0x6AC bei 0x55485). Ohne Ernennung würfelt das
///   Original die Werte Deutschlands (Datei 0x554D9–0x5556D, <see cref="RerollNationalRatings"/>) und läuft danach
///   <b>in dieselbe Schleife</b> Datei 0x55570–0x55740 – im Modus 2 („nur Fahne“) genauso wie im Modus 4 ohne Ernennung.
///   Der Modus 1 („nur Weltkugel“) springt schon am Saisonanfang dorthin (Datei 0x54CEE–0x54D38) und hat im Remake keine
///   Vereinssaison (<see cref="OnDayManager"/>).</item>
/// <item>Ein Tag der Schleife: (a) nur mit Bundestrainer und Tagesbyte ≠ 0 sein Menü, 0x37F/0x380 und 0x952C(5, 0) (Datei
///   0x55587–0x555D6, <see cref="NationalCoach"/>); (b) Modus ≠ 2 und Tagesbyte ≠ 0: WM-Spiele 0x30E7B(1) (Datei 0x555DD–0x555F3);
///   (c) Tageszähler ≡ 0 oder 4 (mod 7): Tagesablauf 0x07169 (Datei 0x5561A–0x5565C, siehe <see cref="WorldCupLoopDailyRun"/>);
///   (d) nächster Tag (Datei 0x55670), je Manager 0x3ED01 und 0x3F833 (Datei 0x5569A–0x556B1), am Tag ≡ 0 (mod 7) 0x3D316
///   (Datei 0x556B5–0x556CD), je Kaderplatz des Managers 0x07C12 (Datei 0x556D2–0x55712); Tagesbyte 0x10 oder 0 → nächster
///   Schleifentag (Datei 0x55724–0x55740), 255 → Saisonwechsel.</item>
/// <item>In der Schleife gibt es keine kleine Wanderung 0x9D1B (nur bei Datei 0x56448, Saisonwechsel), keine Talentsuche 0x06FA5
///   (nur Datei 0x5528A im Hauptweg), keine Jugendteams 0x39DEC (Datei 0x5501C), keinen Listenwechsel 0x494EF des Markts
///   (Datei 0x54E71–0x54E7C, Hauptweg) und kein Training außer in 0x07169.</item>
/// </list>
/// </remarks>
public sealed partial class GameState
{
    /// <summary>
    /// Läuft der Tag in der WM-Schleife des Originals? [ORIGINAL] Alle Tage nach dem Tag mit Markierung 255 (13. April) bis zum
    /// Saisonwechsel, in jeder Spielart mit Vereinssaison und unabhängig von der Ernennung (siehe Klassenkommentar; #664).
    /// </summary>
    private bool InWorldCupLoop(int day) => !WorldOnly && day > SeasonCalendar.ClubSeasonEnd;

    /// <summary>
    /// Ein Tag der WM-Schleife (Datei 0x55570–0x55740) im Tageslauf des Remakes, vor den Spielen des Tages <paramref name="day"/>:
    /// <list type="number">
    /// <item>[ORIGINAL] Schritt (c) des Vortags: Er läuft im Original nach den WM-Spielen des Vortags (Datei 0x5565C hinter 0x555F3),
    ///   im Remake also hier (<see cref="WorldCupLoopDailyRun"/>).</item>
    /// <item>[ORIGINAL] Schritt (d) zum Tag <paramref name="day"/>: Tagesabschluss ohne Talentsuche und Jugendteams
    ///   (<see cref="AdvanceFinanceInWorldCupLoop"/>), Heilung (<see cref="HealInjuries"/>; 0x07C12 zählt wie im Hauptweg nur
    ///   an Tagen ≡ 0 mod 7). [APPROX] Wie <see cref="HealInjuries"/>: auch die Spieler ohne Manager und die freien
    ///   Marktspieler, das Original in der Schleife nur die Kaderplätze der Manager.</item>
    /// <item>[ORIGINAL] Schritt (a): an WM-Tagen mit Bundestrainer nach dessen Menü 0x952C(5, 0) (Datei 0x555D6,
    ///   <see cref="UpdateNationalTeamValues"/>). Keine Neuaufstellung „Tag mit Ereignis“ (0x54D41, R14NT-02) und kein 0x952C(5, 1)
    ///   des Hauptwegs: Es spielt die Aufstellung, die der Bundestrainer zuletzt gesehen hat. Schritt (b) sind die Wettbewerbe
    ///   des Tages nach diesem Tageslauf.</item>
    /// </list>
    /// Kein Training und kein Markt an den übrigen Tagen, keine Wanderung, keine Aufstellungsautomatik außer in Schritt (c) und
    /// nach einer Heilung.
    /// </summary>
    private void OnDayWorldCupLoop(int day)
    {
        var previous = day - 1;
        if (previous > SeasonCalendar.ClubSeasonEnd && MatchEngine.IsDriftDay(previous) && WorldCupPlayedOn(previous))
            WorldCupLoopDailyRun(previous);
        AdvanceFinanceInWorldCupLoop(day);
        HealInjuries(day);
        if (NationalCoachPlaysOn(day))
            UpdateNationalTeamValues();
    }

    /// <summary>
    /// Hatte der Schleifentag WM-Spiele? [ORIGINAL] Datei 0x555DD–0x555ED: Modus ≠ 2 (DGROUP 0xD0B9) und Tagesbyte ≠ 0; in der
    /// Schleife sind die Tagesbytes ≠ 0 die WM-Tage (0x10).
    /// </summary>
    private bool WorldCupPlayedOn(int day) =>
        Mode != GameMode.ClubOnly && WorldCup is not null && SeasonCalendar.Events(day).HasFlag(DayEvents.WorldCup);

    /// <summary>
    /// Schritt (c) der WM-Schleife: Tagesablauf 0x07169 (Datei 0x5565C) für den Manager DGROUP 0xA70E.
    /// <list type="bullet">
    /// <item>[ORIGINAL] 0xA70E ist vor den Spielen 5 (Datei 0x55581/0x555BC); 0x30E7B setzt es über 0x36B5D auf 0 (Datei 0x36B7A),
    ///   und die Strafen der Paarungen ohne Manager (0x35818 aus 0x37A62) schreiben Feld +0x16 des Landes, bei allen Ländern ohne
    ///   Bundestrainer 0 (Datei 0x35AB8–0x35AC3). 0x07169 endet bei 0xA70E ≥ Zahl der Manager sofort (Datei 0x07172–0x0717B →
    ///   0x07C0C). Also wirkt (c) nur nach WM-Spielen und nur für Manager 0; an den übrigen Tagen ≡ 0/4 (217, 221) nichts.</item>
    /// <item>[APPROX] Mit Bundestrainer: Traf die letzte Strafe des Spieltags Deutschland (Feld +0x16 = 6 nach Datei 0x55494),
    ///   bliebe 0xA70E = 5 und (c) wirkungslos; das Remake rechnet immer Manager 0.</item>
    /// <item>Inhalt für Manager 0: Training (Datei 0x07B5E → 0x0659C, <see cref="TrainingRules.Apply"/>, Kosten wie
    ///   <see cref="TrainOneDay"/>), Heilung seiner Kaderplätze (Datei 0x07B4E → 0x07C12 mit dem Tageszähler vor dem Tageswechsel),
    ///   Aufstellungsautomatik (Datei 0x07C07). [APPROX] Markt und Verträge (Gebote, Angebote, Verlängerungen, Sperren; Datei
    ///   0x07215–0x07AEF) über <see cref="TransferMarket"/> bzw. <see cref="AnnounceContractEnds"/> für alle Manager und samt dem
    ///   Listenwechsel des Hauptwegs (0x54E71): Die Tagesfunktionen des Markts kennen keinen einzelnen Manager (Dateien der Spur
    ///   kader-start). Mit einem Manager gleichwertig bis auf den Listenwechsel.</item>
    /// </list>
    /// </summary>
    private void WorldCupLoopDailyRun(int day)
    {
        if (Managers.Count == 0 || Managers[0] is not { Dismissed: false } manager)
            return;
        var club = Clubs[manager.ClubIndex];
        var plan = PrepareTraining(manager);
        Book(manager, FinanceCategory.Training, -plan.CostPerTrainingDay);
        foreach (var player in club.Players)
            TrainingRules.Apply(player, plan, manager.Level, Random);
        if (PlayerInjuries.IsHealingDay(day))
        {
            var healed = false;
            foreach (var p in club.Players)
                healed |= PlayerInjuries.Heal(p);
            if (healed)
                PlayerInjuries.RefillLineup(club);
        }
        TransferMarket.CatchUp(this);
        AnnounceContractEnds(day);
        club.ApplyAutoLineup();
    }

    /// <summary>
    /// Schritt (d) der WM-Schleife für die Finanzen: Tageswechsel bis <paramref name="day"/> wie <see cref="AdvanceFinanceTo"/>
    /// (Tagesabschluss 0x3ED01 samt Börse und Monatsabschluss, Werbebewertung 0x3F833, Stadionbau 0x3D316), aber ohne
    /// Talentsuche 0x06FA5 und Jugendteams 0x39DEC (beide nur im Hauptweg, Datei 0x5528A bzw. 0x5501C). [APPROX] Abgelaufene
    /// Meldungen wie im Hauptweg des Remakes (Stelle im Original nicht gelesen).
    /// </summary>
    private void AdvanceFinanceInWorldCupLoop(int day)
    {
        while (FinanceDay < day)
        {
            FinanceDay++;
            var date = SeasonCalendar.LayoutDate(FinanceDay);
            ExpireMessages();
            DailyStadium();
            AccrueDailyInterest(date);
            DailySponsorOffers(date);
            if (date.Day == 1)
                foreach (var manager in Managers)
                    CountDownSponsorContracts(manager);
            if (date.DayOfWeek == DayOfWeek.Monday)
                WeeklyFinances();
            if (date.Day == DateTime.DaysInMonth(date.Year, date.Month))
                MonthlyFinances();
            DailySponsorRatings();
            AdvanceStocks();
        }
    }
}
