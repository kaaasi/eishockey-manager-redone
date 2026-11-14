using System.Runtime.CompilerServices;

namespace EishockeyManager.Core;

/// <summary>
/// Zuschauer vor dem Spiel (Spur nutzer-befunde, #837). [ORIGINAL] Die Spielabrechnung 0x544C0 läuft je Paarung
/// <b>vor</b> den Minuten (Spieltagsablauf 0x36B5D, Aufrufe Datei 0x36DBD/0x36F9C): Für jeden Manager des Heimvereins
/// rechnet sie die Zuschauer (Datei 0x5464A) und legt sie im Satz <c>DGROUP 0x5D99 + 6 · (Manager + 1)</c> ab (+2 Zuschauer,
/// +1 ausverkauft, Datei 0x54A30–0x54B24); den Satz liest die Zuschauerzeile des Live-Bildschirms (Datei 0x53A11–0x53B37).
/// <list type="bullet">
/// <item>Das Remake rechnet die Zuschauer deshalb in der Vorbereitung jedes nicht gewerteten Spiels
///   (<see cref="MatchEngine"/>.BeginMatch, mit und ohne Anzeige gleich) aus <see cref="FinanceRandom"/> und merkt sie bis
///   zur Spielabrechnung des Wettbewerbs (<see cref="SettleMatchGate"/>), die Eintritt, <see cref="ManagerFinance.LastHomeGame"/>
///   und Statistik daraus bucht. Die Ziehungen bleiben in derselben Reihenfolge (Paarung für Paarung, Manager in
///   Indexreihenfolge); geändert hat sich nur der Zeitpunkt: Stärke (0x4D316) der Mannschaften vor statt nach dem Spiel.</item>
/// <item>[ORIGINAL] „Weniger als neun Spieler“ prüft 0x544C0 ebenfalls vor dem Spiel (Managerwert 0x974B aus 0x952C am
///   Tagesanfang, <see cref="MarkShortHandedClubs"/>): Ein vorbereitetes Spiel war nicht gewertet, also keine Strafe, auch
///   wenn sich im Spiel jemand verletzt (vorher [APPROX] „Stand nach dem Spiel“).</item>
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
    /// Zuschauer für jeden Manager des Heimvereins. Ohne Manager in der Paarung nichts.
    /// </summary>
    internal void PrepareMatchGate(Club home, Club away, MatchCompetition competition)
    {
        if (!Managers.Any(m => m.ClubIndex == home.Index || m.ClubIndex == away.Index))
            return;
        var tableMatchDay = PlayedMatchDays + 1;
        var importance = GateImportance(competition);
        var spectators = new List<(Manager, int)>();
        foreach (var manager in Managers)
            if (manager.ClubIndex == home.Index)
                spectators.Add((manager, Spectators(home, away, Finance(manager).Stadium, tableMatchDay, FinanceRandom, importance,
                    manager.Level)));
        Prepared.Add(new PreparedGate(home.Index, away.Index, tableMatchDay, importance, spectators));
        GateCounters.GetValue(this, _ => new GateCounter()).Prepared++;
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
