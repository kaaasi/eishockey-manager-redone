using System.Text;

namespace EishockeyManager.Core;

/// <summary>Ein Eintrag der Bestenliste (58 Byte in der Datei <c>high\h.&lt;Schlüssel&gt;</c>).</summary>
/// <param name="Name">Name des Managers (Platz 26 Byte, mit 0 abgeschlossen).</param>
/// <param name="Club">Name des Vereins (Platz 23 Byte).</param>
/// <param name="TitlesA">Meisterschaften (erste Zahl im Bild „(X/X/X)“).</param>
/// <param name="TitlesB">Pokalsiege (zweite Zahl).</param>
/// <param name="TitlesC">Europapokalsiege (dritte Zahl).</param>
/// <param name="Score">Punkte (16 Bit mit Vorzeichen); 0 = leerer Platz.</param>
/// <param name="Stamp">Kennung der Sitzung, in der der Eintrag geschrieben wurde (32 Bit).</param>
public readonly record struct HighscoreEntry(string Name, string Club, byte TitlesA, byte TitlesB, byte TitlesC, short Score, uint Stamp)
{
    public const int Size = 58;
    public const int NameLength = 26;
    public const int ClubLength = 23;

    public static HighscoreEntry Empty => new("", "", 0, 0, 0, 0, 0);

    public bool IsEmpty => Score == 0;

    /// <summary>Titel in der Reihenfolge der Anzeige „(Meister/Pokal/Europa)“.</summary>
    public string TitleText => $"({TitlesA}/{TitlesB}/{TitlesC})";

    public static HighscoreEntry Read(ReadOnlySpan<byte> data) => new(
        ReadText(data[..NameLength]),
        ReadText(data.Slice(NameLength, ClubLength)),
        data[49], data[50], data[51],
        BitConverter.ToInt16(data.Slice(52, 2)),
        BitConverter.ToUInt32(data.Slice(54, 4)));

    public void Write(Span<byte> data)
    {
        data[..Size].Clear();
        WriteText(data[..NameLength], Name);
        WriteText(data.Slice(NameLength, ClubLength), Club);
        data[49] = TitlesA;
        data[50] = TitlesB;
        data[51] = TitlesC;
        BitConverter.TryWriteBytes(data.Slice(52, 2), Score);
        BitConverter.TryWriteBytes(data.Slice(54, 4), Stamp);
    }

    private static string ReadText(ReadOnlySpan<byte> field)
    {
        var end = field.IndexOf((byte)0);
        return Encoding.Latin1.GetString(end < 0 ? field : field[..end]);
    }

    private static void WriteText(Span<byte> field, string text)
    {
        var bytes = Encoding.Latin1.GetBytes(text);
        bytes.AsSpan(0, Math.Min(bytes.Length, field.Length - 1)).CopyTo(field);
    }
}

/// <summary>
/// Bestenliste („Highscore“): acht Einträge, absteigend nach Punkten, je Spielvorbereitung eine eigene Liste.
/// </summary>
/// <remarks>
/// [ORIGINAL] em.exe, Overlay-Funktionen Datei 0x5D04F (Punkte), 0x5D4C2 (Eintrag bauen), 0x5D617 (Datei laden/schreiben),
/// 0x5D73D (Einfügen und Sortieren), 0x5D90D (Bildschirm); Aufrufer 0x56647 (Saisonwechsel, je Manager, nur eintragen),
/// 0x5642E (Spielende: eintragen und zeigen) und die Kachel „Highscore“ (Datei 0x45773):
/// <list type="bullet">
/// <item>Datei <c>high\h.&lt;Schlüssel&gt;</c>, 464 Byte = 8 × 58: Name (26) | Verein (23) | Titel (3 × 1) | Punkte (16 Bit) | Kennung (32 Bit).
///   Gibt es sie nicht, ist die Liste leer (alles 0).</item>
/// <item>Einfügen (<see cref="Insert"/>): Gibt es schon einen Eintrag mit gleichem Namen <b>und</b> Verein, ersetzt der neue ihn
///   an derselben Stelle – aber nur, wenn überhaupt ein Platz mit weniger Punkten vorhanden ist (Eigenheit des Originals: ein
///   schlechterer neuer Wert überschreibt den besseren). Sonst rutschen die Einträge ab dem ersten Platz mit weniger Punkten
///   nach unten (der letzte fällt heraus). Danach sortiert ein Austausch-Sortieren (absteigend nach Punkten).</item>
/// </list>
/// </remarks>
public sealed class HighscoreTable
{
    public const int Length = 8;
    public const int FileLength = Length * HighscoreEntry.Size;

