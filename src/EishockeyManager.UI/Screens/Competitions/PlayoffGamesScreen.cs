using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens.Competitions;

/// <summary>Eine Play-Off-Serie: erster Verein der Paarung links, Ergebnisse je Spiel aus seiner Sicht (Spalte = Spielnummer).</summary>
public sealed record PlayoffRow(int Left, int Right, IReadOnlyList<DebCupResult> Results);

/// <summary>Ein Abschnitt „VIERTELFINALE“, „HALBFINALE“, „ENDSPIEL“ mit seinen Serien.</summary>
public sealed record PlayoffSection(string Title, IReadOnlyList<PlayoffRow> Rows);

/// <summary>Zustand der Seite „Play-Off Spiele“ (nur gelesen aus <see cref="Playoffs"/>, ändert nichts).</summary>
/// <param name="Sections">Feststehende Runden; leer vor dem Ende der Hauptrunde.</param>
/// <param name="Headings">Zahl der Überschriften für die Rahmenhöhe (DGROUP 0x4E10[0x7B0]: 1, 2, 3).</param>
/// <param name="NextDay">Nächster Play-Off-Termin ab dem aktuellen Tag für die Datumszeile, sonst null.</param>
public sealed record PlayoffOverview(IReadOnlyList<PlayoffSection> Sections, int Headings, int? NextDay)
{
    /// <summary>[ORIGINAL] Überschriften DGROUP 0x725 + 4 · (0x4E14[k] = 0, 1, 3).</summary>
    public static readonly string[] SectionTitles = ["VIERTELFINALE", "HALBFINALE", "ENDSPIEL"];

    /// <summary>[ORIGINAL] Letzter Tag mit Datumszeile: DGROUP 0x4E00 + 1 = 209 (Wettbewerb ≠ 0, Datei 0x332B6–0x332C2).</summary>
    public const int LastDateLineDay = 209;

    /// <summary>
    /// Seite aus dem Spielstand oder null vor den Play-Offs (DGROUP 0xCFFA = 0xFF). [ORIGINAL] Links steht der Verein auf dem
    /// geraden Platz: 0x31505 tauscht die Plätze samt allen bisherigen Ergebnissen nach jedem der ersten vier Termine einer Runde
    /// (auch wenn die Serie schon entschieden ist, Datei 0x3168B–0x316C9); am fünften Termin nur, wenn der Verein auf dem
    /// ungeraden Platz gewonnen hat (Siege := 0x14, Datei 0x316A3–0x316BF), und beim Rundenwechsel wird dieser Tausch
    /// zurückgenommen (Datei 0x31C24–0x31C46). Abgeschlossene Runden zeigen deshalb den ersten Verein der Paarung links
    /// (folge21_02150: Eisbären Berlin – SC Preussen Berlin „3: 8 2: 6 1: 5“), die laufende Runde nach ungerader Terminzahl
    /// getauscht und nach dem fünften Termin den Sieger links (folge23_02017, Endspiel EHC 80 Nürnberg – ES Weißwasser). [ORIGINAL] Die nächste Runde steht in der
    /// Platztabelle DGROUP 0x5D3B, sobald der fünfte Termin der laufenden Runde gespielt ist (Rundenwechsel Datei 0x31B9C–0x31C88:
    /// Sieger nach 0x5D3A + DGROUP 0x144[i], dann 0x7B0 + 1); das Remake legt die Runde erst am nächsten Termin an
    /// (<see cref="Playoffs"/>), die Seite zeigt sie deshalb schon nach dem fünften Termin mit derselben Paarungsregel.
    /// </summary>
    public static PlayoffOverview? Of(GameState game) => game.Playoffs is { } playoffs ? Of(playoffs, game.CurrentDay) : null;

