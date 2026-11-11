using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens.SaveLoad;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur hok-import, Teil 5 (L6): Speichern-Dialog wie im Original (A338, A339; <c>docs/analyse/re-runde-14.md</c> Sitzung 6,
/// Review R14RD2-05.3/.4). Blättern nur über das Pfeilpaar (150 | 169)/(174 | 169), Schritt 48, kein „SEITE:“, weder Mausrad
/// noch Tasten; Spielstand-Info (em.exe Datei 0x5B661) nur solange die rechte Taste gehalten wird; roter Marker unter der Maus.
/// Der Pixelvergleich mit <c>a338_*</c> aus dem Referenz-Repo läuft lokal (Ablauf <c>speichern-original</c>, Zahlen im PR),
/// die Bilder selbst gehören nie ins Code-Repo.
/// </summary>
public sealed class SaveLoadDialogOriginalTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "em-speichern-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    private ScreenDriver NewDriver()
    {
        var d = FlowTests.CreateDriver();
        var context = SaveContext.For(d.Host);
        context.Directory = _directory;
        context.AutoSaveOverride = false;
        d.StartNewGame();
        return d;
    }

    /// <summary>Legt <paramref name="count"/> Spielstand-Dateien an (Inhalt unwichtig für die Liste).</summary>
    private void Fill(int count)
    {
        Directory.CreateDirectory(_directory);
        for (var i = 0; i < count; i++)
            File.WriteAllBytes(Path.Combine(_directory, $"S{i:D2}.ems"), []);
    }

    private static (int X, int Y) Up => (150 + 3, 169 + 3);
    private static (int X, int Y) Down => (174 + 3, 169 + 3);

    private static void Click(ScreenDriver d, (int X, int Y) p) => d.Click(p.X, p.Y);

    // ---- A338: Blättern nur über das Pfeilpaar ---------------------------------------------------------

    [GameDataFact]
    public void Paging_OnlyThroughArrowPair_InStepsOfFortyEight_NoEffectAtTheEnds()
    {
        Fill(53); // 53 Einträge wie in der Aufnahme: Seite 1 = 48, Seite 2 = 5
        var d = NewDriver();
        d.OpenTile("Sichern");
        var screen = Assert.IsType<SaveLoadScreen>(d.Host.Current);
        Assert.Equal(2, screen.PageCount);
        Assert.Equal(0, screen.Page);

        Click(d, Up); // am Anfang ohne Wirkung
        d.Frame();
        Assert.Equal(0, screen.Page);

        Click(d, Down);
        Assert.Equal(0, screen.Page); // Halten = 1: Wirkung erst beim Loslassen (0x4BA58)
        d.Frame();
        Assert.Equal(1, screen.Page);

        Click(d, Down); // am Ende ohne Wirkung
        d.Frame();
        Assert.Equal(1, screen.Page);

        Click(d, Up);
        d.Frame();
        Assert.Equal(0, screen.Page);
    }

    [GameDataFact]
    public void Paging_WheelAndKeys_DoNothing()
    {
        Fill(53);
        var d = NewDriver();
        d.OpenTile("Sichern");
        var screen = Assert.IsType<SaveLoadScreen>(d.Host.Current);
        d.Wheel(-1);
        d.Press(Key.PageDown);
        d.Press(Key.Down);
        Assert.Equal(0, screen.Page);
        Click(d, Down);
        d.Frame();
        Assert.Equal(1, screen.Page);
        d.Wheel(1);
        d.Press(Key.PageUp);
        Assert.Equal(1, screen.Page);
    }

    [GameDataFact]
    public void Panel_ShowsNoPageLabel()
    {
        // „SEITE:“ liest em.exe nirgends (DGROUP 0xBC0 nie gelesen): Bereich rechts neben „VERZEICHNIS:“ gleich mit 1 und 2 Seiten.
        var region = new Rect(240, 186, 70, 10);
        Fill(1);
        var one = NewDriver();
        one.OpenTile("Sichern");
        var single = Crop(one.Surface, region);

        File.Delete(Path.Combine(_directory, "S00.ems"));
        Fill(53);
        var two = NewDriver();
        two.OpenTile("Sichern");
        Assert.Equal(2, Assert.IsType<SaveLoadScreen>(two.Host.Current).PageCount);
        Assert.Equal(single, Crop(two.Surface, region));

        // #716 R14G4-01: der Vergleich zweier Seitenzahlen sieht ein konstantes Label („SEITE: 1“) nicht. Unabhängig davon muss
        // der Ausschnitt rechts vom Ende der Verzeichniszeile (Text + 1 Pixel Schatten) leere Panelfläche sein: eine einzige
        // Farbe bis vor den rechten Rahmen (ab x 308, Panel 24 + 288 − 4). Gegenprobe: in DrawPanel konstant „SEITE: 1“ im
        // Ausschnitt zeichnen → rot.
        const int frameX = 308;
        var mini = two.Host.Assets.Mini;
        var textEnd = 35 + mini.Measure("VERZEICHNIS: " + SaveFiles.DisplayPath(_directory, 38)) + 1;
        Assert.True(textEnd < frameX - 30, $"Verzeichniszeile reicht bis x {textEnd}");
        var left = Math.Max(region.X, textEnd + 1);
        var free = new Rect(left, region.Y, frameX - left, region.H);
        foreach (var surface in new[] { one.Surface, two.Surface })
            Assert.Single(Crop(surface, free).Distinct());
    }

    [GameDataFact]
    public void Arrows_AreTheKb4Graphics_AtTheOriginalCallSite()
    {
        // [ORIGINAL] 0x5E3B5: ArrowPair(150, 169, 174, 169, 1); losgelassen mit Halten ≠ 0: kb\4 (21 | 1 + 9 · j) 8 × 7 → (x + 1 | y + 1)
        Fill(53);
        var d = NewDriver();
        d.OpenTile("Sichern");
        var arrows = d.Host.Assets.Image(ArrowPair.Image);
        foreach (var (j, x) in new[] { (0, 150), (1, 174) })
        {
            for (var py = 0; py < ArrowPair.ArrowHeight; py++)
            {
                for (var px = 0; px < ArrowPair.ArrowWidth; px++)
                {
                    var expected = arrows.Pixels[(1 + 9 * j + py) * arrows.Width + 21 + px];
                    if (expected == 0)
                        continue;
                    Assert.Equal(expected, d.Surface.Pixels[(169 + 1 + py) * Surface.Width + x + 1 + px]);
                }
            }
        }
    }

    [GameDataFact]
    public void Marker_AppearsLeftOfTheEntryUnderTheMouse_AndIsRemovedWhenLeaving()
    {
        Fill(5);
        var d = NewDriver();
        d.OpenTile("Sichern");
        var screen = Assert.IsType<SaveLoadScreen>(d.Host.Current);
        Assert.Equal(5, screen.Entries.Count);
        var area = new Rect(43, 55 + 7 * 2, 3, 6); // Zeile 2, Spalte 0: (43 + 80 · c | 55 + 7 · r)–(45 + 80 · c | 60 + 7 · r)
        d.Move(100, 55 + 7 * 2 + 3);
        // a338_3 gemessen: Füllung 17, hell 18 oben/links, dunkel 16 rechts/unten
        Assert.Equal(
            [18, 18, 16,
             18, 17, 16,
             18, 17, 16,
             18, 17, 16,
             18, 17, 16,
             18, 16, 16],
            Crop(d.Surface, area));
        d.Move(100, 55 + 7 * 3 + 3);
        Assert.DoesNotContain(SaveLoadScreen.MarkerColor, Crop(d.Surface, area));
        d.Move(100, 20); // außerhalb der Liste
        Assert.DoesNotContain(SaveLoadScreen.MarkerColor, Crop(d.Surface, new Rect(43, 55, 3, 7 * 16)));
    }

    /// <summary>
    /// Review R14HI-06: Spaltenwahl wie Datei 0x5E252–0x5E264 (Spalte zählt hoch, solange 0x7A + 80 · k &lt; x): x = 122 ist noch Spalte
    /// 0, x = 123 Spalte 1, x = 202 noch Spalte 1, x = 203 Spalte 2. Vorher rechnete das Remake (x − 42) / 80 und sprang an diesen
    /// beiden x-Werten eine Spalte zu früh.
    /// </summary>
    [GameDataTheory]
    [InlineData(122, 0)]
    [InlineData(123, 1)]
    [InlineData(202, 1)]
    [InlineData(203, 2)]
    [InlineData(43, 0)]
    [InlineData(265, 2)]
    public void Marker_ColumnBoundaries_LikeTheOriginal(int mouseX, int column)
    {
        Fill(40); // drei Spalten belegt
        var d = NewDriver();
        d.OpenTile("Sichern");
        Assert.Equal(40, Assert.IsType<SaveLoadScreen>(d.Host.Current).Entries.Count);
        d.Move(mouseX, 55 + 7 * 2 + 3);
        for (var c = 0; c < SaveLoadScreen.Columns; c++)
        {
            var area = new Rect(43 + 80 * c, 55 + 7 * 2, 3, 6);
            if (c == column)
                Assert.Equal(SaveLoadScreen.MarkerColor, d.Surface.Pixels[(55 + 7 * 2 + 2) * Surface.Width + 43 + 80 * c + 1]);
            else
                Assert.DoesNotContain(SaveLoadScreen.MarkerColor, Crop(d.Surface, area));
        }
    }

    /// <summary>[ORIGINAL] 0x5E2C7–0x5E2DD: kein Marker auf einer leeren Zeile (Spalte · 16 + Zeile + Seitenanfang ≥ Zahl der Einträge).</summary>
    [GameDataFact]
    public void Marker_NotOnEmptyRows()
    {
        Fill(5);
        var d = NewDriver();
        d.OpenTile("Sichern");
        d.Move(100, 55 + 7 * 4 + 3); // Zeile 4 belegt
        Assert.Equal(SaveLoadScreen.MarkerColor, d.Surface.Pixels[(55 + 7 * 4 + 2) * Surface.Width + 44]);
        d.Move(100, 55 + 7 * 5 + 3); // Zeile 5 leer
        Assert.DoesNotContain(SaveLoadScreen.MarkerColor, Crop(d.Surface, new Rect(43, 55, 3, 7 * 16)));
        d.Move(130, 55 + 3); // Spalte 1 leer
        Assert.DoesNotContain(SaveLoadScreen.MarkerColor, Crop(d.Surface, new Rect(123, 55, 3, 7 * 16)));
    }

    private static byte[] Crop(Surface s, Rect r)
    {
        var data = new List<byte>();
        for (var y = r.Y; y < r.Bottom; y++)
            for (var x = r.X; x < r.Right; x++)
                data.Add(s.Pixels[y * Surface.Width + x]);
        return [.. data];
    }

    // ---- A339: Spielstand-Info bei gehaltener rechter Taste ---------------------------------------------

    private static Rect EntryRectOf(string directory, string fileName)
    {
        var index = SaveFiles.List(directory).ToList().FindIndex(e => e.FileName == fileName);
        Assert.True(index >= 0, $"{fileName} fehlt in der Liste.");
        return SaveLoadScreen.EntryRect(index / SaveLoadScreen.RowsPerColumn, index % SaveLoadScreen.RowsPerColumn);
    }

    private static void HoldRight(ScreenDriver d, Rect rect, bool press) =>
        d.Frame(i => { i.MouseX = rect.X + 3; i.MouseY = rect.Y + 3; i.RightClick = press; i.RightDown = true; });

    [GameDataFact]
    public void Info_OfARemakeSave_ShowsOnlyWhileTheRightButtonIsHeld()
    {
        var d = NewDriver();
        d.PlayMatchDay();
        var game = d.Host.Game!;
        d.OpenTile("Sichern");
        d.Type("info1");
        d.Press(Key.Enter);
        d.OpenTile("Laden");
        var screen = Assert.IsType<SaveLoadScreen>(d.Host.Current);
        var rect = EntryRectOf(_directory, "INFO1.ems");
        var before = (screen.FileName, screen.Entries.Count);

        Assert.Empty(screen.InfoLines);
        HoldRight(d, rect, press: true);
        Assert.Same(screen, d.Host.Current); // Dialog bleibt
        Assert.NotEmpty(screen.InfoLines);
        Assert.StartsWith("SPIELSTAND:", screen.InfoLines[0]);
        Assert.EndsWith("INFO1.EMS", screen.InfoLines[0]);
        Assert.Equal($"Jahr {SeasonCalendar.Year(game.Day, game.Season)}, {game.PlayedMatchDays + 1}.Spieltag", screen.InfoLines[1]);
        Assert.Contains(game.ActiveManager.Name, screen.InfoLines[2]);

        HoldRight(d, rect, press: false); // weiter gehalten: bleibt stehen
        Assert.NotEmpty(screen.InfoLines);

        d.Frame(); // losgelassen: weg, Namensfeld und Auswahl unverändert
        Assert.Empty(screen.InfoLines);
        Assert.Equal(before, (screen.FileName, screen.Entries.Count));
        Assert.Same(screen, d.Host.Current);
    }

    [GameDataFact]
    public void Info_OfAnOriginalSave_ComesFromTheHeader_AsInFile46()
    {
        // FILE46: Kopf „KRON_0.99“, Jahr 1994, 1 Manager, Spieltag 1, Eintrag „Joerg“, „KÖLNER EC“, Liga 0, Byte +1 = 0 (a338_3)
        Directory.CreateDirectory(_directory);
        File.Copy(OriginalSave("FILE46.HOK"), Path.Combine(_directory, "KOPIE30.HOK"));
        var d = NewDriver();
        d.OpenTile("Sichern");
        var screen = Assert.IsType<SaveLoadScreen>(d.Host.Current);
        HoldRight(d, EntryRectOf(_directory, "KOPIE30.HOK"), press: true);
        Assert.Equal("Jahr 1994, 1.Spieltag", screen.InfoLines[1]);
        Assert.Contains("Joerg", screen.InfoLines[2]);
        Assert.Contains("KÖLNER EC", screen.InfoLines[2]);
        Assert.Contains("Bundesliga", screen.InfoLines[2]);
        Assert.EndsWith("KOPIE30.HOK", screen.InfoLines[0]);
        Assert.Equal("", screen.FileName); // die Rechtsklick-Strecke schreibt das Namensfeld nicht
    }

    [GameDataFact]
    public void Info_ShowsNoLeagueInWorldOnlyMode()
    {
        // FILE11: Modus „nur Weltkugel“ (Kopfbyte +1 = 1) → keine Ligazeile (em.exe 0x5B8A3–0x5B9A5)
        Directory.CreateDirectory(_directory);
        File.Copy(OriginalSave("FILE11.HOK"), Path.Combine(_directory, "WELT.HOK"));
        var d = NewDriver();
        d.OpenTile("Laden");
        var screen = Assert.IsType<SaveLoadScreen>(d.Host.Current);
        HoldRight(d, EntryRectOf(_directory, "WELT.HOK"), press: true);
        Assert.Contains("fsdfsd", screen.InfoLines[2]);
        Assert.DoesNotContain("liga", screen.InfoLines[2], StringComparison.OrdinalIgnoreCase);
    }

    [GameDataFact]
    public void Info_OfAForeignFile_ShowsTheNoSaveGameMessage()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllBytes(Path.Combine(_directory, "KAPUTT.HOK"), new byte[300]);
        var d = NewDriver();
        d.OpenTile("Laden");
        var screen = Assert.IsType<SaveLoadScreen>(d.Host.Current);
        HoldRight(d, EntryRectOf(_directory, "KAPUTT.HOK"), press: true);
        Assert.Empty(screen.InfoLines);
        Assert.Equal(["Kein", "Eishockey Manager", "Spielstand.", "!!!"], screen.MessageLines); // DGROUP 0x45D7–0x45FA
    }

    private static string OriginalSave(string name)
    {
        var folder = Environment.GetEnvironmentVariable("EM_SAVEGAMES");
        if (string.IsNullOrEmpty(folder))
        {
            var root = TestGameData.Load().RootDirectory;
            var real = Directory.ResolveLinkTarget(root.TrimEnd('/', '\\'), true)?.FullName ?? root;
            folder = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(real).TrimEnd('/', '\\'))!, "spielstaende");
        }
        var path = Path.Combine(folder, name);
        return File.Exists(path) ? path
            : throw new FileNotFoundException($"Original-Spielstand {name} fehlt in {folder} (Ordner per EM_SAVEGAMES angeben).");
    }
}
