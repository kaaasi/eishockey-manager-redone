using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Screens.Statistics;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>Spur tabellen-highscore Teil 2: Kachel „Highscore“ – Bildschirm, Sperre, Eintrag, Speicherung (em.exe 0x5D90D ff.).</summary>
public class HighscoreScreenTests
{
    private static MenuTile Tile(string name) => MenuTiles.Small.SelectMany(t => t).First(t => t.Name == name);

    [Fact]
    public void Tile_IsRegistered_WithoutPlaceholder()
    {
        Assert.True(MenuEntries.All.ContainsKey("Highscore"));
        Assert.DoesNotContain("Highscore", MenuEntryTests.DocumentedPlaceholders);
    }

    /// <summary>[ORIGINAL] Zeilenraster: Grundlinie 154 + 10 · Zeile, Name x = 56, Verein x = 115, Punkte x = 207, Titel x = 240.</summary>
    [Fact]
    public void Layout_AsInOriginal()
    {
        Assert.Equal(154, HighscoreScreen.FirstBaseline);
        Assert.Equal(10, HighscoreScreen.RowPitch);
        Assert.Equal([56, 115, 207, 240], [HighscoreScreen.NameX, HighscoreScreen.ClubX, HighscoreScreen.ScoreX, HighscoreScreen.TitlesX]);
        Assert.Equal(195, HighscoreScreen.Background);
    }

    [GameDataFact]
    public void Open_EntersManagers_WithoutChangingGameState()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var before = StateFingerprint.Of(game);

        d.OpenTile("Highscore");
        d.Frame();
        var screen = Assert.IsType<HighscoreScreen>(d.Host.Current);
        var entry = Assert.Single(screen.Table.Entries, e => !e.IsEmpty);
        Assert.Equal("LetsShow", entry.Name);
        Assert.Equal(game.Clubs[game.ActiveManager.ClubIndex].Name, entry.Club);
        Assert.True(entry.Score >= 1);
        Assert.Equal(HighscoreStore.SessionStamp, entry.Stamp);

        d.Press(Key.Escape);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        Assert.Equal(before, StateFingerprint.Of(game));
    }

    /// <summary>Zweimal öffnen ergibt keinen zweiten Eintrag (gleicher Name und Verein ersetzt den alten).</summary>
    [GameDataFact]
    public void OpeningTwice_KeepsOneEntry()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        for (var i = 0; i < 2; i++)
        {
            d.OpenTile("Highscore");
            d.Press(Key.Escape);
        }
        d.OpenTile("Highscore");
        Assert.Single(((HighscoreScreen)d.Host.Current!).Table.Entries, e => !e.IsEmpty);
    }

    [GameDataFact]
    public void Tile_IsLocked_WithoutHighscoreList()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        Assert.False(NationalMenu.IsLocked(game, Tile("Highscore")));

        game.Setup.Managers[0].Level = 2; // [ORIGINAL] Level ≤ 2: DGROUP 0x256 = 0 → Kachel gesperrt (Datei 0x44FCE)
        Assert.Null(HighscoreRules.Key(game.Setup));
        Assert.True(NationalMenu.IsLocked(game, Tile("Highscore")));
    }

    /// <summary>[ORIGINAL] Datei 0x5D617: je Schlüssel eine Datei <c>h.&lt;Schlüssel&gt;</c> mit 464 Byte; ein neuer Host liest sie wieder.</summary>
    [GameDataFact]
    public void List_IsPersisted_AsOriginalFile()
    {
        var dir = Path.Combine(Path.GetTempPath(), "em-highscore-" + Guid.NewGuid().ToString("N"));
        try
        {
            var d = FlowTests.CreateDriver();
            HighscoreStore.For(d.Host).Directory = dir;
            d.StartNewGame();
            var key = HighscoreRules.Key(d.Host.Game!.Setup)!.Value;
            d.OpenTile("Highscore");

            var path = Path.Combine(dir, HighscoreRules.FileName(key));
            Assert.Equal(HighscoreTable.FileLength, new FileInfo(path).Length);

            var other = FlowTests.CreateDriver();
            HighscoreStore.For(other.Host).Directory = dir;
            var loaded = HighscoreStore.For(other.Host).Load(key);
            Assert.Equal("LetsShow", loaded.Entries[0].Name);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>[ORIGINAL] Saisonwechsel (Datei 0x56647): jeder Saisonwechsel trägt die Manager ein, auch ohne die Kachel zu öffnen.</summary>
    [GameDataFact]
    public void SeasonChange_RecordsManagers()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        game.Setup.Duration = GameDuration.DreiSaisons;
        var key = HighscoreRules.Key(game.Setup)!.Value;
        var store = HighscoreStore.For(d.Host);
        Assert.All(store.Load(key).Entries, e => Assert.True(e.IsEmpty));

        var guard = 0;
        while (game.Season == GameState.FirstSeason && guard++ < 10000)
            d.Press(Key.Space);
        Assert.Contains(store.Load(key).Entries, e => e.Name == "LetsShow" && !e.IsEmpty);
    }
    /// <summary>
    /// [ORIGINAL] Titel „Highscore“ pixelgleich mit oberflaeche_0844–0855 (Review Runde 4 Teil 2, H2-01): Ausdehnung der
    /// Titelpixel (Helligkeit R+G+B &gt; 600, y 3–17, Fenster x 100–219) im Video x = 117 … 202. Das Remake lag vorher bei 116 … 201.
    /// </summary>
    [GameDataFact]
    public void Title_PixelExtent_MatchesVideo()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.OpenTile("Highscore");
        d.Frame();
        Assert.IsType<HighscoreScreen>(d.Host.Current);

        var rgb = d.Surface.Palette.Rgb;
        int min = int.MaxValue, max = int.MinValue;
        for (var y = 3; y <= 17; y++)
            for (var x = 100; x < 220; x++) // Fenster um den Titel: der Hintergrund hat links und rechts eigene helle Streifen
            {
                var p = d.Surface.Pixels[y * Surface.Width + x] * 3;
                // Palette liegt in 8-Bit-RGB vor (Palette.Rgb), Helligkeit wie bei der Vermessung der Videobilder.
                if (rgb[p] + rgb[p + 1] + rgb[p + 2] > 600)
                {
                    min = Math.Min(min, x);
                    max = Math.Max(max, x);
                }
            }

        Assert.Equal((117, 202), (min, max));
    }
}
