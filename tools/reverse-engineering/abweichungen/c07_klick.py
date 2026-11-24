"""C07 (A277): click on a club in the cup game tables of em.exe (Datei 0x32E23, Stub 0xE8:0x75).

Runs the screen with pairings in the cup lines (teams DGROUP 0x5CFB + 32 · Art + 2 · Zeile, Datei 0x335A0–0x335C9:
Art 0 = DEB-Pokal, 2 = Play-off) and scripted mouse events (event function 0x0C068 replaced: DGROUP 0x5A16 = 1 left,
3 right; position from the far pointer DGROUP 0xC147, +0 = y, +2 = x). Recorded: the rows drawn (y list on the stack
[bp−0x94], Datei 0x3364A–0x3365A), calls of the club window 0x4D385(team, page, 0, 0) (Datei 0x33AAF–0x33B30) and
whether the screen returns.
Usage: python3 c07_klick.py [em.exe]
"""
import sys

import emu16

PATH = sys.argv[1] if len(sys.argv) > 1 else None
NAME = {0: "DEB-Pokalspiele (Art 0)", 2: "Play-Off Spiele (Art 2)"}


def run(art, events):
    c = emu16.load(PATH)
    ov = next(r for r in c.regions if r.stub == 0xE8)
    c.allowed = [(ov.start, ov.end), (emu16.HEADER, emu16.HEADER + 0x46E * 16), (0xB0E6, 0xB447), (0xB954, 0xBAC8)]
    c.sdw(0xC147, 0)
    c.sdw(0xC149, emu16.FAR_HEAP_SEG)
    c.sdw(0x9657, 0x100)
    c.sdw(0x9659, emu16.FAR_HEAP_SEG)
    c.int_hook = lambda cpu, n: (cpu.r.__setitem__("ax", 0), True)[1]
    for i in range(50):
        c.sdw(0x5A57 + 10 * i, 0xFFFF)
    for k in range(16):                                 # pairing k: home 10 + k, away 40 + k
        c.sdb(0x5CFB + 32 * art + 2 * k, 10 + k)
        c.sdb(0x5CFB + 32 * art + 2 * k + 1, 40 + k)
    q = list(events)
    windows, info = [], {}

    def event(cc):
        if not q:
            raise emu16.Stop("keine Ereignisse mehr")
        b, x, y = q.pop(0)
        cc.ww(emu16.FAR_HEAP_SEG, 0, y)
        cc.ww(emu16.FAR_HEAP_SEG, 2, x)
        cc.sdw(0x5A16, b)
        return b

    def snap(cc):
        if "y" not in info:
            bp = cc.r["bp"]
            info["y"] = [cc.rb(cc.s["ss"], bp - 0x94 + i) for i in range(32)]
            info["head"] = cc.rb(cc.s["ss"], bp - 6)
    c.pre[0x33AAF] = snap
    c.hooks[0x0C068] = event
    c.hooks[0x4D385] = lambda cc: windows.append((cc.arg(0) & 0xFF, cc.arg(1) & 0xFF)) or 0
    try:
        c.call(0x32E23, art, 0)
        end = "zurück"
    except emu16.Stop as e:
        end = f"bleibt ({e})"
    return info, windows, end


def main():
    for art in (0, 2):
        info, _, _ = run(art, [(3, 0, 0)])
        ys = [y for y in info["y"] if y]
        print(f"{NAME[art]}: Kopfzeile bis y {info['head'] + 10}, Zeilen (Grundlinie) {ys}")
        cases = [
            ("links, Name links, Zeile 0", [(1, 40, ys[0] - 2)]),
            ("links, Name rechts, Zeile 0", [(1, 140, ys[0] - 2)]),
            ("links, Name links, Zeile 3", [(1, 40, ys[3])]),
            ("links, Name rechts, Zeile 3", [(1, 140, ys[3] - 5)]),
            ("links, Zeile 0, Grundlinie + 1", [(1, 40, ys[0] + 1)]),
            ("links, Kopfzeile", [(1, 40, info["head"] + 5)]),
            ("rechts, Name links, Zeile 0", [(3, 40, ys[0] - 2)]),
        ]
        for label, ev in cases:
            _, win, end = run(art, ev + [(3, 0, 0)] if ev[0][0] == 1 else ev)
            first = "Fenster " + ", ".join(f"0x4D385(Verein {t}, Seite {s})" for t, s in win) if win else "kein Fenster"
            print(f"   {label:32s} → {first}; Bildschirm {end if ev[0][0] == 3 else 'bleibt, Rechtsklick danach: ' + end}")


if __name__ == "__main__":
    main()
