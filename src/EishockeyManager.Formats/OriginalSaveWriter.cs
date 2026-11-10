using System.Text;

namespace EishockeyManager.Formats;

/// <summary>
/// Baut einen Original-Spielstand <c>*.HOK</c> von Grund auf (Spur hok-export, Runde 15, Teil 1): alle Blöcke des
/// <see cref="OriginalSaveGame.Layout"/> mit Nullbytes vorbelegt, Klartextkopf aus den Feldern unten, Verschlüsselung und
/// beide Prüfsummen wie die Sicherungsroutine (<see cref="OriginalSavePatcher.Encode(ReadOnlySpan{byte}, Func{string, byte[]})"/>).
/// Gegenstück zu <see cref="OriginalSaveGame.Read"/>; welche Felder belegt werden, entscheidet der Aufrufer (Spielkern,
/// <c>Core/OriginalExport.cs</c>).
/// </summary>
/// <remarks>
/// [ORIGINAL] Klartextkopf em.exe Datei 0x5B4CB–0x5B660 (Kennung, Jahr, Managerzahl, Kopfbyte 0x7A9, je Manager Liga-Stufe,
/// Weltkugel-Kennzeichen, Name und Vereinsname, Kennung „EH0“, Spielstand-ID, Füllbyte, Startschlüssel); Aufbau in
/// <c>docs/formate/spielstand-original.md</c>, Abschnitt „Klartextteil“.
/// </remarks>
public sealed class OriginalSaveWriter
{
    /// <summary>Länge eines Namens im Kopf und im Managersatz (19 Byte mit Nullbyte; DGROUP 0x965B).</summary>
    public const int NameLength = 0x13;

    /// <summary>Höchstens vier Managereinträge im Kopf (0x10 + 4 × 0x28 = 0xB0).</summary>
    public const int HeaderManagers = 4;

    private readonly Dictionary<string, byte[]> _blocks = OriginalSaveGame.Layout.Where(b => !b.IsChecksum)
        .ToDictionary(b => b.Name, b => new byte[b.Length]);

    /// <summary>Kopf +0x0A: Jahr (DGROUP 0x5A18), nur Anzeige im Ladedialog.</summary>
    public int HeaderYear { get; set; }

    /// <summary>Kopf +0x0F: nächster Spieltag (DGROUP 0x7A9).</summary>
    public byte HeaderMatchDay { get; set; }

    /// <summary>Kopf +0x10 ff.: Managereinträge (Zahl = Kopf +0x0E); höchstens <see cref="HeaderManagers"/>.</summary>
    public List<OriginalSaveManagerHeader> Managers { get; } = [];

    /// <summary>Kopf +0xB4: Spielstand-ID (vorzeichenerweitert, siehe <see cref="OriginalSavePatcher.MinSaveId16"/>).</summary>
    public int SaveId { get; set; }

    /// <summary>Kopf +0xB8: Füllbyte ohne Verwendung (Original: random(0..255), Datei 0x5BC07).</summary>
    public byte Filler { get; set; }

    /// <summary>Kopf +0xB9: Startschlüssel (Original: random(0..255) + 0x6E, Datei 0x5BBF8).</summary>
    public byte Key { get; set; }

    /// <summary>Klartext eines Blocks (Name wie im <see cref="OriginalSaveGame.Layout"/>), zum Beschreiben.</summary>
    public byte[] Block(string name) => _blocks.TryGetValue(name, out var data)
        ? data
        : throw new KeyNotFoundException($"Block „{name}“ gibt es im Spielstand nicht.");

    /// <summary>
    /// Würfelt Spielstand-ID, Füllbyte und Startschlüssel wie das Original beim Sichern (drei Zufallszahlen, Datei 0x5BB51,
    /// 0x5BC07, 0x5BBF8). [APPROX] ID im Bereich der Original-Stände (<see cref="OriginalSavePatcher.NewId"/>).
    /// </summary>
    public void Randomize(Random random)
    {
        SaveId = OriginalSavePatcher.NewId(0, random);
        Filler = (byte)random.Next(256);
        Key = (byte)(random.Next(256) + 0x6E);
    }

    /// <summary>Klartextkopf (0xBA Byte).</summary>
    /// <remarks>
    /// [APPROX] Das Original lässt die Einträge hinter der Managerzahl als nicht initialisierten Stapel stehen (FILE01:
    /// <c>\EM\File01.HOK</c>); hier stehen dort Nullbytes. Das Laden liest nur die belegten Einträge (Datei 0x5B661 ff.).
    /// </remarks>
    public byte[] Header()
    {
        if (Managers.Count > HeaderManagers)
            throw new InvalidOperationException($"Höchstens {HeaderManagers} Manager im Kopf, nicht {Managers.Count}.");
        var header = new byte[OriginalSaveGame.PlainLength];
        Encoding.ASCII.GetBytes(OriginalSaveGame.Magic).CopyTo(header, 0);
        BitConverter.GetBytes(HeaderYear).CopyTo(header, 0x0A);
        header[0x0E] = (byte)Managers.Count;
        header[0x0F] = HeaderMatchDay;
        for (var m = 0; m < Managers.Count; m++)
        {
            var at = 0x10 + 0x28 * m;
            header[at] = (byte)Managers[m].StartLeague;
            header[at + 1] = (byte)(Managers[m].OnlyWorldMode ? 1 : 0);
            Name(Managers[m].Name).CopyTo(header, at + 2);
            Name(Managers[m].Club).CopyTo(header, at + 0x15);
        }
        OriginalSaveGame.Version.CopyTo(header.AsSpan(0xB0));
        BitConverter.GetBytes(SaveId).CopyTo(header, 0xB4);
        header[0xB8] = Filler;
        header[OriginalSaveGame.KeyOffset] = Key;
        return header;
    }

    /// <summary>Fertige Datei (Kopf, verschlüsselte Blöcke, zwei Prüfsummen).</summary>
    public byte[] ToArray() => OriginalSavePatcher.Encode(Header(), Block);

    /// <summary>
    /// Name als Feld fester Länge (<see cref="NameLength"/>, nullterminiert, Zeichensatz des Spiels über
    /// <see cref="GameText.Encode"/>); zu lange Namen werden abgeschnitten.
    /// </summary>
    public static byte[] Name(string text, int length = NameLength)
    {
        var field = new byte[length];
        var raw = GameText.Encode(text);
        raw.AsSpan(0, Math.Min(raw.Length, length - 1)).CopyTo(field);
        return field;
    }
}
