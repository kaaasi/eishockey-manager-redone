namespace EishockeyManager.Core;

/// <summary>
/// Verbindet die Pakete „manager“ und „mannschaft“ mit der Zeitleiste des Pakets „saison“
/// (Anschlusspunkte in <c>SeasonFlow.cs</c>). Die Pakete wurden parallel entwickelt; hier laufen sie zusammen.
/// </summary>
public sealed partial class GameState
{
    /// <summary>
    /// Spielstart nach der Kaderanlage: Börse, Werbeangebote und Trainingspläne für alle Manager vorbereiten,
    /// Pokalwettbewerbe anmelden.
    /// Verarbeitet keinen Tag; die Bildschirme lesen danach ausschließlich den vorhandenen Zustand.
    /// </summary>
    internal void InitializeManagerSystems()
    {
        // Spur transfer-vertraege-komplett: Startgehalt der Kaderplätze der Manager = Wert(Gehalt) (0x29EE4), ohne Zufall.
        ApplyStartSalaries();
        // Spur wettbewerbe-spiel, Teil 7: Kaderplatz +0x19 der Managerkader (Datei 0x299F4), eigene Zufallsquelle.
        RollStartHiddenSkills();
        _ = Stocks;
        foreach (var manager in Managers)
        {
            RenewOffers(manager);
            PrepareTraining(manager);
        }

        // Paket „wettbewerbe“: DEB-Pokal (später Europapokal, WM) nach dem Spielmodus anmelden und auslosen.
        InitializeCompetitions();

        // Spur wettbewerbe-spiel, Teil 8: automatische Aufstellung der Managervereine beim Spielstart (Datei 0x28165).
        ApplyAutoLineups();
        // Integration Runde 11: Bezugspunkt der Spielstärke ist die Aufstellung beim Spielstart (Club.ReferenceRating) –
        // nach der Neuaufstellung der Managervereine neu bewerten, damit die Stärke zu Beginn der Datenbankstärke gleicht.
        // [APPROX] wie die Stärkeformel selbst (TeamStrength).
        foreach (var club in Clubs)
            if (Managers.Any(m => m.ClubIndex == club.Index))
                club.ReferenceRating = TeamStrength.Rating(club.Lineup);
    }

    /// <summary>
    /// Tageswechsel zum Tag <paramref name="day"/> vor den Managermenüs (#688, R688-04): im Original nach den Spielen des Vortags
    /// (Hauptweg Datei 0x55204–0x552D3, WM-Schleife Schritt (c) und (d) Datei 0x5561A–0x55740), dann die Tage mit Ereignis vor den
    /// Zügen (Datei 0x54D68–0x54DBA) und je Zug der Listenwechsel (Datei 0x54E71) vor dem Menü 0x44E22 (Datei 0x54EB2).
    /// </summary>
    partial void OnDayOpen(int day)
    {
        // Spur wettbewerbe-weltkugel: Im Modus „nur Weltkugel“ gibt es keine Vereinssaison – keine Finanzen,
        // kein Training, kein Markt, keine Vertragsmeldungen, keine Ligastatistik (Datei 0x54CEE: kein Tageslauf).
        if (!WorldOnly)
            OpenClubSeasonDay(day);
        else
            OpenWorldOnlyLoopDay(day); // Review #674, R14NT4-04: auch Modus 1 läuft durch die WM-Schleife (CupsNational.WorldCupLoop.cs)
    }

