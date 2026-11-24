using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Statistics;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur bedienung, B11 (= A, Nutzer 24.11.2026): „Jede Taste schließt“ wie im Original, Escape und rechte Maustaste bleiben als
/// Komfort. Fair-Play-Tabelle (Datei 0x2A5DD) und Bestenliste (Datei 0x5DBD8) warten in 0x38:0x118 (Datei 0xB908) nur auf ein
/// Mausereignis (DGROUP 0x5A16 = 1 oder 3), nicht auf Tasten; die Vereinsstatistik schließt wie die Fair-Play-Tabelle.
/// Vorher rot: jede Taste schloss.
/// </summary>
public class BedienungTastenTests
{
    public static TheoryData<string> Screens => new() { "Fairplay", "Vereinsstatistik", "Highscore" };

    private static Screen Open(ScreenDriver d, string name) => name switch
    {
        "Fairplay" => new FairplayScreen(d.Host, d.Host.Current!),
        "Vereinsstatistik" => new ClubStatisticsScreen(d.Host, d.Host.Current!),
        _ => new HighscoreScreen(d.Host, d.Host.Current!),
    };

    [GameDataTheory]
    [MemberData(nameof(Screens))]
    public void OtherKeys_DoNotClose_EscapeAndMouseDo(string name)
    {
        foreach (var close in new Action<ScreenDriver>[] { d => d.Press(Key.Escape), d => d.Click(160, 120), d => d.RightClick(160, 120) })
        {
            var d = FlowTests.CreateDriver();
            d.StartNewGame();
            var menu = d.Host.Current!;
            var screen = Open(d, name);
            d.Host.Show(screen);
            d.Frame();
            foreach (var key in new[] { Key.Enter, Key.Space, Key.Backspace, Key.Up, Key.PageDown })
            {
                d.Press(key);
                Assert.Same(screen, d.Host.Current);
            }
            d.Type("x");
            Assert.Same(screen, d.Host.Current);
            close(d);
            Assert.Same(menu, d.Host.Current);
        }
    }
}
