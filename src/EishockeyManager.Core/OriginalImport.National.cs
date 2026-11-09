using EishockeyManager.Formats;

namespace EishockeyManager.Core;

public sealed partial class GameState
{
    /// <summary>
    /// Nationalkader des Bundestrainers (Teil 4b). [ORIGINAL] Kaderplätze Satz 5 (DGROUP 0x5DC3 + 5 · 0x618) in Platzfolge:
    /// +0x0F Poolindex, +0x10–0x13 Kondition, Technik, Form, Belastung, +0x15 Reihe (Bit 0x80 Sturm), +0x0A eingesetzt; Taktik
    /// DGROUP 0xA6FB/0xA707/0xA701 + 5, 0xCAE9 + 0x30 · 5, 0xCC43 + 8 · 5 (FILE18–FILE20, FILE47–FILE49A).
    /// <list type="bullet">
    /// <item>[APPROX] Ob es einen Bundestrainer gibt und welcher Manager es ist, folgt aus dem Nachspielen (DGROUP 0xD0BA ist an
    ///   allen Ständen 0, auch mit Bundestrainer).</item>
    /// <item>[APPROX] Das Remake führt einen Wertesatz je Spieler: Die Werte des Satzes 5 gelten für Pool- und Marktspieler; Spieler
    ///   eines übernommenen Managerkaders behalten die Werte ihres Vereinsplatzes.</item>
    /// <item>[APPROX] Pool +0x1D eines Nominierten zeigt das Land (FILE18: alle 54); Verein des Mitglieds ist bei Kaderspielern
    ///   eines Managers dessen Verein, sonst der Poolverein.</item>
    /// </list>
    /// </summary>
    private void ApplyNationalSquad(OriginalSaveGame save, int[] clubOf, Dictionary<int, Player> pool,
        Dictionary<int, Player> squadPlayers)
    {
        if (NationalCoach is not { } coach)
            return;
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
    }
}
