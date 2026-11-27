# Nachprüfung PR #1056 – Plan Runde 27

Reviewer: Claude Sonnet 5.5 (frische Sitzung, geplanter Lauf, keine Mitarbeit am PR). Nachprüfung von
`docs/reviews/2026-11-27-plan-runde-27.md` (PR #1057). Geprüft: Base `6506d62`, Head `e61d9e4` (`git rev-parse HEAD` = Head,
`git status --porcelain` leer), nur Delta `e53d7dd..e61d9e4` (`docs/arbeitspakete.md`, +28/−13; `AGENTS.md` unverändert).
Nur Doku: kein Build, kein `tools/pruefen.sh`, CI entfällt. Aussagen zum Code sind **nur Codelesen**, kein Testnachweis nach
`zusammenarbeit.md` §6 Nr. 4; für einen Plan-PR ohne Codeänderung nicht erforderlich.

## 1. Merge-Empfehlung

**Ja für `e61d9e4`.** P2-1 und P2-2 sind im Plan behoben, P3-1 bis P3-7 eingearbeitet, die Dateigrenzen bleiben disjunkt. Offen
bleibt nur ein P3 im Plantext (unten), das die Wellenfolge nicht berührt.

## 2. Befunde

### Behoben (Gegenbeispiele erneut ausgeführt)

- **P2-1 behoben.** `sed -n 586,600p tools/reverse-engineering/funktionskarte.py` zeigt unverändert: `scan_docs` überspringt nur
  `docs/archiv/` und `docs/reviews/` sowie `NOT_EVIDENCE_DOCS` (`re-abdeckung.md`, `analyse/inventur.md`, Zeile 424). Der Plan
  reagiert passend: `re-abdeckung.md` bleibt (Teil 3 und Regel „Abdeckungskarte“); `scan_docs` liest zusätzlich
  `docs/archiv/analyse/` (Teil 2); `docs/archiv/analyse/` existiert noch nicht, es gibt also keine fremden Neubelege, die die
  Verteilung verschieben. Probe `funktionskarte.py <em.exe> --csv` vor/nach mit gleicher Statusverteilung steht in `-archiv`
  (Prüfung) und `-doku` (Prüfung) sowie in den Regeln der Runde. Die verbleibenden Pfade der Skripte (`NOT_EVIDENCE_DOCS`,
  `OTHER_FILE_DOCS = docs/formate/spielstand-original.md`) liegen auf Dateien, die bleiben. Der Lauf selbst war nicht möglich
  (`em.exe` fehlt); es gibt keinen Test zu `funktionskarte.py` (`grep -rln funktionskarte tests src .github` leer).
- **P2-2 behoben.** `grep -n "Einzelpunkte\|Import.Rest\|Export.Rest" docs/arbeitspakete.md` → Zeile 66 (Tabelle) und Zeile 128
  (`aufraeumen-doppelt` Teil 5: `Manager.Einzelpunkte.cs`, `OriginalImport.Rest.cs`, `OriginalExport.Rest.cs` nach Inhalt
  umbenennen/aufteilen, Verhalten unverändert). Dateien liegen im Core, den `-doppelt` ohnehin ändert; Zustandsabdruck und
  Markerprobe sichern ab.
- **P3-1** (`#980 P3-4` ohne Dateiänderung, Vermerk im PR, Teil 2 von `-doku`), **P3-2** (Harness im PR, Seed, Partie, entfernte
  oder umbenannte Felder als erwartete Abweichung), **P3-3** (Regel „Testhilfen“: `tests/*/Support/` nicht verschieben,
  `dotnet build` beider Testprojekte in Welle 4), **P3-4** (entfallene Migration als zweiter Grund der Markerprobe), **P3-5**
  („Thema K11 der Abweichungsliste deckt sich mit Konzept K6“ in Teil 0 und 2 von `-tot`), **P3-6** („nach dem Rundenarchiv
  `docs/archiv/runde-<n>.md`, nicht nach dem Datum“), **P3-7** (Vorbehalt „von der Leitstelle als R27-D3 gelesen, Bestätigung
  erbeten“ im Kopf der Runde): jeweils im Delta nachgewiesen (`git diff e53d7dd..e61d9e4`).

### P3-N1 – „nur `docs/**`“ stimmt für `aufraeumen-archiv` nicht mehr

Die Spur ändert jetzt auch `tools/reverse-engineering/funktionskarte.py` (`scan_docs`). Drei Stellen sagen weiter „nur
`docs/**`“ bzw. „Code gegen `docs/**`“: Wellenfolge (Zeile 54–55), Tabelle (Zeile 64, „mechanisch, nur `docs/**`“) und Überschrift
der Spur (Zeile 90). Ohne Wirkung auf die Wellen: `aufraeumen-migration` ändert nur `src/EishockeyManager.Core/SaveGame*`,
Fundstellen L12/C02 in `src/Core`, Migrationstests und `tests/…/Fixtures/` – keine Überschneidung mit `docs/**` oder
`funktionskarte.py`; `-doku` (Welle 5, `tools/**`) läuft nach `-archiv`. Kosmetik: die drei Stellen angleichen („`docs/**` und
`funktionskarte.py` (`scan_docs`)“), damit der Wortlaut zur Dateigrenze passt.

## 3. Geprüft ohne Befund

- **Dateigrenzen `aufraeumen-archiv`/`aufraeumen-migration`:** disjunkt (Mengen wie in P3-N1). Gemeinsame Datei
  `docs/arbeitspakete.md`: beide nur im eigenen Statusabschnitt.
- Änderung an `scan_docs` ist klein und eindeutig beschrieben („minimal“); Zeilen 594–597 zeigen, wo (Ausschlussliste
  `archiv`/`reviews`) sie ansetzt, ohne `docs/archiv/reviews` einzuschließen.
- Wellenfolge, Zuordnung und Modelle (R27-D4 = B) gegenüber dem ersten Review unverändert konsistent.

## 4. Ausgeführte Prüfungen und Annahmen

- `git diff --stat e53d7dd..e61d9e4`, `git diff e53d7dd..e61d9e4 -- docs/arbeitspakete.md`, `git diff --check` (leer); Lesen von
  `funktionskarte.py` (Zeilen 422–424, 586–600); `ls docs/archiv/`; Greps wie oben.
- **Nicht ausgeführt:** `tools/pruefen.sh` (nur Doku), `funktionskarte.py --csv` (`em.exe` fehlt), `markerliste.py`.
- **`[APPROX]`:** keine neuen. Annahme, nicht geprüft: Das Ziel `docs/archiv/analyse/` bleibt bis zum Lauf von `-archiv` leer.
