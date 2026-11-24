"""K07 / A807: message "Auto-Aufstellung ist deaktiviert" together with another message in the line-up 0x40364.

Same harness as k2_strafbank_bundestrainer.py (Ov 0x108, manager 0, five squad places, one left or right click into the
row of place 0 at x 0x3C, y 30), with the setting DGROUP 0x375 + 2 · manager = 1 or 0 before the click and status +0x09 & 3 of
place 0 = 0, 1, 2. The message box 0x0C0B8(text 1, text 2) is recorded; the switch-off message returns, the next
message ends the run, so the messages of the click are printed in order (Datei 0x40DD5–0x40DF9 switch-off with
message DGROUP 0x1249, then Datei 0x40E01–0x40E27 → 0x40EB9).
Printed: the messages and the setting afterwards.
Usage: python3 k07_meldung.py [em.exe]
"""
import sys

import emu16

PATH = sys.argv[1] if len(sys.argv) > 1 else None
MOUSE = emu16.FAR_HEAP_SEG


class Done(emu16.Stop):
    pass


def one(manager, status, button, auto):
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
    c.sdb(0x375 + 2 * manager, auto)
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
        msgs.append(f"„{a}“ / „{b}“")
        if not a.startswith("Auto-Aufstellung"):
            raise Done("Meldung")                      # the click ends with this message (Datei 0x40EB9)
        return 0
    c.hooks[0x0C068] = event
    c.hooks[0x0C097] = wait
    c.hooks[0x0C0B8] = msg
    try:
        c.call(0x40364, 0)
    except emu16.Stop:
        pass
    return msgs, c.db(0x375 + 2 * manager)


def main():
    for auto in (1, 0):
        for button, label in ((1, "links "), (3, "rechts")):
            for status in (0, 1, 2):
                msgs, after = one(0, status, button, auto)
                print(f"Einstellung 0x375 vorher {auto}, Status {status}, {label}: Meldungen {' → '.join(msgs) or '-'}; "
                      f"0x375 nachher {after}")


if __name__ == "__main__":
    main()
