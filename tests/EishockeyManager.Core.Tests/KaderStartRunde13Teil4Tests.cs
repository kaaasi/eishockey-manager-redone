using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur <c>kader-start</c>, Runde 13, Teil 4: Kaderbefunde aus <c>docs/analyse/re-runde-13.md</c>, Sitzung 2 und 3.
/// <list type="bullet">
/// <item>A091: Nr. = Kaderplatz + 1; Verkauf rückt auf, Kauf nach Spielerart einsortiert (R13GN-01, Stürmer hinten); Auto-Aufstellung und Spiel in Kaderplatzfolge
///   (vorher kleinste freie Rückennummer und Folge Rolle/Nummer).</item>
/// <item>A166: Eingesetzte ohne Lauf von 0x426BC über dessen Ersatzrunde (vorher Summe K + T + F ohne Reihum-Auffüllung).</item>
/// <item>Regressionstest A166 (Review R13RD-06): die Kaderwerte aus FILE05 (Spielstand des Nutzers, Satz 0, Köln) ergeben
///   genau die Reihen und Eingesetzten aus FILE05, mit dem Kauf (DAFFNER hinten) FILE07, ohne HOCK FILE26.</item>
/// </list>
/// Namen sind Platzhalter; die Werte (Kondition, Technik, Form) und die Platzfolge stammen aus <c>emtool hok dump</c> der
/// Stände FILE05/FILE07/FILE26 (Referenz-Repo <c>original/spielstaende/</c>).
/// </summary>
public class KaderStartRunde13Teil4Tests
{
    /// <summary>FILE05, Satz 0, Plätze 1–25: Rolle, Kondition, Technik, Form.</summary>
    private static readonly (PlayerRole Role, int C, int T, int F)[] File05 =
    [
        (PlayerRole.Goalkeeper, 80, 79, 46), (PlayerRole.Goalkeeper, 80, 83, 45),
        (PlayerRole.Defender, 81, 79, 41), (PlayerRole.Defender, 82, 83, 45), (PlayerRole.Defender, 80, 82, 49),
        (PlayerRole.Defender, 83, 81, 54), (PlayerRole.Defender, 79, 83, 56), (PlayerRole.Defender, 80, 82, 60),
        (PlayerRole.Defender, 79, 80, 49), (PlayerRole.Defender, 80, 80, 52), (PlayerRole.Defender, 79, 80, 56),
        (PlayerRole.Defender, 79, 82, 54),
        (PlayerRole.Forward, 80, 82, 46), (PlayerRole.Forward, 82, 81, 54), (PlayerRole.Forward, 79, 82, 56),
        (PlayerRole.Forward, 79, 80, 52), (PlayerRole.Forward, 83, 83, 53), (PlayerRole.Forward, 81, 82, 58),
        (PlayerRole.Forward, 81, 83, 42), (PlayerRole.Forward, 79, 80, 40), (PlayerRole.Forward, 82, 82, 55),
        (PlayerRole.Forward, 79, 80, 54), (PlayerRole.Forward, 79, 79, 45), (PlayerRole.Forward, 79, 80, 45),
        (PlayerRole.Forward, 83, 82, 40),
    ];

    /// <summary>FILE07, Platz 26 (DAFFNER, gekauft).</summary>
    private static readonly (PlayerRole Role, int C, int T, int F) Daffner = (PlayerRole.Forward, 67, 88, 51);

    private static int s_nextId = 1;

    /// <summary>Spieler mit eigener ID („Eingesetzt“ merkt sich IDs; die Vergabe <c>AssignPlayerId</c> ist intern).</summary>
    private static Player NewPlayer((PlayerRole Role, int C, int T, int F) v, string name)
    {
        var p = new Player { Name = name, Role = v.Role, Condition = v.C, Technique = v.T, Form = v.F, Age = 25, ContractYears = 2 };
        typeof(Player).GetProperty(nameof(Player.Id))!.SetValue(p, Interlocked.Increment(ref s_nextId));
        return p;
    }

    private static Club File05Club()
    {
        var club = new Club { Index = 2, Name = "Köln", Strength = 80 };
        club.Players.AddRange(File05.Select((v, i) => NewPlayer(v, $"P{i + 1}")));
        club.RenumberPlayers();
        return club;
    }

    /// <summary>Nr. (Platz + 1 im Original) der Spieler einer Reihe, aufsteigend wie in der Anzeige.</summary>
    private static int[] Row(Club club, int block, bool forwards) =>
        Enumerable.Range(0, forwards ? Lineup.ForwardsPerBlock : Lineup.DefendersPerBlock)
            .Select(k => club.Lineup[forwards ? LineupSlot.Forward(block, k) : LineupSlot.Defender(block, k)]!.Number)
            .ToArray();

    private static int[] NotDressed(Club club) =>
        club.Players.Where(p => !club.IsDressed(p)).Select(p => p.Number).ToArray();

