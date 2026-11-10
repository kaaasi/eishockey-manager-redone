using System.Globalization;

namespace EishockeyManager.Formats;

/// <summary>Ein durch <see cref="OriginalSavePatcher"/> änderbares Feld (für Hilfe und Doku).</summary>
public sealed record OriginalSavePatchField(string Name, string Syntax, string Description);

/// <summary>
/// Erzeugt aus einem Original-Spielstand <c>*.HOK</c> einen veränderten Stand, den das DOS-Original laden kann
/// (Analysewerkzeug: gezielt Situationen herstellen). Es werden nur **belegte** Felder geändert
/// (<c>docs/formate/spielstand-original.md</c>); alles andere – auch Klartextkopf mit Namen und Füllbyte sowie der
/// Startschlüssel – bleibt unverändert. Prüfsummen werden neu berechnet, die Spielstand-ID wird neu gewürfelt
/// (das Original weist eine ID, die es schon gesichert oder geladen hat, mit „Keinen heutigen Spielstand laden !“ ab).
/// </summary>
public static class OriginalSavePatcher
{
    /// <summary>
    /// Kleinster 16-Bit-Wert der erzeugten Spielstand-ID. [APPROX] Alle drei Original-Stände (FILE01–03) haben eine
    /// vorzeichenerweiterte 16-Bit-ID im Bereich 0x8000–0xFFFF (−26229, −20251, −27317), obwohl die Notiz zu em.exe 0x5BB51
    /// <c>random(0..0x8FFF) + 1</c> nennt; die Formel ist also noch nicht geklärt. Das Original vergleicht die ID nur mit
    /// seiner Liste bereits gesicherter/geladener IDs, deshalb genügt ein anderer Wert im beobachteten Bereich.
    /// </summary>
    public const int MinSaveId16 = 0x8000;

    private static readonly OriginalSavePatchField[] FieldList =
    [
        new("konto", "konto=<DM>", "Kontostand des Managers in DM (gespeichert in Drittel-DM, 0x97EA); negativ = Schulden."),
        new("eintritt", "eintritt=<DM>", "Eintrittspreis in DM, 1–99 (0x973F). [APPROX] Obergrenze nicht belegt."),
        new("stadion", "stadion=a,b,c,d,e,f,g,h,i", "Die neun Stadionstufen in Menüreihenfolge (je 0–9, 0x9771). [APPROX] Obergrenze nicht belegt."),
        new("sitz", "sitz=<n>", "Sitzplätze der Halle (0x978E), 0–32767."),
        new("steh", "steh=<n>", "Stehplätze der Halle (0x9792), 0–32767."),
        new("datum", "datum=TT.MM.JJJJ", "Spieldatum (0x5A20/0x5A1C/0x5A18, Monat im Stand 0-basiert) und Jahr im Kopf (0x0A). Ändert weder Tabelle noch Ergebnisse."),
        new("zaehler", "zaehler=<n>", "Tageszähler (0x3B7 und 0xA969, Spielstart = 1), 1–32767. Ändert nichts sonst."),
        new("spieltag", "spieltag=<n>", "Nächster Spieltag aller drei Ligen (0x7A9–0x7AB) und Kopfbyte 0x0F, 1–255. Ändert weder Tabelle noch Ergebnisse."),
        new("level", "level=<0-5>", "Spiel-Level: Setup-Regler 0x118 und davon abgeleitet 0xED8 = 5 − Level (em.exe 0x2988D)."),
    ];

    /// <summary>Regler, die das Original nur beim Spielstart auswertet; ein Patch im geladenen Stand wäre wirkungslos oder widersprüchlich.</summary>
    private static readonly Dictionary<string, string> Rejected = new(StringComparer.OrdinalIgnoreCase)
    {
        ["dauer"] = "Dauer bestimmt das Spielende (0x5A14), die Zuordnung ist nicht belegt (Wert 22222 bei Dauer 3).",
        ["liga"] = "Start-Liga würde Verein, Tabelle und Gegner betreffen; nicht belegt. Stattdessen einen Stand mit der gewünschten Liga neu anlegen.",
        ["staerke"] = "Start-Stärke wirkt nur bei der Erzeugung der Mannschaften; im Stand stehen die fertigen Werte.",
    };

