using EishockeyManager.UI.Screens.Team;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>
/// Spur transfer-vertraege-komplett, Teil 3: Angebotsfenster 0x491F9 für ein unaufgefordertes Angebot (Bit 0x40). Kein
/// Videobeleg; das Bild dient der Sichtprüfung der Lage aus dem Code. Das Angebot wird im Ablauf gesetzt.
/// </summary>
internal static class TransferAngebot
{
    [Scenario("transfer-angebot")]
    private static void Run(ScreenDriver d)
    {
        d.StartNewGame();
        var game = d.Host.Game!;
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        var rows = TeamUi.Rows(club);
        var player = rows.First(p => !p.OnLoan);
        player.UnsolicitedOffer = game.Clubs.First(c => c.Index < 43 && game.Managers.All(m => m.ClubIndex != c.Index));
        d.OpenTile("Transfer");
        d.Click(20, TransferScreen.FirstRowY + rows.ToList().IndexOf(player) * TransferScreen.RowPitch + 1);
        d.Save("transfer_angebot");
    }
}
