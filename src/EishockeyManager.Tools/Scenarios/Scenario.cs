using System.Reflection;
using EishockeyManager.Formats;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>
/// Testablauf für <c>emtool screens</c>: spielt Eingaben durch und speichert Bildschirmfotos.
/// Markierte Methode: <c>static void Name(ScreenDriver d)</c>. Jedes Arbeitspaket legt seine Abläufe
/// in einer eigenen Datei in diesem Ordner an.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ScenarioAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}

public static class ScenarioRunner
{
    public static IReadOnlyDictionary<string, Action<ScreenDriver>> All { get; } = typeof(ScenarioRunner).Assembly
        .GetTypes()
        .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        .Select(m => (Method: m, Attribute: m.GetCustomAttribute<ScenarioAttribute>()))
        .Where(x => x.Attribute is not null)
        .OrderBy(x => x.Attribute!.Name)
        .ToDictionary(x => x.Attribute!.Name, x => x.Method.CreateDelegate<Action<ScreenDriver>>());

    /// <summary>Führt die genannten Abläufe aus (ohne Namen: alle). Bilder landen in Unterordnern je Ablauf.</summary>
    public static void Run(GameData game, string target, IReadOnlyCollection<string> names)
    {
        var selected = names.Count == 0 ? All.Keys.ToList() : names.ToList();
        foreach (var name in selected)
        {
            if (!All.TryGetValue(name, out var scenario))
                throw new ArgumentException($"Unbekannter Ablauf \"{name}\". Vorhanden: {string.Join(", ", All.Keys)}");
            Console.WriteLine($"{name}:");
            scenario(ScreenDriver.Create(game, Path.Combine(target, name)));
        }
    }
}
