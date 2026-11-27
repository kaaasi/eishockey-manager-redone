# Nachprüfung: Integration Runde 14, Spur `hok-import` (PR #670)

Nachprüfung des Reviews `docs/reviews/2026-11-09-integration-hok-import-r14.md`. Bisher geprüfter Head `38ebbad`, jetzt das Delta
`38ebbad7ca78101bd726e3d6172c9b420456af4e..f849736b72d0521774f56f5f19f56a3b983430dd` und die Befunde R14HI-01 bis -07.
Reviewer: Claude (geplanter Lauf, frische Sitzung ohne Implementierungskontext), 09.11.2026.

Checkout: Klon, `git rev-parse HEAD` = `f849736`, `git status --porcelain` leer. Mutationen und Prüfprogramm liefen nur in zwei
eigenen Worktrees desselben Heads. Ausgeführt wurde mit Originaldaten (`kaaasi/eishockey-manager-referenz` `779a1c8`, 52 Stände
und `KAPUTT.HOK`). Das Prüfprogramm ist eine temporäre Testdatei gegen die öffentliche API von `Core`/`Formats` und wurde nicht
committet. Zum Entschlüsseln dient `OriginalSaveGame.Read`; der Leser liegt nicht im Delta und ist im ersten Review unabhängig
geprüft.

H = `https://github.com/kaaasi/eishockey-manager-redone/blob/f849736b72d0521774f56f5f19f56a3b983430dd`

## 1. Merge-Empfehlung

**Ja** für Head `f849736`.

- R14HI-01 bis -07 sind behoben. Alle Gegenbeispiele sind erneut ausgeführt und stimmen jetzt mit dem Original überein.
- Die neuen Fundstellen sind in em.exe nachgelesen und an den Hexdumps von FILE22 und FILE44 bestätigt.
- Das Fuzzing fand keine Ausnahme nach außen.
- Alle drei Läufe von `tools/pruefen.sh` sind grün.
- Der Merge `153c031` bringt nur die Review-Datei aus `main`.
- Es bleiben drei P3-Befunde (Abschnitt 2). Sie blockieren den Merge nicht.

## 2. Befunde

### Stand R14HI-01 bis -07

| Befund | Ergebnis |
|---|---|
| R14HI-01 | **behoben.** FILE44 Aufstiegsrunde Bundesliga: Teilnehmer `CFFC` 00 03 10 0F, Platzliste `CD88+12` = 03 02 00 01. Import: EC Kassel 10:2, ES Weißwasser 10:2, EHC Freiburg 3:9, SC Preussen Berlin 1:11; aufsteigend Kassel und Weißwasser. FILE44 → Saisonwechsel: alle drei Ligen wie FILE46, keine Abweichung. FILE22: EC Ratingen, ERC Schwenningen, EV Landshut, ECD Sauerland bzw. Augsburger EV, SC Riessersee, EV Landsberg, ESC Wedemark (Teilnehmer wie im HOK). FILE22 → Saisonwechsel: Bundesliga und 2. Liga wie FILE23 (Schwenningen oben). In der Oberliga steht nur ein Nachrücker aus der Regionalliga anders (TEV Miesbach / TuS Geretsried, Zufall, im Test begründet). Alle Stände ab Hauptrundenende: nur FILE22 und FILE44 haben eine Vereinssaison. FILE11, 18–21 und 47–49A sind „nur Weltkugel“ mit `CFFC`[0..7] = 0 und `CCA3` = 0 (selbst gedumpt), also ohne Play-offs. Die Meister stimmen mit `D0E6` (Köln bzw. Kaufbeuren) |
| R14HI-02 | **behoben.** FILE43: Sitzkomfort 0 → 1 mit 8 Wochen (Rohbytes `+0x11D` = 01, `+0x12B` = 08). FILE44A: Gastronomie 2 → 3 mit 1 Woche, nach einer Woche Stufe 3. Der Import bucht nicht: Konto = `+0x18F`/3 (668.860), kein `Book` in `OriginalImport*.cs`. Der Kontoverlauf über 7 Tage gleicht FILE41 ohne Baustelle (−103 T gegen −109 T) |
| R14HI-03 | **behoben.** Werbevertrag 1.000.001: Der Stand lädt, der Vertrag hat 333.333 DM über 6 Monate. Gegenprobe mit drei Banden im selben Vertrag: lädt mit Flächen [0 1 2]. `TryImport` fängt jetzt jede Ausnahme. Fuzzing siehe Abschnitt 3 |
| R14HI-04 | **behoben.** Jede Zeile der Tabelle ist an den genannten Ständen nachgeprüft, Ergebnis unten. Die neuen N/U-Zeilen (Vorjahres-Titelträger, Marken, Bonus, `026C`, Pool `+0x1E`, `CD88`) sind unter „Noch nicht übernommen“ begründet; die Begründungen sind tragfähig |
| R14HI-05 | **behoben.** Rückmutationen M03 (N24), M09 (N25) und M45 (N26) sind jetzt rot |
| R14HI-06 | **behoben.** Datei 0x5E252–0x5E264 selbst gelesen: `si = 0; while (si + 0x7A < x) { si += 80; dx++ }`, ohne eigene Obergrenze; wegen x < 266 höchstens Spalte 2, also gleich der Remake-Schleife. 0x5E2C7–0x5E2DD: `col << 4 + row + [bp−0x18] < [bp−0x10] + [bp−0x12]`, sonst kein Marker. Mutationen N27 (alte Formel) und N28 (leere Zeilen) sind rot |
| R14HI-07 | **behoben.** Die PR-Beschreibung nennt alle Dateien außerhalb der Spur mit Grund, auch die neuen (`Playoffs.cs`, `FinanceStadium.cs`, `StadiumStatistics.cs`, `Squad.Tactics.cs`) |

