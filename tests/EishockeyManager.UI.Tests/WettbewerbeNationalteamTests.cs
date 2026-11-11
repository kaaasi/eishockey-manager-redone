using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur wettbewerbe-komplett Teil 2 (Nationalteam), UI-Teil.
/// </summary>
public class WettbewerbeNationalteamTests
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
        setup.Managers.Add(new ManagerSetup { Name = "B", Head = 1, ClubIndex = 2 });
        return NewGame.Create(Formats.ClubDatabase.Parse(data), setup, 3);
    }

    private static void PlayToNationalTurn(GameState game)
    {
        for (var guard = 0; !game.NationalTurn; guard++)
        {
            Assert.True(guard < 3000, "Kein Zug des Bundestrainers");
            foreach (var m in game.Managers)
                m.Money = Math.Max(m.Money, 10_000_000);
            game.Continue();
        }
    }

    /// <summary>
    /// R8-D02: Grenzwert der Bundestrainer-Warnung. [ORIGINAL] Datei 0x454C2–0x454D8: Bundestrainer am Zug (DGROUP 0xA70E = 5), Kaderzahl aus 0x120:0x66, `cmp al,0xA; jnc` – ab 10 Spielern
    /// geht der Tag weiter, mit 9 erscheint die Warnung. Fängt die Mutation `&lt; CoachMinPlayers − 1` (9 bliebe ohne Warnung)
    /// und `&lt;= CoachMinPlayers` (10 bekäme eine Warnung).
    /// </summary>
    [GameDataTheory]
    [InlineData(9, true)]
    [InlineData(10, false)]
    public void CoachWarning_Boundary(int players, bool warning)
    {
        var game = Synthetic();
        // Integration Runde 9: Bundestrainer ausdrücklich ernennen. Vorher hing die Ernennung am Tabellenplatz (Top 5 am
        // Markierungstag, CupsNationalMode.OnNationalMarkerDay) und damit am Zufallsverbrauch; mit dem Spielerpool
        // (transfer-vertraege-komplett Teil 5) endete die Saison ohne Bundestrainer-Zug. Geprüft wird hier nur die Warnung.
        game.AppointNationalCoach(game.Managers[0]);
        PlayToNationalTurn(game);
        game.AutoNominate();
        var coach = game.NationalCoach!;
        while (coach.Squad.Count > players)
            game.ToggleNomination(coach.Squad[^1].Player);
        Assert.Equal(players, coach.Squad.Count);

        var d = FlowTests.CreateDriver();
        d.Host.Game = game;
        var menu = new MainMenuScreen(d.Host);
        d.Host.Show(menu);
        d.Frame();
        var before = StateFingerprint.Of(game);
        var day = game.CurrentDay;

        d.Click(290, 50); // Vereinslogo = Tag weiter
        Assert.Equal(warning, menu.CoachMessage is not null);
        if (warning)
        {
            Assert.Same(menu, d.Host.Current);
            Assert.Equal(before, StateFingerprint.Of(game));
        }
        else
        {
            // Spur nationalteam Runde 13 (A028): Der Bundestrainer ist an jedem WM-Tag am Zug, auch wenn Deutschland nicht
            // spielt („Spielfrei“); dann gibt es keine Spiele zu zeigen und das Menü kommt sofort wieder – geprüft wird der Tag.
            Assert.True(game.CurrentDay > day);
            // #620 (R13GN-03): Halt am nächsten WM-Tag, ob Deutschland spielt oder nicht (A028) – vorher blieb der Zweig auch
            // mit der alten Regel „nur an Spieltagen Deutschlands“ grün.
            var next = Enumerable.Range(day + 1, SeasonCalendar.SeasonEnd - day)
                .First(t => SeasonCalendar.Events(t).HasFlag(DayEvents.WorldCup));
            Assert.Equal(next, game.CurrentDay);

            // #703 R701-02: Der nächste WM-Tag ist hier ein Spieltag Deutschlands; die Zeile darüber bliebe auch mit der alten
            // Regel grün. Deshalb den Zug des Bundestrainers selbst prüfen: am Spieltag Deutschlands und am ersten WM-Tag, an dem
            // Deutschland spielfrei ist (nach dem Ausscheiden). Gegenprobe NationalCoachPlaysOn „… && MatchOn(day,
            // NationalCoach.Nation) is not null“ → rot (die Zeitleiste läuft über die spielfreien WM-Tage bis zum Saisonende);
            // ebenso die Umkehrung „… is null“ (kein Zug am Spieltag Deutschlands). A028 deckt auch
            // NationalteamRunde13Teil2Tests.A028_CoachHaltsOnEveryWorldCupDay.
            var nation = coach.Nation;
            bool FreeWorldCupDay(int t) => SeasonCalendar.Events(t).HasFlag(DayEvents.WorldCup) && game.MatchOn(t, nation) is null;
            void AssertCoachTurnToday()
            {
                var today = game.CurrentDay;
                for (var guard = 0; !game.NationalTurn && game.CurrentDay == today; guard++)
                {
                    Assert.True(guard < 10, "Zug des Bundestrainers bleibt aus");
                    game.Continue(); // Züge der Vereinsmanager an diesem Tag
                }
                Assert.True(game.NationalTurn, $"Bundestrainer am WM-Tag {today} nicht am Zug");
                Assert.Equal(today, game.CurrentDay);
            }
            Assert.NotNull(game.MatchOn(next, nation));
            AssertCoachTurnToday();
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver && !FreeWorldCupDay(game.CurrentDay); guard++)
            {
                Assert.True(guard < 200, "kein spielfreier WM-Tag erreicht");
                game.Continue();
            }
            Assert.Equal(SeasonPhase.ClubSeasonOver, game.Phase); // WM nach der Vereinssaison
            Assert.True(FreeWorldCupDay(game.CurrentDay), $"Tag {game.CurrentDay} ist kein spielfreier WM-Tag");
            AssertCoachTurnToday();
        }
    }
}
