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
/// [APPROX] Was das HOK nicht oder nicht belegt führt (Feldkarte Status N/U) und was der Import noch nicht übernimmt
/// (Teil 4a übernimmt Börse, Werbung, Training, Taktik, Serien, Darlehen in <c>OriginalImport.Rest.cs</c>, Teil 4b Pool und
/// Markt, Nationalkader und Nachrichten in <c>OriginalImport.Market.cs</c>, <c>.National.cs</c>, <c>.Messages.cs</c>, Teil 4c
/// Pokale und WM in <c>OriginalImport.Cups.cs</c>), ergibt sich aus dem Nachspielen: es ist
/// ein gültiger Remake-Zustand am selben Tag, aber nicht der des Originals. Zufall aus neuer Saat (Regel 6).
/// </para>
/// </summary>
public static partial class OriginalImport
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

    /// <summary>
    /// Im Menü des Markierungstags gesichert (Tagesindex DGROUP 0xA969 = Zähler 0x3B7 − 1, sonst gleich)? [ORIGINAL] Der Tageswechsel
    /// 0x55204 läuft dort vor dem Menü 0x55350, der Index zählt erst danach (Datei 0x553A2); FILE22/FILE44 (14.04.1994): 217/216.
    /// Nach dem Laden läuft dieser Tageswechsel erneut (GameState.LoadQuirk.cs). Unterstützt ist der 13. April (Markierung 255 am
    /// Ende der Vereinssaison); einen anderen Stand dieser Art lehnt der Import ab.
    /// </summary>
    public static bool SavedOnMarkerDay(OriginalSaveGame save)
    {
        var counter = Day(save) + 1;
        var index = BinaryPrimitives.ReadInt16LittleEndian(save.Block("A969"));
        if (index != counter - 1)
            return false;
        if (Day(save) != SeasonCalendar.ClubSeasonEnd + 1)
            throw new InvalidDataException($"Tagesindex {index} (DGROUP 0xA969) vor Zähler {counter} außerhalb des 13. April.");
        return true;
    }

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
    /// <summary>
    /// Baut aus einem Original-Spielstand einen gültigen Spielstand des Remakes (siehe <see cref="OriginalImport"/>).
    /// </summary>
    /// <param name="seed">Saat für alle Zufallsquellen (Regel 6: das HOK führt keinen Generatorzustand).</param>
    public static GameState ImportOriginal(OriginalSaveGame save, ClubDatabase database, int seed)
    {
        ArgumentNullException.ThrowIfNull(save);
        ArgumentNullException.ThrowIfNull(database);
        var clubOf = OriginalImport.ClubIndices(save, database);
        var managerCount = save.Block("0386")[0];
        if (managerCount is < 1 or > GameSetup.MaxManagers)
            throw new InvalidDataException($"Zahl der Manager {managerCount} (DGROUP 0x386) ungültig.");

        var game = CreateImported(save, database, clubOf, managerCount, seed);
        game.ReplayTo(save);
        game.ApplyOriginal(save, clubOf, database);
        if (OriginalImport.SavedOnMarkerDay(save))
            game.ReopenMarkerDayAfterLoad(); // Entscheidung Nutzer 09.11.2026 (C): Ladeeigenheit wie im Original
        return game;
    }

    // ---- (1) Spielstart ----------------------------------------------------------------------------------------

    private static GameState CreateImported(OriginalSaveGame save, ClubDatabase database, int[] clubOf, int managerCount, int seed)
    {
        var random = new GameRandom(seed);
        var setupBlock = save.Block("0118");
        var mode = OriginalImport.Mode(save);
        var setup = new GameSetup
        {
            // [ORIGINAL] Regler DGROUP 0x118–0x11B (Level, Dauer, Start-Liga, Start-Stärke), Feldkarte „GameSetup“.
            Duration = (GameDuration)Math.Clamp((int)setupBlock[1], 0, 3),
            DebCup = true, // [APPROX] kein Schalter des Originals (Feldkarte N)
            EuropeAndWorldCup = mode is GameMode.WorldOnly or GameMode.Full,
        };
        if (mode == GameMode.WorldOnly)
            setup.ToggleClubCompetitions();
        // Start-Liga DGROUP 0x11A, ein Wert für alle Manager (Schema 42, kader-start #605): auch ohne Manager gesetzt.
        setup.StartLeague = (League)Math.Clamp((int)setupBlock[2], 0, 2);

        var managerRecords = save.Block("965B");
        var managers = new List<Manager>();
        for (var m = 0; m < managerCount; m++)
        {
            var record = managerRecords.AsSpan(m * OriginalSaveGame.ManagerRecordLength, OriginalSaveGame.ManagerRecordLength);
            var name = CName(record[..0x13]);
            var club = clubOf[record[0x14]];
            // [ORIGINAL] Managerkopf DGROUP 0x178 + m (Block 0178, unbenutzte Plätze 0xFF): das Hauptmenü zeigt diesen Kopf (Bilder
            // file01_2, file05_1, file09_2/3, file11_01, file15_1, file27_2, file28_2, file47_1, file49_1 gegen kb\0; EXPORT01 mit
            // 0178 = 0 und +0x13 = 6 zeigt Kopf 0, export01_1). Managersatz +0x13 ist an allen Ständen eine Kopie (Review 2026-11-10 B3).
            var heads = save.Block("0178");
            var head = m < heads.Length && heads[m] < ManagerSetup.HeadCount ? heads[m] : 0;
            setup.Managers.Add(new ManagerSetup
            {
                Name = name,
                Head = head,
                ClubIndex = club,
                Level = Math.Clamp((int)setupBlock[0], 0, ManagerSetup.LevelCount - 1),
                StartLeague = (League)Math.Clamp((int)setupBlock[2], 0, 2),
                StartStrength = (StartStrength)Math.Clamp((int)setupBlock[3], 0, 2),
            });
            managers.Add(new Manager
            {
                Name = name, Head = head, ClubIndex = club, Level = setup.Managers[m].Level,
                Money = FinanceRules.StartMoney(setup.Managers[m].Level),
            });
        }
        ApplyOptions(save, setup.Options);

        // Regel 2: Satzplätze 0–11, 12–23, 24–35 sind die drei Ligen; andere Vereine spielen in keiner Liga.
        var leagueOf = new Dictionary<int, League>();
        for (var slot = 0; slot < 3 * ClubsPerLeague; slot++)
            leagueOf[clubOf[slot]] = (League)(slot / ClubsPerLeague);
        var clubs = database.Clubs.Select(c =>
        {
            var club = new Club
            {
                Index = c.Index,
                Name = c.Name,
                // [APPROX] Datenbankstärke kb\23. Nach einem Platztausch beim Spielstart (U15-a) führt das Remake die Stärke des
                // Tauschpartners; das HOK zeigt den Tausch nicht (die Teamwerte bleiben am Satz und werden übernommen).
                Strength = c.Strength,
                LogoGraphic = c.LogoGraphic,
                League = leagueOf.TryGetValue(c.Index, out var league) ? league : null,
            };
            club.Players.AddRange(c.Players.Select(p => new Player { Name = p.Name, Role = p.Role, IsForeigner = p.IsForeigner }));
            return club;
        }).ToList();

        // Spielplan: feste Tabelle 0x4E7C7, Buchstabe = Platz in der Satzfolge der Liga (re-nachtrag.md §4). Schedule.Rounds
        // ordnet nach Vereinsindex; über die Platznummern 0–11 gerechnet und auf die Vereine der Satzfolge abgebildet ergibt sich
        // genau der Plan des Originals.
        var schedules = new Dictionary<League, IReadOnlyList<MatchDay>>();
        foreach (var league in Enum.GetValues<League>())
        {
            var slots = Enumerable.Range((int)league * ClubsPerLeague, ClubsPerLeague).Select(s => clubOf[s]).ToArray();
            schedules[league] = Schedule.Rounds(Enumerable.Range(0, ClubsPerLeague).ToList(), rounds: 4)
                .Select(d => new MatchDay(d.Number, d.Matches.Select(p => (slots[p.Home], slots[p.Away])).ToList()))
                .ToList();
        }

        var game = new GameState
        {
            Setup = setup,
            Clubs = clubs,
            Managers = managers,
            Schedules = schedules,
            Random = random,
        };
        game.Season = OriginalImport.Season(save);
        SquadSetup.Initialize(game, seed, forImport: true);
        game.AssignStartPlayerIds();
        game.InitializeManagerSystems();
        game.InitializeLeagueSlots(clubOf.Take(4 * ClubsPerLeague).ToList());
        game.BeginFirstTurn();
        return game;
    }

    /// <summary>Optionen DGROUP 0x21D–0x229 und 0x262 (Feldkarte „GameOptions“).</summary>
    private static void ApplyOptions(OriginalSaveGame save, GameOptions options)
    {
        var switches = save.Block("021D");
        for (var i = 0; i < options.Reports.Length; i++)
            options.Reports[i] = switches[i] != 0;
        options.LiveMode = Math.Clamp((int)switches[9], 0, GameOptions.LiveModeCount - 1);
        options.ShowDebCup = switches[10] != 0;
        options.ShowEuropeanCup = switches[11] != 0;
        options.ShowWorldCup = switches[12] != 0;
        options.SetMatchSpeed(BinaryPrimitives.ReadInt16LittleEndian(save.Block("0262")));
    }

    // ---- (2) Nachspielen bis zum Tag des HOK -------------------------------------------------------------------

    /// <summary>
    /// Zeitleiste von Tag 0 bis vor den Tag des HOK, dazu dessen Tageswechsel (Zähler 0x3B7 = Tag + 1): Das HOK ist im Hauptmenü
    /// des Tages gesichert, nach dem Tagesabschluss, dem Takt 0x07169 und den Würfen des Listenwechsels, vor den Spielen (#688,
    /// R688-04: Datei 0x55204–0x552D3 und 0x54E71 liegen vor dem Menü 0x44E22). Ligaspieltage tragen danach die Ergebnisse des Originals, damit Tabelle,
    /// Play-off-Setzliste und Aufstiegsrunden dem HOK folgen. [APPROX] Pokale, Markt, Training und Finanzen laufen nach den
    /// Regeln des Remakes; was belegt ist, überschreibt <see cref="ApplyOriginal"/>.
    /// </summary>
    private void ReplayTo(OriginalSaveGame save)
    {
        var target = OriginalImport.Day(save);
        if (target is < 0 or >= SeasonCalendar.Length)
            throw new InvalidDataException($"Tageszähler {target + 1} (DGROUP 0x3B7) außerhalb der Saison.");
        var steps = new List<SeasonStep>();
        if (target > 0 && SkipToWorldCup() && CurrentDay > target)
            throw new InvalidDataException($"„Nur Weltkugel“ vor der WM (Tag {target}) wird nicht unterstützt.");
        while (CurrentDay < target)
        {
            var played = PlayedMatchDays;
            ProcessDay(CurrentDay, steps);
            if (PlayedMatchDays != played)
                SubstituteLeagueResults(save, PlayedMatchDays);
            if (Phase is SeasonPhase.SeasonOver or SeasonPhase.GameOver)
                throw new InvalidDataException($"Saison endet beim Nachspielen vor Tag {target}.");
            CurrentDay++;
        }

        foreach (var league in Enum.GetValues<League>())
            if (Schedules[league].Count > 0 && OriginalImport.NextMatchDay(save, league) - 1 != Math.Min(PlayedMatchDays, Schedules[league].Count)
                && !WorldOnly && !RegularSeasonOver)
                throw new InvalidDataException(
                    $"Spieltag {OriginalImport.NextMatchDay(save, league)} (DGROUP 0x7A9) passt nicht zu Tag {target} ({PlayedMatchDays} gespielt).");
        OpenDay(CurrentDay); // #688 (R688-04): Tageswechsel zum gesicherten Tag, vor dem Überschreiben mit den Werten des HOK
        StartTurns();
    }

    /// <summary>
    /// Ersetzt die berechneten Ergebnisse eines Ligaspieltags durch die des HOK (Paarung k = Paarung k des Plans). Steht für den
    /// Spieltag in allen drei Ligen nur 0:0, hat das Original ihn nicht eingetragen (Stand mit vorgestelltem Tageszähler, FILE16A:
    /// die Tabellensätze 0x825A zählen dort 2 Spiele bei 43 Spieltagen); dann entfallen seine Ergebnisse, damit die Tabelle
    /// der des Originals gleicht. [APPROX] 18 Spiele 0:0 an einem Spieltag kommen im Spiel nicht vor.
    /// </summary>
    private void SubstituteLeagueResults(OriginalSaveGame save, int matchDay)
    {
        var block = save.Block("A0CB");
        if (Enum.GetValues<League>().All(l => block.AsSpan(OriginalImport.ResultsPerLeague * (int)l + 12 * (matchDay - 1), 12)
                .IndexOfAnyExcept((byte)0) < 0))
        {
            Results.RemoveAll(r => r.MatchDay == matchDay);
            return;
        }
        foreach (var (league, days) in Schedules)
        {
            if (matchDay > days.Count)
                continue;
            var matches = days[matchDay - 1].Matches;
            for (var k = 0; k < matches.Count; k++)
            {
                var (home, away) = matches[k];
                var index = Results.FindIndex(r => r.League == league && r.MatchDay == matchDay && r.Home == home && r.Away == away);
                if (index < 0)
                    throw new InvalidOperationException($"Ergebnis {league} Spieltag {matchDay} Paarung {k} fehlt.");
                var (hg, ag) = OriginalImport.LeagueResult(save, league, matchDay, k);
                Results[index] = Results[index] with { HomeGoals = hg, AwayGoals = ag };
            }
        }
    }

    // ---- (3) Belegte Felder übernehmen -------------------------------------------------------------------------

    private void ApplyOriginal(OriginalSaveGame save, int[] clubOf, ClubDatabase database)
    {
        var foreigners = database.Clubs.SelectMany(c => c.Players).Where(p => p.IsForeigner).Select(p => p.Name).ToHashSet();
        ApplyTeamValues(save, clubOf);
        var records = save.Block("965B");
        var squadPlayers = new Dictionary<int, Player>(); // Poolindex → Spieler eines übernommenen Managerkaders
        for (var m = 0; m < Managers.Count; m++)
        {
            var record = records.AsSpan(m * OriginalSaveGame.ManagerRecordLength, OriginalSaveGame.ManagerRecordLength);
            ApplyManager(save, m, record, clubOf);
            // #732 (abgleich-rest Teil 3, R16-D2 = A): [ORIGINAL] auch im Modus „nur Weltkugel“ – Satz 0 führt dort seinen Kader mit
            // Gehalt und Preis (FILE20/FILE21 `emtool hok dump`: 25 Plätze, Werte über den Saisonwechsel unverändert).
            ApplySquad(save, Clubs[Managers[m].ClubIndex], m, clubOf, foreigners, squadPlayers);
        }
        ApplyOriginalRest(save, clubOf); // Teil 4a (OriginalImport.Rest.cs)
        var pool = ApplyPoolAndMarket(save, clubOf, squadPlayers, foreigners); // Teil 4b (OriginalImport.Market.cs)
        ApplyNationalSquad(save, clubOf, pool, squadPlayers); // Teil 4b (OriginalImport.National.cs)
        ApplyMessages(save, pool); // Teil 4b (OriginalImport.Messages.cs)
        ApplyCups(save, clubOf); // Teil 4c (OriginalImport.Cups.cs)
        ApplyPostseason(save, clubOf); // Teil 4d (OriginalImport.Playoffs.cs, R14HI-01)
        // [ORIGINAL] Manager am Zug DGROUP 0xA70E (0–3; 5 = Bundestrainer). [APPROX] Der Zug des Bundestrainers beginnt im Remake
        // mit dem Tageslauf; der Import setzt dann Manager 0.
        var current = save.Block("A70E")[0];
        if (current < Managers.Count && current != CurrentManager && !Managers[current].Dismissed)
        {
            CurrentManager = current;
            BeginManagerTurn();
        }
    }

    /// <summary>
    /// Teamwerte far:9657 + 0x17 … 0x1D aller Vereinssätze (Feldkarte „ClubTeamValues“). Kondition und Technik 0x18–0x1B stehen im
    /// Original absolut, das Remake führt die Abweichung vom Datenbankwert (<see cref="MatchEngine.ClubFieldValues"/>): Import
    /// Originalwert − <see cref="MatchEngine.ComputerTeamValue"/> (#729; Gegenstück zum Export, Review 2026-11-10 B1). Dazu Einsatz und
    /// Angriff des letzten Spiels (+0x26, +0x2C Bits 4–6; #804).
    /// </summary>
    private void ApplyTeamValues(OriginalSaveGame save, int[] clubOf)
    {
        var records = save.Block("far:9657");
        for (var slot = 0; slot < clubOf.Length; slot++)
        {
            var r = records.AsSpan(slot * OriginalSaveGame.ClubRecordLength, OriginalSaveGame.ClubRecordLength);
            var club = Clubs[clubOf[slot]];
            if (WorldOnly && Managers.Any(m => m.ClubIndex == club.Index))
            {
                // Satz des Managervereins trägt im Modus „nur Weltkugel“ das Land (Regel 1): keine Vereinswerte, auch nicht die
                // des Nachspielens (#747; sonst hinge der Wert vom Tag des Stands ab: FILE18 leer, FILE48 gezogen).
                club.TeamValues = null;
                continue;
            }
            var value = MatchEngine.ComputerTeamValue(club);
            club.TeamValues = new ClubTeamValues(r[0x18] - value, r[0x19] - value, r[0x1A] - value, r[0x1B] - value, r[0x1C], r[0x1D], r[0x17]);
            // #804: [ORIGINAL] Einsatz 0x26 und Angriff 0x2C Bits 4–6 des letzten Spiels (Tactics.MatchEffort/MatchAttack; Schreiber
            // 0x8365, 0x939A, 0x38D96) stehen im Vereinssatz zwischen den Spielen; ohne sie hinge die nächste Spielvorbereitung vom
            // Nachspielen ab (FILE03/FILE16A/FILE44/FILE53). Die Sätze der Nationalteams führen dort die Anzeige „AUTOM.“ (National.cs).
            if (!TransferMarket.IsNationalTeam(club))
                club.Tactics.RecordMatchFields(r[0x26], (r[0x2C] >> 4) & 7);
        }
    }

    /// <summary>Konto, Eintritt, Stadion, Bankdarlehen, Liga-Stufe, Werbebewertung, Talentpreis, Zinsdrittel, Jugend (Feldkarte
    /// „Manager, ManagerFinance“, „Stadion …“).</summary>
    private void ApplyManager(OriginalSaveGame save, int m, ReadOnlySpan<byte> record, int[] clubOf)
    {
        var manager = Managers[m];
        manager.Money = BinaryPrimitives.ReadInt32LittleEndian(record[0x18F..]) / 3; // Regel 4: Drittel-DM
        var finance = Finance(manager);
        finance.CostLeague = (League)Math.Clamp((int)record[0xEB], 0, 2);
        var stadium = finance.Stadium;
        stadium.TicketPrice = record[0xE4];
        const int s = 0x116; // Stadion: Stufe/Ziel je Bereich, Wochen, Plätze
        for (var i = 0; i < Stadium.FeatureCount; i++)
            stadium.SetLevel((StadiumFeature)i, record[s + 2 * i]);
        stadium.Seats = BinaryPrimitives.ReadInt16LittleEndian(record[0x133..]); // Stadion +0x1D
        stadium.SeatsTarget = BinaryPrimitives.ReadInt16LittleEndian(record[0x135..]); // +0x1F
        stadium.Standing = BinaryPrimitives.ReadInt16LittleEndian(record[0x137..]); // +0x21
        stadium.StandingTarget = BinaryPrimitives.ReadInt16LittleEndian(record[0x139..]); // +0x23
        // [ORIGINAL] Laufende Ausbauten (R14HI-02): Ziel +0x117 + 2 · Bereich (FILE43: Sitzkomfort 0 → 1, FILE44A: Gastronomie 2 → 3),
        // Restwochen +0x128 + Projekt 0–10 (FILE43: 8, FILE44A: 1); Projekt im Bau, wenn das Ziel ≠ 0 ist (Datei 0x3DF1F–0x3DF81).
        for (var p = 0; p < Stadium.ProjectCount; p++)
            stadium.ImportConstruction(p, p < Stadium.FeatureCount ? record[0x117 + 2 * p] : 0, record[0x128 + p]);
        // Bank und Managerdarlehen: ApplyLoans (Teil 4a, OriginalImport.Rest.cs).
        finance.SponsorRating = BinaryPrimitives.ReadInt16LittleEndian(record[0x173..]);
        // [ORIGINAL] Zuschauerstatistik (R14HI-04; Fortschreibung Datei 0x54664–0x54760, Felder als DGROUP bei Manager 0): Heimspiele
        // 0x9748 (+0xED), Kurve 0x9759 + n (+0xFE, Zuschauer / 1000, 22 Plätze), Summe 0x97DE (+0x183), Rekord 0x97E2 (+0x187) mit
        // Gegner 0x976F (+0x114), Minuskulisse 0x97E6 (+0x18B) mit Gegner 0x9770 (+0x115); Gegner als Satzplatz (Regel 1), beim
        // Spielstart 0 (Satz 0, FILE01) und bleiben über den Saisonwechsel stehen (FILE46) – „noch keiner“ (−1) nur vor dem ersten
        // Heimspiel der ersten Saison.
        var homeGames = Math.Min((int)record[0xED], 22);
        var neverPlayedAtHome = homeGames == 0 && OriginalImport.Season(save) == FirstSeason;
        var curve = new List<int>();
        for (var i = 0; i < homeGames; i++)
            curve.Add(record[0xFE + i]);
        int recordOpponent = record[0x114], minimumOpponent = record[0x115];
        finance.HomeStatistics.Import(homeGames, curve,
            BinaryPrimitives.ReadInt32LittleEndian(record[0x183..]), BinaryPrimitives.ReadInt32LittleEndian(record[0x187..]),
            !neverPlayedAtHome && recordOpponent < clubOf.Length ? clubOf[recordOpponent] : -1,
            BinaryPrimitives.ReadInt32LittleEndian(record[0x18B..]),
            !neverPlayedAtHome && minimumOpponent < clubOf.Length ? clubOf[minimumOpponent] : -1);
        finance.TalentPrice = BinaryPrimitives.ReadInt32LittleEndian(save.Block("020D").AsSpan(4 * m)) / 3;
        finance.InterestAccrualThirds = BinaryPrimitives.ReadInt32LittleEndian(save.Block("02A0").AsSpan(4 * m)); // Drittel bleiben

        // Jugend DGROUP 0xA075 + 0x15 · m: Stärke +0..2, Platz +3..5 und Liga +6..8 (0-basiert), Förderung +9 (3 × Drittel-DM).
        var youth = save.Block("A075").AsSpan(0x15 * m, 0x15);
        foreach (var team in finance.Youth)
        {
            var k = (int)team.Kind;
            team.Strength = youth[k];
            team.Rank = youth[3 + k] + 1;
            team.League = youth[6 + k] + 1;
            team.Funding = BinaryPrimitives.ReadInt32LittleEndian(youth[(9 + 4 * k)..]) / 3;
        }
    }

    /// <summary>
    /// Kader eines Managervereins aus den Kaderplätzen 5DC3 (Satz m) und dem Pool far:8253 (Feldkarte „Spieler“). Spieler mit
    /// gleichem Namen und gleicher Rolle behalten ihr Objekt (und ihre ID), damit Verweise des Nachspielens gültig bleiben.
    /// Teil 4b: Ein Spieler, den der Manager auf die Transferliste gesetzt hat, steht im Original nicht mehr im Kadersatz,
    /// sondern im Marktsatz 4 (Pool-Besitzer = Manager; Feldbelege L7, FILE40 → FILE41). Seit A380 (#669) gilt das auch im Remake
    /// (<see cref="TransferMarket.Offer"/>): Der Spieler wird übernommen, steht aber nicht im Kader, nicht in der Aufstellung und
    /// nicht unter den Eingesetzten (+0x0A = 0, Datei 0x47093); das Angebot legt <c>ApplyPoolAndMarket</c> an (#676, Leitstelle
    /// Runde 14; FILE26 HOCK, FILE41 HOHENADL). <paramref name="squadPlayers"/> erhält je Poolindex den Spieler.
    /// </summary>
    private void ApplySquad(OriginalSaveGame save, Club club, int record, int[] clubOf, HashSet<string> foreigners,
        Dictionary<int, Player> squadPlayers)
    {
        var squad = save.Block("5DC3");
        var pool = save.Block("far:8253");
        var old = club.Players.ToList();
        var players = new List<(Player Player, int Line, bool Forward, bool Dressed)>();
        var places = Enumerable.Range(0, OriginalSaveGame.SquadSlotsPerManager)
            .Select(i => (Offset: record * 0x618 + i * OriginalSaveGame.SquadSlotLength, Listed: false))
            .Concat(Enumerable.Range(0, OriginalSaveGame.SquadSlotsPerManager)
                .Select(i => (Offset: OriginalImport.MarketRecord * 0x618 + i * OriginalSaveGame.SquadSlotLength, Listed: true))
                .Where(o => squad[o.Offset + 0x0F] != 0 && pool[squad[o.Offset + 0x0F] * OriginalSaveGame.PoolRecordLength + 0x1A] == record));
        foreach (var (offset, listed) in places)
        {
            var slot = squad.AsSpan(offset, OriginalSaveGame.SquadSlotLength);
            if (!slot.ContainsAnyExcept((byte)0))
                continue;
            var entry = pool.AsSpan(slot[0x0F] * OriginalSaveGame.PoolRecordLength, OriginalSaveGame.PoolRecordLength);
            var name = CName(entry[..0x12]);
            var role = entry[0x18] == 0 ? PlayerRole.Goalkeeper : entry[0x18] < 50 ? PlayerRole.Defender : PlayerRole.Forward;
            var player = old.FirstOrDefault(p => p.Name == name && p.Role == role);
            if (player is not null)
                old.Remove(player);
            else
            {
                player = new Player { Name = name, Role = role, IsForeigner = foreigners.Contains(name) };
                AssignPlayerId(player);
            }
            ApplySlot(player, slot, entry, clubOf, club);
            squadPlayers[slot[0x0F]] = player;
            if (listed)
                continue; // A380: nur auf dem Markt-Platz
            players.Add((player, slot[0x15] & 0x0F, (slot[0x15] & 0x80) != 0, slot[0x0A] != 0));
        }

        club.ClearLineup();
        foreach (var p in old)
            club.RemovePlayer(p);
        club.Players.Clear();
        club.Players.AddRange(players.Select(p => p.Player));
        club.RenumberPlayers(); // Nr. = Kaderplatz + 1 (Feldkarte „Player.Number“)

        // Aufstellung: Reihe 1–3 (+0x15, Bit 0x80 Sturm), Torhüter mit Reihe 1; Plätze in Kaderfolge.
        var used = new Dictionary<(int Block, PlayerRole Role), int>();
        // [ORIGINAL] +0x15 bestimmt den Platz, nicht die Rolle (Ausweichrollen der Automatik, Datei 0x42838; Lineup.Place): Sturm
        // nach Bit 0x80, sonst Verteidigung – ein Torhüter mit Reihe 1 ohne Bit auf den freien Torhüterplatz. Spur hok-export
        // (Rundreise, vier Saisonwechsel passiv: Stürmer M.RAUBAL in der Verteidigung).
        foreach (var (player, line, forward, dressed) in players)
        {
            // [ORIGINAL] #738: Eine Reihe ohne eingesetzt (+0x0A = 0, Handbetrieb nach Verletzung oder Abgang) zählt weder im Spiel
            // (0x120:0x66 Modus 1: +0x0A ≠ 0 und +0x15 ≠ 0) noch für die Teamwerte 0x952C, und die Aufstellung zeigt den Spieler
            // in keiner Reihe (FILE24 STEFAN/CHAIDAROV); FILE38–46 KÖLN: Felder 0x18–0x1D = Werte ohne diese Spieler.
            if (line is < 1 or > 3 || !player.Available || !dressed)
                continue;
            LineupSlot? target = player.Role == PlayerRole.Goalkeeper && !forward && line == 1 && club.Lineup.Goalkeeper is null
                ? LineupSlot.Goalkeeper
                : forward
                    ? NextSlot(line - 1, PlayerRole.Forward, Lineup.ForwardsPerBlock)
                    : NextSlot(line - 1, PlayerRole.Defender, Lineup.DefendersPerBlock);
            if (target is { } t)
                club.Lineup.Place(t, player);
        }
        // Spur hok-export (Rundreise): Eingesetzte genau wie +0x0A – erst die von der Automatik zusätzlich Eingesetzten herausnehmen,
        // sonst ist die Zahl schon voll (22) und der Ersatz des HOK käme nicht hinein. Die Einstellung „automatisch aufstellen“
        // setzt danach ApplyTactics aus 0x375 (OriginalImport.Rest.cs).
        foreach (var (player, _, _, dressed) in players)
            if (!dressed && club.IsDressed(player) && !club.Lineup.Contains(player))
                club.Undress(player);
        foreach (var (player, _, _, dressed) in players)
            if (dressed && !club.Lineup.Contains(player) && club.DressedCount < Club.MaxDressed)
                club.Dress(player);
        return;

        LineupSlot? NextSlot(int block, PlayerRole role, int capacity)
        {
            used.TryGetValue((block, role), out var n);
            if (n >= capacity)
                return null;
            used[(block, role)] = n + 1;
            return role == PlayerRole.Defender ? LineupSlot.Defender(block, n) : LineupSlot.Forward(block, n);
        }
    }

    /// <summary>Werte eines Kaderplatzes (0x34 Byte) und des Pool-Spielers (33 Byte). Geld in Drittel-DM (Regel 4).</summary>
    /// <param name="club">Verein des Kaderplatzes (Leihe: nie der Verleiher) oder null für einen Markt-Platz.</param>
    private void ApplySlot(Player p, ReadOnlySpan<byte> slot, ReadOnlySpan<byte> entry, int[] clubOf, Club? club)
    {
        p.Age = entry[0x14];
        p.Talent = entry[0x12];
        p.Condition = slot[0x10];
        p.Technique = slot[0x11];
        p.Form = slot[0x12];
        p.PoolForm = entry[0x17] != slot[0x12] ? entry[0x17] : null; // A066 (kader-rest): Poolform nach einer Verletzung
        p.Strain = slot[0x13];
        p.TrendControl = slot[0x14];
        // [ORIGINAL] Tendenzwert Kaderplatz +0x0E (30 … 68, Start random(35, 65) Datei 0x4907A; Anzeige (Wert − 30) / 13, 0x41AE0 –
        // Player.Slot.cs); die Stufe folgt daraus (R14HI-04). [APPROX] 0 (nicht gesetzt) lässt die Stufe des Nachspielens stehen.
        p.TrendScore = slot[0x0E];
        if (p.TrendScore != 0)
            p.Trend = Player.TrendOf(p.TrendScore);
        p.Penalties5 = slot[0x00];
        p.Penalties2 = slot[0x01];
        var goals = slot[0x03] + slot[0x04] + slot[0x05];
        p.Games = slot[0x06] + slot[0x07] + slot[0x08];
        // A135 (kader-start Runde 14, Schema 43): Zähler je Wettbewerb – Saisonspiele +0x06/+0x07/+0x08 (Byte), beim Verein
        // +0x1C/+0x1E/+0x20 (Wort); der Übertrag ist der Vereinswert ohne die laufende Saison (Player.Competitions.cs).
        p.CupGames = slot[0x07];
        p.EuropeGames = slot[0x08];
        p.ClubGamesCarry = BinaryPrimitives.ReadInt16LittleEndian(slot[0x1C..]) - slot[0x06];
        p.ClubCupGamesCarry = BinaryPrimitives.ReadInt16LittleEndian(slot[0x1E..]) - slot[0x07];
        p.ClubEuropeGamesCarry = BinaryPrimitives.ReadInt16LittleEndian(slot[0x20..]) - slot[0x08];
        if (p.Role == PlayerRole.Goalkeeper)
        {
            // [APPROX] Beim Torhüter zählen +0x03–0x05 und +0x22–0x26 die Gegentore; das Remake führt Gegentore ohne Wettbewerb und
            // ohne Vereinswert (Tore je Wettbewerb und Überträge 0).
            p.CupGoals = p.EuropeGoals = 0;
            p.ClubGoalsCarry = p.ClubCupGoalsCarry = p.ClubEuropeGoalsCarry = 0;
            p.Goals = 0;
            p.GoalsAgainst = goals;
            p.GoalieMinutes = BinaryPrimitives.ReadUInt16LittleEndian(entry[0x1F..]);
            p.Assists = 0;
        }
        else
        {
            p.Goals = goals;
            p.CupGoals = slot[0x04];
            p.EuropeGoals = slot[0x05];
            p.ClubGoalsCarry = BinaryPrimitives.ReadInt16LittleEndian(slot[0x22..]) - slot[0x03];
            p.ClubCupGoalsCarry = BinaryPrimitives.ReadInt16LittleEndian(slot[0x24..]) - slot[0x04];
            p.ClubEuropeGoalsCarry = BinaryPrimitives.ReadInt16LittleEndian(slot[0x26..]) - slot[0x05];
            p.Assists = BinaryPrimitives.ReadUInt16LittleEndian(entry[0x1F..]);
        }
        p.InjuredGames = (slot[0x09] & 0x02) != 0 ? slot[0x0D] : 0;
        if (p.InjuredGames > 0)
            p.InjuryKind = slot[0x17];
        p.SuspendedGames = 0; // [APPROX] Feldkarte U
        p.ContractYears = slot[0x0B];
        // [ORIGINAL] Kaderplatz +0x18 (docs/transfer.md): Bit 0x80 „wird sich mit Ablauf des Vertrages zur Ruhe setzen“ (Test bei 0x4A6C7 im Zweig ab 0x4A692),
        // > 99 Angebot des Spielers „von 1 auf n Jahre“ mit n = +0x18 − 100 (0x4A3AF; Saisonende 0x27D05 schreibt 101), sonst
        // Verhandlungssperre in Tagen (R14HI-04). Die Meldung dazu steht im Pool +0x1E (offene Meldung), nicht im Vertragsfeld.
        var notice = slot[0x18];
        (p.Notice, p.NoticeYears, p.NegotiationLock) = (notice & 0x80) != 0 ? (ContractNotice.Retiring, 0, 0)
            : notice > 99 ? (ContractNotice.Offered, notice - 100, 0)
            : (ContractNotice.None, 0, (int)notice);
        p.HiddenSkill1 = slot[0x19] & 0x0F;
        p.HiddenSkill2 = slot[0x19] >> 4;
        p.MonthlySalary = BinaryPrimitives.ReadInt32LittleEndian(slot[0x28..]) / 3;
        var price = BinaryPrimitives.ReadUInt32LittleEndian(slot[0x2C..]);
        p.InsuranceLocked = (price & 0x8000_0000) != 0;
        p.PurchasePrice = (price & 0x7FFF_FFFF) / 3;
        p.InsurancePremium = BinaryPrimitives.ReadInt32LittleEndian(slot[0x30..]) / 3;
        p.UnsolicitedOffer = (slot[0x09] & 0x40) != 0 && slot[0x16] < clubOf.Length ? Clubs[clubOf[slot[0x16]]] : null;
        // #688 (R688S-02, A115): Leihe. [ORIGINAL] Kaderplatz +0x0C = Verein + 0x80 (Datei 0x47920–0x47925 bei der Leihe, 0 sonst;
        // Prüfung „… ist leider von <Verein> nur ausgeliehen“ Datei 0x4A692, docs/transfer.md), Pool +0x1A bleibt beim Verleiher
        // (4 = Markt). FILE44 VALLIERE: +0x0C = 0xB9 (Vereinssatz 57, ein Land), Pool +0x1A = 4; am Saisonende frei ohne Ablöse (FILE46,
        // Pool +0x1A = 6, Datei 0x275FF). Der Verein in +0x0C ist der Vereinssatz des Originals (0–47 Ligavereine, ab 52 Länder;
        // Abbildung clubOf wie bei +0x16).
        // [APPROX] Stammvertrag (Laufzeit, Gehalt, Versicherung) in PlayerLoan unbelegt: Werte des Leih-Kaderplatzes; der
        // Saisonwechsel braucht nur den Verleiher (StartSquadSeason).
        var loanSlot = slot[0x0C] & 0x7F;
        if (club is not null && (slot[0x0C] & 0x80) != 0 && loanSlot < clubOf.Length && Clubs[clubOf[loanSlot]] is { } lender && lender != club)
        {
            p.OnLoan = true;
            p.Loan = new PlayerLoan(lender, p.ContractYears, p.MonthlySalary, p.Insured);
        }
        else
        {
            p.OnLoan = false;
            p.Loan = null;
        }
    }

    private static string CName(ReadOnlySpan<byte> raw)
    {
        var end = raw.IndexOf((byte)0);
        return GameText.Decode(end < 0 ? raw : raw[..end]).TrimEnd();
    }
}
