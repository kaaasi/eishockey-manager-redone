using EishockeyManager.Formats;

namespace EishockeyManager.Core;

/// <summary>Sortierung der Feldspieler in „Die besten …“ (Knöpfe TORSCH., VORLEGER, SCORER; Texte DGROUP 0x6B1 ff.).</summary>
public enum SkaterRanking
{
    /// <summary>„TORSCH.“: nur Tore.</summary>
    Goals = 0,

    /// <summary>„VORLEGER“: nur Vorlagen.</summary>
    Assists = 1,

    /// <summary>„SCORER“: Tore + Vorlagen (Vorgabe, auch für die Einzelprämie).</summary>
    Scorer = 2,
}

/// <summary>Eine Zeile der Ranglisten „Die besten …“: Spieler, Verein und Platz innerhalb der eigenen Liste.</summary>
/// <param name="Rank">Platz in der Liste der Feldspieler bzw. Torhüter (ab 1), gezählt über alle Spieler der Liga.</param>
public sealed record PlayerRankingEntry(Player Player, Club Club, int Rank)
{
    public bool IsGoalie => Player.Role == PlayerRole.Goalkeeper;
}

/// <summary>
/// Ranglisten der Spieler (Statistiken → Spieler) und die Einzelprämien „Topscorer“ und „bester Torhüter“
/// (Spur statistiken, #135). [ORIGINAL] em.exe Ov 0xC8, Funktion Datei 0x25E30 (<c>0xC8:0x20</c>, Argument 0 =
/// Bildschirm, 1 = nur Kennungen für das Saisonende 0x5576E):
/// <list type="number">
/// <item>Schlüssel je Spieler des Pools (Datei 0x25F64–0x260B6): Torhüter (Spielerfeld 0x18 = 0) mit mehr als 399
///   Minuten (Feld 0x1F) −(Gegentore · 6000 / Minuten), sonst −99.999 (0xFFFE7961); Feldspieler Tore (Feld 0x1B), bei
///   SCORER zuzüglich Vorlagen (Feld 0x1F), bei VORLEGER nur Vorlagen.</item>
/// <item>Austauschsortierung (Datei 0x25F4E–0x26145): für i = 1 … 198, j = i + 1 … 199 tauschen, wenn Schlüssel j &gt;
///   Schlüssel i oder bei Gleichstand Spiele j (Feld 0x1C) &lt; Spiele i – weniger Spiele stehen also vorn.</item>
/// <item>Aufnahme (Datei 0x26365–0x26414): nur Spieler eines Vereins der Liga des Managers (DGROUP 0x7C6 + 2 · Liga …
///   0x7C7 + 2 · Liga); Feldspieler mit Tore + Vorlagen ≥ 2, Torhüter mit ≥ 399 Minuten (die Sortierung verlangt
///   400, ein Torhüter mit genau 399 Minuten steht mit −99.999 hinten in der Liste).</item>
/// <item>Prämienkennung (Datei 0x26482–0x264D7): ist der erste aufgenommene Feldspieler bzw. Torhüter einer aus dem
///   Verein des Managers am Zug, wird das Byte gesetzt; Rückgabe Feldspieler | Torhüter &lt;&lt; 8 (Datei 0x26D84).</item>
/// </list>
/// [APPROX] Das Original sortiert die 199 Spieler seines Pools (Kader der Manager, Transfermarkt, wenige Spieler der
/// Computervereine); das Remake führt für alle Vereine vollständige Kader (<c>docs/reverse-engineering.md</c>) und
/// sortiert die Spieler der Liga in Vereins- und Kaderreihenfolge. Die Reihenfolge entscheidet nur bei völligem
/// Gleichstand (Schlüssel und Spiele).
/// </summary>
public static class PlayerStatistics
{
    /// <summary>[ORIGINAL] Torhüter kommen ab 400 Minuten in die Sortierung (Datei 0x25FC7: Minuten &gt; 0x18F).</summary>
    public const int GoalieSortMinutes = 400;

    /// <summary>[ORIGINAL] Torhüter werden ab 399 Minuten aufgenommen (Datei 0x263FD: Minuten ≥ 0x18F).</summary>
    public const int GoalieListMinutes = 399;

    /// <summary>[ORIGINAL] Feldspieler werden ab zwei Scorerpunkten aufgenommen (Datei 0x263E4).</summary>
    public const int SkaterListPoints = 2;

