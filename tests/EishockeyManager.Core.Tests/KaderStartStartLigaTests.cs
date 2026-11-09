using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur <c>kader-start</c>, Runde 13, Teil 2, U15-a (D7 = D, L13 = A): Der fest gewählte Verein kommt beim Spielstart in die
/// Start-Liga, bei Bedarf per Platztausch; die Teamwerte (Remake: <see cref="Club.Strength"/>) bleiben am Platz.
/// [ORIGINAL] em.exe Datei 0x29C3B–0x29CF2; Belege FILE27/FILE28 (Köln fest, Start-Liga 2. Liga), FILE15 (Bayreuth bleibt),
/// <c>docs/analyse/re-runde-13.md</c>, Abschnitt U15-a. Vorher blieb ein fester Verein in seiner Liga, ein Verein ohne Liga
/// rückte in die Oberliga (<c>[APPROX]</c> A112).
/// </summary>
public class KaderStartStartLigaTests
{
    private static GameState Create(int seed, League league, params int[] clubs)
    {
        var setup = new GameSetup();
        foreach (var (club, i) in clubs.Select((c, i) => (c, i)))
            setup.Managers.Add(new ManagerSetup { Name = $"M{i}", Head = i, ClubIndex = club });
        setup.StartLeague = league;
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    /// <summary>Stärke der synthetischen Datenbank am Platz <paramref name="index"/> (<see cref="TestGames.CreateDatabase"/>).</summary>
    private static int DatabaseStrength(int index) => 100 - index % 12 * 5;

    private static void AssertTwelvePerLeague(GameState game)
    {
        foreach (var league in Enum.GetValues<League>())
            Assert.Equal(12, game.LeagueClubs(league).Count());
    }

    /// <summary>
    /// FILE27/FILE28: Köln (Index 5, Bundesliga) fest, Start-Liga 2. Liga → Köln spielt in der 2. Liga mit den Teamwerten des
    /// Zweitligaplatzes; genau ein früherer Zweitligist steht in der Bundesliga, mit Kölns alten Teamwerten. Vorher blieb Köln in
    /// der Bundesliga.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(11)]
    public void FixedBundesligaClub_SecondLeague_SwapsPlacesStrengthStays(int seed)
    {
        var game = Create(seed, League.ZweiteLiga, 5);

        Assert.Equal(League.ZweiteLiga, game.Clubs[5].League);
        var promoted = Assert.Single(game.Clubs, c => c.Index is >= 12 and < 24 && c.League == League.Bundesliga);
        Assert.Equal(DatabaseStrength(5), promoted.Strength);
        Assert.Equal(DatabaseStrength(promoted.Index), game.Clubs[5].Strength);
        Assert.All(game.Clubs.Where(c => c.Index < 36 && c.Index != 5 && c != promoted),
            c => Assert.Equal(NewGame.DatabaseLeague(c.Index), c.League));
        AssertTwelvePerLeague(game);
    }

    /// <summary>
    /// FILE15 (Bayreuth bleibt in der 2. Liga): Steht der Verein schon in der Start-Liga (≠ Bundesliga, Platz ≤ 35), bleibt
    /// alles am Platz (Datei 0x29C3B–0x29C75), auch die Stärke.
    /// </summary>
    [Theory]
    [InlineData(17, League.ZweiteLiga)]
    [InlineData(30, League.Oberliga)]
    public void FixedClubAlreadyInStartLeague_Kept(int club, League league)
    {
        var game = Create(3, league, club);

        Assert.All(game.Clubs.Where(c => c.Index < 48), c =>
        {
            Assert.Equal(NewGame.DatabaseLeague(c.Index), c.League);
            Assert.Equal(DatabaseStrength(c.Index), c.Strength);
        });
    }

    /// <summary>
    /// Verein ohne Liga (36–47) landet direkt in der Start-Liga, auch in der 2. Liga oder Bundesliga; der Verdrängte übernimmt
    /// seinen Platz ohne Liga. Vorher rückte er immer in die Oberliga.
    /// </summary>
    [Theory]
    [InlineData(League.Bundesliga)]
    [InlineData(League.ZweiteLiga)]
    [InlineData(League.Oberliga)]
    public void NoLeagueClub_LandsInStartLeague_DisplacedLosesLeague(League league)
    {
        var game = Create(7, league, 42);

        Assert.Equal(league, game.Clubs[42].League);
        var displaced = Assert.Single(game.Clubs, c => c.Index < 36 && c.League is null);
        Assert.Equal(league, NewGame.DatabaseLeague(displaced.Index));
        Assert.Equal(DatabaseStrength(displaced.Index), game.Clubs[42].Strength);
        AssertTwelvePerLeague(game);
    }

    /// <summary>
    /// Start-Liga Bundesliga: immer ein Zufallsplatz der Bundesliga, auch für einen Bundesligisten (Datei 0x29C3B: Behalten nur
    /// bei L ≠ 0). Zieht er den eigenen Platz, bleibt alles; sonst tauschen beide die Stärke. Der Europapokal-Filter
    /// (Zeile 1, Datei 0x29CCC) trifft beim Spielstart nur Platz 0 (leere Tabelle, U15-c) – der Verein dort wandert deshalb immer.
    /// </summary>
    [Fact]
    public void Bundesliga_AlwaysDraws_NeverTheEuropeanCupPlace()
    {
        var kept = 0;
        for (var seed = 0; seed < 40; seed++)
        {
            // U15-c (re-runde-14.md): Beim Spielstart ist die Europapokal-Tabelle leer, der Filter trifft Platz 0 der
            // Startmischung (vorher: Verein 2, der Deutsche Meister).
            var placeZero = NewGame.StartOrder(seed)[0];
            var own = placeZero == 5 ? 6 : 5;
            var game = Create(seed, League.Bundesliga, own);
            Assert.All(game.Clubs.Where(c => c.Index < 36), c => Assert.Equal(NewGame.DatabaseLeague(c.Index), c.League));
            Assert.Equal(DatabaseStrength(placeZero), game.Clubs[placeZero].Strength);
            if (game.Clubs[own].Strength == DatabaseStrength(own))
                kept++;

            var onPlaceZero = Create(seed, League.Bundesliga, placeZero);
            Assert.NotEqual(DatabaseStrength(placeZero), onPlaceZero.Clubs[placeZero].Strength);
        }
        Assert.InRange(kept, 1, 39); // eigener Platz möglich, aber nicht immer
    }

    /// <summary>
    /// Datei 0x29C9B–0x29CC3: Der Zufallsplatz ist nie der Verein eines früheren Managers. Zwei Zweitligisten, Start-Liga
    /// Bundesliga: beide spielen in der Bundesliga, zwei verschiedene Bundesligisten sind in die 2. Liga gewechselt.
    /// </summary>
    [Fact]
    public void TwoManagers_LaterNeverDrawsEarlierManagersClub()
    {
        for (var seed = 0; seed < 30; seed++)
        {
            var game = Create(seed, League.Bundesliga, 13, 14);

            Assert.Equal(League.Bundesliga, game.Clubs[13].League);
            Assert.Equal(League.Bundesliga, game.Clubs[14].League);
            Assert.Equal(2, game.Clubs.Count(c => c.Index < 12 && c.League == League.ZweiteLiga));
            Assert.Equal([13, 14], game.Managers.Select(m => m.ClubIndex));
            AssertTwelvePerLeague(game);
        }
    }

    /// <summary>
    /// Originaldaten wie FILE27: Kölner EC (Datenbank 5) fest, Start-Liga 2. Liga → Köln in der 2. Liga mit der Stärke eines
    /// Zweitligaplatzes, ein Zweitligist in der Bundesliga mit Kölns Datenbankstärke.
    /// </summary>
    [GameDataFact]
    public void OriginalDatabase_KoelnSecondLeague_LikeFile27()
    {
        var database = ClubDatabase.Load(TestGameData.Load());
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Koeln", Head = 0, ClubIndex = 5 });
        setup.StartLeague = League.ZweiteLiga;

        var game = NewGame.Create(database, setup, seed: 1993);

        Assert.Contains("KÖLN", database.Clubs[5].Name.ToUpperInvariant());
        Assert.Equal(League.ZweiteLiga, game.Clubs[5].League);
        var promoted = Assert.Single(game.Clubs, c => c.Index is >= 12 and < 24 && c.League == League.Bundesliga);
        Assert.Equal(database.Clubs[5].Strength, promoted.Strength);
        Assert.Equal(database.Clubs[promoted.Index].Strength, game.Clubs[5].Strength);
    }

    /// <summary>
    /// Mehrsaisonprobe (D7, AGENTS.md §5): Köln fest mit Start-Liga 2. Liga, vier Saisonwechsel, passiver und aktiver Manager,
    /// mit den Prüfungen der Spur kader (kein Spieler verloren oder doppelt, Verträge angekündigt); der getauschte Verein spielt
    /// in jeder Saison in genau einer Liga.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultiSeason_FourChanges_AfterStartSwap(bool active)
    {
        var game = Create(3, League.ZweiteLiga, 5);
        var promoted = game.Clubs.Single(c => c.Index is >= 12 and < 24 && c.League == League.Bundesliga);

        SquadLifecycleTests.RunProbe(game, active, changes: 4);

        Assert.Equal(1997, game.Season);
        Assert.False(game.Managers[0].Dismissed);
        Assert.Equal(5, game.Managers[0].ClubIndex);
        Assert.All(game.History, r => Assert.Single(r.FinalTables.Values, t => t.Contains(promoted.Index)));
        AssertTwelvePerLeague(game);
    }
}
