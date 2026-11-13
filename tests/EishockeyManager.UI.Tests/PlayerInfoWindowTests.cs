using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens.Team;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur spielerinfo (Runde 8): Spielerdaten-Fenster (em.exe 0x041AC) und Hinweiszeile der Spielerlisten (0x41683).
/// Alle Erwartungen stammen aus dem Disassembly (Fundstellen in <c>docs/reverse-engineering.md</c>, „Spur spielerinfo“),
/// die Bildmaße aus dem Referenzbild oberflaeche_1115 (Transferliste, „HANFT“).
/// </summary>
public class PlayerInfoWindowTests
{
    private static Player Skater(int age = 24, int talent = 3, PlayerRole role = PlayerRole.Forward, string name = "TEST") =>
        new() { Name = name, Role = role, Age = age, Talent = talent, Condition = 50, Technique = 50, Form = 50 };

    /// <summary>[ORIGINAL] 0x041AC 0x335–0x3D1: Spalte (3 − Altersstufe) · 17, Zeile Talent · 21; Stufe = (Alter − 18) / 5, höchstens 3.</summary>
    [Theory]
    [InlineData(18, 5, 3 * 17, 5 * 21)]
    [InlineData(22, 0, 3 * 17, 0)]
    [InlineData(23, 9, 2 * 17, 9 * 21)]
    [InlineData(28, 1, 1 * 17, 1 * 21)]
    [InlineData(33, 4, 0, 4 * 21)]
    [InlineData(45, 4, 0, 4 * 21)]
    public void Face_FollowsTalentAndAge(int age, int talent, int x, int y)
    {
        var face = PlayerInfoPopup.Face(Skater(age, talent));
        Assert.Equal(new Rect(x, y, 17, 21), face);
    }

    /// <summary>
    /// [ORIGINAL] 0x041AC 0xBD7–0xBFC: Der Zeiger ist eine senkrechte Linie von Oberkante + 4 bis + 12 (innen, 8 Zeilen)
    /// in der Farbe des Abschnitts, in dem er steht – also auch in den Rändern über und unter dem Farbband
    /// (Videobild oberflaeche_1115, Zeilen 177 und 184 der Überzahl-Leiste).
    /// </summary>
    [Fact]
    public void BarMarker_SpansWholeInnerHeightInSegmentColor()
    {
        var s = new Surface();
        var r = PlayerInfoPopup.Bar(1);
        PlayerInfoPopup.DrawBar(s, r, 50);
        var px = 50 * 94 / 99;
        var x = r.X + 1 + px;
        var color = PlayerInfoPopup.BarColors[px / 12];
        for (var y = r.Y + 2; y <= r.Y + 9; y++)
            Assert.Equal(color, s.Pixels[y * Surface.Width + x]);
    }

    /// <summary>
    /// [ORIGINAL] 0x041AC 0x985/0x9CA/0xA0D/0xA5B: linke Balken bei Fensterursprung + 7, rechte bei + 0x6C, Zeilenabstand 0x13
    /// (Videobild oberflaeche_1115: rechte Leiste 2 Pixel weiter links als bisher gezeichnet).
    /// </summary>
    [Fact]
    public void Bars_SitAtOriginalPositions()
    {
        Assert.Equal(89 + 7, PlayerInfoPopup.Bar(0).X);
        Assert.Equal(89 + 0x6C, PlayerInfoPopup.Bar(1).X);
        Assert.Equal(89 + 7, PlayerInfoPopup.Bar(2).X);
        Assert.Equal(89 + 0x6C, PlayerInfoPopup.Bar(3).X);
        Assert.Equal(PlayerInfoPopup.Bar(0).Y + 0x13, PlayerInfoPopup.Bar(2).Y);
    }

    // ---- Spielerdaten-Fenster 0x041AC: Felder ----

    /// <summary>[ORIGINAL] 0x041AC 0x763–0x77A: Sperren in Minuten = 5 · (5-Min.-Strafen) + 2 · (2-Min.-Strafen).</summary>
    [Fact]
    public void Data_PenaltyMinutes_AreFiveTimesFivePlusTwiceTwo()
    {
        var p = Skater();
        p.Penalties5 = 2;
        p.Penalties2 = 3;
        Assert.Equal(16, PlayerDataWindow.PenaltyMinutes(p));
        p.SuspendedGames = 4; // zählt nicht als Strafminuten
        var values = PlayerDataWindow.DataValues(p, 0, PlayerDataContext.Unknown);
        Assert.Equal("16", values[3]);
        p.Penalties5 = p.Penalties2 = 0;
        Assert.Equal("-", PlayerDataWindow.DataValues(p, 0, PlayerDataContext.Unknown)[3]);
    }

