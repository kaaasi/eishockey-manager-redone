using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens;

/// <summary>Gemeinsame Bausteine des Saisonablaufs: Wettbewerbssymbole, Banner, Texte.</summary>
public static class SeasonInfo
{
    /// <summary>Symbole 14 × 11 in kb\3 (Reihe y = 231): Liga, DEB-Pokal, WM, Europapokal, Saisonende, Play-Off.</summary>
    public static Rect IconSource(Competition? competition) => new(competition switch
    {
        Competition.DebCup => 92,
        Competition.WorldCup => 106,
        Competition.EuropeanCup => 120,
        Competition.Playoffs or Competition.Relegation => 148,
        _ => 78,
    }, 231, 14, 11);

    public static readonly Rect SeasonEndIcon = new(134, 231, 14, 11);

    /// <summary>Wettbewerbssymbol der Infoleiste; nach der Vereinssaison die Zielflagge („Saisonende“).</summary>
    public static Rect CompetitionIcon(GameState game, Competition? competition) =>
        game.Phase == SeasonPhase.Running ? IconSource(competition) : SeasonEndIcon;

    /// <summary>Text im Gegnerfeld der Infoleiste ohne weiteres Spiel.</summary>
    public static string NoMatchText(GameState game) => game.Phase == SeasonPhase.Running ? "" : "Saisonende";

    /// <summary>Banner vor den Spielen eines Wettbewerbs (Texte aus em.exe).</summary>
    public static string BannerText(Competition competition) => competition switch
    {
        Competition.League => "Ligaspiel",
        Competition.DebCup => "DEB-Pokal",
        Competition.EuropeanCup => "Europacup",
        Competition.WorldCup => "Weltmeisterschaft",
        _ => "Play-Off / Aufstiegsrunden",
    };

    /// <summary>Titelzeile des Meisterfotos (belegt: „Deutscher Meister“, „DEB Pokalsieger“, „Europacupsieger“).</summary>
    public static string TrophyTitle(Title title) => title switch
    {
        Title.GermanChampion => "Deutscher Meister",
        Title.CupWinner => "DEB Pokalsieger",
        Title.EuropeanCupWinner => "Europacupsieger",
        _ => "Weltmeister",
    };

    public static readonly Rect Banner = new(3, 92, 314, 57);

    /// <summary>Orangefarbenes Banner mit großer Schrift über dem Sternenhintergrund (z. B. „Ligaspiel“).</summary>
    public static void DrawBanner(Surface s, Assets a, string text)
    {
        Widgets.Frame(s, a, Banner, FrameStyle.Orange);
        Widgets.ShadowText(s, a.Bold, 160, 110, text, 11, 198);
    }

    /// <summary>Vereinsname zweizeilig wie auf dem Saisonende-Bildschirm: erstes Wort, dann der Rest.</summary>
    public static (string First, string Second) SplitName(string name)
    {
        var space = name.IndexOf(' ');
        return space < 0 ? (name, "") : (name[..space], name[(space + 1)..]);
    }

    /// <summary>Liga im Satz „4. Platz der Oberliga“. [APPROX] Belegt ist nur „der Oberliga“.</summary>
    public static string LeagueGenitive(League league) => league switch
    {
        League.Bundesliga => "Bundesliga",
        League.ZweiteLiga => "Zweiten Liga",
        _ => "Oberliga",
    };

    /// <summary>Überschriften der Ligen auf dem Saisonende-Bildschirm.</summary>
    public static string LeagueName(League league) => league switch
    {
        League.Bundesliga => "Bundesliga",
        League.ZweiteLiga => "Zweite Liga",
        _ => "Oberliga",
    };
}
