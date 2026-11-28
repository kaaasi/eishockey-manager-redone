using EishockeyManager.Formats;

namespace EishockeyManager.Core;

public sealed partial class GameState
{
    /// <summary>Modi 0xA6FB/0xA707/0xA701 + Satz, Werte 0xCAE9 + 0x30 · Satz, Minuten 0xCC43 + 8 · Satz (Umkehrung von <c>ApplyTacticValues</c>).</summary>
    private static void WriteTacticValues(OriginalSaveWriter w, Tactics tactics, int record)
    {
        w.Block("A6FB")[record] = (byte)tactics.EffortMode;
        w.Block("A707")[record] = (byte)tactics.AttackMode;
        w.Block("A701")[record] = (byte)tactics.LineChange;
        var values = w.Block("CAE9");
        for (var b = 0; b < Tactics.Blocks; b++)
            for (var i = 0; i < Tactics.Situations; i++)
            {
                values[0x30 * record + 6 * b + i] = OriginalExport.Byte(tactics.Effort(b, (ScoreSituation)i));
                values[0x30 * record + 0x18 + 6 * b + i] = OriginalExport.Byte(tactics.Attack(b, (ScoreSituation)i));
            }
        var minutes = w.Block("CC43");
        for (var part = 0; part < 2; part++)
            for (var line = 0; line < Lineup.Blocks; line++)
                minutes[8 * record + 4 * part + line] = OriginalExport.Byte(tactics.Minutes(line, part == 1));
    }
}
