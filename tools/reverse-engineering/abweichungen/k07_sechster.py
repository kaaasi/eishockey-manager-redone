"""K07 / A971, A972: sixth skater cell of the line-up 0x40364 (Ov 0x108) – flag [bp-0x0E] and grid [bp-0x46].

Harness as k07_meldung.py (manager 0, DGROUP 0x375 = 0, events through 0x0C068/0x0C097, mouse word 0 = y, word 2 = x).
Squad of manager 0 (DGROUP 0x5DC3 + 0x34 · place), roles from the pool (far DGROUP 0x8253, +0x18: 75 forward, 25 defender,
0 goalkeeper) and the real role function 0x04180: places 0–2 forwards with +0x15 = 0x81 (line 1, forward half), places
3–5 defenders with +0x15 = 1 (line 1, defence half; place 5 is the sixth skater), place 6 goalkeeper outside the line-up.
The grid [bp-0x46] has 5 blocks of 6 cells (cells 0–2 forwards, 3–5 defence, block 4 cell 4 = goalkeeper [bp-0x2A]);
a cell holds place + 1. Read in the hooks with the bp of 0x40364 (all events are called directly from 0x40364).
Runs (each: fresh image, the screen built once, then the clicks):
  a) left click on forward cell 0 of line 1 (x 0x108, y 0x1A; Datei 0x4096D–0x409CE),
  b) left click on defence cell 3 of line 1 (x 0x108, y 0x28; with the flag set three cells, Datei 0x40A28–0x40A5F),
  c) as a), then line button 5 (goalkeeper block 4: button check 0x0B0E6 returns 1 only for index 4 at Datei 0x408C3) and
     a left click on the list row of place 6 (x 0x3C, y 0x1D + 6 · 6 + 3),
  d) as a), screen left, then 0x40364 called again with the squad as left by a) (new build, Datei 0x403DB–0x404FD).
Printed: grid of line 1 and block 4, the flag, +0x15 of places 0–6, messages (0x0C0B8) and the numbers drawn into the
cells of line 1 (0x0B651, x/y; first build: the first six).
Usage: python3 k07_sechster.py [em.exe]
"""
import sys

import emu16

PATH = sys.argv[1] if len(sys.argv) > 1 else None
MOUSE = emu16.FAR_HEAP_SEG
POOL = 0x1000
ROLES = [75, 75, 75, 25, 25, 25, 0]
FWD0 = (0x108, 0x1A)
DEF3 = (0x108, 0x28)
GOALIE_ROW = (0x3C, 0x1D + 6 * 6 + 3)                 # list rows 6 pixels high from y 0x1D (list 0x41683)
LINE_BUTTON = (0xFB, 0x20)
IDLE = (0, 0, 0)


class Done(emu16.Stop):
    pass


def place(k):
    return 0x5DC3 + 0x34 * k


def prepare():
    c = emu16.load(PATH)
    ov = next(r for r in c.regions if r.stub == 0x108)
    o120 = next(r for r in c.regions if r.stub == 0x120)
    c.allowed = [(ov.start, ov.end), (emu16.HEADER, emu16.HEADER + 0x46E * 16), (0xB0E6, 0xB447), (0x4BE17, o120.end)]
    c.int_hook = lambda cpu, n: (cpu.r.__setitem__("ax", 0), True)[1]
    for i in range(50):
        c.sdw(0x5A57 + 10 * i, 0xFFFF)
    c.sdw(0xC147, 0)
    c.sdw(0xC149, MOUSE)
    c.sdb(0xA70E, 0)
    c.sdb(0x375, 0)
    c.sdw(0x8253, POOL)
    c.sdw(0x8255, emu16.FAR_HEAP_SEG)
    for k, role in enumerate(ROLES):
        c.wb(emu16.FAR_HEAP_SEG, POOL + 0x21 * (k + 1) + 0x18, role)
        c.sdb(place(k) + 0x0F, k + 1)
        c.sdb(place(k) + 0x15, (0x81 if role == 75 else 1) if role else 0)
        c.sdb(place(k) + 0x0A, 1 if role else 0)
    return c


