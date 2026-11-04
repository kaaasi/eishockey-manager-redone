namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur <c>kader-start</c>, Runde 13, Teil 2: Regler der Spielvorbereitung (U15-b) und Start-Liga für alle Manager (L1).
/// Belege: <c>docs/analyse/re-runde-13.md</c>, Abschnitt U15-b; em.exe Datei 0x2890A–0x2891C.
/// </summary>
public class KaderStartRunde13Teil2Tests
{
    private static GameSetup Setup(int managers = 1)
    {
        var setup = new GameSetup();
        for (var i = 0; i < managers; i++)
            setup.Managers.Add(new ManagerSetup { Name = $"M{i}", Head = i });
        return setup;
    }

    /// <summary>
    /// U15-b, Bild <c>file28_4</c>/<c>file28_5</c>: Mit 2. Liga oder Oberliga bleibt der Dauer-Regler auf „drei Saisons“
    /// stehen; der Schritt auf „eine Saison“ wird zurückgenommen. Vorher sprang die Dauer auf „eine Saison“.
    /// </summary>
    [Theory]
    [InlineData(League.ZweiteLiga)]
    [InlineData(League.Oberliga)]
    public void Duration_OtherStartLeague_StopsAtThreeSeasons(League league)
    {
        var setup = Setup();
        setup.StartLeague = league;
        setup.Duration = GameDuration.DreiSaisons;

        Assert.False(setup.Step(SetupSlider.Duration, -1, setup.Managers[0]));

        Assert.Equal(GameDuration.DreiSaisons, setup.Duration);
        Assert.Equal(league, setup.StartLeague);
    }

    /// <summary>
    /// U15-b, Bild <c>file28_3</c>: Mit „eine Saison“ bleibt die Start-Liga auf Bundesliga stehen (kein Ersatz-Umstellen der
    /// Dauer). Vorher ließ sich die Liga frei verstellen.
    /// </summary>
    [Fact]
    public void StartLeague_OneSeason_StaysBundesliga()
    {
        var setup = Setup();
        setup.Duration = GameDuration.EineSaison;

        Assert.False(setup.Step(SetupSlider.StartLeague, +1, setup.Managers[0]));
        Assert.False(setup.Step(SetupSlider.StartLeague, +1, setup.Managers[0]));

        Assert.Equal(League.Bundesliga, setup.StartLeague);
        Assert.Equal(GameDuration.EineSaison, setup.Duration);
    }

    /// <summary>Gegenprobe: Mit Bundesliga ist „eine Saison“ wählbar, mit „drei Saisons“ jede Liga; Begrenzung wie bisher.</summary>
    [Fact]
    public void Bundesliga_OneSeasonAllowed_LongerDurationAnyLeague()
    {
        var setup = Setup();
        for (var i = 0; i < 5; i++)
            setup.Step(SetupSlider.Duration, -1, setup.Managers[0]);
        Assert.Equal(GameDuration.EineSaison, setup.Duration);

        Assert.True(setup.Step(SetupSlider.Duration, +1, setup.Managers[0]));
        Assert.True(setup.Step(SetupSlider.StartLeague, +1, setup.Managers[0]));
        Assert.True(setup.Step(SetupSlider.StartLeague, +1, setup.Managers[0]));
        Assert.False(setup.Step(SetupSlider.StartLeague, +1, setup.Managers[0])); // Höchstwert 2, kein Umlauf
        Assert.Equal(League.Oberliga, setup.StartLeague);
        Assert.Equal(GameDuration.DreiSaisons, setup.Duration);
    }

    /// <summary>Level und Start-Stärke sind von der Sperre nicht betroffen und brauchen einen Manager.</summary>
    [Fact]
    public void LevelAndStrength_UnaffectedByLock_NeedManager()
    {
        var setup = Setup();
        setup.Duration = GameDuration.EineSaison;
        var m = setup.Managers[0];

        Assert.True(setup.Step(SetupSlider.Level, +1, m));
        Assert.True(setup.Step(SetupSlider.StartStrength, +1, m));
        Assert.Equal(4, m.Level);
        Assert.Equal(StartStrength.Abstiegskampf, m.StartStrength);
        Assert.False(setup.Step(SetupSlider.Level, +1, null));
    }

    /// <summary>
    /// L1 (#567): Die Start-Liga ist ein Wert für alle Manager (DGROUP 0x11A). Verstellen wirkt auf alle, die Sperre prüft
    /// diesen einen Wert. Vorher hatte jeder Manager eine eigene Start-Liga.
    /// </summary>
    [Fact]
    public void StartLeague_SharedByAllManagers()
    {
        var setup = Setup(managers: 3);

        Assert.True(setup.Step(SetupSlider.StartLeague, +1, setup.Managers[2]));

        Assert.All(setup.Managers, m => Assert.Equal(League.ZweiteLiga, m.StartLeague));
        Assert.Equal(League.ZweiteLiga, setup.StartLeague);
        Assert.Equal(League.Bundesliga, new GameSetup().StartLeague); // ohne Manager: Vorgabe 0
    }
}
