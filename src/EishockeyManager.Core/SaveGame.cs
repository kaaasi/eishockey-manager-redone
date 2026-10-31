using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EishockeyManager.Core;

/// <summary>Ergebnis von <see cref="SaveGame.Load(byte[])"/>: der geladene Spielstand oder eine Meldung.</summary>
public sealed record SaveLoadResult(GameState? Game, string? Error)
{
    public bool Success => Game is not null;
}

/// <summary>
/// Ergebnis der Feldprobe eines Zustandstyps (<see cref="SaveGame.CoverageProbe"/>): die Felder, die der Writer
/// tatsächlich schreibt, und die Meldung des Readers beim Zurücklesen (<c>null</c> = ohne Meldung gelesen).
/// </summary>
public sealed record FieldProbe(IReadOnlyList<string> WrittenFields, string? ReaderError);

/// <summary>
/// Eigenes Spielstandformat (Spur „speichern“, Teil „format“; Beschreibung in <c>docs/formate/spielstand.md</c>).
/// <para>
/// Gesichert wird der <b>gesamte</b> Zustand: jedes Instanzfeld von <see cref="GameState"/> und aller daran hängenden
/// Zustandsklassen aus Core und Formats. Spieler werden über ihre ID (<see cref="Player.Id"/>), Vereine und Manager
/// über ihren Index in <see cref="GameState.Clubs"/> bzw. <see cref="GameState.Managers"/> verwiesen; alle übrigen
/// Objekte über eine Objektnummer in der Datei, damit gemeinsam genutzte Objekte gemeinsam bleiben.
/// </para>
/// <para>
/// Laden ist streng: Kennung und Schemaversion werden vor allem anderen geprüft, jedes Feld muss vorhanden und bekannt
/// sein, sonst wird mit Meldung abgebrochen. Laden erzeugt einen neuen Spielstand; der laufende bleibt unberührt.
/// </para>
/// </summary>
public static partial class SaveGame
{
    /// <summary>Kennung am Dateianfang.</summary>
    public const string Identifier = "EISHOCKEY-MANAGER-REDONE-SPIELSTAND";

    /// <summary>Aktuelle Schemaversion. Bei jeder Änderung der Zustandsfelder erhöhen und eine Migration eintragen.</summary>
    public const int SchemaVersion = 15;

    /// <summary>Dateiendung der eigenen Spielstände.</summary>
    public const string FileExtension = ".ems";

    /// <summary>
    /// Migrationen je Schemaversion: Eintrag <c>n</c> hebt eine Datei von Version <c>n</c> auf <c>n + 1</c>.
    /// 1 → 2: neue Finanzfelder der Spur finanzen-2 (<c>SaveGame.Migration1.cs</c>).
    /// 2 → 3: Spielmodus (Fahne) und Buchungen des Nationalkontos, Spur wettbewerbe-weltkugel (<c>SaveGame.Migration2.cs</c>).
    /// 3 → 4: Aufstellung, Taktik und Eingesetzte des Bundestrainers, Spur nationalteam-2 (<c>SaveGame.Migration3.cs</c>).
    /// 4 → 5: Taktikwerte nach em.exe und „Eingesetzt“ der Vereine, Spur taktik-verein (<c>SaveGame.Migration4.cs</c>).
    /// 5 → 6: Zug des Bundestrainers im Modus 4, Spur bundestrainer-modus4 (<c>SaveGame.Migration5.cs</c>).
    /// 6 → 7: Werbebewertung und zweite Eistafel, Spur finanzen-werbung (<c>SaveGame.Migration6.cs</c>).
    /// 7 → 8: Zuschauerstatistik je Manager, Spur heimspiel-statistik (<c>SaveGame.Migration7.cs</c>; Integration Runde 6, ursprünglich 6 → 7).
    /// 8 → 9: Merker „nicht spielfähig“ je Verein, Spur heimspiel-statistik #165 (<c>SaveGame.Migration8.cs</c>).
    /// 9 → 10: Kursmodell der Börse nach em.exe, Spur boerse-start #169 (<c>SaveGame.Migration9.cs</c>).
    /// 10 → 11: Torhüterzähler je Spieler, Spur spielberechnung (<c>SaveGame.Migration10.cs</c>; Integration Runde 7, ursprünglich 9 → 10).
    /// 11 → 12: Taktik des Nationalteams, Spur nationalteam #192 (<c>SaveGame.Migration.Nationalteam.cs</c>; Integration Runde 8).
    /// 12 → 13: Strafminuten der Ligaspiele je Verein, Spur statistiken (<c>SaveGame.Migration.Statistiken.cs</c>; Integration
    /// Runde 8, auf dem Branch 11 → 12).
    /// 13 → 14: Teamwerte des Laders je Verein, Spur spielstaerke-manager (<c>SaveGame.Migration.SpielstaerkeManager.cs</c>;
    /// Integration Runde 8, auf dem Branch 11 → 12).
    /// 14 → 15: Serien je Verein, Rekordserien und Über-/Unterzahl je Manager, Spur statistiken-verein
    /// (<c>SaveGame.Migration.StatistikenVerein.cs</c>; vorläufig, die Leitstelle nummeriert in der Integration).
    /// </summary>
    private static readonly IReadOnlyDictionary<int, Func<System.Text.Json.Nodes.JsonObject, System.Text.Json.Nodes.JsonObject>> Migrations =
        new Dictionary<int, Func<System.Text.Json.Nodes.JsonObject, System.Text.Json.Nodes.JsonObject>>
        {
            [1] = MigrateFrom1,
            [2] = MigrateFrom2,
            [3] = MigrateFrom3,
            [4] = MigrateFrom4,
            [5] = MigrateFrom5,
            [6] = MigrateFrom6,
            [7] = MigrateFrom7,
            [8] = MigrateFrom8,
            [9] = MigrateFrom9,
            [10] = MigrateFrom10,
            [11] = MigrateFromNationalteam,
            [12] = MigrateStatistiken,
            [13] = MigrateFromTeamValues,
            [14] = MigrateStatistikenVerein,
        };

