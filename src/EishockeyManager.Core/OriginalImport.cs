using System.Buffers.Binary;
using EishockeyManager.Formats;

namespace EishockeyManager.Core;

/// <summary>
/// Import eines Original-Spielstands <c>*.HOK</c> (Spur hok-import, Teil 2; Runde 14, E14-1 C). Grundlage ist die Feldkarte
/// in <c>docs/formate/spielstand-original.md</c> (Abschnitt „Feldkarte Remake-Spielstand ↔ HOK“), deren Grundregeln 1–6 hier
/// umgesetzt sind.
/// <para>
/// Ablauf (<see cref="GameState.ImportOriginal"/>): (1) Spielstart wie <see cref="NewGame.Create"/>, aber mit Ligen, Satzfolge,
/// Spielplan und Managervereinen aus dem HOK (kein Platztausch, keine Startmischung); (2) Nachspielen der Zeitleiste bis zum
/// Tag des HOK mit den Ligaergebnissen des Originals (DGROUP 0xA0CB) statt der Spielberechnung; (3) Übernahme der belegten
/// Felder (Kader der Managervereine, Teamwerte, Konto, Stadion, Bank, Optionen …).
/// </para>
/// <para>
/// [APPROX] Was das HOK nicht oder nicht belegt führt (Feldkarte Status N/U) und was dieser Teil noch nicht übernimmt
/// (Pokale, Spielerpool, Transfermarkt, Börse, Werbeverträge, Training, Nachrichten, Taktik), ergibt sich aus dem Nachspielen: es ist
/// ein gültiger Remake-Zustand am selben Tag, aber nicht der des Originals. Zufall aus neuer Saat (Regel 6).
/// </para>
/// </summary>
public static class OriginalImport
{
    /// <summary>Länge des Vereinsnamens im Vereinssatz (far:9657 + 0x00). [ORIGINAL] wie Teil 1 (Regel 1, FILE05–FILE29).</summary>
    public const int ClubNameLength = 0x16;

    /// <summary>Plätze je Ligaabschnitt im Ergebnisblock 0xA0CB: 44 Spieltage × 6 Paarungen × 2 Tore.</summary>
    public const int ResultsPerLeague = 0x210;

    /// <summary>Liest und importiert eine Datei. Prüfsummenfehler brechen nicht ab (Hinweis wie „Der kleine Mogelguru“: Teil 3).</summary>
    public static GameState Load(string path, ClubDatabase database, int seed) =>
        GameState.ImportOriginal(OriginalSaveGame.Read(File.ReadAllBytes(path)), database, seed);

    /// <summary>
    /// Regel 1: Satzplatz 0–149 → Datenbankindex über den Vereinsnamen. Im Modus „nur Weltkugel“ trägt der Satz des
    /// Managervereins den Namen des Landes (FILE11); er bekommt den einzigen Datenbanknamen, der im Satzbestand fehlt.
    /// </summary>
    /// <exception cref="InvalidDataException">Name unbekannt oder mehrdeutig.</exception>
    public static int[] ClubIndices(OriginalSaveGame save, ClubDatabase database)
    {
        var records = save.Block("far:9657");
        var names = Enumerable.Range(0, ClubDatabase.TotalClubs).Select(slot => ClubName(records, slot)).ToArray();
        var byName = database.Clubs.ToDictionary(c => c.Name, c => c.Index);
        var map = new int[names.Length];
        var used = new HashSet<int>();
        var managerSlots = Enumerable.Range(0, save.Block("0386")[0])
            .Select(m => (int)save.Block("965B")[m * OriginalSaveGame.ManagerRecordLength + 0x14]).ToHashSet();
        var open = new List<int>();
        for (var slot = 0; slot < names.Length; slot++)
        {
            if (!byName.TryGetValue(names[slot], out var index))
                throw new InvalidDataException($"Vereinssatz {slot} „{names[slot]}“ ist kein Datenbankverein.");
            if (used.Add(index))
                map[slot] = index;
            else
                open.Add(slot);
        }
        if (open.Count == 0)
            return map;
        // „nur Weltkugel“: genau ein Satz doppelt (der Managerverein heißt wie das Land), genau ein Name fehlt.
        var missing = database.Clubs.Select(c => c.Index).Where(i => !used.Contains(i)).ToList();
        if (open.Count != 1 || missing.Count != 1)
            throw new InvalidDataException($"Vereinssätze nicht eindeutig: {open.Count} doppelt, {missing.Count} fehlend.");
        var duplicate = open[0];
        var twin = Array.IndexOf(names, names[duplicate]);
        var managerSlot = managerSlots.Contains(duplicate) ? duplicate : managerSlots.Contains(twin) ? twin : -1;
        if (managerSlot < 0)
            throw new InvalidDataException($"Doppelter Vereinsname „{names[duplicate]}“ gehört keinem Managerverein.");
        var nationSlot = managerSlot == duplicate ? twin : duplicate;
        map[nationSlot] = byName[names[duplicate]];
        map[managerSlot] = missing[0];
        return map;
    }

    /// <summary>Name eines Vereinssatzes (bis zum ersten Nullbyte).</summary>
    public static string ClubName(ReadOnlySpan<byte> records, int slot)
    {
        var raw = records.Slice(slot * OriginalSaveGame.ClubRecordLength, ClubNameLength);
        var end = raw.IndexOf((byte)0);
        return GameText.Decode(end < 0 ? raw : raw[..end]);
    }

    /// <summary>Regel 5: Remake-Tag = Tageszähler DGROUP 0x3B7 − 1 (10. September = Zähler 1 = Tag 0).</summary>
    public static int Day(OriginalSaveGame save) => BinaryPrimitives.ReadInt32LittleEndian(save.Block("03B7")) - 1;

    /// <summary>Regel 5: Saison = 1993 + Saisonzähler DGROUP 0x3BD.</summary>
    public static int Season(OriginalSaveGame save) =>
        GameState.FirstSeason + BinaryPrimitives.ReadInt16LittleEndian(save.Block("03BD"));

    /// <summary>
    /// Ligaergebnis aus DGROUP 0xA0CB + 0x210 · Liga + 12 · (Spieltag − 1) + 2 · Paarung: Heimtore, Gasttore. [ORIGINAL]
    /// Ergebnis eintragen Datei 0x50B45 (Paarungen 0x5D7B aus 0x4E7C7); an FILE05 gegen die Tabellensätze 0x825A geprüft
    /// (OriginalImportTests).
    /// </summary>
    public static (int Home, int Away) LeagueResult(OriginalSaveGame save, League league, int matchDay, int pair)
    {
        var block = save.Block("A0CB");
        var offset = ResultsPerLeague * (int)league + 12 * (matchDay - 1) + 2 * pair;
        return (block[offset], block[offset + 1]);
    }

    /// <summary>Nächster Ligaspieltag der Liga (DGROUP 0x7A9 + Liga, 1-basiert).</summary>
    public static int NextMatchDay(OriginalSaveGame save, League league) => save.Block("07A9")[(int)league];

    /// <summary>Spielmodus DGROUP 0xD0B9 (1 nur Weltkugel, 2 nur Fahne, 4 beide).</summary>
    public static GameMode Mode(OriginalSaveGame save) => (GameMode)save.Block("D0B9")[0];
}

public sealed partial class GameState
{
    /// <summary>Baut aus einem Original-Spielstand einen gültigen Spielstand des Remakes (noch nicht umgesetzt).</summary>
    public static GameState ImportOriginal(OriginalSaveGame save, ClubDatabase database, int seed) =>
        throw new NotImplementedException("Import noch nicht umgesetzt");
}
