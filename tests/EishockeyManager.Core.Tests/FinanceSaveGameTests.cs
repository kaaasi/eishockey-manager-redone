using EishockeyManager.Core;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur finanzen-2 ↔ speichern: neue Finanzfelder im Spielstand (Schema 2) und Migration von Schema 1.
/// </summary>
public class FinanceSaveGameTests
{
    /// <summary>
    /// Werbeangebote werden seit finanzen-2 einzeln ersetzt und entfernt. Die Reihenfolge der Angebote muss danach
    /// nur vom Inhalt abhängen (nach Partner), sonst schreibt ein geladener Zwilling eine andere Datei
    /// (Zwillingsprobe nach dem Rebase auf #71 rot).
    /// </summary>
    [Fact]
    public void SponsorOffers_StayOrderedByPartner_AfterRefreshes()
    {
        var game = TestGames.Create(3);
        var me = game.Managers[0];
        for (var step = 0; step < 30 && game.Phase == SeasonPhase.Running; step++)
        {
            game.Continue();
            foreach (var (_, offers) in game.Ads(me).Offers)
                Assert.Equal(offers.Keys.Order(), offers.Keys);
        }
    }
}
