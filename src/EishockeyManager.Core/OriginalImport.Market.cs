using System.Buffers.Binary;
using EishockeyManager.Formats;

namespace EishockeyManager.Core;

/// <summary>
/// Import eines Original-Spielstands, Teil 4b (Spur hok-import, Runde 14, E14-4 = A): Spielerpool far:8253 und Transferliste
/// (Kaderplätze DGROUP 0x5DC3, Satz 4). Fundstellen je Feld in der Feldkarte (<c>docs/formate/spielstand-original.md</c>,
/// Abschnitt „Pool, Markt, Nationalkader, Nachrichten (Teil 4b)“).
/// </summary>
public static partial class OriginalImport
{
    /// <summary>Kaderplatz-Satz des Transfermarkts im Block 0x5DC3 (0–3 Manager, 4 Markt, 5 Bundestrainer).</summary>
    public const int MarketRecord = 4;

    /// <summary>Kaderplatz-Satz des Bundestrainers (Nationalkader).</summary>
    public const int NationalRecord = 5;

    /// <summary>[ORIGINAL] Pool +0x1A: 0–3 Manager, 4 Transfermarkt, 6 frei (FILE01–FILE49A; transfer.md).</summary>
    public const byte PoolOwnerMarket = 4;

    /// <summary>Kaderplatz <paramref name="i"/> des Satzes <paramref name="record"/> im Block 0x5DC3.</summary>
    public static ReadOnlySpan<byte> SquadSlot(OriginalSaveGame save, int record, int i) =>
        save.Block("5DC3").AsSpan(record * 0x618 + i * OriginalSaveGame.SquadSlotLength, OriginalSaveGame.SquadSlotLength);

    /// <summary>Poolsatz <paramref name="index"/> (0–199) im Block far:8253.</summary>
    public static ReadOnlySpan<byte> PoolEntry(OriginalSaveGame save, int index) =>
        save.Block("far:8253").AsSpan(index * OriginalSaveGame.PoolRecordLength, OriginalSaveGame.PoolRecordLength);

    /// <summary>Art aus dem Rollenwert +0x18 (0 Torhüter, unter 50 Abwehr, sonst Sturm; Feldkarte „Player.Role“).</summary>
    public static PlayerRole RoleOf(byte roleValue) =>
        roleValue == 0 ? PlayerRole.Goalkeeper : roleValue < 50 ? PlayerRole.Defender : PlayerRole.Forward;
}

public sealed partial class TransferMarket
{
    /// <summary>
    /// Übernahme aus dem Import (Teil 4b): Pool und Liste ersetzen die des Nachspielens. Gebote und Vertragslose verwirft der
    /// Import (im HOK nicht belegt, Feldkarte U); der Stand gilt als am Tag <paramref name="playedMatchDays"/> fortgeschrieben.
    /// </summary>
    internal void ImportOriginal(IEnumerable<PoolSlot> pool, IEnumerable<TransferListing> listings, int playedMatchDays)
    {
        _pool.Clear();
        _pool.AddRange(pool);
        _listings.Clear();
        _listings.AddRange(listings);
        _bids.Clear();
        _freeAgents.Clear();
        UpdatedDay = playedMatchDays;
    }
}

