using System.Buffers.Binary;
using EishockeyManager.Formats;

namespace EishockeyManager.Core;

/// <summary>
/// Export eines Remake-Spielstands als Original-Spielstand <c>*.HOK</c> (Spur hok-export, Runde 15, R14-D2 = B). Gegenstück zum
/// Import (<see cref="OriginalImport"/>) nach der Feldkarte in <c>docs/formate/spielstand-original.md</c> (Abschnitt „Feldkarte
/// Remake-Spielstand ↔ HOK“, Grundregeln 1–6 rückwärts):
/// <list type="number">
/// <item>Vereine: Satzplatz = <see cref="Slots"/> (Ligen in Satzfolge, deutsche Vereine ohne Liga, Länder in WM-Reihenfolge,
///   übrige nach Vereinsindex); alle Vereinsverweise laufen über diese Abbildung.</item>
/// <item>Spieler: Managerkader, Transfermarkt, Nationalkader und Pool einzeln (Kaderplätze 0x5DC3, Pool far:8253); die Kader der
///   Computervereine stehen im Original nicht im Spielstand, nur ihre Teamwerte far:9657 + 0x17 … 0x1D.</item>
/// <item>Geld: DM · 3 = Drittel-DM; Zeit: Zähler 0x3B7 = <see cref="GameState.Counter"/>, Saisonzähler 0x3BD = Saison − 1993.</item>
/// <item>[APPROX] Was die Feldkarte als N/U führt oder an keinem Stand gedeutet ist, schreibt der Export als Vorgabe des
///   Spielstarts (Werte, die an allen Ständen FILE01–FILE49A gleich sind) oder als 0; Einzelheiten je Feld im Code.</item>
/// </list>
/// Der Export verändert den Spielstand nicht (reine Abfrage, keine Zufallszüge aus <see cref="GameState.Random"/>).
/// </summary>
public static partial class OriginalExport
{
    /// <summary>Plätze im Vereinsblock far:9657 (150 Sätze zu 0x3D Byte).</summary>
    public const int ClubSlots = ClubDatabase.TotalClubs;

    /// <summary>Ligaplätze (drei Ligen zu zwölf Sätzen) und deutsche Vereine ohne Liga (Sätze 36–47): Vereine mit Kader unter 48.</summary>
    public const int GermanSlots = 48;

    /// <summary>Exportiert ein Spiel als Dateiinhalt eines <c>*.HOK</c>.</summary>
    /// <param name="game">Spielstand (bleibt unverändert).</param>
    /// <param name="clubDatabase">Dateiinhalt der Vereinsdatenbank <c>kb\23</c> (Namen wie im Original, Farben und Logo der
    ///   Vereinssätze); null → Namen aus dem Spielstand, Farbe und Logo 0 <c>[APPROX]</c>.</param>
    /// <remarks>Spielstand-ID, Füllbyte und Startschlüssel folgen aus dem Inhalt (<see cref="OriginalSaveWriter.DeriveFromContent"/>):
    ///   gleicher Spielstand → gleiche Datei, verschiedene Stände → verschiedene IDs.</remarks>
    public static byte[] Export(GameState game, byte[]? clubDatabase) =>
        Build(game, clubDatabase).ToArray();

    /// <summary>Wie <see cref="Export"/>, aber der Schreiber mit allen Blöcken im Klartext (Tests, Rundreise).</summary>
    public static OriginalSaveWriter Build(GameState game, byte[]? clubDatabase)
    {
        ArgumentNullException.ThrowIfNull(game);
        if (game.Managers.Count is < 1 or > GameSetup.MaxManagers)
            throw new InvalidOperationException($"Zahl der Manager {game.Managers.Count} lässt sich nicht exportieren.");
        var writer = new OriginalSaveWriter();
        game.WriteOriginal(writer, clubDatabase);
        writer.DeriveFromContent();
        return writer;
    }

