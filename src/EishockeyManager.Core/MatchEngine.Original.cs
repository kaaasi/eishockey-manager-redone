namespace EishockeyManager.Core;

/// <summary>
/// Kondition, Technik und Form eines Mannschaftsteils (Abwehr oder Sturm) in einer Reihe, je 0–100.
/// Im Original gepackt zurückgegeben von Datei 0x977F (Form im niedrigsten Byte, dann Technik, Kondition).
/// </summary>
public readonly record struct PartValues(int Condition, int Technique, int Form);

/// <summary>Eine Mannschaft aus Sicht der Torchancen-Berechnung 0x9F5C.</summary>
/// <param name="Strength">Spielstärke aus <see cref="OriginalMatch.TeamStrength"/> (Datei 0x368B1, Gewichtung 1).</param>
/// <param name="Skaters">Feldspieler auf dem Eis (Vereinsfeld 0x32): 5, bei Strafen 4/3, ohne Torhüter 6.</param>
/// <param name="AttackLevel">Angriff 0–7 (Vereinsfeld 0x2C, Bits 4–6).</param>
/// <param name="GoaliePulled">Torhüter draußen (Vereinsfeld 0x2C, Bit 3).</param>
public readonly record struct MatchSide(int Strength, int Skaters = OriginalMatch.FullStrength, int AttackLevel = 0, bool GoaliePulled = false);

/// <summary>
/// Rechenteil der Spielberechnung des Originals ohne Anzeige (Spur spielberechnung, Teil 1).
/// Fundstellen und Ablauf: <c>docs/spielberechnung.md</c>. Angeschlossen über <see cref="MatchEngine.PlayWithEvents"/>
/// (Teil 3).
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>[ORIGINAL] Zwei Zufallsfunktionen: <c>random(a, b)</c> (C-<c>rand</c>, Datei 0xC037) und eine eigene Folge
///   (Datei 0xBFD5, x = (16·x + 167) mod 4.000.000). Beide liefern a … b einschließlich.</item>
/// <item>[APPROX] Das Remake zieht beide aus einem <see cref="Random"/>; Reihenfolge und Grenzen der Züge sind die des
///   Originals, die Zahlenfolge nicht.</item>
/// </list>
/// </remarks>
public static class OriginalMatch
{
    /// <summary>[ORIGINAL] Feldspieler bei voller Stärke (Spielvorbereitung Datei 0x84C2: Vereinsfeld 0x32 = 5).</summary>
    public const int FullStrength = 5;

    /// <summary>[ORIGINAL] Höchstgrenze der Torchancen aus der Stärkedifferenz 1 … 19 (DGROUP 0x4948, Index 0 … 19).</summary>
    public static readonly int[] ChanceLimit = [2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4];

    /// <summary>[ORIGINAL] Gewichte Abwehr/Sturm in Prozent je Modus 0–2 (DGROUP 0x4E99: 85/15, 50/50, 25/75).</summary>
    public static readonly int[,] PartWeights = { { 85, 15 }, { 50, 50 }, { 25, 75 } };

    /// <summary>Original-<c>random(a, b)</c> mit beiden Grenzen inklusive.</summary>
    internal static int Rnd(Random random, int min, int max) => random.Next(min, max + 1);

    /// <summary>
    /// Werte eines Mannschaftsteils in einer Reihe für einen Verein ohne Manager (bzw. ohne Live-Aufstellung).
    /// [ORIGINAL] Datei 0x97F3–0x98F5, 0x9C66–0x9CDB:
    /// <list type="bullet">
    /// <item>Grundwerte = Teamwert + 4 − 4 · Reihe (Reihe 0–2) für Kondition (Vereinsfeld 0x18/0x19), Technik (0x1A/0x1B), Form (0x1C/0x1D).</item>
    /// <item>Überzahl/Unterzahl <paramref name="skaterAdvantage"/> (eigene minus gegnerische Feldspieler): Abwehr in
    ///   Unterzahl Technik &gt;&gt; Unterzahl; Sturm in Überzahl Technik + (Technik &gt;&gt; (3 − Überzahl)).</item>
    /// <item>Technik · (Energie der Reihe / 4 + 75) / 100 (Vereinsfeld 0x1E + 4 · Teil + Reihe; ohne Spiel 100 %).</item>
    /// <item>Im Spiel: Kondition − (100 − Einsatz · Kondition / 100) / 4 (Einsatz = Vereinsfeld 0x26).</item>
    /// <item>Alle drei Werte auf 0 … 100 begrenzt (0x14CF:0x52).</item>
    /// </list>
    /// </summary>
    public static PartValues LineValues(PartValues team, int line, bool attack, int skaterAdvantage, int lineEnergy, int effort, bool inMatch)
    {
        var shift = 4 - 4 * line;
        int c = team.Condition + shift, t = team.Technique + shift, f = team.Form + shift;
        if (skaterAdvantage != 0)
        {
            if (!attack && skaterAdvantage < 0)
                t >>= Math.Min(15, -skaterAdvantage);
            if (attack && skaterAdvantage > 0)
                t += t >> Math.Max(0, 3 - skaterAdvantage);
        }
        var energyFactor = inMatch ? (lineEnergy >> 2) + 75 : 100;
        t = t * energyFactor / 100;
        if (inMatch)
            c -= (100 - effort * c / 100) / 4;
        return new PartValues(Math.Clamp(c, 0, 100), Math.Clamp(t, 0, 100), Math.Clamp(f, 0, 100));
    }

