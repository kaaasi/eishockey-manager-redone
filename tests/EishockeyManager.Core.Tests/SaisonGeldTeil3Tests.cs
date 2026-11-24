using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur saison-geld Teil 3 (Runde 25): Aufräumkonzept K11 (Rollenwert Pool +0x18 als ein Feld für Export und Torwürfe) und K13
/// (eine Platzwahl für Spielstart und Hinzufügen, Datei 0x29C3B–0x29CF7). Belege: docs/analyse/re-abweichungen.md K11/K13,
/// <c>k11_rollenwert.py</c>, <c>k13_platzwahl.py</c>. K11-Tests gegen den Stand 3910fd6 rot (Export 30/70, Torwürfe 33/83 fest).
/// </summary>
public class SaisonGeldTeil3Tests
{
    private static GameState Game(int seed)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", Head = 0, ClubIndex = 2, StartLeague = League.Bundesliga });
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    /// <summary>
    /// K11: [ORIGINAL] Datei 0x5B3BB–0x5B49D – je Poolplatz Torhüter 0, Verteidiger 25 … 41, Stürmer 75 … 91, gewürfelt und am
    /// Spieler geführt (vorher nur der Zufall verbraucht, kein Feld).
    /// </summary>
    [Theory]
    [InlineData(3)]
    [InlineData(11)]
    public void PoolBuild_RoleValuesRolledPerSlot(int seed)
    {
        var game = Game(seed);
        var pool = game.TransferMarket.Pool;
        Assert.Equal(TransferPoolRules.Size - 1, pool.Count);
        foreach (var slot in pool)
        {
            Assert.True(slot.Player.HasRoleValue, $"Platz {slot.Index}");
            var (lo, hi) = slot.Player.Role switch
            {
                PlayerRole.Goalkeeper => (0, 0),
                PlayerRole.Defender => (25, 41),
                _ => (75, 91),
            };
            Assert.InRange(slot.Player.RoleValue, lo, hi);
        }
        // gestreut, nicht die feste Mitte
        Assert.True(pool.Where(s => s.Player.Role == PlayerRole.Defender).Select(s => s.Player.RoleValue).Distinct().Count() > 8);
        Assert.True(pool.Where(s => s.Player.Role == PlayerRole.Forward).Select(s => s.Player.RoleValue).Distinct().Count() > 8);
    }

    /// <summary>K11: Der Wurf verbraucht den Zufall wie zuvor (eine Zahl je Feldspielerplatz, keine beim Torhüter).</summary>
    [Fact]
    public void RollRoleValue_DrawsOnlyForSkaters()
    {
        var layout = TransferPoolLayout.Original;
        var a = new GameRandom(5);
        var b = new GameRandom(5);
        for (var i = 1; i < layout.Size; i++)
        {
            var value = TransferPoolRules.RollRoleValue(layout, i, a);
            if (layout.RoleOf(i) == PlayerRole.Goalkeeper)
                Assert.Equal(0, value);
            else
                Assert.Equal((layout.RoleOf(i) == PlayerRole.Defender ? 25 : 75) + TransferRules.Random16(b, 0, 16), value);
        }
        Assert.Equal(b.Capture(), a.Capture());
    }

    /// <summary>
    /// K11: Ein neuer Spieler eines Poolplatzes übernimmt den Rollenwert des Platzes ([ORIGINAL] einziger Schreiber ist der Poolaufbau).
    /// Ein Spieler ohne Poolplatz hat die Mitte der Spanne ([APPROX]).
    /// </summary>
    [Fact]
    public void PoolSlot_NewPlayer_TakesRoleValueOfSlot()
    {
        var old = new Player { Name = "ALT", Role = PlayerRole.Defender, RoleValue = 27 };
        var slot = new PoolSlot { Index = 30, Player = old, Club = 0 };
        var fresh = new Player { Name = "NEU", Role = PlayerRole.Defender };
        Assert.False(fresh.HasRoleValue);
        Assert.Equal(33, fresh.RoleValue);
        slot.Player = fresh;
        Assert.Equal(27, fresh.RoleValue);
        Assert.Equal(0, new Player { Name = "T", Role = PlayerRole.Goalkeeper }.RoleValue);
        Assert.Equal(83, new Player { Name = "S", Role = PlayerRole.Forward }.RoleValue);
    }

