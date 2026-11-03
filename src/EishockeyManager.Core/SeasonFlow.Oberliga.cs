namespace EishockeyManager.Core;

/// <summary>
/// Spur saisonwechsel, Teil 1 (S-K1, S-K2, #166): Austausch der Oberliga mit den Vereinen ohne Liga wie em.exe
/// (Tauschgruppe 2, Datei 0x56238–0x5636D).
/// </summary>
/// <remarks>
/// [ORIGINAL] em.exe führt die Vereine auf festen Plätzen (Strukturindex): 0–11 Bundesliga, 12–23 2. Liga, 24–35 Oberliga,
/// 36–47 ohne Liga. Auf- und Abstieg vertauscht ganze Vereinssätze zwischen zwei Plätzen (Datei 0x36169), der Absteiger
/// übernimmt also den Platz seines Nachrückers. Nachrücker der Oberliga ist der Verein auf Platz random(36, 41) (Datei
/// 0x56238–0x56282, `0x38:0x847(a, a + 5)`); die Plätze 42–47 rücken nie nach (docs/analyse/re-saisonwechsel.md §3,
/// „Auswertung DOSBox-Beleg“: Wedemark 35 → 40, Miesbach 40 → Oberliga, Vereine 42–47 unverändert draußen).
/// Das Remake vertauscht keine Vereinssätze (der Vereinsindex bleibt fest); die Plätze der Vereine ohne Liga werden daher aus
/// der ersten Saison und den Paaren der Saisonrückblicke (<see cref="SeasonRecord.LeftOberliga"/> mit
/// <see cref="SeasonRecord.PromotedInto"/> der Oberliga) abgeleitet. Das ist kein neuer Zustand und braucht kein Schemafeld.
/// </remarks>
public sealed partial class GameState
{
    /// <summary>Erster Platz der Vereine ohne Liga (em.exe DGROUP 0x7C2 + 0x7B3 = 24 + 12).</summary>
    public const int FirstOutsidePlace = 3 * ClubsPerLeague;

    /// <summary>Plätze, aus denen der Oberliga-Nachrücker gezogen wird (36–41, `random(a, a + 5)`).</summary>
    public const int OutsideDrawPlaces = 6;

    /// <summary>Konto eines Managers, dessen Verein als Letzter oder Vorletzter in der Oberliga bleibt.</summary>
    /// <remarks>[ORIGINAL] Datei 0x562D8/0x562DE: 0x00124F80 Drittel = 400.000 DM, gesetzt, nicht gebucht (DOSBox-Beleg
    /// `file13_15`: −491.221 DM → 400.000 DM). Ohne Kontoprüfung (E5, Datei 0x5629D–0x562DE): auch ein Guthaben wird gekürzt.</remarks>
    public const long OberligaKeptMoney = 400_000;

    /// <summary>
    /// Vereine ohne Liga nach Platz: Eintrag i ist der Verein auf Platz 36 + i (12 Einträge). Die ersten
    /// <see cref="OutsideDrawPlaces"/> sind die möglichen Nachrücker der Oberliga.
    /// </summary>
    /// <remarks>
    /// Start: Platz = Datenbankindex. [APPROX] Hat ein Manager beim Spielstart einen Verein ohne Liga gewählt, verdrängt er einen
    /// Oberligisten (<see cref="NewGame.AssignClubs"/>); im Original nimmt der Verdrängte den Platz des Gewählten ein (Tausch
    /// mit Schalter 0x80, reverse-engineering.md Teil 1). Welcher Verdrängte zu welchem Gewählten gehört, hält das Remake nicht
    /// fest; bei mehreren werden beide aufsteigend nach Vereinsindex gepaart. Danach je Saison: Der Oberliga-Absteiger
    /// übernimmt den Platz seines Nachrückers.
    /// </remarks>
    public IReadOnlyList<int> OutsidePlaces()
    {
        var firstSeason = History.Count > 0
            ? History[0].FinalTables.Values.SelectMany(t => t).ToHashSet()
            : Clubs.Where(c => c.League is not null).Select(c => c.Index).ToHashSet();
        var places = Enumerable.Range(FirstOutsidePlace, NewGame.SelectableClubs - FirstOutsidePlace).ToArray();
        var chosen = Enumerable.Range(0, places.Length).Where(i => firstSeason.Contains(places[i])).ToList();
        var displaced = Enumerable.Range(0, FirstOutsidePlace).Where(c => !firstSeason.Contains(c)).ToList();
        for (var k = 0; k < chosen.Count && k < displaced.Count; k++)
            places[chosen[k]] = displaced[k];

        foreach (var record in History)
            ApplyOberligaPairs(record, places);
        return places;
    }

