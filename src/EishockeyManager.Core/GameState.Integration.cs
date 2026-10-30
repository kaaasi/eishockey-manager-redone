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
        _ = Stocks;
        foreach (var manager in Managers)
        {
            RenewOffers(manager);
            PrepareTraining(manager);
        }

        // Paket „wettbewerbe“: DEB-Pokal (später Europapokal, WM) nach dem Spielmodus anmelden und auslosen.
        InitializeCompetitions();
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
        // Finanzen: Tages-, Wochen- und Monatswechsel bis einschließlich heute
        AdvanceFinanceTo(day);

        // Training: jeder Trainingstag bis heute genau einmal (vorher nur beim Öffnen der Mannschafts-Bildschirme)
        CatchUpTraining();

        // Transfermarkt: je gespieltem Ligaspieltag neue Angebote und Gebote
        TransferMarket.CatchUp(this);

        // Verträge: Spieler im letzten Vertragsjahr kündigen an, ob sie verlängern (Spur „kader“, R2-11)
        AnnounceContractEnds(day);

        // Paket „wettbewerbe“: Ewige Tabelle, Ewige Bilanz und „Früher“ am Saisonende fortschreiben
        // (vor der Titelvergabe in FinishSeason; Liga, Pokale und Abschlusstabellen stehen dann fest).
        if (day == SeasonCalendar.SeasonEnd)
            RecordSeasonStatistics();
    }

    /// <summary>Neue Saison: Finanzbuch umstellen, Tageszähler der Finanzen zurücksetzen.</summary>
    partial void OnNewSeasonManager()
    {
        StartFinanceSeason();
        StartStadiumStatisticsSeason(); // Spur heimspiel-statistik (0x55A86)
        ResetMatchCounters(); // Spur spielberechnung: Spielerzähler je Saison (MatchEngine.Live.cs)
    }

    /// <summary>
    /// Neue Saison: Kader, Stammverträge, Leihende, Aufstellungen und Markt gemeinsam umstellen.
    /// Das Training beginnt mit Tag 0 neu (siehe <see cref="CatchUpTraining"/>).
    /// </summary>
    partial void OnNewSeasonTeam() => StartSquadSeason();
}
