using EishockeyManager.Core;
using EishockeyManager.UI.Screens.Competitions;

namespace EishockeyManager.UI.Screens;

/// <summary>
/// Ligatabelle (Hauptmenü → Tabellen → 1. Liga / 2. Liga / Oberliga). Layout vermessen aus Aufnahmen
/// des Originals: Fenster 312 × 152 bei (4, 38), Knöpfe Heim / Gesamt / Auswärts.
/// Punkte nach der 2-Punkte-Regel als Plus:Minus, Bilanz = letzte acht Spiele in jeder Ansicht (klein = Heim, groß =
/// auswärts; em.exe 0x50283, 0x4F78D), TD = Tendenz zum Vortag, Platz 1–2 grün und 11–12 rot hinterlegt.
/// Reihenfolge je Ansicht: <see cref="LeagueTable.Compute"/> (Tauschsortierung 0x50B45).
/// </summary>
public sealed class TableScreen : Screen
{
    public static readonly Rect Window = new(4, 38, 312, 152);
    public const int FirstRowY = 69;
    public const int RowPitch = 7;
    private const int ButtonY = 164;
    private static readonly int[] ButtonX = [31, 133, 234];
    private static readonly string[] ButtonText = ["Heim", "Gesamt", "Auswärts"];

    private readonly League _league;
    private readonly Screen _back;
    private TableView _view = TableView.Gesamt;

    public TableScreen(ScreenHost host, League league, Screen back) : base(host)
    {
        _league = league;
        _back = back;
    }

    public override void Update(InputState input)
    {
        if (input.Pressed(Key.Escape) || input.RightClick)
        {
            Host.Show(_back);
            return;
        }
        for (var i = 0; i < 3; i++)
            if (input.Clicked(Widgets.ButtonRect(ButtonX[i], ButtonY)))
                _view = (TableView)i;
        if (input.LeftClick && !Window.Contains(input.MouseX, input.MouseY))
            Host.Show(_back);
    }

    public override void Draw(Surface s)
    {
        var game = Host.Game!;
        var mini = Assets.Mini;
        s.Palette = Assets.Palette;
        s.Blit(Assets.Image(MainMenuScreen.Background), 0, 0);
        Widgets.Frame(s, Assets, Window, FrameStyle.Orange);

        // [ORIGINAL] 0x4F78D: Titel „   TABELLE “ (DGROUP 0x1011, drei führende Leerzeichen) + Ansicht + „ “ + Spieltag
        // DGROUP 0x7A9 + „.Spieltag“, zentriert zwischen x = 5 und 313 auf Grundlinie 54, Farbe 10, Schatten 28. 0x7A9 steht
        // beim Spielstart auf 1 (Datei 0x43A7E): vor dem ersten Spieltag „1.Spieltag“ (Video 13:07), danach die Zahl der
        // gespielten Spieltage (karriere_01021 „36.Spieltag“ bei 36 Spielen, folge21_02711 „3.Spieltag“ bei 3 Spielen).
        // [ORIGINAL] Abzug Datei 0x4F986: Zahl = 0x7A9 − 1 bei Aufruf aus dem Hauptmenü (Parameter 1, 0x4583B) und 0x7A9 > 1;
        // die automatische Tabelle nach dem Spieltag (Parameter 0, 0x37882) zeigt 0x7A9 vor dem Weiterzählen (0x37897).
        var matchDay = game.PlayedMatchDays;
        CompetitionWidgets.Centered(s, Assets.Flow, 5, 313, 54, Title(_view, matchDay), 10, 28);

        // [ORIGINAL] Kopfzeile DGROUP 0x100D als ein Text ab (10 | 64), Farbe 1; Linien (12 … 308 | 66) und (12 … 308 | 153).
        CompetitionWidgets.Left(s, mini, 10, 64, Header, 1);
        s.HLine(12, 66, 308 - 12 + 1, 1);
        s.HLine(12, 153, 308 - 12 + 1, 1);

        // [ORIGINAL] Markierung der Plätze (Datei 0x4FAA7 ff.): 1. Liga nur Platz 1, sonst die ersten DGROUP 0x7B8 + Liga = 2
        // in Farbe 15; die letzten DGROUP 0x7B9 + Liga = 2 in Farbe 18; je Block ein Rechteck x 10 … 20.
        var count = game.LeagueClubs(_league).Count();
        var top = _league == League.Bundesliga ? 1 : 2;
        s.Fill(new Rect(10, 68, 11, 7 * top), 15);
        s.Fill(new Rect(10, 68 + 7 * (count - 2), 11, 7 * 2), 18);

        var own = game.Managers.Select(m => m.ClubIndex).ToHashSet();
        var rows = LeagueTable.Compute(game, _league, _view, matchDay);
        foreach (var row in rows)
        {
            // [ORIGINAL] Zeile i auf Grundlinie 73 + 7 i; Farbe 30, Vereine der Manager 24; Platz „n.“ (Mindestbreite 2)
            // ab x = 10 in Farbe 8, Name ab x = 0x21, Bilanz ab x = 0x75, Zahlen als ein Text ab x = 0xA6.
            var baseline = FirstRowY + 4 + (row.Rank - 1) * RowPitch;
            var color = own.Contains(row.Club.Index) ? Colors.Yellow : Colors.White;
            CompetitionWidgets.Left(s, mini, 10, baseline, CompetitionWidgets.Number(row.Rank, 2) + ".", 8);

            var mood = row.PreviousRank == 0 || row.PreviousRank == row.Rank ? Widgets.Mood.Same
                : row.Rank < row.PreviousRank ? Widgets.Mood.Up : Widgets.Mood.Down;
            Widgets.Smiley(s, Assets, mood, 24, baseline - 4);

            CompetitionWidgets.Left(s, mini, 0x21, baseline, row.Club.Name, color);
            CompetitionWidgets.Left(s, mini, 0x75, baseline, Balance(row), color);
            CompetitionWidgets.Left(s, mini, 0xA6, baseline, Numbers(row), color);
        }

        for (var i = 0; i < 3; i++)
            Widgets.Button(s, Assets, ButtonX[i], ButtonY, ButtonText[i], pressed: (int)_view == i);
    }