    private readonly HighscoreEntry[] _entries = new HighscoreEntry[Length];

    public HighscoreTable()
    {
        Array.Fill(_entries, HighscoreEntry.Empty);
    }

    public IReadOnlyList<HighscoreEntry> Entries => _entries;

    /// <summary>Liest eine Datei von 464 Byte; andere Längen ergeben eine leere Liste.</summary>
    public static HighscoreTable Parse(ReadOnlySpan<byte> data)
    {
        var table = new HighscoreTable();
        if (data.Length != FileLength)
            return table;
        for (var i = 0; i < Length; i++)
            table._entries[i] = HighscoreEntry.Read(data.Slice(i * HighscoreEntry.Size, HighscoreEntry.Size));
        return table;
    }

    public byte[] ToBytes()
    {
        var data = new byte[FileLength];
        for (var i = 0; i < Length; i++)
            _entries[i].Write(data.AsSpan(i * HighscoreEntry.Size, HighscoreEntry.Size));
        return data;
    }

    /// <summary>[ORIGINAL] Datei 0x5D73D; siehe Klassenbeschreibung.</summary>
    public void Insert(HighscoreEntry entry)
    {
        var same = -1;
        for (var i = 0; i < Length; i++)
            if (_entries[i].Name == entry.Name && _entries[i].Club == entry.Club)
            {
                same = i;
                break;
            }

        var slot = -1;
        for (var i = 0; i < Length; i++)
            if (_entries[i].Score < entry.Score)
            {
                slot = i;
                break;
            }
        if (slot >= 0)
        {
            if (same >= 0)
                slot = same;
            else
                for (var k = Length - 1; k > slot; k--)
                    _entries[k] = _entries[k - 1];
            _entries[slot] = entry;
        }

        for (var i = 0; i < Length - 1; i++)
            for (var j = i + 1; j < Length; j++)
                if (_entries[i].Score < _entries[j].Score)
                    (_entries[i], _entries[j]) = (_entries[j], _entries[i]);
    }
}

/// <summary>Kennzahlen einer abgeschlossenen Saison im Verlauf eines Managers (je 4 Byte im Original, Manager + 0x18 + 4 · Saison).</summary>
/// <param name="League">Platz (1-basiert), 0x33–0x35 = Play-Off-Aus, +0x80 = Meister; 0xFF = leer.</param>
/// <param name="Deb">DEB-Pokal: erreichte Runde 1–6, +0x80 = Sieger; 0x1E = nicht im Wettbewerb.</param>
/// <param name="Europe">Europapokal: 8 + Stufe, 0x8D = Sieger; 0 = nicht im Wettbewerb.</param>
public readonly record struct SeasonCodes(byte League, byte Deb, byte Europe)
{
    public const byte Empty = 0xFF;
    public const byte DebNotParticipating = 0x1E;
    public const byte EuropeWinner = 0x8D;

    /// <summary>
    /// Kennzahlen aus dem Saisonverlauf des Remakes. [ORIGINAL] Aufbau beim Saisonwechsel Datei 0x55B36–0x55DBD.
    /// [APPROX] Die Play-Off-Stufe des Meisters (0x35) und die Europapokal-Stufen (Gruppe 1, Endrunde 2) sind aus dem Code erschlossen.
    /// </summary>
    public static SeasonCodes Of(ManagerSeasonHistory h)
    {
        var league = h.LeagueOutcome switch
        {
            LeagueOutcome.Rank => (byte)Math.Clamp(h.Rank, 0, 0x7F),
            LeagueOutcome.PlayoffQuarterFinal => (byte)0x33,
            LeagueOutcome.PlayoffSemiFinal => (byte)0x34,
            LeagueOutcome.PlayoffFinal => (byte)0x35,
            _ => (byte)(0x80 | 0x35),
        };
        var deb = h.Deb == CupOutcome.NotParticipating
            ? DebNotParticipating
            : (byte)((h.DebRound + 1) | (h.Deb == CupOutcome.Winner ? 0x80 : 0));
        var europe = h.Europe switch
        {
            CupOutcome.NotParticipating => (byte)0,
            CupOutcome.Winner => EuropeWinner,
            _ => (byte)(8 + (h.EuropeFinalRound ? 2 : 1)),
        };
        return new SeasonCodes(league, deb, europe);
    }
}

