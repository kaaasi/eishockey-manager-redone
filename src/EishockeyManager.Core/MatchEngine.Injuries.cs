namespace EishockeyManager.Core;

/// <summary>
/// Verletzungen der Managervereine (Spur spielberechnung-komplett, Teil 3; <c>docs/spielberechnung.md</c> §17).
/// [ORIGINAL] Verletzung eines Kaderplatzes Datei 0x05B1A (Basis 0x238 + 0x199A), Tabellen in DGROUP:
/// Wochen je Art 0x181 (Paare min/max), Häufigkeit 0x1B9, schwere Arten 0x483B.
/// </summary>
public static class PlayerInjuries
{
    /// <summary>[ORIGINAL] Kaderplatz +0x0D = 0xFF: die Verletzung heilt nicht (0x7C12 zählt 0xFF nicht herunter).</summary>
    public const int Permanent = 0xFF;

    /// <summary>[ORIGINAL] DGROUP 0x181: Dauer in Wochen je Art (random(min, max)), 28 Arten (Namen ab DGROUP 0xA00).</summary>
    private static readonly (int Min, int Max)[] Weeks =
    [
        (14, 28), (9, 19), (2, 4), (8, 15), (10, 16), (6, 12), (10, 16), (16, 30), (1, 3), (7, 30), (8, 20), (10, 16),
        (8, 18), (11, 18), (20, 40), (12, 20), (10, 16), (2, 8), (3, 6), (14, 28), (4, 7), (7, 17), (2, 13), (6, 13),
        (7, 20), (6, 12), (4, 8), (2, 9),
    ];

    /// <summary>[ORIGINAL] DGROUP 0x1B9: eine gezogene Art gilt, wenn random(0, 15) ≤ Wert.</summary>
    private static readonly int[] Weight =
        [1, 4, 6, 4, 5, 1, 3, 2, 6, 1, 2, 4, 5, 3, 1, 4, 5, 6, 7, 1, 6, 5, 1, 2, 1, 1, 1, 2];

    /// <summary>[ORIGINAL] DGROUP 0x483B: Arten, die mit kleiner Wahrscheinlichkeit nicht mehr heilen.</summary>
    private static readonly int[] Serious = [0, 1, 4, 7, 9, 10, 11, 14, 19];

    /// <summary>[ORIGINAL] 0x5B9C: im Spiel nur die Arten 0 … 21 (random(0, 27 − 6 · Schalter)).</summary>
    public const int MatchKinds = 22;

    /// <summary>
    /// Verletzt einen Spieler. [ORIGINAL] Datei 0x05B1A(Kaderplatz, Text, Schalter Spiel):
    /// <list type="number">
    /// <item>Form aus dem Pool (Spielerfeld 0x17) &gt; 20 → − random(10, 15).</item>
    /// <item>Art t = random(0, 27 − 6 · Spiel), bis random(0, 15) ≤ Häufigkeit[t].</item>
    /// <item>Dauer = random(min[t], max[t]) Wochen (+0x0D); schwere Art: mit random(0, 2 · (5 − Level) + 10 − min[t] / 4)
    ///   = 0 für immer (0xFF).</item>
    /// <item>Status verletzt (+0x09 | 2), Art (+0x17), nicht eingesetzt (+0x0A = 0).</item>
    /// </list>
    /// [APPROX] Das Remake führt die Art nicht (keine Anzeige in dieser Spur, kein neues Spielstandfeld) und zählt die
    /// Wochen in <see cref="Player.InjuredGames"/>; die Form des Kaderplatzes steht für die Form des Pools.
    /// </summary>
    /// <param name="difficulty">„5 − Level“ des Managers (DGROUP 0xED8).</param>
    /// <returns>Gezogene Art 0 … 27.</returns>
    public static int Inflict(Player player, bool inMatch, int difficulty, Random random)
    {
        if (player.Form > 20)
            player.Form -= OriginalMatch.Rnd(random, 10, 15);
        int kind;
        do
            kind = OriginalMatch.Rnd(random, 0, 27 - 6 * (inMatch ? 1 : 0));
        while (OriginalMatch.Rnd(random, 0, 15) > Weight[kind]);
        var (min, max) = Weeks[kind];
        var weeks = OriginalMatch.Rnd(random, min, max);
        if (Serious.Contains(kind) && OriginalMatch.Rnd(random, 0, 2 * difficulty + 10 - min / 4) == 0)
            weeks = Permanent;
        player.InjuredGames = weeks;
        return kind;
    }

