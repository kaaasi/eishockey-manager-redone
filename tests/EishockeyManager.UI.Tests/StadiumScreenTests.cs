using EishockeyManager.Tests.Support;
using EishockeyManager.Core;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.ManagerArea;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>Bildschirm Stadion mit den Originaldateien (ohne „gamedata“ übersprungen).</summary>
public class StadiumScreenTests
{
    private static void Click(ScreenDriver d, Rect r) => d.Click(r.X + r.W / 2, r.Y + r.H / 2);

    [GameDataFact]
    public void SelectLevelConfirmAndAdjustTicketPrice()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.OpenTile("Stadion");
        var screen = Assert.IsType<StadiumScreen>(d.Host.Current);
        var game = d.Host.Game!;
        var me = game.ActiveManager;
        var stadium = game.Finance(me).Stadium;

        // ▲ nimmt eine höhere Stufe nur an, wenn das Konto die Kosten deckt (em.exe Datei 0x3E107–0x3E137, Spur geld Teil 1).
        me.Money = 1_000_000;
        d.Click(50, 65);
        Click(d, StadiumScreen.LevelUp);
        Assert.Equal(1, screen.Target); // 2 · 520.000 DM > Konto
        me.Money = 5_000_000;

        // Kino über die Fläche oben links, dann zwei Stufen höher
        d.Click(50, 65);
        Assert.Equal(new StadiumScreen.Choice(StadiumProject.Feature, StadiumFeature.Kino), screen.Selected);
        Assert.Equal(1, screen.Target);
        Click(d, StadiumScreen.LevelUp);
        Click(d, StadiumScreen.LevelUp);
        Click(d, StadiumScreen.LevelUp);
        Assert.Equal(3, screen.Target);
        Click(d, StadiumScreen.LevelDown);
        Assert.Equal(2, screen.Target);

        // Zeile der Liste wählt ebenfalls
        Click(d, StadiumScreen.FeatureRow((int)StadiumFeature.Kuehlanlage));
        Assert.Equal(StadiumFeature.Kuehlanlage, screen.Selected?.Feature);
        Assert.Equal(640_000, screen.CurrentCost());

        Click(d, StadiumScreen.LevelBox);
        Assert.True(screen.AsksConfirmation);
        var quote = screen.Quote!;
        Assert.Equal(640_000, quote.Cost);
        Click(d, StadiumScreen.YesButton);
        Assert.True(stadium.IsUnderConstruction(StadiumProject.Feature, StadiumFeature.Kuehlanlage));
        // Rabatt 0–10 % (Spur geld, Teil 1, em.exe Datei 0x3E1FE ff.)
        Assert.Equal(5_000_000 - quote.DiscountedCost, me.Money);

        // Projekt im Bau: keine Auswahl; anderes Projekt parallel wählbar
        Click(d, StadiumScreen.FeatureRow((int)StadiumFeature.Kuehlanlage));
        Assert.Null(screen.Selected);
        Click(d, StadiumScreen.FeatureRow((int)StadiumFeature.Bowlingbahn));
        Assert.Equal(StadiumFeature.Bowlingbahn, screen.Selected?.Feature);

        // „Kein Gedanke“: Baufirma beleidigt, Kurzmeldung statt Auswahl
        Click(d, StadiumScreen.LevelBox);
        Assert.True(screen.AsksConfirmation);
        Click(d, StadiumScreen.NoButton);
        Assert.False(screen.AsksConfirmation);
        Assert.False(stadium.IsUnderConstruction(StadiumProject.Feature, StadiumFeature.Bowlingbahn));
        Click(d, StadiumScreen.FeatureRow((int)StadiumFeature.Bowlingbahn));
        Assert.Null(screen.Selected);
        // Kurzmeldung 0x3DE9F (0x0C0B8) ohne Knopf; sie schließt nach 75 Takten von selbst (Integration R12, UM-4)
        var notice = Assert.IsType<ShortMessage>(screen.Message.Short);
        Assert.Equal(("Die Baufirma", "ist noch beleidigt..."), (notice.Line1, notice.Line2));
        d.Frames(notice.TotalFrames);
        Assert.Null(screen.Message.Short);

        var price = stadium.TicketPrice;
        Click(d, StadiumScreen.TicketUp);
        Click(d, StadiumScreen.TicketUp);
        Click(d, StadiumScreen.TicketDown);
        Assert.Equal(price + 1, stadium.TicketPrice);

        d.Press(Key.Escape);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
    }
    /// <summary>
    /// Pixelprobe G7 (Spur geld Teil 3): rechter Block gegen DOSBox-Bild <c>file15_2_stadion</c> (FILE15.HOK, Eintritt 12 DM,
    /// Konto 650.000 DM; nur im privaten Referenz-Repo). Gemessene Spalten: Beschriftung ab x = 213, Doppelpunkt x = 267,
    /// Sitz-/Stehplatz- und Eintrittswerte ab x = 271, Konto-Betrag endet bei x = 310; Zeile „Eintritt“ beginnt in y = 190.
    /// </summary>
    [GameDataFact]
    public void RightBlockColumnsMatchDosBoxImage()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        game.ActiveManager.Money = 650_000;
        var stadium = game.Finance(game.ActiveManager).Stadium;
        stadium.Seats = 2_500;
        stadium.Standing = 3_500;
        stadium.TicketPrice = 12;
        d.OpenTile("Stadion");
        d.Move(319, 239);
        var px = d.Surface.Pixels;
        var bg = px[230 * Surface.Width + 200];

        List<(int From, int To)> Runs(int x0, int x1, int y0, int y1)
        {
            var runs = new List<(int, int)>();
            int? start = null;
            for (var x = x0; x < x1; x++)
            {
                var on = false;
                for (var y = y0; y < y1 && !on; y++)
                    on = px[y * Surface.Width + x] != bg;
                if (on && start is null) start = x;
                if (!on && start is { } s) { runs.Add((s, x - 1)); start = null; }
            }
            return runs;
        }

        int Top(int x0, int x1, int y0, int y1)
        {
            for (var y = y0; y < y1; y++)
                for (var x = x0; x < x1; x++)
                    if (px[y * Surface.Width + x] != bg) return y;
            return -1;
        }

        var seats = Runs(205, 320, 177, 185);
        Assert.Equal(213, seats[0].From);
        Assert.Equal(267, seats.First(r => r.From >= 260).From);
        Assert.Equal(271, seats.First(r => r.From >= 268).From);
        var ticket = Runs(212, 320, 187, 197); // ab 212: die Pfeile (kb\4) enden bei 211
        Assert.Equal(213, ticket[0].From);
        Assert.Equal(272, ticket.First(r => r.From >= 268).From);
        Assert.Equal(190, Top(212, 260, 186, 198));
        var account = Runs(205, 320, 228, 237);
        Assert.Equal(213, account[0].From);
        Assert.Equal(310, account[^1].To);
    }
}
