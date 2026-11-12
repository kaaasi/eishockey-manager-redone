using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur bundestrainer-modus4: Bundestrainer im Spielmodus 4 (Liga + Weltkugel).
/// Befund vorher: Im Modus 4 gab es nie einen Bundestrainer; Deutschland spielte die WM immer mit ausgewürfelten Werten.
/// [ORIGINAL] em.exe: Am Tag mit Markierung 255 (13. April) bestimmt das Original ohne Bundestrainer den Manager,
/// dessen Verein unter den ersten fünf der 1. Liga am höchsten steht (Datei 0x553A6 → 0x34769, Tabelle DGROUP 0x5CCB),
/// und ernennt ihn samt Etat (0x55436 ff.); Modus 2 ernennt nie (0x55436). An WM-Tagen folgt ein eigener Zug
/// „Manager 5“ mit dem Menü des Bundestrainers (0x55587–0x555D6).
/// </summary>
public class NationalCoachMode4Tests
{
    /// <summary>Spielt bis nach dem 13. April (Halt am 14. April, Vereinssaison vorbei).</summary>
    internal static void PlayToClubSeasonEnd(GameState game)
    {
        for (var guard = 0; game.CurrentDay <= SeasonCalendar.ClubSeasonEnd; guard++)
        {
            Assert.True(guard < 3000, "13. April nicht erreicht");
            game.Continue();
        }
        Assert.Equal(SeasonCalendar.ClubSeasonEnd + 1, game.CurrentDay);
    }

    /// <summary>Erwarteter Bundestrainer: Manager des höchsten Vereins unter den ersten fünf der 1. Liga.</summary>
    private static int? ExpectedCoach(GameState game)
    {
        foreach (var club in game.FinalTables()[League.Bundesliga].Take(5))
            for (var i = 0; i < game.Managers.Count; i++)
                if (!game.Managers[i].Dismissed && game.Managers[i].ClubIndex == club)
                    return i;
        return null;
    }

    [Fact]
    public void Mode4_ManagerInTopFive_BecomesNationalCoach_OnClubSeasonEnd()
    {
        var game = GameModeTests.Create(3, true, true, 8, 2);
        // Vorbedingung Tabellenplatz; Teil 3 (Verletzungen 0x38B8F) schwächt den Kader. Teil 5 (Penaltyschießen): nur Verein 2
        // (Manager 1) stark, sonst kann Verein 8 vor ihm landen.
        ManagedSquads.PlayAtTopStrength(game, 1);
        PlayToClubSeasonEnd(game);

        Assert.Equal(1, ExpectedCoach(game)); // Vorbedingung: Verein 2 unter den ersten fünf, Verein 8 nicht
        var coach = Assert.IsType<NationalCoach>(game.NationalCoach);
        Assert.Equal(1, coach.Manager);
        Assert.Equal(NationalTeams.Germany, coach.Nation);
        Assert.Empty(coach.Squad);
        // Etat aufs Nationalkonto, nicht aufs Vereinskonto (Datei 0x554A6)
        Assert.Equal(NationalCoach.FirstBudget, game.NationalAccount);
        Assert.Equal([new NationalBooking(game.Season, NationalBookingKind.Budget, NationalCoach.FirstBudget)], game.NationalBookings);
        Assert.Equal(GameMode.Full, game.Mode);
        Assert.False(game.WorldOnly);
    }

    [Fact]
    public void Mode4_TwoManagersInTopFive_HigherPlacedBecomesCoach()
    {
        // Runde 17, Spur wettbewerbe-rest Teil 3 (A113/A142, R15-E2 = C): 71 → 14 – Liga-Spielplan über die Satzfolge (Datei 0x4E7C7) ändert alle Paarungen; unter den Saaten 1–40 hat
        // nur 14 beide Vereine unter den ersten fünf. Rückschaltprobe: alter Code mit Saat 14 ebenfalls grün – die Probe hängt an der Saat.
        var game = GameModeTests.Create(14, true, true, 2, 1); // kader-start U15-a/R-K5 (L13): Saat 3 → 17; #675 (Tageszähler 0x3B7, anderer Zufallsverlauf): 17 → 56; #678 (Tagestakt 0x07169): 56 → 71, Vorbedingung beide unter den ersten fünf
        ManagedSquads.PlayAtDatabaseStrength(game); // Vorbedingung Tabellenplatz (spielstaerke-manager-2, 0x98F8)
        PlayToClubSeasonEnd(game);
        var top = game.FinalTables()[League.Bundesliga].Take(5).ToList();
        // #604 (R13KI-02): Vorbedingung ausdrücklich – beide Managervereine (1 und 2) unter den ersten fünf.
        Assert.True(top.Contains(1) && top.Contains(2), $"Vorbedingung verletzt: erste fünf {string.Join(", ", top)}");
        var expected = top.IndexOf(1) < top.IndexOf(2) ? 1 : 0;
        Assert.Equal(expected, game.NationalCoach?.Manager);
    }

