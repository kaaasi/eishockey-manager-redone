namespace EishockeyManager.Core;

/// <summary>
/// Spielberechnung. Zwei Einstiege:
/// <list type="bullet">
/// <item><see cref="PlayWithEvents"/> – Berechnung des Originals Minute für Minute mit Ereignissen und Spielerzählern
///   (Spur spielberechnung, <c>MatchEngine.Live.cs</c>); Liga, Play-offs, Aufstiegsrunde, DEB-Pokal, Europapokal und WM.</item>
/// <item><see cref="Play"/> – nur Ergebnis, bisherige Näherung ohne Aufrufer im Spielablauf (nur
///   <see cref="KnockoutMatch.Play(int, Club, Club, Random)"/> und Tests). Die Länder-Freundschaftsspiele rechnen seit der
///   Integration Runde 7 mit <see cref="PlayWithEvents"/>, ab Runde 17 mit Wettbewerbscode 0xD
///   (<see cref="MatchCompetition.Friendly"/>, A008/A110).</item>
/// </list>
/// [APPROX] <see cref="Play"/> (im Spiel nicht mehr benutzt): Torerwartung aus der Stärkedifferenz, leichter Heimvorteil, Tore poissonverteilt;
/// kalibriert auf rund 6,5 Tore je Spiel.
/// </summary>
public static partial class MatchEngine
{
    public static (int Home, int Away) Play(Club home, Club away, Random random)
    {
        // Spur heimspiel-statistik (#165): [ORIGINAL] 0x36DC5 – nicht spielfähiger Managerverein, Wertung ohne Spielberechnung
        // (kein Zufallszug). Heimverein zuerst, siehe GameState.MarkShortHandedClubs.
        if (home.ForfeitsMatch)
            return (0, 2);
        if (away.ForfeitsMatch)
            return (2, 0);
        var diff = (home.MatchStrength - away.MatchStrength) / 30.0; // Stärke aus der Aufstellung (Paket „mannschaft“)
        var lambdaHome = 3.45 * Math.Exp(0.45 * diff);
        var lambdaAway = 3.05 * Math.Exp(-0.45 * diff);
        return (Poisson(lambdaHome, random), Poisson(lambdaAway, random));
    }

    private static int Poisson(double lambda, Random random)
    {
        var limit = Math.Exp(-lambda);
        var k = 0;
        var p = random.NextDouble();
        while (p > limit)
        {
            k++;
            p *= random.NextDouble();
        }
        return k;
    }
}
