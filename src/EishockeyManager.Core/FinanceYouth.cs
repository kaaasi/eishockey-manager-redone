namespace EishockeyManager.Core;

/// <summary>Jugendmannschaften im Bildschirm „Jugendteams“ (Reihenfolge wie im Video: rot, grün, blau).</summary>
public enum YouthTeamKind
{
    Schueler,
    Jugend,
    Junioren,
}

/// <summary>Eine Jugendmannschaft: Stärke, Liga (1–3), Platz (1–12) und Förderung pro Monat.</summary>
/// <remarks>
/// [ORIGINAL] Managersatz DGROUP 0xA075 + 0x15 · Manager: Stärke je Team <c>+0..2</c>, Platz <c>+3..5</c> (0-basiert),
/// Liga <c>+6..8</c> (0-basiert), Förderung <c>+9</c> (3 Langwörter, Drittel-DM); Bildschirm em.exe Datei 0x3A24B,
/// Spieltag Datei 0x39DEC, Saisonwechsel Datei 0x3A13B (Doku: <c>docs/finanzen-rekonstruktion.md</c>, <c>docs/analyse/re-geld.md</c>).
/// </remarks>
public sealed class YouthTeam
{
    /// <summary>
    /// Plätze je Jugendliga. [ORIGINAL] Die Fahne steigt 2 Pixel je Platz in einem 24-Pixel-Ligaband (Datei 0x3A3B2),
    /// Folge 4 0:28:25 zeigt „12.Platz 3.Liga“ an der Unterkante.
    /// </summary>
    public const int TeamsPerLeague = 12;
    public const int Leagues = 3;

    /// <summary>Startplatz. [ORIGINAL] Spielstart Datei 0x29BAA setzt das Platzbyte auf 5 (= 6. Platz, UI-Video 8:35).</summary>
    public const int StartRank = 6;

    /// <summary>Breite des Förderbalkens in Pixeln (x 0x78–0xBE, Datei 0x3A6B3).</summary>
    public const int BarPixels = 70;

    /// <summary>Förderdeckel der Schüler; jede ältere Mannschaft das Doppelte (siehe <see cref="MaxFunding"/>).</summary>
    private const long BaseMaxFunding = 10_000;
    private const long BaseFundingStep = 500;

    /// <summary>Stärke-Grenzen. [ORIGINAL] Datei 0x39FED–0x39FFA: Klemme 0x120:0x57 (Datei 0x4BD56) auf 10 … 0x55.</summary>
    public const int MinStrength = 10;
    public const int MaxStrength = 85;

    /// <summary>Bezugsstärke je Jugendliga (1. Liga zuerst). [ORIGINAL] DGROUP 0x4EBA = 85, 70, 57 (Datei 0x1BC8A).</summary>
    public static readonly IReadOnlyList<int> LeagueReference = [85, 70, 57];

    public required YouthTeamKind Kind { get; init; }
    public int League { get; set; } = Leagues;

    /// <summary>Stärke der Mannschaft (10–85, <see cref="MinStrength"/>/<see cref="MaxStrength"/>).</summary>
    public int Strength { get; set; } = StartStrength(Core.League.Oberliga);

    /// <summary>
    /// Startstärke. [ORIGINAL] Spielstart Datei 0x29B76–0x29B98: <c>(2 − StartLiga) · 14 + 0x34</c>, also 80 (Bundesliga),
    /// 66 (2. Liga), 52 (Oberliga), für alle drei Mannschaften gleich.
    /// </summary>
    public static int StartStrength(League startLeague) => (2 - (int)startLeague) * 14 + 52;
    public int Rank { get; set; } = StartRank;

    /// <summary>Förderung in DM pro Monat (0 bis <see cref="MaxFunding"/> in Schritten von <see cref="FundingStep"/>).</summary>
    public long Funding { get; set; }

    /// <summary>Deckel der Förderung. [ORIGINAL] <c>10000 · 2^Team</c> (Datei 0x3A72F, Balken 0x39C1F; UI-Video 8:39 „Jugend : 19000“).</summary>
    public static long MaxFunding(YouthTeamKind kind) => BaseMaxFunding << (int)kind;

