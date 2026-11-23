using EishockeyManager.Core;
using EishockeyManager.UI.Screens.Team;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>Spur abgleich-dosbox-22 (Runde 23), Teil 2: Anzeige im Verhandlungsfenster (A117, DOSBox-Bild <c>file76_11</c>).</summary>
public class AbgleichDosbox22UiTests
{
    /// <summary>
    /// A117: Eingabe 0 im Gehaltsfeld zeigt „0 DM“ (Bild <c>file76_11</c>: A.KRINNER, „Gehalt / Monat : 0 DM“); vorher „1 DM“.
    /// Der Kern bekommt weiter 1 DM (Gehalt 0 = „nicht eingegeben“), das Ergebnis „nicht interessiert“ ist gleich.
    /// </summary>
    [GameDataFact]
    public void ContractDialog_SalaryZero_ShowsZeroDm()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var p = game.Clubs[game.ActiveManager.ClubIndex].Players[0];
        var rect = ContractsScreen.DialogRect(24);

        var typed = new ContractDialog(p, rect, 2, 10_000, 0, () => 0, _ => null, () => null);
        var field = typed.SalaryField;
        typed.Update(new InputState { MouseX = field.X + 2, MouseY = field.Y + 2, LeftClick = true });
        typed.Update(new InputState { Text = "0" });
        var enter = new InputState();
        enter.Keys.Add(Key.Enter);
        typed.Update(enter);
        Assert.Equal(Contracts.MinSalary, typed.Salary);

        // Gleiches Bild wie ein Fenster, dessen Gehalt ohne Eingabe 0 DM ist.
        var zero = new ContractDialog(p, rect, 2, 0, 0, () => 0, _ => null, () => null);
        var a = new Surface();
        var b = new Surface();
        typed.Draw(a, d.Host.Assets);
        zero.Draw(b, d.Host.Assets);
        Assert.Equal(b.Pixels, a.Pixels);
    }
}
