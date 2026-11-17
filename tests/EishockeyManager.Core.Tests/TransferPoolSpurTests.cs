using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Runde 22, Spur transfer-pool: A391 (Poolplatz eines nach Vertragsende verlorenen Spielers), #897 Poolplatz 179, A713, A731,
/// #897 <c>ChangeList</c>, A864. Fundstellen je Test.
/// </summary>
public class TransferPoolSpurTests
{
    private static GameState Create(int seed = 1)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "M5", ClubIndex = 5, StartLeague = League.Bundesliga });
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    private static void PlayToSeasonOver(GameState game)
    {
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 20_000, "Saisonende nicht erreicht");
            if (game.NationalTurn && game.NationalCoach!.Squad.Count == 0)
                game.AutoNominate();
            game.Continue();
        }
    }

    /// <summary>
    /// A391: Der Platz eines nach Vertragsende verlorenen Spielers altert in jedem späteren Saisonwechsel weiter.
    /// [ORIGINAL] Datei 0x274A7 (<c>inc</c> Pool +0x14) liegt in der Schleife über alle 200 Poolplätze (0x2744B–0x27232 mit
    /// Zähler [bp−2] bis 0xC8) und überspringt einen Platz nur bei DGROUP 0xA70E ≠ 0 (Datei 0x2748E–0x27494); Kaderplatz und
    /// Besitzer spielen dort keine Rolle. Beleg FILE44 → FILE46: KJELLBERG (Pool 196, +0x1A = 0) Alter 32 → 33 im Saisonwechsel
    /// des Vertragsendes. Im Saisonwechsel des Vertragsendes altert ihn der Kaderlauf, danach nur noch der Poolplatz.
    /// Rot vorher: Alter blieb nach dem Vertragsende stehen.
    /// </summary>
    [Fact]
    public void A391_LostPlayer_KeepsAgingEverySeason()
    {
        var game = Create();
        var club = game.Clubs[game.Managers[0].ClubIndex];
        foreach (var p in club.Players)
            (p.ContractYears, p.Age) = (3, 25);
        var lost = club.Players[3];
        var index = game.TransferMarket.Pool.First(s => ReferenceEquals(s.Player, lost)).Index;
        lost.ContractYears = 1;

        PlayToSeasonOver(game);
        game.StartNextSeason();
        var slot = game.TransferMarket.Pool[index - 1];
        Assert.Same(lost, slot.Player);
        Assert.Equal(PoolOwner.Taken, slot.Owner);
        Assert.Equal(26, lost.Age); // einmal im Saisonwechsel des Vertragsendes (Kaderlauf)

        PlayToSeasonOver(game);
        game.StartNextSeason();
        Assert.Same(lost, game.TransferMarket.Pool[index - 1].Player);
        Assert.Equal(27, lost.Age); // danach nur der Poolplatz (A391)

        PlayToSeasonOver(game);
        game.StartNextSeason();
        Assert.Equal(28, lost.Age);
    }

    /// <summary>
    /// #897 Poolplatz 179: kein Fehler, das Original trifft ihn auch. [ORIGINAL] Die Soll-Rechnung 0x05521 (aus 0x057E3 und 0x07C7E)
    /// zieht <c>random(1, 0xB3)</c> (Datei 0x0512B–0x05133: <c>push 0xB3; push 1</c>, Aufruf 0x99F:0x847) – 1 … 179 einschließlich –
    /// und gibt einem gezogenen Inlandsspieler über 0x059B8 einen neuen Verein; nur die Schleife von 0x057E3 selbst läuft bis 0xB2
    /// (Datei 0x057E3 + 0xEC: <c>cmp [bp−1], 0xB3; jnc</c>). Gegenproben: FILE05 Platz 179 beim Nationalteam 57, FILE41/FILE46 Platz
    /// 179 bei Ligavereinen 2/6 (<c>docs/transfer.md</c>, „Soll je Liga“). Das Remake bildet beides ab; Plätze ab 180 bleiben immer.
    /// </summary>
    [Fact]
    public void PoolSlot179_IsReachedByQuotaDraw_Not180Up()
    {
        var hit179 = false;
        for (var seed = 1; seed <= 300 && !hit179; seed++)
        {
            var game = Create(seed);
            var pool = game.TransferMarket.Pool;
            var before = pool.Select(s => s.Club).ToArray();
            game.TransferMarket.PoolQuotas(game, new Random(seed));
            hit179 |= pool[178].Club != before[178];
            for (var i = 179; i < pool.Count; i++)
                Assert.Equal(before[i], pool[i].Club); // Plätze 180 … 199
        }
        Assert.True(hit179, "Platz 179 wird von der Soll-Rechnung getroffen (random(1, 0xB3))");
    }
}
