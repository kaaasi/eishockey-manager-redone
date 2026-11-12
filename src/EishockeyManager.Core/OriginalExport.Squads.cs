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
    /// <item>[ORIGINAL] Spur pool (#743): Jeder Spieler eines Managerkaders steht auf seinem Poolplatz (<see cref="TransferMarket.SeatManagerSquads"/>);
    ///   die frühere Ersatzregel (freier Ligaplatz, dessen Poolspieler entfällt) gibt es nicht mehr. Pool +0x13, +0x19, +0x1B, +0x1C,
    ///   +0x1E und Kaderplatz +0x02, +0x1B bleiben 0 (ungedeutet).</item>
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
                // Spur pool, Teil 3: ohne Spieler im Managerkader der gemerkte Manager (verloren, „nur Weltkugel“), sonst 6.
                PoolOwner.Taken => (byte)(ManagerOf(slot.Player) ?? (slot.Manager is >= 0 and < 4 ? slot.Manager : 6)),
                _ => (byte)6,
            };
            var club = slot.Owner == PoolOwner.Taken && ClubOf(slot.Player) is { } c ? c.Index : slot.Club;
            WritePoolEntry(pool, slot.Index, slot.Player, owner, club >= 0 && club < slotOf.Length ? slotOf[club] : 0xFF);
        }

        var marketTwins = TransferMarket.Pool
            .Where(s => s.Owner == PoolOwner.Market && indexOf.Any(e => e.Value == s.Index && !ReferenceEquals(e.Key, s.Player)))
            .Select(s => s.Index).ToHashSet();
        // #732: Kader von Satz 0 auch im Modus „nur Weltkugel“ (Gegenstück zum Import, Rundreise FILE18–FILE20, FILE47–FILE49A).
        for (var m = 0; m < Managers.Count; m++)
        {
            var club = Clubs[Managers[m].ClubIndex];
            var players = club.Players.ToList();
            for (var i = 0; i < Math.Min(players.Count, OriginalSaveGame.SquadSlotsPerManager); i++)
            {
                var p = players[i];
                if (!indexOf.TryGetValue(p, out var index))
                    continue; // [APPROX] kein Poolplatz mehr frei
                // Ein Marktsatz gleichen Namens bleibt Marktsatz (FILE41: Pool 179 Besitzer 4 zugleich Kaderplatz 24).
                if (!marketTwins.Contains(index))
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
    /// Poolindex je Spieler (Pool des Transfermarkts, Satz 1–199). [ORIGINAL] Die Spieler der Managervereine stehen im Pool (Besitzer =
    /// Manager, FILE01/05: 25 vergeben); im Remake seit Spur pool (#743) ebenso (<see cref="TransferMarket.SeatManagerSquads"/>), jeder
    /// Index gehört genau einem Spieler (R727P-04). Einzige Ausnahme: ein Kaderspieler, dessen Poolsatz gleichen Namens und gleicher Art
    /// im Original auf dem Markt blieb (Import, FILE41 Kaderplatz 24 → Pool 179, Besitzer 4) – derselbe Spieler, der Kaderplatz verweist
    /// auf diesen Satz, der Satz bleibt Marktsatz. [APPROX] Ein Kaderspieler ohne Platz (nur denkbar, wenn beim Laden eines Altstands
    /// kein Ligaplatz seiner Art frei war) entfällt im Export.
    /// </summary>
    private Dictionary<Player, int> PoolIndices()
    {
        var map = new Dictionary<Player, int>(ReferenceEqualityComparer.Instance);
        foreach (var slot in TransferMarket.Pool)
            if (slot.Index is >= 1 and < TransferPoolRules.Size)
                map.TryAdd(slot.Player, slot.Index);
        var managed = Managers.Select(m => m.ClubIndex).ToHashSet();
        var missing = new List<Player>();
        foreach (var club in managed.Select(i => Clubs[i]))
            foreach (var p in club.Players.Where(p => !map.ContainsKey(p)))
            {
                var twin = TransferMarket.Pool.FirstOrDefault(s => s.Owner != PoolOwner.Taken && s.Player.Name == p.Name
                                                                   && s.Player.Role == p.Role && s.Index < TransferPoolRules.Size);
                if (twin is not null && !map.Any(e => e.Value == twin.Index && club.Players.Contains(e.Key)))
                    map[p] = twin.Index;
                else
                    missing.Add(p);
            }
        if (missing.Count > 0)
            AddUnseatedSquadPlayers(map, managed, missing);
        return map;
    }

    /// <summary>
    /// [APPROX] Kaderspieler ohne Poolplatz: Im Modus „nur Weltkugel“ setzt das Remake den Kader von Satz 0 nicht in den Pool
    /// (<see cref="TransferMarket.SeatManagerSquads"/>), der Export schreibt ihn aber (#732). Wie vor Spur pool bekommt ein solcher
    /// Spieler den Platz seines Namens beim eigenen Verein, sonst den ersten freien Ligaplatz seiner Art (kein Markt, nicht vergeben,
    /// kein Managerverein, nicht der erste Platz der Art; FILE01/05/09: Managerkader ab Torhüter 2, Verteidiger 22, Stürmer 98) –
    /// dessen Poolspieler entfällt im Export. Ist keiner frei, entfällt der Spieler. Jeder Index gehört genau einem Spieler (R727P-04).
    /// </summary>
    private void AddUnseatedSquadPlayers(Dictionary<Player, int> map, HashSet<int> managed, List<Player> missing)
    {
        var layout = TransferPoolLayout.Original;
        var (defenders, forwards) = layout.LeagueRoleStarts;
        var listed = TransferMarket.Listings.Select(l => l.Player).ToHashSet(ReferenceEqualityComparer.Instance);
        var used = managed.SelectMany(i => Clubs[i].Players).Where(map.ContainsKey).Select(p => map[p]).ToHashSet();
        bool Free(PoolSlot s) => !used.Contains(s.Index);
        var rest = new List<Player>();
        foreach (var p in missing)
        {
            var club = Clubs.First(c => managed.Contains(c.Index) && c.Players.Contains(p));
            var named = TransferMarket.Pool.FirstOrDefault(s => s.Club == club.Index && s.Owner != PoolOwner.Market
                && s.Player.Name == p.Name && s.Player.Role == p.Role && s.Index < TransferPoolRules.Size && Free(s));
            if (named is null)
            {
                rest.Add(p);
                continue;
            }
            map.Remove(named.Player); // sein Poolspieler entfällt im Export
            map[p] = named.Index;
            used.Add(named.Index);
        }
        var spare = TransferMarket.Pool
            .Where(s => s.Owner == PoolOwner.Free && s.Index <= layout.LeagueLast && !managed.Contains(s.Club) && Free(s)
                        && !listed.Contains(s.Player))
            .Select(s => s.Index)
            .OrderBy(i => i is 1 || i == defenders || i == forwards ? 1 : 0).ThenBy(i => i)
            .ToList();
        foreach (var p in rest)
        {
            var at = spare.FindIndex(i => layout.RoleOf(i) == p.Role);
            if (at < 0)
                continue;
            var index = spare[at];
            spare.RemoveAt(at);
            foreach (var e in map.Where(e => e.Value == index).ToList())
                map.Remove(e.Key);
            map[p] = index;
            used.Add(index);
        }
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
        // Das ganze Feld schreiben (Name, Endnull, Rest 0), sonst bleibt ein vorher hier geschriebener Name stehen (R727P-04).
        e[..0x12].Clear();
        if (names.TryGetValue(p.Name, out var raw))
            raw.AsSpan(0, Math.Min(raw.Length, 0x11)).CopyTo(e);
        else
            OriginalSaveWriter.Name(p.Name, 0x12).CopyTo(e);
        e[0x12] = OriginalExport.Byte(p.Talent);
        e[0x14] = OriginalExport.Byte(p.Age);
        e[0x15] = OriginalExport.Byte(p.Condition);
        e[0x16] = OriginalExport.Byte(p.Technique);
        e[0x17] = OriginalExport.Byte(p.PoolForm ?? p.Form); // A066 (kader-rest)
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
