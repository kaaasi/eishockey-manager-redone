# Nachprüfung #688 – Sommerpause ab 1. Mai, Leihe im Import, keine Entlassung wegen Schulden (Delta `a25f251..8770397`)

- **PR:** #688 · **Base:** `7202d62` · **bisher geprüft:** `a25f251` (Review `2026-11-09-tagestakt-hauptweg-sommerpause.md`: Nein, R688S-01)
  · **Head:** `8770397a1c368bf29423738ebc0a6256cfe36a7c`
- **Delta:** `5ee4d64` (Merge von `main`, nur Reviewdateien) und `8770397` (Fix R688S-01 bis -03). Geprüft wurde `5ee4d64..8770397`.
- **Reviewer:** Claude Code (geplanter Lauf, frische Sitzung ohne Implementierungskontext), 09.11.2026
- **Checkout:** Klon, `git rev-parse HEAD` = Head, `git status --porcelain` leer; Build, Tests, Proben und Rückmutationen selbst ausgeführt.

## 1. Merge-Empfehlung

**Ja** für genau diesen Head. Es bleiben nur Befunde der Stufe P3.

- **R688S-01: Der Fix hat recht, das Vorreview hatte unrecht.** Im normalen Spielverlauf gibt es keinen Tagesabschluss für den 30. April
  und keinen April-Monatsabschluss. Die April-Buchung in `file46_2` kommt von einer Eigenheit beim Laden von FILE44 (Beleg unten).
- **R688S-02:** VALLIERE wird jetzt als Leihe importiert und geht ohne Ablöse.
- **R688S-03:** em.exe kennt keine Entlassung wegen Schulden. Der Zwangsabstieg war am Base schon umgesetzt. Auch hier lag das
  Vorreview falsch („den hat das Remake nicht“).
- **Sollwerte:** Alle geänderten Sollwerte und Fingerabdrücke führen auf R688S-01 und R688S-03 zurück (Rückmutation unten).

## 2. Befunde

### Gegenbeispiele erneut ausgeführt

**R688S-01: behoben, Lesart des Fixes bestätigt.** Selbst gelesen mit `disasm.py`:

- **Hauptweg, Kalendertabelle DGROUP 0x828 je Tagesindex 0xA969.** An normalen Tagen gilt Zähler 0x3B7 = Index (FILE03: 11/11). Am Tag mit
  Markierung 255 springt Datei 0x54D5F–0x54D65 direkt nach 0x55073. Danach folgen der Tageswechsel 0x55204 (`inc [0x3B7]`) mit
  Tagesabschluss und das Menü 0x55350. Der Index steht dabei noch auf diesem Tag.
  **FILE44:** 0x3B7 = 217 (0xD9), 0xA969 = 216 (0xD8). Der Stand ist also im Menü des 13. April gesichert, nach dessen Tageswechsel.
- **WM-Schleife 0x55570–0x55740.** Je Eintrag mit Markierung 0x10 oder 0 läuft `inc [0x3B7]` (0x55670), dann 0x3ED01 und 0x3F833 je
  Manager. Danach erhöht 0x55724 den Index. Ist die nächste Markierung weder 0x10 noch 0, geht es nach 0x55743 (Saisonende) ohne
  weiteren Tag. Die Einträge 217–231 (14.–28.04.) heben den Zähler von 217 auf **232**. Der letzte Tagesabschluss ist also der
  29. April. Eintrag 232 (29.04., Markierung 255) verlässt die Schleife. In 0x55743–0x564D2 gibt es weder 0x3ED01 noch 0x3F02C. Den
  Zähler ändern dort nur 0x26F20 (Datei 0x26F55: `mov word [0x3B7], 0xE9` = 233) und 0x564BC (+1 = 234, 1. Mai).
