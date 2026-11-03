namespace EishockeyManager.Core;

/// <summary>
/// Spur saisonwechsel, Teil 2 (S-K3): Zwangsabstieg wegen Schulden wie em.exe (Datei 0x55F17–0x560DB, Statusbit 0x04) und
/// Austausch der Aufstiegsrunden über die Gruppentabelle (Datei 0x56157–0x5636D).
/// </summary>
/// <remarks>
/// [ORIGINAL] DOSBox-Beleg FILE16 → FILE17 (Spur saisonwechsel Teil 5, D3; re-saisonwechsel.md, „Auswertung D3“): Zweitligist mit
/// Konto −1.234.094 DM am 14. April steigt zwangsweise ab, Ereignisse zeigen Symbol 0 ohne Pfeile und keine Aufstiegsprämie
/// (`file16_9`). [APPROX] Nicht belegt sind die Einzelheiten aus dem Disassembly: genaue Grenze, Ausnahme Meister/Europacupsieger,
/// höchstens zwei je Liga, Bundesligist als Zwangsabsteiger, Reihenfolge der Plätze beim Einschub. Am 29. April
/// (Saisonwechsel, Datei 0x54C9F) prüft em.exe vor dem Austausch je Manager in Managerreihenfolge:
/// Konto &lt; −2.000.000 Drittel-DM (Datei 0x55F22–0x55F34, Grenze FFE1 7B80 hex, „gleich“ zählt nicht), weder Deutscher
/// Meister noch Europacupsieger (Statusbits 0x28, Datei 0x55F59), Bundesliga oder 2. Liga (Datei 0x55F74) und höchstens zwei je
/// Liga (Zähler im Rahmen, Datei 0x55F6B). Dann setzt es Bit 0x04 (Datei 0x55F87) und schreibt den Verein in die Gruppentabelle
/// der Aufstiegsrunde seiner Liga (Datei 0x55F93–0x560C5, <see cref="InsertForcedRelegation"/>). Der Austausch (<see
/// cref="RelegationExchange"/>) findet ihn dort als Absteiger. Bit 0x04 unterdrückt die Pfeile (Datei 0x56287–0x56334) und
/// zeigt im Fenster „Ereignisse“ Symbol 0 (Tabelle DGROUP 0x4B2E); auf die Prämien wirkt es nicht (Datei 0x270A8–0x270B2 prüft
/// nur Bit 0x01 und 0x02). docs/analyse/re-saisonwechsel.md §2.
/// </remarks>
public sealed partial class GameState
{
    /// <summary>Grenze des Zwangsabstiegs in Drittel-DM (em.exe rechnet in Drittel): −2.000.000 Drittel = −666.666,67 DM.</summary>
    public const long ForcedRelegationLimitThirds = -2_000_000;

    /// <summary>Höchstens so viele Zwangsabsteiger je Liga (Zähler [bp−0x18 + Liga], Datei 0x55F6B).</summary>
    public const int MaxForcedRelegationsPerLeague = 2;

    /// <summary>Konto unter der Grenze des Zwangsabstiegs (echt kleiner als −666.666,67 DM)?</summary>
    public static bool BelowForcedRelegationLimit(long money) => money * 3 < ForcedRelegationLimitThirds;

    /// <summary>Deutscher Meister oder Europacupsieger der Saison: kein Zwangsabstieg (Statusbits 0x08/0x20, Datei 0x55F59).</summary>
    public static bool ExemptFromForcedRelegation(SeasonRecord record, int club) =>
        record.Titles.TryGetValue(Title.GermanChampion, out var champion) && champion == club
        || record.Titles.TryGetValue(Title.EuropeanCupWinner, out var europe) && europe == club;

