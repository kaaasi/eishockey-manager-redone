using System.Text.Json.Nodes;
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

    /// <summary>
    /// Ein Spielstand mit Schema 1 (vor finanzen-2) lädt: Nationalkonto leer (Etat lag schon auf dem Vereinskonto),
    /// Kostenstufe = Liga des Vereins, keine aufgelaufenen Zinsen, Werbung ohne Option, und „Gastronomie“ geht im
    /// Finanzbuch in „Eintritt“ auf – die Summe bleibt gleich.
    /// </summary>
    [Fact]
    public void SchemaOneSave_IsMigratedToCurrentFinanceFields()
    {
        var game = TestGames.Create(3);
        var me = game.Managers[0];
        for (var day = 0; day < 40; day++)
            game.Continue();
        var shirt = game.Ads(me).Offers[AdSlotKind.Trikot].Keys.First();
        Assert.NotNull(game.AcceptOffer(me, new AdSlot(AdSlotKind.Trikot), shirt));
        game.AppointNationalCoach(me);
        var bookBefore = Enum.GetValues<FinanceCategory>().Select(c => game.Finance(me).Book.Current(c)).ToArray();
        var totalBefore = game.Finance(me).Book.CurrentTotal;

        var root = JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        DowngradeToSchemaOne(root, gastronomy: 1_234);

        var result = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.True(result.Success, result.Error);
        var loaded = result.Game!;
        var loadedMe = loaded.Managers[0];
        var finance = loaded.Finance(loadedMe);

        Assert.Equal(0, loaded.NationalAccount);
        Assert.Equal(loaded.Clubs[loadedMe.ClubIndex].League ?? League.Oberliga, finance.CostLeague);
        Assert.Equal(0, finance.InterestAccrualThirds);
        Assert.Equal(0, finance.ManagerLoanAccrualThirds);
        Assert.All(finance.Sponsoring.Contracts, c => Assert.Equal(0, c.OptionMonths));
        Assert.All(finance.Sponsoring.Offers.Values.SelectMany(o => o.Values), o => Assert.Equal(0, o.OptionMonths));
        Assert.Equal(bookBefore[0] + 1_234, finance.Book.Current(FinanceCategory.Eintritt));
        Assert.Equal(bookBefore.Skip(1), Enum.GetValues<FinanceCategory>().Skip(1).Select(c => finance.Book.Current(c)));
        Assert.Equal(totalBefore + 1_234, finance.Book.CurrentTotal);

        // Der migrierte Stand ist wieder ein gültiger Spielstand der aktuellen Version.
        var again = SaveGame.Save(loaded);
        Assert.Equal(again, SaveGame.Save(SaveGame.Load(again).Game!));
    }

    /// <summary>Baut aus einer Datei der aktuellen Version eine Datei mit dem Feldbestand von Schema 1 (main 6b8dc00).</summary>
    private static void DowngradeToSchemaOne(JsonObject root, long gastronomy)
    {
        root["schema"] = 1;
        root["spiel"]!.AsObject().Remove("NationalAccount");
        foreach (var manager in root["manager"]!.AsArray())
        {
            var finance = manager!["FinanceData"]!.AsObject();
            finance.Remove("CostLeague");
            finance.Remove("InterestAccrualThirds");
            finance.Remove("ManagerLoanAccrualThirds");
        }
        Walk(root);

        void Walk(JsonNode? node)
        {
            switch (node)
            {
                case JsonObject obj:
                    obj.Remove("OptionMonths");
                    if (obj.Count == 3 && obj["_current"] is JsonObject current && obj["_previous"] is JsonObject previous)
                    {
                        current["items"]!.AsArray().Insert(1, gastronomy);
                        previous["items"]!.AsArray().Insert(1, 0);
                    }
                    foreach (var (_, child) in obj.ToArray())
                        Walk(child);
                    break;
                case JsonArray array:
                    foreach (var child in array.ToArray())
                        Walk(child);
                    break;
            }
        }
    }
}
