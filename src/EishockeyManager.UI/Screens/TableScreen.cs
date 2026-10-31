using EishockeyManager.Core;

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

        var matchDay = game.PlayedMatchDays;
        var title = $"TABELLE {ViewName(_view)} {matchDay}.Spieltag";
        Widgets.ShadowText(s, Assets.Flow, 165, 48, title, 11, 198);

        const byte header = 32;
        s.Text(mini, 10, 60, "PL. TD", header);
        s.Text(mini, 44, 60, "VEREIN", header);
        s.Text(mini, 122, 60, "BILANZ", header);
        s.Text(mini, 172, 60, "S-U-N", header);
        s.Text(mini, 207, 60, "PUNKTE", header);
        s.Text(mini, 245, 60, "TORE", header);
        s.Text(mini, 277, 60, "TORDIFF.", header);
        s.HLine(10, 66, 300, 1);
        s.HLine(10, 153, 300, 1);

        var own = game.Managers.Select(m => m.ClubIndex).ToHashSet();
        var rows = LeagueTable.Compute(game, _league, _view, matchDay);
        foreach (var row in rows)
        {
            var y = FirstRowY + (row.Rank - 1) * RowPitch;
            var color = own.Contains(row.Club.Index) ? Colors.Yellow : Colors.White;

            if (row.Rank <= 2)
                s.Fill(new Rect(10, y - 1, 12, 7), 12);
            else if (row.Rank > rows.Count - 2)
                s.Fill(new Rect(10, y - 1, 12, 7), Colors.Red);
            s.TextRight(mini, 22, y, $"{row.Rank}.", color);

            var mood = row.PreviousRank == 0 || row.PreviousRank == row.Rank ? Widgets.Mood.Same
                : row.Rank < row.PreviousRank ? Widgets.Mood.Up : Widgets.Mood.Down;
            Widgets.Smiley(s, Assets, mood, 24, y);

            s.Text(mini, 33, y, row.Club.Name, color);
            s.Text(mini, 117, y, row.Balance, color);
            s.TextRight(mini, 173, y, row.Wins.ToString(), color);
            s.TextRight(mini, 187, y, row.Draws.ToString(), color);
            s.TextRight(mini, 201, y, row.Losses.ToString(), color);
            Pair(s, 207, y, row.PointsPlus, row.PointsMinus, digits: 2, color);
            Pair(s, 236, y, row.GoalsFor, row.GoalsAgainst, digits: 3, color);
            var diff = row.GoalDifference;
            s.Text(mini, 277, y, diff < 0 ? "-" : "+", color);
            s.TextRight(mini, 298, y, Math.Abs(diff).ToString(), color);
        }

        for (var i = 0; i < 3; i++)
            Widgets.Button(s, Assets, ButtonX[i], ButtonY, ButtonText[i], pressed: (int)_view == i);
    }

    /// <summary>„22: 6“ – beide Zahlen rechtsbündig in Feldern fester Breite (Ziffern im Font: 5 Pixel).</summary>
    private void Pair(Surface s, int x, int y, int left, int right, int digits, byte color)
    {
        var field = digits * 5;
        s.TextRight(Assets.Mini, x + field, y, left.ToString(), color);
        s.Text(Assets.Mini, x + field, y, ":", color);
        s.TextRight(Assets.Mini, x + field + 2 + field, y, right.ToString(), color);
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
