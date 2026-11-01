# Nachprüfung Review PR #382 – Leitstelle: Lese- und Schreibzugriff auf beide Repos

**Reviewer:** Claude Code (frische Sitzung ohne Implementierungskontext; die Delta-Commits stammen aus einer anderen
Sitzung) · **Datum:** 01.11.2026
**Base:** `664ff18f8675c12d63c5f6ad4075309d5975b829` · **Head geprüft:** `137bf2a4de87f48b12ae8d7cfa023d8691dec9ee`
**Nachprüfung von:** `docs/reviews/2026-11-01-leitstelle-zugriff.md` (Branch `review/leitstelle-zugriff`), bisher geprüfter
Head `307cb763edaf4877325323f14fda2db00b738100`; geprüft nur Delta `307cb76..137bf2a` (Commits `c977f96`, `137bf2a`) und
die Befunde P3-1 bis P3-3.

## 1. Merge-Empfehlung

**Ja** für genau diesen Head. Weiterhin reiner Doku-PR (5 Dateien, nur `.md`). Alle drei Befunde des Vorreviews sind
behoben. Die Regel „Spuren und Reviews schreiben nie ins Referenz-Repo“ steht jetzt widerspruchsfrei in `AGENTS.md` §3,
`spur-start.md` und `review-anfrage.md` und passt zu `CLAUDE.md` („Leitstelle … ablegen per PR, Branch `daten/…`“). Die
neue Abbruchregel in Schritt 7 ist eindeutig. Die zwei neuen Befunde sind P3 (Formulierung) und blockieren nicht.

## 2. Befunde

### Status der Befunde aus dem Vorreview

