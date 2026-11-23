using System.Runtime.CompilerServices;

namespace EishockeyManager.Core;

/// <summary>
/// Zuschauer vor dem Spiel (Spur nutzer-befunde, #837). [ORIGINAL] Die Spielabrechnung 0x544C0 läuft je Paarung
/// <b>vor</b> den Minuten (Spieltagsablauf 0x36B5D, Aufrufe Datei 0x36DBD/0x36F9C): Für jeden Manager des Heimvereins
/// rechnet sie die Zuschauer (Datei 0x5464A) und legt sie im Satz <c>DGROUP 0x5D99 + 6 · (Manager + 1)</c> ab (+2 Zuschauer,
/// +1 ausverkauft, Datei 0x54A30–0x54B24); den Satz liest die Zuschauerzeile des Live-Bildschirms (Datei 0x53A11–0x53B37).
/// <list type="bullet">
/// <item>Das Remake rechnet die Zuschauer deshalb in der Vorbereitung jedes nicht gewerteten Spiels
///   (<see cref="MatchEngine"/>.BeginMatch, mit und ohne Anzeige gleich) aus <see cref="GameState.Random"/> und merkt sie bis
///   zur Spielabrechnung des Wettbewerbs (<see cref="SettleMatchGate"/>), die Eintritt, <see cref="ManagerFinance.LastHomeGame"/>
///   und Statistik daraus bucht. Die Ziehungen bleiben in derselben Reihenfolge (Paarung für Paarung, Manager in
///   Indexreihenfolge); geändert hat sich nur der Zeitpunkt: Stärke (0x4D316) der Mannschaften vor statt nach dem Spiel.</item>
/// <item>[ORIGINAL] „Weniger als neun Spieler“ prüft 0x544C0 ebenfalls vor dem Spiel (Managerwert 0x974B aus 0x952C am
///   Tagesanfang, <see cref="MarkShortHandedClubs"/>): Ein vorbereitetes Spiel war nicht gewertet, also keine Strafe, auch
///   wenn sich im Spiel jemand verletzt (vorher als Näherung „Stand nach dem Spiel“).</item>
/// <item>[APPROX] Die gemerkten Werte sind kein Spielstand (das Original rechnet Abrechnung und Spiel ohne Unterbrechung
///   für die Sicherung); sie verfallen am nächsten Tagesanfang. Fehlt ein Wert (Spielwertung, direkter Aufruf ohne Spiel),
///   rechnet die Abrechnung wie bisher.</item>
/// </list>
/// </summary>
public sealed partial class GameState
{
    /// <summary>
    /// Vorbereitete Abrechnung einer Paarung: Zuschauer je Manager des Heimvereins (Indexreihenfolge). Bleibt nach der
    /// Abrechnung (<see cref="Settled"/>) bis zum nächsten Tagesanfang lesbar wie der Satz 0x5D99 des Originals.
    /// </summary>
    private sealed record PreparedGate(int Home, int Away, int TableMatchDay, int Importance,
        IReadOnlyList<(Manager Manager, int Spectators)> HomeSpectators)
    {
        public bool Settled { get; set; }

        /// <summary>Spielwertung: der nicht spielfähige Manager, der die Strafe zahlt (Rückgabe 1/2 von 0x544C0), sonst null.</summary>
        public Manager? ShortHanded { get; init; }
    }

    // Statisch über eine schwache Tabelle, kein Instanzfeld: Instanzfelder gehören zum Spielstand (SaveGame.Fields).
    private static readonly ConditionalWeakTable<GameState, List<PreparedGate>> PreparedGates = new();

    private List<PreparedGate> Prepared => PreparedGates.GetValue(this, _ => []);

    /// <summary>Zähler für Proben (Review P3-2): vorbereitete und vorbereitet abgerechnete Paarungen seit dem Aufbau.</summary>
    private sealed class GateCounter
    {
        public int Prepared;
        public int Settled;
    }

    private static readonly ConditionalWeakTable<GameState, GateCounter> GateCounters = new();

