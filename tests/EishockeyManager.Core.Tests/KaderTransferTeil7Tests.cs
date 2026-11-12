using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur <c>kader-transfer</c>, Teil 7 (Inventur A136, A229–A232, A238, A239; P1 Nr. 9): Ankündigungen der Kaderspieler nach dem
/// Tagesablauf 0x07169 (<c>docs/analyse/re-nachtrag.md</c>, Abschnitt 1, Umsetzung U1–U3). Vorher rot: tägliche Chance 1/60 nur im
/// letzten Vertragsjahr mit Frist 30. März, Ruhestand 33–37 als Teil der Ankündigung, 10 % Absagen, Laufzeiten 80/13/7 %.
/// </summary>
public class KaderTransferTeil7Tests
{
    /// <summary>random(a, b) = a.</summary>
    private sealed class Lowest : Random
    {
        public override int Next(int minValue, int maxValue) => minValue;
        public override int Next(int maxValue) => 0;
        public override int Next() => 0;
        public override double NextDouble() => 0;
    }

    private static Player Skater(int age, int condition = 50, int technique = 50) =>
        new() { Name = "X", Role = PlayerRole.Forward, Age = age, Condition = condition, Technique = technique, Form = 50 };

    /// <summary>
    /// [ORIGINAL] Datei 0x0754D–0x0760F: random(350, 450) &lt; Alter · c / 10, c = 100 (Torhüter 85) − (Technik − Kondition) / 2.
    /// Mit dem kleinsten Wurf 350: Feldspieler ab 36, Torhüter ab 42, Technik − Kondition = 20 ab 39.
    /// </summary>
    [Fact]
    public void RetirementRoll_AgeLimits()
    {
        Assert.False(SquadRules.RetirementRoll(Skater(35), new Lowest()));
        Assert.True(SquadRules.RetirementRoll(Skater(36), new Lowest()));
        var goalie = new Player { Name = "T", Role = PlayerRole.Goalkeeper, Age = 41, Condition = 50, Technique = 50, Form = 50 };
        Assert.False(SquadRules.RetirementRoll(goalie, new Lowest()));
        goalie.Age = 42;
        Assert.True(SquadRules.RetirementRoll(goalie, new Lowest()));
        Assert.False(SquadRules.RetirementRoll(Skater(38, 40, 60), new Lowest()));
        Assert.True(SquadRules.RetirementRoll(Skater(39, 40, 60), new Lowest()));
        // Kondition über Technik verschiebt nichts.
        Assert.True(SquadRules.RetirementRoll(Skater(36, 60, 40), new Lowest()));
    }

    /// <summary>[ORIGINAL] Datei 0x076FA–0x07951: p = (60 · s + 40 · a) / 100 − p · e / 10; Beispiel s = 50, Alter 25, 0 Spiele: 70.</summary>
    [Fact]
    public void OfferLimit_FollowsOriginal()
    {
        Assert.Equal(70, SquadRules.OfferLimit(Skater(25)));
        Assert.Equal(30, SquadRules.OfferLimit(Skater(17))); // a = 0
        Assert.Equal(30, SquadRules.OfferLimit(Skater(33)));
        Assert.Equal(60, SquadRules.OfferLimit(Skater(27))); // a = 100 − 100 · 2 / 8 = 75 → (3000 + 3000) / 100
        var regular = Skater(25);
        regular.Games = 72; // e = 2
        Assert.Equal(70 - 70 * 2 / 10, SquadRules.OfferLimit(regular));
        regular.Games = 400; // e höchstens 5
        Assert.Equal(35, SquadRules.OfferLimit(regular));
    }

    /// <summary>[ORIGINAL] r = random(0, 100): über 90 → 4, über 70 → 3, sonst 2 (71 / 20 / 10 von 101).</summary>
    [Fact]
    public void OfferedYears_Distribution()
    {
        var counts = Enumerable.Range(0, 101).GroupBy(SquadRules.OfferedYears).ToDictionary(g => g.Key, g => g.Count());
        Assert.Equal(71, counts[2]);
        Assert.Equal(20, counts[3]);
        Assert.Equal(10, counts[4]);
    }

    private static (GameState Game, Club Club) Game()
    {
        var game = TestGames.Create();
        return (game, game.Clubs[game.ActiveManager.ClubIndex]);
    }

