using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens.Competitions;

/// <summary>Ergebnis eines Pokalspiels in der Übersicht: Tore von links und rechts, dazu 0–2 Punkte dahinter.</summary>
/// <param name="Dots">Marken des Spiels (<see cref="CupTie.MarksOf"/>): 0 = reguläre Spielzeit, 1 = Penaltyschießen (schneller Weg
/// 0x37761, Minutenweg 0x39789; Endstand = Gleichstand + Penaltytore), 2 = Tor in der Verlängerung (0x38402 auf den schon markierten
/// Stand; eisbaeren_f44_01330 „4: 3..“). [ORIGINAL] Je + 30 im Ergebnis ein Punkt (Anzeige Datei 0x338AB–0x338E3).</param>
public readonly record struct DebCupResult(int Left, int Right, int Dots);

/// <summary>Eine Paarung der laufenden Pokalrunde; <see cref="Results"/> hat einen Eintrag je gespieltem Termin (null = nicht ausgetragen).</summary>
public sealed record DebCupRow(int LeftClub, int RightClub, IReadOnlyList<DebCupResult?> Results);

/// <summary>Eine Gruppe „Bundesliga GEGEN Zweite Liga:“ usw.; ohne Paarung zeigt der Bildschirm Striche.</summary>
public sealed record DebCupGroup(string Title, IReadOnlyList<DebCupRow> Rows);

/// <summary>Zustand der Übersicht „DEB-Pokalspiele“ (nur gelesen aus <see cref="DebCup"/>, ändert nichts).</summary>
/// <param name="DatesPlayed">Termine der laufenden Runde, die schon vorbei sind (Spalten der Ergebnisse; 0–3).</param>
/// <param name="NextDay">Nächster Pokaltag ab dem aktuellen Tag, null wenn keiner mehr kommt.</param>
/// <param name="RoundName">Rundenname in der Datumszeile (<see cref="RoundTitle"/>), leer in der ersten Runde ab der zweiten Saison.</param>
public sealed record DebCupOverview(int Round, int DatesPlayed, int? NextDay, IReadOnlyList<DebCupGroup> Groups, string RoundName)
{
    /// <summary>
    /// Rundenname der Datumszeile. [ORIGINAL] Datei 0x3322E–0x3324A: Index in die Namenstabelle DGROUP 0xB5C („“, „Erste Runde “,
    /// „Achtelfinale “ … „Endspiel “) ist das Byte DGROUP 0x7AD. Es steht beim Programmstart auf 1, wird erst beim Ziehen der
    /// Runden 2–5 auf die neue Rundennummer 2–5 gesetzt (0x348E2) und beim Saisonwechsel auf 0 gelöscht (Schleife 0x565D3–0x565EC
    /// über DGROUP 0x7AC–0x7B0). Die erste Runde heißt also nur in der ersten Saison „Erste Runde“, danach bleibt der Name leer
    /// (Videobilder: „Erste Runde“ `oberflaeche_0400` im ersten Spieljahr; nur das Datum `duell_s2a_01217`, `folge13_02545`).
    /// [ORIGINAL] Ein geladener Spielstand der ersten Saison zeigt hier ebenfalls „Erste Runde“: der Block Datei 0x0903B sichert
    /// DGROUP 0x7A9–0x7B0, also auch 0x7AD (formate/spielstand-original.md).
    /// </summary>
    public static string RoundTitle(int round, int season) =>
        round == 0 && season != GameState.FirstSeason ? "" : DebCup.RoundNames[round];

    /// <summary>Spielklassen der Gruppen in der Reihenfolge des Originals (Klasse 1, 2, 4 wie <see cref="CupClass.Of"/>).</summary>
    private static readonly (int Higher, int Lower)[] GroupClasses = [(1, 1), (1, 2), (1, 4), (2, 2), (2, 4), (4, 4)];

    private static readonly Dictionary<int, string> ClassNames = new() { [1] = "Bundesliga", [2] = "Zweite Liga", [4] = "Oberliga" };

    public static string GroupTitle((int Higher, int Lower) classes) => $"{ClassNames[classes.Higher]} GEGEN {ClassNames[classes.Lower]}:";

