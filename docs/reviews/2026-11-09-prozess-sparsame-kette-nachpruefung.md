# Nachprüfung PR #684 – Prozess: sparsame Review-Kette

Review: `2026-11-09-prozess-sparsame-kette.md` (Head damals `8f3fe242533d649593968e017c3ee2b1f7239f19`) · Delta `8f3fe24..91421ff78bdbbc2089dedb043b1cfcb0993f7350` · Reviewer: Claude Sonnet 5.5 (neue Sitzung, keine Mitarbeit am PR)
Umfang: nur Codelesen des Deltas (4 Doku-Dateien, +13/−9), kein Build, keine Tests, kein Testnachweis nach `zusammenarbeit.md` §6 Nr. 4 (entfällt bei Doku).

## 1. Merge-Empfehlung

**Ja** für Head `91421ff` (nur Codelesen). F1–F5 sind behoben, das Delta schafft keinen Widerspruch zu `AGENTS.md`, `CLAUDE.md`, `zusammenarbeit.md` oder `docs/vorlagen/*.md`.

## 2. Befunde

| ID | Status | Beleg |
|---|---|---|
| F1 | behoben | `review-anfrage.md` Ablauf 2: „Erstreview: alle drei Läufe; Nachprüfung: `--schnell`“ mit Verweis auf §5 „Sparsame Kette“. |
| F2 | behoben | `leitstelle-start.md` (Schritt 7) und `spur-start.md` (Schritt 5) erlauben das Modell nach §5 „Modellwahl“ zusätzlich zum Nutzerwunsch. |
| F3 | behoben | „Modellwahl“ ist ein eigener Absatz nach Punkt 6; die nummerierte Liste ist nicht mehr unterbrochen. |
| F4 | behoben | Punkt 1: „(reviewt Claude: mit Claude Opus 5.5)“ – kein Widerspruch zum Standard-Reviewer ChatGPT mehr. |
| F5 | behoben | `review-anfrage.md` Gesamtreview: „nur bei P0/P1, sonst prüft die Leitstelle“ mit Verweis auf §5. |
| N1 | P3, neu | `zusammenarbeit.md` „Modellwahl“ nennt „Nachprüfungen“ als Opus-Fall und „reine Doku-Prüfungen“ als Sonnet-Fall; eine Doku-Nachprüfung (wie diese) fällt unter beide. Im Zweifel gilt Opus, daher kein Widerspruch – optional „Nachprüfungen von Code“ präzisieren. Kein Merge-Hindernis. |

Keine weiteren Widersprüche: `AGENTS.md` §5 (`--schnell` für Normal-PRs) und `leitstelle-start.md` Integration (alle drei Läufe) bleiben stimmig; Merge-Prüfung Nr. 1 und 4 passen zu Punkt 4.

## 3. Ausgeführte Prüfungen

`git diff 8f3fe24..91421ff` gelesen, `zusammenarbeit.md` §5 und `review-anfrage.md` im Head gegengelesen. Keine Befehle mit Build oder Tests. Keine `[APPROX]`-Punkte betroffen.
