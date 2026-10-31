namespace EishockeyManager.Core;

/// <summary>
/// Spielstand – Platzierungen aller bisherigen Weltmeisterschaften für die WM-Statistik (Spur nationalteam-2,
/// em.exe Datei 0x3ADE1).
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>[ORIGINAL] Das Original führt je Land im Satz DGROUP 0x8257 + 0x50 · Verein ab Feld 0x34 eine Bytefolge mit der
///   Platzierung jeder bisherigen WM (0 beendet die Folge; Datei 0x3B22F–0x3B245). Die WM-Statistik zählt je Platz
///   1 … 8 die Punkte 9 − Platz und zeichnet die Flagge in Spalte k (WM k) und Zeile Platz; Plätze ab 9 zählen nicht
///   (Datei 0x3B1BA–0x3B229).</item>
/// <item>Das Remake hält dafür je WM die Abschlussreihenfolge aller zwölf Länder (<see cref="WorldCup.Ranking"/>) – daraus
///   folgt dieselbe Platzierung je Land. [APPROX] Wo das Original die Bytes schreibt, ist nicht gelesen; das Remake trägt
///   die WM ein, sobald ihre Reihenfolge feststeht (nach dem Finale).</item>
/// </list>
/// </remarks>
public sealed partial class GameState
{
    /// <summary>Abschlussreihenfolge (Vereinsindizes, Platz 1 zuerst) jeder bisherigen WM, älteste zuerst.</summary>
    private readonly List<int[]> _worldCupRankings = [];

    /// <summary>Abschlussreihenfolgen aller bisherigen Weltmeisterschaften, älteste zuerst.</summary>
    public IReadOnlyList<IReadOnlyList<int>> WorldCupRankings => _worldCupRankings;

    /// <summary>Platz eines Landes (1 …) in WM <paramref name="index"/> oder 0, wenn es dort nicht geführt ist.</summary>
    public int WorldCupPlace(int index, int nation) => Array.IndexOf(_worldCupRankings[index], nation) + 1;

    /// <summary>
    /// [ORIGINAL] WM-Punkte eines Landes: je Teilnahme 9 − Platz für die Plätze 1 … 8 (Datei 0x3B216–0x3B229).
    /// </summary>
    public int WorldCupPoints(int nation) =>
        Enumerable.Range(0, _worldCupRankings.Count).Select(i => WorldCupPlace(i, nation)).Where(p => p is >= 1 and <= 8).Sum(p => 9 - p);

    /// <summary>Ende einer WM: ihre Reihenfolge für die WM-Statistik festhalten.</summary>
    internal void RecordWorldCupRanking(IReadOnlyList<int> ranking) => _worldCupRankings.Add([.. ranking]);
}
