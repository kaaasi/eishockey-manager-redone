using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur saisonwechsel, Teil 2 (S-K3): Zwangsabstieg wegen Schulden wie em.exe Datei 0x55F17–0x560DB (Statusbit 0x04) und
/// Austausch der Aufstiegsrunden über die Gruppentabelle (Datei 0x56157–0x5636D); docs/analyse/re-saisonwechsel.md §2.
/// </summary>
public class SaisonwechselZwangsabstiegTests
{
    // ---------- Regel am Disassembly (Gruppentabelle, ohne Spielstand) ----------

    // Vereine 0–11 obere Liga (U…), 12–23 untere (L…), wie die Ligagrenze DGROUP 0x7C1.
    private static bool Upper(int club) => club < 12;
    private const int U1 = 10, U2 = 11, L1 = 12, L2 = 13, F = 4, F2 = 5;

    private static int[] Insert(int[] places, params int[] forced)
    {
        var table = places.ToArray();
        for (var k = 0; k < forced.Length; k++)
            GameState.InsertForcedRelegation(table, k, forced[k], Upper);
        return table;
    }

    [Theory]
    // [ORIGINAL] Datei 0x55FD0–0x56047: ab Platz 3 der nächste Verein der oberen Liga wird überschrieben …
    [InlineData(new[] { L1, L2, U1, U2 }, new[] { F }, new[] { L1, L2, F, U2 })]
    [InlineData(new[] { U1, L1, L2, U2 }, new[] { F }, new[] { U1, L1, L2, F })]
    // … gibt es keinen (q = 4), wandert Platz 3 auf Platz 1 und der Zwangsabsteiger auf Platz 3.
    [InlineData(new[] { U1, U2, L1, L2 }, new[] { F }, new[] { L1, U2, F, L2 })]
    // Zwei Zwangsabsteiger: der zweite ab Platz 4.
    [InlineData(new[] { L1, L2, U1, U2 }, new[] { F, F2 }, new[] { L1, L2, F, F2 })]
    [InlineData(new[] { U1, U2, L1, L2 }, new[] { F, F2 }, new[] { L1, L2, F, F2 })]
    // Der zweite überschreibt den ersten, wenn dieser auf Platz 4 steht (Verein der oberen Liga).
    [InlineData(new[] { U1, L1, L2, U2 }, new[] { F, F2 }, new[] { U1, L1, L2, F2 })]
    public void InsertForcedRelegation_FollowsGroupTableRule(int[] places, int[] forced, int[] expected) =>
        Assert.Equal(expected, Insert(places, forced));

    [Fact]
    public void RelegationExchange_WithoutForced_TopTwoPlayUp()
    {
        Assert.Equal(Pairs([L1, L2], [U1, U2]), Exchange([L1, L2, U1, U2]));
        Assert.Equal(Pairs([L1], [U2]), Exchange([U1, L1, L2, U2]));
        Assert.Equal(Pairs([], []), Exchange([U1, U2, L1, L2]));
    }

    private static string Exchange(int[] places)
    {
        var (up, down) = GameState.RelegationExchange(places, Upper);
        return Pairs([.. up], [.. down]);
    }

    private static string Pairs(int[] up, int[] down) => $"auf {string.Join(",", up)} / ab {string.Join(",", down)}";

    [Fact]
    public void RelegationExchange_ForcedOutsideGroup_IsRelegated_SavedClubStaysUp()
    {
        // U1 auf Platz 3 wird überschrieben und bleibt oben, F steigt ab.
        Assert.Equal(Pairs([L1, L2], [F, U2]), Exchange(Insert([L1, L2, U1, U2], F)));
        // Beide Zweitligisten verlieren: der Dritte steigt trotzdem auf, F ab, beide Erstligisten bleiben.
        Assert.Equal(Pairs([L1], [F]), Exchange(Insert([U1, U2, L1, L2], F)));
        Assert.Equal(Pairs([L1, L2], [F, F2]), Exchange(Insert([U1, U2, L1, L2], F, F2)));
    }

