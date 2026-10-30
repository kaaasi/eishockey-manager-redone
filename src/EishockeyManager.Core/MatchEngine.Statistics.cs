using EishockeyManager.Formats;

namespace EishockeyManager.Core;

/// <summary>
/// Spielerstatistik aus dem Endstand für Vereine ohne Manager (Spur spielberechnung, Teil 1).
/// [ORIGINAL] Nach jedem Spieltag ruft 0x52CF für jede Paarung beider Vereine 0x5028(Verein, Tore, Gegentore) auf;
/// Vereine mit Manager (Vereinsfeld 0x16 ≠ 0) und Nationalteams (Index ≥ 0x40) überspringt es – deren Ereignisse
/// entstehen im Spiel (0x541D9 Torschütze/Vorlage, 0x35818 Strafen). Pokale werten das gespeicherte Ergebnis, eine
/// Penaltymarke (+30) vorher abgezogen (0x5422–0x545D). Noch nicht an die Spieltage angeschlossen.
/// </summary>
public static class MatchStatistics
{
    /// <summary>[ORIGINAL] Höchstens 50 Kandidaten für Tore und Vorlagen (Datei 0x5146: Abbruch über 0x31).</summary>
    public const int MaxCandidates = 50;

    /// <summary>[ORIGINAL] Höchstens drei Torhüter je Verein (Datei 0x4E36).</summary>
    public const int MaxGoalies = 3;

    /// <summary>[ORIGINAL] Spieldauer in Minuten, auf die Torhüter verteilt (Datei 0x4FA6: 0x3C).</summary>
    public const int MatchMinutes = 60;

    /// <summary>
    /// [ORIGINAL] Grenze der Vorlagenzüge je Kandidatenzahl n (DGROUP 0x4831: 16, 2, 3 … 10; ab n = 10 gilt 10),
    /// minus 1; über 2 noch einmal minus 1 (Datei 0x5168–0x518A).
    /// </summary>
    private static readonly int[] PickTable = [16, 2, 3, 4, 5, 6, 7, 8, 9, 10];

    /// <summary>
    /// Rollenwert (Spielerfeld 0x18): Torhüter 0, Verteidiger 25–41, Stürmer 75–91 (<c>docs/reverse-engineering.md</c>).
    /// [APPROX] Das Remake führt den Wert nicht; es nimmt die Mitte der Spanne.
    /// </summary>
    public static int RoleValue(PlayerRole role) => role switch
    {
        PlayerRole.Goalkeeper => 0,
        PlayerRole.Defender => 33,
        _ => 83,
    };

    /// <summary>
    /// Tore und Vorlagen der Feldspieler. [ORIGINAL] Datei 0x5077–0x52CB:
    /// <list type="number">
    /// <item>Ohne eigenes Tor passiert nichts – auch die Spiele zählen dann nicht (Datei 0x5077).</item>
    /// <item>Jeder Spieler des Vereins: Spiele + 1; Feldspieler werden Kandidat, wenn Rollenwert + random(5, 25) &gt;
    ///   random(10, 99) (Stürmer fast immer, Verteidiger etwa zur Hälfte).</item>
    /// <item>Unter zwei Kandidaten keine Tore und Vorlagen.</item>
    /// <item>Je Tor: mit 1/5 (random(0, 4) = 0) ohne Torschützen. Sonst Zug random(0, g) mit der Grenze g aus
    ///   <see cref="PickTable"/>: bei 0 neuer Versuch, wenn g ≠ 0 und n &gt; random(0, 1) + 2, sonst Ende. Bei ≠ 0 ein
    ///   Kandidat random(0, n − 1); der erste ist der Torschütze (Tore + 1), danach ein anderer Kandidat (höchstens 250
    ///   Versuche) mit Vorlage + 1. Höchstens eine Vorlage je Tor.</item>
    /// </list>
    /// [APPROX] Reihenfolge der Spieler: Kader des Remakes statt der Reihenfolge im Spielerpool des Originals (nur 200
    /// Spieler, Computervereine haben dort nur ihre Transferspieler). Tore ohne Obergrenze 255.
    /// </summary>
    public static void CreditSkaters(IReadOnlyList<Player> players, int goals, Random random)
    {
        if (goals <= 0)
            return;
        var candidates = new List<Player>();
        foreach (var p in players)
        {
            p.Games++;
            if (p.Role == PlayerRole.Goalkeeper)
                continue;
            if (RoleValue(p.Role) + OriginalMatch.Rnd(random, 5, 25) > OriginalMatch.Rnd(random, 10, 99))
            {
                candidates.Add(p);
                if (candidates.Count >= MaxCandidates)
                    break;
            }
        }
        var n = candidates.Count;
        if (n <= 1)
            return;
        for (var goal = 0; goal < goals; goal++)
        {
            var limit = n > 9 ? 10 : PickTable[n] - 1;
            if (limit > 2)
                limit--;
            if (OriginalMatch.Rnd(random, 0, 4) == 0)
                continue;
            var scorer = -1;
            while (true)
            {
                if (OriginalMatch.Rnd(random, 0, limit) == 0)
                {
                    if (limit != 0 && n > OriginalMatch.Rnd(random, 0, 1) + 2)
                        continue;
                    break;
                }
                int pick;
                var tries = 0;
                do
                    pick = OriginalMatch.Rnd(random, 0, n - 1);
                while (tries++ < 250 && scorer >= 0 && pick == scorer);
                var player = candidates[pick];
                if (scorer < 0)
                    player.Goals++;
                else
                    player.Assists++;
                if (scorer < 0)
                {
                    scorer = pick;
                    continue;
                }
                break;
            }
        }
    }

    /// <summary>
    /// Gegentore und Minuten der Torhüter, nur in der Liga. [ORIGINAL] Datei 0x4DB6 (vor der Torprüfung aufgerufen,
    /// also auch bei 0 Gegentoren): die ersten höchstens drei Torhüter des Vereins; mit mehreren Torhütern mit 1/9
    /// (random(0, 8) = 0) ein Wechsel: der erste bekommt random(0, 100) % der Gegentore, der zweite den Rest; Minuten
    /// dann random(2, 100) · 60 / 100 und der Rest bis 60. Sonst alles für den ersten. Gegentore nur, solange das Byte
    /// nicht über 255 steigt (übernommen).
    /// [APPROX] Reihenfolge: der aufgestellte Torhüter zuerst, dann die übrigen in Kaderreihenfolge (Original: Reihenfolge
    /// im Spielerpool).
    /// </summary>
    public static void CreditGoalies(IReadOnlyList<Player> goalies, int goalsAgainst, Random random)
    {
        var count = Math.Min(goalies.Count, MaxGoalies);
        if (count == 0)
            return;
        var percent = 100;
        if (count > 1 && OriginalMatch.Rnd(random, 0, 8) == 0)
            percent = OriginalMatch.Rnd(random, 0, 100);
        var first = goalsAgainst * percent / 100;
        if (goalies[0].GoalsAgainst + first <= 255)
            goalies[0].GoalsAgainst += first;
        if (count > 1 && goalies[1].GoalsAgainst + goalsAgainst - first <= 255)
            goalies[1].GoalsAgainst += goalsAgainst - first;
        var minutesPercent = percent == 100 ? 100 : OriginalMatch.Rnd(random, 2, 100);
        var minutes = minutesPercent * MatchMinutes / 100;
        goalies[0].GoalieMinutes += minutes;
        if (count > 1)
            goalies[1].GoalieMinutes += MatchMinutes - minutes;
    }
}
