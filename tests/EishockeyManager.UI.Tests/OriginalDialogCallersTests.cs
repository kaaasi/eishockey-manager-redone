using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Screens.ManagerArea;
using EishockeyManager.UI.Screens.Team;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Integration Runde 11 (ui-meldungen): Aufrufer in fremden Dateien auf die Bausteine <see cref="OriginalDialog"/> (Datei 0x4C246)
/// und <see cref="ShortMessage"/> (Datei 0x0C0B8) umgestellt – Lage nach <c>docs/analyse/re-meldungen.md</c> §B/§C (Symbolfelder
/// bei x 83/212, y 112; Fenster ab 75 | 107; Knöpfe 94/164 | 166). Gegenbeispiele (vorher): „?“-Felder bei 82/84 | 110–111 mit
/// Lichtkante, eigene Rahmen, Meldungen mit Klick zu schließen.
/// </summary>
public class OriginalDialogCallersTests
{
    private static byte Px(Surface s, int x, int y) => s.Pixels[y * Surface.Width + x];

    private static void AssertQuestionFieldsAtOriginalPosition(Surface s)
    {
        foreach (var x0 in new[] { 83, 212 })
        {
            Assert.Equal(14, Px(s, x0, 112)); // Fläche ab (x0 | 112)
            Assert.Equal(12, Px(s, x0 + 1, 113)); // Umriss um ein Pixel eingerückt
            Assert.NotEqual(15, Px(s, x0 + 2, 114)); // keine Lichtkante
        }
    }

    private static Surface Render(Action<Surface, Assets> draw)
    {
        var s = new Surface();
        s.Clear(30);
        draw(s, new Assets(TestGameData.Load()));
        return s;
    }

    // ---- TransferDialog (A376, A378, A379) --------------------------------------------------------------------------

    [GameDataFact]
    public void TransferQuestion_Talent_UsesOriginalQuestionLayout()
    {
        var dialog = TransferDialog.ForTalent(250_000);
        Assert.Equal(OriginalDialog.LeftButton, dialog.ConfirmButton);
        Assert.Equal(OriginalDialog.RightButton, dialog.CancelButton);
        Assert.Equal(OriginalDialog.Window, TransferDialog.QuestionBox);

        var s = Render((surface, a) => dialog.Draw(surface, a));
        AssertQuestionFieldsAtOriginalPosition(s);
        Assert.Equal(30, Px(s, 74, 107)); // Fenster ab (75 | 107)
        Assert.NotEqual(30, Px(s, 75, 107));
        // untere Linie einfarbig 0 (vorher zweifarbig 37/dunkel)
        Assert.All(Enumerable.Range(80, 158), x => Assert.Equal(0, Px(s, x, 163)));
    }

    [GameDataFact]
    public void TransferQuestion_AsDialog_CarriesTitleLinesAndButtons()
    {
        var dialog = TransferDialog.ForTalent(250_000).AsDialog("LetsShow");
        Assert.Equal(OriginalDialogKind.Question, dialog.Kind);
        Assert.Equal("LetsShow", dialog.Title);
        Assert.Equal(3, dialog.TextLines.Count);
        Assert.Equal("Aber immer!", dialog.LeftText);
        Assert.Equal("Lieber nicht.", dialog.RightText);
    }

    // ---- DEB-Pokal-Auslosung (A270, A273) ---------------------------------------------------------------------------

    [GameDataFact]
    public void DebCupDrawQuestion_UsesOriginalQuestionLayout()
    {
        var d = DebCupDrawFlowTests.StartStoppingAtDraw();
        Assert.IsType<DebCupDrawLiveScreen>(d.Host.Current);
        d.Frame();
        AssertQuestionFieldsAtOriginalPosition(d.Surface);
        Assert.Equal(OriginalDialog.LeftButton, DebCupDrawLiveScreen.YesButton);
        Assert.Equal(OriginalDialog.RightButton, DebCupDrawLiveScreen.NoButton);
        Assert.Equal(OriginalDialog.Window, DebCupDrawLiveScreen.DialogBox);
        // Die Antwort bleibt wie zuvor: Klick auf den linken Knopf = „Aber klar“
        d.Click(OriginalDialog.LeftButton.X + 5, OriginalDialog.LeftButton.Y + 3);
        Assert.Equal(DebCupDrawLiveScreen.Phase.Reveal, ((DebCupDrawLiveScreen)d.Host.Current!).Current);
    }

    // ---- Werbung: Verlängerungsfrage (A304, A308) -------------------------------------------------------------------

    [GameDataFact]
    public void AdvertisingRenewalQuestion_UsesOriginalQuestionLayout()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.OpenTile("Werbung");
        var screen = Assert.IsType<AdvertisingScreen>(d.Host.Current);
        var game = d.Host.Game!;
        var me = game.ActiveManager;
        var tv = new SponsorContract { Kind = AdSlotKind.Fernsehen, Sponsor = 1, Boards = [], MonthlyAmount = 9_000, OptionMonths = 12, SignedRating = 0 };
        game.Ads(me).Contracts.Add(tv);
        d.Click(AdvertisingScreen.TvPanel.X + 5, AdvertisingScreen.TvPanel.Y + 5);
        Assert.Same(tv, screen.RenewalQuestion);

