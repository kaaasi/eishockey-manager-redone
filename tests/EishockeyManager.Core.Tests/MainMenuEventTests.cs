using System.Text.Json.Nodes;
using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur ui-meldungen, Teil 4 (E4 = B, Entscheidung Nutzer 03.11.2026): was das Hauptmenü des Originals beim Aufbau ändert, als
/// Ereignis im Spielkern (<see cref="GameState.OnMainMenu"/>, Hauptmenü Datei 0x44E22, Prüfung <c>docs/analyse/re-runde-12.md</c> E4).
/// </summary>
public class MainMenuEventTests
{
    private static GameState TwoManagers(int seed = 3)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", Head = 0, ClubIndex = 2 });
        setup.Managers.Add(new ManagerSetup { Name = "B", Head = 1, ClubIndex = 7 });
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    /// <summary>
    /// [ORIGINAL] Kennzeichen DGROUP 0x5A50 + Manager: gesetzt beim Ablegen (0x4B5E6), beim Menüaufbau gezeigt und gelöscht
    /// (0x4527C–0x45291). Vorher hing die Leiste an ungelesenen Nachrichten (gelesen erst beim Zeichnen der Leiste).
    /// </summary>
    [Fact]
    public void NewMessage_ShowsOnFirstMenuBuild_ThenIsCleared()
    {
        var game = TwoManagers();
        var (a, b) = (game.Managers[0], game.Managers[1]);
        game.OnMainMenu(a, isReturn: false);
        Assert.False(a.HasNewMessage);

        game.Notify(a, "Der Stadion-Ausbau (Test)", "ist abgeschlossen.");
        Assert.True(a.HasNewMessage);
        Assert.False(b.HasNewMessage); // je Manager

        Assert.True(game.OnMainMenu(a, isReturn: true));
        Assert.False(a.HasNewMessage);
        Assert.False(game.OnMainMenu(a, isReturn: true)); // zweiter Aufbau: keine Leiste, auch wenn die Nachricht ungelesen ist
        Assert.Contains(game.Inbox(a), m => !m.Read);
    }

    /// <summary>Jede Nachrichtenart setzt das Kennzeichen (auch die Kreditanfrage des Netzwerkmodus).</summary>
    [Fact]
    public void LoanRequest_AlsoMarksNewMessage()
    {
        var game = TwoManagers();
        Assert.NotNull(game.RequestLoan(0, 1, 100_000));
        Assert.True(game.Managers[1].HasNewMessage);
        Assert.False(game.Managers[0].HasNewMessage);
    }

    /// <summary>
    /// [ORIGINAL] Neuaufstellung 0x426BC bei jeder Rückkehr ins Menü (Datei 0x450C6 ab 0x44FBD), nicht nur am Zuganfang.
    /// </summary>
    [Fact]
    public void ReturnToMenu_WithAutoLineup_LinesUpAgain()
    {
        var game = TwoManagers();
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        Assert.True(club.AutoLineupEnabled);
        var full = club.Lineup.Count;
        var player = club.Lineup.Players.First();
        club.Lineup.Remove(player);
        Assert.Equal(full - 1, club.Lineup.Count);

        game.OnMainMenu(game.ActiveManager, isReturn: true);
        Assert.Equal(full, club.Lineup.Count);

        // Ohne die Einstellung bleibt alles, wie es ist.
        club.AutoLineupEnabled = false;
        club.Lineup.Remove(club.Lineup.Players.First());
        game.OnMainMenu(game.ActiveManager, isReturn: true);
        Assert.Equal(full - 1, club.Lineup.Count);
    }

    /// <summary>
    /// [ORIGINAL] DGROUP 0xE1A4 = random(85, 115) zu Beginn jedes Managerzugs (Datei 0x44E3F). Regressionstest: Vorher war der
    /// Faktor rein aus Saison und Tag abgeleitet und damit für beide Manager eines Tages gleich.
    /// </summary>
    [Fact]
    public void ContractTurnFactor_IsDrawnPerManagerTurn_AndStableWithinTheTurn()
    {
        var game = TwoManagers();
        var differs = false;
        var values = new HashSet<int>();
        for (var day = 0; day < 30; day++)
        {
            var first = game.ContractTurnFactor;
            Assert.InRange(first, 85, 115);
            game.OnMainMenu(game.ActiveManager, isReturn: true);
            Assert.Equal(first, game.ContractTurnFactor); // Rückkehr ins Menü zieht nicht neu
            Assert.Equal(first, game.ContractTurnFactor); // Lesen ist rein
            game.Continue(); // Zug an Manager B
            Assert.Equal(1, game.CurrentManager);
            var second = game.ContractTurnFactor;
            differs |= first != second;
            values.Add(first);
            values.Add(second);
            game.Continue(); // Tag spielen, Zug an Manager A
        }
        Assert.True(differs, "zwei Managerzüge eines Tages ziehen je einen eigenen Faktor");
        Assert.True(values.Count > 5);
    }

    /// <summary>Das Ereignis lehnt fremde Manager ab.</summary>
    [Fact]
    public void OnMainMenu_RejectsForeignManager()
    {
        var game = TwoManagers();
        var other = TwoManagers(4).Managers[0];
        Assert.Throws<ArgumentException>(() => game.OnMainMenu(other, isReturn: false));
    }

    /// <summary>Kennzeichen und Faktor überstehen Sichern und Laden; Stände vor Schema 33 bekommen die Startwerte.</summary>
    [Fact]
    public void NewMessageAndTurnFactor_RoundTripAndMigrate()
    {
        var game = TwoManagers();
        game.Notify(game.Managers[1], "Test");
        var factor = game.ContractTurnFactor;
        var saved = SaveGame.Save(game);
        var loaded = SaveGame.Load(saved);
        Assert.True(loaded.Success, loaded.Error);
        Assert.True(loaded.Game!.Managers[1].HasNewMessage);
        Assert.False(loaded.Game.Managers[0].HasNewMessage);
        Assert.Equal(factor, loaded.Game.ContractTurnFactor);
        Assert.Equal(saved, SaveGame.Save(loaded.Game));

        var root = JsonNode.Parse(saved)!.AsObject();
        root["schema"] = 32; // vor Teil 4 (vorläufig 32 → 33)
        var spiel = root["spiel"]!.AsObject();
        spiel.Remove("_turnRandom");
        spiel.Remove("_contractTurnFactor");
        foreach (var m in root["manager"]!.AsArray().OfType<JsonObject>())
            m.Remove("_newMessage");
        var old = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.True(old.Success, old.Error);
        Assert.All(old.Game!.Managers, m => Assert.False(m.HasNewMessage));
        Assert.InRange(old.Game.ContractTurnFactor, 85, 115); // früherer abgeleiteter Wert bis zum nächsten Zug
        old.Game.Continue();
        Assert.InRange(old.Game.ContractTurnFactor, 85, 115);
    }
}
