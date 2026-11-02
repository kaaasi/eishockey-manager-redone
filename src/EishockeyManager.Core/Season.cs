namespace EishockeyManager.Core;

/// <summary>Was an einem Tag der Saison stattfindet (Bitmaske wie in der Kalendertabelle von em.exe).</summary>
[Flags]
public enum DayEvents : byte
{
    None = 0,

    /// <summary>Ligaspieltag. Das Original setzt drei Bits (vermutlich je Liga eins); alle Ligen spielen gemeinsam.</summary>
    League = 7,
    DebCup = 8,
    WorldCup = 16,
    EuropeanCup = 32,

    /// <summary>Play-Off und Aufstiegsrunden.</summary>
    Playoffs = 64,

    /// <summary>Markierung 255: Ende der Vereinssaison (13. April) bzw. Saisonende (29. April).</summary>
    SeasonEnd = 255,
}

/// <summary>
/// Kalender einer Saison. [ORIGINAL] Die Termine stammen aus der Kalendertabelle in em.exe
/// (Dateioffset 0x175F9, ein Byte je Tag ab dem 10. September, Werte siehe <see cref="DayEvents"/>).
/// Belegt in den Videos: 17. Spieltag = Sonntag, 5. Nov.; 37. Spieltag = Mittwoch, 17. Januar;
/// 43. Spieltag = Freitag, 2. Feb.; 44. Spieltag = Sonntag, 4. Feb.; Saisonende = Montag, 29. April.
/// <list type="bullet">
/// <item>Jede Saison beginnt am „Sonntag, 10. Sept.“ – unabhängig vom echten Wochentag des Jahres.</item>
/// <item>Das Original kennt keinen 29. Februar: „Donnerstag, 22. März“ und „Samstag, 14. April“ (Video) passen
///   nur zu einem Kalender ohne Schalttag. Wochentage folgen daher dem Kalender 1989/90.</item>
/// </list>
/// </summary>
public static class SeasonCalendar
{
    /// <summary>Tag 0 einer Saison im Kalenderlayout (Sonntag, 10.09.1989; 1990 ist kein Schaltjahr).</summary>
    public static readonly DateOnly Layout = new(1989, 9, 10);

    /// <summary>Letzter Tag der Vereinssaison (Freitag, 13. April). Danach zeigt das Hauptmenü „Saisonende“.</summary>
    public const int ClubSeasonEnd = 215;

    /// <summary>Saisonende (Montag, 29. April): Bildschirm „Saisonende“, danach beginnt die neue Saison.</summary>
    public const int SeasonEnd = 231;

    public const int Length = SeasonEnd + 1;

    private static readonly int[] LeagueDays =
    [
        0, 3, 7, 10, 17, 21, 24, 26, 28, 31, 35, 38, 45, 49, 52, 54, 56, 63, 66, 70, 80, 84,
        87, 89, 91, 94, 98, 101, 105, 108, 112, 115, 117, 119, 122, 126, 129, 133, 136, 138, 140, 143, 145, 147,
    ];
    private static readonly int[] DebCupDays = [12, 14, 19, 40, 42, 47, 96, 103, 110, 124, 125, 132, 198, 202, 209];
    private static readonly int[] EuropeanCupDays = [55, 58, 60, 62, 65, 67, 69, 72, 74, 76, 78, 81];
    private static readonly int[] PlayoffDays = [149, 151, 153, 156, 158, 163, 165, 168, 182, 188, 193, 195, 200, 205, 207];
    private static readonly int[] WorldCupDays = [216, 218, 220, 222, 224, 226, 228, 230];

    private static readonly DayEvents[] Table = BuildTable();

    private static DayEvents[] BuildTable()
    {
        var table = new DayEvents[Length];
        foreach (var d in LeagueDays) table[d] |= DayEvents.League;
        foreach (var d in DebCupDays) table[d] |= DayEvents.DebCup;
        foreach (var d in EuropeanCupDays) table[d] |= DayEvents.EuropeanCup;
        foreach (var d in PlayoffDays) table[d] |= DayEvents.Playoffs;
        foreach (var d in WorldCupDays) table[d] |= DayEvents.WorldCup;
        table[ClubSeasonEnd] = DayEvents.SeasonEnd;
        table[SeasonEnd] = DayEvents.SeasonEnd;
        return table;
    }

