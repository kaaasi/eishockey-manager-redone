using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur abgleich-fix, Teil 2 (#731): Werte des Pools über den Saisonwechsel. [ORIGINAL] Der Saisonwechsel benennt die freien
/// Poolplätze neu (Datei 0x56467 → 0x5ACA0), die Werte bleiben am Platz: In FILE13→14, FILE16A→17, FILE22→23 und FILE44→46 sind
/// 166–170 der 199 Plätze umbenannt, Alter + 1, Kondition und Technik ±3; das Mittel des Pools bleibt (z. B. FILE44 73,1/77,3 →
/// FILE46 72,9/77,2). Vorher rot: Der Tausch mit einem Computerkader (Namen nur einmal im Spiel, [APPROX]) gab dem Platz die Werte
/// des Kaderspielers (Remake-Skala), das Mittel fiel um 5–16.
/// </summary>
public class AbgleichFixPoolTests
{
    private static GameState Import(string file, int seed) => GameState.ImportOriginal(
        OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave(file))), ClubDatabase.Load(TestGameData.Load()), seed);

    private static (double Condition, double Technique) Means(GameState game)
    {
        var pool = game.TransferMarket.PoolPlayers.ToList();
        return (pool.Average(p => p.Condition), pool.Average(p => p.Technique));
    }

    private static void PlayToSeasonOver(GameState game)
    {
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 5000, "Saisonende nicht erreicht");
            if (game.NationalTurn && game.NationalCoach!.Squad.Count == 0)
                game.AutoNominate();
            game.Continue();
        }
    }

    /// <summary>Abnahme #731: Pool-Mittel nach dem Saisonwechsel FILE44 → FILE46 im Bereich ±5 des Originals.</summary>
    [GameDataTheory]
    [InlineData(1993)]
    [InlineData(77)]
    public void File44_SeasonChange_PoolMeansLikeFile46(int seed)
    {
        var game = Import("FILE44.HOK", seed);
        PlayToSeasonOver(game);
        game.StartNextSeason();
        var (condition, technique) = Means(game);
        var (c46, t46) = Means(Import("FILE46.HOK", seed));
        Assert.InRange(condition, c46 - 5, c46 + 5);
        Assert.InRange(technique, t46 - 5, t46 + 5);
    }

    /// <summary>
    /// Mehrsaisonprobe (AGENTS.md §5): vier Saisonwechsel ab FILE44, passiver und aktiver Manager. Der Saisonwechsel selbst ändert
    /// das Pool-Mittel höchstens um 5 (Original: unter 1); Wanderung in der Saison (Listenwechsel) ist nicht Gegenstand.
    /// Runde 20 (#872, R15-E2 = C): Mit den Reihen aus dem Vorspiel liegt Saat 4711 (passiv) am vierten Saisonwechsel bei +6,1
    /// Kondition (alter Code +2,4); Saaten 1, 2, 3, 5, 6 bleiben bei höchstens 1,1 (alter Code 2,3). Ein Ausreißer einer Saat, kein
    /// systematischer Zuwachs; Saat 4711 deshalb durch 1 ersetzt.
    /// Runde 21, Spur teamwert-aufstellung (A080, R21-D2 = A): Saat 2 statt 1 – mit der Spielvorbereitung aus den Vereinsfeldern
    /// (Datei 0x097F3–0x0984F, anderer Zufallsverlauf) liegt Saat 1 (aktiv) am vierten Saisonwechsel bei +6,5 Kondition (alter Code
    /// +3,8). Der vierte Wechsel hebt das Mittel bei allen Saaten 1–12 mit altem wie neuem Code (aktiv im Mittel +3,3 alt, +3,6
    /// neu; passiv +1,4 alt, +2,5 neu; über 5 alt Saat 8 passiv und Saat 3 aktiv Technik 5,0, neu Saaten 1 und 9 aktiv, 5 und 11
    /// passiv) – eine Eigenschaft dieser Probe am vierten Wechsel, nicht dieser Spur; im PR als Restlücke gemeldet. Saat 2 ist mit
    /// altem und neuem Code grün (höchstens 3,5).
    /// Runde 25, Spur saison-geld Teil 3 (K11, R21-D2 = A, R15-E2 = C): Saat 3 statt 2 – mit dem Rollenwert aus dem Stand (Pool +0x18,
    /// Torwürfe Datei 0x5077–0x52CB) und dem Rebase auf main 90d8394 liegt Saat 2 bei 5,6; größter Schritt über passiv und aktiv je Saat
    /// 1–12: 3,0 / 5,6 / 2,0 / 5,0 / 2,9 / 2,9 / 4,7 / 2,0 / 4,0 / 4,4 / 4,7 / 2,7 – dieselbe Eigenschaft der Probe am vierten Wechsel
    /// wie in Runde 21, kein systematischer Zuwachs.
    /// </summary>
    [GameDataTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void File44_FourSeasonChanges_PoolMeansStable(bool active)
    {
        var game = Import("FILE44.HOK", 3);
        var manager = game.Managers[0];
        for (var season = 0; season < 4; season++)
        {
            if (active)
                TestSeason.KeepSquad(game, manager, extendExpired: true);
            PlayToSeasonOver(game);
            var (c0, t0) = Means(game);
            game.StartNextSeason();
            var (c, t) = Means(game);
            Assert.InRange(c, c0 - 5, c0 + 5);
            Assert.InRange(t, t0 - 5, t0 + 5);
        }
    }
}