    /// <summary>Schritt der Förderung. [ORIGINAL] <c>500 · 2^Team</c> (Datei 0x3A6B7, LXMUL).</summary>
    public static long FundingStep(YouthTeamKind kind) => BaseFundingStep << (int)kind;

    /// <summary>
    /// Förderung nach einem Klick <paramref name="offset"/> Pixel rechts von x = 120 in den Balken (1–70).
    /// [ORIGINAL] Datei 0x3A798: <c>⌊⌊offset · 2^Team · 10000 / 70⌋ / Schritt⌋ · Schritt</c>.
    /// </summary>
    public static long BarFunding(YouthTeamKind kind, int offset)
    {
        var raw = Math.Clamp(offset, 0, BarPixels) * MaxFunding(kind) / BarPixels;
        return raw / FundingStep(kind) * FundingStep(kind);
    }

    /// <summary>Breite der Balkenfüllung in Pixeln. [ORIGINAL] <c>w = Förderung · 70 / Deckel</c>, gezeichnet ab w &gt; 2 mit w − 1 Pixeln (Datei 0x39D52).</summary>
    public int BarFill
    {
        get
        {
            var w = Funding * BarPixels / MaxFunding(Kind);
            return w > 2 ? (int)w - 1 : 0;
        }
    }

    /// <summary>Die drei Jugendmannschaften zu Spielbeginn: 6. Platz in der Start-Liga, Startstärke nach der Start-Liga.</summary>
    /// <remarks>
    /// [ORIGINAL] Spielstart Datei 0x29BBB–0x29BC1 setzt das Ligabyte auf die Start-Liga (Setup DGROUP 0x11A, 0 = Bundesliga,
    /// im Remake <see cref="ManagerSetup.StartLeague"/>, #327), 0x29BAA den Platz 5 (= 6.), 0x29B98 die Stärke.
    /// </remarks>
    public static YouthTeam[] StartTeams(League startLeague) =>
        [.. Enum.GetValues<YouthTeamKind>().Select(kind => new YouthTeam
        {
            Kind = kind, League = (int)startLeague + 1, Strength = StartStrength(startLeague),
        })];

    public string Label => Kind switch
    {
        YouthTeamKind.Schueler => "Schüler",
        YouthTeamKind.Jugend => "Jugend",
        _ => "Junioren",
    };

    /// <summary>Gesamtposition 0 (1. Platz 1. Liga) … 35 (12. Platz 3. Liga).</summary>
    public int Position => (League - 1) * TeamsPerLeague + Rank - 1;

    public void MoveTo(int position)
    {
        position = Math.Clamp(position, 0, Leagues * TeamsPerLeague - 1);
        League = position / TeamsPerLeague + 1;
        Rank = position % TeamsPerLeague + 1;
    }
}

public sealed partial class ManagerFinance
{
    /// <summary>Jugendmannschaften (Schüler, Jugend, Junioren); <see cref="GameState.Finance"/> setzt die Startliga.</summary>
    public YouthTeam[] Youth { get; init; } = YouthTeam.StartTeams(League.Oberliga);

    /// <summary>Gesamte Jugendförderung pro Monat.</summary>
    public long YouthFunding => Youth.Sum(t => t.Funding);
}

public sealed partial class GameState
{
    /// <summary>
    /// Setzt die Förderung einer Jugendmannschaft: begrenzt auf 0 bis <see cref="YouthTeam.MaxFunding"/> und auf den
    /// Schritt der Mannschaft abgerundet (wie der Klick in den Balken, Datei 0x3A798).
    /// </summary>
    public void SetYouthFunding(Manager manager, YouthTeamKind kind, long amount)
    {
        var step = YouthTeam.FundingStep(kind);
        Finance(manager).Youth[(int)kind].Funding = Math.Clamp(amount, 0, YouthTeam.MaxFunding(kind)) / step * step;
    }

