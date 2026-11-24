"""C07 (A482, A759, A924, A098): result marks of the minute path after the overtime of a knock-out game.

Replays the original sequence of the minute path for one pairing (row r of the cup lines DGROUP 0x5C4B, teams
DGROUP 0x5CFB + 32 r, start pairing DGROUP 0xCFF8 + r), as the match day 0x36B5D runs it (Datei 0x37176–0x37295):
1. 0x32AA3(mask, limit, switch 0, row) after minute 60 – a draw gets a mark (home byte + 30);
2. 0x37A62(…, minutes 61–65) and 0x37A62(…, minutes 66–70) – the overtime; the chance roll 0x09F5C and the shot 0xA484
   are hooked: a chance for the given side in the given minute, every shot a goal;
3. 0x32AA3(mask, limit, switch 1, row) – still a draw: second mark and penalty shoot-out 0x3901B (path without
   spectator: random(2, 5) per side until unequal, Datei 0x39105–0x3917B; result Datei 0x39770–0x397B3).
The script prints the home/away bytes of the pairing after each step and the scored-game list (DGROUP 0xA928 home,
0xA918 away, length 0xA968) written by 0x36AAA (Datei 0x38402) and copied back by 0x369C7 (Datei 0x38616).
Case "Zeile 0 DEB-Pokal" uses mask 1 (path Datei 0x32AF4–0x32BAA), rows 2/3 mask 4/8 (path Datei 0x32C7F–0x32D84).
Usage: python3 c07_marken.py [em.exe]
"""
import sys

import emu16

PATH = sys.argv[1] if len(sys.argv) > 1 else None
MASK = {0: 1, 2: 4, 3: 8}
NAME = {0: "DEB-Pokal (Zeile 0)", 2: "Play-off (Zeile 2)", 3: "WM-Endrunde (Zeile 3)"}


def cpu():
    c = emu16.load(PATH)
    ov_f0 = next(r for r in c.regions if r.stub == 0xF0)
    ov_e8 = next(r for r in c.regions if r.stub == 0xE8)
    c.allowed = [(0x369C7, 0x36B5D), (0x37A62, 0x386D1), (0x32AA3, 0x32E23), (0x3901B, 0x397BB),
                 (emu16.HEADER, emu16.HEADER + 0x46E * 16)]
    _ = ov_f0, ov_e8
    c.int_hook = lambda cpu_, n: (cpu_.r.__setitem__("ax", 0), True)[1]
    c.sdw(0x9657, 0)
    c.sdw(0x9659, emu16.FAR_HEAP_SEG)                  # team table: all fields 0 (no national coach, not watched)
    c.max_steps = 5_000_000
    return c


def run(row, h, g, goal_minute=None, side=1):
    """side: 1 = home chance, 2 = away chance in goal_minute (0x09F5C bit 0 / bit 1)."""
    c = cpu()
    base = 32 * row
    c.sdb(0xCFF8 + row, 0)                              # first pairing of the row
    c.sdb(0x5CFB + base, 52)
    c.sdb(0x5CFB + base + 1, 53)
    for i in range(2, 32):
        c.sdb(0x5CFB + base + i, 0xFF)
    c.sdb(0x5C4B + base, h)
    c.sdb(0x5C4B + base + 1, g)
    c.sdb(0xD074, 0)                                    # DEB: round bytes (DGROUP 0xD014 + 32 · round) stay 0 < 100
    c.sdb(0xCCA3, 0)
    c.sdb(0xA968, 0)
    log, shoot = [], []
    minute = {"m": 0}

    def chance(cc):
        minute["m"] += 0
        m = c.db(0xE19E)
        return side if goal_minute is not None and m == goal_minute else 0
    c.hooks[0x09F5C] = chance
    c.hooks[0x0A484] = lambda cc: 1                     # every shot is a goal
    c.pre[0x3901B] = lambda cc: shoot.append((cc.arg(0) & 0xFF, cc.arg(1) & 0xFF,
                                              c.db(0x5C4B + base), c.db(0x5C4C + base)))
    rolls = iter([3, 2, 4, 4, 2, 5])                    # random(2, 5) of the shoot-out without spectator (Datei 0x3911E)
    used = []

    def rnd(cc):
        if (cc.arg(0), cc.arg(1)) != (2, 5):
            return 0
        used.append(next(rolls, 2))
        return used[-1]
    c.hooks[0x0C037] = rnd
    mask = MASK[row]

    def state(label):
        log.append(f"{label}: Heim {c.db(0x5C4B + base)}, Gast {c.db(0x5C4C + base)}")

    r0 = c.call(0x32AA3, mask, 2, 0, row) & 0xFF
    state(f"nach 0x32AA3(Schalter 0) Rückgabe {r0}")
    if r0:
        # the minute counter DGROUP 0xE19E is set by 0x37A62 per minute (Datei 0x37CBE ff.)
        c.call(0x37A62, 0, mask, 1, 0x3D, 0x41, 0, row)
        state("nach 0x37A62(61–65)")
        c.call(0x37A62, 0, mask, 1, 0x42, 0x46, 0, row)
        state("nach 0x37A62(66–70)")
        r1 = c.call(0x32AA3, mask, 2, 1, row) & 0xFF
        state(f"nach 0x32AA3(Schalter 1) mit Penaltyschießen 0x3901B, Rückgabe {r1}")
    n = c.db(0xA968)
    lst = [(c.db(0xA928 + i), c.db(0xA918 + i)) for i in range(n)]
    return log, shoot, lst, used


def marks(v):
    k = 0
    while v > 29:
        v -= 30
        k += 1
    return v, k


def main():
    print("Minutenweg nach Minute 60 (Stand 3:3 bzw. 4:2), Verlängerung 61–70, Chance = Tor")
    for row in (0, 2, 3):
        for (h, g, gm, side, label) in ((3, 3, 0x3F, 1, "3:3, Heimtor Minute 63"),
                                        (3, 3, 0x44, 2, "3:3, Gasttor Minute 68"),
                                        (3, 3, None, 1, "3:3, kein Tor"),
                                        (4, 2, None, 1, "4:2 nach 60")):
            log, shoot, lst, used = run(row, h, g, gm, side)
            print(f"  {NAME[row]}, {label}")
            for line in log:
                print(f"     {line}")
            if lst:
                print(f"     gewertete Spiele 0xA928/0xA918: {lst}")
            if shoot:
                print(f"     0x3901B(Zeile, Paarung) gerufen bei Stand Heim/Gast {shoot[0][2:]}; Würfe random(2, 5) Heim/Gast: {used}")
            fin = log[-1].split(": ", 1)[1]
            hb = int(fin.split(",")[0].split()[1])
            gb = int(fin.split(",")[1].split()[1])
            hv, k = marks(hb)
            print(f"     → Endstand {hv}:{gb}, Marken {k}")


if __name__ == "__main__":
    main()
