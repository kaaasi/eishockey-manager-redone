using System.Globalization;
using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Tools;

/// <summary>Eine Zeile des Abgleichs: Feld, Wert im Original (Import von b), Wert im Remake (a nachgespielt), Klasse.</summary>
public sealed record HokCompareRow(string Path, string Field, string? Original, string? Remake, HokCompareClass Class, bool Deviation,
    string Note)
{
    /// <summary>Kurzform für erwartete Listen: <c>Pfad: Original → Remake</c>.</summary>
    public override string ToString() => $"{Path}: {Original ?? "(fehlt)"} → {Remake ?? "(fehlt)"}";
}

/// <summary>Ergebnis eines Paars.</summary>
public sealed record HokCompareReport(string Pair, string? PlayError, IReadOnlyList<HokCompareRow> Rows)
{
    public IEnumerable<HokCompareRow> Deviations => Rows.Where(r => r.Deviation);

    public int Count(HokCompareClass kind) => Rows.Count(r => r.Class == kind);
}

/// <summary>
/// <c>emtool abgleich &lt;a.hok&gt; &lt;b.hok&gt; [--gamedata ordner] [--seed n] [--alle]</c> (Spur abgleich-original, Teil 1):
/// Stand a importieren, ohne Eingriff bis zum Halt von b spielen (<see cref="GameState.Continue"/>), dann jedes Feld mit dem
/// Import von b vergleichen. Feldklassen: <see cref="HokCompareClasses"/> (R15-D2 = A).
/// </summary>
/// <remarks>
/// [APPROX] „Ohne Eingriff“ heißt im Remake: je Halt einmal <see cref="GameState.OnMainMenu"/> (Aufbau des Hauptmenüs, wie
/// <c>MainMenuScreen</c>), dann „Tag weiter“; am Saisonende <see cref="GameState.StartNextSeason"/> (ausgelaufene Verträge enden wie
/// „Abbruch“), der erste aktive Manager ist wieder am Zug, im Modus „nur Weltkugel“ ein weiterer Tag (wie
/// <c>SeasonEndSummaryScreen</c>). Fragen und Angebote bleiben unbeantwortet. Der Import von b ist der geladene Stand (mit
/// Ladeeigenheit am Markierungstag), nicht der gesicherte.
/// </remarks>
public static class HokCompare
{
    public const int DefaultSeed = 1993;

    /// <summary>Höchstzahl der „Tag weiter“-Schritte (eine Saison mit vier Managern braucht weniger als 2000).</summary>
    public const int MaxSteps = 20_000;

    public static int Run(IReadOnlyList<string> args)
    {
        if (args.Count < 2)
        {
            Console.Error.WriteLine("Aufruf: emtool abgleich <a.hok> <b.hok> [--gamedata ordner] [--seed n] [--alle]");
            return 1;
        }
        string? gameData = null;
        var seed = DefaultSeed;
        var all = false;
        try
        {
            for (var i = 2; i < args.Count; i++)
            {
                switch (args[i])
                {
                    case "--gamedata": gameData = Next(args, ref i); break;
                    case "--seed": seed = int.Parse(Next(args, ref i), CultureInfo.InvariantCulture); break;
                    case "--alle": all = true; break;
                    default: throw new ArgumentException($"Unbekannte Option „{args[i]}“.");
                }
            }
            var database = ClubDatabase.Load(GameData.Locate(gameData));
            var report = Compare(args[0], args[1], database, seed);
            Print(report, all, Console.Out);
            return report.PlayError is null && !report.Deviations.Any() ? 0 : 4;
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or OverflowException or InvalidDataException)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static string Next(IReadOnlyList<string> args, ref int i) =>
        ++i < args.Count ? args[i] : throw new ArgumentException($"Nach {args[i - 1]} fehlt ein Wert.");

    /// <summary>Name des Paars „a→b“ aus den Dateinamen ohne Endung (Schlüssel für <see cref="HokCompareClasses.Interventions"/>).</summary>
    public static string PairName(string a, string b) =>
        $"{Path.GetFileNameWithoutExtension(a).ToUpperInvariant()}→{Path.GetFileNameWithoutExtension(b).ToUpperInvariant()}";

    /// <summary>Paar aus zwei Dateien vergleichen.</summary>
    public static HokCompareReport Compare(string a, string b, ClubDatabase database, int seed = DefaultSeed)
    {
        var start = Import(a, database, seed);
        var target = Import(b, database, seed);
        var pair = PairName(a, b);
        var crossesSeason = start.Season != target.Season;
        var error = PlayTo(start, target);
        var interventions = HokCompareClasses.Interventions.GetValueOrDefault(pair) ?? [];
        return new HokCompareReport(pair, error, Rows(target, start, crossesSeason, interventions));
    }

    public static GameState Import(string path, ClubDatabase database, int seed)
    {
        var result = OriginalImport.TryImport(File.ReadAllBytes(path), database, seed);
        if (result.Game is { } game)
            return game;
        throw new InvalidDataException(result.ChecksumError
            ? $"{path}: Prüfsumme falsch"
            : $"{path} kann nicht importiert werden: {result.Error}");
    }

    /// <summary>Halt eines Spielstands: Saison, Tag, Phase, Manager am Zug, Zug des Bundestrainers.</summary>
    public static string Halt(GameState g) =>
        $"{g.DateText} (Saison {g.Season}, Tag {g.CurrentDay}, {g.Phase}, Manager {g.CurrentManager}{(g.NationalTurn ? ", Bundestrainer" : "")})";

    /// <summary>
    /// Spielt <paramref name="game"/> ohne Eingriff bis zum Halt von <paramref name="target"/>. Liefert null oder den Grund, warum
    /// der Halt nicht erreicht wurde.
    /// </summary>
    public static string? PlayTo(GameState game, GameState target)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(target);
        game.OnMainMenu(game.ActiveManager, isReturn: false); // Laden: erster Aufbau des Hauptmenüs
        for (var step = 0; step < MaxSteps; step++)
        {
            if (SameHalt(game, target))
                return null;
            if (Order(game).CompareTo(Order(target)) > 0)
                return $"Halt von b übersprungen: Remake bei {Halt(game)}, Ziel {Halt(target)}";
            switch (game.Phase)
            {
                case SeasonPhase.GameOver:
                    return $"Spielende vor dem Halt von b: {Halt(game)}";
                case SeasonPhase.SeasonOver:
                    game.StartNextSeason();
                    game.ReturnToFirstActiveManager();
                    if (game.WorldOnly)
                        game.Continue();
                    break;
                default:
                    game.Continue();
                    break;
            }
            game.OnMainMenu(game.ActiveManager, isReturn: false);
        }
        return $"Halt von b nach {MaxSteps} Schritten nicht erreicht: {Halt(game)}";
    }

