namespace EishockeyManager.Core;

/// <summary>Kader, Aufstellung und Spielstärke eines Vereins.</summary>
public sealed partial class Club
{
    public Lineup Lineup { get; } = new();

    /// <summary>
    /// Teamwert der Auto-Aufstellung bei Spielbeginn (<see cref="TeamStrength.Rating"/>). Bezugspunkt nur für
    /// <see cref="MatchStrength"/> (alter Ergebnisweg); die Spielberechnung liest ihn nicht.
    /// </summary>
    public double ReferenceRating { get; set; }

    /// <summary>Stärke für den alten Ergebnisweg <see cref="MatchEngine.Play"/> (<see cref="TeamStrength.MatchStrength"/>).</summary>
    public int MatchStrength => TeamStrength.MatchStrength(this);

    /// <summary>Status-Spalte eines Kaderspielers.</summary>
    public PlayerStatus StatusOf(Player player) =>
        player.InjuredGames > 0 ? PlayerStatus.Injured
        : player.SuspendedGames > 0 ? PlayerStatus.Suspended
        : Lineup.Contains(player) ? PlayerStatus.InTeam
        : PlayerStatus.Reserve;

    /// <summary>
    /// Kader nach Rolle (Torhüter, Verteidiger, Stürmer), je nach Nr. (= Kaderplatz + 1, <see cref="RenumberPlayers"/>).
    /// [APPROX] Nur für Listen der Oberfläche; Spiel und Auto-Aufstellung nehmen die Kaderplatzfolge (<see cref="Players"/>, A091).
    /// </summary>
    public IEnumerable<Player> SortedPlayers => Players.OrderBy(p => p.Role).ThenBy(p => p.Number);

    /// <summary>
    /// A091: Nr. = Kaderplatz + 1. [ORIGINAL] Das Original kennt keine eigene Rückennummer; die „Nr.“ ist der Kaderplatz + 1
    /// (FILE26: nach dem Verkauf von Platz 13 heißen die Plätze 14–24 dann 13–23). Die Kaderplätze sind die Reihenfolge von
    /// <see cref="Players"/>: Kauf nach Spielerart einsortiert (Datei 0x42D55,
    /// R13GN-01; Stürmer hinten FILE07: Platz 25), Verkauf rückt auf (Datei 0x425B6, FILE05 → FILE26),
    /// Saisonwechsel unverändert (A085, FILE22/FILE23: alle 22 Plätze behalten ihren Poolindex).
    /// </summary>
    public void RenumberPlayers()
    {
        for (var i = 0; i < Players.Count; i++)
            Players[i].Number = i + 1;
    }

    /// <summary>
    /// Spieler verlässt den Kader: Platz entfällt, die folgenden Plätze rücken auf (Datei 0x425B6, A091), Aufstellung ohne ihn.
    /// </summary>
    public void RemovePlayer(Player player)
    {
        Players.Remove(player);
        Lineup.Remove(player);
        RenumberPlayers();
    }
}

/// <summary>
/// Teamwert aus der Aufstellung (A174, Spur teamwert-aufstellung).
/// <list type="bullet">
/// <item>[ORIGINAL] Das Original kennt keine eigene Stärkeformel der Aufstellung: 0x952C bildet aus den eingesetzten Spielern der
///   Reihen 0–2 je Teil (Abwehr, Sturm) Kondition, Technik und Form (Reihenwerte 0x98F8 ohne Spiel, Summe der drei Reihen / 3,
///   Datei 0x095A0–0x09666) und schreibt sie in die Vereinsfelder 0x18–0x1D (Datei 0x09721–0x09771); die Stärke 0x368B1 liest
///   über 0x977F diese Felder (Datei 0x097F3–0x0984F) bzw. in der Managerpaarung die Spieler auf dem Eis (0x98F8,
///   <see cref="LiveSquad"/>). Rechnung: <see cref="MatchEngine.LineupTeamValues"/>, <see cref="MatchEngine.ClubLineupTeamValues"/>.</item>
/// <item><see cref="FieldValue"/>: eine Zahl im Maßstab der Datenbankstärke aus den vier Feldern Kondition und Technik – für die
///   Stellen, an denen das Remake Vereine über einen Teamwert vergleicht (<see cref="MatchEngine.TeamValue"/>).</item>
/// </list>
/// Vorher (bis Runde 20) eine eigene Gewichtung: Torhüter 25 %, Blöcke 35/30/25/10 %, Energie zu 25 %.
/// </summary>
public static class TeamStrength
{
    /// <summary>
    /// Spielwert eines Spielers: Stärke, gemindert bei wenig Energie. [APPROX] Nur Reihenfolge beim Auffüllen freier Plätze
    /// (<see cref="Lineup.FillGaps"/>); in keinen Teamwert geht er ein.
    /// </summary>
    public static double PlayerValue(Player p) => p.Strength * (0.75 + 0.25 * Math.Clamp(p.Energy, 0, 100) / 100.0);

