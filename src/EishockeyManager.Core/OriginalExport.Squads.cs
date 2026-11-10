using EishockeyManager.Formats;

namespace EishockeyManager.Core;

public sealed partial class GameState
{
    /// <summary>Rollenwert im Pool +0x18 je Art. [APPROX] Das Original führt eine Zahl (0 Torhüter, unter 50 Abwehr, sonst Sturm;
    /// FILE01: 25–40 Abwehr, 77/78 Sturm); das Remake nur die Art.</summary>
    private static byte RoleValue(PlayerRole role) => role switch
    {
        PlayerRole.Goalkeeper => 0,
        PlayerRole.Defender => 30,
        _ => 70,
    };

    /// <summary>
    /// Spielerpool far:8253, Kaderplätze 0x5DC3 der Managervereine (Satz 0–3) und der Transfermarkt (Satz 4); Nationalkader (Satz 5) in
    /// <see cref="WriteNational"/>. Umkehrung von <c>ApplySquad</c>, <c>ApplySlot</c> und <c>ApplyPoolAndMarket</c>.
    /// <list type="bullet">
    /// <item>[ORIGINAL] Pool +0x00 Name, +0x12 Talent, +0x14 Alter, +0x15–0x17 Grundwerte, +0x18 Rollenwert, +0x1A Besitzer (0–3 Manager,
    ///   4 Markt, 6 frei), +0x1D Verein (Satzplatz, 0xFF keiner), +0x1F Vorlagen bzw. beim Torhüter Spielminuten.</item>
    /// <item>[ORIGINAL] Kaderplatz: Felder wie <c>ApplySlot</c>; Platz = Kaderfolge (Nr. − 1), Angebote eines Managers stehen im Marktsatz
    ///   mit Pool-Besitzer = Manager (A380, FILE41).</item>
    /// <item>[APPROX] Spieler eines Managerkaders ohne Poolplatz (im Remake nicht beobachtet) bekommen einen freien Poolplatz; ist keiner
    ///   frei, entfallen sie. Pool +0x13, +0x19, +0x1B, +0x1C, +0x1E und Kaderplatz +0x02, +0x1B bleiben 0 (ungedeutet).</item>
    /// </list>
    /// </summary>
    private void WriteSquadsAndPool(OriginalSaveWriter w, int[] slotOf, byte[]? clubDatabase)
    {
        var names = clubDatabase is null ? [] : OriginalExport.DatabasePlayerNames(clubDatabase);
        void WritePoolEntry(byte[] pool, int index, Player p, byte owner, int clubSlot) =>
            GameState.WritePoolEntry(pool, index, p, owner, clubSlot, names);
        var pool = w.Block("far:8253");
        var squad = w.Block("5DC3");
        var indexOf = PoolIndices();
        // [ORIGINAL] Satz 0 ist leer bis auf Besitzer 6 (FILE01).
        pool[0x1A] = 6;
        foreach (var slot in TransferMarket.Pool)
        {
            if (slot.Index is < 1 or >= TransferPoolRules.Size)
                continue;
            var owner = slot.Owner switch
            {
                PoolOwner.Market => OriginalImport.PoolOwnerMarket,
                PoolOwner.Taken => (byte)(ManagerOf(slot.Player) ?? 6),
                _ => (byte)6,
            };
            var club = slot.Owner == PoolOwner.Taken && ClubOf(slot.Player) is { } c ? c.Index : slot.Club;
            WritePoolEntry(pool, slot.Index, slot.Player, owner, club >= 0 && club < slotOf.Length ? slotOf[club] : 0xFF);
        }

        for (var m = 0; m < Managers.Count; m++)
        {
            if (WorldOnly)
                break;
            var club = Clubs[Managers[m].ClubIndex];
            var players = club.Players.ToList();
            for (var i = 0; i < Math.Min(players.Count, OriginalSaveGame.SquadSlotsPerManager); i++)
            {
                var p = players[i];
                if (!indexOf.TryGetValue(p, out var index))
                    continue; // [APPROX] kein Poolplatz mehr frei
                WritePoolEntry(pool, index, p, (byte)m, slotOf[club.Index]);
                var at = m * 0x618 + i * OriginalSaveGame.SquadSlotLength;
                WriteSlot(squad.AsSpan(at, OriginalSaveGame.SquadSlotLength), p, index, slotOf, club);
                var s = squad.AsSpan(at, OriginalSaveGame.SquadSlotLength);
                s[0x0A] = (byte)(club.IsDressed(p) ? 1 : 0);
                if (club.Lineup.FindSlot(p) is { } place)
                    s[0x15] = (byte)((place.Role == PlayerRole.Goalkeeper ? 1 : place.Block + 1) | (place.Role == PlayerRole.Forward ? 0x80 : 0));
            }
        }

        // Transfermarkt: Satz 4 in Listenfolge (+0x0F Poolindex, Werte, +0x1A Sperre, +0x2C Preis).
        var k = 0;
        foreach (var listing in TransferMarket.Listings)
        {
            if (k >= OriginalSaveGame.SquadSlotsPerManager)
                break;
            if (!indexOf.TryGetValue(listing.Player, out var index))
                continue;
            var s = squad.AsSpan(OriginalImport.MarketRecord * 0x618 + k * OriginalSaveGame.SquadSlotLength, OriginalSaveGame.SquadSlotLength);
            WriteSlot(s, listing.Player, index, slotOf, null);
            s[0x1A] = OriginalExport.Byte(listing.RejectedBy);
            BitConverter.GetBytes((int)Math.Min(listing.Price * 3, int.MaxValue)).CopyTo(s[0x2C..]);
            if (listing.Seller is { } seller && Managers.ToList().IndexOf(seller) is var m and >= 0)
                pool[index * OriginalSaveGame.PoolRecordLength + 0x1A] = (byte)m;
            k++;
        }
    }

