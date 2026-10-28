namespace EishockeyManager.Core;

/// <summary>Jugendmannschaften im Bildschirm „Jugendteams“ (Reihenfolge wie im Video: rot, grün, blau).</summary>
public enum YouthTeamKind
{
    Schueler,
    Jugend,
    Junioren,
}

/// <summary>Eine Jugendmannschaft: Liga (1–3), Platz (1–6) und Förderung pro Monat.</summary>
public sealed class YouthTeam
{
    /// <summary>Plätze je Jugendliga. [APPROX] Im Video steht zu Spielbeginn jede Mannschaft auf „6.Platz 3.Liga“ ganz unten.</summary>
    public const int TeamsPerLeague = 6;
    public const int Leagues = 3;
    public const long MaxFunding = 10_000;
    public const long FundingStep = 1_000;

    public required YouthTeamKind Kind { get; init; }
    public int League { get; set; } = Leagues;
    public int Rank { get; set; } = TeamsPerLeague;

    /// <summary>Förderung in DM pro Monat (0–10.000 in Schritten von 1.000, Video 8:35).</summary>
    public long Funding { get; set; }

    public string Label => Kind switch
    {
        YouthTeamKind.Schueler => "Schüler",
        YouthTeamKind.Jugend => "Jugend",
        _ => "Junioren",
    };

    /// <summary>Gesamtposition 0 (1. Platz 1. Liga) … 17 (6. Platz 3. Liga).</summary>
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
    public YouthTeam[] Youth { get; } =
        [new() { Kind = YouthTeamKind.Schueler }, new() { Kind = YouthTeamKind.Jugend }, new() { Kind = YouthTeamKind.Junioren }];

    /// <summary>Gesamte Jugendförderung pro Monat.</summary>
    public long YouthFunding => Youth.Sum(t => t.Funding);
}

public sealed partial class GameState
{
    /// <summary>Setzt die Förderung einer Jugendmannschaft (auf 1.000 DM gerundet, 0–10.000 DM).</summary>
    public void SetYouthFunding(Manager manager, YouthTeamKind kind, long amount)
    {
        var steps = (long)Math.Round(Math.Clamp(amount, 0, YouthTeam.MaxFunding) / (double)YouthTeam.FundingStep);
        Finance(manager).Youth[(int)kind].Funding = steps * YouthTeam.FundingStep;
    }

    /// <summary>
    /// Monatlich: Förderung bezahlen (Finanzbuch „Jugend“) und Tabellenstand der Jugendteams fortschreiben.
    /// [APPROX] Wirkung unbekannt: volle Förderung bringt meist einen Platz nach oben, ohne Förderung geht es eher nach unten.
    /// </summary>
    private void SettleYouth(Manager manager)
    {
        foreach (var team in Finance(manager).Youth)
        {
            if (team.Funding > 0)
                Book(manager, FinanceCategory.Jugend, -team.Funding);
            var share = team.Funding / (double)YouthTeam.MaxFunding;
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
    /// <summary>„Rabatt:“ auf die Talentsuche in Prozent (siehe <see cref="TalentScout"/>).</summary>
    public int TalentSearchDiscount(Manager manager) =>
        (int)Math.Min(TalentScout.MaxDiscount,
            Finance(manager).YouthFunding / YouthTeam.FundingStep * TalentScout.DiscountPerFundingStep);

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
