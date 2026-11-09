using System.Buffers.Binary;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur hok-import, Teil 4b (E14-4 = A): Spielerpool far:8253, Transfermarkt (Kaderplätze 5DC3 Satz 4), Nationalkader
/// (Satz 5, Taktik des Bundestrainers) und Nachrichten D294 kommen aus dem HOK statt aus dem Nachspielen. Werte je Feld:
/// Feldkarte in <c>docs/formate/spielstand-original.md</c>, Abschnitt „Pool, Markt, Nationalkader, Nachrichten (Teil 4b)“.
/// </summary>
public sealed class OriginalImportPoolTests
{
    private const int Seed = 4711;
    private const int SquadRecord = 0x618;

    /// <summary>Pool FILE05: 199 Plätze mit Name, Alter, Talent, Grundwerten (+0x15–0x17), Verein (+0x1D, Regel 1) und Besitzer (+0x1A).</summary>
    [GameDataFact]
    public void Import_PoolFromOriginal()
    {
        var (save, game, clubOf) = Import("FILE05.HOK");
        var pool = save.Block("far:8253");
        var slots = game.TransferMarket.Pool;
        Assert.Equal(TransferPoolRules.Size - 1, slots.Count);
        var squad = game.Clubs[game.Managers[0].ClubIndex].Players;
        for (var i = 1; i < TransferPoolRules.Size; i++)
        {
            var e = pool.AsSpan(i * OriginalSaveGame.PoolRecordLength, OriginalSaveGame.PoolRecordLength);
            var slot = slots[i - 1];
            Assert.Equal(i, slot.Index);
            Assert.Equal(Name(e[..0x12]), slot.Player.Name);
            Assert.Equal((e[0x14], e[0x12]), ((byte)slot.Player.Age, (byte)slot.Player.Talent));
            Assert.Equal(clubOf[e[0x1D]], slot.Club);
            Assert.Equal(e[0x18] == 0 ? PlayerRole.Goalkeeper : e[0x18] < 50 ? PlayerRole.Defender : PlayerRole.Forward, slot.Player.Role);
            switch (e[0x1A])
            {
                case 0:
                    Assert.Equal(PoolOwner.Taken, slot.Owner);
                    Assert.Contains(slot.Player, squad); // Kaderspieler des Managers ist derselbe Spieler
                    break;
                case 4:
                    Assert.Equal(PoolOwner.Market, slot.Owner);
                    break;
                default:
                    Assert.Equal(PoolOwner.Free, slot.Owner);
                    Assert.Equal((e[0x15], e[0x16], e[0x17]), ((byte)slot.Player.Condition, (byte)slot.Player.Technique, (byte)slot.Player.Form));
                    break;
            }
        }
        Assert.Equal(25, slots.Count(s => s.Owner == PoolOwner.Taken));
    }

    /// <summary>Transferliste FILE05: sieben Markt-Plätze aus Satz 4 in Folge, Preis +0x2C, Werte +0x10–0x12, Herkunft = Poolverein.</summary>
    [GameDataFact]
    public void Import_MarketFromOriginal()
    {
        var (save, game, clubOf) = Import("FILE05.HOK");
        var squad = save.Block("5DC3");
        var pool = save.Block("far:8253");
        var listings = game.TransferMarket.Listings;
        Assert.Equal(["KÜNAST", "STARTSCHEW", "WURTINGER", "KJELLBERG", "DAFFNER", "KASPERCZYK", "D.HEGEN"],
            listings.Select(l => l.Player.Name));
        for (var k = 0; k < listings.Count; k++)
        {
            var s = squad.AsSpan(4 * SquadRecord + k * OriginalSaveGame.SquadSlotLength, OriginalSaveGame.SquadSlotLength);
            var l = listings[k];
            Assert.True(l.FromPool);
            Assert.Null(l.Seller);
            Assert.Equal(BinaryPrimitives.ReadInt32LittleEndian(s[0x2C..]) / 3, l.Price);
            Assert.Equal((s[0x10], s[0x11], s[0x12]), ((byte)l.Player.Condition, (byte)l.Player.Technique, (byte)l.Player.Form));
            Assert.Equal(game.Clubs[clubOf[pool[s[0x0F] * OriginalSaveGame.PoolRecordLength + 0x1D]]], l.From);
            Assert.Same(game.TransferMarket.Pool[s[0x0F] - 1].Player, l.Player);
        }
        Assert.Equal(435_000L, listings[0].Price);
        Assert.Equal("SCHWEDEN", listings[3].From.Name);
        Assert.Empty(game.TransferMarket.Bids);
        Assert.Empty(game.TransferMarket.FreeAgents);
        Assert.Equal(game.PlayedMatchDays, game.TransferMarket.UpdatedDay);
    }

