using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using EishockeyManager.Tools;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur abgleich-fix, Teil 1 (Geld beim Saisonwechsel, #734, #737): Gegenproben zur Ursache. Beide Kontoabweichungen sind keine
/// Fehler der Finanzrechnung: Mit dem Zustand, den das Original im Paar tatsächlich hatte, landet das Remake im Bereich des
/// Abgleichs (<c>HokCompareClasses</c>, Konto: Veränderung seit a ±10 %, mindestens 50.000 DM); ohne ihn nicht. Bericht:
/// <c>docs/analyse/abgleich-original.md</c>.
/// </summary>
public class AbgleichFixMoneyTests
{
    /// <summary>Saaten der Bereichsprobe; seit #730 (Belastung als Sägezahn) streut das Konto je Saat um ±60.000 DM, daher Mittel.</summary>
    // Runde 16, Spur abgleich-rest (#772, R15-E2 = C): 30 statt 6 Saaten und Median statt Mittel. Die Konten sind je Saat zweigipflig
    // (FILE13: Krefeld bei 3 von 30 Saaten nicht Letzter → kein Konto auf 400.000 DM; FILE16A: ±380.000 DM je nach Restsaison); mit
    // sechs Saaten hing das Ergebnis an der Mischung – schon der alte Code scheitert mit 12 Saaten (101.828 bzw. 186.889 > Grenze).
    // Gemessen mit #772 (30 Saaten): FILE13 Median ohne 169.147 / mit 49.400, FILE16A ohne 422.739 / mit 133.408.
    private static readonly int[] Seeds = [HokCompare.DefaultSeed, 1, 2, 3, 77, 4711, .. Enumerable.Range(4, 24)];

    /// <summary>Bereich des Abgleichs für das Konto: Veränderung seit a ±10 %, mindestens 50.000 DM (<c>HokCompareClasses</c>).</summary>
    private static double Range(string a, string b) =>
        Math.Max(50_000, Math.Abs(Import(b, HokCompare.DefaultSeed).Managers[0].Money - Import(a, HokCompare.DefaultSeed).Managers[0].Money) * 0.10);

    /// <summary>
    /// Die Ursache erklärt die Abweichung: mit ihr höchstens zwei Drittel so weit vom Original und höchstens 1,25 × Bereich (gemessen nach
    /// #730: FILE13 235.000 → 51.000 bei Bereich 50.000, FILE16A 300.000 → 166.000 bei 192.000), ohne sie außerhalb.
    /// </summary>
    private static void AssertExplained(double without, double with, double range)
    {
        Assert.True(without > range, $"ohne Ursache {without:F0} im Bereich {range:F0}");
        Assert.True(with <= without * 2 / 3 && with <= range * 1.25, $"mit Ursache {with:F0} (ohne {without:F0}, Bereich {range:F0})");
    }

    /// <summary>Median des Abstands Remake − Original des Kontos über <see cref="Seeds"/> (Name aus der Zeit des Mittels).</summary>
    private static double MeanDistance(string a, string b, Action<GameState>? prepare = null, Action<GameState>? atSeasonOver = null)
    {
        var d = Seeds.Select(seed => MoneyRow(a, b, seed, prepare, atSeasonOver))
            .Select(r => (double)Math.Abs(long.Parse(r.Remake!) - long.Parse(r.Original!))).Order().ToArray();
        return d.Length % 2 == 1 ? d[d.Length / 2] : (d[d.Length / 2 - 1] + d[d.Length / 2]) / 2;
    }

    private static GameState Import(string file, int seed) =>
        HokCompare.Import(OriginalImportTests.OriginalSave(file + ".HOK"), ClubDatabase.Load(TestGameData.Load()), seed);

    /// <summary>Wie <see cref="HokCompare.PlayTo"/>; <paramref name="atSeasonOver"/> läuft am 29. April vor dem Saisonwechsel.</summary>
    private static void PlayTo(GameState game, GameState target, Action<GameState>? atSeasonOver = null)
    {
        game.OnMainMenu(game.ActiveManager, isReturn: false);
        for (var step = 0; step < 5000; step++)
        {
            if (game.Season == target.Season && game.CurrentDay == target.CurrentDay && game.Phase == target.Phase)
                return;
            if (game.Phase == SeasonPhase.SeasonOver)
            {
                atSeasonOver?.Invoke(game);
                game.StartNextSeason();
                game.ReturnToFirstActiveManager();
            }
            else
                game.Continue();
            game.OnMainMenu(game.ActiveManager, isReturn: false);
        }
        Assert.Fail($"Halt nicht erreicht: {HokCompare.Halt(game)}");
    }

    private static HokCompareRow MoneyRow(string a, string b, int seed, Action<GameState>? prepare = null,
        Action<GameState>? atSeasonOver = null)
    {
        var start = Import(a, seed);
        var target = Import(b, seed);
        prepare?.Invoke(start);
        var before = HokCompare.Values(start);
        PlayTo(start, target, atSeasonOver);
        return HokCompare.Rows(target, start, crossesSeason: true, [], before).Single(r => r.Path == "Managers[0].Money");
    }

    /// <summary>
    /// #734 (FILE13 → FILE14, KREFELDER EV): Im Original stand nach dem Laden nur Reihe 1 (2 Verteidiger, 3 Stürmer, kein Torhüter,
    /// „Eingesetzt: 5“, Bild <c>file13_2</c>; FILE14: genau diese fünf eingesetzt, „automatisch aufstellen“ aus). Weniger als neun
    /// Spieler → jedes Spiel 0:2 und 200.000 DM Strafe ([ORIGINAL] 0x544C0, <see cref="GameState.IsShortHanded"/>): Konto 4.2.1994
    /// 250.000 DM (Bild <c>file13_3</c>), Platz 12 → Konto am 29. April auf 400.000 DM gesetzt (+100.000 DM Fair-Play,
    /// <c>file13_15</c>). Mit dieser Aufstellung landet das Remake im Bereich; mit der Aufstellung aus dem HOK (16 Spieler in
    /// Reihen, alle 20 eingesetzt) nicht. Woher die fünf kommen, belegt der Stand nicht: Datei 0x426BC stellt aus FILE13 voll auf
    /// (Nachrechnung im Bericht). „Automatisch aufstellen“ aus ist ein Eingriff (#733: em.exe schreibt die 0 nur nach einer
    /// Nutzerhandlung, <c>AbgleichOriginalPaareTests.HandLineupFile13</c>); die fünf nach dem Laden bleiben offen (#734).
    /// </summary>
    [GameDataFact]
    public void File13_MoneyDeviation_ComesFromShortHandedLineup()
    {
        var asImported = MeanDistance("FILE13", "FILE14");
        var shortHanded = MeanDistance("FILE13", "FILE14", prepare: game =>
        {
            var club = game.Clubs[game.Managers[0].ClubIndex];
            foreach (var slot in LineupSlot.All.Where(s => s.Role == PlayerRole.Goalkeeper || s.Block > 0).ToList())
                club.ClearLineupSlot(slot);
            foreach (var p in club.Players.Where(p => !club.Lineup.Contains(p)).ToList())
                club.Undress(p);
            Assert.Equal(5, club.DressedCount);
            Assert.False(club.AutoLineupEnabled);
        });
        Assert.True(asImported > Range("FILE13", "FILE14"), $"wie importiert: {asImported:F0}");
        AssertExplained(asImported, shortHanded, Range("FILE13", "FILE14"));
    }

    /// <summary>
    /// #737 (FILE16A → FILE17, SV BAYREUTH): Das Original zahlte am 29. April 200.000 DM Einzelprämien (zwei der drei Symbole
    /// bester Torhüter, Topscorer, Fair-Play; Bild <c>file16_9</c>). Die hängen an Spielstatistik und Strafminuten der Restsaison:
    /// Derselbe Start FILE16A ergab im Original auch „4. Platz, Prämien: keine“ (FILE22 → FILE23, <c>file23_1</c>). Mit den
    /// 200.000 DM landet das Remake im Bereich; die übrige Lücke ist der Überziehungszins (10 % je Monat) auf diesen Betrag über
    /// den Sommer.
    /// </summary>
    [GameDataFact]
    public void File16A_MoneyDeviation_ComesFromSinglePremiums()
    {
        var asPlayed = MeanDistance("FILE16A", "FILE17");
        var withPremiums = MeanDistance("FILE16A", "FILE17", atSeasonOver: game =>
        {
            var manager = game.Managers[0];
            // Je Saat zahlt auch das Remake manchmal eine Einzelprämie (Zufall der Restsaison, seit #748 bei einer der Saaten):
            // auf die 200.000 DM des Originals auffüllen.
            var paid = game.Finance(manager).Book.Current(FinanceCategory.Praemien);
            Assert.InRange(paid, 0, 2 * GameState.SinglePremium);
            game.Book(manager, FinanceCategory.Praemien, 2 * GameState.SinglePremium - paid);
        });
        Assert.True(asPlayed > Range("FILE16A", "FILE17"), $"wie gespielt: {asPlayed:F0}");
        AssertExplained(asPlayed, withPremiums, Range("FILE16A", "FILE17"));
    }
}
