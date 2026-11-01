using EishockeyManager.Tests.Support;
using EishockeyManager.Core;
using EishockeyManager.UI.Screens.ManagerArea;

namespace EishockeyManager.UI.Tests;

/// <summary>Jugendteams gegen em.exe Datei 0x3A24B (Spur finanzen-rest, Teil 1): Fahnenhöhe, Beschriftung, Pfeile, Balken.</summary>
public class YouthOriginalScreenTests
{
    [Theory]
    [InlineData(3, 6, 112, 107)] // UI-Video 8:35: Stangenspitze 109 + 3, Schrift y 103–107
    [InlineData(3, 12, 124, 119)] // Folge 4 0:28:25
    [InlineData(1, 1, 54, 65)] // oben in der Liga: Schrift unter der Fahne
    [InlineData(1, 8, 68, 63)] // 0x43–0x47: Schrift über der Fahne
    [InlineData(2, 7, 90, 101)]
    [InlineData(2, 9, 94, 89)] // 0x5B–0x5F
    public void Flag_RisesTwoPixelsPerPlace(int league, int rank, int flagY, int baseline)
    {
        var team = new YouthTeam { Kind = YouthTeamKind.Schueler, League = league, Rank = rank };
        Assert.Equal(flagY, YouthScreen.FlagY(team));
        Assert.Equal(baseline, YouthScreen.LabelBaseline(team));
    }

    [Theory]
    [InlineData(YouthTeamKind.Schueler, 10_000, 69)]
    [InlineData(YouthTeamKind.Jugend, 19_000, 65)] // w = 66
    [InlineData(YouthTeamKind.Jugend, 1_000, 2)] // w = 3
    [InlineData(YouthTeamKind.Junioren, 1_000, 0)] // w = 1: nicht gezeichnet
    public void BarFill_IsScaledToTheTeamCap(YouthTeamKind kind, long funding, int expected) =>
        Assert.Equal(expected, new YouthTeam { Kind = kind, Funding = funding }.BarFill);

    [GameDataFact]
    public void ClicksBesideTheBar_StepUpToTheCapAndDownToZero()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.OpenTile("Jugend");
        var youth = d.Host.Game!.Finance(d.Host.Game.ActiveManager).Youth;
        var bar = YouthScreen.Bar(1);

        d.Click(bar.X + 71, bar.Y + 4); // rechts neben dem Balken
        d.Click(bar.X + 71, bar.Y + 4);
        Assert.Equal(2_000, youth[1].Funding);
        d.Click(bar.X - 5, bar.Y + 4); // links daneben
        Assert.Equal(1_000, youth[1].Funding);
        d.Click(bar.X + 67, bar.Y + 4); // UI-Video 8:39
        Assert.Equal(19_000, youth[1].Funding);
        d.Click(bar.X + 75, bar.Y + 4);
        d.Click(bar.X + 75, bar.Y + 4);
        Assert.Equal(20_000, youth[1].Funding);
        Assert.IsType<YouthScreen>(d.Host.Current);
    }
}
