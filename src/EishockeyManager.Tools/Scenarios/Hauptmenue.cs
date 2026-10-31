using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>Spur hauptmenue-bank (#205): Sicherheitsfrage von „und tschüß…“ und „Tag weiter“ über das Vereinslogo.</summary>
internal static class Hauptmenue
{
    [Scenario("und-tschuess")]
    private static void Quit(ScreenDriver d)
    {
        d.StartNewGame();
        var tile = MainMenuScreen.BigTile(MainMenuScreen.QuitTile);
        d.Click(tile.X + 30, tile.Y + 30);
        d.Move(10, 230); d.Save("01_sicherheitsfrage");
        var no = QuitQuestion.NoButton;
        d.Click(no.X + 5, no.Y + 5);
        d.Move(10, 230); d.Save("02_kein_gedanke");
        d.NextDay(); d.Frame(); d.Save("03_tag_weiter_banner");
    }
}
