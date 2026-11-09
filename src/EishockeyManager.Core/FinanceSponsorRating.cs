namespace EishockeyManager.Core;

/// <summary>
/// Bewertung eines Vereins für die Werbepartner, 0–100 (Spur finanzen-2, korrigiert in finanzen-werbung #132).
/// [ORIGINAL] em.exe Datei 0x3F833–0x3FB46 (Einsprung 0x100:0x57, Manager-Feld 0x97CE):
/// <code>
/// S  = (2 − StartLiga) · (80 · M + 170)
///    + 32 · (P · (n + 1) − 2), nur wenn n ≠ 0 (n = DEB-Pokal-Runde [0x5CF3])
///    + 47 · E · ([0x7AF] + 1)
///    + 12 · (11 − Ø-Platz der letzten fünf Platzfelder der laufenden Saison, Summe 0 → 6; siehe AverageRank)
///    + Ø(Spielerwert / 5500) + random(20, 50) · min(4, #Spieler mit Wert / 7000 > Ø(Wert / 5500) · random(35, 55) / 10)
///    + 3 · Stadionwert
/// R  = min(100, ((S mod 2^16) / 12 + 10 · (2 − StartLiga)) · 85 / 100), in 16 Bit ohne Vorzeichen
/// </code>
/// M, P, E sind 0/1 für „Verein ist gerade im Wettbewerb“ (Funktion 0x110:0x61, Masken 0x40 Meisterrunde, 0x08
/// DEB-Pokal, 0x20 Europapokal, Datei 0x44A48): kein Titelzähler. Belege: docs/finanzen-rekonstruktion.md.
/// Spielerwerte: <see cref="PlayerValue"/> (0x118:0x70, Preis, Drittel-DM; Spur spielerwert). [ORIGINAL] Die Funktion wird in beiden
/// Schleifen je Spieler neu gerufen (0x3F9E7, 0x3FA56); ein Spieler mit offenem Angebot zieht dabei jedes Mal random(95, 100).
/// </summary>
public static class SponsorRating
{
    /// <summary>Stadionwert ([ORIGINAL] 0x3EBEA, Tabelle DGROUP 0x4F9C), geteilt durch 700.</summary>
    private static readonly int[] FeatureWorth = [4333, 2333, 1166, 2833, 2000, 3333, 2333, 3166, 2250];

    /// <summary>
    /// Stadionwert: min(50.000, (1,5 × Sitzplätze + Stehplätze) / 2) + Σ adj_i × Wert_i, davon 1/700. Die Anpassung
    /// adj: Überdachung − 1 (nur wenn > 0), Kühlanlage mindestens 1, Sitzkomfort fest 2, Zustand fest 3, sonst Stufe.
    /// </summary>
    public static int StadiumWorth(Stadium stadium)
    {
        long worth = Math.Min(50_000, (3 * stadium.Seats / 2 + stadium.Standing) / 2);
        for (var i = 0; i < Stadium.FeatureCount; i++)
        {
            long level = stadium.Level((StadiumFeature)i);
            level = i switch
            {
                0 when level > 0 => level - 1,
                2 => Math.Max(1, level),
                3 => 2,
                4 => 3,
                _ => level,
            };
            worth += level * FeatureWorth[i];
        }
        return (int)(worth / 700);
    }

    /// <summary>
    /// Wettbewerbsanteil von S. [ORIGINAL] 0x3F83B–0x3F913: Meisterterm (2 − Liga) · (80 · M + 170); Pokal-Hilfswert
    /// startet mit 2 und wird nur bei Runde n ≠ 0 zu P · (n + 1), addiert 32 · (Hilfswert − 2) (random(32, 32));
    /// Europaterm 47 · E · (Runde + 1) (random(47, 47)).
    /// </summary>
    public static long CompetitionScore(int tier, bool inChampionship, bool inCup, int cupRound, bool inEurope, int europeRound)
    {
        long s = (2 - tier) * (80L * (inChampionship ? 1 : 0) + 170);
        var cup = cupRound != 0 ? (inCup ? 1 : 0) * (cupRound + 1) : 2;
        s += 32L * (cup - 2);
        s += 47L * (inEurope ? 1 : 0) * (europeRound + 1);
        return s;
    }

    /// <summary>
    /// Anzahl der Star-Spieler, höchstens 4. [ORIGINAL] 0x3F9B8–0x3FAA9: Ø = Σ(Wert / 5500) / Spielerzahl; je Spieler
    /// (mit eigenem Zug random(35, 55)) Star, wenn Wert / 7000 > Ø · random(35, 55) / 10 (32 Bit ohne Vorzeichen).
    /// </summary>
    public static int StarCount(IReadOnlyList<long> valuesThirds, Random random) =>
        StarCount(valuesThirds, i => valuesThirds[i], random);

