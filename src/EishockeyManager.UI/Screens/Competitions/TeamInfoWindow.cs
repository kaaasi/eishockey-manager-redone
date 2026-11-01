using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens.Competitions;

/// <summary>
/// Gegner-Fenster „Info über“ für ein Land (Klick auf eine Flagge in WM-Tabelle, WM-Paarungen und WM-Statistik): Name,
/// Flagge, Stärke, Bilanz, letzte Spiele, Tore je Spiel, Serien, Rekorde und die Ergebnisse gegen Deutschland. Zeigt nur
/// vorhandenen Zustand; „Reicht mir“, Rechtsklick und Esc schließen.
/// </summary>
/// <remarks>
/// [ORIGINAL] em.exe <c>0x128:0x2A</c> = Datei 0x4D385 (Argumente Verein, Seite 0/1/2 = Heim/Auswärts/Gesamt, zwei Werte für den
/// Spielplan). Die WM-Bildschirme rufen es mit Seite 2 (Datei 0x2FAB8, 0x302AA, 0x3B4AE). Belegt an DOSBox
/// <c>nur-weltkugel/14–16</c> und Video <c>bundestrainer_00417</c>, <c>_00610</c>, <c>_00658</c>, <c>eisbaeren_f24_00046</c>.
/// Lage relativ zu (93 | 44) (si = 0x5D, [bp−6] = 0x2C):
/// <list type="bullet">
/// <item>[ORIGINAL] Fenster 9 × 10 Teile ab (93 | 44) (0x30:0x335 mit 0, 3). Umriss der Flagge (258 | 52) … (297 | 91) Farbe 4,
///   Flagge 38 × 38 (Logo, kb\(80 + n)) bei (259 | 53). Graue Fläche (122 | 58) … (228 | 87) Farbe 1, Umriss Farbe 3.</item>
/// <item>[ORIGINAL] Bilanzzeichen als Gesichter aus kb\33 (6 × 5, Datei 0x51AAF): Zeichen i (0 … 7, das jüngste zuletzt) bei
///   (118 + 8 i | 89), Sieg grün, Unentschieden gelb, Niederlage rot; leere Zeichen bleiben frei.</item>
/// <item>[ORIGINAL] Fläche (96 | 96) … (301 | 229) Farbe 4; Name in Schrift 2 (Flow) zentriert 124 … 235, Grundlinie 55, Farbe
///   30, Schatten 0; Linie (98 … 305 | 95) Farbe 29; Knopf „Reicht mir“ bei (103 | 215).</item>
/// <item>[ORIGINAL] Schrift 3 (Mini), Farbe 30: „Stärke:“ + Wert zentriert 93 … 309, Grundlinie 102. Ab Grundlinie 109 je
///   Zeile + 6: „Bilanz:“ (x 98) mit „S Siege, U Unent., N Nied.“ (x 195), „Aus d. letzten Spielen:“ mit „P:M Punkte“ (P = 2 je
///   Sieg, 1 je Unentschieden aus den Bilanzzeichen, M = 2 · Spiele − P), „Tore / Spiel (Gesamt):“ mit dem Schnitt, „Serien
///   (Gesamt):“ und höchstens vier Serien &gt; 1 (Mindestbreite 2) „n Spiele in Folge “ + Art (x 98), sonst „Keine
///   bemerkenswerte Serie vorhanden.“ (x 103, Farbe 6). Danach + 2 und die vier Rekorde (Text x 98, Wert x 183), + 2 und
///   „Historische Ergebnisse gegen DEUTSCHLAND“ mit der Liste darunter.</item>
/// <item>[ORIGINAL] Rekordwert (Datei 0x4E5D5): Heim- und Auswärtsrekord der Art; beide Rückgaben (unteres Byte) 0 → Heimtext,
///   sonst bei Sieg/Niederlage der Text mit dem größeren |oberes − unteres Halbbyte| des unteren Bytes (Gleichstand →
///   Auswärts), bei den Toren der mit dem größeren unteren Byte. Ohne Eintrag „Noch keine Eintragung.“.</item>
/// <item>[ORIGINAL] Die Ergebnisse stehen nur, wenn das Land nicht Deutschland und nicht der Verein des Managers ist; Platz der
///   Liste: Bundestrainer am Zug → 0, sonst die Managernummer. Reihenfolge: ungerade Plätze, dann gerade, je bis zum ersten
///   leeren; „hi:lo“, durch „,“ getrennt.</item>
/// <item>[APPROX] Zeile und dritter Knopf hängen im Original am Wettbewerbscode DGROUP 0xD0C2 = 0 (Hauptmenü; nach
///   Freundschaftsspielen bleibt der Code bis zum nächsten Tag stehen, daher fehlen sie in DOSBox 44 und
///   <c>bundestrainer_00417</c>). Das Remake zeigt die Zeile immer und den dritten Knopf (Einschätzung des Gegners, Vereinsfeld
///   0x34 Bits 6–7 und DGROUP 0xE1B6, Symbole aus einem nicht zugeordneten Bild) nie.</item>
/// <item>[APPROX] Das Gesicht neben der Flagge (0x110:0x5C, Quelle nicht gelesen) bleibt gelb; Lage vermessen.</item>
/// <item>[APPROX] „Tore / Spiel“ kommt im Original aus 0xE0:0x70 (nicht gelesen): hier Tore · 10 / Spiele je Seite, „a.b:c.d“.</item>
/// </list>
/// </remarks>
public sealed class TeamInfoWindow
{
    public const int Left = 93;
    public const int Top = 44;
    public static readonly Rect Box = new(Left, Top, 9 * 24, 10 * 19);
    public static readonly Rect CloseButton = Widgets.ButtonRect(Left + 0x0A, Top + 0xAB);
    public const int LabelX = Left + 5;
    public const int ValueX = Left + 0x66;
    public const int RecordValueX = Left + 0x5A;
    public const int FirstBaseline = Top + 0x41;
    public const int LineStep = 6;
    public const byte TextColor = 30;
    public const byte NoSeriesColor = 6;

