namespace EishockeyManager.Core;

/// <summary>Jugendmannschaften im Bildschirm „Jugendteams“ (Reihenfolge wie im Video: rot, grün, blau).</summary>
public enum YouthTeamKind
{
    Schueler,
    Jugend,
    Junioren,
}

/// <summary>Eine Jugendmannschaft: Liga (1–3), Platz (1–12) und Förderung pro Monat.</summary>
/// <remarks>
/// [ORIGINAL] Managersatz DGROUP 0xA078 + 0x15 · Manager: Platz je Team <c>+0..2</c>, Liga <c>+3..5</c> (0-basiert),
/// Förderung <c>+6</c> (3 Langwörter); Bildschirm em.exe Datei 0x3A24B (Doku: <c>docs/finanzen-rekonstruktion.md</c>).
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

    public required YouthTeamKind Kind { get; init; }
    public int League { get; set; } = Leagues;
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

    /// <summary>Die drei Jugendmannschaften zu Spielbeginn: 6. Platz in der Liga des Vereins.</summary>
    /// <remarks>
    /// [ORIGINAL] Spielstart Datei 0x29BC1 setzt das Ligabyte auf die Start-Liga (Setup DGROUP 0x11A, 0 = Bundesliga).
    /// [APPROX] Das Remake nimmt die Liga des Managervereins (sie ist beim Spielstart die Start-Liga).
    /// </remarks>
    public static YouthTeam[] StartTeams(League? clubLeague)
    {
        var league = (int)(clubLeague ?? Core.League.Oberliga) + 1;
        return [.. Enum.GetValues<YouthTeamKind>().Select(kind => new YouthTeam { Kind = kind, League = league })];
    }

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
    public YouthTeam[] Youth { get; init; } = YouthTeam.StartTeams(null);

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
    /// Monatlich: Förderung bezahlen (Finanzbuch „Jugend“) und Tabellenstand der Jugendteams fortschreiben.
    /// [APPROX] Wirkung nicht übernommen (im Original Datei 0x39DEC und Saisonwechsel 0x3A13B, Restlücke Spur finanzen-rest):
    /// volle Förderung bringt meist einen Platz nach oben, ohne Förderung geht es eher nach unten.
    /// </summary>
    private void SettleYouth(Manager manager)
    {
        foreach (var team in Finance(manager).Youth)
        {
            if (team.Funding > 0)
                Book(manager, FinanceCategory.Jugend, -team.Funding);
            var share = team.Funding / (double)YouthTeam.MaxFunding(team.Kind);
            var r = FinanceRandom.NextDouble();
            if (r < 0.15 + 0.6 * share)
                team.MoveTo(team.Position - 1);
            else if (r > 0.75 + 0.2 * share)
                team.MoveTo(team.Position + 1);
        }
    }
}

/// <summary>Laufende Talentsuche des „Spions“.</summary>
public sealed class TalentSearch
{
    public required int WeeksLeft { get; set; }
}

public sealed partial class ManagerFinance
{
    /// <summary>Laufende Talentsuche oder null.</summary>
    public TalentSearch? TalentSearch { get; set; }
}

public sealed partial class GameState
{
    /// <summary>[APPROX] Rabatt je volle 1.000 DM Jugendförderung (bis Teil 1 der Förderschritt aller Teams).</summary>
    private const long TalentDiscountUnit = 1_000;

    /// <summary>„Rabatt:“ auf die Talentsuche in Prozent (siehe <see cref="TalentScout"/>).</summary>
    public int TalentSearchDiscount(Manager manager) =>
        (int)Math.Min(TalentScout.MaxDiscount,
            Finance(manager).YouthFunding / TalentDiscountUnit * TalentScout.DiscountPerFundingStep);

    /// <summary>„Preis:“ der Talentsuche nach Rabatt.</summary>
    public long TalentSearchPrice(Manager manager) =>
        TalentScout.BasePrice * (100 - TalentSearchDiscount(manager)) / 100;

    /// <summary>
    /// Schickt den „Spion“ los (Jugendteams und Knopf „Talent“ im Transfer) und bezahlt sofort
    /// (Finanzbuch „Talentsuche“). Ohne Wirkung, wenn schon gesucht wird, das Geld fehlt oder der Kader voll ist.
    /// </summary>
    public TalentSearchStart StartTalentSearch(Manager manager)
    {
        var finance = Finance(manager);
        var price = TalentSearchPrice(manager);
        if (finance.TalentSearch is not null)
            return TalentSearchStart.AlreadyRunning;
        if (Clubs[manager.ClubIndex].Players.Count >= TalentScout.MaxSquad)
            return TalentSearchStart.SquadFull;
        if (manager.Money < price)
            return TalentSearchStart.NotEnoughMoney;
        Book(manager, FinanceCategory.Talentsuche, -price);
        finance.TalentSearch = new TalentSearch { WeeksLeft = TalentScout.Weeks };
        return TalentSearchStart.Started;
    }

    /// <summary>Wöchentlich: Ergebnis der Talentsuche als Nachricht (Texte aus em.exe).</summary>
    private void AdvanceTalentSearch(Manager manager)
    {
        var finance = Finance(manager);
        if (finance.TalentSearch is not { } search || --search.WeeksLeft > 0)
            return;
        finance.TalentSearch = null;
        var club = Clubs[manager.ClubIndex];
        if (club.Players.Count < TalentScout.MaxSquad && FinanceRandom.NextDouble() < TalentScout.SuccessChance(finance))
        {
            PlayerFactory.Join(club, CreateTalent(club), SquadSetup.StartContractYears, loan: false);
            Notify(manager, "Ihre Talentsuche war ein Erfolg.", "Ein junger Spieler stößt zu Ihrem Kader.");
        }
        else
            Notify(manager, "Ihr Talentspäher gibt die", "Suche ohne Erfolg auf... Pech...");
    }

    /// <summary>Junger Spieler aus der Talentsuche, vollständig über die <see cref="PlayerFactory"/> erzeugt.</summary>
    private Player CreateTalent(Club club)
    {
        var role = (Formats.PlayerRole)FinanceRandom.Next(3);
        var name = PlayerFactory.TalentName(Clubs.SelectMany(c => c.Players).ToList(), FinanceRandom);
        return PlayerFactory.CreateTalent(this, name, role, club.Strength, FinanceRandom);
    }
}
