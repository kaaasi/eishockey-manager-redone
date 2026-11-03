using System.Text.Json.Nodes;
using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur saisonwechsel, Teil 3 (S-K6, Entscheidung Nutzer 03.11.2026 = A): Satzfolge je Liga wie die Ligamischung em.exe
/// Datei 0x35EDD – beim Spielstart (0x43A17) und im Saisonwechsel (0x5644E) 55 Tausche je Liga; die Fair-Play-Folge bei
/// Gleichstand (0x2A1E0, A194) geht danach (docs/analyse/re-runde-12.md, „S-K6“).
/// </summary>
public class SaisonwechselSatzfolgeTests
{
    private static GameState Create(int seed, params int[] clubs)
    {
        var setup = new GameSetup { Duration = GameDuration.Unbegrenzt, EuropeAndWorldCup = true };
        for (var i = 0; i < clubs.Length; i++)
            setup.Managers.Add(new ManagerSetup { Name = $"M{i}", Head = i, ClubIndex = clubs[i] });
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    private static int[] Members(GameState game, League league) =>
        game.LeagueClubs(league).Select(c => c.Index).Order().ToArray();

    /// <summary>Bis zum Bildschirm „Saisonende“ (29. April); Konto hoch, Kader gehalten (kein Abbruch durch Geld).</summary>
    private static void PlayToSeasonEnd(GameState game, bool active)
    {
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 5000, "Saisonende nicht erreicht");
            foreach (var m in game.Managers)
            {
                m.Money = 10_000_000;
                TestSeason.KeepSquad(game, m);
            }
            if (active && game.Phase == SeasonPhase.Running && !game.RegularSeasonOver)
                TestSeason.PlayMatchDay(game);
            else
                game.Continue();
        }
        Assert.Equal(SeasonPhase.SeasonOver, game.Phase);
    }

    [Fact]
    public void NewGame_EachLeagueSlotsArePermutationOfMembers_NotIndexOrder()
    {
        var game = Create(seed: 3, 5);
        foreach (var league in Enum.GetValues<League>())
            Assert.Equal(Members(game, league), game.LeagueSlots(league).Order());
        // 55 Tausche: dass alle drei Ligen in Indexfolge bleiben, ist praktisch ausgeschlossen.
        Assert.Contains(Enum.GetValues<League>(), l => !game.LeagueSlots(l).SequenceEqual(Members(game, l)));
    }

    [Fact]
    public void Mixing_IsDeterministic_AndDoesNotAdvanceGameRandom()
    {
        var a = Create(seed: 11, 5);
        var b = Create(seed: 11, 5);
        Assert.Equal(a.LeagueSlots(League.Oberliga), b.LeagueSlots(League.Oberliga));
        Assert.Equal(a.Random.Capture(), b.Random.Capture());
        // Andere Seeds mischen anders (nicht nur nach den Einstellungen).
        var c = Create(seed: 12, 5);
        Assert.Contains(Enum.GetValues<League>(), l => !a.LeagueSlots(l).SequenceEqual(c.LeagueSlots(l)));
    }

    [Fact]
    public void FairPlayTie_AtTop_WinnerIsEarlierSlot_NotSmallerIndex()
    {
        // Roter Test für den alten Stand: FairPlayTable sortierte nach Vereinsindex.
        var game = Create(seed: 3, 5);
        var slots = game.LeagueSlots(League.Bundesliga);
        foreach (var club in game.LeagueClubs(League.Bundesliga))
            club.LeaguePenaltyMinutes = 200;
        // Zwei Vereine gleichauf vorn: der mit dem größeren Index steht in der Satzfolge vorn.
        var (first, second) = Enumerable.Range(0, slots.Count).SelectMany(i => Enumerable.Range(i + 1, slots.Count - i - 1), (i, j) => (slots[i], slots[j]))
            .First(p => p.Item1 > p.Item2);
        game.Clubs[first].LeaguePenaltyMinutes = 50;
        game.Clubs[second].LeaguePenaltyMinutes = 50;

        Assert.Equal(first, game.FairPlayTable(League.Bundesliga)[0].Club.Index);
    }

    [Theory]
    [InlineData(false)] // passiver Manager (Continue)
    [InlineData(true)]  // aktiver Manager (Spieltag für Spieltag)
    public void MultiSeason_SlotsRemixedEachSeasonChange_MembersFollowPromotion(bool active)
    {
        // Mehrsaisonprobe (AGENTS.md §5): 4 Saisonwechsel.
        var game = Create(seed: 21, 5, 30);
        var changed = 0;
        for (var season = 0; season < 4; season++)
        {
            PlayToSeasonEnd(game, active);
            var before = Enum.GetValues<League>().ToDictionary(l => l, l => game.LeagueSlots(l).ToArray());
            // Saisonende-Fenster ist offen: die Fair-Play-Prämie sieht die Folge der abgelaufenen Saison ([APPROX] Zeitpunkt).
            Assert.Equal(before[League.Bundesliga], game.LeagueSlots(League.Bundesliga));
            game.StartNextSeason();
            foreach (var league in Enum.GetValues<League>())
            {
                var slots = game.LeagueSlots(league);
                Assert.Equal(GameState.ClubsPerLeague, slots.Count);
                Assert.Equal(Members(game, league), slots.Order());
                if (!slots.SequenceEqual(before[league]))
                    changed++;
            }
        }
        Assert.True(changed >= 10, $"Satzfolge nur {changed}-mal von 12 neu gemischt");
    }

    [Fact]
    public void SaveAndLoad_KeepsSlots_ByteIdentical()
    {
        var game = Create(seed: 5, 5);
        TestSeason.PlayMatchDay(game);
        var bytes = SaveGame.Save(game);
        var loaded = SaveGame.Load(bytes);
        Assert.True(loaded.Success, loaded.Error);
        foreach (var league in Enum.GetValues<League>())
            Assert.Equal(game.LeagueSlots(league), loaded.Game!.LeagueSlots(league));
        Assert.Equal(bytes, SaveGame.Save(loaded.Game!));
    }

    [Fact]
    public void Schema32Save_WithoutSlots_LoadsWithIndexOrder_MixedAtNextSeasonChange()
    {
        var game = Create(seed: 7, 5);
        TestSeason.PlayMatchDay(game);
        var root = JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        root["schema"] = 32;
        Assert.True(root["spiel"]!.AsObject().Remove("_leagueSlots"));
        var loaded = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.True(loaded.Success, loaded.Error);
        var old = loaded.Game!;
        foreach (var league in Enum.GetValues<League>())
            Assert.Equal(Members(old, league), old.LeagueSlots(league));

        PlayToSeasonEnd(old, active: false);
        old.StartNextSeason();
        Assert.Contains(Enum.GetValues<League>(), l => !old.LeagueSlots(l).SequenceEqual(Members(old, l)));
    }

    [Fact]
    public void FairPlayTable_IsReadOnly()
    {
        var game = Create(seed: 3, 5);
        TestSeason.PlayMatchDay(game);
        var before = SaveGame.Save(game);
        _ = game.FairPlayTable(League.ZweiteLiga);
        _ = game.LeagueSlots(League.Oberliga);
        Assert.Equal(before, SaveGame.Save(game));
    }
}
