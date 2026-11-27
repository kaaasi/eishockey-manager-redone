# Review PR #984 – Erfahrungen der Leitstelle aus Runde 23

Reviewer: Claude Sonnet 5.5 (frische Sitzung, keine Mitarbeit am PR) · Base `a51848f` · Head `9f94bb4`
Umfang: reine Doku, nur Codelesen plus Dateiprüfung (kein Build, kein `tools/pruefen.sh`: nur `docs/**`/`*.md`, CI-`paths-ignore`
in `.github/workflows/build.yml` Zeilen 9 und 12 passt).

## 1. Merge-Empfehlung

**Ja** für genau diesen Head `9f94bb4`. Die Änderung ist knapp (12 Zeilen hinzu, 2 geändert) und nur Doku. Befund P2-1 ist ein
Folgeauftrag und blockiert nicht, weil die Regel nur die Leitstelle betrifft (anhang.md F).

Nachgewiesen am Checkout: `git rev-parse HEAD` = `9f94bb4c94ecf8782fb59b872069b15ca79eae47`, `git status --porcelain` leer.

## 2. Befunde

**P2-1 – Neue Nachprüfungsregel widerspricht dem Regelwerk, ohne es zu nennen.**
[`docs/prozess/anhang.md` Zeile 112–114](https://github.com/kaaasi/eishockey-manager-redone/blob/9f94bb4c94ecf8782fb59b872069b15ca79eae47/docs/prozess/anhang.md#L112-L114):
„Fixlauf mit neuer Regel braucht immer eine Nachprüfung, auch wenn die Befunde nur P2/P3 sind.“ Dagegen stehen unverändert
`zusammenarbeit.md` §5 Punkt 3 („Nachprüfung nur, wenn der Fixlauf P0/P1 behebt“), Punkt 4 (nur P2/P3 → Delta-Prüfung der
Leitstelle), §6 Nr. 1 und die Labeltabelle (`leitstelle-delta`), und `spur-start.md` „Fixlauf“ („sonst – nur P2/P3 … – Label
`leitstelle-delta`“). Ein Fixlauf liest nur `spur-start.md`/`zusammenarbeit.md`, setzt daher `leitstelle-delta`, und die
Leitstelle müsste die Ausnahme aus dem Anhang selbst erinnern.
Beleg: `grep -nE 'P2/P3|nur P2' docs/prozess/zusammenarbeit.md docs/vorlagen/spur-start.md docs/prozess/anhang.md` zeigt die
Ausnahme nur in `anhang.md`. Vorschlag: in `zusammenarbeit.md` §5 Punkt 4 einen Halbsatz „außer der Fixlauf setzt eine neue Regel
um (`anhang.md` F)“ ergänzen, oder die Ausnahme dort statt im Anhang führen. Außerdem undefiniert: was „neue Regel“ ist.

**P3 (Verdacht, ohne Beleg) – `CLAUDE.md` Zeile 44–45** behauptet „der Proxy zeigt dort nicht alle“ Branches. Dieser Lauf
konnte das nicht prüfen. `anhang.md` Zeile 66/86 und `spur-start.md` Zeile 13 schreiben `git ls-remote` weiter vor, aber für das
Referenz-Repo (Zugriffsprobe), nicht für Branches im Code-Repo; also kein Widerspruch, nur ein Hinweis für spätere Leser.

## 3. Besonders geprüft

- **Knapp, keine Wiederholung:** `zusammenarbeit.md` §5 Punkt 7 und `CLAUDE.md` sind je ein bis zwei Zeilen und neu. Die
  Schemanummer in `spur-start.md` ergänzt `anhang.md` Zeile 104 (dort: Merge-Reihenfolge und Zeitpunkt des Rebase-Fixlaufs),
  wiederholt sie nicht. Die „send_later“-Zeile in `anhang.md` ist mit Zeile 123 (60–90 Minuten Rückmeldungen) vereinbar und
  begrenzt sie sinnvoll; Zeile 101 („`send_later` stand nicht verlässlich bereit“) betrifft das Warten und bleibt gültig.
- **Reihenfolge der Fertigstellung (anhang.md F):** kein Widerspruch gefunden; kein anderer Text schreibt die Planfolge als
  Pflicht vor (`grep -rniE 'reihenfolge|planfolge'` in `docs/prozess`, `docs/vorlagen`).
- **Kurzaufruf in `spur-start.md` unverändert:** `git diff a51848f 9f94bb4 -- docs/vorlagen/spur-start.md` ändert nur Zeile 38/41
  im Absatz „Fixlauf“ (Base-Wechsel); der Kurzaufruf-Block (Zeilen 1–25) ist unberührt.
- **Keine Spurdateien:** `git diff --name-only a51848f 9f94bb4` = `CLAUDE.md`, `docs/prozess/anhang.md`,
  `docs/prozess/zusammenarbeit.md`, `docs/vorlagen/spur-start.md`; kein Code, keine Tests, kein `docs/arbeitspakete.md`.
  Per `git diff --stat`: 4 Dateien, 12 Einfügungen, 2 Löschungen. `compare` über die REST-Dateiliste (`get_files`) deckt sich.
- **CLAUDE.md-Vorrang:** der neue Punkt liegt im Abschnitt „GitHub aus der Sitzung“ und widerspricht `AGENTS.md` nicht.
- **Beispiel #975** in `zusammenarbeit.md`: Verweis nicht gegen das PR geprüft (kein Zugriff auf Inhalt in diesem Lauf nötig);
  Aussage ist als Regel formuliert und verträglich mit §5 Punkt 7.

## 4. Ausgeführte Prüfungen und `[APPROX]`

| Prüfung | Ergebnis |
|---|---|
| `git rev-parse HEAD` / `git status --porcelain` | Head `9f94bb4…`, sauber |
| `git diff --name-only a51848f 9f94bb4` | 4 Doku-Dateien |
| `grep` auf Widersprüche (send_later, Reihenfolge, Schemanummer, ls-remote, P2/P3) | 1 Widerspruch (P2-1) |
| Build / `tools/pruefen.sh` | nicht ausgeführt, entfällt (reine Doku, CI startet nicht) |

Akzeptierte `[APPROX]`: keine, kein Code. Annahme (nicht geprüft): Schilderungen aus Runde 23 (Proxy, `send_later`,
#975) stammen aus dem PR-Text und sind hier nicht nachgestellt.
