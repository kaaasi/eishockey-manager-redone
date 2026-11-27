# Nachprüfung PR #719 – Plan Runde 15 (reine Doku-Prüfung, nur Delta)

Reviewer: Claude Opus 5.5 (geplanter Lauf, Sitzung ohne Mitarbeit an #719) · Base `892ca856cd3f140f03c23c6a20a1888b89866e61` ·
Head `5f539a8146f3919ae2178d5e51b5a4a6cd8f37a6` · Delta `37f9b5d..5f539a8` · Vorreview `2026-11-10-plan-runde-15.md` (#720) · 10.11.2026

## 1. Merge-Empfehlung

**Ja für genau diesen Head.** R719-01 (P2) ist behoben, R719-02 bis R719-04 erledigt bzw. an `prozess-kern` übergeben.
Drei neue P3-Unschärfen (N1–N3), kein Merge-Hindernis; sie passen in den Auftrag von `prozess-kern`.

## 2. Befunde des Vorreviews

| ID | Ergebnis | Beleg (Gegenbeispiel erneut ausgeführt) |
|---|---|---|
| R719-01 (P2) | behoben | `grep -n 'Rebase durch' docs/vorlagen/*.md` → kein Treffer; `leitstelle-start.md` Z. 18–22 und `zusammenarbeit.md` Z. 132–134 neu |
| R719-02 (P3) | behoben | `grep -n 'Gestapelte\|Folgeteil mit Base'` → kein Treffer; `zusammenarbeit.md` Z. 57–58 nur noch historisch („bis Runde 14“). Rest (z. B. `spur-start.md` Z. 44–45 „PR-Base“) laut `arbeitspakete.md` Abnahme `prozess-kern` dort – bestätigt |
| R719-03 (P3) | behoben | `ls src/EishockeyManager.Core \| grep '^Finance'` → 11 Dateien; `FinanceTalent.cs`/`FinanceYouth.cs` → `kader`, vier Tag-0-Dateien + `GameState.Manager.cs` (`AdvanceFinance`, Z. 141, vorhanden) → `tagesablauf`, übrige fünf über „nennt sie im PR“ |
| R719-04 (P3) | behoben | Abnahme `prozess-kern` „außer den Klärungen aus #712“; Spurmodell nennt Sonnet für Doku-Spuren; R15-D1 A „vier parallel“; Hinweis `abgleich-original` vor `hok-export` übernommen (aber N1) |

**R719-01 im Einzelnen:** Die Regel ist widerspruchsfrei. Fall „konfliktfrei“: kein Rebase, Head = freigegebener SHA (§6 Nr. 1
erfüllt), §6 Nr. 2 durch lokales `--schnell` mit Originaldaten auf `main`+PR mit Tabelle im Merge-Kommentar (zugleich
Nachweis für §6 Nr. 4). Fall „Head muss sich ändern“: Fixlauf, Delta-Prüfung nach §5 Punkt 4 nur bei Rebase ohne Konflikt plus
vom Plan verlangter Anpassung, belegt mit `git range-diff`; Schritt 2 und §6 Nr. 1 decken sich. Fall „Konflikt“: `integration/<runde>`.
Die Lücke aus dem Vorreview (wer rebased, wer prüft `abgleich-original`) ist geschlossen. Kleine Reste: N2, N3.

## 3. Neue Befunde (alle P3)

- **N1 – Widerspruch Merge-Folge `abgleich-original`.** `arbeitspakete.md` Z. 30 erlaubt den Merge vor `hok-export`, Z. 113
  sagt weiter „wird als letzte Spur gemergt“. Beleg: `grep -n 'letzte Spur gemergt\|darf .abgleich-original. vorher' docs/arbeitspakete.md`.
  Vorschlag Z. 113: „wird zuletzt gemergt (ggf. vor `hok-export`, siehe Folge)“.
- **N2 – §5 nennt die neue Ausnahme nicht.** `zusammenarbeit.md` Z. 84–85: „Ändert sich der Head … Nachprüfung nötig, außer im
  Fall ‚nur P2/P3‘“ – die Rebase-Ausnahme steht nur in §6 Nr. 1. Verbindlich ist §6, deshalb nur P3; bei `prozess-kern` angleichen.
- **N3 – Label für Rebase-Fixlauf offen.** §5 Punkt 8 regelt `leitstelle-delta` nur für Fixläufe „mit nur P2/P3“; ein Fixlauf
  ohne Befund (nur Rebase + Listen) hat kein Label, und der Fixlauf-Startaufruf (`spur-start.md`, „Befunde `<IDs>` beheben“)
  passt nicht wörtlich. Vorschlag: „Fixlauf ohne Befund (Rebase/Planpflicht) setzt `leitstelle-delta`“.

## 4. Ausgeführte Prüfungen

| Prüfung | Ergebnis |
|---|---|
| `git checkout 5f539a8`, `git rev-parse HEAD`, `git status --porcelain` | `5f539a8…` / leer |
| `git diff --stat 37f9b5d 5f539a8` | 4 Dateien, nur `docs/**` (25+/14−); gesamt `892ca85..5f539a8` 5 Dateien, nur Doku – CI und `tools/pruefen.sh` entfallen |
| Gegenbeispiele R719-01…03 | wie Tabelle §2 |
| Konsistenz Schritt 2 ↔ §6 Nr. 1/2/4 ↔ §5 Punkt 4/8 ↔ `spur-start.md` Schritt 5/„Teile“ 2 | stimmig bis auf N2, N3 |
| Erwartet offen | Rest #712 und Teile R719-02 an `prozess-kern` (`arbeitspakete.md` Abnahme) – bestätigt |

Nur Doku gelesen, kein Build, keine Tests (Kurzaufruf). Keine `[APPROX]` betroffen.