| Befund | Status | Nachweis |
|---|---|---|
| P3-1 `CLAUDE.md` Einrichtung nur `read` | **behoben** | [CLAUDE.md Z. 16](https://github.com/kaaasi/eishockey-manager-redone/blob/137bf2a4de87f48b12ae8d7cfa023d8691dec9ee/CLAUDE.md#L16) und [Z. 31](https://github.com/kaaasi/eishockey-manager-redone/blob/137bf2a4de87f48b12ae8d7cfa023d8691dec9ee/CLAUDE.md#L31): „access `read`, Leitstelle immer `push`“ – deckt sich mit Z. 45–47 und `leitstelle-start.md` Schritt 0. |
| P3-2 Geerbtes Schreibrecht ohne Nutzungsregel | **behoben** | [AGENTS.md Z. 41](https://github.com/kaaasi/eishockey-manager-redone/blob/137bf2a4de87f48b12ae8d7cfa023d8691dec9ee/AGENTS.md#L41) (Spuren und Reviews nur lesen; ablegen nur Leitstelle und Nutzer per PR), [spur-start.md Z. 22–23](https://github.com/kaaasi/eishockey-manager-redone/blob/137bf2a4de87f48b12ae8d7cfa023d8691dec9ee/docs/vorlagen/spur-start.md#L22-L23) (Erbe benannt, „schreiben dort trotzdem nie“, fehlende Daten im PR melden), [review-anfrage.md Z. 49](https://github.com/kaaasi/eishockey-manager-redone/blob/137bf2a4de87f48b12ae8d7cfa023d8691dec9ee/docs/vorlagen/review-anfrage.md#L49) („nichts ins Referenz-Repo schreiben“). |
| P3-3 Satzbau Schritt 7 | **behoben** | [leitstelle-start.md Z. 28–30](https://github.com/kaaasi/eishockey-manager-redone/blob/137bf2a4de87f48b12ae8d7cfa023d8691dec9ee/docs/vorlagen/leitstelle-start.md#L28-L30): als Bedingung formuliert („Schritt 0 erledigt (beide Repos `push`) und `git ls-remote …` erfolgreich – sonst keinen Lauf anlegen“). |

Widerspruchsfreiheit (besonders geprüft): Die drei Stellen nennen dieselbe Rollentrennung (Spuren/Reviews lesen,
Leitstelle/Nutzer legen ab). Der Zugriffsabsatz der Spuren (`spur-start.md` Z. 13–17, `add_repo` access `read`) ist
unverändert und passt dazu. `AGENTS.md` §3 trägt die Einschränkung nur in der Zeile „Referenzbilder“; die Zeile
„Originaldateien“ nennt dasselbe Repo nur mit „Leserechte … nötig“ – kein Widerspruch, da die Regel ans Repo gebunden ist
und `spur-start.md`/`review-anfrage.md` sie allgemein fassen.

### Neue Befunde im Delta

- **N-1 (P3) – Abbruch in Schritt 7 ohne Meldung.**
  [leitstelle-start.md Z. 29–30](https://github.com/kaaasi/eishockey-manager-redone/blob/137bf2a4de87f48b12ae8d7cfa023d8691dec9ee/docs/vorlagen/leitstelle-start.md#L29-L30):
  „– sonst keinen Lauf anlegen“ sagt nicht, was danach geschieht. Schritt 0 deckt nur „Freigabe lehnt `push` ab →
  melden“; der Fall „`add_repo` erfolgreich, aber `ls-remote` scheitert“ bleibt ohne Anweisung. Vorschlag: „– sonst
  keinen Lauf anlegen und dem Nutzer melden (Fehlermeldung von `ls-remote`).“
  Gegenbeispiel: keins im Repo; Lücke in der Regelung.
- **N-2 (P3) – Prüfung belegt nur Lesezugriff.**
  Gleiche Stelle: Die Bedingung verlangt „beide Repos `push`“, die mechanische Prüfung `git ls-remote … HEAD` weist aber
  nur Lesezugriff nach. Für die Läufe genügt Lesen; „push“ ist durch das `add_repo`-Ergebnis aus Schritt 0 belegt, nicht
  durch `ls-remote`. Kein Fehlverhalten, nur eine stillschweigende Annahme. Optional: „(push laut `add_repo`-Ergebnis)“
  ergänzen. Die strengere Bedingung blockiert Läufe, wenn nur `read` freigegeben wird – das entspricht der Vorgabe des
  Nutzers (Leitstelle immer `push`, sonst melden) und wird hier nicht beanstandet.

## 3. Ausgeführte Prüfungen

| Prüfung | Ergebnis |
|---|---|
| `git fetch origin pull/382/head`, Checkout, `git rev-parse HEAD` | `137bf2a4…`, `git status --porcelain` leer |
| `git merge-base HEAD origin/main` | `664ff18…` = Base = aktueller `main` |
| `git diff --stat 307cb76..137bf2a` | `AGENTS.md`, `CLAUDE.md`, `docs/vorlagen/{leitstelle-start,review-anfrage,spur-start}.md`; +9/−7 |
| `git diff --name-only 664ff18..137bf2a` | nur obige 5 `.md`-Dateien – reiner Doku-PR |
| `git diff --check 664ff18..137bf2a` | sauber |
| `grep -rn -i referenz` mit schreib/push/ablegen/daten/commit (ohne Archiv/Reviews) | nur die geprüften Stellen und `docs/originaldateien.md` Z. 14 (historische Ablage über Referenz-PR #1); kein Widerspruch |
| Zeilenlänge | Neue Zeilen > 120 Zeichen (z. B. `CLAUDE.md` Z. 16, 31); keine Regel/Prüfung im Repo, bestehende Dateien enthalten bereits längere Zeilen – kein Befund |
| Autorschaft Delta | Commits aus Sitzung `…VsfGsFY8gFqBxVuAP`, nicht aus dieser Sitzung |
| CI am Head (Check-Runs) | `build` bei Prüfung noch `in_progress` |
| `tools/pruefen.sh` | **nicht ausgeführt** – reiner Doku-PR, keine Code-/Test-/CI-Dateien |

Gegenbeispiele erneut ausgeführt: keine vorgegeben. Akzeptierte `[APPROX]`: keine (Doku).
Annahmen: `add_repo` mit access `push` funktioniert (in dieser Sitzung für `eishockey-manager-redone` bestätigt; für das
Referenz-Repo nicht geprüft, da das Review dort nichts liest oder schreibt).
