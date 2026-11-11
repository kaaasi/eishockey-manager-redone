using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur abgleich-fix, Teil 3 (#736): Namen der Startkader. [ORIGINAL] Datei 0x5B0CC–0x5B2BE: Je Manager und Art füllt das Original
/// die Kaderplätze (Anzahl DGROUP 0x169) der Reihe nach mit den Datenbanknamen seines Vereins (0x5AA34, Datenbankplatz läuft über
/// alle Arten weiter); ist der Datenbankplatz leer, zieht 0x5AAA0 (66C3:0280) einen Namen dieser Art aus den Vereinen 0–11 ohne
/// Managerverein, der im Pool noch nicht vorkommt. FILE01 (KÖLNER EC, 9 Verteidiger und 12 Stürmer in der Datenbank, 10/13 Plätze):
/// Platz 12 UVIRA (Datenbank EHC FREIBURG), Platz 25 LALA (MANNHEIMER ERC); jeder Poolname einmal (199 verschiedene).
/// Vorher rot: Die Namensliste war die Menge der vergebenen Namen selbst, freie Plätze hießen „SPIELER n“ (EXPORT01: 1173/1174).
/// </summary>
public class AbgleichFixNamesTests
{
    private static GameState NewKoeln(int seed, int? club = 5)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Probe", ClubIndex = club });
        return NewGame.Create(ClubDatabase.Load(TestGameData.Load()), setup, seed);
    }

    [GameDataTheory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(77)]
    public void NewGame_ManagerSquad_HasDatabaseNamesOnly(int seed)
    {
        var database = ClubDatabase.Load(TestGameData.Load());
        var game = NewKoeln(seed);
        var club = game.Clubs[game.Managers[0].ClubIndex];
        Assert.Equal(25, club.Players.Count);
        Assert.DoesNotContain(club.Players, p => p.Name.StartsWith("SPIELER", StringComparison.Ordinal));

        // Die beiden ergänzten Plätze (10. Verteidiger, 13. Stürmer) tragen Datenbanknamen ihrer Art aus den Vereinen 0–35.
        Assert.Equal(PlayerRole.Defender, club.Players[11].Role);
        Assert.Equal("BRANDL", club.Players[12].Name); // wie FILE01: Datenbankplatz 11 leer, BRANDL bleibt Stürmer
        Assert.Equal(PlayerRole.Forward, club.Players[12].Role);
        var own = database.Clubs[5].Players.Select(p => p.Name).ToHashSet();
        var extras = club.Players.Where(p => !own.Contains(p.Name)).ToList();
        Assert.Equal([PlayerRole.Defender, PlayerRole.Forward], extras.Select(p => p.Role).Order());
        foreach (var p in extras)
            Assert.Contains(database.Clubs.Take(36), c => c.Index != 5 && c.Players.Any(d => d.Name == p.Name && d.Role == p.Role));

        // Jeder Name nur einmal im Spiel (Kader und Pool).
        var names = game.Clubs.SelectMany(c => c.Players).Concat(game.TransferMarket.PoolPlayers).Select(p => p.Name).ToList();
        Assert.Equal(names.Count, names.Distinct().Count());
    }

    /// <summary>Managerkader anderer Vereine und Ligen (2/10/13 bzw. Oberliga 2/8/10): kein Platzhaltername, Namen eindeutig.</summary>
    [GameDataTheory]
    [InlineData(1, 42)]
    [InlineData(2, 30)]
    [InlineData(3, null)]
    [InlineData(77, 0)]
    public void NewGame_ManagerSquad_NoPlaceholderNames(int seed, int? clubIndex)
    {
        var game = NewKoeln(seed, clubIndex);
        var club = game.Clubs[game.Managers[0].ClubIndex];
        Assert.DoesNotContain(club.Players, p => p.Name.StartsWith("SPIELER", StringComparison.Ordinal));
        var names = game.Clubs.SelectMany(c => c.Players).Concat(game.TransferMarket.PoolPlayers).Select(p => p.Name).ToList();
        Assert.Equal(names.Count, names.Distinct().Count());
    }
}
