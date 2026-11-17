using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur kaderplatz-werte (Runde 22), Fixlauf zu Review #927: Poolwerte (+0x15/+0x16) neben dem Kaderplatz (+0x10/+0x11).
/// R923-01: Das Training schreibt nur den Kaderplatz (<see cref="Player.SetSlotStrengths"/>). R923-02: Mehrsaisonprobe der Poolwerte.
/// </summary>
public class KaderplatzWerteFixTests
{
    private static Dictionary<Player, (int Condition, int Technique)> PoolOf(Club club)
    {
        var map = new Dictionary<Player, (int, int)>(ReferenceEqualityComparer.Instance);
        foreach (var p in club.Players)
            map[p] = p.PoolStrengths;
        return map;
    }

    /// <summary>
    /// R923-01 (Probe B des Reviews): Vier Spieltage mit Training ändern die Kaderplätze, nicht die Poolwerte – auch nicht bei
    /// Spielern, deren Poolwert vorher gleich dem Kaderplatz war (null). [ORIGINAL] FILE50→FILE63, FILE02→FILE03, FILE71→FILE74:
    /// Kader +0x10/+0x11 geändert, Pool +0x15/+0x16 nie. Vorher wanderte der Poolwert mit (SPIELER 15: Pool 80/80 → 80/79).
    /// </summary>
    [Fact]
    public void Training_KeepsPoolValues_AlsoWhenEqualToSlot()
    {
        var game = TestGames.Create(4);
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        var before = club.Players.Select(p => (p, Slot: (p.Condition, p.Technique), Pool: p.PoolStrengths,
            Equal: p.PoolCondition is null || p.PoolTechnique is null)).ToList();

        for (var i = 0; i < 4; i++)
            TestSeason.PlayMatchDay(game);

        var stayed = before.Where(b => club.Players.Contains(b.p)).ToList();
        Assert.Contains(stayed, b => b.Equal && b.Slot != (b.p.Condition, b.p.Technique)); // Vorbedingung: gleich und trainiert
        Assert.All(stayed, b => Assert.Equal(b.Pool, b.p.PoolStrengths));
    }

    /// <summary>R923-01: <see cref="Player.SetSlotStrengths"/> hält einen gleichen Poolwert fest, ein abweichender bleibt.</summary>
    [Fact]
    public void SetSlotStrengths_PinsEqualPoolValue_KeepsDifferentOne()
    {
        var p = new Player { Name = "PROBE", Role = PlayerRole.Defender, Condition = 80, Technique = 80 };
        p.SetPoolStrengths(80, 75, hasSlot: true);
        Assert.Equal((null, (int?)75), (p.PoolCondition, p.PoolTechnique));

        p.SetSlotStrengths(82, 79);

        Assert.Equal((82, 79), (p.Condition, p.Technique));
        Assert.Equal((80, 75), p.PoolStrengths);
        p.SetSlotStrengths(82, 79); // unverändert: nichts festzuhalten
        Assert.Equal((80, 75), p.PoolStrengths);
    }

    /// <summary>
    /// R923-02 Mehrsaisonprobe (4 Saisonwechsel, passiver und aktiver Manager, Level 5): Innerhalb einer Saison bleiben die Poolwerte
    /// der Kaderspieler stehen (nur die Drift 0x054D9 am Saisonwechsel ändert sie, A714); alle Poolwerte bleiben in 1 … 99, Pool und
    /// Kaderplatz gehen auseinander, und der Spielstand trägt Kader- und Poolwerte (Schema 55).
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultiSeason_FourChanges_PoolValuesOnlyDriftAtSeasonChange(bool active)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Probe", Head = 0, ClubIndex = 2, Level = 5 });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, seed: 7);
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        var pool = PoolOf(club);
        var changes = 0;
        var drifted = 0;
        RngTwinProbeTests.Run(game, active, afterStep: g =>
        {
            foreach (var p in club.Players)
                if (pool.TryGetValue(p, out var was) && changes == 0)
                    Assert.Equal(was, p.PoolStrengths);
            if (changes > 0)
            {
                Assert.All(g.TransferMarket.Pool, s =>
                {
                    var (c, t) = s.Player.PoolStrengths;
                    Assert.InRange(c, 1, 99);
                    Assert.InRange(t, 1, 99);
                });
                drifted = Math.Max(drifted, club.Players.Count(p => p.PoolCondition is not null || p.PoolTechnique is not null));
                changes = 0;
            }
            pool = PoolOf(club);
        }, beforeChange: _ => changes++);
        Assert.True(drifted > 3, $"abweichende Poolwerte: höchstens {drifted}");
        var loaded = SaveGameTests.AssertRoundTrip(game);
        Assert.Equal(club.Players.Select(p => (p.Condition, p.Technique, p.PoolStrengths)),
            loaded.Clubs[club.Index].Players.Select(p => (p.Condition, p.Technique, p.PoolStrengths)));
    }

    /// <summary>
    /// R923-02 mit Originaldatenbank: nach vier Saisonwechseln tragen Export → Import Kader- und Poolwerte der Managerspieler
    /// (Kader +0x10/+0x11, Pool +0x15/+0x16).
    /// </summary>
    [GameDataTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultiSeason_OriginalDatabase_ExportImportKeepsPoolValues(bool active)
    {
        var raw = OriginalExportTests.RawDatabase();
        var database = ClubDatabase.Parse(raw);
        var game = OriginalExportTests.NewGameForExport(database);
        RngTwinProbeTests.Run(game, active);
        var club = game.Clubs[game.Managers[0].ClubIndex];
        // Spur wettbewerbe-regeln Teil 1 (R15-E2 = C): Ob nach vier Saisons ein Managerspieler abweichende Poolwerte trägt, hängt vom
        // Spielverlauf ab (#925/A838/A105 ändern ihn; passiv dann keiner). Damit die Probe den Fall immer prüft, bekommt ein Spieler
        // ohne Abweichung eine (Pool = Kaderplatz + 1, Pool +0x15/+0x16 neben Kader +0x10/+0x11). Rückschaltprobe: alter Code +
        // dieser Test → grün (die Probe wird nicht schwächer).
        if (!club.Players.Any(p => p.PoolCondition is not null || p.PoolTechnique is not null))
            club.Players[0].SetPoolStrengths(club.Players[0].Condition + 1, club.Players[0].Technique + 1, hasSlot: true);
        Assert.Contains(club.Players, p => p.PoolCondition is not null || p.PoolTechnique is not null);

        var again = GameState.ImportOriginal(OriginalSaveGame.Read(OriginalExport.Export(game, raw)), database, 1);

        var imported = again.Clubs[again.Managers[0].ClubIndex].Players;
        Assert.Equal(club.Players.Select(p => (p.Name, p.Condition, p.Technique, p.PoolStrengths)),
            imported.Select(p => (p.Name, p.Condition, p.Technique, p.PoolStrengths)));
    }
}
