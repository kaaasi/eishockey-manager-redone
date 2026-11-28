"""A259: accepting a bid in the transfer list – which checks does the original make before the player leaves?

Replays two click branches of the transfer list handler (Ov with 0x465F1) straight from the bytes of em.exe:

* "markt": click on a market place (record 4, DGROUP 0x7623 + 0x34 * place, click value 1000 + place) whose pool record is
  still owned by the manager (pool +0x1A = manager, Datei 0x47324) and has an open bid (+0x09 bit 0x80); runs
  Datei 0x4710E-0x474E8 (bid amount 0x47347, offer window 0x491F9, sale 0x4743C-0x474D4).
* "angebot": click on an own squad place (DGROUP 0x5DC3 + 0x618 * manager + 0x34 * place) with an unsolicited offer
  (+0x09 bit 0x40); runs Datei 0x46D8F-0x46FCF (amount 0x46E0E, window, sale 0x46F3C-0x46FCF).

The offer window 0x491F9 is replaced by the answer ("verkaufen" = 0, "behalten" = 1). Value 0x49D36 returns a fixed
100000, the random function 0x0C037(lo, hi) returns lo, the long helpers 0x021CA (multiply) and 0x0227C (divide) are
computed in Python. Recorded instead of executed: 0x4B270 (bid/message expiry), 0x058DC (team values of the buyer),
0x425B6 (remove a record) and the screen calls. Printed: every call that leaves the branch, the pool record afterwards
and the manager's money. The buying club is never read beyond its index: there is no squad of computer clubs in the
original, so a "full squad of the bidder" cannot stop the sale.

Variants: bidder club 5 (computer club), bidder club 70 (> 0x3F, foreign), and "markt" with pool owner = market (4)
instead of the manager (the only check before the window). No original data is printed.
Usage: python3 a259_annahme.py [em.exe]
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "abweichungen"))
import emu16  # noqa: E402

PATH = sys.argv[1] if len(sys.argv) > 1 else None
POOL = 0x1000
POOL_INDEX = 7
MANAGER = 0
MONEY = 0x97EA  # DGROUP, + 0x1AF * manager (Datei 0x4749A / 0x46F4C)


def s32(v):
    v &= 0xFFFFFFFF
    return v - (1 << 32) if v & 0x80000000 else v


def setup(log):
    c = emu16.load(PATH)
    c.sdb(0xA70E, MANAGER)
    c.sdb(0xA70D, MANAGER)
    c.sdw(0x8253, POOL)
    c.sdw(0x8255, emu16.FAR_HEAP_SEG)
    c.sdw(MONEY + 0x1AF * MANAGER, 0)
    c.sdw(MONEY + 0x1AF * MANAGER + 2, 0)

    def lmul(cc):
        a = cc.r["ax"] | cc.r["dx"] << 16
        b = cc.r["bx"] | cc.r["cx"] << 16
        p = (a * b) & 0xFFFFFFFF
        return p & 0xFFFF, p >> 16

    def ldiv(cc):
        a = s32(cc.arg(0) | cc.arg(1) << 16)
        b = s32(cc.arg(2) | cc.arg(3) << 16)
        q = int(a / b)
        q &= 0xFFFFFFFF
        # the runtime helper cleans its 8 argument bytes itself (no "add sp" after the calls): move the return address up
        ss, sp = cc.s["ss"], cc.r["sp"]
        rip, rcs = cc.rw(ss, sp), cc.rw(ss, sp + 2)
        cc.ww(ss, sp + 8, rip)
        cc.ww(ss, sp + 10, rcs)
        cc.r["sp"] = (sp + 8) & 0xFFFF
        return q & 0xFFFF, q >> 16

    def value(cc):
        log.append("0x49D36 Spielerwert -> 100000 (fest)")
        return 100000 & 0xFFFF, 100000 >> 16

    def rnd(cc):
        lo, hi = cc.arg(0), cc.arg(1)
        log.append(f"0x0C037 random({lo}, {hi}) -> {lo}")
        return lo

    def rec(name, nargs):
        def hook(cc):
            log.append(f"{name}({', '.join(str(cc.arg(i) & 0xFF) for i in range(nargs))})")
            return 0
        return hook
    c.hooks[0x021CA] = lmul
    c.hooks[0x0227C] = ldiv
    c.hooks[0x49D36] = value
    c.hooks[0x0C037] = rnd
    c.hooks[0x4B270] = rec("0x4B270 Verfall Gebot/Meldung", 0)
    c.hooks[0x058DC] = rec("0x058DC Teamwerte Bieter (Verein, Wert)", 2)
    c.hooks[0x425B6] = rec("0x425B6 Satz entfernen (Platz, 1, Art)", 3)
    return c


def frame(c, locals_):
    c.push(0xFFFF)
    c.push(0xFFFF)
    c.push(0)
    c.r["bp"] = c.r["sp"]
    c.r["sp"] = (c.r["sp"] - 0x120) & 0xFFFF
    for off, v in locals_.items():
        c.ww(c.s["ss"], (c.r["bp"] + off) & 0xFFFF, v)


def pool(c, field):
    return c.rb(emu16.FAR_HEAP_SEG, POOL + 0x21 * POOL_INDEX + field)


def run(kind, bidder, answer, pool_owner=MANAGER):
    log = []
    c = setup(log)
    seg = emu16.FAR_HEAP_SEG
    c.wb(seg, POOL + 0x21 * POOL_INDEX + 0x1A, pool_owner)
    c.wb(seg, POOL + 0x21 * POOL_INDEX + 0x1D, 12)  # old club of the player
    c.wb(seg, POOL + 0x21 * POOL_INDEX + 0x1E, 3)   # message number of the bid

    def window(cc):
        log.append(f"0x491F9 Angebotsfenster -> {answer} ({'behalten' if answer == 1 else 'verkaufen'})")
        return answer
    c.hooks[0x491F9] = window
    place = 2
    if kind == "markt":
        rec = 0x7623 + 0x34 * place
        c.sdb(rec + 0x0F, POOL_INDEX)
        c.sdb(rec + 0x1A, 1 << MANAGER)          # market place locked for this manager
        c.sdb(rec + 0x09, 0x80)                  # open bid
        c.sdb(rec + 0x16, bidder)
        c.sdb(rec + 0x0B, 2)                     # contract years
        for k, v in ((0x10, 60), (0x11, 63), (0x12, 66)):
            c.sdb(rec + k, v)
        frame(c, {-0x8: 1000 + place})
        start, ends = 0x4710E, {0x474E8}
        c.allowed = [(0x4710E, 0x474E8)]
    else:
        rec = 0x5DC3 + 0x618 * MANAGER + 0x34 * place
        c.sdb(rec + 0x0F, POOL_INDEX)
        c.sdb(rec + 0x09, 0x40)                  # unsolicited offer
        c.sdb(rec + 0x16, bidder)
        c.sdb(rec + 0x0B, 2)
        for k, v in ((0x10, 60), (0x11, 63), (0x12, 66)):
            c.sdb(rec + k, v)
        frame(c, {-0x8: place, -0x2F: 0})
        c.sdw(0x5A16, 0)
        start, ends = 0x46D8F, {0x46FCF, 0x46FD4}
        c.allowed = [(0x46D8F, 0x46FD4)]
    c.s["cs"], c.ip = c.at(start)
    c.stop_at = ends | {0x4677A}
    end = None
    try:
        c.run()
    except emu16.Stop:
        end = c.here()
    money = s32(c.dw(MONEY + 0x1AF * MANAGER) | c.dw(MONEY + 0x1AF * MANAGER + 2) << 16)
    head = f"{kind}: Bieter {bidder}, Pool +0x1A vorher {pool_owner}, Antwort {answer}"
    print(head)
    for line in log:
        print("   ", line)
    print(f"    Ende Datei {end:#07x}; Pool +0x1A = {pool(c, 0x1A)}, +0x1D = {pool(c, 0x1D)}; Konto Manager = {money}; "
          f"Satz +0x09 = {c.db(rec + 0x09):#04x}")


def main():
    run("markt", 5, 0)
    run("markt", 70, 0)
    run("markt", 5, 1)
    run("markt", 5, 0, pool_owner=4)
    run("angebot", 5, 0)
    run("angebot", 70, 0)
    run("angebot", 5, 1)


if __name__ == "__main__":
    main()
