using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur kader-transfer, Teil 1 (Inventur P1 Nr. 7, A170): Kadergröße der Managervereine zu Spielbeginn.
/// [ORIGINAL] em.exe Datei 0x29A3F: Tabelle DGROUP 0x169 wird mit <c>[0x11A] · 4 + Gruppe</c> gelesen (0x11A = Start-Liga),
/// Bytes 2/10/13/0, 2/9/11/0, 2/8/10/0 (Datei 0x16F39). Der Spiel-Level (0x118) wirkt nicht.
/// </summary>
public class KaderLigaGroesseTests
{
    [Theory]
    [InlineData(League.Bundesliga, 2, 10, 13)]
    [InlineData(League.ZweiteLiga, 2, 9, 11)]
    [InlineData(League.Oberliga, 2, 8, 10)]
    public void ManagedSquadSize_ByStartLeague(League league, int g, int d, int f) =>
        Assert.Equal((g, d, f), SquadSetup.ManagedSquadSize(league));

    /// <summary>Jede Start-Liga mit jedem Level: Kader nach der Liga, unabhängig vom Level.</summary>
    [Theory]
    [InlineData(League.Bundesliga, 0, 2, 10, 13)]
    [InlineData(League.Bundesliga, 3, 2, 10, 13)]
    [InlineData(League.Bundesliga, 5, 2, 10, 13)]
    [InlineData(League.ZweiteLiga, 0, 2, 9, 11)]
    [InlineData(League.ZweiteLiga, 5, 2, 9, 11)]
    [InlineData(League.Oberliga, 0, 2, 8, 10)]
    [InlineData(League.Oberliga, 3, 2, 8, 10)]
    public void NewGame_SquadOfStartLeague_IndependentOfLevel(League league, int level, int g, int d, int f)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "T", Level = level, StartLeague = league });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, seed: 9);
        var club = game.Clubs[game.Managers[0].ClubIndex];

        Assert.Equal(league, club.League);
        AssertSquad(club, g, d, f);
    }

    /// <summary>
    /// Zwei Manager in verschiedenen Ligen: jeder Kader nach der Liga seines Vereins. [APPROX] Das Original kennt nur eine
    /// Start-Liga für alle Manager (FILE09); das Remake führt sie je Manager wie <see cref="SquadSetup.StartBase"/>.
    /// </summary>
    [Fact]
    public void TwoManagers_EachSquadOfOwnLeague()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", Level = 5, StartLeague = League.Bundesliga });
        setup.Managers.Add(new ManagerSetup { Name = "B", Level = 0, StartLeague = League.Oberliga });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, seed: 5);

        AssertSquad(game.Clubs[game.Managers[0].ClubIndex], 2, 10, 13);
        AssertSquad(game.Clubs[game.Managers[1].ClubIndex], 2, 8, 10);
    }

    /// <summary>
    /// Goldtest Original-Spielstände bei Spielbeginn: FILE01/FILE10 (Bundesliga, Level 3) und FILE09 (Bundesliga, Level 5, zwei
    /// Manager) haben 25 Kaderplätze (2/10/13), FILE12 (Oberliga, Level 0) 20 (2/8/10). Das Remake erzeugt dieselben Größen.
    /// Rolle aus dem Pool-Rollenwert (+0x18: Torhüter 0, Abwehr 25–41, Sturm 76–89; spielstand-original.md).
    /// </summary>
    [GameDataTheory]
    [InlineData("FILE01.HOK", 1, League.Bundesliga, 3, 2, 10, 13)]
    [InlineData("FILE09.HOK", 2, League.Bundesliga, 5, 2, 10, 13)]
    [InlineData("FILE10.HOK", 1, League.Bundesliga, 3, 2, 10, 13)]
    [InlineData("FILE12.HOK", 1, League.Oberliga, 0, 2, 8, 10)]
    public void OriginalSaves_SquadSizeByStartLeague(string file, int managers, League league, int level, int g, int d, int f)
    {
        var save = OriginalSaveGame.Read(File.ReadAllBytes(TransferOriginalTests.OriginalSave(file)));
        for (var m = 0; m < managers; m++)
        {
            var squad = save.Squad(m);
            Assert.Equal(g, squad.Count(s => s.RoleValue == 0));
            Assert.Equal(d, squad.Count(s => s.RoleValue is > 0 and < 50));
            Assert.Equal(f, squad.Count(s => s.RoleValue >= 50));
        }

        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "T", Level = level, StartLeague = league });
        var game = NewGame.Create(ClubDatabase.Load(TestGameData.Load()), setup, seed: 3);
        AssertSquad(game.Clubs[game.Managers[0].ClubIndex], g, d, f);
    }

    /// <summary>
    /// Kaufgrenze: [ORIGINAL] em.exe Datei 0x477CD → 0x42CB1 mit Grenze 0x1D: ab 29 Spielern „Schon 29 Mann / im Team.“
    /// (DGROUP 0x127D). FILE05 → FILE07: der Kölner EC (Bundesliga, 25 Spieler) kauft DAFFNER als 26. Spieler.
    /// Vorher galt 22 (Meldung der Aufstellung), ein Bundesliga-Manager hätte nie kaufen können.
    /// </summary>
    [Theory]
    [InlineData(25, TransferResult.Accepted)]
    [InlineData(28, TransferResult.Accepted)]
    [InlineData(29, TransferResult.SquadFull)]
    public void Buy_SquadLimit29(int squad, TransferResult expected)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "T", StartLeague = League.Bundesliga });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, seed: 9);
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        Assert.Equal(25, club.Players.Count);
        while (club.Players.Count < squad)
            club.Players.Add(new Player { Name = $"X{club.Players.Count}", Role = PlayerRole.Forward });
        manager.Money = 10_000_000;
        var listing = game.TransferMarket.Listings.First(l => l.Seller is null && !l.IsFreeAgent);

        // 135 %: im Original immer angenommen (über random(110, 130) %, 0x4991D), wie TransferTests.Buy_Refusals.
        Assert.Equal(expected, game.TransferMarket.Buy(game, manager, listing, listing.Price * 135 / 100, false));
        if (expected == TransferResult.SquadFull)
            Assert.Equal(["Schon 29 Mann", "im Team."], TransferMarket.Message(expected));
    }

    private static void AssertSquad(Club club, int g, int d, int f)
    {
        Assert.Equal(g, club.Players.Count(p => p.Role == PlayerRole.Goalkeeper));
        Assert.Equal(d, club.Players.Count(p => p.Role == PlayerRole.Defender));
        Assert.Equal(f, club.Players.Count(p => p.Role == PlayerRole.Forward));
    }
}
