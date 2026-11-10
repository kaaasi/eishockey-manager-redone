using EishockeyManager.Formats;

namespace EishockeyManager.Core;

public sealed partial class GameState
{
    /// <summary>Vorjahressieger des DEB-Pokals beim Spielstart. [ORIGINAL] FILE01/FILE05: DGROUP 0x387 = EV LANDSHUT.</summary>
    public const string InitialDebCupWinnerName = "EV LANDSHUT";

    private int InitialDebCupWinner => Clubs.FirstOrDefault(c => c.Name == InitialDebCupWinnerName)?.Index ?? 0;

    /// <summary>DEB-Pokalrunde des Vereins (1-basiert, Runde der letzten Paarung; 1 ohne Pokal) für Managersatz +0xE6 [APPROX].</summary>
    private int DebCupRoundOf(int club)
    {
        if (DebCup is not { } cup)
            return 1;
        for (var r = cup.Rounds.Count - 1; r >= 0; r--)
            if (cup.Rounds[r].Any(t => t.Involves(club)))
                return r + 1;
        return 1;
    }

    /// <summary>
    /// Titelträger 0x387 (DEB-Pokal), 0xD0E6 (Meister), 0x388 (Europapokal): nach dem Endspiel der Sieger, vorher der Vorjahressieger
    /// (<see cref="History"/>, zu Spielbeginn die Werte des Originals); Pokale, Europapokal, WM, Play-offs und Aufstiegsrunden.
    /// </summary>
    private void WriteCupsAndPostseason(OriginalSaveWriter w, int[] slotOf)
    {
        var last = History.Count > 0 ? History[^1] : null;
        int Holder(Title title, int initial) => last?.Titles.GetValueOrDefault(title, initial) ?? initial;
        w.Block("0387")[0] = (byte)slotOf[DebCup?.Winner ?? Holder(Title.CupWinner, InitialDebCupWinner)];
        w.Block("D0E6")[0] = (byte)slotOf[Playoffs?.Champion ?? Holder(Title.GermanChampion, EuropeanCup.InitialChampion)];
        w.Block("0388")[0] = (byte)slotOf[EuropeanCup?.Winner ?? Holder(Title.EuropeanCupWinner, EuropeanCup.InitialWinner)];
        // [APPROX] DGROUP 0xCFF8 (4 Byte): +2 Anfang der laufenden Play-off-Runde, sonst 0xFF (FILE01: 00 00 FF 00).
        w.Block("CFF8")[2] = 0xFF;
        // [ORIGINAL] Unbelegte K.-o.-Plätze der WM/Endrunde 0x5D5B ff. stehen auf 0xFF (FILE01).
        w.Block("5CFB").AsSpan(0x60, 32).Fill(0xFF);
        // [APPROX] Europapokal: Füllpaar je Termin (Sätze 61/63) und Platzliste 4/5 hinter den vier Teilnehmern (FILE01–FILE41).
        for (var g = 0; g < 2; g++)
            for (var d = 0; d < 6; d++)
            {
                w.Block("CED8")[0x90 + 0x24 * g + 6 * d + 4] = 61;
                w.Block("CED8")[0x90 + 0x24 * g + 6 * d + 5] = 63;
            }
        w.Block("5CF3")[3] = 3; // [APPROX] DGROUP 0x5CF6: an allen Ständen 3
        foreach (var at in new[] { 24, 30 })
        {
            w.Block("CD88")[at + 4] = 4;
            w.Block("CD88")[at + 5] = 5;
        }
        w.Block("07A9")[4] = 1;
        DebCup?.ExportOriginal(w, slotOf, this);
        EuropeanCup?.ExportOriginal(w, slotOf, Clubs);
        WorldCup?.ExportOriginal(w, slotOf, Clubs);
        if (!WorldOnly && Playoffs is { } playoffs)
            playoffs.ExportOriginal(w, slotOf);
        for (var g = 0; g < Math.Min(RelegationGroups.Count, 2); g++)
            RelegationGroups[g].ExportOriginal(w, slotOf, g, Clubs);
        // [ORIGINAL] Deutsche Europapokal-Teilnehmer DGROUP 0xA776 (Liste, Rest 0) und 0xA763 (Liste, Rest 0x80; FILE01).
        var entrants = EuropeanCup?.GermanEntrants is { Count: > 0 } e ? e : OriginalExportEntrants();
        var a776 = w.Block("A776");
        var a763 = w.Block("A763");
        a763.AsSpan().Fill(0x80);
        for (var i = 0; i < entrants.Count && i < a776.Length; i++)
            a776[i] = a763[i] = (byte)slotOf[entrants[i]];
    }