    private static void AssertNumbersAreSlots(Club club) =>
        Assert.Equal(Enumerable.Range(1, club.Players.Count), club.Players.Select(p => p.Number));

    // ---- A166: Regressionstest gegen FILE05/FILE07/FILE26 ------------------------------------------------------------

    [Fact]
    public void AutoLineup_File05Values_GiveFile05Lineup()
    {
        var club = File05Club();
        Assert.True(club.ApplyAutoLineup());

        Assert.Equal(2, club.Lineup[LineupSlot.Goalkeeper]!.Number);
        Assert.Equal([7, 8], Row(club, 0, forwards: false));
        Assert.Equal([6, 12], Row(club, 1, forwards: false));
        Assert.Equal([10, 11], Row(club, 2, forwards: false));
        Assert.Equal([17, 18, 21], Row(club, 0, forwards: true));
        Assert.Equal([14, 15, 22], Row(club, 1, forwards: true));
        // Gleichstand Wert 68 (Plätze 19, 24, 25): der spätere Platz gewinnt (Datei 0x4292F).
        Assert.Equal([13, 16, 25], Row(club, 2, forwards: true));
        Assert.Equal(22, club.DressedCount);
        Assert.Equal([3, 20, 23], NotDressed(club));
    }

    [Fact]
    public void AutoLineup_AfterPurchase_GivesFile07()
    {
        var club = File05Club();
        club.ApplyAutoLineup();
        PlayerFactory.Join(club, NewPlayer(Daffner, "Neu"), 2, loan: false);
        Assert.Equal(26, club.Players[^1].Number); // [ORIGINAL] Datei 0x42D55: Stürmer hinten (größte Art)

        club.ApplyAutoLineup(); // Ende der Transferliste (Datei 0x47BB5)

        Assert.Equal([13, 16, 26], Row(club, 2, forwards: true));
        Assert.Equal([3, 19, 20, 23], NotDressed(club));
    }

    [Fact]
    public void AutoLineup_AfterSale_GivesFile26_PlacesMoveUp()
    {
        var club = File05Club();
        club.ApplyAutoLineup();
        var sold = club.Players[13];
        var formerFifteen = club.Players[14];

        club.RemovePlayer(sold); // FILE26: Platz 14 (Nr. 14) auf die Transferliste
        club.ApplyAutoLineup();

        AssertNumbersAreSlots(club);
        Assert.Equal(14, formerFifteen.Number);
        Assert.Equal([16, 17, 20], Row(club, 0, forwards: true));
        Assert.Equal([14, 15, 21], Row(club, 1, forwards: true));
        Assert.Equal([13, 23, 24], Row(club, 2, forwards: true));
        Assert.Equal([3, 19], NotDressed(club));
    }

    // ---- A091: Verkauf, dann Kauf ------------------------------------------------------------------------------------

    /// <summary>
    /// re-runde-13 A091, Testidee: Nr. 14 verkaufen → die bisherigen Nr. 15–25 heißen 14–24; dann einen Stürmer kaufen → er ist
    /// Nr. 25 (nicht die Lücke 14) und gewinnt den Gleichstand gegen gleich starke Spieler mit kleinerer Nr. Vorher bekam er die
    /// kleinste freie Rückennummer 14 und verlor den Gleichstand.
    /// </summary>
    [Fact]
    public void SaleThenPurchase_NewPlayerLast_WinsTie()
    {
        var club = File05Club();
        club.ApplyAutoLineup();
        club.RemovePlayer(club.Players[13]);
        var bought = NewPlayer(Daffner, "Neu"); // Wert 68 wie die Plätze 18, 23, 24 nach dem Aufrücken
        PlayerFactory.Join(club, bought, 2, loan: false);

        Assert.Equal(25, bought.Number);
        AssertNumbersAreSlots(club);
        club.ApplyAutoLineup();
        Assert.Equal(LineupSlot.Forward(2, 2), club.Lineup.FindSlot(bought));
    }

    [Fact]
    public void LiveSquad_FollowsSquadSlots_NotRoleAndNumber()
    {
        var club = File05Club();
        club.ApplyAutoLineup();
        var goalie = NewPlayer((PlayerRole.Goalkeeper, 70, 70, 70), "Torhüter");
        PlayerFactory.Join(club, goalie, 2, loan: false);

        var squad = LiveSquad.Create(club, 3, null);

        Assert.Equal(club.Players, squad.Players.Select(p => p.Player));
        Assert.Same(goalie, squad.Players[2].Player); // hinter den beiden Torhütern (Datei 0x42D55, R13GN-01)
    }

    // ---- A166: Rückfall ohne Lauf von 0x426BC -------------------------------------------------------------------------

