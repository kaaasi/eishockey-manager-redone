using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Screens.Live;
using EishockeyManager.UI.Screens.Team;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur national Teil 4 (A455): Eingriff beim Nationalteam des Bundestrainers im Live-Spiel. [ORIGINAL] Vereinsfeld 0x16 = 6 →
/// 0x5434C mit Manager 5 → 0x48E28 (Datei 0x38FC0–0x38FED, 0x48E2B–0x48E5E): Aufstellung bzw. Taktik des Nationalteams „im Spiel“.
/// Vor dem Teil rot: der Klick auf Kopf/Logo fand keinen Vereinsmanager und öffnete nichts.
/// </summary>
public sealed class NationalTeil4Tests
{
    /// <summary>Modus „nur Weltkugel“ mit einem Manager, nach dem Sprung zur WM ernannt, Kader „auto“.</summary>
    private static (ScreenDriver Driver, GameState Game, NationalCoach Coach) StartWorldCup()
    {
        var d = FlowTests.CreateDriver();
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "TRAINER", Head = 0, ClubIndex = 5, StartLeague = NewGame.DatabaseLeague(5) ?? League.Oberliga });
        if (setup.ClubCompetitions)
            Assert.True(setup.ToggleClubCompetitions());
        if (!setup.EuropeAndWorldCup)
            Assert.True(setup.ToggleEuropeAndWorldCup());
        var game = NewGame.Create(d.Host.Assets.Database, setup, 5);
        Assert.Equal(GameMode.WorldOnly, game.Mode);
        game.Continue();
        game.AutoNominate();
        var coach = Assert.IsType<NationalCoach>(game.NationalCoach);
        Assert.NotEmpty(coach.Squad);
        game.Options.MatchSpeed = 0;
        d.Host.Game = game;
        d.Host.Show(new MainMenuScreen(d.Host));
        d.Frame();
        return (d, game, coach);
    }

    /// <summary>Bis zum laufenden Live-Spiel des Landes des Bundestrainers (ab Minute 5); liefert Bildschirm, Paarung und Seite.</summary>
    private static (LiveMatchScreen Live, LiveMatch Match, int Side) RunToCoachMatch(ScreenDriver d, NationalCoach coach)
    {
        for (var frames = 0; ; frames++)
        {
            Assert.True(frames < 500_000, "kein Live-Spiel des Bundestrainers");
            if (d.Host.Current is MainMenuScreen)
            {
                d.NextDay();
                continue;
            }
            if (d.Host.Current is not LiveMatchScreen live)
            {
                d.Press(Key.Enter);
                continue;
            }
            if (live.WaitingForClick)
            {
                d.Click(160, 120);
                continue;
            }
            var match = live.Session.Shown[^1];
            var side = match.Home.Index == coach.Nation ? 0 : match.Away.Index == coach.Nation ? 1 : -1;
            if (live.Session.Shown.Count != 1 || !match.Running || live.DisplayMinute < 5 || side < 0)
            {
                d.Frame();
                continue;
            }
            return (live, match, side);
        }
    }

    /// <summary>
    /// Linksklick auf Kopf/Logo öffnet die Aufstellung des Nationalteams „im Spiel“ mit Kopfzeile 0x40290 (Land und Bundestrainer),
    /// Wechselknopf zur Taktik des Nationalteams, zurück übernimmt der Spielkader die Aufstellung (ein im Spiel geleerter Platz:
    /// Spieler bleibt eingesetzt, Datei 0x40CFD; Neuzählung 0x38A36), der aktive Manager ist wieder der alte. Rechtsklick öffnet die
    /// Taktik zuerst.
    /// </summary>
    [GameDataFact]
    public void CrestClick_CoachNation_OpensNationalLineupAndTactics_InMatch()
    {
        var (d, game, coach) = StartWorldCup();
        var (live, match, side) = RunToCoachMatch(d, coach);
        var team = side == 0 ? match.HomeTeam! : match.AwayTeam!;
        Assert.True(team.Managed);
        Assert.True(team.Squad!.NationalCoach);
        var before = game.CurrentManager;
        var minute = live.DisplayMinute;

        d.Click(LiveMatchScreen.CrestX(side) + 10, 20);
        Assert.Equal(LiveMatchScreen.ClickResult.Crest, live.LastClick);
        var lineup = Assert.IsType<NationalLineupScreen>(d.Host.Current);
        Assert.True(lineup.InMatch);
        Assert.Equal(coach.Manager, game.CurrentManager);

        // Kopfzeile 0x40290: Land statt Verein des Managers.
        int HeaderPixels(Screen screen)
        {
            screen.Draw(d.Surface);
            return d.Surface.Pixels.Take(320 * 11).Count(b => b == LiveIntervention.HeaderColor);
        }
        Assert.True(HeaderPixels(lineup) > HeaderPixels(new NationalLineupScreen(d.Host, live)) + 20);
        var national = Snapshot(d, lineup);
        var clubHeader = new Surface();
        clubHeader.Palette = d.Surface.Palette;
        LiveIntervention.DrawHeader(clubHeader, d.Host.Assets, game, national: false);
        var nationHeader = new Surface();
        nationHeader.Palette = d.Surface.Palette;
        LiveIntervention.DrawHeader(nationHeader, d.Host.Assets, game, national: true);
        Assert.NotEqual(clubHeader.Pixels.Take(320 * 11), nationHeader.Pixels.Take(320 * 11));
        Assert.Equal(national.Take(320 * 11).Select(b => b == LiveIntervention.HeaderColor),
            nationHeader.Pixels.Take(320 * 11).Select(b => b == LiveIntervention.HeaderColor));

        // Platz im Spiel leeren: Spieler bleibt eingesetzt (Datei 0x40CFD), erst nach der Rückkehr im Spielkader.
        var (slot, taken) = LineupSlot.All.Where(s => s.Role == Formats.PlayerRole.Forward)
            .Select(s => (Slot: s, Player: coach.Formation[s])).First(x => x.Player is not null);
        var livePlayer = team.Squad.Players.Single(p => p.Player == taken);
        Assert.NotEqual(0, livePlayer.Position);
        var r = LineupScreen.SlotButton(slot);
        d.Click(r.X + 2, r.Y + 2);
        Assert.Null(coach.Formation[slot]);
        Assert.True(coach.IsDressed(taken!));
        Assert.NotEqual(0, livePlayer.Position);
        for (var i = 0; lineup.Message.Lines.Count > 0; i++)
        {
            Assert.True(i < 2000);
            d.Frame();
        }

        d.Click(TeamUi.SwitchButton.X + 2, TeamUi.SwitchButton.Y + 2);
        var tactics = Assert.IsType<TacticsScreen>(d.Host.Current);
        Assert.True(tactics.InMatch);
        Assert.Equal(national.Take(320 * 11).Select(b => b == LiveIntervention.HeaderColor),
            Snapshot(d, tactics).Take(320 * 11).Select(b => b == LiveIntervention.HeaderColor));
        d.Press(Key.Escape);
        Assert.Same(live, d.Host.Current);
        Assert.Equal(1, live.Interventions);
        Assert.Equal(before, game.CurrentManager);
        Assert.Equal(0, livePlayer.Position);
        Assert.True(livePlayer.Dressed);
        Assert.Equal(minute, live.DisplayMinute);

        d.RightClick(LiveMatchScreen.CrestX(side) + 10, 20);
        Assert.Equal(InterventionMode.Tactics, live.LastMode);
        Assert.True(Assert.IsType<TacticsScreen>(d.Host.Current).InMatch);
        d.RightClick(5, 5);
        Assert.Same(live, d.Host.Current);
        Assert.Equal(2, live.Interventions);
        Assert.Equal(before, game.CurrentManager);
    }

    private static byte[] Snapshot(ScreenDriver d, Screen screen)
    {
        screen.Draw(d.Surface);
        return d.Surface.Pixels.ToArray();
    }
}