    /// <summary>
    /// Vereinsname wie in der Datenbank (Rohbytes, die das Original beim Spielstart in den Vereinssatz kopiert; Umlaute teils als
    /// <c>[ \ ]</c>, teils CP437 wie „KÖLNER EC“ = 0x99): Vereine mit Kader ab 0x04AE + 523 · Index, übrige ab 0x876E + 23 · (Index − 64).
    /// </summary>
    public static byte[] DatabaseClubName(byte[] clubDatabase, int index)
    {
        var at = index < ClubDatabase.ClubsWithSquad
            ? ClubDatabase.SquadOffset + index * ClubDatabase.SquadRecordLength
            : ClubDatabase.EuropeanNamesOffset + (index - ClubDatabase.ClubsWithSquad) * ClubDatabase.NameLength;
        var raw = clubDatabase.AsSpan(at, ClubDatabase.NameLength);
        var end = raw.IndexOf((byte)0);
        var name = new byte[OriginalImport.ClubNameLength];
        raw[..Math.Min(end < 0 ? raw.Length : end, OriginalImport.ClubNameLength - 1)].CopyTo(name);
        return name;
    }

    /// <summary>
    /// Spielernamen der Datenbank (Kader der Vereine 0–63) als Text → Rohbytes ohne Nullbyte. Das Original kopiert sie beim Spielstart
    /// unverändert in den Pool (Ö meist 0x99, aber BRANNSTRÖM mit 0x5C; FILE01). Namen, deren Text mit verschiedenen Bytes vorkommt,
    /// fehlen (dann gilt <see cref="OriginalSaveWriter.Encode"/>).
    /// </summary>
    public static Dictionary<string, byte[]> DatabasePlayerNames(byte[] clubDatabase)
    {
        var names = new Dictionary<string, byte[]>();
        var ambiguous = new HashSet<string>();
        for (var club = 0; club < ClubDatabase.ClubsWithSquad; club++)
            for (var p = 0; p < ClubDatabase.SquadSize; p++)
            {
                var at = ClubDatabase.SquadOffset + club * ClubDatabase.SquadRecordLength + ClubDatabase.NameLength
                         + p * ClubDatabase.PlayerRecordLength;
                var field = clubDatabase.AsSpan(at, ClubDatabase.PlayerNameLength);
                var end = field.IndexOf((byte)0);
                var raw = (end < 0 ? field : field[..end]).ToArray();
                var text = GameText.Decode(raw);
                if (text.Length == 0 || ambiguous.Contains(text))
                    continue;
                if (names.TryGetValue(text, out var known) && !known.AsSpan().SequenceEqual(raw))
                {
                    names.Remove(text);
                    ambiguous.Add(text);
                }
                else
                    names[text] = raw;
            }
        return names;
    }

    /// <summary>
    /// Satzplatz je Vereinsindex (Umkehrung von Regel 1 und 2 des Imports).
    /// <list type="bullet">
    /// <item>0–35: die drei Ligen in Satzfolge (<see cref="LeagueOrder"/>).</item>
    /// <item>[APPROX] 36–47: deutsche Vereine mit Index unter 48 ohne Liga, nach Index (FILE01: Datenbankfolge).</item>
    /// <item>48–51: Leerplätze, 52–63: Länder in der Reihenfolge der WM (<see cref="WorldCup.Order"/>; die Tausche Datei 0x35EDD
    ///   vertauschen die Sätze), 64–149: Europapokal-Gegner nach Index (FILE01: Datenbankfolge).</item>
    /// </list>
    /// </summary>
    public static int[] Slots(GameState game)
    {
        var slotOf = Enumerable.Repeat(-1, game.Clubs.Count).ToArray();
        var next = 0;
        void Put(int club)
        {
            if (slotOf[club] < 0)
                slotOf[club] = next++;
        }
        foreach (var league in Enum.GetValues<League>())
        {
            var order = LeagueOrder(game, league);
            if (order.Count != GameState.ClubsPerLeague)
                throw new InvalidOperationException($"{league}: {order.Count} Vereine – das Original führt je Liga genau 12.");
            foreach (var club in order)
                Put(club);
        }
        foreach (var club in game.Clubs.Where(c => c.Index < GermanSlots && slotOf[c.Index] < 0).Select(c => c.Index))
            Put(club);
        foreach (var club in Enumerable.Range(GermanSlots, NationalTeams.First - GermanSlots))
            Put(club);
        var nations = game.WorldCup?.Order is { Count: WorldCup.Teams } order2 ? order2
            : Enumerable.Range(NationalTeams.First, WorldCup.Teams).ToList();
        foreach (var club in nations)
            Put(club);
        for (var club = 0; club < game.Clubs.Count; club++)
            Put(club);
        if (next != ClubSlots)
            throw new InvalidOperationException($"{next} Vereinssätze statt {ClubSlots}.");
        return slotOf;
    }

