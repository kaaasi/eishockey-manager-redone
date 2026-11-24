"""T03 / A829: daily market loop of 0x07169 (Datei 0x0718F–0x0727C) – order of decay, lock release and bid expiry.

Runs 0x07169 from its start for manager 0 (DGROUP 0xA70E = 0, 0x386 = 2 managers) until the second loop (Datei 0x0727C).
Real code: 0x07169 and the place counter 0x4BE17 (Ov 0x120). Prepared: four market places (record 4, DGROUP 0x7623 +
0x34 · place; +0x0F pool index, +0x13 strain, +0x1A lock, +0x09 bit 0x20/0x40 = open bid) and the pool (far DGROUP
0x8253, +0x1E = bidder of the open bid). The random function 0x0C037(lo, hi) is replaced: run "0" returns lo, run "1"
returns hi; each call is printed with the place being processed ([bp-0x2]). The bid expiry 0x4B270 is stubbed and
recorded. Printed: the random calls in order and the places afterwards.
Usage: python3 t03_markt.py [em.exe]
"""
import sys

import emu16

PATH = sys.argv[1] if len(sys.argv) > 1 else None
POOL = 0x1000
PLACES = [  # (pool index, strain +0x13, lock +0x1A, +0x09, pool +0x1E)
    (1, 60, 0x01, 0x20, 5),     # strain over 56, locked only by manager 0, open bid
    (2, 50, 0x00, 0x00, 0),     # nothing to do
    (3, 57, 0x02, 0x40, 0),     # locked by manager 1, bid bits without bidder
    (4, 40, 0x81, 0x00, 0),     # locked by manager 0 plus bit 0x80
]


def place(i):
    return 0x7623 + 0x34 * i


def run(mode):
    c = emu16.load(PATH)
    o120 = next(r for r in c.regions if r.stub == 0x120)
    c.allowed = [(0x07169, 0x07C12), (0x4BE17, 0x4BEBD)]
    assert o120.start <= 0x4BE17 < o120.end
    c.sdb(0xA70E, 0)
    c.sdb(0x386, 2)
    c.sdw(0x8253, POOL)
    c.sdw(0x8255, emu16.FAR_HEAP_SEG)
    for i, (pool, strain, lock, b09, bidder) in enumerate(PLACES):
        c.sdb(place(i) + 0x0F, pool)
        c.sdb(place(i) + 0x13, strain)
        c.sdb(place(i) + 0x1A, lock)
        c.sdb(place(i) + 0x09, b09)
        c.wb(emu16.FAR_HEAP_SEG, POOL + 0x21 * pool + 0x1E, bidder)
    log = []

    def rnd(cc):
        lo, hi = cc.arg(0), cc.arg(1)
        log.append(f"Platz {cc.rw(cc.s['ss'], (cc.r['bp'] - 2) & 0xFFFF)}: random({lo}, {hi}) = {lo if mode == 0 else hi}")
        return lo if mode == 0 else hi

    def expire(cc):
        log.append(f"Platz {cc.rw(cc.s['ss'], (cc.r['bp'] - 2) & 0xFFFF)}: 0x4B270 (Gebot verfällt)")
        return 0
    c.hooks[0x0C037] = rnd
    c.hooks[0x4B270] = expire
    c.stop_at = {0x0727C}
    try:
        c.call(0x07169)
    except emu16.Stop:
        pass
    after = [(c.db(place(i) + 0x13), c.db(place(i) + 0x1A), c.db(place(i) + 0x09)) for i in range(len(PLACES))]
    return log, after


def main():
    print("Plätze vorher: " + "; ".join(f"{i}: Kraft {s}, Sperre {l:#04x}, +0x09 {b:#04x}, Bieter {d}"
                                         for i, (_, s, l, b, d) in enumerate(PLACES)))
    for mode in (0, 1):
        log, after = run(mode)
        print(f"Lauf Zufall = {'untere' if mode == 0 else 'obere'} Grenze:")
        for line in log:
            print("   " + line)
        print("   nachher: " + "; ".join(f"{i}: Kraft {s}, Sperre {l:#04x}, +0x09 {b:#04x}"
                                         for i, (s, l, b) in enumerate(after)))


if __name__ == "__main__":
    main()
