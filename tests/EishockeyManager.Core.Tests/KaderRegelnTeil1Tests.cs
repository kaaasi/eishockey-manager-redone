using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur <c>kader-regeln</c>, Runde 22, Teil 1: Spiel-Level und Start-Stärke sind ein Wert je Spiel, nicht je Manager
/// (A054, A518, A060, A462). [ORIGINAL] Setup-Werte DGROUP 0x118–0x11B, em.exe Datei 0x2890A <c>add [bx+0x118], al</c> (ein
/// Befehl für alle vier Regler, unabhängig vom Manager), Datei 0x2988D–0x29893 <c>0xED8 = 5 − [0x118]</c>.
/// </summary>
public class KaderRegelnTeil1Tests
{
    private static GameSetup TwoManagers()
    {
        var setup = new GameSetup();
        setup.AddManager(new ManagerSetup { Name = "A", Head = 0 });
        setup.AddManager(new ManagerSetup { Name = "B", Head = 1 });
        return setup;
    }

    /// <summary>
    /// A054/A518: Der Regler wirkt auf alle Manager, auch wenn der zweite gewählt ist. Vorher änderte <c>Step</c> nur den gewählten
    /// Manager (Level 5 bei B, 3 bei A).
    /// </summary>
    [Theory]
    [InlineData(SetupSlider.Level)]
    [InlineData(SetupSlider.StartStrength)]
    public void Step_WithSecondManagerSelected_ChangesAllManagers(SetupSlider slider)
    {
        var setup = TwoManagers();

        Assert.True(setup.Step(slider, +1, setup.Managers[1]));

        var expected = slider == SetupSlider.Level ? (4, StartStrength.Mittelfeld) : (3, StartStrength.Abstiegskampf);
        Assert.All(setup.Managers, m => Assert.Equal(expected, (m.Level, m.StartStrength)));
    }

    /// <summary>
    /// A518: Wie die Start-Liga (R13KI-03) lassen sich Level und Start-Stärke schon vor dem ersten Manager einstellen; ein danach
    /// angelegter Manager übernimmt sie. Vorher: ohne Manager kein Schritt (<c>false</c>).
    /// </summary>
    [Fact]
    public void Step_WithoutManagers_IsKept_AndAdoptedByNewManagers()
    {
        var setup = new GameSetup();

        Assert.True(setup.Step(SetupSlider.Level, +2, null));
        Assert.True(setup.Step(SetupSlider.StartStrength, -1, null));
        Assert.Equal(5, setup.Level);
        Assert.Equal(StartStrength.OberesDrittel, setup.StartStrength);
        Assert.False(setup.Step(SetupSlider.Level, +1, null)); // [ORIGINAL] Höchstwert DGROUP 0x11C = 5

        var a = setup.AddManager(new ManagerSetup { Name = "A", Head = 0, Level = 0 });
        var b = setup.AddManager(new ManagerSetup { Name = "B", Head = 1, StartStrength = StartStrength.Abstiegskampf });
        Assert.All(new[] { a, b }, m => Assert.Equal((5, StartStrength.OberesDrittel), (m.Level, m.StartStrength)));

        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 1);
        Assert.All(game.Managers, m => Assert.Equal(5, m.Level));
    }

    /// <summary>Entfernt man alle Manager, bleiben Level und Start-Stärke erhalten (ein Wert für das Spiel).</summary>
    [Fact]
    public void Values_SurviveRemovalOfAllManagers()
    {
        var setup = TwoManagers();
        Assert.True(setup.Step(SetupSlider.Level, -3, setup.Managers[1]));
        Assert.True(setup.Step(SetupSlider.StartStrength, +1, setup.Managers[0]));
        setup.Managers.Clear();

        Assert.Equal((0, StartStrength.Abstiegskampf), (setup.Level, setup.StartStrength));
    }

    /// <summary>
    /// A060: Der Highscore-Schlüssel folgt dem Wert des Spiels. Vorher blieb er beim Verstellen mit gewähltem zweiten Manager auf dem
    /// Level von Manager 0 stehen (Schlüssel 1 + 4 · 3 + 32 · 0 + 128 · 1 − 2 = 139 statt 140).
    /// </summary>
    [Fact]
    public void HighscoreKey_FollowsGameWideLevel()
    {
        var setup = TwoManagers();
        Assert.Equal(3 + 4 * 3 + 128 - 2, HighscoreRules.Key(setup));

        Assert.True(setup.Step(SetupSlider.Level, +1, setup.Managers[1]));

        Assert.Equal(4 + 4 * 3 + 128 - 2, HighscoreRules.Key(setup));
    }

    /// <summary>A462: Ein im laufenden Spiel aufgenommener Manager übernimmt Level und Start-Stärke des Spiels.</summary>
    [Fact]
    public void AddManager_InRunningGame_TakesGameWideValues()
    {
        var setup = new GameSetup();
        setup.Step(SetupSlider.Level, +2, null);
        setup.Step(SetupSlider.StartStrength, -1, null);
        setup.AddManager(new ManagerSetup { Name = "A", Head = 0, ClubIndex = 2 });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 1);

        var added = game.AddManager(new ManagerSetup { Name = "B", Head = 1, Level = 0, StartStrength = StartStrength.Abstiegskampf });

        Assert.Equal(5, added.Level);
        Assert.Equal((5, StartStrength.OberesDrittel), (game.Setup.Managers[1].Level, game.Setup.Managers[1].StartStrength));
    }
}
