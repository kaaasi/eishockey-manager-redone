using EishockeyManager.Core;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Screens.SaveLoad;

namespace EishockeyManager.UI.Screens.Statistics;

/// <summary>
/// Ablage der Bestenlisten je <see cref="ScreenHost"/>: im Speicher, und – wenn ein Ordner eingerichtet ist – als Dateien
/// <c>h.&lt;Schlüssel&gt;</c> im Format des Originals (464 Byte, <see cref="HighscoreTable"/>).
/// </summary>
/// <remarks>
/// [ORIGINAL] Datei <c>high\h.&lt;Schlüssel&gt;</c> im Spielordner (em.exe Datei 0x5D617). [APPROX] Das Remake legt sie neben die
/// Spielstände in den Ordner „Bestenliste“ des Benutzerprofils (<see cref="SaveContext.UserDirectory"/>, Geschwisterordner);
/// ohne eingerichteten Ordner (Tests, Werkzeuge) bleibt die Liste im Speicher des Hosts.
/// </remarks>
public sealed class HighscoreStore
{
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<ScreenHost, HighscoreStore> Stores = new();

    /// <summary>
    /// Kennung dieser Sitzung (Sekunden seit 1970 beim ersten Zugriff). [APPROX] Im Original steht an dieser Stelle der Wert
    /// DGROUP 0x2C4; seine Herkunft ist nicht geprüft. Einträge dieser Sitzung zeigt der Bildschirm weiß.
    /// </summary>
    public static uint SessionStamp { get; } = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    private readonly Dictionary<int, HighscoreTable> _tables = [];

    public static HighscoreStore For(ScreenHost host) => Stores.GetValue(host, _ => new HighscoreStore(host));

    private readonly ScreenHost _host;

    private HighscoreStore(ScreenHost host) => _host = host;

    /// <summary>Ordner der Dateien oder null (nur im Speicher). Standard: Ordner „Bestenliste“ neben den Spielständen des Benutzers.</summary>
    public string? Directory
    {
        get => _directory ?? DefaultDirectory();
        set => _directory = value;
    }

    private string? _directory;

    private string? DefaultDirectory()
    {
        var saves = SaveContext.For(_host).Directory;
        if (saves is null || !string.Equals(saves, SaveContext.UserDirectory, StringComparison.Ordinal))
            return null;
        var parent = Path.GetDirectoryName(saves.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return parent is null ? null : Path.Combine(parent, "Bestenliste");
    }

    /// <summary>Liste zum Schlüssel (aus Datei oder Speicher; eine fehlende oder kaputte Datei ergibt eine leere Liste).</summary>
    public HighscoreTable Load(int key)
    {
        if (_tables.TryGetValue(key, out var table))
            return table;
        table = new HighscoreTable();
        if (Directory is { } dir)
        {
            var path = Path.Combine(dir, HighscoreRules.FileName(key));
            try
            {
                if (File.Exists(path))
                    table = HighscoreTable.Parse(File.ReadAllBytes(path));
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // Das Original behandelt eine nicht lesbare Datei wie eine fehlende.
            }
        }
        _tables[key] = table;
        return table;
    }

    /// <summary>Schreibt die Liste (nur mit Ordner); Fehler beim Schreiben bleiben ohne Meldung wie beim Original (Ergebnis unbekannt).</summary>
    public void Save(int key, HighscoreTable table)
    {
        _tables[key] = table;
        if (Directory is not { } dir)
            return;
        try
        {
            System.IO.Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir, HighscoreRules.FileName(key)), table.ToBytes());
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
        }
    }

    /// <summary>
    /// Trägt die Manager des laufenden Spiels ein und speichert. [ORIGINAL] Aufruf mit „nur eintragen“ bei jedem Saisonwechsel
    /// (Datei 0x56647) und vor dem Anzeigen. Ohne Bestenliste (<see cref="HighscoreRules.Key"/> = null) geschieht nichts.
    /// </summary>
    public HighscoreTable? Record()
    {
        if (_host.Game is not { } game || HighscoreRules.Key(game.Setup) is not { } key)
            return null;
        var table = Load(key);
        HighscoreRules.Update(table, game, SessionStamp);
        Save(key, table);
        return table;
    }
}