    [Fact]
    public void RelegationExchange_SamePlaceTwice_SwapsActOnPlaces()
    {
        // F stand schon auf Platz 4, U1 auf Platz 3 wird mit F überschrieben: Platz F kommt zweimal vor. em.exe tauscht
        // F ↔ L1, dann (Platz F) ↔ L2: F landet unten, L2 oben, L1 bleibt unten, U1 oben.
        var table = Insert([L1, L2, U1, F], F);
        Assert.Equal(new[] { L1, L2, F, F }, table);
        Assert.Equal(Pairs([L2], [F]), Exchange(table));
    }

    [Fact]
    public void RelegationExchange_SecondOverwritesFirst_FirstStaysUp_OnlyOnePair()
    {
        // U1, L1, L2, F → F2 überschreibt F; ein Aufsteiger (L1? nein: Platz 1–2 nur L1), ein Absteiger F2.
        Assert.Equal(Pairs([L1], [F2]), Exchange(Insert([U1, L1, L2, U2], F, F2)));
        // Platz 2 wird mit Platz 4 überschrieben (q = 4): L1 fehlt, L2 steigt auf, F2 bleibt mangels Partner oben.
        Assert.Equal(Pairs([L2], [F]), Exchange(Insert([U1, L1, U2, L2], F, F2)));
    }

    [Theory]
    [InlineData(-700_000, true)]
    [InlineData(-600_000, false)]
    [InlineData(-666_666, false)] // −1.999.998 Drittel
    [InlineData(-666_667, true)]  // −2.000.001 Drittel; −2.000.000 selbst zählt nicht (echt kleiner, Datei 0x55F2C)
    public void Limit_StrictlyBelowMinus2MillionThirds(long money, bool below) =>
        Assert.Equal(below, GameState.BelowForcedRelegationLimit(money));

    [Fact]
    public void Exempt_ChampionAndEuropeanCupWinner_NotCupWinner()
    {
        var record = new SeasonRecord { Season = 0, FinalTables = new Dictionary<League, IReadOnlyList<int>>() };
        record.Titles[Title.GermanChampion] = 1;
        record.Titles[Title.EuropeanCupWinner] = 2;
        record.Titles[Title.CupWinner] = 3;
        Assert.True(GameState.ExemptFromForcedRelegation(record, 1));
        Assert.True(GameState.ExemptFromForcedRelegation(record, 2));
        Assert.False(GameState.ExemptFromForcedRelegation(record, 3));
    }

    // ---------- Spielablauf ----------

