# Review PR #947 – Hängeschutz für Testläufe (Issue #935)

Reviewer: Claude Opus 5.5 (geplanter Lauf, unabhängige Sitzung) · Base `4f2ec64` · Head `e2750aa` (Checkout verifiziert,
`git rev-parse HEAD` = `e2750aac28dd47f2b25a666fe8896d87f3dcd59f`, `git status --porcelain` leer) · Erstreview, alle drei Läufe.

## 1. Merge-Empfehlung

**Ja** für Head `e2750aa`. Der Hängeschutz greift nachweislich (Hänger nach 30 s abgebrochen, Testname genannt, Exit 1), die
Umstellung auf die Ergebnisblöcke des Konsolenloggers liefert auf Head und `main` dieselbe Tabelle, der Pflichtlauf ohne Daten
scheitert weiter wie gewollt, `build.yml` läuft mit den neuen Optionen grün. Nur P3-Befunde.

## 2. Befunde

**P3-1 – Selbsttest prüft die Summen der Tabelle nicht.**
[`tools/pruefen-selbsttest.sh`](https://github.com/kaaasi/eishockey-manager-redone/blob/e2750aac28dd47f2b25a666fe8896d87f3dcd59f/tools/pruefen-selbsttest.sh#L20-L25)
liefert die Blöcke, aber kein Fall vergleicht die ausgegebenen Zahlen; das Double kennt nur `Skipped: 0`. Beleg (temporäre Kopie):
`sed -i 's/{s+=\$2}/{s+=0}/' tools/pruefen.sh` bzw. `sed -i 's/{p+=\$2}/{p+=2*$2}/' tools/pruefen.sh` →
`tools/pruefen-selbsttest.sh` bleibt grün (rc 0). Die echte Auswertung stimmt (Teil 3); nur die Regression wäre unbemerkt.
Vorschlag: im Fall `korrekt` bzw. `gruen` die Tabellenzeile gegen erwartete Werte prüfen, ein Projekt mit `Skipped` ausgeben.

**P3-2 – Tabellenzeile bei Hänger ohne Kennzeichnung.** Beim Hänger-Lauf steht
`| Mit Originaldaten (EM_REQUIRE_GAMEDATA=1) | 3603 | 0 | 0 |` ohne Fehlervermerk; nur Kopfmeldung und „Ergebnis: NICHT bestanden“
zeigen das Scheitern. Gleiches Verhalten schon auf `main` bei Absturz (kein Rückschritt). Wer nur die Tabelle in den PR kopiert,
übersieht es. Vorschlag: bei `ergebnisse_da`-Fehler „– **FEHLER: unvollständig**“ an die Zeile hängen (wie beim Pflichtlauf).

**P3-3 – Kleinigkeiten (kein eigener Lauf nötig).**
- `pruefen-selbsttest.sh`: `[ -z "$lim" ] && unset EM_HANG_TIMEOUT` ist wirkungslos (Variable wurde nur für den Unterbefehl
  gesetzt); harmlos, da `${EM_HANG_TIMEOUT:-20m}` leer wie fehlend behandelt.
- Mit `--blame-hang-dump-type none` meldet vstest trotzdem „Collecting hang dumps …“; es entsteht kein Dump (nur
  `Sequence_*.xml` unter `tests/*/TestResults/`, durch `.gitignore` abgedeckt). Kein Handlungsbedarf, ggf. Satz in `umgebung.md`.

Regeltexte: `AGENTS.md` §5 (Hängeschutz, „Original zieht endlos“) und `zusammenarbeit.md` §5 Nr. 1 sind knapp, verweisen
aufeinander und widersprechen weder §5 „Sparsame Kette“ noch der Review-Vorlage. „ohne Fortschritt nach 20 Minuten“ entspricht
der vstest-Semantik (ein einzelner Test läuft länger als das Limit; Meldung „inactivity time … elapsed“). Längster Einzeltest im
Lauf mit Daten: 43 s → 20 min Limit mit großer Reserve.

## 3. Ausgeführte Prüfungen

SDK 10.0.402 (`tools/claude/setup-workspace.sh`), Originaldaten aus dem Referenz-Repo (`ls-remote` erfolgreich).

`tools/pruefen.sh` (alle drei Läufe) auf Head `e2750aa` und zum Vergleich auf `main` `4f2ec64` (eigener Worktree) – **identisch**:

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3774 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2664 | 0 | 609 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2665 | 1109 | 0 |

Ergebnis beider: bestanden. Gegenprobe der Auswertung auf Head: Einzelzeilen `  Passed …`/`  Failed …`/`  Skipped …` im Protokoll
gezählt (`grep -cE '^ +Passed [^:]'` usw.) = 3774/0/0, 2664/0/609, 2665/1109/0; je Lauf 4 Ergebnisblöcke für 4 Testprojekte.

Gegenbeispiele (temporäre Kopie, Worktree):
- **Hänger:** `tests/EishockeyManager.Formats.Tests/HangProbeTests.cs` mit `Thread.Sleep(Timeout.Infinite)`,
  `EM_HANG_TIMEOUT=30s tools/pruefen.sh --schnell` → „inactivity time of 30 seconds has elapsed“, „Test Run Aborted.“,
  „The test running when the crash occurred: EishockeyManager.Formats.Tests.HangProbeTests.HangsForever“, „FEHLER: … Ergebnisse
  für 3 von 4 Testprojekten (Absturz, Hänger oder Ladefehler)“, Exit 1. Übrige Projekte liefen zu Ende.
- **Ohne Hänger:** unveränderte Tabelle (siehe oben, Vergleich mit `main`).
- **`tools/pruefen-selbsttest.sh`** auf Head: alle 8 Fälle ok, rc 0. Rot bei zurückgenommener Änderung:
  `pruefen.sh` von `main` → rc 1 (Pflichtlauf `korrekt` abgelehnt, Hängeschutz 0 von 3); ohne `--blame-hang-dump-type none` →
  rc 1; `TESTOPT` im ersten Testaufruf entfernt → rc 1 (2 von 3); `ergebnisse_da` zählt nur `Successful` → rc 1. Summen-Mutationen
  siehe P3-1 (grün). Selbsttest von `main` gegen neues `pruefen.sh` → `korrekt` abgelehnt (Double-Format veraltet, erwartet).

CI / `build.yml`: Lauf `workflow_dispatch` auf Head
[45055072772](https://github.com/kaaasi/eishockey-manager-redone/actions/runs/45055072772) **success** (Jobs build und publish);
Log zeigt `--blame-hang-timeout 20m --blame-hang-dump-type none` in beiden Testschritten, synthetisch 2664 bestanden, Originaldaten
609 übersprungen (passt zur Tabelle). Der `pull_request`-Lauf 45054448134 ist abgebrochen – **erwartet offen, bestätigt**.

Erwartet offen bestätigt: Grundlage ist Issue #935, kein Abschnitt in `docs/arbeitspakete.md`.

Akzeptierte `[APPROX]`: keine (reine Werkzeugänderung). Annahmen: keine über die Belege hinaus.
