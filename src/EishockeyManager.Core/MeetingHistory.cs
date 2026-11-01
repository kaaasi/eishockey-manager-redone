namespace EishockeyManager.Core;

/// <summary>
/// Bisherige Begegnungen je Manager und Gegner (Infoleiste, zweite Zeile; Spur optionen-wirkung, Teil 3).
/// [ORIGINAL] Tabelle DGROUP 0xAB7B, Index Manager · 0x280 + Gegner · 10 + Platz · 2 + Seite: je Gegner fünf Plätze für
/// eigene Heimspiele (Seite 0) und fünf für Auswärtsspiele (Seite 1); ein Byte = eigene Tore + 16 · Gegentore, 0xFF = leer.
/// Anfangswert überall 0xFF (Datei 0x43760–0x437C4 beim Programmstart; der Spielstand trägt sie weiter), danach wird nie gelöscht – die Tabelle gilt über alle
/// Saisons. Eintrag: 0x4F5A8, aufgerufen von der Tabelleneintragung 0x50283 für beide Vereine, wenn sie einen Manager
/// haben (Vereinsfeld +0x16). Belege: <c>docs/reverse-engineering.md</c>, „Infoleiste 0x43E21“.
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

    /// <summary>Neue, leere Tabelle eines Managers.</summary>
    public static int[] NewTable() => Enumerable.Repeat(Empty, TableSize).ToArray();

    private static int Index(int opponent, int slot, int side) => opponent * Slots * 2 + slot * 2 + side;

    /// <summary>
    /// Ergebnis eintragen. [ORIGINAL] 0x4F5A8: ersten freien Platz der Seite suchen; gegen Nationalteams (ab
    /// <see cref="FirstNation"/>) ist die Seite immer erst 0, dann 1 (zehn Plätze am Stück). Ist alles voll, rücken die
    /// Einträge um einen Platz nach vorn und das neue Ergebnis kommt auf Platz 4. Ein Byte fasst je Zahl nur vier Bit
    /// (<c>eigene + Gegentore &lt;&lt; 4</c>, ohne Begrenzung – so auch im Original).
    /// </summary>
    public static void Record(int[] table, int opponent, bool home, int goalsFor, int goalsAgainst)
    {
        if (opponent is < 0 or >= Opponents)
            return;
        var nation = opponent >= FirstNation;
        var side = nation ? 0 : home ? 0 : 1;
        var slot = FreeSlot(table, opponent, side);
        if (nation && slot == Slots)
        {
            side = 1;
            slot = FreeSlot(table, opponent, side);
        }
        if (slot == Slots)
        {
            if (nation)
            {
                // [ORIGINAL] 0x4F650–0x4F6E5: Seite 0 rückt vor, Platz 0 der Seite 1 wandert auf Platz 4 der Seite 0.
                for (var i = 0; i < Slots - 1; i++)
                    table[Index(opponent, i, 0)] = table[Index(opponent, i + 1, 0)];
                table[Index(opponent, Slots - 1, 0)] = table[Index(opponent, 0, 1)];
            }
            for (var i = 0; i < Slots - 1; i++)
                table[Index(opponent, i, side)] = table[Index(opponent, i + 1, side)];
            slot = Slots - 1;
        }
        table[Index(opponent, slot, side)] = (byte)(goalsFor + (goalsAgainst << 4));
    }

    private static int FreeSlot(int[] table, int opponent, int side)
    {
        var slot = 0;
        while (slot < Slots && table[Index(opponent, slot, side)] != Empty)
            slot++;
        return slot;
    }

    /// <summary>
    /// Einträge der Infoleiste in Anzeigereihenfolge. [ORIGINAL] 0x43E21 (Datei 0x44168–0x4438B): erst die Auswärtsspiele
    /// (Seite 1, „A “), dann die Heimspiele (Seite 0, „H “), je Seite höchstens zwei, neueste zuerst (Platz 4 → 0);
    /// eigene Tore vor den Gegentoren. Gegen Nationalteams ohne „A “/„H “ (Prüfung 0x128:0x20, Datei 0x441F2,
    /// wie beim Heim-/Auswärtssymbol, Datei 0x440AE).
    /// </summary>
    public static IReadOnlyList<string> Entries(int[] table, int opponent)
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

public sealed partial class Manager
{
    /// <summary>Bisherige Begegnungen (<see cref="MeetingHistory"/>), 0x280 Bytes wie DGROUP 0xAB7B + 0x280 · Manager.</summary>
    public int[] MeetingTable { get; init; } = MeetingHistory.NewTable();
}

public sealed partial class GameState
{
    /// <summary>
    /// Ergebnis in die Begegnungstabellen der beteiligten Manager eintragen. [ORIGINAL] 0x50283 (Datei 0x50289–0x5030B):
    /// Heimverein mit Manager → dessen Tabelle, Gegner = Gast, Seite 0, Tore Heim:Gast; Gast mit Manager → Seite 1, Tore
    /// Gast:Heim. Das Nationalteam des Bundestrainers (DGROUP 0x6AC) schreibt immer in die Tabelle von Manager 0 – dieselbe,
    /// die das Bundestrainermenü liest (<see cref="MeetingEntries"/>).
    /// [APPROX] Angeschlossen sind bisher nur die Ligaspiele (<see cref="PlayNextMatchDay"/>); Pokal-, Play-off- und
    /// WM-Spiele laufen im Original ebenfalls über 0x50283 und werden in der Integration angeschlossen (fremde Dateien).
    /// </summary>
    public void RecordMeeting(int home, int away, int homeGoals, int awayGoals)
    {
        if (TableOwner(home) is { } h)
            MeetingHistory.Record(h.MeetingTable, away, home: true, homeGoals, awayGoals);
        if (TableOwner(away) is { } a)
            MeetingHistory.Record(a.MeetingTable, home, home: false, awayGoals, homeGoals);
    }

    private Manager? TableOwner(int club)
    {
        if (NationalCoach is { } coach && coach.Nation == club)
            return Managers.Count > 0 ? Managers[0] : null;
        return Managers.FirstOrDefault(m => m.ClubIndex == club);
    }

    /// <summary>
    /// Bisherige Begegnungen des Managers am Zug mit einem Gegner (Infoleiste). [ORIGINAL] 0x43E21 (Datei 0x4417C–0x4418F):
    /// im Bundestrainermenü (DGROUP 0xA70E = 5) die Tabelle von Manager 0, sonst die des Managers am Zug.
    /// </summary>
    public IReadOnlyList<string> MeetingEntries(int opponent)
    {
        var manager = NationalCoachMenu ? Managers[0] : ActiveManager;
        return MeetingHistory.Entries(manager.MeetingTable, opponent);
    }
}
