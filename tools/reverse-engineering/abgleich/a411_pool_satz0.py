"""A411: where does the original keep the squad of manager record 0 in mode 1 ("nur Weltkugel") in the player pool?

For each given save: mode (DGROUP 0xD0B9), the pool indices (+0x0F) of the occupied squad places of record 0 (DGROUP
0x5DC3 + 0x34 * place), the owner byte (pool far:8253 + 33 * i + 0x1A) of these pool places, and how many pool places
have owner 4 (transfer market) and which of them belong to record 5 (WM squad). Only numbers are printed.
Usage: python3 a411_pool_satz0.py [save ...]   (default FILE18 FILE19 FILE54 FILE80; saves dir ~/referenz/original/spielstaende)
"""
import sys
from collections import Counter
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from hokraw import Hok  # noqa: E402

FOLDER = Path.home() / "referenz/original/spielstaende"


def ranges(values):
    out, start, prev = [], None, None
    for v in values:
        if start is None:
            start = prev = v
        elif v == prev + 1:
            prev = v
        else:
            out.append((start, prev))
            start = prev = v
    if start is not None:
        out.append((start, prev))
    return ", ".join(f"{a}" if a == b else f"{a}-{b}" for a, b in out)


def main():
    names = sys.argv[1:] or ["FILE18", "FILE19", "FILE54", "FILE80"]
    for name in names:
        h = Hok(FOLDER / f"{name}.HOK")
        pool = h.block("pool")
        idx = [h.squad_slot(0, s)[0x0F] for s in range(29) if h.squad_slot(0, s)[0x0F]]
        owners = Counter(pool[33 * i + 0x1A] for i in idx)
        market = [i for i in range(1, 200) if pool[33 * i + 0x1A] == 4]
        wm = {h.squad_slot(5, s)[0x0F] for s in range(29) if h.squad_slot(5, s)[0x0F]}
        print(f"{name}: Modus {h.at(0x00314)[0]}, Satz 0 {len(idx)} Plätze, Pool {ranges(sorted(idx))}, "
              f"Besitzer {dict(owners)}; Markt (Besitzer 4) {len(market)} Plätze, davon im WM-Kader (Satz 5) {len(wm & set(market))}")


if __name__ == "__main__":
    main()
