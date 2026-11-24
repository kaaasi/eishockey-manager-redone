"""B18 (A297, A301, A302, A331): main menu set-up of em.exe – tile mark, manager head, rank curve and lamps of the
national coach. Replays the original code (harness `hauptmenue.py`, direct calls for 0x445E0 and 0x42996).

1. Markierung (A301): rectangle outlines Datei 0x0B5A7 (surface, x1, y1, x2, y2, colour) drawn by the click on a big tile.
2. Leistenfeld neben dem Kopf (A297): tile selected, then click into x 0xE9–0x10F / y 0x22–0x47, then on sub-menu entry 1.
3. Tabellenkurve (A302): Datei 0x445E0 called directly with own/opponent = club or national team (index ≥ DGROUP 0x6AD);
   counted are the drawing calls (line 0x0B7B5, pen/line 0x13495/0x1322A/0x13243, text 0x0B6A5). Also the arguments the
   info bar passes when the national coach (DGROUP 0xA70E = 5) is the manager to move.
4. Lampen (A331): Datei 0x42996 for manager 5 (national coach) and manager 0 with an empty squad; colour per lamp from
   the blit Datei 0x139C9 (source y 0xDB + 6 · green).
Usage: python3 b18_hauptmenue_aufbau.py [em.exe]
"""
import sys

import emu16
import hauptmenue

PATH = sys.argv[1] if len(sys.argv) > 1 else None
TILES = [(40, 100), (120, 100), (200, 100), (270, 100), (40, 170), (120, 170), (200, 170)]
DRAW = {0x0B7B5: "Linie", 0x13495: "Farbe", 0x1322A: "Stift", 0x13243: "Linie2", 0x0B6A5: "Text"}


def recorder(store, name):
    def hook(c):
        store.append((name, [c.arg(i) for i in range(7)]))
        return 0
    return hook


def part1():
    print("1. Markierung der großen Kachel (Umriss 0x0B5A7: x1, y1, x2, y2, Farbe)")
    for k, (x, y) in enumerate(TILES):
        rec = []
        c, outs = hauptmenue.run([(1, x, y)], path=PATH, setup=lambda c: c.hooks.__setitem__(0x0B5A7, recorder(rec, "U")),
                                 on_top=lambda c: rec.clear())
        print(f"   Kachel {k + 1}: " + "; ".join(f"({a[2]}|{a[3]})-({a[4]}|{a[5]}) Farbe {a[6]}" for _, a in rec))
    rec = []
    c, outs = hauptmenue.run([(1, *TILES[0]), (1, *TILES[1])], path=PATH,
                             setup=lambda c: c.hooks.__setitem__(0x0B5A7, recorder(rec, "U")), on_top=lambda c: rec.clear())
    print("   Kachel 1, dann Kachel 2: " + "; ".join(f"({a[2]}|{a[3]})-({a[4]}|{a[5]}) Farbe {a[6]}" for _, a in rec))


def part2():
    print("2. Klick ins Leistenfeld neben dem Kopf (250 | 40) bei gewählter Kachel 1")
    c, outs = hauptmenue.run([(1, *TILES[0]), (1, 250, 40), (1, 27, 220)], path=PATH)
    for name, o in zip(("Kachel 1", "Leistenfeld", "Eintrag 1"), outs):
        sel = "-" if o.selected is None else f"{o.selected:#x}"
        bar = "Leiste 0x4B5F4" if any(d == 0x4B5F4 for _, d in o.calls) else ""
        scr = [f"{d:#x}" for _, d in o.calls if d in (0x58A57,)]
        print(f"   {name:12} {o.kind:8} Auswahl {sel:6} {bar} {' '.join(scr)}")


def part3():
    print("3. Tabellenkurve 0x445E0(x 3, y -2, eigen, Gegner, …): Zeichenaufrufe")
    for own, opp, label in ((0, 1, "Verein 0 gegen Verein 1"), (52, 1, "Nationalteam 52 gegen Verein 1"),
                            (0, 53, "Verein 0 gegen Nationalteam 53"), (52, 53, "Nationalteam gegen Nationalteam")):
        c = emu16.load(PATH)
        ov = next(r for r in c.regions if r.stub == 0x110)
        c.allowed = [(ov.start, ov.end), (emu16.HEADER, emu16.HEADER + 0x46E * 16), (0x4D2F0, 0x4D316), (0x4BDE6, 0x4BE17)]
        c.sdb(0x7A9, 5)                      # 5th match day: curve with four points
        calls = []
        for a, n in DRAW.items():
            c.hooks[a] = recorder(calls, n)
        c.call(0x445E0, 3, 0xFFFE, own, opp, 1, 1)
        kinds = {}
        for n, _ in calls:
            kinds[n] = kinds.get(n, 0) + 1
        print(f"   {label:32} {sum(kinds.values()):3} Aufrufe {kinds}")
    args = []

    def log(c):
        args.append([c.arg(i) for i in range(6)])

    def setup(c):
        c.sdb(0xA70E, 5)                     # manager to move: national coach
        c.sdb(0x966F + 5 * 0x1AF, 52)        # his team: national team 52
        c.pre[0x445E0] = log
    hauptmenue.run([], path=PATH, setup=setup, manager=5)
    for a in args:
        print(f"   Infoleiste Manager 5: 0x445E0(x {a[0]}, y {a[1] - 0x10000}, eigen {a[2]}, Gegner {a[3]}, …)")


def part4():
    print("4. Lampen 0x42996 bei leerem Kader (rot/grün je Lampe 0–5)")
    for m in (5, 0):
        c = emu16.load(PATH)
        ov = next(r for r in c.regions if r.stub == 0x108)
        o120 = next(r for r in c.regions if r.stub == 0x120)
        c.allowed = [(ov.start, ov.end), (emu16.HEADER, emu16.HEADER + 0x46E * 16), (0x4BE17, o120.end)]
        c.sdb(0xA70E, m)
        lamps = {}

        def blit(c):
            dy, sy = c.arg(7), c.arg(3)
            lamps[(dy - 0x21) // 7] = "grün" if sy == 0xDB + 6 else "rot"
            return 0
        c.hooks[0x139C9] = blit
        c.call(0x42996)
        print(f"   Manager {m}: " + " ".join(f"{k}:{v}" for k, v in sorted(lamps.items())))


if __name__ == "__main__":
    part1()
    part2()
    part3()
    part4()
