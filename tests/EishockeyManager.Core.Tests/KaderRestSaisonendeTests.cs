using System.Text.Json.Nodes;
using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur kader-rest (Runde 17), Teil 3: Ereignissymbol 9 im Fenster „Saisonende“. [ORIGINAL] Die Jugend am Saisonende läuft im
/// Fenster 0x26F20 (Datei 0x2728C–0x27320) nach dem Saisonwechsel der Jugendteams (0x3A13B, Datei 0x55889) und setzt je
/// Neuzugang das Symbol 9 (Datei 0x27302–0x2730F); Altern und Herunterzählen folgen erst danach (Datei 0x274A7, 0x27500).
/// </summary>
public class KaderRestSaisonendeTests
{
    /// <summary>Platz nach dem Saisonwechsel der Jugendteams (0x3A13B): 6, nach Aufstieg 11/12, nach Abstieg 2.</summary>
    private static readonly int[] RanksAfterYouthChange = [2, YouthTeam.StartRank, 11, 12];

    private static void PlayToSeasonEnd(GameState game)
    {
        TestSeason.PlayRegularSeason(game);
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 5000, "Saisonende nicht erreicht");
            if (game.NationalTurn && game.NationalCoach!.Squad.Count == 0)
                game.AutoNominate();
            game.Continue();
        }
    }

    private static void NextSeason(GameState game, Manager manager, bool active)
    {
        game.StartNextSeasonWithRenewals();
        foreach (var p in game.PendingRenewals(manager))
        {
            if (active)
            {
                manager.Money = Math.Max(manager.Money, 100_000_000);
                game.NegotiateRenewal(manager, p, 2, Contracts.DemandAfterYears(game, manager, p, 2) ?? 2 * p.MonthlySalary, false,
                    demandFilled: true);
            }
            else
                game.CancelRenewal(manager, p);
        }
    }

    /// <summary>
    /// Regressionstest und Mehrsaisonprobe (vier Saisonwechsel, passiv und aktiv): Auf dem Bildschirm „Saisonende“ stehen die Junioren
    /// schon im Kader, ungealtert, und das Ergebnis des Managers hat je Neuzugang ein <see cref="SeasonEvent.YouthJoined"/>; nach dem
    /// Saisonwechsel sind sie um ein Jahr gealtert. Die Jugendteams sind dann schon gewechselt (Plätze 2/6/11/12) und wechseln beim
    /// Saisonwechsel nicht ein zweites Mal. Vorher rot: Das Remake zog die Jugend erst im Saisonwechsel, kein Ereignis 9
    /// (Summe 0); die Jugendteams zeigten auf dem Bildschirm noch den Abschlussplatz.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SeasonEnd_YouthJoinedBeforeWindow_Symbol9PerNewcomer(bool active)
    {
        var total = 0;
        foreach (var seed in new[] { 3, 5, 7 })
        {
            var game = TestGames.Create(seed);
            var manager = game.Managers[0];
            var club = game.Clubs[manager.ClubIndex];
            for (var change = 0; change < 4; change++)
            {
                var known = club.Players.ToHashSet();
                PlayToSeasonEnd(game);
                var result = game.History[^1].Managers.Single(m => m.Manager == 0);
                var newcomers = club.Players.Where(p => !known.Contains(p)).ToList();
                Assert.Equal(newcomers.Count, result.Events.Count(e => e == SeasonEvent.YouthJoined));
                if (game.Season + 1 <= SeasonEndYouth.LastYearWithout)
                    Assert.Empty(newcomers);
                Assert.All(newcomers, p => Assert.InRange(p.Age, PlayerFactory.ScoutedMinAge, PlayerFactory.ScoutedMaxAge));
                var ages = newcomers.ToDictionary(p => p, p => p.Age);
                var youth = game.Finance(manager).Youth.Select(t => (t.Strength, t.Rank, t.League)).ToArray();
                Assert.All(youth, t => Assert.Contains(t.Rank, RanksAfterYouthChange));

                NextSeason(game, manager, active);
                Assert.Equal(youth, game.Finance(manager).Youth.Select(t => (t.Strength, t.Rank, t.League)).ToArray());
                foreach (var (p, age) in ages)
                    Assert.Equal(age + 1, p.Age);
                total += newcomers.Count;
            }
        }
        Assert.True(total > 0, "Keine Jugend am Saisonende");
    }

    /// <summary>
    /// Spielstand vor Schema 50 vom Bildschirm „Saisonende“: Die Migration merkt Jugendwechsel und Jugend als ausstehend vor
    /// (<c>_seasonEndYouthPending</c>), der Saisonwechsel holt sie nach und löscht die Marke; in anderen Phasen keine Marke.
    /// </summary>
    [Fact]
    public void SaveGame_Before50_AtSeasonEnd_CatchesUpYouth()
    {
        var game = TestGames.Create(3);
        Assert.False(Pending(SaveGame.Save(Migrated(game))));
        PlayToSeasonEnd(game);
        var loaded = Migrated(game);
        Assert.True(Pending(SaveGame.Save(loaded)));

        var manager = loaded.Managers[0];
        // Erwartet: der Jugendwechsel (0x3A13B) noch einmal auf den Stand des Bildschirms (Platz und Liga ohne Zufall).
        var expected = loaded.Finance(manager).Youth.Select(t =>
        {
            var (p, l) = (t.Rank - 1, t.League - 1);
            if (p < 2 && l > 0) return (p + 11, l);
            if (p > 10 && l < YouthTeam.Leagues - 1) return (p - 9, l + 2);
            return (YouthTeam.StartRank, l + 1);
        }).ToArray();
        loaded.StartNextSeason();
        Assert.False(Pending(SaveGame.Save(loaded)));
        Assert.Equal(expected, loaded.Finance(manager).Youth.Select(t => (t.Rank, t.League)).ToArray());
    }

    /// <summary>Spielstand als Schema 49 ohne die Marke laden (Migration KaderRest).</summary>
    private static GameState Migrated(GameState game)
    {
        var root = JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        Assert.True(root["spiel"]!.AsObject().Remove("_seasonEndYouthPending"));
        root["schema"] = 49;
        var loaded = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.Null(loaded.Error);
        return loaded.Game!;
    }

    private static bool Pending(byte[] save) => JsonNode.Parse(save)!["spiel"]!["_seasonEndYouthPending"]!.GetValue<bool>();
}
