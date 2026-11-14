using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur nutzer-befunde, #837: Zuschauer vor dem Spiel. [ORIGINAL] Die Spielabrechnung 0x544C0 läuft je Paarung vor den
/// Minuten (Datei 0x36DBD/0x36F9C); das Remake rechnete die Zuschauer erst nach dem Spieltag, der Live-Bildschirm hatte im
/// Heimspiel keine Zahl.
/// </summary>
public class NutzerBefundeMatchGateTests
{
    private static GameState TwoManagers(int seed, ClubDatabase? database = null)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", Head = 0, ClubIndex = 2 });
        setup.Managers.Add(new ManagerSetup { Name = "B", Head = 1, ClubIndex = 7 });
        return NewGame.Create(database ?? TestGames.CreateDatabase(), setup, seed);
    }

    /// <summary>Hält im Managerspiel an (wie der Live-Bildschirm) und liest die Zuschauerzeile.</summary>
    private sealed class Probe(GameState game) : ILiveMatchDriver
    {
        public List<(int Home, int Away, (int Spectators, bool SoldOut)? Gate)> Seen { get; } = [];

        public void Run(LiveMatchDay day)
        {
            var m = day.Matches[0];
            Seen.Add((m.Home.Index, m.Away.Index, game.HomeGate(m.Home, m.Away)));
        }
    }

    /// <summary>
    /// Während des Spiels stehen die Zuschauer des Managerheimspiels fest; die Abrechnung danach bucht genau sie
    /// (<see cref="ManagerFinance.LastHomeGame"/>). Auswärts keine Zahl.
    /// </summary>
    [Fact]
    public void HomeGate_KnownDuringMatch_EqualsBookedSpectators()
    {
        var game = TwoManagers(3);
        var manager = game.Managers[0];
        var probe = new Probe(game);
        var homeGames = 0;
        using (MatchEngine.UseLiveDriver(probe))
        {
            for (var day = 0; day < 40 && homeGames < 3; day++)
            {
                var seen = probe.Seen.Count;
                game.Continue();
                foreach (var (home, away, gate) in probe.Seen.Skip(seen))
                {
                    if (home != manager.ClubIndex && home != game.Managers[1].ClubIndex)
                    {
                        Assert.Null(gate);
                        continue;
                    }
                    var owner = home == manager.ClubIndex ? manager : game.Managers[1];
                    Assert.NotNull(gate);
                    Assert.Equal(game.Finance(owner).LastHomeGame!.Value.Spectators, gate!.Value.Spectators);
                    if (owner == manager)
                        homeGames++;
                }
                Assert.Equal(0, game.UnsettledGates);
            }
        }
        Assert.Equal(3, homeGames);
    }

    /// <summary>
    /// Mehrsaisonprobe (vier Saisonwechsel, passiver Manager A, aktiver Manager B, ohne Geldfixierung): jede vorbereitete Paarung
    /// wird am selben Tag abgerechnet (Tabellenspieltag und Spielbedeutung stimmen, sonst Ausnahme), je Saison gibt es
    /// Vorbereitungen (vorbereitet = abgerechnet &gt; 0, Review P3-2), der aktive Manager nimmt Eintritt ein, und nach jedem
    /// Saisonwechsel läuft das geladene Spiel weiter (Sichern/Laden bytegleich, die Vorbereitung ist kein Spielstand).
    /// </summary>
    [GameDataFact]
    public void MultiSeason_FourChanges_EveryPreparedGateIsSettled()
    {
        var game = TwoManagers(5, ClubDatabase.Load(TestGameData.Load()));
        var firstSeason = game.Season;
        for (var change = 0; change < 4; change++)
        {
            var income = 0L;
            var (preparedBefore, settledBefore) = game.GateCounts;
            var active = game.Managers[1];
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 2000, "Saisonende nicht erreicht");
                TestSeason.KeepSquad(game, active, extendExpired: true);
                game.Continue();
                Assert.Equal(0, game.UnsettledGates);
                income = Math.Max(income, game.Finance(active).Book.Current(FinanceCategory.Eintritt)); // Saisonende leert das Buch
            }
            // Der passive Manager verlängert keinen Vertrag: später nicht spielfähig, Spiele gewertet (keine Vorbereitung).
            var prepared = game.GateCounts.Prepared - preparedBefore;
            var settled = game.GateCounts.Settled - settledBefore;
            Assert.True(prepared > 0, $"keine vorbereitete Paarung in Saison {game.Season}");
            Assert.Equal(prepared, settled);
            Assert.True(income > 0, $"kein Eintritt in Saison {game.Season}");
            Assert.All(game.Managers, m => Assert.False(m.Dismissed));
            TestSeason.KeepSquad(game, game.Managers[0]);
            TestSeason.KeepSquad(game, active, extendExpired: true);
            game.StartNextSeason();

            var saved = SaveGame.Save(game);
            var loaded = SaveGame.Load(saved);
            Assert.True(loaded.Success, loaded.Error);
            Assert.Equal(saved, SaveGame.Save(loaded.Game!));
            game = loaded.Game!;
        }
        Assert.Equal(firstSeason + 4, game.Season);
    }
}
