using EishockeyManager.Core;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>Saisonablauf: Ende der Hauptrunde, Play-Offs, Meisterfoto, Saisonende, neue Saison.</summary>
internal static class Saison
{
    [Scenario("saison")]
    private static void Season(ScreenDriver d)
    {
        d.StartNewGame();
        var game = d.Host.Game!;
        while (game.PlayedMatchDays < 43)
            d.PlayMatchDay();
        d.Move(10, 230); d.Save("01_vor_44_spieltag");

        // 44. Spieltag: Banner „Ligaspiel“, dann Banner „Play-Off / Aufstiegsrunden“ und Meisterfoto
        d.NextDay();
        d.Frame(); d.Save("02_banner_ligaspiel");
        if (Until<MatchDayScreen>(d, () => Banner(d) == "Play-Off / Aufstiegsrunden"))
        {
            d.Frame(); d.Save("03_banner_playoff");
        }
        if (Until<SeasonEndTrophyScreen>(d))
            d.Save("04_deutscher_meister");
        Until<MainMenuScreen>(d);
        d.Move(10, 230); d.Save("05_hauptmenue_saisonende");

        // 29. April: Saisonende
        d.NextDay();
        Until<SeasonEndScreen>(d);
        d.Move(170, 110); d.Save("06_saisonende");
        d.Press(Key.Space);
        d.Move(170, 110); d.Save("07_saisonende_ereignisse");
        d.Press(Key.Space);
        d.Move(10, 230); d.Save("08_neue_saison");
    }

    /// <summary>Stärkster Oberligist (SC Riessersee): Aufstiegsrunde, Saisonende mit Ereignissen.</summary>
    [Scenario("saison_aufstieg")]
    private static void Promotion(ScreenDriver d)
    {
        d.StartNewGame(clubIndex: 30);
        var game = d.Host.Game!;
        while (!game.RegularSeasonOver)
            d.PlayMatchDay();
        d.Move(10, 230); d.Save("01_nach_hauptrunde");
        d.NextDay();
        d.Frame(); d.Save("02_banner_aufstiegsrunde");
        Until<MainMenuScreen>(d);
        d.Move(10, 230); d.Save("03_hauptmenue_aufstiegsrunde");
        Until<SeasonEndScreen>(d);
        d.Move(170, 110); d.Save("04_saisonende");
        d.Press(Key.Space);
        d.Move(170, 110); d.Save("05_saisonende_ereignisse");
    }

    /// <summary>Weiter (Hauptmenü: Vereinslogo, sonst Leertaste), bis der gewünschte Bildschirm (und ggf. Zustand) erreicht ist.</summary>
    private static bool Until<T>(ScreenDriver d, Func<bool>? condition = null) where T : Screen
    {
        for (var i = 0; i < 400; i++)
        {
            if (d.Host.Current is T && (condition?.Invoke() ?? true))
                return true;
            if (d.Host.Current is MatchDayScreen && typeof(T) == typeof(MatchDayScreen))
                d.Frame();
            else
                d.Advance();
        }
        return false;
    }

    private static string? Banner(ScreenDriver d) => (d.Host.Current as MatchDayScreen)?.CurrentBanner;
}
