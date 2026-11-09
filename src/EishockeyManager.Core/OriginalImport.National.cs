using EishockeyManager.Formats;

namespace EishockeyManager.Core;

public sealed partial class GameState
{
    /// <summary>
    /// Nationalkader des Bundestrainers (Teil 4b). [ORIGINAL] Kaderplätze Satz 5 (DGROUP 0x5DC3 + 5 · 0x618) in Platzfolge:
    /// +0x0F Poolindex, +0x10–0x13 Kondition, Technik, Form, Belastung, +0x15 Reihe (Bit 0x80 Sturm), +0x0A eingesetzt; Taktik
    /// DGROUP 0xA6FB/0xA707/0xA701 + 5, 0xCAE9 + 0x30 · 5, 0xCC43 + 8 · 5 (FILE18–FILE20, FILE47–FILE49A).
    /// <list type="bullet">
    /// <item>[ORIGINAL] Bundestrainer im Amt, wenn der Vereinssatz des Landes (DGROUP 0x6AC) +0x16 = 6 ist (Ernennung Datei 0x55494,
    ///   Amtsende 0x55752); Manager in Satz 5 +0xEB (0x554B1), Land in Satz 5 +0x14 (0x55485). DGROUP 0xD0BA + Manager ist nur das
    ///   Kennzeichen „Ernennung steht an“ (0x54D19 → 1, nach der Ernennung 0x554BA → 0) und deshalb an allen Ständen 0 (R14HI-04;
    ///   FILE11/FILE18–21/FILE47–49A: +0x16 = 6, +0xEB = 0; FILE05: 0 / 50).</item>
    /// <item>[ORIGINAL] Freundschaftsspiele: Zahl DGROUP 0xD0C1, je Spiel Heim 0xD0DB, Gast 0xD0D3, Heimtore 0xD0CB, Gasttore 0xD0C3
    ///   (Schreiber 0x3C248–0x3C288 mit dem Ergebnis von 0x30365 = Heimtore &lt;&lt; 8 | Gasttore; FILE19: Deutschland – Schweden 2:1).
    ///   [APPROX] Der Wertebonus des Spiels steht nicht im HOK (0).</item>
    /// <item>[ORIGINAL] Anzeige unter „AUTOM.“ der Nationalteam-Taktik aus dem Vereinssatz des Landes (+0x26–0x2B Einsatz, +0x35–0x3A
    ///   Angriff, Spielvorbereitung 0x8365; FILE19), eigene Werte aus Satz 5 – ohne Bundestrainer in der Taktik des Landes
    ///   (<c>_vacantNationalTactics</c>).</item>
    /// <item>[APPROX] Das Remake führt einen Wertesatz je Spieler: Die Werte des Satzes 5 gelten für Pool- und Marktspieler; Spieler
    ///   eines übernommenen Managerkaders behalten die Werte ihres Vereinsplatzes.</item>
    /// <item>[APPROX] Pool +0x1D eines Nominierten zeigt das Land (FILE18: alle 54); Verein des Mitglieds ist bei Kaderspielern
    ///   eines Managers dessen Verein, sonst der Poolverein.</item>
    /// </list>
    /// </summary>
    private void ApplyNationalSquad(OriginalSaveGame save, int[] clubOf, Dictionary<int, Player> pool,
        Dictionary<int, Player> squadPlayers)
    {
        // A009 (Schema 41): „automatisch aufstellen“ des Bundestrainers = Satz 5 im Block 0375 (0x375 + 2 · 5 aktuell, 0x380 gesichert).
        // [APPROX] Der Zug gilt als nicht offen (wie nach der Migration 40 → 41): Es zählt der gesicherte Wert 0x380.
        _nationalAutoLineupSaved = save.Block("0375")[2 * OriginalImport.NationalRecord + 1] != 0;
        ApplyNationalCoachOffice(save, clubOf);
        var nationRecord = save.Block("far:9657").AsSpan(save.Block("06AC")[0] * OriginalSaveGame.ClubRecordLength, OriginalSaveGame.ClubRecordLength);
        if (NationalCoach is not { } coach)
        {
            _vacantNationalTactics ??= Tactics.National();
            ApplyTacticValues(save, _vacantNationalTactics, OriginalImport.NationalRecord);
            _vacantNationalTactics.ImportAutoLine(nationRecord.Slice(0x26, Tactics.Situations), nationRecord.Slice(0x35, Tactics.Situations));
            return;
        }
        coach.ClearFriendlies();
        int friendlies = save.Block("D0C1")[0];
        for (var i = 0; i < Math.Min(friendlies, NationalCoach.MaxFriendlies); i++)
        {
            int home = save.Block("D0DB")[i], away = save.Block("D0D3")[i];
            if (home >= clubOf.Length || away >= clubOf.Length)
                throw new InvalidDataException($"Freundschaftsspiel {i + 1} (DGROUP 0xD0DB/0xD0D3) ohne Verein.");
            coach.AddFriendly(new FriendlyMatch(clubOf[home], clubOf[away], save.Block("D0CB")[i], save.Block("D0C3")[i], 0));
        }
        coach.ClearSquad();
        var lines = new List<(Player Player, int Line, bool Dressed)>();
        for (var i = 0; i < NationalCoach.MaxSquad; i++)
        {
            var s = OriginalImport.SquadSlot(save, OriginalImport.NationalRecord, i);
            int index = s[0x0F];
            if (index == 0 || !pool.TryGetValue(index, out var player) || coach.Contains(player))
                continue;
            var e = OriginalImport.PoolEntry(save, index);
            var club = Clubs.FirstOrDefault(c => c.Players.Contains(player))?.Index
                       ?? (e[0x1D] < clubOf.Length ? clubOf[e[0x1D]] : coach.Nation);
            if (!squadPlayers.ContainsValue(player))
            {
                player.Condition = s[0x10];
                player.Technique = s[0x11];
                player.Form = s[0x12];
                player.Strain = s[0x13];
            }
            coach.Add(player, club);
            lines.Add((player, s[0x15] & 0x0F, s[0x0A] != 0));
        }

        var used = new Dictionary<(int Block, PlayerRole Role), int>();
        foreach (var (player, line, _) in lines)
        {
            if (line is < 1 or > 3)
                continue;
            used.TryGetValue((line - 1, player.Role), out var n);
            LineupSlot? slot = player.Role switch
            {
                PlayerRole.Goalkeeper when line == 1 && coach.Formation.Goalkeeper is null => LineupSlot.Goalkeeper,
                PlayerRole.Defender when n < Lineup.DefendersPerBlock => LineupSlot.Defender(line - 1, n),
                PlayerRole.Forward when n < Lineup.ForwardsPerBlock => LineupSlot.Forward(line - 1, n),
                _ => null,
            };
            if (slot is { } target && coach.Assign(target, player) == LineupResult.Assigned)
                used[(line - 1, player.Role)] = n + 1;
        }
        foreach (var (player, _, dressed) in lines)
            if (dressed && !coach.Formation.Contains(player))
                coach.Dress(player);
        ApplyTacticValues(save, coach.Tactics, OriginalImport.NationalRecord);
        coach.Tactics.ImportAutoLine(nationRecord.Slice(0x26, Tactics.Situations), nationRecord.Slice(0x35, Tactics.Situations));
        coach.TakeOverAutoLineup(_nationalAutoLineupSaved); // nach dem Aufstellen: Assign/Dress dürfen den Wert nicht verändern
    }