    private IReadOnlyList<int> OriginalExportEntrants() => EuropeanCup.Entrants(this).Where(c => c < EuropeanCup.FirstOpponent).ToList();

    /// <summary>
    /// Nationalkader (Kaderplätze Satz 5), Bundestrainer (Satz 5 +0x14 Land, +0xEB Manager), Nationalteam 0x6AC, Taktik des Satzes 5,
    /// Freundschaftsspiele 0xD0C1 ff., „automatisch aufstellen“ 0x37F/0x380. Umkehrung von <c>ApplyNationalSquad</c>.
    /// [APPROX] Nominierte ohne Poolplatz (Spieler der Computervereine) führt das Original nicht einzeln; sie entfallen.
    /// </summary>
    private void WriteNational(OriginalSaveWriter w, int[] slotOf)
    {
        var nation = NationalCoach?.Nation ?? NationalTeams.Germany;
        w.Block("06AC")[0] = (byte)slotOf[nation];
        // [ORIGINAL] DGROUP 0x26C: Kosten eines Auswärtsspiels je Nationalteam in Satzfolge 52–63 (Drittel-DM; FILE01).
        foreach (var team in Enumerable.Range(NationalTeams.First, WorldCup.Teams))
            OriginalExport.Write32(w.Block("026C"), 4 * (slotOf[team] - NationalTeams.First),
                OriginalExport.Thirds(NationalCoach.FriendlyCost(team, home: false)));
        var rec = w.Block("965B");
        var national = OriginalImport.NationalRecord * OriginalSaveGame.ManagerRecordLength;
        var tactics = NationalCoach?.Tactics ?? _vacantNationalTactics;
        if (tactics is not null)
            WriteTacticValues(w, tactics, OriginalImport.NationalRecord);
        w.Block("0375")[2 * OriginalImport.NationalRecord + 1] = (byte)(_nationalAutoLineupSaved ? 1 : 0);
        w.Block("0375")[2 * OriginalImport.NationalRecord] = 1; // [APPROX] ohne Bundestrainer 1 (FILE01)
        if (NationalCoach is not { } coach)
            return;
        rec[national + 0x14] = (byte)slotOf[coach.Nation];
        // [ORIGINAL] Satz 5 +0x00: Name des Bundestrainers (FILE11, FILE18–FILE21, FILE47–FILE49A).
        if (coach.Manager < Managers.Count)
            OriginalSaveWriter.Name(Managers[coach.Manager].Name).CopyTo(rec, national);
        rec[national + 0xEB] = (byte)coach.Manager;
        w.Block("0375")[2 * OriginalImport.NationalRecord] = (byte)(coach.AutoLineupEnabled ? 1 : 0);

        var friendlies = coach.Friendlies.Take(NationalCoach.MaxFriendlies).ToList();
        w.Block("D0C1")[0] = (byte)friendlies.Count;
        for (var i = 0; i < friendlies.Count && i < 8; i++)
        {
            w.Block("D0DB")[i] = (byte)slotOf[friendlies[i].Home];
            w.Block("D0D3")[i] = (byte)slotOf[friendlies[i].Away];
            w.Block("D0CB")[i] = OriginalExport.Byte(friendlies[i].HomeGoals);
            w.Block("D0C3")[i] = OriginalExport.Byte(friendlies[i].AwayGoals);
        }

        var indexOf = PoolIndices();
        var squad = w.Block("5DC3");
        var pool = w.Block("far:8253");
        var k = 0;
        foreach (var player in coach.Squad.Select(s => s.Player))
        {
            if (k >= NationalCoach.MaxSquad || !indexOf.TryGetValue(player, out var index))
                continue;
            var s = squad.AsSpan(OriginalImport.NationalRecord * 0x618 + k * OriginalSaveGame.SquadSlotLength, OriginalSaveGame.SquadSlotLength);
            s[0x0F] = (byte)index;
            s[0x10] = OriginalExport.Byte(player.Condition);
            s[0x11] = OriginalExport.Byte(player.Technique);
            s[0x12] = OriginalExport.Byte(player.Form);
            s[0x13] = OriginalExport.Byte(player.Strain);
            s[0x0A] = (byte)(coach.IsDressed(player) || coach.Formation.Contains(player) ? 1 : 0);
            if (coach.Formation.FindSlot(player) is { } place)
                s[0x15] = (byte)((place.Role == PlayerRole.Goalkeeper ? 1 : place.Block + 1) | (place.Role == PlayerRole.Forward ? 0x80 : 0));
            // [APPROX] Pool +0x1D eines Nominierten zeigt das Land (FILE18).
            pool[index * OriginalSaveGame.PoolRecordLength + 0x1D] = (byte)slotOf[coach.Nation];
            k++;
        }
    }

