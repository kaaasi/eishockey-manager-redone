using EishockeyManager.Core;
using System.Text.Json.Nodes;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur <c>kader-start</c>, Runde 14, Teil 1: Start-Liga ohne Manager (#605, R13KI-03) und Einsortieren bei der Talentsuche
/// (#625, R623-01).
/// </summary>
public class KaderStartRunde14Tests
{
    /// <summary>
    /// R13KI-03: Die Start-Liga ist ein Wert für alle Manager ([ORIGINAL] DGROUP 0x11A, L1) und lässt sich schon vor dem ersten
    /// Manager einstellen; ein danach angelegter Manager übernimmt sie. Vorher: <c>Step</c> meldete <c>true</c>, der Wert blieb
    /// Bundesliga.
    /// </summary>
    [Fact]
    public void StartLeague_WithoutManagers_IsKept_AndAdoptedByNewManager()
    {
        var setup = new GameSetup();

        Assert.True(setup.Step(SetupSlider.StartLeague, +1, null));
        Assert.Equal(League.ZweiteLiga, setup.StartLeague);

        var m = setup.AddManager(new ManagerSetup { Name = "A", Head = 0 });
        Assert.Same(m, Assert.Single(setup.Managers));
        Assert.Equal(League.ZweiteLiga, m.StartLeague);
        Assert.Equal(League.ZweiteLiga, setup.StartLeague);

        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 1);
        Assert.Equal(League.ZweiteLiga, game.Clubs[game.Managers[0].ClubIndex].League);
    }

    /// <summary>Am oberen Ende (Oberliga) und mit „eine Saison“ ohne Manager: kein Schritt, <c>false</c> (U15-b, Datei 0x2890E).</summary>
    [Fact]
    public void StartLeague_WithoutManagers_StepReportsOnlyRealChanges()
    {
        var setup = new GameSetup();
        Assert.True(setup.Step(SetupSlider.StartLeague, +2, null));
        Assert.Equal(League.Oberliga, setup.StartLeague);
        Assert.False(setup.Step(SetupSlider.StartLeague, +1, null));

        setup.StartLeague = League.Bundesliga;
        setup.Duration = GameDuration.EineSaison;
        Assert.False(setup.Step(SetupSlider.StartLeague, +1, null));
        Assert.Equal(League.Bundesliga, setup.StartLeague);
    }

    /// <summary>Entfernt man den letzten Manager, bleibt die eingestellte Start-Liga erhalten (ein globaler Wert, L1).</summary>
    [Fact]
    public void StartLeague_SurvivesRemovalOfLastManager()
    {
        var setup = new GameSetup();
        setup.AddManager(new ManagerSetup { Name = "A", Head = 0 });
        Assert.True(setup.Step(SetupSlider.StartLeague, +2, setup.Managers[0]));
        setup.Managers.RemoveAt(0);

        Assert.Equal(League.Oberliga, setup.StartLeague);
        Assert.Equal(League.Oberliga, setup.AddManager(new ManagerSetup { Name = "B", Head = 1 }).StartLeague);
    }

    /// <summary>
    /// R623-01: Die Talentsuche legt den neuen Kaderplatz über 0x42CB1 an ([ORIGINAL] Aufruf Datei 0x06EB9 in 0x06D7F, Managerkader):
    /// ein Talent-Torhüter oder -Verteidiger steht hinter dem letzten Spieler seiner Art, nicht hinten (Regel Datei 0x42D55–0x42DA1).
    /// </summary>
    [Fact]
    public void JoinScoutedTalent_GoalkeeperOrDefender_SortedIn()
    {
        var game = TestGames.Create(3);
        var me = game.ActiveManager;
        var club = game.Clubs[me.ClubIndex];
        for (var i = 0; i < 60; i++)
        {
            var talent = game.JoinScoutedTalent(me, 60);
            Assert.NotNull(talent);
            if (talent.Role == PlayerRole.Forward)
            {
                Assert.Same(talent, club.Players[^1]);
                club.RemovePlayer(talent);
                continue;
            }

            var at = club.Players.IndexOf(talent);
            Assert.All(club.Players.Take(at), q => Assert.True(q.Role <= talent.Role, $"{q.Name} ({q.Role}) vor {talent.Name}"));
            Assert.True(club.Players[at + 1].Role > talent.Role, "vorher: hinten angehängt");
            Assert.Equal(Enumerable.Range(1, club.Players.Count), club.Players.Select(p => p.Number));
            return;
        }
        Assert.Fail("kein Torhüter oder Verteidiger unter 60 Talenten");
    }

    /// <summary>Migration 41 → 42 (<c>SaveGame.Migration.KaderStart.cs</c>): ältere Stände übernehmen die Start-Liga von Manager 0.</summary>
    [Fact]
    public void Migration_41To42_TakesStartLeagueOfFirstManager()
    {
        var setup = new GameSetup();
        setup.StartLeague = League.ZweiteLiga;
        setup.AddManager(new ManagerSetup { Name = "A", Head = 0 });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 1);
        var root = JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        Assert.Equal(SaveGame.SchemaVersion, (int)root["schema"]!);
        root["schema"] = 41;
        Assert.True(root["spiel"]!["Setup"]!.AsObject().Remove("_startLeague"));
        root["spiel"]!["Setup"]!["Managers"]!["items"]![0]!["StartLeague"] = "Oberliga";

        var result = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));

        Assert.True(result.Success, result.Error);
        Assert.Equal(League.Oberliga, result.Game!.Setup.StartLeague);
        result.Game.Setup.Managers.Clear();
        Assert.Equal(League.Oberliga, result.Game.Setup.StartLeague);
        var again = SaveGame.Save(result.Game);
        Assert.Equal(again, SaveGame.Save(SaveGame.Load(again).Game!));
    }
}
