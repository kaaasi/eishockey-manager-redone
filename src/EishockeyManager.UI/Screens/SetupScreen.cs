using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.UI.Screens;

/// <summary>
/// Spielvorbereitung (Hintergrund kb\150). Koordinaten aus Videoaufnahmen des Originals vermessen.
///
/// Links bis zu vier Manager: Kopf → Kopfauswahl, Namensfeld → Name tippen, Logo → Vereinsauswahl.
/// Rechts Spielmodus (Fahne = Liga und DEB-Pokal, Weltkugel = Europapokal und WM), Spiel-Level, Spiel-Dauer, Start-Liga, Start-Stärke,
/// unten Notizbuch (Laden) und „Los jetzt!“.
/// </summary>
public sealed class SetupScreen : Screen
{
    public const int Background = 150;
    public const int Elements = 1;   // kb\1: Level-Figuren, Lineale, Schriftzüge, Knöpfe
    public const int PickerPanel = 7; // kb\7: beiges Auswahlfenster
    public const int SlotPitch = 51;

    // Manager-Zeilen
    public static Rect HeadArea(int i) => new(3, 38 + i * SlotPitch, 44, 44);
    public static Rect NameArea(int i) => new(47, 38 + i * SlotPitch, 87, 44);
    public static Rect LogoArea(int i) => new(134, 38 + i * SlotPitch, 44, 44);
    public static Rect NameInput(int i) => new(47, 42 + i * SlotPitch, 86, 7);

    // Rechte Seite
    /// <summary>Knopf „Fahne“ (Bit 0) und „Weltkugel“ (Bit 1), gezeichnet aus kb\1 (Quelle x = 113).</summary>
    public static readonly Rect ModeFlag = new(193, 47, 57, 13);
    public static readonly Rect ModeGlobe = new(259, 47, 57, 13);

    /// <summary>
    /// Klickflächen der Modusknöpfe. [ORIGINAL] Datei 0x286F6 ff.: x &gt; 190, 45 &lt; y &lt; 61; links davon bis x &lt; 254
    /// die Fahne, ab 254 die Weltkugel.
    /// </summary>
    public static readonly Rect ModeFlagHit = new(191, 46, 63, 15);
    public static readonly Rect ModeGlobeHit = new(254, 46, 66, 15);
    public static Rect Field(int row) => new(195, 81 + row * 28, 110, 10);
    public static Rect ArrowUp(int row) => new(310, 78 + row * 28, 10, 7);
    public static Rect ArrowDown(int row) => new(310, 87 + row * 28, 10, 7);
    public static readonly Rect LoadButton = new(194, 194, 54, 38);
    public static readonly Rect StartButton = new(258, 194, 57, 38);

    // Auswahlfenster
    public const int PickerX = 184;
    public const int PickerY = 32;
    public static Rect HeadCell(int head) => new(193 + head % 3 * 41, 39 + head / 3 * 41, 40, 40);
    public static Rect ClubCell(int k) => new(190 + k % 3 * 40, 41 + k / 3 * 40, 38, 38);
    public static readonly Rect ScrollBar = new(310, 39, 10, 163);
    public const int PickerTextCenter = 252;

    /// <summary>Textbereich der Hinweise unter der Kopfwahl. [ORIGINAL] em.exe Datei 0x28D02/0x28D06.</summary>
    private const int HintX1 = 188, HintX2 = 319;
    public const int ClubsPerPage = 12;
    public static int ClubPages => NewGame.SelectableClubs / ClubsPerPage;

    private enum Mode { Normal, HeadPicker, NameEntry, ClubPicker }

    private readonly GameSetup _setup = new();
    private Mode _mode = Mode.Normal;
    private int _selected = -1;
    private int _clubPage;
    private int _mouseX = -1, _mouseY = -1;
    private int _frame;
    private string? _message;

