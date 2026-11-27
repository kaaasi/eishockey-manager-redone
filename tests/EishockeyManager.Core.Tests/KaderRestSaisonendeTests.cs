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
        // A665 (Spur einzelpunkte): Die Platzverschiebung bei Zwangsabstieg ändert die Folge der Seeds 3 und 7 (Manager pleite, Zwangsabsteiger
        // schon unter den Plätzen 3/4); die Behauptung „Summe > 0“ hängt von der Zufallsfolge ab, daher mehr Seeds.
        var total = 0;
        // Runde 22, Spur national Teil 1 (A109, A424, A494, A551; R15-E2 = C): Saat 9 dazu – Länderspiele mit Spielvorbereitung 0x8365 und stehendem Einsatz 0x26, Tagesform nach WM-Gruppenterminen, Verletzungen und Belastung im Satz 5 ändern den Zufallsverbrauch ab der ersten WM; mit 3, 5, 7 rückt beim aktiven
        // Lauf keine Jugend nach (Vorbedingung „total > 0“). Die Probe hängt an der Saat; die Regel je Neuzugang bleibt.
        foreach (var seed in new[] { 3, 5, 7, 9, 13, 15, 17 })
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

    private static bool Pending(byte[] save) => JsonNode.Parse(save)!["spiel"]!["_seasonEndYouthPending"]!.GetValue<bool>();
}
