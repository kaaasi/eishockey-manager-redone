using System.Text.Json.Nodes;
using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur pixel-marker, Teil 3 (R-K6, Entscheidung Nutzer D2 = C; D8 = A): Nachrichtenspeicher wie im Original – 24 gemeinsame
/// Plätze (em.exe Datei 0x4B101), Meldungen ohne Spieler räumen nach drei Tagen (Tageslauf Datei 0x54F07–0x54F87), Sortierung
/// (0x4B127), Löschen der Meldung eines Spielers (0x4B270). Kein Feld „gelesen“ je Nachricht.
/// </summary>
public class MessageStoreTests
{
    private static GameState TwoManagers(int seed = 3)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", Head = 0, ClubIndex = 2 });
        setup.Managers.Add(new ManagerSetup { Name = "B", Head = 1, ClubIndex = 7 });
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    private static void Day(GameState game) => game.AdvanceFinanceTo(game.FinanceDay + 1);

    /// <summary>Der 25. Platz entfällt (Datei 0x4B544: kein freier Satz, Rückgabe 0); die 24 Plätze teilen sich alle Manager.</summary>
    [Fact]
    public void Store_HasTwentyFourSharedSlots_FullStoreDropsNewMessage()
    {
        var game = TwoManagers();
        var (a, b) = (game.Managers[0], game.Managers[1]);
        for (var i = 0; i < 20; i++)
            Assert.NotNull(game.Notify(a, $"A {i}"));
        for (var i = 0; i < 4; i++)
            Assert.NotNull(game.Notify(b, $"B {i}"));
        Assert.Equal(GameState.MessageCapacity, game.MessagesStored);
        game.OnMainMenu(a, isReturn: false);
        game.OnMainMenu(b, isReturn: false);

        Assert.Null(game.Notify(a, "zu viel"));
        Assert.Null(game.Notify(b, "zu viel"));
        Assert.Null(game.RequestLoan(0, 1, 100_000));
        Assert.Equal(GameState.MessageCapacity, game.MessagesStored);
        Assert.False(a.HasNewMessage); // kein Platz, kein Kennzeichen (Datei 0x4B544 springt vor 0x4B5E6)
        Assert.False(b.HasNewMessage);
    }

    /// <summary>Meldungen ohne Spieler zählen im Tageslauf von 3 herunter und geben den Platz bei 0 frei.</summary>
    [Fact]
    public void InfoMessage_LeavesTheStoreAfterThreeDays()
    {
        var game = TwoManagers();
        var a = game.Managers[0];
        var message = game.Notify(a, "Der Stadion-Ausbau (Test)", "ist abgeschlossen.")!;
        Assert.Equal(3, message.DaysLeft);

        Day(game);
        Day(game);
        Assert.Contains(message, game.Inbox(a));
        Day(game);
        Assert.Empty(game.Inbox(a));
        Assert.Equal(0, game.MessagesStored);
        Assert.NotNull(game.Notify(a, "neu")); // Platz wieder frei
    }

    /// <summary>Fragen altern nicht, solange sie offen sind; danach (beantwortet oder verfallen) wie jede Meldung.</summary>
    [Fact]
    public void Question_StaysUntilAnswered_ThenAges()
    {
        var game = TwoManagers();
        var lender = game.Managers[1];
        var request = game.RequestLoan(0, 1, 100_000)!;
        for (var i = 0; i < 5; i++)
            Day(game);
        Assert.True(request.IsQuestion);
        Assert.Contains(request, game.Inbox(lender));

        Assert.True(game.Answer(lender, request, yes: false));
        for (var i = 0; i < 3; i++)
            Day(game);
        Assert.DoesNotContain(request, game.Inbox(lender));
    }

    /// <summary>Meldung zu einem Spieler (Angebot) bleibt, bis sie gelöscht wird (Datei 0x4B270) oder das Angebot erledigt ist.</summary>
    [Fact]
    public void PlayerMessage_StaysUntilRemoved()
    {
        var game = TwoManagers();
        var a = game.Managers[0];
        var club = game.Clubs[a.ClubIndex];
        var (p, q) = (club.Players[0], club.Players[1]);
        (p.Notice, q.Notice) = (ContractNotice.Offered, ContractNotice.Offered);
        var offerP = game.NotifyAbout(a, p, "Spieler P bietet Ihnen an", "zu verlängern.")!;
        var offerQ = game.NotifyAbout(a, q, "Spieler Q bietet Ihnen an", "zu verlängern.")!;
        for (var i = 0; i < 5; i++)
            Day(game);
        Assert.Contains(offerP, game.Inbox(a));
        Assert.Contains(offerQ, game.Inbox(a));

        game.RemoveMessagesOf(p);
        Assert.DoesNotContain(offerP, game.Inbox(a));
        Assert.Contains(offerQ, game.Inbox(a));

        q.Notice = ContractNotice.None; // Angebot erledigt (angenommen, verfallen): Tageslauf räumt die Meldung
        Day(game);
        Assert.DoesNotContain(offerQ, game.Inbox(a));
    }

