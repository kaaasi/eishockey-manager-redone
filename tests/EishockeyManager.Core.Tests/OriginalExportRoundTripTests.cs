using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using Xunit.Abstractions;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur hok-export, Teil 2 (Rundreise): (1) jeder Original-Stand FILE01–FILE49A → Import → Export gleicht dem Klartext des
/// Originals bis auf die Felder in <see cref="Allowed"/> (Feldkarte: neu berechnet, Status N/U, Rundung, ungedeutet); (2) Remake →
/// HOK → Remake → HOK ohne Abweichung, auch über vier Saisonwechsel mit passivem und aktivem Manager (Mehrsaisonprobe).
/// </summary>
public sealed class OriginalExportRoundTripTests(ITestOutputHelper output)
{
    private const int Seed = 4711;

    /// <summary>Merkmale eines Original-Stands, an die eine Regel gebunden sein kann.</summary>
    [Flags]
    private enum When
    {
        Always = 0,
        /// <summary>Modus „nur Weltkugel“ (0xD0B9 = 1): Satz und Kader des Managervereins übernimmt der Import nicht (Regel 1).</summary>
        WorldOnly = 1,
        /// <summary>Folgesaison vor dem ersten Spieltag (0x3BD &gt; 0, 0x7A9 = 1): Vorsaisonwerte, die das Remake nicht führt.</summary>
        NewSeason = 2,
        /// <summary>Ab der zweiten Saison (0x3BD &gt; 0): Saisonverlauf und Titel der Vorsaison führt das Remake nach dem Import nicht (History N).</summary>
        LaterSeason = 4,
    }

    /// <summary>Erlaubte Abweichung: Block, Bereich im Satz (Versatz mod Satzlänge), Bedingung, Begründung.</summary>
    private sealed record Rule(string Block, int Record, int From, int To, string Reason, When When = When.Always)
    {
        public bool Covers(string block, int offset, When stand)
        {
            if (block != Block || (When != When.Always && (stand & When) == 0))
                return false;
            // Tabellensätze beginnen bei DGROUP 0x825A, also 3 Byte hinter dem Blockanfang 0x8257.
            var at = Block == "8257" ? (offset - 3 + Record) % Record : Record > 0 ? offset % Record : offset;
            return at >= From && at <= To;
        }
    }