- **Fehlschluss im Vorreview:** Es hat den Eintrag des 29. April (Markierung 255) als letzten Durchlauf der WM-Schleife gezählt.
- **Eigenheit beim Laden, bestätigt:**
  - 0x437D5 ruft 0x54C9F mit dem Rückgabewert von 0x28165 (Datei 0x43A67/0x43B09).
  - Ist `bp+6` ≠ 0, überspringt 0x54C9F den Start und geht über 0x54D3B nach 0x54D41. Für den gesicherten Markierungstag führt das
    erneut über 0x55073 nach 0x55204: Der Zähler steigt ein zweites Mal, auf 218.
  - Datei 0x55332–0x5533E stellt genau für diesen Wiedereinstieg den Manager am Zug wieder her. Das ist eine bewusste Ladestelle.
  - Bild `file46_1` zeigt nach dem Laden „Sonntag, 15. April 1994“ mit unverändertem Konto 18.027.183 DM.
  - Folge: Die Schleife endet bei 233, also 30.04., mit April-Abschluss. Das passt zu `file46_2` (30. April, 17.523.876 DM).
- **Probe am Head:** FILE44 → Saisonende-Bildschirm 18.127.183 DM (nur die Prämie). FILE44 → neue Saison −1.617.704 DM.
  - Ablöse +158.000 DM, nur KJELLBERG.
  - Gehälter: 4 × −291.100 DM.
  - Original −2.139.514 DM. Ohne den April-Abschluss der Ladeeigenheit (−603.307 DM) sind es −1.536.207 DM.
  - Rest −81.497 DM. Davon kommen −2.500 DM vom Zufall der Ablöse. Der Rest liegt in den vier Sommerabschlüssen: rund 469.000 DM
    statt 448.000 DM je Monat, also +4,7 %. Das stammt nicht aus dem Delta (Kategoriesummen fehlen im HOK, wie im Vorreview).

**R688S-02: behoben.**

