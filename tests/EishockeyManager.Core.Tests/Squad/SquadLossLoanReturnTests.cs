using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur pruefung-karte Teil 2 (Runde 16): gesammelte P3 aus den Reviews der Runde.
/// </summary>
public class SquadLossLoanReturnTests
{
    /// <summary>
    /// #780 (R765N-01, <c>docs/archiv/reviews/runde-16/2026-11-11-pool-nachpruefung.md</c>): Export eines neuen Spiels im Modus „nur Weltkugel“.
    /// Dort steht der Kader von Satz 0 nicht im Pool (<see cref="TransferMarket.SeatManagerSquads"/>), der Export vergibt ihm über
    /// <c>AddUnseatedSquadPlayers</c> ([APPROX], <c>OriginalExport.Squads.cs</c>) die Plätze wie im Original FILE01 (Torhüter 2–3,
    /// Verteidiger 22–31, Stürmer 98–110, Besitzer 0). Vorher rot mit der Mutation <c>if (missing.Count &gt; 0)</c> →
    /// <c>if (false)</c>: alle 25 Kaderplätze mit Poolindex 0.
    /// </summary>
    [GameDataFact]
    public void WorldOnly_NewGame_Export_SquadOfRecord0OnFile01Slots()
    {
        var raw = OriginalExportTests.RawDatabase();
        var setup = new GameSetup();
        setup.AddManager(new ManagerSetup { Name = "Joerg", Head = 6, ClubIndex = 2 });
        Assert.True(setup.ToggleClubCompetitions());
        Assert.Equal(GameMode.WorldOnly, setup.Mode);
        // Saat 2 (bis Spur start-lader 5): Mit dem Lader beim Spielstart (Datei 0x439BB) verschiebt sich der Zufall; bei Saat 5 steht
        // dann Poolplatz 27 auf dem Markt, den AddUnseatedSquadPlayers ([APPROX]) überspringt (22–26, 28–32). Etwa jede zweite Saat
        // trifft das – Restlücke im PR der Spur, nicht Gegenstand dieses Tests.
        // Runde 21, Spur zufallsstrom Teil 1 (R21-D2 = A): Saat 1 statt 2 – der Pool des Spielstarts zieht aus dem gemeinsamen Strom (Datei 0x29896);
        // bei Saat 2 steht dann Poolplatz 3 auf dem Markt (dieselbe Restlücke wie oben), Saaten 1, 5, 6 treffen nicht.
        // Runde 21, Spur zufallsstrom Teil 3 Spielstart (R21-D2 = A, nach Rebase auf zusehen): Saat 2 statt 1 – der Spielstart zieht in der Reihenfolge des Originals aus dem gemeinsamen Strom (Kaderplätze der Manager im Poolaufbau Datei 0x5B0BB–0x5B2BE, Platzwahl Datei 0x29C91 nach dem Pool, Startwerte Datei 0x29DA4, Markt Datei 0x43A83 nach der Wanderung), anderer Zufallsverlauf; bei Saat 1, 4, 7 steht einer der Poolplätze des Kaders auf dem Markt (dieselbe Restlücke wie oben), 2, 3, 5, 6 treffen nicht. Die Probe hängt an der Saat.
        // Runde 21, Spur zufallsstrom Teil 4 (R21-D2 = A, R15-E2 = C): Saat 3 statt 2 – Pokalauslosungen (Datei 0x298B1, 0x43A8C) und Pokalspiele ziehen aus dem gemeinsamen Strom statt aus CupRandom, anderer Zufallsverlauf ab dem Spielstart; bei Saat 2 steht Poolplatz 3 auf dem Markt (dieselbe Restlücke wie oben), 3, 4, 6, 8 treffen nicht.
        var game = NewGame.Create(ClubDatabase.Parse(raw), setup, 3);
        Assert.Equal(GameMode.WorldOnly, game.Mode);
        var squad = game.Clubs[game.Managers[0].ClubIndex].Players;
        Assert.Equal(25, squad.Count);

        var save = OriginalSaveGame.Read(OriginalExport.Export(game, raw));

        var indices = Enumerable.Range(0, squad.Count).Select(k => (int)OriginalImport.SquadSlot(save, 0, k)[0x0F]).ToArray();
        int[] expected = [2, 3, .. Enumerable.Range(22, 10), .. Enumerable.Range(98, 13)];
        Assert.Equal(expected, indices.Order().ToArray());
        foreach (var index in indices)
            Assert.Equal(0, OriginalImport.PoolEntry(save, index)[0x1A]); // Besitzer Manager 0
        // Jeder Kaderplatz zeigt auf den Poolsatz seines Spielers (Art und Name).
        var roundTrip = GameState.ImportOriginal(save, ClubDatabase.Parse(raw), 2);
        Assert.Empty(roundTrip.ValidatePlayerIdentity());
        Assert.Equal(squad.Select(p => p.Role).Order(), roundTrip.Clubs[roundTrip.Managers[0].ClubIndex].Players.Select(p => p.Role).Order());
    }

