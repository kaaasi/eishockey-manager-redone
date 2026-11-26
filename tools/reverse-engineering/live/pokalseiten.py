"""S04 (A292, A436): the cup games page 0x32E23 (Stub 0xE8:0x75) with and without a minute.

The page selector 0x3A88A calls 0x32E23(competition, minute) after each period of a live day (`drittelseiten.py`). This
script runs 0x32E23 from the bytes of em.exe (overlay 0xE8 and the drawing helpers of the base code) once with minute 0
(table from the main menu) and once with minute 20/60, logs every text drawn by 0x0B6A5 (centred: x1, x2, y) and
0x0B651 (left: x, y) and the window frame 0x0B155, and leaves at the first event (right click). Prepared memory:
pairings in the cup lines (teams DGROUP 0x5CFB + 32 · line, results DGROUP 0x5C4B + 32 · line), DEB-Pokal round bytes
DGROUP 0xD014 + 32 · date (date DGROUP 0xD074), play-off results DGROUP 0xCCB2 + 14 · game (game DGROUP 0xCCA3), World Cup
stage DGROUP 0x7AE = 5. Club names are not read (the team table is empty), so only numbers, positions and fixed texts
of the page are printed. Result texts appear as "%h:%g" (the number formatting does not run in the emulator; h and g are the
result bytes, e.g. "%3:%1" = live score 3:1 of the current date).
Usage: python3 tools/reverse-engineering/live/pokalseiten.py [em.exe]
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "abweichungen"))
import emu16  # noqa: E402

PATH = sys.argv[1] if len(sys.argv) > 1 else None


def run(art, minute, pairs=2, live=(3, 1), date=1, game=1):
    c = emu16.load(PATH)
    ov = next(r for r in c.regions if r.stub == 0xE8)
    c.allowed = [(ov.start, ov.end), (emu16.HEADER, emu16.HEADER + 0x46E * 16), (0x3800, 0x3C00), (0xB954, 0xBAC8),
                 (0x37A4A, 0x37A62)]
    c.sdw(0xC147, 0)
    c.sdw(0xC149, emu16.FAR_HEAP_SEG)
    c.sdw(0x9657, 0x100)
    c.sdw(0x9659, emu16.FAR_HEAP_SEG)
    c.int_hook = lambda cpu, n: (cpu.r.__setitem__("ax", 0), True)[1]
    c.max_steps = 20_000_000
    for i in range(50):
        c.sdw(0x5A57 + 10 * i, 0xFFFF)
    for line in range(4):
        for i in range(32):
            c.sdb(0x5CFB + 32 * line + i, 0xFF)
            c.sdb(0x5C4B + 32 * line + i, 0)
    for k in range(pairs):
        c.sdb(0x5CFB + 32 * art + 2 * k, 10 + k)          # DEB: Bundesliga clubs (< 20) → first group
        c.sdb(0x5CFB + 32 * art + 2 * k + 1, 12 + k)
        c.sdb(0x5C4B + 32 * art + 2 * k, live[0] + k)      # running score of the current date (0x369C7(2, 1))
        c.sdb(0x5C4B + 32 * art + 2 * k + 1, live[1])
    c.sdb(0xD074, date)
    for d in range(3):
        for k in range(2 * pairs):
            c.sdb(0xD014 + 32 * d + k, 0 if d >= date else 4 + d)
    c.sdb(0xCCA3, game)
    for g in range(5):
        for k in range(14):
            c.sdb(0xCCB2 + 14 * g + k, 0 if g >= game else 5)
    c.sdb(0x7AE, 5)
    c.sdb(0xCFF8 + art, 0)
    c.sdb(0xCFFA, 0)
    c.sdw(0x3B7, 100)
    out = []

    def text(kind):
        def h(cc):
            s = cc.cstr(cc.arg(3), cc.arg(2))
            if kind == "zentriert":
                out.append((kind, cc.arg(4), cc.arg(5), cc.arg(6), s))
            else:
                out.append((kind, cc.arg(4), cc.arg(5), s))
            return 0
        return h

    c.hooks[0x0B6A5] = text("zentriert")
    c.hooks[0x0B651] = text("links")
    c.hooks[0x0B155] = lambda cc: out.append(("Rahmen", cc.arg(0), cc.arg(1), cc.arg(2), cc.arg(3))) or 0

    def event(cc):
        raise emu16.Stop("Ereignis")
    c.hooks[0x0C068] = event
    try:
        c.call(0x32E23, art, minute)
    except emu16.Stop:
        pass
    return out


def main():
    for art, name in ((0, "DEB-Pokalspiele (Wettbewerb 0), 2. Termin der Runde"),
                      (2, "Play-Off Spiele (Wettbewerb 2), 2. Spiel"),
                      (3, "WM-Endrundenspiele (Wettbewerb 3)")):
        print(name)
        for minute in (0, 20, 60):
            print(f"   Minute {minute}:")
            for e in run(art, minute):
                print("     ", e)


if __name__ == "__main__":
    main()