- `File44_Valliere_ImportedAsLoan_EndsFreeWithoutCompensation` ist grün.
- [`OriginalImport.cs` Z. 561](https://github.com/kaaasi/eishockey-manager-redone/blob/8770397a1c368bf29423738ebc0a6256cfe36a7c/src/EishockeyManager.Core/OriginalImport.cs#L561):
  +0x0C = Verein + 0x80, über `clubOf` abgebildet, mit Bereichsprüfung.
- In der Probe kommt Ablöse nur für KJELLBERG an, und der Kader hat 24 Spieler.

**R688S-03: behoben.**

- `DeepDebts_NoDismissal_GameContinues` und `PassiveBundesligaManager_NeverDismissed_ForcedRelegationInstead` (Saaten 5 und 41, 4
  Saisonwechsel) sind grün. Ebenso `FinanceCalibrationTests.PassiveBundesligaManager_WithoutCare_NotDismissed_ForcedRelegation`.
- Der Zwangsabstieg tritt dabei ein.
- `SeasonFlow.Zwangsabstieg.cs` (`ApplyForcedRelegation`, aufgerufen in `SeasonFlow.cs` Z. 365) war am Base schon vorhanden
  (gemergt mit #576).

### Besonders prüfen

1. **R688S-01 / Zwangsabstieg:** siehe oben. Der Fix hat recht. Das Remake prüft den Zwangsabstieg nach dem Tagesabschluss des
   29. April. Das stimmt mit der Reihenfolge des Originals überein (0x55670 vor 0x55F22).
2. **Keine Entlassung wegen Schulden:** `disasm.py --xref 0x97EA 0x97ED` gibt alle Zugriffe auf das Konto aus.
   - Bedingte Vergleiche gibt es nur an diesen Stellen: 0x2E20C (Bildschirm „Verein“ 0x2D5AE, Vermögensrechnung), 0x3E11A (Aufbau des
     Stadions 0x3D7F8, Prüfung, ob das Geld reicht), 0x3EADB (Stadionauftrag), 0x3EE05 (Tagesabschluss, Sollzinsen), 0x3F10E/0x3F142
     (Kontogrenze ±500 Mio. Drittel), 0x42A8F (Kauf), 0x55F22/0x55F2E (Zwangsabstieg) und 0x596F2/0x597A6/0x598EA (Börse 0x58A57).
   - Keiner dieser Vergleiche setzt ein Statusbit oder beendet das Spiel.
   - In em.exe und `gamedata/` kommen „entlass“ und „gefeuert“ nicht vor. Der Entlassungstext des Remakes ist `[APPROX]` (A341).
   - **Migration 45 → 46:** Probe mit temporärem Test. Ein Stand mit Schema 45, `DebtDays = 30` und `Dismissed = true` lädt. `Dismissed`
     bleibt erhalten, und das nächste `Continue()` führt mit einem einzigen Manager zu `GameOver`. Schema 46 mit `DebtDays` wird
     abgelehnt („Unbekanntes Feld DebtDays“), wie erwartet.
   - `DismissalStep` erzeugt die Zeitleiste nicht mehr. Der Typ bleibt für die Oberfläche.
3. **Sollwerte und Fingerabdrücke:**
   - **Rückmutation A:** Nur `SummerBreakCounters` beginnt wieder bei 233 (Z. 239). 24 Tests werden rot: RngTwin ×6,
     Spielberechnung ×2, Jugend ×2, `Mode4_ReferenceValues_Unchanged`, alle 8 Hüllkurven von `FinanceCalibrationTests`, 3 Tests
     `TagestaktHauptwegReviewTests` und die beiden neuen Tests zu R688S-01.
   - **Rückmutation B:** `DailyRun`, `SeasonDismissal`, `SeasonFlow`, `Zwangsabstieg`, `SeasonRecord` und `SaveGame` auf `5ee4d64`
     zurück, Migration entfernt, der Leih-Import bleibt; alle Core-Tests auf dem Stand `5ee4d64`. Ergebnis: **1762/1764 grün**. Alle
     alten Sollwerte sind wieder erreicht: RngTwin, Spielberechnung, Jugend, Mode4Reference, Hüllkurven und `File44_ToNewSeason`.
     Rot sind nur die beiden Tests der Feldkarte. Sie lesen die schon geänderte `docs/formate/spielstand-original.md` (Schema 46).
     Jeder geänderte Wert geht damit auf R688S-01 bzw. R688S-03 zurück. Der Leih-Import verändert keinen dieser Werte.
   - **Rückmutation C:** Die Schuldenregel steht wieder in `CheckDismissed`, der Leih-Import ist abgeschaltet. Rot werden alle Tests
     zu R688S-03 (`Debts_DoNotDismiss`, `DeepDebts_…`, beide Saaten `PassiveBundesligaManager_…` in beiden Klassen), dazu
     `Mode4_ReferenceValues_Unchanged` und zwei Hüllkurven (Bundesliga passiv). Ebenfalls rot wird `File44_Valliere_…` (R688S-02).

### Erwartet offen (bestätigt)

- **Ladeeigenheit des Markierungstags im Import (`[APPROX]`):** Der Import spielt den doppelten Tageswechsel nicht nach. Er ist im Code
  und im Test benannt. Ein Saisonende aus FILE44 liegt deshalb im Remake 603.307 DM über `file46_2`.
- **A158:** Datum im Fenster „Saisonende“. In `docs/saison.md` als offen geführt. Nach dem Code ist es Zähler 233 = 30. April, so
  zeigt es auch `file46_2`.

### Neue Befunde

#### R688S2-01 (P3): Liste der Kontovergleiche in den Kommentaren unvollständig

- **Stelle:** [`SeasonDismissal.cs` Z. 6–11](https://github.com/kaaasi/eishockey-manager-redone/blob/8770397a1c368bf29423738ebc0a6256cfe36a7c/src/EishockeyManager.Core/SeasonDismissal.cs#L6-L11),
  [`TagestaktSommerpauseReviewTests.cs` Z. 15–17](https://github.com/kaaasi/eishockey-manager-redone/blob/8770397a1c368bf29423738ebc0a6256cfe36a7c/tests/EishockeyManager.Core.Tests/TagestaktSommerpauseReviewTests.cs#L15-L17)
  und `docs/saison.md`.
- **Problem:** Die Texte sagen „alle Vergleiche“, nennen aber nur 4 Stellen. Es fehlen 0x2E20C, 0x3E11A, 0x3EE05 und 0x596F2/0x597A6/0x598EA (siehe oben).
- **Wirkung:** Die Schlussfolgerung bleibt richtig. Nur die Fundstelle ist ungenau.
- **Vorschlag:** Die Liste ergänzen, wenn die Datei ohnehin geändert wird.

#### R688S2-02 (P3): Veralteter Kommentar im Bildschirm Spielende

- **Stelle:** [`SeasonEndGameOverScreen.cs` Z. 6](https://github.com/kaaasi/eishockey-manager-redone/blob/8770397a1c368bf29423738ebc0a6256cfe36a7c/src/EishockeyManager.UI/Screens/SeasonEndGameOverScreen.cs#L6)
- **Problem:** Dort steht noch „Entlassung eines Managers (zu lange zu hohe Schulden)“.
- **Wirkung:** Der Text „hat Sie wegen der hohen Schulden“ erscheint nur noch für ausgeschiedene Manager älterer Spielstände. Der Kommentar sollte das sagen.

#### R688S2-03 (P3): Kein Regressionstest für die Migration 45 → 46

- **Problem:** Die Migration `MigrateSchulden` (Wiring
  [`SaveGame.cs` Z. 170](https://github.com/kaaasi/eishockey-manager-redone/blob/8770397a1c368bf29423738ebc0a6256cfe36a7c/src/EishockeyManager.Core/SaveGame.cs#L170))
  hat keinen eigenen Test. Sie wird nur indirekt über ältere Migrationstests abgedeckt; diese werden rot, sobald `DebtDays` wieder
  als Feld existiert (Rückmutation C).
- **Vorschlag:** Die Probe aus Abschnitt 1, Punkt 2 als Test übernehmen: Schema 45 mit `DebtDays` und `Dismissed` laden, dann `Dismissed` und das Spielende prüfen.

## 3. Ausgeführte Prüfungen

- **Disassembly** (`python3 tools/reverse-engineering/disasm.py gamedata/em.exe …`): 0x55560–0x55760 (WM-Schleife),
  0x54C9F–0x54D80 (Einstieg, Laden), 0x55060–0x55370 (Tageswechsel und Menü am Markierungstag), 0x55743–0x564E0 (Saisonende,
  Anfang der Sommerschleife), 0x26F20–0x26F70, 0x437D5 (Aufruf von 0x54C9F); dazu `--xref 0x97EA 0x97ED` und die Umgebung jedes
  Vergleichs.
- **Spielstände und Bilder:** `emtool hok dump` FILE44/FILE46 (0x3B7, 0xA969, Konto) sowie die Bilder `file46_1` und `file46_2`.
- **Proben:** temporärer Test, nicht committet. Migration 45 → 46 und FILE44 → Saisonende → neue Saison.
- **Rückmutationen A–C** in einem temporären Worktree (`dotnet test tests/EishockeyManager.Core.Tests -c Release`, `EM_REQUIRE_GAMEDATA=1`).
- **Mehrsaisonprobe:** 4 Wechsel, passiver und aktiver Manager: `FinanceCalibrationTests.FourSeasonChanges_*`,
  `RngTwinProbeTests.FourSeasonChanges_*`, `JugendTeamsTests.FourSeasonChanges_*`, `SpielberechnungAnschlussTests.FourSeasonChanges_*`
  und `PassiveBundesligaManager_*`. Alle grün im Lauf unten.
- **`tools/pruefen.sh --schnell`** am Head:

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2610 | 0 | 0 |
| Ohne Originaldaten | CI `build` success (Head `8770397`) | | |
| Pflichtlauf ohne Daten | nicht verlangt (sparsame Kette) | | |

**Akzeptierte `[APPROX]`:**

- Der Import spielt die Ladeeigenheit des Markierungstags nicht nach.
- Der Stammvertrag einer importierten Leihe (Laufzeit, Gehalt, Versicherung) übernimmt die Werte des Leih-Kaderplatzes.
- Ausgeschiedene Manager älterer Spielstände bleiben im Hot-Seat übersprungen. Für sie gilt der Zwangsabstieg nicht.

**Annahmen:**

- 0x28165 liefert nach dem Laden einen Wert ≠ 0. Das ist nicht im Einzelnen gelesen, aber gestützt durch die Wiederherstellung in
  0x55332 und durch `file46_1` (15. April).
- Die übrigen 4,7 % je Sommermonat (FILE44 → FILE46) bleiben ohne Kategoriesummen offen. Sie stammen nicht aus dem Delta.
