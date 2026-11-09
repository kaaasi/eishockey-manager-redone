using EishockeyManager.Core;
using EishockeyManager.UI.Screens.Team;

namespace EishockeyManager.UI.Screens.SaveLoad;

/// <summary>Was der Dialog tut: einen Spielstand laden oder den laufenden sichern.</summary>
public enum SaveLoadMode
{
    Load,
    Save,
}

/// <summary>
/// Dialog „Laden“ und „Speichern“ (Kacheln „Laden“ und „Sichern“ unter „Gemischtes“, Notizbuch in der Spielvorbereitung).
/// </summary>
/// <remarks>
/// <para>
/// <b>Bild [ORIGINAL]:</b> Referenzbilder Video „Oberfläche“ 1:00–1:10 (<c>oberflaeche_0100</c>–<c>_0103</c>): Titelfeld
/// (Rahmen kb\2 orange, Schrift newbold), Dateiliste in drei Spalten zu je 16 Zeilen mit Trennbalken und Seitenpfeilen,
/// darunter „VERZEICHNIS:“, „FILENAME :“, Laufwerksknöpfe „A:“–„G:“ und „LÖSCHEN“. Spielstände gelb, alles andere grau.
/// Texte aus em.exe: „Laden“ (DGROUP 0x3787), „Speichern“ (0x377D), „VERZEICHNIS: “ (0x26EE), „FILENAME      :“ (0x26FC),
/// „LÖSCHEN“ (0x2714), „Die Datei … kann nicht geladen werden.“ (0x40CD), „Achtung ! Fehler beim
/// Diskettenzugriff ! Vorgang abgebrochen.“ (0x3EB0), „löschen ?“, „Mach schon!“, „Lieber nicht.“.
/// </para>
/// <para>
/// <b>Bedienung [ORIGINAL]</b> (em.exe 0x5DBE5, Spur hok-import Teil 5, A338/A339, <c>docs/analyse/re-runde-14.md</c> Sitzung 6):
/// Linksklick auf eine Datei übernimmt den Namen, bei gleichem Namen führt er aus. Blättern (mehr als 48 Einträge) nur über das
/// Pfeilpaar <see cref="ArrowPair"/> (150 | 169)/(174 | 169), Halten = 1 (Wirkung beim Loslassen), Schritt 48, an den Enden ohne
/// Wirkung (Datei 0x5E3B5, 0x5E386–0x5E42B); es gibt <b>kein „SEITE:“</b> (DGROUP 0xBC0 liest em.exe nirgends), kein Mausrad und
/// keine Tasten zum Blättern (die Dialogschleife 0x38:0x878 liest nur Mausereignisse). Unter der Maus zeichnet das Original links
/// neben dem Eintrag einen roten Marker (Datei 0x5E2C7–0x5E329). Rechtsklick auf eine Datei zeigt die Spielstand-Info 0x5B661
/// (<see cref="SaveInfo"/>) nur, solange die rechte Taste gehalten wird, und lässt Namensfeld, Auswahl und Dialog unverändert;
/// Rechtsklick sonst bricht ab.
/// </para>
/// <para>
/// <b>Abweichungen [APPROX]:</b> Das Video zeigt nur das Laden-Bild; das Bild des Speichern-Dialogs ist erst durch <c>a338_*</c>
/// belegt (gleicher Aufbau, Titel „Speichern“). Klick wählt den Spielstand und trägt seinen Namen ein, Enter führt aus
/// (Laden; Sichern überschreibt erst nach Rückfrage). Es gibt einen Ordner statt Laufwerken: die Laufwerksknöpfe sind
/// gezeichnet, haben aber keine Wirkung.
/// </para>
/// <para>
/// <b>Original-Spielstände (Spur hok-import, Teil 3):</b> Der Dialog listet auch <c>*.HOK</c>. Beim Laden sind sie gelb und
/// werden importiert (<see cref="OriginalImport.TryImport"/>); beim Sichern sind sie grau. Eine falsche Prüfsumme zeigt
/// „Der kleine Mogelguru“ und lädt nichts.
/// </para>
/// <para>
/// Das Öffnen und Abbrechen ändert den Spielzustand nicht (Test <c>OpenAndCancel_ChangesNoGameState</c>); ein Fehler beim
/// Laden lässt den laufenden Spielstand unberührt.
/// </para>
/// </remarks>
public sealed class SaveLoadScreen : Screen
{
    // ---- Lage (320 × 240), aus Referenzbild oberflaeche_0102 vermessen -----------------------------

