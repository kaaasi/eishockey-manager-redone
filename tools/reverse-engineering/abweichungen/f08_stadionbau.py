"""F08 / A509: construction week 0x3D316 (Datei 0x3D333–0x3D44F) for a construction site without target.

Runs 0x3D316 for manager 0 (DGROUP 0xA70E = 0). Stadium record DGROUP 0x9771 + 0x1AF · manager: features p = 0 … 8 level
+2p, target +2p+1; seats/standing p = 9, 10 word +0x1D + 4(p − 9) = places, +0x1F + 4(p − 9) = target places; weeks left
+0x12 + p. Prepared: feature 2 level 3 target 0 (site without target), feature 4 level 2 target 5, seats project 9
places 1000 target 0, each with one week left. Stubs (printed with the project [bp-0x5]): 0x3CFA0 (before the loop),
text and message calls of the finished site. Printed: levels, targets and weeks afterwards.
Usage: python3 f08_stadionbau.py [em.exe]
"""
import sys

import emu16

PATH = sys.argv[1] if len(sys.argv) > 1 else None
REC = 0x9771


def main():
    c = emu16.load(PATH)
    c.allowed = [(0x3D316, 0x3D45F)]
    c.sdb(0xA70E, 0)
    for p, level, target in ((2, 3, 0), (4, 2, 5)):
        c.sdb(REC + 2 * p, level)
        c.sdb(REC + 2 * p + 1, target)
        c.sdb(REC + 0x12 + p, 1)
    c.sdw(REC + 0x1D, 1000)
    c.sdw(REC + 0x1F, 0)
    c.sdb(REC + 0x12 + 9, 1)
    calls = []
    orig = c.enter_target

    def enter(cs, ip, far, src):
        n = len(c.calls)
        orig(cs, ip, far, src)
        if len(c.calls) > n:
            p = c.rb(c.s["ss"], (c.r["bp"] - 5) & 0xFFFF)
            calls.append(f"Projekt {p}: {c.calls[-1][1]:#07x} (aus Datei {src:#07x})")
    c.enter_target = enter
    try:
        c.call(0x3D316)
    except emu16.Stop:
        pass
    for line in calls:
        print("   " + line)
    for p in (2, 4):
        print(f"Merkmal {p}: Stufe {c.db(REC + 2 * p)}, Ziel {c.db(REC + 2 * p + 1)}, Restwochen {c.db(REC + 0x12 + p)}")
    print(f"Sitzplätze (Projekt 9): Plätze {c.dw(REC + 0x1D)}, Ziel {c.dw(REC + 0x1F)}, Restwochen {c.db(REC + 0x12 + 9)}")


if __name__ == "__main__":
    main()
