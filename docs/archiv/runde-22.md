# Archiv: Runde 22 – Restliste Klasse V: Kaderplatz, Nationalteam, Kader, Transfer, Wettbewerbe, Einzelpunkte (17.–22.11.2026)

Aus `docs/arbeitspakete.md` ausgelagert am 22.11.2026. Inhalt unverändert bis auf Überschrift, „Ergebnis“, „Abschluss“ und
„Entscheidungen während der Runde“, ohne „Zurückgestellt“ und „Vormerkungen“ (bleiben in `arbeitspakete.md`). Die Statuszeilen
der Spuren geben den Stand vor dem jeweiligen Merge wieder. Reviews: `docs/reviews/` (Dateien vom 17.–22.11.2026). Nur bei Bedarf
lesen. Relative Links beziehen sich auf `docs/`.

**Ergebnis:** Plan #917 (Plan-Review #918), DOSBox-Aufnahmeliste `runde-22` #919 (#920). Alle zehn Spuren gemergt:
`kaderplatz-werte` (#923, Erstreview #927 Nein P2, Fixlauf; Schema 55), `hok-play-off-marken` (#924, #926; Schema 56, Fixes #896),
`zusehen-rest` (#922, #929; Fixes #908, Integrationsfix eines Tests), `national` (#921, #937; Schema 57), `kader-regeln` (#932,
#940; Schema 58), `transfer-pool` (#931, #942 Nein P2, Fixlauf; A731 nach R22-E1 = A), `einzelpunkte` (#934, #943 Nein P2,
Fixlauf; Schema 59–61), `wettbewerbe-regeln` (#933, #944; Schema 62, #925 Play-offs), `pruefung-karte` (#946, #949; P3 der Runde,
Markerliste V 67 → 5), `marker-x` (#952, #953; 72 X-Markerzeilen auf `[ORIGINAL]`). Prozess-PR #947 (#950): Hängeschutz für
Testläufe (Fixes #935). Gesamtreview (#955, ChatGPT/Codex) **Ja** für `350f550`: keine neuen Produktfehler.

**Abschluss (22.11.2026):** offen für Runde 23: #939 (sechster Feldspieler, R22-E2 = B), #948 (V A873, A913, A951), #951
(`pruefen.sh`-Punkte), #954 (P3-Verdacht marker-x), #925 (DEB-Pokal-Rest), Reste aus #938 (P3-1 A876, P3-3) und #930 (A917),
Auswertung der DOSBox-Aufnahme `runde-22` (21 D, nach Aufnahme durch den Nutzer); 24 X-Funktionszeilen bis zur Abschlussprüfung.

**Entscheidungen während der Runde:** Nutzer 17.11.2026: R22-D1 = A, R22-D2 = A, R22-D3 = B (`marker-x` als Welle 4), R22-D4 = A,
R22-D5 = A; Go. **R22-E1 = A** mit E (A731: Wurf beim Anklicken als Kern-Aktion, vorher Prüfung in em.exe – Wurf liegt im
Klickzweig 0x47347). **R22-E2 = B** (sechster Feldspieler eigene Spur Runde 23, #939). Nutzer 17.11.2026: Schutz gegen hängende
Tests als Prozess-PR (#935), Aufräumen des gesamten Repos als letzter Schritt vor v1.0 (#936). Nutzer 22.11.2026: Läufe melden
ihr Ende selbst (Push-Benachrichtigung; `anhang.md` F). Leitstelle: Integrationsfolge nach Fertigstellung statt Planfolge
(`zusehen-rest` und `hok-play-off-marken` vor `national`; `kader-regeln` und `transfer-pool` vor `einzelpunkte` und
`wettbewerbe-regeln`); Schemanummern beim Rebase vergeben (55–62); A553 und A808 mit minimalen Aufrufen in `TacticsScreen.cs`
bzw. `LineupScreen.cs` (wie A858); #908 für die Lesart der Spur (Import = Laden, `beleg-strittig` entschieden); Abgleichpaare
FILE02→03 (LEER3) und FILE38→39 (KJELLBERG) als zufallsabhängig (`Chance`, R15-D2 = A); 24 X-Funktionszeilen bis zur
Abschlussprüfung; Läufe vom 18.–22.11. warteten auf die Leitstelle (Ende ohne Meldung).

## Aktuelle Runde: Runde 22 – Restliste Klasse V: Kaderplatz, Nationalteam, Kader, Transfer, Wettbewerbe, Einzelpunkte (Plan 17.11.2026, Go 17.11.2026)

Runde 21 abgeschlossen am 17.11.2026: [archiv/runde-21.md](archiv/runde-21.md). Grundlage: Restliste
[analyse/restliste-v1.md](analyse/restliste-v1.md) (V 67, D 21; „Klasse V nach Themen“, „Zuschnitt Runde 21 ff.“), neue V aus
[analyse/re-vor-dosbox.md](analyse/re-vor-dosbox.md), Issues #896, #897, #899, #908, #912, #914. **Ziel der Runde:** alle 67 V
umsetzen (Zuordnung je ID unten, vollständig geprüft), dazu die neuen Befunde aus #897/#908; X auf `[ORIGINAL]` (`marker-x`), danach
Generatorlauf. Danach bleiben bis zur Abschlussprüfung nur D (DOSBox, R22-D4) und die Abweichungsliste an den Nutzer.
**Entscheidungen Nutzer 17.11.2026** (Optionen im Plan-PR #917): R22-D1 = A, R22-D2 = A, R22-D3 = B, R22-D4 = A, R22-D5 = A. Plan-Review #918 Ja (P3-1…3 eingearbeitet). **Go Nutzer 17.11.2026** (alle Spuren samt Folgeläufen, Modelle wie unten).

**Regeln wie Runde 21** (Archiv): Base = aktueller `main` beim Start der Spur, tatsächlicher Base-SHA in den Draft-PR; ein Branch
`paket/<spur>`, ein PR, Teile als Commits, höchstens drei Fortsetzungen; Befund reproduzieren, roter Regressionstest mit Fundstelle
`Datei 0x…`, dann grün; Mehrsaisonprobe (4 Wechsel, passiv/aktiv) bei Saisonwechsel, Kader oder Geld; `[ORIGINAL]`/`[APPROX]`;
fremde Tests nach R15-E2 = C, erwartete Werte nach R21-D2 = A (je Testdatei, Originalsollwerte nie bloß neu eintragen); keine
geplanten Aufgaben aus Läufen (R15-E4). Bei Unklarheit „wie im Original“, sonst `[APPROX]` mit Begründung, die Leitstelle bündelt.
N2 = B, N6 = A, R21-E1 = A gelten. **Markerliste:** wie R21-D3 – kein `markerliste.py --write` außer in `pruefung-karte`; jede Spur
nennt im PR die geänderten, neuen und entfallenen Markerzeilen (Datei, alte ID, neue Klasse). Neue Spielstandfelder: Migration je
Spur in eigener Datei `Core/SaveGame.Migration.<Spur>.cs`. **Entwickler und Erstreviews sind Claude-Sitzungen;** ChatGPT nur fürs
Gesamtreview.

**Folge (R22-D1 = A, R22-D3 = B):** Welle 1 parallel `kaderplatz-werte`, `national`, `zusehen-rest`, `hok-play-off-marken`
(Integration in dieser Folge). Welle 2 nach Merge von `kaderplatz-werte` parallel `transfer-pool`, `kader-regeln`,
`wettbewerbe-regeln`, `einzelpunkte` (Integration in dieser Folge, Spätere rebasen und ziehen erwartete Werte nach). Welle 3
`pruefung-karte` nach Merge aller übrigen, Welle 4 `marker-x` nach Merge von `pruefung-karte`. Danach Gesamtreview (R22-D5).
**DOSBox (R22-D4 = A):** Die Leitstelle legt nach dem Go die Aufnahmeliste `runde-22` (21 D) an; Aufnahme nach Lust des Nutzers,
Auswertung in Runde 23.

**Modelle (R22-D2 = A; Nutzer 17.11.2026 „wenig Tokens für brauchbares Ergebnis“):** Opus 5.5, wo Spielregeln aus
em.exe, Datenmodell oder Zufallsfolge geändert werden (Sonnet provoziert dort Nacharbeit, Nutzer 09.11.2026); Sonnet 5.5, wo die
Fundstellen schon gelesen sind und die Änderungen klein und einzeln sind; Haiku nicht vorgesehen (jeder Lauf baut, testet und liest
Fundstellen nach). Erstreviews von Code Opus 5.5, von `pruefung-karte` und `marker-x` Sonnet 5.5. Fixläufe im Modell der Spur, Fixläufe ohne
Befund (nur Rebase/erwartete Werte) Sonnet 5.5. Plan-Review Sonnet 5.5. Gesamtreview ChatGPT GPT-6.1 Terra (Codex).

| Spur | Welle | IDs | Modell | Grund |
|---|---|---|---|---|
| `kaderplatz-werte` | 1 | A093, A545, A674, A690, A691, A714 | Opus 5.5 | L, Datenmodell + Migration, verschiebt Zufallsverbrauch |
| `national` | 1 | A424, A097, A551, A109, A494, A497, A498, A499, A628, A825, A020, A487, A493, A412, A455 | Opus 5.5 | Satz 5 getrennt vom Vereinsplatz, WM-Schleife, viele Fundstellen |
| `zusehen-rest` | 1 | A832, A834, A837, A840, A858; #908 | Opus 5.5 | Regeln an der Reihenliste, HOK-Import, Managerpaarung |
| `hok-play-off-marken` | 1 | A612 (#896) | Opus 5.5 | em.exe zuerst, Modell + Export + Import + Migration |
| `transfer-pool` | 2 | A391, A713, A731, A864; #897 `ChangeList`, Poolplatz 179 | Sonnet 5.5 | S, Fundstellen gelesen, einzelne Stellen |
| `kader-regeln` | 2 | A054, A518, A060, A462, A118, A120, A490, A683, A827, A394, A631 | Opus 5.5 | M–L, Level je Spiel, Gehalt, Aufstellen, Mehrsaison |
| `wettbewerbe-regeln` | 2 | A071, A105, A191, A549, A553, A670, A384, A101, A443, A456, A677, A836, A838; #897 Gegentore, `SlotSeed`; Teil-V A808 | Opus 5.5 | Spielrechnung und Zufallsfolge |
| `einzelpunkte` | 2 | A367, A371, A582, A104, A847, A206, A421, A671, A698, A672, A665, A492; Teil-V A673/A681; #897 Finanzübersicht | Sonnet 5.5 | je S, Fundstellen gelesen (`re-vor-dosbox`, Inventur) |
| `pruefung-karte` | 3 | #912, #914, Markerliste, Funktionskarte | Sonnet 5.5 | mechanisch, Einordnung nach PR-Angaben |
| `marker-x` | 4 | Klasse X → `[ORIGINAL]` (R22-D3 = B), zweiter Generatorlauf | Sonnet 5.5 | mechanisch, Fundstellen im Feld „Beleg“ |

### Zuständigkeit für gemeinsame Dateien

- Je Datei nur die Stellen der eigenen IDs: `Core/MatchEngine.Managed.cs` (`kaderplatz-werte` A093, `national` A551,
  `wettbewerbe-regeln` A549), `Core/MatchEngine.Live.cs` (`kaderplatz-werte` A545, `zusehen-rest` A837,
  `wettbewerbe-regeln` A836, A838, `CreditLineup`), `Core/MatchEngine.TeamValues.cs` (`national` A109, `wettbewerbe-regeln` A105),
  `Core/CupsNational.Lineup.cs` (`kader-regeln` A490, `einzelpunkte` A492), `Core/Transfer.PoolDistribution.cs`
  (`kaderplatz-werte` A714, `transfer-pool` A713), `Core/Squad.Lineup.cs` (`kader-regeln` A683, `einzelpunkte` A681),
  `Core/SeasonFlow.cs` (`wettbewerbe-regeln` A670, `einzelpunkte` A671), `UI/Screens/Live/LiveMatchScreen.cs` (`national` A455,
  `wettbewerbe-regeln` A456), `Core/OriginalImport.Rest.cs` nur `zusehen-rest` (#908), `Core/OriginalImport.Playoffs.cs`,
  `Core/OriginalExport.Cups.cs`, `Core/Playoffs.cs` nur `hok-play-off-marken`; `Core/Transfer.Contract.cs` nur `einzelpunkte` (A206),
  `Core/MatchEngine.Statistics.cs` nur `einzelpunkte` (A104) (Review #918 P3-1); `UI/Screens/Team/LineupScreen.cs` und
  `Core/GameState.DailyRun.cs` (`UpdateManagerTeamValues`) nur `zusehen-rest` (A858), `Core/CupsNational.GameState.cs` nur
  `national` (P3-2).
- **Schema und Import (P3-3):** Die Schemanummer (`Core/SaveGame.cs`: `SchemaVersion`, Migrationstabelle) vergibt jede Spur mit
  Migration beim Rebase in Integrationsfolge (nächste freie Nummer); in `SaveGame.cs` nur diese Stellen. `Core/OriginalImport.cs`
  nur `kaderplatz-werte` (Import der Kaderplatzwerte, z. B. HiddenSkill); übrige Spuren nur ihre `OriginalImport.<Teil>.cs`.
- `docs/analyse/restliste-v1.md` (generierter Block, Zahlen), `restliste-v1-klassen.csv`, `markerliste.py --write`,
  `funktionskarte.py --write` allein `pruefung-karte` (Welle 3) und danach `marker-x` (Welle 4, R22-D3 = B).

### Spur `kaderplatz-werte` – Werte am Pool-/Kaderplatz statt am Spieler (Welle 1)

- **Teile:** 1. Datenmodell: versteckte Werte, Belastung, Torfeld 0x1B, Drift am Kaderplatz (A674, A690, A691, A545, A714) samt
  Migration und HOK-Import; 2. A093 Technikabzug außerhalb der eigenen Reihe (Kaderplatz +0x02, Datei 0x98F8; #899 P3-1: Abzug auch
  bei +0x02 = 0, Vergleich [+0x11]·[bp−0xA]/100, Datei 0x09A54–0x09A8A; nur „im Spiel“, Schalter 0x977F ≠ 0) mit Stand `a093`
  (FILE71–74); 3. erwartete Werte (R21-D2), Mehrsaisonprobe.
- **Dateien:** `Core/Squad.HiddenSkills.cs`, `Core/Squad.Training.cs`, Stellen oben, `Core/SaveGame.Migration.KaderplatzWerte.cs`,
  zugehörige Tests. **Erstreview:** Opus 5.5. **Status:** Teile 1–3 fertig (PR #923); Fixlauf R923-01/R923-02 (Review #927) erledigt, wartet auf Delta-Prüfung der
  Leitstelle.

### Spur `national` – Satz des Bundestrainers und WM (Welle 1)

- **Teile:** 1. Satz 5 und Managerwert (A424, A097, A551, A109, A494 – Einsatz des Vorspiels je Land); 2. WM-Schleife und Modus
  „nur Weltkugel“ (A497, A498, A499, A628, A825); 3. Kandidaten und Kader (A020, A487, A493, A412 HOK-Import); 4. Eingriff beim
  Nationalteam im Live-Spiel (A455; 0x48E28 für Manager 5 zuerst lesen). Mehrsaisonprobe.
- **Dateien:** `Core/CupsNational*.cs` (ohne A490/A492), `Core/CupsWorld.cs`, `Core/MatchEngine.National.cs`,
  `Core/MatchEngine.Minutes.cs` (A097), `Core/Player.Competitions.cs`, `Core/OriginalImport.National.cs`, Stellen oben.
  **Erstreview:** Opus 5.5. **Status:** fertig, wartet auf Erstreview (PR #921): Teile 1 (A424, A097, A551, A109, A494), 2 (A497, A498, A499, A628, A825), 3 (A020, A487, A493, A412) und 4 (A455). Fixlauf 17.11.2026: Rebase auf `c58e738` (#923 Schema 55, #922, #924 Schema 56), eigene Migration auf Schema 57, Erwartungswerte nach R21-D2 = A nachgezogen; wartet auf Delta-Prüfung der Leitstelle.

### Spur `zusehen-rest` – Zusehen und Aufstellung beim Import (Welle 1)

- **Teile:** 1. A832 Tormeldung, A834 Import der Markierung 0x34/0x22C; 2. A837/A840 Managerregeln an der Reihenliste statt am
  Manager; 3. #908: Reihentausch beim HOK-Import (0x375/0x376, FILE72 → FILE73 behält den Tausch wie im Original) und A858 0x952C
  beim Verlassen der Vereinsaufstellung (Datei 0x41506–0x4151A); Kommentar `Squad.Strength.cs:88`/`TeamwertAufstellungTests.cs:139`. A858 nur der Vereinsfall
  (Aufruf in `LineupScreen.cs`, Kern in `GameState.DailyRun.cs`); der Bundestrainerfall (`OnNationalLineupLeft`) bleibt unberührt.
- **Dateien:** `Core/GameState.Watch.cs`, `Core/MatchEngine.Scoreboard.cs`, `Core/Squad.Strength.cs`, `UI/Screens/Team/LineupScreen.cs`,
  `Core/GameState.DailyRun.cs` (nur A858), Stellen oben, Tests.
  **Erstreview:** Opus 5.5. **Status:** PR #922, Teile 1–3 fertig (Teil 3: A858 umgesetzt; #908-Import bleibt – FILE73 ohne Laden entstanden, Beleg im PR), Review offen; Fixlauf 17.11.2026: Rebase auf `ce658d4`, Tauschprobe FILE71→FILE73 gegen Zufallsverletzung nach A093 (#923) gehärtet, wartet auf Delta-Prüfung der Leitstelle.

### Spur `hok-play-off-marken` – Marken der Play-off-Spiele (Welle 1, R21-E2 = A)

- **Teile:** 1. em.exe 0x32AA3/0x38402/0x376F0 lesen (Bedeutung der Marken, auch bei klaren Serien wie 4:2); 2. Modell
  (`Core/Playoffs.cs`), Export, Import, Migration; roter Test mit Original-Spielstand. Details: PR #892 „Restlücken“. Fixes #896.
- **Erstreview:** Opus 5.5. **Status:** Teile 1–2 fertig (PR #924, 17.11.2026), wartet auf Erstreview; Schema 55 vorläufig.

### Spur `transfer-pool` – Transfer und Pool (Welle 2)

- **Teile:** A391 Poolplatz eines verlorenen Spielers und #897 Poolplatz 179; A713 Verteilung am Saisonwechsel; A731 Angebote für
  eigene Spieler; #897 `TransferMarket.ChangeList` Gehalt = Wert(Gehalt) statt 0 (0x497A1); A864 Verletzung außerhalb des Spiels
  (Datei 0x07462–0x07548, Wurf im Strom). Ohne A247, A701, A715 (N2 = B). Mehrsaisonprobe.
- **Dateien:** `Core/Transfer*.cs` (PoolDistribution nur A713, ohne `Transfer.Contract.cs`), Tests. **Erstreview:** Opus 5.5. **Status:** Draft-PR #931 (Claude Code): A391, A713, #897 `ChangeList`, Poolplatz 179 (kein Befund), A864 fertig, Mehrsaisonprobe; A731 nach Entscheidung R22-E1 = A (Wurf beim Anklicken, `RollBid`) umgesetzt; Fortsetzung 1 von 3. Fixlauf Review #942 (P2-1 Gruppe 3 = sieben Vereine ohne Liga [APPROX], P3-1, P3-2), Rebase auf main 4a3a724.

### Spur `kader-regeln` – Level, Gehalt, Aufstellen, Talente (Welle 2)

- **Teile:** 1. Spiel-Level und Start-Stärke je Spiel (A054, A518, A060, A462, Highscore-Schlüssel); 2. Gehalt geliehener Spieler,
  Forderung (A118, A120); 3. Aufstellen andere Hälfte/sechster Feldspieler (A683, A490); 4. A827, A394, A631. Mehrsaisonprobe.
- **Dateien:** `Core/GameSetup.cs`, `Core/GameState.AddManager.cs`, `Core/Highscore.cs`, `Core/Player.Contract.cs`,
  `Core/Player.Factory.cs`, `Core/FinanceTalent.cs`, `Core/Squad.Setup.cs`, Stellen oben. **Erstreview:** Opus 5.5.
  **Status:** Teile 1–4 fertig (#932, sechster Feldspielerplatz bleibt [APPROX]), wartet auf Erstreview.

### Spur `wettbewerbe-regeln` – Spielrechnung, Wertung, Live-Einzelregeln (Welle 2)

- **Teile:** 1. Spielrechnung und Wertung (A071, A105, A191, A549, A553, A836, A838; #897 Gegentore des Managertorhüters nur in der
  Liga, 0xA3F7); 2. Tagesablauf (A670 mit A384; #897 `SlotSeed`: Ligamischung am 29. April aus `lrandom`, Datei 0x5644E); 3. Live-
  Einzelregeln (A443, A456, A677, Teil-V A808) und A101 Penaltyschießen (Rechnung, Anzeige mit Markierung aus `zusehen`).
- **Dateien:** `Core/MatchEngine.*.cs` (Stellen oben, ohne `MatchEngine.Statistics.cs`), `Core/SeasonFlow.LeagueSlots.cs`, `Core/GameState.Manager.cs`,
  `Core/StadiumStatistics.cs`, `Core/Squad.Lineup.InMatch.cs`, `UI/Screens/Team/PlayerHintLine.cs` (A808), Tests.
  **Erstreview:** Opus 5.5. **Status:** Teile 1–3, A553 und A808 fertig (A808: Kernabfrage `MatchEngine.PenaltyBoxMinutes`, `LineupScreen.cs` minimal nach Entscheidung Leitstelle 22.11.2026), Erstreview #944 Ja (`3ca753a`); Rebase-Fixlauf 22.11.2026 auf `main` f7f0410 (Squash je Teil, Schema 62, Sollwerte nach R21-D2 = A); zwei neue Zufallsmuster im Abgleich vorläufig optional – Entscheidung Leitstelle.

### Spur `einzelpunkte` – kleine V (Welle 2)

- **Teile:** 1. Bedienung: A367, A371, A582, A104, A847, A492 mit Teil-V A673/A681 (Schalter „automatisch aufstellen“), A672;
  2. Saisonablauf: A206 (#899 P3-2: Schleife 0x5613E Liga 0–2, Rücksetzung 0x565E0 Liga 3–7), A421, A671, A698, A665; 3. #897
  Finanzübersicht Zeilen 3/4 und 6/7 wie im Original (Σ Darlehen, Monatszins; 0x57F96, 0x57C64). Ohne A775 (N2 = B).
- **Dateien:** `UI/Screens/Team/PlayerDataWindow.cs`, `PlayerInfoPopup.cs`, `UI/Screens/Manager/FinanceScreen.cs`,
  `Core/OriginalExport.Rest.cs`, `Core/MatchEngine.Statistics.cs`, `Core/NewGame.cs`, `Core/Transfer.Contract.cs`,
  `Core/FinanceSponsorRating.cs`, `Core/StatisticsHistory.cs`, `Core/SeasonFlow.Zwangsabstieg.cs`, `Core/Squad.AutoLineup.cs`,
  Stellen oben. **Erstreview:** Opus 5.5. **Status:** alle Teile umgesetzt (Claude Code, Draft-PR #934): A367, A371, A582, A104, A847, A492/A673/A681, A672, A206, A421, A671, A698, A665, #897 (Schemata 59–61 nach `kader-regeln`); Fixlauf zu Review #943 (P2-1, P3-1, P3-2; P3-3/P3-4 beantwortet) auf `main` 4a3a724 rebased; wartet auf Delta-Prüfung der Leitstelle.

### Spur `pruefung-karte` – P3 und Markerliste (Welle 3)

- **Teile:** 1. #912 (Sollwert aus Vorbereitung ohne Squad), #914 (je Schema 51–53 eine echte Spielstanddatei als Testdatei, ohne
  Originaldaten), übrige P3 der Runde; A059 Folgeverdacht (#897, #899 P3-3) als E nach R21-E1 = A vermerken; 2. Lauf
  `markerliste.py --write` (alle neuen Marker nach PR-Angaben einordnen, Ziel V = 0) und `funktionskarte.py --write`.
- **Voraussetzung:** Welle 2 gemergt. **Erstreview:** Sonnet 5.5. **Status:** alle Teile umgesetzt (Claude Code, Draft-PR #946): Teil 1 #912, #914 (echte Stände Schema 51–53), #928, #930, #941, #945, #938 (P3-2; Zähler Satz 5 über den Saisonwechsel mit Beleg zurückgestellt, A876), A059-Folgeverdacht als E vermerkt; #925-Rest (DEB-Pokal) ohne Beleg Video/0x32B5F Restlücke; Teil 2 `markerliste.py --write` (90 neue IDs, V 67 → 5: #939 A875/A937 sowie A873, A913, A951), Teil 3 `funktionskarte.py --write`; wartet auf Review (Sonnet 5.5).

### Spur `marker-x` – X-Marker auf `[ORIGINAL]` (Welle 4, R22-D3 = B)

- **Teile:** 1. alle Markerzeilen der Klasse X (nach dem Generatorlauf von `pruefung-karte`) auf `[ORIGINAL]` mit Fundstelle aus dem
  Feld „Beleg“ bzw. Nicht-Marker (Rückblicke, Verweise) ohne `[APPROX]` umformulieren; nur Kommentare, kein Verhalten;
  2. zweiter Lauf `markerliste.py --write` (X = 0, entfallene IDs) und `funktionskarte.py --write`.
- **Dateien:** Kommentarzeilen unter `src/`, `docs/analyse/restliste-v1*.{md,csv}`. **Tests:** `--schnell` (nur Kommentare).
- **Voraussetzung:** `pruefung-karte` gemergt. **Erstreview:** Sonnet 5.5. **Status:** alle Teile umgesetzt (Claude Code, Draft-PR #952): 72 X-Markerzeilen in 46 Dateien nur als Kommentar umformuliert (Code je Zeile unverändert geprüft), `markerliste.py --write` (X 72 → 0, 572 Markerzeilen) und `funktionskarte.py --write`; dazu #951 P3-1…3 (Vermerke in der CSV); 24 X-Funktionszeilen (F-IDs) haben keine Markerzeile und bleiben; wartet auf Review (Sonnet 5.5).

### Gesamtreview Runde 22 (R22-D5)

ChatGPT (Codex, GPT-6.1 Terra) nach Merge von `marker-x`, risikoorientiert (`anhang.md` C), besonders: neue Felder am
Kaderplatz (Migration, Laden/Speichern, Import), Satz des Bundestrainers über mehrere Saisons, Zufallsfolge nach den Welle-2-Spuren.

