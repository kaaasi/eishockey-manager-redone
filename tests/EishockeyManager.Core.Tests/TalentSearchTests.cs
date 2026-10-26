using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Regressionstest (Runde 2, Spur wirtschaft): Jugend-„Spion“ und Transfer-Knopf „Talent“ sind dieselbe Talentsuche –
/// gleicher Preis, gleiche Grenzen, gleiche Ergebniszustände. Vor der Zusammenführung kostete der Transfer-Knopf
/// 90.000 DM, der „Spion“ 30.000 DM, und beide Suchen liefen unabhängig voneinander.
/// </summary>
public class TalentSearchTests
{
    [Fact]
    public void TransferButton_And_YouthSpy_AreTheSameSearch()
    {
        var game = TestGames.Create();
        var me = game.ActiveManager;
        me.Money = 1_000_000;
        var price = game.TalentSearchPrice(me);

        // Transfer-Knopf: kostet denselben Preis wie der Jugend-„Spion“
        Assert.Equal(TalentSearchStart.Started, TalentScout.Search(game, me));
        Assert.Equal(1_000_000 - price, me.Money);

        // … und ist dieselbe laufende Suche: der „Spion“ kann nicht zusätzlich losgeschickt werden
        Assert.Equal(TalentSearchStart.AlreadyRunning, game.StartTalentSearch(me));
        Assert.Equal(TalentSearchStart.AlreadyRunning, TalentScout.Search(game, me));
        Assert.Equal(1_000_000 - price, me.Money);
        Assert.Equal(-price, game.Finance(me).Book.Current(FinanceCategory.Talentsuche));
    }

    [Fact]
    public void Price_IsVideoPriceMinusDiscount()
    {
        var game = TestGames.Create();
        var me = game.ActiveManager;
        Assert.Equal(TalentScout.BasePrice, game.TalentSearchPrice(me)); // Video: „Preis: 90.000 DM“
        game.SetYouthFunding(me, YouthTeamKind.Schueler, 10_000);
        game.SetYouthFunding(me, YouthTeamKind.Jugend, 5_000);
        Assert.Equal(30, game.TalentSearchDiscount(me));
        Assert.Equal(63_000, game.TalentSearchPrice(me));
        game.SetYouthFunding(me, YouthTeamKind.Junioren, 10_000);
        Assert.Equal(TalentScout.MaxDiscount, game.TalentSearchDiscount(me));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Limits_AreTheSameForBothEntries(bool viaTransferButton)
    {
        TalentSearchStart Start(GameState g) =>
            viaTransferButton ? TalentScout.Search(g, g.ActiveManager) : g.StartTalentSearch(g.ActiveManager);

        var game = TestGames.Create();
        var me = game.ActiveManager;
        me.Money = game.TalentSearchPrice(me) - 1;
        Assert.Equal(TalentSearchStart.NotEnoughMoney, Start(game));
        Assert.Null(game.Finance(me).TalentSearch);

        me.Money = 1_000_000;
        var club = game.Clubs[me.ClubIndex];
        while (club.Players.Count < TalentScout.MaxSquad)
            club.Players.Add(new Player { Name = "X", Role = PlayerRole.Forward });
        Assert.Equal(TalentSearchStart.SquadFull, Start(game));
        Assert.Equal(1_000_000, me.Money);
    }

    [Fact]
    public void TransferButton_ResultArrivesAsMessage_WithCompleteNewPlayer()
    {
        var game = TestGames.Create();
        var me = game.ActiveManager;
        var club = game.Clubs[me.ClubIndex];
        foreach (var team in game.Finance(me).Youth)
            team.MoveTo(0);
        var before = club.Players.ToList();
        var others = game.Clubs.Where(c => c != club).Sum(c => c.Players.Count);

        Assert.Equal(TalentSearchStart.Started, TalentScout.Search(game, me));
        Assert.Equal(before.Count, club.Players.Count); // kein Sofortergebnis
        for (var i = 0; i < 400 && game.Finance(me).TalentSearch is not null; i++)
            game.Continue();

        var report = Assert.Single(game.Inbox(me), m => m.Lines[0].StartsWith("Ihr"));
        if (report.Lines[0] == "Ihre Talentsuche war ein Erfolg.")
        {
            var talent = Assert.Single(club.Players.Except(before));
            Assert.Empty(PlayerFactory.Validate(club, talent));
            Assert.InRange(talent.Age, PlayerFactory.TalentMinAge, PlayerFactory.TalentMaxAge);
        }
        else
            Assert.Equal(before.Count, club.Players.Count);
        Assert.Equal(others, game.Clubs.Where(c => c != club).Sum(c => c.Players.Count)); // kein anderer Verein verliert Spieler
    }
}
