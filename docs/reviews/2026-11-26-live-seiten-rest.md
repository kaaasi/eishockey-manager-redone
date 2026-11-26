# Review PR #1042 – Spur live-seiten-rest (Erstreview)

Geprüft: Head `3556c26` (Base `ad7a7d9` = `main`). Checkout nachgewiesen: `git rev-parse HEAD` =
`3556c2684025fac27bc4f9437016491f1659b315`, `git status --porcelain` leer. Reviewer: Claude Code (Opus 5.5), geplanter Lauf,
frische Sitzung ohne Implementierungskontext.

## 1. Merge-Empfehlung

**Ja** für genau `3556c26`. Alle drei Läufe von `tools/pruefen.sh` sind grün, CI `build` ist grün auf dem Head. Die Nachbildung
`aufstieg_europapokal.py` läuft über die Bytes von em.exe und deckt sich mit der eigenen Disassembly. Der Code folgt ihr:
Terminzahl, Regel Ergebnis/Termin, Tabelle ab Minute 60, zweite Aufstiegsgruppe und Endrunde ab Termin 7. PW-04 ist mit
einem weiteren Videobild belegt, das der PR nicht nennt (unten). 10 von 11 Mutationen machen mindestens einen Test rot. Es gibt
keinen offenen P0–P2.

## 2. Befunde