    /// <summary>
    /// Wie <see cref="StarCount(IReadOnlyList{long}, Random)"/>, der Wert der zweiten Schleife wird aber je Spieler neu bestimmt
    /// (<paramref name="secondPass"/>, vor random(35, 55) – Reihenfolge wie 0x3FA56/0x3FA76).
    /// </summary>
    public static int StarCount(IReadOnlyList<long> firstPass, Func<int, long> secondPass, Random random)
    {
        if (firstPass.Count == 0)
            return 0;
        var average = firstPass.Sum(v => v / 5500) / firstPass.Count;
        var stars = 0;
        for (var i = 0; i < firstPass.Count; i++)
        {
            var value = secondPass(i);
            var threshold = average * SquadSetup.Between(random, 35, 55) / 10;
            if ((ulong)(value / 7000) > (ulong)threshold && stars < 4)
                stars++;
        }
        return stars;
    }

    /// <summary>Durchschnitt der Spielerwerte / 5500 (0x3F9C2–0x3FA21).</summary>
    public static long AverageValueScore(IReadOnlyList<long> valuesThirds) =>
        valuesThirds.Count == 0 ? 0 : valuesThirds.Sum(v => v / 5500) / valuesThirds.Count;

    /// <summary>
    /// Ø-Platz für die Bewertung. [ORIGINAL] em.exe Datei 0x3F915–0x3F98E: N = Spieltagszähler der Liga
    /// (DGROUP 0x7A9 + Liga, 1 vor dem ersten Spieltag, nach Spieltag m also m + 1); für i = N − 5 … N − 1 und nur i &gt; 0
    /// Summe += Platzfeld(i) + 1 (0x110:0x4D = Datei 0x43BED, Halbbyte i des Platzverlaufs DGROUP 0x828B, 0 = Platz 1);
    /// Teiler N − max(0, N − 5) zählt die übersprungenen i ≤ 0 mit (`div bx`, ganzzahlig); Summe 0 → 6 (0x3F98E).
    /// Das Platzfeld i schreibt die Minutenschleife beim Spieltag i + 1 (Datei 0x51049: Index 0x7A9 − 1, vor dem
    /// `inc 0x7A9` in 0x37897). Das Fenster umfasst damit Platzfeld 0 nie und als letztes das Feld des noch nicht
    /// gespielten Spieltags N: es trägt den Wert der Vorsaison (der Verlauf wird nur beim Spielstart geleert,
    /// 0x54CE9 → 0x436DD), in der ersten Saison 0 (Platz 1).
    /// </summary>
    /// <param name="nextMatchDay">N = gespielte Ligaspieltage + 1.</param>
    /// <param name="place">Platzfeld i (0 = Platz 1).</param>
    public static int AverageRank(int nextMatchDay, Func<int, int> place)
    {
        var first = nextMatchDay - 5;
        var sum = 0;
        for (var i = first; i < nextMatchDay; i++)
            if (i > 0)
                sum += place(i) + 1;
        if (sum == 0)
            return 6;
        return sum / (nextMatchDay - Math.Max(0, first));
    }

    /// <summary>
    /// Ende der Rechnung in 16 Bit ohne Vorzeichen. [ORIGINAL] 0x3FAC5–0x3FB2E: S wird als Wort ohne Vorzeichen durch 12
    /// geteilt, + 10 · (2 − Liga), · 85 (nur das untere Wort) / 100; nur nach oben auf 100 gekappt. Ein negatives S
    /// ergibt damit einen hohen Wert (S = −12, Oberliga: 53).
    /// </summary>
    public static int Finish(long score, int tier)
    {
        var s = (ushort)score;
        var t = (ushort)(s / 12 + 10 * (2 - tier));
        var r = (ushort)(t * 85) / 100;
        return Math.Min(r, 100);
    }

    /// <param name="randomSource">Zufallsstrom; ohne Angabe der des Spielstands. Anzeigen (Highscore) übergeben einen eigenen Strom,
    /// damit das Öffnen eines Bildschirms den Spielzustand nicht verändert.</param>
    public static int Compute(GameState game, Manager manager, Random? randomSource = null)
    {
        var club = game.Clubs[manager.ClubIndex];
        var finance = game.Finance(manager);
        var tier = (int)finance.CostLeague;
        var random = randomSource ?? game.FinanceRandom;

        var p = game.CurrentParticipation(club.Index);
        var s = CompetitionScore(tier, p.Championship, p.Cup, p.CupRound, p.Europe, p.EuropeRound);

        // [ORIGINAL] 0x3F928–0x3F98E: Ø-Platz aus dem Platzverlauf der laufenden Saison (Spur geld Teil 2, A036).
        s += 12L * (11 - game.SponsorAverageRank(club));

        var players = club.Players.ToList();
        long Value(Player pl) => game.PlayerValueOf(pl, club, PlayerValueKind.Price, random, manager.Level);
        var values = players.Select(Value).ToList();
        if (values.Count > 0)
        {
            s += AverageValueScore(values);
            var stars = StarCount(values, i => Value(players[i]), random);
            s += (long)SquadSetup.Between(random, 20, 50) * stars;
        }

        s += 3L * StadiumWorth(finance.Stadium);
        return Finish(s, tier);
    }
}