    private static bool SameHalt(GameState g, GameState t) =>
        g.Season == t.Season && g.CurrentDay == t.CurrentDay && g.Phase == t.Phase && g.CurrentManager == t.CurrentManager
        && g.NationalTurn == t.NationalTurn;

    private static (int, int, int) Order(GameState g) => (g.Season, g.CurrentDay, g.Phase == SeasonPhase.SeasonOver ? 1 : 0);

    /// <summary>Zeilen des Vergleichs Original (<paramref name="original"/>) ↔ Remake (<paramref name="remake"/>).</summary>
    public static IReadOnlyList<HokCompareRow> Rows(GameState original, GameState remake, bool crossesSeason,
        IReadOnlyCollection<string> interventions)
    {
        var left = Entries(original);
        var right = Entries(remake);
        var rows = new List<HokCompareRow>();
        foreach (var path in left.Keys.Union(right.Keys).Order(StringComparer.Ordinal))
        {
            left.TryGetValue(path, out var o);
            right.TryGetValue(path, out var r);
            var entry = (o ?? r)!;
            var field = entry.Field;
            var rule = HokCompareClasses.Of(field);
            var kind = rule.ClassFor(crossesSeason);
            // Unter einem zufallsabhängigen Behälter (Markt, Pool, Liga nach dem Saisonwechsel) ist auch ein festes Feld zufallsabhängig.
            if (kind is HokCompareClass.Exact or HokCompareClass.Random && entry.Container is { } container && container != field
                && HokCompareClasses.Of(container).ClassFor(crossesSeason) == HokCompareClass.Random)
                (kind, rule) = (HokCompareClass.Random, HokCompareClasses.Of(container));
            if (HokCompareClasses.Matches(path, interventions))
                kind = HokCompareClass.Intervention;
            var (deviation, note) = Judge(kind, rule, o?.Value, r?.Value);
            rows.Add(new HokCompareRow(path, field, o?.Value, r?.Value, kind, deviation, note));
        }
        return rows;
    }

