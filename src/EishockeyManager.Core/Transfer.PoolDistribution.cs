namespace EishockeyManager.Core;

/// <summary>
/// Regeln der Poolumverteilung (Spur kader-transfer, Teil 5; Inventur M-K7, M-K8, F002–F004, F013–F015).
/// Fundstellen und Ablauf: <c>docs/transfer.md</c>, „Poolumverteilung“.
/// </summary>
public static class PoolDistributionRules
{
    /// <summary>Grenzen der Poolwerte nach der Drift. [ORIGINAL] Datei 0x054D9: 0x4BD35(Wert, 30, 99).</summary>
    public const int DriftMin = 30;

    /// <inheritdoc cref="DriftMin"/>
    public const int DriftMax = 99;

    /// <summary>Höchstens so viele Spieler im Ausland, bevor 0x05521 sie zurückholt (Datei 0x05672: Anzahl &gt; 4).</summary>
    public const int MaxForeign = 4;

    /// <summary>
    /// Platz in der Liga je Zählerstand. [ORIGINAL] DGROUP 0x48FE (17 Einträge): die ersten fünf Vereine einer Liga zweimal.
    /// </summary>
    public static readonly IReadOnlyList<int> ClubTurns = [0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 6, 7, 8, 9, 10, 11];

    /// <summary>Erster und letzter Verein für einen Spieler „im Ausland“ (Datei 0x07EF1: random(64, 150)).</summary>
    public const int FirstForeign = 64;

    /// <inheritdoc cref="FirstForeign"/>
    public const int LastForeign = 150;

    /// <summary>Drift eines Poolwerts. [ORIGINAL] Datei 0x054D9: Wert + random(1, 7) − 4, begrenzt auf 30 … 99.</summary>
    public static int Drift(int value, Random random) =>
        Math.Clamp((byte)value + TransferRules.Random16(random, 1, 7) - 4, DriftMin, DriftMax);

    /// <summary>
    /// Soll-Spielerzahl je Liga. [ORIGINAL] Datei 0x05760–0x057DD: T = Σ k + Auslandsspieler, R = 179 − T (vor der Ersetzung
    /// T = 0 → 1), s[l] = (100 · k[l] / T) · R / 100 in Langzahlen (LXMUL 0x021CA, LDIV 0x0227C). <paramref name="leaguePlayers"/>
    /// ist 179 (letzter Ligaplatz des Pools). [APPROX] R wird bei größeren Kadern nicht unter 0 gezogen (im Original Byte).
    /// </summary>
    public static int[] Quotas(IReadOnlyList<int> squadSlots, int foreign, int leaguePlayers = 179)
    {
        var total = squadSlots.Sum() + foreign;
        var rest = Math.Max(0, leaguePlayers - total);
        if (total == 0)
            total = 1;
        return squadSlots.Select(k => (int)(100L * k / total * rest / 100)).ToArray();
    }
}

public sealed partial class TransferMarket
{
    // [ORIGINAL] DGROUP 0x48FB–0x48FD: Zähler je Liga für den Zielverein (0x07EF1), Startwert 0, nach 17 wieder 0.
    private int _poolTurnBundesliga;
    private int _poolTurnZweiteLiga;
    private int _poolTurnOberliga;

    /// <summary>
    /// Spielstart: freie Poolspieler auf die Ligen verteilen. [ORIGINAL] Datei 0x057E3 (Aufruf 0x43A83, nach der Spielvorbereitung
    /// 0x28165 und vor dem ersten Listenwechsel 0x43A9F): Soll je Liga aus <see cref="PoolQuotas"/>; für jeden Platz 1 … 178
    /// (Schleife bis 0xB3 ausschließlich), dessen Verein (+0x1D) kein Managerverein ist: für jede Liga q = 0 … 2 mit Soll ≠ 0
    /// Verein = <see cref="NextPoolClub"/>(q), Soll − 1 – ohne Abbruch nach der ersten Liga, ein Spieler zählt also bei allen
    /// Ligen mit Rest und landet in der letzten. Die Vereinslisten 0x07F6B werden nicht benutzt (F015). Die Prüfung auf das Jahr
    /// (DGROUP 0x5A18 = 0) greift im Spiel nie.
    /// </summary>
    internal void DistributePoolAtStart(GameState game, Random random)
    {
        if (_pool.Count == 0)
            return;
        var layout = PoolLayout;
        var quotas = PoolQuotas(game, random);
        for (var i = 1; i < layout.LeagueLast; i++)
        {
            var slot = _pool[i - 1];
            if (IsManaged(game, game.Clubs[slot.Club]))
                continue;
            for (var q = 0; q < quotas.Length; q++)
            {
                if (quotas[q] == 0)
                    continue;
                slot.Club = NextPoolClub(game, q, random);
                quotas[q]--;
            }
        }
    }

