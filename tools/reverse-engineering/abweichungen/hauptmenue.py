"""Shared harness: run the main menu of em.exe (Ov 0x110, Datei 0x44E22) with scripted mouse events.

Executed for real: Ov 0x110, the C runtime (Basis 0x0 and 0x238) and the button functions Datei 0xB0E6–0xB446
(hit test 0xB0E6, create 0xB155). Everything else (drawing, image loading, other overlays = the screens behind the
tiles) is stubbed and recorded. The mouse is read in the loop head Datei 0x4535B from the far pointer DGROUP 0xC147
(word +0 = y, +2 = x); the event function Datei 0x0C068 is replaced: it sets DGROUP 0x5A16 to the button
(1 = left, 3 = right, as in the original) and returns it.
"""
from __future__ import annotations

import emu16

LOOP_HEAD = 0x4535B     # top of the event loop
LEAVE = 0x4592F         # common exit of the menu ("Tag weiter" and other exits), button in [bp-7]
MENU = 0x44E22
MOUSE_SEG = emu16.FAR_HEAP_SEG


class Outcome:
    def __init__(self):
        self.kind = ""
        self.detail = ""
        self.calls: list[tuple[int, int]] = []
        self.selected = None   # [bp-6] at the next loop head: selected tile index (0xFFFF = none)


def run(events, setup=None, manager=0, path=None, stop_extra=(), on_top=None):
    """events: list of (button, x, y). Returns (cpu, list of per-event Outcome)."""
    c = emu16.load(path)
    ov = next(r for r in c.regions if r.stub == 0x110)
    c.allowed = [(ov.start, ov.end), (emu16.HEADER, emu16.HEADER + 0x46E * 16), (0xB0E6, 0xB447)]
    c.sdw(0xC147, 0)
    c.sdw(0xC149, MOUSE_SEG)
    c.int_hook = lambda cpu, n: (cpu.r.__setitem__("ax", 0), True)[1]
    for i in range(50):                      # button table DGROUP 0x5A57 (10 bytes each): free = x 0xFFFF
        c.sdw(0x5A57 + 10 * i, 0xFFFF)
    c.hooks[0x0C17D] = "stub"
    c.hooks[0x0C729] = "stub"
    if setup:
        setup(c)
    state = {"i": -1, "b": 0, "mark": 0}
    outcomes: list[Outcome] = []

    def top(cpu):
        if state["i"] >= 0:
            o = outcomes[-1]
            o.kind = o.kind or "Schleife"
            o.calls = cpu.calls[state["mark"]:]
            o.selected = cpu.rw(cpu.s["ss"], cpu.r["bp"] - 6)
        state["i"] += 1
        if state["i"] >= len(events):
            raise emu16.Stop("events done")
        if on_top:
            on_top(cpu)
        b, x, y = events[state["i"]]
        cpu.ww(MOUSE_SEG, 0, y)
        cpu.ww(MOUSE_SEG, 2, x)
        state["b"] = b
        state["mark"] = len(cpu.calls)
        outcomes.append(Outcome())

    def event(cpu):
        b = state["b"]
        state["b"] = 0
        cpu.sdw(0x5A16, b)
        cpu.sdw(0x5A26, 0)
        cpu.sdw(0x5A24, 1 if b else 0)
        return b

    c.pre[LOOP_HEAD] = top
    c.hooks[0x0C068] = event
    c.stop_at = {LEAVE, *stop_extra}
    try:
        c.call(MENU, manager)
    except emu16.Stop as e:
        if outcomes and str(e).startswith("stop at"):
            o = outcomes[-1]
            o.kind = "Ende" if c.here() == LEAVE else f"Halt {c.here():#x}"
            o.detail = f"[bp-7] = {c.rb(c.s['ss'], c.r['bp'] - 7)}"
            o.calls = c.calls[state["mark"]:]
    return c, outcomes
