using EishockeyManager.Formats;

namespace EishockeyManager.Core;

/// <summary>Spieler im WM-Kader und sein Verein (im Original wird der Verein während der Nominierung gemerkt).</summary>
public sealed record NationalSquadMember(Player Player, int Club);

/// <summary>Ein Freundschaftsspiel der „WM Vorbereitung“ (Heim, Gast, Ergebnis) und der Wertebonus für den Kader.</summary>
public sealed record FriendlyMatch(int Home, int Away, int HomeGoals, int AwayGoals, int Bonus);

/// <summary>Ergebnis eines Klicks in „Spieler in WM-Kader“.</summary>
public enum NominationResult
{
    Added,
    Removed,

    /// <summary>[ORIGINAL] Schon 29 Spieler im Kader (Datei 0x3CAD5: Zähler &gt; 28 → abgelehnt, ohne Meldung).</summary>
    SquadFull,

    /// <summary>Verletzt und nicht im Kader: Das Original tut nichts.</summary>
    Unavailable,
}

/// <summary>Ergebnis einer Vereinbarung in „WM Vorbereitung“.</summary>
public enum FriendlyResult
{
    Played,

    /// <summary>„Nicht mehr als / 8 Freundschaftsspiele“.</summary>
    TooMany,

    /// <summary>„Sie haben nicht / genug Geld !“</summary>
    NotEnoughMoney,

    /// <summary>Das eigene Land ist gesperrt (kein Spiel, keine Meldung).</summary>
    OwnNation,
}

/// <summary>
/// Bundestrainer: WM-Kader und Freundschaftsspiele („WM Vorbereitung“) eines Managers für ein Nationalteam.
/// </summary>
/// <remarks>
/// Belege aus em.exe (Overlay 0xF8, Datei-Basis 0x399D0; Ablauf Datei 0x54CEE ff.):
/// <list type="bullet">
/// <item>[ORIGINAL] Den Bundestrainer gibt es nur im Spielmodus „nur Weltkugel“ (DGROUP 0xD0B9 = 1). Er spielt als
///   „Manager 5“ mit eigenem Kontostand (DGROUP 0xA055, anfangs 0) und bekommt beim Amtsantritt den Etat DGROUP 0x29C
///   (Datei 0x554A6).</item>
/// <item>[ORIGINAL] Etat: anfangs 1.500.000 Drittel-DM = 500.000 DM; nach jeder WM (Datei 0x31293 ff.) 100.000 DM, für
///   die Plätze 1–8 des eigenen Landes (11 − Platz) · 100.000 DM.</item>
/// <item>[ORIGINAL] Kader „Spieler in WM-Kader“ (Datei 0x3C37C): höchstens 29 Spieler (Datei 0x3CAD5), „auto“ wählt
///   3 Torhüter, 10 Verteidiger, 16 Stürmer (DGROUP 0x4EDE) – je Art den Unverletzten mit der höchsten Summe aus
///   Kondition, Technik und Form (Datei 0x3CC01). Am Saisonende kehren alle zu ihren Vereinen zurück (Datei 0x557B0).</item>
/// <item>[ORIGINAL] „WM Vorbereitung“ (Datei 0x3B8F7): Kosten je Gegner aus DGROUP 0x26C (Drittel-DM, im Spielstand
///   gesichert), Heimspiel doppelt; höchstens 8 Spiele je Saison (DGROUP 0xD0C1, Saisonbeginn 0); das Spiel wird
///   sofort ausgetragen (Datei 0x3C05E). Wertebonus siehe <see cref="FriendlyBonus"/>.</item>
/// <item>[APPROX] Kandidaten: Das Original listet seine 200 ausführlich geführten Spieler (Kader der Manager und
///   Transfermarkt). Das Remake führt alle Kader vollständig; angeboten werden deutsche Spieler (keine Ausländer) der
///   Ligavereine und des Transfermarkts, nach Kondition + Technik + Form absteigend.</item>
/// </list>
/// </remarks>
public sealed partial class NationalCoach
{
    public const int MaxSquad = 29;
    public const int MaxFriendlies = 8;

