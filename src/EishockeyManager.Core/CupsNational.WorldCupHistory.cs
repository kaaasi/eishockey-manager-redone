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
/// <item>[ORIGINAL] Schreibstelle Datei 0x311CF–0x31351 (Spur wettbewerbe-komplett Teil 2): Länge n der Folge am ersten
///   Land der Platzierungsspiele (DGROUP 0x5D5B); bei n &gt; 15 rücken alle zwölf Folgen um ein Byte nach vorn (die älteste
///   WM entfällt, Datei 0x31202–0x3125C), n = 15. Dann Byte n = 11 für alle zwölf Länder (0x31266–0x31291) und für die vier
///   Platzierungsspiele ab DGROUP 0xCFFB (Paare 0x5D5B/0x5D5C, der Erste des Paares mit dem besseren Platz) Platz i − 0xCFFB + 1 bzw. + 2
///   (0x312A5–0x31351). Höchstens 16 WMs bleiben also stehen, die jüngsten.</item>
/// <item>Das Remake hält dafür je WM die Abschlussreihenfolge aller zwölf Länder (<see cref="WorldCup.Ranking"/>) – daraus
///   folgt dieselbe Platzierung je Land für die Plätze 1 … 8; Plätze 9 … 12 (im Original alle 11) zählen nirgends. Eingetragen
///   wird nach dem Finale, sobald die Reihenfolge feststeht.</item>
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

    /// <summary>[ORIGINAL] Höchstzahl gespeicherter WMs (Datei 0x311F8: n &gt; 15 → Folge rückt nach vorn).</summary>
    public const int MaxWorldCupRankings = 16;

    /// <summary>Ende einer WM: ihre Reihenfolge für die WM-Statistik festhalten; ab der 17. WM entfällt die älteste.</summary>
    internal void RecordWorldCupRanking(IReadOnlyList<int> ranking)
    {
        if (_worldCupRankings.Count >= MaxWorldCupRankings)
            _worldCupRankings.RemoveRange(0, _worldCupRankings.Count - MaxWorldCupRankings + 1);
        _worldCupRankings.Add([.. ranking]);
    }
}
