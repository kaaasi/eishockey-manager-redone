using System.Runtime.CompilerServices;

namespace EishockeyManager.Core;

/// <summary>
/// Platztausch eines hinzukommenden Managers in die Start-Liga (Spur neuer-manager, #847, R18-E5 = A).
/// </summary>
/// <remarks>
/// [ORIGINAL] Belege in <c>docs/reverse-engineering.md</c>, Abschnitt „Neuer Manager mitten in der Saison“: Die Spielvorbereitung
/// 0x28165 durchläuft im Hinzufügen-Modus für jeden neuen Manager dieselbe Prüfung wie beim Spielstart (Datei 0x29C3B–0x29CF7)
/// und tauscht mit 0x36169(v, r, 0x80) nur die Kennung des Vereinssatzes (Name, Manager, Logo, Farbcode), die Spieler (+0x1D)
/// und die Managerzuordnung (DGROUP 0x966F + 0x1AF · m). Tabellensatz, Serien, Teamwerte, Spielplan, Ergebnisse, Pokale und
/// Titel bleiben am Platz.
/// </remarks>
/// <summary>
/// Platzwahl in der Start-Liga (Aufräumkonzept K13, Spur saison-geld Teil 3): eine Umsetzung für Spielstart
/// (<see cref="NewGame.PlaceInStartLeague"/>) und Hinzufügen (<see cref="GameState.DrawStartLeaguePlace"/>).
/// </summary>
/// <remarks>
/// [ORIGINAL] Spielstart und Hinzufügen laufen in der Spielvorbereitung 0x28165 durch denselben Code, Datei 0x29C3B–0x29CF7 (Beleg
/// <c>k13_platzwahl.py</c>, <c>docs/archiv/analyse/re-abweichungen.md</c> K13): behalten, wenn Start-Liga L ≠ Bundesliga, Liga des Vereins
/// = L (0x34FD5(v) / 2) und Satz v ≤ DGROUP 0x7CB (35); sonst Satz r = random(Basis L, Basis L + 11) (Datei 0x29C78), neu gezogen,
/// solange r der Verein eines Managers mit kleinerer Nummer ist (Datei 0x29C9B–0x29CC3) oder 0x45CD0(r, 1) ≠ 0 (Datei 0x29CCC); dann
/// Tausch 0x36169(v, r, 0x80) – auch bei r = v.
/// <para>[ORIGINAL] 0x45CD0(r, 1) durchsucht die ersten 2 · DGROUP (0x3B1 + DGROUP 0x5CF4) Einträge ab Zeile 1 der Tabelle
/// DGROUP 0x5CFB (Tabelle 0x3B1 = 32, 16, 8, …). DGROUP 0x5CF4 (Runde des Europapokals) schreiben nur die Auslosung 0x341AB:
/// 0 zu Beginn (Datei 0x341CC), 1 danach (Datei 0x346B3). Die Rundenfortschaltung 0x3489D (Datei 0x348DB/0x34AE3) läuft nur für
/// Wettbewerb 0: ihre Schleife in 0x34B76 (Datei 0x34F10–0x34F71) beginnt mit 0 und endet vor 1. Beim Spielstart ist die Tabelle noch
/// leer (Runde 0, 64 Einträge 0 → nur Satz 0), nach der Auslosung stehen in Zeile 1 die acht Plätze und dahinter 24 × 0 (Runde 1,
/// 32 Einträge → die acht Plätze und Satz 0). Der in <c>re-abweichungen.md</c> K13 vermutete Randfall (Runde 3 bzw. 4: Satz 0 bzw.
/// hintere Plätze wieder frei) tritt damit nicht ein.</para>
/// <para>Das Remake zieht Satzplätze: <c>clubAt(r)</c> ist der Verein auf Satz r (Spielstart: Startmischung, sonst
/// <see cref="GameState.LeagueSlots"/>); die Europapokal-Plätze führt es als Vereinsindizes.</para>
/// </remarks>
internal static class StartLeaguePlace
{
    /// <summary>[ORIGINAL] Sätze je Liga (DGROUP 0x7B1 = 12) und letzter Ligasatz (DGROUP 0x7CB = 35).</summary>
    public const int LeagueSize = 12, LastLeagueRecord = 35;

    /// <summary>[ORIGINAL] Einträge einer Zeile der Tabelle DGROUP 0x5CFB (32 Byte).</summary>
    public const int CupLineLength = 32;

