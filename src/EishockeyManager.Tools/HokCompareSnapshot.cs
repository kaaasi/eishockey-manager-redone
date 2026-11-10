using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using EishockeyManager.Core;

namespace EishockeyManager.Tools;

/// <summary>Ein Wert des Abdrucks: lesbarer Pfad, Schemafeld (<c>Typ.Feld</c> wie in <see cref="SaveGame.SchemaDescription"/>) und Wert.</summary>
/// <param name="Container">Nächstes übergeordnetes Schemafeld, das zufallsabhängig oder saisongebunden ist (z. B. Spieler auf dem
/// Transfermarkt), sonst null: Felder darunter erben dessen Klasse.</param>
public sealed record HokCompareEntry(string Path, string Field, string Value, string? Container = null);

/// <summary>
/// Abdruck eines Spielstands für den Abgleich (Spur abgleich-original, Teil 1): alle Instanzfelder wie im eigenen Spielstandformat
/// (<see cref="SaveGame"/>: Feldnamen ohne Backing-Field-Hülle, Typnamen ohne Namensraum), rekursiv als flache Liste.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Vereine nach Index, Manager nach Index; Verweise auf Vereine, Manager und Spieler als Text („Verein 2“, „Spieler NAME“),
///   damit zwei unabhängig importierte Stände vergleichbar bleiben (Spieler-IDs vergibt der Import neu, Feldkarte Regel 3).</item>
/// <item>Spieler werden nur dort ganz abgedruckt, wo das HOK sie einzeln führt (Feldkarte Regel 3): Kader der Managervereine,
///   Transfermarkt, Pool, Nationalkader. Schlüssel ist der Name (Doppelte mit „#2“ …), nicht die Listenposition – Zu- und
///   Abgänge verschieben sonst alle Folgezeilen. Die Kader der Computervereine erzeugt der Import aus der Datenbank; sie stehen
///   nicht im HOK und fehlen im Abdruck.</item>
/// <item>Felder, deren Klasse <see cref="HokCompareClass.NotInHok"/> ist, werden nicht betreten (<see cref="HokCompareClasses"/>).</item>
/// </list>
/// </remarks>
public static class HokCompareSnapshot
{
    private static readonly Assembly[] StateAssemblies = [typeof(GameState).Assembly, typeof(EishockeyManager.Formats.ClubDatabase).Assembly];

    /// <summary>Felder, über die ein Spieler vollständig abgedruckt wird (sonst nur als Verweis).</summary>
    private static readonly HashSet<string> OwningPlayerFields = new(StringComparer.Ordinal)
    {
        "Club.Players", "TransferListing.Player", "PoolSlot.Player", "NationalSquadMember.Player",
    };

    public static IReadOnlyList<HokCompareEntry> Of(GameState game)
    {
        ArgumentNullException.ThrowIfNull(game);
        var walker = new Walker(game);
        walker.Object(game, typeof(GameState), "", "GameState");
        return walker.Entries;
    }

    /// <summary>Typname wie <c>SaveGame.TypeName</c> (ohne Namensraum, Generika in spitzen Klammern).</summary>
    public static string TypeName(Type type)
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

    private static readonly Regex BackingField = new("^<(?<name>[^>]+)>k__BackingField$", RegexOptions.CultureInvariant);