    /// <summary>
    /// K11: Torwürfe lesen das Feld ([ORIGINAL] Datei 0x5077–0x52CB: Kandidat, wenn Rollenwert + random(5, 25) &gt; random(10, 99)).
    /// Zwei Verteidiger mit Rollenwert 200 sind immer Kandidaten; mit der festen Mitte 33 nur etwa jeder Zweite, unter zwei
    /// Kandidaten gibt es keine Tore – bei gleichem Strom also deutlich weniger Tore.
    /// </summary>
    [Fact]
    public void CreditSkaters_ReadsRoleValueOfPlayer()
    {
        static double Rate(int? roleValue)
        {
            var random = new Random(4);
            var players = new List<Player>();
            for (var i = 0; i < 2; i++)
            {
                var p = new Player { Name = $"V{i}", Role = PlayerRole.Defender };
                if (roleValue is { } v)
                    p.RoleValue = v;
                players.Add(p);
            }
            const int games = 2_000;
            for (var g = 0; g < games; g++)
                MatchStatistics.CreditSkaters(players, 1, random);
            return players.Sum(p => p.Goals) / (double)games;
        }
        var always = Rate(200);
        var middle = Rate(null);
        Assert.True(always > 2 * middle, $"Rollenwert 200: {always}, Mitte 33: {middle}");
    }