    /// <summary>[ORIGINAL] Behält der Verein auf Satz <paramref name="record"/> seinen Platz (Datei 0x29C3B–0x29C75)?</summary>
    public static bool Keeps(League start, League? clubLeague, int record) =>
        start != League.Bundesliga && clubLeague == start && record <= LastLeagueRecord;

    /// <summary>
    /// [ORIGINAL] 0x45CD0(r, 1): Steht Satz <paramref name="record"/> unter den ersten 2 · (32 ≫ Runde) Einträgen ab Zeile 1? Zeile 1 =
    /// <paramref name="cupLine"/> (Vereine) und dahinter leere Einträge (Satz 0); Zeile 2 ist im Remake immer leer.
    /// </summary>
    public static bool InCupLine(int record, int club, IReadOnlyList<int> cupLine, int cupRound)
    {
        var searched = 2 * (CupLineLength >> Math.Clamp(cupRound, 0, 5));
        var listed = Math.Min(searched, cupLine.Count);
        for (var i = 0; i < listed; i++)
        {
            if (cupLine[i] == club)
                return true;
        }
        return record == 0 && searched > listed;
    }

    /// <summary>
    /// Zufallssatz der Start-Liga unter <paramref name="count"/> Sätzen ab Satz <paramref name="first"/> ([ORIGINAL] 12; Datei 0x29C78–0x29CCC) aus dem gemeinsamen Strom
    /// (<see cref="TransferRules.Random16"/>, Spur zufallsstrom N3 = B).
    /// </summary>
    /// <returns>Gezogener Satz oder null, wenn alle Sätze ausgeschlossen sind ([APPROX] das Original zöge endlos; mit 12 Sätzen je Liga
    /// und höchstens vier Managern nicht erreichbar).</returns>
    public static int? Draw(int first, int count, Func<int, int> clubAt, IReadOnlySet<int> earlierManagerClubs, IReadOnlyList<int> cupLine,
        int cupRound, Random random)
    {
        bool Excluded(int record)
        {
            var club = clubAt(record);
            return earlierManagerClubs.Contains(club) || InCupLine(record, club, cupLine, cupRound);
        }
        if (count <= 0 || Enumerable.Range(first, count).All(Excluded))
            return null;
        int record;
        do
            record = TransferRules.Random16(random, first, first + count - 1);
        while (Excluded(record));
        return record;
    }
}

public sealed partial class GameState
{
    /// <summary>
    /// Zielplatz eines fest gewählten Vereins in der Start-Liga des Spiels (Hinzufügen-Modus). [ORIGINAL] Datei 0x29C3B–0x29CF7 wie
    /// beim Spielstart (<see cref="StartLeaguePlace"/>): Zeile 1 der Tabelle DGROUP 0x5CFB hält nach der Auslosung die acht Plätze des
    /// Europapokals (Runde 1), vorher ist sie leer (Runde 0). Bei Start-Liga Bundesliga ist der eigene Platz möglich (dann kein Tausch).
    /// Reine Abfrage bis auf den Zufall.
    /// </summary>
    /// <returns>Verein auf dem gezogenen Platz (auch <paramref name="club"/> selbst) oder null, wenn der Verein bleibt.</returns>
    internal int? DrawStartLeaguePlace(int club, IReadOnlySet<int> earlierManagerClubs, Random random)
    {
        var league = Setup.StartLeague;
        if (Clubs[club].League is { } own && StartLeaguePlace.Keeps(league, own, RecordOf(club, own)))
            return null;
        var slots = LeagueSlots(league);
        var first = (int)league * StartLeaguePlace.LeagueSize;
        int ClubAt(int record) => slots[record - first];
        var europe = EuropeanCup?.Draw ?? [];
        var record = StartLeaguePlace.Draw(first, slots.Count, ClubAt, earlierManagerClubs, europe, europe.Count > 0 ? 1 : 0, random);
        return record is { } r ? ClubAt(r) : null;
    }

    /// <summary>Satz eines Vereins: Basis der Liga + Stelle in der Satzfolge (<see cref="LeagueSlots"/>).</summary>
    private int RecordOf(int club, League league)
    {
        var at = LeagueSlots(league).ToList().IndexOf(club);
        return at < 0 ? int.MaxValue : (int)league * StartLeaguePlace.LeagueSize + at;
    }

