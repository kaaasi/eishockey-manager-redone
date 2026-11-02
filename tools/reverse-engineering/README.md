# Werkzeuge für das Reverse Engineering von em.exe

Keine Originaldaten ins Repo: Die Skripte lesen `em.exe` aus dem Referenz-Repo (`tools/claude/fetch-reference.sh original`)
und schreiben nur Adressen, Größen und kurze Textauszüge.

| Skript | Zweck | Aufruf |
|---|---|---|
| `funktionskarte.py` | Funktionskarte aller Basissegmente und Overlays mit Abdeckungsstatus (umgesetzt/genähert/dokumentiert/unbekannt) aus den Fundstellen in `src/` und `docs/`; erneuert den erzeugten Block in `docs/re-abdeckung.md` | `python3 tools/reverse-engineering/funktionskarte.py ~/referenz/original/gamedata/em.exe --write` (ohne `--write`: Ausgabe auf stdout; `--csv <datei>` außerhalb des Repos: alle Funktionen) |
| `disasm.py` | Annotiertes Disassembly eines Bereichs (Funktionsköpfe mit Status und Aufrufern, aufgelöste Fern-/Nahaufrufe, Sprungziele, DGROUP-Texte) oder mit `--xref` alle Leser und Schreiber eines DGROUP-Bereichs im Spielcode | `python3 tools/reverse-engineering/disasm.py ~/referenz/original/gamedata/em.exe 0x51E90 0x57480 --cache <scratch>/an.pickle > <scratch>/ov130.asm`; `… --xref 0xA839 0xA846 [--imm]` (Ausgaben nie ins Repo) |

Voraussetzung: Python 3, `ndisasm` (Paket `nasm`). Für Einzelstellen weiterhin `ndisasm -b16 -e <offset> -o <offset> em.exe`
bzw. einen Ausschnitt (`dd`/Python) disassemblieren; Adressrechnung in `docs/reverse-engineering.md`.
