# Review PR #956: Rundenabschluss Runde 22 (nur Doku)

## 1. Merge-Empfehlung

**Ja** für genau diesen Head `137c62989ef09cae19299fed4da65669ef2781a1` (Base `81620ee`). Archiv enthält den Rundenabschnitt
wörtlich, `arbeitspakete.md` verliert außerhalb des Rundenabschnitts nichts, Ergebnis/Abschluss/Entscheidungen passen zu GitHub,
die Prozessänderungen widersprechen `AGENTS.md` nicht. Nur P3. Prüfung: **nur Codelesen** plus GitHub-Abfragen (Doku-PR,
`tools/pruefen.sh` entfällt; kein Originaldaten-Nachweis nötig).

## 2. Befunde

- **P3-1 – Branch hinter `main`.** `git merge-base 81620ee 137c629` = `350f550`; `main` enthält zusätzlich nur die Gesamtreview-Datei
  `docs/reviews/2026-11-22-runde-22-gesamt.md` (#955). Ein Zwei-Punkt-Diff gegen `81620ee` zeigt sie deshalb als „gelöscht“; der
  Drei-Punkt-Diff (`350f550..137c629`) berührt nur 4 Dateien (`git diff --stat`), sie wird **nicht** gelöscht. Kein Fehler;
  Rebase vor dem Merge ist nicht nötig, der Merge ist sauber.
- **P3-2 – Vormerkung „Runde 22: #896, #897, #899, #908, #912, #914“ entfällt.** Beleg: Zeilenabgleich Base-Abschnitt (Z. 11–179)
  gegen Archiv ∪ Head: genau diese eine Zeile fehlt in beiden. Sachlich erledigt (alle sechs Issues geschlossen, Abgleich per
  `gh api`), im Archiv-Kopf wird „Vormerkungen bleiben in `arbeitspakete.md`“ zugesagt – daher formal eine Abweichung, inhaltlich
  unschädlich.
- **P3-3 – #925 uneinheitlich benannt.** Issue-Titel: „A873: Zusatzwurf des schnellen Wegs bei Play-offs …“ (offen); der Kopf
  Runde 23 nennt A873 unter #948 und #925 als „DEB-Pokal-Rest“. Beschreibung von #925 nennt den DEB-Pokal-Fall (FILE44 `D014`) –
  vertretbar, aber Zuordnung A873 ↔ #925/#948 sollte in Runde 23 einmal geklärt werden.
- Verdacht, ohne Beleg: Neue Zeile in `zusammenarbeit.md` §5 (Modellwahl-Zusatz, ~190 Zeichen in einer Zeile) ist deutlich länger als
  die Umgebung; nur Lesbarkeit.

## 3. Ausgeführte Prüfungen

- Head: `gh api repos/…/pulls/956 --jq .head.sha` = `137c629…`; `git fetch origin pull/956/head`, `git rev-parse` gleich; Arbeitsbaum sauber.
- **Archiv wörtlich:** Python-Zeilenabgleich: jede nichtleere Zeile aus `81620ee:docs/arbeitspakete.md` Z. 11–179 steht in
  `docs/archiv/runde-22.md` **oder** im Head-`arbeitspakete.md`, bis auf P3-2. Kopfzeilen 1–10 und Z. 180–221 (Pakete `hok-format`,
  `wettbewerbe`, `speichern`, „Später“) im Head Zeile für Zeile identisch (`diff` leer). „Zurückgestellt“/„Vormerkungen“ bleiben erhalten.
- **GitHub-Abgleich:** Die im Ergebnis genannten PRs #917–#924, #926, #927, #929, #931–#934, #937, #940, #942–#944, #946, #947, #949, #950,
  #952, #953, #955: alle `state=closed, merged=true`, Branchnamen passen zu den Spuren (28 Stichproben via `gh api`; #928, #930, #941, #945
  nicht einzeln abgefragt). Issues #896, #897, #899, #908, #912, #930, #935, #938 geschlossen; offen: #925, #936, #939, #948, #951, #954 –
  deckt sich mit „Abschluss“ und Kopf Runde 23. `docs/reviews/` enthält 18 Dateien vom 17.–22.11.2026.
- **Prozess:** `anhang.md` F („Läufe melden ihr Ende selbst“: Push-Benachrichtigung, `send_later`) und `zusammenarbeit.md` §5
  (Modellwahl, Fable nur Ausnahme) widersprechen `AGENTS.md` §6/§7 und `CLAUDE.md` nicht; Merge weiter nur Leitstelle/Nutzer; das
  Verbot geplanter Aufgaben aus Läufen (R15-E4) betrifft Läufe, nicht die Leitstelle.
- **Erwartet offen bestätigt:** #939, #948, #951, #954, #925, Reste #938 (A876, P3-3)/#930 (A917), #936, DOSBox-Aufnahme `runde-22`
  (Archiv und Kopf Runde 23 nennen alle).
- **Nicht geprüft:** Inhalt der Spurabschnitte im Archiv gegen die Merge-Stände der Code-PRs; `[APPROX]`: keine neuen.
