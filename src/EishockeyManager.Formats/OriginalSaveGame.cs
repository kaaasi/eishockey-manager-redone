namespace EishockeyManager.Formats;

/// <summary>
/// Ein Block des verschlüsselten Datenstroms: so viele Bytes, wie das Original ab einer DGROUP-Adresse schreibt.
/// </summary>
/// <param name="Name">Kurzname (DGROUP-Adresse, hexadezimal; <c>far:…</c> für die zwei Blöcke hinter Zeigern).</param>
/// <param name="Address">DGROUP-Adresse bzw. bei <paramref name="Far"/> die Adresse des Zeigers.</param>
/// <param name="Length">Länge in Byte; 0 bei <see cref="IsChecksum"/>.</param>
/// <param name="Far">Der Block liegt hinter einem Fernzeiger an <paramref name="Address"/> (Vereine, Spielerpool).</param>
/// <param name="IsChecksum">Statt eines Blocks steht hier ein unverschlüsseltes Prüfsummenbyte.</param>
/// <param name="Meaning">Deutung, soweit belegt; sonst leer.</param>
public sealed record OriginalSaveBlock(string Name, int Address, int Length, bool Far, bool IsChecksum, string Meaning);

/// <summary>Eintrag im Klartextkopf: ein Manager mit Start-Liga, Name und Vereinsname.</summary>
public sealed record OriginalSaveManagerHeader(int StartLeague, bool OnlyWorldMode, string Name, string Club);

/// <summary>Gelesenes Prüfsummenbyte und der aus dem Klartext berechnete Wert.</summary>
public sealed record OriginalSaveChecksum(int FileOffset, byte Stored, byte Computed)
{
    public bool IsValid => Stored == Computed;
}

/// <summary>
/// Original-Spielstand <c>*.HOK</c> des DOS-Spiels (nur Lesen). [ORIGINAL] em.exe: Sichern Datei 0x5BA70 ff.,
/// Kopf 0x5B4CB, verschlüsselndes Schreiben 0x5A894, Laden 0x5C440 ff., entschlüsselndes Lesen 0x5A9AE.
/// Aufbau und offene Punkte: <c>docs/formate/spielstand-original.md</c>.
/// </summary>
public sealed class OriginalSaveGame
{
    /// <summary>Kennung am Dateianfang (10 Byte mit Nullbyte). [ORIGINAL] DGROUP 0x5604 über Zeiger 0x55DF.</summary>
    public const string Magic = "KRON_0.99";

    /// <summary>Zweite Kennung (4 Byte, „EH0“ + Nullbyte). [ORIGINAL] DGROUP 0x55E7, Vergleich beim Laden 0x5C53D.</summary>
    public static ReadOnlySpan<byte> Version => "EH0\0"u8;

    /// <summary>Länge des Klartextteils vor dem verschlüsselten Strom.</summary>
    public const int PlainLength = 0xBA;

    /// <summary>Position des Startschlüssels (letztes Klartextbyte).</summary>
    public const int KeyOffset = 0xB9;

    /// <summary>Schlüsselschritt je Byte: <c>k += 0xD7</c>, also <c>k −= 0x29</c>. [ORIGINAL] em.exe 0x5A89A.</summary>
    public const byte KeyStep = 0xD7;

    /// <summary>Größe eines Manager-Datensatzes im Block 0x965B. [ORIGINAL] Multiplikator 0x1AF (Datei 0x5B528).</summary>
    public const int ManagerRecordLength = 0x1AF;

    /// <summary>Größe eines Vereinsdatensatzes im Block hinter 0x9657. [ORIGINAL] Multiplikator 0x3D.</summary>
    public const int ClubRecordLength = 0x3D;

    public required IReadOnlyList<OriginalSaveManagerHeader> Managers { get; init; }
    public required int HeaderYear { get; init; }
    public required int HeaderManagerCount { get; init; }
    public required byte HeaderByte7A9 { get; init; }
    public required int SaveId { get; init; }
    public required byte Filler { get; init; }
    public required byte Key { get; init; }
    public required IReadOnlyList<OriginalSaveChecksum> Checksums { get; init; }

    /// <summary>Entschlüsselte Blöcke in Dateireihenfolge (ohne Prüfsummen), mit Dateioffset.</summary>
    public required IReadOnlyList<(OriginalSaveBlock Block, int FileOffset, byte[] Data)> Blocks { get; init; }

    public bool ChecksumsValid => Checksums.All(c => c.IsValid);

    public byte[] Block(string name) => Blocks.First(b => b.Block.Name == name).Data;

    /// <summary>Gesamtlänge eines Spielstands (unabhängig vom Inhalt).</summary>
    public static int FileLength => PlainLength + Layout.Sum(b => b.IsChecksum ? 1 : b.Length);

