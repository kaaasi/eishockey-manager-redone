using System.Collections;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

public static partial class SaveGame
{
    /// <summary>Lädt einen Spielstand. Fehler werden als Meldung zurückgegeben, nie als Teilzustand.</summary>
    public static SaveLoadResult Load(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return Load(buffer.ToArray());
    }

    /// <summary>Lädt einen Spielstand. Fehler werden als Meldung zurückgegeben, nie als Teilzustand.</summary>
    public static SaveLoadResult Load(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        JsonObject root;
        try
        {
            root = JsonNode.Parse(data, documentOptions: new JsonDocumentOptions { MaxDepth = 1024 }) as JsonObject
                   ?? throw new JsonException();
        }
        catch (Exception e) when (e is JsonException or ArgumentException or InvalidOperationException)
        {
            return Fail("Die Datei ist kein Spielstand dieses Spiels.");
        }

        // Kennung und Version vor allem anderen prüfen.
        if (root["kennung"] is not JsonValue identifier || !identifier.TryGetValue<string>(out var text) || text != Identifier)
            return Fail("Die Datei ist kein Spielstand dieses Spiels.");
        if (root["schema"] is not JsonValue schemaValue || !schemaValue.TryGetValue<int>(out var schema) || schema < 1)
            return Fail("Der Spielstand hat keine gültige Version.");
        if (schema > SchemaVersion)
            return Fail($"Der Spielstand stammt aus einer neueren Version (Schema {schema}, unterstützt bis {SchemaVersion}).");

        for (; schema < SchemaVersion; schema++)
        {
            if (!Migrations.TryGetValue(schema, out var migrate))
                return Fail($"Spielstände mit Schema {schema} können nicht mehr geladen werden.");
            root = migrate(root);
        }

        try
        {
            using var document = JsonDocument.Parse(root.ToJsonString(), new JsonDocumentOptions { MaxDepth = 1024 });
            var game = new Reader(document.RootElement).Read();
            var problems = game.ValidatePlayerIdentity();
            if (problems.Count > 0)
                return Fail("Der Spielstand ist beschädigt: " + problems[0]);
            return new SaveLoadResult(game, null);
        }
        catch (Exception e) when (e is FormatException or JsonException or InvalidOperationException or KeyNotFoundException
                                       or InvalidCastException or ArgumentException or OverflowException or IndexOutOfRangeException
                                       or NotSupportedException)
        {
            return Fail("Der Spielstand ist beschädigt: " + e.Message);
        }
    }

    private static SaveLoadResult Fail(string message) => new(null, message);

    private sealed class Reader(JsonElement root)
    {
        private readonly List<object> _objects = [];
        private readonly Dictionary<int, Player> _players = [];
        private readonly Stack<string> _path = new();
        private Club[] _clubs = [];
        private Manager[] _managers = [];
        private GameState _game = null!;

        public GameState Read()
        {
            var clubs = Property(root, "vereine");
            var managers = Property(root, "manager");
            var players = Property(root, "spieler");
            _clubs = clubs.EnumerateArray().Select(_ => Shell<Club>()).ToArray();
            _managers = managers.EnumerateArray().Select(_ => Shell<Manager>()).ToArray();
            foreach (var entry in players.EnumerateArray())
            {
                var id = Property(entry, "Id").GetInt32();
                if (id <= 0 || !_players.TryAdd(id, Shell<Player>()))
                    throw Error($"Spieler-ID {id} ungültig oder doppelt.");
            }
            _game = Shell<GameState>();

            // Gleiche Reihenfolge wie beim Sichern, damit die Objektnummern übereinstimmen.
            Body(_game, typeof(GameState), Property(root, "spiel"), "spiel");
            var i = 0;
            foreach (var entry in clubs.EnumerateArray())
                Body(_clubs[i], typeof(Club), entry, $"Verein {i++}");
            i = 0;
            foreach (var entry in managers.EnumerateArray())
                Body(_managers[i], typeof(Manager), entry, $"Manager {i++}");
            foreach (var entry in players.EnumerateArray())
            {
                var id = Property(entry, "Id").GetInt32();
                Body(_players[id], typeof(Player), entry, $"Spieler {id}");
            }

            var expected = new HashSet<string>(["kennung", "schema", "spiel", "vereine", "manager", "spieler"]);
            foreach (var property in root.EnumerateObject())
                if (!expected.Remove(property.Name))
                    throw Error($"Unbekannter Abschnitt {property.Name}.");
            return _game;
        }