    /// <summary>[ORIGINAL] DGROUP 0x4EDE: Torhüter, Verteidiger, Stürmer bei „auto“.</summary>
    public static readonly int[] AutoSquad = [3, 10, 16];

    /// <summary>
    /// [ORIGINAL] Belastung (Kaderplatz +0x13 = 0x5DD6 − 0x5DC3) der Kaderspieler zu Beginn der Freundschaftsspiele: Datei
    /// 0x3BB51 (<c>mov byte [bx+0x5DD6],0x46</c>, Schleife über die 29 Plätze ab 0x3BB22); die Spalte E zeigt 70 · 10 / 15 =
    /// 46 (A184, Runde 12; vorher als Energie gelesen). Name aus Kompatibilität. Zeitpunkt siehe
    /// <see cref="GameState.PlayFriendly"/> ([APPROX]).
    /// </summary>
    public const int FriendlyEnergy = 70;

    /// <summary>[ORIGINAL] Etat beim ersten Amtsantritt (DGROUP 0x29C = 1.500.000 Drittel-DM).</summary>
    public const long FirstBudget = 500_000;

    /// <summary>[ORIGINAL] DGROUP 0x26C: Kosten eines Auswärtsspiels je Nationalteam 52–63 in Drittel-DM.</summary>
    private static readonly long[] FriendlyCostThirds =
        [300000, 165000, 180000, 135000, 120000, 150000, 90000, 60000, 105000, 75000, 45000, 195000];

    /// <summary>
    /// Reihen der Automatik (<see cref="Lineup"/>) und der Reihenwerte der Anzeige: Torhüter, drei Reihen à zwei Verteidiger
    /// und drei Stürmer. [ORIGINAL] Im Spiel zählt mit eigener Aufstellung auch die vierte Reihe (0x8EF2/0x91FF: drei bzw.
    /// mit vierter Reihe vier Reihen, <see cref="MatchEngine.NationalSquad"/>).
    /// </summary>
    public const int Lines = 3;

    private readonly List<NationalSquadMember> _squad = [];
    private readonly List<FriendlyMatch> _friendlies = [];

    public NationalCoach(int manager, int nation)
    {
        Manager = manager;
        Nation = nation;
    }

    /// <summary>Index des Managers, der Bundestrainer ist.</summary>
    public int Manager { get; }

    /// <summary>Nationalteam (Vereinsindex 52–63), im Original DGROUP 0x6AC.</summary>
    public int Nation { get; }

    public IReadOnlyList<NationalSquadMember> Squad => _squad;

    /// <summary>Freundschaftsspiele dieser Saison (höchstens 8), älteste zuerst.</summary>
    public IReadOnlyList<FriendlyMatch> Friendlies => _friendlies;

    public bool Contains(Player player) => _squad.Any(m => m.Player == player);

    /// <summary>Kosten eines Freundschaftsspiels in DM; Heimspiel doppelt (Datei 0x3BE78).</summary>
    public static long FriendlyCost(int opponent, bool home)
    {
        var i = opponent - NationalTeams.First;
        if ((uint)i >= (uint)FriendlyCostThirds.Length)
            throw new ArgumentOutOfRangeException(nameof(opponent));
        return FriendlyCostThirds[i] / 3 * (home ? 2 : 1);
    }

    /// <summary>Etat für die nächste Saison aus dem WM-Platz des eigenen Landes (null = keine WM gespielt).</summary>
    public static long BudgetFor(int? place) => place is >= 1 and <= 8 ? (11 - place.Value) * 100_000L : 100_000L;

