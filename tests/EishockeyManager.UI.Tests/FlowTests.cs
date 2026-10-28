using EishockeyManager.Tests.Support;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Testing;
using Xunit.Abstractions;

namespace EishockeyManager.UI.Tests;

/// <summary>Prüft die Anmeldung der Bildschirme (läuft ohne Originaldateien).</summary>
public class MenuEntryTests(ITestOutputHelper output)
{
    /// <summary>
    /// Kacheln, die noch keinen eigenen Bildschirm haben und deshalb den <see cref="PlaceholderScreen"/> zeigen.
    /// Wer einen Bildschirm anmeldet, darf den Namen hier streichen (vergessene Einträge melden die Tests nur).
    /// Neue Platzhalter – etwa durch eine beim Zusammenführen verlorene Anmeldung – lassen die Tests scheitern.
    /// </summary>
    public static readonly IReadOnlySet<string> DocumentedPlaceholders = new HashSet<string>
    {
        "Highscore",
        "Totale", "Früher", "Verein", "Spieler", "Fairplay", "WM-Statistik",
        "Tabelle WM",
        "Paarungen 1. Liga", "Paarungen 2. Liga", "Paarungen Oberliga", "Europapokal", "WM",
        "Stärken 1. Liga", "Stärken 2. Liga", "Stärken Oberliga", "Stärken Länderteams",
        "Laden", "Sichern", "Einstellungen",
    };

    /// <summary>Kacheln ohne angemeldeten Bildschirm (in der Reihenfolge des Hauptmenüs).</summary>
    public static IReadOnlyList<string> PlaceholderTiles =>
        MenuTiles.AllNames.Where(n => !MenuEntries.All.ContainsKey(n)).ToList();

    [Fact]
    public void Registry_IsValid() => Assert.NotEmpty(MenuEntries.All); // wirft bei unbekannten oder doppelten Kacheln

    [Fact]
    public void TileNames_AreUnique()
    {
        var names = MenuTiles.Small.SelectMany(t => t).Where(t => !t.IsEmpty).Select(t => t.Name).ToList();
        Assert.Equal(names.Count, names.Distinct().Count());
    }

    /// <summary>
    /// A07: Platzhalter sichtbar auflisten und nur die dokumentierten zulassen. Die Liste erscheint in der
    /// Testausgabe und im CI-Bericht („Platzhalter-Kacheln“).
    /// </summary>
    [Fact]
    public void PlaceholderTiles_AreListedAndDocumented()
    {
        var placeholders = PlaceholderTiles;
        var real = MenuTiles.AllNames.Where(MenuEntries.All.ContainsKey).ToList();
        output.WriteLine($"Platzhalter-Kacheln ({placeholders.Count}): {string.Join(", ", placeholders)}");
        output.WriteLine($"Echte Bildschirme ({real.Count}): {string.Join(", ", real)}");

        var stale = DocumentedPlaceholders.Where(n => MenuEntries.All.ContainsKey(n)).ToList();
        if (stale.Count > 0)
            output.WriteLine($"Hinweis: inzwischen angemeldet, aus {nameof(DocumentedPlaceholders)} streichen: {string.Join(", ", stale)}");

        Assert.All(DocumentedPlaceholders, n => Assert.Contains(n, MenuTiles.AllNames));
        var undocumented = placeholders.Where(n => !DocumentedPlaceholders.Contains(n)).ToList();
        Assert.True(undocumented.Count == 0,
            $"Kachel(n) ohne Bildschirm, aber nicht als Platzhalter dokumentiert: {string.Join(", ", undocumented)}");
    }
}

/// <summary>
/// Spielt Abläufe mit den echten Originaldateien durch. Ohne Ordner "gamedata" (z. B. auf GitHub) werden die
/// Tests als übersprungen gemeldet; mit <c>EM_REQUIRE_GAMEDATA=1</c> scheitern sie stattdessen.
/// </summary>
public class FlowTests(ITestOutputHelper output)
{
    /// <summary>Treiber mit den Originaldateien (scheitert, wenn sie fehlen – nur in <see cref="GameDataFactAttribute"/>-Tests nutzen).</summary>
    internal static ScreenDriver CreateDriver(int seed = 1993) => ScreenDriver.Create(TestGameData.Load(), seed: seed);

    [GameDataFact]
    public void FullSeason_FromTitleToLastMatchDay()
    {
        var d = CreateDriver();

        d.StartNewGame();
        Assert.NotNull(d.Host.Game);
        while (!d.Host.Game!.RegularSeasonOver)
            d.PlayMatchDay();
        Assert.Equal(44, d.Host.Game.PlayedMatchDays);
    }

    /// <summary>
    /// A07: Jede Kachel öffnet entweder ihren angemeldeten Bildschirm oder – nur wenn keiner angemeldet ist – den
    /// Platzhalter. Ein Platzhalter zählt nicht als funktionierender Bildschirm.
    /// </summary>
    [GameDataFact]
    public void EveryMenuTile_OpensRegisteredScreenOrPlaceholder()
    {
        var d = CreateDriver();

        d.StartNewGame();
        d.PlayMatchDay();
        var placeholders = new List<string>();
        foreach (var name in MenuTiles.AllNames)
        {
            d.OpenTile(name);
            var screen = d.Host.Current;
            Assert.IsNotType<MainMenuScreen>(screen);
            if (MenuEntries.All.ContainsKey(name))
                Assert.False(screen is PlaceholderScreen, $"Kachel \"{name}\" ist angemeldet, zeigt aber den Platzhalter.");
            else
            {
                Assert.IsType<PlaceholderScreen>(screen);
                placeholders.Add(name);
            }
            d.Frame();
            ReturnToMainMenu(d);
        }
        output.WriteLine($"Platzhalter-Kacheln ({placeholders.Count}): {string.Join(", ", placeholders)}");
        Assert.Equal(MenuEntryTests.PlaceholderTiles, placeholders);
    }

    /// <summary>A08: Ein Spieltag, der nicht ins Hauptmenü zurückführt, ist ein Fehler – kein stiller Abbruch.</summary>
    [GameDataFact]
    public void PlayMatchDay_Throws_WhenMainMenuIsNeverReached()
    {
        var d = CreateDriver();
        d.StartNewGame();
        d.Host.Show(new StuckScreen(d.Host));

        var error = Assert.Throws<TimeoutException>(d.PlayMatchDay);
        Assert.Contains(nameof(StuckScreen), error.Message);
    }

    /// <summary>Verlässt einen Bildschirm (auch mit offenem Dialog) und kehrt ins Hauptmenü zurück.</summary>
    internal static void ReturnToMainMenu(ScreenDriver d)
    {
        for (var attempt = 0; attempt < 5 && d.Host.Current is not MainMenuScreen; attempt++)
        {
            d.Press(Key.Escape);
            if (d.Host.Current is not MainMenuScreen)
                d.RightClick(1, 1);
        }
        Assert.IsType<MainMenuScreen>(d.Host.Current);
    }

    /// <summary>Bildschirm, der jede Eingabe ignoriert (Fehlerfall für <see cref="ScreenDriver.PlayMatchDay"/>).</summary>
    private sealed class StuckScreen(ScreenHost host) : Screen(host)
    {
        public override void Update(InputState input) { }
        public override void Draw(Surface surface) { }
    }
}
