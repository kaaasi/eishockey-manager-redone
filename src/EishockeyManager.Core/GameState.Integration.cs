namespace EishockeyManager.Core;

/// <summary>
/// Verbindet die Pakete „manager“ und „mannschaft“ mit der Zeitleiste des Pakets „saison“
/// (Anschlusspunkte in <c>SeasonFlow.cs</c>). Die Pakete wurden parallel entwickelt; hier laufen sie zusammen.
/// </summary>
public sealed partial class GameState
{
    /// <summary>Jeder Tag der Zeitleiste, bevor dessen Spiele ausgetragen werden.</summary>
    partial void OnDayManager(int day)
    {
        // Finanzen: Tages-, Wochen- und Monatswechsel bis einschließlich heute
        AdvanceFinanceTo(day);

        // Training: jeder Trainingstag bis heute genau einmal (vorher nur beim Öffnen der Mannschafts-Bildschirme)
        CatchUpTraining();

        // Transfermarkt: je gespieltem Ligaspieltag neue Angebote und Gebote
        TransferMarket.CatchUp(this);
    }

    /// <summary>Neue Saison: Finanzbuch umstellen, Tageszähler der Finanzen zurücksetzen.</summary>
    partial void OnNewSeasonManager() => StartFinanceSeason();

    /// <summary>
    /// Neue Saison für den Kader: Transfermarkt zählt die Spieltage neu. [APPROX] Altern, Vertragsende und neue
    /// Talente sind im Paket „mannschaft“ noch nicht umgesetzt; das Training beginnt mit Tag 0 neu
    /// (siehe <see cref="CatchUpTraining"/>).
    /// </summary>
    partial void OnNewSeasonTeam() => TransferMarket.StartSeason();
}
