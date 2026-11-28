# Review PR #1076 – Spur aufraeumen-doku (Erstreview)

Reviewer: Claude (geplanter Lauf, frische Sitzung ohne Implementierungskontext, Sonnet 5.5) · Base `77719af` · Head `b87d0c5` ·
28.11.2026

## 1. Merge-Empfehlung

**Ja** für genau `b87d0c5`. Die Codeänderung ist belegt reiner Kommentartext (Normalisierungsprobe über alle 175 geänderten
`.cs`/`.py`/`.sh`-Dateien, 0 Abweichungen im Codeteil), keine neu gebrochenen Links, keine Marker-ID verloren, Statusverteilung der
Funktionskarte gleich, `markerliste.py` Rückgabe 0 und Arbeitsbaum danach sauber, #1075 offen. Nur Doku-P3, kein Merge-Hindernis.
**Einschränkung:** In meiner Umgebung gibt es kein .NET-SDK; die Testtabelle stammt aus dem Kommentar der Leitstelle (drei Läufe
auf `b87d0c5`, 4084/0/0, 2813/0/674, Pflichtlauf rot wie gewollt), kein eigener Lauf. Die Aussage trägt, weil der Codeteil
unverändert ist (siehe 3).

## 2. Befunde

| Nr | Schwere | Befund |
|---|---|---|
| P3-1 | P3 | Alte Pfade der verschobenen Doku stehen noch in Klassentexten der Restliste: [`restliste-v1.md:564`](https://github.com/kaaasi/eishockey-manager-redone/blob/b87d0c5/docs/analyse/restliste-v1.md#L564) und `restliste-v1-klassen.csv:511` (A754, `docs/analyse/re-runde-12.md`), `…csv:3` (A002, `docs/analyse/re-vor-dosbox.md`), `…csv:723` (F018, `docs/analyse/belastung-staerke.md:353`). Kein Link, kein Marker-Schlüssel (A754-Wortlaut im Code ist `re-runde-12.md`, ID stabil), aber der Pfad führt ins Leere. |
| P3-2 | P3 | [`inventur.md:342`](https://github.com/kaaasi/eishockey-manager-redone/blob/b87d0c5/docs/analyse/inventur.md#L342) nennt `OriginalPairsTests.Strain730_DeviatesAtMostSeeds`; die Methode gibt es nicht (heißt [`Strain_DeviatesAtMostSeeds_DriftGuard`](https://github.com/kaaasi/eishockey-manager-redone/blob/b87d0c5/tests/EishockeyManager.Core.Tests/Compare/OriginalPairsTests.cs#L302)). Schon auf Base mit alter Klasse veraltet, der PR zog nur den Klassennamen nach. |
| P3-3 | P3 | `docs/transfer.md` (Spur-Teil „Verträge komplett“) nennt `Core/SaveGame.Migration.TransferVertraegeKomplett5.cs` als Umsetzung; Datei entfällt seit A1 = B. Anders als in `spielstand*.md` fehlt dort der Herkunftshinweis. |

Belege: P3-1 `git grep -nE 'docs/analyse/(re-|abgleich|belastung)' -- docs/analyse/restliste-v1-klassen.csv docs/analyse/restliste-v1.md`;
P3-2 `grep -n 'Strain730' -r tests` (leer) und `grep -n 'public void Strain_' tests/…/OriginalPairsTests.cs`; P3-3
`ls src/EishockeyManager.Core/SaveGame.Migration*` (leer). Gleiche Probe für alle in den geänderten Doku-Zeilen genannten Namen:
66 Testklassen, 15 `Core/…`-Pfade, 36 `Klasse.Mitglied` – nur P3-2/P3-3 und die Kurzformen `R204`/`PR3501` (Präfixe von
Methodennamen, in Ordnung) fanden sich nicht.

## 3. Ausgeführte Prüfungen (Gegenbeispiele)

Checkout Base und Head je als Worktree, `git rev-parse HEAD` = `b87d0c5…`, `git status --porcelain` leer.

| Prüfung | Ergebnis |
|---|---|
| `git diff --stat 77719af b87d0c5` | 208 Dateien, +642/−754; 176 ohne `.md`: 175 Code/Skript + 1 CSV |
| Codeprobe: je Zeile Base/Head vergleichen, Pfad (`docs/…md`, `AGENTS.md`) normalisieren, Teil vor `//`/`#` gleich | 0 Abweichungen; keine Zeilenzahländerung; jede geänderte Zeile enthält einen Kommentar |
| Linkprüfung aller relativen `](…)` in allen `*.md` (ohne Codeblöcke) | Base 88 tote, Head 57; **neu gebrochen 0**, 31 repariert; Rest nur Platzhalter `H#L…`, `archiv/…` in archivierten Reviews (wie Base) |
| Alte Pfade aus `aufraeumen-verweise.md` (255 Zeilen, Base) in `src/ tests/ tools/ AGENTS.md .github/` | 0 Treffer; `docs/analyse/aufraeumen-verweise.md` im Head entfernt |
| Alte Reviewpfade `docs/reviews/<verschobene Datei>` | 0 außerhalb des Archivs (im Archiv historisch) |
| `python3 tools/reverse-engineering/markerliste.py` auf Head | Rückgabe 0, danach `git status` sauber (Liste aktuell); Meldungen nur „Klasse ohne Marker (entfallen)“ |
| Markerdiff Base/Head (Tabelle `restliste-v1.md`, IDs) | 1081 → 1082, **keine ID verloren**, neu nur A1035 (`Tools/Scenarios/PixelVerein.cs:92`, `[APPROX]`, Klasse E); CSV gleich (+A1035). Verlorene Spuren-Listen #1059/#1064: keine |
| A659/A686 (Wortlaut mit Pfad) | Zeilen tragen `docs/archiv/analyse/…`, ID und Klasse unverändert; A754 Wortlaut unverändert |
| `funktionskarte.py em.exe --repo . --csv` Base/Head | je 679 Zeilen, Datei/Bereich/Offset/Größe/Status je Zeile gleich; umgesetzt 339, genähert 3, dokumentiert 110, unbekannt 227 |
| #1075 | offen, Label `befund`/`P3`, beschreibt `wettbewerbe.md:234` gegen `MatchEngine.Minutes.cs`/`PowerPlayCountersTests`; im PR nicht angeglichen (bestätigt) |

**Besonders geprüft**
- Themendokus D5: Stichproben der Namen siehe oben (66/15/36 Treffer geprüft); Widerspruch nur #1075.
- #980 P3-1: Schlüssel von A659/A686 nachgezogen (s. o.). P3-2: `abgleich-dosbox-22 §3 (A057)`, `§5 (A614)` stimmen mit den
  Überschriften der Datei (`## 3. A057`, `## 5. A614`) überein. P3-4: nur Vermerk, keine Dateiänderung (Diff bestätigt).
- #1024 P3-2: Nachtrag in `abweichungen-v1.md` mit 57 IDs, alle in der CSV vorhanden und Klasse E/K; 13 davon stehen oben nur
  im Fließtext (Verweis „Reste … als Marker“), nicht als Thema – vertretbar. Ob die 55 älteren E/K-IDs ohne Thema (z. B. A386)
  schon vor dem PR so waren, habe ich nicht geprüft.
- K13: A973-Text nennt jetzt `_salaryZero`; im Code [`ContractDialog.cs:64,163,188`](https://github.com/kaaasi/eishockey-manager-redone/blob/b87d0c5/src/EishockeyManager.UI/Screens/Team/ContractDialog.cs#L188) bestätigt (zeigt 0 DM, Kern bekommt 1).
- W1: `tools/markerbericht.py` existiert, Aufruf in `.github/workflows/build.yml:62`; README-Zeile stimmt.
- Klassen der Restliste auf neue Dateinamen: Fundorte in `restliste-v1.md` (z. B. A895 → `Manager.Season.cs`, A966 →
  `OriginalExport.Finance.cs`) existieren; keine Datei der Marker-Fundorte fehlt (`markerliste.py` Rückgabe 0).

**Akzeptierte `[APPROX]`:** A1035 (Szenario ernennt Bundestrainer am ersten WM-Tag, wie A741 begründet). Keine weitere neue.
**Annahmen:** Testtabelle der Leitstelle übernommen (kein SDK hier); Originaldaten-Lauf nicht von mir ausgeführt.