    /// <summary>
    /// Wertebonus eines Freundschaftsspiels (Datei 0x3BFCC–0x3C183, alle Werte ganzzahlig, random mit beiden Grenzen):
    /// x = (Kosten in Drittel-DM / 2000 + random(0, 50)) / 50; b = eigene Tore − Gegentore; auswärts mit x &gt; 0
    /// b += random(1, x), sonst b −= 1; x &lt; 2: b −= 2 − x − (auswärts ? 1 : 0); x &gt; 2: b += x; danach b auf
    /// (x &gt; 2 ? 1 : 0) … (x &lt; 2 ? 1 : 2) begrenzt und bei x &gt; 3 um eins erhöht.
    /// </summary>
    /// <param name="roll">random(0, 50), im Original vor dem Spiel gezogen.</param>
    /// <param name="random">Quelle für random(1, x) nach dem Spiel.</param>
    public static (int X, int Bonus) FriendlyBonus(long costDm, int roll, bool home, int ownGoals, int opponentGoals, Random random)
    {
        var x = (int)((costDm * 3 / 2000 + roll) / 50);
        var b = ownGoals - opponentGoals;
        if (!home && x != 0)
            b += random.Next(1, x + 1);
        else
            b--;
        if (x < 2)
            b -= 2 - x - (home ? 0 : 1);
        if (x > 2)
            b += x;
        var low = x > 2 ? 1 : 0;
        if (b < low)
            b = low;
        var high = x < 2 ? 1 : 2;
        if (b > high)
            b = high;
        if (x > 3)
            b++;
        return (x, b);
    }

    /// <summary>
    /// Bonus auf die drei Werte jedes Kaderspielers (Datei 0x3C18C): ein Wert bekommt eins weniger – bei x &gt; 2 der
    /// erste, sonst der zweite; höchstens 99. [ORIGINAL] Spielerfelder 0x10–0x12 = Kondition, Technik, Form (A023, Spur
    /// nationalteam Runde 13, <c>docs/analyse/re-runde-13.md</c>): „Spieler in WM-Kader“ zeigt in KO/TE/FO der Reihe nach
    /// +0x10/+0x11/+0x12 (DOSBox `file18_01`: KÜNAST 79/81/53, MERK 84/81/50 = FILE18, Satz 5, Platz 2/1). Nach einem
    /// Freundschaftsspiel mit x &gt; 2 FILE18 → FILE19 alle Kaderplätze +1/+2/+2; ZERWESZ und SCHINKO bleiben bei
    /// +0x10 = 99.
    /// </summary>
    public static void ApplyBonus(Player player, int x, int bonus)
    {
        if (bonus <= 0)
            return;
        int Delta(int k) => bonus - ((x > 2 && k == 0) || (x < 3 && k == 1) ? 1 : 0);
        player.Condition = Math.Min(Player.MaxValue, player.Condition + Delta(0));
        player.Technique = Math.Min(Player.MaxValue, player.Technique + Delta(1));
        player.Form = Math.Min(Player.MaxValue, player.Form + Delta(2));
    }

    /// <summary>
    /// Mittel aus Kondition und Technik eines Spielers (nur <see cref="NationalLineup.LineValue"/>). [APPROX] Die Reihenwerte der
    /// Anzeige bildet das Original aus 0x977F (Datei 0x3B65C); den Wert liefert <see cref="GameState.NationalPanelLineValue"/>.
    /// </summary>
    public static int Value(Player p) => (p.Condition + p.Technique) / 2;

