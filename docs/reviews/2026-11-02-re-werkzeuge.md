# Review: RE-Werkzeuge (#409, #410)

Reviewer: Claude Code (Claude Sonnet 5.5), frische Sitzung ohne Beteiligung an beiden PRs.
Geprüft: #409 Base `162e0d1` · Head `dc2a308ac7ff9f8dfa22d813d7b068789223ce81` und #410 Base `162e0d1` · Head `7687f743f9860f9baac8b597f4518df420454f6f`.
Art der Prüfung: **Ausführung an nachgewiesenem Checkout** (kein „nur Codelesen“).

## 1. Merge-Empfehlung

- **#409: ja** für genau `dc2a308`. Eine Zeile Doku, Aussage am Original reproduziert; nur ein Formulierungsbefund (P3).
- **#410: ja** für genau `7687f74`, mit einem Prozessvorbehalt (siehe R-5): Der Übergabekommentar nennt den PR als Draft,
  „weil der Nutzer nur das Speichern der Techniken bestätigt hat, nicht die Aufnahme ins Repo“. Der PR ist aber kein
  Draft. Ob die Aufnahme gewollt ist, entscheidet Leitstelle/Nutzer – technisch steht nichts entgegen. Offene Befunde
  sind ausschließlich P3.
- **Gemeinsam:** Die README-Änderungen vertragen sich (siehe 3). Reihenfolge egal.

## 2. Befunde

| ID | Prio | PR | Befund |
|---|---|---|---|
| R-1 | P3 | #410 | Cache nicht an Stand gebunden: [`disasm.py` Z. 27–37](https://github.com/kaaasi/eishockey-manager-redone/blob/7687f743f9860f9baac8b597f4518df420454f6f/tools/reverse-engineering/disasm.py#L27-L37) |
| R-2 | P3 | #410 | `pickle.loads` auf einer Cache-Datei: [Z. 29](https://github.com/kaaasi/eishockey-manager-redone/blob/7687f743f9860f9baac8b597f4518df420454f6f/tools/reverse-engineering/disasm.py#L29) |
| R-3 | P3 | #410 | `--xref` zählt bp-Rahmenzugriffe als DGROUP: [Z. 91–95](https://github.com/kaaasi/eishockey-manager-redone/blob/7687f743f9860f9baac8b597f4518df420454f6f/tools/reverse-engineering/disasm.py#L91-L95) |
| R-4 | P3 | #410 | Randverhalten: Bereichsende, Regionsgrenze, Broken Pipe |
| R-5 | P3 | #410 | Prozess: Draft-Angabe im Übergabekommentar und Prüftabelle des PR |
| R-6 | P3 | #409 | Formulierung „nur über den Stub erkannt“: [README Z. 13–15](https://github.com/kaaasi/eishockey-manager-redone/blob/dc2a308ac7ff9f8dfa22d813d7b068789223ce81/tools/reverse-engineering/README.md#L13-L15) |

**R-1 (P3).** Der Zwischenspeicher (`--cache`) enthält auch den Abdeckungsstatus aus `src/` und `docs/`, wird aber weder an
`em.exe` noch an den Stand der Fundstellen gebunden. Gegenbeispiel: Cache mit dem Repo-Stand erzeugen, danach dieselbe
Abfrage mit `--repo <Kopie ohne Stub-Schreibweise in re-rest.md>` und demselben Cache:
`disasm.py em.exe 0x255C0 0x255C8 --cache c.pickle` → `Status dokumentiert`; mit `--repo <Kopie>` und Cache weiterhin
`dokumentiert`, ohne Cache `unbekannt`. Der Status im Kopf ist dann still falsch. Vorschlag: Cache-Schlüssel aus
SHA-256 von `em.exe` plus Zeitstempeln der gescannten Dateien, oder in der README „Cache nach Änderungen an `src/`/`docs/`
löschen“.