    /// <summary>
    /// Spielstart-Seed, fest ab dem Aufbau des Bildschirms: Er bestimmt die Startmischung der Vereinsauswahl (R-K5,
    /// <see cref="NewGame.StartOrder"/>) und den Spielstart. Die Mischung leitet sich nur daraus ab (kein Zufallszug beim Öffnen).
    /// </summary>
    private readonly int _seed;
    private readonly int[] _places;

    public SetupScreen(ScreenHost host) : base(host)
    {
        _seed = host.Seed ?? Environment.TickCount;
        _places = NewGame.StartOrder(_seed);
    }

    public GameSetup Setup => _setup;

    private ManagerSetup? Selected => _selected >= 0 && _selected < _setup.Managers.Count ? _setup.Managers[_selected] : null;

    public override void Update(InputState input)
    {
        _mouseX = input.MouseX;
        _mouseY = input.MouseY;
        _frame++;
        if (input.LeftClick || input.Keys.Count > 0)
            _message = null;

        switch (_mode)
        {
            case Mode.HeadPicker: UpdateHeadPicker(input); break;
            case Mode.NameEntry: UpdateNameEntry(input); break;
            case Mode.ClubPicker: UpdateClubPicker(input); break;
            default: UpdateNormal(input); break;
        }
    }

    private void UpdateNormal(InputState input)
    {
        if (input.Pressed(Key.Escape))
        {
            Host.Show(new TitleScreen(Host));
            return;
        }

        for (var i = 0; i < GameSetup.MaxManagers; i++)
        {
            var slot = new Rect(3, 38 + i * SlotPitch, 175, 44);
            if (i < _setup.Managers.Count)
            {
                if (input.RightClicked(NameArea(i)))
                {
                    _setup.Managers.RemoveAt(i);
                    _selected = Math.Min(_selected, _setup.Managers.Count - 1);
                    return;
                }
                if (input.Clicked(HeadArea(i))) { _selected = i; _mode = Mode.HeadPicker; return; }
                if (input.Clicked(NameArea(i))) { _selected = i; _mode = Mode.NameEntry; return; }
                if (input.Clicked(LogoArea(i))) { _selected = i; OpenClubPicker(); return; }
            }
            else if (i == _setup.Managers.Count && input.Clicked(slot))
            {
                // Im Original bleibt das Kopffeld bis zur Auswahl leer; die Start-Liga gilt für alle Manager (L1, DGROUP 0x11A).
                _setup.Managers.Add(new ManagerSetup { Head = -1, StartLeague = _setup.StartLeague });
                _selected = i;
                _mode = Mode.HeadPicker;
                return;
            }
        }

        // Spielmodus (Spur wettbewerbe-weltkugel): Die Fahne bleibt bei mehreren Managern gesperrt (GameSetup).
        if (input.Clicked(ModeFlagHit))
            _setup.ToggleClubCompetitions();
        else if (input.Clicked(ModeGlobeHit))
            _setup.ToggleEuropeAndWorldCup();

        for (var row = 0; row < 4; row++)
        {
            if (input.Clicked(ArrowUp(row)))
                Step(row, -1);
            else if (input.Clicked(ArrowDown(row)) || input.Clicked(Field(row)))
                Step(row, +1);
        }

        if (input.Clicked(LoadButton))
        {
            Host.Show(new SaveLoad.SaveLoadScreen(Host, this, SaveLoad.SaveLoadMode.Load));
            return;
        }

        if (input.Clicked(StartButton) || input.Pressed(Key.Enter))
            TryStart();
    }

    /// <summary>
    /// Regler verstellen: begrenzt, kein Umlauf, „eine Saison“ nur mit Start-Liga Bundesliga (U15-b, siehe
    /// <see cref="GameSetup.Step"/>; [ORIGINAL] Datei 0x2890E–0x2891C).
    /// </summary>
    private void Step(int row, int delta) => _setup.Step((SetupSlider)row, delta, Selected);

