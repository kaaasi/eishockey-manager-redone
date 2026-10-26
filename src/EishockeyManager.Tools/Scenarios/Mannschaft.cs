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
        d.Click(60, 30); d.Click(264, 27); d.Save("03_falscher_platz");
        d.Click(10, 10); d.Click(60, 36); d.Click(282, 207); d.Move(282, 207); d.Save("04_torhueter_getauscht");
        d.RightClick(264, 27); d.Move(212, 157); d.Save("05_platz_geleert");
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
}
