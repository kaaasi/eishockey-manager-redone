using System.Text.Json.Nodes;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur <c>kader-transfer</c>, Teil 2: Spielerdaten im Modell (Inventur A360, A368, A369; P2 Nr. 8). Kaufpreis
/// (Kaderplatz +0x2C, <see cref="Player.PurchasePrice"/>) und Verletzungsart (+0x17, <see cref="Player.InjuryKind"/>).
/// Vorher rot: das Remake führte beide nicht (kein Feld; Spielerdaten-Fenster und Hinweiszeile ohne Werte).
/// </summary>
public class KaderTransferTeil2Tests
{
    /// <summary>
    /// Spielstart: jeder Kaderplatz eines Managers trägt Wert(Preis) / 3 (0x29EB4); Computervereine 0. Zieht keinen Zufall.
    /// </summary>
    [Fact]
    public void NewGame_ManagerSquads_StartWithValuePrice()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", Head = 0, ClubIndex = 2, Level = 1 });
        setup.Managers.Add(new ManagerSetup { Name = "B", Head = 1, ClubIndex = 17, Level = 5 });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 11);
        foreach (var manager in game.Managers)
        {
            var club = game.Clubs[manager.ClubIndex];
            Assert.NotEmpty(club.Players);
            foreach (var p in club.Players)
            {
                var thirds = game.PlayerValueOf(p, club, PlayerValueKind.Price, new NoDraw(), manager.Level);
                Assert.True(thirds > 0);
                Assert.Equal(thirds / 3, p.PurchasePrice);
                Assert.Equal(p.PurchasePrice, Contracts.ValuePrice(game, manager, p));
            }
        }
        var managed = game.Managers.Select(m => m.ClubIndex).ToHashSet();
        Assert.All(game.Clubs.Where(c => !managed.Contains(c.Index)).SelectMany(c => c.Players),
            p => Assert.Equal(0, p.PurchasePrice));

        var twin = NewGame.Create(TestGames.CreateDatabase(), setup, 11);
        Assert.Equal(twin.Random.Next(), game.Random.Next());
    }

    /// <summary>Kauf: +0x2C = angenommenes Gebot (0x4781B), nicht der Listenpreis.</summary>
    [Fact]
    public void Purchase_SetsPurchasePrice_ToOffer()
    {
        var (game, manager, club, listing) = Market();
        var p = listing.Player;
        var offer = listing.Price * 2;
        Assert.Equal(TransferResult.Accepted, game.TransferMarket.Buy(game, manager, listing, offer, loan: false));
        Assert.Contains(p, club.Players);
        Assert.Equal(offer, p.PurchasePrice);
    }

    /// <summary>Leihe: derselbe Weg 0x4781B (Gebot ohne Verhandlung, 0x47851) – +0x2C = Leihgebühr.</summary>
    [Fact]
    public void Loan_SetsPurchasePrice_ToFee()
    {
        var (game, manager, club, listing) = Market();
        var p = listing.Player;
        var fee = listing.LoanFee * 2;
        Assert.Equal(TransferResult.Accepted, game.TransferMarket.Bid(game, manager, listing, fee, loan: true, out _));
        Assert.Contains(p, club.Players);
        Assert.True(p.OnLoan);
        Assert.Equal(fee, p.PurchasePrice);
    }

    /// <summary>Namen der Verletzungsarten (DGROUP 0xA00): 28 Einträge, Index wie die Wochentabelle 0x181.</summary>
    [Fact]
    public void InjuryKinds_Names()
    {
        Assert.Equal(28, InjuryKinds.Names.Length);
        Assert.Equal("Schädelbruch", InjuryKinds.Name(0));
        Assert.Equal("Meniskuskorbhenkelriß", InjuryKinds.Name(5));
        Assert.Equal("Darmerkrankung", InjuryKinds.Name(27));
        Assert.Null(InjuryKinds.Name(InjuryKinds.Unknown));
        Assert.Null(InjuryKinds.Name(28));
        Assert.Null(new Player { Name = "X", Role = PlayerRole.Forward }.InjuryName);
    }

    /// <summary>Spielstand-Rundlauf der neuen Felder.</summary>
    [Fact]
    public void SaveGame_RoundTrip_NewFields()
    {
        var game = TestGames.Create(4);
        var p = game.Clubs[game.ActiveManager.ClubIndex].Players[3];
        p.PurchasePrice = 123_456;
        p.InjuryKind = 13;
        var saved = SaveGame.Save(game);
        var loaded = SaveGame.Load(saved);
        Assert.True(loaded.Success, loaded.Error);
        var q = loaded.Game!.Clubs[game.ActiveManager.ClubIndex].Players[3];
        Assert.Equal((123_456L, 13), (q.PurchasePrice, q.InjuryKind));
        Assert.Equal("Mittelfußknochenbruch", q.InjuryName);
        Assert.Equal(saved, SaveGame.Save(loaded.Game!));
    }

    /// <summary>Migration 22 → 23: Kaufpreis 0, Art unbekannt.</summary>
    [Fact]
    public void Migration_FromSchema27()
    {
        var game = TestGames.Create(4);
        var json = JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        json["schema"] = 27; // vor Teil 2 (Integration Runde 11: 27 → 28)
        foreach (var player in json["spieler"]!.AsArray().OfType<JsonObject>())
        {
            Assert.True(player.Remove("PurchasePrice"));
            Assert.True(player.Remove("InjuryKind"));
        }
        var result = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(json.ToJsonString()));
        Assert.True(result.Success, result.Error);
        var all = result.Game!.Clubs.SelectMany(c => c.Players).ToList();
        Assert.All(all, p => Assert.Equal((0L, InjuryKinds.Unknown), (p.PurchasePrice, p.InjuryKind)));
    }

    /// <summary>
    /// Original-Spielstände bei Spielbeginn (FILE01, FILE09 mit zwei Managern und Level 5, FILE10): +0x2C jedes Kaderplatzes
    /// der Manager (ohne Bit 31) ist genau Wert(Preis) aus den gespeicherten Eingängen (0x29EB4), ohne Zufall. FILE07: der
    /// gekaufte DAFFNER trägt das Gebot (400.000 DM). Alle 13 Spielstände: Sperre (+0x09 Bit 0) steht außerhalb des Spiels nie
    /// (0x551E0 löscht sie nach dem Spiel); verletzte Plätze (+0x09 &amp; 2) hätten eine Art 0 … 27 (+0x17) und eine Dauer (+0x0D)
    /// bis zum Höchstwert der Art oder 0xFF.
    /// </summary>
    [GameDataFact]
    public void OriginalSaves_PriceAndInjuryKind()
    {
        var checkedSlots = 0;
        foreach (var (name, managers) in new[] { ("FILE01.HOK", 1), ("FILE09.HOK", 2), ("FILE10.HOK", 1) })
        {
            var save = OriginalSaveGame.Read(File.ReadAllBytes(OriginalSave(name)));
            for (var m = 0; m < managers; m++)
            {
                foreach (var slot in Slots(save, m))
                {
                    Assert.Equal(BitConverter.ToInt32(slot, 0x2C) & 0x7FFF_FFFF, Price(save, slot));
                    checkedSlots++;
                }
            }
        }
        Assert.True(checkedSlots >= 4 * 22, $"nur {checkedSlots} Kaderplätze");

        foreach (var name in AllSaves)
        {
            var save = OriginalSaveGame.Read(File.ReadAllBytes(OriginalSave(name)));
            for (var m = 0; m < save.HeaderManagerCount; m++)
            {
                foreach (var slot in Slots(save, m))
                {
                    Assert.Equal(0, slot[0x09] & 1);
                    if ((slot[0x09] & 2) == 0)
                        continue;
                    Assert.InRange(slot[0x17], 0, InjuryKinds.Names.Length - 1);
                    Assert.True(slot[0x0D] == PlayerInjuries.Permanent || slot[0x0D] <= MaxWeeks[slot[0x17]],
                        $"{name}: Art {slot[0x17]}, Dauer {slot[0x0D]}");
                }
            }
        }
        // Keiner der 13 Spielstände hat einen verletzten Kaderplatz eines Managers (Stand 02.11.2026); die Prüfung der Art
        // greift, sobald einer hinzukommt. Bis dahin ist die Art nur per Disassembly belegt (0x05B1A, 0x41A57–0x41AA4).

        var bought = OriginalSaveGame.Read(File.ReadAllBytes(OriginalSave("FILE07.HOK")));
        Assert.Single(Slots(bought, 0), s => (BitConverter.ToInt32(s, 0x2C) & 0x7FFF_FFFF) == 400_000 * 3);
    }

    private static readonly string[] AllSaves =
        ["FILE01.HOK", "FILE02.HOK", "FILE03.HOK", "FILE05.HOK", "FILE06.HOK", "FILE07.HOK", "FILE08.HOK", "FILE09.HOK",
         "FILE10.HOK", "FILE11.HOK", "FILE12.HOK", "FILE13.HOK", "FILE14.HOK"];

    /// <summary>Höchstdauer je Art (DGROUP 0x181, zweiter Wert der Paare; wie <see cref="PlayerInjuries"/>).</summary>
    private static readonly int[] MaxWeeks =
        [28, 19, 4, 15, 16, 12, 16, 30, 3, 30, 20, 16, 18, 18, 40, 20, 16, 8, 6, 28, 7, 17, 13, 13, 20, 12, 8, 9];

    /// <summary>
    /// Mehrsaisonprobe (4 Saisonwechsel, passiv und aktiv): aktiv kauft der Manager je Saison einen Spieler; der Kaufpreis
    /// bleibt das Gebot, die Startpreise bleiben über die Saisonwechsel stehen, Speichern und Laden ändert nichts.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultiSeason_FourChanges_PurchasePrice(bool active)
    {
        var game = TestGames.Create(21);
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        var prices = club.Players.ToDictionary(p => p, p => p.PurchasePrice);
        Assert.All(prices.Values, v => Assert.True(v > 0));
        var bought = 0;
        for (var change = 0; change < 4; change++)
        {
            var boughtThisSeason = false;
            for (var day = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; day++)
            {
                Assert.True(day < 1000, "Saisonende nicht erreicht");
                TestSeason.KeepSquad(game, manager);
                manager.Money = Math.Max(manager.Money, 5_000_000);
                if (active && !boughtThisSeason && game.Phase == SeasonPhase.Running && club.Players.Count < TransferMarket.MaxSquad
                    && game.TransferMarket.Listings.FirstOrDefault(l => !l.IsFreeAgent && l.Seller is null
                        && l.From.Index != manager.ClubIndex) is { } listing)
                {
                    var p = listing.Player;
                    var offer = listing.Price * 2;
                    if (game.TransferMarket.Buy(game, manager, listing, offer, loan: false) == TransferResult.Accepted)
                    {
                        Assert.Equal(offer, p.PurchasePrice);
                        prices[p] = offer;
                        bought++;
                        boughtThisSeason = true;
                    }
                }
                game.Continue();
            }
            game.StartNextSeason();
            foreach (var p in club.Players.Where(prices.ContainsKey))
                Assert.Equal(prices[p], p.PurchasePrice);
            var saved = SaveGame.Save(game);
            var loaded = SaveGame.Load(saved);
            Assert.True(loaded.Success, loaded.Error);
            Assert.Equal(saved, SaveGame.Save(loaded.Game!));
        }
        if (active)
            Assert.True(bought >= 2, $"nur {bought} Käufe");
    }

    private static (GameState Game, Manager Manager, Club Club, TransferListing Listing) Market()
    {
        var game = TestGames.Create(3);
        var manager = game.ActiveManager;
        manager.Money = 10_000_000;
        var club = game.Clubs[manager.ClubIndex];
        while (club.Players.Count >= TransferMarket.MaxSquad)
            club.Players.RemoveAt(club.Players.Count - 1);
        var listing = game.TransferMarket.Listings.First(l => !l.IsFreeAgent && l.Seller is null && l.From != club);
        return (game, manager, club, listing);
    }

    private static IEnumerable<byte[]> Slots(OriginalSaveGame save, int manager)
    {
        var squad = save.Block("5DC3");
        for (var i = 0; i < OriginalSaveGame.SquadSlotsPerManager; i++)
        {
            var slot = squad.AsSpan(manager * 0x618 + i * OriginalSaveGame.SquadSlotLength, OriginalSaveGame.SquadSlotLength);
            if (slot.ContainsAnyExcept((byte)0))
                yield return slot.ToArray();
        }
    }

    /// <summary>Wert(Preis) aus den gespeicherten Eingängen (Zuordnung wie <see cref="TransferVertraegeKomplettTeil2bTests"/>).</summary>
    private static int Price(OriginalSaveGame save, byte[] slot)
    {
        var pool = save.Block("far:8253");
        var player = pool.AsSpan(slot[0x0F] * OriginalSaveGame.PoolRecordLength, OriginalSaveGame.PoolRecordLength);
        Assert.Equal(0, slot[0x09] & 0x80); // bei Spielbeginn kein Angebot eines Vereins
        var inputs = new PlayerValueInputs
        {
            Penalties5 = slot[0x00],
            Penalties2 = slot[0x01],
            Goals0 = slot[0x03],
            Goals1 = slot[0x04],
            Goals2 = slot[0x05],
            Trend = slot[0x0E],
            Condition = slot[0x10],
            Technique = slot[0x11],
            Games0 = BitConverter.ToUInt16(slot, 0x1C),
            Games1 = BitConverter.ToUInt16(slot, 0x1E),
            Games2 = BitConverter.ToUInt16(slot, 0x20),
            Age = player[0x14],
            Goalkeeper = player[0x18] == 0,
            ClubClass = OriginalClass(player[0x1D]),
            LevelValue = (sbyte)save.Block("0ED8")[0],
        };
        return PlayerValue.Compute(inputs, PlayerValueKind.Price, (_, _) => throw new InvalidOperationException());
    }

    /// <summary>Spielklasse nach Vereinsindex. [ORIGINAL] em.exe 0x34FD5 mit DGROUP 0x7C1 = 12, 0x7C2 = 24, 0x6AD = 52.</summary>
    private static int OriginalClass(int club) => club < 12 || club > 52 ? 1 : club < 24 ? 2 : 4;

    private static string OriginalSave(string name)
    {
        var folder = Environment.GetEnvironmentVariable("EM_SAVEGAMES");
        if (string.IsNullOrEmpty(folder))
        {
            var root = TestGameData.Load().RootDirectory;
            var real = Directory.ResolveLinkTarget(root.TrimEnd('/', '\\'), true)?.FullName ?? root;
            folder = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(real).TrimEnd('/', '\\'))!, "spielstaende");
        }
        var path = Path.Combine(folder, name);
        return File.Exists(path) ? path
            : throw new FileNotFoundException($"Original-Spielstand {name} fehlt in {folder} (Ordner per EM_SAVEGAMES angeben).");
    }

    private sealed class NoDraw : Random
    {
        public override int Next(int minValue, int maxValue) => throw new InvalidOperationException("Zufall gezogen");

        public override int Next() => throw new InvalidOperationException("Zufall gezogen");

        public override int Next(int maxValue) => throw new InvalidOperationException("Zufall gezogen");
    }
}