    /// <summary>Vorbereitete und vorbereitet abgerechnete Paarungen dieses Objekts (kein Spielstand; Proben: beide gleich, &gt; 0).</summary>
    internal (int Prepared, int Settled) GateCounts
    {
        get
        {
            var c = GateCounters.GetValue(this, _ => new GateCounter());
            return (c.Prepared, c.Settled);
        }
    }

    /// <summary>
    /// Spielbedeutung der Zuschauerformel nach Wettbewerb (wie die Aufrufer von <see cref="SettleHomeMatch"/>): Liga 0,
    /// Play-Off/Aufstiegsrunde und DEB-Pokal 1, Europapokal nach Termin (<see cref="SpectatorRules.EuropeanCupImportance"/>),
    /// WM und Freundschaftsspiel 0.
    /// </summary>
    internal int GateImportance(MatchCompetition competition) => competition switch
    {
        MatchCompetition.Playoff or MatchCompetition.Relegation or MatchCompetition.DebCup => 1,
        MatchCompetition.EuropeanCup => SpectatorRules.EuropeanCupImportance(EuropeanCup.DateIndex(Day)),
        _ => 0,
    };

    /// <summary>
    /// Vorbereitung der Spielabrechnung vor den Minuten (Aufruf aus <c>MatchEngine.BeginMatch</c>, nur ohne Spielwertung):
    /// Zuschauer für jeden Manager des Heimvereins, danach je Manager der Paarung die eingespielte Reihe
    /// (<see cref="SquadSlotRules.RollPlayedLines"/>, A093). Ohne Manager in der Paarung nichts. Spielwertung:
    /// <see cref="PrepareForfeitGate"/>.
    /// </summary>
    internal void PrepareMatchGate(Club home, Club away, MatchCompetition competition)
    {
        if (!Managers.Any(m => m.ClubIndex == home.Index || m.ClubIndex == away.Index))
            return;
        var tableMatchDay = PlayedMatchDays + 1;
        var importance = GateImportance(competition);
        var spectators = new List<(Manager, int)>();
        foreach (var manager in Managers)
        {
            if (manager.ClubIndex == home.Index)
                spectators.Add((manager, Spectators(home, away, Finance(manager).Stadium, tableMatchDay, Random, importance,
                    manager.Level)));
            // A093 (kaderplatz-werte): [ORIGINAL] in derselben Managerrunde nach den Zuschauern (Datei 0x548A8).
            if (manager.ClubIndex == home.Index || manager.ClubIndex == away.Index)
                SquadSlotRules.RollPlayedLines(Clubs[manager.ClubIndex], Random);
        }
        Prepared.Add(new PreparedGate(home.Index, away.Index, tableMatchDay, importance, spectators));
        GateCounters.GetValue(this, _ => new GateCounter()).Prepared++;
    }