    /// <summary>[APPROX] Gesicht neben der Flagge, vermessen an DOSBox 14 und <c>bundestrainer_00610</c>.</summary>
    public const int MoodX = 250;
    public const int MoodY = 89;

    /// <summary>[ORIGINAL] Texte der Serien (DGROUP 0xF41 ff.).</summary>
    public static readonly string[] SeriesTexts =
        ["gewonnen", "verloren", "unentschieden", "nicht gewonnen", "nicht verloren", "ohne Gegentor", "ohne Torerfolg"];

    /// <summary>[ORIGINAL] Rekordtexte der Seite „Gesamt“ (DGROUP 0xF69 ff.).</summary>
    public static readonly string[] RecordTexts = ["Höchster Sieg", "Höchste Niederl.", "Erzielte Tore", "Kassierte Tore"];

    public const string NoEntry = "Noch keine Eintragung.";
    public const string NoSeries = "Keine bemerkenswerte Serie vorhanden.";

    private int? _club;

    public bool Visible => _club is not null;
    public int? Club => _club;

    public void Show(int club) => _club = club;

    public void Close() => _club = null;

    /// <summary>Liefert true, solange das Fenster offen ist (Eingaben verbraucht).</summary>
    public bool Update(InputState input)
    {
        if (_club is null)
            return false;
        if (input.Clicked(CloseButton) || input.RightClick || input.Pressed(Key.Escape) || input.Pressed(Key.Enter))
            Close();
        return true;
    }

    /// <summary>Ein gezeichneter Text des Zeilenlaufs (Mini).</summary>
    public sealed record Line(string Text, int X, int Baseline, byte Color);

