namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur tagesablauf (Runde 15, Teil 2, #699 Punkt 1): Listenwechsel-Wurf je Managerzug im Hot-Seat.
/// </summary>
/// <remarks>
/// [ORIGINAL] Selbst nachgelesen (<c>tools/reverse-engineering/disasm.py gamedata/em.exe 0x54D38 0x54EF8</c>): je Manager
/// (DGROUP 0xA70E = 0 … [0x386] − 1) der Wurf Datei 0x54E65–0x54E7C (random(0, Zahl + 2) = 0 → 0x494EF), dann dessen Menü
/// (Datei 0x54EB2), Schleife Datei 0x54E3F–0x54EF4.
/// </remarks>
public sealed class TagesablaufRunde15Tests
{
    private static GameState TwoManagers(int seed)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", Head = 0, ClubIndex = 2 });
        setup.Managers.Add(new ManagerSetup { Name = "B", Head = 1, ClubIndex = 5 });
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    private static GameState Reload(GameState game) => SaveGame.Load(SaveGame.Save(game)).Game!;

    /// <summary>
    /// Teil 2: Im Hot-Seat würfelt das Original den Listenwechsel von Manager 1 erst nach dem Menü von Manager 0 (Datei
    /// 0x54E3F–0x54EF4: Wurf 0x54E65–0x54E7C, dann Menü 0x54EB2, dann der nächste Manager). Das Weitergeben des Zugs zieht also
    /// genau einen Wurf aus der Hauptquelle. Prüfung: Stand im Menü von Manager 0 sichern, Zug weitergeben; der geladene Stand mit
    /// einem Wurf von Hand muss dieselbe Hauptquelle und dieselbe Transferliste haben. Vorher rot: alle Würfe vor dem ersten Menü,
    /// das Weitergeben zieht nichts.
    /// </summary>
    [Theory]
    [InlineData(3)]
    [InlineData(11)]
    [InlineData(29)]
    public void HotSeat_ListChangeRollOfSecondManager_AfterFirstMenu(int seed)
    {
        var game = TwoManagers(seed);
        for (var guard = 0; guard < 40 && !(game.IsListChangeDay(game.CurrentDay) && game.CurrentManager == 0); guard++)
            game.Continue();
        Assert.True(game.IsListChangeDay(game.CurrentDay));
        Assert.Equal(0, game.CurrentManager);
        var probe = Reload(game);

        Assert.Empty(game.Continue()); // Zug an Manager 1
        Assert.Equal(1, game.CurrentManager);
        probe.TransferMarket.ListChangeTurn(probe);

        Assert.Equal(probe.Random.Next(), game.Random.Next());
        Assert.Equal(probe.TransferMarket.Listings.Select(l => l.Player.Id), game.TransferMarket.Listings.Select(l => l.Player.Id));
    }

    /// <summary>
    /// Teil 2, Rundreise: Die Verteilung der Würfe auf die Managerzüge braucht kein neues Spielstandsfeld (sie folgt aus
    /// <see cref="GameState.CurrentManager"/> und dem Tag). Ein Lauf, der vor jedem Zug sichert und lädt, gleicht dem Lauf ohne
    /// Unterbrechung (Tag, Manager am Zug, Hauptquelle).
    /// </summary>
    [Fact]
    public void HotSeat_DayAfterPass_SameAsUninterruptedRun()
    {
        var a = TwoManagers(7);
        var b = Reload(a);
        for (var i = 0; i < 6; i++)
        {
            a.Continue();
            b = Reload(b);
            b.Continue();
        }
        Assert.Equal(a.CurrentDay, b.CurrentDay);
        Assert.Equal(a.CurrentManager, b.CurrentManager);
        Assert.Equal(a.Random.Next(), b.Random.Next());
    }
}
