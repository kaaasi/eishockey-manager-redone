using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens.Statistics;
using EishockeyManager.UI.Screens.Team;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur statistik-anzeige, Teil 2 (Runde 25): B29 Bilanzzeichen und Gesicht im Bildschirm „Stärken“ (A361), Teamwerte vor dem
/// ersten Spiel (A700), B33 Balken der Spielerinfo (A372) und Gegentore beim Verein (A957).
/// </summary>
public class StrengthBalanceFaceScreenTests
{
    private static int[] Form(string marks) => marks.Select(c => c switch { 's' or 'S' => 3, 'u' or 'U' => 2, 'n' or 'N' => 1, _ => 0 }).ToArray();

    /// <summary>[ORIGINAL] Gesicht 0x51AAF: 2 + Siege − Niederlagen, erst danach auf 1 … 3 begrenzt (b15_gegnerfenster.py).</summary>
    [Theory]
    [InlineData("--------", 2)]
    [InlineData("s-------", 3)]
    [InlineData("n-------", 1)]
    [InlineData("uuuuuuuu", 2)]
    [InlineData("snsnsnsn", 2)]
    [InlineData("ssssnnnn", 2)]
    [InlineData("nnnnnsss", 1)]
    [InlineData("ssssssss", 3)]
    public void FormMood_LikeOriginal(string marks, int mood) => Assert.Equal(mood, GameState.FormMood(Form(marks)));

