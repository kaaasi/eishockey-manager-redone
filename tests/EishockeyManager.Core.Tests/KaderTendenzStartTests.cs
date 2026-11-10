using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur kader, Teil 2 (A134, Folge R688N-02): Tendenz +0x0E und Steuerbyte +0x14 der Managerkader beim Spielstart. [ORIGINAL] Der
/// Spielstart legt jeden Kaderplatz über 0x42CB1 an (Datei 0x29996), das nach der Energie 0x4907A ruft (Datei 0x42E6B); erst danach
/// stehen Wert(Preis) und Wert(Gehalt) (Datei 0x29EB4), die die Tendenz lesen. Vorher hatte im Remake kein Startkader Wert oder
/// Steuerbyte, die Tendenz je Takt (0x07B64) wirkte in einem neuen Spiel auf keinen Managerspieler.
/// </summary>
public class KaderTendenzStartTests
{
    private static void AssertRolled(Player p)
    {
        Assert.InRange(p.TrendScore, 35, 65);
        Assert.InRange(p.TrendControl & 0x0F, 1, 13);
        Assert.InRange((p.TrendControl >> 4) & 7, 1, 3);
        Assert.Equal(Player.TrendOf(p.TrendScore), p.Trend);
    }

    private static GameState OriginalGame(int seed)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Joerg", ClubIndex = 12 });
        return NewGame.Create(ClubDatabase.Load(TestGameData.Load()), setup, seed);
    }

    /// <summary>Regressionstest (vorher rot: 0 von 25 Plätzen mit Wert und Steuerbyte, R688N-02).</summary>
    [Fact]
    public void NewGame_ManagerSquad_HasTrendAndControl_SalaryReadsIt()
    {
        var game = TestGames.Create(3);
        var manager = game.Managers[0];
        var club = game.Clubs[manager.ClubIndex];
        Assert.NotEmpty(club.Players);
        Assert.All(club.Players, AssertRolled);
        // Start-Gehalt = Wert(Gehalt) mit der gewürfelten Tendenz (Datei 0x29EB4 nach 0x29996).
        Assert.All(club.Players, p => Assert.Equal(Contracts.ValueSalary(game, manager, p), p.MonthlySalary));
        // Beide Bits der Richtung kommen vor (random(0, 1) << 7).
        Assert.Contains(club.Players, p => p.TrendControl >= 0x80);
        Assert.Contains(club.Players, p => p.TrendControl < 0x80);
        // Computervereine haben im Original keinen Kaderplatz (Datensatz 0x21 Byte): kein Wert.
        Assert.All(game.Clubs.Where(c => c != club).SelectMany(c => c.Players), p => Assert.Equal(0, p.TrendControl));
    }

    /// <summary>
    /// Gegenprobe mit den Originalständen, sofort nach dem Spielstart gesichert (FILE01, FILE27, FILE28, FILE29): alle Plätze des
    /// Managerkaders mit Wert 35–65, Zähler 1–13 und Schritt 1–3, wie ein neues Spiel des Remakes mit der Originaldatenbank.
    /// </summary>
    [GameDataTheory]
    [InlineData("FILE01.HOK")]
    [InlineData("FILE27.HOK")]
    [InlineData("FILE28.HOK")]
    [InlineData("FILE29.HOK")]
    public void OriginalStartSaves_AndNewGame_SameRanges(string file)
    {
        var save = OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave(file)));
        var imported = GameState.ImportOriginal(save, ClubDatabase.Load(TestGameData.Load()), 1);
        Assert.Equal(0, imported.CurrentDay);
        Assert.All(imported.Clubs[imported.Managers[0].ClubIndex].Players, AssertRolled);

        var game = OriginalGame(file.Length);
        Assert.All(game.Clubs[12].Players, AssertRolled);
    }

    /// <summary>[ORIGINAL] Talentsuche und Jugend am Saisonende legen den Platz über 0x42CB1 an (0x06D7F, Datei 0x06EB9).</summary>
    [Fact]
    public void ScoutedTalent_HasTrendAndControl()
    {
        var game = TestGames.Create(3);
        var p = game.JoinScoutedTalent(game.Managers[0], 60);
        Assert.NotNull(p);
        AssertRolled(p);
    }

    /// <summary>
    /// Mehrsaisonprobe (vier Saisonwechsel, passiv und aktiv): Die Tendenz je Takt (0x07B64–0x07BFC) läuft im Managerkader eines neuen
    /// Spiels; jeder Platz hat danach Wert 30–68 und ein Steuerbyte, mindestens ein Wert hat sich bewegt.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FourSeasonChanges_TrendMovesInManagerSquad(bool active)
    {
        var game = TestGames.Create(3);
        var manager = game.Managers[0];
        var club = game.Clubs[manager.ClubIndex];
        var start = club.Players.ToDictionary(p => p, p => p.TrendScore);
        var moved = false;
        for (var change = 0; change < 4; change++)
        {
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 1000, "Saisonende nicht erreicht");
                manager.Money = Math.Max(manager.Money, 10_000_000);
                if (active)
                    TestSeason.KeepSquad(game, manager, extendExpired: true);
                game.Continue();
                moved |= club.Players.Any(p => start.TryGetValue(p, out var s) && s != p.TrendScore);
            }
            game.StartNextSeason();
            Assert.All(club.Players, p =>
            {
                Assert.InRange(p.TrendScore, 30, 68);
                Assert.NotEqual(0, p.TrendControl & 0x0F);
            });
        }
        Assert.Equal(1997, game.Season);
        Assert.True(moved, "Tendenz im Managerkader unverändert");
    }
}
