namespace EishockeyManager.Core;

/// <summary>
/// Spur saisonwechsel, Teil 3 (S-K6, Entscheidung Nutzer 03.11.2026 = A): Satzfolge der Vereine je Liga wie em.exe.
/// </summary>
/// <remarks>
/// [ORIGINAL] Ligamischung Datei 0x35EDD (Ov 0xF0, Stub 0xF0:0x43; docs/analyse/re-runde-12.md, „S-K6“), aufgerufen beim
/// Spielstart (Datei 0x43A17) und im Saisonwechsel (Datei 0x5644E, nach Auf- und Abstieg): je Liga 55-mal zwei Plätze
/// random(b, b + 11) tauschen (auch denselben Platz; Datei 0x35FD2–0x3613F). Der Tausch 0x36169 mit Schalter 1 bewegt den
/// ganzen Vereinssatz; die Platzliste DGROUP 0x5CCB (Tabellenfolge) behält dabei ihre Vereine. Die Vereine bleiben in ihrer
/// Liga, nur ihre Satzplätze wechseln. Beleg FILE13 → FILE14: Indizes in allen drei Ligen permutiert.
/// Das Remake behält feste Vereinsindizes und führt die Satzfolge als eigene Liste (<see cref="LeagueSlots"/>); was im
/// Original nach Satzplatz innerhalb der Liga geht (Fair-Play-Folge 0x2A1E0, A194), richtet sich danach – seit Runde 17 auch
/// der Liga-Spielplan (Datei 0x4E7C7, A113; GameState.Spielplan.cs).
/// Zufallsquelle: Das Original zieht aus lrandom (Datei 0xBFD5, <c>0x38:0x7E5</c>; Datei 0x36013/0x3602E: lrandom(b, b + 11)),
/// einem eigenen Generator x = (16 · x + 167) mod 4.000.000, dessen Zustand (DGROUP 0xE017) nicht im Spielstand steht
/// (docs/analyse/re-runde-13.md, S-K6-b). Spur wettbewerbe-regeln Teil 2 (#897, N3 = B: eigene Zufallsquellen auf
/// <see cref="GameState.Random"/>): Die 3 · 55 Tausche ziehen je zwei Würfe random(0, 11) aus dem gemeinsamen Strom, am 29. April
/// an der Stelle der Mischung (Datei 0x5644E), nach den 40 Würfen der Länder (A825, Datei 0x35EE3–0x35FC9). Bis Runde 22 eine abgeleitete Quelle (Startwert aus Saison und Stromzustand,
/// ohne Weiterschalten). [APPROX] lrandom ist nicht nachgebaut (Folge nicht zugexakt, N3 = B). [ORIGINAL] Die Teamwert-Wanderung
/// zieht unmittelbar davor (Datei 0x56448; seit Spur saison-geld, T03, <see cref="DriftSeasonEnd"/>).
/// [ORIGINAL] Zeitpunkt (Spur saison Teil 3, S-K6-a, re-runde-13.md; Entscheidung Leitstelle L5): em.exe mischt am 29. April
/// nach Auf- und Abstieg, Ewiger Tabelle und Teamwert-Wanderung, vor dem Fenster „Saisonende“ mit den Prämien (Datei 0x5644E
/// vor 0x5645D). Das Remake bildet die Satzfolge der nächsten Saison daher schon in <see cref="FinishSeason"/>
/// (<see cref="PrepareNextLeagueSlots"/>, gespeichert ab Schema 38) und übernimmt sie in <see cref="StartNextSeason"/>
/// unverändert. Die Fair-Play-Prämie wertet diese Folge (SeasonFlow.FairPlay.cs, S-K6-c).
/// [ORIGINAL] Datei 0x346D5 (<c>0xE8:0x8E</c>, Datei 0x56453 direkt nach der Mischung, beim Spielstart nach 0x43A17): legt die
/// deutschen Europapokal-Teilnehmer fest (DGROUP 0xA763/0xA776: Deutscher Meister DGROUP 0xD0E6, Titelverteidiger DGROUP 0x388).
/// Der Tausch 0x36169 führt beide Indizes über 0x36146 mit (Datei 0x36270/0x36284), die Mischung ändert die Teilnehmer also
/// nicht (Spielstände FILE22 → FILE23: Meister KÖLNER EC Satz 4 → 8, Teilnehmerliste 8). Im Remake mit festen Vereinsindizes:
/// <see cref="EuropeanCup"/> beim Saisonstart, gleiches Ergebnis (zwischen 29. April und Saisonstart ändert sich nichts daran).
/// Spielstart (R-K5, Spur kader-start): die Startmischung entsteht vor der Spielvorbereitung aus dem Spielstart-Seed
/// (<see cref="NewGame.StartOrder"/>), die Vereinsauswahl zeigt sie; danach nur der Managertausch, keine zweite Mischung.
/// </remarks>
public sealed partial class GameState
{
    /// <summary>[ORIGINAL] Tausche je Liga (Datei 0x35FD2: Zähler bis 55).</summary>
    public const int LeagueSlotSwaps = 55;

