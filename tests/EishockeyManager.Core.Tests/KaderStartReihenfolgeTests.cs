namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur <c>kader-start</c>, Runde 13, Teil 3, R-K5 (#549): Die Vereinssätze sind beim Öffnen der Spielvorbereitung schon
/// gemischt (Ligamischung Datei 0x43A17 → 0x35EDD vor der Spielvorbereitung Datei 0x43A61); die Vereinsauswahl zeigt diese
/// Folge, der Spielstart übernimmt sie als Satzfolge (<see cref="GameState.LeagueSlots"/>), danach nur der Managertausch.
/// Belege <c>oberflaeche_0128/_0133/_0136</c>, FILE01/10/27/28 (<c>docs/analyse/re-runde-13.md</c>, R-K5). Vorher zeigte die
/// Auswahl die Datenbankfolge, und die Satzfolge wurde erst nach der Vereinsvergabe gemischt.
/// </summary>
public class KaderStartReihenfolgeTests
{
    /// <summary>Seiten 0–2 sind je die 12 Vereine ihrer Liga, gemischt; Seite 3 (ohne Liga) bleibt in Datenbankfolge (_0136).</summary>
    [Theory]
    [InlineData(1)]
    [InlineData(1993)]
    [InlineData(123456)]
    public void StartOrder_PagesHoldTheirLeague_PageThreeUnmixed(int seed)
    {
        var places = NewGame.StartOrder(seed);

        Assert.Equal(NewGame.SelectableClubs, places.Length);
        for (var page = 0; page < 3; page++)
        {
            var slice = places.Skip(page * 12).Take(12).ToArray();
            Assert.Equal(Enumerable.Range(page * 12, 12), slice.Order());
            Assert.NotEqual(Enumerable.Range(page * 12, 12), slice); // gemischt (55 Tausche)
        }
        Assert.Equal(Enumerable.Range(36, 12), places.Skip(36));
        Assert.Equal(places, NewGame.StartOrder(seed)); // dieselbe Saat, dieselbe Folge (kein Zufallszug beim Öffnen)
    }

    /// <summary>Ohne festen Verein: Die Satzfolge des neuen Spiels ist genau die Startmischung derselben Saat.</summary>
    [Theory]
    [InlineData(3)]
    [InlineData(11)]
    public void NewGame_LeagueSlotsAreTheStartOrder(int seed)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Z", StartLeague = League.Oberliga, StartStrength = StartStrength.Mittelfeld });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, seed);
        var places = NewGame.StartOrder(seed);

        foreach (var league in Enum.GetValues<League>())
            Assert.Equal(places.Skip((int)league * 12).Take(12), game.LeagueSlots(league));
    }

    /// <summary>
    /// U15-a mit Satzfolge: Köln (5) fest, Start-Liga 2. Liga. Der Zweitligist übernimmt Kölns Satzplatz in der Bundesliga,
    /// Köln den Satzplatz des Zweitligisten (0x36169 tauscht die Plätze); alle übrigen Plätze bleiben wie in der Startmischung.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(11)]
    public void StartSwap_ExchangesSlots_OthersKeepStartOrder(int seed)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Koeln", ClubIndex = 5 });
        setup.StartLeague = League.ZweiteLiga;
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, seed);
        var places = NewGame.StartOrder(seed);
        var partner = game.Clubs.Single(c => c.Index is >= 12 and < 24 && c.League == League.Bundesliga).Index;

        var expected = places.Take(36).Select(c => c == 5 ? partner : c == partner ? 5 : c).ToArray();
        foreach (var league in Enum.GetValues<League>())
            Assert.Equal(expected.Skip((int)league * 12).Take(12), game.LeagueSlots(league));
    }
}
