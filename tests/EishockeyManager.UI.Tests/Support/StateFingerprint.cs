using System.Collections;
using System.Reflection;
using System.Text;

namespace EishockeyManager.Tests.Support;

/// <summary>
/// Vollständiger Abdruck eines Objektgraphen über alle Instanzfelder (öffentlich und privat), rekursiv.
/// </summary>
/// <remarks>
/// Liest nur Felder, ruft keine Eigenschaften auf – faul angelegte Werte (Aktienmarkt, Finanz-Zufall …) werden
/// dadurch weder erzeugt noch verändert. Erfasst auch den inneren Zustand von <see cref="Random"/>, Geld,
/// Finanzbuch, Spielerwerte und Transfermarkt. Mehrfach erreichte Objekte erscheinen als Verweis (<c>#n</c>),
/// damit auch Objektidentität (z. B. derselbe Spieler zweimal im Kader) sichtbar bleibt.
/// </remarks>
public static class StateFingerprint
{
    public static string Of(object root)
    {
        var sb = new StringBuilder();
        var seen = new Dictionary<object, int>(ReferenceEqualityComparer.Instance);
        Write(sb, root, seen, 0);
        return sb.ToString();
    }

    /// <summary>Erste abweichende Zeile zweier Abdrücke (für lesbare Fehlermeldungen) oder null.</summary>
    public static string? FirstDifference(string expected, string actual)
    {
        var a = expected.Split('\n');
        var b = actual.Split('\n');
        for (var i = 0; i < Math.Max(a.Length, b.Length); i++)
        {
            var x = i < a.Length ? a[i] : "(Ende)";
            var y = i < b.Length ? b[i] : "(Ende)";
            if (x != y)
                return $"Zeile {i + 1}:\n  vorher:  {x.Trim()}\n  nachher: {y.Trim()}";
        }
        return null;
    }

    private static void Write(StringBuilder sb, object? value, Dictionary<object, int> seen, int depth)
    {
        if (value is null)
        {
            sb.Append("null");
            return;
        }
        var type = value.GetType();
        if (type.IsPrimitive || type.IsEnum || value is string or decimal or DateTime or TimeSpan)
        {
            sb.Append(value is IFormattable f ? f.ToString(null, System.Globalization.CultureInfo.InvariantCulture) : value.ToString());
            return;
        }
        if (value is Delegate or Type or MemberInfo)
        {
            sb.Append('<').Append(type.Name).Append('>');
            return;
        }
        if (!type.IsValueType)
        {
            if (seen.TryGetValue(value, out var id))
            {
                sb.Append('#').Append(id);
                return;
            }
            seen[value] = seen.Count + 1;
            sb.Append('#').Append(seen.Count).Append(' ');
        }

        var indent = new string(' ', 2 * (depth + 1));
        if (value is IEnumerable and not IDictionary && (type.IsArray || type.Namespace?.StartsWith("System.Collections") == true))
        {
            sb.Append(type.Name).Append(" [");
            var i = 0;
            foreach (var item in (IEnumerable)value)
            {
                sb.Append('\n').Append(indent).Append(i++).Append(": ");
                Write(sb, item, seen, depth + 1);
            }
            sb.Append(']');
            return;
        }
        if (value is IDictionary dict)
        {
            sb.Append(type.Name).Append(" {");
            foreach (DictionaryEntry e in dict)
            {
                sb.Append('\n').Append(indent);
                Write(sb, e.Key, seen, depth + 1);
                sb.Append(" = ");
                Write(sb, e.Value, seen, depth + 1);
            }
            sb.Append('}');
            return;
        }

        sb.Append(type.Name).Append(" {");
        for (var t = type; t is not null && t != typeof(object); t = t.BaseType)
        {
            foreach (var field in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                sb.Append('\n').Append(indent).Append(field.Name).Append(" = ");
                Write(sb, field.GetValue(value), seen, depth + 1);
            }
        }
        sb.Append('}');
    }
}