    /// <summary>
    /// Nachrichten 0xD294 + 0x71 · k (24 Sätze): +0 Bits 0–2 Manager, Bit 3 ohne Spieler, oberes Halbbyte Restlaufzeit; +1 Schlüssel
    /// (Tag + 1, höchstens 255); +2 Poolspieler; ab +3 Text, Zeilen durch „^“ getrennt. Umkehrung von <c>ApplyMessages</c>.
    /// [APPROX] Erledigte Nachrichten und Kreditwünsche führt das Original anders (Feldkarte U); der Export schreibt nur offene.
    /// </summary>
    private void WriteMessages(OriginalSaveWriter w)
    {
        var block = w.Block("D294");
        var indexOf = PoolIndices();
        var k = 0;
        for (var m = 0; m < Managers.Count; m++)
            foreach (var message in Finance(Managers[m]).Inbox)
            {
                if (k >= MessageCapacity || message.Done)
                    continue;
                var r = block.AsSpan(k * OriginalImport.MessageRecordLength, OriginalImport.MessageRecordLength);
                var withPlayer = message.Player is { } p && indexOf.ContainsKey(p);
                r[0] = (byte)(m | (withPlayer ? 0 : 8) | (Math.Clamp(message.DaysLeft, 1, 15) << 4));
                r[1] = OriginalExport.Byte(message.Day + 1);
                r[2] = (byte)(withPlayer ? indexOf[message.Player!] : 0);
                var text = GameText.Encode(string.Join("^", message.Lines));
                text.AsSpan(0, Math.Min(text.Length, OriginalImport.MessageRecordLength - 4)).CopyTo(r[3..]);
                k++;
            }
    }
}

