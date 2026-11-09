# Werkzeuge für das Reverse Engineering von em.exe

Keine Originaldaten ins Repo: Die Skripte lesen `em.exe` aus dem Referenz-Repo (`tools/claude/fetch-reference.sh original`)
und schreiben nur Adressen, Größen und kurze Textauszüge.

| Skript | Zweck | Aufruf |
|---|---|---|
| `funktionskarte.py` | Funktionskarte aller Basissegmente und Overlays mit Abdeckungsstatus (umgesetzt/genähert/dokumentiert/unbekannt) aus den Fundstellen in `src/` und `docs/`; erneuert den erzeugten Block in `docs/re-abdeckung.md` | `python3 tools/reverse-engineering/funktionskarte.py ~/referenz/original/gamedata/em.exe --write` (ohne `--write`: Ausgabe auf stdout; `--csv <datei>` außerhalb des Repos: alle Funktionen) |
| `disasm.py` | Annotiertes Disassembly eines Bereichs (Funktionsköpfe mit Status und Aufrufern, aufgelöste Fern-/Nahaufrufe, Sprungziele, DGROUP-Texte) oder mit `--xref` alle Leser und Schreiber eines DGROUP-Bereichs im Spielcode | `python3 tools/reverse-engineering/disasm.py ~/referenz/original/gamedata/em.exe 0x51E90 0x57480 --cache <scratch>/an.pickle > <scratch>/ov130.asm`; `… --xref 0xA839 0xA846 [--imm]` (Ausgaben nie ins Repo) |

Voraussetzung: Python 3, `ndisasm` (Paket `nasm`). Für Einzelstellen weiterhin `ndisasm -b16 -e <offset> -o <offset> em.exe`
bzw. einen Ausschnitt (`dd`/Python) disassemblieren; Adressrechnung in `docs/reverse-engineering.md`.

Hinweise zu `disasm.py`:

- **Grenzen:** Im Listing ist die Obergrenze exklusiv (Adressen < `hi`), bei `--xref` inklusiv (`lo` ≤ Wert ≤ `hi`; das
  Beispiel `0xA839 0xA846` und Einzelwerte wie `0xA845 0xA845` brauchen das). Ein Listing umfasst eine Code-Region; reicht `hi` darüber hinaus, warnt das
  Skript auf stderr und endet am Regionsende.
- **`--xref` bei kleinen Werten:** erfasst jeden `[reg±0x…]`-Operanden, also auch Stack-Zugriffe wie `[bp+0x8]`. Kleine
  Bereiche (z. B. `0x8 0x8`) liefern deshalb Hunderte Treffer; sinnvoll sind DGROUP-Adressen ab etwa `0x100`.
- **Zwischenspeicher (`--cache`):** enthält auch den Abdeckungsstatus aus `src/` und `docs/`. Er ist an den SHA-256 von
  `em.exe`, von `funktionskarte.py` und `disasm.py` und an die Zeitstempel aller Fundstellen-Dateien gebunden (Datei `<cache>.key`); bei Abweichung wird er neu
  erzeugt. Es ist ein Pickle: nur im eigenen Scratch-Ordner anlegen, nie eine fremde Cache-Datei angeben.

Hinweis zu Fundstellen: Die Layout-Konstanten (u. a. 0x255C0 = Overlay-Basis) zählt die Karte nie als Adresse. Die
Funktion am Anfang von Ov 0xC0 (Datei 0x255C0) wird zwar auch über Aufruf, Prolog und Segmentanfang als *Funktion*
erkannt, zählt als *Fundstelle* aber nur über die Stub-Schreibweise, z. B. „Stub 0xC0 Einsprung 0x20“ (belegt in
PR #381); erkannt werden auch „Stub-Selektor“ sowie die Wörter „Funktionsoffset“, „Stub-Offset“, „Einsprung“ und „Offset“.
Eine Adresse mit direkt folgendem Doppelpunkt („Datei 0x3688F: …“) zählt als Adresse, solange kein Hexwert folgt
(sonst `Segment:Offset`). `docs/analyse/inventur.md` und `docs/re-abdeckung.md` zählt die Karte nicht als Fundstelle.
Offsets im entschlüsselten Spielstand schreibt die Doku als `HOK 0x…` (Beispiel `docs/analyse/re-runde-13.md`); die Karte zählt eine Adresse mit
direkt vorangehendem `HOK` nie als em.exe-Fundstelle (Runde 13, #567, R13RD-01). Ohne das Präfix würde jede Hexzahl einer
Spielstand-Auswertung als Dateioffset in `em.exe` gelten.
Das Präfix gilt nur für die Adresse direkt dahinter: **je Adresse ein `HOK`** (`HOK 0x08EED` / `HOK 0x08EF7`, nicht
`HOK 0x08EED` / `0x08EF7`; Aufzählungen wie „HOK 0x00844, 0x575C3“ zählen die zweite Adresse als em.exe-Fundstelle, #611, R13PK-04).