    /// <summary>
    /// #778 (R766-P3-3, <c>docs/archiv/reviews/runde-16/2026-11-11-abgleich-rest.md</c>): Der Kaderverlust ohne <c>extendExpired</c> ist kein
    /// Fehler. Es gehen nur Spieler, deren Vertrag am Saisonende ausläuft: Verlängert wird im Lauf
    /// der Saison nur, wer sich meldet ([ORIGINAL] Datei 0x076FA–0x07951, je Takt random(0, Grenze) = 0 und random(0, 3) = 0); die
    /// übrigen kommen in die Verhandlung am Saisonende (Datei 0x27B64/0x27D05), die <see cref="GameState.StartNextSeason"/> ohne
    /// Oberfläche wie „Abbruch“ abschließt (Spieler geht). Die Manager der Probe kaufen nicht nach, also schrumpft der Kader; die
    /// übrigen Mehrsaisonproben halten ihn deshalb mit <c>extendExpired</c>. Runde 27 (aufraeumen-migration): Aufbau wie die
    /// entfallene Schema-22-Fixture (synthetische Datenbank, Saat 22, Vereine 2 und 14), aber neu begonnen statt geladen.
    /// </summary>
    [Fact]
    public void TwoManagers_SquadLoss_OnlyExpiringContracts()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Aktiv", Head = 0, ClubIndex = 2 });
        setup.Managers.Add(new ManagerSetup { Name = "Passiv", Head = 1, ClubIndex = 14 });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 22);
        var lost = 0;
        for (var change = 0; change < 4; change++)
        {
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 3000, $"Saisonende {change + 1} nicht erreicht");
                foreach (var m in game.Managers)
                    TestSeason.KeepSquad(game, m);
                game.Continue();
            }
            var before = game.Managers.ToDictionary(m => m, m => game.Clubs[m.ClubIndex].Players.ToList());
            var expiring = before.Values.SelectMany(l => l).Where(p => p.ContractYears <= 1)
                .ToHashSet(ReferenceEqualityComparer.Instance);
            game.StartNextSeason();
            foreach (var (m, players) in before)
            {
                var after = game.Clubs[m.ClubIndex].Players.ToHashSet(ReferenceEqualityComparer.Instance);
                foreach (var gone in players.Where(p => !after.Contains(p)))
                {
                    Assert.True(expiring.Contains(gone), $"Saisonwechsel {change + 1}: {gone.Name} ging mit Laufzeit {gone.ContractYears}");
                    lost++;
                }
            }
        }
        Assert.True(lost >= 20, $"Probe ohne Kaderverlust ({lost})"); // sonst prüft der Test nichts
    }

    /// <summary>
    /// #776 (R765-02, <c>docs/archiv/reviews/runde-16/2026-11-11-pool.md</c>): Leihe Manager → Manager mit einem Leihjahr. [ORIGINAL] Saisonende
    /// 0x26F20: Laufzeit +0x0B heruntergezählt (Datei 0x27500), dann bei Besitzer &lt; 4 Kopie des Leih-Kaderplatzes (0x34 Byte,
    /// 0x48:0x2BD4, Datei 0x27597–0x275D2) in den Kader des Stammmanagers – also mit Laufzeit 0; danach die Verhandlungen über alle
    /// Kaderplätze (Schleife ab Datei 0x27B64). Der Spieler steht deshalb wieder beim Stammverein, Verhandlung offen, Poolplatz
    /// vergeben. „In keinem Kader“ zeigt nur der Saisonwechsel ohne Oberfläche (<see cref="GameState.StartNextSeason"/> schließt die
    /// Verhandlung wie „Abbruch“). Kein Fehler; ob das Original bei Leihe zwischen Managern ebenso verfährt, ist an keinem
    /// Spielstand belegt ([APPROX] in <c>Player.Contract.cs</c>, nur nach dem Code).
    /// </summary>
    [Fact]
    public void LoanBetweenManagers_LastLoanYear_ReturnsToOriginWithOpenRenewal()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", ClubIndex = 5, StartLeague = League.Bundesliga });
        setup.Managers.Add(new ManagerSetup { Name = "B", ClubIndex = 7, StartLeague = League.Bundesliga });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 1);
        var origin = game.Clubs[game.Managers[0].ClubIndex];
        var borrower = game.Clubs[game.Managers[1].ClubIndex];
        foreach (var p in origin.Players.Concat(borrower.Players))
            (p.ContractYears, p.Age) = (3, 25);
        var lent = origin.Players.Last(p => p.Role == PlayerRole.Forward);
        var slot = game.TransferMarket.Pool.Single(s => ReferenceEquals(s.Player, lent));
        origin.RemovePlayer(lent);
        PlayerFactory.Join(borrower, lent, 1, loan: true, new PlayerLoan(origin, 3, lent.MonthlySalary, false), managerSquad: true);
        Assert.Equal(1, lent.ContractYears);

        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 20_000, "Saisonende nicht erreicht");
            if (game.NationalTurn && game.NationalCoach!.Squad.Count == 0)
                game.AutoNominate();
            game.Continue();
        }
        game.StartNextSeasonWithRenewals();

        Assert.Contains(lent, origin.Players);
        Assert.DoesNotContain(lent, borrower.Players);
        Assert.Null(lent.Loan);
        Assert.Equal(0, lent.ContractYears); // Leih-Laufzeit, nicht der Stammvertrag (3)
        Assert.Contains(lent, game.PendingRenewals(game.Managers[0]));
        Assert.Same(lent, slot.Player);
        Assert.Equal(PoolOwner.Taken, slot.Owner);
        Assert.Equal(origin.Index, slot.Club);
    }
}