R14HI-04 je Zeile:

- `NationalCoach._friendlies`: FILE19/FILE20 1 Spiel, Deutschland – Schweden 2:1; FILE18 0. Schreiber 0x3C248–0x3C288 gelesen: Heim `[bp−7]` → 0xD0DB, Gast `[bp−8]` → 0xD0D3, Ergebnis `>> 8` → 0xD0CB, `& 0xFF` → 0xD0C3.
- Zuschauerstatistik: FILE44 `+0xED` = 22, Summe 145.109, Rekord 7.500, Minimum 5.249 mit Gegnern; der Import stimmt mit dem Rohstand überein. FILE41/43 haben 14 Heimspiele.
- `Player.Notice`/`NoticeYears` aus `5DC3+0x18`: 0x4A3A6–0x4A3BE (`+0x18` > 0x63) und 0x27D15 (`+0x18` = 0x65 bei Vertrag 0) gelesen. Das Bit 0x80 wird erst bei 0x4A6C7 getestet, nicht bei 0x4A692 (siehe R14HN-03). Pool `+0x1E` → N mit Grund.
- `Player.TrendScore`/`Trend` aus `+0x0E`: 0x41AE0 `(+0x0E − 0x1E) / 13` gelesen. Die Startbelegung liegt in 0x4907A (`random(0x23, 0x41)`).
- `_vacantNationalTactics`, `_shownAuto*`: aus Vereinssatz des Landes `+0x26`/`+0x35` (Code gelesen, N21 rot).
- `NationalCoach.Manager`: 0x55482–0x554BF gelesen. Dort steht `[0x9657]+0x3D·[0x6AC]+0x16 = 6`, das Land in DGROUP 0x9EDA (= Satz 5 +0x14, Satzlänge 0x1AF), der Manager in 0x9FB1 (= Satz 5 +0xEB) und `D0BA[Manager] = 0`. Das Etat aus DGROUP 0x29C geht auf Satz 5 +0x18F, der Import bucht es nicht erneut (Kommentar und Code). N20 rot.
- `DebCup.Winner`/`EuropeanCup.Winner`: 0x34ED6 (`[bp−9]` → 0x387) und 0x314D6 (0xCDAC → 0x388) gelesen. N22/N23 (gerechneter statt gelesener Sieger) bleiben grün, weil an allen Ständen beide Werte gleich sind (FILE37A/38: 0x388 = 101). Das ist eine äquivalente Mutation am Datenbestand, kein Befund.

### R14HN-01 (P3) – Feldkartenprüfung lässt sich durch Kommentare erfüllen