public sealed partial class DebCup
{
    /// <summary>
    /// Laufende Runde (Umkehrung von <c>ImportOriginal</c>): Runde 0x5CF3, Plätze 0x5CFB (Platz 2k/2k + 1, nach ungerader Spielzahl
    /// getauscht; dahinter 0xFF), Spielzähler 0xD074, Tore 0xD014 + 32 · Spiel (Heimbyte + 30 je Marke, 100 = nicht ausgetragen), Siege
    /// 0xD075 + Platz. Nach dem Endspiel: Zähler 2, Siege 0, Plätze wie vor dem dritten Termin.
    /// [APPROX] Eine fertige Runde vor der nächsten Auslosung steht wie nach dem Endspiel (Zähler 2, Siege 0).
    /// </summary>
    internal void ExportOriginal(OriginalSaveWriter w, int[] slotOf, GameState game)
    {
        var slots = w.Block("5CFB");
        for (var i = 0; i < 32; i++)
            slots[i] = OriginalImport.CupSlotEnd;
        if (_rounds.Count == 0)
            return;
        var round = _rounds.Count;
        w.Block("5CF3")[0] = (byte)round;
        // [APPROX] DGROUP 0x7AD: an den Ständen die laufende Pokalrunde, ab der zweiten Saison bis zum ersten Pokaltermin 0.
        w.Block("07A9")[4] = (byte)(game.Season > GameState.FirstSeason && game.CurrentDay < DayOf(0, 0) ? 0 : round);
        var ties = _rounds[^1];
        var played = ties.Count == 0 ? 0 : ties.Max(t => t.Games.Count);
        var finished = ties.All(t => t.IsDecided);
        var counter = finished ? 2 : played;
        var swapped = !finished && counter % 2 == 1;
        w.Block("D074")[0] = (byte)counter;
        var goals = w.Block("D014");
        var wins = w.Block("D075");
        for (var k = 0; k < ties.Count && 2 * k + 1 < 32; k++)
        {
            var tie = ties[k];
            var (first, second) = swapped ? (2 * k + 1, 2 * k) : (2 * k, 2 * k + 1);
            slots[first] = (byte)slotOf[tie.First];
            slots[second] = (byte)slotOf[tie.Second];
            if (!finished)
            {
                wins[first] = (byte)tie.Wins(tie.First);
                wins[second] = (byte)tie.Wins(tie.Second);
            }
            for (var g = 0; g < DatesPerRound; g++)
            {
                if (g >= tie.Games.Count)
                {
                    if (tie.IsDecided)
                        goals[32 * g + first] = goals[32 * g + second] = OriginalImport.CupNotPlayed;
                    continue;
                }
                var match = tie.Games[g];
                var marks = tie.MarksOf(g);
                var firstHome = match.Home == tie.First;
                var fg = firstHome ? match.HomeGoals : match.AwayGoals;
                var sg = firstHome ? match.AwayGoals : match.HomeGoals;
                goals[32 * g + first] = OriginalExport.Byte(fg + (firstHome ? 30 * marks : 0));
                goals[32 * g + second] = OriginalExport.Byte(sg + (firstHome ? 0 : 30 * marks));
            }
        }
    }
}

public sealed partial class EuropeanCup
{
    /// <summary>
    /// Gruppen (Wettbewerb 2: Teilnehmer 0xD004, Paarungen/Tore 0xCED8/0xCDB8 + 0x90), Endrunde (Wettbewerb 3, + 0xD8), Termine 0x7AF.
    /// [APPROX] Drittes Paar je Termin (Füllpaar) 0.
    /// </summary>
    internal void ExportOriginal(OriginalSaveWriter w, int[] slotOf, IReadOnlyList<Club> clubs)
    {
        var teams = w.Block("CFFC");
        for (var i = 0; i < _draw.Count && i < 8; i++)
            teams[8 + i] = w.Block("5CFB")[0x20 + i] = (byte)slotOf[_draw[i]]; // [ORIGINAL] Auslosung auch in 0x5D1B (FILE01)
        if (_draw.Count > 0)
            w.Block("5CF3")[1] = 1;
        var dates = 0;
        for (var g = 0; g < _groups.Count && g < 2; g++)
        {
            dates = Math.Max(dates, _groups[g].Results.Count / 2);
            OriginalExportGroup.Write(w, slotOf, clubs, 2, g, _groups[g], 2);
        }
        if (FinalRound is { } final)
        {
            for (var i = 0; i < final.Teams.Count && i < 4; i++)
                teams[16 + i] = (byte)slotOf[final.Teams[i]];
            OriginalExportGroup.Write(w, slotOf, clubs, 3, 0, final, 2);
            dates = GroupDates + final.Results.Count / 2;
        }
        w.Block("07A9")[6] = (byte)dates;
    }
}

public sealed partial class WorldCup
{
    /// <summary>
    /// Gruppen (Wettbewerb 0, Gruppe g = Sätze 52 + 6g, also <see cref="Order"/>), Paarungen/Tore 0xCED8/0xCDB8 + 0x24 · g, Termine
    /// 0x7AE, K.-o.-Plätze 0x5D5B (24 wie <c>_slots</c>). [APPROX] Tore der K.-o.-Spiele führt das HOK nicht.
    /// </summary>
    internal void ExportOriginal(OriginalSaveWriter w, int[] slotOf, IReadOnlyList<Club> clubs)
    {
        var dates = 0;
        for (var g = 0; g < _groups.Count && g < 2; g++)
        {
            dates = Math.Max(dates, _groups[g].Results.Count / 3);
            OriginalExportGroup.Write(w, slotOf, clubs, 0, g, _groups[g], 3);
        }
        dates += _knockout.Count;
        w.Block("07A9")[5] = (byte)dates;
        if (_groups.Count == 0)
            return;
        var slots = w.Block("5CFB");
        for (var i = 0; i < _slots.Length; i++)
            slots[0x60 + i] = _slots[i] is { } c ? (byte)slotOf[c] : OriginalImport.CupSlotEnd;
    }
}

