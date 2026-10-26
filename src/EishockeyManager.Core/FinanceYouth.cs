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
    /// Talente aus der Jugend für den Kader folgen, sobald das Paket „mannschaft“ Spieler anlegen kann.
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
    /// <summary>
    /// Höchstzahl Spieler im Kader für die Talentsuche. [APPROX] Nicht belegt: Die Datenbank hat schon bis zu
    /// 24 Spieler je Verein, „Schon 22 Mann im Team !“ (em.exe) gilt daher wohl nur für den Transfermarkt
    /// (Paket „mannschaft“). Bis das geklärt ist, eine großzügige Grenze.
    /// </summary>
    public const int MaxSquad = 30;

    /// <summary>[APPROX] Grundpreis und Dauer der Talentsuche; im Video wird der Spion nicht losgeschickt.</summary>
    public const long TalentSearchBasePrice = 30_000;
    public const int TalentSearchWeeks = 4;

    /// <summary>
    /// „Rabatt:“ auf die Talentsuche in Prozent. [APPROX] Angenommen: je 1.000 DM Jugendförderung im Monat
    /// gibt es 2 % Rabatt, höchstens 50 % (gute Jugendarbeit macht die Suche billiger).
    /// </summary>
    public int TalentSearchDiscount(Manager manager) =>
        (int)Math.Min(50, Finance(manager).YouthFunding / YouthTeam.FundingStep * 2);

    /// <summary>„Preis:“ der Talentsuche nach Rabatt.</summary>
    public long TalentSearchPrice(Manager manager) =>
        TalentSearchBasePrice * (100 - TalentSearchDiscount(manager)) / 100;

    /// <summary>
    /// „Möchten Sie, daß Ihr 'Spion' für Sie auf Talentsuche geht?“ – bezahlt sofort (Finanzbuch „Talentsuche“).
    /// Liefert false, wenn schon gesucht wird oder der Kader voll ist.
    /// </summary>
    public bool StartTalentSearch(Manager manager)
    {
        var finance = Finance(manager);
        if (finance.TalentSearch is not null || Clubs[manager.ClubIndex].Players.Count >= MaxSquad)
            return false;
        Book(manager, FinanceCategory.Talentsuche, -TalentSearchPrice(manager));
        finance.TalentSearch = new TalentSearch { WeeksLeft = TalentSearchWeeks };
        return true;
    }

    /// <summary>
    /// Wöchentlich: Ergebnis der Talentsuche mit den Meldungen aus em.exe.
    /// [APPROX] Erfolgschance 40 %, mit der besten Jugendmannschaft bis zu 85 % (1. Platz 1. Liga).
    /// </summary>
    private void AdvanceTalentSearch(Manager manager)
    {
        var finance = Finance(manager);
        if (finance.TalentSearch is not { } search || --search.WeeksLeft > 0)
            return;
        finance.TalentSearch = null;
        var best = finance.Youth.Min(t => t.Position);
        var chance = 0.4 + 0.45 * (1 - best / (double)(YouthTeam.Leagues * YouthTeam.TeamsPerLeague - 1));
        var club = Clubs[manager.ClubIndex];
        if (club.Players.Count < MaxSquad && FinanceRandom.NextDouble() < chance)
        {
            club.Players.Add(CreateTalent());
            Notify(manager, "Ihre Talentsuche war ein Erfolg.", "Ein junger Spieler stößt zu Ihrem Kader.");
        }
        else
            Notify(manager, "Ihr Talentspäher gibt die", "Suche ohne Erfolg auf... Pech...");
    }

    /// <summary>
    /// Junger Spieler aus der Talentsuche. [APPROX] Name aus Vor- und Nachnamen zweier zufälliger Spieler der
    /// Datenbank zusammengesetzt; Alter und Stärken legt das Paket „mannschaft“ fest.
    /// </summary>
    private Player CreateTalent()
    {
        var all = Clubs.SelectMany(c => c.Players).ToList();
        var role = (Formats.PlayerRole)FinanceRandom.Next(3);
        if (all.Count == 0)
            return new Player { Name = "Talent", Role = role };
        static string[] Parts(string name) => name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var first = Parts(all[FinanceRandom.Next(all.Count)].Name);
        var last = Parts(all[FinanceRandom.Next(all.Count)].Name);
        var name = first.Length > 1 && last.Length > 0 ? $"{first[0]} {last[^1]}" : all[FinanceRandom.Next(all.Count)].Name;
        return new Player { Name = name, Role = role };
    }
}