    /// <summary>Jeder Tag der Zeitleiste nach den Managermenüs, bevor dessen Spiele ausgetragen werden.</summary>
    partial void OnDayManager(int day)
    {
        // Spur nationalteam Runde 13 (A009, L9): Länderwerte aus der Aufstellung des Bundestrainers (0x952C mit Manager 5,
        // Tageslauf Datei 0x54F9C–0x54FBF und nach dem Menü an WM-Tagen Datei 0x555D6) – vor der Ernennung des Tages wie im
        // Original (Ernennung erst nach dem Tag, Datei 0x553A2). #688 (R688-04/-08): nach den Menüs, also nach dem Takt des Tages
        // (OnDayOpen) wie im Original; nach der Ernennung im Modus 4 nur noch in der WM-Schleife (R14NT-01).
        if (WorldOnly)
            UpdateNationalTeamValues(); // Modus 1: Schritt (a) der Schleife ([APPROX] vor dem ersten WM-Tag kein Tageslauf im Original)
        else if (InWorldCupLoop(day))
            WorldCupLoopStepA(day);
        else if (IsWorldCupEntryDay(day))
            OnDayWorldCupEntry(day);
        else
            // [ORIGINAL] 0x952C(5, 1) nach den Zügen, Datei 0x54F9C–0x54FBF (#594); davor an Tagen mit Ereignis die Neuaufstellung von
            // Satz 5 mit der gesicherten Einstellung (A009, Datei 0x54D41–0x54DAA; [APPROX] nach den Menüs, siehe MainPathEventDayLineups).
            OnDayNationalTeam(day);

        // Paket „wettbewerbe“: an den Tagen mit Markierung 255 im Modus 4 den Bundestrainer ernennen (Spur
        // bundestrainer-modus4), dann die Werte Deutschlands ohne Bundestrainer neu auswürfeln.
        if (day is SeasonCalendar.ClubSeasonEnd or SeasonCalendar.SeasonEnd)
            OnNationalMarkerDay();
    }

    /// <summary>Tageswechsel der Vereinssaison (Finanzen, Takt mit Training, Markt und Verträgen, Listenwechsel).</summary>
    private void OpenClubSeasonDay(int day)
    {
        // Spur transfer-vertraege-komplett: offene Verhandlungen am Saisonende enden vor dem ersten Tag wie „Abbruch“ (0x27D91).
        ResolveRenewals();

        // Spur nationalteam Runde 14 (R14NT-01/02, Review #662; Teil 4, #664): Nach dem 13. April läuft im Original jeder Tag in
        // der WM-Schleife (Datei 0x55570–0x55740, siehe CupsNational.WorldCupLoop.cs) – mit und ohne Bundestrainer: eigener
        // Tagesabschluss, kein Training und kein Markt außer nach den WM-Spielen der Tage ≡ 0/4, keine kleine Wanderung, kein
        // 0x952C des Hauptwegs, keine Neuaufstellung „Tag mit Ereignis“. Ausgenommen der erste Tag (R14NT4N-01, unten).
        if (InWorldCupLoop(day))
        {
            OpenWorldCupLoopDay(day);
            return;
        }
        // Nachprüfung #681 (R14NT4N-01): Der Tageswechsel nach den Spielen des 13. April (Zähler 217) gehört im Original noch
        // zum Hauptweg (Datei 0x55204–0x552D3), erst danach beginnt die Schleife (Datei 0x552D5); siehe OnDayWorldCupEntry.

        // Finanzen: Tages-, Wochen- und Monatswechsel bis einschließlich heute. [ORIGINAL] Tagesabschluss 0x3ED01/0x3F833 je
        // Manager direkt nach dem Weiterzählen (Datei 0x5522B/0x55230), vor dem Takt.
        AdvanceFinanceTo(day);

        // Paket „wettbewerbe“: Ewige Tabelle, Ewige Bilanz und „Früher“ schreibt seit Spur saisonwechsel Teil 3 (S-K4)
        // FinishSeason nach Zwangsabstieg und Austausch fort (em.exe Datei 0x563AF).

        // Spur spielberechnung-komplett (#204): kleine Wanderung der Teamwerte zweimal je Woche (0x5525A–0x55292). #675 (Review
        // #674, R14NT4-03): Datei 0x55204 zählt DGROUP 0x3B7 nach den Spielen des Vortags weiter, Talentsuche, Wanderung und
        // 0x07169 (Datei 0x55253–0x552D3) prüfen den neuen Stand = Tag + 1 (DayCounter); ebenso TalentScout.IsTick und die
        // Heilung (PlayerInjuries.IsHealingDay).
        if (MatchEngine.IsDriftDay(DayCounter.Of(day)))
            MatchEngine.DriftTeamValues(this, TeamValueDrift.Weekly);

        // #688 (R688-07): Die Heilung der Managerkader und ihrer Markt-Plätze rechnet 0x07169 je Manager (MainPathDailyRun);
        // [APPROX] Spieler ohne Kaderplatz eines Managers (Computervereine, Vertragslose) heilen weiter am Heiltag.
        HealUnmanaged(day);

        // #678: Training, Markt (Verfall, Gebote, Angebote), Verträge, Heilung und Aufstellung nur am Takttag, je Manager
        // (0x07169, Datei 0x552A6; GameState.DailyRun.cs). Vorher [APPROX] Training Mi/So, Markt und Verträge täglich.
        MainPathDailyRun(day);

        // #688 (R688-07): an Tagen mit Ereignis vor den Zügen je Satz die Neuaufstellung (Datei 0x54D68–0x54DBA, 0x426BC bei
        // 0x54D9E). Vorher [APPROX] täglich am Ende des Tageslaufs.
        MainPathEventDayLineups(day);

        // #678: Listenwechsel 0x494EF in den Managerzügen der Tage mit Ereignis (Datei 0x54E71; nicht am Eintrittstag).
        MainPathListChanges(day);
    }

