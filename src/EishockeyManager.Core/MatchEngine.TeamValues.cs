namespace EishockeyManager.Core;

/// <summary>
/// Teamwerte eines Vereins aus dem Lader der Datenbank (em.exe Datei 0x2EF1B, Ov 0xE0 Offset 0x4D3B), gespeichert als
/// Abweichung vom Datenbankwert (Kondition, Technik) bzw. als Wert (Form, Tagesform). Spur spielstaerke-manager (#204).
/// </summary>
/// <param name="DefenseCondition">Vereinsfeld 0x18 minus Datenbankwert: −random(0, v/10) + random(0, v/10).</param>
/// <param name="AttackCondition">Vereinsfeld 0x19 minus Datenbankwert.</param>
/// <param name="DefenseTechnique">Vereinsfeld 0x1A minus Datenbankwert.</param>
/// <param name="AttackTechnique">Vereinsfeld 0x1B minus Datenbankwert.</param>
/// <param name="DefenseForm">Vereinsfeld 0x1C: random(45, 55), Europapokal-Gegner (Index &gt; 64) 5 weniger.</param>
/// <param name="AttackForm">Vereinsfeld 0x1D wie 0x1C.</param>
/// <param name="DayForm">Vereinsfeld 0x17: random(47, 53), nach jedem Ligaspiel nachgeführt (0x4F527).</param>
public readonly record struct ClubTeamValues(
    int DefenseCondition, int AttackCondition, int DefenseTechnique, int AttackTechnique,
    int DefenseForm, int AttackForm, int DayForm);

public sealed partial class Club
{
    /// <summary>
    /// Teamwerte aus dem Lader 0x2EF1B (<see cref="MatchEngine.LoadTeamValues"/>); <c>null</c>, solange sie noch nicht
    /// gezogen sind (neues Spiel vor dem ersten Spiel, Spielstand vor Schema 12).
    /// </summary>
    public ClubTeamValues? TeamValues { get; set; }
}

public static partial class MatchEngine
{
    /// <summary>[ORIGINAL] Grenzen der Tagesform (Vereinsfeld 0x17): Lader 0x2EFE0 random(0x2F, 0x35), 0x4F586 begrenzt ebenso.</summary>
    public const int DayFormMin = 47, DayFormMax = 53;

    /// <summary>
    /// Lader der Teamwerte. [ORIGINAL] em.exe Datei 0x2EFDE–0x2F158, je Verein i = 0 … 149 in dieser Reihenfolge:
    /// <list type="number">
    /// <item>0x17 = random(47, 53).</item>
    /// <item>Je Teil k = 0 (Abwehr), 1 (Sturm) mit v = Datenbankbyte k, t = v / 10: Kondition 0x18 + k =
    ///   v − random(0, t) + random(0, t), dann Technik 0x1A + k ebenso, dann Form 0x1C + k = random(45, 55) − 5 · (i &gt; 64).</item>
    /// <item>Nationalteams (Index 52 … 63): Kondition und Technik aus DGROUP 0x4C4E überschrieben (ohne Streuung) – im
    ///   Remake über <c>RatedNation</c>, hier Abweichung 0.</item>
    /// </list>
    /// [APPROX] Für Byte 1 (Sturm) nimmt das Remake wie bisher die Vereinsstärke (Byte 0; bei den Vereinen 0 … 63 sind
    /// beide gleich). Zeitpunkt: das Original lädt einmal beim Programmstart (0x437D5 → 0x2EF1B vor der
    /// Spielvorbereitung) und ändert die Werte danach nicht mehr (außer 0x17 und den Länderwerten Deutschlands; Suche nach
    /// Schreibzugriffen auf die Vereinsfelder 0x17–0x1D, <c>docs/spielberechnung.md</c> §12). [APPROX] Das Remake zieht
    /// sie beim ersten Spiel eines Spielstands aus <see cref="GameState.Random"/>, damit Spielstart und Kaderaufbau
    /// unverändert bleiben; ältere Spielstände bekommen sie ebenso beim nächsten Spiel.
    /// </summary>
    public static void LoadTeamValues(GameState game)
    {
        var random = game.Random;
        foreach (var club in game.Clubs.OrderBy(c => c.Index))
        {
            var dayForm = OriginalMatch.Rnd(random, DayFormMin, DayFormMax);
            var v = club.Strength;
            var t = Math.Max(0, v / 10);
            int Spread() => -OriginalMatch.Rnd(random, 0, t) + OriginalMatch.Rnd(random, 0, t);
            int Form() => OriginalMatch.Rnd(random, 45, 55) - (club.Index > 64 ? 5 : 0);
            var dc = Spread();
            var dt = Spread();
            var df = Form();
            var ac = Spread();
            var at = Spread();
            var af = Form();
            if (club.Index is >= NationalTeams.First and <= NationalTeams.Last)
                dc = dt = ac = at = 0;
            club.TeamValues = new ClubTeamValues(dc, ac, dt, at, df, af, dayForm);
        }
    }

    /// <summary>Zieht die Teamwerte, falls ein Verein des Spielstands noch keine hat (siehe <see cref="LoadTeamValues"/>).</summary>
    public static void EnsureTeamValues(GameState game)
    {
        if (game.Clubs.Any(c => c.TeamValues is null))
            LoadTeamValues(game);
    }

    /// <summary>Teamwerte eines Vereins; ohne gezogene Werte (z. B. Nationalteam als <c>RatedNation</c>) neutral.</summary>
    public static ClubTeamValues TeamValuesOf(Club club) =>
        club.TeamValues ?? new ClubTeamValues(0, 0, 0, 0, club.Index > 64 ? 45 : 50, club.Index > 64 ? 45 : 50, 50);

    /// <summary>
    /// Tagesform nach einem Ligaspiel. [ORIGINAL] em.exe Datei 0x4F527 (Ov 0x128, gerufen aus der Tabellenrechnung
    /// 0x50283 mit d = eigene − gegnerische Tore, nur bei Wettbewerbscode [0xD0C2] = 7 Liga bzw. 8 WM-Gruppe):
    /// r = random(0, 2 · |d|), 0x17 + r bei d &gt; 0, sonst − r; begrenzt auf 47 … 53 (0x120:0x52).
    /// [APPROX] WM-Spiele ohne Nachführung (Nationalteams spielen im Remake als <c>RatedNation</c> ohne eigene Werte).
    /// </summary>
    public static void UpdateDayForm(Club club, int goalDifference, Random random)
    {
        if (club.TeamValues is not { } values)
            return;
        var r = OriginalMatch.Rnd(random, 0, 2 * Math.Abs(goalDifference));
        var form = goalDifference > 0 ? values.DayForm + r : values.DayForm - r;
        club.TeamValues = values with { DayForm = Math.Clamp(form, DayFormMin, DayFormMax) };
    }
}
