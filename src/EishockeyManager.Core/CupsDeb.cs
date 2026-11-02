namespace EishockeyManager.Core;

/// <summary>
/// DEB-Pokal: 32 Vereine, fünf K.-o.-Runden (16, 8, 4, 2 Paarungen, Endspiel) mit je drei Terminen.
/// </summary>
/// <remarks>
/// Belege aus em.exe (Overlay „Statistik/DEB-Pokal-Auslosung“, Dateioffsets):
/// <list type="bullet">
/// <item>[ORIGINAL] Teilnehmerzahl je Runde 32, 16, 8, 4, 2, 1 (DGROUP 0x3B1); 15 Termine im Kalender = 5 Runden × 3.</item>
/// <item>[ORIGINAL] Auslosung 0x341AB: Die Vereine aller Manager bekommen zuerst zufällige Plätze 0–31; die übrigen
///   Plätze der Reihe nach mit zufälligen Ligavereinen (Datenbank 0–35 = DGROUP 0x7CB) ohne Doppelte. Paarung =
///   Plätze 2k und 2k+1 (links/rechts in der Auslosung).</item>
/// <item>[ORIGINAL] Heimrecht 0x347EF: Spielt rechts ein Oberligist gegen einen höherklassigen Verein oder ein
///   Zweitligist gegen einen Bundesligisten, tauschen beide die Plätze – der unterklassige Verein hat Heimrecht.</item>
/// <item>[ORIGINAL] Spiele und Siege 0x34B76: siehe <see cref="CupTie"/>. Nach dem dritten Termin einer Runde wird
///   sofort die nächste Runde ausgelost (0x3489D): Sieger der Reihe nach, dann 20 zufällige Vertauschungen.</item>
/// <item>[ORIGINAL] Gegner eines Managers 0x44D75: Platz 2k hat Heimrecht, Spiel nur, solange die Paarung offen ist.</item>
/// </list>
/// [ORIGINAL] Unentschieden: schneller Weg random(0, 2) je Seite bis ungleich, Minutenweg Verlängerung und Penaltyschießen
/// 0x3901B; Endstand mit Marke + 30 (<see cref="CupMatch"/>, <see cref="CupTie.Marks"/>; spielberechnung.md §3/§19).
/// </remarks>
public sealed class DebCup : ISeasonCompetition
{
    public const int Teams = 32;
    public const int RoundCount = 5;
    public const int DatesPerRound = CupTie.MaxGames;

    /// <summary>[ORIGINAL] Vertauschungen bei der Auslosung ab der zweiten Runde (em.exe 0x3494B).</summary>
    public const int DrawSwaps = 20;

    /// <summary>[ORIGINAL] Rundennamen (Texte in em.exe, DGROUP 0x2576 ff.).</summary>
    public static readonly string[] RoundNames = ["Erste Runde", "Achtelfinale", "Viertelfinale", "Halbfinale", "Endspiel"];

    private readonly List<List<CupTie>> _rounds = [];
    private readonly List<int> _draw = [];

    public Competition Kind => Competition.DebCup;
    public DayEvents Days => DayEvents.DebCup;
    public Title? Title => Core.Title.CupWinner;

    /// <summary>Ausgeloste Runden; die letzte ist die laufende.</summary>
    public IReadOnlyList<IReadOnlyList<CupTie>> Rounds => _rounds;

    /// <summary>
    /// Plätze der laufenden Runde in Ziehungsreihenfolge (gerade = links, ungerade = rechts), wie sie der Bildschirm
    /// „DEB-Pokal Auslosung“ zeigt – im Original vor dem Heimrecht-Tausch (0x347EF läuft erst nach der Anzeige).
    /// </summary>
    public IReadOnlyList<int> Draw => _draw;

    /// <summary>Alle 32 Teilnehmer der ersten Runde.</summary>
    public IReadOnlyList<int> Participants => _rounds.Count == 0 ? [] : _rounds[0].SelectMany(t => new[] { t.First, t.Second }).ToList();

    public int? Winner { get; private set; }

    /// <summary>Laufende Runde (0 = 1. Runde) oder −1 vor der Auslosung.</summary>
    public int CurrentRound => _rounds.Count - 1;

    /// <summary>Alle Spiele dieser Saison in der Reihenfolge der Termine.</summary>
    public IEnumerable<KnockoutMatch> Matches => _rounds.SelectMany(r => r).SelectMany(t => t.Games).OrderBy(g => g.Day);

    /// <summary>Termin (Tag seit Saisonbeginn) eines Spiels: Runde 0–4, Spiel 0–2.</summary>
    public static int DayOf(int round, int game) => SeasonCalendar.DaysOf(DayEvents.DebCup)[round * DatesPerRound + game];

    private static (int Round, int Game)? DateOf(int day)
    {
        var index = SeasonCalendar.DaysOf(DayEvents.DebCup).ToList().IndexOf(day);
        return index < 0 ? null : (index / DatesPerRound, index % DatesPerRound);
    }

    public void StartSeason(GameState game)
    {
        _rounds.Clear();
        _draw.Clear();
        Winner = null;
        DrawFirstRound(game);
    }

