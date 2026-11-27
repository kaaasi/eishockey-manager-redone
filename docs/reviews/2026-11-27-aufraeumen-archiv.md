# Review PR #1060 – Spur aufraeumen-archiv

Geprüft: Base `b0a548d`, Head `a1f8fe7` (`git rev-parse HEAD` = `a1f8fe738508283397e6b734af22addbfa89e97c`, `git status --porcelain`
leer). Reviewer: Claude Sonnet 5.5 (geplanter Lauf, Sitzung ohne Implementierungskontext). Erstreview. Nur Doku und ein
Python-Werkzeug: `tools/pruefen.sh` entfällt, kein SDK-Lauf.

## 1. Merge-Empfehlung

**Nein** für genau diesen Head, wegen P2-1: 35 von 284 archivierten Reviews liegen im falschen `runde-<n>/`-Ordner. Der Rest
(Renames, Werkzeug, Links, Verweisliste) ist in Ordnung; nach Umsortieren (mechanisch, kein Inhalt) und Nachziehen von
`aufraeumen-verweise.md` ist ein Ja zu erwarten. Jetzt korrigieren lohnt: `aufraeumen-doku` schreibt die Pfade sonst fest in
`src/`, `tests/`, `tools/`.

## 2. Befunde

- **P2-1 – Reviews in falscher Runde.** Die Ablage folgt dem Datum, nicht der Runde: Reviews vom Rundenwechsel liegen in der
  Vorrunde, obwohl `docs/archiv/runde-<n>.md` ihre PR als Ergebnis der Folgerunde führt. Beleg: `docs/archiv/reviews/runde-21/
  2026-11-17-einzelpunkte.md` (PR #934) – `runde-22.md` Z. 11 und 162 nennen `einzelpunkte` (#934, Review #943) als Spur der Runde 22,
  `runde-21.md` nur einmal als Plan (Z. 41). Probe: je Datei die erste `#nnn` im Kopf mit `#nnn` in `runde-<n>.md` des Ordners
  und in allen anderen `runde-*.md` abgleichen; PR in der eigenen Runde fehlt, aber in genau einer anderen Runde vorhanden:
  - Runde 21 → 22: `einzelpunkte`, `hok-play-off-marken`, `kader-regeln`, `kaderplatz-werte`, `national`, `transfer-pool`, `zusehen-rest`
  - Runde 20 → 21: `finanzuebersicht`, `re-vor-dosbox`, `teamwert-aufstellung`, `zusehen`; Runde 21 → 20: `inventur-v1` (#883)
  - Runde 15 → 16: `pool`, `pool-nachpruefung`; 16 → 17: `re-livespiel-2`, `wettbewerbe-rest`; 17 → 18: `livespiel-ui-2`;
    18 → 19: `neuer-manager`, `re-belastung`; 14 → 15: `abgleich-original`, `hok-export-nachpruefung`; 15 → 14: `prozess-effizienz` (+Nachprüfung)
  - Runde 11 → 12: `re-pruefung` (+Nachprüfung), `wettbewerbe-spiel-1`; 22 → 23: `leitstelle-erfahrung-r22`;
    23 → 24: `leitstelle-erfahrung-r23` (+Nachprüfung); 24 → 23: `feldspieler-sechs`
  - Runde 8 → 10: `leitstelle-zugriff` (+Nachprüfung), `re-saisonwechsel-dosbox` (nennen R10 und R11); 8 → 9: `playoffs-schneller-weg`; 7 → 8: `spielerwert-239`
  Die Probe ist ein Zahlenabgleich (je Fall Spurname im Archiv stichprobenartig gegengelesen: `pool`, `zusehen`, `neuer-manager`,
  `hok-export`, `re-pruefung`, `feldspieler-sechs`), kein Beweis jedes Einzelfalls. Die 26./27.11.-Stichprobe ist sauber: alle 10
  Dateien in `runde-26/` stehen in `runde-26.md` (#1030–#1054); die zwei vom 27.11. bleiben in `docs/reviews/`.
  Folge: `aufraeumen-verweise.md` (Zielpfade), alle Archivtexte mit neuem Pfad und die Aufräum-Nachweise stimmen dann mit zu verschieben.
- **P3-1 – Marker-Wortlaut (Hinweis für `aufraeumen-doku`).** Drei Marker in `restliste-v1.md` tragen alte Pfade im Wortlaut
  (A659 `re-nachtrag.md`, A686 `re-runde-14.md`, A754 `re-runde-12.md`; Zeilen 484, 514, 617). `aufraeumen-verweise.md` führt die
  Quellzeilen, warnt aber nicht, dass die Pfadänderung die ID-Schlüssel (Datei + Wortlaut) ändert; danach `markerliste.py`-Lauf
  (`aufraeumen-v1.md` §2.4/A6) einplanen. Kein Fehler dieses PR.
- **P3-2 – `runden-1-2`:** `docs/archiv/reviews/runde-3/2026-10-26-pr31-review.md` (#31) steht in `runden-1-2.md`; es gibt keinen Ordner für
  Runde 1–2. Verdacht, nur Hinweis (dort bewusst gesammelt?).

## 3. Ausgeführte Prüfungen

| Prüfung | Ergebnis |
|---|---|
| Kopf = Head, sauberer Baum, 2 Reviews der Runde 27 bleiben in `docs/reviews/` | ok |
| `git diff -M --name-status b0a548d a1f8fe7`: 305 × R100, 14 × R073–R099, 40 M, 1 A | wie gemeldet |
| 14 Renames einzeln (`git diff <base>:<alt> <head>:<neu>`) | nur Pfad-/Linkzeilen; `bestandsaufnahme.md` zusätzlich Archivvermerk (R073, dokumentiert); `treffer19.py`: nur Kommentarpfad |
| 284 Reviews im Archiv + 2 in `docs/reviews/` = 286 vor dem PR | vollständig |
| `funktionskarte.py <em.exe> --csv`, Base/Head | beide 679 Funktionen; genähert 3, unbekannt 227, dokumentiert 110, umgesetzt 339; alle Spalten außer `fundstellen` gleich (nur Pfad, 378 Zeilen) |
| `markerliste.py` (ohne `--write`), Base/Head | 367315 Bytes, `cmp` identisch |
| Linkprüfung `docs/**/*.md` (relative Links, Basename+Ziel verglichen) | 71 → 62 gebrochen, 0 neu, 9 behoben |
| Bleibende Analysen | `inventur.md`, `restliste-v1.md`, `-klassen.csv`, `abweichungen-v1.md`, `aufraeumen-v1.md`, `re-abdeckung.md` vorhanden |
| `scan_docs`-Änderung | eine Bedingung (`parts[1:3] != ("archiv","analyse")`) plus Docstring; minimal, hält die verschobenen Analysen in der Fundstellenkarte |
| Erwartet offen: Altpfade in `src/ tests/ tools/ AGENTS.md CLAUDE.md` | bestätigt; alle 206 Trefferzeilen stehen als `Datei:Zeile` in `aufraeumen-verweise.md` (Abgleich per Skript, 0 fehlend); `docs/analyse/live-spieltag.md`/`torszene-dauer.md` bleiben gültig |

Akzeptierte `[APPROX]`: keine neuen. Annahme (nicht geprüft): Dateiinhalt der 305 R100-Renames ist per Git-Hash identisch.
Ohne Originaldaten-Lauf `pruefen.sh` (entfällt laut Auftrag).
