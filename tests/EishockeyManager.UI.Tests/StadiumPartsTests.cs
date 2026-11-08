using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens.ManagerArea;
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
        Assert.Equal($"Rabatt: {screen.Quote!.Discount}%", dialog.Line2);
        Assert.Equal($"Bauzeit: ca. {screen.Quote.Weeks} Wochen", dialog.Line3);
        Assert.Equal(("Ja.", "Nein."), (dialog.LeftText, dialog.RightText));

        // Klick daneben und rechte Maustaste lassen die Frage offen (nur Knöpfe antworten)
        d.Click(5, 5);
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
}
