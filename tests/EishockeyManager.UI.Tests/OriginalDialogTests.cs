using EishockeyManager.Tests.Support;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur ui-meldungen, Teil 1 (M-K1, M-K2, Inventur Nr. 9): Meldungs- und Fragefenster em.exe Datei 0x4C246 und
/// Kurzmeldung Datei 0x0C0B8 (<c>docs/analyse/re-meldungen.md</c> §B, §C). Pixelwerte am Video 3:46 vermessen
/// (<c>oberflaeche_0346</c>, Frage der DEB-Pokal-Auslosung); die Lage ist dort für alle Elemente auf 0 | 0 genau.
/// </summary>
public class OriginalDialogTests
{
    private static OriginalDialog Question() =>
        new(OriginalDialogKind.Question, "", "Sind Ihre Nerven stark genug,", "um die DEB-Pokal-Auslosung",
            "live zu verfolgen ?", "Aber klar", "Kein Gedanke");

    private static InputState Click(int x, int y) => new() { MouseX = x, MouseY = y, LeftClick = true };

    private static InputState Click(Rect r) => Click(r.X + 5, r.Y + 5);

    // ---- Eingabe (ohne Originaldaten) -----------------------------------------------------------------------------

    [Fact]
    public void Question_LeftButtonIsOne_RightButtonIsZero()
    {
        var yes = Question();
        yes.Update(Click(OriginalDialog.LeftButton));
        Assert.Equal(1, yes.Result);

        var no = Question();
        no.Update(Click(OriginalDialog.RightButton));
        Assert.Equal(0, no.Result);
    }

    /// <summary>[ORIGINAL] 0x4C246 wertet nur Mausereignis 1 aus: weder Tastatur noch rechte Taste noch ein Klick daneben schließen eine Frage.</summary>
    [Fact]
    public void Question_OnlyLeftClickOnAButtonCloses()
    {
        var d = Question();
        d.Update(new InputState { MouseX = 100, MouseY = 170, RightClick = true });
        var escape = new InputState();
        escape.Keys.Add(Key.Escape);
        d.Update(escape);
        var enter = new InputState();
        enter.Keys.Add(Key.Enter);
        d.Update(enter);
        d.Update(Click(160, 125)); // Linksklick auf den Text, nicht auf einen Knopf
        Assert.False(d.Closed);
        Assert.Null(d.Result);
    }

    [Fact]
    public void Message_AnyLeftClickCloses_ButNotRightClickOrKeys()
    {
        var d = new OriginalDialog(OriginalDialogKind.Message, "Hans", "Erste", "Zweite", "Dritte", "", "Na gut.");
        d.Update(new InputState { MouseX = 160, MouseY = 125, RightClick = true });
        var key = new InputState();
        key.Keys.Add(Key.Enter);
        d.Update(key);
        Assert.False(d.Closed);

        d.Update(Click(5, 5));
        Assert.Equal(0, d.Result);
    }

    [Fact]
    public void Message_ClickOnLeftButtonReturnsOne()
    {
        var d = new OriginalDialog(OriginalDialogKind.Message, "Hans", "Erste", "Zweite", "Dritte", "", "Na gut.");
        d.Update(Click(OriginalDialog.LeftButton));
        Assert.Equal(1, d.Result);
    }

    // ---- Aufbau (ohne Originaldaten) ------------------------------------------------------------------------------

    /// <summary>[ORIGINAL] Knöpfe nur, wenn Zeile 3 oder 4 gefüllt ist; links Zeile 4 (fehlt sie: Zeile 5), rechts Zeile 5.</summary>
    [Fact]
    public void Buttons_OnlyWithLine3Or4_AndTexts()
    {
        var q = Question();
        Assert.True(q.HasButtons);
        Assert.Equal("Aber klar", q.LeftText);
        Assert.Equal("Kein Gedanke", q.RightText);

        var withThree = new OriginalDialog(OriginalDialogKind.Message, "", "a", "b", "c", "", "Na gut.");
        Assert.True(withThree.HasButtons);
        Assert.Equal("Na gut.", withThree.LeftText);
        Assert.Equal("Na gut.", withThree.RightText);

        var twoLines = new OriginalDialog(OriginalDialogKind.Message, "", "a", "b", "", "", "Na gut.");
        Assert.False(twoLines.HasButtons);
    }

    [Fact]
    public void Title_DefaultsToAnAlle()
    {
        Assert.Equal("An Alle:", Question().Title);
        Assert.Equal("Hans", new OriginalDialog(OriginalDialogKind.Message, "Hans", "a").Title);
    }

    /// <summary>[ORIGINAL] Grundlinien: 141 + 8 · i; ohne dritte Zeile 145, 153; Meldung mit vierter Zeile 139, 146, 153, 160 (ohne dritte 4 tiefer).</summary>
    [Fact]
    public void Baselines_FollowTheOriginalTable()
    {
        Assert.Equal([141, 149, 157], Question().Baselines);
        Assert.Equal([145, 153], new OriginalDialog(OriginalDialogKind.Question, "", "a", "b", "", "Ja", "Nein").Baselines);
        var four = new OriginalDialog(OriginalDialogKind.Message, "", "a", "b", "c", "d", "Na gut.");
        Assert.Equal([139, 146, 153, 160], four.Baselines);
        Assert.Equal(["a", "b", "c", "d"], four.TextLines);
        var noThird = new OriginalDialog(OriginalDialogKind.Message, "", "a", "b", "", "d", "Na gut.");
        Assert.Equal([143, 150, 157], noThird.Baselines);
    }

    // ---- Kurzmeldung (ohne Originaldaten) -------------------------------------------------------------------------

