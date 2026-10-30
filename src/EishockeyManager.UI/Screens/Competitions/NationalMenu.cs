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
    /// „Highscore“ (Platz 6 der Reihe Manager) steht nicht in dieser Liste: Das Original überschreibt den Platz beim
    /// Aufbau (Datei 0x45020–0x4504A) mit „DGROUP 0x256 = 0 oder (Zug Manager 5 und Modus 4)“, siehe
    /// <see cref="CoachHighscoreLocked"/>.
    /// Videobeleg Modus 4 (Spur pixel-verein, Pixelvergleich mit `pixel-verein-bundestrainer`): Reihen Manager
    /// `eisbaeren_f24_00049`, Mannschaft `_00051`, Statistiken `_00053`, Tabellen `_00057` – gleiche Sperren wie hier.
    /// </summary>
    public static readonly IReadOnlySet<string> CoachTiles = new HashSet<string>(StringComparer.Ordinal)
    {
        "Aufstellung", "Taktik", "Transfer", "Training",
        "WM-Statistik", "Tabelle WM", "WM", "Stärken Länderteams",
        "Laden", "Sichern", "Einstellungen",
    };

    /// <summary>
    /// Im Spielmodus 2 („nur Fahne“) gesperrte Kacheln. [ORIGINAL] Datei 0x44FE8–0x4501D; Video: WM-Kacheln gesperrt,
    /// Europapokal frei (`folge01_00526`, `duell_s2a_01203`), mit beiden Knöpfen offen (`eisbaeren_f01_01431`).
    /// </summary>
    public static readonly IReadOnlySet<string> WorldTiles = new HashSet<string>(StringComparer.Ordinal)
    {
        "WM-Statistik", "Tabelle WM", "WM", "Stärken Länderteams",
    };

    /// <summary>Kacheln des Bundestrainers, die erst nach der Ernennung etwas zu zeigen haben.</summary>
    private static readonly HashSet<string> NeedsCoach = new(StringComparer.Ordinal) { "Aufstellung", "Taktik", "Transfer", "Training" };

    /// <summary>
    /// „Highscore“ im Menü des Bundestrainers. [ORIGINAL] Datei 0x45020–0x4504A: gesperrt, wenn es keine Bestenliste gibt
    /// (DGROUP 0x256 = 0, Datei 0x44FCE) oder im Zug „Manager 5“ (DGROUP 0xA70E = 5) im Modus 4 (DGROUP 0xD0B9 = 4,
    /// Datei 0x4502C). Die Ernennung (DGROUP 0xD0BA) fragt der Aufbau nicht ab. Im Modus 1 gibt es immer die Bestenliste
    /// mit Schlüssel 30000 (Datei 0x2899D, `docs/highscore.md`) – die Kachel ist dort also offen, vor wie nach der
    /// Ernennung. Beleg DOSBox `dosbox/nur-weltkugel/07_kachelreihe_manager_highscore_offen.png` (vor der Ernennung)
    /// und `19_highscore.png` (Liste „Joerg – DEUTSCHLAND – 1“). Das WM-Video zeigt die Kachel gesperrt
    /// (`bundestrainer_00042`, `00220`, `00418`, Saison 2008); mit em.exe ist das nur bei DGROUP 0x256 = 0 erklärbar –
    /// offener Befund in `docs/dosbox-befunde.md`.
    /// </summary>
    public static bool CoachHighscoreLocked(GameState game) =>
        HighscoreRules.Key(game.Setup) is null || game.Mode == GameMode.Full;

    /// <summary>Ist die kleine Kachel im aktuellen Menü gesperrt?</summary>
    public static bool IsLocked(GameState game, MenuTile tile)
    {
        if (tile.IsEmpty)
            return false;
        if (game.NationalCoachMenu && tile.Name == "Highscore")
            return CoachHighscoreLocked(game);
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
