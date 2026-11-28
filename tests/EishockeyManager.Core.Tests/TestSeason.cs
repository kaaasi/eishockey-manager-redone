using EishockeyManager.Core;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Zeit vorspulen wie im Spiel: über die Zeitleiste (<see cref="GameState.Continue"/>), damit Finanzen,
/// Training und alle anderen Tagesabläufe mitlaufen. <see cref="GameState.PlayNextMatchDay"/> allein
/// spielt nur die Ligaspiele und lässt den Kalender stehen.
/// </summary>
/// <summary>
/// Alle Nachrichten, die ein Spiel je abgelegt hat. Der Nachrichtenspeicher des Originals (R-K6, pixel-marker Teil 3) hat 24 Plätze und
/// räumt Meldungen nach drei Tagen; Proben, die über Wochen zählen, was gemeldet wurde, sammeln hier nach jedem Tag.
/// </summary>
public sealed class MessageLog
{
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<GameState, MessageLog> Logs = new();
    private readonly HashSet<Message> _seen = new(ReferenceEqualityComparer.Instance);

    private readonly Dictionary<Manager, List<Message>> _byManager = [];

    public List<Message> All { get; } = [];

    /// <summary>Nachrichten, die <paramref name="manager"/> je bekommen hat.</summary>
    public IReadOnlyList<Message> Of(Manager manager) => _byManager.TryGetValue(manager, out var list) ? list : [];

    public static MessageLog For(GameState game) => Logs.GetOrCreateValue(game);

    /// <summary>Nimmt die neuen Nachrichten aller Manager auf; nach jedem <see cref="GameState.Continue"/> aufrufen.</summary>
    public void Collect(GameState game)
    {
        foreach (var manager in game.Managers)
            foreach (var message in game.Inbox(manager))
                if (_seen.Add(message))
                {
                    All.Add(message);
                    if (!_byManager.TryGetValue(manager, out var list))
                        _byManager[manager] = list = [];
                    list.Add(message);
                }
    }
}

public static class TestSeason
{
    /// <summary>Bis einschließlich des nächsten Ligaspieltags.</summary>
    public static void PlayMatchDay(GameState game)
    {
        var played = game.PlayedMatchDays;
        while (game.PlayedMatchDays == played && game.Phase == SeasonPhase.Running && !game.RegularSeasonOver)
        {
            game.Continue();
            MessageLog.For(game).Collect(game);
        }
    }

    /// <summary>Kader halten: <see cref="SquadKeeper.KeepSquad"/> (gemeinsame Testhilfe).</summary>
    public static void KeepSquad(GameState game, Manager manager, bool extendExpired = false) =>
        SquadKeeper.KeepSquad(game, manager, extendExpired);

    /// <summary>
    /// Über die Zeitleiste, bis der Tag <paramref name="day"/> gespielt ist (<see cref="GameState.Continue"/> spielt den aktuellen
    /// Tag und die folgenden bis zum nächsten Managerspiel; danach ist <see cref="GameState.Day"/> der nächste offene Tag).
    /// </summary>
    public static void RunThroughDay(GameState game, int day)
    {
        while (game.Day <= day && game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver)
        {
            game.Continue();
            MessageLog.For(game).Collect(game);
        }
    }

    /// <summary>
    /// Zeitleiste, bis <paramref name="done"/> gilt – höchstens <paramref name="maxSteps"/> Aufrufe von
    /// <see cref="GameState.Continue"/> und nur in der laufenden Saison (Abbruchzähler, AGENTS.md §5). Für Aufbauten wie „bis ein
    /// Gebot auf dem eigenen Markt-Platz liegt“: Angebotswurf und Gebotsverfall laufen im Tagesablauf 0x07169 am Takttag
    /// (<see cref="GameState.DailyRunMarket"/>), der Listenwechsel je Managerzug (<see cref="TransferMarket.ListChangeTurn"/>).
    /// </summary>
    public static bool RunUntil(GameState game, Func<bool> done, int maxSteps = 400)
    {
        for (var i = 0; i < maxSteps && !done() && game.Phase == SeasonPhase.Running; i++)
        {
            game.Continue();
            MessageLog.For(game).Collect(game);
        }
        return done();
    }

    /// <summary>Die ganze Hauptrunde.</summary>
    public static void PlayRegularSeason(GameState game)
    {
        while (!game.RegularSeasonOver && game.Phase == SeasonPhase.Running)
            PlayMatchDay(game);
    }
}
