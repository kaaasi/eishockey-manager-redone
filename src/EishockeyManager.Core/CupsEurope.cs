namespace EishockeyManager.Core;

/// <summary>
/// Europapokal: zwei Gruppen à vier Vereine (Hin- und Rückrunde), danach die Endrunde der Gruppenersten und
/// -zweiten. Zwölf Termine im November (4.11. … 30.11.).
/// </summary>
/// <remarks>
/// Belege aus em.exe (Dateioffsets):
/// <list type="bullet">
/// <item>[ORIGINAL] Auslosung 0x341AB (Wettbewerb 1): acht Plätze mit zufälligen Europapokal-Gegnern (Datenbank
///   64–149) ohne Doppelte; ein Gegner mit Stärke unter 70 wird nur zu einem Viertel angenommen (sonst neu
///   gezogen). Danach ersetzt jeder deutsche Teilnehmer einen zufälligen der acht Plätze, der noch einen
///   Europapokal-Gegner hält.</item>
/// <item>[ORIGINAL] Deutsche Teilnehmer 0x346D5: der Deutsche Meister (DGROUP 0xD0E6, zu Spielbeginn Verein 2 –
///   0x43A12) und der Titelverteidiger (DGROUP 0x388, zu Spielbeginn Verein 64), wenn er ein deutscher Verein
///   (Index &lt; 64) und nicht schon dabei ist.</item>
/// <item>[ORIGINAL] Gruppen 0x30427: Plätze 0–3 = Gruppe 1, 4–7 = Gruppe 2; Endrunde = Erster und Zweiter von
///   Gruppe 1, dann Erster und Zweiter von Gruppe 2. Spielplan siehe <see cref="CupGroup"/>.</item>
/// <item>[ORIGINAL] Termine 0x3139F: Termin 0–5 Gruppen, 6–11 Endrunde; nach Termin 11 ist der Erste der Endrunde
///   Europacupsieger (DGROUP 0x388), das Meisterfoto erscheint nur für einen deutschen Sieger (Index &lt; 64).
///   Video (Karriere 29:41): „Europacupsieger SC Preussen Berlin“ zwischen dem 20. und 22. Spieltag.</item>
/// </list>
/// [APPROX] „Stärke“ der Gegner = erster Datenbankwert (Abwehr); der zweite Wert (Sturm) wird im Remake nicht
/// geführt (Befund A01). Die Formabschläge der Europapokal-Gegner (−5) wirken nicht, weil das Remake ohne Formwerte rechnet.
/// </remarks>
public sealed class EuropeanCup : ISeasonCompetition
{
    public const int Teams = 8;
    public const int GroupSize = 4;
    public const int FirstOpponent = 64;
    public const int LastOpponent = 149;

    /// <summary>[ORIGINAL] Gegner unter dieser Stärke werden nur mit Wahrscheinlichkeit 1/4 angenommen.</summary>
    public const int WeakOpponent = 70;

    /// <summary>[ORIGINAL] Deutscher Meister zu Spielbeginn (DGROUP 0xD0E6 = 2, Düsseldorfer EG).</summary>
    public const int InitialChampion = 2;

    /// <summary>[ORIGINAL] Europacupsieger vor der ersten Saison (DGROUP 0x388 = 64, kein deutscher Verein).</summary>
    public const int InitialWinner = 64;

    public const int GroupDates = 6;

    private readonly List<int> _draw = [];
    private readonly List<CupGroup> _groups = [];

    public Competition Kind => Competition.EuropeanCup;
    public DayEvents Days => DayEvents.EuropeanCup;
    public Title? Title => Core.Title.EuropeanCupWinner;

    /// <summary>Acht Plätze der Auslosung (Gruppe 1 = 0–3, Gruppe 2 = 4–7).</summary>
    public IReadOnlyList<int> Draw => _draw;

    /// <summary>Deutsche Teilnehmer dieser Saison.</summary>
    public IReadOnlyList<int> GermanEntrants { get; private set; } = [];

    /// <summary>Gruppe 1 und 2.</summary>
    public IReadOnlyList<CupGroup> Groups => _groups;

    /// <summary>Endrunde (ab dem siebten Termin) oder null.</summary>
    public CupGroup? FinalRound { get; private set; }

    public int? Winner { get; private set; }

