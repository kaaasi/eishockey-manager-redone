using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur <c>kader-transfer</c>, Teil 5 (Inventur A226, P2 Nr. 5): Spielerwert am Saisonende. Das Original setzt vor der
/// Vertragsverhandlung (Datei 0x27B64 ff.) nur Strafen, Tore und die Saisonspiele zurück (Datei 0x27A0C–0x27AA8: Kaderplatz
/// +0x00, +0x01, +0x03–+0x05, +0x06–+0x08), nicht die Spiele beim jetzigen Verein (+0x1C/+0x1E/+0x20, nur erhöht in
/// Datei 0x41622 und 0x5499E, Gewicht 50/20/30 im Spielerwert). Vorher rot: Das Remake setzte die Spiele mit den übrigen
/// Saisonzählern zurück; Gehalt und Ablöse am Saisonende fielen ohne den Spieleanteil zu niedrig aus.
/// </summary>
public class KaderTransferTeil5Tests
{
    private static void PlaySeason(GameState game)
    {
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 1000, "Saisonende nicht erreicht");
            foreach (var m in game.Managers)
                m.Money = Math.Max(m.Money, 5_000_000);
            game.Continue();
        }
        Assert.Equal(SeasonPhase.SeasonOver, game.Phase);
    }

    /// <summary>Saisonende; der Stürmer mit den meisten Spielen hat einen auslaufenden Vertrag, alle anderen drei Jahre.</summary>
    private static (GameState Game, Manager Manager, Club Club, Player Player) SeasonEnd(int seed = 3)
    {
        var game = TestGames.Create(seed);
        PlaySeason(game);
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        foreach (var other in club.Players)
        {
            other.ContractYears = 3;
            other.Notice = ContractNotice.None;
        }
        var p = club.Players.Where(x => x.Role == PlayerRole.Forward && !x.OnLoan).MaxBy(x => x.Games)!;
        p.ContractYears = 1;
        return (game, manager, club, p);
    }

    /// <summary>Spielerwert mit den Zählern des Originals am Saisonende: Strafen und Tore 0, Spiele beim Verein erhalten.</summary>
    private static int ValueAtSeasonEnd(GameState game, Manager manager, Club club, Player p, int clubGames, PlayerValueKind kind,
        Random random)
    {
        var inputs = PlayerValue.Inputs(p, club, null, manager.Level) with
        {
            Goals0 = 0, Penalties2 = 0, Penalties5 = 0, Games0 = clubGames,
        };
        return PlayerValue.Compute(inputs, kind, (a, b) => SquadSetup.Between(random, a, b));
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x27D1F–0x27D70: Gehalt des Angebots = Wert(Gehalt) · (147 − Alter) / 100 mit den Spielen der
    /// abgelaufenen Saison in +0x1C (Datei 0x27A0C–0x27AA8 setzt +0x1C nicht zurück).
    /// </summary>
    [Fact]
    public void SeasonEnd_OfferSalary_KeepsGamesForClub()
    {
        var (game, manager, club, p) = SeasonEnd();
        var games = p.SeasonGames(CompetitionSlot.League); // A135: +0x1C zählt nur die Liga
        Assert.True(games > 10, $"zu wenige Spiele: {games}");
        game.StartNextSeasonWithRenewals();
        Assert.True(GameState.IsRenewalPending(p));
        Assert.Equal(0, p.Games); // Saisonspiele (+0x06) zurückgesetzt
        Assert.Equal(games, p.GamesForClub);
        var value = ValueAtSeasonEnd(game, manager, club, p, games, PlayerValueKind.Salary, new Random(0));
        var without = ValueAtSeasonEnd(game, manager, club, p, 0, PlayerValueKind.Salary, new Random(0));
        // A135: +0x1C zählt nur noch Ligaspiele; bei rund 50 Spielen rundet der Gehaltsterm (Gewicht 7) auf 0.
        Assert.True(value >= without, "Spiele dürfen den Wert nicht senken");
        Assert.True(ValueAtSeasonEnd(game, manager, club, p, 400, PlayerValueKind.Salary, new Random(0)) > without,
            "Spiele müssen den Wert erhöhen");
        Assert.Equal(Math.Max(Contracts.MinSalary, value * (147 - p.Age) / 100 / 3), p.MonthlySalary);
        Assert.Equal(value, game.PlayerValueOf(p, club, PlayerValueKind.Salary, new Random(0), manager.Level));
    }

    /// <summary>[ORIGINAL] Datei 0x27DE3–0x27E1C: Ablöse = Wert(Preis) / 2 mit denselben Zählern.</summary>
    [Fact]
    public void SeasonEnd_Compensation_KeepsGamesForClub()
    {
        var (game, manager, club, p) = SeasonEnd();
        var games = p.SeasonGames(CompetitionSlot.League); // A135
        game.StartNextSeasonWithRenewals();
        // #716 R14G5-01: Spielzähler unabhängig prüfen; Gegenprobe Q43 Übertrag der Vereinsspiele beim Saisonwechsel auslassen
        // (CarryGamesForClub) → 0 statt der Ligaspiele → rot
        Assert.True(games > 10, $"zu wenige Spiele: {games}");
        Assert.Equal(games, p.GamesForClub);
        // Der Preis zieht nur bei einem Angebot Zufall (0x49E1B); hier ohne Angebot.
        var value = ValueAtSeasonEnd(game, manager, club, p, games, PlayerValueKind.Price, new Random(0));
        var outcome = game.CancelRenewal(manager, p)!;
        Assert.False(outcome.Stays);
        Assert.Equal((long)(value / 2) / 3, outcome.Compensation);
    }

    /// <summary>
    /// Spiele beim jetzigen Verein (+0x1C) laufen über Saisonwechsel weiter und beginnen bei einem Vereinswechsel neu
    /// (neuer Kaderplatz, Datei 0x42CB1). Mehrsaisonprobe über vier Wechsel, passiver und aktiver Manager.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GamesForClub_AccumulateOverFourSeasonChanges(bool active)
    {
        var game = TestGames.Create(7);
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        var p = club.Players.First(x => x.Role == PlayerRole.Forward && !x.OnLoan);
        var expected = 0;
        for (var season = 0; season < 4; season++)
        {
            foreach (var other in club.Players)
                other.ContractYears = Math.Max(other.ContractYears, 3);
            PlaySeason(game);
            expected += p.SeasonGames(CompetitionSlot.League); // A135: +0x1C zählt nur die Liga
            Assert.Equal(expected, p.GamesForClub);
            if (active && season == 1)
            {
                // Verkauf an einen Computerverein: neuer Kaderplatz, Spiele beim Verein beginnen bei 0.
                var buyer = game.Clubs.First(c => c.League == League.Bundesliga && c.Index != club.Index
                    && game.Managers.All(m => m.ClubIndex != c.Index));
                club.Players.Remove(p);
                club.Lineup.Remove(p);
                PlayerFactory.Join(buyer, p, 2, loan: false);
                Assert.Equal(0, p.GamesForClub);
                expected = 0;
                club = buyer;
            }
            game.StartNextSeason();
            Assert.Equal(0, p.Games);
            Assert.Equal(expected, p.GamesForClub);
            Assert.Equal(Math.Min(expected, 0xFFFF), PlayerValue.Inputs(p, club).Games0);
        }
        Assert.True(expected > 0);
    }

    /// <summary>Nach einem Wechsel im Lauf der Saison zählen nur die Spiele beim neuen Verein (+0x1C = 0 nach dem Kauf, FILE07).</summary>
    [Fact]
    public void GamesForClub_StartsAtZeroAfterTransfer()
    {
        var game = TestGames.Create();
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        var p = club.Players.First(x => x.Role == PlayerRole.Forward);
        p.Games = 12;
        Assert.Equal(12, p.GamesForClub);
        var other = game.Clubs.First(c => c.Index != club.Index && game.Managers.All(m => m.ClubIndex != c.Index));
        club.Players.Remove(p);
        PlayerFactory.Join(other, p, 2, loan: false);
        Assert.Equal(12, p.Games); // Spalte SP behält die Spiele beim alten Verein (+0x06, FILE07)
        Assert.Equal(0, p.GamesForClub);
        Assert.Equal(0, PlayerValue.Inputs(p, other).Games0);
        p.Games = 15;
        Assert.Equal(3, p.GamesForClub);
    }

    /// <summary>Der Übertrag der Spiele beim Verein übersteht Sichern und Laden; Spielstände vor Schema 30 bekommen 0.</summary>
    [Fact]
    public void GamesForClub_RoundTripsAndMigrates()
    {
        var game = TestGames.Create();
        PlaySeason(game);
        game.StartNextSeason();
        var p = game.Clubs[game.ActiveManager.ClubIndex].Players.First(x => x.GamesForClub > 0);
        var saved = SaveGame.Save(game);
        var loaded = SaveGame.Load(saved);
        Assert.True(loaded.Success, loaded.Error);
        var again = loaded.Game!.Clubs[game.ActiveManager.ClubIndex].Players.First(x => x.Id == p.Id);
        Assert.Equal(p.GamesForClub, again.GamesForClub);
        Assert.Equal(saved, SaveGame.Save(loaded.Game!));

        var root = System.Text.Json.Nodes.JsonNode.Parse(saved)!.AsObject();
        root["schema"] = 29; // vor Teil 5 (Integration Runde 11: 29 → 30)
        foreach (var o in root["spieler"]!.AsArray().OfType<System.Text.Json.Nodes.JsonObject>())
            o.Remove("ClubGamesCarry");
        var old = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.True(old.Success, old.Error);
        Assert.All(old.Game!.Clubs.SelectMany(c => c.Players), x => Assert.Equal(x.Games, x.GamesForClub));
    }
}
