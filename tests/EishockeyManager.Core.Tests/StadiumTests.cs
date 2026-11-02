using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>Stadionausbau: Stufen, Kosten aus dem Video, Baustelle.</summary>
public class StadiumTests
{
    [Fact]
    public void Levels_MatchTheTextsInEmExe()
    {
        Assert.Equal(["OHNE", "KLEIN", "MITTEL", "GROß"], Stadium.Features[(int)StadiumFeature.Kino].Levels);
        Assert.Equal(["OHNE", "VORHANDEN"], Stadium.Features[(int)StadiumFeature.Bowlingbahn].Levels);
    }

    /// <summary>
    /// Kosten je Stufe nach em.exe DGROUP 0xC0 (Spur geld, Teil 1). Vorher galt „Straße 1.200.000 DM“ aus dem Video und eine
    /// steigende Stufe n · erster Schritt; die Tabelle des Originals ist linear und nennt für die Verkehrsanbindung
    /// 1.300.000 DM (Abweichung zum Video offen, re-geld.md).
    /// </summary>
    [Theory]
    [InlineData(StadiumFeature.Kuehlanlage, 640_000)]
    [InlineData(StadiumFeature.Verkehrsanbindung, 1_300_000)]
    [InlineData(StadiumFeature.Gastronomie, 580_000)]
    [InlineData(StadiumFeature.Kino, 520_000)]
    [InlineData(StadiumFeature.Bowlingbahn, 450_000)]
    public void FirstStep_CostsAsInVideo(StadiumFeature feature, long cost)
    {
        var stadium = new Stadium();
        Assert.Equal(cost, stadium.Cost(StadiumProject.Feature, feature, stadium.Level(feature) + 1));
    }

    /// <summary>Mehrere Stufen auf einmal: (Ziel − jetzt) · K (Spur geld, Teil 1; vorher 1 + 2 + 3 Schritte).</summary>
    [Fact]
    public void Offer_SkippingLevelsAddsUpSteps()
    {
        var stadium = new Stadium();
        Assert.Equal(3 * 520_000, stadium.Cost(StadiumProject.Feature, StadiumFeature.Kino, 3));
        Assert.Null(stadium.Cost(StadiumProject.Feature, StadiumFeature.Kino, 4));
        Assert.Null(stadium.Cost(StadiumProject.Feature, StadiumFeature.Kino, 0));
    }

    [Fact]
    public void Construction_PaysUpFrontAndCompletesAfterBuildTime()
    {
        var game = TestGames.Create();
        var me = game.ActiveManager;
        var stadium = game.Finance(me).Stadium;
        // Seit Spur geld Teil 1 nur mit Konto ≥ Kosten (em.exe Datei 0x3E107); das Startkonto reicht nicht für jeden Ausbau.
        me.Money = 5_000_000;

        // Zustand „MITTEL“ eine Stufe höher. Spur geld Teil 1: Bauzeit 5–8 (± 2) Wochen statt Gastronomie (17–24 Wochen,
        // länger als die Hauptrunde ab Spielbeginn).
        var target = stadium.Level(StadiumFeature.Zustand) + 1;
        var quote = game.QuoteConstruction(me, StadiumProject.Feature, StadiumFeature.Zustand, target)!;
        Assert.True(game.AnswerConstruction(me, quote, accept: true));
        Assert.Equal(5_000_000 - quote.DiscountedCost, me.Money);
        Assert.Equal(-quote.DiscountedCost, game.Finance(me).Book.Current(FinanceCategory.Bau));
        Assert.False(game.StartConstruction(me, StadiumProject.Feature, StadiumFeature.Zustand, target)); // schon im Bau

        // Seit Spur geld Teil 1 parallel (vorher erst nach dem Ende der ersten Baustelle).
        var seats = stadium.Seats;
        Assert.True(game.StartConstruction(me, StadiumProject.Sitzplaetze, default, 2));
        for (var i = 0; i < 80 && stadium.Constructions.Any(); i++)
            TestSeason.PlayMatchDay(game);
        Assert.Empty(stadium.Constructions);
        Assert.Equal(target, stadium.Level(StadiumFeature.Zustand));
        Assert.Equal(seats + 2 * Stadium.PlaceStep, stadium.Seats);
    }
}