/// <summary>Rohwerte der Punkteformel (Datei 0x5D04F), alle Geldwerte in Drittel-DM wie im Original.</summary>
public sealed class HighscoreInputs
{
    /// <summary>Bewertung des Vereins 0–100 (Aufruf 0x100:0x57).</summary>
    public int Rating { get; init; }

    /// <summary>Ausbaustufen der neun Stadionmerkmale.</summary>
    public int[] StadiumLevels { get; init; } = new int[9];

    public int Seats { get; init; }
    public int Standing { get; init; }

    /// <summary>Vermögen vor der Obergrenze: Konto + Aktien + Forderungen + halbe Spielerwerte − Schulden.</summary>
    public long Wealth { get; init; }

    /// <summary>Tabellenplatz des Vereins, 0-basiert, und Ligaversatz 0/12/24 (Tabelle DGROUP 0x7C0).</summary>
    public int Rank0 { get; init; }

    public int LeagueOffset { get; init; }

    /// <summary>Laufender Pokalstand, vier Bytes ab Manager + 0xE6 (DEB-Pokal, Europapokal, 2 weitere); 1–6 zählt.</summary>
    public int[] Cups { get; init; } = new int[4];

    /// <summary>Verlauf der letzten Saisons (höchstens 50 werden gewertet).</summary>
    public IReadOnlyList<SeasonCodes> History { get; init; } = [];
}