    /// <summary>Neue Saison: Finanzbuch umstellen, Tageszähler der Finanzen zurücksetzen.</summary>
    partial void OnNewSeasonManager()
    {
        EndLoadCounterOffset(); // Teil 5 pruefung-karte: Datei 0x26F55 setzt den Zähler fest (GameState.LoadQuirk.cs)
        StartFinanceSeason();
        StartStadiumStatisticsSeason(); // Spur heimspiel-statistik (0x55A86)
        // Spur kader-start (A091/A380, Review R14KS-02): nicht verkaufte eigene Spieler von der Transferliste zurück in den Kader,
        // bevor Spiele übertragen und Saisonzähler gelöscht werden. [ORIGINAL] Rückkehr Datei 0x27547–0x27665 in der
        // Satzschleife der Saisonschleife 0x26F20, Löschen der Zähler erst danach ab Datei 0x27A0C.
        TransferMarket.ReturnListedPlayers(this);
        CarryGamesForClub(); // Spur kader-transfer, Teil 5 (A226): Spiele beim Verein (+0x1C) vor dem Zurücksetzen übertragen
        ResetMatchCounters(); // Spur spielberechnung: Spielerzähler je Saison (MatchEngine.Live.cs)
        ResetLeaguePenalties(); // Spur statistiken: Strafminuten der Fair-Play-Tabelle (StatisticsClub.cs, 0x5647A)
        ResetPowerPlayStatistics(); // Spur statistiken-verein: Über-/Unterzahl je Manager (StatisticsClub.cs, 0x5588E)
        // Spur spielberechnung-komplett (#204): große Wanderung der Teamwerte nach Auf- und Abstieg (0x56448).
        // [ORIGINAL] auch im Modus „nur Weltkugel“ (A058, Spur saison Teil 4; docs/analyse/re-runde-13.md): Datei 0x56448 ruft
        // 0x9D1B mit Modus 10 ohne Modusprüfung. Spielstände FILE20 → FILE21 (Modus 1): Ligavereine Kondition/Technik
        // (Felder 0x18–0x1B) gewandert und in den Ligagrenzen (Kaufbeuren 80 78 79 75 → 92 91 90 87, Düsseldorf 94 × 4 an der
        // Obergrenze), alle zwölf Nationalteams dort unverändert, Form 0x1C–0x1E überall neu.
        MatchEngine.DriftTeamValues(this, TeamValueDrift.Season);
    }

    /// <summary>
    /// Neue Saison: Kader, Stammverträge, Leihende, Aufstellungen und Markt gemeinsam umstellen.
    /// Das Training beginnt mit Tag 0 neu (siehe <see cref="CatchUpTraining"/>).
    /// </summary>
    partial void OnNewSeasonTeam()
    {
        StartSquadSeason();
        // #688 (R688-03, Nutzer 09.11.2026: „Sommerpause wie im Original berechnen“): Sommerpause zwischen Saisonende und erstem Tag
        // (Datei 0x564D2–0x56593) nach dem Saisonende der Kader (0x26F20, Datei 0x5645D) und vor den Werbeangeboten des
        // Saisonbeginns (0x3FB47, Datei 0x56650): je Kalendertag Stadionbau, Tagesabschluss 0x3ED01 und Heilung.
        RunSummerBreak();
        StartSponsorSeason();
    }
}