    /// <summary>
    /// Saisonwechsel: freie Poolspieler auf Ligen mit Fehlbedarf umhängen. [ORIGINAL] Datei 0x07C7E (Aufruf 0x56631 im Tageslauf
    /// 0x54C9F, nach den neuen Namen 0x56467/0x56470):
    /// <list type="number">
    /// <item>Soll s[0 … 2] aus <see cref="PoolQuotas"/>, s[3] = 0.</item>
    /// <item>Für jede Gruppe q = 0 … 3 (Ligen und Vereine 36 … 42) und jeden Platz 1 … 179 mit Verein in q: s[q] − 1 (nicht unter 0);
    ///   Besitzer &gt; 3 (Markt oder frei) → Liste frei[q] (Datei 0x07D04–0x07D90).</item>
    /// <item>Für l = 0 … 3, solange s[l] &gt; 0 (höchstens 1000 Durchgänge): für q ≠ l, solange frei[q] und s[l] Rest haben:
    ///   Listenplatz random(0, Länge(q) − 1), bis zu 1000 Wiederholungen auf einen schon genommenen; Spieler nehmen, Verein =
    ///   <see cref="NextPoolClub"/>(l); Rest q − 1, s[l] − 1 (Datei 0x07DEB–0x07E93).</item>
    /// </list>
    /// [APPROX] Gruppe einer Liga = Vereine mit dieser Liga (im Original Indexbereiche DGROUP 0x7C6/0x7C7 nach der Ligamischung),
    /// Gruppe 3 = Vereine ohne Liga (Index &lt; 48; im Original 36 … 42). Vergebene Plätze (Besitzer 0–3) sind Spieler im Kader eines
    /// Managers.
    /// </summary>
    internal void DistributePoolAtSeasonChange(GameState game, Random random)
    {
        if (_pool.Count == 0)
            return;
        var layout = PoolLayout;
        var quotas = new int[4];
        PoolQuotas(game, random).CopyTo(quotas, 0);
        var free = new List<int>[4];
        for (var q = 0; q < 4; q++)
        {
            free[q] = [];
            for (var i = 1; i <= layout.LeagueLast; i++)
            {
                var slot = _pool[i - 1];
                if (PoolGroup(game, slot.Club) != q)
                    continue;
                if (quotas[q] != 0)
                    quotas[q]--;
                if (slot.Owner != PoolOwner.Taken)
                    free[q].Add(i);
            }
        }
        var counts = free.Select(f => f.Count).ToArray();
        var left = counts.ToArray();
        for (var l = 0; l < 4; l++)
        {
            for (var guard = 0; quotas[l] > 0 && guard < 1000; guard++)
                for (var q = 0; q < 4; q++)
                {
                    if (q == l)
                        continue;
                    while (left[q] > 0 && quotas[l] > 0)
                    {
                        int r;
                        var tries = 0;
                        do
                            r = TransferRules.Random16(random, 0, counts[q] - 1);
                        while (free[q][r] < 0 && tries++ < 1000);
                        var index = free[q][r];
                        free[q][r] = -1;
                        if (index > 0)
                            _pool[index - 1].Club = NextPoolClub(game, l, random);
                        left[q]--;
                        quotas[l]--;
                    }
                }
        }
    }