    /// <summary>Kalendertabelle wie im Original (ein Byte je Tag, Tag 0 = 10. September).</summary>
    public static ReadOnlySpan<DayEvents> Days => Table;

    public static DayEvents Events(int day) => (uint)day < Length ? Table[day] : DayEvents.None;

    /// <summary>Tage (seit Saisonbeginn) der Termine eines Wettbewerbs, aufsteigend.</summary>
    public static IReadOnlyList<int> DaysOf(DayEvents kind) => kind switch
    {
        DayEvents.League => LeagueDays,
        DayEvents.DebCup => DebCupDays,
        DayEvents.EuropeanCup => EuropeanCupDays,
        DayEvents.Playoffs => PlayoffDays,
        DayEvents.WorldCup => WorldCupDays,
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static readonly string[] Weekdays = ["Sonntag", "Montag", "Dienstag", "Mittwoch", "Donnerstag", "Freitag", "Samstag"];

    /// <summary>[APPROX] Monatsnamen: kurze ausgeschrieben, lange abgekürzt (belegt: „Sept.“, „Okt.“, „Nov.“, „Dez.“, „Januar“, „Februar“, „März“, „April“).</summary>
    private static readonly string[] Months = ["Januar", "Februar", "März", "April", "Mai", "Juni", "Juli", "Aug.", "Sept.", "Okt.", "Nov.", "Dez."];

    public static DateOnly LayoutDate(int day) => Layout.AddDays(day);

    /// <summary>Kalenderjahr eines Saisontages (ab Januar das Folgejahr).</summary>
    public static int Year(int day, int season) => LayoutDate(day).Month >= 9 ? season : season + 1;

    /// <summary>Datumszeile der Infoleiste, z. B. „Sonntag, 10. Sept.  1993“.</summary>
    public static string Format(int day, int season)
    {
        var d = LayoutDate(day);
        // [APPROX] Ursache unbekannt: Das Original zeigt am Saisonende „Montag, 29. April“, obwohl der Kalender sonst einen Sonntag ergibt
        // (beide Saisonenden im Video). Der Wochentag wird dort wie im Original um eins weitergezählt.
        var weekday = ((int)d.DayOfWeek + (day == SeasonEnd ? 1 : 0)) % 7;
        return $"{Weekdays[weekday]}, {d.Day}. {Months[d.Month - 1]}  {Year(day, season)}";
    }

    /// <summary>Kurzes Datum für Meldungen, z. B. „1. Nov. 1995“.</summary>
    public static string FormatShort(int day, int season)
    {
        var d = LayoutDate(day);
        return $"{d.Day}. {Months[d.Month - 1]} {Year(day, season)}";
    }

    /// <summary>Tage (seit Saisonbeginn) der ersten <paramref name="count"/> Ligaspieltage.</summary>
    public static IReadOnlyList<int> LeagueMatchDays(int count) =>
        count <= LeagueDays.Length ? LeagueDays[..count] : throw new ArgumentOutOfRangeException(nameof(count));
}

public sealed record MatchResult(League League, int MatchDay, int Home, int Away, int HomeGoals, int AwayGoals)
{
    public bool Involves(int club) => Home == club || Away == club;

    /// <summary>Ergebnis aus Sicht eines Vereins: (eigene Tore, Gegentore, Heimspiel).</summary>
    public (int For, int Against, bool Home) For(int club) =>
        club == Home ? (HomeGoals, AwayGoals, true) : (AwayGoals, HomeGoals, false);
}

public enum TableView
{
    Heim,
    Gesamt,
    Auswaerts,
}

/// <summary>Eine Tabellenzeile.</summary>
public sealed class TableRow
{
    public required Club Club { get; init; }
    public int Rank { get; set; }

    /// <summary>Platz nach dem vorigen Spieltag (für die Tendenz), 0 = unbekannt.</summary>
    public int PreviousRank { get; set; }

    public int Games => Wins + Draws + Losses;
    public int Wins { get; set; }
    public int Draws { get; set; }
    public int Losses { get; set; }
    public int GoalsFor { get; set; }
    public int GoalsAgainst { get; set; }
    public int GoalDifference => GoalsFor - GoalsAgainst;

    /// <summary>Pluspunkte nach der 2-Punkte-Regel.</summary>
    public int PointsPlus => 2 * Wins + Draws;

    /// <summary>Minuspunkte nach der 2-Punkte-Regel.</summary>
    public int PointsMinus => 2 * Losses + Draws;

