# Review: Spur `marker-werkzeug`, Teile 1 und 2 (#422, #423)

Reviewer: Claude Code (geplanter Lauf, Claude Opus 5.5), eigene Sitzung ohne Implementierungskontext (die Umsetzung kam
aus Sitzung `session_01GhACvVV1kVkKVBa9rCMcz9`). Datum: 02.11.2026.

| PR | Base | Head |
|---|---|---|
| #422 | `7332a0fd04628e1a63acde30d9131ecb2f2d2afb` (= `main`) | `92d83c6c5eeb7c4be9397f91a5f9bf0690236790` |
| #423 | `92d83c6c5eeb7c4be9397f91a5f9bf0690236790` (Branch von #422) | `8a92ddf49bdf3f241599562a95b9a66c3206fcc4` |

## 1. Merge-Empfehlung

- **#422: ja** für genau `92d83c6`. Gegenbeispiel R10N-01 ist behoben (Mutation ohne Statuswechsel), `inventur.md`
  zählt nicht mehr als Fundstelle (#406, mit Gegenprobe), R-1 bis R-4 und R-6 sind in der Sache behoben. Die
  Änderung der Statuszahlen in `docs/re-abdeckung.md` (umgesetzt 202 → 210, dokumentiert 212 → 204) ist genau durch
  die 8 Funktionen mit `[ORIGINAL] 0x…:` in `src/` begründet; keine andere Funktion wechselt den Status. Offen bleiben
  nur P3-Befunde (R11M-01, R11M-02).
- **#423: ja** für genau `8a92ddf`, nach #422 (gestapelt; Base vor dem Merge auf `main` umstellen). R10A-01 bis -03,
  die beiden Hinweise außerhalb des Diffs und #363 sind in der Sache behoben; ein P3-Restwortlaut (R11M-03).
- Beide Heads gebaut und mit Originaldaten getestet (Teil 3); Nachweis nach `zusammenarbeit.md` §6 Nr. 4 liegt damit vor.
- Erwartet offen bestätigt: **Teil 3** (Marker ohne Codeänderung) ist nicht enthalten und laut `arbeitspakete.md`
  ausdrücklich erst nach der Integration vorgesehen.

## 2. Befunde

| ID | P | PR | Kurz |
|---|---|---|---|
| R11M-01 | P3 | #422 | Cache-Schlüssel ohne Werkzeugstand: nach Änderung von `funktionskarte.py` bleibt der Status still alt |
| R11M-02 | P3 | #422 | README verweist auf ein Beispiel `0xA845 0xA845`, das es dort nicht gibt |
| R11M-03 | P3 | #423 | `inventur.md` Tabelle 2: „Karte erkennt ihn nicht“ ist nach #422 überholt |

**R11M-01 (P3).** [`disasm.py` `cache_key` Z. 35–42](https://github.com/kaaasi/eishockey-manager-redone/blob/92d83c6c5eeb7c4be9397f91a5f9bf0690236790/tools/reverse-engineering/disasm.py#L35-L42)
bindet den Zwischenspeicher an `em.exe` und die Fundstellen-Dateien, nicht an `funktionskarte.py`/`disasm.py` selbst.
Ändert sich die Erkennung (wie in genau diesem PR die `BARE`-Regel), liefert ein vorhandener Cache den alten Status.
Gegenbeispiel: Cache mit Head-Werkzeug erzeugen (`disasm.py em.exe 0x3688F 0x36890 --repo <Head> --cache c3.pickle` →
`Status umgesetzt`); Kopie der Werkzeuge mit der alten Regel `BARE … (?![\w:])` mit demselben Repo und Cache →
weiter `umgesetzt`, ohne Cache → `dokumentiert`. Selten (Cache liegt im eigenen Scratch-Ordner), aber still falsch.
Vorschlag: SHA-256 von `funktionskarte.py` und `disasm.py` in den Schlüssel aufnehmen, oder in der README „nach Änderungen
an den Werkzeugen Cache löschen“.

**R11M-02 (P3).** [README Z. 16–17](https://github.com/kaaasi/eishockey-manager-redone/blob/92d83c6c5eeb7c4be9397f91a5f9bf0690236790/tools/reverse-engineering/README.md#L16-L17):
„die Beispiele `0xA845 0xA845` brauchen das“ – das README-Beispiel ist `--xref 0xA839 0xA846` (Z. 9); `0xA845 0xA845`
steht nur im Review `2026-11-02-re-werkzeuge.md`. Sachlich stimmt die Grenze (inklusiv, Code Z. 120/123). Vorschlag:
„das Beispiel `0xA839 0xA846` und Einzelwerte wie `0xA845 0xA845` brauchen das“.

**R11M-03 (P3).** [`inventur.md` Tabelle 2](https://github.com/kaaasi/eishockey-manager-redone/blob/8a92ddf49bdf3f241599562a95b9a66c3206fcc4/docs/analyse/inventur.md#L682)
F016/F069/F070/F080: „vorhanden mit `[ORIGINAL]` (Karte erkennt ihn nicht, R10N-01)“ – mit #422 erkennt die Karte sie
(CSV am Head: alle vier `umgesetzt`); P3 Nr. 1 (Z. 161–165) sagt richtig „in Runde 11 Teil 1 behoben“. Zudem steht in
der Remake-Spalte von F016/F069/F070 weiter „ohne Datei-Präfix“, die von R10A-01 als widerlegt genannte Ursache.
Die Tabelle ist als Stand `2d0b081` gekennzeichnet, daher nur Wortlaut. Vorschlag: „(Karte erkannte ihn bis Runde 11
Teil 1 nicht, R10N-01)“, „ohne Datei-Präfix“ streichen – kann mit Teil 3 erfolgen.

**Hinweis (kein Befund):** #423 ändert Kommentarzeilen in `Core/MatchEngine.Live.cs` (Zuständigkeit `wettbewerbe-spiel`).
Das Paket nennt #363 ausdrücklich als Teil 2, der PR benennt die fremde Datei; `git merge-tree pr/420 pr/423` (Head
`38accc1` von #420) ist konfliktfrei. Entscheidung über die Reihenfolge bei der Leitstelle.

### Geprüft ohne Befund

- **R10N-01 (#401):** neue Regel `BARE … (?![\w]|:[0-9A-Fa-f])` ([Z. 417](https://github.com/kaaasi/eishockey-manager-redone/blob/92d83c6c5eeb7c4be9397f91a5f9bf0690236790/tools/reverse-engineering/funktionskarte.py#L417));
  `seg:off` (Hexwert nach dem Doppelpunkt) bleibt ausgeschlossen und läuft weiter über `SEGOFF`.
- **Statuszahlen `re-abdeckung.md`:** Statusvergleich `--csv` Base ↔ #422: genau 8 Wechsel dokumentiert → umgesetzt
  (0x0A3F7, 0x0B954, 0x346D5, 0x347EF, 0x350B6, 0x3688F, 0x3EB30, 0x581C2), je mit neuer Fundstelle in `src/`
  (z. B. `MatchEngine.Live.cs:292` „[ORIGINAL] 0xA3F7:“, `FinanceScreen.cs:107` „em.exe 0x58304:“ innerhalb 0x581C2).
  Fundstellen: 0 verloren außer `inventur.md`, 102 Funktionen mit neu gezählten Doppelpunkt-Fundstellen; Stichproben
  (6) sind echte Adressen. Keine Funktion hatte `inventur.md` als einzige Fundstelle (Base: 248 Funktionen mit
  `inventur.md`-Fundstelle). Zuordnungen 4112 → 3741 = entfallene `inventur.md`-Einträge abzüglich der neuen
  Doppelpunkt-Fundstellen. #422 ↔ #423: keine Statusänderung.
- **R-1:** Cache wird bei anderem Fundstellen-Stand verworfen (Base: `dokumentiert` trotz Kopie; Head: `unbekannt` wie
  ohne Cache). **R-2:** Schlüssel wird vor `pickle.loads` geprüft, README-Hinweis vorhanden. **R-3:** README-Satz zu
  Stack-Operanden vorhanden (`--xref 0x8 0x8`: 735 Zeilen, unverändert – Doku-Fix wie vorgeschlagen). **R-4a:** Grenzen
  in Kopf und README. **R-4b:** `0x27F80 0x28040` → Warnung „hinter dem Ende der Region (0x27f8b)“ auf stderr.
  **R-4c:** `| head -2` → Rückgabe 0, stderr leer (Base: Traceback). **R-6:** Formulierung Funktion/Fundstelle und
  weitere Stub-Wörter ergänzt.
- **R10A-01:** F016/F069/F070/F080 umgestellt, P3 Nr. 1 eingeschränkt (Rest R11M-03). **R10A-02:** Kopfsatz nennt
  Status und Links; „#398 (Doku-Teil)“, „#401 (Doku-Teil)“; Status `inventur` mit „Code-Teil von R10N-01 in Runde 11“.
  **R10A-03:** „altes Platzfeld“. **Hinweise:** `re-abdeckung.md` „Einordnung“ 0x2FC2F und 0x07089 laut CSV am Head
  `umgesetzt` (0x07089 zusätzlich `genähert` in `MatchEngine.Injuries.cs:123`, wie beschrieben); `reverse-engineering.md`
  FILE13-Beleg #389.
- **#363:** Kommentare in `GameOptions.cs` und `PlayWithEvents` stimmen mit `CupMatchUsesMinuteLoop` (Play-offs/
  Aufstiegsrunde über `ReportKind.Results`, Index 1/4/7) und dem Rumpf (`WorldCup && knockOut` → schneller Weg) überein.
- **Dateigrenzen:** #422 nur `tools/reverse-engineering/*`, `docs/re-abdeckung.md` (erzeugt), eigener Status in
  `arbeitspakete.md`; #423 die im Paket genannten Doku-Dateien, eigener Status, zwei Kommentarblöcke in `src/` (siehe
  Hinweis). Keine Originaldaten oder Auszüge im Diff.

## 3. Ausgeführte Prüfungen

Checkouts: Klon, `git rev-parse HEAD` = Head, `git status --porcelain` leer (je eigener Worktree), `gamedata/` als
Verknüpfung auf `~/referenz/original/gamedata` (Referenz-Repo, `git ls-remote` erreichbar). SDK 10.0.402.

| Prüfung | Befehl / Ablauf | Ergebnis |
|---|---|---|
| R10N-01 | `perl -pi -e 's/(0x[0-9A-Fa-f]{4,5}):(?=[^0-9A-Fa-fx]\|$)/$1 :/g'` auf alle `src/**/*.cs` einer Kopie, `funktionskarte.py --csv` mit/ohne Mutation | Base: 8 Statuswechsel; #422 und #423: CSV identisch (0 Zeilen Diff) |
| #406 | Status-/Fundstellenvergleich Base ↔ #422; Gegenprobe: `Datei 0x020CA:` an `inventur.md` angehängt → `unbekannt`; dieselbe Datei als `inventur2.md` → `dokumentiert` | Ausschluss wirkt nur für `inventur.md` |
| `--write` | `funktionskarte.py --repo . --write` am Head von #422 und #423 | jeweils kein Diff |
| Gegenprobe `BARE` | alte Regel in einer Werkzeugkopie → 0x3688F `dokumentiert` | Änderung ist wirksam |
| #414 R-1, R-4b/c, R-3 | wie in Teil 2, je Base und Head | behoben bzw. dokumentiert |
| Konflikt | `git merge-tree --write-tree pr/420 pr/423` | konfliktfrei |
| CI | Check-Runs `build` auf `92d83c6` und `8a92ddf` | success (`publish` skipped) |

`tools/pruefen.sh` (alle drei Läufe) am Head von #423 (`8a92ddf`):

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1631 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1351 | 0 | 251 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1351 | 280 | 0 |

`tools/pruefen.sh --schnell` am Head von #422 (`92d83c6`):

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1631 | 0 | 0 |

**Akzeptierte `[APPROX]`:** keine neuen. **Annahmen:** Die 102 neu gezählten Doppelpunkt-Fundstellen sind nur
stichprobenhaft (6) geprüft; sie ändern keinen Status. `disasm.py`-Listings und Zwischenspeicher nur im Scratch-Ordner.
