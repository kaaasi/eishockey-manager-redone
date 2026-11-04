using EishockeyManager.Core;

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

    /// <summary>
    /// Kader halten (Spur heimspiel-statistik, #136): nimmt jedes angebotene Verlängerungsangebot des Managervereins zur
    /// Forderung an (Annahmeweg „Forderung steht im Feld“, 0x4ABF4: kein Zufallszug, keine Sperre). Seit „Weniger als neun Spieler“ (em.exe 0x54420) zahlt ein Manager, dessen Startverträge nach zwei
    /// Saisons auslaufen, je Spiel 200.000 DM und wird entlassen. Proben, die einen passiven Manager über mehrere Saisons
    /// brauchen (Eintritt, Finanzkorridor, Pokale), halten damit seinen Kader; sonst greift er weiter nicht ein.
    /// Zieht aus einem eigenen Generator, damit keine Zufallsquelle des Spiels verbraucht wird.
    /// </summary>
    public static void KeepSquad(GameState game, Manager manager, bool extendExpired = false)
    {
        var club = game.Clubs[manager.ClubIndex];
        foreach (var p in club.Players.Where(p => p.Notice == ContractNotice.Offered && p.ContractYears == 1).ToArray())
            Contracts.Negotiate(game, manager, p, p.NoticeYears, Contracts.Demand(p) + 100, false, demandFilled: true);
        // Runde 12, U4 (0x0767A): Ein Angebot verfällt nach einigen Tagen. Standard (R12K-03, #532): nur angebotene Verlängerungen,
        // damit passive Proben den Kaderschwund zeigen. Proben, die den Kader trotzdem voll brauchen (Abdrücke, Entlassung
        // vermeiden), setzen <paramref name="extendExpired"/> = true: dann wird jeder Spieler im letzten Vertragsjahr ohne
        // Angebot um zwei Jahre verlängert (Forderung steht im Feld), soweit nicht gesperrt oder im Ruhestand.
        if (!extendExpired)
            return;
        foreach (var p in club.Players.Where(p => p.Notice == ContractNotice.None && p.ContractYears == 1 && !p.OnLoan
                     && p.NegotiationLock == 0).ToArray())
            Contracts.Negotiate(game, manager, p, 2, Contracts.Demand(p) + 100, false, demandFilled: true);
    }

    /// <summary>Die ganze Hauptrunde.</summary>
    public static void PlayRegularSeason(GameState game)
    {
        while (!game.RegularSeasonOver && game.Phase == SeasonPhase.Running)
            PlayMatchDay(game);
    }
}
