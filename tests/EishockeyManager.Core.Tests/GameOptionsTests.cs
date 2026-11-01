using System.Text.Json.Nodes;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur <c>optionen-wirkung</c>, Teil 1: Spielstand-Optionen des Optionsbildschirms 0x2CD1C (<see cref="GameOptions"/>).
/// Belege: <c>docs/reverse-engineering.md</c>, „Wirkung der Spielstand-Optionen“.
/// </summary>
public class GameOptionsTests
{
    private static GameState NewTestGame()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", Head = 0, ClubIndex = 2 });
        return NewGame.Create(TestGames.CreateDatabase(), setup, 3);
    }

    /// <summary>
    /// R8-D03: [ORIGINAL] DGROUP 0x21D… = 0 × 10, 1, 0 …: DEB-Pokal (Index 10) an, Live-Darstellung (Index 9) 0, Europapokal und
    /// WM aus, Matrix aus; Spielgeschwindigkeit 70 (DGROUP 0x262). Vorher rot: kein Zustand der Optionen im Spielstand.
    /// </summary>
    [Fact]
    public void NewGame_HasOriginalInitialValues()
    {
        var options = NewTestGame().Options;
        Assert.Equal(0, options.LiveMode);
        Assert.True(options.ShowDebCup);
        Assert.False(options.ShowEuropeanCup);
        Assert.False(options.ShowWorldCup);
        Assert.All(options.Reports, Assert.False);
        Assert.Equal(70, options.MatchSpeed);
    }

    /// <summary>
    /// [ORIGINAL] 0x3702E–0x37085: Pokalspiele ohne Managerverein laufen bei eingeschaltetem Anzeigeschalter durch die
    /// Minutenschleife, sonst den schnellen Weg; je Wettbewerb genau sein Schalter (DEB 0x227, Europa 0x228, WM 0x229).
    /// </summary>
    [Theory]
    [InlineData(MatchCompetition.DebCup)]
    [InlineData(MatchCompetition.EuropeanCup)]
    [InlineData(MatchCompetition.WorldCup)]
    public void CupSwitch_SelectsMinuteLoopOnlyForItsCompetition(MatchCompetition competition)
    {
        var options = new GameOptions { ShowDebCup = false };
        Assert.All(Enum.GetValues<MatchCompetition>(), c => Assert.False(options.CupMatchUsesMinuteLoop(c)));

        switch (competition)
        {
            case MatchCompetition.DebCup: options.ShowDebCup = true; break;
            case MatchCompetition.EuropeanCup: options.ShowEuropeanCup = true; break;
            case MatchCompetition.WorldCup: options.ShowWorldCup = true; break;
        }
        foreach (var c in Enum.GetValues<MatchCompetition>())
            Assert.Equal(c == competition, options.CupMatchUsesMinuteLoop(c));
    }

    /// <summary>Liga, Play-offs und Aufstiegsrunde haben keinen Pokalschalter.</summary>
    [Theory]
    [InlineData(MatchCompetition.League)]
    [InlineData(MatchCompetition.Playoff)]
    [InlineData(MatchCompetition.Relegation)]
    public void NonCupCompetitions_HaveNoSwitch(MatchCompetition competition) =>
        Assert.Null(new GameOptions().ShowsCup(competition));

    /// <summary>
    /// Die Optionen ändern weder Spielmodus noch Wettbewerbe: [ORIGINAL] DGROUP 0xD0B9 wird nur in der Spielvorbereitung
    /// geschrieben (0x29858/0x29862).
    /// </summary>
    [Fact]
    public void Switches_DoNotChangeGameMode()
    {
        var game = NewTestGame();
        var mode = game.Mode;
        game.Options.ShowDebCup = false;
        game.Options.ShowEuropeanCup = true;
        game.Options.ShowWorldCup = true;
        game.Options.NextLiveMode();
        game.Options.NextLiveMode();
        Assert.Equal(mode, game.Mode);
        Assert.Equal(GameOptions.PermanentRinkLiveMode, game.Options.LiveMode);
    }

    /// <summary>[ORIGINAL] Index 9: inc, bei 3 → 0; bei 2 → 0, wenn DGROUP 0x5A56 ≠ 0 (Datei 0x2D32A ff.).</summary>
    [Fact]
    public void LiveMode_CyclesLikeTheOriginal()
    {
        var options = new GameOptions();
        options.NextLiveMode();
        Assert.Equal(1, options.LiveMode);
        options.NextLiveMode();
        Assert.Equal(2, options.LiveMode);
        options.NextLiveMode();
        Assert.Equal(0, options.LiveMode);

        options.LiveMode = 1;
        options.NextLiveMode(permanentRinkBlocked: true);
        Assert.Equal(0, options.LiveMode);
    }

    /// <summary>
    /// [ORIGINAL] Matrix: Liga · 3 + Zeile. Schaltertabellen DGROUP 0x4C06 (x) = 222, 222, 222, 252, … und 0x4BE8 (y) = 130,
    /// 144, 158, 130, … (Zeichenaufruf 0x2CBC5, x vor y): Index 0–2 liegen in der Spalte „1.Liga“. Bild 49: Ergebnisse
    /// 1. Liga = Index 1 (DGROUP 0x21E, vom Spieltagsablauf für Liga 1 gelesen), Tabelle 2. Liga = Index 5.
    /// Vorher rot: Zeile · 3 + Liga (Index 3 und 7).
    /// </summary>
    [Fact]
    public void Reports_AreIndexedByLeagueAndRow()
    {
        var options = new GameOptions();
        options.ToggleReport(ReportKind.Results, League.Bundesliga);
        options.ToggleReport(ReportKind.Table, League.ZweiteLiga);
        Assert.True(options.Reports[1]);
        Assert.True(options.Reports[5]);
        Assert.Equal(2, options.Reports.Count(r => r));
        Assert.True(options.ShowsReport(ReportKind.Results, League.Bundesliga));
        options.ToggleReport(ReportKind.Results, League.Bundesliga);
        Assert.False(options.ShowsReport(ReportKind.Results, League.Bundesliga));
    }

    /// <summary>[ORIGINAL] Regler 0–80 in Zehnerschritten.</summary>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(35, 30)]
    [InlineData(80, 80)]
    [InlineData(95, 80)]
    [InlineData(-10, 0)]
    public void MatchSpeed_IsClampedToTens(int input, int expected)
    {
        var options = new GameOptions();
        options.SetMatchSpeed(input);
        Assert.Equal(expected, options.MatchSpeed);
    }

    /// <summary>Geänderte Optionen überstehen Sichern und Laden.</summary>
    [Fact]
    public void Options_SurviveSaveAndLoad()
    {
        var game = NewTestGame();
        game.Options.ShowDebCup = false;
        game.Options.ShowWorldCup = true;
        game.Options.LiveMode = 2;
        game.Options.SetMatchSpeed(30);
        game.Options.ToggleReport(ReportKind.Intermission, League.Oberliga);

        var result = SaveGame.Load(SaveGame.Save(game));
        Assert.True(result.Success, result.Error);
        var loaded = result.Game!.Options;
        Assert.False(loaded.ShowDebCup);
        Assert.False(loaded.ShowEuropeanCup);
        Assert.True(loaded.ShowWorldCup);
        Assert.Equal(2, loaded.LiveMode);
        Assert.Equal(30, loaded.MatchSpeed);
        Assert.True(loaded.ShowsReport(ReportKind.Intermission, League.Oberliga));
        Assert.Equal(SaveGame.Save(game), SaveGame.Save(result.Game));
    }

    /// <summary>
    /// Migration 18 → 19: Spielstände ohne Optionen bekommen die Anfangswerte des Originals; Laden ergibt denselben Spielstand
    /// wie ein neues Spiel.
    /// </summary>
    [Fact]
    public void Migration_FromSchema18_UsesOriginalInitialValues()
    {
        var game = NewTestGame();
        var json = JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        json["schema"] = 18;
        Assert.True(json["spiel"]!["Setup"]!.AsObject().Remove("Options"));

        var result = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(json.ToJsonString()));
        Assert.True(result.Success, result.Error);
        Assert.Equal(SaveGame.Save(game), SaveGame.Save(result.Game!));
    }
}
