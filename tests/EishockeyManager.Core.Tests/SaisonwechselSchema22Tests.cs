using System.Text.Json.Nodes;
using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur saisonwechsel, Runde 12 Teil 4 (Gesamtreview #474, Bereich 1): echter Schema-22-Spielstand, laden, migrieren und über
/// vier Saisonwechsel weiterspielen.
/// </summary>
/// <remarks>
/// <c>Fixtures/schema22-synthetisch.ems.gz</c> ist kein Originalmaterial: erzeugt am letzten Stand von <c>main</c> mit
/// Schema 22 (<c>02c4bd7</c>, Merge #446) mit synthetischer Datenbank (<c>TestGames.CreateDatabase()</c>), Seed 22, Spieldauer
/// unbegrenzt; Manager „Aktiv“ auf Verein 2 (Konto mindestens 5 Mio. DM, Kader gehalten) und „Passiv“ auf Verein 14 (keine
/// Eingriffe). Eine volle Saison, Saisonwechsel, zehn Spieltage der zweiten Saison (1994, Tag 35), dann <c>SaveGame.Save</c>.
/// Der Erzeuger steht in der PR-Beschreibung (#488), nicht im Repo, weil er nur am alten Stand übersetzt.
/// </remarks>
public class SaisonwechselSchema22Tests
{
    private static byte[] LoadFixture()
    {
        using var stream = typeof(SaisonwechselSchema22Tests).Assembly
            .GetManifestResourceStream("Fixtures.schema22-synthetisch.ems.gz")!;
        using var unzip = new System.IO.Compression.GZipStream(stream, System.IO.Compression.CompressionMode.Decompress);
        using var bytes = new MemoryStream();
        unzip.CopyTo(bytes);
        return bytes.ToArray();
    }

    [Fact]
    public void RealSchema22Save_Migrates_AndPlaysFourSeasonChanges()
    {
        var bytes = LoadFixture();
        Assert.Equal(22, JsonNode.Parse(bytes)!["schema"]!.GetValue<int>());

        var loaded = SaveGame.Load(bytes);
        Assert.True(loaded.Success, loaded.Error);
        var game = loaded.Game!;
        Assert.Equal(1994, game.Season);
        Assert.Single(game.History);
        Assert.Equal(2, game.Managers.Count);
        Assert.Empty(game.ValidatePlayerIdentity());
        SaveGameTests.AssertRoundTrip(game);

        var eternalBefore = Enumerable.Range(0, NewGame.SelectableClubs).Sum(c => game.Eternal(c).Points);
        Assert.True(eternalBefore > 0, "Ewige Tabelle der ersten Saison fehlt nach der Migration");

        // Mehrsaisonprobe (AGENTS.md §5): aktiver Manager hält Konto und Kader; passiver Manager greift nicht ein, nur sein Kader
        // wird gehalten (TestSeason.KeepSquad, sonst laufen die Startverträge nach zwei Saisons aus).
        for (var change = 0; change < 4; change++)
        {
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 3000, $"Saisonende {change + 1} nicht erreicht");
                game.Managers[0].Money = Math.Max(game.Managers[0].Money, 5_000_000);
                foreach (var m in game.Managers)
                    // #772 (R15-E2 = C): auch abgelaufene Verträge verlängern – mit der Spielvorbereitung 0x8365 verlieren beide Manager
                    // in Saison 4 ihren Kader bis auf 0/1 Spieler (Verträge, Ruhestand), die Probe hält ihn wie die übrigen Mehrsaisonproben.
                    TestSeason.KeepSquad(game, m, extendExpired: true);
                game.Continue();
            }
            Assert.Equal(SeasonPhase.SeasonOver, game.Phase);
            game.StartNextSeason();

            Assert.Equal(1995 + change, game.Season);
            Assert.Equal(2 + change, game.History.Count);
            Assert.All(Enum.GetValues<League>(), l => Assert.Equal(GameState.ClubsPerLeague, game.LeagueClubs(l).Count()));
            Assert.Empty(game.ValidatePlayerIdentity());
            Assert.All(game.Managers, m => Assert.True(m.Dismissed || game.Clubs[m.ClubIndex].Players.Count() > 0));
            SaveGameTests.AssertRoundTrip(game);
        }
        Assert.False(game.Managers[0].Dismissed);
        Assert.True(Enumerable.Range(0, NewGame.SelectableClubs).Sum(c => game.Eternal(c).Points) > eternalBefore);
        Assert.Equal(5, game.ManagerHistory(0).Count);
    }
}
