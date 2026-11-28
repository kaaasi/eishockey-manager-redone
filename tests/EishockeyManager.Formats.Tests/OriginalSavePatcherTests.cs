using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Formats.Tests;

/// <summary>
/// <c>emtool hok patch</c>: Spielstände fürs Original erzeugen. Synthetische Tests bauen den Stand unabhängig vom
/// Patcher nach em.exe 0x5A894 (Verschlüsselung wie in <see cref="OriginalSaveGameTests"/>).
/// </summary>
public class OriginalSavePatcherTests
{
    private static KeyValuePair<string, string> C(string name, string value) => new(name, value);

    [Fact]
    public void Patch_ChangesOnlyNamedFields_AndKeepsHeaderFillerAndKey()
    {
        var template = Template();
        var before = OriginalSaveGame.Read(template);

        var patched = OriginalSavePatcher.Patch(template, [C("konto", "750000"), C("eintritt", "21")], newId: 1234);
        var after = OriginalSaveGame.Read(patched);

        Assert.True(after.ChecksumsValid);
        Assert.Equal(1234, after.SaveId);
        Assert.Equal(before.Filler, after.Filler);
        Assert.Equal(before.Key, after.Key);
        Assert.Equal(before.Managers, after.Managers);
        // Klartextkopf bis auf die ID byteweise gleich
        Assert.Equal(template[..0xB4], patched[..0xB4]);
        Assert.Equal(template[0xB8..0xBA], patched[0xB8..0xBA]);

        foreach (var (block, _, data) in before.Blocks)
        {
            var other = after.Block(block.Name);
            if (block.Name != "965B")
                Assert.Equal(data, other);
            else
            {
                var diff = Enumerable.Range(0, data.Length).Where(i => data[i] != other[i]).ToList();
                Assert.All(diff, i => Assert.True(i is >= 0x97EA - 0x965B and < 0x97EA - 0x965B + 4 || i == 0x973F - 0x965B));
                Assert.Equal(2_250_000, BitConverter.ToInt32(other, 0x97EA - 0x965B));
                Assert.Equal(21, other[0x973F - 0x965B]);
            }
        }
    }

    [Fact]
    public void Patch_WithoutChanges_OnlyChangesIdAndStaysDecodable()
    {
        var template = Template();
        var patched = OriginalSavePatcher.Patch(template, [], random: new Random(7));
        var a = OriginalSaveGame.Read(template);
        var b = OriginalSaveGame.Read(patched);

        Assert.True(b.ChecksumsValid);
        Assert.NotEqual(a.SaveId, b.SaveId);
        Assert.InRange(b.SaveId, short.MinValue, -1);
        Assert.Equal(template.Length, patched.Length);
        Assert.All(a.Blocks, x => Assert.Equal(x.Data, b.Block(x.Block.Name)));
    }

    /// <summary>Lesen und wieder Schreiben ergibt dieselben Bytes (Gegenprobe der Verschlüsselung und der Prüfsummen).</summary>
    [Fact]
    public void Encode_RoundTripsReadResult_ForRandomContent()
    {
        var rnd = new Random(42);
        var template = Template(fill: blocks =>
        {
            foreach (var b in blocks.Values.Where(v => v.Length > 1)) rnd.NextBytes(b);
            blocks["0386"][0] = 1;
        }, key: 0x9C);
        var save = OriginalSaveGame.Read(template);

        Assert.Equal(template, OriginalSavePatcher.Encode(template.AsSpan(0, OriginalSaveGame.PlainLength), save));
    }