public sealed partial class Playoffs
{
    /// <summary>
    /// Play-offs (Umkehrung von <c>ImportOriginal</c>): Plätze 0x5D3B (Viertelfinale 0–7, Halbfinale 8–11, Endspiel 12–13, Verlierer 23),
    /// Runde 0x7B0, Spiel der Runde 0xCCA3, Anfang der Runde 0xCFFA, Spiele 0xCCB2 + 14 · Spiel + Platz (100 = nicht ausgetragen),
    /// Siege 0xCCA4 + Platz. In der laufenden Runde nach ungerader Spielzahl getauscht (0x31505).
    /// [APPROX] Marken (+30) und das Siegbyte 0x14 des fünften Termins schreibt der Export nicht.
    /// </summary>
    internal void ExportOriginal(OriginalSaveWriter w, int[] slotOf)
    {
        if (_rounds.Count == 0)
            return;
        var slots = w.Block("5CFB");
        var goals = w.Block("CCB2");
        var wins = w.Block("CCA4");
        int[][] pairSlots = [[0, 2, 4, 6], [8, 10], [12]];
        var round = _rounds.Count - 1;
        var current = _rounds[round];
        var game = current.Max(s => s.Games.Count);
        var roundOpen = current.Any(s => !s.IsDecided);
        if (!roundOpen)
            game = DatesPerRound;
        for (var r = 0; r <= round; r++)
        {
            var open = r == round && roundOpen;
            for (var i = 0; i < _rounds[r].Count && i < pairSlots[r].Length; i++)
            {
                var p = pairSlots[r][i];
                var s = _rounds[r][i];
                var swapped = open && game % 2 == 1;
                var (a, b) = swapped ? (p + 1, p) : (p, p + 1);
                slots[0x40 + a] = (byte)slotOf[s.HomeClub];
                slots[0x40 + b] = (byte)slotOf[s.AwayClub];
                if (r != round)
                    continue;
                wins[a] = (byte)s.Wins(s.HomeClub);
                wins[b] = (byte)s.Wins(s.AwayClub);
                for (var g = 0; g < DatesPerRound; g++)
                {
                    if (g >= s.Games.Count)
                    {
                        if (s.IsDecided || !open)
                            goals[OriginalImport.PlayoffSlotCount * g + a] = goals[OriginalImport.PlayoffSlotCount * g + b] = OriginalImport.CupNotPlayed;
                        continue;
                    }
                    var m = s.Games[g];
                    var homeFirst = m.Home == s.HomeClub;
                    // [ORIGINAL] Im Original steht der Penaltytreffer im Ergebnis (kein Gleichstand, Import: „Gleichstand zählt für den
                    // Gast“ nur zur Sicherheit). [APPROX] Ein Remake-Spiel mit Gleichstand und Penaltysieger bekommt dessen Treffer dazu.
                    var (hg, ag) = (m.HomeGoals, m.AwayGoals);
                    if (hg == ag && m.PenaltyWinner is { } pw)
                        (hg, ag) = pw == m.Home ? (hg + 1, ag) : (hg, ag + 1);
                    goals[OriginalImport.PlayoffSlotCount * g + a] = OriginalExport.Byte(homeFirst ? hg : ag);
                    goals[OriginalImport.PlayoffSlotCount * g + b] = OriginalExport.Byte(homeFirst ? ag : hg);
                }
            }
        }
        w.Block("07A9")[7] = (byte)round;
        w.Block("CCA3")[0] = (byte)game;
        w.Block("CFF8")[2] = (byte)new[] { 0, 8, 12 }[round];
    }
}

