using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens.Competitions;

/// <summary>
/// Vereinsfenster „Info über“ (Spur ui-meldungen, Teil 2, UM-2): Vereinszweig des Gegner-/Vereinsfensters für Vereine und
/// Europapokal-Gegner. Das Länderfenster bleibt <see cref="TeamInfoWindow"/>; Rahmen, Kopf, Rekorde und Hilfen teilen sich
/// beide. Zeigt nur vorhandenen Zustand; „Reicht mir“, Rechtsklick und Esc schließen.
/// </summary>
/// <remarks>
/// [ORIGINAL] em.exe <c>0x128:0x2A</c> = Datei 0x4D385, Vereinszweig (Team &lt; DGROUP 0x6AD = 52, [bp−0x14] = 0) bzw. Teams ab
/// 0x40; Aufbau in <c>docs/analyse/re-runde-12.md</c> „UM-2“. Lage relativ zu (93 | 44) wie im Länderzweig:
/// <list type="bullet">
/// <item>[ORIGINAL] Kopf wie <see cref="TeamInfoWindow"/>: Rahmen, Umriss (258 | 52) … (297 | 91), Logo bei (259 | 53) (kb\(0x50 +
///   Vereinsfeld 0x34 &amp; 0x3F), Teams ab 0x40 kb\0x8F, Datei 0x4D41D–0x4D440), graue Fläche (122 | 58) … (228 | 87).</item>
/// <item>[ORIGINAL] Tabellenkurve 0x445E0(x + 30, y − 30, Team, 0xFF, 0, 0) (Datei 0x4D56D–0x4D588), nur Team &lt; 0x40: Ursprung
///   (123 | 14), Achsen (125 | 62)–(125 | 85) und (126 | 86)–(221 | 86), nur der eigene Verein. Die beiden letzten Argumente 0
///   schalten „n.Spieltag“ und „Vor dem ersten Spieltag.“ ab (Datei 0x4497E, 0x44A09).</item>
/// <item>[ORIGINAL] Bilanzzeichen 0x51AAF über (118 | 89)–(249 | 89) aus dem Tabellensatz (DGROUP 0x825E), nur Team &lt; 0x40.</item>
/// <item>[ORIGINAL] Zeile 1 (Grundlinie 102, zentriert 93 … 309, Farbe 30): Vereine „P.“ + „ Platz in der “ + Liganame
///   (DGROUP 0x9E0 + 4 · (0x34FD5(Team) &gt;&gt; 1): „Bundesliga“, „Zweiten Liga“, „Oberliga“) + „, Stärke: “ + 0x4D316;
///   Teams ab 0x40 „Stärke:“ + Wert (Datei 0x4D7DE–0x4D8DB).</item>
/// <item>[ORIGINAL] Teams ab 0x40: statt der Statistik drei Zeilen zentriert 93 … 317, Grundlinien 120/128/138, Farbe 1
///   (DGROUP 0xF7D–0xF85, Datei 0x4D6CC–0x4D748).</item>
/// <item>[ORIGINAL] Statistik ab Grundlinie 109 (+ 0x41, Datei 0x4D8EC): „Heimbilanz:“ (Heimfelder +0x21/+0x29/+0x2D) und
///   „Auswtrsbilanz:“ mit dem Schreibfehler des Originals (DGROUP 0xFAD: „Ausw{trsbilanz:“; Auswärtsfelder +0x22/+0x2A/+0x2E),
///   dann wie im Länderzweig; „Historische Ergebnisse gegen “ + Verein des Managers am Zug (Datei 0x4E0F6), darunter
///   „H: …“ (Heimspiele des Vereins = Seite 1 der Tabelle des Managers) „ -- A: …“, leere Seite „ Noch keine“ (Datei
///   0x4E15C–0x4E31A), danach + 8. Ohne Liste (eigener Verein, Bundestrainer am Zug) folgt die nächste Zeile ohne Abstand.</item>
/// <item>[ORIGINAL] „Punkte in der Ewigen Tabelle: “ + Punkte (Farbe 1) bzw. „Noch keine...“ (Farbe 6), x 98 (Datei
///   0x4E348–0x4E3CE).</item>
/// <item>[ORIGINAL] Knöpfe auf y 215: „Reicht mir“ (103) und „Spielplan“ (173), dieser nur bei Tageszähler DGROUP 0x3B7 ≠ 0 und
///   Team &lt; 52 (Datei 0x4D752–0x4D787).</item>
/// <item>[APPROX] „Spielplan“ öffnet im Original den Spielplan des Gegners 0x05CA0 (<c>re-abdeckung.md</c> Nr. 12); den
///   Bildschirm gibt es im Remake nicht, der Knopf steht ohne Wirkung. Dritter Knopf (Einschätzung, ändert Spielzustand) und
///   Wettbewerbscode DGROUP 0xD0C2 wie im Länderzweig (<see cref="TeamInfoWindow"/>).</item>
/// <item>[APPROX] Bilanz, Bilanzzeichen und „Tore / Spiel“ aus den Ligaergebnissen der Saison (<see cref="GameState.Results"/>,
///   wie der Tabellensatz, den nur Wettbewerbscode 7/8 füllt und der Saisonwechsel leert). Rekorde der Vereine trägt das
///   Remake noch nicht ein (<see cref="GameState.EnterNationalResult"/> nur für Länder): bis dahin „Noch keine Eintragung.“.</item>
/// </list>
/// </remarks>
public sealed class ClubInfoWindow
{
    public const int Left = TeamInfoWindow.Left;
    public const int Top = TeamInfoWindow.Top;
    public static readonly Rect CloseButton = TeamInfoWindow.CloseButton;
    public static readonly Rect ScheduleButton = Widgets.ButtonRect(Left + 0x50, Top + 0xAB);

