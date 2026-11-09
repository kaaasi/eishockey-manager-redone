using EishockeyManager.Formats;

namespace EishockeyManager.Core;

/// <summary>
/// Import eines Original-Spielstands, Teil 4d (Review R14HI-01, PR #670): Play-offs und Aufstiegsrunden aus dem HOK statt aus
/// dem Nachspielen. Zuordnung aus em.exe (Overlay 0xE8; Datei-Offsets):
/// <list type="bullet">
/// <item>[ORIGINAL] Play-off-Plätze DGROUP 0x5D3B (= <c>5CFB</c>+0x40, 24 Plätze): Setzung 0x319AC–0x319D4 schreibt Platz 2i = 0x5CCB[i]
///   (besser Platzierter), 2i + 1 = 0x5CCB[7 − i]; Viertelfinale Plätze 0–7, Halbfinale 8–11, Endspiel 12–13 (Tabelle DGROUP 0x144:
///   Sieger von Platz 0 → 8, 2 → 10, 4 → 9, 6 → 11, 8 → 12, 10 → 13; Verlierer → Platz 23). Runde DGROUP 0x7B0 (= <c>07A9</c>+7), Spiel
///   der Runde DGROUP 0xCCA3 (0–4; 5 nach dem Endspiel), Anfang der Runde DGROUP 0xCFFA (0, 8, 12).</item>
/// <item>[ORIGINAL] Spiele DGROUP 0xCCB2 + 14 · Spiel + Platz (0x3173A–0x317AE), Tore des Heimbytes + 30 je Marke (Schleife
///   0x31622–0x31631 zieht sie vor der Wertung ab), 100 = nicht ausgetragen (0x31800). Sieger nach Toren; Gleichstand zählt für den
///   Gast (0x3166E „jnc“ → 0xCCA5). Siege DGROUP 0xCCA4 + Platz. Siegbyte 0x14 (0x3168B–0x316BF, R14HN-02): Hat
///   beim fünften Termin (<c>CCA3</c> = 4) der Gast dieses Termins mehr als zwei Siege, stehen seine Siege danach als 0x14, und
///   0x316C9 tauscht die Paarung – auch wenn die Serie schon vorher entschieden war (FILE22: Endspiel 3:0 für Köln, Spiele 4/5 = 100,
///   <c>CCA4[12]</c> = 0x14; ebenso Viertelfinale 0/1). Der Sieger steht damit auf Platz 2k. Nach jedem der ersten vier Spiele tauschen Heim und Gast samt Siegen und bisherigen Spielen (0x31505, Aufruf
///   0x316C9); beim Rundenwechsel stellt 0x31C3A–0x31C46 die Setzreihenfolge wieder her. Heimrecht in Spiel 1, 3, 5 beim Platz 2k.</item>
/// <item>[ORIGINAL] Aufstiegsrunden = Wettbewerb 1 der Gruppentabellen (0x30852, Aufruf 0x38691): Teilnehmer DGROUP 0xCFF4 + 8 + 4 · Gruppe
///   (<c>CFFC</c>; Gruppe 0 Bundesliga/2. Liga, 1 = 2. Liga/Oberliga: die zwei Letzten der oberen, dann die zwei Ersten der unteren
///   Liga, 0x30511), Paarungen <c>CED8</c> und Tore <c>CDB8</c> + 0x48 + 0x24 · Gruppe + 6 · Termin (zwei Paare, drittes Paar
///   Füllwert), Punkte <c>CD58</c>, Tore <c>CD28</c>, Gegentore <c>CCF8</c> und Platzliste <c>CD88</c> je + 12 + 6 · Gruppe + Platz.</item>
/// </list>
/// [APPROX] Die Marken (+30) der Play-off-Spiele führt <see cref="PlayoffSeries"/> nicht; an FILE22/FILE44 stehen sie auch bei
/// klaren Ergebnissen (4:2). Sie werden abgezogen und nicht gedeutet. Einen Zähler der gespielten Aufstiegsrunden-Termine führt das
/// HOK nicht; er folgt aus der Punktsumme der Gruppe (zwei Punkte je Spiel).
/// </summary>
public static partial class OriginalImport
{
    /// <summary>Zahl der Play-off-Plätze in DGROUP 0x5D3B (Viertelfinale, Halbfinale, Endspiel).</summary>
    public const int PlayoffSlotCount = 14;

    /// <summary>Siegbyte des Gastes des fünften Termins mit mehr als zwei Siegen (0x316BF; auch bei vorher entschiedener Serie).</summary>
    public const byte PlayoffAwayWinsMark = 0x14;

    /// <summary>Play-off-Plätze DGROUP 0x5D3B ff. (<c>5CFB</c> + 0x40, 24 Byte).</summary>
    public static ReadOnlySpan<byte> PlayoffSlots(OriginalSaveGame save) => save.Block("5CFB").AsSpan(0x40, 24);