    internal static GameState Create(int seed, bool europe = true, params int[] clubs)
    {
        var setup = new GameSetup { Duration = GameDuration.Unbegrenzt, EuropeAndWorldCup = europe };
        for (var i = 0; i < clubs.Length; i++)
            setup.Managers.Add(new ManagerSetup { Name = $"M{i}", Head = i, ClubIndex = clubs[i] });
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    /// <summary>
    /// Ligaergebnisse so umschreiben, dass die Vereine <paramref name="middle"/> direkt hinter den Play-Off-Plätzen stehen (Platz 9
    /// und folgende): Sie verlieren gegen die übrigen Vereine und gewinnen gegen <paramref name="bottom"/> (bzw. die letzten
    /// Vereine nach Index), die auch gegen alle übrigen verlieren. Spiele innerhalb einer Gruppe bleiben. Nur Testaufbau.
    /// </summary>
    private static void ForceRanks(GameState game, League league, int[] middle, int[] bottom)
    {
        var members = game.LeagueClubs(league).Select(c => c.Index).ToList();
        var low = bottom.Length > 0 ? bottom
            : members.Where(c => !middle.Contains(c)).OrderDescending().Take(GameState.ClubsPerLeague - 8 - middle.Length).ToArray();
        int Level(int c) => middle.Contains(c) ? 1 : low.Contains(c) ? 0 : 2;
        for (var i = 0; i < game.Results.Count; i++)
        {
            if (game.Results[i] is not { } r || r.League != league || Level(r.Home) == Level(r.Away))
                continue;
            game.Results[i] = Level(r.Home) > Level(r.Away) ? r with { HomeGoals = 10, AwayGoals = 0 } : r with { HomeGoals = 0, AwayGoals = 10 };
        }
    }

    /// <summary>
    /// Eine Saison bis „Saisonende“ (29. April). Bis zum Ende der Vereinssaison hält das Konto 10 Mio. DM (keine Entlassung);
    /// danach je „und tschüß…“ den Betrag aus <paramref name="money"/>. Bei <paramref name="middle"/> stehen diese Vereine nach
    /// der Hauptrunde außerhalb von Play-Offs und Aufstiegsrunde.
    /// </summary>
    internal static SeasonRecord PlaySeason(GameState game, Func<int, long> money, League? league = null, int[]? bottom = null,
        params int[] middle)
    {
        for (var guard = 0; !game.RegularSeasonOver; guard++)
        {
            Assert.True(guard < 2000, "Hauptrunde nicht beendet");
            foreach (var m in game.Managers)
            {
                m.Money = 10_000_000;
                TestSeason.KeepSquad(game, m);
            }
            TestSeason.PlayMatchDay(game);
            if (league is { } l)
                ForceRanks(game, l, middle, bottom ?? []);
        }
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 1000, "Saisonende nicht erreicht");
            for (var i = 0; i < game.Managers.Count; i++)
                game.Managers[i].Money = game.Phase == SeasonPhase.ClubSeasonOver ? money(i) : 10_000_000;
            game.Continue();
        }
        return game.History[^1];
    }

    private static int LeagueSize(GameState game, League league) => game.LeagueClubs(league).Count();

    [Theory]
    [InlineData(League.Bundesliga, 5)]
    [InlineData(League.ZweiteLiga, 17)]
    public void Minus700k_OutsideRelegationRound_ForcedDown_Minus600k_Stays(League league, int club)
    {
        // Roter Test für den alten Stand (S-K3 fehlte): −700.000 DM am 29. April → Abstieg, Symbol 0 statt Pfeil;
        // −600.000 DM → bleibt.
        foreach (var (money, forced) in new[] { (-700_000L, true), (-600_000L, false) })
        {
            var game = Create(seed: 41, clubs: club);
            var record = PlaySeason(game, _ => money, league, null, club);
            Assert.False(GameState.ExemptFromForcedRelegation(record, club));
            Assert.Equal(club, record.FinalTables[league][8]); // Testaufbau: weder Play-Offs noch Aufstiegsrunde

            var result = Assert.Single(record.Managers);
            Assert.Equal(forced, result.Events.Contains(SeasonEvent.ForcedRelegation));
            Assert.DoesNotContain(SeasonEvent.Relegation, result.Events); // Bit 0x04 unterdrückt den Pfeil
            Assert.Equal(forced, record.RelegatedInto[league + 1].Contains(club));
            Assert.Equal(record.PromotedInto[league].Count, record.RelegatedInto[league + 1].Count);

            game.StartNextSeason();
            Assert.Equal(forced ? league + 1 : league, game.Clubs[club].League);
            Assert.All(Enum.GetValues<League>(), l => Assert.Equal(GameState.ClubsPerLeague, LeagueSize(game, l)));
        }
    }

    [Fact]
    public void Champion_WithDebts_NotForced()
    {
        var game = Create(seed: 43, clubs: 5);
        var record = PlaySeason(game, _ => -700_000, League.Bundesliga, null, 5);
        Assert.Contains(SeasonEvent.ForcedRelegation, record.Managers[0].Events);

        // Gleicher Lauf, aber der Verein ist Meister (Statusbit 0x08): kein Zwangsabstieg.
        game = Create(seed: 43, clubs: 5);
        for (var guard = 0; game.Phase != SeasonPhase.ClubSeasonOver || game.CurrentRecord is null; guard++)
        {
            Assert.True(guard < 3000);
            foreach (var m in game.Managers)
            {
                m.Money = 10_000_000;
                TestSeason.KeepSquad(game, m);
            }
            if (!game.RegularSeasonOver)
            {
                TestSeason.PlayMatchDay(game);
                ForceRanks(game, League.Bundesliga, [5], []);
            }
            else
                game.Continue();
        }
        game.CurrentRecord!.Titles[Title.GermanChampion] = 5; // Testaufbau: Meistertitel setzen
        while (game.Phase == SeasonPhase.ClubSeasonOver)
        {
            game.Managers[0].Money = -700_000;
            game.Continue();
        }
        record = game.History[^1];
        Assert.DoesNotContain(SeasonEvent.ForcedRelegation, record.Managers[0].Events);
        Assert.DoesNotContain(5, record.RelegatedInto[League.ZweiteLiga]);
    }

    [Fact]
    public void AtMostTwoPerLeague_InManagerOrder()
    {
        // Manager 0 und 1 auf Platz 9/10 der Bundesliga, Manager 2 (Verein 6) in der Aufstiegsrunde; alle −700.000 DM.
        var game = Create(seed: 45, clubs: [3, 4, 6]);
        var record = PlaySeason(game, _ => -700_000, League.Bundesliga, [6, 7], 3, 4);
        Assert.All(new[] { 3, 4, 6 }, c => Assert.False(GameState.ExemptFromForcedRelegation(record, c)));
        Assert.Equal([3, 4], record.FinalTables[League.Bundesliga].Skip(8).Take(2).Order());

        Assert.Contains(SeasonEvent.ForcedRelegation, record.Managers[0].Events);
        Assert.Contains(SeasonEvent.ForcedRelegation, record.Managers[1].Events);
        Assert.DoesNotContain(SeasonEvent.ForcedRelegation, record.Managers[2].Events); // dritter in der Liga: Zähler voll
        Assert.Equal(record.PromotedInto[League.Bundesliga].Count, record.RelegatedInto[League.ZweiteLiga].Count);
        // Mindestens einer steigt ab; der zweite kann den ersten in der Gruppentabelle überschreiben (InsertForcedRelegation).
        Assert.Contains(record.RelegatedInto[League.ZweiteLiga], c => c is 3 or 4);
        game.StartNextSeason();
        Assert.All(Enum.GetValues<League>(), l => Assert.Equal(GameState.ClubsPerLeague, LeagueSize(game, l)));
    }

    [Fact]
    public void Oberliga_NeverForced()
    {
        var game = Create(seed: 47, clubs: 30);
        var record = PlaySeason(game, _ => -1_000_000);
        Assert.DoesNotContain(SeasonEvent.ForcedRelegation, record.Managers[0].Events);
    }

    [Fact]
    public void ForcedEvent_SurvivesSaveAndLoad()
    {
        var game = Create(seed: 41, clubs: 5);
        PlaySeason(game, _ => -700_000, League.Bundesliga, null, 5);
        var loaded = SaveGameTests.AssertRoundTrip(game);
        Assert.Contains(SeasonEvent.ForcedRelegation, loaded.History[^1].Managers[0].Events);
    }

    [Theory]
    [InlineData(true)]  // Modus 4
    [InlineData(false)] // Modus 2
    public void MultiSeason_PassiveAndActive_ForcedDownStepByStep(bool europe)
    {
        // Mehrsaisonprobe (AGENTS.md §5), 20 Saisonwechsel: aktiver Manager mit −700.000 DM steigt jede Saison zwangsweise ab,
        // bis er in der Oberliga ist (dort kein Zwangsabstieg); passiver Manager mit Guthaben bleibt unberührt. Ligagrößen
        // bleiben 12/12/12.
        var game = Create(seed: 49, europe: europe, clubs: [5, 8]);
        for (var season = 0; season < 20; season++)
        {
            var league = game.Clubs[5].League!.Value;
            var record = PlaySeason(game, i => i == 0 ? -700_000 : 2_000_000,
                league == League.Oberliga ? null : league, null, 5);
            var active = record.Managers[0];
            var expected = league != League.Oberliga && !GameState.ExemptFromForcedRelegation(record, 5);
            Assert.Equal(expected, active.Events.Contains(SeasonEvent.ForcedRelegation));
            Assert.All(record.Managers.Skip(1), r => Assert.DoesNotContain(SeasonEvent.ForcedRelegation, r.Events));
            game.StartNextSeason();
            if (expected)
                Assert.Equal(league + 1, game.Clubs[5].League);
            Assert.All(Enum.GetValues<League>(), l => Assert.Equal(GameState.ClubsPerLeague, LeagueSize(game, l)));
        }
        Assert.Equal(League.Oberliga, game.Clubs[5].League);
    }
}