    private static readonly Assembly CoreAssembly = typeof(GameState).Assembly;
    private static readonly Assembly FormatsAssembly = typeof(Formats.PlayerRole).Assembly;

    private static bool IsStateAssembly(Assembly assembly) => assembly == CoreAssembly || assembly == FormatsAssembly;

    /// <summary>Art eines Feldtyps im Format.</summary>
    internal enum Kind
    {
        String, Bool, Integer, Floating, Enum, Nullable,
        Player, Club, Manager, Game, Random,
        Array, Array2, List, Dictionary, HashSet,
        ReadOnlySequence, ReadOnlyMap,
        Object, Struct, Polymorphic,
    }

    internal static Kind Classify(Type type)
    {
        if (type == typeof(string)) return Kind.String;
        if (type == typeof(bool)) return Kind.Bool;
        if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte)
            || type == typeof(sbyte) || type == typeof(ushort) || type == typeof(uint) || type == typeof(ulong)
            || type == typeof(char))
            return Kind.Integer;
        if (type == typeof(double) || type == typeof(float) || type == typeof(decimal)) return Kind.Floating;
        if (type.IsEnum) return Kind.Enum;
        if (Nullable.GetUnderlyingType(type) is not null) return Kind.Nullable;
        if (type == typeof(Player)) return Kind.Player;
        if (type == typeof(Club)) return Kind.Club;
        if (type == typeof(Manager)) return Kind.Manager;
        if (type == typeof(GameState)) return Kind.Game;
        if (type == typeof(GameRandom)) return Kind.Random;
        if (type.IsArray) return type.GetArrayRank() switch
        {
            1 => Kind.Array,
            2 => Kind.Array2,
            _ => throw Unsupported(type),
        };
        if (type.IsGenericType)
        {
            var definition = type.GetGenericTypeDefinition();
            if (definition == typeof(List<>)) return Kind.List;
            if (definition == typeof(Dictionary<,>)) return Kind.Dictionary;
            if (definition == typeof(HashSet<>)) return Kind.HashSet;
            if (definition == typeof(IReadOnlyList<>) || definition == typeof(IReadOnlyCollection<>)
                || definition == typeof(IEnumerable<>) || definition == typeof(IList<>) || definition == typeof(ICollection<>))
                return Kind.ReadOnlySequence;
            if (definition == typeof(IReadOnlyDictionary<,>) || definition == typeof(IDictionary<,>)) return Kind.ReadOnlyMap;
            if (type.IsValueType && definition.FullName!.StartsWith("System.ValueTuple`", StringComparison.Ordinal))
                return Kind.Struct;
        }
        if (IsStateAssembly(type.Assembly))
        {
            if (type.IsValueType) return Kind.Struct;
            if (type.IsInterface || type.IsAbstract) return Kind.Polymorphic;
            if (typeof(Delegate).IsAssignableFrom(type)) throw Unsupported(type);
            return Kind.Object;
        }
        throw Unsupported(type);
    }

    private static NotSupportedException Unsupported(Type type) =>
        new($"Der Typ {TypeName(type)} kann nicht im Spielstand gesichert werden.");

    private static readonly Dictionary<Type, FieldInfo[]> FieldCache = [];
    private static readonly Dictionary<FieldInfo, string> FieldNames = [];

    /// <summary>
    /// Alle Instanzfelder eines Zustandstyps (auch geerbte aus Core/Formats), nach Name sortiert.
    /// Rückfelder automatischer Eigenschaften heißen in der Datei wie die Eigenschaft.
    /// </summary>
    internal static FieldInfo[] Fields(Type type)
    {
        lock (FieldCache)
        {
            if (FieldCache.TryGetValue(type, out var cached))
                return cached;
            var fields = new List<FieldInfo>();
            for (var t = type; t is not null && t != typeof(object) && t != typeof(ValueType)
                 && (IsStateAssembly(t.Assembly) || t.IsValueType); t = t.BaseType)
                fields.AddRange(t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly));
            foreach (var field in fields)
                FieldNames[field] = JsonFieldName(field, type);
            var result = fields.OrderBy(f => FieldNames[f], StringComparer.Ordinal).ToArray();
            var duplicate = result.GroupBy(f => FieldNames[f]).FirstOrDefault(g => g.Count() > 1);
            if (duplicate is not null)
                throw new NotSupportedException($"Feldname {duplicate.Key} in {TypeName(type)} ist nicht eindeutig.");
            FieldCache[type] = result;
            return result;
        }
    }

    internal static string FieldName(FieldInfo field)
    {
        lock (FieldCache)
            return FieldNames[field];
    }

    private static readonly Regex BackingField = new("^<(?<name>[^>]+)>k__BackingField$", RegexOptions.CultureInvariant);

    private static string JsonFieldName(FieldInfo field, Type owner)
    {
        var name = BackingField.Match(field.Name) is { Success: true } m ? m.Groups["name"].Value : field.Name;
        return field.DeclaringType == owner || owner.IsValueType ? name : $"{TypeName(field.DeclaringType!)}.{name}";
    }

    /// <summary>Lesbarer Typname (ohne Namensraum) für Meldungen und das Feld „$type“.</summary>
    internal static string TypeName(Type type)
    {
        if (type.IsArray)
            return TypeName(type.GetElementType()!) + "[" + new string(',', type.GetArrayRank() - 1) + "]";
        if (type.IsGenericType)
        {
            var name = type.Name[..type.Name.IndexOf('`')];
            return $"{name}<{string.Join(",", type.GetGenericArguments().Select(TypeName))}>";
        }
        var full = type.FullName ?? type.Name;
        var ns = type.Namespace is { Length: > 0 } n ? n + "." : "";
        return full.StartsWith(ns, StringComparison.Ordinal) ? full[ns.Length..] : full;
    }

    private static readonly Lazy<Dictionary<string, Type>> PolymorphicTypes = new(() =>
        CoreAssembly.GetTypes().Concat(FormatsAssembly.GetTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false } && !typeof(Delegate).IsAssignableFrom(t)
                        && !t.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false))
            .GroupBy(TypeName, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal));

    /// <summary>Konkrete Zustandsklasse zu einem Namen aus „$type“ (nur Core und Formats, nie fremde Typen).</summary>
    private static Type? ResolveType(string name) => PolymorphicTypes.Value.GetValueOrDefault(name);

    /// <summary>Elementtyp einer Sequenz bzw. Schlüssel- und Werttyp einer Zuordnung.</summary>
    private static Type[] Arguments(Type type) => type.IsArray ? [type.GetElementType()!] : type.GetGenericArguments();

    /// <summary>
    /// Abnahme 5 (Vollständigkeit): Prüft über die Typen – nicht über einen Beispielspielstand –, dass jedes Feld
    /// von <see cref="GameState"/> und jeder erreichbaren Zustandsklasse gesichert werden kann. Ausgenommen ist nichts;
    /// <see cref="GameRandom"/> wird über seinen Zustand (<see cref="RngState"/>) gesichert. Dazu schreibt und liest eine Probe
    /// jedes Typs tatsächlich alle Feldnamen (#81, <see cref="CoverageProbe"/>). Leere Liste = vollständig.
    /// </summary>
    public static IReadOnlyList<string> CheckSchemaCoverage()
    {
        var problems = new List<string>();
        var seen = new HashSet<Type>();
        var queue = new Queue<(Type Type, string Path)>();
        queue.Enqueue((typeof(GameState), "GameState"));
        while (queue.Count > 0)
        {
            var (type, path) = queue.Dequeue();
            if (!seen.Add(type))
                continue;
            Kind kind;
            try
            {
                kind = Classify(type);
            }
            catch (NotSupportedException e)
            {
                problems.Add($"{path}: {e.Message}");
                continue;
            }
            switch (kind)
            {
                case Kind.Nullable:
                    queue.Enqueue((Nullable.GetUnderlyingType(type)!, path));
                    break;
                case Kind.Array or Kind.Array2 or Kind.List or Kind.HashSet or Kind.ReadOnlySequence or Kind.Dictionary or Kind.ReadOnlyMap:
                    foreach (var argument in Arguments(type))
                        queue.Enqueue((argument, $"{path}[]"));
                    break;
                case Kind.Polymorphic:
                    var implementations = PolymorphicTypes.Value.Values.Where(type.IsAssignableFrom).ToList();
                    if (implementations.Count == 0)
                        problems.Add($"{path}: keine konkrete Klasse für {TypeName(type)}.");
                    foreach (var implementation in implementations)
                        queue.Enqueue((implementation, $"{path}<{TypeName(implementation)}>"));
                    break;
                case Kind.Object or Kind.Struct or Kind.Player or Kind.Club or Kind.Manager or Kind.Game:
                    FieldInfo[] fields;
                    try
                    {
                        fields = Fields(type);
                    }
                    catch (NotSupportedException e)
                    {
                        problems.Add($"{path}: {e.Message}");
                        break;
                    }
                    foreach (var field in fields)
                        queue.Enqueue((field.FieldType, $"{TypeName(type)}.{FieldName(field)}"));
                    problems.AddRange(ProbeProblems(type, fields));
                    break;
            }
        }
        return problems;
    }

    /// <summary>
    /// #81: Gleicht für einen Typ ab, was Writer und Reader tatsächlich tun, mit dem Schema (<see cref="Fields"/>).
    /// Unabhängig davon, ob der Typ in einem Beispielspielstand belegt ist.
    /// </summary>
    private static IEnumerable<string> ProbeProblems(Type type, FieldInfo[] fields)
    {
        FieldProbe probe;
        try
        {
            probe = Probe(type);
        }
        catch (Exception e) when (e is InvalidOperationException or NotSupportedException or ArgumentException)
        {
            return [$"{TypeName(type)}: Writer-Probe scheitert: {e.Message}"];
        }
        var schema = fields.Select(FieldName).ToHashSet(StringComparer.Ordinal);
        var problems = schema.Except(probe.WrittenFields, StringComparer.Ordinal)
            .Select(name => $"{TypeName(type)}: Feld {name} wird nicht geschrieben.")
            .Concat(probe.WrittenFields.Except(schema, StringComparer.Ordinal)
                .Select(name => $"{TypeName(type)}: Feld {name} wird geschrieben, steht aber nicht im Schema."))
            .ToList();
        if (probe.ReaderError is not null)
            problems.Add($"{TypeName(type)}: Reader lehnt die Probe ab: {probe.ReaderError}");
        return problems;
    }

    /// <summary>
    /// #81 (Abnahme 5, Nachweis über Writer und Reader): Für jeden erreichbaren Zustandstyp wird ein leeres Objekt
    /// (alle Felder auf Standardwert) geschrieben und zurückgelesen – auch für Typen, die in keinem Beispielspielstand
    /// vorkommen. Schlüssel ist der Typname wie in <see cref="SchemaDescription"/>.
    /// </summary>
    public static IReadOnlyDictionary<string, FieldProbe> CoverageProbe()
    {
        var result = new SortedDictionary<string, FieldProbe>(StringComparer.Ordinal);
        foreach (var type in StateTypes())
            if (Classify(type) is Kind.Object or Kind.Struct or Kind.Player or Kind.Club or Kind.Manager or Kind.Game)
                result.Add(TypeName(type), Probe(type));
        return result;
    }

    private static FieldProbe Probe(Type type)
    {
        using var buffer = new MemoryStream();
        using (var json = new Utf8JsonWriter(buffer))
            new Writer(null!, json).WriteProbe(RuntimeHelpers.GetUninitializedObject(type), type);
        using var document = JsonDocument.Parse(buffer.ToArray());
        var written = document.RootElement.EnumerateObject().Select(p => p.Name).ToArray();
        string? error = null;
        try
        {
            // Ohne Nullbarkeitsprüfung: Die Probe ist absichtlich leer, Pflichtverweise prüft der Ladetest (#79).
            new Reader(document.RootElement, checkNullability: false)
                .ReadProbe(RuntimeHelpers.GetUninitializedObject(type), type);
        }
        catch (Exception e) when (e is FormatException or JsonException or InvalidOperationException or InvalidCastException
                                       or ArgumentException or KeyNotFoundException or OverflowException)
        {
            error = e.Message;
        }
        return new FieldProbe(written, error);
    }

    /// <summary>Alle von <see cref="GameState"/> aus erreichbaren Typen (Feldtypen, Elementtypen, Implementierungen).</summary>
    private static IEnumerable<Type> StateTypes()
    {
        var seen = new HashSet<Type>();
        var queue = new Queue<Type>([typeof(GameState)]);
        while (queue.Count > 0)
        {
            var type = queue.Dequeue();
            if (!seen.Add(type))
                continue;
            yield return type;
            if (type.IsEnum)
                continue;
            switch (Classify(type))
            {
                case Kind.Nullable:
                    queue.Enqueue(Nullable.GetUnderlyingType(type)!);
                    break;
                case Kind.Array or Kind.Array2 or Kind.List or Kind.HashSet or Kind.ReadOnlySequence or Kind.Dictionary or Kind.ReadOnlyMap:
                    foreach (var argument in Arguments(type))
                        queue.Enqueue(argument);
                    break;
                case Kind.Polymorphic:
                    foreach (var implementation in PolymorphicTypes.Value.Values.Where(type.IsAssignableFrom))
                        queue.Enqueue(implementation);
                    break;
                case Kind.Object or Kind.Struct or Kind.Player or Kind.Club or Kind.Manager or Kind.Game:
                    foreach (var field in Fields(type))
                        queue.Enqueue(field.FieldType);
                    break;
            }
        }
    }

    /// <summary>
    /// Abdruck des Schemas: alle erreichbaren Zustandstypen mit Feldname und Feldtyp. Ändert er sich, muss
    /// <see cref="SchemaVersion"/> steigen (mit Migration), sonst lassen sich alte Spielstände nicht mehr laden.
    /// </summary>
    public static string SchemaFingerprint() =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(string.Join("\n", SchemaDescription()))))[..16];

    /// <summary>Schema als Text: eine Zeile je Feld („Typ.Feld: Feldtyp“), sortiert.</summary>
    public static IReadOnlyList<string> SchemaDescription()
    {
        var lines = new SortedSet<string>(StringComparer.Ordinal);
        var seen = new HashSet<Type>();
        var queue = new Queue<Type>([typeof(GameState)]);
        while (queue.Count > 0)
        {
            var type = queue.Dequeue();
            if (!seen.Add(type))
                continue;
            if (type.IsEnum)
            {
                lines.Add($"{TypeName(type)} = {string.Join(",", Enum.GetNames(type))}");
                continue;
            }
            switch (Classify(type))
            {
                case Kind.Nullable:
                    queue.Enqueue(Nullable.GetUnderlyingType(type)!);
                    break;
                case Kind.Array or Kind.Array2 or Kind.List or Kind.HashSet or Kind.ReadOnlySequence or Kind.Dictionary or Kind.ReadOnlyMap:
                    foreach (var argument in Arguments(type))
                        queue.Enqueue(argument);
                    break;
                case Kind.Polymorphic:
                    foreach (var implementation in PolymorphicTypes.Value.Values.Where(type.IsAssignableFrom))
                    {
                        lines.Add($"{TypeName(type)} : {TypeName(implementation)}");
                        queue.Enqueue(implementation);
                    }
                    break;
                case Kind.Object or Kind.Struct or Kind.Player or Kind.Club or Kind.Manager or Kind.Game:
                    foreach (var field in Fields(type))
                    {
                        lines.Add($"{TypeName(type)}.{FieldName(field)}: {TypeName(field.FieldType)}");
                        queue.Enqueue(field.FieldType);
                    }
                    break;
            }
        }
        return [.. lines];
    }

    private static string Invariant(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    private static IEnumerable<object?> Items(object collection) => ((IEnumerable)collection).Cast<object?>();
}
