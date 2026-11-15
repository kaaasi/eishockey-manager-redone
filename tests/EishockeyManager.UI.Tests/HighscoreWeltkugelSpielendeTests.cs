using System.Reflection;
using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Statistics;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// #874 (Runde 20, Spur pruefung-karte Teil 3; Gesamtreview Runde 19, PR #873): Eintragung und Darstellung der Bestenliste am
/// Spielende im Modus „nur Weltkugel“. Vorlage DOSBox `runde-17` FILE54 (Dauer „eine Saison“, Manager „Spieler“, Deutschland
/// Vierter der WM): Bild <c>file54_4</c> zeigt als einzigen Eintrag auf Platz 1 „Spieler – DEUTSCHLAND – 5“ ohne Titelspalte,
/// danach „Das wars…“ (<c>file54_5</c>; <c>docs/analyse/abgleich-dosbox.md</c> §3.3).
/// [ORIGINAL] Spielende Datei 0x5642E (Bestenliste eintragen und zeigen, dann 0x56438 „Das wars…“), Schlüssel 30000 (DGROUP
/// 0x256 = 0x7530), Datei <c>h.30000</c> (Datei 0x5D617), Punkte Σ 9 − Platz (Datei 0x5D59B–0x5D607, <see cref="HighscoreRules.WorldOnlyScore"/>).
/// </summary>
public sealed class HighscoreWeltkugelSpielendeTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "em-highscore-weltkugel-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    private ScreenDriver StartWorldOnly(string name)
    {
        var d = FlowTests.CreateDriver();
        HighscoreStore.For(d.Host).Directory = _directory;
        d.Host.Show(new TitleScreen(d.Host));
        d.Frame();
        d.Click(160, 120);
        d.Click(20, 60);
        var cell = SetupScreen.HeadCell(3);
        d.Click(cell.X + 5, cell.Y + 5);
        d.Type(name);
        d.Press(Key.Enter);
        d.Click(SetupScreen.ModeFlag.X + 5, SetupScreen.ModeFlag.Y + 5); // nur Weltkugel
        d.Click(SetupScreen.StartButton.X + 5, SetupScreen.StartButton.Y + 5);
        Assert.Equal(GameMode.WorldOnly, d.Host.Game!.Mode);
        return d;
    }

    /// <summary>Eine WM mit Deutschland auf <paramref name="germanyPlace"/> in die Platzierungsfolge eintragen.</summary>
    private static void RecordWorldCup(GameState game, int germanyPlace)
    {
        var others = Enumerable.Range(NationalTeams.First, 12).Where(n => n != NationalTeams.Germany).ToList();
        others.Insert(germanyPlace - 1, NationalTeams.Germany);
        typeof(GameState).GetMethod("RecordWorldCupRanking", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(game, [others]);
    }

    /// <summary>
    /// Wie FILE54 → <c>file54_4</c>: Am Spielende steht der Manager als einziger Eintrag auf Platz 1 mit Verein DEUTSCHLAND und
    /// 5 Punkten (Vierter), die Liste ist als <c>h.30000</c> gespeichert, und ein Klick führt zu „Das wars…“. Rot, wenn die
    /// Punkte wieder „Weltmeistertitel + 1“ zählten (1), der Eintrag am Spielende fehlte oder doppelt käme.
    /// </summary>
    [GameDataFact]
    public void GameEnd_RecordsAndShowsManager_LikeFile54()
    {
        var d = StartWorldOnly("Spieler");
        var game = d.Host.Game!;
        RecordWorldCup(game, 4);

        d.Host.Show(new SeasonEndGameOverScreen(d.Host, null, null));
        d.Frame();

        var screen = Assert.IsType<HighscoreScreen>(d.Host.Current);
        var entries = screen.Table.Entries;
        Assert.Equal(("Spieler", "DEUTSCHLAND", (short)5), (entries[0].Name, entries[0].Club, entries[0].Score));
        Assert.Equal((0, 0, 0), (entries[0].TitlesA, entries[0].TitlesB, entries[0].TitlesC));
        Assert.All(entries.Skip(1), e => Assert.True(e.IsEmpty));

        var path = Path.Combine(_directory, HighscoreRules.FileName(HighscoreRules.WorldOnlyKey));
        var saved = HighscoreTable.Parse(File.ReadAllBytes(path));
        Assert.Equal(entries, saved.Entries);

        d.Click(160, 120);
        d.Frame();
        Assert.IsType<SeasonEndGameOverScreen>(d.Host.Current);
    }

    /// <summary>
    /// Darstellung wie <c>file54_4</c>: Platz 1 (Grundlinie 154) trägt Name, Verein und Punkte „00005“ in den Spalten des Originals,
    /// die Titelspalte ab x = 240 bleibt im Modus „nur Weltkugel“ leer; die Zeilen 2 … 8 bleiben leer. Geprüft gegen dasselbe Bild
    /// ohne Eintrag (nur Hintergrund und Titel) und gegen einen Eintrag mit 6 Punkten (andere Ziffer nur in der Punktespalte).
    /// </summary>
    [GameDataFact]
    public void GameEnd_DrawsRowOne_WithoutTitleColumn_LikeFile54()
    {
        var d = StartWorldOnly("Spieler");
        var game = d.Host.Game!;
        RecordWorldCup(game, 4);
        d.Host.Show(new SeasonEndGameOverScreen(d.Host, null, null));
        d.Frame();
        var screen = Assert.IsType<HighscoreScreen>(d.Host.Current);
        var shown = (byte[])d.Surface.Pixels.Clone();

        byte[] Render(HighscoreTable table)
        {
            d.Host.Show(new HighscoreScreen(d.Host, d.Host.Current!, table));
            d.Frame();
            return (byte[])d.Surface.Pixels.Clone();
        }

        var empty = Render(new HighscoreTable());
        var six = new HighscoreTable();
        var entry = screen.Table.Entries[0];
        six.Insert(entry with { Score = 6 });
        var other = Render(six);

        // Zeile 1: Oberkante Mini etwa 7 Pixel über der Grundlinie, Schatten 1 Pixel darunter.
        const int top = HighscoreScreen.FirstBaseline - 8, bottom = HighscoreScreen.FirstBaseline + 2;
        bool Differs(byte[] a, byte[] b, int x0, int x1, int y0, int y1)
        {
            for (var y = y0; y <= y1; y++)
                for (var x = x0; x <= x1; x++)
                    if (a[y * Surface.Width + x] != b[y * Surface.Width + x])
                        return true;
            return false;
        }

        Assert.True(Differs(shown, empty, HighscoreScreen.NameX, HighscoreScreen.ClubX - 2, top, bottom), "Name fehlt");
        Assert.True(Differs(shown, empty, HighscoreScreen.ClubX, HighscoreScreen.ScoreX - 2, top, bottom), "Verein fehlt");
        Assert.True(Differs(shown, empty, HighscoreScreen.ScoreX, HighscoreScreen.TitlesX - 2, top, bottom), "Punkte fehlen");
        Assert.False(Differs(shown, empty, HighscoreScreen.TitlesX, Surface.Width - 1, top, bottom), "Titelspalte gezeichnet");
        Assert.False(Differs(shown, empty, 0, Surface.Width - 1, bottom + 1, Surface.Height - 1), "Zeilen 2 … 8 nicht leer");
        // 5 gegen 6: nur die Punktespalte ändert sich.
        Assert.False(Differs(shown, other, 0, HighscoreScreen.ScoreX - 1, top, bottom));
        Assert.True(Differs(shown, other, HighscoreScreen.ScoreX, HighscoreScreen.TitlesX - 2, top, bottom));
    }
}
