namespace EishockeyManager.Core;

/// <summary>
/// Bisherige Begegnungen je Manager und Gegner (Infoleiste, zweite Zeile; Spur optionen-wirkung, Teil 3).
/// [ORIGINAL] Tabelle DGROUP 0xAB7B, Index Manager · 0x280 + Gegner · 10 + Platz · 2 + Seite: je Gegner fünf Plätze für
/// eigene Heimspiele (Seite 0) und fünf für Auswärtsspiele (Seite 1); ein Byte = eigene Tore + 16 · Gegentore, 0xFF = leer.
/// Anfangswert überall 0xFF (Datei 0x43760–0x437C4 beim Programmstart; der Spielstand trägt sie weiter), danach wird nie gelöscht – die Tabelle gilt über alle
/// Saisons. Eintrag: 0x4F5A8, aufgerufen von der Tabelleneintragung 0x50283 für beide Vereine, wenn sie einen Manager
/// haben (Vereinsfeld +0x16). Belege: <c>docs/reverse-engineering.md</c>, „Infoleiste 0x43E21“.
/// <para>Integration Runde 9: Dieselbe Tabelle liest das Gegner-Fenster als Kopf-an-Kopf-Liste (Spur wettbewerbe-komplett,
/// <see cref="GameState.HeadToHead"/>). Es gibt nur einen Speicher (<c>GameState._headToHead</c>, Platz = Managernummer) und
/// nur einen Schreiber (<see cref="RecordAt"/>) für Liga- und Länderspiele.</para>
/// </summary>
public static class MeetingHistory
{
    /// <summary>[ORIGINAL] Gegner je Manager (Vereinsindex 0 … 63).</summary>
    public const int Opponents = 64;

    /// <summary>[ORIGINAL] Plätze je Seite.</summary>
    public const int Slots = 5;

    /// <summary>Bytes je Manager (0x280).</summary>
    public const int TableSize = Opponents * Slots * 2;

    /// <summary>Leerer Platz.</summary>
    public const int Empty = 0xFF;

    /// <summary>[ORIGINAL] Erster Vereinsindex der Nationalteams (DGROUP 0x6AD = 52): eine gemeinsame Liste ohne Heim/Auswärts.</summary>
    public const int FirstNation = 52;

    private static int Index(int opponent, int slot, int side) => opponent * Slots * 2 + slot * 2 + side;

    /// <summary>
    /// Ergebnis eintragen. [ORIGINAL] 0x4F5A8: ersten freien Platz der Seite suchen; gegen Nationalteams (ab
    /// <see cref="FirstNation"/>) ist die Seite immer erst 0, dann 1 (zehn Plätze am Stück). Ist alles voll, rücken die
    /// Einträge um einen Platz nach vorn und das neue Ergebnis kommt auf Platz 4. Ein Byte fasst je Zahl nur vier Bit
    /// (<c>eigene + Gegentore &lt;&lt; 4</c>, ohne Begrenzung – so auch im Original).
    /// </summary>
    public static void Record(IList<int> table, int opponent, bool home, int goalsFor, int goalsAgainst)
    {
        if (opponent is < 0 or >= Opponents)
            return;
        RecordAt(table, Index(opponent, 0, 0), opponent >= FirstNation, home ? 0 : 1, goalsFor, goalsAgainst);
    }

    /// <summary>
    /// Ein Ergebnis in die zehn Bytes eines Gegners ab <paramref name="offset"/> schreiben (Seite 0 = gerade, 1 = ungerade
    /// Plätze). [ORIGINAL] 0x4F5A8 (Manager, Verein, Seite, lo = eigene Tore, hi = Gegentore); Länder: erst Seite 0, dann 1.
    /// </summary>
    public static void RecordAt(IList<int> table, int offset, bool nation, int side, int goalsFor, int goalsAgainst)
    {
        if (nation)
            side = 0;
        var slot = FreeSlot(table, offset, side);
        if (nation && slot == Slots)
        {
            side = 1;
            slot = FreeSlot(table, offset, side);
        }
        if (slot == Slots)
        {
            if (nation)
            {
                // [ORIGINAL] 0x4F650–0x4F6E5: Seite 0 rückt vor, Platz 0 der Seite 1 wandert auf Platz 4 der Seite 0.
                for (var i = 0; i < Slots - 1; i++)
                    table[offset + i * 2] = table[offset + (i + 1) * 2];
                table[offset + (Slots - 1) * 2] = table[offset + 1];
            }
            for (var i = 0; i < Slots - 1; i++)
                table[offset + i * 2 + side] = table[offset + (i + 1) * 2 + side];
            slot = Slots - 1;
        }
        table[offset + slot * 2 + side] = (byte)(goalsFor + (goalsAgainst << 4));
    }

    private static int FreeSlot(IList<int> table, int offset, int side)
    {
        var slot = 0;
        while (slot < Slots && table[offset + slot * 2 + side] != Empty)
            slot++;
        return slot;
    }