    /// <summary>Zeilen ab „Bilanz:“ in der Reihenfolge des Originals (Datei 0x4D8EC–0x4E3C5) für ein Land.</summary>
    public static IReadOnlyList<Line> Lines(GameState game, int nation)
    {
        var lines = new List<Line>();
        var y = FirstBaseline;
        void Add(string text, int x, byte color = TextColor) => lines.Add(new Line(text, x, y, color));

        var (games, wins, losses, goalsFor, goalsAgainst) = game.NationalGroupRecord(nation);
        Add("Bilanz:", LabelX);
        Add($"{wins} Siege, {games - wins - losses} Unent., {losses} Nied.", ValueX);
        y += LineStep;

        var (points, played) = FormPoints(game.NationalForm(nation));
        Add("Aus d. letzten Spielen:", LabelX);
        Add($"{points}:{2 * played - points} Punkte", ValueX);
        y += LineStep;

        Add("Tore / Spiel (Gesamt):  ", LabelX);
        Add(Average(goalsFor, games) + ":" + Average(goalsAgainst, games), ValueX);
        y += LineStep;

        Add("Serien (Gesamt):  ", LabelX);
        y += LineStep;
        var series = game.Clubs[nation].SeriesCurrent;
        var count = 0;
        for (var kind = 0; kind < SeriesTexts.Length && count < 4; kind++)
        {
            var value = series[Series.Index((SeriesKind)kind, SeriesColumn.Total)];
            if (value <= 1)
                continue;
            Add(CompetitionWidgets.Number(value, 2) + " Spiele in Folge " + SeriesTexts[kind], LabelX);
            y += LineStep;
            count++;
        }
        if (count == 0)
        {
            Add(NoSeries, Left + 0x0A, NoSeriesColor);
            y += LineStep;
        }
        y += 2;

        for (var r = 0; r < 4; r++)
        {
            Add(RecordTexts[r], LabelX);
            Add(RecordValue(game, nation, r), RecordValueX);
            y += LineStep;
        }
        y += 2;

        if (HeadToHeadSlot(game, nation) is { } slot)
        {
            Add("Historische Ergebnisse gegen " + game.Clubs[NationalTeams.Germany].Name, LabelX);
            y += LineStep;
            Add(HeadToHeadText(game.HeadToHead(slot, nation)), LabelX);
        }
        return lines;
    }

    /// <summary>[ORIGINAL] Punkte und Spiele aus den Bilanzzeichen (Datei 0x51AAF: Sieg 2, Unentschieden 1).</summary>
    public static (int Points, int Games) FormPoints(IReadOnlyList<int> form)
    {
        int points = 0, games = 0;
        foreach (var f in form)
        {
            if (f == 0)
                continue;
            games++;
            points += f == 3 ? 2 : f == 2 ? 1 : 0;
        }
        return (points, games);
    }

    /// <summary>[APPROX] Schnitt mit einer Nachkommastelle, abgeschnitten (ohne Spiel 0.0).</summary>
    public static string Average(int goals, int games)
    {
        var tenths = games == 0 ? 0 : goals * 10 / games;
        return tenths / 10 + "." + tenths % 10;
    }

    /// <summary>[ORIGINAL] Text und Rückgabe eines Rekords (Datei 0x4E5D5).</summary>
    public static (string Text, int Low) RecordText(GameState game, int club, TeamRecordKind kind)
    {
        var (word, opponent) = game.TeamRecord(club, kind);
        if (word == 0)
            return (NoEntry, 0);
        var k = (int)kind;
        var high = word >> 8 & 0xFF;
        var low = word & 0xFF;
        string value;
        if (k is 0 or 1 or 4 or 5)
        {
            var (first, second) = k <= 3 ? (high, low) : (low, high);
            value = CompetitionWidgets.Number(first, 2) + ":" + CompetitionWidgets.Number(second, 2);
        }
        else
            value = CompetitionWidgets.Number(high != 0 ? high : low, 2);
        var name = game.Clubs.ElementAtOrDefault(opponent)?.Name ?? "";
        return (value + " (" + name + ")", low);
    }