    /// <summary>
    /// Heilung. [ORIGINAL] Datei 0x7C12 aus dem Tageslauf 0x07169 für die Kaderplätze der Manager und ihre Marktplätze:
    /// verletzt (+0x09 &amp; 2), nicht 0xFF und nicht 0 → + 0x0D − 1, wenn (Tageszähler 0x3B7 − [0x6AF]) durch 7 teilbar
    /// ist; danach Status frei, wenn +0x0D = 0. [APPROX] Versatz DGROUP 0x6AF nicht gelesen (0); alle Spieler des
    /// Spielstands (im Remake verletzen sich nur Spieler der Manager).
    /// </summary>
    public static bool IsHealingDay(int day) => day % 7 == 0;

    /// <returns>Ist der Spieler jetzt geheilt?</returns>
    public static bool Heal(Player player)
    {
        if (player.InjuredGames is not (> 0 and < Permanent))
            return false;
        player.InjuredGames--;
        return player.InjuredGames == 0;
    }

    /// <summary>
    /// Aufstellung nach Verletzung oder Heilung. [ORIGINAL] Nach jeder Verletzung (0x54197, ohne Live-Anzeige) und jeder
    /// Heilung (0x7C77) sowie täglich für Verletzte und Gesperrte (0x7621) ruft das Original 0x426BC (Stub 0x14B7:0x4D):
    /// ist die Einstellung DGROUP 0x375 + 2 · Manager gesetzt, stellt es die Kaderplätze des Managers neu auf (+0x0A, +0x15
    /// über 0x42838; Teil 8: <see cref="Club.ApplyAutoLineup"/>, <see cref="AutoLineupRule"/>). [APPROX] Nach dem Spiel
    /// statt in der Minute; ohne die Einstellung nimmt das Remake Verletzte heraus und füllt Lücken mit eingesetzten
    /// Reservisten (das Original lässt sie). Eingesetzt (+0x0A) und die Grenze von 22: <see cref="Club.RefillLineup"/>.
    /// </summary>
    public static void RefillLineup(Club club) => club.RefillLineup();
}

public static partial class OriginalMinutes
{
    /// <summary>[ORIGINAL] 0x38C3F: unter 4 eingesetzten Spielern der Aufstellung (0x120:0x66 Modus 1) keine Verletzung.</summary>
    public const int MinimumLineupForInjuries = 4;

    /// <summary>[ORIGINAL] 0x54215: Torschütze erst ab 4 eingesetzten Spielern der Aufstellung (0x120:0x66 Modus 1 &gt; 3).</summary>
    public const int MinimumLineupForScorers = 4;

    /// <summary>[ORIGINAL] 0x388A1: höchstens 250 Züge in 0x386D1.</summary>
    public const int MaxScorerDraws = 250;

    /// <summary>[APPROX] Schutz gegen eine Endlosschleife der Vorlagenwahl (das Original wiederholt ohne Grenze).</summary>
    public const int MaxAssistDraws = 1000;

    /// <summary>Eingesetzte Spieler mit Platz in der Aufstellung (0x120:0x66 Modus 1: +0x0A ≠ 0 und +0x15 ≠ 0).</summary>
    public static int LineupPlayers(LiveSquad squad) => squad.Players.Count(p => p.Dressed && p.Position != 0);

