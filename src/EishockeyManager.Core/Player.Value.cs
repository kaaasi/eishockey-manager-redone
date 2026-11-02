namespace EishockeyManager.Core;

/// <summary>Was die Spielerwert-Funktion 0x118:0x70 liefert (Bit 0 ihres zweiten Arguments).</summary>
public enum PlayerValueKind
{
    /// <summary>Wert/Kaufpreis (Bit 0 = 0): Kaderplatz +0x2C, Highscore, Werbebewertung, Angebote, Versicherung.</summary>
    Price = 0,

    /// <summary>Monatliches Gehalt (Bit 0 = 1): Kaderplatz +0x28, Vertragsverhandlung.</summary>
    Salary = 1,
}

/// <summary>
/// Eingänge der Spielerwert-Funktion in den Feldern des Originals: Kaderplatz (0x34 Byte, DGROUP 0x5DC3 + 0x618 · Satz +
/// 0x34 · Platz), Spielerpool (33 Byte, <c>far:8253</c>) und DGROUP 0xED8. Bytes werden als 0–255, Wörter als 0–65535
/// erwartet; größere Werte schneidet <see cref="PlayerValue.Compute"/> wie das Original ab.
/// </summary>
public readonly record struct PlayerValueInputs
{
    /// <summary>5-Minuten-Strafen, Kaderplatz +0x00 (Byte).</summary>
    public int Penalties5 { get; init; }

    /// <summary>2-Minuten-Strafen, Kaderplatz +0x01 (Byte).</summary>
    public int Penalties2 { get; init; }

    /// <summary>Tore im ersten Wettbewerb (Liga), Kaderplatz +0x03 (Byte), Gewicht 30.</summary>
    public int Goals0 { get; init; }

    /// <summary>Tore im zweiten Wettbewerb, Kaderplatz +0x04 (Byte), Gewicht 30.</summary>
    public int Goals1 { get; init; }

    /// <summary>Tore im dritten Wettbewerb, Kaderplatz +0x05 (Byte), Gewicht 40.</summary>
    public int Goals2 { get; init; }

    /// <summary>Tendenz 30–68 (Kaderplatz +0x0E; Anzeige (Wert − 30) / 13: Fallend, Konstant, Steigend).</summary>
    public int Trend { get; init; }

    /// <summary>Erste Stärke (Kondition), Kaderplatz +0x10 (Byte).</summary>
    public int Condition { get; init; }

    /// <summary>Zweite Stärke (Technik), Kaderplatz +0x11 (Byte). Die Form (+0x12) geht nicht ein.</summary>
    public int Technique { get; init; }

    /// <summary>
    /// Kaderplatz +0x1C (Wort), Gewicht 50: laut <c>docs/formate/spielstand-original.md</c> (FILE07) die Spiele für den
    /// jetzigen Verein (nach einem Kauf 0) – nicht „Spiele im ersten Wettbewerb“ (die stehen in +0x06–+0x08; SW-02).
    /// </summary>
    public int Games0 { get; init; }

    /// <summary>Kaderplatz +0x1E (Wort), Gewicht 20; Bedeutung offen (<c>spielstand-original.md</c>, SW-02).</summary>
    public int Games1 { get; init; }

    /// <summary>Kaderplatz +0x20 (Wort), Gewicht 30; Bedeutung offen (<c>spielstand-original.md</c>, SW-02).</summary>
    public int Games2 { get; init; }

    /// <summary>
    /// Verein, der ein Angebot für den Spieler abgegeben hat (Kaderplatz +0x09 Bit 0x80 gesetzt, Verein in +0x16), oder null.
    /// </summary>
    public int? OfferClub { get; init; }

    /// <summary>Alter, Spielerpool +0x14 (Byte).</summary>
    public int Age { get; init; }

    /// <summary>Torhüter (Rollenwert Spielerpool +0x18 = 0).</summary>
    public bool Goalkeeper { get; init; }

    /// <summary>Spielklasse des Vereins (Spielerpool +0x1D über 0x34FD5, <see cref="CupClass.Of"/>): 1, 2 oder 4.</summary>
    public int ClubClass { get; init; }

    /// <summary>„5 − Level“ (DGROUP 0xED8); nur beim Gehalt von Belang.</summary>
    public int LevelValue { get; init; }
}

