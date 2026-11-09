using EishockeyManager.Formats;

namespace EishockeyManager.Core;

/// <summary>
/// Import eines Original-Spielstands, Teil 4c (Spur hok-import, Runde 14, E14-4 = A): DEB-Pokal, Europapokal und WM aus dem HOK
/// statt aus dem Nachspielen. Zuordnung aus em.exe (Feldkarte <c>docs/formate/spielstand-original.md</c>, Abschnitt
/// „Pokale und WM (Teil 4c)“):
/// <list type="bullet">
/// <item>[ORIGINAL] Plätze DGROUP 0x5CFB + 32 · Wettbewerb, Runde 0x5CF3 + Wettbewerb (Auslosung 0x341AB setzt die Runde 1,
///   0x3489D zählt sie hoch und schreibt die Sieger 2k → k, dahinter 0xFF). Wettbewerb 0 = DEB-Pokal; Platz 0x5D5B ff. führt die
///   K.-o.-Plätze der WM (Platzlogik wie <see cref="WorldCup"/>, FILE20).</item>
/// <item>[ORIGINAL] DEB-Pokal-Termin 0x34B76: 0x5C4B sind die Tore des laufenden Tages (nach dem Eintragen gelöscht, daher an jedem
///   Stand 0); 0x34B3E sichert sie vorher nach DGROUP 0xD014 + 32 · Spiel (Spiel = DGROUP 0xD074, 0–2), Siege je Platz DGROUP
///   0xD075. Nach Spiel 1 und 2 tauschen die Vereine einer Paarung ihre Plätze samt Siegen und gesicherter Tore; nach Spiel 3
///   kommt der Sieger auf Platz 2k, Spielzähler und Siege gehen auf 0, die nächste Runde wird ausgelost. Nach dem Endspiel steht
///   der Sieger in DGROUP 0x387, der Spielzähler wieder auf 2 und die Plätze wie vor dem dritten Termin. Eintrag 100 = nicht
///   ausgetragen.</item>
/// <item>[ORIGINAL] Gruppen von Europapokal und WM (0x30427): Teilnehmer DGROUP 0xCFF4 + 8 · Wettbewerb + 4 · Gruppe (Europapokal
///   Wettbewerb 2, Endrunde 3), Paarungen DGROUP 0xCED8 und Tore 0xCDB8 + 0x48 · Wettbewerb + 0x24 · Gruppe + 6 · Termin (je
///   Termin drei Paare; Europapokal-Gruppen mit einem Füllpaar). Termine gespielt: Europapokal DGROUP 0x7AF, WM 0x7AE.</item>
/// </list>
/// </summary>
public static partial class OriginalImport
{
    /// <summary>[ORIGINAL] Ergebnis „nicht auszutragen“ (0x34CE5, wie die Play-Offs).</summary>
    public const byte CupNotPlayed = 100;

    /// <summary>[ORIGINAL] Ende der Platzliste nach der Auslosung (0x3493F).</summary>
    public const byte CupSlotEnd = 0xFF;

    /// <summary>Plätze des Wettbewerbs <paramref name="competition"/> (DGROUP 0x5CFB + 32 · Wettbewerb).</summary>
    public static ReadOnlySpan<byte> CupSlots(OriginalSaveGame save, int competition) =>
        save.Block("5CFB").AsSpan(32 * competition, 32);

    /// <summary>Gesicherte Tore des DEB-Pokal-Spiels <paramref name="game"/> (0–2) je Platz (DGROUP 0xD014 + 32 · Spiel).</summary>
    public static ReadOnlySpan<byte> DebCupGoals(OriginalSaveGame save, int game) => save.Block("D014").AsSpan(32 * game, 32);

    /// <summary>Paarungen und Tore (Heim, Gast) einer Gruppe am Termin (DGROUP 0xCED8/0xCDB8 + 0x48 · Wettbewerb + 0x24 · Gruppe + 6 · Termin).</summary>
    public static IReadOnlyList<(int Home, int Away, int HomeGoals, int AwayGoals)> GroupDate(OriginalSaveGame save, int competition,
        int group, int date, int pairs)
    {
        var at = 0x48 * competition + 0x24 * group + 6 * date;
        var teams = save.Block("CED8");
        var goals = save.Block("CDB8");
        return Enumerable.Range(0, pairs)
            .Select(p => ((int)teams[at + 2 * p], (int)teams[at + 2 * p + 1], (int)goals[at + 2 * p], (int)goals[at + 2 * p + 1]))
            .ToList();
    }