    public static readonly Rect TitleFrame = new(92, 4, 144, 38);
    public static readonly Rect ListFrame = new(33, 48, 264, 133); // [ORIGINAL] 11 × 7 Kacheln zu 24 × 19, an a338_1 Pixel für Pixel gemessen (vorher nach dem Video 266 × 134)
    public static readonly Rect Panel = new(24, 182, 288, 57); // [ORIGINAL] 12 × 3 Kacheln zu 24 × 19, an a338_1 gemessen (vorher nach dem Video 289 × 58)

    public const int Columns = 3;
    public const int RowsPerColumn = 16;
    public const int EntriesPerPage = Columns * RowsPerColumn;

    private const int FirstBarX = 41;
    private const int BarPitch = 80;
    private const int BarTop = 54;
    private const int BarHeight = 113;
    private const int FirstRowY = 56;
    private const int RowPitch = 7;

    /// <summary>Fläche eines Eintrags (Spalte 0–2, Zeile 0–15).</summary>
    public static Rect EntryRect(int column, int row) => new(FirstBarX + column * BarPitch + 5, FirstRowY + row * RowPitch, 74, RowPitch);

    /// <summary>[ORIGINAL] Pfeilpaar zum Blättern: Aufruf 0x4BA58 mit (150, 169, 174, 169, 1), Datei 0x5E3B5.</summary>
    public static readonly Rect PageUpRect = new(150, 169, ArrowPair.HitWidth, ArrowPair.HitHeight);
    public static readonly Rect PageDownRect = new(174, 169, ArrowPair.HitWidth, ArrowPair.HitHeight);

    /// <summary>
    /// Füllfarbe des Markers unter der Maus. [ORIGINAL] Fläche (43 + 80 · Spalte | 55 + 7 · Zeile)–(45 + 80 · Spalte | 60 + 7 · Zeile),
    /// Datei 0x5E2C7–0x5E329; Löschen mit Farbe 3 (0x5E334–0x5E377). [APPROX] Die Abschrägung (hell 18 oben und links, dunkel 16
    /// rechts und unten) und die Indizes sind am Bild <c>a338_3</c> gemessen, nicht im Code gelesen.
    /// </summary>
    public const byte MarkerColor = 17;
    private const byte MarkerLight = 18;
    private const byte MarkerDark = 16;

    /// <summary>Spielstand-Info 0x5B661: Fenster (71 | 71), 8 × 3 Kacheln = 192 × 57 Pixel (Datei 0x5B73C–0x5B764).</summary>
    public static readonly Rect InfoFrame = new(71, 71, 192, 57);

    private const int LabelX = 35;
    private const int DirectoryY = 188;
    private const int FileNameY = 198;

    /// <summary>Knöpfe unten: Reihe 0 = A: bis D:, Reihe 1 = E: bis G: und LÖSCHEN.</summary>
    public static Rect ButtonRect(int row, int column) => new(53 + column * 59, 207 + row * 13, Widgets.ButtonWidth, Widgets.ButtonHeight);

    public static Rect DeleteButton => ButtonRect(1, 3);

    // Rückfrage (Überschreiben, Löschen)
    public static readonly Rect ConfirmFrame = new(70, 84, 180, 70);
    public static readonly Rect YesButton = Widgets.ButtonRect(84, 134);
    public static readonly Rect NoButton = Widgets.ButtonRect(179, 134);

    // ---- Farben der Standardpalette (aus den Referenzbildern zugeordnet) --------------------------

    private const byte Fill = 4;
    private const byte DirectoryColor = 6;
    private const byte LabelColor = 35;
    private const byte LabelShadow = 43;

    private enum Pending
    {
        None,
        Overwrite,
        Delete,
    }

    private readonly Screen _back;
    private readonly ArrowPair _pageArrows = new(PageUpRect.X, PageUpRect.Y, PageDownRect.X, PageDownRect.Y, waitForRelease: true);
    private SaveInfo? _info;
    private string _infoPath = "";
    private int _mouseX = -1;
    private int _mouseY = -1;
    private readonly MessageBox _message;
    private IReadOnlyList<SaveEntry> _entries = [];
    private int _page;
    private int _selected = -1;
    private string _name = "";
    private Pending _pending;
    private string _pendingName = "";

