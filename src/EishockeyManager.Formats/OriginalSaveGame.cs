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

/// <summary>Ein belegter Kaderplatz (Platz 0x34 Byte) mit dem verknüpften Pool-Spieler.</summary>
/// <param name="Slot">Platz 0–28.</param>
/// <param name="PoolIndex">Platz +0x0F: Index im Spielerpool.</param>
/// <param name="Line">Platz +0x15, untere Bits: Reihe 1–3 (0 = keine); Bit 0x80 = Sturm.</param>
/// <param name="ContractYears">Platz +0x0B: Vertragsdauer in Jahren (Spalte V.DAUER).</param>
/// <param name="Dressed">Platz +0x0A: eingesetzt (Status „IM TEAM“ oder „RESERVE“).</param>
/// <param name="Games">Platz +0x06: Spiele (Spalte SP; nach einem Kauf mit den Spielen beim alten Verein).</param>
/// <param name="ClubGames">Platz +0x1C: Spiele für den jetzigen Verein (nach einem Kauf 0, sonst gleich <paramref name="Games"/>).</param>
/// <param name="Goals">Spalte TO = Platz +0x03 + +0x04 + +0x05 (Tore je Wettbewerb). Beim Torhüter zeigt das Original „-“; dort stehen vermutlich die Gegentore. [ORIGINAL] em.exe 0x421D6.</param>
/// <param name="TwoMinutePenalties">Spalte 2m: Platz +0x01. [ORIGINAL] em.exe 0x4221D.</param>
/// <param name="FiveMinutePenalties">Spalte 5m: Platz +0x00. [ORIGINAL] em.exe 0x4221D.</param>
/// <param name="E">Spalte E = Platz +0x13 · 10 / 15 (ganzzahlig; hervorgehoben bei +0x13 &gt; 0x82). [ORIGINAL] em.exe 0x42335.</param>
/// <param name="Insured">Platz +0x30/+0x32 ≠ 0: Zusatz „(v)“ bei der Vertragsdauer (versichert). [ORIGINAL] em.exe 0x42410.</param>
/// <param name="SalaryThirds">Platz +0x28: Gehalt in Drittel-DM (Spalte WERT bei „Verträge“).</param>
/// <param name="PriceThirds">Platz +0x2C: Preis in Drittel-DM (Transferliste „(K)“; nach dem Kauf das bezahlte Gebot).</param>
public sealed record OriginalSquadSlot(
    int Slot, int PoolIndex, string Name, int Strength1, int Strength2, int Strength3, int Age, int Talent, int RoleValue,
    int ContractYears, bool Dressed, int Line, bool Forward, int Games, int ClubGames, int Goals, int TwoMinutePenalties, int FiveMinutePenalties, int E, bool Insured,
    int SalaryThirds, int PriceThirds)
{
    /// <summary>Torhüter: Rollenwert 0 (Pool +0x18).</summary>
    public bool IsGoalkeeper => RoleValue == 0;
}

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

    /// <summary>Größe eines Kaderplatzes. [ORIGINAL] DGROUP 0x5DC3 + 0x618 · Manager + 0x34 · Platz.</summary>
    public const int SquadSlotLength = 0x34;

    /// <summary>Kaderplätze je Manager-Satz (0x618 / 0x34).</summary>
    public const int SquadSlotsPerManager = 0x618 / SquadSlotLength;

    /// <summary>Größe eines Spielerdatensatzes im Pool. [ORIGINAL] reverse-engineering.md, Spielerstruktur.</summary>
    public const int PoolRecordLength = 33;

    /// <summary>
    /// Belegte Kaderplätze eines Manager-Satzes (0–3 Manager, 4 = Transfermarkt). Feldbelegung an FILE05 gegen die
    /// Bildschirme „Mannschaft“, „Aufstellung“, „Verträge“ und „Transfer“ geprüft (spielstand-original.md).
    /// </summary>
    public IReadOnlyList<OriginalSquadSlot> Squad(int managerRecord)
    {
        var squad = Block("5DC3");
        var pool = Block("far:8253");
        var list = new List<OriginalSquadSlot>();
        for (var i = 0; i < SquadSlotsPerManager; i++)
        {
            var slot = squad.AsSpan(managerRecord * 0x618 + i * SquadSlotLength, SquadSlotLength);
            if (!slot.ContainsAnyExcept((byte)0))
                continue;
            int index = slot[0x0F];
            var player = pool.AsSpan(index * PoolRecordLength, PoolRecordLength);
            list.Add(new OriginalSquadSlot(
                i, index, GameText.Decode(player[..0x12]), slot[0x10], slot[0x11], slot[0x12],
                player[0x14], player[0x12], player[0x18], slot[0x0B], slot[0x0A] != 0, slot[0x15] & 0x0F, (slot[0x15] & 0x80) != 0,
                slot[0x06], slot[0x1C], slot[0x03] + slot[0x04] + slot[0x05], slot[0x01], slot[0x00], slot[0x13] * 10 / 15,
                BitConverter.ToInt32(slot.Slice(0x30, 4)) != 0,
                BitConverter.ToInt32(slot.Slice(0x28, 4)), BitConverter.ToInt32(slot.Slice(0x2C, 4))));
        }
        return list;
    }

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
        B(0xA707, 6, "Taktikmodus Angriff je Manager (0 = AUTOM.)"),
        B(0xA701, 6),
        B(0xA6FB, 6, "Taktikmodus Einsatz je Manager (0 = AUTOM.)"),
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
        B(0xA8A1, 0x70, "Trainingsplan: je Manager 28 Byte = 7 Tage × (oben 2, unten 2) Trainingsart"),
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
        Far(0x8253, 200 * 33, "Spielerpool (200 × 33): Name, Talent 0x12, Alter 0x14, Grundstärken 0x15–0x17, Besitzer 0x1A (4 = Markt), Verein 0x1D"),
        B(0x5DC3, 0x2490, "Kaderplätze (6 × 0x618, Platz 0x34; Manager 4 = Transfermarkt)"),
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
        B(0xCAE9, 0x120, "Taktikwerte Einsatz/Angriff (0x30 je Manager; bei AUTOM. 0)"),
        B(0xCAE3, 6),
        Checksum,
    ];
}