    /// <summary>Tageslauf sortiert nach Saison und Tag (Datei 0x4B127, Schlüssel +1); gleiche Schlüssel behalten ihre Reihenfolge.</summary>
    [Fact]
    public void DailyRun_SortsByDay_Stable()
    {
        var game = TwoManagers();
        var a = game.Managers[0];
        var inbox = game.Finance(a).Inbox;
        Message Make(int day, string text) => new() { Kind = MessageKind.Info, Day = day, Lines = [text] };
        var (late, early1, early2) = (Make(50, "spät"), Make(10, "früh 1"), Make(10, "früh 2"));
        inbox.AddRange([late, early1, early2]);

        Day(game);
        Assert.Equal([early1, early2, late], inbox);
    }

    /// <summary>
    /// Mehrsaisonprobe (vier Saisonwechsel, passiver und aktiver Manager): der Speicher überschreitet nie 24 Plätze, Meldungen
    /// ohne Spieler liegen nie länger als drei Tage, und Sichern/Laden ändert nichts.
    /// </summary>
    [Fact]
    public void MultiSeason_FourChanges_StoreStaysWithinLimits()
    {
        var game = TwoManagers(5);
        var active = game.Managers[1];
        var firstSeason = game.Season;
        var posted = 0;
        for (var change = 0; change < 4; change++)
        {
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 2000, "Saisonende nicht erreicht");
                foreach (var m in game.Managers)
                    m.Money = 10_000_000;
                TestSeason.KeepSquad(game, active);
                game.Continue();
                MessageLog.For(game).Collect(game);
                Assert.InRange(game.MessagesStored, 0, GameState.MessageCapacity);
                foreach (var m in game.Managers)
                    Assert.All(game.Inbox(m), x => Assert.InRange(x.DaysLeft, 1, GameState.MessageLifetimeDays));
            }
            posted = MessageLog.For(game).All.Count;
            TestSeason.KeepSquad(game, game.Managers[0]);
            TestSeason.KeepSquad(game, active);
            game.StartNextSeason();
        }
        Assert.Equal(firstSeason + 4, game.Season);
        Assert.True(posted > GameState.MessageCapacity, $"nur {posted} Nachrichten in vier Saisons");

        var saved = SaveGame.Save(game);
        var loaded = SaveGame.Load(saved);
        Assert.True(loaded.Success, loaded.Error);
        Assert.Equal(saved, SaveGame.Save(loaded.Game!));
    }

    /// <summary>Schema 35 → 36: <c>Read</c> entfällt, <c>DaysLeft</c> = 3 und <c>Player</c> = null kommen hinzu.</summary>
    [Fact]
    public void Migration_DropsRead_AddsDaysLeftAndPlayer()
    {
        var game = TwoManagers();
        game.Notify(game.Managers[0], "alt", "Meldung");
        var root = JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        root["schema"] = 35;
        var count = 0;
        Visit(root, obj =>
        {
            if (obj.ContainsKey("DaysLeft") && obj.ContainsKey("Lines"))
            {
                obj.Remove("DaysLeft");
                obj.Remove("Player");
                obj["Read"] = true;
                count++;
            }
        });
        Assert.Equal(1, count);

        var loaded = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.True(loaded.Success, loaded.Error);
        var message = Assert.Single(loaded.Game!.Inbox(loaded.Game.Managers[0]));
        Assert.Equal(GameState.MessageLifetimeDays, message.DaysLeft);
        Assert.Null(message.Player);
    }

    private static void Visit(JsonNode? node, Action<JsonObject> action)
    {
        switch (node)
        {
            case JsonObject obj:
                action(obj);
                foreach (var child in obj.Select(f => f.Value).ToList())
                    Visit(child, action);
                break;
            case JsonArray array:
                foreach (var child in array.ToList())
                    Visit(child, action);
                break;
        }
    }
}
