"""A408: does the daily squad run 0x07169 of the original depend on the game mode (DGROUP 0xD0B9)?

Replays 0x07169 (Datei 0x07169-0x07C12) from the bytes of em.exe for manager 0 with the full state of an original save
(default FILE19: mode 1 "nur Weltkugel", one manager, WM loop day 217) loaded into DGROUP and the far blocks (0x9657 clubs,
0x8253 pool). Everything outside 0x07169-0x07C12 and the place counter 0x4BE17 is stubbed and recorded (random 0x0C037 returns its lower bound).
The run is done twice: with the mode byte of the save and with mode 0xD0B9 forced to 0 (flag and globe). Printed: the
stubbed calls (call site -> target) of both runs and whether they are equal. Calls into the training (0x0659C), the
healing (0x07C12) and the contract part (0x073E3-0x07AEF) show which parts of the squad run happen for record 0.
Usage: python3 a408_tageslauf_modus1.py [save.HOK] [em.exe]
"""
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parent / "abweichungen"))
import emu16  # noqa: E402
import hokraw  # noqa: E402

SAVE = sys.argv[1] if len(sys.argv) > 1 else str(Path.home() / "referenz/original/spielstaende/FILE19.HOK")
EXE = sys.argv[2] if len(sys.argv) > 2 else None
NAMES = {0x0659C: "Training", 0x07C12: "Heilung", 0x0C037: "random", 0x4B270: "Gebotsverfall", 0x4BE17: "Platzzähler"}


def run(mode_override):
    c = emu16.load(EXE)
    far_seg = emu16.FAR_HEAP_SEG
    for dg, far, data in hokraw.blocks(SAVE):
        if far:
            c.sdw(dg, 0)
            c.sdw(dg + 2, far_seg)
            for i, v in enumerate(data):
                c.wb(far_seg, i, v)
            far_seg += (len(data) + 15) // 16 + 1
        else:
            for i, v in enumerate(data):
                c.sdb(dg + i, v)
    mode = c.db(0xD0B9)
    if mode_override is not None:
        c.sdb(0xD0B9, mode_override)
    c.sdb(0xA70E, 0)  # WM loop step (c): 0xA70E = 0 after the matches (Datei 0x36B7A)
    c.allowed = [(0x07169, 0x07C12), (0x4BE17, 0x4BEBD)]  # 0x4BE17 counts the occupied places (real code)
    c.hooks[0x0C037] = lambda cc: cc.arg(0)
    c.max_steps = 5_000_000
    try:
        c.call(0x07169)
    except emu16.Stop as e:
        print("  Abbruch:", e)
    return mode, [(s, t) for s, t in c.calls]


def main():
    mode, a = run(None)
    _, b = run(0)
    print(f"Stand {Path(SAVE).name}: Spielmodus 0xD0B9 = {mode}, Manager 0, 0xA70E = 0")
    for label, calls in ((f"Modus {mode} (Stand)", a), ("Modus 0 (erzwungen)", b)):
        targets = {}
        for s, t in calls:
            targets.setdefault(t, []).append(s)
        print(f"{label}: {len(calls)} Aufrufe außerhalb")
        for t, srcs in sorted(targets.items()):
            print(f"   -> {t:#07x} {NAMES.get(t, ''):14s} {len(srcs):4d}x, erste Stelle {srcs[0]:#07x}")
    print("Aufruffolge gleich:", a == b)


if __name__ == "__main__":
    main()
