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
        // Runde 20, Spur pruefung-karte Teil 2 (#872, R15-E2 = C): Saat 11 statt 10 – mit den Reihen aus dem Vorspiel (0x2E/0x30,
        // Datei 0x84EB–0x857D) wird Manager 0 bei Saat 10 Fünfter ohne Prämie; unter 1–16 haben beide Manager bei 1, 4, 5, 7–9, 11,
        // 13 und 15 eine Prämie. Mit altem Code ist Saat 11 ebenfalls grün – die Probe hängt an der Saat.
        // Runde 21, Spur start-lader (R21-D2 = A): Saat 8 statt 11 – mit dem Lader beim Spielstart (Datei 0x439BB, anderer
        // Zufallsverlauf) hat unter 1–5, 7–16 nur bei Saat 8 auch Manager 1 eine Prämie. Die Probe hängt an der Saat.
        // Runde 21, Spur teamwert-aufstellung (A080, R21-D2 = A): Saat 4 statt 8 – mit der Spielvorbereitung aus den Vereinsfeldern
        // des Managervereins (Datei 0x097F3–0x0984F, anderer Einsatz der Automatik, anderer Zufallsverlauf) bekommt Manager 0 bei
        // Saat 8 keine Prämie (Rang 4); unter 1–16 haben beide Manager bei 4, 6, 7, 9, 14 und 16 eine Prämie. Die Probe hängt an
        // der Saat.
        // Runde 21, Spur zusehen Teil 2 (A541, R21-D2 = A, Rebase auf teamwert-aufstellung): Saat 6 statt 4 – die Ernennung zum
        // Bundestrainer im Saisonverlauf schaltet „WM-Spiele“ ein (Datei 0x5545F), die WM-Gruppenspiele laufen durch die
        // Minutenschleife (Datei 0x3702E, anderer Zufallsverlauf); bei Saat 4 bekommt Manager 1 keine Prämie. Unter 1–16 haben
        // beide Manager bei 6, 7, 14 und 16 eine Prämie. Rückschaltprobe: ohne „Options.ShowWorldCup = true“ in
        // GameState.ShowWorldCupForNationalCoach hat Saat 4 wieder beide Prämien; Saat 6 ist auch dort grün – die Probe hängt an
        // der Saat.
        // Runde 21, Spur zufallsstrom Teile 2 und 3 (R21-D2 = A, nach Rebase auf teamwert-aufstellung): Saat 5 statt 4 – mit Wanderung
        // in Satzfolge (Datei 0x9D1B), Tendenz im Tageslauf (Datei 0x07B77) und Faktor je Managerzug (Datei 0x44E3F) im gemeinsamen
        // Strom wird Manager 1 bei Saat 4 Fünfter ohne Prämie; unter 1–16 haben beide Manager bei 5, 8 und 15 eine Prämie. Die Probe
        // hängt an der Saat.
        // Runde 21, Spur zufallsstrom Teil 4 (R21-D2 = A, R15-E2 = C): Saat 2 statt 5 – Pokalauslosungen (Datei 0x298B1, 0x43A8C) und Pokalspiele ziehen aus dem gemeinsamen Strom statt aus CupRandom, anderer Zufallsverlauf ab dem Spielstart; bei Saat 5 bekommt Manager 1 keine Prämie, unter 1–10 haben beide Manager bei 2 und 8 eine Prämie. Die Probe hängt an der Saat.
        // Runde 22, Spur kaderplatz-werte Teil 2 (A093; R21-D2 = A, R15-E2 = C): Saat 3 statt 2 – die Spielabrechnung 0x544C0 würfelt vor jedem Spiel eines Managervereins je Kaderplatz random(0, 4) (Datei 0x548A8–0x548E7) aus dem gemeinsamen Strom, anderer Zufallsverlauf ab dem ersten Managerspiel; bei Saat 2 bekommt ein Manager keine Prämie, unter 1–16 haben beide Manager bei 1, 3, 4, 6, 8
        // und 11 eine Prämie. Rückschaltprobe: src/ von faa64d2 mit Saat 3 rot (eine Prämie fehlt) – die Probe hängt an der Saat.
        // Runde 25, Spur saison-geld Teil 1 (T03, R21-D2 = A, R15-E2 = C): Saat 7 statt 3 – Markt-Takt random(0, 3) je Markt-Platz (A829, Datei 0x07215),
        // anderer Zufallsverlauf; bei Saat 3 bekommt Manager 0 keine Prämie, unter 1–15 erfüllen 7, 8, 10–14 die Vorbedingung.
        var (d, game) = StartAtSeasonEnd(nationalCoach: false, seed: 7);
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
