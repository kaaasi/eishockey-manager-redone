"""A707: base strengths of a new market player whose pool club is a national team (track abgleich-dosbox-22b, part 1).

Replays two pieces of em.exe with emu16 (tools/reverse-engineering/abweichungen/emu16.py):

Part A – list change 0x494EF (Ov 0x118), first draw only: which club record does the original pass to the strength routine
0x4981E for a pool player whose club (+0x1D) is the record of the coach's nation? Prepared: market record 4 empty, pool
player P owned by nobody (+0x1A = 6) with +0x1D = NATION, club record NATION +0x16 = 6 (Bundestrainer, as in all original
saves with a coach) or 0 (no coach). The random function 0x0C037 is scripted (slot 0, pool index P, no national draw, form
50, stand-in = lower bound); 0x425B6 is hooked to "slot not held" (1); everything else outside 0x494EF is stubbed. The
hook on 0x4981E prints its arguments and ends the run.

Part B – strength routine 0x4981E itself (real code incl. 0x4D2F0 and the clamp 0x4BD35) for the national record and for
a league record with distinct field values: which club fields go into pool +0x15/+0x16, and t (random lower bound 10 * t).

Usage: python3 a707_grundstaerken.py [em.exe]
Only addresses and computed numbers are printed.
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "abweichungen"))
import emu16  # noqa: E402

PATH = sys.argv[1] if len(sys.argv) > 1 else None
POOL_OFF = 0x0000        # far pool (200 x 0x21) in FAR_HEAP_SEG
CLUBS_OFF = 0x2000       # far club records (150 x 0x3D) in FAR_HEAP_SEG
NATION = 54              # record of the coach's nation in FILE80/FILE81 (Germany, +0x16 = 6)
LEAGUE = 30              # an Oberliga record used as stand-in in part B
P = 2                    # pool index of a German player in FILE80/FILE81 (+0x1D = 54)
SEG = emu16.FAR_HEAP_SEG


def rec(i):
    return CLUBS_OFF + 0x3D * i


def prepare(nation16):
    c = emu16.load(PATH)
    c.sdw(0x8253, POOL_OFF)
    c.sdw(0x8255, SEG)
    c.sdw(0x9657, CLUBS_OFF)
    c.sdw(0x9659, SEG)
    c.wb(SEG, rec(NATION) + 0x16, nation16)
    for f, v in ((0x18, 82), (0x19, 81), (0x1A, 81), (0x1B, 80)):   # FILE80 values of record 54
        c.wb(SEG, rec(NATION) + f, v)
    for f, v in ((0x18, 41), (0x19, 43), (0x1A, 45), (0x1B, 47)):
        c.wb(SEG, rec(LEAGUE) + f, v)
    c.wb(SEG, POOL_OFF + 0x21 * P + 0x1A, 6)
    c.wb(SEG, POOL_OFF + 0x21 * P + 0x1D, NATION)
    return c


def part_a(nation16):
    c = prepare(nation16)
    for k in range(10):
        c.sdb(0x7623 + 0x34 * k + 0x0F, 0)        # market record 4, slot k: no pool player
    c.allowed = [(0x494EF, 0x4981E)]
    log = []

    def rnd(cc):
        lo, hi = cc.arg(0), cc.arg(1)
        if (lo, hi) == (0, 9):
            v = 0
        elif (lo, hi) == (1, 0xB3):
            v = P
        elif (lo, hi) == (0, 5):
            v = 1
        elif (lo, hi) == (0x2D, 0x37):
            v = 50
        else:
            v = lo
            log.append(f"stand-in random({lo}, {hi}) = {v}")
            return v
        return v

    def strength(cc):
        log.append(f"0x4981E(pool {cc.arg(0) & 0xFF}, club record {cc.arg(1) & 0xFF})")
        raise emu16.Stop("strength routine reached")
    c.hooks[0x0C037] = rnd
    c.hooks[0x425B6] = lambda cc: 1
    c.hooks[0x4981E] = strength
    try:
        c.call(0x494EF)
    except emu16.Stop:
        pass
    lead = [f"0x7C0[l] = {c.db(0x7C0 + i)}, 0x7B1[l] = {c.db(0x7B1 + i)}, 0x7C6/0x7C7[2l] = {c.db(0x7C6 + 2 * i)}…"
            f"{c.db(0x7C7 + 2 * i)}" for i in range(3)]
    return log, lead


def part_b(club, mode):
    c = prepare(6)
    c.allowed = [(0x4981E, 0x4991D), (0x4D2F0, 0x4D316), (0x4BD35, 0x4BD56)]
    log = []

    def rnd(cc):
        lo, hi = cc.arg(0), cc.arg(1)
        v = lo if mode == 0 else hi
        log.append(f"random({lo}, {hi}) = {v}")
        return v
    c.hooks[0x0C037] = rnd
    c.call(0x4981E, P, club)
    k, t = c.rb(SEG, POOL_OFF + 0x21 * P + 0x15), c.rb(SEG, POOL_OFF + 0x21 * P + 0x16)
    return log, k, t, c.db(0x6AD)


def main():
    print("Teil A – Listenwechsel 0x494EF, erster Zug, Poolspieler mit +0x1D = Satz des Landes:")
    for n16 in (6, 0):
        log, lead = part_a(n16)
        print(f"  Satz {NATION} +0x16 = {n16}: " + "; ".join(log))
    print("  Ligagrenzen im Abbild: " + " | ".join(lead))
    print("Teil B – Grundstärken 0x4981E (Satz 54: 0x19 = 81, 0x1B = 80; Satz 30: 0x19 = 43, 0x1B = 47):")
    for club in (NATION, LEAGUE):
        for mode in (0, 1):
            log, k, t, first = part_b(club, mode)
            print(f"  Satz {club}, Zufall {'unten' if mode == 0 else 'oben'}: {'; '.join(log)} -> +0x15 = {k}, +0x16 = {t}"
                  f" (DGROUP 0x6AD = {first})")


if __name__ == "__main__":
    main()
