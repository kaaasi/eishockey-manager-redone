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
    public const int LevelCount = 4;

    public string Name { get; set; } = "";

    /// <summary>Managerkopf 0–11 (kb\0, 3 Spalten × 4 Reihen), −1 = noch nicht gewählt.</summary>
    public int Head { get; set; }

    /// <summary>Gewählter Verein (Datenbankindex 0–47) oder null = zufällig nach Liga und Stärke.</summary>
    public int? ClubIndex { get; set; }

    /// <summary>Spiel-Level 0–3. Voreinstellung im Original: 3.</summary>
    public int Level { get; set; } = 3;

    public League StartLeague { get; set; } = League.Bundesliga;
    public StartStrength StartStrength { get; set; } = StartStrength.Mittelfeld;
}

/// <summary>Alle Angaben der Spielvorbereitung (Bildschirm kb\150).</summary>
public sealed class GameSetup
{
    public const int MaxManagers = 4;

    public List<ManagerSetup> Managers { get; } = [];

    /// <summary>Spielmodus links: DEB-Pokal an/aus.</summary>
    public bool DebCup { get; set; } = true;

    /// <summary>Spielmodus rechts: Europapokal und WM an/aus.</summary>
    public bool EuropeAndWorldCup { get; set; } = true;

    public GameDuration Duration { get; set; } = GameDuration.Unbegrenzt;

    public bool CanStart => Managers.Count > 0 && Managers.All(m => m.Name.Length > 0);

    /// <summary>Verein, der bereits von einem anderen Manager gewählt wurde?</summary>
    public bool IsClubTaken(int clubIndex, ManagerSetup? except = null) =>
        Managers.Any(m => m != except && m.ClubIndex == clubIndex);
}
