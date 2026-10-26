using EishockeyManager.Tests.Support;
using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Levelumfang und Regler der Spielvorbereitung gegen die Originalbytes (Befund „Levelgrenze“, Folgereview 26.10.2026).
/// </summary>
public class SetupLevelTests
{
    /// <summary>DGROUP (Segment 0x14FD) beginnt in em.exe bei Datei 0x1E00 + 0x14FD0.</summary>
    private const int DgroupFile = 0x1E00 + 0x14FD0;

    private static byte[] ReadExe() => File.ReadAllBytes(Path.Combine(TestGameData.Load().RootDirectory, "em.exe"));

    [Fact]
    public void LevelRange_IsZeroToFive_DefaultThree()
    {
        Assert.Equal(6, ManagerSetup.LevelCount);
        Assert.Equal(3, new ManagerSetup().Level);
    }

    /// <summary>Höchstwerte DGROUP 0x11C und Vorgaben 0x118 (übersprungen ohne Originaldateien).</summary>
    [GameDataFact]
    public void SliderMaxima_MatchEmExe()
    {
        var exe = ReadExe();

        Assert.Equal(new byte[] { 5, 3, 2, 2 }, exe[(DgroupFile + 0x11C)..(DgroupFile + 0x120)]);
        Assert.Equal(ManagerSetup.LevelCount - 1, exe[DgroupFile + 0x11C]);
        Assert.Equal(exe[DgroupFile + 0x118], new ManagerSetup().Level);
        Assert.Equal(exe[DgroupFile + 0x119], (int)new GameSetup().Duration);
        Assert.Equal(exe[DgroupFile + 0x11B], (int)new ManagerSetup().StartStrength);
    }

    /// <summary>Erster Streifen je Regler in kb\1, DGROUP 0x4B3E (übersprungen ohne Originaldateien).</summary>
    [GameDataFact]
    public void SliderStrips_MatchEmExe()
    {
        var exe = ReadExe();

        Assert.Equal(new byte[] { 0, 6, 10, 13 }, exe[(DgroupFile + 0x4B3E)..(DgroupFile + 0x4B42)]);
        foreach (var slider in Enum.GetValues<SetupSlider>())
            Assert.Equal(exe[DgroupFile + 0x4B3E + (int)slider], SetupSliders.Strip(slider, 0));
    }

    [Fact]
    public void Maxima_AreFiveThreeTwoTwo()
    {
        Assert.Equal(5, SetupSliders.Maximum(SetupSlider.Level));
        Assert.Equal(3, SetupSliders.Maximum(SetupSlider.Duration));
        Assert.Equal(2, SetupSliders.Maximum(SetupSlider.StartLeague));
        Assert.Equal(2, SetupSliders.Maximum(SetupSlider.StartStrength));
        Assert.Equal(ManagerSetup.LevelCount - 1, SetupSliders.Maximum(SetupSlider.Level));
    }

    [Theory]
    [InlineData(3, +1, 4)]
    [InlineData(4, +1, 5)]
    [InlineData(5, +1, 5)] // kein Umlauf nach 0
    [InlineData(0, -1, 0)] // kein Umlauf nach 5
    [InlineData(1, -1, 0)]
    public void Step_ClampsLevelWithoutWrap(int value, int delta, int expected) =>
        Assert.Equal(expected, SetupSliders.Step(SetupSlider.Level, value, delta));

    [Theory]
    [InlineData(SetupSlider.Duration, 3)]
    [InlineData(SetupSlider.StartLeague, 2)]
    [InlineData(SetupSlider.StartStrength, 2)]
    public void Step_ClampsOtherSliders(SetupSlider slider, int max)
    {
        Assert.Equal(max, SetupSliders.Step(slider, max, +1));
        Assert.Equal(0, SetupSliders.Step(slider, 0, -1));
    }

    [Fact]
    public void Strips_FollowEachOther()
    {
        // Jeder Regler belegt so viele 10-px-Streifen in kb\1, wie er Werte hat.
        Assert.Equal(SetupSliders.Strip(SetupSlider.Duration, 0), SetupSliders.Strip(SetupSlider.Level, 5) + 1);
        Assert.Equal(SetupSliders.Strip(SetupSlider.StartLeague, 0), SetupSliders.Strip(SetupSlider.Duration, 3) + 1);
        Assert.Equal(SetupSliders.Strip(SetupSlider.StartStrength, 0), SetupSliders.Strip(SetupSlider.StartLeague, 2) + 1);
    }

    /// <summary>Level 5 ist jetzt erreichbar: Spielstart übernimmt ihn, das Training ist dann immer frei (46E:0653).</summary>
    [Fact]
    public void Level5_ReachesGameAndTraining()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", ClubIndex = 0, Level = SetupSliders.Maximum(SetupSlider.Level) });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 3);

        Assert.Equal(5, game.Managers[0].Level);
        var plan = game.PrepareTraining(game.Managers[0]);
        for (var day = 0; day < TrainingPlan.Days; day++)
            for (var unit = 0; unit < TrainingPlan.UnitsPerDay; unit++)
                Assert.Equal(TrainingKind.Free, plan[day, unit]);
    }
}