    /// <summary>
    /// Verletzungen im Spiel. [ORIGINAL] Datei 0x38B8F, je Minute einer Managerpaarung vor den Torchancen (0x37F06), wenn
    /// einer der beiden Vereine einen Manager hat:
    /// <list type="number">
    /// <item>Feldspieler beider Vereine neu zählen (0x38A36, Datei 0x38BA9–0x38BBA; <see cref="MatchEngine.RecountSkaters"/>,
    ///   nur Managervereine).</item>
    /// <item>Manager m = 0 … Zahl der Manager − 1 (Satz aktiv, DGROUP 0xA729 + m) und der Bundestrainer (5, nur mit einem
    ///   Verein ≤ 0x3F – also nie mit dem Nationalteam), dessen Verein spielt, in dieser Reihenfolge.</item>
    /// <item>Managerwert des Managers am Zug (DGROUP 0x974B + 0x1AF · [0xA70E]) = 100 oder weniger als 4 eingesetzte Spieler
    ///   der Aufstellung → Ende der Funktion (auch für die übrigen Manager). [APPROX] Der Manager am Zug ist im Remake der
    ///   jeweilige Manager.</item>
    /// <item>random(0, 20 · clamp(60 − M, 0, 40) + 200 · (5 − Level) + 200) = 0 (M = Managerwert des Managers, Schalter
    ///   DGROUP 0x4E80 [APPROX] aus) → Spieler 0x378C5 (<see cref="PenalizedPlayer"/>; keiner → nächster Manager).</item>
    /// <item>Ereignis 4 (0x130:0x7F = 0x53C57 → 0x5411E): Verletzung 0x05B1A mit Schalter Spiel
    ///   (<see cref="PlayerInjuries.Inflict"/>), der Spieler geht vom Eis (+0x0A = 0). Versicherte Spieler: Zahlung 0x07089
    ///   (Kaderplatz +0x30 · 4 an den Manager) – Anschluss in Spur transfer-vertraege-komplett (Teil 2), hier nicht.</item>
    /// </list>
    /// </summary>
    public static void Injuries(LiveTeam home, LiveTeam away, int minute, MatchReport report, Random random)
    {
        MatchEngine.RecountSkaters(home);
        MatchEngine.RecountSkaters(away);
        var teams = new List<(LiveTeam Team, int Side)>();
        if (home.InjuryManager is not null)
            teams.Add((home, 0));
        if (away.InjuryManager is not null)
            teams.Add((away, 1));
        foreach (var (team, side) in teams.OrderBy(t => t.Team.InjuryManager))
        {
            var squad = team.Squad!;
            if (team.ManagerValue == 100 || LineupPlayers(squad) < MinimumLineupForInjuries)
                return;
            var v = Math.Clamp(60 - team.ManagerValue, 0, 40);
            if (OriginalMatch.Rnd(random, 0, 20 * v + 200 * squad.Difficulty + 200) != 0)
                continue;
            var player = PenalizedPlayer(team, random);
            if (player is null)
                continue;
            PlayerInjuries.Inflict(player.Player, inMatch: true, squad.Difficulty, random);
            player.Dressed = false;
            report.Events.Add(new MatchEvent(minute, side, MatchEventKind.Injury, Player: player.Player));
        }
    }

    /// <summary>
    /// Torschütze und Vorlage eines Abschlusses einer gemanagten Mannschaft. [ORIGINAL] Datei 0xAB47–0xAB90: in der
    /// Managerpaarung ruft jeder Abschluss (auch ohne Tor; nicht der Abbruch in der Verlängerung 0xA64E) die Szene
    /// 0x14E1:0x7F (0x53C57) mit Art 1 = Tor bzw. 0 = kein Tor; ohne Torszenen (DGROUP 0x226 = 0, [APPROX] wie im Remake
    /// üblich) direkt 0x541D9(Verein, Art, 0): Manager des Vereins (erster Satz mit diesem Verein, auch der Bundestrainer
    /// mit dem Nationalteam), mehr als 3 eingesetzte Spieler der Aufstellung → Schütze 0x386D1(m, 0, Art), Vorlage
    /// 0x386D1(m, 1, 1) bis ungleich dem Schützen. Nur bei einem Tor zählen Vorlage (Spielerfeld 0x1F) und Tor (Spielerfeld
    /// 0x1B in Wettbewerb 0, Kaderplatz +0x22/+0x03 je Wettbewerb DGROUP 0xD0BF).
    /// </summary>
    /// <returns>Schütze und Vorlage oder null (kein Kader, zu wenige Spieler).</returns>
    public static (LivePlayer Scorer, LivePlayer Assist)? ScoringPlayers(LiveTeam team, bool goal, Random random)
    {
        if (team.PenaltyPlayers is not { } squad || LineupPlayers(squad) < MinimumLineupForScorers)
            return null;
        var scorer = ScorerIndex(team, squad, assist: false, formula: goal, random);
        int assist;
        var tries = 0;
        do
            assist = ScorerIndex(team, squad, assist: true, formula: true, random);
        while (assist == scorer && ++tries < MaxAssistDraws);
        return (squad.Players[scorer], squad.Players[assist]);
    }