    public SaveLoadScreen(ScreenHost host, Screen back, SaveLoadMode mode) : base(host)
    {
        _back = back;
        _message = new(() => host.Game?.ActiveManager.Name ?? "");
        Mode = mode;
    }

    public SaveLoadMode Mode { get; }

    /// <summary>Eingetippter bzw. gewählter Name (für Tests).</summary>
    public string FileName => _name;

    /// <summary>Gerade angezeigte Meldung oder leer (für Tests).</summary>
    public IReadOnlyList<string> MessageLines => _message.Lines;

    /// <summary>Fragt der Dialog gerade nach, ob überschrieben oder gelöscht werden soll (für Tests)?</summary>
    public bool IsConfirming => _pending != Pending.None;

    public IReadOnlyList<SaveEntry> Entries => _entries;

    /// <summary>Angezeigte Seite (0-basiert; für Tests).</summary>
    public int Page => _page;

    /// <summary>
    /// Zeilen der Spielstand-Info, solange sie steht (sonst leer; für Tests): „SPIELSTAND:Pfad“, „Jahr …, n.Spieltag“ und je
    /// Manager „Name  Verein  Liga“.
    /// </summary>
    public IReadOnlyList<string> InfoLines => _info is null ? [] :
    [
        "SPIELSTAND:" + _infoPath,
        _info.DateLine,
        .. _info.Managers.Select(m => string.Join("  ", new[] { m.Name, m.Club, m.League }.Where(x => x is not null))),
    ];

    public int PageCount => Math.Max(1, (_entries.Count + EntriesPerPage - 1) / EntriesPerPage);

    private SaveContext Context => SaveContext.For(Host);

    private string? Directory => Context.Directory;

    public override void Enter() => Refresh();

    /// <summary>Liest die Dateiliste neu (nur lesend).</summary>
    private void Refresh()
    {
        _entries = Directory is { } directory ? SaveFiles.List(directory) : [];
        _page = Math.Clamp(_page, 0, PageCount - 1);
        if (_selected >= _entries.Count)
            _selected = -1;
    }

    private SaveEntry? EntryAt(int column, int row)
    {
        var index = _page * EntriesPerPage + column * RowsPerColumn + row;
        return index < _entries.Count ? _entries[index] : null;
    }

    private int IndexAt(int column, int row) => _page * EntriesPerPage + column * RowsPerColumn + row;

    // ---- Eingaben --------------------------------------------------------------------------------

    public override void Update(InputState input)
    {
        if (_message.Visible)
        {
            _message.Update(input);
            return;
        }
        if (_pending != Pending.None)
        {
            UpdateConfirm(input);
            return;
        }
        _mouseX = input.MouseX;
        _mouseY = input.MouseY;
        if (_info is not null)
        {
            // [ORIGINAL] 0x5B9A8–0x5B9D4: Schleife 0x38:0x878 bis zum Loslassen, dann Fenster zurück und weiter im Dialog.
            if (!input.RightDown && !input.LeftDown)
                _info = null;
            return;
        }

        if (input.Pressed(Key.Escape))
        {
            Host.Show(_back);
            return;
        }
        if (input.RightClick)
        {
            if (!TryRightClickEntry(input))
                Host.Show(_back);
            return;
        }

        // [ORIGINAL] Blättern nur über das Pfeilpaar, Schritt 48; am Anfang und Ende wirkungslos. Kein Mausrad, keine Tasten.
        ChangePage(_pageArrows.Update(input));

        foreach (var ch in input.Text)
        {
            if (_name.Length < SaveFiles.MaxNameLength && SaveFiles.IsAllowed(ch))
            {
                _name += char.ToUpperInvariant(ch);
                _selected = -1;
            }
        }
        if (input.Pressed(Key.Backspace) && _name.Length > 0)
        {
            _name = _name[..^1];
            _selected = -1;
        }
        if (input.Pressed(Key.Enter))
        {
            Execute();
            return;
        }

        if (input.Clicked(DeleteButton))
        {
            AskDelete();
            return;
        }

        if (!input.LeftClick)
            return;
        for (var column = 0; column < Columns; column++)
        {
            for (var row = 0; row < RowsPerColumn; row++)
            {
                if (!input.Clicked(EntryRect(column, row)))
                    continue;
                if (EntryAt(column, row) is { } entry && IsSelectable(entry))
                    ClickEntry(IndexAt(column, row), entry);
                return;
            }
        }
    }

