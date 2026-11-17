using System.Runtime.CompilerServices;

namespace EishockeyManager.Core;

public static partial class MatchEngine
{
    private static readonly ConditionalWeakTable<object, int[,]> MinuteCounterStore = new();

    /// <summary>
    /// A549: Minutenzähler der Minuten-Vorgabe (<see cref="LiveSquad.MinuteCounters"/>) eines Managers bzw. des Bundestrainers.
    /// [ORIGINAL] DGROUP 0x4926 + 8 · Manager + 4 · Teil + Reihe; nur 0x8EF2 schreibt ihn (Datei 0x911C + 1, Datei 0x9166 = 0 beim
    /// Wechsel), kein weiterer Leser, Schreiber oder Blockzugriff auf DGROUP 0x4926–0x4955 – er läuft über das Spielende in das
    /// nächste Spiel des Managers weiter. [APPROX] Er gehört im Original nicht zum Spielstand (HOK); das Remake hält ihn ebenso
    /// außerhalb des Spielstands am Manager (nach dem Laden 0 wie beim Programmstart; das Original behielte beim Laden im
    /// laufenden Programm den alten Wert).
    /// </summary>
    internal static int[,] MinuteCountersOf(object manager) => MinuteCounterStore.GetValue(manager, _ => new int[2, 4]);
}