    /// <summary>[ORIGINAL] Ursprung der Tabellenkurve (x + 30, y − 30), Datei 0x4D57B–0x4D584.</summary>
    public const int CurveX = Left + 30, CurveY = Top - 30;

    /// <summary>[ORIGINAL] Liganamen DGROUP 0x9E0 ff. (Index 0x34FD5 &gt;&gt; 1).</summary>
    public static readonly string[] LeagueNames = ["Bundesliga", "Zweiten Liga", "Oberliga"];

    /// <summary>
    /// [ORIGINAL] DGROUP 0xF7D/0xF81/0xF85. [APPROX] Das erste Zeichen der dritten Zeile ist im Original Byte 0xE4 (nicht in der
    /// Umlautkodierung <c>GameText</c>); als „Ä“ gelesen wie in <c>re-runde-12.md</c>, die Glyphe ist nicht geprüft.
    /// </summary>
    public static readonly string[] NoInformation =
    [
        "Leider sind aus vielerlei Gründen keine",
        "Informationen verfügbar...",
        "Ähm... der Grund ist der begrenzte Speicher...",
    ];

    /// <summary>[ORIGINAL] DGROUP 0xFAD, mit dem Schreibfehler des Originals.</summary>
    public const string AwayRecordLabel = "Auswätrsbilanz:";

    public const string NoMeetings = " Noch keine";
    public const string EternalLabel = "Punkte in der Ewigen Tabelle: ";
    public const string NoEternal = "Noch keine...";

    private int? _club;

    public bool Visible => _club is not null;
    public int? Club => _club;

    public void Show(int club) => _club = club;

    public void Close() => _club = null;

    /// <summary>Teams ab 0x40: Europapokal-Gegner ohne Statistik.</summary>
    public static bool IsForeign(int club) => club >= EuropeanCup.FirstOpponent;

    /// <summary>[ORIGINAL] Knopf „Spielplan“ sichtbar (Datei 0x4D752–0x4D762).</summary>
    public static bool HasSchedule(GameState game, int club) => game.Day != 0 && club < NationalTeams.First;

    /// <summary>Liefert true, solange das Fenster offen ist (Eingaben verbraucht).</summary>
    public bool Update(InputState input)
    {
        if (_club is null)
            return false;
        if (input.Clicked(CloseButton) || input.RightClick || input.Pressed(Key.Escape) || input.Pressed(Key.Enter))
            Close();
        return true;
    }

    /// <summary>Zeile 1 (Datei 0x4D7DE–0x4D8DB).</summary>
    public static string HeadLine(GameState game, int club)
    {
        var c = game.Clubs[club];
        var strength = SpectatorRules.Strength(game, c);
        if (IsForeign(club))
            return "Stärke:" + strength;
        var rank = c.League is { } league
            ? LeagueTable.Compute(game, league, TableView.Gesamt, game.PlayedMatchDays).First(r => r.Club.Index == club).Rank
            : 1;
        // [APPROX] Vereine ohne Liga zeigt das Original mit Liga 0x34FD5 = 4 („Oberliga“); im Remake hat jeder Ligaverein eine Liga.
        var name = LeagueNames[c.League is { } l ? (int)l : 2];
        return $"{rank}. Platz in der {name}, Stärke: {strength}";
    }

    /// <summary>Heim- bzw. Auswärtsbilanz der Saison (Siege, Unentschieden, Niederlagen) aus den Ligaergebnissen.</summary>
    public static (int Wins, int Draws, int Losses) LeagueRecord(GameState game, int club, bool home)
    {
        int w = 0, d = 0, l = 0;
        foreach (var r in game.Results)
        {
            if ((home ? r.Home : r.Away) != club)
                continue;
            var (f, a) = home ? (r.HomeGoals, r.AwayGoals) : (r.AwayGoals, r.HomeGoals);
            if (f > a) w++;
            else if (f < a) l++;
            else d++;
        }
        return (w, d, l);
    }

