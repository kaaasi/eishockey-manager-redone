# Arbeitspakete

Die Portierung ist in Pakete aufgeteilt, die parallel in eigenen Chats bearbeitet werden können.
Jedes Paket hat einen Branch `paket/<name>`, eigene Dateien und klare Abnahmekriterien.
Regeln und Einrichtung: [AGENTS.md](../AGENTS.md). Den Status pflegt jedes Paket nur in seinem eigenen Abschnitt.
Abgeschlossene Runden stehen in [archiv/](archiv/) und müssen beim Einstieg nicht gelesen werden.

**Koordination:** Die Leitstelle verteilt Pakete, prüft und mergt Pull Requests und pflegt Fahrplan und Doku
(Rollen: [prozess/zusammenarbeit.md](prozess/zusammenarbeit.md)).

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
  **Erstreview:** Opus 5.5. **Status:** wartet auf Go.

### Spur `zusehen-rest` – Zusehen und Aufstellung beim Import (Welle 1)

- **Teile:** 1. A832 Tormeldung, A834 Import der Markierung 0x34/0x22C; 2. A837/A840 Managerregeln an der Reihenliste statt am
  Manager; 3. #908: Reihentausch beim HOK-Import (0x375/0x376, FILE72 → FILE73 behält den Tausch wie im Original) und A858 0x952C
  beim Verlassen der Vereinsaufstellung (Datei 0x41506–0x4151A); Kommentar `Squad.Strength.cs:88`/`TeamwertAufstellungTests.cs:139`. A858 nur der Vereinsfall
  (Aufruf in `LineupScreen.cs`, Kern in `GameState.DailyRun.cs`); der Bundestrainerfall (`OnNationalLineupLeft`) bleibt unberührt.
- **Dateien:** `Core/GameState.Watch.cs`, `Core/MatchEngine.Scoreboard.cs`, `Core/Squad.Strength.cs`, `UI/Screens/Team/LineupScreen.cs`,
  `Core/GameState.DailyRun.cs` (nur A858), Stellen oben, Tests.
  **Erstreview:** Opus 5.5. **Status:** wartet auf Go.

### Spur `hok-play-off-marken` – Marken der Play-off-Spiele (Welle 1, R21-E2 = A)

- **Teile:** 1. em.exe 0x32AA3/0x38402/0x376F0 lesen (Bedeutung der Marken, auch bei klaren Serien wie 4:2); 2. Modell
  (`Core/Playoffs.cs`), Export, Import, Migration; roter Test mit Original-Spielstand. Details: PR #892 „Restlücken“. Fixes #896.
- **Erstreview:** Opus 5.5. **Status:** wartet auf Go.

### Spur `transfer-pool` – Transfer und Pool (Welle 2)

- **Teile:** A391 Poolplatz eines verlorenen Spielers und #897 Poolplatz 179; A713 Verteilung am Saisonwechsel; A731 Angebote für
  eigene Spieler; #897 `TransferMarket.ChangeList` Gehalt = Wert(Gehalt) statt 0 (0x497A1); A864 Verletzung außerhalb des Spiels
  (Datei 0x07462–0x07548, Wurf im Strom). Ohne A247, A701, A715 (N2 = B). Mehrsaisonprobe.
- **Dateien:** `Core/Transfer*.cs` (PoolDistribution nur A713, ohne `Transfer.Contract.cs`), Tests. **Erstreview:** Opus 5.5. **Status:** wartet auf Go.

### Spur `kader-regeln` – Level, Gehalt, Aufstellen, Talente (Welle 2)

- **Teile:** 1. Spiel-Level und Start-Stärke je Spiel (A054, A518, A060, A462, Highscore-Schlüssel); 2. Gehalt geliehener Spieler,
  Forderung (A118, A120); 3. Aufstellen andere Hälfte/sechster Feldspieler (A683, A490); 4. A827, A394, A631. Mehrsaisonprobe.
- **Dateien:** `Core/GameSetup.cs`, `Core/GameState.AddManager.cs`, `Core/Highscore.cs`, `Core/Player.Contract.cs`,
  `Core/Player.Factory.cs`, `Core/FinanceTalent.cs`, `Core/Squad.Setup.cs`, Stellen oben. **Erstreview:** Opus 5.5.
  **Status:** wartet auf Go.

### Spur `wettbewerbe-regeln` – Spielrechnung, Wertung, Live-Einzelregeln (Welle 2)