**R-2 (P3).** `load()` entpickelt jede Datei, die `--cache` nennt; eine untergeschobene Cache-Datei führt Code aus. Im
Scratch-Ordner des Nutzers vertretbar, einen Satz in der README oder JSON statt Pickle wert.

**R-3 (P3).** `fk.MEM` erfasst jeden `[reg±0x…]`-Operanden, also auch `[bp+0x8]` (Parameter, Stack). Für Adressen wie
0xA845 ohne Wirkung; bei kleinen Bereichen entsteht Rauschen: `--xref 0x8 0x8` liefert Hunderte Treffer
(Häufigste: `mov al,[bp+0x8]` 262×, `mov dl,[bp+0x8]` 81×). Die README sagt „DGROUP-Bereich“; ein Satz „kleine Werte
treffen auch Stack-Operanden“ genügt.

**R-4 (P3).** (a) Obergrenze ist im Listing exklusiv (`addr >= hi`, Z. 58), in `--xref` inklusiv (`lo <= v <= hi`, Z. 95);
aus dem Code gelesen, nicht gesondert ausgeführt. Die README-Beispiele (`0xA845 0xA845`) setzen die inklusive Lesart voraus.
(b) Das Listing bestimmt die Region nur aus `lo` (Z. 52); reicht `hi` in die nächste Region, endet die Ausgabe ohne
Hinweis. (c) `… | head` beendet das Skript mit `BrokenPipeError`-Traceback (beobachtet).