    /// <summary>
    /// Felder, die der Export nicht wie das Original schreiben kann. Jede Zeile nennt den Grund; die Liste ist die Abnahme von
    /// Teil 2 („nur Abweichungen in Feldern, die die Feldkarte als neu berechnet führt“).
    /// </summary>
    private static readonly Rule[] Allowed =
    [
        // Kopf und Zähler
        new("0178", 0, 0, 0, "ungedeutet (Vereinssatz, FILE01 6)"),
        new("A88D", 0, 0, 3, "ungedeutet (ändert sich mit dem Spieltag)"),
        new("CAE3", 0, 0, 5, "ungedeutet"),
        new("CC73", 0, 0, 0x2F, "Minuten-Vorgabe umgerechnet: Umrechnung aus 0xCC43 nicht belegt"),
        new("021D", 0, 15, 15, "ungedeutet (nur Weltkugel)"),
        new("A8A0", 0, 0, 0, "ungedeutet (meist der Monat)"),
        new("A70E", 0, 0, 0, "Zug des Bundestrainers: Import setzt Manager 0 (Feldkarte, OriginalImport.ApplyOriginal)"),
        new("0375", 0, 0, 11, "„automatisch aufstellen“ gesichert 0x376 + 2 · m: das Remake führt einen Wert (Feldkarte Club._autoLineup)"),
        new("07A9", 0, 4, 4, "ungedeutet (0x7AD, laufende Pokalrunde?)"),
        // Titelträger der Vorsaison und Tabellenfolge vor dem ersten Spieltag: im Remake nur in History (Feldkarte N)
        new("0387", 0, 0, 0, "Vorjahressieger DEB-Pokal nur in History (Feldkarte N, R14HI-04)"),
        new("D0E6", 0, 0, 0, "Vorjahresmeister nur in History (Feldkarte N)"),
        new("5CCB", 0, 0, 35, "Tabellenfolge bei Gleichstand (vor dem ersten Spieltag Startordnung bzw. Vorsaison, LeagueTable.StartOrder)"),
        new("5D7B", 0, 0, 35, "Paarungen nach Saisonende bzw. in gepatchten Ständen (FILE13/FILE16: Datum ohne Spieltag)"),
        // Länder: Satzfolge ohne WM-Objekt nicht im Remake
        new("06AC", 0, 0, 0, "Satz des Landes: Satzfolge der Länder ohne WM (Feldkarte U)"),
        new("026C", 0, 0, 0x2F, "Kosten je Satz der Länder: Satzfolge ohne WM"),
        new("8257", 0x50, 0x49, 0x49, "Wert je Land (+0x49, Weltrangliste?) ungedeutet, Feldkarte _worldCupRankings U"),
        // Managersatz
        new("965B", 0x1AF, 0xF0, 0xF0, "ungedeutet"),
        new("965B", 0x1AF, 0xF3, 0xF3, "ungedeutet"),
        new("965B", 0x1AF, 0x13, 0x13, "Managerkopf im Modus „nur Weltkugel“ und Bundestrainer-Satz (Feldkarte U)"),
        new("965B", 0x1AF, 0x18F, 0x192, "Konto: Rundung Drittel-DM → DM (Regel 4)"),
        new("965B", 0x1AF, 0x197, 0x19A, "zuletzt berechnete Einnahmen (Feldkarte U, LastHomeGame)"),
        new("965B", 0x1AF, 0x19B, 0x1AE, "Darlehen: Rundung Drittel-DM"),
        // Vereinssätze
        new("far:9657", 0x3D, 0x1E, 0x25, "ungedeutet (Zufallswert 45–55, Folgebytes)"),
        new("far:9657", 0x3D, 0x26, 0x3A, "Automatik-Anzeige der Computervereine (vor jedem Spiel neu, 0x8365) und ungedeutete Bytes"),
        new("far:9657", 0x3D, 0x00, 0x15, "Name im Modus „nur Weltkugel“ (Land statt Verein, Regel 1)"),
        // Pool und Kaderplätze
        new("far:8253", 33, 0x13, 0x13, "ungedeutet"),
        new("far:8253", 33, 0x15, 0x17, "Grundwerte vergebener und gelisteter Poolspieler: das Remake führt einen Wertesatz je Spieler"),
        new("far:8253", 33, 0x18, 0x18, "Rollenwert: das Remake führt nur die Art (Feldkarte Player.Role)"),
        new("far:8253", 33, 0x19, 0x1C, "ungedeutet (Statistik der Poolspieler?)"),
        new("far:8253", 33, 0x1D, 0x1D, "Verein eines Nominierten (Land) bzw. ohne übernommenen Kader"),
        new("far:8253", 33, 0x1E, 0x20, "offene Meldung, Vorlagen/Minuten der Poolspieler (Feldkarte: nur Kaderspieler)"),
        new("far:8253", 33, 0x00, 0x11, "Name: Poolspieler, die beim Import einem Remake-Namen weichen (jeder Name nur einmal)"),
        new("5DC3", 0x34, 0x02, 0x02, "ungedeutet"),
        new("5DC3", 0x34, 0x1A, 0x1B, "ungedeutet (Kaderplatz; Markt: Sperre)"),
        new("5DC3", 0x34, 0x22, 0x27, "Torhüter: Vereinswert Gegentore (Feldkarte: Torhüter 0)"),
        new("5DC3", 0x34, 0x28, 0x2F, "Markt: Gehalt und Rundung Drittel-DM (Feldkarte TransferListing)"),
        new("5DC3", 0x34, 0x0A, 0x0A, "eingesetzt: Automatik neu berechnet"),
        new("5DC3", 0x34, 0x10, 0x14, "Werte im Nationalkader: ein Wertesatz je Spieler (Feldkarte, Teil 4b)"),
        new("5DC3", 0x34, 0x0E, 0x0E, "Tendenzwert 0 (nicht gesetzt) übernimmt die Stufe des Nachspielens"),
        // Pokale
        new("5CFB", 0, 0, 0x7F, "Reste älterer Pokalrunden hinter 0xFF (0x3489D) und K.-o.-Plätze vor dem fünften WM-Termin"),
        new("5CF3", 0, 0, 7, "Runde ungenutzter Wettbewerbe"),
        new("CED8", 0, 0, 0x11F, "Füllpaare und Paarungen außerhalb laufender Gruppen"),
        new("CDB8", 0, 0, 0x11F, "Tore nicht ausgetragener Termine"),
        new("CD88", 0, 0, 0x2F, "Platzliste außerhalb laufender Gruppen"),
        new("CD58", 0, 0, 0x2F, "Gruppentabelle je Platz/Teilnehmer nicht belegt"),
        new("CD28", 0, 0, 0x2F, "Gruppentabelle je Platz/Teilnehmer nicht belegt"),
        new("CCF8", 0, 0, 0x2F, "Gruppentabelle je Platz/Teilnehmer nicht belegt"),
        new("CFFC", 0, 0, 0x17, "Teilnehmer abgeschlossener Gruppen"),
        new("CCB2", 0, 0, 0x45, "Marken (+30) der Play-off-Spiele (R14HI-04)"),
        new("CCA4", 0, 0, 0x0D, "Siegbyte 0x14 und Siege abgeschlossener Runden"),
        new("CFF8", 0, 0, 3, "ungedeutet"),
        new("5C4B", 0, 0, 0x7F, "Pokaltore des laufenden Tages (Reste)"),
        new("A776", 0, 0, 9, "Europapokal-Teilnehmer der Vorsaison"),
        new("A763", 0, 0, 0x11, "Auslosung Europapokal (offen)"),
        // Rest
        new("C313", 10, 0, 3, "Werbeangebote: Rundung Drittel-DM"),
        new("C313", 10, 4, 9, "Werbeangebote mit Betrag 0 gelten als gelöscht (Import)"),
        new("D294", 0x71, 0, 0x70, "Reste gelöschter Nachrichten hinter freiem Satz"),
        new("AB7B", 0, 0, 0x1593, "Serien, Rekorde, Ewige Tabelle: Vorsaison und ungedeutete Teile"),
        new("D0F4", 0, 0, 0x11F, "Kursverlauf: Rundung Drittel-DM"),
        new("D0DB", 0, 0, 7, "Freundschaftsspiele: Satzfolge der Länder"),
        new("D0D3", 0, 0, 7, "Freundschaftsspiele: Satzfolge der Länder"),
        new("D0CB", 0, 0, 7, "Freundschaftsspiele"),
        new("D0C3", 0, 0, 7, "Freundschaftsspiele"),
        new("0178", 0, 0, 3, "ungedeutet (je Manager ein Satz? FILE09: 03 0A)"),
        new("0388", 0, 0, 0, "Vorjahressieger Europapokal nur in History (Feldkarte N)", When.LaterSeason),
        new("038B", 0, 0, 0, "ungedeutet (ab dem ersten Saisonende 1)"),
        new("5DC3", 0x34, 0x03, 0x08, "ungenutzte Kadersätze und Marktplätze: Reste der Tor- und Spielzähler (Feldkarte: ein Wertesatz je Spieler)"),
        new("5DC3", 0x34, 0x15, 0x15, "Reihe verletzter oder gelisteter Spieler: das Remake stellt sie nicht auf"),
        new("5DC3", 0x34, 0x16, 0x17, "Verletzungsart nach der Heilung, Anfrage-Verein ohne Kennzeichen (Import liest beide nur mit Kennzeichen)"),
        new("5DC3", 0x34, 0x19, 0x19, "verdeckte Werte der Marktplätze: ein Wertesatz je Spieler"),
        new("5DC3", 0x34, 0x0F, 0x0F, "Poolplatz eines aus dem Pool gekauften Spielers mit zweitem Poolsatz gleichen Namens"),
        new("far:8253", 33, 0x14, 0x14, "Alter eines Poolsatzes ohne Spieler im Remake (zweiter Satz gleichen Namens)"),
        new("5DC3", 0x34, 0x09, 0x0D, "Kadersatz des Managers bzw. Vertragslose (+0x0B = 0xFF) am Saisonstart", When.LaterSeason),
        // Modus „nur Weltkugel“: Managerverein ohne Kader und Satz (Regel 1, OriginalImport.ApplyOriginal)
        new("5DC3", 0x34, 0x00, 0x33, "Kader des Managervereins im Modus „nur Weltkugel“ (Import übernimmt ihn nicht)", When.WorldOnly),
        new("far:8253", 33, 0x12, 0x14, "Poolsätze des Managervereins im Modus „nur Weltkugel“", When.WorldOnly),
        new("far:9657", 0x3D, 0x16, 0x1D, "Satz des Managervereins im Modus „nur Weltkugel“ (trägt das Land, Regel 1)", When.WorldOnly),
        new("965B", 0x1AF, 0x177, 0x182, "Über-/Unterzahl des Nationalteams (Satz 5; Remake führt sie nur je Manager)", When.WorldOnly),
        new("8257", 0x50, 0x00, 0x2F, "WM-Gruppentabellen in den Ländersätzen: schreibt der Export nicht (Restlücke)", When.WorldOnly),
        // Folgesaison vor dem ersten Spieltag
        new("8257", 0x50, 0x00, 0x4F, "Tabellensätze der Vorsaison (Sicherung, Bilanz, Platzfelder, Tabellenfeld)", When.NewSeason),
        new("965B", 0x1AF, 0x18, 0xE3, "Saisonverlauf der Vorsaisons (Highscore, Feldkarte N History)", When.LaterSeason),
        new("965B", 0x1AF, 0xFE, 0x159, "Zuschauerkurve und ungedeutete Wörter der Vorsaison", When.LaterSeason),
        new("965B", 0x1AF, 0x173, 0x173, "Werbebewertung am Saisonstart"),
        new("8257", 0x50, 0x4D, 0x4E, "ungedeutet (Wort hinter den Strafminuten)"),
        new("965B", 0x1AF, 0xE6, 0xE6, "erreichte Pokalrunde: frühere DEB-Pokal-Runden nach dem Import aus dem Nachspielen (Teil 4c)"),
    ];

