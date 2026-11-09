using System.Reflection;
using EishockeyManager.Core;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens.Competitions;

namespace EishockeyManager.Tests.Support;

/// <summary>
/// Findet über Reflexion alle Bildschirme der Oberfläche (jede nicht abstrakte Ableitung von <see cref="Screen"/>, auch
/// nicht öffentliche und verschachtelte) und baut für jeden Konstruktor Argumente aus einem Spielzustand
/// (Spur <c>pruefung-karte</c> Teil 1, Bereich aus #557).
/// </summary>
/// <remarks>
/// Nicht erzeugbar sind nur Konstruktoren mit Parametern, für die <see cref="TryResolve"/> kein Argument kennt; sie werden
/// mit dem Parametertyp gemeldet und in <c>BildschirmeFreipruefungTests</c> mit der festen Liste <c>NotConstructible</c>
/// verglichen, damit die Abdeckung nicht still schrumpft (#610, R13PK-02).
/// </remarks>
public static class ScreenReflection
{
    private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    /// <summary>Alle konkreten Bildschirmtypen der UI-Assembly, nach Namen sortiert.</summary>
    public static IReadOnlyList<Type> AllScreenTypes() =>
        typeof(Screen).Assembly.GetTypes()
            .Where(t => typeof(Screen).IsAssignableFrom(t) && !t.IsAbstract && !t.IsGenericTypeDefinition)
            .OrderBy(t => t.FullName, StringComparer.Ordinal)
            .ToArray();

    /// <summary>Statische Fabrikmethoden, die einen Bildschirm liefern (indirekte Aufrufe wie <c>ForSeasonEnd</c>, <c>Open</c>, <c>Create</c>).</summary>
    public static IEnumerable<MethodInfo> Factories() =>
        typeof(Screen).Assembly.GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            .Where(m => typeof(Screen).IsAssignableFrom(m.ReturnType) && !m.IsGenericMethodDefinition && !m.Name.StartsWith('<'))
            .OrderBy(m => $"{m.DeclaringType!.FullName}.{m.Name}", StringComparer.Ordinal);

    public static IEnumerable<ConstructorInfo> Constructors(Type type) => type.GetConstructors(AnyInstance);

    /// <summary>Kontext, aus dem die Argumente genommen werden.</summary>
    public sealed record Context(ScreenHost Host, GameState Game, Screen Back);

    /// <summary>Argumente für einen Konstruktor oder null mit dem Namen des ersten unbekannten Parametertyps in <paramref name="missing"/>.</summary>
    public static object?[]? BuildArguments(MethodBase ctor, Context ctx, int variant, out string? missing)
    {
        var parameters = ctor.GetParameters();
        var args = new object?[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            if (!TryResolve(parameters[i], ctx, variant, out args[i]))
            {
                missing = $"{parameters[i].ParameterType.Name} {parameters[i].Name}";
                return null;
            }
        }
        missing = null;
        return args;
    }

    private static Func<T> Constant<T>(T value) => () => value;

    private static bool TryResolve(ParameterInfo p, Context ctx, int variant, out object? value)
    {
        var t = Nullable.GetUnderlyingType(p.ParameterType) ?? p.ParameterType;
        var game = ctx.Game;
        var manager = game.Managers[Math.Min(game.CurrentManager, game.Managers.Count - 1)];
        var club = game.Clubs[manager.ClubIndex];
        value = null;

        if (t == typeof(ScreenHost)) { value = ctx.Host; return true; }
        if (t == typeof(GameState)) { value = game; return true; }
        if (t.IsAssignableFrom(ctx.Back.GetType()) && typeof(Screen).IsAssignableFrom(t)) { value = ctx.Back; return true; }
        if (t == typeof(Func<Screen>)) { value = (Func<Screen>)(() => ctx.Back); return true; }
        if (t == typeof(Action)) { value = (Action)(() => { }); return true; }
        if (t == typeof(Manager)) { value = manager; return true; }
        if (t == typeof(Club)) { value = club; return true; }
        if (t == typeof(Player)) { value = club.Players[Math.Min(variant, club.Players.Count - 1)]; return true; }
        if (t == typeof(int)) { value = variant; return true; }
        if (t == typeof(bool)) { value = variant % 2 == 1; return true; }
        if (t == typeof(string)) { value = ""; return true; }
        if (t == typeof(IReadOnlyList<int>) || t == typeof(int[]) || t == typeof(List<int>))
        {
            value = t == typeof(int[]) ? new[] { 0, 1 } : new List<int> { 0, 1 };
            return true;
        }
        if (t.IsEnum)
        {
            var values = Enum.GetValues(t);
            value = values.GetValue(variant % values.Length);
            return true;
        }
        if (t == typeof(TrophyStep)) { value = new TrophyStep(0, Title.GermanChampion, manager.ClubIndex); return true; }
        if (t == typeof(DebCupOverview))
        {
            value = DebCupOverview.Of(game) ?? new DebCupOverview(0, 0, null, [], "");
            return true;
        }
        if (t == typeof(PairingsPage)) { value = new PairingsPage("", []); return true; }
        if (t == typeof(Tactics)) { value = new Tactics(); return true; }
        if (t.IsGenericType)
        {
            var def = t.GetGenericTypeDefinition();
            var arg = t.GetGenericArguments()[0];
            if (def == typeof(IReadOnlyList<>) || def == typeof(IReadOnlyCollection<>) || def == typeof(IEnumerable<>) || def == typeof(IList<>))
            {
                value = Activator.CreateInstance(typeof(List<>).MakeGenericType(arg));
                return true;
            }
            if (def == typeof(Queue<>)) { value = Activator.CreateInstance(t); return true; }
            if (def == typeof(Func<>) && arg.GetConstructor(Type.EmptyTypes) is not null)
            {
                var made = Activator.CreateInstance(arg);
                value = typeof(ScreenReflection).GetMethod(nameof(Constant), BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod(arg).Invoke(null, [made]);
                return true;
            }
        }
        if (p.HasDefaultValue) { value = p.DefaultValue; return true; }
        if (p.ParameterType != t) { value = null; return true; } // Nullable<T>
        return false;
    }
}
