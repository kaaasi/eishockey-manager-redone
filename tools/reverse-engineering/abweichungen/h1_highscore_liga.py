"""Befund #1007 (R25-H1): league byte of the season history, Datei 0x55C0E–0x55C56 (season change 0x55B36–0x55DBD).

The script enters the writer at Datei 0x55C0E with its locals ([bp−0x0A] = place or play-off code, si = season slot) and stops after
the `add [bx−0x698D], al` at Datei 0x55C52, which adds DGROUP 0x7C0 + league (0, 12, 24; the manager's league index at
0x9746 + 0x1AF · m). Result: byte = place + 12 · league, for every league (here: place 1, 5, 12).
Usage: python3 h1_highscore_liga.py [em.exe]
"""
import sys

import emu16

PATH = sys.argv[1] if len(sys.argv) > 1 else None


def run(league, place, m=0, slot=5):
    c = emu16.load(PATH)
    c.allowed = [(0x55C0E, 0x55C56)]
    c.sdb(0xA70E, m)
    c.sdb(0x9746 + 0x1AF * m, league)   # league index of manager m (0 Bundesliga, 1 Zweite, 2 Oberliga)
    c.sdb(0x966F + 0x1AF * m, 0xFE)     # club ≠ champion [0xD0E6]
    c.sdb(0xD0E6, 0xFD)
    cs, ip = c.at(0x55C0E)
    c.push(0xFFFF)
    c.push(0xFFFF)
    c.push(0)
    c.r["bp"] = c.r["sp"]
    c.r["sp"] = (c.r["sp"] - 0x20) & 0xFFFF
    c.ww(c.s["ss"], c.r["bp"] - 0x0A, place)
    c.r["si"] = slot
    c.s["cs"], c.ip = cs, ip
    c.stop_at = {0x55C56}
    try:
        c.run()
    except emu16.Stop:
        pass
    return c.db(0x9673 + 0x1AF * m + 4 * slot)


def main():
    tab = [emu16.load(PATH).db(0x7C0 + i) for i in range(3)]
    print("DGROUP 0x7C0 =", tab)
    for league, name in enumerate(["Bundesliga", "Zweite Liga", "Oberliga"]):
        print(f"{name:12s}", ", ".join(f"Platz {p} → Byte {run(league, p)}" for p in (1, 5, 12)))


main()