    /// <summary>
    /// Satzfolge einer Liga für den Export. Das Original rechnet den Spielplan aus der festen Tabelle 0x4E7C7 über die Satzfolge
    /// (re-nachtrag.md §4); das Remake bildet ihn je Saison aus <see cref="Schedule.Rounds"/> über die Platznummern 0–11 (nach einem
    /// Import und in neuen Spielen ab Runde 17, A113: Satzfolge; ältere Spielstände: Rang des Vereinsindex). Damit Spielplan,
    /// Ergebnisse 0xA0CB und Tabelle im Original zusammen passen, gilt die Folge, deren Plan dem des Remakes gleicht: zuerst <see cref="GameState.LeagueSlots"/>, sonst die Indexfolge.
    /// [APPROX] Passt keine (nur synthetische Spielstände), gilt <see cref="GameState.LeagueSlots"/>.
    /// </summary>
    public static IReadOnlyList<int> LeagueOrder(GameState game, League league)
    {
        var stored = game.LeagueSlots(league);
        var plan = game.Schedules.GetValueOrDefault(league);
        if (plan is null || plan.Count == 0)
            return stored;
        foreach (var candidate in new[] { stored, stored.Order().ToList() })
            if (PlanMatches(candidate, plan))
                return candidate;
        return stored;
    }

    private static bool PlanMatches(IReadOnlyList<int> order, IReadOnlyList<MatchDay> plan)
    {
        if (order.Count != GameState.ClubsPerLeague)
            return false;
        var rounds = Schedule.Rounds(Enumerable.Range(0, GameState.ClubsPerLeague).ToList(), rounds: plan.Count / 11);
        if (rounds.Count != plan.Count)
            return false;
        for (var d = 0; d < plan.Count; d++)
        {
            var expected = rounds[d].Matches.Select(p => (order[p.Home], order[p.Away])).ToList();
            if (!expected.SequenceEqual(plan[d].Matches))
                return false;
        }
        return true;
    }

    internal static void Write16(byte[] block, int offset, int value) =>
        BinaryPrimitives.WriteInt16LittleEndian(block.AsSpan(offset), (short)value);

    internal static void Write32(byte[] block, int offset, long value) =>
        BinaryPrimitives.WriteInt32LittleEndian(block.AsSpan(offset), (int)Math.Clamp(value, int.MinValue, int.MaxValue));

    /// <summary>DM → Drittel-DM (Regel 4 rückwärts), auf 32 Bit begrenzt.</summary>
    internal static long Thirds(long marks) => Math.Clamp(marks * 3, int.MinValue, int.MaxValue);

    internal static byte Byte(int value) => (byte)Math.Clamp(value, 0, 255);
}