    /// <summary>
    /// Schreibt einen Zwangsabsteiger in die Gruppentabelle einer Aufstiegsrunde (Plätze 1–4, Index 0–3).
    /// </summary>
    /// <param name="places">Vereine der Gruppentabelle nach Platz; wird verändert.</param>
    /// <param name="count">Zwangsabsteiger dieser Liga vor diesem (0 oder 1).</param>
    /// <param name="club">Verein des Managers.</param>
    /// <param name="isUpper">Verein gehört zur oberen Liga der Gruppe (em.exe: Vereinsindex &lt; Ligagrenze DGROUP 0x7C1 + Liga).</param>
    /// <remarks>
    /// [APPROX] Disassembly ohne DOSBox-Beleg (K3), Datei 0x55FC8–0x560B4: Ab Platz <c>count + 3</c> (Index q = count + 2)
    /// sucht em.exe den nächsten Verein der oberen Liga bis Platz 4 und überschreibt ihn mit dem Zwangsabsteiger – der
    /// überschriebene Verein fehlt danach in der Tabelle und bleibt oben. Findet es keinen (q = 4), schreibt es den Verein von
    /// Platz <c>count + 3</c> auf Platz <c>count + 1</c> und den Zwangsabsteiger auf Platz <c>count + 3</c> (Datei
    /// 0x55FD6–0x5604D). Ein Verein der oberen Liga zählt auch dann als „oben“, wenn er selbst schon Zwangsabsteiger ist: Der
    /// zweite kann den ersten überschreiben, der dann trotz Bit 0x04 oben bleibt.
    /// </remarks>
    public static void InsertForcedRelegation(int[] places, int count, int club, Func<int, bool> isUpper)
    {
        var q = count + 2;
        int slot;
        while (true)
        {
            if (q == 4)
            {
                places[count] = places[count + 2];
                slot = count + 2;
                break;
            }
            slot = q++;
            if (isUpper(places[slot]))
                break;
        }
        places[slot] = club;
    }

    /// <summary>
    /// Austausch einer Aufstiegsrunde nach der Gruppentabelle: Aufsteiger (in die obere Liga) und Absteiger (in die untere),
    /// beide in Paarreihenfolge.
    /// </summary>
    /// <remarks>
    /// [ORIGINAL] Datei 0x56157–0x56235 (Teil 1 der Spur wettbewerbe-komplett): je Paar der nächste Verein der unteren Liga
    /// unter den Plätzen 1–2 (Zeiger [bp−4]) und der nächste Verein der oberen Liga unter den Plätzen 3–4 (Zeiger [bp−5]);
    /// fehlt einer, endet die Gruppe. Datei 0x5636D tauscht die beiden Vereinsplätze (0x36169). Ohne Zwangsabstieg ist das „die
    /// ersten beiden spielen oben“. [APPROX] (K3, ohne Beleg) Mit Zwangsabstieg kann derselbe Platz zweimal vorkommen
    /// (Zwangsabsteiger auf Platz 3 und 4); dann wirken beide Tausche nacheinander auf die Plätze, wie in em.exe. Das Ergebnis
    /// ist die Wanderung zwischen den Ligen. [APPROX] Das Remake hält keine Vereinsplätze; die Startordnung der Folgesaison
    /// (<see cref="LeagueTable.SuccessorOrder"/>) paart Auf- und Absteiger in Listenreihenfolge. Nur im Fall mit doppeltem Platz
    /// weicht sie ab (em.exe verschiebt dort auch einen Verein innerhalb der unteren Liga).
    /// </remarks>
    public static (List<int> Promoted, List<int> Relegated) RelegationExchange(IReadOnlyList<int> places, Func<int, bool> isUpper)
    {
        var at = RelegationPlaces(places, isUpper);
        int At(int place) => at.TryGetValue(place, out var c) ? c : place;
        var promoted = at.Keys.Where(isUpper).Select(At).Where(c => !isUpper(c)).ToList();
        var relegated = at.Keys.Where(c => !isUpper(c)).Select(At).Where(isUpper).ToList();
        return (promoted, relegated);
    }

    /// <summary>
    /// Platzabbildung des Austauschs einer Aufstiegsrunde: Platz (= Verein, der dort vor dem Austausch stand) → Verein nach allen
    /// Tauschen der Gruppe. Enthält nur getauschte Plätze in Paarreihenfolge (siehe <see cref="RelegationExchange"/>).
    /// </summary>
    public static Dictionary<int, int> RelegationPlaces(IReadOnlyList<int> places, Func<int, bool> isUpper)
    {
        var up = places.Take(2).Where(c => !isUpper(c)).ToList();
        var down = places.Skip(2).Take(2).Where(isUpper).ToList();
        var pairs = Math.Min(up.Count, down.Count);
        var at = new Dictionary<int, int>();
        int At(int place) => at.TryGetValue(place, out var c) ? c : place;
        for (var k = 0; k < pairs; k++)
        {
            var (upper, lower) = (down[k], up[k]);
            var club = At(upper);
            at[upper] = At(lower);
            at[lower] = club;
        }
        return at;
    }

