using EishockeyManager.Formats;
using EishockeyManager.UI.Screens;

namespace EishockeyManager.UI.Testing;

/// <summary>
/// Steuert die Oberfläche ohne Fenster: Klicks, Tasten und Texteingaben Frame für Frame,
/// Bildschirmfotos als PNG. Für Tests und für <c>emtool screens</c>.
/// </summary>
public sealed class ScreenDriver
{
    private readonly InputState _input = new();

    public ScreenDriver(ScreenHost host, string? outputDirectory = null)
    {
        Host = host;
        OutputDirectory = outputDirectory;
    }

    public ScreenHost Host { get; }
    public Surface Surface { get; } = new();
    public string? OutputDirectory { get; }

    public static ScreenDriver Create(GameData game, string? outputDirectory = null, int seed = 1993) =>
        new(new ScreenHost(new Assets(game)) { Seed = seed }, outputDirectory);

    public void Frame(Action<InputState>? setup = null)
    {
        _input.Reset();
        setup?.Invoke(_input);
        Host.Update(_input);
        Host.Draw(Surface);
    }

    public void Frames(int count)
    {
        for (var i = 0; i < count; i++)
            Frame();
    }

    public void Click(int x, int y) => Frame(i => { i.MouseX = x; i.MouseY = y; i.LeftClick = true; });
    public void RightClick(int x, int y) => Frame(i => { i.MouseX = x; i.MouseY = y; i.RightClick = true; });
    public void Move(int x, int y) => Frame(i => { i.MouseX = x; i.MouseY = y; });

    /// <summary>Linke Taste bei der ersten Position drücken, über die weiteren ziehen, dann loslassen.</summary>
    public void Drag(params (int X, int Y)[] points)
    {
        for (var k = 0; k < points.Length; k++)
        {
            var (x, y) = points[k];
            var first = k == 0;
            Frame(i => { i.MouseX = x; i.MouseY = y; i.LeftDown = true; i.LeftClick = first; });
        }
        Move(points[^1].X, points[^1].Y);
    }
    public void Type(string text) => Frame(i => i.Text = text);
    public void Press(Key key) => Frame(i => i.Keys.Add(key));
    public void Wheel(int delta) => Frame(i => i.Wheel = delta);

    /// <summary>Speichert den aktuellen Bildschirm als PNG (nur mit Ausgabeordner).</summary>
    public void Save(string name)
    {
        if (OutputDirectory is null)
            return;
        Directory.CreateDirectory(OutputDirectory);
        IndexedPngWriter.Save(Path.Combine(OutputDirectory, name + ".png"), Surface.ToImage(), Surface.Palette);
        Console.WriteLine($"  {name}.png");
    }

    // ---- Häufige Abläufe ------------------------------------------------------------------

    /// <summary>Titel → Spielvorbereitung → ein Manager mit Kopf, Name und optionalem Verein → Hauptmenü.</summary>
    public void StartNewGame(string name = "LetsShow", int head = 3, int? clubIndex = 42)
    {
        Host.Show(new TitleScreen(Host));
        Frame();
        Click(160, 120);
        Click(20, 60);
        var cell = SetupScreen.HeadCell(head);
        Click(cell.X + 5, cell.Y + 5);
        Type(name);
        Press(Key.Enter);
        if (clubIndex is { } club)
        {
            Click(150, 60);
            for (var p = 0; p < club / SetupScreen.ClubsPerPage; p++)
                Wheel(-1);
            var c = SetupScreen.ClubCell(club % SetupScreen.ClubsPerPage);
            Click(c.X + 5, c.Y + 5);
        }
        Click(SetupScreen.StartButton.X + 10, SetupScreen.StartButton.Y + 10);
        // Spur debpokal-auslosung: nach „Los jetzt!“ fragt das Spiel, ob die DEB-Pokal-Auslosung live gezeigt werden soll
        if (Host.Current is Screens.Competitions.DebCupDrawLiveScreen)
        {
            if (StopAtDebCupDraw)
                return;
            Press(Key.Escape); // „Kein Gedanke“
        }
        if (Host.Current is not MainMenuScreen)
            throw new InvalidOperationException("Spielstart fehlgeschlagen.");
    }

    /// <summary>
    /// <see cref="StartNewGame"/> hält an der Frage zur DEB-Pokal-Auslosung an, statt sie mit „Kein Gedanke“ zu beantworten.
    /// </summary>
    public bool StopAtDebCupDraw { get; set; }

    /// <summary>Höchstzahl der Tastendrücke, bis <see cref="PlayMatchDay"/> wieder im Hauptmenü sein muss.</summary>
    public int MatchDayFrameLimit { get; set; } = 1000;