    /// <summary>
    /// Tausch der Kennung zweier Vereinsplätze mitten im Spiel. [ORIGINAL] 0x36169(v, r, 0x80), Datei 0x36169–0x3672C: Spieler
    /// (Poolfeld +0x1D, Datei 0x36173–0x361E2) und Manager (Datei 0x361E4–0x3624B) wechseln den Platz, im Vereinssatz nur
    /// Name, Manager, Logo und Farbcode (Datei 0x3628F–0x363F6); danach Sprung ans Ende (Datei 0x364AE–0x364B4).
    /// <para>Im Remake trägt der Platz den Index: Die beiden <see cref="Club"/>-Objekte tauschen ihren Platz in <see cref="Clubs"/>
    /// – damit wandern Name, Logo, Spieler, Aufstellung, Taktik und alle Objektverweise (Markt, Gebote, Leihen, Angebote) –, und
    /// die Felder des Platzes tauschen zurück (<see cref="Club.SwapPlaceFields"/>). Alles, was über den Index läuft (Ergebnisse,
    /// Spielpläne, Satzfolge, Pokale, Titel, Ewige Tabelle, Statistik der Ränge), bleibt am Platz. Poolplätze, Manager und die
    /// Spielvorbereitung tauschen den Index.</para>
    /// <para>[APPROX] Die zuletzt angezeigte Zeile der Taktik-Automatik (Vereinsfelder 0x26–0x2B/0x35–0x3A, im Remake in
    /// <see cref="Tactics"/>) wandert mit; das Original schreibt sie vor dem nächsten Spiel ohnehin neu (Datei 0x8365).</para>
    /// <para>Gleichstand v = r: wirkungslos wie im Original.</para>
    /// </summary>
    /// <param name="pending">Noch nicht angelegte Zeilen desselben „Los jetzt!“ (ihr gewählter Verein tauscht mit).</param>
    internal void SwapClubPlaces(int v, int r, IEnumerable<ManagerSetup>? pending = null)
    {
        if (v == r)
            return;
        var clubs = (IList<Club>)Clubs;
        var (a, b) = (clubs[v], clubs[r]);
        Club.SwapPlaceFields(a, b);
        clubs[v] = b;
        clubs[r] = a;

        foreach (var slot in TransferMarket.Pool)
            slot.Club = slot.Club == v ? r : slot.Club == r ? v : slot.Club;
        foreach (var manager in Managers)
            manager.ClubIndex = manager.ClubIndex == v ? r : manager.ClubIndex == r ? v : manager.ClubIndex;
        foreach (var setup in Setup.Managers.Concat(pending ?? []))
            setup.ClubIndex = setup.ClubIndex == v ? r : setup.ClubIndex == r ? v : setup.ClubIndex;
    }

    /// <summary>Ist die Vereinsliste für <see cref="SwapClubPlaces"/> veränderbar (Liste oder Feld)?</summary>
    private bool ClubsSwappable => Clubs is List<Club> or Club[];
}

public sealed partial class Club
{
    /// <summary>
    /// Tauscht die Felder, die im Original am Vereinsplatz bleiben (0x36169 mit Schalter 0x80): Index, Datenbankstärke (Grundlage
    /// der Teamwerte), Liga, Teamwerte 0x17–0x33 (<see cref="TeamValues"/>), Platzverlauf DGROUP 0x828B und Tabellenfeld 0x8288
    /// (Tabellensatz 0x8257), Strafminuten der Fair-Play-Wertung (Tabellensatz) und Serien DGROUP 0xB57B.
    /// </summary>
    internal static void SwapPlaceFields(Club a, Club b)
    {
        (IndexField(a), IndexField(b)) = (b.Index, a.Index);
        (StrengthField(a), StrengthField(b)) = (b.Strength, a.Strength);
        (a.League, b.League) = (b.League, a.League);
        (a.TeamValues, b.TeamValues) = (b.TeamValues, a.TeamValues);
        (a._tableRank, b._tableRank) = (b._tableRank, a._tableRank);
        (a.LeaguePenaltyMinutes, b.LeaguePenaltyMinutes) = (b.LeaguePenaltyMinutes, a.LeaguePenaltyMinutes);
        SwapContents(a._rankSlots, b._rankSlots);
        SwapContents(a.SeriesCurrent, b.SeriesCurrent);
    }

    private static void SwapContents(int[] x, int[] y)
    {
        for (var i = 0; i < Math.Min(x.Length, y.Length); i++)
            (x[i], y[i]) = (y[i], x[i]);
    }

    // Index und Stärke sind „required init“ (GameState.cs, gemeinsame Datei). Der Tausch setzt die Hintergrundfelder direkt; deren
    // Namen trägt auch der Spielstand (SaveGame, Feldnamen ohne „k__BackingField“), das Schema bleibt gleich.
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "<Index>k__BackingField")]
    private static extern ref int IndexField(Club club);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "<Strength>k__BackingField")]
    private static extern ref int StrengthField(Club club);
}
