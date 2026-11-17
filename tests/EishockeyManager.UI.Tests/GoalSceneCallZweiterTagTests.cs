using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Live;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// #862 (Review <c>docs/reviews/2026-11-15-torszenen.md</c> P3-1, P3-2): Kopfzeile der Torszene über die ersten zwei
/// Spieltage. Am ersten Spieltag gibt es weder ein Heimspiel mit Zuschauern ≥ 1000 noch zwei Chancen derselben Minute nach
/// einem Tor; deshalb blieben die Mutationen „Tausenderpunkt an“ (<c>GoalSceneCall.Spectators</c>) und „Spielstand innerhalb
/// der Minute nicht fortschreiben“ (<c>LiveMatchScreen</c>) in <see cref="GoalSceneCallTests"/> grün.
/// </summary>
public sealed class GoalSceneCallZweiterTagTests
{
    private static List<GoalSceneRequest> PlayMatchDays(int count)
    {
        // Runde 21, Spur zufallsstrom Teil 4 (R21-D2 = A, R15-E2 = C): Saat 1994 statt 1993 – Werbung, Börse, Zuschauer, Talentsuche, Jugend und Baufirmen ziehen aus dem gemeinsamen Strom statt aus FinanceRandom (Werbeangebote des Spielstarts Datei 0x2A03B–0x2A061, Börse Datei 0x43AD5), anderer Zufallsverlauf ab dem Spielstart; bei Saat 1993 gibt es in sechs (und bis zu zehn) Spieltagen keinen Fall „Chance nach Tor in derselben Minute“, 1994–1999 erfüllen alle Proben der Klasse.
        var d = FlowTests.CreateDriver(1994);
        d.StartNewGame();
        var game = d.Host.Game!;
        game.Options.MatchSpeed = 60;
        game.Options.LiveMode = 1;
        var requests = new List<GoalSceneRequest>();
        var days = 0;
        for (var guard = 0; days < count; guard++)
        {
            Assert.True(guard < 20, $"{count} Spieltage nicht erreicht");
            d.NextDay();
            LiveMatchScreen? live = null;
            var frames = 0;
            while (d.Host.Current is not MainMenuScreen)
            {
                Assert.True(++frames < 400_000, "Live-Spiel endet nicht");
                switch (d.Host.Current)
                {
                    case LiveMatchScreen l:
                        live = l;
                        if (l.WaitingForClick)
                            d.Click(160, 120);
                        else
                            d.Frame();
                        break;
                    case GoalSceneScreen:
                        d.Frame();
                        break;
                    default:
                        d.Press(Key.Enter);
                        break;
                }
            }
            if (live is null)
                continue;
            days++;
            // Kopfzeile = Stand vor der Chance: Tore steigen nur um die Tore der früheren Szenen der Paarung.
            foreach (var group in live.ScenesShown.GroupBy(r => (r.Header.HomeName, r.Header.AwayName)))
            {
                var (home, away) = (0, 0);
                foreach (var r in group)
                {
                    Assert.Equal((home, away), (r.Header.HomeGoals, r.Header.AwayGoals));
                    if (r.Goal)
                        (home, away) = r.Side == 0 ? (home + 1, away) : (home, away + 1);
                }
            }
            requests.AddRange(live.ScenesShown);
        }
        return requests;
    }

    /// <summary>
    /// P3-1: Heimspiel des Managers mit mindestens 1000 Zuschauern → Zahl ohne Tausenderpunkt ([ORIGINAL] 0x0BAB2 mit
    /// <c>DGROUP 0x38D</c> = 0, Datei 0x53A9C; Video <c>eisbaeren_f01_01150</c>, siehe <see cref="GoalSceneCall.Spectators"/>).
    /// P3-2: mindestens eine Chance in derselben Minute nach einem Tor, deren Kopfzeile das Tor schon zählt.
    /// Runde 20, Spur pruefung-karte Teil 2 (#872, R15-E2 = C): drei statt zwei Spieltage – mit den Reihen aus dem Vorspiel
    /// (0x2E/0x30, Datei 0x84EB–0x857D) verläuft Spieltag 2 anders und enthält keinen solchen Fall mehr; der Name bleibt.
    /// </summary>
    [GameDataFact]
    public void TwoMatchDays_SpectatorsWithoutThousandsDot_ScoreWithinMinute()
    {
        // Runde 21, Spur zufallsstrom Teil 1 (R21-D2 = A): sechs statt drei Spieltage – Pool (Datei 0x29896), Starttendenz (Datei 0x42E6B) und der Tageslauf 0x07169 (Datei 0x07402–0x07AEF) ziehen aus dem gemeinsamen Strom, anderer Zufallsverlauf;
        // der erste Fall „Chance nach Tor in derselben Minute“ fällt jetzt am sechsten Spieltag (3–5: keiner).
        var requests = PlayMatchDays(6);

        var spectators = requests.Select(r => r.Header.SpectatorText).Where(t => t.EndsWith(GoalSceneCall.SpectatorSuffix)).ToList();
        Assert.Contains(spectators, t => int.TryParse(t[..^GoalSceneCall.SpectatorSuffix.Length], out var n) && n >= 1000);
        Assert.All(spectators, t => Assert.DoesNotContain(".", t));

        var withinMinute = requests.Zip(requests.Skip(1)).Count(p =>
            p.First.Goal && p.First.Header.Minute == p.Second.Header.Minute
            && (p.First.Header.HomeName, p.First.Header.AwayName) == (p.Second.Header.HomeName, p.Second.Header.AwayName));
        Assert.True(withinMinute > 0, "Kein Fall „Chance nach Tor in derselben Minute“ in sechs Spieltagen");
    }
}

/// <summary>
/// #857: Trikotfarbe der Torszene nach einem Platztausch = Farbcode des eingetauschten Vereins ([ORIGINAL] 0x36169 tauscht
/// Vereinsfeld +0x3B mit, Datei 0x3628F–0x363F6); vorher nahm <see cref="GoalSceneCall.ColorCode"/> den Satz des Platzes.
/// </summary>
public sealed class GoalSceneCallKennungTests
{
    [GameDataFact]
    public void ColorCode_FollowsClubIdentity()
    {
        var d = FlowTests.CreateDriver();
        var db = d.Host.Assets.Database;
        var (place, identity) = Enumerable.Range(0, 36).SelectMany(a => Enumerable.Range(0, 36), (a, b) => (a, b))
            .First(p => db[p.a].ColorCode != db[p.b].ColorCode);
        var moved = new Club { Index = place, Name = db[identity].Name, Strength = db[identity].Strength };
        Assert.Equal(db[identity].ColorCode, GoalSceneCall.ColorCode(d.Host.Assets, moved));
        var stays = new Club { Index = place, Name = db[place].Name, Strength = db[place].Strength };
        Assert.Equal(db[place].ColorCode, GoalSceneCall.ColorCode(d.Host.Assets, stays));
    }
}