    /// <summary>
    /// Amt des Bundestrainers aus dem HOK (siehe <see cref="ApplyNationalSquad"/>): im Amt → Manager aus Satz 5 +0xEB mit der Taktik des
    /// Landes (wie <see cref="AppointNationalCoach"/>, aber ohne Etatbuchung – das Nationalkonto kommt aus Satz 5 +0x18F); nicht im
    /// Amt → kein Bundestrainer, die Taktik bleibt beim Land.
    /// </summary>
    private void ApplyNationalCoachOffice(OriginalSaveGame save, int[] clubOf)
    {
        var nation = save.Block("06AC")[0];
        var inOffice = save.Block("far:9657")[nation * OriginalSaveGame.ClubRecordLength + 0x16] == 6;
        var coachRecord = save.Block("965B").AsSpan(OriginalImport.NationalRecord * OriginalSaveGame.ManagerRecordLength, OriginalSaveGame.ManagerRecordLength);
        int manager = coachRecord[0xEB];
        if (inOffice && manager < Managers.Count)
        {
            if (NationalCoach is { } current && current.Manager == manager)
                return;
            var coach = new NationalCoach(manager, nation < clubOf.Length ? clubOf[nation] : NationalTeams.Germany);
            NationalCoach = coach;
            HandNationalTacticsToCoach(coach);
            return;
        }
        if (NationalCoach is not null)
        {
            TakeNationalTacticsFromCoach();
            NationalCoach = null;
        }
    }
}

public sealed partial class Tactics
{
    /// <summary>Anzeige unter „AUTOM.“ (Grundwert und fünf Zuschläge) aus einem Original-Spielstand (Vereinssatz des Landes +0x26 bzw. +0x35; Spur hok-import, R14HI-04).</summary>
    internal void ImportAutoLine(ReadOnlySpan<byte> effort, ReadOnlySpan<byte> attack)
    {
        if (_shownAutoEffort is null || _shownAutoAttack is null)
            return;
        for (var i = 0; i < Situations; i++)
        {
            _shownAutoEffort[i] = effort[i];
            _shownAutoAttack[i] = attack[i];
        }
    }
}
