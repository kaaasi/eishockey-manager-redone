"""B13 (A757): right click on a player in the line-up of the national team (em.exe Datei 0x40364, Ov 0x108).

The club and the national line-up are the same function 0x40364 (manager to move DGROUP 0xA70E; national coach = 5).
The script runs 0x40364 for manager 0 (club) and 5 (national coach) with five squad places (DGROUP 0x5DC3 + 0x618 ·
manager + 0x34 · place, field +0x0F ≠ 0 counts, 0x120:0x66 mode 0) and feeds one right click (event 3) per run into the
name column (x 0x3C) at every y from 20 to 200 (wait 0x0C097 at Datei 0x407D3, then read 0x0C068 at 0x4080E). Recorded: calls of the player info Datei 0x041AC (argument = place) –
0x041AC itself addresses the squad of DGROUP 0xA70E (Datei 0x041B4–0x041CD). Everything else is stubbed.
Usage: python3 b13_nationalaufstellung_rechtsklick.py [em.exe]
"""
import sys

import emu16

PATH = sys.argv[1] if len(sys.argv) > 1 else None
MOUSE = emu16.FAR_HEAP_SEG


class Done(emu16.Stop):
    pass


def one(manager, x, y, button=3):
    c = emu16.load(PATH)
    ov = next(r for r in c.regions if r.stub == 0x108)
    o120 = next(r for r in c.regions if r.stub == 0x120)
    c.allowed = [(ov.start, ov.end), (emu16.HEADER, emu16.HEADER + 0x46E * 16), (0xB0E6, 0xB447), (0x4BE17, o120.end)]
    c.int_hook = lambda cpu, n: (cpu.r.__setitem__("ax", 0), True)[1]
    for i in range(50):
        c.sdw(0x5A57 + 10 * i, 0xFFFF)
    c.sdw(0xC147, 0)
    c.sdw(0xC149, MOUSE)
    c.sdb(0xA70E, manager)
    for k in range(5):
        c.sdb(0x5DC3 + 0x618 * manager + 0x34 * k + 0x0F, 1)
    state = {"n": 0}

    def wait(cpu):  # 0x0C097: waits for the next click (loop head Datei 0x407D3)
        state["n"] += 1
        if state["n"] > 1:
            raise Done("second event")
        return event(cpu)

    def event(cpu):  # 0x0C068: reads the click that 0x0C097 waited for (Datei 0x4080E)
        cpu.ww(MOUSE, 0, y)
        cpu.ww(MOUSE, 2, x)
        cpu.sdw(0x5A16, button)
        cpu.sdw(0x5A24, 1)
        return button

    def info(cpu):
        raise Done(f"info {cpu.arg(0) & 0xFF}")
    c.hooks[0x0C068] = event
    c.hooks[0x0C097] = wait
    c.hooks[0x041AC] = info
    try:
        c.call(0x40364, 0)
    except Done as e:
        return str(e)
    except emu16.Stop as e:
        return str(e)
    return "zurück"


def main():
    for manager, label in ((0, "Verein (Manager 0)"), (5, "Nationalteam (Manager 5)")):
        hits = {}
        for y in range(20, 201):
            r = one(manager, 0x3C, y)
            if r.startswith("info"):
                hits.setdefault(r, []).append(y)
        rows = ", ".join(f"{k.replace('info', 'Platz')}: y {v[0]}–{v[-1]}" for k, v in hits.items()) or "keine"
        print(f"{label:26} Rechtsklick x 0x3C → Spielerinfo 0x041AC: {rows}")


if __name__ == "__main__":
    main()