public sealed partial class GameState
{
    /// <summary>
    /// Pool und Transferliste. [ORIGINAL] Poolsatz far:8253 + 33 · i (i = 1–199): +0x00 Name, +0x12 Talent, +0x14 Alter,
    /// +0x15–0x17 Grundwerte, +0x18 Rollenwert, +0x1A Besitzer, +0x1D Verein (Regel 1); Markt = Satz 4 der Kaderplätze in
    /// Platzfolge, +0x0F Poolindex, +0x10–0x14 Werte, +0x19 verdeckte Werte, +0x1A Sperre, +0x2C Preis (Drittel-DM).
    /// <list type="bullet">
    /// <item>Besitzer 0–3 mit Platz in einem übernommenen Managerkader: derselbe Spieler, vergeben. [APPROX] Ohne übernommenen
    ///   Kader („nur Weltkugel“) der gleichnamige Spieler des Managervereins, sonst frei. Besitzer 0–3 ohne Kaderplatz (mit
    ///   übernommenem Kader): nach Vertragsende verlorener Spieler, Platz bleibt vergeben (#749, R15-E3 = C, FILE46 KJELLBERG).</item>
    /// <item>Marktplatz mit Besitzer 4: Angebot aus dem Pool (<see cref="TransferListing.FromPool"/>); mit Besitzer 0–3: Angebot
    ///   dieses Managers, der Spieler steht nur auf dem Markt-Platz, nicht im Kader (Feldbelege L7; A380 wie
    ///   <see cref="TransferMarket.Offer"/>, #676). [APPROX] Ein Platz mit Poolindex 0 ist der Rest des
    ///   aufgerückten Satzes (FILE41 Platz 11) und kein Eintrag; Vertragslose (Preis 0) sind an keinem Stand belegt und gelten
    ///   wie Poolangebote.</item>
    /// <item>[APPROX] Gleichnamige Spieler gleicher Art aus dem Pool des Nachspielens behalten Objekt und ID. Jeder Name nur
    ///   einmal im Spiel (Entscheidung Nutzer 01.11.2026): ein Poolname im Kader eines Computervereins weicht dort einem nicht
    ///   mehr gebrauchten Poolspieler des Nachspielens gleicher Art (wie der Tausch beim Umbenennen), sonst einem neuen Spieler mit
    ///   seinen Werten und einem freien Datenbanknamen; die Kader behalten ihre Größe.</item>
    /// <item>[APPROX] Marktwerte wie beim Listenwechsel des Remakes: Vertrag und Gehalt 0 (<see cref="TransferMarket.ChangeList"/>),
    ///   Verein +0x1D außerhalb der 150 Sätze → Verein 0 (an keinem Stand beobachtet).</item>
    /// </list>
    /// </summary>
    /// <returns>Spieler je Poolindex 1–199.</returns>
    private Dictionary<int, Player> ApplyPoolAndMarket(OriginalSaveGame save, int[] clubOf, Dictionary<int, Player> squadPlayers,
        HashSet<string> foreigners)
    {
        var market = TransferMarket;
        var spare = market.Pool.Where(s => s.Owner != PoolOwner.Taken).Select(s => s.Player).ToList();
        var byName = spare.GroupBy(p => (p.Name, p.Role)).ToDictionary(g => g.Key, g => g.First());
        var slots = new List<PoolSlot>();
        var players = new Dictionary<int, Player>();
        for (var i = 1; i < TransferPoolRules.Size; i++)
        {
            var e = OriginalImport.PoolEntry(save, i);
            var name = CName(e[..0x12]);
            var role = OriginalImport.RoleOf(e[0x18]);
            var owner = e[0x1A];
            var club = e[0x1D] < clubOf.Length ? clubOf[e[0x1D]] : 0;
            Player? player = null;
            var taken = false;
            if (owner < Managers.Count)
            {
                player = squadPlayers.GetValueOrDefault(i)
                         ?? Clubs[Managers[owner].ClubIndex].Players.FirstOrDefault(p => p.Name == name && p.Role == role);
                // [ORIGINAL] #749, R15-E3 = C: Besitzer 0–3 ohne Kader- und Marktplatz ist ein nach Vertragsende verlorener Spieler
                // (Datei 0x27DE3–0x27E9D schreibt +0x1A nicht zurück); der Platz bleibt vergeben, 0x494EF zieht nur Besitzer 6
                // (FILE46 Pool 196 KJELLBERG). #732: auch im Modus „nur Weltkugel“ (Kader von Satz 0 übernommen, ApplySquad).
                taken = true;
            }
            if (player is null)
            {
                if (byName.Remove((name, role), out var reused))
                    player = reused;
                else
                {
                    player = new Player { Name = name, Role = role, IsForeigner = foreigners.Contains(name) };
                    AssignPlayerId(player);
                }
                player.Age = e[0x14];
                player.Talent = e[0x12];
                player.Condition = e[0x15];
                player.Technique = e[0x16];
                player.Form = e[0x17];
                player.ContractYears = 0;
                player.MonthlySalary = 0;
            }
            players[i] = player;
            slots.Add(new PoolSlot
            {
                Index = i,
                Player = player,
                Club = club,
                Owner = taken ? PoolOwner.Taken : owner == OriginalImport.PoolOwnerMarket ? PoolOwner.Market : PoolOwner.Free,
            });
        }

        var listings = new List<TransferListing>();
        for (var k = 0; k < OriginalSaveGame.SquadSlotsPerManager; k++)
        {
            var s = OriginalImport.SquadSlot(save, OriginalImport.MarketRecord, k);
            int index = s[0x0F];
            if (index == 0 || index >= TransferPoolRules.Size)
                continue;
            var e = OriginalImport.PoolEntry(save, index);
            var slot = slots[index - 1];
            var price = (BinaryPrimitives.ReadUInt32LittleEndian(s[0x2C..]) & 0x7FFF_FFFF) / 3;
            if (slot.Owner == PoolOwner.Market)
            {
                var p = slot.Player;
                ApplySlot(p, s, e, clubOf, club: null);
                p.ContractYears = 0;
                p.MonthlySalary = 0;
                p.PurchasePrice = 0;
                p.InsurancePremium = 0;
                p.InsuranceLocked = false;
                listings.Add(new TransferListing
                {
                    Player = p, From = Clubs[slot.Club], Price = price, ListedDay = PlayedMatchDays, FromPool = true, RejectedBy = s[0x1A],
                });
            }
            else if (slot.Owner == PoolOwner.Taken && e[0x1A] < Managers.Count
                     && Clubs[Managers[e[0x1A]].ClubIndex] is var own
                     && (squadPlayers.GetValueOrDefault(index) == slot.Player || own.Players.Contains(slot.Player)))
                listings.Add(new TransferListing
                {
                    Player = slot.Player, From = own, Price = price, Seller = Managers[e[0x1A]], ListedDay = PlayedMatchDays,
                    RejectedBy = s[0x1A],
                });
        }

        // Jeder Name nur einmal: Poolnamen aus den Computerkadern nehmen, Ersatz aus den übrigen Poolspielern des Nachspielens.
        var names = slots.Select(s => s.Player.Name)
            .Concat(Managers.SelectMany(m => Clubs[m.ClubIndex].Players).Select(p => p.Name)).ToHashSet(StringComparer.Ordinal);
        // Vergebene Plätze ohne Managerkader (verlorene Spieler, #749) zählen mit: ihr Name gehört dem Platz.
        var managerPlayers = Managers.SelectMany(m => Clubs[m.ClubIndex].Players).ToHashSet(ReferenceEqualityComparer.Instance);
        var poolNames = slots.Where(s => s.Owner != PoolOwner.Taken || !managerPlayers.Contains(s.Player))
            .Select(s => s.Player.Name).ToHashSet(StringComparer.Ordinal);
        var used = players.Values.ToHashSet(ReferenceEqualityComparer.Instance);
        var replacements = spare.Where(p => !used.Contains(p) && !names.Contains(p.Name)).ToList();
        var inUse = Clubs.SelectMany(c => c.Players).Concat(RetiredPlayers).Select(p => p.Name).Concat(names)
            .ToHashSet(StringComparer.Ordinal);
        var freeNames = new Queue<string>(market.DatabaseNames.Select(n => n.Name).Distinct()
            .Where(n => !inUse.Contains(n) && replacements.All(r => r.Name != n)));
        foreach (var club in Clubs.Where(c => Managers.All(m => m.ClubIndex != c.Index)))
        {
            var removed = club.Players.Where(p => poolNames.Contains(p.Name)).ToList();
            foreach (var p in removed)
            {
                club.RemovePlayer(p);
                NationalCoach?.Remove(p);
                var stand = replacements.FirstOrDefault(r => r.Role == p.Role);
                if (stand is not null)
                    replacements.Remove(stand);
                else
                {
                    // Kein Poolspieler dieser Art übrig: neuer Spieler mit den Werten des alten und einem freien Datenbanknamen.
                    stand = new Player
                    {
                        Name = freeNames.Count > 0 ? freeNames.Dequeue() : $"SPIELER {NextPlayerId}",
                        Role = p.Role, Age = p.Age, Talent = p.Talent, Condition = p.Condition, Technique = p.Technique, Form = p.Form,
                        Strain = p.Strain,
                    };
                    AssignPlayerId(stand);
                }
                stand.UnsolicitedOffer = null;
                PlayerFactory.Join(club, stand, Math.Max(1, p.ContractYears), loan: false, managerSquad: false);
            }
            if (removed.Count > 0)
                club.Lineup.FillGaps(club.Players);
        }
        market.ImportOriginal(slots, listings, PlayedMatchDays);
        return players;
    }
}