    /// <summary>Seite der Play-Offs am Tag <paramref name="today"/> (Termine davor sind vorbei).</summary>
    public static PlayoffOverview Of(Playoffs playoffs, int today)
    {
        var days = SeasonCalendar.DaysOf(DayEvents.Playoffs);
        var rounds = playoffs.Rounds.Select(r => r.Select(s => (s.HomeClub, s.AwayClub, s.Games.ToList())).ToList()).ToList();
        var last = playoffs.Rounds[^1];
        var lastIndex = playoffs.Rounds.Count - 1;
        if (rounds.Count < 3 && last.All(s => s.IsDecided) && days[Playoffs.DatesPerRound * lastIndex + Playoffs.DatesPerRound - 1] < today)
        {
            // [ORIGINAL] Tabelle DGROUP 0x144: Serie k = Sieger der Serie k gegen Sieger der Serie k + Serienzahl / 2 (wie Playoffs.Prepare).
            var winners = last.Select(s => s.Winner!.Value).ToList();
            var half = winners.Count / 2;
            rounds.Add(Enumerable.Range(0, half).Select(k => (winners[k], winners[k + half], new List<KnockoutMatch>())).ToList());
        }

        var sections = new List<PlayoffSection>();
        for (var r = 0; r < rounds.Count; r++)
        {
            var finished = r < rounds.Count - 1;
            var passed = days.Skip(Playoffs.DatesPerRound * r).Take(Playoffs.DatesPerRound).Count(d => d < today);
            var rows = new List<PlayoffRow>();
            foreach (var (home, away, games) in rounds[r])
            {
                var swapped = !finished && (passed == Playoffs.DatesPerRound ? WinnerOf(games, home, away) == away : passed % 2 == 1);
                var (left, right) = swapped ? (away, home) : (home, away);
                rows.Add(new PlayoffRow(left, right, games.Select(m => ResultOf(m, left)).ToList()));
            }
            sections.Add(new PlayoffSection(SectionTitles[r], rows));
        }
        int? next = today <= LastDateLineDay ? days.Where(d => d >= today).Select(d => (int?)d).FirstOrDefault() : null;
        return new PlayoffOverview(sections, sections.Count, next);
    }

    private static int? WinnerOf(List<KnockoutMatch> games, int home, int away)
    {
        var homeWins = games.Count(m => m.Winner == home);
        var awayWins = games.Count(m => m.Winner == away);
        return homeWins >= PlayoffSeries.WinsNeeded ? home : awayWins >= PlayoffSeries.WinsNeeded ? away : null;
    }

    /// <summary>
    /// Ergebnis eines Serienspiels aus Sicht des linken Vereins. [ORIGINAL] Tore je Platz DGROUP 0xCCB2 + 14 · Spiel + Platz
    /// (Datei 0x337EE–0x33817), links Platz 2k (folge21_02150: Berliner SC – ES Weißwasser „3: 2  0: 4  4: 2  4: 0“ bei drei
    /// Siegen des BSC). [APPROX] Punkte: einer nach Penaltyschießen; zwei nach einem Tor in der Verlängerung (Video
    /// folge21_02150 „7: 5..“) kennt das Remake für Play-Offs nicht.
    /// </summary>
    public static DebCupResult ResultOf(KnockoutMatch match, int left)
    {
        var leftIsHome = match.Home == left;
        return new DebCupResult(leftIsHome ? match.HomeGoals : match.AwayGoals, leftIsHome ? match.AwayGoals : match.HomeGoals,
            match.PenaltyWinner is null ? 0 : 1);
    }
}

