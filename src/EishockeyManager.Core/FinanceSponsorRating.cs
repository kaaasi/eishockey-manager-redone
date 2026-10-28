namespace EishockeyManager.Core;

/// <summary>
/// Bewertung eines Vereins für die Werbepartner, 0–100 (Spur finanzen-2). [ORIGINAL] em.exe Datei 0x3F833
/// (Einsprung 0x100:0x57, Manager-Feld 0x97CE):
/// <code>
/// S  = (2 − StartLiga) · (80 · Titel + 170)
///    + 32 · (Pokalsiege · (n + 1) − 2)  + 47 · Europapokalsiege · (k + 1)
///    + 12 · (11 − Mittel der Ligaplätze der letzten ≤ 5 Saisons, ohne Historie 6)
///    + Mittel(Spielerwert / 5500) + random(20, 50) · min(4, Spieler mit Spielerwert / 7000 > random(35, 55) / 10)
/// R  = min(100, ((S + 3 · Stadionwert) / 12 + 10 · (2 − StartLiga)) · 85 / 100)
/// </code>
/// Belege: docs/finanzen-rekonstruktion.md. Die drei Titelzähler (Masken 0x40, 0x08, 0x20 der Funktion 0x110:0x61)
/// sind nicht aufgelöst: [APPROX] hier Meisterschaften, DEB-Pokalsiege und Europapokalsiege aus der Ewigen Tabelle,
/// ohne die unbekannten Faktoren n und k. Spielerwerte: Kaufpreis des Remakes × 3 (Drittel) [APPROX].
/// </summary>
public static class SponsorRating
{
    /// <summary>Stadionwert ([ORIGINAL] 0x3EBEA, Tabelle DGROUP 0x4F9C), geteilt durch 700.</summary>
    private static readonly int[] FeatureWorth = [4333, 2333, 1166, 2833, 2000, 3333, 2333, 3166, 2250];

    /// <summary>
    /// Stadionwert: min(50.000, (1,5 × Sitzplätze + Stehplätze) / 2) + Σ adj_i × Wert_i, davon 1/700. Die Anpassung
    /// adj: Überdachung − 1 (nur wenn > 0), Kühlanlage mindestens 1, Sitzkomfort fest 2, Zustand fest 3, sonst Stufe.
    /// </summary>
    public static int StadiumWorth(Stadium stadium)
    {
        long worth = Math.Min(50_000, (3 * stadium.Seats / 2 + stadium.Standing) / 2);
        for (var i = 0; i < Stadium.FeatureCount; i++)
        {
            long level = stadium.Level((StadiumFeature)i);
            level = i switch
            {
                0 when level > 0 => level - 1,
                2 => Math.Max(1, level),
                3 => 2,
                4 => 3,
                _ => level,
            };
            worth += level * FeatureWorth[i];
        }
        return (int)(worth / 700);
    }

    public static int Compute(GameState game, Manager manager)
    {
        var club = game.Clubs[manager.ClubIndex];
        var finance = game.Finance(manager);
        var tier = (int)finance.StartLeague;
        var random = game.FinanceRandom;
        var eternal = game.Eternal(club.Index);

        long s = (2 - tier) * (80 * eternal.Championships + 170);
        s += 32L * eternal.CupWins;
        s += 47L * eternal.EuropeanCupWins;

        var history = game.ManagerHistory(game.IndexOf(manager));
        var recent = history.TakeLast(5).ToList();
        var averageRank = recent.Count == 0 ? 6 : recent.Sum(h => h.Rank) / recent.Count;
        s += 12L * (11 - averageRank);

        var players = club.Players;
        if (players.Count > 0)
        {
            s += players.Sum(p => TransferMarket.Price(p) * FinanceRules.ThirdsPerMark / 5500) / players.Count;
            var stars = 0;
            foreach (var p in players)
                if (TransferMarket.Price(p) * FinanceRules.ThirdsPerMark / 7000 > SquadSetup.Between(random, 35, 55) / 10 && stars < 4)
                    stars++;
            s += (long)SquadSetup.Between(random, 20, 50) * stars;
        }

        s += 3L * StadiumWorth(finance.Stadium);
        var rating = (s / 12 + 10 * (2 - tier)) * 85 / 100;
        return (int)Math.Clamp(rating, 0, 100);
    }
}