    /// <summary>
    /// Spielerwahl 0x386D1(Manager, Vorlage v, Schalter s). [ORIGINAL] Datei 0x386D1–0x388F5:
    /// <list type="number">
    /// <item>n = belegte Kaderplätze − 1 (0x120:0x66 Modus 0); höchstens 250 Züge Platz random(0, n).</item>
    /// <item>Nur Feldspieler (Rollenwert ≠ 0), eingesetzt (+0x0A), in der Managerpaarung (0x130:0x4D, im Remake immer) in
    ///   der Reihe ihres Teils auf dem Eis ((+0x15 &amp; 0x7F) − 1 = Vereinsfeld 0x2E + 2 · Sturm).</item>
    /// <item>Gewicht w = Technik · 3 / (5 − 3v) + Kondition / 5 + Form / 5 (mit v oder s, sonst random(120, 170))
    ///   + Gruppe (0x238:0, Rollenwert / 25 ≤ 2) · (72 − 17v) + Tore (+0x03) · 46 + 700 im Sturm (+0x15 Bit 7);
    ///   gewählt, wenn random(0, 3000) &lt; w.</item>
    /// <item>Ohne Treffer der erste Feldspieler unter den Plätzen 0 … n − 1 (sonst n).</item>
    /// </list>
    /// [APPROX] Tore (+0x03, Wettbewerb 0) = Tore der Saison (<see cref="Player.Goals"/>) samt den Toren dieses Spiels.
    /// </summary>
    public static int ScorerIndex(LiveTeam team, LiveSquad squad, bool assist, bool formula, Random random)
    {
        var n = squad.Players.Count - 1;
        var v = assist ? 1 : 0;
        for (var tries = 0; tries < MaxScorerDraws; tries++)
        {
            var index = OriginalMatch.Rnd(random, 0, n);
            var p = squad.Players[index];
            if (p.Goalie || !p.Dressed || p.Line != team.Line(p.Part))
                continue;
            var player = p.Player;
            var weight = assist || formula
                ? player.Technique * 3 / (5 - 3 * v) + player.Condition / 5 + player.Form / 5
                : OriginalMatch.Rnd(random, 120, 170);
            weight += TrainingRules.Group(player.Role) * (72 - 17 * v);
            weight += (player.Goals + p.MatchGoals) * 46;
            if (p.Part == 1)
                weight += 700;
            if (OriginalMatch.Rnd(random, 0, 3000) < weight)
                return index;
        }
        for (var i = 0; i < n; i++)
            if (!squad.Players[i].Goalie)
                return i;
        return n;
    }
}

public sealed partial class GameState
{
    /// <summary>Heilung der Verletzungen (siehe <see cref="PlayerInjuries.Heal"/>), einmal je Tag des Tageslaufs.</summary>
    public void HealInjuries(int day)
    {
        if (!PlayerInjuries.IsHealingDay(day))
            return;
        foreach (var club in Clubs)
        {
            var healed = false;
            foreach (var p in club.Players)
                healed |= PlayerInjuries.Heal(p);
            if (healed && Managers.Any(m => m.ClubIndex == club.Index))
                PlayerInjuries.RefillLineup(club);
        }
        foreach (var p in TransferMarket.FreeAgents)
            PlayerInjuries.Heal(p);
    }
}
