# Review: PR #1028 – Abschluss Runde 25, Kopf Runde 26

**Geprüft:** Base `7dc2576`, Head `8965548` (`git rev-parse HEAD` = Head, `git status --porcelain` leer). Reviewer: Claude
(Sonnet 5.5), geplanter Lauf ohne Implementierungskontext. Nur Doku; `tools/pruefen.sh` entfällt (Auftrag).

## 1. Merge-Empfehlung

**Ja** für Head `8965548`. Keine P0–P2. Ein P3 (Wortlaut), kein Hinderungsgrund.

## 2. Befunde

- **P3-1 – Widerspruch zu #1024:** [`docs/archiv/runde-25.md` Z. 16](../archiv/runde-25.md) nennt „#1024 (Rest P3-2/P3-3)“ im Aufräumen,
  P3-3 (Wortlaut K13/B29) setzt dieser PR aber um (`abweichungen-v1.md` Abschnitt 0, Diff 2 Zeilen). Richtig: „Rest P3-2“.
  Beleg: `sed -n 16p docs/archiv/runde-25.md`; PR-Text: „P3-3 … umgesetzt“. Rein sprachlich.

Bestätigt, keine Befunde:

- **Archiv wörtlich:** Skript-Probe (Python): Abschnitt `## Aktuelle Runde: Runde 25` von `7dc2576:docs/arbeitspakete.md`
  (Zeilen 10–169, 160 Zeilen) gegen `docs/archiv/runde-25.md`: jede nichtleere Zeile kommt vor; Ausnahme nur die 19 Zeilen
  „Zurückgestellt“ und „Vormerkungen“, wie im Archivkopf vermerkt (bleiben in `arbeitspakete.md`, dort vorhanden).
- **Ergebnis/Abschluss/Entscheidungen gegen GitHub** (`gh api …/pulls/<nr>`): #996, #997, #998, #1001 (Nein, ein P2), #1002–#1006,
  #1008, #1009, #1013, #1015, #1017, #1022, #1023, #1025, #1027 alle `merged`; #1026 closed; #1018, #1019, #1024, #978, #980,
  #936 open – stimmt mit dem Abschluss überein. #1025 Titel „Gesamtreview Runde 25 – Schema-63-Migration beim Laden“ passt zu
  „Nein wegen R25-G1 → Fix #1027“.
- **Kopf Runde 26** (`docs/arbeitspakete.md` Z. 10–20): Auftrag (Live-Spieltag, #1018 SG-01 = A), Stand (V 0, D 26, E 319, K 190,
  Schema 65 – gleich dem Generatorblock), Danach (Aufräumen #936 mit #978, #980, #1019, #1024), Modellwahl, Meldung; Vormerkungen
  Live-Spieltag und Tagesabschluss (#1018) vollständig, übrige Vormerkungen erhalten. Verweise `archiv/runde-25.md` und
  `analyse/…` zeigen auf vorhandene Dateien.
- **Vier Klassenzeilen:** A102 (Rollenwert 0x18 → K11, `Player.RoleValue`, `MatchEngine.Statistics.cs:35`), A404 (HOK-Export
  Rollenwert, K11, `OriginalExport.Squads.cs:11` [ORIGINAL]), A486 (Verlassen der Aufstellung → B31, #1004), A907 (Strafbank-Abfrage
  → K2, #1004): zu entfallenen Markern umgesetzter Themen; `git grep '\[APPROX\]'` in `src/` findet keinen zugehörigen Marker;
  Generator meldet für alle vier „Klasse ohne Marker (entfallen)“. Zusatz: A463 → X, Marker lebt als A977 (E) weiter; A699 bleibt K.

## 3. Ausgeführte Prüfungen

| Prüfung | Ergebnis |
|---|---|
| `python3 tools/reverse-engineering/markerliste.py` (ohne `--write`), Head | Rückgabe 0; `git status --porcelain` danach leer → Generatorblock `restliste-v1.md` unverändert (222 Dateien, 535 Marker; V 0, D 26, E 319, K 190, X 0) |
| Archiv-Wörtlichkeitsprobe (Skript, s. oben) | bestanden |
| GitHub-Abgleich PRs/Issues | stimmt |
| Erwartet offen (nur bestätigt) | #1024 P3-2 (Aufräumen #936), #1019, #978, #980 offen; Runde 26 Live und #1018 offen |
| `tools/pruefen.sh` | nicht ausgeführt (nur Doku, laut Auftrag) |

**`[APPROX]`:** keine neuen, kein Codeeingriff. Originaldaten nicht geprüft.
