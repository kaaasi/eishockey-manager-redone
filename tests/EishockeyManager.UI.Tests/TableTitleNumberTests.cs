using EishockeyManager.Core;
using EishockeyManager.UI.Screens;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur wettbewerbe-komplett Teil 1: Spieltagszahl im Titel der Tabellenansicht nach em.exe 0x4F78D, Abzug Datei 0x4F986.
/// </summary>
public class TableTitleNumberTests
{
    /// <summary>
    /// [ORIGINAL] Datei 0x4F986–0x4F9DE: Zahl = DGROUP 0x7A9[Liga] − 1, wenn der zweite Parameter 1 ist und 0x7A9 &gt; 1,
    /// sonst 0x7A9. Parameter 1 übergibt das Hauptmenü (Datei 0x4583B, Kachel „Tabelle“), Parameter 0 die automatische
    /// Tabelle nach dem Spieltag (Datei 0x37882, Schalter DGROUP 0x21F + 3 · Liga) – dort noch vor dem Weiterzählen von
    /// 0x7A9 (Datei 0x37897). 0x7A9 = 1 beim Spielstart (0x43A7E) und zum Saisonbeginn (0x5613E).
    /// </summary>
    private static int OriginalNumber(int caller, int counter) => caller == 1 && counter > 1 ? counter - 1 : counter;

    [Fact]
    public void Title_NumberLikeBothCallersOfTheOriginal()
    {
        for (var played = 0; played <= 44; played++)
        {
            var title = TableScreen.Title(TableView.Gesamt, played);
            // Hauptmenü: 0x7A9 steht nach dem Weiterzählen auf „gespielte Spieltage + 1“.
            Assert.Equal($"   TABELLE Gesamt {OriginalNumber(1, played + 1)}.Spieltag", title);
            // Automatische Tabelle: 0x7A9 steht noch auf dem eben gespielten Spieltag.
            if (played > 0)
                Assert.Equal($"   TABELLE Gesamt {OriginalNumber(0, played)}.Spieltag", title);
        }
    }
}