    /// <summary>
    /// Pfeilklick neben dem Balken: einen Schritt mehr bis zum Deckel bzw. weniger bis 0.
    /// [ORIGINAL] Datei 0x3A713 (x &gt; 190, unter dem Deckel: + Schritt) und 0x3A755 (x &lt; 121, nicht 0: − Schritt).
    /// </summary>
    public void StepYouthFunding(Manager manager, YouthTeamKind kind, bool up)
    {
        var team = Finance(manager).Youth[(int)kind];
        var step = YouthTeam.FundingStep(kind);
        if (up && team.Funding < YouthTeam.MaxFunding(kind))
            team.Funding += step;
        else if (!up && team.Funding != 0)
            team.Funding -= step;
    }

    /// <summary>
    /// Monatlich: Förderung bezahlen (Finanzbuch „Jugend“, [ORIGINAL] Ausgaben Datei 0x3E811). Die Wirkung läuft je
    /// Ligaspieltag (<see cref="YouthMatchDay"/>) und im Saisonwechsel (<see cref="YouthSeasonChange"/>).
    /// </summary>
    private void SettleYouth(Manager manager)
    {
        foreach (var team in Finance(manager).Youth)
            if (team.Funding > 0)
                Book(manager, FinanceCategory.Jugend, -team.Funding);
    }

    /// <summary>
    /// Ligaspieltag für die Jugendteams aller Manager, vor den Spielen des Tages. [ORIGINAL] Tagesablauf Datei
    /// 0x55008–0x5502C: an Tagen mit Kalenderbit 0–2 (<see cref="DayEvents.League"/>) je menschlichem Manager Datei 0x39DEC
    /// (Stub 0xF8:0x2F), danach erst die Spiele (0xF0:0x3E). Der Zähler DGROUP 0x7A9 (Bundesliga) steht dann auf dem
    /// noch nicht gespielten Spieltag; das Remake bestimmt ihn aus dem Datum, damit ein Nachholen ihn nicht verschiebt.
    /// </summary>
    private void DailyYouth(int day)
    {
        var index = SeasonCalendar.DaysOf(DayEvents.League).ToList().IndexOf(day);
        if (index < 0)
            return;
        var counter = (Schedules.TryGetValue(Core.League.Bundesliga, out var days) ? Math.Min(index, days.Count) : index) + 1;
        foreach (var manager in Managers)
            YouthMatchDay(manager, counter);
    }

    /// <summary>Anzahl der Schrittweiten-Felder der Jugend (Stapelfeld <c>[bp−0x36]</c>, Datei 0x39DF3–0x39E2F).</summary>
    public const int YouthStepSlots = 44;