    [Fact]
    public void Patch_DatumZaehlerSpieltagLevel_SetAllCoupledBytes()
    {
        var patched = OriginalSavePatcher.Patch(Template(),
            [C("datum", "20.09.1993"), C("zaehler", "11"), C("spieltag", "4"), C("level", "5")], newId: 5);
        var s = OriginalSaveGame.Read(patched);

        Assert.True(s.ChecksumsValid);
        Assert.Equal(20, BitConverter.ToInt32(s.Block("5A20")));
        Assert.Equal(8, BitConverter.ToInt32(s.Block("5A1C"))); // Monat 0-basiert
        Assert.Equal(1993, BitConverter.ToInt32(s.Block("5A18")));
        Assert.Equal(1993, s.HeaderYear);
        Assert.Equal(11, BitConverter.ToInt32(s.Block("03B7")));
        Assert.Equal(11, BitConverter.ToInt16(s.Block("A969")));
        Assert.Equal([4, 4, 4], s.Block("07A9").Take(3));
        Assert.Equal(4, s.HeaderByte7A9);
        Assert.Equal(5, s.Block("0118")[0]);
        Assert.Equal(0, s.Block("0ED8")[0]);
    }

    [Fact]
    public void Patch_StadiumAndSecondManager()
    {
        var template = Template(fill: b => b["0386"][0] = 2);
        var patched = OriginalSavePatcher.Patch(template,
            [C("stadion", "1,2,3,4,5,6,7,8,9"), C("sitz", "5000"), C("steh", "250")], manager: 1, newId: 9);
        var m = OriginalSaveGame.Read(patched).Block("965B");
        var o = OriginalSaveGame.ManagerRecordLength;

        Assert.Equal(Enumerable.Range(1, 9), Enumerable.Range(0, 9).Select(n => (int)BitConverter.ToInt16(m, o + 0x9771 - 0x965B + 2 * n)));
        Assert.Equal(5000, BitConverter.ToInt16(m, o + 0x9771 - 0x965B + 0x1D));
        Assert.Equal(250, BitConverter.ToInt16(m, o + 0x9771 - 0x965B + 0x21));
        Assert.All(m.AsSpan(0, o).ToArray(), x => Assert.Equal(0, x)); // Manager 0 unberührt
    }

    [Theory]
    [InlineData("gibtsnicht", "1")]   // unbekannt
    [InlineData("dauer", "2")]        // abgewiesen, nicht belegt
    [InlineData("liga", "1")]
    [InlineData("staerke", "2")]
    [InlineData("eintritt", "0")]     // Bereich
    [InlineData("eintritt", "100")]
    [InlineData("eintritt", "x")]
    [InlineData("level", "6")]
    [InlineData("datum", "31.02.1993")]
    [InlineData("datum", "1993")]
    [InlineData("stadion", "1,2,3")]
    [InlineData("stadion", "1,2,3,4,5,6,7,8,10")]
    [InlineData("spieltag", "0")]
    [InlineData("konto", "999999999999")]
    public void Patch_RejectsUnknownRejectedAndInvalidFields(string field, string value)
    {
        Assert.Throws<ArgumentException>(() => OriginalSavePatcher.Patch(Template(), [C(field, value)]));
    }

    [Fact]
    public void Patch_RejectsDuplicateFieldBadManagerAndBadTemplate()
    {
        var template = Template();
        Assert.Throws<ArgumentException>(() => OriginalSavePatcher.Patch(template, [C("konto", "1"), C("KONTO", "2")]));
        Assert.Throws<ArgumentException>(() => OriginalSavePatcher.Patch(template, [C("konto", "1")], manager: 1));
        template[^1] ^= 0xFF;
        Assert.Throws<InvalidDataException>(() => OriginalSavePatcher.Patch(template, [C("konto", "1")]));
    }

    [Fact]
    public void Patch_DoesNotModifyTemplateBytes()
    {
        var template = Template();
        var copy = (byte[])template.Clone();
        OriginalSavePatcher.Patch(template, [C("konto", "5")]);
        Assert.Equal(copy, template);
    }

    [Fact]
    public void NewId_StaysInObservedRangeAndDiffersFromOld()
    {
        var rnd = new Random(1);
        for (var i = 0; i < 20_000; i++)
        {
            var id = OriginalSavePatcher.NewId(5, rnd);
            Assert.NotEqual(5, id);
            Assert.InRange(id, short.MinValue, -1); // wie FILE01–03: 16 Bit 0x8000–0xFFFF, vorzeichenerweitert
        }
    }

    // ---- Teil 2 hok-patch-tabellen (Option A, Nutzer 28.11.2026): startliga und ligaplaetze ----

