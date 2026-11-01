# Nachprüfung 2: Integration Runde 8, Welle 3 (#268)

- PR: #268 · Base `99fa995b941d9e85234cebb0ca4c7dc00507a100` · Head `19d822ad4f4d8003323539b6e54ffac47aacccc7`
- Vorherige Nachprüfung: `docs/reviews/2026-11-01-integration-runde-8-welle-3-nachpruefung.md` (PR #273), Head `d61ef21f`.
  Geprüft wurde nur das Delta `d61ef21..19d822a` (ein Commit, 5 Dateien) sowie R8W3I-12 bis -15.
- Reviewer: Claude, frische Sitzung ohne Implementierungskontext, geplanter Lauf, unbeaufsichtigt, 01.11.2026.
- Checkout: Klon mit `git rev-parse HEAD` = Head-SHA und leerem `git status --porcelain`. Originaldaten aus
  `kaaasi/eishockey-manager-referenz` (`tools/claude/fetch-reference.sh original`), SDK 10.0.402 aus dem Release
  `claude-werkzeuge`. Proben und Mutationen in einem eigenen Worktree, nicht im Repo.
- **Annahmen** (unbeaufsichtigter Lauf): „Bei gleicher Laufzeit nicht schon ab dem alten Gehalt annehmen“ wird mit 3 Jahren
  (= bisherige Laufzeit) und 2.300 DM (= bisheriges Gehalt) nach dem Saisonende geprüft; erwartet ist kein `Accepted`.

## 1. Merge-Empfehlung

**Ja für genau diesen Head.** R8W3I-12 ist behoben, R8W3I-13 und -14 sind umgesetzt, das Delta enthält nur Fix, Kommentare,
Test und Plan. Alle drei Läufe von `tools/pruefen.sh` sind am exakten Head grün. Offen bleibt nur R8W3I-15 (Runde 9) und ein
P3-Hinweis zur Testabdeckung.

- **R8W3I-12 (#272) behoben.** `Negotiate` übernimmt nach dem Saisonende Laufzeit und Forderung über `ComputedDemand` ohne
  Levelabfrage; nur `DemandAfterYears` (Füllen des Feldes) bleibt ab Level 5 `null`. Das entspricht dem Original: in
  0x4A81B–0x4A9F2 ist `0x4A9DB cmp byte [0xed8],0` die einzige Abfrage von 0xED8, das Rückschreiben (+0x2A/+0x28 bei
  0x4A9A4/0x4A9A8, +0x0B bei 0x4A9AF) liegt davor (eigenes `ndisasm -b16`).
- **R8W3I-13 umgesetzt.** `[ORIGINAL] 0x4AD3E→0x4AD54` mit Sicherung 0x4A239–0x4A25F in `Player.Contract.cs`, „Ablehnung
  und Abbruch“ in `Transfer.Contract.cs`. Disassembly bestätigt: Sperre 0x4AD3E–0x4AD50, danach ab 0x4AD54 Rückstellen von
  +0x30/+0x32, +0x0B, +0x28/+0x2A.
- **R8W3I-14 umgesetzt.** Der Kommentar nennt jetzt die Streuung über den gemeinsamen Zufallsstrom (5,97–6,69, Seeds 1–12)
  statt einer Folge des Spielstands; die Grenzen sind unverändert.
- **Delta-Umfang:** `Player.Contract.cs`, `Transfer.Contract.cs` (nur Kommentar), `IntegrationRunde8Welle3Tests.cs` (neuer
  Test), `SpielberechnungAnschlussTests.cs` (nur Kommentar), `docs/arbeitspakete.md` (Nachprüfungszeile). Nichts sonst.

## 2. Befunde

Links auf den Head: `https://github.com/kaaasi/eishockey-manager-redone/blob/19d822ad4f4d8003323539b6e54ffac47aacccc7/…`

Keine neuen Befunde P0–P2.

### P3

- **R8W3I-16: Der neue Test deckt nur die erste Hälfte des Gegenbeispiels ab.**
  - Stelle: [`IntegrationRunde8Welle3Tests.cs` Z. 138–155](https://github.com/kaaasi/eishockey-manager-redone/blob/19d822ad4f4d8003323539b6e54ffac47aacccc7/tests/EishockeyManager.Core.Tests/IntegrationRunde8Welle3Tests.cs#L138-L155).
  - `Negotiate_AfterSeason_WritesBackOnEveryLevel` prüft die Annahme 2 Jahre/23.000 DM. Die Umkehrung (Level 5, gleiche
    Laufzeit, altes Gehalt) ist nicht als Test festgehalten. Der Code ist richtig (Probe in 3.2), ein Rückfall würde aber
    nur über die erste Hälfte auffallen. Empfehlung: eine Zeile `Assert.NotEqual(Accepted, Negotiate(…, 3, 2_300, …))` in
    Runde 9 ergänzen. Kein Merge-Hindernis.
- **Hinweis ohne ID:** Der neue Kommentar in `Negotiate` (Z. 297) sagt „das Remake ändert den Spieler dann gar nicht“; die
  Sperre (`NegotiationLock`) wird wie im Original gesetzt. Gemeint sind Laufzeit und Gehalt – sprachlich ungenau, sachlich
  richtig.

## 3. Ausgeführte Prüfungen

### 3.1 `tools/pruefen.sh` (alle drei Läufe, exakter Head, Release, Warnungen = Fehler)

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1408 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1177 | 0 | 207 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1177 | 231 | 0 |

Commit `19d822ad4f4d8003323539b6e54ffac47aacccc7`, SDK 10.0.402, Ergebnis „bestanden“. Gegenüber `d61ef21` +2 Tests (die
beiden Fälle der neuen Theorie).

### 3.2 Gegenbeispiel R8W3I-12 erneut ausgeführt

Probe: synthetische Datenbank, Seed 3, Verein 2, `LifecycleRegressionTests.FinishSeason`, erster verfügbarer Stürmer mit
3 Jahren und 2.300 DM, `demandFilled: false`.

| Level | Feld (2 J.) | 2 J./23.000 DM | 3 J./2.300 DM (Head) | 3 J./2.300 DM mit `Player.Contract.cs` von `d61ef21` |
|---|---:|---|---|---|
| 0 | 17.149 | Accepted → 2/23.000 | NotInterested, Sperre 18 | NotInterested |
| 3 | 18.066 | Accepted → 2/23.000 | NotInterested, Sperre 18 | NotInterested |
| 4 | 20.291 | Accepted → 2/23.000 | NotInterested, Sperre 18 | NotInterested |
| 5 | – (`null`) | Accepted → 2/23.000 | NotInterested, Sperre 18 | **Accepted** (alter Fehler) |

Am Head zusätzlich Level 5 mit 2 J./23.000 DM vor dem Fix: `TooShort` (wie in der Nachprüfung). Bei 3 J./2.300 DM wird
auf keinem Level mehr ab dem alten Gehalt angenommen; der Vergleich läuft gegen die Forderung (Level 0–4: 19.256–22.784 DM).

### 3.3 Gegenprobe des neuen Tests

| Mutation | rot |
|---|---|
| `Player.Contract.cs` auf Stand `d61ef21` zurück | `Negotiate_AfterSeason_WritesBackOnEveryLevel(level: 5)` (Level 3 bleibt grün, wie erwartet) |

Übrige Tests in `IntegrationRunde8Welle3Tests` und `TransferOriginalVertrag*` blieben bei der Mutation grün; `DemandAfterYears`
behält sein Verhalten (UI-Aufrufer `ContractsScreen` Z. 182 unverändert).

### 3.4 Disassembly (`ndisasm -b16`, Dateiadressen)

- **0x4A81B–0x4A9F2:** Schreiben +0x2A/+0x28/+0x0B bei 0x4A9A4–0x4A9AF, einzige Abfrage von 0xED8 bei 0x4A9DB.
- **0x4AD3E–0x4AD7A:** `random(10, 18)` → +0x18 (0x4AD50), dann Rückstellen aus `[bp-0x32..-0x3c]` ab 0x4AD54.

### 3.5 Erwartet offen – bestätigt

- **R8W3I-15:** unverändert offen, als Verdacht für Runde 9 (spielstaerke-manager Teil 3) im Plan geführt. Das Delta berührt
  die Spielberechnung nicht.

### 3.6 Akzeptierte `[APPROX]`

- Keine neuen. Das frühere `[APPROX]` ist durch `[ORIGINAL] 0x4AD3E→0x4AD54` ersetzt (R8W3I-13).