        private static T Shell<T>() => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));

        private JsonElement Property(JsonElement element, string name) =>
            element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value)
                ? value
                : throw Error($"„{name}“ fehlt.");

        private FormatException Error(string message) => new($"{string.Join(".", _path.Reverse())}: {message}");

        private void Body(object target, Type type, JsonElement element, string path)
        {
            _path.Push(path);
            if (element.ValueKind != JsonValueKind.Object)
                throw Error("Objekt erwartet.");
            ReadFields(target, type, element, skip: null);
            _path.Pop();
        }

        /// <summary>Liest alle Felder eines Typs; jedes muss vorhanden sein, unbekannte sind ein Fehler.</summary>
        private void ReadFields(object target, Type type, JsonElement element, HashSet<string>? skip)
        {
            var fields = Fields(type);
            var expected = fields.ToDictionary(FieldName, StringComparer.Ordinal);
            var count = 0;
            foreach (var property in element.EnumerateObject())
            {
                if (skip?.Contains(property.Name) == true)
                    continue;
                if (!expected.TryGetValue(property.Name, out var field))
                    throw Error($"Unbekanntes Feld {property.Name} in {TypeName(type)}.");
                _path.Push(property.Name);
                field.SetValue(target, ReadValue(property.Value, field.FieldType));
                _path.Pop();
                count++;
            }
            if (count != fields.Length)
            {
                var present = element.EnumerateObject().Select(p => p.Name).ToHashSet();
                var missing = expected.Keys.First(k => !present.Contains(k));
                throw Error($"Feld {missing} in {TypeName(type)} fehlt.");
            }
        }

        private static readonly HashSet<string> ObjectHeader = ["$id", "$type"];

        private object? ReadValue(JsonElement element, Type declared)
        {
            if (element.ValueKind == JsonValueKind.Null)
            {
                if (declared.IsValueType && Nullable.GetUnderlyingType(declared) is null)
                    throw Error($"Wert für {TypeName(declared)} fehlt.");
                return null;
            }

            var kind = Classify(declared);
            switch (kind)
            {
                case Kind.Nullable:
                    return ReadValue(element, Nullable.GetUnderlyingType(declared)!);
                case Kind.String:
                    return element.GetString();
                case Kind.Bool:
                    return element.GetBoolean();
                case Kind.Integer:
                    return declared == typeof(ulong)
                        ? element.GetUInt64()
                        : declared == typeof(char)
                            ? Convert.ToChar(checked((ushort)element.GetInt64()))
                            : Convert.ChangeType(element.GetInt64(), declared, CultureInfo.InvariantCulture);
                case Kind.Floating:
                    var number = element.GetString() ?? throw Error("Zahl fehlt.");
                    return declared == typeof(double) ? double.Parse(number, NumberStyles.Float, CultureInfo.InvariantCulture)
                        : declared == typeof(float) ? float.Parse(number, NumberStyles.Float, CultureInfo.InvariantCulture)
                        : decimal.Parse(number, NumberStyles.Number, CultureInfo.InvariantCulture);
                case Kind.Enum:
                    return Enum.Parse(declared, element.GetString() ?? throw Error("Aufzählungswert fehlt."));
                case Kind.Player:
                    return _players.TryGetValue(Reference(element, "$spieler"), out var player)
                        ? player : throw Error("Verweis auf einen unbekannten Spieler.");
                case Kind.Club:
                    return _clubs[Reference(element, "$verein")];
                case Kind.Manager:
                    return _managers[Reference(element, "$manager")];
                case Kind.Game:
                    Reference(element, "$spiel");
                    return _game;
                case Kind.Struct:
                    var value = RuntimeHelpers.GetUninitializedObject(declared);
                    ReadFields(value, declared, element, skip: null);
                    return value;
            }

            if (element.ValueKind != JsonValueKind.Object)
                throw Error($"Objekt für {TypeName(declared)} erwartet.");
            if (element.TryGetProperty("$ref", out var reference))
            {
                var id = reference.GetInt32();
                var target = id >= 0 && id < _objects.Count ? _objects[id] : throw Error($"Verweis {id} ins Leere.");
                return declared.IsInstanceOfType(target) ? target : throw Error($"Verweis {id} hat den falschen Typ.");
            }
            if (Property(element, "$id").GetInt32() != _objects.Count)
                throw Error("Objektnummern nicht fortlaufend.");

            switch (kind)
            {
                case Kind.Random:
                    var seed = Property(element, "seed").EnumerateArray().Select(e => e.GetInt32()).ToArray();
                    return Register(GameRandom.FromState(new RngState(seed, Property(element, "next").GetInt32(),
                        Property(element, "nextP").GetInt32())));
                case Kind.Array or Kind.ReadOnlySequence when declared.IsArray || IsReadOnly(declared):
                {
                    var elementType = Arguments(declared)[0];
                    var items = Property(element, "items");
                    var array = Array.CreateInstance(elementType, items.GetArrayLength());
                    Register(array);
                    var i = 0;
                    foreach (var item in items.EnumerateArray())
                        array.SetValue(Item(item, elementType, i), i++);
                    return array;
                }
                case Kind.Array2:
                {
                    var elementType = declared.GetElementType()!;
                    var dims = Property(element, "dims").EnumerateArray().Select(e => e.GetInt32()).ToArray();
                    var array = Array.CreateInstance(elementType, dims[0], dims[1]);
                    Register(array);
                    var items = Property(element, "items");
                    if (items.GetArrayLength() != dims[0] * dims[1])
                        throw Error("Feldgröße passt nicht.");
                    var i = 0;
                    foreach (var item in items.EnumerateArray())
                    {
                        array.SetValue(Item(item, elementType, i), i / dims[1], i % dims[1]);
                        i++;
                    }
                    return array;
                }
                case Kind.List or Kind.HashSet or Kind.ReadOnlySequence:
                {
                    var elementType = Arguments(declared)[0];
                    var concrete = kind == Kind.HashSet ? typeof(HashSet<>).MakeGenericType(elementType)
                        : typeof(List<>).MakeGenericType(elementType);
                    var collection = Activator.CreateInstance(concrete)!;
                    Register(collection);
                    var add = concrete.GetMethod("Add")!;
                    var i = 0;
                    foreach (var item in Property(element, "items").EnumerateArray())
                        if (add.Invoke(collection, [Item(item, elementType, i++)]) is false)
                            throw Error("Doppelter Eintrag in einer Menge.");
                    return collection;
                }
                case Kind.Dictionary or Kind.ReadOnlyMap:
                {
                    var arguments = Arguments(declared);
                    var dictionary = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(arguments))!;
                    Register(dictionary);
                    var i = 0;
                    foreach (var entry in Property(element, "entries").EnumerateArray())
                    {
                        _path.Push($"[{i++}]");
                        if (entry.GetArrayLength() != 2)
                            throw Error("Eintrag braucht Schlüssel und Wert.");
                        var key = ReadValue(entry[0], arguments[0]) ?? throw Error("Schlüssel fehlt.");
                        dictionary.Add(key, ReadValue(entry[1], arguments[1]));
                        _path.Pop();
                    }
                    return dictionary;
                }
                case Kind.Object or Kind.Polymorphic:
                {
                    var type = declared;
                    if (element.TryGetProperty("$type", out var typeName))
                    {
                        type = ResolveType(typeName.GetString() ?? "") ?? throw Error($"Unbekannter Typ {typeName}.");
                        if (!declared.IsAssignableFrom(type))
                            throw Error($"Typ {TypeName(type)} passt nicht zu {TypeName(declared)}.");
                    }
                    else if (kind == Kind.Polymorphic)
                        throw Error($"Typangabe für {TypeName(declared)} fehlt.");
                    if (Classify(type) != Kind.Object)
                        throw Error($"{TypeName(type)} ist keine Zustandsklasse.");
                    var target = RuntimeHelpers.GetUninitializedObject(type);
                    Register(target);
                    ReadFields(target, type, element, ObjectHeader);
                    return target;
                }
                default:
                    throw Error($"{TypeName(declared)} wird nicht unterstützt.");
            }
        }

        private static bool IsReadOnly(Type declared)
        {
            var definition = declared.GetGenericTypeDefinition();
            return definition == typeof(IReadOnlyList<>) || definition == typeof(IReadOnlyCollection<>) || definition == typeof(IEnumerable<>);
        }

        private object? Item(JsonElement item, Type type, int index)
        {
            _path.Push($"[{index}]");
            var value = ReadValue(item, type);
            _path.Pop();
            return value;
        }

        private object Register(object value)
        {
            _objects.Add(value);
            return value;
        }

        private int Reference(JsonElement element, string name)
        {
            if (element.ValueKind != JsonValueKind.Object)
                throw Error($"Verweis {name} erwartet.");
            var value = Property(element, name).GetInt32();
            if (element.EnumerateObject().Count() != 1)
                throw Error($"Verweis {name} mit weiteren Angaben.");
            return value;
        }
    }
}