/// <summary>
/// Wert und Gehalt eines Spielers – einzige Quelle im Remake. [ORIGINAL] em.exe Datei 0x49D36–0x4A1DA (Overlay 0x118 +
/// 0x3D36, Einsprung 0x118:0x70, 1189 Byte), Ergebnis in <b>Drittel-DM</b> (vorzeichenbehaftete 32-Bit-Zahl). Im Emulator
/// gegen die Originalfunktion geprüft (<c>SpielerwertTests</c>, 2.000 Zufallsfälle gleich).
/// <para>Rechnung (alle Teilungen ganzzahlig, zur Null hin; „Zeile“ = Tabelle DGROUP 0x5312, 6 Byte je Art):</para>
/// <code>
/// Zeile Preis 58, 4, 4, 5, 4, 25 · Zeile Gehalt 70, 8, 7, 5, 8, 2
/// v  = (KO + TE) / 2 · z0 / 100                                                (0x49D6D)
///    + z1 · (T · 100 / 20) / 100,  T = clamp((30·T0 + 30·T1 + 40·T2) / 100, 0, 20)  (0x49DA6, 0x49E42)
///    + z2 · (S · 100 / 200) / 100, S = clamp(((50·S0 + 20·S1 + 30·S2) mod 2^16) / 100, 0, 200) (0x49DE6, 0x49E73)
///    + Tendenz · z3 / 100                                                      (0x49EA4)
///    + z4 · (P · 100 / 20) / 100,  P = clamp(6 · 5-Min + 2-Min, 0, 20)          (0x49ED0)
///    + z5 · (A · 100 / 27) / 100,  A = clamp(45 − Alter, 0, 27)                 (0x49F2D)
/// Preis:  v = v · 5000; über 55.000 → − 50.000 (0x4A079); Bit 1 → / 3 (0x4A0A7, von keinem Aufrufer gesetzt);
///         Angebot (+0x09 Bit 0x80): Verein &gt; 63 → · 160 / 100, sonst · random(95, 100) / 100 (0x4A0C5)
/// Gehalt: v = v³ / (200 · [0xED8] + 1400) (0x49F96); E = KO + TE, über 100 → (E − 100) / 2 + 100, dann über 135
///         → v · (E − 35) / 100 (0x49FD2); mindestens 2; · 85 (0x4A066)
/// beide:  v = v · 10 / 3 (0x4A11D); Torhüter (Rollenwert 0) → (v · 16) / 10 (0x4A141)
/// Preis:  v / 3000 · 3000 (0x4A1AA) · Gehalt: (v + Grundgehalt) / 300 · 300 mit Grundgehalt 2400 / 1800 / 990
///         nach Spielklasse 1 / 2 / 4 (DGROUP 0x52C2, Index Klasse &gt;&gt; 1, 0x4A176)
/// </code>
/// Zufall: nur Preis mit Angebot eines Vereins mit Index ≤ 63 zieht random(95, 100) (0x4A0F9).
/// </summary>
public static class PlayerValue
{
    /// <summary>Gewichte je Art (DGROUP 0x5312, vorzeichenbehaftete Bytes).</summary>
    private static readonly int[][] Weights = [[58, 4, 4, 5, 4, 25], [70, 8, 7, 5, 8, 2]];

    /// <summary>Grundgehalt in Drittel-DM nach Spielklasse &gt;&gt; 1 (DGROUP 0x52C2, Dwords).</summary>
    private static readonly int[] BaseSalary = [2400, 1800, 990];

    /// <summary>
    /// Die Originalrechnung. <paramref name="third"/> ist Bit 1 des Arguments (nur beim Preis wirksam);
    /// <paramref name="random"/> liefert random(a, b) mit beiden Grenzen inklusive.
    /// </summary>
    public static int Compute(in PlayerValueInputs x, PlayerValueKind kind, Func<int, int, int> random, bool third = false)
    {
        var salary = kind == PlayerValueKind.Salary;
        var z = Weights[salary ? 1 : 0];
        int s0 = (byte)x.Condition, s1 = (byte)x.Technique;

        int v = (s0 + s1) / 2 * z[0] / 100;

        var goals = (short)((byte)x.Goals0 * 30 + (byte)x.Goals1 * 30 + (byte)x.Goals2 * 40) / 100;
        var games = (ushort)((ushort)x.Games0 * 50 + (ushort)x.Games1 * 20 + (ushort)x.Games2 * 30) / 100;
        goals = Math.Clamp(goals, 0, 20);
        games = Math.Clamp(games, 0, 200);
        v += z[1] * (goals * 100 / 20) / 100;
        v += z[2] * (games * 100 / 200) / 100;
        v += (byte)x.Trend * z[3] / 100;

        var penalties = Math.Clamp((byte)x.Penalties5 * 6 + (byte)x.Penalties2, 0, 20);
        v += z[4] * (penalties * 100 / 20) / 100;
        var youth = Math.Clamp(45 - (byte)x.Age, 0, 27);
        v += z[5] * (youth * 100 / 27) / 100;

        if (salary)
        {
            var divisor = (int)(short)((sbyte)x.LevelValue * 200 + 1400);
            v = unchecked(v * v * v) / divisor;
            var e = s0 + s1;
            if (e > 100)
            {
                e = ((e - 100) >> 1) + 100;
                if (e > 135)
                    v = unchecked(v * (e - 35)) / 100;
            }
            if (v < 2)
                v = 2;
            v = unchecked(v * 85);
        }
        else
        {
            v = unchecked(v * 5000);
            if (v > 55_000)
                v -= 50_000;
            if (third)
                v /= 3;
            if (x.OfferClub is { } club)
            {
                if ((byte)club > 63)
                    v = unchecked(v * 160) / 100;
                else
                    v = unchecked(v * random(95, 100)) / 100;
            }
        }

        v = unchecked(v * 10) / 3;
        if (x.Goalkeeper)
            v = unchecked(v << 4) / 10;

        return salary
            ? unchecked((v + BaseSalary[ClassIndex(x.ClubClass)]) / 300 * 300)
            : unchecked(v / 3000 * 3000);
    }

