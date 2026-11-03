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
/// <item>[ORIGINAL] Gelesen werden die Vereinsfelder 0x18 + k (Kondition, Datei 0x5149A), 0x1A + k (Technik, 0x514BD) und
///   0x1C + k (Form, 0x514E3) für k = 0 (Abwehr), 1 (Sturm), der Trendbalken aus Vereinsfeld 0x17 (Tagesform, 0x51900) –
///   keine Spielerwerte (Spur wettbewerbe-spiel, Teil 4; A200, A201; <c>docs/spielberechnung.md</c> §21).</item>
/// <item>Computervereine: [ORIGINAL] Teamwerte des Laders 0x2EF1B samt Wanderung 0x9D1B (<see cref="MatchEngine.ClubFieldValues"/>,
///   dieselben Werte wie in der Minutenrechnung).</item>
/// <item>Managervereine: [ORIGINAL] 0x952C schreibt die Felder 0x18–0x1D im Tageslauf 0x54F9C–0x54FBF für jeden Manager aus
///   Reihe 0–2 der Aufstellung (<see cref="MatchEngine.LineupTeamValues"/>). [APPROX] Aus der aktuellen Aufstellung statt dem
///   Stand des letzten Tageslaufs; ohne Aufstellung die Felder des Laders.</item>
/// <item>Länderteams: Kondition und Technik aus <see cref="GameState.NationalRatings"/> (DGROUP 0x4C4E, Deutschland vor der
///   WM neu ausgewürfelt), Form und Tagesform aus den Feldern des Vereinsplatzes. Land des Bundestrainers mit eigener
///   Aufstellung: Kondition, Technik und Form aus der Aufstellung (0x952C, #193).</item>
/// <item>[APPROX] Vor dem ersten Spiel eines Spielstands sind die Teamwerte noch nicht gezogen
///   (<see cref="MatchEngine.LoadTeamValues"/>): Streuung 0, Form und Tagesform 50 (<see cref="MatchEngine.TeamValuesOf"/>).</item>
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
            var (defense, attack) = FieldValues(game, club);
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
            // [ORIGINAL] Trendbalken = Vereinsfeld 0x17 (Datei 0x51900), begrenzt wie 0x4F527 auf 47 … 53.
            var trend = Math.Clamp(MatchEngine.TeamValuesOf(club).DayForm, MatchEngine.DayFormMin, MatchEngine.DayFormMax);
            rows.Add((new StrengthRow(club, Clamp(c / div), Clamp(t / div), Clamp(f / div), sum / 3 / div, sum, trend), order++));
        }
        return rows.OrderByDescending(r => r.Row.Sum).ThenBy(r => r.Order).Select(r => r.Row).ToList();
    }

    private static int Clamp(int v) => Math.Clamp(v, 0, 99);

    /// <summary>
    /// (Kondition, Technik, Form) für Abwehr und Sturm aus den Vereinsfeldern 0x18–0x1D – wie die Stärketabelle
    /// (<see cref="Compute"/>) und für die Finanzen (Zuschauer <c>SpectatorRules.Strength</c>, Datei 0x4D316; Spur geld, Teil 2).
    /// </summary>
    public static ((double C, double T, double F) Defense, (double C, double T, double F) Attack) TeamValues(GameState game, Club club) => FieldValues(game, club);

    /// <summary>(Kondition, Technik, Form) für Abwehr und Sturm aus den Vereinsfeldern 0x18–0x1D (siehe Klassenbeschreibung).</summary>
    private static ((double C, double T, double F) Defense, (double C, double T, double F) Attack) FieldValues(GameState game, Club club)
    {
        if (club.Index is >= NationalTeams.First and <= NationalTeams.Last)
        {
            // #193: [ORIGINAL] Das Land des Bundestrainers zeigt die Teamwerte, die 0x952C mit Manager 5 aus dessen
            // Aufstellung schreibt (Tageslauf 0x54F9C–0x54FBF, nach jedem Freundschaftsspiel 0x555D6; DOSBox 27–29 → 41–43).
            if (game.NationalCoach is { } coach && coach.Nation == club.Index && coach.Formation.Count > 0)
                return Of(MatchEngine.LineupTeamValues(coach.Formation));
            var loaded = MatchEngine.TeamValuesOf(club);
            var (kd, td, ks, ts) = game.NationalRatings(club.Index);
            return ((kd, td, loaded.DefenseForm), (ks, ts, loaded.AttackForm));
        }
        if (game.ActiveManagers.Any(m => m.ClubIndex == club.Index) && club.Lineup.Count > 0)
            return Of(MatchEngine.LineupTeamValues(club.Lineup));
        var (defense, attack) = MatchEngine.ClubFieldValues(club);
        return ((defense.Condition, defense.Technique, defense.Form), (attack.Condition, attack.Technique, attack.Form));

        static ((double, double, double), (double, double, double)) Of((PartValues D, PartValues A, int) v) =>
            ((v.D.Condition, v.D.Technique, v.D.Form), (v.A.Condition, v.A.Technique, v.A.Form));
    }
}
