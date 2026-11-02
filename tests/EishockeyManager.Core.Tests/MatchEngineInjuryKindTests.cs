using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Integration Runde 11 (Anschluss aus kader-transfer Teil 2 in wettbewerbe-spiel, A368/A369): [ORIGINAL] Datei 0x05B1A schreibt
/// die gezogene Verletzungsart in den Kaderplatz (+0x17); das Remake führt sie in <see cref="Player.InjuryKind"/>.
/// </summary>
public class MatchEngineInjuryKindTests
{
    [Theory]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(7, true)]
    public void Inflict_StoresDrawnKind(int seed, bool inMatch)
    {
        var player = new Player { Name = "X", Role = PlayerRole.Forward, Age = 25, Form = 60 };
        Assert.Equal(InjuryKinds.Unknown, player.InjuryKind);
        var kind = PlayerInjuries.Inflict(player, inMatch, difficulty: 2, new Random(seed));
        Assert.Equal(kind, player.InjuryKind);
        Assert.NotNull(player.InjuryName);
    }
}
