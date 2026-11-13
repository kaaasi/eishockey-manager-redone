namespace EishockeyManager.Core;

/// <summary>
/// Zyklus der eigenen Zufallsfolge des Originals (Spur abgleich-dosbox, Teil 2, #773). [ORIGINAL] lrandom(a, b), Datei 0xBFD5
/// (`0x99F:0x7E5`): x = (16 · x + 167) mod 4.000.000 (DGROUP 0xE017), Ergebnis a + x · (b − a + 1) / 4.000.000. Weil 16 gerade
/// ist, stehen die unteren 8 Bit nach zwei Schritten fest; modulo 5^6 ist der Schritt umkehrbar – die Folge läuft also auf
/// einem Zyklus von 15.625 Zuständen, und aufeinanderfolgende Züge hängen eng zusammen (x' ≈ 16 · x).
/// </summary>
/// <remarks>
/// Das Remake zieht sonst unabhängig aus <see cref="Random"/> (<see cref="OriginalMatch"/>, [APPROX]). Wo das Original mehrere
/// lrandom-Züge unmittelbar hintereinander macht und der Zusammenhang die Wahrscheinlichkeit merklich verschiebt, liefert diese
/// Klasse die Häufigkeiten über den ganzen Zyklus. [APPROX] Die Lage im Zyklus beim ersten Zug gilt als gleichverteilt
/// (dazwischen liegen im Original viele andere Züge).
/// </remarks>
public static class LRandomCycle
{
    /// <summary>[ORIGINAL] Modul der Folge (Datei 0xBFD8/0xBFDB: 0x3D0900).</summary>
    public const int Modulus = 4_000_000;

    /// <summary>[ORIGINAL] Ein Schritt der Folge (Datei 0xBFE0–0xBFFF).</summary>
    public static int Step(int x) => (int)((16L * x + 167) % Modulus);

    /// <summary>[ORIGINAL] Ergebnis eines Zugs aus dem neuen Zustand (Datei 0xC002–0xC036), a … b einschließlich.</summary>
    public static int Value(int x, int min, int max) => min + (int)((long)x * (max - min + 1) / Modulus);

    private static readonly Lazy<int[]> CycleStates = new(() =>
    {
        var states = new List<int>();
        var start = Step(Step(0));
        var x = start;
        do
        {
            states.Add(x);
            x = Step(x);
        }
        while (x != start);
        return [.. states];
    });

    /// <summary>Alle Zustände des Zyklus (15.625).</summary>
    public static IReadOnlyList<int> Cycle => CycleStates.Value;

    private static readonly Lazy<(int Total, int Five)[,]> PenaltyOdds = new(() =>
    {
        // [ORIGINAL] Datei 0x358B7–0x3592D: d1 = lrandom(0, 12); nur bei d1 ≠ 0 d2 = lrandom(0, 110 − Einsatz); Strafe bei
        // d1 = 0 oder d2 = 0; dann (Platz 0 frei) d3 = lrandom(0, 15), 5 Minuten bei d3 = 0 – ohne Züge dazwischen.
        var odds = new (int Total, int Five)[101, 2];
        var cycle = Cycle;
        for (var effort = 0; effort <= 100; effort++)
        {
            foreach (var x1 in cycle)
            {
                var x2 = Step(x1);
                if (Value(x1, 0, 12) == 0)
                {
                    odds[effort, 0].Total++;
                    if (Value(x2, 0, 15) == 0)
                        odds[effort, 0].Five++;
                }
                else if (Value(x2, 0, 110 - effort) == 0)
                {
                    odds[effort, 1].Total++;
                    if (Value(Step(x2), 0, 15) == 0)
                        odds[effort, 1].Five++;
                }
            }
        }
        return odds;
    });

    /// <summary>
    /// Häufigkeit der 5-Minuten-Strafe über den Zyklus: von <c>Total</c> Lagen, in denen der Strafwurf auf diesem Weg trifft
    /// (<paramref name="firstDraw"/>: schon lrandom(0, 12) = 0, sonst erst lrandom(0, 110 − Einsatz) = 0), folgt in <c>Five</c>
    /// lrandom(0, 15) = 0. Über den Zyklus: erster Weg 122 von 1.202 Zuständen (10,1 %), zweiter Weg bei Einsatz 40 53 von 192
    /// (27,6 %), bei 60 53 von 268 (19,8 %) – zusammen 12,55 % bzw. 11,90 % statt 1/16.
    /// </summary>
    public static (int Total, int Five) FiveMinuteOdds(int effort, bool firstDraw) =>
        PenaltyOdds.Value[Math.Clamp(effort, 0, 100), firstDraw ? 0 : 1];
}
