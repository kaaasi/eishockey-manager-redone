"""F08 / A512: offended construction firms in the day close 0x3ED01 (Datei 0x3ED19–0x3ED48).

Runs 0x3ED01 from its start for manager 1 (DGROUP 0xA70E = 1) until the share call Datei 0x3ED4A. Prepared: the offended
flags DGROUP 0xCC09 + 11 · manager + project (11 projects) – set for projects 2 and 5 of manager 1, project 4 of
manager 0. The random function 0x0C037(lo, hi) is replaced: 0 for projects 2 and 3, 7 otherwise. Printed: every random
call with the project ([bp-0x5]) and the flags of both managers afterwards.
Usage: python3 f08_baufirma.py [em.exe]
"""
import sys

import emu16

PATH = sys.argv[1] if len(sys.argv) > 1 else None
FLAGS = 0xCC09


def main():
    c = emu16.load(PATH)
    c.allowed = [(0x3ED01, 0x3F185)]
    c.sdb(0xA70E, 1)
    for m, p in ((1, 2), (1, 5), (0, 4)):
        c.sdb(FLAGS + 11 * m + p, 1)
    before = [[c.db(FLAGS + 11 * m + p) for p in range(11)] for m in range(2)]
    log = []

    def rnd(cc):
        lo, hi = cc.arg(0), cc.arg(1)
        p = cc.rb(cc.s["ss"], (cc.r["bp"] - 5) & 0xFFFF)
        v = 0 if p in (2, 3) else 7
        log.append(f"Projekt {p:2}: random({lo}, {hi}) = {v}")
        return v
    c.hooks[0x0C037] = rnd
    c.stop_at = {0x3ED4A}
    try:
        c.call(0x3ED01)
    except emu16.Stop:
        pass
    after = [[c.db(FLAGS + 11 * m + p) for p in range(11)] for m in range(2)]
    print(f"Merker vorher : Manager 0 {before[0]}, Manager 1 {before[1]}")
    for line in log:
        print("   " + line)
    print(f"Merker nachher: Manager 0 {after[0]}, Manager 1 {after[1]}")


if __name__ == "__main__":
    main()
