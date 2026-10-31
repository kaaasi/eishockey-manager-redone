using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>Paket „wettbewerbe“: Bildschirm „DEB-Pokal Auslosung“ (Paarungen → DEB-Pokal).</summary>
public class DebCupScreenTests
{
    /// <summary>[ORIGINAL] Plätze nach em.exe 0x33E5E, vermessen am Video (3:55): 16 Zeilen ab y = 40, Abstand 11.</summary>
    [Fact]
    public void SlotPositions_AsInOriginal()
    {
        Assert.Equal((37, 40), DebCupDrawScreen.SlotPosition(0, 32));
        Assert.Equal((164, 40), DebCupDrawScreen.SlotPosition(1, 32));
        Assert.Equal((164, 205), DebCupDrawScreen.SlotPosition(31, 32));
        Assert.Equal((37, 84), DebCupDrawScreen.SlotPosition(0, 16)); // zweite Runde: mittig
        Assert.Equal((164, 123), DebCupDrawScreen.SlotPosition(1, 2)); // Endspiel: 128 − (1 · 11)/2
    }

    /// <summary>
    /// R4-01: Paarungen → „DEB-Pokal“ ruft im Original denselben Stub wie „Tabelle DEB-Pokal“ (Datei 0x4584A/0x45897 → 0x32E23)
    /// und zeigt „DEB-Pokalspiele“, nicht die Auslosung.
    /// </summary>
    [GameDataFact]
    public void PairingsTile_OpensOverview_LikeTableTile()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var before = StateFingerprint.Of(d.Host.Game!);

        d.OpenTile("DEB-Pokal");
        d.Frame();
        Assert.IsType<EishockeyManager.UI.Screens.Competitions.DebCupGamesScreen>(d.Host.Current);
        FlowTests.ReturnToMainMenu(d);
        Assert.Equal(before, StateFingerprint.Of(d.Host.Game!));
    }

    [GameDataFact]
    public void DrawScreen_ShowsEveryNameInItsStrip_WithoutChangingState()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var before = StateFingerprint.Of(game);

        // Die Auslosung hat keine eigene Kachel mehr; sie wird direkt gezeigt (Ablauf „CupDrawStep“ folgt im Spieltagsablauf)
        d.Host.Show(new DebCupDrawScreen(d.Host, new MainMenuScreen(d.Host)));
        d.Frame();
        var screen = Assert.IsType<DebCupDrawScreen>(d.Host.Current);
        Assert.Equal(game.DebCup!.Draw.Select(c => (int?)c), screen.Slots);
        Assert.Contains((int?)game.ActiveManager.ClubIndex, screen.Slots);

        // Jeder Name steht im Streifen seines Platzes (x 37–157 bzw. 164–284, 10 Zeilen hoch)
        var pixels = d.Surface.Pixels;
        for (var i = 0; i < screen.Slots.Count; i++)
        {
            var (x, y) = DebCupDrawScreen.SlotPosition(i, screen.Slots.Count);
            var ink = 0;
            for (var py = y; py < y + DebCupDrawScreen.StripHeight; py++)
                for (var px = x; px < x + DebCupDrawScreen.StripWidth; px++)
                {
                    var c = pixels[py * Surface.Width + px];
                    if (c == Colors.Yellow || DebCupDrawScreen.ClassColors.Contains(c))
                        ink++;
                }
            Assert.True(ink > 20, $"Platz {i}: kein Name im Streifen");
        }

        d.Advance();
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        Assert.Equal(before, StateFingerprint.Of(game));
    }

    /// <summary>Auf einem angehaltenen Pokaltag: Öffnen spielt nichts und lost nichts aus.</summary>
    [GameDataFact]
    public void OnPausedCupDay_OpeningDrawChangesNothing()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var club = game.ActiveManager.ClubIndex;
        for (var guard = 0; game.MatchOn(game.CurrentDay, club)?.Competition != Competition.DebCup; guard++)
        {
            Assert.True(guard < 30, "Kein Pokaltermin des Managers erreicht");
            d.PlayMatchDay();
        }
        var before = StateFingerprint.Of(game);
        var round = game.DebCup!.CurrentRound;

        d.OpenTile("DEB-Pokal");
        d.Frame();
        FlowTests.ReturnToMainMenu(d);

        Assert.Equal(round, game.DebCup.CurrentRound);
        Assert.Equal(before, StateFingerprint.Of(game));
    }

    [GameDataFact]
    public void EmptySlots_ShowGreenStrips_AndNoKeyHint()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var empty = Enumerable.Repeat<int?>(null, DebCup.Teams).ToList();
        d.Host.Show(new DebCupDrawScreen(d.Host, new MainMenuScreen(d.Host), empty));
        d.Frame();

        var strip = d.Host.Assets.Image(DebCupDrawScreen.StripGraphic);
        foreach (var slot in new[] { 0, 1, 30, 31 })
        {
            var (x, y) = DebCupDrawScreen.SlotPosition(slot, DebCup.Teams);
            for (var py = 0; py < strip.Height; py++)
                for (var px = 0; px < strip.Width; px++)
                    Assert.Equal(strip.Pixels[py * strip.Width + px], d.Surface.Pixels[(y + py) * Surface.Width + x + px]);
        }
        // „TASTE DRÜCKEN“ erst nach der letzten Kugel: Zeilen 217–222 zwischen x 90 und 237 ohne Weiß
        for (var py = 217; py <= 222; py++)
            for (var px = 90; px < 237; px++)
                Assert.NotEqual(Colors.White, d.Surface.Pixels[py * Surface.Width + px]);
    }
}