    /// <summary>Zeitleiste bis mindestens Tag <paramref name="day"/> (<see cref="GameState.Continue"/>).</summary>
    private static void Days(GameState game, int day)
    {
        while (game.CurrentDay < day && game.Phase == SeasonPhase.Running)
        {
            foreach (var m in game.Managers)
                m.Money = Math.Max(m.Money, 5_000_000);
            game.Continue();
            MessageLog.For(game).Collect(game);
        }
    }

    private static int Offers(GameState game, Player p) => MessageLog.For(game).All.Where(m => m.Lines.Length > 0)
        .Count(m => m.Season == game.Season && string.Join(" ", m.Lines).StartsWith($"{p.Name} bietet Ihnen an, von 1 auf", StringComparison.Ordinal));

    /// <summary>
    /// U1: Der Ruhestand meldet sich unabhängig von der Laufzeit, auch beim Leihspieler – ein 40-jähriger Feldspieler binnen 30 Tagen
    /// (täglich 50/101), ein 35-jähriger nie. Meldung Art 4 „… kündigt an, daß er seinen Vertrag nicht mehr verlängern wird.“
    /// </summary>
    [Fact]
    public void Retirement_AnnouncedRegardlessOfContract()
    {
        var (game, club) = Game();
        var old = club.Players.First(p => p.Role == PlayerRole.Forward);
        var young = club.Players.Last(p => p.Role == PlayerRole.Forward);
        (old.Age, old.ContractYears, old.Condition, old.Technique) = (40, 3, 50, 50);
        (young.Age, young.ContractYears) = (35, 1);
        Days(game, 30);
        Assert.InRange(game.CurrentDay, 30, 40);
        Assert.Equal(ContractNotice.Retiring, old.Notice);
        Assert.NotEqual(ContractNotice.Retiring, young.Notice);
        Assert.Contains(MessageLog.For(game).All, m =>
            string.Join(" ", m.Lines) == $"{old.Name} kündigt an, daß er seinen Vertrag nicht mehr verlängern wird.");
        Assert.Equal(NegotiationResult.Retiring, Contracts.Negotiate(game, game.ActiveManager, old, 3, 99_999, false, false));
    }

    /// <summary>
    /// U1: Das Ruhestand-Bit bleibt über den Saisonwechsel (0x26F20 lässt +0x18 stehen); der Spieler hört erst bei Vertragsende auf
    /// (Datei 0x2787A, Meldung Art 3).
    /// </summary>
    [Fact]
    public void Retirement_KeptUntilContractEnds()
    {
        var (game, club) = Game();
        var p = club.Players.First(x => x.Role == PlayerRole.Defender);
        p.ContractYears = 2;
        p.Notice = ContractNotice.Retiring;
        foreach (var other in club.Players.Where(x => x != p))
            other.ContractYears = 3;
        SeasonHelper.PlaySeason(game);
        game.StartNextSeason();
        Assert.Contains(p, club.Players);
        Assert.Equal((1, ContractNotice.Retiring), (p.ContractYears, p.Notice));
        SeasonHelper.PlaySeason(game);
        game.StartNextSeason();
        Assert.DoesNotContain(p, club.Players);
        Assert.Contains(p, game.RetiredPlayers);
    }

