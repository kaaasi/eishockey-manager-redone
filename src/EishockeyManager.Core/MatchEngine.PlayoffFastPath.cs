namespace EishockeyManager.Core;

public static partial class MatchEngine
{
    /// <summary>
    /// #925 (A873): true, wenn die Play-off-Paarung die erste gerechnete ihres Termins ist und noch eine andere offene Paarung der
    /// Runde aussteht. [ORIGINAL] Die Zeile 2 (DGROUP 0x5C4B + 64) steht zu Beginn eines Termins auf 0:0 (0x317B2); 0x32AA3
    /// findet nach der ersten gerechneten Paarung die noch nicht gerechneten als Gleichstand und markiert sie (+ 30, danach kein
    /// Gleichstand mehr) – ebenso nach einer Paarung der Minutenschleife (Datei 0x37211). Offen = Serie nicht entschieden
    /// (Ergebnisbyte DGROUP 0xCCB2 + 14 · Spiel + Platz ≤ 0x63, Datei 0x32CD9–0x32D0E). Die Paarungen laufen in Platzfolge
    /// (<see cref="Playoffs.PlayDate"/>), alle offenen Serien einer Runde spielen an jedem Termin.
    /// </summary>
    internal static bool FirstPlayoffPairingOfDate(GameState game, Club home, Club away)
    {
        if (game.Playoffs is not { } playoffs)
            return false;
        foreach (var round in playoffs.Rounds)
        {
            var series = round.FirstOrDefault(s => !s.IsDecided && s.Involves(home.Index) && s.Involves(away.Index));
            if (series is null)
                continue;
            // Grenze 1: ist eine Paarung dieses Termins schon gerechnet, meldet 0x32AA3 keinen Gleichstand mehr.
            return OpenPairingStillTied(round, series, 1, s => s.IsDecided, s => s.Games.Count);
        }
        return false;
    }
}