    /// <summary>
    /// Satzfolge aller drei Ligen hintereinander (Bundesliga, 2. Liga, Oberliga; je <see cref="ClubsPerLeague"/> Einträge).
    /// Leer in Spielständen vor Schema 34 (Migration); dann gilt bis zum nächsten Saisonwechsel die Indexfolge.
    /// </summary>
    private readonly List<int> _leagueSlots = [];

    /// <summary>
    /// Satzfolge der nächsten Saison (gleiches Format wie <see cref="_leagueSlots"/>, nach Liga der nächsten Saison), gebildet am
    /// 29. April in <see cref="FinishSeason"/> und in <see cref="StartNextSeason"/> übernommen; sonst leer. Leer auch in
    /// Spielständen vor Schema 38 (Migration): dann bildet <see cref="StartNextSeason"/> die Folge wie bisher selbst.
    /// </summary>
    private readonly List<int> _nextLeagueSlots = [];

    /// <summary>
    /// Vereine einer Liga in der Satzfolge des Originals (Plätze DGROUP 0x7C0 + Liga ff.). Reine Abfrage: Vereine, die nicht
    /// (mehr) in der gespeicherten Folge stehen, folgen nach Vereinsindex.
    /// </summary>
    public IReadOnlyList<int> LeagueSlots(League league)
    {
        var members = LeagueClubs(league).Select(c => c.Index).ToHashSet();
        var order = StoredSlots(league).Where(members.Contains).ToList();
        order.AddRange(members.Where(c => !order.Contains(c)).Order());
        return order;
    }

    private IEnumerable<int> StoredSlots(League league) => Segment(_leagueSlots, league);

    private static IEnumerable<int> Segment(List<int> slots, League league) =>
        slots.Count == 3 * ClubsPerLeague ? slots.Skip((int)league * ClubsPerLeague).Take(ClubsPerLeague) : [];

    /// <summary>
    /// Satzfolge einer Liga der nächsten Saison (Mitglieder nach Auf- und Abstieg), sobald sie am 29. April gebildet ist
    /// (Phase <see cref="SeasonPhase.SeasonOver"/>); sonst leer. Reine Abfrage.
    /// </summary>
    public IReadOnlyList<int> NextLeagueSlots(League league) => Segment(_nextLeagueSlots, league).ToList();

    /// <summary>
    /// Spielstart: Satzfolge = Startmischung (Datei 0x43A17, vor der Spielvorbereitung; <see cref="NewGame.StartOrder"/>) mit
    /// den Plätzen nach dem Managertausch (U15-a, Datei 0x29CF2), ohne weitere Mischung (R-K5, Spur kader-start).
    /// </summary>
    /// <param name="places">Platz → Verein, mindestens die 36 Ligaplätze.</param>
    internal void InitializeLeagueSlots(IReadOnlyList<int> places)
    {
        _leagueSlots.Clear();
        _leagueSlots.AddRange(places.Take(3 * ClubsPerLeague));
        foreach (var league in Enum.GetValues<League>())
            if (!StoredSlots(league).Order().SequenceEqual(LeagueClubs(league).Select(c => c.Index).Order()))
            {
                _leagueSlots.Clear(); // ungewöhnliche Ligabelegung (synthetische Spielstände): Indexfolge gilt
                return;
            }
    }

    /// <summary>
    /// 29. April (Datei 0x5644E, aus <see cref="FinishSeason"/> nach Auf-/Abstieg und Zwangsabstieg): Satzfolge der nächsten
    /// Saison bilden und mischen. Die Ligen der Vereine wechseln im Remake erst in <see cref="StartNextSeason"/>; die Mitglieder
    /// der nächsten Saison kommen daher aus <see cref="SeasonRecord.NextLeague"/>.
    /// </summary>
    private void PrepareNextLeagueSlots(SeasonRecord record)
    {
        _nextLeagueSlots.Clear();
        if (SuccessorSlots(record, NextLeagueMembers(record)) is { } next)
        {
            _nextLeagueSlots.AddRange(next);
            Mix(_nextLeagueSlots);
        }
    }

