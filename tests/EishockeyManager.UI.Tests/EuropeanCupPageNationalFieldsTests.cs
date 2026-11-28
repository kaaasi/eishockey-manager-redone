using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Runde 25, Spur wettbewerbe-anzeige: B10 (gemerkte Seite der Europapokal-Kacheln, A268/A269) und B17 (Reihenwerte der
/// Nationalteam-Mannschaftsfelder über 0x977F, A285/A419/A961).
/// </summary>
public class EuropeanCupPageNationalFieldsTests
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
        setup.Managers.Add(new ManagerSetup { Name = "A", Head = 0, ClubIndex = 8 });
        return NewGame.Create(Formats.ClubDatabase.Parse(data), setup, 3);
    }

    private sealed class Dummy(ScreenHost host) : Screen(host)
    {
        public override void Update(InputState input) { }
        public override void Draw(Surface s) { }
    }

    private static InputState Click(Rect r) => new() { MouseX = r.X + 5, MouseY = r.Y + 5, LeftClick = true };

    private static readonly InputState RightClick = new() { MouseX = 160, MouseY = 100, RightClick = true };

    /// <summary>
    /// B17 (A285, A419): [ORIGINAL] Die Mannschaftsfelder zeigen (K + T) / 2 aus 0x977F (Datei 0x3B65C, 0x3B664–0x3B691): Länder
    /// ohne Bundestrainer Teamwerte + 4 − 4 · Reihe, je auf 0 … 100 begrenzt. Erwartete Werte nach der Nachbildung
    /// `b17_reihenwerte_national.py` (docs/archiv/analyse/re-abweichungen.md B17). Land 55 (DGROUP 0x4C4E: K/T Abwehr 90/90, Sturm 95/93):
    /// Abwehr 94, 90, 86, Sturm 98, 94, 90. Vorher zeigte das Remake (K + T) / 2 ohne Reihenstufe: Abwehr 90, 90, 90, Sturm
    /// 94, 94, 94. Land 52 (Abwehr 97/103): 100, 98, 96 – vorher 99 in allen Reihen (Grenze 99).
    /// </summary>
    [Theory]
    [InlineData(55, false, new[] { 94, 90, 86 })]
    [InlineData(55, true, new[] { 98, 94, 90 })]
    [InlineData(52, false, new[] { 100, 98, 96 })]
    public void TeamPanel_LineValues_Of0x977F(int nation, bool attack, int[] expected)
    {
        var game = Synthetic();
        Assert.Equal(expected, Enumerable.Range(0, NationalCoach.Lines).Select(l => NationalWidgets.LineValue(game, nation, l, attack)));
    }

    /// <summary>
    /// B17 (A961): Land des Bundestrainers – die Anzeige nimmt denselben Weg 0x977F → 0x98F8 wie <see cref="GameState.NationalPanelLineValue"/>
    /// (über <see cref="MatchEngine.NationalSquad"/>) statt des Mittels der Aufstellung (<see cref="NationalLineup.LineValue"/>).
    /// </summary>
    [Fact]
    public void TeamPanel_CoachNation_UsesPanelLineValue()
    {
        var game = Synthetic();
        game.AppointNationalCoach(game.Managers[0]);
        game.AutoNominate();
        var coach = game.NationalCoach!;
        coach.AutoFormation();
        for (var line = 0; line < NationalCoach.Lines; line++)
            foreach (var attack in new[] { false, true })
                Assert.Equal(game.NationalPanelLineValue(coach.Nation, line, attack), NationalWidgets.LineValue(game, coach.Nation, line, attack));
        Assert.Contains(Enumerable.Range(0, NationalCoach.Lines),
            l => NationalWidgets.LineValue(game, coach.Nation, l, false) != coach.Lineup().LineValue(l, attack: false)
                || NationalWidgets.LineValue(game, coach.Nation, l, true) != coach.Lineup().LineValue(l, attack: true));
    }

    /// <summary>
    /// B10 (A269, A268): [ORIGINAL] „Tabelle Europapokal“ (DGROUP 0x5123) und „Europapokal“ der Paarungen (DGROUP 0x5126) merken je
    /// die zuletzt gezeigte Seite (Rückgabe des Seitenwählers 0x3A88A, Datei 0x45862 bzw. 0x458BB; Seiten 3/4/5 = Gruppe 1/2,
    /// Endrunde). Vorher öffneten beide immer Gruppe 1. Der Saisonwechsel setzt beide auf 3 zurück (Datei 0x5649D–0x564BA).
    /// </summary>
    [Fact]
    public void EuropeanCupTiles_RememberLastPage_PerTile_UntilSeasonChange()
    {
        var game = Synthetic();
        var host = new ScreenHost(null!) { Game = game };
        var menu = new Dummy(host);
        var table = MenuEntries.All["Tabelle Europapokal"];
        var pairings = MenuEntries.All["Europapokal"];
        var before = StateFingerprint.Of(game);

        var first = Assert.IsType<EuropeanCupScreen>(table(host, menu));
        Assert.Equal(0, first.View);
        host.Show(first);
        first.Update(Click(EuropeanCupScreen.RightButton)); // Gruppe 1 → Endrunde (Seite 5)
        Assert.Equal(2, first.View);
        first.Update(RightClick);
        Assert.Same(menu, host.Current);

        Assert.Equal(2, Assert.IsType<EuropeanCupScreen>(table(host, menu)).View);
        var other = Assert.IsType<EuropeanCupScreen>(pairings(host, menu)); // eigenes Byte 0x5126
        Assert.Equal(0, other.View);
        host.Show(other);
        other.Update(Click(EuropeanCupScreen.LeftButton)); // Gruppe 1 → Gruppe 2 (Seite 4)
        Assert.Equal(1, other.View);
        other.Update(new InputState { Keys = { Key.Enter } });
        Assert.Equal(1, Assert.IsType<EuropeanCupScreen>(pairings(host, menu)).View);
        Assert.Equal(2, Assert.IsType<EuropeanCupScreen>(table(host, menu)).View);
        Assert.Equal(before, StateFingerprint.Of(game));

        game.Season++;
        Assert.Equal(0, Assert.IsType<EuropeanCupScreen>(table(host, menu)).View);
        Assert.Equal(0, Assert.IsType<EuropeanCupScreen>(pairings(host, menu)).View);
    }
    // ---- Teil 2: B15/B16 Gegner- und Vereinsfenster ------------------------------------------------------------------

    private static int[] Form(string signs) => [.. signs.Select(c => c switch { 's' => 3, 'u' => 2, 'n' => 1, _ => 0 })];

    /// <summary>
    /// B15 (A287): [ORIGINAL] Gesicht 0x51AAF – Stimmung 2 + Siege − Niederlagen über die acht Bilanzzeichen, erst danach auf
    /// 1 … 3 begrenzt. Erwartete Werte aus <c>b15_gegnerfenster.py</c> Teil 1 (re-abweichungen B15/B16). Vorher immer gelb.
    /// </summary>
    [Theory]
    [InlineData("--------", Widgets.Mood.Same)]
    [InlineData("s-------", Widgets.Mood.Up)]
    [InlineData("n-------", Widgets.Mood.Down)]
    [InlineData("uuuuuuuu", Widgets.Mood.Same)]
    [InlineData("snsnsnsn", Widgets.Mood.Same)]
    [InlineData("ssssnnnn", Widgets.Mood.Same)]
    [InlineData("nnnnnsss", Widgets.Mood.Down)]
    [InlineData("ssssssss", Widgets.Mood.Up)]
    public void FormMood_FromRecordSigns(string signs, Widgets.Mood expected) =>
        Assert.Equal(expected, TeamInfoWindow.FormMood(Form(signs)));

    /// <summary>
    /// B15 (A287, A289): [ORIGINAL] Das Fenster zeichnet das Gesicht bei (249 | 89) (Ziel 0xF9 der Bildausgabe in 0x51AAF) nach der
    /// Bilanz: nach einem WM-Gruppensieg grün. Vorher gelb bei x = 250.
    /// </summary>
    [GameDataFact]
    public void TeamInfoWindow_DrawsFaceOfRecord()
    {
        var d = ScreenDriver.Create(TestGameData.Load());
        d.StartNewGame();
        var game = d.Host.Game!;
        game.EnterNationalResult(55, 56, 4, 1, NationalMatchKind.WorldCupGroup);
        var screen = new WorldCupScreen(d.Host, new MainMenuScreen(d.Host));
        d.Host.Show(screen);
        screen.Info.Show(55);
        d.Frame();
        var expected = new Surface { Palette = d.Surface.Palette };
        expected.Fill(new Rect(0, 0, 320, 240), 4);
        Widgets.Smiley(expected, d.Host.Assets, Widgets.Mood.Up, TeamInfoWindow.MoodX, TeamInfoWindow.MoodY);
        Assert.Equal(249, TeamInfoWindow.MoodX);
        for (var y = 89; y < 94; y++)
            for (var x = 249; x < 255; x++)
                if (expected.Pixels[y * 320 + x] != 4)
                    Assert.Equal(expected.Pixels[y * 320 + x], d.Surface.Pixels[y * 320 + x]);
    }

    /// <summary>
    /// B15/B16 (A286, A867): [ORIGINAL] Zeile „Historische Ergebnisse“ und dritter Knopf nur bei Wettbewerbscode DGROUP 0xD0C2 = 0
    /// (Datei 0x4D7AA, 0x4E07A). Nach einem Freundschaftsspiel (Code 0xD, Datei 0x30390) fehlen beide bis zum nächsten Aufbau
    /// des Hauptmenüs (nächster Zug); ein Klick auf die Stelle des Knopfs schaltet dann nichts um. Vorher immer gezeigt.
    /// </summary>
    [Fact]
    public void HistoryLineAndWatchButton_OnlyWithCompetitionCodeZero()
    {
        var game = Synthetic();
        var host = new ScreenHost(null!) { Game = game };
        const int nation = 55;
        static bool HasHistory(GameState g) => TeamInfoWindow.Lines(g, nation).Any(l => l.Text.StartsWith("Historische Ergebnisse"));
        Assert.True(CompetitionCode.IsMainMenu(game));
        Assert.True(HasHistory(game));
        Assert.Contains(ClubInfoWindow.Lines(game, 20), l => l.Text.StartsWith("Historische Ergebnisse"));

        CompetitionCode.FriendlyPlayed(game);
        Assert.False(CompetitionCode.IsMainMenu(game));
        Assert.False(HasHistory(game));
        var window = new TeamInfoWindow();
        window.Show(nation);
        var mode = game.Clubs[nation].WatchMode;
        window.Update(Click(TeamInfoWindow.WatchButton), game);
        Assert.Equal(mode, game.Clubs[nation].WatchMode);
        Assert.DoesNotContain(ClubInfoWindow.Lines(game, 20), l => l.Text.StartsWith("Historische Ergebnisse"));

        game.Continue(); // nächster Zug: das Hauptmenü setzt Code 0 (Datei 0x44E32)
        Assert.True(CompetitionCode.IsMainMenu(game));
        Assert.True(HasHistory(game));
        window.Update(Click(TeamInfoWindow.WatchButton), game);
        Assert.NotEqual(mode, game.Clubs[nation].WatchMode);
    }

    /// <summary>
    /// Befund #1010 (R25-W1): Knopf zum Zusehen und Gesicht im Vereinsfenster über die Zeichnung abgesichert (B15/B16, Datei 0x4BFF2,
    /// 0x51AAF): Der dritte Knopf steht nur bei Wettbewerbscode 0, das Gesicht folgt der Bilanz (nicht immer gelb). Rückschaltprobe:
    /// <c>FormMood(form)</c> → <c>Mood.Same</c> bzw. <c>IsMainMenu</c> → <c>true</c> in <c>ClubInfoWindow.Draw</c> macht rot.
    /// </summary>
    [GameDataFact]
    public void ClubInfoWindow_DrawsWatchButtonOnlyInMainMenuCode_AndFaceOfRecord()
    {
        var d = ScreenDriver.Create(TestGameData.Load());
        d.StartNewGame();
        for (var i = 0; i < 6; i++)
            d.PlayMatchDay();
        var game = d.Host.Game!;
        var menu = Assert.IsType<MainMenuScreen>(d.Host.Current);
        var club = Enumerable.Range(0, NewGame.SelectableClubs)
            .First(c => TeamInfoWindow.FormMood(ClubInfoWindow.Form(game, c)) != Widgets.Mood.Same);
        var mood = TeamInfoWindow.FormMood(ClubInfoWindow.Form(game, club));
        Assert.True(CompetitionCode.IsMainMenu(game));

        menu.InfoWindows.Club.Show(club);
        d.Frame();
        var assets = d.Host.Assets;
        var button = TeamInfoWindow.WatchButton;
        var expected = new Surface { Palette = d.Surface.Palette };
        expected.Fill(new Rect(0, 0, 320, 240), 4);
        expected.Blit(assets.Image(2), new Rect(17, 179, Widgets.ButtonWidth, Widgets.ButtonHeight), button.X, button.Y, transparent: 0);
        expected.Blit(assets.Image(TeamInfoWindow.WatchSymbolImage), TeamInfoWindow.WatchSymbol(game.Clubs[club].WatchMode),
            button.X + 23, button.Y + 1);
        var face = new Surface { Palette = d.Surface.Palette };
        face.Fill(new Rect(0, 0, 320, 240), 4);
        Widgets.Smiley(face, assets, mood, TeamInfoWindow.MoodX, TeamInfoWindow.MoodY);

        int Diff(Surface reference, int x0, int y0, int w, int h)
        {
            var n = 0;
            for (var y = y0; y < y0 + h; y++)
                for (var x = x0; x < x0 + w; x++)
                    if (reference.Pixels[y * 320 + x] != 4 && reference.Pixels[y * 320 + x] != d.Surface.Pixels[y * 320 + x])
                        n++;
            return n;
        }

        Assert.Equal(0, Diff(face, TeamInfoWindow.MoodX, TeamInfoWindow.MoodY, 6, 5));
        Assert.Equal(0, Diff(expected, button.X, button.Y, button.W, button.H));
        // Das Gesicht des Gegenteils unterscheidet sich (sonst prüfte der Vergleich nichts).
        var other = new Surface { Palette = d.Surface.Palette };
        other.Fill(new Rect(0, 0, 320, 240), 4);
        Widgets.Smiley(other, assets, mood == Widgets.Mood.Up ? Widgets.Mood.Down : Widgets.Mood.Up, TeamInfoWindow.MoodX, TeamInfoWindow.MoodY);
        Assert.NotEqual(0, Diff(other, TeamInfoWindow.MoodX, TeamInfoWindow.MoodY, 6, 5));

        // Nach einem Freundschaftsspiel (Code ≠ 0) fehlt der Knopf: dort bleibt das Fensterbild.
        CompetitionCode.FriendlyPlayed(game);
        Assert.False(CompetitionCode.IsMainMenu(game));
        d.Frame();
        Assert.NotEqual(0, Diff(expected, button.X, button.Y, button.W, button.H));
    }

    /// <summary>Befund #1010, Länderfenster: Der dritte Knopf wird nach einem Freundschaftsspiel (Code ≠ 0) nicht gezeichnet.</summary>
    [GameDataFact]
    public void TeamInfoWindow_DrawsWatchButtonOnlyInMainMenuCode()
    {
        var d = ScreenDriver.Create(TestGameData.Load());
        d.StartNewGame();
        var game = d.Host.Game!;
        var screen = new WorldCupScreen(d.Host, new MainMenuScreen(d.Host));
        d.Host.Show(screen);
        screen.Info.Show(55);
        var button = TeamInfoWindow.WatchButton;
        var expected = new Surface { Palette = d.Surface.Palette };
        expected.Fill(new Rect(0, 0, 320, 240), 4);
        expected.Blit(d.Host.Assets.Image(2), new Rect(17, 179, Widgets.ButtonWidth, Widgets.ButtonHeight), button.X, button.Y, transparent: 0);
        expected.Blit(d.Host.Assets.Image(TeamInfoWindow.WatchSymbolImage), TeamInfoWindow.WatchSymbol(game.Clubs[55].WatchMode),
            button.X + 23, button.Y + 1);
        int Diff()
        {
            var n = 0;
            for (var y = button.Y; y < button.Y + button.H; y++)
                for (var x = button.X; x < button.X + button.W; x++)
                    if (expected.Pixels[y * 320 + x] != 4 && expected.Pixels[y * 320 + x] != d.Surface.Pixels[y * 320 + x])
                        n++;
            return n;
        }

        d.Frame();
        Assert.Equal(0, Diff());
        CompetitionCode.FriendlyPlayed(game);
        d.Frame();
        Assert.NotEqual(0, Diff());
    }

    // ---- Teil 3: C07 Klick auf einen Verein ---------------------------------------------------------------------------

    private static InputState LeftAt(int x, int y) => new() { MouseX = x, MouseY = y, LeftClick = true };

    /// <summary>
    /// C07 (A277): [ORIGINAL] „DEB-Pokalspiele“ (0x32E23, Beleg <c>c07_klick.py</c>): Linksklick auf eine Zeile (Grundlinie − 6 &lt; y ≤
    /// Grundlinie) öffnet das Vereinsfenster, x ≤ 0x63 links, sonst rechts; der Bildschirm bleibt. Linksklick in die Kopfzeile oder
    /// eine Zeile darunter tut nichts, Rechtsklick führt zurück. Vorher führte jeder Klick zurück.
    /// </summary>
    [Fact]
    public void DebCupGames_ClickOnClub_OpensClubWindow_RightClickLeaves()
    {
        var game = Synthetic();
        var host = new ScreenHost(null!) { Game = game };
        var menu = new Dummy(host);
        host.Show(menu);
        string[] titles = ["A", "B", "C", "D", "E", "F"];
        var groups = titles.Select((t, i) => new DebCupGroup(t, i == 0 ? [new DebCupRow(23, 33, []), new DebCupRow(24, 34, [])] : [])).ToList();
        var overview = new DebCupOverview(0, 0, null, groups, "");
        var screen = new DebCupGamesScreen(host, menu, overview);
        host.Show(screen);
        var lines = DebCupGamesScreen.RowLines(overview);
        Assert.Equal(2, lines.Count);
        var (_, top) = DebCupGamesScreen.FrameFor(2, 5);
        Assert.Equal(top + 46 + 8, lines[0].Y);

        Assert.Equal(23, DebCupGamesScreen.ClubAt(overview, 0x63, lines[0].Y));
        Assert.Equal(33, DebCupGamesScreen.ClubAt(overview, 0x64, lines[0].Y - 5));
        Assert.Equal(34, DebCupGamesScreen.ClubAt(overview, 150, lines[1].Y));
        Assert.Null(DebCupGamesScreen.ClubAt(overview, 50, lines[0].Y - 6));
        Assert.Null(DebCupGamesScreen.ClubAt(overview, 50, lines[1].Y + 1));

        var before = StateFingerprint.Of(game);
        screen.Update(LeftAt(50, top + 20)); // Kopfzeile
        Assert.Same(screen, host.Current);
        Assert.False(screen.InfoWindows.Visible);
        screen.Update(LeftAt(150, lines[0].Y));
        Assert.Same(screen, host.Current);
        Assert.Equal(33, screen.InfoWindows.Club.Club);
        screen.Update(RightClick); // schließt das Fenster
        Assert.False(screen.InfoWindows.Visible);
        Assert.Same(screen, host.Current);
        screen.Update(RightClick);
        Assert.Same(menu, host.Current);
        Assert.Equal(before, StateFingerprint.Of(game));
    }

    /// <summary>
    /// C07 (A277): [ORIGINAL] „WM-Endrundenspiele“ (Art 3, Seite 2): Klick auf ein Land öffnet das Gegner-Fenster, der Bildschirm
    /// bleibt; Rechtsklick führt zurück. Vorher führte jeder Linksklick zurück.
    /// </summary>
    [Fact]
    public void WorldCupKnockout_ClickOnTeam_OpensTeamWindow()
    {
        var game = Synthetic();
        var host = new ScreenHost(null!) { Game = game };
        var menu = new Dummy(host);
        host.Show(menu);
        var sections = new List<WorldCupKnockoutSection> { new("Viertelfinale", [new(52, 55, 0, 0, 0), new(53, 56, 0, 0, 0)]) };
        var screen = new WorldCupKnockoutScreen(host, menu, sections);
        host.Show(screen);
        var (_, top) = WorldCupKnockoutScreen.FrameFor(2, 1, live: false);
        var first = top + 46 + WorldCupKnockoutScreen.HeadingPitch;
        Assert.Equal(52, screen.TeamAt(40, first));
        Assert.Equal(56, screen.TeamAt(120, first + DebCupGamesScreen.RowPitch));
        Assert.Null(screen.TeamAt(40, first + 1 + DebCupGamesScreen.RowPitch));

        screen.Update(LeftAt(120, first));
        Assert.Same(screen, host.Current);
        Assert.Equal(55, screen.InfoWindows.Nation.Club);
        screen.Update(RightClick);
        Assert.False(screen.InfoWindows.Visible);
        screen.Update(LeftAt(10, top + 5));
        Assert.Same(screen, host.Current);
        screen.Update(RightClick);
        Assert.Same(menu, host.Current);
    }
}
