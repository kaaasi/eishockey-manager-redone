# Nachprüfung Review PR #480 (Spur `re-pruefung`)

Base `af907c5af899c58424de2d843b60952a8c76b460` · Head `534e06c4fcd11133c396c33688d9bb751ba62fc2` · bisher geprüft `1bd70a8`
(Review `docs/reviews/2026-11-03-re-pruefung.md`, PR #494) · Reviewer: Claude Opus 5.5, geplanter Lauf, neue Sitzung ohne
Implementierungsbeteiligung. Delta `1bd70a8..534e06c`: ein Commit, nur `docs/analyse/re-runde-12.md` und der eigene Statuspunkt
in `docs/arbeitspakete.md`.

## 1. Merge-Empfehlung

**Ja für `534e06c`.** R12RP-01 bis R12RP-03 sind behoben und am Original bzw. mit der Funktionskarte nachgeprüft, R12RP-05 und
R12RP-06 ebenfalls. R12RP-04 ist im Bericht begründet und als Leitstellen-Erweiterung gekennzeichnet; offen bleibt nur die
Übernahme in `docs/arbeitspakete.md` (Teil 4 `kader-transfer` lautet dort weiter „Text angleichen“). Das liegt außerhalb der
Dateigrenzen der Spur und blockiert den Merge nicht.

## 2. Befunde

| ID | Stand | Nachweis |
|---|---|---|
| R12RP-01 (#496) | **behoben** | [Z. 29](https://github.com/kaaasi/eishockey-manager-redone/blob/534e06c4fcd11133c396c33688d9bb751ba62fc2/docs/analyse/re-runde-12.md#L29): „DGROUP 0x40AE/0x409E“. Gegenbeispiel `funktionskarte.py … --csv` ohne `--write` erneut: 0x03FB7 jetzt **unbekannt** (bei `1bd70a8` dokumentiert, Base unbekannt); Gesamt dokumentiert 171, keine weitere Statusänderung gegenüber Base. |
| R12RP-02 (#497) | **behoben** | [Z. 60–79](https://github.com/kaaasi/eishockey-manager-redone/blob/534e06c4fcd11133c396c33688d9bb751ba62fc2/docs/analyse/re-runde-12.md#L60-L79): 0x44E3F/0x44E46 (0xE1A4 = random(0x55, 0x73), 0x0C037), 0x44EB1 (0xA70D = [0xA70E]), 0x44EB4 (0xE1A6 = 0xFF), 0x450C3 und 0x45957 (0xA70E = [0xA70D]) ergänzt; Umfang „bis zur Eingabeschleife und beim Rücksprung, Klickzweige nicht aufgeführt“ ist jetzt genannt. Gegenprobe `disasm.py 0x44E22 0x45AA7`, alle Schreiber mit DGROUP-Ziel: vor 0x45297 und im Rücksprung keine fehlende Adresse; 0x453AE ff., 0x4575E/0x45761, 0x45865 ff. liegen in Klickzweigen. `--xref 0xE1A4 0xE1A5`: genau 0x44E46 (Schreiber) und 0x4A832 (Leser) – stimmt. Ausnahme für 0xE1A4 in E4 ausdrücklich formuliert; `ContractTurnFactor` (`Transfer.Contract.cs:127`) ist tatsächlich rein abgeleitet. |
| R12RP-03 (#498) | **behoben** | [Z. 190](https://github.com/kaaasi/eishockey-manager-redone/blob/534e06c4fcd11133c396c33688d9bb751ba62fc2/docs/analyse/re-runde-12.md#L190): `Core/Squad.Lineup.cs` nur noch „Anschluss melden, keine eigene Änderung“ mit Verweis auf `wettbewerbe-spiel`. |
| R12RP-04 (#499) | **begründet offen** | Z. 190 nennt den erweiterten Umfang und „Erweiterung von Teil 4 durch die Leitstelle“. `docs/arbeitspakete.md:90` lautet noch „Text angleichen“ – die Leitstelle muss Teil 4 vor dessen Start erweitern (Spur darf die Datei nur im eigenen Statuspunkt ändern). |
| R12RP-05 (#500) | **behoben** | 0x45996/0x4599C nur beim Verlassen ([bp−7] ≠ 0), Rückkehr über 0x45961/0x4596C (Datei 0x4595A–0x4596F bestätigt); 0x4527C mit `cmp al,[0x386]`/`jnc` bei 0x45273/0x45277 bestätigt; E4 nennt `FinanceInbox.Post` (privat, alle Arten; `Notify` ruft `Post`, `FinanceInbox.cs:55–57`) und `GameState.HotSeat.cs` (`BeginManagerTurn`, Z. 65). |
| R12RP-06 (#501) | **behoben** | In `re-runde-12.md` kein „prüfen“ mehr; E2-Tabelle (Automatik, Keine Reihe) → `kader-transfer` Teil 4, UM-4 0x414C6/0x5C559 → `ui-meldungen` Teil 3 zugewiesen. |

**Neue Beobachtung (P3, kein Befund mit Wirkung):** Durch die neue Zeile 66 ordnet die Funktionskarte `re-runde-12.md:66` auch
0x0D9F2 (vermutlich über „0xE1A4“ als Dateiadresse gelesen) und 0x4A1DB zu, Z. 61 über das Bereichsende 0x45AA7 zusätzlich
0x45AA7. Alle drei sind bereits „umgesetzt“; kein Status ändert sich. Nur zur Kenntnis für künftige DGROUP-Angaben ohne Präfix.

## 3. Ausgeführte Prüfungen

- Zugriff: `git ls-remote https://github.com/kaaasi/eishockey-manager-referenz HEAD` erreichbar; Originaldateien per
  `tools/claude/fetch-reference.sh original`.
- Checkout: `git rev-parse HEAD` = `534e06c4…`, `git status --porcelain` leer; Merge-Base mit `main` = Base `af907c5`
  (`main` unverändert). Delta mit `git diff 1bd70a8 534e06c` gelesen.
- `python3 tools/reverse-engineering/funktionskarte.py ~/referenz/original/gamedata/em.exe --repo <stand> --csv <scratch>` für
  Base, `1bd70a8` und Head (Base und alter Head in temporären Worktrees außerhalb des Repos); Statusvergleich je Funktion:
  einzige Änderung zwischen den Ständen ist 0x03FB7 (unbekannt → dokumentiert → unbekannt). Head: 679 Funktionen,
  unbekannt 257 / umgesetzt 247 / dokumentiert 171 / genähert 4. `git status` danach leer.
- `disasm.py … 0x44E22 0x45AA7` (Schreiber und Bedingungen siehe Tabelle), `disasm.py --xref … 0xE1A4 0xE1A5`.
- Remake: `Transfer.Contract.cs:122–129`, `FinanceInbox.cs:55–57`, `GameState.HotSeat.cs:65` gelesen.

**`tools/pruefen.sh`** (alle drei Läufe, SDK 10.0.402, Commit `534e06c`), Ergebnis „bestanden“:

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1982 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1670 | 0 | 280 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1671 | 311 | 0 |

Keine Gegenproben: Delta ohne Code und Tests.

**Akzeptierte `[APPROX]`:** keine neuen. **Annahmen:** wie im ersten Review (nicht erneut geprüft).