[`FieldMap_ImportedRows_HaveTheirBlocksAndOffsetsInTheImportCode`](H/tests/EishockeyManager.Core.Tests/OriginalImportFieldMapTests.cs)
sucht die Versätze einer B/A-Zeile als Hexzahl irgendwo im Importcode, Kommentare eingeschlossen. Die Blöcke müssen dagegen als
`"…"`-Literal vorkommen; dieser Teil ist streng. Gegenprobe N30: Die Zeile `stadium.ImportConstruction(…)` wird entfernt, die Karte
führt `965B+0x117`/`+0x128` weiter als B. Die Prüfung bleibt grün, weil 0x117 und 0x128 nur im Kommentar darüber stehen; der Code
rechnet `s + 2 * p + 1` bzw. `s + 0x12 + p`. Die Gegenprobe der Aufgabe ist dagegen rot: eine B-Zeile ohne Importcode
(`FriendlyMatch.Bonus` → B `026C`, N29) und ein entfernter Blockzugriff (`D0C1`, N31). Den Befund R14HI-02 fangen die
Verhaltenstests (N10/N11 rot). Vorschlag: Kommentare vor dem Abgleich entfernen, oder die Versatzprüfung als Hinweis führen.

### R14HN-02 (P3) – Beschreibung des Siegbytes 0x14 zu eng