    /// <summary>
    /// Spielstärke einer Mannschaft. [ORIGINAL] Datei 0x368B1: je Teil (Abwehr, Sturm) Technik − Minute · 10 / (Technik + 1)
    /// (nicht unter 0), dazu Kondition und Form, höchstens 255; gewichtet nach <see cref="PartWeights"/> (ganzzahlig, je Teil
    /// /100); ist die Summe nicht 0, kommt (Vereinsfeld 0x17 − 44) / 2 dazu (0x17 = random(47, 53) bei Spielbeginn).
    /// </summary>
    public static int TeamStrength(PartValues defense, PartValues attack, int mode, int minute, int clubForm)
    {
        var sum = PartStrength(defense, minute) * PartWeights[mode, 0] / 100
                  + PartStrength(attack, minute) * PartWeights[mode, 1] / 100;
        return sum == 0 ? 0 : sum + (clubForm - 44) / 2;
    }

    private static int PartStrength(PartValues v, int minute)
    {
        var t = Math.Max(0, v.Technique - minute * 10 / (v.Technique + 1));
        return Math.Min(255, t + v.Condition + v.Form);
    }

    /// <summary>Endergebnis in einem Zug (schneller Weg des Spieltagsablaufs, Datei 0x3731F → 0x9F5C(…, 0, 60)).</summary>
    public static (int Home, int Away) FullResult(MatchSide home, MatchSide away, bool worldCup, bool playoff, Random random)
    {
        var (h, a) = Budget(home, away, 0, 60, worldCup, playoff, random);
        return (h, a);
    }

    /// <summary>
    /// Torchancen einer Spielminute: Bit 0 Heim, Bit 1 Gast. [ORIGINAL] 0x9F5C mit <paramref name="minute"/> ≠ 0 bzw.
    /// <paramref name="periodEnd"/> ≠ 60: je Torbudget-Punkt mit 1/20 eine Chance. Beim Gast wird der Zähler je Treffer
    /// um 2 erhöht (Datei 0xA3E2) – zwei Treffer ergeben 4, das Bit 1 ist dann aus; übernommen wie im Original.
    /// </summary>
    public static int MinuteChances(MatchSide home, MatchSide away, int minute, int periodEnd, bool worldCup, bool playoff, Random random)
    {
        var (h, a) = Budget(home, away, minute, periodEnd, worldCup, playoff, random);
        if (playoff || periodEnd > 60)
        {
            h++;
            a++;
        }
        var flags = 0;
        for (var i = 0; i < h; i++)
        {
            if (Rnd(random, 1, 20) == 1)
                flags = 1;
        }
        for (var i = 0; i < a; i++)
        {
            if (Rnd(random, 1, 20) == 1)
                flags += 2;
        }
        return flags;
    }

