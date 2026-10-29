using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Screens.Team;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur nationalteam-2: Aufstellung und Taktik des Nationalteams im Menü des Bundestrainers.
/// </summary>
public class NationalLineupScreenTests
{
    /// <summary>Spielmodus „nur Weltkugel“ starten und bis zur Ernennung springen (wie WorldOnlyMenuTests).</summary>
    internal static (ScreenDriver Driver, GameState Game) StartWorldOnly()
    {
        var d = FlowTests.CreateDriver();
        d.Host.Show(new TitleScreen(d.Host));
        d.Frame();
        d.Click(160, 120);
        Assert.IsType<SetupScreen>(d.Host.Current);
        d.Click(20, 60);
        var cell = SetupScreen.HeadCell(3);
        d.Click(cell.X + 5, cell.Y + 5);
        d.Type("LetsShow");
        d.Press(Key.Enter);
        d.Click(SetupScreen.ModeFlag.X + 5, SetupScreen.ModeFlag.Y + 5);
        d.Click(SetupScreen.StartButton.X + 5, SetupScreen.StartButton.Y + 5);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        var game = d.Host.Game!;
        Assert.Equal(GameMode.WorldOnly, game.Mode);
        d.PlayMatchDay();
        Assert.NotNull(game.NationalCoach);
        return (d, game);
    }

    /// <summary>
    /// Befund: Die Kacheln „Aufstellung“ und „Taktik“ des Bundestrainers zeigten nur den Platzhalter.
    /// Jetzt öffnen sie die Bildschirme des Nationalteams; Öffnen, Überfahren und Umschalten ändern nichts.
    /// </summary>
    [GameDataFact]
    public void CoachMenu_OpensNationalLineupAndTactics_WithoutChangingState()
    {
        var (d, game) = StartWorldOnly();
        game.AutoNominate();
        var before = StateFingerprint.Of(game);

        d.OpenTile("Aufstellung");
        Assert.IsNotType<PlaceholderScreen>(d.Host.Current);
        Assert.IsType<NationalLineupScreen>(d.Host.Current);
        d.Move(100, 60);
        d.Frame();
        d.Press(Key.Escape);
        Assert.IsType<MainMenuScreen>(d.Host.Current);

        d.OpenTile("Taktik");
        Assert.IsNotType<PlaceholderScreen>(d.Host.Current);
        d.Frame();
        d.Press(Key.Escape);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        Assert.Equal(before, StateFingerprint.Of(game));
    }

    private static int RowY(int row) => LineupScreen.FirstRowY + row * LineupScreen.RowPitch + 2;

    private static int Count(Surface s, int x0, int y0, int x1, int y1, Func<byte, bool> match)
    {
        var n = 0;
        for (var y = y0; y < y1; y++)
            for (var x = x0; x < x1; x++)
                if (match(s.Pixels[y * Surface.Width + x]))
                    n++;
        return n;
    }

    /// <summary>
    /// Abnahme (3), Merkmale aus `bundestrainer_00056` (Pixelvergleich in docs/wettbewerbe.md): Belegung wie im Video
    /// → NR und STATUS nur bei eingesetzten Zeilen, Platzknöpfe mit der Zeilennummer, „Eingesetzt: 22“ in Farbe 16.
    /// </summary>
    [GameDataFact]
    public void Render_MatchesVideoFeatures()
    {
        var (d, game) = StartWorldOnly();
        game.AutoNominate();
        var coach = game.NationalCoach!;
        Player Row(int number) => coach.Rows[number - 1];
        coach.Assign(LineupSlot.Goalkeeper, Row(2));
        int[][] forwards = [[14, 15, 16], [17, 18, 20], [19, 21, 22]];
        int[][] defenders = [[4, 5], [6, 7], [8, 11]];
        for (var b = 0; b < 3; b++)
        {
            for (var i = 0; i < 3; i++)
                Assert.Equal(LineupResult.Assigned, coach.Assign(LineupSlot.Forward(b, i), Row(forwards[b][i])));
            for (var i = 0; i < 2; i++)
                Assert.Equal(LineupResult.Assigned, coach.Assign(LineupSlot.Defender(b, i), Row(defenders[b][i])));
        }
        foreach (var reserve in new[] { 1, 3, 9, 10, 25, 26 })
            Assert.Equal(LineupResult.Dressed, coach.Dress(Row(reserve)));
        Assert.Equal(NationalCoach.MaxDressed, coach.DressedCount);

        d.Host.Show(new NationalLineupScreen(d.Host, new MainMenuScreen(d.Host)));
        d.Move(300, 235);
        var s = d.Surface;
        bool Text(byte c) => c is 30 or 24 or 9; // Artfarben der Liste
        for (var row = 0; row < NationalCoach.MaxSquad; row++)
        {
            var y = LineupScreen.FirstRowY + row * LineupScreen.RowPitch;
            var dressed = coach.IsDressed(coach.Rows[row]);
            var nr = Count(s, 6, y, 18, y + 5, Text);
            var status = Count(s, 185, y, 219, y + 5, Text);
            Assert.True(dressed ? nr > 0 : nr == 0, $"NR Zeile {row + 1}");
            Assert.True(dressed ? status > 0 : status == 0, $"STATUS Zeile {row + 1}");
        }
        Assert.True(Count(s, 250, 186, 316, 196, c => c == NationalLineupScreen.DressedColor) > 20);
        Assert.Equal(0, Count(s, 250, 186, 316, 196, c => c == Screens.Team.TeamUi.HoverColor));
    }

