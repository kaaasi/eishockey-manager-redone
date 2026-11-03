using EishockeyManager.Core;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Kader halten (Spur heimspiel-statistik, #136), Gegenstück zu <c>TestSeason.KeepSquad</c> der Core-Tests: Jedes
/// angebotene Verlängerungsangebot der Managervereine wird zur Forderung angenommen. Seit „Weniger als neun Spieler“
/// (em.exe 0x54420) zahlt ein Manager mit ausgelaufenen Startverträgen je Spiel 200.000 DM und wird entlassen; Abläufe
/// über mehrere Saisons durch die Oberfläche brauchen einen spielfähigen Kader. Eigener Generator: keine Zufallsquelle
/// des Spiels wird verbraucht.
/// </summary>
internal static class KeepSquadHelper
{
    public static void KeepSquads(GameState game)
    {
        foreach (var manager in game.Managers)
        {
            foreach (var p in game.Clubs[manager.ClubIndex].Players
                         .Where(p => p.Notice == ContractNotice.Offered && p.ContractYears == 1).ToArray())
                Contracts.Negotiate(game, manager, p, p.NoticeYears, Contracts.Demand(p) + 100, false, demandFilled: true);
            // Runde 12, U4 (0x0767A): Ein Angebot verfällt nach einigen Tagen; wer sich nicht mehr meldet, wird um zwei Jahre
            // verlängert (Forderung steht im Feld), soweit nicht gesperrt oder im Ruhestand (wie TestSeason.KeepSquad).
            foreach (var p in game.Clubs[manager.ClubIndex].Players
                         .Where(p => p.Notice == ContractNotice.None && p.ContractYears == 1 && !p.OnLoan && p.NegotiationLock == 0)
                         .ToArray())
                Contracts.Negotiate(game, manager, p, 2, Contracts.Demand(p) + 100, false, demandFilled: true);
        }
    }
}
