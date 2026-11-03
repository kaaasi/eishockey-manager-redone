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
/// [APPROX] Zufallsquelle: Das Original zieht aus dem gemeinsamen Zufall. Das Remake leitet die Mischung aus dem Zustand
/// von <see cref="GameState.Random"/> ab, ohne ihn weiterzuschalten – sonst verschöben sich alle übrigen Spielverläufe
/// (Liga, Pokale, Transfers); die Verteilung der Satzfolge (55 Zufallstausche) ist dieselbe.
/// [APPROX] Zeitpunkt: em.exe mischt am 29. April vor dem Fenster „Saisonende“ (0x5644E vor 0x5645D); das Remake wechselt
/// die Ligen erst in <see cref="GameState.StartNextSeason"/> und mischt dort. Die Fair-Play-Prämie des Saisonendes sieht
/// daher noch die Satzfolge der abgelaufenen Saison (Wirkung nur bei Gleichstand um Platz 1).
/// </remarks>
public sealed partial class GameState
{
    /// <summary>[ORIGINAL] Tausche je Liga (Datei 0x35FD2: Zähler bis 55).</summary>
    public const int LeagueSlotSwaps = 55;

    /// <summary>
    /// Satzfolge aller drei Ligen hintereinander (Bundesliga, 2. Liga, Oberliga; je <see cref="ClubsPerLeague"/> Einträge).
    /// Leer in Spielständen vor Schema 33 (Migration); dann gilt bis zum nächsten Saisonwechsel die Indexfolge.
    /// </summary>
    private readonly List<int> _leagueSlots = [];

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

    private IEnumerable<int> StoredSlots(League league) =>
        _leagueSlots.Count == 3 * ClubsPerLeague ? _leagueSlots.Skip((int)league * ClubsPerLeague).Take(ClubsPerLeague) : [];

    /// <summary>Spielstart (Datei 0x43A17): Satzfolge = Vereinsindex, dann gemischt.</summary>
    internal void InitializeLeagueSlots()
    {
        _leagueSlots.Clear();
        foreach (var league in Enum.GetValues<League>())
            _leagueSlots.AddRange(LeagueClubs(league).Select(c => c.Index).Order());
        MixLeagueSlots();
    }

    /// <summary>
    /// Saisonwechsel (Datei 0x5644E): Der Aufsteiger bzw. Absteiger übernimmt den Satzplatz seines Tauschpartners (0x36169),
    /// dann wird gemischt. Aufruf aus <see cref="StartNextSeason"/> nach dem Ligawechsel.
    /// </summary>
    private void AdvanceLeagueSlots(SeasonRecord record)
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
                Pair(record.PromotedInto[league - 1], record.RelegatedInto[league]);
            if (league < League.Oberliga)
                Pair(record.RelegatedInto[league + 1], record.PromotedInto[league]);
        }
        Pair(record.LeftOberliga, record.PromotedInto[League.Oberliga]);

        var next = new List<int>();
        foreach (var league in Enum.GetValues<League>())
        {
            var members = LeagueClubs(league).Select(c => c.Index).ToHashSet();
            var order = StoredSlots(league).Select(c => partner.TryGetValue(c, out var p) && !members.Contains(c) ? p : c)
                .Where(members.Contains).Distinct().ToList();
            order.AddRange(members.Where(c => !order.Contains(c)).Order());
            if (order.Count != ClubsPerLeague)
                return; // ungewöhnliche Ligagröße (synthetische Spielstände): Folge nicht führen, Indexfolge gilt
            next.AddRange(order);
        }
        _leagueSlots.Clear();
        _leagueSlots.AddRange(next);
        MixLeagueSlots();
    }

    private void MixLeagueSlots()
    {
        if (_leagueSlots.Count != 3 * ClubsPerLeague)
        {
            _leagueSlots.Clear();
            return;
        }
        var random = new GameRandom(SlotSeed());
        for (var league = 0; league < 3; league++)
        {
            var b = league * ClubsPerLeague;
            for (var n = 0; n < LeagueSlotSwaps; n++)
            {
                var x = b + random.Next(ClubsPerLeague);
                var y = b + random.Next(ClubsPerLeague);
                (_leagueSlots[x], _leagueSlots[y]) = (_leagueSlots[y], _leagueSlots[x]);
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
