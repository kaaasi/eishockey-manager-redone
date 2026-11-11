namespace EishockeyManager.Core;

/// <summary>
/// Ladeeigenheit des Markierungstags (Spur pruefung-karte Teil 5; Entscheidung Nutzer 09.11.2026, C: wie im Original): Versatz des
/// Tageszählers DGROUP 0x3B7 gegen den Tag des Remakes bis zum Saisonwechsel.
/// </summary>
/// <remarks>
/// [ORIGINAL] Selbst nachgelesen (<c>disasm.py gamedata/em.exe 0x54C9F 0x55420</c>): Der Lader 0x437D5 ruft die Tagesfunktion
/// 0x54C9F mit dem Rückgabewert von 0x28165 (Datei 0x43A67/0x43B09). Ist <c>bp+6</c> ≠ 0, überspringt sie den Spielstart
/// (Datei 0x54CA6–0x54CC2) und liest über 0x54D3B bei Datei 0x54D41 die Markierung des Tagesindex DGROUP 0xA969. Markierung 0 oder
/// 255 führt über Datei 0x54D5F–0x54D65 nach 0x55073 und weiter zum Tageswechsel 0x55204 (<c>inc [0x3B7]</c>, Tagesabschluss
/// 0x3ED01/0x3F833 je Manager, Takt bei Zähler ≡ 0/4), dann über 0x552D5 (Markierung 255) zum Menü 0x55350; Datei 0x55332–0x5533E
/// stellt für genau diesen Wiedereinstieg den Manager am Zug wieder her. Ein im Menü des Markierungstags gesicherter Stand (FILE22,
/// FILE44: Zähler 217, Index 216; sonst gilt Zähler = Index) zählt nach dem Laden also ein zweites Mal weiter, auf 218 = 15. April
/// (DOSBox-Bild <c>file46_1</c>). Der Index bleibt; die WM-Schleife (Datei 0x55570–0x55740) spielt dieselben Einträge 217–231, aber
/// jeden mit einem Zähler mehr, und schließt zuletzt den 30. April ab – mit Monatsabschluss (<c>file46_2</c>). Erst das Saisonende
/// setzt den Zähler wieder fest (Datei 0x26F55: <c>mov word [0x3B7], 0xE9</c>, <see cref="SeasonEndCounter"/>).
/// <para>
/// Im Remake: Zähler zum Tag d = <see cref="DayCounter.Of"/>(d) + <see cref="LoadCounterOffset"/> (<see cref="Counter"/>), der
/// Finanztag (<see cref="FinanceDay"/>, Datum und Zähler des Tagesabschlusses) läuft dem Tag um den Versatz voraus. Remake-Spielstände
/// und Importe anderer Stände haben den Versatz 0.
/// </para>
/// </remarks>
public sealed partial class GameState
{
    /// <summary>
    /// Versatz des Tageszählers 0x3B7 gegen <see cref="DayCounter.Of"/> (0 oder 1): 1 nach dem HOK-Import eines im Menü des
    /// Markierungstags gesicherten Stands, bis zum Saisonwechsel (siehe Klassenkommentar).
    /// </summary>
    public int LoadCounterOffset { get; private set; }

    /// <summary>Zählerstand DGROUP 0x3B7 im Menü und bei den Spielen des Tages <paramref name="day"/> (mit Ladeversatz).</summary>
    public int Counter(int day) => DayCounter.Of(day) + LoadCounterOffset;

    /// <summary>
    /// Tag des Tagesabschlusses zum Tag <paramref name="day"/>: <see cref="FinanceDay"/> läuft um den Ladeversatz voraus (Datum
    /// <see cref="SeasonCalendar.LayoutDate"/>(Finanztag) = Datum des Zählers <see cref="Counter"/>(day)).
    /// </summary>
    internal int FinanceTarget(int day) => day + LoadCounterOffset;

    /// <summary>
    /// Zweiter Tageswechsel nach dem Laden eines am Markierungstag gesicherten Stands (Datei 0x54D41 → 0x55073 → 0x55204): Zähler
    /// + 1, Tagesabschluss des neuen Zählers wie im Hauptweg (<see cref="AdvanceFinance"/> mit <c>clubDay: true</c>; Jugendteams und
    /// Talentsuche haben am Zähler 218 nichts zu tun), Takt 0x07169, Wanderung und Stadionbau nur bei Zähler ≡ 0/4 bzw. ≡ 0.
    /// [APPROX] Die Neuaufstellung 0x426BC und 0x952C(m, 0) je Manager (Datei 0x552E3–0x55329) sind schon im HOK enthalten (gleicher
    /// Kader, gleiche Einstellung) und werden nicht wiederholt; ebenso die Rücksetzungen Datei 0x550A0–0x55202.
    /// </summary>
    internal void ReopenMarkerDayAfterLoad()
    {
        LoadCounterOffset = 1;
        var day = CurrentDay;
        AdvanceFinance(day, clubDay: true); // Finanztag + 1: Zähler 218, 15. April
        if (MatchEngine.IsDriftDay(Counter(day)))
        {
            // Wie im Hauptweg (Datei 0x5525A–0x552D3). #703 (R701-03): nicht erreichbar – der Import lässt nur den 13. April mit
            // Zähler 218 zu (OriginalImport, SavedOnMarkerDay), 218 mod 7 = 1; der Zweig steht nur für die Vollständigkeit der Regel.
            MatchEngine.DriftTeamValues(this, TeamValueDrift.Weekly);
            foreach (var manager in Managers.ToList())
                ManagerDailyRun(day, manager);
        }
    }

    /// <summary>[ORIGINAL] Saisonende 0x26F20 setzt den Zähler fest (Datei 0x26F55, 233): Der Ladeversatz endet.</summary>
    private void EndLoadCounterOffset() => LoadCounterOffset = 0;
}
