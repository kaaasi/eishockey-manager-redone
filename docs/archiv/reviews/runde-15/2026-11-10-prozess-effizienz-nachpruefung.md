# Nachprüfung Review PR #708 – Prozess: Effizienz (Doku-Prüfung)

Reviewer: Claude Code (geplanter Lauf, eigene Sitzung, nicht an #708 beteiligt) · Base `b13046422b285404ad6ddbe7b5f1b18e22069eea` ·
Head `5951f4a0df55f0db4b0be1a248d80116cbb85561` · Vorbericht `docs/reviews/2026-11-10-prozess-effizienz.md` (Head `28de8b7`) ·
Umfang: nur Delta `28de8b7..5951f4a` und R-01 bis R-05 · nur Codelesen (reine Doku, kein Build, CI entfällt)

## 1. Merge-Empfehlung

**Ja** für genau diesen Head (nur Codelesen). R-01 bis R-05 sind behoben; das Delta (5 Dateien, nur `*.md`) bringt keinen
Widerspruch zu §5/§6 `zusammenarbeit.md`, `spur-start.md`, `leitstelle-start.md`, `review-anfrage.md` oder `CLAUDE.md`.
Zwei neue P3-Hinweise (N-01, N-02) blockieren nicht. R-06 (P3) war nicht Teil des Auftrags und ist unverändert offen.

**Erwartet offen bestätigt:** Verdichten der Prozessdoku bleibt Vormerkung Runde 15; das Delta berührt `docs/arbeitspakete.md` nicht.

## 2. Befunde

| ID | Status | Gegenbeispiel / Beleg |
|---|---|---|
| R-01 | behoben | ChatGPT/Codex-Review ohne Label: [zusammenarbeit.md:109–110](https://github.com/kaaasi/eishockey-manager-redone/blob/5951f4a0df55f0db4b0be1a248d80116cbb85561/docs/prozess/zusammenarbeit.md#L109) „setzen kein Label; dort wartet die Leitstelle auf den Review-PR (`review/*`) und setzt das Label beim Auswerten“; Schleife fragt zusätzlich offene PRs mit Head `review/*` ab ([leitstelle-start.md:46](https://github.com/kaaasi/eishockey-manager-redone/blob/5951f4a0df55f0db4b0be1a248d80116cbb85561/docs/vorlagen/leitstelle-start.md#L46), [CLAUDE.md:65](https://github.com/kaaasi/eishockey-manager-redone/blob/5951f4a0df55f0db4b0be1a248d80116cbb85561/CLAUDE.md#L65)). Die Schleife sieht den Review-PR → endet nicht erst an der Obergrenze. |
| R-02 | behoben | „Wer entfernt welches Label wann?“: [zusammenarbeit.md:107–108](https://github.com/kaaasi/eishockey-manager-redone/blob/5951f4a0df55f0db4b0be1a248d80116cbb85561/docs/prozess/zusammenarbeit.md#L107) – Fixlauf `review-nein` mit dem Push des neuen Heads (gleichlautend [spur-start.md:34](https://github.com/kaaasi/eishockey-manager-redone/blob/5951f4a0df55f0db4b0be1a248d80116cbb85561/docs/vorlagen/spur-start.md#L34)), Leitstelle `leitstelle-delta`, `beleg-strittig`, `spur-wartet` nach Entscheidung/Anlegen; `review-ja`/`review-nein` gegenseitig durch das Review. Gegenbeispiel aus dem Vorbericht (`review-nein` + `leitstelle-delta` gleichzeitig) entsteht nicht mehr. Rest siehe N-01. |
| R-03 | behoben | Ausnahme „Label“ in Rollentabelle [zusammenarbeit.md:14](https://github.com/kaaasi/eishockey-manager-redone/blob/5951f4a0df55f0db4b0be1a248d80116cbb85561/docs/prozess/zusammenarbeit.md#L14), Kurzaufruf [review-anfrage.md:11–12](https://github.com/kaaasi/eishockey-manager-redone/blob/5951f4a0df55f0db4b0be1a248d80116cbb85561/docs/vorlagen/review-anfrage.md#L11) und Schritt 5 [review-anfrage.md:53](https://github.com/kaaasi/eishockey-manager-redone/blob/5951f4a0df55f0db4b0be1a248d80116cbb85561/docs/vorlagen/review-anfrage.md#L53). |
| R-04 | behoben | „je Label eine Abfrage … mehrere Labels in einer Abfrage sind UND-verknüpft“ [leitstelle-start.md:45](https://github.com/kaaasi/eishockey-manager-redone/blob/5951f4a0df55f0db4b0be1a248d80116cbb85561/docs/vorlagen/leitstelle-start.md#L45); `CLAUDE.md:65` ebenso. |
| R-05 | behoben | Dublettenprüfung per `list_triggers` in [zusammenarbeit.md:104–105](https://github.com/kaaasi/eishockey-manager-redone/blob/5951f4a0df55f0db4b0be1a248d80116cbb85561/docs/prozess/zusammenarbeit.md#L104) und [spur-start.md:32](https://github.com/kaaasi/eishockey-manager-redone/blob/5951f4a0df55f0db4b0be1a248d80116cbb85561/docs/vorlagen/spur-start.md#L32); Modell „nach Modellwahl“ bzw. „lässt sich nicht wählen … Modell prüfen (Soll: …)“ im Übergabekommentar – deckt sich mit „Teile und Fortsetzung“ Schritt 5. |

**N-01 (P3, neu) – `spur-wartet` mit „Entscheidung Nutzer“ bleibt dauerhaft offen.** Kettenende bzw. „blockiert“
([spur-start.md:78, 91–93](https://github.com/kaaasi/eishockey-manager-redone/blob/5951f4a0df55f0db4b0be1a248d80116cbb85561/docs/vorlagen/spur-start.md#L91)) wartet auf den Nutzer, nicht auf die Leitstelle; nach
[zusammenarbeit.md:108](https://github.com/kaaasi/eishockey-manager-redone/blob/5951f4a0df55f0db4b0be1a248d80116cbb85561/docs/prozess/zusammenarbeit.md#L108) entfernt die Leitstelle das Label erst „nach ihrer
Entscheidung“. Bis dahin meldet die Label-Schleife diesen PR bei jedem Lauf sofort. Probe: `git grep -n spur-wartet 5951f4a -- docs/vorlagen docs/prozess`
→ keine Regel für „vorgelegt, wartet auf Nutzer“. Vorschlag (Runde 15, Verdichten): Schleife meldet nur PRs, deren Label seit dem
letzten Lauf neu ist, oder Leitstelle entfernt `spur-wartet`, sobald sie dem Nutzer vorgelegt hat.

**N-02 (P3, neu) – Netzannahme für Codex uneinheitlich formuliert.** [CLAUDE.md:62](https://github.com/kaaasi/eishockey-manager-redone/blob/5951f4a0df55f0db4b0be1a248d80116cbb85561/CLAUDE.md#L62) „Reviews durch ChatGPT
brauchen Codex mit Netzzugang“ gegenüber [zusammenarbeit.md:109](https://github.com/kaaasi/eishockey-manager-redone/blob/5951f4a0df55f0db4b0be1a248d80116cbb85561/docs/prozess/zusammenarbeit.md#L109) „Reviews ohne Netz
(ChatGPT/Codex)“ und [review-anfrage.md:79](https://github.com/kaaasi/eishockey-manager-redone/blob/5951f4a0df55f0db4b0be1a248d80116cbb85561/docs/vorlagen/review-anfrage.md#L79) „arbeitet danach ohne Netz“. Folgenlos für das
Warten, weil die Schleife bei ChatGPT/Codex immer auch `review/*` abfragt; beim Verdichten einheitlich fassen.

## 3. Ausgeführte Prüfungen

| Prüfung | Ergebnis |
|---|---|
| PR #708 per REST: Head/Base | `5951f4a…` / `b130464…` ✓ |
| `git diff --stat 28de8b7 5951f4a` | 5 Dateien, nur `*.md`, +18/−9 |
| Delta vollständig gelesen; Kontext §5 Punkte 7–9, „Modellwahl“, Rollentabelle, `spur-start.md` Fixlauf und Schritte 5–6, `leitstelle-start.md` Schritt 10, `CLAUDE.md` Warteschleife | gelesen |
| `git grep -n -E 'review-nein\|leitstelle-delta\|beleg-strittig\|spur-wartet' 5951f4a -- docs/prozess docs/vorlagen CLAUDE.md` | Entfernregeln konsistent (R-02), keine widersprüchliche Stelle |
| `git grep -n -i 'ohne netz\|netzzugang' 5951f4a -- CLAUDE.md docs/vorlagen docs/prozess` | N-02 |
| Build, `tools/pruefen.sh` | nicht ausgeführt – reine Doku, laut Auftrag nicht nötig |

Akzeptierte `[APPROX]`: keine (Doku-PR). Annahme aus dem Vorbericht unverändert: Codex setzt ohne Netz kein Label (nicht erprobt).