    /// <summary>Auf- und Absteiger einer Aufstiegsrunde in den Saisonrückblick eintragen (ersetzt frühere Einträge der Gruppe).</summary>
    private void RecordRelegationGroup(SeasonRecord record, RelegationGroup group, IReadOnlyList<int> places)
    {
        var (promoted, relegated) = RelegationExchange(places, c => Clubs[c].League == group.Upper);
        record.PromotedInto[group.Upper].Clear();
        record.PromotedInto[group.Upper].AddRange(promoted);
        record.RelegatedInto[group.Lower].Clear();
        record.RelegatedInto[group.Lower].AddRange(relegated);
    }

    /// <summary>
    /// 29. April, vor den Ereignissen: Zwangsabstieg wegen Schulden. Liefert die Manager (Index) mit Statusbit 0x04.
    /// </summary>
    /// <remarks>
    /// [APPROX] Nur aktive Manager: Die Entlassung (8 Wochen unter −1,5 Mio. DM) stammt aus den Anforderungen, nicht aus
    /// em.exe; das Konto eines Entlassenen ändert sich nicht mehr und würde seinen früheren Verein jede Saison absteigen
    /// lassen. [APPROX] Das Remake prüft nach dem Tageswechsel der Finanzen des 29. April (<see cref="OnDayManager"/>); die
    /// Reihenfolge in em.exe zwischen Tagesbuchungen und Saisonwechsel ist nicht gelesen.
    /// </remarks>
    private HashSet<int> ApplyForcedRelegation(SeasonRecord record)
    {
        var forced = new HashSet<int>();
        if (WorldOnly)
            return forced;
        var tables = RelegationGroups.ToDictionary(g => g.Upper, g => g.Table(Clubs).Select(r => r.Club.Index).ToArray());
        var counts = new Dictionary<League, int>();
        for (var i = 0; i < Managers.Count; i++)
        {
            var manager = Managers[i];
            var club = manager.ClubIndex;
            if (manager.Dismissed || !BelowForcedRelegationLimit(manager.Money) || ExemptFromForcedRelegation(record, club))
                continue;
            if (Clubs[club].League is not { } league || league == League.Oberliga || !tables.TryGetValue(league, out var places))
                continue;
            var count = counts.GetValueOrDefault(league);
            if (count >= MaxForcedRelegationsPerLeague)
                continue;
            forced.Add(i);
            InsertForcedRelegation(places, count, club, c => Clubs[c].League == league);
            counts[league] = count + 1;
        }
        // [ORIGINAL] em.exe leert am 29. April alle Auf-/Absteigerlisten (Datei 0x560EB–0x5612E) und füllt sie im Austausch
        // aller Gruppen aus den Gruppentabellen dieses Tages (Datei 0x56137–0x563A4; re-saisonwechsel.md §1 Schritte 5–7).
        // Integrationsfehler R12: Vorher wurden nur die Gruppen mit Zwangsabsteiger neu eingetragen; die Gruppe
        // Bundesliga/2. Liga behielt die Listen vom 13. April, während ResolveDoubleMoves ihre Tabelle vom 29. April nahm.
        // Wich die Tabelle ab, ging ein Bundesligist in die Oberliga, ohne als Absteiger gezählt zu sein (Ligen 11/13/12).
        // Ohne Zwangsabsteiger bleibt alles beim 13. April (gleiche Tabelle, solange die Ergebnisse unverändert sind).
        if (counts.Count > 0)
            foreach (var group in RelegationGroups.Where(g => tables.ContainsKey(g.Upper)))
                RecordRelegationGroup(record, group, tables[group.Upper]);
        if (tables.TryGetValue(League.Bundesliga, out var upperPlaces))
            ResolveDoubleMoves(record, upperPlaces, c => Clubs[c].League == League.Bundesliga);
        return forced;
    }

