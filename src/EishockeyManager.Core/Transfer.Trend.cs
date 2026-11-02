namespace EishockeyManager.Core;

/// <summary>Spielstand – Spur kader-transfer, Teil 4: Zufallsquelle der Tendenz neuer Kaderplätze (F113).</summary>
public sealed partial class GameState
{
    private GameRandom? _trendRandom;

    /// <summary>
    /// Eigene Zufallsquelle für die Nachbearbeitung neuer Kaderplätze (Datei 0x4907A, <see cref="Player.RollTrend"/>).
    /// [APPROX] Das Original zieht aus demselben Generator wie alles andere; das Remake hält die Züge aus der Hauptquelle
    /// heraus, damit Markt, Spiele und Proben ihre Folgen behalten (wie <see cref="ContractRandom"/>). Ältere Spielstände
    /// beginnen die Quelle beim ersten Zug.
    /// </summary>
    public GameRandom TrendRandom
    {
        get => _trendRandom ??= new GameRandom(unchecked(Season * 7_919 + Managers.Sum(m => m.ClubIndex * 41 + m.Head * 13) + 23));
        init => _trendRandom = value;
    }
}