    /// <summary>
    /// Vorbereitung einer Spielwertung (Aufruf aus <c>MatchEngine.BeginMatch</c>, Paarung mit nicht spielfähigem Managerverein).
    /// [ORIGINAL] 0x544C0 läuft auch hier vor dem Eintrag 0:2/2:0 und prüft je Manager in Indexreihenfolge: Heimverein und
    /// Managerwert 100 (Datei 0x54589) → Ende mit 1, sonst Zuschauer (Datei 0x5464A); Gastverein und Managerwert 100 (Datei
    /// 0x547AA) → Ende mit 2; danach würfelt der Manager der Paarung die eingespielte Reihe (Datei 0x548A8–0x548E7). Die Manager mit
    /// kleinerem Index als der nicht spielfähige ziehen also Zuschauer und Reihen wie vor einem Spiel (A913, Spur v-rest); der nicht
    /// spielfähige und alle späteren ziehen nichts. Die Abrechnung bucht die Zuschauer und danach die Strafe
    /// (<see cref="SettlePreparedGate"/>, wie bisher <see cref="SettleMatchGate"/>). Managerwert 100 = Verein mit
    /// <see cref="Club.ForfeitsMatch"/> eines nicht entlassenen Managers (<see cref="MarkShortHandedClubs"/>).
    /// Diese Manager zählen außerdem Spiele und begrenzen die Belastung wie vor einem Spiel (<see cref="CountForfeitGames"/>, A961).
    /// </summary>
    internal void PrepareForfeitGate(Club home, Club away, MatchCompetition competition)
    {
        if (!Managers.Any(m => m.ClubIndex == home.Index || m.ClubIndex == away.Index))
            return;
        var tableMatchDay = PlayedMatchDays + 1;
        var importance = GateImportance(competition);
        var spectators = new List<(Manager, int)>();
        Manager? shortHanded = null;
        foreach (var manager in Managers)
        {
            var forfeits = !manager.Dismissed && Clubs[manager.ClubIndex].ForfeitsMatch;
            if (manager.ClubIndex == home.Index)
            {
                if (forfeits)
                {
                    shortHanded = manager; // Datei 0x54589–0x5459C
                    break;
                }
                spectators.Add((manager, Spectators(home, away, Finance(manager).Stadium, tableMatchDay, Random, importance,
                    manager.Level)));
            }
            if (manager.ClubIndex == away.Index && forfeits)
            {
                shortHanded = manager; // Datei 0x547AA–0x547BD
                break;
            }
            if (manager.ClubIndex == home.Index || manager.ClubIndex == away.Index)
            {
                var club = Clubs[manager.ClubIndex];
                SquadSlotRules.RollPlayedLines(club, Random); // Datei 0x548A8
                CountForfeitGames(club, competition); // A961
            }
        }
        Prepared.Add(new PreparedGate(home.Index, away.Index, tableMatchDay, importance, spectators)
        {
            ShortHanded = shortHanded,
        });
        GateCounters.GetValue(this, _ => new GateCounter()).Prepared++;
    }

    /// <summary>
    /// Rest des Managerblocks von 0x544C0 vor einer Spielwertung für einen Manager mit kleinerem Index als der nicht spielfähige
    /// (A961, R23-E1 = A, Nutzer 23.11.2026; im gespielten Spiel zählt <c>MatchEngine.Credit</c> dasselbe nach dem Spiel, das bei
    /// der Spielwertung endet). Zieht keinen Zufall.
    /// <list type="bullet">
    /// <item>[ORIGINAL] Spiele: Kaderplatzschleife Datei 0x54918–0x5499E zählt jeden Aufgestellten (+0x0A ≠ 0 und +0x15 ≠ 0) im
    ///   Feld des Wettbewerbs (+0x06/+0x1C + Wettbewerb; Argument 1 = Pokal, 8 = Europapokal, sonst Liga), Pool +0x1C nur bei
    ///   Wettbewerb 0 (Datei 0x5492E–0x54948); wie <see cref="Player.CountGame"/> mit <see cref="CompetitionSlots.Of"/>.</item>
    /// <item>[ORIGINAL] Belastung jedes Kaderplatzes auf 50 … 150 (Datei 0x549A2–0x549BB, <see cref="TrainingRules.ClampStrain"/>).</item>
    /// <item>[ORIGINAL] Kennzeichen „im Spiel“ DGROUP 0x2D6 + Manager = 5 (Datei 0x54821): ohne Zustand im Remake, der Wert ist
    ///   immer 5 (Startwert, einziger Schreiber; <see cref="MatchIntervention.InMatchFlag"/>).</item>
    /// <item>[ORIGINAL] Strafenzähler +0x0D − 1 bei Status +0x09 Bit 0 für die Markt-Plätze des Managers (Satz 4, DGROUP 0x7623,
    ///   Pool +0x1A = Manager, Datei 0x5483B–0x54897) und seine Kaderplätze (Datei 0x548EB–0x5490C), beide nur bei Wettbewerb
    ///   0 (<c>[bp+0xA] = 0</c>): wie im gespielten Spiel ohne Zustand im Remake – Bit 0 und +0x0D gelten nur, solange der Spieler
    ///   auf der Strafbank sitzt, und nach einem Spiel sitzt dort niemand (<c>MatchEngine.PenaltyBoxQuery.cs</c>, dortiges
    ///   [APPROX]).</item>
    /// </list>
    /// </summary>
    private static void CountForfeitGames(Club club, MatchCompetition competition)
    {
        var slot = CompetitionSlots.Of(competition);
        foreach (var p in club.Lineup.Players)
            p.CountGame(slot); // Datei 0x54918–0x5499E
        TrainingRules.ClampStrain(club.Players); // Datei 0x549A2–0x549BB
    }

