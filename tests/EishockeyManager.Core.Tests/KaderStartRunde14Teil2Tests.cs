using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur <c>kader-start</c>, Runde 14, Teil 2: Spielstart aus der DOSBox-Aufnahme <c>runde-13</c>
/// (<c>docs/analyse/re-runde-14.md</c>): A056 (nur Weltkugel), U15-c (eine Saison, ein Manager) mit dem Europapokal-Filter
/// „Platz 0“ in U15-a.
/// </summary>
public class KaderStartRunde14Teil2Tests
{
    private static GameSetup Setup(GameDuration duration, params int?[] clubs)
    {
        var setup = new GameSetup { Duration = duration };
        foreach (var (club, i) in clubs.Select((c, i) => (c, i)))
            setup.AddManager(new ManagerSetup { Name = $"M{i}", Head = i, ClubIndex = club });
        return setup;
    }

    /// <summary>Stärke der synthetischen Datenbank am Platz <paramref name="index"/> (<see cref="TestGames.CreateDatabase"/>).</summary>
    private static int DatabaseStrength(int index) => 100 - index % 12 * 5;

    /// <summary>A056, Datei 0x28ACF: bei „nur Weltkugel“ wirkt jede Managerzeile wie Zeile 0, sonst die Zeile selbst.</summary>
    [Fact]
    public void WorldOnly_EveryManagerRowMapsToRowZero()
    {
        var setup = new GameSetup();
        Assert.Equal(new[] { 0, 1, 2, 3 }, Enumerable.Range(0, 4).Select(setup.ManagerRowFor));
        Assert.True(setup.ToggleClubCompetitions());
        Assert.Equal(GameMode.WorldOnly, setup.Mode);
        Assert.All(Enumerable.Range(0, 4), row => Assert.Equal(0, setup.ManagerRowFor(row)));
    }

    /// <summary>
    /// U15-c, FILE29: ein Manager, eine Saison → der Managerverein steht auf Platz 0 der Bundesliga, mit den Teamwerten von
    /// Platz 0 (Datei 0x29CFA–0x29D9C). Vorher blieb er auf dem Zufallsplatz aus U15-a.
    /// </summary>
    [Theory]
    [InlineData(1, 5)]
    [InlineData(4, 17)]
    [InlineData(9, 40)]
    [InlineData(13, null)]
    public void OneSeason_SingleManager_ClubOnPlaceZero(int seed, int? club)
    {
        var placeZero = NewGame.StartOrder(seed)[0];
        var game = NewGame.Create(TestGames.CreateDatabase(), Setup(GameDuration.EineSaison, club), seed);

        var own = game.Managers[0].ClubIndex;
        Assert.Equal(own, game.LeagueSlots(League.Bundesliga)[0]);
        Assert.Equal(League.Bundesliga, game.Clubs[own].League);
        Assert.Equal(DatabaseStrength(placeZero), game.Clubs[own].Strength);
        Assert.All(Enum.GetValues<League>(), l => Assert.Equal(12, game.LeagueClubs(l).Count()));
    }

    /// <summary>Kein Sonderfall bei zwei Managern oder mehr als einer Saison: Platz 0 bleibt beim Verein der Startmischung.</summary>
    [Theory]
    [InlineData(GameDuration.DreiSaisons, 1)]
    [InlineData(GameDuration.EineSaison, 2)]
    public void NoSpecialCase_PlaceZeroUntouched(GameDuration duration, int managers)
    {
        for (var seed = 0; seed < 40; seed++)
        {
            var placeZero = NewGame.StartOrder(seed)[0];
            int?[] clubs = managers == 1 ? [placeZero == 5 ? 6 : 5] : [placeZero == 5 ? 6 : 5, placeZero == 20 ? 21 : 20];
            var game = NewGame.Create(TestGames.CreateDatabase(), Setup(duration, clubs), seed);
            Assert.Equal(placeZero, game.LeagueSlots(League.Bundesliga)[0]);
            Assert.Equal(DatabaseStrength(placeZero), game.Clubs[placeZero].Strength);
        }
    }

    /// <summary>
    /// U15-a mit dem Filter aus U15-c: Der Zufallsplatz der Bundesliga ist nie Platz 0 (Datei 0x29CCC, leere Tabelle DGROUP
    /// 0x5CFB beim Spielstart), wohl aber der Deutsche Meister (Verein 2). Vorher war Verein 2 ausgeschlossen.
    /// </summary>
    [Fact]
    public void StartLeagueDraw_NeverPlaceZero_ChampionPossible()
    {
        var championDrawn = false;
        for (var seed = 0; seed < 300; seed++)
        {
            var order = NewGame.StartOrder(seed);
            var own = order[0] == 30 ? 31 : 30; // Oberligist, Start-Liga Bundesliga → immer getauscht
            var game = NewGame.Create(TestGames.CreateDatabase(), Setup(GameDuration.Unbegrenzt, own), seed);
            var slots = game.LeagueSlots(League.Bundesliga);
            Assert.NotEqual(own, slots[0]);
            Assert.Equal(order[0], slots[0]);
            if (game.Clubs[EuropeanCup.InitialChampion].League == League.Oberliga)
                championDrawn = true;
        }
        Assert.True(championDrawn);
    }
}