    [Fact]
    public void Patch_Startliga_SetsOnlyRegulator0x11A()
    {
        var template = Template(fill: b => b["0118"][1] = 3);
        var before = OriginalSaveGame.Read(template);
        var after = OriginalSaveGame.Read(OriginalSavePatcher.Patch(template, [C("startliga", "2")], newId: 3));

        Assert.True(after.ChecksumsValid);
        Assert.Equal([0, 3, 2, 0], after.Block("0118"));
        Assert.All(before.Blocks.Where(b => b.Block.Name != "0118"), b => Assert.Equal(b.Data, after.Block(b.Block.Name)));
    }

    /// <summary>[ORIGINAL] Datei 0x2890E–0x28920: bei Dauer „eine Saison“ nur Start-Liga Bundesliga.</summary>
    [Fact]
    public void Patch_Startliga_RejectsOtherLeagueForOneSeason()
    {
        var template = Template(); // Dauer 0
        var ex = Assert.Throws<ArgumentException>(() => OriginalSavePatcher.Patch(template, [C("startliga", "1")]));
        Assert.Contains("eine Saison", ex.Message);
        Assert.Equal(0, OriginalSaveGame.Read(OriginalSavePatcher.Patch(template, [C("startliga", "0")])).Block("0118")[2]);
        Assert.Throws<ArgumentException>(() => OriginalSavePatcher.Patch(Template(fill: b => b["0118"][1] = 3), [C("startliga", "3")]));
    }

    [Fact]
    public void Patch_Ligaplaetze_WritesFixedPointAndNothingElse()
    {
        // Bundesliga: Satz 0–11, Punkte 30 − Satz → gespeicherte Folge 0…11; Satz 4 und 5 punktgleich mit gleichen Toren.
        var template = LeagueTemplate(day: 10);
        var before = OriginalSaveGame.Read(template);
        var after = OriginalSaveGame.Read(OriginalSavePatcher.Patch(template, [C("ligaplaetze", "0:0,1,2,3,5,4,6,7,8,9,10,11")], newId: 4));

        Assert.True(after.ChecksumsValid);
        Assert.Equal([0, 1, 2, 3, 5, 4, 6, 7, 8, 9, 10, 11], after.LeaguePlaces(0));
        Assert.Equal(before.LeaguePlaces(1), after.LeaguePlaces(1));
        Assert.All(before.Blocks.Where(b => b.Block.Name != "5CCB"), b => Assert.Equal(b.Data, after.Block(b.Block.Name)));
    }

