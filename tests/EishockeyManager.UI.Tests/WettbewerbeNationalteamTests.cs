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

        d.Click(290, 50); // Vereinslogo = Tag weiter
        Assert.Equal(warning, menu.Message.Visible);
        if (warning)
        {
            Assert.Same(menu, d.Host.Current);
            Assert.Equal(before, StateFingerprint.Of(game));
        }
        else
            Assert.NotSame(menu, d.Host.Current);
    }
}