/// <summary>
/// „Play-Off Spiele“ (Paarungen 1. Liga → „Play-Off“): die Serien der Play-Offs nach Runden mit allen Ergebnissen. Zeigt nur
/// vorhandenen Zustand; „Liga“ führt zu den Ligapaarungen zurück.
/// </summary>
/// <remarks>
/// [ORIGINAL] Dieselbe Bildschirmfunktion wie „DEB-Pokalspiele“ und „WM-Endrundenspiele“ (Datei 0x32E23, Stub 0xE8:0x75) mit
/// Wettbewerb 2, aufgerufen aus Seite 1 des Seitenwählers (Datei 0x3AA90, <see cref="LeaguePairingsPages"/>). Vermessen an
/// <c>folge21_02150</c>, <c>folge23_02017</c> (Spur ui-rest, Ablauf <c>pixel-playoff</c>):
/// <list type="bullet">
/// <item>[ORIGINAL] Titel DGROUP 0xCD0 + 4 · 2 „Play-Off Spiele“, Knopftext DGROUP 0x650 + 4 · 2 „Liga“; Pokalbild kb\36 Quelle
///   (0 | 0) (Datei 0x33112–0x3312F: [bp−0xC] = 0 für Wettbewerb 2).</item>
/// <item>[ORIGINAL] Zeilen: Paare der Platztabelle DGROUP 0x5D3B bis 0xFF (Datei 0x32E93–0x32EC2), ohne Play-Offs (DGROUP 0xCFFA
///   = 0xFF) keine; Rahmen wie <see cref="WorldCupKnockoutScreen.FrameFor"/> mit Zeilen + 1 − 6 + DGROUP 0x4E10[0x7B0] (1, 2, 3
///   Überschriften; Datei 0x32FC0–0x32FD3).</item>
/// <item>[ORIGINAL] Überschrift k vor Zeile DGROUP 0x4E0D[k] = 1, 5, 7 (Mini, 15 … 183, Farbe 7, Schatten 0), danach + 7
///   (Datei 0x3351B–0x33583); Zeile ab Oberkante + 46, Abstand 7, Namen wie „DEB-Pokalspiele“.</item>
/// <item>[ORIGINAL] Ergebnisse „g:g“ (zweistellig) bei x = 187 + 26 · Spiel, Farbe 1 / 30 / 1 / 30 / 1 (Datei 0x33932–0x339B3);
///   frühere Runden alle fünf Spalten, die laufende so viele wie Termine vorbei sind; nicht ausgetragene Spiele (≥ 100) leer.
///   Punkte wie bei „DEB-Pokalspiele“ (+ 30 je Punkt, Pixel bei x + 22 + 2 i).</item>
/// <item>[ORIGINAL] Datumszeile (Flow, Grundlinie Oberkante + 34, Farbe 30, Schatten 0) „   t.m.“ mit dem nächsten Play-Off-Tag
///   ab heute (Maske 0x20 auf DGROUP 0x828 &gt;&gt; 1), nur bis Tag 209; ohne Play-Offs stattdessen „Die Paarungen stehen noch
///   nicht fest...“ (DGROUP 0x924, Datei 0x3337B–0x333C8).</item>
/// <item>[ORIGINAL] Bedienung (Datei 0x33A70–0x33B43): Knopf „Liga“ (136 | 201) → Ligapaarungen (Rückgabe 7); Klick auf einen
///   Namen (Zeile y − 6 &lt; Maus-y ≤ y, rechts ab x = 100) → Vereinsfenster 0x128:0x2A; Rechtsklick oder Taste → Hauptmenü;
///   andere Linksklicks bewirken nichts.</item>
/// <item>[APPROX] Die Anzeige im Spielablauf („n.Minute“) gibt es im Remake noch nicht (Spur livespiel).</item>
/// </list>
/// </remarks>
public sealed class PlayoffGamesScreen : Screen
{
    public const string Title = "Play-Off Spiele";
    public const string ButtonText = "Liga";
    public static readonly Rect Button = WorldCupKnockoutScreen.Button;
    public static readonly Rect TrophySource = new(0, 0, 22, 22);

    /// <summary>[ORIGINAL] Ergebnisspalte i bei x = 0xBB + 26 · i (Datei 0x33978–0x33996, Wettbewerb 2).</summary>
    public static int ResultX(int game) => 187 + 26 * game;

    /// <summary>[ORIGINAL] Farbe je Spiel: gerade Spielnummer 1, ungerade 30 (Datei 0x33932–0x33942).</summary>
    public static byte ResultColor(int game) => game % 2 == 0 ? (byte)1 : Colors.TileBackground;

    private readonly Screen _back;
    private readonly PlayoffOverview? _fixed;
    private readonly IReadOnlyCollection<int>? _highlighted;

    public PlayoffGamesScreen(ScreenHost host, Screen back) : base(host) => _back = back;

    /// <summary>Feste Runden statt des Spielstands (Bildschirmfotos wie im Video), ohne Datumszeile.</summary>
    public PlayoffGamesScreen(ScreenHost host, Screen back, IReadOnlyList<PlayoffSection> sections, IReadOnlyCollection<int>? highlighted = null)
        : base(host) => (_back, _fixed, _highlighted) = (back, new PlayoffOverview(sections, Math.Max(sections.Count, 1), null), highlighted);

    public PlayoffOverview? Overview => _fixed ?? (Host.Game is { } game ? PlayoffOverview.Of(game) : null);

    /// <summary>Vereinsfenster nach Klick auf einen Namen.</summary>
    public InfoBarWindows InfoWindows { get; } = new();

    /// <summary>Rahmenteile und Oberkante.</summary>
    public static (int Parts, int Y) FrameFor(PlayoffOverview? overview) =>
        WorldCupKnockoutScreen.FrameFor(overview?.Sections.Sum(s => s.Rows.Count) ?? 0, overview?.Headings ?? 1, live: false);

    /// <summary>Grundlinien der Zeilen mit ihren Vereinen, wie <see cref="Draw"/> sie legt.</summary>
    public static IReadOnlyList<(int Y, int Left, int Right)> RowLines(PlayoffOverview? overview, int top)
    {
        var lines = new List<(int, int, int)>();
        var line = top + 46;
        foreach (var section in overview?.Sections ?? [])
        {
            line += WorldCupKnockoutScreen.HeadingPitch;
            foreach (var row in section.Rows)
            {
                lines.Add((line, row.Left, row.Right));
                line += DebCupGamesScreen.RowPitch;
            }
        }
        return lines;
    }