    public static IReadOnlyList<OriginalSavePatchField> Fields => FieldList;

    /// <summary>Patcht einen Spielstand.</summary>
    /// <param name="template">Dateiinhalt der Vorlage (bleibt unverändert).</param>
    /// <param name="changes">Feldname → Wert (Namen siehe <see cref="Fields"/>); Name doppelt oder unbekannt → <see cref="ArgumentException"/>.</param>
    /// <param name="manager">Manager (0-basiert), auf den sich konto, eintritt, stadion, sitz und steh beziehen.</param>
    /// <param name="newId">Feste neue Spielstand-ID; sonst zufällig und verschieden von der Vorlage.</param>
    /// <param name="random">Zufallsquelle für die ID (Tests).</param>
    public static byte[] Patch(ReadOnlySpan<byte> template, IEnumerable<KeyValuePair<string, string>> changes,
        int manager = 0, int? newId = null, Random? random = null)
    {
        var save = OriginalSaveGame.Read(template);
        if (!save.ChecksumsValid)
            throw new InvalidDataException("Die Vorlage hat falsche Prüfsummen; sie wird nicht als Grundlage genommen.");

        var list = changes.ToList();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, _) in list)
        {
            if (Rejected.TryGetValue(name, out var why))
                throw new ArgumentException($"Feld „{name}“ wird nicht gepatcht: {why}");
            if (!FieldList.Any(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException($"Unbekanntes Feld „{name}“. Bekannt: {string.Join(", ", FieldList.Select(f => f.Name))}.");
            if (!seen.Add(name))
                throw new ArgumentException($"Feld „{name}“ ist doppelt angegeben.");
        }

        var managerCount = save.Block("0386")[0];
        if (manager < 0 || manager >= managerCount || manager >= 4)
            throw new ArgumentException($"Manager {manager} gibt es nicht (Stand hat {managerCount}).");

        var header = template[..OriginalSaveGame.PlainLength].ToArray();
        var rec = save.Block("965B");
        var recStart = manager * OriginalSaveGame.ManagerRecordLength;
        void Rec16(int dgroup, int value) => BitConverter.GetBytes((short)value).CopyTo(rec, recStart + dgroup - 0x965B);

        foreach (var (rawName, value) in list)
        {
            switch (rawName.ToLowerInvariant())
            {
                case "konto":
                {
                    var dm = Int(rawName, value, -(int.MaxValue / 3), int.MaxValue / 3);
                    BitConverter.GetBytes(dm * 3).CopyTo(rec, recStart + 0x97EA - 0x965B);
                    break;
                }
                case "eintritt":
                    rec[recStart + 0x973F - 0x965B] = (byte)Int(rawName, value, 1, 99);
                    break;
                case "stadion":
                {
                    var parts = value.Split(',');
                    if (parts.Length != 9)
                        throw new ArgumentException("Feld „stadion“ braucht neun Stufen, durch Komma getrennt.");
                    for (var n = 0; n < 9; n++)
                        Rec16(0x9771 + 2 * n, Int("stadion", parts[n], 0, 9));
                    break;
                }
                case "sitz":
                    Rec16(0x9771 + 0x1D, Int(rawName, value, 0, short.MaxValue));
                    break;
                case "steh":
                    Rec16(0x9771 + 0x21, Int(rawName, value, 0, short.MaxValue));
                    break;
                case "datum":
                {
                    var p = value.Split('.');
                    if (p.Length != 3)
                        throw new ArgumentException("Feld „datum“ erwartet TT.MM.JJJJ.");
                    var year = Int("datum (Jahr)", p[2], 1900, 2200);
                    var month = Int("datum (Monat)", p[1], 1, 12);
                    var day = Int("datum (Tag)", p[0], 1, DateTime.DaysInMonth(year, month));
                    BitConverter.GetBytes(day).CopyTo(save.Block("5A20"), 0);
                    BitConverter.GetBytes(month - 1).CopyTo(save.Block("5A1C"), 0);
                    BitConverter.GetBytes(year).CopyTo(save.Block("5A18"), 0);
                    BitConverter.GetBytes(year).CopyTo(header, 0x0A);
                    break;
                }
                case "zaehler":
                {
                    var n = Int(rawName, value, 1, short.MaxValue);
                    BitConverter.GetBytes(n).CopyTo(save.Block("03B7"), 0);
                    BitConverter.GetBytes((short)n).CopyTo(save.Block("A969"), 0);
                    break;
                }
                case "spieltag":
                {
                    var n = (byte)Int(rawName, value, 1, 255);
                    var block = save.Block("07A9");
                    block[0] = block[1] = block[2] = n;
                    header[0x0F] = n;
                    break;
                }
                case "level":
                {
                    var level = Int(rawName, value, 0, 5);
                    save.Block("0118")[0] = (byte)level;
                    save.Block("0ED8")[0] = (byte)(5 - level);
                    break;
                }
            }
        }

        var id = newId ?? NewId(save.SaveId, random ?? Random.Shared);
        BitConverter.GetBytes(id).CopyTo(header, 0xB4);
        return Encode(header, save);
    }

    /// <summary>
    /// Würfelt eine Spielstand-ID im Bereich der Original-Stände (16 Bit 0x8000–0xFFFF, vorzeichenerweitert,
    /// siehe <see cref="MinSaveId16"/>), die von <paramref name="oldId"/> abweicht.
    /// </summary>
    public static int NewId(int oldId, Random random)
    {
        int id;
        do id = (short)random.Next(MinSaveId16, 0x10000);
        while (id == oldId);
        return id;
    }

    /// <summary>
    /// Schreibt einen Spielstand wie die Sicherungsroutine (em.exe 0x5A894): Klartextkopf unverändert (Startschlüssel
    /// im letzten Kopfbyte), Blöcke mit <c>(klar XOR k) + k</c>, Prüfsumme (Startschlüssel + Summe der Klartextbytes)
    /// unverschlüsselt an den zwei Stellen des Layouts. Gegenstück zu <see cref="OriginalSaveGame.Read"/>.
    /// </summary>
    public static byte[] Encode(ReadOnlySpan<byte> plainHeader, OriginalSaveGame save) => Encode(plainHeader, save.Block);

    /// <summary>Wie <see cref="Encode(ReadOnlySpan{byte}, OriginalSaveGame)"/>, Blöcke über ihren Namen (auch <see cref="OriginalSaveWriter"/>).</summary>
    public static byte[] Encode(ReadOnlySpan<byte> plainHeader, Func<string, byte[]> block)
    {
        if (plainHeader.Length != OriginalSaveGame.PlainLength)
            throw new ArgumentException($"Kopf muss {OriginalSaveGame.PlainLength} Byte lang sein.", nameof(plainHeader));

        var file = new byte[OriginalSaveGame.FileLength];
        plainHeader.CopyTo(file);
        var k = plainHeader[OriginalSaveGame.KeyOffset];
        var sum = k;
        var pos = OriginalSaveGame.PlainLength;
        foreach (var entry in OriginalSaveGame.Layout)
        {
            if (entry.IsChecksum)
            {
                file[pos++] = sum;
                continue;
            }
            var data = block(entry.Name);
            if (data.Length != entry.Length)
                throw new ArgumentException($"Block „{entry.Name}“ hat {data.Length} statt {entry.Length} Byte.");
            for (var i = 0; i < entry.Length; i++)
            {
                file[pos++] = (byte)((data[i] ^ k) + k);
                sum += data[i];
                k += OriginalSaveGame.KeyStep;
            }
        }
        return file;
    }

    private static int Int(string field, string text, int min, int max)
    {
        if (!int.TryParse(text.Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var n))
            throw new ArgumentException($"Feld „{field}“: „{text}“ ist keine ganze Zahl.");
        if (n < min || n > max)
            throw new ArgumentException($"Feld „{field}“: {n} außerhalb {min}…{max}.");
        return n;
    }
}
