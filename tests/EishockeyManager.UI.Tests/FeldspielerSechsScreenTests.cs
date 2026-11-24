using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Screens.Team;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur <c>feldspieler-sechs</c>, Runde 23, Teil 3 (#939, A937/A875): Aufstellungsbilder mit offenem sechstem Feldspielerplatz.
/// [ORIGINAL] Die Abwehrhälfte der Reihe 1 hat dann drei Knöpfe auf den x-Werten des Sturms (Datei 0x40681–0x4068D, 0x413E8–0x414B4;
/// <see cref="LineupScreen.SlotButton(LineupSlot, bool)"/>), beim Verein wie beim Nationalteam (gleicher Bildschirm 0x40364).
/// Ein Videobild mit offenem Platz gibt es nicht (<c>docs/referenz-videos.md</c>); geprüft werden Knopflage und Klick.
/// </summary>
public sealed class FeldspielerSechsScreenTests
{
    private static Rect Sixth => LineupScreen.SlotButton(LineupSlot.SixthSkater, sixthOpen: true);

    private static byte[] Region(ScreenDriver d, Rect r)
    {
        var px = d.Surface.Pixels.ToArray();
        var list = new List<byte>();
        for (var y = r.Y; y < r.Y + r.H; y++)
            for (var x = r.X; x < r.X + r.W; x++)
                list.Add(px[y * 320 + x]);
        return [.. list];
    }

    /// <summary>
    /// Nationalaufstellung (A875): offen zeichnet der Bildschirm den sechsten Knopf, ein Klick darauf leert den sechsten Platz (nicht den
    /// zweiten Verteidigerplatz, dessen geschlossener Knopf dort läge). Vorher: kein Knopf, der Klick traf Verteidiger 2.
    /// </summary>
    [GameDataFact]
    public void NationalLineup_SixthOpen_DrawsAndClicksThreeDefenceButtons()
    {
        var d = FlowTests.CreateDriver();
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "TRAINER", Head = 0, ClubIndex = 5, StartLeague = NewGame.DatabaseLeague(5) ?? League.Oberliga });
        if (setup.ClubCompetitions)
            Assert.True(setup.ToggleClubCompetitions());
        if (!setup.EuropeAndWorldCup)
            Assert.True(setup.ToggleEuropeAndWorldCup());
        var game = NewGame.Create(d.Host.Assets.Database, setup, 5);
        game.Continue();
        game.AutoNominate();
        var coach = Assert.IsType<NationalCoach>(game.NationalCoach);
        coach.AutoFormation();
        coach.ClearSlot(LineupSlot.Goalkeeper);
        d.Host.Game = game;
        d.Host.Show(new NationalLineupScreen(d.Host, new MainMenuScreen(d.Host)));
        d.Frame();
        var closed = Region(d, Sixth);

        int? line = 0;
        var sixth = coach.Squad.Select(m => m.Player).First(p => p.Role == PlayerRole.Defender && p.Available && !coach.Formation.Contains(p));
        Assert.Equal(LineupResult.Assigned, coach.ClickPlayer(sixth, ref line, right: false));
        var second = coach.Formation[LineupSlot.Defender(0, 1)];
        d.Frame();
        Assert.NotEqual(closed, Region(d, Sixth));

        d.Click(Sixth.X + Sixth.W / 2, Sixth.Y + Sixth.H / 2);
        Assert.Null(coach.Formation.SixthSkater);
        Assert.Same(second, coach.Formation[LineupSlot.Defender(0, 1)]);
    }

    /// <summary>Vereinsaufstellung (A937): dieselbe Knopflage und derselbe Klick wie beim Nationalteam.</summary>
    [GameDataFact]
    public void ClubLineup_SixthOpen_DrawsAndClicksThreeDefenceButtons()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var club = TeamUi.OwnClub(d.Host);
        club.AutoLineupEnabled = false;
        club.ClearLineupSlot(LineupSlot.Goalkeeper);
        d.Host.Show(new LineupScreen(d.Host, new MainMenuScreen(d.Host)));
        d.Frame();
        var closed = Region(d, Sixth);

        int? line = 0;
        var sixth = club.Players.First(p => p.Role == PlayerRole.Defender && p.Available && !club.Lineup.Contains(p));
        Assert.Equal(LineupResult.Assigned, club.ClickPlayer(sixth, ref line, right: false));
        var second = club.Lineup[LineupSlot.Defender(0, 1)];
        // A971: Das Kennzeichen ist Bildschirmzustand und entsteht beim Aufbau (Datei 0x403DB–0x404FD); der Klick oben lief am
        // Bildschirm vorbei, also neu aufbauen.
        d.Host.Show(new LineupScreen(d.Host, new MainMenuScreen(d.Host)));
        d.Frame();
        Assert.NotEqual(closed, Region(d, Sixth));

        d.Click(Sixth.X + Sixth.W / 2, Sixth.Y + Sixth.H / 2);
        Assert.Null(club.Lineup.SixthSkater);
        Assert.Same(second, club.Lineup[LineupSlot.Defender(0, 1)]);
    }
}