/// <summary>Schlüssel der Bestenliste und Punkte eines Managers.</summary>
/// <remarks>
/// [ORIGINAL] Schlüssel (Datei 0x28A2C ff. in der Spielvorbereitung, DGROUP 0x256): 0 = keine Bestenliste (die Kachel
/// „Highscore“ ist gesperrt, Datei 0x44FCE); Spielmodus „nur Weltkugel“ (Knopfbits = 2) → 30000;
/// sonst, wenn Level &gt; 2 und (Startstärke = Mittelfeld oder (Dauer = eine Saison und Startliga = Bundesliga)):
/// <c>Level + 4 · Dauer + 32 · Startliga + 128 · Startstärke − 2</c>.
/// Punkte (Datei 0x5D04F, im Emulator gegen <see cref="Points"/> geprüft, 500 Zufallsfälle): Summe / 20 (mindestens 1 bei der
/// Eintragung); dazu <c>8 · Meisterschaften + 6 · Pokalsiege + 10 · Europapokalsiege</c> (Datei 0x5D579 ff.).
/// <list type="bullet">
/// <item>Bewertung des Vereins (0x100:0x57, <see cref="SponsorRating"/>) · 100.</item>
/// <item>Stadion: Σ Stufe · (Preis / 1000) mit den neun Preisen DGROUP 0xC0 (3,60; 1,35; 1,92; 1,11; 0,99; 3,90; 1,74; 1,56;
///   1,35 Mio.), dazu Sitzplätze · 540000 und Stehplätze · 405000 (DGROUP 0xE4), je <b>32-Bit-Produkt ohne Vorzeichen</b> (läuft
///   über ab 7.954 Sitz- bzw. 10.607 Stehplätzen, wie im Original) geteilt durch 10⁶.</item>
/// <item>Vermögen (Drittel): Konto + Σ Aktienzahl · Kurs (4 Werte) + Forderungen an andere Manager und Bank (negative Einträge der
///   anderen Strukturen) + Σ halbe Spielerwerte (0x118:0x70) − eigene Schulden bei Managern und Bank (0x138:0x2A, Aufruf mit
///   0 / 0x63); höchstens 50.000.000; Punkte = Vermögen / 2000 (vorzeichenbehaftet).</item>
/// <item>Platzierung: 25 · (36 − Platz − Ligaversatz), nicht unter 0 (Platz 1-basiert).</item>
/// <item>Laufender Pokalstand: je Byte 1–6 ⇒ 150 · Wert (Manager + 0xE6 ff.; DEB-Pokal: erreichte Runde, Datei 0x34D8D).</item>
/// <item>Saisonverlauf (je Saison, ≤ 50, durch Saisons + 1 geteilt): 7 · (36 − Ligakennzahl) (außer 0xFF); DEB-Kennzahl: Sieger 80, sonst
///   10 · (Kennzahl &amp; 7) – auch „nicht im Wettbewerb“ (0x1E → 60); Europapokal nur, wenn die DEB-Kennzahl ein Sieger ist
///   oder ≥ 8: Sieger 130, sonst 20 · (Kennzahl &amp; 7) (Eigenheit des Originals, Bit-Verschiebung nach der Maskierung).</item>
/// </list>
/// [APPROX]
/// <list type="bullet">
/// <item>Spielerwerte nach 0x49D36 (<see cref="PlayerValue"/>, Spur spielerwert) mit den Eingängen des Remakes (dort [APPROX]); die
///   Bewertung und die Spielerwerte rechnen mit festen Zufallsströmen, damit das Öffnen des Bildschirms nichts verändert.</item>
/// <item>Der Europapokal-Stand (Gruppe 1, Endrunde 2) und die weiteren Pokalbytes (nur 0x9744 = 1 bei einem Manager im Spieljahr 1994
///   ist belegt) sind nicht abgebildet; Meister-Kennzahl 0x80 | 0x35 statt der Play-Off-Stufe des Brackets.</item>
/// <item>Das Remake führt Level, Startliga und Startstärke je Manager; für den Schlüssel gelten die des ersten Managers.</item>
/// <item>Im Spielmodus „nur Weltkugel“ zählt das Original die Platzierungen der Nationalmannschaft (Σ 9 − Platz) – hier die
///   Weltmeistertitel + 1.</item>
/// </list>
/// Die Formel enthält Eigenheiten, die wie im Original übernommen sind: Der Meister erhält durch die Kennzahl 0x80 + … einen
/// <i>negativen</i> Ligaterm (36 − 181) · 7, und die Europapokal-Wertung entfällt für ausgeschiedene DEB-Teilnehmer.
/// </remarks>
public static class HighscoreRules
{
    /// <summary>Schlüssel für „nur Weltkugel“ (Bundestrainer). [ORIGINAL] DGROUP 0x256 = 0x7530.</summary>
    public const int WorldOnlyKey = 30000;

    public const long MaxWealth = 50_000_000;

    private static readonly long[] StadiumPrices = [3_600_000, 1_350_000, 1_920_000, 1_110_000, 990_000, 3_900_000, 1_740_000, 1_560_000, 1_350_000];

