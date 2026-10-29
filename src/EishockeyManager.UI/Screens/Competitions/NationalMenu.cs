using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens.Competitions;

/// <summary>
/// Hauptmenü des Bundestrainers und Kachelsperren nach Spielmodus (Spur wettbewerbe-weltkugel).
/// </summary>
/// <remarks>
/// [ORIGINAL] Sperrtabelle DGROUP 0x5159: 7 Reihen (große Kacheln) × 6 Plätze, 1 = gesperrt; 42 Bytes für den Verein
/// (alle frei), 42 Bytes für den Zug des Nationalteams (Manager 5, Datei 0x44FBD). Im Spielmodus 2 sperrt das Original
/// zusätzlich Platz 6 der Reihen Statistiken, Tabellen, Paarungen und Platz 4 der Stärken (Datei 0x44FE8–0x4501D).
/// Gesperrte Kacheln zeigen das Symbol kb\3 (157 | 203) 38 × 28 und nehmen keinen Klick an (Datei 0x45B63).
/// </remarks>
public static class NationalMenu
{
    /// <summary>Symbol einer gesperrten Kachel in kb\3. [ORIGINAL] Datei 0x45B69 ff.</summary>
    public static readonly Rect LockedIcon = new(157, 203, 38, 28);

    /// <summary>
    /// Freie Kacheln des Bundestrainers. [ORIGINAL] Zweiter Teil der Tabelle DGROUP 0x5159 (+42):
    /// Manager alles gesperrt; Mannschaft frei außer „Verträge“; Statistiken nur „WM-Statistik“; Tabellen nur WM;
    /// Paarungen nur WM; Stärken nur Länderteams; Gemischtes alles frei.
    /// „Highscore“ (Platz 6 der Reihe Manager) überschreibt das Original beim Aufbau (Datei 0x45020–0x4504A) mit
    /// „DGROUP 0x256 = 0 oder (Bundestrainer und Modus 4)“: im Modus 4 ausdrücklich gesperrt (0x4502C), im Modus 1 über
    /// 0x256 = 0 – ohne Fahne gibt es keine Bestenliste (Datei 0x289A3). In beiden Modi also gesperrt (Video
    /// `bundestrainer_00042`, Spur bundestrainer-modus4).
    /// </summary>
    public static readonly IReadOnlySet<string> CoachTiles = new HashSet<string>(StringComparer.Ordinal)
    {
        "Aufstellung", "Taktik", "Transfer", "Training",
        "WM-Statistik", "Tabelle WM", "WM", "Stärken Länderteams",
        "Laden", "Sichern", "Einstellungen",
    };

    /// <summary>Im Spielmodus 2 („nur Fahne“) gesperrte Kacheln. [ORIGINAL] Datei 0x44FE8–0x4501D.</summary>
    public static readonly IReadOnlySet<string> WorldTiles = new HashSet<string>(StringComparer.Ordinal)
    {
        "WM-Statistik", "Tabelle WM", "WM", "Stärken Länderteams",
    };

    /// <summary>Kacheln des Bundestrainers, die erst nach der Ernennung etwas zu zeigen haben.</summary>
    private static readonly HashSet<string> NeedsCoach = new(StringComparer.Ordinal) { "Aufstellung", "Taktik", "Transfer", "Training" };

    /// <summary>Ist die kleine Kachel im aktuellen Menü gesperrt?</summary>
    public static bool IsLocked(GameState game, MenuTile tile)
    {
        if (tile.IsEmpty)
            return false;
        if (game.NationalCoachMenu)
            // [APPROX] Vor der Ernennung (Saisonbeginn bis zum ersten WM-Termin) bleibt auch die Mannschaft gesperrt.
            return !CoachTiles.Contains(tile.Name) || (game.NationalCoach is null && NeedsCoach.Contains(tile.Name));
        return (game.Mode == GameMode.ClubOnly && WorldTiles.Contains(tile.Name)) || TileLocks.IsLocked(game, tile); // Sperren der Spielvorbereitung (Spur tabellen-highscore)
    }

    /// <summary>
    /// Bildschirm einer Kachel im Menü des Bundestrainers: „Transfer“ = „Spieler in WM-Kader“ (Stub 0xF8:0x5C,
    /// Datei 0x457C0), „Training“ = „WM Vorbereitung“ (Stub 0xF8:0x57, Datei 0x457D9). Sonst null (normale Anmeldung).
    /// </summary>
    public static Screen? Open(GameState game, string tile, ScreenHost host, Screen back)
    {
        if (!game.NationalCoachMenu || game.NationalCoach is null)
            return null;
        return tile switch
        {
            "Transfer" => new NationalSquadScreen(host, back),
            "Training" => new FriendliesScreen(host, back),
            // [ORIGINAL] Gleiche Bildschirme wie beim Verein, aber mit dem WM-Kader (Datei 0x48E28, Spur nationalteam-2).
            "Aufstellung" => new NationalLineupScreen(host, back),
            "Taktik" => NationalLineupScreen.Tactics(host, back),
            _ => null,
        };
    }

    /// <summary>Mannschaft der Infoleiste: im Menü des Bundestrainers das Nationalteam, sonst der Verein des Managers.</summary>
    public static int Team(GameState game) =>
        game.NationalCoachMenu ? game.NationalCoach?.Nation ?? NationalTeams.Germany : game.ActiveManager.ClubIndex;

    /// <summary>Kontostand der Infoleiste: im Menü des Bundestrainers das Nationalkonto (DGROUP 0xA055).</summary>
    public static long Money(GameState game) =>
        game.NationalCoachMenu ? game.NationalAccount : game.ActiveManager.Money;
}
