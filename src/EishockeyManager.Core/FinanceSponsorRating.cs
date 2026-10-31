namespace EishockeyManager.Core;

/// <summary>
/// Bewertung eines Vereins für die Werbepartner, 0–100 (Spur finanzen-2, korrigiert in finanzen-werbung #132).
/// [ORIGINAL] em.exe Datei 0x3F833–0x3FB46 (Einsprung 0x100:0x57, Manager-Feld 0x97CE):
/// <code>
/// S  = (2 − StartLiga) · (80 · M + 170)
///    + 32 · (P · (n + 1) − 2), nur wenn n ≠ 0 (n = DEB-Pokal-Runde [0x5CF3])
///    + 47 · E · ([0x7AF] + 1)
///    + 12 · (11 − Mittel der Ligaplätze der letzten ≤ 5 Saisons, ohne Historie 6)
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

        // [APPROX] Ø-Rang: das Original zählt die Saisons [0x7A9 + Liga] − 5 … − 1 und überspringt Index 0 (0x3F936);
        // dessen Herkunft ist nicht sicher geprüft (#132). Hier die letzten ≤ 5 abgeschlossenen Saisons des Managers.
        var history = game.ManagerHistory(game.IndexOf(manager));
        var recent = history.TakeLast(5).ToList();
        var averageRank = recent.Count == 0 ? 6 : recent.Sum(h => h.Rank) / recent.Count;
        s += 12L * (11 - averageRank);

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