    /// <summary>Übersicht der laufenden Runde oder null vor der Auslosung (kein Pokal).</summary>
    public static DebCupOverview? Of(GameState game)
    {
        if (game.DebCup is not { CurrentRound: >= 0 } cup)
            return null;
        var round = cup.CurrentRound;
        var dates = Enumerable.Range(0, DebCup.DatesPerRound).Select(g => DebCup.DayOf(round, g)).ToList();
        var played = dates.Count(d => d < game.CurrentDay);

        var rows = cup.Rounds[round].Select(tie => (Tie: tie, Key: KeyOf(game, tie))).ToList();
        var groups = GroupClasses.Select(g => new DebCupGroup(
            GroupTitle(g),
            rows.Where(r => r.Key == g).Select(r => Row(r.Tie, dates, played)).ToList())).ToList();

        int? next = SeasonCalendar.DaysOf(DayEvents.DebCup).Where(d => d >= game.CurrentDay).Select(d => (int?)d).FirstOrDefault();
        return new DebCupOverview(round, played, next, groups, RoundTitle(round, game.Season));
    }

    private static (int, int) KeyOf(GameState game, CupTie tie)
    {
        var (a, b) = (CupClass.Of(game.Clubs[tie.First]), CupClass.Of(game.Clubs[tie.Second]));
        return a <= b ? (a, b) : (b, a);
    }

    /// <summary>
    /// Eine Paarung nach <paramref name="played"/> vergangenen Terminen. [ORIGINAL] Nach jedem der beiden ersten Termine tauscht
    /// em.exe die Paarung samt den bisherigen Ergebnissen (Datei 0x34DCD–0x34E7C), damit links der Verein mit Heimrecht im nächsten
    /// Spiel steht: vor dem ersten Termin links der Verein mit Heimrecht im ersten Spiel, nach dem ersten Termin der andere (die
    /// Ergebnisse links:rechts folgen den Namen), nach dem zweiten wieder wie zuerst. Nach dem dritten Termin stellt die Funktion
    /// 0x34ED6 die Paarung für das Endspiel wieder her, die Zwischenrunden werden sofort neu ausgelost.
    /// </summary>
    public static DebCupRow Row(CupTie tie, IReadOnlyList<int> dates, int played)
    {
        var swapped = played == 1;
        var (left, right) = swapped ? (tie.Second, tie.First) : (tie.First, tie.Second);
        var results = new List<DebCupResult?>();
        for (var g = 0; g < played; g++)
        {
            var index = tie.Games.FindIndex(m => m.Day == dates[g]);
            if (index < 0)
                results.Add(null);
            else
            {
                var match = tie.Games[index];
                var leftIsHome = match.Home == left;
                results.Add(new DebCupResult(
                    leftIsHome ? match.HomeGoals : match.AwayGoals,
                    leftIsHome ? match.AwayGoals : match.HomeGoals,
                    tie.MarksOf(index)));
            }
        }
        return new DebCupRow(left, right, results);
    }
}

