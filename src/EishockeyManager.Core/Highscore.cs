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

/// <summary>Schlüssel der Bestenliste und Punkte eines Managers.</summary>
/// <remarks>
/// [ORIGINAL] Schlüssel (Datei 0x28A2C ff. in der Spielvorbereitung, DGROUP 0x256): 0 = keine Bestenliste (die Kachel
/// „Highscore“ ist gesperrt, Datei 0x44FCE); Spielmodus „nur Weltkugel“ (Knopfbits = 2) → 30000;
/// sonst, wenn Level &gt; 2 und (Startstärke = Mittelfeld oder (Dauer = eine Saison und Startliga = Bundesliga)):
/// <c>Level + 4 · Dauer + 32 · Startliga + 128 · Startstärke − 2</c>.
/// Punkte (Datei 0x5D04F): Summe (Stadionwerte, Plätze, Vermögen, Platzierung, Saisonverlauf) / 20, mindestens 1; dazu
/// <c>8 · Meisterschaften + 6 · Pokalsiege + 10 · Europapokalsiege</c> (Datei 0x5D579 ff.).
/// <list type="bullet">
/// <item>[ORIGINAL] Stadion: Σ Stufe · Preis / 1000 mit den neun Preisen DGROUP 0xC0 (3,60; 1,35; 1,92; 1,11; 0,99; 3,90; 1,74; 1,56;
///   1,35 Mio.), dazu Sitzplätze · 0,54 + Stehplätze · 0,405 (DGROUP 0xE4, geteilt durch 10⁶).</item>
/// <item>[ORIGINAL] Vermögen: Konto + Aktiendepot + Forderungen an andere Manager (+ Spielerwerte), höchstens 50 Mio.; Punkte = Vermögen / 2000.</item>
/// <item>[ORIGINAL] Platzierung: 25 · (36 − Platz − Ligaversatz 0/12/24), nicht unter 0.</item>
/// <item>[ORIGINAL] Saisonverlauf je Saison: 7 · (36 − Liga-Kennzahl), Pokal 80 (Sieger) bzw. 10 · Runde, Europapokal 130 bzw. 20 · Runde;
///   die Summe wird durch (Saisons + 1) geteilt (höchstens 50 Saisons).</item>
/// </list>
/// [APPROX]
/// <list type="bullet">
/// <item>Nicht abgebildet, weil die Felder im Remake fehlen oder nicht belegt sind: die Funktion 0x100:0x57 (Summand · 100),
///   die vier Bytes DGROUP 0x9741 ff. (je 1–6 → 150 · Wert), der Aufruf 0x138:0x2A (Abzug beim Vermögen), die Spielerwerte
///   (Summe halber Marktwerte); Bankguthaben und Kredit stehen stellvertretend für den Abzug.</item>
/// <item>Das Remake führt Level, Startliga und Startstärke je Manager; für den Schlüssel gelten die des ersten Managers.</item>
/// <item>Die Kennzahlen des Saisonverlaufs folgen der Kodierung von „Früher“ (Platz, 0x33–0x35 Play-Off-Aus, 0x80 Meister;
///   DEB-Pokal: Runde 0–7, Sieger Bit 7; Europapokal ebenso) – im Original steht dort derselbe Datensatz, die Zuordnung der
///   Bytes ist aber nur aus dem Code erschlossen.</item>
/// <item>Titelzähler: Meister, Pokalsieger, Europapokalsieger aus den Saisonergebnissen; im Spielmodus „nur Weltkugel“
///   zählt das Original die Platzierungen der Nationalmannschaft (Σ 9 − Platz) – hier die Weltmeistertitel + 1.</item>
/// </list>
/// </remarks>
public static class HighscoreRules
{
    /// <summary>Schlüssel für „nur Weltkugel“ (Bundestrainer). [ORIGINAL] DGROUP 0x256 = 0x7530.</summary>
    public const int WorldOnlyKey = 30000;

    public const int MaxWealth = 50_000_000;

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
        var a = (byte)Math.Min(255, history.Count(h => h.LeagueOutcome == LeagueOutcome.GermanChampion));
        var b = (byte)Math.Min(255, history.Count(h => h.Deb == CupOutcome.Winner));
        var c = (byte)Math.Min(255, history.Count(h => h.Europe == CupOutcome.Winner));
        var score = Score(game, manager) + 8 * a + 6 * b + 10 * c;
        return new HighscoreEntry(m.Name, game.Clubs[m.ClubIndex].Name, a, b, c, (short)Math.Clamp(score, 1, short.MaxValue), stamp);
    }

    /// <summary>Punkte ohne Titelzuschlag (<c>Summe / 20</c>, mindestens 1).</summary>
    public static int Score(GameState game, int manager)
    {
        var m = game.Managers[manager];
        var finance = game.Finance(m);
        long sum = 0;

        // Stadion
        for (var i = 0; i < StadiumPrices.Length && i < Stadium.FeatureCount; i++)
            sum += finance.Stadium.Level((StadiumFeature)i) * (StadiumPrices[i] / 1000);
        sum += (finance.Stadium.Seats * 540_000L + finance.Stadium.Standing * 405_000L) / 1_000_000;

        // Vermögen
        var wealth = m.Money + game.DepotValue(m) + game.ManagerReceivables(m) + finance.Bank.Balance;
        sum += Math.Min(wealth, MaxWealth) / 2000;

        // Platzierung
        var club = game.Clubs[m.ClubIndex];
        if (club.League is { } league && game.PlayedMatchDays > 0)
        {
            var rank = LeagueTable.Compute(game, league, TableView.Gesamt, game.PlayedMatchDays).First(r => r.Club.Index == club.Index).Rank;
            sum += 25L * Math.Max(0, 36 - rank - 12 * (int)league);
        }

        // Saisonverlauf
        var seasons = Math.Min(50, game.ManagerHistory(manager).Count);
        long past = 0;
        foreach (var h in game.ManagerHistory(manager).Take(seasons))
        {
            var code = h.LeagueOutcome switch
            {
                LeagueOutcome.Rank => h.Rank - 1,
                LeagueOutcome.PlayoffQuarterFinal => 0x33,
                LeagueOutcome.PlayoffSemiFinal => 0x34,
                LeagueOutcome.PlayoffFinal => 0x35,
                _ => 0x80,
            };
            past += (36 - code) * 7;
            past += h.Deb == CupOutcome.Winner ? 80 : 10 * h.DebRound;
            past += h.Europe == CupOutcome.Winner ? 130 : h.Europe == CupOutcome.NotParticipating ? 0 : h.EuropeFinalRound ? 40 : 20;
        }
        if (seasons > 0)
            past /= seasons + 1;
        sum += past;

        return (int)Math.Max(1, sum / 20);
    }

    /// <summary>
    /// Trägt die aktuellen Einträge aller Manager ein ([ORIGINAL] Datei 0x5D90D: für jeden Manager Eintrag bauen, einfügen).
    /// </summary>
    public static void Update(HighscoreTable table, GameState game, uint stamp)
    {
        for (var m = 0; m < game.Managers.Count; m++)
            table.Insert(Entry(game, m, stamp));
    }
}
