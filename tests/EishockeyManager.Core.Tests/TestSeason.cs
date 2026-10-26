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

    /// <summary>Die ganze Hauptrunde.</summary>
    public static void PlayRegularSeason(GameState game)
    {
        while (!game.RegularSeasonOver && game.Phase == SeasonPhase.Running)
            PlayMatchDay(game);
    }
}
