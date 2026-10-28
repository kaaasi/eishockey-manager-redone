namespace EishockeyManager.Core;

/// <summary>
/// Verbindet die Pakete „manager“ und „mannschaft“ mit der Zeitleiste des Pakets „saison“
/// (Anschlusspunkte in <c>SeasonFlow.cs</c>). Die Pakete wurden parallel entwickelt; hier laufen sie zusammen.
/// </summary>
public sealed partial class GameState
{
    /// <summary>
    /// Spielstart nach der Kaderanlage: Börse, Werbeangebote und Trainingspläne für alle Manager vorbereiten.
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
    }

    /// <summary>Jeder Tag der Zeitleiste, bevor dessen Spiele ausgetragen werden.</summary>
    partial void OnDayManager(int day)
    {
        // Finanzen: Tages-, Wochen- und Monatswechsel bis einschließlich heute
        AdvanceFinanceTo(day);

        // Training: jeder Trainingstag bis heute genau einmal (vorher nur beim Öffnen der Mannschafts-Bildschirme)
        CatchUpTraining();

        // Transfermarkt: je gespieltem Ligaspieltag neue Angebote und Gebote
        TransferMarket.CatchUp(this);

        // Verträge: Spieler im letzten Vertragsjahr kündigen an, ob sie verlängern (Spur „kader“, R2-11)
        AnnounceContractEnds(day);
    }

    /// <summary>Neue Saison: Finanzbuch umstellen, Tageszähler der Finanzen zurücksetzen.</summary>
    partial void OnNewSeasonManager() => StartFinanceSeason();

    /// <summary>
    /// Neue Saison: Kader, Stammverträge, Leihende, Aufstellungen und Markt gemeinsam umstellen.
    /// Das Training beginnt mit Tag 0 neu (siehe <see cref="CatchUpTraining"/>).
    /// </summary>
    partial void OnNewSeasonTeam() => StartSquadSeason();
}
