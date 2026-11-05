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

    /// <summary>Jeder Tag der Zeitleiste, bevor dessen Spiele ausgetragen werden.</summary>
    partial void OnDayManager(int day)
    {
        // Spur wettbewerbe-weltkugel: Im Modus „nur Weltkugel“ gibt es keine Vereinssaison – keine Finanzen,
        // kein Training, kein Markt, keine Vertragsmeldungen, keine Ligastatistik (Datei 0x54CEE: kein Tageslauf).
        // Spur nationalteam Runde 13 (A009, L9): Länderwerte aus der Aufstellung des Bundestrainers (0x952C mit Manager 5,
        // Tageslauf Datei 0x54F9C–0x54FBF und nach dem Menü an WM-Tagen Datei 0x555D6) – vor der Ernennung des Tages wie im
        // Original (Ernennung erst nach dem Tag, Datei 0x553A2). Im Tageslauf der Vereinssaison vor der kleinen Wanderung
        // (siehe OnDayClubSeason, #594); im Modus „nur Weltkugel“ ([APPROX], kein Tageslauf im Original) hier.
        if (!WorldOnly)
            OnDayClubSeason(day);
        else
            UpdateNationalTeamValues();

        // Paket „wettbewerbe“: an den Tagen mit Markierung 255 im Modus 4 den Bundestrainer ernennen (Spur
        // bundestrainer-modus4), dann die Werte Deutschlands ohne Bundestrainer neu auswürfeln.
        if (day is SeasonCalendar.ClubSeasonEnd or SeasonCalendar.SeasonEnd)
            OnNationalMarkerDay();
    }

    /// <summary>Tagesablauf der Vereinssaison (Finanzen, Training, Markt, Verträge, Statistik).</summary>
    private void OnDayClubSeason(int day)
    {
        // Spur transfer-vertraege-komplett: offene Verhandlungen am Saisonende enden vor dem ersten Tag wie „Abbruch“ (0x27D91).
        ResolveRenewals();

        // Finanzen: Tages-, Wochen- und Monatswechsel bis einschließlich heute
        AdvanceFinanceTo(day);

        // Training: jeder Trainingstag bis heute genau einmal (vorher nur beim Öffnen der Mannschafts-Bildschirme)
        CatchUpTraining();

        // Transfermarkt: täglich Listenwechsel und Angebote (0x54E71, 0x072DD; Spur kader-transfer, Teil 4)
        TransferMarket.CatchUp(this);

        // Verträge: Spieler im letzten Vertragsjahr kündigen an, ob sie verlängern (Spur „kader“, R2-11)
        AnnounceContractEnds(day);

        // Paket „wettbewerbe“: Ewige Tabelle, Ewige Bilanz und „Früher“ schreibt seit Spur saisonwechsel Teil 3 (S-K4)
        // FinishSeason nach Zwangsabstieg und Austausch fort (em.exe Datei 0x563AF).

        // Spur nationalteam Runde 14 (#594, R13IN-01): [ORIGINAL] 0x952C(Manager 5, 1) liegt in der Tagesfunktion 0x54C9F bei
        // Datei 0x54F9C–0x54FBF, die kleine Wanderung danach bei Datei 0x5525A–0x55292 (dazwischen kein Sprung aus dem Bereich).
        // Die Wanderung würfelt die Form 0x1C/0x1D Deutschlands neu; an Wandertagen gilt also die gewürfelte Form, nicht die
        // der Aufstellung. Auch die Heilung (0x07169, Datei 0x552A6) und die Aufstellungsautomatik (Datei 0x07C07) folgen erst
        // danach. [APPROX] Das Original rechnet 0x952C nach den Menüs der Manager; Finanzen, Training und Markt des Remakes
        // stehen davor wie bisher (Reihenfolge zu ihnen nicht einzeln gelesen).
        UpdateNationalTeamValues();

        // Spur spielberechnung-komplett (#204): kleine Wanderung der Teamwerte zweimal je Woche (0x5525A–0x55292)
        if (MatchEngine.IsDriftDay(day))
            MatchEngine.DriftTeamValues(this, TeamValueDrift.Weekly);

        // Spur spielberechnung-komplett, Teil 3: Verletzungen heilen wochenweise (0x7C12 im Tageslauf 0x07169)
        HealInjuries(day);

        // Spur wettbewerbe-spiel, Teil 8: automatische Aufstellung am Ende des Tageslaufs (Datei 0x07C07, A068)
        ApplyAutoLineups();
    }

    /// <summary>Neue Saison: Finanzbuch umstellen, Tageszähler der Finanzen zurücksetzen.</summary>
    partial void OnNewSeasonManager()
    {
        StartFinanceSeason();
        StartStadiumStatisticsSeason(); // Spur heimspiel-statistik (0x55A86)
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
    partial void OnNewSeasonTeam() => StartSquadSeason();
}