    /// <summary>
    /// Spielt einen Spieltag im Schnelldurchlauf (im Hauptmenü <see cref="NextDay"/>, danach Enter) und kehrt ins Hauptmenü zurück.
    /// Wirft <see cref="TimeoutException"/>, wenn das Hauptmenü nicht innerhalb von <see cref="MatchDayFrameLimit"/>
    /// Tastendrücken wieder erreicht wird (statt still abzubrechen).
    /// </summary>
    public void PlayMatchDay()
    {
        Advance();
        for (var presses = 1; Host.Current is not MainMenuScreen; presses++)
        {
            if (presses >= MatchDayFrameLimit)
                throw new TimeoutException(
                    $"Spieltag nach {presses} Tastendrücken nicht beendet – Bildschirm {Host.Current?.GetType().Name ?? "(keiner)"} statt Hauptmenü.");
            Step();
        }
    }

    /// <summary>
    /// Ist auf dem aktuellen Bildschirm ein Meldungsfenster (<see cref="Screens.Team.MessageBox"/>) offen? Es schließt wie im
    /// Original nur per Linksklick (Datei 0x4C246, E3 Runde 12), Enter wirkt dort nicht.
    /// </summary>
    public bool MessageOpen => Host.Current switch
    {
        Screens.Team.ContractsScreen c => c.Message.Visible && c.Message.Short is null,
        Screens.Team.TransferScreen t => t.Message.Visible && t.Message.Short is null,
        Screens.Competitions.FriendliesScreen f => f.Message.Visible && f.Message.Short is null,
        Screens.SaveLoad.SaveLoadScreen l => l.MessageLines.Count > 0,
        _ => false,
    };

    /// <summary>Schließt ein offenes Meldungsfenster mit einem Linksklick in das Fenster (0x4C246).</summary>
    public void CloseMessage() => Click(OriginalDialog.Window.X + 20, OriginalDialog.Window.Y + 40);

    /// <summary>Ein Schritt außerhalb des Hauptmenüs: offene Meldung per Linksklick schließen, sonst Enter.</summary>
    private void Step()
    {
        if (MessageOpen)
            CloseMessage();
        else
            Press(Key.Enter);
    }

    /// <summary>
    /// „Tag weiter“: Klick aufs Vereinslogo der Infoleiste (<see cref="MainMenuScreen.ContinueLogo"/>, #205). Im Hot-Seat
    /// gibt das nur an den nächsten Manager ab. Nur im Hauptmenü.
    /// </summary>
    public void NextDay()
    {
        if (Host.Current is not MainMenuScreen)
            throw new InvalidOperationException($"„Tag weiter“ nur im Hauptmenü, nicht in {Host.Current?.GetType().Name ?? "(keiner)"}.");
        var logo = MainMenuScreen.ContinueLogo;
        Click(logo.X + logo.W / 2, logo.Y + logo.H / 2);
        // Bundestrainer mit weniger als 10 Kaderspielern (#217): die Kurzmeldung (ohne Knopf, M-K2) ablaufen lassen, „auto“ wählen
        // (GameState.AutoNominate) und das Logo noch einmal anklicken.
        if (Host.Current is MainMenuScreen { CoachMessage: { } coachMessage } && Host.Game is { NationalCoach: not null } game)
        {
            Frames(coachMessage.TotalFrames + 1); // Meldung schließt von selbst
            game.AutoNominate();
            Click(logo.X + logo.W / 2, logo.Y + logo.H / 2);
        }
    }

    /// <summary>
    /// Einen Schritt weiter, wo immer man steht: im Hauptmenü <see cref="NextDay"/>, sonst Enter (Spieltag,
    /// Meldungen, Auslosung …). Für Schleifen, die viele Tage durchlaufen. Die Leertaste hat im Remake keine
    /// Weiter-Funktion mehr (Entscheidung Nutzer, 31.10.2026, #205).
    /// </summary>
    public void Advance()
    {
        if (Host.Current is MainMenuScreen)
            NextDay();
        else
            Step();
    }

    /// <summary>Öffnet im Hauptmenü eine kleine Kachel über ihren Namen (siehe <see cref="MenuTiles"/>).</summary>
    public void OpenTile(string name)
    {
        for (var big = 0; big < MenuTiles.Small.Length; big++)
        {
            var index = Array.FindIndex(MenuTiles.Small[big], t => t.Name == name);
            if (index < 0)
                continue;
            var b = MainMenuScreen.BigTile(big);
            Click(b.X + 10, b.Y + 10);
            var s = MainMenuScreen.SmallTile(index);
            Click(s.X + 10, s.Y + 10);
            return;
        }
        throw new ArgumentException($"Unbekannte Kachel \"{name}\".", nameof(name));
    }
}