/// <summary>
/// „DEB-Pokalspiele“: die Paarungen der laufenden Pokalrunde nach Spielklassen gruppiert, mit den Ergebnissen der bereits
/// gespielten Termine. Zeigt nur vorhandenen Zustand, verändert nichts.
/// </summary>
/// <remarks>
/// Kachel „Tabelle DEB-Pokal“ (Hauptmenü → Tabellen), Funktion Datei 0x32E23 in em.exe (Stub 0xE8:0x75). Der Aufbau ist mit
/// einem eigenen 8086-Interpreter an synthetischem DGROUP-Zustand abgelesen (Zeichenaufrufe der Funktion mitgeschrieben):
/// <list type="bullet">
/// <item>[ORIGINAL] Zeilen: n = 1 + Paarungen + leere Gruppen; Rahmenhöhe h' = min(11, (7 · (n + 4) + 60) / 19) Teile zu 19
///   Pixeln (mindestens 2, gezeichnet h' + 1), Rahmen ab x = 5 (13 Teile = 312 Pixel breit), Oberkante y = (217 − 19 · h') / 2.</item>
/// <item>[ORIGINAL] Titel „DEB-Pokalspiele“ (Schrift Bold, weiß, Schatten Farbe 1) zentriert 0 … 319 auf Grundlinie y + 19, Pokalbild
///   (kb\36, Quelle 0 | 22, 22 × 22) bei x = 13 und x = 286, y + 4. Datumszeile (Flow, Farbe 30, Schatten 0) „Erste Runde    22.9.“ auf
///   y + 34: Rundenname (im Original mit einem Leerzeichen am Ende), insgesamt vier Leerzeichen und der erste Pokaltag ab dem aktuellen Tag.</item>
/// <item>[ORIGINAL] Gruppen ab y + 46 in der Reihenfolge Bundesliga/Bundesliga, B/Zweite, B/Ober, Z/Z, Z/Ober, Ober/Ober: Überschrift
///   (Mini, weiß, Schatten 0, x 15 … 183), darunter je Paarung eine Zeile im Abstand 7 (erste Zeile Überschrift + 8, die nächste
///   Überschrift 7 unter der letzten Zeile); ohne Paarung eine Zeile aus neun Strichen. Name links (x 15 … 94, Farbe 30), „-“ (98 … 100,
///   weiß), Name rechts (104 … 183, Farbe 1), Vereine der Manager gelb (24), alle ohne Schatten.</item>
/// <item>[ORIGINAL] Namen und Ergebnisse: Die Paarung wird nach dem ersten Termin getauscht (links der Verein mit Heimrecht im nächsten
///   Spiel), die Ergebnisse folgen den Namen – im Emulator an der Rundenverarbeitung (Datei 0x34B76) nachgespielt, siehe <see cref="Row"/>.
///   Nach dem letzten Pokaltag entfällt die Datumszeile (Datei 0x332B6: Vergleich mit dem letzten Termin, Tag 211), der Rundenname bleibt.</item>
/// <item>[ORIGINAL] Ergebnisse „ 3: 2“ (zweistellig aufgefüllt, Mini) ab x = 207 − 10 · (Termine − 1) im Abstand 36, Farbe 1, 30, 1
///   nach Terminnummer. Spiele, die nicht mehr nötig waren (Serie entschieden), bleiben leer. Der Wert „+ 30“ bzw. „+ 60“ im
///   Ergebnis setzt einen bzw. zwei Punkte hinter das Ergebnis (x + 22 und x + 24).</item>
/// </list>
/// [APPROX]
/// <list type="bullet">
/// <item>Zwei Punkte nach einem Tor in der Verlängerung stützen sich auf das Videobild (Herkunft des Stands vor 0x38402 nicht verfolgt,
///   siehe <see cref="CupMatch"/>). [ORIGINAL] Ein Punkt ist ein Pixel: Aufruf 0x50:0x3B0 ohne Farbargument (Zeichenfarbe des Ergebnisses)
///   bei x + 22 + 2 · i auf der Textzeile (Datei 0x339BD–0x339E3); am Video eisbaeren_f44_01330 ein Pixel auf der Grundlinie.</item>
/// <item>Ein Klick auf einen Verein im Original wird nicht nachgebildet; jede Taste und jeder Klick führt zurück.</item>
/// </list>
/// </remarks>
public sealed class DebCupGamesScreen : Screen
{
    public const string Title = "DEB-Pokalspiele";
    public const int FrameX = 5;
    public const int FramePartWidth = 24;
    public const int FramePartHeight = 19;
    public const int FrameParts = 13;
    public const int TrophyGraphic = EuropeanCupScreen.TrophyGraphic;
    public static readonly Rect TrophySource = new(0, 22, 22, 22);
    public const int RowPitch = 7;

    /// <summary>[ORIGINAL] Farben der Ergebnisse je Terminnummer (1., 2., 3. Spiel).</summary>
    public static readonly byte[] ResultColors = [1, 30, 1];

    private readonly Screen _back;
    private readonly DebCupOverview? _fixed;
    private readonly IReadOnlyCollection<int>? _highlighted;

    public DebCupGamesScreen(ScreenHost host, Screen back) : base(host) => _back = back;

    /// <summary>Feste Übersicht statt des laufenden Pokals, z. B. für Bildschirmfotos wie im Video; <paramref name="highlighted"/>
    /// sind die gelb gezeigten Vereine (sonst die Vereine der Manager).</summary>
    public DebCupGamesScreen(ScreenHost host, Screen back, DebCupOverview overview, IReadOnlyCollection<int>? highlighted = null)
        : base(host) => (_back, _fixed, _highlighted) = (back, overview, highlighted);

    public DebCupOverview? Overview => _fixed ?? (Host.Game is { } game ? DebCupOverview.Of(game) : null);

    /// <summary>Teile des Rahmens (ohne Zusatzteil), Oberkante und Gesamtteile für <paramref name="pairs"/> Paarungen und <paramref name="emptyGroups"/> leere Gruppen.</summary>
    public static (int Parts, int Y) FrameFor(int pairs, int emptyGroups)
    {
        var n = 1 + pairs + emptyGroups;
        var h = Math.Min(11, (7 * (n + 4) + 60) / 19);
        return (Math.Max(h, 2) + 1, (217 - 19 * h) / 2);
    }