    /// <summary>
    /// Rechtsklick auf einen Spielstand bricht nicht ab, zeigt die Spielstand-Info (solange die rechte Taste gehalten wird) und
    /// ändert weder Namensfeld noch Auswahl; woanders bricht er ab.
    /// [ORIGINAL] em.exe 0x5DBE5 (Datei 0x5E5ED–0x5E667: Taste 3 auf einem Dateieintrag baut Verzeichnis + Name in einem
    /// lokalen Puffer, ruft die Spielstand-Info 0x5B661 und bleibt im Dialog; das Namensfeld [bp+6] bleibt unberührt.
    /// Taste 3 sonst bricht ab: 0x5E79F). [APPROX] Die Info gibt es für Remake- und Original-Spielstände in beiden Modi
    /// (im Original ist jede Datei der Liste ein Spielstand); für Verzeichnisse und andere Dateien bricht der Rechtsklick ab.
    /// </summary>
    private bool TryRightClickEntry(InputState input)
    {
        for (var column = 0; column < Columns; column++)
        {
            for (var row = 0; row < RowsPerColumn; row++)
            {
                if (input.MouseX < 0 || !EntryRect(column, row).Contains(input.MouseX, input.MouseY))
                    continue;
                if (EntryAt(column, row) is not { Kind: SaveEntryKind.Save or SaveEntryKind.Original } entry)
                    return false;
                ShowInfo(entry);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Liest den Kopf der Datei (0x5B661): scheitert das Öffnen, kommt der Fehlerweg; stimmt die Kennung nicht, die Meldung
    /// „Kein Eishockey Manager Spielstand. !!!“ (DGROUP 0x45D7–0x45FA); sonst steht das Infofeld bis zum Loslassen.
    /// </summary>
    private void ShowInfo(SaveEntry entry)
    {
        if (Directory is not { } directory)
            return;
        var path = Path.Combine(directory, entry.FileName);
        byte[] data;
        try
        {
            data = File.ReadAllBytes(path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Warn("Achtung !", "Fehler beim Dateizugriff !", "Vorgang abgebrochen.");
            return;
        }
        if (SaveInfo.Read(data, entry.Kind) is not { } info)
        {
            Warn("Kein", "Eishockey Manager", "Spielstand.", "!!!");
            return;
        }
        // [APPROX] Das Original zeigt den DOS-Pfad; hier steht der Ordner (vorn gekürzt, damit die Zeile ins Fenster passt).
        var name = "/" + entry.FileName.ToUpperInvariant();
        var shown = SaveFiles.DisplayPath(directory, 40);
        while (shown.Length > 4 && Assets.Mini.Measure("SPIELSTAND:" + shown + name) > InfoFrame.W - 14)
            shown = SaveFiles.DisplayPath(directory, shown.Length - 1);
        _infoPath = shown + name;
        _info = info;
    }

    /// <summary>
    /// Wählbar sind Remake-Spielstände und – nur beim Laden – Original-Spielstände <c>*.HOK</c> (Import, Spur hok-import Teil 3).
    /// [APPROX] Beim Sichern sind sie grau: das Remake schreibt kein HOK (Export erst Runde 15, R14-D2 = B).
    /// </summary>
    private bool IsSelectable(SaveEntry entry) =>
        entry.Kind == SaveEntryKind.Save || (entry.Kind == SaveEntryKind.Original && Mode == SaveLoadMode.Load);

    private void ChangePage(int delta) => _page = Math.Clamp(_page + delta, 0, PageCount - 1);

    /// <summary>Erster Klick wählt den Spielstand (Name erscheint), ein weiterer auf denselben (oder bei gleichem Namen im Feld) führt aus.</summary>
    private void ClickEntry(int index, SaveEntry entry)
    {
        // [ORIGINAL] em.exe 0x5E66A–0x5E68C: gleicher Name wie im Feld → ausführen, sonst Name übernehmen.
        if (_selected == index || string.Equals(_name, entry.BaseName, StringComparison.OrdinalIgnoreCase))
        {
            _selected = index; // die angeklickte Datei gilt (gleicher Name als .ems und .HOK)
            Execute();
            return;
        }
        _selected = index;
        _name = entry.BaseName;
    }

    private void Execute()
    {
        if (Directory is not { } directory)
        {
            Warn("Achtung !", "Kein Spielstandordner eingerichtet.", "Vorgang abgebrochen.");
            return;
        }
        if (_name.Length == 0)
            return;
        if (SaveFiles.NormalizeName(_name) is not { } name)
        {
            Warn("Dieser Name ist nicht erlaubt !");
            return;
        }

        if (Mode == SaveLoadMode.Load)
        {
            if (SelectedEntry is { Kind: SaveEntryKind.Original } original && original.BaseName == name)
                Import(Path.Combine(directory, original.FileName));
            else if (!SaveFiles.Exists(directory, name) && File.Exists(SaveFiles.OriginalPathOf(directory, name)))
                Import(SaveFiles.OriginalPathOf(directory, name));
            else
                Load(directory, name);
        }
        else if (SaveFiles.Exists(directory, name))
            Ask(Pending.Overwrite, name);
        else
            Save(directory, name);
    }

    private SaveEntry? SelectedEntry => _selected >= 0 && _selected < _entries.Count ? _entries[_selected] : null;

    private void Warn(params string[] lines) => _message.Show(lines);

    private void Ask(Pending what, string name)
    {
        _pending = what;
        _pendingName = name;
    }

    private void AskDelete()
    {
        if (Directory is not { } directory)
            return;
        if (SaveFiles.NormalizeName(_name) is { } name && SaveFiles.Exists(directory, name))
            Ask(Pending.Delete, name);
    }

    private void UpdateConfirm(InputState input)
    {
        var yes = input.Pressed(Key.Enter) || input.Clicked(YesButton);
        var no = input.Pressed(Key.Escape) || input.RightClick || input.Clicked(NoButton);
        if (!yes && !no)
            return;
        var what = _pending;
        var name = _pendingName;
        _pending = Pending.None;
        if (!yes || Directory is not { } directory)
            return;
        if (what == Pending.Overwrite)
            Save(directory, name);
        else
            Delete(directory, name);
    }

    // ---- Laden, Sichern, Löschen -------------------------------------------------------------------

    private void Load(string directory, string name)
    {
        var shown = SaveFiles.FileNameOf(name).ToUpperInvariant();
        var path = SaveFiles.PathOf(directory, name);
        byte[] data;
        try
        {
            data = File.ReadAllBytes(path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Warn(["Die Datei", shown, "kann nicht geladen werden.", .. Wrap(File.Exists(path) ? "Sie ist nicht lesbar." : "Sie ist nicht vorhanden.")]);
            return;
        }

        var result = SaveGame.Load(data);
        if (result.WrongVersion)
        {
            // [ORIGINAL] Datei 0x5C559: Kennung stimmt, Version nicht → Kurzmeldung (0x0C0B8), Datei schließen, zurück zur
            // Auswahl (Integration Runde 12, Anschluss ui-meldungen Teil 3, UM-4). [APPROX] Der Grund (Schema) entfällt wie im
            // Original; das Remake unterscheidet „fehlt“, „neuer“ und „ohne Migration“ nur in SaveLoadResult.Error.
            var text = ShortMessageTexts.WrongVersion;
            _message.ShowShort(text.Line1, text.Line2);
            return;
        }
        if (result.Game is not { } game)
        {
            Warn(["Die Datei", shown, "kann nicht geladen werden.", .. Wrap(result.Error ?? "Unbekannter Fehler.")]);
            return;
        }

        Host.Game = game;
        Host.Show(new MainMenuScreen(Host));
    }

    /// <summary>
    /// Original-Spielstand importieren (Spur hok-import, Teil 3; <see cref="OriginalImport.TryImport"/>). Getippt gilt der
    /// Remake-Spielstand vor dem gleichnamigen HOK; ein Klick auf den HOK-Eintrag importiert ihn. Falsche Prüfsumme:
    /// „Der kleine Mogelguru“ (<see cref="MogelguruScreen"/>), es wird nichts geladen. [APPROX] Zufall: neue Saat
    /// (Feldkarte Regel 6) – <see cref="ScreenHost.Seed"/>, sonst die Uhr wie beim neuen Spiel.
    /// </summary>
    private void Import(string path)
    {
        var shown = Path.GetFileName(path).ToUpperInvariant();
        byte[] data;
        try
        {
            data = File.ReadAllBytes(path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Warn(["Die Datei", shown, "kann nicht geladen werden.", .. Wrap(File.Exists(path) ? "Sie ist nicht lesbar." : "Sie ist nicht vorhanden.")]);
            return;
        }

        var result = OriginalImport.TryImport(data, Assets.Database, Host.Seed ?? Environment.TickCount);
        if (result.ChecksumError)
        {
            Host.Show(new MogelguruScreen(Host, this));
            return;
        }
        if (result.Game is not { } game)
        {
            Warn(["Die Datei", shown, "kann nicht geladen werden.", .. Wrap(result.Error ?? "Unbekannter Fehler.")]);
            return;
        }

        Host.Game = game;
        Host.Show(new MainMenuScreen(Host));
    }

    private void Save(string directory, string name)
    {
        if (Host.Game is not { } game)
        {
            Warn("Achtung !", "Es läuft kein Spiel.", "Vorgang abgebrochen.");
            return;
        }
        byte[] data;
        try
        {
            data = SaveGame.Save(game);
        }
        catch (InvalidOperationException e)
        {
            Warn(["Achtung !", "Der Spielstand lässt sich nicht sichern.", .. Wrap(e.Message), "Vorgang abgebrochen."]);
            return;
        }
        try
        {
            SaveFiles.WriteAtomic(SaveFiles.PathOf(directory, name), data);
        }
        catch (IOException)
        {
            // [ORIGINAL] „Achtung ! Fehler beim Diskettenzugriff ! Vorgang abgebrochen.“ (hier: Dateizugriff)
            Warn("Achtung !", "Fehler beim Dateizugriff !", "Vorgang abgebrochen.");
            return;
        }
        Host.Show(_back);
    }

    private void Delete(string directory, string name)
    {
        try
        {
            File.Delete(SaveFiles.PathOf(directory, name));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Warn("Achtung !", "Fehler beim Dateizugriff !", "Vorgang abgebrochen.");
            return;
        }
        _name = "";
        _selected = -1;
        Refresh();
    }

    /// <summary>
    /// Bricht einen Text auf Zeilen um, die in das Meldungsfenster passen. [ORIGINAL] Die Breite 158 px ist die Linie unten
    /// des Fensters 0x4C246 (80 … 237 = 158 Pixel, <see cref="OriginalDialog"/>, Datei 0x4C246). [APPROX] Der Umbruch selbst: das
    /// Original bricht nicht um, seine Meldungen sind feste DGROUP-Zeilen (<c>docs/analyse/re-meldungen.md</c> §C: „Meldungen
    /// mit mehr als vier Zeilen (nur Remake …)“); nur die Fehlertexte mit Dateiname und Fehlertext des Remakes brauchen ihn.
    /// Mehr als diese Breite ist nicht zu belegen. Vorher 200 px, damit reichten Fehlertexte über den Rahmen (Restlücke #483).
    /// </summary>
    private string[] Wrap(string text)
    {
        const int maxWidth = 158;
        var lines = new List<string>();
        var line = "";
        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var next = line.Length == 0 ? word : line + " " + word;
            if (line.Length > 0 && Assets.Mini.Measure(next) > maxWidth)
            {
                lines.Add(line);
                line = word;
            }
            else
                line = next;
        }
        if (line.Length > 0)
            lines.Add(line);
        return [.. lines];
    }

    // ---- Zeichnen --------------------------------------------------------------------------------

    public override void Draw(Surface s)
    {
        s.Palette = Assets.Palette;
        s.Blit(Assets.Image(MainMenuScreen.Background), 0, 0);

        Widgets.Frame(s, Assets, TitleFrame, FrameStyle.Orange, Fill);
        Widgets.ShadowText(s, Assets.Bold, TitleFrame.X + TitleFrame.W / 2, TitleFrame.Y + 10,
            Mode == SaveLoadMode.Load ? "Laden" : "Speichern", Colors.White, Colors.Black);

        Widgets.Frame(s, Assets, ListFrame, FrameStyle.Orange, Fill);
        DrawList(s);
        Widgets.Frame(s, Assets, Panel, FrameStyle.Orange, Fill);
        DrawPanel(s);

        if (_pending != Pending.None)
            DrawConfirm(s);
        DrawInfo(s);
        _message.Draw(s, Assets);
    }

    private void DrawList(Surface s)
    {
        for (var column = 0; column < Columns; column++)
            DrawBar(s, FirstBarX + 1 + column * BarPitch);

        for (var column = 0; column < Columns; column++)
        {
            for (var row = 0; row < RowsPerColumn; row++)
            {
                if (EntryAt(column, row) is not { } entry)
                    continue;
                var selected = _selected == IndexAt(column, row);
                var color = selected ? Colors.White : IsSelectable(entry) ? Colors.Yellow : DirectoryColor;
                s.Text(Assets.Mini, FirstBarX + column * BarPitch + 7, FirstRowY + row * RowPitch, entry.DisplayName, color);
            }
        }

        DrawMarker(s);

        // [ORIGINAL] Pfeilpaar 0x4BA58 (kb\4): ungedrückt immer, gedrückt solange die Taste unten ist
        var arrows = Assets.Image(ArrowPair.Image);
        _pageArrows.DrawReleased(s, arrows, -1);
        _pageArrows.DrawReleased(s, arrows, +1);
        _pageArrows.Draw(s, arrows);
    }

    /// <summary>
    /// Roter Marker links neben dem Eintrag unter der Maus. [ORIGINAL] Trefferfeld 42 &lt; x &lt; 266, 53 &lt; y &lt; 166, Zeile
    /// (y − 54) / 7 (Datei 0x5E221–0x5E250); Spalte zählt hoch, solange 0x7A + 80 · Spalte &lt; x (0x5E252–0x5E264): x = 122 ist noch
    /// Spalte 0, x = 202 noch Spalte 1 (Review R14HI-06). Gezeichnet wird nur bei einem belegten Eintrag: Spalte · 16 + Zeile +
    /// Seitenanfang &lt; Zahl der Einträge (0x5E2C7–0x5E2DD); Fläche (43 + 80 · Spalte | 55 + 7 · Zeile), 3 × 6 (0x5E2E2–0x5E329).
    /// </summary>
    private void DrawMarker(Surface s)
    {
        if (_message.Visible || _pending != Pending.None)
            return;
        if (_mouseX <= 42 || _mouseX >= 266 || _mouseY <= 53 || _mouseY >= 166)
            return;
        var row = (_mouseY - 54) / RowPitch;
        var column = 0;
        while (column + 1 < Columns && 0x7A + BarPitch * column < _mouseX)
            column++;
        if (row >= RowsPerColumn || EntryAt(column, row) is null)
            return;
        var (x, y) = (43 + BarPitch * column, 55 + RowPitch * row);
        s.Fill(new Rect(x, y, 3, 6), MarkerColor);
        s.HLine(x, y, 2, MarkerLight);
        s.VLine(x, y, 6, MarkerLight);
        s.VLine(x + 2, y, 6, MarkerDark);
        s.HLine(x + 1, y + 5, 2, MarkerDark);
    }

    /// <summary>
    /// Spielstand-Info 0x5B661 (nur solange die rechte Taste gehalten wird). [ORIGINAL] Fenster (71 | 71) 8 × 3 Kacheln, Stil 3
    /// (Datei 0x5B73C–0x5B764); Zeile 1 „SPIELSTAND:“ + Pfad mittig zwischen x 72 und 261, Argument y 81 (Remake: Oberkante y 77), Farbe 7, Linie (77 | 83)–(254 | 83);
    /// Zeile 2 mittig, Argument y 90 (Oberkante 86), Farbe 1; je Manager Argument y = 99 + 7 · i (Oberkante 95 + 7 · i), Farbe 1: Name x 76, Verein x 133, Liga x 212 (0x5B767–0x5B9A5).
    /// </summary>
    private void DrawInfo(Surface s)
    {
        if (_info is not { } info)
            return;
        var mini = Assets.Mini;
        Widgets.Frame(s, Assets, InfoFrame, FrameStyle.Orange, Fill);
        InfoText(s, 167 - mini.Measure("SPIELSTAND:" + _infoPath) / 2, 77, "SPIELSTAND:" + _infoPath, Colors.White);
        s.HLine(77, 83, 178, Colors.White);
        InfoText(s, 167 - mini.Measure(info.DateLine) / 2, 86, info.DateLine, 1);
        for (var i = 0; i < info.Managers.Count; i++)
        {
            var y = 95 + 7 * i; // Argument y 99 + 7 · i = Unterkante: der Zeichensatz des Remakes setzt y an die Oberkante (−4, am Bild a338_3 gemessen)
            InfoText(s, 76, y, info.Managers[i].Name, 1);
            InfoText(s, 133, y, info.Managers[i].Club, 1);
            if (info.Managers[i].League is { } league)
                InfoText(s, 212, y, league, 1);
        }
    }

    /// <summary>Text des Infofelds mit schwarzem Schatten (1 Pixel nach rechts unten; am Bild <c>a338_3</c> gemessen, [APPROX] Versatz).</summary>
    private void InfoText(Surface s, int x, int y, string text, byte color)
    {
        s.Text(Assets.Mini, x + 1, y + 1, text, Colors.Black);
        s.Text(Assets.Mini, x, y, text, color);
    }

    /// <summary>
    /// Trennbalken einer Spalte: 5 × 113 Pixel bei (42 + 80 · Spalte | 54), abgeschrägt (hell 1 oben und links, dunkel 6 rechts und
    /// unten, Füllung 3). [ORIGINAL] am DOSBox-Bild <c>a338_1</c> Pixel für Pixel gemessen; das Löschen des Markers mit Farbe 3
    /// (Datei 0x5E334–0x5E377) stellt die Füllung wieder her. Vorher nach dem unscharfen Video um 1 Pixel versetzt und mit
    /// anderen Farben [APPROX].
    /// </summary>
    private static void DrawBar(Surface s, int x)
    {
        s.Fill(new Rect(x, BarTop, 5, BarHeight), 3);
        s.HLine(x, BarTop, 5, 1);
        s.VLine(x, BarTop, BarHeight, 1);
        s.VLine(x + 4, BarTop + 1, BarHeight - 1, 6);
        s.HLine(x + 1, BarTop + BarHeight - 1, 4, 6);
    }

    private void DrawPanel(Surface s)
    {
        var mini = Assets.Mini;
        Label(s, LabelX, DirectoryY, "VERZEICHNIS: " + (Directory is { } d ? SaveFiles.DisplayPath(d, 38) : "-"));
        Label(s, LabelX, FileNameY, "FILENAME      :");
        var nameX = LabelX + mini.Measure("FILENAME      :") + 6;
        var end = s.Text(mini, nameX, FileNameY, _name, Colors.White);
        if (_pending == Pending.None && !_message.Visible)
            s.HLine(end + 1, FileNameY + 6, 5, Colors.Yellow); // Eingabemarke (der Zeichensatz hat kein „_“)

        var letters = new[] { "A:", "B:", "C:", "D:", "E:", "F:", "G:" };
        for (var i = 0; i < letters.Length; i++)
        {
            var r = ButtonRect(i / 4, i % 4);
            TeamUi.DialogButton(s, Assets, r.X, r.Y, letters[i]);
        }
        var delete = DeleteButton;
        TeamUi.DialogButton(s, Assets, delete.X, delete.Y, "LÖSCHEN");
    }

    private void Label(Surface s, int x, int y, string text)
    {
        s.Text(Assets.Mini, x + 1, y + 1, text, LabelShadow);
        s.Text(Assets.Mini, x, y, text, LabelColor);
    }

    private void DrawConfirm(Surface s)
    {
        Widgets.Frame(s, Assets, ConfirmFrame, FrameStyle.Orange, Fill);
        var shown = SaveFiles.FileNameOf(_pendingName).ToUpperInvariant();
        s.TextCentered(Assets.Mini, 160, ConfirmFrame.Y + 16, shown, Colors.Yellow);
        s.TextCentered(Assets.Mini, 160, ConfirmFrame.Y + 28, _pending == Pending.Delete ? "löschen ?" : "überschreiben ?", Colors.White);
        TeamUi.DialogButton(s, Assets, YesButton.X, YesButton.Y, "Mach schon!");
        TeamUi.DialogButton(s, Assets, NoButton.X, NoButton.Y, "Lieber nicht.");
    }
}

/// <summary>Meldet die Dialoge unter „Gemischtes“ an.</summary>
internal static class SaveLoadMenuEntries
{
    [MenuEntry("Laden")]
    private static Screen Load(ScreenHost host, Screen back) => new SaveLoadScreen(host, back, SaveLoadMode.Load);

    [MenuEntry("Sichern")]
    private static Screen Save(ScreenHost host, Screen back) => new SaveLoadScreen(host, back, SaveLoadMode.Save);
}
