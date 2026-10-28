using EishockeyManager.Formats;

namespace EishockeyManager.Core;

/// <summary>Ansicht der Stärken-Tabelle (Knöpfe „Gesamt“, „Abwehr“, „Sturm“).</summary>
public enum StrengthView
{
    Gesamt,
    Abwehr,
    Sturm,
}

/// <summary>Eine Zeile der Stärken-Tabelle.</summary>
public sealed record StrengthRow(Club Club, int Condition, int Technique, int Form, int Average, int Sum, int Trend);

/// <summary>
/// Stärken der Vereine einer Liga oder der Länderteams (Hauptmenü → Stärken).
/// </summary>
/// <remarks>
/// [ORIGINAL] em.exe Datei 0x51206: Je Verein Kondition (Vereinsfeld 0x18/0x19), Technik (0x1A/0x1B) und Form
/// (0x1C/0x1D) für Abwehr bzw. Sturm. „Gesamt“ zeigt das Mittel aus Abwehr und Sturm, „Abwehr“/„Sturm“ den
/// jeweiligen Wert; jede Spalte auf 0–99 begrenzt (0x120:0x52). „Mittel“ = (Kondition + Technik + Form) / 3 der
/// ungerundeten Summen. Sortiert absteigend nach dieser Summe (stabil in Datenbankreihenfolge).
/// Belegt am Video (13:18): Russland Gesamt 99 99 51 → Mittel 85, Kanada 98 93 53 → 81.
/// <list type="bullet">
/// <item>Länderteams: Kondition und Technik aus <see cref="GameState.NationalRatings"/> (Tabelle in em.exe, Deutschland vor der WM neu ausgewürfelt).</item>
/// <item>[APPROX] Ligavereine: Das Original führt Teamwerte je Verein (aus der Datenbankstärke ± 10 %); das Remake
///   hat stattdessen Spielerwerte. Abwehr = Mittel der Verteidiger der Aufstellung, Sturm = Mittel der Stürmer.</item>
/// <item>[APPROX] Form der Länderteams und aller Vereine: Mittel der Spielerform (Original: random(45, 55) je Feld).</item>
/// <item>[APPROX] Trendbalken: Das Original zeigt Vereinsfeld 0x17 (zu Spielbeginn random(47, 53), Bedeutung
///   unbekannt); hier aus der Form abgeleitet, 47 … 53.</item>
/// </list>
/// </remarks>
public static class StrengthTable
{
    public static IReadOnlyList<StrengthRow> Compute(GameState game, IEnumerable<Club> clubs, StrengthView view)
    {
        var rows = new List<(StrengthRow Row, int Order)>();
        var order = 0;
        foreach (var club in clubs)
        {
            var (defense, attack) = Values(game, club);
            var parts = view switch
            {
                StrengthView.Abwehr => new[] { defense },
                StrengthView.Sturm => [attack],
                _ => [defense, attack],
            };
            var div = parts.Length;
            int Sum(Func<(double C, double T, double F), double> f) => (int)parts.Sum(f);
            var c = Sum(p => p.C);
            var t = Sum(p => p.T);
            var f = Sum(p => p.F);
            var sum = c + t + f;
            var formAverage = (defense.F + attack.F) / 2;
            var trend = Math.Clamp(50 + (int)Math.Round((formAverage - 50) * 0.6), 47, 53);
            rows.Add((new StrengthRow(club, Clamp(c / div), Clamp(t / div), Clamp(f / div), sum / 3 / div, sum, trend), order++));
        }
        return rows.OrderByDescending(r => r.Row.Sum).ThenBy(r => r.Order).Select(r => r.Row).ToList();
    }

    private static int Clamp(int v) => Math.Clamp(v, 0, 99);

    /// <summary>(Kondition, Technik, Form) für Abwehr und Sturm.</summary>
    private static ((double C, double T, double F) Defense, (double C, double T, double F) Attack) Values(GameState game, Club club)
    {
        var form = Average(club.Players, p => p.Form, 50);
        if (club.Index is >= NationalTeams.First and <= NationalTeams.Last)
        {
            var (kd, td, ks, ts) = game.NationalRatings(club.Index);
            var defenders = club.Players.Where(p => p.Role == PlayerRole.Defender);
            var forwards = club.Players.Where(p => p.Role == PlayerRole.Forward);
            return ((kd, td, Average(defenders, p => p.Form, form)), (ks, ts, Average(forwards, p => p.Form, form)));
        }
        var lineup = club.Lineup.Players.ToList();
        var d = lineup.Where(p => p.Role == PlayerRole.Defender).ToList();
        var a = lineup.Where(p => p.Role == PlayerRole.Forward).ToList();
        if (d.Count == 0) d = club.Players.Where(p => p.Role == PlayerRole.Defender).ToList();
        if (a.Count == 0) a = club.Players.Where(p => p.Role == PlayerRole.Forward).ToList();
        (double, double, double) Of(List<Player> ps) =>
            (Average(ps, p => p.Condition, club.Strength), Average(ps, p => p.Technique, club.Strength), Average(ps, p => p.Form, form));
        return (Of(d), Of(a));
    }

    private static double Average(IEnumerable<Player> players, Func<Player, int> value, double fallback)
    {
        var list = players.ToList();
        return list.Count == 0 ? fallback : list.Average(value);
    }
}