    /// <summary>Play-off-Runde DGROUP 0x7B0 (0–2).</summary>
    public static int PlayoffRound(OriginalSaveGame save) => save.Block("07A9")[7];

    /// <summary>Spiel der laufenden Play-off-Runde DGROUP 0xCCA3 (0–4; 5 nach dem Endspiel).</summary>
    public static int PlayoffGame(OriginalSaveGame save) => save.Block("CCA3")[0];

    /// <summary>Tore eines Play-off-Spiels: (Platz 2k, Platz 2k + 1) aus DGROUP 0xCCB2 + 14 · Spiel + Platz, Marken abgezogen; null = nicht ausgetragen.</summary>
    public static (int First, int Second)? PlayoffGoals(OriginalSaveGame save, int game, int slot)
    {
        var results = save.Block("CCB2");
        var (a, b) = (results[PlayoffSlotCount * game + slot], results[PlayoffSlotCount * game + slot + 1]);
        return a >= CupNotPlayed || b >= CupNotPlayed ? null : (a % 30, b % 30);
    }
}

public sealed partial class GameState
{
    /// <summary>Teil 4d: Play-offs und Aufstiegsrunden aus dem HOK; ohne Vereinssaison (nur Weltkugel) oder vor dem Ende der Hauptrunde bleibt der Stand des Nachspielens.</summary>
    private void ApplyPostseason(OriginalSaveGame save, int[] clubOf)
    {
        if (WorldOnly || Playoffs is null)
            return;
        var finalTable = save.Block("5CCB");
        var seeds = Enumerable.Range(0, Playoffs.Teams).Select(i => clubOf[finalTable[i]]).ToList();
        Playoffs = Playoffs.ImportOriginal(save, clubOf, seeds);
        // [ORIGINAL] Deutscher Meister DGROUP 0xD0E6 (0x31CBF) – nach dem Endspiel muss er der Sieger der Serie sein.
        int titleHolder = save.Block("D0E6")[0];
        if (Playoffs.Champion is { } decided && (titleHolder >= clubOf.Length || clubOf[titleHolder] != decided))
            throw new InvalidDataException($"Deutscher Meister (DGROUP 0xD0E6 = {titleHolder}) passt nicht zum Endspiel.");
        PlayoffResults.Clear();
        PlayoffResults.AddRange(Playoffs.Rounds.SelectMany(r => r).SelectMany(s => s.Games).OrderBy(g => g.Day));

        RelegationGroups.Clear();
        var teams = OriginalImport.GroupTeams(save, 1).ToArray();
        if (teams.Any(t => t >= 3 * ClubsPerLeague) || teams.Distinct().Count() != teams.Length)
            throw new InvalidDataException("Aufstiegsrunden (DGROUP 0xCFFC) ohne gültige Teilnehmer nach dem Ende der Hauptrunde.");
        for (var g = 0; g < 2; g++)
            RelegationGroups.Add(RelegationGroup.ImportOriginal(save, clubOf, g == 0 ? League.Bundesliga : League.ZweiteLiga, g));

        // Vereinssaison schon beendet (13. April im Nachspielen): Titel und Auf-/Absteiger im Saisonrückblick aus den übernommenen
        // Wettbewerben neu eintragen (SeasonFlow.FinishClubSeason hatte die des Nachspielens eingetragen).
        if (CurrentRecord is { } record)
        {
            if (Playoffs.Champion is { } champion)
                record.Titles[Title.GermanChampion] = champion;
            else
                record.Titles.Remove(Title.GermanChampion);
            foreach (var group in RelegationGroups)
                RecordRelegationGroup(record, group, group.Table(Clubs).Select(r => r.Club.Index).ToList());
        }
    }
}

