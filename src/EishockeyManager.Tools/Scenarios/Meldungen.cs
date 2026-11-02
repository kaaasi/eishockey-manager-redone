using EishockeyManager.UI;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>Spur ui-meldungen: Bildschirmfotos der Meldungsbausteine (Fenster 0x4C246, Kurzmeldung 0x0C0B8).</summary>
internal static class Meldungen
{
    /// <summary>Hintergrund (Auslosungstafel) mit einem Baustein darüber; nur für Bildschirmfotos.</summary>
    private sealed class DialogScreen(ScreenHost host, OriginalDialog? dialog, ShortMessage? shortMessage) : Screen(host)
    {
        public override void Update(InputState input)
        {
            dialog?.Update(input);
            shortMessage?.Update(input);
        }

        public override void Draw(Surface s)
        {
            DebCupDrawScreen.DrawBoard(s, Assets, Host.Game, new int?[16]);
            dialog?.Draw(s, Assets);
            shortMessage?.Draw(s, Assets);
        }
    }

    private static void Show(ScreenDriver d, string name, OriginalDialog? dialog, ShortMessage? shortMessage = null)
    {
        d.Host.Show(new DialogScreen(d.Host, dialog, shortMessage));
        d.Move(170, 170);
        d.Save(name);
    }

    [Scenario("meldungen")]
    private static void All(ScreenDriver d)
    {
        d.StartNewGame();
        // Frage der DEB-Pokal-Auslosung (Video 3:46, Referenzbild oberflaeche_0346)
        Show(d, "01_frage_deb_pokal", new OriginalDialog(OriginalDialogKind.Question, "", "Sind Ihre Nerven stark genug,",
            "um die DEB-Pokal-Auslosung", "live zu verfolgen ?", "Aber klar", "Kein Gedanke"));
        // Meldung mit zwei Zeilen und mit drei Zeilen und Knöpfen
        Show(d, "02_meldung_zwei_zeilen", new OriginalDialog(OriginalDialogKind.Message, "LetsShow", "Wollen Sie die WM wirklich",
            "mit SO wenigen Spielern bestreiten ?", "", "", "Na gut."));
        Show(d, "03_meldung_drei_zeilen", new OriginalDialog(OriginalDialogKind.Message, "LetsShow", "Erste Zeile", "Zweite Zeile",
            "Dritte Zeile", "", "Na gut."));
        Show(d, "04_meldung_vier_zeilen", new OriginalDialog(OriginalDialogKind.Message, "LetsShow", "Erste Zeile", "Zweite Zeile",
            "Dritte Zeile", "Vierte Zeile", "Na gut."));
        // Kurzmeldung ohne Knopf (Bundestrainer-Warnung)
        Show(d, "05_kurzmeldung", null, new ShortMessage("LetsShow", "Wollen Sie die WM wirklich", "mit SO wenigen Spielern bestreiten ?"));
    }
}
