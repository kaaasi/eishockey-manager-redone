namespace EishockeyManager.Tools;

/// <summary>Gemeinsame Argumenthilfen der Befehle <c>emtool hok …</c> (Vergleich, Export, Import, Patch).</summary>
internal static class HokArgs
{
    /// <summary>
    /// Startwert des Zufalls beim Import eines Original-Spielstands, wenn <c>--seed</c> fehlt (<see cref="HokCompare.DefaultSeed"/>;
    /// dort, weil die Core-Tests <c>HokCompare*.cs</c> als Link-Datei einbinden).
    /// </summary>
    public const int DefaultSeed = HokCompare.DefaultSeed;

    /// <summary>Wert hinter der Option <c>args[i]</c>; rückt <paramref name="i"/> weiter. Fehlt er: „&lt;Option&gt; braucht einen Wert.“</summary>
    public static string Next(IReadOnlyList<string> args, ref int i) =>
        ++i < args.Count ? args[i] : throw new ArgumentException($"{args[i - 1]} braucht einen Wert.");
}