/// <summary>Teilnahme eines Vereins an den laufenden Wettbewerben, wie sie die Werbebewertung abfragt.</summary>
public readonly record struct CompetitionParticipation(bool Championship, bool Cup, int CupRound, bool Europe, int EuropeRound);

public sealed partial class GameState
{
    /// <summary>
    /// „Verein ist gerade im Wettbewerb“ für die Werbebewertung. [ORIGINAL] 0x110:0x61 (Datei 0x44A48) mit Flag 1:
    /// 1, sobald der Verein in den Paarungen der laufenden Runde steht (Meisterrunde Maske 0x40, DEB-Pokal 0x08 mit
    /// Runde [0x5CF3], Europapokal 0x20 mit Termin [0x7AF]).
    /// [APPROX] Abbildung auf das Remake: Meisterrunde = Paarungen der laufenden Playoff-Runde oder eine Relegationsrunde
    /// des Vereins (Liste 0x5D3B); Pokal = Paarungen der zuletzt ausgelosten Runde; Europapokal = Gruppe bzw. Endrunde,
    /// Termin = Zahl der schon gespielten Europapokaltermine. Wann das Original die Teilnahme nach dem Ausscheiden bzw.
    /// nach dem Finale zurücksetzt, ist nicht geprüft.
    /// </summary>
    public CompetitionParticipation CurrentParticipation(int club)
    {
        var championship = Playoffs is { Rounds.Count: > 0 } playoffs && playoffs.Rounds[^1].Any(s => s.Involves(club))
            || RelegationGroups.Any(g => g.Clubs.Contains(club));

        var cupRound = DebCup?.Rounds.Count ?? 0;
        var inCup = cupRound > 0 && DebCup!.Rounds[^1].Any(t => t.Involves(club));

        var europe = EuropeanCup;
        var inEurope = europe is not null
            && (europe.FinalRound is { } final ? final.Contains(club) : europe.Groups.Any(g => g.Contains(club)));
        var europeRound = europe is null ? 0 : SeasonCalendar.DaysOf(DayEvents.EuropeanCup).Count(d => d < Day);

        return new CompetitionParticipation(championship, inCup, cupRound, inEurope, europeRound);
    }
}

public sealed partial class Club
{
    /// <summary>
    /// Platzverlauf der Liga ([ORIGINAL] DGROUP 0x828B + 0x50 · Verein, 22 Byte = 44 Halbbytes, 0 = Platz 1; Feld i =
    /// Platz nach Spieltag i + 1). Geschrieben nach jedem Ligaspieltag (<see cref="GameState.RecordRankSlots"/>), geleert
    /// nur beim Spielstart; die Werte der Vorsaison bleiben stehen, bis der Spieltag sie überschreibt.
    /// </summary>
    private readonly int[] _rankSlots = new int[GameState.RankSlotCount];

    /// <summary>Gespeichertes Platzfeld (0 = Platz 1).</summary>
    public int StoredRankSlot(int slot) => _rankSlots[slot];

    internal void StoreRankSlot(int slot, int place) => _rankSlots[slot] = place;

    /// <summary>
    /// Tabellenfeld der Zuschauerformel (A046, Runde 14; 0 = Platz 1). [ORIGINAL] DGROUP 0x8288 + 0x50 · Verein, geschrieben mit
    /// dem Platzverlauf nach jedem Ligaspieltag (Datei 0x51115 in 0x51049), gelesen von der Zuschauerformel 0x3F185 (Datei
    /// 0x54608–0x5464A). Beim Spielstart 0 für alle Vereine, beim Saisonwechsel unverändert (Endstand der Vorsaison; HOK
    /// 0x03C5F + 0x31 + 0x50 · i: FILE29/30/01/12 alle 0, FILE14/17/23 vor dem 1. Spieltag der zweiten Saison die Plätze der
    /// Vorsaison). Bleibt beim Verein, auch nach Auf- oder Abstieg (Doppelte innerhalb einer Liga in FILE14/17/23).
    /// </summary>
    private int _tableRank;