    /// <summary>Liga eines Vereins in der nächsten Saison (am 29. April, vor dem Ligawechsel in <see cref="StartNextSeason"/>).</summary>
    private League? NextLeagueOf(SeasonRecord record, Club club) =>
        club.Index < NewGame.SelectableClubs ? record.NextLeague(club.Index, club.League) : club.League;

    private Dictionary<League, HashSet<int>> NextLeagueMembers(SeasonRecord record) =>
        Enum.GetValues<League>().ToDictionary(l => l, l => Clubs.Where(c => NextLeagueOf(record, c) == l).Select(c => c.Index).ToHashSet());

    /// <summary>
    /// Satzfolge am 29. April nach den Tauschen von Auf- und Absteigern, vor der Mischung: die Ligaplätze der alten Saison mit den
    /// Aufsteigern bzw. Absteigern auf den Plätzen ihrer Tauschpartner (<see cref="SuccessorSlots"/>, ungemischt), dann die Plätze
    /// ohne Liga nach dem Oberliga-Austausch dieser Saison (<see cref="OutsidePlaces"/> mit <paramref name="record"/>), dann die
    /// übrigen Vereine nach Index. Fehlt eine geführte Folge (ungewöhnliche Ligagröße), gilt <see cref="ClubRecordOrder"/>.
    /// </summary>
    /// <remarks>
    /// [ORIGINAL] T03/A843/A935 (Spur saison-geld; docs/analyse/re-abweichungen.md T03): Die Wanderung 0x09D1B(10) (Datei 0x56448)
    /// zählt die Vereinssätze in der Folge nach dem Austausch der Gruppen 0–2 (0x36169 mit Schalter 1 tauscht ganze Sätze,
    /// Datei 0x56137–0x5636D) und vor der Mischung 0x35EDD (Datei 0x5644E).
    /// </remarks>
    internal IReadOnlyList<int> SeasonEndDriftOrder(SeasonRecord record)
    {
        var order = SuccessorSlots(record, NextLeagueMembers(record));
        if (order is null)
            return ClubRecordOrder();
        var places = OutsidePlaces().ToArray();
        ApplyOberligaPairs(record, places);
        order.AddRange(places.Where(c => !order.Contains(c)));
        order.AddRange(Enumerable.Range(0, Math.Min(NewGame.SelectableClubs, Clubs.Count)).Where(c => !order.Contains(c)));
        return order;
    }

    /// <summary>
    /// 29. April: große Wanderung der Teamwerte (Modus 10) in der Satzfolge <see cref="SeasonEndDriftOrder"/>, mit den Ligagrenzen
    /// des Platzes nach dem Austausch (= Liga der nächsten Saison).
    /// </summary>
    /// <remarks>
    /// [ORIGINAL] Grenzen nach Satzplatz (0x34FD5(i) / 2 in 0x9D1B, MatchEngine.TeamValues.cs): Nach dem Tausch 0x36169 steht der
    /// Aufsteiger auf dem Platz der höheren Liga. [ORIGINAL] auch im Modus „nur Weltkugel“ (A058, Spur saison Teil 4;
    /// docs/analyse/re-runde-13.md): Datei 0x56448 ruft 0x9D1B mit Modus 10 ohne Modusprüfung. Spielstände FILE20 → FILE21 (Modus 1):
    /// Ligavereine Kondition/Technik (Felder 0x18–0x1B) gewandert und in den Ligagrenzen (Kaufbeuren 80 78 79 75 → 92 91 90 87,
    /// Düsseldorf 94 × 4 an der Obergrenze), alle zwölf Nationalteams dort unverändert, Form 0x1C–0x1E überall neu.
    /// [APPROX] Ein Spielstand vom Bildschirm „Saisonende“ (Phase <see cref="SeasonPhase.SeasonOver"/>), gesichert vor Runde 25, hat
    /// den 29. April ohne diese Wanderung abgeschlossen; da <see cref="StartNextSeason"/> nicht mehr wandert, entfällt sie für diesen
    /// einen Saisonwechsel. Kein Schemafeld dafür (nur Spielstände genau dieses Bildschirms, Wirkung eine Wanderung).
    /// </remarks>
    private void DriftSeasonEnd(SeasonRecord record)
    {
        var order = SeasonEndDriftOrder(record);
        MatchEngine.DriftTeamValues(this, TeamValueDrift.Season, MatchEngine.DriftOrder(this, order), c => NextLeagueOf(record, c));
    }

