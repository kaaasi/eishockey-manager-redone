namespace EishockeyManager.Core;

/// <summary>Kader, Aufstellung und Spielstärke eines Vereins.</summary>
public sealed partial class Club
{
    public Lineup Lineup { get; } = new();

    /// <summary>
    /// Bewertung der Auto-Aufstellung bei Spielbeginn. Bezugspunkt für <see cref="MatchStrength"/>:
    /// eine gleich gute Aufstellung ergibt genau die Stärke aus der Datenbank.
    /// </summary>
    public double ReferenceRating { get; set; }

    /// <summary>
    /// Stärke im Spiel: Datenbankstärke, verschoben um die Veränderung der Aufstellung gegenüber dem
    /// Spielbeginn (Spielerwerte, Energie, Lücken). Wird von <see cref="MatchEngine"/> verwendet.
    /// </summary>
    public int MatchStrength => TeamStrength.MatchStrength(this);

    /// <summary>Status-Spalte eines Kaderspielers.</summary>
    public PlayerStatus StatusOf(Player player) =>
        player.InjuredGames > 0 ? PlayerStatus.Injured
        : player.SuspendedGames > 0 ? PlayerStatus.Suspended
        : Lineup.Contains(player) ? PlayerStatus.InTeam
        : PlayerStatus.Reserve;

    /// <summary>Kader sortiert wie in den Listen des Originals: Torhüter, Verteidiger, Stürmer, je nach Rückennummer.</summary>
    public IEnumerable<Player> SortedPlayers => Players.OrderBy(p => p.Role).ThenBy(p => p.Number);
}

/// <summary>
/// Spielstärke aus der Aufstellung.
/// [APPROX] Die Formel des Originals (Overlay 5E73) ist nicht rekonstruiert. Gewichtung: Torhüter 25 %,
/// Blöcke 1–4 mit 35/30/25/10 % der Feldspieler; leere Plätze zählen 0. Energie wirkt zu 25 % auf den Spielwert.
/// </summary>
public static class TeamStrength
{
    public const double GoalkeeperWeight = 0.25;
    public static readonly double[] BlockWeights = [0.35, 0.30, 0.25, 0.10];

    /// <summary>Umrechnung Spielerwerte → Vereinsstärke (Generierung: Basis = 20 + Stärke/2).</summary>
    public const double StrengthPerRatingPoint = 2.0;

    /// <summary>Spielwert eines Spielers: Stärke, gemindert bei wenig Energie.</summary>
    public static double PlayerValue(Player p) => p.Strength * (0.75 + 0.25 * Math.Clamp(p.Energy, 0, 100) / 100.0);

    /// <summary>Bewertung der Aufstellung (Größenordnung wie die Spielerstärken).</summary>
    public static double Rating(Lineup lineup)
    {
        var goalie = lineup.Goalkeeper is { } g ? PlayerValue(g) : 0;
        double skaters = 0;
        for (var b = 0; b < Lineup.Blocks; b++)
        {
            double sum = 0;
            for (var i = 0; i < Lineup.ForwardsPerBlock; i++)
                sum += lineup[LineupSlot.Forward(b, i)] is { } f ? PlayerValue(f) : 0;
            for (var i = 0; i < Lineup.DefendersPerBlock; i++)
                sum += lineup[LineupSlot.Defender(b, i)] is { } d ? PlayerValue(d) : 0;
            var average = sum / (Lineup.ForwardsPerBlock + Lineup.DefendersPerBlock);
            // Ein vierter Block entlastet nur: fehlt er (ganz oder teilweise), zählt sein Anteil wie 90 % von Block 3.
            if (b == 3)
                average = Math.Max(average, BlockAverage(lineup, 2) * 0.9);
            skaters += BlockWeights[b] * average;
        }
        return GoalkeeperWeight * goalie + (1 - GoalkeeperWeight) * skaters;
    }

    private static double BlockAverage(Lineup lineup, int block)
    {
        double sum = 0;
        foreach (var p in lineup.Block(block))
            sum += PlayerValue(p);
        return sum / (Lineup.ForwardsPerBlock + Lineup.DefendersPerBlock);
    }

    public static int MatchStrength(Club club)
    {
        if (club.Players.Count == 0)
            return club.Strength;
        var delta = (Rating(club.Lineup) - club.ReferenceRating) * StrengthPerRatingPoint
                    + club.Tactics.EffortStrengthBonus();
        return Math.Clamp((int)Math.Round(club.Strength + delta), 1, 150);
    }

    /// <summary>Durchschnittsstärke eines Kaders (Statuszeile „Durchschnittsstärke“).</summary>
    public static int AverageStrength(IEnumerable<Player> players)
    {
        var list = players.ToList();
        return list.Count == 0 ? 0 : (int)list.Average(p => p.Strength);
    }
}