    /// <summary>
    /// Poolindex je Spieler (Pool des Transfermarkts, Satz 1–199). Im Original stehen die Spieler der Managervereine im Pool
    /// (Besitzer = Manager, FILE05: 25 vergeben). Im Remake-Spiel führt der Pool für den Managerverein eigene Einträge gleichen
    /// Namens (<see cref="TransferMarket.ReservePool"/> nimmt sie nur den Computervereinen weg): Ein Kaderspieler ohne eigenen
    /// Poolplatz bekommt den Platz seines Namens beim eigenen Verein. [APPROX] Fehlt auch der, nimmt er den letzten freien Platz
    /// eines Computervereins (dieser Poolspieler entfällt im Export).
    /// </summary>
    private Dictionary<Player, int> PoolIndices()
    {
        var map = new Dictionary<Player, int>(ReferenceEqualityComparer.Instance);
        foreach (var slot in TransferMarket.Pool)
            if (slot.Index is >= 1 and < TransferPoolRules.Size)
                map.TryAdd(slot.Player, slot.Index);
        if (WorldOnly)
            return map;
        var squadIndex = new HashSet<int>();
        var managed = Managers.Select(m => m.ClubIndex).ToHashSet();
        var missing = new List<Player>();
        foreach (var club in managed.Select(i => Clubs[i]))
            foreach (var p in club.Players)
            {
                if (map.TryGetValue(p, out var own))
                {
                    squadIndex.Add(own);
                    continue;
                }
                var named = TransferMarket.Pool.FirstOrDefault(s => s.Club == club.Index && s.Owner != PoolOwner.Market
                    && s.Player.Name == p.Name && s.Player.Role == p.Role && !squadIndex.Contains(s.Index));
                if (named is not null)
                {
                    map[p] = named.Index;
                    squadIndex.Add(named.Index);
                }
                else
                    missing.Add(p);
            }
        var spare = new Stack<int>(TransferMarket.Pool
            .Where(s => s.Owner == PoolOwner.Free && !managed.Contains(s.Club) && !squadIndex.Contains(s.Index)
                        && TransferMarket.Listings.All(l => l.Player != s.Player))
            .Select(s => s.Index).Order());
        foreach (var p in missing)
            if (spare.TryPop(out var index))
                map[p] = index;
        return map;
    }

    private int? ManagerOf(Player player)
    {
        for (var m = 0; m < Managers.Count; m++)
            if (Clubs[Managers[m].ClubIndex].Players.Contains(player)
                || TransferMarket.Listings.Any(l => l.Player == player && l.Seller == Managers[m]))
                return m;
        return null;
    }

    private Club? ClubOf(Player player) => Clubs.FirstOrDefault(c => c.Players.Contains(player))
        ?? TransferMarket.Listings.FirstOrDefault(l => l.Player == player && l.Seller is not null)?.From;

    private static void WritePoolEntry(byte[] pool, int index, Player p, byte owner, int clubSlot, Dictionary<string, byte[]> names)
    {
        var e = pool.AsSpan(index * OriginalSaveGame.PoolRecordLength, OriginalSaveGame.PoolRecordLength);
        // [ORIGINAL] Namen aus der Datenbank kopiert das Original roh (Ö meist 0x99, BRANNSTRÖM als 0x5C; FILE01); andere Namen in der
        // Schreibweise der Stände (OriginalSaveWriter.Encode). Review 2026-11-10 B2.
        if (names.TryGetValue(p.Name, out var raw))
            raw.AsSpan(0, Math.Min(raw.Length, 0x11)).CopyTo(e);
        else
            OriginalSaveWriter.Name(p.Name, 0x12).CopyTo(e);
        e[0x12] = OriginalExport.Byte(p.Talent);
        e[0x14] = OriginalExport.Byte(p.Age);
        e[0x15] = OriginalExport.Byte(p.Condition);
        e[0x16] = OriginalExport.Byte(p.Technique);
        e[0x17] = OriginalExport.Byte(p.Form);
        e[0x18] = RoleValue(p.Role);
        e[0x1A] = owner;
        e[0x1D] = (byte)clubSlot;
        BitConverter.GetBytes((ushort)Math.Clamp(p.Role == PlayerRole.Goalkeeper ? p.GoalieMinutes : p.Assists, 0, ushort.MaxValue))
            .CopyTo(e[0x1F..]);
    }

