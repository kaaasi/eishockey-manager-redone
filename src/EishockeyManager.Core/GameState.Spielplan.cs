namespace EishockeyManager.Core;

/// <summary>
/// Spur wettbewerbe-rest, Teil 3 (A113): Liga-Spielplan der neuen Saison über die Satzfolge.
/// </summary>
public sealed partial class GameState
{
    /// <summary>
    /// Saisonwechsel nach <see cref="AdvanceLeagueSlots"/>: Spielplan je Liga aus der festen Tabelle über die Satzfolge
    /// (<see cref="Schedule.ForSlots"/>, em.exe Datei 0x4E7C7).
    /// [APPROX] Das Original würfelt hier nichts. Das Remake zog bisher je Liga eine Mischung der Mitglieder aus
    /// <see cref="Random"/> (für den festen Plan bedeutungslos); die Züge bleiben, damit die übrigen Spielverläufe (Liga,
    /// Pokale, Transfers) unverändert bleiben.
    /// </summary>
    private void BuildLeagueSchedules()
    {
        foreach (var league in Enum.GetValues<League>())
        {
            var members = LeagueClubs(league).Select(c => c.Index).ToList();
            for (var i = members.Count - 1; i > 0; i--)
                Random.Next(i + 1);
            Schedules[league] = Schedule.ForSlots(LeagueSlots(league), rounds: 4);
        }
    }
}
