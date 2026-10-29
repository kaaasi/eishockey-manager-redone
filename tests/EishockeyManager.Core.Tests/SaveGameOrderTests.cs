using EishockeyManager.Core;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur „speichern-reihenfolge“ (Befund S71-02, #80): Die Aufzählungsreihenfolge eines Wörterbuchs hängt von
/// früheren Entnahmen ab (wiederverwendete Plätze) und ist nach Sichern und Laden eine andere. Der Spielstand
/// verspricht deshalb keine Sammlungshistorie; wo die Reihenfolge das Spiel beeinflusst (Zufallsverbrauch,
/// Ergebnisreihenfolge), wird stabil sortiert.
/// </summary>
public class SaveGameOrderTests
{
    /// <summary>
    /// Gegenbeispiel aus dem Review: Eintrag entfernen, speichern, laden, Eintrag wieder einfügen. Das Original
    /// hat den alten Platz wiederverwendet, das geladene Wörterbuch nicht. Beide müssen trotzdem gleich spielen.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Schedules_RemoveAdd_PlaysIdenticallyBeforeAndAfterLoad(int removed)
    {
        var game = TestGames.Create();
        var key = game.Schedules.Keys.ElementAt(removed);
        var schedule = game.Schedules[key];
        game.Schedules.Remove(key);
        var twin = SaveGame.Load(SaveGame.Save(game)).Game!;
        game.Schedules.Add(key, schedule);
        twin.Schedules.Add(key, schedule);

        for (var day = 0; day < 3; day++)
        {
            Assert.True(game.PlayNextMatchDay());
            Assert.True(twin.PlayNextMatchDay());
        }

        Assert.Equal(game.Results, twin.Results);
        Assert.Equal(RngTwinProbeTests.Snapshot(game), RngTwinProbeTests.Snapshot(twin));
        // Nicht verglichen werden die Dateibytes: Sie halten die Wörterbuchreihenfolge fest, und die ist nach
        // Entnahmen verlaufsabhängig (siehe docs/formate/spielstand.md, „Keine Sammlungshistorie“). Dafür muss
        // jede Seite für sich bytegleich sichern, laden und wieder sichern.
        var saved = SaveGame.Save(twin);
        Assert.Equal(saved, SaveGame.Save(SaveGame.Load(saved).Game!));
    }

    /// <summary>Die Ligen werden unabhängig von der Wörterbuchhistorie immer in Ligareihenfolge gespielt.</summary>
    [Fact]
    public void Schedules_AreAlwaysPlayedInLeagueOrder()
    {
        var game = TestGames.Create();
        var first = game.Schedules[League.Bundesliga];
        var second = game.Schedules[League.ZweiteLiga];
        game.Schedules.Remove(League.Bundesliga);
        game.Schedules.Remove(League.ZweiteLiga);
        // Der zuletzt freigegebene Platz wird zuerst wiederverwendet: Aufzählung jetzt Zweite, Erste, Dritte Liga.
        game.Schedules.Add(League.Bundesliga, first);
        game.Schedules.Add(League.ZweiteLiga, second);

        Assert.True(game.PlayNextMatchDay());

        var order = game.Results.Select(r => r.League).Distinct().ToArray();
        Assert.Equal(order.OrderBy(l => l).ToArray(), order);
    }
}
