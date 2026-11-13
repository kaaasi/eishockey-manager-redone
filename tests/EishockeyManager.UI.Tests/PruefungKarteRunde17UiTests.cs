using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur pruefung-karte Teil 1 (Runde 17), #785 (R783-T2): Kontoanzeige „Saisonende – Ereignisse“ (#768) für weitere Manager und
/// den Bundestrainer. [ORIGINAL] 0x26F20 zeigt je Manager zuerst das Fenster 0x26D9A (Datei 0x2700A) und bucht danach dessen
/// Prämien (Datei 0x270DB–0x27258). Beleg zweiter Manager: <c>duell_s2c_03225</c> (Hauptmenü EISBÄREN BERLIN am Saisonende,
/// 32.650 DM) und <c>duell_s2c_03443</c> (Fenster desselben Managers, „Erhaltene Prämien: 500.000 DM“, Infoleiste 32.650 DM).
/// Beleg Bundestrainer (Liga mit Weltkugel): <c>eisbaeren_f02_02121</c>–<c>02139</c> – das Fenster zeigt das Vereinskonto
/// (205.360 DM), nicht das Konto des Nationalteams.
/// </summary>
public class PruefungKarteRunde17UiTests
{
    /// <summary>
    /// Zwei Manager, beide mit Prämie: Nach dem Wechsel auf den zweiten Manager zeigt die Infoleiste dessen Konto vor dessen
    /// Prämie, nicht das gebuchte Konto und nicht das Konto des ersten Managers. Rückmutation: Abzug nur beim ersten Manager
    /// (<c>result.Manager == 0</c>) → rot.
    /// </summary>
    [GameDataFact]
    public void SeasonEndEvents_SecondManager_ShowsOwnMoneyBeforePremium()
    {
        // Fixlauf kader-rest nach Rebase auf main 1d83cc0 (A130, R15-E2 = C): Saat 4 statt 5 – mit der Startbelastung 80–120 (Datei
        // 0x42E4C) und dem Spielplan über die Satzfolge bekommt Manager 1 bei Saat 5 keine Prämie (Rang 3); unter 1–16 haben beide
        // Manager nur bei 4, 10, 16 eine Prämie. Die Probe hängt an der Saat.
        // Runde 18, Spur abgleich-dosbox Teil 2 (#773, R15-E2 = C): Saat 10 statt 4 – mit den 5-Minuten-Strafen nach dem
        // lrandom-Zyklus (LRandomCycle) bekommt Manager 1 bei Saat 4 keine Prämie; unter 1–16 haben beide Manager bei 2, 7, 9–13
        // und 15 eine Prämie. Rückschaltprobe: MatchEngine.Original.cs von 5367ba8 mit Saat 10 ebenfalls grün – die Probe hängt an
        // der Saat.
        var (d, game) = StartAtSeasonEnd(nationalCoach: false, seed: 10);
        var records = game.History[^1].Managers;
        Assert.Equal(2, records.Count);
        Assert.All(records, r => Assert.True(r.Premium > 0, "Probe ohne Prämie prüft nichts"));

        d.Click(160, 120); // Eingabe: nächster Manager (SeasonEndSummaryScreen.Update)
        Assert.IsType<SeasonEndSummaryScreen>(d.Host.Current);
        Assert.Equal(records[1].Manager, game.CurrentManager);
        d.Frame();
        AssertWindowShowsMoneyBeforePremium(d, game, records[1].Premium);
    }

    /// <summary>
    /// Der Manager ist zugleich Bundestrainer (Liga mit Weltkugel): Die Infoleiste zeigt das Vereinskonto vor der Prämie, nicht
    /// das Konto des Nationalteams. Rückmutation: Kontoabzug nur ohne ernannten Bundestrainer (<c>NationalCoach is null</c>) → rot.
    /// </summary>
    [GameDataFact]
    public void SeasonEndEvents_NationalCoach_ShowsClubMoneyBeforePremium()
    {
        var (d, game) = StartAtSeasonEnd(nationalCoach: true);
        Assert.NotNull(game.NationalCoach);
        Assert.False(game.NationalCoachMenu);
        var result = game.History[^1].Managers[0];
        Assert.Equal(result.Manager, game.CurrentManager);
        Assert.Equal(game.NationalCoach!.Manager, game.CurrentManager);
        Assert.True(result.Premium > 0, "Probe ohne Prämie prüft nichts");
        game.NationalAccount = 777_000; // unterscheidbar vom Vereinskonto
        Assert.NotEqual(game.NationalAccount, game.ActiveManager.Money - result.Premium);

        d.Frame();
        AssertWindowShowsMoneyBeforePremium(d, game, result.Premium);
    }

    private static (ScreenDriver Driver, GameState Game) StartAtSeasonEnd(bool nationalCoach, int seed = 5)
    {
        var d = FlowTests.CreateDriver();
        var setup = new GameSetup();
        int[] clubs = nationalCoach ? [2] : [2, 14];
        for (var i = 0; i < clubs.Length; i++)
            setup.Managers.Add(new ManagerSetup { Name = $"M{i}", Head = i, ClubIndex = clubs[i] });
        var game = NewGame.Create(d.Host.Assets.Database, setup, seed);
        ManagedSquadsUi.PlayAtTopStrength(game);
        d.Host.Game = game;
        d.Host.Show(new MainMenuScreen(d.Host));
        d.Frame();
        for (var guard = 0; d.Host.Current is not SeasonEndSummaryScreen && guard++ < 40000;)
        {
            if (nationalCoach && game.NationalCoach is null && game.Phase == SeasonPhase.Running)
                game.AppointNationalCoach(game.Managers[0]);
            KeepSquadHelper.KeepSquads(game);
            foreach (var m in game.Managers)
                m.Money = Math.Max(m.Money, 2_000_000); // keine Entlassung im Testlauf
            d.Advance();
        }
        Assert.IsType<SeasonEndSummaryScreen>(d.Host.Current);
        return (d, game);
    }

    private static void AssertWindowShowsMoneyBeforePremium(ScreenDriver d, GameState game, long premium)
    {
        var window = MoneyRow(d.Surface);
        var booked = game.ActiveManager.Money;
        var menu = new Surface();
        new MainMenuScreen(d.Host) { SelectedBigTile = -1 }.Draw(menu);
        Assert.NotEqual(MoneyRow(menu), window); // gebuchtes Konto

        game.ActiveManager.Money = booked - premium; // nur der Vergleich, kein Spielablauf danach
        try
        {
            new MainMenuScreen(d.Host) { SelectedBigTile = -1 }.Draw(menu);
            Assert.Equal(MoneyRow(menu), window);
        }
        finally
        {
            game.ActiveManager.Money = booked;
        }
    }

    /// <summary>Kontofeld der Infoleiste (Text bei x = 184, y = 64, Mini-Schrift), wie <c>PruefungKarteRunde16UiTests</c>.</summary>
    private static byte[] MoneyRow(Surface s)
    {
        var bytes = new List<byte>();
        for (var y = 62; y <= 70; y++)
            for (var x = 130; x < 240; x++)
                bytes.Add(s.Pixels[y * Surface.Width + x]);
        return [.. bytes];
    }
}
