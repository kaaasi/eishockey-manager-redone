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
    private static GameState Import(string file) =>
        HokCompare.Import(OriginalImportTests.OriginalSave(file + ".HOK"), ClubDatabase.Load(TestGameData.Load()), HokCompare.DefaultSeed);

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

    private static HokCompareRow MoneyRow(string a, string b, Action<GameState>? prepare = null, Action<GameState>? atSeasonOver = null)
    {
        var start = Import(a);
        var target = Import(b);
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
    /// (Nachrechnung im Bericht) – offen unter #733.
    /// </summary>
    [GameDataFact]
    public void File13_MoneyDeviation_ComesFromShortHandedLineup()
    {
        var asImported = MoneyRow("FILE13", "FILE14");
        Assert.True(asImported.Deviation, asImported.ToString());

        var shortHanded = MoneyRow("FILE13", "FILE14", prepare: game =>
        {
            var club = game.Clubs[game.Managers[0].ClubIndex];
            foreach (var slot in LineupSlot.All.Where(s => s.Role == PlayerRole.Goalkeeper || s.Block > 0).ToList())
                club.ClearLineupSlot(slot);
            foreach (var p in club.Players.Where(p => !club.Lineup.Contains(p)).ToList())
                club.Undress(p);
            Assert.Equal(5, club.DressedCount);
            Assert.False(club.AutoLineupEnabled);
        });
        Assert.False(shortHanded.Deviation, shortHanded.ToString());
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
        var asPlayed = MoneyRow("FILE16A", "FILE17");
        Assert.True(asPlayed.Deviation, asPlayed.ToString());

        var withPremiums = MoneyRow("FILE16A", "FILE17", atSeasonOver: game =>
        {
            var manager = game.Managers[0];
            Assert.Equal(0, game.Finance(manager).Book.Current(FinanceCategory.Praemien));
            game.Book(manager, FinanceCategory.Praemien, 2 * GameState.SinglePremium);
        });
        Assert.False(withPremiums.Deviation, withPremiums.ToString());
    }
}