    /// <summary>Teilnehmer DGROUP 0xCFF4 + 8 · Wettbewerb (im HOK ab 0xCFFC, also Wettbewerb 1–3).</summary>
    public static ReadOnlySpan<byte> GroupTeams(OriginalSaveGame save, int competition) =>
        save.Block("CFFC").AsSpan(8 * (competition - 1), 8);
}

public sealed partial class GameState
{
    /// <summary>Teil 4c: Pokale und WM aus dem HOK; Wettbewerbe, die das Remake nicht führt (Spielmodus), entfallen.</summary>
    private void ApplyCups(OriginalSaveGame save, int[] clubOf)
    {
        DebCup?.ImportOriginal(save, clubOf);
        EuropeanCup?.ImportOriginal(save, clubOf, Clubs);
        WorldCup?.ImportOriginal(save, clubOf, Clubs);
    }
}

public sealed partial class DebCup
{
    /// <summary>
    /// Laufende Runde aus dem HOK (siehe <see cref="OriginalImport"/>, Teil 4c). Spiele der Runde mit Toren und Marken
    /// (Heimbyte div 30, <see cref="CupTie.Marks"/>), Heimrecht nach Spielnummer.
    /// <list type="bullet">
    /// <item>[APPROX] Frühere Runden führt das Original nicht (nur die laufende); sie bleiben aus dem Nachspielen.</item>
    /// <item>[APPROX] <see cref="Draw"/> = Plätze mit Heimrecht nach dem Tausch 0x347EF (die Ziehungsfolge davor sichert das HOK nicht).</item>
    /// </list>
    /// </summary>
    internal void ImportOriginal(OriginalSaveGame save, int[] clubOf)
    {
        int round = save.Block("5CF3")[0];
        if (round is < 1 or > RoundCount)
            return;
        var slots = OriginalImport.CupSlots(save, 0);
        var teams = Teams >> (round - 1);
        for (var i = 0; i < teams; i++)
            if (slots[i] >= clubOf.Length)
                return;
        int counter = save.Block("D074")[0];
        var wins = save.Block("D075");
        // Nach dem Endspiel: Spielzähler 2, Siege gelöscht (0x34EA7, 0x34EFC). Mitten in einer Runde zählen die Siege die Spiele.
        var finished = round == RoundCount && counter == 2 && wins.All(w => w == 0);
        var swapped = !finished && counter % 2 == 1; // jedes Spiel außer dem dritten tauscht die Plätze
        var played = finished ? Enumerable.Range(0, DatesPerRound).Count(g => OriginalImport.DebCupGoals(save, g)[0] != OriginalImport.CupNotPlayed) : counter;

        var ties = new List<CupTie>();
        for (var k = 0; k < teams / 2; k++)
        {
            var (left, right) = swapped ? (2 * k + 1, 2 * k) : (2 * k, 2 * k + 1); // Platz des Vereins mit Heimrecht in Spiel 1
            var tie = new CupTie(clubOf[slots[left]], clubOf[slots[right]]);
            for (var g = 0; g < played; g++)
            {
                var goals = OriginalImport.DebCupGoals(save, g);
                var (home, away) = g % 2 == 0 ? (left, right) : (right, left);
                if (goals[home] == OriginalImport.CupNotPlayed)
                    break;
                var marks = Math.Min(goals[home] / 30, CupMatch.MaxMarks);
                var (hg, ag) = (goals[home] % 30, (int)goals[away]);
                var (h, a) = CupTie.Pairing(tie, g);
                int? penalty = marks == 1 ? (hg >= ag ? h : a) : null;
                tie.Add(new KnockoutMatch(DayOf(round - 1, g), h, a, hg, ag, penalty), marks);
            }
            ties.Add(tie);
        }

        while (_rounds.Count >= round)
            _rounds.RemoveAt(_rounds.Count - 1);
        _rounds.Add(ties);
        _draw.Clear();
        _draw.AddRange(ties.SelectMany(t => new[] { t.First, t.Second }));
        // [ORIGINAL] DEB-Pokalsieger DGROUP 0x387 (0x34ED6) nach dem Endspiel (R14HI-04). Vor dem Endspiel hält 0x387 den Vorjahressieger
        // (FILE05: EV Landshut), den das Remake nur in History führt (Feldkarte N): Winner bleibt null, sonst stünde der Pokal still.
        int holder = save.Block("0387")[0];
        Winner = !finished ? null : holder < clubOf.Length ? clubOf[holder] : ties[0].Winner;
    }
}

