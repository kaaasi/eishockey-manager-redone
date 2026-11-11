using System.Text.Json.Nodes;
using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur saison, Teil 3 (S-K6-Rest, docs/analyse/re-runde-13.md; Entscheidung Leitstelle L5): Die Satzfolge der nächsten Saison
/// entsteht am 29. April vor dem Fenster „Saisonende“ (em.exe Datei 0x5644E vor 0x5645D) und wird gespeichert (Schema 38);
/// die Fair-Play-Prämie wertet die Liga der nächsten Saison in dieser Folge (Datei 0x2A1E0 über DGROUP 0x9746, Datei 0x56409).
/// </summary>
public class SaisonSatzfolgeSaisonendeTests
{
    private static GameState Create(int seed, params int[] clubs)
    {
        var setup = new GameSetup { Duration = GameDuration.Unbegrenzt, EuropeAndWorldCup = true };
        for (var i = 0; i < clubs.Length; i++)
            setup.Managers.Add(new ManagerSetup { Name = $"M{i}", Head = i, ClubIndex = clubs[i], StartLeague = (League)(clubs[0] / 12) }); // kader-start U15-a (L13)
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    private static void Hold(GameState game)
    {
        foreach (var m in game.Managers)
        {
            m.Money = 10_000_000;
            TestSeason.KeepSquad(game, m);
        }
    }

    /// <summary>Bis zum 14. April (Auf- und Abstieg stehen fest, <see cref="GameState.CurrentRecord"/> gesetzt).</summary>
    private static void PlayToClubSeasonEnd(GameState game, bool active = false)
    {
        for (var guard = 0; game.Phase == SeasonPhase.Running; guard++)
        {
            Assert.True(guard < 5000, "Ende der Vereinssaison nicht erreicht");
            Hold(game);
            if (active && !game.RegularSeasonOver)
                TestSeason.PlayMatchDay(game);
            else
                game.Continue();
        }
        Assert.Equal(SeasonPhase.ClubSeasonOver, game.Phase);
    }

    /// <summary>Vom 14. bis zum 29. April (Saisonende, Prämien).</summary>
    private static void PlayToSeasonEnd(GameState game)
    {
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 5000, "Saisonende nicht erreicht");
            Hold(game);
            game.Continue();
        }
        Assert.Equal(SeasonPhase.SeasonOver, game.Phase);
    }

    private static int[] NextMembers(GameState game, League league) =>
        game.Clubs.Where(c => c.Index < NewGame.SelectableClubs && game.CurrentRecord!.NextLeague(c.Index, c.League) == league)
            .Select(c => c.Index).Order().ToArray();

    private static ManagerSeasonResult Result(GameState game, int manager) =>
        game.History[^1].Managers.Single(r => r.Manager == manager);

    [Theory]
    [InlineData(false)] // passiver Manager (Continue)
    [InlineData(true)]  // aktiver Manager (Spieltag für Spieltag)
    public void MultiSeason_NextSlotsFormedOn29April_StartNextSeasonAdoptsThemUnchanged(bool active)
    {
        // Roter Test für den alten Stand: Die Folge entstand erst in StartNextSeason, am 29. April gab es keine.
        // Mehrsaisonprobe (AGENTS.md §5): 4 Saisonwechsel.
        var game = Create(seed: 21, 5, 30);
        for (var season = 0; season < 4; season++)
        {
            PlayToClubSeasonEnd(game, active);
            foreach (var league in Enum.GetValues<League>())
                Assert.Empty(game.NextLeagueSlots(league)); // vor dem 29. April keine Folge der nächsten Saison
            PlayToSeasonEnd(game);
            var next = Enum.GetValues<League>().ToDictionary(l => l, l => game.NextLeagueSlots(l).ToArray());
            foreach (var league in Enum.GetValues<League>())
            {
                Assert.Equal(GameState.ClubsPerLeague, next[league].Length);
                Assert.Equal(NextMembers(game, league), next[league].Order());
            }
            // R13IS-01 (#591): Zufall zwischen 29. April und Saisonstart; ohne Übernahme der Folge würde sie neu gemischt (anderer Seed).
            game.Random.Next(1_000_000);
            var random = game.Random.Capture();
            game.StartNextSeason();
            foreach (var league in Enum.GetValues<League>())
            {
                Assert.Equal(next[league], game.LeagueSlots(league));
                Assert.Empty(game.NextLeagueSlots(league));
            }
            Assert.NotEqual(random, game.Random.Capture()); // Spielpläne ziehen weiter Zufall, die Mischung nicht (siehe unten)
        }
    }

    /// <summary>
    /// Die Bildung am 29. April zieht keinen Zufall (Startwert aus dem Zufallszustand, ohne Zug). #620 (R13GN-03): Der frühere
    /// Vergleich zweier gleich gesäter Spiele blieb auch mit einer Mischung aus <c>GameState.Random</c> grün. Jetzt wird die
    /// Bildung am Saisonende noch einmal direkt aufgerufen: Zufallszustand und Folge bleiben gleich.
    /// </summary>
    [Fact]
    public void Preparing_NextSlots_DoesNotAdvanceGameRandom()
    {
        var game = Create(seed: 9, 5);
        PlayToSeasonEnd(game);
        var random = game.Random.Capture();
        var slots = Enum.GetValues<League>().ToDictionary(l => l, l => game.NextLeagueSlots(l).ToArray());
        Assert.All(slots.Values, s => Assert.Equal(GameState.ClubsPerLeague, s.Length));
        typeof(GameState).GetMethod("PrepareNextLeagueSlots",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(game, [game.History[^1]]);
        Assert.Equal(random, game.Random.Capture());
        foreach (var league in Enum.GetValues<League>())
            Assert.Equal(slots[league], game.NextLeagueSlots(league));
    }

    /// <summary>
    /// Hauptrunde mit Siegen von <paramref name="club"/>, Aufstiegsrunde Bundesliga/2. Liga mit ihm vorn, bis 14. April
    /// (Aufbau wie SaisonwechselAustauschStandTests). Nur Testaufbau.
    /// </summary>
    private static void PlayToClubSeasonEndPromoting(GameState game, int club)
    {
        for (var guard = 0; game.Phase == SeasonPhase.Running; guard++)
        {
            Assert.True(guard < 2000, "Ende der Vereinssaison nicht erreicht");
            Hold(game);
            if (game.RegularSeasonOver)
                game.Continue();
            else
                TestSeason.PlayMatchDay(game);
            for (var i = 0; i < game.Results.Count; i++)
                if (game.Results[i] is var r && r.Involves(club))
                    game.Results[i] = r.Home == club ? r with { HomeGoals = 9, AwayGoals = 0 } : r with { HomeGoals = 0, AwayGoals = 9 };
            foreach (var group in game.RelegationGroups.Where(g => g.Upper == League.Bundesliga))
                for (var i = 0; i < group.Results.Count; i++)
                    if (group.Results[i] is var r && r.Involves(club))
                        group.Results[i] = r.Home == club ? r with { HomeGoals = 9, AwayGoals = 0 } : r with { HomeGoals = 0, AwayGoals = 9 };
        }
        Assert.Equal(SeasonPhase.ClubSeasonOver, game.Phase);
    }

    [Theory]
    [InlineData(true)]  // Bundesliga hat einen Verein mit weniger Minuten → keine Prämie (alter Stand: Prämie)
    [InlineData(false)] // alte 2. Liga hat einen Verein mit weniger Minuten → Prämie (alter Stand: keine)
    public void FairPlay_Promoted_ComparedWithNewLeague_NotOldLeague(bool betterInBundesliga)
    {
        const int club = 12, manager = 0;
        var game = Create(seed: 4, club);
        PlayToClubSeasonEndPromoting(game, club);
        var record = game.CurrentRecord!;
        Assert.Equal(League.Bundesliga, record.NextLeague(club, League.ZweiteLiga)); // Vorbedingung Testaufbau
        foreach (var c in game.Clubs)
            c.LeaguePenaltyMinutes = 50;
        game.Clubs[club].LeaguePenaltyMinutes = 20;
        var other = betterInBundesliga
            ? game.LeagueClubs(League.Bundesliga).First(c => record.NextLeague(c.Index, c.League) == League.Bundesliga)
            : game.LeagueClubs(League.ZweiteLiga).First(c => c.Index != club && record.NextLeague(c.Index, c.League) == League.ZweiteLiga);
        other.LeaguePenaltyMinutes = 10;

        PlayToSeasonEnd(game);

        Assert.Equal(betterInBundesliga ? other.Index : club, game.FairPlayPremiumWinner(record, club));
        Assert.Equal(!betterInBundesliga, Result(game, manager).Events.Contains(SeasonEvent.FairPlay));
        Assert.Contains(SeasonEvent.Promotion, Result(game, manager).Events);
    }

    /// <summary>Bis 14. April; der Verein gewinnt jedes zweite Spiel (Mittelfeld: weder Aufstiegsrunde noch Austausch). Nur Testaufbau.</summary>
    private static void PlayToClubSeasonEndMidTable(GameState game, int club)
    {
        for (var guard = 0; game.Phase == SeasonPhase.Running; guard++)
        {
            Assert.True(guard < 2000, "Ende der Vereinssaison nicht erreicht");
            Hold(game);
            if (game.RegularSeasonOver)
                game.Continue();
            else
                TestSeason.PlayMatchDay(game);
            var n = 0;
            for (var i = 0; i < game.Results.Count; i++)
                if (game.Results[i] is var r && r.Involves(club))
                    game.Results[i] = (r.Home == club) == (n++ % 2 == 0)
                        ? r with { HomeGoals = 5, AwayGoals = 0 }
                        : r with { HomeGoals = 0, AwayGoals = 5 };
        }
        Assert.Equal(SeasonPhase.ClubSeasonOver, game.Phase);
    }

    [Fact]
    public void FairPlay_OberligaManagerWithZeroMinutes_WinsOnlyIfSlotBeforeBothNewcomers()
    {
        // Nachrücker aus den ligalosen Vereinen haben immer 0 Minuten (re-runde-13.md, S-K6-c). Alter Stand: Der einzige
        // Verein mit 0 Minuten der alten Oberliga bekam die Prämie immer.
        var outcomes = new HashSet<bool>();
        for (var seed = 1; seed < 40 && outcomes.Count < 2; seed++)
        {
            var game = Create(seed, 26);
            PlayToClubSeasonEndMidTable(game, 26);
            var record = game.CurrentRecord!;
            // Nur ein Verein, der regulär in der Oberliga bleibt (nicht als Letzter gehalten, SeasonFlow.Oberliga.cs).
            if (record.NextLeague(26, League.Oberliga) != League.Oberliga || record.PromotedInto[League.Oberliga].Contains(26))
                continue;
            foreach (var c in game.Clubs)
                c.LeaguePenaltyMinutes = c.League is null ? 0 : 30; // ligalose Vereine spielen keine Liga
            game.Clubs[26].LeaguePenaltyMinutes = 0;
            Assert.All(record.PromotedInto[League.Oberliga], c => Assert.Equal(0, game.Clubs[c].LeaguePenaltyMinutes));

            PlayToSeasonEnd(game);

            var slots = game.NextLeagueSlots(League.Oberliga).ToList();
            var first = slots.IndexOf(26) < record.PromotedInto[League.Oberliga].Min(slots.IndexOf);
            Assert.Equal(first, Result(game, 0).Events.Contains(SeasonEvent.FairPlay));
            outcomes.Add(first);
        }
        Assert.Equal(2, outcomes.Count); // beide Fälle kamen vor
    }

    [Fact]
    public void SaveAndLoad_InSeasonOver_KeepsNextSlots_ByteIdentical_SameSeasonAfterwards()
    {
        var game = Create(seed: 5, 5);
        PlayToSeasonEnd(game);
        var bytes = SaveGame.Save(game);
        var loaded = SaveGame.Load(bytes);
        Assert.True(loaded.Success, loaded.Error);
        var copy = loaded.Game!;
        Assert.Equal(bytes, SaveGame.Save(copy));
        foreach (var league in Enum.GetValues<League>())
            Assert.Equal(game.NextLeagueSlots(league), copy.NextLeagueSlots(league));
        game.StartNextSeason();
        copy.StartNextSeason();
        foreach (var league in Enum.GetValues<League>())
            Assert.Equal(game.LeagueSlots(league), copy.LeagueSlots(league));
    }

    [Fact]
    public void Schema37Save_InSeasonOver_WithoutNextSlots_Migrates_StartNextSeasonFormsSlots()
    {
        // Weg 37 → 38 (SaveGame.Migration.Saison.cs): Spielstand vom Bildschirm „Saisonende“ ohne vorab gebildete Folge.
        var game = Create(seed: 7, 5);
        PlayToSeasonEnd(game);
        var root = JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        root["schema"] = 37;
        Assert.True(root["spiel"]!.AsObject().Remove("_nextLeagueSlots"));
        var loaded = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.True(loaded.Success, loaded.Error);
        var old = loaded.Game!;
        foreach (var league in Enum.GetValues<League>())
            Assert.Empty(old.NextLeagueSlots(league));

        var before = Enum.GetValues<League>().ToDictionary(l => l, l => old.LeagueSlots(l).ToList());
        old.StartNextSeason();
        // #620 (R13GN-03): Der Rückfallweg (AdvanceLeagueSlots ohne gebildete Folge) mischt nach dem Bilden neu (MixLeagueSlots).
        // Ohne Mischen behielte jeder in seiner Liga gebliebene Verein seinen Satzplatz (Nachfolger übernehmen nur die Plätze ihrer
        // Tauschpartner). Gegenprobe: MixLeagueSlots() im Rückfallweg ausgelassen → rot.
        Assert.Contains(Enum.GetValues<League>(), l =>
        {
            var now = old.LeagueSlots(l).ToList();
            return before[l].Where(now.Contains).Any(c => before[l].IndexOf(c) != now.IndexOf(c));
        });
        foreach (var league in Enum.GetValues<League>())
        {
            Assert.Equal(GameState.ClubsPerLeague, old.LeagueSlots(league).Count);
            Assert.Equal(old.LeagueClubs(league).Select(c => c.Index).Order(), old.LeagueSlots(league).Order());
        }
        Assert.Contains(Enum.GetValues<League>(), l => !old.LeagueSlots(l).SequenceEqual(old.LeagueClubs(l).Select(c => c.Index).Order()));
    }

    [Fact]
    public void NextLeagueSlots_AndFairPlayWinner_AreReadOnly()
    {
        var game = Create(seed: 3, 5);
        PlayToSeasonEnd(game);
        var before = SaveGame.Save(game);
        _ = game.NextLeagueSlots(League.Oberliga);
        _ = game.FairPlayPremiumWinner(game.CurrentRecord!, 5);
        Assert.Equal(before, SaveGame.Save(game));
    }

    [Fact]
    public void FairPlayWinner_WithoutFormedSlots_FallsBackToIndexOrder()
    {
        // R13IS-02 (#592), rot vor dem Fix: ohne gespeicherte Folge (Schema-37-Stand) lieferte FairPlayPremiumWinner null.
        const int club = 5;
        var game = Create(seed: 7, club);
        PlayToSeasonEnd(game);
        var root = JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        root["schema"] = 37;
        Assert.True(root["spiel"]!.AsObject().Remove("_nextLeagueSlots"));
        var loaded = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.True(loaded.Success, loaded.Error);
        var old = loaded.Game!;
        var record = old.CurrentRecord!;
        foreach (var c in old.Clubs)
            c.LeaguePenaltyMinutes = 50;
        old.Clubs[club].LeaguePenaltyMinutes = 10;

        Assert.Equal(club, old.FairPlayPremiumWinner(record, club));
        var other = old.Clubs.First(c => c.Index != club && record.NextLeague(c.Index, c.League) == record.NextLeague(club, old.Clubs[club].League));
        other.LeaguePenaltyMinutes = 10; // Gleichstand → erster der Indexfolge
        Assert.Equal(Math.Min(club, other.Index), old.FairPlayPremiumWinner(record, club));
    }
}
