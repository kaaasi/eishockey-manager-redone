using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens.ManagerArea;
using EishockeyManager.UI.Screens.Team;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>Spur einzelpunkte, Teil 1 (Runde 22): Bedienung und Anzeige der Spielerdaten.</summary>
public class EinzelpunkteBedienungTests
{
    private static Player Skater(int skill1, int skill2) => new()
    {
        Name = "TEST", Role = PlayerRole.Forward, Condition = 3, Technique = 9, HiddenSkill1 = skill1, HiddenSkill2 = skill2,
    };

    /// <summary>
    /// A371, em.exe Datei 0x4B09–0x4B23 (Unterzahl: (+0x19 &amp; 0xF) · 94 / 15) und 0x4B4A–0x4B52 (Überzahl: (+0x19 &gt;&gt; 4) · 94 / 15),
    /// auf 1 … 94 begrenzt. Vorher: Technik bzw. (Kondition + Technik) / 2. Erwartete Werte von Hand gerechnet.
    /// </summary>
    [Theory]
    [InlineData(0, 15, 1, 94)]
    [InlineData(7, 7, 43, 43)]
    [InlineData(15, 1, 94, 6)]
    [InlineData(3, 12, 18, 75)]
    public void InfoPopup_UnderOverBars_UseHiddenSkills(int skill1, int skill2, int under, int over)
    {
        var lengths = PlayerInfoPopup.BarPixelLengths(Skater(skill1, skill2));
        Assert.Equal(under, lengths[0]);
        Assert.Equal(over, lengths[1]);
    }

    /// <summary>
    /// A367, em.exe Datei 0x4651–0x469B (Tore: Byte +0x03 + i, Klammer Wort +0x22 + 2 i) und 0x4753/0x4793 (Spiele: +0x06 + i,
    /// +0x1C + 2 i): je Spalte Liga/DEB-Pokal/Europapokal „Zahl( n)“, 0 als „-“. Vorher: Pokalspalten immer „-( -)“, Klammer fest „( -)“.
    /// </summary>
    [Fact]
    public void DataWindow_GoalsAndGames_PerCompetitionWithClubCounters()
    {
        var p = new Player
        {
            Name = "TEST", Role = PlayerRole.Forward,
            Games = 30, CupGames = 3, EuropeGames = 2, Goals = 12, CupGoals = 2, EuropeGoals = 1,
            ClubGamesCarry = 40, ClubCupGamesCarry = 6, ClubGoalsCarry = 9,
        };
        // Liga: 25 Spiele (30 − 3 − 2), 9 Tore (12 − 2 − 1); beim Verein 25 + 40 und 9 + 9; Pokal 3 + 6 Spiele, 2 + 0 Tore.
        Assert.Equal("9( 18)", PlayerDataWindow.CompetitionCell(p, 0, goals: true));
        Assert.Equal("25( 65)", PlayerDataWindow.CompetitionCell(p, 0, goals: false));
        Assert.Equal("2( 2)", PlayerDataWindow.CompetitionCell(p, 1, goals: true));
        Assert.Equal("3( 9)", PlayerDataWindow.CompetitionCell(p, 1, goals: false));
        Assert.Equal("1( 1)", PlayerDataWindow.CompetitionCell(p, 2, goals: true));
        Assert.Equal("2( 2)", PlayerDataWindow.CompetitionCell(p, 2, goals: false));
        Assert.Equal("-( -)", PlayerDataWindow.CompetitionCell(new Player { Name = "N", Role = PlayerRole.Forward }, 1, goals: true));
    }

    /// <summary>
    /// A582, em.exe 0x2AC84: Im Vertragsfeld eines kündbaren Vertrags (Bit 0x80 der Option) steht bei x 275 „Kündbar.“; die Fläche zählt
    /// als frei, ein geblättertes Angebot ist annehmbar. Vorher: kein Hinweis, „Annehmen“ fehlte.
    /// </summary>
    [GameDataFact]
    public void CancellableContract_ShowsKuendbar_AndOfferIsAcceptable()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.OpenTile("Werbung");
        var screen = Assert.IsType<AdvertisingScreen>(d.Host.Current);
        var game = d.Host.Game!;
        var ads = game.Ads(game.ActiveManager);
        ads.Contracts.Add(new SponsorContract
        {
            Kind = AdSlotKind.Fernsehen, Sponsor = 1, Boards = [], MonthlyAmount = 9_000, MonthsLeft = 5, Cancellable = true,
        });

        var tv = AdvertisingScreen.TvPanel;
        d.Click(tv.X + tv.W / 2, tv.Y + tv.H / 2);
        Assert.Contains(new AdvertisingScreen.PanelText(275, 189, "Kündbar."), screen.PanelTexts());

        ads.Offers[AdSlotKind.Fernsehen][0] = new SponsorOffer(AdSlotKind.Fernsehen, 0, 12, 0, 4_000);
        var arrow = AdvertisingScreen.ArrowDown;
        d.Click(arrow.X + arrow.W / 2, arrow.Y + arrow.H / 2);
        Assert.NotNull(screen.CurrentOffer());
    }
}
