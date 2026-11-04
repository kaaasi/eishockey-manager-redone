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
/// Original nach Satzplatz innerhalb der Liga geht (Fair-Play-Folge 0x2A1E0, A194), richtet sich danach. Der Spielplan
/// wird wie bisher je Saison eigens gewürfelt (re-runde-12.md: dafür genügt das).
/// [APPROX] Zufallsquelle: Das Original zieht aus lrandom (Datei 0xBFD5, <c>0x38:0x7E5</c>; Datei 0x36013/0x3602E:
/// lrandom(b, b + 11)), einem eigenen Generator x = (16 · x + 167) mod 4.000.000, dessen Zustand (DGROUP 0xE017) nicht im
/// Spielstand steht und den sich die Mischung mit Spielberechnung und Teamwert-Wanderung (Datei 0x56448, unmittelbar davor)
/// teilt – zugexakt nicht nachbildbar (docs/analyse/re-runde-13.md, S-K6-b). Das Remake leitet die Mischung aus dem Zustand
/// von <see cref="GameState.Random"/> ab, ohne ihn weiterzuschalten – sonst verschöben sich alle übrigen Spielverläufe
/// (Liga, Pokale, Transfers); die Verteilung der Satzfolge (55 Zufallstausche) ist dieselbe.
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

    /// <summary>Spielstart (Datei 0x43A17): Satzfolge = Vereinsindex, dann gemischt.</summary>
    internal void InitializeLeagueSlots()
    {
        _leagueSlots.Clear();
        foreach (var league in Enum.GetValues<League>())
            _leagueSlots.AddRange(LeagueClubs(league).Select(c => c.Index).Order());
        MixLeagueSlots();
    }

    /// <summary>
    /// 29. April (Datei 0x5644E, aus <see cref="FinishSeason"/> nach Auf-/Abstieg und Zwangsabstieg): Satzfolge der nächsten
    /// Saison bilden und mischen. Die Ligen der Vereine wechseln im Remake erst in <see cref="StartNextSeason"/>; die Mitglieder
    /// der nächsten Saison kommen daher aus <see cref="SeasonRecord.NextLeague"/>.
    /// </summary>
    private void PrepareNextLeagueSlots(SeasonRecord record)
    {
        _nextLeagueSlots.Clear();
        var members = Enum.GetValues<League>().ToDictionary(l => l, l => Clubs
            .Where(c => (c.Index < NewGame.SelectableClubs ? record.NextLeague(c.Index, c.League) : c.League) == l)
            .Select(c => c.Index).ToHashSet());
        if (SuccessorSlots(record, members) is { } next)
        {
            _nextLeagueSlots.AddRange(next);
            Mix(_nextLeagueSlots);
        }
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
            var order = StoredSlots(league).Select(c => partner.TryGetValue(c, out var p) && !members.Contains(c) ? p : c)
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
        var random = new GameRandom(SlotSeed());
        for (var league = 0; league < 3; league++)
        {
            var b = league * ClubsPerLeague;
            for (var n = 0; n < LeagueSlotSwaps; n++)
            {
                var x = b + random.Next(ClubsPerLeague);
                var y = b + random.Next(ClubsPerLeague);
                (slots[x], slots[y]) = (slots[y], slots[x]);
            }
        }
    }

    /// <summary>Startwert der Mischung aus dem Zufallszustand und der Saison; schaltet <see cref="Random"/> nicht weiter.</summary>
    private int SlotSeed()
    {
        var state = Random.Capture();
        var hash = unchecked(Season * 7919 + state.Next * 31 + state.NextP);
        foreach (var value in state.SeedArray)
            hash = unchecked(hash * 31 + value);
        return hash & int.MaxValue;
    }
}
