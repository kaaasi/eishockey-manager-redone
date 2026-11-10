using EishockeyManager.Formats;

namespace EishockeyManager.Core;

public sealed partial class GameState
{
    /// <summary>
    /// Vereinssätze far:9657 + 0x3D · Satz (Feldkarte „Vereine, Teamwerte“).
    /// <list type="bullet">
    /// <item>[ORIGINAL] +0x00 Name (0x16 Byte), +0x17 Tagesform, +0x18–0x1D Teamwerte (Import <c>ApplyTeamValues</c>); im Modus
    ///   „nur Weltkugel“ trägt der Satz des Managervereins den Namen des Landes (FILE11, Datei 0x5B556).</item>
    /// <item>[ORIGINAL] +0x16: 1 Managerverein (FILE01: KÖLNER EC), 6 Land mit Bundestrainer im Amt (Datei 0x55494), sonst 0.</item>
    /// <item>[ORIGINAL] +0x26–0x2B/+0x35–0x3A: Anzeige „AUTOM.“ des Nationalteams (Spielvorbereitung 0x8365; FILE19).
    ///   [APPROX] Für die übrigen Vereine rechnet das Original die Werte vor jedem Spiel neu; der Export schreibt 0 (Stand FILE01).</item>
    /// <item>[ORIGINAL] +0x34 Logo (<see cref="DatabaseClub.LogoNumber"/>, beim Managerverein + 0x80; FILE01: KÖLNER EC 0x8C) und
    ///   +0x3B/+0x3C Farbcode (<c>kb\23</c> 0x0000, niederes Byte zuerst), an FILE01–FILE41 gegen die Datenbank geprüft.
    ///   [APPROX] +0x1E (45–55, ab Satz 64 nach dem ersten Tag 90) und +0x1F–0x25 (ab dem ersten Tag 90) ungedeutet.</item>
    /// </list>
    /// </summary>
    private void WriteClubRecords(OriginalSaveWriter w, int[] slotOf, ClubDatabase? database, byte[]? raw)
    {
        var records = w.Block("far:9657");
        var managerClubs = Managers.Select(m => m.ClubIndex).ToHashSet();
        var nation = NationalCoach?.Nation ?? NationalTeams.Germany;
        foreach (var club in Clubs)
        {
            var at = slotOf[club.Index] * OriginalSaveGame.ClubRecordLength;
            var r = records.AsSpan(at, OriginalSaveGame.ClubRecordLength);
            var name = WorldOnly && club.Index == Managers[0].ClubIndex ? Clubs[nation].Name : club.Name;
            var nameIndex = WorldOnly && club.Index == Managers[0].ClubIndex ? nation : club.Index;
            (raw is not null && Clubs[nameIndex].Name == database?[nameIndex].Name
                ? OriginalExport.DatabaseClubName(raw, nameIndex)
                : OriginalSaveWriter.Name(name, OriginalImport.ClubNameLength)).CopyTo(r);
            r[0x16] = (byte)(managerClubs.Contains(club.Index) && !WorldOnly ? 1
                : NationalCoach is { } coach && coach.Nation == club.Index ? 6 : 0);
            if (club.TeamValues is { } v)
            {
                r[0x17] = OriginalExport.Byte(v.DayForm);
                r[0x18] = OriginalExport.Byte(v.DefenseCondition);
                r[0x19] = OriginalExport.Byte(v.AttackCondition);
                r[0x1A] = OriginalExport.Byte(v.DefenseTechnique);
                r[0x1B] = OriginalExport.Byte(v.AttackTechnique);
                r[0x1C] = OriginalExport.Byte(v.DefenseForm);
                r[0x1D] = OriginalExport.Byte(v.AttackForm);
            }
            r[0x1E] = 50; // [APPROX] ungedeutet (FILE01: 45–55)
            if (CurrentDay > 0 || Season > FirstSeason)
                r.Slice(0x1F, 7).Fill(90); // [APPROX] ungedeutet, ab dem ersten Tag an fast allen Sätzen 90
            if (database is not null && club.Index < database.Clubs.Count)
            {
                var db = database[club.Index];
                if (club.Index < ClubDatabase.ClubsWithSquad && db.LogoNumber >= 0)
                    r[0x34] = (byte)(db.LogoNumber | (managerClubs.Contains(club.Index) ? 0x80 : 0));
                r[0x3B] = (byte)db.ColorCode;
                r[0x3C] = (byte)(db.ColorCode >> 8);
            }
        }
        // Anzeige „AUTOM.“ der Nationalteam-Taktik im Satz des Landes.
        var shown = NationalCoach?.Tactics ?? _vacantNationalTactics;
        if (shown is not null)
        {
            var at = slotOf[nation] * OriginalSaveGame.ClubRecordLength;
            for (var i = 0; i < Tactics.Situations; i++)
            {
                records[at + 0x26 + i] = OriginalExport.Byte(shown.ExportAutoEffort(i));
                records[at + 0x35 + i] = OriginalExport.Byte(shown.ExportAutoAttack(i));
            }
        }
    }