    /// <summary>
    /// FILE41: HOHENADL (Pool 28, Besitzer 0) steht auf dem Markt-Platz 7 – Angebot des Managers, Spieler bleibt im Kader
    /// (Feldbelege L7, A380); der Rest des aufgerückten Satzes (Platz 11 mit Poolindex 0) ist kein Eintrag.
    /// </summary>
    [GameDataFact]
    public void Import_MarketListingOfManager()
    {
        var (_, game, _) = Import("FILE41.HOK");
        var listings = game.TransferMarket.Listings;
        Assert.Equal(10, listings.Count);
        var own = listings[6];
        Assert.Equal("HOHENADL", own.Player.Name);
        Assert.Same(game.Managers[0], own.Seller);
        Assert.False(own.FromPool);
        Assert.Equal(316_000L, own.Price);
        var club = game.Clubs[game.Managers[0].ClubIndex];
        Assert.Same(club, own.From);
        Assert.Contains(own.Player, club.Players);
        Assert.Same(own, game.TransferMarket.ListingOf(own.Player));
        Assert.All(listings.Where(l => l != own), l => Assert.True(l.FromPool));
    }

    /// <summary>
    /// Jeder Name nur einmal im Spiel (Entscheidung Nutzer 01.11.2026): Poolnamen stehen in keinem Computerkader, die Kader
    /// behalten die Mindestbesetzung, IDs und Verweise sind gültig; danach ein Spieltag über die Zeitleiste.
    /// </summary>
    [GameDataTheory]
    [InlineData("FILE05.HOK")]
    [InlineData("FILE36.HOK")]
    [InlineData("FILE41.HOK")]
    public void Import_PoolNamesUnique_AndContinue(string file)
    {
        var (_, game, _) = Import(file);
        var poolNames = game.TransferMarket.Pool.Where(s => s.Owner != PoolOwner.Taken).Select(s => s.Player.Name).ToHashSet();
        var managed = game.Managers.Select(m => m.ClubIndex).ToHashSet();
        foreach (var club in game.Clubs.Where(c => !managed.Contains(c.Index)))
            Assert.DoesNotContain(club.Players, p => poolNames.Contains(p.Name));
        foreach (var club in game.Clubs.Where(c => !managed.Contains(c.Index) && c.League is not null))
        {
            Assert.True(club.Players.Count(p => p.Role == PlayerRole.Goalkeeper) >= TransferMarket.MinGoalkeepers, club.Name);
            Assert.True(club.Players.Count(p => p.Role == PlayerRole.Defender) >= TransferMarket.MinDefenders, club.Name);
            Assert.True(club.Players.Count(p => p.Role == PlayerRole.Forward) >= TransferMarket.MinForwards, club.Name);
        }
        Assert.Empty(game.ValidatePlayerIdentity());
        TestSeason.PlayMatchDay(game);
        Assert.Empty(game.ValidatePlayerIdentity());
    }

