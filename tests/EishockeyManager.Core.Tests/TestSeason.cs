using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Zeit vorspulen wie im Spiel: über die Zeitleiste (<see cref="GameState.Continue"/>), damit Finanzen,
/// Training und alle anderen Tagesabläufe mitlaufen. <see cref="GameState.PlayNextMatchDay"/> allein
/// spielt nur die Ligaspiele und lässt den Kalender stehen.
/// </summary>
public static class TestSeason
{
    /// <summary>Bis einschließlich des nächsten Ligaspieltags.</summary>
    public static void PlayMatchDay(GameState game)
    {
        var played = game.PlayedMatchDays;
        while (game.PlayedMatchDays == played && game.Phase == SeasonPhase.Running && !game.RegularSeasonOver)
            game.Continue();
    }

    /// <summary>
    /// Kader halten (Spur heimspiel-statistik, #136): nimmt jedes angebotene Verlängerungsangebot des Managervereins zur
    /// Forderung an (Annahmeweg „Forderung steht im Feld“, 0x4ABF4: kein Zufallszug, keine Sperre). Seit „Weniger als neun Spieler“ (em.exe 0x54420) zahlt ein Manager, dessen Startverträge nach zwei
    /// Saisons auslaufen, je Spiel 200.000 DM und wird entlassen. Proben, die einen passiven Manager über mehrere Saisons
    /// brauchen (Eintritt, Finanzkorridor, Pokale), halten damit seinen Kader; sonst greift er weiter nicht ein.
    /// Zieht aus einem eigenen Generator, damit keine Zufallsquelle des Spiels verbraucht wird.
    /// </summary>
    public static void KeepSquad(GameState game, Manager manager)
    {
        var club = game.Clubs[manager.ClubIndex];
        foreach (var p in club.Players.Where(p => p.Notice == ContractNotice.Offered && p.ContractYears == 1).ToArray())
            Contracts.Negotiate(game, manager, p, p.NoticeYears, Contracts.Demand(p) + 100, false, demandFilled: true);
    }

    /// <summary>Die ganze Hauptrunde.</summary>
    public static void PlayRegularSeason(GameState game)
    {
        while (!game.RegularSeasonOver && game.Phase == SeasonPhase.Running)
            PlayMatchDay(game);
    }
}
