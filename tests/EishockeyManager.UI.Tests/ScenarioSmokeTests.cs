using EishockeyManager.Tests.Support;
using EishockeyManager.Tools.Scenarios;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Rauchtest der Bildschirmfoto-Abläufe (<c>emtool screens</c>, Aufräumkonzept U14/A5 = A): Jeder Ablauf läuft einmal bis zum
/// Ende durch und schreibt mindestens ein Bild. So fällt ein gebrochener Ablauf sofort auf (auf <c>69e9c40</c> waren es zwei).
/// Braucht die Originaldateien, läuft also nur im Lauf mit Daten.
/// </summary>
public class ScenarioSmokeTests
{
    public static IEnumerable<object[]> Names => ScenarioRunner.All.Keys.Select(k => new object[] { k });

    [GameDataTheory]
    [MemberData(nameof(Names))]
    public void Scenario_RunsAndWritesImages(string name)
    {
        var target = Path.Combine(Path.GetTempPath(), "em-smoke-" + Guid.NewGuid().ToString("N"));
        try
        {
            ScenarioRunner.Run(TestGameData.Load(), target, [name]);
            var folder = Path.Combine(target, name);
            Assert.True(Directory.Exists(folder) && Directory.EnumerateFiles(folder, "*.png").Any(),
                $"Ablauf \"{name}\" schrieb kein Bild.");
        }
        finally
        {
            if (Directory.Exists(target)) Directory.Delete(target, recursive: true);
        }
    }
}