        d.Frame();
        AssertQuestionFieldsAtOriginalPosition(d.Surface);
        Assert.Equal(OriginalDialog.LeftButton, AdvertisingScreen.YesButton);
        Assert.Equal(me.Name, screen.RenewalDialog!.Title); // Überschrift = Manager am Zug
        Assert.Equal(["Möchten Sie den Vertrag mit", tv.Partner.Name, "verlängern ?"], screen.RenewalDialog.TextLines);
    }

    // ---- TeamUi.MessageBox (A375, M-K3) -----------------------------------------------------------------------------

    [GameDataFact]
    public void MessageBox_TwoLines_IsOriginalMessageWindowWithoutButtons_ClosedByLeftClickOnly()
    {
        var box = new MessageBox(() => "Hans");
        box.Show("Der darf da", "nicht rein !");
        var s = Render((surface, a) => box.Draw(surface, a));
        Assert.Equal(17, Px(s, 83, 112)); // „!“-Felder (Meldung: Fläche 17, Umriss 16)
        Assert.Equal(16, Px(s, 84, 113));
        Assert.Equal(30, Px(s, 74, 107)); // Fenster ab (75 | 107), nicht mehr um die Bildmitte zentriert
        Assert.NotEqual(30, Px(s, 75, 107));
        var button = OriginalDialog.LeftButton; // Knöpfe nur bei Zeile 3 oder 4: Fensterfüllung
        Assert.Equal(Px(s, button.X + 30, button.Y + 6), Px(s, button.X + 2, button.Y + 1));

        Assert.True(box.Update(new InputState { MouseX = 100, MouseY = 100, LeftClick = true }));
        Assert.False(box.Visible);
    }

    [Fact]
    public void MessageBox_ThreeLines_HasButtons_AndLeftClickCloses()
    {
        var box = new MessageBox();
        box.Show("a", "b", "c");
        Assert.True(box.Update(new InputState { MouseX = 5, MouseY = 5, LeftClick = true }));
        Assert.False(box.Visible);
    }

    [Fact]
    public void MessageBox_MoreThanFourLines_KeepsOldFrame_AndStillCloses()
    {
        var box = new MessageBox();
        box.Show("a", "b", "c", "d", "e");
        Assert.True(box.Visible);
        Assert.True(box.Update(new InputState { MouseX = 5, MouseY = 5, LeftClick = true }));
        Assert.False(box.Visible);
    }

    [Fact]
    public void MessageBox_ShortMessage_IgnoresInput_AndClosesByItself()
    {
        var box = new MessageBox(() => "Hans");
        box.ShowShort("Der darf da", "nicht rein !");
        Assert.Equal(["Der darf da", "nicht rein !"], box.Lines);
        var frames = box.Short!.TotalFrames;
        var click = new InputState { MouseX = 160, MouseY = 170, LeftClick = true };
        click.Keys.Add(Key.Enter);
        for (var i = 0; i < frames - 1; i++)
        {
            Assert.True(box.Update(click));
            Assert.True(box.Visible);
        }
        Assert.True(box.Update(new InputState())); // letztes Bild
        Assert.False(box.Visible);
        Assert.False(box.Update(click)); // danach wieder normale Eingabe
    }

    // ---- Kurzmeldungen K2: Aufstellung, Transferliste, Freundschaftsspiele ----------------------------------------------

    [GameDataFact]
    public void Lineup_AutoDisabledMessage_IsShortMessageWith45Ticks()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.OpenTile("Aufstellung");
        var screen = Assert.IsType<LineupScreen>(d.Host.Current);
        var club = d.Host.Game!.Clubs[d.Host.Game.ActiveManager.ClubIndex];
        club.AutoLineupEnabled = true;
        var b = LineupScreen.SlotButton(LineupSlot.Defender(0, 0));
        d.RightClick(b.X + 5, b.Y + 5);
        Assert.Equal(["Auto-Aufstellung", "ist deaktiviert !"], screen.MessageLines);

        // [ORIGINAL] 0x0C0B8: „Auto-…“ steht 45 / 3 Takte (49 Bilder), ein Klick schließt nicht und löst nichts aus
        var other = LineupScreen.SlotButton(LineupSlot.Defender(0, 1));
        for (var i = 0; i < ShortMessage.FramesFor(45) - 1; i++)
            d.Click(other.X + 5, other.Y + 5);
        Assert.NotEmpty(screen.MessageLines);
        d.Frame();
        Assert.Empty(screen.MessageLines);
    }

    [GameDataFact]
    public void Lineup_WrongRole_IsShortMessage_AndIgnoresClicks()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.OpenTile("Aufstellung");
        var screen = Assert.IsType<LineupScreen>(d.Host.Current);
        var club = d.Host.Game!.Clubs[d.Host.Game.ActiveManager.ClubIndex];
        club.AutoLineupEnabled = false;
        var rows = TeamUi.Rows(club).ToList();
        var forward = rows.First(p => p.Role == PlayerRole.Forward && p.Available);
        var slot = LineupScreen.SlotButton(LineupSlot.Defender(0, 0));
        d.Click(slot.X + 5, slot.Y + 5);
        d.Click(60, LineupScreen.FirstRowY + rows.IndexOf(forward) * LineupScreen.RowPitch + 2);
        Assert.Equal(["Der darf da", "nicht rein !"], screen.MessageLines);
        d.Click(10, 10); // Kurzmeldung nimmt keine Eingabe an
        Assert.Equal(["Der darf da", "nicht rein !"], screen.MessageLines);
        d.Frames(100);
        Assert.Empty(screen.MessageLines);
    }

    [GameDataFact]
    public void TransferScreen_RejectedAndMissingMoney_AreShortMessages_AcceptedIsWindow()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.OpenTile("Transfer");
        var screen = Assert.IsType<TransferScreen>(d.Host.Current);
        var game = d.Host.Game!;
        var manager = game.ActiveManager;
        var market = game.TransferMarket;
        var index = market.Listings.ToList().FindIndex(l => l.Seller is null && !l.IsFreeAgent);
        var listing = market.Listings[index];
        manager.Money = 50_000_000;
        Assert.Equal(TransferResult.Rejected, market.Buy(game, manager, listing, listing.Price / 2, false));

        var buy = TransferScreen.EntryBuy(index);
        d.Click(buy.X + 3, buy.Y + 2); // gesperrt: „Angebot wurde / bereits abgelehnt.“ (Datei 0x47B11 → 0x0C0B8)
        Assert.Equal(TransferMarket.Message(TransferResult.AlreadyRejected), screen.Message.Lines);
        Assert.NotNull(screen.Message.Short);
        d.Click(10, 10);
        Assert.True(screen.Message.Visible); // Klick schließt eine Kurzmeldung nicht
        d.Frames(100);
        Assert.False(screen.Message.Visible);
    }
    /// <summary>MU-01 (Review #469): Bei einer Meldung ist der linke Knopf immer Zeile 5 (Datei 0x4C4E2), nur die Frage nimmt Zeile 4.</summary>
    [Fact]
    public void MessageWithFourthLine_LeftButtonIsLineFive()
    {
        var message = new OriginalDialog(OriginalDialogKind.Message, "Hans", "Die Firma", "Acme", "wird den Vertrag",
            "nicht verlängern.", MessageBox.ButtonText);
        Assert.Equal(MessageBox.ButtonText, message.LeftText);
        var question = new OriginalDialog(OriginalDialogKind.Question, "Hans", "Möchten Sie", "den Vertrag", "verlängern ?",
            "Ja", "Nein");
        Assert.Equal("Ja", question.LeftText);
    }

    /// <summary>MU-02 (Review #469): jede Kurzmeldung der Transferliste (Aufrufe 0x46A4D ff., 0x42CE3, Texte in DGROUP).</summary>
    [Theory]
    [InlineData(TransferResult.NotEnoughMoney)]
    [InlineData(TransferResult.TooManyOwnListings)]
    [InlineData(TransferResult.MarketFull)]
    [InlineData(TransferResult.Rejected)]
    [InlineData(TransferResult.AlreadyRejected)]
    [InlineData(TransferResult.Impossible)]
    public void TransferScreen_ShortMessages(TransferResult result) => Assert.True(TransferScreen.IsShortMessage(result));

    /// <summary>MU-02 (Review #469): Die Sperre in der Aufstellung ist eine Kurzmeldung (0x0C0B8). Text seit E2 (Spur kader-transfer
    /// Teil 4): Listenklick → „Spieler ist“ / „Gesperrt.“ (Datei 0x40E01); „noch gesperrt !“ gehört zum Klick auf den Platz (0x40B55).</summary>
    [GameDataFact]
    public void Lineup_Suspended_IsShortMessage()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.OpenTile("Aufstellung");
        var screen = Assert.IsType<LineupScreen>(d.Host.Current);
        var club = d.Host.Game!.Clubs[d.Host.Game.ActiveManager.ClubIndex];
        club.AutoLineupEnabled = false;
        var rows = TeamUi.Rows(club).ToList();
        var forward = rows.First(p => p.Role == PlayerRole.Forward && p.Available && !club.Lineup.Contains(p));
        forward.SuspendedGames = 2;
        var slot = LineupScreen.SlotButton(LineupSlot.Forward(0, 0));
        d.Click(slot.X + 5, slot.Y + 5);
        d.Click(60, LineupScreen.FirstRowY + rows.IndexOf(forward) * LineupScreen.RowPitch + 2);
        Assert.Equal(["Spieler ist", "Gesperrt."], screen.MessageLines);
        Assert.True(screen.MessageIsShort);
    }
}