    private static void ApplyOberligaPairs(SeasonRecord record, int[] places)
    {
        var arriving = record.PromotedInto[League.Oberliga];
        for (var k = 0; k < record.LeftOberliga.Count && k < arriving.Count; k++)
        {
            var index = Array.IndexOf(places, arriving[k]);
            if (index >= 0 && record.LeftOberliga[k] != arriving[k])
                places[index] = record.LeftOberliga[k];
        }
    }

    /// <summary>
    /// Tauschgruppe 2 am Ende der Vereinssaison: Paare (Letzter, Nachrücker) und (Vorletzter, Nachrücker).
    /// </summary>
    /// <remarks>
    /// [ORIGINAL] Datei 0x56238–0x56284: aus = Platzliste 0x5CCB + 24 + 11 − k (k = 0 Letzter, k = 1 Vorletzter), ein =
    /// Verein auf Platz random(36, 41), neu gewürfelt, solange der Zug dem vorigen gleicht. Datei 0x562C6–0x5636D: Gehört der
    /// Absteiger einem Manager, tauscht em.exe zweimal – der Verein bleibt in der Oberliga, der gezogene Verein bleibt draußen,
    /// der Platz wird nicht nachbesetzt (DOSBox-Beleg #389: Krefelder EV bleibt als Letzter, nur ein Verein rückt nach). Die
    /// Listen enthalten das Paar trotzdem; das Remake trägt den Managerverein dafür als Absteiger und als Nachrücker ein
    /// (Fenster „Saisonende“ zeigt ihn grün in der Spalte Oberliga, `file13_14`). Gezogen wird auch für dieses Paar.
    /// [APPROX] Wie bisher zählen alle Manager, auch entlassene; ob em.exe deren Vereinsfeld +0x16 löscht, ist nicht gelesen.
    /// [APPROX] Das Remake zieht schon am 13. April (Ende der Vereinssaison), em.exe am 29. April; dazwischen ändert sich
    /// nichts, was den Zug beeinflusst (Ligaspiele ruhen).
    /// </remarks>
    private void ExchangeOberliga(IReadOnlyList<int> oberligaTable, SeasonRecord record)
    {
        var managerClubs = Managers.Select(m => m.ClubIndex).ToHashSet();
        var draw = OutsidePlaces().Take(OutsideDrawPlaces).ToArray();
        var previous = -1;
        for (var k = 0; k < 2; k++)
        {
            var leaving = oberligaTable[oberligaTable.Count - 1 - k];
            int pick;
            do pick = draw[Random.Next(draw.Length)];
            while (pick == previous);
            previous = pick;
            record.LeftOberliga.Add(leaving);
            record.PromotedInto[League.Oberliga].Add(managerClubs.Contains(leaving) ? leaving : pick);
        }
    }

    /// <summary>
    /// Ereignisse, nach denen die Saisonprämie berechnet wird: ohne Aufstieg, wenn der Verein in der Oberliga geblieben ist.
    /// </summary>
    /// <remarks>[ORIGINAL] Aufstiegsprämie nur mit Bit 0x01 ohne Bit 0x02 (Datei 0x270A8–0x27124; DOSBox-Beleg `file13_15`: nur
    /// 100.000 DM Fair-Play-Prämie, nichts für den Aufstieg). <c>FinancePremiums.cs</c> gehört der Spur geld, deshalb hier.</remarks>
    public static IReadOnlyCollection<SeasonEvent> PremiumEvents(SeasonRecord record, int club, IReadOnlyCollection<SeasonEvent> events) =>
        KeptInOberliga(record, club) ? events.Where(e => e != SeasonEvent.Promotion).ToList() : events;

    /// <summary>Verein eines Managers ist als Letzter oder Vorletzter in der Oberliga geblieben (Doppeltausch).</summary>
    public static bool KeptInOberliga(SeasonRecord record, int club) =>
        record.LeftOberliga.Contains(club) && record.PromotedInto[League.Oberliga].Contains(club);
}
