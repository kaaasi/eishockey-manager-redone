namespace EishockeyManager.Core;

public sealed partial class Club
{
    /// <summary>
    /// Strafminuten des Vereins in den Ligaspielen dieser Saison (Fair-Play-Tabelle). [ORIGINAL] DGROUP 0x82A1 + 0x50 ·
    /// Verein (Wort): Datei 0x35967–0x35986 addiert bei jeder Strafe (Code &amp; 7 = 2 bzw. 5 Minuten) nur am
    /// Ligaspieltag (Wettbewerbscode DGROUP 0xD0C2 = 7); Datei 0x5647A–0x56497 setzt die Werte der Vereine 0–63 nach den
    /// Prämien des Saisonendes auf 0.
    /// </summary>
    public int LeaguePenaltyMinutes { get; set; }
}

/// <summary>Zeile der Fair-Play-Tabelle: Platz, Verein, Strafminuten und Ligaspiele.</summary>
public sealed record FairPlayRow(int Rank, Club Club, int PenaltyMinutes, int Games)
{
    /// <summary>
    /// [ORIGINAL] „PRO SPIEL“ in Hundertsteln (Datei 0x2A4B5–0x2A4FA): Strafminuten · 100 / Spiele, ohne Spiele durch 1.
    /// </summary>
    public int PerGameHundredths => PenaltyMinutes * 100 / Math.Max(1, Games);
}

public sealed partial class GameState
{
    /// <summary>
    /// Strafminuten eines Ligaspiels gutschreiben (Aufruf aus <see cref="PlayNextMatchDay"/>). [ORIGINAL] Datei 0x35967:
    /// nur am Ligaspieltag; Pokale, Play-Offs, Relegation und WM zählen nicht.
    /// </summary>
    internal static void CountLeaguePenalties(Club home, Club away, MatchReport report)
    {
        home.LeaguePenaltyMinutes += report.HomePenaltyMinutes;
        away.LeaguePenaltyMinutes += report.AwayPenaltyMinutes;
    }

    /// <summary>[ORIGINAL] Neue Saison: Strafminuten aller Vereine auf 0 (Datei 0x5647A, nach den Prämien 0x5645D).</summary>
    internal void ResetLeaguePenalties()
    {
        foreach (var club in Clubs)
            club.LeaguePenaltyMinutes = 0;
    }

    /// <summary>
    /// Ligaspiele eines Vereins in dieser Saison. [ORIGINAL] Datei 0x2A4C1–0x2A4DD: Vereinsstatistik +0 (Heimspiele) +
    /// +1 (Auswärtsspiele), DGROUP 0x8278 + 0x50 · Verein; im Remake die gespielten Ligaspiele in <see cref="Results"/>.
    /// </summary>
    public int LeagueGames(Club club) => Results.Count(r => r.Involves(club.Index));

    /// <summary>
    /// Fair-Play-Tabelle einer Liga. [ORIGINAL] em.exe Ov 0xE0, Datei 0x2A1E0 (<c>0xE0:0x20</c>): die zwölf Vereine ab
    /// DGROUP 0x7C0 + Liga in Indexfolge, Austauschsortierung aufsteigend nach Strafminuten (Datei 0x2A21D–0x2A292:
    /// Tausch nur bei echt mehr Minuten). Bei Gleichstand um Platz 1 steht der Verein mit dem kleineren Index vorn
    /// (das entscheidet die Fair-Play-Prämie); dahinter ist die Sortierung nicht stabil und wird genau so nachgebildet.
    /// [APPROX] Reihenfolge der Ligaplätze: Vereinsindex des Remakes (das Original tauscht die Vereinssätze beim Auf- und
    /// Abstieg, die Indexfolge innerhalb einer Liga kann daher abweichen).
    /// </summary>
    public IReadOnlyList<FairPlayRow> FairPlayTable(League league)
    {
        var order = LeagueClubs(league).OrderBy(c => c.Index).ToList();
        for (var i = 0; i < order.Count; i++)
            for (var j = i + 1; j < order.Count; j++)
                if (order[i].LeaguePenaltyMinutes > order[j].LeaguePenaltyMinutes)
                    (order[i], order[j]) = (order[j], order[i]);
        return order.Select((c, i) => new FairPlayRow(i + 1, c, c.LeaguePenaltyMinutes, LeagueGames(c))).ToList();
    }

    /// <summary>
    /// Einzelprämien eines Managers am Saisonende in der Reihenfolge des Fensters „Saisonende“ (Datei 0x27184–0x2725D):
    /// bester Torhüter (Symbol 12), Topscorer (Symbol 3), Fair-Play-Erster (Symbol 13). Je Ereignis
    /// <see cref="SinglePremium"/> über <see cref="SeasonPremium"/>.
    /// <list type="bullet">
    /// <item>[ORIGINAL] Torhüter und Topscorer: Kennungen aus <c>0xC8:0x20(1)</c> je Manager (Datei 0x5576E), beide
    ///   aus der Liga des Managers (<see cref="PlayerStatistics"/>).</item>
    /// <item>[ORIGINAL] Fair-Play: <c>0xE0:0x20(0)</c> (Datei 0x27213) liefert 1, wenn der Erste der Fair-Play-Tabelle der
    ///   eigenen Liga der Verein des Managers ist (Vereinsfeld 0x16 = Manager + 1, Datei 0x2A29D–0x2A2CC).</item>
    /// </list>
    /// </summary>
    public IReadOnlyList<SeasonEvent> SinglePremiumEvents(Manager manager, League league)
    {
        var events = new List<SeasonEvent>();
        if (PlayerStatistics.BestGoalie(this, league) is { } goalie && goalie.Club.Index == manager.ClubIndex)
            events.Add(SeasonEvent.BestGoalie);
        if (PlayerStatistics.TopScorer(this, league) is { } scorer && scorer.Club.Index == manager.ClubIndex)
            events.Add(SeasonEvent.TopScorer);
        if (FairPlayTable(league) is [var first, ..] && first.Club.Index == manager.ClubIndex)
            events.Add(SeasonEvent.FairPlay);
        return events;
    }
}