    private static void ClickSlot(ScreenDriver d, LineupSlot slot, bool right = false)
    {
        var r = LineupScreen.SlotButton(slot);
        if (right)
            d.RightClick(r.X + 5, r.Y + 5);
        else
            d.Click(r.X + 5, r.Y + 5);
    }

    [GameDataFact]
    public void Lineup_Clicks_FollowOriginalRules_AndTacticsBelongToNationalTeam()
    {
        var (d, game) = StartWorldOnly();
        game.AutoNominate();
        var coach = game.NationalCoach!;
        d.OpenTile("Aufstellung");
        var screen = Assert.IsType<NationalLineupScreen>(d.Host.Current);
        d.Frame();

        // Platz, dann Spieler: Torhüter in Zeile 2 ins Tor – eingesetzt, Nummer 2 wie die Zeile.
        ClickSlot(d, LineupSlot.Goalkeeper);
        Assert.Equal(LineupSlot.Goalkeeper, screen.SelectedSlot);
        d.Click(60, RowY(1));
        var keeper = coach.Rows[1];
        Assert.Same(keeper, coach.Formation.Goalkeeper);
        Assert.Equal(2, coach.NumberOf(keeper));
        Assert.Equal(1, coach.DressedCount);

        // Rechtsklick auf einen nicht Eingesetzten setzt ihn ein, Linksklick nimmt ihn wieder heraus.
        d.RightClick(60, RowY(0));
        Assert.True(coach.IsDressed(coach.Rows[0]));
        d.Click(60, RowY(0));
        Assert.False(coach.IsDressed(coach.Rows[0]));

        // Rechtsklick auf den Platz leert ihn; der Torhüter ist danach nicht mehr eingesetzt.
        ClickSlot(d, LineupSlot.Goalkeeper, right: true);
        Assert.Null(coach.Formation.Goalkeeper);
        Assert.Equal(0, coach.DressedCount);

        // 22 eingesetzt: der 23. wird mit „Schon 22 Mann / im Team !“ abgewiesen.
        for (var i = 0; i < NationalCoach.MaxDressed; i++)
            d.RightClick(60, RowY(i));
        Assert.Equal(NationalCoach.MaxDressed, coach.DressedCount);
        d.RightClick(60, RowY(NationalCoach.MaxDressed));
        Assert.Equal(NationalCoach.MaxDressed, coach.DressedCount);
        d.Frame();
        d.Click(10, 10); // Meldung schließen

        // Taktik über den Umschalter: Änderungen treffen das Nationalteam, nicht den Verein.
        var clubTactics = game.Clubs[game.Managers[0].ClubIndex].Tactics;
        d.Click(TeamUi.SwitchButton.X + 5, TeamUi.SwitchButton.Y + 5);
        Assert.IsType<TacticsScreen>(d.Host.Current);
        d.Frame();
        d.Click(TacticsScreen.EffortArrows.X + 2, TacticsScreen.EffortArrows.Y + 2);
        Assert.Equal(TacticMode.Semi, coach.Tactics.EffortMode);
        Assert.Equal(TacticMode.Auto, clubTactics.EffortMode);
        d.Click(TeamUi.SwitchButton.X + 5, TeamUi.SwitchButton.Y + 5);
        Assert.IsType<NationalLineupScreen>(d.Host.Current);
    }
}
