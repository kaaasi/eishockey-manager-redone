"""F08 / A526: summer break loop of the season change in 0x54C9F (Datei 0x56476–0x56598).

Runs 0x54C9F from Datei 0x56476 (season counter DGROUP 0x3BD + 1) with a frame of its own for three calendar days and
two managers (DGROUP 0x386 = 2, two squad places each; the place counter 0x4BE17 runs for real). Day counter DGROUP
0x3B7 = 6 before the run, so Datei 0x564BC makes the first day 7 (≡ 0 mod 7). Stubs, printed in order with the manager
in DGROUP 0xA70D: stadium construction 0x3D316, day close 0x3ED01, healing 0x07C12 (squad place = DGROUP offset),
date step 0x367B1 (sets the date words DGROUP 0x5A1C/0x5A1E/0x5A20/0x5A22; after the third day 8/0/9/0 = end of the
loop, Datei 0x56570–0x56593). Printed: the season counter before the run and at each day close, and the call order per day.
Usage: python3 f08_sommerpause.py [em.exe]
"""
import sys

import emu16

PATH = sys.argv[1] if len(sys.argv) > 1 else None
DAYS = 3


def main():
    c = emu16.load(PATH)
    o120 = next(r for r in c.regions if r.stub == 0x120)
    assert o120.start <= 0x4BE17 < o120.end
    # + C runtime (long modulo 0x0228B)
    c.allowed = [(0x54C9F, 0x5667A), (0x4BE17, 0x4BEBD), (emu16.HEADER, emu16.HEADER + 0x46E * 16)]
    c.sdb(0x386, 2)
    c.sdw(0x3B7, 6)
    c.sdw(0x3BD, 3)
    for m in range(2):
        for k in range(2):
            c.sdb(0x5DC3 + 0x618 * m + 0x34 * k + 0x0F, 1 + k)
    log = []
    state = {"day": 0}

    def step(cc):
        state["day"] += 1
        log.append(f"--- 0x367B1 (Datum weiter), Zähler 0x3B7 = {cc.dw(0x3B7)}")
        if state["day"] >= DAYS:
            for off, v in ((0x5A1C, 8), (0x5A1E, 0), (0x5A20, 9), (0x5A22, 0)):
                cc.sdw(off, v)
        else:
            cc.sdw(0x5A22, 1)
        return 0

    def named(name, place=False, season=False):
        def fn(cc):
            extra = f" Platz {(cc.arg(0) - 0x5DC3) % 0x618 // 0x34}" if place else ""
            extra += f" (Saisonzähler 0x3BD = {cc.dw(0x3BD)})" if season else ""
            log.append(f"Manager {cc.db(0xA70D)}: {name}{extra}")
            return 0
        return fn
    c.hooks[0x367B1] = step
    c.hooks[0x3D316] = named("Stadionbau 0x3D316")
    c.hooks[0x3ED01] = named("Tagesabschluss 0x3ED01", season=True)
    c.hooks[0x07C12] = named("Heilung 0x07C12", place=True)
    cs, ip = c.at(0x56476)
    c.push(0xFFFF)
    c.push(0xFFFF)
    c.push(0)
    c.r["bp"] = c.r["sp"]
    c.r["sp"] = (c.r["sp"] - 0x40) & 0xFFFF
    c.s["cs"], c.ip = cs, ip
    c.stop_at = {0x56598}
    season0 = c.dw(0x3BD)
    try:
        c.run()
    except emu16.Stop:
        pass
    print(f"Saisonzähler 0x3BD vor Datei 0x56476: {season0}")
    for line in log:
        print("   " + line)


if __name__ == "__main__":
    main()
