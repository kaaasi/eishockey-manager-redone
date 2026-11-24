"""Aufräumkonzept K13 (A463): place of a manager's club in the start league, Datei 0x29C3B–0x29CF7 (game preparation 0x28165).

The same code runs at game start and when a manager joins (add mode): the script enters 0x28165 at Datei 0x29C3B with its
locals ([bp−0x5E] = start league L, [bp−0x5A] = drawn record r) and stops at Datei 0x29CFA. Real: league of a record
0x34FD5 and the cup line test 0x45CD0(r, 1); replaced: random 0x0C037(lo, hi) (scripted sequence, calls recorded) and the
swap 0x36169(v, r, 0x80) (recorded). Records 0 … 51 = clubs (DGROUP 0x7C0 = 0, 12, 24 league bases, 0x7B1 = 12 each,
0x7CB = 35 last league record), manager clubs DGROUP 0x966F + 0x1AF · m, manager to place DGROUP 0xA70E.
Usage: python3 k13_platzwahl.py [em.exe]
"""
import sys

import emu16

PATH = sys.argv[1] if len(sys.argv) > 1 else None


def run(league, m, clubs, rolls, cup_round=0, line1=(), line2=()):
    c = emu16.load(PATH)
    o110 = next(r for r in c.regions if r.stub == 0x110)
    oe8 = next(r for r in c.regions if r.stub == 0xE8)
    c.allowed = [(0x29C3B, 0x29CFA), (0x34FD5, 0x34FF9), (0x45CD0, 0x45D46)]
    _ = o110, oe8
    c.sdb(0xA70E, m)
    for i, v in enumerate(clubs):
        c.sdb(0x966F + 0x1AF * i, v)
    c.sdb(0x5CF4, cup_round)
    for i in range(32):
        c.sdb(0x5CFB + 32 + i, line1[i] if i < len(line1) else 0)
        c.sdb(0x5CFB + 64 + i, line2[i] if i < len(line2) else 0)
    seq = iter(rolls)
    draws, swaps = [], []

    def rnd(cc):
        v = next(seq)
        draws.append((cc.arg(0), cc.arg(1), v))
        return v
    c.hooks[0x0C037] = rnd
    c.hooks[0x36169] = lambda cc: swaps.append((cc.arg(0) & 0xFF, cc.arg(1) & 0xFF, cc.arg(2))) or 0
    cs, ip = c.at(0x29C3B)
    c.push(0xFFFF)
    c.push(0xFFFF)
    c.push(0)
    c.r["bp"] = c.r["sp"]
    c.r["sp"] = (c.r["sp"] - 0x70) & 0xFFFF
    c.ww(c.s["ss"], c.r["bp"] - 0x5E, league)
    c.s["cs"], c.ip = cs, ip
    c.stop_at = {0x29CFA}
    try:
        c.run()
    except emu16.Stop:
        pass
    except StopIteration:
        return draws, swaps, "Würfe aufgebraucht (zieht weiter)"
    return draws, swaps, ""


def main():
    ec = [3, 7, 14, 20, 1, 5, 9, 11]                     # eight cup places after the draw (line 1)
    cases = [
        ("Start, L = Ober, Verein 30 (Oberliga)", dict(league=2, m=0, clubs=[30], rolls=[26])),
        ("Start, L = Ober, Verein 40 (ohne Liga)", dict(league=2, m=0, clubs=[40], rolls=[26])),
        ("Start, L = Zweite, Verein 30 (Oberliga)", dict(league=1, m=0, clubs=[30], rolls=[15])),
        ("Start, L = Bundesliga, Verein 4 (Bundesliga)", dict(league=0, m=0, clubs=[4], rolls=[4])),
        ("Start, L = Bundesliga, Verein 4, Wurf 0", dict(league=0, m=0, clubs=[4], rolls=[0, 0, 6])),
        ("Start, Manager 1, Wurf = Verein von Manager 0", dict(league=2, m=1, clubs=[26, 40], rolls=[26, 27])),
        ("Start, Manager 0, Wurf = Verein von Manager 1", dict(league=2, m=0, clubs=[40, 26], rolls=[26])),
        ("Zug, L = Bundesliga, Runde 1, Wurf Europapokal", dict(league=0, m=0, clubs=[30], rolls=[3, 7, 0, 8],
                                                                cup_round=1, line1=ec)),
        ("Zug, L = Bundesliga, Runde 1, Zeile 2 belegt", dict(league=0, m=0, clubs=[30], rolls=[2, 8], cup_round=1,
                                                             line1=ec, line2=[2, 6])),
        ("Start, L = Bundesliga, Zeile 2 belegt", dict(league=0, m=0, clubs=[30], rolls=[2, 8], cup_round=0,
                                                      line2=[2, 6])),
        ("Zug, L = Bundesliga, Runde 3, Wurf Europapokal", dict(league=0, m=0, clubs=[30], rolls=[3, 1, 8],
                                                                cup_round=3, line1=ec)),
        ("Zug, L = Bundesliga, Runde 1, Wurf 0", dict(league=0, m=0, clubs=[30], rolls=[0, 8], cup_round=1, line1=ec)),
        ("Zug, L = Bundesliga, Runde 3, Wurf 0", dict(league=0, m=0, clubs=[30], rolls=[0, 8], cup_round=3, line1=ec)),
        ("Zug, L = Bundesliga, Runde 4, Wurf Europapokal", dict(league=0, m=0, clubs=[30], rolls=[3, 1, 8],
                                                                cup_round=4, line1=ec)),
    ]
    for label, kw in cases:
        draws, swaps, note = run(**kw)
        d = ", ".join(f"random({lo}, {hi}) = {v}" for lo, hi, v in draws) or "kein Wurf"
        s = ", ".join(f"0x36169({v}, {r}, {f:#x})" for v, r, f in swaps) or "kein Tausch (behalten)"
        print(f"   {label:48} {d}; {s}{'; ' + note if note else ''}")


if __name__ == "__main__":
    main()