    /// <summary>
    /// Ein Byte entschlüsseln: <c>klar = (verschlüsselt − k) XOR k</c>. [ORIGINAL] em.exe 0x5A9F8–0x5AA05.
    /// Gegenstück beim Sichern: <c>verschlüsselt = (klar XOR k) + k</c> (0x5A8C3–0x5A8CE).
    /// </summary>
    public static byte DecryptByte(byte encrypted, byte key) => (byte)((byte)(encrypted - key) ^ key);

    public static OriginalSaveGame Read(ReadOnlySpan<byte> file)
    {
        if (file.Length < PlainLength)
            throw new InvalidDataException($"Spielstand zu kurz ({file.Length} Byte).");
        if (!file[..Magic.Length].SequenceEqual(System.Text.Encoding.ASCII.GetBytes(Magic)) || file[Magic.Length] != 0)
            throw new InvalidDataException($"Keine Kennung „{Magic}“ – kein Original-Spielstand.");
        if (!file.Slice(0xB0, 4).SequenceEqual(Version))
            throw new InvalidDataException("Zweite Kennung „EH0“ fehlt.");
        if (file.Length != FileLength)
            throw new InvalidDataException($"Länge {file.Length} Byte, erwartet {FileLength}.");

        var count = file[0x0E];
        var managers = new List<OriginalSaveManagerHeader>();
        for (var i = 0; i < Math.Min((int)count, 4); i++)
        {
            var e = file.Slice(0x10 + 0x28 * i, 0x28);
            managers.Add(new OriginalSaveManagerHeader(e[0], e[1] != 0, CString(e.Slice(2, 0x13)), CString(e.Slice(0x15, 0x13))));
        }

        var key = file[KeyOffset];
        var k = key;
        var sum = key;
        var pos = PlainLength;
        var blocks = new List<(OriginalSaveBlock, int, byte[])>();
        var checksums = new List<OriginalSaveChecksum>();
        foreach (var block in Layout)
        {
            if (block.IsChecksum)
            {
                checksums.Add(new OriginalSaveChecksum(pos, file[pos], sum));
                pos++;
                continue;
            }
            var data = new byte[block.Length];
            for (var i = 0; i < data.Length; i++)
            {
                var plain = DecryptByte(file[pos + i], k);
                data[i] = plain;
                sum += plain;
                k += KeyStep;
            }
            blocks.Add((block, pos, data));
            pos += block.Length;
        }

        return new OriginalSaveGame
        {
            Managers = managers,
            HeaderYear = BitConverter.ToInt32(file.Slice(0x0A, 4)),
            HeaderManagerCount = count,
            HeaderByte7A9 = file[0x0F],
            SaveId = BitConverter.ToInt32(file.Slice(0xB4, 4)),
            Filler = file[0xB8],
            Key = key,
            Checksums = checksums,
            Blocks = blocks,
        };
    }

    private static string CString(ReadOnlySpan<byte> bytes)
    {
        var end = bytes.IndexOf((byte)0);
        return GameText.Decode(end < 0 ? bytes : bytes[..end]);
    }

    private static OriginalSaveBlock B(int address, int length, string meaning = "") =>
        new($"{address:X4}", address, length, false, false, meaning);

    private static OriginalSaveBlock Far(int pointer, int length, string meaning) =>
        new($"far:{pointer:X4}", pointer, length, true, false, meaning);

    private static readonly OriginalSaveBlock Checksum = new("Prüfsumme", 0, 0, false, true, "Startschlüssel + Summe aller Klartextbytes (8 Bit)");

