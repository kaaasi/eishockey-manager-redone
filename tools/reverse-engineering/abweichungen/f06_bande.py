"""F06 (A888): accepting a board offer in the advertising screen 0x2B0C0 and the monthly income 0x3E657.

1. Acceptance, Datei 0x2BAD4–0x2BB80 (entered with its locals: [bp−0x14]/[bp−0x12] = far pointer to the offer record,
   +4 = number of boards): per board the first board field 0 … 6 (DGROUP 0xC15B + 0x6E · manager + 10 · field) that is
   empty (+0 = 0) or cancellable (+6 bit 0x80) gets a copy of the offer (movmem 0x48:0x2BD4 = Datei 0x105C4, replaced by a
   copy) and +4 = contract number DGROUP 0x17C; afterwards DGROUP 0x17C + 1.
2. Income 0x3E657 (Stub 0x100:0x39): the sum of +0 of the 11 fields goes in as advertising income (Datei 0x3E70C–0x3E734);
   the script returns the function value with the team part set to 0.
Usage: python3 f06_bande.py [em.exe]
"""
import sys

import emu16

PATH = sys.argv[1] if len(sys.argv) > 1 else None
FIELDS = 0xC15B
OFFER = 0x2000            # offer record at FAR_HEAP_SEG:0x2000


def setup(fields):
    c = emu16.load(PATH)
    c.int_hook = lambda cpu, n: (cpu.r.__setitem__("ax", 0), True)[1]
    c.sdb(0xA70E, 0)
    c.sdb(0x17C, 3)
    for k, f in enumerate(fields):
        amount, number, months, option = f
        a = FIELDS + 10 * k
        c.sdw(a, amount & 0xFFFF)
        c.sdw(a + 2, amount >> 16)
        c.sdb(a + 4, number)
        c.sdb(a + 5, months)
        c.sdb(a + 6, option)
    return c


def accept(fields, boards, amount=900):
    c = setup(fields)
    c.allowed = [(0x2BAD4, 0x2BB84)]
    o = OFFER
    seg = emu16.FAR_HEAP_SEG
    c.ww(seg, o, amount)
    c.ww(seg, o + 2, 0)
    c.wb(seg, o + 4, boards)
    c.wb(seg, o + 5, 12)
    c.wb(seg, o + 7, 6)

    def movmem(cc):
        src = cc.lin(cc.arg(1), cc.arg(0))
        dst = cc.lin(cc.arg(3), cc.arg(2))
        n = cc.arg(4)
        cc.mem[dst:dst + n] = cc.mem[src:src + n]
        return 0
    c.hooks[0x105C4] = movmem
    cs, ip = c.at(0x2BAD4)
    c.push(0xFFFF)
    c.push(0xFFFF)
    c.push(0)
    c.r["bp"] = c.r["sp"]
    c.r["sp"] = (c.r["sp"] - 0x50) & 0xFFFF
    c.ww(c.s["ss"], c.r["bp"] - 0x14, o)
    c.ww(c.s["ss"], c.r["bp"] - 0x12, seg)
    c.s["cs"], c.ip = cs, ip
    c.stop_at = {0x2BB84}
    try:
        c.run()
    except emu16.Stop:
        pass
    out = []
    for k in range(7):
        a = FIELDS + 10 * k
        out.append((c.dw(a) | c.dw(a + 2) << 16, c.db(a + 4), c.db(a + 5), c.db(a + 6), c.db(a + 7)))
    return out, c.db(0x17C)


def income(fields):
    c = setup(fields)
    ov = next(r for r in c.regions if r.stub == 0x100)
    c.allowed = [(ov.start, ov.end), (emu16.HEADER, emu16.HEADER + 0x46E * 16)]
    c.sdw(0x9657, 0x100)
    c.sdw(0x9659, emu16.FAR_HEAP_SEG)
    c.call(0x3E657, 0)
    return c.r["ax"] | c.r["dx"] << 16


def show(fields):
    return " ".join(f"[{k}: {a} Nr {n}{' k' if o & 0x80 else ''}]" if a else f"[{k}: -]" for k, (a, n, m, o, *_) in enumerate(fields))


def main():
    old = [(300, 1, 10, 0x80), (300, 1, 10, 0x80), (500, 2, 10, 0), (0, 0, 0, 0), (0, 0, 0, 0), (0, 0, 0, 0),
           (0, 0, 0, 0)]
    print("1. Annahme eines Bandenangebots (Betrag 900, Partner 6); Felder vorher:", show(old),
          "(Nr 1 = kündbarer Vertrag über zwei Felder)")
    for boards in (1, 2, 3):
        new, nxt = accept(old, boards)
        print(f"   {boards} Bande(n): {show(new)}; DGROUP 0x17C danach {nxt}")
    print("2. Monatseinnahme 0x3E657 (Teamanteil 0): Summe der Felder +0")
    full = old + [(0, 0, 0, 0)] * 4
    for label, f in (("vorher", full), ("ein Feld", [(900, 3, 12, 0)] + [(0, 0, 0, 0)] * 10),
                     ("zwei Felder", [(900, 3, 12, 0)] * 2 + [(0, 0, 0, 0)] * 9),
                     ("drei Felder", [(900, 3, 12, 0)] * 3 + [(0, 0, 0, 0)] * 8)):
        print(f"   {label:12}: Rückgabe {income(f)}")


if __name__ == "__main__":
    main()
