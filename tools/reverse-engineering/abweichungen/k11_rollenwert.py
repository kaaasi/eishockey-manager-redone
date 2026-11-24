"""Aufräumkonzept K11: role value of the player pool +0x18 (far DGROUP 0x8253, 200 × 0x21).

Runs the part of the pool set-up 0x5AF50 (Ov 0x140, called at game start Datei 0x29896) that writes +0x18
(Datei 0x5B391–0x5B4A9) with the random function 0x0C037(lo, hi) replaced: run 1 returns lo, run 2 returns hi, run 3 a
fixed sequence. Printed: the value ranges per pool place and the random calls. It is the only writer of pool +0x18
in the game code (search of all `mov [es:bx+0x18]` behind `les bx,[0x8253]`: Datei 0x5B3A6–0x5B49D; the other hits
0x27534, 0x27D15, 0x076C3, 0x078A7, 0x4A3BE, 0x4AD50 address squad places DGROUP 0x5DC3, +0x18 = negotiation lock).
Usage: python3 k11_rollenwert.py [em.exe]
"""
import sys

import emu16

PATH = sys.argv[1] if len(sys.argv) > 1 else None
POOL = 0x1000                      # pool at FAR_HEAP_SEG:0x1000


def run(mode):
    c = emu16.load(PATH)
    ov = next(r for r in c.regions if r.stub == 0x140)
    c.allowed = [(ov.start, ov.end)]
    c.sdw(0x8253, POOL)
    c.sdw(0x8255, emu16.FAR_HEAP_SEG)
    for i in range(200):
        c.wb(emu16.FAR_HEAP_SEG, POOL + 0x21 * i + 0x18, 0xEE)   # marker: not written
    calls = []
    seq = iter([3, 16, 0, 9] * 100)

    def rnd(cc):
        lo, hi = cc.arg(0), cc.arg(1)
        calls.append((lo, hi))
        return lo if mode == 0 else hi if mode == 1 else next(seq)
    c.hooks[0x0C037] = rnd
    # enter the function body at Datei 0x5B391 with a frame of its own (local word [bp-2] = pool place)
    cs, ip = c.at(0x5B391)
    c.push(0xFFFF)
    c.push(0xFFFF)
    c.push(0)                      # old bp
    c.r["bp"] = c.r["sp"]
    c.r["sp"] = (c.r["sp"] - 0x20) & 0xFFFF
    c.s["cs"], c.ip = cs, ip
    c.stop_at = {0x5B4AB}
    try:
        c.run()
    except emu16.Stop:
        pass
    vals = [c.rb(emu16.FAR_HEAP_SEG, POOL + 0x21 * i + 0x18) for i in range(200)]
    return vals, calls


def ranges(vals):
    out, start = [], 0
    for i in range(1, 201):
        if i == 200 or (vals[i] == 0) != (vals[start] == 0) or (vals[i] == 0xEE) != (vals[start] == 0xEE) \
                or (vals[i] not in (0, 0xEE) and (vals[i] < 50) != (vals[start] < 50)):
            out.append((start, i - 1, vals[start:i]))
            start = i
    return out


def main():
    lo, calls = run(0)
    hi, _ = run(1)
    mid, _ = run(2)
    print(f"Zufallsaufrufe 0x0C037: {len(calls)}, Grenzen {sorted(set(calls))}")
    for a, b, _ in ranges(lo):
        vl, vh, vm = lo[a:b + 1], hi[a:b + 1], mid[a:b + 1]
        kind = "nicht geschrieben" if vl[0] == 0xEE else "0 (Torhüter)" if vl[0] == 0 else "Abwehr" if vl[0] < 50 else "Sturm"
        print(f"   Plätze {a:3}–{b:3}: {kind:17} min {min(vl):3} max {max(vh):3}  Beispiel {vm[:4]}")


if __name__ == "__main__":
    main()
