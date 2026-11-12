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
    public const int BannerBaseline = 126;

    /// <summary>Orangefarbenes Banner mit großer Schrift über dem Sternenhintergrund (z. B. „Ligaspiel“).</summary>
    public static void DrawBanner(Surface s, Assets a, string text)
    {
        Widgets.Frame(s, a, Banner, FrameStyle.Orange);
        // [ORIGINAL] em.exe Datei 0x35D5D (R-K3): Text zentriert, zuerst Farbe 0 auf Grundlinie 127, dann Farbe 11 auf 126 –
        // der Schatten liegt nur eine Zeile tiefer, nicht seitlich versetzt.
        var x = 160 - a.Bold.Measure(text) / 2;
        s.Text(a.Bold, x, Competitions.CompetitionWidgets.Top(a.Bold, BannerBaseline + 1), text, 0);
        s.Text(a.Bold, x, Competitions.CompetitionWidgets.Top(a.Bold, BannerBaseline), text, 11);
    }

    /// <summary>Vereinsname zweizeilig wie auf dem Saisonende-Bildschirm: erstes Wort, dann der Rest.</summary>
    public static (string First, string Second) SplitName(string name)
    {
        var space = name.IndexOf(' ');
        return space < 0 ? (name, "") : (name[..space], name[(space + 1)..]);
    }

    /// <summary>
    /// Liga im Satz „4. Platz der Oberliga“. [ORIGINAL] Tabelle DGROUP 0x9EC + 4 · Liga (Datei 0x2C163 in 0x2C099): „Bundesliga“,
    /// „Zweite Liga“, „Oberliga“ – nicht die Tabelle 0x9E0 („Zweiten Liga“) des Vereinsfensters. DOSBox-Bild <c>file16_9</c>
    /// „1. Platz der Zweite Liga“; ebenso das Karriere-Video (<c>FinanceCalibrationTests</c>).
    /// </summary>
    public static string LeagueGenitive(League league) => league switch
    {
        League.Bundesliga => "Bundesliga",
        League.ZweiteLiga => "Zweite Liga",
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
