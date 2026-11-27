# Review: Nachtrag Spur `inventur` (Plan Runde 10, PR #394)

- PR [#394](https://github.com/kaaasi/eishockey-manager-redone/pull/394) · Base `2d0b081bf10e2b03add5660b921dad30e6fe39ed` ·
  Head `6b9816c93e4fad8c5bc654a17b4a3f318190b4de` · Reviewer: Claude Opus 5.5 (geplanter Lauf, eigene Sitzung, an #394
  nicht beteiligt).
- Checkout per Klon, `git fetch origin pull/394/head`; `git rev-parse HEAD` = Head, `git status --porcelain` leer. Merge-Base
  von Head und Base = Base (ein Commit, 6b9816c).
- `em.exe` aus dem Referenz-Repo (SHA-256 `f41eb08a…294b` = `original/SHA256SUMS`).

Links: `https://github.com/kaaasi/eishockey-manager-redone/blob/6b9816c93e4fad8c5bc654a17b4a3f318190b4de/docs/arbeitspakete.md#L<n>`.

## 1. Merge-Empfehlung

**Ja für genau diesen Head.** Reiner Doku-PR (nur `docs/arbeitspakete.md`, +21 Zeilen), R10P-01 bleibt behoben (Karte an
Base und Head byte-gleich, keine Fundstelle in `arbeitspakete.md`), die Zahlen des Plans stimmen (382 Zeilen in 128 Dateien;
159 dokumentiert / 42 667 Byte; 7 genähert), `tools/pruefen.sh` (alle drei Läufe) und CI sind grün. Keine P0–P2-Befunde.
Die drei P3-Befunde betreffen die Eindeutigkeit für den unbeaufsichtigten Lauf; R10V-01 und R10V-02 sollten vor dem Start
der Spur behoben sein (eine Zeile, z. B. in der Integration oder einem kleinen Folge-PR), sonst muss der Lauf raten.

## 2. Befunde

### R10V-01 (P3) – „K1–K6“ gibt es nicht; `re-saisonwechsel.md` hat K1–K5

[L132](https://github.com/kaaasi/eishockey-manager-redone/blob/6b9816c93e4fad8c5bc654a17b4a3f318190b4de/docs/arbeitspakete.md#L132)
nennt die Kandidaten „G1–G8, K1–K10, K1–K6, L1–L8, K1–K7“. Gegenbeispiel: in `docs/analyse/*.md` am Head

| Datei | Kennungen |
|---|---|
| `re-geld.md` | G1–G8 |
| `re-meldungen.md` | K1–K10 |
| `re-saisonwechsel.md` | **K1–K5** (Tabelle L215–219, kein K6 im ganzen Dokument) |
| `re-livespiel.md` | L1–L8 |
| `re-rest.md` | K1–K7 |

(`grep -oE '^\| *[GKL][0-9]+ *\||\*\*[GKL][0-9]+\*\*' docs/analyse/<datei>.md`). Ein unbeaufsichtigter Lauf findet K6 nicht
und muss entscheiden, ob ein Kandidat fehlt. Vorschlag: „K1–K5“.

### R10V-02 (P3) – Kennungen K… dreifach vergeben, Zuordnung zur Datei fehlt

K1 steht in `re-meldungen.md`, `re-saisonwechsel.md` und `re-rest.md` für drei verschiedene Kandidaten (z. B. K1
Oberliga-Austausch vs. K1 Talentsuche). Die Liste in L132 ordnet die Bereiche nur über die Reihenfolge zu; die Inventur
soll sie aber den Punkten aus 1 und 2 zuordnen und daraus Issues für Runde 11 ableiten. Vorschlag: Bereiche mit Datei nennen
und in `inventur.md` eine eindeutige Schreibweise vorgeben, z. B. „`re-geld` G1–G8, `re-meldungen` K1–K10,
`re-saisonwechsel` K1–K5, `re-livespiel` L1–L8, `re-rest` K1–K7; in `inventur.md` als `<spur>/<Kennung>` (z. B.
`re-rest/K1`)“.

### R10V-03 (P3) – Zähleinheit von „`[APPROX]`-Stelle“ nicht festgelegt

[L125](https://github.com/kaaasi/eishockey-manager-redone/blob/6b9816c93e4fad8c5bc654a17b4a3f318190b4de/docs/arbeitspakete.md#L125)
und die Abnahme
([L137](https://github.com/kaaasi/eishockey-manager-redone/blob/6b9816c93e4fad8c5bc654a17b4a3f318190b4de/docs/arbeitspakete.md#L137))
verlangen „jede `[APPROX]`-Stelle … genau einer Gruppe“, Gegenprobe `grep -rc`. `grep -rc` zählt **Zeilen** (382);
Vorkommen sind es 383 (`grep -ro '\[APPROX\]' src | wc -l`): `src/EishockeyManager.Core/StadiumStatistics.cs:144` enthält
zwei Marker in einer Zeile. Zugleich dürfen gleichartige Stellen zu einer Tabellenzeile zusammengefasst werden – dann muss
jede Zusammenfassung die Zahl ihrer Zeilen tragen, sonst ist die Gegenprobe nicht nachrechenbar. Vorschlag: „Stelle =
Zeile mit `[APPROX]` (`grep -rc`, 382); zusammengefasste Zeilen nennen Datei und Anzahl, die Summe je Gruppe ergibt 382.“

### Hinweis (kein Befund) – Umfang in einem Lauf

Machbar, aber groß: 382 Zeilen in 128 Dateien, 166 Funktionen (159 + 7, 46,9 KB) und 37 Kandidaten. Die Zusammenfassung je
Datei und die vorhandenen Fundstellen der Karte (Spalte `fundstellen`) machen es in einem Lauf erreichbar. Die Abnahme ist
aber alles oder nichts; reicht die Zeit nicht, greift nur die Regel „Draft mit `spur-wartet`“. Eine Reihenfolge für diesen
Fall („zuerst 2 und 3, dann 1“) wäre eine Halbzeile wert. Ebenso gilt neben der eigenen Abnahme die allgemeine „Abnahme je
Spur“ (L46–52: Spalte `fundstellen` nennt `inventur.md`, Abschnitt „Kandidaten Runde 11“) – zusammen eindeutig, wenn
`inventur.md` die Adressen in normaler Schreibweise führt; die priorisierte Liste sollte dann so heißen. Die einzige
unbekannte Spielcode-Funktion 0x575C3 (421 Byte) fällt nicht in Punkt 2; sie ist unter „Zurückgestellt“ geführt (#393),
also richtig.

### Erwartet offen

- **#386** (R10Z-01), **#391** (R10I-01), **#392** (R10I-02), **#393** (R10I-03): alle als Issue `open` bestätigt; am Head
  nicht berührt (der PR ändert nur den neuen Abschnitt L120–140). #392 und #393 sind am Head weiter sichtbar (L73 „wartet auf
  Review“, L144 „dort mitzuanalysieren“).

## 3. Ausgeführte Prüfungen

| Prüfung | Ergebnis |
|---|---|
| `git diff --stat 2d0b081 6b9816c` | 1 Datei, `docs/arbeitspakete.md` +21; kein `src/`, `tests/`, `tools/` |
| R10P-01: `funktionskarte.py <em.exe> --repo . --csv …` an Head und Base | CSV und Markdown byte-gleich (`cmp`, `diff`); `arbeitspakete` in `fundstellen`: 0 |
| Gegenprobe R10P-01 (Mutation in temporärem Worktree: Zeile „0x575C3“ im neuen Abschnitt) | 0x575C3 wird `dokumentiert`, Fundstelle `docs/arbeitspakete.md:140` → Prüfung ist empfindlich |
| Neue Zeilen auf `0x…`-Adressen | keine (nur `h`-Schreibweise in Kontextzeilen) |
| `grep -rc '\[APPROX\]' src` (Summe, Dateien > 0) | 382 in 128 ✓ (Vorkommen 383, siehe R10V-03) |
| Karte `--csv`, Spielcode (ohne Systemsegmente) | dokumentiert 159 / 42 667 Byte ✓ („etwa 43 KB“), genähert 7 ✓, unbekannt 1 (0x575C3) |
| Kandidaten-Kennungen in `docs/analyse/*.md` | G1–G8, K1–K10, **K1–K5**, L1–L8, K1–K7 (R10V-01) |
| Issues #386, #391–#393 | `open` |
| CI am Head (Check-Runs) | `build` success, `publish` skipped |
| `tools/pruefen.sh` (SDK 10.0.402, alle drei Läufe) | siehe unten, Ergebnis: bestanden |

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1631 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1351 | 0 | 251 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1351 | 280 | 0 |

Commit: 6b9816c93e4fad8c5bc654a17b4a3f318190b4de · SDK: 10.0.402

**Akzeptierte `[APPROX]`:** keine (reiner Doku-PR). **Annahmen:** keine; alle Zahlen am Head nachgezählt.