    /// <summary>Gespeichertes Tabellenfeld (0 = Platz 1; siehe Feld).</summary>
    public int StoredTableRank => _tableRank;

    internal void StoreTableRank(int place) => _tableRank = place;
}

public sealed partial class GameState
{
    /// <summary>[ORIGINAL] Platzfelder je Verein: 22 Byte ab DGROUP 0x828B (Datei 0x436DD leert 0x16 Byte je Satz).</summary>
    public const int RankSlotCount = 44;

    /// <summary>
    /// Zahl der Ligaspieltage, deren Platzfelder schon in <see cref="Club"/> stehen (Spur geld Teil 2). Kleiner als
    /// <see cref="PlayedMatchDays"/> nur bis zum nächsten Tagesabschluss; größer nach dem Saisonwechsel (dann von vorn).
    /// </summary>
    public int RankSlotsRecorded { get; set; }

    /// <summary>
    /// Spieltagszähler einer Liga wie DGROUP 0x7A9 + Liga: gespielte Ligaspieltage + 1 (Ligen mit kürzerem Spielplan
    /// zählen nur ihre eigenen Spieltage).
    /// </summary>
    public int LeagueMatchDayCounter(League league) =>
        Math.Min(PlayedMatchDays, Schedules.TryGetValue(league, out var days) ? days.Count : 0) + 1;

    /// <summary>
    /// Platzfeld i eines Vereins ([ORIGINAL] 0x43BED; 0 = Platz 1). Felder gespielter Spieltage der laufenden Saison
    /// aus der Tabelle (wie <see cref="RankHistory"/>), übrige aus dem gespeicherten Verlauf (Vorsaison bzw. 0).
    /// Feld 44 und 45 liegen im Original hinter dem Verlauf im unteren Byte der Strafminuten (DGROUP 0x82A1, Halbbyte
    /// unten bzw. oben; <see cref="Club.LeaguePenaltyMinutes"/>); gelesen wird Feld 44 nach dem 44. Spieltag (N = 45).
    /// [APPROX] Felder ab 46 kommen im Remake nicht vor (höchstens 44 Ligaspieltage) und zählen als 0.
    /// </summary>
    public int RankSlot(Club club, int slot)
    {
        if (club.League is { } league && slot < LeagueMatchDayCounter(league) - 1 && slot < RankSlotCount)
            return LeagueTable.Compute(this, league, TableView.Gesamt, slot + 1).First(r => r.Club.Index == club.Index).Rank - 1;
        return slot switch
        {
            < RankSlotCount => club.StoredRankSlot(slot),
            RankSlotCount => club.LeaguePenaltyMinutes & 0x0F,
            RankSlotCount + 1 => (club.LeaguePenaltyMinutes >> 4) & 0x0F,
            _ => 0,
        };
    }

    /// <summary>
    /// Ø-Platz der Werbebewertung ([ORIGINAL] 0x3F915–0x3F98E, <see cref="SponsorRating.AverageRank"/>).
    /// [APPROX] Ohne Liga (Original: Liga des Managers, DGROUP 0x9746) zählt N = 1, also Ø 6.
    /// </summary>
    public int SponsorAverageRank(Club club)
    {
        var counter = club.League is { } league ? LeagueMatchDayCounter(league) : 1;
        return SponsorRating.AverageRank(counter, i => RankSlot(club, i));
    }

    /// <summary>
    /// Platzfelder der gespielten Ligaspieltage speichern ([ORIGINAL] Datei 0x51049 aus der Minutenschleife, je
    /// Ligaspieltag für alle Vereine der Liga). [APPROX] Das Remake schreibt sie im nächsten Tagesabschluss nach; gelesen
    /// werden die Felder der laufenden Saison ohnehin aus der Tabelle (<see cref="RankSlot"/>), gespeichert bleiben sie
    /// für die nächste Saison stehen.
    /// </summary>
    public void RecordRankSlots()
    {
        if (RankSlotsRecorded > PlayedMatchDays)
            RankSlotsRecorded = 0;
        foreach (var (league, days) in Schedules.OrderBy(s => s.Key))
        {
            var last = Math.Min(Math.Min(PlayedMatchDays, days.Count), RankSlotCount);
            for (var md = RankSlotsRecorded + 1; md <= last; md++)
                foreach (var row in LeagueTable.Compute(this, league, TableView.Gesamt, md))
                {
                    row.Club.StoreRankSlot(md - 1, row.Rank - 1);
                    row.Club.StoreTableRank(row.Rank - 1); // A046: DGROUP 0x8288, derselbe Schreiber (Datei 0x51115)
                }
        }
        RankSlotsRecorded = PlayedMatchDays;
    }
}
