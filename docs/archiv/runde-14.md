# Archiv: Runde 14 – Restpunkte, DOSBox-Auswertung `runde-13`, Import `*.HOK` (05.–10.11.2026)

Aus `docs/arbeitspakete.md` ausgelagert am 10.11.2026. Inhalt unverändert bis auf Überschrift, „Ergebnis“ und „Abschluss“, ohne „Zurückgestellt“ und „Vormerkungen“ (bleiben in `arbeitspakete.md`). Die
Statuszeilen der Spuren geben den Stand vor der jeweiligen Integration wieder. Reviews: `docs/reviews/` (Dateien vom 05.–10.11.2026).
Nur bei Bedarf lesen. Relative Links beziehen sich auf `docs/`.

**Ergebnis:** Spuren `re-dosbox`, `saison`, `nationalteam`, `kader-start`, `ui-rest`, `hok-import` und `pruefung-karte` integriert
(#652, #661, #666, #669, #670, #672, #701), dazu #683 (Play-off-Halbfinale) und #688 (Tagestakt im Hauptweg, Sommerpause, keine
Entlassung wegen Schulden). Spielstand-Schema 39 → 47. Gesamtreview in sechs Teilen (#704–#707, #710, #715): Teil 6 **Ja**
(risikoorientiert, Entscheidung C 10.11.2026); keine P0–P2, Testlücken in #716.

**Abschluss (10.11.2026):** offen für Runde 15: #620 (Rest), #698, #699, #703, #712, #716; Vormerkungen in `arbeitspakete.md`.
Prozess: sparsame Kette, Modellwahl (#684/#686), Befund mit Beleg, Verkettung über Labels, Längengrenzen, Leitstelle je Runde (#708),
Gesamtreview risikoorientiert, Vollprüfung als Abschlussprüfung (#713).

---

## Runde 14 – Restpunkte, DOSBox-Auswertung `runde-13`, Import `*.HOK` (Plan 05.11.2026, Go 05.11.2026)

Runde 13 (04.–05.11.2026) ist abgeschlossen: [archiv/runde-13.md](runde-13.md) (Spuren, Integrationen, Reviews,
Entscheidungen L1–L15, Funktionskarte: Spielcode 90 %); frühere Runden in [archiv/](./).
**Schreibweise in diesem Abschnitt:** Adressen sind Dateioffsets mit Suffix `h` (R10P-01). Im Code und in `docs/analyse/` gilt
die normale Schreibweise.

**Ziel der Runde (Nutzer 05.11.2026: alle offenen Issues bearbeiten und lösen):** alle Kandidaten aus dem Übergang (unten, Vorentscheidungen E14-1 bis E14-3, Issue #601) genau einer Spur
zuordnen und abarbeiten: Auswertung der DOSBox-Aufnahme `runde-13` mit Umsetzung der belegten Inventur-Punkte, alle P3-Issues
(#591, #592, #594, #595, #597, #598, #603–#605, #610, #611, #619–#621, #625), Nebenbefunde `nationalteam` (#580), Restlücken
#600/#623, #349 (R14-D4 = A) und **Import von Original-Spielständen `*.HOK`** (E14-1 C, R14-D2 = B: Export in Runde 15). `livespiel` bleibt
zurückgestellt.

**Stand DOSBox-Aufnahme `runde-13`** (Plan `vorlagen/dosbox-aufnahme.md`): Sitzung 1 (FILE29–31, Referenz-PR #15) und
Sitzung 2 (FILE32–41, FILE37A, Referenz-PR #16) liegen im Referenz-Repo; Sitzungen 3–6 (FILE43–49) folgen (E14-2 = A).

**Base für alle Spuren:** aktueller `main` (Planstand `401efa7`); tatsächlichen Base-SHA in den Draft-PR. Regeln wie Runde 13
(Archiv): Befund reproduzieren, roter Regressionstest mit Fundstelle, dann grün; Mehrsaisonprobe (4 Wechsel, passiv und aktiv)
bei Saisonwechsel, Kader oder Geld; Schema-Änderung mit Migration in neuer Datei `Core/SaveGame.Migration.<Spur>[<n>].cs`
(Stand Schema 39, die Leitstelle nummeriert bei der Integration); `[ORIGINAL]`/`[APPROX]` mit `Datei 0x…`; Befund-Issues mit
`Fixes #<nr>`; höchstens drei Fortsetzungen je Spur. Bei Unklarheit gilt „wie im Original“; bleibt es unklar, Teil blockiert
lassen und die Leitstelle entscheiden lassen. **Entwickler und Reviews sind Claude-Sitzungen** (geplante Läufe); ChatGPT nur
für das Gesamtreview (E14-3). **Abhängigkeit:** Teile mit „nach `re-dosbox`“ beginnen erst, wenn der PR von `re-dosbox` Teil 1
gemergt ist (sonst blockiert, übrige Teile zuerst). **Integration** je Spur am Ende, Folge: `saison` → `nationalteam` →
`ui-rest` → `kader-start` → `hok-import`; danach Welle 2 `pruefung-karte`. Am Rundenende Gesamtreview nach E14-3.

### Zuständigkeit für gemeinsame Dateien

Wie Runde 13 (Archiv) mit diesen Änderungen:
- `docs/analyse/re-runde-14.md`: allein `re-dosbox`. `docs/analyse/inventur.md` (Stand je Zeile), `docs/re-abdeckung.md`,
  `tools/reverse-engineering/*`, `tests/**/Support/`: allein `pruefung-karte` (Welle 2); die anderen Spuren nennen ihre Zeilen
  im PR.
- `Core/SeasonFlow*.cs`, `Core/GameState.SeasonEndTurn.cs`: allein `saison`.
- `Core/CupsNational*.cs`, `Core/MatchEngine.National.cs`, `UI/Screens/Competitions/NationalMenu.cs`, `WorldCupScreen.cs`,
  `NationalLineupScreen.cs`: allein `nationalteam`. Die Sperre Transfer/Training ab der WM-Gruppenphase setzt `nationalteam`
  über eine Abfrage im Spielkern (`CupsNational*.cs`); `kader-start` schließt die Aufrufstellen in `Transfer*.cs`/`Squad.Training.cs`
  an (ein Aufruf je Stelle, im PR genannt).
- `Core/NewGame.cs`, `Core/GameSetup.cs`, `Core/Transfer*.cs`, `Core/Player*.cs`, `Core/Squad*.cs`, `Core/FinanceTalent.cs`
  (nur #625), `MatchEngine.Managed.cs`/`.Live.cs` (nur Kaderreihenfolge, A088), `UI/Screens/SetupScreen.cs`: allein `kader-start`.
- `UI/Screens/Manager/*`, `UI/Screens/Team/TeamUi.cs`, `UI/Screens/Team/TransferDialog.cs`, Transferliste und Taktik-Bildschirm
  (nur Darstellung): `ui-rest`.
- `src/EishockeyManager.Formats/OriginalSaveGame*.cs`, neue Dateien `Core/OriginalImport*.cs`, `UI/Screens/SaveLoad/*`,
  `src/EishockeyManager.Tools/Hok*.cs`, `docs/formate/spielstand-original.md`: allein `hok-import`.
- `Core/GameState.MainMenu.cs`: ein Aufruf für A009 `nationalteam` (L8), danach Kommentar #598 `pruefung-karte` (Welle 2).
- Tests nur in den Testdateien der eigenen Spur; fremde Testdateien nur `pruefung-karte` (Welle 2, #603, #604, #620, #621).

### Spur `re-dosbox` – Auswertung der DOSBox-Aufnahme `runde-13` (zuerst, nur Doku)

- **Teil 1 (sofort):** Spielstände FILE29–41 und FILE37A mit Bildern aus `original/spielstaende/` (Referenz-Repo, README mit
  den Notizen des Nutzers; `emtool hok dump`, Vergleich gegen Remake-Code und em.exe) je Inventur-ID auswerten. Sitzung 1:
  A056, U15-c, A180, A166, A082, A046. Sitzung 2: A373, Taktik-Befund DGROUP 375h, A364, A012, A167, A091, A135, A189, A380,
  A382, A377, A374, A313, A321, A357, A115 (falls FILE41 Leihe zeigt). Dazu A088 (L14) und A009/A085/A167/A189 aus dem Übergang,
  soweit diese Stände sie belegen.
- **Teil 2 (Fortsetzung, sobald abgelegt):** Sitzungen 3–6 (FILE43–49): A323, A325, A326 (Bau-Tabelle), A362, A085, A091,
  A009, A085 Handaufnahme, A293 „Endrunde“, A338. Eigener PR, gleiche Datei.
- **Abnahme:** Datei `docs/analyse/re-runde-14.md` mit Ergebnis je ID (belegt / widerlegt / nicht entscheidbar, Fundstelle:
  Datei, Byte, Bild) und Abschnitt „Umsetzung“ je Punkt (Ziel, Dateien, Testidee, Spur dieser Runde). Selbstprüfung
  `funktionskarte.py --csv` ohne `--write`.
- **Dateigrenzen:** nur `docs/analyse/re-runde-14.md` und der eigene Statuspunkt. **Modell:** Claude Opus 5.5.
  **Status:** Teil 1 (Sitzungen 1–2, FILE29–41, FILE37A) fertig, PR #628 (gemergt nach Review #643/#644); Teil 2
  (Sitzungen 3–7, FILE43–49A) fertig, PR #642.

### Spur `saison` – Satzfolge, Fair-Play, Oberliga

- **Teile:**
  1. **#591, #592:** Mehrsaisonprobe zieht vor `StartNextSeason` Zufall (Gegenprobe ohne Übernahme wird rot);
     `FairPlayPremiumWinner` ohne `SuccessorSlots` mit Rückfall wie `LeagueSlots` (Fundstelle 27213h) oder `[APPROX]`.
  2. **#600-Rest:** `OutsidePlaces` in `SeasonFlow.Oberliga.cs` paart nach Index – gegen das Original prüfen, Kommentar A112
     berichtigen; Mehrsaisonprobe.
  3. ~~Saisonbefunde aus `re-dosbox`~~ – entfällt für Teil 1 (`re-runde-14.md` ordnet `saison` nichts zu); nur falls Teil 2 etwas
     zuordnet.
  4. **#349 (R14-D4 = A):** Merker „Managerverein spielt“ je Spieltag wie im Original (36FEAh, 3702Eh–37085h) als abschaltbare
     Einstellung; Vorgabe Remake-Verhalten. Test: Spieltag mit Managerverein im Pokal rechnet alle Paarungen in der Minutenschleife,
     Zufallsverbrauch gegenüber Remake-Verhalten verschieden; Mehrsaisonprobe mit beiden Einstellungen. Doku
     `anforderungen/zusatzfunktionen.md` Abschnitt C nachziehen.
- **Dateigrenzen:** `Core/SeasonFlow*.cs`, `Core/GameState.SeasonEndTurn.cs`, für Teil 4 die Auswahl der Minutenrechnung
  (`Core/Cups*.cs` außer `CupsNational*`, `Core/MatchEngine*.cs` nur diese Stelle), ein Schalter in `GameOptionsScreen.cs` und
  im Optionsmodell, `docs/anforderungen/zusatzfunktionen.md`, eigene Tests. **Modell:** Claude Sonnet 5.5.
  **Status (08.11.2026):** integriert in PR #652 (Teile 1, 2, 4: #632, #635, #636; Teil 3 entfällt, #645), wartet auf Review.

### Integration Runde 14

Schema-Nummern dieser Runde: `saison` behält 40 (erste integrierte Spur); `nationalteam` 41 (vom Spurbranch mit 40 umnummeriert); `ui-rest` ändert den Spielstand nicht (bleibt 41); `kader-start` 42 (Start-Liga) und 43 (Tabellenfeld, Wettbewerbszähler); die folgenden Spuren nummerieren ab 44.

| Spur | Status | PR | Head-SHA |
|---|---|---|---|
| `saison` | integriert, Prüfung grün (2288 / 0 / 0 mit Daten), wartet auf Review | #652 (Branch `integration/runde-14-saison`, Base `b7e1eb2`) | im Übergabekommentar von #652 |
| `nationalteam` | integriert (#630, #637, #653; Schema 41); Review #662 „Nein“ → Korrekturen R14NT-01–06 auf dem Branch (08.11.2026), wartet auf Nachprüfung | #661 (Branch `integration/runde-14-nationalteam`, Base `3e0cf69`) | im Übergabekommentar von #661 |
| `ui-rest` | integriert (#634, #647, #657; Schema bleibt 41, Spur ändert den Spielstand nicht), Prüfung grün (2321 / 0 / 0 mit Daten), wartet auf Review | #666 (Branch `integration/runde-14-ui-rest`, Base `6e1bf44`) | im Übergabekommentar von #666 |
| `hok-import` | integriert (#633, #638, #640, #648, #651, #654, #658; Schema bleibt 41, Spur ändert den Spielstand nicht); Review #673 „Nein“ → Befunde R14HI-01–07 auf dem Branch behoben (09.11.2026: Play-offs/Aufstiegsrunden Teil 4d, Ausbauten, Werbevertrag-Schlüssel, Feldkarte B/A gegen den Code geprüft, Testlücken, Marker); Mehrsaisonprobe aller 52 Stände; wartet auf Nachprüfung | #670 (Branch `integration/runde-14-hok-import`, Base `1e9a233`, danach `main` mit #666 und #673 hineingemergt) | im Übergabekommentar von #670 |
| `kader-start` | integriert (#631, #646, #655, #659, #663, #667; Schema 42 = Start-Liga, 43 = Tabellenfeld/Wettbewerbszähler); Review #677 „Nein“ → Fixlauf R14KS-01–08, R14N-01–03, R14NT4-05/07 erledigt, #670 hineingemergt (09.11.2026), Prüfung grün (2542 / 0 / 0 mit Daten), wartet auf Nachprüfung | #669 (Branch `integration/runde-14-kader-start`, Base `1e9a233`) | im Übergabekommentar von #669 |

Inventur-Zeilen (nur im PR): A112 (Kommentar `OutsidePlaces` berichtigt), #349 → `pruefung-karte`.

### Spur `nationalteam` – Tagesfolge 952Ch, Länderwerte, Nebenbefunde

- **Teile:**
  1. **#594, #595:** Reihenfolge `UpdateNationalTeamValues()` gegenüber der Wanderung wie in der Tagesfunktion 54C9Fh
     (54F9Ch–54FBFh vor 5525Ah–55292h), roter Test an einem Wandertag ohne WM-Spiel; Regressionstest R13RD-04 (FILE20→FILE21).
  2. **Nebenbefunde #580:** Reihenlogik der Vereinsaufstellung wie A010 (Fundstelle nachlesen); Sperre Transfer/Training ab der
     WM-Gruppenphase (Fundstelle, neue Inventur-ID über `pruefung-karte`); WM-Kader-Reihenfolge beim Einsortieren (3CABFh,
     Restlücke #623).
  3. **A012** (L7): `[APPROX]` in `CupsNational.Lineup.cs` → `[ORIGINAL]` (Bilder `file36_1`/`file36_2`, `file37_2`/`file39_3`);
     dazu **Bundestrainer-Befunde aus `re-dosbox` Teil 2** (#642): A009 („automatisch aufstellen“ des Bundestrainers, zwei Bytes
     DGROUP 0x37F/0x380 mit Schema und Migration in eigener Datei, Neuaufstellung über `OnMainMenu`; L8), A085 (WM-Kader), A293
     wie im Original nach Review #649 R14RD2-02 (Startseite nur beim ersten Aufbau eines Zugs, nur bei WM-Stufe = 5; ohne gemerkte
     Seite `[APPROX]`).
- **Dateigrenzen:** wie oben „allein `nationalteam`“, eigene Tests; nach L8 zusätzlich `Core/GameState.MainMenu.cs` (ein Aufruf in
  `OnMainMenu` für A009), `Core/GameState.HotSeat.cs` (nur falls für A009 nötig), `UI/Screens/Competitions/WorldCupKnockoutScreen.cs`
  und `CupPairingsScreen.cs` (A293). **Modell:** Claude Opus 5.5. **Status (08.11.2026):** Teil 1 (#594, #595) fertig, PR #630; Teil 2 (Sperre Transfer/Training ab dem ersten WM-Termin, WM-Kader nach Art) fertig, PR #637; Teil 3 (A012, A009, A085, A293) fertig, PR #653; alle drei integriert in `integration/runde-14-nationalteam` (Schema 41), wartet auf Review. Reihenlogik der Vereinsaufstellung (Teil 2, A010) liegt bei `kader-start` (L3). **Status (09.11.2026):** Teil 4 (#664 WM-Schleife auch ohne Bundestrainer, ohne Training/Markt außer nach bestimmten WM-Spieltagen; R14NT-N1; Rechtsklick in `NationalLineupScreen` verlässt den Bildschirm) fertig, PR #672; Review #674 „Nein“ → Korrekturen R14NT4-01–07 und #675 auf dem Branch (09.11.2026: Tageszähler 0x3B7 = Tag + 1, (c) nach den WM-Spielen der Tage 216/220/230 samt Markt-Verfall, Modus 1 in der Schleife, Takttage des Hauptwegs); Nachprüfung #681 „Nein“ → R14NT4N-01–03 auf dem Branch behoben (09.11.2026: Tag 216 rechnet noch den Takt des Hauptwegs bei Zähler 217, Tests Modus-1-Heilung und Untergrenze des Markt-Verfalls), wartet auf Nachprüfung; kein neues Spielstandfeld.

### Spur `kader-start` – Spielstart, Kader, Aufstellung

- **Teile:**
  1. **#605, #625:** Start-Liga in `GameSetup` selbst führen, ohne Manager `false`; Test Einsortieren bei `JoinScoutedTalent`,
     Aufruferliste in `Player.Factory.cs` (Adressen aus #625).
  2. **Spielstart und Kader aus `re-dosbox`** (nach `re-dosbox`): U15-c, A056, A180, A166, A082, A046, A088 (L14), A167, A091,
     A135, A189, A115 (A012 → `nationalteam`, L7) – nur abweichend Belegtes umsetzen, Übereinstimmungen auf `[ORIGINAL]`; Mehrsaisonprobe.
     Nach L1–L4 (#628): U15-c umsetzen (L2 Runde 13 aufgehoben); Schemafelder A046/A135/A380; Taktik 375h im Kern
     (`MatchEngine.Injuries.cs` nur dafür, vorher 0x53C57 lesen); A364 und Reihenlogik der Vereinsaufstellung wie A010 (aus
     `nationalteam`, #637) in `UI/Screens/Team/LineupScreen.cs` und `Core/Squad.Lineup.cs`.
  3. ~~Anschluss Sperre ab WM-Gruppenphase~~ – entfällt (L5: Sperre nur im Menü des Bundestrainers, #637).
  4. **Saisonende-Abgang aus `re-dosbox` Teil 2** (#642): A362 als Abbild im Bildschirm ohne Schemaänderung (L9), A091/A380
     (Rückkehr von der Transferliste), A115 nach dem Code wie im Original (Kopierrichtung belegt, Review #649 R14RD2-03; Leihe
     zwischen Managern nur aus dem Code, L10), A088 (belegt nur `+0x00`, `+0x01`, `+0x03`, `+0x04`, `+0x06`, `+0x07`, R14RD2-05),
     Saisonende-Test (Prämie, Gehalt, Ablöse).
- **Dateigrenzen:** wie oben „allein `kader-start`“, `UI/Screens/Team/*` außer `TeamUi.cs` und `TransferDialog.cs`, eigene Tests,
  `docs/transfer.md`. Dazu nach L4/L7 jeweils nur für den genannten Punkt: `Core/MatchEngine.Injuries.cs` (Taktik 375h),
  `Core/FinanceStadium.cs` (`TableRank`) und `Core/FinanceSponsorRating.cs` (Tabellenfeld, A046), `Core/MatchEngine.Live.cs`
  (Kommentar A082), `Core/StadiumStatistics.cs` (Kommentar A189), Buchung von Spielen und Toren je Wettbewerb in `Core/Cups*.cs`
  außer `CupsNational*` und `Core/MatchEngine*.cs` (A135), `Core/Highscore.cs` (U15-c). **Modell:** Claude Opus 5.5. **Status (09.11.2026):** Teile 1, 2a–2c, 4 und 6 fertig (PR #631, #646, #655, #659, #663, #667, Branches `paket/kader-start-r14-1…6`), integriert in #669 (`integration/runde-14-kader-start`, Schema 42/43), wartet auf Review. Offen für `nationalteam` (#664): Rechtsklick auf einen Platz in `NationalLineupScreen` verlässt im Original den Bildschirm, geleert wird per Linksklick.

### Spur `ui-rest` – Darstellung aus der Aufnahme, Stadionpfeile

- **Teile:**
  1. **#597:** Eintrittspfeile 10 Pixel breit wie im Original (Aufbau 3D7F8h ff., Grundgrafik kb\4 (20 | 0)), Pixeltest gegen
     `file01_3`.
  2. **Darstellung aus `re-dosbox`** (nach `re-dosbox`): A373 (Pfeilfolge Taktik), Taktik-Befund DGROUP 375h (falls Anzeige),
     A380, A382, A377, A374, A313, A321, A357 (A364 → `kader-start`, L3) – Pixeltests gegen die Bilder der Sitzung 2.
  3. **Stadion aus `re-dosbox` Teil 2** (A323, A325, A326), sobald Sitzung 3 ausgewertet ist. A338/A339 → `hok-import` (L6).
- **Dateigrenzen:** wie oben `ui-rest`, eigene Tests; nach L7 zusätzlich `UI/Screens/Statistics/HistoryScreen.cs` (A357). **Modell:** Claude Sonnet 5.5. **Status:** Teil 1 (#597) umgesetzt, PR `paket/ui-rest-1` (05.11.2026); Teil 2 umgesetzt (A373, A374, A377, A382, A313, A321, A357), PR #647 `paket/ui-rest-2` (08.11.2026); offen daraus: Taktik-Knopf 375h, A380/A180-Anzeige (hängen am Kern von `kader-start`), A364 (`kader-start`); Teil 3 umgesetzt (A323, A325 Tabelle/Folge/Maske, A326 Bauanzeige und Ausbau-Frage), `paket/ui-rest-3` (08.11.2026); offen: Hotspot Kino/Gastronomie nach der neuen Lage nicht belegt (`[APPROX]`). Alle drei Teile integriert in PR #666 (`integration/runde-14-ui-rest`, 09.11.2026), wartet auf Review.

### Spur `hok-import` – Original-Spielstände `*.HOK` ins Remake (E14-1 C)

Umfang nach Entscheidung **R14-D2 = B**: Feldkarte und Import, Export in Runde 15; **E14-4 = A**: alle beim Import bisher
nicht übernommenen Felder noch in Runde 14. Fortsetzungen: höchstens 5 statt 3 (Nutzer 08.11.2026).
- **Teile:**
  1. **Feldkarte:** jedes Feld des Remake-Spielstands (Schema 39) einem HOK-Feld zuordnen (belegt / abgeleitet / nicht im
     Original / unbekannt), Grundlage `docs/formate/spielstand-original.md`, `OriginalSaveGame.cs`, FILE01–41. Ergebnis als
     Abschnitt in `docs/formate/spielstand-original.md`.
  2. **Import Spielkern:** `Core/OriginalImport*.cs` baut aus `OriginalSaveGame` einen gültigen `GameState` (Datum, Manager,
     Vereine, Ligen, Kader, Spielerpool, Finanzen, Wettbewerbe, Zufallszustand). Nicht abbildbare Felder neu berechnet und
     `[APPROX]`. Tests mit Originaldaten: FILE05, FILE11, FILE16A, FILE29 laden, Rundreise Remake-Speichern/Laden, danach
     `Continue()` über einen Spieltag und eine Mehrsaisonprobe ohne Fehler.
  3. **Zugang:** `emtool hok import <hok> <ziel>` und Laden-Dialog listet `*.HOK` (Import beim Laden, Hinweis bei
     Prüfsummenfehler wie „Der kleine Mogelguru“). Bildschirmfoto-Ablauf Hauptmenü nach Import gegen das DOSBox-Bild des Stands.
  4. **Restfelder (E14-4 = A):** alle in #638 nicht übernommenen Bereiche importieren – Pokale, Pool/Transfermarkt,
     Nationalkader, Börse, Werbung, Training, Taktik, Nachrichten, Serien, Managerdarlehen. Unbekannte Bytes zuerst an
     FILE01–49A und in em.exe klären, Feldkarte nachziehen; was unbelegt bleibt, `[APPROX]` mit Grund. Bei Bedarf als zwei Teile
     (4a/4b), Tests je Bereich mit Originaldaten.
  5. **Speichern-Dialog wie im Original (L6, nach `re-dosbox` Teil 2):** A338/A339 – Blättern nur über die zwei Pfeile, kein
     „SEITE:“, Infofeld nur bei gehaltener rechter Maustaste; Pixeltest gegen `a338_*`.

- **Dateigrenzen:** wie oben „allein `hok-import`“, eigene Tests. **Modell:** Claude Opus 5.5. **Status:** Teil 1 (Feldkarte) fertig, PR #633 (08.11.2026); Teil 2 (Import Spielkern) fertig, PR #638 (08.11.2026); Teil 3 (Zugang) fertig, PR #640 (08.11.2026); Teil 4a (Restfelder ohne Pokale, Pool/Markt, Nationalkader, Nachrichten) fertig, PR #648 (08.11.2026); Teil 4b (Pool/Transfermarkt, Nationalkader, Nachrichten) fertig, PR #651 (08.11.2026); Teil 4c (DEB-Pokal, Europapokal, WM aus em.exe zugeordnet) fertig, PR #654 (08.11.2026); Teil 5 (Speichern-Dialog A338/A339, L6; zusätzlicher Lauf E14-6 = A) fertig, PR #658 (08.11.2026); alle sieben integriert in `integration/runde-14-hok-import` (PR #670, 09.11.2026), wartet auf Review.

### Spur `pruefung-karte` – Welle 2 (nach der Integration aller Spuren)

- **Inhalt:** #598 (Kommentaradresse 44E22h), #603, #604, #610, #611, #619, #620, #621 (Fundstellen, Tests ohne Wirkung,
  Saaten, Kommentare, Doku-Platzhalter); Inventur-Stand aller Spuren dieser Runde nachziehen, neue ID für die Sperre ab der
  WM-Gruppenphase; Funktionskarte `--write` (zweiter Lauf ohne Diff); Freiprüfung der Bildschirme.
- **Dateigrenzen:** `docs/analyse/inventur.md`, `docs/re-abdeckung.md`, `tools/reverse-engineering/*`, `tests/**/Support/`,
  die in den Issues genannten Test- und Kommentarstellen, `Core/GameState.MainMenu.cs` (nur Kommentar). **Modell:**
  Claude Sonnet 5.5 (Lauf 09.11.2026: Claude Opus 5.5, Leitstelle). **Status:** Teile 1–4 fertig (#693 Inventur/Karte/Doku,
  #694 P3 Leitstelle, #695 #603/#604/#619/#621, #696 #610/#611/#620 – #620 teilweise); Teil 5 (HOK-Import mit Ladeeigenheit
  des Markierungstags, Entscheidung C, Schema 47) fertig #697; Teil 6 (Restlücken #688: A158 `[ORIGINAL]`, übrige als Issue #699) fertig #700.

### Gesamtreview am Rundenende (E14-3)

Ursprünglich Vollprüfung (E14-3) durch ChatGPT (Codex mit Netzzugang) nach `vorlagen/review-anfrage.md`, Abschnitt „Gesamtreview am Rundenende“ (jetzt `prozess/anhang.md` C);
Nachprüfung durch Claude nur für das Delta von Fixes. **Modell:** GPT-6.1 Terra (Codex).
**Stand 10.11.2026:** Teile 1–5 (#704–#707, #710) ohne P0–P2, 14 P3 (überwiegend Testlücken), Vollprüfung vom Nutzer abgebrochen.
**Entscheidung Nutzer 10.11.2026 = C:** ein letzter, risikoorientierter Teil (Entscheidungen der Runde, neue `[ORIGINAL]`-Fundstellen in
riskanten Übergängen), danach Rundenabschluss; künftig Gesamtreview risikoorientiert, Vollprüfung einmal als Abschlussprüfung.

### Entscheidungen zum Plan (Nutzer, Go 05.11.2026, PR #627)

- **R14-D1 = A:** sieben Spuren wie oben, Modelle wie in den Spurabschnitten.
- **R14-D2 = B:** `*.HOK` in Runde 14 Feldkarte und Import; Export in Runde 15.
- **R14-D3 = A:** Sitzungen 3–6 über `re-dosbox` Teil 2, umgesetzt in Folgeteilen der Fachspuren bis zu deren
  Integrationsbeginn; später Eintreffendes → Runde 15.
- **R14-D4 = A:** #349 jetzt als Einstellung „wie im Original“, Vorgabe Remake-Verhalten (Entscheidung 01.11.2026 vorgezogen).
- **E14-4 = A** (08.11.2026): Restfelder des Imports in Runde 14 (`hok-import` Teil 4), dafür 1–2 zusätzliche Fortsetzungen.
- **E14-5 = C** (08.11.2026): keine Nachaufnahme für A115, A189, A321, A357, A364; die Punkte bleiben `[APPROX]`, eine
  Aufnahme später nach Lust des Nutzers.
- **Leitstelle L8–L10** (08.11.2026, offene Fragen Teil 2 in `re-runde-14.md`, Review #649): L8 A009 → `nationalteam` mit
  den Dateien oben (R14RD2-01); L9 A362 als Abbild ohne Schemaänderung (`kader-start` Teil 4); L10 A115 nach dem Code wie im
  Original. Die P3-Präzisierungen R14RD2-02 bis -05 aus Review #649 und R14RD1N-02/-03 aus #644 gelten für die Umsetzung in den
  Spuren; die Korrektur in `re-runde-14.md` übernimmt `pruefung-karte` (Welle 2).
- **Leitstelle L7** (08.11.2026, `re-runde-14.md` Frage 6, Review #643 R14RD1-02): Dateigrenzen in den Spurabschnitten
  `kader-start` und `ui-rest` erweitert (unten); A012 → `nationalteam`; Feldbelege in `formate/spielstand-original.md` → `hok-import`.
- **Leitstelle L1–L6** (08.11.2026, Kommentar in #628): U15-c umsetzen; Schemafelder in `kader-start` Teil 2;
  `LineupScreen.cs` allein `kader-start` (A364, A010-Reihenlogik); Taktik 375h im Kern bei `kader-start`; `kader-start` Teil 3
  entfällt; Speichern-Dialog A338/A339 wie im Original bei `hok-import`.

### Vorentscheidungen Runde 14 (Nutzer, 04.11.2026, Issue #601)

- **E14-1 = A + C:** Restpunkte und zusätzlich Import/Export von Original-Spielständen `*.HOK` (Grundlage `hok-format`,
  `formate/spielstand-original.md`). `livespiel` bleibt zurückgestellt.
- **E14-2 = A:** Start ohne Warten auf alle Aufnahmen; bereits abgelegte Sitzungen der Aufnahme `runde-13` sofort auswerten,
  spätere in einem Folgeteil derselben Analysespur.
- **E14-3 = A** (abgelöst durch Entscheidung C, 10.11.2026)**:** Gesamtreview durch ChatGPT als Vollprüfung (`vorlagen/review-anfrage.md`, #614/#617); keine Nachprüfung durch
  Claude außer dem Delta von Fixes.
