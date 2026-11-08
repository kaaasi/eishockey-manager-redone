namespace EishockeyManager.Core;

/// <summary>
/// Spur saison, Teil 3 (S-K6-c, docs/analyse/re-runde-13.md): Fair-Play-Prämie des Saisonendes über die Liga der nächsten
/// Saison in deren gemischter Satzfolge.
/// </summary>
/// <remarks>
/// [ORIGINAL] Das Fenster „Saisonende“ (Datei 0x5645D → 0x26F20, Fair-Play Datei 0x27213 <c>0xE0:0x20(0)</c>) läuft nach der
/// Ligamischung (Datei 0x5644E). 0x2A1E0 liest die Liga aus dem Managerfeld DGROUP 0x9746 (Datei 0x2A1F2), das erst Datei 0x56409
/// aus dem Index nach dem Austausch setzt – also die Liga der nächsten Saison – und sortiert deren zwölf Vereine ab DGROUP
/// 0x7C0 + Liga (Satzfolge nach der Mischung) aufsteigend nach Strafminuten (Datei 0x2A21D–0x2A292, Tausch nur bei echt mehr
/// Minuten: Erster ist der erste Verein der Satzfolge mit den wenigsten Minuten). Die Strafminuten (Tabellensatz +0x4A) sind die
/// der abgelaufenen Saison, wandern mit dem Vereinssatz (0x36169) und werden erst danach gelöscht (Datei 0x5647A–0x56497);
/// Nachrücker aus den ligalosen Vereinen haben 0. Prämie, wenn der Erste der Verein des Managers ist (Datei 0x2A29D).
/// Topscorer und Torhüter (Datei 0x5576B, vor dem Austausch) bleiben bei der alten Liga.
/// Die Sortierung entspricht <see cref="FairPlayTable"/> (StatisticsClub.cs, Spur ui-rest); die Anzeige der Tabelle zwischen
/// 29. April und Saisonstart (im Original schon die neue Liga mit 0 Minuten) ist im Remake nicht erreichbar – der Bildschirm
/// „Saisonende“ führt direkt in die neue Saison.
/// </remarks>
public sealed partial class GameState
{
    /// <summary>
    /// Fair-Play-Erster der Liga, in der <paramref name="club"/> nächste Saison spielt (am 29. April nach
    /// <see cref="PrepareNextLeagueSlots"/>); null ohne Liga oder ohne gebildete Folge.
    /// </summary>
    public int? FairPlayPremiumWinner(SeasonRecord record, int club)
    {
        if (record.NextLeague(club, Clubs[club].League) is not { } league)
            return null;
        int? first = null;
        foreach (var c in NextLeagueOrder(record, league))
            if (first is null || Clubs[c].LeaguePenaltyMinutes < Clubs[first.Value].LeaguePenaltyMinutes)
                first = c;
        return first;
    }

    /// <summary>
    /// Vereine der Liga der nächsten Saison in Satzfolge; fehlt die am 29. April gebildete Folge (Spielstand vor Schema 38,
    /// ungewöhnliche Ligagröße, nur synthetisch), gilt wie bei <see cref="LeagueSlots"/> die Indexfolge der Mitglieder.
    /// </summary>
    /// <remarks>
    /// [APPROX] Rückfall auf die Indexfolge: Im Original gibt es immer eine Satzfolge (DGROUP 0x7C0 ff.); nur bei gleichen
    /// Strafminuten entscheidet die Reihenfolge, daher ist die Abweichung auf Spielstände ohne gespeicherte Folge begrenzt (R13IS-02, #592).
    /// </remarks>
    private IEnumerable<int> NextLeagueOrder(SeasonRecord record, League league)
    {
        var slots = NextLeagueSlots(league);
        if (slots.Count > 0)
            return slots;
        return Clubs
            .Where(c => (c.Index < NewGame.SelectableClubs ? record.NextLeague(c.Index, c.League) : c.League) == league)
            .Select(c => c.Index).Order();
    }

    /// <summary>
    /// Einzelprämien eines Managers am 29. April: Torhüter und Topscorer der alten Liga (<see cref="SinglePremiumEvents"/>),
    /// Fair-Play über die neue Liga (<see cref="FairPlayPremiumWinner"/>), Reihenfolge des Fensters (Datei 0x27184–0x2725D).
    /// </summary>
    private IEnumerable<SeasonEvent> SeasonEndSinglePremiums(SeasonRecord record, Manager manager, League league)
    {
        foreach (var e in SinglePremiumEvents(manager, league))
            if (e != SeasonEvent.FairPlay)
                yield return e;
        if (FairPlayPremiumWinner(record, manager.ClubIndex) == manager.ClubIndex)
            yield return SeasonEvent.FairPlay;
    }
}