    /// <summary>Instanzfelder wie <c>SaveGame.Fields</c>: eigene und geerbte (aus Core/Formats), nach Feldname sortiert.</summary>
    public static IReadOnlyList<(FieldInfo Field, string Name)> Fields(Type type)
    {
        var fields = new List<(FieldInfo, string)>();
        for (var t = type; t is not null && t != typeof(object) && t != typeof(ValueType)
             && (StateAssemblies.Contains(t.Assembly) || t.IsValueType); t = t.BaseType)
            foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                var name = BackingField.Match(f.Name) is { Success: true } m ? m.Groups["name"].Value : f.Name;
                fields.Add((f, f.DeclaringType == type || type.IsValueType ? name : $"{TypeName(f.DeclaringType!)}.{name}"));
            }
        return fields.OrderBy(f => f.Item2, StringComparer.Ordinal).ToList();
    }

    private sealed class Walker(GameState game)
    {
        private readonly HashSet<int> _managerClubs = game.Managers.Select(m => m.ClubIndex).ToHashSet();
        private readonly HashSet<object> _active = new(ReferenceEqualityComparer.Instance);

        public List<HokCompareEntry> Entries { get; } = [];

        private string? _container;

        private void Add(string path, string field, string value) => Entries.Add(new HokCompareEntry(path, field, value, _container));

        /// <summary>Felder eines Objekts; <paramref name="owner"/> = Schemafeld, über das es erreicht wurde (für Werte ohne Felder).</summary>
        public void Object(object value, Type declared, string path, string owner)
        {
            var type = value.GetType();
            if (!_active.Add(value) && !type.IsValueType)
            {
                Add(path, owner, "(Zyklus)");
                return;
            }
            try
            {
                var typeName = TypeName(type);
                var fields = Fields(type);
                if (fields.Count == 0)
                {
                    Add(path, owner, Scalar(value));
                    return;
                }
                // ValueTuple und andere Hilfsstrukturen: Klasse vom Feld, über das sie erreicht wurden (Feldkarte ohne Tupelfelder).
                var helper = type.IsValueType && type.IsGenericType;
                foreach (var (field, name) in fields)
                {
                    var key = helper ? owner : $"{typeName}.{name}";
                    var rule = HokCompareClasses.Of(key);
                    if (rule.Class == HokCompareClass.NotInHok)
                        continue;
                    var outer = _container;
                    if (outer is null && (rule.Class == HokCompareClass.Random || rule.SeasonBound))
                        _container = key;
                    try
                    {
                        Value(field.GetValue(value), field.FieldType, Join(path, name), key);
                    }
                    finally
                    {
                        _container = outer;
                    }
                }
            }
            finally
            {
                _active.Remove(value);
            }
        }

        private void Value(object? value, Type declared, string path, string field)
        {
            switch (value)
            {
                case null:
                    Add(path, field, "null");
                    return;
                case Player p:
                    if (OwningPlayerFields.Contains(field))
                        Object(p, typeof(Player), path, field);
                    else
                        Add(path, field, Reference(p));
                    return;
                case Club c when field != "GameState.Clubs":
                    Add(path, field, $"Verein {c.Index}");
                    return;
                case Manager m when field != "GameState.Managers":
                    Add(path, field, $"Manager {game.Managers.ToList().IndexOf(m)}");
                    return;
                case Random:
                    return; // Generatorzustand: nicht im HOK (Feldkarte Regel 6)
                case string or Enum:
                    Add(path, field, Scalar(value));
                    return;
            }
            var type = value.GetType();
            if (type.IsPrimitive || value is decimal)
            {
                Add(path, field, Scalar(value));
                return;
            }
            if (value is Array { Rank: 2 } grid)
            {
                for (var i = 0; i < grid.GetLength(0); i++)
                    for (var j = 0; j < grid.GetLength(1); j++)
                        Value(grid.GetValue(i, j), type.GetElementType()!, $"{path}[{i},{j}]", field);
                return;
            }
            if (value is IDictionary dictionary)
            {
                foreach (var key in dictionary.Keys.Cast<object>().OrderBy(Scalar, StringComparer.Ordinal))
                    Value(dictionary[key], typeof(object), $"{path}[{Scalar(key)}]", field);
                return;
            }
            if (IsSet(type))
            {
                var items = ((IEnumerable)value).Cast<object?>().Select(i => i switch
                {
                    Player p => p.Name,
                    Manager m => $"Manager {game.Managers.ToList().IndexOf(m)}",
                    _ => Scalar(i),
                }).OrderBy(s => s, StringComparer.Ordinal);
                Add(path, field, "{" + string.Join(",", items) + "}");
                return;
            }
            if (value is IEnumerable sequence)
            {
                Sequence(sequence, path, field);
                return;
            }
            Object(value, declared, path, field);
        }

        private void Sequence(IEnumerable sequence, string path, string field)
        {
            var items = sequence.Cast<object?>().ToList();
            if (field == "Club.Players")
            {
                // Kader der Computervereine erzeugt der Import (Feldkarte Regel 3): nur die Größe vergleichen.
                var club = ClubIndexOf(path);
                if (club is { } index && !_managerClubs.Contains(index))
                    return;
            }
            Add($"{path}.Count", field, items.Count.ToString(CultureInfo.InvariantCulture));
            var byName = items.Count > 0 && items.All(i => i is Player or TransferListing or NationalSquadMember);
            var seen = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < items.Count; i++)
            {
                string key;
                if (byName)
                {
                    var name = items[i] switch
                    {
                        Player p => p.Name,
                        TransferListing l => l.Player.Name,
                        NationalSquadMember n => n.Player.Name,
                        _ => "",
                    };
                    seen[name] = seen.GetValueOrDefault(name) + 1;
                    key = seen[name] == 1 ? name : $"{name}#{seen[name]}";
                }
                else if (items[i] is Club c && field == "GameState.Clubs")
                    key = $"{c.Index} {c.Name}";
                else
                    key = i.ToString(CultureInfo.InvariantCulture);
                Value(items[i], typeof(object), $"{path}[{key}]", field);
            }
        }

        private static int? ClubIndexOf(string path)
        {
            var m = Regex.Match(path, @"^Clubs\[(\d+) ");
            return m.Success ? int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) : null;
        }

        private static bool IsSet(Type type) =>
            type.IsGenericType && type.GetGenericTypeDefinition() == typeof(HashSet<>);

        private static string Reference(Player p) => $"Spieler {p.Name}";

        private static string Join(string path, string name) => path.Length == 0 ? name : $"{path}.{name}";
    }

    internal static string Scalar(object? value) => value switch
    {
        null => "null",
        double d => d.ToString("R", CultureInfo.InvariantCulture),
        float f => f.ToString("R", CultureInfo.InvariantCulture),
        IFormattable x => x.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "",
    };
}