    /// <summary>
    /// Stände, die (1) nicht prüft: im Menü des Markierungstags gesichert – der Import lässt den Tageswechsel nach dem Laden erneut
    /// laufen (Ladeeigenheit, GameState.LoadQuirk.cs; Nutzer 09.11.2026 C), Börse, Zähler und Zinsen stehen danach einen Tag weiter.
    /// </summary>
    private static readonly HashSet<string> Skipped = ["FILE22.HOK", "FILE44.HOK"];

    public static TheoryData<string> Saves()
    {
        var data = new TheoryData<string>();
        foreach (var file in new[]
                 {
                     "FILE01", "FILE02", "FILE03", "FILE05", "FILE06", "FILE07", "FILE08", "FILE09", "FILE10", "FILE11", "FILE12", "FILE13",
                     "FILE14", "FILE15", "FILE16", "FILE16A", "FILE17", "FILE18", "FILE19", "FILE20", "FILE21", "FILE22", "FILE23", "FILE24",
                     "FILE25", "FILE26", "FILE27", "FILE28", "FILE29", "FILE30", "FILE31", "FILE32", "FILE33", "FILE34", "FILE35", "FILE36",
                     "FILE37", "FILE37A", "FILE38", "FILE39", "FILE40", "FILE41", "FILE43", "FILE43A", "FILE44", "FILE44A", "FILE44B",
                     "FILE46", "FILE47", "FILE48", "FILE49", "FILE49A",
                 })
            data.Add(file + ".HOK");
        return data;
    }

