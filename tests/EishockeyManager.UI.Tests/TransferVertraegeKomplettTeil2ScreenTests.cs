using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens.Team;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur <c>transfer-vertraege-komplett</c>, Teil 2 über die Oberfläche: Feld „Versicherung“ im Verhandlungsfenster
/// (em.exe 0x4AA74–0x4AB1C, R8W3I-07). Vorher rot: Umschalten immer möglich, Anzeige „abgeschlossen“ statt des Betrags.
/// </summary>
public class TransferVertraegeKomplettTeil2ScreenTests
{
    [GameDataFact]
    public void InsuranceField_ShowsPremium_AndRespectsLock()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        var rows = TeamUi.Rows(club).ToList();
        var p = rows.First(x => !x.OnLoan);
        var other = rows.Last(x => !x.OnLoan && x != p);
        p.InsurancePremium = 0;
        p.InsuranceLocked = false;
        other.InsurancePremium = 777;
        other.InsuranceLocked = true;
        var save = SaveGame.Save(game);

        d.OpenTile("Verträge");
        var screen = Assert.IsType<ContractsScreen>(d.Host.Current);
        d.Click(60, ContractsScreen.FirstRowY + rows.IndexOf(p) * ContractsScreen.RowPitch + 2);
        var dialog = Assert.IsType<ContractDialog>(screen.Dialog);
        var field = dialog.InsuranceField;
        d.Click(field.X + 5, field.Y + 3);
        var offer = Contracts.InsuranceOffer(game, game.ActiveManager, p);
        Assert.Equal(offer, dialog.Insurance);
        d.Click(field.X + 5, field.Y + 3);
        Assert.Equal(0, dialog.Insurance);
        d.Press(Key.Escape);
        Assert.Equal(save, SaveGame.Save(game)); // Umschalten und Abbruch ändern nichts (0x4AB6E → 0x4AD54)

        d.Click(60, ContractsScreen.FirstRowY + rows.IndexOf(other) * ContractsScreen.RowPitch + 2);
        dialog = Assert.IsType<ContractDialog>(screen.Dialog);
        Assert.Equal(777, dialog.Insurance);
        d.Click(dialog.InsuranceField.X + 5, dialog.InsuranceField.Y + 3);
        Assert.Equal(777, dialog.Insurance); // abgeschlossen: nicht kündbar
    }
}
