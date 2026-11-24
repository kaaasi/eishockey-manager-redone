using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Befund #1012 (SA-01, Review #1009 zu #1005): Bilanzzeichen je Team (<c>GameState.TeamForm</c>) über vier Saisonwechsel, passiver
/// und aktiver Manager. [ORIGINAL] Das Original schiebt die Zeichen nur bei Wettbewerbscode 7 (Liga) und leert alle am Saisonwechsel
/// (Datei 0x55EBA–0x55EE0); im Remake hängen sie an <c>Results</c> (nur Ligaspiele) und dessen Leerung in <c>StartNextSeason</c>.
/// Rückschaltprobe: <c>Results.Clear()</c> in <c>SeasonFlow.cs</c> (StartNextSeason) entfernen → rot.
/// </summary>
public class TeamFormMultiSeasonTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TeamForm_FourChanges_ClearedAtSeasonChange_OnlyLeagueGames_SurvivesSaveLoad(bool active)
    {
        var game = TestGames.Create(3);
        var season = game.Season;
        var changes = 0;
        var filled = 0;
        RngTwinProbeTests.Run(game, active, afterStep: g =>
        {
            if (g.Season != season)
            {
                season = g.Season;
                changes++;
                // Saisonwechsel: alle Teams „--------“.
                Assert.All(g.Clubs.Where(c => c.Index < NewGame.SelectableClubs), c => Assert.All(g.TeamForm(c.Index), z => Assert.Equal(0, z)));
                var empty = SaveGame.Load(SaveGame.Save(g)).Game!;
                Assert.All(empty.Clubs.Where(c => c.Index < NewGame.SelectableClubs), c => Assert.All(empty.TeamForm(c.Index), z => Assert.Equal(0, z)));
                return;
            }
            foreach (var club in g.Clubs.Where(c => c.Index < NewGame.SelectableClubs))
            {
                var form = g.TeamForm(club.Index);
                Assert.Equal(8, form.Count);
                // Nur Ligaspiele (Results): so viele Zeichen wie Spiele, höchstens acht; Play-off- und Pokalspiele bleiben draußen.
                Assert.Equal(Math.Min(8, g.Results.Count(r => r.Involves(club.Index))), form.Count(z => z != 0));
                Assert.All(form, z => Assert.InRange(z, 0, 3));
                if (form.Any(z => z != 0))
                    filled++;
            }
            if (g.PlayedMatchDays == 10)
            {
                var loaded = SaveGame.Load(SaveGame.Save(g)).Game!;
                foreach (var c in g.Clubs.Where(c => c.Index < NewGame.SelectableClubs))
                    Assert.Equal(g.TeamForm(c.Index), loaded.TeamForm(c.Index));
            }
        });
        Assert.Equal(4, changes);
        Assert.True(filled > 0, "nie ein Zeichen gesetzt");
    }
}
