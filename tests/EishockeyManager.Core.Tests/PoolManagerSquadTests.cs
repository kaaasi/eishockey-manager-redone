using System.Text.Json.Nodes;
using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur pool (Runde 16, #743), Teil 2: Die Managerkader stehen im Spielerpool wie im Original. [ORIGINAL] Spielvorbereitung
/// Datei 0x5AF50: je Manager und Art die Plätze DGROUP 0x382[a] − Zähler[a] + 1 (Datei 0x5B0BB–0x5B2BE, Zähler über alle Manager),
/// Besitzer = Manager (Datei 0x5B1E9); Gegenprobe FILE01 (2–3, 22–31, 98–110), FILE09 (zweiter Manager 4–5, 32–41, 111–123),
/// FILE12 (Oberliga 2–3, 22–29, 98–107); <c>docs/formate/spielstand-original.md</c>, „Managerkader im Spielerpool“.
/// Vorher rot: Der Remake-Pool führte 199 Poolspieler neben den Managerkadern, kein Startkaderspieler hatte einen Platz.
/// </summary>
public class PoolManagerSquadTests
{
    private static GameState Create(params (int Club, League League)[] managers)
    {
        var setup = new GameSetup();
        foreach (var (club, league) in managers)
            setup.Managers.Add(new ManagerSetup { Name = $"M{club}", ClubIndex = club, StartLeague = league });
        return NewGame.Create(TestGames.CreateDatabase(), setup, 1);
    }

    private static int[] Range(int first, int count) => Enumerable.Range(first, count).ToArray();

    /// <summary>Poolindizes der Spieler eines Kaders in Kaderfolge (−1: ohne Platz).</summary>
    private static int[] IndicesOf(GameState game, Club club) =>
        club.Players.Select(p => game.TransferMarket.Pool.FirstOrDefault(s => ReferenceEquals(s.Player, p))?.Index ?? -1).ToArray();

    [Fact]
    public void NewGame_ManagerSquad_TakesPoolSlotsLikeFile01()
    {
        var game = Create((5, League.Bundesliga));
        var club = game.Clubs[game.Managers[0].ClubIndex];
        Assert.Equal(League.Bundesliga, club.League);

        Assert.Equal([.. Range(2, 2), .. Range(22, 10), .. Range(98, 13)], IndicesOf(game, club));
        foreach (var index in IndicesOf(game, club))
        {
            var slot = game.TransferMarket.Pool[index - 1];
            Assert.Equal(PoolOwner.Taken, slot.Owner);
            Assert.Equal(club.Index, slot.Club);
        }
        // Im Pool stehen genau 199 Plätze, jeder Spieler höchstens einmal; die übrigen sind frei oder auf dem Markt.
        Assert.Equal(TransferPoolRules.Size - 1, game.TransferMarket.Pool.Count);
        Assert.Equal(game.TransferMarket.Pool.Count,
            game.TransferMarket.Pool.Select(s => s.Player).Distinct(ReferenceEqualityComparer.Instance).Count());
        Assert.Equal(25, game.TransferMarket.Pool.Count(s => s.Owner == PoolOwner.Taken));
        Assert.Empty(game.ValidatePlayerIdentity());
    }

    [Fact]
    public void NewGame_TwoManagers_CountersRunOnLikeFile09()
    {
        var game = Create((5, League.Bundesliga), (7, League.Bundesliga));
        Assert.Equal([.. Range(2, 2), .. Range(22, 10), .. Range(98, 13)], IndicesOf(game, game.Clubs[game.Managers[0].ClubIndex]));
        Assert.Equal([.. Range(4, 2), .. Range(32, 10), .. Range(111, 13)], IndicesOf(game, game.Clubs[game.Managers[1].ClubIndex]));
    }

    [Fact]
    public void NewGame_OberligaManager_TwentySlotsLikeFile12()
    {
        var game = Create((30, League.Oberliga));
        var club = game.Clubs[game.Managers[0].ClubIndex];
        Assert.Equal(League.Oberliga, club.League);
        Assert.Equal([.. Range(2, 2), .. Range(22, 8), .. Range(98, 10)], IndicesOf(game, club));
    }

    /// <summary>Gegenprobe mit Originaldaten: dieselben Plätze wie FILE01 (Bundesliga, ein Manager), Name in Platzfolge.</summary>
    [GameDataFact]
    public void NewGame_RealDatabase_SameSlotsAsFile01()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "M", ClubIndex = 5, StartLeague = League.Bundesliga });
        var game = NewGame.Create(ClubDatabase.Load(TestGameData.Load()), setup, 1);
        var club = game.Clubs[game.Managers[0].ClubIndex];

        var save = OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave("FILE01.HOK")));
        var expected = Enumerable.Range(1, TransferPoolRules.Size - 1).Where(i => OriginalImport.PoolEntry(save, i)[0x1A] == 0).ToArray();
        Assert.Equal(expected, IndicesOf(game, club));
        Assert.Equal(expected, game.TransferMarket.Pool.Where(s => s.Owner == PoolOwner.Taken).Select(s => s.Index).ToArray());
    }

    /// <summary>
    /// Teil 3 (#743): Export eines Remake-Spiels ohne Ersatzregel – jeder Kaderplatz eines Managers verweist auf den Poolsatz seines
    /// Spielers (Name, Besitzer = Manager), kein freier Poolspieler weicht. Vorher bekamen alle 25 Startkaderspieler einen freien
    /// Ligaplatz, dessen Poolspieler im Export entfiel.
    /// </summary>
    [GameDataFact]
    public void Export_RemakeGame_SquadSlotsPointToOwnPoolSlots()
    {
        var raw = OriginalExportTests.RawDatabase();
        var game = OriginalExportTests.NewGameForExport(ClubDatabase.Parse(raw));
        var exported = OriginalSaveGame.Read(OriginalExport.Export(game, raw));
        for (var m = 0; m < game.Managers.Count; m++)
        {
            var club = game.Clubs[game.Managers[m].ClubIndex];
            var expected = IndicesOf(game, club);
            Assert.DoesNotContain(-1, expected);
            for (var k = 0; k < club.Players.Count; k++)
            {
                var index = OriginalImport.SquadSlot(exported, m, k)[0x0F];
                Assert.Equal(expected[k], index);
                Assert.Equal(m, OriginalImport.PoolEntry(exported, index)[0x1A]);
            }
            Assert.Equal(club.Players.Count, Enumerable.Range(1, TransferPoolRules.Size - 1)
                .Count(i => OriginalImport.PoolEntry(exported, i)[0x1A] == m));
        }
    }

    /// <summary>
    /// Teil 3: Vertragsende im Managerkader (R15-E3 = C) – der Spieler ist verloren, sein Platz bleibt vergeben und gehört dem Manager
    /// ([ORIGINAL] Datei 0x27DE3–0x27E9D lässt Pool +0x1A stehen, FILE44 → FILE46 KJELLBERG Pool 196 +0x1A = 0); Sichern/Laden behält
    /// ihn. Vorher schrieb der Export für einen solchen Platz 6.
    /// </summary>
    [Fact]
    public void ContractEnd_LostPlayer_SlotKeepsManager()
    {
        var game = Create((5, League.Bundesliga));
        var club = game.Clubs[game.Managers[0].ClubIndex];
        foreach (var p in club.Players)
            p.ContractYears = 3;
        var lost = club.Players[3];
        var index = IndicesOf(game, club)[3];
        lost.ContractYears = 1;
        PlayToSeasonOver(game);
        game.StartNextSeason();

        Assert.DoesNotContain(lost, club.Players);
        var slot = game.TransferMarket.Pool[index - 1];
        Assert.Same(lost, slot.Player);
        Assert.Equal(PoolOwner.Taken, slot.Owner);
        Assert.Equal(0, slot.Manager);
        var loaded = SaveGame.Load(SaveGame.Save(game)).Game!;
        Assert.Equal(0, loaded.TransferMarket.Pool[index - 1].Manager);
    }

    /// <summary>
    /// R765-01 (#775): Leihe Manager → Manager über den Saisonwechsel. Der Spieler kehrt in den Kader des Stammvereins zurück
    /// ([ORIGINAL] Datei 0x27547–0x275D2, Rückkehr zum Managerverein mit der Leih-Laufzeit, Player.Contract.cs) und behält seinen
    /// Poolplatz: vergeben, nicht umbenannt, kein gemerkter Manager. Rückschaltprobe: Ohne die Kaderklausel in StartPoolSeason
    /// würde der Platz frei und „POOLSPIELER n“.
    /// </summary>
    [Fact]
    public void LoanBetweenManagers_OverSeasonChange_KeepsPoolSlot()
    {
        var game = Create((5, League.Bundesliga), (7, League.Bundesliga));
        var origin = game.Clubs[game.Managers[0].ClubIndex];
        var borrower = game.Clubs[game.Managers[1].ClubIndex];
        foreach (var p in origin.Players.Concat(borrower.Players))
            (p.ContractYears, p.Age) = (3, 25);
        var lent = origin.Players.Last(p => p.Role == PlayerRole.Forward);
        var index = IndicesOf(game, origin)[origin.Players.IndexOf(lent)];
        origin.RemovePlayer(lent);
        PlayerFactory.Join(borrower, lent, 2, loan: true, new PlayerLoan(origin, 3, lent.MonthlySalary, false), managerSquad: true);
        Assert.Equal(index, IndicesOf(game, borrower)[borrower.Players.IndexOf(lent)]);

        PlayToSeasonOver(game);
        game.StartNextSeason();

        Assert.Contains(lent, origin.Players);
        Assert.DoesNotContain(lent, borrower.Players);
        Assert.Null(lent.Loan);
        var slot = game.TransferMarket.Pool[index - 1];
        Assert.Same(lent, slot.Player);
        Assert.Equal(PoolOwner.Taken, slot.Owner);
        Assert.Equal(-1, slot.Manager);
        Assert.Equal(origin.Index, slot.Club);
        AssertSeated(game);
    }

    /// <summary>Speichern und Laden behalten die Plätze (derselbe Spieler im Kader und im Pool).</summary>
    [Fact]
    public void SaveAndLoad_KeepsSeatedSquad()
    {
        var game = Create((5, League.Bundesliga));
        var loaded = SaveGame.Load(SaveGame.Save(game)).Game!;
        var club = loaded.Clubs[loaded.Managers[0].ClubIndex];
        Assert.Equal(IndicesOf(game, game.Clubs[club.Index]), IndicesOf(loaded, club));
        Assert.Equal(SaveGame.Save(game), SaveGame.Save(loaded));
    }

    /// <summary>
    /// Migration 48 → 49 (<c>SaveGame.Migration.Pool.cs</c>): Ein Spielstand, dessen Startkader außerhalb des Pools steht (wie vor
    /// dieser Spur: eigene Poolspieler auf allen 199 Plätzen), bekommt beim Laden die freien Ligaplätze seiner Art.
    /// </summary>
    [Fact]
    public void OldSave_SquadOutsidePool_IsSeatedOnLoad()
    {
        var game = Create((5, League.Bundesliga));
        var root = OldSave(game);
        var loaded = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.Null(loaded.Error);
        var club = loaded.Game!.Clubs[loaded.Game.Managers[0].ClubIndex];
        Assert.Equal([.. Range(2, 2), .. Range(22, 10), .. Range(98, 13)], IndicesOf(loaded.Game, club));
        Assert.Empty(loaded.Game.ValidatePlayerIdentity());

        // Ohne Migration (Schema 49) bleibt der Stand, wie er ist.
        root["schema"] = SaveGame.SchemaVersion;
        var current = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString())).Game!;
        Assert.All(IndicesOf(current, current.Clubs[club.Index]), i => Assert.Equal(-1, i));
    }

    /// <summary>Stand wie vor der Spur: Die Plätze der Managerkader tragen eigene, freie Poolspieler; Schema 48.</summary>
    private static JsonObject OldSave(GameState game)
    {
        var root = JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        var players = root["spieler"]!.AsArray();
        var byId = players.Select(p => p!.AsObject()).ToDictionary(p => (int)p["Id"]!);
        var next = (int)root["spiel"]!["NextPlayerId"]!;
        foreach (var slot in root["spiel"]!["TransferMarket"]!["_pool"]!["items"]!.AsArray().Select(s => s!.AsObject()))
        {
            if ((string)slot["Owner"]! != "Taken")
                continue;
            var copy = byId[(int)slot["Player"]!["$spieler"]!].DeepClone().AsObject();
            copy["Id"] = next;
            copy["Name"] = $"ALT {next}";
            players.Add(copy);
            slot["Player"] = new JsonObject { ["$spieler"] = next++ };
            slot["Owner"] = "Free";
            slot["Club"] = 0;
        }
        root["spiel"]!["NextPlayerId"] = next;
        root["schema"] = 48;
        return root;
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
    /// Mehrsaisonprobe (AGENTS.md §5): vier Saisonwechsel, passiver und aktiver Manager (kauft je Saison einen Poolspieler, bietet
    /// einen Kaderspieler an). Nach jedem Wechsel steht jeder Spieler eines Managerkaders auf genau einem vergebenen Poolplatz, kein
    /// Spieler auf zwei Plätzen, und kein vergebener Platz gehört einem Spieler eines Computervereins (Verkauf, Leihende geben ihn
    /// frei).
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FourSeasonChanges_ManagerSquadsStayInPool(bool active)
    {
        var game = Create((5, League.Bundesliga));
        var manager = game.Managers[0];
        var club = game.Clubs[manager.ClubIndex];
        for (var season = 0; season < 4; season++)
        {
            TestSeason.KeepSquad(game, manager, extendExpired: true);
            if (active)
            {
                manager.Money += 5_000_000;
                if (game.TransferMarket.Listings.FirstOrDefault(l => l.FromPool) is { } listing)
                    game.TransferMarket.Buy(game, manager, listing, listing.Price * 2, loan: false);
                if (club.Players.LastOrDefault(p => p.Role == PlayerRole.Forward) is { } offered)
                    game.TransferMarket.Offer(game, manager, offered);
            }
            PlayToSeasonOver(game);
            game.StartNextSeason();
            AssertSeated(game);
        }
    }

    private static void AssertSeated(GameState game)
    {
        var pool = game.TransferMarket.Pool;
        Assert.Equal(TransferPoolRules.Size - 1, pool.Count);
        Assert.Equal(pool.Count, pool.Select(s => s.Player).Distinct(ReferenceEqualityComparer.Instance).Count());
        var managed = game.Managers.Select(m => game.Clubs[m.ClubIndex]).ToHashSet();
        foreach (var club in managed)
            foreach (var p in club.Players)
                Assert.True(pool.Any(s => s.Owner == PoolOwner.Taken && ReferenceEquals(s.Player, p)),
                    $"{p.Name} ({club.Name}) ohne vergebenen Poolplatz");
        foreach (var slot in pool.Where(s => s.Owner == PoolOwner.Taken))
        {
            var holder = game.Clubs.FirstOrDefault(c => c.Players.Contains(slot.Player));
            if (holder is null || managed.Contains(holder))
                continue;
            Assert.True(slot.Player.Loan?.Origin is { } origin && managed.Contains(origin),
                $"Platz {slot.Index} ({slot.Player.Name}) vergeben, Spieler bei {holder.Name}");
        }
        Assert.Empty(game.ValidatePlayerIdentity());
    }
}