    /// <summary>Schlüssel der Bestenliste dieser Spielvorbereitung oder null, wenn es keine gibt.</summary>
    public static int? Key(GameSetup setup)
    {
        var bits = setup.ModeBits;
        if (bits == 2)
            return WorldOnlyKey;
        if ((bits & 1) == 0 || setup.Managers.Count == 0)
            return null;
        var first = setup.Managers[0];
        var duration = (int)setup.Duration;
        var league = (int)first.StartLeague;
        var strength = (int)first.StartStrength;
        if (first.Level <= 2)
            return null;
        if (strength != (int)StartStrength.Mittelfeld && !(duration == 0 && league == 0))
            return null;
        return first.Level + 4 * duration + 32 * league + 128 * strength - 2;
    }

    /// <summary>Dateiname der Liste (ohne Ordner): <c>h.&lt;Schlüssel&gt;</c>.</summary>
    public static string FileName(int key) => $"h.{key}";

    /// <summary>Eintrag, den der Manager <paramref name="manager"/> zum jetzigen Stand verdient.</summary>
    public static HighscoreEntry Entry(GameState game, int manager, uint stamp)
    {
        var m = game.Managers[manager];
        if (game.WorldOnly)
        {
            var worldTitles = (byte)Math.Min(255, game.History.Count(r => r.Titles.TryGetValue(Title.WorldChampion, out _)));
            return new HighscoreEntry(m.Name, game.Clubs[NationalTeams.Germany].Name, 0, 0, 0, (short)Math.Max(1, 1 + worldTitles), stamp);
        }

        var history = game.ManagerHistory(manager);
        // [APPROX] Titelzähler aus dem Saisonverlauf; das Original liest die Bytes Manager + 0x15–0x17 (0x5D545–0x5D56D).
        var a = (byte)Math.Min(255, history.Count(h => h.LeagueOutcome == LeagueOutcome.GermanChampion));
        var b = (byte)Math.Min(255, history.Count(h => h.Deb == CupOutcome.Winner));
        var c = (byte)Math.Min(255, history.Count(h => h.Europe == CupOutcome.Winner));
        var score = Score(game, manager) + 8 * a + 6 * b + 10 * c;
        // [APPROX] Begrenzung auf 1 … 32767; das Original rechnet die Gesamtpunkte als 16-Bit-Wert und läuft über.
        return new HighscoreEntry(m.Name, game.Clubs[m.ClubIndex].Name, a, b, c, (short)Math.Clamp(score, 1, short.MaxValue), stamp);
    }

    /// <summary>
    /// Punkte aus den Rohwerten ([ORIGINAL] Datei 0x5D04F), ohne Titelzuschlag und ohne Untergrenze; Ergebnis wie im Original
    /// ein 16-Bit-Wert.
    /// </summary>
    public static int Points(HighscoreInputs input)
    {
        long sum = input.Rating * 100L;
        for (var i = 0; i < StadiumPrices.Length; i++)
            sum += (StadiumPrices[i] / 1000) * (input.StadiumLevels[i] & 0xFF);
        sum += unchecked((uint)(input.Seats * 540_000)) / 1_000_000L;
        sum += unchecked((uint)(input.Standing * 405_000)) / 1_000_000L;
        sum += Math.Min(input.Wealth, MaxWealth) / 2000;
        sum += 25L * Math.Max(0, 36 - (input.Rank0 + input.LeagueOffset + 1));
        foreach (var v in input.Cups)
            if (v is > 0 and < 7)
                sum += 150L * v;

        // [APPROX] Saisonzahl = Länge des Verlaufs dieses Managers. Das Original zählt den globalen Saisonzähler
        // (DGROUP 0x3BD, höchstens 50, Teiler + 1, 0x5D3A3); der Unterschied wirkt nur bei entlassenen Managern.
        var seasons = Math.Min(50, input.History.Count);
        long past = 0;
        for (var i = 0; i < seasons; i++)
        {
            var h = input.History[i];
            if (h.League != SeasonCodes.Empty)
                past += (36 - h.League) * 7;
            int gate;
            if ((h.Deb & 0x80) != 0)
            {
                past += 80;
                gate = h.Deb;
            }
            else
            {
                past += (h.Deb & 7) * 10;
                gate = h.Deb & 7;
            }
            if (gate >> 3 != 0)
                past += (h.Europe & 0x80) != 0 ? 130 : (h.Europe & 7) * 20;
        }
        if (seasons > 0)
            past /= seasons + 1;
        sum += past;
        return unchecked((short)(sum / 20));
    }

