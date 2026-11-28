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
        new("startliga", "startliga=<0-2>", "Start-Liga des Spiels (Regler 0x11A; 0 Bundesliga, 1 2. Liga, 2 Oberliga). Wirkt nur auf neue Manager " +
            "(Kachel „Neuer“: Platzwahl, Startkader, Liga-Stufe); Vereine und Tabellen bleiben. Bei Dauer „eine Saison“ nur 0."),
        new("ligaplaetze", "ligaplaetze=<liga>:s1,…,s12", "Platzliste einer Liga (0x5CCB + 12 · Liga, Satznummern für Platz 1–12): nur eine " +
            "Umordnung der gespeicherten Sätze. Vor dem 45. Spieltag muss sie Fixpunkt der Sortierung 0x50B45 sein, nach der Saison " +
            "zu den Teilnehmern der Aufstiegsrunde (0xCFFC) passen."),
    ];

    /// <summary>Regler, die das Original nur beim Spielstart auswertet; ein Patch im geladenen Stand wäre wirkungslos oder widersprüchlich.</summary>
    private static readonly Dictionary<string, string> Rejected = new(StringComparer.OrdinalIgnoreCase)
    {
        ["dauer"] = "Dauer bestimmt das Spielende (0x5A14), die Zuordnung ist nicht belegt (Wert 22222 bei Dauer 3).",
        ["liga"] = "Die Liga eines Vereins ist nicht patchbar (kein Vereinstausch). Für neue Manager „startliga“, für die Reihenfolge einer Liga „ligaplaetze“.",
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
        void Rec16(int field, int value) => BitConverter.GetBytes((short)value).CopyTo(rec, recStart + field);
        (int League, int[] Order)? places = null;

        foreach (var (rawName, value) in list)
        {
            switch (rawName.ToLowerInvariant())
            {
                case "konto":
                {
                    var dm = Int(rawName, value, -(int.MaxValue / 3), int.MaxValue / 3);
                    BitConverter.GetBytes(dm * 3).CopyTo(rec, recStart + OriginalSaveGame.ManagerAccountField);
                    break;
                }
                case "eintritt":
                    rec[recStart + OriginalSaveGame.ManagerTicketPriceField] = (byte)Int(rawName, value, 1, 99);
                    break;
                case "stadion":
                {
                    var parts = value.Split(',');
                    if (parts.Length != 9)
                        throw new ArgumentException("Feld „stadion“ braucht neun Stufen, durch Komma getrennt.");
                    for (var n = 0; n < 9; n++)
                        Rec16(OriginalSaveGame.ManagerStadiumField + 2 * n, Int("stadion", parts[n], 0, 9));
                    break;
                }
                case "sitz":
                    Rec16(OriginalSaveGame.ManagerStadiumField + OriginalSaveGame.StadiumSeatsField, Int(rawName, value, 0, short.MaxValue));
                    break;
                case "steh":
                    Rec16(OriginalSaveGame.ManagerStadiumField + OriginalSaveGame.StadiumStandingField, Int(rawName, value, 0, short.MaxValue));
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
                case "startliga":
                {
                    var league = Int(rawName, value, 0, OriginalSaveGame.LeagueCount - 1);
                    var setup = save.Block("0118");
                    // [ORIGINAL] 0x11A lesen nur die Spielvorbereitung 0x28165 (u. a. Startkader Datei 0x29A3F, Liga-Stufe 0x29BBB,
                    // Platzwahl 0x29BCE – auch für „Neuer“ im laufenden Spiel) und 0x5AF50 (Datei 0x5B289);
                    // `disasm.py --xref <em.exe> 0x11A 0x11A`. Im Hinzufügen-Modus ist der Regler nicht verstellbar (Datei 0x286DA).
                    // [ORIGINAL] Spielvorbereitung Datei 0x2890E–0x28920: Bei Dauer „eine Saison“ (0x119 = 0) nimmt sie jede
                    // Start-Liga ≠ Bundesliga zurück; einen solchen Stand erzeugt das Original nicht.
                    if (league != 0 && setup[OriginalSaveGame.SetupDurationField] == 0)
                        throw new ArgumentException("Feld „startliga“: Bei Dauer „eine Saison“ lässt das Original nur die Bundesliga zu (Datei 0x2890E).");
                    setup[OriginalSaveGame.SetupStartLeagueField] = (byte)league;
                    break;
                }
                case "ligaplaetze":
                    places = ParsePlaces(value);
                    break;
            }
        }

        // Nach den übrigen Feldern prüfen: „spieltag“ ändert, ob die Sortierung noch läuft.
        if (places is { } chosen)
            ApplyPlaces(save, chosen.League, chosen.Order);

        var id = newId ?? NewId(save.SaveId, random ?? Random.Shared);
        BitConverter.GetBytes(id).CopyTo(header, 0xB4);
        return Encode(header, save);
    }

    private static (int League, int[] Order) ParsePlaces(string value)
    {
        var colon = value.IndexOf(':');
        if (colon <= 0)
            throw new ArgumentException("Feld „ligaplaetze“ erwartet <liga>:s1,…,s12.");
        var league = Int("ligaplaetze (Liga)", value[..colon], 0, OriginalSaveGame.LeagueCount - 1);
        var parts = value[(colon + 1)..].Split(',');
        if (parts.Length != OriginalSaveGame.LeagueSize)
            throw new ArgumentException($"Feld „ligaplaetze“ braucht {OriginalSaveGame.LeagueSize} Satznummern, durch Komma getrennt.");
        return (league, parts.Select(p => Int("ligaplaetze (Satz)", p, 0, 255)).ToArray());
    }

    /// <summary>
    /// Schreibt die Platzliste einer Liga, wenn sie zum übrigen Stand passt (sonst <see cref="ArgumentException"/>):
    /// <list type="number">
    /// <item>dieselben Sätze wie gespeichert (kein Vereinstausch; der 29. April sucht den Managerverein in der Zeile seiner Liga,
    ///   Datei 0x55F93–0x560D8);</item>
    /// <item>solange DGROUP 0x7A9 + Liga ≤ 44: Fixpunkt der Sortierung 0x50B45, die das Hauptmenü bei jedem Aufbau ausführt
    ///   (Datei 0x44EFD; <see cref="SortLeague"/>);</item>
    /// <item>danach, wenn die Aufstiegsrunde ausgelost ist (Teilnehmer DGROUP 0xCFFC + 4 · Gruppe nicht alle 0): Teilnehmer =
    ///   Plätze 11, 12 der oberen und 1, 2 der unteren Liga in dieser Reihenfolge (Auslosung 0x30427; FILE22, FILE44);</item>
    /// <item>Bundesliga danach, wenn die Play-offs gesetzt sind (DGROUP 0x5D3B nicht alle 0): Viertelfinalpaare = Platz i gegen
    ///   9 − i (Setzung 0x319AC; FILE22, FILE44).</item>
    /// </list>
    /// [APPROX] Regeln 3 und 4 sind eine Vorsicht des Patchers: Nach dem 44. Spieltag, aber vor Auslosung bzw. Setzung könnten
    /// 0xCFFC und 0x5D3B noch Werte der Vorsaison halten (FILE23, FILE44A tragen sie nach dem Saisonwechsel weiter); einen solchen
    /// Stand weist der Patcher ab, auch wenn das Original neu auslosen würde.
    /// Die Platzfelder der Tabellensätze (0x8288, 0x828B) bleiben; 0x51049 schreibt sie am nächsten Ligaspieltag neu.
    /// </summary>
    private static void ApplyPlaces(OriginalSaveGame save, int league, int[] order)
    {
        var size = OriginalSaveGame.LeagueSize;
        var block = save.Block("5CCB");
        var stored = block.AsSpan(league * size, size).ToArray().Select(b => (int)b).ToArray();
        if (!order.Order().SequenceEqual(stored.Order()))
            throw new ArgumentException($"Feld „ligaplaetze“: Liga {league} hat die Sätze {string.Join(",", stored)}; " +
                                        "erlaubt ist nur eine Umordnung (jeder Satz genau einmal).");

        var matchDay = save.Block("07A9")[league];
        if (matchDay <= OriginalSaveGame.LastSortedMatchDay)
        {
            var sorted = SortLeague(save.Block("8257"), order);
            if (!sorted.SequenceEqual(order))
                throw new ArgumentException($"Feld „ligaplaetze“: Vor dem 45. Spieltag (0x7A9 + {league} = {matchDay}) sortiert das Hauptmenü " +
                                            $"die Liste nach der Tabelle um (0x50B45) zu {string.Join(",", sorted)}; nur ein Fixpunkt bleibt stehen.");
        }

        var next = (byte[])block.Clone();
        for (var p = 0; p < size; p++)
            next[league * size + p] = (byte)order[p];
        var participants = save.Block("CFFC");
        if (matchDay > OriginalSaveGame.LastSortedMatchDay)
        {
            for (var group = Math.Max(0, league - 1); group <= Math.Min(1, league); group++)
            {
                var drawn = participants.AsSpan(4 * group, 4);
                if (!drawn.ContainsAnyExcept((byte)0))
                    continue;
                var expected = next.AsSpan(size * group + size - 2, 4);
                if (!drawn.SequenceEqual(expected))
                    throw new ArgumentException($"Feld „ligaplaetze“: Teilnehmer der Aufstiegsrunde Gruppe {group} sind {string.Join(",", drawn.ToArray())}, " +
                                                $"die neue Liste ergäbe {string.Join(",", expected.ToArray())} (Plätze 11, 12 oben, 1, 2 unten).");
            }
        }
        // Play-off-Viertelfinale DGROUP 0x5D3B (= 5CFB + 0x40): Setzung 0x319AC–0x319D4 paart Platz i mit Platz 8 − i der
        // Bundesliga (Seite je Paarung offen, wie der Import in OriginalImport.Playoffs.cs).
        var playoffs = save.Block("5CFB")[0x40..0x48];
        if (league == 0 && matchDay > OriginalSaveGame.LastSortedMatchDay && playoffs.Any(b => b != 0))
        {
            static string Pairs(IEnumerable<(int, int)> pairs) =>
                string.Join(" ", pairs.Select(p => (Math.Min(p.Item1, p.Item2), Math.Max(p.Item1, p.Item2))).Order().Select(p => $"{p.Item1}-{p.Item2}"));
            var seeded = Pairs(Enumerable.Range(0, 4).Select(i => ((int)playoffs[2 * i], (int)playoffs[2 * i + 1])));
            var wanted = Pairs(Enumerable.Range(0, 4).Select(i => ((int)next[i], (int)next[7 - i])));
            if (seeded != wanted)
                throw new ArgumentException($"Feld „ligaplaetze“: Das Play-off-Viertelfinale (0x5D3B) paart {seeded}, die neue Liste ergäbe {wanted} " +
                                            "(Platz i gegen Platz 9 − i).");
        }
        next.CopyTo(block, 0);
    }

    /// <summary>
    /// [ORIGINAL] Tauschsortierung der Platzliste wie em.exe 0x50B45, Gesamtansicht (Datei 0x50D6D–0x51011): für jeden Platz i
    /// alle späteren Plätze j; j verdrängt i bei mehr Punkten, bei gleichen Punkten weniger Spielen, dann höherer Tordifferenz,
    /// dann mehr Toren. Werte je Satz = Heim- + Auswärtsbyte des Tabellensatzes (DGROUP 0x825A + 0x50 · Satz: Punkte +0x00,
    /// Tore +0x16, Gegentore +0x1A, Spiele +0x1E). Gegenprobe gegen 0x50B45 aus den Bytes:
    /// <c>tools/reverse-engineering/hok/fixpunkt.py</c> (396 von 396 Folgen gleich).
    /// </summary>
    /// <param name="tableBlock">Block <c>8257</c> des Spielstands.</param>
    /// <param name="order">Satznummern der Liga in Platzfolge (bleibt unverändert).</param>
    public static int[] SortLeague(ReadOnlySpan<byte> tableBlock, IReadOnlyList<int> order)
    {
        var o = order.ToArray();
        var keys = new Dictionary<int, (int Points, int Games, int Goals, int Against)>();
        foreach (var slot in o)
        {
            var r = tableBlock.Slice(OriginalSaveGame.TableRecordStart + OriginalSaveGame.TableRecordLength * slot, OriginalSaveGame.TableRecordLength);
            keys[slot] = (r[0x00] + r[0x01], r[0x1E] + r[0x1F], r[0x16] + r[0x17], r[0x1A] + r[0x1B]);
        }
        bool Better(int bSlot, int aSlot)
        {
            var (b, a) = (keys[bSlot], keys[aSlot]);
            if (b.Points != a.Points)
                return b.Points > a.Points;
            if (b.Games != a.Games)
                return b.Games < a.Games;
            if (b.Goals - b.Against != a.Goals - a.Against)
                return b.Goals - b.Against > a.Goals - a.Against;
            return b.Goals > a.Goals;
        }
        for (var i = 0; i < o.Length - 1; i++)
            for (var j = i + 1; j < o.Length; j++)
                if (Better(o[j], o[i]))
                    (o[i], o[j]) = (o[j], o[i]);
        return o;
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
