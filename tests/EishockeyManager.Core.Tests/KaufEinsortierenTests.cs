using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Fix R13GN-01 (#622, Review <c>docs/reviews/2026-11-05-runde-13-gesamt-nachpruefung.md</c>): ein neuer Kaderplatz wird nach
/// Spielerart einsortiert, nicht hinten angehängt. [ORIGINAL] em.exe Datei 0x42D55–0x42DA1: ab Platz 0 der erste leere Platz
/// oder der erste, dessen Art (Datei 0x04180: 0 Torhüter, 1 Verteidiger, 2 Stürmer) größer als die des Neuen ist; ab dort
/// rückt die Folge per memmove nach hinten (Datei 0x42DC3 ff.). Vorher: immer hinten an (Platz 25, Nr. 26 bei 25 Spielern).
/// </summary>
public class KaufEinsortierenTests
{
    private static int s_nextId = 100_000;

    private static Player NewPlayer(PlayerRole role, int value, string name)
    {
        var p = new Player { Name = name, Role = role, Condition = value, Technique = value, Form = value, Age = 25, ContractYears = 2 };
        typeof(Player).GetProperty(nameof(Player.Id))!.SetValue(p, Interlocked.Increment(ref s_nextId));
        return p;
    }

    /// <summary>Regel 0x42D55: davor nur Spieler gleicher oder kleinerer Art, dahinter (falls vorhanden) eine größere Art.</summary>
    private static void AssertSortedIn(Club club, Player p)
    {
        var i = club.Players.IndexOf(p);
        Assert.True(i >= 0, "nicht im Kader");
        Assert.All(club.Players.Take(i), q => Assert.True(q.Role <= p.Role, $"{q.Name} ({q.Role}) vor {p.Name} ({p.Role})"));
        if (i + 1 < club.Players.Count)
            Assert.True(club.Players[i + 1].Role > p.Role, $"hinter {p.Name} ({p.Role}) steht {club.Players[i + 1].Role}");
    }

    private static void AssertNumbersAreSlots(Club club) =>
        Assert.Equal(Enumerable.Range(1, club.Players.Count), club.Players.Select(p => p.Number));

    private static (GameState Game, Club Own, Club Other) Game()
    {
        var game = TestGames.Create(3);
        var own = game.Clubs[game.Managers[0].ClubIndex];
        var other = game.Clubs.First(c => c != own && c.League is not null && c.Players.Count > 0);
        return (game, own, other);
    }

    private static Player TakeFrom(Club club, PlayerRole role)
    {
        var p = club.Players.Last(q => q.Role == role);
        club.RemovePlayer(p);
        return p;
    }

    [Fact]
    public void Join_Goalkeeper_LandsBehindLastGoalkeeper()
    {
        var (_, own, other) = Game();
        var lastGoalie = own.Players.FindLastIndex(p => p.Role == PlayerRole.Goalkeeper);
        var count = own.Players.Count;
        var keeper = TakeFrom(other, PlayerRole.Goalkeeper);

        PlayerFactory.Join(own, keeper, 2, loan: false);

        Assert.Equal(2, lastGoalie + 1); // Aufbau TestGames.Create(3): zwei Torhüter vorn (Review R13GN-01)
        Assert.Equal(lastGoalie + 1, own.Players.IndexOf(keeper)); // vorher: Platz 25 (hinten an)
        Assert.Equal(lastGoalie + 2, keeper.Number);
        Assert.Equal(count + 1, own.Players.Count);
        AssertSortedIn(own, keeper);
        AssertNumbersAreSlots(own); // alle Folgespieler rücken um eine Nr. auf
    }

    [Fact]
    public void Join_Defender_LandsBehindLastDefender()
    {
        var (_, own, other) = Game();
        var lastDefender = own.Players.FindLastIndex(p => p.Role == PlayerRole.Defender);
        var firstForward = own.Players.First(p => p.Role == PlayerRole.Forward);
        var forwardNumber = firstForward.Number;
        var defender = TakeFrom(other, PlayerRole.Defender);

        PlayerFactory.Join(own, defender, 2, loan: false);

        Assert.Equal(lastDefender + 1, own.Players.IndexOf(defender));
        Assert.Equal(forwardNumber + 1, firstForward.Number);
        AssertSortedIn(own, defender);
        AssertNumbersAreSlots(own);
    }

    [Fact]
    public void Join_Forward_LandsLast()
    {
        var (_, own, other) = Game();
        var before = own.Players.ToArray();
        var forward = TakeFrom(other, PlayerRole.Forward);

        PlayerFactory.Join(own, forward, 2, loan: false);

        Assert.Same(forward, own.Players[^1]);
        Assert.Equal(before, own.Players.Take(before.Length));
        AssertNumbersAreSlots(own);
    }

    /// <summary>
    /// Nicht nach Art geordneter Kader (z. B. nach früheren Käufen in alten Spielständen): eingefügt wird vor dem ersten Platz
    /// größerer Art, nicht hinter dem letzten Spieler gleicher Art (Datei 0x42D96: <c>cmp al,[bp-2]</c>/<c>ja</c>).
    /// </summary>
    [Fact]
    public void Join_UnsortedSquad_InsertsBeforeFirstGreaterRole()
    {
        var club = new Club { Index = 2, Name = "Köln", Strength = 80 };
        club.Players.AddRange([
            NewPlayer(PlayerRole.Goalkeeper, 60, "T1"), NewPlayer(PlayerRole.Forward, 60, "S1"),
            NewPlayer(PlayerRole.Defender, 60, "A1"), NewPlayer(PlayerRole.Goalkeeper, 60, "T2"),
        ]);
        club.RenumberPlayers();
        var defender = NewPlayer(PlayerRole.Defender, 60, "Neu");

        PlayerFactory.Join(club, defender, 2, loan: false);

        Assert.Equal(["T1", "Neu", "S1", "A1", "T2"], club.Players.Select(p => p.Name));
        AssertNumbersAreSlots(club);
    }

    /// <summary>
    /// Auto-Aufstellung nach dem Kauf eines gleich starken Torhüters: bei Gleichstand gewinnt der spätere Platz
    /// (Datei 0x4292F, <c>Squad.AutoLineup.cs</c>) – der Neue steht hinter den bisherigen Torhütern und ist Nr. 3, nicht Nr. 26.
    /// </summary>
    [Fact]
    public void AutoLineup_AfterGoalkeeperPurchase_TieWonByLaterSlot()
    {
        var club = new Club { Index = 2, Name = "Köln", Strength = 80 };
        club.Players.Add(NewPlayer(PlayerRole.Goalkeeper, 70, "T1"));
        club.Players.Add(NewPlayer(PlayerRole.Goalkeeper, 60, "T2"));
        club.Players.AddRange(Enumerable.Range(0, 10).Select(i => NewPlayer(PlayerRole.Defender, 60, $"A{i}")));
        club.Players.AddRange(Enumerable.Range(0, 13).Select(i => NewPlayer(PlayerRole.Forward, 60, $"S{i}")));
        club.RenumberPlayers();
        Assert.True(club.ApplyAutoLineup());
        Assert.Equal("T1", club.Lineup[LineupSlot.Goalkeeper]!.Name);

        var bought = NewPlayer(PlayerRole.Goalkeeper, 70, "Neu"); // Wert 70 wie T1
        PlayerFactory.Join(club, bought, 2, loan: false);
        club.ApplyAutoLineup(); // Ende der Transferliste (Datei 0x47BB5)

        Assert.Same(bought, club.Lineup[LineupSlot.Goalkeeper]);
        Assert.Equal(3, bought.Number);
        Assert.Equal(4, club.Players.First(p => p.Name == "A0").Number);
        AssertNumbersAreSlots(club);
    }

    [Fact]
    public void Buy_Goalkeeper_FromTransferList_SortedIn()
    {
        var (game, own, _) = Game();
        SquadLifecycleTests.PlaySeason(game, () => { });
        game.StartNextSeason();
        var listing = game.TransferMarket.Listings.FirstOrDefault(l => l.From != own && !l.Player.OnLoan
                                                                         && l.Player.Role == PlayerRole.Goalkeeper);
        Assert.NotNull(listing);
        game.Managers[0].Money = Math.Max(game.Managers[0].Money, listing.Price * 3);

        Assert.Equal(TransferResult.Accepted, game.TransferMarket.Buy(game, game.Managers[0], listing, listing.Price * 2, loan: false));

        AssertSortedIn(own, listing.Player);
        Assert.NotSame(listing.Player, own.Players[^1]);
        AssertNumbersAreSlots(own);
    }

    /// <summary>
    /// Leihrückkehr zu einem Managerverein (Saisonende Datei 0x2758B → 0x42CB1 mit dem Manager des Stammvereins, Pool +0x1A):
    /// der zurückkehrende Torhüter steht hinter dem letzten Torhüter des Stammvereins (vorher hinten an).
    /// </summary>
    [Fact]
    public void LoanReturn_ToManagerClub_SortedIn()
    {
        var game = GameModeTests.Create(3, true, true, 5, 6);
        var origin = game.Clubs[game.Managers[0].ClubIndex];
        var borrower = game.Clubs[game.Managers[1].ClubIndex];
        var keeper = origin.Players.First(p => p.Role == PlayerRole.Goalkeeper);
        keeper.Age = 25;
        origin.RemovePlayer(keeper);
        PlayerFactory.Join(borrower, keeper, 1, loan: true, new PlayerLoan(origin, 2, keeper.MonthlySalary, false));
        keeper.ContractYears = 2; // A115 (kader-start Teil 4): Rückkehr mit dem Leih-Kaderplatz, also mit Restlaufzeit
        AssertSortedIn(borrower, keeper);

        LifecycleRegressionTests.FinishSeason(game);
        game.StartNextSeason();

        Assert.False(keeper.OnLoan);
        Assert.Contains(keeper, origin.Players);
        AssertSortedIn(origin, keeper);
        AssertNumbersAreSlots(origin);
    }
}
