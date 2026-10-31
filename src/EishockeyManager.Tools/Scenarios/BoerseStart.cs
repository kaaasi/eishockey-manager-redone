using EishockeyManager.Core;
using EishockeyManager.UI;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>
/// Spur boerse-start (#169): Bildschirm Finanzen zu Spielbeginn. Vergleich mit <c>folge01_00510</c> (nur im privaten
/// Referenz-Repo): Kennzahl und Vorgehen in <c>docs/finanzen-rekonstruktion.md</c>, Abschnitt „Börse“.
/// </summary>
internal static class BoerseStart
{
    /// <summary>
    /// Kursverläufe aus <c>folge01_00510</c> zurückgerechnet (Oberkante der Linie je Tag bei x = 134 + 3·Tag, Versatz 160
    /// aus den Endpunkten; ±1 DM durch Videounschärfe), letzter Tag = angezeigte Kurse 98 / 96 / 134 / 126 DM.
    /// </summary>
    internal static readonly int[][] Folge01History =
    [
        [97, 96, 95, 96, 95, 96, 97, 98, 98, 98, 98, 97, 97, 97, 97, 96, 96, 96, 96, 96, 96, 97, 97, 99, 99, 98, 99, 99, 99, 97, 96, 96, 96, 96, 97, 98],
        [91, 91, 91, 91, 91, 91, 92, 92, 92, 91, 90, 90, 91, 91, 92, 93, 94, 94, 94, 95, 94, 96, 95, 94, 94, 94, 95, 96, 96, 96, 95, 95, 95, 95, 96, 96],
        [131, 132, 131, 130, 129, 130, 130, 129, 128, 128, 129, 131, 131, 132, 131, 129, 130, 130, 131, 132, 133, 133, 133, 132, 131, 132, 133, 133, 133, 133, 134, 134, 135, 135, 134, 134],
        [111, 111, 109, 105, 105, 109, 113, 114, 115, 116, 116, 115, 115, 115, 112, 113, 115, 115, 116, 115, 119, 123, 124, 124, 124, 123, 123, 125, 126, 127, 128, 128, 127, 127, 125, 126],
    ];

    /// <summary>Börse im Zustand von folge01 (Basiswerte und Tendenzen sind für das Bild ohne Bedeutung).</summary>
    internal static StockMarket Folge01Market()
    {
        var stocks = StockMarket.Defaults.Select((d, i) =>
        {
            var stock = new Stock { Name = d.Name, Base = d.Base, Price = Folge01History[i][^1] };
            stock.History.AddRange(Folge01History[i]);
            return stock;
        }).ToList();
        return new StockMarket { Stocks = stocks };
    }

    [Scenario("boerse-start")]
    private static void Run(ScreenDriver d)
    {
        // Spielbeginn mit ausgewürfelten Kursen (90 Börsentage, em.exe 0x43AD5) und dem Ausschnitt nach 0x58BD4.
        d.StartNewGame();
        d.OpenTile("Finanzen"); d.Move(319, 239); d.Save("01_finanzen_spielbeginn");
        d.Press(Key.Escape);

        // Kurse und Verlauf wie folge01_00510, Kontostand wie im Video (Spiel-Level 5: 400.000 DM).
        var game = d.Host.Game!;
        game.ActiveManager.Money = 400_000;
        game.Stocks = Folge01Market();
        d.OpenTile("Finanzen"); d.Move(319, 239); d.Save("02_finanzen_folge01_00510");
    }
}