    /// <summary>[ORIGINAL] 0x041AC 0x551–0x570 und 0x45D–0x47B: Torhüter nur Spalte „Liga“, Zeile „GTore:“.</summary>
    [Fact]
    public void Data_Goalkeeper_ShowsOnlyLeagueColumnAndGoalsAgainst()
    {
        var g = new Player { Name = "G", Role = PlayerRole.Goalkeeper, Games = 7, Goals = 0, GoalsAgainst = 19 };
        Assert.Equal(1, PlayerDataWindow.CompetitionColumns(g));
        Assert.Equal("GTore:", PlayerDataWindow.GoalsLabel(g));
        Assert.Equal("19( -)", PlayerDataWindow.CompetitionCell(g, 0, goals: true));
        var f = Skater();
        f.Games = 7;
        f.Goals = 4;
        Assert.Equal(3, PlayerDataWindow.CompetitionColumns(f));
        Assert.Equal("Tore:", PlayerDataWindow.GoalsLabel(f));
        Assert.Equal("4( -)", PlayerDataWindow.CompetitionCell(f, 0, goals: true));
        Assert.Equal("-( -)", PlayerDataWindow.CompetitionCell(f, 1, goals: true));
    }

    /// <summary>[ORIGINAL] 0x041AC 0x7D6–0x82B: fremder Verein blendet Gehalt aus, Marktwert ab „5 − Level“ &lt; 3 (R8-D01, #253).</summary>
    [Theory]
    [InlineData(true, 2, "30.000 DM", "1.200 DM")]
    [InlineData(false, 2, "-", "-")]
    [InlineData(false, 3, "30.000 DM", "-")]
    public void Data_MarketValueAndSalary_HiddenForForeignClub(bool same, int levelValue, string market, string salary)
    {
        var p = Skater();
        p.MonthlySalary = 1200;
        var values = PlayerDataWindow.DataValues(p, 150_000, new PlayerDataContext(30_000, same, levelValue));
        Assert.Equal("150.000 DM", values[0]);
        Assert.Equal(market, values[1]);
        Assert.Equal(salary, values[4]);
    }

    /// <summary>[ORIGINAL] 0x041AC 0x8F6–0x915 und 0x8C2–0x8F3: „GELIEHEN“ ersetzt den Vertrag, sonst „1 Jahr“ / „n Jahre“.</summary>
    [Fact]
    public void Data_Contract_LoanOverridesYears()
    {
        var p = Skater();
        p.ContractYears = 1;
        Assert.Equal("1 Jahr", PlayerDataWindow.DataValues(p, 0, PlayerDataContext.Unknown)[5]);
        p.ContractYears = 3;
        Assert.Equal("3 Jahre", PlayerDataWindow.DataValues(p, 0, PlayerDataContext.Unknown)[5]);
        p.OnLoan = true;
        Assert.Equal("GELIEHEN", PlayerDataWindow.DataValues(p, 0, PlayerDataContext.Unknown)[5]);
    }

    /// <summary>[ORIGINAL] 0x041AC 0xBBA–0xBCA: Balkenlänge 1–94 Pixel.</summary>
    [Theory]
    [InlineData(-5, 1)]
    [InlineData(0, 1)]
    [InlineData(99, 94)]
    [InlineData(500, 94)]
    public void Bar_PixelsAreClampedToOneAndNinetyFour(int value, int expected) =>
        Assert.Equal(expected, PlayerDataWindow.BarPixels(value));

    // ---- Hinweiszeile 0x41683 ----

    private static readonly PlayerHintInfo InLine2 = new(true, 2);

    /// <summary>
    /// [ORIGINAL] 0x41683 0x8D–0xB6: Spalten aus der Tabelle DGROUP 0x39F, ab Rand 7 über die in der Maske gesetzten Bits.
    /// Aufstellung (Maske 0xF0C7): 11, 18, 57, 14, 36, 13, 27, 40, 16 Pixel.
    /// </summary>
    [Theory]
    [InlineData(-1, -1)]
    [InlineData(0, 0)]
    [InlineData(17, 0)]
    [InlineData(18, 1)]
    [InlineData(35, 1)]
    [InlineData(36, 2)]
    [InlineData(92, 2)]
    [InlineData(93, 6)]
    [InlineData(106, 6)]
    [InlineData(107, 7)]
    [InlineData(142, 7)]
    [InlineData(143, 12)]
    [InlineData(155, 12)]
    [InlineData(156, 13)]
    [InlineData(182, 13)]
    [InlineData(183, 14)]
    [InlineData(222, 14)]
    [InlineData(223, 15)]
    [InlineData(233, 15)]
    [InlineData(234, -1)]
    public void Hint_Column_FollowsWidthTableAndMask(int x, int expected) =>
        Assert.Equal(expected, PlayerHintLine.Column(PlayerListLayout.Lineup(10), x));

    /// <summary>[ORIGINAL] 0x41683 0x2D–0x41, 0x9E–0xAC: Zeile = (y − 29) / 6 zur Null hin; nur 0 bis letzte Zeile.</summary>
    [Theory]
    [InlineData(23, -1)]
    [InlineData(24, 0)]
    [InlineData(29, 0)]
    [InlineData(34, 0)]
    [InlineData(35, 1)]
    [InlineData(29 + 6 * 9 + 5, 9)]
    [InlineData(29 + 6 * 10, -1)]
    public void Hint_Row_TruncatesTowardZero(int y, int expected) =>
        Assert.Equal(expected, PlayerHintLine.Row(PlayerListLayout.Lineup(10), y));