    /// <summary>Letzte Spiele, älteste zuerst: S/U/N, klein = Heimspiel, groß = Auswärtsspiel (Liga: em.exe 0x50283).</summary>
    public List<char> Form { get; } = [];

    /// <summary>Bilanz der letzten acht Spiele wie im Original.</summary>
    public string Balance => new(Form.TakeLast(8).ToArray());
}

public static class LeagueTable
{
    public const int BalanceLength = 8;

    /// <summary>Tabelle nach Spieltag <paramref name="upToMatchDay"/> (1-basiert) mit Tendenz zum Vortag.</summary>
    public static IReadOnlyList<TableRow> Compute(GameState game, League league, TableView view, int upToMatchDay)
    {
        var sorted = ComputeRanks(game, league, view, upToMatchDay);
        if (upToMatchDay > 1)
        {
            var before = ComputeRanks(game, league, view, upToMatchDay - 1).ToDictionary(r => r.Club.Index, r => r.Rank);
            foreach (var row in sorted)
                row.PreviousRank = before.GetValueOrDefault(row.Club.Index);
        }
        return sorted;
    }

    /// <remarks>
    /// [ORIGINAL] em.exe führt je Verein einen Satz von 0x50 Byte (DGROUP 0x825A + 0x50 · Verein; je Feld Heim/Auswärts):
    /// Punkte +0/+1, Bilanz +4 (8 Zeichen), Tore +0x16/+0x17, Gegentore +0x1A/+0x1B, Spiele +0x1E/+0x1F, Siege +0x26/+0x27,
    /// Niederlagen +0x2A/+0x2B. Eingetragen wird jedes Ligaspiel mit 0x50283 (Sieg DGROUP 0x7C5 = 2 Punkte, Unentschieden 1).
    /// Nach dem Spieltag sortiert 0x50B45 (Liga, 0, Ansicht 1) die Platzliste DGROUP 0x5CCB (Liga · 12 + Platz) an Ort und
    /// Stelle mit <see cref="ExchangeSort"/> und dem Vergleich <see cref="Better"/>. Die Ausgangsreihenfolge ist die Liste
    /// des Vortags; beim Spielstart ist sie die Vereinsreihenfolge (0x2F1A2). Die Tabellenansicht 0x4F78D sortiert dieselbe
    /// Liste für „Heim“/„Auswärts“ (Ansicht 0/2, nur das Heim- bzw. Auswärtsfeld) und beim Verlassen wieder nach „Gesamt“.
    /// Ab der zweiten Saison ist die Ausgangsreihenfolge die Platzliste der Vorsaison nach Auf- und Abstieg
    /// (<see cref="SuccessorOrder"/>).
    /// [APPROX] Dass eine angesehene Heim-/Auswärtstabelle die Ausgangsreihenfolge des nächsten Sortierens ändert, bildet
    /// das Remake nicht nach – Bildschirme verändern keinen Spielzustand (wirkt nur bei vollem Gleichstand).
    /// [APPROX] DGROUP 0x6B0 (schaltet im Vergleich die Spiele ab) gilt als 0: kein Code schreibt die Stelle.
    /// </remarks>
    private static List<TableRow> ComputeRanks(GameState game, League league, TableView view, int upToMatchDay)
    {
        var clubs = game.LeagueClubs(league).ToList();
        var total = clubs.ToDictionary(c => c.Index, c => new TableRow { Club = c });
        var shown = view == TableView.Gesamt ? total : clubs.ToDictionary(c => c.Index, c => new TableRow { Club = c });
        var order = StartOrder(game, league, clubs.Select(c => c.Index).ToList());

        var byDay = game.Results.Where(r => r.League == league && r.MatchDay <= upToMatchDay)
            .GroupBy(r => r.MatchDay).OrderBy(g => g.Key);
        foreach (var day in byDay)
        {
            foreach (var r in day)
            {
                if (!total.TryGetValue(r.Home, out var home) || !total.TryGetValue(r.Away, out var away))
                    continue;
                Add(home, r.HomeGoals, r.AwayGoals, true);
                Add(away, r.AwayGoals, r.HomeGoals, false);
                if (view == TableView.Heim)
                    Add(shown[r.Home], r.HomeGoals, r.AwayGoals, true);
                else if (view == TableView.Auswaerts)
                    Add(shown[r.Away], r.AwayGoals, r.HomeGoals, false);
            }
            ExchangeSort(order, (b, a) => Better(total[b], total[a]));
        }

        if (view != TableView.Gesamt)
        {
            ExchangeSort(order, (b, a) => Better(shown[b], shown[a]));
            foreach (var (index, row) in shown)
            {
                row.Form.Clear();
                row.Form.AddRange(total[index].Form); // [ORIGINAL] Bilanz unabhängig von der Ansicht (0x4F78D, DGROUP 0x825E)
            }
        }

        var list = order.Select(i => shown[i]).ToList();
        for (var i = 0; i < list.Count; i++)
            list[i].Rank = i + 1;
        return list;
    }

