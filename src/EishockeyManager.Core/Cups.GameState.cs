namespace EishockeyManager.Core;

/// <summary>Spielstand – Teil des Pakets „wettbewerbe“: Anmeldung und Abrechnung der Pokalwettbewerbe.</summary>
public sealed partial class GameState
{
    private Random? _cupRandom;

    /// <summary>
    /// Eigene Zufallsquelle der Pokale (Auslosung, Pokalspiele). Sie wird beim Spielstart einmal aus
    /// <see cref="Random"/> abgeleitet – unabhängig vom Spielmodus –, danach laufen Liga, Play-Offs und
    /// Aufstiegsrunden gleich, egal ob DEB-Pokal oder Europapokal/WM eingeschaltet sind.
    /// </summary>
    public Random CupRandom
    {
        get => _cupRandom ??= new Random(unchecked(Season * 6271 + Managers.Sum(m => m.ClubIndex * 37 + m.Head * 11) + Random.Next()));
        init => _cupRandom = value;
    }

    /// <summary>DEB-Pokal dieser Saison oder null (Spielmodus „DEB-Pokal aus“).</summary>
    public DebCup? DebCup => Competitions.OfType<DebCup>().FirstOrDefault();

    /// <summary>
    /// Spielstart: Wettbewerbe nach dem Spielmodus der Spielvorbereitung anmelden und die erste Saison auslosen.
    /// Wird einmal aus <see cref="InitializeManagerSystems"/> aufgerufen; jede weitere Saison startet
    /// <see cref="StartNextSeason"/> über <see cref="ISeasonCompetition.StartSeason"/>.
    /// </summary>
    internal void InitializeCompetitions()
    {
        if (Competitions.Count > 0)
            return;
        _ = CupRandom;
        if (Setup.DebCup)
            Competitions.Add(new DebCup());
        if (Setup.EuropeAndWorldCup)
        {
            Competitions.Add(new EuropeanCup());
            Competitions.Add(new WorldCup());
        }
        foreach (var competition in Competitions)
            competition.StartSeason(this);
    }

    /// <summary>Heimspiele eines Pokaltermins über die gemeinsame Spielabrechnung (genau einmal je Spiel).</summary>
    internal void SettleCupMatches(IEnumerable<KnockoutMatch> matches)
    {
        foreach (var match in matches)
            SettleHomeMatch(match.Home, match.Away, PlayedMatchDays + 1);
    }

    /// <summary>Europapokal dieser Saison oder null (Spielmodus „Europapokal + WM aus“).</summary>
    public EuropeanCup? EuropeanCup => Competitions.OfType<EuropeanCup>().FirstOrDefault();

    /// <summary>Weltmeisterschaft dieser Saison oder null (Spielmodus „Europapokal + WM aus“).</summary>
    public WorldCup? WorldCup => Competitions.OfType<WorldCup>().FirstOrDefault();

    /// <summary>Alle Pokalspiele (DEB-Pokal, Europapokal, WM) dieser Saison.</summary>
    public IEnumerable<KnockoutMatch> CupMatches =>
        (DebCup?.Matches ?? []).Concat(EuropeanCup?.Matches ?? []).Concat(WorldCup?.Matches ?? []);
}