**R-5 (P3, Prozess).** Der Übergabekommentar (#410) nennt Draft und eine offene Nutzerentscheidung; die API meldet
`draft: false`. Die PR-Beschreibung hat eine leere Testtabelle; der Nachweis steht jetzt in 3. Die Entscheidung über die
Aufnahme ins Repo liegt bei Leitstelle/Nutzer.

**R-6 (P3).** Die README sagt, die Funktion bei 0x255C0 werde „nur über den Stub erkannt“. Als *Funktion* erkennt die
Karte sie auch über Aufruf, Prolog und Segmentanfang (CSV: `Aufruf+Prolog+Segmentanfang+Stub`); nur als *Fundstelle* zählt
allein die Stub-Schreibweise. Besser: „wird nur über die Stub-Schreibweise als Fundstelle gezählt“. Außerdem nennt der
Satz eine einzige Schreibweise; [`STUBREF`](https://github.com/kaaasi/eishockey-manager-redone/blob/dc2a308ac7ff9f8dfa22d813d7b068789223ce81/tools/reverse-engineering/funktionskarte.py#L418)
akzeptiert auch „Stub-Selektor“ sowie „Funktionsoffset“, „Stub-Offset“ und „Offset“.

## 3. Ausgeführte Prüfungen

**Stand:** beide Köpfe als getrennte Worktrees; `git rev-parse HEAD` = genannter Head, `git status --porcelain` leer
(`gamedata/` ist ignoriert, verlinkt auf die Originaldateien). `em.exe` aus `kaaasi/eishockey-manager-referenz`
(`original/gamedata/em.exe`, 387 968 Bytes). SDK 10.0.402.

**Gegenbeispiele**

| Gegenbeispiel | Ablauf | Ergebnis |
|---|---|---|
| #409 „Datei 0x255C0“ ohne/mit Stub-Schreibweise | `funktionskarte.py em.exe --repo . --csv …` am Head; dann Kopie, in der `docs/analyse/re-rest.md` die Schreibweise „(Stub 0xC0 Einsprung 0x20 …)“ verliert (übrig bleibt „**Datei 0x255C0**“) | mit: `0x255C0` **dokumentiert** (Fundstellen `re-rest.md:23`, `:114`); ohne: **unbekannt**. Aussage der README bestätigt; „belegt in #381“ stimmt (#381 gemergt, Runde 10 re-rest). |
| #410 `disasm.py em.exe 0x51F3D 0x51FEA` | Aufruf wie genannt | Kopf `0x51f3d Größe 173 Status dokumentiert Stub 0x130:0x25 Aufrufer 0x54c9f`, Fernaufrufe aufgelöst; identisch zur Karte. |
| #410 `--xref 0xA845 0xA845` | Aufruf wie genannt | `0x359DC` Schreiber (`mov [bx-0x57bb],al`), `0x52EFE` Leser; **zusätzlich Leser `0x359BA`** (`mov al,[bx-0x57bb]`). `--imm` liefert 3 Zeilen. Der PR nennt `0x359BA` nicht; kein Widerspruch. |

**Kopf gegen Karte (Schwerpunkt):** gesamte Overlay-Region `0x51E90–0x57480` (README-Beispiel) mit `disasm.py` erzeugt und
Kopfzeile für Kopfzeile mit `funktionskarte.py --csv` verglichen (Größe, Status, Stub-Eintrag, Aufrufer): **29 von 29
Funktionsköpfen gleich, 0 Abweichungen**; die CSV enthält im Bereich ebenfalls genau 29 Funktionen.

**README-Verträglichkeit:** Beide PRs ändern verschiedene Stellen (#410: Tabellenzeile; #409: Absatz am Dateiende).
`git merge-tree` #409 → #410 gegen die Base und gegen den aktuellen `main` (`2526698`, seit der Base nur Docs aus #408,
keine der drei Dateien betroffen): sauber, ohne Konflikt. Das zusammengeführte README gelesen: Tabelle mit beiden Skripten,
darunter „Voraussetzung“ und der neue Hinweis; inhaltlich widerspruchsfrei. `re-rest.md` auf `main` enthält die
Stub-Schreibweise weiterhin (2×). Ein Integrationslauf nach dem Merge wurde nicht gemacht (kein `src/`-Code betroffen).

**Weitere:** `referenz-videos.md` (#410): `git show HEAD:oberflaeche/oberflaeche_0000.png` im Sparse-`blob:none`-Klon liefert
das Bild (57 176 Bytes) ohne Auschecken der Reihe; `git cat-file -e` auf einen fehlenden Pfad meldet „does not exist“. Der
Hinweis stimmt. Die Ausgabe von `disasm.py` enthält Originalcode-Bytes; die Doku sagt „nie ins Repo“, Ausgaben lagen im
Scratch. CI auf beiden Köpfen: `build` success, `publish` skipped.

**Testtabelle `tools/pruefen.sh` (alle drei Läufe, je Head einzeln)**

| Head | Mit Originaldaten | Ohne Originaldaten | Pflichtlauf ohne Daten (scheitert wie gewollt) | Ergebnis |
|---|---|---|---|---|
| `dc2a308` (#409) | 1631 / 0 / 0 | 1351 / 0 / 251 | 1351 / 280 / 0 | bestanden |
| `7687f74` (#410) | 1631 / 0 / 0 | 1351 / 0 / 251 | 1351 / 280 / 0 | bestanden |

(Format bestanden / fehlgeschlagen / übersprungen.) Gleiche Zahlen bei beiden Köpfen sind erwartet: keiner berührt `src/`
oder `tests/`.

**Gegenprobe:** Keiner der PRs bringt einen Test mit; es gibt keine Testabdeckung für `funktionskarte.py` und
`disasm.py` (nicht in `tests/`, `.github/`, `tools/pruefen.sh`). Als Gegenprobe zu #409 dient die Variante ohne
Stub-Schreibweise oben. Eine Mutation am Skript wäre ohne Test nicht „rot“ geworden.

**Akzeptierte `[APPROX]`:** keine (kein Code unter `src/` geändert). **Annahmen:** Bedeutung der Befehle, die `ndisasm`
liefert, und die Richtigkeit der Funktionsgrenzen sind aus `funktionskarte.py` übernommen und nicht neu belegt; geprüft
wurde nur die Übereinstimmung von `disasm.py` mit der Karte.