    /// <summary>Index in die Grundgehälter: Spielklasse &gt;&gt; 1 (1 → 0, 2 → 1, 4 → 2).</summary>
    private static int ClassIndex(int clubClass) => Math.Clamp(clubClass >> 1, 0, BaseSalary.Length - 1);

    /// <summary>
    /// Tendenz einer Stufe ohne <see cref="Player.TrendScore"/> (alte Spielstände, Spieler ohne neuen Kaderplatz). [APPROX] Mitte der Spannen
    /// 30–42, 43–55, 56–68 (Anzeige (Wert − 30) / 13, Datei 0x41AE0).
    /// </summary>
    public static int TrendValue(PlayerTrend trend) => trend switch
    {
        PlayerTrend.Falling => 36,
        PlayerTrend.Rising => 62,
        _ => 49,
    };

    /// <summary>
    /// Eingänge eines Spielers des Remakes. <paramref name="club"/> ist der Verein, in dessen Kader er steht (null =
    /// vertragslos; Original 0xFF → Spielklasse 1); <paramref name="offer"/> der Verein mit einem offenen Angebot.
    /// [APPROX] Das Remake zählt Tore nicht je Wettbewerb (alle in +0x03) und Spiele nicht je Wettbewerb (alle Spiele beim jetzigen
    /// Verein in +0x1C, <see cref="Player.GamesForClub"/>; +0x1E/+0x20 bleiben 0; SW-02);
    /// Tendenz: <see cref="Player.TrendScore"/>, ohne Wert die Mitte der Stufe (<see cref="TrendValue"/>); der Level ist im Remake je Manager.
    /// </summary>
    public static PlayerValueInputs Inputs(Player player, Club? club, Club? offer = null, int level = 3) => new()
    {
        Penalties5 = Math.Min(player.Penalties5, 255),
        Penalties2 = Math.Min(player.Penalties2, 255),
        Goals0 = Math.Min(player.Goals, 255),
        Trend = player.TrendScore != 0 ? player.TrendScore : TrendValue(player.Trend),
        Condition = Math.Clamp(player.Condition, 0, 255),
        Technique = Math.Clamp(player.Technique, 0, 255),
        Games0 = Math.Min(player.GamesForClub, 0xFFFF),
        OfferClub = offer?.Index,
        Age = Math.Clamp(player.Age, 0, 255),
        Goalkeeper = player.Role == Formats.PlayerRole.Goalkeeper,
        ClubClass = club is null ? 1 : CupClass.Of(club),
        LevelValue = TrainingRules.LevelValue(level),
    };
}

public sealed partial class GameState
{
    /// <summary>
    /// Wert (bzw. Gehalt) eines Kaderspielers in Drittel-DM nach 0x118:0x70. Ein offenes Angebot eines Computervereins
    /// (<see cref="TransferMarket.Bids"/>, erstes Gebot) gilt als Kaderplatz +0x09 Bit 0x80 / +0x16. [ORIGINAL] Bit 0x80 setzt nur
    /// der Tagesablauf für die selbst angebotenen Marktplätze (Datei 0x07358, Verein Datei 0x0736E); Kaderspieler bekommen nur
    /// Bit 0x40 (Datei 0x07A5C/0x07A8F), das nicht in den Wert eingeht (0x4A0C8) – Spur kader-transfer, Teil 7 (A136,
    /// <c>docs/analyse/re-nachtrag.md</c>, Abschnitt 2).
    /// <paramref name="random"/> wird nur beim Preis eines Spielers mit Angebot benutzt.
    /// </summary>
    public int PlayerValueOf(Player player, Club? club, PlayerValueKind kind, Random random, int level = 3)
    {
        var offer = TransferMarket.Bids.FirstOrDefault(b => b.Listing.Player == player)?.Buyer;
        var inputs = PlayerValue.Inputs(player, club, offer, level);
        return PlayerValue.Compute(inputs, kind, (a, b) => SquadSetup.Between(random, a, b));
    }
}