public sealed partial class Playoffs
{
    /// <summary>
    /// Play-offs aus dem HOK (siehe <see cref="OriginalImport"/>, Teil 4d): Runden 0 … DGROUP 0x7B0 aus den Plätzen 0x5D3B, Spiele aus
    /// 0xCCB2. Paarung (2k, 2k + 1) steht in Setzreihenfolge; in der laufenden Runde ist sie nach ungerader Spielzahl getauscht
    /// (0x31505), im fertigen Endspiel bei Siegbyte 0x14 (Gast des fünften Termins mit drei Siegen).
    /// </summary>
    internal static Playoffs ImportOriginal(OriginalSaveGame save, int[] clubOf, IReadOnlyList<int> seeds)
    {
        var slots = OriginalImport.PlayoffSlots(save);
        var wins = save.Block("CCA4");
        var round = OriginalImport.PlayoffRound(save);
        var game = OriginalImport.PlayoffGame(save);
        if (round > 2 || game > DatesPerRound)
            throw new InvalidDataException($"Play-off-Runde {round}, Spiel {game} (DGROUP 0x7B0/0xCCA3) ungültig.");
        var days = SeasonCalendar.DaysOf(DayEvents.Playoffs);
        var playoffs = new Playoffs(seeds);
        var expectedFirst = playoffs._rounds[0].Select(s => (s.HomeClub, s.AwayClub)).ToList();
        playoffs._rounds.Clear();
        // Plätze der Paarungen je Runde in Platzfolge wie Playoffs.Rounds; Prepare paart eine noch offene Runde nach Tabelle DGROUP
        // 0x144 wie das Original (1–8 gegen 3–6, 2–7 gegen 4–5, Heimrecht beim kleineren Platz; #676).
        int[][] pairSlots = [[0, 2, 4, 6], [8, 10], [12]];
        for (var r = 0; r <= round; r++)
        {
            var current = r == round && game < DatesPerRound;
            var series = new List<PlayoffSeries>();
            foreach (var p in pairSlots[r])
            {
                if (slots[p] >= clubOf.Length || slots[p + 1] >= clubOf.Length)
                    throw new InvalidDataException($"Play-off-Platz {p} (DGROUP 0x5D3B) ohne Verein.");
                var swapped = current ? game % 2 == 1 : r == 2 && wins[p] == OriginalImport.PlayoffAwayWinsMark;
                var (first, second) = swapped ? (clubOf[slots[p + 1]], clubOf[slots[p]]) : (clubOf[slots[p]], clubOf[slots[p + 1]]);
                var s = new PlayoffSeries(first, second);
                var limit = current ? game : DatesPerRound;
                for (var g = 0; g < limit; g++)
                {
                    if (OriginalImport.PlayoffGoals(save, g, p) is not { } goals)
                        break;
                    var (a, b) = swapped ? (goals.Second, goals.First) : (goals.First, goals.Second);
                    var (home, away, hg, ag) = g % 2 == 0 ? (first, second, a, b) : (second, first, b, a);
                    // Gleichstand gibt es nach dem Penaltyschießen nicht (Treffer stehen im Ergebnis); zur Sicherheit wie 0x3166E: Gast.
                    s.Games.Add(new KnockoutMatch(days[DatesPerRound * r + g], home, away, hg, ag, hg == ag ? away : null));
                }
                series.Add(s);
            }
            if (r == 0 && !series.Select(s => (s.HomeClub, s.AwayClub)).Order().SequenceEqual(expectedFirst.Order()))
                throw new InvalidDataException("Play-off-Viertelfinale (DGROUP 0x5D3B) passt nicht zur Abschlusstabelle 0x5CCB.");
            playoffs._rounds.Add(series);
        }
        return playoffs;
    }
}

public sealed partial class RelegationGroup
{
    private RelegationGroup(League upper, IReadOnlyList<int> clubs, IReadOnlyList<MatchDay> rounds)
    {
        Upper = upper;
        Clubs = clubs;
        Rounds = rounds;
    }

    /// <summary>
    /// Aufstiegsrunde <paramref name="group"/> (0 Bundesliga/2. Liga, 1 = 2. Liga/Oberliga) aus dem HOK (Teil 4d): Teilnehmer
    /// DGROUP 0xCFFC + 4 · Gruppe, Paarungen und Tore je Termin, gespielte Termine aus der Punktsumme (zwei Punkte je Spiel).
    /// </summary>
    internal static RelegationGroup ImportOriginal(OriginalSaveGame save, int[] clubOf, League upper, int group)
    {
        var teams = OriginalImport.GroupTeams(save, 1).Slice(4 * group, 4).ToArray().Select(t => clubOf[t]).ToList();
        var rounds = new List<MatchDay>();
        var results = new List<MatchResult>();
        var points = save.Block("CD58").AsSpan(12 + 6 * group, 4);
        var sum = points[0] + points[1] + points[2] + points[3];
        if (sum % 4 != 0 || sum / 4 > 6)
            throw new InvalidDataException($"Aufstiegsrunde {group + 1}: Punktsumme {sum} (DGROUP 0xCD58) passt zu keiner Zahl gespielter Termine.");
        var played = sum / 4;
        for (var d = 0; d < 6; d++)
        {
            var date = OriginalImport.GroupDate(save, 1, group, d, 2);
            var pairs = new List<(int Home, int Away)>();
            foreach (var (home, away, hg, ag) in date)
            {
                if (home >= clubOf.Length || away >= clubOf.Length || !teams.Contains(clubOf[home]) || !teams.Contains(clubOf[away]))
                    throw new InvalidDataException($"Aufstiegsrunde {group + 1}, Termin {d + 1}: Paarung {home}–{away} nicht in der Gruppe.");
                pairs.Add((clubOf[home], clubOf[away]));
                if (d < played)
                    results.Add(new MatchResult(upper, d + 1, clubOf[home], clubOf[away], hg, ag));
            }
            rounds.Add(new MatchDay(d + 1, pairs));
        }
        var imported = new RelegationGroup(upper, teams, rounds);
        imported.Results.AddRange(results);
        imported.PlayedRounds = played;
        return imported;
    }
}