    /// <summary>[ORIGINAL] Wert der Rekordzeile <paramref name="row"/> 0 … 3 auf der Seite „Gesamt“ (Datei 0x4DF3D–0x4E030).</summary>
    public static string RecordValue(GameState game, int club, int row)
    {
        var (homeText, home) = RecordText(game, club, (TeamRecordKind)row);
        var (awayText, away) = RecordText(game, club, (TeamRecordKind)(row + 4));
        if (home == 0 && away == 0)
            return homeText;
        if (row < 2)
        {
            home = Math.Abs((home >> 4) - (home & 0xF));
            away = Math.Abs((away >> 4) - (away & 0xF));
        }
        return home > away ? homeText : awayText;
    }

    /// <summary>
    /// [ORIGINAL] Platz der Kopf-an-Kopf-Liste oder null, wenn die Zeile fehlt (Datei 0x4E072–0x4E0D4): nicht für Deutschland
    /// und nicht für den Verein des Managers am Zug; Bundestrainer am Zug → Platz 0, sonst die Managernummer.
    /// </summary>
    public static int? HeadToHeadSlot(GameState game, int club)
    {
        if (club == NationalTeams.Germany)
            return null;
        if (game.NationalCoachMenu)
            return 0;
        if (game.ActiveManager.ClubIndex == club)
            return null;
        return game.CurrentManager;
    }

    /// <summary>[ORIGINAL] Liste der Ergebnisse (Datei 0x4E0EB–0x4E2D4): erst Plätze 1, 3 … 9, dann 0, 2 … 8, je bis zum ersten leeren.</summary>
    public static string HeadToHeadText(IReadOnlyList<int> list)
    {
        var text = "";
        for (var side = 1; side >= 0; side--)
            for (var i = 0; i < 5; i++)
            {
                var v = list[2 * i + side];
                if (v == GameState.HeadToHeadEmpty)
                    break;
                if (text.Length > 0)
                    text += ",";
                text += (v >> 4) + ":" + (v & 0xF);
            }
        return text;
    }

    public void Draw(Surface s, Assets a, GameState game)
    {
        if (_club is not { } club)
            return;
        Widgets.Frame(s, a, Box, FrameStyle.Orange);
        s.Outline(new Rect(258, 52, 40, 40), 4);
        Widgets.Logo(s, a, game.Clubs[club].LogoGraphic, 259, 53);
        NationalWidgets.FillInclusive(s, 122, 58, 228, 87, 1);
        s.Outline(new Rect(122, 58, 107, 30), 3);
        Widgets.Smiley(s, a, Widgets.Mood.Same, MoodX, MoodY);
        var form = game.NationalForm(club);
        for (var i = 0; i < form.Count; i++)
            if (form[i] != 0)
                Widgets.Smiley(s, a, form[i] == 3 ? Widgets.Mood.Up : form[i] == 1 ? Widgets.Mood.Down : Widgets.Mood.Same, 118 + 8 * i, 89);
        NationalWidgets.FillInclusive(s, 96, 96, 301, 229, 4);
        CompetitionWidgets.Centered(s, a.Flow, 124, 235, 55, game.Clubs[club].Name, TextColor, 0);
        s.HLine(98, 95, 305 - 98 + 1, 29);
        var mini = a.Mini;
        // [ORIGINAL] Knopf 0x120:0x75 wie in PlayerStatisticsScreen: kb\2 (17 | 179), Text zentriert x … x + 59, Grundlinie y + 7.
        s.Blit(a.Image(2), new Rect(17, 179, Widgets.ButtonWidth, Widgets.ButtonHeight), CloseButton.X, CloseButton.Y,
            transparent: 0);
        CompetitionWidgets.Centered(s, mini, CloseButton.X, CloseButton.X + 59, CloseButton.Y + 7, "Reicht mir", 29, 1);
        CompetitionWidgets.Centered(s, mini, Left, Left + 0xD8, Top + 0x3A, "Stärke:" + game.NationalInfoStrength(club), TextColor, null);
        foreach (var line in Lines(game, club))
            CompetitionWidgets.Left(s, mini, line.X, line.Baseline, line.Text, line.Color);
    }
}