    private int FirstFreeHead()
    {
        for (var h = 0; h < ManagerSetup.HeadCount; h++)
            if (_setup.Managers.All(m => m.Head != h))
                return h;
        return 0;
    }

    private void TryStart()
    {
        if (_setup.Managers.Count == 0)
        {
            _message = "Bitte zuerst einen Manager anlegen.";
            return;
        }
        // [ORIGINAL] Datei 0x295DF: Ohne Spielmodus nimmt „Los jetzt!“ keinen Klick an (keine Meldung).
        if (_setup.Mode is not { } mode)
            return;
        // [APPROX] Meldung nicht aus dem Original: „nur Weltkugel“ gibt es nur für einen Manager (Bundestrainer).
        if (mode == GameMode.WorldOnly && _setup.Managers.Count > 1)
        {
            _message = "Nur Weltkugel: nur ein Manager.";
            return;
        }
        for (var i = 0; i < _setup.Managers.Count; i++)
        {
            if (_setup.Managers[i].Name.Length == 0)
                _setup.Managers[i].Name = $"Manager {i + 1}";
            if (_setup.Managers[i].Head < 0)
                _setup.Managers[i].Head = FirstFreeHead();
        }

        Host.Game = NewGame.Create(Assets.Database, _setup, _seed);
        // [ORIGINAL] #195: Im Modus „nur Weltkugel“ läuft nach „Los jetzt!“ zuerst die Tagesfunktion Datei 0x54C9F (Sprung
        // 0x54CEE zum WM-Termin, Ernennung), das Hauptmenü 0x44E22 kommt erst am Tagesende (0x555A3) – DOSBox 06.
        if (Host.Game.WorldOnly)
            Host.Game.Continue();
        // [ORIGINAL] Datei 0x43A8C: die Auslosung der ersten DEB-Pokal-Runde läuft mit Anzeige vor dem Hauptmenü (Spur debpokal-auslosung)
        Host.Show(Competitions.DebCupDrawFlow.AtSeasonStart(Host, new MainMenuScreen(Host)));
    }

    private void UpdateHeadPicker(InputState input)
    {
        if (input.Pressed(Key.Escape) || input.RightClick)
        {
            _mode = Mode.Normal;
            return;
        }
        for (var h = 0; h < ManagerSetup.HeadCount; h++)
        {
            if (input.Clicked(HeadCell(h)) && Selected is { } m)
            {
                m.Head = h;
                _mode = m.Name.Length == 0 ? Mode.NameEntry : Mode.Normal;
                return;
            }
        }
    }

    private void UpdateNameEntry(InputState input)
    {
        if (Selected is not { } m)
        {
            _mode = Mode.Normal;
            return;
        }
        foreach (var ch in input.Text)
        {
            if (m.Name.Length < ManagerSetup.MaxNameLength && IsAllowed(ch))
                m.Name += ch;
        }
        if (input.Pressed(Key.Backspace) && m.Name.Length > 0)
            m.Name = m.Name[..^1];
        if (input.Pressed(Key.Enter) || input.Pressed(Key.Escape) || (input.LeftClick && !input.Hover(NameInput(_selected))))
            _mode = Mode.Normal;
    }

    private static bool IsAllowed(char ch) =>
        ch is >= ' ' and < '\u007F' and not ('[' or '\\' or ']' or '{' or '|' or '}' or '~') || "ÄÖÜäöüß".Contains(ch);

    private void OpenClubPicker()
    {
        _mode = Mode.ClubPicker;
        var club = Selected?.ClubIndex;
        _clubPage = club is { } c ? CellOf(c).Page : 0;
    }