    /// <summary>Alle Spiele dieser Saison in Terminreihenfolge.</summary>
    public IEnumerable<KnockoutMatch> Matches =>
        _groups.Concat(FinalRound is null ? [] : [FinalRound]).SelectMany(g => g.Results).OrderBy(m => m.Day);

    /// <summary>Termin (0–11) eines Tages oder −1.</summary>
    public static int DateIndex(int day) => SeasonCalendar.DaysOf(DayEvents.EuropeanCup).ToList().IndexOf(day);

    /// <summary>Gruppen, die an Termin <paramref name="date"/> spielen.</summary>
    private IReadOnlyList<CupGroup> GroupsOn(int date) =>
        date < GroupDates ? _groups : FinalRound is { } final ? [final] : [];

    /// <summary>Deutscher Meister und Europacupsieger der Vorsaison (zu Spielbeginn die Werte des Originals).</summary>
    public static IReadOnlyList<int> Entrants(GameState game)
    {
        var last = game.History.Count > 0 ? game.History[^1] : null;
        var champion = last?.Titles.GetValueOrDefault(Core.Title.GermanChampion, InitialChampion) ?? InitialChampion;
        var holder = last?.Titles.GetValueOrDefault(Core.Title.EuropeanCupWinner, InitialWinner) ?? InitialWinner;
        var list = new List<int> { champion };
        if (holder < FirstOpponent && holder != champion)
            list.Add(holder);
        return list;
    }

    public void StartSeason(GameState game)
    {
        _draw.Clear();
        _groups.Clear();
        FinalRound = null;
        Winner = null;
        GermanEntrants = Entrants(game);

        var random = game.CupRandom;
        for (var slot = 0; slot < Teams; slot++)
        {
            int club;
            while (true)
            {
                club = random.Next(FirstOpponent, Math.Min(LastOpponent, game.Clubs.Count - 1) + 1); // random(64, 149)
                if (_draw.Contains(club))
                    continue;
                if (game.Clubs[club].Strength < WeakOpponent && random.Next(0, 4) != 0) // random(0, 3) ≠ 0 → neu ziehen
                    continue;
                break;
            }
            _draw.Add(club);
        }
        foreach (var german in GermanEntrants)
        {
            int slot;
            do slot = random.Next(0, Teams); // random(0, 7)
            while (_draw[slot] < FirstOpponent);
            _draw[slot] = german;
        }
        _groups.Add(new CupGroup(1, _draw.Take(GroupSize).ToList()));
        _groups.Add(new CupGroup(2, _draw.Skip(GroupSize).Take(GroupSize).ToList()));
    }

    public (int Opponent, bool Home)? MatchOn(GameState game, int day, int club)
    {
        var date = DateIndex(day);
        if (date < 0 || Winner is not null)
            return null;
        foreach (var group in GroupsOn(date))
            foreach (var (home, away) in group.Pairings(date % GroupDates))
                if (home == club || away == club)
                    return (home == club ? away : home, home == club);
        return null;
    }

    public void PlayDay(GameState game, int day, List<SeasonStep> steps)
    {
        var date = DateIndex(day);
        if (date < 0 || Winner is not null)
            return;
        var played = GroupsOn(date).SelectMany(g => g.Play(date % GroupDates, day, game, game.CupRandom)).ToList();
        game.SettleCupMatches(played, importance: SpectatorRules.EuropeanCupImportance(date)); // Spur heimspiel-statistik

        var managerClubs = game.ActiveManagers.Select(m => m.ClubIndex).ToHashSet();
        if (played.Count > 0)
            steps.Add(new MatchesStep(day, Competition.EuropeanCup, played.Any(m => managerClubs.Contains(m.Home) || managerClubs.Contains(m.Away))));

        if (date == GroupDates - 1)
        {
            var tables = _groups.Select(g => g.Table(game.Clubs)).ToList();
            FinalRound = new CupGroup(0, [tables[0][0].Club.Index, tables[0][1].Club.Index, tables[1][0].Club.Index, tables[1][1].Club.Index]);
        }
        else if (date == 2 * GroupDates - 1 && FinalRound is { } final)
        {
            Winner = final.Table(game.Clubs)[0].Club.Index;
            if (Winner < FirstOpponent)
                steps.Add(new TrophyStep(day, Core.Title.EuropeanCupWinner, Winner.Value));
        }
    }
}
