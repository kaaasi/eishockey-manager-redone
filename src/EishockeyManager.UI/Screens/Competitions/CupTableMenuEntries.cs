namespace EishockeyManager.UI.Screens.Competitions;

/// <summary>
/// Anmeldung der Kacheln „Tabelle DEB-Pokal“ (Hauptmenü → Tabellen, Platz 4: Übersicht „DEB-Pokalspiele“, R4-01/#108) und
/// „Tabelle Europapokal“ (Platz 5).
/// </summary>
/// <remarks>
/// [ORIGINAL] em.exe ruft für die beiden Pokal-Kacheln der Tabellen dieselben Bildschirme auf wie für „DEB-Pokal“ und „Europapokal“
/// der Reihe Paarungen (Verteiler der kleinen Kacheln, Datei 0x45829 ff.):
/// <list type="bullet">
/// <item>„Tabelle DEB-Pokal“: Sprung 0x4584A → 0x45897 (<c>push 0; push 0; call 0xE8:0x75</c>), dieselbe Stelle wie
///   „DEB-Pokal“ bei den Paarungen. Der Stub löst auf Datei <b>0x32E23</b> auf (Review R4-01: Segmenttabelle 0x163C0,
///   Stub 0x16700, Funktionsoffset 0x38F3): Titel „DEB-Pokalspiele“ (Datei 0x198B5), Gegner und Ergebnisse
///   (DGROUP 0xD012/0xD013) – nicht die Auslosung (Datei 0x33B83). Umsetzung: <see cref="DebCupGamesScreen"/>.</item>
/// <item>„Tabelle Europapokal“: <c>call 0xF8:0x3E</c> (Datei 0x4584C) mit dem Seitenwert der Kachel in DGROUP
///   0x511F + Platz = 0x5123, Vorgabe 3 – derselbe Wert wie DGROUP 0x5126 bei „Europapokal“ der Paarungen: der
///   Gruppenbildschirm „Europacup Gruppe 1“ (<see cref="EuropeanCupScreen"/>) mit den Knöpfen zu Gruppe 2 und Endrunde.</item>
/// </list>
/// [APPROX] Das Original merkt sich die zuletzt gezeigte Seite je Kachel (die Funktion gibt sie zurück, Datei 0x45862);
/// das Remake zeigt beim Öffnen immer Gruppe 1. Der Bildschirm zeigt nur vorhandenen Zustand.
/// </remarks>
internal static class CupTableMenuEntries
{
    [MenuEntry("Tabelle DEB-Pokal")]
    private static Screen DebCupGames(ScreenHost host, Screen back) => new DebCupGamesScreen(host, back);

    [MenuEntry("Tabelle Europapokal")]
    private static Screen EuropeanCup(ScreenHost host, Screen back) => new EuropeanCupScreen(host, back);
}