public sealed partial class EuropeanCup
{
    /// <summary>
    /// Gruppen, Endrunde und Ergebnisse aus dem HOK (Teil 4c). [ORIGINAL] Teilnehmer DGROUP 0xD004 (Gruppe 1 = 0–3, Gruppe 2 =
    /// 4–7), Endrunde 0xD00C, Paarungen und Tore ab 0xCED8/0xCDB8 + 0x90 (Endrunde + 0xD8), Termine 0x7AF (FILE36: 7, FILE38: 12).
    /// [APPROX] Deutsche Teilnehmer = Vereine der Auslosung unter 64 in der Folge von DGROUP 0xA776 (die Zahl 0xA775 sichert das
    /// HOK nicht). Europacupsieger nach dem zwölften Termin aus DGROUP 0x388 (R14HI-04).
    /// </summary>
    internal void ImportOriginal(OriginalSaveGame save, int[] clubOf, IReadOnlyList<Club> clubs)
    {
        var teams = OriginalImport.GroupTeams(save, 2).ToArray();
        if (teams.Any(t => t >= clubOf.Length) || teams.Distinct().Count() != Teams)
            return; // vor der ersten Auslosung (Spielmodus ohne Europapokal) bleibt der Stand des Nachspielens
        int dates = save.Block("07A9")[6];
        var days = SeasonCalendar.DaysOf(DayEvents.EuropeanCup);

        _draw.Clear();
        foreach (var t in teams)
            _draw.Add(clubOf[t]);
        var entrants = save.Block("A776").Where(t => t < FirstOpponent && teams.Contains(t)).Distinct().Select(t => clubOf[t]).ToList();
        GermanEntrants = entrants;
        _groups.Clear();
        _groups.Add(new CupGroup(1, _draw.Take(GroupSize).ToList()));
        _groups.Add(new CupGroup(2, _draw.Skip(GroupSize).ToList()));
        FinalRound = null;
        Winner = null;
        for (var d = 0; d < Math.Min(dates, GroupDates); d++)
            for (var g = 0; g < 2; g++)
                AddResults(_groups[g], OriginalImport.GroupDate(save, 2, g, d, 2), d, days[d], clubOf);
        if (dates < GroupDates)
            return;
        var final = OriginalImport.GroupTeams(save, 3);
        FinalRound = new CupGroup(0, final[..GroupSize].ToArray().Select(t => clubOf[t]).ToList());
        for (var d = GroupDates; d < Math.Min(dates, 2 * GroupDates); d++)
            AddResults(FinalRound, OriginalImport.GroupDate(save, 3, 0, d - GroupDates, 2), d - GroupDates, days[d], clubOf);
        // [ORIGINAL] Europacupsieger DGROUP 0x388 (0x314D6: Platz 1 der Endrunde nach dem zwölften Termin; FILE37A/FILE38: Malmö FF). Vorher
        // Vorjahressieger (FILE05: 64 = Klagenfurter AC), nur in History (Feldkarte N): Winner bleibt null (R14HI-04).
        int holder = save.Block("0388")[0];
        if (dates >= 2 * GroupDates)
            Winner = holder < clubOf.Length ? clubOf[holder] : FinalRound.Table(clubs)[0].Club.Index;
    }

    /// <summary>Ergebnisse eines Termins; die Paarungen des HOK müssen dem Spielplan der Gruppe entsprechen.</summary>
    internal static void AddResults(CupGroup group, IReadOnlyList<(int Home, int Away, int HomeGoals, int AwayGoals)> results, int round,
        int day, int[] clubOf)
    {
        var plan = group.Pairings(round);
        for (var p = 0; p < plan.Count; p++)
        {
            var r = results[p];
            if (r.Home >= clubOf.Length || r.Away >= clubOf.Length || (clubOf[r.Home], clubOf[r.Away]) != plan[p])
                throw new InvalidDataException($"Gruppe {group.Number}, Termin {round + 1}: Paarung {r.Home}–{r.Away} nicht im Spielplan.");
            group.Results.Add(new KnockoutMatch(day, plan[p].Home, plan[p].Away, r.HomeGoals, r.AwayGoals));
        }
    }
}

