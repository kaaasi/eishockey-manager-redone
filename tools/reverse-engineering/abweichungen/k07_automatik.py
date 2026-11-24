"""K07 / A165, A936, A939: automatic line-up 0x426BC (Ov 0x108) with the selection 0x42838 and the role 0x04180.

Runs 0x426BC for manager 1 (DGROUP 0xA70E = 1). Real code: 0x426BC, 0x42838, 0x04180. Prepared squad of manager 1
(DGROUP 0x5DC3 + 0x618 · 1 + 0x34 · place): 3 goalkeepers, 9 defenders, 12 forwards (pool +0x18 = 0 / 25 / 75, far
DGROUP 0x8253), values +0x10/+0x11/+0x12 = 40 + place (later place = stronger), place 4 (defender) injured
(+0x09 = 2) and before the run in the line-up (+0x0A = 1, +0x15 = 2). Every place starts with +0x15 = 0x55 (marker)
except place 4. Runs: setting DGROUP 0x375 + 2 · manager (byte, Datei 0x426C4–0x426E4) for manager 0 / manager 1 =
1 / 0 and 0 / 1. Printed: +0x0A (in the line-up) and +0x15 (line: block + 1, bit 0x80 = forward) per place.
Usage: python3 k07_automatik.py [em.exe]
"""
import sys

import emu16

PATH = sys.argv[1] if len(sys.argv) > 1 else None
POOL = 0x1000
MANAGER = 1
ROLES = [0] * 3 + [25] * 9 + [75] * 12
INJURED = 4


def place(k):
    return 0x5DC3 + 0x618 * MANAGER + 0x34 * k


def run(setting0, setting1):
    c = emu16.load(PATH)
    o108 = next(r for r in c.regions if r.stub == 0x108)
    c.allowed = [(o108.start, o108.end), (0x04180, 0x041AC)]
    c.sdb(0xA70E, MANAGER)
    c.sdb(0x375, setting0)
    c.sdb(0x377, setting1)
    c.sdw(0x8253, POOL)
    c.sdw(0x8255, emu16.FAR_HEAP_SEG)
    for k, role in enumerate(ROLES):
        c.wb(emu16.FAR_HEAP_SEG, POOL + 0x21 * (k + 1) + 0x18, role)
        c.sdb(place(k) + 0x0F, k + 1)
        for f in (0x10, 0x11, 0x12):
            c.sdb(place(k) + f, 40 + k)
        c.sdb(place(k) + 0x15, 0x55)
    c.sdb(place(INJURED) + 0x09, 2)
    c.sdb(place(INJURED) + 0x0A, 1)
    c.sdb(place(INJURED) + 0x15, 2)
    c.call(0x426BC)
    return [(c.db(place(k) + 0x0A), c.db(place(k) + 0x15)) for k in range(len(ROLES))], c.calls


def main():
    kind = {0: "T", 25: "A", 75: "S"}
    for s0, s1 in ((1, 0), (0, 1)):
        rows, calls = run(s0, s1)
        stubs = sorted({f"{t:#07x}" for _, t in calls})
        print(f"Einstellung 0x375 (Manager 0) = {s0}, 0x377 (Manager 1) = {s1}; Stubs: {' '.join(stubs) or '-'}")
        print("   Platz: " + " ".join(f"{k:>4}" for k in range(len(ROLES))))
        print("   Art  : " + " ".join(f"{kind[r] + ('v' if k == INJURED else ''):>4}" for k, r in enumerate(ROLES)))
        print("   +0x0A: " + " ".join(f"{a:>4}" for a, _ in rows))
        print("   +0x15: " + " ".join(f"{b:#4x}" for _, b in rows))


if __name__ == "__main__":
    main()
