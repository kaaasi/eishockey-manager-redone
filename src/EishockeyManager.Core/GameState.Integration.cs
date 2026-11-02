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
        if (!WorldOnly)
            OnDayClubSeason(day);

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
        // [APPROX] nicht im Modus „nur Weltkugel“ (dort kein Tageslauf, 0x54CEE; Saisonwechsel nicht geprüft).
        if (!WorldOnly)
            MatchEngine.DriftTeamValues(this, TeamValueDrift.Season);
    }

    /// <summary>
    /// Neue Saison: Kader, Stammverträge, Leihende, Aufstellungen und Markt gemeinsam umstellen.
    /// Das Training beginnt mit Tag 0 neu (siehe <see cref="CatchUpTraining"/>).
    /// </summary>
    partial void OnNewSeasonTeam() => StartSquadSeason();
}
