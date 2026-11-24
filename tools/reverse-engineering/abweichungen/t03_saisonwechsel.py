"""T03 / A661, A843, A935: order of the season change in the day function 0x54C9F (Datei 0x56130–0x56476).

Runs the end of the season change of 0x54C9F from Datei 0x56130 (exchange groups 0–2, [bp-0x1] = 0) to the season
counter Datei 0x56476 with a frame of its own. Only 0x54C9F runs; every call leaves to a stub and is printed in order
with its arguments: Oberliga draw random(a, a + 5) (0x0C037, Datei 0x56275), club swap 0x36169, cost level loop
Datei 0x563C0–0x56418 (no call), all-time table 0x5667A, team value drift 0x09D1B(mode), league shuffle 0x35EDD,
0x346D5, season loop 0x26F20, 0x5ACA0. Prepared: groups 0 and 1 without pairs (DGROUP 0x7B9 = 0, 0x7BA = 0;
their swaps draw nothing), group 2 with two pairs (0x7BB = 2), Oberliga base 0x7C2 = 24, 0x7B3 = 12, place list 0x5CCB (Oberliga places 24 … 35 = clubs 24 … 35),
one manager on club 0. Random: 2, 2, 4, … (a repeated draw is drawn again, Datei 0x56266–0x56282).
The calendar day is not prepared: between Datei 0x56130 and 0x56476 the day counter DGROUP 0x3B7 and the day index
DGROUP 0xA969 are not written (only Datei 0x564BC and later), so everything printed happens on one day.
Usage: python3 t03_saisonwechsel.py [em.exe]
"""
import sys

import emu16

PATH = sys.argv[1] if len(sys.argv) > 1 else None
CLUBS = 0x2000
NAMES = {0x0C037: "random", 0x36169: "Tausch 0x36169", 0x09D1B: "Wanderung 0x09D1B", 0x35EDD: "Ligamischung 0x35EDD",
         0x346D5: "0x346D5", 0x26F20: "Saisonschleife 0x26F20", 0x5ACA0: "0x5ACA0", 0x5667A: "Ewige Tabelle 0x5667A"}


def main():
    c = emu16.load(PATH)
    c.allowed = [(0x54C9F, 0x5667A)]
    c.sdb(0x7B9, 0)
    c.sdb(0x7BA, 0)
    c.sdb(0x7BB, 2)
    c.sdb(0x7C1, 12)
    c.sdb(0x7C2, 24)
    c.sdb(0x7B3, 12)
    c.sdb(0x386, 1)
    c.sdw(0x9657, CLUBS)
    c.sdw(0x9659, emu16.FAR_HEAP_SEG)
    for p in range(52):
        c.sdb(0x5CCB + p, p)
    seq = iter([2, 2, 4, 1, 3, 5] * 20)
    log = []
    writes = []

    def logger(target):
        def fn(cc):
            if target == 0x0C037:
                lo, hi = cc.arg(0), cc.arg(1)
                v = lo + next(seq) % (hi - lo + 1)
                log.append(f"random({lo}, {hi}) = {v}")
                return v
            name = NAMES[target]
            if target == 0x36169:
                log.append(f"{name}({cc.arg(0) & 0xFF}, {cc.arg(1) & 0xFF}, {cc.arg(2) & 0xFF:#x})")
            elif target in (0x09D1B, 0x5ACA0):
                log.append(f"{name}({cc.arg(0) & 0xFF})")
            else:
                log.append(name)
            return 0
        return fn
    for t in NAMES:
        c.hooks[t] = logger(t)
    orig_ww, orig_wb = c.ww, c.wb

    def ww(seg, off, v):
        if seg == emu16.DGROUP and off in (0x3B7, 0x3B9, 0xA969):
            writes.append(off)
        orig_ww(seg, off, v)

    def wb(seg, off, v):
        if seg == emu16.DGROUP and off in (0x3B7, 0x3B8, 0x3B9, 0x3BA, 0xA969, 0xA96A):
            writes.append(off)
        orig_wb(seg, off, v)
    c.ww, c.wb = ww, wb
    cs, ip = c.at(0x56130)
    c.push(0xFFFF)
    c.push(0xFFFF)
    c.push(0)
    c.r["bp"] = c.r["sp"]
    c.r["sp"] = (c.r["sp"] - 0x40) & 0xFFFF
    c.s["cs"], c.ip = cs, ip
    c.stop_at = {0x56476}
    try:
        c.run()
    except emu16.Stop as e:
        end = str(e)
    others = sorted({f"{t:#07x}" for _, t in c.calls if t not in NAMES})
    print(f"Lauf Datei 0x56130 bis {end}:")
    for i, line in enumerate(log, 1):
        print(f"   {i:2}. {line}")
    print(f"   weitere Stubs: {' '.join(others) or '-'}")
    print(f"   Schreibzugriffe auf Tageszähler 0x3B7/0x3B9 und Tagesindex 0xA969: {len(writes)}")


if __name__ == "__main__":
    main()