    /// <summary>
    /// U2 (RN-2): Ein Verlängerungsangebot gibt es nur bei Restlaufzeit 1 und ohne Leihe, aber ohne Frist; Restlaufzeit 2 nie.
    /// U3: keine Absage ohne Ruhestand. Über eine Saison mit allen Spielern bei Laufzeit 1 meldet sich ein Teil, nicht jeder.
    /// </summary>
    [Fact]
    public void Offer_OnlyLastYear_NoDeadline_NoRefusal()
    {
        // Runde 17, Spur wettbewerbe-rest Teil 3 (A113/A142, R15-E2 = C): Saat 1 statt 3 – mit dem Spielplan über die Satzfolge (Datei 0x4E7C7) melden sich bei Saat 3 nur
        // zwei Spieler (Grenze 3); Saaten 1–40: 2–4. Rückschaltprobe: alter Code mit Saat 1 ebenfalls grün – die Probe hängt an der
        // Saat, die Grenzen bleiben.
        // Spur kader-rest (A130) brauchte allein Saat 2 statt 3 (Saat 3: 2 von 22); nach dem Rebase auf main 1d83cc0 hält Saat 1 auch
        // mit kader-rest (keine Änderung, nur Kommentar zusammengeführt).
        var game = TestGames.Create(1);
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        var twoYears = club.Players.Where(p => p.Role == PlayerRole.Forward).Take(3).ToList();
        foreach (var p in club.Players)
        {
            p.ContractYears = twoYears.Contains(p) ? 2 : 1;
            p.Age = Math.Min(p.Age, 30); // kein Ruhestand
        }
        var expiring = club.Players.Where(p => !twoYears.Contains(p)).ToList();
        SeasonHelper.PlaySeason(game);
        Assert.All(expiring.Concat(twoYears), p => Assert.NotEqual(ContractNotice.Refused, p.Notice));
        Assert.All(twoYears, p => Assert.Equal(0, Offers(game, p)));
        var offered = expiring.Where(p => Offers(game, p) > 0).ToList();
        // Runde 12, U4: Ein Angebot verfällt (random(0, 5) = 0 je Tag) und kann neu kommen.
        Assert.All(offered, p => Assert.InRange(Offers(game, p), 1, 10));
        Assert.All(offered.Where(p => p.Notice == ContractNotice.Offered), p => Assert.InRange(p.NoticeYears, 2, 4)); // verfallene: 0
        // Chance je Tag 1/(4 · (p + 1)), p um 40 … 70: ein Teil meldet sich, nicht jeder (keine Frist).
        Assert.InRange(offered.Count, 3, expiring.Count - 3);
        // Wer sich nicht gemeldet hat, geht in die Verhandlung am Saisonende (0x27D05).
        game.StartNextSeasonWithRenewals();
        Assert.All(expiring.Except(offered).Where(club.Players.Contains), p => Assert.True(GameState.IsRenewalPending(p), p.Name));
    }

    /// <summary>
    /// RN-2 (Review #446, KT-03 aus Review #463): Ohne Restlaufzeit 1 nie ein Angebot (Datei 0x07865
    /// <c>cmp byte [es:bx+0xb],0x1</c>, geprüft nach den beiden Würfen). Alle Spieler des Kaders mit Laufzeit 2 über eine ganze
    /// Saison: bei rund 25 Spielern und Tageschance um 1/200 kämen ohne diese Bedingung mehrere Angebote
    /// zusammen (gemessen bei der Mutation: 8 von 25 Spielern, je eines) – die Probe ist also auch ohne festen Zufall scharf (Mutation „Laufzeit nicht prüfen“: rot).
    /// </summary>
    [Fact]
    public void Offer_NeverWithTwoYearsLeft_WholeSquad()
    {
        var (game, club) = Game();
        foreach (var p in club.Players)
        {
            p.ContractYears = 2;
            p.Age = Math.Min(p.Age, 30); // kein Ruhestand
        }
        SeasonHelper.PlaySeason(game);
        Assert.All(club.Players, p => Assert.Equal(0, Offers(game, p)));
        Assert.All(club.Players, p => Assert.NotEqual(ContractNotice.Offered, p.Notice));
    }

    /// <summary>U2: Leihspieler bekommen kein Verlängerungsangebot (Datei 0x076FA: +0x0C ≠ 0).</summary>
    [Fact]
    public void Offer_NeverForLoanedPlayer()
    {
        var (game, club) = Game();
        var other = game.Clubs.First(c => c.Index != club.Index && c.League is not null);
        var loaned = other.Players.First(p => p.Role == PlayerRole.Forward);
        other.Players.Remove(loaned);
        PlayerFactory.Join(club, loaned, 1, loan: true, new PlayerLoan(other, 2, loaned.MonthlySalary, false));
        loaned.Age = 25;
        SeasonHelper.PlaySeason(game);
        Assert.Equal(0, Offers(game, loaned));
        Assert.NotEqual(ContractNotice.Offered, loaned.Notice);
    }
}

/// <summary>Gemeinsamer Saisonablauf der Proben.</summary>
internal static class SeasonHelper
{
    internal static void PlaySeason(GameState game)
    {
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 1000, "Saisonende nicht erreicht");
            foreach (var m in game.Managers)
                m.Money = Math.Max(m.Money, 5_000_000);
            game.Continue();
            MessageLog.For(game).Collect(game);
        }
    }
}
