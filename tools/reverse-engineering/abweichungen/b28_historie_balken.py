"""B28 (A356, A805, A806): bars of the history screen "Früher" (em.exe 0x2C1EE, Ov 0xE0).

Runs the original screen with a prepared history of manager 0 (DGROUP 0x965B + 0x1AF · manager + 4 · season, bytes
+0x18 league, +0x1A DEB cup, +0x1B European cup; season count DGROUP 0x3BD) and records the bar fills 0x0B620
(x1, y1, x2, y2, colour). One season per input value; printed: height in pixels (y2 − y1 + 1) and colour of each fill,
from the axis upwards; y1 > y2 is printed as is (behaviour of 0x0B620 for it not replayed). Axis data from DGROUP 0x4BA6 ff. (x 135/75/115, bottom y 31/77/174, block colours 23/22/29).
Usage: python3 b28_historie_balken.py [em.exe]
"""
import sys

import emu16

PATH = sys.argv[1] if len(sys.argv) > 1 else None
AXIS_X = {0: 135, 1: 75, 2: 115}            # Europa, DEB, Liga (DGROUP 0x4BA6)
NAMES = {0: "Europacup", 1: "DEB-Pokal", 2: "Liga"}


def run(values):
    """values: list of (league byte, deb byte, europe byte) per season. Returns fills per season and graph."""
    c = emu16.load(PATH)
    ov = next(r for r in c.regions if r.stub == 0xE0)
    c.allowed = [(ov.start, ov.end), (emu16.HEADER, emu16.HEADER + 0x46E * 16), (0xB0E6, 0xB447), (0xB954, 0xBAC8)]
    c.int_hook = lambda cpu, n: (cpu.r.__setitem__("ax", 0), True)[1]
    for i in range(50):
        c.sdw(0x5A57 + 10 * i, 0xFFFF)
    c.sdb(0xA70E, 0)
    c.sdw(0x3BD, len(values))
    for s, (lg, deb, eu) in enumerate(values):
        base = 0x965B + 4 * s
        c.sdb(base + 0x18, lg)
        c.sdb(base + 0x1A, deb)
        c.sdb(base + 0x1B, eu)
    fills = []
    c.hooks[0x0B620] = lambda cpu: fills.append(tuple(cpu.arg(i) for i in range(2, 7))) or 0

    def stop(cpu):
        raise emu16.Stop("event")
    c.hooks[0x0C068] = stop
    c.hooks[0x0C097] = stop
    try:
        c.call(0x2C1EE)
    except emu16.Stop:
        pass
    out = {}
    for x1, y1, x2, y2, col in fills:
        for g, ax in AXIS_X.items():
            s, r = divmod(x1 - ax - 2, 3)
            if r == 0 and 0 <= s < len(values) and x2 == x1 + 1:
                out.setdefault((s, g), []).append((y1, y2, col))
    return out


def show(title, graph, inputs):
    vals = [(v if graph == 2 else 0, v if graph == 1 else 0, v if graph == 0 else 0) for v in inputs]
    res = run(vals)
    print(title)
    for s, v in enumerate(inputs):
        bars = sorted(res.get((s, graph), []), key=lambda b: -b[1])
        desc = " + ".join(f"y {b[0]}–{b[1]} Farbe {b[2]}" + (f" ({b[1] - b[0] + 1} px)" if b[1] >= b[0] else " (y1 > y2)")
                          for b in bars) or "kein Balken"
        print(f"   Byte {v:#04x} ({v:3}): {desc}")


def main():
    show("Liga (Byte +0x18; 0x80 = Meister-Kennzeichen, 0x33–0x35 Play-off)", 2,
         [1, 2, 6, 12, 13, 18, 24, 25, 30, 36, 0x33, 0x34, 0x35, 0x80, 0x81])
    show("DEB-Pokal (Byte +0x1A; 0x80 = Sieger)", 1, [0, 1, 2, 3, 4, 5, 6, 0x80, 0x81])
    show("Europacup (Byte +0x1B; Bit 3–6 = im Wettbewerb, Bit 0–2 = Runde, 0x80 = Sieger)", 0,
         [0, 1, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x10, 0x88, 0x80])


if __name__ == "__main__":
    main()