    /// <summary>Titel wie 0x4F78D (Belege in <see cref="Draw"/>).</summary>
    public static string Title(TableView view, int playedMatchDays) =>
        $"   TABELLE {ViewName(view)} {Math.Max(1, playedMatchDays)}.Spieltag";

    /// <summary>[ORIGINAL] Kopfzeile DGROUP 0x100D.</summary>
    public const string Header =
        "PL. TD      VEREIN                          BILANZ            S-U-N     PUNKTE     TORE       TORDIFF.";

    /// <summary>
    /// [ORIGINAL] Bilanz DGROUP 0x825E + 0x50 · Verein: acht Zeichen, beim Spielstart „--------“, jedes Spiel schiebt nach
    /// links und hängt hinten an (0x4F4F4). Video: „--------“ vor dem ersten Spieltag (13:07), „-----sSs“ nach drei
    /// Spieltagen (folge21_02711).
    /// </summary>
    public static string Balance(TableRow row) => row.Balance.PadLeft(LeagueTable.BalanceLength, '-');

    /// <summary>
    /// [ORIGINAL] Zahlenzeile von 0x4F78D (Datei 0x4FDB1–0x50074) als ein Text: Siege, Unentschieden, Niederlagen
    /// (Mindestbreite 2, „ “ dazwischen), „    “, Punkte „P:M“ (2), „   “, Tore „T:G“ (3), „      “, Vorzeichen der Tordifferenz („`“ bei 0,
    /// „+“, „-“; DGROUP 0x53CC ff.) und Betrag (3). Fehlende Stellen so breit wie „0“ (<see cref="CompetitionWidgets.Number"/>).
    /// Das vorher kopierte „*“ (DGROUP 0x53AA, Datei 0x4FCB7) überschreibt das Original mit dem Kopieren der Siege (0x4FDBF).
    /// </summary>
    public static string Numbers(TableRow row)
    {
        static string N(int v, int w) => CompetitionWidgets.Number(v, w);
        var diff = row.GoalDifference;
        var sign = diff == 0 ? "`" : diff > 0 ? "+" : "-";
        return N(row.Wins, 2) + " " + N(row.Draws, 2) + " " + N(row.Losses, 2) + "    "
               + N(row.PointsPlus, 2) + ":" + N(row.PointsMinus, 2) + "   "
               + N(row.GoalsFor, 3) + ":" + N(row.GoalsAgainst, 3) + "      " + sign + N(Math.Abs(diff), 3);
    }

    private static string ViewName(TableView view) => view switch
    {
        TableView.Heim => "Heim",
        TableView.Auswaerts => "Auswärts",
        _ => "Gesamt",
    };
}

/// <summary>Anmeldung der Tabellen im Hauptmenü.</summary>
internal static class TableMenuEntries
{
    [MenuEntry("Tabelle 1. Liga")]
    private static Screen FirstLeague(ScreenHost host, Screen back) => new TableScreen(host, League.Bundesliga, back);

    [MenuEntry("Tabelle 2. Liga")]
    private static Screen SecondLeague(ScreenHost host, Screen back) => new TableScreen(host, League.ZweiteLiga, back);

    [MenuEntry("Tabelle Oberliga")]
    private static Screen Oberliga(ScreenHost host, Screen back) => new TableScreen(host, League.Oberliga, back);
}
