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
        club.Lineup.FillGaps(club.Players);
    }

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
