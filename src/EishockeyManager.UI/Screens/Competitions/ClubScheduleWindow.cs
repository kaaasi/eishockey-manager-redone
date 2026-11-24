using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens.Competitions;

/// <summary>
/// Spielplan des Gegners im Vereinsfenster (Spur zusehen, Teil 1b, A752): Knopf „Spielplan“ von <see cref="ClubInfoWindow"/>.
/// Überdeckt die untere Hälfte des Fensters mit dem Ligaspielplan des Vereins – je Zeile Gegner, „Platz. (Stärke)“ und die
/// beiden Begegnungen der gewählten Hälfte (Datum oder Ergebnis). Zeigt nur vorhandenen Zustand.
/// </summary>
/// <remarks>
/// [ORIGINAL] em.exe Datei 0x05CA0 (1822 B, Argumente (Team, a, b), nur von 0x4D385 gerufen: Datei 0x4E50C–0x4E539, Klick auf
/// „Spielplan“ und kein Land 0x4D2F0). Lage relativ zu (x | y) = (93 | 44) wie das Fenster:
/// <list type="bullet">
/// <item>[ORIGINAL] Fläche (x + 3 | y + 52)–(x + 208 | y + 185) Farbe 4 (0x0B620, Datei 0x5D1E–0x5D47), dann bei jedem Aufbau
///   (x + 137 | y + 61)–(x + 211 | y + 185) Farbe 4 (Datei 0x5D99–0x5DBC; reicht 3 Pixel über die erste hinaus).</item>
/// <item>[ORIGINAL] Knöpfe auf y + 171: „Reicht mir“ (x + 10, DGROUP 0xE84), „Info“ (x + 80, DGROUP 0xE88) und an der Stelle des
///   dritten Knopfs (x + 150) „2.Runde “ bzw. in der zweiten Hälfte „1.Runde “ (DGROUP 0xE70 + 4 · Hälfte, Datei 0x5E2E–0x5E58).
///   Alle Texte Schrift 3 (0x0BECD, Datei 0x5DFC).</item>
/// <item>[ORIGINAL] Hälfte zu Beginn: zweite, wenn Spieltagszähler DGROUP 0x7A9 + Liga + b &gt; Spieltage DGROUP 0x7B6 / 2
///   (Datei 0x5CF3–0x5D1A).</item>
/// <item>[ORIGINAL] Kopf (Grundlinie y + 60, Farbe 1): „GEGNER“ x + 15, „PL.(ST)“ x + 101 (0x0B651), „DATUM/ERGEBNIS“ zentriert
///   auf x + 175 (0x0B6A5, ohne Schatten; Datei 0x5E63–0x5EDD).</item>
/// <item>[ORIGINAL] Zeilen ab Grundlinie y + 69, Schritt 8: Spieltag d = 1 + Hälfte · 22 … + 10 (Spieltage / 2, / 4); Spalten d und
///   d + 11 (Datei 0x5EE0–0x6290). Je Spalte die Paarung des Vereins aus dem Spielplan 0x4E7C7: Heimspiel Farbe 30, Auswärts
///   Farbe 1 (Datei 0x5FAA–0x6044). Nur in der ersten Spalte und nur beim ersten Aufbau: Name des Gegners bei x + 5 (0x0B651)
///   und „Platz. (Stärke)“ des Gegners bei x + 101 (0x4F1F1 mit Kennzeichen 0: Platz + 1 und Stärke zweistellig, „. (“,
///   „)“, Datei 0x4F1F1–0x4F31C).</item>
/// <item>[ORIGINAL] Datum/Ergebnis zentriert auf x + 157 bzw. x + 194 (Datei 0x61EB–0x6228), Zahlen zweistellig (DGROUP 0x374 = 2,
///   Datei 0x6099): nicht gespielt „?:“ + Tag + „.“ + Monat + „.“ (0x064C9 n-ter Ligatag, 0x367B1; Datei 0x60A9–0x6133), gespielt
///   eigene Tore „:“ Gegentore (Ergebnistabelle DGROUP 0xA0BF, Datei 0x6136–0x61DD).</item>
/// <item>[ORIGINAL] Eingabe nur mit der Maus (Datei 0x629D–0x63B7): Klick auf den Rundenknopf wechselt die Hälfte und zeichnet nur
///   die Spalte Datum/Ergebnis und den Knopf neu (Name und Platz bleiben stehen); Rechtsklick und „Reicht mir“ schließen das
///   ganze Fenster (Rückgabe 1), „Info“ (Maus-x &gt; 172) kehrt ins Vereinsfenster zurück (Rückgabe 2, Datei 0x4E536).</item>
/// <item>[APPROX] Gespielt heißt im Remake: Ergebnis des Spieltags vorhanden (<see cref="GameState.Results"/>). Das Original
///   vergleicht den Spieltag mit DGROUP 0x7A9 + Liga + b; b = 1 nur aus den Paarungen (Seite 2, 0x5A; Datei 0x4F0C9–0x4F0DB) und
///   der Tabelle (Datei 0x5018C), weil dort der Zähler nach dem Spieltag noch nicht weitergezählt ist. Alle Aufrufer des Remakes
///   übergeben b = 0 (<see cref="Show"/>).</item>
/// <item>[APPROX] Esc schließt wie der Rechtsklick (wie im Vereinsfenster des Remakes; bleibt nach B11 = A, Nutzer
///   24.11.2026). Der Zähler 0x7A9 = 0 (Rückgabe 0 ohne
///   Fenster, Datei 0x5CEC) kommt im Remake nicht vor.</item>
/// </list>
/// </remarks>
public sealed class ClubScheduleWindow
{
    public const int Left = TeamInfoWindow.Left;
    public const int Top = TeamInfoWindow.Top;
    public static readonly Rect CloseButton = TeamInfoWindow.CloseButton;
    public static readonly Rect InfoButton = Widgets.ButtonRect(Left + 0x50, Top + 0xAB);
    public static readonly Rect RoundButton = TeamInfoWindow.WatchButton;

