"""A899: field player into the goal line – lineup click 0x40364 (Ov 0x108) from Datei 0x40DAC to the redraw 0x40806.

Real code: Datei 0x40DAC–0x41532 (click on a squad player with a line selected) and the role function 0x04180
(pool +0x18 / 25, capped at 2: 0 goalkeeper, 1 defender, 2 forward). Prepared: manager 0 (DGROUP 0xA70E), squad place 3
of record 0 (DGROUP 0x5DC3 + 0x34 · 3; +0x0A dressed = 1, +0x15 = 0, +0x0F = pool place 1), the pool (far DGROUP 0x8253,
+0x18 = role value), the selected line [bp-0x9] = 4 (goal line), the mouse event DGROUP 0x5A16 (1 = left, 3 = right
button, CreditsScreen.cs) and the in-match value [bp+0x6] (0 = outside a match, 1 = in the match, DGROUP 0x2D6 + manager).
The lineup grid [bp-0x46] (5 lines × 6 cells; the placing search of the goal line looks only at cell 4 = [bp-0x2A],
Datei 0x412A1–0x413B0) is empty or holds a goalkeeper in that cell. A counter check puts a goalkeeper into line index 0
(defence of line 1). The squad place is printed when the click takes the player out of the team (+0x0A = 0). Stubbed and printed: the message box 0x0C0B8 (text of its two
pointers) and the placing routine 0x41533 (line and cell); everything else outside the range is stubbed silently.
Usage: python3 a899_tor.py [em.exe]
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "abweichungen"))
import emu16  # noqa: E402

PATH = sys.argv[1] if len(sys.argv) > 1 else None
POOL = 0x1000
PLACE = 0x5DC3 + 0x34 * 3
ROLES = {"Verteidiger": 35, "Stürmer": 80}
GOALIE = 10
BUTTONS = {"links": 1, "rechts": 3}
GRIDS = {"Tor leer": (0, 0), "Torhüter im Tor": (1, 0)}


def run(role, button, in_match, grid, line=4):
    c = emu16.load(PATH)
    c.allowed = [(0x40DAC, 0x41533), (0x04180, 0x041AC)]
    bp = 0xF000
    c.r["bp"], c.r["sp"] = bp, bp - 0x200
    ss = c.s["ss"]
    c.sdb(0xA70E, 0)
    c.sdb(0x375, 0)
    c.sdw(0x5A16, button)
    c.sdw(0x8253, POOL)
    c.sdw(0x8255, emu16.FAR_HEAP_SEG)
    c.wb(emu16.FAR_HEAP_SEG, POOL + 0x21 * 1 + 0x18, role)
    for k in range(0x34):
        c.sdb(PLACE + k, 0)
    c.sdb(PLACE + 0x0A, 1)
    c.sdb(PLACE + 0x0F, 1)
    c.ww(ss, bp - 0x20, PLACE)
    c.ww(ss, bp - 0x1E, emu16.DGROUP)
    c.wb(ss, bp + 0x6, in_match)
    c.wb(ss, bp - 0x9, line)
    c.wb(ss, bp - 0xE, 0)
    for k in range(5 * 6):
        c.wb(ss, bp - 0x46 + k, 0)
    c.wb(ss, bp - 0x46 + 4 * 6 + 4, grid[0])
    c.wb(ss, bp - 0x46 + 4 * 6 + 5, grid[1])
    out = []

    def message(cc):
        texts = [cc.cstr(cc.arg(1), cc.arg(0)), cc.cstr(cc.arg(3), cc.arg(2))]
        out.append(f"Meldung „{texts[0]} {texts[1]}“")
        return 1

    def place(cc):
        line_, cell, value = cc.arg(2) & 0xFF, cc.arg(6), cc.arg(9) & 0xFF
        out.append(f"aufgestellt: Reihe {line_} Zelle {cell} (Kennwert {value})")
        raise emu16.Stop("placed")

    c.hooks[0x0C0B8] = message
    c.hooks[0x41533] = place
    c.stop_at = {0x40806}
    c.s["cs"], c.ip = c.at(0x40DAC)
    try:
        c.run()
    except emu16.Stop:
        pass
    if c.db(PLACE + 0x0A) != 1:
        out.append(f"eingesetzt +0x0A = {c.db(PLACE + 0x0A)} (ausgesetzt)")
    return "; ".join(out) or "nichts (kein Platz, keine Meldung)"


def main():
    for in_match in (0, 1):
        for bname, button in BUTTONS.items():
            for rname, role in ROLES.items():
                for gname, grid in GRIDS.items():
                    where = "im Spiel" if in_match else "vor dem Spiel"
                    print(f"{where:13} {bname:6} {rname:11} {gname:16}: {run(role, button, in_match, grid)}")
    # counter check: a goalkeeper into defence line 1 (line index 0) with an empty grid
    for in_match in (0, 1):
        for bname, button in BUTTONS.items():
            where = "im Spiel" if in_match else "vor dem Spiel"
            print(f"{where:13} {bname:6} Torhüter    Reihe 1 (Abwehr): {run(GOALIE, button, in_match, (0, 0), line=0)}")


if __name__ == "__main__":
    main()
