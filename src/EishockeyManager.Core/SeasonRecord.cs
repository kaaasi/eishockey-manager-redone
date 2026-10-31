namespace EishockeyManager.Core;

/// <summary>Wettbewerbe einer Saison (Texte im Original: „Liga“, „DEB-Pokal“, „Europapokal“, „Play-Off“ …).</summary>
public enum Competition
{
    League,
    DebCup,
    EuropeanCup,
    WorldCup,

    /// <summary>Play-Offs um die Deutsche Meisterschaft.</summary>
    Playoffs,

    /// <summary>Aufstiegsrunden zwischen den Ligen.</summary>
    Relegation,
}

/// <summary>Titel, die am Saisonende angezeigt werden.</summary>
public enum Title
{
    WorldChampion,
    CupWinner,
    GermanChampion,
    EuropeanCupWinner,
}

/// <summary>Ereignisse eines Managers in der Saison (Symbole aus kb\13 im Fenster „Saisonende“).</summary>
public enum SeasonEvent
{
    Promotion,
    Relegation,
    GermanChampion,
    CupWinner,
    EuropeanCupWinner,

    // Spur statistiken (#135): Einzelprämien des Saisonendes (Datei 0x27184–0x2725D), siehe GameState.SinglePremiumEvents.
    /// <summary>Bester Torhüter der eigenen Liga im Kader (Symbol 12).</summary>
    BestGoalie,

    /// <summary>Topscorer der eigenen Liga im Kader (Symbol 3).</summary>
    TopScorer,

    /// <summary>Erster der Fair-Play-Tabelle der eigenen Liga (Symbol 13).</summary>
    FairPlay,
}

/// <summary>Abschneiden eines Managers in einer Saison (Fenster „Saisonende“).</summary>
public sealed record ManagerSeasonResult(int Manager, int Club, League League, int Rank, long Premium, IReadOnlyList<SeasonEvent> Events);

/// <summary>Ergebnis einer abgeschlossenen Saison – Grundlage für „Saisonende“ und die Statistik „Früher“.</summary>
public sealed class SeasonRecord
{
    public required int Season { get; init; }

    /// <summary>Abschlusstabellen der Hauptrunde (Vereinsindizes, Platz 1 zuerst).</summary>
    public required IReadOnlyDictionary<League, IReadOnlyList<int>> FinalTables { get; init; }

    /// <summary>Titelträger; fehlt ein Titel (Wettbewerb nicht ausgetragen), zeigt das Original „Wer weiß...“.</summary>
    public Dictionary<Title, int> Titles { get; } = [];

    /// <summary>Vereine, die in der nächsten Saison neu in dieser Liga spielen, weil sie aufgestiegen sind
    /// (für die Oberliga: neu aus der Regionalliga). Im Original grün mit Pfeil nach oben.</summary>
    public Dictionary<League, List<int>> PromotedInto { get; } = Enum.GetValues<League>().ToDictionary(l => l, _ => new List<int>());

    /// <summary>Vereine, die in der nächsten Saison neu in dieser Liga spielen, weil sie abgestiegen sind (rot, Pfeil nach unten).</summary>
    public Dictionary<League, List<int>> RelegatedInto { get; } = Enum.GetValues<League>().ToDictionary(l => l, _ => new List<int>());

    /// <summary>Vereine, die die Oberliga verlassen (ohne Liga in der nächsten Saison).</summary>
    public List<int> LeftOberliga { get; } = [];

    public List<ManagerSeasonResult> Managers { get; } = [];

    /// <summary>
    /// Stand der Abschlusstabelle für Statusbit 0x40: Steht der Verein unter den ersten fünf der 1. Liga (Plätze 1–5 der Bundesliga)?
    /// Abgeleitet, nicht gespeichert – die Abschlusstabelle liegt schon im Spielstand (siehe <see cref="GameState.HasTopFiveStatus"/>).
    /// </summary>
    public bool IsTopFive(int club) => FinalTables[League.Bundesliga].Take(5).Contains(club);

    /// <summary>Liga eines Vereins in der nächsten Saison nach Auf- und Abstieg.</summary>
    public League? NextLeague(int club, League? current)
    {
        foreach (var league in Enum.GetValues<League>())
            if (PromotedInto[league].Contains(club) || RelegatedInto[league].Contains(club))
                return league;
        return LeftOberliga.Contains(club) ? null : current;
    }
}

/// <summary>Ein Schritt, den die Oberfläche nach „und tschüß…“ anzeigt.</summary>
public abstract record SeasonStep(int Day);

/// <summary>An diesem Tag wurden Spiele eines Wettbewerbs ausgetragen.</summary>
public sealed record MatchesStep(int Day, Competition Competition, bool ManagerInvolved) : SeasonStep(Day);

/// <summary>Banner wie „Play-Off / Aufstiegsrunden“ oder „Europacup“.</summary>
public sealed record BannerStep(int Day, string Text) : SeasonStep(Day);

/// <summary>Meisterfoto (kb\16) mit Titel und Sieger.</summary>
public sealed record TrophyStep(int Day, Title Title, int Club) : SeasonStep(Day);

/// <summary>Ein Manager wurde wegen zu hoher Schulden entlassen.</summary>
public sealed record DismissalStep(int Day, int Manager) : SeasonStep(Day);

/// <summary>Saisonende erreicht: Bildschirm „Saisonende“, danach Fenster mit dem eigenen Abschneiden.</summary>
public sealed record SeasonEndStep(int Day) : SeasonStep(Day);

/// <summary>
/// Schnittstelle für weitere Wettbewerbe (DEB-Pokal, Europapokal, WM – Paket <c>wettbewerbe</c>).
/// Registrierte Wettbewerbe (<see cref="GameState.Competitions"/>) werden an ihren Kalendertagen aufgerufen.
/// </summary>
public interface ISeasonCompetition
{
    Competition Kind { get; }

    /// <summary>Kalenderkennung der Spieltage, z. B. <see cref="DayEvents.DebCup"/>.</summary>
    DayEvents Days { get; }

    /// <summary>Titel, den der Sieger am Saisonende erhält, oder null.</summary>
    Title? Title { get; }

    /// <summary>Neue Saison: Auslosung, Teilnehmer …</summary>
    void StartSeason(GameState game);

    /// <summary>Spiel eines Vereins an diesem Tag (für Infoleiste und die Frage, ob „und tschüß…“ hier anhält).</summary>
    (int Opponent, bool Home)? MatchOn(GameState game, int day, int club);

    /// <summary>Spielt alle Spiele des Tages; darf Banner und Pokalfotos in <paramref name="steps"/> eintragen.</summary>
    void PlayDay(GameState game, int day, List<SeasonStep> steps);

    /// <summary>Sieger des Wettbewerbs dieser Saison oder null.</summary>
    int? Winner { get; }
}
