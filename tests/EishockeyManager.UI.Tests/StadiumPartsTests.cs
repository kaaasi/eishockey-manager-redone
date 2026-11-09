using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens.ManagerArea;
using EishockeyManager.UI.Screens.Team;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>Stadionbild nach der Teile-Tabelle des Originals (A323/A325) und Bauanzeige (A326); Sitzung 3/7 der Aufnahme.</summary>
public class StadiumPartsTests
{
    private static void ClearAll(Stadium stadium)
    {
        foreach (var f in Enum.GetValues<StadiumFeature>())
            stadium.SetLevel(f, 0);
    }

    [GameDataFact]
    public void EveryPartIsDrawnFromItsTableEntryWithMask0xC0()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var stadium = d.Host.Game!.Finance(d.Host.Game.ActiveManager).Stadium;
        d.OpenTile("Stadion");
        var parts = d.Host.Assets.Image(StadiumScreen.Parts);
        var back = d.Host.Assets.Image(StadiumScreen.Background);

        foreach (var (feature, entries) in StadiumScreen.PartTable)
            for (var level = 1; level <= entries.Length; level++)
            {
                ClearAll(stadium);
                stadium.SetLevel(feature, level);
                d.Move(319, 239);
                var e = entries[level - 1];
                var differing = 0;
                for (var y = 0; y < e.Source.H; y++)
                    for (var x = 0; x < e.Source.W; x++)
                    {
                        var src = parts.Pixels[(e.Source.Y + y) * parts.Width + e.Source.X + x];
                        var dx = e.DestX + x;
                        var dy = e.DestY + y;
                        if (dx >= Surface.Width || dy >= StadiumScreen.ImageHeight)
                            continue;
                        var expected = src == StadiumScreen.PartMask ? back.Pixels[dy * back.Width + dx] : src;
                        if (d.Surface.Pixels[dy * Surface.Width + dx] != expected)
                            differing++;
                    }
                Assert.True(differing == 0, $"{feature} Stufe {level}: {differing} abweichende Pixel");
            }
    }

    [GameDataFact]
    public void HallCoversRestaurantInDrawOrder()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var stadium = d.Host.Game!.Finance(d.Host.Game.ActiveManager).Stadium;
        d.OpenTile("Stadion");
        ClearAll(stadium);
        stadium.SetLevel(StadiumFeature.Gastronomie, 3);
        stadium.SetLevel(StadiumFeature.Ueberdachung, 2);
        d.Move(319, 239);
        var parts = d.Host.Assets.Image(StadiumScreen.Parts);
        var hall = StadiumScreen.PartTable[StadiumFeature.Ueberdachung][1];
        var rest = StadiumScreen.PartTable[StadiumFeature.Gastronomie][2];

        // Überlappung: Wo beide Teile deckend sind und verschiedene Farben haben, gewinnt die Halle (Folge 6 vor 0).
        var checkedPixels = 0;
        for (var y = 91; y < 113; y++)
            for (var x = 80; x < 81 + 5; x++)
            {
                var h = parts.Pixels[(hall.Source.Y + y - hall.DestY) * parts.Width + hall.Source.X + x - hall.DestX];
                var r = parts.Pixels[(rest.Source.Y + y - rest.DestY) * parts.Width + rest.Source.X + x - rest.DestX];
                if (h == StadiumScreen.PartMask || r == StadiumScreen.PartMask || h == r)
                    continue;
                checkedPixels++;
                Assert.Equal(h, d.Surface.Pixels[y * Surface.Width + x]);
            }
        Assert.True(checkedPixels > 0, "keine unterscheidbaren Überlappungspixel");
        Assert.Equal(new[] { StadiumFeature.Verkehrsanbindung, StadiumFeature.Gastronomie, StadiumFeature.Bowlingbahn,
            StadiumFeature.Ueberdachung, StadiumFeature.Kino }, StadiumScreen.PartOrder);
    }

    [GameDataFact]
    public void ProjectUnderConstructionShowsOnlyHintAndQuestionUsesOriginalDialog()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.OpenTile("Stadion");
        var screen = Assert.IsType<StadiumScreen>(d.Host.Current);
        var game = d.Host.Game!;
        var me = game.ActiveManager;
        me.Money = 20_000_000;

        var row = StadiumScreen.FeatureRow((int)StadiumFeature.Kuehlanlage);
        d.Click(row.X + 5, row.Y + 3);
        d.Click(StadiumScreen.LevelBox.X + 5, StadiumScreen.LevelBox.Y + 3);
        Assert.True(screen.AsksConfirmation);
        var dialog = Assert.IsType<OriginalDialog>(screen.Dialog);
        Assert.Equal(OriginalDialogKind.Question, dialog.Kind);
        Assert.Equal(me.Name, dialog.Title);
        Assert.Equal("Möchten Sie Ihr Stadion ausbauen ?", dialog.Line1);
        // R14UR-03: „Rabatt:“ + Zahl Breite 2 + „#“ (Prozentzeichen der Schrift), „Bauzeit: ca. “ + Zahl Breite 2 + „ Wochen“
        Assert.Equal("Rabatt:" + EishockeyManager.UI.Screens.Statistics.ClubStatisticsScreen.Number(screen.Quote!.Discount, 2, false) + "#", dialog.Line2);
        Assert.Equal("Bauzeit: ca. " + EishockeyManager.UI.Screens.Statistics.ClubStatisticsScreen.Number(screen.Quote.Weeks, 2, false) + " Wochen", dialog.Line3);
        Assert.Equal(("Ja.", "Nein."), (dialog.LeftText, dialog.RightText));

        // Klick daneben und rechte Maustaste lassen die Frage offen (nur Knöpfe antworten)
        d.Click(5, 5);
        Assert.True(screen.AsksConfirmation);
        // R14UR-09: die rechte Maustaste schließt die Frage nicht – weder daneben noch auf „Ja.“ oder „Nein.“
        d.RightClick(5, 5);
        d.RightClick(StadiumScreen.YesButton.X + 5, StadiumScreen.YesButton.Y + 3);
        d.RightClick(StadiumScreen.NoButton.X + 5, StadiumScreen.NoButton.Y + 3);
        Assert.True(screen.AsksConfirmation);
        d.Click(StadiumScreen.YesButton.X + 5, StadiumScreen.YesButton.Y + 3);
        Assert.False(screen.AsksConfirmation);

        // Bereich im Bau überfahren: nur „… ausbauen“, keine Zeile „(Noch n Wochen)“ (y 221)
        d.Move(row.X + 5, row.Y + 3);
        var px = d.Surface.Pixels;
        for (var y = 219; y < 228; y++)
            for (var x = 213; x < 316; x++)
                Assert.Equal(px[230 * Surface.Width + 200], px[y * Surface.Width + x]);
    }

    /// <summary>
    /// R14UR-03 / R14UR-08: Ausbau-Frage gegen DOSBox `file43_5` (Rabatt 6, 8 Wochen): je Pixelzeile der beiden Textbänder
    /// Anzahl der Schriftpixel und äußerste x-Werte (Mitte 157, Zahlen mit unsichtbarer Auffüllung), Knopfkanten bei x 95 / 165.
    /// </summary>
    [GameDataFact]
    public void ConstructionQuestion_TextRowsAndButtons_LikeDosbox()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var dialog = new OriginalDialog(OriginalDialogKind.Question, "Kjellberg", "Möchten Sie Ihr Stadion ausbauen ?",
            StadiumScreen.ConstructionDiscountLine(6), StadiumScreen.ConstructionWeeksLine(8), "Ja.", "Nein.");
        d.Surface.Fill(new Rect(0, 0, Surface.Width, Surface.Height), TeamUi.DialogFill);
        dialog.Draw(d.Surface, d.Host.Assets);
        var px = d.Surface.Pixels;

        (int Min, int Max, int Count) Row(int y)
        {
            var xs = Enumerable.Range(82, 154).Where(x => px[y * Surface.Width + x] != TeamUi.DialogFill).ToList();
            return (xs.Min(), xs.Max(), xs.Count);
        }
        // Zeilen 145–150 „Rabatt: 6#“ (Pixelzeilen y 145 … 150), 153–158 „Bauzeit: ca.  8 Wochen“
        Assert.Equal([(134, 178, 11), (134, 179, 26), (134, 178, 28), (134, 178, 28), (134, 179, 29), (135, 179, 19)],
            Enumerable.Range(145, 6).Select(Row));
        Assert.Equal([(112, 187, 10), (112, 199, 42), (112, 200, 55), (112, 201, 57), (112, 201, 55), (113, 201, 40)],
            Enumerable.Range(153, 6).Select(Row));

        // Knöpfe: Kante links bei x 95 (Zeile 167), Oberkante Zeile 166 ab x 96; rechter Knopf bei x 165 / 166
        byte At(int x, int y) => px[y * Surface.Width + x];
        Assert.Equal(95, OriginalDialog.LeftButton.X);
        Assert.Equal(165, OriginalDialog.RightButton.X);
        foreach (var bx in new[] { 95, 165 })
        {
            Assert.Equal(TeamUi.DialogFill, At(bx - 1, 167));
            Assert.NotEqual(TeamUi.DialogFill, At(bx, 167));
            Assert.Equal(TeamUi.DialogFill, At(bx, 166));
            Assert.NotEqual(TeamUi.DialogFill, At(bx + 1, 166));
        }
        // Beschriftung: „Ja.“ beginnt bei x 120, „Nein.“ bei x 185 (Zeile 170 / 169)
        Assert.Equal(TeamUi.DialogDark, At(120, 169));
        Assert.Equal(TeamUi.DialogDark, At(185, 169));
        Assert.NotEqual(TeamUi.DialogDark, At(119, 169));
        Assert.NotEqual(TeamUi.DialogDark, At(184, 169));
    }

    /// <summary>
    /// R14UR-06: Trefferflächen nach em.exe 0x3DB83–0x3DC4B – Stufe-1-Eintrag der Teile-Tabelle, Grenzen einschließlich
    /// (Ziel … Ziel + Größe), Folge Verkehr, Gastronomie, Bowling, Überdachung, Kino; der erste Treffer gewinnt.
    /// </summary>
    [Theory]
    [InlineData(StadiumFeature.Verkehrsanbindung, 244, 50, 67, 119)]
    [InlineData(StadiumFeature.Gastronomie, 25, 112, 51, 31)]
    [InlineData(StadiumFeature.Bowlingbahn, 201, 94, 69, 56)]
    [InlineData(StadiumFeature.Ueberdachung, 79, 57, 145, 61)]
    [InlineData(StadiumFeature.Kino, 29, 44, 53, 22)]
    public void Hotspot_EdgesAreInclusive_LikeOriginal(StadiumFeature feature, int x, int y, int w, int h)
    {
        var area = StadiumScreen.Hotspots.Single(hs => hs.Feature == feature).Area;
        Assert.Equal(new Rect(x, y, w, h), area);
        // Innenpunkte an allen vier Rändern (einschließlich Ziel + Größe), die nicht von einem früheren Bereich überdeckt sind
        var order = StadiumScreen.PartOrder.ToList();
        bool Earlier(int px, int py) => order.TakeWhile(f => f != feature)
            .Any(f => StadiumScreen.Hotspots.Single(hs => hs.Feature == f).Area.Contains(px, py));
        foreach (var (px, py) in new[] { (x, y + h / 2), (x + w - 1, y + h / 2), (x + w / 2, y), (x + w / 2, y + h - 1) })
            if (!Earlier(px, py))
                Assert.Equal(feature, StadiumScreen.HotspotAt(px, py));
        // knapp daneben: kein Treffer dieses Bereichs
        foreach (var (px, py) in new[] { (x - 1, y + h / 2), (x + w, y + h / 2), (x + w / 2, y - 1), (x + w / 2, y + h) })
            Assert.NotEqual(feature, StadiumScreen.HotspotAt(px, py));
    }

    [Fact]
    public void Hotspot_FirstHitWins_InOrderOfTheOriginal()
    {
        // Kino (29 … 82 | 44 … 65) überlappt die Überdachung (79 … 224 | 57 … 117) bei x 79–82, y 57–65: Überdachung kommt zuerst
        Assert.Equal(StadiumFeature.Ueberdachung, StadiumScreen.HotspotAt(80, 60));
        Assert.Equal(StadiumFeature.Kino, StadiumScreen.HotspotAt(78, 60));
        Assert.Equal(StadiumFeature.Kino, StadiumScreen.HotspotAt(80, 56));
    }
}