    /// <summary>Erste Runde: Manager-Vereine auf zufällige Plätze, Rest zufällig aus den Ligavereinen.</summary>
    private void DrawFirstRound(GameState game)
    {
        var random = game.CupRandom;
        var slots = new int?[Teams];
        var managerClubs = game.ActiveManagers.Select(m => m.ClubIndex).Distinct().ToList();
        foreach (var club in managerClubs.Take(Teams))
        {
            int slot;
            do slot = random.Next(0, Teams); // random(0, 31)
            while (slots[slot] is not null);
            slots[slot] = club;
        }

        // Datenbank 0–35 im Original = alle Ligavereine; im Remake nach aktueller Liga (Auf-/Abstieg, ligalose Wahl).
        var pool = game.Clubs.Where(c => c.League is not null).Select(c => c.Index).ToList();
        for (var slot = 0; slot < Teams; slot++)
        {
            if (slots[slot] is not null)
                continue;
            int club;
            do club = pool[random.Next(pool.Count)];
            while (slots.Contains(club) || managerClubs.Contains(club));
            slots[slot] = club;
        }
        StartRound(game, slots.Select(s => s!.Value).ToList());
    }

    /// <summary>Nächste Runde: Sieger in Paarungsreihenfolge, dann <see cref="DrawSwaps"/> zufällige Vertauschungen.</summary>
    private void DrawNextRound(GameState game)
    {
        var winners = _rounds[^1].Select(t => t.Winner!.Value).ToList();
        var random = game.CupRandom;
        for (var i = 0; i < DrawSwaps; i++)
        {
            var a = random.Next(1, winners.Count + 1) - 1; // random(1, n) − 1
            var b = random.Next(1, winners.Count + 1) - 1;
            (winners[a], winners[b]) = (winners[b], winners[a]);
        }
        StartRound(game, winners);
    }

    private void StartRound(GameState game, List<int> slots)
    {
        _draw.Clear();
        _draw.AddRange(slots);
        for (var k = 1; k < slots.Count; k += 2)
        {
            var left = game.Clubs[slots[k - 1]];
            var right = game.Clubs[slots[k]];
            var (l, r) = (CupClass.Of(left), CupClass.Of(right));
            if ((r == 4 && l < 4) || (r == 2 && l == 1))
                (slots[k - 1], slots[k]) = (slots[k], slots[k - 1]);
        }
        var round = new List<CupTie>();
        for (var k = 0; k + 1 < slots.Count; k += 2)
            round.Add(new CupTie(slots[k], slots[k + 1]));
        _rounds.Add(round);
    }

    public (int Opponent, bool Home)? MatchOn(GameState game, int day, int club)
    {
        if (DateOf(day) is not { } date || date.Round != CurrentRound)
            return null;
        var (round, number) = date;
        foreach (var tie in _rounds[round])
        {
            if (!tie.Involves(club) || !tie.PlaysGame(number))
                continue;
            var (home, away) = CupTie.Pairing(tie, number);
            return (home == club ? away : home, home == club);
        }
        return null;
    }

    public void PlayDay(GameState game, int day, List<SeasonStep> steps)
    {
        if (DateOf(day) is not { } date || date.Round != CurrentRound || Winner is not null)
            return;
        var (round, number) = date;

        var managerClubs = game.ActiveManagers.Select(m => m.ClubIndex).ToHashSet();
        var played = new List<KnockoutMatch>();
        foreach (var tie in _rounds[round])
        {
            if (!tie.PlaysGame(number))
                continue;
            var (home, away) = CupTie.Pairing(tie, number);
            var (match, marks) = CupMatch.Play(game, day, game.Clubs[home], game.Clubs[away], MatchCompetition.DebCup, game.CupRandom);
            tie.Add(match, marks);
            played.Add(match);
        }
        game.SettleCupMatches(played, importance: 1); // gemeinsame Spielabrechnung, genau einmal je Spiel
        // [ORIGINAL] 0x34B76 trägt jedes gespielte Spiel über 0x50283 ein (Datei 0x34C5C, Endstand ohne Marke:
        // 0x34C06–0x34C2A zieht 30 ab, solange das Heimbyte > 29) – Begegnungstabelle (Spur wettbewerbe-spiel, Teil 2).
        game.RecordMeetings(played);

        if (played.Count > 0)
            steps.Add(new MatchesStep(day, Competition.DebCup, played.Any(m => managerClubs.Contains(m.Home) || managerClubs.Contains(m.Away))));

        if (number < DatesPerRound - 1)
            return;
        // Dritter Termin: jede Paarung ist entschieden (höchstens drei Spiele, eines gewinnt immer).
        if (round < RoundCount - 1)
        {
            DrawNextRound(game);
            steps.Add(new CupDrawStep(day, Competition.DebCup, round + 1, _draw.ToList()));
        }
        else
        {
            Winner = _rounds[round][0].Winner;
            steps.Add(new TrophyStep(day, Core.Title.CupWinner, Winner!.Value));
        }
    }
}

/// <summary>
/// Eine neue Pokalrunde wurde ausgelost (Bildschirm „DEB-Pokal Auslosung“ im Spieltagsablauf).
/// <paramref name="Slots"/> = Plätze in Ziehungsreihenfolge (<see cref="DebCup.Draw"/>) zum Zeitpunkt der Auslosung: Ein
/// „und tschüß…“ kann nach dem dritten Termin weitere Tage spielen, die Anzeige darf dann nicht die spätere Runde zeigen.
/// </summary>
/// <remarks>
/// [ORIGINAL] Die Anzeige gehört im Original zur Auslosung selbst: Erste Runde 0x341AB (Aufrufe beim Spielstart Datei
/// 0x43A8C und beim Saisonwechsel 0x56609), weitere Runden 0x3489D (Aufruf nach dem dritten Termin 0x34F64); beide rufen
/// die Anzeige 0x33B83 vor dem Ziehen auf.
/// </remarks>
public sealed record CupDrawStep(int Day, Competition Competition, int Round, IReadOnlyList<int> Slots) : SeasonStep(Day);