    /// <summary>[ORIGINAL] DGROUP 0xE70/0xE74 (mit Leerzeichen am Ende).</summary>
    public static readonly string[] RoundTexts = ["2.Runde ", "1.Runde "];

    public const string OpponentHeader = "GEGNER", PlaceHeader = "PL.(ST)", DateHeader = "DATUM/ERGEBNIS";

    /// <summary>[ORIGINAL] Farben der Zeilen (Datei 0x5FB2, 0x6005).</summary>
    public const byte HomeColor = 30, AwayColor = 1;

    /// <summary>[ORIGINAL] Spaltenmitten Datum/Ergebnis (x + 0x98 + 0 bzw. + 0x25).</summary>
    public static readonly int[] EntryCenters = [Left + 5 + 0x98, Left + 5 + 0x98 + 0x25];

    public const int FirstBaseline = Top + 0x3C + 9, RowStep = 8;

    public enum Outcome { Open, Close, Back }

    /// <summary>Eine Begegnung: Text (Datum oder Ergebnis) und Farbe (Heim 30, Auswärts 1).</summary>
    public sealed record Entry(string Text, byte Color);

    /// <summary>Eine Zeile: Gegner und Heimrecht der ersten Spalte, „Platz. (Stärke)“ des Gegners, beide Spalten.</summary>
    public sealed record Row(int Opponent, byte Color, string Place, IReadOnlyList<Entry> Entries);

    private int? _club;

    public bool Visible => _club is not null;
    public int? Club => _club;

    /// <summary>Gezeigte Hälfte: false = Spieltage 1–22, true = 23–44.</summary>
    public bool SecondHalf { get; private set; }

    /// <summary>Öffnet den Spielplan (Datei 0x05CA0); b wie im Original, im Remake immer 0 (Klassenkommentar).</summary>
    public void Show(GameState game, int club, int b = 0)
    {
        _club = club;
        var count = MatchDays(game, club);
        SecondHalf = game.PlayedMatchDays + 1 + b > count / 2;
    }

    public void Close() => _club = null;

    private static int MatchDays(GameState game, int club) =>
        game.Clubs[club].League is { } league && game.Schedules.TryGetValue(league, out var days) ? days.Count : 0;

    /// <summary>Bedient die Knöpfe; liefert, ob das Fenster offen bleibt, ganz schließt oder ins Vereinsfenster zurückkehrt.</summary>
    public Outcome Update(InputState input)
    {
        if (_club is null)
            return Outcome.Close;
        if (input.RightClick || input.Pressed(Key.Escape) || input.Clicked(CloseButton))
        {
            Close();
            return Outcome.Close;
        }
        if (input.Clicked(RoundButton))
            SecondHalf = !SecondHalf;
        else if (input.Clicked(InfoButton))
        {
            Close();
            return Outcome.Back;
        }
        return Outcome.Open;
    }

    /// <summary>[ORIGINAL] 0x4F1F1 mit Kennzeichen 0: „Platz. (Stärke)“, beide Zahlen zweistellig (DGROUP 0x374 = 2).</summary>
    public static string PlaceText(int rank, int strength) =>
        CompetitionWidgets.Number(rank, 2) + ". (" + CompetitionWidgets.Number(strength, 2) + ")";

