"""Aufräumkonzept K2: penalty box check of the line-up 0x40364 (Ov 0x108) for the national coach.

1. The penalty routine 0x35818 (Datei 0x35AA8–0x35C02) marks the penalised player in the squad of the manager of the team
   (team field +0x16 = manager + 1, so the national coach 5 for his country, Datei 0x35AB8–0x35AC6): squad place
   DGROUP 0x5DC3 + 0x618 · manager + 0x34 · place, +0x09 |= 1 (read, see Datei 0x35B3C–0x35C02).
2. The script runs 0x40364 for manager 0 (club) and 5 (national coach) with the status bits +0x09 & 3 = 0, 1, 2 of place 0
   and one click into its row (x 0x3C, y 30; left and right) and records the message box 0x0C0B8(text 1, text 2)
   (Datei 0x40DAF–0x40E27 → 0x40EB9) and further stubbed calls. Same harness as b13.
Usage: python3 k2_strafbank_bundestrainer.py [em.exe]
"""
import sys

import emu16

PATH = sys.argv[1] if len(sys.argv) > 1 else None
MOUSE = emu16.FAR_HEAP_SEG


class Done(emu16.Stop):
    pass


def one(manager, status, button):
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
        c.sdb(0x5DC3 + 0x618 * manager + 0x34 * k + 0x0F, 1 + k)
    c.sdb(0x5DC3 + 0x618 * manager + 0x09, status)
    state = {"n": 0}
    msgs = []

    def wait(cpu):
        state["n"] += 1
        if state["n"] > 1:
            raise Done("nächstes Ereignis")
        return event(cpu)

    def event(cpu):
        cpu.ww(MOUSE, 0, 30)
        cpu.ww(MOUSE, 2, 0x3C)
        cpu.sdw(0x5A16, button)
        cpu.sdw(0x5A24, 1)
        return button

    def msg(cpu):
        a = cpu.cstr(cpu.arg(1), cpu.arg(0))
        b = cpu.cstr(cpu.arg(3), cpu.arg(2))
        if a.startswith("Auto-Aufstellung"):              # 0x375 + 2 · manager (Datei 0x40DD5), not part of K2
            return 0
        msgs.append(f"„{a}“ / „{b}“")
        raise Done("Meldung")
    c.hooks[0x0C068] = event
    c.hooks[0x0C097] = wait
    c.hooks[0x0C0B8] = msg
    mark = 0
    try:
        c.call(0x40364, 0)
    except emu16.Stop:
        pass
    _ = mark
    return msgs, c.calls


def main():
    for manager, label in ((0, "Verein (Manager 0)"), (5, "Nationalteam (Manager 5)")):
        for status in (0, 1, 2):
            for button, bl in ((1, "links"), (3, "rechts")):
                msgs, calls = one(manager, status, button)
                hit = sorted({f"{t:#07x}" for _, t in calls if t in (0x41533, 0x041AC, 0x4115F)})
                print(f"{label:25} Status {status} {bl:6}: Meldung {', '.join(msgs) or '-'}"
                      f"{'; Aufrufe ' + ' '.join(hit) if hit else ''}")


if __name__ == "__main__":
    main()