    /// <summary>
    /// K11, Mehrsaisonprobe (AGENTS.md §5, passiv und aktiv): Über vier Saisonwechsel bleibt der Rollenwert am Poolplatz ([ORIGINAL]
    /// einziger Schreiber ist der Poolaufbau beim Spielstart), auch wenn Plätze neue Spieler bekommen.
    /// </summary>
    [GameDataTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void FourSeasonChanges_RoleValueStaysWithSlot(bool active)
    {
        var game = OriginalExportTests.NewGameForExport(ClubDatabase.Parse(OriginalExportTests.RawDatabase()), 3);
        var before = game.TransferMarket.Pool.ToDictionary(s => s.Index, s => s.Player.RoleValue);
        var players = game.TransferMarket.Pool.ToDictionary(s => s.Index, s => s.Player);
        RngTwinProbeTests.Run(game, active);
        Assert.Equal(before, game.TransferMarket.Pool.ToDictionary(s => s.Index, s => s.Player.RoleValue));
        Assert.Contains(game.TransferMarket.Pool, s => !ReferenceEquals(players[s.Index], s.Player)); // Plätze wechselten den Spieler
    }

    /// <summary>K11: Der Export schreibt Pool +0x18 aus dem Feld (vorher 0/30/70 je Art).</summary>
    [GameDataFact]
    public void Export_PoolRoleValue_IsField()
    {
        var raw = OriginalExportTests.RawDatabase();
        var game = OriginalExportTests.NewGameForExport(ClubDatabase.Parse(raw));
        var exported = OriginalSaveGame.Read(OriginalExport.Export(game, raw));
        var defenders = new HashSet<int>();
        foreach (var slot in game.TransferMarket.Pool)
        {
            var value = OriginalImport.PoolEntry(exported, slot.Index)[0x18];
            Assert.Equal(slot.Player.RoleValue, value);
            if (slot.Player.Role == PlayerRole.Defender)
                defenders.Add(value);
        }
        Assert.True(defenders.Count > 8, $"Verteidiger: {string.Join(", ", defenders)}"); // gestreut 25 … 41, nicht fest 30
    }

    /// <summary>K11: Import und Export eines Originalstands geben Pool +0x18 unverändert zurück (FILE01: 25 … 40 Abwehr, 77/78 Sturm).</summary>
    [GameDataTheory]
    [InlineData("FILE01.HOK")]
    [InlineData("FILE27.HOK")]
    public void OriginalRoundTrip_PoolRoleValueKept(string file)
    {
        var raw = OriginalExportTests.RawDatabase();
        var original = OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave(file)));
        var game = GameState.ImportOriginal(original, ClubDatabase.Parse(raw), 4711);
        var exported = OriginalSaveGame.Read(OriginalExport.Export(game, raw));
        for (var i = 1; i < TransferPoolRules.Size; i++)
            Assert.Equal(OriginalImport.PoolEntry(original, i)[0x18], OriginalImport.PoolEntry(exported, i)[0x18]);
    }

    /// <summary>K11: Der Rollenwert übersteht Speichern und Laden; ein Stand mit Schema 64 lädt mit der Mitte der Spanne.</summary>
    [Fact]
    public void SaveLoad_RoleValueKept_Schema64Migrated()
    {
        var game = Game(7);
        var loaded = SaveGame.Load(SaveGame.Save(game)).Game!;
        Assert.Equal(game.TransferMarket.Pool.Select(s => s.Player.RoleValue), loaded.TransferMarket.Pool.Select(s => s.Player.RoleValue));

        var root = System.Text.Json.Nodes.JsonNode.Parse(System.Text.Encoding.UTF8.GetString(SaveGame.Save(game)))!.AsObject();
        root["schema"] = 64;
        foreach (var p in root["spieler"]!.AsArray().OfType<System.Text.Json.Nodes.JsonObject>())
            p.Remove("_roleValue");
        var migrated = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString())).Game!;
        Assert.All(migrated.TransferMarket.Pool, s => Assert.Equal(Player.DefaultRoleValue(s.Player.Role), s.Player.RoleValue));
        Assert.All(migrated.TransferMarket.Pool, s => Assert.False(s.Player.HasRoleValue));
    }

    /// <summary>Würfe nach Vorgabe (wie <c>k13_platzwahl.py</c>); prüft die Grenzen random(a, b).</summary>
    private sealed class Scripted(int lo, int hi, params int[] rolls) : Random
    {
        private readonly Queue<int> _rolls = new(rolls);

        public override int Next(int minValue, int maxValue)
        {
            Assert.Equal((lo, hi), (minValue, maxValue - 1));
            return _rolls.Dequeue();
        }

        public bool Done => _rolls.Count == 0;
    }

    /// <summary>K13: [ORIGINAL] Behalten nur bei Start-Liga ≠ Bundesliga, Verein in ihr und Satz ≤ 35 (Datei 0x29C3B–0x29C75).</summary>
    [Theory]
    [InlineData(League.Oberliga, League.Oberliga, 30, true)]
    [InlineData(League.Oberliga, null, 40, false)]
    [InlineData(League.ZweiteLiga, League.Oberliga, 30, false)]
    [InlineData(League.Bundesliga, League.Bundesliga, 4, false)]
    [InlineData(League.Oberliga, League.Oberliga, 36, false)]
    public void Keeps_AsInOriginal(League start, League? clubLeague, int record, bool keeps) =>
        Assert.Equal(keeps, StartLeaguePlace.Keeps(start, clubLeague, record));

    /// <summary>
    /// K13: Ziehung wie <c>k13_platzwahl.py</c> (Satz = Verein): neu gezogen bei Verein eines früheren Managers und bei einem Eintrag
    /// der ersten 2 · (32 ≫ Runde) Einträge von Zeile 1 (leer = Satz 0).
    /// </summary>
    [Theory]
    // Start, L = Bundesliga, Wurf 0: random(0, 11) = 0, 0, 6 → 6
    [InlineData(0, 0, new int[0], new int[0], new[] { 0, 0, 6 }, 6)]
    // Start, L = Ober, Manager 1, Wurf = Verein von Manager 0: random(24, 35) = 26, 27 → 27
    [InlineData(2, 0, new[] { 26 }, new int[0], new[] { 26, 27 }, 27)]
    // Zug, L = Bundesliga, Runde 1, Wurf Europapokal: 3, 7, 0, 8 → 8
    [InlineData(0, 1, new int[0], new[] { 3, 7, 14, 20, 1, 5, 9, 11 }, new[] { 3, 7, 0, 8 }, 8)]
    // Zug, L = Bundesliga, Runde 1, Wurf 0: 0, 8 → 8
    [InlineData(0, 1, new int[0], new[] { 3, 7, 14, 20, 1, 5, 9, 11 }, new[] { 0, 8 }, 8)]
    // Formel 0x45CD0 für Runde 3/4 (im Spiel nicht erreicht, DGROUP 0x5CF4 ist höchstens 1): Wurf 0 frei bzw. hintere Plätze frei
    [InlineData(0, 3, new int[0], new[] { 3, 7, 14, 20, 1, 5, 9, 11 }, new[] { 0 }, 0)]
    [InlineData(0, 4, new int[0], new[] { 3, 7, 14, 20, 1, 5, 9, 11 }, new[] { 3, 1 }, 1)]
    public void Draw_AsInOriginal(int league, int round, int[] earlier, int[] line, int[] rolls, int expected)
    {
        var first = 12 * league;
        var random = new Scripted(first, first + 11, rolls);
        Assert.Equal(expected, StartLeaguePlace.Draw(first, 12, r => r, earlier.ToHashSet(), line, round, random));
        Assert.True(random.Done);
    }

    /// <summary>
    /// K13: Spielstart und Hinzufügen nehmen dieselbe Ziehung. Beim Hinzufügen nach der Auslosung (Runde 1) sind die acht
    /// Europapokal-Plätze und Satz 0 gesperrt – wie vor der Zusammenlegung.
    /// </summary>
    [Fact]
    public void AddManager_ExcludesCupPlacesAndRecordZero()
    {
        var game = Game(9);
        var top = game.LeagueSlots(League.Bundesliga);
        var europe = game.EuropeanCup?.Draw ?? [];
        var hits = new HashSet<int>();
        for (var s = 0; s < 400; s++)
        {
            var partner = game.DrawStartLeaguePlace(top[5], new HashSet<int>(), new Random(s));
            Assert.NotNull(partner);
            hits.Add(partner!.Value);
        }
        Assert.DoesNotContain(top[0], hits);
        Assert.All(europe, c => Assert.DoesNotContain(c, hits));
        Assert.Equal(top.Skip(1).Where(c => !europe.Contains(c)).Order(), hits.Order());
    }
}