    /// <summary>Überschrift einer Gruppe, wie sie gezeichnet wird (Großbuchstaben).</summary>
    public static string GroupHeading(DebCupGroup group) => group.Title.ToUpperInvariant();

    public static int ResultX(int dates, int index) => 207 - 10 * (dates - 1) + 36 * index;

    public override void Update(InputState input)
    {
        if (input.LeftClick || input.RightClick || input.Keys.Count > 0)
            Host.Show(_back);
    }

    public override void Draw(Surface s)
    {
        s.Palette = Assets.Palette;
        s.Blit(Assets.Image(MainMenuScreen.Background), 0, 0);
        if (Overview is not { } overview || Host.Game is not { } game)
        {
            Widgets.Frame(s, Assets, new Rect(FrameX, 40, FramePartWidth * FrameParts, FramePartHeight * 3), FrameStyle.Orange);
            CompetitionWidgets.Centered(s, Assets.Bold, 0, 319, 80, Title, Colors.White, DebCupDrawScreen.TitleShadow);
            return;
        }

        var empty = overview.Groups.Count(g => g.Rows.Count == 0);
        var pairs = overview.Groups.Sum(g => g.Rows.Count);
        var (parts, y) = FrameFor(pairs, empty);
        Widgets.Frame(s, Assets, new Rect(FrameX, y, FramePartWidth * FrameParts, FramePartHeight * parts), FrameStyle.Orange);
        var trophies = Assets.Image(TrophyGraphic);
        s.Blit(trophies, TrophySource, 13, y + 4);
        s.Blit(trophies, TrophySource, 286, y + 4);

        CompetitionWidgets.Centered(s, Assets.Bold, 0, 319, y + 19, Title, Colors.White, DebCupDrawScreen.TitleShadow);
        var heading = overview.RoundName;
        if (overview.NextDay is { } day)
        {
            var date = SeasonCalendar.LayoutDate(day);
            heading += $"    {date.Day}.{date.Month}.";
        }
        CompetitionWidgets.Centered(s, Assets.Flow, 0, 319, y + 34, heading, Colors.TileBackground, Colors.Black);

        var managers = (_highlighted ?? game.Managers.Select(m => m.ClubIndex)).ToHashSet();
        var mini = Assets.Mini;
        var line = y + 46;
        foreach (var group in overview.Groups)
        {
            // [ORIGINAL] Die Überschrift steht in Großbuchstaben („BUNDESLIGA GEGEN ZWEITE LIGA:“), obwohl die Namen in DGROUP
            // gemischt geschrieben sind (0x2094 ff.); belegt in allen vier Videobildern (folge13_02545, duell_s2a_01217,
            // duell_s6a_03100, eisbaeren_f60_02758) und oberflaeche_0400.
            CompetitionWidgets.Centered(s, mini, 15, 183, line, GroupHeading(group), Colors.White, Colors.Black);
            line += 8;
            if (group.Rows.Count == 0)
                CompetitionWidgets.Centered(s, mini, 98, 100, line, "---------", Colors.White, null);
            for (var r = 0; r < group.Rows.Count; r++)
            {
                var row = group.Rows[r];
                var rowLine = line + RowPitch * r;
                var (left, right) = (game.Clubs[row.LeftClub], game.Clubs[row.RightClub]);
                CompetitionWidgets.Centered(s, mini, 15, 94, rowLine, left.Name, managers.Contains(row.LeftClub) ? Colors.Yellow : Colors.TileBackground, null);
                CompetitionWidgets.Centered(s, mini, 98, 100, rowLine, "-", Colors.White, null);
                CompetitionWidgets.Centered(s, mini, 104, 183, rowLine, right.Name, managers.Contains(row.RightClub) ? Colors.Yellow : (byte)1, null);
                for (var g = 0; g < row.Results.Count; g++)
                {
                    if (row.Results[g] is not { } result)
                        continue;
                    var x = ResultX(overview.DatesPlayed, g);
                    var color = ResultColors[g];
                    CompetitionWidgets.Left(s, mini, x, rowLine,
                        $"{CompetitionWidgets.Number(result.Left, 2)}:{CompetitionWidgets.Number(result.Right, 2)}", color);
                    for (var d = 0; d < result.Dots; d++)
                        s.Set(x + 22 + 2 * d, rowLine, color);
                }
            }
            line += RowPitch * Math.Max(group.Rows.Count - 1, 0) + RowPitch;
        }
    }
}
