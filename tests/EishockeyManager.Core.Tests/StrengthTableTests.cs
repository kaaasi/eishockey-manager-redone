using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>Paket „wettbewerbe“: Stärken-Tabelle (em.exe 0x51206).</summary>
public class StrengthTableTests
{
    private static GameState Create() => TestGames.Create(seed: 4);

    private static Club Nation(GameState game, int index, int form)
    {
        var club = new Club { Index = index, Name = $"LAND {index}", Strength = 80 };
        club.Players.Add(new Player { Name = "A", Role = PlayerRole.Defender, Form = form });
        club.Players.Add(new Player { Name = "B", Role = PlayerRole.Forward, Form = form });
        return club;
    }

    /// <summary>[ORIGINAL] Gesamt = Mittel aus Abwehr und Sturm, auf 99 begrenzt, Mittel aus ungerundeten Summen (Video 13:18).</summary>
    [Fact]
    public void NationalTeams_Gesamt_AsInVideo()
    {
        var game = Create();
        var rows = StrengthTable.Compute(game, [Nation(game, 52, 51), Nation(game, 54, 53), Nation(game, 56, 55)], StrengthView.Gesamt);
        Assert.Equal(
            [("LAND 52", 99, 99, 51, 85), ("LAND 54", 98, 93, 53, 81), ("LAND 56", 88, 91, 55, 78)],
            rows.Select(r => (r.Club.Name, r.Condition, r.Technique, r.Form, r.Average)));
    }

    /// <summary>[ORIGINAL] „Abwehr“/„Sturm“ zeigen nur die Werte der Abwehr bzw. des Sturms (Teiler 1).</summary>
    [Fact]
    public void NationalTeams_AbwehrUndSturm()
    {
        var game = Create();
        var russia = Nation(game, 52, 50);
        var (cd, td, ca, ta) = NationalTeamRatings.Of(52);
        var defense = StrengthTable.Compute(game, [russia], StrengthView.Abwehr).Single();
        var attack = StrengthTable.Compute(game, [russia], StrengthView.Sturm).Single();
        Assert.Equal((Math.Min(99, cd), Math.Min(99, td), 50, (cd + td + 50) / 3), (defense.Condition, defense.Technique, defense.Form, defense.Average));
        Assert.Equal((Math.Min(99, ca), Math.Min(99, ta), 50, (ca + ta + 50) / 3), (attack.Condition, attack.Technique, attack.Form, attack.Average));
    }

    [Fact]
    public void LeagueClubs_SortedBySum_TwelveRows_ValuesInRange()
    {
        var game = Create();
        foreach (var league in Enum.GetValues<League>())
            foreach (var view in Enum.GetValues<StrengthView>())
            {
                var rows = StrengthTable.Compute(game, game.LeagueClubs(league), view);
                Assert.Equal(12, rows.Count);
                Assert.Equal(rows.Select(r => r.Sum).OrderDescending(), rows.Select(r => r.Sum));
                Assert.All(rows, r =>
                {
                    Assert.InRange(r.Condition, 0, 99);
                    Assert.InRange(r.Technique, 0, 99);
                    Assert.InRange(r.Trend, 47, 53);
                });
            }
    }

    /// <summary>Die Tabelle liest nur: Spielstand vorher und nachher gleich (auch der Zufall).</summary>
    [Fact]
    public void Compute_DoesNotChangeState()
    {
        var game = Create();
        var random = game.Random.Next();
        var again = TestGames.Create(seed: 4);
        foreach (var league in Enum.GetValues<League>())
            StrengthTable.Compute(game, game.LeagueClubs(league), StrengthView.Gesamt);
        _ = again.Random.Next();
        Assert.Equal(again.Random.Next(), game.Random.Next());
    }
}
