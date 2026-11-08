# Arbeitspakete

Die Portierung ist in Pakete aufgeteilt, die parallel in eigenen Chats bearbeitet werden können.
Jedes Paket hat einen Branch `paket/<name>`, eigene Dateien und klare Abnahmekriterien.
Regeln und Einrichtung: [AGENTS.md](../AGENTS.md). Den Status pflegt jedes Paket nur in seinem eigenen Abschnitt.
Abgeschlossene Runden stehen in [archiv/](archiv/) und müssen beim Einstieg nicht gelesen werden.

**Koordination:** Die Leitstelle verteilt Pakete, prüft und mergt Pull Requests und pflegt Fahrplan und Doku
(Rollen: [prozess/zusammenarbeit.md](prozess/zusammenarbeit.md)).

## Aktuelle Runde: Runde 14 – Restpunkte, DOSBox-Auswertung `runde-13`, Import `*.HOK` (Plan 05.11.2026, Go 05.11.2026)

Runde 13 (04.–05.11.2026) ist abgeschlossen: [archiv/runde-13.md](archiv/runde-13.md) (Spuren, Integrationen, Reviews,
Entscheidungen L1–L15, Funktionskarte: Spielcode 90 %); frühere Runden in [archiv/](archiv/).
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
- `Core/GameState.MainMenu.cs` (nur Kommentar #598): `pruefung-karte`.
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
  **Status:** Go 05.11.2026, Start offen.

### Spur `saison` – Satzfolge, Fair-Play, Oberliga

- **Teile:**
  1. **#591, #592:** Mehrsaisonprobe zieht vor `StartNextSeason` Zufall (Gegenprobe ohne Übernahme wird rot);
     `FairPlayPremiumWinner` ohne `SuccessorSlots` mit Rückfall wie `LeagueSlots` (Fundstelle 27213h) oder `[APPROX]`.
  2. **#600-Rest:** `OutsidePlaces` in `SeasonFlow.Oberliga.cs` paart nach Index – gegen das Original prüfen, Kommentar A112
     berichtigen; Mehrsaisonprobe.
  3. **Saisonbefunde aus `re-dosbox`** (nach `re-dosbox`, nur falls dort der Spur `saison` zugeordnet).
  4. **#349 (R14-D4 = A):** Merker „Managerverein spielt“ je Spieltag wie im Original (36FEAh, 3702Eh–37085h) als abschaltbare
     Einstellung; Vorgabe Remake-Verhalten. Test: Spieltag mit Managerverein im Pokal rechnet alle Paarungen in der Minutenschleife,
     Zufallsverbrauch gegenüber Remake-Verhalten verschieden; Mehrsaisonprobe mit beiden Einstellungen. Doku
     `anforderungen/zusatzfunktionen.md` Abschnitt C nachziehen.
- **Dateigrenzen:** `Core/SeasonFlow*.cs`, `Core/GameState.SeasonEndTurn.cs`, für Teil 4 die Auswahl der Minutenrechnung
  (`Core/Cups*.cs` außer `CupsNational*`, `Core/MatchEngine*.cs` nur diese Stelle), ein Schalter in `GameOptionsScreen.cs` und
  im Optionsmodell, `docs/anforderungen/zusatzfunktionen.md`, eigene Tests. **Modell:** Claude Sonnet 5.5.
  **Status:** Go 05.11.2026, Start offen.

### Spur `nationalteam` – Tagesfolge 952Ch, Länderwerte, Nebenbefunde

- **Teile:**
  1. **#594, #595:** Reihenfolge `UpdateNationalTeamValues()` gegenüber der Wanderung wie in der Tagesfunktion 54C9Fh
     (54F9Ch–54FBFh vor 5525Ah–55292h), roter Test an einem Wandertag ohne WM-Spiel; Regressionstest R13RD-04 (FILE20→FILE21).
  2. **Nebenbefunde #580:** Reihenlogik der Vereinsaufstellung wie A010 (Fundstelle nachlesen); Sperre Transfer/Training ab der
     WM-Gruppenphase (Fundstelle, neue Inventur-ID über `pruefung-karte`); WM-Kader-Reihenfolge beim Einsortieren (3CABFh,
     Restlücke #623).
  3. **Bundestrainer-Befunde aus `re-dosbox` Teil 2** (A009 leer, A085 Handaufnahme, A293), sobald Sitzung 5 ausgewertet ist.
- **Dateigrenzen:** wie oben „allein `nationalteam`“, eigene Tests. **Modell:** Claude Opus 5.5. **Status:** Go 05.11.2026, Start offen.

### Spur `kader-start` – Spielstart, Kader, Aufstellung

- **Teile:**
  1. **#605, #625:** Start-Liga in `GameSetup` selbst führen, ohne Manager `false`; Test Einsortieren bei `JoinScoutedTalent`,
     Aufruferliste in `Player.Factory.cs` (Adressen aus #625).
  2. **Spielstart und Kader aus `re-dosbox`** (nach `re-dosbox`): U15-c, A056, A180, A166, A082, A046, A088 (L14), A167, A091,
     A135, A189, A012, A115 – nur abweichend Belegtes umsetzen, Übereinstimmungen auf `[ORIGINAL]`; Mehrsaisonprobe.
  3. **Anschluss Sperre ab WM-Gruppenphase** an Transfer und Training (nach `nationalteam` Teil 2).
  4. **Saisonende-Abgang aus `re-dosbox` Teil 2** (A362, A085, A091), sobald Sitzung 4 ausgewertet ist.
- **Dateigrenzen:** wie oben „allein `kader-start`“, `UI/Screens/Team/*` außer `TeamUi.cs` und `TransferDialog.cs`, eigene Tests,
  `docs/transfer.md`. **Modell:** Claude Opus 5.5. **Status:** Go 05.11.2026, Start offen.

### Spur `ui-rest` – Darstellung aus der Aufnahme, Stadionpfeile

- **Teile:**
  1. **#597:** Eintrittspfeile 10 Pixel breit wie im Original (Aufbau 3D7F8h ff., Grundgrafik kb\4 (20 | 0)), Pixeltest gegen
     `file01_3`.
  2. **Darstellung aus `re-dosbox`** (nach `re-dosbox`): A373 (Pfeilfolge Taktik), Taktik-Befund DGROUP 375h (falls Anzeige),
     A364, A380, A382, A377, A374, A313, A321, A357 – Pixeltests gegen die Bilder der Sitzung 2.
  3. **Stadion aus `re-dosbox` Teil 2** (A323, A325, A326), sobald Sitzung 3 ausgewertet ist; A338 nur, falls Sitzung 6 kommt.
- **Dateigrenzen:** wie oben `ui-rest`, eigene Tests. **Modell:** Claude Sonnet 5.5. **Status:** Go 05.11.2026, Start offen.

### Spur `hok-import` – Original-Spielstände `*.HOK` ins Remake (E14-1 C)

Umfang nach Entscheidung **R14-D2 = B**: Feldkarte und Import, Export in Runde 15.
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
- **Dateigrenzen:** wie oben „allein `hok-import`“, eigene Tests. **Modell:** Claude Opus 5.5. **Status:** Teil 1 (Feldkarte) fertig, PR #633 (08.11.2026); Teil 2 (Import Spielkern) fertig, PR #638 (08.11.2026); Teil 3 (Zugang) fertig, PR #640 (08.11.2026); Teil 4 (Speichern-Dialog A338/A339, L6) nach `re-dosbox` Teil 2, legt die Leitstelle an.

### Spur `pruefung-karte` – Welle 2 (nach der Integration aller Spuren)

- **Inhalt:** #598 (Kommentaradresse 44E22h), #603, #604, #610, #611, #619, #620, #621 (Fundstellen, Tests ohne Wirkung,
  Saaten, Kommentare, Doku-Platzhalter); Inventur-Stand aller Spuren dieser Runde nachziehen, neue ID für die Sperre ab der
  WM-Gruppenphase; Funktionskarte `--write` (zweiter Lauf ohne Diff); Freiprüfung der Bildschirme.
- **Dateigrenzen:** `docs/analyse/inventur.md`, `docs/re-abdeckung.md`, `tools/reverse-engineering/*`, `tests/**/Support/`,
  die in den Issues genannten Test- und Kommentarstellen, `Core/GameState.MainMenu.cs` (nur Kommentar). **Modell:**
  Claude Sonnet 5.5. **Status:** Go 05.11.2026, Start offen.

### Gesamtreview am Rundenende (E14-3)

Vollprüfung durch ChatGPT (Codex mit Netzzugang) nach `vorlagen/review-anfrage.md`, Abschnitt „Gesamtreview am Rundenende“;
Nachprüfung durch Claude nur für das Delta von Fixes. **Modell:** GPT-6.1 Terra (Codex).

### Entscheidungen zum Plan (Nutzer, Go 05.11.2026, PR #627)

- **R14-D1 = A:** sieben Spuren wie oben, Modelle wie in den Spurabschnitten.
- **R14-D2 = B:** `*.HOK` in Runde 14 Feldkarte und Import; Export in Runde 15.
- **R14-D3 = A:** Sitzungen 3–6 über `re-dosbox` Teil 2, umgesetzt in Folgeteilen der Fachspuren bis zu deren
  Integrationsbeginn; später Eintreffendes → Runde 15.
- **R14-D4 = A:** #349 jetzt als Einstellung „wie im Original“, Vorgabe Remake-Verhalten (Entscheidung 01.11.2026 vorgezogen).

### Vorentscheidungen Runde 14 (Nutzer, 04.11.2026, Issue #601)

- **E14-1 = A + C:** Restpunkte und zusätzlich Import/Export von Original-Spielständen `*.HOK` (Grundlage `hok-format`,
  `formate/spielstand-original.md`). `livespiel` bleibt zurückgestellt.
- **E14-2 = A:** Start ohne Warten auf alle Aufnahmen; bereits abgelegte Sitzungen der Aufnahme `runde-13` sofort auswerten,
  spätere in einem Folgeteil derselben Analysespur.
- **E14-3 = A:** Gesamtreview durch ChatGPT als Vollprüfung (`vorlagen/review-anfrage.md`, #614/#617); keine Nachprüfung durch
  Claude außer dem Delta von Fixes.

### Zurückgestellt (Entscheidung Nutzer bzw. fehlende Grundlage)

- `livespiel` (Torszenen, Anzeigetafel, Reporter; Nr. 14/16): Umsetzung als letzter Schritt vor v1.0; Analyse in
  `re-livespiel`. Noch unbekannt und bei der Umsetzung von `livespiel` mitzuanalysieren (zusammen mit L7, `tore\`-Format):
  575C3h (Ov 138h, Lader `tore\`, einziger Aufrufer 567C8h; R10R-01 #387).
- #349: in Runde 14 (R14-D4 = A, Spur `saison` Teil 4).
- Highscore am Spielende und Saisonende im Modus „nur Weltkugel“ (Kandidat für eine DOSBox-Aufnahme mit `hok-patch`).
  Import/Export ist in Runde 14 (E14-1, Spur `hok-import`).
- R8-A01 (Spielstände aus Spurbranches laden nicht): Spurbranches sind keine Ausgabestände.

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