    /// <summary>
    /// Aufstellung für Länderspiele, Spielerzähler und Anzeige: die Torhüter- und Reihenplätze 1–3 der
    /// <see cref="Formation"/> (Spur nationalteam-2); Verletzte und Gesperrte fallen heraus. Die Spielberechnung nimmt
    /// alle vier Blöcke (<see cref="MatchEngine.NationalSquad"/>).
    /// [ORIGINAL] Ohne die Einstellung „automatisch aufstellen“ spielt eine leere Formation leer (A009, DOSBox FILE48: Einstellung
    /// 0x37F = 0x380 = 0, 0:17 gegen TSCHECHEI, kein Spieler bekommt ein Spiel). Aufgestellt wird über die Einstellung
    /// (<c>CupsNational.AutoLineup.cs</c>).
    /// A024 (Spur wettbewerbe-rest, Teil 1): Mit der Einstellung, aber leerer Formation (Kader gewählt, das Menü seitdem nicht neu
    /// aufgebaut und kein Tag mit Ereignis gelaufen – nur in Abläufen ohne Oberfläche; im Original folgt auf „Spieler in WM-Kader“
    /// immer der Menüaufbau mit Neuaufstellung, Datei 0x450C6 → 0x426BC) gilt die Aufstellung, die diese Neuaufstellung setzen
    /// würde: [ORIGINAL] Regel 0x426BC/0x42838 für Satz 5 (<see cref="StandInFormation"/>, <see cref="AutoLineupRule.Apply"/>:
    /// Torhüter, drei Abwehr- und drei Sturmreihen nach (Kondition + Technik + Form) / 3, Ersatz bis 22 eingesetzt).
    /// [APPROX] Nur der Zeitpunkt: das Remake wendet die Regel beim Lesen an, ohne die Formation zu schreiben (bis Runde 16
    /// eine eigene Automatik nach Kondition + Technik + Form ohne Ersatz und alle Verfügbaren eingesetzt).
    /// </summary>
    public NationalLineup Lineup() => FormationLineup(UsesStandInLineup ? StandInFormation().Formation : Formation);

    /// <summary>Ersatz von <see cref="Lineup"/>: leere Formation mit Einstellung „automatisch aufstellen“ (A024).</summary>
    internal bool UsesStandInLineup => Formation.Count == 0 && AutoLineupEnabled;

    /// <summary>
    /// Nimmt einen Spieler in den WM-Kader auf. [ORIGINAL] 0x3CABF (Handaufnahme Datei 0x3C97A, „auto“ Datei 0x3CA4C) ruft
    /// 0x42CB1 für Manager 5 mit Grenze 0x1D (Datei 0x3CAF6–0x3CB09): erster Platz, dessen Spielerart größer als die des Neuen
    /// ist, sonst hinten an – wie <see cref="PlayerFactory.InsertSlot"/> (#623, Spur nationalteam Runde 14). „auto“ nimmt je Art
    /// nacheinander auf und bleibt damit unverändert.
    /// </summary>
    internal void Add(Player player, int club)
    {
        var at = _squad.FindIndex(m => m.Player.Role > player.Role);
        _squad.Insert(at < 0 ? _squad.Count : at, new NationalSquadMember(player, club));
    }

    internal void Remove(Player player)
    {
        _squad.RemoveAll(m => m.Player == player);
        ReleaseFromFormation(player);
    }

    internal void ClearSquad()
    {
        _squad.Clear();
        ClearFormationState();
    }

    internal void AddFriendly(FriendlyMatch match) => _friendlies.Add(match);

    internal void ClearFriendlies() => _friendlies.Clear();
}

/// <summary>
/// Aufstellung des Nationalteams (Torhüter, Verteidiger und Stürmer, nach Reihen geordnet). Leere Plätze einer
/// Formation stehen als null in den Listen, damit die Reihen an ihrem Platz bleiben.
/// </summary>
public sealed record NationalLineup(Player? Goalkeeper, IReadOnlyList<Player?> Defenders, IReadOnlyList<Player?> Forwards)
{
    public IEnumerable<Player> Players =>
        (Goalkeeper is null ? Enumerable.Empty<Player>() : [Goalkeeper]).Concat(Defenders.OfType<Player>())
        .Concat(Forwards.OfType<Player>());

    /// <summary>Wert einer Reihe 0–2 (Abwehr bzw. Sturm); fehlende Spieler zählen 0.</summary>
    public int LineValue(int line, bool attack)
    {
        var size = attack ? 3 : 2;
        var list = attack ? Forwards : Defenders;
        return list.Skip(line * size).Take(size).Sum(p => p is null ? 0 : NationalCoach.Value(p)) / size;
    }
}