- **Teile:** 1. Spielrechnung und Wertung (A071, A105, A191, A549, A553, A836, A838; #897 Gegentore des Managertorhüters nur in der
  Liga, 0xA3F7); 2. Tagesablauf (A670 mit A384; #897 `SlotSeed`: Ligamischung am 29. April aus `lrandom`, Datei 0x5644E); 3. Live-
  Einzelregeln (A443, A456, A677, Teil-V A808) und A101 Penaltyschießen (Rechnung, Anzeige mit Markierung aus `zusehen`).
- **Dateien:** `Core/MatchEngine.*.cs` (Stellen oben, ohne `MatchEngine.Statistics.cs`), `Core/SeasonFlow.LeagueSlots.cs`, `Core/GameState.Manager.cs`,
  `Core/StadiumStatistics.cs`, `Core/Squad.Lineup.InMatch.cs`, `UI/Screens/Team/PlayerHintLine.cs` (A808), Tests.
  **Erstreview:** Opus 5.5. **Status:** wartet auf Go.

### Spur `einzelpunkte` – kleine V (Welle 2)

- **Teile:** 1. Bedienung: A367, A371, A582, A104, A847, A492 mit Teil-V A673/A681 (Schalter „automatisch aufstellen“), A672;
  2. Saisonablauf: A206 (#899 P3-2: Schleife 0x5613E Liga 0–2, Rücksetzung 0x565E0 Liga 3–7), A421, A671, A698, A665; 3. #897
  Finanzübersicht Zeilen 3/4 und 6/7 wie im Original (Σ Darlehen, Monatszins; 0x57F96, 0x57C64). Ohne A775 (N2 = B).
- **Dateien:** `UI/Screens/Team/PlayerDataWindow.cs`, `PlayerInfoPopup.cs`, `UI/Screens/Manager/FinanceScreen.cs`,
  `Core/OriginalExport.Rest.cs`, `Core/MatchEngine.Statistics.cs`, `Core/NewGame.cs`, `Core/Transfer.Contract.cs`,
  `Core/FinanceSponsorRating.cs`, `Core/StatisticsHistory.cs`, `Core/SeasonFlow.Zwangsabstieg.cs`, `Core/Squad.AutoLineup.cs`,
  Stellen oben. **Erstreview:** Opus 5.5. **Status:** wartet auf Go.

### Spur `pruefung-karte` – P3 und Markerliste (Welle 3)

- **Teile:** 1. #912 (Sollwert aus Vorbereitung ohne Squad), #914 (je Schema 51–53 eine echte Spielstanddatei als Testdatei, ohne
  Originaldaten), übrige P3 der Runde; A059 Folgeverdacht (#897, #899 P3-3) als E nach R21-E1 = A vermerken; 2. Lauf
  `markerliste.py --write` (alle neuen Marker nach PR-Angaben einordnen, Ziel V = 0) und `funktionskarte.py --write`.
- **Voraussetzung:** Welle 2 gemergt. **Erstreview:** Sonnet 5.5. **Status:** wartet auf Go.

### Spur `marker-x` – X-Marker auf `[ORIGINAL]` (Welle 4, R22-D3 = B)

- **Teile:** 1. alle Markerzeilen der Klasse X (nach dem Generatorlauf von `pruefung-karte`) auf `[ORIGINAL]` mit Fundstelle aus dem
  Feld „Beleg“ bzw. Nicht-Marker (Rückblicke, Verweise) ohne `[APPROX]` umformulieren; nur Kommentare, kein Verhalten;
  2. zweiter Lauf `markerliste.py --write` (X = 0, entfallene IDs) und `funktionskarte.py --write`.
- **Dateien:** Kommentarzeilen unter `src/`, `docs/analyse/restliste-v1*.{md,csv}`. **Tests:** `--schnell` (nur Kommentare).
- **Voraussetzung:** `pruefung-karte` gemergt. **Erstreview:** Sonnet 5.5. **Status:** wartet auf Go.

### Gesamtreview Runde 22 (R22-D5)

ChatGPT (Codex, GPT-6.1 Terra) nach Merge von `marker-x`, risikoorientiert (`anhang.md` C), besonders: neue Felder am
Kaderplatz (Migration, Laden/Speichern, Import), Satz des Bundestrainers über mehrere Saisons, Zufallsfolge nach den Welle-2-Spuren.

### Zurückgestellt (Entscheidung Nutzer bzw. fehlende Grundlage)

- R8-A01 (Spielstände aus Spurbranches laden nicht): Spurbranches sind keine Ausgabestände.

### Vormerkungen

- **Runde 22:** #896, #897, #899, #908, #912, #914 – den Spuren oben zugeordnet.
- **Testlücke Gesamtreview #848:** Speichern/Laden mitten in einer angehaltenen Live-Paarung (über die Oberfläche nicht
  erreichbar; prüfen, sobald ein Speicherweg im Live-Spiel dazukommt).
- **Vor v1.0 (R20-D3-Zusatz, Nutzer 15.11.2026):** Liste aller noch abweichenden Bereiche und Funktionen an den Nutzer
  (`python3 tools/reverse-engineering/markerliste.py --abweichungen`), der entscheidet, was noch geändert wird.
- **Mit v1.0 und dem bitgenauen Zufallsmodus (N6 = A, Nutzer 16.11.2026):** eigene Zufallsquellen der Darstellung (A061, A271,
  A274, A438, A452) erneut prüfen.
- **Nach v1.0:** Einstellungen aus R15-E3, bitgenauer Zufallsmodus (`anforderungen/zusatzfunktionen.md` C), Sprachen und
  Währungen (N1 = B, `anforderungen/zusatzfunktionen.md` A 2).

## Paket `hok-format` – Original-Spielstände `*.HOK`

Branch: `paket/hok-format` · Status: Analyse gemergt (#176, Review #178): Verschlüsselung, Prüfsummen, Kopf und Blockfolge
belegt, `emtool hok dump`, Doku `docs/formate/spielstand-original.md` (offene Liste dort). Entscheidung Nutzer:
Sicher-/Ladesperre des Originals nicht übernehmen. Weiter in Runde 7 mit `hok-patch` (Spielstände fürs Original);
Import/Export ins Remake danach.

- Nachtrag (#197, Branch `paket/hok-format-2`): Kaderplätze, Spielerpool, Training und Taktik an FILE05 belegt;
  Kaderausgabe in `emtool hok dump`. Spielstände FILE01–05 und `KAPUTT.HOK` im Referenz-Repo `original/spielstaende/`.

## Paket `wettbewerbe` – Paarungen, Stärken, Statistiken, Pokale

Branch: `paket/wettbewerbe` · Status: PR 1–7 gemergt (Runde 3); Restarbeiten in Runde 4 gemergt; PR 8 in Runde 5 gemergt (`paarungen-liga`, #128). Regeln und Belege: [wettbewerbe.md](wettbewerbe.md)

| PR | Inhalt | Stand |
|---|---|---|
| 1 | DEB-Pokal: Auslosung, Heimrecht, „best of three“, Abrechnung, Bildschirm „DEB-Pokal Auslosung“ | gemergt (Runde 3, #54) |
| 2 | Europapokal (Gruppe + Endrunde), Bildschirm Gruppen/Endrunde | gemergt (Runde 3, #54) |
| 3 | WM (2 Gruppen + K.-o.), Bildschirm Gruppe/Tabelle WM | gemergt (Runde 3, #54) |
| 4 | Stärken (Ligen, Länderteams) | gemergt (Runde 3, #54) |
| 5 | Statistiken: Ewige Tabelle (Totale), Früher | gemergt (Runde 3, #54) |
| 6 | Bundestrainer: WM-Kader, „WM Vorbereitung“ (Freundschaftsspiele), Etat; WM: Werte Deutschlands neu ausgewürfelt, Länder 20-mal getauscht | gemergt (Runde 3, #54) |
| 7 | Spielmodus „nur Weltkugel“: Spielvorbereitung, Vereinssaison überspringen, Menü des Bundestrainers | gemergt (#87 über #89) |
| 8 | Paarungen Liga | gemergt (Runde 5, #128); Pixelvergleich Runde 6 (`pixel-wettbewerbe`) |
| – | Statistiken Spieler, Verein, Fairplay, WM-Statistik | Runde 8 (`statistiken`, `nationalteam`) |

- Paarungen (Liga, Playoffs, DEB-Pokal, Europapokal, WM), Freundschaftsspiele.
- Stärken (Ligen, Länderteams: Gesamt/Abwehr/Sturm, Smileys), Statistiken (Totale, Früher, Verein, Spieler, Fairplay, WM).
- DEB-Pokal (32 Teams), Europapokal (Gruppe + Endrunde), WM (2 Gruppen + K.-o.).
- Eigene Dateien: `Core/Cups*.cs`, `Core/Statistics*.cs`, `UI/Screens/Competitions/*`, `UI/Screens/Statistics/*`.

## Paket `speichern` – Laden und Sichern

Branch: `paket/speichern` · Status: Format, Dialog und automatisches Speichern gemergt (Runde 3, [archiv/runde-3.md](archiv/runde-3.md)); #80 in Runde 4 behoben (#105)

- Eigenes Spielstandformat, Laden/Sichern-Dialog wie im Original (Dateiliste, Verzeichnis, Dateiname).
- Optional: Import von Original-Spielständen `*.HOK` (Verschlüsselung in `dateiformate.md`) und Export per Vorlage (`hok-format`) – wartet auf Original-Spielstände des Nutzers.

## Später (nach v0.5)

- `livespiel` – Anzeigetafel, Reporter, Torszenen auf den Ereignissen aus `spielberechnung` (optional abschaltbar, letzter Schritt vor v1.0)
- Zusatzfunktionen nach `docs/anforderungen/zusatzfunktionen.md`