    /// <summary>
    /// Bilanzzeichen (acht Felder, das letzte das jüngste; 0 = leer, 1 = N, 2 = U, 3 = S) aus den letzten acht Ligaspielen wie
    /// <see cref="TableRow.Balance"/>.
    /// </summary>
    public static IReadOnlyList<int> Form(GameState game, int club)
    {
        var form = new int[GameState.NationalFormLength];
        var last = game.Results.Where(r => r.Involves(club)).TakeLast(form.Length).ToList();
        for (var i = 0; i < last.Count; i++)
        {
            var r = last[i];
            var (f, a) = r.Home == club ? (r.HomeGoals, r.AwayGoals) : (r.AwayGoals, r.HomeGoals);
            form[form.Length - last.Count + i] = f > a ? 3 : f == a ? 2 : 1;
        }
        return form;
    }

    /// <summary>
    /// [ORIGINAL] Platz der Kopf-an-Kopf-Liste im Vereinszweig oder null (Datei 0x4E086–0x4E0D3): nicht für den Verein des Managers
    /// am Zug, nicht im Bundestrainermenü (Bundestrainer am Zug und Team kein Land).
    /// </summary>
    public static int? HeadToHeadSlot(GameState game, int club)
    {
        if (game.NationalCoachMenu || game.ActiveManager.ClubIndex == club)
            return null;
        return game.CurrentManager;
    }

    /// <summary>
    /// [ORIGINAL] Ergebnisliste „H: … -- A: …“ (Datei 0x4E15C–0x4E31A): erst die Heimspiele des Vereins (Seite 1 der Tabelle),
    /// dann die Auswärtsspiele (Seite 0), je Platz 0 … 4, leere Plätze übersprungen, Komma vor jedem Platz außer dem ersten;
    /// eine leere Seite bekommt „ Noch keine“. Je Eintrag Tore des Vereins : Tore des Managers.
    /// </summary>
    public static string HeadToHeadText(IReadOnlyList<int> list)
    {
        var text = "H: ";
        for (var side = 0; side < 2; side++)
        {
            for (var i = 0; i < MeetingHistory.Slots; i++)
            {
                var v = list[2 * i + 1 - side];
                if (v == GameState.HeadToHeadEmpty)
                    continue;
                if (i != 0)
                    text += ",";
                text += (v >> 4) + ":" + (v & 0xF);
            }
            if (side == 0)
            {
                if (text.Length == 3)
                    text += NoMeetings;
                text += " -- A: ";
            }
        }
        if (text[^1] == ' ')
            text += NoMeetings;
        return text;
    }

    /// <summary>Zeilen ab „Heimbilanz:“ in der Reihenfolge des Originals (Datei 0x4D8EC–0x4E3CE); leer für Teams ab 0x40.</summary>
    public static IReadOnlyList<TeamInfoWindow.Line> Lines(GameState game, int club)
    {
        var lines = new List<TeamInfoWindow.Line>();
        if (IsForeign(club))
            return lines;
        var y = TeamInfoWindow.FirstBaseline;
        void Add(string text, int x, byte color = TeamInfoWindow.TextColor) => lines.Add(new TeamInfoWindow.Line(text, x, y, color));
        const int labelX = TeamInfoWindow.LabelX, valueX = TeamInfoWindow.ValueX;

        var (hw, hd, hl) = LeagueRecord(game, club, home: true);
        Add("Heimbilanz:", labelX);
        Add($"{hw} Siege, {hd} Unent., {hl} Nied.", valueX);
        y += TeamInfoWindow.LineStep;
        var (aw, ad, al) = LeagueRecord(game, club, home: false);
        Add(AwayRecordLabel, labelX);
        Add($"{aw} Siege, {ad} Unent., {al} Nied.", valueX);
        y += TeamInfoWindow.LineStep;

        var (points, played) = TeamInfoWindow.FormPoints(Form(game, club));
        Add("Aus d. letzten Spielen:", labelX);
        Add($"{points}:{2 * played - points} Punkte", valueX);
        y += TeamInfoWindow.LineStep;

        int games = 0, goalsFor = 0, goalsAgainst = 0;
        foreach (var r in game.Results.Where(r => r.Involves(club)))
        {
            games++;
            goalsFor += r.Home == club ? r.HomeGoals : r.AwayGoals;
            goalsAgainst += r.Home == club ? r.AwayGoals : r.HomeGoals;
        }
        Add("Tore / Spiel (Gesamt):  ", labelX);
        Add(TeamInfoWindow.Average(goalsFor, games) + ":" + TeamInfoWindow.Average(goalsAgainst, games), valueX);
        y += TeamInfoWindow.LineStep;

        Add("Serien (Gesamt):  ", labelX);
        y += TeamInfoWindow.LineStep;
        var series = game.Clubs[club].SeriesCurrent;
        var count = 0;
        for (var kind = 0; kind < TeamInfoWindow.SeriesTexts.Length && count < 4; kind++)
        {
            var value = series[Series.Index((SeriesKind)kind, SeriesColumn.Total)];
            if (value <= 1)
                continue;
            Add(CompetitionWidgets.Number(value, 2) + " Spiele in Folge " + TeamInfoWindow.SeriesTexts[kind], labelX);
            y += TeamInfoWindow.LineStep;
            count++;
        }
        if (count == 0)
        {
            Add(TeamInfoWindow.NoSeries, Left + 0x0A, TeamInfoWindow.NoSeriesColor);
            y += TeamInfoWindow.LineStep;
        }
        y += 2;

        for (var r = 0; r < 4; r++)
        {
            Add(TeamInfoWindow.RecordTexts[r], labelX);
            Add(TeamInfoWindow.RecordValue(game, club, r), TeamInfoWindow.RecordValueX);
            y += TeamInfoWindow.LineStep;
        }
        y += 2;

        if (HeadToHeadSlot(game, club) is { } slot)
        {
            Add("Historische Ergebnisse gegen " + game.Clubs[game.ActiveManager.ClubIndex].Name, labelX);
            y += TeamInfoWindow.LineStep;
            Add(HeadToHeadText(game.HeadToHead(slot, club)), labelX);
            y += 8;
        }

        var eternal = game.Eternal(club).Points;
        Add(EternalLabel + (eternal != 0 ? eternal.ToString() : NoEternal), labelX, eternal != 0 ? (byte)1 : TeamInfoWindow.NoSeriesColor);
        return lines;
    }