    /// <summary>[ORIGINAL] Schlüssel eines Torhüters unter 400 Minuten (Datei 0x26002: 0xFFFE7961).</summary>
    public const long NoGoalieKey = -99_999;

    /// <summary>Sortierschlüssel nach Datei 0x25F64–0x26035 (größer = besser).</summary>
    public static long Key(Player p, SkaterRanking mode)
    {
        if (p.Role == PlayerRole.Goalkeeper)
            return p.GoalieMinutes >= GoalieSortMinutes ? -(p.GoalsAgainst * 6000L / p.GoalieMinutes) : NoGoalieKey;
        long key = mode == SkaterRanking.Assists ? 0 : p.Goals;
        if (mode != SkaterRanking.Goals)
            key += p.Assists;
        return key;
    }

    /// <summary>Steht <paramref name="p"/> in der Rangliste (Datei 0x263D3–0x26405)?</summary>
    public static bool Listed(Player p) => p.Role == PlayerRole.Goalkeeper
        ? p.GoalieMinutes >= GoalieListMinutes
        : p.Goals + p.Assists >= SkaterListPoints;

    /// <summary>
    /// Alle aufgenommenen Spieler der Liga in der Reihenfolge des Originals, Feldspieler und Torhüter gemischt; der Platz
    /// zählt je Art (Datei 0x26482: Zähler je Art, auch für Zeilen, die die Ansicht „Manager“ ausblendet).
    /// </summary>
    public static IReadOnlyList<PlayerRankingEntry> Ranking(GameState game, League league, SkaterRanking mode)
    {
        var pool = new List<(Player Player, Club Club)>();
        foreach (var club in game.LeagueClubs(league).OrderBy(c => c.Index))
            foreach (var p in club.Players)
                pool.Add((p, club));
        var keys = pool.Select(e => Key(e.Player, mode)).ToArray();
        // [ORIGINAL] Austauschsortierung wie Datei 0x25F4E–0x26145 (nicht stabil; bei Gleichstand von Schlüssel und Spielen
        // entscheidet die Ausgangsreihenfolge).
        for (var i = 0; i < pool.Count; i++)
            for (var j = i + 1; j < pool.Count; j++)
            {
                var better = keys[j] > keys[i] || (keys[j] == keys[i] && pool[j].Player.Games < pool[i].Player.Games);
                if (!better)
                    continue;
                (pool[i], pool[j]) = (pool[j], pool[i]);
                (keys[i], keys[j]) = (keys[j], keys[i]);
            }
        var result = new List<PlayerRankingEntry>();
        int skaters = 0, goalies = 0;
        foreach (var (player, club) in pool)
        {
            if (!Listed(player))
                continue;
            var rank = player.Role == PlayerRole.Goalkeeper ? ++goalies : ++skaters;
            result.Add(new PlayerRankingEntry(player, club, rank));
        }
        return result;
    }

    /// <summary>Topscorer (Tore + Vorlagen) der Liga oder null – Platz 1 der Feldspieler in <see cref="Ranking"/>.</summary>
    public static PlayerRankingEntry? TopScorer(GameState game, League league) =>
        Ranking(game, league, SkaterRanking.Scorer).FirstOrDefault(e => !e.IsGoalie);

    /// <summary>Bester Torhüter der Liga oder null – Platz 1 der Torhüter in <see cref="Ranking"/>.</summary>
    public static PlayerRankingEntry? BestGoalie(GameState game, League league) =>
        Ranking(game, league, SkaterRanking.Scorer).FirstOrDefault(e => e.IsGoalie);

    /// <summary>
    /// [ORIGINAL] Tore je Spiel in Hundertsteln (Datei 0x26723–0x26772): Tore · 100 / Spiele, ohne Spiele Tore · 100.
    /// </summary>
    public static long GoalsPerGameHundredths(Player p) => p.Games == 0 ? p.Goals * 100L : p.Goals * 100L / p.Games;

    /// <summary>
    /// [ORIGINAL] Gegentore je 60 Minuten in Hundertsteln (Datei 0x267F2–0x26813): Gegentore · 6000 / Minuten.
    /// [APPROX] Ohne Minuten (im Original nicht erreichbar, weil nur Torhüter ab 399 Minuten angezeigt werden) 0.
    /// </summary>
    public static long GoalsAgainstPerGameHundredths(Player p) =>
        p.GoalieMinutes == 0 ? 0 : p.GoalsAgainst * 6000L / p.GoalieMinutes;
}