    [Fact]
    public void Mode4_NoManagerInTopFive_NoCoach()
    {
        var game = GameModeTests.Create(3, true, true, 8, 20); // #621 (R13GN-04): Saat wieder 3 – die Änderung 3 → 4 (L13) war nicht nötig
        ManagedSquads.PlayAtDatabaseStrength(game); // Vorbedingung Tabellenplatz (spielstaerke-manager-2, 0x98F8)
        PlayToClubSeasonEnd(game);
        Assert.Null(ExpectedCoach(game));
        Assert.Null(game.NationalCoach);
        Assert.Empty(game.NationalBookings);
    }

    [Fact]
    public void Mode2_ManagerInTopFive_NeverCoach()
    {
        var game = GameModeTests.Create(22, true, false, 2); // kader-start U15-a/R-K5 (L13): Saat 3 → 22, Vorbedingung nach dem Starttausch (Teamwerte bleiben am Platz)
        ManagedSquads.PlayAtDatabaseStrength(game); // Vorbedingung Tabellenplatz (spielstaerke-manager-2, 0x98F8)
        PlayToClubSeasonEnd(game);
        Assert.NotNull(ExpectedCoach(game));
        Assert.Null(game.NationalCoach);
    }

    [Fact]
    public void Mode4_Appointment_BeforeReroll_GermanyKeepsBaseRatings()
    {
        // [ORIGINAL] Datei 0x554CF: Ausgewürfelt wird nur, wenn nach der Ernennung kein Bundestrainer da ist.
        var game = GameModeTests.Create(22, true, true, 2); // kader-start U15-a/R-K5 (L13): Saat 3 → 2; #675 (Tageszähler 0x3B7, anderer Zufallsverlauf): 2 → 22, Vorbedingung Verein 2 unter den ersten fünf
        ManagedSquads.PlayAtDatabaseStrength(game); // Vorbedingung Tabellenplatz (spielstaerke-manager-2, 0x98F8)
        PlayToClubSeasonEnd(game);
        Assert.NotNull(game.NationalCoach);
        Assert.Equal(NationalTeamRatings.Of(NationalTeams.Germany), game.NationalRatings(NationalTeams.Germany));

        var without = GameModeTests.Create(3, true, true, 20);

        ManagedSquads.PlayAtDatabaseStrength(without); // Vorbedingung Tabellenplatz (spielstaerke-manager-2, 0x98F8)
        PlayToClubSeasonEnd(without);
        Assert.Null(without.NationalCoach);
        Assert.NotEqual(NationalTeamRatings.Of(NationalTeams.Germany), without.NationalRatings(NationalTeams.Germany));
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x55587–0x555D6: An WM-Tagen folgt nach den Zügen der Vereinsmanager der Zug „Manager 5“
    /// (DGROUP 0xA70E = 5) mit dem Menü des Bundestrainers; erst danach wird der Tag gespielt.
    /// </summary>
    [Fact]
    public void Mode4_WorldCupDay_CoachTurnAfterClubManagers()
    {
        var game = GameModeTests.Create(3, true, true, 8, 2);
        // Vorbedingung Tabellenplatz; Teil 3 (Verletzungen 0x38B8F) schwächt den Kader. Teil 5 (Penaltyschießen): nur Verein 2
        // (Manager 1) stark, sonst kann Verein 8 vor ihm landen.
        ManagedSquads.PlayAtTopStrength(game, 1);
        PlayToClubSeasonEnd(game);
        var day = game.CurrentDay;
        Assert.True(SeasonCalendar.Events(day).HasFlag(DayEvents.WorldCup));
        Assert.False(game.NationalTurn);
        Assert.False(game.NationalCoachMenu);

        Assert.Equal(0, game.CurrentManager);
        game.Continue(); // M0 → M1
        Assert.Equal(1, game.CurrentManager);
        Assert.False(game.NationalTurn);
        game.Continue(); // M1 bereit → Zug des Bundestrainers, Tag noch nicht gespielt
        Assert.Equal(day, game.CurrentDay);
        Assert.True(game.NationalTurn);
        Assert.True(game.NationalCoachMenu);
        Assert.Equal(1, game.CurrentManager); // Manager 5 übernimmt den Namen des ernannten Managers (0x55475)
        // Infoleiste des Bundestrainers: WM-Gegner wie im Modus 1 (Video bundestrainer_00026 „SCHWEIZ“), nicht „Saisonende“.
        var next = Assert.NotNull(game.UpcomingMatch(NationalTeams.Germany));
        Assert.Equal(Competition.WorldCup, next.Competition);
        Assert.Null(game.UpcomingMatch(game.ActiveManager.ClubIndex));
        Assert.DoesNotContain(game.WorldCup!.Matches, m => m.Day == day);

        game.Continue(); // Bundestrainer bereit → Tag spielen
        Assert.True(game.CurrentDay > day);
        Assert.False(game.NationalTurn);
        Assert.False(game.NationalCoachMenu);
    }

    [Fact]
    public void Mode4_NationalTurn_CanNominateAndPlayFriendly()
    {
        var game = GameModeTests.Create(3, true, true, 8, 2);
        // Vorbedingung Tabellenplatz; Teil 3 (Verletzungen 0x38B8F) schwächt den Kader. Teil 5 (Penaltyschießen): nur Verein 2
        // (Manager 1) stark, sonst kann Verein 8 vor ihm landen.
        ManagedSquads.PlayAtTopStrength(game, 1);
        PlayToClubSeasonEnd(game);
        game.Continue();
        game.Continue();
        Assert.True(game.NationalTurn);
        var clubMoney = game.Managers[1].Money;
        game.AutoNominate();
        Assert.Equal(29, game.NationalCoach!.Squad.Count);
        Assert.Equal(FriendlyResult.Played, game.PlayFriendly(52, home: true));
        Assert.Equal(clubMoney, game.Managers[1].Money);
        Assert.Equal(game.NationalAccount, game.NationalBookings.Sum(b => b.Amount));
    }

    [Fact]
    public void Mode4_NewSeason_CoachEnds_AndIsChosenAgain()
    {
        var game = GameModeTests.Create(3, true, true, 8, 2);
        // Vorbedingung Tabellenplatz; Teil 3 (Verletzungen 0x38B8F) schwächt den Kader. Teil 5 (Penaltyschießen): nur Verein 2
        // (Manager 1) stark, sonst kann Verein 8 vor ihm landen.
        ManagedSquads.PlayAtTopStrength(game, 1);
        while (game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver)
            game.Continue();
        Assert.NotNull(game.NationalCoach);
        game.StartNextSeason();
        Assert.Null(game.NationalCoach);
        Assert.False(game.NationalTurn);
        PlayToClubSeasonEnd(game);
        Assert.Equal(ExpectedCoach(game), game.NationalCoach?.Manager);
    }

    // ---- Mehrsaisonprobe Modus 4 (4 Saisonwechsel, passiv/aktiv) -------------------------------------------

    /// <summary>
    /// Abnahme (3): Modus 4 über 4 Saisonwechsel. Jede Saison wird am 13. April der Manager des höchsten Vereins unter
    /// den ersten fünf der 1. Liga ernannt (oder niemand); das Amt endet zum Saisonwechsel. Der Etat folgt dem WM-Platz
    /// der Vorsaison (0x31293), das Nationalkonto ist die Summe seiner Buchungen und berührt nie das Vereinskonto.
    /// Aktiv: Der Bundestrainer nominiert in seinem Zug („auto“) und bestreitet ein Freundschaftsspiel.
    /// </summary>
    [Theory]
    [InlineData(false, 3)]
    [InlineData(true, 3)]
    [InlineData(true, 11)]
    public void Mode4_MultiSeason_PassiveAndActive(bool active, int seed)
    {
        var game = GameModeTests.Create(seed, true, true, 2, 30);
        ManagedSquads.PlayAtDatabaseStrength(game); // Vorbedingung Tabellenplatz (spielstaerke-manager-2, 0x98F8)
        // wettbewerbe-spiel, Teil 7 (+0x19 gewürfelt): Mit Datenbankstärke stand Verein 2 bei Seed 3/11 in keiner Saison mehr
        // unter den ersten fünf (60 Seeds, eine Saison: 48 statt 50 von 60, Mittel Platz 4,28 statt 4,18 – Streuung, keine
        // Schwächung). Vorbedingung deshalb wie in den Einzelfällen über Spielerwerte 99 für Manager 0.
        ManagedSquads.PlayAtTopStrength(game, 0);
        var expectedBudget = NationalCoach.FirstBudget;
        var coached = 0;
        for (var change = 0; change <= 4; change++)
        {
            var bookingsBefore = game.NationalBookings.Count;
            var nationalTurns = 0;
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 3000, "Saisonende nicht erreicht");
                foreach (var m in game.Managers)
                    m.Money = Math.Max(m.Money, 10_000_000); // keine Entlassung
                if (game.NationalTurn)
                {
                    nationalTurns++;
                    Assert.Equal(game.NationalCoach!.Manager, game.CurrentManager);
                    Assert.True(SeasonCalendar.Events(game.CurrentDay).HasFlag(DayEvents.WorldCup));
                    if (active && game.NationalCoach.Squad.Count == 0)
                    {
                        var money = game.Managers.Select(m => m.Money).ToList();
                        game.AutoNominate();
                        Assert.Equal(FriendlyResult.Played, game.PlayFriendly(53, home: false));
                        Assert.Equal(money, game.Managers.Select(m => m.Money));
                    }
                }
                if (active)
                    GameModeTests.ActiveSecondManager(game);
                game.Continue();
                if (game.CurrentDay == SeasonCalendar.ClubSeasonEnd + 1 && game.Phase == SeasonPhase.ClubSeasonOver
                    && game.NationalBookings.Count == bookingsBefore + 1)
                {
                    // Ernennung am 13. April mit dem Etat aus der Vorsaison
                    Assert.Equal(new NationalBooking(game.Season, NationalBookingKind.Budget, expectedBudget), game.NationalBookings[^1]);
                }
            }
            Assert.Equal(SeasonPhase.SeasonOver, game.Phase);
            var expectedCoach = ExpectedCoachAtSeasonEnd(game);
            Assert.Equal(expectedCoach, game.NationalCoach?.Manager);
            if (expectedCoach is not null)
            {
                coached++;
                Assert.True(nationalTurns > 0, "Bundestrainer ohne eigenen Zug");
            }
            else
                Assert.Equal(0, nationalTurns);
            Assert.Equal(game.NationalAccount, game.NationalBookings.Sum(b => b.Amount));
            expectedBudget = NationalCoach.BudgetFor(game.WorldCup!.Ranking.ToList().IndexOf(NationalTeams.Germany) + 1);
            if (change < 4)
            {
                game.StartNextSeason();
                Assert.Null(game.NationalCoach);
                Assert.False(game.NationalTurn);
            }
        }
        Assert.Equal(5, game.History.Count);
        Assert.True(coached > 0, "Probe ohne Bundestrainer – Vorbedingung verfehlt");
        if (active)
            Assert.Contains(game.NationalBookings, b => b.Kind == NationalBookingKind.Friendly);
        else
            Assert.DoesNotContain(game.NationalBookings, b => b.Kind == NationalBookingKind.Friendly);
    }

    /// <summary>Wie <see cref="ExpectedCoach"/>, aber aus der Abschlusstabelle der gerade beendeten Saison.</summary>
    private static int? ExpectedCoachAtSeasonEnd(GameState game)
    {
        foreach (var club in game.History[^1].FinalTables[League.Bundesliga].Take(5))
            for (var i = 0; i < game.Managers.Count; i++)
                if (!game.Managers[i].Dismissed && game.Managers[i].ClubIndex == club)
                    return i;
        return null;
    }

    // ---- Spielstand ------------------------------------------------------------------------------------------

    [Fact]
    public void SaveLoad_DuringNationalTurn_KeepsTurnAndCoach()
    {
        var game = GameModeTests.Create(3, true, true, 8, 2);
        // Vorbedingung Tabellenplatz; Teil 3 (Verletzungen 0x38B8F) schwächt den Kader. Teil 5 (Penaltyschießen): nur Verein 2
        // (Manager 1) stark, sonst kann Verein 8 vor ihm landen.
        ManagedSquads.PlayAtTopStrength(game, 1);
        PlayToClubSeasonEnd(game);
        game.Continue();
        game.Continue();
        Assert.True(game.NationalTurn);
        var bytes = SaveGame.Save(game);
        var loaded = SaveGame.Load(bytes).Game!;
        Assert.True(loaded.NationalTurn);
        Assert.True(loaded.NationalCoachMenu);
        Assert.Equal(1, loaded.CurrentManager);
        Assert.Equal(game.NationalBookings, loaded.NationalBookings);
        Assert.Equal(bytes, SaveGame.Save(loaded));

        // Beide spielen denselben Tag gleich weiter.
        game.Continue();
        loaded.Continue();
        Assert.False(loaded.NationalTurn);
        Assert.Equal(game.CurrentDay, loaded.CurrentDay);
        Assert.Equal(SaveGame.Save(game), SaveGame.Save(loaded));
    }

    /// <summary>Abnahme (4): Schema 5 → 6 (Integration Runde 5, vorher 4 → 5) – alte Spielstände bekommen den Zug des Bundestrainers als „nicht offen“.</summary>
    [Fact]
    public void SchemaFourSave_IsMigrated()
    {
        var game = GameModeTests.Create(3, true, true, 8, 2);
        ManagedSquads.PlayAtDatabaseStrength(game); // Vorbedingung Tabellenplatz (spielstaerke-manager-2, 0x98F8)
        PlayToClubSeasonEnd(game);
        var root = System.Text.Json.Nodes.JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        root["schema"] = 5;
        Assert.True(root["spiel"]!.AsObject().Remove("_nationalTurn"));

        var result = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.True(result.Success, result.Error);
        Assert.False(result.Game!.NationalTurn);
        Assert.Equal(SaveGame.Save(game), SaveGame.Save(result.Game));
    }
}