**P3-1 (Testabdeckung): Der Meldungsfilter der zweiten Aufstiegsgruppe ist ungeprüft.**
[`LivePageScreens.cs` Z. 50](https://github.com/kaaasi/eishockey-manager-redone/blob/3556c2684025fac27bc4f9437016491f1659b315/src/EishockeyManager.UI/Screens/Live/LivePageScreens.cs#L50)
`game.Options.Reports[6 + (minute == 60 ? 1 : 0)]` (Index 6/7, Datei 0x3A8C5–0x3A8F5).
Beleg: Bedingung entfernt (`… && game.Options.Reports[6 + …])` → `)`), dann
`dotnet test tests/EishockeyManager.UI.Tests --filter "FullyQualifiedName~LiveSeitenRestTests|FullyQualifiedName~EuropeanCup_MainMenuDatesForOpenGames_NoneInLivePage"`
(mit `EM_REQUIRE_GAMEDATA=1`). Ergebnis: 12/12 grün.
Ursache: `PromotionDay_BothGroups_RunningScore_NoDates` setzt Index 6 und 7 auf `true` und prüft keinen Fall mit „Oberliga“ aus.
Vorschlag: im selben Test `Reports[6] = false` → nach Minute 20 nur die Seite der Gruppe 0.

Keine weiteren Befunde.

## 3. Ausgeführte Prüfungen

`tools/pruefen.sh` (alle drei Läufe) auf `3556c26`, SDK 10.0.402:

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 4055 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2876 | 0 | 663 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2877 | 1178 | 0 |

CI auf `3556c26`: `build` success (`publish` skipped).

**Gegenbeispiel Rest S04** (`python3 -I tools/reverse-engineering/live/aufstieg_europapokal.py gamedata/em.exe`):
- Teil 1 (Argumente 0x31CD5 aus dem Seitenwähler): bei Europapokal-Gruppe 1 mit 0x7AF = 2 ergeben Minute 0/20/40/60/65/70 die
  Werte Zahl/Pause (2,0) (1,1) (1,1) (2,0) (1,0) (1,0).
  Aufstiegsrunde Gruppe 0/1: Merker „im Spiel“ = 1 bei Minute ≠ 0, Maske 0x40. Endrunde mit 0x7AF = 8: Zahl 2 bei Minute 0.
- Eigene Disassembly (`disasm.py` 0x3AA9A–0x3ABD8) bestätigt die Basen: Aufstieg min(2 · 0x7B0 + (0xCCA3 > 0) + (0xCCA3 > 2), 7),
  Gruppen min(0x7AF, 6), Endrunde `0x7AF > 5 ? 0x7AF − 6 : 0x7AF` (0x3AB8E–0x3AB93).
  Damit ist die Lesart „6 bzw. − 6“ aus #1039 P3-5 so aufgelöst, wie der PR und `ShownGames`/`PlayedDates` sie nachbilden.
  `inc byte [0x7af]` steht nach dem Spieltagsaufruf (Datei 0x314FC); die Basis zählt also die Termine vor dem laufenden.
  `RelegationGroup.DateIndices` [0, 2, 5, 7, 10, 12] ergibt genau 2 · 0x7B0 + (0xCCA3 > 0) + (0xCCA3 > 2).
- Teil 2 (0x31CD5 aus den Bytes): Ergebnis für g < c bzw. g = c im Spiel; nach Minute 65 entfällt der laufende Termin; Termin nur
  ohne „im Spiel“. Das entspricht `ShowsResult`/`ShowsDate`.
- Datei 0x38642–0x386C5: Die zweite Gruppe 0x3A88A(2, 2, m) läuft nur bei 0xCCA3 ∈ {0, 2}, wie in `LivePageScreens.For`.
  Die Tabelle 0x30852 kommt nur bei Minute 60 (0x38652/0x38691; Europapokal 0x3A95D–0x3A982).
  0x3183C (Datei 0x3183C–0x31933) schreibt die Live-Tore beider Gruppen (Schleife über `cl`, Endrunde eine).
- Teil 3 (Hauptmenü 0x3A88A(3, 0, 0)): 0x7AF = 3 → drei Termine der Rückspiele.

**Gegenbeispiel PW-04** (Videobilder angesehen):
- `eisbaeren_f51_03300` zeigt das Live-Spiel Ambri – Eisbären (Runde 2). Danach folgt `03311`/`03314` mit der Tabelle einschließlich
  dieses Spiels (Eisbären 4:0) und ohne Ergebnis der Runde 3. Das passt zur Minute 60 des zweiten Termins (c = 1).
- `03345`/`03346`: Runde 3 mit 3:3/2:2, Tabelle mit Termin 3 → Minute 60 des dritten Termins.
- `eisbaeren_f20_02930` (Live) → `02950`/`02951`: dasselbe Muster.
- Zusätzlicher Beleg für Termine im Hauptmenü, im PR nicht genannt: `folge20_02316` (Gruppe 1, fünf Spiele gespielt) zeigt
  „16.11.“ beim offenen Rückspiel der Runde 3 in der ersten Zeile.
- Fremder Test nach R15-E2 = C: Die Begründung trägt (Nachbildung Teil 3, Bilder oben). Rückschaltprobe: alte Regel
  (`|| group.Results.Count > 0` an `ShowsDate`) → `EuropeanCup_MainMenuDatesForOpenGames_NoneInLivePage` rot.

**Mutationen** (temporäre Kopie, Filter wie in P3-1):

| Mutation | Ergebnis |
|---|---|
| M1 `ShownGames` mit +1 auch bei 65/70 | rot (`EuropeanCupGroup_Live_…`, `ShownGames_…`) |
| M2 zweite Aufstiegsgruppe nicht anfügen | rot (`PromotionDay_…`) |
| M3 EC-Tabelle mit Termin schon ab 20 | rot (`EuropeanCupDay_…`, `EuropeanCupGroup_Live_…`) |
| M4 Aufstieg ohne laufenden Stand | rot (`PromotionDay_…`) |
| M5 PW-04 alte Regel | rot (`EuropeanCup_MainMenuDates…`) |
| M6 Endrunde erst ab Termin 8 | rot (`EuropeanCupView_…`) |
| M7 Termine auch „im Spiel“ | rot (5 Tests) |
| M8 Aufstiegstabelle mit Termin ab 20 | rot (`PromotionDay_…`) |
| M9 Filter Index 6/7 entfernt | **grün** → P3-1 |
| M10/M11 `Create` ohne Fall Aufstieg/EC (Zustand vorher) | rot (`PromotionDay_…` bzw. `EuropeanCupDay_…`) |

**Bildschirmfoto** (`dotnet run -c Release --project src/EishockeyManager.Tools -- screens gamedata <ziel> live-aufstieg-europapokal`)
gegen das Videobild. Verschiebung (−0,25 | +0,5), Gauß 0,5, Graustufen; der Fensterwert enthält den Mauszeiger.

| Bild | Fenster mittel / >60 | Paarungen | Tabelle |
|---|---|---|---|
| `eisbaeren_f51_03346` | 7,9 / 91 | 7,6 / 10 | 9,0 / 0 |
| `eisbaeren_f58_01323` | 8,6 / 57 | 8,3 / 3 | 9,8 / 0 |

Sichtvergleich: Ergebnisse, Rückspiel „2: 0 / 0: 5“ der Endrunde, keine Termine, Tabelle und Knöpfe stimmen.
`aufstieg_live_1/2` folgen dem Aufbau von `folge06_01257`, ohne Termine. Gruppe 1 ist leer (erwartetes `[APPROX]`).

**Hängeschutz:** Die Schleifen der neuen Tests haben Abbruchzähler (`PassTo` 1000, `StepTo` 100, `LiveDay` 20000, Seitenschleife 200000).

**Akzeptierte `[APPROX]`/Restlücken** (erwartet offen, bestätigt):
- Zwei Gruppen eines Termins werden je Gruppe nacheinander gerechnet (Teil 2b aus #1032; `CupsGroup.cs` Z. 126,
  `Playoffs.cs` Z. 301). Im Original füllt 0x3183C beide Gruppen mit Live-Toren (siehe oben).
- Knöpfe „Liga“ im Spielablauf ohne Minute (`PromotionRoundScreen.cs` Z. 98; Original Seite 7 mit Minute, Datei 0x3ACBB–0x3ACC1).
- Restlücke `pixel-europapokal` (fremd) zeichnet f51/f58 weiter als Hauptmenü. Kein Test betroffen (alle Läufe grün).

**Nicht bewertet:** Konflikt von `LivePageScreens.cs` mit #1033 (laut Auftrag integriert die Leitstelle).

**Annahmen:** Die Live-Tore von 0x3183C (DGROUP 0x5CAB) entsprechen dem laufenden Stand von `LivePageScreens.Score`. Das ist
nur gelesen, nicht nachgebildet.