    /// <summary>
    /// Zwangsabsteiger der 2. Liga, der zugleich über die Aufstiegsrunde in die Bundesliga aufsteigt (Review #441, SW-01):
    /// Er bleibt in der Bundesliga; sein Tauschpartner aus der Bundesliga geht statt seiner in die Oberliga.
    /// </summary>
    /// <param name="record">Saisonrückblick mit den Listen beider Aufstiegsrunden; wird verändert.</param>
    /// <param name="upperPlaces">Gruppentabelle der Aufstiegsrunde Bundesliga/2. Liga (nach dem Zwangsabstieg).</param>
    /// <param name="isBundesliga">Verein spielt in der Bundesliga.</param>
    /// <remarks>
    /// [ORIGINAL] Fall ohne doppelten Platz belegt: DOSBox-Lauf FILE16 → FILE17 (Spur saisonwechsel Teil 5, D3; `file16_6`,
    /// `file16_8`, `file17_2`–`file17_4`): SV Bayreuth (2. Liga, Zwangsabsteiger) wird Zweiter der Aufstiegsrunde und spielt in der
    /// Bundesliga, der Bundesligist auf Platz 3 (EV Landshut, Paarpartner) in der Oberliga, Ligen 12/12/12
    /// (SaisonwechselDoppelwechselOriginalTests). Hergeleitet aus dem Disassembly (Review #441 SW-01; Datei 0x55F93 ff.: Gruppe nach
    /// Liga-Stufe 1, Austausch Gruppe 0 vor Gruppe 1, Datei 0x56137–0x5636D): em.exe tauscht Vereinsplätze (0x36169), nicht
    /// Vereine. Der erste Tausch (Bundesliga/2. Liga) setzt den Zwangsabsteiger auf den Bundesligaplatz seines Partners und den
    /// Partner auf seinen Zweitligaplatz; der zweite Tausch (2. Liga/Oberliga) bewegt diesen Platz – also den Partner – in die
    /// Oberliga. Vorher stand der Verein in beiden Listen, <see cref="SeasonRecord.NextLeague"/> nahm die Bundesliga, und die
    /// Ligen hatten 12/13/11 Vereine. Jetzt bleiben 12/12/12, und jeder Verein steht in höchstens einer Liste.
    /// Review #458, SW-06: Welcher Verein in die Oberliga geht, folgt der Platzabbildung des ersten Tauschs
    /// (<see cref="RelegationPlaces"/>): der Verein, der danach auf dem Platz des Zweitligisten steht. Ohne doppelten Platz ist
    /// das der Absteiger mit demselben Paarindex. [APPROX] Ohne DOSBox-Beleg: Mit doppeltem Platz (Bundesliga-Zwangsabsteiger
    /// auf Platz 3 und 4) kann es ein Zweitligist sein, der in der 2. Liga nur den Platz wechselt; dann bleibt der
    /// Zwangsabsteiger in der 2. Liga, wenn er nicht aufsteigt. [ORIGINAL] Anzeige und Startordnung (Teil 6, D5): siehe
    /// <see cref="ShownRelegatedInto"/> und <see cref="FirstExchangeRelegatedInto"/> – die betroffenen Vereine erhalten den Platz
    /// ihres Partners (`file16_8`, `file17_3`).
    /// </remarks>
    public static void ResolveDoubleMoves(SeasonRecord record, IReadOnlyList<int> upperPlaces, Func<int, bool> isBundesliga)
    {
        var at = RelegationPlaces(upperPlaces, isBundesliga);
        var down = record.RelegatedInto[League.ZweiteLiga];
        var oberliga = record.RelegatedInto[League.Oberliga];
        for (var i = 0; i < oberliga.Count; i++)
        {
            if (!at.TryGetValue(oberliga[i], out var moved) || moved == oberliga[i])
                continue;
            oberliga[i] = moved; // der Platz des Zweitligisten geht in die Oberliga, mit dem Verein, der jetzt dort steht
            down.Remove(moved);  // ein Bundesligist geht damit zwei Ligen tiefer
        }
    }

