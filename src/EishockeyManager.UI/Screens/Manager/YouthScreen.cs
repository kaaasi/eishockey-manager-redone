using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens.ManagerArea;

/// <summary>
/// Bildschirm „Jugendteams“ (Manager → Jugend), vermessen am UI-Video (8:33–8:45): oben der Tabellenstand der
/// drei Jugendmannschaften als Fahnen in drei Ligen, unten die Förderung je Mannschaft als Balken (0–10.000 DM).
/// Klick in einen Balken setzt die Förderung.
/// </summary>
/// <remarks>
/// [APPROX] Talentsuche: Wo der „Spion“ im Original losgeschickt wird, ist im Video nicht zu sehen. Hier fragt ein
/// Klick in das Fenster „Jugendteams“ mit den Texten aus em.exe („Möchten Sie, daß Ihr 'Spion' …“, „Preis:“, „Rabatt:“);
/// Kästchen wie bei der Stadion-Rückfrage, Knöpfe „Ach bitte !“ / „Bloß nicht !“.
/// </remarks>
public sealed class YouthScreen : Screen
{
    public static readonly Rect ChartWindow = new(31, 21, 240, 115);
    public static readonly Rect FundingWindow = new(104, 157, 96, 76);

    /// <summary>Förderbalken der Mannschaft i (70 Pixel = 10.000 DM).</summary>
    public static Rect Bar(int i) => new(120, 168 + 24 * i, 71, 10);

    private const int ChartLeft = 57;
    private const int ChartRight = 243;
    private const int FirstLineY = 53;
    private const int LeaguePitch = 24;
    private static int FlagX(int i) => 60 + 69 * i;

    /// <summary>Farben rot, grün, blau (Video). [APPROX] Indizes der Standardpalette.</summary>
    public static readonly byte[] TeamColors = [Colors.Red, 15, 151];
    private const byte LineColor = Colors.Red;
    private const byte AxisColor = 50;
    private const byte LabelColor = 212;
    private const byte BarColor = 48;
    private const byte LineHighlight = 59;

    public static readonly Rect Dialog = new(60, 70, 200, 58);
    public static readonly Rect YesButton = new(75, 114, 80, 9);
    public static readonly Rect NoButton = new(165, 114, 80, 9);
    private const byte DialogText = Colors.White;
    private const byte DialogValue = Colors.Yellow;

    private readonly Screen _back;
    private bool _asking;
    private string[]? _notice;

    /// <summary>Rückfrage zur Talentsuche ist offen.</summary>
    public bool AsksTalentSearch => _asking;

    public YouthScreen(ScreenHost host, Screen back) : base(host) => _back = back;

    private GameState Game => Host.Game!;

    public override void Update(InputState input)
    {
        if (_asking || _notice is not null)
        {
            if (_asking && (input.Clicked(YesButton) || input.Pressed(Key.Enter)))
            {
                _asking = false;
                var result = Game.StartTalentSearch(Game.ActiveManager);
                if (result != TalentSearchStart.Started)
                    _notice = TalentScout.Message(result);
            }
            else if (input.LeftClick || input.RightClick || input.Keys.Count > 0)
                (_asking, _notice) = (false, null);
            return;
        }
        if (input.Pressed(Key.Escape) || input.RightClick)
        {
            Host.Show(_back);
            return;
        }
        if (!input.LeftClick)
            return;
        for (var i = 0; i < 3; i++)
        {
            var bar = Bar(i);
            if (bar.Inflate(1).Contains(input.MouseX, input.MouseY))
            {
                var share = Math.Clamp((input.MouseX - bar.X + 1) / (double)(bar.W - 1), 0, 1);
                Game.SetYouthFunding(Game.ActiveManager, (YouthTeamKind)i, (long)Math.Round(share * YouthTeam.MaxFunding));
                return;
            }
        }
        if (ChartWindow.Contains(input.MouseX, input.MouseY))
            AskTalentSearch();
        else if (!FundingWindow.Contains(input.MouseX, input.MouseY))
            Host.Show(_back);
    }

    private void AskTalentSearch()
    {
        var me = Game.ActiveManager;
        if (Game.Finance(me).TalentSearch is not null)
            _notice = TalentScout.Message(TalentSearchStart.AlreadyRunning);
        else if (Game.Clubs[me.ClubIndex].Players.Count >= TalentScout.MaxSquad)
            _notice = TalentScout.Message(TalentSearchStart.SquadFull);
        else
            _asking = true;
    }