    /// <summary>
    /// Reihenfolge der Blöcke im verschlüsselten Strom. [ORIGINAL] Aufrufe von 0x5A894 in der Sicherungsroutine
    /// (Datei 0x5BC3C–0x5C3B8); das Laden liest dieselbe Folge mit 0x5A9AE. Deutungen nach
    /// <c>docs/formate/spielstand-original.md</c>.
    /// </summary>
    public static IReadOnlyList<OriginalSaveBlock> Layout { get; } =
    [
        B(0x02A0, 0x18, "Zinskonto je Manager (6 × 4)"),
        B(0x020D, 0x10),
        B(0x0118, 4, "Setup-Regler Level, Dauer, Start-Liga, Start-Stärke"),
        B(0x0120, 4),
        B(0xD214, 0x80),
        B(0xD0F4, 0x120),
        B(0x01FD, 0x10),
        B(0xA89A, 6),
        B(0xA891, 4),
        B(0xA895, 4),
        B(0x0178, 4),
        B(0x017C, 1),
        B(0xA707, 6, "Taktikmodus Angriff je Manager"),
        B(0xA701, 6),
        B(0xA6FB, 6, "Taktikmodus Einsatz je Manager"),
        B(0xCCB2, 0x46),
        B(0xCCA4, 0x0E),
        B(0xCCA3, 1, "Meisterrunde"),
        B(0xD0B9, 1, "Spielmodus Fahne/Weltkugel"),
        B(0xCC73, 0x30),
        B(0xCC43, 0x30),
        B(0xCD28, 0x30),
        B(0xCCF8, 0x30),
        B(0xD074, 1),
        B(0xD014, 0x60),
        B(0xD075, 0x20),
        B(0xCFFC, 0x18, "DEB-Pokal (Spiele)"),
        B(0xCFF8, 4),
        B(0xCED8, 0x120, "Play-Off-/Aufstiegsrunden-Gruppen (ab 0xCF20)"),
        B(0xCDB8, 0x120),
        B(0xCD88, 0x30),
        B(0xCD58, 0x30),
        B(0x026C, 0x30, "Kosten Freundschaftsspiele je Nationalteam"),
        B(0xD0BA, 4, "Bundestrainer-Ernennung je Manager"),
        B(0x06AC, 1, "Nationalteam (Vereinsindex)"),
        B(0xD0C1, 1, "Zahl der Freundschaftsspiele"),
        B(0xD0DB, 8),
        B(0xD0D3, 8),
        B(0xD0CB, 8),
        B(0xD0C3, 8, "Ergebnisse Freundschaftsspiele"),
        B(0x0256, 2, "Highscore-Schlüssel"),
        B(0x02BC, 4, "Preisindex"),
        B(0x02C0, 4, "Konjunkturfaktor"),
        B(0xA075, 0x54),
        B(0xA8A1, 0x70, "Trainingsplan (28 Byte je Manager)"),
        B(0xD0E4, 2),
        B(0x022D, 6),
        B(0xA88D, 4),
        B(0xA70E, 1, "Manager am Zug"),
        B(0xA70D, 1),
        B(0x0375, 0x0C),
        B(0xA0CB, 0x630, "Ergebnisse: 3 Ligen × 0x210, je Spieltag 6 Paarungen × 2 Tore"),
        B(0x0386, 1, "Zahl der Manager"),
        B(0x0387, 1, "DEB-Pokalsieger"),
        B(0xD0E6, 1, "Deutscher Meister"),
        B(0x0388, 1, "Europacupsieger"),
        B(0x0389, 1),
        B(0x038A, 1),
        B(0x038B, 1),
        B(0x965B, 6 * ManagerRecordLength, "Manager (6 × 0x1AF)"),
        Far(0x9657, 150 * ClubRecordLength, "Vereine und Nationalteams (150 × 0x3D)"),
        B(0x8257, 0x1400),
        Far(0x8253, 200 * 33, "Spieler (200 × 33)"),
        B(0x5DC3, 0x2490, "Kaderplätze (6 × 0x618, Platz 0x34)"),
        B(0x5D7B, 0x24),
        B(0x5A20, 4, "Tag"),
        B(0x5A1C, 4, "Monat (0 = Januar)"),
        B(0x5A18, 4, "Jahr"),
        B(0x03B7, 4, "Tageszähler (Spielstart = 1)"),
        B(0x03BB, 2),
        B(0xA776, 0x0A),
        B(0xA763, 0x12),
        Checksum,
        B(0xA75A, 2),
        B(0x5CFB, 0x80, "DEB-Pokal-Paarungen"),
        B(0x5CF3, 8, "DEB-Pokal-Runde"),
        B(0x5CF0, 3),
        B(0x5CCB, 0x24, "Tabelle: Vereinsindex je Liga × 12 + Platz"),
        B(0x5C4B, 0x80),
        B(0x07A9, 8, "0x7A9–0x7AB nächster Spieltag je Liga; 0x7AF Europapokal-Termin, 0x7B0 Runden"),
        B(0xAB7B, 0x1594),
        B(0x021D, 0x10, "Wettbewerbsschalter (0x227 DEB-Pokal)"),
        B(0xC313, 0x7D0),
        B(0xC15B, 0x1B8, "Werbeflächen (0x6E je Manager)"),
        B(0x0ED8, 1, "5 − Spiel-Level"),
        B(0x5A14, 2, "Spielende (Jahr)"),
        B(0xD294, 0xA98),
        B(0x0262, 2),
        B(0x03BD, 2, "Saisonzähler"),
        B(0xA969, 2, "Tageszähler (wie 0x3B7, Rolle offen)"),
        B(0x0246, 0x10),
        B(0xA8A0, 1),
        B(0x5A50, 5),
        B(0xCAE9, 0x120, "Taktikwerte Einsatz/Angriff (0x30 je Manager)"),
        B(0xCAE3, 6),
        Checksum,
    ];
}