    /// <summary>[ORIGINAL] 0x0C0B8: Modus 75, bei Zeile 1 mit „Auto-“ 75 − 30 = 45; Pause Modus / 3 Takte zu 18,2 Hz.</summary>
    [Theory]
    [InlineData("Wollen Sie die WM wirklich", 75, 82)]
    [InlineData("Auto-Aufstellung", 45, 49)]
    public void ShortMessage_DurationFollowsMode(string line1, int mode, int frames)
    {
        var m = new ShortMessage("Hans", line1);
        Assert.Equal(mode, m.Mode);
        Assert.Equal(frames, m.TotalFrames);
    }

    [Fact]
    public void ShortMessage_ClosesByItself_AndIgnoresInput()
    {
        var m = new ShortMessage("Hans", "Wollen Sie die WM wirklich", "mit SO wenigen Spielern bestreiten ?");
        var click = Click(160, 170);
        click.Keys.Add(Key.Enter);
        for (var i = 0; i < m.TotalFrames; i++)
        {
            Assert.False(m.Closed);
            Assert.True(m.Update(click)); // verbraucht die Eingabe, wirkt aber nicht
        }
        Assert.True(m.Closed);
        Assert.False(m.Update(new InputState()));
    }

    [Fact]
    public void ShortMessage_MissingSecondLine_ShowsEinlegen()
    {
        Assert.Equal("einlegen.", new ShortMessage("Hans", "Bitte Diskette").Line2);
    }

    // ---- Pixel (Originaldaten) ------------------------------------------------------------------------------------

    private static Surface Render(OriginalDialog dialog)
    {
        var s = new Surface();
        s.Clear(30);
        dialog.Draw(s, new Assets(TestGameData.Load()));
        return s;
    }

    private static byte Px(Surface s, int x, int y) => s.Pixels[y * Surface.Width + x];

    /// <summary>
    /// Regressionstest (Befund A270/A333): Vorher lagen die „?“-Felder bei x 84 / 213 und y 110 mit Lichtkante; das Video 3:46 zeigt
    /// Fläche 14 ab (83 | 112) bzw. (212 | 112), 22 × 22, Umriss 12 um ein Pixel eingerückt, das Zeichen auf Grundlinie 129.
    /// </summary>
    [GameDataFact]
    public void Question_SymbolFields_AtOriginalPosition()
    {
        var s = Render(Question());
        foreach (var x0 in new[] { 83, 212 })
        {
            Assert.Equal(14, Px(s, x0, 112));
            Assert.Equal(14, Px(s, x0 + 21, 133));
            Assert.Equal(12, Px(s, x0 + 1, 113));
            Assert.Equal(12, Px(s, x0 + 20, 132));
            Assert.NotEqual(14, Px(s, x0 - 1, 112)); // links daneben kein Feld
            Assert.NotEqual(14, Px(s, x0, 111));     // darüber kein Feld (Lichtkante und y 110 des alten Aufbaus)
            Assert.NotEqual(15, Px(s, x0 + 2, 114)); // keine Lichtkante

            // „?“: schwarze Pixel im Feld von Zeile 115 bis zur Grundlinie 129
            var rows = Enumerable.Range(114, 18).Where(y => Enumerable.Range(x0 + 2, 19).Any(x => Px(s, x, y) == 0)).ToList();
            Assert.Equal(115, rows.Min());
            Assert.Equal(129, rows.Max());
        }
    }

    [GameDataFact]
    public void Message_SymbolFields_UseColors16And17()
    {
        var s = Render(new OriginalDialog(OriginalDialogKind.Message, "Hans", "a", "b", "c", "", "Na gut."));
        Assert.Equal(17, Px(s, 83, 112));
        Assert.Equal(16, Px(s, 84, 113));
    }

    [GameDataFact]
    public void Dialog_LinesAndButtons_AtOriginalPosition()
    {
        var s = Render(Question());
        // Linie oben (109 | 131)–(207 | 131) Farbe 7
        Assert.All(Enumerable.Range(109, 99), x => Assert.Equal(7, Px(s, x, 131)));
        Assert.NotEqual(7, Px(s, 108, 131));
        Assert.NotEqual(7, Px(s, 208, 131));
        // Linie unten (80 | 163)–(237 | 163) Farbe 0
        Assert.All(Enumerable.Range(80, 158), x => Assert.Equal(0, Px(s, x, 163)));
        Assert.NotEqual(0, Px(s, 79, 163));
        Assert.NotEqual(0, Px(s, 238, 163));
        // Fenster ab (75 | 107), 168 × 76
        Assert.Equal(30, Px(s, 74, 107));
        Assert.NotEqual(30, Px(s, 75, 107));
        Assert.NotEqual(30, Px(s, 242, 182));
        Assert.Equal(30, Px(s, 243, 182));
    }

    [GameDataFact]
    public void MessageWithoutButtons_DrawsNoButtonGraphics()
    {
        var with = Render(Question());
        var without = Render(new OriginalDialog(OriginalDialogKind.Message, "Hans", "a", "b", "", "", "Na gut."));
        var button = OriginalDialog.LeftButton;
        var same = true;
        for (var y = button.Y; y < button.Bottom && same; y++)
            for (var x = button.X + 2; x < button.Right - 2 && same; x++)
                same = Px(with, x, y) == Px(without, x, y);
        Assert.False(same, "Mit Knöpfen muss der Knopfbereich anders aussehen als ohne.");
        // ohne Knöpfe: Fläche des Knopfbereichs ist Fensterfüllung (kein 57 × 12 großes Bild)
        Assert.Equal(Px(without, button.X + 30, button.Y + 6), Px(without, button.X + 2, button.Y + 1));
    }
}
