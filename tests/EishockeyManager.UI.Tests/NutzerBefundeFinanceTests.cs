using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Screens.ManagerArea;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>Spur nutzer-befunde, #839 (Schrift der rechten Spalte) und #840 (Finanzbuch über eigenen Knopf).</summary>
public class NutzerBefundeFinanceTests
{
    private static (ScreenDriver Driver, FinanceScreen Screen) Open()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.OpenTile("Finanzen");
        return (d, Assert.IsType<FinanceScreen>(d.Host.Current));
    }

    /// <summary>
    /// #839: Nutzerprobe zeigte die Beschriftungen zweifarbig (oben 212, unten 50). Original Datei 0x580AE–0x580DE: einfarbig
    /// 0x1E mit Schatten 6 auf den Grundlinien 0x6B + 2 + … (Tabelle DGROUP 0xDF0).
    /// </summary>
    [GameDataFact]
    public void SummaryLabels_AreSingleColored_WithShadow_OnOriginalBaselines()
    {
        var (d, _) = Open();
        var mini = d.Host.Assets.Mini;
        var pixels = d.Surface.Pixels;
        foreach (var (baseline, text) in new[] { (109, "Kontostand:"), (127, "Untereinander:"), (153, "Bank:"), (179, "Aktien:"),
                     (189, "Kaufpreis:"), (207, "Depot-Wert:"), (225, "Depot-Saldo:") })
        {
            var expected = new Surface();
            CompetitionWidgets.Centered(expected, mini, 249, 249, baseline, text, FinanceScreen.SummaryLabelColor,
                FinanceScreen.SummaryLabelShadow, left: true);
            var compared = 0;
            for (var i = 0; i < expected.Pixels.Length; i++)
            {
                if (expected.Pixels[i] == 0)
                    continue;
                Assert.Equal(expected.Pixels[i], pixels[i]);
                compared++;
            }
            Assert.True(compared > 10, text);
        }
    }

    /// <summary>#839: Beträge nach Vorzeichen gefärbt (Datei 0x5811F–0x5815B).</summary>
    [Fact]
    public void SummaryValueColor_FollowsSign()
    {
        Assert.Equal(12, FinanceScreen.SummaryValueColor(1));
        Assert.Equal(16, FinanceScreen.SummaryValueColor(-1));
        Assert.Equal(0x1E, FinanceScreen.SummaryValueColor(0));
    }

    /// <summary>#840: Klick auf Bankbild bzw. Kontostand wählt nur die Bank bzw. tut nichts (em.exe 0x59BB8); Knopf öffnet das Buch.</summary>
    [GameDataFact]
    public void BankPicture_OnlySelectsBank_BookButtonOpensBook()
    {
        var (d, screen) = Open();
        d.Click(FinanceScreen.BankPicture.X + 10, FinanceScreen.BankPicture.Y + 10);
        Assert.Same(screen, d.Host.Current);
        Assert.Equal(FinanceScreen.SelectionKind.Bank, screen.Selected.Kind);
        d.Click(270, 112); // Kontostand
        Assert.Same(screen, d.Host.Current);
        d.Click(FinanceScreen.BookButton.X + 5, FinanceScreen.BookButton.Y + 5);
        Assert.IsType<FinanceBookScreen>(d.Host.Current);
    }
}
