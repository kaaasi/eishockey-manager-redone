namespace EishockeyManager.Core;

public enum TalentResult
{
    /// <summary>„Ihre Talentsuche war ein Erfolg. Ein junger Spieler stößt zu Ihrem Kader.“</summary>
    Success,

    /// <summary>„Ihr Talentspäher gibt die Suche ohne Erfolg auf... Pech...“</summary>
    Failure,

    NotEnoughMoney,
    SquadFull,
}

/// <summary>
/// Talentsuche (Knopf „Talent“ im Transfer): „Möchten Sie, daß Ihr 'Spion' für Sie auf Talentsuche geht?
/// Preis: 90.000 DM“ (Video). [APPROX] Ergebnis sofort statt nach einigen Tagen, Erfolg zu 60 %;
/// das Talent kommt aus einem Verein ohne Liga (Oberliga-Reserve), ist 17–19 Jahre alt und hat Talent 6–9.
/// </summary>
public static class TalentScout
{
    public const long Cost = 90_000;
    public const double SuccessChance = 0.6;

    public static TalentResult Search(GameState game, Manager manager, Random random)
    {
        var club = game.Clubs[manager.ClubIndex];
        if (manager.Money < Cost)
            return TalentResult.NotEnoughMoney;
        if (club.Players.Count >= TransferMarket.MaxSquad)
            return TalentResult.SquadFull;
        game.Book(manager, FinanceCategory.Talentsuche, -Cost);

        var sources = game.Clubs.Where(c => c.League is null && !TransferMarket.IsNationalTeam(c) && c.Players.Count > 0).ToList();
        if (random.NextDouble() >= SuccessChance || sources.Count == 0)
            return TalentResult.Failure;

        var from = sources[random.Next(sources.Count)];
        var p = from.Players[random.Next(from.Players.Count)];
        from.Players.Remove(p);
        from.Lineup.Remove(p);
        p.Age = SquadSetup.Between(random, 17, 19);
        p.Talent = SquadSetup.Between(random, 6, 9);
        var b = SquadSetup.BaseValue(club.Strength);
        p.Condition = Player.Clamp(b - SquadSetup.Between(random, 2, 8));
        p.Technique = Player.Clamp(b - SquadSetup.Between(random, 2, 8));
        p.Form = SquadSetup.Between(random, 45, 60);
        p.Energy = 80;
        p.Trend = PlayerTrend.Rising;
        p.Number = Enumerable.Range(1, 99).First(n => club.Players.All(q => q.Number != n));
        p.ContractYears = SquadSetup.StartContractYears;
        p.MonthlySalary = Contracts.Demand(p);
        p.OnLoan = false;
        p.Insured = false;
        club.Players.Add(p);
        return TalentResult.Success;
    }
}