    /// <summary>
    /// Ausgangsreihenfolge der Platzliste DGROUP 0x5CCB. [ORIGINAL] Beim Spielstart Vereinsreihenfolge der Liga (0x2F1A2,
    /// Vereinsindex aufsteigend); danach tauscht em.exe in der 1. Liga die Plätze 1 und 3 (Datei 0x43A08/0x43A0D:
    /// DGROUP 0x5CCB = 2, 0x5CCD = 0) – der Deutsche Meister Düsseldorfer EG (DGROUP 0xD0E6 = 2) steht vorn; so im Video
    /// „Die Oberfläche von EM“ 13:07 (Tabelle 1. Liga vor dem ersten Spieltag: Düsseldorfer EG, SC Preussen Berlin, Eisbären
    /// Berlin, …). Ab der zweiten Saison siehe <see cref="SuccessorOrder"/>.
    /// </summary>
    private static List<int> StartOrder(GameState game, League league, List<int> order)
    {
        if (game.Season > GameState.FirstSeason
            && game.History.LastOrDefault(r => r.Season == game.Season - 1) is { } previous)
            return SuccessorOrder(previous, league, order);
        if (league == League.Bundesliga && game.Season == GameState.FirstSeason
            && order.IndexOf(0) is var a and >= 0 && order.IndexOf(2) is var b and >= 0)
            (order[a], order[b]) = (order[b], order[a]);
        return order;
    }

    /// <summary>
    /// Ausgangsreihenfolge einer Folgesaison aus dem Ergebnis der Vorsaison.
    /// </summary>
    /// <remarks>
    /// [ORIGINAL] em.exe behält die Platzliste 0x5CCB über den Saisonwechsel: Außer der Sortierung (0x50B45) und der
    /// WM-Reihenfolge (0x35EDD) schreibt kein Code die Liste, und der Saisonwechsel setzt nur die Tabellenfelder aller 64
    /// Vereine auf 0 (Datei 0x55E39–0x55EF8: Punkte, Tore, Gegentore, Spiele, Siege, Niederlagen, Bilanz). Auf- und Abstieg
    /// tauscht die Vereine selbst über ihre Indizes (Funktion 0x36169: Vereinssatz DGROUP 0x8257 + 0x50 · Verein,
    /// Vereinsnummer der 200 Spieler +0x1D, Verein der Manager DGROUP 0x966F + 0x1AF · Manager). Damit nimmt der Aufsteiger
    /// den Listenplatz seines Tauschpartners ein und umgekehrt. Paare (Datei 0x56137–0x5636D, Ligen 0 und 1): je
    /// Aufstiegsrunde der nächste Verein der unteren Liga unter den ersten beiden der Gruppentabelle (DGROUP 0xCD94 + 6 · Liga)
    /// mit dem nächsten Verein der oberen Liga unter den Plätzen 3 und 4 – also in Tabellenreihenfolge, wie
    /// <see cref="SeasonRecord.PromotedInto"/> und <see cref="SeasonRecord.RelegatedInto"/> gefüllt werden. Oberliga
    /// (Liga 2): der Letzte, dann der Vorletzte der Platzliste (0x5CCB + 24 + 11 − k) mit je einem Verein ohne Liga
    /// (<see cref="SeasonRecord.LeftOberliga"/> mit <see cref="SeasonRecord.PromotedInto"/> der Oberliga). Am ersten Spieltag
    /// der neuen Saison sind alle Tabellenfelder gleich; die Tauschsortierung behält dann die Reihenfolge.
    /// [APPROX] Das Remake hält keine eigene Platzliste: Es setzt sie aus der Abschlusstabelle „Gesamt“ der Vorsaison
    /// zusammen (gleich bis auf die Reihenfolge vollständig gleichstehender Vereine nach angesehenen Heim-/Auswärtstabellen,
    /// siehe <see cref="ComputeRanks"/>). Vereine ohne Partner (z. B. nach einem Moduswechsel) entfallen, übrige
    /// Ligamitglieder folgen in Vereinsreihenfolge.
    /// [APPROX] (#329, R9W-D02) Spielstände ab Schema 18 sichern den Saisonrekord mit Auf- und Absteigern in Tabellenreihenfolge;
    /// geladen ergibt sich dieselbe Startordnung (Probe <c>SaisonwechselProbenTests</c>). Ältere Spielstände (Schema ≤ 17)
    /// führen sie noch in der Reihenfolge der Gruppe; mitten in Saison ≥ 2 geladen, rechnet die laufende Saison mit der neuen
    /// Startordnung und diesen Paaren. Abweichung vom Stand vor dem Laden nur bei vollem Gleichstand; kein eigenes Schemafeld.
    /// </remarks>
    public static List<int> SuccessorOrder(SeasonRecord previous, League league, IReadOnlyList<int> members)
    {
        if (!previous.FinalTables.TryGetValue(league, out var final))
            return members.ToList();
        var partner = new Dictionary<int, int>();
        void Pair(IReadOnlyList<int> leaving, IReadOnlyList<int> arriving)
        {
            for (var i = 0; i < leaving.Count && i < arriving.Count; i++)
                partner[leaving[i]] = arriving[i];
        }
        if (league > League.Bundesliga)
            Pair(previous.PromotedInto[league - 1], previous.RelegatedInto[league]);
        if (league < League.Oberliga)
            Pair(previous.RelegatedInto[league + 1], previous.PromotedInto[league]);
        else
            Pair(previous.LeftOberliga, previous.PromotedInto[League.Oberliga]);

        var inLeague = members.ToHashSet();
        var order = final.Select(c => partner.TryGetValue(c, out var p) ? p : c).Where(inLeague.Contains).Distinct().ToList();
        order.AddRange(members.Where(c => !order.Contains(c)));
        return order;
    }