    /// <summary>
    /// Jugendteams eines Managers an einem Ligaspieltag. [ORIGINAL] em.exe Datei 0x39DEC (Ov 0xF8 + 0x41C), Zufall
    /// random(a, b) = Datei 0xC037 (<see cref="TransferRules.Random16"/>), Reihenfolge der Züge wie im Original:
    /// <code>
    /// T[0..43]: a = 4; je k: T[k] = a; wenn a &gt; 0 und random(0, 8) == 0: a −= 1      (bei jedem Aufruf neu)
    /// je Team t = 0..2:
    ///   F = Förderung / 3 (DM); s = Stärke; p = Platz (0–11); ref = DGROUP 0x4EBA[Liga] − p
    ///   X = (F · 100 / (10000 · 2^t)) / 2 + 40;  Y = X · 100 / s
    ///   d = sign(Y − 100); wenn random(0, 4) == 0: d = random(0, 2) − 1
    ///   e = random(0, 30) &lt; |Y − 100| ? d : 0;  wenn random(0, 2) == 0: s += e;  s = klemme(s, 10, 85)
    ///   g = s − ref; d = sign(g); wenn random(0, 4) == 0: d = random(0, 2) − 1
    ///   wenn random(0, 3) != 0 und random(1, 18) &gt; 2 · |g|: d = 0
    ///   p −= random(1, T[DGROUP 0x7A9]) · d;  p = klemme(p, 0, 11)       (Klemme Datei 0x4BD56 mit Vorzeichen)
    /// </code>
    /// T[44] liegt im Original auf dem Stapelplatz von a (<c>[bp−0xA]</c>) und trägt dessen Endwert; der Zähler erreicht 44
    /// am 44. Bundesliga-Spieltag. Der Remake-Spielstand führt die Förderung in DM (Original Drittel-DM, DGROUP 0x79D = 3,
    /// 0x7A1 = 1), F ist damit dieselbe Zahl.
    /// </summary>
    /// <param name="nextMatchDay">DGROUP 0x7A9: Nummer des anstehenden Bundesliga-Spieltags (1 am ersten).</param>
    public void YouthMatchDay(Manager manager, int nextMatchDay)
    {
        var random = FinanceRandom;
        int R(int lo, int hi) => TransferRules.Random16(random, lo, hi);

        var steps = new int[YouthStepSlots + 1];
        var a = 4;
        for (var k = 0; k < YouthStepSlots; k++)
        {
            steps[k] = a;
            if (a != 0 && R(0, 8) == 0)
                a--;
        }
        steps[YouthStepSlots] = a;
        // [APPROX] Zähler über 44 kommen im Remake nicht vor (höchstens 44 Ligaspieltage); sie läsen im Original weitere
        // Stapelbytes. Das Remake nimmt dann das letzte Feld.
        var step = steps[Math.Clamp(nextMatchDay, 0, YouthStepSlots)];

        foreach (var team in Finance(manager).Youth)
        {
            var t = (int)team.Kind;
            var s = team.Strength;
            var p = team.Rank - 1;
            var reference = YouthTeam.LeagueReference[team.League - 1] - p;

            var x = team.Funding * 100 / (10_000L << t) / 2 + 40;
            var y = x * 100 / s;
            var d = Math.Sign(y - 100);
            if (R(0, 4) == 0)
                d = R(0, 2) - 1;
            var e = R(0, 30) < Math.Abs(y - 100) ? d : 0;
            if (R(0, 2) == 0)
                s += e;
            s = Math.Clamp(s, YouthTeam.MinStrength, YouthTeam.MaxStrength);
            team.Strength = s;

            var g = s - reference;
            d = Math.Sign(g);
            if (R(0, 4) == 0)
                d = R(0, 2) - 1;
            if (R(0, 3) != 0 && R(1, 18) > 2 * Math.Abs(g))
                d = 0;
            p -= R(1, step) * d;
            team.Rank = Math.Clamp(p, 0, YouthTeam.TeamsPerLeague - 1) + 1;
        }
    }

    /// <summary>
    /// Saisonwechsel der Jugendteams aller Manager (ohne Zufall). [ORIGINAL] em.exe Datei 0x3A13B (Ov 0xF8 + 0x76B), gerufen
    /// je menschlichem Manager im Saisonwechsel (Datei 0x55889), Teams t = 2, 1, 0:
    /// <code>
    /// neu = 5 (6. Platz); Platz 1–2 und nicht 1. Liga: eine Liga höher, neu = Platz + 10 (11./12.)
    /// Platz 12 und nicht 3. Liga: eine Liga tiefer, neu = 1 (2. Platz)
    /// t &gt; 0: Stärke[t] = (2 · Stärke[t] + Stärke[t − 1]) / 3      (das jüngere Team rückt nach)
    /// </code>
    /// </summary>
    public void YouthSeasonChange(Manager manager)
    {
        var youth = Finance(manager).Youth;
        for (var t = youth.Length - 1; t >= 0; t--)
        {
            var team = youth[t];
            var p = team.Rank - 1;
            var l = team.League - 1;
            var next = YouthTeam.StartRank - 1;
            if (p < 2 && l > 0)
            {
                l--;
                next = p + 10;
            }
            if (p > 10 && l < YouthTeam.Leagues - 1)
            {
                l++;
                next = p - 10;
            }
            if (t > 0)
                team.Strength = (2 * team.Strength + youth[t - 1].Strength) / 3;
            team.Rank = next + 1;
            team.League = l + 1;
        }
    }
}
