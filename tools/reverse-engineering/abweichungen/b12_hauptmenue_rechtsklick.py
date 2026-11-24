"""B12 (A296): what the right mouse button does in the main menu of em.exe (Datei 0x44E22, loop 0x4535B–0x4592F).

Replays the original event loop (harness `hauptmenue.py`) for click sequences with the left (1) and the right (3)
button and prints where each click leads: "Ende" = the menu returns through Datei 0x4592F ([bp-7] = button that ended
it: 1/3 = "Tag weiter", 0 = a screen behind a menu entry was opened), "Schleife" = back to the loop head.
"Auswahl" = selected tile [bp-6] afterwards (0xffff = none). Calls into other overlays are listed (screens).
Usage: python3 b12_hauptmenue_rechtsklick.py [em.exe]
"""
import sys

import hauptmenue

TILE0 = (40, 100)      # button rectangle x 8–80, y 80–137 (read from the button table after the menu set-up)
ENTRY0 = (27, 220)     # first entry of the sub-menu, x 3–51, y 201–239
CASES = [
    ("Logo (Tag weiter)", [], (288, 48)),
    ("Kachel 1", [], TILE0),
    ("Kachel 1 erneut", [(1, *TILE0)], TILE0),
    ("Eintrag 1 nach Kachel 1", [(1, *TILE0)], ENTRY0),
    ("Leere Fläche nach Kachel 1", [(1, *TILE0)], (160, 160)),
    ("Kopfzeile y < 0x15 (Credits)", [], (160, 10)),
    ("Fläche x 0xE9–0x10F, y 0x22–0x47", [], (250, 40)),
    ("Gegnername x 0x8A–0xE6 (0xE1A6 = 5)", [], (176, 39)),
    ("Feld x 0x13E, y 0xEF", [], (0x13E, 0xEF)),
]


def screens(c, o):
    out = []
    for _, dst in o.calls:
        r = next((r for r in c.regions if r.start <= dst < r.end), None)
        if r is not None and r.stub is not None and r.stub not in (0x110, 0x120, 0x108):
            out.append(f"{dst:#07x} (Ov {r.stub:#x})")
        elif dst == 0x4D385:
            out.append("0x4d385 (Vereinsfenster)")
        elif dst == 0x4B5F4:
            out.append("0x4b5f4 (Ov 0x120, Meldungen)")
    return ", ".join(dict.fromkeys(out)) or "-"


def main():
    path = sys.argv[1] if len(sys.argv) > 1 else None
    print(f"{'Taste':6} {'Klick':34} {'Ergebnis':8} {'[bp-7]':6} {'Auswahl':7} {'0x1DB':5} Bildschirme")
    for name, before, (x, y) in CASES:
        for button, label in ((1, "links"), (3, "rechts")):
            top = (lambda cpu: cpu.sdb(0xE1A6, 5)) if "0xE1A6" in name else None
            c, outs = hauptmenue.run(before + [(button, x, y)], path=path, on_top=top)
            o = outs[-1]
            sel = "-" if o.selected is None else f"{o.selected:#x}"
            b7 = o.detail.split("= ")[-1] if o.detail else "-"
            print(f"{label:6} {name:34} {o.kind:8} {b7:6} {sel:7} {c.db(0x1DB):5} {screens(c, o)}")


if __name__ == "__main__":
    main()