    /// <summary>Kaderplatz (0x34 Byte) aus einem Spieler; Umkehrung von <c>ApplySlot</c>.</summary>
    private void WriteSlot(Span<byte> s, Player p, int poolIndex, int[] slotOf, Club? club)
    {
        s[0x00] = OriginalExport.Byte(p.Penalties5);
        s[0x01] = OriginalExport.Byte(p.Penalties2);
        if (p.Role == PlayerRole.Goalkeeper)
        {
            // [APPROX] Gegentore ohne Wettbewerb: alles in die Liga (+0x03); Vereinswert +0x22 bleibt 0 (FILE05, FILE41).
            s[0x03] = OriginalExport.Byte(p.GoalsAgainst);
        }
        else
        {
            s[0x03] = OriginalExport.Byte(p.Goals - p.CupGoals - p.EuropeGoals);
            s[0x04] = OriginalExport.Byte(p.CupGoals);
            s[0x05] = OriginalExport.Byte(p.EuropeGoals);
            BitConverter.GetBytes((short)(s[0x03] + p.ClubGoalsCarry)).CopyTo(s[0x22..]);
            BitConverter.GetBytes((short)(s[0x04] + p.ClubCupGoalsCarry)).CopyTo(s[0x24..]);
            BitConverter.GetBytes((short)(s[0x05] + p.ClubEuropeGoalsCarry)).CopyTo(s[0x26..]);
        }
        s[0x06] = OriginalExport.Byte(p.Games - p.CupGames - p.EuropeGames);
        s[0x07] = OriginalExport.Byte(p.CupGames);
        s[0x08] = OriginalExport.Byte(p.EuropeGames);
        BitConverter.GetBytes((short)(s[0x06] + p.ClubGamesCarry)).CopyTo(s[0x1C..]);
        BitConverter.GetBytes((short)(s[0x07] + p.ClubCupGamesCarry)).CopyTo(s[0x1E..]);
        BitConverter.GetBytes((short)(s[0x08] + p.ClubEuropeGamesCarry)).CopyTo(s[0x20..]);
        var flags = 0;
        if (p.InjuredGames > 0)
        {
            flags |= 0x02;
            s[0x0D] = OriginalExport.Byte(p.InjuredGames);
            s[0x17] = OriginalExport.Byte(p.InjuryKind);
        }
        if (p.UnsolicitedOffer is { } offer && club is not null)
        {
            flags |= 0x40;
            s[0x16] = (byte)slotOf[offer.Index];
        }
        s[0x09] = (byte)flags;
        s[0x0B] = OriginalExport.Byte(p.ContractYears);
        if (club is not null && p.OnLoan && p.Loan is { } loan)
            s[0x0C] = (byte)(0x80 | (slotOf[loan.Origin.Index] & 0x7F));
        s[0x0E] = OriginalExport.Byte(p.TrendScore);
        s[0x0F] = (byte)poolIndex;
        s[0x10] = OriginalExport.Byte(p.Condition);
        s[0x11] = OriginalExport.Byte(p.Technique);
        s[0x12] = OriginalExport.Byte(p.Form);
        s[0x13] = OriginalExport.Byte(p.Strain);
        s[0x14] = OriginalExport.Byte(p.TrendControl);
        s[0x18] = p.Notice switch
        {
            ContractNotice.Retiring => 0x80,
            ContractNotice.Offered => OriginalExport.Byte(100 + p.NoticeYears),
            _ => OriginalExport.Byte(p.NegotiationLock),
        };
        if (CurrentDay > 0 || Season > FirstSeason)
            s[0x1B] = 90; // [APPROX] +0x1A/+0x1B ungedeutet: nach dem ersten Tag meist 0/90, sonst 0x80/Wert (FILE05, FILE41)
        s[0x19] = (byte)((p.HiddenSkill1 & 0x0F) | ((p.HiddenSkill2 & 0x0F) << 4));
        BitConverter.GetBytes((int)Math.Min(p.MonthlySalary * 3L, int.MaxValue)).CopyTo(s[0x28..]);
        var price = (uint)Math.Min(p.PurchasePrice * 3L, 0x7FFF_FFFF) | (p.InsuranceLocked ? 0x8000_0000u : 0);
        BitConverter.GetBytes(price).CopyTo(s[0x2C..]);
        BitConverter.GetBytes((int)Math.Min(p.InsurancePremium * 3L, int.MaxValue)).CopyTo(s[0x30..]);
    }
}
