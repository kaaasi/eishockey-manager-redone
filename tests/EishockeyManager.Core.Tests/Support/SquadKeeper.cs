using EishockeyManager.Core;

namespace EishockeyManager.Tests.Support;

/// <summary>Kader der Managervereine über mehrere Saisons spielfähig halten (Testhilfe).</summary>
internal static class SquadKeeper
{
    /// <summary>
    /// Kader halten (Spur heimspiel-statistik, #136): nimmt jedes angebotene Verlängerungsangebot des Managervereins zur
    /// Forderung an (Annahmeweg „Forderung steht im Feld“, 0x4ABF4: kein Zufallszug, keine Sperre). Seit „Weniger als neun Spieler“ (em.exe 0x54420) zahlt ein Manager, dessen Startverträge nach zwei
    /// Saisons auslaufen, je Spiel 200.000 DM und wird entlassen. Proben, die einen passiven Manager über mehrere Saisons
    /// brauchen (Eintritt, Finanzkorridor, Pokale), halten damit seinen Kader; sonst greift er weiter nicht ein.
    /// Zieht aus einem eigenen Generator, damit keine Zufallsquelle des Spiels verbraucht wird. Gemeinsam für Core- und UI-Tests
    /// (UI: Link-Datei wie <c>GameDataFact.cs</c>).
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

    /// <summary>
    /// <see cref="KeepSquad"/> für alle Manager (Abläufe über mehrere Saisons durch die Oberfläche).
    /// </summary>
    /// <param name="extendExpired">R12K-03 (#532): Standard nur angebotene Verlängerungen; <c>true</c> verlängert auch Spieler im
    /// letzten Vertragsjahr ohne Angebot.</param>
    public static void KeepSquads(GameState game, bool extendExpired = false)
    {
        foreach (var manager in game.Managers)
            KeepSquad(game, manager, extendExpired);
    }
}
