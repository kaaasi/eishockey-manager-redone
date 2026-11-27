# Review PR #684 – Prozess: sparsame Review-Kette

Base `e5fe46f3f9e5c343366e505eab1cc5d48f863d47` · Head `8f3fe242533d649593968e017c3ee2b1f7239f19` · Reviewer: Claude Sonnet 5.5 (neue Sitzung, keine Mitarbeit am PR)
Umfang: nur Codelesen der Doku-Änderung (`*.md`), kein Build, keine Tests. Checkout geprüft: `git rev-parse HEAD` = Head-SHA.

## 1. Merge-Empfehlung

**Nein, für genau diesen Head** – die Entscheidung (Punkte 1+2, Modellwahl) ist inhaltlich vollständig umgesetzt, aber zwei bestehende Stellen widersprechen ihr noch (F1, F2). Nach deren Anpassung (kleine Doku-Änderung) wäre ein Merge unkritisch. Kein Testnachweis nach `zusammenarbeit.md` §6 Nr. 4 (Doku, entfällt).

## 2. Befunde

| ID | Prio | Stelle | Befund |
|---|---|---|---|
| F1 | P2 | `docs/vorlagen/review-anfrage.md:37` | Reviewer-Ablauf verlangt für jedes Review „`tools/pruefen.sh` (alle drei Läufe)“, auch für Nachprüfungen (Zeilen 17–18). Neu: Fixlauf/Nachprüfung nur `--schnell` (`zusammenarbeit.md:84–86`, `AGENTS.md:109–110`). Ein Reviewer, der nur die Vorlage liest, führt weiter alle drei Läufe aus. Die Vorlage ist der Ort, den Reviewer wirklich lesen; dort Ausnahme für Nachprüfung ergänzen. |
| F2 | P2 | `docs/vorlagen/leitstelle-start.md:32–34` | „Modell … setzen, wenn es von Opus abweicht – nur auf ausdrücklichen Wunsch des Nutzers“. Die neue Modellwahl (`zusammenarbeit.md:87–90`) legt Sonnet 5.5 für mechanische Arbeit als Regel fest. Unklar, ob die Leitstelle Sonnet nun selbst wählen darf oder weiterhin je Lauf den Nutzer fragen muss. Einen Satz angleichen („außer mechanische Arbeit nach `zusammenarbeit.md` §5“ oder ausdrücklich „Nutzer bestätigt weiterhin“). Gleiches gilt für `spur-start.md:76–81` („Modell der Spur“). |
| F3 | P3 | `docs/prozess/zusammenarbeit.md:87–90` | Der Bullet „Modellwahl“ steht zwischen Punkt 3 und 4 der nummerierten Liste und trennt sie; besser nach Punkt 6 oder als eigener Absatz. |
| F4 | P3 | `zusammenarbeit.md:82` gegen `:16` und `CLAUDE.md:62` | „Erstreview … vollständig mit Claude Opus 5.5“, während als Standard-Reviewer ChatGPT geführt wird (Zeile 16, Erstreviews und Gesamtreview). Gemeint ist offenbar „wenn Claude reviewt“; klarstellen, sonst widersprüchlich. |
| F5 | P3 | `review-anfrage.md:55–56` | „Nachgeprüft wird nur noch das Delta von Fixes aus dem Gesamtreview“ – ohne Verweis auf die P0/P1-Grenze der sparsamen Kette. Kein Widerspruch, aber ein Verweis wäre hilfreich. |

Geprüft und ohne Widerspruch: `AGENTS.md:108–110` (Erstreviews, `--schnell`-Verweis stimmt mit §5), `zusammenarbeit.md:79` (Ausnahme „nur P2/P3“), `:107` und `:115–116` (Merge-Prüfung Nr. 1 und 4 konsistent mit Punkt 4), `leitstelle-start.md:18` (Integration: alle drei Läufe bleibt richtig), AGENTS.md Regel 2.4 (Leitstelle prüft ein Delta, das sie nicht umgesetzt hat; zulässig). Die PR-Beschreibung stimmt mit dem Diff überein (2 Dateien, +25/−5).

## 3. Ausgeführte Prüfungen

Diff und PR-Beschreibung gelesen, die genannten fünf Dokumente per `grep` nach Nachprüfung, Läufen, Modell und Reviewer durchsucht. Keine Befehle mit Build oder Tests. Keine `[APPROX]`-Punkte betroffen.