    public void Draw(Surface s, Assets a, GameState game)
    {
        if (_club is not { } club)
            return;
        var foreign = IsForeign(club);
        Widgets.Frame(s, a, TeamInfoWindow.Box, FrameStyle.Orange);
        s.Outline(new Rect(258, 52, 40, 40), 4);
        Widgets.Logo(s, a, foreign ? ForeignLogo : game.Clubs[club].LogoGraphic, 259, 53);
        NationalWidgets.FillInclusive(s, 122, 58, 228, 87, 1);
        s.Outline(new Rect(122, 58, 107, 30), 3);
        if (!foreign)
        {
            RankCurve.Draw(s, a, game.RankHistory(club), null, CurveX, CurveY);
            Widgets.Smiley(s, a, Widgets.Mood.Same, TeamInfoWindow.MoodX, TeamInfoWindow.MoodY);
            var form = Form(game, club);
            for (var i = 0; i < form.Count; i++)
                if (form[i] != 0)
                    Widgets.Smiley(s, a, form[i] == 3 ? Widgets.Mood.Up : form[i] == 1 ? Widgets.Mood.Down : Widgets.Mood.Same, 118 + 8 * i, 89);
        }
        NationalWidgets.FillInclusive(s, 96, 96, 301, 229, 4);
        CompetitionWidgets.Centered(s, a.Flow, 124, 235, 55, game.Clubs[club].Name, TeamInfoWindow.TextColor, 0);
        s.HLine(98, 95, 305 - 98 + 1, 29);
        var mini = a.Mini;
        DrawButton(s, a, CloseButton, "Reicht mir");
        if (HasSchedule(game, club))
            DrawButton(s, a, ScheduleButton, "Spielplan");
        CompetitionWidgets.Centered(s, mini, Left, Left + 0xD8, Top + 0x3A, HeadLine(game, club), TeamInfoWindow.TextColor, null);
        if (foreign)
        {
            int[] baselines = [Top + 0x4C, Top + 0x54, Top + 0x5E];
            for (var i = 0; i < NoInformation.Length; i++)
                CompetitionWidgets.Centered(s, mini, Left, Left + 0xE0, baselines[i], NoInformation[i], 1, null);
            return;
        }
        foreach (var line in Lines(game, club))
            CompetitionWidgets.Left(s, mini, line.X, line.Baseline, line.Text, line.Color);
    }

    /// <summary>[ORIGINAL] Bild der Europapokal-Gegner kb\0x8F (Datei 0x4D440).</summary>
    public const int ForeignLogo = 0x8F;

    // [ORIGINAL] Knopf 0x120:0x75 wie in TeamInfoWindow: kb\2 (17 | 179), Text zentriert x … x + 59, Grundlinie y + 7.
    private static void DrawButton(Surface s, Assets a, Rect r, string text)
    {
        s.Blit(a.Image(2), new Rect(17, 179, Widgets.ButtonWidth, Widgets.ButtonHeight), r.X, r.Y, transparent: 0);
        CompetitionWidgets.Centered(s, a.Mini, r.X, r.X + 59, r.Y + 7, text, 29, 1);
    }
}
