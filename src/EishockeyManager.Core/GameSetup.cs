namespace EishockeyManager.Core;

/// <summary>Spielklassen in der Reihenfolge des Originals (Index = Zeile in kb\1).</summary>
public enum League
{
    Bundesliga = 0,
    ZweiteLiga = 1,
    Oberliga = 2,
}

/// <summary>Start-Stärke: aus welchem Drittel der Liga ein zufälliger Verein gewählt wird.</summary>
public enum StartStrength
{
    OberesDrittel = 0,
    Mittelfeld = 1,
    Abstiegskampf = 2,
}

/// <summary>Spieldauer (Index = Lineal in kb\1).</summary>
public enum GameDuration
{
    EineSaison = 0,
    DreiSaisons = 1,
    FuenfSaisons = 2,
    Unbegrenzt = 3,
}

/// <summary>Einstellungen eines Managers in der Spielvorbereitung.</summary>
public sealed class ManagerSetup
{
    public const int MaxNameLength = 16;
    public const int HeadCount = 12;
    /// <summary>
    /// Spiel-Level 0–5. [ORIGINAL] Höchstwert DGROUP 0x11C = 5, sechs Level-Figuren in kb\1 (Streifen 0–5).
    /// </summary>
    public const int LevelCount = 6;

    public string Name { get; set; } = "";

    /// <summary>Managerkopf 0–11 (kb\0, 3 Spalten × 4 Reihen), −1 = noch nicht gewählt.</summary>
    public int Head { get; set; }

    /// <summary>Gewählter Verein (Datenbankindex 0–47) oder null = zufällig nach Liga und Stärke.</summary>
    public int? ClubIndex { get; set; }

    /// <summary>
    /// Spiel-Level 0–5, Vorgabe 3 ([ORIGINAL] DGROUP 0x118). Das Spiel rechnet mit „5 − Level“ (DGROUP 0xED8),
    /// Level 5 heißt z. B. Training immer frei. [APPROX] Im Original ist der Level ein einziger Wert für alle
    /// Manager; das Remake führt ihn je Manager.
    /// </summary>
    public int Level { get; set; } = 3;

    public League StartLeague { get; set; } = League.Bundesliga;
    public StartStrength StartStrength { get; set; } = StartStrength.Mittelfeld;
}

/// <summary>Die vier Regler rechts in der Spielvorbereitung, in der Reihenfolge der Setup-Werte DGROUP 0x118–0x11B.</summary>
public enum SetupSlider
{
    Level = 0,
    Duration = 1,
    StartLeague = 2,
    StartStrength = 3,
}

/// <summary>
/// Verhalten der Regler wie im Original. [ORIGINAL] Overlay „Spielstart“ ab Datei 0x2890A in em.exe:
/// Wert ± 1, unter 0 → 0, über dem Höchstwert (DGROUP 0x11C ff.: 5, 3, 2, 2) → Höchstwert. Kein Umlauf.
/// </summary>
public static class SetupSliders
{
    private static readonly int[] Maxima = [5, 3, 2, 2];
    private static readonly int[] FirstStrips = [0, 6, 10, 13];

    /// <summary>Höchstwert des Reglers. [ORIGINAL] DGROUP 0x11C.</summary>
    public static int Maximum(SetupSlider slider) => Maxima[(int)slider];

    /// <summary>Einen Schritt verstellen, an beiden Enden begrenzt.</summary>
    public static int Step(SetupSlider slider, int value, int delta) => Math.Clamp(value + delta, 0, Maximum(slider));

    /// <summary>
    /// Streifen (je 10 Pixel hoch) in kb\1 für den Wert. [ORIGINAL] Wert + Tabelle DGROUP 0x4B3E (0, 6, 10, 13),
    /// gezeichnet bei (195 | 81 + 28 · Zeile).
    /// </summary>
    public static int Strip(SetupSlider slider, int value) => FirstStrips[(int)slider] + value;
}

/// <summary>Alle Angaben der Spielvorbereitung (Bildschirm kb\150).</summary>
public sealed class GameSetup
{
    public const int MaxManagers = 4;

    public List<ManagerSetup> Managers { get; } = [];

    /// <summary>
    /// Spielmodus links, deutsche Fahne (Bit 0): Vereinssaison mit Liga und DEB-Pokal.
    /// [ORIGINAL] Datei 0x28717 ff., Vorgabe an (Datei 0x28191). Umschalten über <see cref="ToggleClubCompetitions"/>.
    /// </summary>
    public bool ClubCompetitions { get; private set; } = true;

    /// <summary>
    /// Spielmodus rechts, Weltkugel (Bit 1): Europapokal und WM. [ORIGINAL] Datei 0x28741 ff., Vorgabe an.
    /// Umschalten in der Spielvorbereitung über <see cref="ToggleEuropeAndWorldCup"/>.
    /// </summary>
    public bool EuropeAndWorldCup { get; set; } = true;

    /// <summary>
    /// [APPROX] Kein Schalter des Originals (das Remake deutete den linken Knopf früher so): DEB-Pokal in einem
    /// Modus mit Vereinssaison abschalten. Die Spielvorbereitung setzt ihn nicht; er dient Tests, die ohne Pokal rechnen.
    /// </summary>
    public bool DebCup { get; set; } = true;

    public GameDuration Duration { get; set; } = GameDuration.Unbegrenzt;

    /// <summary>Knopfbits wie im Original: Bit 0 Fahne, Bit 1 Weltkugel.</summary>
    public int ModeBits => (ClubCompetitions ? 1 : 0) | (EuropeAndWorldCup ? 2 : 0);

    /// <summary>Spielmodus (DGROUP 0xD0B9) oder null, wenn kein Knopf gedrückt ist.</summary>
    public GameMode? Mode => GameModes.FromBits(ModeBits);

    /// <summary>
    /// Linker Knopf (Fahne). [ORIGINAL] Datei 0x28717 ff.: Bei mehr als einem Manager mit Namen bleibt der Klick
    /// wirkungslos – nur ein einzelner Manager kann „nur Weltkugel“ wählen (Bundestrainer ist Manager 0).
    /// </summary>
    public bool ToggleClubCompetitions()
    {
        if (Managers.Count(m => m.Name.Length > 0) > 1)
            return false;
        ClubCompetitions = !ClubCompetitions;
        return true;
    }

    /// <summary>Rechter Knopf (Weltkugel). [ORIGINAL] Datei 0x28741 ff.: ohne Bedingung.</summary>
    public bool ToggleEuropeAndWorldCup()
    {
        EuropeAndWorldCup = !EuropeAndWorldCup;
        return true;
    }

    /// <summary>
    /// „Los jetzt!“ ist möglich: alle Manager benannt, ein Spielmodus gewählt ([ORIGINAL] Datei 0x295DF: ohne Knopf
    /// kein Start). [APPROX] „nur Weltkugel“ nur mit genau einem Manager – das Original verhindert mehrere Manager
    /// dort nur über die Sperre der Fahne, nicht beim späteren Hinzufügen.
    /// </summary>
    public bool CanStart => Managers.Count > 0 && Managers.All(m => m.Name.Length > 0) && Mode is { } mode
        && (mode != GameMode.WorldOnly || Managers.Count == 1);

    /// <summary>Verein, der bereits von einem anderen Manager gewählt wurde?</summary>
    public bool IsClubTaken(int clubIndex, ManagerSetup? except = null) =>
        Managers.Any(m => m != except && m.ClubIndex == clubIndex);
}