    /// <summary>
    /// [ORIGINAL] Vergleich von 0x50B45 (Datei 0x50D76–0x50F8A): <paramref name="b"/> (späterer Platz) verdrängt
    /// <paramref name="a"/>, wenn er mehr Punkte hat; bei gleichen Punkten weniger Spiele; dann höhere Tordifferenz; dann mehr
    /// Tore. Sonst bleibt die Reihenfolge. Bei der 2-Punkte-Regel entspricht „weniger Spiele“ den weniger Minuspunkten.
    /// </summary>
    private static bool Better(TableRow b, TableRow a)
    {
        if (b.PointsPlus != a.PointsPlus)
            return b.PointsPlus > a.PointsPlus;
        if (b.Games != a.Games)
            return b.Games < a.Games;
        if (b.GoalDifference != a.GoalDifference)
            return b.GoalDifference > a.GoalDifference;
        return b.GoalsFor > a.GoalsFor;
    }

    /// <summary>
    /// [ORIGINAL] Tauschsortierung von em.exe 0x50B45 (Datei 0x50D6D–0x51011): für jeden Platz i alle späteren Plätze j, Tausch,
    /// wenn der Verein auf j <paramref name="better"/> ist als der auf i. Nicht stabil.
    /// </summary>
    public static void ExchangeSort(IList<int> order, Func<int, int, bool> better)
    {
        for (var i = 0; i < order.Count - 1; i++)
            for (var j = i + 1; j < order.Count; j++)
                if (better(order[j], order[i]))
                    (order[i], order[j]) = (order[j], order[i]);
    }

    private static void Add(TableRow row, int goalsFor, int goalsAgainst, bool home)
    {
        row.GoalsFor += goalsFor;
        row.GoalsAgainst += goalsAgainst;
        char c;
        if (goalsFor > goalsAgainst) { row.Wins++; c = 'S'; }
        else if (goalsFor == goalsAgainst) { row.Draws++; c = 'U'; }
        else { row.Losses++; c = 'N'; }
        // [ORIGINAL] 0x50283 mit DGROUP 0x7CE „sSnNuU“: Heimverein klein (s/u/n), Gast groß (S/U/N).
        row.Form.Add(home ? char.ToLowerInvariant(c) : c);
    }
}