public sealed partial class GameState
{
    /// <summary>Alle Blöcke eines HOK aus dem Spielstand (siehe <see cref="OriginalExport"/>).</summary>
    internal void WriteOriginal(OriginalSaveWriter w, byte[]? clubDatabase)
    {
        // Vor dem ersten Spiel sind die Teamwerte noch nicht gezogen (MatchEngine.LoadTeamValues [APPROX]: beim ersten Spiel); das
        // Original hat sie schon beim Spielstart (FILE01). Der Export zieht sie wie das erste Spiel und stellt danach Teamwerte und
        // Zufallszustand wieder her, damit der Spielstand unverändert bleibt (Review 2026-11-10 B1, Export_IsPure).
        if (Clubs.Any(c => c.TeamValues is null))
        {
            var random = Random.Capture();
            var before = Clubs.Select(c => c.TeamValues).ToArray();
            try
            {
                MatchEngine.EnsureTeamValues(this);
                WriteOriginalBlocks(w, clubDatabase);
            }
            finally
            {
                Random.Restore(random);
                for (var i = 0; i < Clubs.Count; i++)
                    Clubs[i].TeamValues = before[i];
            }
            return;
        }
        WriteOriginalBlocks(w, clubDatabase);
    }

    private void WriteOriginalBlocks(OriginalSaveWriter w, byte[]? clubDatabase)
    {
        var database = clubDatabase is null ? null : ClubDatabase.Parse(clubDatabase);
        var slotOf = OriginalExport.Slots(this);
        WriteHeaderAndSetup(w);
        WriteClubRecords(w, slotOf, database, clubDatabase);
        WriteLeague(w, slotOf);
        WriteManagers(w, slotOf);
        WriteSquadsAndPool(w, slotOf, clubDatabase);
        WriteOriginalRest(w, slotOf);
        WriteCupsAndPostseason(w, slotOf);
        WriteNational(w, slotOf);
        WriteMessages(w);
    }

    // ---- Kopf, Setup, Datum, Optionen ------------------------------------------------------------------------------