    private static (bool Deviation, string Note) Judge(HokCompareClass kind, HokCompareRule rule, string? original, string? remake)
    {
        switch (kind)
        {
            case HokCompareClass.Intervention or HokCompareClass.NotInHok:
                return (false, "");
            case HokCompareClass.Unclassified:
                return (true, "nicht eingeordnet");
            case HokCompareClass.Exact:
                return original == remake ? (false, "") : (true, original is null ? "fehlt im Original" : remake is null ? "fehlt im Remake" : "");
        }
        // Zufallsabhängig: Vorhandensein ist nicht geprüft (Markt, Kader, Nachrichten wechseln); Bereich nur für Zahlen.
        if (original is null || remake is null || rule.Tolerance is null && rule.Relative is null)
            return (false, "");
        if (!double.TryParse(original, NumberStyles.Float, CultureInfo.InvariantCulture, out var o)
            || !double.TryParse(remake, NumberStyles.Float, CultureInfo.InvariantCulture, out var r))
            return (original != remake, "Bereich: kein Zahlenwert");
        var limit = Math.Max(rule.Tolerance ?? 0, (rule.Relative ?? 0) * Math.Abs(o));
        return Math.Abs(r - o) <= limit ? (false, "") : (true, $"außerhalb ±{limit.ToString("0.##", CultureInfo.InvariantCulture)}");
    }

    private static Dictionary<string, HokCompareEntry> Entries(GameState game) =>
        HokCompareSnapshot.Of(game).Concat(Invariants(game)).ToDictionary(e => e.Path, StringComparer.Ordinal);

    /// <summary>
    /// Invarianten zufallsabhängiger Felder (R15-D2 = A): gelten im Original wie im Remake unabhängig vom Zufall und werden exakt
    /// verglichen. Spiele je Verein der laufenden Hauptrunde (min–max) und Zahl der Ergebnisse je Liga.
    /// </summary>
    public static IEnumerable<HokCompareEntry> Invariants(GameState game)
    {
        foreach (var league in game.Schedules.Keys.Order())
        {
            var clubs = game.Clubs.Where(c => c.League == league).Select(c => c.Index).ToList();
            var results = game.Results.Where(r => r.League == league).ToList();
            var games = clubs.Select(c => results.Count(r => r.Home == c || r.Away == c)).DefaultIfEmpty(0).ToList();
            yield return new HokCompareEntry($"Invariante.Ergebnisse[{league}]", "Invariante.Ergebnisse",
                results.Count.ToString(CultureInfo.InvariantCulture));
            yield return new HokCompareEntry($"Invariante.SpieleJeVerein[{league}]", "Invariante.SpieleJeVerein",
                $"{games.Min()}–{games.Max()}");
        }
        var pool = game.TransferMarket.PoolPlayers.ToList();
        yield return new HokCompareEntry("Invariante.PoolSpieler", "Invariante.PoolSpieler", pool.Count.ToString(CultureInfo.InvariantCulture));
        foreach (var (name, value) in new (string, Func<Player, int>)[]
                 { ("Talent", p => p.Talent), ("Technique", p => p.Technique), ("Condition", p => p.Condition), ("Strain", p => p.Strain) })
            yield return new HokCompareEntry($"Invariante.PoolMittel.{name}", "Invariante.PoolMittel",
                (pool.Count == 0 ? 0 : pool.Average(value)).ToString("0.#", CultureInfo.InvariantCulture));
        yield return new HokCompareEntry("Invariante.Vereine", "Invariante.Vereine", game.Clubs.Count.ToString(CultureInfo.InvariantCulture));
        foreach (var (manager, index) in game.Managers.Select((m, i) => (m, i)))
        {
            var squad = game.Clubs[manager.ClubIndex].Players;
            yield return new HokCompareEntry($"Invariante.Torhüter[{index}]", "Invariante.Torhüter",
                (squad.Count(p => p.Role == PlayerRole.Goalkeeper) > 0).ToString());
        }
    }

    /// <summary>Tabelle (Feld, Original, Remake, Klasse): Abweichungen, mit <paramref name="all"/> jede Zeile; dazu die Summen je Klasse.</summary>
    public static void Print(HokCompareReport report, bool all, TextWriter output)
    {
        output.WriteLine($"Abgleich {report.Pair}");
        if (report.PlayError is { } error)
            output.WriteLine($"  NICHT ERREICHT: {error}");
        output.WriteLine($"{"Feld",-70} {"Original",-24} {"Remake",-24} Klasse");
        foreach (var row in report.Rows.Where(r => all || r.Deviation))
            output.WriteLine($"{Cut(row.Path, 70),-70} {Cut(row.Original ?? "(fehlt)", 24),-24} {Cut(row.Remake ?? "(fehlt)", 24),-24} " +
                             $"{Label(row.Class)}{(row.Deviation ? " ABWEICHUNG" : "")}{(row.Note.Length > 0 ? " – " + row.Note : "")}");
        output.WriteLine($"Zeilen {report.Rows.Count}: deterministisch {report.Count(HokCompareClass.Exact)}, zufallsabhängig " +
                         $"{report.Count(HokCompareClass.Random)}, Eingriff {report.Count(HokCompareClass.Intervention)}, nicht eingeordnet " +
                         $"{report.Count(HokCompareClass.Unclassified)}; Abweichungen {report.Deviations.Count()}");
    }

    public static string Label(HokCompareClass kind) => kind switch
    {
        HokCompareClass.Exact => "deterministisch",
        HokCompareClass.Random => "zufallsabhängig",
        HokCompareClass.Intervention => "Eingriff",
        HokCompareClass.NotInHok => "nicht im HOK",
        _ => "nicht eingeordnet",
    };

    private static string Cut(string s, int n) => s.Length <= n ? s : s[..(n - 1)] + "…";
}