    /// <summary>
    /// Einträge der Infoleiste in Anzeigereihenfolge. [ORIGINAL] 0x43E21 (Datei 0x44168–0x4438B): erst die Auswärtsspiele
    /// (Seite 1, „A “), dann die Heimspiele (Seite 0, „H “), je Seite höchstens zwei, neueste zuerst (Platz 4 → 0);
    /// eigene Tore vor den Gegentoren. Gegen Nationalteams ohne „A “/„H “ (Prüfung 0x128:0x20, Datei 0x441F2,
    /// wie beim Heim-/Auswärtssymbol, Datei 0x440AE).
    /// </summary>
    public static IReadOnlyList<string> Entries(IList<int> table, int opponent)
    {
        var entries = new List<string>();
        if (opponent is < 0 or >= Opponents)
            return entries;
        var nation = opponent >= FirstNation;
        foreach (var side in new[] { 1, 0 })
        {
            var shown = 0;
            for (var slot = Slots - 1; slot >= 0 && shown < 2; slot--)
            {
                var value = table[Index(opponent, slot, side)];
                if (value == Empty)
                    continue;
                var prefix = nation ? "" : side == 1 ? "A " : "H ";
                entries.Add($"{prefix}{value & 0xF}:{value >> 4}");
                shown++;
            }
        }
        return entries;
    }
}

public sealed partial class GameState
{
    /// <summary>
    /// Ergebnis in die Begegnungstabellen der beteiligten Manager eintragen. [ORIGINAL] 0x50283 (Datei 0x50289–0x5030B):
    /// Heimverein mit Manager (Vereinsfeld +0x16) → dessen Tabelle, Gegner = Gast, Seite 0, Tore Heim:Gast; Gast mit Manager
    /// → Seite 1, Tore Gast:Heim. Das Nationalteam DGROUP 0x6AC schreibt immer in die Tabelle von Manager 0 (Datei
    /// 0x502B4–0x502C8, <c>mov cl, 1</c>) – dieselbe, die das Bundestrainermenü liest (<see cref="MeetingEntries"/>).
    /// [ORIGINAL] 0x6AC ist Deutschland (Startwert 56); einziger Schreiber ist die WM-Auslosung (Datei 0x35FAC), die den
    /// Index nach dem Tausch der Ländersätze nachführt. Im Remake behalten die Länder ihren Index – also immer
    /// <see cref="NationalTeams.Germany"/>, ob ein Bundestrainer ernannt ist oder nicht (#344).
    /// <para>Aufrufer wie im Original (Spur wettbewerbe-spiel, Teil 2, A111): Liga (<see cref="PlayNextMatchDay"/>, 0x50B45),
    /// DEB-Pokal (<see cref="DebCup.PlayDay"/>, Datei 0x34C5C), WM-Gruppe und -Endrunde, Freundschaftsspiel (über
    /// <see cref="EnterNationalResult"/>, Datei 0x30942, 0x3108B, 0x303BA), Play-offs (Datei 0x3165B) und Aufstiegsrunden
    /// (0x30852 mit Wettbewerb 1) über <see cref="RecordMeetings"/>. Europapokal nicht: 0x30852 ruft 0x50283 nur für
    /// Wettbewerb 0 und 1 (Datei 0x3091C–0x30942). Pokal- und Play-off-Ergebnisse ohne Marken (Endstand).</para>
    /// </summary>
    public void RecordMeeting(int home, int away, int homeGoals, int awayGoals)
    {
        if (TableOwner(home) is { } h)
            MeetingHistory.Record(MeetingTable(h), away, home: true, homeGoals, awayGoals);
        if (TableOwner(away) is { } a)
            MeetingHistory.Record(MeetingTable(a), home, home: false, awayGoals, homeGoals);
    }

    /// <summary>Mehrere Spiele eintragen, je Spiel einmal <see cref="RecordMeeting"/> (Endstand ohne Marken).</summary>
    public void RecordMeetings(IEnumerable<KnockoutMatch> games)
    {
        foreach (var g in games)
            RecordMeeting(g.Home, g.Away, g.HomeGoals, g.AwayGoals);
    }

    /// <summary>
    /// Begegnungstabelle eines Managers: 0x280 Bytes wie DGROUP 0xAB7B + 0x280 · Manager, im gemeinsamen Speicher der
    /// Kopf-an-Kopf-Liste (Integration Runde 9).
    /// </summary>
    public ArraySegment<int> MeetingTable(int manager) =>
        new(_headToHead, manager * MeetingHistory.TableSize, MeetingHistory.TableSize);

    /// <summary>Begegnungstabelle eines Managers (<see cref="MeetingTable(int)"/>).</summary>
    public ArraySegment<int> MeetingTable(Manager manager) => MeetingTable(ManagerIndex(manager));

    private int ManagerIndex(Manager manager)
    {
        for (var i = 0; i < Managers.Count; i++)
            if (ReferenceEquals(Managers[i], manager))
                return i;
        throw new ArgumentException("Manager gehört nicht zu diesem Spiel.", nameof(manager));
    }

    private int? TableOwner(int club)
    {
        if (club == NationalTeams.Germany)
            return Managers.Count > 0 ? 0 : null;
        for (var i = 0; i < Managers.Count && i < HeadToHeadSlots; i++)
            if (Managers[i].ClubIndex == club)
                return i;
        return null;
    }

    /// <summary>
    /// Bisherige Begegnungen des Managers am Zug mit einem Gegner (Infoleiste). [ORIGINAL] 0x43E21 (Datei 0x4417C–0x4418F):
    /// im Bundestrainermenü (DGROUP 0xA70E = 5) die Tabelle von Manager 0, sonst die des Managers am Zug.
    /// </summary>
    public IReadOnlyList<string> MeetingEntries(int opponent)
    {
        var manager = NationalCoachMenu ? Managers[0] : ActiveManager;
        return MeetingHistory.Entries(MeetingTable(manager), opponent);
    }
}
