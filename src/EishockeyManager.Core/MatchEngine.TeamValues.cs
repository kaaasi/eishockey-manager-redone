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
    /// Teamwerte aus dem Lader 0x2EF1B (<see cref="MatchEngine.LoadTeamValues(GameState)"/>, beim Spielstart in
    /// <see cref="NewGame.Create"/>); <c>null</c> nur in Spielständen vor Schema 12, in Aufbauten ohne <see cref="NewGame.Create"/>
    /// und beim Managerverein im Modus „nur Weltkugel“ nach dem HOK-Import (#747).
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
    /// [ORIGINAL] A105/A836: Byte 1 (Sturm) ist bei den Europapokal-Gegnern (Index ab 64) eigen (kb\23.vga 0x16C, z. B. 79/72);
    /// der Lader nimmt es für den Sturm (Datei 0x2F004–0x2F009: Byte <c>[bp−0x8] + si</c>, si = Teil). Das Remake führt die
    /// Teamwerte als Abweichung von <see cref="Club.Strength"/> (Byte 0); die Sturmwerte enthalten deshalb Byte 1 − Byte 0
    /// (bei den Vereinen 0 … 63 sind beide gleich). [APPROX] Ohne Datenbankbytes (Rückfall <see cref="EnsureTeamValues"/>,
    /// Aufbauten ohne <see cref="NewGame.Create"/>) gilt Byte 0 auch für den Sturm.
    /// [ORIGINAL] Zeitpunkt (Spur start-lader, A106): einmal beim Programmstart, Datei 0x439BB <c>call 0xE0:0x75</c> in 0x437D5,
    /// vor der Spielvorbereitung 0x28165 (Datei 0x43A61); danach wandern Kondition, Technik und Form mit
    /// <see cref="DriftTeamValues"/> (Spielstart, Saisonwechsel, zweimal je Woche; <c>docs/spielberechnung.md</c> §15). Das
    /// Remake zieht sie deshalb in <see cref="NewGame.Create"/> als erste Züge aus <see cref="GameState.Random"/>, vor der
    /// Vereinswahl (<see cref="StartGame"/> schließt den Spielstart ab). Beim Laden eines Spielstands gibt 0x28165 AL ≠ 0 zurück,
    /// Datei 0x43A6E springt über die Wanderung des Spielstarts nach 0x43AE2: die Werte des Laders werden durch die gesicherten
    /// Vereinssätze ersetzt (Spielstände des Remakes ab Schema 12 führen <see cref="Club.TeamValues"/>; HOK-Import 0x18–0x1D).
    /// [APPROX] Ältere Spielstände ohne Teamwerte (vor Schema 12) und Aufbauten ohne <see cref="NewGame.Create"/> ziehen sie
    /// weiterhin beim ersten Bedarf (<see cref="EnsureTeamValues"/>).
    /// </summary>
    public static void LoadTeamValues(GameState game) => LoadTeamValues(game.Clubs, game.Random);

    /// <summary>Lader 0x2EF1B über eine Vereinsliste (siehe <see cref="LoadTeamValues(GameState)"/>), für den Programmstart vor
    /// dem Aufbau des Spielstands (<see cref="NewGame.Create"/>).</summary>
    /// <param name="attackBytes">Datenbankbyte 1 (Sturm) je Vereinsindex (A105/A836); ohne Eintrag Byte 0 (auch bei Byte 1 = 0, das
    /// in kb\23.vga bei keinem Verein vorkommt – nur in Testdatenbanken ohne Byte 1).</param>
    public static void LoadTeamValues(IEnumerable<Club> clubs, Random random, IReadOnlyDictionary<int, int>? attackBytes = null)
    {
        foreach (var club in clubs.OrderBy(c => c.Index))
        {
            var dayForm = OriginalMatch.Rnd(random, DayFormMin, DayFormMax);
            var v = club.Strength;
            var attack = attackBytes is not null && attackBytes.TryGetValue(club.Index, out var a) ? a : v;
            int Spread(int value)
            {
                var t = Math.Max(0, value / 10);
                return -OriginalMatch.Rnd(random, 0, t) + OriginalMatch.Rnd(random, 0, t);
            }
            int Form() => OriginalMatch.Rnd(random, 45, 55) - (club.Index > 64 ? 5 : 0);
            var dc = Spread(v);
            var dt = Spread(v);
            var df = Form();
            var ac = attack - v + Spread(attack);
            var at = attack - v + Spread(attack);
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
    /// [ORIGINAL] Spur zufallsstrom (A107, N3 = B): Reihenfolge = Satzfolge des Originals (<see cref="DriftOrder"/>), nicht der
    /// Vereinsindex des Remakes – der Satz i liegt im Original am Ligaplatz (0x34FD5(i): Sätze 0–11 Bundesliga, 12–23 2. Liga,
    /// 24–35 Oberliga, danach die übrigen Vereine, ab [0x6AD] die Nationalteams).
    /// [APPROX] Kondition und Technik als Abweichung vom Datenbankwert (<see cref="Club.Strength"/>) gespeichert, die Liga-Grenzen
    /// gelten für Datenbankwert + Abweichung. Nationalteams spielen als <c>RatedNation</c> mit eigenen Werten; ihre Form wird nur
    /// gezogen und gesetzt.
    /// </summary>
    public static void DriftTeamValues(GameState game, TeamValueDrift drift)
    {
        var random = game.Random;
        var mode = (int)drift;
        var b = drift == TeamValueDrift.Season ? 3 : 1;
        foreach (var club in DriftOrder(game))
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

    /// <summary>
    /// Vereine in der Reihenfolge der Wanderung: Satzfolge der drei Ligen und der Plätze ohne Liga
    /// (<see cref="GameState.ClubRecordOrder"/>), dann die übrigen Sätze bis zu den Nationalteams nach Index. [ORIGINAL] 0x9D1B
    /// zählt die Vereinssätze i = 0 … [0x6AD] + 11 (Datei 0x09D23–0x09D33, 0x09F41); die Sätze stehen in der Folge der letzten
    /// Ligamischung (Datei 0x35EDD). [APPROX] Saisonwechsel: Das Original wandert am 29. April nach dem Platztausch von Auf- und
    /// Absteigern und vor der Mischung der neuen Saison (Datei 0x56448 vor 0x5644E); das Remake wandert in
    /// <see cref="GameState.StartNextSeason"/> und geht dort schon nach der gemischten Folge der neuen Saison. Vereine ohne Liga
    /// außerhalb der Auswahl (Index 48–51) und Vereine, die in keiner geführten Folge stehen, folgen nach Index.
    /// </summary>
    public static IReadOnlyList<Club> DriftOrder(GameState game)
    {
        ArgumentNullException.ThrowIfNull(game);
        var order = game.ClubRecordOrder().Where(i => i < game.Clubs.Count && i < NationalTeams.First).Distinct().ToList();
        var seen = order.ToHashSet();
        order.AddRange(game.Clubs.Where(c => c.Index <= NationalTeams.Last && !seen.Contains(c.Index)).Select(c => c.Index).Order());
        return order.Select(i => game.Clubs[i]).ToList();
    }

    /// <summary>[ORIGINAL] Tage der kleinen Wanderung im Tageslauf 0x54C9F: Tageszähler ≡ 0 oder 4 (mod 7).</summary>
    public static bool IsDriftDay(int day) => day % 7 is 0 or 4;

    /// <summary>
    /// Abschluss des Spielstarts nach der Spielvorbereitung (<see cref="NewGame.Create"/>):
    /// <list type="number">
    /// <item>[ORIGINAL] Teamwerte der Managervereine aus der Aufstellung: in der Managerschleife der Spielvorbereitung nach der
    ///   Aufstellung 0x426BC <c>0x952C(m, 0)</c> (Datei 0x2A067–0x2A078, Schleife ab Manager <c>[bp−0x64]</c> = 0 beim neuen
    ///   Spiel bis DGROUP 0x386, Datei 0x29B57–0x2A0A3; A519/A080). Im Modus „nur Weltkugel“ nicht (#747, wie
    ///   <see cref="GameState.UpdateManagerTeamValues"/>).</item>
    /// <item>[ORIGINAL] Wanderung des Spielstarts Modus 10 (Datei 0x43A73, nach 0x28165 und vor der Auslosung des DEB-Pokals
    ///   0x341AB(0, 0) bei Datei 0x43A8C; A108). Der Europapokal ist dann schon ausgelost (Datei 0x298B1 in 0x28165; Spur
    ///   zufallsstrom, Teil 4).</item>
    /// </list>
    /// </summary>
    internal static void StartGame(GameState game)
    {
        game.UpdateManagerTeamValues();
        DriftTeamValues(game, TeamValueDrift.Season);
    }

    /// <summary>
    /// [APPROX] Rückfall für Spielstände ohne Teamwerte (vor Schema 12) und Aufbauten ohne <see cref="NewGame.Create"/>: zieht
    /// die Teamwerte, falls ein Verein des Spielstands noch keine hat (siehe <see cref="LoadTeamValues(GameState)"/>), und
    /// wendet die Wanderung des Spielstarts an (<see cref="DriftTeamValues"/>). Neue Spiele haben sie ab dem Spielstart.
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
        // [ORIGINAL] Wanderung des Spielstarts (0x43A73, Modus 10); [APPROX] hier erst beim ersten Bedarf (siehe oben).
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
    /// r = random(0, 2 · |d|), 0x17 + r bei d &gt; 0, sonst − r; begrenzt auf 47 … 53 (0x120:0x52). WM-Gruppe: nach allen Spielen
    /// des Termins (A109, <c>WorldCup.PlayDay</c>).
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
