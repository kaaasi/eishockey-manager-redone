using EishockeyManager.Core;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Live;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>
/// Spur torszene-dauer, Teil 2 (A472, <c>docs/analyse/torszene-dauer.md</c> §10): Darstellung 2 über einen DEB-Pokaltag der
/// Managerpaarung mit Toren und Penaltyschießen. Bilder: Eis nach dem Aufbau (Minute 1), stehendes Eis nach der ersten Szene,
/// Ankündigung des Penaltyschießens, Eis des Penaltyschießens nach dem ersten Schuss, Endergebnis. Der Gleichstand vor
/// Minute 60 ist gesetzt (Aufbau für das Penaltyschießen); es gibt kein Videobild mit Darstellung 2 (Index
/// <c>docs/referenz-videos.md</c>), Vergleich gegen die Einzelszene.
/// </summary>
internal static class TorszeneDauer
{
    [Scenario("torszene-dauer")]
    private static void Run(ScreenDriver d)
    {
        for (var seed = 1; seed <= 16; seed++)
        {
            if (TryRun(d, seed))
                return;
        }
        throw new InvalidOperationException("Kein Penaltyschießen der Managerpaarung entstanden.");
    }

    private static bool TryRun(ScreenDriver d, int seed)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "M0", Head = 0, ClubIndex = 2 });
        var game = Core.NewGame.Create(d.Host.Assets.Database, setup, seed);
        game.Options.MatchSpeed = 60;
        game.Options.LiveMode = GameOptions.PermanentRinkLiveMode;
        game.Options.ShowDebCup = false;
        d.Host.Game = game;
        d.Host.Show(new MainMenuScreen(d.Host));
        d.Frame();
        LiveMatchScreen? live = null;
        for (var guard = 0; guard < 20000 && live is null; guard++)
        {
            if (d.Host.Current is MainMenuScreen)
                d.NextDay();
            else if (d.Host.Current is LiveMatchScreen s && s.Session.Day?.Matches[0].Competition == MatchCompetition.DebCup)
                live = s;
            else if (d.Host.Current is LiveMatchScreen)
                d.Press(UI.Key.Escape);
            else
                d.Frame();
        }
        if (live is null)
            return false;
        var match = live.Session.Shown[0];
        var saved = new HashSet<string>();
        void Save(string name)
        {
            if (saved.Add(name))
                d.Save($"{seed:00}_{name}");
        }
        LivePenaltyScreen? last = null;
        for (var i = 0; i < 400000 && !live.Done; i++)
        {
            if (live.Session.Day is { Minute: >= 50 and < 60 })
                match.Report.AwayGoals = match.Report.HomeGoals;
            if (d.Host.Current == live)
            {
                if (live.DisplayMinute == 1)
                    Save("01_aufbau");
                if (live.Rink is { Scenes: 1, Minute: < 70 })
                    Save("02_nach_szene");
                if (live.Rink is { Scenes: 1, Minute: 70 } && live.RinksBuilt.Count == 2)
                    Save("04_penalty_eis");
                d.Frame();
            }
            else if (d.Host.Current is LivePenaltyScreen p)
            {
                if (!ReferenceEquals(p, last))
                    Save(p.Result is null ? "03_ankuendigung" : "05_endergebnis");
                last = p;
                d.Click(160, 120);
            }
            else if (d.Host.Current is GoalSceneScreen)
                d.Frame();
            else
                d.RightClick(5, 5);
        }
        return match.Report.ShootoutGoals is not null;
    }
}