    /// <summary>[ORIGINAL] Verein unter der Maus (Datei 0x33AAF–0x33B30): Zeile mit y − 6 &lt; Maus-y ≤ y, rechts ab x = 100,
    /// nur unterhalb von Oberkante + 26.</summary>
    public static int? ClubAt(PlayoffOverview? overview, int x, int y)
    {
        var (_, top) = FrameFor(overview);
        if (y <= top + 16 + 10)
            return null;
        foreach (var (line, left, right) in RowLines(overview, top))
            if (line - 6 < y && line >= y)
                return x > 99 ? right : left;
        return null;
    }

    public override void Update(InputState input)
    {
        if (InfoWindows.Nation.Update(input) | InfoWindows.Club.Update(input))
            return;
        if (input.RightClick || input.Keys.Count > 0)
            Host.Show(_back);
        else if (input.Clicked(Button))
            Host.Show(LeaguePairingsPages.Open(Host, _back, League.Bundesliga, LeaguePairingsPages.Pairings));
        else if (input.LeftClick && Host.Game is not null && ClubAt(Overview, input.MouseX, input.MouseY) is { } club)
            InfoWindows.Show(club);
    }

    public override void Draw(Surface s)
    {
        s.Palette = Assets.Palette;
        s.Blit(Assets.Image(MainMenuScreen.Background), 0, 0);
        var overview = Overview;
        var (parts, y) = FrameFor(overview);
        Widgets.Frame(s, Assets, Button, FrameStyle.Orange);
        CompetitionWidgets.Centered(s, Assets.Mini, 160, 160, 221, ButtonText, Colors.White, null);
        Widgets.Frame(s, Assets, new Rect(DebCupGamesScreen.FrameX, y, DebCupGamesScreen.FramePartWidth * DebCupGamesScreen.FrameParts,
            DebCupGamesScreen.FramePartHeight * parts), FrameStyle.Orange);
        var trophies = Assets.Image(DebCupGamesScreen.TrophyGraphic);
        s.Blit(trophies, TrophySource, 13, y + 4);
        s.Blit(trophies, TrophySource, 286, y + 4);
        CompetitionWidgets.Centered(s, Assets.Bold, 0, 319, y + 19, Title, Colors.White, DebCupDrawScreen.TitleShadow);
        if (overview is null)
            CompetitionWidgets.Centered(s, Assets.Flow, 0, 319, y + 34, WorldCupKnockoutScreen.NotYetKnown, Colors.TileBackground, Colors.Black);
        else if (overview.NextDay is { } day)
        {
            var date = SeasonCalendar.LayoutDate(day);
            CompetitionWidgets.Centered(s, Assets.Flow, 0, 319, y + 34, $"   {date.Day}.{date.Month}.", Colors.TileBackground, Colors.Black);
        }

        if (Host.Game is { } game && overview is not null)
        {
            var highlighted = (_highlighted ?? game.Managers.Select(m => m.ClubIndex)).ToHashSet();
            var mini = Assets.Mini;
            var line = y + 46;
            foreach (var section in overview.Sections)
            {
                CompetitionWidgets.Centered(s, mini, 15, 183, line, section.Title, Colors.White, Colors.Black);
                line += WorldCupKnockoutScreen.HeadingPitch;
                foreach (var row in section.Rows)
                {
                    CompetitionWidgets.Centered(s, mini, 15, 94, line, game.Clubs[row.Left].Name,
                        highlighted.Contains(row.Left) ? Colors.Yellow : Colors.TileBackground, null);
                    CompetitionWidgets.Centered(s, mini, 98, 100, line, "-", Colors.White, null);
                    CompetitionWidgets.Centered(s, mini, 104, 183, line, game.Clubs[row.Right].Name,
                        highlighted.Contains(row.Right) ? Colors.Yellow : (byte)1, null);
                    for (var g = 0; g < Math.Min(row.Results.Count, Playoffs.DatesPerRound); g++)
                    {
                        var result = row.Results[g];
                        var x = ResultX(g);
                        var color = ResultColor(g);
                        CompetitionWidgets.Left(s, mini, x, line,
                            $"{CompetitionWidgets.Number(result.Left, 2)}:{CompetitionWidgets.Number(result.Right, 2)}", color);
                        for (var d = 0; d < result.Dots; d++)
                            s.Set(x + 22 + 2 * d, line, color);
                    }
                    line += DebCupGamesScreen.RowPitch;
                }
            }
            InfoWindows.Draw(s, Assets, game);
        }
    }
}