    /// <summary>
    /// Saisonwechsel in <see cref="StartNextSeason"/> nach dem Ligawechsel: die am 29. April gebildete Folge übernehmen (nicht
    /// erneut mischen). Fehlt sie oder passt sie nicht zu den Ligen (Spielstand vor Schema 38, synthetische Spielstände), wird
    /// sie hier wie bis Schema 37 gebildet und gemischt.
    /// </summary>
    private void AdvanceLeagueSlots(SeasonRecord record)
    {
        var members = Enum.GetValues<League>().ToDictionary(l => l, l => LeagueClubs(l).Select(c => c.Index).ToHashSet());
        var prepared = _nextLeagueSlots.ToList();
        _nextLeagueSlots.Clear();
        if (prepared.Count == 3 * ClubsPerLeague
            && Enum.GetValues<League>().All(l => Segment(prepared, l).ToHashSet().SetEquals(members[l])))
        {
            _leagueSlots.Clear();
            _leagueSlots.AddRange(prepared);
            return;
        }
        var next = SuccessorSlots(record, members);
        _leagueSlots.Clear();
        if (next is null)
            return; // ungewöhnliche Ligagröße (synthetische Spielstände): Folge nicht führen, Indexfolge gilt
        _leagueSlots.AddRange(next);
        MixLeagueSlots();
    }

    /// <summary>
    /// Der Aufsteiger bzw. Absteiger übernimmt den Satzplatz seines Tauschpartners (0x36169); null bei ungewöhnlicher Ligagröße.
    /// </summary>
    private List<int>? SuccessorSlots(SeasonRecord record, IReadOnlyDictionary<League, HashSet<int>> membersOf)
    {
        var partner = new Dictionary<int, int>();
        void Pair(IReadOnlyList<int> leaving, IReadOnlyList<int> arriving)
        {
            for (var i = 0; i < leaving.Count && i < arriving.Count; i++)
                partner.TryAdd(leaving[i], arriving[i]);
        }
        foreach (var league in Enum.GetValues<League>())
        {
            if (league > League.Bundesliga)
                Pair(record.PromotedInto[league - 1], ShownRelegatedInto(record, league));
            if (league < League.Oberliga)
                Pair(FirstExchangeRelegatedInto(record, league + 1), record.PromotedInto[league]);
        }
        Pair(record.LeftOberliga, record.PromotedInto[League.Oberliga]);

        var next = new List<int>();
        foreach (var league in Enum.GetValues<League>())
        {
            var members = membersOf[league];
            // [ORIGINAL] A665: Platztausch innerhalb der unteren Liga (Datei 0x5636D, 0x36169) gilt auch für Vereine, die in der Liga bleiben.
            var inner = record.InnerMoves.Where(m => m.Lower == league).ToDictionary(m => m.Slot, m => m.Club);
            var order = StoredSlots(league).Select(c => inner.TryGetValue(c, out var q) ? q : partner.TryGetValue(c, out var p) && !members.Contains(c) ? p : c)
                .Where(members.Contains).Distinct().ToList();
            order.AddRange(members.Where(c => !order.Contains(c)).Order());
            if (order.Count != ClubsPerLeague)
                return null;
            next.AddRange(order);
        }
        return next;
    }

    private void MixLeagueSlots()
    {
        if (_leagueSlots.Count != 3 * ClubsPerLeague)
        {
            _leagueSlots.Clear();
            return;
        }
        Mix(_leagueSlots);
    }

    private void Mix(List<int> slots)
    {
        for (var league = 0; league < 3; league++)
        {
            var b = league * ClubsPerLeague;
            for (var n = 0; n < LeagueSlotSwaps; n++)
            {
                // [ORIGINAL] Datei 0x36013/0x3602E: zwei Würfe random(b, b + 11), dann Tausch 0x36169 (auch derselbe Platz).
                var x = b + Random.Next(ClubsPerLeague);
                var y = b + Random.Next(ClubsPerLeague);
                (slots[x], slots[y]) = (slots[y], slots[x]);
            }
        }
    }
}
