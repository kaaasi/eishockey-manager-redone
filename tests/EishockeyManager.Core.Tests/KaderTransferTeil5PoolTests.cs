using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur <c>kader-transfer</c>, Teil 5: Poolumverteilung (Inventur M-K7, M-K8, F002–F004, F013–F015; P2 Nr. 6). Spielstart Datei
/// 0x057E3, Saisonwechsel 0x07C7E, Soll je Liga und Drift 0x05521/0x054D9, Zielverein 0x07EF1. Vorher rot: Die Poolspieler
/// behielten die Vereine aus der Namensvergabe (zufällig 1. und 2. Liga), die Poolwerte wanderten nie.
/// </summary>
public class KaderTransferTeil5PoolTests
{
    private static void PlaySeason(GameState game)
    {
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 1000, "Saisonende nicht erreicht");
            foreach (var m in game.Managers)
                m.Money = Math.Max(m.Money, 5_000_000);
            game.Continue();
        }
    }

    private static IEnumerable<PoolSlot> LeagueSlots(GameState game) =>
        game.TransferMarket.Pool.Where(s => s.Index <= game.TransferMarket.PoolLayout.LeagueLast);

    private static int InLeague(GameState game, League league) =>
        LeagueSlots(game).Count(s => game.Clubs[s.Club].League == league);

    /// <summary>[ORIGINAL] Datei 0x05760–0x057DD: s[l] = (100 · k[l] / T) · (179 − T) / 100, T = Σ k + Ausland (0 → 1).</summary>
    [Fact]
    public void Quotas_FollowOriginalFormula()
    {
        Assert.Equal([154, 0, 0], PoolDistributionRules.Quotas([25, 0, 0], 0));
        // T = 48, R = 131: 2500 / 48 = 52 → 52 · 131 / 100 = 68; 2000 / 48 = 41 → 53.
        Assert.Equal([68, 53, 0], PoolDistributionRules.Quotas([25, 20, 0], 3));
        Assert.Equal([0, 0, 0], PoolDistributionRules.Quotas([0, 0, 0], 0));
    }

    /// <summary>[ORIGINAL] Datei 0x054D9: Wert + random(1, 7) − 4, begrenzt auf 30 … 99.</summary>
    [Fact]
    public void Drift_StaysWithinThreeAndBounds()
    {
        var random = new GameRandom(5);
        var seen = new HashSet<int>();
        for (var i = 0; i < 500; i++)
        {
            var v = PoolDistributionRules.Drift(60, random);
            Assert.InRange(v, 57, 63);
            seen.Add(v);
            Assert.InRange(PoolDistributionRules.Drift(99, random), 96, 99);
            Assert.Equal(30, PoolDistributionRules.Drift(10, random));
        }
        Assert.Equal(7, seen.Count);
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x07EF1 mit DGROUP 0x48FE: Plätze 0 0 1 1 2 2 3 3 4 4 5 … 11, Managerverein übersprungen; Liga &gt; 2 im
    /// Ausland random(64, 150).
    /// </summary>
    [Fact]
    public void NextPoolClub_CyclesFirstFiveTwiceAndSkipsManagedClub()
    {
        var game = TestGames.Create(); // Manager bei Verein 2 (Bundesliga)
        var market = game.TransferMarket;
        var bundesliga = game.LeagueClubs(League.Bundesliga).OrderBy(c => c.Index).Select(c => c.Index).ToList();
        var expected = new[] { 0, 0, 1, 1, 3, 3, 4, 4, 5, 6, 7, 8, 9, 10, 11 }.Select(p => bundesliga[p]).ToList();
        // Spielstart hat den Zähler schon bewegt: bis zum Anfang der Folge weiterdrehen.
        var random = new GameRandom(1);
        var first = new List<int>();
        for (var i = 0; i < 60; i++)
            first.Add(market.NextPoolClub(game, 0, random));
        var start = Enumerable.Range(0, 30).First(i => first.Skip(i).Take(expected.Count).SequenceEqual(expected));
        Assert.Equal(expected, first.Skip(start + expected.Count).Take(expected.Count));
        Assert.DoesNotContain(2, first);
        for (var i = 0; i < 50; i++)
            Assert.InRange(market.NextPoolClub(game, 3, random), 64, game.Clubs.Count - 1);
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x057E3: Beim Spielstart gehen die Poolspieler in die Liga des Managers (Soll nach seiner Kadergröße), die
    /// ersten fünf Vereine bekommen doppelt so viele.
    /// </summary>
    [Fact]
    public void Start_MovesPoolToManagersLeague()
    {
        var game = TestGames.Create();
        var bundesliga = InLeague(game, League.Bundesliga);
        Assert.True(bundesliga >= 150, $"Bundesliga {bundesliga}");
        Assert.True(InLeague(game, League.ZweiteLiga) <= 25, $"2. Liga {InLeague(game, League.ZweiteLiga)}");
        var clubs = LeagueSlots(game).GroupBy(s => s.Club).ToDictionary(g => g.Key, g => g.Count());
        var first = game.LeagueClubs(League.Bundesliga).OrderBy(c => c.Index).First().Index;
        var last = game.LeagueClubs(League.Bundesliga).OrderBy(c => c.Index).Last().Index;
        Assert.True(clubs[first] > clubs[last] * 3 / 2, $"erste {clubs[first]}, letzte {clubs[last]}");
    }

    /// <summary>
    /// Goldtest FILE05 (Spielstart, Manager Köln = Verein 2, 1. Liga): Die freien Ligaplätze stehen nach 0x057E3 auf den Vereinen
    /// der 1. Liga ohne Köln, die Plätze 0, 1, 3, 4 (die ersten fünf ohne den Managerverein, DGROUP 0x48FE) mit rund doppelt so
    /// vielen Spielern wie 5 … 11 (FILE05: 20/20/20/22 gegen 9–10). Das Remake mit der Originaldatenbank zeigt dasselbe Bild.
    /// </summary>
    [GameDataFact]
    public void Start_OriginalDatabase_MatchesFile05Distribution()
    {
        var save = OriginalSaveGame.Read(File.ReadAllBytes(TransferOriginalTests.OriginalSave("FILE05.HOK")));
        var pool = save.Block("far:8253");
        var original = Enumerable.Range(1, 178)
            .Where(i => pool[i * OriginalSaveGame.PoolRecordLength + 0x1A] is 4 or 6)
            .Select(i => (int)pool[i * OriginalSaveGame.PoolRecordLength + 0x1D]).ToList();
        AssertDoubledFirstFive(original, "FILE05");

        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Joerg", ClubIndex = 2 });
        var game = NewGame.Create(ClubDatabase.Load(TestGameData.Load()), setup, 5);
        AssertDoubledFirstFive(LeagueSlots(game).Where(s => s.Index < 179).Select(s => s.Club).ToList(), "Remake");
    }

    private static void AssertDoubledFirstFive(IReadOnlyList<int> clubs, string what)
    {
        var count = Enumerable.Range(0, 12).Select(c => clubs.Count(x => x == c)).ToArray();
        Assert.Equal(0, count[2]);
        Assert.True(count.Sum() >= 140, $"{what}: nur {count.Sum()} in der 1. Liga");
        var doubled = new[] { 0, 1, 3, 4 }.Average(c => count[c]);
        var single = Enumerable.Range(5, 7).Average(c => count[c]);
        Assert.True(doubled >= 1.6 * single, $"{what}: {string.Join(" ", count)}");
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x057E3–0x058CA: Die Schleife über die Ligen bricht nach einer Zuteilung nicht ab – mit Soll in zwei Ligen
    /// zählt jeder Spieler bei beiden und landet in der zweiten.
    /// </summary>
    [Fact]
    public void Start_TwoLeagues_PlayerCountsForBothAndEndsInSecond()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", Head = 0, ClubIndex = 2 });
        setup.Managers.Add(new ManagerSetup { Name = "B", Head = 1, ClubIndex = 14 });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 3);
        Assert.Equal(League.ZweiteLiga, game.Clubs[14].League);
        // Die ersten Plätze (Schleife ab Index 1) landen alle in der 2. Liga, nicht in der ersten Liga mit Soll.
        var first = LeagueSlots(game).Where(s => s.Club is not 2 and not 14).Take(40).ToList();
        Assert.All(first, s => Assert.Equal(League.ZweiteLiga, game.Clubs[s.Club].League));
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x07C7E nach den neuen Namen (0x56467): Der Saisonwechsel holt Poolspieler aus den anderen Ligen zurück in
    /// die Liga des Managers; freie Plätze driften (0x054D9). Mehrsaisonprobe über vier Wechsel, passiver und aktiver Manager.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SeasonChange_RefillsManagersLeague_FourChanges(bool active)
    {
        var game = TestGames.Create(11);
        var manager = game.ActiveManager;
        for (var season = 0; season < 4; season++)
        {
            if (active)
                foreach (var p in game.Clubs[manager.ClubIndex].Players)
                    p.ContractYears = Math.Max(p.ContractYears, 3); // aktiver Manager: hält seinen Kader unter Vertrag
            PlaySeason(game);
            var before = game.TransferMarket.Pool.Where(s => s.Owner == PoolOwner.Free)
                .ToDictionary(s => s.Index, s => (s.Player, s.Player.Condition, s.Player.Technique));
            game.StartNextSeason();
            var club = game.Clubs[manager.ClubIndex];
            var league = club.League!.Value;
            if (club.Players.Count >= 20)
                Assert.True(InLeague(game, league) >= 140, $"Saison {season}: {InLeague(game, league)} in {league}");
            else
                Assert.False(active); // passiver Manager: ausgelaufene Verträge leeren den Kader, Soll 0 (k[l] = 0)
            Assert.Equal(199, game.TransferMarket.Pool.Count);
            Assert.All(game.TransferMarket.Pool, s => Assert.InRange(s.Club, 0, game.Clubs.Count - 1));
            // Freie Plätze desselben Spielers: Drift höchstens ±3 (ausgenommen, wen der erste Listenwechsel kurz zog, 0x4981E).
            int checkedSlots = 0, within = 0, moved = 0;
            foreach (var slot in game.TransferMarket.Pool.Where(s => s.Owner == PoolOwner.Free && before.ContainsKey(s.Index)))
            {
                var (player, condition, technique) = before[slot.Index];
                if (!ReferenceEquals(player, slot.Player))
                    continue; // umbenannt
                checkedSlots++;
                var dc = Math.Abs(slot.Player.Condition - Math.Clamp(condition, 30, 99));
                var dt = Math.Abs(slot.Player.Technique - Math.Clamp(technique, 30, 99));
                if (dc <= 3 && dt <= 3)
                    within++;
                if (dc != 0 || dt != 0)
                    moved++;
            }
            Assert.True(checkedSlots > 50, $"Saison {season}: nur {checkedSlots} Plätze geprüft");
            Assert.True(within >= checkedSlots * 9 / 10, $"Saison {season}: {within} von {checkedSlots} mit Drift ≤ 3");
            Assert.True(moved >= checkedSlots / 2, $"Saison {season}: nur {moved} von {checkedSlots} gewandert");
        }
    }

    /// <summary>Die Zähler je Liga (DGROUP 0x48FB) überstehen Sichern und Laden; Spielstände vor Schema 30 beginnen bei 0.</summary>
    [Fact]
    public void PoolTurns_RoundTripAndMigrate()
    {
        var game = TestGames.Create();
        var saved = SaveGame.Save(game);
        var loaded = SaveGame.Load(saved);
        Assert.True(loaded.Success, loaded.Error);
        Assert.Equal(saved, SaveGame.Save(loaded.Game!));
        var a = Enumerable.Range(0, 20).Select(_ => game.TransferMarket.NextPoolClub(game, 0, new GameRandom(0))).ToList();
        var b = Enumerable.Range(0, 20).Select(_ => loaded.Game!.TransferMarket.NextPoolClub(loaded.Game!, 0, new GameRandom(0))).ToList();
        Assert.Equal(a, b);

        var root = System.Text.Json.Nodes.JsonNode.Parse(saved)!.AsObject();
        root["schema"] = 29; // vor Teil 5 (Integration Runde 11: 29 → 30)
        var text = root.ToJsonString();
        foreach (var name in new[] { "_poolTurnBundesliga", "_poolTurnZweiteLiga", "_poolTurnOberliga" })
            Assert.Contains($"\"{name}\"", text);
        RemoveFields(root, "_poolTurnBundesliga", "_poolTurnZweiteLiga", "_poolTurnOberliga");
        var old = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.True(old.Success, old.Error);
        var bundesliga = old.Game!.LeagueClubs(League.Bundesliga).OrderBy(c => c.Index).First().Index;
        Assert.Equal(bundesliga, old.Game!.TransferMarket.NextPoolClub(old.Game!, 0, new GameRandom(0)));
    }

    private static void RemoveFields(System.Text.Json.Nodes.JsonNode? node, params string[] names)
    {
        switch (node)
        {
            case System.Text.Json.Nodes.JsonObject obj:
                foreach (var name in names)
                    obj.Remove(name);
                foreach (var (_, child) in obj.ToArray())
                    RemoveFields(child, names);
                break;
            case System.Text.Json.Nodes.JsonArray array:
                foreach (var child in array)
                    RemoveFields(child, names);
                break;
        }
    }
}
