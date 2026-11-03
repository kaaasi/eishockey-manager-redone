using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Runde 3, Spur „kader“: R2-11 (Vertragsende ohne Vorwarnung) und R2-12 (Kader-KI der Computervereine).
/// </summary>
public class SquadLifecycleTests
{
    private static Club OwnClub(GameState game) => game.Clubs[game.Managers[0].ClubIndex];

    /// <summary>Spielt bis zum Saisonende; <paramref name="eachStep"/> läuft nach jedem Weiter-Schritt.</summary>
    internal static void PlaySeason(GameState game, Action? eachStep = null)
    {
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 1000, "Saisonende nicht erreicht");
            foreach (var manager in game.Managers)
                manager.Money = 10_000_000; // keine Entlassung im Testlauf
            game.Continue();
            eachStep?.Invoke();
        }
        Assert.Equal(SeasonPhase.SeasonOver, game.Phase);
    }

    private static Message[] ContractMessages(GameState game, Player p) => game.Inbox(game.Managers[0])
        .Where(m => m.Season == game.Season && m.Lines.Length > 0 && m.Lines[0].StartsWith(p.Name + " ", StringComparison.Ordinal))
        .ToArray();

    // ---- R2-11 -------------------------------------------------------------------------------------

    /// <summary>
    /// R2-11 (P1): Vorher verlor ein passiver Manager zu Beginn der dritten Saison alle Spieler, ohne dass irgendeine
    /// Nachricht im Posteingang stand. Spur kader-transfer, Teil 7 (Tagesablauf 0x07169): Ein Teil der Spieler im letzten
    /// Vertragsjahr meldet sich mit einem Angebot oder der Ruhestandsmeldung (höchstens einmal, ohne Frist); wer sich nicht
    /// meldet, kommt in die Verhandlung am Saisonende (0x27D05) – gewarnt wird also jeder.
    /// </summary>
    [Fact]
    public void R211_PassiveManager_IsWarnedOnceForEveryExpiringPlayer_BeforeTheDeadline()
    {
        var game = TestGames.Create();
        PlaySeason(game);
        game.StartNextSeason();
        var expiring = OwnClub(game).Players.Where(p => p.ContractYears == 1 && !p.OnLoan).ToArray();
        Assert.Equal(OwnClub(game).Players.Count, expiring.Length); // alle Startverträge laufen jetzt aus

        PlaySeason(game);

        var announced = 0;
        foreach (var p in expiring)
        {
            var messages = ContractMessages(game, p);
            Assert.InRange(messages.Length, 0, 1);
            if (messages.Length == 0)
            {
                Assert.Equal(ContractNotice.None, p.Notice);
                continue;
            }
            announced++;
            var text = string.Join(" ", messages[0].Lines);
            Assert.True(text.Contains("bietet Ihnen an, von 1 auf") && text.EndsWith("Jahre zu verlängern.")
                || text.EndsWith("kündigt an, daß er seinen Vertrag nicht mehr verlängern wird."), text);
            Assert.NotEqual(ContractNotice.None, p.Notice);
        }
        Assert.InRange(announced, 1, expiring.Length - 1);
        game.StartNextSeasonWithRenewals();
        Assert.All(expiring.Where(p => p.Notice == ContractNotice.None && OwnClub(game).Players.Contains(p)),
            p => Assert.True(GameState.IsRenewalPending(p), p.Name));
    }

    /// <summary>R2-11: Nach dem Angebot lässt sich der Vertrag verlängern; der Spieler bleibt über den Saisonwechsel.</summary>
    [Fact]
    public void R211_OfferedPlayer_CanBeExtended_AndStaysAfterSeasonChange()
    {
        var game = TestGames.Create();
        PlaySeason(game);
        game.StartNextSeason();
        var extended = new Dictionary<Player, int>();
        PlaySeason(game, () =>
        {
            foreach (var p in OwnClub(game).Players.Where(p => p.Notice == ContractNotice.Offered && p.ContractYears == 1))
            {
                Assert.Equal(NegotiationResult.Accepted, Contracts.Negotiate(game, game.Managers[0], p, p.NoticeYears,
                    Contracts.Demand(p) + 100, false, demandFilled: true));
                extended[p] = p.NoticeYears;
            }
        });
        Assert.NotEmpty(extended);
        var leaving = OwnClub(game).Players.Where(p => p.ContractYears == 1).ToArray();

        game.StartNextSeason();

        foreach (var (p, years) in extended)
        {
            Assert.Contains(p, OwnClub(game).Players);
            Assert.Equal(years - 1, p.ContractYears);
        }
        foreach (var p in leaving)
            Assert.DoesNotContain(p, OwnClub(game).Players);
        Assert.All(OwnClub(game).Players, p => Assert.Equal(ContractNotice.None, p.Notice));
    }

    /// <summary>R2-11: Wer abgesagt hat oder aufhört, ist nicht verhandlungsbereit (Meldungen aus em.exe).</summary>
    [Fact]
    public void R211_RefusingOrRetiringPlayer_CannotBeExtended()
    {
        var game = TestGames.Create();
        var p = new Player { Name = "HUBER", Role = PlayerRole.Forward, Condition = 40, Technique = 40, Form = 40, ContractYears = 1, MonthlySalary = 900 };
        game.Clubs[game.ActiveManager.ClubIndex].Players.Add(p);
        p.Notice = ContractNotice.Refused;
        Assert.Equal(NegotiationResult.NotWilling, Contracts.Negotiate(game, game.ActiveManager, p, 3, 99_999, false, false));
        Assert.Equal(["HUBER", "ist im Moment", "nicht verhandlungs-", "bereit."], Contracts.Message(NegotiationResult.NotWilling, p));
        p.Notice = ContractNotice.Retiring;
        Assert.Equal(NegotiationResult.Retiring, Contracts.Negotiate(game, game.ActiveManager, p, 3, 99_999, false, false));
        Assert.Equal(["HUBER", "wird sich mit", "Ablauf des Vertrages", "zur Ruhe setzen."], Contracts.Message(NegotiationResult.Retiring, p));
        Assert.Equal(1, p.ContractYears);
        Assert.Equal(900, p.MonthlySalary);
    }

    // ---- R2-12 und Mehrsaisonprobe ----------------------------------------------------------------

    /// <summary>Mindestbesetzung eines Computerkaders (wie beim Spielstart): Torhüter, Verteidiger, Stürmer.</summary>
    private static readonly (PlayerRole Role, int Minimum)[] Minimum =
        [(PlayerRole.Goalkeeper, 2), (PlayerRole.Defender, 8), (PlayerRole.Forward, 10)];

    /// <summary>
    /// Mehrsaisonprobe (Pflicht seit Runde 3): sechs Saisonwechsel, passiver und aktiver Manager.
    /// Vorher: Computerkader blieben mit denselben Spielern bestehen und alterten unbegrenzt (keine Talente, kein
    /// Ruhestand, Verlängerung immer – [APPROX]-Überbrückung aus Welle B).
    /// </summary>
    [Theory]
    [InlineData(false, 3)]
    [InlineData(true, 3)]
    [InlineData(false, 11)]
    [InlineData(true, 11)]
    public void MultiSeason_SixChanges_ComputerSquadsStayPlayable(bool active, int seed) =>
        RunProbe(TestGames.Create(seed), active, changes: 6);

    /// <summary>Dieselbe Probe mit der Originaldatenbank (Verein 42 wie in den Reviews).</summary>
    [GameDataTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultiSeason_SixChanges_OriginalDatabase(bool active)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Probe", ClubIndex = 42 });
        var game = NewGame.Create(ClubDatabase.Load(TestGameData.Load()), setup, seed: 3);
        RunProbe(game, active, changes: 6);
    }

    internal static void RunProbe(GameState game, bool active, int changes)
    {
        var managed = game.Managers.Select(m => m.ClubIndex).ToHashSet();
        var computer = game.Clubs.Where(c => c.Players.Count > 0 && !managed.Contains(c.Index)).ToArray();
        var known = new HashSet<Player>(AllPlayers(game));
        var startPlayers = known.ToArray();
        var managerPlayers = OwnClub(game).Players.ToArray();

        for (var change = 1; change <= changes; change++)
        {
            PlaySeason(game, active ? () => ExtendOffers(game) : null);
            var expiring = OwnClub(game).Players.Where(p => p.ContractYears == 1 && !p.OnLoan).ToArray();
            // Spur kader-transfer, Teil 7 (0x07169): höchstens eine Ankündigung, ohne Frist; die übrigen gehen in die Verhandlung
            // am Saisonende (0x27D05).
            foreach (var p in expiring)
                Assert.True(ContractMessages(game, p).Length <= 1, $"Saison {game.Season}: mehrere Ankündigungen für {p.Name}");

            game.StartNextSeason();

            // Kein Spieler verloren oder doppelt: Kader, Pool und Laufbahnende zusammen.
            var all = AllPlayers(game).ToArray();
            Assert.Equal(all.Length, all.Distinct().Count());
            Assert.Empty(known.Except(all));
            known.UnionWith(all);

            foreach (var club in computer)
            {
                foreach (var (role, minimum) in Minimum)
                    Assert.True(club.Players.Count(p => p.Role == role) >= minimum,
                        $"Wechsel {change}: {club.Name} hat zu wenige {role}");
                Assert.InRange(club.Players.Count, 20, TransferMarket.MaxSquad + 3);
                foreach (var p in club.Players)
                    Assert.Empty(PlayerFactory.Validate(club, p));
            }
            Assert.All(computer.SelectMany(c => c.Players), p => Assert.InRange(p.Age, PlayerFactory.TalentMinAge, 36));
            // Spur kader-transfer, Teil 7 (0x0754D): Im Managerkader kündigen Feldspieler ab 36, Torhüter ab 42 (Technik über
            // Kondition später) den Ruhestand an und hören erst bei Vertragsende auf – ältere Spieler sind dort möglich.
            Assert.All(OwnClub(game).Players, p => Assert.InRange(p.Age, PlayerFactory.TalentMinAge, 45));
            Assert.InRange(game.TransferMarket.FreeAgents.Count(), 0, TransferMarket.MaxFreeAgents);
            Assert.All(game.TransferMarket.FreeAgents, p => Assert.Equal(0, p.ContractYears));
            Assert.All(game.RetiredPlayers, p => Assert.DoesNotContain(p, game.Clubs.SelectMany(c => c.Players)));

            if (active)
                SignFreeAgents(game);
            else if (change >= 2)
                Assert.Empty(OwnClub(game).Players); // passiv: alle Startverträge enden nach zwei Jahren (angekündigt)
        }

        // Plausible Altersverteilung nach sechs Wechseln.
        var ages = computer.SelectMany(c => c.Players).Select(p => p.Age).ToArray();
        Assert.InRange(ages.Average(), 23.0, 29.0);
        Assert.True(ages.Count(a => a <= 23) >= ages.Length / 10, "kaum junge Spieler");
        Assert.True(ages.Count(a => a >= 33) <= ages.Length / 10, "zu viele alte Spieler");
        foreach (var club in computer)
            Assert.InRange(club.Players.Average(p => p.Age), 21.0, 31.0);
        Assert.NotEmpty(game.RetiredPlayers);
        Assert.True(known.Count > startPlayers.Length, "keine Neuzugänge aus Talenten");
        Assert.Contains(startPlayers, p => p.Age >= 32 && game.RetiredPlayers.Contains(p));
        if (active)
            Assert.True(OwnClub(game).Players.Count >= 18, $"aktiver Manager hat nur {OwnClub(game).Players.Count} Spieler");
        _ = managerPlayers;
    }

    // Teil 5 (transfer-vertraege-komplett): auch der Spielerpool – beim Saisonwechsel tauscht er Spieler mit den Computerkadern.
    private static IEnumerable<Player> AllPlayers(GameState game) =>
        game.Clubs.SelectMany(c => c.Players).Concat(game.TransferMarket.FreeAgents).Concat(game.RetiredPlayers)
            .Concat(game.TransferMarket.PoolPlayers);

    /// <summary>Aktiver Manager: nimmt jedes Verlängerungsangebot zur Forderung an.</summary>
    internal static void ExtendOffers(GameState game)
    {
        foreach (var p in OwnClub(game).Players.Where(p => p.Notice == ContractNotice.Offered && p.ContractYears == 1))
            Assert.Equal(NegotiationResult.Accepted,
                Contracts.Negotiate(game, game.Managers[0], p, p.NoticeYears, Contracts.Demand(p) + 100, false, demandFilled: true));
    }

    /// <summary>Aktiver Manager: füllt den Kader zu Saisonbeginn mit ablösefreien Spielern auf 20 auf.</summary>
    internal static void SignFreeAgents(GameState game)
    {
        var manager = game.Managers[0];
        while (OwnClub(game).Players.Count < 20
               && game.TransferMarket.Listings.FirstOrDefault(l => l.IsFreeAgent) is { } listing)
            Assert.Equal(TransferResult.Accepted, game.TransferMarket.Buy(game, manager, listing, 0, loan: false));
        // Teil 5 (0x494EF): Vertragslose anderer Vereine stehen nicht mehr auf der Liste; der aktive Manager kauft dann
        // Poolspieler bis 30 Jahre (Poolspieler altern im Original ohne Laufbahnende, 0x274A7) über 130 % des Listenpreises
        // (immer angenommen, 0x4991D).
        while (OwnClub(game).Players.Count < 20
               && game.TransferMarket.Listings.FirstOrDefault(l => l.FromPool && l.Player.Age <= 30) is { } pool)
        {
            manager.Money = Math.Max(manager.Money, pool.Price * 2);
            Assert.Equal(TransferResult.Accepted, game.TransferMarket.Buy(game, manager, pool, pool.Price * 135 / 100, loan: false));
            pool.Player.ContractYears = 2; // wie die Startverträge; die Ankündigungsprobe bleibt bei den übrigen Spielern
        }
        // #351 (Leitstelle): Ob genug Poolspieler bis 30 auf der Liste stehen, hängt am Zufall des Listenwechsels; mit dem
        // geänderten Verlauf (Play-offs ohne Manager über den schnellen Weg) fehlten nach sechs Wechseln (Seed 3) Spieler.
        // Dann auch Poolspieler bis 32 kaufen (Altersgrenze 36 der Probe) – Vorbedingung ist ein voller Kader des aktiven Managers.
        while (OwnClub(game).Players.Count < 20
               && game.TransferMarket.Listings.FirstOrDefault(l => l.FromPool && l.Player.Age <= 32) is { } older)
        {
            manager.Money = Math.Max(manager.Money, older.Price * 2);
            Assert.Equal(TransferResult.Accepted, game.TransferMarket.Buy(game, manager, older, older.Price * 135 / 100, loan: false));
            older.Player.ContractYears = 2;
        }
    }
}
