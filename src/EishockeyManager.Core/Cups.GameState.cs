namespace EishockeyManager.Core;

/// <summary>Spielstand – Teil des Pakets „wettbewerbe“: Anmeldung und Abrechnung der Pokalwettbewerbe.</summary>
public sealed partial class GameState
{
    /// <summary>
    /// Nur während <see cref="NewGame.Create(Formats.ClubDatabase, GameSetup, int)"/> gesetzt (kein Spielzustand): Das neue Spiel lost
    /// Europapokal und DEB-Pokal an den Stellen des Originals aus (<see cref="DrawStartEuropeanCup"/>,
    /// <see cref="DrawStartDebCup"/>) und würfelt dort auch Werbeangebote und Börse; <see cref="InitializeCompetitions"/> und
    /// <see cref="InitializeManagerSystems"/> ziehen dann nicht.
    /// </summary>
    [ThreadStatic]
    internal static bool s_startDrawsAtOriginalPlace;

    /// <summary>DEB-Pokal dieser Saison oder null (Spielmodus „DEB-Pokal aus“).</summary>
    public DebCup? DebCup => Competitions.OfType<DebCup>().FirstOrDefault();

    /// <summary>Wettbewerbe nach dem Spielmodus der Spielvorbereitung anmelden, ohne auszulosen.</summary>
    internal void RegisterCompetitions()
    {
        if (Competitions.Count > 0)
            return;
        // Spielmodus (DGROUP 0xD0B9): Fahne = Liga und DEB-Pokal, Weltkugel = Europapokal und WM. Im Modus
        // „nur Weltkugel“ gibt es keine Vereinssaison und damit auch keinen Europapokal (seine Termine liegen vor dem
        // Sprung zur WM, Datei 0x54CEE).
        if (Setup.ClubCompetitions && Setup.DebCup)
            Competitions.Add(new DebCup());
        if (Setup.EuropeAndWorldCup && Setup.ClubCompetitions)
            Competitions.Add(new EuropeanCup());
        if (Setup.EuropeAndWorldCup)
            Competitions.Add(new WorldCup());
    }

    /// <summary>
    /// Europapokal auslosen. [ORIGINAL] 0x3139F(0) → 0x30427(2) → 0x341AB(1, 0) ohne Prüfung des Spielmodus: beim Spielstart in
    /// der Spielvorbereitung nach dem Poolaufbau und vor der Managerschleife (Datei 0x298B1), zum Saisonwechsel Datei 0x565FF.
    /// Ohne angemeldeten Europapokal zieht die Auslosung trotzdem (in ein verworfenes Objekt), damit der Strom gleich weiterläuft.
    /// </summary>
    internal void DrawEuropeanCup()
    {
        if (EuropeanCup is { } cup)
            cup.StartSeason(this);
        else if (EuropeanCup.CanDraw(this))
            new EuropeanCup().StartSeason(this);
    }

    /// <summary>
    /// Erste Runde des DEB-Pokals auslosen. [ORIGINAL] 0x341AB(0, 0) ohne Prüfung des Spielmodus (nur die Anzeige 0x33B83 prüft
    /// ihn, Datei 0x33BAB): beim Spielstart zwischen dem Verteilen des Pools (Datei 0x43A83) und dem ersten Listenwechsel (Datei
    /// 0x43A9F), Aufruf Datei 0x43A8C; zum Saisonwechsel Datei 0x56609 nach dem Europapokal. Ohne angemeldeten DEB-Pokal wie
    /// <see cref="DrawEuropeanCup"/>.
    /// </summary>
    internal void DrawDebCup()
    {
        if (DebCup is { } cup)
            cup.StartSeason(this);
        else if (Core.DebCup.CanDraw(this))
            new DebCup().StartSeason(this);
    }

    /// <summary>Spielstart: Europapokal an der Stelle des Originals (Datei 0x298B1).</summary>
    internal void DrawStartEuropeanCup()
    {
        RegisterCompetitions();
        DrawEuropeanCup();
    }

    /// <summary>Spielstart: DEB-Pokal an der Stelle des Originals (Datei 0x43A8C).</summary>
    internal void DrawStartDebCup()
    {
        RegisterCompetitions();
        DrawDebCup();
    }

