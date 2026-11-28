namespace EishockeyManager.Core;

public static partial class MatchEngine
{
    /// <summary>
    /// [ORIGINAL] Höchstens so viele schon gerechnete Paarungen eines DEB-Pokaltermins lassen die Markierung 0x32AA3 eine noch
    /// offene Paarung (0:0) weiter als „Gleichstand“ melden: Der erste Durchlauf hebt sie auf 30:0 (Heim = Gast, 0x32B23), der
    /// zweite auf 60:0 (Heim − 30 = Gast, 0x32B3F–0x32B5A), danach greift keiner der beiden Vergleiche mehr.
    /// </summary>
    internal const int DebCupExtraRollPairings = 2;

    /// <summary>
    /// RR-01 (Review #965, Fixlauf re-reste): true, wenn die DEB-Pokalpaarung im schnellen Weg 0x3731F auch bei ungleichem
    /// Ergebnis den Zusatzwurf bekommt. [ORIGINAL] Nach dem Eintragen jeder Paarung ruft 0x376E1 die Markierung 0x32AA3 über alle
    /// Plätze der Zeile 0; noch nicht gerechnete Paarungen des Termins stehen auf 0:0 (0x34C94–0x34CAA setzt DGROUP 0x5C4B/0x5C4C
    /// nach jedem gewerteten Spiel zurück, 0x34F7D zu Rundenbeginn), ihr D014-Byte ist ≤ 0x63 (offen, 0x32B71/0x32B88). Jede
    /// Rückgabe ≠ 0 würfelt für die eben gerechnete Paarung (0x376E9 <c>or al,al; jnz 0x376F0</c>) bis Heim + x ≠ Gast + y
    /// (0x37724–0x37748) und setzt eine Marke (0x3775D–0x37774); der Sieger folgt der Summe (0x34C76). Die Markierung erkennt eine
    /// offene Paarung nur zweimal (<see cref="DebCupExtraRollPairings"/>): 0:0 → 30:0 (Heim = Gast) → 60:0 (Heim − 30 = Gast); bei
    /// 60:0 greift keiner der beiden Vergleiche – deshalb würfeln nur die ersten beiden gerechneten Paarungen eines Termins mit, die
    /// hinter sich noch eine offene Paarung haben (Beleg strittig zu RR-01 „jede Paarung“, siehe Übergabe PR #965). Die Paarungen
    /// laufen in Platzfolge (<see cref="DebCup.PlayDay"/>), auch eine Paarung der Minutenschleife zählt als gerechnet (Markierung
    /// 0x37211). Eigener Gleichstand wird davon unabhängig markiert (<see cref="OriginalMinutes.FastCupResultWithTie"/>).
    /// </summary>
    internal static bool DebCupExtraRoll(GameState game, Club home, Club away)
    {
        if (game.DebCup is not { CurrentRound: >= 0 } cup)
            return false;
        var round = cup.Rounds[cup.CurrentRound];
        var tie = round.FirstOrDefault(t => !t.IsDecided && t.Involves(home.Index) && t.Involves(away.Index));
        if (tie is null)
            return false;
        return OpenPairingStillTied(round, tie, DebCupExtraRollPairings, t => t.IsDecided, t => t.Games.Count);
    }

    /// <summary>
    /// Gemeinsame Prüfung der Markierung 0x32AA3 für DEB-Pokal (<see cref="DebCupExtraRoll"/>) und Play-offs
    /// (<see cref="FirstPlayoffPairingOfDate"/>): true, wenn an diesem Termin weniger als <paramref name="limit"/> Paarungen der
    /// Runde schon gerechnet sind (mehr Spiele als <paramref name="pairing"/>) und noch eine andere offene Paarung mit gleich vielen
    /// Spielen aussteht.
    /// </summary>
    private static bool OpenPairingStillTied<T>(IReadOnlyList<T> round, T pairing, int limit, Func<T, bool> isDecided,
        Func<T, int> games) where T : class
    {
        var number = games(pairing);
        var computed = round.Count(t => games(t) > number); // an diesem Termin schon gerechnet
        if (computed >= limit)
            return false;
        return round.Any(t => t != pairing && !isDecided(t) && games(t) == number);
    }
}
