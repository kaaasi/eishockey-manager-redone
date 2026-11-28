using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur zusehen, Teil 1b (A752): Spielplan des Gegners, Knopf „Spielplan“ im Vereinsfenster (em.exe Datei 0x05CA0, Klick Datei
/// 0x4E505–0x4E539). Vorher tat der Knopf nichts: das Vereinsfenster blieb unverändert stehen.
/// </summary>
public class OpponentScheduleButtonTests
{
    private static GameState Synthetic()
    {
        var data = new byte[Formats.ClubDatabase.FileLength];
        for (var i = 0; i < Formats.ClubDatabase.ClubsWithSquad; i++)
        {
            System.Text.Encoding.ASCII.GetBytes($"VEREIN {i}").CopyTo(data, Formats.ClubDatabase.SquadOffset + i * Formats.ClubDatabase.SquadRecordLength);
            data[Formats.ClubDatabase.StrengthOffset + i * 3] = (byte)(100 - i % 12 * 5);
        }
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", Head = 0, ClubIndex = 2 });
        return NewGame.Create(Formats.ClubDatabase.Parse(data), setup, 3);
    }

    private static GameState Played(int days)
    {
        var game = Synthetic();
        while (game.PlayedMatchDays < days) // Zeitleiste wie TestSeason.PlayMatchDay (Core-Tests)
            game.Continue();
        return game;
    }

    private static InputState Click(int x, int y) => new() { MouseX = x, MouseY = y, LeftClick = true };

    /// <summary>[ORIGINAL] Knöpfe auf y + 171: „Reicht mir“ x + 10, „Info“ x + 80, Runde x + 150 (Datei 0x5D4F–0x5E58).</summary>
    [Fact]
    public void Buttons_AtTheOriginalPlaces()
    {
        Assert.Equal(new Rect(103, 215, Widgets.ButtonWidth, Widgets.ButtonHeight), ClubScheduleWindow.CloseButton);
        Assert.Equal(new Rect(173, 215, Widgets.ButtonWidth, Widgets.ButtonHeight), ClubScheduleWindow.InfoButton);
        Assert.Equal(new Rect(243, 215, Widgets.ButtonWidth, Widgets.ButtonHeight), ClubScheduleWindow.RoundButton);
    }

    /// <summary>
    /// Regression: „Spielplan“ öffnet den Spielplan; der Rundenknopf wechselt die Hälfte (ohne die Markierung zum Zusehen zu
    /// ändern), „Info“ kehrt ins Vereinsfenster zurück, „Reicht mir“ schließt alles (Rückgabe 1/2, Datei 0x63A4–0x63B7,
    /// 0x4E536). Spielzustand unverändert.
    /// </summary>
    [Fact]
    public void ScheduleButton_OpensSchedule_InfoReturns_CloseCloses()
    {
        var game = Played(3);
        var host = new ScreenHost(null!) { Game = game };
        var menu = new MainMenuScreen(host);
        host.Show(menu);
        var before = StateFingerprint.Of(game);
        menu.InfoWindows.Show(7);
        var window = menu.InfoWindows.Club;

        menu.Update(Click(180, 220)); // „Spielplan“
        Assert.True(window.Schedule.Visible);
        Assert.False(window.Schedule.SecondHalf);
        menu.Update(Click(250, 220)); // „2.Runde “
        Assert.True(window.Schedule.SecondHalf);
        Assert.Equal(0, game.Clubs[7].WatchMode);
        menu.Update(Click(180, 220)); // „Info“
        Assert.False(window.Schedule.Visible);
        Assert.True(window.Visible);

        menu.Update(Click(180, 220));
        Assert.True(window.Schedule.Visible);
        menu.Update(Click(120, 220)); // „Reicht mir“
        Assert.False(menu.InfoWindows.Visible);

        menu.InfoWindows.Show(7);
        menu.Update(Click(180, 220));
        menu.Update(new InputState { MouseX = 160, MouseY = 120, RightClick = true });
        Assert.False(menu.InfoWindows.Visible);
        Assert.Equal(before, StateFingerprint.Of(game));
    }

    /// <summary>Vor dem ersten Spieltag (Tageszähler 0) fehlt der Knopf; ein Klick dort öffnet nichts (Datei 0x4D752).</summary>
    [Fact]
    public void BeforeTheFirstDay_NoSchedule()
    {
        var game = Synthetic();
        var window = new ClubInfoWindow();
        window.Show(7);
        window.Update(Click(180, 220), game);
        Assert.False(window.Schedule.Visible);
    }

    /// <summary>
    /// [ORIGINAL] Zeilen Datei 0x5EE0–0x6290: Spieltage 1 … 11 mit den Spalten d und d + 11; Gegner und Heimrecht aus dem
    /// Spielplan (Heim Farbe 30, Auswärts 1); gespielt „eigene:Gegentore“ zweistellig, sonst „?:Tag.Monat.“ zweistellig.
    /// </summary>
    [Fact]
    public void Rows_FirstHalf_ResultsThenDates()
    {
        var game = Played(3);
        const int club = 7;
        var league = game.Clubs[club].League!.Value;
        var days = game.Schedules[league];
        var rows = ClubScheduleWindow.Rows(game, club, secondHalf: false);
        Assert.Equal(11, rows.Count);
        var dates = SeasonCalendar.LeagueMatchDays(days.Count);
        for (var r = 0; r < rows.Count; r++)
        {
            int[] columns = [r + 1, r + 12];
            Assert.Equal(2, rows[r].Entries.Count);
            for (var c = 0; c < 2; c++)
            {
                var day = columns[c];
                var (home, away) = days[day - 1].Matches.Single(m => m.Home == club || m.Away == club);
                var entry = rows[r].Entries[c];
                Assert.Equal(home == club ? ClubScheduleWindow.HomeColor : ClubScheduleWindow.AwayColor, entry.Color);
                if (c == 0)
                {
                    Assert.Equal(home == club ? away : home, rows[r].Opponent);
                    Assert.Equal(entry.Color, rows[r].Color);
                }
                if (day <= 3)
                {
                    var (own, against, _) = game.Results.Single(m => m.League == league && m.MatchDay == day && m.Involves(club)).For(club);
                    Assert.Equal(CompetitionWidgets.Number(own, 2) + ":" + CompetitionWidgets.Number(against, 2), entry.Text);
                }
                else
                {
                    var date = SeasonCalendar.LayoutDate(dates[day - 1]);
                    Assert.Equal("?:" + CompetitionWidgets.Number(date.Day, 2) + "." + CompetitionWidgets.Number(date.Month, 2) + ".", entry.Text);
                }
            }
        }
        // Zweite Hälfte: Spieltage 23 … 44, dieselben Gegner (Runde 3 = Runde 1), alle noch nicht gespielt.
        var second = ClubScheduleWindow.Rows(game, club, secondHalf: true);
        Assert.Equal(rows.Select(r => (r.Opponent, r.Color)), second.Select(r => (r.Opponent, r.Color)));
        Assert.All(second.SelectMany(r => r.Entries), e => Assert.StartsWith("?:", e.Text));
    }

    /// <summary>[ORIGINAL] 0x4F1F1 mit Kennzeichen 0: Platz und Stärke zweistellig, „. (“ und „)“.</summary>
    [Fact]
    public void PlaceText_AsInTheOriginal() =>
        Assert.Equal(CompetitionWidgets.FigureSpace + "3. (45)", ClubScheduleWindow.PlaceText(3, 45));

    /// <summary>
    /// [ORIGINAL] Hälfte zu Beginn (Datei 0x5CF3–0x5D1A): zweite, wenn Spieltagszähler 0x7A9 (= gespielte + 1) &gt; 22.
    /// </summary>
    [Theory]
    [InlineData(3, false)]
    [InlineData(21, false)]
    [InlineData(22, true)]
    public void InitialHalf_FollowsTheMatchDayCounter(int played, bool second)
    {
        var game = Played(played);
        var schedule = new ClubScheduleWindow();
        schedule.Show(game, 7);
        Assert.Equal(second, schedule.SecondHalf);
    }

    /// <summary>
    /// Bild mit Originaldaten: Flächen Farbe 4 bis x 304 (Datei 0x5D99), Rundenknopf aus kb\2 an der Stelle des dritten Knopfs,
    /// Kopf „GEGNER“ in Farbe 1 auf Grundlinie 104; Öffnen ändert den Spielzustand nicht. Am Video <c>eisbaeren_f12_01646</c>
    /// (Spielplan der Eisbären, 12. Spieltag) vermessen: Kopf, Knöpfe und Zeilen (Grundlinien 113 + 8 · i) am besten bei Versatz
    /// (0 | 0); Heim hell (30), auswärts grau (1), „?: 1.11.“ mit Ziffernplatz, Ergebnisse „0: 5“.
    /// </summary>
    [GameDataFact]
    public void Render_ScheduleOverTheClubWindow()
    {
        var d = ScreenDriver.Create(TestGameData.Load());
        d.StartNewGame();
        d.PlayMatchDay();
        var game = d.Host.Game!;
        d.Frame();
        var before = StateFingerprint.Of(game);
        d.Click(184, 39); // Gegner
        d.Frame();
        d.Click(180, 220); // „Spielplan“
        d.Frame();
        Assert.Equal(before, StateFingerprint.Of(game));
        byte Px(int x, int y) => d.Surface.Pixels[y * Surface.Width + x];
        Assert.Equal(4, Px(97, 97));
        Assert.Equal(4, Px(303, 200));
        Assert.Equal(4, Px(300, 228));
        var button = d.Host.Assets.Image(2);
        Assert.Equal(button[17 + 5, 179 + 6], Px(243 + 5, 215 + 6));
        // „GEGNER“: Farbe 1 innerhalb x 108 … 140 auf den Zeilen über der Grundlinie 104.
        var header = Enumerable.Range(108, 32).SelectMany(x => Enumerable.Range(98, 7).Select(y => Px(x, y)));
        Assert.Contains((byte)1, header);
        // Erste Zeile: Ergebnis des gespielten ersten Spieltags in der linken Spalte (Farbe 30 oder 1 um x 250).
        var entry = Enumerable.Range(236, 30).SelectMany(x => Enumerable.Range(107, 7).Select(y => Px(x, y))).ToList();
        Assert.True(entry.Contains(30) || entry.Contains(1));
    }
}
