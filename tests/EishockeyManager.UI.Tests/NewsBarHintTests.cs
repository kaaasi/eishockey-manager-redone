using EishockeyManager.Core;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.ManagerArea;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// R4-02 (#109), #841: Die Kachel „Neuer“ ist nur bei voller Managerzahl gesperrt. Der frühere Hinweis „(Klicken)“ bei einer Frage
/// entfällt wie im Original (B07, A778: Datei 0x4B6F0–0x4B76D zeichnet keinen Hinweis; Prüfung in NewsBarDosboxTests).
/// </summary>
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

    /// <summary>
    /// #841: Die Kachel „Neuer“ öffnet die Spielvorbereitung für weitere Manager (Datei 0x45755 → 0x28165), der Posteingang
    /// ist nur über die Nachricht erreichbar.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(4)]
    public void NewTile_LockedOnlyWithAllManagers(int managers)
    {
        var game = Synthetic(managers);
        Assert.Equal(managers >= GameSetup.MaxManagers, TileLocks.IsLocked(game, Tile("Neuer")));
    }
}