    /// <summary>
    /// Torbudget beider Mannschaften. [ORIGINAL] Datei 0x9F5C–0xA35A (Ablauf in <c>docs/spielberechnung.md</c>).
    /// </summary>
    private static (int Home, int Away) Budget(MatchSide home, MatchSide away, int from, int to, bool worldCup, bool playoff, Random random)
    {
        var wm = worldCup ? 1 : 0;
        // 0x9F92–0xA019: Angriffszuschlag 0–2 = max(0, random(0, Angriff) − 5)
        var bonusHome = Math.Max(0, Rnd(random, 0, home.AttackLevel) - 5);
        var bonusAway = Math.Max(0, Rnd(random, 0, away.AttackLevel) - 5);
        var sh = home.Strength;
        var sa = away.Strength;
        var d = sh / 3 - sa / 3;
        var ga = worldCup && d < 19 ? Rnd(random, 1, 2) : 0;
        var gh = d > -19 ? Rnd(random, 1, 2) : 0;
        if (d > 0)
        {
            var limit = d > 19 ? 5 : ChanceLimit[d];
            var x = Rnd(random, 1, limit);
            var y = Rnd(random, 1, limit);
            gh += d > 8 && Rnd(random, 0, 2) == 0 ? BetterOf(x, y, random) : x;
            if (d < 25)
                ga += Rnd(random, 1, 2);
        }
        else
        {
            d = -d;
            var limit = d > 19 ? d / 5 : ChanceLimit[d];
            if (d < 25)
                gh += Rnd(random, 1, 3 - wm);
            var x = Rnd(random, 1, limit);
            var y = Rnd(random, 1, limit);
            ga += d > 8 && Rnd(random, 0, 3 - wm) == 0 ? BetterOf(x, y, random) : x;
        }
        // 0xA233–0xA298: je ein Punkt, der Angriffszuschlag des Gegners erhöht die eigene Chance nicht, sondern die des Gegners
        if (Rnd(random, 0, 1) == 0 || Rnd(random, 0, bonusHome) == 0)
            gh++;
        else
            ga++;
        if (Rnd(random, 0, 1) == 0 || Rnd(random, 0, bonusAway) == 0)
            ga++;
        else
            gh++;
        if (!(from == 0 && to == 60))
        {
            while (gh > 10 || ga > 10)
            {
                gh--;
                ga--;
            }
        }
        gh = Math.Max(0, gh);
        ga = Math.Max(0, ga);
        // 0xA2BF–0xA2E0: Mannschaft ohne Stärke (keine Aufstellung)
        if (sh == 0)
        {
            gh = 0;
            if (sa > 20)
                ga = 9;
        }
        if (sa == 0)
        {
            ga = 0;
            if (sh > 20)
                gh = 9;
        }
        // 0xA2E1–0xA2FF: Play-off-/Aufstiegsrunde (Wettbewerbscode 1) und Verlängerung halbieren
        if (playoff || from > 60)
        {
            if (sh != 0)
                gh = gh / 2 + 1;
            if (sa != 0)
                ga = ga / 2 + 1;
        }
        // 0xA305–0xA31B: Unterzahl viertelt
        if (home.Skaters < away.Skaters)
            gh >>= 2;
        if (away.Skaters < home.Skaters)
            ga >>= 2;
        // 0xA31D–0xA354: Torhüter draußen vor Minute 58
        if (from < 58 && (home.GoaliePulled || away.GoaliePulled))
            gh = ga = 9;
        return (gh, ga);
    }

    /// <summary>[ORIGINAL] Datei 0x3688F: mit 1/2 der größere Wert, sonst der zweite.</summary>
    private static int BetterOf(int x, int y, Random random) => Rnd(random, 0, 1) != 0 && x > y ? x : y;

    /// <summary>
    /// Strafe in dieser Minute? [ORIGINAL] Datei 0x358B7–0x3592D (Aufruf jede Minute für jede Mannschaft, 0x3808E):
    /// Strafe, wenn lrandom(0, 12) = 0 oder lrandom(0, 110 − Einsatz) = 0 (der zweite Zug nur, wenn der erste nicht 0 ist).
    /// Ist der erste Platz der Strafbank frei, wird es mit 1/16 (lrandom(0, 15) = 0) eine 5-Minuten-Strafe.
    /// Einsatz = Vereinsfeld 0x26, beim Managerverein + Managerwert 0x974B − 20, begrenzt auf 0 … 100.
    /// </summary>
    /// <returns>0 (keine Strafe), 2 oder 5 Minuten.</returns>
    public static int PenaltyMinutes(int effort, bool firstBoxSlotFree, Random random)
    {
        effort = Math.Clamp(effort, 0, 100);
        if (Rnd(random, 0, 12) != 0 && Rnd(random, 0, 110 - effort) != 0)
            return 0;
        return firstBoxSlotFree && Rnd(random, 0, 15) == 0 ? 5 : 2;
    }
}