    /// <summary>Regressionstest: Vor dem 45. Spieltag sortiert das Hauptmenü (0x44EFD → 0x50B45) jede andere Folge um.</summary>
    [Fact]
    public void Patch_Ligaplaetze_RejectsOrderTheMainMenuWouldResort()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            OriginalSavePatcher.Patch(LeagueTemplate(day: 44), [C("ligaplaetze", "0:1,0,2,3,4,5,6,7,8,9,10,11")]));
        Assert.Contains("0x50B45", ex.Message);
        Assert.Contains("0,1,2,3,4,5,6,7,8,9,10,11", ex.Message);
    }

    [Theory]
    [InlineData("0:0,1,2,3,4,5,6,7,8,9,10,12")]  // fremder Satz (2. Liga)
    [InlineData("0:0,1,2,3,4,5,6,7,8,9,10,10")]  // doppelt
    [InlineData("0:0,1,2,3,4,5,6,7,8,9,10")]     // nur 11
    [InlineData("3:0,1,2,3,4,5,6,7,8,9,10,11")]  // Liga
    [InlineData("0,1,2,3,4,5,6,7,8,9,10,11")]    // ohne Liga
    public void Patch_Ligaplaetze_RejectsSyntaxAndOtherSlots(string value)
    {
        Assert.Throws<ArgumentException>(() => OriginalSavePatcher.Patch(LeagueTemplate(day: 45), [C("ligaplaetze", value)]));
    }

    /// <summary>Nach dem 44. Spieltag sortiert 0x50B45 nicht mehr (Datei 0x50B4D–0x50B71); „spieltag“ im selben Aufruf zählt.</summary>
    [Fact]
    public void Patch_Ligaplaetze_AfterSeasonAcceptsAnyOrder()
    {
        const string order = "1:23,22,21,20,19,18,17,16,15,14,13,12";
        var after = OriginalSaveGame.Read(OriginalSavePatcher.Patch(LeagueTemplate(day: 45), [C("ligaplaetze", order)]));
        Assert.Equal(Enumerable.Range(12, 12).Reverse(), after.LeaguePlaces(1));

        Assert.Throws<ArgumentException>(() => OriginalSavePatcher.Patch(LeagueTemplate(day: 20), [C("ligaplaetze", order)]));
        var later = OriginalSaveGame.Read(OriginalSavePatcher.Patch(LeagueTemplate(day: 20), [C("ligaplaetze", order), C("spieltag", "45")]));
        Assert.Equal(Enumerable.Range(12, 12).Reverse(), later.LeaguePlaces(1));
    }

    /// <summary>Ausgeloste Aufstiegsrunde (0xCFFC) und gesetzte Play-offs (0x5D3B) müssen zur neuen Liste passen.</summary>
    [Fact]
    public void Patch_Ligaplaetze_KeepsPromotionRoundAndPlayoffConsistent()
    {
        var drawn = LeagueTemplate(day: 45, fill: b =>
        {
            byte[] cffc = [10, 11, 12, 13, 22, 23, 24, 25];
            cffc.CopyTo(b["CFFC"], 0);
            byte[] seeds = [0, 7, 1, 6, 2, 5, 3, 4];
            seeds.CopyTo(b["5CFB"], 0x40);
        });

        // Gruppe 0: Plätze 11, 12 der Bundesliga, Gruppe 1: Plätze 1, 2 der Oberliga
        Assert.Throws<ArgumentException>(() => OriginalSavePatcher.Patch(drawn, [C("ligaplaetze", "0:0,1,2,3,4,5,6,7,8,9,11,10")]));
        Assert.Throws<ArgumentException>(() => OriginalSavePatcher.Patch(drawn, [C("ligaplaetze", "2:25,24,26,27,28,29,30,31,32,33,34,35")]));
        Assert.Throws<ArgumentException>(() => OriginalSavePatcher.Patch(drawn, [C("ligaplaetze", "1:12,13,14,15,16,17,18,19,20,21,23,22")]));
        // Play-offs: Platz 1 gegen 8 bleibt, nur wenn die Paare bleiben
        Assert.Throws<ArgumentException>(() => OriginalSavePatcher.Patch(drawn, [C("ligaplaetze", "0:0,1,2,3,4,5,6,8,7,9,10,11")]));
        var swapped = OriginalSaveGame.Read(OriginalSavePatcher.Patch(drawn, [C("ligaplaetze", "0:7,1,2,3,4,5,6,0,8,9,10,11")]));
        Assert.Equal(7, swapped.LeaguePlaces(0)[0]);
        // Oberliga Platz 3–12 frei (A671: Managersatz auf Platz 12)
        var low = OriginalSaveGame.Read(OriginalSavePatcher.Patch(drawn, [C("ligaplaetze", "2:24,25,35,34,33,32,31,30,29,28,27,26")]));
        Assert.Equal(26, low.LeaguePlaces(2)[11]);
        // Mitten in der Saison gelten die Altwerte nicht (Fixpunkt reicht)
        var mid = LeagueTemplate(day: 30, fill: b => ((byte[])[10, 11, 12, 13, 22, 23, 24, 25]).CopyTo(b["CFFC"], 0));
        Assert.Equal([0, 1, 2, 3, 5, 4, 6, 7, 8, 9, 10, 11],
            OriginalSaveGame.Read(OriginalSavePatcher.Patch(mid, [C("ligaplaetze", "0:0,1,2,3,5,4,6,7,8,9,10,11")])).LeaguePlaces(0));
    }

    /// <summary>[ORIGINAL] 0x50B45: Punkte, dann weniger Spiele, dann Tordifferenz, dann Tore; Gleichstand tauscht nicht.</summary>
    [Fact]
    public void SortLeague_UsesPointsGamesDifferenceGoals_HomePlusAway()
    {
        var table = new byte[0x1400];
        void Set(int slot, int pointsHome, int pointsAway, int games, int goals, int against)
        {
            var r = OriginalSaveGame.TableRecordStart + OriginalSaveGame.TableRecordLength * slot;
            table[r] = (byte)pointsHome;
            table[r + 1] = (byte)pointsAway;
            table[r + 0x1E] = (byte)games;
            table[r + 0x16] = (byte)goals;
            table[r + 0x1A] = (byte)against;
        }
        Set(0, 5, 5, 10, 30, 30);   // 10 Punkte, 10 Spiele
        Set(1, 4, 6, 9, 20, 25);    // 10 Punkte, 9 Spiele → vor 0
        Set(2, 10, 1, 12, 10, 40);  // 11 Punkte → Erster
        Set(3, 6, 4, 10, 31, 30);   // wie 0, Tordifferenz +1 → vor 0
        Set(4, 6, 4, 10, 32, 31);   // wie 3, mehr Tore → vor 3
        Set(5, 6, 4, 10, 32, 31);   // wie 4 → bleibt hinter 4

        Assert.Equal([2, 1, 4, 5, 3, 0], OriginalSavePatcher.SortLeague(table, [0, 1, 2, 3, 4, 5]));
    }

    // ---- Originaldaten: Spielstände des Nutzers (Referenz-Repo original/spielstaende, nie im Code-Repo) ----

    [GameDataFact]
    public void Original_RoundTripIsByteIdentical_ForAllSavedGames()
    {
        foreach (var file in OriginalSaves())
        {
            var bytes = File.ReadAllBytes(file);
            var save = OriginalSaveGame.Read(bytes);
            Assert.Equal(bytes, OriginalSavePatcher.Encode(bytes.AsSpan(0, OriginalSaveGame.PlainLength), save));
        }
    }

    [GameDataFact]
    public void Original_PatchedFile01_HasValidChecksumsAndOnlyTheChangedFields()
    {
        var bytes = File.ReadAllBytes(OriginalSave("FILE01.HOK"));
        var before = OriginalSaveGame.Read(bytes);

        var patched = OriginalSavePatcher.Patch(bytes,
            [C("konto", "1000000"), C("eintritt", "25"), C("datum", "20.09.1993"), C("zaehler", "11"), C("spieltag", "4")],
            random: new Random(3));
        var after = OriginalSaveGame.Read(patched);

        Assert.True(after.ChecksumsValid);
        Assert.Equal(bytes.Length, patched.Length);
        Assert.NotEqual(before.SaveId, after.SaveId);
        Assert.Equal(before.Key, after.Key);
        Assert.Equal(before.Filler, after.Filler);
        Assert.Equal(3_000_000, BitConverter.ToInt32(after.Block("965B"), 0x97EA - 0x965B));
        Assert.Equal(25, after.Block("965B")[0x973F - 0x965B]);
        var changed = before.Blocks.Where(b => !b.Data.AsSpan().SequenceEqual(after.Block(b.Block.Name))).Select(b => b.Block.Name).Order();
        // FILE01 steht schon im 09.1993: vom Datum ändert sich nur der Tag.
        Assert.Equal(new[] { "03B7", "07A9", "5A20", "965B", "A969" }.Order(), changed);
    }

    /// <summary>FILE02 ist FILE01 mit Eintritt 17: gleicher Patch auf FILE01 ergibt denselben Klartext wie FILE02.</summary>
    [GameDataFact]
    public void Original_PatchingFile01WithEintritt17_EqualsPlaintextOfFile02()
    {
        var f1 = File.ReadAllBytes(OriginalSave("FILE01.HOK"));
        var f2 = OriginalSaveGame.Read(File.ReadAllBytes(OriginalSave("FILE02.HOK")));
        var p = OriginalSaveGame.Read(OriginalSavePatcher.Patch(f1, [C("eintritt", "17")]));

        Assert.All(f2.Blocks, b => Assert.Equal(b.Data, p.Block(b.Block.Name)));
    }

    /// <summary>Gespeicherte Platzlisten sind Fixpunkte der nachgebauten Sortierung (Gegenprobe zu fixpunkt.py).</summary>
    [GameDataFact]
    public void Original_StoredPlaceListsAreFixedPointsOfSortLeague()
    {
        foreach (var name in new[] { "FILE03.HOK", "FILE05.HOK", "FILE13.HOK", "FILE16A.HOK", "FILE18.HOK", "FILE22.HOK", "FILE36.HOK", "FILE44.HOK" })
        {
            var save = OriginalSaveGame.Read(File.ReadAllBytes(OriginalSave(name)));
            for (var league = 0; league < OriginalSaveGame.LeagueCount; league++)
                Assert.Equal(save.LeaguePlaces(league), OriginalSavePatcher.SortLeague(save.Block("8257"), save.LeaguePlaces(league)));
        }
    }

    /// <summary>FILE05 (4. Spieltag): umgekehrte Bundesliga wird abgewiesen, die gespeicherte angenommen (unveränderter Rest).</summary>
    [GameDataFact]
    public void Original_File05_LigaplaetzeOnlyFixedPoint()
    {
        var bytes = File.ReadAllBytes(OriginalSave("FILE05.HOK"));
        var save = OriginalSaveGame.Read(bytes);
        var stored = save.LeaguePlaces(0);
        Assert.Throws<ArgumentException>(() => OriginalSavePatcher.Patch(bytes, [C("ligaplaetze", "0:" + string.Join(",", stored.Reverse()))]));

        var same = OriginalSaveGame.Read(OriginalSavePatcher.Patch(bytes, [C("ligaplaetze", "0:" + string.Join(",", stored))]));
        Assert.All(save.Blocks, b => Assert.Equal(b.Data, same.Block(b.Block.Name)));
    }

    /// <summary>
    /// Vorlage A667 (hok-tabellen.md): FILE44 nach der Saison, Aufstiegsrunde und Play-offs ausgelost. Start-Liga 2. Liga geht
    /// (Dauer 3); die Plätze 2 und 3 der Bundesliga lassen sich tauschen, Platz 11/12 (Aufstiegsrunde) nicht.
    /// </summary>
    [GameDataFact]
    public void Original_File44_StartligaAndPromotionRoundGuard()
    {
        var bytes = File.ReadAllBytes(OriginalSave("FILE44.HOK"));
        var save = OriginalSaveGame.Read(bytes);
        Assert.Equal(45, save.NextMatchDay(0));

        var p = OriginalSaveGame.Read(OriginalSavePatcher.Patch(bytes, [C("startliga", "1")]));
        Assert.True(p.ChecksumsValid);
        Assert.Equal(1, p.Block("0118")[2]);
        Assert.Equal(["0118"], save.Blocks.Where(b => !b.Data.AsSpan().SequenceEqual(p.Block(b.Block.Name))).Select(b => b.Block.Name));

        var bl = save.LeaguePlaces(0).ToArray();
        var outside = (int[])bl.Clone();
        (outside[0], outside[7]) = (outside[7], outside[0]); // Paar 1–8 bleibt
        Assert.Equal(outside, OriginalSaveGame.Read(OriginalSavePatcher.Patch(bytes, [C("ligaplaetze", "0:" + string.Join(",", outside))])).LeaguePlaces(0));
        var inside = (int[])bl.Clone();
        (inside[10], inside[11]) = (inside[11], inside[10]);
        Assert.Throws<ArgumentException>(() => OriginalSavePatcher.Patch(bytes, [C("ligaplaetze", "0:" + string.Join(",", inside))]));
    }

    /// <summary>Vorlage A671: im Modus „nur Weltkugel“ (FILE18) sind alle Sortierschlüssel gleich, jede Oberligafolge bleibt stehen.</summary>
    [GameDataFact]
    public void Original_File18_WorldOnlyAcceptsAnyOberligaOrder()
    {
        var bytes = File.ReadAllBytes(OriginalSave("FILE18.HOK"));
        var save = OriginalSaveGame.Read(bytes);
        var reversed = save.LeaguePlaces(2).Reverse().ToArray();
        var p = OriginalSaveGame.Read(OriginalSavePatcher.Patch(bytes, [C("ligaplaetze", "2:" + string.Join(",", reversed))]));
        Assert.Equal(reversed, p.LeaguePlaces(2));
        Assert.Equal(["5CCB"], save.Blocks.Where(b => !b.Data.AsSpan().SequenceEqual(p.Block(b.Block.Name))).Select(b => b.Block.Name));
    }

    private static IEnumerable<string> OriginalSaves() =>
        new[] { "FILE01.HOK", "FILE02.HOK", "FILE03.HOK" }.Select(OriginalSave);

    /// <summary>
    /// Ordner mit den Original-Spielständen: <c>EM_SAVEGAMES</c> oder <c>spielstaende</c> neben dem (aufgelösten)
    /// Ordner <c>gamedata</c> (Referenz-Repo: <c>original/gamedata</c>, <c>original/spielstaende</c>).
    /// </summary>
    private static string OriginalSave(string name)
    {
        var folder = Environment.GetEnvironmentVariable("EM_SAVEGAMES");
        if (string.IsNullOrEmpty(folder))
        {
            var root = TestGameData.Load().RootDirectory;
            var real = Directory.ResolveLinkTarget(root.TrimEnd('/', '\\'), true)?.FullName ?? root;
            folder = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(real).TrimEnd('/', '\\'))!, "spielstaende");
        }
        var path = Path.Combine(folder, name);
        return File.Exists(path) ? path
            : throw new FileNotFoundException($"Original-Spielstand {name} fehlt in {folder} (Ordner per EM_SAVEGAMES angeben).");
    }

    /// <summary>Stand mit Platzliste Satz 0–35 der Reihe nach, nächster Spieltag <paramref name="day"/>, Punkte 30 − Satz
    /// je Liga (Satz 4 und 5 punkt- und torgleich), Dauer 3.</summary>
    private static byte[] LeagueTemplate(int day, Action<Dictionary<string, byte[]>>? fill = null) => Template(fill: b =>
    {
        b["0118"][1] = 3;
        for (var i = 0; i < 36; i++)
            b["5CCB"][i] = (byte)i;
        b["07A9"][0] = b["07A9"][1] = b["07A9"][2] = (byte)day;
        for (var slot = 0; slot < 36; slot++)
        {
            var r = OriginalSaveGame.TableRecordStart + OriginalSaveGame.TableRecordLength * slot;
            b["8257"][r] = (byte)(30 - slot % 12 - (slot % 12 == 4 ? 1 : 0));
            b["8257"][r + 0x1E] = 10;
        }
        fill?.Invoke(b);
    });

    private static byte[] Template(Action<Dictionary<string, byte[]>>? fill = null, byte key = 0xE5)
    {
        var blocks = OriginalSaveGame.Layout.Where(b => !b.IsChecksum).ToDictionary(b => b.Name, b => new byte[b.Length]);
        blocks["0386"][0] = 1;
        fill?.Invoke(blocks);

        var file = new List<byte>();
        file.AddRange("KRON_0.99\0"u8.ToArray());
        file.AddRange(BitConverter.GetBytes(1993));
        file.Add(blocks["0386"][0]);
        file.Add(1);
        var header = new byte[0xA0];
        for (var m = 0; m < blocks["0386"][0]; m++)
        {
            "Joerg"u8.CopyTo(header.AsSpan(m * 0x28 + 2));
            "K\\LNER EC"u8.CopyTo(header.AsSpan(m * 0x28 + 0x15));
        }
        file.AddRange(header);
        file.AddRange("EH0\0"u8.ToArray());
        file.AddRange(BitConverter.GetBytes(-26229));
        file.Add(0x35);
        file.Add(key);

        byte k = key, sum = key;
        foreach (var block in OriginalSaveGame.Layout)
        {
            if (block.IsChecksum)
            {
                file.Add(sum);
                continue;
            }
            foreach (var plain in blocks[block.Name])
            {
                file.Add((byte)((plain ^ k) + k));
                sum += plain;
                k -= 0x29;
            }
        }
        return [.. file];
    }
}