    /// <summary>
    /// Liga: Ergebnisse 0xA0CB, nächster Spieltag 0x7A9–0x7AB (und Kopfbyte 0x0F), Platzliste 0x5CCB, Paarungen des Spieltags 0x5D7B,
    /// Tabellensätze 0x825A + 0x50 · Satz (Punkte, Bilanz, Tore, Gegentore, Spiele, Siege, Niederlagen je Heim/Auswärts mit Sicherung,
    /// Tabellenfeld 0x8288, Platzfelder 0x828B, Strafminuten 0x82A1; reverse-engineering.md „Ligatabelle“).
    /// </summary>
    private void WriteLeague(OriginalSaveWriter w, int[] slotOf)
    {
        var results = w.Block("A0CB");
        var next = w.Block("07A9");
        var table = w.Block("8257");
        var order = w.Block("5CCB");
        var pairs = w.Block("5D7B");
        var balances = new Dictionary<int, string>();
        foreach (var league in Enum.GetValues<League>())
        {
            var plan = Schedules.GetValueOrDefault(league) ?? [];
            var played = Math.Min(PlayedMatchDays, plan.Count);
            // [ORIGINAL] Ergebnis von Paarung k am Spieltag d: 0xA0CB + 0x210 · Liga + 12 · (d − 1) + 2 · k (Heimtore, Gasttore).
            for (var d = 1; d <= played; d++)
            {
                var matches = plan[d - 1].Matches;
                for (var k = 0; k < matches.Count; k++)
                {
                    var (home, away) = matches[k];
                    var r = Results.FirstOrDefault(x => x.League == league && x.MatchDay == d && x.Home == home && x.Away == away);
                    if (r is null)
                        continue; // Spieltag nicht eingetragen (FILE16A): 0:0 wie im Original
                    var at = OriginalImport.ResultsPerLeague * (int)league + 12 * (d - 1) + 2 * k;
                    results[at] = OriginalExport.Byte(r.HomeGoals);
                    results[at + 1] = OriginalExport.Byte(r.AwayGoals);
                }
            }
            next[(int)league] = (byte)(played + 1);
            // [ORIGINAL] Paarungen des laufenden Spieltags DGROUP 0x5D7B + 12 · Liga (Satzplätze; Spielplan 0x4E7C7, Datei 0x36CD3).
            // [APPROX] Nach dem letzten Spieltag bleibt der letzte stehen.
            if (plan.Count > 0)
            {
                var day = plan[Math.Min(played, plan.Count - 1)].Matches;
                for (var k = 0; k < Math.Min(day.Count, 6); k++)
                {
                    pairs[12 * (int)league + 2 * k] = (byte)slotOf[day[k].Home];
                    pairs[12 * (int)league + 2 * k + 1] = (byte)slotOf[day[k].Away];
                }
            }

            // [ORIGINAL] Tabelle: Platzliste 0x5CCB + 12 · Liga = Satzplätze in Tabellenfolge (Sortierung 0x50D6D).
            var total = LeagueTable.Compute(this, league, TableView.Gesamt, played);
            var homeRows = LeagueTable.Compute(this, league, TableView.Heim, played).ToDictionary(r => r.Club.Index);
            var awayRows = LeagueTable.Compute(this, league, TableView.Auswaerts, played).ToDictionary(r => r.Club.Index);
            for (var i = 0; i < Math.Min(total.Count, ClubsPerLeague); i++)
                order[ClubsPerLeague * (int)league + i] = (byte)slotOf[total[i].Club.Index];
            foreach (var row in total)
            {
                balances[row.Club.Index] = row.Balance;
                var h = homeRows[row.Club.Index];
                var a = awayRows[row.Club.Index];
                var at = OriginalImport.TableField(0x825A, slotOf[row.Club.Index]);
                for (var copy = 0; copy < 2; copy++)
                {
                    // Feld und Sicherung (Hauptmenü 0x44EFD sichert vor dem Sichern: Sicherung = Feld).
                    table[at + 0x00 + 2 * copy] = OriginalExport.Byte(h.PointsPlus);
                    table[at + 0x01 + 2 * copy] = OriginalExport.Byte(a.PointsPlus);
                    table[at + 0x16 + 2 * copy] = OriginalExport.Byte(h.GoalsFor);
                    table[at + 0x17 + 2 * copy] = OriginalExport.Byte(a.GoalsFor);
                    table[at + 0x1A + 2 * copy] = OriginalExport.Byte(h.GoalsAgainst);
                    table[at + 0x1B + 2 * copy] = OriginalExport.Byte(a.GoalsAgainst);
                }
                // [ORIGINAL] Siege +0x26/+0x27, Niederlagen +0x2A/+0x2B; die Sicherung +0x28/+0x2C führt an allen Ständen nur das
                // Heimfeld (+0x29/+0x2D = 0, FILE05, FILE41).
                table[at + 0x26] = table[at + 0x28] = OriginalExport.Byte(h.Wins);
                table[at + 0x27] = OriginalExport.Byte(a.Wins);
                table[at + 0x2A] = table[at + 0x2C] = OriginalExport.Byte(h.Losses);
                table[at + 0x2B] = OriginalExport.Byte(a.Losses);
                table[at + 0x1E] = OriginalExport.Byte(h.Games);
                table[at + 0x1F] = OriginalExport.Byte(a.Games);
            }
        }
        w.HeaderMatchDay = next[0];

        // Bilanz (8 Zeichen, '-' = noch kein Spiel, FILE01) aller Sätze 0–63, Tabellenfeld, Platzfelder, Strafminuten.
        foreach (var club in Clubs.Where(c => slotOf[c.Index] < OriginalImport.TeamSlots))
        {
            var slot = slotOf[club.Index];
            var at = OriginalImport.TableField(0x825A, slot);
            var balance = balances.GetValueOrDefault(club.Index, "");
            var text = balance.PadLeft(LeagueTable.BalanceLength, '-');
            var raw = GameText.Encode(text);
            raw.AsSpan(0, LeagueTable.BalanceLength).CopyTo(table.AsSpan(at + 0x04));
            if (club.League is not null)
                raw.AsSpan(0, LeagueTable.BalanceLength).CopyTo(table.AsSpan(at + 0x0D));
            table[OriginalImport.TableField(0x8288, slot)] = OriginalExport.Byte(club.StoredTableRank);
            for (var i = 0; i < RankSlotCount; i++)
            {
                var b = OriginalImport.TableField(0x828B, slot) + i / 2;
                var v = club.StoredRankSlot(i) & 0x0F;
                table[b] = (byte)(i % 2 == 0 ? (table[b] & 0xF0) | v : (table[b] & 0x0F) | (v << 4));
            }
            OriginalExport.Write16(table, OriginalImport.TableField(0x82A1, slot), club.LeaguePenaltyMinutes);
        }
    }
}

public sealed partial class Tactics
{
    /// <summary>Anzeige unter „AUTOM.“ (Grundwert und fünf Zuschläge) für den Export (Gegenstück zu <c>ImportAutoLine</c>); 0 ohne Anzeige.</summary>
    internal int ExportAutoEffort(int situation) => _shownAutoEffort?[situation] ?? 0;

    /// <summary>Wie <see cref="ExportAutoEffort"/> für den Angriff.</summary>
    internal int ExportAutoAttack(int situation) => _shownAutoAttack?[situation] ?? 0;
}
