using System.Reflection;

namespace EishockeyManager.UI;

/// <summary>
/// Meldet einen Bildschirm für eine kleine Kachel des Hauptmenüs an. Die markierte Methode muss
/// <c>static Screen Name(ScreenHost host, Screen back)</c> sein.
/// </summary>
/// <remarks>
/// So kann jedes Arbeitspaket seine Bildschirme in eigenen Dateien anmelden, ohne das Hauptmenü
/// zu ändern (vermeidet Konflikte bei paralleler Arbeit). Gültige Namen: <see cref="Screens.MenuTiles"/>.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MenuEntryAttribute(string tile) : Attribute
{
    public string Tile { get; } = tile;
}

/// <summary>Findet alle mit <see cref="MenuEntryAttribute"/> angemeldeten Bildschirme.</summary>
public static class MenuEntries
{
    private static readonly Lazy<IReadOnlyDictionary<string, Func<ScreenHost, Screen, Screen>>> Registry = new(Scan);

    public static IReadOnlyDictionary<string, Func<ScreenHost, Screen, Screen>> All => Registry.Value;

    public static bool TryCreate(string tile, ScreenHost host, Screen back, out Screen? screen)
    {
        screen = All.TryGetValue(tile, out var factory) ? factory(host, back) : null;
        return screen is not null;
    }

    private static IReadOnlyDictionary<string, Func<ScreenHost, Screen, Screen>> Scan()
    {
        var known = Screens.MenuTiles.AllNames.ToHashSet();
        var result = new Dictionary<string, Func<ScreenHost, Screen, Screen>>();
        var methods = typeof(MenuEntries).Assembly.GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));
        foreach (var method in methods)
        {
            foreach (var attribute in method.GetCustomAttributes<MenuEntryAttribute>())
            {
                if (!known.Contains(attribute.Tile))
                    throw new InvalidOperationException($"Unbekannte Kachel \"{attribute.Tile}\" an {method.DeclaringType?.Name}.{method.Name}.");
                if (result.ContainsKey(attribute.Tile))
                    throw new InvalidOperationException($"Kachel \"{attribute.Tile}\" ist doppelt angemeldet ({method.DeclaringType?.Name}.{method.Name}).");
                var factory = method.CreateDelegate<Func<ScreenHost, Screen, Screen>>();
                result[attribute.Tile] = factory;
            }
        }
        return result;
    }
}
