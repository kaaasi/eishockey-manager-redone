using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur wettbewerbe-komplett Teil 3 (Mehrsaisonproben robust): hält den Verein eines Managers über viele Saisons
/// spielfähig, statt die Vorbedingung dem Zufall (Seed) zu überlassen. Vorher hielt <see cref="TestSeason.KeepSquad"/> nur
/// angebotene Verlängerungen; Spieler, die nicht verlängern oder aufhören, gingen trotzdem, und ihre Plätze in der
/// Aufstellung blieben leer (Modus-4-Referenz: Kader 20/20/19/16/12, Saison 5 mit acht Aufgestellten nicht spielfähig).
/// Eingriffe wie ein Manager am Bildschirm, ohne eigene Zufallszüge der Probe (aber mit den Zügen des Spiels, s. u.):
/// <list type="number">
/// <item>angebotene Verlängerungen zur Forderung (wie <see cref="TestSeason.KeepSquad"/>);</item>
/// <item>unter <see cref="MinSquad"/> Spielern vertragslose Spieler der Transferliste zum Preis 0 holen (nach der Art,
/// die am meisten fehlt; <c>TransferMarket.Buy</c>). Seit kader-transfer Teil 3 würfelt jeder Kauf den Sponsorzuschuss
/// (random(0, 6) aus der Hauptquelle, Datei 0x063BE), auch beim Vertragslosen; das Angebot wird nicht angenommen, also
/// keine Buchung, aber anderer Zufallsverbrauch (KT-02, Review #463).</item>
/// <item>gibt es keinen vertragslosen Spieler mehr, einen Spieler des größten Computerkaders ohne Kauf umsetzen
/// (Integration Runde 11);</item>
/// <item>freie Plätze der Aufstellung mit freien Spielern des Kaders füllen (<c>Lineup.FillGaps</c>, bestehende Plätze
/// bleiben).</item>
/// </list>
/// </summary>
internal static class TestSquadKeeper
{
    /// <summary>Kadergröße, unter der nachgekauft wird (Aufstellung 16 = Torhüter, sechs Verteidiger, neun Stürmer).</summary>
    public const int MinSquad = 18;

    public static void KeepPlayable(GameState game, Manager manager)
    {
        if (manager.Dismissed)
            return;
        TestSeason.KeepSquad(game, manager);
        var club = game.Clubs[manager.ClubIndex];
        while (club.Players.Count < MinSquad && Next(game, club) is { } listing)
        {
            if (game.TransferMarket.Buy(game, manager, listing, 0, loan: false) != TransferResult.Accepted)
                break;
        }
        // Integration Runde 11 (+ wettbewerbe-spiel): Seit der Poolumverteilung (kader-transfer Teil 5) steht zu Saisonbeginn oft
        // kein vertragsloser Spieler auf der Liste (Originaldaten, Seed 3: Kader 12 nach dem Abstieg). Dann einen Spieler eines
        // Computervereins ohne Kauf umsetzen – keine Buchung, kein Zufall; nur die Probe, kein Spielweg.
        while (club.Players.Count < MinSquad && Donor(game, club) is { } donor)
        {
            var role = Needed(club);
            var player = donor.Players.Where(p => p.Role == role && p.Available).OrderBy(p => p.Name, StringComparer.Ordinal).FirstOrDefault()
                         ?? donor.Players.Where(p => p.Available).OrderBy(p => p.Name, StringComparer.Ordinal).First();
            donor.Lineup.Remove(player);
            donor.Players.Remove(player);
            player.Number = PlayerFactory.FreeNumber(club, player);
            club.Players.Add(player);
        }
        club.Lineup.FillGaps(club.Players);
    }

    private static Club? Donor(GameState game, Club club) => game.Clubs
        .Where(c => c != club && c.League is not null && game.Managers.All(m => m.ClubIndex != c.Index)
                    && c.Players.Count(p => p.Available) > MinSquad + 2)
        .OrderByDescending(c => c.Players.Count).ThenBy(c => c.Index).FirstOrDefault();

    private static TransferListing? Next(GameState game, Club club)
    {
        var role = Needed(club);
        var free = game.TransferMarket.Listings.Where(l => l.IsFreeAgent).ToList();
        return free.Where(l => l.Player.Role == role).OrderBy(l => l.Player.Name, StringComparer.Ordinal).FirstOrDefault()
               ?? free.OrderBy(l => l.Player.Name, StringComparer.Ordinal).FirstOrDefault();
    }

    private static PlayerRole Needed(Club club)
    {
        int Count(PlayerRole r) => club.Players.Count(p => p.Role == r);
        if (Count(PlayerRole.Goalkeeper) < 2)
            return PlayerRole.Goalkeeper;
        return Count(PlayerRole.Defender) * 10 < Count(PlayerRole.Forward) * 7 ? PlayerRole.Defender : PlayerRole.Forward;
    }
}