[`OriginalImport.Playoffs.cs`](H/src/EishockeyManager.Core/OriginalImport.Playoffs.cs#L16) und die Karte schreiben: „gewinnt der
Gast im fünften Spiel die Serie, steht dort 0x14“. Laut 0x3168B–0x316C9 wird 0x14 bei `CCA3` = 4 immer gesetzt, wenn der Gast dieses
Termins drei Siege hat. Danach wird getauscht. Das gilt auch, wenn die Serie vorher schon entschieden war: Bei FILE22 endet das
Endspiel 3:0 für Köln (`CCB2` Spiel 4/5 = 100) und `CCA4[12]` = 0x14. Dasselbe gilt für Viertelfinale 0/1. Der Code wertet das richtig
aus, weil er das Siegbyte prüft und nicht die Zahl der Spiele. Nur der Text ist ungenau.

### R14HN-03 (P3) – Zwei Importwege ohne Beleg im Datenbestand; kleine Fundstellenabweichung

- Laufende Play-off-Runde: Tausch bei ungerader `CCA3` und `limit = game`. Kein Stand liegt mitten in den Play-offs (nur FILE22/44,
  beide `CCA3` = 5). Die Mutation N08 (kein Tausch) bleibt grün. Der Weg ist nur durch Codelesen belegt: 0x31505 tauscht Plätze,
  Siege und die Spiele 0 … `CCA3`−1; 0x316C9 ruft das nach jedem Spiel 0–3 auf, auch bei nicht ausgetragenen Spielen. Daraus folgt
  die Parität `CCA3`. Die Logik ist richtig, ein Test fehlt.
- Teilweise gespielte Aufstiegsrunde: Termine = Punktsumme / 4. Unentschieden zählen 1:1 (FILE44 Gruppe 0, Termin 1: 2:2), die Summe
  bleibt also 4 je Termin. N06 (immer 6 Termine) bleibt grün, weil alle Stände fertig sind. `[APPROX]` ist genannt.
- `TryImport` fängt jede Ausnahme. N13 (alte Typliste) bleibt grün, weil nach der Behebung kein Test einen anderen Ausnahmetyp
  auslöst. Das Fuzzing fand ebenfalls keinen.
- Die Fundstelle „Bit 0x80 … (0x4A692)“ für `Player.Notice` steht tatsächlich bei 0x4A6C7 (`test [bp−0xA], 0x80`). 0x4A692 ist der
  Anfang des Zweigs und prüft `+0x0C`.

Vorschlag: einen synthetischen Stand mitten im Viertelfinale (aus FILE44 zurückgerechnet) und einen mit 3 von 6 Terminen der
Aufstiegsrunde als Test ergänzen. Das kann eine Folgeaufgabe sein.

### Erwartet offen – bestätigt

- **#669 (Schema 42/43):** nicht gemergt. Start-Liga, A046, A135, Leihe, Transferliste und Taktik 375h fehlen in der Karte; so steht es im
  Abschnitt „Stand Integration Runde 14“ und in der Übergabe. Head und `main` haben `SaveGame.SchemaVersion = 41`.
- **Halbfinale 1–8 gegen 3–6 (#676):** Tabelle DGROUP 0x144 selbst gelesen (Datei 0x16DD0 + 0x144 = `09 18 0B 18 0A 18 0C 18 0D 18 0E`).
  Mit dem Schreiber 0x31BFF–0x31C07 (`[0x5D3A + t]`) ergeben sich die Wege 0 → 8, 2 → 10, 4 → 9, 6 → 11, 8 → 12, 10 → 13 und
  Verlierer → 23, also 1–8 gegen 3–6. `[APPROX]` steht in `Playoffs.cs`. Der Import ordnet die Serien als `[0, 4, 2, 6]`, damit eine
  offene Runde des Remakes wie im Original paart (N03 rot).
- **`[APPROX]` laut Übergabe:** Marken +30 (abgezogen, 0x31622–0x31631 bestätigt: nur das Heimbyte, Schleife `−30` solange > 0x1D),
  Termine = Punktsumme/4, Tendenz 0 lässt die Stufe stehen, Bonus der Freundschaftsspiele 0, Vorjahres-Titelträger nicht abbildbar
  (`History` leer). Alle fünf sind im Code und in der Karte markiert und akzeptiert.

## 3. Ausgeführte Prüfungen

### `tools/pruefen.sh` (alle drei Läufe, Head `f849736`, mit Originaldaten, SDK 10.0.402)

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2465 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1962 | 0 | 414 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1963 | 502 | 0 |

Ergebnis „bestanden“. Die Zahlen stimmen mit der Übergabe überein. `Import_AllStands_FourSeasonChanges` (alle 52 Stände, 4
Saisonwechsel bzw. Spielende) ist im ersten Lauf enthalten und grün.

### Merge von `main`

`git diff 38ebbad 153c031` ist ohne `index`-Zeilen identisch mit `git diff $(merge-base) fe09c20`: nur
`docs/reviews/2026-11-09-integration-hok-import-r14.md`. `fe09c20` liegt in `origin/main`.

### Dateien außerhalb der Spur, Wirkung auf das normale Spiel

- `Playoffs.cs`: nur `partial`, privater Setter für `Rounds` und Kommentar.
- `Squad.Tactics.cs`: nur `partial`.
- `FinanceStadium.cs` `ImportConstruction` und `StadiumStatistics.cs` `Import`: neue `internal`-Methoden, die nur der Import aufruft.

Keine Änderung an Regeln oder Abläufen. Kein Testfile außerhalb des Imports im Delta, also kein geänderter Fingerabdruck oder
Sollwert anderer Spuren. Die volle Suite ist grün. §4: keine `Book`-Aufrufe im Import, Ausbau und Bundestrainer-Etat werden nicht
erneut gebucht (Konto FILE43 siehe R14HI-02).

### Fundstellen in em.exe (`tools/reverse-engineering/disasm.py`) und Hexdumps

- Gelesen: 0x319AC–0x319D4 (Setzung 0x5D3B/0x5D3C), 0x31505 (Tausch), 0x315E9–0x31830 (Wertung, Marken, Gast bei Gleichstand
  `jnc` → 0xCCA5, 0x14 bei 0x316BF, Speichern nach 0xCCB2 bei 0x3178C/0x317AE, 100 bei 0x31800), 0x31A80–0x31CD0 (Rundenwechsel,
  Tabelle 0x144, `inc [0x7B0]`, `CFFA`, Wiederherstellung 0x31C3A, Meister 0x31CBF), 0x38691 (Aufruf 0x30852 mit Wettbewerb 1),
  0x30511 (`5CD5`/`5CD6` + 12 · Liga → `CFF4` + 8 · Wettbewerb + 4 · Gruppe), 0x30B0F–0x30BE1 (`CCF8`/`CD28`/`CD58` + 12 · Wettbewerb
  + 6 · Gruppe + Platz).
- Hexdumps (aus `P1`):
  - FILE44: `07A9` = `2D2D2D0005000C02` (Runde 2), `CCA3` = 5, `CCA4` = `0214030203000301001403010302`, Plätze `0106070B090A0508060907050907…`,
    `CD58+12` = `03010A0A0000 0401090A0000`.
  - FILE22: `CCA4` = `0014030003000300030100141400`, Plätze `0A0701060200040B070201040407…`, `CCA3` = 5.
  - Nachgerechnet: Endspiel FILE44 (`CCB2` Plätze 12/13 je Spiel 03 02 / 02 03 / 03 02 / 02 03 / 03 02 → Kaufbeuren 3:2 Siege).
    FILE22 Endspiel 0x14 → getauscht, Köln 3:0.

### Mutationen (Worktree, `dotnet test -c Release`, Originaldaten)

| Nr. | Mutation | rot |
|---|---|---|
| N01 | `ApplyPostseason` aus | ja (6) |
| N02 | Siegbyte 0x14 ignoriert | ja (1) |
| N03 | Serienordnung `[0, 2, 4, 6]` | ja (2) |
| N04 | Tore ohne modulo 30 | ja (2) |
| N05 | Gruppen der Aufstiegsrunde vertauscht | ja (7) |
| N06 | immer 6 Termine | **nein** (R14HN-03) |
| N07 | Saisonrückblick nicht neu eingetragen | ja (2) |
| N08 | laufende Runde ohne Tausch | **nein** (R14HN-03) |
| N10 | Ausbau aus | ja (2) |
| N11 | Restwochen +0x13 statt +0x12 | ja (2) |
| N12 | Sponsor-Schlüssel wie vorher | ja (1) |
| N13 | `TryImport` mit alter Typliste | **nein** (R14HN-03) |
| N14 | Freundschaftsspiele 0 | ja (2) |
| N15 | Heim-/Gasttore vertauscht | ja (2) |
| N16 | Zuschauerstatistik aus | ja (3) |
| N17 | Tendenzwert 0 | ja (1) |
| N32 | Stufe aus dem Tendenzwert nicht gesetzt | ja (1) |
| N18 | Ruhestandsbit 0x40 statt 0x80 | ja (1) |
| N19 | Angebot −99 statt −100 | ja (1) |
| N20 | Amt bei `+0x16` = 7 | ja (9) |
| N21 | `ImportAutoLine` aus | ja (2) |
| N22 | DEB-Pokalsieger gerechnet statt gelesen | nein (äquivalent am Datenbestand) |
| N23 | Europacupsieger gerechnet statt gelesen | nein (äquivalent am Datenbestand) |
| N24 | M03: Land und Managerverein vertauscht | ja (8) |
| N25 | M09: `0375[2m+1]` | ja (2) |
| N26 | M45: `_selected = index` entfernt | ja (1) |
| N27 | Marker-Spalte alte Formel | ja (2) |
| N28 | Marker auch bei leeren Zeilen | ja (1) |
| N29 | Karte: `FriendlyMatch.Bonus` B `026C` | ja (1) |
| N30 | Ausbau-Code entfernt, Karte unverändert (Versatz nur im Kommentar) | **nein** im `FieldMap`-Test (R14HN-01; R14HI-02-Tests rot) |
| N31 | Blockzugriff `D0C1` entfernt | ja (1) |

### Fuzzing mit gültigen Prüfsummen (`OriginalSavePatcher.Encode`)

- 3.754 Läufe insgesamt.
- Einzelbyte-Änderungen (0, 1, 0x14, 30, 100, 0xFF) in den neuen Bereichen:
  - FILE44: `5CFB+0x40…0x57`, `07A9`, `CCA3`, `CCA4`, `CCB2`, `CFFC`, `CED8`/`CDB8+0x48…0x8F`, `CD58`, `CD88+12`, `D0E6`, `0387`.
  - FILE22: dieselben Bereiche, teils jedes 2./3. Byte.
  - FILE19: `D0C1`, `D0DB`, `D0D3`, `D0CB`, `D0C3`, `06AC`.
  - FILE43: `965B+0xED…0x12F`, `+0x183…0x18E`.
  - FILE37A: `0388`. FILE05: `C15B`, `0387`.
- Dazu 400 zufällige Mehrbyte-Änderungen in allen Blöcken (FILE44, 22, 43, 19, 05).
- Ergebnis: 2.893 laden, 861 sind Ladefehler mit Meldung. Keine Ausnahme nach außen, kein Ergebnis mit `Success` ≠ (`Game` ≠ null).
- 297 geladene Stände liefen 15 `Continue` (bei Saisonende `StartNextSeason`) ohne Ausnahme.
- Ein halb geladener Zustand ist ausgeschlossen: Der Import baut einen neuen `GameState`, und der Dialog setzt `Host.Game` nur bei
  Erfolg (erster Review, Code unverändert).

### Akzeptierte `[APPROX]`

Die fünf `[APPROX]` aus der Übergabe (Abschnitt 2). Dazu „Bundestrainer am Zug → Manager 0“ (unverändert) und die
Halbfinal-Paarung des Remakes für eigene Spiele (#676).

### Offene Prüfbereiche

Keine für dieses Delta. Die Wege in R14HN-03 sind nur durch Codelesen und em.exe belegt, weil kein Stand sie abdeckt.