    /// <summary>Rohwerte des Managers im jetzigen Spielstand.</summary>
    public static HighscoreInputs Inputs(GameState game, int manager)
    {
        var m = game.Managers[manager];
        var finance = game.Finance(m);
        var club = game.Clubs[m.ClubIndex];

        // Vermögen in Drittel: Konto, Aktien, Forderungen, Bank (Sparbuch − Kredit), Darlehen an/von Managern, halbe Spielerwerte
        var marks = m.Money + game.DepotValue(m) + game.ManagerReceivables(m) - game.ManagerDebts(m) + finance.Bank.Balance;
        // [ORIGINAL] 0x5D242–0x5D28A: je Kaderplatz 0x118:0x70(Platz, 0) / 2 (vorzeichenbehaftet). Eigener fester Zufallsstrom
        // (nur Spieler mit Angebot ziehen), damit die Anzeige den Spielzustand nicht verändert.
        var valueRandom = new Random(club.Index);
        var wealth = marks * FinanceRules.ThirdsPerMark
            + club.Players.Sum(p => (long)(game.PlayerValueOf(p, club, PlayerValueKind.Price, valueRandom, m.Level) / 2));

        // [APPROX] Ohne gespielte Tabelle (vor dem ersten Spieltag, Weltmodus) bleibt der Platzierungsterm 0; das Original
        // verwendet dort das Platzbyte des Vereins.
        var rank0 = 35;
        var offset = 0;
        if (club.League is { } league && game.PlayedMatchDays > 0)
        {
            rank0 = LeagueTable.Compute(game, league, TableView.Gesamt, game.PlayedMatchDays).First(r => r.Club.Index == club.Index).Rank - 1;
            offset = 12 * (int)league;
        }

        var cups = new int[4];
        if (game.DebCup is { } deb && deb.Participants.Contains(club.Index))
            cups[0] = deb.Rounds.Select((r, i) => (r, i)).LastOrDefault(x => x.r.Any(t => t.Involves(club.Index))).i + 1;
        if (game.EuropeanCup is { } europe && europe.Draw.Contains(club.Index))
            cups[1] = europe.FinalRound?.Contains(club.Index) ?? false ? 2 : 1;

        return new HighscoreInputs
        {
            Rating = SponsorRating.Compute(game, m, new Random(club.Index)),
            StadiumLevels = Enumerable.Range(0, 9).Select(i => i < Stadium.FeatureCount ? finance.Stadium.Level((StadiumFeature)i) : 0).ToArray(),
            Seats = finance.Stadium.Seats,
            Standing = finance.Stadium.Standing,
            Wealth = wealth,
            Rank0 = rank0,
            LeagueOffset = offset,
            Cups = cups,
            History = game.ManagerHistory(manager).Select(SeasonCodes.Of).ToList(),
        };
    }

    /// <summary>Punkte ohne Titelzuschlag.</summary>
    public static int Score(GameState game, int manager) => Points(Inputs(game, manager));

    /// <summary>
    /// Trägt die aktuellen Einträge aller Manager ein ([ORIGINAL] Datei 0x5D90D: für jeden Manager Eintrag bauen, einfügen).
    /// </summary>
    public static void Update(HighscoreTable table, GameState game, uint stamp)
    {
        for (var m = 0; m < game.Managers.Count; m++)
            table.Insert(Entry(game, m, stamp));
    }
}
