using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur <c>transfer-vertraege-komplett</c>, Rest von Teil 2: Start- und Wechselgehalt = Wert(Gehalt) (0x118:0x70 Art 1).
/// [ORIGINAL] Spielstart Datei 0x29EE4 (nur Kaderplätze der Manager), Kauf über den neuen Kaderplatz 0x42CB1 (0x42F11, ohne
/// Leihe). Vorher rot: Startgehalt und Gehalt beim Kauf waren die Forderung des Remakes (<see cref="Contracts.Demand"/>).
/// </summary>
public class TransferVertraegeKomplettTeil2bTests
{
    /// <summary>Spielstart: alle Kaderplätze der Manager tragen Wert(Gehalt) im eigenen Verein und Level; Computervereine nicht.</summary>
    [Fact]
    public void NewGame_ManagerSquads_StartWithValueSalary()
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
                var thirds = game.PlayerValueOf(p, club, PlayerValueKind.Salary, new NoDraw(), manager.Level);
                Assert.Equal(0, thirds % 300); // ganze 100 DM
                Assert.Equal(thirds / 3, p.MonthlySalary);
                Assert.Equal(p.MonthlySalary, Contracts.ValueSalary(game, manager, p));
            }
        }
        var managed = game.Managers.Select(m => m.ClubIndex).ToHashSet();
        var computer = game.Clubs.Where(c => !managed.Contains(c.Index)).SelectMany(c => c.Players).ToList();
        Assert.NotEmpty(computer);
        Assert.All(computer, p => Assert.Equal(Contracts.Demand(p), p.MonthlySalary));
    }

    /// <summary>Die Umstellung zieht keinen Zufall: Zufallsfolge nach dem Spielstart wie bei einem zweiten Spiel gleicher Saat.</summary>
    [Fact]
    public void NewGame_StartSalary_IsDeterministic()
    {
        var a = TestGames.Create(5);
        var b = TestGames.Create(5);
        Assert.Equal(a.Random.Next(), b.Random.Next());
        var club = a.Clubs[a.ActiveManager.ClubIndex];
        Assert.Contains(club.Players, p => p.MonthlySalary != Contracts.Demand(p));
    }

    /// <summary>Kauf: die Verhandlung beginnt mit 1 Jahr und Wert(Gehalt) im Verein des Käufers (0x42CB1, nicht im abgebenden
    /// Verein); „Mach' ich!“ ohne Eingaben übernimmt beides.</summary>
    [Fact]
    public void Bid_Purchase_StartsWithValueSalary_OfBuyerClub()
    {
        var (game, manager, club, listing) = Market();
        var p = listing.Player;
        Assert.Equal(TransferResult.Accepted,
            game.TransferMarket.Bid(game, manager, listing, listing.Price * 2, loan: false, out var purchase));
        Assert.NotNull(purchase);
        var expected = game.PlayerValueOf(p, club, PlayerValueKind.Salary, new NoDraw(), manager.Level) / 3;
        Assert.Equal(expected, purchase.Salary);
        Assert.NotEqual(Contracts.Demand(p), purchase.Salary);
        Assert.Equal(1, purchase.Years);
        Assert.Equal(NegotiationResult.Accepted, game.TransferMarket.Sign(game, purchase, 0, 0, insured: false, false));
        Assert.Contains(p, club.Players);
        Assert.Equal((1, expected), (p.ContractYears, p.MonthlySalary));
    }

    /// <summary>
    /// Original-Spielstände bei Spielbeginn (FILE01, FILE09 mit zwei Managern und Level 5, FILE10): +0x28 jedes Kaderplatzes der
    /// Manager ist genau Wert(Gehalt) aus den gespeicherten Eingängen (0x29EE4). FILE07: der gekaufte DAFFNER (+0x2C = Gebot
    /// 400.000 DM) trägt Wert(Gehalt) = 9.900 DM (0x42F11) und 1 Jahr.
    /// </summary>
    [GameDataFact]
    public void OriginalSaves_StartAndPurchaseSalary_AreTheSalaryValue()
    {
        var checkedSlots = 0;
        foreach (var (name, managers) in new[] { ("FILE01.HOK", 1), ("FILE09.HOK", 2), ("FILE10.HOK", 1) })
        {
            var save = OriginalSaveGame.Read(File.ReadAllBytes(OriginalSave(name)));
            for (var m = 0; m < managers; m++)
            {
                foreach (var slot in Slots(save, m))
                {
                    Assert.Equal(BitConverter.ToInt32(slot, 0x28), Salary(save, slot));
                    checkedSlots++;
                }
            }
        }
        Assert.True(checkedSlots >= 4 * 22, $"nur {checkedSlots} Kaderplätze");

        var bought = OriginalSaveGame.Read(File.ReadAllBytes(OriginalSave("FILE07.HOK")));
        var daffner = Assert.Single(Slots(bought, 0), s => (BitConverter.ToInt32(s, 0x2C) & 0x7FFF_FFFF) == 400_000 * 3);
        Assert.Equal(9_900 * 3, BitConverter.ToInt32(daffner, 0x28));
        Assert.Equal(9_900 * 3, Salary(bought, daffner));
        Assert.Equal(1, daffner[0x0B]); // Laufzeit des neuen Kaderplatzes (0x42CB1)
    }

    /// <summary>
    /// Mehrsaisonprobe (4 Saisonwechsel, passiv und aktiv): aktiv kauft der Manager je Saison einen Spieler ohne Eingaben; der
    /// Vertrag steht zu Wert(Gehalt) des Käufers. Gehälter bleiben mindestens 1 DM, Speichern und Laden ändert nichts.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultiSeason_FourChanges_PurchaseSalary(bool active)
    {
        var game = TestGames.Create(21);
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
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
                    var expected = Contracts.ValueSalary(game, manager, p);
                    if (game.TransferMarket.Buy(game, manager, listing, listing.Price * 2, loan: false) == TransferResult.Accepted)
                    {
                        Assert.Contains(p, club.Players);
                        Assert.Equal(expected, p.MonthlySalary);
                        bought++;
                        boughtThisSeason = true;
                    }
                }
                game.Continue();
                Assert.All(club.Players, p => Assert.True(p.MonthlySalary >= Contracts.MinSalary));
            }
            game.StartNextSeason();
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

    /// <summary>Wert(Gehalt) aus den gespeicherten Eingängen eines Kaderplatzes (Zuordnung wie in <see cref="TransferOriginalTests"/>).</summary>
    private static int Salary(OriginalSaveGame save, byte[] slot)
    {
        var pool = save.Block("far:8253");
        var player = pool.AsSpan(slot[0x0F] * OriginalSaveGame.PoolRecordLength, OriginalSaveGame.PoolRecordLength);
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
        return PlayerValue.Compute(inputs, PlayerValueKind.Salary, (_, _) => throw new InvalidOperationException());
    }

    /// <summary>Spielklasse nach Vereinsindex. [ORIGINAL] em.exe 0x34FD5 mit DGROUP 0x7C1 = 12, 0x7C2 = 24, 0x6AD = 52.</summary>
    private static int OriginalClass(int club) => club < 12 || club > 52 ? 1 : club < 24 ? 2 : 4;

    /// <summary>Original-Spielstand: <c>EM_SAVEGAMES</c> oder <c>spielstaende</c> neben dem aufgelösten <c>gamedata</c>.</summary>
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
        public override int Next(int minValue, int maxValue) => throw new InvalidOperationException();
        public override int Next() => throw new InvalidOperationException();
        public override int Next(int maxValue) => throw new InvalidOperationException();
        public override double NextDouble() => throw new InvalidOperationException();
    }
}