public sealed partial class RelegationGroup
{
    /// <summary>Aufstiegsrunde <paramref name="group"/> als Wettbewerb 1 der Gruppentabellen (Teilnehmer 0xCFFC + 4 · Gruppe).</summary>
    internal void ExportOriginal(OriginalSaveWriter w, int[] slotOf, int group, IReadOnlyList<Club> clubs)
    {
        var teams = w.Block("CFFC");
        for (var i = 0; i < Clubs.Count && i < 4; i++)
            teams[4 * group + i] = (byte)slotOf[Clubs[i]];
        var pairs = w.Block("CED8");
        var goals = w.Block("CDB8");
        for (var d = 0; d < Rounds.Count && d < 6; d++)
        {
            var at = 0x48 + 0x24 * group + 6 * d;
            var matches = Rounds[d].Matches;
            for (var p = 0; p < matches.Count && p < 2; p++)
            {
                pairs[at + 2 * p] = (byte)slotOf[matches[p].Home];
                pairs[at + 2 * p + 1] = (byte)slotOf[matches[p].Away];
                if (Results.FirstOrDefault(r => r.MatchDay == d + 1 && r.Home == matches[p].Home && r.Away == matches[p].Away) is { } res)
                {
                    goals[at + 2 * p] = OriginalExport.Byte(res.HomeGoals);
                    goals[at + 2 * p + 1] = OriginalExport.Byte(res.AwayGoals);
                }
            }
        }
        OriginalExportGroup.WriteTable(w, slotOf, 1, group, Clubs, Table(clubs));
    }
}

/// <summary>Gruppentabellen des Originals (Wettbewerb 0 WM, 1 Aufstiegsrunden, 2 Europapokal, 3 Endrunde).</summary>
internal static class OriginalExportGroup
{
    /// <summary>Paarungen und Tore je Termin (0xCED8/0xCDB8 + 0x48 · Wettbewerb + 0x24 · Gruppe + 6 · Termin) und Tabelle.</summary>
    public static void Write(OriginalSaveWriter w, int[] slotOf, IReadOnlyList<Club> clubs, int competition, int group, CupGroup cup, int pairsPerDate)
    {
        var pairs = w.Block("CED8");
        var goals = w.Block("CDB8");
        for (var d = 0; d < cup.RoundCount && d < 6; d++)
        {
            var at = 0x48 * competition + 0x24 * group + 6 * d;
            var plan = cup.Pairings(d);
            for (var p = 0; p < plan.Count && p < pairsPerDate; p++)
            {
                pairs[at + 2 * p] = (byte)slotOf[plan[p].Home];
                pairs[at + 2 * p + 1] = (byte)slotOf[plan[p].Away];
                var index = d * plan.Count + p;
                if (index < cup.Results.Count)
                {
                    goals[at + 2 * p] = OriginalExport.Byte(cup.Results[index].HomeGoals);
                    goals[at + 2 * p + 1] = OriginalExport.Byte(cup.Results[index].AwayGoals);
                }
            }
        }
        WriteTable(w, slotOf, competition, group, cup.Teams, cup.Table(clubs));
    }

    /// <summary>
    /// Platzliste 0xCD88 (Index des Teilnehmers je Platz), Punkte 0xCD58, Tore 0xCD28, Gegentore 0xCCF8 je + 12 · Wettbewerb + 6 · Gruppe.
    /// [APPROX] Punkte und Tore je Teilnehmerindex (die Summe prüft der Import; Zuordnung je Platz oder je Teilnehmer nicht belegt).
    /// </summary>
    public static void WriteTable(OriginalSaveWriter w, int[] slotOf, int competition, int group, IReadOnlyList<int> teams, IReadOnlyList<TableRow> table)
    {
        var at = 12 * competition + 6 * group;
        for (var place = 0; place < table.Count && place < 6; place++)
        {
            var row = table[place];
            var index = teams.ToList().IndexOf(row.Club.Index);
            w.Block("CD88")[at + place] = (byte)Math.Max(index, 0);
            if (index < 0)
                continue;
            w.Block("CD58")[at + index] = OriginalExport.Byte(row.PointsPlus);
            w.Block("CD28")[at + index] = OriginalExport.Byte(row.GoalsFor);
            w.Block("CCF8")[at + index] = OriginalExport.Byte(row.GoalsAgainst);
        }
    }
}
