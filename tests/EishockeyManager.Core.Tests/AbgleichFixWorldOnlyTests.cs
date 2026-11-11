using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using EishockeyManager.Tools;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur abgleich-fix (#732): Saisonwechsel im Modus „nur Weltkugel“. [ORIGINAL] Die Managerschleife von 0x26F20 beginnt bei
/// [0x386] · ([0xD0B9] = 1) (Datei 0x26F2F–0x26F4C) und springt bei 0x26FAD auf die Abfrage 0x279F1 – im Modus 1 läuft sie also
/// nie: kein Altern der Poolplätze (0x274A7), kein Herunterzählen der Laufzeit (0x27500), keine Jugend (0x2728C). Beleg FILE20 →
/// FILE21: Kader des Managersatzes 0 mit Alter und Laufzeit unverändert, alle 199 Poolplätze mit gleichem Alter +0x14.
/// </summary>
public class AbgleichFixWorldOnlyTests
{
    private static GameState Import(string file, int seed) =>
        HokCompare.Import(OriginalImportTests.OriginalSave(file + ".HOK"), ClubDatabase.Load(TestGameData.Load()), seed);

    [GameDataTheory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(77)]
    public void File20To21_WorldOnlySeasonChange_KeepsManagerSquadAndPoolAges(int seed)
    {
        var game = Import("FILE20", seed);
        Assert.True(game.WorldOnly);
        var target = Import("FILE21", seed);
        var club = game.Clubs[game.Managers[0].ClubIndex];
        var squad = club.Players.Select(p => (Player: p, p.Age, p.ContractYears)).ToList();
        var pool = game.TransferMarket.Pool.Select(s => (Slot: s, s.Player.Age)).ToList();

        Assert.Null(HokCompare.PlayTo(game, target));

        Assert.Equal(squad.Select(s => s.Player), club.Players);
        Assert.All(squad, s => Assert.Equal((s.Age, s.ContractYears), (s.Player.Age, s.Player.ContractYears)));
        Assert.All(pool, s => Assert.Equal(s.Age, s.Slot.Player.Age));
    }

    /// <summary>
    /// Mehrsaisonprobe (AGENTS.md §5): vier Saisonwechsel im Modus „nur Weltkugel“, passiv und aktiv (Freundschaftsspiele). Der
    /// Kader des Managervereins und die Poolplätze altern nicht, Laufzeiten bleiben, kein Spieler kommt oder geht.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WorldOnly_MultiSeason_ManagerSquadAndPoolDoNotAge(bool active)
    {
        var game = GameModeTests.Create(11, flag: false, globe: true, 5);
        Assert.Equal(GameMode.WorldOnly, game.Mode);
        var club = game.Clubs[game.Managers[0].ClubIndex];
        var squad = club.Players.Select(p => (Player: p, p.Age, p.ContractYears)).ToList();
        var pool = game.TransferMarket.Pool.Select(s => (Slot: s, s.Player.Age)).ToList();
        for (var change = 0; change < 4; change++)
        {
            game.Continue();
            game.AutoNominate();
            if (active)
                for (var opponent = NationalTeams.First; opponent <= NationalTeams.Last; opponent++)
                    if (opponent != NationalTeams.Germany)
                        game.PlayFriendly(opponent, home: opponent % 2 == 0);
            for (var guard = 0; game.Phase == SeasonPhase.Running; guard++)
            {
                Assert.True(guard < 100, "Saisonende nicht erreicht");
                game.Continue();
            }
            game.StartNextSeason();
            Assert.Equal(squad.Select(s => s.Player), club.Players);
            Assert.All(squad, s => Assert.Equal((s.Age, s.ContractYears), (s.Player.Age, s.Player.ContractYears)));
            Assert.All(pool, s => Assert.Equal(s.Age, s.Slot.Player.Age));
        }
    }
}
