using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace EishockeyManager.Core;

public static partial class SaveGame
{
    /// <summary>Sichert den gesamten Spielstand als UTF-8-Text (JSON). Gleicher Zustand → gleiche Bytes.</summary>
    public static byte[] Save(GameState game)
    {
        using var stream = new MemoryStream();
        Save(game, stream);
        return stream.ToArray();
    }

    /// <summary>Sichert den gesamten Spielstand in einen Strom. Verändert den Spielstand nicht.</summary>
    /// <exception cref="InvalidOperationException">Spieler-IDs oder Spielerverweise sind fehlerhaft.</exception>
    public static void Save(GameState game, Stream stream)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(stream);
        var problems = game.ValidatePlayerIdentity();
        if (problems.Count > 0)
            throw new InvalidOperationException("Spielstand nicht speicherbar: " + string.Join(" ", problems));

        using var buffer = new MemoryStream();
        using (var json = new Utf8JsonWriter(buffer, new JsonWriterOptions
               {
                   Indented = true,
                   Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                   SkipValidation = false,
               }))
            new Writer(game, json).Write();
        buffer.WriteTo(stream);
    }

    private sealed class Writer(GameState game, Utf8JsonWriter json)
    {
        private readonly Dictionary<object, int> _ids = new(ReferenceEqualityComparer.Instance);
        private readonly SortedDictionary<int, Player> _players = [];
        private readonly HashSet<int> _writtenPlayers = [];
        private readonly Dictionary<Club, int> _clubs = new(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<Manager, int> _managers = new(ReferenceEqualityComparer.Instance);
        private readonly Stack<string> _path = new();

        public void Write()
        {
            for (var i = 0; i < game.Clubs.Count; i++)
                _clubs.Add(game.Clubs[i], i);
            for (var i = 0; i < game.Managers.Count; i++)
                _managers.Add(game.Managers[i], i);

            json.WriteStartObject();
            json.WriteString("kennung", Identifier);
            json.WriteNumber("schema", SchemaVersion);

            json.WritePropertyName("spiel");
            _path.Push("spiel");
            WriteFields(game, typeof(GameState));
            _path.Pop();

            json.WriteStartArray("vereine");
            foreach (var club in game.Clubs)
                WriteBody(club, typeof(Club), $"Verein {club.Index}");
            json.WriteEndArray();

            json.WriteStartArray("manager");
            foreach (var manager in game.Managers)
                WriteBody(manager, typeof(Manager), $"Manager {manager.Name}");
            json.WriteEndArray();

            // Spieler zuletzt: erst jetzt sind alle Verweise gesammelt. Ein Spieler verweist nie auf einen Spieler.
            json.WriteStartArray("spieler");
            foreach (var (id, player) in _players.ToArray())
            {
                WriteBody(player, typeof(Player), $"Spieler {id}");
                _writtenPlayers.Add(id);
            }
            if (_players.Count != _writtenPlayers.Count)
                throw new InvalidOperationException("Ein Spieler verweist auf einen weiteren Spieler.");
            json.WriteEndArray();

            json.WriteEndObject();
        }

        private void WriteBody(object value, Type type, string path)
        {
            _path.Push(path);
            WriteFields(value, type);
            _path.Pop();
        }

        private void WriteFields(object value, Type type)
        {
            json.WriteStartObject();
            WriteFieldsInner(value, type);
            json.WriteEndObject();
        }

        private void WriteFieldsInner(object value, Type type)
        {
            foreach (var field in Fields(type))
            {
                var name = FieldName(field);
                json.WritePropertyName(name);
                _path.Push(name);
                WriteValue(field.GetValue(value), field.FieldType);
                _path.Pop();
            }
        }

        private InvalidOperationException Error(string message) =>
            new($"{string.Join(".", _path.Reverse())}: {message}");

        private void WriteValue(object? value, Type declared)
        {
            if (value is null)
            {
                json.WriteNullValue();
                return;
            }

            var runtime = value.GetType();
            var kind = Classify(declared);
            if (kind == Kind.Nullable)
            {
                WriteValue(value, Nullable.GetUnderlyingType(declared)!);
                return;
            }
            if (kind == Kind.Polymorphic)
            {
                kind = Classify(runtime);
                if (kind != Kind.Object)
                    throw Error($"{TypeName(runtime)} in einem Feld vom Typ {TypeName(declared)} wird nicht unterstützt.");
            }
            else if (runtime != declared && kind is not (Kind.ReadOnlySequence or Kind.ReadOnlyMap))
                throw Error($"Laufzeittyp {TypeName(runtime)} weicht vom Feldtyp {TypeName(declared)} ab.");

            switch (kind)
            {
                case Kind.String:
                    json.WriteStringValue((string)value);
                    return;
                case Kind.Bool:
                    json.WriteBooleanValue((bool)value);
                    return;
                case Kind.Integer:
                    if (value is ulong unsigned)
                        json.WriteNumberValue(unsigned);
                    else
                        json.WriteNumberValue(Convert.ToInt64(value, CultureInfo.InvariantCulture));
                    return;
                case Kind.Floating:
                    json.WriteStringValue(value switch
                    {
                        double d => Invariant(d),
                        float f => f.ToString("R", CultureInfo.InvariantCulture),
                        decimal m => m.ToString(CultureInfo.InvariantCulture),
                        _ => throw Error("Zahl"),
                    });
                    return;
                case Kind.Enum:
                    json.WriteStringValue(value.ToString());
                    return;
                case Kind.Player:
                    var player = (Player)value;
                    if (player.Id <= 0)
                        throw Error($"Spieler {player.Name} hat keine ID.");
                    if (_players.TryGetValue(player.Id, out var known) && !ReferenceEquals(known, player))
                        throw Error($"ID {player.Id} doppelt vergeben.");
                    if (_writtenPlayers.Count > 0 && !_players.ContainsKey(player.Id))
                        throw Error("Verweis auf einen weiteren Spieler aus einem Spieler.");
                    _players[player.Id] = player;
                    WriteReference("$spieler", player.Id);
                    return;
                case Kind.Club:
                    WriteReference("$verein", _clubs.TryGetValue((Club)value, out var club)
                        ? club : throw Error($"Verein {((Club)value).Name} gehört nicht zum Spielstand."));
                    return;
                case Kind.Manager:
                    WriteReference("$manager", _managers.TryGetValue((Manager)value, out var manager)
                        ? manager : throw Error($"Manager {((Manager)value).Name} gehört nicht zum Spielstand."));
                    return;
                case Kind.Game:
                    if (!ReferenceEquals(value, game))
                        throw Error("Verweis auf einen fremden Spielstand.");
                    WriteReference("$spiel", 0);
                    return;
                case Kind.Struct:
                    WriteFields(value, runtime);
                    return;
            }

            // Referenztypen: beim ersten Auftreten mit Nummer ausgeschrieben, danach als Verweis.
            if (_ids.TryGetValue(value, out var existing))
            {
                WriteReference("$ref", existing);
                return;
            }
            var id = _ids.Count;
            _ids.Add(value, id);
            json.WriteStartObject();
            json.WriteNumber("$id", id);
            switch (kind)
            {
                case Kind.Random:
                    var state = ((GameRandom)value).Capture();
                    json.WriteNumber("next", state.Next);
                    json.WriteNumber("nextP", state.NextP);
                    json.WriteStartArray("seed");
                    foreach (var number in state.SeedArray)
                        json.WriteNumberValue(number);
                    json.WriteEndArray();
                    break;
                case Kind.Array or Kind.List or Kind.HashSet or Kind.ReadOnlySequence:
                    WriteItems(value, Arguments(declared)[0]);
                    break;
                case Kind.Array2:
                    var array = (Array)value;
                    json.WriteStartArray("dims");
                    json.WriteNumberValue(array.GetLength(0));
                    json.WriteNumberValue(array.GetLength(1));
                    json.WriteEndArray();
                    WriteItems(value, declared.GetElementType()!);
                    break;
                case Kind.Dictionary or Kind.ReadOnlyMap:
                    var arguments = Arguments(declared);
                    json.WriteStartArray("entries");
                    var index = 0;
                    foreach (var entry in Items(value))
                    {
                        var entryType = entry!.GetType();
                        _path.Push($"[{index++}]");
                        json.WriteStartArray();
                        WriteValue(entryType.GetProperty("Key")!.GetValue(entry), arguments[0]);
                        WriteValue(entryType.GetProperty("Value")!.GetValue(entry), arguments[1]);
                        json.WriteEndArray();
                        _path.Pop();
                    }
                    json.WriteEndArray();
                    break;
                case Kind.Object:
                    if (runtime != declared)
                        json.WriteString("$type", TypeName(runtime));
                    WriteFieldsInner(value, runtime);
                    break;
                default:
                    throw Error($"Art {kind} nicht unterstützt.");
            }
            json.WriteEndObject();
        }

        private void WriteItems(object value, Type element)
        {
            json.WriteStartArray("items");
            var index = 0;
            foreach (var item in Items(value))
            {
                _path.Push($"[{index++}]");
                WriteValue(item, element);
                _path.Pop();
            }
            json.WriteEndArray();
        }

        private void WriteReference(string kind, int value)
        {
            json.WriteStartObject();
            json.WriteNumber(kind, value);
            json.WriteEndObject();
        }
    }
}