def screen(c, clicks):
    """Runs 0x40364 once; clicks = list of (x, y, button) or "line4". Returns snapshots after every event."""
    queue = list(clicks)
    snaps, msgs, drawn = [], [], []
    state = {"line4": False}

    def snap(cpu):
        bp, ss = cpu.r["bp"], cpu.s["ss"]
        g = [cpu.rb(ss, (bp - 0x46 + i) & 0xFFFF) for i in range(30)]
        return {"line1": g[0:6], "gk": g[24 + 4], "flag": cpu.rb(ss, (bp - 0x0E) & 0xFFFF),
                "x15": [cpu.db(place(k) + 0x15) for k in range(len(ROLES))]}

    def event(cpu):
        snaps.append(snap(cpu))
        if not queue:
            raise Done("Ende der Klicks")
        item = queue.pop(0)
        state["line4"] = item == "line4"
        x, y, b = (*LINE_BUTTON, 1) if item == "line4" else item
        cpu.ww(MOUSE, 0, y)
        cpu.ww(MOUSE, 2, x)
        cpu.sdw(0x5A16, b)
        cpu.sdw(0x5A24, 1 if b else 0)
        return b

    def button(cpu):
        if cpu.calls[-1][0] == 0x408C3:                    # line buttons 0–4 (Datei 0x408A6–0x4094E)
            bp, ss = cpu.r["bp"], cpu.s["ss"]
            return 1 if state["line4"] and cpu.rw(ss, (bp - 0x1A) & 0xFFFF) == 4 else 0
        return None

    def msg(cpu):
        msgs.append(f"„{cpu.cstr(cpu.arg(1), cpu.arg(0))}“ / „{cpu.cstr(cpu.arg(3), cpu.arg(2))}“")
        return 0

    def number(cpu):
        x, y = cpu.arg(4), cpu.arg(5)
        if 0x100 <= x < 0x140 and y < 0x30:
            drawn.append((x, y))                           # value via 0x38:0x2C2 (stubbed), only the place
        return 0

    c.hooks[0x0C068] = event
    c.hooks[0x0C097] = event
    c.hooks[0x0C0B8] = msg
    c.hooks[0x0B651] = number
    real_button = c.hooks.get(0x0B0E6)

    def button_hook(cpu):
        res = button(cpu)
        if res is None:
            return real_button(cpu) if real_button else 0
        return res
    c.hooks[0x0B0E6] = button_hook
    try:
        c.call(0x40364, 0)
    except Done:
        pass
    return snaps, msgs, drawn


def show(label, s):
    cells = " ".join(f"{v:>2}" for v in s["line1"])
    x15 = " ".join(f"{v:#04x}" for v in s["x15"])
    print(f"  {label:<26} Reihe 1 Zellen 0–5: {cells} | Torhüterzelle {s['gk']} | [bp-0x0E] {s['flag']} | +0x15 Plätze 0–6: {x15}")


def main():
    print("a) Klick auf Sturmzelle 0 der Reihe 1")
    snaps, msgs, _ = screen(prepare(), [IDLE, (*FWD0, 1), IDLE])
    show("nach dem Aufbau", snaps[1])
    show("nach dem Klick", snaps[-1])
    print(f"  Meldungen: {' → '.join(msgs) or '-'}")

    print("b) Klick auf Abwehrzelle 3 der Reihe 1")
    snaps, msgs, _ = screen(prepare(), [IDLE, (*DEF3, 1), IDLE])
    show("nach dem Aufbau", snaps[1])
    show("nach dem Klick", snaps[-1])
    print(f"  Meldungen: {' → '.join(msgs) or '-'}")

    print("c) wie a), dann Reihe 5 (Torhüter) und Torhüter (Platz 6) aus der Liste")
    snaps, msgs, _ = screen(prepare(), [IDLE, (*FWD0, 1), IDLE, "line4", IDLE, (*GOALIE_ROW, 1), IDLE])
    show("nach dem Sturmklick", snaps[3])
    show("nach dem Torhüterklick", snaps[-1])
    print(f"  Meldungen: {' → '.join(msgs) or '-'}")

    print("d) wie a), Bildschirm neu aufgebaut")
    c = prepare()
    _, _, drawn = screen(c, [IDLE, (*FWD0, 1), IDLE])
    print(f"  Zellen mit Nummer beim ersten Aufbau (0x0B651, x/y): {cells(drawn[:6])}")
    snaps, msgs, drawn = screen(c, [IDLE])
    show("nach dem neuen Aufbau", snaps[0])
    print(f"  Zellen mit Nummer beim neuen Aufbau (0x0B651, x/y): {cells(drawn)}")


def cells(drawn):
    return ", ".join(f"{x:#x}/{y:#x}" for x, y in drawn) or "-"


if __name__ == "__main__":
    main()
