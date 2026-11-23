# Nachprüfung PR #984 – Erfahrungen der Leitstelle aus Runde 23

Reviewer: Claude Sonnet 5.5 (frische Sitzung, keine Mitarbeit am PR) · Base `a51848f` · bisher geprüft `9f94bb4` · Head `15d5ba5`
Umfang: nur Delta `9f94bb4..15d5ba5` und Befund P2-1; reine Doku, nur Codelesen und Dateiprüfung (kein Build, kein `tools/pruefen.sh`).

## 1. Merge-Empfehlung

**Ja** für genau diesen Head `15d5ba5dd69af08a495bc4ee4abbd69310e142c9` (PR-Head per API bestätigt). P2-1 ist behoben; es bleibt
ein P3-Hinweis ohne Blockade.

Nachgewiesen: `git rev-parse HEAD` = `15d5ba5dd69af08a495bc4ee4abbd69310e142c9`.

## 2. Delta und CI

`git diff --stat 9f94bb4 15d5ba5`: 3 Dateien, 5 Einfügungen, 3 Löschungen (`docs/prozess/anhang.md`, `docs/prozess/zusammenarbeit.md`,
`docs/vorlagen/spur-start.md`). `git diff --name-only a51848f 15d5ba5` = diese drei plus `CLAUDE.md`; alles `docs/**`/`*.md`, daher
startet die CI nicht und ein Build entfällt.

## 3. Befund P2-1 – behoben

Gegenbeispiel erneut ausgeführt: `grep -nE 'P2/P3|nur P2|neue Regel' docs/prozess/zusammenarbeit.md docs/vorlagen/spur-start.md docs/prozess/anhang.md`.

- `zusammenarbeit.md` §5 Punkt 3 (Zeile 69–71): Nachprüfung „nur, wenn der Fixlauf P0/P1 behebt **oder eine neue Regel umsetzt**
  (Verhalten, das kein Befund verlangt, z. B. nach einer Nutzerentscheidung; `anhang.md` F)“.
- `spur-start.md` „Fixlauf“ (Zeile 37–38): Startaufruf der Nachprüfung auch, wenn der Lauf eine neue Regel umsetzt (Verweis auf
  §5 Punkt 3); sonst – nur P2/P3 oder ohne Befund – `leitstelle-delta`.
- `anhang.md` F (Zeile 114–115): Verweis auf §5 Punkt 3 statt eigener Regel.

Die Ausnahme steht jetzt dort, wo Fixlauf und Leitstelle lesen. „Neue Regel“ ist durch Klammer und Beispiel verständlich definiert.
Der Hauptwiderspruch ist damit weg.

## 4. Restpunkt

**P3 – Punkt 4, §6 Nr. 1 und Labeltabelle nennen die Ausnahme nicht.** `zusammenarbeit.md` §5 Punkt 4 (Delta-Prüfung bei „nur
P2/P3“), §6 Nr. 1 und die Zeile `leitstelle-delta` („Fixlauf, der nur P2/P3 behebt“) sind unverändert. Ein Lauf mit neuer Regel
und nur P2/P3-Befunden fällt wörtlich nicht unter „behebt nur P2/P3“ (er setzt mehr um), wird also nicht fehlgeleitet; ausdrücklich
ausgeschlossen ist er dort aber nicht. Außerdem ist „vom Plan verlangte Anpassung“ (Punkt 4, ohne Nachprüfung) von „Verhalten, das
kein Befund verlangt“ (Punkt 3) nur durch den Wortsinn getrennt. Vorschlag bei Gelegenheit: in Punkt 4 „und keine neue Regel
umsetzt (Punkt 3)“ ergänzen. Blockiert nichts.

## 5. Erwartet offen

**P3-Verdacht `CLAUDE.md`** (Branches per REST statt `git ls-remote`, „der Proxy zeigt dort nicht alle“): Beobachtung des Nutzers,
in diesem Lauf nicht prüfbar, unverändert offen; der Text liegt im Delta nicht.

## 6. Ausgeführte Prüfungen

| Prüfung | Ergebnis |
|---|---|
| PR-Head per API | `15d5ba5…`, Base `a51848f` |
| `git diff --stat 9f94bb4 15d5ba5` / `--name-only a51848f 15d5ba5` | nur Doku und `CLAUDE.md` |
| `grep` P2/P3, nur P2, neue Regel | Ausnahme in §5 Punkt 3, `spur-start.md`, `anhang.md`; kein Widerspruch zu Punkt 4, §6 Nr. 1, Labeltabelle (nur P3 oben) |
| Build / `tools/pruefen.sh` | nicht ausgeführt, entfällt (reine Doku) |

Akzeptierte `[APPROX]`: keine, kein Code.