    /// <summary>
    /// Nationalkader FILE18 („nur Weltkugel“, Bundestrainer): 29 Spieler aus Satz 5 in Platzfolge, Werte +0x10–0x12, Reihen
    /// (+0x15, Bit 0x80 Sturm), Eingesetzte (+0x0A); Taktik des Satzes 5 (A6FB/A707/A701 + 5, CAE9 + 0xF0, CC43 + 0x28).
    /// </summary>
    [GameDataFact]
    public void Import_NationalSquadFromOriginal()
    {
        var (save, game, _) = Import("FILE18.HOK");
        var coach = Assert.IsType<NationalCoach>(game.NationalCoach);
        var squad = save.Block("5DC3");
        var pool = save.Block("far:8253");
        var slots = Enumerable.Range(0, 29)
            .Select(i => squad.AsSpan(5 * SquadRecord + i * OriginalSaveGame.SquadSlotLength, OriginalSaveGame.SquadSlotLength).ToArray())
            .ToList();
        Assert.Equal(slots.Select(s => Name(pool.AsSpan(s[0x0F] * OriginalSaveGame.PoolRecordLength, 0x12))),
            coach.Squad.Select(m => m.Player.Name));
        for (var i = 0; i < slots.Count; i++)
        {
            var (s, p) = (slots[i], coach.Squad[i].Player);
            Assert.Equal((s[0x10], s[0x11], s[0x12]), ((byte)p.Condition, (byte)p.Technique, (byte)p.Form));
            Assert.Equal(s[0x0A] != 0, coach.IsDressed(p));
            if ((s[0x15] & 0x0F) is >= 1 and <= 3)
                Assert.True(coach.Formation.Contains(p), $"{p.Name} (Reihe {s[0x15] & 0x0F}) fehlt in der Aufstellung");
            else if (p.Role != PlayerRole.Goalkeeper)
                Assert.False(coach.Formation.Contains(p));
        }
        Assert.Equal("HEISS", coach.Formation.Goalkeeper?.Name);
        Assert.Equal(new[] { "ZERWESZ", "JOORIS" }, coach.Formation.Block(0).Where(p => p.Role == PlayerRole.Forward).Select(p => p.Name).Take(2));
        // KÜNAST (Besitzer 0, Kader des Managers) und MERK (Markt) sind dieselben Spieler wie im Pool bzw. auf der Liste.
        Assert.Contains(game.TransferMarket.Listings, l => l.Player == coach.Squad[1].Player);
        Assert.Empty(game.ValidatePlayerIdentity());

        var tactics = coach.Tactics;
        var values = save.Block("CAE9").AsSpan(0x30 * 5, 0x30).ToArray();
        Assert.Equal(Enumerable.Range(0, 6).Select(i => (int)values[i]), Enumerable.Range(0, 6).Select(i => tactics.Effort(0, (ScoreSituation)i)));
        var minutes = save.Block("CC43").AsSpan(8 * 5, 8).ToArray();
        Assert.Equal(Enumerable.Range(0, 4).Select(l => (int)minutes[l]), Enumerable.Range(0, 4).Select(l => tactics.Minutes(l, false)));
    }

    /// <summary>
    /// Nachrichten D294 (24 × 0x71): FILE20 zwei Meldungen an Manager 0 mit Text aus dem Satz (+3, Zeilen mit „^“), Restlaufzeit
    /// oberes Halbbyte +0 = 3, Tag +1; FILE05 ohne Nachricht (die Meldung aus dem Nachspielen entfällt, Sichtvergleich Teil 3).
    /// </summary>
    [GameDataFact]
    public void Import_MessagesFromOriginal()
    {
        var (_, game, _) = Import("FILE20.HOK");
        var inbox = game.Inbox(game.Managers[0]);
        Assert.Equal(2, inbox.Count);
        Assert.Equal(["11. April 1994", "Verletzung außerhalb des Spieles:", "ABSTREITER  (Achillessehnenriß)"], inbox[0].Lines);
        Assert.Equal("PURVES  (Handrückenbruch)", inbox[1].Lines[2]);
        Assert.All(inbox, m => Assert.Equal((MessageKind.Info, 3, (Player?)null, false), (m.Kind, m.DaysLeft, m.Player, m.Done)));
        Assert.Equal([216, 220], inbox.Select(m => m.Day));
        Assert.All(inbox, m => Assert.Equal(game.Season, m.Season));
        Assert.Equal(2, game.MessagesStored);

        var (_, file05, _) = Import("FILE05.HOK");
        Assert.Empty(file05.Inbox(file05.Managers[0]));
    }

