using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur pruefung-karte Teil 2 (Runde 17): gesammelte P3 aus den Reviews der Runde.
/// </summary>
public class PruefungKarteRunde17Tests
{
    /// <summary>Börsentage einer Saison im Remake (FinanceDay 0–231 je Saison, gemessen an der Probe von #260).</summary>
    private const int DaysPerSeason = 231;

    /// <summary>
    /// #800 (R791-B1): Wachstum der Basiswerte mit großem Saatsatz, nur das Kursmodell (Tagesrechnung 0x5784A) mit eigenen,
    /// unabhängigen Zufallsfolgen. Die Probe im Spiel (<see cref="BoerseStartTests.MultiSeason_BaseValuesGrow_AcrossSeeds"/>)
    /// zieht keine unabhängigen Stichproben: <see cref="GameState.FinanceRandom"/> hängt nur an Saison und Managern, nicht an der
    /// Saat des Spiels – alle Saaten starten die Börse mit denselben Kursen (1993: Basiswerte 6448/11273/13864/17455) und
    /// unterscheiden sich nur in der Verschränkung mit den übrigen Finanzzügen. Gemessen am Stand e947eb5: im Spiel 406 von 768
    /// (Saaten 1–192: 47 %, 57 %, 51 %, 56 % je 48), Kursmodell ab diesem Startstand 60,0 % (1500 Folgen), ab dem Programmstart
    /// 62,5 % (4000 Folgen, 4 · 231 Tage). Hier: 1000 Folgen ab dem Programmstart.
    /// <para>
    /// #803 (R802-01): Das frühere Fenster 55–70 % hielt auch ein Drittel der Rate (1/10.000 je Tag) für richtig. Gemessen am
    /// Stand 5367ba8 mit veränderter Rate r je 10.000 und Tag (0x57854–0x57897, Original r = 3), Anteil gewachsen /
    /// Median ln(Basis Ende / Basis Anfang): r = 0: 43,9 % / −0,001; r = 1: 56,0 % / 0,056; r = 2: 58,0 % / 0,142;
    /// r = 3: 63,0 % / 0,236; r = 4: 66,4 % / 0,330; r = 6: 75,0 % / 0,496. Der Median folgt der Rate deutlich (ohne
    /// Kursereignisse 924 · ln 1,0003 = 0,277; die Neusetzungen des Basiswerts drücken ihn); das Fenster 0,19–0,28 trennt
    /// r = 3 von r = 2 und r = 4, der Anteil 60–66 % zusätzlich r ≤ 2 und r ≥ 5.
    /// </para>
    /// </summary>
    [Fact]
    public void StockModel_BaseValuesGrow_LargeSeedSet()
    {
        int grown = 0, total = 0;
        var logGrowth = new List<double>();
        for (var seed = 1; seed <= 1000; seed++)
        {
            var random = new GameRandom(seed);
            var market = StockMarket.Create(random);
            var bases = market.Stocks.Select(s => s.Base).ToArray();
            for (var day = 0; day < 4 * DaysPerSeason; day++)
                market.Advance(random);
            grown += market.Stocks.Zip(bases).Count(p => p.First.Base > p.Second);
            logGrowth.AddRange(market.Stocks.Zip(bases).Select(p => Math.Log((double)p.First.Base / p.Second)));
            total += market.Stocks.Count;
        }
        logGrowth.Sort();
        Assert.InRange(grown * 100 / total, 60, 66);
        Assert.InRange(logGrowth[logGrowth.Count / 2], 0.19, 0.28);
    }
}