    private void UpdateClubPicker(InputState input)
    {
        if (input.Pressed(Key.Escape) || input.RightClick)
        {
            _mode = Mode.Normal;
            return;
        }

        var pageDelta = 0;
        if (input.Clicked(ScrollBar))
            pageDelta = input.MouseY < ScrollBar.Y + ScrollBar.H / 2 ? -1 : 1;
        if (input.Wheel != 0)
            pageDelta = input.Wheel > 0 ? -1 : 1;
        if (input.Pressed(Key.PageUp) || input.Pressed(Key.Up))
            pageDelta = -1;
        if (input.Pressed(Key.PageDown) || input.Pressed(Key.Down))
            pageDelta = 1;
        // [ORIGINAL] em.exe Datei 0x28E4B–0x28E64 (R-K5): die Seite blättert zyklisch (−1 von 0 → 3, +1 von 3 → 0).
        _clubPage = (_clubPage + pageDelta + ClubPages) % ClubPages;

        for (var k = 0; k < ClubsPerPage; k++)
        {
            if (!input.Clicked(ClubCell(k)) || Selected is not { } m)
                continue;
            var index = ClubAt(k);
            if (_setup.IsClubTaken(index, m))
                return;
            m.ClubIndex = index;
            _mode = Mode.Normal;
            return;
        }
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x28030 (R-K5, #549): Seite p zeigt die Vereinssätze 12 · p … 12 · p + 11 in Satzfolge; der Aufrufer
    /// blättert zyklisch (Datei 0x28E4B). Die Sätze sind beim Öffnen schon gemischt (Ligamischung Datei 0x43A17 vor der
    /// Spielvorbereitung Datei 0x43A61): Seiten 0–2 = Vereine der Liga in der Startmischung, Seite 3 (ohne Liga) in
    /// Datenbankfolge. Belege `oberflaeche_0128`/`_0133` (Seite 0/1 permutiert), `_0136` (Seite 3, Zelle 8 = HEC Bonn = DB 44),
    /// <c>docs/analyse/re-runde-13.md</c>, R-K5. Zufallsquelle `[APPROX]` siehe <see cref="NewGame.StartOrder"/>.
    /// </summary>
    private int ClubAt(int cell) => _places[_clubPage * ClubsPerPage + cell];

    /// <summary>Seite und Zelle, in der die Vereinsauswahl den Verein zeigt (für Bildschirmabläufe und Tests).</summary>
    public (int Page, int Cell) CellOf(int clubIndex)
    {
        var place = Array.IndexOf(_places, clubIndex);
        return (place / ClubsPerPage, place % ClubsPerPage);
    }

    public override void Draw(Surface s)
    {
        s.Palette = Assets.Palette;
        s.Blit(Assets.Image(Background), 0, 0);
        DrawManagers(s);

        switch (_mode)
        {
            case Mode.HeadPicker: DrawHeadPicker(s); break;
            case Mode.ClubPicker: DrawClubPicker(s); break;
            default: DrawSettings(s); break;
        }

        if (_mode == Mode.NameEntry)
            DrawNameInput(s);

        if (_message is not null)
        {
            var w = Assets.Mini.Measure(_message) + 12;
            var box = new Rect(Surface.Width / 2 - w / 2, 220, w, 13);
            s.Fill(box, Colors.Black);
            s.Outline(box, Colors.Yellow);
            s.TextCentered(Assets.Mini, Surface.Width / 2, 224, _message, Colors.White);
        }
    }

    private void DrawManagers(Surface s)
    {
        var db = Assets.Database;
        for (var i = 0; i < _setup.Managers.Count; i++)
        {
            var m = _setup.Managers[i];
            var y = 40 + i * SlotPitch;
            if (m.Head >= 0)
                Widgets.Head(s, Assets, m.Head, 5, y);
            if (i == _selected && _mode != Mode.Normal)
            {
                s.HLine(5, y - 1, 40, Colors.Yellow);
                s.HLine(5, y + 40, 40, Colors.Yellow);
            }
            if (_mode != Mode.NameEntry || i != _selected)
                s.TextCentered(Assets.Mini, 90, y + 4, m.Name, Colors.White);
            if (m.ClubIndex is { } c)
            {
                s.TextCentered(Assets.Mini, 90, y + 12, db[c].Name, Colors.White);
                Widgets.Logo(s, Assets, db[c].LogoGraphic, 138, y + 1);
            }
        }
    }

    private void DrawSettings(Surface s)
    {
        var elements = Assets.Image(Elements);
        // [ORIGINAL] Datei 0x28506/0x28543: Quelle y = 76 + 13 · Bit 0 bzw. 102 + 13 · Bit 1.
        s.Blit(elements, new Rect(113, _setup.ClubCompetitions ? 89 : 76, 57, 13), ModeFlag.X, ModeFlag.Y);
        s.Blit(elements, new Rect(113, _setup.EuropeAndWorldCup ? 115 : 102, 57, 13), ModeGlobe.X, ModeGlobe.Y);

        var m = Selected ?? new ManagerSetup();
        int[] values = [m.Level, (int)_setup.Duration, (int)_setup.StartLeague, (int)m.StartStrength];
        for (var row = 0; row < values.Length; row++)
        {
            var strip = SetupSliders.Strip((SetupSlider)row, values[row]);
            s.Blit(elements, new Rect(0, strip * 10, 110, 10), Field(row).X, Field(row).Y);
        }

        s.Blit(elements, new Rect(113, 38, 54, 38), LoadButton.X, LoadButton.Y);
    }

    private void DrawNameInput(Surface s)
    {
        if (Selected is not { } m)
            return;
        var r = NameInput(_selected);
        s.Fill(r, 6);
        s.HLine(r.X, r.Y, r.W, 241);
        s.VLine(r.X, r.Y, r.H, 241);
        var x = s.Text(Assets.Mini, r.X + 2, r.Y + 1, m.Name, Colors.White);
        if (_frame / 15 % 2 == 0)
            s.Fill(new Rect(x + 1, r.Y + 1, 2, 5), Colors.White);
    }

    private void DrawHeadPicker(Surface s)
    {
        s.Blit(Assets.Image(PickerPanel), PickerX, PickerY);
        s.Blit(Assets.Image(0), PickerX + 9, PickerY + 7);
        for (var h = 0; h < ManagerSetup.HeadCount; h++)
        {
            if (HeadCell(h).Contains(_mouseX, _mouseY))
                s.Outline(HeadCell(h), Colors.Red);
        }
        // [ORIGINAL] em.exe Datei 0x28CF8–0x28D43: 0x30:0x885 mit x 188 … 319, Grundlinie 214 und 222, Farbe 0, ohne
        // Schatten (Spur pixel-verein, Vorlage speedrun_t1_00056).
        Competitions.CompetitionWidgets.Centered(s, Assets.Mini, HintX1, HintX2, 214, "Suchen Sie sich einen", Colors.Black, null);
        Competitions.CompetitionWidgets.Centered(s, Assets.Mini, HintX1, HintX2, 222, "passenden Kopf aus.", Colors.Black, null);
    }

    private void DrawClubPicker(Surface s)
    {
        var db = Assets.Database;
        s.Blit(Assets.Image(PickerPanel), PickerX, PickerY);
        string? hover = null;
        for (var k = 0; k < ClubsPerPage; k++)
        {
            var club = db[ClubAt(k)];
            var cell = ClubCell(k);
            Widgets.Logo(s, Assets, club.LogoGraphic, cell.X, cell.Y);
            if (Selected is { } m && _setup.IsClubTaken(club.Index, m))
                s.Outline(cell, Colors.Black);
            if (cell.Contains(_mouseX, _mouseY))
            {
                s.Outline(cell, Colors.Red);
                hover = club.Name;
            }
        }
        s.Blit(Assets.Image(4), new Rect(0, 0, 10, 163), ScrollBar.X, ScrollBar.Y);
        hover ??= Selected?.ClubIndex is { } sel ? db[sel].Name : null;
        if (hover is not null)
            s.TextCentered(Assets.Mini, PickerTextCenter, 220, hover, Colors.Black);
    }
}
