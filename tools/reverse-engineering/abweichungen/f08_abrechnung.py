"""F08 / A468, A695: match day 0x36B5D – settlement 0x544C0 of every pairing before the minute loop 0x37A62.

Runs 0x36B5D (Ov 0xF0, league path, argument 0) with only its own code and the C runtime real; every other call is a stub
(return 0) and is printed in order (directly repeated calls collapsed), up to the first call of the
minute loop 0x37A62. Shows: the settlement 0x544C0 (spectators, short-handed check) runs for each of the six pairings of
the day (Datei 0x36DBD) before any minute is played, and between the last settlement and the first minute only the
stubbed calls listed run (no menu 0x44E22, no save).
Usage: python3 f08_abrechnung.py [em.exe]
"""
import sys

import emu16

PATH = sys.argv[1] if len(sys.argv) > 1 else None
NAMES = {0x544C0: "Abrechnung 0x544C0", 0x37A62: "Minutenschleife 0x37A62", 0x44E22: "Hauptmenü 0x44E22"}


def main():
    c = emu16.load(PATH)
    c.allowed = [(0x36B5D, 0x378C5), (emu16.HEADER, emu16.HEADER + 0x46E * 16)]
    c.sdb(0x386, 1)
    c.hooks[0x37A62] = lambda cc: 0
    c.max_steps = 300_000
    try:
        c.call(0x36B5D, 0)
    except emu16.Stop:
        pass
    out = []
    for src, t in c.calls:
        label = f"{NAMES.get(t, f'{t:#07x}')} (aus Datei {src:#07x})"
        if out and out[-1][0] == label:
            out[-1][1] += 1
        else:
            out.append([label, 1])
        if t == 0x37A62:
            break
    for label, n in out:
        print(f"   {label}{f' × {n}' if n > 1 else ''}")
    targets = [t for _, t in c.calls]
    first = targets.index(0x37A62)
    print(f"Abrechnungen vor der ersten Minute: {targets[:first].count(0x544C0)}; "
          f"Hauptmenü/Sicherung davor: {'ja' if 0x44E22 in targets[:first] else 'nein'}")


if __name__ == "__main__":
    main()
