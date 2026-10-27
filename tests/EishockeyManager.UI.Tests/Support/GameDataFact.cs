using EishockeyManager.Formats;
using Xunit.Sdk;

namespace EishockeyManager.Tests.Support;

/// <summary>
/// Zugang der Tests zu den Originaldateien (Ordner <c>gamedata</c>, nie im Repository).
/// </summary>
/// <remarks>
/// Fehlen die Dateien, werden Tests mit <see cref="GameDataFactAttribute"/> als <b>übersprungen</b> gemeldet
/// (nicht als bestanden). Mit <c>EM_REQUIRE_GAMEDATA=1</c> werden sie trotzdem ausgeführt und scheitern
/// an <see cref="Load"/> – so kann ein ausdrücklich angeforderter Originaldatenlauf nicht still leer bleiben.
/// Mit <c>EM_SKIP_GAMEDATA=1</c> gelten vorhandene Originaldateien als fehlend: So lässt sich der Lauf „ohne Daten“
/// (wie in der CI) lokal prüfen, ohne den Ordner zu verschieben (<c>tools/pruefen.sh</c>).
/// </remarks>
public static class TestGameData
{
    /// <summary>Umgebungsvariable für den Pflichtlauf mit Originaldaten.</summary>
    public const string RequireVariable = "EM_REQUIRE_GAMEDATA";

    /// <summary>Umgebungsvariable, mit der vorhandene Originaldaten ignoriert werden (Lauf „ohne Daten“).</summary>
    public const string SkipVariable = "EM_SKIP_GAMEDATA";

    /// <summary>Kategorie (Trait „Category“), unter der die Originaldaten-Tests im Testbericht erscheinen.</summary>
    public const string Category = "Originaldaten";

    private static readonly Lazy<GameData?> Data = new(() =>
    {
        if (Environment.GetEnvironmentVariable(SkipVariable) == "1")
            return null;
        try { return GameData.Locate(); }
        catch (DirectoryNotFoundException) { return null; }
    });

    public static bool Required => Environment.GetEnvironmentVariable(RequireVariable) == "1";

    public static bool Available => Data.Value is not null;

    /// <summary>Grund für das Überspringen oder null, wenn die Tests laufen sollen.</summary>
    public static string? SkipReason => Available || Required
        ? null
        : $"Originaldaten fehlen (Ordner \"{GameData.FolderName}\") – übersprungen. Pflichtlauf: {RequireVariable}=1";

    /// <summary>Originaldateien; scheitert, wenn sie fehlen (nur im Pflichtlauf erreichbar).</summary>
    public static GameData Load() => Data.Value ?? throw new InvalidOperationException(
        $"Originaldaten fehlen (Ordner \"{GameData.FolderName}\"), aber {RequireVariable}=1 verlangt einen Lauf mit Originaldaten.");
}

/// <summary>
/// Test, der die Originaldateien braucht: ohne Daten echtes „Skipped“, im Pflichtlauf Fehler.
/// Trägt den Trait <c>Category=Originaldaten</c> für den getrennten Testbericht.
/// </summary>
/// <remarks>
/// Den Trait liefert der eingebaute <c>Xunit.Sdk.TraitDiscoverer</c> aus den (voreingestellten)
/// Konstruktorargumenten – so funktioniert die Datei unverändert in mehreren Testprojekten.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
[XunitTestCaseDiscoverer("Xunit.Sdk.FactDiscoverer", "xunit.execution.{Platform}")]
[TraitDiscoverer("Xunit.Sdk.TraitDiscoverer", "xunit.core")]
public sealed class GameDataFactAttribute : FactAttribute, ITraitAttribute
{
    public GameDataFactAttribute(string name = "Category", string value = TestGameData.Category)
    {
        Name = name;
        Value = value;
        if (TestGameData.SkipReason is { } reason)
            Skip = reason;
    }

    public string Name { get; }
    public string Value { get; }
}

/// <summary>Wie <see cref="GameDataFactAttribute"/>, für Tests mit Datenzeilen (<c>[InlineData]</c>).</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
[XunitTestCaseDiscoverer("Xunit.Sdk.TheoryDiscoverer", "xunit.execution.{Platform}")]
[TraitDiscoverer("Xunit.Sdk.TraitDiscoverer", "xunit.core")]
public sealed class GameDataTheoryAttribute : TheoryAttribute, ITraitAttribute
{
    public GameDataTheoryAttribute(string name = "Category", string value = TestGameData.Category)
    {
        Name = name;
        Value = value;
        if (TestGameData.SkipReason is { } reason)
            Skip = reason;
    }

    public string Name { get; }
    public string Value { get; }
}
