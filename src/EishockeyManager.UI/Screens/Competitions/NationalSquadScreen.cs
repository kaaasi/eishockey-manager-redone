using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.UI.Screens.Competitions;

/// <summary>
/// „Spieler in WM-Kader“ (Bundestrainer, Kachel „Transfer“ im Menü des Nationalteams): Liste der Kandidaten,
/// Klick nimmt einen Spieler auf oder entlässt ihn, „auto“ stellt den Kader neu zusammen.
/// </summary>
/// <remarks>
/// Aufbau aus em.exe (Overlay 0xF8 Eintrag 12, Datei 0x3C37C); kein Videobeleg, daher kein Pixelvergleich:
/// <list type="bullet">
/// <item>[ORIGINAL] Fenster 12 × 12 Rahmenteile ab (16 | 2); Titel Bold zentriert 0 … 319, Grundlinie 21, Farbe 7 ohne
///   Schatten; Linie (27 … 292 | 23) Farbe 7; Kasten (21 | 28) … (298 | 185).</item>
/// <item>[ORIGINAL] Kopf (Mini, x = 35, Grundlinie 44, Farbe 7) „ART SPIELER VEREIN KO TE FO GES Lä-Sp.“, Linien
///   (32 … 287 | 45) und (32 … 287 | 155) Farbe 7.</item>
/// <item>[ORIGINAL] Liste: 15 Zeilen ab (32 | 48), Zeilenabstand 7, Grundlinie 5 + 7 · Zeile; Art x = 1, Name x = 20,
///   Verein x = 80 (Kaderspieler: das Nationalteam), Werte x = 163 „KO TE FO GES Lä-Sp.“ zweistellig. Farbe 7,
///   Kaderspieler 29, Verletzte 3.</item>
/// <item>[ORIGINAL] Knöpfe „-“, „auto“, „+“ bei x = 49 + 84 i, y = 165; „-“/„+“ blättern um 15 Zeilen.</item>
/// <item>[ORIGINAL] Unten links das Mannschaftsfeld des Nationalteams (<see cref="NationalWidgets.TeamPanel"/>) mit
///   Umriss (30 | 191) … (154 | 225) Farbe 1 und senkrechten Linien x = 29 / 155 (192 … 224) Farbe 0.</item>
/// </list>
/// </remarks>
public sealed class NationalSquadScreen : Screen
{
    public static readonly Rect Window = new(16, 2, 288, 228);
    public const string Title = "Spieler in WM-Kader";
    public const string Header = "ART     SPIELER                VEREIN                         KO    TE     FO   GES   Lä-Sp.";
    public static readonly Rect List = new(32, 48, 255, 104);
    public const int Rows = 15;
    public const int RowPitch = 7;
    public static Rect ButtonRect(int i) => Widgets.ButtonRect(49 + 84 * i, 165);
    public static readonly string[] ButtonText = ["-", "auto", "+"];

    private readonly Screen _back;

    /// <summary>Erste sichtbare Zeile der Kandidatenliste.</summary>
    public int First { get; private set; }

    public NationalSquadScreen(ScreenHost host, Screen back) : base(host) => _back = back;

    public IReadOnlyList<NationalSquadMember> Candidates =>
        Host.Game is { NationalCoach: not null } game ? game.NationalCandidates() : [];

    /// <summary>Kandidat in einer Bildschirmzeile oder null.</summary>
    public NationalSquadMember? At(int x, int y)
    {
        if (x < List.X || x >= List.Right || y <= List.Y || y >= List.Y + Rows * RowPitch)
            return null;
        var index = First + (y - List.Y) / RowPitch;
        var list = Candidates;
        return index < list.Count ? list[index] : null;
    }

    public override void Update(InputState input)
    {
        if (input.Pressed(Key.Escape) || input.RightClick)
        {
            Host.Show(_back);
            return;
        }
        if (Host.Game is not { NationalCoach: not null } game)
        {
            if (input.LeftClick)
                Host.Show(_back);
            return;
        }
        var count = Candidates.Count;
        if (input.Clicked(ButtonRect(0)) || input.Pressed(Key.PageUp))
            First = Math.Max(0, First - Rows);
        else if (input.Clicked(ButtonRect(2)) || input.Pressed(Key.PageDown))
            First = First + Rows < count ? First + Rows : First;
        else if (input.Clicked(ButtonRect(1)))
            game.AutoNominate();
        else if (input.LeftClick && At(input.MouseX, input.MouseY) is { } candidate)
            game.ToggleNomination(candidate.Player);
    }

    public override void Draw(Surface s)
    {
        s.Palette = Assets.Palette;
        s.Blit(Assets.Image(MainMenuScreen.Background), 0, 0);
        Widgets.Frame(s, Assets, Window, FrameStyle.Orange);
        CompetitionWidgets.Centered(s, Assets.Bold, 0, 319, 21, Title, Colors.White, null);
        s.HLine(27, 23, 292 - 27 + 1, Colors.White);
        NationalWidgets.Box(s, 21, 28, 298, 185);
        s.HLine(32, 45, 287 - 32 + 1, Colors.White);
        var mini = Assets.Mini;
        CompetitionWidgets.Left(s, mini, 35, 44, Header, Colors.White);
        s.HLine(32, 155, 287 - 32 + 1, Colors.White);
        NationalWidgets.Outline(s, 30, 191, 154, 225, 1);
        s.VLine(29, 192, 33, Colors.Black);
        s.VLine(155, 192, 33, Colors.Black);
        for (var i = 0; i < ButtonText.Length; i++)
            Widgets.Button(s, Assets, ButtonRect(i).X, ButtonRect(i).Y, ButtonText[i]);

        if (Host.Game is not { NationalCoach: { } coach } game)
            return;
        NationalWidgets.FillInclusive(s, List.X, List.Y, List.X + 254, List.Y + 103, 1);
        var list = Candidates;
        for (var row = 0; row < Rows && First + row < list.Count; row++)
        {
            var (p, club) = list[First + row];
            var member = coach.Contains(p);
            byte color = p.InjuredGames > 0 ? (byte)3 : member ? (byte)29 : Colors.White;
            var y = List.Y + 5 + RowPitch * row;
            CompetitionWidgets.Left(s, mini, List.X + 1, y, Role(p.Role), color);
            CompetitionWidgets.Left(s, mini, List.X + 20, y, p.Name, color);
            CompetitionWidgets.Left(s, mini, List.X + 80, y, game.Clubs[member ? coach.Nation : club].Name, color);
            static string N(int v) => CompetitionWidgets.Number(v, 2);
            var average = (p.Condition + p.Technique + p.Form) / 3;
            CompetitionWidgets.Left(s, mini, List.X + 163, y,
                N(p.Condition) + "    " + N(p.Technique) + "    " + N(p.Form) + "    " + N(average) + "     " + N(p.Caps), color);
        }
        NationalWidgets.TeamPanel(s, Assets, game, coach.Nation, 0);
    }

    /// <summary>[ORIGINAL] Kürzel der Art (DGROUP 0x944): „TOR“, „ABW“, „STU“.</summary>
    public static string Role(PlayerRole role) => role switch
    {
        PlayerRole.Goalkeeper => "TOR",
        PlayerRole.Defender => "ABW",
        _ => "STU",
    };
}
