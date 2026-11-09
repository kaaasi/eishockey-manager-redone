using EishockeyManager.UI;
using EishockeyManager.UI.Screens.Team;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>Bildschirme des Pakets „mannschaft“ (Vergleich mit dem UI-Video ab ca. 9:00).</summary>
internal static class Mannschaft
{
    [Scenario("aufstellung")]
    private static void Lineup(ScreenDriver d)
    {
        d.StartNewGame();
        d.OpenTile("Aufstellung");
        d.Move(212, 157); d.Save("01_aufstellung");
        d.Move(170, 48); d.Save("02_hover_strafen");
        // Bedienung wie im Original (A010, Datei 0x40890–0x414C6): Reihe per Klick in den Block wählen, Linksklick auf einen
        // Spieler setzt ihn auf den ersten freien Platz seiner Art, Linksklick auf einen belegten Platz leert ihn,
        // Rechtsklick auf einen Platz verlässt den Bildschirm.
        // Ohne diesen Schritt erscheint bei der ersten Änderung „Auto-Aufstellung ist deaktiviert!“ (Datei 0x40DCD–0x40DE4) und
        // verdeckt die Bilder; hier gezielt abgeschaltet wie in `aufstellung-reihen`.
        d.Host.Game!.Clubs[d.Host.Game.ActiveManager.ClubIndex].AutoLineupEnabled = false;
        d.Click(250, 50); d.Click(60, 36); d.Save("03_falscher_platz");
        d.Frames(300); d.Click(60, 30); d.Click(60, 36); d.Move(282, 207); d.Save("04_torhueter_getauscht");
        d.Click(264, 27); d.Move(212, 157); d.Save("05_platz_geleert");
        d.RightClick(264, 27); d.Save("06_rechtsklick_verlaesst");
    }

    [Scenario("vertraege")]
    private static void Contracts(ScreenDriver d)
    {
        d.StartNewGame();
        d.OpenTile("Verträge");
        d.Move(148, 34); d.Save("01_vertraege");
        d.Click(60, 31); d.Move(93, 153); d.Save("02_verhandlung");
        d.Click(100, 136); d.Type("4"); d.Save("03_eingabe_dauer");
        d.Press(Key.Enter); d.Click(100, 152); d.Click(30, 164); d.Save("04_angenommen");
        d.Click(10, 10); d.Move(148, 34); d.Save("05_vier_jahre");
        d.Click(60, 145); d.Move(93, 70); d.Save("06_verhandlung_oben");
    }

    [Scenario("taktik")]
    private static void Tactics(ScreenDriver d)
    {
        d.StartNewGame();
        d.OpenTile("Taktik");
        d.Move(300, 230); d.Save("01_taktik");
        d.Click(205, 84); d.Save("02_einsatz_semi");
        d.Click(212, 41); d.Click(181, 104); d.Click(181, 104); d.Save("03_block2_grundwert");
        d.Click(118, 120); d.Click(118, 120); d.Click(118, 120); d.Click(118, 120); d.Click(41, 148); d.Save("04_minuten");
        d.Click(270, 230); d.Save("05_zur_aufstellung");
    }

    [Scenario("transfer")]
    private static void Transfer(ScreenDriver d)
    {
        d.StartNewGame();
        d.OpenTile("Transfer");
        d.Move(190, 112); d.Save("01_transfer");
        d.Click(190, 72); d.Move(240, 200); d.Save("02_spielerinfo");
        d.Click(196, 213); d.Click(200, 47); d.Move(160, 140); d.Save("03_kaufen");
        d.Press(Key.Enter); d.Save("04_ergebnis");
        d.Click(10, 10); d.Click(280, 231); d.Save("05_talentsuche");
        d.Press(Key.Escape); d.Click(60, 75); d.Save("06_verkaufen");
    }

    [Scenario("training")]
    private static void Training(ScreenDriver d)
    {
        d.StartNewGame();
        d.OpenTile("Training");
        d.Move(300, 20); d.Save("01_training");
        d.Click(70, 225); d.Drag((20, 110), (64, 110), (108, 110), (152, 110)); d.Save("02_frei_ueberstrichen");
        d.Click(40, 225); d.Click(108, 172); d.Click(284, 172); d.Move(300, 20); d.Save("03_zwei_sonder");
        d.Click(70, 225);
        for (var day = 0; day < 7; day++)
            d.Drag((20 + 44 * day, 110), (20 + 44 * day, 130), (20 + 44 * day, 175), (20 + 44 * day, 195));
        d.Click(40, 225); d.Click(108, 172); d.Move(300, 20); d.Save("04_wie_video_1130");
        d.RightClick(160, 120); d.PlayMatchDay(); d.PlayMatchDay(); d.OpenTile("Training"); d.Save("05_nach_zwei_spieltagen");
    }
}