    /// <summary>
    /// Bilanzzeichen eines Vereins = seine Ligaspiele der Saison, das jüngste zuletzt (0x50283 schiebt nur bei Code 7); der
    /// Saisonwechsel leert sie (0x55EBA). Bildschirm „Stärken“: Zeichen bei (80 + 8 i | 36 + 15 n) und Gesicht bei (240 | 36 + 15 n)
    /// nach der Bilanz. Vorher: keine Zeichen, Gesicht immer gelb.
    /// </summary>
    [GameDataFact]
    public void StrengthScreen_DrawsFormMarksAndFace()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        for (var i = 0; i < 3; i++)
            d.PlayMatchDay();
        var league = game.Clubs[game.ActiveManager.ClubIndex].League;
        var screen = new StrengthScreen(d.Host, d.Host.Current!, league);
        d.Host.Show(screen);
        d.Frame();
        var rows = screen.Rows;
        var sheet = d.Host.Assets.Image(33);
        bool IsFace(int x, int y, Widgets.Mood mood)
        {
            var src = mood switch { Widgets.Mood.Up => (25, 36), Widgets.Mood.Down => (19, 36), _ => (25, 41) };
            for (var dy = 0; dy < 5; dy++)
                for (var dx = 0; dx < 6; dx++)
                {
                    var p = sheet.Pixels[(src.Item2 + dy) * sheet.Width + src.Item1 + dx];
                    if (p != 4 && d.Surface.Pixels[(y + dy) * Surface.Width + x + dx] != p)
                        return false;
                }
            return true;
        }
        var moods = new HashSet<int>();
        for (var n = 0; n < rows.Count; n++)
        {
            var club = rows[n].Club.Index;
            var form = game.TeamForm(club);
            var games = game.Results.Where(r => r.Involves(club)).ToList();
            Assert.Equal(Math.Min(8, games.Count), form.Count(c => c != 0));
            var last = games[^1];
            var (f, a) = last.Home == club ? (last.HomeGoals, last.AwayGoals) : (last.AwayGoals, last.HomeGoals);
            Assert.Equal(f > a ? 3 : f == a ? 2 : 1, form[^1]);
            var y = 36 + StrengthScreen.RowPitch * n;
            for (var i = 0; i < 8; i++)
                if (form[i] != 0)
                    Assert.True(IsFace(StrengthScreen.MarksX + 8 * i, y, StrengthScreen.Mood(form[i])), $"Zeile {n}, Zeichen {i}");
            var mood = GameState.FormMood(form);
            moods.Add(mood);
            Assert.True(IsFace(240, y, StrengthScreen.Mood(mood)), $"Zeile {n}, Gesicht");
        }
        Assert.True(moods.Count > 1, "alle Gesichter gleich – Testaufbau ohne Aussage");
    }

    /// <summary>
    /// A700: [ORIGINAL] Der Lader 0x2EF1B läuft beim Programmstart (Datei 0x439BB vor 0x43A61); das Remake zieht die Teamwerte in
    /// <see cref="NewGame.Create"/>. Vor dem ersten Spiel zeigen die Länder deshalb Form 45 … 55 (DOSBox nur-weltkugel/27: 46 … 54),
    /// nicht überall 50.
    /// </summary>
    [GameDataFact]
    public void Strength_BeforeFirstMatch_ShowsLoadedForm()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        Assert.Equal(0, game.PlayedMatchDays);
        var nations = game.Clubs.Where(c => c.Index is >= NationalTeams.First and <= NationalTeams.Last);
        var forms = StrengthTable.Compute(game, nations, StrengthView.Gesamt).Select(r => r.Form).ToList();
        Assert.All(forms, f => Assert.InRange(f, 45, 55));
        Assert.True(forms.Distinct().Count() > 1);
    }

    /// <summary>
    /// [ORIGINAL] Balken 0x04C75 (Datei 0x04CB0–0x04DAD): Fläche Farbe 1 (Balken.Y … + 10, x + 1 … x + 99), innen schwarz, Farbband
    /// fünf Zeilen (Y + 3 … Y + 7), je Stufe 13 Pixel breit, Zeiger Y + 1 … Y + 9. Vorher Rand 2/3/197 und sechs Zeilen Farbband.
    /// </summary>
    [Fact]
    public void Bar_LikeFunction04C75()
    {
        var s = new Surface();
        var r = PlayerInfoPopup.Bar(0);
        PlayerInfoPopup.DrawBar(s, r, 0);
        byte At(int x, int y) => s.Pixels[y * Surface.Width + x];
        Assert.Equal(1, At(r.X + 1, r.Y));
        Assert.Equal(1, At(r.X + 99, r.Y + 10));
        Assert.Equal(0, At(r.X, r.Y + 5)); // links von x + 1 nichts
        Assert.Equal(0, At(r.X + 3, r.Y + 1));
        Assert.Equal(0, At(r.X + 50, r.Y + 2));
        Assert.Equal(0, At(r.X + 50, r.Y + 8));
        for (var y = r.Y + 3; y <= r.Y + 7; y++)
        {
            Assert.Equal(PlayerInfoPopup.BarColors[4], At(r.X + 2 + 12 * 4, y));
            Assert.Equal(PlayerInfoPopup.BarColors[7], At(r.X + 98, y));
        }
        // Stufe i reicht bis x + 14 + 12 i (die nächste überschreibt die Grenze)
        Assert.Equal(PlayerInfoPopup.BarColors[7], At(r.X + 14 + 12 * 7 - 12, r.Y + 3));
        // Zeiger bei Länge 1: x + 2, Y + 1 … Y + 9 in Farbe der ersten Stufe
        for (var y = r.Y + 1; y <= r.Y + 9; y++)
            Assert.Equal(PlayerInfoPopup.BarColors[0], At(r.X + 2, y));
    }

    /// <summary>
    /// A957: [ORIGINAL] Gegentore des Torhüters zählt nur Datei 0xA3F7 in +0x03 (Liga, Code 7); den Vereinswert +0x22 schreibt nur der
    /// Torschütze (Datei 0x542F4–0x54333), 0x42E9A beim Anlegen nur +0x03. In Klammern steht deshalb beim Torhüter immer „-“.
    /// </summary>
    [Fact]
    public void GoalieClubGoalsAgainst_AlwaysDash()
    {
        var goalie = new Player { Name = "T", Role = Formats.PlayerRole.Goalkeeper, GoalsAgainst = 17 };
        Assert.Equal("17( -)", PlayerDataWindow.CompetitionCell(goalie, 0, goals: true));
    }
}