    /// <summary>Nachricht zu einem Poolspieler (Bit 3 frei, +2 = Poolindex; an keinem Stand belegt, gepatcht): Spieler des Pools.</summary>
    [GameDataFact]
    public void Import_MessageAboutPoolPlayer()
    {
        var (save, database) = Load("FILE05.HOK");
        var block = save.Block("D294");
        block[0] = 0x01; // Manager 1 gibt es nicht → verworfen
        block[0x71] = 0x30; // Manager 0, mit Spieler
        block[0x71 + 1] = 11;
        block[0x71 + 2] = 91; // STARTSCHEW
        "EC RATINGEN ist an^STARTSCHEW interessiert."u8.CopyTo(block.AsSpan(0x71 + 3));
        var game = GameState.ImportOriginal(save, database, Seed);
        var message = Assert.Single(game.Inbox(game.Managers[0]));
        Assert.Same(game.TransferMarket.Pool[90].Player, message.Player);
        Assert.Equal(["EC RATINGEN ist an", "STARTSCHEW interessiert."], message.Lines);
        Assert.Equal(10, message.Day);
    }

    /// <summary>Rundreise mit Pool, Markt, Nationalkader und Nachrichten: Remake-Spielstand speichern, laden, weiterspielen.</summary>
    [GameDataTheory]
    [InlineData("FILE18.HOK")]
    [InlineData("FILE20.HOK")]
    [InlineData("FILE41.HOK")]
    public void Import_RoundTripWithPoolMarketNational(string file)
    {
        var (_, game, _) = Import(file);
        using var stream = new MemoryStream();
        SaveGame.Save(game, stream);
        var loaded = SaveGame.Load(stream.ToArray());
        Assert.True(loaded.Success, loaded.Error);
        var copy = loaded.Game!;
        Assert.Equal(game.TransferMarket.Pool.Select(s => (s.Index, s.Player.Id, s.Owner, s.Club)),
            copy.TransferMarket.Pool.Select(s => (s.Index, s.Player.Id, s.Owner, s.Club)));
        Assert.Equal(game.TransferMarket.Listings.Select(l => (l.Player.Id, l.Price, l.Seller?.Name)),
            copy.TransferMarket.Listings.Select(l => (l.Player.Id, l.Price, l.Seller?.Name)));
        Assert.Equal(game.NationalCoach?.Squad.Select(m => m.Player.Id), copy.NationalCoach?.Squad.Select(m => m.Player.Id));
        Assert.Equal(game.Inbox(game.Managers[0]).Select(m => string.Join("|", m.Lines)),
            copy.Inbox(copy.Managers[0]).Select(m => string.Join("|", m.Lines)));
        if (copy.Mode == GameMode.WorldOnly)
            copy.Continue();
        else
            TestSeason.PlayMatchDay(copy);
        Assert.Empty(copy.ValidatePlayerIdentity());
    }

    private static string Name(ReadOnlySpan<byte> raw)
    {
        var end = raw.IndexOf((byte)0);
        return GameText.Decode(end < 0 ? raw : raw[..end]).TrimEnd();
    }

    private static (OriginalSaveGame Save, GameState Game, int[] ClubOf) Import(string file)
    {
        var (save, database) = Load(file);
        return (save, GameState.ImportOriginal(save, database, Seed), OriginalImport.ClubIndices(save, database));
    }

    private static (OriginalSaveGame Save, ClubDatabase Database) Load(string file) =>
        (OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave(file))), ClubDatabase.Load(TestGameData.Load()));
}
