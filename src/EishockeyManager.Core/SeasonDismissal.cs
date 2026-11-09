namespace EishockeyManager.Core;

/// <summary>Ausgeschiedener Manager (Saisonablauf).</summary>
/// <remarks>
/// [ORIGINAL] Keine Entlassung wegen Schulden (#688, R688S-03; vorher Remake-Regel „acht Wochen unter −1,5 Mio. DM“ aus
/// <c>docs/anforderungen/bildschirme.md</c>, nie belegt). Selbst nachgelesen (<c>disasm.py … --xref 0x97EA 0x97ED</c>): Alle
/// Vergleiche des Kontos (DGROUP 0x97EA + 0x1AF · Manager) in em.exe sind Stadionauftrag (Datei 0x3EADB), Kauf (0x42A8F),
/// Zwangsabstieg am 29. April (0x55F22–0x55F34, <see cref="GameState.BelowForcedRelegationLimit"/>) und die Kontogrenze
/// ±500.000.000 Drittel im Monatsabschluss (0x3F10E–0x3F165, <see cref="FinanceRules.BalanceLimit"/>); das Spielende hängt nur
/// am Endjahr (DGROUP 0x5A14, Datei 0x5641A–0x56444). Schulden führen also zum Zwangsabstieg (<c>SeasonFlow.Zwangsabstieg.cs</c>),
/// nie zur Entlassung.
/// </remarks>
public sealed partial class Manager
{
    /// <summary>
    /// Ausgeschieden – spielt nicht mehr mit. Kein Weg des Spiels setzt das Kennzeichen mehr (R688S-03); es bleibt für ältere
    /// Spielstände und für den Hot-Seat-Ablauf (<c>SeasonFlow.CheckDismissed</c>: Zug an den nächsten aktiven Manager, Spielende,
    /// wenn keiner mehr aktiv ist).
    /// </summary>
    public bool Dismissed { get; set; }
}
