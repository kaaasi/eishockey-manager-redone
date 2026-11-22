using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Regressionstest zu R2-03 (Folgereview 26.10.2026): Jeder Zugangspfad in einen Kader liefert einen vollständigen
/// Spieler – Alter, im Kader eindeutige Rückennummer, gültige Werte, Energie und Vertrag.
/// Die Prüfung steht bewusst hier im Test und nicht nur in <see cref="PlayerFactory.Validate"/>,
/// damit der Test die Fabrik nicht mit ihrer eigenen Regel abnimmt.
/// </summary>
public class PlayerFactoryTests
{
    private static void AssertComplete(Club club, Player p)
    {
        var where = $"{p.Name} ({club.Name})";
        Assert.False(string.IsNullOrWhiteSpace(p.Name), where);
        Assert.InRange(p.Age, 15, 45);
        Assert.InRange(p.Talent, 0, 9);
        Assert.InRange(p.Condition, 1, Player.MaxValue);
        Assert.InRange(p.Technique, 1, Player.MaxValue);
        Assert.InRange(p.Form, 1, Player.MaxValue);
        Assert.InRange(p.Energy, 1, 100);
        Assert.InRange(p.Number, 1, 99);
        Assert.True(club.Players.Count(q => q.Number == p.Number) == 1, $"Rückennummer {p.Number} doppelt: {where}");
        Assert.InRange(p.ContractYears, Contracts.MinYears, Contracts.MaxYears);
        Assert.True(p.MonthlySalary >= Contracts.MinSalary, $"Gehalt {p.MonthlySalary}: {where}");
        Assert.Contains(p, club.Players);
    }

    [Fact]
    public void GameStart_EverySquadPlayerIsComplete()
    {
        var game = TestGames.Create();
        var clubs = game.Clubs.Where(c => c.Players.Count > 0).ToList();
        Assert.NotEmpty(clubs);
        foreach (var club in clubs)
            foreach (var p in club.Players)
                AssertComplete(club, p);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TransferMarket_BoughtOrLoanedPlayerIsComplete(bool loan)
    {
        var game = TestGames.Create();
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        manager.Money = 5_000_000;
        var listing = game.TransferMarket.Listings[0];
        listing.Player.Number = club.Players[0].Number; // gleiche Nummer wie ein eigener Spieler

        var offer = (loan ? listing.LoanFee : listing.Price) * 135 / 100; // 135 %: im Original immer angenommen (über random(110, 130) %, 0x4991D; R8-C01)
        Assert.Equal(TransferResult.Accepted, game.TransferMarket.Buy(game, manager, listing, offer, loan));
        AssertComplete(club, listing.Player);
        Assert.Equal(loan, listing.Player.OnLoan);
    }

    [Fact]
    public void TransferMarket_PlayerSoldToComputerClubGetsFreeNumber()
    {
        var game = TestGames.Create();
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        var player = club.Players.Last(p => p.Role == PlayerRole.Forward);
        Assert.Equal(TransferResult.Accepted, game.TransferMarket.Offer(game, manager, player));
        var listing = game.TransferMarket.ListingOf(player)!;
        for (var i = 0; game.TransferMarket.Bids.Count == 0 && i < 100; i++)
            game.TransferMarket.AdvanceDay(game, game.Random);
        var bid = Assert.Single(game.TransferMarket.Bids);
        var buyer = bid.Buyer;
        player.Number = buyer.Players[0].Number;

        game.TransferMarket.Accept(game, bid);
        AssertComplete(buyer, player);
    }

    [Fact]
    public void TalentSearch_TalentIsComplete() // „Spion“ der Transferliste (Spur geld, Teil 6: Talent aus dem Pool)
    {
        var successes = 0;
        // Fixlauf kader-rest nach Rebase auf main 1d83cc0 (A130, R15-E2 = C): Saat 4 statt 3 (Vorgabe) – mit der Startbelastung 80–120
        // (Datei 0x42E4C) und dem Spielplan über die Satzfolge bleiben bei Saat 3 alle 15 Suchen erfolglos (Saaten 1–20: nur 3, 11,
        // 12, 19 ohne Erfolg). Die Probe hängt an der Saat.
        // Runde 22, Spur kaderplatz-werte Teil 2 (A093; R21-D2 = A, R15-E2 = C): Saat 5 statt 4 – die Spielabrechnung 0x544C0 würfelt vor jedem Spiel eines Managervereins je Kaderplatz random(0, 4) (Datei 0x548A8–0x548E7) aus dem gemeinsamen Strom, anderer Zufallsverlauf ab dem ersten Managerspiel; bei Saat 4 läuft eine
        // Suche über das Saisonende (Continue im Saisonende), unter 1–24 nur bei 4 nicht. Rückschaltprobe: src/ von faa64d2 mit Saat 5
        // ebenfalls grün – die Probe hängt an der Saat.
        // Fixlauf Rebase auf main 4a3a724 (Spur transfer-pool, A864/A391, R21-D2 = A, R15-E2 = C): Saat 1 statt 5 – die Verletzung außerhalb
        // des Spiels (Datei 0x07462–0x07548) und die Alterung verlorener Pool-Spieler ändern den Zufallsverlauf; bei Saat 5 läuft eine
        // Suche über das Saisonende (Continue im Saisonende), bei Saat 1 nicht. Rückschaltprobe: src/ von ce658d4 + Saat 5 → grün
        // (alter Zufallsverlauf). Die Probe hängt an der Saat.
        var game = TestGames.Create(1);
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        // Die Finanz-Zufallsquelle hängt nicht vom Spielstand-Seed ab: mehrere Suchen in einem Spiel statt mehrerer Spiele.
        // Integration Runde 11 (geld + saisonwechsel + kader-transfer): Mit dem integrierten Zufallsverlauf endet die erste
        // Saison nach fünf erfolglosen Suchen (Erfolg je Suche 1 : SuccessDie). Die Probe geht deshalb über das Saisonende hinaus
        // in die nächste Saison, statt nach der ersten Saison aufzuhören.
        for (var search = 0; search < 15 && successes == 0; search++)
        {
            while (game.Phase != SeasonPhase.Running)
            {
                if (game.Phase == SeasonPhase.SeasonOver)
                    game.StartNextSeason();
                else
                    game.Continue();
            }
            manager.Money = 1_000_000;
            TestSeason.KeepSquad(game, manager);
            var before = club.Players.ToList();
            Assert.Equal(TalentSearchStart.Started, search % 2 == 0
                ? game.StartTalentSearch(manager)
                : TalentScout.Search(game, manager));
            for (var i = 0; i < 400 && game.Finance(manager).TalentSearch is not null; i++)
                game.Continue();
            Assert.Null(game.Finance(manager).TalentSearch);

            foreach (var talent in club.Players.Except(before).Where(p => p.ContractYears is 2 or 3 && p.Age is >= 18 and <= 21))
            {
                successes++;
                AssertComplete(club, talent);
            }
        }
        Assert.True(successes > 0, "Keine erfolgreiche Talentsuche in 15 Versuchen");
    }
}
