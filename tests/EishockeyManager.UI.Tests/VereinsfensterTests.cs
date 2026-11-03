using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur ui-meldungen Teil 2 (UM-2): Vereinsfenster, Vereinszweig von em.exe Datei 0x4D385 (<c>docs/analyse/re-runde-12.md</c>
/// „UM-2“). Vorher gab es im Remake kein Vereinsfenster: Ein Klick auf den Vereins- oder Gegnernamen der Infoleiste tat nichts.
/// </summary>
public class VereinsfensterTests
{
    private static GameState Synthetic(int managers = 1)
    {
        var data = new byte[Formats.ClubDatabase.FileLength];
        for (var i = 0; i < Formats.ClubDatabase.ClubsWithSquad; i++)
        {
            System.Text.Encoding.ASCII.GetBytes($"VEREIN {i}").CopyTo(data, Formats.ClubDatabase.SquadOffset + i * Formats.ClubDatabase.SquadRecordLength);
            data[Formats.ClubDatabase.StrengthOffset + i * 3] = (byte)(100 - i % 12 * 5);
        }
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", Head = 0, ClubIndex = 2 });
        if (managers > 1)
            setup.Managers.Add(new ManagerSetup { Name = "B", Head = 1, ClubIndex = 5 });
        return NewGame.Create(Formats.ClubDatabase.Parse(data), setup, 3);
    }

    private static InputState Click(int x, int y) => new() { MouseX = x, MouseY = y, LeftClick = true };

    /// <summary>
    /// Regression: Klick auf den eigenen Vereinsnamen oben rechts (21 &lt; y &lt; 31, x &gt; 229, Datei 0x453B4) öffnet das
    /// Vereinsfenster für den eigenen Verein; Rechtsklick schließt es, das Menü bleibt; Spielzustand unverändert.
    /// </summary>
    [Fact]
    public void MainMenu_ClickOnOwnClubName_OpensClubWindow()
    {
        var game = Synthetic();
        var host = new ScreenHost(null!) { Game = game };
        var menu = new MainMenuScreen(host);
        host.Show(menu);
        var before = StateFingerprint.Of(game);

        menu.Update(Click(272, 26));
        Assert.Same(menu, host.Current);
        Assert.Equal(game.ActiveManager.ClubIndex, menu.InfoWindows.Club.Club);
        Assert.Equal(before, StateFingerprint.Of(game));

        menu.Update(new InputState { RightClick = true });
        Assert.False(menu.InfoWindows.Visible);
        Assert.Same(menu, host.Current);
        Assert.Equal(before, StateFingerprint.Of(game));
    }

    /// <summary>Regression: Klick auf den Gegnernamen (34 &lt; y &lt; 44, 137 &lt; x &lt; 231, Datei 0x45375) öffnet das Fenster des Gegners.</summary>
    [Fact]
    public void MainMenu_ClickOnOpponentName_OpensWindowOfNextOpponent()
    {
        var game = Synthetic();
        var host = new ScreenHost(null!) { Game = game };
        var menu = new MainMenuScreen(host);
        host.Show(menu);
        var opponent = game.UpcomingMatch(game.ActiveManager.ClubIndex)!.Value.Opponent.Index;

        menu.Update(Click(184, 39));
        Assert.Equal(opponent, menu.InfoWindows.Club.Club);
        // Solange das Fenster steht, gehen Klicks nicht ans Menü („Tag weiter“ auf dem Logo).
        var day = game.CurrentDay;
        menu.Update(Click(290, 50));
        Assert.Equal(day, game.CurrentDay);
        Assert.True(menu.InfoWindows.Visible);
    }

    /// <summary>[ORIGINAL] Trefferflächen Datei 0x4537C–0x45392 und 0x453BB–0x453CA.</summary>
    [Fact]
    public void HitAreas_FollowTheOriginal()
    {
        Assert.False(InfoBarWindows.OpponentName.Contains(184, 34));
        Assert.True(InfoBarWindows.OpponentName.Contains(138, 35));
        Assert.True(InfoBarWindows.OpponentName.Contains(230, 43));
        Assert.False(InfoBarWindows.OpponentName.Contains(231, 40));
        Assert.False(InfoBarWindows.OpponentName.Contains(137, 40));
        Assert.True(InfoBarWindows.OwnClubName.Contains(230, 22));
        Assert.True(InfoBarWindows.OwnClubName.Contains(319, 30));
        Assert.False(InfoBarWindows.OwnClubName.Contains(229, 26));
        Assert.False(InfoBarWindows.OwnClubName.Contains(260, 31));
    }

    /// <summary>
    /// [ORIGINAL] Zeilen des Vereinszweigs: „Heimbilanz:“/„Auswtrsbilanz:“ (Schreibfehler DGROUP 0xFAD) getrennt, Kopf-an-Kopf
    /// gegen den Verein des Managers, Ewige Tabelle zuletzt („Noch keine...“ in der ersten Saison).
    /// </summary>
    [Fact]
    public void Lines_ClubBranch_HomeAwayHeadToHeadEternal()
    {
        var game = Synthetic();
        while (game.PlayedMatchDays < 4) // Zeitleiste wie TestSeason.PlayMatchDay (Core-Tests)
            game.Continue();
        var own = game.ActiveManager.ClubIndex;
        var other = game.Results.First(r => r.Involves(own)) is var m && m.Home == own ? m.Away : m.Home;

        var lines = ClubInfoWindow.Lines(game, other).ToList();
        Assert.Equal("Heimbilanz:", lines[0].Text);
        Assert.Equal(TeamInfoWindow.FirstBaseline, lines[0].Baseline);
        var (hw, hd, hl) = ClubInfoWindow.LeagueRecord(game, other, home: true);
        Assert.Equal($"{hw} Siege, {hd} Unent., {hl} Nied.", lines[1].Text);
        Assert.Equal("Auswätrsbilanz:", lines[2].Text);
        Assert.Equal(TeamInfoWindow.FirstBaseline + 6, lines[2].Baseline);
        var played = game.Results.Count(r => r.Involves(other));
        var (aw, ad, al) = ClubInfoWindow.LeagueRecord(game, other, home: false);
        Assert.Equal(played, hw + hd + hl + aw + ad + al);

        var h2h = lines.Single(l => l.Text.StartsWith("Historische Ergebnisse gegen "));
        Assert.Equal("Historische Ergebnisse gegen " + game.Clubs[own].Name, h2h.Text);
        var list = lines[lines.IndexOf(h2h) + 1];
        Assert.StartsWith("H: ", list.Text);
        Assert.Contains(" -- A: ", list.Text);
        var eternal = lines[^1];
        Assert.Equal("Punkte in der Ewigen Tabelle: Noch keine...", eternal.Text);
        Assert.Equal(TeamInfoWindow.NoSeriesColor, eternal.Color);
        Assert.Equal(list.Baseline + 8, eternal.Baseline);

        // Eigener Verein: keine Kopf-an-Kopf-Liste, die Ewige Tabelle folgt ohne Abstand (Datei 0x4E092–0x4E0A6).
        var ownLines = ClubInfoWindow.Lines(game, own);
        Assert.DoesNotContain(ownLines, l => l.Text.StartsWith("Historische"));
        Assert.StartsWith(ClubInfoWindow.EternalLabel, ownLines[^1].Text);
    }

    /// <summary>[ORIGINAL] Liste „H: … -- A: …“ (Datei 0x4E15C–0x4E31A): Heimspiele = Seite 1, leere Seite „ Noch keine“.</summary>
    [Fact]
    public void HeadToHeadText_FollowsTheOriginal()
    {
        var list = Enumerable.Repeat(GameState.HeadToHeadEmpty, 10).ToArray();
        Assert.Equal("H:  Noch keine -- A:  Noch keine", ClubInfoWindow.HeadToHeadText(list));
        list[1] = 3 << 4 | 1; // Seite 1, Platz 0: Verein 3, Manager 1
        list[3] = 0 << 4 | 2;
        list[0] = 5 << 4 | 4; // Seite 0, Platz 0
        Assert.Equal("H: 3:1,0:2 -- A: 5:4", ClubInfoWindow.HeadToHeadText(list));
    }

    /// <summary>[ORIGINAL] Zeile 1 und Knopf „Spielplan“ (Datei 0x4D752, 0x4D7F2): Platz, Liga, Stärke; Europapokal-Gegner nur „Stärke:“.</summary>
    [Fact]
    public void HeadLine_AndScheduleButton()
    {
        var game = Synthetic();
        var own = game.ActiveManager.ClubIndex;
        var league = game.Clubs[own].League!.Value;
        Assert.Matches($@"^\d+\. Platz in der {ClubInfoWindow.LeagueNames[(int)league]}, Stärke: \d+$", ClubInfoWindow.HeadLine(game, own));
        Assert.Equal(game.Day != 0, ClubInfoWindow.HasSchedule(game, own));
        Assert.False(ClubInfoWindow.HasSchedule(game, NationalTeams.Germany));

        if (game.Clubs.Count > EuropeanCup.FirstOpponent)
        {
            var foreign = EuropeanCup.FirstOpponent;
            Assert.StartsWith("Stärke:", ClubInfoWindow.HeadLine(game, foreign));
            Assert.Empty(ClubInfoWindow.Lines(game, foreign));
            Assert.False(ClubInfoWindow.HasSchedule(game, foreign));
        }
    }

    /// <summary>
    /// [ORIGINAL] Bild mit Originaldaten: Tabellenkurve mit Ursprung (123 | 14) – Achsen (125 | 62)–(125 | 85) Farbe 6 und
    /// (126 | 86)–(221 | 86) Farbe 8 (Datei 0x4D588, 0x4460D–0x4465E); Fenster öffnen ändert den Spielzustand nicht.
    /// </summary>
    [GameDataFact]
    public void Render_CurveAxesAtTheOriginalPlace()
    {
        var d = ScreenDriver.Create(TestGameData.Load());
        d.StartNewGame();
        d.PlayMatchDay();
        d.PlayMatchDay();
        var menu = Assert.IsType<MainMenuScreen>(d.Host.Current);
        var game = d.Host.Game!;
        d.Frame(); // [APPROX] bis Teil 4 (E4) setzt schon das Zeichnen der Nachrichtenleiste „gelesen“ – vorher abwarten
        var before = StateFingerprint.Of(game);
        d.Click(272, 26);
        d.Frame();
        Assert.True(menu.InfoWindows.Club.Visible);
        for (var y = 62; y <= 85; y++)
            Assert.Equal(RankCurve.VerticalAxisColor, d.Surface.Pixels[y * Surface.Width + 125]);
        Assert.Equal(RankCurve.HorizontalAxisColor, d.Surface.Pixels[86 * Surface.Width + 126]);
        Assert.Equal(RankCurve.HorizontalAxisColor, d.Surface.Pixels[86 * Surface.Width + 221]);
        Assert.Equal(before, StateFingerprint.Of(game));
    }
}
