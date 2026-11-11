namespace EishockeyManager.Core;

/// <summary>Modus der Wanderung <see cref="MatchEngine.DriftTeamValues"/> (em.exe 0x9D1B; jedes Argument ≠ 1 wirkt als 3).</summary>
public enum TeamValueDrift
{
    /// <summary>Tageslauf, zweimal je Woche: kleiner Schritt, begrenzt auf 1 … 99.</summary>
    Weekly = 1,

    /// <summary>Spielstart und Saisonwechsel (Argument 10): großer Schritt, begrenzt auf die Grenzen der Liga.</summary>
    Season = 3,
}

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
    /// Spielvorbereitung); danach wandern Kondition, Technik und Form mit <see cref="DriftTeamValues"/> (Spielstart,
    /// Saisonwechsel, zweimal je Woche; <c>docs/spielberechnung.md</c> §15). [APPROX] Das Remake zieht sie beim ersten
    /// Spiel eines Spielstands aus <see cref="GameState.Random"/>, damit Spielstart und Kaderaufbau unverändert bleiben,
    /// und wendet dort gleich die Wanderung des Spielstarts an (0x43A73); ältere Spielstände ebenso beim nächsten Spiel.
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

    /// <summary>
    /// [ORIGINAL] Grenzen der großen Wanderung je Liga: DGROUP 0x125 (Paare Minimum, Maximum), Index 0x34FD5 / 2. Bestätigt
    /// an <c>FILE01.HOK</c> (neues Spiel, nach der Wanderung des Spielstarts gesichert): alle Computervereine darin, mehrere
    /// genau auf den Grenzen (Rosenheim 69, Düsseldorf 94, Memmingen 39, Bonn 15).
    /// </summary>
    public static (int Min, int Max) DriftBounds(League? league) => league switch
    {
        League.Bundesliga => (69, 94),
        League.ZweiteLiga => (39, 73),
        _ => (15, 43),
    };

    /// <summary>
    /// Wanderung der Teamwerte. [ORIGINAL] em.exe Datei 0x9D1B (Basis 0x62C:0x1C5B), Argument Modus (≠ 1 gilt als 3):
    /// je Vereinsplatz i = 0 … [0x6AD] + 11 in dieser Reihenfolge –
    /// <list type="number">
    /// <item>Vereine (i &lt; [0x6AD] = 52): Liga L = 0x34FD5(i) / 2 (Bundesliga 0, Zweite Liga 1, sonst 2). r = lrandom(0, 2·Modus),
    ///   b = 3 (Modus 3) bzw. 1, Schritt s = lrandom(0, 2r + 2b) − (Modus + b), also −6 … 2r bzw. −2 … 2r, Mittel 0.
    ///   Kondition 0x18 + k (k = 0, 1): + lrandom(s − 1, s + 1). Technik 0x1A + k: lrandom(0, 2) ziehen; bei 0 oder Modus 3
    ///   + lrandom(s − 1, s + 1). Begrenzt mit 0x120:0x52 auf die Liga-Grenzen DGROUP 0x125 (69 … 94, 39 … 73, 15 … 43)
    ///   bzw. bei Modus 1 auf 1 … 99.</item>
    /// <item>Alle Plätze einschließlich der Nationalteams: Form 0x1C + k = lrandom(45, 55) für k = 0, 1, 2 (die Schleife
    ///   läuft bis k &lt; 3 und überschreibt auch Feld 0x1E; im Remake nur gezogen).</item>
    /// </list>
    /// Aufrufe: Spielstart Modus 10 (0x43A73, nach der Ligamischung und der Spielvorbereitung), Saisonwechsel Modus 10
    /// (0x56448, nach Auf- und Abstieg), Tageslauf 0x54C9F Modus 1 an Tagen mit Tageszähler 0x3B7 ≡ 0 oder 4 (mod 7)
    /// (0x5525A–0x55292). Die Europapokal-Gegner (Index &gt; 64) wandern nicht.
    /// [APPROX] Reihenfolge nach Vereinsindex statt nach Tabellenplatz des Originals; Kondition und Technik als Abweichung
    /// vom Datenbankwert (<see cref="Club.Strength"/>) gespeichert, die Liga-Grenzen gelten für Datenbankwert + Abweichung.
    /// Nationalteams spielen als <c>RatedNation</c> mit eigenen Werten; ihre Form wird nur gezogen und gesetzt.
    /// </summary>
    public static void DriftTeamValues(GameState game, TeamValueDrift drift)
    {
        var random = game.Random;
        var mode = (int)drift;
        var b = drift == TeamValueDrift.Season ? 3 : 1;
        foreach (var club in game.Clubs.Where(c => c.Index <= NationalTeams.Last).OrderBy(c => c.Index))
        {
            if (club.TeamValues is not { } v)
                continue;
            if (club.Index < NationalTeams.First)
            {
                var (min, max) = drift == TeamValueDrift.Season ? DriftBounds(club.League) : (1, 99);
                var r = OriginalMatch.Rnd(random, 0, 2 * mode);
                var step = OriginalMatch.Rnd(random, 0, 2 * r + 2 * b) - (mode + b);
                int Move(int deviation) =>
                    Math.Clamp(club.Strength + deviation + OriginalMatch.Rnd(random, step - 1, step + 1), min, max) - club.Strength;
                int Clamp(int deviation) => Math.Clamp(club.Strength + deviation, min, max) - club.Strength;
                var dc = Move(v.DefenseCondition);
                var ac = Move(v.AttackCondition);
                var dt = OriginalMatch.Rnd(random, 0, 2) == 0 || drift == TeamValueDrift.Season ? Move(v.DefenseTechnique) : Clamp(v.DefenseTechnique);
                var at = OriginalMatch.Rnd(random, 0, 2) == 0 || drift == TeamValueDrift.Season ? Move(v.AttackTechnique) : Clamp(v.AttackTechnique);
                v = v with { DefenseCondition = dc, AttackCondition = ac, DefenseTechnique = dt, AttackTechnique = at };
            }
            var df = OriginalMatch.Rnd(random, 45, 55);
            var af = OriginalMatch.Rnd(random, 45, 55);
            _ = OriginalMatch.Rnd(random, 45, 55); // Feld 0x1E (siehe oben)
            club.TeamValues = v with { DefenseForm = df, AttackForm = af };
        }
    }

    /// <summary>[ORIGINAL] Tage der kleinen Wanderung im Tageslauf 0x54C9F: Tageszähler ≡ 0 oder 4 (mod 7).</summary>
    public static bool IsDriftDay(int day) => day % 7 is 0 or 4;

    /// <summary>
    /// Zieht die Teamwerte, falls ein Verein des Spielstands noch keine hat (siehe <see cref="LoadTeamValues"/>), und
    /// wendet die Wanderung des Spielstarts an (<see cref="DriftTeamValues"/>).
    /// [ORIGINAL] #747: Im Modus „nur Weltkugel“ trägt der Vereinssatz des Managervereins das Land (Regel 1 der Feldkarte); der
    /// Import lässt seine Teamwerte leer. Das zählt nicht als „noch nicht gezogen“ – sonst würfelte das erste Spiel alle
    /// importierten Teamwerte neu (FILE18 → FILE48: Teamwerte unverändert, gleicher Tag 218). Der Verein spielt in diesem Modus
    /// nicht; Anzeigen nutzen <see cref="TeamValuesOf"/>.
    /// </summary>
    public static void EnsureTeamValues(GameState game)
    {
        if (!game.Clubs.Any(c => c.TeamValues is null && !(game.WorldOnly && game.Managers.Any(m => m.ClubIndex == c.Index))))
            return;
        LoadTeamValues(game);
        // [ORIGINAL] Wanderung des Spielstarts (0x43A73, Modus 10); [APPROX] beim ersten Spiel (siehe LoadTeamValues).
        DriftTeamValues(game, TeamValueDrift.Season);
    }

    /// <summary>Teamwerte eines Vereins; ohne gezogene Werte (z. B. Nationalteam als <c>RatedNation</c>) neutral.</summary>
    public static ClubTeamValues TeamValuesOf(Club club) =>
        club.TeamValues ?? new ClubTeamValues(0, 0, 0, 0, club.Index > 64 ? 45 : 50, club.Index > 64 ? 45 : 50, 50);

    /// <summary>
    /// Vereinsfelder 0x18–0x1D eines Computervereins (Abwehr, Sturm): Kondition und Technik = <see cref="ComputerTeamValue"/> +
    /// Streuung des Laders bzw. Stand der Wanderung, Form 0x1C/0x1D. Dieselben Werte nutzt die Minutenrechnung
    /// (<see cref="LiveTeamOf"/>) und die Stärketabelle (<see cref="StrengthTable"/>, 0x51206). [ORIGINAL] Der Lader begrenzt
    /// nicht (bis v + v/10); begrenzt wird erst beim Lesen (0x977F bzw. 0x120:0x52 in 0x51206).
    /// </summary>
    public static (PartValues Defense, PartValues Attack) ClubFieldValues(Club club)
    {
        var value = ComputerTeamValue(club);
        var v = TeamValuesOf(club);
        return (new PartValues(value + v.DefenseCondition, value + v.DefenseTechnique, v.DefenseForm),
            new PartValues(value + v.AttackCondition, value + v.AttackTechnique, v.AttackForm));
    }

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
