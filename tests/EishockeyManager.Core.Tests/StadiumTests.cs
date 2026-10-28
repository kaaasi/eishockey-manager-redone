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

    [Theory]
    [InlineData(StadiumFeature.Kuehlanlage, 640_000)]
    [InlineData(StadiumFeature.Verkehrsanbindung, 1_200_000)]
    [InlineData(StadiumFeature.Gastronomie, 580_000)]
    [InlineData(StadiumFeature.Kino, 520_000)]
    [InlineData(StadiumFeature.Bowlingbahn, 450_000)]
    public void FirstStep_CostsAsInVideo(StadiumFeature feature, long cost)
    {
        var stadium = new Stadium();
        Assert.Equal(cost, stadium.Offer(feature, stadium.Level(feature) + 1)!.Value.Cost);
    }

    [Fact]
    public void Offer_SkippingLevelsAddsUpSteps()
    {
        var stadium = new Stadium();
        Assert.Equal(520_000 + 2 * 520_000 + 3 * 520_000, stadium.Offer(StadiumFeature.Kino, 3)!.Value.Cost);
        Assert.Null(stadium.Offer(StadiumFeature.Kino, 4));
        Assert.Null(stadium.Offer(StadiumFeature.Kino, 0));
    }

    [Fact]
    public void Construction_PaysUpFrontAndCompletesAfterBuildTime()
    {
        var game = TestGames.Create();
        var me = game.ActiveManager;
        var stadium = game.Finance(me).Stadium;

        // Bundesliga-Startstadion hat bereits eine Imbißbude (Spur finanzen-2); Ausbau auf die nächste Stufe.
        var target = stadium.Level(StadiumFeature.Gastronomie) + 1;
        var cost = stadium.Offer(StadiumFeature.Gastronomie, target)!.Value.Cost;
        Assert.True(game.StartConstruction(me, StadiumProject.Feature, StadiumFeature.Gastronomie, target));
        Assert.Equal(GameState.StartMoney - cost, me.Money);
        Assert.Equal(-cost, game.Finance(me).Book.Current(FinanceCategory.Bau));
        Assert.False(game.StartConstruction(me, StadiumProject.Sitzplaetze, default, 1)); // nur eine Baustelle

        for (var i = 0; i < 20 && stadium.Construction is not null; i++)
            TestSeason.PlayMatchDay(game);
        Assert.Null(stadium.Construction);
        Assert.Equal(target, stadium.Level(StadiumFeature.Gastronomie));

        var seats = stadium.Seats;
        Assert.True(game.StartConstruction(me, StadiumProject.Sitzplaetze, default, 2));
        for (var i = 0; i < 20 && stadium.Construction is not null; i++)
            TestSeason.PlayMatchDay(game);
        Assert.Equal(seats + 2 * Stadium.SeatStep, stadium.Seats);
    }
}
