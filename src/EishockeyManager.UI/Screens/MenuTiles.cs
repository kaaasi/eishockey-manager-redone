namespace EishockeyManager.UI.Screens;

/// <summary>Kleine Kachel im Hauptmenü (Symbol aus kb\3, 38 × 28).</summary>
public sealed record MenuTile(string Name, int Column, int Row)
{
    /// <summary>Platzhalter (Fisch) für unbelegte Plätze.</summary>
    public static readonly MenuTile Empty = new("", 3, 4);

    public bool IsEmpty => Name.Length == 0;
    public Rect Source => new(1 + Column * 39, 87 + Row * 29, 38, 28);
}

/// <summary>
/// Belegung des Hauptmenüs wie im Original (Video). Die Namen sind die Schlüssel, unter denen
/// Bildschirme per <see cref="MenuEntryAttribute"/> angemeldet werden.
/// </summary>
public static class MenuTiles
{
    public static readonly string[] Big =
    [
        "Manager", "Mannschaft", "Statistiken", "Tabellen",
        "Paarungen", "Stärken", "Gemischtes", "und tschüß…",
    ];

    public static readonly MenuTile[][] Small =
    [
        [new("Finanzen", 0, 0), new("Werbung", 1, 0), new("Stadion", 2, 0), new("Jugend", 3, 0), new("Neuer", 4, 0), new("Highscore", 5, 0)],
        [new("Aufstellung", 0, 1), new("Taktik", 1, 1), new("Verträge", 2, 1), new("Transfer", 3, 1), new("Training", 4, 1), MenuTile.Empty],
        [new("Totale", 5, 1), new("Früher", 0, 2), new("Verein", 1, 2), new("Spieler", 2, 2), new("Fairplay", 3, 2), new("WM-Statistik", 4, 2)],
        // [ORIGINAL] Sechs Tabellen-Kacheln, Symbolstart DGROUP 0x5149 = 17, ab Platz 4 um eine Spalte versetzt (Datei 0x45B95):
        // 1. Liga, 2. Liga, Oberliga, DEB-Pokal, Europapokal, WM. DEB-Pokal und Europapokal öffnen dieselben Bildschirme wie bei den Paarungen
        // (CupTableMenuEntries).
        [new("Tabelle 1. Liga", 5, 2), new("Tabelle 2. Liga", 0, 3), new("Tabelle Oberliga", 1, 3), new("Tabelle DEB-Pokal", 3, 3), new("Tabelle Europapokal", 4, 3), new("Tabelle WM", 5, 3)],
        [new("Paarungen 1. Liga", 5, 2), new("Paarungen 2. Liga", 0, 3), new("Paarungen Oberliga", 1, 3), new("DEB-Pokal", 3, 3), new("Europapokal", 4, 3), new("WM", 5, 3)],
        // [APPROX] Belegung der Stärken nicht im Video zu sehen; Länderteams laut Bildschirmtitel vorhanden.
        [new("Stärken 1. Liga", 5, 2), new("Stärken 2. Liga", 0, 3), new("Stärken Oberliga", 1, 3), new("Stärken Länderteams", 2, 3), MenuTile.Empty, MenuTile.Empty],
        [new("Laden", 0, 4), new("Sichern", 1, 4), new("Einstellungen", 2, 4), MenuTile.Empty, MenuTile.Empty, MenuTile.Empty],
        [],
    ];

    public static IEnumerable<string> AllNames => Small.SelectMany(t => t).Where(t => !t.IsEmpty).Select(t => t.Name).Distinct();
}
