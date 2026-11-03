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
/// „SEITE: “ (0x270C), „LÖSCHEN“ (0x2714), „Die Datei … kann nicht geladen werden.“ (0x40CD), „Achtung ! Fehler beim
/// Diskettenzugriff ! Vorgang abgebrochen.“ (0x3EB0), „löschen ?“, „Mach schon!“, „Lieber nicht.“.
/// </para>
/// <para>
/// <b>Abweichungen [APPROX]:</b> Das Video zeigt nur das Laden-Bild und keine Bedienung. Belegt ist das Klickverhalten (em.exe 0x5DBE5: Linksklick auf eine Datei übernimmt den Namen, bei gleichem Namen führt
/// er aus; Rechtsklick auf eine Datei zeigt die Spielstand-Info 0x5B661 und lässt Namensfeld und Dialog unverändert; Rechtsklick
/// sonst bricht ab). Das Infofeld des Rechtsklicks fehlt hier [APPROX] (#326). Nicht belegt ist das Bild des Speichern-Dialogs (hier derselbe Aufbau mit Titel
/// „Speichern“). Hier: Klick wählt den Spielstand und trägt seinen Namen ein, Enter oder ein zweiter Klick führt aus
/// (Laden; Sichern überschreibt erst nach Rückfrage). Es gibt einen Ordner statt Laufwerken: die Laufwerksknöpfe sind
/// gezeichnet, haben aber keine Wirkung. Seitenpfeile und „SEITE:“ blättern, wenn mehr als 48 Einträge da sind (Lage von
/// „SEITE:“ nicht belegt). Die Pfeile ▲▼ sind nach den Pixeln des Videos nachgezeichnet.
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
    public static readonly Rect ListFrame = new(32, 48, 266, 134);
    public static readonly Rect Panel = new(24, 182, 289, 58);

    public const int Columns = 3;
    public const int RowsPerColumn = 16;
    public const int EntriesPerPage = Columns * RowsPerColumn;

    private const int FirstBarX = 41;
    private const int BarPitch = 80;
    private const int BarTop = 54;
    private const int BarHeight = 114;
    private const int FirstRowY = 56;
    private const int RowPitch = 7;

    /// <summary>Fläche eines Eintrags (Spalte 0–2, Zeile 0–15).</summary>
    public static Rect EntryRect(int column, int row) => new(FirstBarX + column * BarPitch + 5, FirstRowY + row * RowPitch, 74, RowPitch);

    public static readonly Rect PageUpRect = new(146, 166, 16, 14);
    public static readonly Rect PageDownRect = new(170, 166, 18, 14);

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

        if (input.Pressed(Key.Escape) || (input.RightClick && !TryRightClickEntry(input)))
        {
            Host.Show(_back);
            return;
        }

        if (input.Wheel != 0)
            ChangePage(input.Wheel > 0 ? -1 : 1);
        if (input.Pressed(Key.PageUp) || input.Clicked(PageUpRect))
            ChangePage(-1);
        if (input.Pressed(Key.PageDown) || input.Clicked(PageDownRect))
            ChangePage(1);

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
                if (EntryAt(column, row) is { Kind: SaveEntryKind.Save } entry)
                    ClickEntry(IndexAt(column, row), entry);
                return;
            }
        }
    }

    /// <summary>
    /// Rechtsklick auf einen Spielstand bricht nicht ab und ändert weder Namensfeld noch Auswahl; woanders bricht er ab.
    /// [ORIGINAL] em.exe 0x5DBE5 (Datei 0x5E5ED–0x5E667: Taste 3 auf einem Dateieintrag baut Verzeichnis + Name in einem
    /// lokalen Puffer, ruft die Spielstand-Info 0x5B661 und bleibt im Dialog; das Namensfeld [bp+6] bleibt unberührt.
    /// Taste 3 sonst bricht ab: 0x5E79F). [APPROX] Das Infofeld von 0x5B661 (Kopf des Spielstands) ist nicht nachgebaut (#326).
    /// </summary>
    private bool TryRightClickEntry(InputState input)
    {
        for (var column = 0; column < Columns; column++)
        {
            for (var row = 0; row < RowsPerColumn; row++)
            {
                if (input.MouseX < 0 || !EntryRect(column, row).Contains(input.MouseX, input.MouseY))
                    continue;
                return EntryAt(column, row) is { Kind: SaveEntryKind.Save };
            }
        }
        return false;
    }

    private void ChangePage(int delta) => _page = Math.Clamp(_page + delta, 0, PageCount - 1);

    /// <summary>Erster Klick wählt den Spielstand (Name erscheint), ein weiterer auf denselben (oder bei gleichem Namen im Feld) führt aus.</summary>
    private void ClickEntry(int index, SaveEntry entry)
    {
        // [ORIGINAL] em.exe 0x5E66A–0x5E68C: gleicher Name wie im Feld → ausführen, sonst Name übernehmen.
        if (_selected == index || string.Equals(_name, entry.BaseName, StringComparison.OrdinalIgnoreCase))
        {
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
            Load(directory, name);
        else if (SaveFiles.Exists(directory, name))
            Ask(Pending.Overwrite, name);
        else
            Save(directory, name);
    }

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
    /// Bricht einen Text auf Zeilen um, die in das Meldungsfenster passen. [APPROX] Breite 158 px = Linie unten des Fensters
    /// 0x4C246 (80 … 237, <see cref="OriginalDialog"/>); vorher 200 px, damit reichten Fehlertexte über den Rahmen (168 px,
    /// Restlücke #483; Integration Runde 12).
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
        _message.Draw(s, Assets);
    }

    private void DrawList(Surface s)
    {
        for (var column = 0; column < Columns; column++)
            DrawBar(s, FirstBarX + column * BarPitch);

        for (var column = 0; column < Columns; column++)
        {
            for (var row = 0; row < RowsPerColumn; row++)
            {
                if (EntryAt(column, row) is not { } entry)
                    continue;
                var selected = _selected == IndexAt(column, row);
                var color = selected ? Colors.White : entry.Kind == SaveEntryKind.Save ? Colors.Yellow : DirectoryColor;
                s.Text(Assets.Mini, FirstBarX + column * BarPitch + 7, FirstRowY + row * RowPitch, entry.DisplayName, color);
            }
        }

        DrawArrow(s, PageUpRect.X + 5, PageUpRect.Y + 4, up: true);
        DrawArrow(s, PageDownRect.X + 4, PageDownRect.Y + 4, up: false);
    }

    /// <summary>Trennbalken einer Spalte (5 Pixel breit) – Farben aus dem Referenzbild.</summary>
    private static void DrawBar(Surface s, int x)
    {
        s.VLine(x, BarTop, BarHeight, 39);
        s.VLine(x + 1, BarTop, BarHeight, 36);
        s.VLine(x + 2, BarTop, BarHeight, 3);
        s.VLine(x + 3, BarTop, BarHeight, 3);
        s.VLine(x + 4, BarTop, BarHeight, 43);
        s.HLine(x + 1, BarTop, 3, 35);
        s.HLine(x + 1, BarTop + 1, 3, 39);
        s.HLine(x, BarTop + BarHeight - 2, 5, 45);
        s.HLine(x, BarTop + BarHeight - 1, 5, 44);
    }

    /// <summary>
    /// Seitenpfeil ▲ oder ▼ (gold mit dunklem Rand). [APPROX] nach den Pixeln des Referenzbilds gezeichnet; im Original
    /// konnte keine passende Grafik gefunden werden.
    /// </summary>
    private static void DrawArrow(Surface s, int x, int y, bool up)
    {
        const byte dark = 48, light = 213, gold = 60;
        if (up)
        {
            s.HLine(x + 2, y, 3, dark);
            s.HLine(x + 3, y + 1, 2, light);
            s.HLine(x + 2, y + 2, 4, light);
            s.HLine(x + 1, y + 3, 6, light);
            s.HLine(x, y + 4, 8, gold);
            s.HLine(x + 1, y + 5, 6, dark);
        }
        else
        {
            s.HLine(x, y, 9, dark);
            s.HLine(x + 1, y + 1, 7, light);
            s.HLine(x + 2, y + 2, 6, light);
            s.HLine(x + 3, y + 3, 4, gold);
            s.HLine(x + 4, y + 4, 2, gold);
        }
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
        if (PageCount > 1)
            Label(s, 250, DirectoryY, $"SEITE: {_page + 1}");

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