    /// <summary>
    /// Soll-Spielerzahl je Liga 0 … 2 samt Drift und Auslandsrückholung. [ORIGINAL] Datei 0x05521 (aus 0x057E3 und 0x07C7E):
    /// <list type="number">
    /// <item>k[l] = Σ belegte Kaderplätze (0x4BE17 Modus 0) der Manager in Liga l (Managerfeld DGROUP 0x9746 + 0x1AF · m).</item>
    /// <item>Je Platz 1 … 199: Drift von +0x15 und +0x16 (<see cref="PoolDistributionRules.Drift"/>); Plätze &lt; 180 mit Verein
    ///   ≥ 64 zählen als Auslandsspieler.</item>
    /// <item>random(5, 15)-mal: Platz random(1, 179); sind mehr als 4 im Ausland und ist er im Ausland, bekommt er einen neuen
    ///   Verein (0x059B8 mit Schalter 0) und der Zug zählt nicht; Spieler eines Managervereins, und solange mehr als 4 im Ausland
    ///   sind jeder andere, werden verworfen; sonst neuer Verein über 0x059B8 (Datei 0x0564C–0x05712).</item>
    /// <item>Soll nach <see cref="PoolDistributionRules.Quotas"/> mit den Auslandsspielern nach der Rückholung.</item>
    /// </list>
    /// [APPROX] Die Drift wirkt nur auf freie Plätze: Spieler auf dem Markt und in Managerkadern führen ihre Werte im Remake am
    /// Spieler selbst (im Original im Kaderplatz +0x10/+0x11); gezogen wird für jeden Platz. Ohne passenden Verein (synthetische
    /// Datenbank) bleibt der Verein; nach 100.000 verworfenen Zügen endet die Schleife (im Original ohne Grenze).
    /// </summary>
    internal int[] PoolQuotas(GameState game, Random random)
    {
        var layout = PoolLayout;
        var slots = new int[3];
        foreach (var m in game.ActiveManagers)
            if (game.Clubs[m.ClubIndex].League is { } league && (int)league < slots.Length)
                slots[(int)league] += game.Clubs[m.ClubIndex].Players.Count;

        var foreign = 0;
        foreach (var slot in _pool)
        {
            var condition = PoolDistributionRules.Drift(slot.Player.Condition, random);
            var technique = PoolDistributionRules.Drift(slot.Player.Technique, random);
            if (slot.Owner == PoolOwner.Free)
                (slot.Player.Condition, slot.Player.Technique) = (condition, technique);
            if (slot.Index <= layout.LeagueLast && IsForeignPoolClub(slot.Club))
                foreign++;
        }

        var turns = TransferRules.Random16(random, 5, 15);
        for (var j = 0; j < turns; j++)
        {
            for (var attempt = 0; attempt < 100_000; attempt++)
            {
                var slot = _pool[TransferRules.Random16(random, 1, layout.LeagueLast) - 1];
                var club = slot.Club;
                var taken = true;
                if (foreign > PoolDistributionRules.MaxForeign && IsForeignPoolClub(club))
                {
                    foreign--;
                    slot.Club = PoolBidder(game, slot, random);
                    taken = false;
                }
                if (IsManaged(game, game.Clubs[club]) || !taken || foreign > PoolDistributionRules.MaxForeign)
                    continue;
                slot.Club = PoolBidder(game, slot, random);
                break;
            }
        }

        foreign = _pool.Count(s => s.Index <= layout.LeagueLast && IsForeignPoolClub(s.Club));
        return PoolDistributionRules.Quotas(slots, foreign, layout.LeagueLast);
    }

    /// <summary>
    /// Zielverein eines Poolspielers in Liga <paramref name="league"/>. [ORIGINAL] Datei 0x07EF1: Liga &gt; 2 → random(64, 150);
    /// sonst Verein = Ligaanfang DGROUP 0x7C0[l] + DGROUP 0x48FE[z] mit dem Zähler z = DGROUP 0x48FB[l] (danach z + 1, nach 17
    /// wieder 0); Managerverein → nächster Zählerwert. [APPROX] Platz in der Liga = Rang nach Vereinsindex (das Original ordnet die
    /// Sätze beim Ligawechsel um, 0x35EDD); Verein 150 gibt es im Remake nicht (149). Ohne freien Verein (synthetisch) der erste.
    /// </summary>
    public int NextPoolClub(GameState game, int league, Random random)
    {
        if (league > 2)
            return Math.Min(TransferRules.Random16(random, PoolDistributionRules.FirstForeign, PoolDistributionRules.LastForeign),
                game.Clubs.Count - 1);
        var members = game.LeagueClubs((League)league).OrderBy(c => c.Index).ToList();
        for (var attempt = 0; attempt < 1000; attempt++)
        {
            var position = PoolDistributionRules.ClubTurns[PoolTurn(league)];
            SetPoolTurn(league, (PoolTurn(league) + 1) % PoolDistributionRules.ClubTurns.Count);
            if (position < members.Count && !IsManaged(game, members[position]))
                return members[position].Index;
        }
        return members.Count > 0 ? members[0].Index : 0;
    }

    /// <summary>Gruppe eines Vereins für 0x07C7E: Liga 0 … 2, 3 = ohne Liga (unter 48), sonst −1 (Nationalteams, Ausland).</summary>
    private static int PoolGroup(GameState game, int club) =>
        game.Clubs[club].League is { } league ? (int)league : club < NewGame.SelectableClubs ? 3 : -1;

    private static bool IsForeignPoolClub(int club) => club >= PoolDistributionRules.FirstForeign;

    /// <summary>Neuer Verein über 0x059B8 (Schalter 0: Vereine 0 … 42) mit der Stärke der Grundwerte +0x15 … +0x17.</summary>
    private static int PoolBidder(GameState game, PoolSlot slot, Random random)
    {
        var club = UnsolicitedOfferRules.Bidder(slot.Player.Strength, foreign: false, game.BidderStrength, random);
        return club < 0 ? slot.Club : club;
    }

    private int PoolTurn(int league) => league switch
    {
        0 => _poolTurnBundesliga,
        1 => _poolTurnZweiteLiga,
        _ => _poolTurnOberliga,
    };

    private void SetPoolTurn(int league, int value)
    {
        switch (league)
        {
            case 0: _poolTurnBundesliga = value; break;
            case 1: _poolTurnZweiteLiga = value; break;
            default: _poolTurnOberliga = value; break;
        }
    }
}