    [Fact]
    public void Hint_TransferList_UsesItsOwnMaskAndEdge()
    {
        var l = PlayerListLayout.TransferList(8);
        Assert.Equal(1, PlayerHintLine.Column(l, 24));
        Assert.Equal(2, PlayerHintLine.Column(l, 25));
        Assert.Equal(6, PlayerHintLine.Column(l, 83));
        Assert.Equal(7, PlayerHintLine.Column(l, 97));
        Assert.Equal(12, PlayerHintLine.Column(l, 133));
        Assert.Equal(15, PlayerHintLine.Column(l, 146));
        Assert.Equal(-1, PlayerHintLine.Column(l, 156));
    }

    [Fact]
    public void Hint_Number_AddsLineWhenAssigned()
    {
        var p = Skater();
        Assert.Equal("Rückennummer des Spielers (Reihe: 2)", PlayerHintLine.Text(p, 0, InLine2, false));
        Assert.Equal("Rückennummer des Spielers", PlayerHintLine.Text(p, 0, new PlayerHintInfo(false, 0), false));
    }

    [Fact]
    public void Hint_Role_IsNameOnlyWithLoanSuffix()
    {
        Assert.Equal("Stürmer", PlayerHintLine.Text(Skater(), 1, InLine2, false));
        Assert.Equal("Abwehrspieler", PlayerHintLine.Text(Skater(role: PlayerRole.Defender), 1, InLine2, false));
        var g = Skater(role: PlayerRole.Goalkeeper);
        g.OnLoan = true;
        Assert.Equal("Torwart (ausgeliehen)", PlayerHintLine.Text(g, 1, InLine2, false));
    }

    [Fact]
    public void Hint_NameAndAge()
    {
        var p = Skater(age: 27, name: "R.HANFT");
        Assert.Equal("Name: R.HANFT (27 Jahre)", PlayerHintLine.Text(p, 2, InLine2, false));
    }

    [Fact]
    public void Hint_StrengthColumn_AppendsAverageOfThree()
    {
        var p = Skater();
        p.Condition = 16;
        p.Technique = 20;
        p.Form = 53;
        Assert.Equal("Kondition, Technik, Form (29)", PlayerHintLine.Text(p, 7, InLine2, false));
    }

    [Fact]
    public void Hint_FixedTexts()
    {
        var p = Skater();
        Assert.Equal("Spiele gesamt (Liga und Pokal)", PlayerHintLine.Text(p, 6, InLine2, false));
        Assert.Equal("Tore gesamt (Liga und Pokal)", PlayerHintLine.Text(p, 12, InLine2, false));
        Assert.Equal("2/5 Min.-Strafzeiten", PlayerHintLine.Text(p, 13, InLine2, false));
        Assert.Equal("Dauer des Vertrages mit dem Spieler (in Saisons)", PlayerHintLine.Text(p, 16, InLine2, false));
        Assert.Equal("Monatliches Gehalt des Spielers", PlayerHintLine.Text(p, 17, InLine2, false));
        Assert.Null(PlayerHintLine.Text(p, 3, InLine2, false));
    }

    [Fact]
    public void Hint_Status_FollowsOriginalOrder()
    {
        var p = Skater();
        Assert.Equal("Status: IM TEAM", PlayerHintLine.Text(p, 14, InLine2, false));
        Assert.Equal("Status: RESERVE", PlayerHintLine.Text(p, 14, new PlayerHintInfo(true, 0), false));
        Assert.Equal("Status: Hängt halt so rum...", PlayerHintLine.Text(p, 14, new PlayerHintInfo(false, 0), false));
        p.InjuredGames = 3;
        Assert.Equal("Status: Verletzt.", PlayerHintLine.Text(p, 14, InLine2, false));
        p.InjuredGames = 0;
        p.SuspendedGames = 5;
        Assert.Equal("Status: Noch 5 Minuten Gesperrt.", PlayerHintLine.Text(p, 14, InLine2, false));
        // eingesetzt ohne Reihe gilt vor Verletzung und Sperre
        Assert.Equal("Status: RESERVE", PlayerHintLine.Text(p, 14, new PlayerHintInfo(true, 0), false));
    }

    [Fact]
    public void Hint_Trend_ListsExhaustionOnlyInLineup()
    {
        var p = Skater();
        p.Energy = 70; // Belastung +0x13 = 80 → 80 · 10 / 15 = 53 (Datei 0x41B29–0x41B38, #805)
        p.Trend = PlayerTrend.Rising;
        Assert.Equal("Tendenz des Spielers: Steigend.", PlayerHintLine.Text(p, 15, InLine2, false));
        Assert.Equal("Tendenz des Spielers: Steigend. (Erschöpfung:53)", PlayerHintLine.Text(p, 15, InLine2, true));
        p.Trend = PlayerTrend.Falling;
        Assert.StartsWith("Tendenz des Spielers: Fallend.", PlayerHintLine.Text(p, 15, InLine2, false));
    }
}
