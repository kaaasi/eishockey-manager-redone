namespace EishockeyManager.Core;

/// <summary>
/// Bilanzzeichen je Team (Spur statistik-anzeige, B29/A361; <c>docs/archiv/analyse/re-abweichungen.md</c> B15 und B29).
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>[ORIGINAL] Das Original führt je Team acht Zeichen im Teamsatz (DGROUP 0x825E + 0x50 · Team; „s/S“ Sieg, „n/N“
///   Niederlage, „u/U“ Unentschieden, klein = Heim, groß = Gast, DGROUP 0x7CE–0x7D3). 0x50283 schiebt sie je Spiel über 0x4F4F4
///   (Datei 0x5078A–0x5079A, 0x5091D–0x5092D, 0x50B0D–0x50B3B), aber nur bei Wettbewerbscode 7 (Liga) und 8 (WM-Gruppe); der
///   Saisonwechsel setzt alle 64 Teams auf „--------“ (Datei 0x55EBA–0x55EE0).</item>
/// <item>Vereine: Die Ligaergebnisse der laufenden Saison (<see cref="Results"/>) sind dieselbe Menge – nur Code-7-Spiele, geleert
///   im selben Schritt des Saisonwechsels (<see cref="StartNextSeason"/>) und beim Import aus den Spieltagen aufgebaut. Die Zeichen
///   werden daraus abgeleitet, ein eigenes Feld im Spielstand braucht es nicht. Länder: <see cref="NationalForm"/>.</item>
/// <item>[ORIGINAL] Gesicht 0x51AAF: Stimmung = 2 + Siege − Niederlagen der acht Zeichen, erst danach auf 1 … 3 begrenzt
///   (1 rot, 2 gelb, 3 grün; Nachbildung <c>b15_gegnerfenster.py</c>, <c>b29_staerken.py</c>). Groß- und Kleinschreibung
///   (Heim/Gast) ändern weder Zeichen noch Gesicht.</item>
/// </list>
/// </remarks>
public sealed partial class GameState
{
    /// <summary>
    /// Bilanzzeichen eines Teams: acht Felder, das letzte das jüngste Spiel (0 = leer, 1 = Niederlage, 2 = Unentschieden,
    /// 3 = Sieg). Vereine aus den Ligaspielen der Saison, Länder aus den WM-Gruppenspielen (<see cref="NationalForm"/>).
    /// </summary>
    public IReadOnlyList<int> TeamForm(int team)
    {
        if (team is >= NationalTeams.First and <= NationalTeams.Last)
            return NationalForm(team);
        var form = new int[NationalFormLength];
        var last = Results.Where(r => r.Involves(team)).TakeLast(form.Length).ToList();
        for (var i = 0; i < last.Count; i++)
        {
            var r = last[i];
            var (f, a) = r.Home == team ? (r.HomeGoals, r.AwayGoals) : (r.AwayGoals, r.HomeGoals);
            form[form.Length - last.Count + i] = f > a ? 3 : f == a ? 2 : 1;
        }
        return form;
    }

    /// <summary>[ORIGINAL] Stimmung des Gesichts 0x51AAF: 2 + Siege − Niederlagen, begrenzt auf 1 (rot) … 3 (grün).</summary>
    public static int FormMood(IReadOnlyList<int> form) =>
        Math.Clamp(2 + form.Count(c => c == 3) - form.Count(c => c == 1), 1, 3);
}