    /// <summary>(1) Original → Remake → HOK: jede Abweichung vom Klartext des Originals ist ein erlaubtes Feld.</summary>
    [GameDataTheory]
    [MemberData(nameof(Saves))]
    public void OriginalStand_ExportMatchesOriginal_ExceptAllowedFields(string file)
    {
        var raw = OriginalExportTests.RawDatabase();
        var database = ClubDatabase.Parse(raw);
        var original = OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave(file)));
        var game = GameState.ImportOriginal(original, database, Seed);
        var exported = OriginalSaveGame.Read(OriginalExport.Export(game, raw, Seed));
        if (Skipped.Contains(file))
        {
            Assert.True(OriginalImport.SavedOnMarkerDay(original));
            Assert.True(exported.ChecksumsValid);
            return;
        }
        var stand = (OriginalImport.Mode(original) == GameMode.WorldOnly ? When.WorldOnly : When.Always)
                    | (OriginalImport.Season(original) > GameState.FirstSeason && OriginalImport.NextMatchDay(original, League.Bundesliga) == 1
                        ? When.NewSeason : When.Always)
                    | (OriginalImport.Season(original) > GameState.FirstSeason ? When.LaterSeason : When.Always);
        // Sätze mit einem anderen Verein als im Original (Satzfolge der deutschen Vereine ohne Liga ab der zweiten Saison, der Länder
        // ohne WM): alle Felder dieser Sätze sind erlaubt (die Satzfolge außerhalb der Ligen führt das Remake nicht).
        var moved = Enumerable.Range(0, OriginalExport.ClubSlots)
            .Where(slot => OriginalImport.ClubName(original.Block("far:9657"), slot) != OriginalImport.ClubName(exported.Block("far:9657"), slot))
            .ToHashSet();

        Assert.True(exported.ChecksumsValid);
        var uncovered = new List<string>();
        var differing = 0;
        foreach (var (block, _, data) in original.Blocks)
        {
            var other = exported.Block(block.Name);
            for (var i = 0; i < data.Length; i++)
            {
                if (data[i] == other[i])
                    continue;
                differing++;
                if (MovedSlotField(block.Name, i, moved) || Allowed.Any(r => r.Covers(block.Name, i, stand)))
                    continue;
                uncovered.Add($"{block.Name}+0x{i:X}: Original 0x{data[i]:X2}, Export 0x{other[i]:X2}");
            }
        }
        var total = original.Blocks.Sum(b => b.Data.Length);
        output.WriteLine($"{file}: {total - differing} von {total} Byte gleich, {differing} in erlaubten Feldern");
        foreach (var u in uncovered)
            output.WriteLine($"UNCOVERED {file} {u}");
        Assert.True(uncovered.Count == 0, $"{file}: {uncovered.Count} Abweichungen außerhalb der Liste, z. B. " +
                                          string.Join("; ", uncovered.Take(12)));
        // Kopf: Managerzahl, Namen, Jahr, Spieltag wie im Original.
        Assert.Equal(original.HeaderManagerCount, exported.HeaderManagerCount);
        Assert.Equal(original.HeaderYear, exported.HeaderYear);
        Assert.Equal(original.Managers.Select(m => m.Name), exported.Managers.Select(m => m.Name));
    }

    private static bool MovedSlotField(string block, int offset, HashSet<int> moved) => block switch
    {
        "far:9657" => moved.Contains(offset / OriginalSaveGame.ClubRecordLength),
        "8257" => offset >= 3 && moved.Contains((offset - 3) / 0x50),
        _ => false,
    };

    /// <summary>(2) Remake → HOK → Remake → HOK ohne Abweichung: Spielstart, nach 3, 22 und 44 Spieltagen (Play-offs), zwei Manager.</summary>
    [GameDataFact]
    public void RemakeStand_SecondRoundTrip_IsIdentical()
    {
        var raw = OriginalExportTests.RawDatabase();
        var database = ClubDatabase.Parse(raw);
        var game = OriginalExportTests.NewGameForExport(database);
        foreach (var days in new[] { 0, 3, 22, 44 })
        {
            while (game.PlayedMatchDays < days && !game.RegularSeasonOver)
                TestSeason.PlayMatchDay(game);
            AssertSecondRoundTrip(game, raw, database, $"nach {days} Spieltagen");
        }
        var two = new GameSetup();
        two.AddManager(new ManagerSetup { Name = "Joerg", Head = 6, ClubIndex = 5 });
        two.AddManager(new ManagerSetup { Name = "Anna", Head = 3, ClubIndex = 20 });
        var pair = NewGame.Create(database, two, Seed);
        for (var d = 0; d < 5; d++)
            TestSeason.PlayMatchDay(pair);
        AssertSecondRoundTrip(pair, raw, database, "zwei Manager");
    }

    /// <summary>
    /// Mehrsaisonprobe (AGENTS.md §5): vier Saisonwechsel, passiver und aktiver Manager (Verträge verlängert); an jedem Saisonstart und
    /// am Ende der Hauptrunde ist die zweite Rundreise ohne Abweichung, und das Spiel läuft weiter.
    /// </summary>
    [GameDataTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultiSeason_SecondRoundTrip_IsIdentical(bool active)
    {
        var raw = OriginalExportTests.RawDatabase();
        var database = ClubDatabase.Parse(raw);
        var game = OriginalExportTests.NewGameForExport(database);
        var manager = game.Managers[0];
        for (var season = GameState.FirstSeason; season < GameState.FirstSeason + 4; season++)
        {
            if (active)
                TestSeason.KeepSquad(game, manager, extendExpired: true);
            TestSeason.PlayRegularSeason(game);
            AssertSecondRoundTrip(game, raw, database, $"Saison {season}, Ende der Hauptrunde");
            var checkpoints = new[] { SeasonCalendar.DaysOf(DayEvents.Playoffs)[6], SeasonCalendar.ClubSeasonEnd + 6 };
            for (var guard = 0; game.Season == season && !game.IsGameOver; guard++)
            {
                Assert.True(guard < 2000, "Saisonwechsel bleibt hängen.");
                if (game.Phase == SeasonPhase.SeasonOver)
                {
                    game.StartNextSeason();
                    continue;
                }
                var day = game.CurrentDay;
                game.Continue();
                if (game.Season == season && checkpoints.Any(c => day < c && c <= game.CurrentDay))
                    AssertSecondRoundTrip(game, raw, database, $"Saison {season}, Tag {game.CurrentDay}");
            }
            Assert.Equal(season + 1, game.Season);
            AssertSecondRoundTrip(game, raw, database, $"Saison {season + 1}, Saisonstart");
        }
    }

    /// <summary>
    /// Was der Import bekannt nicht zurückholt (Feldkarte): Managersatz +0xE6 (erreichte Pokalrunde; frühere DEB-Pokal-Runden kommen
    /// nach dem Import aus dem Nachspielen, Teil 4c [APPROX]) und „eingesetzt“ (+0x0A) im Kader eines Managers mit gesperrtem Spieler
    /// (Sperre: Feldkarte U; die Automatik setzt ihn nach dem Import ein und lässt dafür den letzten Ersatzspieler weg).
    /// </summary>
    private static bool KnownLoss(GameState game, string block, int offset)
    {
        // Ab der zweiten Saison: Titel der Vorsaison (0x387, 0xD0E6, 0x388) und die Startordnung der Tabelle (Platzliste der Vorsaison,
        // LeagueTable.SuccessorOrder) kommen aus History, die der Import leer lässt (Feldkarte GameState.History N).
        if (game.History.Count > 0 && (block is "0387" or "D0E6" or "0388" || (block == "5CCB" && game.PlayedMatchDays == 0)))
            return true;
        if (block == "965B")
            return offset % OriginalSaveGame.ManagerRecordLength == 0xE6;
        if (block != "5DC3" || offset % OriginalSaveGame.SquadSlotLength != 0x0A)
            return false;
        var record = offset / 0x618;
        return record < game.Managers.Count && game.Clubs[game.Managers[record].ClubIndex].Players.Any(p => p.SuspendedGames > 0);
    }

    private void AssertSecondRoundTrip(GameState game, byte[] raw, ClubDatabase database, string label)
    {
        var first = OriginalSaveGame.Read(OriginalExport.Export(game, raw, Seed));
        Assert.True(first.ChecksumsValid);
        var again = GameState.ImportOriginal(first, database, Seed);
        var second = OriginalSaveGame.Read(OriginalExport.Export(again, raw, Seed));
        var diff = new List<string>();
        foreach (var (block, _, data) in first.Blocks)
        {
            var other = second.Block(block.Name);
            for (var i = 0; i < data.Length; i++)
                if (data[i] != other[i] && !KnownLoss(game, block.Name, i))
                    diff.Add($"{block.Name}+0x{i:X}: 0x{data[i]:X2}/0x{other[i]:X2}");
        }
        output.WriteLine($"{label}: {game.DateText}, {diff.Count} Abweichungen");
        Assert.True(diff.Count == 0, $"{label}: {string.Join("; ", diff.Take(40))}");
        Assert.Equal(game.Managers.Select(m => (m.Name, m.Head, m.ClubIndex, m.Money)),
            again.Managers.Select(m => (m.Name, m.Head, m.ClubIndex, m.Money)));
    }
}
