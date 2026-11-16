namespace EishockeyManager.Core;

/// <summary>Spielstand – Spur kader-transfer, Teil 4: Zufallsquelle der Tendenz neuer Kaderplätze (F113).</summary>
public sealed partial class GameState
{
    private GameRandom? _trendRandom;

    /// <summary>
    /// Eigene Zufallsquelle für die Nachbearbeitung neuer Kaderplätze (Datei 0x4907A, <see cref="Player.RollTrend"/>).
    /// [APPROX] Das Original zieht aus demselben Generator wie alles andere. Spur zufallsstrom (A725, N3 = B): Markt
    /// (<see cref="TransferPoolRules.RollSlotFields"/>) und Spielstart (<see cref="ApplyStartSalaries"/>) ziehen seit Teil 1 aus
    /// <see cref="Random"/>; die Quelle bleibt nur noch für Talent (A388), neuen Manager (A464) und Tagesablauf (A524), bis die
    /// Teile 2 und 3 sie umstellen, und entfällt danach. Ältere Spielstände beginnen die Quelle beim ersten Zug.
    /// </summary>
    public GameRandom TrendRandom
    {
        get => _trendRandom ??= new GameRandom(unchecked(Season * 7_919 + Managers.Sum(m => m.ClubIndex * 41 + m.Head * 13) + 23));
        init => _trendRandom = value;
    }
}
