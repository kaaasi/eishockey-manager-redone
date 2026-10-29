using EishockeyManager.UI;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>
/// Spur debpokal-auslosung: Live-Auslosung im Spielablauf. Vergleich: Frage mit <c>oberflaeche_0346.png</c>, einzelner
/// Platz mit <c>oberflaeche_0350.png</c>, fertige Ziehung mit <c>oberflaeche_0355.png</c>, Übersicht mit <c>oberflaeche_0400.png</c>.
/// </summary>
internal static class DebPokalAuslosung
{
    [Scenario("debpokal-auslosung")]
    private static void Live(ScreenDriver d)
    {
        d.StopAtDebCupDraw = true;
        d.StartNewGame(); // Berliner SC wie im Video

        // Spielstart: Frage an alle (Video 3:40–3:46)
        d.Frame(); d.Save("01_frage_spielstart");

        // „Aber klar“: erster Platz aufgedeckt (Video 3:50)
        var yes = DebCupDrawLiveScreen.YesButton;
        d.Click(yes.X + 5, yes.Y + 3);
        for (var i = 0; i < DebCupDrawLiveScreen.RevealFrames; i++)
            d.Frame();
        d.Save("02_erster_platz");

        // Taste: alle übrigen Plätze, „TASTE DRÜCKEN“ (Video 3:55)
        d.Press(Key.Space);
        d.Save("03_fertig");

        // Danach die Übersicht „DEB-Pokalspiele“ (Video 4:00)
        d.Press(Key.Space);
        d.Save("04_uebersicht");
        d.Press(Key.Space);

        // Nach dem dritten Termin der ersten Runde: Frage zur Auslosung des Achtelfinales im Spieltagsablauf
        for (var guard = 0; d.Host.Current is not DebCupDrawLiveScreen && guard < 5000; guard++)
            d.Press(Key.Space);
        if (d.Host.Current is not DebCupDrawLiveScreen { Round: 1 })
            throw new InvalidOperationException("Auslosung des Achtelfinales nicht erreicht.");
        d.Save("05_frage_achtelfinale");
        d.Press(Key.Space);
        d.Press(Key.Space);
        d.Save("06_achtelfinale");
    }
}
