using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur kaderplatz-werte (Runde 22), Fixlauf zu Review #927: Poolwerte (+0x15/+0x16) neben dem Kaderplatz (+0x10/+0x11).
/// R923-01: Das Training schreibt nur den Kaderplatz (<see cref="Player.SetSlotStrengths"/>). R923-02: Mehrsaisonprobe der Poolwerte.
/// </summary>
public class SlotPoolValuesMultiSeasonTests
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
        // Fixlauf Rebase auf main 4a3a724 (Spur transfer-pool, A864/A391, R21-D2 = A, R15-E2 = C): Saat 2 statt der gemeinsamen – die
        // Verletzung außerhalb des Spiels und die Alterung verlorener Pool-Spieler ändern den Zufallsverlauf; beim passiven Manager
        // trägt nach vier Saisonwechseln mit der gemeinsamen Saat kein Spieler mehr einen abweichenden Poolwert (Vorbedingung der
        // Probe). Rückschaltprobe: src/ von ce658d4 + gemeinsame Saat → grün (alter Zufallsverlauf). Die Probe hängt an der Saat.
        // Spur saison-geld Teil 1 (Runde 25, T03, R21-D2 = A, R15-E2 = C): Saat 3 statt 2 – anderer Zufallsverlauf ab dem ersten Takttag
        // (A829, Datei 0x07215); bei Saat 2 trägt der passive Manager nach vier Wechseln keinen abweichenden Poolwert mehr (Vorbedingung),
        // unter 2–19 erfüllen 3, 8, 9 sie passiv und aktiv. Die Probe hängt an der Saat.
        // Spur saison-geld Teil 2 (Runde 25, F08 A512/A526, R21-D2 = A, R15-E2 = C): Saat 5 statt 3 – 11 Würfe der Baufirmen je Manager und
        // Tag (Datei 0x3ED19–0x3ED48), Sommerpause je Manager verschränkt; bei Saat 3 trägt der passive Manager nach vier Wechseln keinen
        // abweichenden Poolwert mehr, unter 3–11 erfüllt nur 5 die Vorbedingung passiv und aktiv. Die Probe hängt an der Saat.
        // Spur saison-geld Teil 3 (Runde 25, K11, R21-D2 = A, R15-E2 = C): Saat 3 statt 5 – Torwürfe der Poolspieler mit dem gewürfelten
        // Rollenwert (Datei 0x5077–0x52CB), zugleich Rebase auf main 90d8394; bei Saat 5 trägt der passive Manager nach vier Wechseln
        // keinen abweichenden Poolwert mehr, unter 2–15 erfüllen 3, 6, 7, 11, 15 die Vorbedingung passiv und aktiv. Die Probe hängt an der Saat.
        // Spur tagesabschluss Teil 2 (Runde 26, R26-E1 = B, R21-D2 = A, R15-E2 = C): Saat 5 statt 3 – die wöchentlichen Stadionschäden 0x3CFA0 (Datei 0x3D330) würfeln je Manager und Bautag mindestens random(0, 2·d + 8) (Datei 0x3CFC5), anderer Zufallsverlauf ab dem ersten Bautag; bei Saat 3 trägt der passive Manager nach vier Wechseln keinen abweichenden Poolwert mehr (Vorbedingung), unter 4–12 erfüllen 5, 6, 8, 9, 11, 12 sie passiv und aktiv. Die Probe hängt an der Saat.
        // Spur live-verschraenkt Teil 2 (Runde 26, S01, R21-D2 = A, R15-E2 = C): Saat 7 statt 3 – der Ligaspieltag rechnet je
        // Minute alle Paarungen aus dem gemeinsamen Zufall (0x37A62, docs/analyse/live-spieltag.md), anderer Zufallsverlauf ab dem
        // ersten Ligaspieltag; mit 3 ist die Vorbedingung verfehlt. Rückschaltprobe siehe PR #1032. Die Probe hängt an der Saat.
        // Spur live-verschraenkt Teil 2b (Runde 26, S01, R21-D2 = A, R15-E2 = C): Saat 10 statt 7 – Pokal-, Play-off- und WM-Termine
        // rechnen je Minute alle Paarungen (0x37A62 Datei 0x380D0–0x3853E, MatchEngine.PlayDate); bei 7 trägt der passive Manager nach
        // vier Wechseln keinen abweichenden Poolwert mehr, unter 7–16, 24–26 erfüllen 10, 11, 14, 15, 24, 25 die Vorbedingung passiv und
        // aktiv. Rückschaltprobe siehe PR #1032. Die Probe hängt an der Saat.
        // Fixlauf live-verschraenkt, Rebase auf main 9d9ae39 (Runde 26, R21-D2 = A, R15-E2 = C): Saat 12 statt 10 – verschränkte
        // Liga- und Pokaltermine (0x37A62, #1032) und Stadionschäden je Manager (0x3CFA0, Datei 0x3D330, #1034) zusammen ergeben
        // einen anderen Zufallsverlauf als jede Änderung allein; bei 10 trägt der passive Manager nach vier Wechseln keinen
        // abweichenden Poolwert mehr (Vorbedingung), 11 ebenfalls nicht, 12 passiv und aktiv. Rückschaltprobe: src/ von 9d9ae39
        // (main) + Saat 12 grün, src/ von 5d491e5 (alter Head) + Saat 12 passiv rot. Die Probe hängt an der Saat.
        var game = OriginalExportTests.NewGameForExport(database, 12);
        RngTwinProbeTests.Run(game, active);
        var club = game.Clubs[game.Managers[0].ClubIndex];
        Assert.Contains(club.Players, p => p.PoolCondition is not null || p.PoolTechnique is not null);

        var again = GameState.ImportOriginal(OriginalSaveGame.Read(OriginalExport.Export(game, raw)), database, 1);

        var imported = again.Clubs[again.Managers[0].ClubIndex].Players;
        Assert.Equal(club.Players.Select(p => (p.Name, p.Condition, p.Technique, p.PoolStrengths)),
            imported.Select(p => (p.Name, p.Condition, p.Technique, p.PoolStrengths)));
    }
}