    /// <summary>
    /// Spielstart: Wettbewerbe nach dem Spielmodus der Spielvorbereitung anmelden und die erste Saison beginnen.
    /// Wird einmal aus <see cref="InitializeManagerSystems"/> aufgerufen; jede weitere Saison startet
    /// <see cref="StartNextSeason"/> über <see cref="StartCompetitionSeasons"/>. Das neue Spiel lost Europapokal und DEB-Pokal an
    /// den Stellen des Originals aus (<see cref="s_startDrawsAtOriginalPlace"/>); der Import eines Originalspielstands hat keine
    /// solche Stelle und lost hier in der Reihenfolge des Saisonwechsels.
    /// </summary>
    internal void InitializeCompetitions()
    {
        RegisterCompetitions();
        // [ORIGINAL] Datei 0x298A3 → 0x30E7B(0): keine Würfe. Spur zufallsstrom, Teil 4 (N3 = B): Die eigene Quelle CupRandom ist
        // entfernt – Auslosungen und Pokalspiele ziehen aus Random.
        // [APPROX] Beim Spielstart mischt das Original die Länder in 0x35EDD vor der Spielvorbereitung (Datei 0x43A17, zusammen
        // mit der Ligamischung NewGame.StartOrder, die die Vereinsauswahl schon zeigt; AGENTS.md §4): Das Remake leitet diese eine
        // Mischung aus dem Zustand des Stroms ab, ohne ihn weiterzuschalten.
        WorldCup?.StartSeason(this, new GameRandom(StartMixSeed()));
        if (!s_startDrawsAtOriginalPlace)
        {
            DrawEuropeanCup();
            DrawDebCup();
        }
        foreach (var competition in Competitions)
            if (competition is not (Core.DebCup or Core.EuropeanCup or Core.WorldCup))
                competition.StartSeason(this);
    }

    /// <summary>
    /// Saisonwechsel: Wettbewerbe in der Reihenfolge des Originals beginnen. [ORIGINAL] Datei 0x565F6 WM 0x30E7B(0) (keine Würfe),
    /// Datei 0x565FF Europapokal 0x3139F(0), Datei 0x56609 DEB-Pokal 0x341AB(0, 0). Vorher in der Reihenfolge der Anmeldung
    /// (DEB-Pokal, Europapokal, WM) aus der eigenen Quelle <c>CupRandom</c>.
    /// </summary>
    internal void StartCompetitionSeasons()
    {
        WorldCup?.StartSeason(this);
        DrawEuropeanCup();
        DrawDebCup();
        foreach (var competition in Competitions)
            if (competition is not (Core.DebCup or Core.EuropeanCup or Core.WorldCup))
                competition.StartSeason(this);
    }

    /// <summary>Startwert der WM-Mischung des Spielstarts aus dem Zustand von <see cref="Random"/>; schaltet ihn nicht weiter.</summary>
    private int StartMixSeed()
    {
        var state = Random.Capture();
        var hash = unchecked(0x35EDD + state.Next * 31 + state.NextP);
        foreach (var value in state.SeedArray)
            hash = unchecked(hash * 31 + value);
        return hash & int.MaxValue;
    }

    /// <summary>
    /// Heimspiele eines Pokaltermins über die gemeinsame Spielabrechnung (genau einmal je Spiel).
    /// <paramref name="importance"/> = Wettbewerbsbonus der Zuschauerformel (Spur finanzen-2: 1 DEB-Pokal, 2 Europapokal).
    /// </summary>
    internal void SettleCupMatches(IEnumerable<KnockoutMatch> matches, int importance = 0)
    {
        foreach (var match in matches)
            SettleHomeMatch(match.Home, match.Away, PlayedMatchDays + 1, importance);
    }

    /// <summary>Europapokal dieser Saison oder null (Spielmodus „Europapokal + WM aus“).</summary>
    public EuropeanCup? EuropeanCup => Competitions.OfType<EuropeanCup>().FirstOrDefault();

    /// <summary>Weltmeisterschaft dieser Saison oder null (Spielmodus „Europapokal + WM aus“).</summary>
    public WorldCup? WorldCup => Competitions.OfType<WorldCup>().FirstOrDefault();

    /// <summary>Alle Pokalspiele (DEB-Pokal, Europapokal, WM) dieser Saison.</summary>
    public IEnumerable<KnockoutMatch> CupMatches =>
        (DebCup?.Matches ?? []).Concat(EuropeanCup?.Matches ?? []).Concat(WorldCup?.Matches ?? []);
}
