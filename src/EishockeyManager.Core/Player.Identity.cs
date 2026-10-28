namespace EishockeyManager.Core;

/// <summary>Spieler – Teil der Spur „speichern“: dauerhafte Kennung.</summary>
public sealed partial class Player
{
    /// <summary>
    /// Dauerhafte Spieler-ID (ab 1; 0 = noch nicht im Spiel). Wird beim Spielstart und für jedes Talent genau einmal
    /// vergeben und nie wiederverwendet – auch nicht nach dem Laufbahnende. Spielstände sichern Verweise auf Spieler
    /// (Kader, Aufstellung, Markt, Pool, Leihe, Nationalkader, Laufbahnende) über diese ID.
    /// [APPROX] Das Original kennt keine IDs; sie gehören nur zum eigenen Spielstandformat.
    /// </summary>
    public int Id { get; internal set; }
}

/// <summary>Spielstand – Teil der Spur „speichern“: Vergabe und Prüfung der Spieler-IDs.</summary>
public sealed partial class GameState
{
    /// <summary>Nächste freie Spieler-ID. Steigt nur; alle vergebenen IDs sind kleiner.</summary>
    public int NextPlayerId { get; internal set; } = 1;

    /// <summary>Vergibt die nächste ID an einen neuen Spieler. Ein Spieler mit ID ist ein Programmfehler.</summary>
    internal void AssignPlayerId(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (player.Id != 0)
            throw new InvalidOperationException($"{player.Name} hat schon die ID {player.Id}.");
        player.Id = NextPlayerId++;
    }

    /// <summary>Spielstart: alle Kaderspieler in Vereins- und Kaderreihenfolge (verbraucht keinen Zufall).</summary>
    internal void AssignStartPlayerIds()
    {
        foreach (var club in Clubs)
            foreach (var player in club.Players)
                AssignPlayerId(player);
    }

    /// <summary>Alle Spieler des Spielstands: Kader, vertragsloser Pool und Laufbahnende.</summary>
    public IEnumerable<Player> AllPlayers =>
        Clubs.SelectMany(c => c.Players).Concat(TransferMarket.FreeAgents).Concat(RetiredPlayers);

    /// <summary>Spieler zur ID oder null.</summary>
    public Player? FindPlayer(int id) => id <= 0 ? null : AllPlayers.FirstOrDefault(p => p.Id == id);

    /// <summary>
    /// Prüft IDs und Spielerverweise (leere Liste = in Ordnung): jeder Spieler genau einmal mit eindeutiger ID
    /// unter <see cref="NextPlayerId"/>; Aufstellung, Transfermarkt, Gebote, Leihen und Nationalkader zeigen nur auf
    /// vorhandene Spieler bzw. Vereine.
    /// </summary>
    public IReadOnlyList<string> ValidatePlayerIdentity()
    {
        var problems = new List<string>();
        var all = AllPlayers.ToList();
        var known = new HashSet<Player>(ReferenceEqualityComparer.Instance);
        var ids = new Dictionary<int, Player>();
        foreach (var p in all)
        {
            if (!known.Add(p))
                problems.Add($"{p.Name} (ID {p.Id}) steht mehrfach im Spielstand.");
            if (p.Id <= 0 || p.Id >= NextPlayerId)
                problems.Add($"{p.Name} hat die ungültige ID {p.Id} (nächste freie {NextPlayerId}).");
            else if (ids.TryGetValue(p.Id, out var other) && !ReferenceEquals(other, p))
                problems.Add($"ID {p.Id} doppelt: {other.Name} und {p.Name}.");
            else
                ids[p.Id] = p;
        }

        var clubs = new HashSet<Club>(Clubs, ReferenceEqualityComparer.Instance);
        foreach (var club in Clubs)
        {
            foreach (var p in club.Lineup.Players.Where(p => !club.Players.Contains(p)))
                problems.Add($"Aufstellung von {club.Name}: {p.Name} (ID {p.Id}) gehört nicht zum Kader.");
            foreach (var p in club.Players.Where(p => p.Loan is not null))
            {
                if (!clubs.Contains(p.Loan!.Origin))
                    problems.Add($"Leihe {p.Name} (ID {p.Id}): Stammverein fehlt.");
                else if (p.Loan.Origin == club)
                    problems.Add($"Leihe {p.Name} (ID {p.Id}): an den eigenen Stammverein.");
            }
        }

        foreach (var listing in TransferMarket.Listings.Where(l => !known.Contains(l.Player)))
            problems.Add($"Transfermarkt: {listing.Player.Name} (ID {listing.Player.Id}) fehlt im Spielstand.");
        foreach (var bid in TransferMarket.Bids.Where(b => !TransferMarket.Listings.Contains(b.Listing)))
            problems.Add($"Gebot auf {bid.Listing.Player.Name} (ID {bid.Listing.Player.Id}) ohne Angebot.");
        if (NationalCoach is { } coach)
            foreach (var member in coach.Squad.Where(m => !known.Contains(m.Player)))
                problems.Add($"Nationalkader: {member.Player.Name} (ID {member.Player.Id}) fehlt im Spielstand.");
        return problems;
    }
}