    /// <summary>
    /// Doppelwechsel des Saisonrückblicks: Bundesligist <c>Club</c>, der nach <see cref="ResolveDoubleMoves"/> in die Oberliga
    /// geht, sein Eintrag in <see cref="SeasonRecord.RelegatedInto"/> der Oberliga (<c>OberligaIndex</c>) und sein Paarindex im
    /// ersten Tausch Bundesliga/2. Liga (<c>PairIndex</c>, −1 = unbekannt).
    /// </summary>
    /// <remarks>
    /// Paarindex: Der erste Tausch paart <c>down[k]</c> mit <c>up[k]</c> (<see cref="RelegationPlaces"/>); <c>up[k]</c> ist der
    /// Zwangsabsteiger, erkennbar am Ereignis <see cref="SeasonEvent.ForcedRelegation"/> seines Managers. [APPROX] Ohne
    /// Ereignis (synthetische Rückblicke) oder bei zwei Zwangsabsteigern in einer Gruppe in Reihenfolge der Manager.
    /// </remarks>
    private static List<(int Club, int OberligaIndex, int PairIndex)> DoubleMoves(SeasonRecord record)
    {
        var moves = new List<(int, int, int)>();
        if (!record.FinalTables.TryGetValue(League.Bundesliga, out var bundesliga))
            return moves;
        var up = record.PromotedInto[League.Bundesliga];
        var pairs = record.Managers.Where(m => m.Events.Contains(SeasonEvent.ForcedRelegation))
            .Select(m => up.IndexOf(m.Club)).Where(k => k >= 0).Distinct().ToList();
        var oberliga = record.RelegatedInto[League.Oberliga];
        for (var i = 0; i < oberliga.Count; i++)
        {
            if (!bundesliga.Contains(oberliga[i]))
                continue;
            var k = pairs.Count > 0 ? pairs[0] : -1;
            if (pairs.Count > 0)
                pairs.RemoveAt(0);
            moves.Add((oberliga[i], i, k));
        }
        return moves;
    }

    private static IReadOnlyList<int> WithDoubleMoves(SeasonRecord record, League league, Func<(int Club, int OberligaIndex, int PairIndex), int?> entry)
    {
        var list = record.RelegatedInto[league];
        if (league != League.ZweiteLiga)
            return list;
        var moves = DoubleMoves(record);
        if (moves.Count == 0)
            return list;
        var result = list.ToList();
        foreach (var move in moves.OrderBy(m => m.PairIndex < 0 ? int.MaxValue : m.PairIndex))
            if (entry(move) is { } club)
                result.Insert(move.PairIndex < 0 ? result.Count : Math.Min(move.PairIndex, result.Count), club);
        return result;
    }

    /// <summary>
    /// Absteiger in die Liga, wie das Fenster „Saisonende“ sie zeigt (rot, Pfeil nach unten).
    /// </summary>
    /// <remarks>
    /// [ORIGINAL] Spur saisonwechsel Teil 6 (D5 = A, DOSBox-Beleg `file16_8_saisonende`): em.exe zeigt Plätze, nicht Vereine.
    /// Der Eintrag „Absteiger in die 2. Liga“ ist der Platz, auf den der erste Tausch den Bundesligisten gesetzt hat; der zweite
    /// Tausch (2. Liga/Oberliga) belegt ihn mit dem Aufsteiger aus der Oberliga, der mit diesem Bundesligisten gepaart ist
    /// (<see cref="SeasonRecord.PromotedInto"/> der 2. Liga, gleicher Index wie in <see cref="SeasonRecord.RelegatedInto"/> der
    /// Oberliga). So steht SC Riessersee in der Spalte „Zweite Liga“ rot und grün, EV Landshut nur in der Oberliga.
    /// Ohne Doppelwechsel und für die übrigen Ligen gleich <see cref="SeasonRecord.RelegatedInto"/>.
    /// </remarks>
    public static IReadOnlyList<int> ShownRelegatedInto(SeasonRecord record, League league) =>
        WithDoubleMoves(record, league, m => record.PromotedInto[League.ZweiteLiga] is var p && m.OberligaIndex < p.Count ? p[m.OberligaIndex] : null);

    /// <summary>
    /// Absteiger in die Liga nach dem ersten Tausch (vor <see cref="ResolveDoubleMoves"/>): für die 2. Liga mit dem Bundesligisten,
    /// der danach weiter in die Oberliga geht. Grundlage der Platzpaare (Startordnung, Satzfolge): Der Aufsteiger in die Bundesliga
    /// übernimmt dessen Bundesligaplatz (Datei 0x36169; Beleg `file17_2`).
    /// </summary>
    public static IReadOnlyList<int> FirstExchangeRelegatedInto(SeasonRecord record, League league) =>
        WithDoubleMoves(record, league, m => m.Club);
}