    /// <summary>Zeilen der Hälfte (Datei 0x5EE0–0x6290).</summary>
    public static IReadOnlyList<Row> Rows(GameState game, int club, bool secondHalf)
    {
        var rows = new List<Row>();
        if (game.Clubs[club].League is not { } league || !game.Schedules.TryGetValue(league, out var days) || days.Count == 0)
            return rows;
        var count = days.Count;
        var start = count / 2 * (secondHalf ? 1 : 0) + 1;
        var table = LeagueTable.Compute(game, league, TableView.Gesamt, game.PlayedMatchDays);
        var dates = SeasonCalendar.LeagueMatchDays(count);
        for (var d = start; d < start + count / 4; d++)
        {
            int? opponent = null;
            byte color = 0;
            var entries = new List<Entry>();
            for (var day = d; day < d + count / 2; day += count / 4)
            {
                var matches = days[day - 1].Matches;
                var index = matches.ToList().FindIndex(m => m.Home == club || m.Away == club);
                if (index < 0)
                    continue;
                var (home, away) = matches[index];
                var isHome = home == club;
                var other = isHome ? away : home;
                var entryColor = isHome ? HomeColor : AwayColor;
                if (day == d)
                {
                    opponent = other;
                    color = entryColor;
                }
                var result = game.Results.FirstOrDefault(r => r.League == league && r.MatchDay == day && r.Involves(club));
                string text;
                if (result is not null)
                {
                    var (own, against, _) = result.For(club);
                    text = CompetitionWidgets.Number(own, 2) + ":" + CompetitionWidgets.Number(against, 2);
                }
                else
                {
                    var date = SeasonCalendar.LayoutDate(dates[day - 1]);
                    text = "?:" + CompetitionWidgets.Number(date.Day, 2) + "." + CompetitionWidgets.Number(date.Month, 2) + ".";
                }
                entries.Add(new Entry(text, entryColor));
            }
            if (opponent is not { } o)
                continue;
            var rank = table.FirstOrDefault(r => r.Club.Index == o)?.Rank ?? 0;
            rows.Add(new Row(o, color, PlaceText(rank, SpectatorRules.Strength(game, game.Clubs[o])), entries));
        }
        return rows;
    }

    /// <summary>Zeichnet über das bereits gezeichnete Vereinsfenster.</summary>
    public void Draw(Surface s, Assets a, GameState game)
    {
        if (_club is not { } club)
            return;
        NationalWidgets.FillInclusive(s, Left + 3, Top + 0x34, Left + 0xD0, Top + 0xB9, 4);
        NationalWidgets.FillInclusive(s, Left + 0x89, Top + 0x3D, Left + 0xD3, Top + 0xB9, 4);
        ClubInfoWindow.DrawButton(s, a, CloseButton, "Reicht mir");
        ClubInfoWindow.DrawButton(s, a, InfoButton, "Info");
        ClubInfoWindow.DrawButton(s, a, RoundButton, RoundTexts[SecondHalf ? 1 : 0]);
        var mini = a.Mini;
        var x = Left + 5;
        var baseline = Top + 0x3C;
        CompetitionWidgets.Left(s, mini, x + 0x0A, baseline, OpponentHeader, 1);
        CompetitionWidgets.Left(s, mini, x + 0x60, baseline, PlaceHeader, 1);
        CompetitionWidgets.Centered(s, mini, x + 0xAA, x + 0xAA, baseline, DateHeader, 1, null);
        // [ORIGINAL] Name und Platz stammen vom ersten Aufbau; nach einem Wechsel der Hälfte bleiben sie stehen. Die Gegner der
        // beiden Hälften sind dieselben (Spielplan 0x4E7C7: Runde 3 = Runde 1), daher genügt die gezeigte Hälfte.
        baseline = FirstBaseline;
        foreach (var row in Rows(game, club, SecondHalf))
        {
            CompetitionWidgets.Left(s, mini, x, baseline, game.Clubs[row.Opponent].Name, row.Color);
            CompetitionWidgets.Left(s, mini, x + 0x60, baseline, row.Place, row.Color);
            for (var i = 0; i < row.Entries.Count && i < EntryCenters.Length; i++)
                CompetitionWidgets.Centered(s, mini, EntryCenters[i], EntryCenters[i], baseline, row.Entries[i].Text, row.Entries[i].Color, null);
            baseline += RowStep;
        }
    }
}