    /// <summary>
    /// Zuschauer des laufenden Heimspiels für die Zuschauerzeile des Live-Bildschirms: [ORIGINAL] Datei 0x53A11–0x53B37 liest
    /// den Satz des Managers mit dem Heimverein, sonst des Gastvereins; der Gastmanager hat dort 0 (Datei 0x54B9F: der
    /// Zweig für ihn setzt nur bei <c>DGROUP 0xD0C0</c> = 2, das kein Schreiber setzt). <c>SoldOut</c> = Zuschauer gleich Sitz-
    /// und Stehplätzen (Datei 0x53AC2–0x53AF5). Null ohne vorbereitetes Heimspiel eines Managers oder mit 0 Zuschauern.
    /// </summary>
    public (int Spectators, bool SoldOut)? HomeGate(Club home, Club away)
    {
        var gate = Prepared.LastOrDefault(g => g.Home == home.Index && g.Away == away.Index);
        if (gate is null || gate.HomeSpectators.Count == 0 || gate.HomeSpectators[0].Spectators == 0)
            return null;
        var (manager, spectators) = gate.HomeSpectators[0];
        return (spectators, spectators == Finance(manager).Stadium.Capacity);
    }

    /// <summary>Vorbereitete, noch nicht abgerechnete Paarungen (Proben: nach jedem Tag 0).</summary>
    internal int UnsettledGates => Prepared.Count(g => !g.Settled);

    /// <summary>Am Tagesanfang: Vorbereitungen des Vortags verfallen (abgerechnet oder nicht).</summary>
    internal void ClearPreparedGates() => Prepared.Clear();

    /// <summary>
    /// Abrechnung mit vorbereiteten Zuschauern (aus <see cref="SettleMatchGate"/>): Eintritt, letztes Heimspiel und
    /// Statistik für jeden Manager des Heimvereins, keine Strafe. False, wenn die Paarung nicht vorbereitet ist.
    /// </summary>
    /// <exception cref="InvalidOperationException">Tabellenspieltag oder Spielbedeutung weichen von der Vorbereitung ab.</exception>
    private bool SettlePreparedGate(int home, int away, int tableMatchDay, int importance, bool leagueMatch)
    {
        var gate = Prepared.FirstOrDefault(g => !g.Settled && g.Home == home && g.Away == away);
        if (gate is null)
            return false;
        gate.Settled = true;
        GateCounters.GetValue(this, _ => new GateCounter()).Settled++;
        if (gate.TableMatchDay != tableMatchDay || gate.Importance != importance)
            throw new InvalidOperationException(
                $"Spielabrechnung {home}–{away}: vorbereitet Spieltag {gate.TableMatchDay}/Bedeutung {gate.Importance}, " +
                $"abgerechnet {tableMatchDay}/{importance}.");
        foreach (var (manager, spectators) in gate.HomeSpectators)
            BookGate(manager, spectators, away, leagueMatch);
        if (gate.ShortHanded is { } shortHanded)
            PenalizeShortHanded(shortHanded);
        return true;
    }

    /// <summary>Eintritt buchen, letztes Heimspiel und (nur Liga) Zuschauerstatistik. [ORIGINAL] 0x5464A, 0x54664.</summary>
    private void BookGate(Manager manager, int spectators, int away, bool leagueMatch)
    {
        var finance = Finance(manager);
        var tickets = (long)spectators * finance.Stadium.TicketPrice;
        Book(manager, FinanceCategory.Eintritt, tickets);
        finance.LastHomeGame = (spectators, tickets);
        if (leagueMatch)
            finance.HomeStatistics.Add(spectators, away);
    }
}