public sealed partial class WorldCup
{
    /// <summary>
    /// Reihenfolge, Gruppen, Ergebnisse und K.-o.-Plätze aus dem HOK (Teil 4c). [ORIGINAL] Gruppe g = Plätze 52 + 6g … (Satz, Regel 1;
    /// die Tausche 0x35EDD vertauschen die Sätze), Paarungen und Tore ab DGROUP 0xCED8/0xCDB8 + 0x24 · g, Termine 0x7AE, K.-o.-Plätze
    /// DGROUP 0x5D5B (24 Plätze wie <c>_slots</c>; FILE20: nach Halbfinale).
    /// [APPROX] Tore der K.-o.-Spiele sichert das HOK nicht (0x5C4B ist nach jedem Termin leer): Sieger 1:0, Marke 0.
    /// </summary>
    internal void ImportOriginal(OriginalSaveGame save, int[] clubOf, IReadOnlyList<Club> clubs)
    {
        int dates = save.Block("07A9")[5];
        var days = SeasonCalendar.DaysOf(DayEvents.WorldCup);
        var order = Enumerable.Range(NationalTeams.First, Teams).Select(s => clubOf[s]).ToList();
        if (order.Distinct().Count() != Teams || order.Any(c => c is < NationalTeams.First or > NationalTeams.Last))
            return;
        _order.Clear();
        _order.AddRange(order);
        _groups.Clear();
        _groups.Add(new CupGroup(1, _order.Take(GroupSize).ToList()));
        _groups.Add(new CupGroup(2, _order.Skip(GroupSize).ToList()));
        _knockout.Clear();
        _knockoutMarks.Clear();
        Array.Fill(_slots, null);
        Winner = null;
        Ranking = [];
        for (var d = 0; d < Math.Min(dates, GroupDates); d++)
            for (var g = 0; g < 2; g++)
                EuropeanCup.AddResults(_groups[g], OriginalImport.GroupDate(save, 0, g, d, GroupSize / 2), d, days[d], clubOf);
        if (dates < GroupDates)
            return;

        // Viertelfinale wie nach dem fünften Termin, dann je K.-o.-Termin die gesicherten Plätze (Sieger vorn).
        var a = _groups[0].Table(clubs).Select(r => r.Club.Index).ToList();
        var b = _groups[1].Table(clubs).Select(r => r.Club.Index).ToList();
        for (var k = 0; k < 4; k++)
        {
            _slots[2 * k] = a[k];
            _slots[2 * k + 1] = b[3 - k];
        }
        var saved = OriginalImport.CupSlots(save, 3);
        for (var stage = 0; stage < Math.Min(dates - GroupDates, 3); stage++)
        {
            var pairs = KnockoutPairings(stage);
            var games = new List<KnockoutMatch>();
            for (var k = 0; k < pairs.Count; k++)
            {
                var (home, away) = pairs[k];
                var winner = saved[8 * stage + 2 * k] < clubOf.Length ? clubOf[saved[8 * stage + 2 * k]] : -1;
                if (winner != home && winner != away)
                    throw new InvalidDataException($"WM-K.-o. Termin {stage + 1}, Spiel {k + 1}: Sieger nicht in der Paarung.");
                games.Add(new KnockoutMatch(days[GroupDates + stage], home, away, winner == home ? 1 : 0, winner == home ? 0 : 1));
            }
            _knockout.Add(games);
            _knockoutMarks.Add(games.Select(_ => 0).ToList());
            for (var k = 0; k < 4; k++)
            {
                _slots[8 * stage + 2 * k] = games[k].Winner;
                _slots[8 * stage + 2 * k + 1] = games[k].Loser;
            }
            if (stage < 2)
                for (var i = 8 * stage; i < 8 * stage + 8; i++)
                    _slots[Advance[i] - 1] = _slots[i];
            else
            {
                Winner = games[0].Winner;
                Ranking = Enumerable.Range(16, 8).Select(i => _slots[i]!.Value)
                    .Concat(new[] { a[4], b[4], a[5], b[5] }).ToList();
            }
        }
    }
}
