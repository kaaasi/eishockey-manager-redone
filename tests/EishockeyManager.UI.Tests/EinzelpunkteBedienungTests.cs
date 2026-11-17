using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.UI.Screens.Team;

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
}
