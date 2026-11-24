"""B29 (A361, A699): strength screen of em.exe (0x128:0x70 = Datei 0x51206).

Runs the original screen (league 0) with the result strings of the team records (DGROUP 0x8257 + 0x50 · team, +7…+14,
the youngest last) and records:
1. what is drawn per row: the eight result marks and the face via 0x51AAF(kb\\33, record, x 0x50, y, x 0xF0, y)
   (Datei 0x518AB–0x518ED; blits 0x139C9: source x/y of the sprite, target x/y) – the face is the trend of the eight
   results (2 + wins − losses, clamped 1…3, see b15), not a value history;
2. the calls of 0x952C(manager, 0) on entry (Datei 0x51217–0x5123A: managers 0 … DGROUP 0x386 − 1 and manager 5), which
   write the team fields of the managers' clubs from their current line-up before the table is drawn.
Usage: python3 b29_staerken.py [em.exe]
"""
import sys

import emu16

PATH = sys.argv[1] if len(sys.argv) > 1 else None
SPRITE = {(19, 36): "rot", (25, 41): "gelb", (25, 36): "grün"}
ROWS = ["ssss----", "nnn-----", "suusnnnu", "--------", "nnnnnnss", "ssssssss"]


def main():
    c = emu16.load(PATH)
    ov = next(r for r in c.regions if r.stub == 0x128)
    c.allowed = [(ov.start, ov.end), (emu16.HEADER, emu16.HEADER + 0x46E * 16), (0xB0E6, 0xB447), (0xB954, 0xBAC8),
                 (0x4BD35, 0x4BE17)]
    c.int_hook = lambda cpu, n: (cpu.r.__setitem__("ax", 0), True)[1]
    for i in range(50):
        c.sdw(0x5A57 + 10 * i, 0xFFFF)
    c.sdw(0x9657, 0)
    c.sdw(0x9659, emu16.FAR_HEAP_SEG)
    c.sdb(0x386, 2)                                    # two managers
    for t in range(64):
        seq = ROWS[t % len(ROWS)]
        for i, ch in enumerate(seq):
            c.sdb(0x8257 + 0x50 * t + 7 + i, ord(ch))
    blits, calls952c = [], []
    c.hooks[0x139C9] = lambda cc: blits.append(((cc.arg(2), cc.arg(3)), (cc.arg(6), cc.arg(7)))) or 0
    c.hooks[0x0952C] = lambda cc: calls952c.append((cc.arg(0) & 0xFF, cc.arg(1) & 0xFF)) or 0
    c.hooks[0x0C17D] = lambda cc: (0x100, 0x1234)      # images: a non-null far pointer, so 0x51AAF is reached

    def stop(cc):
        raise emu16.Stop("event")
    c.hooks[0x0C068] = stop
    c.hooks[0x0C097] = stop
    try:
        c.call(0x51206, 0)
    except emu16.Stop:
        pass
    print("1. Zeichen und Gesicht je Zeile (Zeile n: y = 36 + 15 n)")
    rows = {}
    for src, (x, y) in blits:
        if src in SPRITE and (y - 36) % 15 == 0:
            rows.setdefault((y - 36) // 15, []).append((x, SPRITE[src]))
    for n in sorted(rows):
        marks = [f"{x}:{col}" for x, col in rows[n] if x < 0xF0]
        face = [col for x, col in rows[n] if x == 0xF0]
        print(f"   Zeile {n} ({ROWS[n % len(ROWS)]}): Zeichen {' '.join(marks) or '-'}; Gesicht bei x 240: {face[0] if face else '-'}")
    print("2. Aufrufe 0x952C(Manager, 0) beim Öffnen:", calls952c)


if __name__ == "__main__":
    main()
