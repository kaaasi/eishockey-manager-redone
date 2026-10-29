using EishockeyManager.Core;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.ManagerArea;

namespace EishockeyManager.UI.Tests;

/// <summary>R4-02 (#109): Der Hinweis bei einer Frage darf nicht auf die gesperrte Kachel „Neuer“ verweisen.</summary>
public class NewsBarHintTests
{
    private static GameState Synthetic(int managers)
    {
        var data = new byte[Formats.ClubDatabase.FileLength];
        for (var i = 0; i < Formats.ClubDatabase.ClubsWithSquad; i++)
        {
            System.Text.Encoding.ASCII.GetBytes($"VEREIN {i}").CopyTo(data, Formats.ClubDatabase.SquadOffset + i * Formats.ClubDatabase.SquadRecordLength);
            data[Formats.ClubDatabase.StrengthOffset + i * 3] = (byte)(100 - i % 12 * 5);
        }
        var setup = new GameSetup { Duration = GameDuration.Unbegrenzt };
        for (var m = 0; m < managers; m++)
            setup.Managers.Add(new ManagerSetup { Name = $"M{m}", Head = m, ClubIndex = 2 + m });
        return NewGame.Create(Formats.ClubDatabase.Parse(data), setup, 3);
    }

    private static MenuTile Tile(string name) => MenuTiles.Small.SelectMany(t => t).First(t => t.Name == name);

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void QuestionHint_PointsToNewTile_WhileItIsFree(int managers)
    {
        var game = Synthetic(managers);
        Assert.False(TileLocks.IsLocked(game, Tile("Neuer")));
        Assert.Equal("(Neuer)", NewsBar.QuestionHint(game));
    }

    [Fact]
    public void QuestionHint_DoesNotPointToLockedNewTile_WithFourManagers()
    {
        var game = Synthetic(4);
        Assert.True(TileLocks.IsLocked(game, Tile("Neuer")));
        Assert.NotEqual("(Neuer)", NewsBar.QuestionHint(game));
    }
}
