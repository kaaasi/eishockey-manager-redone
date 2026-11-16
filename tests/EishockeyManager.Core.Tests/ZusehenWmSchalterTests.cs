using System.Text;
using System.Text.Json.Nodes;
using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur zusehen, Teil 2 (A295, A541): Schalter „WM-Spiele“ (Option 12, DGROUP 0x229) und Markierung Deutschlands zum Zusehen.
/// Vorher schaltete Option 12 nur den Schalter (Deutschland blieb unmarkiert), und die Ernennung zum Bundestrainer ließ den
/// Schalter aus: WM-Gruppenspiele anderer Länder liefen dann im schnellen Weg statt in der Minutenschleife (Datei 0x3702E).
/// </summary>
public sealed class ZusehenWmSchalterTests
{
    private static GameState Game(int seed, params int[] managerClubs)
    {
        var setup = new GameSetup();
        foreach (var club in managerClubs)
            setup.Managers.Add(new ManagerSetup { Name = $"M{club}", Head = 0, ClubIndex = club });
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    private static Club Germany(GameState game) => game.Clubs[NationalTeams.Germany];

    /// <summary>
    /// [ORIGINAL] Datei 0x2D3CA–0x2D402: Manager am Zug mit Verein ≠ Deutschland → 0x0AE2B(Deutschland, 1, 2 · neuer Wert).
    /// Einschalten markiert Deutschland für die Live-Anzeige, Ausschalten nimmt die Markierung wieder weg.
    /// </summary>
    [Fact]
    public void Option12_MarksGermany()
    {
        var game = Game(1, 2);
        Assert.False(game.Options.ShowWorldCup); // [ORIGINAL] Anfangswert DGROUP 0x229 = 0
        Assert.Equal(GameState.WatchOff, Germany(game).WatchMode);

        Assert.True(game.ToggleWorldCupOption());
        Assert.True(game.Options.ShowWorldCup);
        Assert.Equal(GameState.WatchLive, Germany(game).WatchMode);

        Assert.False(game.ToggleWorldCupOption());
        Assert.Equal(GameState.WatchOff, Germany(game).WatchMode);
    }

    /// <summary>
    /// [ORIGINAL] Mit Verdrängen (Argument 1): Stehen schon vier Länder im Modus 2, weicht das erste ohne Manager (Datei 0xAEB4).
    /// </summary>
    [Fact]
    public void Option12_EvictsTheFirstNationWithoutManager()
    {
        var game = Game(1, 2);
        var nations = Enumerable.Range(NationalTeams.First, 4).Where(n => n != NationalTeams.Germany).Take(3).ToList();
        foreach (var n in nations)
            game.SetWatchMode(game.Clubs[n], evict: false, GameState.WatchLive);
        var last = Enumerable.Range(NationalTeams.Germany + 1, NationalTeams.Last - NationalTeams.Germany).First();
        game.SetWatchMode(game.Clubs[last], evict: false, GameState.WatchLive);
        Assert.Equal(4, game.Clubs.Count(c => c.Index >= NationalTeams.First && c.WatchMode == GameState.WatchLive));

        game.ToggleWorldCupOption();
        Assert.Equal(GameState.WatchLive, Germany(game).WatchMode);
        Assert.Equal(GameState.WatchGoals, game.Clubs[nations[0]].WatchMode);
    }

    /// <summary>
    /// Bundestrainer am Zug (Verein = Deutschland, Modus „nur Weltkugel“): [ORIGINAL] Datei 0x2D3EA springt am Schalter vorbei,
    /// nur der Schalter wechselt.
    /// </summary>
    [Fact]
    public void Option12_NationalCoachTurn_OnlyTheSwitch()
    {
        var game = GameModeTests.Create(7, false, true, 2);
        Assert.Equal(GameMode.WorldOnly, game.Mode);
        game.Continue(); // Sprung zur WM, Ernennung (Datei 0x54D38 → 0x553FF)
        Assert.True(game.NationalCoachMenu);
        var before = Germany(game).WatchMode;
        var option = game.Options.ShowWorldCup;
        Assert.Equal(!option, game.ToggleWorldCupOption());
        Assert.Equal(before, Germany(game).WatchMode);
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x5541F/0x5545F: Bei der Ernennung wird der Schalter nach 0x22C gesichert und eingeschaltet; Datei
    /// 0x55757–0x5575A stellt ihn nach der WM zurück (die Schleife 0x55570–0x55740 kehrt nie vor 0x5541F zurück, die Sicherung
    /// läuft je Saison einmal). Die Markierung Deutschlands (0x55457) bleibt.
    /// </summary>
    [Fact]
    public void Appointment_SwitchesOn_SeasonChangeRestores()
    {
        var game = Game(1, 2);
        game.AppointNationalCoach(game.Managers[0]);
        Assert.True(game.Options.ShowWorldCup);
        game.AppointNationalCoach(game.Managers[0]); // zweiter Aufruf derselben Saison: keine neue Sicherung
        game.StartNationalSeason();
        Assert.False(game.Options.ShowWorldCup);
        Assert.Equal(GameState.WatchLive, Germany(game).WatchMode);
        game.StartNationalSeason(); // ohne offene Sicherung: keine Änderung
        Assert.False(game.Options.ShowWorldCup);
    }

    /// <summary>
    /// Mehrsaisonprobe (AGENTS.md §5, Saisonwechsel): Modus „nur Weltkugel“, Ernennung jede Saison, vier Saisonwechsel, passiver
    /// und aktiver Manager (der aktive schaltet in der WM selbst aus – die Rückstellung nimmt den Wert vor der Ernennung).
    /// Während der WM ist der Schalter an, nach jedem Saisonwechsel wieder aus (das Remake rechnet Datei 0x55743 ff. im
/// Saisonwechsel <c>StartNextSeason</c>); Speichern/Laden mitten in der WM behält die
    /// Sicherung.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultiSeason_WorldOnly_SwitchOnDuringWorldCupOnly(bool active)
    {
        var game = GameModeTests.Create(7, false, true, 2);
        for (var change = 0; change <= 4; change++)
        {
            Assert.False(game.Options.ShowWorldCup);
            game.Continue();
            Assert.NotNull(game.NationalCoach);
            Assert.True(game.Options.ShowWorldCup);
            if (active)
            {
                game.AutoNominate();
                game.ToggleWorldCupOption();
                Assert.False(game.Options.ShowWorldCup);
            }
            var loaded = SaveGame.Load(SaveGame.Save(game));
            Assert.True(loaded.Success, loaded.Error);
            game = loaded.Game!;
            while (game.Phase == SeasonPhase.Running)
                game.Continue();
            if (change < 4)
            {
                game.StartNextSeason(); // Saisonwechsel: Rückstellung vor dem Amtsende (StartNationalSeason)
                Assert.False(game.Options.ShowWorldCup);
            }
        }
        Assert.Equal(5, game.History.Count);
    }

    /// <summary>
    /// Spielstand: Die Sicherung gehört zum Stand (HOK-Block 0x0A5D7 enthält 0x22C). Ein Stand des Schemas 51 lädt ohne
    /// Sicherung; der strenge Leser lehnt Schema 52 ohne das Feld ab.
    /// </summary>
    [Fact]
    public void SaveGame_KeepsTheBackup_Schema51WithoutIt()
    {
        var game = Game(1, 2);
        game.AppointNationalCoach(game.Managers[0]);
        var loaded = SaveGame.Load(SaveGame.Save(game));
        Assert.True(loaded.Success, loaded.Error);
        loaded.Game!.StartNationalSeason();
        Assert.False(loaded.Game.Options.ShowWorldCup);

        var root = JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        Assert.True(root["spiel"]!.AsObject().Remove("_worldCupOptionBackup"));
        Assert.False(SaveGame.Load(Encoding.UTF8.GetBytes(root.ToJsonString())).Success);
        root["schema"] = 51;
        var old = SaveGame.Load(Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.True(old.Success, old.Error);
        old.Game!.StartNationalSeason();
        Assert.True(old.Game.Options.ShowWorldCup); // ohne Sicherung bleibt der Schalter, wie er war
    }
}