/// <summary>
/// „Highscore“: die acht besten Einträge, Name | Verein | Punkte | (Meister/Pokal/Europa).
/// </summary>
/// <remarks>
/// Aufbau aus em.exe (Datei 0x5D90D) und am Video „Die Oberfläche von EM“ (8:40–8:55, <c>oberflaeche_0844</c>–<c>0855</c>) geprüft:
/// <list type="bullet">
/// <item>[ORIGINAL] Hintergrund kb\195; Titel „Highscore“ (Schrift Bold, Farbe 14, Schatten 1) zentriert 0 … 320, Grundlinie 15.</item>
/// <item>[ORIGINAL] Zeilen (Mini) für jeden Eintrag mit Punkten ≠ 0, Grundlinie 154 + 10 · Zeile: Name ab x = 56 (so lange gekürzt, bis er
///   höchstens 57 Pixel breit ist), Verein ab x = 115, Punkte fünfstellig aufgefüllt ab x = 207, „(X/X/X)“ ab x = 240; Schatten Farbe 4.
///   Farbe 10, Einträge dieser Sitzung (gleiche Kennung) Farbe 7.</item>
/// </list>
/// Eintrag und Aufruf zu den Zeitpunkten des Originals: Kachel Highscore (Datei 0x45773), Spielende (0x5642E), Saisonwechsel (0x56647).
/// [APPROX] Jede Taste und jeder Klick führt zurück. Die Punkte siehe <see cref="HighscoreRules"/>.
/// </remarks>
public sealed class HighscoreScreen : Screen
{
    public const int Background = 195;
    public const string Title = "Highscore";
    public const int TitleBaseline = 15;
    public const byte TitleColor = 14;
    public const byte TitleShadow = 1;

    /// <summary>
    /// Rechte Grenze der Titelzentrierung. [ORIGINAL] Der Aufruf (0x5D9B8–0x5D9D6) übergibt 0x140 = 320. [APPROX] Mit 320
    /// liefert <see cref="CompetitionWidgets.Centered"/> den Titel 1 Pixel zu weit links (x = 116 … 201 statt Video
    /// 117 … 202, oberflaeche_0844–0855): bei ungerader Textbreite rundet das Original offenbar anders. 321 trifft das
    /// Video wie bei <c>DebCupDrawScreen</c>; die Rundungsregel selbst ist nicht belegt, deshalb Korrektur nur für diesen
    /// Aufruf (alle übrigen Aufrufer von <c>Centered</c> bleiben unberührt).
    /// </summary>
    public const int TitleRight = 321;
    public const int FirstBaseline = 154;
    public const int RowPitch = 10;
    public const int NameX = 56;
    public const int NameMaxWidth = 57;
    public const int ClubX = 115;
    public const int ScoreX = 207;
    public const int TitlesX = 240;
    public const byte RowShadow = 4;
    public const byte RowColor = 10;
    public const byte NewRowColor = 7;

    private readonly Screen _back;

    /// <param name="table">Nur für Bildschirmfotos: feste Liste statt der des laufenden Spiels (dann ohne Eintragen).</param>
    public HighscoreScreen(ScreenHost host, Screen back, HighscoreTable? table = null) : base(host)
    {
        _back = back;
        _showTitles = host.Game is not { WorldOnly: true };
        Table = table ?? HighscoreStore.For(host).Record() ?? new HighscoreTable();
    }

    private readonly bool _showTitles;

    public HighscoreTable Table { get; }

    public override void Update(InputState input)
    {
        if (input.LeftClick || input.RightClick || input.Keys.Count > 0)
            Host.Show(_back);
    }

    public override void Draw(Surface s)
    {
        var background = Assets.Image(Background);
        s.Palette = background.Palette ?? Assets.Palette; // kb\195 bringt eine eigene Palette mit
        s.Blit(background, 0, 0);
        CompetitionWidgets.Centered(s, Assets.Bold, 0, TitleRight, TitleBaseline, Title, TitleColor, TitleShadow);

        var mini = Assets.Mini;
        for (var i = 0; i < Table.Entries.Count; i++)
        {
            var entry = Table.Entries[i];
            if (entry.IsEmpty)
                continue;
            var y = FirstBaseline + RowPitch * i;
            var color = entry.Stamp == HighscoreStore.SessionStamp ? NewRowColor : RowColor;
            CompetitionWidgets.Centered(s, mini, NameX, 0, y, Shorten(mini, entry.Name), color, RowShadow, left: true);
            CompetitionWidgets.Centered(s, mini, ClubX, 0, y, entry.Club, color, RowShadow, left: true);
            CompetitionWidgets.Centered(s, mini, ScoreX, 0, y, CompetitionWidgets.Number(entry.Score, 5), color, RowShadow, left: true);
            if (_showTitles)
                CompetitionWidgets.Centered(s, mini, TitlesX, 0, y, entry.TitleText, color, RowShadow, left: true);
        }
    }

    private static string Shorten(Formats.BitmapFont font, string text)
    {
        while (text.Length > 0 && font.Measure(text) > NameMaxWidth)
            text = text[..^1];
        return text;
    }
}

/// <summary>Anmeldung im Hauptmenü: Manager → Highscore.</summary>
internal static class HighscoreMenuEntries
{
    [MenuEntry("Highscore")]
    private static Screen Highscore(ScreenHost host, Screen back) => new HighscoreScreen(host, back);
}
