using System.Text.Json.Nodes;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur <c>transfer-vertraege-komplett</c>, Teil 2: Versicherung (Prämie Wert(Gehalt) / 2, Abschluss +0x2C Bit 31, Umschalten
/// nur ohne Abschluss und ohne Verletzung, Leistung 0x07089; R8W3I-07) und Verhandlungssperre über den Saisonwechsel (R8W3I-09).
/// Vorher rot: Prämie 10 % des Gehalts, beliebig umschaltbar, keine Leistung; die Sperre fiel beim Saisonwechsel auf 0.
/// </summary>
public class TransferVertraegeKomplettTeil2Tests
{
    private static (GameState Game, Manager Manager, Club Club, Player Player) Setup(int seed = 3)
    {
        var game = TestGames.Create(seed);
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        foreach (var p in club.Players)
        {
            p.InsurancePremium = 0;
            p.InsuranceLocked = false;
            p.NegotiationLock = 0;
        }
        return (game, manager, club, club.Players.First(p => !p.OnLoan && p.InjuredGames == 0));
    }

    /// <summary>[ORIGINAL] 0x4AA9C: Prämie = Wert(Gehalt) / 2 (Drittel-DM → DM), ohne Zufall; 0x4AB73: Abschluss gesetzt.</summary>
    [Fact]
    public void Negotiate_InsuranceOn_PremiumIsHalfSalaryValue_AndLocked()
    {
        var (game, manager, club, p) = Setup();
        var value = game.PlayerValueOf(p, club, PlayerValueKind.Salary, new NoDraw(), manager.Level);
        var offer = Contracts.InsuranceOffer(game, manager, p);
        Assert.Equal(Math.Max(1, value / 6), offer);
        Assert.Equal(offer, Contracts.InsuranceOffer(game, manager, p)); // rein

        Assert.Equal(NegotiationResult.Accepted, Contracts.Negotiate(game, manager, p, 0, 0, insured: true, demandFilled: false));
        Assert.Equal(offer, p.InsurancePremium);
        Assert.True(p.Insured);
        Assert.True(p.InsuranceLocked);
        Assert.Equal(club.Players.Sum(x => (long)x.InsurancePremium), FinanceRules.MonthlyInsurance(club));
        Assert.Equal(offer, FinanceRules.MonthlyInsurance(club));
    }

    /// <summary>
    /// [ORIGINAL] 0x4AB88 setzt Bit 31 vor der Prüfung des Ergebnisses; 0x4AD54 stellt die Prämie bei einer gescheiterten
    /// Verhandlung zurück, das Bit bleibt. Danach lässt sich die Versicherung nicht mehr einschalten (0x4AA74).
    /// </summary>
    [Fact]
    public void Negotiate_Failed_RestoresPremium_ButKeepsLock()
    {
        var (game, manager, _, p) = Setup();
        Assert.Equal(NegotiationResult.TooLong, Contracts.Negotiate(game, manager, p, 6, 0, insured: true, demandFilled: false));
        Assert.Equal(0, p.InsurancePremium);
        Assert.True(p.InsuranceLocked);
        Assert.False(Contracts.CanChangeInsurance(p));

        p.NegotiationLock = 0;
        Assert.Equal(NegotiationResult.Accepted, Contracts.Negotiate(game, manager, p, 0, 0, insured: true, demandFilled: false));
        Assert.Equal(0, p.InsurancePremium); // Umschalten gesperrt: Stand wie beim Öffnen
    }

    /// <summary>[ORIGINAL] 0x4AA74–0x4AA90: abgeschlossene Versicherung lässt sich nicht kündigen; verletzt: nicht umschaltbar.</summary>
    [Fact]
    public void Insurance_LockedOrInjured_CannotChange()
    {
        var (game, manager, _, p) = Setup();
        p.InsurancePremium = 321;
        p.InsuranceLocked = true;
        Assert.Equal(NegotiationResult.Accepted, Contracts.Negotiate(game, manager, p, 0, 0, insured: false, demandFilled: false));
        Assert.Equal(321, p.InsurancePremium);

        p.InsuranceLocked = false;
        p.InjuredGames = 3;
        Assert.False(Contracts.CanChangeInsurance(p));
        Assert.Equal(NegotiationResult.Accepted, Contracts.Negotiate(game, manager, p, 0, 0, insured: false, demandFilled: false));
        Assert.Equal(321, p.InsurancePremium);

        p.InjuredGames = 0;
        Assert.Equal(NegotiationResult.Accepted, Contracts.Negotiate(game, manager, p, 0, 0, insured: false, demandFilled: false));
        Assert.Equal(0, p.InsurancePremium); // ohne Abschluss: ausschaltbar (0x4AAC5)
        Assert.False(p.InsuranceLocked);
    }

    /// <summary>[ORIGINAL] 0x7089: Leistung = Prämie · 4, Meldung DGROUP 0x134D–0x1359; Laufbahnende · 60 (0x277DE).</summary>
    [Fact]
    public void PayInsurance_FourOrSixtyTimesPremium()
    {
        var (game, manager, _, p) = Setup();
        Assert.Equal(0, game.PayInsurance(manager, p, careerEnd: false));

        p.InsurancePremium = 250;
        var money = manager.Money;
        var book = game.Finance(manager).Book;
        var before = book.Current(FinanceCategory.Versicherung);
        Assert.Equal(1_000, game.PayInsurance(manager, p, careerEnd: false));
        Assert.Equal(money + 1_000, manager.Money);
        Assert.Equal(before + 1_000, book.Current(FinanceCategory.Versicherung));
        Assert.Equal([p.Name + " ist verletzt.", "Ein Fall für die Versicherung: Sie zahlt Ihnen 1.000 DM"],
            GameState.InsuranceMessage(p, 1_000, careerEnd: false));

        Assert.Equal(15_000, game.PayInsurance(manager, p, careerEnd: true));
        Assert.Equal(p.Name + " ist invalide.", GameState.InsuranceMessage(p, 15_000, careerEnd: true)[0]);
    }

    /// <summary>
    /// R8W3I-09, [ORIGINAL] Saisonende 0x274E8–0x27539: die Sperre +0x18 bleibt über den Saisonwechsel (nur der Durchlauf ohne
    /// Halt setzt sie bei 0x27534 auf 0).
    /// </summary>
    [Fact]
    public void SeasonChange_KeepsNegotiationLock()
    {
        var (game, _, club, _) = Setup();
        LifecycleRegressionTests.FinishSeason(game);
        var p = club.Players.First(x => !x.OnLoan && x.ContractYears >= 2);
        p.NegotiationLock = 7;
        game.StartNextSeason();
        Assert.Equal(7, p.NegotiationLock);
    }

    /// <summary>Migration 17 → 18: „Insured“ → Prämie (bisher 10 % des Gehalts), nicht abgeschlossen.</summary>
    [Fact]
    public void Migration_FromSchema17()
    {
        var (game, _, club, p) = Setup();
        p.MonthlySalary = 4_567;
        p.InsurancePremium = 456;
        var json = JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        json["schema"] = 17;
        foreach (var player in json["spieler"]!.AsArray().OfType<JsonObject>())
        {
            var premium = player["InsurancePremium"]!.GetValue<int>();
            player.Remove("InsurancePremium");
            player.Remove("InsuranceLocked");
            player["Insured"] = premium > 0;
        }
        var result = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(json.ToJsonString()));
        Assert.True(result.Success, result.Error);
        Assert.Equal(SaveGame.Save(game), SaveGame.Save(result.Game!));
        Assert.NotNull(club);
    }

    /// <summary>
    /// Mehrsaisonprobe (4 Saisonwechsel, passiv und aktiv): aktiv schließt der Manager wöchentlich eine Versicherung ab und
    /// erhält monatlich eine Leistung. Prämien = Kosten, Sperren 0–18 auch über den Saisonwechsel, Spielstand bleibt gleich
    /// nach Speichern und Laden; passiv bleibt alles unversichert.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultiSeason_FourChanges_InsuranceAndLocks(bool active)
    {
        var (game, manager, club, _) = Setup(11);
        long paid = 0;
        var insuredDeals = 0;
        for (var change = 0; change < 4; change++)
        {
            for (var day = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; day++)
            {
                Assert.True(day < 1000, "Saisonende nicht erreicht");
                manager.Money = Math.Max(manager.Money, 5_000_000);
                if (active && day % 7 == 0
                    && club.Players.FirstOrDefault(p => !p.OnLoan && !p.Insured && Contracts.CanChangeInsurance(p)
                        && Contracts.Availability(game, p) is null) is { } candidate)
                {
                    var offer = Contracts.InsuranceOffer(game, manager, candidate);
                    if (Contracts.Negotiate(game, manager, candidate, 0, 0, insured: true, demandFilled: false)
                        == NegotiationResult.Accepted)
                    {
                        Assert.Equal(offer, candidate.InsurancePremium);
                        insuredDeals++;
                    }
                    Assert.True(candidate.InsuranceLocked);
                }
                if (active && day % 30 == 0 && club.Players.FirstOrDefault(p => p.Insured) is { } insured)
                    paid += game.PayInsurance(manager, insured, careerEnd: false);
                Assert.All(club.Players, p => Assert.InRange(p.NegotiationLock, 0, 18));
                game.Continue();
            }
            Assert.Equal(club.Players.Sum(p => (long)p.InsurancePremium), FinanceRules.MonthlyInsurance(club));
            game.StartNextSeason();
            var saved = SaveGame.Save(game);
            var loaded = SaveGame.Load(saved);
            Assert.True(loaded.Success, loaded.Error);
            Assert.Equal(saved, SaveGame.Save(loaded.Game!));
        }
        if (active)
        {
            Assert.True(insuredDeals > 0, "keine Versicherung abgeschlossen");
            Assert.True(paid > 0, "keine Leistung");
        }
        else
            Assert.All(club.Players, p => Assert.False(p.Insured));
    }

    private sealed class NoDraw : Random
    {
        public override int Next(int minValue, int maxValue) => throw new InvalidOperationException();
        public override int Next() => throw new InvalidOperationException();
        public override int Next(int maxValue) => throw new InvalidOperationException();
        public override double NextDouble() => throw new InvalidOperationException();
    }
}