    /// <summary>
    /// Teamwert einer Aufstellung im Maßstab der Vereinsfelder: Mittel aus Kondition und Technik von Abwehr und Sturm, wie 0x952C
    /// sie aus den Reihen 0–2 bildet (siehe Klasse). Leere Plätze zählen 0.
    /// </summary>
    public static double Rating(Lineup lineup)
    {
        var (defense, attack, _) = MatchEngine.LineupTeamValues(lineup);
        return Mean(defense, attack);
    }

    /// <summary>
    /// Teamwert eines Vereins aus seinen Vereinsfeldern 0x18–0x1B (Kondition und Technik von Abwehr und Sturm,
    /// <see cref="MatchEngine.ClubFieldValues"/>), begrenzt auf 0 … 100. [ORIGINAL] Die Felder: bei Managervereinen von 0x952C aus
    /// der Aufstellung geschrieben (Tage mit Ereignis, Datei 0x54DAA und 0x54FBF, <see cref="GameState.UpdateManagerTeamValues"/>,
    /// und beim Verlassen der Vereinsaufstellung, Datei 0x41513–0x4151A, <see cref="GameState.OnLineupLeft"/>), dazwischen bewegt
    /// von der Wanderung 0x9D1B. [APPROX] Das Mittel der vier Felder als eine Zahl gibt es im Original nicht;
    /// das Remake braucht sie nur, wo es Vereine im Maßstab der Datenbankstärke vergleicht (Spiele gegen Nationalteams ohne
    /// Spielvorbereitung 0x8365, siehe <see cref="MatchEngine.TeamValue"/>).
    /// </summary>
    public static int FieldValue(Club club)
    {
        var (defense, attack) = MatchEngine.ClubFieldValues(club);
        return Math.Clamp((int)Math.Round(Mean(defense, attack)), 0, 100);
    }

    private static double Mean(PartValues defense, PartValues attack) =>
        (defense.Condition + defense.Technique + attack.Condition + attack.Technique) / 4.0;

    /// <summary>
    /// Stärke für den alten Ergebnisweg <see cref="MatchEngine.Play"/> (ohne Aufrufer im Spielablauf): Datenbankstärke, verschoben
    /// um die Veränderung des Teamwerts der Aufstellung (<see cref="Rating"/>) gegenüber dem Spielbeginn
    /// (<see cref="Club.ReferenceRating"/>). [APPROX] Diesen Weg und diese Verschiebung gibt es im Original nicht; die
    /// Spielberechnung (<see cref="MatchEngine.PlayWithEvents"/>) benutzt sie nicht.
    /// </summary>
    public static int MatchStrength(Club club)
    {
        if (club.Players.Count == 0)
            return club.Strength;
        return Math.Clamp((int)Math.Round(club.Strength + Rating(club.Lineup) - club.ReferenceRating), 1, 150);
    }

    /// <summary>Durchschnittsstärke eines Kaders (Statuszeile „Durchschnittsstärke“).</summary>
    public static int AverageStrength(IEnumerable<Player> players)
    {
        var list = players.ToList();
        return list.Count == 0 ? 0 : (int)list.Average(p => p.Strength);
    }
}