    private void WriteHeaderAndSetup(OriginalSaveWriter w)
    {
        var date = SeasonCalendar.LayoutDate(CurrentDay);
        var year = SeasonCalendar.Year(CurrentDay, Season);
        var level = Managers[0].Level;
        w.HeaderYear = year;
        foreach (var m in Managers)
            w.Managers.Add(new OriginalSaveManagerHeader((int)Finance(m).CostLeague, WorldOnly, m.Name,
                WorldOnly && m == Managers[0] ? Clubs[NationalTeams.Germany].Name : Clubs[m.ClubIndex].Name));

        // [ORIGINAL] Regler DGROUP 0x118–0x11B (Level, Dauer, Start-Liga, Start-Stärke), Spielende 0x5A14, 0xED8 = 5 − Level
        // (em.exe Datei 0x2988D). Ein Wert für alle Manager (Schema 42); [APPROX] Level/Stärke von Manager 0.
        var setup = w.Block("0118");
        setup[0] = (byte)level;
        setup[1] = (byte)Setup.Duration;
        setup[2] = (byte)Setup.StartLeague;
        setup[3] = (byte)(Setup.Managers.Count > 0 ? (int)Setup.Managers[0].StartStrength : (int)StartStrength.Mittelfeld);
        w.Block("0ED8")[0] = (byte)(5 - level);
        // [ORIGINAL] Spielende: Dauer 0 → 1994 (FILE09, FILE29), Dauer 3 „∞“ → 22222 (FILE01); [APPROX] 1/2 → 1996/1998 (FILE28: 1996).
        OriginalExport.Write16(w.Block("5A14"), 0, Setup.Duration switch
        {
            GameDuration.EineSaison => FirstSeason + 1,
            GameDuration.DreiSaisons => FirstSeason + 3,
            GameDuration.FuenfSaisons => FirstSeason + 5,
            _ => 22222,
        });
        // [ORIGINAL] Spielmodus DGROUP 0xD0B9 (1 nur Weltkugel, 2 nur Fahne, 4 beide).
        w.Block("D0B9")[0] = (byte)Mode;

        // [ORIGINAL] Datum DGROUP 0x5A20/0x5A1C/0x5A18 (Monat 0-basiert), Tageszähler 0x3B7 und Tagesindex 0xA969 (gleich; im Menü des
        // Markierungstags gesichert eins kleiner, LoadCounterOffset), Saisonzähler 0x3BD, Saisonjahr 0x3BB (FILE01 1993, FILE14 1994).
        OriginalExport.Write32(w.Block("5A20"), 0, date.Day);
        OriginalExport.Write32(w.Block("5A1C"), 0, date.Month - 1);
        OriginalExport.Write32(w.Block("5A18"), 0, year);
        var counter = Counter(CurrentDay);
        OriginalExport.Write32(w.Block("03B7"), 0, counter);
        OriginalExport.Write16(w.Block("A969"), 0, counter - LoadCounterOffset);
        OriginalExport.Write16(w.Block("03BD"), 0, Season - FirstSeason);
        OriginalExport.Write16(w.Block("03BB"), 0, Season);
        // [APPROX] DGROUP 0xA8A0: an den Ständen meist der Monat (0-basiert, FILE16A 1, FILE36 10, FILE44 3), Ende Januar 8 (FILE13).
        w.Block("A8A0")[0] = (byte)(date.Month - 1);

        // [ORIGINAL] Optionen DGROUP 0x21D–0x229 (Berichte 0–8, Livemodus 9, Pokalanzeigen 10–12) und Tempo 0x262.
        var switches = w.Block("021D");
        for (var i = 0; i < Options.Reports.Length; i++)
            switches[i] = (byte)(Options.Reports[i] ? 1 : 0);
        switches[9] = (byte)Options.LiveMode;
        switches[10] = (byte)(Options.ShowDebCup ? 1 : 0);
        switches[11] = (byte)(Options.ShowEuropeanCup ? 1 : 0);
        switches[12] = (byte)(Options.ShowWorldCup ? 1 : 0);
        switches[13] = 1; // [APPROX] DGROUP 0x22A: an allen Ständen 1, Bedeutung offen
        OriginalExport.Write16(w.Block("0262"), 0, Options.MatchSpeed);

        // [APPROX] Blöcke ohne Deutung, an allen Ständen FILE01–FILE49A gleich: Vorgabe des Spielstarts.
        w.Block("0120").AsSpan().Fill(1);
        w.Block("017C")[0] = 1;
        w.Block("D0E4").AsSpan().Fill(1);
        new byte[] { 7, 6, 5, 4, 3, 2 }.CopyTo(w.Block("022D"), 0);
        new byte[] { 0xB0, 0xFF, 0xCE, 0xFF, 0x9C, 0xFF, 0xC4, 0xFF, 0x50, 0x00, 0x32, 0x00, 0x46, 0x00, 0x3C, 0x00 }.CopyTo(w.Block("0246"), 0);
        // [ORIGINAL] Highscore-Schlüssel DGROUP 0x256 (Datei 0x28A2C ff.; 0 = keine Bestenliste).
        OriginalExport.Write16(w.Block("0256"), 0, HighscoreRules.Key(Setup) ?? 0);
        // [ORIGINAL] Konjunkturfaktor DGROUP 0x2C0 (an allen Ständen 110); Preisindex 0x2BC unten (Training).
        OriginalExport.Write32(w.Block("02C0"), 0, 110);
        // [ORIGINAL] DGROUP 0x178 + m: Managerkopf (WriteManagers), unbenutzte Plätze 0xFF (FILE01 06 FF FF FF, FILE09 03 0A FF FF).
        w.Block("0178").AsSpan().Fill(0xFF);

        // [ORIGINAL] Zahl der Manager DGROUP 0x386, Manager am Zug 0xA70E (5 = Bundestrainer), 0xA70D letzter Zugteilnehmer
        // [APPROX] (Stände: Managerzahl − 1, mit Bundestrainer 5).
        w.Block("0386")[0] = (byte)Managers.Count;
        w.Block("A70E")[0] = (byte)(_nationalTurn ? OriginalImport.NationalRecord : CurrentManager);
        w.Block("A70D")[0] = (byte)(NationalCoach is not null ? OriginalImport.NationalRecord : Managers.Count - 1);
    }
}
