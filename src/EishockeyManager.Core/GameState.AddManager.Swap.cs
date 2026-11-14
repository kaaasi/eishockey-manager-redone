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
public sealed partial class GameState
{
    /// <summary>
    /// Zielplatz eines fest gewählten Vereins in der Start-Liga des Spiels. [ORIGINAL] Datei 0x29C3B–0x29C75: behalten, wenn
    /// Start-Liga ≠ Bundesliga und der Verein in ihr spielt (0x34FD5(v) / 2 = L, Satz ≤ 35). Sonst Zufallsplatz der Start-Liga
    /// (Datei 0x29C78 <c>random([0x7C0+L], [0x7C0+L]+[0x7B1+L]−1)</c>), neu gezogen, solange dort der Verein eines Managers mit
    /// kleinerer Nummer steht (Datei 0x29C9B–0x29CC3: im Hinzufügen-Modus alle vorhandenen und die zuvor angelegten neuen) oder
    /// 0x45CD0(r, 1) ≠ 0 (Datei 0x29CCC): Zeile 1 der Tabelle DGROUP 0x5CFB, nach der Auslosung 32 Einträge = die acht Plätze
    /// des Europapokals und 24 × 0 – ausgeschlossen sind die Europapokal-Teilnehmer und Platz 0 (erster Bundesliga-Satz).
    /// Bei Start-Liga Bundesliga ist der eigene Platz möglich (dann kein Tausch). Reine Abfrage bis auf den Zufall.
    /// </summary>
    /// <returns>Verein auf dem gezogenen Platz (auch <paramref name="club"/> selbst) oder null, wenn der Verein bleibt.</returns>
    internal int? DrawStartLeaguePlace(int club, IReadOnlySet<int> earlierManagerClubs, Random random)
    {
        var league = Setup.StartLeague;
        if (league != League.Bundesliga && Clubs[club].League == league)
            return null;
        var slots = LeagueSlots(league);
        var first = LeagueSlots(League.Bundesliga) is { Count: > 0 } top ? top[0] : -1;
        var europe = EuropeanCup?.Draw ?? [];
        bool Excluded(int c) => earlierManagerClubs.Contains(c) || c == first || europe.Contains(c);
        if (slots.Count == 0 || slots.All(Excluded))
            return null; // [APPROX] Das Original zöge endlos; mit 12 Plätzen je Liga nicht erreichbar.
        int partner;
        do
            partner = slots[TransferRules.Random16(random, 0, slots.Count - 1)];
        while (Excluded(partner));
        return partner;
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