    /// <summary>
    /// Ohne Lauf von 0x426BC: Aufgestellte und dann reihum Sturm, Abwehr, Torhüter (Datei 0x427C0–0x4282C). Vorher nahm der
    /// Rückfall die stärksten übrigen Spieler nach K + T + F – hier sechs Stürmer statt Sturm/Abwehr/Torhüter reihum.
    /// </summary>
    [Fact]
    public void Dressed_WithoutAutoLineupRun_UsesReserveRound()
    {
        var club = new Club { Index = 2, Name = "Köln", Strength = 80 };
        var lineup = new List<Player> { NewPlayer((PlayerRole.Goalkeeper, 60, 60, 60), "T") };
        lineup.AddRange(Enumerable.Range(0, 6).Select(i => NewPlayer((PlayerRole.Defender, 60, 60, 60), $"A{i}")));
        lineup.AddRange(Enumerable.Range(0, 9).Select(i => NewPlayer((PlayerRole.Forward, 60, 60, 60), $"S{i}")));
        var forwards = Enumerable.Range(0, 6).Select(i => NewPlayer((PlayerRole.Forward, 80, 80, 80), $"RS{i}")).ToArray();
        var defenders = Enumerable.Range(0, 2).Select(i => NewPlayer((PlayerRole.Defender, 50, 50, 50), $"RA{i}")).ToArray();
        var reserveGoalie = NewPlayer((PlayerRole.Goalkeeper, 40, 40, 40), "RT");
        club.Players.AddRange(lineup);
        club.Players.AddRange(forwards);
        club.Players.AddRange(defenders);
        club.Players.Add(reserveGoalie);
        club.RenumberPlayers();
        Assert.True(club.Lineup.Assign(LineupSlot.Goalkeeper, lineup[0]));
        for (var b = 0; b < 3; b++)
        {
            for (var k = 0; k < 2; k++)
                Assert.True(club.Lineup.Assign(LineupSlot.Defender(b, k), lineup[1 + 2 * b + k]));
            for (var k = 0; k < 3; k++)
                Assert.True(club.Lineup.Assign(LineupSlot.Forward(b, k), lineup[7 + 3 * b + k]));
        }

        // S, A, T, S, A, (T → Abwehr → Sturm): drei Stürmer, beide Verteidiger, der Torhüter.
        Assert.Equal(22, club.DressedCount);
        Assert.True(club.IsDressed(reserveGoalie));
        Assert.All(defenders, p => Assert.True(club.IsDressed(p)));
        Assert.Equal(3, forwards.Count(club.IsDressed));
        // Gleichstand: der spätere Platz zuerst (Datei 0x4292F).
        Assert.Equal(forwards.Skip(3), forwards.Where(club.IsDressed));
    }

    // ---- A085/A091: Mehrsaisonprobe -----------------------------------------------------------------------------------

    /// <summary>
    /// Mehrsaisonprobe (AGENTS.md §5): vier Saisonwechsel, passiver und aktiver Manager. In jedem Kader gilt Nr. = Platz + 1.
    /// Im Managerkader behält, wer bleibt, seine Folge (A085: der Saisonwechsel ordnet nicht um, FILE22/FILE23); Neue stehen
    /// nach Spielerart einsortiert (Datei 0x42D55, R13GN-01).
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultiSeason_FourChanges_SlotOrderKept(bool active)
    {
        var game = TestGames.Create(3);
        var own = game.Clubs[game.Managers[0].ClubIndex];
        for (var change = 1; change <= 4; change++)
        {
            var before = own.Players.ToArray();
            var bought = false;
            SquadLifecycleTests.PlaySeason(game, active ? Step : null);
            game.StartNextSeason();
            if (active)
                Assert.True(bought, $"Wechsel {change}: kein Kauf");

            void Step()
            {
                SquadLifecycleTests.ExtendOffers(game);
                if (bought || game.Phase != SeasonPhase.Running || own.Players.Count >= TransferMarket.MaxSquad
                    || game.TransferMarket.Listings.FirstOrDefault(l => l.From != own && !l.Player.OnLoan) is not { } listing)
                    return;
                // Ein Kauf je Saison: nach Spielerart einsortiert (Datei 0x42D55).
                game.Managers[0].Money = Math.Max(game.Managers[0].Money, listing.Price * 3);
                Assert.Equal(TransferResult.Accepted,
                    game.TransferMarket.Buy(game, game.Managers[0], listing, listing.Price * 2, loan: false));
                var at = own.Players.IndexOf(listing.Player);
                Assert.All(own.Players.Take(at), q => Assert.True(q.Role <= listing.Player.Role));
                Assert.True(at + 1 == own.Players.Count || own.Players[at + 1].Role > listing.Player.Role);
                AssertNumbersAreSlots(own);
                bought = true;
            }

            Assert.All(game.Clubs.Where(c => c.Players.Count > 0), AssertNumbersAreSlots);
            var stayed = own.Players.Where(before.Contains).ToArray();
            Assert.Equal(before.Where(stayed.Contains), stayed);
        }
        Assert.Equal(1997, game.Season);
    }
}