    /// <summary>Unteres Ende der Fahnenstange: Liga-Band (24 Pixel) und Platz darin (6. Platz = Unterkante).</summary>
    public static int FlagBottom(YouthTeam team) =>
        FirstLineY + LeaguePitch * (team.League - 1) + LeaguePitch * team.Rank / YouthTeam.TeamsPerLeague;

    public override void Draw(Surface s)
    {
        var mini = Assets.Mini;
        var teams = Game.Finance(Game.ActiveManager).Youth;
        s.Palette = Assets.Palette;
        s.Blit(Assets.Image(MainMenuScreen.Background), 0, 0);

        Widgets.Frame(s, Assets, ChartWindow, FrameStyle.Orange, Colors.PanelFill);
        s.TextCentered(Assets.Bold, 151, 26, "Jugendteams", Colors.White);
        s.Fill(new Rect(ChartLeft - 3, FirstLineY, 3, LeaguePitch * 3 + 3), AxisColor);
        for (var l = 0; l <= 3; l++)
        {
            s.HLine(ChartLeft, FirstLineY + LeaguePitch * l - 1, ChartRight - ChartLeft, LineHighlight);
            s.HLine(ChartLeft, FirstLineY + LeaguePitch * l, ChartRight - ChartLeft, LineColor);
        }
        s.HLine(ChartLeft - 3, FirstLineY + LeaguePitch * 3 + 1, ChartRight - ChartLeft + 3, AxisColor);

        for (var i = 0; i < teams.Length; i++)
        {
            var team = teams[i];
            var color = TeamColors[i];
            var x = FlagX(i);
            var bottom = FlagBottom(team);
            // [APPROX] Fahne gezeichnet (im Original eine kleine Grafik mit Gesicht).
            s.VLine(x, bottom - 14, 15, Colors.White);
            for (var row = 0; row < 6; row++)
                s.HLine(x + 1, bottom - 14 + row, 10 - Math.Abs(row - 2) * 2, color);
            s.Text(mini, x + 2, bottom - 21, $"{team.Rank}.Platz", color);
            s.Text(mini, x + 37, bottom - 21, $"{team.League}.Liga", color);
        }

        Widgets.Frame(s, Assets, FundingWindow, FrameStyle.Orange, Colors.PanelFill);
        for (var i = 0; i < teams.Length; i++)
        {
            var bar = Bar(i);
            s.Text(mini, bar.X, bar.Y - 6, teams[i].Label, LabelColor);
            s.Text(mini, bar.X + 38, bar.Y - 6, ":", LabelColor);
            s.TextRight(mini, bar.Right, bar.Y - 6, teams[i].Funding.ToString(), LabelColor);
            s.Fill(bar, BarColor);
            s.Outline(bar, AxisColor);
            var width = (int)(teams[i].Funding * (bar.W - 2) / YouthTeam.MaxFunding);
            s.Fill(new Rect(bar.X + 1, bar.Y + 1, width, bar.H - 2), TeamColors[i]);
            s.Fill(new Rect(110, bar.Y + 3, 7, 5), TeamColors[i]);
        }

        if (_asking || _notice is not null)
            DrawDialog(s);
    }

    private void DrawDialog(Surface s)
    {
        var mini = Assets.Mini;
        var me = Game.ActiveManager;
        s.Fill(Dialog, Colors.Black);
        s.Outline(Dialog, AxisColor);
        if (_notice is { } notice)
        {
            for (var i = 0; i < notice.Length; i++)
                s.TextCentered(mini, 160, Dialog.Y + 18 + 9 * i, notice[i], DialogText);
            return;
        }
        s.TextCentered(mini, 160, Dialog.Y + 6, "Möchten Sie, daß Ihr 'Spion'", DialogText);
        s.TextCentered(mini, 160, Dialog.Y + 14, "für Sie auf Talentsuche geht?", DialogText);
        s.TextCentered(mini, 160, Dialog.Y + 25, "Preis: " + GameState.FormatMoney(Game.TalentSearchPrice(me)), DialogValue);
        s.TextCentered(mini, 160, Dialog.Y + 33, $"Rabatt: {Game.TalentSearchDiscount(me)}#" /* „#“ = Prozentzeichen im Original-Font */, DialogValue);
        s.TextCentered(mini, YesButton.X + YesButton.W / 2, YesButton.Y + 2, "Ach bitte !", DialogValue);
        s.TextCentered(mini, NoButton.X + NoButton.W / 2, NoButton.Y + 2, "Bloß nicht !", DialogValue);
    }
}

/// <summary>Anmeldung im Hauptmenü (Manager → Jugend).</summary>
internal static class YouthMenuEntries
{
    [MenuEntry("Jugend")]
    private static Screen Open(ScreenHost host, Screen back) => new YouthScreen(host, back);
}
